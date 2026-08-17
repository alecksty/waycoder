using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// CFB（Compound File Binary / OLE2 复合文档）解析器。
/// 手搓实现，零外部依赖，AOT 兼容。
///
/// 老式二进制 Office 文档（.doc/.xls/.ppt）以及 WPS 的老格式（.wps/.et/.dps）
/// 本质都是 CFB 容器：一个文件内部按「扇区 + FAT + 目录」组织成若干命名流，
/// 如 WordDocument（文字）、Workbook/Book（表格）、PowerPoint Document（演示）。
/// 本解析器负责把这些流原样取出，供上层做文本提取。
///
/// 规范参考：[MS-CFB] Compound File Binary File Format。
/// </summary>
public static class CfbParser
{
    // FAT 特殊链值
    public const uint EndOfChain = 0xFFFFFFFE;
    public const uint FreeSect = 0xFFFFFFFF;
    public const uint FatSect = 0xFFFFFFFD;
    public const uint DifSect = 0xFFFFFFFC;

    /// <summary>判断字节是否为 CFB 签名（D0 CF 11 E0 A1 B1 1A E1）。</summary>
    public static bool IsCfb(byte[] data) =>
        data.Length >= 8 &&
        data[0] == 0xD0 && data[1] == 0xCF && data[2] == 0x11 && data[3] == 0xE0 &&
        data[4] == 0xA1 && data[5] == 0xB1 && data[6] == 0x1A && data[7] == 0xE1;

    /// <summary>解析 CFB 容器，失败返回 null（损坏/非法输入）。</summary>
    public static CfbDocument? Open(byte[] data)
    {
        try
        {
            if (!IsCfb(data)) return null;
            if (data.Length < 512) return null;

            int sectorShift = Bin.U16(data, 30);
            int miniSectorShift = Bin.U16(data, 32);
            if (sectorShift < 7 || sectorShift > 12) return null; // 512 ~ 4096
            if (miniSectorShift < 2 || miniSectorShift > 12) return null;

            int sectorSize = 1 << sectorShift;
            int miniSectorSize = 1 << miniSectorShift;

            uint numFatSectors = Bin.U32(data, 44);
            uint firstDirSector = Bin.U32(data, 48);
            uint miniStreamCutoff = Bin.U32(data, 56);
            uint firstMiniFatSector = Bin.U32(data, 60);
            uint numMiniFatSectors = Bin.U32(data, 64);
            uint firstDifatSector = Bin.U32(data, 68);
            uint numDifatSectors = Bin.U32(data, 72);

            // 收集 FAT 扇区号（DIFAT 数组 + DIFAT 链）
            var fatSectors = new List<uint>(128);
            for (int i = 0; i < 109; i++)
            {
                uint v = Bin.U32(data, 76 + i * 4);
                if (v != FreeSect) fatSectors.Add(v);
            }

            uint difatSector = firstDifatSector;
            uint remaining = numDifatSectors;
            int entriesPerSector = sectorSize / 4;
            while (difatSector != EndOfChain && difatSector != FreeSect && remaining-- > 0)
            {
                int off = SectorOffset(difatSector, sectorSize);
                if (off + sectorSize > data.Length) break;
                for (int i = 0; i < entriesPerSector - 1; i++)
                {
                    uint v = Bin.U32(data, off + i * 4);
                    if (v != FreeSect) fatSectors.Add(v);
                }
                difatSector = Bin.U32(data, off + (entriesPerSector - 1) * 4);
            }

            if (fatSectors.Count == 0) return null;
            if (fatSectors.Count > 1_000_000 / Math.Max(sectorSize / 4, 1)) return null; // 防御损坏

            // 读取 FAT
            var fat = new uint[fatSectors.Count * (sectorSize / 4)];
            int fatIdx = 0;
            foreach (var fs in fatSectors)
            {
                int off = SectorOffset(fs, sectorSize);
                if (off + sectorSize > data.Length) return null;
                for (int i = 0; i < sectorSize / 4; i++)
                    fat[fatIdx++] = Bin.U32(data, off + i * 4);
            }

            var doc = new CfbDocument(data, sectorSize, miniSectorSize, miniStreamCutoff, fat);

            // 读取目录
            var dirBytes = doc.ReadChain(firstDirSector);
            doc.LoadDirectory(dirBytes);

            // 读取 mini 流（root 条目指向的常规扇区链）
            var root = doc.RootEntry;
            if (root != null && root.StartSector != EndOfChain && root.Size > 0)
                doc.MiniStream = doc.ReadChain(root.StartSector, (int)Math.Min(root.Size, (ulong)data.Length));

            // 读取 mini FAT
            if (numMiniFatSectors > 0 && firstMiniFatSector != EndOfChain && firstMiniFatSector != FreeSect)
            {
                var miniFatBytes = doc.ReadChain(firstMiniFatSector);
                if (miniFatBytes.Length > 0)
                {
                    var mf = new uint[miniFatBytes.Length / 4];
                    for (int i = 0; i < mf.Length; i++)
                        mf[i] = Bin.U32(miniFatBytes, i * 4);
                    doc.MiniFat = mf;
                }
            }

            return doc;
        }
        catch
        {
            return null;
        }
    }

