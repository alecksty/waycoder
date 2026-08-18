using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 环境变量清理 —— 子进程启动前移除凭据形状的环境变量，
/// 防止密钥经命令输出 / env / spill 文件泄露（对标 deepseek-harness 的 scrubbedParentEnv）。
/// 默认继承父环境，仅删除敏感名；显式在命令行内联传的凭据不受影响。
/// </summary>
public static class EnvScrubber
{
    // 覆盖常见凭据形状：KEY/PASSWORD/PASS/PWD/SECRET/TOKEN/CREDENTIAL/AUTH/CONNECTION_STRING 等。
    // 补齐此前漏掉的 DB_PASS / MYSQL_PWD / GOOGLE_APPLICATION_CREDENTIALS / DOCKER_AUTH / NETRC / AZURE_*_CONNECTION_STRING。
    private static readonly Regex SensitivePattern = new(
        @"KEY|PASSWORD|PASSWD|PASS|PWD|SECRET|TOKEN|CREDENTIAL|CRED|AUTH|CONNECTION_STRING|NETRC|PRIVATE_KEY",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// 判断环境变量名是否敏感：匹配 KEY/PASSWORD/SECRET/TOKEN，或以 WAYCODER_ 开头
    /// （harness 自身凭据如 WAYCODER_API_KEY 不应泄漏给子进程）。
    /// </summary>
    internal static bool IsSensitive(string name)
    {
        if (name.StartsWith("WAYCODER_", StringComparison.OrdinalIgnoreCase)) return true;
        return SensitivePattern.IsMatch(name);
    }

    /// <summary>从 ProcessStartInfo 的环境变量中移除敏感项（保留其余继承项）。</summary>
    public static void Scrub(ProcessStartInfo psi)
    {
        // Keys 为非泛型 ICollection，无法走 Linq；先收集敏感项再移除，避免边迭代边删。
        var sensitive = new List<string>();
        foreach (string key in psi.EnvironmentVariables.Keys)
            if (IsSensitive(key))
                sensitive.Add(key);

        foreach (var key in sensitive)
            psi.EnvironmentVariables.Remove(key);
    }
}
