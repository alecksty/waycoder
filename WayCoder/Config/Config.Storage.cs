namespace WayCoder;

public partial class Config
{
    // ════════════════════════════════════════════════════════════
    // 保存到 .env 文件（从 Schema 自动生成）
    // ════════════════════════════════════════════════════════════

    public void SaveToEnvFile()
    {
        lock (SaveLock)
        {
            // 全部配置 → 全局 config.json（secret 密钥除外，独立管理走 api_keys.json）
            SaveToConfigJson();
            // .env 只保留 5 个基本引导配置（服务商/地址/API_KEY/经济模式/是否使用鼠标）。
            // 普通启动不再读取 .env —— 它是 config.json 被删除后首次启动的恢复引导源。
            SaveMinimalDotEnv();
        }
    }

    /// <summary>全局 config.json 路径（~/.waycoder/config.json，配置权威源）。</summary>
    private static string ConfigJsonPath => Global.GlobalConfigPath("config.json");

    /// <summary>
    /// 读取全局 config.json 覆盖当前值（config.json 唯一权威源；环境变量仅首次启动导入）。
    /// secret（API Key）不存 config.json，仍走 api_keys.json（首次启动导入的环境变量 key）。
    /// </summary>
    private void LoadConfigJson()
    {
        try
        {
            var path = ConfigJsonPath;
            if (!File.Exists(path)) return;
            var root = Json.Parse(File.ReadAllText(path));
            if (root is not { Kind: JKind.Object }) return;

            foreach (var p in _schema)
            {
                if (p.Type == "secret") continue;
                var val = root[p.Key]?.AsString();
                if (val == null) continue;
                try { p.Setter(this, val); }
                catch { /* 非法值（如越界数字）忽略，保留当前值，避免启动崩溃 */ }
            }

            // 非 schema 的辅助字段：/free 切换前的模型（PreviousModel 跨会话持久化）
            FreePrevProvider = root["freePrevProvider"]?.AsString();
            FreePrevModel = root["freePrevModel"]?.AsString();
            FreePrevBaseUrl = root["freePrevBaseUrl"]?.AsString();
        }
        catch { /* config.json 损坏时静默忽略，回退 .env/默认 */ }
    }

    /// <summary>
    /// 保存全部配置到全局 config.json（secret 密钥除外）。
    /// 手写 JSON 序列化（AOT 无反射），临时文件 + 原子替换（同卷 rename）。
    /// </summary>
    public void SaveToConfigJson()
    {
        lock (SaveLock)
        {
            try
            {
                var path = ConfigJsonPath;
                Global.EnsureDir(path);

                var obj = JNode.Object();
                foreach (var p in _schema)
                {
                    // API Key 不写入 config.json：密钥独立管理，走全局 api_keys.json（一个服务商一个 key）
                    if (p.Type == "secret") continue;
                    var val = p.Getter(this);
                    if (p.SkipIfEmpty && string.IsNullOrEmpty(val)) continue;
                    if (p.DefaultStr != null && val == p.DefaultStr) continue;
                    obj.Set(p.Key, JNode.From(val));
                }

                // 非 schema 辅助字段：/free 切换前模型（有记录才写，恢复/清空后不再残留）
                if (!string.IsNullOrEmpty(FreePrevModel))
                {
                    obj.Set("freePrevProvider", JNode.From(FreePrevProvider ?? ""));
                    obj.Set("freePrevModel", JNode.From(FreePrevModel));
                    obj.Set("freePrevBaseUrl", JNode.From(FreePrevBaseUrl ?? ""));
                }

                Global.WriteAllTextAtomic(path, Json.Serialize(obj, indent: true)); // 同卷原子替换
            }
            catch { /* 保存失败不崩溃（磁盘满/权限） */ }
        }
    }

    /// <summary>
    /// .env 只保留 5 个基本引导配置：服务商 / 地址 / API_KEY / 经济模式 / 是否使用鼠标。
    /// 其余 WAYCODER_* 行清理（值已迁入 config.json）；非 WAYCODER_* 键（如 GITEE_TOKEN）保留。
    /// </summary>
    private void SaveMinimalDotEnv()
    {
        var envPath = FindEnvFile() ?? Global.GlobalConfigPath(".env");
        Global.EnsureDir(envPath);
        var lines = File.Exists(envPath) ? File.ReadAllLines(envPath).ToList() : [];

        // 移除所有 WAYCODER_* 行（稍后只重写 5 项），保留注释与非 WAYCODER_* 键（token 等）
        lines.RemoveAll(l =>
        {
            var t = l.Trim();
            if (t.Length == 0 || t.StartsWith('#')) return false;
            var eq = t.IndexOf('=');
            if (eq <= 0) return false;
            return t[..eq].Trim().StartsWith("WAYCODER_", StringComparison.OrdinalIgnoreCase);
        });

        // 追加 5 个基本配置（非空）
        foreach (var envVar in BasicDotEnvKeys)
        {
            var p = _schema.FirstOrDefault(s => string.Equals(s.EnvVar, envVar, StringComparison.OrdinalIgnoreCase));
            if (p == null) continue;
            var val = p.Getter(this);
            if (string.IsNullOrEmpty(val)) continue;
            lines.Add($"{envVar}={val}");
        }

        try { File.WriteAllLines(envPath, lines); }
        catch { /* 写失败不崩溃 */ }
    }

    /// <summary>.env 精简后只保留的 5 个基本引导配置（服务商/地址/API_KEY/经济模式/是否使用鼠标）</summary>
    private static readonly string[] BasicDotEnvKeys =
        ["WAYCODER_PROVIDER", "WAYCODER_BASE_URL", "WAYCODER_API_KEY", "WAYCODER_ECONOMY", "WAYCODER_MOUSE"];

    /// <summary>
    /// 每次启动：全局 ~/.waycoder/config.json 若与项目本地副本内容不同，复制一份到
    /// 项目本地 .waycoder/config.json 保存 —— 防止全局文件意外损坏/丢失时配置无法恢复。
    /// 内容相同则跳过（幂等）。
    /// </summary>
    private void SyncConfigJsonToLocal()
    {
        try
        {
            var global = ConfigJsonPath;
            if (!File.Exists(global)) return;

            // 先确认全局 config.json 正常（可解析为对象）才备份；损坏则不备份——备份已坏的文件没意义
            var root = Json.Parse(File.ReadAllText(global));
            if (root is not { Kind: JKind.Object }) return;

            var local = Global.WriteConfigPath(Directory.GetCurrentDirectory(), "config.json");
            Global.EnsureDir(local);

            if (File.Exists(local) && File.ReadAllText(local) == File.ReadAllText(global))
                return; // 无更新，跳过

            File.Copy(global, local, overwrite: true);
        }
        catch { /* 本地副本同步失败不阻塞启动 */ }
    }
}
