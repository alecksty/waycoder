using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WayCoder.Infra;

namespace WayCoder.Sql;

/// <summary>
/// 精简 SQL 引擎 —— 纯 C# 手搓、零依赖、AOT 安全，替代移动端的 Microsoft.Data.Sqlite。
/// 支持 CREATE TABLE / INSERT / SELECT（WHERE、ORDER BY、LIMIT、聚合）/ UPDATE / DELETE / DROP TABLE，
/// 动态类型（NULL / INTEGER / REAL / TEXT），内存表 + 自定格式（JSON）持久化。
/// 不读取真实 .db/.sqlite 二进制文件（那是桌面端 sqlite3 CLI 的职责，见主工程 Tools/SqliteTool.cs）。
/// </summary>
public sealed class SqlDatabase
{
    private readonly Dictionary<string, Table> _tables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>执行一段（可含多条的）SQL，返回结果文本（查询=对齐表格，写操作=影响行数）。</summary>
    public string Execute(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return "（无 SQL 输入）";
        var sb = new StringBuilder();
        foreach (var stmt in SplitStatements(sql))
        {
            if (string.IsNullOrWhiteSpace(stmt)) continue;
            try { sb.Append(ExecuteOne(stmt)); }
            catch (SqlException ex) { sb.Append("错误：").Append(ex.Message).AppendLine(); }
        }
        var result = sb.ToString().TrimEnd();
        return string.IsNullOrEmpty(result) ? "（查询无结果）" : result;
    }

    // ─────────────────────────── 持久化（JSON + 类型标记）───────────────────────────

    /// <summary>从文件加载数据库（自定 JSON 格式）。文件不存在返回空库。</summary>
    public static SqlDatabase Load(string path)
    {
        var db = new SqlDatabase();
        if (!File.Exists(path)) return db;
        var node = Json.Parse(File.ReadAllText(path));
        if (node == null) return db;
        var tables = node.Get("tables");
        if (tables == null) return db;
        foreach (var tj in tables.Items)
        {
            var name = tj.GetString("name") ?? "";
            var cols = tj.Get("cols");
            var rows = tj.Get("rows");
            if (string.IsNullOrEmpty(name) || cols == null) continue;
            var table = new Table { Name = name };
            foreach (var c in cols.Items) table.Columns.Add(c.AsString() ?? "");
            if (rows != null)
            {
                foreach (var rj in rows.Items)
                {
                    var row = new object?[table.Columns.Count];
                    int i = 0;
                    foreach (var cell in rj.Items)
                    {
                        if (i >= row.Length) break;
                        row[i++] = DecodeValue(cell.AsString() ?? "");
                    }
                    table.Rows.Add(row);
                }
            }
            db._tables[table.Name] = table;
        }
        return db;
    }

