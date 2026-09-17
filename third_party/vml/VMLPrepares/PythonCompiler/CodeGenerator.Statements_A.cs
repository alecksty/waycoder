using VMLAssembler;

namespace PythonCompiler
{
    public partial class CodeGenerator
    {
        public void VisitProgram(ProgramNode node)
        {
            // 生成数据段字符串常量
            dataSection["py_version"] = "Python 3.11 (VML)";
            
            // 先处理所有函数定义，保存程序语句的起始指令位置
            _mainEntry = instructions.Count;
            foreach (var stmt in node.Body)
            {
                if (stmt is FunctionDefNode || stmt is ClassDefNode)
                {
                    stmt.Accept(this);
                }
            }
            _mainEntry = instructions.Count;
            foreach (var stmt in node.Body)
            {
                if (!(stmt is FunctionDefNode || stmt is ClassDefNode))
                {
                    stmt.Accept(this);
                }
            }
        }

        public void VisitFunctionDef(FunctionDefNode node)
        {
            currentFunction = node.Name;
            Vars.ResetLocals();
            localVars.Clear();
            stackOffset = 0;
            
            labels[node.Name] = instructions.Count;
            
            // 函数级注释
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");
            var sourceDecl = $"def {node.Name}(";
            for (var i = 0; i < node.Args.Count; i++)
            {
                sourceDecl += node.Args[i];
                if (i < node.Args.Count - 1) sourceDecl += ",";
            }
            sourceDecl += ")";
            Emit(OpCode.NOP, new List<Operand>(), $"; source   : {sourceDecl}");
            Emit(OpCode.NOP, new List<Operand>(), $"; function : {node.Name}");
            foreach (var arg in node.Args)
            {
                Emit(OpCode.NOP, new List<Operand>(), $"; param   : {arg}");
            }
            Emit(OpCode.NOP, new List<Operand>(), "; return   : var");
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");

            // 函数序言
            EmitPrologue();
            // 帧占位：EmitPrologue 只压了 R15/R12 并设了 R12，**没有给局部量留空间** ——
            // R13 仍等于 R12，于是每个 push/call 都直接写进 [R12-4]、[R12-8] … 这些局部量槽。
            // 帧大小要等函数体生成完才知道，故先占位、最后回填（与 DartCompiler 同一口径）。
            int framePatchIndex = instructions.Count;
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> {
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.IMMEDIATE, 0) }));
            
            // 为参数分配栈空间并保存CCv2寄存器参数(R0-R3)
            // CCv2: 第1个参数在 R12+12 (越过 saved_R12, R15, CALL-RA)
            for (int i = 0; i < node.Args.Count; i++)
            {
                var vi = Vars.AllocParam(node.Args[i], 4);
                localVars[node.Args[i]] = vi.Offset;
                if (i < 4)
                {
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, Vars.FormatOffset(vi.Offset)), new Operand(OperandType.REGISTER, i));
                }
            }
            
            // 函数体
            foreach (var stmt in node.Body)
            {
                stmt.Accept(this);
            }
            
            // 函数尾声（如果没有 return）
            // 回填帧大小（+8 安全边界：最深的局部量必须严格高于 R13）
            int frameSize = -stackOffset + 8;
            instructions[framePatchIndex] = new Instruction(OpCode.SUB, new List<Operand> {
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.IMMEDIATE, frameSize) });
            EmitEpilogue();

            // 装饰器应用: @deco → func = deco(func)
            foreach (var deco in node.Decorators)
            {
                // LEA R0, funcLabel — 获取函数地址 (函数标签 = node.Name)
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, node.Name));
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                if (deco is CallNode call)
                {
                    // @deco(args) → CALL deco (简化: 忽略参数)
                    Emit(OpCode.CALL, new Operand(OperandType.LABEL, call.FuncName));
                }
                else if (deco is NameNode nameNode)
                {
                    // @deco → CALL deco_name
                    Emit(OpCode.CALL, new Operand(OperandType.LABEL, nameNode.Name));
                }
            }
        }

        public void VisitClassDef(ClassDefNode node)
        {
            string className = node.Name;

            // 保存当前类上下文（用于 super() 解析）
            string? prevClassName = _currentClassName;
            _currentClassName = className;

            // 创建类信息
            var info = new ClassInfo
            {
                TypeLabel = $"type_{className}",
                InitLabel = $"{className}__init__",
                ParentName = node.ParentName
            };

            // Inherit parent methods
            if (node.ParentName != null && classInfo.TryGetValue(node.ParentName, out var parentInfo))
            {
                foreach (var kv in parentInfo.Methods)
                    if (!info.Methods.ContainsKey(kv.Key))
                        info.Methods[kv.Key] = kv.Value;
            }

            // 在数据段中记录类类型
            dataSection[info.TypeLabel] = 0;
            // 实例指针 (单实例简化模型)
            dataSection[$"{className}_instance"] = 0;

            // 为构造函数生成标签
            labels[info.InitLabel] = instructions.Count;

            // 生成构造函数代码
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)); // self参数
            Emit(OpCode.MOVE, new Operand(OperandType.LABEL, $"{className}_instance"), new Operand(OperandType.REGISTER, 0));

            // Call parent __init__ if exists
            if (node.ParentName != null && classInfo.TryGetValue(node.ParentName, out var pi))
            {
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0)); // push self
                Emit(OpCode.CALL, new Operand(OperandType.LABEL, pi.InitLabel));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4));
            }

            Emit(OpCode.RET);

            // 处理类体
            foreach (var stmt in node.Body)
            {
                if (stmt is FunctionDefNode funcDef)
                {
                    // 记录方法
                    string methodLabel = $"{className}_{funcDef.Name}";
                    info.Methods[funcDef.Name] = methodLabel;

                    // 为方法生成代码
                    labels[methodLabel] = instructions.Count;

                    // 保存当前函数上下文
                    string prevFunction = currentFunction;
                    currentFunction = methodLabel;

                    // 生成方法体
                    foreach (var methodStmt in funcDef.Body)
                    {
                        methodStmt.Accept(this);
                    }

                    // 如果没有返回语句，添加默认返回
                    if (!funcDef.Body.Any(s => s is ReturnNode))
                    {
                        Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                        Emit(OpCode.RET);
                    }

                    // 恢复函数上下文
                    currentFunction = prevFunction;
                }
                else
                {
                    // 类变量定义
                    stmt.Accept(this);
                }
            }

            // 保存类信息
            classInfo[className] = info;

            // 恢复类上下文
            _currentClassName = prevClassName;
        }

        public void VisitIf(IfNode node)
        {
            // 三元表达式: x if cond else y
            // Parser: Test=真值表达式, Body[0]=条件, Orelse[0]=假值表达式
            bool isTernary = node.Body.Count == 1 && node.Orelse.Count == 1 &&
                             node.Body[0] is ExprStmtNode && node.Orelse[0] is ExprStmtNode;

            if (isTernary)
            {
                string elseLabel = NewLabel("tern_else");
                string endLabel = NewLabel("tern_end");

                // 条件: Body[0]
                ((ExprStmtNode)node.Body[0]).Value.Accept(this);
                Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                Emit(OpCode.JE, new Operand(OperandType.LABEL, elseLabel));

                // 真值: Test
                node.Test.Accept(this);
                Emit(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));

                PlaceLabel(elseLabel);
                // 假值: Orelse[0]
                ((ExprStmtNode)node.Orelse[0]).Value.Accept(this);

                PlaceLabel(endLabel);
                return;
            }

            // if/elif/else — elif 通过递归 AST 嵌套处理
            Sta!.EmitIf(
                () => node.Test.Accept(this),
                () => { foreach (var stmt in node.Body) stmt.Accept(this); },
                node.Orelse.Count > 0 ? () => { foreach (var stmt in node.Orelse) stmt.Accept(this); } : null);
        }

        public void VisitWhile(WhileNode node)
        {
            string startLabel = NewLabel("while");
            string endLabel = NewLabel("endwhile");
            string elseLabel = NewLabel("while_else");
            
            // break 跳过 else，直接到最终出口
            string exitLabel = node.Orelse != null && node.Orelse.Count > 0 
                ? NewLabel("while_exit") : endLabel;

            Sta!.PushLoopLabels(exitLabel, startLabel);

            PlaceLabel(startLabel);

            // 条件
            node.Test.Accept(this);
            if (node.Orelse != null && node.Orelse.Count > 0)
                Sta!.EmitJumpIfFalse(elseLabel);
            else
                Sta!.EmitJumpIfFalse(endLabel);

            // 循环体
            foreach (var stmt in node.Body)
                stmt.Accept(this);

            Emit(OpCode.JMP, new Operand(OperandType.LABEL, startLabel));
            PlaceLabel(endLabel);

            // else 分支（条件为假时执行，break 不执行）
            if (node.Orelse != null && node.Orelse.Count > 0)
            {
                PlaceLabel(elseLabel);
                foreach (var stmt in node.Orelse)
                    stmt.Accept(this);
                PlaceLabel(exitLabel);
            }

            Sta!.PopLoopLabels();
        }

        public void VisitFor(ForNode node)
        {
            string startLabel = NewLabel("for");
            string endLabel = NewLabel("endfor");

            string exitLabel = node.Orelse != null && node.Orelse.Count > 0
                ? NewLabel("for_exit") : endLabel;

            Sta!.PushLoopLabels(exitLabel, startLabel);

            if (!localVars.ContainsKey(node.Target))
            {
                Vars.AllocLocal(node.Target, 4);
                stackOffset -= 4;
                localVars[node.Target] = stackOffset;
            }

            // for x in range(n) / range(start, end[, step])
            if (node.Iter is CallNode call && call.FuncName == "range")
            {
                int rangeEndOffset, rangeStepOffset;
                stackOffset -= 4;
                rangeEndOffset = stackOffset;
                stackOffset -= 4;
                rangeStepOffset = stackOffset;

                if (call.Args.Count == 1)
                {
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
                    call.Args[0].Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{rangeEndOffset}"), new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{rangeStepOffset}"), new Operand(OperandType.REGISTER, 0));
                }
                else if (call.Args.Count >= 2)
                {
                    call.Args[0].Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
                    call.Args[1].Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{rangeEndOffset}"), new Operand(OperandType.REGISTER, 0));
                    if (call.Args.Count >= 3)
                        call.Args[2].Accept(this);
                    else
                        Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{rangeStepOffset}"), new Operand(OperandType.REGISTER, 0));
                }

                PlaceLabel(startLabel);
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"));
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{rangeEndOffset}"));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));

                if (node.Orelse != null && node.Orelse.Count > 0)
                {
                    string elseLabel = NewLabel("for_else");
                    Emit(OpCode.JGE, new Operand(OperandType.LABEL, elseLabel));
                    foreach (var stmt in node.Body) stmt.Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"));
                    Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{rangeStepOffset}"));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.JMP, new Operand(OperandType.LABEL, startLabel));
                    PlaceLabel(endLabel);
                    PlaceLabel(elseLabel);
                    foreach (var stmt in node.Orelse) stmt.Accept(this);
                    PlaceLabel(exitLabel);
                }
                else
                {
                    Emit(OpCode.JGE, new Operand(OperandType.LABEL, endLabel));
                    foreach (var stmt in node.Body) stmt.Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.JMP, new Operand(OperandType.LABEL, startLabel));
                    PlaceLabel(endLabel);
                }
            }
            // for x in list/tuple: 迭代容器元素
            else if (node.Iter is NameNode || node.Iter is ListNode || node.Iter is TupleNode)
            {
                // 分配: 容器地址(idxOffset), 索引(cntOffset), 长度(lenOffset)
                int addrOffset, indexOffset, lenOffset;
                stackOffset -= 4; addrOffset = stackOffset;
                stackOffset -= 4; indexOffset = stackOffset;
                stackOffset -= 4; lenOffset = stackOffset;

                node.Iter.Accept(this); // R0 = container address
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{addrOffset}"), new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "0(R0)")); // length
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{lenOffset}"), new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{indexOffset}"), new Operand(OperandType.REGISTER, 0));

                PlaceLabel(startLabel);
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{indexOffset}"));
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12{lenOffset}"));
                Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));

                if (node.Orelse != null && node.Orelse.Count > 0)
                {
                    string elseLabel = NewLabel("for_else");
                    Emit(OpCode.JGE, new Operand(OperandType.LABEL, elseLabel));
                    // load element: container+4+index*4
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{addrOffset}"));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12{indexOffset}"));
                    Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "0(R0)"));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
                    foreach (var stmt in node.Body) stmt.Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{indexOffset}"));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{indexOffset}"), new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.JMP, new Operand(OperandType.LABEL, startLabel));
                    PlaceLabel(endLabel);
                    PlaceLabel(elseLabel);
                    foreach (var stmt in node.Orelse) stmt.Accept(this);
                    PlaceLabel(exitLabel);
                }
                else
                {
                    Emit(OpCode.JGE, new Operand(OperandType.LABEL, endLabel));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{addrOffset}"));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12{indexOffset}"));
                    Emit(OpCode.MUL, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "0(R0)"));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
                    foreach (var stmt in node.Body) stmt.Accept(this);
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{indexOffset}"));
                    Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{indexOffset}"), new Operand(OperandType.REGISTER, 0));
                    Emit(OpCode.JMP, new Operand(OperandType.LABEL, startLabel));
                    PlaceLabel(endLabel);
                }
            }
            else
            {
                // 非range/list迭代器，简化为0
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
            }

            Sta!.PopLoopLabels();
        }

        public void VisitBreak(BreakNode node)
        {
            Sta!.EmitBreak();
        }

        public void VisitContinue(ContinueNode node)
        {
            Sta!.EmitContinue();
        }

        public void VisitReturn(ReturnNode node)
        {
            if (node.Value != null)
            {
                node.Value.Accept(this);
                // 返回值在 R0
            }
            
            EmitEpilogue();
        }

        public void VisitAssign(AssignNode node)
        {
            // 特殊处理: f = lambda x, y: x + y → 注册 lambda 标签到变量名
            if (node.Value is LambdaNode lambdaNode)
            {
                string lambdaLabel = $"lambda_f_{labelCounter}";
                lambdaVars[node.Target] = lambdaLabel;
                
                // 生成 JMP 跳过函数体
                string skipLabel = NewLabel("lambda_skip");
                Emit(OpCode.JMP, new Operand(OperandType.LABEL, skipLabel));
                
                // 函数体
                labels[lambdaLabel] = instructions.Count;
                
                // 保存上下文
                var savedLocalVars = new Dictionary<string, int>(localVars);
                var savedStackOffset = stackOffset;
                var savedCurrentFunc = currentFunction;
                localVars.Clear();
                stackOffset = 0;
                Vars.ResetLocals();
                currentFunction = lambdaLabel;
                
                // prologue
                EmitPrologue();
                // 帧占位：EmitPrologue 只压了 R15/R12 并设了 R12，**没有给局部量留空间** ——
                // R13 仍等于 R12，于是每个 push/call 都直接写进 [R12-4]、[R12-8] … 这些局部量槽。
                // 帧大小要等函数体生成完才知道，故先占位、最后回填（与 DartCompiler 同一口径）。
                int framePatchIndex = instructions.Count;
                instructions.Add(new Instruction(OpCode.SUB, new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, 0) }));
                
                for (int i = 0; i < lambdaNode.Args.Count; i++)
                    localVars[lambdaNode.Args[i]] = 12 + 4 * i; // 第1个参数在 R12+12 (越过 saved_R12, saved_R15, CALL-RA)
                
                lambdaNode.Body.Accept(this);
                
                // epilogue
                // 回填帧大小（+8 安全边界：最深的局部量必须严格高于 R13）
                int frameSize = -stackOffset + 8;
                instructions[framePatchIndex] = new Instruction(OpCode.SUB, new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, frameSize) });
                EmitEpilogue();
                
                // 恢复上下文
                localVars = savedLocalVars;
                stackOffset = savedStackOffset;
                currentFunction = savedCurrentFunc;
                
                PlaceLabel(skipLabel);
                
                // 分配变量空间（lambda 赋值不需要实际存值，调用时通过标签跳转）
                if (!localVars.ContainsKey(node.Target))
                {
                    Vars.AllocLocal(node.Target, 4);
                    stackOffset -= 4;
                    localVars[node.Target] = stackOffset;
                }
                varTypes[node.Target] = PythonType.Object;
                return;
            }

            // 属性/下标赋值: obj.attr = value / container[index] = value
            if (node.TargetExpr != null)
            {
                // 下标赋值：a[i] = v
                // 此前这里**整段没有存储指令** —— 右值算完就 return，一个字节都没写进去，
                // 症状正是"列表读可以、写不生效"（a[i]=v 之后读回来还是旧值）。
                // 地址计算与读取共用 EmitSubscriptAddress，避免读写两处偏移各算一遍。
                if (node.TargetExpr is SubscriptNode subTarget)
                {
                    string subOob = NewLabel("sub_store_oob");

                    // 先算地址：越界就整条跳过（不写、也不求值 —— 越界赋值本就是错误路径）
                    EmitSubscriptAddress(subTarget, subOob);
                    Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 2)); // 保存元素地址

                    node.Value.Accept(this); // R0 = value（求值会冲掉 R2，故地址先入栈）

                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 2));  // R2 = 元素地址
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R2)"),
                         new Operand(OperandType.REGISTER, 0));              // 存入元素

                    // 越界直接落到这里：跳过存储（栈是平的 —— PUSH 在那句之后才执行）
                    PlaceLabel(subOob);
                    return;
                }

                node.Value.Accept(this);  // R0 = value
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                node.TargetExpr.Accept(this);  // evaluate target (pushes address?)
                Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));  // R1 = value
                // For self.x attribute, VisitAttribute loads from label
                // We need to store instead. The AttributeNode stores to instance_x.
                if (node.TargetExpr is AttributeNode attrNode &&
                    attrNode.Value is NameNode nameNode && nameNode.Name == "self")
                {
                    string instanceVarLabel = $"instance_{attrNode.Attr}";
                    if (!dataSection.ContainsKey(instanceVarLabel))
                        dataSection[instanceVarLabel] = 0;
                    Emit(OpCode.MOVE, new Operand(OperandType.LABEL, instanceVarLabel), new Operand(OperandType.REGISTER, 1));
                }
                return;
            }
            
            // 推断表达式类型
            PythonType exprType = InferExpressionType(node.Value);
            
            // 记录变量类型
            varTypes[node.Target] = exprType;
            
            node.Value.Accept(this);
            
            // 检查全局变量
            if (globalVars.ContainsKey(node.Target))
            {
                OpCode storeOp = GetStoreInstruction(exprType);
                Emit(storeOp, new Operand(OperandType.MEMORY, $"global_{node.Target}"), new Operand(OperandType.REGISTER, 0));
                return;
            }
            
            if (!localVars.ContainsKey(node.Target))
            {
                Vars.AllocLocal(node.Target, 4);
                stackOffset -= 4;
                localVars[node.Target] = stackOffset;
            }

            OpCode storeOpLocal = GetStoreInstruction(exprType);
            Emit(storeOpLocal, new Operand(OperandType.MEMORY, $"R12{localVars[node.Target]}"), new Operand(OperandType.REGISTER, 0));
        }

        public void VisitAugAssign(AugAssignNode node)
        {
            // 属性/下标增强赋值
            if (node.TargetExpr != null)
            {
                // 下标增强赋值：a[i] += v —— 与 VisitAssign 同一个洞（算完就丢）。
                // 另：原路径落不到任何存储分支时，末尾压进去的 R0 **没人弹** ⇒ 每执行一次漏一个栈槽。
                if (node.TargetExpr is SubscriptNode augSub)
                {
                    string augOob = NewLabel("sub_aug_oob");

                    EmitSubscriptAddress(augSub, augOob);                     // R2 = 元素地址
                    Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 2));  // 保存地址
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                         new Operand(OperandType.MEMORY, "0(R2)"));           // R0 = 当前值
                    Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));

                    node.Value.Accept(this);                                  // R0 = 右值

                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));   // R1 = 当前值
                    OpCode augArithOp = GetArithmeticInstruction(node.Op.Replace("=", ""), PythonType.Int);
                    Emit(augArithOp, new Operand(OperandType.REGISTER, 0),
                         new Operand(OperandType.REGISTER, 1),
                         new Operand(OperandType.REGISTER, 0));               // R0 = 当前值 op 右值
                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 2));   // R2 = 元素地址
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R2)"),
                         new Operand(OperandType.REGISTER, 0));               // 写回元素

                    PlaceLabel(augOob);
                    return;
                }

                node.TargetExpr.Accept(this);  // R0 = current value
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                node.Value.Accept(this);       // R0 = right value
                Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));  // R1 = left value
                OpCode arithOp = GetArithmeticInstruction(node.Op.Replace("=", ""), PythonType.Int);
                Emit(arithOp, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.REGISTER, 0));
                Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
                if (node.TargetExpr is AttributeNode attr &&
                    attr.Value is NameNode nn && nn.Name == "self")
                {
                    string label = $"instance_{attr.Attr}";
                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                    Emit(OpCode.MOVE, new Operand(OperandType.LABEL, label), new Operand(OperandType.REGISTER, 1));
                }
                return;
            }
            var target = WrapTargetExpr(node.Target);
            var value = WrapExpr(node.Value);
            string op = node.Op.Replace("=", "");
            _expr!.EmitCompoundAssign(target, value, op);
        }

        public void VisitMultiAssign(MultiAssignNode node)
        {
            // 求值右侧表达式（元组/列表） → R0 = 容器地址
            node.Value.Accept(this);

            // 保存容器地址到栈上
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));

            for (int i = 0; i < node.Targets.Count; i++)
            {
                string target = node.Targets[i];

                // 从栈顶取容器地址，计算元素偏移: base + 4 + i*4
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "0(R13)"));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4 + i * 4));
                // 加载元素值到 R0
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "0(R1)"));

                // 分配目标变量
                if (!localVars.ContainsKey(target))
                {
                    Vars.AllocLocal(target, 4);
                    stackOffset -= 4;
                    localVars[target] = stackOffset;
                }
                varTypes[target] = PythonType.Int;

                // 存储到目标变量
                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R12{localVars[target]}"), new Operand(OperandType.REGISTER, 0));
            }

            // 弹出保存的容器地址
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
        }
    }
}
