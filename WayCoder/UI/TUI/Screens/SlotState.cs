using System.Collections.Concurrent;
using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.Tools;
using WayCoder.UI.Tui.ToolRenderers;

using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
namespace WayCoder.UI.Tui.Screens;

/// <summary>槽位状态</summary>
public enum SlotState : byte
{
    Idle = 0,
    Working = 1,
    WaitingPerm = 2,
    Error = 3
}
