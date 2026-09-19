using VMLAssembler;
using System.Collections.Generic;
using CompilerBase;

namespace JavaCompiler
{
    /// <summary>
    /// Java语言数据类型枚举
    /// </summary>
    public enum JavaTypeEnum
    {
        Boolean,    // boolean
        Byte,       // byte
        Short,      // short
        Char,       // char
        Int,        // int
        Long,       // long
        Float,      // float
        Double,     // double
        Object,     // Object
        String,     // String
        Array,      // 数组
        Void        // void
    }

    /// <summary>
    /// Java语言代码生成器
    /// </summary>
    public partial class CodeGenerator : OopCodeGenerator
    {
        private int _methodReturnLabelId = -1;
        private int _currentVarOffset;
        private new Dictionary<string, int> _varOffsets = new Dictionary<string, int>();
        private Dictionary<string, JavaTypeEnum> _varTypes = new Dictionary<string, JavaTypeEnum>();
        private Dictionary<string, (string breakLabel, string continueLabel)> _labeledLoops = new();
        internal HashSet<string> nativeMethods = new HashSet<string>();
        internal Dictionary<string, JavaTypeEnum> _methodReturnTypes = new Dictionary<string, JavaTypeEnum>();
        public CodeGenerator() : base()
        {
            InitSimpleCompiler(framePointerReg: 14);  // Java uses R14 as frame pointer
        }

        private Program? _program;

        public override VmlProgram GenerateCode()
        {
            if (_program == null) throw new System.InvalidOperationException("No AST program set");
            return Generate(_program);
        }
        
        #region 数据类型敏感指令选择方法

        /// <summary>
        /// 根据Java类型名称获取JavaTypeEnum
        /// </summary>
        private JavaTypeEnum GetJavaTypeEnum(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return JavaTypeEnum.Int; // 默认int类型

            string lowerType = typeName.ToLower();
            
            return lowerType switch
            {
                "boolean" => JavaTypeEnum.Boolean,
                "byte" => JavaTypeEnum.Byte,
                "short" => JavaTypeEnum.Short,
                "char" => JavaTypeEnum.Char,
                "int" => JavaTypeEnum.Int,
                "long" => JavaTypeEnum.Long,
                "float" => JavaTypeEnum.Float,
                "double" => JavaTypeEnum.Double,
                "string" => JavaTypeEnum.String,
                "void" => JavaTypeEnum.Void,
                _ when lowerType.EndsWith("[]") => JavaTypeEnum.Array,
                _ => JavaTypeEnum.Object // 默认对象类型
            };
        }

        // JavaTypeEnum -> (byteSize, isFloat, isDouble)
        // Long 用 double 路径 (isDouble=true) 因为 VML R0-R15 的 long 操作截断到 32 位
        private static int GetTypeSize(JavaTypeEnum t) => t switch
        {
            JavaTypeEnum.Boolean or JavaTypeEnum.Byte => 1,
            JavaTypeEnum.Short or JavaTypeEnum.Char => 2,
            JavaTypeEnum.Double or JavaTypeEnum.Long => 8,
            _ => 4,
        };

        private static (int size, bool isFloat, bool isDouble, bool isLong) JavaTypeInfo(JavaTypeEnum t) => t switch
        {
            JavaTypeEnum.Boolean or JavaTypeEnum.Byte => (1, false, false, false),
            JavaTypeEnum.Short or JavaTypeEnum.Char => (2, false, false, false),
            JavaTypeEnum.Float => (4, true, false, false),
            JavaTypeEnum.Double => (8, false, true, false),
            JavaTypeEnum.Long => (8, false, false, true),  // 64 位整数用 long 路径 (MOVEL/ADDL)
            _ => (4, false, false, false),
        };

