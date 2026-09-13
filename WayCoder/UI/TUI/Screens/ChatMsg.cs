using System.Collections.Concurrent;
using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.Tools;
using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
namespace WayCoder.UI.Tui.Screens;

/// <summary>聊天消息数据结构</summary>
public class ChatMsg
{
    public string Role { get; set; } = "system";
    public string Content { get; set; } = "";
    public string? SessionId { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public int TokenCount { get; set; }
    public bool Streaming { get; set; }
    /// <summary>内容横向居中（仅欢迎消息使用）</summary>
    public bool Centered { get; set; }
    /// <summary>嵌套层级（0=顶层；1=工具子消息，缩进在所属 assistant 消息下）</summary>
    public int Indent { get; set; }
    /// <summary>Shell/命令输出块：每行加 │ 竖线前缀（模拟终端滚动区）。槽位切换/恢复会话需忠实重建，必须持久化到 DTO。</summary>
    public bool ShellBlock { get; set; }

    /// <summary>
    /// 思考正文（仅内存，**不落盘**）。思考定稿折叠后正文从渲染层移除，靠这里保留一份
    /// 尾部窗口（<see cref="Global.MaxSingleMessageChars"/>）供「点开思考详情」查看。
    /// 对齐 MAUI：思考不进会话文件（会话由 Agent.SnapshotMessages 生成，本字段不在其中）。
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>思考耗时（秒）。0 = 思考中/未知；定稿折叠时写入（≥1）。</summary>
    public int ThinkingSeconds { get; set; }
}
