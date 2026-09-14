using CompilerBase;
using VMLAssembler;
using System.Linq;

namespace PythonCompiler
{
    public partial class CodeGenerator
    {
#pragma warning disable CS0414
        private int currentLine;
#pragma warning restore CS0414
        private Dictionary<string, int> localVars;
        private int stackOffset;
        private string currentFunction;
        private string? _currentClassName; // 当前正在编译的类名（用于 super() 解析）
        private Dictionary<string, string> lambdaVars; // 变量名→lambda函数标签
        private Dictionary<string, int> globalVars; // 全局变量名→地址偏移
        private int globalVarOffset; // 全局变量偏移（从-4开始递减）
        private Dictionary<string, ClassInfo> classInfo; // 类名→类信息
        private int _mainEntry = 0; // 程序主入口指令位置
        private Dictionary<string, PythonType> varTypes; // 变量名→类型信息

        public CodeGenerator() : base()
        {
            currentLine = 0;
            localVars = new();
            stackOffset = 0;
            currentFunction = "";
            lambdaVars = new();
            globalVars = new();
            globalVarOffset = 0;
            classInfo = new();
            varTypes = new();
            InitSimpleCompiler();
        }

#pragma warning disable CS0809
        [System.Obsolete("Use Generate(ASTNode) instead", true)]
        public override VmlProgram GenerateCode() => throw new System.NotSupportedException("Use Generate(ASTNode) instead");
#pragma warning restore CS0809

        private new void Emit(OpCode opcode, List<Operand> operands, string label = "")
        {
            base.Emit(opcode, operands, label);
        }

        private void Emit(OpCode opcode, params Operand[] operands)
        {
            Emit(opcode, new List<Operand>(operands));
        }

        /// <summary>
        /// 放置标签到下一条指令的位置（非独立指令）
        /// </summary>
        private void PlaceLabel(string name)
        {
            labels[name] = instructions.Count;
        }

        // PythonType → (byteSize, isFloat, isDouble, isLong)
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(PythonType t) => t switch
        {
            PythonType.Float => (4, true, false, false),
            PythonType.Bool => (1, false, false, false),
            _ => (4, false, false, false),
        };

        private void GenerateTypeConversion(PythonType fromType, PythonType toType)
            => EmitTypeConversion(fromType, toType);

        private void GenerateExpressionWithType(ASTNode node, PythonType targetType)
        {
            PythonType sourceType = InferExpressionType(node);
            node.Accept(this);
            GenerateTypeConversion(sourceType, targetType);
        }

        private void EmitConversion(int fromSize, bool fromFloat, bool fromDouble,
                                     int toSize, bool toFloat, bool toDouble,
                                     bool fromLong = false, bool toLong = false)
        {
            var op = ExpressionManager.SelectConversionOp(fromSize, fromFloat, fromDouble, toSize, toFloat, toDouble, fromLong, toLong);
            if (op != null)
                Emit(op.Value, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0));
        }

        protected override OpCode GetArithmeticInstruction(string op, PythonType type)
        {
            // Python 特有运算符优先
            return op switch
            {
                "//" => OpCode.DIV,
                "**" => OpCode.MUL,
                "&" => OpCode.AND,
                "|" => OpCode.OR,
                "^" => OpCode.XOR,
                "<<" => OpCode.SHLV,
                ">>" => OpCode.SHRV,
                _ => base.GetArithmeticInstruction(op, type)
            };
        }

        /// <summary>
        /// 根据ValueType字符串获取PythonType
        /// </summary>
        private PythonType GetPythonType(string valueType)
        {
            return valueType switch
            {
                "int" => PythonType.Int,
                "float" => PythonType.Float,
                "bool" => PythonType.Bool,
                "str" => PythonType.String,
                "None" => PythonType.None,
                "list" => PythonType.List,
                "dict" => PythonType.Dict,
                "tuple" => PythonType.Tuple,
                "set" => PythonType.Set,
                _ => PythonType.Object
            };
        }

