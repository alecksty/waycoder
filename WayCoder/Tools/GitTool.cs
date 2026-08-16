using System.Diagnostics;

namespace WayCoder.Tools;

/// <summary>
/// Git 操作工具 —— 安全封装常用 git 命令。
/// 输出自动截断，禁止 force push / hard reset 等危险操作。
/// </summary>
public class GitTool : ITool, ICancellableTool
{
    public string Name => "git";
    public string Description => "执行 Git 操作：status、log、diff、add、commit、branch、blame。自动检测仓库根目录。禁止 force push / hard reset。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("command", JNode.Object()
                .Set("type", "string")
                .Set("description", "Git 子命令及参数，如 'status'、'log --oneline -10'、'diff HEAD~1'、'add src/'、'commit -m \"msg\"'")))
        .Set("required", JNode.Array().Add("command"));

    private static readonly HashSet<string> BlockedPatterns =
        ["push --force", "push -f", "reset --hard", "clean -f", "clean -fd",
         "checkout -- .", "stash drop", "branch -D"];

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
        => await ExecuteAsync(arguments, CancellationToken.None);

    /// <summary>可取消执行（ICancellableTool）：中断时杀掉 git 子进程。</summary>
    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        return await Execute(command, cancellationToken);
    }

    private static async Task<string> Execute(string command, CancellationToken cancellationToken)
    {
        // 安全检查 1：命令注入拦截 —— git -c/--config 可设 alias.*='!cmd'、core.pager、core.sshCommand
        // 等，使 git 内部经 shell 执行任意命令（完全绕过 BashGuard 与权限确认）。
        if (HasDangerousGitArgs(command))
            return "⚠ 已阻止：git 配置注入（-c/--config/--upload-pack/--receive-pack/--exec 可执行任意命令），请勿使用这些参数。";

        // 安全检查 2：危险操作
        foreach (var blocked in BlockedPatterns)
        {
            if (command.Contains(blocked, StringComparison.OrdinalIgnoreCase))
                return $"⚠ 已阻止：'{blocked}' 是危险操作，请手动执行或使用更安全的替代方式。";
        }

        try
        {
            var (exitCode, outStr, errStr) = await GitRunner.RunAsync(command, null, cancellationToken);

            var result = outStr;
            if (!string.IsNullOrEmpty(errStr))
                result += $"\n[stderr]\n{errStr}";
            if (exitCode != 0)
                result += $"\n[退出码：{exitCode}]";

            // 截断长输出
            if (result.Length > 8000)
                result = result[..6000] + $"\n... (已截断，共 {result.Length} 字符) ...\n" + result[^1000..];

            return string.IsNullOrWhiteSpace(result) ? "（无输出）" : result.Trim();
        }
        catch (Exception ex)
        {
            return $"Git 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 判断 git 命令是否含命令注入参数。git 的 `-c`/`--config` 可设置
    /// `alias.*='!cmd'`、`core.pager`、`core.sshCommand` 等配置，使 git 内部
    /// 经 shell 执行任意命令（安全 agent 实测 `git -c alias.x='!echo PWNED_$(id -u)' x`）。
    /// 纯逻辑，便于自测。
    /// </summary>
    internal static bool HasDangerousGitArgs(string command)
    {
        foreach (var token in command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token is "-c" or "--config" or "--config-env" or "--upload-pack" or "--receive-pack" or "--exec")
                return true;
            if (token.StartsWith("-c=", StringComparison.Ordinal)
                || token.StartsWith("--config=", StringComparison.Ordinal)
                || token.StartsWith("--config-env=", StringComparison.Ordinal)
                || token.StartsWith("--upload-pack=", StringComparison.Ordinal)
                || token.StartsWith("--receive-pack=", StringComparison.Ordinal)
                || token.StartsWith("--exec=", StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
