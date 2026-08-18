using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 数据列表 —— 用 .tui 单元格模板渲染每行（自定义单元格）。
/// 每项数据是 key-value，单元格模板用 {key} 占位符引用（如 text="{name}"）。
/// </summary>
public class TuiDataList : TuiControl
{
    /// <summary>数据项（key-value，供单元格模板 {key} 占位符）。</summary>
    public List<Dictionary<string, string>> Items { get; set; } = [];

    /// <summary>单元格模板（.tui 片段，含 {key} 占位符）。</summary>
    public string CellMarkup { get; set; } = "";

    public int SelectedIndex { get; set; } = -1;

    public override bool CanFocus => false;

    public TuiDataList() { Height = 1; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int h = Math.Min(Items.Count, Height);
        for (int i = 0; i < h; i++)
        {
            TuiView cell;
            try { cell = TuiMarkup.LoadCell(CellMarkup, Items[i]); }
            catch { cell = new TuiVBox(); cell.Add(new TuiLabel(Items[i].GetValueOrDefault("text", ""))); }

            cell.Width = Width;
            cell.Height = 1;
            cell.OnResize(Width, 1); // 触发布局，放置子控件（否则子控件堆在 0,0 重叠）
            cell.Render(sb, absX, absY + i, ClipLeft, ClipTop, ClipRight, ClipBottom);
        }
    }
}
