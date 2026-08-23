using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 最小 SQLite 只读解析器 —— 零依赖、AOT 安全，按 SQLite 公开文件格式（fileformat2）手写。
///
/// 仅支持读取表数据（table B-tree 遍历 + record 解码），供跨工具会话桥接读取
/// OpenCode(~/.local/share/opencode/opencode.db) 与 Crush(~/.crush/crush.db) 的会话。
///
/// 限制：
///   - 只读，不处理 WAL（要求数据已 checkpoint 到主文件——这两个 db 的 -wal 通常为 0 字节）
///   - 已实现 overflow page 链式读取（payload 超出页内上限时沿 4 字节指针链补齐）
///   - 不支持 WITHOUT ROWID 表（opencode/crush 均为普通 rowid 表）
///
/// 用法：
///   var t = SqliteReader.Open(dbPath, "messages");       // 读整个表
///   var name = t.GetString(rowIdx, "role");              // 按列名取字符串
///   var ts = t.GetLong(rowIdx, "time_created");          // 按列名取整数
/// </summary>
public static class SqliteReader
{
    /// <summary>一张表的读取结果</summary>
    public sealed class Table
    {
        public string[] Columns = [];
        public List<object?[]> Rows = [];
        Dictionary<string, int> _index = new();

        public int ColumnIndex(string name)
        {
            if (_index.TryGetValue(name, out var i)) return i;
            for (int k = 0; k < Columns.Length; k++)
            {
                if (string.Equals(Columns[k], name, StringComparison.OrdinalIgnoreCase))
                {
                    _index[name] = k;
                    return k;
                }
            }
            return -1;
        }

        public string? GetString(int row, string col)
        {
            var i = ColumnIndex(col);
            return i >= 0 && i < Rows[row].Length ? Rows[row][i] as string : null;
        }

        public long GetLong(int row, string col)
        {
            var i = ColumnIndex(col);
            return i >= 0 && i < Rows[row].Length ? Rows[row][i] as long? ?? 0 : 0;
        }
    }

    /// <summary>打开 db，读取指定表的全部行。失败返回 null。</summary>
    public static Table? Open(string dbPath, string tableName)
    {
        try
        {
            using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[100];
            if (fs.Read(header, 0, 100) < 100) return null;
            if (Encoding.ASCII.GetString(header, 0, 15) != "SQLite format 3") return null;

            int pageSize = header[16] << 8 | header[17];
            if (pageSize == 1) pageSize = 65536;

            // 遍历 page 1 的 sqlite_master，找目标表
            var master = ReadPage(fs, 1, pageSize, new List<object?[]>());
            if (master.Count == 0) return null;

            object?[]? tableRow = null;
            foreach (var row in master)
            {
                // sqlite_master 列: type, name, tbl_name, rootpage, sql
                if (row.Length >= 5 && row[0] as string == "table" && row[1] as string == tableName)
                {
                    tableRow = row;
                    break;
                }
            }
            if (tableRow == null) return null;

            long rootPage = tableRow[3] as long? ?? 0;
            string? createSql = tableRow[4] as string;
            if (rootPage == 0) return null;

            var rows = ReadPage(fs, (int)rootPage, pageSize, new List<object?[]>());
            var table = new Table
            {
                Columns = createSql != null ? ParseColumns(createSql) : [],
                Rows = rows,
            };
            return table;
        }
        catch { return null; }
    }

    // ── B-tree 遍历 ──

