using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VMLRuntime.Device;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(DeviceProfile))]
[JsonSerializable(typeof(CpuProfile))]
[JsonSerializable(typeof(DisplayProfile))]
[JsonSerializable(typeof(KeyboardProfile))]
[JsonSerializable(typeof(MouseProfile))]
[JsonSerializable(typeof(MemoryMapEntry))]
[JsonSerializable(typeof(AudioProfile))]
[JsonSerializable(typeof(List<string>))]
internal partial class DeviceProfileJsonContext : JsonSerializerContext { }

/// <summary>JSON 转换器：支持 "0x..." 十六进制字符串和普通数字</summary>
public class HexIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrEmpty(s)) return 0;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("$", StringComparison.OrdinalIgnoreCase))
                return int.Parse(s.AsSpan(s[0] == '$' ? 1 : 2), NumberStyles.HexNumber, null);
            return int.Parse(s, NumberStyles.Integer, null);
        }
        if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32();
        return 0;
    }
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

/// <summary>CPU 配置</summary>
public class CpuProfile
{
    [JsonPropertyName("architecture")] public string Architecture { get; set; } = "vml";
    [JsonPropertyName("speed")] public int Speed { get; set; } = 1_000_000;
}

/// <summary>显示配置</summary>
public class DisplayProfile
{
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("mode")] public int Mode { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; } = 80;
    [JsonPropertyName("height")] public int Height { get; set; } = 25;
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("framebufferAddress")] public int FramebufferAddress { get; set; } = 0xB8000;
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("graphicsFramebuffer")] public int GraphicsFramebuffer { get; set; } = 0xB80000;
    [JsonPropertyName("type")] public string Type { get; set; } = "text"; // text, graphics, both
    [JsonPropertyName("bytesPerCell")] public int BytesPerCell { get; set; } = 2;
    [JsonPropertyName("bytesPerPixel")] public int BytesPerPixel { get; set; } = 3;
    [JsonPropertyName("refreshRate")] public int RefreshRate { get; set; } = 60;
}

/// <summary>键盘配置</summary>
public class KeyboardProfile
{
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("dataAddress")] public int DataAddress { get; set; } = 0x60;
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("statusAddress")] public int StatusAddress { get; set; } = 0x64;
    [JsonPropertyName("layout")] public string Layout { get; set; } = "us";
}

/// <summary>鼠标配置</summary>
public class MouseProfile
{
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("xAddress")] public int XAddress { get; set; } = 0x300;
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("yAddress")] public int YAddress { get; set; } = 0x302;
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("buttonAddress")] public int ButtonAddress { get; set; } = 0x304;
}

/// <summary>内存映射条目 — 定义一段内存区域的访问权限</summary>
public class MemoryMapEntry
{
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("start")] public int Start { get; set; }
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("size")] public int Size { get; set; }
    [JsonPropertyName("readable")] public bool Readable { get; set; } = true;
    [JsonPropertyName("writable")] public bool Writable { get; set; } = false;
    [JsonPropertyName("executable")] public bool Executable { get; set; } = false;
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

/// <summary>音频配置</summary>
public class AudioProfile
{
    [JsonPropertyName("type")] public string Type { get; set; } = "pc-speaker";
    [JsonPropertyName("channels")] public int Channels { get; set; } = 1;
}

