using System.Text;
using WayCoder.Maui.Services;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 内置代码编辑器（M4）。
///
/// 编辑 UI 用 MAUI <see cref="Editor"/> 原生控件（触控软键盘 + 系统级撤销/重做），
/// 文件读写走 <see cref="SandboxFsService"/>（沙箱钳制），大纲/符号提取复用
/// <see cref="EditorCore"/>（纯数据模型，无渲染/键盘依赖）。
///
/// 编辑与分析的桥接：<see cref="BuildCoreFromEditor"/> 用 LoadFile 设置 FilePath+Syntax
/// （正确推断扩展名/语法），再把编辑器当前文本覆盖进 Lines，供 ExtractOutline 分析——
/// 这样未保存的编辑也能出大纲，而非只能分析落盘旧内容。
/// </summary>
[QueryProperty(nameof(FilePath), "path")]
public partial class EditorPage : ContentPage
{
    /// <summary>沙箱内相对路径（Shell 路由参数 "path"）。</summary>
    public string FilePath
    {
        set => _ = LoadAsync(value);
    }

    private string _filePath = "";
    private bool _modified;
    private bool _loading; // 装载时抑制 TextChanged 把初值误标为「已修改」

    public EditorPage()
    {
        InitializeComponent();
    }

    /// <summary>从沙箱相对路径加载文件内容到编辑器。</summary>
    private async Task LoadAsync(string relPath)
    {
        _loading = true;
        _filePath = relPath;
        var content = SandboxFsService.ReadText(relPath);
        if (content == null)
        {
            CodeEditor.Text = "";
            FileLabel.Text = Path.GetFileName(relPath);
            Title = Path.GetFileName(relPath);
            _modified = false;
            _loading = false;
            UpdateStatus();
            await DisplayAlertAsync("无法打开", $"文件不存在或为二进制：{relPath}", "关闭");
            return;
        }

        CodeEditor.Text = content;
        FileLabel.Text = relPath;
        Title = Path.GetFileName(relPath);
        _modified = false;
        _loading = false;
        UpdateStatus();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _modified = true;
        UpdateStatus();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath)) return;
        try
        {
            SandboxFsService.WriteText(_filePath, CodeEditor.Text ?? "");
            _modified = false;
            UpdateStatus();
            await DisplayAlertAsync("已保存", _filePath, "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("保存失败", ex.Message, "关闭");
        }
    }

    /// <summary>提取大纲并让用户点选跳行。</summary>
    private async void OnOutlineClicked(object? sender, EventArgs e)
    {
        var outline = BuildCoreFromEditor().ExtractOutline();
        if (outline.Count == 0)
        {
            await DisplayAlertAsync("大纲", "当前文件没有可识别的符号", "关闭");
            return;
        }

        var names = outline.Select(o => $"{o.Icon} {o.Name}  (L{o.Line})").ToArray();
        var choice = await DisplayActionSheetAsync("大纲", "取消", null, names);
        if (string.IsNullOrEmpty(choice) || choice == "取消") return;

        var idx = Array.IndexOf(names, choice);
        if (idx < 0) return;
        JumpToLine(outline[idx].Line);
    }

    /// <summary>把编辑器当前内容构造为一个 EditorCore（供大纲/诊断分析，含未保存编辑）。</summary>
    private EditorCore BuildCoreFromEditor()
    {
        var core = new EditorCore();
        // LoadFile 设置 FilePath（推断扩展名）+ Syntax；文件不存在时仍能正确设定二者
        var full = SandboxFsService.ResolveInSandbox(_filePath) ?? Path.GetFileName(_filePath);
        core.LoadFile(full);

        // 覆盖 Lines 为编辑器当前文本（含未保存内容），Normalize 行尾避免 CRLF 干扰
        core.Lines.Clear();
        var text = (CodeEditor.Text ?? "").Replace("\r\n", "\n");
        foreach (var ln in text.Split('\n'))
            core.Lines.Add(new StringBuilder(ln));
        if (core.Lines.Count == 0)
            core.Lines.Add(new StringBuilder());
        return core;
    }

    /// <summary>跳转到 1-based 行号（计算该行首字符偏移并聚焦）。</summary>
    private void JumpToLine(int oneBasedLine)
    {
        var text = CodeEditor.Text ?? "";
        int line = 1, offset = 0;
        while (offset < text.Length && line < oneBasedLine)
        {
            if (text[offset] == '\n') line++;
            offset++;
        }
        CodeEditor.CursorPosition = offset;
        CodeEditor.Focus();
    }

    private void UpdateStatus()
    {
        var text = CodeEditor.Text ?? "";
        int lines = 1, chars = text.Length;
        foreach (var c in text)
            if (c == '\n') lines++;

        var mark = _modified ? "● " : "";
        StatusLabel.Text = $"{mark}{lines} 行 · {chars} 字符";
    }
}
