using System.Text;
using WayCoder.UI.Tui.Controls;

namespace WayCoder;

/// <summary>
/// 对话框仅绘制演示 —— 用 TuiDialog.Show 把 1~6 行消息的对话框逐帧画出来，
/// 不进入输入循环、不响应按键，供抓屏核对布局（消息折行/省略号/指定位置）。
/// 运行：waycoder --dialog-show
///   - 交互终端：每帧 Enter 前进，可逐帧抓屏；Ctrl+C 退出。
///   - 重定向（管道/文件）：连续输出全部帧，帧间以纯文本分隔标记。
/// </summary>
public static class DialogShow
{
    public static void Run()
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        bool interactive = !Console.IsInputRedirected;

        // 1~6 行消息（第 6 行触发 MaxMessageLines=5 的折叠，末尾显示省略号）
        var samples = new (string label, string msg)[]
        {
            ("1 行消息", "这是第一行消息"),
            ("2 行消息", "这是第一行消息\n这是第二行消息"),
            ("3 行消息", "这是第一行消息\n这是第二行消息\n这是第三行消息"),
            ("4 行消息", "一\n二\n三\n四"),
            ("5 行消息", "一\n二\n三\n四\n五"),
            ("6 行消息（折叠）", "一\n二\n三\n四\n五\n六（第 6 行，超 5 行折叠）"),
        };

        foreach (var (label, msg) in samples)
        {
            Console.Write(TuiDialog.Show(TuiDialog.Info("消息框测试", msg)));
            Advance(label + "（居中）", interactive);
        }

        // 确认框（2 按钮）+ 确认框（3 按钮）：同样按内容自适应宽高
        Console.Write(TuiDialog.Show(TuiDialog.Confirm("确认框", "确定要删除选中的文件吗？\n此操作不可撤销。\n请再次确认。\n删除后无法恢复。", _ => { })));
        Advance("确认框（2 按钮，4 行消息）", interactive);

        Console.Write(TuiDialog.Show(TuiDialog.Confirm3("三选确认", "请选择要执行的操作：\n继续 / 取消 / 终止。", _ => { })));
        Advance("确认框（3 按钮，2 行消息）", interactive);

        // 任意指定位置：左上角 (3, 2)
        Console.Write(TuiDialog.Show(TuiDialog.Info("指定位置", "左上角 (3, 2)"), x: 3, y: 2));
        Advance("指定位置 (3, 2)", interactive);
    }

    /// <summary>交互终端下等待 Enter 前进（便于逐帧抓屏）；重定向下打印纯文本分隔。</summary>
    private static void Advance(string label, bool interactive)
    {
        Console.Write("\x1b[0m"); // 复位 SGR，避免分隔文字继承对话框配色
        if (interactive)
        {
            Console.Write("\n\n—— 上面是「" + label + "」。按 Enter 看下一个，Ctrl+C 退出 ——");
            try { Console.ReadKey(true); } catch (InvalidOperationException) { }
        }
        else
        {
            Console.Write("\n\n===== [" + label + "] =====\n");
        }
    }
}
