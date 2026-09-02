namespace WayCoder;

public static partial class ModelCatalog
{
    /// <summary>旧版单文件模型库路径（全局，仅兼容读 + 一次性迁移）</summary>
    public static string GlobalModelsPath => Global.GlobalConfigPath("models.json");

    /// <summary>旧版单文件模型库路径（本地，仅兼容读 + 一次性迁移）</summary>
    public static string LocalModelsPath =>
        Path.Combine(Environment.CurrentDirectory, Global.ConfigDirName, "models.json");

    /// <summary>按供应商分类的模型库目录（全局 ~/.waycoder/provider/）</summary>
    public static string GlobalProviderDir => Global.GlobalConfigPath("provider");

    /// <summary>按供应商分类的模型库目录（本地 .waycoder/provider/）</summary>
    public static string LocalProviderDir =>
        Path.Combine(Environment.CurrentDirectory, Global.ConfigDirName, "provider");

    /// <summary>供应商 → 分类文件名：local/custom/ollama/lmstudio 等本地模型归 locals.json，其余按 providerId 命名。</summary>
    public static string ProviderFile(string providerId, bool local)
    {
        var group = ProviderGroupName(providerId);
        var dir = local ? LocalProviderDir : GlobalProviderDir;
        return Path.Combine(dir, group + ".json");
    }

    /// <summary>计算供应商归属的分类文件名（不含扩展名）。id 规范化（全小写、去特殊符号），防路径穿越。</summary>
    public static string ProviderGroupName(string? providerId)
    {
        var pid = NormalizeId(providerId ?? "custom");
        if (pid is "local" or "custom" or "ollama" or "lmstudio" or "lm-studio" or "locals" or "open-webui" or "vllm" or "text-generation-webui" or "localai")
            return "locals";
        return pid.Length > 0 ? pid : "custom";
    }

    /// <summary>删除全局+本地全部自定义模型文件（provider 分文件 + 汇总 models.json），返回删除数。</summary>
    private static int DeleteAllCustomFiles()
    {
        int n = 0;
        foreach (var dir in new[] { GlobalProviderDir, LocalProviderDir })
        {
            try
            {
                if (!Directory.Exists(dir)) continue;
                // 删除所有供应商模型列表 json 文件（两层架构：provider 下模型库）
                foreach (var f in Directory.EnumerateFiles(dir, "*.json"))
                { try { File.Delete(f); n++; } catch { } }
                // 删空后移除 provider 目录本身，避免残留空目录
                try { if (!Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir); } catch { }
            }
            catch { }
        }
        TryDeleteFile(GlobalModelsPath);
        TryDeleteFile(LocalModelsPath);
        return n;
    }

    private static Dictionary<string, ModelInfo> LoadCustom()
    {
        if (_custom != null) return _custom;
        lock (_lock)
        {
            if (_custom != null) return _custom;
            MigrateLegacyModels(); // 首次加载：把旧 models.json 迁移到 provider/ 分类文件
            var merged = new Dictionary<string, ModelInfo>();
            foreach (var file in EnumerateModelFiles())
                foreach (var m in ReadFile(file).Values) merged[ModelKey(m.ProviderId, m.Id)] = m;
            _custom = merged;
            return _custom;
        }
    }

