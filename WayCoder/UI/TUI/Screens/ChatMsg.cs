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
}
