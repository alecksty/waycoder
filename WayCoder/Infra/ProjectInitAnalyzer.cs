using System.Text;

namespace WayCoder;

/// <summary>init 上下文 —— 程序化收集的代码库快照，喂给 LLM 分析。</summary>
public sealed record InitContext(
    string ProjectInfo,
    string Commands,
    string RepoMap,
    string ExistingRules,
    string ReadmeHead,
    string ExistingTarget,
    string GitStatus);

/// <summary>
/// LLM 驱动的项目初始化分析器 —— 收集代码库上下文，组装 init 提示词。
///
/// 对标 Crush initialize.md.tpl（渐进披露：只写非显然知识、绝不虚构）
/// 与 Claude Code init.ts（命令/架构/合并已有规则、不写显然指令）。
/// 纯逻辑、无 IO 副作用以外的网络调用，便于自测；LLM 调用由 InitCommand 负责。
/// </summary>
public static class ProjectInitAnalyzer
{
    const int PerFileMax = 4_000;      // 单规则文件截断（对齐 ProjectContext.LoadInstructions）
    const int ReadmeMax = 4_000;       // README 只取头部
    const int TargetMax = 8_000;       // 已有 AGENT.md/CLAUDE.md
    const int RepoMapMax = 20_000;     // 仓库地图
    const int RulesMax = 20_000;       // 全部规则合并上限
    const int TotalContextMax = 60_000; // 整体提示词兜底（指令常驻头部）

    // ════════════════════════════════════════════════════════════
    // 提示词模板（原始字符串，无 $ 前缀避免花括号插值问题；占位符 .Replace() 注入）
    // ════════════════════════════════════════════════════════════
    const string InitTemplate = """
你是一名资深的软件架构师。请分析下面提供的代码库上下文，为这个仓库撰写一份 {FILE_NAME} 指导文件。

本文件会被注入到 AI 编程助手（WayCoder 道码 / Claude Code）的系统提示词中，指导它在此仓库中高效、安全地工作。它是给 AI 看的「工作须知」，不是给人看的 README。

## 内容标准（严格遵守）

1. 【只写非显然的知识】优先记录代码库的「坑」、隐式约定、意外标志、测试怪癖、目录怪癖、命名风格、禁止事项。不要写泛泛的通用建议（如「写清晰注释」「提交前先测试」「遵循最佳实践」）。
2. 【绝不虚构】只能依据下面给出的上下文（项目检测、常用命令、仓库地图、已有规则、README、Git 状态）写作。上下文未提到的信息不要脑补；宁可省略，也不要编造。
3. 【命令要准确】构建/测试/lint 命令直接采用「常用命令」区块中给出的命令，不要自己猜测或改写。
4. 【架构要真实】依据「仓库地图」中的核心文件、文件树与符号信息写高层架构：核心模块、关键文件、大致数据流/分层。不要编造不存在的模块或目录。
5. 【合并已有规则】「已有规则/指令文件」中仍有价值的内容应合并保留（去重、去冲突）。「已有目标文件内容」保留其中真实有用的部分；与分析结果冲突时以新分析为准。
6. 【渐进披露】只写「不读源码就看不出」的知识。显而易见的（语言名、框架名）一句带过即可。
7. 【不要占位符】不要出现「在此补充」「待补充」「TODO」等占位符。不确定的章节直接省略，不要留空。

## 输出格式

直接输出 {FILE_NAME} 的完整内容（Markdown）。不要输出任何解释、前言、后记或代码围栏。

- 第一行必须是：# {FILE_NAME}
- 建议结构（可按需增删）：
  - 项目概述（1-2 行：语言 / 框架 / 定位）
  - 常用命令（构建 / 测试 / lint，代码块）
  - 架构（核心模块与关键文件，基于仓库地图）
  - 约定与规范（真实存在的：命名、目录、提交、代码风格）
  - 注意事项（坑、隐式约定、意外标志、测试怪癖）
- 正文用中文（命令与代码标识符保留原文）。
- 长度：精炼优先，通常 60~200 行；不要注水。

# 代码库上下文

## 项目检测
{PROJECT_INFO}

## 常用命令
{COMMANDS}

## 仓库地图
{REPO_MAP}

## 已有规则/指令文件
{EXISTING_RULES}

## README 摘要（仅头部）
{README_HEAD}

## 已有目标文件内容（可能为空；用于改进而非推翻）
{EXISTING_TARGET}

## Git 状态
{GIT_STATUS}
""";

    /// <summary>LLM 可用性判定（null 则降级静态模板）。</summary>
    public static bool ShouldUseLlm(LLM? llm) => llm != null;

    /// <summary>收集代码库上下文（各节独立截断，预算可控）。</summary>
    public static InitContext CollectInitContext(ProjectInfo info, string fileName)
    {
        var root = info.ProjectRoot;
        var commands = string.Join("\n", ProjectInitializer.DetectCommands(root));

        string repoMap;
        try { repoMap = RepoMapGenerator.Generate(root, forceRefresh: true); }
        catch (Exception ex)
        {
            DebugLog.Log("init", $"RepoMap 生成失败: {ex.Message}");
            repoMap = "";
        }

        var existingRules = ContextManager.TruncateByRunes(
            CollectExistingRules(root, fileName), RulesMax);
        var targetPath = Path.Combine(root, fileName);
        var targetContent = File.Exists(targetPath)
            ? ReadHeadSafe(targetPath, TargetMax) : "";

        return new InitContext(
            ProjectInfo: ContextManager.TruncateByRunes(info.ToMarkdown(), 2_000),
            Commands: ContextManager.TruncateByRunes(commands, 2_000),
            RepoMap: ContextManager.TruncateByRunes(repoMap, RepoMapMax),
            ExistingRules: existingRules,
            ReadmeHead: ReadHeadSafe(Path.Combine(root, "README.md"), ReadmeMax),
            ExistingTarget: targetContent,
            GitStatus: CollectGitStatus(info));
    }

