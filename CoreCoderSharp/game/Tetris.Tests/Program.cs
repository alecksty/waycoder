using Tetris;

int passed = 0, failed = 0;

void Check(string name, bool cond)
{
    if (cond) { passed++; Console.WriteLine($"  ✅ {name}"); }
    else { failed++; Console.WriteLine($"  ❌ {name}"); }
}

Console.WriteLine("== 方块形状 ==");
// 每种形状非空格数 = 4
foreach (TetrominoType t in Enum.GetValues<TetrominoType>())
{
    var shape = Tetromino.GetShape(t, 0);
    int cells = 0;
    for (int y = 0; y < 4; y++)
    for (int x = 0; x < 4; x++)
        if (shape[y, x] == 1) cells++;
    Check($"形状 {t} 有 4 格 (实际 {cells})", cells == 4);
}

// I 旋转 90° 后应竖起来
var i0 = Tetromino.GetShape(TetrominoType.I, 0);
var i1 = Tetromino.GetShape(TetrominoType.I, 1);
Check("I 初始横向", i0[1, 0] == 1 && i0[1, 3] == 1);
Check("I 旋转后纵向", i1[0, 2] == 1 && i1[3, 2] == 1);

// 旋转 4 次回到原状
var i4 = Tetromino.GetShape(TetrominoType.I, 4);
bool same = true;
for (int y = 0; y < 4; y++)
for (int x = 0; x < 4; x++)
    if (i4[y, x] != i0[y, x]) same = false;
Check("I 旋转 4 次复原", same);

Console.WriteLine("\n== 游戏引擎 ==");
var g = new GameEngine();
Check("初始棋盘 20x10 全空", Enumerable.Range(0, 20).All(y => Enumerable.Range(0, 10).All(x => g.GetCell(y, x) is null)));
Check("初始得分 0", g.Score == 0);
Check("初始等级 1", g.Level == 1);
Check("初始未结束", !g.IsGameOver);

// 左右移动
int x0 = g.CurrentX;
Check("左移成功", g.MoveLeft() && g.CurrentX == x0 - 1);
g.MoveRight();
Check("右移回到原位", g.CurrentX == x0);

Console.WriteLine("\n== 硬降与锁定 ==");
g.Reset();
// 连续硬降直到游戏结束，验证不会越界/崩溃，且最终 IsGameOver
int guard = 0;
while (!g.IsGameOver && guard++ < 2000)
    g.HardDrop();
Check($"循环内硬降后正常结束 (步数 {guard})", g.IsGameOver);

Console.WriteLine("\n== 压力测试：200 局随机玩法 ==");
int totalCleared = 0, totalScore = 0, minScore = int.MaxValue, maxScore = 0;
bool illegalState = false;
for (int game = 0; game < 200; game++)
{
    var eg = new GameEngine();
    int steps = 0;
    while (!eg.IsGameOver && steps++ < 500)
    {
        // 随机 0~4 次操作
        for (int i = 0; i < 4; i++)
        {
            switch (Random.Shared.Next(4))
            {
                case 0: eg.MoveLeft(); break;
                case 1: eg.MoveRight(); break;
                case 2: eg.Rotate(); break;
                case 3: eg.SoftDrop(); break;
            }
        }
        eg.HardDrop();
        // 状态合法性：当前方块位置必须在棋盘列范围内
        if (eg.CurrentX < -4 || eg.CurrentX > 9 || eg.CurrentY > 19)
            illegalState = true;
    }
    totalCleared += eg.LinesCleared;
    totalScore += eg.Score;
    minScore = Math.Min(minScore, eg.Score);
    maxScore = Math.Max(maxScore, eg.Score);
    Check($"第 {game + 1} 局: 得分 {eg.Score}, 消行 {eg.LinesCleared}, 等级 {eg.Level}", !illegalState && eg.IsGameOver);
}
Check($"200 局随机玩法全部正常结束且状态合法 (共 {totalCleared} 消行)", !illegalState);

