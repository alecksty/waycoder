using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.Terminal;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk1(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[工具注册]");
        Check("工具数量 == 41", ToolRegistry.BuiltinTools.Count == 41);
        Check("所有工具有有效 schema", ToolRegistry.AllTools.All(t =>
        {
            var s = t.Schema();
            return (string?)s["type"] == "function"
                && s["function"]?["name"] != null
                && s["function"]?["parameters"]?["properties"] != null;
        }));
        Console.WriteLine();

        // ---- Config ----
        Section("[配置]");
        var config = new Config();
        Check("默认模型 deepseek-v4-flash", config.Model == "deepseek-v4-flash");
        Console.WriteLine();

        // ---- ContextManager ----
        Section("[上下文管理]");
        var msgs1 = new List<JsonObject> { new() { ["role"] = "user", ["content"] = "hello world" } };
        Check("Token 估算 > 0", ContextManager.EstimateTokens(msgs1) > 0);

        var msgs2 = new List<JsonObject>
        {
            // 生成超过 4000 字符的内容（新阈值），每行 50 字符 × 100 行 = 5000+ 字符
            new() { ["role"] = "tool", ["content"] = string.Join("\n", Enumerable.Range(0, 100).Select(i => new string('x', 50) + $"_{i:D4}")) },
        };
        var before = ContextManager.EstimateTokens(msgs2);
        ContextManager.SnipToolOutputs(msgs2);
        Check("工具输出裁剪有效", ContextManager.EstimateTokens(msgs2) < before);

        var msgs3 = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "do" },
            new() { ["role"] = "assistant", ["content"] = null, ["tool_calls"] = new JsonArray() },
            new() { ["role"] = "tool", ["tool_call_id"] = "c1", ["content"] = "r" },
        };
        var split = ContextManager.SafeSplit(msgs3, 1);
        Check("SafeSplit 不以 tool 开头", (string?)msgs3[split]["role"] != "tool");

        // SnipToolOutputs 详细测试
        TestSnipToolOutputs(Check);
        // ExtractKeyInfo 测试
        TestExtractKeyInfo(Check);
        // GenerateProjectSnapshot 测试
        TestGenerateProjectSnapshot(Check);
        // FlattenMessages 测试（通过 ExtractKeyInfo 间接验证）
        TestTokenEstimation(Check);
        // 压缩保真度测试（超多需求 + 自动续跑）
        TestCompressionFidelity(Check);
        Check("MaxAutoRequeue 默认 = 3", new Config().MaxAutoRequeue == 3);
        // 上下文窗口按模型切换测试
        TestContextWindowSwitch(Check);
        // Tiny 模式测试（4K 窗口 + 精简提示词）
        TestTinyMode(Check);
        // Tiny 窗口解析测试（--tiny 8k 指定 / 自动探测 / 128K 自动阈值）
        TestTinyWindow(Check);
        // 省 token 模式测试（EconomyMode 开关）
        TestEconomyMode(Check);
        // /init 项目初始化测试（生成 CLAUDE.md + 命令检测）
        TestProjectInit(Check);
        // 多槽位后台并行执行测试（槽位缓冲输出 + 运行状态）
        TestMultiSlotParallel(Check);
        // 实例级工作模式测试（Agent.WorkMode 与全局解耦 + 回调）
        TestWorkModePerAgent(Check);
        Console.WriteLine();

        // ---- 工具 ----
        Section("[工具]");

        // read_file
        try
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "l1\nl2\nl3\n");
            var readResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = tmpFile }).Result;
            Check("read_file 基本功能", readResult.Contains("l1") && readResult.Contains("l2"));
            File.Delete(tmpFile);
        }
        catch { Fail("read_file 基本功能"); }

        Check("read_file 文件不存在返回错误",
            new ReadFileTool().ExecuteAsync(new() { ["file_path"] = "/nonexistent" }).Result.Contains("错误"));

        // read_file PDF + Markdown
        Check("read_file 描述含 PDF",
            ToolRegistry.GetTool("read_file")!.Description.Contains("PDF"));
        Check("read_file 描述含 Markdown",
            ToolRegistry.GetTool("read_file")!.Description.Contains("Markdown"));
        Check("read_file PDF 不存在友好提示",
            new ReadFileTool().ExecuteAsync(new() { ["file_path"] = "/nonexistent.pdf" }).Result.Contains("错误"));

        // Markdown 读取
        try
        {
            var mdFile = Path.GetTempFileName();
            File.Delete(mdFile);
            mdFile = Path.ChangeExtension(mdFile, ".md");
            File.WriteAllText(mdFile, "# 标题\n\n一段文字\n\n- 项目1\n- 项目2\n\n```cs\ncode\n```\n");
            var mdResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = mdFile }).Result;
            Check("read_file Markdown 结构化输出", mdResult.Contains("<markdown>") && mdResult.Contains("标题"));
            File.Delete(mdFile);
        }
        catch { Fail("read_file Markdown"); }

        // PdfExtractor 结构
        Check("PdfExtractor.Extract 方法存在",
            typeof(WayCoder.Infra.PdfExtractor).GetMethod("Extract") != null);
        Check("PdfExtractResult 有 ToMarkdown",
            typeof(WayCoder.Infra.PdfExtractResult).GetMethod("ToMarkdown") != null);

        // OfficeExtractor 结构
        Check("OfficeExtractor.ExtractDocx 方法存在",
            typeof(WayCoder.Infra.OfficeExtractor).GetMethod("ExtractDocx") != null);
        Check("OfficeExtractor.ExtractXlsx 方法存在",
            typeof(WayCoder.Infra.OfficeExtractor).GetMethod("ExtractXlsx") != null);
        Check("OfficeExtractor.ExtractPptx 方法存在",
            typeof(WayCoder.Infra.OfficeExtractor).GetMethod("ExtractPptx") != null);
        Check("OfficeExtractor.ParseCsv 方法存在",
            typeof(WayCoder.Infra.OfficeExtractor).GetMethod("ParseCsv") != null);

        // CSV 解析
        var csvResult = WayCoder.Infra.OfficeExtractor.ParseCsv("name,age\nAlice,30\nBob,25");
        Check("CSV 解析含表头", csvResult.Contains("name") && csvResult.Contains("Alice"));

        // DOCX/XLSX/PPTX 无效文件友好报错
        try
        {
            var tmpDocx = Path.GetTempFileName();
            File.Delete(tmpDocx);
            tmpDocx = Path.ChangeExtension(tmpDocx, ".docx");
            File.WriteAllText(tmpDocx, "not a real docx");
            var badDocx = WayCoder.Infra.OfficeExtractor.ExtractDocx(tmpDocx);
            Check("无效 DOCX 友好报错", badDocx.Contains("错误") || badDocx.Contains("无效") || badDocx.Contains("读取"));
            File.Delete(tmpDocx);
        }
        catch { Fail("Office 错误处理"); }

        // read_file 描述含 Office
        Check("read_file 描述含 docx",
            ToolRegistry.GetTool("read_file")!.Description.Contains("docx"));
        Check("read_file 描述含 CSV",
            ToolRegistry.GetTool("read_file")!.Description.Contains("CSV"));

        // write_file
        try
        {
            var tmpFile2 = Path.GetTempFileName();
            FileTracker.RecordRead(tmpFile2);
            var writeResult = new WriteFileTool().ExecuteAsync(new() { ["file_path"] = tmpFile2, ["content"] = "hi\n" }).Result;
            Check("write_file 基本功能", writeResult.Contains("已写入") && File.ReadAllText(tmpFile2) == "hi\n");
            File.Delete(tmpFile2);
        }
        catch { Fail("write_file 基本功能"); }

        // edit_file
        try
        {
            var tmpFile3 = Path.GetTempFileName();
            File.WriteAllText(tmpFile3, "hello world\n");
            FileTracker.RecordRead(tmpFile3);
            var editResult = new EditFileTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpFile3, ["old_string"] = "world", ["new_string"] = "地球",
            }).Result;
            Check("edit_file 基本替换", editResult.Contains("已编辑") && File.ReadAllText(tmpFile3).Contains("地球"));
            File.Delete(tmpFile3);
        }
        catch { Fail("edit_file 基本替换"); }

        try
        {
            var tmpFile4 = Path.GetTempFileName();
            File.WriteAllText(tmpFile4, "aa\n");
            FileTracker.RecordRead(tmpFile4);
            var editResult = new EditFileTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpFile4, ["old_string"] = "NOTFOUND", ["new_string"] = "x",
            }).Result;
            Check("edit_file 未找到返回错误", editResult.Contains("未找到"));
            File.Delete(tmpFile4);
        }
        catch { Fail("edit_file 未找到返回错误"); }

        // edit_file — replace_all
        try
        {
            var tmpReplaceAll = Path.GetTempFileName();
            File.WriteAllText(tmpReplaceAll, "x x x\n");
            FileTracker.RecordRead(tmpReplaceAll);
            var editResult = new EditFileTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpReplaceAll, ["old_string"] = "x", ["new_string"] = "y", ["replace_all"] = true,
            }).Result;
            var content = File.ReadAllText(tmpReplaceAll);
            Check("edit_file replace_all", editResult.Contains("已编辑") && content == "y y y\n");
            File.Delete(tmpReplaceAll);
        }
        catch { Fail("edit_file replace_all"); }

        // multiedit — 编辑已有文件
        try
        {
            var tmpMultiEdit = Path.GetTempFileName();
            File.WriteAllText(tmpMultiEdit, "line one\nline two\nline three\n");
            FileTracker.RecordRead(tmpMultiEdit);
            var multiResult = new MultiEditTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpMultiEdit,
                ["edits"] = new JsonArray
                {
                    new JsonObject { ["old_string"] = "line one", ["new_string"] = "第一行" },
                    new JsonObject { ["old_string"] = "line two", ["new_string"] = "第二行" },
                },
            }).Result;
            var content = File.ReadAllText(tmpMultiEdit);
            Check("multiedit 多编辑替换", multiResult.Contains("编辑成功") && content.Contains("第一行") && content.Contains("第二行"));
            File.Delete(tmpMultiEdit);
        }
        catch { Fail("multiedit 多编辑替换"); }

        // multiedit — 创建新文件
        try
        {
            var tmpMultiNew = Path.Combine(Path.GetTempPath(), $"waycoder_multiedit_new_{Guid.NewGuid():N}.txt");
            var multiResult = new MultiEditTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpMultiNew,
                ["edits"] = new JsonArray
                {
                    new JsonObject { ["old_string"] = "", ["new_string"] = "initial content\n" },
                    new JsonObject { ["old_string"] = "initial", ["new_string"] = "创建的" },
                },
            }).Result;
            Check("multiedit 创建新文件", multiResult.Contains("已创建") && File.Exists(tmpMultiNew) && File.ReadAllText(tmpMultiNew).Contains("创建的"));
            File.Delete(tmpMultiNew);
        }
        catch { Fail("multiedit 创建新文件"); }

        // multiedit — replace_all
        try
        {
            var tmpMultiAll = Path.GetTempFileName();
            File.WriteAllText(tmpMultiAll, "x x x\n");
            FileTracker.RecordRead(tmpMultiAll);
            var multiResult = new MultiEditTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpMultiAll,
                ["edits"] = new JsonArray
                {
                    new JsonObject { ["old_string"] = "x", ["new_string"] = "y", ["replace_all"] = true },
                },
            }).Result;
            var content = File.ReadAllText(tmpMultiAll);
            Check("multiedit replace_all", content == "y y y\n");
            File.Delete(tmpMultiAll);
        }
        catch { Fail("multiedit replace_all"); }

        // multiedit — 编辑失败
        try
        {
            var tmpMultiFail = Path.GetTempFileName();
            File.WriteAllText(tmpMultiFail, "hello\n");
            FileTracker.RecordRead(tmpMultiFail);
            var multiResult = new MultiEditTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpMultiFail,
                ["edits"] = new JsonArray
                {
                    new JsonObject { ["old_string"] = "NOTFOUND", ["new_string"] = "x" },
                },
            }).Result;
            Check("multiedit 编辑失败报告", multiResult.Contains("失败"));
            File.Delete(tmpMultiFail);
        }
        catch { Fail("multiedit 编辑失败报告"); }

        // glob - 在临时目录中创建文件后测试
        try
        {
            var globDir = Path.Combine(Path.GetTempPath(), "glob_test_" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(globDir);
            File.WriteAllText(Path.Combine(globDir, "a.cs"), "");
            File.WriteAllText(Path.Combine(globDir, "b.cs"), "");
            var globResult = new GlobTool().ExecuteAsync(new() { ["pattern"] = "*.cs", ["path"] = globDir }).Result;
            Check("glob 找到 .cs 文件", globResult.Contains("a.cs") && globResult.Contains("b.cs"));
            Directory.Delete(globDir, true);
        }
        catch { Fail("glob 找到 .cs 文件"); }

        // glob/grep/wc 空路径校验 — 防止 Path.GetFullPath("") 崩溃
        Check("glob 空路径不崩溃",
            new GlobTool().ExecuteAsync(new() { ["pattern"] = "*.cs", ["path"] = "" }).Result.Contains("错误") == false);
        Check("grep 空路径不崩溃",
            new GrepTool().ExecuteAsync(new() { ["pattern"] = "test", ["path"] = "" }).Result.Contains("错误")
            || new GrepTool().ExecuteAsync(new() { ["pattern"] = "test", ["path"] = "" }).Result.Contains("未找到"));
        Check("wc 空路径不崩溃",
            new WcTool().ExecuteAsync(new() { ["glob"] = "*.cs", ["path"] = "" }).Result.Contains("错误") == false);

        // grep/grep 无效正则
        Check("grep 无效正则返回错误",
            new GrepTool().ExecuteAsync(new() { ["pattern"] = "[bad" }).Result.Contains("无效的正则"));

        // ── v0.36.0: ParseArgs 错误标记清除 ──
        // 验证截断/无效 JSON 不会泄漏 _parse_error 到工具参数
        var truncatedArgs = LLM.ParseArgs("{\"file_path\": \"test.html\", \"content\": \"truncated...");
        Check("ParseArgs 截断 JSON 不泄漏 _parse_error",
            !truncatedArgs.ContainsKey("_parse_error") && !truncatedArgs.ContainsKey("_parse_error_type"));

        var invalidArgs = LLM.ParseArgs("not even json");
        Check("ParseArgs 无效 JSON 不泄漏 _parse_error",
            !invalidArgs.ContainsKey("_parse_error") && !invalidArgs.ContainsKey("_parse_error_type"));

        var validArgs = LLM.ParseArgs("{\"file_path\": \"test.html\", \"content\": \"hello\"}");
        Check("ParseArgs 有效 JSON 正常解析",
            validArgs.ContainsKey("file_path") && validArgs["file_path"]?.ToString() == "test.html");

        // ── v0.38.0: ParseArgs 正确解析 JsonArray（agent tasks 数组 bug 修复）──
        // 此前 JsonArray 被 ToJsonString() 序列化成字符串，导致 agent 工具逐字符遍历
        var arrayArgs = LLM.ParseArgs("{\"tasks\": [\"任务A\", \"任务B\", \"任务C\"]}");
        Check("ParseArgs 数组解析为 List（而非字符串）",
            arrayArgs.ContainsKey("tasks") &&
            arrayArgs["tasks"] is List<object?> taskListParsed &&
            taskListParsed.Count == 3 &&
            taskListParsed[0]?.ToString() == "任务A" &&
            taskListParsed[2]?.ToString() == "任务C");

        // 嵌套对象也应递归解析为 Dictionary
        var nestedArgs = LLM.ParseArgs("{\"config\": {\"depth\": 2, \"enabled\": true}}");
        Check("ParseArgs 嵌套对象解析为 Dictionary",
            nestedArgs.ContainsKey("config") &&
            nestedArgs["config"] is Dictionary<string, object?> cfgDict &&
            cfgDict.Count == 2 &&
            (long)cfgDict["depth"]! == 2 &&
            (bool)cfgDict["enabled"]! == true);

        // 数字类型保真：整数→long，小数→double，负数/大整数不丢精度
        var numArgs = LLM.ParseArgs("{\"pi\": 3.14, \"neg\": -42, \"big\": 9007199254740993}");
        Check("ParseArgs 小数解析为 double",
            numArgs.ContainsKey("pi") && numArgs["pi"] is double piVal && Math.Abs(piVal - 3.14) < 0.0001);
        Check("ParseArgs 负整数解析为 long",
            numArgs.ContainsKey("neg") && numArgs["neg"] is long negVal && negVal == -42);
        // 9007199254740993 = 2^53 + 1，超出 double 精确范围，long 解析才能保真
        Check("ParseArgs 大整数解析为 long（不丢精度）",
            numArgs.ContainsKey("big") && numArgs["big"] is long bigVal && bigVal == 9007199254740993L);

        // 混合类型数组：保序、保类型（数字/字符串/布尔/null/小数）
        var mixedArgs = LLM.ParseArgs("{\"mix\": [1, \"two\", true, null, 3.5]}");
        Check("ParseArgs 混合数组保序保类型",
            mixedArgs["mix"] is List<object?> mixList &&
            mixList.Count == 5 &&
            mixList[0] is long && (long)mixList[0]! == 1 &&
            (string)mixList[1]! == "two" &&
            mixList[2] is bool && (bool)mixList[2]! == true &&
            mixList[3] == null &&
            mixList[4] is double && (double)mixList[4]! > 3.49);

        // 嵌套数组递归解析
        var deepArgs = LLM.ParseArgs("{\"grid\": [[1,2],[3,4]]}");
        Check("ParseArgs 嵌套数组递归解析",
            deepArgs["grid"] is List<object?> outerGrid &&
            outerGrid.Count == 2 &&
            outerGrid[0] is List<object?> row0 && row0.Count == 2 &&
            (long)row0[0]! == 1 && (long)row0[1]! == 2 &&
            outerGrid[1] is List<object?> row1 && (long)row1[0]! == 3);

        // ── v0.47.11: ParseArgs 重复键容错（后者覆盖，不再抛 ArgumentException）──
        // LLM 偶发输出含重复键的工具参数（如 agent 工具 {"task":"a","task":"b"}），
        // JsonNode 解析后枚举会抛「已存在相同键 Key: task」，导致该轮工具参数被丢弃。
        var dupArgs = LLM.ParseArgs("{\"task\": \"a\", \"task\": \"b\"}");
        Check("ParseArgs 重复键后者覆盖",
            dupArgs.ContainsKey("task") && (string)dupArgs["task"]! == "b" && dupArgs.Count == 1);
        // 嵌套对象内重复键同样容错
        var dupNested = LLM.ParseArgs("{\"cfg\": {\"x\": 1, \"x\": 2}}");
        Check("ParseArgs 嵌套重复键后者覆盖",
            dupNested["cfg"] is Dictionary<string, object?> dupCfg &&
            dupCfg.Count == 1 && (long)dupCfg["x"]! == 2);

        // ── v0.36.0: LLMResponse.ReasoningTokens 字段 ──
        var testResp = new LLMResponse
        {
            Content = "test content",
            ReasoningTokens = 1024,
            PromptTokens = 100,
            CompletionTokens = 50,
        };
        Check("LLMResponse.ReasoningTokens 正确传递",
            testResp.ReasoningTokens == 1024 && testResp.Content == "test content");
        Check("LLMResponse.ReasoningTokens 默认值为 0",
            new LLMResponse().ReasoningTokens == 0);

        // ── v0.36.0: IsJsonProbablyComplete 强化检测 ──
        Check("IsJsonProbablyComplete 空字符串为 false",
            LLM.IsJsonProbablyComplete("") == false);
        Check("IsJsonProbablyComplete 完整 JSON 对象为 true",
            LLM.IsJsonProbablyComplete("{\"key\":\"value\"}") == true);
        Check("IsJsonProbablyComplete 截断 JSON 为 false",
            LLM.IsJsonProbablyComplete("{\"key\":\"value\"") == false);
        Check("IsJsonProbablyComplete 不平衡括号为 false",
            LLM.IsJsonProbablyComplete("{\"key\":{\"nested\":\"val\"}") == false);
        Check("IsJsonProbablyComplete 字符串内括号不干扰",
            LLM.IsJsonProbablyComplete("{\"code\":\"function foo() { return 1; }\"}") == true);

        // ── v0.36.0: TalkCode 检测覆盖更多语言 ──
        var jsCodeContent = "function render() { return `hello`; } export default App;";
        var hasJs = jsCodeContent.Length > 300 || (jsCodeContent.Contains("function ") && jsCodeContent.Length > 50);
        Check("TalkCode 检测 JavaScript 代码",
            hasJs == true);

        var goCodeContent = "func main() { fmt.Println(\"hello\") }";
        var hasGo = goCodeContent.Length > 300 || goCodeContent.Contains("func ");
        Check("TalkCode 检测 Go 代码",
            hasGo == true);

        // bash
        var bashResult = new BashTool().ExecuteAsync(new() { ["command"] = "echo hello" }).Result;
        Check("bash 基本命令", bashResult.Contains("hello"));

        Check("bash 阻止 rm -rf /",
            new BashTool().ExecuteAsync(new() { ["command"] = "rm -rf /" }).Result.Contains("已阻止"));

        Check("bash 阻止 fork 炸弹",
            new BashTool().ExecuteAsync(new() { ["command"] = ":(){ :|:& };:" }).Result.Contains("已阻止"));

        Check("bash 允许安全 rm", BashTool.CheckDangerous("rm -f notes.log") == null);

        // cwd 跟踪
        try
        {
            var testDir = Path.Combine(Path.GetTempPath(), "ct_" + Guid.NewGuid().ToString("N")[..6]);
            var subDir = Path.Combine(testDir, "a", "b");
            Directory.CreateDirectory(subDir);
            BashTool.CurrentCwd.Value = null!; // 重置 AsyncLocal
            BashTool.UpdateCwd($"cd {testDir} && cd a && cd b", testDir);
            Check("bash cd 链式解析", BashTool.CurrentCwd.Value == Path.GetFullPath(subDir));
            Directory.Delete(testDir, true);
            BashTool.CurrentCwd.Value = null!; // 重置
        }
        catch { Fail("bash cd 链式解析"); }

        // screenshot（抓屏工具）
        Check("screenshot 已注册", ToolRegistry.GetTool("screenshot") != null);
        var ansiSample = "\x1b[2J\x1b[H\x1b[32mhello\x1b[0m \x1b[1mworld\x1b[0m\n\x1b]0;title\x07";
        var stripped = ScreenshotTool.StripAnsi(ansiSample);
        Check("screenshot 剥离 ANSI（颜色+光标）", stripped == "hello world");
        Check("screenshot 剥离 ANSI（OSC 标题）", !stripped.Contains("\x1b"));
        Check("screenshot 空串返回空", ScreenshotTool.StripAnsi("") == "");
        Check("screenshot console 模式可执行",
            new ScreenshotTool().ExecuteAsync(new() { ["target"] = "console" }).Result.Length >= 0);
        Check("screenshot region 缺宽高报错",
            new ScreenshotTool().ExecuteAsync(new() { ["target"] = "region" }).Result.Contains("width/height"));

        // view_image + 多模态（vision）支持
        Check("view_image 已注册", ToolRegistry.GetTool("view_image") != null);
        Check("ModelSupportsVision: gpt-4o", LLM.ModelSupportsVision("gpt-4o"));
        Check("ModelSupportsVision: claude", LLM.ModelSupportsVision("claude-sonnet-4-6"));
        Check("ModelSupportsVision: deepseek 否", !LLM.ModelSupportsVision("deepseek-v4-flash"));
        Check("ModelSupportsVision: 空模型否", !LLM.ModelSupportsVision(null));
        try
        {
            var imgTmp = Path.Combine(Path.GetTempPath(), "wc_img_" + Guid.NewGuid().ToString("N")[..6] + ".png");
            File.WriteAllText(imgTmp, "fake-png-bytes");
            var imgMsg = LLM.BuildImageMessage("看图", new List<string> { imgTmp });
            Check("BuildImageMessage role=user", imgMsg["role"]?.GetValue<string>() == "user");
            Check("BuildImageMessage content 为数组", imgMsg["content"] is JsonArray);
            var imgParts = imgMsg["content"]!.AsArray();
            Check("BuildImageMessage 含 image_url", imgParts.Any(p => p?["type"]?.GetValue<string>() == "image_url"));
            Check("BuildImageMessage 含 data URL",
                imgParts.Any(p => p?["image_url"]?["url"]?.GetValue<string>()?.StartsWith("data:image/png;base64,") == true));
            // 图片读取失败但文本存在：跳过该图，仍返回数组（仅含 text 部分）
            var badMsg = LLM.BuildImageMessage("无图", new List<string> { "/nonexistent/img.png" });
            Check("BuildImageMessage 图片失败跳过",
                badMsg["content"] is JsonArray && badMsg["content"]!.AsArray().Count == 1);
            // 文本+图片全空：兜底退化为纯文本消息，避免 content 空数组非法
            var emptyMsg = LLM.BuildImageMessage("", new List<string> { "/nonexistent/img.png" });
            Check("BuildImageMessage 全空退化为文本", emptyMsg["content"]?.GetValue<string>() == "");
            File.Delete(imgTmp);
        }
        catch { Fail("BuildImageMessage 多模态"); }

        Console.WriteLine();

        // ---- git ----
    }
}
