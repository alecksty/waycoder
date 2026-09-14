using VMLAssembler;
using CompilerBase;
using System.Linq;

namespace LuaCompiler
{
    public partial class CodeGenerator : TypedCodeGen<LuaType>
    {
        // 符号表：变量名 -> 栈偏移
        private Dictionary<string, int> symbolTable;
        private Dictionary<string, LuaType> varTypes; // 变量名 -> 类型信息
        private int _nextMathLabel;
        private int nextStackOffset;
        /// <summary>
        /// 源代码所在目录（用于 dofile/loadfile 解析相对路径）
        /// </summary>
        public string SourceDirectory { get; set; } = ".";

        /// <summary>LuaType → (byteSize, isFloat, isDouble, isLong)</summary>
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(LuaType t) => t switch
        {
            LuaType.Boolean => (1, false, false, false),
            _ => (4, false, false, false),
        };

        public CodeGenerator() : base()
        {
            symbolTable = new Dictionary<string, int>();
            varTypes = new Dictionary<string, LuaType>();
            nextStackOffset = 0;
            InitSimpleCompiler();
        }

#pragma warning disable CS0809
        [System.Obsolete("Use GenerateCode(ProgramNode) instead", true)]
        public override VmlProgram GenerateCode() => throw new System.NotSupportedException("Use GenerateCode(ProgramNode) instead");
#pragma warning restore CS0809

        public VmlProgram GenerateCode(ProgramNode program)
        {
            // 添加主标签
            AddLabel("main");

            // 生成栈帧
            EmitPrologue();

            // 生成程序语句
            foreach (var stmt in program.Statements)
            {
                GenerateStatement(stmt);
            }
            // 保留最后一条语句的结果在 R0 中作为退出码，不自作主张加载全局变量
            EmitExit();

            return BuildProgram("main");
        }

        /// <summary>
        /// 根据类型字符串获取LuaType
        /// </summary>
        private LuaType GetLuaType(string typeStr)
        {
            return typeStr switch
            {
                "nil" => LuaType.Nil,
                "number" => LuaType.Number,
                "string" => LuaType.String,
                "boolean" => LuaType.Boolean,
                "table" => LuaType.Table,
                "function" => LuaType.Function,
                _ => LuaType.Object
            };
        }

        /// <summary>
        /// 根据常量值推断LuaType
        /// </summary>
        private LuaType GetLuaTypeFromValue(object value, string typeStr)
        {
            if (value == null) return LuaType.Nil;
            
            return typeStr switch
            {
                "number" => LuaType.Number,
                "string" => LuaType.String,
                "boolean" => LuaType.Boolean,
                _ => LuaType.Object
            };
        }

        /// <summary>
        /// 推断表达式的类型（简化实现）
        /// </summary>
        private LuaType InferExpressionType(ASTNode node)
        {
            if (node is ConstantNode constNode)
            {
                return GetLuaTypeFromValue(constNode.Value, constNode.Type);
            }
            else if (node is IdentifierNode identNode)
            {
                if (varTypes.TryGetValue(identNode.Name, out LuaType type))
                {
                    return type;
                }
                return LuaType.Number; // 默认数字类型
            }
            else if (node is BinaryOperationNode binOpNode)
            {
                if (binOpNode.Operator == TokenType.CONCAT)
                {
                    return LuaType.String;
                }
                // 对于二元运算，推断为操作数的类型
                LuaType leftType = InferExpressionType(binOpNode.Left);
                LuaType rightType = InferExpressionType(binOpNode.Right);
                
                // 如果有一个操作数是数字，结果就是数字
                if (leftType == LuaType.Number || rightType == LuaType.Number)
                {
                    return LuaType.Number;
                }
                return leftType; // 默认返回左操作数的类型
            }
            else if (node is TableConstructorNode)
            {
                return LuaType.Table;
            }
            else if (node is FunctionExpressionNode)
            {
                return LuaType.Function;
            }
            else if (node is TableAccessNode)
            {
                return LuaType.Object;
            }
            else if (node is FunctionCallNode)
            {
                return LuaType.Object;
            }
             
            return LuaType.Number; // 默认数字类型
        }
        
        /// <summary>
        /// 生成类型转换指令
        /// </summary>
        private void GenerateTypeConversion(LuaType fromType, LuaType toType)
        {
            if (fromType == toType) return;
            var (fs, ff, fd, fl) = GetTypeInfo(fromType);
            var (ts, tf, td, tl) = GetTypeInfo(toType);
            _expr!.EmitConversion(fs, ff, fd, ts, tf, td, fl, tl);
        }
        
        /// <summary>
        /// 生成带目标类型的表达式代码
        /// </summary>
        private void GenerateExpressionWithType(ASTNode node, LuaType targetType)
        {
            // 生成表达式值到R0
            GenerateExpression(node);
            
            // 推断表达式类型并进行类型转换
            LuaType exprType = InferExpressionType(node);
            GenerateTypeConversion(exprType, targetType);
        }
    }
}