    /// <summary>保存数据库到文件（自定 JSON 格式，自动建目录）。</summary>
    public void Save(string path)
    {
        var root = JNode.Object();
        var tables = JNode.Array();
        foreach (var t in _tables.Values)
        {
            var tj = JNode.Object();
            tj.Set("name", t.Name);
            var cols = JNode.Array();
            foreach (var c in t.Columns) cols.Add(c);
            tj.Set("cols", cols);
            var rows = JNode.Array();
            foreach (var row in t.Rows)
            {
                var rj = JNode.Array();
                foreach (var v in row) rj.Add(EncodeValue(v));
                rows.Add(rj);
            }
            tj.Set("rows", rows);
            tables.Add(tj);
        }
        root.Set("tables", tables);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, root.ToJson(indent: false), new UTF8Encoding(false));
    }

    /// <summary>单元格值 → 带类型标记的字符串（i:整数 / d:浮点 / s:文本 / n:null），保证 long/double 无损。</summary>
    private static string EncodeValue(object? v) => v switch
    {
        null => "n",
        long l => "i:" + l.ToString(CultureInfo.InvariantCulture),
        int i => "i:" + i.ToString(CultureInfo.InvariantCulture),
        double d => "d:" + d.ToString("R", CultureInfo.InvariantCulture),
        float f => "d:" + f.ToString("R", CultureInfo.InvariantCulture),
        string s => "s:" + s,
        _ => "s:" + v.ToString(),
    };

    /// <summary>带类型标记字符串 → 单元格值。</summary>
    private static object? DecodeValue(string s)
    {
        if (s == "n" || string.IsNullOrEmpty(s)) return s == "n" ? null : "";
        if (s.Length < 2) return s;
        var body = s[2..]; // 跳过 "x:" 前缀（ASCII，安全）
        return s[0] switch
        {
            'i' => long.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : body,
            'd' => double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : body,
            _ => body,
        };
    }

    // ─────────────────────────── 值工具（嵌套类共享）───────────────────────────

    internal static bool IsTruthy(object? v) => v switch
    {
        null => false,
        bool b => b,
        long l => l != 0,
        int i => i != 0,
        double d => d != 0,
        string s => s.Length > 0,
        _ => true,
    };

    internal static bool IsNumeric(object? v) => v is long or int or double or float;

    internal static double ToDouble(object? v) => v switch
    {
        long l => l,
        int i => i,
        double d => d,
        float f => f,
        _ => 0,
    };

    internal static int CompareValues(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;  // NULL 排最后
        if (b == null) return -1;
        if (IsNumeric(a) && IsNumeric(b))
        {
            var d = ToDouble(a) - ToDouble(b);
            return d < 0 ? -1 : d > 0 ? 1 : 0;
        }
        return string.CompareOrdinal(a.ToString(), b.ToString());
    }

    // ─────────────────────────── 语句执行 ───────────────────────────

    private string ExecuteOne(string stmt)
    {
        var tokens = Tokenize(stmt);
        if (tokens.Count == 0) return "";
        return new Parser(tokens, _tables).ParseAndExecute();
    }

    // ─────────────────────────── 词法分析 ───────────────────────────

    private enum TokKind { Word, Number, String, Sym, End }

    private readonly record struct Tok(TokKind Kind, string Text);

    private static List<Tok> Tokenize(string sql)
    {
        var result = new List<Tok>();
        int i = 0, n = sql.Length;
        while (i < n)
        {
            char c = sql[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '-' && i + 1 < n && sql[i + 1] == '-') { while (i < n && sql[i] != '\n') i++; continue; }
            if (c == '/' && i + 1 < n && sql[i + 1] == '*') { i += 2; while (i + 1 < n && !(sql[i] == '*' && sql[i + 1] == '/')) i++; i = Math.Min(i + 2, n); continue; }
            if (c == '\'' || c == '"')
            {
                char quote = c;
                var sb = new StringBuilder();
                i++;
                while (i < n)
                {
                    if (sql[i] == quote)
                    {
                        if (i + 1 < n && sql[i + 1] == quote) { sb.Append(quote); i += 2; }
                        else { i++; break; }
                    }
                    else { sb.Append(sql[i]); i++; }
                }
                result.Add(new Tok(TokKind.String, sb.ToString()));
                continue;
            }
            if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(sql[i + 1])))
            {
                int start = i;
                while (i < n && (char.IsDigit(sql[i]) || sql[i] == '.' || sql[i] == 'e' || sql[i] == 'E' || ((sql[i] == '+' || sql[i] == '-') && i > start && (sql[i - 1] == 'e' || sql[i - 1] == 'E')))) i++;
                result.Add(new Tok(TokKind.Number, sql[start..i]));
                continue;
            }
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < n && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_' || sql[i] == '$')) i++;
                result.Add(new Tok(TokKind.Word, sql[start..i]));
                continue;
            }
            if (i + 1 < n)
            {
                var two = sql.Substring(i, 2);
                if (two is "<=" or ">=" or "!=" or "<>" or "||")
                { result.Add(new Tok(TokKind.Sym, two)); i += 2; continue; }
            }
            result.Add(new Tok(TokKind.Sym, c.ToString()));
            i++;
        }
        result.Add(new Tok(TokKind.End, ""));
        return result;
    }

    // ─────────────────────────── 多语句拆分（字符串/注释状态机）───────────────────────────

    private static List<string> SplitStatements(string sql)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        char quote = '\0';
        bool lineComment = false, blockComment = false;
        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i], next = i + 1 < sql.Length ? sql[i + 1] : '\0';
            if (lineComment) { if (c == '\n') { lineComment = false; sb.Append(c); } continue; }
            if (blockComment) { if (c == '*' && next == '/') { blockComment = false; i++; } continue; }
            if (quote != '\0')
            {
                if (c == quote)
                {
                    if (next == quote) { sb.Append(c).Append(c); i++; }
                    else { quote = '\0'; sb.Append(c); }
                }
                else sb.Append(c);
                continue;
            }
            if (c == '-' && next == '-') { lineComment = true; i++; continue; }
            if (c == '/' && next == '*') { blockComment = true; i++; continue; }
            if (c == '\'' || c == '"') { quote = c; sb.Append(c); continue; }
            if (c == ';') { if (sb.Length > 0) result.Add(sb.ToString()); sb.Clear(); continue; }
            sb.Append(c);
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }

    // ─────────────────────────── 表结构 ───────────────────────────

    internal sealed class Table
    {
        public string Name = "";
        public List<string> Columns = new();
        public List<object?[]> Rows = new();

        public int ColIndex(string name)
        {
            for (int i = 0; i < Columns.Count; i++)
                if (string.Equals(Columns[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
    }

    internal sealed class SqlException(string message) : Exception(message);

    // ─────────────────────────── 解析器 + 执行器 ───────────────────────────

    private sealed class Parser
    {
        private readonly List<Tok> _tokens;
        private readonly Dictionary<string, Table> _tables;
        private int _pos;

        public Parser(List<Tok> tokens, Dictionary<string, Table> tables)
        {
            _tokens = tokens;
            _tables = tables;
        }

        private Tok Cur => _tokens[_pos];
        private bool IsEnd => Cur.Kind == TokKind.End;
        private void Advance() { if (!IsEnd) _pos++; }

        private bool EatWord(string kw)
        {
            if (Cur.Kind == TokKind.Word && string.Equals(Cur.Text, kw, StringComparison.OrdinalIgnoreCase)) { Advance(); return true; }
            return false;
        }

        private bool EatSym(string sym)
        {
            if (Cur.Kind == TokKind.Sym && Cur.Text == sym) { Advance(); return true; }
            return false;
        }

        private string ExpectIdentifier(string what)
        {
            if (Cur.Kind == TokKind.Word) { var t = Cur.Text; Advance(); return t; }
            if (Cur.Kind == TokKind.String) { var t = Cur.Text; Advance(); return t; } // 引号标识符
            throw new SqlException($"语法错误：{what} 处期望标识符");
        }

        public string ParseAndExecute()
        {
            if (EatWord("CREATE")) return ExecuteCreate();
            if (EatWord("INSERT")) return ExecuteInsert();
            if (EatWord("SELECT")) return ExecuteSelect();
            if (EatWord("UPDATE")) return ExecuteUpdate();
            if (EatWord("DELETE")) return ExecuteDelete();
            if (EatWord("DROP")) return ExecuteDrop();
            throw new SqlException($"不支持的语句：{Cur.Text}（仅支持 CREATE/INSERT/SELECT/UPDATE/DELETE/DROP）");
        }

        // ── CREATE TABLE name (col [TYPE] [constraints], ...) ──
        private string ExecuteCreate()
        {
            if (!EatWord("TABLE")) throw new SqlException("CREATE 后期望 TABLE");
            var name = ExpectIdentifier("表名");
            if (!EatSym("(")) throw new SqlException("CREATE TABLE 期望 '('");
            var table = new Table { Name = name };
            while (true)
            {
                if (IsEnd) throw new SqlException("CREATE TABLE 缺少 ')'");
                var col = ExpectIdentifier("列名");
                table.Columns.Add(col);
                SkipColumnDefTail();
                if (EatSym(",")) continue;
                if (EatSym(")")) break;
                throw new SqlException("CREATE TABLE 列定义后期望 ',' 或 ')'");
            }
            _tables[name] = table;
            return $"已创建表 {name}（{table.Columns.Count} 列：{string.Join(", ", table.Columns)}）\n";
        }

        /// <summary>跳过列定义的类型与约束（括号配对计数，避免把类型内的括号当分隔）。</summary>
        private void SkipColumnDefTail()
        {
            int depth = 0;
            while (!IsEnd)
            {
                if (Cur.Kind == TokKind.Sym && Cur.Text == "(") { depth++; Advance(); continue; }
                if (Cur.Kind == TokKind.Sym && Cur.Text == ")")
                {
                    if (depth == 0) return;
                    depth--; Advance(); continue;
                }
                if (Cur.Kind == TokKind.Sym && Cur.Text == "," && depth == 0) return;
                Advance();
            }
        }

        // ── INSERT INTO name [(cols)] VALUES (v,...), (v,...) ──
        private string ExecuteInsert()
        {
            if (!EatWord("INTO")) throw new SqlException("INSERT 后期望 INTO");
            var name = ExpectIdentifier("表名");
            if (!_tables.TryGetValue(name, out var table)) throw new SqlException($"表不存在：{name}");

            List<string>? cols = null;
            if (EatSym("("))
            {
                cols = new List<string>();
                while (true)
                {
                    cols.Add(ExpectIdentifier("列名"));
                    if (EatSym(",")) continue;
                    if (EatSym(")")) break;
                    throw new SqlException("INSERT 列清单期望 ',' 或 ')'");
                }
            }

            if (!EatWord("VALUES")) throw new SqlException("INSERT 期望 VALUES");

            int inserted = 0;
            do
            {
                if (!EatSym("(")) throw new SqlException("VALUES 后期望 '('");
                var values = new List<Expr>();
                if (!EatSym(")"))
                {
                    while (true)
                    {
                        values.Add(ParseExpr());
                        if (EatSym(",")) continue;
                        if (EatSym(")")) break;
                        throw new SqlException("VALUES 值清单期望 ',' 或 ')'");
                    }
                }

                int[] targetIdx;
                if (cols != null)
                {
                    targetIdx = new int[cols.Count];
                    for (int i = 0; i < cols.Count; i++)
                    {
                        int idx = table.ColIndex(cols[i]);
                        if (idx < 0) throw new SqlException($"列不存在：{cols[i]}");
                        targetIdx[i] = idx;
                    }
                }
                else
                {
                    if (values.Count != table.Columns.Count)
                        throw new SqlException($"INSERT 值数量({values.Count})与表列数({table.Columns.Count})不符");
                    targetIdx = new int[table.Columns.Count];
                    for (int i = 0; i < targetIdx.Length; i++) targetIdx[i] = i;
                }
                if (values.Count != targetIdx.Length)
                    throw new SqlException($"INSERT 值数量({values.Count})与目标列数({targetIdx.Length})不符");

                var row = new object?[table.Columns.Count];
                for (int i = 0; i < values.Count; i++)
                    row[targetIdx[i]] = values[i].Eval(null, table.Columns);
                table.Rows.Add(row);
                inserted++;
            }
            while (EatSym(","));

            return $"(已插入 {inserted} 行)\n";
        }

        // ── UPDATE name SET col=expr, ... [WHERE cond] ──
        private string ExecuteUpdate()
        {
            var name = ExpectIdentifier("表名");
            if (!_tables.TryGetValue(name, out var table)) throw new SqlException($"表不存在：{name}");
            if (!EatWord("SET")) throw new SqlException("UPDATE 期望 SET");

            var sets = new List<(int Col, Expr E)>();
            while (true)
            {
                var col = ExpectIdentifier("列名");
                int idx = table.ColIndex(col);
                if (idx < 0) throw new SqlException($"列不存在：{col}");
                if (!EatSym("=")) throw new SqlException("SET 期望 '='");
                sets.Add((idx, ParseExpr()));
                if (EatSym(",")) continue;
                break;
            }

            Expr? where = ParseWhere();
            int affected = 0;
            foreach (var row in table.Rows)
            {
                if (where != null && !SqlDatabase.IsTruthy(where.Eval(row, table.Columns))) continue;
                foreach (var (col, e) in sets) row[col] = e.Eval(row, table.Columns);
                affected++;
            }
            return $"(已影响 {affected} 行)\n";
        }

        // ── DELETE FROM name [WHERE cond] ──
        private string ExecuteDelete()
        {
            if (!EatWord("FROM")) throw new SqlException("DELETE 期望 FROM");
            var name = ExpectIdentifier("表名");
            if (!_tables.TryGetValue(name, out var table)) throw new SqlException($"表不存在：{name}");
            Expr? where = ParseWhere();
            int before = table.Rows.Count;
            table.Rows.RemoveAll(row => where != null && SqlDatabase.IsTruthy(where.Eval(row, table.Columns)));
            return $"(已删除 {before - table.Rows.Count} 行)\n";
        }

        // ── DROP TABLE name ──
        private string ExecuteDrop()
        {
            if (!EatWord("TABLE")) throw new SqlException("DROP 后期望 TABLE");
            var name = ExpectIdentifier("表名");
            if (_tables.Remove(name)) return $"已删除表 {name}\n";
            throw new SqlException($"表不存在：{name}");
        }

        // ── SELECT [DISTINCT] cols FROM name [WHERE] [ORDER BY] [LIMIT] ──
        private string ExecuteSelect()
        {
            bool distinct = EatWord("DISTINCT");

            var cols = new List<(string? Alias, Expr E)>();
            while (true)
            {
                cols.Add(ParseSelectItem());
                if (EatSym(",")) continue;
                break;
            }

            if (!EatWord("FROM")) throw new SqlException("SELECT 期望 FROM（本引擎不支持无表查询）");
            var name = ExpectIdentifier("表名");
            if (!_tables.TryGetValue(name, out var table)) throw new SqlException($"表不存在：{name}");

            Expr? where = ParseWhere();

            // ORDER BY col [ASC|DESC], ...（列名或 1-based 列序号）
            var orderBy = new List<(string Col, bool Desc)>();
            if (EatWord("ORDER"))
            {
                if (!EatWord("BY")) throw new SqlException("ORDER 期望 BY");
                while (true)
                {
                    string col;
                    if (Cur.Kind == TokKind.Number) { col = "#" + Cur.Text; Advance(); }
                    else col = ExpectIdentifier("排序列"); // ExpectIdentifier 内部已 Advance
                    bool desc = EatWord("DESC");
                    if (!desc) EatWord("ASC");
                    orderBy.Add((col, desc));
                    if (EatSym(",")) continue;
                    break;
                }
            }

            long limit = long.MaxValue, offset = 0;
            if (EatWord("LIMIT"))
            {
                if (Cur.Kind != TokKind.Number) throw new SqlException("LIMIT 期望数字");
                limit = long.Parse(Cur.Text); Advance();
                if (EatSym(",")) { offset = limit; if (Cur.Kind != TokKind.Number) throw new SqlException("LIMIT m,n 期望第二个数字"); limit = long.Parse(Cur.Text); Advance(); }
                else if (EatWord("OFFSET")) { if (Cur.Kind != TokKind.Number) throw new SqlException("OFFSET 期望数字"); offset = long.Parse(Cur.Text); Advance(); }
            }

            // 展开星号：SELECT * → 全部列；SELECT *, x → 全部列 + x
            var projected = new List<(string Header, Expr E)>();
            foreach (var (alias, e) in cols)
            {
                if (e is StarExpr)
                {
                    foreach (var c in table.Columns) projected.Add((c, new ColExpr(c)));
                }
                else
                {
                    projected.Add((alias ?? ExprText(e), e));
                }
            }
            var headers = projected.Select(p => p.Header).ToArray();

            bool hasAgg = projected.Any(p => p.E is AggExpr);
            if (hasAgg)
            {
                var row = new object?[projected.Count];
                for (int i = 0; i < projected.Count; i++)
                    row[i] = projected[i].E.EvalAgg(table, where);
                return RenderTable(headers, new List<object?[]> { row });
            }

            // 过滤（源表行）
            var matched = table.Rows.Where(r => where == null || SqlDatabase.IsTruthy(where.Eval(r, table.Columns))).ToList();

            // 排序（基于源表行，可引用任意原表列）
            if (orderBy.Count > 0)
            {
                matched.Sort((a, b) =>
                {
                    foreach (var (col, desc) in orderBy)
                    {
                        int c = SqlDatabase.CompareValues(ResolveOrderKey(col, a, table), ResolveOrderKey(col, b, table));
                        if (c != 0) return desc ? -c : c;
                    }
                    return 0;
                });
            }

            // 分页（在投影前分页，避免重复投影）
            var paged = matched.Skip((int)Math.Min(offset, int.MaxValue)).Take(limit >= int.MaxValue ? int.MaxValue : (int)limit).ToList();

            // 投影
            var outRows = new List<object?[]>();
            foreach (var src in paged)
            {
                var row = new object?[projected.Count];
                for (int i = 0; i < projected.Count; i++)
                    row[i] = projected[i].E.Eval(src, table.Columns);
                outRows.Add(row);
            }

            // 去重
            if (distinct)
            {
                var seen = new HashSet<string>();
                var deduped = new List<object?[]>();
                foreach (var r in outRows)
                {
                    var k = string.Join("", r.Select(v => v?.ToString() ?? " "));
                    if (seen.Add(k)) deduped.Add(r);
                }
                outRows = deduped;
            }

            return RenderTable(headers, outRows);
        }

        private object? ResolveOrderKey(string col, object?[] row, Table table)
        {
            if (col.Length > 1 && col[0] == '#')
            {
                // 1-based 列序号（ORDER BY 1）
                if (int.TryParse(col[1..], out var ord) && ord >= 1 && ord <= table.Columns.Count)
                    return row[ord - 1];
                return null;
            }
            int ci = table.ColIndex(col);
            return ci >= 0 && ci < row.Length ? row[ci] : null;
        }

        private (string? Alias, Expr E) ParseSelectItem()
        {
            if (EatSym("*")) return (null, new StarExpr());
            var e = ParseExpr();
            string? alias = null;
            if (EatWord("AS")) alias = ExpectIdentifier("别名");
            return (alias, e);
        }

        private static string ExprText(Expr e) => e switch
        {
            ColExpr c => c.Name,
            StarExpr => "*",
            _ => e.ToString() ?? "",
        };

        private Expr? ParseWhere()
        {
            if (!EatWord("WHERE")) return null;
            return ParseOr();
        }

        // ── 表达式（优先级：OR < AND < NOT < 比较 < 加减 < 乘除 < 一元 < 主）──
        private Expr ParseExpr() => ParseOr();

        private Expr ParseOr()
        {
            var left = ParseAnd();
            while (EatWord("OR")) left = new BinaryExpr("OR", left, ParseAnd());
            return left;
        }

        private Expr ParseAnd()
        {
            var left = ParseNot();
            while (EatWord("AND")) left = new BinaryExpr("AND", left, ParseNot());
            return left;
        }

        private Expr ParseNot()
        {
            if (EatWord("NOT")) return new UnaryExpr("NOT", ParseNot());
            return ParseCompare();
        }

        private Expr ParseCompare()
        {
            var left = ParseAdditive();
            if (EatWord("IS"))
            {
                bool negate = EatWord("NOT");
                if (!EatWord("NULL")) throw new SqlException("IS 后期望 NULL");
                return new IsNullExpr(left, negate);
            }
            var ops = new[] { "=", "!=", "<>", "<", ">", "<=", ">=" };
            foreach (var op in ops)
            {
                if (Cur.Kind == TokKind.Sym && Cur.Text == op)
                {
                    Advance();
                    return new BinaryExpr(op, left, ParseAdditive());
                }
            }
            if (EatWord("LIKE")) return new BinaryExpr("LIKE", left, ParseAdditive());
            if (EatWord("IN"))
            {
                if (!EatSym("(")) throw new SqlException("IN 期望 '('");
                var list = new List<Expr>();
                if (!EatSym(")"))
                {
                    while (true)
                    {
                        list.Add(ParseExpr());
                        if (EatSym(",")) continue;
                        if (EatSym(")")) break;
                    }
                }
                return new InExpr(left, list);
            }
            return left;
        }

        private Expr ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (Cur.Kind == TokKind.Sym && (Cur.Text == "+" || Cur.Text == "-" || Cur.Text == "||"))
            {
                var op = Cur.Text; Advance();
                left = new BinaryExpr(op, left, ParseMultiplicative());
            }
            return left;
        }

        private Expr ParseMultiplicative()
        {
            var left = ParseUnary();
            while (Cur.Kind == TokKind.Sym && (Cur.Text == "*" || Cur.Text == "/" || Cur.Text == "%"))
            {
                var op = Cur.Text; Advance();
                left = new BinaryExpr(op, left, ParseUnary());
            }
            return left;
        }

        private Expr ParseUnary()
        {
            if (EatSym("-")) return new UnaryExpr("-", ParseUnary());
            if (EatSym("+")) return ParseUnary();
            return ParsePrimary();
        }

        private Expr ParsePrimary()
        {
            if (EatSym("(")) { var e = ParseExpr(); if (!EatSym(")")) throw new SqlException("期望 ')'"); return e; }
            if (Cur.Kind == TokKind.Number)
            {
                var t = Cur.Text; Advance();
                if (t.IndexOf('.') < 0 && t.IndexOf('e') < 0 && t.IndexOf('E') < 0 && long.TryParse(t, out var li)) return new LiteralExpr(li);
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return new LiteralExpr(d);
                return new LiteralExpr(t);
            }
            if (Cur.Kind == TokKind.String) { var t = Cur.Text; Advance(); return new LiteralExpr(t); }
            if (EatWord("NULL")) return new LiteralExpr(null);
            if (EatWord("TRUE")) return new LiteralExpr(1L);
            if (EatWord("FALSE")) return new LiteralExpr(0L);
            if (Cur.Kind == TokKind.Word)
            {
                var w = Cur.Text; Advance();
                var upper = w.ToUpperInvariant();
                if (upper is "COUNT" or "SUM" or "AVG" or "MIN" or "MAX")
                {
                    if (!EatSym("(")) throw new SqlException($"聚合函数 {w} 期望 '('");
                    bool star = EatSym("*");
                    string? argCol = star ? null : ExpectIdentifier("聚合列名");
                    if (!EatSym(")")) throw new SqlException("聚合函数期望 ')'");
                    return new AggExpr(upper, star, argCol);
                }
                return new ColExpr(w);
            }
            throw new SqlException($"无法解析表达式：{Cur.Text}");
        }

        // ── 表格渲染 ──
        private static string RenderTable(string[] headers, List<object?[]> rows)
        {
            int cols = headers.Length;
            var cellRows = new List<string[]>();
            foreach (var r in rows)
            {
                var cells = new string[cols];
                for (int i = 0; i < cols; i++)
                    cells[i] = FormatCell(i < r.Length ? r[i] : null);
                cellRows.Add(cells);
            }

            var widths = new int[cols];
            for (int i = 0; i < cols; i++)
            {
                int w = headers[i].Length;
                foreach (var row in cellRows) w = Math.Max(w, row[i].Length);
                widths[i] = w;
            }

            var sb = new StringBuilder();
            sb.AppendLine(JoinRow(headers, widths));
            sb.AppendLine(JoinRow(headers.Select(h => new string('-', Math.Max(1, h.Length))).ToArray(), widths));
            foreach (var row in cellRows) sb.AppendLine(JoinRow(row, widths));
            if (cellRows.Count == 0) sb.AppendLine("(0 行)");
            return sb.ToString();
        }

        private static string JoinRow(string[] cells, int[] widths)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(cells[i].PadRight(widths[i]));
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatCell(object? value)
        {
            if (value == null) return "NULL";
            if (value is double d) return d.ToString("R", CultureInfo.InvariantCulture);
            if (value is float f) return f.ToString("R", CultureInfo.InvariantCulture);
            if (value is bool b) return b ? "1" : "0";
            var s = value.ToString() ?? "";
            return s.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
        }
    }

    // ─────────────────────────── 表达式 AST ───────────────────────────

    private abstract class Expr
    {
        public abstract object? Eval(object?[]? row, List<string> columns);
        public virtual object? EvalAgg(Table table, Expr? where) => Eval(null, table.Columns);
    }

    private sealed class LiteralExpr(object? value) : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns) => value;
    }

    private sealed class ColExpr(string name) : Expr
    {
        public string Name => name;
        public override object? Eval(object?[]? row, List<string> columns)
        {
            if (row == null) return null;
            for (int i = 0; i < columns.Count; i++)
                if (string.Equals(columns[i], name, StringComparison.OrdinalIgnoreCase)) return i < row.Length ? row[i] : null;
            return null;
        }
        public override string ToString() => name;
    }

    private sealed class StarExpr : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns) => null;
        public override string ToString() => "*";
    }

    private sealed class UnaryExpr(string op, Expr operand) : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns)
        {
            var v = operand.Eval(row, columns);
            return op switch
            {
                "-" => v switch { long l => -l, int i => -i, double d => -d, float f => -f, _ => null },
                "NOT" => !SqlDatabase.IsTruthy(v),
                _ => null,
            };
        }
    }

    private sealed class IsNullExpr(Expr operand, bool negate) : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns)
        {
            bool isNull = operand.Eval(row, columns) == null;
            return negate ? !isNull : isNull;
        }
    }

    private sealed class InExpr(Expr operand, List<Expr> list) : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns)
        {
            var v = operand.Eval(row, columns);
            foreach (var e in list)
            {
                var item = e.Eval(row, columns);
                if (v == null && item == null) return true;
                if (v != null && item != null && SqlDatabase.CompareValues(v, item) == 0) return true;
            }
            return false;
        }
    }

    private sealed class BinaryExpr(string op, Expr left, Expr right) : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns)
        {
            if (op == "AND") return SqlDatabase.IsTruthy(left.Eval(row, columns)) && SqlDatabase.IsTruthy(right.Eval(row, columns));
            if (op == "OR") return SqlDatabase.IsTruthy(left.Eval(row, columns)) || SqlDatabase.IsTruthy(right.Eval(row, columns));

            var a = left.Eval(row, columns);
            var b = right.Eval(row, columns);

            switch (op)
            {
                case "=": return a == null || b == null ? false : SqlDatabase.CompareValues(a, b) == 0;
                case "!=":
                case "<>": return a == null || b == null ? false : SqlDatabase.CompareValues(a, b) != 0;
                case "<": return a == null || b == null ? false : SqlDatabase.CompareValues(a, b) < 0;
                case ">": return a == null || b == null ? false : SqlDatabase.CompareValues(a, b) > 0;
                case "<=": return a == null || b == null ? false : SqlDatabase.CompareValues(a, b) <= 0;
                case ">=": return a == null || b == null ? false : SqlDatabase.CompareValues(a, b) >= 0;
                case "LIKE": return Like(a, b);
                case "+": return Arith(a, b, '+');
                case "-": return Arith(a, b, '-');
                case "*": return Arith(a, b, '*');
                case "/": return Arith(a, b, '/');
                case "%": return Arith(a, b, '%');
                case "||": return (a?.ToString() ?? "") + (b?.ToString() ?? "");
                default: return null;
            }
        }

        private static object? Arith(object? a, object? b, char op)
        {
            if (a == null || b == null) return null;
            bool aInt = a is long or int, bInt = b is long or int;
            if (aInt && bInt)
            {
                long x = Convert.ToInt64(a, CultureInfo.InvariantCulture), y = Convert.ToInt64(b, CultureInfo.InvariantCulture);
                return op switch
                {
                    '+' => x + y,
                    '-' => x - y,
                    '*' => x * y,
                    '/' => y == 0 ? null : x / y,
                    '%' => y == 0 ? null : x % y,
                    _ => null,
                };
            }
            if (SqlDatabase.IsNumeric(a) && SqlDatabase.IsNumeric(b))
            {
                double xd = SqlDatabase.ToDouble(a), yd = SqlDatabase.ToDouble(b);
                return op switch
                {
                    '+' => xd + yd,
                    '-' => xd - yd,
                    '*' => xd * yd,
                    '/' => yd == 0 ? null : xd / yd,
                    '%' => yd == 0 ? null : xd % yd,
                    _ => null,
                };
            }
            return null;
        }

        private static bool Like(object? a, object? b)
        {
            var s = a?.ToString() ?? "";
            var pattern = b?.ToString() ?? "";
            var re = new StringBuilder("^");
            foreach (var ch in pattern)
            {
                if (ch == '%') re.Append(".*");
                else if (ch == '_') re.Append('.');
                else re.Append(Regex.Escape(ch.ToString()));
            }
            re.Append('$');
            return Regex.IsMatch(s, re.ToString(), RegexOptions.Singleline);
        }
    }

    private sealed class AggExpr(string fn, bool star, string? col) : Expr
    {
        public override object? Eval(object?[]? row, List<string> columns) => null;
        public override object? EvalAgg(Table table, Expr? where)
        {
            int colIdx = star ? -1 : (col != null ? table.ColIndex(col) : -1);
            if (!star && colIdx < 0) return null;

            long count = 0;
            double sum = 0;
            object? min = null, max = null;
            bool any = false;

            foreach (var r in table.Rows)
            {
                if (where != null && !SqlDatabase.IsTruthy(where.Eval(r, table.Columns))) continue;
                if (star) { count++; continue; }
                var v = r[colIdx];
                if (v == null) continue;
                count++;
                any = true;
                if (SqlDatabase.IsNumeric(v)) sum += SqlDatabase.ToDouble(v);
                if (min == null || SqlDatabase.CompareValues(v, min) < 0) min = v;
                if (max == null || SqlDatabase.CompareValues(v, max) > 0) max = v;
            }

            return fn switch
            {
                "COUNT" => count,
                "SUM" => sum,
                "AVG" => count == 0 ? null : sum / count,
                "MIN" => any ? min : null,
                "MAX" => any ? max : null,
                _ => null,
            };
        }

        public override string ToString() => star ? $"{fn}(*)" : $"{fn}({col})";
    }
}
