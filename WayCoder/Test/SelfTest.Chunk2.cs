using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk2(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[Git]");
        var gitTool = new GitTool();
        var gitResult = gitTool.ExecuteAsync(new() { ["command"] = "--version" }).Result;
        Check("git --version 可执行", gitResult.Contains("git version"));
        Check("git push --force 被阻止",
            gitTool.ExecuteAsync(new() { ["command"] = "push --force origin main" }).Result.Contains("已阻止"));
        Console.WriteLine();

        // ---- fetch ----
        Section("[Fetch]");
        var fetchTool = new FetchTool();
        Check("fetch 拒绝非 http URL",
            fetchTool.ExecuteAsync(new() { ["url"] = "ftp://evil.com" }).Result.Contains("错误"));
        Check("fetch 拒绝非法 HTTP 方法",
            fetchTool.ExecuteAsync(new() { ["url"] = "https://example.com", ["method"] = "TRACE" }).Result.Contains("不支持的 HTTP 方法"));
        Check("fetch method 大小写规范化",
            fetchTool.ExecuteAsync(new() { ["url"] = "https://example.com", ["method"] = "trace" }).Result.Contains("不支持的 HTTP 方法"));
        // headers 解析（离线可测，不发起网络）
        var h = FetchTool.ParseHeaders("{\"Authorization\":\"Bearer abc\",\"Content-Type\":\"application/json\"}");
        Check("fetch headers 解析", h != null && h.Count == 2 && h["authorization"] == "Bearer abc");
        Check("fetch headers 空/非法 JSON 返回 null",
            FetchTool.ParseHeaders(null) == null && FetchTool.ParseHeaders("not-json") == null && FetchTool.ParseHeaders("  ") == null);
        Console.WriteLine();

        // ---- sqlite ----
        Section("[Sqlite]");
        var sqliteTool = new SqliteTool();
        try
        {
            var sqliteResult = sqliteTool.ExecuteAsync(new() { ["query"] = "SELECT 1 AS x" }).Result;
            if (sqliteResult.Contains("未找到"))
                Check("sqlite 未安装友好提示", sqliteResult.Contains("sqlite3"));
            else
                Check("sqlite SELECT 查询", sqliteResult.Contains("1") && !sqliteResult.Contains("错误"));
        }
        catch { Fail("sqlite 查询不崩溃"); }
        Check("sqlite 空查询报错",
            sqliteTool.ExecuteAsync(new() { ["query"] = "" }).Result.Contains("错误"));
        Console.WriteLine();

        // ---- test ----
        Section("[Test]");
        var testTool = new TestTool();
        // 解析逻辑（纯离线，不依赖真实测试框架）
        var testPass = TestTool.BuildSummary(0, "100 passed, 0 failed in 5.2s");
        Check("test 解析通过", testPass.Contains("通过") && testPass.Contains("100"));
        var testFail = TestTool.BuildSummary(1, "FAILED tests/test_x.py::test_bar\n2 passed, 1 failed in 3s");
        Check("test 解析失败", testFail.Contains("失败") && testFail.Contains("test_x"));
        var testCounts = TestTool.ExtractCounts("10 passed, 3 failed");
        Check("test 提取 pytest 统计", testCounts.Passed == 10 && testCounts.Failed == 3);
        var testDotnet = TestTool.ExtractCounts("Passed!  - Failed: 0, Passed: 100, Skipped: 2");
        Check("test 提取 dotnet 统计", testDotnet.Passed == 100 && testDotnet.Failed == 0);
        var testNoStats = TestTool.ExtractCounts("no recognizable output here");
        Check("test 无统计返回 -1", testNoStats.Passed == -1 && testNoStats.Failed == -1);
        var testFailures = TestTool.ExtractFailures("FAILED test_a\nError: boom\n2 passed, 1 failed\nFAILED test_b");
        Check("test 提取失败用例（排除摘要）",
            testFailures.Count == 3 && testFailures.Contains("FAILED test_a") && !testFailures.Contains("2 passed, 1 failed"));
        Check("test 空命令报错",
            testTool.ExecuteAsync(new() { ["command"] = "" }).Result.Contains("错误"));
        Console.WriteLine();

        // ---- todo ----
        Section("[Todo]");
        var todoTool = new TodoTool();
        // 先清空，确保干净状态
        todoTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();
        var createResult = todoTool.ExecuteAsync(new() { ["action"] = "create", ["id"] = "test-1", ["title"] = "测试任务" }).Result;
        Check("todo create", createResult.Contains("创建") && TodoTool.Items.Count == 1);
        var updateResult = todoTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = "test-1", ["status"] = "completed" }).Result;
        Check("todo update", updateResult.Contains("completed") && TodoTool.Items[0].Status == "completed");
        var listResult = todoTool.ExecuteAsync(new() { ["action"] = "list" }).Result;
        Check("todo list", listResult.Contains("✅") && listResult.Contains("测试任务"));
        todoTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();
        Check("todo clear", TodoTool.Items.Count == 0);
        Console.WriteLine();

        // ---- 权限系统 ----
        Section("[权限]");
        PermissionManager.SetMode("yolo");
        Check("权限 YOLO 模式", PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        var permCheck = PermissionManager.CheckAsync("bash", new() { ["command"] = "echo test" }).Result;
        Check("YOLO 模式自动放行", permCheck == true);
        PermissionManager.SetMode("ask");
        Console.WriteLine();

        // ---- 记忆系统（临时目录隔离，避免污染真实 memory.md）----
        Section("[记忆]");
        var savedMemCwd = Directory.GetCurrentDirectory();
        var memTestDir = Path.Combine(Path.GetTempPath(), "waycoder_memtest_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(memTestDir);
        try
        {
            Directory.SetCurrentDirectory(memTestDir);

            var e1 = StructuredMemory.Create("dotnet-aot", "项目使用 .NET 10 AOT 编译", "project", "C# AOT 单文件编译");
            Check("结构化创建", e1.Name == "dotnet-aot");
            Check("结构化读取", StructuredMemory.Get("dotnet-aot")?.Type == "project");
            StructuredMemory.Create("user-pref", "用户偏好中文界面", "user", "终端青色主题");
            Check("结构化搜索", StructuredMemory.Search("中文").Count == 1);
            Check("索引文件存在", File.Exists(StructuredMemory.IndexPath));
            var upd = StructuredMemory.Update("dotnet-aot", content: "C# AOT 单文件 ~8MB");
            Check("结构化更新", upd != null && upd.Content.Contains("8MB"));
            Check("结构化删除", StructuredMemory.Delete("user-pref"));
            Check("结构化计数", StructuredMemory.Count == 1);

            // 共享记忆
            StructuredMemory.SetShared("dotnet-aot", true);
            var sharedEntry = StructuredMemory.Get("dotnet-aot");
            Check("SetShared 标记为共享", sharedEntry?.IsShared == true);
            Check("ListShared 返回共享记忆", StructuredMemory.ListShared().Count == 1);
            StructuredMemory.SetShared("dotnet-aot", false);
            Check("Unshare 取消共享", StructuredMemory.Get("dotnet-aot")?.IsShared == false);
            Check("取消后 ListShared 为空", StructuredMemory.ListShared().Count == 0);
            Check("IsGitRepo 可调用", SharedMemoryManager.IsGitRepo() || !SharedMemoryManager.IsGitRepo());
            SharedMemoryManager.ResetCache();
        }
        finally
        {
            Directory.SetCurrentDirectory(savedMemCwd);
            try { Directory.Delete(memTestDir, true); } catch { }
        }
        Console.WriteLine();

        // ---- 代码知识库（源码符号提取 + TF-IDF 召回）----
        Section("[代码知识库]");
        var codeDir = Path.Combine(Path.GetTempPath(), "waycoder_codetest_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(codeDir);
        try
        {
            File.WriteAllText(Path.Combine(codeDir, "auth.cs"),
                @"/// <summary>验证用户登录凭证并返回 JWT token。</summary>
public string Authenticate(string username, string password)
{
    return IssueToken(username);
}

public class UserRepository
{
    public User FindByEmail(string email) { return null; }
}");
            File.WriteAllText(Path.Combine(codeDir, "util.py"),
                @"# 计算两个字符串的编辑距离
def levenshtein_distance(a, b):
    return 0

class Matrix:
    def multiply(self, x, y):
        return []");

            var chunks = CodeKnowledge.Ingest(codeDir);
            var titles = chunks.Select(c => c.Title).ToList();
            Check("代码符号块数 > 0", chunks.Count > 0);
            Check("提取 C# 方法 Authenticate", titles.Any(t => t.Contains("auth.cs") && t.Contains("Authenticate")));
            Check("提取 C# 类 UserRepository", titles.Any(t => t.Contains("UserRepository")));
            Check("提取 Python 函数 levenshtein_distance", titles.Any(t => t.Contains("levenshtein_distance")));
            Check("提取 Python 类 Matrix", titles.Any(t => t.Contains("Matrix")));

            var authChunk = chunks.FirstOrDefault(c => c.Title.Contains("Authenticate"));
            Check("Authenticate 块含文档注释", authChunk != null && authChunk.Content.Contains("验证用户登录"));

            var hits = SemanticMemory.SearchRelevant(chunks, "用户登录凭证 JWT token", 3);
            Check("TF-IDF 召回登录相关代码", hits.Any(h => h.Doc.Title.Contains("Authenticate")));
        }
        finally
        {
            try { Directory.Delete(codeDir, true); } catch { }
        }
        Console.WriteLine();

        // ---- 语义代码检索（向量嵌入混合检索，注入假 embedder 无需真实 API）----
        Section("[语义代码检索]");
        var vecCwd = Path.Combine(Path.GetTempPath(), "vec_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(vecCwd);
        var vecSaved = Environment.CurrentDirectory;
        var savedEmbeddingConfig = Config.Instance.EmbeddingEnabled;
        var savedStoreEnabled = EmbeddingStore.Enabled;
        Environment.CurrentDirectory = vecCwd;
        try
        {
            CodeEmbeddingCache.Reset();

            // 缓存：块键内容敏感 + 往返 + Prune 孤儿
            var docA = new SemanticMemory.MemoryDocument { Title = "src/auth.cs › Authenticate", Content = "验证用户登录凭证" };
            var docA2 = new SemanticMemory.MemoryDocument { Title = "src/auth.cs › Authenticate", Content = "验证用户登录凭证（改了内容）" };
            Check("块键内容敏感", CodeEmbeddingCache.ChunkKey(docA) != CodeEmbeddingCache.ChunkKey(docA2));
            Check("块键同内容稳定", CodeEmbeddingCache.ChunkKey(docA) == CodeEmbeddingCache.ChunkKey(
                new SemanticMemory.MemoryDocument { Title = "src/auth.cs › Authenticate", Content = "验证用户登录凭证" }));

            var keyA = CodeEmbeddingCache.ChunkKey(docA);
            CodeEmbeddingCache.SaveVector(keyA, new float[] { 1, 0, 0 });
            Check("缓存往返", CodeEmbeddingCache.GetVector(keyA)?.Length == 3);
            CodeEmbeddingCache.Prune(new HashSet<string> { "other-key" });
            Check("Prune 删孤儿", CodeEmbeddingCache.GetVector(keyA) == null);

            // 混合检索：注入确定性 embedder（任何输入返回 [1,0,0]）
            EmbeddingStore.Enabled = true;
            Config.Instance.EmbeddingEnabled = true;
            var loginDoc = new SemanticMemory.MemoryDocument { Title = "src/auth.cs › Authenticate", Content = "验证用户登录凭证，返回 JWT token" };
            var drawDoc = new SemanticMemory.MemoryDocument { Title = "src/canvas.cs › DrawPixel", Content = "绘制图形像素填充扫描线" };
            CodeEmbeddingCache.SaveVector(CodeEmbeddingCache.ChunkKey(loginDoc), new float[] { 1, 0, 0 }); // 与查询向量接近
            var docs2 = new List<SemanticMemory.MemoryDocument> { loginDoc, drawDoc };
            Func<string, Task<float[]?>> fakeEmbed = _ => Task.FromResult<float[]?>(new float[] { 1, 0, 0 });
            var hybrid = EmbeddingStore.SearchRelevantHybrid(docs2, "登录验证", 2, fakeEmbed).GetAwaiter().GetResult();
            Check("混合检索登录块排前", hybrid.Count >= 1 && hybrid[0].Doc.Title.Contains("Authenticate"));
            Check("混合检索登录块分 > 0", hybrid.Count >= 1 && hybrid[0].Score > 0);

            // 回退：embedder 返回 null（模拟 API 失败）→ 结果与纯 TF-IDF 一致
            Func<string, Task<float[]?>> nullEmbed = _ => Task.FromResult<float[]?>(null);
            var fallback = EmbeddingStore.SearchRelevantHybrid(docs2, "登录验证", 2, nullEmbed).GetAwaiter().GetResult();
            var tfidfTop = SemanticMemory.SearchRelevant(docs2, "登录验证", 2);
            Check("向量失败回退 TF-IDF", fallback.Count >= 1 && tfidfTop.Count >= 1 && fallback[0].Doc.Title == tfidfTop[0].Doc.Title);

            // QueryAsync：EmbeddingEnabled=false → 走 TF-IDF（不依赖 LLM/API）
            File.WriteAllText(Path.Combine(vecCwd, "auth.cs"),
                "public class Auth { public string Authenticate() { return \"登录凭证\"; } }");
            Config.Instance.EmbeddingEnabled = false;
            ProjectKnowledge.Ingest(vecCwd);
            var qa = ProjectKnowledge.QueryAsync("登录").GetAwaiter().GetResult();
            Check("QueryAsync 关闭嵌入走 TF-IDF", qa.Contains("Authenticate"));
        }
        finally
        {
            Config.Instance.EmbeddingEnabled = savedEmbeddingConfig;
            EmbeddingStore.Enabled = savedStoreEnabled;
            Environment.CurrentDirectory = vecSaved;
            try { Directory.Delete(vecCwd, true); } catch { }
        }
        Console.WriteLine();

        // ---- 学习路径推荐（欠缺→进阶路线，注入假 summarize 无需真实 LLM）----
        Section("[学习路径]");
        var pathCwd = Path.Combine(Path.GetTempPath(), "lpath_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(pathCwd);
        var pathSaved = Environment.CurrentDirectory;
        Environment.CurrentDirectory = pathCwd;
        try
        {
            // 防御清理 + 种一个 gap 条目（降级测试素材）
            foreach (var n in new[] { "path-并发编程基础", "path-测试驱动开发", "gap-aot反射" })
                KbIndex.DeleteEntry(n);
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "gap-aot反射", Description = "欠缺知识：AOT 反射禁令", Kind = "gap", Content = "**欠缺**：AOT 无反射", Source = "git-gap" });

            // ParseLearningPath 纯解析
            var parsed = KbIndex.ParseLearningPath(
                "{\"path\":[{\"topic\":\"并发编程基础\",\"why\":\"多线程易错\",\"practice\":\"写生产者消费者\",\"check\":\"无锁实现\"},{\"topic\":\"测试驱动开发\",\"why\":\"回归\",\"practice\":\"红绿重构\",\"check\":\"TDD 流程\"}]}");
            Check("ParseLearningPath 解析步骤", parsed.Count == 2 && parsed[0].Topic == "并发编程基础" && parsed[0].Check == "无锁实现");

            // GenerateLearningPath 注入假 summarize → 写路径步（kind=gap, source=path）
            Func<string, Task<string?>> fakePath = _ => Task.FromResult<string?>(
                "{\"path\":[{\"topic\":\"并发编程基础\",\"why\":\"多线程易错\",\"practice\":\"生产者消费者\",\"check\":\"无锁实现\"}]}");
            var (gen, steps) = KbIndex.GenerateLearningPath(fakePath).GetAwaiter().GetResult();
            Check("生成路径步数", gen == 1 && steps.Count == 1);
            var pathEntry = KbIndex.ListEntries().FirstOrDefault(e => e.Source == "path");
            Check("路径步写入 source=path", pathEntry != null && pathEntry.Kind == "gap");
            Check("路径步内容含问答标记", pathEntry != null && pathEntry.Content.Contains("**现象**") && pathEntry.Content.Contains("**教训**"));

            // 复习集成：路径步进入 PickNextDue；未掌握提权重
            var due = KbIndex.PickNextDue(KbIndex.ListEntries());
            Check("路径步纳入复习轮换", due != null && due.Source == "path");
            if (pathEntry != null)
            {
                KbIndex.MarkReview(pathEntry.Name, false, "gap", ["path"]);
                var st = KbIndex.LoadReviewState();
                Check("路径步未掌握提权重", st.FirstOrDefault(i => i.Name == pathEntry.Name)?.Weight > 1.0);
            }

            // 降级：summarize 返回 null → 用 gap 清单生成基础路径（不失败）
            Func<string, Task<string?>> nullPath = _ => Task.FromResult<string?>(null);
            var (gen2, steps2) = KbIndex.GenerateLearningPath(nullPath).GetAwaiter().GetResult();
            Check("降级用 gap 清单生成", gen2 >= 1 && steps2.Count >= 1);

            // ProfileToJson 导出
            var pjson = KbIndex.ProfileToJson();
            Check("ProfileToJson 含画像字段", pjson.Contains("\"total_entries\"") && pjson.Contains("\"kb_kinds\"") && pjson.Contains("\"schema\""));
        }
        finally
        {
            // 清理本节的 KB 条目（含降级生成的 path-*），避免污染后续 [知识库经验] 计数断言
            foreach (var e in KbIndex.ListEntries())
                if (e.Source == "path") KbIndex.DeleteEntry(e.Name);
            KbIndex.DeleteEntry("gap-aot反射");
            Environment.CurrentDirectory = pathSaved;
            try { Directory.Delete(pathCwd, true); } catch { }
        }
        Console.WriteLine();

        // ---- 教学模式闭环（测验评判 → 更新 gap 权重，注入假 summarize 无需真实 LLM）----
        Section("[教学模式]");
        var teachCwd = Path.Combine(Path.GetTempPath(), "teach_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(teachCwd);
        var teachSaved = Environment.CurrentDirectory;
        var savedTeachMode = Config.Instance.TeachModeEnabled;
        Environment.CurrentDirectory = teachCwd;
        try
        {
            foreach (var n in new[] { "gap-并发", "gap-锁" }) KbIndex.DeleteEntry(n);
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "gap-并发", Description = "欠缺知识：并发编程", Kind = "gap", Content = "**欠缺**：并发", Source = "git-gap" });
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "gap-锁", Description = "欠缺知识：锁与同步", Kind = "gap", Content = "**欠缺**：锁", Source = "git-gap" });

            // 权重方法：降权 / clamp 上限
            KbIndex.SetGapWeight("gap-并发", 1.0);
            KbIndex.AdjustGapWeight("gap-并发", KbIndex.MasteredDelta);
            Check("掌握降权", KbIndex.LoadReviewState().First(i => i.Name == "gap-并发").Weight <= 0.7);
            KbIndex.AdjustGapWeight("gap-并发", 5.0);
            Check("权重上限 clamp", KbIndex.LoadReviewState().First(i => i.Name == "gap-并发").Weight <= 5.0);
            KbIndex.SetGapWeight("gap-并发", 1.0); // 重置，避免干扰后续评估断言

            // ParseAssessment 纯解析
            var (am, aw) = KbIndex.ParseAssessment("{\"mastered\":[\"并发编程\"],\"weak\":[\"锁\"]}");
            Check("ParseAssessment 解析", am.Count == 1 && am[0] == "并发编程" && aw.Count == 1 && aw[0] == "锁");

            // AssessTranscript 注入假 summarize → ApplyAssessment 更新权重
            Func<string, Task<string?>> fakeAssess = _ => Task.FromResult<string?>(
                "{\"mastered\":[\"并发编程\"],\"weak\":[\"锁与同步\"]}");
            var (tm, tw) = KbIndex.AssessTranscript("## assistant\n测验：并发与锁\n## user\n回答正确", fakeAssess).GetAwaiter().GetResult();
            var (mA, wA) = KbIndex.ApplyAssessment(tm, tw);
            Check("评估应用 mastered 降权", mA == 1 && KbIndex.LoadReviewState().First(i => i.Name == "gap-并发").Weight < 1.0);
            Check("评估应用 weak 提权", wA == 1 && KbIndex.LoadReviewState().First(i => i.Name == "gap-锁").Weight > 1.0);

            // 复习集成：弱项进 PickNextDue
            var teachDue = KbIndex.PickNextDue(KbIndex.ListEntries());
            Check("弱项进复习轮换", teachDue != null);

            // TeachBlock 教学法强化
            Config.Instance.TeachModeEnabled = true;
            try
            {
                var tp = SystemPrompt.Generate(WayCoder.Tools.ToolRegistry.AllTools);
                Check("TeachBlock 教学法强化", tp.Contains("归因") || tp.Contains("类比"));
            }
            finally { Config.Instance.TeachModeEnabled = false; }
        }
        finally
        {
            Config.Instance.TeachModeEnabled = savedTeachMode;
            foreach (var n in new[] { "gap-并发", "gap-锁" }) KbIndex.DeleteEntry(n);
            Environment.CurrentDirectory = teachSaved;
            try { Directory.Delete(teachCwd, true); } catch { }
        }
        Console.WriteLine();

        // ---- 自主学习知识库（四类经验存储 + LLM JSON 提炼 + 间隔重复 + 薄弱统计）----
        Section("[知识库经验]");
        try
        {
            // 存储 + 四类过滤（全局 ~/.waycoder/kb/，SelfTest 已 HomeOverride 隔离）
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "git-force-push", Description = "push 被拒先 rebase", Kind = "mistake", Content = "**现象**：push 被拒\n**根因**：历史分叉\n**修复**：fetch+rebase\n**教训**：不裸 force", Source = "test", Tags = ["git", "rebase"] });
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "fix-window-size", Description = "CI 无终端尺寸为 0", Kind = "bugfix", Content = "**现象**：CI 崩溃\n**根因**：Console 返回 0\n**修复**：回退 80/24", Source = "test", Tags = ["console", "ci"] });
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "habit-subagent", Description = "子智能体靠约束", Kind = "habit", Content = "**习惯**：不给工具", Source = "test" });
            KbIndex.WriteEntry(new KbIndex.KbEntry { Name = "gap-aot", Description = "欠缺：AOT 反射禁令", Kind = "gap", Content = "**欠缺**：AOT 无反射", Source = "test", Tags = ["aot"] });

            var all = KbIndex.ListEntries();
            Check("知识库四类条目入库", all.Count == 4);
            Check("四类过滤 kind 正确", all.All(e => KbIndex.KbKinds.Contains(e.Kind)));
            Check("mistake 条目可取回", KbIndex.Get("git-force-push")?.Kind == "mistake");
            Check("gap 条目带 tags", KbIndex.Get("gap-aot")?.Tags.Contains("aot") == true);

            // BuildEntry（LLM 提炼 JSON 解析）
            var draft = KbIndex.BuildEntry("{\"name\":\"json-test\",\"description\":\"JSON 提炼\",\"kind\":\"bugfix\",\"phenomenon\":\"P\",\"root_cause\":\"C\",\"fix\":\"F\",\"lesson\":\"L\",\"tags\":[\"a\",\"b\"],\"gaps\":[]}");
            Check("BuildEntry 解析 kind/tags", draft != null && draft.Kind == "bugfix" && draft.Tags.Count == 2);
            Check("BuildEntry 内容含现象/教训", draft != null && draft.Content.Contains("**现象**") && draft.Content.Contains("**教训**"));
            Check("BuildEntry 非法 JSON 返回 null", KbIndex.BuildEntry("not json") == null);
            Check("BuildEntry 未知 kind 归一", KbIndex.BuildEntry("{\"name\":\"x\",\"description\":\"d\",\"kind\":\"weird\",\"tags\":[]}")?.Kind == "bugfix");

            // gaps[] → 欠缺知识条目
            var gaps = KbIndex.ExtractGaps("{\"gaps\":[\"AOT 反射禁令\",\"并发锁\"],\"name\":\"x\"}");
            Check("gaps[] 提取欠缺条目", gaps.Count == 2 && gaps.All(g => g.Kind == "gap"));

            // 降级路径（无 LLM）
            var fallback = KbIndex.BuildFallback("fix: 修了个 bug", " 1 file changed");
            Check("Mine 降级生成 bugfix 条目", fallback.Kind == "bugfix" && fallback.Content.Contains("fix: 修了个 bug"));

            // 复习调度：未复习过 = 立即到期；mistake/bugfix 优先于 habit/gap
            var next = KbIndex.PickNextDue(KbIndex.ListEntries());
            Check("PickNextDue 未复习即到期且优先高优先级", next != null && (next.Kind is "mistake" or "bugfix"));
            KbIndex.MarkReview("git-force-push", true, "mistake", ["git", "rebase"]);
            next = KbIndex.PickNextDue(KbIndex.ListEntries());
            Check("掌握后跳过该条目", next?.Name == "fix-window-size");
            KbIndex.MarkReview("fix-window-size", true, "bugfix", ["console", "ci"]);
            next = KbIndex.PickNextDue(KbIndex.ListEntries());
            Check("高优先级全掌握后轮候低优先级", next != null && (next.Kind is "habit" or "gap"));

            var st = KbIndex.LoadReviewState();
            Check("掌握后间隔 3 天", st.First(i => i.Name == "git-force-push").IntervalDays == 3);
            Check("掌握后间隔 3 天(bugfix)", st.First(i => i.Name == "fix-window-size").IntervalDays == 3);

            // 未掌握 → 间隔重置 1 天 + 关联 gap 权重提升
            KbIndex.MarkReview("fix-window-size", false, "bugfix", ["aot"]); // tags 含 aot → 关联 gap-aot
            st = KbIndex.LoadReviewState();
            Check("未掌握间隔重置 1 天", st.First(i => i.Name == "fix-window-size").IntervalDays == 1);
            var gapItem = st.FirstOrDefault(i => i.Name == "gap-aot");
            Check("未掌握提升 gap 权重", gapItem != null && gapItem.Weight > 1.0);

            // weak 统计：gap 按权重排序在前、薄弱标签聚合、ErrorLog 信号
            var report = KbIndex.WeakStats();
            Check("weak gap 清单非空", report.Gaps.Count >= 1);
            Check("weak 薄弱标签聚合 git", report.WeakTags.Any(t => t.Tag == "git" && t.Count >= 1));

            // ErrorLog 信号（假日志文件）
            var logDir = Path.Combine(Path.GetTempPath(), "waycoder_kblog_" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(logDir);
            try
            {
                File.WriteAllText(Path.Combine(logDir, "error_20260825.log"),
                    "[2026-08-25 10:00:00] [ERROR] [Tool:Bash] command failed\n" +
                    "[2026-08-25 10:01:00] [ERROR] [Tool:Bash] command failed\n" +
                    "[2026-08-25 10:02:00] [FATAL] [LLM] timeout\n");
                var signals = KbIndex.ErrorLogSignals(logDir);
                Check("ErrorLog 信号按 source 聚合", signals.Count == 2 && signals.First().Source == "Tool:Bash" && signals.First().Count == 2);
            }
            finally { try { Directory.Delete(logDir, true); } catch { } }

            // /mind 是 /kb 的别名（SlashCommand.Aliases 匹配）
            Check("/mind 别名匹配 /kb", new WayCoder.UI.Cli.Commands.KbCommand().Matches("/mind save 测试"));
            Check("/kb 自身匹配", new WayCoder.UI.Cli.Commands.KbCommand().Matches("/kb weak"));

            // /mind 手动记忆：save 带日期 / search 检索 / forget 删除 / update 更新 / code 片段
            var manual = KbIndex.SaveManual("用户开发了 a 软件（22 种语言的一体编译器）");
            Check("/mind save 带日期上下文", manual.Content.StartsWith("**20") && manual.Content.Contains("22 种语言"));
            var found = KbIndex.Search("一体编译器", 3);
            Check("/mind search 检索命中", found.Count >= 1 && found.Any(h => h.Entry.Name == manual.Name));

            var snippet = KbIndex.SaveManual("class Fib { int F(int n) => n < 2 ? n : F(n-1) + F(n-2); }", "code");
            Check("/mind save code 自动识别代码片段", snippet.Kind == "code");
            Check("/mind save 自动识别代码", KbIndex.SaveManual("public static int Add(int a, int b) { return a + b; }").Kind == "code");
            Check("/mind save 普通内容默认 habit", KbIndex.SaveManual("我喜欢用 Vim").Kind == "habit");

            var updated = KbIndex.UpdateBestMatch("一体编译器", "a 软件升级为 42 种语言的一体编译器");
            Check("/mind update 更新内容", updated != null && updated.Content.Contains("42 种语言"));
            Check("/mind update 可检索新内容", KbIndex.Search("42 种语言", 2).Any(h => h.Entry.Name == manual.Name));

            var removed = KbIndex.DeleteBestMatch("42 种语言的一体编译器");
            Check("/mind forget 删除命中", removed != null && KbIndex.Get(manual.Name) == null);

            // kb 工具：聊天中运行时检索全局知识库
            Check("kb 工具已注册", WayCoder.Tools.ToolRegistry.BuiltinTools.Any(t => t.Name == "kb"));
            var kbOut = new WayCoder.Tools.KbTool().ExecuteAsync(new() { ["query"] = "git rebase push" }).GetAwaiter().GetResult();
            Check("kb 工具检索返回条目", kbOut.Contains("知识库匹配") && kbOut.Contains("push 被拒"));

            // ① 即时错误诊断（KB + git 修复史）
            var diag = KbIndex.DiagnoseError("git push 被拒", 2,
                "h1|fix: 修复 git push 被拒\nh2|feat: 新功能").GetAwaiter().GetResult();
            Check("诊断召回知识库", diag.Contains("知识库经验") && diag.Contains("push 被拒先 rebase"));
            Check("诊断召回 git 修复史", diag.Contains("历史修复") && diag.Contains("fix: 修复 git push 被拒"));
            var fixHits = KbIndex.MatchFixCommits("push 问题", "a|fix: 重试逻辑\nb|feat: 新功能\nc|refactor: 重构", 5);
            Check("MatchFixCommits 只取 fix/refactor", fixHits.Count == 2 && fixHits.All(h => !h.Subject.StartsWith("feat")));

            // ② 技能画像
            var (types, total) = KbIndex.ParseGitLog("fix: a\nfeat: b\nfix: c\ndocs: d");
            Check("ParseGitLog 前缀计数", total == 4 && types.GetValueOrDefault("fix") == 2 && types.GetValueOrDefault("feat") == 1);
            var profile = KbIndex.ProfileStats(gitLogOverride: "fix: 修 bug\nfeat: 加功能\nfix: 再修\nrefactor: 重构");
            Check("画像 KB 分类分布", profile.KbKinds.Any(k => k.Kind == "mistake"));
            Check("画像 git 计数", profile.TotalCommits == 4 && profile.GitCommitTypes.GetValueOrDefault("fix") == 2);
            Check("画像渲染含标题", KbIndex.FormatProfile(profile).Contains("技能画像"));

            // ③ 教学模式（SystemPrompt 教学块）
            var savedTeach = Config.Instance.TeachModeEnabled;
            Config.Instance.TeachModeEnabled = true;
            try
            {
                var teachPrompt = SystemPrompt.Generate(WayCoder.Tools.ToolRegistry.AllTools);
                Check("教学模式 SystemPrompt 含教学块", teachPrompt.Contains("<teach_mode>") && teachPrompt.Contains("测验"));
            }
            finally { Config.Instance.TeachModeEnabled = savedTeach; }

            // ④ 会话复盘（纯解析）
            var lessons = KbIndex.ParseLessons(
                "{\"lessons\":[{\"kind\":\"mistake\",\"description\":\"不要裸 force push\",\"content\":\"先 rebase\",\"tags\":[\"git\"]},{\"kind\":\"weird\",\"description\":\"未知类型\",\"content\":\"c\"}]}");
            Check("ParseLessons 提炼条目", lessons.Count == 2 && lessons[0].Kind == "mistake" && lessons[1].Kind == "bugfix");

            // kb 工具 diagnose
            var kbDiag = new WayCoder.Tools.KbTool().ExecuteAsync(new() { ["action"] = "diagnose", ["query"] = "push 被拒" }).GetAwaiter().GetResult();
            Check("kb 工具 diagnose 召回", kbDiag.Contains("知识库经验") || kbDiag.Contains("历史修复"));
        }
        finally
        {
            foreach (var n in new[] { "git-force-push", "fix-window-size", "habit-subagent", "gap-aot" })
                KbIndex.DeleteEntry(n);
        }
        Console.WriteLine();

        // ---- frontmatter 解析边界（未闭合不污染正文）----
        Section("[StructuredMemory.ParseFrontmatter]");
        var pfOk = StructuredMemory.ParseFrontmatter("---\nname: x\ndescription: d\n---\n正文内容");
        Check("frontmatter 正常解析 name", pfOk.Frontmatter.GetValueOrDefault("name") == "x");
        Check("frontmatter 正常解析正文", pfOk.Body == "正文内容");
        var pfUnclosed = StructuredMemory.ParseFrontmatter("---\nname: x\ndescription: d\n正文内容\n带:冒号的行");
        Check("frontmatter 未闭合 name 为空", !pfUnclosed.Frontmatter.ContainsKey("name"));
        Check("frontmatter 未闭合正文=全文", pfUnclosed.Body.StartsWith("---") && pfUnclosed.Body.Contains("正文内容"));
        Console.WriteLine();

        // ---- Watch 模式 AI 注释提取（块注释后跟行注释不再吞）----
        Section("[WatchMode]");
        var wc = WatchMode.ExtractAiComments("/* block */ // AI! 块后行注释", "test.cs");
        Check("Watch 块注释后行注释", wc.Count == 1 && wc[0] == "块后行注释");
        var wc2 = WatchMode.ExtractAiComments("/* AI! 多行\n第二行 */ // AI! 块后", "test.cs");
        Check("Watch 多行块注释结束后行注释", wc2.Count == 2 && wc2[0] == "多行" && wc2[1] == "块后");
        var wc3 = WatchMode.ExtractAiComments("// AI! 普通行注释", "test.cs");
        Check("Watch 普通行注释", wc3.Count == 1 && wc3[0] == "普通行注释");
        Console.WriteLine();

        // ---- 项目检测忽略目录：按路径段精确匹配（非前缀）----
        Section("[ProjectContext.IsIgnoredPath]");
        Check("忽略 .git 段", ProjectContext.IsIgnoredPath(".git/config"));
        Check("忽略 node_modules 段", ProjectContext.IsIgnoredPath("a/node_modules/x.js"));
        Check("忽略 bin 段", ProjectContext.IsIgnoredPath("bin/out.dll"));
        Check("不误伤 .github", !ProjectContext.IsIgnoredPath(".github/workflows/ci.yml"));
        Check("不误伤 bin-tools", !ProjectContext.IsIgnoredPath("bin-tools/src.cs"));
        Check("不误伤 objc", !ProjectContext.IsIgnoredPath("objc/file.m"));
        Console.WriteLine();

        // ---- 后台任务 ----
        Section("[后台任务]");
        var bgId = BackgroundTaskManager.Start("echo bg_test", 5);
        Check("后台任务启动", bgId > 0);
        // 轮询等输出：echo 毫秒级完成，固定 Sleep(1500) 白白拖慢自测；最多轮询 5s 兜底
        var bgOutput = "";
        var bgDeadline = System.Environment.TickCount64 + 5000;
        while (!bgOutput.Contains("bg_test") && System.Environment.TickCount64 < bgDeadline)
        {
            bgOutput = BackgroundTaskManager.GetOutput(bgId);
            if (!bgOutput.Contains("bg_test")) System.Threading.Thread.Sleep(50);
        }
        Check("后台任务输出", bgOutput.Contains("bg_test"));
        var bgList = BackgroundTaskManager.ListTasks();
        Check("后台任务列表", bgList.Contains("completed") || bgList.Contains("running"));
        BackgroundTaskManager.Cleanup();
        Console.WriteLine();

        // ---- LSP 工具 ----
        Section("[LSP]");
        ITool lspTool = new LspTool();
        Check("lsp 工具名称正确", lspTool.Name == "lsp");
        Check("lsp 有 definition/references/hover/symbols", lspTool.Description.Contains("定义"));
        Check("lsp 支持 14 种语言", LspTool.SupportedServers.Count == 14);
        Check("lsp 含 cpp(clangd)", LspTool.SupportedServers.ContainsKey("cpp") && LspTool.SupportedServers["cpp"].Command == "clangd");
        Check("lsp 含 java(jdtls)", LspTool.SupportedServers.ContainsKey("java") && LspTool.SupportedServers["java"].Command == "jdtls");
        Check("lsp 含 kotlin", LspTool.SupportedServers.ContainsKey("kotlin") && LspTool.SupportedServers["kotlin"].Command == "kotlin-language-server");
        Check("lsp 含 ruby(solargraph)", LspTool.SupportedServers.ContainsKey("ruby") && LspTool.SupportedServers["ruby"].Command == "solargraph");
        Check("lsp 含 php(intelephense)", LspTool.SupportedServers.ContainsKey("php") && LspTool.SupportedServers["php"].Command == "intelephense");
        Check("lsp 含 lua", LspTool.SupportedServers.ContainsKey("lua") && LspTool.SupportedServers["lua"].Command == "lua-language-server");
        Check("lsp 含 bash", LspTool.SupportedServers.ContainsKey("bash") && LspTool.SupportedServers["bash"].Command == "bash-language-server");
        Check("lsp 含 swift", LspTool.SupportedServers.ContainsKey("swift") && LspTool.SupportedServers["swift"].Command == "sourcekit-lsp");
        Check("lsp 含 zig", LspTool.SupportedServers.ContainsKey("zig") && LspTool.SupportedServers["zig"].Command == "zls");
        // 会话缓存复用（离线可测部分：项目根查找 + 会话清理）
        var lspRoot = LspTool.FindProjectRoot(Path.Combine(AppContext.BaseDirectory, "WayCoder.dll"));
        Check("lsp 向上查找项目根", !string.IsNullOrEmpty(lspRoot) && lspRoot.EndsWith("WayCoder"));
        Check("lsp 无标记路径不崩溃",
            !string.IsNullOrEmpty(LspTool.FindProjectRoot(Path.Combine(Path.GetTempPath(), "no_marker_dir", "x.py"))));
        LspTool.ShutdownAllSessions(); // 空态清理不崩溃
        Check("lsp 会话清理不崩溃", true);
        Console.WriteLine();

        // ---- 流式工具执行 (编译期已验证 onToolCall 参数) ----
        // ChatAsync 方法签名已通过 LLM.cs 编译验证，此处确认 LLM 实例可创建
        Section("[LLM 流式]");
        try
        {
            var llmTest = new LLM("test", "sk-test");
            Check("LLM onToolCall 支持 (编译期)", true);
        }
        catch { Fail("LLM onToolCall 支持 (编译期)"); }

        // 端点解析：BaseUrl 约定不含 /v1；误传 /v1 或尾部斜杠都被归一化，避免 /v1/v1
        var llmEp1 = new LLM("m", "k", "http://localhost:1234");
        Check("LLM 端点: 无 /v1", llmEp1.Endpoint == "http://localhost:1234/v1/chat/completions");
        var llmEp2 = new LLM("m", "k", "http://localhost:1234/v1");
        Check("LLM 端点: 误传 /v1 不重复", llmEp2.Endpoint == "http://localhost:1234/v1/chat/completions");
        var llmEp3 = new LLM("m", "k", "http://localhost:1234/");
        Check("LLM 端点: 尾部斜杠", llmEp3.Endpoint == "http://localhost:1234/v1/chat/completions");
        Console.WriteLine();

        // ---- LLM 定价 ----
        Section("[LLM]");
        var llm = new LLM("deepseek-v4-flash", "sk-test");
        // TotalPromptTokens/TotalCompletionTokens 为 getter-only（Interlocked 累加），用 AddUsage 注入 token 数
        llm.AddUsage(1_000_000, 500_000);
        Check("deepseek-v4-flash 成本 ≈ 0.28", Math.Abs(llm.EstimatedCost!.Value - 0.28) < 0.01);

        var llm2 = new LLM("unknown-model", "sk-test");
        Check("未知模型成本为 null", llm2.EstimatedCost == null);

        // 任务级花费追踪
        var llmTask = new LLM("deepseek-v4-flash", "sk-test");
        llmTask.AddUsage(500_000, 250_000);
        llmTask.SnapshotTaskCost();
        // 模拟任务产生了 200K 输入 + 100K 输出
        llmTask.AddUsage(200_000, 100_000);
        Check("任务 Token 增量 = 200K+100K", llmTask.TaskPromptTokens == 200_000 && llmTask.TaskCompletionTokens == 100_000);
        Check("任务花费 ≈ $0.056", llmTask.TaskCost.HasValue && Math.Abs(llmTask.TaskCost!.Value - 0.056) < 0.01);
        // 未知模型 TaskCost 为 null
        var llmUnknown = new LLM("unknown-model", "sk-test");
        llmUnknown.AddUsage(100_000, 50_000);
        llmUnknown.SnapshotTaskCost();
        llmUnknown.AddUsage(100_000, 50_000);
        Check("未知模型任务花费为 null", llmUnknown.TaskCost == null);
        Check("未知模型任务 Token 增量正确", llmUnknown.TaskPromptTokens == 100_000 && llmUnknown.TaskCompletionTokens == 50_000);

        // ---- LLM 重试/超时配置 ----
        Check("默认重试次数 = 5", Config.Instance.LlmMaxRetries == 5);
        // 全新实例 = 代码默认值，不受 .env 覆盖影响（.env 可合法地把超时调到 600 等）
        var defaultConfig = new Config();
        Check("默认 HTTP 超时 = 300", defaultConfig.LlmHttpTimeoutSec == 300);
        Check("默认连接超时 = 300", defaultConfig.LlmConnectionTimeoutSec == 300);

        // ---- LLM 渐进超时倍率 ----
        Check("TimeoutMultipliers[0] = 1.0", Math.Abs(LLM.TimeoutMultipliers[0] - 1.0) < 0.01);
        Check("TimeoutMultipliers[3] = 3.0", Math.Abs(LLM.TimeoutMultipliers[3] - 3.0) < 0.01);
        Check("TimeoutMultipliers[6] = 8.0", Math.Abs(LLM.TimeoutMultipliers[6] - 8.0) < 0.01);
        Check("GetTimeoutMultiplier(0) = 1.0", Math.Abs(LLM.GetTimeoutMultiplier(0) - 1.0) < 0.01);
        Check("GetTimeoutMultiplier(2) = 2.0", Math.Abs(LLM.GetTimeoutMultiplier(2) - 2.0) < 0.01);
        Check("GetTimeoutMultiplier(7) = 9.0", Math.Abs(LLM.GetTimeoutMultiplier(7) - 9.0) < 0.01);
        Check("GetTimeoutMultiplier(10) = 12.0", Math.Abs(LLM.GetTimeoutMultiplier(10) - 12.0) < 0.01);
        // 第 1 次尝试 (attempt=0): 600*1.0=600s, 第 5 次 (attempt=4): 600*4.0=2400s
        var t1 = 600 * LLM.GetTimeoutMultiplier(0);
        var t5 = 600 * LLM.GetTimeoutMultiplier(4);
        Check($"第1次超时={t1:F0}s", Math.Abs(t1 - 600) < 1);
        Check($"第5次超时={t5:F0}s", Math.Abs(t5 - 2400) < 1);
        Console.WriteLine();

        // ---- 系统提示词 ----
        Section("[系统提示词]");
        // 完整版基线：显式指定 Off 生成，不受 .env 的 WAYCODER_ECONOMY 影响
        // （省钱模式换的是另一套精简提示词，拿它跑完整版断言会集体假失败）
        var prompt = PromptWithMode(EconomyMode.Off);
        Check("包含 read_file", prompt.Contains("read_file"));
        Check("包含 edit_file", prompt.Contains("edit_file"));
        Check("包含当前目录", prompt.Contains(Directory.GetCurrentDirectory()));
        // 新结构化区块检查
        Check("包含 critical_rules", prompt.Contains("<critical_rules>"));
        Check("包含 workflow", prompt.Contains("<workflow>"));
        Check("包含 editing_files", prompt.Contains("<editing_files>"));
        Check("包含 exact_matching", prompt.Contains("<exact_matching>"));
        Check("包含 task_completion", prompt.Contains("<task_completion>"));
        Check("包含 error_handling", prompt.Contains("<error_handling>"));
        Check("包含 testing", prompt.Contains("<testing>"));
        Check("包含 code_conventions", prompt.Contains("<code_conventions>"));
        Check("包含 15 条规则", prompt.Contains("15."));
        // 新增 systematic_phases 章节
        Check("包含 systematic_phases", prompt.Contains("<systematic_phases>"));
        Check("包含调查阶段", prompt.Contains("调查"));
        Check("包含分析阶段", prompt.Contains("分析"));
        Check("包含规划阶段", prompt.Contains("规划"));
        Check("包含拆分阶段", prompt.Contains("拆分"));
        Check("包含分工阶段", prompt.Contains("分工"));
        Check("包含执行阶段", prompt.Contains("执行"));
        Check("包含调试阶段", prompt.Contains("调试"));
        Check("包含审核阶段", prompt.Contains("审核"));
        Check("包含提交阶段", prompt.Contains("提交"));
        Check("包含总结阶段", prompt.Contains("总结"));
        Check("包含流水线说明", prompt.Contains("内部流水线"));

        // 当前生效档位（.env / 环境变量 WAYCODER_ECONOMY）实际会喂给模型什么：
        // 未开=完整版；on=精简版（留硬骨架、砍软性区块）；extreme=极致版（仅工具名+核心规则）
        var liveMode = Config.Instance.EconomyMode;
        var live = SystemPrompt.Generate(ToolRegistry.AllTools);
        Check($"生效档位[{liveMode}]: 含 read_file/edit_file",
            live.Contains("read_file") && live.Contains("edit_file"));
        switch (liveMode)
        {
            case EconomyMode.Extreme:
                Check("生效档位[Extreme]: 极简标识", live.Contains("极简模式"));
                Check("生效档位[Extreme]: 砍掉 10 阶段流水线", !live.Contains("<systematic_phases>"));
                Check("生效档位[Extreme]: 比精简版更短", live.Length < PromptWithMode(EconomyMode.On).Length);
                break;
            case EconomyMode.On:
                Check("生效档位[On]: 含工作目录", live.Contains(Directory.GetCurrentDirectory()));
                Check("生效档位[On]: 含先读后改规则", live.Contains("先读后改"));
                Check("生效档位[On]: 砍掉 10 阶段流水线", !live.Contains("<systematic_phases>"));
                Check("生效档位[On]: 比完整版更短", live.Length < prompt.Length);
                break;
            default: // Off / Auto 均用完整提示词（Auto 只动压缩阈值，不换提示词）
                Check($"生效档位[{liveMode}]: 用完整提示词", live.Contains("<systematic_phases>"));
                Check($"生效档位[{liveMode}]: 含 15 条规则", live.Contains("15."));
                break;
        }
        Console.WriteLine();
        Section("[Agent]");
        var agent = new Agent(new LLM("test", "sk-test"));
        agent.Messages.Add(JNode.Object().Set("role", "user").Set("content", "x"));
        agent.Reset();
        Check("Reset 清空消息", agent.Messages.Count == 0);

        var readTool = ToolRegistry.GetTool("read_file")!;
        var agent2 = new Agent(new LLM("test", "sk-test"), [readTool!]);
        Check("工具范围隔离", agent2.ToolByName.Count == 1 && agent2.ToolByName.ContainsKey("read_file"));

        // 优雅暂停标志（Ctrl+Z / /pause）
        var pauseAgent = new Agent(new LLM("test", "sk-test"));
        Check("PauseRequested 默认 false", pauseAgent.PauseRequested == false);
        pauseAgent.PauseRequested = true;
        Check("PauseRequested 可置位", pauseAgent.PauseRequested == true);
        pauseAgent.PauseRequested = false;
        Check("PauseRequested 可复位", pauseAgent.PauseRequested == false);
        Console.WriteLine();

        // ---- JsonHelper ----
        Section("[JSON 辅助]");
        var json = JsonHelper.SerializeArgs(new() { ["k"] = "v", ["n"] = 42 });
        Check("序列化包含键值", json.Contains("\"k\":\"v\"") && json.Contains("\"n\":42"));

        // P1-3 回归：集合/字典/JsonNode 递归序列化（而非 ToString 回显 System.Collections...）
        var listJson = JsonHelper.SerializeArgs(new() { ["tasks"] = new List<object?> { "a", "b", 3 } });
        Check("List 序列化为 JSON 数组", listJson == "{\"tasks\":[\"a\",\"b\",3]}");
        Check("List 不含 ToString 泄漏", !listJson.Contains("System.Collections"));

        var dictJson = JsonHelper.SerializeArgs(new() { ["opts"] = new Dictionary<string, object?> { ["x"] = 1, ["y"] = "z" } });
        Check("字典序列化为 JSON 对象", dictJson == "{\"opts\":{\"x\":1,\"y\":\"z\"}}");
        Check("字典不含 ToString 泄漏", !dictJson.Contains("System.Collections"));

        var nodeJson = JsonHelper.SerializeArgs(new()
        {
            ["arr"] = JNode.Array().Add("m").Add("n"),
            ["obj"] = JNode.Object().Set("deep", true),
        });
        Check("JsonArray/JsonObject 走 ToJsonString", nodeJson.Contains("\"arr\":[\"m\",\"n\"]") && nodeJson.Contains("\"deep\":true"));

        // P2 回归：JsonHelper 统一委托 Json.SerializeValue —— NaN/Inf → null、控制字符完整转义
        var nanJson = JsonHelper.SerializeArgs(new() { ["d"] = double.NaN, ["inf"] = double.PositiveInfinity, ["ninf"] = double.NegativeInfinity });
        Check("NaN/Inf 序列化为 null", nanJson.Contains("\"d\":null") && nanJson.Contains("\"inf\":null") && nanJson.Contains("\"ninf\":null"));
        var ctrlJson = JsonHelper.SerializeArgs(new() { ["s"] = "a\nb\tcd\bd\fe" });
        Check("控制字符完整转义", ctrlJson.Contains("\\n") && ctrlJson.Contains("\\t") && ctrlJson.Contains("\\u0001") && ctrlJson.Contains("\\b") && ctrlJson.Contains("\\f"));
        var ctrlRound = Json.Parse(ctrlJson);
        Check("控制字符 JSON 往返可解析", ctrlRound != null && ctrlRound!["s"]?.AsString() == "a\nb\tcd\bd\fe");
        // 反斜杠/双引号往返（此前 CheckpointManager 手写 Replace 会漏转义，统一后由 Json.Quote 兜底）
        var quoteJson = JsonHelper.SerializeArgs(new() { ["s"] = "he said \"hi\" \\ done" });
        Check("引号/反斜杠往返一致", Json.Parse(quoteJson)!["s"]?.AsString() == "he said \"hi\" \\ done");
        Console.WriteLine();

        // ================================================================
        //  以下为 v0.6.0+ 新增功能的测试
        // ================================================================

        // ---- 权限系统 扩展 ----
        Section("[权限系统 扩展]");
        // Ask 模式: 危险工具需要确认（非交互环境会失败，测边界逻辑）
        PermissionManager.SetMode("ask");
        PermissionManager.Reset();
        Check("SetMode ask → CurrentMode == Ask",
            PermissionManager.CurrentMode == PermissionManager.Mode.Ask);
        PermissionManager.SetMode("auto");
        Check("SetMode auto → CurrentMode == Auto",
            PermissionManager.CurrentMode == PermissionManager.Mode.Auto);
        PermissionManager.SetMode("yolo");
        Check("SetMode yolo → CurrentMode == Yolo",
            PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        PermissionManager.SetMode("god");
        Check("SetMode god → CurrentMode == Yolo",
            PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        PermissionManager.SetMode("smart");
        Check("SetMode smart → CurrentMode == SmartAuto",
            PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto);
        PermissionManager.SetMode("smartauto");
        Check("SetMode smartauto → CurrentMode == SmartAuto",
            PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto);
        PermissionManager.SetMode("auto");
        Check("SetMode auto → CurrentMode == Auto",
            PermissionManager.CurrentMode == PermissionManager.Mode.Auto);
        PermissionManager.SetMode("unknown");
        Check("SetMode unknown → CurrentMode == Ask",
            PermissionManager.CurrentMode == PermissionManager.Mode.Ask);

        // 非危险工具直接放行
        var safeCheck = PermissionManager.CheckAsync("read_file", new() { ["file_path"] = "/tmp/x" }).Result;
        Check("read_file (非危险) 直接放行", safeCheck == true);
        var safeCheck2 = PermissionManager.CheckAsync("glob", new() { ["pattern"] = "*.cs" }).Result;
        Check("glob (非危险) 直接放行", safeCheck2 == true);
        var safeCheck3 = PermissionManager.CheckAsync("grep", new() { ["pattern"] = "x" }).Result;
        Check("grep (非危险) 直接放行", safeCheck3 == true);

        // YOLO 模式：危险工具也放行
        PermissionManager.SetMode("yolo");
        var yoloDanger = PermissionManager.CheckAsync("bash", new() { ["command"] = "echo hi" }).Result;
        Check("YOLO 模式 bash 放行", yoloDanger == true);

        // Reset 清空 Auto 记录
        PermissionManager.SetMode("ask");
        PermissionManager.Reset();
        Check("Reset 后 Ask 模式恢复", PermissionManager.CurrentMode == PermissionManager.Mode.Ask);

        // ---- AutoMode 智能分类器 ----
        Section("[AutoMode 智能分类器]");

        // 风险分级
        Check("read_file → Safe", AutoModeClassifier.Classify("read_file") == AutoModeClassifier.RiskLevel.Safe);
        Check("ls → Safe", AutoModeClassifier.Classify("ls") == AutoModeClassifier.RiskLevel.Safe);
        Check("grep → Safe", AutoModeClassifier.Classify("grep") == AutoModeClassifier.RiskLevel.Safe);
        Check("glob → Safe", AutoModeClassifier.Classify("glob") == AutoModeClassifier.RiskLevel.Safe);
        Check("stat → Safe", AutoModeClassifier.Classify("stat") == AutoModeClassifier.RiskLevel.Safe);
        Check("diff → Safe", AutoModeClassifier.Classify("diff") == AutoModeClassifier.RiskLevel.Safe);
        Check("tree → Safe", AutoModeClassifier.Classify("tree") == AutoModeClassifier.RiskLevel.Safe);
        Check("fetch → Safe", AutoModeClassifier.Classify("fetch") == AutoModeClassifier.RiskLevel.Safe);
        Check("lsp → Safe", AutoModeClassifier.Classify("lsp") == AutoModeClassifier.RiskLevel.Safe);

        Check("write_file → Cautious", AutoModeClassifier.Classify("write_file") == AutoModeClassifier.RiskLevel.Cautious);
        Check("edit_file → Cautious", AutoModeClassifier.Classify("edit_file") == AutoModeClassifier.RiskLevel.Cautious);
        Check("mkdir → Cautious", AutoModeClassifier.Classify("mkdir") == AutoModeClassifier.RiskLevel.Cautious);
        Check("cp → Cautious", AutoModeClassifier.Classify("cp") == AutoModeClassifier.RiskLevel.Cautious);
        Check("mv → Cautious", AutoModeClassifier.Classify("mv") == AutoModeClassifier.RiskLevel.Cautious);

        Check("rm → Dangerous", AutoModeClassifier.Classify("rm") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("bash → Dangerous", AutoModeClassifier.Classify("bash") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("git → Dangerous", AutoModeClassifier.Classify("git") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("kill → Dangerous", AutoModeClassifier.Classify("kill") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("agent → Dangerous", AutoModeClassifier.Classify("agent") == AutoModeClassifier.RiskLevel.Dangerous);

        // 未知工具默认 Dangerous
        Check("unknown_tool → Dangerous", AutoModeClassifier.Classify("unknown_tool") == AutoModeClassifier.RiskLevel.Dangerous);

        // 单一事实源：权限确认名单与智能分类共用 ToolSafetyRegistry
        Check("ToolSafety: read_file 无需确认", !ToolSafetyRegistry.RequiresConfirmation("read_file"));
        Check("ToolSafety: git 需确认", ToolSafetyRegistry.RequiresConfirmation("git"));
        Check("ToolSafety: job_kill 需确认", ToolSafetyRegistry.RequiresConfirmation("job_kill"));
        Check("ToolSafety: sqlite 需确认", ToolSafetyRegistry.RequiresConfirmation("sqlite"));
        Check("ToolSafety: find_replace 归为 Cautious", AutoModeClassifier.Classify("find_replace") == AutoModeClassifier.RiskLevel.Cautious);
        Check("ToolSafety: test 归为 Dangerous", AutoModeClassifier.Classify("test") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("ToolSafety: PermissionManager 与注册表一致", PermissionManager.IsDangerousTool("git"));

        // 连续阻止计数
        AutoModeClassifier.Reset();
        Check("初始连续阻止=0", AutoModeClassifier.ConsecutiveDangerousBlocks == 0);

        AutoModeClassifier.RecordDangerousBlock();
        Check("阻止1次=1", AutoModeClassifier.ConsecutiveDangerousBlocks == 1);
        AutoModeClassifier.RecordDangerousBlock();
        Check("阻止2次=2", AutoModeClassifier.ConsecutiveDangerousBlocks == 2);

        // 允许后重置
        AutoModeClassifier.RecordDangerousAllow();
        Check("允许→计数归零", AutoModeClassifier.ConsecutiveDangerousBlocks == 0);

        // 阈值触发
        bool fallbackTriggered = false;
        AutoModeClassifier.FallbackToManualTriggered += () => { fallbackTriggered = true; };
        AutoModeClassifier.RecordDangerousBlock(); // 1
        AutoModeClassifier.RecordDangerousBlock(); // 2
        AutoModeClassifier.RecordDangerousBlock(); // 3 → 触发
        Check("连续3次阻止→触发退回事件", fallbackTriggered);
        Check("触发后计数归零", AutoModeClassifier.ConsecutiveDangerousBlocks == 0);

        // SmartAuto 模式下 Safe 工具放行
        PermissionManager.SetMode("smartauto");
        var smartSafe = PermissionManager.CheckAsync("read_file", new() { ["file_path"] = "/tmp/x" }).Result;
        Check("SmartAuto: read_file 自动放行", smartSafe == true);
        var smartSafe2 = PermissionManager.CheckAsync("ls", new() { ["path"] = "." }).Result;
        Check("SmartAuto: ls 自动放行", smartSafe2 == true);

        // 恢复默认
        PermissionManager.SetMode("ask");
        AutoModeClassifier.Reset();

        // ---- 工作模式管理器 ----
        Section("[工作模式管理器]");

        // 默认模式
        Check("默认模式=Build", WorkModeManager.CurrentMode == WorkMode.Build);

        // 格式输出
        Check("Format(Build) 含🔨", WorkModeManager.Format(WorkMode.Build).Contains("🔨"));
        Check("Format(Plan) 含🧠", WorkModeManager.Format(WorkMode.Plan).Contains("🧠"));
        Check("Format(Chat) 含💬", WorkModeManager.Format(WorkMode.Chat).Contains("💬"));

        // 工具约束：Plan 模式（只读白名单 + bash 命令门控）
        Check("Plan: write_file 阻止", WorkModeManager.CheckToolAllowed("write_file", WorkMode.Plan) != null);
        Check("Plan: edit_file 阻止", WorkModeManager.CheckToolAllowed("edit_file", WorkMode.Plan) != null);
        Check("Plan: rm 阻止", WorkModeManager.CheckToolAllowed("rm", WorkMode.Plan) != null);
        Check("Plan: git 阻止", WorkModeManager.CheckToolAllowed("git", WorkMode.Plan) != null);
        Check("Plan: agent 阻止", WorkModeManager.CheckToolAllowed("agent", WorkMode.Plan) != null);
        Check("Plan: sqlite 阻止", WorkModeManager.CheckToolAllowed("sqlite", WorkMode.Plan) != null);
        Check("Plan: read_file 允许", WorkModeManager.CheckToolAllowed("read_file", WorkMode.Plan) == null);
        Check("Plan: grep 允许", WorkModeManager.CheckToolAllowed("grep", WorkMode.Plan) == null);
        Check("Plan: lsp 允许", WorkModeManager.CheckToolAllowed("lsp", WorkMode.Plan) == null);
        Check("Plan: doc 允许", WorkModeManager.CheckToolAllowed("doc", WorkMode.Plan) == null);
        // Plan 的 bash 只读命令门控（fail-closed：无参数默认阻止）
        Check("Plan: bash 无参数 阻止", WorkModeManager.CheckToolAllowed("bash", WorkMode.Plan) != null);
        Check("Plan: bash git status 允许", WorkModeManager.CheckToolAllowed("bash", WorkMode.Plan, new Dictionary<string, object?> { ["command"] = "git status" }) == null);
        Check("Plan: bash rm 阻止", WorkModeManager.CheckToolAllowed("bash", WorkMode.Plan, new Dictionary<string, object?> { ["command"] = "rm -rf x" }) != null);

        // 工具约束：Chat 模式全禁（纯聊天 0 工具）
        Check("Chat: read_file 阻止", WorkModeManager.CheckToolAllowed("read_file", WorkMode.Chat) != null);
        Check("Chat: bash 阻止", WorkModeManager.CheckToolAllowed("bash", WorkMode.Chat) != null);

        // 工具约束：Build 模式全允许
        Check("Build: write_file 允许", WorkModeManager.CheckToolAllowed("write_file", WorkMode.Build) == null);
        Check("Build: bash 允许", WorkModeManager.CheckToolAllowed("bash", WorkMode.Build) == null);
        Check("Build: rm 允许", WorkModeManager.CheckToolAllowed("rm", WorkMode.Build) == null);

        // 模式切换
        WorkModeManager.SetMode(WorkMode.Plan);
        Check("SetMode→Plan", WorkModeManager.CurrentMode == WorkMode.Plan);
        WorkModeManager.SetMode(WorkMode.Chat);
        Check("SetMode→Chat", WorkModeManager.CurrentMode == WorkMode.Chat);

        // 循环切换 Build→Plan→Chat→Build
        WorkModeManager.SetMode(WorkMode.Build);
        var m1 = WorkModeManager.CycleNext();
        Check("CycleNext: Build→Plan", m1 == WorkMode.Plan);
        var m2 = WorkModeManager.CycleNext();
        Check("CycleNext: Plan→Chat", m2 == WorkMode.Chat);
        var m3 = WorkModeManager.CycleNext();
        Check("CycleNext: Chat→Build", m3 == WorkMode.Build);

        // ModeChanged 事件
        bool eventFired = false;
        WorkMode received = WorkMode.Build;
        Action<WorkMode> handler = m => { eventFired = true; received = m; };
        WorkModeManager.ModeChanged += handler;
        WorkModeManager.SetMode(WorkMode.Plan);
        Check("ModeChanged 事件触发", eventFired && received == WorkMode.Plan);
        // 清理：移除 handler（AOT 不支持 -= lambda）
        WorkModeManager.SetMode(WorkMode.Build);

        // System Prompt 生成
        var planPrompt = WorkModeManager.GetModePrompt(WorkMode.Plan);
        Check("Plan Prompt 含计划模式", planPrompt.Contains("计划模式"));
        var chatPrompt = WorkModeManager.GetModePrompt(WorkMode.Chat);
        Check("Chat Prompt 为空", string.IsNullOrEmpty(chatPrompt));
        var buildPrompt = WorkModeManager.GetModePrompt(WorkMode.Build);
        Check("Build Prompt 为空", string.IsNullOrEmpty(buildPrompt));

        // 计划审批门（Plan 模式产出计划后弹出审批）—— 纯逻辑判定
        Check("审批门: Plan+计划文本 触发", Agent.ShouldPromptPlanApproval(WorkMode.Plan, 200));
        Check("审批门: Plan+空文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Plan, 0));
        Check("审批门: Build+计划文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Build, 200));
        Check("审批门: Chat+计划文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Chat, 200));

        // 恢复默认
        WorkModeManager.SetMode(WorkMode.Build);

        // ---- 跨槽位消息传递 ----
        Section("[跨槽位消息]");

        // AgentSlot 初始化
        var testSlot = new AgentSlot();
        Check("新槽位 PendingMessages 为空", testSlot.PendingMessages.Count == 0);

        // 投递消息到非活跃槽位（无 activeScreen）→ 排队
        testSlot.DeliverMessage(0, "测试消息", null, 1);
        Check("Deliver 后 PendingMessages=1", testSlot.PendingMessages.Count == 1);
        Check("Pending 内容匹配", testSlot.PendingMessages[0].Message == "测试消息");
        Check("Pending 来源槽位", testSlot.PendingMessages[0].FromSlot == 0);

        // 投递多条消息
        testSlot.DeliverMessage(2, "第二条消息", null, 1);
        Check("Deliver 后 PendingMessages=2", testSlot.PendingMessages.Count == 2);

        // 刷新队列（没有真实 ChatScreen 时直接清空）
        testSlot.PendingMessages.Clear();
        Check("Clear 后 PendingMessages=0", testSlot.PendingMessages.Count == 0);

        // AgentSlot.Count 常量
        Check("AgentSlot.Count=10", AgentSlot.Count == 10);

        // ---- 槽位独立工作目录 ----
        Section("[槽位独立工作目录]");

        var cwdSlot = new AgentSlot();
        Check("新槽位 WorkingDirectory 默认 null（回退进程启动目录）", cwdSlot.WorkingDirectory == null);

        var slotA = Path.Combine(Path.GetTempPath(), "waycoder-slot-a");
        var slotB = Path.Combine(Path.GetTempPath(), "waycoder-slot-b");
        cwdSlot.WorkingDirectory = slotA;
        Check("WorkingDirectory 可设置", cwdSlot.WorkingDirectory == slotA);

        var cwdSlot2 = new AgentSlot { WorkingDirectory = slotB };
        Check("不同槽位工作目录互不影响", cwdSlot.WorkingDirectory != cwdSlot2.WorkingDirectory);
        Check("槽位 B 工作目录独立持久化", cwdSlot2.WorkingDirectory == slotB);

        Console.WriteLine();

        // ---- 会话管理 ----
        Section("[会话管理]");

        static List<JNode> MakeMsgs()
        {
            return [
                JNode.Object().Set("role", "user").Set("content", "你好"),
                JNode.Object().Set("role", "assistant").Set("content", "你好！有什么可以帮你的？"),
            ];
        }

        // 保存会话
        var sid = SessionManager.SaveSession(MakeMsgs(), "deepseek-v4-flash", "test-session-001");
        Check("保存会话返回 ID", sid == "test-session-001");

        // 加载会话
        var loaded = SessionManager.LoadSession("test-session-001");
        Check("加载会话成功", loaded != null);
        Check("加载消息数正确", loaded?.Messages.Count == 2);
        Check("加载模型正确", loaded?.Model == "deepseek-v4-flash");

        // 列出会话
        var list = SessionManager.ListSessions();
        Check("会话列表包含测试会话", list.Any(s => s.Id == "test-session-001"));

        // 会话 ID 净化 —— 路径穿越
        var safeId1 = SessionManager.SaveSession(MakeMsgs(), "gpt-5.5", "../../../etc/passwd");
        Check("路径穿越 ID 被净化", safeId1 != "../../../etc/passwd" && !safeId1.Contains(".."));

        // 会话 ID 净化 —— 特殊字符
        var safeId2 = SessionManager.SaveSession(MakeMsgs(), "gpt-5.5", "hello world!@#$");
        Check("特殊字符 ID 被净化", !safeId2.Contains(" ") && !safeId2.Contains("!") && !safeId2.Contains("@") && !safeId2.Contains("#"));

        // 空 sessionId 自动生成
        var autoId = SessionManager.SaveSession(MakeMsgs(), "gpt-5.5", null);
        Check("空 sessionId 自动生成", autoId.StartsWith("session_") && autoId.Length > 10);

        // 加载不存在的会话
        var notFound = SessionManager.LoadSession("nonexistent-session-xyz");
        Check("加载不存在会话返回 null", notFound == null);

        // 清理测试文件
        try
        {
            var sessionsDir = Global.GlobalConfigPath("sessions");
            foreach (var f in Directory.GetFiles(sessionsDir, "test-session-*"))
                File.Delete(f);
        }
        catch { }

        Console.WriteLine();

        // ---- Agent 工作区（F1-F10 槽位） ----
    }
}
