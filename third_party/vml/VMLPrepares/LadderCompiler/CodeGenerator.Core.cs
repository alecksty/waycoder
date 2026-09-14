using VMLAssembler;
using CompilerBase;

namespace LadderCompiler
{
    public partial class CodeGenerator
    {
        private int _tempVarCounter = 0;

        // I/O内存映射
        private readonly Dictionary<string, string> _ioMap = new(); // 变量名 -> %I/%Q/%M地址
        private readonly List<string> _inputVars = new(); // 输入变量列表
        private readonly List<string> _outputVars = new(); // 输出变量列表
        private readonly List<VariableDeclarationNode> _allVars = new(); // 所有变量
        
        // 变量类型跟踪字典
        private readonly Dictionary<string, LadderTypeEnum> _varTypes = new();
        private readonly Dictionary<string, (int Lower, int Upper, string ElementType)> _arrays = new();
        private readonly Dictionary<string, List<(string Name, string Type)>> _structs = new();
        private readonly Dictionary<string, int> _enumValues = new();

        // I/O内存区域基地址（可通过命令行 --io-base 配置）
        private int _inputBaseAddr = 0x1000;
        private int _outputBaseAddr = 0x2000;
        private int _memoryBaseAddr = 0x3000;
        public int InputBaseAddr { get => _inputBaseAddr; set => _inputBaseAddr = value; }
        public int OutputBaseAddr { get => _outputBaseAddr; set => _outputBaseAddr = value; }
        public int MemoryBaseAddr { get => _memoryBaseAddr; set => _memoryBaseAddr = value; }

        /// <summary>
        /// 解析IEC 61131-3内存地址 (%I0.0, %Q0.0, %M0.0等)
        /// 返回内存地址偏移量
        /// </summary>
        private int ParseMemoryAddress(string address)
        {
            if (string.IsNullOrEmpty(address) || !address.StartsWith("%"))
                return -1;

            // 格式: %I0.0, %Q0.0, %M0.0, %IW0, %QW0, %ID0, %QD0
            string addr = address.Substring(1); // 去掉%
            char areaType = char.ToUpper(addr[0]);
            string numPart = addr.Substring(1);

            // 解析字节/位地址
            if (numPart.Contains('.'))
            {
                // 位地址: %I0.0 -> 字节0, 位0
                var parts = numPart.Split('.');
                int byteAddr = int.Parse(parts[0]);
                int bitAddr = int.Parse(parts[1]);
                return byteAddr * 8 + bitAddr;
            }
            else
            {
                // 字/双字地址: %IW0, %QW0
                if (numPart.Length > 1 && (numPart[0] == 'X' || numPart[0] == 'W' || numPart[0] == 'D'))
                {
                    return int.Parse(numPart.Substring(1));
                }
                return int.Parse(numPart);
            }
        }

        /// <summary>
        /// 获取变量的内存地址
        /// </summary>
        private int GetVariableAddress(string varName)
        {
            if (_ioMap.TryGetValue(varName, out string address))
            {
                return ParseMemoryAddress(address);
            }
            return -1;
        }

        /// <summary>
        /// 获取变量类型前缀 (%I, %Q, %M)
        /// </summary>
        private string GetVariableArea(string varName)
        {
            if (_ioMap.TryGetValue(varName, out string address))
            {
                if (address.StartsWith("%I") || address.StartsWith("%i"))
                    return "I";
                if (address.StartsWith("%Q") || address.StartsWith("%q"))
                    return "Q";
                if (address.StartsWith("%M") || address.StartsWith("%m"))
                    return "M";
            }
            return "M"; // 默认内存区域
        }

        #region TypedCodeGen — 类型信息映射

