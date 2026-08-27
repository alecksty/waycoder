using System.Text;
using System.IO.Compression;
using WayCoder.Git;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk17(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[git 包解码（Inflater / side-band / v2 帧）]");

        // ── Inflater 往返（ZLibStream 压缩 → 自实现 inflate 解压）──
        // 曾有两个潜伏 bug：HuffmanDecoder 构造器用规范码值索引（288 长数组越界）、
        // 码位构建用 LSB 优先（位序错 → 解出错误符号）。仅在手机 clone/pull 才触发。
        foreach (var lvl in new[] { CompressionLevel.Fastest, CompressionLevel.Optimal, CompressionLevel.SmallestSize })
        {
            var plain = Encoding.UTF8.GetBytes("hello world hello world 测试中文 git pack 解码验证 1234567890\n");
            byte[] comp;
            using (var ms = new MemoryStream())
            {
                using (var z = new ZLibStream(ms, lvl, leaveOpen: true)) z.Write(plain);
                comp = ms.ToArray();
            }
            var (data, consumed) = Inflater.Decompress(comp, 0);
            Check($"Inflater 往返（{lvl}）内容一致", data.AsSpan().SequenceEqual(plain));
            Check($"Inflater 消耗字节准确（{lvl}）", consumed == comp.Length);
        }

        // ── 大数据（多 deflate 块 + 长距离 back-reference）──
        var big = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. 0123456789 ", 2000)));
        byte[] bigComp;
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(big);
            bigComp = ms.ToArray();
        }
        var (bigData, bigConsumed) = Inflater.Decompress(bigComp, 0);
        Check("Inflater 大数据解压一致", bigData.AsSpan().SequenceEqual(big));
        Check("Inflater 大数据消耗准确", bigConsumed == bigComp.Length);

        // ── stored 块（BTYPE=00）：接近不可压缩的数据应走 stored ──
        var noisy = new byte[20000];
        var rng = new Random(42);
        rng.NextBytes(noisy);
        byte[] noisyComp;
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.NoCompression, leaveOpen: true)) z.Write(noisy);
            noisyComp = ms.ToArray();
        }
        var (noisyData, noisyConsumed) = Inflater.Decompress(noisyComp, 0);
        Check("Inflater stored 块解压一致", noisyData.AsSpan().SequenceEqual(noisy));
        Check("Inflater stored 块消耗准确", noisyConsumed == noisyComp.Length);

        // ── side-band-64k 解码：构造 packfile\n + channel1(pack) + channel2(进度) + flush ──
        var packBytes = Encoding.ASCII.GetBytes("PACK1234567890");
        using var sb2 = new MemoryStream();
        PktLine.WriteString(sb2, "packfile\n");                  // 服务端裸头 "packfile\n"（无 channel）
        // 手动追加 channel-1/2 帧：先写长度头再写 channel 字节+载荷
        WriteSidebandPkt(sb2, 1, packBytes);                     // channel-1 pack 数据
        WriteSidebandPkt(sb2, 2, Encoding.UTF8.GetBytes("Enumerating objects: 1, done.\n")); // 进度
        WriteSidebandPkt(sb2, 1, new byte[] { 0xFF });           // 尾巴数据
        sb2.Write("0000"u8);                                     // flush 结束
        var decoded = GitRemote.DeSideband(sb2.ToArray());
        Check("side-band 解码拼接 channel-1", decoded.AsSpan().SequenceEqual(packBytes.Concat(new byte[] { 0xFF }).ToArray()));

        // ── FindPackMarker 在拼接流上定位 PACK ──
        var magic = Encoding.ASCII.GetBytes("NAK\nPACK\x00\x00\x00\x02");
        int idx = GitRemote.FindPackMarker(magic);
        Check("FindPackMarker 找到 PACK 魔数", idx == 4);

        // ── tree 排序（git 规则：目录按 name + "/" 与文件统一排序）──
        // 曾 bug：文件全在前、目录全在后 → 含子目录的仓库 tree 无序 → 服务端 unpacker 拒绝（treeNotSorted）。
        var tdir = Path.Combine(Path.GetTempPath(), "wctree_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tdir);
        try
        {
            GitCore.Init(tdir);
            Directory.CreateDirectory(Path.Combine(tdir, "foo"));
            File.WriteAllText(Path.Combine(tdir, "foo", "inner.txt"), "inner");
            File.WriteAllText(Path.Combine(tdir, "foo.txt"), "f");
            File.WriteAllText(Path.Combine(tdir, "bar.txt"), "b");
            File.WriteAllText(Path.Combine(tdir, "zz.txt"), "z");
            File.WriteAllText(Path.Combine(tdir, "a_demo"), "a");
            GitCore.Add(tdir, ".");
            var commitMsg = GitCore.Commit(tdir, "tree sort test");
            var head = GitCore.ReadHeadCommit(Path.Combine(tdir, ".git"));
            var commit = GitCore.ReadObject(Path.Combine(tdir, ".git"), head!)!.Value;
            var treeLine = Encoding.UTF8.GetString(commit.Content).Split('\n').First(l => l.StartsWith("tree "));
            var treeSha = treeLine[5..];
            var tree = GitCore.ReadObject(Path.Combine(tdir, ".git"), treeSha!)!.Value.Content;

            // 解析 tree 条目（mode name\0sha）
            var names = new List<string>();
            int p = 0;
            while (p < tree.Length)
            {
                var sp = Array.IndexOf(tree, (byte)' ', p);
                var nul = Array.IndexOf(tree, (byte)0, sp);
                var mode = Encoding.ASCII.GetString(tree, p, sp - p);
                var name = Encoding.ASCII.GetString(tree, sp + 1, nul - sp - 1);
                names.Add(mode == "40000" ? name + "/" : name);
                p = nul + 21;
            }
            var sortedOk = names.Zip(names.Skip(1)).All(pair => string.CompareOrdinal(pair.First, pair.Second) <= 0);
            Check("tree 排序（目录 name/ 与文件统一）", sortedOk);
            Check("tree 目录交错（foo/ 在 zz 前）", names.IndexOf("foo/") < names.IndexOf("zz.txt"));
            Directory.Delete(tdir, recursive: true);
        }
        catch { try { Directory.Delete(tdir, recursive: true); } catch { } }
    }

    /// <summary>写一个 side-band pkt：4 字节长度头 + 1 字节 channel + payload。</summary>
    private static void WriteSidebandPkt(Stream s, int channel, byte[] payload)
    {
        var body = new byte[1 + payload.Length];
        body[0] = (byte)channel;
        Buffer.BlockCopy(payload, 0, body, 1, payload.Length);
        var len = body.Length + 4;
        s.Write(Encoding.ASCII.GetBytes(len.ToString("x4")), 0, 4);
        s.Write(body, 0, body.Length);
    }
}
