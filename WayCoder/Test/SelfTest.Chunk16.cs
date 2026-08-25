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
