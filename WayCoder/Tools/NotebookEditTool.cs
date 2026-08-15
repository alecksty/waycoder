using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// Jupyter Notebook 编辑工具 —— 替换、插入、删除 notebook 中的 cell。
///
/// 支持 .ipynb (JSON) 格式。操作基于 cell 索引（0-based），
/// 直接读写 JSON 结构（AOT 兼容，手写序列化）。
///
/// 安全措施：
/// - 只操作 .ipynb 文件
/// - 操作前验证 JSON 结构完整性
/// - 写文件使用 FileLockManager 防并发冲突
/// </summary>
public class NotebookEditTool : ITool
{
    public string Name => "notebook_edit";
    public string Description =>
        "编辑 Jupyter Notebook (.ipynb) 文件。支持三种操作: replace（替换指定 cell 的源代码）、" +
        "insert（在指定位置后插入新 cell）、delete（删除指定 cell）。" +
        "cell_index 从 0 开始计数。insert 时需要提供 cell_type（code 或 markdown）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("notebook_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "Notebook 文件路径（.ipynb）"))
            .Set("cell_index", JNode.Object()
                .Set("type", "integer")
                .Set("description", "Cell 索引（0-based）。replace/delete 时指定目标 cell；insert 时新 cell 插入到该索引之后（-1 表示插入到开头）"))
            .Set("new_source", JNode.Object()
                .Set("type", "string")
                .Set("description", "新内容。replace 时替换 cell 源代码；insert 时为整个新 cell 的源代码"))
            .Set("cell_type", JNode.Object()
                .Set("type", "string")
                .Set("description", "Cell 类型（insert 时需要）: code | markdown"))
            .Set("edit_mode", JNode.Object()
                .Set("type", "string")
                .Set("description", "编辑模式: replace（默认，替换 cell 源） | insert（插入新 cell） | delete（删除 cell）")))
        .Set("required", JNode.Array().Add("notebook_path").Add("new_source"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var notebookPath = arguments.GetValueOrDefault("notebook_path")?.ToString() ?? "";
        var newSource = arguments.GetValueOrDefault("new_source")?.ToString() ?? "";
        var cellType = arguments.GetValueOrDefault("cell_type")?.ToString() ?? "code";
        var editMode = arguments.GetValueOrDefault("edit_mode")?.ToString() ?? "replace";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        // cell_index 可能是 int 或 long (JSON 数字)
        var cellIndexRaw = arguments.GetValueOrDefault("cell_index");
        var cellIndex = cellIndexRaw switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string s => int.TryParse(s, out var p) ? p : -1,
            _ => -1,
        };

        return await ExecuteAsync(notebookPath, cellIndex, newSource, cellType, editMode, agentId);
    }

    private static async Task<string> ExecuteAsync(
        string notebookPath, int cellIndex, string newSource,
        string cellType, string editMode, string agentId)
    {
        if (string.IsNullOrWhiteSpace(notebookPath))
            return "错误：notebook_path 不能为空 — 请提供有效的文件路径。";

        var path = Path.GetFullPath(notebookPath);

        // 验证扩展名
        if (!path.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase))
            return $"错误：{notebookPath} 不是 .ipynb 文件。notebook_edit 只能编辑 Jupyter Notebook。";

        // 文件锁
        if (!FileLockManager.TryAcquire(path, agentId))
        {
            var lockInfo = FileLockManager.GetLockInfo(path);
            return $"❌ 文件被锁定: {lockInfo?.Status ?? "未知"} — 请等待锁释放";
        }

        try
        {
            if (!File.Exists(path))
                return $"错误：{notebookPath} 未找到";

            // 读取并解析 JSON
            string jsonText;
            try { jsonText = File.ReadAllText(path, Encoding.UTF8); }
            catch (Exception ex) { return $"错误：无法读取 {notebookPath} — {ex.Message}"; }

            JNode? root;
            try { root = Json.Parse(jsonText); }
            catch (Exception ex) { return $"错误：{notebookPath} 不是有效的 JSON — {ex.Message}"; }

            if (root == null || root.Kind != JKind.Object)
                return $"错误：{notebookPath} JSON 结构异常（根节点不是对象）";

            // 验证 notebook 结构
            var cellsArray = root["cells"];
            if (cellsArray == null || cellsArray.Kind != JKind.Array)
                return $"错误：{notebookPath} 缺少 cells 数组（不是有效的 Jupyter Notebook）";

            return editMode switch
            {
                "insert" => InsertCell(root, cellsArray, cellIndex, newSource, cellType, path),
                "delete" => DeleteCell(root, cellsArray, cellIndex, path),
                _ => ReplaceCell(root, cellsArray, cellIndex, newSource, path),
            };
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            FileLockManager.Release(path, agentId);
        }
    }

    /// <summary>替换指定 cell 的 source</summary>
    private static string ReplaceCell(JNode root, JNode cells, int index, string newSource, string path)
    {
        if (index < 0 || index >= cells.Count)
            return $"错误：cell_index {index} 超出范围（共 {cells.Count} 个 cell）";

        var cell = cells[index];
        if (cell is not { Kind: JKind.Object })
            return $"错误：cell[{index}] 不是有效的对象";

        var oldSource = GetSourceText(cell);
        SetSourceText(cell, newSource);

        // 清除 cell 的输出（代码改变后旧输出无效）
        if (cell["cell_type"]?.AsString() == "code")
        {
            cell["outputs"] = JNode.Array();
            cell["execution_count"] = JNode.Null();
        }

        WriteNotebook(root, path);

        var preview = Truncate(newSource, 200);
        var oldPreview = Truncate(oldSource, 80);
        return $"✅ 已替换 {path} 的 cell[{index}]\n" +
               $"  旧内容: {oldPreview}\n" +
               $"  新内容: {preview}";
    }

    /// <summary>在指定位置后插入新 cell</summary>
    private static string InsertCell(JNode root, JNode cells, int afterIndex, string newSource, string cellType, string path)
    {
        // 规范化 cell_type
        var normalizedType = cellType.ToLowerInvariant() switch
        {
            "code" or "python" or "py" => "code",
            "markdown" or "md" or "text" => "markdown",
            "raw" => "raw",
            _ => "code",
        };

        var newCell = JNode.Object()
            .Set("cell_type", normalizedType)
            .Set("metadata", JNode.Object())
            .Set("source", newSource); // 先尝试字符串格式

        // 为 code cell 添加默认字段
        if (normalizedType == "code")
        {
            newCell["outputs"] = JNode.Array();
            newCell["execution_count"] = JNode.Null();
        }

        var insertAt = Math.Clamp(afterIndex + 1, 0, cells.Count);
        var cellList = cells.Items.ToList();
        cellList.Insert(insertAt, newCell);
        var newCells = JNode.Array();
        foreach (var c in cellList) newCells.Add(c);
        root.Set("cells", newCells);

        WriteNotebook(root, path);

        var preview = Truncate(newSource, 100);
        return $"✅ 已插入 {normalizedType} cell 到 {path} cell[{afterIndex}] 之后（新索引 {insertAt}）\n" +
               $"  内容: {preview}";
    }

    /// <summary>删除指定 cell</summary>
    private static string DeleteCell(JNode root, JNode cells, int index, string path)
    {
        if (index < 0 || index >= cells.Count)
            return $"错误：cell_index {index} 超出范围（共 {cells.Count} 个 cell）";

        var cell = cells[index];
        var cellType = cell?["cell_type"]?.AsString() ?? "code";
        var sourcePreview = cell != null ? Truncate(GetSourceText(cell), 80) : "";

        var cellList = cells.Items.ToList();
        cellList.RemoveAt(index);
        var newCells = JNode.Array();
        foreach (var c in cellList) newCells.Add(c);
        root.Set("cells", newCells);

        WriteNotebook(root, path);

        return $"✅ 已删除 {path} 的 cell[{index}]（{cellType}）\n" +
               $"  内容: {sourcePreview}";
    }

    /// <summary>获取 cell 的 source 文本（支持字符串和数组格式）</summary>
    private static string GetSourceText(JNode cell)
    {
        var sourceNode = cell["source"];
        if (sourceNode == null) return "";

        // 数组格式：["line1\n", "line2\n"]
        if (sourceNode.Kind == JKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var line in sourceNode.Items)
                sb.Append(line.AsString() ?? "");
            return sb.ToString();
        }

        // 字符串格式
        return sourceNode.AsString() ?? "";
    }

    /// <summary>设置 cell 的 source（使用规范的字符串格式）</summary>
    private static void SetSourceText(JNode cell, string text)
    {
        // 使用数组格式（每行一个元素），兼容性最好
        var sourceArray = JNode.Array();
        // 按行分割，保留换行符
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i < lines.Length - 1)
                sourceArray.Add(lines[i] + "\n");
            else if (lines[i].Length > 0)
                sourceArray.Add(lines[i]);
        }
        cell["source"] = sourceArray;
    }

    /// <summary>将 notebook JSON 写回磁盘</summary>
    private static void WriteNotebook(JNode root, string path)
    {
        var json = root.ToJson(true);
        File.WriteAllText(path, json + "\n", Encoding.UTF8);
    }

    /// <summary>截断文本用于预览</summary>
    private static string Truncate(string text, int maxLen)
    {
        text = text.Replace("\n", "\\n").Replace("\r", "");
        if (text.Length <= maxLen) return text;
        return text[..maxLen] + "...";
    }
}