    /// <summary>递归读取一个 table B-tree 页，收集所有叶子单元格的行。</summary>
    static List<object?[]> ReadPage(FileStream fs, int pageNum, int pageSize, List<object?[]> acc)
    {
        var buf = new byte[pageSize];
        fs.Seek((long)(pageNum - 1) * pageSize, SeekOrigin.Begin);
        int read = fs.Read(buf, 0, pageSize);

        // 第 1 页前 100 字节是文件头（"SQLite format 3" 等），B-tree 页头从偏移 100 开始
        int hdr = pageNum == 1 ? 100 : 0;
        if (read < hdr + 8) return acc;

        int type = buf[hdr];
        int cellCount = buf[hdr + 3] << 8 | buf[hdr + 4];

        if (type == 0x0D) // leaf table
        {
            // cell pointer array 紧跟在 8 字节页头之后（不在页尾）
            int ptrBase = hdr + 8;
            for (int i = 0; i < cellCount; i++)
            {
                int ptrOff = ptrBase + 2 * i;
                if (ptrOff + 1 >= pageSize) break;
                int cellOff = buf[ptrOff] << 8 | buf[ptrOff + 1];
                if (cellOff <= 0 || cellOff >= pageSize) continue;
                var row = ParseLeafCell(fs, buf, cellOff, pageSize);
                if (row != null) acc.Add(row);
            }
        }
        else if (type == 0x05) // interior table
        {
            // 先处理右侧指针（12 字节页头内，offset 8 处）
            if (pageSize >= hdr + 12)
            {
                int right = (int)Be32(buf, hdr + 8);
                if (right > 0 && right != pageNum) ReadPage(fs, right, pageSize, acc);
            }
            // cell pointer array 紧跟在 12 字节页头之后
            int ptrBase = hdr + 12;
            for (int i = 0; i < cellCount; i++)
            {
                int ptrOff = ptrBase + 2 * i;
                if (ptrOff + 1 >= pageSize) break;
                int cellOff = buf[ptrOff] << 8 | buf[ptrOff + 1];
                if (cellOff + 4 > pageSize) continue;
                int left = (int)Be32(buf, cellOff);
                if (left > 0 && left != pageNum) ReadPage(fs, left, pageSize, acc);
            }
        }
        // 其他类型（index b-tree / 溢出 / 空闲）跳过
        return acc;
    }

    /// <summary>解析一个 table leaf cell → 行值数组。payload 超过页内上限时走 overflow page 链表。</summary>
    static object?[]? ParseLeafCell(FileStream fs, byte[] page, int cellOff, int pageSize)
    {
        try
        {
            int pos = cellOff;
            long payloadSize = ReadVarint(page, ref pos, pageSize);
            _ = ReadVarint(page, ref pos, pageSize); // rowid，无需

            // 页内本地 payload 上限（reserved=0，usableSize=pageSize），对齐 SQLite btree.c fillInCell。
            // 注意：table leaf cell 用 maxLeaf = usableSize-35（不是 index 页的 maxLocal=64/255 公式）。
            int usable = pageSize;
            int maxLeaf = usable - 35;
            int minLeaf = (usable - 12) * 32 / 255 - 23;

            byte[] payload;
            if (payloadSize <= maxLeaf)
            {
                payload = new byte[payloadSize];
                Array.Copy(page, pos, payload, 0, (int)payloadSize);
            }
            else
            {
                int surplus = minLeaf + (int)((payloadSize - minLeaf) % (usable - 4));
                int nLocal = surplus <= maxLeaf ? surplus : minLeaf;
                payload = new byte[payloadSize];
                Array.Copy(page, pos, payload, 0, nLocal);
                pos += nLocal;

                // 4 字节 overflow page 号，随后沿链表补齐剩余 payload
                int ovflPage = (int)Be32(page, pos);
                int copied = nLocal;
                while (ovflPage != 0 && copied < payloadSize)
                {
                    var obuf = new byte[usable];
                    fs.Seek((long)(ovflPage - 1) * usable, SeekOrigin.Begin);
                    int r = fs.Read(obuf, 0, usable);
                    if (r < 4) break;
                    int next = (int)Be32(obuf, 0);
                    int toCopy = (int)Math.Min(payloadSize - copied, usable - 4);
                    Array.Copy(obuf, 4, payload, copied, toCopy);
                    copied += toCopy;
                    ovflPage = next;
                }
            }
            return ParseRecord(payload);
        }
        catch { return null; }
    }

    // ── record 解码 ──

    static object?[] ParseRecord(byte[] payload)
    {
        int pos = 0;
        long headerSize = ReadVarint(payload, ref pos, payload.Length);
        var types = new List<long>();
        while (pos < headerSize)
            types.Add(ReadVarint(payload, ref pos, (int)headerSize));

        var row = new object?[types.Count];
        for (int i = 0; i < types.Count; i++)
            row[i] = ReadSerial(payload, ref pos, types[i]);
        return row;
    }