        /// <summary>
        /// LadderTypeEnum → (byteSize, isFloat, isDouble, isLong)
        /// 修复: LReal=8字节double, LWord/LInt=8字节long
        /// </summary>
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(LadderTypeEnum t) => t switch
        {
            LadderTypeEnum.Bool or LadderTypeEnum.Byte => (1, false, false, false),
            LadderTypeEnum.Word or LadderTypeEnum.Int => (2, false, false, false),
            LadderTypeEnum.DWord or LadderTypeEnum.DInt => (4, false, false, false),
            LadderTypeEnum.LWord or LadderTypeEnum.LInt => (8, false, false, true),  // 64-bit int → isLong
            LadderTypeEnum.Real => (4, true, false, false),
            LadderTypeEnum.LReal => (8, false, true, false),  // 修复: LREAL=8字节double
            LadderTypeEnum.String or LadderTypeEnum.Timer or LadderTypeEnum.Counter
                or LadderTypeEnum.Array or LadderTypeEnum.Struct or LadderTypeEnum.Object => (4, false, false, false),
            _ => (4, false, false, false),
        };

        // GetLoadInstruction/GetStoreInstruction/GetMoveInstruction/GetPushInstruction/GetPopInstruction
        // GetArithmeticInstruction/GetCompareInstruction — 全部由 TypedCodeGen<LadderTypeEnum> 提供

        /// <summary>
        /// 根据梯形图类型名称获取LadderTypeEnum
        /// </summary>
        private LadderTypeEnum GetLadderTypeEnum(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return LadderTypeEnum.Bool;

            string upperType = typeName.ToUpper();
            return upperType switch
            {
                "BOOL" => LadderTypeEnum.Bool,
                "BYTE" => LadderTypeEnum.Byte,
                "WORD" => LadderTypeEnum.Word,
                "DWORD" => LadderTypeEnum.DWord,
                "LWORD" => LadderTypeEnum.LWord,
                "INT" => LadderTypeEnum.Int,
                "DINT" => LadderTypeEnum.DInt,
                "LINT" => LadderTypeEnum.LInt,
                "REAL" => LadderTypeEnum.Real,
                "LREAL" => LadderTypeEnum.LReal,
                "STRING" => LadderTypeEnum.String,
                "TIME" => LadderTypeEnum.DInt,
                "DATE" => LadderTypeEnum.DInt,
                "TOD" => LadderTypeEnum.DInt,
                "TIME_OF_DAY" => LadderTypeEnum.DInt,
                "DATE_AND_TIME" => LadderTypeEnum.DInt,
                "TIMER" => LadderTypeEnum.Timer,
                "COUNTER" => LadderTypeEnum.Counter,
                _ when upperType.StartsWith("ARRAY") => LadderTypeEnum.Array,
                _ when upperType.StartsWith("STRUCT") => LadderTypeEnum.Struct,
                _ when upperType.StartsWith("ENUM") => LadderTypeEnum.Int,
                _ when upperType.StartsWith("SUBRANGE") => LadderTypeEnum.Int,
                _ => LadderTypeEnum.Object
            };
        }

        /// <summary>获取类型字节大小 — 委托到 GetTypeInfo</summary>
        private int GetTypeSize(LadderTypeEnum type) => GetTypeInfo(type).byteSize;

        #endregion

        #region 辅助方法

        private static string NormalizeStorageName(string varName)
        {
            return varName.Replace("[", "_").Replace("]", "").Replace('.', '_');
        }

        private string ResolveStorageName(string varName)
        {
            if (dataSection.ContainsKey(varName))
                return varName;

            string normalized = NormalizeStorageName(varName);
            if (dataSection.ContainsKey(normalized))
                return normalized;

            return normalized;
        }

        private static bool TryParseArrayType(string type, out int lower, out int upper, out string elementType)
        {
            lower = 0;
            upper = -1;
            elementType = "INT";
            string upperType = type.ToUpper();
            if (!upperType.StartsWith("ARRAY["))
                return false;

            int start = type.IndexOf('[');
            int end = type.IndexOf(']');
            if (start < 0 || end <= start)
                return false;

            string range = type.Substring(start + 1, end - start - 1);
            var parts = range.Split(new[] { ".." }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                int.TryParse(parts[0], out lower);
                int.TryParse(parts[1], out upper);
            }
            else if (int.TryParse(range, out upper))
            {
                lower = 0;
            }

            int ofIndex = upperType.IndexOf(" OF ", StringComparison.Ordinal);
            if (ofIndex >= 0)
                elementType = type.Substring(ofIndex + 4).Trim();
            return upper >= lower;
        }

