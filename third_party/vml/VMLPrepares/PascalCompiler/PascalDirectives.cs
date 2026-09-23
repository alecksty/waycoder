using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal **编译器指令**（`{$...}` / `(*$...*)`）预处理器 —— 老程序里那一整套
    /// `{$IFDEF FPC} … {$ELSE} … {$ENDIF}`。
    ///
    /// <para>
    /// ## 为什么必须有这一层
    ///
    /// 从前 `Lexer.SkipComment` 只特判了 `{$param …}`，**其余 `{$…}` 一律当注释丢掉**，
    /// 而**两个分支的代码都留在流里**。于是 `{$IFDEF FPC} PtcGraph {$ELSE} Graph {$ENDIF}`
    /// 会把 FPC 分支和 Turbo 分支**同时**编进去：重复声明、引到本机不存在的单元、
    /// 报出一串指向别处（`未声明的变量`）的错误。语料 112 份 Pascal 老程序里
    /// **64 份**带指令，`{$IFDEF FPC}` 出现 99 次 —— 这是它们编不过的最大单一原因。
    /// </para>
    ///
    /// <para>
    /// ## 三条不肯让步的规矩
    ///
    /// <list type="number">
    /// <item><b>不活跃的区域根本不进词法器</b> —— 不是"编进去再跳过"。那段代码
    /// 本来就可能是为另一个编译器写的（引用不存在的单元、用本前端没有的写法），
    /// 词法/语法分析走一遍就会报错。所以这里是**字符级**跳过，只认
    /// `{` / `(*` / `'` / `//` 四个界定符。</item>
    /// <item><b>默认符号集是空的</b> —— **不定义 `FPC`、也不定义 `WINDOWS`**。
    /// 被这两条守着的代码里，`{$IFDEF FPC}` 那支是 Free Pascal 专属、
    /// `{$ELSE}` 那支才是 Turbo Pascal，而本前端离后者近得多
    /// （`Lib/pascal/*.pas` 那套声明就是按 Turbo 写的）。要 FPC 那支走 `-d FPC`。</item>
    /// <item><b>不认识的指令原样留着</b> —— `{$F+}` `{$M 4096,0,655360}` `{$R+}`
    /// `{$I+}` 这些代码生成开关一律**逐字保留**，由词法器继续当注释忽略，
    /// 与改动前**一个字节都不差**。只有「条件编译 / DEFINE / UNDEF / IFOPT / 真包含」
    /// 这几类会被处理掉 —— 改动面越小，越不会在别处踩到东西。</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// ## 行号
    ///
    /// 每个输出行都有一条 <see cref="LineMap"/>（第 N 项 = 输出第 N 行的来源 `(文件, 行号)`），
    /// 由调用方交给 `CompilerHelper.TrackPreprocessedOutput` 投递给词法器/解析器/代码生成器
    /// （与 `#include` 那条管道同一套，见 `CompilerHelper` 里那段说明）。
    /// 两条具体做法让行号**尽量不动**：
    /// </para>
    /// <list type="bullet">
    ///   <item>被跳过的区域**保留换行**（只删字符），所以主文件的行号是**恒等映射** ——
    ///         即使哪天映射管道断了，位置也还是对的；</item>
    ///   <item>被吃掉的指令注释也保留其中的换行（`{$M 4096,\n 0, 655360}` 这种跨行写法）。</item>
    /// </list>
    /// <para>
    /// 只有 <c>{$I 文件}</c> 展开会让行号真正偏移 —— 那些行映射到**被包含文件自己的**
    /// 文件名与行号，所以「错在 `cube.vec` 第 12 行」报的就是 `cube.vec` 第 12 行。
    /// </para>
    ///
    /// <para>
    /// ⚠ **这个文件是纯逻辑**（只依赖 `System.*`）：它同时被
    /// `third_party/vml/VMLPrepares/PascalCompiler`（前端自己用）与
    /// `WayCoder`（桌面自测要能直接调它）编译 —— `WayCoder.csproj` 里有一条
    /// `<Compile Include>` 跨项目链接。所以**不要**往这里加 `CompilerBase` /
    /// `VMLPlugins` 的依赖（那些程序集桌面端没有）。需要诊断袋、符号集来源之类的东西，
    /// 都留在 `PascalCompiler.cs` 那层做。
    /// </para>
    ///
    /// <para>
    /// ⚠ **一个实例只服务一次 <see cref="Process"/>**（状态放在字段上，便于把
    /// `Put`/`ConsumeTo` 这些改写输出的动作从局部函数提成方法）。顺序复用没问题
    /// （<see cref="Process"/> 开头会清干净），**并发复用不行**。
    /// </para>
    /// </summary>
    public sealed class PascalDirectives
    {
        /// <summary><c>{$I}</c> 的最大嵌套层数（防病态输入；正常语料是 1 层）。</summary>
        public const int MaxIncludeDepth = 32;

        /// <summary>当前符号集（`{$IFDEF}` 查它）。比较**不区分大小写**（Pascal 语言本身如此）。</summary>
        private readonly HashSet<string> _symbols = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>输出第 N 行的来源 `(文件, 行号)`（第 N 项 = 第 N 行）。</summary>
        public List<(string File, int Line)> LineMap { get; } = new();

        /// <summary>处理过程中的告警（找不到的包含文件、多余的 `{$ENDIF}`…）。</summary>
        public List<(string File, int Line, string Message)> Warnings { get; } = new();

        /// <summary>读过的文件（主文件 + 展开进来的），仅用于自测/诊断。</summary>
        public List<string> IncludedFiles { get; } = new();

        /// <summary>本次处理**真的改动过**文本吗（没改动 ⇒ 调用方应原样返回、不要登记映射）。</summary>
        public bool Changed { get; private set; }

        /// <param name="symbols">预定义符号（**默认空**，见类注释第 2 条）。</param>
        public PascalDirectives(IEnumerable<string> symbols)
        {
            if (symbols == null) return;
            foreach (var s in symbols)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                var t = s.Trim();
                int eq = t.IndexOf('=');            // `-d FOO=1` 只取名字那一半
                if (eq >= 0) t = t.Substring(0, eq).Trim();
                if (t.Length > 0) _symbols.Add(t);
            }
        }

        /// <summary>某个符号当前是否已定义（自测用）。</summary>
        public bool IsDefined(string symbol) => _symbols.Contains(symbol);

        // ── 扫描状态 ────────────────────────────────────────────────────────

        /// <summary>一份参与扫描的文本（主文件或 `{$I}` 进来的文件）。</summary>
        private sealed class Frame
        {
            public string Text;
            public int Pos;
            public int Line = 1;
            /// <summary>报给用户用的文件名（主文件 = 调用方传进来的那个，包含文件 = 绝对路径）。</summary>
            public string File;
            /// <summary>`{$I}` 的相对目录（= 本文件所在目录）。</summary>
            public string Dir;
        }

        /// <summary>一层条件编译。<c>Active</c> 已经把外层状态折进去了。</summary>
        private sealed class Cond
        {
            public bool ParentActive;
            public bool Taken;      // 本条件块已经有分支被选中
            public bool InElse;
            public bool Active;
        }

        private StringBuilder _sb;
        private int _outLine = 1;
        private Stack<Frame> _frames;
        private Stack<Cond> _conds;

        private const string UnknownFile = "<input>";

        // ── 入口 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 处理 <paramref name="source"/> 里的编译器指令。
        /// **没有需要处理的指令时原样返回传入的那个字符串实例**（调用方据此跳过映射登记）。
        /// </summary>
        /// <param name="filePath">主文件路径（拿它算 `{$I}` 的相对目录；内存编译时可为 null）。</param>
        public string Process(string source, string filePath = null)
        {
            LineMap.Clear();
            Warnings.Clear();
            IncludedFiles.Clear();
            Changed = false;
            if (string.IsNullOrEmpty(source)) return source;

            string mainFile = string.IsNullOrEmpty(filePath) ? UnknownFile : filePath;

            _frames = new Stack<Frame>();
            _frames.Push(new Frame { Text = source, File = mainFile, Dir = DirOf(filePath) });
            IncludedFiles.Add(mainFile);
            _conds = new Stack<Cond>();
            _sb = new StringBuilder(source.Length + 16);
            _outLine = 1;

            while (_frames.Count > 0)
            {
                var f = _frames.Peek();
                if (f.Pos >= f.Text.Length) { _frames.Pop(); continue; }

                char c = f.Text[f.Pos];
                bool active = IsActive;

                if (c == '\n') { Put('\n'); f.Pos++; f.Line++; continue; }
                if (c == '\r') { if (active) Put('\r'); f.Pos++; continue; }

                // Pascal 字符串：**只看单引号**（`''` 是转义的单引号）。字符串里的 `{` 不算注释。
                if (c == '\'')
                {
                    int end = ScanString(f.Text, f.Pos);
                    // 未闭合：活跃时与词法器一致（吃到文件尾）；不活跃时**退到行尾**
                    // —— 跳过区里一个孤立的撇号（法文注释、乱码）不该把后面整个文件吞掉。
                    if (end < 0) end = active ? f.Text.Length : LineEnd(f.Text, f.Pos);
                    ConsumeTo(f, end, active);
                    continue;
                }

                if (c == '{')
                {
                    int end = ScanBrace(f.Text, f.Pos, out int cs, out int ce);
                    HandleComment(f, end, cs, ce, active);
                    continue;
                }

                if (c == '(' && f.Pos + 1 < f.Text.Length && f.Text[f.Pos + 1] == '*')
                {
                    int end = ScanParenStar(f.Text, f.Pos, out int cs, out int ce);
                    HandleComment(f, end, cs, ce, active);
                    continue;
                }

                // `//` 行注释：整个跳过去（它里面的 `{$…}` 不是指令）。
                if (c == '/' && f.Pos + 1 < f.Text.Length && f.Text[f.Pos + 1] == '/')
                {
                    ConsumeTo(f, LineEnd(f.Text, f.Pos), active);
                    continue;
                }

                ConsumeTo(f, f.Pos + 1, active);
            }

            // 还没闭合的条件块：不报错、不崩，记一条告警（余下的输入按"块已结束"处理）。
            if (_conds.Count > 0)
                Warnings.Add((mainFile, 0,
                    $"条件编译块没有闭合：文件结束时还差 {_conds.Count} 个 {{$ENDIF}}（已按未闭合处理）"));

            // 补齐到 `_outLine`：输出以换行结尾时这是**多出来的最后一行**（EOF 记号可能落在它上面）。
            while (LineMap.Count < _outLine)
                LineMap.Add(LineMap.Count > 0 ? LineMap[LineMap.Count - 1] : (mainFile, 1));

            var result = _sb;
            _sb = null;
            _frames = null;
            _conds = null;

            // 一个字节都没动 ⇒ 把**原实例**还回去（调用方据此不登记映射、不换源码字符串）。
            if (!Changed) return source;
            return result.ToString();
        }

        private bool IsActive => _conds.Count == 0 || _conds.Peek().Active;

        // ── 输出 ────────────────────────────────────────────────────────────

        /// <summary>补齐当前输出行的映射条目（用**当前帧**的文件/行号）。</summary>
        private void EnsureEntry()
        {
            if (LineMap.Count < _outLine)
            {
                var f = _frames.Peek();
                LineMap.Add((f.File, f.Line));
            }
        }

        private void Put(char c)
        {
            EnsureEntry();
            _sb.Append(c);
            if (c == '\n') _outLine++;
        }

        /// <summary>
        /// 从 <paramref name="f"/> 的当前位置走到 <paramref name="end"/>：
        /// 换行**总是**输出（保住行号），其余字符按 <paramref name="emit"/> 决定。
        /// </summary>
        private void ConsumeTo(Frame f, int end, bool emit)
        {
            if (end > f.Text.Length) end = f.Text.Length;
            while (f.Pos < end)
            {
                char ch = f.Text[f.Pos];
                if (ch == '\n') { Put('\n'); f.Line++; f.Pos++; }
                else { if (emit) Put(ch); f.Pos++; }
            }
        }

        // ── 界定符扫描（返回"结束后的下标"；未闭合返回文本长度）────────────────

        /// <summary>Pascal 字符串闭合之后的下标；未闭合返回 -1。</summary>
        private static int ScanString(string t, int pos)
        {
            int i = pos + 1;
            while (i < t.Length)
            {
                if (t[i] == '\'')
                {
                    if (i + 1 < t.Length && t[i + 1] == '\'') { i += 2; continue; }   // '' = 转义
                    return i + 1;
                }
                i++;
            }
            return -1;
        }

        private static int ScanBrace(string t, int pos, out int contentStart, out int contentEnd)
        {
            contentStart = pos + 1;
            int i = pos + 1;
            while (i < t.Length && t[i] != '}') i++;
            if (i >= t.Length) { contentEnd = t.Length; return t.Length; }   // 未闭合的注释
            contentEnd = i;
            return i + 1;
        }

        private static int ScanParenStar(string t, int pos, out int contentStart, out int contentEnd)
        {
            contentStart = pos + 2;
            int i = pos + 2;
            while (i + 1 < t.Length && !(t[i] == '*' && t[i + 1] == ')')) i++;
            if (i + 1 >= t.Length)
            {
                // `(*` 之后没有 `*)`：整个尾部都算注释（与词法器一致）
                contentEnd = t.Length;
                return t.Length;
            }
            contentEnd = i;
            return i + 2;
        }

        /// <summary>当前行尾（不含换行符）的下标。</summary>
        private static int LineEnd(string t, int pos)
        {
            int i = pos;
            while (i < t.Length && t[i] != '\n') i++;
            return i;
        }

        private static string DirOf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            try { return Path.GetDirectoryName(Path.GetFullPath(filePath)); }
            catch { return null; }
        }

        // ── 注释 / 指令 ────────────────────────────────────────────────────

        private void HandleComment(Frame f, int end, int cs, int ce, bool active)
        {
            string inner = cs < ce ? f.Text.Substring(cs, ce - cs) : string.Empty;
            string trimmed = inner.TrimStart();

            if (trimmed.Length > 1 && trimmed[0] == '$')
            {
                string body = trimmed.Substring(1);
                int p = 0;
                while (p < body.Length && char.IsLetterOrDigit(body[p])) p++;
                string name = body.Substring(0, p).ToUpperInvariant();
                string rest = body.Substring(p);

                // `{$param lib(...)}` / `(*$param path(...)*)` —— **原样留着**，
                // 交给词法器收集（那是既有的自动链接机制，这里一个字都不碰）。
                if (name != "PARAM" && IsKnownDirective(name, rest))
                {
                    // 先把注释本体吃掉（保留其中的换行），再改状态 ——
                    // `{$I}` 推帧时当前帧必须已经停在指令之后。
                    ConsumeTo(f, end, false);
                    // 注释在 Pascal 里是**分隔符**，删掉得补一个空格，
                    // 否则 `a{$R+}b` 会粘成一个标识符。
                    if (active) Put(' ');
                    Changed = true;
                    HandleDirective(name, rest, active, f);
                    return;
                }
            }

            // 普通注释、`{$param}`、以及一切我们不管的 `{$F+}`/`{$M…}`/`{$I+}`：
            // **逐字保留**（活跃时），语义与改动前一模一样。
            ConsumeTo(f, end, active);
        }

        /// <summary>这条指令归我们管吗（不管的必须逐字保留，见类注释第 3 条）。</summary>
        private static bool IsKnownDirective(string name, string rest)
        {
            switch (name)
            {
                case "IFDEF":
                case "IFNDEF":
                case "ELSE":
                case "ENDIF":
                case "IFEND":
                case "DEFINE":
                case "UNDEF":
                case "IFOPT":
                    return true;
                case "I":
                case "INCLUDE":
                    // `{$I+}` / `{$I-}` 是 **I/O 检查开关**，不是包含 —— 不归我们管。
                    // 包含：`{$I cube.vec}`、`{$INCLUDE 'x.pas'}`。
                    {
                        var a = rest.Trim();
                        if (a.Length == 0) return false;
                        return a[0] != '+' && a[0] != '-';
                    }
                default:
                    return false;
            }
        }

        private void HandleDirective(string name, string rest, bool active, Frame f)
        {
            switch (name)
            {
                case "IFDEF":
                case "IFNDEF":
                {
                    string sym = FirstWord(rest);
                    bool defined = sym.Length > 0 && _symbols.Contains(sym);
                    bool cond = name == "IFDEF" ? defined : !defined;
                    _conds.Push(new Cond { ParentActive = active, Taken = cond, Active = active && cond });
                    break;
                }
                case "IFOPT":
                    // ⚠ **一律判假**（于是 `{$ELSE}` 那支胜出）。
                    //
                    // `{$IFOPT O+}` / `{$IFOPT G+}` 问的是"某个代码生成开关开着吗"，
                    // 而我们**没有**与 Turbo Pascal 逐位对齐的开关模型 —— 猜一个答案
                    // 比固定一个答案更糟：猜"真"会把 `{$IFOPT G+}` 那一支
                    // （"开着就短路求值"那类优化专用写法）编进来，而本前端不实现它；
                    // 猜"假"最多走 `{$ELSE}` 那条保守分支。语料里 2 处，都落在一个
                    // 可选的运行时检查块上，走 `{$ELSE}` 是安全的。
                    _conds.Push(new Cond { ParentActive = active, Taken = false, Active = false });
                    break;
                case "ELSE":
                {
                    if (_conds.Count == 0)
                    {
                        Warnings.Add((f.File, f.Line, "多余的 {$ELSE}（没有对应的 {$IFDEF}/{$IFNDEF}）—— 已忽略"));
                        break;
                    }
                    var top = _conds.Peek();
                    if (top.InElse)
                    {
                        Warnings.Add((f.File, f.Line, "重复的 {$ELSE} —— 已忽略"));
                        break;
                    }
                    top.InElse = true;
                    bool cond = !top.Taken;
                    top.Active = top.ParentActive && cond;
                    if (cond) top.Taken = true;
                    break;
                }
                case "ENDIF":
                case "IFEND":
                    if (_conds.Count == 0)
                        Warnings.Add((f.File, f.Line, "多余的 {$ENDIF}（没有对应的 {$IFDEF}/{$IFNDEF}）—— 已忽略"));
                    else
                        _conds.Pop();
                    break;
                case "DEFINE":
                {
                    string sym = FirstWord(rest);
                    if (active && sym.Length > 0) _symbols.Add(sym);
                    break;
                }
                case "UNDEF":
                {
                    string sym = FirstWord(rest);
                    if (active && sym.Length > 0) _symbols.Remove(sym);
                    break;
                }
                case "I":
                case "INCLUDE":
                    if (active) Include(rest);
                    break;
            }
        }

        private static string FirstWord(string rest)
        {
            if (string.IsNullOrEmpty(rest)) return string.Empty;
            var t = rest.Trim();
            int i = 0;
            while (i < t.Length && !char.IsWhiteSpace(t[i]) && t[i] != ',') i++;
            return t.Substring(0, i);
        }

        // ── `{$I 文件}` ────────────────────────────────────────────────────

        private void Include(string rest)
        {
            var f = _frames.Peek();
            string arg = rest.Trim();
            if (arg.Length == 0) return;
            if (arg.Length >= 2 &&
                ((arg[0] == '\'' && arg[arg.Length - 1] == '\'') ||
                 (arg[0] == '"' && arg[arg.Length - 1] == '"')))
                arg = arg.Substring(1, arg.Length - 2);

            string resolved = Resolve(arg, f.Dir);
            if (resolved == null)
            {
                // 找不着就跳过 —— **不报错**。语料里 `{$I cube.vec}` 那几份 .vec
                // 本来就不在仓库里；从前它是注释（静默丢弃），现在也只是多一条告警。
                Warnings.Add((f.File, f.Line, $"找不到包含文件 '{{$I {arg}}}' —— 已跳过"));
                return;
            }
            if (_frames.Count >= MaxIncludeDepth)
            {
                Warnings.Add((f.File, f.Line, $"包含层数超过 {MaxIncludeDepth} 层（'{{$I {arg}}}'）—— 已跳过"));
                return;
            }
            // **循环判据 = "已经在包含链上"**（不是"曾经包含过"）：同一个文件在**不同位置**
            // 被包含两次是合法的（文本内联语义），只有自我包含才是环。
            foreach (var fr in _frames)
            {
                if (string.Equals(fr.File, resolved, StringComparison.OrdinalIgnoreCase))
                {
                    Warnings.Add((f.File, f.Line, $"包含循环：'{{$I {arg}}}' 已经在包含链上 —— 已跳过"));
                    return;
                }
            }

            string text;
            try { text = File.ReadAllText(resolved); }
            catch (Exception ex)
            {
                Warnings.Add((f.File, f.Line, $"读不了包含文件 '{arg}'：{ex.Message} —— 已跳过"));
                return;
            }
            if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);

            IncludedFiles.Add(resolved);
            _frames.Push(new Frame { Text = text, File = resolved, Dir = DirOf(resolved) });
        }

        /// <summary>按"包含文件所在目录"找（找不到再试相对 cwd）；找不到返回 null。</summary>
        private static string Resolve(string name, string dir)
        {
            var cands = new List<string>();
            if (Path.IsPathRooted(name)) cands.Add(name);
            else
            {
                if (!string.IsNullOrEmpty(dir)) cands.Add(Path.Combine(dir, name));
                cands.Add(name);
            }
            foreach (var c in cands)
            {
                try { if (File.Exists(c)) return Path.GetFullPath(c); } catch { }
            }
            return null;
        }
    }
}
