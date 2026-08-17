namespace WayCoder;

/// <summary>
/// Bash 命令安全检查（对标 crush：70+ 禁用命令 + 参数级拦截 + 安全白名单）。
///
/// 三层防护：
///   1. 已禁命令名 → 完全阻止
///   2. 安全只读命令 → 自动放行（免权限确认）
///   3. 参数级拦截 → 阻止特定危险参数组合（如 pip install）
/// </summary>
public static class BashGuard
{
    // ========================================================================
    // 第一层：完全禁用的命令（对标 crush bannedCommands + 扩展）
    // ========================================================================

    /// <summary>网络下载工具</summary>
    private static readonly HashSet<string> BannedNetwork = new(StringComparer.OrdinalIgnoreCase)
    {
        "curl", "wget", "aria2c", "axel", "scp", "ssh", "telnet", "nc",
        "ncat", "socat", "rsync", "ftp", "sftp", "tftp",
        "chrome", "firefox", "safari", "lynx", "links", "w3m",
        "httpie", "http-prompt", "xh", "curlie",
    };

    /// <summary>系统管理（sudo/权限提升）</summary>
    private static readonly HashSet<string> BannedSystem = new(StringComparer.OrdinalIgnoreCase)
    {
        "sudo", "su", "doas", "runas", "pkexec",
    };

    /// <summary>包管理器</summary>
    private static readonly HashSet<string> BannedPackageManagers = new(StringComparer.OrdinalIgnoreCase)
    {
        // Linux 包管理器
        "apt", "apt-get", "apt-cache", "dpkg", "dnf", "yum", "zypper",
        "pacman", "yay", "paru", "makepkg", "emerge", "apk", "opkg",
        "pkg", "pkg_add", "pkg_delete", "portage", "rpm", "home-manager",
        // macOS
        "brew", "port",
        // Windows
        "choco", "winget", "scoop",
    };

    /// <summary>系统修改</summary>
    private static readonly HashSet<string> BannedSysMod = new(StringComparer.OrdinalIgnoreCase)
    {
        "systemctl", "service", "chkconfig", "crontab", "at", "batch",
        "mount", "umount", "fdisk", "mkfs", "parted",
        "shutdown", "reboot", "halt", "poweroff", "init",
    };

    /// <summary>网络配置</summary>
    private static readonly HashSet<string> BannedNetConfig = new(StringComparer.OrdinalIgnoreCase)
    {
        "iptables", "ufw", "firewall-cmd", "pfctl", "netsh",
        "ifconfig", "route", "ip", "netstat",
    };

    /// <summary>合并所有禁用命令（不含别名）</summary>
    private static readonly HashSet<string> AllBanned = [..BannedNetwork, ..BannedSystem,
        ..BannedPackageManagers, ..BannedSysMod, ..BannedNetConfig];