/// <summary>设备描述文件 — 定义一台模拟机器的完整硬件拓扑</summary>
public class DeviceProfile
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Generic";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("memorySize")] public int MemorySize { get; set; } = 640 * 1024;
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("dataBase")] public int DataBase { get; set; } = 0x400; // 数据段起始 (IBM-PC: 0x8000)
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("stackTop")] public int StackTop { get; set; } = 0; // 栈顶地址，0=自动计算
    [JsonConverter(typeof(HexIntConverter))]
    [JsonPropertyName("programStart")] public int ProgramStart { get; set; } = 0; // 程序起始地址
    [JsonPropertyName("year")] public int Year { get; set; } = 1981;

    [JsonPropertyName("cpu")] public CpuProfile? Cpu { get; set; }
    [JsonPropertyName("display")] public DisplayProfile? Display { get; set; }
    [JsonPropertyName("keyboard")] public KeyboardProfile? Keyboard { get; set; }
    [JsonPropertyName("mouse")] public MouseProfile? Mouse { get; set; }
    [JsonPropertyName("audio")] public AudioProfile? Audio { get; set; }

    [JsonPropertyName("devices")]
    public List<string> Devices { get; set; } = new() { "console", "stdin", "stdout", "stderr", "kbd" };

    [JsonPropertyName("memoryMap")]
    public List<MemoryMapEntry> MemoryMap { get; set; } = new();

    /// <summary>应用此配置到 VmConfig</summary>
    public VmConfig ToVmConfig()
    {
        var cfg = new VmConfig { MemorySize = MemorySize, DataBase = DataBase, StackTop = StackTop, ProgramStart = ProgramStart };

        if (Cpu != null)
        {
            cfg.CpuSpeed = Cpu.Speed;
        }

        if (Display != null)
        {
            cfg.VgaDisplay.Mode = Display.Mode;
            cfg.VgaDisplay.Width = Display.Width;
            cfg.VgaDisplay.Height = Display.Height;
            cfg.VgaStartAddress = Display.FramebufferAddress;
            cfg.VgaDisplay.BytesPerCell = Display.BytesPerCell;
        }

        if (Keyboard != null)
        {
            cfg.KeyboardDataAddress = Keyboard.DataAddress;
            cfg.KeyboardStatusAddress = Keyboard.StatusAddress;
        }

        if (Mouse != null)
        {
            cfg.MouseXAddress = Mouse.XAddress;
            cfg.MouseYAddress = Mouse.YAddress;
            cfg.MouseButtonAddress = Mouse.ButtonAddress;
        }

        foreach (var m in MemoryMap)
        {
            cfg.MemoryMap.Add(new VmMemoryMapEntry
            {
                Start = m.Start, Size = m.Size,
                Readable = m.Readable, Writable = m.Writable, Executable = m.Executable,
                Description = m.Description
            });
        }

        return cfg;
    }

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
    };

    public static DeviceProfile? LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, DeviceProfileJsonContext.Default.DeviceProfile);
    }

    public static Dictionary<string, string> ListAvailable()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 构建搜索路径（三层: ./ → $VML_HOME → AppDomain 目录树）
        var searchPathList = new List<string>();

        // 1. 当前工作目录
        searchPathList.Add(Path.Combine(Directory.GetCurrentDirectory(), "Config"));
        searchPathList.Add(Path.Combine(Directory.GetCurrentDirectory(), "config"));
        searchPathList.Add(Path.Combine(Directory.GetCurrentDirectory(), "Devices"));
        searchPathList.Add(Path.Combine(Directory.GetCurrentDirectory(), "devices"));

        // 2. $VML_HOME (工具集根路径)
        var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
            ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
        if (!string.IsNullOrEmpty(vmlHome))
        {
            searchPathList.Add(Path.Combine(vmlHome, "Config"));
            searchPathList.Add(Path.Combine(vmlHome, "config"));
            searchPathList.Add(Path.Combine(vmlHome, "Devices"));
            searchPathList.Add(Path.Combine(vmlHome, "devices"));
        }

        // 3. AppDomain 目录树 (开发/发布环境)
        searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config"));
        searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"));
        searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Devices"));
        searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices"));
        // 向上搜索 (开发时 Config/ 在项目根目录)
        for (int up = 1; up <= 5; up++)
        {
            var prefix = string.Join("/", Enumerable.Repeat("..", up));
            searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, prefix, "Config"));
            searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, prefix, "config"));
            searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, prefix, "Devices"));
            searchPathList.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, prefix, "devices"));
        }

        var searchPaths = searchPathList.ToArray();
        foreach (var dir in searchPaths)
        {
            var norm = Path.GetFullPath(dir);
            if (!Directory.Exists(norm)) continue;
            foreach (var f in Directory.GetFiles(norm, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (!result.ContainsKey(name)) result[name] = f;
            }
        }
        return result;
    }

    public static DeviceProfile? LoadByName(string name)
    {
        var available = ListAvailable();
        if (available.TryGetValue(name, out var path))
            return LoadFromFile(path);
        return null;
    }
}
