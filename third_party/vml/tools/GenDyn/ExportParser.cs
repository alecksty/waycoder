using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GenDyn;

/// <summary>
/// 从动态库(.dll/.so/.dylib)和可选的 C 头文件中解析导出函数
/// </summary>
public static class ExportParser
{
    /// <summary>
    /// 解析动态库导出符号 (仅名称，不含类型)
    /// </summary>
    public static List<string> ParseDllExports(string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"动态库未找到: {dllPath}");

        var exports = new List<string>();

        if (OperatingSystem.IsWindows())
            exports = ParseWindowsExports(dllPath);
        else if (OperatingSystem.IsLinux())
            exports = ParseLinuxExports(dllPath);
        else if (OperatingSystem.IsMacOS())
            exports = ParseMacExports(dllPath);

        return exports.Distinct().OrderBy(n => n).ToList();
    }

    private static List<string> ParseWindowsExports(string dllPath)
    {
        var exports = new List<string>();

        // 尝试 dumpbin (MSVC)
        try
        {
            var psi = new ProcessStartInfo("dumpbin", $"/exports \"{dllPath}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                // 匹配: ordinal hint RVA name
                // 格式:    1    0 00001000 glh_init
                var regex = new Regex(@"^\s*\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)", RegexOptions.Multiline);
                foreach (Match m in regex.Matches(output))
                {
                    var name = m.Groups[1].Value;
                    if (!IsBoringExport(name))
                        exports.Add(name);
                }
            }
        }
        catch
        {
            // dumpbin 不可用，尝试 PowerShell
            exports = ParseWindowsExportsPowerShell(dllPath);
        }

        if (exports.Count == 0)
            exports = ParseWindowsExportsPowerShell(dllPath);

        return exports;
    }

    private static List<string> ParseWindowsExportsPowerShell(string dllPath)
    {
        var exports = new List<string>();
        try
        {
            var psi = new ProcessStartInfo("powershell", $"-Command \"& {{ $asm = [System.Reflection.Assembly]::LoadFile('{dllPath.Replace('\\', '/')}'); $asm.GetExportedTypes(); foreach($t in $asm.GetExportedTypes()) {{ $t.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static) | ForEach-Object {{ $_.Name }} }} }}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                foreach (var line in output.Split('\n'))
                {
                    var name = line.Trim();
                    if (!string.IsNullOrEmpty(name) && !IsBoringExport(name))
                        exports.Add(name);
                }
            }
        }
        catch { }

        // 最终回退: 使用 dumpbin 的备用位置
        if (exports.Count == 0)
        {
            try
            {
                var vsPath = FindDumpbin();
                if (!string.IsNullOrEmpty(vsPath))
                {
                    var psi = new ProcessStartInfo(vsPath, $"/exports \"{dllPath}\"")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        var output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        var regex = new Regex(@"^\s*\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)", RegexOptions.Multiline);
                        foreach (Match m in regex.Matches(output))
                        {
                            var name = m.Groups[1].Value;
                            if (!IsBoringExport(name))
                                exports.Add(name);
                        }
                    }
                }
            }
            catch { }
        }

        return exports;
    }

    private static string? FindDumpbin()
    {
        // 1) 通过 vswhere 发现 VS 2022+，扫描最新 MSVC 版本
        var vsWhere = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (File.Exists(vsWhere))
        {
            var psi = new ProcessStartInfo(vsWhere, "-latest -products * -property installationPath")
            {
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            var p = Process.Start(psi);
            if (p != null)
            {
                var vsPath = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                if (!string.IsNullOrEmpty(vsPath))
                {
                    var toolsDir = Path.Combine(vsPath, "VC", "Tools", "MSVC");
                    if (Directory.Exists(toolsDir))
                    {
                        foreach (var verDir in Directory.GetDirectories(toolsDir))
                        {
                            var dumpbin = Path.Combine(verDir, "bin", "Hostx64", "x64", "dumpbin.exe");
                            if (File.Exists(dumpbin)) return dumpbin;
                        }
                    }
                }
            }
        }

        // 2) 扫描 VS 2022 所有版本
        var baseDir = @"C:\Program Files\Microsoft Visual Studio\2022";
        if (Directory.Exists(baseDir))
        {
            foreach (var edition in Directory.GetDirectories(baseDir))
            {
                var toolsDir = Path.Combine(edition, "VC", "Tools", "MSVC");
                if (Directory.Exists(toolsDir))
                {
                    foreach (var verDir in Directory.GetDirectories(toolsDir))
                    {
                        var dumpbin = Path.Combine(verDir, "bin", "Hostx64", "x64", "dumpbin.exe");
                        if (File.Exists(dumpbin)) return dumpbin;
                    }
                }
            }
        }

        // 3) PATH 中查找
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var d in pathDirs)
        {
            var probe = Path.Combine(d, "dumpbin.exe");
            if (File.Exists(probe)) return probe;
        }

        return null;
    }

    private static List<string> ParseLinuxExports(string dllPath)
    {
        var exports = new List<string>();
        try
        {
            var psi = new ProcessStartInfo("nm", $"-D --defined-only \"{dllPath}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                // 格式: 0000000000001000 T function_name
                var regex = new Regex(@"^[0-9A-Fa-f]+\s+[Tt]\s+(\S+)", RegexOptions.Multiline);
                foreach (Match m in regex.Matches(output))
                {
                    var name = m.Groups[1].Value;
                    if (!IsBoringExport(name))
                        exports.Add(name);
                }
            }
        }
        catch { }
        return exports;
    }

    private static List<string> ParseMacExports(string dllPath)
    {
        var exports = new List<string>();
        try
        {
            var psi = new ProcessStartInfo("nm", $"-gU \"{dllPath}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                var regex = new Regex(@"^[0-9A-Fa-f]+\s+[Tt]\s+_?(\S+)", RegexOptions.Multiline);
                foreach (Match m in regex.Matches(output))
                {
                    var name = m.Groups[1].Value;
                    if (!IsBoringExport(name))
                        exports.Add(name);
                }
            }
        }
        catch { }
        return exports;
    }

    /// <summary>
    /// 过滤掉 CRT/编译器内部导出符号
    /// </summary>
    private static bool IsBoringExport(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        if (name.StartsWith("__")) return true;
        if (name.StartsWith("_")) return true; // macOS 下划线前缀
        if (name.Contains('$')) return true;
        if (name.Contains('<')) return true; // C++ mangled names
        // CRT 内部
        if (name.StartsWith("_Cxx")) return true;
        if (name.StartsWith("_Xcpt")) return true;
        return false;
    }

    /// <summary>
    /// 从 C 头文件解析函数声明以获取类型信息
    /// </summary>
    public static List<FunctionInfo> ParseHeaderFile(string headerPath)
    {
        if (!File.Exists(headerPath))
            throw new FileNotFoundException($"头文件未找到: {headerPath}");

        var content = File.ReadAllText(headerPath);
        var functions = new List<FunctionInfo>();

        // 解析 struct 定义以获取大小
        var structSizes = ParseStructDefinitions(headerPath);

        // 匹配 extern 函数声明 (支持 struct/union 返回类型)
        // extern int func(int a, float b);
        // extern struct Point func(int a);
        var regex = new Regex(
            @"extern\s+((?:const\s+)?(?:struct\s+|union\s+)?\w+(?:\s*\*)?)\s+(\w+)\s*\(([^)]*)\)\s*;",
            RegexOptions.Multiline | RegexOptions.Singleline);

        foreach (Match m in regex.Matches(content))
        {
            var retTypeStr = m.Groups[1].Value.Trim();
            var funcName = m.Groups[2].Value.Trim();
            var paramsStr = m.Groups[3].Value.Trim();

            var func = new FunctionInfo
            {
                Name = funcName,
                OriginalName = funcName,
                ReturnType = ParseCType(retTypeStr)
            };

            if (!string.IsNullOrEmpty(paramsStr) && paramsStr != "void")
            {
                var paramParts = paramsStr.Split(',');
                for (int i = 0; i < paramParts.Length; i++)
                {
                    var part = paramParts[i].Trim();
                    // 格式: "int name" 或 "const char* name" — 按最后一个空格分割类型和参数名
                    var lastSpace = part.LastIndexOf(' ');
                    string typeStr, paramName;
                    if (lastSpace > 0)
                    {
                        typeStr = part.Substring(0, lastSpace).Trim();
                        paramName = part.Substring(lastSpace + 1).Trim();
                        // 将参数名前导的 * 移回类型 (如 "*path" → typeStr="const char*", paramName="path")
                        if (paramName.StartsWith('*'))
                        {
                            typeStr += "*";
                            paramName = paramName.Substring(1).TrimStart();
                        }
                    }
                    else
                    {
                        typeStr = part;
                        paramName = $"arg{i}";
                    }
                    // 参数名残留 * 清理 (如 "char*name" → typeStr 已包含 *)
                    if (paramName.StartsWith('*'))
                    {
                        typeStr += "*";
                        paramName = paramName.Substring(1);
                    }
                    var paramType = ParseCType(typeStr);

                    var paramInfo = new ParamInfo { Name = paramName, Type = paramType };
                    // 解析 struct 大小
                    if (paramType == ParamType.Struct || paramType == ParamType.StructPtr)
                    {
                        string structName = ExtractStructName(typeStr);
                        if (structSizes.TryGetValue(structName, out int sz))
                            paramInfo.StructSize = sz;
                    }
                    func.Params.Add(paramInfo);
                }
            }

            functions.Add(func);
        }

        return functions;
    }

    private static ParamType ParseCType(string cType)
    {
        string trimmed = cType.Trim();
        return trimmed switch
        {
            "float" => ParamType.Float,
            "double" => ParamType.Float64,
            "void" => ParamType.Void,
            "const char*" or "char*" or "const char *" or "char *"
                or "const char* const" => ParamType.String,
            "long" or "long long" or "int64_t" => ParamType.Int64,
            _ => DetectStructType(trimmed)
        };
    }

    /// <summary>检测 struct/union 类型 (按值或指针)</summary>
    private static ParamType DetectStructType(string cType)
    {
        // struct X* / const struct X* (pointer)
        if (cType.EndsWith('*'))
        {
            string inner = cType.TrimEnd('*').Trim();
            if (inner.StartsWith("struct ") || inner.StartsWith("union ") ||
                inner.StartsWith("const struct ") || inner.StartsWith("const union "))
                return ParamType.StructPtr;
        }
        // struct X / const struct X (by-value)
        if (cType.StartsWith("struct ") || cType.StartsWith("union ") ||
            cType.StartsWith("const struct ") || cType.StartsWith("const union "))
            return ParamType.Struct;
        return ParamType.Int;
    }

    /// <summary>从 C 头文件解析 struct 定义，返回 struct 名 → packed 大小(字节) 的字典</summary>
    public static Dictionary<string, int> ParseStructDefinitions(string headerPath)
    {
        var structSizes = new Dictionary<string, int>();
        if (!File.Exists(headerPath)) return structSizes;

        var content = File.ReadAllText(headerPath);
        // 匹配: typedef struct { ... } Name;  或  struct Name { ... };
        var structRegex = new Regex(
            @"(?:typedef\s+)?struct\s+(?:(\w+)\s*)?\{([^}]+)\}\s*(?:(\w+))?\s*;",
            RegexOptions.Singleline);

        foreach (Match m in structRegex.Matches(content))
        {
            string tagName = m.Groups[1].Value;   // struct 标签名
            string body = m.Groups[2].Value;       // 成员列表
            string typedefName = m.Groups[3].Value; // typedef 别名

            int size = CalculatePackedStructSize(body);
            if (!string.IsNullOrEmpty(typedefName))
                structSizes[typedefName] = size;
            if (!string.IsNullOrEmpty(tagName))
                structSizes[tagName] = size;
        }
        return structSizes;
    }

    /// <summary>基于成员类型声明计算 struct packed 大小 (与 VML 布局一致)</summary>
    private static int CalculatePackedStructSize(string memberBlock)
    {
        int total = 0;
        // 每个成员一行: "int x;"  "float y;"  "char name[32];"
        var members = memberBlock.Split(';');
        foreach (var member in members)
        {
            string m = member.Trim();
            if (string.IsNullOrEmpty(m)) continue;
            // 移除注释
            int commentIdx = m.IndexOf("//");
            if (commentIdx >= 0) m = m.Substring(0, commentIdx).Trim();
            if (string.IsNullOrEmpty(m)) continue;

            // 提取类型 (第一个词 或 前两个词 如 "unsigned int" / "const char*")
            string[] parts = m.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            // 最后一部分是成员名 (可能带数组后缀)
            string typeStr = string.Join(" ", parts.Take(parts.Length - 1));
            string memberName = parts.Last();

            int baseSize = typeStr switch
            {
                "char" or "unsigned char" or "signed char" => 1,
                "short" or "unsigned short" => 2,
                "int" or "unsigned int" or "float" or "long" => 4,
                "double" or "long long" or "unsigned long long" => 8,
                _ => typeStr.StartsWith("struct ") || typeStr.StartsWith("union ") ? 4 : 4
            };

            // 检查数组后缀 [N]
            var arrMatch = Regex.Match(memberName, @"\[(\d+)\]");
            if (arrMatch.Success)
                baseSize *= int.Parse(arrMatch.Groups[1].Value);

            total += baseSize;
        }
        return total > 0 ? total : 4;
    }

    /// <summary>
    /// 解析 struct 类型名 (去除 const/struct 前缀, 提取纯名称)
    /// </summary>
    public static string ExtractStructName(string cType)
    {
        string name = cType.Trim().TrimEnd('*').Trim();
        name = name.Replace("const", "").Replace("struct", "").Replace("union", "").Trim();
        return name;
    }

    /// <summary>
    /// 将无类型的导出列表转换为 FunctionInfo (默认全部 int 参数)
    /// </summary>
    public static List<FunctionInfo> ExportsToFunctions(List<string> exports, int defaultParamCount = 2)
    {
        var functions = new List<FunctionInfo>();
        foreach (var name in exports)
        {
            // 猜测参数数量 (基于前缀启发式)
            int paramCount = defaultParamCount;
            if (name.Contains("init")) paramCount = 2;
            else if (name.Contains("close") || name.Contains("destroy") || name.Contains("end")) paramCount = 0;
            else if (name.Contains("get") || name.Contains("should")) paramCount = 1;

            var func = new FunctionInfo
            {
                Name = name,
                OriginalName = name,
                ReturnType = ParamType.Int,
            };

            for (int i = 0; i < paramCount; i++)
                func.Params.Add(new ParamInfo { Name = $"arg{i}", Type = ParamType.Int });

            functions.Add(func);
        }
        return functions;
    }
}
