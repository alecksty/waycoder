using WayCoder.Infra;

namespace WayCoder.Maui.Services;

/// <summary>
/// 手机端模式记忆：工作模式 / 权限模式 / 经济模式 持久化到 Global.Home/modes.json。
/// 手机没有快捷键，每次切换麻烦——记住上次选择的模式，下次启动直接恢复。
/// </summary>
public static class MauiModeStore
{
    private static string StorePath => Path.Combine(WayCoder.Global.Home, "modes.json");

    /// <summary>读取上次保存的模式；文件缺失/损坏返回 null（调用方用默认值）。</summary>
    public static (WorkMode Work, PermissionManager.Mode Perm, EconomyMode Economy)? Load()
    {
        try
        {
            if (!System.IO.File.Exists(StorePath)) return null;
            var root = Json.Parse(System.IO.File.ReadAllText(StorePath));
            if (root == null) return null;
            var work = Enum.TryParse<WorkMode>(root.GetString("work"), out var w) ? w : WorkMode.Build;
            var perm = Enum.TryParse<PermissionManager.Mode>(root.GetString("perm"), out var p) ? p : PermissionManager.Mode.Ask;
            var eco = Enum.TryParse<EconomyMode>(root.GetString("economy"), out var e) ? e : EconomyMode.Off;
            return (work, perm, eco);
        }
        catch { return null; }
    }

    /// <summary>保存当前三种模式（下次启动恢复）。</summary>
    public static void Save(WorkMode work, PermissionManager.Mode perm, EconomyMode economy)
    {
        try
        {
            var root = JNode.Object();
            root.Set("work", work.ToString());
            root.Set("perm", perm.ToString());
            root.Set("economy", economy.ToString());
            System.IO.File.WriteAllText(StorePath, root.ToJson());
        }
        catch { /* 保存失败不崩溃 */ }
    }
}
