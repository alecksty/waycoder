using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace VMLAssembler
{
    /// <summary>
    /// VML 程序
    /// </summary>
    public partial class VmlProgram
    {
        public List<Instruction> Instructions { get; set; } = new List<Instruction>();
        public Dictionary<string, int> Labels { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, object> DataSection { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Constants { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 程序入口标签名（默认 "main"）
        /// </summary>
        public string EntryPoint { get; set; } = "main";

        /// <summary>
        /// 「预处理拼接后的行号 → (原文件, 原行)」的映射表（`Preprocessor.LineMap` 原样带过来）。
        /// 由 `CompilerHelper.CompileWithDiagnostics` 在返回程序前盖上（一处覆盖各门）。
        /// **null = 没有映射**（没预处理过），此时按拼接行号报。
        ///
        /// <para>
        /// 为什么放在程序对象上：**链接器也要用它**（「未定义的函数」是链接期才发现的错，
        /// 而报错要指到用户写的那一行）。链接器手里只有程序对象 —— 编译器那边的
        /// "生效中的表"它够不着（依赖方向 `CompilerBase → VMLAssembler` 是单向的）。
        /// </para>
        /// </summary>
        public List<(string, int)>? SourceLineMap { get; set; }

        /// <summary>
        /// 栈顶地址（字节偏移量，相对于 memory 起始地址，默认 1MB = 1048576）
        /// </summary>
        public int StackTop { get; set; } = 0;  // 0 表示使用默认值（memorySize - 4）
        public int StackBottom { get; set; } = 0; // 栈底地址（0 表示未指定）

        /// <summary>
        /// 中断向量表基址（字节偏移量，默认 0x0000）
        /// </summary>
        public int VectorTable { get; set; } = 0;

        /// <summary>
        /// 是否为库模式（true=保留所有函数, false=应用模式, 移除未使用代码）
        /// </summary>
        public bool IsLibrary { get; set; } = false;

        /// <summary>
        /// .linked  文件列表 — 编译时由 #param lib 自动注入
        /// 序列化时在 .data 前输出 .linked  "path" 指令
        /// 连接时由 Linker 去重处理, 确保每个库只链接一次
        /// </summary>
        public List<string> Includes { get; set; } = new List<string>();
        public List<string> LinkedFiles { get; set; } = new();         // .linked <file>
        public HashSet<string> Externs { get; set; } = new();          // .extern <label>

        /// <summary>
        /// CPU 速度（指令/秒），0 = 无延时（全速）
        /// 由 .SPEED 伪指令设置，或从设备配置加载
        /// </summary>
        public int CpuSpeed { get; set; } = 0;

        /// <summary>
        /// 源码行文本（行号从 1 开始，为 null 时禁用源码注释）
        /// </summary>
        public string[]? SourceLines { get; set; }

        /// <summary>
        /// .skip 保留内存区域列表 (address, size)
        /// 分配内存时跳过这些区域，保护寄存器/字库等固定地址
        /// </summary>
        public List<(int Address, int Size)> SkipRegions { get; set; } = new();

        /// <summary>
        /// 原始汇编指令行 (如 .skip 0x6000, 0x1000)，直接输出到 .data 之前
        /// </summary>
        public List<string> RawDirectives { get; set; } = new();

        /// <summary>
        /// .export 公开函数别名映射 (public_name → internal_name)
        /// 库文件用 .export public internal 声明公开API，
        /// 链接器自动为 public_name 创建标签别名指向 internal_name
        /// </summary>
        public Dictionary<string, string> Exports { get; set; } = new();

        /// <summary>
        /// 应用 .export 映射 — 为每个 public_name 在 Labels 中创建别名指向 internal_name
        /// </summary>
        /// <returns>创建的别名数量</returns>
        public int ApplyExports()
        {
            int count = 0;
            foreach (var exp in Exports)
            {
                string publicName = exp.Key;
                string internalName = exp.Value;
                // 查找内部标签 (优先 lib_ 前缀，其次原始名)
                if (Labels.TryGetValue(internalName, out int addr))
                {
                    if (!Labels.ContainsKey(publicName))
                    {
                        Labels[publicName] = addr;
                        count++;
                    }
                }
                else
                {
                    // 尝试查找 lib_xxx_internalName 格式的标签
                    foreach (var kvp in Labels)
                    {
                        if (kvp.Key.EndsWith("_" + internalName))
                        {
                            if (!Labels.ContainsKey(publicName))
                            {
                                Labels[publicName] = kvp.Value;
                                count++;
                            }
                            break;
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 是否在 VML 输出中包含源码行注释（默认开启，需 SourceLines 非空且指令有 SourceLine）
        /// </summary>
        public bool SourceCommentEnabled { get; set; } = true;

        /// <summary>
        /// 初始化程序
        /// </summary>
        /// <param name="instructions">指令列表</param>
        /// <param name="labels">标签列表</param>
        /// <param name="dataSection">数据段</param>
        /// <param name="constants">常量段</param>
        public VmlProgram(List<Instruction> instructions, Dictionary<string, int> labels, Dictionary<string, object> dataSection, Dictionary<string, object> constants)
        {
            Instructions = instructions;
            Labels = labels;
            DataSection = dataSection;
            Constants = constants;
        }

        /// <summary>
        /// 从字符串加载 VMB 程序
        /// </summary>
        public static VmlProgram Load(string vmbContent)
        {
            var instructions = new List<Instruction>();
            var labels = new Dictionary<string, int>();
            var dataSection = new Dictionary<string, object>();
            var constants = new Dictionary<string, object>();

            var lines = vmbContent.Split('\n');
            bool inDataSection = false;
            bool inTextSection = false;
            int address = 0;
            string lastDataLabel = null;
            List<int> lastDataValues = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed == ".data")
                {
                    // Finalize any pending multi-word data
                    if (lastDataLabel != null && lastDataValues != null)
                        dataSection[lastDataLabel] = lastDataValues.ToArray();
                    inDataSection = true;
                    inTextSection = false;
                    lastDataLabel = null;
                    lastDataValues = null;
                    continue;
                }
                if (trimmed == ".text")
                {
                    // Finalize any pending multi-word data
                    if (lastDataLabel != null && lastDataValues != null)
                        dataSection[lastDataLabel] = lastDataValues.ToArray();
                    inDataSection = false;
                    inTextSection = true;
                    lastDataLabel = null;
                    lastDataValues = null;
                    continue;
                }

                // 处理数据段
                if (inDataSection)
                {
                    // Continuation .word line (no label)
                    if (trimmed.StartsWith(".word") && !trimmed.Contains(":"))
                    {
                        var valPart = trimmed.Replace(".word", "").Trim();
                        if (int.TryParse(valPart, out int val) && lastDataValues != null)
                        {
                            lastDataValues.Add(val);
                        }
                        continue;
                    }

                    if (trimmed.Contains(":"))
                    {
                        // Finalize previous multi-word data
                        if (lastDataLabel != null && lastDataValues != null)
                            dataSection[lastDataLabel] = lastDataValues.ToArray();

                        var parts = trimmed.Split(':');
                        var name = parts[0].Trim();
                        var valueStr = parts[1].Trim();

                        if (string.IsNullOrEmpty(valueStr))
                        {
                            // Label only: start of multi-word data
                            lastDataLabel = name;
                            lastDataValues = new List<int>();
                        }
                        else if (valueStr.Contains(".word"))
                        {
                            lastDataLabel = null;
                            lastDataValues = null;
                            var valPart = valueStr.Replace(".word", "").Trim();
                            if (int.TryParse(valPart, out int val))
                            {
                                dataSection[name] = val;
                            }
                            else
                            {
                                /* `.word <标签名>` —— **取址初始化**（`T *p = &x;`）落成的形态。
                                   解析成 `LabelRef`，序列化时再写回 `.word <名字>`，
                                   由链接器把那个名字解析成**地址**。
                                   ⚠ 此前这一支是**空的**：`.word sc_win` 解析不出整数就
                                   什么都不做 ⇒ `dataSection` 里没这一项 ⇒ 该槽位**丢失**
                                   （实测 `stdscr` 恒为 NULL，见 `LabelRef` 的注释）。 */
                                dataSection[name] = new LabelRef(valPart);
                            }
                        }
                        else if (valueStr.Contains(".string"))
                        {
                            lastDataLabel = null;
                            lastDataValues = null;
                            var str = valueStr.Replace(".string", "").Trim().Trim('"');
                            dataSection[name] = str;
                        }
                        continue;
                    }
                }

                // 处理代码段
                if (inTextSection)
                {
                    // 处理标签
                    if (trimmed.EndsWith(":"))
                    {
                        var labelName = trimmed.TrimEnd(':');
                        labels[labelName] = address;
                        continue;
                    }

                    // 解析指令
                    var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

                    var opcodeName = parts[0].ToUpper();
                    if (!Enum.TryParse<OpCode>(opcodeName, out var opcode)) continue;

                    var operands = new List<Operand>();
                    if (parts.Length > 1)
                    {
                        // 使用空格分隔操作数
                        var operandStrs = parts[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var opStr in operandStrs)
                        {
                            if (!string.IsNullOrWhiteSpace(opStr))
                                operands.Add(ParseOperand(opStr.Trim()));
                        }
                    }

                    instructions.Add(new Instruction(opcode, operands, address));
                    address++;
                }
            }

            return new VmlProgram(instructions, labels, dataSection, constants);
        }

        private static Operand ParseOperand(string opStr)
        {
            opStr = opStr.Trim();

            // 立即数
            if (opStr.StartsWith("#"))
            {
                var numStr = opStr.Substring(1);
                if (numStr.StartsWith("0x"))
                {
                    return new Operand(OperandType.IMMEDIATE, Convert.ToInt32(numStr.Substring(2), 16));
                }
                if (double.TryParse(numStr, out var d)) return new Operand(OperandType.IMMEDIATE, d);
                if (int.TryParse(numStr, out var iv)) return new Operand(OperandType.IMMEDIATE, iv);
                return new Operand(OperandType.IMMEDIATE, numStr);
            }

            // 寄存器
            if (opStr.StartsWith("R") || opStr.StartsWith("r"))
            {
                return new Operand(OperandType.REGISTER, int.Parse(opStr.Substring(1)));
            }

            // 内存寻址 [xxx]
            if (opStr.StartsWith("[") && opStr.EndsWith("]"))
            {
                var inner = opStr.Trim('[', ']');
                if (inner.StartsWith("R") || inner.StartsWith("r"))
                {
                    return new Operand(OperandType.MEMORY, "R" + inner.Substring(1));
                }
                if (inner.StartsWith("0x"))
                {
                    return new Operand(OperandType.MEMORY, Convert.ToInt32(inner.Substring(2), 16));
                }
                return new Operand(OperandType.MEMORY, int.Parse(inner));
            }

            // 标签
            return new Operand(OperandType.LABEL, opStr);
        }

        public override string ToString()
        {
            // 应用模式：执行死代码消除
            RemoveUnusedFunctions();

            var sb = new StringBuilder();

            if (!IsLibrary)
            {
                // 应用模式：输出入口配置
                if (!IsLibrary)
                {
                    sb.Append($".entry {EntryPoint}\n");
                    sb.Append($".stack {StackTop}\n");
                    sb.Append($".vectors 0x{VectorTable:X}\n");
                }
                sb.Append("\n");
            }

            // 输出原始指令（如 .skip）
            foreach (var directive in RawDirectives)
            {
                sb.Append(directive);
                sb.Append('\n');
            }

            // 输出 .linked 指令 (#param lib / .linked 自动注入)
            if (LinkedFiles.Count > 0)
            {
                foreach (var f in LinkedFiles)
                    sb.AppendLine($".linked \"{f}\"");
            }
            if (Includes.Count > 0)
            {
                foreach (var inc in Includes)
                    sb.AppendLine($".linked  \"{inc}\"");
                sb.AppendLine();
            }

            // 输出 .skip 保留区域
            foreach (var (addr, size) in SkipRegions)
            {
                sb.Append($".skip 0x{addr:X}, 0x{size:X}\n");
            }

            // 输出 .export 公开API映射
            if (Exports.Count > 0)
            {
                sb.AppendLine();
                foreach (var exp in Exports)
                    sb.AppendLine($".export {exp.Key} {exp.Value}");
            }

            // 数据段
            sb.Append(".data\n");
            foreach (var data in DataSection)
            {
                if (data.Key == null)
                {
                    throw new ArgumentNullException("DataSection key is null");
                }
                if (data.Value is int[] arr)
                {
                    // **等值数组压成一行**（v0.96.177）：`int a[1024]` 是 1024 个 0，
                    // 逐元素写出来就是 1024 行 —— 而数据在内存里本来就是压缩的
                    // （编译器放的就是 `int[N]`），只有"落成文本"这一步把它摊平了。
                    // 非等值的仍逐元素写（那才是真的每个都不一样）。
                    if (TryUniform(arr, out var only))
                        sb.Append($"{data.Key}: .word[{arr.Length}] {only}\n");
                    else
                    {
                        sb.Append($"{data.Key}:\n");
                        foreach (var elem in arr)
                            sb.Append($"    .word {elem}\n");
                    }
                }
                else if (data.Value is object[] objArr)
                {
                    if (TryUniform(objArr, out var onlyObj))
                        sb.Append($"{data.Key}: .word[{objArr.Length}] {onlyObj}\n");
                    else
                    {
                        sb.Append($"{data.Key}:\n");
                        foreach (var elem in objArr)
                            sb.Append($"    .word {elem}\n");
                    }
                }
                else if (data.Value is DataString ds)
                {
                    string escaped = ds.Value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                    string directive = ds.Width switch
                    {
                        StringWidth.Wide => ".wstring",
                        StringWidth.Unicode => ".ustring",
                        _ => ".string"
                    };
                    sb.Append($"{data.Key}: {directive} \"{escaped}\"\n");
                }
                else if (data.Value is string str)
                {
                    string escaped = str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                    sb.Append($"{data.Key}: .string \"{escaped}\"\n");
                }
                else if (data.Value is LabelRef labelRef)
                {
                    /* **标签引用** ⇒ `.word <标签名>`（汇编器把它解析成那个标签的地址）。
                       ⚠ 不能省成 `string` 那条分支 —— 那会写成 `.string "sc_win"`
                       （一段**内容**等于标签名的字符数据），不是地址。见 `LabelRef` 的注释。 */
                    sb.Append($"{data.Key}: .word {labelRef.Name}\n");
                }
                else if (data.Key != null && data.Key.StartsWith("flt_"))
                {
                    // 浮点常量需要以 IEEE 754 位模式存储 (v1.66.38 fix)
                    int floatBits = data.Value is float f ? BitConverter.SingleToInt32Bits(f)
                        : data.Value is int i ? i
                        : Convert.ToInt32(data.Value);
                    sb.Append($"{data.Key}: .word {floatBits}\n");
                }
                else if (data.Key != null && data.Key.StartsWith("dbl_"))
                {
                    long doubleBits = data.Value is double d ? BitConverter.DoubleToInt64Bits(d)
                        : data.Value is long l ? l
                        : Convert.ToInt64(data.Value);
                    sb.Append($"{data.Key}: .dword {doubleBits}\n");
                }
                else if (data.Value is double || data.Value is long)
                    sb.Append($"{data.Key}: .dword {data.Value}\n");
                else
                    sb.Append($"{data.Key}: .word {data.Value}\n");
            }
            sb.Append("\n");

            // 常量段
            if (Constants.Count > 0)
            {
                sb.Append(".const\n");
                foreach (var kvp in Constants)
                {
                    if (kvp.Value is double d)
                        sb.Append($"{kvp.Key}: .const {d}\n");
                    else
                        sb.Append($"{kvp.Key}: .const {kvp.Value}\n");
                }
                sb.Append("\n");
            }

            // 代码段
            sb.Append(".text\n");

            // 创建标签到地址的映射，用于在生成代码时插入标签
            Dictionary<int, List<string>> addressToLabels = new Dictionary<int, List<string>>();
            foreach (var label in Labels)
            {
                // 跳过与 dataSection 重复的标签（避免覆盖数据段标签地址）
                if (DataSection.ContainsKey(label.Key))
                    continue;
                if (!addressToLabels.ContainsKey(label.Value))
                {
                    addressToLabels[label.Value] = new List<string>();
                }
                addressToLabels[label.Value].Add(label.Key);
            }

            // 也从 LABEL 指令中提取标签
            for (int i = 0; i < Instructions.Count; i++)
            {
                var instruction = Instructions[i];
                if (instruction.Opcode == OpCode.LABEL && instruction.Operands.Count > 0 &&
                    instruction.Operands[0].Type == OperandType.LABEL)
                {
                    string labelName = instruction.Operands[0].Value?.ToString() ?? "";
                    if (!addressToLabels.ContainsKey(i))
                    {
                        addressToLabels[i] = new List<string>();
                    }
                    // 检查是否已存在（避免重复）
                    if (!addressToLabels[i].Contains(labelName))
                    {
                        addressToLabels[i].Add(labelName);
                    }
                }
            }

            // 生成代码，在适当的位置插入标签
            int lastSourceLine = -1;
            for (int i = 0; i < Instructions.Count; i++)
            {
                var instruction = Instructions[i];

                // 检查当前地址是否有标签（在跳过 LABEL 指令之前输出）
                if (addressToLabels.ContainsKey(i))
                {
                    foreach (var label in addressToLabels[i])
                    {
                        sb.Append($"{label}:\n");
                    }
                }

                // 跳过纯标签指令（不输出代码体，标签已在上方输出）
                if (instruction.Opcode == OpCode.LABEL)
                    continue;

                // 输出源码行注释（当源码行号变化时）
                //
                // ⚠ 判据是 `>= 1` **不是 `>= 0`** —— 行号是 1-based，下面那句是
                //   `SourceLines[sl - 1]`；`sl == 0` 时右边界 `sl - 1 < Length` 恒真
                //   （`-1 < n`），于是直接 `SourceLines[-1]` 抛 IndexOutOfRange。
                //   `Instruction.SourceLine` 的默认值恰好是 **-1**（不输出），所以今天
                //   走不到 0；但 `InstrList.Add` 的闸门是 `>= 0`，任何一门语言只要在
                //   某条语句上把 `CurrentSourceLine` 置成 0，整份产物就炸在这儿。
                if (SourceCommentEnabled && SourceLines != null && instruction.SourceLine >= 1)
                {
                    int sl = instruction.SourceLine;
                    if (sl != lastSourceLine && sl - 1 < SourceLines.Length)
                    {
                        lastSourceLine = sl;
                        string srcLine = SourceLines[sl - 1];
                        if (!string.IsNullOrWhiteSpace(srcLine))
                        {
                            sb.Append($"; {sl}: {srcLine}\n");
                        }
                    }
                }

                // 生成指令
                // 检查是否是注释（LABEL 指令的 Label 属性以 ; 开头）
                if (instruction.Opcode == OpCode.NOP && !string.IsNullOrEmpty(instruction.Label) && instruction.Label.StartsWith(";"))
                {
                    // 输出注释，左对齐
                    sb.Append($"{instruction.Label}\n");
                }
                else
                {
                    var instructionStr = instruction.ToString();
                    // 所有指令左边留 8 个空格
                    sb.Append($"        {instructionStr}\n");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将VML程序转换为包含.include伪指令的文本
        /// </summary>
        /// <param name="includeFiles">要包含的文件列表</param>
        /// <param name="basePath">基础路径（用于相对路径解析）</param>
        /// <returns>包含.include伪指令的VML文本</returns>
        public string ToVmlTextWithIncludes(List<string>? includeFiles = null, string? basePath = null)
        {
            return IncludeProcessor.ToVmlTextWithIncludes(this, includeFiles, basePath);
        }

        /// <summary>
        /// 将VML程序序列化为VMB二进制格式
        /// </summary>
        /// <returns>VMB二进制数据</returns>
        // instructionIndex → VMB字节偏移 (仅代码标签, 数据标签保持0)
        private Dictionary<int, uint>? _vmbOffsets;

        public byte[] ToVmbBytes()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                WriteVmbHeader(writer);

                long codeOff = stream.Position;
                WriteCodeSection(writer);
                long codeSz = stream.Position - codeOff;

                long dataOff = stream.Position;
                WriteDataSection(writer);
                long dataSz = stream.Position - dataOff;

                long constOff = stream.Position;
                WriteConstSection(writer);
                long constSz = stream.Position - constOff;

                long symOff = stream.Position;
                WriteSymbolsSection(writer);
                long symSz = stream.Position - symOff;

                long relocOff = stream.Position;
                WriteRelocationSection(writer);

                long fileSize = stream.Length + 4;

                stream.Seek(12, SeekOrigin.Begin);
                writer.Write((uint)fileSize);

                stream.Seek(16, SeekOrigin.Begin);
                writer.Write((uint)codeOff); writer.Write((uint)codeSz);
                writer.Write((uint)dataOff); writer.Write((uint)dataSz);
                writer.Write((uint)constOff); writer.Write((uint)constSz);
                writer.Write((uint)symOff); writer.Write((uint)symSz);

                var checksum = ComputeChecksum(stream);
                stream.Seek(0, SeekOrigin.End);
                stream.Write(BitConverter.GetBytes(checksum), 0, 4);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// 从VMB二进制数据反序列化VML程序
        /// </summary>
        /// <param name="vmbData">VMB二进制数据</param>
        /// <returns>VML程序对象</returns>
        public static VmlProgram FromVmbBytes(byte[] vmbData)
        {
            if (vmbData.Length >= 4)
            {
                uint storedChecksum = BitConverter.ToUInt32(vmbData, vmbData.Length - 4);
                uint computed = 0;
                for (int i = 0; i < vmbData.Length - 4; i++)
                    computed = (computed + vmbData[i]) & 0xFFFFFFFF;
                if (storedChecksum != computed)
                    throw new InvalidDataException("VMB checksum mismatch");
            }

            using (var stream = new MemoryStream(vmbData))
            using (var reader = new BinaryReader(stream))
            {
                var header = ReadVmbHeader(reader);
                ValidateHeader(header, vmbData.Length);

                var instructions = ReadCodeSection(reader, header);
                var dataSection = ReadDataSection(reader, header);
                var constants = ReadConstSection(reader, header);
                var labels = ReadSymbolsSection(reader, header);
                ApplyRelocations(reader, header, instructions, labels);

                var prog = new VmlProgram(instructions, labels, dataSection, constants)
                {
                    // 优先匹配 "main"，避免数据标签(addr=0)被误认为入口点
                    EntryPoint = labels.ContainsKey("main") ? "main" :
                        (labels.Count > 0 ? labels.FirstOrDefault(kv => kv.Value == header.EntryPoint).Key ?? "" : ""),
                    StackTop = (int)header.StackTop,
                    VectorTable = (int)header.VectorTable,
                    CpuSpeed = header.CpuSpeed
                };

                return prog;
            }
        }

        /// <summary>
        /// 将VMB二进制数据保存到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void SaveToVmbFile(string filePath)
        {
            var vmbData = ToVmbBytes();
            File.WriteAllBytes(filePath, vmbData);
        }

        /// <summary>
        /// 从VMB文件加载VML程序
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>VML程序对象</returns>
        public static VmlProgram LoadFromVmbFile(string filePath)
        {
            var vmbData = File.ReadAllBytes(filePath);
            return FromVmbBytes(vmbData);
        }

        /// <summary>
        /// 移除未使用的函数（死代码消除）
        /// 仅保留从入口点可达的 CALL 目标函数，移除未使用的代码和数据
        /// </summary>
        public void RemoveUnusedFunctions()
        {
            if (IsLibrary) return;

            // 步骤4：移除未引用的数据段（始终执行，不依赖 label 追踪）
            RemoveUnusedData();

            // 步骤1-3（函数级死代码消除）：暂时禁用，等待正确性验证
            // 函数调用链路分析尚不完善，误删代码会导致无限循环
            return;

#if false  // 步骤1-3（函数级死代码消除）：暂时禁用，等待正确性验证
            var jumpOps = new HashSet<OpCode> { OpCode.CALL, OpCode.JMP, OpCode.JZ, OpCode.JNZ,
                OpCode.JE, OpCode.JNE, OpCode.JG, OpCode.JL, OpCode.JGE, OpCode.JLE };

            // 辅助方法：获取指令中的标签操作数（跳转指令: operand[0]=LABEL 或 operand[1]=LABEL）
            string GetJumpLabel(Instruction instr)
            {
                if (instr.Operands.Count == 0) return null;
                // CALL/JMP: 第一个操作数是标签
                if (instr.Operands[0].Type == OperandType.LABEL)
                    return instr.Operands[0].Value?.ToString() ?? "";
                // Jcc Rd, label: 第二个操作数是标签
                if (instr.Operands.Count > 1 && instr.Operands[1].Type == OperandType.LABEL)
                    return instr.Operands[1].Value?.ToString() ?? "";
                return null;
            }

            // 收集所有被 LOAD 指令引用的标签（如 ON ERROR GOTO handler），
            // LOAD Rd, label — label 是第二个操作数
            var loadReferencedLabels = new HashSet<string>();
            foreach (var instr in Instructions)
            {
                if (instr.Opcode == OpCode.MOVE && instr.Operands.Count > 1
                    && instr.Operands[1].Type == OperandType.LABEL)
                {
                    string t = instr.Operands[1].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(t) && Labels.ContainsKey(t))
                        loadReferencedLabels.Add(t);
                }
            }

            // 步骤2：从入口点追溯可达函数
            var reachable = new HashSet<string> { EntryPoint };
            foreach (var lbl in loadReferencedLabels)
                reachable.Add(lbl);
            bool changed = true;
            while (changed)
            {
                changed = false;
                var sortedReachable = reachable
                    .Where(l => Labels.ContainsKey(l))
                    .Select(l => new { Label = l, Pos = Labels[l] })
                    .OrderBy(x => x.Pos)
                    .ToList();

                foreach (var rl in reachable.ToList())
                {
                    if (!Labels.TryGetValue(rl, out int rStart)) continue;
                    int rEnd = Instructions.Count;
                    int idx = sortedReachable.FindIndex(r => r.Label == rl);
                    if (idx >= 0 && idx + 1 < sortedReachable.Count)
                        rEnd = sortedReachable[idx + 1].Pos;

                    for (int i = rStart; i < rEnd && i < Instructions.Count; i++)
                    {
                        var instr = Instructions[i];
                        if (!jumpOps.Contains(instr.Opcode)) continue;
                        string t = GetJumpLabel(instr);
                        if (!string.IsNullOrEmpty(t) && Labels.ContainsKey(t)
                            && !reachable.Contains(t))
                        { reachable.Add(t); changed = true; }
                    }
                }
            }

            // 步骤3：重建 — 对每个可达函数，保留从入口到最近 RET 的指令
            var reachablePositions = reachable
                .Where(l => Labels.ContainsKey(l))
                .Select(l => new { Label = l, Pos = Labels[l] })
                .OrderBy(x => x.Pos)
                .ToList();

            var keep = new bool[Instructions.Count];
            for (int ri = 0; ri < reachablePositions.Count; ri++)
            {
                int start = reachablePositions[ri].Pos;
                int end = Instructions.Count;
                for (int i = start; i < Instructions.Count; i++)
                {
                    if (Instructions[i].Opcode == OpCode.RET)
                    {
                        end = i + 1;
                        break;
                    }
                    if (ri + 1 < reachablePositions.Count && i >= reachablePositions[ri + 1].Pos)
                        break;
                }
                if (end >= Instructions.Count && ri + 1 < reachablePositions.Count)
                    end = reachablePositions[ri + 1].Pos;
                for (int i = start; i < end && i < keep.Length; i++)
                    keep[i] = true;
            }

            var newInstrs = new List<Instruction>();
            var newLabels = new Dictionary<string, int>();
            int newOff = 0;
            for (int i = 0; i < Instructions.Count; i++)
            {
                if (!keep[i]) continue;
                foreach (var kv in Labels)
                    if (kv.Value == i && !newLabels.ContainsKey(kv.Key))
                        newLabels[kv.Key] = newOff;
                newInstrs.Add(Instructions[i]);
                newOff++;
            }

            Instructions.Clear();
            Instructions.AddRange(newInstrs);
            Labels.Clear();
            foreach (var kv in newLabels)
                Labels[kv.Key] = kv.Value;
#endif
        }

        /// <summary>
        /// 步骤4：移除未被任何指令引用的数据段条目
        /// </summary>
        public void RemoveUnusedData()
        {
            var usedData = new HashSet<string>();
            foreach (var instr in Instructions)
                foreach (var op in instr.Operands)
                {
                    if (op.Type == OperandType.LABEL && op.Value != null)
                        usedData.Add(op.Value.ToString() ?? "");
                    if (op.Type == OperandType.MEMORY && op.Value is string s)
                    {
                        usedData.Add(s);
                        // Extract register from offset patterns like "R10+0" → "R10"
                        var plusIdx = s.IndexOf('+');
                        var minusIdx = s.IndexOf('-');
                        if (plusIdx > 0) usedData.Add(s.Substring(0, plusIdx));
                        if (minusIdx > 0) usedData.Add(s.Substring(0, minusIdx));
                    }
                }
            // ⚠ **数据段内部的引用也要算可达**（传递闭包）。
            //
            // 指针表 —— `char *rows[] = {"abc","def"}`、nyancat 的帧数组、`argv` 那类 ——
            // 里面的字符串**只被别的数据项引用**（序列化成 `.word L_x`），
            // 在**指令**里一次都不出现。只按指令算可达的话，它们会被整条删掉，
            // 而 `rows[0]` 读到的是一个不存在的标签 ⇒ `(null)`。
            // 实测症状：`char *rows[]={"abc","def"}` 连**顶层全局**都取不到。
            //
            // 必须走到不动点：表里的标签本身又可能是另一张表（多级指针表）。
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var kv in DataSection)
                {
                    if (!usedData.Contains(kv.Key)) continue;
                    foreach (var name in DataRefs(kv.Value))
                        if (usedData.Add(name)) grew = true;
                }
            }

            var newData = new Dictionary<string, object>();
            foreach (var kv in DataSection)
                if (usedData.Contains(kv.Key))
                    newData[kv.Key] = kv.Value;
            if (newData.Count > 0)
            {
                DataSection.Clear();
                foreach (var kv in newData)
                    DataSection[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// 一个数据项**引用了哪些标签**。
        ///
        /// ⚠ 只有 `object[]`（数组 / 指针表）里的字符串是**标签名**；
        /// 纯 `string` 与 `DataString` 是**内容**（`.string "abc"` 的 `abc` 是正文，
        /// 不是标签）—— 把内容当标签扫，会把恰好同名的数据项"救活"，那是最难查的一类。
        /// </summary>
        static IEnumerable<string> DataRefs(object value)
        {
            if (value is not object[] arr) yield break;
            foreach (var e in arr)
                if (e is string s && s.Length > 0) yield return s;
        }

        #region 私有辅助方法

        private void WriteVmbHeader(BinaryWriter writer)
        {
            writer.Write(new byte[] { 0x56, 0x4D, 0x42, 0x00 });
            writer.Write((ushort)2);
            writer.Write((ushort)1);   // minor version bump: 0→1 for CpuSpeed
            writer.Write((uint)68);    // header size increased from 64
            writer.Write((uint)0);     // FileSize placeholder
            for (int i = 0; i < 8; i++)
                writer.Write((uint)0); // section layout placeholder
            // 入口点: 写指令索引 (C#兼容)
            writer.Write((uint)(Labels.TryGetValue(EntryPoint, out int epIdx) ? epIdx : 0));
            writer.Write((uint)StackTop);
            writer.Write((uint)VectorTable);
            writer.Write((uint)0x01);  // Flags
            writer.Write((uint)CpuSpeed);
        }

        private static bool IsPseudoOp(OpCode op) => op switch
        {
            OpCode.LABEL => true,
            OpCode.BREAK => true,
            OpCode.DUMP  => true,
            OpCode.TRACE => true,
            OpCode.ASM   => true,
            OpCode.CHIPASM => true,
            _ => false
        };

        private void WriteCodeSection(BinaryWriter writer)
        {
            var realInstrs = Instructions.Where(i => !IsPseudoOp(i.Opcode)).ToList();
            writer.Write((uint)realInstrs.Count);

            // 计算 VMB 字节偏移到 _vmbOffsets (键=完整Instructions索引)
            _vmbOffsets = new Dictionary<int, uint>();
            int ri = 0; // realInstrs 索引
            for (int fi = 0; fi < Instructions.Count; fi++)
            {
                if (IsPseudoOp(Instructions[fi].Opcode))
                {
                    // 伪指令: 偏移 = 当前或下一个真实指令的位置
                    _vmbOffsets[fi] = _vmbOffsets.Count > 0 ? _vmbOffsets[fi-1] : 4;
                }
                else
                {
                    _vmbOffsets[fi] = (uint)(writer.BaseStream.Position - 0x44); // VMB代码字节偏移 (减去64字节头)
                    ri++;
                    writer.Write((byte)Instructions[fi].Opcode);
                    writer.Write((byte)Instructions[fi].Operands.Count);
                    foreach (var operand in Instructions[fi].Operands)
                        WriteOperand(writer, operand);
                }
            }
        }

        private void WriteOperand(BinaryWriter writer, Operand operand)
        {
            switch (operand.Type)
            {
                case OperandType.REGISTER:
                    writer.Write((byte)0x01);
                    writer.Write((byte)(int)operand.Value);
                    break;

                case OperandType.IMMEDIATE:
                    writer.Write((byte)0x02);
                    if (operand.Value is string immStr)
                    {
                        if (!int.TryParse(immStr, out int immVal))
                            immVal = 0;
                        writer.Write((byte)0x04);
                        writer.Write(immVal);
                        break;
                    }
                    var immValue = Convert.ToInt32(operand.Value);
                    if (immValue >= -128 && immValue <= 127)
                    {
                        writer.Write((byte)0x01);
                        writer.Write((sbyte)immValue);
                    }
                    else if (immValue >= -32768 && immValue <= 32767)
                    {
                        writer.Write((byte)0x02);
                        writer.Write((short)immValue);
                    }
                    else
                    {
                        writer.Write((byte)0x04);
                        writer.Write(immValue);
                    }
                    break;

                case OperandType.MEMORY:
                    writer.Write((byte)0x03);
                    if (operand.Value is string regRelStr)
                    {
                        int plus = regRelStr.IndexOf('+');
                        int minus = regRelStr.IndexOf('-');
                        if (plus > 0)
                        {
                            // 寄存器相对寻址: R12+8
                            writer.Write((byte)0x02);
                            var left = regRelStr.AsSpan(0, plus);
                            var right = regRelStr.AsSpan(plus + 1);
                            int regNum = int.Parse(left[1..]);
                            int offset = int.Parse(right);
                            writer.Write((byte)regNum);
                            writer.Write(offset >= 0 ? (sbyte)0 : (sbyte)1);
                            writer.Write(offset < 0 ? -offset : offset);
                        }
                        else if (minus > 0)
                        {
                            // 寄存器相对寻址: R12-4
                            writer.Write((byte)0x02);
                            var left = regRelStr.AsSpan(0, minus);
                            var right = regRelStr.AsSpan(minus + 1);
                            int regNum = int.Parse(left[1..]);
                            int offset = int.Parse(right);
                            writer.Write((byte)regNum);
                            writer.Write((sbyte)1); // sign = negative
                            writer.Write(offset);
                        }
                        else if (regRelStr.StartsWith("R", StringComparison.OrdinalIgnoreCase) &&
                                 int.TryParse(regRelStr.AsSpan(1), out int bareReg))
                        {
                            // 单独寄存器引用: R0
                            writer.Write((byte)0x02);
                            writer.Write((byte)bareReg);
                            writer.Write((sbyte)0);
                            writer.Write(0);
                        }
                        else if (char.IsLetter(regRelStr[0]) || regRelStr[0] == '_')
                        {
                            // 标签引用: [label] 或 [_label]
                            writer.Write((byte)0x03);
                            var lblBytes = Encoding.UTF8.GetBytes(regRelStr);
                            writer.Write((ushort)lblBytes.Length);
                            writer.Write(lblBytes);
                        }
                        else
                        {
                            writer.Write((byte)0x01);
                            writer.Write(Convert.ToInt32(operand.Value));
                        }
                    }
                    else
                    {
                        writer.Write((byte)0x01);
                        writer.Write(Convert.ToInt32(operand.Value));
                    }
                    break;

                case OperandType.LABEL:
                    writer.Write((byte)0x04);
                    var labelStr = operand.Value?.ToString() ?? "";
                    var labelBytes = Encoding.UTF8.GetBytes(labelStr);
                    writer.Write((ushort)labelBytes.Length);
                    writer.Write(labelBytes);
                    break;

                case OperandType.INDIRECT:
                    // **自己的 tag 0x05**。
                    //
                    // 原来借用 MEMORY(0x03) + 一个 bit31 置位的 uint，读取侧靠"peek 后面 4 字节的
                    // 最高位"来区分它和"寄存器相对寻址"（子模式 0x02）—— 那个启发式**必然误判**：
                    // 子模式 0x02 的第 4 个字节正是**偏移量的最低字节**，于是 `[R12+128]` 这种
                    // 偏移低字节 ≥ 0x80 的地址会被当成间接寻址读掉 4 字节（实际是 6 字节），
                    // 后面的字节流整条错位，最后报 `Unknown operand type tag: 0x00`。
                    // 实测：偏移 8 / 127 往返一致，**128 / 200 / 384 全部错**。
                    // 一个 type tag 换掉一次"猜"，是这个格式里最划算的一处修改。
                    writer.Write((byte)0x05);
                    if (operand.Value is int indRegNum)
                    {
                        // @0 → [R0+0], @1 → [R1+0] 等
                        writer.Write((uint)(0x80000000 | (indRegNum << 24)));
                    }
                    else if (operand.Value is string indStr)
                    {
                        if (indStr.StartsWith("[R", StringComparison.OrdinalIgnoreCase))
                        {
                            // [R14-8] 格式: 寄存器相对寻址
                            int rp = indStr.IndexOf('+');
                            int rm = indStr.IndexOf('-');
                            int sep = rp > 0 ? rp : rm;
                            if (sep > 0)
                            {
                                var regPart = indStr[2..sep];
                                var offPart = indStr[(sep + 1)..];
                                if (offPart.EndsWith("]"))
                                    offPart = offPart[..^1];
                                int rN = int.TryParse(regPart, out var rv) ? rv : 0;
                                int off = int.TryParse(offPart, out var ov) ? ov : 0;
                                if (rm > 0) off = -off;
                                writer.Write((uint)(0x80000000u | ((uint)rN << 24) | ((uint)off & 0x00FFFFFF)));
                            }
                            else
                            {
                                // [R14] 格式
                                var regOnly = indStr[2..];
                                if (regOnly.EndsWith("]"))
                                    regOnly = regOnly[..^1];
                                int rN = int.TryParse(regOnly, out var rv2) ? rv2 : 0;
                                writer.Write((uint)(0x80000000u | ((uint)rN << 24)));
                            }
                        }
                        else if (indStr.StartsWith("R", StringComparison.OrdinalIgnoreCase) &&
                                 int.TryParse(indStr.AsSpan(1), out int rN))
                        {
                            // "R0" 格式
                            writer.Write((uint)(0x80000000u | ((uint)rN << 24)));
                        }
                        else
                        {
                            // 标签或常量地址
                            writer.Write((uint)0x80000000);
                        }
                    }
                    else
                    {
                        writer.Write((uint)0x80000000);
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unsupported OperandType: {operand.Type}");
            }
        }

        /// <summary>
        /// 数组元素是否**全部相同、且都是 4 字节整数** —— 只有这样才能压成 `.word[N] v` 一行。
        ///
        /// ⚠ **必须连类型一起判**（v0.96.177 修正）：`.word[N]` 的默认值只收整数
        /// （汇编器那侧就是 `int`）。只判"值相同"会把**等值的字符串/浮点数组**也压成一行 ——
        /// 那行回来的东西就不是数组了：`object[]{"ok","ok","ok"}` 往返之后只剩一个字符串，
        /// 而且**只有全等才会中招**（有一个不同就退回逐元素写），所以拿 `int` 数组去测永远测不出来。
        /// 前端确实会产出等值的字符串数组（`GenerateStaticArrayInit` 与全局初始化器都会），
        /// 这不是假想。
        ///
        /// 少于 2 个元素返回 false：压了反而更啰嗦，也不值得为它多担一条路径。
        /// </summary>
        private static bool TryUniform(System.Collections.IList list, out object? only)
        {
            only = null;
            if (list.Count < 2) return false;
            if (list[0] is not int) return false;       // 只压 4 字节字（与 `.word[N]` 的语义对齐）
            only = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                if (list[i] is not int) return false;
                if (!Equals(list[i], only)) return false;
            }
            return true;
        }

        private void WriteDataSection(BinaryWriter writer)
        {
            writer.Write((uint)DataSection.Count);

            foreach (var kvp in DataSection)
            {
                var nameBytes = Encoding.UTF8.GetBytes(kvp.Key ?? "");
                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);

                if (kvp.Value is string str)
                {
                    writer.Write((byte)0x20);
                    var bytes = Encoding.UTF8.GetBytes(str + "\0");
                    writer.Write((uint)bytes.Length);
                    writer.Write(bytes);
                }
                else if (kvp.Value is int intVal)
                {
                    writer.Write((byte)0x04);
                    writer.Write(intVal);
                }
                else if (kvp.Value is float floatVal)
                {
                    writer.Write((byte)0x08);
                    writer.Write(floatVal);
                }
                else if (kvp.Value is double doubleVal)
                {
                    writer.Write((byte)0x10);
                    writer.Write(doubleVal);
                }
                else if (kvp.Value is long longVal)
                {
                    // **64 位整数**。原来这里直接抛 `Unsupported data type: System.Int64`，
                    // 而 64 位常量在 C 标准库里到处都是（实测一个 hello 级程序的数据段 160 项里
                    // 7 项是 Int64，全来自 conv.vml / convert64.vml 的浮点常量）——
                    // 于是「.vml 编 .vmb」对几乎所有 C 程序都失败。
                    //
                    // 编码沿用 double 那一路（tag 0x10 + 8 字节小端）：装载侧对 tag 0x10 是
                    // `ReadDouble()` → `BitConverter.GetBytes(doubleValue)` **逐字节搬运**，
                    // 三处（写 / 读 / 装载）都没有一处按数值语义去解释它，所以位模式原样保留、等价。
                    // 宿主侧的 `MauiVml.NormalizeLongConstants` 做的就是同一件事；编码器修好之后
                    // 那一步已经不是必需的（留着不影响 —— 两条路产出的字节完全一致）。
                    writer.Write((byte)0x10);
                    writer.Write(longVal);
                }
                else if (kvp.Value is System.Collections.IList objList)
                {
                    writer.Write((byte)0x40);
                    writer.Write((uint)objList.Count);
                    foreach (var elem in objList)
                    {
                        // ⚠ 列表元素也要认 long：原来的 else 分支**静默写成 0** ——
                        // 一个 64 位常量落在数组里就变成 0，而且不报错（比抛异常难查得多）。
                        if (elem is int i) { writer.Write((byte)0x04); writer.Write(i); }
                        else if (elem is float f) { writer.Write((byte)0x08); writer.Write(f); }
                        else if (elem is double d) { writer.Write((byte)0x10); writer.Write(d); }
                        else if (elem is long l) { writer.Write((byte)0x10); writer.Write(l); }
                        else { writer.Write((byte)0x04); writer.Write(0); }
                    }
                }
                else if (kvp.Value is DataString ds)
                {
                    writer.Write((byte)0x20);
                    var bytes = Encoding.UTF8.GetBytes(ds.Value + "\0");
                    writer.Write((uint)bytes.Length);
                    writer.Write(bytes);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported data type: {kvp.Value.GetType()}");
                }
            }
        }

        private void WriteConstSection(BinaryWriter writer)
        {
            writer.Write((uint)Constants.Count);

            foreach (var kvp in Constants)
            {
                var nameBytes = Encoding.UTF8.GetBytes(kvp.Key ?? "");
                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);

                if (kvp.Value is string str)
                {
                    writer.Write((byte)0x20);
                    var bytes = Encoding.UTF8.GetBytes(str + "\0");
                    writer.Write((uint)bytes.Length);
                    writer.Write(bytes);
                }
                else if (kvp.Value is int intVal)
                {
                    writer.Write((byte)0x04);
                    writer.Write(intVal);
                }
                else if (kvp.Value is double dVal)
                {
                    writer.Write((byte)0x10);
                    writer.Write(dVal);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported const type: {kvp.Value.GetType()}");
                }
            }
        }

        private void WriteSymbolsSection(BinaryWriter writer)
        {
            writer.Write((uint)Labels.Count);

            foreach (var label in Labels)
            {
                var nameBytes = Encoding.UTF8.GetBytes(label.Key);
                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);
                writer.Write((byte)0x01);
                writer.Write((byte)0x01);
                // 符号表存储原始指令索引(C#兼容); C运行时在label_map中自行转换
                uint addr = (uint)label.Value;
                string lname = label.Key;
                // 数据标签固定为0 (由DataSection加载时分配heap地址)
                if (lname.StartsWith("str_data_") || lname.StartsWith("dbl_") || lname.StartsWith("flt_"))
                    addr = 0;
                writer.Write(addr);
                writer.Write((byte)0x00);
                writer.Write(new byte[3]);
            }
        }

        private void WriteRelocationSection(BinaryWriter writer)
        {
            writer.Write((uint)0);
        }

        private static uint ComputeChecksum(MemoryStream stream)
        {
            var data = stream.ToArray();
            uint checksum = 0;
            for (int i = 0; i < data.Length; i++)
                checksum = (checksum + data[i]) & 0xFFFFFFFF;
            return checksum;
        }

        private static VmbHeader ReadVmbHeader(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;

            var magic = reader.ReadBytes(4);
            if (!magic.SequenceEqual(new byte[] { 0x56, 0x4D, 0x42, 0x00 }))
                throw new InvalidDataException("Invalid VMB magic number");

            var major = reader.ReadUInt16();
            var minor = reader.ReadUInt16();

            if (major < 1 || major > 2)
                throw new NotSupportedException($"Unsupported VMB version: {major}.{minor} (本运行时支持 1.x-2.x)");

            return new VmbHeader
            {
                Magic = magic,
                VersionMajor = major,
                VersionMinor = minor,
                HeaderSize = reader.ReadUInt32(),
                FileSize = reader.ReadUInt32(),
                CodeOffset = reader.ReadUInt32(),
                CodeSize = reader.ReadUInt32(),
                DataOffset = reader.ReadUInt32(),
                DataSize = reader.ReadUInt32(),
                ConstOffset = reader.ReadUInt32(),
                ConstSize = reader.ReadUInt32(),
                SymOffset = reader.ReadUInt32(),
                SymSize = reader.ReadUInt32(),
                EntryPoint = reader.ReadUInt32(),
                StackTop = reader.ReadUInt32(),
                VectorTable = reader.ReadUInt32(),
                Flags = reader.ReadUInt32(),
                CpuSpeed = minor >= 1 ? (int)reader.ReadUInt32() : 0
            };
        }

        private static void ValidateHeader(VmbHeader header, int fileSize)
        {
            if (header.HeaderSize != 64 && header.HeaderSize != 68)
                throw new InvalidDataException($"无效 VMB 头部大小: {header.HeaderSize}，预期 64 或 68");
            if (fileSize < header.HeaderSize + 4)
                throw new InvalidDataException($"VMB 文件过小: {fileSize} 字节");
            if (header.FileSize != 0 && header.FileSize != fileSize)
                throw new InvalidDataException($"VMB 文件大小不匹配: 声明={header.FileSize}，实际={fileSize}");

            // 校验各段偏移不越界且不重叠
            var sections = new (uint offset, uint size, string name)[]
            {
                (header.CodeOffset, header.CodeSize, "Code"),
                (header.DataOffset, header.DataSize, "Data"),
                (header.ConstOffset, header.ConstSize, "Const"),
                (header.SymOffset, header.SymSize, "Symbols")
            };

            foreach (var (offset, size, name) in sections)
            {
                if (size > 0 && offset < header.HeaderSize)
                    throw new InvalidDataException($"VMB {name} 段偏移与头部重叠: offset={offset}");
                if (size > 0 && (long)offset + size > fileSize - 4)
                    throw new InvalidDataException($"VMB {name} 段越界: offset={offset}, size={size}, fileSize={fileSize}");
            }

            // 校验段间不重叠（简化：只检查相邻段排序）
            var sorted = sections.Where(s => s.size > 0).OrderBy(s => s.offset).ToArray();
            for (int i = 1; i < sorted.Length; i++)
            {
                var prev = sorted[i - 1];
                var curr = sorted[i];
                if (prev.offset + prev.size > curr.offset)
                    throw new InvalidDataException($"VMB 段重叠: {prev.name} 结束于 {prev.offset + prev.size}，{curr.name} 起始于 {curr.offset}");
            }
        }

        private static List<Instruction> ReadCodeSection(BinaryReader reader, VmbHeader header)
        {
            var instructions = new List<Instruction>();
            if (header.CodeSize == 0) return instructions;

            reader.BaseStream.Seek(header.CodeOffset, SeekOrigin.Begin);
            var count = reader.ReadUInt32();

            for (uint i = 0; i < count; i++)
            {
                var opcode = (OpCode)reader.ReadByte();
                var operandCount = reader.ReadByte();
                var operands = new List<Operand>();

                for (int j = 0; j < operandCount; j++)
                {
                    operands.Add(ReadOperand(reader));
                }

                instructions.Add(new Instruction(opcode, operands, (int)i));
            }

            return instructions;
        }

        private static Operand ReadOperand(BinaryReader reader)
        {
            var typeTag = reader.ReadByte();

            switch (typeTag)
            {
                case 0x01:
                    return new Operand(OperandType.REGISTER, (int)reader.ReadByte());

                case 0x02:
                    var width = reader.ReadByte();
                    switch (width)
                    {
                        case 0x01: return new Operand(OperandType.IMMEDIATE, (int)reader.ReadSByte());
                        case 0x02: return new Operand(OperandType.IMMEDIATE, (int)reader.ReadInt16());
                        case 0x04: return new Operand(OperandType.IMMEDIATE, reader.ReadInt32());
                        default: throw new InvalidDataException($"Unknown immediate width: {width}");
                    }

                // 间接寻址（写侧 0x05）。**不再用"peek 后面 4 字节最高位"去猜** ——
                // 那个启发式与 MEMORY 的子模式 0x02 撞车（第 4 字节就是偏移量的最低字节），
                // 偏移 ≥ 128 的寄存器相对寻址会被误读成间接寻址、后面整条错位。
                // 详见写侧 `case OperandType.INDIRECT` 的注释。
                case 0x05:
                    {
                        uint enc = reader.ReadUInt32();
                        int rn = (int)((enc >> 24) & 0x7F);
                        int off = (int)(enc & 0x00FFFFFF);
                        if ((off & 0x800000) != 0) off |= unchecked((int)0xFF000000);
                        return new Operand(OperandType.MEMORY, $"R{rn}{(off >= 0 ? "+" : "-")}{Math.Abs(off)}");
                    }

                case 0x03:
                    var memMode = reader.ReadByte();
                    if (memMode == 0x02)
                    {
                        var regNum = reader.ReadByte();
                        var sign = reader.ReadSByte();
                        var offset = reader.ReadInt32();
                        // ⚠ 符号位是**约定值**（写侧：0 = 正、1 = 负），不是有符号数。
                        // 原来写成 `sign >= 0 ? "+" : "-"` ⇒ 无论正负都是 "+" ——
                        // 于是 `[R12-24]` 读回来变成 `[R12+24]`，**局部变量与参数全部指错**。
                        // 这种错不会抛异常，只是程序行为诡异地不对（最难查的一类）。
                        string regStr = $"R{regNum}{(sign == 0 ? "+" : "-")}{offset}";
                        return new Operand(OperandType.MEMORY, regStr);
                    }
                    if (memMode == 0x03)
                    {
                        var lblLen = reader.ReadUInt16();
                        var lblName = Encoding.UTF8.GetString(reader.ReadBytes(lblLen));
                        return new Operand(OperandType.MEMORY, lblName);
                    }
                    return new Operand(OperandType.MEMORY, reader.ReadInt32());

                case 0x04:
                    var nameLen = reader.ReadUInt16();
                    var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                    return new Operand(OperandType.LABEL, name);

                default:
                    throw new InvalidDataException($"Unknown operand type tag: 0x{typeTag:X2}");
            }
        }

        private static Dictionary<string, object> ReadDataSection(BinaryReader reader, VmbHeader header)
        {
            var result = new Dictionary<string, object>();
            if (header.DataSize == 0) return result;

            reader.BaseStream.Seek(header.DataOffset, SeekOrigin.Begin);
            var count = reader.ReadUInt32();

            for (uint i = 0; i < count; i++)
            {
                var nameLen = reader.ReadUInt16();
                var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                var typeTag = reader.ReadByte();

                switch (typeTag)
                {
                    case 0x04:
                        result[name] = reader.ReadInt32();
                        break;
                    case 0x08:
                        result[name] = reader.ReadSingle();
                        break;
                    case 0x10:
                        result[name] = reader.ReadDouble();
                        break;
                    case 0x20:
                        var strLen = reader.ReadUInt32();
                        var strBytes = reader.ReadBytes((int)strLen);
                        var str = Encoding.UTF8.GetString(strBytes);
                        result[name] = str.TrimEnd('\0');
                        break;
                    case 0x40:
                        // **整数/浮点数组**（写侧 tag 0x40）。原来读侧根本没有这一支 ⇒
                        // 只要程序的数据段里有数组，`FromVmbBytes` 就抛 `Unknown data type tag: 0x40`。
                        // 与写侧同一份元素编码（0x04 int / 0x08 float / 0x10 double）。
                        {
                            var listLen = reader.ReadUInt32();
                            var list = new List<object>((int)listLen);
                            for (uint k = 0; k < listLen; k++)
                            {
                                switch (reader.ReadByte())
                                {
                                    case 0x04: list.Add(reader.ReadInt32()); break;
                                    case 0x08: list.Add(reader.ReadSingle()); break;
                                    case 0x10: list.Add(reader.ReadDouble()); break;
                                    // 写侧对不认识的元素写 0x04 + 0；这里照单读回，保持前进不中断
                                    default: throw new InvalidDataException("Unknown list element tag");
                                }
                            }
                            result[name] = list;
                        }
                        break;
                    default:
                        throw new InvalidDataException($"Unknown data type tag: 0x{typeTag:X2}");
                }
            }

            return result;
        }

        private static Dictionary<string, object> ReadConstSection(BinaryReader reader, VmbHeader header)
        {
            var result = new Dictionary<string, object>();
            if (header.ConstSize == 0) return result;

            reader.BaseStream.Seek(header.ConstOffset, SeekOrigin.Begin);
            var count = reader.ReadUInt32();

            for (uint i = 0; i < count; i++)
            {
                var nameLen = reader.ReadUInt16();
                var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                var typeTag = reader.ReadByte();

                switch (typeTag)
                {
                    case 0x04:
                        result[name] = reader.ReadInt32();
                        break;
                    case 0x10:
                        result[name] = reader.ReadDouble();
                        break;
                    case 0x20:
                        var strLen = reader.ReadUInt32();
                        var strBytes = reader.ReadBytes((int)strLen);
                        var str = Encoding.UTF8.GetString(strBytes);
                        result[name] = str.TrimEnd('\0');
                        break;
                    default:
                        throw new InvalidDataException($"Unknown const type tag: 0x{typeTag:X2}");
                }
            }

            return result;
        }

        private static Dictionary<string, int> ReadSymbolsSection(BinaryReader reader, VmbHeader header)
        {
            var result = new Dictionary<string, int>();
            if (header.SymSize == 0) return result;

            reader.BaseStream.Seek(header.SymOffset, SeekOrigin.Begin);
            var count = reader.ReadUInt32();

            for (uint i = 0; i < count; i++)
            {
                var nameLen = reader.ReadUInt16();
                var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                var type = reader.ReadByte();
                var defined = reader.ReadByte();
                var address = reader.ReadUInt32();
                var segment = reader.ReadByte();
                var padding = reader.ReadBytes(3);

                result[name] = (int)address;
            }

            return result;
        }

        private static void ApplyRelocations(BinaryReader reader, VmbHeader header, List<Instruction> instructions, Dictionary<string, int> labels)
        {
        }

        private struct VmbHeader
        {
            public byte[] Magic;
            public ushort VersionMajor;
            public ushort VersionMinor;
            public uint HeaderSize;
            public uint FileSize;
            public uint CodeOffset;
            public uint CodeSize;
            public uint DataOffset;
            public uint DataSize;
            public uint ConstOffset;
            public uint ConstSize;
            public uint SymOffset;
            public uint SymSize;
            public uint EntryPoint;
            public uint StackTop;
            public uint VectorTable;
            public uint Flags;
            public int CpuSpeed;
        }

        #endregion

        // ====== VML 链接器: 合并多个 VmlProgram ======

        /// <summary>
        /// 链接多个 VmlProgram 为一个。处理指令合并、标签重定位、dataSection 去重。
        /// 第一个程序的入口点保留为主入口。
        /// </summary>
        /// <param name="gcSections">启用死代码消除: 从入口点追踪可达代码, 丢弃未引用的函数</param>
        public static VmlProgram Link(List<VmlProgram> programs, bool gcSections = true)
        {
            if (programs == null || programs.Count == 0) throw new System.ArgumentException("至少需要一个程序");
            if (programs.Count == 1) return programs[0];

            if (gcSections)
                return LinkWithGC(programs);
            return LinkAll(programs);
        }

        /// <summary>全量合并 (无优化)</summary>
        private static VmlProgram LinkAll(List<VmlProgram> programs)
        {
            var merged = new List<Instruction>();
            var mergedLabels = new Dictionary<string, int>();
            var mergedData = new Dictionary<string, object>();
            var mergedConst = new Dictionary<string, object>();
            int instrOffset = 0;

            for (int pi = 0; pi < programs.Count; pi++)
            {
                var prog = programs[pi];
                var remap = RemapLabels(prog, mergedLabels, instrOffset, pi);
                AppendProgram(prog, merged, mergedLabels, mergedData, mergedConst, remap, ref instrOffset);
            }

            return new VmlProgram(merged, mergedLabels, mergedData, mergedConst)
            {
                EntryPoint = programs[0].EntryPoint, StackTop = programs[0].StackTop,
                StackBottom = programs[0].StackBottom, VectorTable = programs[0].VectorTable,
            };
        }

        /// <summary>死代码消除链接: 从入口追踪可达函数, 丢弃死代码</summary>
        private static VmlProgram LinkWithGC(List<VmlProgram> programs)
        {
            // 1. 先全量合并 (获得完整标签映射)
            var fullMerged = new List<Instruction>();
            var fullLabels = new Dictionary<string, int>();
            var fullData = new Dictionary<string, object>();
            var fullConst = new Dictionary<string, object>();
            var remaps = new List<Dictionary<string, string>>();
            int offset = 0;

            for (int pi = 0; pi < programs.Count; pi++)
            {
                var remap = RemapLabels(programs[pi], fullLabels, offset, pi);
                remaps.Add(remap);
                AppendProgram(programs[pi], fullMerged, fullLabels, fullData, fullConst, remap, ref offset);
            }

            // 2. 如果找不到入口函数(main), 跳过死代码消除
            if (!fullLabels.ContainsKey(programs[0].EntryPoint))
                return LinkAll(programs);

            // 3. 从入口点BFS追踪可达指令
            var reachable = new HashSet<int>();
            var queue = new Queue<int>();
            if (fullLabels.TryGetValue(programs[0].EntryPoint, out int entryAddr))
            {
                queue.Enqueue(entryAddr);
                reachable.Add(entryAddr);
            }

            while (queue.Count > 0)
            {
                int addr = queue.Dequeue();
                if (addr < 0 || addr >= fullMerged.Count) continue;
                var instr = fullMerged[addr];

                // 追踪下一条指令 (fall-through)
                int next = addr + 1;
                if (next < fullMerged.Count && !reachable.Contains(next) && !IsUnconditionalJump(instr))
                {
                    reachable.Add(next);
                    queue.Enqueue(next);
                }

                // 追踪跳转目标
                foreach (var op in instr.Operands)
                {
                    if (op.Type == OperandType.LABEL && op.Value is string lbl && fullLabels.TryGetValue(lbl, out int target))
                    {
                        if (!reachable.Contains(target))
                        {
                            reachable.Add(target);
                            queue.Enqueue(target);
                        }
                    }
                }
            }

            // 3. 保留可达指令 + 所有数据段
            var merged = new List<Instruction>();
            var mergedLabels = new Dictionary<string, int>();
            var addrMap = new Dictionary<int, int>(); // 旧地址→新地址
            int newAddr = 0;

            for (int i = 0; i < fullMerged.Count; i++)
            {
                if (reachable.Contains(i) || fullMerged[i].Opcode == OpCode.LABEL)
                {
                    addrMap[i] = newAddr;
                    var newInstr = new Instruction(fullMerged[i].Opcode,
                        fullMerged[i].Operands.Select(op => op).ToList(), newAddr, fullMerged[i].Label);
                    merged.Add(newInstr);
                    newAddr++;
                }
            }

            // 重建标签表 (只保留可达标签 + 数据段标签)
            foreach (var kv in fullLabels)
            {
                if (addrMap.TryGetValue(kv.Value, out int na))
                    mergedLabels[kv.Key] = na;
            }
            // 数据段标签保留
            foreach (var kv in fullData)
                if (!mergedLabels.ContainsKey(kv.Key))
                    mergedLabels[kv.Key] = fullLabels.GetValueOrDefault(kv.Key, 0);

            return new VmlProgram(merged, mergedLabels, fullData, fullConst)
            {
                EntryPoint = programs[0].EntryPoint, StackTop = programs[0].StackTop,
                StackBottom = programs[0].StackBottom, VectorTable = programs[0].VectorTable,
            };
        }

        private static bool IsUnconditionalJump(Instruction instr) =>
            instr.Opcode == OpCode.JMP || instr.Opcode == OpCode.RET || instr.Opcode == OpCode.HALT;

        private static Dictionary<string, string> RemapLabels(VmlProgram prog, Dictionary<string, int> mergedLabels, int offset, int pi)
        {
            var remap = new Dictionary<string, string>();
            foreach (var kv in prog.Labels)
            {
                string name = kv.Key;
                if (pi > 0 && mergedLabels.ContainsKey(name) && !prog.DataSection.ContainsKey(name))
                {
                    string unique = $"{name}_{pi}";
                    remap[name] = unique;
                    mergedLabels[unique] = kv.Value + offset;
                }
                else
                {
                    remap[name] = name;
                    mergedLabels[name] = kv.Value + offset;
                }
            }
            return remap;
        }

        private static void AppendProgram(VmlProgram prog, List<Instruction> merged,
            Dictionary<string, int> mergedLabels, Dictionary<string, object> mergedData,
            Dictionary<string, object> mergedConst, Dictionary<string, string> remap, ref int offset)
        {
            foreach (var instr in prog.Instructions)
            {
                var newOps = instr.Operands.Select(op =>
                {
                    if (op.Type == OperandType.LABEL && op.Value is string l && remap.TryGetValue(l, out var r))
                        return new Operand(OperandType.LABEL, r);
                    if (op.Type == OperandType.MEMORY && op.Value is string m && remap.TryGetValue(m, out var r2))
                        return new Operand(OperandType.MEMORY, r2);
                    return op;
                }).ToList();
                merged.Add(new Instruction(instr.Opcode, newOps, merged.Count, instr.Label));
            }
            foreach (var kv in prog.DataSection)
            {
                string name = remap.TryGetValue(kv.Key, out var r) ? r : kv.Key;
                if (!mergedData.ContainsKey(name)) mergedData[name] = kv.Value;
            }
            foreach (var kv in prog.Constants)
                if (!mergedConst.ContainsKey(kv.Key)) mergedConst[kv.Key] = kv.Value;
            offset = merged.Count;
        }
    }
}