Console.WriteLine("\n== 启发式 AI 玩法（验证消行能力）==");
// 简单启发式：对每个方块尝试所有旋转与列位置，选落点最低（y 最大）的放置
int aiCleared = 0, aiScore = 0, aiGames = 0;
for (int game = 0; game < 50; game++)
{
    var eg = new GameEngine();
    int steps = 0;
    while (!eg.IsGameOver && steps++ < 500)
    {
        int bestX = eg.CurrentX, bestRot = eg.CurrentRotation, bestY = int.MinValue;
        for (int rot = 0; rot < Tetromino.RotationCount(eg.CurrentType); rot++)
        {
            for (int x = -4; x <= 9; x++)
            {
                // 模拟：把方块放到该列能到达的最低位置
                int y = 0;
                var shape = Tetromino.GetShape(eg.CurrentType, rot);
                // 从下往上找第一个能放的位置
                for (int yy = -4; yy <= 19; yy++)
                {
                    if (CanPlaceForTest(eg, shape, x, yy))
                        y = yy;
                }
                if (y > bestY) { bestY = y; bestX = x; bestRot = rot; }
            }
        }
        // 移动到目标列并旋转（引擎内部会做碰撞检查）
        while (eg.CurrentRotation != bestRot && eg.Rotate()) { }
        while (eg.CurrentX < bestX && eg.MoveRight()) { }
        while (eg.CurrentX > bestX && eg.MoveLeft()) { }
        eg.HardDrop();
    }
    aiCleared += eg.LinesCleared;
    aiScore += eg.Score;
    aiGames++;
    Check($"AI 第 {game + 1} 局: 得分 {eg.Score}, 消行 {eg.LinesCleared}", eg.IsGameOver);
}
Check($"AI 50 局共消行 {aiCleared} 行", aiCleared > 0);
Check($"AI 50 局共得分 {aiScore} (平均 {aiScore / (double)aiGames:F1})", aiScore > 0);

static bool CanPlaceForTest(GameEngine e, int[,] shape, int x, int y)
{
    for (int dy = 0; dy < 4; dy++)
    for (int dx = 0; dx < 4; dx++)
    {
        if (shape[dy, dx] == 0) continue;
        int bx = x + dx, by = y + dy;
        if (bx < 0 || bx >= 10 || by >= 20) return false;
        if (by >= 0 && e.GetCell(by, bx) is not null) return false;
    }
    return true;
}

Console.WriteLine("\n== 旋转 ==");
g.Reset();
var before = g.CurrentRotation;
bool rotated = g.Rotate();
Check("旋转操作不抛异常", true);
Check("旋转后状态合法", g.CurrentRotation is >= 0 and < 4);

Console.WriteLine("\n== 暂停 ==");
g.Reset();
g.IsPaused = true;
int cx = g.CurrentX;
Check("暂停时不能移动", !g.MoveLeft() && g.CurrentX == cx);

Console.WriteLine("\n== Hold 暂存 ==");
g.Reset();
var firstType = g.CurrentType;
Check("初始可暂存", g.CanHold && g.HeldType is null);
Check("暂存成功", g.Hold());
Check("暂存后 HeldType 为原方块", g.HeldType == firstType);
Check("暂存后当前方块变为新方块", g.CurrentType != firstType);
Check("暂存后不可再次暂存", !g.CanHold && !g.Hold());
// 锁定后恢复暂存能力
g.HardDrop();
Check("锁定后恢复可暂存", g.CanHold);
// 锁定后再暂存 = 与 Hold 交换（取回之前暂存的方块）
g.Hold();
Check("交换后回到原方块", g.CurrentType == firstType);
Check("交换后不可连续暂存", !g.CanHold);
// 游戏结束后不能暂存
g.Reset();
while (!g.IsGameOver && guard < 2000) { g.HardDrop(); guard++; }
Check("游戏结束后不能暂存", !g.Hold());

Console.WriteLine("\n== 预览队列 ×3 ==");
g.Reset();
bool validQueue = true;
for (int i = 0; i < 3; i++)
    validQueue &= Enum.IsDefined(g.GetNextType(i));
Check("预览队列长度 3 且类型合法", validQueue);
TetrominoType n0 = g.GetNextType(0), n1 = g.GetNextType(1), n2 = g.GetNextType(2);
Check("预览队列各元素可访问", n0 != default && n1 != default && n2 != default);
g.HardDrop();
Check("落子后队列前移", g.GetNextType(0) == n1 && g.GetNextType(1) == n2);

Console.WriteLine("\n== Ghost 投影 ==");
g.Reset();
int ghostY = g.GhostY;
Check("Ghost 不低于当前方块", ghostY >= g.CurrentY);
Check("Ghost 在棋盘内", ghostY >= 0 && ghostY <= 19);
// 硬降后当前方块应停在 Ghost 位置
while (g.SoftDrop()) { }
Check("软降到底等于 Ghost 位置", g.CurrentY == ghostY);

Console.WriteLine("\n== 音效 WAV 生成 ==");
SoundManager.Init();
// 无法验证播放，仅确认不抛异常
Check("音效初始化无异常", true);

Console.WriteLine("\n== 最高分持久化 ==");
var saved = HighScoreManager.SaveIfHigher(99999);
var loaded = HighScoreManager.Load();
Check($"保存 99999 成功 (本次 {saved})", saved);
Check($"读取最高分 = 99999 (实际 {loaded})", loaded == 99999);
Check("较小分数不覆盖", !HighScoreManager.SaveIfHigher(100) && HighScoreManager.Load() == 99999);

Console.WriteLine($"\n结果: {passed} 通过, {failed} 失败");
return failed == 0 ? 0 : 1;
