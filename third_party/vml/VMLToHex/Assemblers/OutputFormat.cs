using System;
using System.IO;
using System.Text;
using VMLToHex.Formatters;

namespace VMLToHex.Assemblers;

public static class OutputFormat
{
    public static void Write(string format, byte[] data, int baseAddr, string outputFile)
    {
        switch (format.ToLower())
        {
            case "bin":   WriteBin(data, outputFile); break;
            case "hex":   WriteHex(data, baseAddr, outputFile); break;
            case "elf":   WriteElf(data, baseAddr, outputFile); break;
            case "exe":   WriteExe(data, outputFile); break;
            case "com":   WriteCom(data, outputFile); break;
            case "dump":  WriteDump(data, baseAddr, outputFile); break;
            case "s19":   WriteS19(data, baseAddr, outputFile); break;
            case "srec":  WriteS19(data, baseAddr, outputFile); break;
            case "vmb":   WriteVmb(data, outputFile); break;
            case "vmapp": WriteVmApp(data, outputFile); break;
            case "wasm":
            case "wat":
                // Wasm/Wat is text format — raw copy as UTF-8 text
                var watText = System.Text.Encoding.UTF8.GetString(data);
                File.WriteAllText(outputFile, watText);
                Console.WriteLine($"WAT: {data.Length} 字节 -> {outputFile}");
                break;
            case "jvm":
            case "java":
                var jvmData = JvmClassWriter.Write(data, Path.GetFileNameWithoutExtension(outputFile));
                File.WriteAllBytes(outputFile, jvmData);
                Console.WriteLine($"JVM: {data.Length} 字节 -> {outputFile}");
                break;
            case "dotnet":
            case "net":
                var netData = DotNetPeWriter.Write(data, Path.GetFileNameWithoutExtension(outputFile), true);
                File.WriteAllBytes(outputFile, netData);
                Console.WriteLine($".NET: {data.Length} 字节 -> {outputFile}");
                break;
        }
    }

    static void WriteBin(byte[] data, string path)
    {
        File.WriteAllBytes(path, data);
        Console.WriteLine($"BIN: {data.Length} 字节 -> {path}");
    }

    static void WriteHex(byte[] data, int baseAddr, string path)
    {
        using var w = new StreamWriter(path);
        // 如果基地址 > 64KB，写入 Extended Linear Address 记录
        int segmentBase = 0;
        if ((baseAddr & 0xFFFF0000) != 0)
        {
            segmentBase = (baseAddr >> 16) & 0xFFFF;
            int ck = 2 + 4 + (byte)(segmentBase >> 8) + (byte)(segmentBase & 0xFF);
            ck = (0x100 - (ck & 0xFF)) & 0xFF;
            w.WriteLine($":02000004{segmentBase:X4}{ck:X2}");
        }
        for (int i = 0; i < data.Length; i += 16)
        {
            int cnt = Math.Min(16, data.Length - i);
            int addr = (baseAddr + i) & 0xFFFF;
            int ck = cnt + (addr >> 8) + (addr & 0xFF);
            var ls = $":{cnt:X2}{addr:X4}00";
            for (int j = 0; j < cnt; j++) { ls += $"{data[i + j]:X2}"; ck += data[i + j]; }
            ck = (0x100 - (ck & 0xFF)) & 0xFF;
            w.WriteLine(ls + $"{ck:X2}");
        }
        w.WriteLine(":00000001FF");
        Console.WriteLine($"HEX: {data.Length} 字节 -> {path}");
    }

    static void WriteElf(byte[] data, int baseAddr, string path)
    {
        var elf = new byte[64 + 56 + data.Length];
        int i = 0;
        var magic = new byte[] { 0x7F, 0x45, 0x4C, 0x46, 2, 1, 1, 0 };
        Array.Copy(magic, 0, elf, i, 8); i += 8;
        i += 8; // padding
        elf[i++] = 2; elf[i++] = 0; // ET_EXEC
        elf[i++] = 0x3E; elf[i++] = 0; // x86-64
        elf[i++] = 1; elf[i++] = 0; elf[i++] = 0; elf[i++] = 0;
        BitConverter.GetBytes((long)baseAddr).CopyTo(elf, i); i += 8;
        BitConverter.GetBytes((long)64).CopyTo(elf, i); i += 8;
        BitConverter.GetBytes((long)0).CopyTo(elf, i); i += 8;
        i += 4; elf[i++] = 64; elf[i++] = 56; elf[i++] = 1; elf[i++] = 0;
        elf[i++] = 0; elf[i++] = 0; elf[i++] = 0; elf[i++] = 0;
        elf[i++] = 1; elf[i++] = 0; elf[i++] = 0; elf[i++] = 0;
        elf[i++] = 7; elf[i++] = 0; elf[i++] = 0; elf[i++] = 0;
        i += 4;
        BitConverter.GetBytes((long)baseAddr).CopyTo(elf, i); i += 8;
        BitConverter.GetBytes((long)baseAddr).CopyTo(elf, i); i += 8;
        BitConverter.GetBytes((long)(64 + 56 + data.Length)).CopyTo(elf, i); i += 8;
        BitConverter.GetBytes((long)(64 + 56 + data.Length)).CopyTo(elf, i); i += 8;
        BitConverter.GetBytes((long)4096).CopyTo(elf, i); i += 8;
        Array.Copy(data, 0, elf, i, data.Length);
        File.WriteAllBytes(path, elf);
        Console.WriteLine($"ELF: {data.Length} 字节 -> {path}");
    }

