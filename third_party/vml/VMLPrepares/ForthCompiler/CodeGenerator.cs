using VMLAssembler;
using CompilerBase;

namespace ForthCompiler
{
    /// <summary>
    /// Forth语言数据类型枚举
    /// </summary>
    public enum ForthTypeEnum
    {
        Cell,       // 单元（默认整数类型，通常是32位）
        Byte,       // 字节（8位）
        Word,       // 字（16位）
        Double,     // 双单元（64位整数）
        Float,      // 单精度浮点（32位）
        DoubleFloat, // 双精度浮点（64位）
        Char,       // 字符
        String,     // 字符串
        Address,    // 地址/指针
        Boolean     // 布尔值
    }

    /// <summary>
    /// Forth语言代码生成器（基础版本）
    /// </summary>
    public partial class CodeGenerator : TypedCodeGen<ForthTypeEnum>
    {
        private Program ast;
        private int stackPointer; // 模拟数据栈指针
        private readonly Stack<string> catchLabels = new();
        private readonly Dictionary<string, ASTNode> constantValues = new();
        
        // 变量类型跟踪字典
        private readonly Dictionary<string, ForthTypeEnum> _varTypes = new();

        public CodeGenerator(Program ast) : base()
        {
            this.ast = ast;
            stackPointer = 0;
            InitSimpleCompiler();
        }

        private string NewLabel()
        {
            // `L_` 前缀不能省：`L0`–`L7` 会被汇编器当成**长整数寄存器**，跳转静默失效（v0.96.192）
            return $"L_{labelCounter++}";
        }

