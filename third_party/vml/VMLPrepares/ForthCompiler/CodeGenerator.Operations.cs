using VMLAssembler;
using CompilerBase;

namespace ForthCompiler
{
    public partial class CodeGenerator
    {
        // ── 共享库输出函数调用（`print_str` / `print_int`）──────────────────────────
        //
        // **不要自己压栈。**（2026-09-17 调用约定统一后改的，此前这里有一句 `PUSH R0`。）
        //
        // 旧约定下 `Lib` 里这两个函数是 **被调方清栈**
        // （`… move R1 @13; add R13 #8; push R1; ret`），净效果是替调用方多弹 4 字节；
        // 而 **Forth 的数据栈就是 VML 的 R13**，那多出来的 4 字节正好吃掉栈顶的累加器
        // —— 实测 `." SKEL-SUM="` 之后的 `.` 会印出错值。
        // 当时的变通是调用方**多压一格**抵消（压栈 + 被调方弹掉 = 净 0）。
        //
        // 现在 `Lib/` 已按统一约定重生成：**被调方一律裸 `ret`、调用方清栈**，
        // 而且形参槽一律 4 字节。这里压一格就是**净 +4 的泄漏**（每次 `.` 吃一格数据栈），
        // 所以这一句必须去掉 —— 基类只发 `CALL print_str`（R0 承载实参）刚好是对的。
        private void EmitCallPrintString()
        {
            EmitPrintString();
        }

        private void EmitCallPrintInt()
        {
            EmitPrintInt();
        }

