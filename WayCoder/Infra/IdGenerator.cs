using System.Security.Cryptography;

namespace WayCoder;

/// <summary>
/// 短 ID 生成器 —— 安全随机数 + 去歧义字符集。
/// 用于生成工具调用 ID、任务 ID、子智能体标识等。
/// </summary>
public static class IdGenerator
{
    /// <summary>去歧义字符集：小写字母+数字，排除 0/o/1/l/i（易混淆）</summary>
    private const string SafeChars = "abcdefghjkmnpqrstuvwxyz23456789";

    /// <summary>安全随机数生成器</summary>
    private static readonly ThreadLocal<RandomNumberGenerator> _rng =
        new(() => RandomNumberGenerator.Create());

    // ── 词表（用于可读 slug）──
    private static readonly string[] Adjectives =
    [
        "blue", "red", "gold", "silver", "swift", "brave", "calm", "eager",
        "fair", "grand", "happy", "keen", "lucky", "proud", "quiet", "rapid",
        "sharp", "tiny", "vivid", "warm", "zesty", "bold", "cool", "dark",
        "fresh", "green", "hot", "icy", "light", "neon",
    ];

    private static readonly string[] Animals =
    [
        "tiger", "eagle", "shark", "panda", "hawk", "fox", "wolf", "bear",
        "deer", "dove", "frog", "goat", "hare", "jay", "koi", "lynx",
        "mink", "newt", "owl", "ray", "seal", "swan", "toad", "wren",
        "crane", "dolphin", "falcon", "heron", "ibis", "skua",
    ];

    private static readonly string[] Nouns =
    [
        "moon", "star", "lake", "hill", "wood", "stone", "river", "cloud",
        "storm", "field", "flame", "wave", "cave", "peak", "reef", "snow",
        "wind", "dawn", "dusk", "mist", "rain", "sand", "pine", "oak",
        "elm", "fern", "moss", "vine", "ash", "bay",
    ];

    /// <summary>
    /// 生成指定长度的安全随机短 ID。
    /// 字符集：a-z(去歧义) + 2-9，共 30 个字符。
    /// </summary>
    /// <param name="length">ID 长度（默认 8）</param>
    public static string NewId(int length = 8)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "长度必须大于 0");
        var bytes = new byte[length];
        _rng.Value!.GetBytes(bytes);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = SafeChars[bytes[i] % SafeChars.Length];
        return new string(chars);
    }

    /// <summary>
    /// 生成可读的短 slug（如 "blue-tiger-moon"）。
    /// 词数默认 3，格式：形容词-动物-名词。
    /// </summary>
    /// <param name="words">单词数（1-5）</param>
    public static string NewSlug(int words = 3)
    {
        words = Math.Clamp(words, 1, 5);
        var parts = new string[words];
        var bytes = new byte[words * 4];
        _rng.Value!.GetBytes(bytes);

        for (int i = 0; i < words; i++)
        {
            var idx = BitConverter.ToInt32(bytes, i * 4) & int.MaxValue; // 非负
            parts[i] = (i % 3) switch
            {
                0 => Adjectives[idx % Adjectives.Length],
                1 => Animals[idx % Animals.Length],
                _ => Nouns[idx % Nouns.Length],
            };
        }
        return string.Join("-", parts);
    }

    /// <summary>生成带前缀的 ID（如 "wf_a3kf7x2m"）。</summary>
    /// <param name="prefix">前缀（不含下划线）</param>
    /// <param name="length">随机部分长度（默认 6）</param>
    public static string NewPrefixed(string prefix, int length = 6)
        => $"{prefix}_{NewId(length)}";
}
