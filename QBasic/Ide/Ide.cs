// =============================================================
// Ide.cs —— QBasic 风格 IDE 主界面
//
// 基于 TuiApp 搭建：顶部菜单栏（File/Edit/Run/Help）、中部全屏
// 编辑器（TextBox + BASIC 语法高亮）、底部状态栏。F5 运行程序、
// Ctrl+C 停止（协作式中断）、运行结果以内嵌 TUI 视图展示，
// 不再破坏全屏。
// =============================================================
using System.Collections.Concurrent;
using QBasic.Compiler;
using QBasic.Controls;
using QBasic.Tui;

namespace QBasic.Ide;

/// <summary>供 INPUT 语句使用的、由 TUI 事件循环喂入的行队列。</summary>
public sealed class IdeInputProvider : IInputProvider
{
    private readonly BlockingCollection<string> _q = new();
    private CancellationToken _token;

    public void SetToken(CancellationToken t) => _token = t;
    public void Enqueue(string line) => _q.Add(line);

    public string? ReadLine()
    {
        // 阻塞直到有输入或程序被中断
        return _q.Take(_token);
    }
}

/// <summary>IDE 主类。</summary>
public sealed class Ide
{
    private readonly TuiApp _app;
    private readonly TextBox _editor;
    private readonly StatusBar _status;
    private readonly MenuBar _menuBar;
    private string _filename = "untitled.bas";
    private bool _showOutput;   // 是否处于输出视图
    private bool _running;      // 程序是否在后台运行
    private string _outputText = "";
    private string _statusLine = "按任意键返回编辑器";

    private CancellationTokenSource? _cts;
    private Thread? _runThread;
    private IdeInputProvider? _inputProvider;
    private string _inputBuffer = "";

    public Ide()
    {
        _app = new TuiApp();
        _editor = new TextBox();
        _status = new StatusBar();
        _menuBar = BuildMenu();
        SetupLayout();
        _app.OnF5 = RunProgram;
        _app.OnCtrlC = StopRun;
        _app.OnMenuSelect = HandleMenu;
        _app.GlobalInput = GlobalInputHandler;
        _app.Add(_editor);
        _app.SetFocus(_editor);
    }

    private MenuBar BuildMenu()
    {
        var bar = new MenuBar();
        var file = new Menu("File", 'f');
        file.Items.Add(new MenuItem("New", () => { _editor.SetText(""); _filename = "untitled.bas"; }));
        file.Items.Add(new MenuItem("Open...", OpenFile));
        file.Items.Add(new MenuItem("Save", SaveFile));
        file.Items.Add(new MenuItem("Save As...", SaveAsFile));
        file.Items.Add(new MenuItem("Exit", () => _app.QuitRequested = true));
        bar.Add(file);

        var edit = new Menu("Edit", 'e');
        edit.Items.Add(new MenuItem("Cut", () => _editor.OnKey(NewCtrl('x'))));
        edit.Items.Add(new MenuItem("Copy", () => _editor.OnKey(NewCtrl('c'))));
        edit.Items.Add(new MenuItem("Paste", () => _editor.OnKey(NewCtrl('v'))));
        bar.Add(edit);

        var run = new Menu("Run", 'r');
        run.Items.Add(new MenuItem("Start (F5)", RunProgram));
        run.Items.Add(new MenuItem("Stop (Ctrl+C)", StopRun));
        bar.Add(run);

        var help = new Menu("Help", 'h');
        help.Items.Add(new MenuItem("About", ShowAbout));
        bar.Add(help);
        return bar;
    }

    private static InputEvent NewCtrl(char c) => new() { Ch = c, Mods = KeyMods.Ctrl };

    private void SetupLayout()
    {
        _app.MenuBar = _menuBar;
        _app.StatusBar = _status;
        _editor.Row = 2;
        _editor.Col = 1;
        _editor.Height = _app.Screen.Rows - 2;
        _editor.Width = _app.Screen.Cols - 1;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        _status.Left = $"{_filename}   {_editor.CursorLine + 1}:{_editor.CursorCol + 1}   {(_editor.Overwrite ? "Overwrite" : "Insert")}";
        _status.Right = _running ? "运行中 Ctrl+C 停止" : "F5 Run";
    }

    public void Run()
    {
        _app.Run();
    }

    private void HandleMenu(string item)
    {
        foreach (var m in _menuBar.Menus)
            foreach (var it in m.Items)
                if (it.Text == item)
                {
                    it.Action?.Invoke();
                    return;
                }
    }

    // ---------- 运行 / 停止 ----------