        private OpCode GetLoadInstruction(JavaTypeEnum type)
        { var (s, f, d, l) = JavaTypeInfo(type); return ExpressionManager.SelectLoadOp(s, f, d, l); }
        private OpCode GetStoreInstruction(JavaTypeEnum type)
        { var (s, f, d, l) = JavaTypeInfo(type); return ExpressionManager.SelectStoreUnifiedOp(s, f, d, l); }
        private OpCode GetMoveInstruction(JavaTypeEnum type)
        { var (s, f, d, l) = JavaTypeInfo(type); return ExpressionManager.SelectMoveOp(s, f, d, l); }
        private OpCode GetPushInstruction(JavaTypeEnum type)
        { var (s, f, d, l) = JavaTypeInfo(type); return ExpressionManager.SelectPushOp(s, f, d, l); }
        private OpCode GetPopInstruction(JavaTypeEnum type)
        { var (s, f, d, l) = JavaTypeInfo(type); return ExpressionManager.SelectPopOp(s, f, d, l); }

        private OpCode GetArithmeticInstruction(string op, JavaTypeEnum type)
        {
            var (_, f, d, l) = JavaTypeInfo(type);
            if (op is "+" or "-" or "*" or "/" or "%")
                return ExpressionManager.SelectArithmeticOp(op, f, d, l);
            return op switch { "&" => OpCode.AND, "|" => OpCode.OR, "^" => OpCode.XOR, _ => OpCode.ADD };
        }

        private OpCode GetCompareInstruction(JavaTypeEnum type)
        { var (_, f, d, l) = JavaTypeInfo(type); return ExpressionManager.SelectCompareOp(f, d, l); }

        #endregion

        /// <summary>
        /// 生成类型转换指令 - 将R0中的值从源类型转换为目标类型
        /// </summary>
        private void GenerateTypeConversion(JavaTypeEnum fromType, JavaTypeEnum toType)
        {
            if (fromType == toType) return;
            var (fs, ff, fd, fl) = JavaTypeInfo(fromType);
            var (ts, tf, td, tl) = JavaTypeInfo(toType);
            var op = ExpressionManager.SelectConversionOp(fs, ff, fd, ts, tf, td, fl, tl);
            if (op != null)
                AddRR(op.Value, 0, 0);
        }

        /// <summary>
        /// 生成带类型转换的表达式
        /// </summary>
        private void GenerateExpressionWithType(Expression expr, JavaTypeEnum targetType)
        {
            JavaTypeEnum sourceType = InferExpressionType(expr);
            GenerateExpression(expr);
            GenerateTypeConversion(sourceType, targetType);
        }

        public VmlProgram Generate(Program program)
        {
            _program = program;
            // 为每个类生成代码
            foreach (var classDecl in program.Classes)
            {
                GenerateClass(classDecl);
            }

            // 生成枚举常量
            foreach (var enumDecl in program.EnumDecls)
            {
                GenerateEnum(enumDecl);
            }
            
            // 如果没有main方法，添加默认的main方法
            if (!labels.ContainsKey("main"))
            {
                EmitDefaultMain(stackTop: 1048572, frameReg: 14, customBody: () => {
                    string msgLabel = $"msg_{instructions.Count}";
                    dataSection[msgLabel] = "Hello from Java!\n\0";
                    AddInstruction(OpCode.MOVE, Reg(0), LabelOp(msgLabel));
                    EmitPrintString();
                });
            }
            
            return BuildProgram("main");
        }
        
