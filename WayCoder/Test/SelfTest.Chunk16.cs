using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Edit;
using WayCoder.UI.Web;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk16(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[EditorCore 选区锚点]");
        var tmpDir = Path.Combine(Path.GetTempPath(), "wc_selftest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tmpDir);
        try
        {
            // 编辑器核心经 LoadFile 初始化（空缓冲 InsertText 会越界，与真实编辑器流程一致）
            var selFile = Path.Combine(tmpDir, "sel.txt");
            File.WriteAllText(selFile, "hello world");
            var ec = new EditorCore();
            ec.LoadFile(selFile);
            Check("无选区时 SelectionAnchor 为 null", ec.SelectionAnchor == null);
            ec.StartSelection();
            ec.MoveCursor(5, 0); // 锚点(0,0)，光标(0,5)
            Check("StartSelection 后锚点 = 起点", ec.SelectionAnchor is { Line: 0, Col: 0 });
            Check("选区存在", ec.HasSelection);
            Check("选区文本", ec.GetSelectedText() == "hello");
            ec.ClearSelection();
            Check("ClearSelection 后锚点 null", ec.SelectionAnchor == null && !ec.HasSelection);
            ec.SelectAll();
            Check("SelectAll 后有选区", ec.HasSelection && ec.SelectionAnchor != null);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }

        Section("[EditorCore 行尾]");
        tmpDir = Path.Combine(Path.GetTempPath(), "wc_selftest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tmpDir);
        try
        {
            // CRLF 文件：Load → 修改 → Save 必须保留 \r\n
            var crlf = Path.Combine(tmpDir, "crlf.txt");
            File.WriteAllText(crlf, "line1\r\nline2\r\n", Encoding.UTF8);
            var ec2 = new EditorCore();
            ec2.LoadFile(crlf);
            ec2.InsertText("X");
            ec2.Save();
            var after = File.ReadAllBytes(crlf);
            // 注意 File.WriteAllText+Encoding.UTF8 带 BOM（EF BB BF），下标需跳过 BOM
            Check("CRLF 文件保存后保留 \\r\\n", after[9] == (byte)'\r' && after[10] == (byte)'\n');
            Check("CRLF 文件保存内容正确", Encoding.UTF8.GetString(after, 3, after.Length - 3).StartsWith("Xline1\r\nline2"));

            // LF 文件：保存后保持 LF
            var lf = Path.Combine(tmpDir, "lf.txt");
            File.WriteAllText(lf, "a\nb\n", Encoding.UTF8);
            var ec3 = new EditorCore();
            ec3.LoadFile(lf);
            ec3.InsertText("Z");
            ec3.Save();
            var afterLf = File.ReadAllBytes(lf);
            Check("LF 文件保存后保持 LF", afterLf[5] == (byte)'\n' && afterLf[6] == (byte)'b');
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }

        Section("[文件编码 BOM]");
        tmpDir = Path.Combine(Path.GetTempPath(), "wc_selftest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tmpDir);
        try
        {
            // 新文件写入：无 BOM
            var noBom = Path.Combine(tmpDir, "plain.py");
            Global.WriteAllTextPreserveBom(noBom, "print('hi')\n");
            var b1 = File.ReadAllBytes(noBom);
            Check("新文件写入无 BOM", b1.Length >= 3 && !(b1[0] == 0xEF && b1[1] == 0xBB && b1[2] == 0xBF));

            // 原文件带 BOM：保留
            var bom = Path.Combine(tmpDir, "bom.py");
            File.WriteAllText(bom, "x = 1\n", new UTF8Encoding(true));
            Global.WriteAllTextPreserveBom(bom, "x = 2\n");
            var b2 = File.ReadAllBytes(bom);
            Check("原带 BOM 文件保留 BOM", b2[0] == 0xEF && b2[1] == 0xBB && b2[2] == 0xBF);

            // EditorCore.Save：无 BOM 文件保存后仍无 BOM
            var ecBom = new EditorCore();
            ecBom.LoadFile(noBom);
            ecBom.InsertText("# ");
            ecBom.Save();
            var b3 = File.ReadAllBytes(noBom);
            Check("EditorCore 保存无 BOM 文件不新增 BOM", b3[0] != 0xEF || b3[1] != 0xBB || b3[2] != 0xBF);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }

        Section("[文件编码 自动识别]");
        // UTF-8 无 BOM
        var dUtf8 = TextEncoding.Detect(Encoding.UTF8.GetBytes("hello 中文"));
        Check("UTF-8 无 BOM 识别", dUtf8.EncodingName == "UTF-8" && dUtf8.Text == "hello 中文");

        // UTF-8 BOM
        var dUtf8Bom = TextEncoding.Detect(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("x")).ToArray());
        Check("UTF-8 BOM 识别", dUtf8Bom.EncodingName == "UTF-8 BOM" && dUtf8Bom.Text == "x");

        // UTF-16 LE / BE BOM
        var dU16le = TextEncoding.Detect(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("ab")).ToArray());
        Check("UTF-16 LE 识别", dU16le.EncodingName == "UTF-16 LE" && dU16le.Text == "ab");
        var dU16be = TextEncoding.Detect(Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("ab")).ToArray());
        Check("UTF-16 BE 识别", dU16be.EncodingName == "UTF-16 BE" && dU16be.Text == "ab");

        // GB18030（GBK/GB2312 中文旧编码，GBK 字节序列非合法 UTF-8，应落入 GB18030 分支）
        var dGb = TextEncoding.Detect(TextEncoding.GB18030.GetBytes("你好，GBK 编码文件"));
        Check("GB18030 识别 + 解码", dGb.EncodingName == "GB18030" && dGb.Text == "你好，GBK 编码文件");

        // EditorCore 端到端：GB18030 文件 → LoadFile 识别 → Save 保真（不转 UTF-8、不加 BOM）
        var encDir = Path.Combine(Path.GetTempPath(), "wc_selftest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(encDir);
        try
        {
            var gbFile = Path.Combine(encDir, "gbk.txt");
            File.WriteAllBytes(gbFile, TextEncoding.GB18030.GetBytes("中文内容"));
            var ecGb = new EditorCore();
            ecGb.LoadFile(gbFile);
            Check("EditorCore 识别 GB18030", ecGb.EncodingName == "GB18030" && ecGb.Lines[0].ToString() == "中文内容");
            ecGb.Save();
            var gbAfter = TextEncoding.Detect(File.ReadAllBytes(gbFile));
            Check("EditorCore 保存 GB18030 保真", gbAfter.EncodingName == "GB18030" && gbAfter.Text == "中文内容");
        }
        finally
        {
            try { Directory.Delete(encDir, true); } catch { }
        }

        Section("[编码转换]");
        // ResolveEncoding：编码名/别名/代码页数字解析（默认 UTF-8）
        Check("ResolveEncoding utf-8 无 BOM", TextEncoding.ResolveEncoding("utf-8").GetPreamble().Length == 0);
        Check("ResolveEncoding utf-8-bom 带 BOM", TextEncoding.ResolveEncoding("utf-8-bom").GetPreamble().Length == 3);
        Check("ResolveEncoding gbk=936", TextEncoding.ResolveEncoding("gbk").CodePage == 936);
        Check("ResolveEncoding big5=950", TextEncoding.ResolveEncoding("big5").CodePage == 950);
        Check("ResolveEncoding shift-jis=932", TextEncoding.ResolveEncoding("shift-jis").CodePage == 932);
        Check("ResolveEncoding euc-kr=51949", TextEncoding.ResolveEncoding("euc-kr").CodePage == 51949);
        Check("ResolveEncoding iso-8859-1=28591", TextEncoding.ResolveEncoding("iso-8859-1").CodePage == 28591);
        Check("ResolveEncoding windows-1252=1252", TextEncoding.ResolveEncoding("windows-1252").CodePage == 1252);
        Check("ResolveEncoding 代码页数字 936", TextEncoding.ResolveEncoding("936").CodePage == 936);
        Check("ResolveEncoding 未知名回退 UTF-8", TextEncoding.ResolveEncoding("不存在的编码xyz").WebName == "utf-8");

        var encConvDir = Path.Combine(Path.GetTempPath(), "wc_enc_conv_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(encConvDir);
        try
        {
            var tool = new WayCoder.Tools.ConvertEncodingTool();
            var gbk = TextEncoding.ResolveEncoding("gbk");
            var srcText = "你好，世界！GBK 编码转换测试";

            // Decode：GBK 字节 → 文本（往返）
            var gbkBytes = gbk.GetBytes(srcText);
            Check("Decode GBK 往返", TextEncoding.Decode(gbkBytes, gbk) == srcText);

            // 工具：GBK → UTF-8 原地覆盖
            var gbkFile = Path.Combine(encConvDir, "gbk.txt");
            File.WriteAllBytes(gbkFile, gbkBytes);
            var r1 = tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = gbkFile, ["from_encoding"] = "gbk", ["to_encoding"] = "utf-8" }).GetAwaiter().GetResult();
            var o1 = File.ReadAllBytes(gbkFile);
            Check("convert_encoding GBK→UTF-8", Encoding.UTF8.GetString(o1) == srcText && !(o1[0] == 0xEF && o1[1] == 0xBB && o1[2] == 0xBF));
            Check("convert_encoding 结果含编码名", r1.Contains("gbk") && r1.Contains("utf-8"));

            // 工具：auto 自动识别 GBK → UTF-8（不指定 from）
            var autoFile = Path.Combine(encConvDir, "auto.txt");
            File.WriteAllBytes(autoFile, gbkBytes);
            tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = autoFile }).GetAwaiter().GetResult();
            Check("convert_encoding auto 识别 GBK", Encoding.UTF8.GetString(File.ReadAllBytes(autoFile)) == srcText);

            // 工具：output 到新路径，原文件不动
            var keepFile = Path.Combine(encConvDir, "keep.txt");
            File.WriteAllBytes(keepFile, gbkBytes);
            var outFile = Path.Combine(encConvDir, "out.txt");
            tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = keepFile, ["from_encoding"] = "gbk", ["to_encoding"] = "utf-8", ["output"] = outFile }).GetAwaiter().GetResult();
            Check("convert_encoding output 新路径原文件不动", Encoding.UTF8.GetString(File.ReadAllBytes(outFile)) == srcText && File.ReadAllBytes(keepFile).SequenceEqual(gbkBytes));

            // 工具：UTF-8 → GBK 反向
            var revFile = Path.Combine(encConvDir, "rev.txt");
            File.WriteAllText(revFile, srcText, new UTF8Encoding(false));
            tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = revFile, ["from_encoding"] = "utf-8", ["to_encoding"] = "gbk" }).GetAwaiter().GetResult();
            Check("convert_encoding UTF-8→GBK 反向", gbk.GetString(File.ReadAllBytes(revFile)) == srcText);

            // 工具：输出 UTF-8 BOM
            var bomFile = Path.Combine(encConvDir, "bom.txt");
            File.WriteAllText(bomFile, "hi", new UTF8Encoding(false));
            tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = bomFile, ["to_encoding"] = "utf-8-bom" }).GetAwaiter().GetResult();
            var bomOut = File.ReadAllBytes(bomFile);
            Check("convert_encoding 输出 UTF-8 BOM", bomOut.Length >= 3 && bomOut[0] == 0xEF && bomOut[1] == 0xBB && bomOut[2] == 0xBF);

            // 二进制文件拒绝
            var binFile = Path.Combine(encConvDir, "bin.dat");
            File.WriteAllBytes(binFile, new byte[] { 1, 0, 2, 3, 4 });
            var rBin = tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = binFile }).GetAwaiter().GetResult();
            Check("convert_encoding 拒绝二进制", rBin.Contains("二进制"));
        }
        finally
        {
            try { Directory.Delete(encConvDir, true); } catch { }
        }

        Section("[Syntax ANSI 契约]");
        var allowed = new HashSet<int> { 0, 2, 31, 32, 33, 34, 35, 36, 41, 103 };
        var samples = new (string Lang, string Line)[]
        {
            ("csharp", "public static void Main(string[] args) { var x = 42; /* 注释 */ }"),
            ("python", "def foo(x):  # 注释\n    return x + 1"),
            ("js",     "const f = (a, b) => { return a + b; }; // 行注释"),
            ("go",     "func main() { var s = \"hello\" }"),
            ("json",   "{\"key\": [1, 2, 3]}"),
            ("bash",   "if [ -f x ]; then echo hi; fi"),
            ("sql",    "SELECT * FROM t WHERE id = 1"),
        };
        var contractOk = true;
        foreach (var (lang, line) in samples)
        {
            var tokens = Syntax.ByLanguage(lang).Tokenize(line);
            foreach (var (_, color) in tokens)
            {
                if (!allowed.Contains(color)) { contractOk = false; break; }
            }
        }
        Check("Tokenize 色值 ∈ {0,2,31..36,41,103}（跨端映射契约）", contractOk);
        // 空串返回 1 个占位 token（空格, Default），不崩溃
        var empty = Syntax.ByLanguage("csharp").Tokenize("");
        Check("空串 Tokenize 返回占位 token 不崩溃", empty.Count == 1 && empty[0].Color == 0);

        Section("[Web 编辑器路径]");
        var root = Path.Combine(Path.GetTempPath(), "wc_editroot_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        try
        {
            // SafeResolveWithinRoot：正常 / 越界 / 绝对路径 / 编码穿越
            Check("根内相对路径解析", WebChatServer.SafeResolveWithinRoot(root, "src/foo.cs") != null);
            Check("../ 越界拒绝", WebChatServer.SafeResolveWithinRoot(root, "../evil.txt") == null);
            Check("../../ 深层越界拒绝", WebChatServer.SafeResolveWithinRoot(root, "a/../../evil.txt") == null);
            Check("根外绝对路径拒绝", WebChatServer.SafeResolveWithinRoot(root, Path.Combine(Path.GetTempPath(), "x.txt")) == null);
            Check("根自身允许", WebChatServer.SafeResolveWithinRoot(root, root) != null);

            // SaveEditorFile：写内/拒外/往返
            var saved = WebChatServer.SaveEditorFile(root, "sub/nested.txt", "hello\nworld");
            Check("SaveEditorFile 建目录并写入", saved != null && File.Exists(Path.Combine(root, "sub", "nested.txt")));
            Check("SaveEditorFile 内容往返", File.ReadAllText(Path.Combine(root, "sub", "nested.txt")) == "hello\nworld");
            Check("SaveEditorFile 越界拒绝", WebChatServer.SaveEditorFile(root, "../evil.txt", "x") == null && !File.Exists(Path.Combine(Path.GetTempPath(), "evil.txt")));

            // SerializeEditorList：dirs 优先 + 排序 + 过滤隐藏
            Directory.CreateDirectory(Path.Combine(root, "b_dir"));
            Directory.CreateDirectory(Path.Combine(root, "a_dir"));
            File.WriteAllText(Path.Combine(root, "z_file.txt"), "");
            File.WriteAllText(Path.Combine(root, "a_file.txt"), "");
            File.WriteAllText(Path.Combine(root, ".hidden"), "");   // 应被过滤
            File.WriteAllText(Path.Combine(root, "node_modules"), ""); // 应被过滤（junk）
            var entries = System.Text.Json.JsonDocument.Parse(WebChatServer.SerializeEditorList(root));
            var arr = entries.RootElement.EnumerateArray().ToList(); // SerializeEditorList 直接返回数组 JSON
            var names = arr.Select(x => x.GetProperty("name").GetString()!).ToList();
            var isDirs = arr.Select(x => x.GetProperty("isDir").GetBoolean()).ToList();
            Check("目录排在文件前", names[0] == "a_dir/" && names[1] == "b_dir/" && isDirs[0] && isDirs[1]);
            Check("文件按名排序", names.Contains("a_file.txt") && names.Contains("z_file.txt"));
            Check("过滤隐藏/构建产物", !names.Contains(".hidden") && !names.Contains("node_modules"));
            Check("条目含 path", arr.All(x => x.GetProperty("path").GetString()!.Length > 0));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
