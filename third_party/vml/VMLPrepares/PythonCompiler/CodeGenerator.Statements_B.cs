using VMLAssembler;

namespace PythonCompiler
{
    public partial class CodeGenerator
    {
        public void VisitExprStmt(ExprStmtNode node)
        {
            // 跳过文档字符串 — 无需生成运行时代码
            if (node.Value is ConstantNode cn && cn.Value is string)
                return;
            node.Value.Accept(this);
        }

        public void VisitPass(PassNode node)
        {
            // pass 不生成代码
        }

        // ── 新增 Visit 方法 ──────────────────────────────────
        public void VisitLambda(LambdaNode node)
        {
            // Lambda: 生成函数定义 + 调用点
            // 策略: 在当前位置跳过函数体，函数体放在 JMP 之后，调用参数压栈 + CALL 在 JMP 之前
            string tempFunc = $"lambda_f_{labelCounter}";
            int funcId = labelCounter++;
            
            // 先生成调用点代码（参数压栈 + CALL + 清理）
            // 注意: 参数由调用者在外面压栈（在 lambda 出现的表达式上下文中）
            // 这里只记录函数名，让 CallNode 处理调用
            // 实际上 lambda 赋值给变量，变量名就是函数标签
            
            // 生成 JMP 跳过函数体
            string skipLabel = NewLabel("lambda_skip");
            Emit(OpCode.JMP, new Operand(OperandType.LABEL, skipLabel));
            
            // 函数体
            labels[tempFunc] = instructions.Count;
            
            // 保存当前上下文
            var savedLocalVars = new Dictionary<string, int>(localVars);
            var savedStackOffset = stackOffset;
            Vars.ResetLocals();
            localVars.Clear();
            stackOffset = 0;

            // prologue
            EmitPrologue();

            // 参数映射 (CCv2: 第1个参数在 R12+12)
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
            node.Body.Accept(this);
            
            // epilogue
            EmitEpilogue();
            
            // 恢复上下文
            localVars = savedLocalVars;
            stackOffset = savedStackOffset;
            
            PlaceLabel(skipLabel);
            
            // R0 = 函数标签地址（简化: 用 label 名代替）
            // 让 CallNode 通过 labels 字典找到这个函数
            // 此处将 lambda 标签注册到 labels 中即可
            // R0 设为 0 占位（lambda 不能作为值传递，只能通过变量名调用）
            // 更好的方案: 将 lambda 关联到当前赋值的目标变量
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            
            // 注册 lambda 函数名到最近赋值目标
            // 在 AssignNode 中 lambda 变量名会被解析为函数调用
            // 所以需要在 VisitCall 中也检查未定义的函数
        }

        public void VisitYield(YieldNode node)
        {
            // 简化：yield 暂不实现（需要协程支持）
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
        }

        public void VisitAwait(AwaitNode node)
        {
            // 简化：await 暂不实现
            node.Value.Accept(this);
        }