        private void GenerateClass(ClassDecl classDecl)
        {
            // 字段（含 `static int[] A = new int[10];` 这类）**登记进 dataSection**。
            //
            // ⚠ 解析器把字段收进了 `ClassDecl.Fields`，而这里只遍历构造函数与方法 ——
            //   字段**从来没被代码生成消费过**（`grep '\.Fields' CodeGenerator*.cs` 零命中）。
            //   此前之所以没暴露，是因为 `GenerateVariable` 有一条「查不到就假定是全局、
            //   现场建个 `var_x` 槽」的兜底 **把它蒙对了**（数组基址要的正是地址，
            //   而那条恰好发的是 `LABEL`）。现在那条兜底升级成硬报错，就必须先把
            //   「哪些名字是声明过的」补上，否则 `Examples/java/catch.java` 的 `A` 会被误报。
            //
            // 生成出来的代码**与登记之前逐字相同**：标签一直是 `var_<名字>`，
            // 这里只是把它的存在提前告诉符号表（值同样是 0）。
            foreach (var field in classDecl.Fields)
            {
                string fieldLabel = $"var_{field.Name}";
                if (!dataSection.ContainsKey(fieldLabel)) dataSection[fieldLabel] = 0;
            }

            // 为每个构造函数生成代码
            foreach (var ctor in classDecl.Constructors)
            {
                AddLabel(ctor.Name);
                EmitPrologue();
                // 如果有 super() 调用，先执行
                if (ctor.SuperCall != null)
                {
                    string superLabel = ctor.SuperCall.Arguments.Count > 0 && ctor.SuperCall.Arguments[0] is LiteralExpression
                        ? "super" : "super";
                    foreach (var arg in ctor.SuperCall.Arguments)
                    {
                        GenerateExpression(arg);
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    }
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, ctor.Name.Contains(".") ? ctor.Name.Substring(0, ctor.Name.IndexOf('.')) : ctor.Name)]));
                    if (ctor.SuperCall.Arguments.Count > 0)
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, ctor.SuperCall.Arguments.Count * 4)]));
                }
                GenerateBlock(ctor.Body);
                EmitEpilogue();
            }
            // 为每个方法生成代码（跳过抽象方法/接口方法，但 native 方法无 body 仍需注册）
            foreach (var method in classDecl.Methods)
            {
                // native 方法无 body 但仍需注册返回类型和 nativeMethods
                if (method.Body == null && !method.Modifiers.IsNative) continue;
                if (method.Name == "main")
                {
                    GenerateMainMethod(method);
                }
                else
                {
                    GenerateMethod(method);
                }
            }
        }
        
        private void GenerateMainMethod(MethodDecl method)
        {
            // 添加main标签
            AddLabel("main");

            // 设置帧指针和栈帧（SP由运行时设置）
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 12),
                new Operand(OperandType.REGISTER, 13)
            }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 14),
                new Operand(OperandType.REGISTER, 13)
            }));
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> {
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.IMMEDIATE, 256)
            }));

            // 设置返回标签，使 return 语句能正确跳转
            int savedReturnLabel = _methodReturnLabelId;
            _methodReturnLabelId = labelCounter++;
            string mainReturnLabel = $"return_{_methodReturnLabelId}";

            // 生成方法体
            GenerateBlock(method.Body);

            // 返回标签：main 的返回值即为程序退出码
            AddLabel(mainReturnLabel);
            EmitExit();

            _methodReturnLabelId = savedReturnLabel;
        }
        
        private void GenerateMethod(MethodDecl method)
        {
            // 记录方法返回类型（用于类型推断，native 和非 native 都记录）
            _methodReturnTypes[method.Name] = GetJavaTypeEnum(method.ReturnType);

            // native 方法：注册到 nativeMethods 集合，不生成函数体
            if (method.Modifiers.IsNative)
            {
                nativeMethods.Add(method.Name);
                return;
            }

            string methodLabel = $"method_{method.Name}";
            AddLabel(methodLabel);

            // 添加函数注释
            var headerParams = new List<(string, string)>();
            foreach (var p in method.Parameters) headerParams.Add((p.Type, p.Name));
            EmitMethodHeader(method.Name, method.ReturnType, headerParams);

            // 方法序言（CCv2）
            //
            // ⚠ 这里**手写**而不再复用基类的 EmitPrologue/EmitEpilogue：R14 是帧指针
            //   (`InitSimpleCompiler(framePointerReg: 14)` ⇒ 局部变量一律 `R14-offset` 寻址)，
            //   而基类那对只存 R15/R12，**不存 R14** —— 被调方一进来就 `MOVE R14, R13` 把
            //   它改成自己的帧，返回时又不还原 ⇒ 调用方此后**所有**局部变量读写整体错位。
            //   症状极具迷惑性：单次调用看不出问题，一旦在循环里反复调用（如
            //   `a[i] = plus1(a[i]); s = s + a[i]`），循环变量 / 累加器 / 数组指针会一起
            //   漂到别的内存上 —— 实测元素被写成 0、累加器变成无关数字。
            //   形状与 Swift 前端一致（CodeGenerator.cs 的 `PUSH R14; MOVE R14, R13`）。
            //
            //   顺序不可换：R14 必须是**最后一个压**（紧挨着帧基），尾声才能
            //   `MOVE R13, R14` 回到帧基、先 `POP R14` 再依次弹回 R12/R15、最后 RET
            //   （RET 弹的是 CALL 压的返回地址，它恰好在帧基之上）。
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 15)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 12)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 13)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 14), new Operand(OperandType.REGISTER, 13)]));

            // Save method parameters to stack frame (CCv2: first param in R0)
            int savedReturnLabel = _methodReturnLabelId;
            int savedVarOffset = _currentVarOffset;
            var savedVarOffsets = new Dictionary<string, int>(_varOffsets);
            _methodReturnLabelId = labelCounter++;
            _currentVarOffset = 0;
            _varOffsets.Clear();
            Vars.ResetLocals();
            int paramSpace = 0;
            for (int pi = 0; pi < method.Parameters.Count; pi++)
            {
                _currentVarOffset += 4;
                _varOffsets[method.Parameters[pi].Name] = _currentVarOffset;
                _varTypes[method.Parameters[pi].Name] = JavaTypeEnum.Int;
                Vars.AllocLocal(method.Parameters[pi].Name, 4);
                // ⚠ **每个形参都从栈上取**（2026-09-17 调用约定统一后改的）。
                //
                // 原先 `pi == 0` 走一个特例分支（直接从 R0 存进形参槽），其余才从栈取 ——
                // 那是「第 1 个实参放 R0」的 CCv2 寄存器约定。统一之后调用点把**全部**实参
                // 都右到左压栈，R0 里不再有特供的首参（R0 只在调用前被镜像层顺手写一次，
                // 是给 `Lib` 里那些内联汇编用的，不是传参通道）。
                //
                // 栈帧（本文件上面那段手写序言的布局）：
                //   [R14+0]  = 保存的调用方 R14
                //   [R14+4]  = 保存的 R12
                //   [R14+8]  = 保存的 R15
                //   [R14+12] = **CALL 压入的返回地址**
                //   [R14+16 + 4i] = 调用方从右到左压入的实参（第 1 个形参最后压、在最低地址）
                // ⚠ 比原来的 `12 +` 多 4 —— 多存的那个 R14 让帧基整体下移一格。
                //
                // 历史：这一句更早的形态是 `MOVE [R14+n], R0` —— **操作数写反**（`MOVE` 是
                // dest 在前），做的是 store 而不是 load ⇒ 除首参外每个形参都等于首参
                // （实测 `f(11,22,33)` 三个形参全是 11）。修的时候别只看一句"长得像 load"。
                int stackOff = 16 + pi * 4;
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, Vars.FormatOffset(stackOff))]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, Vars.FormatOffset(-_currentVarOffset)), new Operand(OperandType.REGISTER, 0)]));
                paramSpace += 4;
            }
            instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, Math.Max(64, paramSpace))]));
            string returnLabel = $"return_{_methodReturnLabelId}";
            GenerateBlock(method.Body);
            AddLabel(returnLabel);
            _currentVarOffset = savedVarOffset;
            _varOffsets.Clear();
            foreach (var kv in savedVarOffsets) _varOffsets[kv.Key] = kv.Value;

            // 尾声 —— 与上面的手写序言严格镜像（见那段注释）：
            //   MOVE R13, R12 (= 帧基, R12/R14 在进入时被设成同一个值) → POP R14 → POP R12 → POP R15 → RET
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 12)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 12)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 15)]));
            instructions.Add(new Instruction(OpCode.RET, []));
            _methodReturnLabelId = savedReturnLabel;
        }
        
        private void GenerateBlock(Block block)
        {
            foreach (var statement in block.Statements)
            {
                GenerateStatement(statement);
            }
        }
        
        private void GenerateStatement(Statement statement)
        {
            // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
            // 判据 `> 0`：行号是 1-based，没填的节点是 0，置 0 会把上一句的行号冲掉。
            if (statement.Line > 0) { CurrentSourceLine = statement.Line; CurrentSourceColumn = statement.Column; }
            if (statement is ExpressionStatement exprStmt)
            {
                GenerateExpression(exprStmt.Expression);
            }
            else if (statement is VariableDeclStatement varDecl)
            {
                JavaTypeEnum varType = GetJavaTypeEnum(varDecl.Type);
                _varTypes[varDecl.Name] = varType;
                int typeSize = GetTypeSize(varType);
                _currentVarOffset += typeSize;
                _varOffsets[varDecl.Name] = _currentVarOffset;
                Vars.AllocLocal(varDecl.Name, typeSize);

                if (varDecl.Initializer != null)
                {
                    GenerateExpression(varDecl.Initializer);
                    OpCode storeOp = GetStoreInstruction(varType);
                    instructions.Add(new Instruction(storeOp, [new Operand(OperandType.MEMORY, Vars.FormatOffset(-_currentVarOffset)), new Operand(OperandType.REGISTER, 0)]));
                }
            }
            else if (statement is ReturnStatement returnStmt)
            {
                if (returnStmt.Value != null)
                {
                    GenerateExpression(returnStmt.Value);
                }
                int rl = _methodReturnLabelId >= 0 ? _methodReturnLabelId : labelCounter - 1;
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, $"return_{rl}")]));
            }
            else if (statement is IfStatement ifStmt)
            {
                GenerateIfStatement(ifStmt);
            }
            else if (statement is WhileStatement whileStmt)
            {
                GenerateWhileStatement(whileStmt);
            }
            else if (statement is ForStatement forStmt)
            {
                GenerateForStatement(forStmt);
            }
            else if (statement is BlockStatement blockStmt)
            {
                GenerateBlock(blockStmt.Block);
            }
            else if (statement is DoWhileStatement doWhileStmt)
            {
                GenerateDoWhileStatement(doWhileStmt);
            }
            else if (statement is SwitchStatement switchStmt)
            {
                GenerateSwitchStatement(switchStmt);
            }
            else if (statement is BreakStatement breakStmt)
            {
                if (breakStmt.Label != null && _labeledLoops.TryGetValue(breakStmt.Label, out var loop))
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, loop.breakLabel)]));
                else
                    Sta!.EmitBreak();
            }
            else if (statement is ContinueStatement continueStmt)
            {
                if (continueStmt.Label != null && _labeledLoops.TryGetValue(continueStmt.Label, out var loop))
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, loop.continueLabel)]));
                else
                    Sta!.EmitContinue();
            }
            else if (statement is ForEachStatement foreachStmt)
            {
                GenerateForEachStatement(foreachStmt);
            }
            else if (statement is LabeledStatement labeledStmt)
            {
                int beforeCount = Sta!.LoopStackCount;
                GenerateStatement(labeledStmt.Statement);
                if (Sta.LoopStackCount > beforeCount)
                    _labeledLoops[labeledStmt.Label] = Sta.GetLoopLabels();
            }
            else if (statement is AssertStatement assertStmt)
            {
                // assert cond : msg — evaluate cond, if false, output msg and halt
                string assertEnd = $"assert_end_{labelCounter++}";
                GenerateExpression(assertStmt.Condition);
                instructions.Add(new Instruction(OpCode.JNZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, assertEnd)]));
                if (assertStmt.Message != null)
                {
                    GenerateExpression(assertStmt.Message);
                    EmitExit();
                }
                labels[assertEnd] = instructions.Count;
            }
            else if (statement is ThrowStatement throwStmt)
            {
                if (HandleMCUThrow(() => { if (throwStmt.Value != null) GenerateExpression(throwStmt.Value); }))
                    return;
                if (throwStmt.Value != null)
                    GenerateExpression(throwStmt.Value);
                else
                    AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.THROW, [new Operand(OperandType.REGISTER, 0)]));
            }
            else if (statement is TryStatement tryStmt)
            {
                if (HandleMCUTry(() => GenerateStatement(tryStmt.Body)))
                    return;

                string catchLabel = $"try_catch_{labelCounter++}";
                string endLabel = $"try_end_{labelCounter++}";

                instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
                GenerateStatement(tryStmt.Body);
                instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));

                labels[catchLabel] = instructions.Count;
                foreach (var cc in tryStmt.Catches)
                {
                    if (cc.VariableName != null)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE,
                            [new Operand(OperandType.LABEL, $"var_{cc.VariableName}"), new Operand(OperandType.REGISTER, 0)]));
                    }
                    if (cc.Body != null) GenerateStatement(cc.Body);
                    instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                }
                labels[endLabel] = instructions.Count;
            }
        }
        
        private void GenerateIfStatement(IfStatement ifStmt)
        {
            Sta!.EmitIf(
                () => GenerateExpression(ifStmt.Condition),
                () => GenerateStatement(ifStmt.ThenBranch),
                ifStmt.ElseBranch != null ? () => GenerateStatement(ifStmt.ElseBranch) : null);
        }
        
        private void GenerateWhileStatement(WhileStatement whileStmt)
        {
            Sta!.EmitWhile(
                () => GenerateExpression(whileStmt.Condition),
                () => GenerateStatement(whileStmt.Body));
        }

        private void GenerateForStatement(ForStatement forStmt)
        {
            Sta!.EmitFor(
                forStmt.Initializer != null ? () => GenerateStatement(forStmt.Initializer) : null,
                forStmt.Condition != null ? () => GenerateExpression(forStmt.Condition) : null,
                forStmt.Increment != null ? () => GenerateExpression(forStmt.Increment) : null,
                () => GenerateStatement(forStmt.Body));
        }

        private void GenerateDoWhileStatement(DoWhileStatement doStmt)
        {
            Sta!.EmitDoWhile(
                () => GenerateStatement(doStmt.Body),
                () => GenerateExpression(doStmt.Condition));
        }

        private void GenerateSwitchStatement(SwitchStatement switchStmt)
        {
            string endLabel = $"switch_end_{labelCounter++}";

            // break 作用域
            Sta!.PushLoopLabels(endLabel, null);

            // switch 值保存到 R1，栈保护
            GenerateExpression(switchStmt.Value);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));

            string defaultLabel = endLabel;
            foreach (var sc in switchStmt.Cases)
                if (sc.Value == null) defaultLabel = $"switch_def_{labelCounter}";

            int ci = 0;
            foreach (var sc in switchStmt.Cases)
            {
                string nextLabel = $"switch_nxt_{labelCounter}_{ci}";
                ci++;
                if (sc.Value == null) continue;

                // PUSH R1 保护 → 求值 case → POP R1 恢复 → 比较
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                GenerateExpression(sc.Value);
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.JNE, [new Operand(OperandType.LABEL, nextLabel)]));
                foreach (var stmt in sc.Body)
                    GenerateStatement(stmt);
                labels[nextLabel] = instructions.Count;
            }

            // 无匹配: JMP default
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, defaultLabel)]));

            if (defaultLabel != endLabel)
            {
                labels[defaultLabel] = instructions.Count;
                foreach (var sc in switchStmt.Cases)
                    if (sc.Value == null)
                        foreach (var stmt in sc.Body)
                            GenerateStatement(stmt);
            }
            labels[endLabel] = instructions.Count;
            Sta!.PopLoopLabels();
        }

        private void GenerateForEachStatement(ForEachStatement foreachStmt)
        {
            string startLabel = $"foreach_start_{labelCounter}";
            string endLabel = $"foreach_end_{labelCounter}";
            string arrLabel = $"__fe_arr_{labelCounter}";
            string idxLabel = $"__fe_idx_{labelCounter}";
            labelCounter++;
            dataSection[arrLabel] = 0;
            dataSection[idxLabel] = 0;

            Sta!.PushLoopLabels(endLabel, startLabel);

            GenerateExpression(foreachStmt.Collection);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, arrLabel), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), new Operand(OperandType.REGISTER, 0)]));

            labels[startLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R1")]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
            instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, endLabel)]));

            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));

            string varLabel = $"var_{foreachStmt.VariableName}";
            dataSection[varLabel] = 0;
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, varLabel), new Operand(OperandType.REGISTER, 0)]));

            GenerateStatement(foreachStmt.Body);

            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, startLabel)]));
            labels[endLabel] = instructions.Count;

            Sta!.PopLoopLabels();
        }

        private void GenerateConditionalExpression(ConditionalExpression condExpr)
        {
            _expr!.EmitConditional(WrapExpr(condExpr.Condition), WrapExpr(condExpr.TrueValue), WrapExpr(condExpr.FalseValue));
        }

        /// <summary>
        /// 把数组元素 `a[i]` 的**地址**算进 R0：`base + i*4 + 4`。
        ///
        /// <para>读取（<see cref="GenerateArrayAccess"/>）与写入（<c>GenerateArrayElementAssignment</c>）
        /// 共用这一处 —— 地址公式只有一份，两条路不会各自漂。</para>
        ///
        /// <para>布局来自数组字面量/声明时的 `Alloc((count+1)*4)`：
        /// `[count, e0, e1, …]`，故元素 i 在 `base + i*4 + 4`（**+4 跳过 count 头**）。</para>
        /// </summary>
        private void EmitArrayElementAddress(ArrayAccessExpression arrExpr)
        {
            GenerateExpression(arrExpr.Array);          // R0 = 数组块地址
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));

            GenerateExpression(arrExpr.Index);          // R0 = 下标

            // ⚠ `SHL` 是 **dest 在前**：原来的 `SHL [IMMEDIATE 2, REGISTER 0]` 把立即数当成
            //   了目标，运行时 `ExecuteShl` 只在 dest 是寄存器时才写回 ⇒ **整条是个空操作**，
            //   于是下标根本没乘 4（元素地址退化成"下标 + 4 + 基址"）。
            instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));

            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
        }

        /// <summary>
        /// 读取数组元素 `a[i]`（结果留在 R0）。
        ///
        /// <para>原实现有两处独立的错，合起来的表现是**永远读回数组头**：</para>
        /// <list type="number">
        /// <item><c>SHL [#2, R0]</c> —— 立即数当目标，空操作（见 <see cref="EmitArrayElementAddress"/>）。</item>
        /// <item>末尾那句写的是 <c>MOVE R0, [R1]</c> —— R1 里装的是**块基址**（不是刚算完的
        /// 元素地址），于是取到的是 `[base]` = count；而且 `[R1]` 在文本汇编里往返一趟会
        /// 变成 `INDIRECT 1`，跟 `[R1+4]` 那种"寄存器相对"形态还不是同一类操作数。</item>
        /// </list>
        /// <para>另外原实现把中间量存进 <c>__aa_arr_N</c>/<c>__aa_idx_N</c> 全局数据字，
        /// 现在改成纯寄存器 + 栈，函数可重入（递归取下标不再互相踩）。</para>
        /// </summary>
        private void GenerateArrayAccess(ArrayAccessExpression arrExpr)
        {
            EmitArrayElementAddress(arrExpr);           // R0 = base + i*4 + 4
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
        }

        private void GenerateEnum(EnumDeclStatement enumDecl)
        {
            for (int i = 0; i < enumDecl.Members.Count; i++)
            {
                string label = $"{enumDecl.Name}_{enumDecl.Members[i]}";
                dataSection[label] = i;
            }
        }
    }
}