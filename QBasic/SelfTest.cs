// =============================================================
// SelfTest.cs —— 全量自测
//
// 覆盖：词法 token、表达式求值、变量赋值、IF/ELSE、FOR 循环、
// GOTO、GOSUB/RETURN、字符串函数、数组、运行时错误、TUI 纯逻辑
// （CJK 宽度、差分重绘、颜色编码）。断言 ≥ 50 项。
// =============================================================
using QBasic.Compiler;
using QBasic.Controls;
using QBasic.Tui;

namespace QBasic;

/// <summary>自测入口。</summary>
public static class SelfTest
{
    private static int _pass, _fail;

    public static bool RunAll()
    {
        _pass = 0; _fail = 0;

        // ===== TUI 纯逻辑 =====
        TestTui();

        // ===== 词法 =====
        TestLexer();

        // ===== 表达式求值 =====
        TestExpr();

        // ===== 语句执行 =====
        TestStatements();

        // ===== 控制流 =====
        TestControlFlow();

        // ===== 字符串函数 =====
        TestStringFuncs();

        // ===== 数组 =====
        TestArrays();

        // ===== 运行时错误 =====
        TestRuntimeErrors();

        // ===== 新增特性 =====
        TestSelectCase();
        TestDoLoop();
        TestRandom();
        TestDataRead();
        TestNewStringFuncs();
        TestTwoDimArray();
        TestCancel();
        TestHighlight();
        TestGraphics();
        TestRoutines();
        TestDefFnAndZeroArg();
        TestGorillaPlayable();

        Console.WriteLine();
        Console.WriteLine($"自测结果: {_pass} 通过, {_fail} 失败, 共 {_pass + _fail} 项");
        return _fail == 0;
    }

    private static void Check(string name, bool cond)
    {
        if (cond) { _pass++; }
        else { _fail++; Console.WriteLine($"  ✗ {name}"); }
    }

    // ---------- 执行辅助 ----------
    private static (string output, Chunk chunk, Vm vm) Exec(string src, params string[] input)
    {
        var lexer = new Lexer(src);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var stmts = parser.ParseProgram();
        var gen = new CodeGen();
        var chunk = gen.Compile(stmts, parser.Data);
        var outp = new MemoryOutput();
        var vm = new Vm(new QueueInputProvider(input), outp);
        vm.Run(chunk);
        return (outp.All, chunk, vm);
    }

    // 执行完整程序（含 SUB/FUNCTION/TYPE/CONST —— 需把解析器收集的例程/类型传给 CodeGen）
    private static (string output, Chunk chunk, Vm vm) ExecProg(string src, params string[] input)
    {
        var lexer = new Lexer(src);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var stmts = parser.ParseProgram();
        var gen = new CodeGen(parser.Routines, parser.DefFns, parser.Types);
        var chunk = gen.Compile(stmts, parser.Data);
        var outp = new MemoryOutput();
        var vm = new Vm(new QueueInputProvider(input), outp);
        vm.Run(chunk);
        return (outp.All, chunk, vm);
    }

    // ---------- TUI ----------
    private static void TestTui()
    {
        Check("CJK 中文宽 2", Cjk.Width("中a") == 3);
        Check("CJK ASCII 宽 1", Cjk.Width("abc") == 3);
        Check("CJK 全角标点", Cjk.IsWide('，'));
        Check("CJK Fit 补齐", Cjk.Fit("a", 4).Length == 4);
        Check("CJK Fit 裁剪", Cjk.Fit("abcdef", 3) == "ab…");
        Check("Ansi 颜色", Ansi.Fg(Color.Red).StartsWith("\u001b[31m"));
        Check("Ansi RGB", Ansi.FgRgb(1, 2, 3) == "\u001b[38;2;1;2;3m");
        Check("Ansi 光标", Ansi.CursorTo(3, 5) == "\u001b[3;5H");
        Check("Ansi 备用屏", Ansi.EnterAltScreen.Contains("1049"));
        Check("Ansi 隐藏光标", Ansi.HideCursor == "\u001b[?25l");

        // 差分重绘
        var scr = new Screen(3, 5);
        scr.PutText(1, 1, "hello", Color.White, Color.Black);
        string diff1 = scr.Flush();
        Check("差分首帧非空", diff1.Length > 0);
        string diff2 = scr.Flush();
        Check("差分无变化为空", diff2.Length == 0);
        scr.Put(1, 1, 'H', Color.White, Color.Black);
        string diff3 = scr.Flush();
        Check("差分检测到变更", diff3.Length > 0);
    }

