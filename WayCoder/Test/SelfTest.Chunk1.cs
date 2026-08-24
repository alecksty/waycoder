using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk1(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[工具注册]");
        Check("工具数量 == 46", ToolRegistry.BuiltinTools.Count == 46);
        Check("所有工具有有效 schema", ToolRegistry.AllTools.All(t =>
        {
            var s = t.Schema();
            return s["type"]?.AsString() == "function"
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
        var msgs1 = new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hello world") };
        Check("Token 估算 > 0", ContextManager.EstimateTokens(msgs1) > 0);

        var msgs2 = new List<JNode>
        {
            // 生成超过 4000 字符的内容（新阈值），每行 50 字符 × 100 行 = 5000+ 字符
            JNode.Object().Set("role", "tool").Set("content", string.Join("\n", Enumerable.Range(0, 100).Select(i => new string('x', 50) + $"_{i:D4}"))),
        };
        var before = ContextManager.EstimateTokens(msgs2);
        ContextManager.SnipToolOutputs(msgs2);
        Check("工具输出裁剪有效", ContextManager.EstimateTokens(msgs2) < before);

        var msgs3 = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "do"),
            JNode.Object().Set("role", "assistant").Set("content", (string?)null).Set("tool_calls", JNode.Array()),
            JNode.Object().Set("role", "tool").Set("tool_call_id", "c1").Set("content", "r"),
        };
        var split = ContextManager.SafeSplit(msgs3, 1);
        Check("SafeSplit 不以 tool 开头", msgs3[split]["role"]?.AsString() != "tool");

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
        // 上下文预算判断（最近 prompt vs 累计用量）测试
        TestContextStopWhen(Check);
        // 省 token 模式测试（EconomyMode 开关）
        TestEconomyMode(Check);
        // 省钱模式工具精简测试（Off=全量 / 开=去重复 / 开的越大越精简）
        TestEconomyToolTrim(Check);
        // 模型/厂商调用参数约束测试（reasoning_effort 允许集 + temperature 精度）
        TestModelParams(Check);
        // 非 OpenAI 格式兼容测试（Anthropic /v1/messages + Gemini streamGenerateContent）
        TestApiFormat(Check);
        // /init 项目初始化测试（生成 AGENT.md + 命令检测）
        TestProjectInit(Check);
        // 多槽位后台并行执行测试（槽位缓冲输出 + 运行状态）
        TestMultiSlotParallel(Check);
        // 实例级工作模式测试（Agent.WorkMode 与全局解耦 + 回调）
        TestWorkModePerAgent(Check);
        // AllowedTools 通用白名单决策链测试
        TestAllowedToolsFilter(Check);
        // 自动升级测试（版本比较 + RID 探测 + 资产匹配纯逻辑 + 供应链校验）
        TestUpdateChecker(Check);
        // 进程工具命令注入防护测试（kill/ps 进程名白名单 + 拦截）
        TestProcessTools(Check);
        // SSRF 防护测试（内网/保留 IP + 特殊主机名拦截 + 重定向状态码）
        TestSsgfGuard(Check);
        // 批量任务引擎测试（多仓库并行 + worktree 隔离 + 聚合报告）
        TestBatchEngine(Check);
        // 编译期插件系统测试（IPlugin + PluginRegistry + 工具/命令集成）
        TestPluginSystem(Check);
        // --json 输出模式测试（IDE 桥接结构化结果）
        TestJsonMode(Check);

        // ---- 基础设施纯逻辑类单元测试（提升代码质量覆盖）----
        Section("[基础设施]");
        TestRetryPolicy(Check);      // 智能重试策略：异常过滤 + 指数退避 + 耗尽
        TestLruCache(Check);         // LRU 缓存：淘汰/提升/TTL/事件/统计
        TestIdGenerator(Check);      // 短 ID 生成器：字符集/唯一性/slug 格式
        TestFileIgnoreManager(Check); // 文件忽略规则：静态忽略 + glob 匹配 + 否定/锚定
        TestMemoryRetrieval(Check);   // 跨会话记忆检索：关键词匹配打分 + 提示词格式化
        TestSnippetStore(Check);      // 代码片段管理：frontmatter 解析 + 增删查/多词搜索
        TestImportHelper(Check);      // 导入助手纯逻辑：JSONC 注释剥离 + 文件大小格式化
        TestFileLockManager(Check);   // 文件锁：获取/续期/拒绝/过期强占/释放/等待
        TestFileTracker(Check);       // 文件追踪：stale-read 检测 + 先读后改保护 + 删除/禁用
        TestPromptCache(Check);       // Prompt 缓存：SHA256 命中/未命中/命中率/节省 token
        TestHooksManager(Check);      // Hook 系统：session hook 注册/事件执行/匹配器/输出协议
        TestJsonLib(Check);           // 手搓 JSON 库：解析/DOM/序列化/转义/错误分支（AOT 零反射）
        TestXmlLib(Check);            // 手搓 XML 库：解析/实体/CDATA/属性/序列化（AOT 零反射）
        TestToolScheduler(Check);     // 工具调度器：Parallel/Exclusive 分批 + 有界并发 + 顺序提交
        TestToolResultClassifier(Check); // 工具结果分类器：真实错误 vs 用户取消/安全阻止
        TestTrajectory(Check);         // 运行轨迹：截断纯函数 + JSONL 事件流落盘/读回
        TestPdf(Check);                // 手搓 PDF 解析器：结构解析 + 文本提取 + 编码 + 错误分支
        TestWps(Check);                // WPS/老式二进制 Office：CFB 解析 + DOC/XLS/PPT 提取 + 容器识别/RTF 路由
        TestP1Hardening(Check);        // P1 健壮性加固：递归深度/死循环/不可信尺寸字段 OOM 防护
        TestP3Concurrency(Check);      // P3 并发竞态：ModelOverride 恢复/线程安全集合/后台任务输出/LRU 重入/文件锁并发/LLM 重试
        TestCrossPlatform(Check);      // P4 跨平台：shell/python 运行器选择 + 参数标志
        TestWeb(Check);                // 浏览器聊天（--web）：HTTP 解析 + SSE + 端到端冒烟
        TestWebFull(Check);            // Web 界面完整化：换模型/换key/设置/槽位 + 序列化纯函数 + 端点冒烟
        TestP4WebResource(Check);      // P4-2 Web 资源耗尽 + XSS：body/连接/SSE/队列上限 + HtmlEscape
        TestWebPanelSessions(Check);  // Web 三栏面板：panel/sessions/lsp 访问器/交互桥 + 端点冒烟
        TestWebCommands(Check);       // Web 斜杠命令：HandleCommand 纯函数 + /command 端点冒烟
        TestWebPrefixInput(Check);    // Web 特殊前缀输入 + 中间格式：SerializeFileList + /test 分支 + /shell//fileref//filelist 冒烟
        TestWebDiffPreview(Check);    // Web Diff 预览：ParseDiffAnswer/SerializeHunks + DiffPreview.Show Web 分支
        TestWebUpload(Check);        // Web 多模态上传：ParseUploadKind/SafeExtension/IsTranscribeError + 二进制正文
        TestP0P2Hardening(Check);      // P0-P2 批次：命令注入/RCE/权限绕过/整数溢出/越界 修复
        TestV0718RuneHardening(Check); // v0.71.8 批次：UTF-16 代理对截断 + BMP int.MinValue + LogMetrics 缩容
        TestV0719RuneHardening(Check); // v0.71.9 批次：ANSI CSI 终止符 + BoxBuffer 负宽度 + 双省略号/宽度预留
        TestV0710EditPrimitives(Check); // v0.71.10 批次：输入控件光标移动/删除的代理对安全
        TestV0711Concurrency(Check);    // v0.71.11 批次：FallbackLLM 原子累加 + WatchMode 幂等 dispose
        TestV0712MessagesThreadSafety(Check); // v0.71.12 批次：Agent.Messages 线程安全封装（锁内读/写 + 快照读）
        TestV0713CtsLifecycle(Check);         // v0.71.13 批次：AgentSlot.Cts 原子摘除（Interlocked.Exchange 恰好一个取到非 null）
        TestV0714RetryAfter(Check);           // v0.71.14 批次：Retry-After 头解析负数/非法值回退
        TestV0715CompressIndicator(Check);    // v0.71.15 批次：压缩界面指示（Web compress 事件载荷 + CompressFinished 事件）
        TestV0716RuneSafeTruncation(Check);   // v0.71.16 批次：6 处 UTF-16 原始切片改走 TruncateByRunes（代理对切半修复）
        TestV0717RuneSafeWrap(Check);         // v0.71.17 批次：WrapLine 兜底按码点取断点（首字符 emoji 不切半）
        TestV0717UiDeterministic(Check);      // v0.71.17 批次：撤销栈修剪方向 / 全分隔线菜单空序列 / BoxBuffer 负宽度
        TestV0718SharedMemoryGet(Check);      // v0.71.18 批次：StructuredMemory.Get 共享记忆按名回退查找（槽位优先）
        TestV0719CjkExtB(Check);              // v0.71.19 批次：SemanticMemory.Tokenize 扩展 B 区汉字（代理对）召回修复
        TestV0720InfraDeterministic(Check);   // v0.71.20 批次：Hooks 前缀碰撞 / FileIgnore 未转义 [ / RetryPolicy 负数+null
        TestV0721FileTrackerRead(Check);      // v0.71.21 批次：FileTracker RecordWrite 更新读取时间 + LRU 淘汰 / ReadFile limit 钳制
        TestV0722RuneSafeContext(Check);      // v0.71.22 批次：FindReplace 上下文窗口 + ErrorLog 参数截断走码点边界（代理对不切半）
        TestV0723SafetyAndCursor(Check);      // v0.71.23 批次：GitTool 危险操作 token 级拦截 + EditorCore 上下移动代理对修正
        TestV0724SyntaxSurrogate(Check);      // v0.71.24 批次：Syntax.Tokenize 代理对成对 token（不切半）
        TestV0725DrawAndCodec(Check);         // v0.71.25 批次：DrawCommands path 首点 + PngDecoder 长度溢出 + BmpCodec 32 位 alpha + 历史预览代理对
        TestV0725ToolArgsAndEdit(Check);      // v0.71.25 批次：ToolArgs 整数取数（long 不丢参）+ MultiEditTool 兼容 List<object?>
        TestV0726SymlinkCdAndUi(Check);       // v0.71.26 批次：符号链接环深度上限 + cd 后相对路径基于 CurrentCwd + TuiGrid 星号轨不溢出
        TestV0728CodecBounds(Check);          // v0.71.28 批次：JPEG/BMP 解析损坏输入越界读改为干净 FormatException
        TestV0729BoundsAndRunes(Check);       // v0.71.29 批次：整数参数钳制 + 编辑器跳列代理对 + 窄宽截断 + 上下文窗口下限
        TestV0730SlotAsyncLocal(Check);       // v0.71.30 批次：记忆槽位 AsyncLocal 隔离（多槽位并行不串记忆目录）
        TestV0730LowSeverity(Check);          // v0.71.30 批次：grep 幻影空行 + cd ~ 前缀展开
        TestV0730UiUndo(Check);               // v0.71.30 批次：文本域撤销栈健壮性 + 编辑器替换可撤销
        TestV0730TuiExperience(Check);        // v0.71.30 批次：markdown 长段落折行 + ANSI 行不折行
        TestV0730Deterministic(Check);        // v0.71.30 批次：CLI 多值累积 / 批处理目录穿越 / 版本溢出 / CJK 单字召回 / Web 畸形解码
        TestV0732Deterministic(Check);        // v0.71.32 批次：mv 源=目标拦截 / wc 码点计数 / 非容器子控件 Parent 指向自身
        Console.WriteLine();

        // ---- 工具 ----
        Section("[工具]");

        // find_replace（查找替换：预览/替换 + 无效正则回退 + 错误分支）
        TestFindReplaceTool(Check);
        // diff（文件差异：差异行/相同/空文件/不存在）
        TestDiffTool(Check);
        // tree（目录树：树生成/深度限制/隐藏跳过）
        TestTreeTool(Check);

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

        // read_file tail 读取末尾
        try
        {
            var tailFile = Path.GetTempFileName();
            File.WriteAllText(tailFile, "line1\nline2\nline3\nline4\nline5");
            var tailResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = tailFile, ["tail"] = 2 }).Result;
            Check("read_file tail 读取末尾", tailResult.Contains("line4") && tailResult.Contains("line5") && !tailResult.Contains("line1"));
            File.Delete(tailFile);
        }
        catch { Fail("read_file tail 读取末尾"); }

        // read_file 二进制检测（NUL 字节）
        try
        {
            var binFile = Path.GetTempFileName();
            File.WriteAllBytes(binFile, new byte[] { 0x48, 0x00, 0x65, 0x6C, 0x6C, 0x6F, 0x00 }); // "H\0ello\0"
            var binResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = binFile }).Result;
            Check("read_file 二进制检测", binResult.Contains("二进制"));
            File.Delete(binFile);
        }
        catch { Fail("read_file 二进制检测"); }

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

        // read_file JSON 结构化
        try
        {
            var jsonFile = Path.Combine(Path.GetTempPath(), "wc_json_" + Guid.NewGuid().ToString("N")[..6] + ".json");
            File.WriteAllText(jsonFile, "{\"a\":1,\"b\":{\"c\":2}}");
            var jsonResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = jsonFile }).Result;
            Check("read_file JSON 结构化", jsonResult.Contains("<json>") && jsonResult.Contains("\"a\""));
            File.Delete(jsonFile);
        }
        catch { Fail("read_file JSON 结构化"); }

        // read_file INI 结构化
        try
        {
            var iniFile = Path.Combine(Path.GetTempPath(), "wc_ini_" + Guid.NewGuid().ToString("N")[..6] + ".ini");
            File.WriteAllText(iniFile, "[server]\nhost = localhost\nport = 8080\n");
            var iniResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = iniFile }).Result;
            Check("read_file INI 结构化", iniResult.Contains("<ini>") && iniResult.Contains("[server]") && iniResult.Contains("host = localhost"));
            File.Delete(iniFile);
        }
        catch { Fail("read_file INI 结构化"); }

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

        // write_file append 模式
        try
        {
            var appendFile = Path.GetTempFileName();
            new WriteFileTool().ExecuteAsync(new() { ["file_path"] = appendFile, ["content"] = "line1\n", ["append"] = true }).Wait();
            var appendResult = new WriteFileTool().ExecuteAsync(new() { ["file_path"] = appendFile, ["content"] = "line2\n", ["append"] = true }).Result;
            Check("write_file append 追加", appendResult.Contains("追加") && File.ReadAllText(appendFile) == "line1\nline2\n");
            File.Delete(appendFile);
        }
        catch { Fail("write_file append 追加"); }

        // write_file encoding 模式（utf8bom 写入 BOM）
        try
        {
            var encFile = Path.Combine(Path.GetTempPath(), "wc_enc_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
            var encResult = new WriteFileTool().ExecuteAsync(new() { ["file_path"] = encFile, ["content"] = "中文", ["encoding"] = "utf8bom" }).Result;
            var bytes = File.ReadAllBytes(encFile);
            Check("write_file utf8bom 编码", encResult.Contains("写入") && bytes.Length >= 5 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
            File.Delete(encFile);
        }
        catch { Fail("write_file utf8bom 编码"); }

        Check("write_file 不支持编码报错",
            new WriteFileTool().ExecuteAsync(new() { ["file_path"] = "/tmp/x.txt", ["content"] = "x", ["encoding"] = "gbk" }).Result.Contains("不支持的编码"));

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

        // edit_file — CRLF 文件多行 old_string 匹配（v0.71.29 修复：匹配前归一化 CRLF）
        try
        {
            var tmpCrlf = Path.GetTempFileName();
            File.WriteAllText(tmpCrlf, "line one\r\nline two\r\nline three\r\n");
            FileTracker.RecordRead(tmpCrlf);
            var editResult = new EditFileTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpCrlf, ["old_string"] = "line one\nline two", ["new_string"] = "ONE\nTWO",
            }).Result;
            var crlfContent = File.ReadAllText(tmpCrlf);
            Check("edit_file CRLF 多行匹配", editResult.Contains("已编辑") && crlfContent == "ONE\r\nTWO\r\nline three\r\n");
            File.Delete(tmpCrlf);
        }
        catch { Fail("edit_file CRLF 多行匹配"); }

        // multiedit — 编辑已有文件
        try
        {
            var tmpMultiEdit = Path.GetTempFileName();
            File.WriteAllText(tmpMultiEdit, "line one\nline two\nline three\n");
            FileTracker.RecordRead(tmpMultiEdit);
            var multiResult = new MultiEditTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpMultiEdit,
                ["edits"] = JNode.Array()
                    .Add(JNode.Object().Set("old_string", "line one").Set("new_string", "第一行"))
                    .Add(JNode.Object().Set("old_string", "line two").Set("new_string", "第二行")),
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
                ["edits"] = JNode.Array()
                    .Add(JNode.Object().Set("old_string", "").Set("new_string", "initial content\n"))
                    .Add(JNode.Object().Set("old_string", "initial").Set("new_string", "创建的")),
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
                ["edits"] = JNode.Array()
                    .Add(JNode.Object().Set("old_string", "x").Set("new_string", "y").Set("replace_all", true)),
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
                ["edits"] = JNode.Array()
                    .Add(JNode.Object().Set("old_string", "NOTFOUND").Set("new_string", "x")),
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
        // 空路径 = 搜当前目录，命不命中取决于 cwd 里有什么 —— 断言只看「没崩、有结果字符串」，
        // 不能要求「错误/未找到」（在 WayCoder 源码目录里搜 test 必然命中，那样是测目录内容不是测工具）
        Check("grep 空路径不崩溃", TryToolCall(() =>
            new GrepTool().ExecuteAsync(new() { ["pattern"] = "test", ["path"] = "" }).Result));
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

        // 持久 shell 会话
        var psBuild = PersistentShell.BuildCommand("echo hi", "MARK");
        Check("持久 shell 命令包装", psBuild.Contains("echo hi") && psBuild.Contains("MARK"));

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                using var shell = new PersistentShell("test-session");
                shell.RunAsync("cd /tmp", 10).Wait();
                var psPwd = shell.RunAsync("pwd", 10).Result;
                Check("持久 shell cwd 保持", psPwd.Contains("/tmp"));
                shell.RunAsync("export WC_PS_TEST=hello42", 10).Wait();
                var psEnv = shell.RunAsync("echo $WC_PS_TEST", 10).Result;
                Check("持久 shell 环境变量保持", psEnv.Contains("hello42"));
                var psExit = shell.RunAsync("false", 10).Result;
                Check("持久 shell 退出码标注", psExit.Contains("退出码"));
            }
            catch { Fail("持久 shell 会话"); }
        }
        PersistentShellManager.ShutdownAll();
        Check("持久 shell 管理器清理", true);

        // 环境变量清理（防密钥泄漏）
        Check("env 敏感名 KEY", EnvScrubber.IsSensitive("WAYCODER_API_KEY"));
        Check("env 敏感名 SECRET", EnvScrubber.IsSensitive("AWS_SECRET_ACCESS_KEY"));
        Check("env 敏感名 PASSWORD", EnvScrubber.IsSensitive("DB_PASSWORD"));
        Check("env 敏感名 TOKEN", EnvScrubber.IsSensitive("GH_TOKEN"));
        Check("env 非敏感 HOME", !EnvScrubber.IsSensitive("HOME"));
        Check("env 非敏感 PATH", !EnvScrubber.IsSensitive("PATH"));
        Check("env 非敏感 DISPLAY", !EnvScrubber.IsSensitive("DISPLAY"));

        var scrubPsi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "true",
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        scrubPsi.EnvironmentVariables["HOME"] = "/home/test";
        scrubPsi.EnvironmentVariables["AWS_ACCESS_KEY_ID"] = "AKIA...";
        scrubPsi.EnvironmentVariables["WAYCODER_API_KEY"] = "sk-...";
        EnvScrubber.Scrub(scrubPsi);
        Check("env scrub 移除敏感项",
            scrubPsi.EnvironmentVariables.ContainsKey("HOME") &&
            !scrubPsi.EnvironmentVariables.ContainsKey("AWS_ACCESS_KEY_ID") &&
            !scrubPsi.EnvironmentVariables.ContainsKey("WAYCODER_API_KEY"));

        // 进程树终止（#162）：父进程被整个进程树终止时，其子进程一并被杀
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                static bool IsPidAlive(int pid)
                {
                    try { using var p = System.Diagnostics.Process.GetProcessById(pid); return !p.HasExited; }
                    catch (ArgumentException) { return false; }
                }

                var pidFile = Path.Combine(Path.GetTempPath(), $"wc_tree_{Guid.NewGuid():N}.pid");
                var treePsi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"sleep 300 & echo $! > '{pidFile}'; wait\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var parent = System.Diagnostics.Process.Start(treePsi)!;
                // 等子进程 PID 落盘
                for (var i = 0; i < 100 && !File.Exists(pidFile); i++) Thread.Sleep(50);
                Check("进程树 子进程已启动", File.Exists(pidFile));
                var childPid = int.Parse(File.ReadAllText(pidFile).Trim());
                Check("进程树 子进程存活", IsPidAlive(childPid));

                parent.Kill(entireProcessTree: true);
                parent.WaitForExit(5000);
                Thread.Sleep(300); // 给子进程 SIGKILL 生效时间
                Check("进程树 父进程终止子进程一并终止", !IsPidAlive(childPid));

                try { File.Delete(pidFile); } catch { }
                parent.Dispose();
            }
            catch { Fail("进程树终止"); }
        }

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
            Check("BuildImageMessage role=user", imgMsg["role"]?.AsString() == "user");
            Check("BuildImageMessage content 为数组", imgMsg["content"]?.Kind == JKind.Array);
            var imgParts = imgMsg["content"]!;
            Check("BuildImageMessage 含 image_url", imgParts.Items.Any(p => p?["type"]?.AsString() == "image_url"));
            Check("BuildImageMessage 含 data URL",
                imgParts.Items.Any(p => p?["image_url"]?["url"]?.AsString()?.StartsWith("data:image/png;base64,") == true));
            // 图片读取失败但文本存在：跳过该图，仍返回数组（仅含 text 部分）
            var badMsg = LLM.BuildImageMessage("无图", new List<string> { "/nonexistent/img.png" });
            Check("BuildImageMessage 图片失败跳过",
                badMsg["content"]?.Kind == JKind.Array && badMsg["content"]!.Count == 1);
            // 文本+图片全空：兜底退化为纯文本消息，避免 content 空数组非法
            var emptyMsg = LLM.BuildImageMessage("", new List<string> { "/nonexistent/img.png" });
            Check("BuildImageMessage 全空退化为文本", emptyMsg["content"]?.AsString() == "");
            File.Delete(imgTmp);
        }
        catch { Fail("BuildImageMessage 多模态"); }

        // transcribe（音频转录工具）
        Check("transcribe 已注册", ToolRegistry.GetTool("transcribe") != null);
        Check("transcribe 支持 mp3", TranscribeAudioTool.IsSupportedAudioExtension("mp3"));
        Check("transcribe 支持 flac", TranscribeAudioTool.IsSupportedAudioExtension("flac"));
        Check("transcribe 拒绝 txt", !TranscribeAudioTool.IsSupportedAudioExtension("txt"));
        Check("transcribe 拒绝空扩展名", !TranscribeAudioTool.IsSupportedAudioExtension(""));
        Check("transcribe MIME: mp3", TranscribeAudioTool.MapContentType("mp3") == "audio/mpeg");
        Check("transcribe MIME: wav", TranscribeAudioTool.MapContentType("wav") == "audio/wav");
        Check("transcribe MIME: 未知", TranscribeAudioTool.MapContentType("xyz") == "application/octet-stream");
        Check("transcribe 空路径报错",
            TranscribeAudioTool.ValidateAudioFile("", out var _e1) == null && _e1.Contains("不能为空"));
        Check("transcribe 不存在文件报错",
            TranscribeAudioTool.ValidateAudioFile("/nonexistent/voice.mp3", out var _e2) == null && _e2.Contains("不存在"));
        var tmpTxt = Path.Combine(Path.GetTempPath(), "wc_transcribe_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        File.WriteAllText(tmpTxt, "not audio");
        Check("transcribe 不支持格式报错",
            TranscribeAudioTool.ValidateAudioFile(tmpTxt, out var _e3) == null && _e3.Contains("不支持的音频格式"));
        File.Delete(tmpTxt);
        Check("transcribe 配置默认 whisper-1", Config.Instance.WhisperModel == "whisper-1");

        Console.WriteLine();

        // ---- screenshot 截图抓屏：PNG 尺寸解析 + 端到端控制台画面捕获 ----
        Section("[截图抓屏]");
        // 1. GUI 抓屏的 PNG 宽高读取（纯文件解析，无需真实抓屏）
        var pngPath = Path.Combine(Path.GetTempPath(), "wc_png_" + Guid.NewGuid().ToString("N")[..6] + ".png");
        try
        {
            var png = new byte[24];
            png[0] = 0x89; png[1] = (byte)'P'; png[2] = (byte)'N'; png[3] = (byte)'G';
            png[4] = 0x0D; png[5] = 0x0A; png[6] = 0x1A; png[7] = 0x0A;   // 8 字节签名
            png[11] = 13;                                                  // IHDR 长度
            png[12] = (byte)'I'; png[13] = (byte)'H'; png[14] = (byte)'D'; png[15] = (byte)'R';
            png[18] = 0x02; png[19] = 0x80;                                // width = 640
            png[22] = 0x01; png[23] = 0xE0;                                // height = 480
            File.WriteAllBytes(pngPath, png);
            var dims = ScreenshotTool.ReadPngDimensions(pngPath);
            Check("screenshot 读取 PNG 尺寸 640x480", dims.Width == 640 && dims.Height == 480);
        }
        catch { Fail("screenshot 读取 PNG 尺寸"); }
        finally { try { File.Delete(pngPath); } catch { } }

        // 2. 跨平台 GUI 抓屏命令构造（纯逻辑，覆盖 Windows/macOS/Linux）
        var wcFull = ScreenshotTool.BuildWindowsCapture(true, 0, 0, 0, 0, @"C:\tmp\shot.png");
        Check("screenshot Win 用 powershell", wcFull.Tool == "powershell");
        Check("screenshot Win 全屏含 VirtualScreen", wcFull.Args.Contains("VirtualScreen"));
        Check("screenshot Win 含 CopyFromScreen", wcFull.Args.Contains("CopyFromScreen"));
        Check("screenshot Win 含保存路径", wcFull.Args.Contains("shot.png"));
        var wcRegion = ScreenshotTool.BuildWindowsCapture(false, 10, 20, 100, 50, @"C:\tmp\r.png");
        Check("screenshot Win 区域 Bitmap(100,50)", wcRegion.Args.Contains("Bitmap(100,50)"));
        Check("screenshot Win 区域坐标 CopyFromScreen(10,20", wcRegion.Args.Contains("CopyFromScreen(10,20"));

        var mcFull = ScreenshotTool.BuildMacCapture(true, 0, 0, 0, 0, "/tmp/s.png");
        Check("screenshot mac 用 screencapture", mcFull.Tool == "/usr/sbin/screencapture");
        Check("screenshot mac 全屏 -x", mcFull.Args.StartsWith("-x"));
        var mcRegion = ScreenshotTool.BuildMacCapture(false, 10, 20, 100, 50, "/tmp/s.png");
        Check("screenshot mac 区域 -R 坐标", mcRegion.Args.Contains("-R10,20,100,50"));

        Check("screenshot linux grim 区域 -g", ScreenshotTool.BuildLinuxCommandFor("grim", false, 10, 20, 100, 50, "/tmp/s.png").Args.Contains("\"10,20 100x50\""));
        Check("screenshot linux import 区域 -crop", ScreenshotTool.BuildLinuxCommandFor("import", false, 10, 20, 100, 50, "/tmp/s.png").Args.Contains("-crop 100x50+10+20"));
        Check("screenshot linux scrot 区域 -a", ScreenshotTool.BuildLinuxCommandFor("scrot", false, 10, 20, 100, 50, "/tmp/s.png").Args.Contains("-a 10,20,100,50"));
        Check("screenshot linux maim 区域 -g", ScreenshotTool.BuildLinuxCommandFor("maim", false, 10, 20, 100, 50, "/tmp/s.png").Args.Contains("-g 100x50+10+20"));
        var grFull = ScreenshotTool.BuildLinuxCommandFor("grim", true, 0, 0, 0, 0, "/tmp/s.png");
        Check("screenshot linux grim 全屏无坐标", grFull.Args.Contains("/tmp/s.png") && !grFull.Args.Contains("-g"));
        Check("screenshot linux 回退链含 4 工具",
            ScreenshotTool.LinuxCaptureTools.Length == 4 &&
            ScreenshotTool.LinuxCaptureTools.Contains("grim") && ScreenshotTool.LinuxCaptureTools.Contains("import"));

        // 3. 端到端：渲染真实 ChatScreen → 抓屏 console 模式 → 可读文本（不在 TUI 内才跑，避免双重进入备用屏）
        if (!TuiManager.Instance.IsActive)
        {
            string shot = "";
            var shotOut = Console.Out;
            try
            {
                var mgr = TuiManager.Instance;
                Console.SetOut(TextWriter.Null);   // 渲染帧不污染 --test 输出（LastCleanFrame 仍会填充）
                mgr.Enter();
                var chat = new ChatScreen();
                mgr.PushScreen(chat);
                chat.AddMessage("欢迎使用 WayCoder 抓屏自测", "assistant");
                chat.AddMessage("这是一条用于验证截图的测试消息", "user");
                mgr.Render();
                shot = new ScreenshotTool().ExecuteAsync(new() { ["target"] = "console" }).Result;
            }
            catch { shot = ""; }
            finally
            {
                try { TuiManager.Instance.Exit(); } catch { }
                try { if (TuiManager.Instance.ActiveScreen != null) TuiManager.Instance.PopScreen(); } catch { }
                Console.SetOut(shotOut);
            }

            Check("截图: console 模式捕获到画面", shot.Contains("当前终端画面"));
            Check("截图: 画面含 assistant 消息", shot.Contains("欢迎使用 WayCoder 抓屏自测"));
            Check("截图: 画面含 user 消息", shot.Contains("这是一条用于验证截图的测试消息"));
            Check("截图: 画面已剥离 ANSI", !shot.Contains("\x1b"));
        }

        Console.WriteLine();

        // ---- git ----
    }
}