    private static int SectorOffset(uint sector, int sectorSize) => (int)((sector + 1) * (uint)sectorSize);
}

/// <summary>已解析的 CFB 文档，提供按名取流。</summary>
public sealed class CfbDocument
{
    private readonly byte[] _data;
    private readonly int _sectorSize;
    private readonly int _miniSectorSize;
    private readonly uint _miniCutoff;
    private readonly uint[] _fat;
    private readonly List<CfbEntry> _entries = new();

    internal byte[] MiniStream { get; set; } = Array.Empty<byte>();
    internal uint[] MiniFat { get; set; } = Array.Empty<uint>();

    internal CfbDocument(byte[] data, int sectorSize, int miniSectorSize, uint miniCutoff, uint[] fat)
    {
        _data = data;
        _sectorSize = sectorSize;
        _miniSectorSize = miniSectorSize;
        _miniCutoff = miniCutoff;
        _fat = fat;
    }

    internal CfbEntry? RootEntry { get; private set; }

    /// <summary>目录中所有流（type=2）的名称。</summary>
    public IEnumerable<string> StreamNames => _entries.Where(e => e.Type == 2).Select(e => e.Name);

    internal void LoadDirectory(byte[] dirBytes)
    {
        for (int i = 0; i + 128 <= dirBytes.Length; i += 128)
        {
            int type = dirBytes[i + 66];
            if (type == 0) continue; // 空槽

            int nameLen = Bin.U16(dirBytes, i + 64);
            string name = DecodeName(dirBytes, i, nameLen);
            uint start = Bin.U32(dirBytes, i + 116);
            ulong size = Bin.U64(dirBytes, i + 120);
            var entry = new CfbEntry(name, type, start, size);
            _entries.Add(entry);
            if (type == 5) RootEntry = entry;
        }
    }

    private static string DecodeName(byte[] buf, int off, int byteLen)
    {
        int charCount = Math.Min(byteLen, 64) / 2;
        var sb = new StringBuilder(charCount);
        for (int i = 0; i < charCount; i++)
        {
            ushort u = Bin.U16(buf, off + i * 2);
            if (u == 0) break;
            sb.Append((char)u);
        }
        return sb.ToString();
    }

    /// <summary>把不可信的流尺寸钳制到文件实际大小（防目录项 Size 字段声明超大导致 OOM）。</summary>
    private int ClampToData(ulong size) => (int)Math.Min(size, (ulong)_data.Length);