    /// <summary>枚举所有模型文件：provider/ 分类文件（全局+本地）+ 旧 models.json（全局+本地，兼容）。</summary>
    private static IEnumerable<string> EnumerateModelFiles()
    {
        foreach (var dir in new[] { GlobalProviderDir, LocalProviderDir })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                yield return file;
        }
        if (File.Exists(GlobalModelsPath)) yield return GlobalModelsPath;
        if (File.Exists(LocalModelsPath)) yield return LocalModelsPath;
    }

    /// <summary>一次性迁移：把旧 models.json 的模型按供应商分类合并写入 provider/ 文件，全部写成功后才删除旧文件。
    /// 迁移完成写 .migrated 标记（防误判：仅凭 provider/ 有文件就删旧数据，会让未迁移的旧模型永久丢失）。</summary>
    private static void MigrateLegacyModels()
    {
        foreach (var legacy in new[] { (GlobalModelsPath, false), (LocalModelsPath, true) })
        {
            var file = legacy.Item1;
            if (!File.Exists(file)) continue;
            var dir = legacy.Item2 ? LocalProviderDir : GlobalProviderDir;
            var mark = Path.Combine(dir, ".migrated");

            // 已有迁移标记：旧文件残留则清理，不再重复迁移
            if (File.Exists(mark))
            {
                TryDeleteFile(file);
                continue;
            }

            var models = ReadFile(file);
            if (models.Count == 0) { TryDeleteFile(file); continue; }

            // 合并写入各分类文件（保留 provider/ 已存在的其他模型，不覆盖）
            var allOk = true;
            foreach (var g in models.Values.GroupBy(m => ProviderGroupName(m.ProviderId)))
            {
                var path = Path.Combine(dir, g.Key + ".json");
                var existing = ReadFile(path);
                foreach (var m in g)
                    existing[ModelKey(m.ProviderId, m.Id)] = m;
                if (!SaveCustom(existing, path)) { allOk = false; break; }
            }

            // 全部写成功才删旧文件 + 写迁移标记；任何失败保留旧文件防数据丢失
            if (!allOk) continue;
            try { Directory.CreateDirectory(dir); File.WriteAllText(mark, DateTime.UtcNow.ToString("O")); } catch { }
            TryDeleteFile(file);
        }
    }

    private static Dictionary<string, ModelInfo> ReadFile(string path)
    {
        var result = new Dictionary<string, ModelInfo>();
        if (!File.Exists(path)) return result;
        try
        {
            var root = Json.Parse(File.ReadAllText(path));
            var arr = Arr(root) ?? Arr(root?["models"]);
            if (arr != null)
            {
                foreach (var node in arr.Items)
                {
                    var info = FromJson(node);
                    if (info != null) result[ModelKey(info.ProviderId, info.Id)] = info;
                }
            }
        }
        catch (Exception ex)
        {
            // 损坏/截断文件静默返回空会令模型「无声消失」并被后续写回固化，记录日志便于排查
            ErrorLog.Warning("ModelCatalog", $"读取模型文件失败（将视为空）: {path}", ex);
        }
        return result;
    }

    /// <summary>写模型文件（临时文件 + 原子 rename 替换，防崩溃/磁盘满留下截断文件覆盖全量数据）。返回是否成功。</summary>
    private static bool SaveCustom(Dictionary<string, ModelInfo> models, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            var arr = JNode.Array();
            foreach (var m in models.Values.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase))
                arr.Add(ToJson(m));
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, arr.ToJson(indent: true));
            File.Move(tmp, path, overwrite: true); // 同卷原子替换
            return true;
        }
        catch { return false; } // 写入失败不崩溃，由调用方决定是否保留现场
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    /// <summary>写前备份数据文件：复制为 path.{yyyyMMddHHmmssfff}.bak（重名冲突回退 .{Guid:N}.bak）。
    /// 仿 Infra/DoctorEngine.BackupFile 命名惯例；失败返回 null（调用方决定是否继续）。</summary>
    private static string? BackupFile(string path)
    {
        try
        {
            var bak = path + "." + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".bak";
            if (File.Exists(bak)) bak = path + "." + Guid.NewGuid().ToString("N") + ".bak";
            File.Copy(path, bak, overwrite: false);
            return bak;
        }
        catch { return null; }
    }

    /// <summary>判断文件是否落在本地（cwd/.waycoder）作用域 —— 决定归并迁移写到 global 还是 local 桶。</summary>
    private static bool IsLocalModelFile(string path) =>
        path.StartsWith(LocalProviderDir, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, LocalModelsPath, StringComparison.OrdinalIgnoreCase);
}
