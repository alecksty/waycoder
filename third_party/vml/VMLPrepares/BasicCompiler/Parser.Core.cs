using System.Text;
using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>
        /// 吃掉一个**名字**：`name` / `name.part` / `a.b.c`，返回**小写、`.` 已换成 `_`** 的整体名。
        ///
        /// <para>
        /// 为什么要有这一条：**老 BASIC 的标签名允许含 `.`** —— QBasic/QuickBASIC 的
        /// `Examples/basic/thirdparty/maze.bas` 整篇都是这种写法：
        /// <c>begin.of.editor:</c> / <c>GOSUB editor.newmaze</c> / <c>GOTO begin.of.editor</c>。
        /// 原来这三个地方各自只吃**一个** `IDENTIFIER`：
        /// </para>
        /// <list type="bullet">
        /// <item>标签定义认不出来（判据是"下一个 token 是 `:`"，而这里是 `.`）⇒
        /// <c>begin.of.editor:</c> 整行落进"未知语句"那条路，编成一次
        /// <c>CALL func_begin</c>；</item>
        /// <item>GOTO/GOSUB 只取到 `begin` ⇒ 又一处指向不存在的 `func_begin`。</item>
        /// </list>
        /// <para>
        /// 两者的共同后果是**链接期**报「未定义的函数 'func_begin'（引用 N 次）」——
        /// 那条消息指不到真正的行，也没说出"这是个标签名"。收成这一个方法之后，
        /// "什么算一个名字"只有一份实现，定义端与引用端不可能漂。
        /// </para>
        /// <para>
        /// ⚠ **`.` 换成 `_` 是刻意的**：VML 的 `.` 是**伪指令前缀**（`.string` / `.linked`），
        /// 带点的标签交给汇编器是一颗雷（"`a.b:`" 会被切成 `a` + 未知伪指令 `.b`）。
        /// 定义端与引用端都走这一个方法 ⇒ 换出来的名字必然一致。
        /// 代价：`a.b` 与 `a_b` 两个标签会撞名（老程序里几乎不会同时出现，认了）。
        /// </para>
        /// </summary>
        private string? TryParseDottedName()
        {
            if (AtEnd() || Peek().Type != TokenType.IDENTIFIER) return null;
            var sb = new System.Text.StringBuilder(Advance().Value.ToLowerInvariant());
            while (current + 1 < tokens.Count
                   && tokens[current].Type == TokenType.DOT
                   && tokens[current + 1].Type == TokenType.IDENTIFIER)
            {
                Advance();                                              // 吃掉 `.`
                sb.Append('_').Append(Advance().Value.ToLowerInvariant());
            }
            return sb.ToString();
        }

        /// <summary>
        /// 当前位置是不是一个**标签定义**：`name` / `a.b.c` 后面紧跟 `:`。
        ///
        /// 只看不消费（`TryParseDottedName` 的预读版）—— 调它之后位置不变。
        /// 与 `TryParseDottedName` 是同一套判据的两半，改一处必须改另一处。
        /// </summary>
        private bool DottedNameIsLabel()
        {
            if (AtEnd() || Peek().Type != TokenType.IDENTIFIER) return false;
            int j = current + 1;
            while (j + 1 < tokens.Count
                   && tokens[j].Type == TokenType.DOT
                   && tokens[j + 1].Type == TokenType.IDENTIFIER)
                j += 2;
            return j < tokens.Count && tokens[j].Type == TokenType.COLON;
        }

        /// <summary>
        /// 诊断取位置用**本文本解析器自己的**当前 Token。
        ///
        /// ⚠ 不覆写这一条，`ParserBase.ResolveDiagnosticPosition` 会读基类的 `Cur` ——
        ///   而本类把游标做成了自己的 `private int current`（`Peek`/`Advance`/`Previous`
        ///   都读写它），基类的 `_pos` **从不移动**、恒指向 `tokens[0]`
        ///   ⇒ 本前端报的**每条位置都落在第 1 行**。
        ///   实测（`.scratch/multi/t.bas` 那类输入）修之前是 `1:1`。
        ///
        /// 与 `CSharpCompiler.Parser` 那处**同源同因**（它也是自维护游标、也覆写本属性）——
        /// 判据是「你这门语言的当前位置在哪」，所以只有一处实现，不做"两个游标同步"
        /// （那正是本仓反复踩的「同一件事两处实现」）。
        /// </summary>
        protected override Token CurrentToken => Peek();

        private List<Token> tokens;
        private int current;
        private Dictionary<string, int> _enumValues = new(); // ENUM 成员→值映射 (v1.66.32+)

        /// <summary>
        /// `CONST 名 = 值` 的值表，**解析期**用。
        ///
        /// <para>
        /// 为什么解析器也得有一张：`DIM a(N) AS INTEGER` 里的维度要在**解析期**变成数字
        /// —— 数组大小要拿去分配存储。从前那条分支遇到标识符维度直接写死 `lowerBound = 1`
        /// （占位），注释还写着"实际大小在代码生成阶段算"，而**那个阶段根本没人算** ⇒
        /// `DIM terr(COLS)`（COLS=30）只分配出 2 格，`terr(c)` 一写就越界
        /// （LANDER 实测循环变量一路跑到 760、把地形数组和邻居变量一起写花）。
        /// </para>
        /// <para>
        /// ⚠ 只收**字面量**常量（`CONST N = 5`）。`CONST N = M * 2` 这种表达式这里解不了
        /// —— 那要常量折叠，而前端没有；查不到就退回原来的占位行为（不更糟）。
        /// 顺序上 `CONST` 必须写在 `DIM` **之前**（QBasic 也是这个要求）。
        /// </para>
        /// </summary>
        private readonly Dictionary<string, int> _constValues = new();

        /// <summary>
        /// **点名常量**：`AppSettings.cols = 8` ⇒ `"appsettings.cols" → 8`。
        ///
        /// <para>
        /// 只为 `DIM` 的维度服务。老程序把数组尺寸写在记录字段里是常见写法：
        /// <c>DIM SHARED Terrain(AppSettings.cols, AppSettings.rows) AS TerrainType</c>
        /// （`thirdparty/w84d_spaceship.bas` / `w84d_desert_rally.bas`）。解析器解不了
        /// 运行期的字段值，但那个值在**同一份源码里以字面量赋过一次**，扫一遍就得到了。
        /// </para>
        /// <para>
        /// ⚠ **先到先得**（后面的同字面量赋值不覆盖）：拿到的应当是初始化那一句，
        /// 而不是循环体里的 `AppSettings.cols = i`。这是个**启发式**，判据只是
        /// "比拿不到强" —— 拿不到的后果是数组只分到 2 格、一写就越界。
        /// 扫不到就退回原来的占位行为，不更糟（与 `_constValues` 同一约定）。
        /// </para>
        /// </summary>
        private readonly Dictionary<string, int> _fieldConstValues = new();

        /// <summary>`DIM` 维度取值的唯一入口：先查 `CONST` 常量表，再查点名表。</summary>
        private bool TryLookupConstValue(string name, out int value)
        {
            string key = name.ToLowerInvariant();
            return _constValues.TryGetValue(key, out value)
                || _fieldConstValues.TryGetValue(key, out value);
        }

        /// <summary>
        /// 把一个**常量表达式**折成整数 —— 只认 `字面量` / `已登记的常量名` /
        /// `+ - * /` 的组合（`PAREN` 由表达式树本身消化了）。
        ///
        /// <para>
        /// 为什么要它：`CONST GCELLS = GW * GH` 这种**引用别的常量**的写法很常见，
        /// 而只收字面量的话 `GCELLS` 进不了表 ⇒ `DIM bd(GCELLS)` 分不到格子
        /// （实测只分到 1 格、后面全写越界，表现为"空格数 0、右下角不是墙"）。
        /// </para>
        /// <para>
        /// 折不了就返回 false —— 调用方退回占位行为，**不更糟**。别在这里追求完备：
        /// 真正需要的是"常见写法能过"，不是实现一个完整的常量折叠。
        /// </para>
        /// </summary>
        private bool TryEvalConst(Expression e, out int value)
        {
            value = 0;
            if (e is NumberLiteral num)
            {
                value = (int)num.Value;
                return true;
            }
            if (e is Identifier id)
                return _constValues.TryGetValue(id.Name.ToLower(), out value);
            if (e is BinaryExpression bin)
            {
                if (!TryEvalConst(bin.Left, out int lv)) return false;
                if (!TryEvalConst(bin.Right, out int rv)) return false;
                switch (bin.Operator)
                {
                    case "+": value = lv + rv; return true;
                    case "-": value = lv - rv; return true;
                    case "*": value = lv * rv; return true;
                    case "/":
                        if (rv == 0) return false;
                        value = lv / rv;
                        return true;
                    default: return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 源文件里出现过 `OPTION EXPLICIT` ⇒ **变量必须先声明**，未声明的引用报**错误**；
        /// 没出现则报**警告**（QBasic 默认「未声明即隐式全局、值 0」是合法语义）。
        ///
        /// 这是**每文件**的开关（不是每语言），由 <c>BasicCompiler</c> 从解析器带给代码生成。
        /// </summary>
        public bool OptionExplicit { get; private set; }

        /// <summary>当前 BASIC 方言 (v1.66.32+)</summary>
        private VMLPlugins.BasicDialect CurrentDialect =>
            VMLPlugins.CompilerOptionsContext.Current.BasicDialect;

        /// <summary>检查当前方言</summary>
        private bool IsDialect(VMLPlugins.BasicDialect d) => CurrentDialect == d;
        private bool IsAnyDialect(params VMLPlugins.BasicDialect[] ds) => ds.Contains(CurrentDialect);
        private HashSet<string> declaredArrays;
        private bool _pendingStdCall;
        private bool _pendingNative;
        private HashSet<string> declaredFunctions;
        private HashSet<string> declaredSubs;
        private HashSet<string> declaredFnFunctions;
        private Dictionary<string, string> declaredArrayTypes = new Dictionary<string, string>();

        public Parser(List<Token> tokens) : base(tokens)
        {
            this.tokens = tokens;
            current = 0;
            declaredArrays = new HashSet<string>();
            declaredFunctions = new HashSet<string>();
            declaredSubs = new HashSet<string>();
            declaredFnFunctions = new HashSet<string>();
        }

        /// <summary>
        /// 预扫一遍 token 流，把 `点名 = 字面量` 这一种赋值收进 <see cref="_fieldConstValues"/>。
        ///
        /// <para>
        /// 之所以是**预扫**而不是"解析到赋值时顺手记"：`DIM` 与那句赋值**谁先谁后都有可能**
        /// （实测两份语料里 `AppSettings.cols = 8` 在前、但那是巧合，不能依赖顺序）。
        /// </para>
        /// <para>
        /// ⚠ 判据要**整条点名连起来**再看等号：只看 `a . b` 三个 token 的话，
        /// <c>IF a.b &lt;&gt; 8 THEN</c> 之类的比较也会被当成赋值。
        /// 只认**字面量**右值（`= 8`），认不出的不记 —— 与 `_constValues` 同一约定。
        /// </para>
        /// </summary>
        private void CollectFieldConstants()
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type != TokenType.IDENTIFIER) continue;
                if (i + 2 >= tokens.Count) break;
                if (tokens[i + 1].Type != TokenType.DOT || tokens[i + 2].Type != TokenType.IDENTIFIER) continue;

                var sb = new StringBuilder(tokens[i].Value.ToLowerInvariant());
                int j = i;
                while (j + 2 < tokens.Count
                       && tokens[j + 1].Type == TokenType.DOT && tokens[j + 2].Type == TokenType.IDENTIFIER)
                {
                    sb.Append('.').Append(tokens[j + 2].Value.ToLowerInvariant());
                    j += 2;
                }

                if (j + 2 < tokens.Count && tokens[j + 1].Type == TokenType.EQUALS
                    && tokens[j + 2].Type == TokenType.NUMBER
                    && int.TryParse(tokens[j + 2].Value, out int v))
                {
                    _fieldConstValues.TryAdd(sb.ToString(), v);   // 先到先得
                }
            }
        }

        public BasicProgram Parse()
        {
            CollectFieldConstants();

            // First pass: collect SUB/FUNCTION/DIM/DEF FN declarations
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == TokenType.DIM && i + 1 < tokens.Count)
                {
                    // `DIM [SHARED] 名[(维度)] [AS 类型] [, 名…] …`
                    //
                    // ⚠⚠ **只扫本行**（`tokens[].Line` 相同）。词法里没有换行 token，
                    //   而这里从前是「从 `DIM` 后面一路扫到下一个 `:` 或 EOF」——
                    //   也就是**扫到文件尾**（BASIC 里很少写 `:`）。于是 `DIM SHARED r(3)`
                    //   后面**整个文件的每个标识符**都被登记成"数组"：子过程名、形参名、
                    //   局部变量名全在内。
                    //   后果是**顺序相关**的怪病：`SUB t (Rec(), n)` 写在 `DIM SHARED r(3)`
                    //   **之前**时 `Rec` 没被登记 ⇒ 读侧 `Rec(n)` 被当成函数调用、
                    //   报「未定义的函数 'func_rec'」；写在**之后**反而"好了"——
                    //   靠的是扫描越界误打误撞把 `Rec` 收进了数组表。
                    //   实测 GORILLA.BAS 的 `SUB UpdateScores (Record(), …)` 就是这一条。
                    //
                    //   括号深度也要跟：`DIM SHARED LBan&(x), …` 里的 `x` 是**维度表达式**
                    //   里引用的常量，不是被声明的名字（从前它也被收进 declaredArrays）。
                    bool isShared = tokens[i + 1].Type == TokenType.SHARED;
                    int nameStart = isShared ? i + 2 : i + 1;
                    int dimLine = tokens[i].Line;
                    if (nameStart < tokens.Count && tokens[nameStart].Type == TokenType.IDENTIFIER)
                    {
                        int parenDepth = 0;
                        for (int j = nameStart; j < tokens.Count; j++)
                        {
                            if (tokens[j].Line != dimLine) break;              // 换行 = DIM 语句结束
                            if (tokens[j].Type == TokenType.COLON) break;      // 同行冒号 = 语句分隔
                            if (tokens[j].Type == TokenType.LPAREN) { parenDepth++; continue; }
                            if (tokens[j].Type == TokenType.RPAREN) { if (parenDepth > 0) parenDepth--; continue; }
                            if (tokens[j].Type != TokenType.IDENTIFIER || parenDepth > 0) continue;

                            string arrName = tokens[j].Value;
                            declaredArrays.Add(arrName);

                            // 向后找 `AS 类型名`（同样收在本行、且必须在括号之外）
                            int depth2 = 0;
                            for (int k = j + 1; k < tokens.Count && tokens[k].Line == dimLine; k++)
                            {
                                if (tokens[k].Type == TokenType.LPAREN) depth2++;
                                else if (tokens[k].Type == TokenType.RPAREN) depth2--;
                                else if (tokens[k].Type == TokenType.COLON && depth2 == 0) break;
                                else if (depth2 == 0 && tokens[k].Type == TokenType.AS
                                         && k + 1 < tokens.Count && IsTypeNameToken(tokens[k + 1]))
                                {
                                    declaredArrayTypes[arrName] = tokens[k + 1].Value;
                                    break;
                                }
                            }
                        }
                    }
                }
                // ⚠ `SUB <名>` 必须先排除 `END SUB` —— 词法里没有换行 token，
                //   而 `END SUB` 后面紧跟的**主程序第一条语句**往往就是一个标识符：
                //     `SUB t() / … / END SUB / q = 1`
                //   于是 `SUB q` 被当成了「又声明了一个叫 q 的子过程」，
                //   后面那条真语句就被 `case TokenType.IDENTIFIER` 的
                //   `declaredSubs.Contains` 判成**裸调用**，链接期报「未定义的函数 'func_q'」。
                //   症状极具误导性：**报错的那一行本身完全合法**，删掉它就"修好了"，
                //   而真凶是前面那个 END（实测：只要 END SUB 与 `q = 1` 之间**夹任意一条语句**
                //   就一切正常 —— 那只是因为夹的那条先被消费掉了）。
                //
                // ⚠⚠ 但「前一个 token 是 END」**不等于**「这是 END SUB」—— 见
                //   `IsCompoundEnd` 的长注释：QBasic 的标准写法是主程序 `END` + 之后
                //   全部子程序，那时 `SUB <名>` 的前一个 token **正是那个独立的 END**。
                //   所以判据必须带上**行号**（`END SUB` 写在同一行）。
                else if (tokens[i].Type == TokenType.SUB && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.IDENTIFIER
                         && !IsCompoundEnd(i - 1))
                {
                    declaredSubs.Add(tokens[i + 1].Value);
                    CollectArrayParams(i + 2);
                }
                else if (tokens[i].Type == TokenType.FUNCTION && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.IDENTIFIER
                         && !IsCompoundEnd(i - 1))
                {
                    declaredFunctions.Add(tokens[i + 1].Value);
                    CollectArrayParams(i + 2);
                }
                else if (tokens[i].Type == TokenType.DECLARE && i + 1 < tokens.Count)
                {
                    if (tokens[i + 1].Type == TokenType.SUB && i + 2 < tokens.Count && tokens[i + 2].Type == TokenType.IDENTIFIER)
                    {
                        declaredSubs.Add(tokens[i + 2].Value);
                        CollectArrayParams(i + 3);
                    }
                    else if (tokens[i + 1].Type == TokenType.FUNCTION && i + 2 < tokens.Count && tokens[i + 2].Type == TokenType.IDENTIFIER)
                    {
                        declaredFunctions.Add(tokens[i + 2].Value);
                        CollectArrayParams(i + 3);
                    }
                }
                else if (tokens[i].Type == TokenType.ENUM_KW && i + 1 < tokens.Count)
                {
                    // ENUM: 收集成员为隐式常量 (v1.66.32+)
                    int j = i + 1; if (tokens[j].Type == TokenType.IDENTIFIER) j++;
                    int val = 0;
                    while (j < tokens.Count && tokens[j].Type != TokenType.END)
                    {
                        if (tokens[j].Type == TokenType.IDENTIFIER)
                        {
                            string n = tokens[j].Value; j++;
                            if (j < tokens.Count && tokens[j].Type == TokenType.EQUALS)
                                { j++; if (j < tokens.Count && tokens[j].Type == TokenType.NUMBER) { val = int.Parse(tokens[j].Value); j++; } }
                            _enumValues[n] = val++;
                        }
                        else j++;
                    }
                    while (j < tokens.Count && tokens[j].Type != TokenType.ENUM_KW) j++;
                    i = j;
                }
                else if (tokens[i].Type == TokenType.DEF_KW && i + 1 < tokens.Count)
                {
                    if (tokens[i + 1].Type == TokenType.IDENTIFIER)
                    {
                        string ident = tokens[i + 1].Value;
                        if (ident.StartsWith("FN") || ident.StartsWith("fn"))
                        {
                            declaredFunctions.Add(ident);
                            declaredFnFunctions.Add(ident);
                        }
                    }
                    else if (tokens[i + 1].Type == TokenType.FN_KW && i + 2 < tokens.Count && tokens[i + 2].Type == TokenType.IDENTIFIER)
                    {
                        string fnName = "FN" + tokens[i + 2].Value;
                        declaredFunctions.Add(fnName);
                        declaredFnFunctions.Add(fnName);
                    }
                }
            }

            BasicProgram program = new BasicProgram(1, 1);

            while (!AtEnd())
            {
                // 跳过 : 分隔符（BASIC 语句分隔符）
                if (Peek().Type == TokenType.COLON)
                {
                    Advance();
                    continue;
                }
                var stmt = ParseStatement();
                if (stmt != null)
                    program.Statements.Add(stmt);
            }

            // Propagate array type info discovered in first pass
            foreach (var kv in declaredArrayTypes)
                program.ArrayTypes[kv.Key] = kv.Value;
            foreach (var kv in _enumValues)
                program.EnumValues[kv.Key] = kv.Value;

            return program;
        }

        /// <summary>
        /// 当前这条语句**是不是赋值** —— 判据是「本行内、括号深度为 0 处出现 `=`」。
        ///
        /// 词法里没有换行 token，所以"本行"要靠 token 的 `Line` 判定（语句不跨行）。
        /// 括号深度是为了不把 `arr(f(1) = 2)` 这种（虽怪但合法）的下标表达式看成本行的 `=`。
        /// 字符串里的 `=` 天然安全 —— 它整个是一个 STRING token，不会被认成 EQUALS。
        /// </summary>
        private bool HasTopLevelEqualsOnLine()
        {
            if (current >= tokens.Count) return false;
            int line = tokens[current].Line;
            int depth = 0;
            for (int j = current; j < tokens.Count; j++)
            {
                var t = tokens[j];
                if (t.Type == TokenType.EOF) break;
                // 换行 = 语句结束（`a = 1 : b = 2` 那种同行冒号分隔由外面拆开，这里不越过冒号）
                if (t.Line != line) break;
                if (depth == 0)
                {
                    if (t.Type == TokenType.EQUALS) return true;
                    if (t.Type == TokenType.COLON) break;
                }
                if (t.Type == TokenType.LPAREN) depth++;
                else if (t.Type == TokenType.RPAREN) { if (depth > 0) depth--; }
            }
            return false;
        }

        /// <summary>
        /// 跳过**当前语句**剩余的所有 token —— 到 `:`（同行还有下一条语句）或换行/EOF 为止。
        ///
        /// <para>
        /// 与 <see cref="SkipToNextLine"/> 的差别只有 `:` 这一条，而它要紧：老 BASIC 里
        /// `PCOPY 0, 1: PRINT "x"` / `SHELL "cmd": END` 这种同行接续很常见。按"整行"
        /// 跳会把冒号后面那条**真实语句**一起吃掉，而跳掉的又是我们本来就没实现的东西
        /// ⇒ 症状是"程序少做了一件事"，且一声不响（与本文件里反复记的
        /// 「语句层对 null 是静默跳过」同一个坑）。
        /// </para>
        /// </summary>
        private void SkipRestOfStatement()
        {
            if (AtEnd()) return;
            int currentLine = Peek().Line;
            while (!AtEnd() && Peek().Line == currentLine && Peek().Type != TokenType.COLON)
                Advance();
        }

        /// <summary>
        /// 跳过当前行剩余的所有 token（到换行或 EOF）
        /// </summary>
        private void SkipToNextLine()
        {
            if (AtEnd()) return;
            int currentLine = Peek().Line;
            while (!AtEnd() && Peek().Line == currentLine)
                Advance();
        }

        /// <summary>
        /// `tokens[endIdx]` 是不是一个**复合结束语句**（`END SUB` / `END FUNCTION` /
        /// `END IF` / `END SELECT` / `END TYPE` / `END CLASS`）的开头。
        ///
        /// <para>
        /// <b>为什么需要行号这个维度</b>：BASIC 的词法器**不产出换行 token**，于是
        /// 「这一行的 `END` 后面紧跟着下一个 token」在 token 流里**与「跨行的 END 后面
        /// 跟别的东西」完全同形**。只看 token 相邻的判据会把这两种情况混为一谈，
        /// 而 QBasic 程序里**恰恰**到处都是第二种写法 —— 主程序以一条独立的 `END`
        /// 收尾，**全部 SUB/FUNCTION 与 DATA 都写在它后面**（微软官方的
        /// `GORILLA.BAS` 就是这个形状）。
        /// </para>
        ///
        /// <para>
        /// 混起来的后果（修前实测，`--lang basic` 一个 7 行的最小复现）：
        /// `<c>PRINT f(2) / END / FUNCTION f(x) … END FUNCTION</c>` 里的 `FUNCTION`
        /// 被这条 END 吃掉 ⇒ ① `f` 进不了 <c>declaredFunctions</c>，
        /// 调用点编成 `CALL func_f` ⇒ 链接期报「未定义的函数 'func_f'」；
        /// ② 函数体那几行被当成了**主程序的普通语句**，`FUNCTION f(x)` 那行
        /// 甚至被编成一次对 `f` 的自调用 ⇒ **主程序顺着往下跑进子程序**。
        /// 修前那份产物的 `.text` 里 `main:` 与 `func_f:` 是**同一个地址**
        /// （`Labels["func_f"]` 从未被填过，被链接器的"裸名别名"兜到了变量 `f` 的 0 号地址），
        /// 运行时 `call func_f` 跳回 main 自己 ⇒ 无限递归、报「内存不足，无法分配」。
        /// </para>
        ///
        /// <para>
        /// 判据取「同一个源行」：`END SUB` / `END IF` 在 QBasic 里本就是**一条**语句，
        /// 不允许拆成两行写；而独立 `END` 与它后面的语句**必然不在同一行**
        /// （同一行要写成 `END : FUNCTION f(x)` 才是合法的同一行序列，那时也确实
        /// 应该按"同行"处理）。所以这个判据与 QBasic 的语法边界一致，不是启发式。
        /// </para>
        ///
        /// <para>越界（<paramref name="endIdx"/> 落在范围外 / 其后无 token）一律返回 false。</para>
        /// </summary>
        private bool IsCompoundEnd(int endIdx)
        {
            return IsEndOfBlock(endIdx, TokenType.IF) || IsEndOfBlock(endIdx, TokenType.SELECT)
                || IsEndOfBlock(endIdx, TokenType.SUB) || IsEndOfBlock(endIdx, TokenType.FUNCTION)
                || IsEndOfBlock(endIdx, TokenType.TYPE_KW) || IsEndOfBlock(endIdx, TokenType.CLASS_KW);
        }

        /// <summary>
        /// `tokens[endIdx]` 与 `tokens[endIdx+1]` 是不是同一个源行上的 `END &lt;关键字&gt;`。
        /// 这是「`END X` 是一条复合语句」的**唯一判据**（`IsCompoundEnd` 与各处
        /// `END SUB`/`END FUNCTION` 的体循环共用它 —— 判据写两份必然漂移）。
        /// </summary>
        /// <summary>
        /// 把 `SUB`/`FUNCTION`/`DECLARE …` **形参表里带 `()` 的参数**登记成数组。
        ///
        /// <para>
        /// 判据：形参表（括号深度 ≥ 1 的范围内）里**紧跟着 `(`** 的那个标识符就是数组形参
        /// （`SUB t (BCoor(), n)` / `SUB UpdateScores (Record(), PlayerNum, Results)`）。
        /// </para>
        /// <para>
        /// <b>为什么必须有这一步</b>：`declaredArrays` 此前**只从 `DIM` 语句收**，而数组
        /// 形参根本不是 `DIM` 出来的 ⇒ 它在体内被读到时（`Record(n) = Record(n) + 1`
        /// 的**读侧**）会被当成**函数调用**、链接期报「未定义的函数 'func_record'」。
        /// 而写侧（`= ` 左边）走的是 `HasTopLevelEqualsOnLine` 那条路，能正常编成数组访问
        /// ——于是症状是「**同一行里，写进去对、读出来报错**」，很容易被当成"数组读坏了"。
        /// GORILLA.BAS 的 `SUB UpdateScores (Record(), …)` 就是这一条。
        /// </para>
        /// <para>
        /// （此前它只是"碰巧"能编：`DIM SHARED` 那条扫描越界，把后面**整个文件**的
        /// 标识符都收进了 `declaredArrays`，`Record` 顺带被收进去 —— 于是还呈**顺序相关**：
        /// 声明写在 `DIM SHARED` 之前就报错、写在之后反而"正常"。那个越界扫描已修，见上。）
        /// </para>
        /// </summary>
        private void CollectArrayParams(int openParenIdx)
        {
            if (openParenIdx >= tokens.Count || tokens[openParenIdx].Type != TokenType.LPAREN) return;
            int depth = 0;
            for (int j = openParenIdx; j < tokens.Count; j++)
            {
                if (tokens[j].Type == TokenType.LPAREN)
                {
                    // `名(` 就是数组形参（返回类型/类型名后面不会直接跟 `(`）。
                    if (depth >= 1 && j > 0 && tokens[j - 1].Type == TokenType.IDENTIFIER)
                        declaredArrays.Add(tokens[j - 1].Value);
                    depth++;
                    continue;
                }
                if (tokens[j].Type == TokenType.RPAREN)
                {
                    depth--;
                    if (depth <= 0) break;   // 形参表结束
                }
                if (tokens[j].Type == TokenType.EOF) break;
            }
        }

        private bool IsEndOfBlock(int endIdx, TokenType keyword)
        {
            if (endIdx < 0 || endIdx + 1 >= tokens.Count) return false;
            if (tokens[endIdx].Type != TokenType.END || tokens[endIdx + 1].Type != keyword) return false;
            // 行号是 1-based；两个 token 的行号相同 = 同一个源行。
            return tokens[endIdx + 1].Line == tokens[endIdx].Line;
        }

        private Statement ParseStatement()
        {
            Token token = Peek();

            switch (token.Type)
            {
                case TokenType.PRINT:
                    if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.USING_KW)
                        return ParsePrintUsingStatement();
                    return ParsePrintStatement();
                case TokenType.INPUT:
                    return ParseInputStatement();
                case TokenType.LET:
                    return ParseLetStatement();
                case TokenType.IF:
                    return ParseIfStatement();
                case TokenType.GOTO:
                    return ParseGotoStatement();
                case TokenType.GOSUB:
                    return ParseGosubStatement();
                case TokenType.RETURN:
                    return ParseReturnStatement();
                case TokenType.FOR:
                    return ParseForStatement();
                case TokenType.DIM:
                    return ParseDimStatement();
                case TokenType.WHILE:
                    return ParseWhileStatement();
                case TokenType.DO:
                    return ParseDoLoopStatement();
                case TokenType.END:
                    // 处理 END IF/SELECT/SUB — 同时消耗 END 和后续关键字
                    //
                    // ⚠ **必须判"同一个源行"**（`IsCompoundEnd` 的长注释记着完整来龙去脉）。
                    //   只看"下一个 token 是 SUB/FUNCTION"的话，QBasic 最标准的那个形状
                    //   —— 主程序一条独立的 `END` 收尾、**全部子程序写在它后面** ——
                    //   会被整片吃掉：`FUNCTION` 被当成了 `END FUNCTION` 的后半截，
                    //   于是函数体变成主程序的一部分、函数名进不了 declaredFunctions。
                    if (IsCompoundEnd(current))
                    {
                        Advance(); // skip END
                        Advance(); // skip IF/SELECT/SUB/FUNCTION/TYPE/CLASS
                        return null;
                    }
                    return ParseEndStatement();
                case TokenType.STDCALL:
                    _pendingStdCall = true;
                    Advance();
                    return ParseStatement(); // 递归解析下一个语句
                case TokenType.NATIVE:
                    _pendingNative = true;
                    Advance();
                    return ParseStatement(); // 递归解析下一个语句
                case TokenType.SUB:
                    var sub = ParseSubDeclaration();
                    if (sub != null) { sub.IsStdCall = _pendingStdCall; sub.IsNative = _pendingNative; }
                    _pendingStdCall = false;
                    _pendingNative = false;
                    return sub;
                case TokenType.FUNCTION:
                    var func = ParseFunctionDeclaration();
                    if (func != null) { func.IsStdCall = _pendingStdCall; func.IsNative = _pendingNative; }
                    _pendingStdCall = false;
                    _pendingNative = false;
                    return func;
                case TokenType.CALL:
                    return ParseCallStatement();
                case TokenType.EXIT:
                    return ParseExitStatement();
                case TokenType.SELECT:
                    return ParseSelectCaseStatement();
                // DECLARE SUB/FUNCTION — 前向声明
                case TokenType.DECLARE:
                    return ParseDeclareStatement();
                // CONST 常量
                case TokenType.CONST_KW:
                    return ParseConstStatement();
                // TYPE...END TYPE 结构
                case TokenType.TYPE_KW:
                    return ParseTypeDeclaration();
                // CLASS...END CLASS OOP (FreeBasic v1.66.31+)
                case TokenType.CLASS_KW:
                    return ParseClassDeclaration();
                // 文件操作
                case TokenType.OPEN:
                    return ParseOpenStatement();
                case TokenType.CLOSE:
                    return ParseCloseStatement();
                // ON ERROR GOTO
                case TokenType.ON:
                    return ParseOnErrorStatement();
                // DATA 数据语句 —— **必须进 AST**（v0.96.330 修）。
                //
                // ⚠ 从前这里是 `SkipToNextLine(); return null;`，注释写着"编译期跳过、
                //   运行时由 READ 读取" —— 但"跳过"的是**整条语句**：值从来没进过 AST，
                //   于是 `CollectVariablesAndLabels` 收集到的 `dataValues` 永远是空的，
                //   序言里那句"把 DATA 值写进静态区"的循环一次都不执行 ⇒ READ 读到的是
                //   未初始化内存（恒为 0）。而真正的实现 `ParseDataStatement()` 就在
                //   `Parser.Qbasic.cs` 里、**一个调用点都没有**，是死代码。
                //   实测：`DATA 11,22` + `READ a,b` 打出 `0 0`。
                case TokenType.DATA:
                    return ParseDataStatement();
                // READ/RESTORE/RESUME
                case TokenType.READ_KW:
                    return ParseReadStatement();
                case TokenType.RESTORE:
                    return ParseRestoreStatement();
                case TokenType.RESUME:
                    return ParseResumeStatement();
                // ── 老程序兼容：「接受并空转」的语句 ──────────────────────────────
                //
                // 判据是**本平台没有这个东西的语义**，而不是"懒得实现"：
                //   · `PCOPY a, b` —— 视频页复制。本平台没有分页显存，复制到哪一页
                //     都不会改变屏幕上的东西（tty/BGI 两条路都是直接落屏）。
                //   · `SHELL "cmd"` —— 起子进程。运行期不给子进程，空转是唯一安全的答复。
                //   · `WRITE` —— 与 `PRINT #n` 同形（见 `ParsePrintStatement` 的 `#` 分支），
                //     交给同一份输出实现，别各自再写一遍。
                //
                // 关键是**必须把剩余 token 吃掉**：`PCOPY 0, 1` 里的 `0, 1` 如果留在流上，
                // 语句层会把它们当新语句，而 `0` 恰好落进「NUMBER 开头的行号」那条分支，
                // 编出一段莫名其妙的跳转。空转 = 收掉这一条语句，不是"什么都不做"。
                case TokenType.PCOPY:
                    Advance();              // skip PCOPY
                    SkipRestOfStatement();
                    return null;
                case TokenType.SHELL:
                    Advance();              // skip SHELL
                    SkipRestOfStatement();
                    return null;
                case TokenType.WRITE_KW:
                    // `WRITE #n, …` 与 `PRINT #n, …` 同形 ⇒ 复用同一份解析，
                    // 输出到本平台的常规输出通道（`#n` 无独立流语义，与 INPUT 一致）。
                    return ParsePrintStatement();
                // `TIMER ON` / `TIMER OFF` —— 定时器中断开关。本平台没有定时器中断
                // （`ON TIMER` 那侧同样空转），开关与否都不改变行为。
                // ⚠ 只在**语句位置**收它：`TIMER` 还是表达式里的取秒函数（`TIMER_FUNC`），
                //   表达式那条路由 `ParsePrimary` 走，不经过这里。
                case TokenType.TIMER_FUNC:
                    Advance();              // skip TIMER
                    // 后面无论是 `ON` / `OFF` / `= n`，本平台一律空转 —— 一次收干净，
                    // 不去认那三种尾巴（`OFF` 在本词法器里根本没有专属 token，是个
                    // 普通标识符，逐种认反而会漏）。
                    SkipRestOfStatement();
                    return null;
                // REDIM
                case TokenType.REDIM:
                    return ParseRedimStatement();
                // VIEW [PRINT]
                case TokenType.VIEW:
                    return ParseViewPrintStatement();
                // WINDOW 坐标系统 — 编译器跳过
                case TokenType.WINDOW:
                    SkipToNextLine();
                    return null;
                // DEF: DEF FN / DEF SEG
                case TokenType.DEF_KW:
                    if (current + 1 < tokens.Count)
                    {
                        var nextType = tokens[current + 1].Type;
                        var nextVal = tokens[current + 1].Value.ToUpper();
                        if (nextType == TokenType.FN_KW || (nextType == TokenType.IDENTIFIER && nextVal.StartsWith("FN")))
                            return ParseDefFnStatement();
                        if (nextVal == "SEG")
                            return ParseDefSegStatement();
                    }
                    SkipToNextLine();
                    return null;
                case TokenType.DEFINT:
                case TokenType.DEFSNG:
                case TokenType.DEFSTR:
                case TokenType.DEFDBL:
                case TokenType.DEFLNG:
                    return ParseDefTypeStatement();
                case TokenType.SHARED:
                    return ParseSharedStatement();
                case TokenType.COMMON:
                    return ParseCommonStatement();
                case TokenType.OPTION_KW:
                    return ParseOptionBaseStatement();
                // LPRINT — 打印机输出（编译器跳过）
                case TokenType.LPRINT:
                    SkipToNextLine();
                    return null;
                case TokenType.NUMBER:
                    // 记录 BASIC 行号 (老式行号 BASIC)，供 GOTO/GOSUB 目标标签生成
                    //
                    // ⚠ **只有整数才是行号**。`int.Parse` 对 `0.02` / `1.5` 这类 token
                    //   会抛 FormatException，而这里**没有 try** ⇒ 整个编译器挂掉，
                    //   用户只看到 `The input string '0.02' was not in a correct format.`
                    //   （实测触发点是 `ON TIMER(0.02) …` 的尾巴重新落回语句层）。
                    //   非整数一律**丢掉这一个 token 继续**：这种输入本来就语法有误，
                    //   编译器该做的是尽量往下走、把问题报在别处，而不是当场炸掉。
                    if (!int.TryParse(token.Value, out int basicLineNumber))
                    {
                        Advance();   // 不是行号 ⇒ 当无意义 token 吃掉
                        return null;
                    }
                    Advance();
                    var numberedStmt = ParseStatement(); // 递归解析行号后的语句
                    if (numberedStmt != null)
                        numberedStmt.BasicLineNumber = basicLineNumber;
                    return numberedStmt;
                case TokenType.SCREEN:
                    return ParseScreenStatement();
                case TokenType.CLS_KW:
                    {
                        Token clsToken = Advance(); // consume CLS
                        return new ClsStatement(clsToken.Line, clsToken.Column);
                    }
                case TokenType.SYSTEM:
                    return ParseSystemStatement();
                case TokenType.IDENTIFIER:
                    // Check if it's a line label (identifier followed by ':')
                    //
                    // ⚠ 名字可以**带点**（`begin.of.editor:`）—— 见 `TryParseDottedName`。
                    //   判据要**先按整条点名往前扫**再看向的是不是 `:`：只看紧邻那一个 token
                    //   的话，`a.b:` 会被判成"不是标签"，而 `rec.f = 1`（记录字段赋值）
                    //   又必须继续落到赋值那条路 —— 两者的区别正是"点号链后面是 `:` 还是 `=`"。
                    if (DottedNameIsLabel())
                    {
                        string labelName = TryParseDottedName()!;
                        Advance(); // consume ':'
                        Statement body = null;
                        if (current < tokens.Count && Peek().Type != TokenType.EOF)
                            body = ParseStatement();
                        return new LabelStatement(token.Line, token.Column, labelName, body);
                    }
                    // ── 赋值先行：`x = 1` / `arr(3) = 42` / `rec.f = 1` ───────────────────
                    //
                    // ⚠ 判据 = 「**本行内、括号深度为 0** 的地方有没有 `=`」。不能只看
                    //   紧邻的那一个 token：数组/记录赋值左边是 `arr` 而下一个 token 是 `(`。
                    //
                    // 此前 `arr(3) = 42` 会落到下面的「一律当裸调用」分支 ⇒ 编成 `func_arr`、
                    // 链接期报「未定义的函数 'func_arr'」——**数组整个不可用**，
                    // 而报错文案指向的是一个名字，与"数组"二字毫无关联。
                    // 顺序也必须排在 `declaredSubs` 判断**之前**：否则一个与子过程同名的
                    // 变量（`counter = 1` 而恰好有 `SUB counter`）会被判成调用。
                    if (HasTopLevelEqualsOnLine())
                    {
                        return ParseLetStatement();
                    }
                    // Check for implicit SUB call (without CALL keyword) — QBasic allows bare sub name calls
                    if (declaredSubs.Contains(token.Value))
                    {
                        return ParseImplicitCallStatement();
                    }
                    // 裸调**函数**同样合法（`ui_win_open "打地鼠", w, h`）—— QBasic 里
                    // 「有返回值的函数当语句用」就是丢弃返回值。此前只认 `declaredSubs`，
                    // 函数名的裸调用于是落到下面的 `ParseLetStatement`，而没有 `=` 就被
                    // **静默丢掉**（连 `line_N` 行标都不发，看汇编完全看不出少了东西）。
                    // 实测：`Examples/basic` 两份游戏都不弹绘图窗口 —— `ui_win_open` 一次都没被调用。
                    // ⚠ 必须排除赋值：`x = ...` 左边也可能是与函数同名的变量（少见但合法）。
                    bool nextIsAssign = current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.EQUALS;
                    if (!nextIsAssign && declaredFunctions.Contains(token.Value))
                    {
                        return ParseImplicitCallStatement();
                    }
                    // 赋值照旧走 `LET` 解析（`x = 1` 左边也可能是与函数同名的变量）。
                    if (nextIsAssign)
                    {
                        return ParseLetStatement();
                    }
                    // 剩下的全是「名字 + 后面不是 `=`」——**一律当裸调用解析**，
                    // 不再 `return ParseLetStatement()`。
                    //
                    // ⚠ 为什么不能交给 ParseLetStatement：它读到一个非 `=` 的 token 就
                    //   `return null`，而语句层对 null 是**静默跳过** ⇒ 整条语句凭空消失。
                    //   于是 `nosuch(1)`（名字没声明过）在汇编里一个字都不留，用户要等到
                    //   运行才发现「这行没效果」——正是「非要等到运行才报错」那种形态。
                    //   （v0.96.204 修过一次同类：`ui_win_open` 是 declaredFunctions 里的一员，
                    //     那次只补上了「已声明」这一半；没声明过的名字仍然漏。）
                    //
                    // 当裸调用解析之后，**裁决权交给链接器**：名字对 ⇒ 库函数被链上；
                    // 名字错 ⇒ 链接期报「未定义的函数 '<name>'」，带文件名和出处。
                    // 这里不自己查表，是因为前端手里的 `declaredSubs`/`declaredFunctions`
                    // 本来就看不见库（`Lib/basic/*.vml` 里的几万个函数）。
                    return ParseImplicitCallStatement();
                // QBASIC 图形/硬件关键字
                case TokenType.PSET:        return ParsePsetStatement();
                case TokenType.QB_LINE:
                    // ⚠ `LINE` 这个词法是**图形语句**（`LINE (x1,y1)-(x2,y2)`），
                    //   但 `LINE INPUT "prompt"; v$` 是**输入语句** —— 两者共用同一个 token。
                    //   不在这里分开的话，`LINE INPUT …` 会走图形解析器：`INPUT` 被当成
                    //   一个表达式啃掉、后面的变量留在流上，编出 `CALL func_v$`
                    //   （GORILLA.BAS 的 `GetInputs` 就是这条，一次 4 个报错）。
                    //   判据只看下一个 token 是不是 `INPUT`，与图形 LINE 不冲突
                    //   （图形 LINE 后面只可能跟 `(` 或坐标/`STEP`）。
                    if (current + 1 < tokens.Count && tokens[current + 1].Type == TokenType.INPUT)
                    {
                        Advance(); // skip LINE
                        return ParseInputStatement();   // 与裸 INPUT **同一份实现**
                    }
                    return ParseQbLineStatement();
                case TokenType.QB_CIRCLE:   return ParseQbCircleStatement();
                case TokenType.PAINT:       return ParseQbPaintStatement();
                case TokenType.LOCATE:      return ParseLocateStatement();
                case TokenType.QB_COLOR:    return ParseQbColorStatement();
                case TokenType.DRAW_KW:     return ParseDrawStatement();
                case TokenType.QB_WIDTH:    return ParseQbWidthStatement();
                case TokenType.PALETTE_KW:  return ParsePaletteStatement();
                case TokenType.GET_KW:      return ParseGetStatement();
                case TokenType.PUT_KW:      return ParsePutStatement();
                case TokenType.PLAY_KW:     return ParsePlayStatement();
                case TokenType.SOUND_KW:    return ParseSoundStatement();
                case TokenType.RANDOMIZE:   return ParseRandomizeStatement();
                case TokenType.KB_GETCH:    return ParseKbGetChStatement();
                case TokenType.SLEEP:       return ParseSleepStatement();
                case TokenType.SWAP:        return ParseSwapStatement();
                case TokenType.ERASE:       return ParseEraseStatement();
                case TokenType.CHIPASM:     return ParseChipAsmStatement();
                case TokenType.POKE:        return ParsePokeStatement();
                // FreeBasic OOP 扩展 (v1.66.32+)
                case TokenType.ENUM_KW:
                    return ParseEnumStatement();
                case TokenType.PTR_KW:
                case TokenType.CAST_KW:
                case TokenType.EXTENDS_KW:
                case TokenType.OPERATOR_KW:
                // PureBasic 扩展
                case TokenType.PROCEDURE_KW:
                case TokenType.GLOBAL_KW:
                case TokenType.PROTECTED_KW:
                case TokenType.INTERFACE_KW:
                case TokenType.ENDINTERFACE:
                // NEW type → 堆分配 (v1.66.32+)
                case TokenType.NEW_KW:
                    Advance(); // 跳过 NEW
                    if (Peek().Type == TokenType.IDENTIFIER) Advance(); // 跳过类型名
                    return new AllocStatement(token.Line, token.Column);
                // ChipBasic MCU GPIO (v1.66.32+) — 编译为 CALL __gpio_xxx
                case TokenType.PINMODE_KW:
                    return ParseGpioStatement("pinmode");
                case TokenType.DIGITALWRITE:
                    return ParseGpioStatement("digitalwrite");
                case TokenType.DIGITALREAD:
                    return ParseGpioStatement("digitalread");
                // TrueBasic 扩展
                case TokenType.MAT_KW:
                    return ParseMatStatement();
                case TokenType.ZER_KW:
                    Advance(); // 跳过 ZER
                    return new LetStatement(token.Line, token.Column) { Variable = new Identifier(token.Line, token.Column, "__zer"), Expression = new NumberLiteral(token.Line, token.Column, 0) };
                case TokenType.CON_KW:
                    Advance(); // 跳过 CON
                    return null; // CON 作为常量声明，在 CollectVariablesAndLabels 中处理
                // GW-BASIC BLOAD/BSAVE → 跨平台文件I/O库调用 (v1.66.32+)
                case TokenType.BLOAD_KW:
                    return ParseGpioStatement("bload");
                case TokenType.BSAVE_KW:
                    return ParseGpioStatement("bsave");
                case TokenType.KEY_KW:
                case TokenType.DEFSEG:
                // PowerBASIC 扩展
                case TokenType.THREADED:
                case TokenType.REGISTER_KW:
                case TokenType.FASTPROC:
                // VisualBasic 扩展 — Private/Public/Friend 作为修饰符跳过
                case TokenType.PRIVATE_KW:
                case TokenType.PUBLIC_KW:
                case TokenType.FRIEND_KW:
                case TokenType.OPTIONAL_KW:
                case TokenType.PARAMARRAY:
                case TokenType.WITH_KW:
                case TokenType.ENDWITH:
                    Advance(); // 已识别但尚未完全实现
                    return null;
                default:
                    Advance();
                    return null;
            }
        }

        // ===== KB/MOUSE 硬件接口解析 =====

        private Statement ParseKbHitStatement()
        {
            Token token = Advance();
            KbHitStatement stmt = new KbHitStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseKbGetChStatement()
        {
            Token token = Advance();
            KbGetChStatement stmt = new KbGetChStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseGetXStatement()
        {
            Token token = Advance();
            MouseGetXStatement stmt = new MouseGetXStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseGetYStatement()
        {
            Token token = Advance();
            MouseGetYStatement stmt = new MouseGetYStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseLeftStatement()
        {
            Token token = Advance();
            MouseLeftStatement stmt = new MouseLeftStatement(token.Line, token.Column);
            return stmt;
        }

        private Statement ParseMouseRightStatement()
        {
            Token token = Advance();
            MouseRightStatement stmt = new MouseRightStatement(token.Line, token.Column);
            return stmt;
        }
    }
}
