namespace Tetris;

/// <summary>方块类型。</summary>
public enum TetrominoType
{
    I, O, T, S, Z, J, L
}

/// <summary>
/// 七种标准俄罗斯方块（Tetromino）的定义。
/// 每个形状用 4x4 网格表示，1 表示有方块。
/// 旋转基于 4x4 矩阵的顺时针 90° 变换，O 方块不旋转。
/// </summary>
public static class Tetromino
{
    /// <summary>每个方块的旋转状态数量。</summary>
    public static int RotationCount(TetrominoType type) => type == TetrominoType.O ? 1 : 4;

    /// <summary>获取指定类型、指定旋转状态的形状矩阵（4x4，row-major）。</summary>
    public static int[,] GetShape(TetrominoType type, int rotation)
    {
        int[,] baseShape = type switch
        {
            TetrominoType.I => new int[,]
            {
                { 0, 0, 0, 0 },
                { 1, 1, 1, 1 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            TetrominoType.O => new int[,]
            {
                { 1, 1, 0, 0 },
                { 1, 1, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            TetrominoType.T => new int[,]
            {
                { 0, 1, 0, 0 },
                { 1, 1, 1, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            TetrominoType.S => new int[,]
            {
                { 0, 1, 1, 0 },
                { 1, 1, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            TetrominoType.Z => new int[,]
            {
                { 1, 1, 0, 0 },
                { 0, 1, 1, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            TetrominoType.J => new int[,]
            {
                { 1, 0, 0, 0 },
                { 1, 1, 1, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            TetrominoType.L => new int[,]
            {
                { 0, 0, 1, 0 },
                { 1, 1, 1, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        // 顺时针旋转 rotation 次（O 方块只有 1 个状态，直接返回）
        var result = (int[,])baseShape.Clone();
        for (int r = 0; r < rotation % 4; r++)
            result = RotateClockwise(result);
        return result;
    }

    /// <summary>4x4 矩阵顺时针旋转 90°。</summary>
    private static int[,] RotateClockwise(int[,] m)
    {
        var result = new int[4, 4];
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
            result[x, 3 - y] = m[y, x];
        return result;
    }

    /// <summary>方块对应的颜色（Avalonia 颜色字符串）。</summary>
    public static string ColorHex(TetrominoType type) => type switch
    {
        TetrominoType.I => "#00E5FF", // 青
        TetrominoType.O => "#FFD600", // 黄
        TetrominoType.T => "#AA47F7", // 紫
        TetrominoType.S => "#69F0AE", // 绿
        TetrominoType.Z => "#FF5252", // 红
        TetrominoType.J => "#448AFF", // 蓝
        TetrominoType.L => "#FF9100", // 橙
        _ => "#FFFFFF",
    };
}
