using WayCoder.Infra;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk15(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[Doctor]");

        var root = Path.Combine(Path.GetTempPath(), "waycoder_doctor_" + Guid.NewGuid().ToString("N")[..8]);
        var home = Path.Combine(root, "home");
        var cwd = Path.Combine(root, "cwd");
        Directory.CreateDirectory(home);
        Directory.CreateDirectory(cwd);

        try
        {
            DoctorOptions Options(bool fix) => new()
            {
                Home = home,
                Cwd = cwd,
                Fix = fix,
                CheckApiKeyAvailability = false,
                Models = ["deepseek-v4-flash"],
            };

            var missing = DoctorEngine.RunAsync(Options(false)).GetAwaiter().GetResult();
            var missingHome = missing.Issues.FirstOrDefault(i => i.Name == "全局配置目录");
            Check("Doctor: 缺失 .waycoder 报错误", missingHome?.Status == DoctorStatus.Error);
            Check("Doctor: 只读自检不创建目录", !Directory.Exists(Path.Combine(home, ".waycoder")));

            var fixedReport = DoctorEngine.RunAsync(Options(true)).GetAwaiter().GetResult();
            var waycoderDir = Path.Combine(home, ".waycoder");
            var configPath = Path.Combine(waycoderDir, "config.json");
            Check("Doctor: fix 创建 .waycoder", Directory.Exists(waycoderDir));
            Check("Doctor: fix 创建空 config.json", File.Exists(configPath));
            Check("Doctor: fix 后目录检查通过", fixedReport.Issues.Any(i => i.Name == "全局配置目录" && i.Status == DoctorStatus.Ok));
            Check("Doctor: 报告含汇总", fixedReport.Render().Contains("结果: "));

            File.WriteAllText(configPath, "{broken json");
            var configFixed = DoctorEngine.RunAsync(Options(true)).GetAwaiter().GetResult();
            var configIssue = configFixed.Issues.FirstOrDefault(i => i.Name == "config.json");
            Check("Doctor: 损坏 config.json 备份并重置", configIssue?.Status == DoctorStatus.Warning && configIssue.Message.Contains("已备份"));
            Check("Doctor: 重置后的 config.json 可解析", Json.Parse(File.ReadAllText(configPath)) is { Kind: JKind.Object });
            Check("Doctor: config.json 备份保留", Directory.GetFiles(waycoderDir, "config.json.*.bak").Length > 0);

            var envHome = Path.Combine(root, "env-home");
            var envCwd = Path.Combine(root, "env-cwd");
            Directory.CreateDirectory(Path.Combine(envHome, ".waycoder"));
            Directory.CreateDirectory(envCwd);
            File.WriteAllText(Path.Combine(envHome, ".waycoder", ".env"), "GOOD=1\nBAD LINE\n");
            var envReport = DoctorEngine.RunAsync(new DoctorOptions
            {
                Home = envHome,
                Cwd = envCwd,
                CheckApiKeyAvailability = false,
            }).GetAwaiter().GetResult();
            Check("Doctor: .env 非法行被识别", envReport.Issues.Any(i =>
                i.Name == ".env" && i.Status == DoctorStatus.Error && i.Message.Contains("格式非法")));

            var keyHome = Path.Combine(root, "key-home");
            var keyCwd = Path.Combine(root, "key-cwd");
            Directory.CreateDirectory(Path.Combine(keyHome, ".waycoder"));
            Directory.CreateDirectory(keyCwd);
            var apiKeys = Path.Combine(keyHome, ".waycoder", "api_keys.json");
            File.WriteAllText(apiKeys, """[{"provider":"doctor-test","apikey":"sk-doctor-secret-123"}]""");
            var keyReport = DoctorEngine.RunAsync(new DoctorOptions
            {
                Home = keyHome,
                Cwd = keyCwd,
                CheckApiKeyAvailability = false,
            }).GetAwaiter().GetResult();
            Check("Doctor: api_keys.json 可解析", keyReport.Issues.Any(i => i.Name == "api_keys.json" && i.Status == DoctorStatus.Ok));
            Check("Doctor: 报告不泄露 API Key", !keyReport.Render().Contains("sk-doctor-secret-123"));

            File.WriteAllText(apiKeys, "{broken");
            var keyFix = DoctorEngine.RunAsync(new DoctorOptions
            {
                Home = keyHome,
                Cwd = keyCwd,
                Fix = true,
                CheckApiKeyAvailability = false,
            }).GetAwaiter().GetResult();
            Check("Doctor: 损坏 api_keys.json 备份且不改写",
                keyFix.Issues.Any(i => i.Name == "api_keys.json" && i.Message.Contains("已备份")) && File.ReadAllText(apiKeys) == "{broken");
            Check("Doctor: api_keys.json 备份存在", Directory.GetFiles(Path.Combine(keyHome, ".waycoder"), "api_keys.json.*.bak").Length > 0);

            var tmpHome = Path.Combine(root, "tmp-home");
            var tmpCwd = Path.Combine(root, "tmp-cwd");
            Directory.CreateDirectory(Path.Combine(tmpHome, ".waycoder"));
            Directory.CreateDirectory(tmpCwd);
            var tmpFile = Path.Combine(tmpHome, ".waycoder", "stale.tmp");
            File.WriteAllText(tmpFile, "x");
            var tmpReport = DoctorEngine.RunAsync(new DoctorOptions
            {
                Home = tmpHome,
                Cwd = tmpCwd,
                CheckApiKeyAvailability = false,
            }).GetAwaiter().GetResult();
            Check("Doctor: 发现 .tmp 残留", tmpReport.Issues.Any(i => i.Name == "临时文件" && i.Status == DoctorStatus.Warning));
            var tmpFix = DoctorEngine.RunAsync(new DoctorOptions
            {
                Home = tmpHome,
                Cwd = tmpCwd,
                Fix = true,
                CheckApiKeyAvailability = false,
            }).GetAwaiter().GetResult();
            Check("Doctor: fix 清理 .tmp", !File.Exists(tmpFile) && tmpFix.Issues.Any(i => i.Name == "临时文件" && i.Message.Contains("已清理")));

            var mcpHome = Path.Combine(root, "mcp-home");
            var mcpCwd = Path.Combine(root, "mcp-cwd");
            Directory.CreateDirectory(Path.Combine(mcpCwd, ".waycoder"));
            Directory.CreateDirectory(mcpHome);
            File.WriteAllText(Path.Combine(mcpCwd, ".waycoder", "mcp_servers.json"), "{broken");
            var mcpReport = DoctorEngine.RunAsync(new DoctorOptions
            {
                Home = mcpHome,
                Cwd = mcpCwd,
                CheckApiKeyAvailability = false,
            }).GetAwaiter().GetResult();
            Check("Doctor: 损坏 mcp_servers.json 报错", mcpReport.Issues.Any(i =>
                i.Name == "MCP" && i.Status == DoctorStatus.Error && i.Message.Contains("损坏")));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
