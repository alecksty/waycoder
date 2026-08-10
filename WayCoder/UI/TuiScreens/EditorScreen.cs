using System.Text;
using WayCoder.Terminal;
using WayCoder.Tools;
using WayCoder.UI.Controls;

using WayCoder.UI.TuiControls;

namespace WayCoder.UI.TuiScreens;

/// <summary>
/// 编辑器屏幕 —— 终端内源码编辑器，与 ChatScreen 平级。
///
/// 布局：
///   RootView (VBox)
///   ├─ TitleBar      TuiLabel      " /edit: filename.cs [已修改] "
///   ├─ EditorView    TuiRichEditor  编辑区域（TH-4 行）
///   ├─ StatusBar1    TuiLabel      " L1:C10 | 行:42 字符:2048 | C# · UTF-8"
///   └─ StatusBar2    TuiLabel      " ^S保存 ^Z撤销 ^G跳行 Esc退出"
///
/// 生命周期：
///   Activate() → 有路径: LoadAndBuild / 无路径: ShowFilePicker（不阻塞）
///   OnKey → 模态窗口优先 → 路由 EditorView → 未处理回退基类
///   OnResize  → 重建布局 + 重新绑定事件
///   Deactivate → 基础清理
/// </summary>
public class EditorScreen : TuiScreen
{
    // ── 组件 ──
    public EditorCore Core { get; private set; } = null!;
    public TuiRichEditor EditorView { get; private set; } = null!;
    public TuiLabel TitleBar { get; private set; } = null!;
    public TuiLabel StatusBar1 { get; private set; } = null!;
    public TuiLabel StatusBar2 { get; private set; } = null!;

    private Action? _onContentChangedHandler;

    /// <summary>要编辑的文件路径（空 = 弹出文件选择器）</summary>
    public string FilePath { get; set; }

    /// <summary>退出前是否已保存</summary>
    public bool WasSaved { get; private set; }

    public EditorScreen(string filePath = "")
    {
        Name = "editor";
        FilePath = filePath;
    }

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    public override void Activate()
    {
        base.Activate();

        if (string.IsNullOrWhiteSpace(FilePath))
            ShowFilePicker();   // 纯回调驱动，不阻塞
        else
            LoadAndBuild(FilePath);
    }

    public override void OnResize(int newW, int newH)
    {
        base.OnResize(newW, newH);

        // 如果还没加载完（文件选择器打开中），跳过
        if (EditorView == null) return;

        // 重建布局以适应新尺寸
        TitleBar.Width = TW;
        EditorView.Width = TW;
        EditorView.Height = Math.Max(5, TH - 4);
        StatusBar1.Width = TW;
        StatusBar2.Width = TW;
        RootView.Layout();
        MarkDirty();
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }

    // ════════════════════════════════════════════════════════════════
    // 文件加载 + 布局构建
    // ════════════════════════════════════════════════════════════════

    private void LoadAndBuild(string path)
    {
        Core = new EditorCore();
        Core.LoadFile(path);
        _onContentChangedHandler = () => MarkDirty();
        Core.OnContentChanged += _onContentChangedHandler;
        BuildLayout();
    }

    private void BuildLayout()
    {
        RootView = new TuiVBox { Width = TW, Height = TH };

        // ── 标题栏 ──
        TitleBar = new TuiLabel("") { Width = TW, Height = 1, Bg = 33, Fg = 30 };
        RootView.Add(TitleBar);

        // ── 编辑区域 ──
        int editorH = Math.Max(5, TH - 4);
        EditorView = new TuiRichEditor
        {
            Core = Core,
            Width = TW,
            Height = editorH,
            Focused = true
        };
        EditorView.OnSaveRequested += HandleSave;
        EditorView.OnJumpRequested += HandleJump;
        EditorView.OnExitRequested += HandleExit;
        RootView.Add(EditorView);

        // ── 状态栏 1 — 光标 + 统计 + 诊断 ──
        StatusBar1 = new TuiLabel("") { Width = TW, Height = 1, Bg = 47 };
        RootView.Add(StatusBar1);

        // ── 状态栏 2 — 文件路径 + 快捷键 ──
        StatusBar2 = new TuiLabel("") { Width = TW, Height = 1 };
        RootView.Add(StatusBar2);

        RootView.Layout();
        MarkDirty();
    }

    /// <summary>屏幕销毁时取消所有事件订阅，避免泄漏</summary>
    public override void OnDestroy()
    {
        if (Core != null && _onContentChangedHandler != null)
            Core.OnContentChanged -= _onContentChangedHandler;
        if (EditorView != null)
        {
            EditorView.OnSaveRequested -= HandleSave;
            EditorView.OnJumpRequested -= HandleJump;
            EditorView.OnExitRequested -= HandleExit;
        }
        base.OnDestroy();
    }

    // ════════════════════════════════════════════════════════════════
    // 渲染
    // ════════════════════════════════════════════════════════════════

