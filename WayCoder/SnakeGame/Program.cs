using System.Diagnostics;

namespace SnakeGame;

internal static class Program
{
    private const int Width = 40;   // 游戏区域宽
    private const int Height = 20;  // 游戏区域高

    private static readonly List<(int X, int Y)> Snake = new();
    private static (int X, int Y) Food;
    private static (int X, int Y) Dir = (1, 0);   // 当前方向
    private static (int X, int Y) _nextDir = (1, 0);
    private static int _score;
    private static bool _gameOver;
    private static readonly Random Rand = new();

    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Clear();
        StartGame();

        using var timer = new System.Threading.Timer(_ => Tick(), null, 0, 120);
        while (!_gameOver)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                _nextDir = key switch
                {
                    ConsoleKey.UpArrow or ConsoleKey.W => (0, -1),
                    ConsoleKey.DownArrow or ConsoleKey.S => (0, 1),
                    ConsoleKey.LeftArrow or ConsoleKey.A => (-1, 0),
                    ConsoleKey.RightArrow or ConsoleKey.D => (1, 0),
                    _ => _nextDir,
                };
            }
        }
        timer.Dispose();

        Console.SetCursorPosition(0, Height + 3);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  Game Over! 得分: " + _score);
        Console.ResetColor();
        Console.WriteLine("  按任意键退出...");
        Console.ReadKey(true);
        Console.CursorVisible = true;
    }

    private static void StartGame()
    {
        _gameOver = false;
        _score = 0;
        Dir = (1, 0);
        _nextDir = (1, 0);
        Snake.Clear();
        Snake.Add((Width / 2, Height / 2));
        Snake.Add((Width / 2 - 1, Height / 2));
        Snake.Add((Width / 2 - 2, Height / 2));
        SpawnFood();
        DrawBorders();
        DrawScore();
    }

    private static void SpawnFood()
    {
        while (true)
        {
            var pos = (Rand.Next(1, Width - 1), Rand.Next(1, Height - 1));
            if (!Snake.Contains(pos))
            {
                Food = pos;
                return;
            }
        }
    }

    private static void DrawBorders()
    {
        Console.Clear();
        for (var x = 0; x <= Width; x++)
        {
            SetChar(x, 0, '#'); SetChar(x, Height, '#');
        }
        for (var y = 0; y <= Height; y++)
        {
            SetChar(0, y, '#'); SetChar(Width, y, '#');
        }
    }

    private static void DrawScore()
    {
        Console.SetCursorPosition(0, Height + 1);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  得分: " + _score + "    方向: 方向键/WASD");
        Console.ResetColor();
    }

    private static void SetChar(int x, int y, char c)
    {
        Console.SetCursorPosition(x, y);
        Console.Write(c);
    }

    private static void Tick()
    {
        if (_gameOver) return;

        // 防止 180° 反向
        if (_nextDir != (-Dir.X, -Dir.Y) && _nextDir != (0, 0))
            Dir = _nextDir;

        var head = (Snake[0].X + Dir.X, Snake[0].Y + Dir.Y);

        // 撞墙或撞自己
        if (head.X <= 0 || head.X >= Width || head.Y <= 0 || head.Y >= Height ||
            Snake.Contains(head))
        {
            _gameOver = true;
            return;
        }

        Snake.Insert(0, head);
        SetChar(head.X, head.Y, 'O');

        if (head == Food)
        {
            _score += 10;
            DrawScore();
            SpawnFood();
            SetChar(Food.X, Food.Y, '@');
        }
        else
        {
            var tail = Snake[^1];
            Snake.RemoveAt(Snake.Count - 1);
            SetChar(tail.X, tail.Y, ' ');
        }
    }
}
