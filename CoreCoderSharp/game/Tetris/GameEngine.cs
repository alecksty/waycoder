using System;
using System.Collections.Generic;

namespace Tetris;

/// <summary>
/// 俄罗斯方块核心游戏引擎：棋盘、当前方块、碰撞检测、
/// 旋转（含墙踢 Wall Kick）、消行、计分、等级、Hold 暂存、
/// 预览队列与 Ghost 投影。纯逻辑，不依赖 UI，便于测试。
/// </summary>
public class GameEngine
{
    public const int Cols = 10;
    public const int Rows = 20;

    /// <summary>预览队列长度（显示接下来几个方块）。</summary>
    public const int PreviewCount = 3;

    /// <summary>棋盘格子：null 为空，否则为方块类型（用于取色）。</summary>
    private readonly TetrominoType?[,] _board = new TetrominoType?[Rows, Cols];

    /// <summary>当前活动方块类型。</summary>
    public TetrominoType CurrentType { get; private set; }

    /// <summary>当前活动方块旋转状态。</summary>
    public int CurrentRotation { get; private set; }

    /// <summary>当前活动方块左上角在棋盘中的行（y）。</summary>
    public int CurrentY { get; private set; }

    /// <summary>当前活动方块左上角在棋盘中的列（x）。</summary>
    public int CurrentX { get; private set; }

    /// <summary>暂存中的方块（null = 尚未暂存过）。</summary>
    public TetrominoType? HeldType { get; private set; }

    /// <summary>当前方块是否允许暂存（每个方块只能暂存一次）。</summary>
    public bool CanHold { get; private set; }

    /// <summary>得分。</summary>
    public int Score { get; private set; }

    /// <summary>已消除的总行数。</summary>
    public int LinesCleared { get; private set; }

    /// <summary>等级（每消 10 行升一级，速度加快）。</summary>
    public int Level => LinesCleared / 10 + 1;

    /// <summary>当前下落间隔（毫秒）。</summary>
    public int DropIntervalMs => Math.Max(80, 800 - (Level - 1) * 70);

    /// <summary>游戏是否结束。</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>游戏是否暂停。</summary>
    public bool IsPaused { get; set; }

    /// <summary>刚消除的行数（用于得分提示，每步重置）。</summary>
    public int LastClearCount { get; private set; }

    private readonly Random _random = new();

    /// <summary>7-bag 随机序列（保证每个形状在每 7 个中恰好出现一次）。</summary>
    private readonly List<TetrominoType> _bag = new();

    /// <summary>预览队列（_queue[0] 是下一个方块）。</summary>
    private readonly List<TetrominoType> _queue = new();

    public GameEngine()
    {
        Reset();
    }

    /// <summary>重置游戏。</summary>
    public void Reset()
    {
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Cols; x++)
            _board[y, x] = null;

