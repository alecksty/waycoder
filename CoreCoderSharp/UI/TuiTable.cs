using Spectre.Console;

namespace CoreCoderSharp.UI;

/// <summary>
/// 表格控件 —— Spectre.Console Table 的便捷封装。
/// CJK 感知：Spectre.Console Table 内置 Unicode 宽度处理，中文自动对齐。
/// </summary>
public class TuiTable
{
    private readonly Table _table;

    public TuiTable(string? title = null)
    {
        _table = new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = TuiColors.TableBorder,
        };

        if (title != null)
        {
            _table.Title = new TableTitle(
                TuiHelper.Esc(title),
                TuiColors.TableHeading);
        }
    }

    /// <summary>添加一列</summary>
    /// <param name="header">列标题</param>
    /// <param name="width">固定宽度（字符数），可选。不设则自动分配。</param>
    /// <param name="alignment">对齐方式</param>
    public TuiTable AddColumn(string header, int? width = null,
        Justify alignment = Justify.Left)
    {
        var col = new TableColumn(TuiHelper.Esc(header))
        {
            Width = width,
        };
        _table.AddColumn(col);
        return this;
    }

    /// <summary>添加一个数据行。参数自动转义 Spectre 标记。</summary>
    public TuiTable AddRow(params string[] cells)
    {
        var escaped = cells.Select(c => new Markup(TuiHelper.Esc(c))).ToArray();
        _table.AddRow(escaped);
        return this;
    }

    /// <summary>添加一个带 Markup 样式的行（不对内容转义，调用方负责安全）。</summary>
    public TuiTable AddMarkupRow(params string[] markupCells)
    {
        var markups = markupCells.Select(m => new Markup(m)).ToArray();
        _table.AddRow(markups);
        return this;
    }

    /// <summary>渲染表格到控制台</summary>
    public void Render()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(_table);
        AnsiConsole.WriteLine();
    }

    /// <summary>获取内部 Table 对象（用于高级定制）</summary>
    public Table Raw => _table;
}