    static void WriteExe(byte[] data, string path)
    {
        // DOS MZ executable header + data
        var exe = new byte[512 + data.Length];
        exe[0] = 0x4D; exe[1] = 0x5A; // MZ magic
        // Header at offset 0, code at offset 512
        var headerSize = 512 / 16;
        exe[8] = (byte)(headerSize & 0xFF); exe[9] = (byte)((headerSize >> 8) & 0xFF); // header size in paragraphs
        exe[10] = 0; exe[11] = 0; // min extra paragraphs
        exe[12] = 0xFF; exe[13] = 0xFF; // max extra paragraphs
        exe[14] = (byte)((data.Length + 511) / 512 % 512); // initial SS
        exe[16] = 0x10; exe[17] = 0x00; // initial SP
        exe[20] = 0x00; exe[21] = 0x00; // IP
        exe[22] = 0x10; exe[23] = 0x00; // CS
        Array.Copy(data, 0, exe, 512, data.Length);
        File.WriteAllBytes(path, exe);
        Console.WriteLine($"EXE: {data.Length} 字节 -> {path}");
    }

    static void WriteCom(byte[] data, string path)
    {
        // COM: raw binary loaded at CS:0100, max 64KB
        if (data.Length > 65024) { Console.WriteLine("警告: COM 文件超过 64KB，已截断"); Array.Resize(ref data, 65024); }
        File.WriteAllBytes(path, data);
        Console.WriteLine($"COM: {data.Length} 字节 -> {path}");
    }

    static void WriteS19(byte[] data, int baseAddr, string path)
    {
        // Motorola S-Record format (S19)
        using var w = new StreamWriter(path);
        int addr = baseAddr;
        for (int i = 0; i < data.Length; i += 16)
        {
            int cnt = Math.Min(16, data.Length - i);
            int sum = cnt + 1 + (addr >> 8) + (addr & 0xFF);
            var s = $"S1{cnt + 3:X2}{addr:X4}";
            for (int j = 0; j < cnt; j++) { s += $"{data[i + j]:X2}"; sum += data[i + j]; }
            sum = (~sum) & 0xFF;
            w.WriteLine(s + $"{sum:X2}");
            addr += cnt;
        }
        w.WriteLine($"S9030000FC"); // S9 termination
        Console.WriteLine($"S19: {data.Length} 字节 -> {path}");
    }

    static void WriteDump(byte[] data, int baseAddr, string path)
    {
        using var w = new StreamWriter(path);
        w.WriteLine($"VML Dump ({data.Length} bytes) @0x{baseAddr:X8}");
        w.WriteLine(new string('-', 78));
        for (int i = 0; i < data.Length; i += 16)
        {
            w.Write($"{(baseAddr + i):X8}  ");
            for (int j = 0; j < 16; j++) w.Write(i + j < data.Length ? $"{data[i + j]:X2} " : "   ");
            w.Write(" |");
            for (int j = 0; j < 16 && i + j < data.Length; j++)
                w.Write(char.IsControl((char)data[i + j]) ? '.' : (char)data[i + j]);
            w.WriteLine("|");
        }
        w.WriteLine(new string('-', 78));
        Console.WriteLine($"DUMP: -> {path}");
    }

    static void WriteVmb(byte[] data, string path)
    {
        File.WriteAllBytes(path, data);
        Console.WriteLine($"VMB: {data.Length} 字节 -> {path}");
    }

    static void WriteVmApp(byte[] data, string path)
    {
        using var w = new StreamWriter(path);
        w.WriteLine("#!/bin/sh");
        w.WriteLine($"# VML Standalone Program ({data.Length} bytes)");
        w.WriteLine("# Generated by VMLToHex");
        w.WriteLine("SELF=\"$(cd \"$(dirname \"$0\")\" && pwd)\"");
        w.WriteLine("VMLRUN=\"$SELF/vmlrun\"");
        w.WriteLine("[ -f \"$VMLRUN\" ] || VMLRUN=$(which vmlrun 2>/dev/null)");
        w.WriteLine("[ -f \"$VMLRUN\" ] || { echo \"Error: vmlrun not found\" >&2; exit 1; }");
        w.WriteLine("TMPDIR=$(mktemp -d)");
        w.WriteLine("trap 'rm -rf $TMPDIR' EXIT");
        w.WriteLine("VMBFILE=\"$TMPDIR/program.vmb\"");
        w.WriteLine();
        w.WriteLine("# Embedded VMB data (hex, decoded via xxd)");
        w.WriteLine("xxd -r -p << 'VMBEOF' > \"$VMBFILE\"");
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sb.Append($"{data[i]:X2}");
            if ((i + 1) % 32 == 0) { w.WriteLine(sb.ToString()); sb.Clear(); }
        }
        if (sb.Length > 0) w.WriteLine(sb.ToString());
        w.WriteLine("VMBEOF");
        w.WriteLine();
        w.WriteLine("exec \"$VMLRUN\" \"$VMBFILE\" \"$@\"");
        Console.WriteLine($"VMAPP: {data.Length} 字节 -> {path}");
    }
}