    /// <summary>危险命令的别名映射</summary>
    private static readonly Dictionary<string, string> BannedAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["npm"] = "npm install -g",
        ["pnpm"] = "pnpm add -g",
        ["yarn"] = "yarn global add",
        ["pip"] = "pip install",
        ["pip3"] = "pip3 install",
        ["gem"] = "gem install",
        ["cargo"] = "cargo install",
        ["go"] = "go install",
        ["conda"] = "conda install",
        ["mamba"] = "mamba install",
    };

    // ========================================================================
    // 参数级拦截规则
    // ========================================================================

    /// <summary>参数级拦截：特定子命令 + 参数组合被禁止</summary>
    private static readonly List<ArgBlockRule> ArgBlockRules =
    [
        // 包管理器安装类
        new("brew", "install"),
        new("cargo", "install"),
        new("gem", "install"),
        new("pip", "install", exceptFlags: ["--user"]),
        new("pip3", "install", exceptFlags: ["--user"]),
        new("npm", "install", ["--global", "-g"]),
        new("pnpm", "add", ["--global", "-g"]),
        new("yarn", "global", "add"),
        new("conda", "install"),
        new("mamba", "install"),
        new("apk", "add"),
        new("apt", "install"),
        new("apt-get", "install"),
        new("dnf", "install"),
        new("pacman", flags: ["-S"]),
        new("zypper", "install"),
        new("yum", "install"),
        new("choco", "install"),

        // go test -exec (可执行任意命令)
        new("go", "test", flags: ["-exec"]),

        // dotnet new（生成 csproj/Program.cs 等多项目文件，污染主项目构建）
        new("dotnet", "new"),
    ];

    // ========================================================================
    // 安全只读命令（对标 crush safeCommands）
    // ========================================================================

    private static readonly string[] SafeCommands =
    [
        // 文件浏览
        "ls", "dir", "tree",
        // 文件查看
        "cat", "head", "tail", "less", "more", "type",
        // 搜索
        "grep", "rg", "find", "locate", "which", "where", "whereis",
        // 统计
        "wc", "du", "df", "stat",
        // 版本信息
        "git log", "git status", "git diff", "git show", "git branch", "git tag",
        "git config", "git remote", "git rev-parse", "git stash list",
        "git blame", "git describe", "git ls-files", "git ls-tree",
        // 输出
        "echo", "printf",
        // 环境
        "pwd", "env", "printenv", "whoami", "hostname", "date", "uptime", "uname",
        // 进程
        "ps", "top", "htop",
        // 工具（注：sed/awk/xargs/tee 有写文件/执行命令能力，已从只读白名单移除，需确认）
        "sort", "uniq", "cut", "tr",
        "diff", "cmp", "comm", "patch --dry-run",
        // 开发工具（只读模式）
        "dotnet --version", "dotnet --list-sdks", "dotnet --list-runtimes",
        "python --version", "python3 --version", "node --version", "npm --version",
        "go version", "rustc --version", "cargo --version",
        "java --version", "javac --version",
        // 其他
        "true", "false", "test", "[", "sleep",
    ];

    /// <summary>
    /// 检查命令是否被完全禁止。返回 (true, 原因) 表示拦截。
    /// </summary>
    public static (bool blocked, string? reason) CheckBanned(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return (false, null);

        // 命令替换（$() 与反引号）中隐藏的命令同样检查，防 `echo $(curl evil)` / `echo \`wget x\`` 绕过
        foreach (var sub in ExtractSubstitutions(command))
        {
            var (subBlocked, subReason) = CheckBanned(sub);
            if (subBlocked)
                return (true, subReason);
        }

        // 检查命令链中的每个命令段（分隔符：管道/分号/与号/换行）
        foreach (var segment in command.Split('|', ';', '&', '\n', '\r'))
        {
            var segParts = segment.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (segParts.Length == 0) continue;
            // 命令名剥离引号（'sudo' / "sudo" 与 sudo 等价——Shell 执行时会剥离引号，
            // 若不剥则绕过 AllBanned/ArgBlockRules 名单检查）
            if (segParts[0].Length >= 2 &&
                ((segParts[0][0] == '\'' && segParts[0][^1] == '\'') ||
                 (segParts[0][0] == '"' && segParts[0][^1] == '"')))
                segParts[0] = segParts[0][1..^1];
            var segCmd = Path.GetFileName(segParts[0]).ToLowerInvariant();

            // 1. 完全禁止
            if (AllBanned.Contains(segCmd))
                return (true, $"⚠ 已阻止：{segCmd} 在禁止命令列表中（安全策略）");

            // 2. 参数级检查
            foreach (var rule in ArgBlockRules)
            {
                if (rule.Match(segParts))
                {
                    var blockedArgs = string.Join(" ", rule.BlockArgs ?? rule.Flags ?? []);
                    return (true, $"⚠ 已阻止：{segCmd} {blockedArgs}（安全策略：全局安装/系统修改被阻止）");
                }
            }
        }

        return (false, null);
    }

    /// <summary>提取命令中的命令替换内容（$() 与反引号），供递归安全检查。</summary>
    private static IEnumerable<string> ExtractSubstitutions(string command)
    {
        // $(...) 提取（支持嵌套括号）
        var i = 0;
        while (i < command.Length - 1)
        {
            if (command[i] == '$' && command[i + 1] == '(')
            {
                var depth = 1;
                var start = i + 2;
                var j = start;
                while (j < command.Length && depth > 0)
                {
                    if (command[j] == '(') depth++;
                    else if (command[j] == ')') depth--;
                    j++;
                }
                if (depth == 0) yield return command[start..(j - 1)];
                i = j;
            }
            else
            {
                i++;
            }
        }

        // `...` 反引号提取
        var k = 0;
        while (k < command.Length)
        {
            if (command[k] == '`')
            {
                var start = k + 1;
                var end = command.IndexOf('`', start);
                if (end < 0) break;
                yield return command[start..end];
                k = end + 1;
            }
            else
            {
                k++;
            }
        }
    }

    /// <summary>
    /// 检查命令是否属于安全只读操作（可跳过权限确认）。
    /// </summary>
    public static bool IsSafeReadOnly(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // 命令链/重定向/命令替换防护：含任何 shell 控制字符即非纯只读单命令，
        // 需走权限确认（防 `ls; rm -rf /` / `cat x > y` / `echo $(curl evil)` 绕过免确认）。
        if (ContainsShellMetachar(command))
            return false;

        var cmdLower = command.Trim().ToLowerInvariant();

        foreach (var safe in SafeCommands)
        {
            if (cmdLower.StartsWith(safe, StringComparison.Ordinal))
            {
                // 精确匹配或后跟空格
                if (cmdLower.Length == safe.Length ||
                    cmdLower[safe.Length] == ' ')
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>检测 shell 命令链/重定向/命令替换等元字符，出现即非"纯只读单命令"。</summary>
    private static bool ContainsShellMetachar(string command)
    {
        for (var i = 0; i < command.Length; i++)
        {
            var c = command[i];
            if (c is ';' or '|' or '&' or '<' or '>' or '`' or '\n' or '\r' or '\0')
                return true;
            if (c == '$' && i + 1 < command.Length && command[i + 1] == '(')
                return true; // $() 命令替换
        }
        return false;
    }

    /// <summary>
    /// 获取安全命令列表（用于工具描述注入）。
    /// </summary>
    public static string GetSafeCommandsDescription()
    {
        return string.Join(", ", SafeCommands.Take(20)) + " 等";
    }
}

/// <summary>
/// 参数级拦截规则。
/// </summary>
internal class ArgBlockRule
{
    public string Command { get; }
    public string[]? SubCommands { get; }
    public string[]? BlockArgs { get; }
    public string[]? Flags { get; }
    public string[]? ExceptFlags { get; }

    public ArgBlockRule(string command, params string[] subAndArgs)
    {
        Command = command;
        // 根据参数类型推断：以 - 开头的是 flag，否则是子命令/参数
        var subCmds = new List<string>();
        var args = new List<string>();
        var flags = new List<string>();

        foreach (var s in subAndArgs)
        {
            if (s.StartsWith('-'))
                flags.Add(s);
            else
                args.Add(s);
        }

        if (args.Count > 0)
        {
            // 第一个参数作为子命令
            SubCommands = [.. args.Take(1)];
            if (args.Count > 1)
                BlockArgs = [.. args.Skip(1)];
        }
        else if (flags.Count > 0)
        {
            Flags = [.. flags];
        }
        else
        {
            SubCommands = [];
        }
    }

    public ArgBlockRule(string command, string? subCommand = null, string[]? blockArgs = null,
        string[]? flags = null, string[]? exceptFlags = null)
    {
        Command = command;
        SubCommands = subCommand != null ? [subCommand] : null;
        BlockArgs = blockArgs;
        Flags = flags;
        ExceptFlags = exceptFlags;
    }

    /// <summary>
    /// 测试命令片段是否匹配此规则。
    /// </summary>
    public bool Match(string[] parts)
    {
        if (parts.Length < 2) return false;
        if (!string.Equals(Path.GetFileName(parts[0]), Command, StringComparison.OrdinalIgnoreCase))
            return false;

        // 检查子命令
        if (SubCommands != null && SubCommands.Length > 0)
        {
            if (parts.Length < 2) return false;
            if (!SubCommands.Any(s => string.Equals(parts[1], s, StringComparison.OrdinalIgnoreCase)))
                return false;
            // 纯子命令禁止（无额外 flags/blockArgs/exceptFlags 规则）：命中子命令即拦截，
            // 例如 `dotnet new`、`cargo install`。否则交由参数级规则判定。
            if (Flags == null && BlockArgs == null && ExceptFlags == null)
                return true;
            // 剩余参数从 parts[2..] 检查
            return MatchArgs(parts[2..]);
        }

        // 无子命令则从 parts[1..] 检查标志
        return MatchArgs(parts[1..]);
    }

    private bool MatchArgs(string[] remainingArgs)
    {
        // 白名单语义（exceptFlags）：命中排除标志 → 放行；未命中 → 默认拦截该子命令。
        if (ExceptFlags != null)
        {
            bool hasExceptFlag = false;
            foreach (var arg in remainingArgs)
            {
                if (ExceptFlags.Any(f => arg.StartsWith(f)))
                {
                    hasExceptFlag = true;
                    break;
                }
            }
            if (hasExceptFlag) return false; // 用户指定了允许的标志，不拦截
        }

        // 黑名单语义（flags/blockArgs）：命中禁止标志/参数 → 拦截
        if (Flags != null)
        {
            foreach (var arg in remainingArgs)
            {
                if (Flags.Any(f => arg.StartsWith(f)))
                    return true; // 命中禁止标志
            }
        }

        if (BlockArgs != null)
        {
            foreach (var arg in remainingArgs)
            {
                if (BlockArgs.Any(a => string.Equals(arg, a, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }

        // 有 exceptFlags 但未命中任何排除标志 → 默认拦截（子命令危险，仅白名单放行）；
        // 纯黑名单规则（无 exceptFlags）未命中 → 默认放行。
        return ExceptFlags != null;
    }
}
