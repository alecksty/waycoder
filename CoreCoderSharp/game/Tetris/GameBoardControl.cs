using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Tetris;

/// <summary>
/// 主棋盘渲染控件：绘制网格、Ghost 投影、已锁定方块、当前活动方块，
/// 游戏结束时叠加半透明遮罩与提示文字。
/// </summary>
public class GameBoardControl : Control
{
    public static readonly StyledProperty<GameEngine?> EngineProperty =
        AvaloniaProperty.Register<GameBoardControl, GameEngine?>(nameof(Engine));

    public static readonly StyledProperty<int> CellSizeProperty =
        AvaloniaProperty.Register<GameBoardControl, int>(nameof(CellSize), 30);

    public GameEngine? Engine
    {
        get => GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }

    public int CellSize
    {
        get => GetValue(CellSizeProperty);
        set => SetValue(CellSizeProperty, value);
    }

    static GameBoardControl()
    {
        AffectsRender<GameBoardControl>(EngineProperty, CellSizeProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var engine = Engine;
        if (engine is null) return;

        int cell = CellSize;
        int width = GameEngine.Cols * cell;
        int height = GameEngine.Rows * cell;

        // 背景
        context.DrawRectangle(Brush("#1A1A2E"), null, new Rect(0, 0, width, height));

        // 网格线
        var gridPen = new Pen(Brush("#2A2A4A"), 0.5);
        for (int x = 1; x < GameEngine.Cols; x++)
            context.DrawLine(gridPen, new Point(x * cell, 0), new Point(x * cell, height));
        for (int y = 1; y < GameEngine.Rows; y++)
            context.DrawLine(gridPen, new Point(0, y * cell), new Point(width, y * cell));

        // 已锁定方块
        for (int y = 0; y < GameEngine.Rows; y++)
        for (int x = 0; x < GameEngine.Cols; x++)
        {
            var t = engine.GetCell(y, x);
            if (t is null) continue;
            DrawBlock(context, x * cell, y * cell, cell, Tetromino.ColorHex(t.Value));
        }

        if (!engine.IsGameOver)
        {
            var shape = engine.CurrentShape;
            var hex = Tetromino.ColorHex(engine.CurrentType);

            // Ghost 投影（当前方块垂直下落到的位置，半透明描边）
            int gy = engine.GhostY;
            int gpx = engine.CurrentX * cell;
            int gpy = gy * cell;
            var baseColor = Color.Parse(hex);
            var ghostBrush = new SolidColorBrush(new Color(45, baseColor.R, baseColor.G, baseColor.B));
            var ghostPen = new Pen(new SolidColorBrush(new Color(160, baseColor.R, baseColor.G, baseColor.B)), 2);
            for (int dy = 0; dy < 4; dy++)
            for (int dx = 0; dx < 4; dx++)
            {
                if (shape[dy, dx] == 0) continue;
                int bx = gpx + dx * cell;
                int by = gpy + dy * cell;
                if (by < 0) continue;
                var rect = new Rect(bx + 1, by + 1, cell - 2, cell - 2);
                context.DrawRectangle(ghostBrush, ghostPen, rect);
            }

            // 当前活动方块
            int px = engine.CurrentX * cell;
            int py = engine.CurrentY * cell;
            for (int dy = 0; dy < 4; dy++)
            for (int dx = 0; dx < 4; dx++)
            {
                if (shape[dy, dx] == 0) continue;
                int bx = px + dx * cell;
                int by = py + dy * cell;
                if (by < 0) continue; // 顶部未进入棋盘的部分不画
                DrawBlock(context, bx, by, cell, hex);
            }
        }

        // 游戏结束遮罩
        if (engine.IsGameOver)
        {
            context.DrawRectangle(Brush("#B0102030"), null, new Rect(0, 0, width, height));
            var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            var title = new FormattedText(
                "GAME OVER", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 32, Brush("#FFFFFF"));
            var sub = new FormattedText(
                "按 R 重新开始", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 16, Brush("#CCFFFFFF"));

            double cx = (width - title.Width) / 2;
            double cy = (height - title.Height) / 2 - 12;
            context.DrawText(title, new Point(cx, cy));
            context.DrawText(sub, new Point((width - sub.Width) / 2, cy + 44));
        }
    }

    private static void DrawBlock(DrawingContext context, double x, double y, int cell, string hex)
    {
        var fill = Brush(hex);
        var border = new Pen(Brush("#1A1A2E"), 2);
        var rect = new Rect(x + 1, y + 1, cell - 2, cell - 2);

        // 立体效果：主体 + 顶部高光
        context.DrawRectangle(fill, null, rect);
        context.DrawRectangle(null, border, rect);
        context.DrawRectangle(
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(90, 255, 255, 255), 0),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.5),
                },
            },
            null,
            new Rect(x + 1, y + 1, cell - 2, (cell - 2) / 2));
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

/// <summary>
/// 方块预览控件：居中显示指定方块类型的 4x4 形状网格。
/// Piece 为 null 时显示空（"--" 提示）。
/// </summary>
public class PreviewControl : Control
{
    public static readonly StyledProperty<TetrominoType?> PieceProperty =
        AvaloniaProperty.Register<PreviewControl, TetrominoType?>(nameof(Piece));

    public TetrominoType? Piece
    {
        get => GetValue(PieceProperty);
        set => SetValue(PieceProperty, value);
    }

    static PreviewControl()
    {
        AffectsRender<PreviewControl>(PieceProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        context.DrawRectangle(Brush("#1A1A2E"), null, new Rect(0, 0, w, h));

        if (Piece is null)
        {
            var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            var text = new FormattedText(
                "--", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 22, Brush("#4A4A6A"));
            context.DrawText(text, new Point((w - text.Width) / 2, (h - text.Height) / 2));
            return;
        }

        var shape = Tetromino.GetShape(Piece.Value, 0);
        double cell = Math.Min(w / 4.0, h / 4.0);
        double ox = (w - cell * 4) / 2;
        double oy = (h - cell * 4) / 2;

        for (int dy = 0; dy < 4; dy++)
        for (int dx = 0; dx < 4; dx++)
        {
            if (shape[dy, dx] == 0) continue;
            var fill = Brush(Tetromino.ColorHex(Piece.Value));
            var rect = new Rect(ox + dx * cell + 1, oy + dy * cell + 1, cell - 2, cell - 2);
            context.DrawRectangle(fill, new Pen(Brush("#1A1A2E"), 1.5), rect);
        }
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}
