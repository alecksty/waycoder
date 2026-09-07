namespace WayCoder;

/// <summary>
/// 斜杠命令匹配的唯一实现：精确匹配主名/别名，或前缀 "Name "/"Alias "（取余下为参数）。
/// 主工程 <c>SlashCommandRegistry</c>、GUI <c>CoreStubs</c> 注册表、Web <c>WebCommands</c> 三处共用，
/// 避免复制漂移（此前别名约定不一致导致无斜杠别名永不匹配的回归）。
/// GUI / MAUI 项目会编译本文件并各自 resolve 到自己项目命名的 <c>ISlashCommand</c>（同名接口），直接可用。
/// </summary>
public static class SlashMatcher
{
    /// <summary>
    /// 提取参数：未匹配返回 null；已匹配返回去掉命令名/别名前缀并 Trim 后的参数（精确匹配时为空串）。
    /// </summary>
    public static string? ExtractArgs(ISlashCommand cmd, string input)
    {
        if (string.Equals(input, cmd.Name, StringComparison.OrdinalIgnoreCase)) return "";

        var nameSpace = cmd.Name + " ";
        if (input.StartsWith(nameSpace, StringComparison.OrdinalIgnoreCase))
            return input[nameSpace.Length..].Trim();

        foreach (var alias in cmd.Aliases)
        {
            if (string.Equals(input, alias, StringComparison.OrdinalIgnoreCase)) return "";
            var aliasSpace = alias + " ";
            if (input.StartsWith(aliasSpace, StringComparison.OrdinalIgnoreCase))
                return input[aliasSpace.Length..].Trim();
        }

        return null;
    }
}
