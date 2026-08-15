using System.Net;

namespace WayCoder;

/// <summary>
/// SSRF 防护 —— 拦截指向内网/环回/链路本地/保留地址的目标 URL，
/// 防止 fetch/download 被诱导访问云元数据（169.254.169.254）、内网服务等。
///
/// 纯逻辑（IP/主机名判断）与网络（DNS 解析）分离，便于确定性自测。
/// </summary>
public static class SsgfGuard
{
    /// <summary>内网/保留主机名后缀（含常见内部域名约定）。</summary>
    private static readonly string[] PrivateHostnameSuffixes =
        [".local", ".internal", ".lan", ".localhost", ".home", ".corp", ".intranet"];

    /// <summary>
    /// 校验 URL 是否可安全访问。返回 (true, null) 表示放行，否则 (false, 原因)。
    /// 仅允许 http/https；host 不得是内网/环回/链路本地/保留的字面量 IP 或特殊主机名。
    /// </summary>
    public static (bool safe, string? reason) CheckUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "URL 为空");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, "URL 格式非法");

        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https"))
            return (false, $"不支持的协议 '{scheme}'（仅 http/https）");

        var host = uri.Host.ToLowerInvariant();
        if (string.IsNullOrEmpty(host))
            return (false, "URL 缺少主机名");

        // 1. 字面量 IP 检查（不依赖 DNS，覆盖 127.0.0.1、10.x、169.254.169.254 等）
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateOrReserved(ip))
                return (false, $"已阻止访问内网/保留地址 {host}（SSRF 防护）");
            return (true, null);
        }

        // 2. 特殊环回主机名检查
        if (host is "localhost" or "localhost.localdomain" or "ip6-localhost" or "ip6-loopback")
            return (false, $"已阻止访问环回主机名 {host}（SSRF 防护）");

        // 3. 内网域名后缀检查
        foreach (var suffix in PrivateHostnameSuffixes)
        {
            if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return (false, $"已阻止访问内网主机名 {host}（SSRF 防护）");
        }

        return (true, null);
    }

    /// <summary>
    /// 解析主机名 DNS，检查是否解析到内网地址（防 DNS 指向内网）。
    /// 字面量 IP 直接放行（已在 CheckUrl 检查）；解析失败放行（避免因临时网络问题误伤）。
    /// </summary>
    public static (bool safe, string? reason) CheckDns(string host)
    {
        if (string.IsNullOrEmpty(host)) return (false, "主机名为空");
        if (IPAddress.TryParse(host, out _)) return (true, null); // 字面量 IP 已在 CheckUrl 处理

        try
        {
            var addrs = Dns.GetHostAddresses(host);
            foreach (var addr in addrs)
            {
                if (IsPrivateOrReserved(addr))
                    return (false, $"已阻止访问解析到内网地址 {addr} 的主机 {host}（SSRF 防护）");
            }
        }
        catch { /* DNS 解析失败，放行（不因网络问题误伤） */ }

        return (true, null);
    }

    /// <summary>判断 IP 字符串是否为私网/环回/链路本地/保留地址（无效 IP 返回 false）。</summary>
    public static bool IsPrivateIp(string ip)
        => IPAddress.TryParse(ip, out var addr) && IsPrivateOrReserved(addr);

    /// <summary>判断 IP 是否为私网/环回/链路本地/保留/组播地址（IPv4 + IPv6）。</summary>
    public static bool IsPrivateOrReserved(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        // IPv4
        if (bytes.Length == 4)
        {
            var a = bytes[0]; var b = bytes[1]; var c = bytes[2];
            if (a == 0) return true;                          // 0.0.0.0/8
            if (a == 10) return true;                         // 10.0.0.0/8
            if (a == 100 && b >= 64 && b <= 127) return true; // 100.64.0.0/10 (CGNAT)
            if (a == 127) return true;                        // 127.0.0.0/8 (loopback)
            if (a == 169 && b == 254) return true;            // 169.254.0.0/16 (link-local / 云元数据)
            if (a == 172 && b >= 16 && b <= 31) return true;  // 172.16.0.0/12
            if (a == 192 && b == 0 && c == 0) return true;    // 192.0.0.0/24
            if (a == 192 && b == 0 && c == 2) return true;    // 192.0.2.0/24 (TEST-NET-1)
            if (a == 192 && b == 168) return true;            // 192.168.0.0/16
            if (a == 198 && (b == 18 || b == 19)) return true; // 198.18.0.0/15 (benchmark)
            if (a == 198 && b == 51 && c == 100) return true; // 198.51.100.0/24 (TEST-NET-2)
            if (a == 203 && b == 0 && c == 113) return true;  // 203.0.113.0/24 (TEST-NET-3)
            if (a >= 224) return true;                        // 224.0.0.0/4 multicast + 240.0.0.0/4 reserved
            return false;
        }

        // IPv6 (16 bytes)
        if (bytes.Length == 16)
        {
            // ::1 loopback
            var isLoopback = true;
            for (var i = 0; i < 15; i++) if (bytes[i] != 0) { isLoopback = false; break; }
            if (isLoopback && bytes[15] == 1) return true;

            // :: (unspecified)
            var allZero = true;
            for (var i = 0; i < 16; i++) if (bytes[i] != 0) { allZero = false; break; }
            if (allZero) return true;

            // ::ffff:a.b.c.d (IPv4-mapped)：检查映射的 IPv4
            var mappedPrefix = true;
            for (var i = 0; i < 10; i++) if (bytes[i] != 0) { mappedPrefix = false; break; }
            if (mappedPrefix && bytes[10] == 0xFF && bytes[11] == 0xFF)
                return IsPrivateOrReserved(new IPAddress(new byte[] { bytes[12], bytes[13], bytes[14], bytes[15] }));

            var b0 = bytes[0];
            var b1 = bytes[1];
            if ((b0 & 0xFE) == 0xFC) return true;               // fc00::/7 (ULA)
            if (b0 == 0xFE && (b1 & 0xC0) == 0x80) return true; // fe80::/10 (link-local)
            if (b0 == 0xFF) return true;                        // ff00::/8 (multicast)
        }

        return false;
    }

    /// <summary>判断 HTTP 状态码是否为重定向（301/302/303/307/308）。</summary>
    public static bool IsRedirect(int statusCode) => statusCode is 301 or 302 or 303 or 307 or 308;
}

/// <summary>SSRF 拦截异常 —— fetch/download 工具捕获后转为错误文案（不进入网络重试）。</summary>
public sealed class SsgfBlockedException : Exception
{
    public SsgfBlockedException(string reason) : base(reason) { }
}