        private static List<(string Name, string Type)> ParseStructFields(string type)
        {
            var result = new List<(string Name, string Type)>();
            if (!type.StartsWith("STRUCT{", StringComparison.OrdinalIgnoreCase) || !type.EndsWith("}"))
                return result;

            string body = type.Substring(7, type.Length - 8);
            foreach (var part in body.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = part.IndexOf(':');
                if (colon <= 0) continue;
                result.Add((part.Substring(0, colon).Trim(), part.Substring(colon + 1).Trim()));
            }
            return result;
        }

        private static List<string> SplitInitialList(string initialValue)
        {
            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(initialValue))
                return values;
            string trimmed = initialValue.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            foreach (var item in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries))
                values.Add(item.Trim());
            return values;
        }

        private static object ParseInitialValue(string initialValue, object defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(initialValue))
                return defaultValue ?? 0;

            string value = initialValue.Trim().Trim('"', '\'');
            if (value.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return 1;
            if (value.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return 0;
            if (value.StartsWith("T#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("TIME#", StringComparison.OrdinalIgnoreCase))
                return ParseTimeLiteral(value);
            if (value.StartsWith("D#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("DATE#", StringComparison.OrdinalIgnoreCase))
                return ParseDateLikeLiteral(value);
            if (value.StartsWith("TOD#", StringComparison.OrdinalIgnoreCase) || value.StartsWith("TIME_OF_DAY#", StringComparison.OrdinalIgnoreCase))
                return ParseDateLikeLiteral(value);
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int hexVal))
                return hexVal;
            if (int.TryParse(value, out int intVal)) return intVal;
            if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal)) return floatVal;
            return value;
        }

        private static int ParseTimeLiteral(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return 0;
            string s = timeStr.Trim().ToUpperInvariant();
            if (s.StartsWith("TIME#")) s = s.Substring(5);
            else if (s.StartsWith("T#")) s = s.Substring(2);
            int ms = 0;
            int pos = 0;
            while (pos < s.Length)
            {
                int start = pos;
                while (pos < s.Length && char.IsDigit(s[pos])) pos++;
                if (start == pos) break;
                int value = int.Parse(s.Substring(start, pos - start));
                if (pos >= s.Length) { ms += value; break; }
                char unit = s[pos++];
                if (unit == 'D') ms += value * 86400000;
                else if (unit == 'H') ms += value * 3600000;
                else if (unit == 'S') ms += value * 1000;
                else if (unit == 'M')
                {
                    if (pos < s.Length && s[pos] == 'S') { ms += value; pos++; }
                    else ms += value * 60000;
                }
            }
            return ms;
        }

        private static int ParseDateLikeLiteral(string value)
        {
            unchecked
            {
                return value.ToUpperInvariant().GetHashCode();
            }
        }

        #endregion

        /// <summary>
        /// 推断表达式的类型
        /// </summary>
        private LadderTypeEnum InferExpressionType(object expr)
        {
            if (expr is IdentifierNode idNode)
            {
                // 如果是标识符，返回变量类型
                return _varTypes.ContainsKey(idNode.Name) ? _varTypes[idNode.Name] : LadderTypeEnum.Bool;
            }
            else if (expr is LiteralNode litNode)
            {
                // 如果是字面值，根据值推断类型
                if (litNode.Value is bool) return LadderTypeEnum.Bool;
                if (litNode.Value is int i)
                {
                    // 根据值范围推断类型
                    if (i >= -128 && i <= 127) return LadderTypeEnum.Byte;
                    if (i >= -32768 && i <= 32767) return LadderTypeEnum.Int;
                    return LadderTypeEnum.DInt;
                }
                if (litNode.Value is float) return LadderTypeEnum.Real;
                if (litNode.Value is double) return LadderTypeEnum.LReal;
                if (litNode.Value is string) return LadderTypeEnum.String;
            }
            
            return LadderTypeEnum.Bool; // 默认BOOL类型
        }

        /// <summary>
        /// 从输入区域读取变量到R0
        /// </summary>
        private void ReadInput(string varName)
        {
            int offset = GetVariableAddress(varName);
            if (offset >= 0)
            {
                // 从输入内存区域读取
                int addr = InputBaseAddr + offset;
                
                // 获取变量类型并使用正确的加载指令
                LadderTypeEnum varType = _varTypes.ContainsKey(varName) ? _varTypes[varName] : LadderTypeEnum.Bool;
                OpCode loadOp = GetLoadInstruction(varType);
                
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.IMMEDIATE, addr));
                AddInstruction(loadOp,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 1));
            }
            else
            {
                // 无地址映射，从数据段读取
                LoadVariable(varName);
            }
        }

        /// <summary>
        /// 将R0写入输出区域
        /// </summary>
        private void WriteOutput(string varName)
        {
            int offset = GetVariableAddress(varName);
            if (offset >= 0)
            {
                // 写入输出内存区域
                int addr = OutputBaseAddr + offset;
                
                // 获取变量类型并使用正确的存储指令
                LadderTypeEnum varType = _varTypes.ContainsKey(varName) ? _varTypes[varName] : LadderTypeEnum.Bool;
                OpCode storeOp = GetStoreInstruction(varType);
                
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.IMMEDIATE, addr));
                AddInstruction(storeOp,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 1));
            }
            else
            {
                // 无地址映射，写入数据段
                StoreVariable(varName);
            }
        }

        /// <summary>
        /// 生成I/O扫描周期代码
        /// 梯形图程序通常运行在循环中: 读取输入 -> 执行逻辑 -> 写入输出
        /// </summary>
        private void GenerateIOScanCycle()
        {
            var program = _program!;
            string scanLoopLabel = "scan_loop";
            string scanEndLabel = "scan_end";

            AddLabel(scanLoopLabel);

            // 1. 读取所有输入变量
            foreach (var varName in _inputVars)
            {
                ReadInput(varName);
                // 将输入值复制到数据段变量
                if (dataSection.ContainsKey(varName))
                {
                    AddInstruction(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                    AddInstruction(OpCode.MOVE,
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.LABEL, varName));
                    AddInstruction(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                    AddInstruction(OpCode.MOVE,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 1));
                }
            }

            // 2. 执行 ST 语句 (IF/WHILE/FOR)
            foreach (var stmt in program.StStatements)
            {
                stmt.Accept(this);
            }

            // 3. 执行梯形图逻辑
            foreach (var rung in program.Rungs)
            {
                Visit(rung);
            }

            // 3. 写入所有输出变量
            foreach (var varName in _outputVars)
            {
                // 从数据段读取输出值
                if (dataSection.ContainsKey(varName))
                {
                    AddInstruction(OpCode.MOVE,
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, varName));
                    WriteOutput(varName);
                }
            }

            // 4. 循环
            AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, scanLoopLabel));

            AddLabel(scanEndLabel);
        }

        /// <summary>
        /// 生成VML代码
        /// </summary>
        public override VmlProgram GenerateCode()
        {
            var program = _program!;

            // JMP over function definitions to main
            string afterFuncsLabel = $"after_funcs_{labelCounter++}";
            bool hasFunctions = program.Functions.Count > 0;
            if (hasFunctions)
            {
                AddInstruction(OpCode.JMP, new Operand(OperandType.LABEL, afterFuncsLabel));
            }

            // Generate FUNCTION definitions
            foreach (var func in program.Functions)
            {
                GenerateFunctionDefinition(func);
            }

            if (hasFunctions)
            {
                AddLabel(afterFuncsLabel);
            }

            // 添加主程序入口标签
            AddLabel("main");

            // 第一阶段: 初始化所有变量到数据段
            foreach (var varDecl in program.Variables)
            {
                // 收集I/O映射信息
                if (!string.IsNullOrEmpty(varDecl.MemoryLocation))
                {
                    _ioMap[varDecl.Name] = varDecl.MemoryLocation;
                }
                if (varDecl.IsInput)
                {
                    _inputVars.Add(varDecl.Name);
                }
                if (varDecl.IsOutput)
                {
                    _outputVars.Add(varDecl.Name);
                }
                _allVars.Add(varDecl);
                Visit(varDecl);
            }

            // 第二阶段: 生成I/O扫描周期(读取输入->执行逻辑->写入输出)
            if (_inputVars.Count > 0 || _outputVars.Count > 0)
            {
                GenerateIOScanCycle();
            }
            else
            {
                // 无I/O映射，直接生成梯形图逻辑和ST语句后停机
                // ST 语句 (IF/WHILE/FOR)
                foreach (var stmt in program.StStatements)
                {
                    stmt.Accept(this);
                }
                // 梯形图逻辑
                foreach (var rung in program.Rungs)
                {
                    Visit(rung);
                }
                EmitExit();
            }

            return BuildProgram("main");
        }

        private void GenerateFunctionDefinition(FunctionDefinitionNode func)
        {
            string funcLabel = $"LADDER_{func.Name.ToUpperInvariant()}";
            AddLabel(funcLabel);

            // === 序言: 栈帧建立 (支持递归) ===
            EmitPrologue();

            // Register-based calling: input in R0, output in R0
            // Save input parameter to local variable for ST access
            if (func.InputParams.Count > 0)
            {
                var firstParam = func.InputParams[0].Name;
                StoreVariable(firstParam); // STORE R0 → param variable
            }

            // Process ST statements (IF/WHILE/FOR) before rungs
            foreach (var stmt in func.StStatements)
            {
                stmt.Accept(this);
            }
            foreach (var rung in func.Rungs)
            {
                if (rung.Output is AssignmentNode assign)
                {
                    // Use proper Visitor pattern to handle any expression type
                    Visit(assign);
                }
            }

            // === 尾声: 栈帧恢复 (EmitEpilogue 含 RET) ===
            EmitEpilogue();
        }

        private string NewTempVar() => $"_temp{_tempVarCounter++}";

        /// <summary>
        /// 加载变量值到R0
        /// </summary>
        private void LoadVariable(string varName)
        {
            // 防御 null (解析器未识别的 token)
            if (varName == null)
            {
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0));
                return;
            }

            // TRUE/FALSE 常量
            if (varName.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 1));
                return;
            }
            if (varName.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0));
                return;
            }

            if (_enumValues.TryGetValue(varName, out int enumValue))
            {
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, enumValue));
                return;
            }

            varName = ResolveStorageName(varName);
            if (dataSection.ContainsKey(varName))
            {
                // 获取变量类型并使用正确的加载指令
                LadderTypeEnum varType = _varTypes.ContainsKey(varName) ? _varTypes[varName] : LadderTypeEnum.Bool;
                OpCode loadOp = GetLoadInstruction(varType);

                AddInstruction(loadOp,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, varName));
            }
            else
            {
                // 变量未声明，加载0
                AddInstruction(OpCode.MOVE,
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0));
            }
        }

        /// <summary>
        /// 存储R0到变量
        /// </summary>
        private void StoreVariable(string varName)
        {
            if (varName == null) return;
            varName = ResolveStorageName(varName);
            if (!dataSection.ContainsKey(varName))
            {
                dataSection[varName] = 0;
            }
            
            // 获取变量类型并使用正确的存储指令
            LadderTypeEnum varType = _varTypes.ContainsKey(varName) ? _varTypes[varName] : LadderTypeEnum.Bool;
            OpCode storeOp = GetStoreInstruction(varType);
            
            AddInstruction(storeOp,
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.MEMORY, varName));
        }

        // ========== Visitor实现 ==========

        public void Visit(ProgramNode node)
        {
            // ProgramNode由Generate方法处理
        }

        public void Visit(VariableDeclarationNode node)
        {
            // 在数据段中声明变量
            if (!dataSection.ContainsKey(node.Name))
            {
                if (TryParseArrayType(node.Type, out int lower, out int upper, out string elementType))
                {
                    _arrays[node.Name] = (lower, upper, elementType);
                    _varTypes[node.Name] = LadderTypeEnum.Array;
                    var initValues = SplitInitialList(node.InitialValue);
                    for (int i = lower; i <= upper; i++)
                    {
                        string elementName = $"{node.Name}_{i}";
                        string init = (i - lower) < initValues.Count ? initValues[i - lower] : null;
                        dataSection[elementName] = ParseInitialValue(init, 0);
                        _varTypes[elementName] = GetLadderTypeEnum(elementType);
                    }
                    dataSection[node.Name] = 0;
                    return;
                }

                if (node.Type.StartsWith("STRUCT", StringComparison.OrdinalIgnoreCase))
                {
                    var fields = ParseStructFields(node.Type);
                    _structs[node.Name] = fields;
                    _varTypes[node.Name] = LadderTypeEnum.Struct;
                    foreach (var field in fields)
                    {
                        string fieldName = $"{node.Name}_{field.Name}";
                        dataSection[fieldName] = 0;
                        _varTypes[fieldName] = GetLadderTypeEnum(field.Type);
                    }
                    dataSection[node.Name] = 0;
                    return;
                }

                if (node.Type.StartsWith("ENUM", StringComparison.OrdinalIgnoreCase))
                {
                    string body = node.Type.Length > 6 ? node.Type.Substring(5, node.Type.Length - 6) : "";
                    int index = 0;
                    foreach (var name in body.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        _enumValues[name.Trim()] = index++;
                }

                // 根据类型设置初始值
                object initialValue = node.Type.ToUpper() switch
                {
                    var t when t.StartsWith("BOOL") => 0,
                    var t when t.StartsWith("INT") || t.StartsWith("DINT") || t.StartsWith("WORD") || t.StartsWith("DWORD") || t.StartsWith("BYTE") => 0,
                    var t when t.StartsWith("REAL") => 0.0f,
                    var t when t.StartsWith("STRING") => "",
                    var t when t.StartsWith("TIME") || t.StartsWith("DATE") || t.StartsWith("TOD") => 0,
                    _ => 0
                };

                // 如果有初始值字符串，尝试解析
                initialValue = ParseInitialValue(node.InitialValue, initialValue);
                if (initialValue is string enumName && _enumValues.TryGetValue(enumName, out int enumInitial))
                    initialValue = enumInitial;

                dataSection[node.Name] = initialValue;
                
                // 记录变量类型
                LadderTypeEnum varType = GetLadderTypeEnum(node.Type);
                _varTypes[node.Name] = varType;
            }
        }

        public void Visit(LadderRungNode node)
        {
            // 梯级由左侧条件和右侧输出组成
            // 简化处理：依次处理每个元素
            
            foreach (var element in node.Elements)
            {
                VisitNode(element);
            }

            // 生成输出
            if (node.Output != null)
            {
                VisitNode(node.Output);
            }
        }

        private void VisitNode(ASTNode node)
        {
            switch (node)
            {
                case ContactNode contact: Visit(contact); break;
                case CoilNode coil: Visit(coil); break;
                case FunctionBlockNode fb: Visit(fb); break;
                case AssignmentNode assignment: Visit(assignment); break;
                case IdentifierNode id: Visit(id); break;
                case LiteralNode literal: Visit(literal); break;
                case BinaryExpressionNode binary: Visit(binary); break;
                case UnaryExpressionNode unary: Visit(unary); break;
                case CallNode call: Visit(call); break;
                case ArrayAccessNode array: Visit(array); break;
                case MemberAccessNode member: Visit(member); break;
                case ExpressionNode expr: Visit(expr); break;
            }
        }
    }
}
