using System.Globalization;
using System.Text;

// ============================================================
//  彩票号码计算器
//  原理：以日期为随机种子 → 同一天算出的号码永远相同。
//  "计算"彩票：确定性的、可复现的 —— 但中奖与否仍是随机的 😉
//  用法：
//    dotnet run                          # 今天 双色球
//    dotnet run -- --type dlt            # 今天 大乐透
//    dotnet run -- --date 2026-08-08     # 指定日期
//    dotnet run -- --count 7             # 未来 7 天
//    dotnet run -- --type both --count 3 # 两种都算，未来 3 天
// ============================================================

Console.OutputEncoding = Encoding.UTF8;

// ---- 解析参数 ----
DateTime start = DateTime.Today;
int days = 1;
var type = "ssq"; // ssq | dlt | both

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--date" when i + 1 < args.Length:
            start = DateTime.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--count" when i + 1 < args.Length:
            days = Math.Clamp(int.Parse(args[++i]), 1, 31);
            break;
        case "--type" when i + 1 < args.Length:
            type = args[++i].ToLowerInvariant();
            break;
        case "-h" or "--help":
            PrintHelp();
            return;
    }
}

if (type is not ("ssq" or "dlt" or "both"))
{
    Console.Error.WriteLine($"未知彩种: {type}（支持 ssq / dlt / both）");
    return;
}

// ---- 标题 ----
Console.WriteLine();
Console.WriteLine("  ┌────────────────────────────────────────────┐");
Console.WriteLine("  │      🎰 彩票号码计算器（日期种子版）        │");
Console.WriteLine("  └────────────────────────────────────────────┘");
Console.WriteLine($"  📅 起始日期: {start:yyyy-MM-dd}   天数: {days}   彩种: {TypeName(type)}");
Console.WriteLine("  🔑 种子 = 日期 → 同一天算出的号码永远相同");
Console.WriteLine();

// ---- 逐天计算 ----
for (int d = 0; d < days; d++)
{
    var date = start.AddDays(d);
    Console.WriteLine($"  ─── {date:yyyy-MM-dd} ({WeekName(date.DayOfWeek)}) ───");
    if (type is "ssq" or "both")
    {
        var (reds, blue) = DrawSsq(date);
        Console.Write("    双色球  ");
        foreach (var r in reds) Console.Write(R("● " + r.ToString("D2")) + " ");
        Console.WriteLine("  +  " + B("● " + blue.ToString("D2")));
    }
    if (type is "dlt" or "both")
    {
        var (fronts, backs) = DrawDlt(date);
        Console.Write("    大乐透  ");
        foreach (var f in fronts) Console.Write(R("● " + f.ToString("D2")) + " ");
        Console.WriteLine("  +  " + string.Join(" ", backs.Select(x => B("● " + x.ToString("D2")))));
    }
    Console.WriteLine();
}

// ---- 幽默免责声明 ----
Console.WriteLine("  ⚠️ 郑重声明：");
Console.WriteLine("    本程序只能保证『同一天算出来的号码一样』，");
Console.WriteLine("    不能保证『中奖』。");
Console.WriteLine($"    双色球头奖概率 1/17,721,088 —— 比你代码一次编译通过的概率还低。");
Console.WriteLine("    明天开奖号码与我无关，祝你好运 🍀");
Console.WriteLine();

// ================= 工具函数 =================

string TypeName(string t) => t switch
{
    "ssq" => "双色球",
    "dlt" => "大乐透",
    _ => "双色球 + 大乐透",
};

string WeekName(DayOfWeek w) => w switch
{
    DayOfWeek.Monday => "周一", DayOfWeek.Tuesday => "周二", DayOfWeek.Wednesday => "周三",
    DayOfWeek.Thursday => "周四", DayOfWeek.Friday => "周五", DayOfWeek.Saturday => "周六",
    _ => "周日",
};

// 双色球：6 红 (1-33) + 1 蓝 (1-16)
(int[] reds, int blue) DrawSsq(DateTime date)
{
    var rng = new Random(Seed(date));
    var reds = Pick(rng, 6, 33);
    var blue = rng.Next(1, 17);
    return (reds, blue);
}

// 大乐透：5 前区 (1-35) + 2 后区 (1-12)
(int[] fronts, int[] backs) DrawDlt(DateTime date)
{
    var rng = new Random(Seed(date) ^ 0x5A5A5A5A);
    var fronts = Pick(rng, 5, 35);
    var backs = Pick(rng, 2, 12);
    return (fronts, backs);
}

// 种子 = 日期整数值（2026-08-08 → 20260808），保证同一天结果确定
int Seed(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

// 从 1..max 中取 count 个不重复的数（Fisher-Yates 部分洗牌），升序返回
int[] Pick(Random rng, int count, int max)
{
    var pool = Enumerable.Range(1, max).ToArray();
    for (int i = 0; i < count; i++)
    {
        int j = rng.Next(i, max);
        (pool[i], pool[j]) = (pool[j], pool[i]);
    }
    return pool.Take(count).OrderBy(x => x).ToArray();
}

string R(string s) => "\x1b[91;1m" + s + "\x1b[0m"; // 红球：亮红
string B(string s) => "\x1b[94;1m" + s + "\x1b[0m"; // 蓝球：亮蓝

void PrintHelp()
{
    Console.WriteLine("""
        彩票号码计算器 —— 以日期为种子的确定性抽号器

        用法:
          dotnet run                         今天 · 双色球
          dotnet run -- --type dlt           今天 · 大乐透
          dotnet run -- --type both          今天 · 两种都算
          dotnet run -- --date 2026-08-08    指定日期
          dotnet run -- --count 7            未来 7 天
          dotnet run -- --count 7 --type both  未来 7 天两种都算

        参数:
          --date <YYYY-MM-DD>   起始日期（默认今天）
          --count <N>           连续 N 天（1-31，默认 1）
          --type <ssq|dlt|both> 彩种（默认 ssq）
          -h, --help            显示帮助

        原理: 种子 = 日期 → 同一天号码永远相同，可复现、可"计算"。
        """);
}
