namespace WayCoder;

/// <summary>
/// 工具风险单一事实源。
/// PermissionManager 的确认名单与 AutoModeClassifier 的三级风险分类统一从这里读取，
/// 避免同一工具在权限层/智能分类层维护两份可能不一致的集合。
/// </summary>
public static class ToolSafetyRegistry
{
    public enum ToolRisk
    {
        Safe,
        Cautious,
        Dangerous,
    }

    /// <summary>SmartAuto 三级分类：Safe 自动放行 / Cautious 首次确认后记住 / Dangerous 每次确认。</summary>
    private static readonly Dictionary<string, ToolRisk> ToolRisks = new(StringComparer.OrdinalIgnoreCase)
    {
        // 只读 / 分析 / 查询
        ["read_file"] = ToolRisk.Safe,
        ["ls"] = ToolRisk.Safe,
        ["grep"] = ToolRisk.Safe,
        ["glob"] = ToolRisk.Safe,
        ["stat"] = ToolRisk.Safe,
        ["pwd"] = ToolRisk.Safe,
        ["wc"] = ToolRisk.Safe,
        ["diff"] = ToolRisk.Safe,
        ["tree"] = ToolRisk.Safe,

        // 任务管理（只改内部任务清单）
        ["todo"] = ToolRisk.Safe,
        ["task_create"] = ToolRisk.Safe,
        ["task_update"] = ToolRisk.Safe,
        ["task_list"] = ToolRisk.Safe,
        ["task_get"] = ToolRisk.Safe,

        // LSP / lint / 外部只读查询
        ["lsp"] = ToolRisk.Safe,
        ["lint"] = ToolRisk.Safe,
        ["fetch"] = ToolRisk.Safe,
        ["web_search"] = ToolRisk.Safe,
        ["doc"] = ToolRisk.Safe,
        ["transcribe"] = ToolRisk.Safe,

        // 记忆 / 技能 / 系统查看 / 用户交互 / 后台查询
        ["memory"] = ToolRisk.Safe,
        ["skill"] = ToolRisk.Safe,
        ["ps"] = ToolRisk.Safe,
        ["ask_user_question"] = ToolRisk.Safe,
        ["job_output"] = ToolRisk.Safe,
        ["view_image"] = ToolRisk.Safe,

        // 文件/项目修改
        ["write_file"] = ToolRisk.Cautious,
        ["edit_file"] = ToolRisk.Cautious,
        ["notebook_edit"] = ToolRisk.Cautious,
        ["multiedit"] = ToolRisk.Cautious,
        ["find_replace"] = ToolRisk.Cautious,
        ["mkdir"] = ToolRisk.Cautious,
        ["cp"] = ToolRisk.Cautious,
        ["mv"] = ToolRisk.Cautious,
        ["cd"] = ToolRisk.Cautious,
        ["struct_todo"] = ToolRisk.Cautious,
        ["export"] = ToolRisk.Cautious,
        ["screenshot"] = ToolRisk.Cautious,
        ["draw"] = ToolRisk.Cautious,
        ["image_convert"] = ToolRisk.Cautious,

        // 破坏性 / 外部执行 / 子智能体
        ["rm"] = ToolRisk.Dangerous,
        ["bash"] = ToolRisk.Dangerous,
        ["git"] = ToolRisk.Dangerous,
        ["git_pr"] = ToolRisk.Dangerous,
        ["kill"] = ToolRisk.Dangerous,
        ["agent"] = ToolRisk.Dangerous,
        ["download"] = ToolRisk.Dangerous,
        ["job_kill"] = ToolRisk.Dangerous,
        ["sqlite"] = ToolRisk.Dangerous,
        ["test"] = ToolRisk.Dangerous,
    };

    public static bool RequiresConfirmation(string toolName) => ClassifyRisk(toolName) != ToolRisk.Safe;

    public static ToolRisk ClassifyRisk(string toolName)
        => ToolRisks.TryGetValue(toolName, out var risk) ? risk : ToolRisk.Dangerous;

    public static int CountRisk(ToolRisk risk)
    {
        var count = 0;
        foreach (var value in ToolRisks.Values)
            if (value == risk) count++;
        return count;
    }

    public static IReadOnlyCollection<string> ConfirmationToolNames
    {
        get
        {
            var names = new List<string>();
            foreach (var kv in ToolRisks)
                if (kv.Value != ToolRisk.Safe) names.Add(kv.Key);
            return names;
        }
    }
}