    // ---------- 词法 ----------
    private static void TestLexer()
    {
        var lx = new Lexer("PRINT 10 + 20\nLET x$ = \"hi\"");
        var toks = lx.Tokenize();
        Check("词法: PRINT 关键字", toks[0].Type == TokenType.Ident && toks[0].Text == "PRINT");
        Check("词法: 数字", toks[1].Type == TokenType.Number && toks[1].Num == 10);
        Check("词法: 加号", toks[2].Type == TokenType.Op && toks[2].Text == "+");
        Check("词法: 换行", toks[4].Type == TokenType.Newline);
        Check("词法: 字符串", toks[8].Type == TokenType.Str && toks[8].Str == "hi");
        Check("词法: 行号标签", new Lexer("10 PRINT 5").Tokenize()[0].Type == TokenType.LineNum);
        Check("词法: 注释跳过", new Lexer("x=1 REM 注释").Tokenize().Count(t => t.Type == TokenType.Ident && t.Text == "PRINT") == 0);
        Check("词法: 单引号注释", new Lexer("' 注释").Tokenize().Count(t => t.Type != TokenType.Newline && t.Type != TokenType.Eof) == 0);
        Check("词法: MOD", new Lexer("5 MOD 2").Tokenize()[1].Text == "MOD");
        Check("词法: 比较符", new Lexer("a <= b").Tokenize()[1].Text == "<=");
    }

    // ---------- 表达式 ----------
    private static void TestExpr()
    {
        var (o, _, _) = Exec("PRINT 2+3*4");
        Check("表达式: 优先级", o == "14\n");
        var (o2, _, _) = Exec("PRINT (2+3)*4");
        Check("表达式: 括号", o2 == "20\n");
        var (o3, _, _) = Exec("PRINT 10 MOD 3");
        Check("表达式: MOD", o3 == "1\n");
        var (o4, _, _) = Exec("PRINT 7/2");
        Check("表达式: 除法", o4 == "3.5\n");
        var (o5, _, _) = Exec("PRINT 5 > 3");
        Check("表达式: 比较真", o5 == "1\n");
        var (o6, _, _) = Exec("PRINT 5 < 3");
        Check("表达式: 比较假", o6 == "0\n");
        var (o7, _, _) = Exec("PRINT 5 = 5 AND 1 = 1");
        Check("表达式: AND", o7 == "1\n");
        var (o8, _, _) = Exec("PRINT NOT 0");
        Check("表达式: NOT", o8 == "1\n");
        var (o9, _, _) = Exec("PRINT -5 + 3");
        Check("表达式: 一元负", o9 == "-2\n");
    }

    // ---------- 语句 ----------
    private static void TestStatements()
    {
        var (o1, _, _) = Exec("LET x = 10\nPRINT x");
        Check("赋值: LET 数字", o1 == "10\n");
        var (o2, _, _) = Exec("x = 42\nPRINT x");
        Check("赋值: 省略 LET", o2 == "42\n");
        var (o3, _, _) = Exec("LET s$ = \"hi\"\nPRINT s$");
        Check("赋值: 字符串", o3 == "hi\n");
        var (o4, _, _) = Exec("PRINT \"a\"; \"b\"");
        Check("PRINT: 分号拼接", o4 == "ab\n");
        var (o5, _, _) = Exec("PRINT 1, 2");
        Check("PRINT: 逗号分隔", o5.StartsWith("1"));
        var (o6, _, _) = Exec("PRINT \"x\"\nPRINT");
        Check("PRINT: 空换行", o6 == "x\n\n");
        var (o7, _, _) = Exec("INPUT a\nPRINT a", "7");
        Check("INPUT: 读取数字", o7 == "7\n");
        var (o8, _, _) = Exec("INPUT s$\nPRINT s$", "hello");
        Check("INPUT: 读取字符串", o8 == "hello\n");
    }

    // ---------- 控制流 ----------
    private static void TestControlFlow()
    {
        var (o1, _, _) = Exec("IF 1 = 1 THEN PRINT \"yes\"");
        Check("IF: 单行真", o1 == "yes\n");
        var (o2, _, _) = Exec("IF 1 = 2 THEN PRINT \"a\" ELSE PRINT \"b\"");
        Check("IF: 单行 ELSE", o2 == "b\n");
        var (o3, _, _) = Exec("IF 1 = 2 THEN\nPRINT \"a\"\nELSE\nPRINT \"b\"\nEND IF");
        Check("IF: 块 ELSE", o3 == "b\n");
        var (o4, _, _) = Exec("FOR i = 1 TO 5\nPRINT i\nNEXT");
        Check("FOR: 1-5 循环", o4 == "1\n2\n3\n4\n5\n");
        var (o5, _, _) = Exec("FOR i = 5 TO 1 STEP -1\nPRINT i\nNEXT");
        Check("FOR: 递减循环", o5 == "5\n4\n3\n2\n1\n");
        var (o6, _, _) = Exec("s = 0\nFOR i = 1 TO 100\ns = s + i\nNEXT\nPRINT s");
        Check("FOR: 求和", o6 == "5050\n");
        var (o7, _, _) = Exec("i = 0\nWHILE i < 3\ni = i + 1\nWEND\nPRINT i");
        Check("WHILE: 循环", o7 == "3\n");
        var (o8, _, _) = Exec("GOTO 10\nPRINT \"skip\"\n10 PRINT \"here\"");
        Check("GOTO: 数字标签", o8 == "here\n");
        var (o9, _, _) = Exec("start:\nPRINT \"a\"\nGOTO end\nPRINT \"b\"\nend:\nPRINT \"c\"");
        Check("GOTO: 命名标签", o9 == "a\nc\n");
        var (o10, _, _) = Exec("GOSUB 20\nPRINT \"back\"\nEND\n20 PRINT \"sub\"\nRETURN");
        Check("GOSUB: 调用并返回", o10 == "sub\nback\n");
    }