    private void RunProgram()
    {
        StopRun(); // 取消上一次运行
        _inputBuffer = "";
        _outputText = "";
        string src = _editor.GetText();

        // 编译（同步，出错直接进入输出视图）
        Chunk chunk;
        try
        {
            var lexer = new Lexer(src);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var stmts = parser.ParseProgram();
            chunk = new CodeGen().Compile(stmts, parser.Data);
        }
        catch (Exception ex) when (ex is ParseException or CompileException)
        {
            _outputText = $"编译错误: {ex.Message}";
            _statusLine = "编译错误 - 按任意键返回编辑器";
            EnterOutputView();
            return;
        }

        var output = new MemoryOutput();
        var input = new IdeInputProvider();
        _inputProvider = input;
        _cts = new CancellationTokenSource();
        input.SetToken(_cts.Token);
        var token = _cts.Token;

        _running = true;
        _showOutput = false;
        _app.CustomRender = null;
        UpdateStatus();
        _app.Invalidate();

        _runThread = new Thread(() =>
        {
            try
            {
                var vm = new Vm(input, output);
                vm.Run(chunk, token);
                _outputText = output.All;
                _statusLine = "运行结束 - 按任意键返回编辑器";
            }
            catch (OperationCanceledException)
            {
                _outputText = output.All + "\n[程序已由 Ctrl+C 停止]";
                _statusLine = "已停止 - 按任意键返回编辑器";
            }
            catch (RuntimeError re)
            {
                _outputText = output.All + $"\n运行时错误(第 {re.Line} 行): {re.Message}";
                _statusLine = "运行时错误 - 按任意键返回编辑器";
            }
            _running = false;
            _cts?.Dispose();
            _cts = null;
            EnterOutputView();
        })
        { IsBackground = true };
        _runThread.Start();
    }

    private void StopRun()
    {
        _cts?.Cancel();
    }

    // ---------- 输出视图（TUI 内嵌，复用 Screen） ----------

    private void EnterOutputView()
    {
        _showOutput = true;
        _app.CustomRender = RenderOutputView;
        UpdateStatus();
        _app.Invalidate();
    }

    private void ExitOutputView()
    {
        _showOutput = false;
        _app.CustomRender = null;
        _app.SetFocus(_editor);
        UpdateStatus();
        _app.Invalidate();
    }

    private void RenderOutputView(Screen screen)
    {
        // 顶栏
        for (int c = 0; c < screen.Cols; c++) screen.Put(0, c, ' ', Color.BrightWhite, Color.Blue);
        screen.PutText(0, 1, "=== 程序输出 ===", Color.BrightWhite, Color.Blue, true);
        // 输出正文
        int row = 1;
        foreach (var line in _outputText.Split('\n'))
        {
            if (row >= screen.Rows - 1) break;
            screen.PutText(row, 1, Cjk.Fit(line, screen.Cols - 1), Color.White, Color.Black);
            row++;
        }
        // 底栏
        for (int c = 0; c < screen.Cols; c++) screen.Put(screen.Rows - 1, c, ' ', Color.BrightWhite, Color.Blue);
        screen.PutText(screen.Rows - 1, 1, Cjk.Fit(_statusLine, screen.Cols - 1), Color.BrightWhite, Color.Blue);
    }

    /// <summary>全局输入钩子：运行期喂给 INPUT，输出屏任意键返回编辑器。</summary>
    private bool GlobalInputHandler(InputEvent ev)
    {
        if (_running)
        {
            // 把键盘输入喂给运行中程序的 INPUT 语句
            if (ev.IsKey(KeyCode.Enter))
            {
                _inputProvider?.Enqueue(_inputBuffer);
                _inputBuffer = "";
                _app.Invalidate();
                return true;
            }
            if (ev.IsKey(KeyCode.Backspace))
            {
                if (_inputBuffer.Length > 0) _inputBuffer = _inputBuffer[..^1];
                _app.Invalidate();
                return true;
            }
            if (ev.Key == KeyCode.None && ev.Mods == KeyMods.None && ev.Ch != 0)
            {
                _inputBuffer += ev.Ch;
                _app.Invalidate();
                return true;
            }
            return true; // 运行中吞掉其余按键，避免干扰编辑器
        }
        if (_showOutput)
        {
            ExitOutputView();
            return true;
        }
        return false;
    }

    // ---------- 文件操作 ----------

    private void OpenFile()
    {
        PromptFile("输入要打开的文件名: ", file =>
        {
            if (!File.Exists(file)) return;
            _editor.SetText(File.ReadAllText(file));
            _filename = Path.GetFileName(file);
            UpdateStatus();
        });
    }

    private void SaveFile() => SaveTo(_filename);

    private void SaveAsFile() => PromptFile("输入要保存的文件名: ", SaveTo);

    private void SaveTo(string file)
    {
        File.WriteAllText(file, _editor.GetText());
        _filename = Path.GetFileName(file);
        UpdateStatus();
    }

    private void PromptFile(string msg, Action<string> done)
    {
        Console.WriteLine(msg);
        string? input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) done(input.Trim());
    }

    private void ShowAbout()
    {
        _outputText = "QBasic IDE (WayCoder 实现)\n一个 DOS QBasic 风格的开发环境：TUI + 编译器 + 虚拟机。\n";
        _statusLine = "按任意键返回编辑器";
        EnterOutputView();
    }
}