        Score = 0;
        LinesCleared = 0;
        LastClearCount = 0;
        IsGameOver = false;
        IsPaused = false;
        HeldType = null;
        CanHold = true;
        _bag.Clear();
        _queue.Clear();
        for (int i = 0; i < PreviewCount; i++)
            _queue.Add(PopFromBag());
        SpawnNext();
    }

    /// <summary>读取棋盘某个格子。</summary>
    public TetrominoType? GetCell(int y, int x) => _board[y, x];

    /// <summary>读取当前方块在当前旋转下的 4x4 形状。</summary>
    public int[,] CurrentShape => Tetromino.GetShape(CurrentType, CurrentRotation);

    /// <summary>第 index 个预览方块（0 = 下一个）。</summary>
    public TetrominoType GetNextType(int index) => _queue[index];

    /// <summary>下一个方块形状（用于预览）。</summary>
    public int[,] NextShape => Tetromino.GetShape(GetNextType(0), 0);

    /// <summary>Ghost 投影落点的 y 坐标（当前方块垂直下落能到达的最低位）。</summary>
    public int GhostY
    {
        get
        {
            int y = CurrentY;
            while (CanPlace(CurrentType, CurrentRotation, CurrentX, y + 1)) y++;
            return y;
        }
    }

    /// <summary>从 7-bag 中取一个方块类型。</summary>
    private TetrominoType PopFromBag()
    {
        if (_bag.Count == 0)
        {
            _bag.AddRange(new[]
            {
                TetrominoType.I, TetrominoType.O, TetrominoType.T,
                TetrominoType.S, TetrominoType.Z, TetrominoType.J, TetrominoType.L,
            });
            // Fisher-Yates 洗牌
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
        var t = _bag[^1];
        _bag.RemoveAt(_bag.Count - 1);
        return t;
    }

    /// <summary>从预览队列取出下一个方块作为当前方块，并补充队列尾部。</summary>
    private void SpawnNext()
    {
        CurrentType = _queue[0];
        CurrentRotation = 0;
        CurrentX = Cols / 2 - 2; // 4x4 网格居中
        CurrentY = 0;
        _queue.RemoveAt(0);
        _queue.Add(PopFromBag());

        // 生成即碰撞 → 游戏结束
        if (!CanPlace(CurrentType, CurrentRotation, CurrentX, CurrentY))
            IsGameOver = true;
    }

    /// <summary>检测某个方块在指定位置是否可放置（不越界、不重叠）。</summary>
    private bool CanPlace(TetrominoType type, int rotation, int x, int y)
    {
        var shape = Tetromino.GetShape(type, rotation);
        for (int dy = 0; dy < 4; dy++)
        for (int dx = 0; dx < 4; dx++)
        {
            if (shape[dy, dx] == 0) continue;
            int bx = x + dx;
            int by = y + dy;
            if (bx < 0 || bx >= Cols || by >= Rows) return false;
            if (by >= 0 && _board[by, bx] != null) return false;
        }
        return true;
    }

    /// <summary>尝试向左移动。</summary>
    public bool MoveLeft() => TryMove(-1, 0);

    /// <summary>尝试向右移动。</summary>
    public bool MoveRight() => TryMove(1, 0);

    /// <summary>软降（向下移动一格），成功返回 true。</summary>
    public bool SoftDrop() => TryMove(0, 1);

    /// <summary>
    /// 硬降：方块直接落到底部并锁定。
    /// 返回本次消行数。
    /// </summary>
    public int HardDrop()
    {
        while (TryMove(0, 1)) { }
        return LockPiece();
    }

    /// <summary>
    /// 暂存当前方块（每个方块只能暂存一次，锁定后恢复）。
    /// 第一次暂存：当前方块存入 Hold，从队列取新方块；
    /// 之后：当前方块与 Hold 中的方块交换。
    /// </summary>
    public bool Hold()
    {
        if (IsGameOver || IsPaused || !CanHold) return false;

        if (HeldType is null)
        {
            HeldType = CurrentType;
            CanHold = false;
            SpawnNext();
        }
        else
        {
            var held = HeldType.Value;
            HeldType = CurrentType;
            CanHold = false;
            CurrentType = held;
            CurrentRotation = 0;
            CurrentX = Cols / 2 - 2;
            CurrentY = 0;
            if (!CanPlace(CurrentType, CurrentRotation, CurrentX, CurrentY))
                IsGameOver = true;
        }
        return true;
    }

    /// <summary>尝试移动当前方块。</summary>
    private bool TryMove(int dx, int dy)
    {
        if (IsGameOver || IsPaused) return false;
        if (!CanPlace(CurrentType, CurrentRotation, CurrentX + dx, CurrentY + dy))
            return false;
        CurrentX += dx;
        CurrentY += dy;
        return true;
    }

    /// <summary>尝试顺时针旋转（带简单墙踢：尝试偏移 0、-1、+1、-2、+2）。</summary>
    public bool Rotate()
    {
        if (IsGameOver || IsPaused) return false;
        if (CurrentType == TetrominoType.O) return false;

        int newRot = (CurrentRotation + 1) % 4;
        int[] kicks = { 0, -1, 1, -2, 2 };
        foreach (int kick in kicks)
        {
            if (CanPlace(CurrentType, newRot, CurrentX + kick, CurrentY))
            {
                CurrentRotation = newRot;
                CurrentX += kick;
                return true;
            }
        }
        return false;
    }

    /// <summary>将当前方块锁定到棋盘，然后消除满行并生成新方块。</summary>
    private int LockPiece()
    {
        if (IsGameOver) return 0;

        var shape = Tetromino.GetShape(CurrentType, CurrentRotation);
        for (int dy = 0; dy < 4; dy++)
        for (int dx = 0; dx < 4; dx++)
        {
            if (shape[dy, dx] == 0) continue;
            int by = CurrentY + dy;
            int bx = CurrentX + dx;
            if (by >= 0 && by < Rows && bx >= 0 && bx < Cols)
                _board[by, bx] = CurrentType;
        }

        LastClearCount = ClearLines();

        SpawnNext();
        CanHold = true; // 新方块可再次暂存
        return LastClearCount;
    }

    /// <summary>消除满行并计分，返回消除行数。</summary>
    private int ClearLines()
    {
        int cleared = 0;
        for (int y = Rows - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < Cols; x++)
            {
                if (_board[y, x] == null) { full = false; break; }
            }
            if (!full) continue;

            // 上方的行整体下移
            for (int yy = y; yy > 0; yy--)
            for (int x = 0; x < Cols; x++)
                _board[yy, x] = _board[yy - 1, x];
            for (int x = 0; x < Cols; x++)
                _board[0, x] = null;

            cleared++;
            y++; // 重新检查当前行（因为上面下移了一行）
        }

        if (cleared > 0)
        {
            LinesCleared += cleared;
            // 经典计分：1 行 100，2 行 300，3 行 500，4 行 800，乘以等级
            int[] points = { 0, 100, 300, 500, 800 };
            Score += points[Math.Min(cleared, 4)] * Level;
        }
        return cleared;
    }
}