    // ---------- 字符串函数 ----------
    private static void TestStringFuncs()
    {
        var (o1, _, _) = Exec("PRINT LEN(\"hello\")");
        Check("函数: LEN", o1 == "5\n");
        var (o2, _, _) = Exec("PRINT CHR$(65)");
        Check("函数: CHR$", o2 == "A\n");
        var (o3, _, _) = Exec("PRINT VAL(\"42\") + 1");
        Check("函数: VAL", o3 == "43\n");
        var (o4, _, _) = Exec("PRINT STR$(3.5)");
        Check("函数: STR$", o4 == "3.5\n");
        var (o5, _, _) = Exec("PRINT MID$(\"hello\", 2, 3)");
        Check("函数: MID$", o5 == "ell\n");
        var (o6, _, _) = Exec("PRINT LEFT$(\"hello\", 2)");
        Check("函数: LEFT$", o6 == "he\n");
        var (o7, _, _) = Exec("PRINT RIGHT$(\"hello\", 2)");
        Check("函数: RIGHT$", o7 == "lo\n");
        var (o8, _, _) = Exec("PRINT INT(3.7)");
        Check("函数: INT", o8 == "3\n");
        var (o9, _, _) = Exec("PRINT SQR(16)");
        Check("函数: SQR", o9 == "4\n");
        var (o10, _, _) = Exec("PRINT ABS(-9)");
        Check("函数: ABS", o10 == "9\n");
        var (o11, _, _) = Exec("PRINT \"a\" + \"b\"");
        Check("字符串: 拼接", o11 == "ab\n");
    }

    // ---------- 数组 ----------
    private static void TestArrays()
    {
        var (o1, _, _) = Exec("DIM a(5)\na(0) = 10\na(1) = 20\nPRINT a(0) + a(1)");
        Check("数组: 声明+赋值", o1 == "30\n");
        var (o2, _, _) = Exec("DIM a(3)\nFOR i = 0 TO 2\na(i) = i * 2\nNEXT\nPRINT a(2)");
        Check("数组: 循环赋值", o2 == "4\n");
        var (o3, _, _) = Exec("DIM s$(2)\ns$(1) = \"hi\"\nPRINT s$(1)");
        Check("数组: 字符串数组", o3 == "hi\n");
        var (o4, _, _) = Exec("DIM a\na = 42\nPRINT a");
        Check("数组: 标量 DIM 后赋值", o4 == "42\n");
    }

    // ---------- 运行时错误 ----------
    private static void TestRuntimeErrors()
    {
        try
        {
            var lexer = new Lexer("PRINT 10 / 0");
            var stmts = new Parser(lexer.Tokenize()).ParseProgram();
            var chunk = new CodeGen().Compile(stmts);
            var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput());
            vm.Run(chunk);
            Check("运行时: 除零", false);
        }
        catch (RuntimeError re)
        {
            Check("运行时: 除零", re.Message.Contains("除零"));
        }

