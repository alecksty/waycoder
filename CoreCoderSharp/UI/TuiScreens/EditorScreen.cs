using System.Text;
using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.Controls;

namespace CoreCoderSharp.UI;

/// <summary>
/// 编辑器屏幕 —— 终端内源码编辑器，使用新 TUI 架构。
///
/// 布局：
///   RootView (VBox)
///   ├─ TitleBar      TuiLabel      " /edit: filename.cs [已修改] "
///   ├─ EditorView    TuiRichEditor  编辑区域
///   ├─ StatusBar1    TuiLabel      " L1:C10 | 行:42 字符:2048 | C# · UTF-8"
///   └─ StatusBar2    TuiLabel      " ^S保存 ^Z撤销 ^G跳行 Esc退出"
///
/// 键盘：编辑键 → TuiRichEditor → EditorCore
///       全局键 → Ctrl+S(保存) Ctrl+G(跳行) Escape(退出)
/// </summary>
public class EditorScreen : TuiScreen
{
    // ── 组件 ──
    public EditorCore Core { get; private set; } = null!;
    public TuiRichEditor EditorView { get; private set; } = null!;
    public TuiLabel TitleBar { get; private set; } = null!;
    public TuiLabel StatusBar1 { get; private set; } = null!;
    public TuiLabel StatusBar2 { get; private set; } = null!;

    /// <summary>要编辑的文件路径（空 = 提示输入）</summary>
    public string FilePath { get; set; }

    /// <summary>退出后是否已保存（供调用方检查）</summary>
    public bool WasSaved { get; private set; }

    public EditorScreen(string filePath = "")
    {
        Name = "editor";
        FilePath = filePath;
    }

    // ── 生命周期 ──

    public override void Activate()
    {
        base.Activate();

        if (string.IsNullOrWhiteSpace(FilePath))
        {
            ShowFilePicker();
            // 不阻塞 — 对话框回调会触发 LoadAndBuild 或 PopScreen
        }
        else
        {
            LoadAndBuild(FilePath);
        }
    }

    /// <summary>加载文件并构建编辑器布局</summary>
    private void LoadAndBuild(string path)
    {
        Core = new EditorCore();
        Core.LoadFile(path);
        Core.OnContentChanged += () => MarkDirty();
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

        // ── 状态栏 1（文件信息 + 诊断） ──
        StatusBar1 = new TuiLabel("") { Width = TW, Height = 1, Bg = 7 };
        RootView.Add(StatusBar1);

        // ── 状态栏 2（文件路径 + 快捷键提示） ──
        StatusBar2 = new TuiLabel("") { Width = TW, Height = 1 };
        RootView.Add(StatusBar2);

        RootView.Layout();
        MarkDirty();
    }

    // ── 渲染前更新动态文本 ──

    private void UpdateDynamicText()
    {
        // 标题栏
        var fileName = Path.GetFileName(Core.FilePath);
        var title = $" /edit: {fileName} ";
        if (Core.Modified) title += "[已修改] ";
        TitleBar.Text = title;

        // 状态栏 1: 光标位置 + 统计 + 诊断摘要
        var (errors, warnings) = Core.GetDiagSummary();
        var diagPart = "";
        if (errors > 0) diagPart = $" | \x1b[31m● {errors} errors";
        else if (warnings > 0) diagPart = $" | \x1b[33m▲ {warnings} warnings";

        StatusBar1.Text = $" L{Core.Cy + 1}:C{Core.Cx + 1} | " +
                          $"行:{Core.TotalLines} 字符:{Core.TotalChars} | " +
                          $"{EditorCore.FormatSize(Core.FileSizeBytes)} | " +
                          $"{Core.Syntax.Name} · UTF-8{diagPart}";

        // 状态栏 2: 路径 + 快捷键
        var pathDisplay = Core.FilePath;
        if (pathDisplay.Length > 60) pathDisplay = "..." + pathDisplay[^57..];
        StatusBar2.Text = $" {pathDisplay}  " +
                          "^S保存 ^Z撤销 ^G跳行 ^X剪切 ^C复制 ^V粘贴 Esc退出";
    }

    public override void Render(StringBuilder sb)
    {
        UpdateDynamicText();
        AdjustScrollForEditor();
        base.Render(sb);
    }

    private void AdjustScrollForEditor()
    {
        EditorView.Height = Math.Max(5, TH - 4);
        EditorView.Width = TW;
    }

    // ── 全局键盘处理 ──

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        // 有模态窗口时，让基类处理
        if (HasModal)
            return base.HandleKey(key);

        // 路由给 EditorView
        return EditorView.HandleKey(key);
    }

    // ── 事件处理 ──

    private void HandleSave()
    {
        try
        {
            Core.Save();
            _ = Core.SaveAsync(); // 异步触发 lint
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
                {
                    if (Core.JumpToLine(ln))
                        MarkDirty();
                }
            });
        ShowWindow(win);
    }

    private void HandleExit()
    {
        if (Core.Modified)
        {
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
                        case TuiDialog.DialogResult.Cancel:
                            break; // 继续编辑
                    }
                });
            ShowWindow(win);
        }
        else
        {
            Manager?.PopScreen();
        }
    }

    // ── 文件选择（纯回调驱动，不阻塞 Activate） ──

    /// <summary>显示文件选择对话框，选好后加载文件构建布局</summary>
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
                return; // 分隔线，不处理

            if (idx == 0)
            {
                // "输入文件路径..." → 弹出输入框
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
                // 最近文件
                var file = recent[idx - (recent.Count > 0 ? 2 : 1)];
                LoadAndBuild(file);
            }
        });

        // 用户 Esc 关闭选择对话框 → 返回聊天界面
        selectWin.OnClosed = () =>
        {
            if (Core == null)   // 没选文件
                Manager?.PopScreen();
        };
        ShowWindow(selectWin);
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }
}