        public void VisitSet(SetNode node)
        {
            // 集合：堆分配，布局 [元素数, 元素1, 元素2, ...]
            // 编译期无法去重，去重语义由运行时保证
            int elementCount = node.Elements.Count;

            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0),
                 new Operand(OperandType.IMMEDIATE, elementCount * 4 + 4));
            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 40));

            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                 new Operand(OperandType.IMMEDIATE, elementCount));
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R0)"), new Operand(OperandType.REGISTER, 1));

            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));
            for (int i = 0; i < elementCount; i++)
            {
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.MEMORY, "0(R13)"));
                Emit(OpCode.ADD, new Operand(OperandType.REGISTER, 1),
                     new Operand(OperandType.IMMEDIATE, 4 + i * 4));

                node.Elements[i].Accept(this);

                Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, "0(R1)"), new Operand(OperandType.REGISTER, 0));
            }
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
        }

        public void VisitSlice(SliceNode node)
        {
            // 简化：切片 → R0 = 基地址（参数已在栈上）
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
        }

        public void VisitStarred(StarredNode node)
        {
            // *expr：先求值
            node.Value.Accept(this);
        }

        public void VisitNamedExpr(NamedExprNode node)
        {
            // := walrus
            node.Value.Accept(this);
            if (!localVars.ContainsKey(node.Target))
            {
                Vars.AllocLocal(node.Target, 4);
                stackOffset -= 4;
                localVars[node.Target] = stackOffset;
            }
            Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, Vars.FormatOffset(localVars[node.Target])), new Operand(OperandType.REGISTER, 0));
        }

        public void VisitDel(DelNode node)
        {
            if (localVars.ContainsKey(node.Target))
                localVars.Remove(node.Target);
        }

        public void VisitAssert(AssertNode node)
        {
            // assert expr, msg → if not expr: raise AssertionError(msg)
            node.Test.Accept(this);
            string skipRaise = NewLabel("assert_ok");
            Sta.EmitJumpIfTrue(skipRaise);
            // 简化：打印错误
            Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, -1));
            Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 99)); // 自定义：assert 失败
            PlaceLabel(skipRaise);
        }

        public void VisitRaise(RaiseNode node)
        {
            if (VMLPlugins.CompilerOptionsContext.Current.IsMCU)
            {
                // MCU: 打印错误码并终止
                if (node.Exc != null) node.Exc.Accept(this);
                Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 99));
                return;
            }
            // OS 模式: THROW 指令
            if (node.Exc != null)
                node.Exc.Accept(this);
            else
                Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
            Emit(OpCode.THROW, new Operand(OperandType.REGISTER, 0));
        }

        public void VisitGlobal(GlobalNode node)
        {
            // global var: 将变量标记为全局（使用固定地址而非栈帧偏移）
            foreach (var name in node.Names)
            {
                if (!globalVars.ContainsKey(name))
                {
                    globalVarOffset -= 4;
                    globalVars[name] = globalVarOffset;
                }
                // 从局部变量中移除（如果存在）
                localVars.Remove(name);
            }
        }

        public void VisitNonlocal(NonlocalNode node)
        {
            // 简化：nonlocal 暂不实现
        }

        public void VisitImport(ImportNode node)
        {
            if (node.ModuleName != null)
            {
                // from X import Y: create variable slots for imported names
                string moduleLabel = $"module_{node.ModuleName}";
                if (!dataSection.ContainsKey(moduleLabel))
                    dataSection[moduleLabel] = 1; // Mark module as imported

                foreach (var (name, alias) in node.Items)
                {
                    string varName = alias ?? name;
                    string funcLabel = $"{node.ModuleName}_{name}";
                    // Store a reference label for the imported function
                    string varLabel = $"var_{varName}";
                    if (!dataSection.ContainsKey(varLabel))
                        dataSection[varLabel] = 0;
                    // Emit: LOAD R0, funcLabel; STORE varLabel
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, funcLabel));
                    Emit(OpCode.MOVE, new Operand(OperandType.LABEL, varLabel), new Operand(OperandType.REGISTER, 0));
                }
            }
            else
            {
                // import X: mark module as loaded (no-op for MCU, symbols linked at compile time)
                foreach (var (name, _) in node.Items)
                {
                    string moduleLabel = $"module_{name}";
                    if (!dataSection.ContainsKey(moduleLabel))
                        dataSection[moduleLabel] = 1;
                }
            }
        }

        public void VisitWith(WithNode node)
        {
            // with expr as name: body → evaluate expr, store alias, execute body, cleanup
            foreach (var (item, alias) in node.Items)
            {
                item.Accept(this);
                if (alias != null)
                {
                    if (!localVars.ContainsKey(alias))
                    {
                        Vars.AllocLocal(alias, 4);
                        stackOffset -= 4;
                        localVars[alias] = stackOffset;
                    }
                    Emit(OpCode.MOVE, new Operand(OperandType.MEMORY, Vars.FormatOffset(localVars[alias])), new Operand(OperandType.REGISTER, 0));
                }
            }
            foreach (var stmt in node.Body)
                stmt.Accept(this);

            // Cleanup: for open() calls, emit close on the file handle
            foreach (var (item, alias) in node.Items)
            {
                if (item is CallNode call && call.FuncName == "open" && alias != null && localVars.ContainsKey(alias))
                {
                    Emit(OpCode.MOVE, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12{localVars[alias]}"));
                    Emit(OpCode.SYSCALL, new Operand(OperandType.IMMEDIATE, 5));  // close file
                }
            }
        }

        public void VisitTry(TryNode node)
        {
            if (VMLPlugins.CompilerOptionsContext.Current.IsMCU)
            {
                // MCU: 仅执行 try/except/finally body，不生成异常处理
                foreach (var stmt in node.Body) stmt.Accept(this);
                foreach (var (_, __, handlerBody) in node.Handlers)
                    foreach (var stmt in handlerBody) stmt.Accept(this);
                if (node.Orelse.Count > 0)
                    foreach (var stmt in node.Orelse) stmt.Accept(this);
                foreach (var stmt in node.Finalbody) stmt.Accept(this);
                return;
            }

            // OS 模式: 生成 CATCH/ENDCATCH 异常处理
            string catchLabel = NewLabel("try_catch");
            string endLabel = NewLabel("try_end");

            Emit(OpCode.CATCH, new Operand(OperandType.LABEL, catchLabel));
            foreach (var stmt in node.Body)
                stmt.Accept(this);
            Emit(OpCode.ENDCATCH);
            Emit(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));

            PlaceLabel(catchLabel);
            foreach (var (_, varName, handlerBody) in node.Handlers)
            {
                if (varName != null)
                {
                    // R0 包含异常值，存入 catch 变量
                    Emit(OpCode.MOVE, new Operand(OperandType.LABEL, $"var_{varName}"), new Operand(OperandType.REGISTER, 0));
                }
                foreach (var stmt in handlerBody)
                    stmt.Accept(this);
                Emit(OpCode.ENDCATCH);
            }

            PlaceLabel(endLabel);
            if (node.Orelse.Count > 0)
                foreach (var stmt in node.Orelse) stmt.Accept(this);
            foreach (var stmt in node.Finalbody)
                stmt.Accept(this);
        }

        public void VisitMatch(MatchNode node)
        {
            // 将 subject 压栈，逐 case 比较（PUSH/POP 保护模式）
            node.Subject.Accept(this);
            Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 0));

            string endLabel = NewLabel("match_end");
            Sta!.PushLoopLabels(endLabel, null);

            for (int i = 0; i < node.Cases.Count; i++)
            {
                string caseEnd = NewLabel($"case_{i}_end");
                var c = node.Cases[i];

                // 检查是否通配符模式 _
                bool isWildcard = c.Pattern is NameNode name && name.Name == "_";

                if (!isWildcard)
                {
                    // POP subject → R1, PUSH 回栈保护(防止 pattern 求值修改)
                    Emit(OpCode.POP, new Operand(OperandType.REGISTER, 1));
                    Emit(OpCode.PUSH, new Operand(OperandType.REGISTER, 1));
                    // 求值 pattern → R0
                    c.Pattern.Accept(this);
                    // 比较 pattern(R0) vs subject(R1)
                    Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1));
                    Emit(OpCode.JNE, new Operand(OperandType.LABEL, caseEnd));
                }

                // guard 检查 (subject 仍在栈上)
                if (c.Guard != null)
                {
                    c.Guard.Accept(this);
                    Emit(OpCode.CMP, new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0));
                    Emit(OpCode.JE, new Operand(OperandType.LABEL, caseEnd));
                }

                // 匹配成功: 清理栈上的 subject
                Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
                // case body
                foreach (var stmt in c.Body)
                    stmt.Accept(this);
                Emit(OpCode.JMP, new Operand(OperandType.LABEL, endLabel));
                PlaceLabel(caseEnd);
            }

            // 无匹配: 清理栈上的 subject
            Emit(OpCode.POP, new Operand(OperandType.REGISTER, 0));
            PlaceLabel(endLabel);
            Sta!.PopLoopLabels();
        }

        public void VisitCase(CaseNode node)
        {
            // Case 节点由 VisitMatch 展开处理，此处作为占位
            node.Pattern.Accept(this);
        }
    }
}