        private static string MangleName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";
            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }
            string result = new string(chars);
            if (!char.IsLetter(result[0]) && result[0] != '_')
                result = "_" + result;
            return result;
        }

        #region TypedCodeGen — 类型信息映射

        /// <summary>
        /// ForthTypeEnum → (byteSize, isFloat, isDouble, isLong)
        /// 修复: Double=64位整数使用 isLong
        /// </summary>
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(ForthTypeEnum type) => type switch
        {
            ForthTypeEnum.Byte => (1, false, false, false),
            ForthTypeEnum.Word => (2, false, false, false),
            ForthTypeEnum.Cell => (4, false, false, false),
            ForthTypeEnum.Double => (8, false, false, true),  // 64-bit int → isLong
            ForthTypeEnum.Float => (4, true, false, false),
            ForthTypeEnum.DoubleFloat => (8, false, true, false),
            ForthTypeEnum.Char => (1, false, false, false),
            ForthTypeEnum.String => (4, false, false, false),
            ForthTypeEnum.Address => (4, false, false, false),
            ForthTypeEnum.Boolean => (4, false, false, false),
            _ => (4, false, false, false)
        };

        // GetLoadInstruction/GetStoreInstruction/GetMoveInstruction/GetPushInstruction/GetPopInstruction
        // GetArithmeticInstruction/GetCompareInstruction — 全部由 TypedCodeGen<ForthTypeEnum> 提供

        /// <summary>
        /// 推断表达式的类型
        /// </summary>
        private ForthTypeEnum InferExpressionType(object expr)
        {
            if (expr is NumberLiteral num && num.IsFloat)
                return ForthTypeEnum.Float;
            return ForthTypeEnum.Cell;
        }

        /// <summary>
        /// 生成类型转换指令 — 委托到 TypedCodeGen.GetConversionInstruction
        /// </summary>
        private void GenerateTypeConversion(ForthTypeEnum fromType, ForthTypeEnum toType)
            => EmitTypeConversion(fromType, toType);

        /// <summary>
        /// 生成带目标类型的表达式代码
        /// </summary>
        private void GenerateExpressionWithType(object expr, ForthTypeEnum targetType)
        {
            if (expr is ASTNode node)
            {
                GenerateStatement(node);
                ForthTypeEnum exprType = InferExpressionType(expr);
                GenerateTypeConversion(exprType, targetType);
            }
        }

        #endregion

        public override VmlProgram GenerateCode()
        {
            string mainBodyLabel = NewLabel();

            // 程序入口：跳转到主程序体，跳过词定义
            labels["main"] = 0;
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> 
                { new Operand(OperandType.LABEL, "main") }, 0, "main"));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                { new Operand(OperandType.LABEL, mainBodyLabel) }, instructions.Count));

            // 首先处理所有词定义
            foreach (var stmt in ast.Statements)
            {
                if (stmt is WordDefinition)
                {
                    GenerateStatement(stmt);
                }
            }
            
            // 主程序体
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> 
                { new Operand(OperandType.LABEL, mainBodyLabel) }, instructions.Count, mainBodyLabel));
            
            // 生成栈帧 (使用基类方法)
            EmitPrologue();

            // 处理非词定义语句（包括MAIN调用）
            bool hasCallToMain = false;
            foreach (var stmt in ast.Statements)
            {
                if (!(stmt is WordDefinition))
                {
                    GenerateStatement(stmt);
                }
                else
                {
                    hasCallToMain = true;
                }
            }

            // 如果有词定义且末尾无显式调用，自动调用最后定义入口词
            if (hasCallToMain && ast.Statements.Count > 0)
            {
                // 仅当最后一个语句是词定义时才自动调用（避免与显式 WordCall 重复）
                var lastStmt = ast.Statements[ast.Statements.Count - 1];
                if (lastStmt is WordDefinition lastWord)
                {
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
                        { new Operand(OperandType.LABEL, $"word_{lastWord.Name}") }, instructions.Count));
                }
            }

            // 将栈顶值弹出到 R0 作为退出码
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            // 程序结束
            EmitExit();

            return BuildProgram("main");
        }

        private void GenerateStatement(ASTNode node)
        {
            // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
            // `> 0`：行号是 1-based，Line 没填的节点是 0，置成 0 会把上一句的行号冲掉。
            if (node.Line > 0) { CurrentSourceLine = node.Line; CurrentSourceColumn = node.Column; }
            if (node is WordDefinition wordDef)
            {
                GenerateWordDefinition(wordDef);
            }
            else if (node is VariableDefinition varDef)
            {
                GenerateVariableDefinition(varDef);
            }
            else if (node is CreateDefinition createDef)
            {
                GenerateCreateDefinition(createDef);
            }
            else if (node is ConstantDefinition constDef)
            {
                constantValues[constDef.Name] = constDef.Value;
                if (constDef.Value is NumberLiteral num)
                {
                    dataSection[$"const_{MangleName(constDef.Name)}"] = ParseNumberValue(num);
                }
                else if (constDef.Value is StringLiteral str)
                {
                    dataSection[$"const_{MangleName(constDef.Name)}"] = str.Value;
                }
                else if (constDef.Value is IOOperation io && io.Argument is StringLiteral dotStr)
                {
                    dataSection[$"const_{MangleName(constDef.Name)}"] = dotStr.Value;
                }
                else if (constDef.Value is CharLiteral ch)
                {
                    dataSection[$"const_{MangleName(constDef.Name)}"] = (int)ch.Value;
                }
            }
            else if (node is NumberLiteral numLit)
            {
                GenerateNumberLiteral(numLit);
            }
            else if (node is StringLiteral strLit)
            {
                GenerateStringLiteral(strLit);
            }
            else if (node is CharLiteral charLit)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)charLit.Value) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer++;
            }
            else if (node is WordCall wordCall)
            {
                GenerateWordCall(wordCall);
            }
            // ASM 已移除 — asm() 仅限 C/ObjC/C++ 语言，Forth 通过 Lib/shared/vmlsys.c 调用
            else if (node is IOOperation ioOp)
            {
                GenerateIOOperation(ioOp);
            }
            else if (node is StackOperation stackOp)
            {
                GenerateStackOperation(stackOp);
            }
            else if (node is ArithmeticOperation arithOp)
            {
                GenerateArithmeticOperation(arithOp);
            }
            else if (node is ComparisonOperation compOp)
            {
                GenerateComparisonOperation(compOp);
            }
            else if (node is LogicalOperation logicOp)
            {
                GenerateLogicalOperation(logicOp);
            }
            else if (node is MemoryOperation memOp)
            {
                GenerateMemoryOperation(memOp);
            }
            else if (node is IfStatement ifStmt)
            {
                GenerateIfStatement(ifStmt);
            }
            else if (node is LoopStatement loopStmt)
            {
                GenerateLoopStatement(loopStmt);
            }
            else if (node is CaseStatement caseStmt)
            {
                GenerateCaseStatement(caseStmt);
            }
            else if (node is ExitStatement exitStmt)
            {
                GenerateExitStatement(exitStmt);
            }
            else if (node is ExceptionOperation exceptionOp)
            {
                GenerateExceptionOperation(exceptionOp);
            }
            else if (node is VariableAccess varAccess)
            {
                GenerateVariableAccess(varAccess);
            }
            else if (node is ConstantAccess constAccess)
            {
                GenerateConstantAccess(constAccess);
            }
            else if (node is StackIndexOperation stackIdxOp)
            {
                GenerateStackIndexOperation(stackIdxOp);
            }
        }

    }
}