    public override void Render(StringBuilder sb)
    {
        if (EditorView == null) { base.Render(sb); return; }

        UpdateStatusBars();
        // 确保编辑区尺寸跟随终端
        EditorView.Height = Math.Max(5, TH - 4);
        EditorView.Width = TW;
        base.Render(sb);
    }

    private void UpdateStatusBars()
    {
        var fileName = Path.GetFileName(Core.FilePath);
        var title = $" /edit: {fileName} ";
        if (Core.Modified) title += "[已修改] ";
        TitleBar.Text = title;

        var (errors, warnings) = Core.GetDiagSummary();
        var diagPart = "";
        if (errors > 0) diagPart = $" | {AnsiTty.Fg(31)}● {errors} errors";
        else if (warnings > 0) diagPart = $" | {AnsiTty.Fg(33)}▲ {warnings} warnings";

        StatusBar1.Text = $" L{Core.Cy + 1}:C{Core.Cx + 1} | " +
                          $"行:{Core.TotalLines} 字符:{Core.TotalChars} | " +
                          $"{EditorCore.FormatSize(Core.FileSizeBytes)} | " +
                          $"{Core.Syntax.Name} · UTF-8{diagPart}";

        var pathDisplay = Core.FilePath;
        if (pathDisplay.Length > 60) pathDisplay = "..." + pathDisplay[^57..];
        StatusBar2.Text = $" {pathDisplay}  " +
                          "^S保存 ^Z撤销 ^G跳行 ^X剪切 ^C复制 ^V粘贴 Esc退出";
    }

    // ════════════════════════════════════════════════════════════════
    // 键盘路由
    // ════════════════════════════════════════════════════════════════

    public override bool OnKey(ConsoleKeyInfo key)
    {
        // 模态窗口优先
        if (HasModal)
            return base.OnKey(key);

        // 未加载完成（文件选择器打开中）
        if (EditorView == null)
            return base.OnKey(key);

        // 路由 EditorView → 未处理回退基类
        return EditorView.OnKey(key) || base.OnKey(key);
    }

    // ════════════════════════════════════════════════════════════════
    // 事件处理
    // ════════════════════════════════════════════════════════════════

    private void HandleSave()
    {
        try
        {
            Core.Save();
            _ = Core.SaveAsync();   // 异步触发 lint
            WasSaved = true;
            ShowToast("已保存", 1200);
        }
        catch (Exception ex)
        {
            ShowWindow(TuiDialog.Error("保存失败", ex.Message));
        }
    }

    private void HandleJump()
    {
        var win = TuiDialog.Input("跳转到行",
            $"输入行号 (1-{Core.TotalLines})",
            (Core.Cy + 1).ToString(),
            input =>
            {
                if (int.TryParse(input, out var ln) && ln >= 1 && ln <= Core.TotalLines)
                    if (Core.JumpToLine(ln))
                        MarkDirty();
            });
        ShowWindow(win);
    }

    private void HandleExit()
    {
        if (!Core.Modified)
        {
            Manager?.PopScreen();
            return;
        }

        var win = TuiDialog.Confirm3("文件已修改",
            "是否保存更改？",
            result =>
            {
                switch (result)
                {
                    case TuiDialog.DialogResult.Yes:
                        try { Core.Save(); WasSaved = true; }
                        catch (Exception ex)
                        {
                            ShowWindow(TuiDialog.Error("保存失败", ex.Message));
                            return;
                        }
                        Manager?.PopScreen();
                        break;
                    case TuiDialog.DialogResult.No:
                        Manager?.PopScreen();
                        break;
                    // Cancel → 继续编辑
                }
            });
        ShowWindow(win);
    }

    // ════════════════════════════════════════════════════════════════
    // 文件选择器（纯回调驱动）
    // ════════════════════════════════════════════════════════════════

    private void ShowFilePicker()
    {
        var recent = EditFileTool.ChangedFiles.Take(9).ToList();
        var choices = new List<string> { "📝 输入文件路径..." };
        if (recent.Count > 0)
        {
            choices.Add("── 最近编辑 ──");
            choices.AddRange(recent);
        }

        var selectWin = TuiDialog.Select("选择要编辑的文件", choices, idx =>
        {
            if (choices[idx].StartsWith("──"))
                return;

            if (idx == 0)
            {
                var inputWin = TuiDialog.Input("文件路径",
                    "输入要编辑的文件路径（相对或绝对路径）", "",
                    path =>
                    {
                        var trimmed = path?.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            LoadAndBuild(trimmed);
                        else
                            Manager?.PopScreen();
                    });
                ShowWindow(inputWin);
            }
            else
            {
                var file = recent[idx - (recent.Count > 0 ? 2 : 1)];
                LoadAndBuild(file);
            }
        });

        selectWin.OnClosed = () =>
        {
            if (Core == null) Manager?.PopScreen();
        };
        ShowWindow(selectWin);
    }
}
