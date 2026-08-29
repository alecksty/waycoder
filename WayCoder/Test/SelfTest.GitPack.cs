using System.Text;
using System.IO.Compression;
using WayCoder.Git;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk17(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[Git 包解码（Inflater / side-band / v2 帧）]");

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

        // ── Inflater.Skip：只推进偏移不物化输出，消耗字节须与 Decompress 一致（预扫描定位对象边界用）──
        foreach (var lvl in new[] { CompressionLevel.Fastest, CompressionLevel.Optimal, CompressionLevel.SmallestSize })
        {
            var skipPlain = Encoding.UTF8.GetBytes("hello world hello world 测试中文 git pack 解码验证 1234567890\n");
            byte[] skipComp;
            using (var skipMs = new MemoryStream())
            {
                using (var sz = new ZLibStream(skipMs, lvl, leaveOpen: true)) sz.Write(skipPlain);
                skipComp = skipMs.ToArray();
            }
            var skipConsumed = Inflater.Skip(skipComp, 0);
            var (_, decConsumed) = Inflater.Decompress(skipComp, 0);
            Check($"Inflater.Skip 消耗一致（{lvl}）", skipConsumed == decConsumed);
        }

        // ── PackFileReader 新回调 API：逐对象回调，内容一致 ──
        {
            var plainA = Encoding.UTF8.GetBytes("hello world");
            var plainB = Encoding.UTF8.GetBytes("second blob content");
            var shaA = PackFileReader.ObjectSha("blob", plainA);
            var shaB = PackFileReader.ObjectSha("blob", plainB);
            var writerObjs = new List<(string Type, string Sha, byte[] Content)>
            {
                ("blob", shaA, plainA),
                ("blob", shaB, plainB),
            };
            var writerPack = PackFileWriter.Write(writerObjs);
            var seen = new Dictionary<string, byte[]>();
            int cbCount = PackFileReader.Read(writerPack, null, (type, sha, content) => seen[sha] = content!.ToArray());
            Check("PackFileReader 回调对象数=2", cbCount == 2);
            Check("回调 blob A 内容一致",
                seen.TryGetValue(shaA, out var gotA) && gotA.AsSpan().SequenceEqual(plainA));
            Check("回调 blob B 内容一致",
                seen.TryGetValue(shaB, out var gotB) && gotB.AsSpan().SequenceEqual(plainB));
        }

        // ── ofs-delta 包解码：手动构造 full blob + ofs-delta（base 提前、delta 引用其偏移）──
        {
            var baseContent = Encoding.UTF8.GetBytes("abcdefghijklmnop");
            byte[] CompressZ(byte[] data)
            {
                using var ms = new MemoryStream();
                using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(data);
                return ms.ToArray();
            }
            var baseZ = CompressZ(baseContent);
            // delta = src16 dst16 + copy(offset=0,size=16)；copy 命令须置 bit7(0x80)，
            // bit0=offset 有 1 字节、bit4=size 有 1 字节 → 0x91
            byte[] delta = { 0x10, 0x10, 0x91, 0x00, 0x10 };
            var deltaZ = CompressZ(delta);

            using var opms = new MemoryStream();
            opms.Write("PACK"u8.ToArray());
            opms.WriteByte(0); opms.WriteByte(0); opms.WriteByte(0); opms.WriteByte(2);   // version 2
            opms.WriteByte(0); opms.WriteByte(0); opms.WriteByte(0); opms.WriteByte(2);   // 2 objects
            // object0：blob size=16 → 头 0xB0 0x01 + zlib
            opms.WriteByte(0xB0); opms.WriteByte(0x01);
            opms.Write(baseZ);
            // object1：ofs-delta size=5 → 头 0x65 + ofs 距离 + zlib；距离 = object0 总长
            opms.WriteByte(0x65);
            opms.WriteByte((byte)(2 + baseZ.Length));
            opms.Write(deltaZ);
            var opackBody = opms.ToArray();
            using var ofull = new MemoryStream();
            ofull.Write(opackBody);
            ofull.Write(System.Security.Cryptography.SHA1.HashData(opackBody));
            var opack = ofull.ToArray();

            string? gotType = null, gotSha = null;
            byte[]? gotContent = null;
            int ocnt = PackFileReader.Read(opack, null,
                (type, sha, content) => { gotType = type; gotSha = sha; gotContent = content!.ToArray(); });
            // 包含 2 个对象（full blob + ofs-delta），Read 应回调 2 次
            Check("ofs-delta 包回调对象数=2", ocnt == 2);
            Check("ofs-delta 解出类型 blob", gotType == "blob");
            Check("ofs-delta 解出 sha 正确", gotSha == PackFileReader.ObjectSha("blob", baseContent));
            Check("ofs-delta 解出内容一致", gotContent != null && gotContent.AsSpan().SequenceEqual(baseContent));
        }

        // ── ReadFile：从临时文件解码（大仓库 pack 数百 MB 不能整包读内存，走文件分块）──
        {
            var rfPlain = Encoding.UTF8.GetBytes("file-backed decode hello world 测试");
            var rfSha = PackFileReader.ObjectSha("blob", rfPlain);
            var rfPack = PackFileWriter.Write(new List<(string, string, byte[])> { ("blob", rfSha, rfPlain) });
            var rfFile = Path.Combine(Path.GetTempPath(), "wcrf_" + Guid.NewGuid().ToString("N")[..8] + ".pack");
            File.WriteAllBytes(rfFile, rfPack);
            try
            {
                Check("ReadObjectCount 读取对象数", PackFileReader.ReadObjectCount(rfFile) == 1);
                byte[]? rfGot = null;
                int rfCnt = PackFileReader.ReadFile(rfFile, null, (type, sha, content) => rfGot = content!.ToArray());
                Check("ReadFile 回调对象数=1", rfCnt == 1);
                Check("ReadFile 内容一致", rfGot != null && rfGot.AsSpan().SequenceEqual(rfPlain));
            }
            finally { try { File.Delete(rfFile); } catch { } }
        }

        // ── 大对象原生路径：>16MB 对象内容走原生内存（onLargeObject），不物化托管 byte[]（安卓堆装不下）──
        {
            var bigPlain = new byte[20 * 1024 * 1024];
            new Random(7).NextBytes(bigPlain);   // 不可压缩 → stored 块，压缩数据≈内容
            var bigSha = PackFileReader.ObjectSha("blob", bigPlain);
            var bigPack = PackFileWriter.Write(new List<(string, string, byte[])> { ("blob", bigSha, bigPlain) });
            long gotLen = 0;
            string? gotType = null, shaGot = null;
            byte[]? gotContent = null;
            int callbacks = 0;
            int bcnt = PackFileReader.Read(bigPack, null,
                (type, sha, content) => { if (content == null) { gotType = type; shaGot = sha; } callbacks++; },
                null,
                onLargeObject: (type, sha, ptr, len) =>
                {
                    // 必须在回调内复制内容：回调返回后原生内存即被释放
                    gotLen = len;
                    gotContent = new byte[len];
                    unsafe { new ReadOnlySpan<byte>(ptr.ToPointer(), (int)len).CopyTo(gotContent); }
                });
            Check("大对象走原生回调（onObject null）", bcnt == 1 && callbacks == 1 && gotType == "blob" && shaGot == bigSha && gotLen == bigPlain.Length);
            Check("大对象原生内容一致", gotContent != null && gotContent.AsSpan().SequenceEqual(bigPlain));
        }

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

        // ── ReadObject 回退到 pack（系统 git gc 后对象入 pack）──
        // 曾 bug：增量 pull 时 thin pack 的 base 落在本地 pack（系统 git clone/`gc` 过），
        // ReadObject 只读 loose → 「delta 对象的 base 未找到」。修复后先 loose 再 pack。
        var gdir = Path.Combine(Path.GetTempPath(), "wcgc_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(gdir);
        try
        {
            if (RunGit(gdir, "init -q -b main"))
            {
                File.WriteAllText(Path.Combine(gdir, "a.txt"), "pack base content 0");
                RunGit(gdir, "add -A");
                RunGit(gdir, "commit -q -m init");
                for (int i = 1; i <= 12; i++)
                {
                    File.WriteAllText(Path.Combine(gdir, "a.txt"), "pack base content " + i);
                    RunGit(gdir, "commit -q -am c" + i);
                }
                // repack -d 打包所有对象并删除 loose，但保留 loose ref（gc 会连带 pack-refs，
                // 使 ReadHeadCommit 读不到 refs/heads/main → head 为 null → 遍历空）。故用 repack。
                RunGit(gdir, "repack -q -d");
                var gitDir = Path.Combine(gdir, ".git");

                int packRead = 0, fail = 0, total = 0;
                var queue = new Queue<string>();
                var seen = new HashSet<string>();
                var head = GitCore.ReadHeadCommit(gitDir);
                if (head != null) queue.Enqueue(head);
                while (queue.Count > 0 && total < 2000)
                {
                    var sha = queue.Dequeue();
                    if (!seen.Add(sha)) continue;
                    total++;
                    var obj = GitCore.ReadObject(gitDir, sha);
                    if (obj == null) { fail++; continue; }
                    var loosePath = Path.Combine(gitDir, "objects", sha[..2], sha[2..]);
                    if (!File.Exists(loosePath)) packRead++;
                    var (type, content) = obj.Value;
                    if (type == "commit")
                    {
                        foreach (var line in Encoding.UTF8.GetString(content).Split('\n'))
                        {
                            if (line.StartsWith("tree ")) queue.Enqueue(line[5..]);
                            else if (line.StartsWith("parent ")) queue.Enqueue(line[7..]);
                        }
                    }
                    else if (type == "tree")
                    {
                        int p = 0;
                        while (p < content.Length)
                        {
                            var sp = Array.IndexOf(content, (byte)' ', p);
                            if (sp < 0) break;
                            var nul = Array.IndexOf(content, (byte)0, sp);
                            if (nul < 0) break;
                            queue.Enqueue(Convert.ToHexString(content, nul + 1, 20).ToLowerInvariant());
                            p = nul + 1 + 20;
                        }
                    }
                }
                Check($"ReadObject 回退 pack（{total} 对象，pack 读 {packRead}，失败 {fail}）", fail == 0 && packRead > 0);
            }
            Directory.Delete(gdir, recursive: true);
        }
        catch { try { Directory.Delete(gdir, recursive: true); } catch { } }
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

    /// <summary>在指定工作目录跑系统 git（自测构造 pack 仓库用；无 git 环境返回 false）。</summary>
    private static bool RunGit(string workDir, string args)
    {
        try
        {
            using var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            p.Start();
            p.WaitForExit();
            return p.ExitCode == 0;
        }
        catch { return false; }
    }
}
