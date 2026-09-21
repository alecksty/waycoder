using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder;

/// <summary>临时探针（定位 conio 画面右边框丢失），用完即删。</summary>
public static partial class SelfTest
{
    private static void TestZzTmp(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[ZZTMP] 全屏画面过共用层");

        string? path = null;
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null && path == null; d = d.Parent)
        {
            var p = Path.Combine(d.FullName, ".scratch", "cs.out");
            if (File.Exists(p)) path = p;
        }
        if (path == null) { Console.WriteLine("      （找不到 .scratch/cs.out）"); return; }

        var raw = File.ReadAllText(path);
        Console.WriteLine($"      raw 长度={raw.Length} 竖线={raw.Count(c => c == '|')}");

        var fb = new FrameBuffer(25, 80);
        fb.Apply(raw);
        var dumpAnsi = fb.DumpAnsi();
        Console.WriteLine($"      DumpAnsi 竖线={string.Concat(dumpAnsi).Count(c => c == '|')}");

        var dump = fb.Dump();
        for (int i = 0; i < dump.Count; i++)
            if (dump[i].Contains('|'))
                Console.WriteLine($"      dump[{i}] len={dump[i].Length} | {dump[i]}");

        var markup = AnsiMarkup.ToMarkup(string.Join("\n", dumpAnsi)).TrimEnd();
        Console.WriteLine($"      markup 竖线={markup.Count(c => c == '|')}");

        // 走命令行页那条路：只解标记、不解析 Markdown
        var segs = MarkdownParser.ParseMarkupOnly(markup);
        var joined = string.Concat(segs.Select(s => s.Text));
        Console.WriteLine($"      ParseMarkupOnly 段数={segs.Count} 竖线={joined.Count(c => c == '|')}");

        // 再走一次「Markdown 块级」那条（旧路）做对照
        var nodes = MarkdownParser.Parse(markup);
        var viaMd = string.Concat(nodes.OfType<MdParagraph>().Select(p => p.Text));
        Console.WriteLine($"      Markdown 块级 竖线={viaMd.Count(c => c == '|')} 块数={nodes.Count}");

        Check("画面里的竖线在共用层没被吃掉", joined.Count(c => c == '|') == 12);
    }
}