        private void GenerateIOOperation(IOOperation ioOp)
        {
            switch (ioOp.Operation)
            {
                case TokenType.DOTSTRING:
                    if (ioOp.Argument is StringLiteral strLit)
                    {
                        // 存储字符串
                        string label = $"msg_{dataSection.Count}";
                        dataSection[label] = strLit.Value;
                        
                        // 输出字符串
                        AddInstruction(OpCode.MOVE, new List<Operand>
                            { Reg(0), LabelOp(label) });
                        EmitCallPrintString(); // print_str（R0 承载实参，调用方不压栈）
                    }
                    break;

                case TokenType.DOT:
                    // 弹出栈顶并输出整数
                    AddInstruction(OpCode.POP, Reg(0));
                    EmitCallPrintInt(); // print_int（R0 承载实参，调用方不压栈）
                    break;

                case TokenType.FDOT:
                    AddInstruction(OpCode.FPOP, Reg(0));
                    AddSyscall(8); // SYSCALL 8: 输出浮点数
                    break;
                    
                case TokenType.CR:
                    // 输出换行
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)'\n') }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> 
                        { new Operand(OperandType.IMMEDIATE, 4) }, instructions.Count)); // SYSCALL 4: 输出字符
                    break;

                case TokenType.KEY:
                    // KEY: 读取一个字符，ASCII值压栈
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 5) }, instructions.Count)); // SYSCALL 5: 输入字符
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer++;
                    break;

                case TokenType.EMIT:
                    // EMIT: 弹出栈顶，输出为字符
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 4) }, instructions.Count)); // SYSCALL 4: 输出字符
                    break;

                case TokenType.TYPE:
                    // addr len TYPE：当前字符串均以0结尾，长度仅用于保持Forth栈效应
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // len
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // addr
                    stackPointer -= 2;
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
                    break;

                case TokenType.COUNT:
                    // addr COUNT -> addr len（对编译期字符串/常量主要由S"直接产生）
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer++;
                    break;

                case TokenType.SPACE:
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, (int)' ') }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 4) }, instructions.Count));
                    break;

                case TokenType.SPACES:
                    // n SPACES: 弹出n，输出n个空格
                    var spacesLoop = NewLabel();
                    var spacesEnd = NewLabel();
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, spacesLoop) }, instructions.Count, spacesLoop));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.JLE, new List<Operand>
                        { new Operand(OperandType.LABEL, spacesEnd) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, (int)' ') }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 4) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                        { new Operand(OperandType.LABEL, spacesLoop) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, spacesEnd) }, instructions.Count, spacesEnd));
                    break;
            }
        }

        private void GenerateStackOperation(StackOperation stackOp)
        {
            // 对于栈操作，推断栈顶元素类型
            ForthTypeEnum operandType = ForthTypeEnum.Cell; // 默认Cell
            // 尝试从栈操作上下文推断类型
            OpCode popOp = GetPopInstruction(operandType);
            OpCode pushOp = GetPushInstruction(operandType);
            
            switch (stackOp.Operation)
            {
                case TokenType.DUP:
                    // 复制栈顶
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;
                    
                case TokenType.DROP:
                    // 丢弃栈顶
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    break;
                    
                case TokenType.SWAP:
                    // 交换栈顶两个元素
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    break;

                case TokenType.OVER:
                    // (a b -- a b a)
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    stackPointer++;
                    break;

                case TokenType.ROT:
                    // (a b c -- b c a)
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 2) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 2) }, instructions.Count));
                    break;

                case TokenType.QDUP:
                    // (n -- n n) if n != 0, (0 -- 0) if n == 0
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    string qdupEnd = NewLabel();
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.JE, new List<Operand>
                        { new Operand(OperandType.LABEL, qdupEnd) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, qdupEnd) }, instructions.Count, qdupEnd));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.DUP2:
                    // (a b -- a b a b)
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer += 2;
                    break;

                case TokenType.DROP2:
                    // (a b -- )
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer -= 2;
                    break;

                case TokenType.DEPTH:
                    // ( -- n ) push current stack depth
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, stackPointer) }, instructions.Count));
                    stackPointer++;
                    break;
            }
        }

        private void GenerateStackIndexOperation(StackIndexOperation stackIdxOp)
        {
            // PICK ( n -- val ): 复制栈上第n个元素。0 PICK = DUP。
            // 弹出索引n，计算 BP + stackPointer - n 地址，LOAD值再PUSH。
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer--;

            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 12) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, stackPointer) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer++;
        }

        private void GenerateArithmeticOperation(ArithmeticOperation arithOp)
        {
            // 对于Forth，我们假设操作数类型为Cell（默认整数类型）
            // 在实际实现中，需要更复杂的类型推断系统
            ForthTypeEnum operandType = ForthTypeEnum.Cell;
            OpCode popOp = GetPopInstruction(operandType);
            OpCode pushOp = GetPushInstruction(operandType);
            
            // 根据操作符获取算术指令
            string opStr = arithOp.Operation switch
            {
                TokenType.PLUS => "+",
                TokenType.MINUS => "-",
                TokenType.MULTIPLY => "*",
                TokenType.DIVIDE => "/",
                TokenType.MOD => "MOD",
                _ => "+"
            };
            OpCode arithmeticOp = GetArithmeticInstruction(opStr, operandType);
            
            switch (arithOp.Operation)
            {
                case TokenType.MOD:
                    // 取模
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // b
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // a
                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    break;

                case TokenType.PLUS:
                    // 加法: a b + -> 弹出b和a，计算a+b，结果压栈
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // b
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // a
                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--; // 两个弹出，一个压入，净减少1
                    break;
                    
                case TokenType.MULTIPLY:
                    // 乘法
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    break;
                    
                case TokenType.MINUS:
                    // 减法: a b - -> 弹出b和a，计算a-b，结果压栈
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // b
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // a
                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    break;
                    
                case TokenType.DIVIDE:
                    // 除法: a b / -> 弹出b和a，计算a/b，结果压栈
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // b
                    instructions.Add(new Instruction(popOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // a
                    instructions.Add(new Instruction(arithmeticOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    break;

                case TokenType.INCREMENT:
                    // 1+: 弹出栈顶, +1, 压回
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.DECREMENT:
                    // 1-: 弹出栈顶, -1, 压回
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.DOUBLE:
                    // 2*: 弹出栈顶, *2, 压回
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.HALF:
                    // 2/: 弹出栈顶, /2, 压回
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.DIV, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2) }, instructions.Count));
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.DIVMOD:
                    // /MOD (a b -- rem quot): a/b的商和余数
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // b
                    instructions.Add(new Instruction(popOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // a
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // save a
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // save b
                    instructions.Add(new Instruction(OpCode.DIV, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) }, instructions.Count)); // R0 = a/b
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // push quotient
                    instructions.Add(new Instruction(OpCode.MOD, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) }, instructions.Count)); // R0 = a%b
                    instructions.Add(new Instruction(pushOp, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // push remainder
                    break;
            }
        }

        private void GenerateComparisonOperation(ComparisonOperation compOp)
        {
            // 对于Forth，我们假设操作数类型为Cell（默认整数类型）
            ForthTypeEnum operandType = ForthTypeEnum.Cell;
            OpCode popOp = GetPopInstruction(operandType);
            OpCode pushOp = GetPushInstruction(operandType);
            OpCode cmpOp = GetCompareInstruction(operandType);
            OpCode moveOp = GetMoveInstruction(ForthTypeEnum.Boolean); // 比较结果是布尔值
            
            string trueLabel = NewLabel();
            string endLabel = NewLabel();

            // 比较操作: 弹出操作数，比较，结果1或0压栈
            if (compOp.Operation is TokenType.ZEROEQUAL or TokenType.ZERONOTEQUAL
                or TokenType.ZEROLESSTHAN or TokenType.ZEROGREATERTHAN)
            {
                // Zero-comparisons: single operand, compare stack top with 0
                instructions.Add(new Instruction(popOp, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                var zeroJmpOp = compOp.Operation switch
                {
                    TokenType.ZEROEQUAL => OpCode.JE,
                    TokenType.ZERONOTEQUAL => OpCode.JNE,
                    TokenType.ZEROLESSTHAN => OpCode.JL,
                    TokenType.ZEROGREATERTHAN => OpCode.JG,
                    _ => OpCode.JE
                };
                instructions.Add(new Instruction(zeroJmpOp, new List<Operand>
                    { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                stackPointer--;
            }
            else
            {
                // 比较运算: a b > 弹出b和a，比较a>b
                instructions.Add(new Instruction(popOp, new List<Operand>
                    { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // b
                instructions.Add(new Instruction(popOp, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // a
                stackPointer -= 2;

                switch (compOp.Operation)
                {
                    case TokenType.EQUAL:
                        instructions.Add(new Instruction(cmpOp, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                        instructions.Add(new Instruction(OpCode.JE, new List<Operand>
                            { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                        break;
                    case TokenType.NOTEQUAL:
                        instructions.Add(new Instruction(cmpOp, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                        instructions.Add(new Instruction(OpCode.JNE, new List<Operand>
                            { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                        break;
                    case TokenType.LESSTHAN:
                        instructions.Add(new Instruction(cmpOp, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                        instructions.Add(new Instruction(OpCode.JL, new List<Operand>
                            { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                        break;
                    case TokenType.GREATERTHAN:
                        instructions.Add(new Instruction(cmpOp, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                        instructions.Add(new Instruction(OpCode.JG, new List<Operand>
                            { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                        break;
                    case TokenType.LESSEQUAL:
                        instructions.Add(new Instruction(cmpOp, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                        instructions.Add(new Instruction(OpCode.JLE, new List<Operand>
                            { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                        break;
                    case TokenType.GREATEREQUAL:
                        instructions.Add(new Instruction(cmpOp, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                        instructions.Add(new Instruction(OpCode.JGE, new List<Operand>
                            { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count));
                        break;
                    default:
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                            { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));
                        break;
                }
            }

            // 结果: 假=0
            instructions.Add(new Instruction(moveOp, new List<Operand>
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));

            // 真=-1 (Forth standard: TRUE = all bits set)
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                { new Operand(OperandType.LABEL, trueLabel) }, instructions.Count, trueLabel));
            instructions.Add(new Instruction(moveOp, new List<Operand>
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -1) }, instructions.Count));

            // 结果压栈
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                { new Operand(OperandType.LABEL, endLabel) }, instructions.Count, endLabel));
            instructions.Add(new Instruction(pushOp, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer++; // 弹出(1或2)个，压入1个
        }

        private void GenerateLogicalOperation(LogicalOperation logicOp)
        {
            if (logicOp.Operation == TokenType.NOT || logicOp.Operation == TokenType.INVERT)
            {
                instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.NOT, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                return;
            }

            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            OpCode op = logicOp.Operation switch
            {
                TokenType.OR => OpCode.OR,
                TokenType.XOR => OpCode.XOR,
                _ => OpCode.AND
            };
            instructions.Add(new Instruction(op, new List<Operand>
                { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer--;
        }

        private void GenerateMemoryOperation(MemoryOperation memOp)
        {
            switch (memOp.Operation)
            {
                case TokenType.STORE: // ! (存储)
                    // Forth: value addr ! -> stack [value, addr] with addr on top
                    // POP top = addr, POP next = value
                    // STORE src=R0(value), dest=R1(addr) -> store value at memory[addr]
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // addr
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // value
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { Mem("R1"), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer -= 2; // 弹出两个值，没有压入
                    break;
                    
                case TokenType.FETCH: // @ (读取)
                    // addr @ : 弹出addr，读取addr处的值，压栈
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // addr
                    // ⚠ `MOVE R0, R0` 是**自赋值**、空操作 —— 压回栈的是**地址**而不是元素值。
                    //   实测：`CREATE a 16 ALLOT  1 a !  a @` 得到 65536（= 0x10000，就是块地址）。
                    //   `MOVE dest, src` 是 **dest 在前** —— 别写反（同族问题本仓库栽过多次，
                    //   见 patches/0015-go-arrays.patch）。写入侧 `MOVE [R1], R0` 方向本来就是对的。
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), Mem("R0") }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> 
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--; // 弹出一个值，压入一个值，净减少0
                    break;

                case TokenType.CSTORE:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand>
                        { Mem("R1"), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer -= 2;
                    break;

                case TokenType.CFETCH:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    // ⚠ 同 `@`：原本是 `MOVEB R0, R0`（自赋值），压回栈的是地址不是字节值。
                    instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), Mem("R0") }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.HERE:
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4096) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer++;
                    break;

                case TokenType.ALLOC:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.FREE:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 41) }, instructions.Count));
                    stackPointer--;
                    break;

                case TokenType.ALLOT:
                    // ALLOT ( n -- a-addr ) — allocate n bytes, push address
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        { new Operand(OperandType.IMMEDIATE, 40) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.FLOAD:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0") }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.FPUSH, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    break;

                case TokenType.FSTORE:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count)); // addr
                    instructions.Add(new Instruction(OpCode.FPOP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count)); // value
                    instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                        { new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    stackPointer -= 2;
                    break;
                    
                default:
                    // 其他内存操作暂不支持
                    break;
            }
        }
        
        private void GenerateIfStatement(IfStatement ifStmt)
        {
            // Forth的IF语句：栈顶为条件（0为假，非0为真） — POP 作为条件发射器
            Sta.EmitIf(
                () =>
                {
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)]));
                    stackPointer--;
                },
                () => { foreach (var stmt in ifStmt.ThenBranch) GenerateStatement(stmt); },
                ifStmt.ElseBranch != null && ifStmt.ElseBranch.Count > 0
                    ? () => { foreach (var stmt in ifStmt.ElseBranch) GenerateStatement(stmt); }
                    : null);
        }
        
        private void GenerateLoopStatement(LoopStatement loopStmt)
        {
            switch (loopStmt.LoopType)
            {
                case TokenType.BEGIN:
                case TokenType.UNTIL:
                    // BEGIN...UNTIL 循环 (条件为真时退出)
                    string untilStart = NewLabel();
                    string untilEnd = NewLabel();

                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, untilStart) }, instructions.Count, untilStart));

                    Sta.PushLoopLabels(untilEnd, null);
                    foreach (var stmt in loopStmt.Body)
                    {
                        GenerateStatement(stmt);
                    }
                    Sta.PopLoopLabels();

                    // 生成条件（应该是一个表达式，生成到栈顶）
                    foreach (var condStmt in loopStmt.Condition)
                    {
                        GenerateStatement(condStmt);
                    }

                    // 弹出条件值
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;

                    // 条件为假时跳回循环开始 (UNTIL在条件为真时退出)
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.JE, new List<Operand>
                        { new Operand(OperandType.LABEL, untilStart) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, untilEnd) }, instructions.Count, untilEnd));
                    break;

                case TokenType.WHILE:
                    // BEGIN ... WHILE ... REPEAT
                    // Body contains the WHILE condition, Condition contains the REPEAT body
                    string whileBegin = NewLabel();
                    string whileEnd = NewLabel();
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, whileBegin) }, instructions.Count, whileBegin));
                    Sta.PushLoopLabels(whileEnd, null);
                    // Generate condition (WHILE part, in Body)
                    foreach (var stmt in loopStmt.Body)
                        GenerateStatement(stmt);
                    Sta.PopLoopLabels();
                    // Pop condition and check
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.JE, new List<Operand>
                        { new Operand(OperandType.LABEL, whileEnd) }, instructions.Count));
                    // Generate REPEAT body (in Condition)
                    foreach (var stmt in loopStmt.Condition)
                        GenerateStatement(stmt);
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                        { new Operand(OperandType.LABEL, whileBegin) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, whileEnd) }, instructions.Count, whileEnd));
                    break;

                case TokenType.AGAIN:
                    // BEGIN...AGAIN 无条件循环 (LEAVE 退出)
                    string againStart = NewLabel();
                    string againEnd = NewLabel();
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, againStart) }, instructions.Count, againStart));
                    Sta.PushLoopLabels(againEnd, null);
                    foreach (var stmt in loopStmt.Body)
                    {
                        GenerateStatement(stmt);
                    }
                    Sta.PopLoopLabels();
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                        { new Operand(OperandType.LABEL, againStart) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, againEnd) }, instructions.Count, againEnd));
                    break;
                    
                case TokenType.PLUSLOOP:
                case TokenType.DO:
                    string doStartLabel = NewLabel();
                    string doEndLabel = NewLabel();

                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                    stackPointer -= 2;

                    // 用 R2 作循环计数器，R3 存限制值（避免循环体内算术操作覆盖 R1）
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1) }, instructions.Count));

                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, doStartLabel) }, instructions.Count, doStartLabel));

                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.JGE, new List<Operand>
                        { new Operand(OperandType.LABEL, doEndLabel) }, instructions.Count));

                    Sta.PushLoopLabels(doEndLabel, null);
                    foreach (var stmt in loopStmt.Body)
                    {
                        GenerateStatement(stmt);
                    }
                    Sta.PopLoopLabels();

                    // Increment loop counter: +LOOP pops increment from stack, LOOP adds #1
                    if (loopStmt.LoopType == TokenType.PLUSLOOP)
                    {
                        instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                        stackPointer--;
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                            { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    }
                    else
                    {
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                            { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }, instructions.Count));
                    }

                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                        { new Operand(OperandType.LABEL, doStartLabel) }, instructions.Count));

                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, doEndLabel) }, instructions.Count, doEndLabel));
                    break;

                case TokenType.LEAVE:
                    // LEAVE: 立即退出当前循环 (DO-LOOP/BEGIN-UNTIL/BEGIN-WHILE/BEGIN-AGAIN)
                    Sta.EmitBreak();
                    break;
                    
                default:
                    // 其他循环类型暂不支持
                    break;
            }
        }

        private void GenerateCaseStatement(CaseStatement caseStmt)
        {
            // CASE: selector is on top of eval stack (R13)
            // Each branch: execute value expr (pushes V), compare with selector, if match execute body
            // Default: DROP selector, execute default body

            string endcaseLabel = NewLabel();
            Sta!.PushLoopLabels(endcaseLabel, null);
            var nextLabels = new List<string>();

            foreach (var branch in caseStmt.Branches)
            {
                string nextLabel = NewLabel();
                nextLabels.Add(nextLabel);

                // Execute value expression — pushes V onto stack: [..., N, V]
                foreach (var valStmt in branch.ValueExpr)
                    GenerateStatement(valStmt);

                // POP V, POP selector → CMP, PUSH selector back for fallback
                instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                stackPointer--;
                instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer--;
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer++;
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }, instructions.Count));
                instructions.Add(new Instruction(OpCode.JNE, new List<Operand>
                    { new Operand(OperandType.LABEL, nextLabel) }, instructions.Count));

                // Match: consume N from stack
                instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                    { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                stackPointer--;

                // Execute body
                foreach (var bodyStmt in branch.Body)
                    GenerateStatement(bodyStmt);

                // Jump to end
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                    { new Operand(OperandType.LABEL, endcaseLabel) }, instructions.Count));

                // Next branch label
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                    { new Operand(OperandType.LABEL, nextLabel) }, instructions.Count, nextLabel));
            }

            // Default: consume N, execute default body
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
            stackPointer--;
            foreach (var stmt in caseStmt.DefaultBody)
                GenerateStatement(stmt);

            // ENDCASE label
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                { new Operand(OperandType.LABEL, endcaseLabel) }, instructions.Count, endcaseLabel));
            Sta!.PopLoopLabels();
        }

        private void GenerateExitStatement(ExitStatement exitStmt)
        {
            // EXIT语句：从当前词定义返回
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>(), instructions.Count));
        }

        private void GenerateExceptionOperation(ExceptionOperation exceptionOp)
        {
            switch (exceptionOp.Operation)
            {
                case TokenType.CATCH:
                    string catchLabel = NewLabel();
                    catchLabels.Push(catchLabel);
                    instructions.Add(new Instruction(OpCode.CATCH, new List<Operand>
                        { new Operand(OperandType.LABEL, catchLabel) }, instructions.Count));
                    break;

                case TokenType.THROW:
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.THROW, new List<Operand>
                        { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                    stackPointer--;
                    break;

                case TokenType.ENDCATCH:
                    string endLabel = NewLabel();
                    instructions.Add(new Instruction(OpCode.ENDCATCH, new List<Operand>(), instructions.Count));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                        { new Operand(OperandType.LABEL, endLabel) }, instructions.Count));
                    if (catchLabels.Count > 0)
                    {
                        string handlerLabel = catchLabels.Pop();
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                            { new Operand(OperandType.LABEL, handlerLabel) }, instructions.Count, handlerLabel));
                        instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                            { new Operand(OperandType.REGISTER, 0) }, instructions.Count));
                        stackPointer++;
                    }
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand>
                        { new Operand(OperandType.LABEL, endLabel) }, instructions.Count, endLabel));
                    break;
            }
        }
    }
}