        try
        {
            var lexer = new Lexer("PRINT xyz");
            var stmts = new Parser(lexer.Tokenize()).ParseProgram();
            var chunk = new CodeGen().Compile(stmts);
            var outp = new MemoryOutput();
            var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), outp);
            vm.Run(chunk);
            Check("运行时: 未定义变量默认0", outp.All == "0\n");
        }
        catch (RuntimeError)
        {
            Check("运行时: 未定义变量默认0", false);
        }

        // 类型不匹配：数值变量赋字符串
        try
        {
            var lexer = new Lexer("LET s = \"abc\"");
            var stmts = new Parser(lexer.Tokenize()).ParseProgram();
            var chunk = new CodeGen().Compile(stmts);
            var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput());
            vm.Run(chunk);
            Check("运行时: 类型不匹配", false);
        }
        catch (RuntimeError re)
        {
            Check("运行时: 类型不匹配", re.Message.Contains("字符串"));
        }

        // 编译错误
        try
        {
            var lexer = new Lexer("GOTO missing");
            var stmts = new Parser(lexer.Tokenize()).ParseProgram();
            new CodeGen().Compile(stmts);
            Check("编译: 未定义标签", false);
        }
        catch (CompileException)
        {
            Check("编译: 未定义标签", true);
        }

        // 数组越界
        try
        {
            var lexer = new Lexer("DIM a(2)\nPRINT a(10)");
            var stmts = new Parser(lexer.Tokenize()).ParseProgram();
            var chunk = new CodeGen().Compile(stmts);
            var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput());
            vm.Run(chunk);
            Check("运行时: 数组越界", false);
        }
        catch (RuntimeError re)
        {
            Check("运行时: 数组越界", re.Message.Contains("越界"));
        }
    }﻿
    // ---------- SELECT CASE ----------
    private static void TestSelectCase()
    {
        var (o1, _, _) = Exec("x = 2\nSELECT CASE x\nCASE 1\nPRINT \"one\"\nCASE 2\nPRINT \"two\"\nCASE 3\nPRINT \"three\"\nEND SELECT");
        Check("SELECT: 匹配 case", o1 == "two\n");
        var (o2, _, _) = Exec("x = 5\nSELECT CASE x\nCASE 1\nPRINT \"a\"\nCASE ELSE\nPRINT \"other\"\nEND SELECT");
        Check("SELECT: case else", o2 == "other\n");
        var (o3, _, _) = Exec("x = 2\nSELECT CASE x\nCASE 1, 2\nPRINT \"low\"\nCASE ELSE\nPRINT \"high\"\nEND SELECT");
        Check("SELECT: 多值 case", o3 == "low\n");
        var (o4, _, _) = Exec("x = 10\nSELECT CASE x\nCASE IS > 5\nPRINT \"big\"\nCASE ELSE\nPRINT \"small\"\nEND SELECT");
        Check("SELECT: case is", o4 == "big\n");
        var (o5, _, _) = Exec("s$ = \"hi\"\nSELECT CASE s$\nCASE \"hi\"\nPRINT \"hello\"\nCASE ELSE\nPRINT \"no\"\nEND SELECT");
        Check("SELECT: 字符串匹配", o5 == "hello\n");
    }

    // ---------- DO ... LOOP ----------
    private static void TestDoLoop()
    {
        var (o1, _, _) = Exec("i = 0\nDO WHILE i < 3\ni = i + 1\nLOOP\nPRINT i");
        Check("DO: while 前置", o1 == "3\n");
        var (o2, _, _) = Exec("i = 0\nDO\ni = i + 1\nLOOP WHILE i < 3\nPRINT i");
        Check("DO: while 后置", o2 == "3\n");
        var (o3, _, _) = Exec("i = 0\nDO UNTIL i >= 3\ni = i + 1\nLOOP\nPRINT i");
        Check("DO: until 前置", o3 == "3\n");
        var (o4, _, _) = Exec("i = 0\nDO\ni = i + 1\nLOOP UNTIL i >= 3\nPRINT i");
        Check("DO: until 后置", o4 == "3\n");
        var (o5, _, _) = Exec("i = 0\nDO\ni = i + 1\nLOOP UNTIL i >= 2\nPRINT i");
        Check("DO: 后置至少执行一次", o5 == "2\n");
        var (o6, _, _) = Exec("i = 0\nDO\nPRINT i\ni = i + 1\nLOOP WHILE i < 2");
        Check("DO: 后置多次输出", o6 == "0\n1\n");
    }

    // ---------- RND / RANDOMIZE ----------
    private static void TestRandom()
    {
        var (o1, _, _) = Exec("RANDOMIZE\nx = RND(1)\nPRINT x >= 0 AND x < 1");
        Check("RND: 在 [0,1) 区间", o1 == "1\n");
        var (o2, _, _) = Exec("RANDOMIZE\na = RND(1)\nb = RND(1)\nPRINT a <> b");
        Check("RND: 两次不同", o2 == "1\n");
        // RANDOMIZE 带括号参数：括号内表达式被正确跳过，不吞后续语句
        var (o3, _, _) = Exec("RANDOMIZE (TIMER)\nPRINT 123");
        Check("RANDOMIZE: 带括号不吞语句", o3 == "123\n");
        // RANDOMIZE 带括号出现在 SUB 内，后续例程定义不被吞（回归：初始括号深度错误会吞到 EOF）
        var lp = new Lexer("SUB A\nRANDOMIZE (TIMER)\nPRINT 1\nEND SUB\nSUB B\nPRINT 2\nEND SUB");
        var pp = new Parser(lp.Tokenize());
        pp.ParseProgram();
        Check("RANDOMIZE: 括号后例程不被吞", pp.Routines.Count == 2);
    }

    // ---------- DATA / READ / RESTORE ----------
    private static void TestDataRead()
    {
        var (o1, _, _) = Exec("DATA 10, 20, 30\nREAD a\nREAD b\nREAD c\nPRINT a + b + c");
        Check("DATA/READ: 读取数字", o1 == "60\n");
        var (o2, _, _) = Exec("DATA \"hello\", \"world\"\nREAD a$\nREAD b$\nPRINT a$ + \" \" + b$");
        Check("DATA/READ: 读取字符串", o2 == "hello world\n");
        var (o3, _, _) = Exec("DATA 1, 2\nRESTORE\nREAD a\nREAD b\nPRINT a + b");
        Check("RESTORE: 重置指针", o3 == "3\n");
        var (o4, _, _) = Exec("DATA 5\nREAD a\nRESTORE\nREAD b\nPRINT a + b");
        Check("RESTORE: 重复读取", o4 == "10\n");
        var (o5, _, _) = Exec("DATA -5, 7\nREAD a\nREAD b\nPRINT a + b");
        Check("DATA: 负数", o5 == "2\n");
    }

    // ---------- 新增字符串函数 ----------
    private static void TestNewStringFuncs()
    {
        var (o1, _, _) = Exec("PRINT INSTR(\"hello world\", \"world\")");
        Check("函数: INSTR", o1 == "7\n");
        var (o2, _, _) = Exec("PRINT INSTR(3, \"abcabc\", \"bc\")");
        Check("函数: INSTR 起始位置", o2 == "5\n");
        var (o3, _, _) = Exec("PRINT UCASE$(\"Hello\")");
        Check("函数: UCASE$", o3 == "HELLO\n");
        var (o4, _, _) = Exec("PRINT LCASE$(\"Hello\")");
        Check("函数: LCASE$", o4 == "hello\n");
        var (o5, _, _) = Exec("PRINT STRING$(3, \"A\")");
        Check("函数: STRING$", o5 == "AAA\n");
        var (o6, _, _) = Exec("PRINT SPACE$(4); \"x\"");
        Check("函数: SPACE$", o6 == "    x\n");
        var (o7, _, _) = Exec("PRINT LTRIM$(\"  hi\")");
        Check("函数: LTRIM$", o7 == "hi\n");
        var (o8, _, _) = Exec("PRINT RTRIM$(\"hi  \")");
        Check("函数: RTRIM$", o8 == "hi\n");
    }

    // ---------- 二维数组 ----------
    private static void TestTwoDimArray()
    {
        var (o1, _, _) = Exec("DIM a(2, 3)\na(0, 0) = 5\na(1, 2) = 7\nPRINT a(0, 0) + a(1, 2)");
        Check("二维数组: 赋值读取", o1 == "12\n");
        var (o2, _, _) = Exec("DIM m(2, 2)\nFOR i = 0 TO 1\nFOR j = 0 TO 1\nm(i, j) = i * 2 + j\nNEXT\nNEXT\nPRINT m(1, 1)");
        Check("二维数组: 循环填充", o2 == "3\n");
        var (o3, _, _) = Exec("DIM s$(2, 2)\ns$(1, 0) = \"cell\"\nPRINT s$(1, 0)");
        Check("二维数组: 字符串", o3 == "cell\n");
        var (o4, _, _) = Exec("DIM a(2, 2)\na(1, 1) = 9\nPRINT a(1, 1) * 2");
        Check("二维数组: 算术", o4 == "18\n");
    }

    // ---------- Ctrl+C 协作式中断 ----------
    private static void TestCancel()
    {
        {
            var lexer = new Lexer("DO\nLOOP");
            var stmts = new Parser(lexer.Tokenize()).ParseProgram();
            var chunk = new CodeGen().Compile(stmts, new Parser(lexer.Tokenize()).Data);
            var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput());
            using var cts = new CancellationTokenSource();
            bool canceled = false;
            // 在后台线程启动无限循环，主线程随即取消，验证协作式中断钩子
            var t = new Thread(() =>
            {
                try { vm.Run(chunk, cts.Token); }
                catch (OperationCanceledException) { canceled = true; }
            });
            t.Start();
            Thread.Sleep(30);
            cts.Cancel();
            t.Join();
            Check("Ctrl+C: 中断", canceled);
        }
    }

    // ---------- TextBox 语法高亮 ----------
    private static void TestHighlight()
    {
        var tb = new TextBox();
        var segs = tb.TokenizeForHighlight("PRINT \"hi\" + 42 REM note");
        bool hasKeyword = false, hasStr = false, hasNum = false, hasComment = false;
        foreach (var (text, color) in segs)
        {
            if (text == "PRINT" && color == tb.KeywordColor) hasKeyword = true;
            if (text == "\"hi\"" && color == tb.StringColor) hasStr = true;
            if (text == "42" && color == tb.NumberColor) hasNum = true;
            if (text.StartsWith("REM") && color == tb.CommentColor) hasComment = true;
        }
        Check("高亮: 关键字", hasKeyword);
        Check("高亮: 字符串", hasStr);
        Check("高亮: 数字", hasNum);
        Check("高亮: 注释", hasComment);
        var segs2 = tb.TokenizeForHighlight("' 整行注释");
        Check("高亮: 单引号注释", segs2.Count == 1 && segs2[0].Item2 == tb.CommentColor);
    }

    // ---------- 图形层：PixelBuffer / 文本渲染 / PEEK ----------
    private static void TestGraphics()
    {
        // PixelBuffer 基本读写
        var px = new PixelBuffer(320, 200);
        px.Set(10, 20, 3);
        Check("图形: Set/Get 往返", px.Get(10, 20) == 3);
        Check("图形: 越界读返回 0", px.Get(-1, 0) == 0 && px.Get(320, 200) == 0);
        px.Clear(7);
        Check("图形: Clear 整屏", px.Get(0, 0) == 7 && px.Get(319, 199) == 7);

        // FillRect 填充（自动归一化坐标）
        var fr = new PixelBuffer(50, 50);
        fr.FillRect(15, 15, 5, 5, 7);
        Check("图形: FillRect 角点", fr.Get(5, 5) == 7 && fr.Get(15, 15) == 7);
        Check("图形: FillRect 外部未染", fr.Get(4, 4) == 0 && fr.Get(16, 16) == 0);

        // Line 直线（Bresenham 含端点）
        var ln = new PixelBuffer(30, 30);
        ln.Line(0, 0, 20, 0, 5);
        Check("图形: Line 端点", ln.Get(0, 0) == 5 && ln.Get(20, 0) == 5);
        Check("图形: Line 中点", ln.Get(10, 0) == 5);

        // Circle 圆周（上下左右四极点应在圆周上）
        var c = new PixelBuffer(40, 40);
        c.Circle(20, 20, 10, 4, 0, 0, false, 1);
        Check("图形: Circle 圆周有点",
            c.Get(30, 20) == 4 || c.Get(10, 20) == 4 || c.Get(20, 30) == 4 || c.Get(20, 10) == 4);

        // Flood 洪泛填充（画框后填内部，边界保留）
        var fl = new PixelBuffer(30, 30);
        fl.Line(5, 5, 20, 5, 1);
        fl.Line(20, 5, 20, 20, 1);
        fl.Line(20, 20, 5, 20, 1);
        fl.Line(5, 20, 5, 5, 1);
        fl.Flood(10, 10, 9, 1);
        Check("图形: Flood 填充内部", fl.Get(10, 10) == 9);
        Check("图形: Flood 边界保留", fl.Get(5, 5) == 1);

        // GetSprite / PutSprite 往返
        var sp = new PixelBuffer(30, 30);
        sp.Set(10, 10, 6);
        var data = sp.GetSprite(8, 8, 12, 12);
        Check("图形: GetSprite 尺寸", data.Length == 2 + 5 * 5 && data[0] == 5 && data[1] == 5);
        var dst = new PixelBuffer(30, 30);
        dst.PutSprite(0, 0, data, false);
        Check("图形: PutSprite 恢复像素", dst.Get(2, 2) == 6);

        // PEEK 返回 0（非 DOS 环境，NumLock 等硬件状态无关紧要）
        var (po, _, _) = Exec("PRINT PEEK(1047)");
        Check("函数: PEEK 返回 0", po == "0\n");

        // 文本渲染：RenderMode=true 时 PRINT 写入 TextLayer（非 stdout）
        var lp = new Lexer("PRINT \"AB\"");
        var pp = new Parser(lp.Tokenize());
        var stmts = pp.ParseProgram();
        var chunk = new CodeGen().Compile(stmts, pp.Data);
        var gfx = new GfxDevice();
        var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput())
        {
            Gfx = gfx,
            RenderMode = true,
            Present = _ => { },
        };
        vm.Run(chunk);
        Check("文本: RenderMode 写入 TextLayer", gfx.Text.GetChar(0, 0) == 'A' && gfx.Text.GetChar(0, 1) == 'B');

        // 图形语句经 VM 端到端执行（SCREEN/LINE/PSET/CIRCLE/PAINT 落到 Pixels 缓冲）
        var gp = new Lexer("SCREEN 9\nLINE (10,10)-(50,10),4\nPSET (100,100),7\nCIRCLE (160,100),50,2\nPAINT (160,100),3,2");
        var gpp = new Parser(gp.Tokenize());
        var gstmts = gpp.ParseProgram();
        var gchunk = new CodeGen().Compile(gstmts, gpp.Data);
        var ggfx = new GfxDevice();
        var gvm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput()) { Gfx = ggfx, RenderMode = true, Present = _ => { } };
        gvm.Run(gchunk);
        Check("图形: VM 执行 LINE", ggfx.Pixels.Get(30, 10) == 4);
        Check("图形: VM 执行 PSET", ggfx.Pixels.Get(100, 100) == 7);
        Check("图形: VM 执行 CIRCLE+PAINT", ggfx.Pixels.Get(160, 100) == 3);
    }

    // ---------- SUB / FUNCTION / TYPE / CONST / DECLARE / ON ERROR ----------
    private static void TestRoutines()
    {
        // SUB 无参 + CALL
        var (o1, _, _) = ExecProg("CALL Hello()\nEND\nSUB Hello\nPRINT \"hi\"\nEND SUB");
        Check("SUB: CALL 无参", o1 == "hi\n");

        // SUB 带参
        var (o2, _, _) = ExecProg("CALL Greet(\"hi\")\nEND\nSUB Greet(m$)\nPRINT m$\nEND SUB");
        Check("SUB: 带参调用", o2 == "hi\n");

        // SUB 裸名调用（不带 CALL）
        var (o3, _, _) = ExecProg("Greet \"hello\"\nEND\nSUB Greet(m$)\nPRINT m$\nEND SUB");
        Check("SUB: 裸名调用", o3 == "hello\n");

        // FUNCTION 返回值
        var (o4, _, _) = ExecProg("PRINT Add(2, 3)\nEND\nFUNCTION Add(a, b)\nAdd = a + b\nEND FUNCTION");
        Check("FUNCTION: 返回值", o4 == "5\n");

        // FUNCTION 用于表达式
        var (o5, _, _) = ExecProg("PRINT Add(2, 3) * 2\nEND\nFUNCTION Add(a, b)\nAdd = a + b\nEND FUNCTION");
        Check("FUNCTION: 表达式", o5 == "10\n");

        // 例程声明顺序无关（先调用后定义）
        var (o6, _, _) = ExecProg("PRINT Twice(21)\nEND\nFUNCTION Twice(x)\nTwice = x * 2\nEND FUNCTION");
        Check("FUNCTION: 后定义", o6 == "42\n");

        // TYPE 字段访问
        var (o7, _, _) = ExecProg("TYPE Point\nx AS INTEGER\ny AS INTEGER\nEND TYPE\nDIM p AS Point\np.x = 3\np.y = 4\nPRINT p.x + p.y");
        Check("TYPE: 字段访问", o7 == "7\n");

        // CONST 常量
        var (o8, _, _) = ExecProg("CONST X = 5\nPRINT X + 1");
        Check("CONST: 常量", o8 == "6\n");

        // CONST 表达式引用常量
        var (o9, _, _) = ExecProg("CONST A = 2\nCONST B = A * 3\nPRINT B");
        Check("CONST: 表达式常量", o9 == "6\n");

        // DECLARE 声明（应被忽略）+ 后续例程可用
        var (o10, _, _) = ExecProg("DECLARE SUB Hello()\nCALL Hello()\nEND\nSUB Hello\nPRINT \"ok\"\nEND SUB");
        Check("DECLARE: 声明不影响例程", o10 == "ok\n");

        // ON ERROR GOTO 捕获除零
        var (o11, _, _) = ExecProg("ON ERROR GOTO handler\nPRINT 10 / 0\nEND\nhandler:\nPRINT \"caught\"\nEND");
        Check("ON ERROR: 捕获除零", o11 == "caught\n");
    }

    // ---------- DEF FN / 零参函数 / 字符串关系比较 ----------
    private static void TestDefFnAndZeroArg()
    {
        // DEF FN 连写形式（DEF FnName，FN 与名称无空格）
        var (o1, _, _) = ExecProg("DEF FnDbl(x) = x * 2\nPRINT FnDbl(21)");
        Check("DEF FN: 连写形式", o1 == "42\n");

        // DEF FN 参数隔离：形参 x 不得覆盖调用处的同名变量 x
        var (o2, _, _) = ExecProg("x = 10\nDEF FnAdd(x) = x + 1\nPRINT FnAdd(5) + x");
        Check("DEF FN: 参数隔离", o2 == "16\n");

        // 零参函数裸用（不带括号）：INKEY$（空队列返回 ""）
        var il = new Lexer("IF INKEY$ = \"\" THEN PRINT \"empty\"");
        var ip = new Parser(il.Tokenize());
        var ists = ip.ParseProgram();
        var ichunk = new CodeGen(ip.Routines, ip.DefFns, ip.Types).Compile(ists, ip.Data);
        var iout = new MemoryOutput();
        var ivm = new Vm(new QueueInputProvider(Array.Empty<string>()), iout) { Keys = new QueueKeyProvider() };
        ivm.Run(ichunk);
        Check("零参函数: INKEY$ 裸用", iout.All == "empty\n");

        // 零参函数裸用（不带括号）：TIMER（始终 >= 0）
        var (o3, _, _) = ExecProg("IF TIMER >= 0 THEN PRINT \"t\"");
        Check("零参函数: TIMER 裸用", o3 == "t\n");

        // 字符串关系比较（CASE "0" TO "9" 数字范围判定）
        var (o4, _, _) = ExecProg("SELECT CASE \"5\"\nCASE \"0\" TO \"9\"\nPRINT \"digit\"\nEND SELECT");
        Check("字符串比较: 数字范围", o4 == "digit\n");

        // 零参用户函数裸调用（无括号、无类型后缀）：MachSpeed = CalcDelay（声明 CalcDelay!）
        var (o5, _, _) = ExecProg("MachSpeed = CalcDelay\nPRINT MachSpeed\nFUNCTION CalcDelay!\nCalcDelay! = 42\nEND FUNCTION");
        Check("零参函数: 用户函数裸用", o5 == "42\n");
    }

    // ---------- GORILLA.BAS 可玩性冒烟 ----------
    private static void TestGorillaPlayable()
    {
        // 定位官方 GORILLA.BAS（相对 cwd 搜索多个候选路径）
        string? path = null;
        foreach (var cand in new[] { "samples/GORILLA.BAS", "QBasic/samples/GORILLA.BAS", "../samples/GORILLA.BAS", "../../QBasic/samples/GORILLA.BAS" })
            if (System.IO.File.Exists(cand)) { path = cand; break; }
        if (path == null) { Check("GORILLA: 样本文件定位", false); return; }
        Check("GORILLA: 样本文件定位", true);

        var lexer = new Lexer(System.IO.File.ReadAllText(path));
        var parser = new Parser(lexer.Tokenize());
        var stmts = parser.ParseProgram();
        var gen = new CodeGen(parser.Routines, parser.DefFns, parser.Types);
        var chunk = gen.Compile(stmts, parser.Data);
        Check("GORILLA: 编译通过", true);

        var gfx = new GfxDevice();
        var vm = new Vm(new QueueInputProvider(Array.Empty<string>()), new MemoryOutput())
        {
            Gfx = gfx,
            RenderMode = true,
            Present = _ => { },
            // 时间门控按键：Intro 的 SparklePause 需空格，GorillaIntro 需任意键。
            // 绝对时间确保各 `WHILE INKEY$ <> "": WEND` 清空循环在键释放前看到空队列。
            Keys = new TimedKeyProvider((900, " "), (1800, "P")),
            MaxSteps = int.MaxValue,
        };

        bool runtimeError = false;
        using var cts = new System.Threading.CancellationTokenSource(6000);
        try { vm.Run(chunk, cts.Token); }
        catch (OperationCanceledException) { /* 预期：到达角度输入等待后超时中止 */ }
        catch (RuntimeError) { runtimeError = true; }

        Check("GORILLA: 无运行时错误", !runtimeError);
        Check("GORILLA: 进入图形模式", gfx.Mode == 9);

        // 城市/大猩猩/太阳已绘制（非背景像素足够多）
        int nonZero = 0;
        for (int y = 0; y < gfx.Pixels.Height; y++)
            for (int x = 0; x < gfx.Pixels.Width; x++)
                if (gfx.Pixels.Get(x, y) != 0) nonZero++;
        Check("GORILLA: 城市已绘制", nonZero > 1000);

        // 到达射击输入提示（"Angle: _"）—— 真正可玩的标志
        bool anglePrompt = false;
        for (int r = 0; r < Math.Min(3, gfx.Text.Rows) && !anglePrompt; r++)
            for (int c = 0; c + 5 < gfx.Text.Cols && !anglePrompt; c++)
                if (gfx.Text.GetChar(r, c) == 'A' && gfx.Text.GetChar(r, c + 1) == 'n' &&
                    gfx.Text.GetChar(r, c + 2) == 'g' && gfx.Text.GetChar(r, c + 3) == 'l' && gfx.Text.GetChar(r, c + 4) == 'e')
                    anglePrompt = true;
        Check("GORILLA: 到达射击输入", anglePrompt);
    }

}