    /// <summary>组装提示词（指令在前、上下文在后；尾部兜底截断不裁指令）。</summary>
    public static string BuildPrompt(string fileName, InitContext ctx)
    {
        var prompt = InitTemplate
            .Replace("{FILE_NAME}", fileName)
            .Replace("{PROJECT_INFO}", ctx.ProjectInfo)
            .Replace("{COMMANDS}", ctx.Commands)
            .Replace("{REPO_MAP}", ctx.RepoMap)
            .Replace("{EXISTING_RULES}", ctx.ExistingRules)
            .Replace("{README_HEAD}", ctx.ReadmeHead)
            .Replace("{EXISTING_TARGET}", ctx.ExistingTarget)
            .Replace("{GIT_STATUS}", ctx.GitStatus);
        return ContextManager.TruncateByRunes(prompt, TotalContextMax);
    }

    /// <summary>剥掉 LLM 可能包的外层 ```markdown / ``` 围栏。</summary>
    public static string CleanFenced(string content)
    {
        var s = content.Trim();
        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var newlineIdx = s.IndexOf('\n');
            if (newlineIdx > 0)
                s = s[(newlineIdx + 1)..];
            else
                s = s[3..];
        }
        if (s.EndsWith("```", StringComparison.Ordinal))
            s = s[..^3];
        return s.Trim();
    }

    /// <summary>降级内容：委托现有静态模板（无 LLM / 调用失败时）。</summary>
    public static string FallbackContent(ProjectInfo info, string fileName)
        => ProjectInitializer.GenerateAgentMd(info, fileName);

    // ════════════════════════════════════════════════════════════
    // 上下文收集
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 收集已有规则文件：单文件（.cursorrules / copilot-instructions / AGENTS.md / 非目标文件）
    /// + 目录规则（.claude/.waycoder/.corecoder/*.md，跳过 memory.md）。逐文件截断 4000 runes。
    /// </summary>
    static string CollectExistingRules(string root, string targetFileName)
    {
        var sb = new StringBuilder();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddFile(string path, string label)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            var full = Path.GetFullPath(path);
            if (!seen.Add(full)) return;
            try
            {
                var content = File.ReadAllText(full);
                sb.AppendLine($"## {label}");
                if (content.Length > PerFileMax)
                    sb.AppendLine(ContextManager.TruncateByRunes(content, PerFileMax) + "\n... (已截断)");
                else
                    sb.AppendLine(content);
                sb.AppendLine();
            }
            catch (Exception ex) { DebugLog.Log("init", $"读取规则文件失败 {path}: {ex.Message}"); }
        }

        // 单文件规则
        AddFile(Path.Combine(root, ".cursorrules"), ".cursorrules");
        AddFile(Path.Combine(root, ".github", "copilot-instructions.md"), ".github/copilot-instructions.md");
        AddFile(Path.Combine(root, "AGENTS.md"), "AGENTS.md");
        // 非目标文件：生成 AGENT.md 时补读 CLAUDE.md，反之亦然（供合并）
        AddFile(Path.Combine(root, targetFileName.Equals("CLAUDE.md", StringComparison.OrdinalIgnoreCase)
            ? "AGENT.md" : "CLAUDE.md"), "已有目标文件的另一形式");

        // 目录规则
        foreach (var dir in new[] { ".claude", ".waycoder", ".corecoder" })
        {
            var dirPath = Path.Combine(root, dir);
            if (!Directory.Exists(dirPath)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dirPath, "*.md", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(file);
                    if (name.Equals("memory.md", StringComparison.OrdinalIgnoreCase)) continue;
                    AddFile(file, $"{dir}/{name}");
                }
            }
            catch (Exception ex) { DebugLog.Log("init", $"枚举规则目录失败 {dirPath}: {ex.Message}"); }
        }

        return sb.ToString();
    }

    /// <summary>Git 状态：分支/remote（零开销）+ best-effort 变更文件数。</summary>
    static string CollectGitStatus(ProjectInfo info)
    {
        var sb = new StringBuilder();
        if (info.GitBranch != null) sb.AppendLine($"- 分支: {info.GitBranch}");
        if (info.GitRemote != null) sb.AppendLine($"- Remote: {info.GitRemote}");

        // best-effort：git status --porcelain 统计变更数（失败静默）
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", "status --porcelain")
            {
                WorkingDirectory = info.ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(3000);
                var changed = output.Split('\n').Count(l => l.Trim().Length > 0);
                if (changed > 0) sb.AppendLine($"- 未提交变更: {changed} 个文件");
            }
        }
        catch (Exception ex) { DebugLog.Log("init", $"git status 失败: {ex.Message}"); }

        return sb.Length > 0 ? sb.ToString() : "- 非 Git 仓库";
    }

    /// <summary>安全读取文件头部（不存在/读失败返回空串）。</summary>
    static string ReadHeadSafe(string path, int maxRunes)
    {
        try
        {
            if (!File.Exists(path)) return "";
            var content = File.ReadAllText(path);
            return content.Length > maxRunes
                ? ContextManager.TruncateByRunes(content, maxRunes) + "\n... (已截断)"
                : content;
        }
        catch (Exception ex) { DebugLog.Log("init", $"读取失败 {path}: {ex.Message}"); return ""; }
    }
}