    static object? ReadSerial(byte[] buf, ref int pos, long serialType)
    {
        switch (serialType)
        {
            case 0: return null;
            case 1: return (long)buf[pos++];
            case 2: { long v = (uint)buf[pos] << 8 | buf[pos + 1]; pos += 2; return v; }
            case 3: { long v = (uint)buf[pos] << 16 | (uint)buf[pos + 1] << 8 | buf[pos + 2]; pos += 3; return v; }
            case 4: { long v = Be32(buf, pos); pos += 4; return v; }
            case 5: { long v = (long)buf[pos] << 40 | (long)buf[pos + 1] << 32 | (long)Be32(buf, pos + 2); pos += 6; return v; }
            case 6: { long v = (long)Be64(buf, pos); pos += 8; return v; }
            case 7: { long bits = (long)Be64(buf, pos); pos += 8; return BitConverter.Int64BitsToDouble(bits); }
            case 8: return 0L;
            case 9: return 1L;
            default:
                if (serialType >= 13 && (serialType & 1) == 1)
                {
                    int len = (int)((serialType - 13) / 2);
                    if (pos + len > buf.Length) { pos = buf.Length; return null; }
                    var s = Encoding.UTF8.GetString(buf, pos, len);
                    pos += len;
                    return s;
                }
                if (serialType >= 12 && (serialType & 1) == 0)
                {
                    int len = (int)((serialType - 12) / 2);
                    if (pos + len > buf.Length) { pos = buf.Length; return null; }
                    var b = new byte[len];
                    Array.Copy(buf, pos, b, 0, len);
                    pos += len;
                    return b;
                }
                return null;
        }
    }

    /// <summary>读取 SQLite varint（7-bit 变长整数）。</summary>
    static long ReadVarint(byte[] buf, ref int pos, int limit)
    {
        long v = 0;
        for (int i = 0; i < 9 && pos < limit; i++)
        {
            int b = buf[pos++];
            if (i == 8) { v = (v << 8) | (uint)b; break; }
            v = (v << 7) | (uint)(b & 0x7F);
            if ((b & 0x80) == 0) break;
        }
        return v;
    }

    static uint Be32(byte[] b, int off) => (uint)b[off] << 24 | (uint)b[off + 1] << 16 | (uint)b[off + 2] << 8 | b[off + 3];
    static ulong Be64(byte[] b, int off) =>
        (ulong)b[off] << 56 | (ulong)b[off + 1] << 48 | (ulong)b[off + 2] << 40 | (ulong)b[off + 3] << 32 |
        (ulong)b[off + 4] << 24 | (ulong)b[off + 5] << 16 | (ulong)b[off + 6] << 8 | b[off + 7];

    // ── CREATE TABLE 列名解析 ──

    /// <summary>从 CREATE TABLE 语句解析列名（按顶层逗号分割，跳过表级约束）。</summary>
    static string[] ParseColumns(string createSql)
    {
        int start = createSql.IndexOf('(');
        int end = createSql.LastIndexOf(')');
        if (start < 0 || end <= start) return [];

        var inner = createSql[(start + 1)..end];
        var cols = new List<string>();
        int depth = 0, segStart = 0;
        for (int i = 0; i < inner.Length; i++)
        {
            char c = inner[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0)
            {
                AddColumn(cols, inner[segStart..i]);
                segStart = i + 1;
            }
        }
        AddColumn(cols, inner[segStart..]);
        return cols.ToArray();
    }

    static void AddColumn(List<string> cols, string segment)
    {
        var name = FirstToken(segment);
        if (name == null) return;
        var up = name.ToUpperInvariant();
        if (up is "CONSTRAINT" or "PRIMARY" or "FOREIGN" or "UNIQUE" or "CHECK")
            return; // 表级约束，不是列
        cols.Add(name);
    }

    /// <summary>取段的第一个标识符（去引号/反引号）。</summary>
    static string? FirstToken(string segment)
    {
        int i = 0;
        while (i < segment.Length && char.IsWhiteSpace(segment[i])) i++;
        if (i >= segment.Length) return null;

        char q = segment[i];
        if (q is '"' or '`' or '\'')
        {
            int j = i + 1;
            var sb = new StringBuilder();
            while (j < segment.Length && segment[j] != q) { sb.Append(segment[j]); j++; }
            return sb.ToString();
        }

        int k = i;
        while (k < segment.Length && !char.IsWhiteSpace(segment[k]) && segment[k] != '\t') k++;
        return segment[i..k];
    }
}