    /// <summary>按名取流（大小写不敏感），未找到返回 null。</summary>
    public byte[]? GetStream(string name)
    {
        foreach (var e in _entries)
        {
            if (e.Type != 2) continue;
            if (!string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

            int size = ClampToData(e.Size); // 尺寸字段不可信，钳制到文件实际大小防 OOM
            if (e.Size >= _miniCutoff)
                return ReadChain(e.StartSector, size);
            return ReadMiniChain(e.StartSector, size);
        }
        return null;
    }

    /// <summary>判断是否存在指定流。</summary>
    public bool HasStream(string name) => _entries.Any(e => e.Type == 2 &&
        string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>沿 FAT 链读取常规扇区，直到 EndOfChain（完整链）。</summary>
    internal byte[] ReadChain(uint start)
    {
        using var ms = new MemoryStream();
        uint s = start;
        int guard = 0;
        while (s != CfbParser.EndOfChain && s != CfbParser.FreeSect && s < (uint)_fat.Length && guard++ < 1_000_000)
        {
            int off = (int)((s + 1) * (uint)_sectorSize);
            if (off + _sectorSize > _data.Length) break;
            ms.Write(_data, off, _sectorSize);
            s = _fat[s];
        }
        return ms.ToArray();
    }

    /// <summary>沿 FAT 链读取常规扇区，读取恰好 size 字节。</summary>
    internal byte[] ReadChain(uint start, int size)
    {
        if (size <= 0) return Array.Empty<byte>();
        var buf = new byte[size];
        uint s = start;
        int written = 0;
        int guard = 0;
        while (s != CfbParser.EndOfChain && s != CfbParser.FreeSect && s < (uint)_fat.Length && written < size && guard++ < 1_000_000)
        {
            int off = (int)((s + 1) * (uint)_sectorSize);
            if (off + _sectorSize > _data.Length) break;
            int n = Math.Min(_sectorSize, size - written);
            Array.Copy(_data, off, buf, written, n);
            written += n;
            s = _fat[s];
        }
        return buf;
    }

    /// <summary>沿 mini FAT 链读取 mini 扇区（小流，&lt; cutoff）。</summary>
    private byte[] ReadMiniChain(uint start, int size)
    {
        if (size <= 0) return Array.Empty<byte>();
        var buf = new byte[size];
        uint s = start;
        int written = 0;
        int guard = 0;
        while (s != CfbParser.EndOfChain && s != CfbParser.FreeSect && s < (uint)MiniFat.Length && written < size && guard++ < 1_000_000)
        {
            int srcOff = (int)(s * (uint)_miniSectorSize);
            if (srcOff + _miniSectorSize > MiniStream.Length) break;
            int n = Math.Min(_miniSectorSize, size - written);
            Array.Copy(MiniStream, srcOff, buf, written, n);
            written += n;
            s = MiniFat[s];
        }
        return buf;
    }
}

/// <summary>CFB 目录条目。</summary>
internal sealed class CfbEntry
{
    public string Name;
    public int Type;          // 1=storage 2=stream 5=root
    public uint StartSector;
    public ulong Size;

    public CfbEntry(string name, int type, uint startSector, ulong size)
    {
        Name = name;
        Type = type;
        StartSector = startSector;
        Size = size;
    }
}

/// <summary>小端二进制读取辅助（CFB/BIFF/PPT 共用）。</summary>
internal static class Bin
{
    public static ushort U16(byte[] b, int o) => (ushort)((uint)b[o] | ((uint)b[o + 1] << 8));
    public static short I16(byte[] b, int o) => (short)((uint)b[o] | ((uint)b[o + 1] << 8));
    public static uint U32(byte[] b, int o) => (uint)b[o] | ((uint)b[o + 1] << 8) | ((uint)b[o + 2] << 16) | ((uint)b[o + 3] << 24);
    public static int I32(byte[] b, int o) => (int)U32(b, o);
    public static ulong U64(byte[] b, int o) => (ulong)U32(b, o) | ((ulong)U32(b, o + 4) << 32);
}
