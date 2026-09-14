using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace GoCompiler
{
    public partial class CodeGenerator : CLikeCodegen<CodeGenerator>
    {
        private Program ast;
        private Dictionary<string, int> variables;
        private Dictionary<string, string> functionEndLabels;
        private Function currentFunction;
        private int localVarOffset;
        
        // 变量类型跟踪字典
        private Dictionary<string, GoTypeEnum> _varTypes = new Dictionary<string, GoTypeEnum>();
        // 完整类型元数据（用于 struct 布局等）
        private Dictionary<string, GoType> _typeDefs = new Dictionary<string, GoType>();

        private List<(string label, FuncLiteral funcLit)> pendingFuncLiterals
            = new List<(string, FuncLiteral)>();
        private List<ASTNode> _deferredCalls = new(); // LIFO defer queue per function
        public void AddPendingFuncLiteral(string label, FuncLiteral funcLit)
        {
            pendingFuncLiterals.Add((label, funcLit));
        }

        private void GeneratePendingFuncLiterals()
        {
            foreach (var (label, funcLit) in pendingFuncLiterals)
            {
                AddLabel(label);
                AddInstruction(OpCode.PUSH, Reg(14));
                AddRR(OpCode.MOVE, 14, 13);
                if (funcLit.Body != null)
                    GenerateBlock(funcLit.Body);
                AddInstruction(OpCode.POP, Reg(14));
                Sta!.EmitReturn();
            }
            pendingFuncLiterals.Clear();
        }

        public CodeGenerator(Program ast) : base()
        {
            // Go 使用 R14 作为帧指针，参数和局部变量统一向负方向增长
            InitSimpleCompiler(framePointerReg: 14, threeOperandInt: false, newLabel: () => NewLabel(), paramStart: -4);
            this.ast = ast;
            variables = new Dictionary<string, int>();
            functionEndLabels = new Dictionary<string, string>();
            InitTypeDefs();
        }

        // GoTypeEnum → (byteSize, isFloat, isDouble) 映射
        // 注意：Int64/UInt64 用 isDouble=true 以使用 VML double 操作码 (MOVED/DADD/DPUSH)，
        // 因为 VML 运行时 R0-R15 的 long 操作只处理 32 位，必须通过 double 路径操作 64 位
        private static (int size, bool isFloat, bool isDouble) TypeInfo(GoTypeEnum t) => t switch
        {
            GoTypeEnum.Bool or GoTypeEnum.Byte => (1, false, false),
            GoTypeEnum.Rune => (2, false, false),
            GoTypeEnum.Float => (4, true, false),
            GoTypeEnum.Double or GoTypeEnum.Int64 or GoTypeEnum.UInt64 => (8, false, true),
            _ => (4, false, false), // Int, UInt, String, Array, Slice, Struct, etc.
        };

        /// <summary>GoTypeEnum → isLong 判断 (Int64/UInt64 用 double 路径，不需要 isLong)</summary>
        private static bool IsLongType(GoTypeEnum t) => false;

        /// <summary>
        /// Map Go type string to (byteSize, isFloat, isDouble) for pointer dereference.
        /// </summary>
        public static (int size, bool isFloat, bool isDouble) GoTypeInfoFromString(string typeName)
        {
            string t = typeName.Trim().ToLower();
            if (t.Contains("*")) t = t.Replace("*", "").Trim();
            return t switch
            {
                "bool" or "byte" or "uint8" or "int8" => (1, false, false),
                "int16" or "uint16" => (2, false, false),
                "float32" or "float" => (4, true, false),
                "float64" or "double" => (8, false, true),
                "int" or "int32" or "uint" or "uint32" or "rune" or "string"
                    or "slice" or "array" or "map" or "struct" or "interface" => (4, false, false),
                _ when t.StartsWith("int") || t.StartsWith("uint") => (8, false, false),
                _ => (4, false, false),
            };
        }

        /// <summary>从 ast.Types 初始化类型定义字典</summary>
        private void InitTypeDefs()
        {
            foreach (var td in ast.Types)
            {
                if (td.Type.Name == "struct")
                    _typeDefs[td.Name] = td.Type;
            }
        }

        /// <summary>计算 struct 类型的大小（字节）</summary>
        private int GetStructSize(GoType st)
        {
            int size = 0;
            if (st.Fields != null)
                foreach (var f in st.Fields)
                    size += GetTypeSize(GetGoTypeEnum(f.Type));
            return size;
        }

        /// <summary>获取 struct 字段的字节偏移</summary>
        private int GetFieldOffset(GoType st, string fieldName)
        {
            int offset = 0;
            if (st.Fields != null)
                foreach (var f in st.Fields)
                {
                    if (f.Names != null)
                        for (int i = 0; i < f.Names.Count; i++)
                        {
                            if (f.Names[i] == fieldName)
                                return offset;
                            offset += GetTypeSize(GetGoTypeEnum(f.Type));
                        }
                    else
                        offset += GetTypeSize(GetGoTypeEnum(f.Type));
                }
            return offset;
        }

        /// <summary>获取类型的大小（字节）</summary>
        private int GetTypeSize(GoTypeEnum t) => t switch
        {
            GoTypeEnum.Bool or GoTypeEnum.Byte => 1,
            GoTypeEnum.Rune => 2,
            GoTypeEnum.Int or GoTypeEnum.UInt or GoTypeEnum.Float => 4,
            GoTypeEnum.Int64 or GoTypeEnum.UInt64 or GoTypeEnum.Double => 8,
            GoTypeEnum.Struct => 4, // 指针
            _ => 4
        };

        /// <summary>查找变量对应的 struct 类型定义</summary>
        private GoType GetVarStructType(string varName)
        {
            if (_varTypes.TryGetValue(varName, out var te) && te == GoTypeEnum.Struct)
            {
                // 从变量声明中查找完整类型名
                foreach (var vd in ast.Variables)
                    if (vd.Names != null && vd.Names.Contains(varName) && vd.Type != null)
                        return _typeDefs.GetValueOrDefault(vd.Type.Name);
            }
            return null;
        }

        // ====== CLikeCodegen 抽象方法实现 ======
        protected override int GetTypeSizeByEnum(int typeEnum) => typeEnum switch
        {
            (int)GoTypeEnum.Bool or (int)GoTypeEnum.Byte => 1,
            (int)GoTypeEnum.Rune => 2,
            (int)GoTypeEnum.Int or (int)GoTypeEnum.UInt or (int)GoTypeEnum.Float => 4,
            (int)GoTypeEnum.Int64 or (int)GoTypeEnum.UInt64 or (int)GoTypeEnum.Double => 8,
            _ => 4
        };

        protected override bool IsFloatType(int typeEnum) => typeEnum switch
        {
            (int)GoTypeEnum.Float or (int)GoTypeEnum.Double => true,
            _ => false
        };

        protected override int GetDefaultType() => (int)GoTypeEnum.Int;
        // ====== ======

        private string NewLabel()
        {
            return $"L{labelCounter++}";
        }

        #region 数据类型敏感指令选择方法

        /// <summary>
        /// 根据Go类型名称获取GoTypeEnum
        /// </summary>
        private GoTypeEnum GetGoTypeEnum(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return GoTypeEnum.Int; // 默认类型

            return typeName.ToLower() switch
            {
                "int" or "int32" => GoTypeEnum.Int,
                "int64" => GoTypeEnum.Int64,
                "uint" or "uint32" => GoTypeEnum.UInt,
                "uint64" => GoTypeEnum.UInt64,
                "float32" => GoTypeEnum.Float,
                "float64" => GoTypeEnum.Double,
                "bool" => GoTypeEnum.Bool,
                "byte" => GoTypeEnum.Byte,
                "rune" => GoTypeEnum.Rune,
                "string" => GoTypeEnum.String,
                _ when typeName.StartsWith("[") => GoTypeEnum.Array,
                _ when typeName.StartsWith("[]") => GoTypeEnum.Slice,
                "map" => GoTypeEnum.Map,
                "chan" => GoTypeEnum.Chan,
                _ when _typeDefs.ContainsKey(typeName) => GoTypeEnum.Struct,
                _ => GoTypeEnum.Object // 默认对象类型
            };
        }

        /// <summary>
        /// 根据GoType对象获取GoTypeEnum
        /// </summary>
        private GoTypeEnum GetGoTypeEnum(GoType goType)
        {
            if (goType == null)
                return GoTypeEnum.Int;

            return GetGoTypeEnum(goType.Name);
        }

        private OpCode GetLoadInstruction(GoTypeEnum type)
        {
            var (s, f, d) = TypeInfo(type);
            return ExpressionManager.SelectLoadOp(s, f, d, IsLongType(type));
        }

        private OpCode GetStoreInstruction(GoTypeEnum type)
        {
            var (s, f, d) = TypeInfo(type);
            return ExpressionManager.SelectStoreUnifiedOp(s, f, d, IsLongType(type));
        }

        private OpCode GetMoveInstruction(GoTypeEnum type)
        {
            var (s, f, d) = TypeInfo(type);
            return ExpressionManager.SelectMoveOp(s, f, d, IsLongType(type));
        }

        private OpCode GetPushInstruction(GoTypeEnum type)
        {
            var (s, f, d) = TypeInfo(type);
            return ExpressionManager.SelectPushOp(s, f, d, IsLongType(type));
        }

        private OpCode GetPopInstruction(GoTypeEnum type)
        {
            var (s, f, d) = TypeInfo(type);
            return ExpressionManager.SelectPopOp(s, f, d, IsLongType(type));
        }

        /// <summary>
        /// 生成类型转换指令 - 将R0中的值从源类型转换为目标类型
        /// </summary>
        private void GenerateTypeConversion(GoTypeEnum fromType, GoTypeEnum toType)
        {
            if (fromType == toType) return;
            var (fs, ff, fd) = TypeInfo(fromType);
            var (ts, tf, td) = TypeInfo(toType);
            _expr!.EmitConversion(fs, ff, fd, ts, tf, td, IsLongType(fromType), IsLongType(toType));
        }

        /// <summary>
        /// 生成带类型转换的表达式
        /// </summary>
        private void GenerateExpressionWithType(ASTNode expr, GoTypeEnum targetType)
        {
            GoTypeEnum sourceType = InferExpressionType(expr);
            GenerateExpression(expr);
            GenerateTypeConversion(sourceType, targetType);
        }

        /// <summary>
        /// 根据运算符和类型返回正确的算术运算指令
        /// </summary>
        private OpCode GetArithmeticInstruction(string op, GoTypeEnum type)
        {
            var (_, f, d) = TypeInfo(type);
            return ExpressionManager.SelectArithmeticOp(op, f, d, IsLongType(type));
        }

        private OpCode GetCompareInstruction(GoTypeEnum type)
        {
            var (_, f, d) = TypeInfo(type);
            return ExpressionManager.SelectCompareOp(f, d, IsLongType(type));
        }

        /// <summary>
        /// 推断表达式的类型
        /// </summary>
        private GoTypeEnum InferExpressionType(ASTNode expr)
        {
            if (expr == null)
                return GoTypeEnum.Int;

            if (expr is NumberLiteral numLit)
            {
                return numLit.IsFloat ? GoTypeEnum.Float : GoTypeEnum.Int;
            }
            else if (expr is BoolLiteral)
            {
                return GoTypeEnum.Bool;
            }
            else if (expr is StringLiteral)
            {
                return GoTypeEnum.String;
            }
            else if (expr is Identifier ident)
            {
                // 查找变量类型
                if (_varTypes.ContainsKey(ident.Name))
                    return _varTypes[ident.Name];
                else
                    return GoTypeEnum.Int; // 默认类型
            }
            else if (expr is BinaryOp binary)
            {
                // 推断二元操作的类型
                var leftType = InferExpressionType(binary.Left);
                var rightType = InferExpressionType(binary.Right);

                // 如果两边类型不同，返回更通用的类型
                if (leftType == GoTypeEnum.Double || rightType == GoTypeEnum.Double)
                    return GoTypeEnum.Double;
                else if (leftType == GoTypeEnum.Float || rightType == GoTypeEnum.Float)
                    return GoTypeEnum.Float;
                else if (leftType == GoTypeEnum.Int64 || rightType == GoTypeEnum.Int64
                      || leftType == GoTypeEnum.UInt64 || rightType == GoTypeEnum.UInt64)
                    return GoTypeEnum.Int64;
                else
                    return GoTypeEnum.Int;
            }
            else if (expr is TypeConversion conv)
            {
                // 类型转换 — 返回目标类型
                return GetGoTypeEnum(conv.Type);
            }
            else if (expr is UnaryOp unary)
            {
                // 一元操作的类型与操作数相同
                return InferExpressionType(unary.Operand);
            }

            return GoTypeEnum.Int; // 默认类型
        }

        #endregion

        public override VmlProgram GenerateCode()
        {
            // 处理全局变量
            foreach (var varDecl in ast.Variables)
            {
                GenerateGlobalVariable(varDecl);
            }

            // 处理常量
            foreach (var constDecl in ast.Constants)
            {
                GenerateGlobalConstant(constDecl);
            }

            // 处理函数
            foreach (var function in ast.Functions)
            {
                GenerateFunction(function);
            }

            GeneratePendingFuncLiterals();

            return BuildProgram("main");
        }
    }
}