        /// <summary>
        /// 根据常量值推断PythonType
        /// </summary>
        private PythonType GetPythonTypeFromValue(object value)
        {
            return value switch
            {
                int => PythonType.Int,
                float => PythonType.Float,
                double => PythonType.Float,
                bool => PythonType.Bool,
                string => PythonType.String,
                null => PythonType.None,
                _ => PythonType.Object
            };
        }

        /// <summary>
        /// 推断表达式的类型（简化实现）
        /// </summary>
        private PythonType InferExpressionType(ASTNode node)
        {
            if (node is ConstantNode constNode)
            {
                return GetPythonTypeFromValue(constNode.Value);
            }
            else if (node is NameNode nameNode)
            {
                if (varTypes.TryGetValue(nameNode.Name, out PythonType type))
                {
                    return type;
                }
                return PythonType.Int; // 默认整数类型
            }
            else if (node is BinOpNode binOpNode)
            {
                // 对于二元运算，推断为操作数的类型
                PythonType leftType = InferExpressionType(binOpNode.Left);
                PythonType rightType = InferExpressionType(binOpNode.Right);
                
                // 如果有一个操作数是浮点数，结果就是浮点数
                if (leftType == PythonType.Float || rightType == PythonType.Float)
                {
                    return PythonType.Float;
                }
                // 如果有一个操作数是布尔值，结果就是整数
                else if (leftType == PythonType.Bool || rightType == PythonType.Bool)
                {
                    return PythonType.Int;
                }
                return leftType; // 默认返回左操作数的类型
            }
            else if (node is UnaryOpNode unaryNode)
            {
                return InferExpressionType(unaryNode.Operand);
            }
            else if (node is CallNode callNode)
            {
                // 函数调用，根据函数名推断类型
                return callNode.FuncName switch
                {
                    "float" => PythonType.Float,
                    "int" => PythonType.Int,
                    "bool" => PythonType.Bool,
                    "str" => PythonType.String,
                    _ => PythonType.Int // 默认整数类型
                };
            }
            else if (node is MethodCallNode methodCall)
            {
                // 方法调用：根据方法名推断返回类型
                return methodCall.Method switch
                {
                    "split" => PythonType.List,
                    "join" => PythonType.String,
                    "replace" => PythonType.String,
                    "upper" => PythonType.String,
                    "lower" => PythonType.String,
                    "strip" => PythonType.String,
                    "startswith" => PythonType.Bool,
                    "endswith" => PythonType.Bool,
                    "find" => PythonType.Int,
                    _ => PythonType.Object
                };
            }
            
            return PythonType.Int; // 默认整数类型
        }

        public VmlProgram Generate(ASTNode ast)
        {
            ast.Accept(this);
            
            // Auto-exit: load first top-level local variable (largest negative offset = closest to zero)
            int autoExitOffset = int.MinValue;
            foreach (var kvp in localVars)
                if (kvp.Value < 0 && kvp.Value > autoExitOffset) autoExitOffset = kvp.Value;
            if (autoExitOffset > int.MinValue)
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{autoExitOffset}")]));
            EmitExit();
            
            // 添加 main 入口（跳过函数定义，指向程序级语句）
            if (!labels.ContainsKey("main"))
            {
                labels["main"] = _mainEntry;
                
                // 在程序级语句前插入帧指针初始化（SP由运行时设置）
                instructions.Insert(_mainEntry, new Instruction(OpCode.SUB, new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, -stackOffset)  // 分配局部变量空间
                }));
                instructions.Insert(_mainEntry, new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 12),
                    new Operand(OperandType.REGISTER, 13)
                }));
                // 修正 _mainEntry 后的所有标签偏移
                var shift = 2;
                var keys = labels.Keys.ToList();
                foreach (var k in keys)
                    if (labels[k] >= _mainEntry && k != "main")
                        labels[k] += shift;
            }
            
            return BuildProgram("main");
        }
    }
}
