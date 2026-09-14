using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VMLToHex.Assemblers
{
    public abstract class BaseAssembler
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        protected readonly Dictionary<string, byte[]> Opcodes = new(StringComparer.OrdinalIgnoreCase);
        protected readonly Dictionary<string, Func<string[], Dictionary<string, int>, int, byte[]>> CustomHandlers = new(StringComparer.OrdinalIgnoreCase);

        public virtual byte[] Assemble(string asmCode, out int baseAddr, out string architecture)
        {
            baseAddr = 0;
            architecture = Name;
            var output = new List<byte>();
            var labels = new Dictionary<string, int>();
            var lines = asmCode.Split('\n');

            // Pre-process: expand ldr =imm, .rept, and ; multi-stmt ARM syntax
            var expandedLines = new List<string>();
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                // ARM: split ";" multi-statement lines into separate lines
                if (line.Contains(';') && !line.StartsWith(";") && !line.StartsWith("@"))
                {
                    foreach (var seg in line.Split(';'))
                    {
                        var s = seg.Trim();
                        if (!string.IsNullOrEmpty(s) && !s.StartsWith("@"))
                            expandedLines.Add(s);
                    }
                    continue;
                }
                // Expand .rept N ... .endr
                if (line.StartsWith(".rept", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && int.TryParse(parts[1], out var reptCount))
                    {
                        expandedLines.Add($"@ .rept {reptCount} (expanded below)");
                        for (int r = 0; r < reptCount; r++)
                            expandedLines.Add(".word 0");
                    }
                    continue;
                }
                if (line.StartsWith(".endr", StringComparison.OrdinalIgnoreCase))
                    continue; // already handled by .rept
                if (line.StartsWith("ldr ", StringComparison.OrdinalIgnoreCase) && line.Contains("="))
                {
                    var parts = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    string rd = parts[1].Trim().TrimEnd(',');  // r0
                    string valStr = string.Join("", parts.Skip(2)).Trim().TrimStart('=');
                    if (int.TryParse(valStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int imm))
                    {
                        var expanded = ExpandLoadImmediate(rd, imm);
                        Console.Error.WriteLine($"EXPAND: {line} → {expanded.Replace("\n","\\n")}");
                        expandedLines.AddRange(expanded.Split('\n'));
                    }
                    else if (valStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(valStr.Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexImm))
                    {
                        var expanded = ExpandLoadImmediate(rd, hexImm);
                        Console.Error.WriteLine($"EXPAND: {line} → {expanded.Replace("\n","\\n")}");
                        expandedLines.AddRange(expanded.Split('\n'));
                    }
                    else
                    {
                        // Label reference: keep as-is
                        expandedLines.Add(line);
                    }
                }
                else
                {
                    expandedLines.Add(raw);
                }
            }
            lines = expandedLines.ToArray();
            var unresolvedLabels = new List<(int addr, string label, int size)>();

            // Pass 1: collect labels and .org, emit known opcodes
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Strip comments
                int commentIdx = FindCommentStart(line, Name);
                if (commentIdx >= 0) line = line.Substring(0, commentIdx).Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // ARMASM directives (silently ignored or handled)
                if (line.Equals("PRESERVE8", StringComparison.OrdinalIgnoreCase) ||
                    line.Equals("THUMB", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("AREA", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("PROC", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("ENDP", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("ALIGN", StringComparison.OrdinalIgnoreCase))
                    continue;

                // DCD = .word (32-bit) — use same logic as .word
                if (line.StartsWith("DCD", StringComparison.OrdinalIgnoreCase))
                {
                    var valStr = line.Substring(3).Trim();
                    if (string.IsNullOrEmpty(valStr)) continue;
                    EmitWord32(valStr, output, unresolvedLabels);
                    continue;
                }

                // EXPORT / GLOBAL — register label at current position
                if (line.StartsWith("EXPORT", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("GLOBAL", StringComparison.OrdinalIgnoreCase))
                    continue;

                // END — stop assembly
                if (line.StartsWith("END", StringComparison.OrdinalIgnoreCase))
                    break;

                // Pseudo-ops
                if (line.StartsWith(".org", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(' ', '\t');
                    if (parts.Length > 1) baseAddr = ParseNumber(parts[1]);
                    continue;
                }
                // .dc.l / .dc.w / .dc.b (68000-style data directives)
                if (line.StartsWith(".dc.l", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(5).Split(',').Select(p => ParseNumber(p.Trim()));
                    foreach (var val in data)
                        output.AddRange(new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF), (byte)((val >> 16) & 0xFF), (byte)((val >> 24) & 0xFF) });
                    continue;
                }
                if (line.StartsWith(".dc.w", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(5).Split(',').Select(p => ParseNumber(p.Trim()));
                    foreach (var val in data)
                        output.AddRange(new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) });
                    continue;
                }
                if (line.StartsWith(".dc.b", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(5).Split(',').Select(p => (byte)ParseNumber(p.Trim()));
                    output.AddRange(data);
                    continue;
                }
                if (line.StartsWith(".byte", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(5).Split(',').Select(p => (byte)ParseNumber(p.Trim()));
                    output.AddRange(data);
                    continue;
                }
                // .asciiz / .asciz / .ascii string directives
                if (line.StartsWith(".asciiz", StringComparison.OrdinalIgnoreCase) || line.StartsWith(".asciz", StringComparison.OrdinalIgnoreCase))
                {
                    int q1 = line.IndexOf('"');
                    int q2 = line.LastIndexOf('"');
                    if (q1 >= 0 && q2 > q1)
                    {
                        string str = line.Substring(q1 + 1, q2 - q1 - 1);
                        output.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
                        output.Add(0);
                    }
                    continue;
                }
                if (line.StartsWith(".ascii", StringComparison.OrdinalIgnoreCase))
                {
                    int q1 = line.IndexOf('"');
                    int q2 = line.LastIndexOf('"');
                    if (q1 >= 0 && q2 > q1)
                    {
                        string str = line.Substring(q1 + 1, q2 - q1 - 1);
                        output.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
                    }
                    continue;
                }
                if (line.StartsWith(".word", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith(".hword", StringComparison.OrdinalIgnoreCase))
                {
                    bool isHword = line.StartsWith(".hword", StringComparison.OrdinalIgnoreCase);
                    int prefixLen = isHword ? 6 : 5;
                    var lineData = line.Substring(prefixLen);
                    int armComment = lineData.IndexOf('@');
                    if (armComment >= 0) lineData = lineData.Substring(0, armComment);
                    int semiComment = lineData.IndexOf(';');
                    if (semiComment >= 0) lineData = lineData.Substring(0, semiComment);
                    // Handle both ".word 1, 2, 3" (comma-sep) AND ".word 0 .word 0" (space-sep, multiple .word on one line)
                    var rawVals = lineData
                        .Replace(",", " ")
                        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(v => v != ".word" && v != ".hword" && v != "")
                        .ToArray();
                    foreach (var val in rawVals)
                    {
                        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
                        {
                            var bytes = new byte[] { (byte)(num & 0xFF), (byte)((num >> 8) & 0xFF), (byte)((num >> 16) & 0xFF), (byte)((num >> 24) & 0xFF) };
                            output.AddRange(bytes);
                        }
                        else if (uint.TryParse(val.Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                        {
                            var bytes = new byte[] { (byte)(hex & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)((hex >> 16) & 0xFF), (byte)((hex >> 24) & 0xFF) };
                            output.AddRange(bytes);
                        }
                        else
                        {
                            // Label reference — emit 4-byte placeholder and record for Pass 2
                            unresolvedLabels.Add((output.Count, val, 4));
                            output.Add(0);
                            output.Add(0);
                            output.Add(0);
                            output.Add(0);
                        }
                    }
                    continue;
                }

                // Label (standalone or followed by data/instruction: "var_r: .dc.l 0")
                int colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    var label = line.Substring(0, colonIdx).Trim();
                    if (!labels.ContainsKey(label)) labels[label] = output.Count;
                    // Check if there's data after the colon
                    var rest = line.Substring(colonIdx + 1).Trim();
                    if (!string.IsNullOrEmpty(rest))
                    {
                        // Process the rest as a data directive on the same line
                        if (rest.StartsWith(".dc.l", StringComparison.OrdinalIgnoreCase))
                        {
                            var vals = rest.Substring(5).Split(',').Select(p => ParseNumber(p.Trim()));
                            foreach (var val in vals)
                                output.AddRange(new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF), (byte)((val >> 16) & 0xFF), (byte)((val >> 24) & 0xFF) });
                        }
                        else if (rest.StartsWith(".dc.w", StringComparison.OrdinalIgnoreCase))
                        {
                            var vals = rest.Substring(5).Split(',').Select(p => ParseNumber(p.Trim()));
                            foreach (var val in vals)
                                output.AddRange(new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) });
                        }
                        else if (rest.StartsWith(".dc.b", StringComparison.OrdinalIgnoreCase))
                        {
                            var data = rest.Substring(5).Split(',').Select(p => (byte)ParseNumber(p.Trim()));
                            output.AddRange(data);
                        }
                        else if (rest.StartsWith(".word", StringComparison.OrdinalIgnoreCase))
                        {
                            var v = ParseNumber(rest.Substring(5).Trim());
                            output.AddRange(new byte[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF) });
                        }
                        else if (rest.StartsWith(".byte", StringComparison.OrdinalIgnoreCase))
                        {
                            var data = rest.Substring(5).Split(',').Select(p => (byte)ParseNumber(p.Trim()));
                            output.AddRange(data);
                        }
                        else if (rest.StartsWith(".space", StringComparison.OrdinalIgnoreCase) || rest.StartsWith(".ds", StringComparison.OrdinalIgnoreCase))
                        {
                            // BSS — reserve space
                        }
                        else if (rest.StartsWith(".asciiz", StringComparison.OrdinalIgnoreCase) || rest.StartsWith(".asciz", StringComparison.OrdinalIgnoreCase))
                        {
                            // Null-terminated string: extract quoted content
                            int q1 = rest.IndexOf('"');
                            int q2 = rest.LastIndexOf('"');
                            if (q1 >= 0 && q2 > q1)
                            {
                                string str = rest.Substring(q1 + 1, q2 - q1 - 1);
                                output.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
                                output.Add(0); // null terminator
                            }
                        }
                        else if (rest.StartsWith(".ascii", StringComparison.OrdinalIgnoreCase))
                        {
                            int q1 = rest.IndexOf('"');
                            int q2 = rest.LastIndexOf('"');
                            if (q1 >= 0 && q2 > q1)
                            {
                                string str = rest.Substring(q1 + 1, q2 - q1 - 1);
                                output.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
                            }
                        }
                    }
                    continue;
                }
                // ARMASM PROC label: "label PROC"
                if (line.EndsWith("PROC", StringComparison.OrdinalIgnoreCase))
                {
                    var label = line.Substring(0, line.Length - 4).Trim();
                    if (!labels.ContainsKey(label)) labels[label] = output.Count;
                    continue;
                }

                // Data directive
                if (line.StartsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("db ", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(line.IndexOf(' ') + 1).Split(',')
                        .Select(p => { p = p.Trim().Trim('"'); if (p.Length > 1 && p.StartsWith("'")) return (byte)p[1]; return (byte)ParseNumber(p); });
                    output.AddRange(data);
                    continue;
                }
                if (line.StartsWith(".dw", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("dw ", StringComparison.OrdinalIgnoreCase))
                {
                    var data = line.Substring(line.IndexOf(' ') + 1).Split(',')
                        .Select(p => { var v = ParseNumber(p.Trim()); return new byte[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) }; })
                        .SelectMany(b => b);
                    output.AddRange(data);
                    continue;
                }

                // .fill / .space / .ds — treat as BSS (reserve space without emitting zeros)
                if (line.StartsWith(".fill", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".space", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".ds", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(' ', ',');
                    var count = parts.Length > 1 ? ParseNumber(parts[1]) : 0;
                    // Reserve space in BSS: advance address counter without emitting bytes
                    // Output will be padded to align with the address counter
                    continue;
                }

                // End directive
                if (line.StartsWith(".end", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("end", StringComparison.OrdinalIgnoreCase))
                    continue;

                // .set / .equ directive
                if (line.StartsWith(".set", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".equ", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse: .set __stack_top, 0x20010000
                    var afterKeyword = line.Substring(line.IndexOf(' ')).Trim();
                    var eqIdx = afterKeyword.IndexOf(',');
                    if (eqIdx >= 0)
                    {
                        var label = afterKeyword.Substring(0, eqIdx).Trim();
                        var valStr = afterKeyword.Substring(eqIdx + 1).Trim();
                        var val = ParseNumber(valStr);
                        if (!labels.ContainsKey(label)) labels[label] = val;
                    }
                    continue;
                }

                // .weak directive — treat as label declaration
                if (line.StartsWith(".weak", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".global", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(' ', '\t');
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        // .weak handler — define handler label at current position
                        foreach (var part in parts.Skip(1))
                        {
                            var name = part.Trim();
                            if (!string.IsNullOrEmpty(name) && !name.StartsWith("@") && !name.StartsWith(";"))
                            {
                                if (!labels.ContainsKey(name)) labels[name] = output.Count;
                            }
                        }
                    }
                    continue;
                }
            }

            // Pass 1.5: calculate correct label positions (instructions add 2-4 bytes each)
            // Re-scan from start, tracking what the actual output size will be after pass 2
            int realPos = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i].Trim();
                if (string.IsNullOrEmpty(l) || l.StartsWith("@")) continue;
                if (l.EndsWith(":") && !l.StartsWith(".") && l.Length > 1)
                {
                    var lbl = l.TrimEnd(':');
                    if (labels.ContainsKey(lbl)) labels[lbl] = realPos;
                }
                if (l.StartsWith(".word") || l.StartsWith(".byte") || l.StartsWith(".hword") || l.StartsWith("DCD"))
                    { realPos += 4; continue; }
                if (l.StartsWith(".") || l.StartsWith("@") || l.StartsWith(".endr")) continue;
                if (l.StartsWith("bl ", StringComparison.OrdinalIgnoreCase))
                    { realPos += 4; continue; }
                // instruction line (not a pseudo-op, not a label-only line, not data)
                if (!l.EndsWith(":") && !string.IsNullOrEmpty(l))
                    realPos += 2; // Thumb: most instructions are 2 bytes
            }

            // Resolve label references for .word/DCD directives
            bool isArm = (Name == "arm-cm" || Name == "arm");
            foreach (var (addr, label, size) in unresolvedLabels)
            {
                if (labels.TryGetValue(label, out var targetAddr))
                {
                    if (isArm) targetAddr |= 1;  // Cortex-M: handler 需要 Thumb bit (bit0=1)
                    output[addr] = (byte)(targetAddr & 0xFF);
                    output[addr + 1] = (byte)((targetAddr >> 8) & 0xFF);
                    output[addr + 2] = (byte)((targetAddr >> 16) & 0xFF);
                    output[addr + 3] = (byte)((targetAddr >> 24) & 0xFF);
                }
                else
                {
                    Console.Error.WriteLine($"汇编警告: 未解析的标签 '{label}' (地址 0x{addr:X})");
                }
            }

            // Pass 2: emit instructions
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                int commentIdx = FindCommentStart(line, Name);
                if (commentIdx >= 0) line = line.Substring(0, commentIdx).Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Skip pseudo-ops, labels, data directives
                if (line.StartsWith(".") && !IsInstruction(line.Split(' ')[0])) continue;
                if (line.EndsWith(":")) continue;
                if (line.StartsWith("db ", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("dw ", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".dw", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".fill", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".space", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(".ds", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Parse instruction
                var parts = line.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                var mnemonic = parts[0].TrimEnd(':');
                // Normalize size suffix: MOVE.L → MOVE, ADD.B → ADD, etc.
                if (mnemonic.Length > 2 && mnemonic[^2] == '.')
                    mnemonic = mnemonic.Substring(0, mnemonic.Length - 2);
                var rawOps = parts.Length > 1
                    ? string.Join(" ", parts.Skip(1)).Split(',').Select(o => o.Trim()).ToArray()
                    : Array.Empty<string>();

                // Expand parenthesized operands: "0($sp)" → "0", "$sp"
                var operands = new List<string>();
                foreach (var op in rawOps)
                {
                    var parenIdx = op.IndexOf('(');
                    if (parenIdx > 0 && op.EndsWith(")"))
                    {
                        var before = op.Substring(0, parenIdx);
                        var inside = op.Substring(parenIdx + 1, op.Length - parenIdx - 2);
                        operands.Add(before);
                        operands.Add(inside);
                    }
                    else if (op.StartsWith("[") && op.Contains("]"))
                    {
                        // Bracket format: [R12+4] → keep as-is for now
                        operands.Add(op);
                    }
                    else
                    {
                        operands.Add(op);
                    }
                }
                var operandArray = operands.ToArray();

                // Try custom handler first
                if (mnemonic == "LSLS" || mnemonic == "LSRS" || mnemonic == "mov")
                    Console.Error.WriteLine($"HANDLER: mnemonic={mnemonic} ops=[{string.Join(",", operands)}]");
                if (CustomHandlers.TryGetValue(mnemonic, out var handler))
                {
                    try
                    {
                        var bytes = handler(operandArray, labels, output.Count);
                        if (bytes != null) output.AddRange(bytes);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"汇编错误: {mnemonic} {string.Join(",", operandArray)} — {ex.Message}");
                    }
                    continue;
                }

                // Try opcode map
                if (Opcodes.TryGetValue(mnemonic, out var template))
                {
                    try
                    {
                        var bytes = EncodeTemplate(template, operandArray, labels, output.Count);
                        output.AddRange(bytes);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"汇编错误: {mnemonic} {string.Join(",", operandArray)} — {ex.Message}");
                    }
                    continue;
                }

                // Skip unknown but don't crash (may be assembler directives)
            }

            return output.ToArray();
        }

        /// <summary>
        /// Expand ldr rX, =imm into Thumb instruction sequence
        /// Uses MOVS for ≤8-bit values, MOVW+MOVT for ≤16-bit, LDR [PC] with inline literal for larger
        /// </summary>
        protected virtual string ExpandLoadImmediate(string rd, int imm)
        {
            int rt = int.Parse(rd.TrimStart('r', 'R'));
            if (imm >= 0 && imm <= 255)
            {
                return $"movs {rd}, #{imm}";
            }
            // PC-relative LDR with inline literal: ldr rt, [pc, #0]; b #4; .word imm
            // Encoding: ldr=0x4800|rt, b=0xE001, .word=imm(LE)
            int b2 = (imm >> 8) & 0xFF;
            int b3 = (imm >> 16) & 0xFF;
            int b4 = (imm >> 24) & 0xFF;
            return $".byte 0x{(0x48 | rt):X2}, 0x00\n    .byte 0x01, 0xE0\n    .word {imm}";
        }

        protected virtual byte[] EncodeTemplate(byte[] template, string[] operands,
            Dictionary<string, int> labels, int currentAddr)
        {
            var result = new List<byte>(template);
            int opIdx = 0;
            for (int j = 0; j < result.Count; j++)
            {
                if (result[j] == 0xFD && opIdx < operands.Length)
                {
                    var val = ResolveOperand(operands[opIdx++], labels, currentAddr);
                    result[j] = (byte)(val & 0xFF);
                    if (j + 1 < result.Count && result[j + 1] == 0xFE)
                    {
                        result[j + 1] = (byte)((val >> 8) & 0xFF);
                        j++;
                    }
                }
            }
            return result.ToArray();
        }

        protected virtual int ResolveOperand(string operand, Dictionary<string, int>? labels, int currentAddr)
        {
            operand = operand.Trim();
            // Label reference
            if (!string.IsNullOrEmpty(operand) && !char.IsDigit(operand[0]) && operand[0] != '#' && operand[0] != '$' && operand[0] != '%')
            {
                if (labels != null && labels.TryGetValue(operand, out var addr))
                    return addr;
                return 0; // forward reference defaults to 0
            }
            return ParseNumber(operand);
        }

        protected static int EvaluateExpr(string s)
        {
            if (int.TryParse(s, out int result))
                return result;

            int opIndex = -1;
            char op = '+';
            for (int i = 1; i < s.Length; i++)
            {
                if (s[i] == '+' || s[i] == '-')
                {
                    opIndex = i;
                    op = s[i];
                    break;
                }
            }

            if (opIndex > 0)
            {
                string left = s.Substring(0, opIndex);
                string right = s.Substring(opIndex + 1);
                if (int.TryParse(left, out int leftVal) && int.TryParse(right, out int rightVal))
                    return op == '+' ? leftVal + rightVal : leftVal - rightVal;
            }

            return 0;
        }

        protected int ParseNumber(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            s = s.Trim();
            if (s.StartsWith("#")) s = s.Substring(1);
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(s.Substring(2), NumberStyles.HexNumber, null, out var h) ? h : 0;
            if (s.StartsWith("$")) return int.TryParse(s.Substring(1), NumberStyles.HexNumber, null, out var hx) ? hx : 0;
            if (s.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt32(s.Substring(2), 2);
            if (s.StartsWith("%"))
            {
                try { return Convert.ToInt32(s.Substring(1), 2); } catch { return 0; }
            }
            if (int.TryParse(s, out var n)) return n;
            return EvaluateExpr(s);
        }

        /// <summary>Emit a 32-bit word, handling numeric/hex/label references</summary>
        void EmitWord32(string val, List<byte> output, List<(int addr, string label, int size)> unresolvedLabels)
        {
            var trimmed = val.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
            {
                output.AddRange(new byte[] { (byte)(num & 0xFF), (byte)((num >> 8) & 0xFF), (byte)((num >> 16) & 0xFF), (byte)((num >> 24) & 0xFF) });
            }
            else if (uint.TryParse(trimmed.Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            {
                output.AddRange(new byte[] { (byte)(hex & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)((hex >> 16) & 0xFF), (byte)((hex >> 24) & 0xFF) });
            }
            else
            {
                unresolvedLabels.Add((output.Count, trimmed, 4));
                output.Add(0); output.Add(0); output.Add(0); output.Add(0);
            }
        }

        public static int FindCommentStart(string line, string arch)
        {
            // Assembly comments: ; or # or // depending on arch
            if (arch == "arm-cm" || arch == "arm") return line.IndexOf(';');
            var idx = line.IndexOf(';');
            if (idx >= 0) return idx;
            idx = line.IndexOf("//");
            if (idx >= 0) return idx;
            if (arch == "z80" || arch == "8051")
            {
                idx = line.IndexOf(';');
                return idx >= 0 ? idx : -1;
            }
            return -1;
        }

        protected virtual int RegNum(string reg)
        {
            reg = reg.Trim().TrimStart('$', '%', 'r', 'R');
            return int.TryParse(reg, out var n) ? n : 0;
        }

        protected int ResolveValue(string operand, Dictionary<string, int>? labels, int currentAddr)
        {
            return ResolveOperand(operand, labels, currentAddr);
        }

        public static bool IsInstruction(string token)
        {
            return token.StartsWith(".") || token.EndsWith(":") ||
                   token.Equals("org", StringComparison.OrdinalIgnoreCase) ||
                   token.Equals("end", StringComparison.OrdinalIgnoreCase);
        }
    }
}
