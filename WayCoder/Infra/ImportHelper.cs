using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 从 Claude Code / OpenCode 导入配置数据到 WayCoder。
///
/// 支持导入：
///   - 模型/API 配置：从 settings.json / opencode.jsonc 提取 API Key、Base URL、模型名
///   - MCP 服务器：从插件列表 / mcp 对象映射到 mcp_servers.json
///   - 项目上下文：CLAUDE.md → prompt.md
///   - 会话数据：transcripts → sessions
///
/// 用法：ImportHelper.Detect() 扫描可导入项，ImportHelper.Import() 执行导入。
/// </summary>
public static class ImportHelper
{
    /// <summary>Claude Code 全局配置目录</summary>
    public static readonly string ClaudeHome = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

    /// <summary>OpenCode 全局配置目录</summary>
    public static readonly string OpenCodeHome = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "opencode");

    /// <summary>导入结果项</summary>
    public record ImportItem(string Category, string Name, string Description, bool CanImport);

    /// <summary>导入来源</summary>
    public enum Source { Claude, OpenCode, Cursor, Cline }

    /// <summary>
    /// 扫描所有可导入的配置（Claude Code + OpenCode），返回清单。
    /// </summary>
    public static List<ImportItem> Detect()
    {
        var items = new List<ImportItem>();
        DetectClaude(items);
        DetectOpenCode(items);
        DetectCursor(items);
        DetectCline(items);
        return items;
    }

    private static void DetectClaude(List<ImportItem> items)
    {
        // 1. 模型 / API 配置
        var settingsPath = Path.Combine(ClaudeHome, "settings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = Json.Parse(File.ReadAllText(settingsPath, Encoding.UTF8));
                var env = json?["env"];
                if (env != null)
                {
                    var apiKey = env.Entries.FirstOrDefault(kv =>
                        kv.Key.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
                        kv.Key.Contains("AUTH_TOKEN", StringComparison.OrdinalIgnoreCase)).Value?.AsString();

                    var baseUrl = env.Entries.FirstOrDefault(kv =>
                        kv.Key.Contains("BASE_URL", StringComparison.OrdinalIgnoreCase)).Value?.AsString();

                    var models = env.Entries.Where(kv => kv.Key.Contains("MODEL", StringComparison.OrdinalIgnoreCase))
                        .Select(kv => $"{kv.Key}={kv.Value.AsString()}")
                        .ToList();

                    var desc = new List<string>();
                    if (!string.IsNullOrEmpty(apiKey)) desc.Add($"API Key: {apiKey[..Math.Min(12, apiKey.Length)]}...");
                    if (!string.IsNullOrEmpty(baseUrl)) desc.Add($"Base URL: {baseUrl}");
                    if (models.Count > 0) desc.Add($"{models.Count} 个模型映射");

                    if (desc.Count > 0)
                        items.Add(new ImportItem("📡 模型/API", "[Claude] 模型配置",
                            string.Join(" · ", desc), true));
                    else
                        items.Add(new ImportItem("📡 模型/API", "[Claude] 模型配置",
                            "settings.json 存在但无有效配置", false));
                }
            }
            catch { }
        }

        // 2. MCP 服务器（从 installed_plugins.json 提取）
        var pluginsPath = Path.Combine(ClaudeHome, "plugins", "installed_plugins.json");
        if (File.Exists(pluginsPath))
        {
            try
            {
                var json = Json.Parse(File.ReadAllText(pluginsPath, Encoding.UTF8));
                var plugins = json?["plugins"];
                if (plugins != null && plugins.Count > 0)
                {
                    var names = plugins.Entries.Select(p => p.Key).ToList();
                    items.Add(new ImportItem("🔌 MCP 服务器", "[Claude] 插件",
                        $"{plugins.Count} 个: {string.Join(", ", names.Take(5))}{(names.Count > 5 ? "…" : "")}",
                        true));
                }
            }
            catch { }
        }

        // 3. 项目上下文（CLAUDE.md）
        var claudeMd = FindClaudeMdInTree(Environment.CurrentDirectory);
        if (claudeMd != null)
        {
            var size = new FileInfo(claudeMd).Length;
            items.Add(new ImportItem("📋 项目上下文", "[Claude] CLAUDE.md",
                $"{claudeMd} ({FormatSize(size)})", true));
        }

        // 4. 会话/数据
        var sessionsDir = Path.Combine(ClaudeHome, "sessions");
        if (Directory.Exists(sessionsDir))
        {
            var sessionFiles = Directory.GetFiles(sessionsDir, "*.json");
            if (sessionFiles.Length > 0)
            {
                items.Add(new ImportItem("💬 会话数据", "[Claude] 会话",
                    $"{sessionFiles.Length} 个会话文件", true));
            }
        }

        // 5. 项目权限
        var projectClaudeDir = FindProjectClaudeDir(Environment.CurrentDirectory);
        if (projectClaudeDir != null)
        {
            var localSettings = Path.Combine(projectClaudeDir, "settings.local.json");
            if (File.Exists(localSettings))
            {
                try
                {
                    var json = Json.Parse(File.ReadAllText(localSettings, Encoding.UTF8));
                    var perms = json?["permissions"]?["allow"];
                    if (perms != null && perms.Count > 0)
                        items.Add(new ImportItem("🔑 权限规则", "[Claude] 权限",
                            $"{perms.Count} 条允许规则", true));
                }
                catch { }
            }
        }
    }

    private static void DetectOpenCode(List<ImportItem> items)
    {
        var configPath = Path.Combine(OpenCodeHome, "opencode.jsonc");
        if (!File.Exists(configPath)) return;

        try
        {
            var raw = File.ReadAllText(configPath, Encoding.UTF8);
            var json = Json.Parse(StripJsonComments(raw));
            if (json == null) return;

            // MCP 服务器
            var mcp = json["mcp"];
            if (mcp != null && mcp.Count > 0)
            {
                var enabled = mcp.Entries.Where(kv =>
                {
                    var enabled = kv.Value?["enabled"]?.AsBool();
                    return enabled != false; // 缺省为 true
                }).ToList();

                if (enabled.Count > 0)
                {
                    var names = enabled.Select(kv => kv.Key).ToList();
                    items.Add(new ImportItem("🔌 MCP 服务器", "[OpenCode] MCP",
                        $"{enabled.Count} 个: {string.Join(", ", names.Take(5))}{(names.Count > 5 ? "…" : "")}",
                        true));
                }
            }

            // 插件列表
            var plugins = json["plugin"];
            if (plugins != null && plugins.Count > 0)
            {
                var names = plugins.Items.Select(p => p.AsString() ?? "").Where(n => n != "").ToList();
                if (names.Count > 0)
                    items.Add(new ImportItem("🧩 插件参考", "[OpenCode] 插件",
                        $"{names.Count} 个: {string.Join(", ", names.Take(5))}{(names.Count > 5 ? "…" : "")}",
                        false)); // 仅供参考，不能直接导入
            }
        }
        catch { }
    }

    /// <summary>扫描 Cursor 配置（.cursor/mcp.json + .cursorrules）</summary>
    private static void DetectCursor(List<ImportItem> items)
    {
        var cwd = Environment.CurrentDirectory;

        // Cursor MCP 配置 (.cursor/mcp.json)
        var mcpPath = FindInTree(cwd, ".cursor", "mcp.json");
        if (mcpPath != null)
        {
            try
            {
                var json = Json.Parse(File.ReadAllText(mcpPath, Encoding.UTF8));
                var servers = json?["mcpServers"];
                if (servers != null && servers.Count > 0)
                {
                    var names = servers.Entries.Select(s => s.Key).ToList();
                    items.Add(new ImportItem("🔌 MCP 服务器", "[Cursor] MCP",
                        $"{servers.Count} 个: {string.Join(", ", names.Take(5))}{(names.Count > 5 ? "…" : "")}",
                        true));
                }
            }
            catch { }
        }

        // Cursor Rules (.cursor/rules/*.mdc 或 .cursorrules)
        var rulesDir = FindInTree(cwd, ".cursor", "rules");
        var rulesFiles = rulesDir != null && Directory.Exists(rulesDir)
            ? Directory.GetFiles(rulesDir, "*.mdc").ToList()
            : new List<string>();

        var legacyRules = FindInTree(cwd, ".cursorrules");
        if (legacyRules != null) rulesFiles.Add(legacyRules);

        if (rulesFiles.Count > 0)
        {
            items.Add(new ImportItem("📋 项目上下文", "[Cursor] Rules",
                $"{rulesFiles.Count} 个规则文件", true));
        }

        // Cursor 全局设置 (模型配置)
        var cursorHome = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cursor");
        if (Directory.Exists(cursorHome))
        {
            var settingsPath = Path.Combine(cursorHome, "settings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = Json.Parse(File.ReadAllText(settingsPath, Encoding.UTF8));
                    var apiKey = json?["openaiApiKey"]?.AsString()
                              ?? json?["anthropicApiKey"]?.AsString();
                    var model = json?["model"]?.AsString();
                    if (!string.IsNullOrEmpty(apiKey) || !string.IsNullOrEmpty(model))
                        items.Add(new ImportItem("📡 模型/API", "[Cursor] 模型配置",
                            "settings.json 中有 API/模型配置", true));
                }
                catch { }
            }
        }
    }

    /// <summary>扫描 Cline 配置（.clinerules + MCP settings）</summary>
    private static void DetectCline(List<ImportItem> items)
    {
        var cwd = Environment.CurrentDirectory;

        // Cline Rules (.clinerules 文件)
        var clineRules = FindInTree(cwd, ".clinerules");
        if (clineRules != null)
        {
            var size = new FileInfo(clineRules).Length;
            items.Add(new ImportItem("📋 项目上下文", "[Cline] Rules",
                $".clinerules ({FormatSize(size)})", true));
        }

        // Cline MCP settings (通常存在 VS Code 全局存储中)
        var clineHome = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cline");
        if (Directory.Exists(clineHome))
        {
            var mcpPath = Path.Combine(clineHome, "mcp_settings.json");
            if (File.Exists(mcpPath))
            {
                try
                {
                    var json = Json.Parse(File.ReadAllText(mcpPath, Encoding.UTF8));
                    var servers = json?["mcpServers"];
                    if (servers != null && servers.Count > 0)
                    {
                        var names = servers.Entries.Select(s => s.Key).ToList();
                        items.Add(new ImportItem("🔌 MCP 服务器", "[Cline] MCP",
                            $"{servers.Count} 个: {string.Join(", ", names.Take(5))}{(names.Count > 5 ? "…" : "")}",
                            true));
                    }
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// 执行导入。返回导入报告。
    /// </summary>
    /// <param name="categories">
    /// null = 全部导入；
    /// 可指定: "models", "mcp", "context", "sessions", "permissions"
    /// </param>
    public static async Task<string> ImportAsync(HashSet<string>? categories = null)
    {
        var all = categories == null || categories.Count == 0;
        var report = new StringBuilder();
        report.AppendLine("## 导入报告");
        report.AppendLine();

        // ── 1. 模型 / API 配置 ──
        if (all || categories!.Contains("models"))
        {
            var result = ImportModels();
            report.AppendLine(result);
        }

        // ── 2. MCP 服务器（Claude）──
        if (all || categories!.Contains("mcp"))
        {
            var result = await ImportMcpServersAsync(Source.Claude);
            report.AppendLine(result);
        }

        // ── 2b. MCP 服务器（OpenCode）──
        if (all || categories!.Contains("mcp"))
        {
            var result = await ImportMcpServersAsync(Source.OpenCode);
            report.AppendLine(result);
        }

        // ── 2c. MCP 服务器（Cursor）──
        if (all || categories!.Contains("mcp"))
        {
            var result = await ImportMcpServersAsync(Source.Cursor);
            report.AppendLine(result);
        }

        // ── 2d. MCP 服务器（Cline）──
        if (all || categories!.Contains("mcp"))
        {
            var result = await ImportMcpServersAsync(Source.Cline);
            report.AppendLine(result);
        }

        // ── 3. 项目上下文 ──
        if (all || categories!.Contains("context"))
        {
            var result = ImportContext();
            report.AppendLine(result);
        }

        // ── 4. 会话数据 ──
        if (all || categories!.Contains("sessions"))
        {
            var result = await ImportSessionsAsync();
            report.AppendLine(result);
        }

        // ── 5. 权限规则 ──
        if (all || categories!.Contains("permissions"))
        {
            var result = ImportPermissions();
            report.AppendLine(result);
        }

        return report.ToString().Trim();
    }

    // ── 各模块导入实现 ──

    /// <summary>
    /// 导入模型和 API 配置。写回 .env 或直接设置环境变量提示。
    /// </summary>
    private static string ImportModels()
    {
        var settingsPath = Path.Combine(ClaudeHome, "settings.json");
        if (!File.Exists(settingsPath))
            return "❌ 模型/API: 未找到 ~/.claude/settings.json";

        try
        {
            var json = Json.Parse(File.ReadAllText(settingsPath, Encoding.UTF8));
            var env = json?["env"];
            if (env == null) return "❌ 模型/API: settings.json 中无 env 配置";

            var sb = new StringBuilder();
            sb.AppendLine("📡 模型/API 配置:");

            // API Key
            var apiKey = env.Entries.FirstOrDefault(kv =>
                kv.Key.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
                kv.Key.Contains("AUTH_TOKEN", StringComparison.OrdinalIgnoreCase)).Value?.AsString();

            // Base URL
            var baseUrl = env.Entries.FirstOrDefault(kv =>
                kv.Key.Contains("BASE_URL", StringComparison.OrdinalIgnoreCase)).Value?.AsString();

            // 模型映射
            var modelMap = new Dictionary<string, string>();
            foreach (var kv in env.Entries)
            {
                if (kv.Key.EndsWith("_MODEL", StringComparison.OrdinalIgnoreCase) &&
                    kv.Value != null)
                {
                    var modelName = kv.Value.AsString();
                    if (!string.IsNullOrEmpty(modelName))
                        modelMap[kv.Key] = modelName;
                }
            }

            // 确定主模型和小模型
            var haikuModel = modelMap.FirstOrDefault(kv => kv.Key.Contains("HAIKU", StringComparison.OrdinalIgnoreCase)).Value
                          ?? "deepseek-v4-flash";
            var sonnetModel = modelMap.FirstOrDefault(kv => kv.Key.Contains("SONNET", StringComparison.OrdinalIgnoreCase)).Value
                           ?? "deepseek-v4-pro";

            if (!string.IsNullOrEmpty(apiKey))
            {
                sb.AppendLine($"  ✅ API Key: {apiKey[..Math.Min(12, apiKey.Length)]}...");
                sb.AppendLine($"     → 设置: export WAYCODER_API_KEY={apiKey[..Math.Min(12, apiKey.Length)]}...");
            }
            if (!string.IsNullOrEmpty(baseUrl))
            {
                sb.AppendLine($"  ✅ Base URL: {baseUrl}");
                sb.AppendLine($"     → 设置: export WAYCODER_BASE_URL={baseUrl}");
            }
            sb.AppendLine($"  ✅ 大模型: {sonnetModel}  小模型: {haikuModel}");
            sb.AppendLine($"     → 设置: export WAYCODER_MODEL={sonnetModel}");
            sb.AppendLine($"     → 设置: export WAYCODER_SMALL_MODEL={haikuModel}");

            // 写入配置：config.json 全量 + .env 精简为 5 项基本引导配置（服务商/地址/API_KEY/经济模式/鼠标）
            try
            {
                var cfg = Config.Instance;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    cfg.ApiKey = apiKey;
                    ApiKeyStore.Set("openai", apiKey);
                }
                // 「切换模型 = 切换 connect」：经 connect 统一入口写大/小模型
                ConnectionConfig.ApplyModelChoice("openai", sonnetModel, isLarge: true, out _, baseUrl);
                ConnectionConfig.ApplyModelChoice("openai", haikuModel, isLarge: false, out _);
                cfg.SaveToEnvFile();
                sb.AppendLine("  📝 已写入: ~/.waycoder/config.json（全量）+ .env（5 项基本配置）");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  ⚠ 写入配置失败: {ex.Message}");
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"❌ 模型/API: 解析失败 — {ex.Message}";
        }
    }

    /// <summary>
    /// 导入 MCP 服务器配置。从 Claude Code 插件列表 或 OpenCode mcp 对象转换为 WayCoder mcp_servers.json。
    /// </summary>
    private static async Task<string> ImportMcpServersAsync(Source source)
    {
        return source switch
        {
            Source.Claude => await ImportClaudeMcpAsync(),
            Source.OpenCode => await ImportOpenCodeMcpAsync(),
            Source.Cursor => await ImportCursorMcpAsync(),
            Source.Cline => await ImportClineMcpAsync(),
            _ => $"❌ MCP: 未知来源 {source}"
        };
    }

    private static async Task<string> ImportClaudeMcpAsync()
    {
        var pluginsPath = Path.Combine(ClaudeHome, "plugins", "installed_plugins.json");
        if (!File.Exists(pluginsPath))
            return "❌ MCP: 未找到 installed_plugins.json";

        try
        {
            var json = Json.Parse(File.ReadAllText(pluginsPath, Encoding.UTF8));
            var plugins = json?["plugins"];
            if (plugins == null || plugins.Count == 0)
                return "❌ MCP: 无已安装插件";

            var sb = new StringBuilder();
            sb.AppendLine("🔌 MCP 服务器:");

            // Claude Code 插件 → WayCoder MCP 服务器映射
            var knownPlugins = new Dictionary<string, (string Command, string[] Args)>
            {
                ["context7@claude-plugins-official"] = ("npx", new[] { "-y", "@upstash/context7-mcp" }),
                ["frontend-design@claude-plugins-official"] = ("npx", new[] { "-y", "@anthropic/mcp-server-frontend-design" }),
                ["clangd-lsp@claude-plugins-official"] = ("clangd", Array.Empty<string>()),
                ["pyright-lsp@claude-plugins-official"] = ("pyright-langserver", new[] { "--stdio" }),
            };

            var imported = new List<JNode>();
            foreach (var (name, _) in plugins.Entries)
            {
                if (knownPlugins.TryGetValue(name, out var mapping))
                {
                    sb.AppendLine($"  ✅ {name}");
                    var argsArr = JNode.Array();
                    foreach (var a in mapping.Args) argsArr.Add(a);
                    imported.Add(JNode.Object()
                        .Set("name", name.Split('@')[0])
                        .Set("command", mapping.Command)
                        .Set("args", argsArr)
                        .Set("env", JNode.Object())
                        .Set("_comment", $"从 Claude Code 导入: {name}"));
                }
                else
                {
                    sb.AppendLine($"  ⏭ {name} (未识别，跳过 — 可手动配置)");
                }
            }

            return await WriteMcpServersAsync(imported, sb);
        }
        catch (Exception ex)
        {
            return $"❌ MCP: 导入失败 — {ex.Message}";
        }
    }

    /// <summary>
    /// 从 OpenCode opencode.jsonc 导入 MCP 服务器。
    /// </summary>
    private static async Task<string> ImportOpenCodeMcpAsync()
    {
        var configPath = Path.Combine(OpenCodeHome, "opencode.jsonc");
        if (!File.Exists(configPath))
            return "⏭ OpenCode MCP: 未找到 opencode.jsonc";

        try
        {
            var raw = File.ReadAllText(configPath, Encoding.UTF8);
            var json = Json.Parse(StripJsonComments(raw));
            var mcp = json?["mcp"];
            if (mcp == null || mcp.Count == 0)
                return "⏭ OpenCode MCP: 无 mcp 配置";

            var sb = new StringBuilder();
            sb.AppendLine("🔌 OpenCode MCP 服务器:");

            var imported = new List<JNode>();
            foreach (var (name, config) in mcp.Entries)
            {
                var enabled = config?["enabled"]?.AsBool() ?? true;
                if (!enabled)
                {
                    sb.AppendLine($"  ⏭ {name} (已禁用)");
                    continue;
                }

                var type = config?["type"]?.AsString() ?? "local";
                var command = config?["command"]?.Items
                    ?.Select(c => c?.AsString() ?? "").ToArray();

                if (command == null || command.Length == 0)
                {
                    sb.AppendLine($"  ⚠ {name} (无 command，跳过)");
                    continue;
                }

                var mainCmd = command[0];
                var args = command.Length > 1 ? command[1..] : Array.Empty<string>();

                sb.AppendLine($"  ✅ {name} ({type}) — {mainCmd}");
                var argsArr = JNode.Array();
                foreach (var a in args) argsArr.Add(a);
                imported.Add(JNode.Object()
                    .Set("name", name)
                    .Set("command", mainCmd)
                    .Set("args", argsArr)
                    .Set("env", JNode.Object())
                    .Set("_comment", $"从 OpenCode 导入: {name}"));
            }

            return await WriteMcpServersAsync(imported, sb);
        }
        catch (Exception ex)
        {
            return $"❌ OpenCode MCP: 导入失败 — {ex.Message}";
        }
    }

    /// <summary>从 Cursor .cursor/mcp.json 导入 MCP 服务器</summary>
    private static async Task<string> ImportCursorMcpAsync()
    {
        var mcpPath = FindInTree(Environment.CurrentDirectory, ".cursor", "mcp.json");
        if (mcpPath == null) return "⏭ Cursor MCP: 未找到 .cursor/mcp.json";

        try
        {
            var json = Json.Parse(File.ReadAllText(mcpPath, Encoding.UTF8));
            var servers = json?["mcpServers"];
            if (servers == null || servers.Count == 0)
                return "⏭ Cursor MCP: 无 mcpServers 配置";

            var sb = new StringBuilder();
            sb.AppendLine("🔌 Cursor MCP 服务器:");

            var imported = new List<JNode>();
            foreach (var (name, config) in servers.Entries)
            {
                var command = config?["command"]?.AsString();
                var args = config?["args"]?.Items
                    ?.Select(a => a?.AsString() ?? "").ToArray() ?? [];

                if (string.IsNullOrEmpty(command))
                {
                    sb.AppendLine($"  ⚠ {name} (无 command，跳过)");
                    continue;
                }

                sb.AppendLine($"  ✅ {name} — {command} {string.Join(" ", args)}");
                var argsArr = JNode.Array();
                foreach (var a in args) argsArr.Add(a);
                imported.Add(JNode.Object()
                    .Set("name", name)
                    .Set("command", command)
                    .Set("args", argsArr)
                    .Set("env", JNode.Object())
                    .Set("_comment", $"从 Cursor 导入: {name}"));
            }

            return await WriteMcpServersAsync(imported, sb);
        }
        catch (Exception ex)
        {
            return $"❌ Cursor MCP: 导入失败 — {ex.Message}";
        }
    }

    /// <summary>从 Cline mcp_settings.json 导入 MCP 服务器</summary>
    private static async Task<string> ImportClineMcpAsync()
    {
        var clineHome = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cline");
        var mcpPath = Path.Combine(clineHome, "mcp_settings.json");
        if (!File.Exists(mcpPath)) return "⏭ Cline MCP: 未找到 ~/.cline/mcp_settings.json";

        try
        {
            var json = Json.Parse(File.ReadAllText(mcpPath, Encoding.UTF8));
            var servers = json?["mcpServers"];
            if (servers == null || servers.Count == 0)
                return "⏭ Cline MCP: 无 mcpServers 配置";

            var sb = new StringBuilder();
            sb.AppendLine("🔌 Cline MCP 服务器:");

            var imported = new List<JNode>();
            foreach (var (name, config) in servers.Entries)
            {
                var command = config?["command"]?.AsString();
                var args = config?["args"]?.Items
                    ?.Select(a => a?.AsString() ?? "").ToArray() ?? [];

                if (string.IsNullOrEmpty(command))
                {
                    sb.AppendLine($"  ⚠ {name} (无 command，跳过)");
                    continue;
                }

                sb.AppendLine($"  ✅ {name} — {command}");
                var argsArr = JNode.Array();
                foreach (var a in args) argsArr.Add(a);
                imported.Add(JNode.Object()
                    .Set("name", name)
                    .Set("command", command)
                    .Set("args", argsArr)
                    .Set("env", JNode.Object())
                    .Set("_comment", $"从 Cline 导入: {name}"));
            }

            return await WriteMcpServersAsync(imported, sb);
        }
        catch (Exception ex)
        {
            return $"❌ Cline MCP: 导入失败 — {ex.Message}";
        }
    }

    /// <summary>将 MCP 服务器列表去重写入 mcp_servers.json</summary>
    private static async Task<string> WriteMcpServersAsync(List<JNode> imported, StringBuilder sb)
    {
        if (imported.Count == 0) return sb.ToString().Trim();

        var cwd = Environment.CurrentDirectory;
        var waycoderDir = Global.FindExistingConfigDir(cwd);
        string targetDir;
        if (waycoderDir != null)
        {
            targetDir = Path.Combine(cwd, waycoderDir);
        }
        else
        {
            targetDir = Path.Combine(cwd, ".waycoder");
            Directory.CreateDirectory(targetDir);
        }

        var mcpPath = Path.Combine(targetDir, "mcp_servers.json");
        var existing = JNode.Array();
        if (File.Exists(mcpPath))
        {
            try
            {
                var existingJson = Json.Parse(File.ReadAllText(mcpPath, Encoding.UTF8));
                if (existingJson is { Kind: JKind.Array } arr)
                {
                    foreach (var item in arr.Items)
                    {
                        var comment = item?["_comment"]?.AsString() ?? "";
                        if (!comment.Contains("示例"))
                            existing.Add(item!.Clone()!);
                    }
                }
            }
            catch { }
        }

        var existingNames = existing.Items
            .Select(e => e?["name"]?.AsString())
            .Where(n => n != null)
            .ToHashSet();

        foreach (var item in imported)
        {
            var itemName = item["name"]?.AsString();
            if (itemName != null && !existingNames.Contains(itemName))
                existing.Add(item);
        }

        var jsonStr = existing.ToJson(true);
        await File.WriteAllTextAsync(mcpPath, jsonStr, Encoding.UTF8);
        sb.AppendLine($"  📝 已写入 {imported.Count} 个服务器 → {mcpPath}");
        return sb.ToString().Trim();
    }

    /// <summary>
    /// 导入项目上下文。CLAUDE.md → prompt.md / 项目记忆。
    /// </summary>
    private static string ImportContext()
    {
        var claudeMd = FindClaudeMdInTree(Environment.CurrentDirectory);
        if (claudeMd == null)
            return "⏭ 项目上下文: 未在当前项目找到 CLAUDE.md";

        try
        {
            var content = File.ReadAllText(claudeMd, Encoding.UTF8);
            var cwd = Environment.CurrentDirectory;
            var waycoderDir = Global.FindExistingConfigDir(cwd) ?? ".waycoder";
            var targetDir = Path.Combine(cwd, waycoderDir);
            Directory.CreateDirectory(targetDir);

            // 写入 prompt.md
            var promptPath = Path.Combine(targetDir, "prompt.md");
            if (File.Exists(promptPath))
            {
                var existingContent = File.ReadAllText(promptPath, Encoding.UTF8);
                if (existingContent.Contains("CLAUDE.md 导入"))
                    return "⏭ 项目上下文: 已存在导入标记，跳过（避免重复导入）";

                // 追加到现有
                File.WriteAllText(promptPath, existingContent.TrimEnd() + "\n\n---\n\n## 从 Claude Code 导入 (CLAUDE.md)\n\n" + content, Encoding.UTF8);
                return $"✅ 项目上下文: 已追加 CLAUDE.md → {promptPath} ({content.Length} 字符)";
            }
            else
            {
                var header = $""""
                    # 项目提示词

                    > 📥 从 Claude Code 导入 (CLAUDE.md) — {DateTime.Now:yyyy-MM-dd HH:mm}

                    """";
                File.WriteAllText(promptPath, header + content, Encoding.UTF8);
                return $"✅ 项目上下文: 已创建 prompt.md ← CLAUDE.md ({content.Length} 字符)";
            }
        }
        catch (Exception ex)
        {
            return $"❌ 项目上下文: 导入失败 — {ex.Message}";
        }
    }

    /// <summary>
    /// 导入 Claude Code 会话数据。
    /// </summary>
    private static async Task<string> ImportSessionsAsync()
    {
        var sessionsDir = Path.Combine(ClaudeHome, "sessions");
        if (!Directory.Exists(sessionsDir))
            return "⏭ 会话: ~/.claude/sessions/ 不存在";

        try
        {
            var sessionFiles = Directory.GetFiles(sessionsDir, "*.json");
            if (sessionFiles.Length == 0)
                return "⏭ 会话: 无会话文件";

            var targetDir = Global.GlobalConfigPath("sessions");
            Directory.CreateDirectory(targetDir);

            var imported = 0;
            var skipped = 0;

            foreach (var file in sessionFiles.Take(20)) // 最多导入 20 个
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetPath = Path.Combine(targetDir, $"claude_{fileName}.json");

                    if (File.Exists(targetPath))
                    {
                        skipped++;
                        continue;
                    }

                    // Claude Code session 格式 → WayCoder 格式（简化）
                    var json = Json.Parse(await File.ReadAllTextAsync(file, Encoding.UTF8));
                    if (json == null) { skipped++; continue; }

                    // 尝试提取消息
                    var messages = json["messages"];
                    if (messages == null)
                    {
                        // 可能整个文件就是消息数组
                        messages = json;
                    }

                    if (messages != null && messages.Count > 0)
                    {
                        // 简化：直接保存原格式，WayCoder 能读取
                        await File.WriteAllTextAsync(targetPath,
                            json.ToJson(true), Encoding.UTF8);
                        imported++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch
                {
                    skipped++;
                }
            }

            return $"✅ 会话: 导入 {imported} 个，跳过 {skipped} 个 → {targetDir}";
        }
        catch (Exception ex)
        {
            return $"❌ 会话: 导入失败 — {ex.Message}";
        }
    }

    /// <summary>
    /// 导入权限规则。
    /// </summary>
    private static string ImportPermissions()
    {
        var projectClaudeDir = FindProjectClaudeDir(Environment.CurrentDirectory);
        if (projectClaudeDir == null)
            return "⏭ 权限: 未找到项目 .claude/ 目录";

        var localSettings = Path.Combine(projectClaudeDir, "settings.local.json");
        if (!File.Exists(localSettings))
            return "⏭ 权限: 无 settings.local.json";

        try
        {
            var json = Json.Parse(File.ReadAllText(localSettings, Encoding.UTF8));
            var perms = json?["permissions"]?["allow"];
            if (perms == null || perms.Count == 0)
                return "⏭ 权限: 无 allow 规则";

            var sb = new StringBuilder();
            sb.AppendLine($"🔑 权限规则: {perms.Count} 条");
            sb.AppendLine("  ⚠ 权限规则格式不同，已列出供手动设置:");
            foreach (var perm in perms.Items)
            {
                sb.AppendLine($"    /perm add {perm.AsString() ?? perm.ToJson()}");
            }
            sb.AppendLine("  💡 在 WayCoder 中使用 /perm yolo 可跳过所有确认");

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"❌ 权限: 导入失败 — {ex.Message}";
        }
    }

    // ── 辅助方法 ──

    /// <summary>向上搜索 CLAUDE.md</summary>
    private static string? FindClaudeMdInTree(string cwd)
    {
        var dir = cwd;
        while (dir != null)
        {
            var path = Path.Combine(dir, "CLAUDE.md");
            if (File.Exists(path)) return path;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>向上搜索 project .claude/ 目录</summary>
    private static string? FindProjectClaudeDir(string cwd)
    {
        var dir = cwd;
        while (dir != null)
        {
            var path = Path.Combine(dir, ".claude");
            if (Directory.Exists(path)) return path;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>向上搜索目录中的文件。FindInTree(cwd, ".cursor", "mcp.json") → 完整路径或 null</summary>
    private static string? FindInTree(string cwd, string dirName, string? fileName = null)
    {
        var dir = cwd;
        while (dir != null)
        {
            if (fileName != null)
            {
                var path = Path.Combine(dir, dirName, fileName);
                if (File.Exists(path)) return path;
            }
            else
            {
                var path = Path.Combine(dir, dirName);
                if (File.Exists(path)) return path;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>去除 JSONC 注释（// 和 /* */），返回纯 JSON 字符串</summary>
    internal static string StripJsonComments(string jsonc)
    {
        var result = new StringBuilder();
        var inString = false;
        var inBlockComment = false;
        var inLineComment = false;

        for (int i = 0; i < jsonc.Length; i++)
        {
            var ch = jsonc[i];
            var next = i + 1 < jsonc.Length ? jsonc[i + 1] : '\0';

            if (inBlockComment)
            {
                if (ch == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }
            if (inLineComment)
            {
                if (ch == '\n' || ch == '\r') { inLineComment = false; result.Append(ch); }
                continue;
            }
            if (inString)
            {
                result.Append(ch);
                if (ch == '\\' && next != '\0') { result.Append(next); i++; }
                else if (ch == '"') inString = false;
                continue;
            }
            if (ch == '"')
            {
                inString = true;
                result.Append(ch);
                continue;
            }
            if (ch == '/' && next == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }
            if (ch == '/' && next == '/')
            {
                inLineComment = true;
                i++;
                continue;
            }
            result.Append(ch);
        }

        return result.ToString();
    }

    internal static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
        };
    }
}
