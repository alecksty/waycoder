using System;
using System.IO;
using System.Text.Json;

namespace Tetris;

/// <summary>
/// 最高分持久化：保存到用户本地数据目录 Tetris/highscore.json。
/// </summary>
public static class HighScoreManager
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tetris");

    private static readonly string File = Path.Combine(Dir, "highscore.json");

    /// <summary>读取最高分（无记录时返回 0）。</summary>
    public static int Load()
    {
        try
        {
            if (!System.IO.File.Exists(File)) return 0;
            var json = System.IO.File.ReadAllText(File);
            var data = JsonSerializer.Deserialize<HighScoreData>(json);
            return data?.Score ?? 0;
        }
        catch
        {
            return 0; // 损坏或不可读时静默降级
        }
    }

    /// <summary>若 score 超过现有最高分则保存，返回是否刷新纪录。</summary>
    public static bool SaveIfHigher(int score)
    {
        try
        {
            int current = Load();
            if (score <= current) return false;
            Directory.CreateDirectory(Dir);
            var data = new HighScoreData { Score = score, Date = DateTime.Now };
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(data));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class HighScoreData
    {
        public int Score { get; set; }
        public DateTime Date { get; set; }
    }
}
