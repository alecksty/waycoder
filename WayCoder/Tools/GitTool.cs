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

        // 安全检查 2：危险操作（按 token 精确匹配，flag 挪到分支名/远端名后也无法绕过）
        if (HasBlockedGitOperation(command))
            return "⚠ 已阻止：检测到危险 git 操作（force push / hard reset / clean -f / checkout -- . / stash drop / branch -D），请手动执行或使用更安全的替代方式。";

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
                result = ContextManager.TruncateByRunes(result, 6000) + $"\n... (已截断，共 {result.Length} 字符) ...\n" + ContextManager.TruncateTailByRunes(result, 1000);

            return string.IsNullOrWhiteSpace(result) ? "（无输出）" : result.Trim();
        }
        catch (Exception ex)
        {
            return $"Git 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 判断 git 命令是否含危险操作（force push / hard reset / clean -f 等）。
    /// 旧实现用整串子串匹配（command.Contains("push --force")），flag 挪到分支名之后
    /// （如 `push origin main --force`、`reset HEAD --hard`）即可绕过。改为按 token
    /// 精确匹配：子命令（大小写不敏感）+ 危险 flag（区分大小写，保留 -D vs -d 语义）。
    /// 纯逻辑，便于自测。
    /// </summary>
    internal static bool HasBlockedGitOperation(string command)
    {
        var tokens = command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;

        bool HasSub(string sub) => tokens.Any(t => string.Equals(t, sub, StringComparison.OrdinalIgnoreCase));
        // git flag 区分大小写：-D（强制删除）与 -d（普通删除）语义不同，须精确匹配
        bool HasFlag(string flag) => tokens.Any(t => t == flag);

        if (HasSub("push") && (HasFlag("--force") || HasFlag("-f"))) return true;
        if (HasSub("reset") && HasFlag("--hard")) return true;
        if (HasSub("clean") && (HasFlag("-f") || HasFlag("-fd") || HasFlag("--force"))) return true;
        if (HasSub("checkout") && HasFlag("--") && HasFlag(".")) return true;
        if (HasSub("stash") && HasSub("drop")) return true;
        if (HasSub("branch") && HasFlag("-D")) return true;
        return false;
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
