using VMLAssembler;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(LoopStatementNode node)
        {
            string startLabel = NewLabel("loop_start");
            string endLabel = NewLabel("loop_end");
            Sta!.PushLoopLabels(endLabel, startLabel);

            AddLabel(startLabel);
            node.Body.Accept(this);
            AddInstruction(OpCode.JMP, startLabel);
            AddLabel(endLabel);

            Sta!.PopLoopLabels();
        }

        public void Visit(BreakStatementNode node)
        {
            Sta!.EmitBreak();
        }

        public void Visit(ContinueStatementNode node)
        {
            Sta!.EmitContinue();
        }

        public void Visit(MatchStatementNode node)
        {
            _hasMatch = true;
            string endLabel = NewLabel("match_end");
            Sta!.PushLoopLabels(endLabel, null);
            node.Value.Accept(this);
            AddInstruction(OpCode.PUSH, "R0");

            int armBaseOffset = _variableOffset;

            // Pre-scan: find max binding count across all arms
            int maxBindings = 0;
            foreach (var arm in node.Arms)
            {
                int bc = 0;
                if (arm.Pattern is EnumPatternNode ep && ep.BindName != null) bc = 1;
                else if (arm.Pattern is TupleExprNode tp)
                    bc = tp.Elements.Count(e => e is IdentifierNode);
                if (bc > maxBindings) maxBindings = bc;
            }
            int bodyBaseOffset = armBaseOffset - maxBindings * 4;
            int maxDepthOffset = armBaseOffset;

            foreach (var arm in node.Arms)
            {
                string nextLabel = NewLabel("match_next");

                // Reset variables to pre-match state for each arm
                _variableOffset = armBaseOffset;
                var savedVars = new Dictionary<string, int>(_variables);

                if (arm.Pattern is IdentifierNode id && id.Name == "_")
                {
                    // wildcard — handled after the loop as catch-all
                }
                else if (arm.Pattern is EnumPatternNode ep)
                {
                    AddInstruction(OpCode.POP, "R0");  // R0 = pointer to enum
                    AddInstruction(OpCode.PUSH, "R0"); // restore on stack
                    AddInstruction(OpCode.MOVE, "R1", "(R0)");     // R1 = tag
                    int expectedTag = GetEnumVariantTag(ep.VariantName);
                    AddInstruction(OpCode.MOVE, "R2", $"#{expectedTag}");
                    AddInstruction(OpCode.CMP, "R1", "R2");
                    AddInstruction(OpCode.JNE, nextLabel);
                    if (ep.BindName != null)
                    {
                        _variableOffset -= 4;
                        _variables[ep.BindName] = _variableOffset;
                        AddInstruction(OpCode.POP, "R0");  // pointer to enum
                        AddInstruction(OpCode.PUSH, "R0"); // restore on stack
                        AddInstruction(OpCode.MOVE, "R0", "4(R0)");    // load payload
                        AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                    }
                }
                else if (arm.Pattern is TupleExprNode tuplePat)
                {
                    AddInstruction(OpCode.POP, "R1");  // R1 = pointer to tuple
                    AddInstruction(OpCode.PUSH, "R1"); // restore on stack
                    for (int i = 0; i < tuplePat.Elements.Count; i++)
                    {
                        if (tuplePat.Elements[i] is IdentifierNode bindId)
                        {
                            _variableOffset -= 4;
                            _variables[bindId.Name] = _variableOffset;
                            AddInstruction(OpCode.MOVE, "R0", $"{i * 4}(R1)");
                            AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                        }
                    }
                }
                else
                {
                    AddInstruction(OpCode.POP, "R1");  // R1 = match value
                    AddInstruction(OpCode.PUSH, "R1"); // restore on stack
                    arm.Pattern.Accept(this);          // R0 = pattern value
                    AddInstruction(OpCode.CMP, "R0", "R1");
                    AddInstruction(OpCode.JNE, nextLabel);
                }

                // Ensure body starts from the same offset for all arms
                _variableOffset = bodyBaseOffset;

                AddInstruction(OpCode.POP, "R0"); // pop match value
                if (arm.Body is BlockNode block)
                    block.Accept(this);
                else
                    arm.Body.Accept(this);

                if (_variableOffset < maxDepthOffset)
                    maxDepthOffset = _variableOffset;

                AddInstruction(OpCode.JMP, endLabel);
                AddLabel(nextLabel);

                // Restore variables to pre-arm state (remove pattern bindings added this arm)
                var keysToRemove = new List<string>();
                foreach (var k in _variables.Keys)
                    if (!savedVars.ContainsKey(k)) keysToRemove.Add(k);
                foreach (var k in keysToRemove)
                    _variables.Remove(k);
            }

            _variableOffset = maxDepthOffset;

            // wildcard (always last arm, or implicit)
            var lastArm = node.Arms.Count > 0 ? node.Arms[node.Arms.Count - 1] : null;
            if (lastArm != null && lastArm.Pattern is IdentifierNode wild && wild.Name == "_")
            {
                _variableOffset = bodyBaseOffset;
                AddInstruction(OpCode.POP, "R0"); // pop match value
                if (lastArm.Body is BlockNode block2)
                    block2.Accept(this);
                else
                    lastArm.Body.Accept(this);
                if (_variableOffset < maxDepthOffset)
                    maxDepthOffset = _variableOffset;
                _variableOffset = maxDepthOffset;
            }
            AddLabel(endLabel);
            Sta!.PopLoopLabels();
        }

        private int GetEnumVariantTag(string variantName)
        {
            if (constants.TryGetValue(variantName, out object val) && val is int iv)
                return iv;
            return 0;
        }

        public void Visit(EnumPatternNode node)
        {
            // Patterns are handled inline in Visit(MatchStatementNode) / Visit(IfLetStatementNode)
        }

        public void Visit(IfLetStatementNode node)
        {
            _hasMatch = true;
            string elseLabel = NewLabel("iflet_else");
            string endLabel = NewLabel("iflet_end");

            node.Value.Accept(this);
            AddInstruction(OpCode.PUSH, "R0");

            if (node.Pattern is EnumPatternNode ep)
            {
                AddInstruction(OpCode.POP, "R0");  // R0 = pointer to enum
                AddInstruction(OpCode.PUSH, "R0"); // restore on stack
                AddInstruction(OpCode.MOVE, "R1", "(R0)");        // tag
                int expectedTag = GetEnumVariantTag(ep.VariantName);
                AddInstruction(OpCode.MOVE, "R2", $"#{expectedTag}");
                AddInstruction(OpCode.CMP, "R1", "R2");
                AddInstruction(OpCode.JNE, elseLabel);
                AddInstruction(OpCode.POP, "R0");  // clean stack, R0 = pointer
                if (ep.BindName != null)
                {
                    _variableOffset -= 4;
                    _variables[ep.BindName] = _variableOffset;
                    AddInstruction(OpCode.MOVE, "R0", "4(R0)");   // load payload
                    AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                }
            }
            else if (node.Pattern is IdentifierNode id && id.Name != "_")
            {
                // if let x = expr — simple binding
                AddInstruction(OpCode.POP, "R0");  // R0 = match value
                AddInstruction(OpCode.PUSH, "R0"); // restore on stack
                AddInstruction(OpCode.CMP, "R0", "#0");
                AddInstruction(OpCode.JE, elseLabel);
                AddInstruction(OpCode.POP, "R0");  // clean stack, R0 = value
                _variableOffset -= 4;
                _variables[id.Name] = _variableOffset;
                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
            }

            node.ThenBlock.Accept(this);
            AddInstruction(OpCode.JMP, endLabel);
            AddLabel(elseLabel);
            AddInstruction(OpCode.POP, "R0"); // pop value on else path
            if (node.ElseBlock != null)
            {
                if (node.ElseBlock is BlockNode elseBlock)
                    elseBlock.Accept(this);
                else
                    node.ElseBlock.Accept(this);
            }
            AddLabel(endLabel);
        }

        public void Visit(EnumDeclNode node)
        {
            for (int i = 0; i < node.Variants.Count; i++)
            {
                string fullName = $"{node.Name}_{node.Variants[i].Name}";
                constants[fullName] = i;
                // Store variant metadata for codegen: variant index + payload info
                _enumVariants[fullName] = (i, node.Variants[i].PayloadType);
            }
        }

        public void Visit(StructDeclNode node)
        {
            // struct declarations are stored for later use by impl blocks
            _structFields[node.Name] = node.Fields;
        }

        public void Visit(TraitDeclNode node)
        {
            _traitMethods[node.Name] = node.Methods.Select(m => m.Name).ToList();
        }

        public void Visit(ImplBlockNode node)
        {
            foreach (var method in node.Methods)
            {
                string label = $"method_{node.StructName}_{method.Name}";
                _currentStructName = node.StructName;
                AddLabel(label);
                VisitMethodBody(method);
                _currentStructName = "";
            }

            // Build vtable mapping for trait implementations
            if (node.TraitName != null && _traitMethods.TryGetValue(node.TraitName, out var traitMethodNames))
            {
                string key = $"{node.StructName}:{node.TraitName}";
                var methodMap = new Dictionary<string, string>();
                foreach (var tm in traitMethodNames)
                {
                    string structMethodLabel = $"method_{node.StructName}_{tm}";
                    methodMap[tm] = structMethodLabel;
                    // Store vtable entry in data section
                    dataSection[$"vtab_{node.StructName}_{node.TraitName}_{tm}"] = structMethodLabel;
                }
                _traitImpls[key] = methodMap;
                dataSection[$"vtab_{node.StructName}_{node.TraitName}_len"] = traitMethodNames.Count;
            }
        }

        private void VisitMethodBody(FunctionNode method)
        {
            _variableOffset = 0;
            _variables.Clear();
            EnterScope();

            // 参数: 第一个参数是 self
            for (int i = method.Parameters.Count - 1; i >= 0; i--)
            {
                var param = method.Parameters[i];
                _variableOffset -= 4;
                _variables[param.Name] = _variableOffset;
                AddInstruction(OpCode.POP, "R0");
                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
            }

            method.Body.Accept(this);

            ExitScope();
            AddInstruction(OpCode.POP, "R12");
            AddInstruction(OpCode.POP, "R15");
            AddInstruction(OpCode.RET);
        }

        public void Visit(ClosureExprNode node)
        {
            if (node.Body != null && node.Body is ASTNode body)
                body.Accept(this);
        }

        public void Visit(TupleExprNode node)
        {
            // Allocate memory for all tuple elements
            int count = node.Elements.Count;
            if (count == 0)
            {
                AddInstruction(OpCode.MOVE, "R0", "#0");
                return;
            }
            int allocSize = count * 4;
            AddInstruction(OpCode.MOVE, "R0", $"#{allocSize}");
            AddInstruction(OpCode.SYSCALL, "#40");
            AddInstruction(OpCode.MOVE, "R1", "R0");
            for (int i = 0; i < count; i++)
            {
                node.Elements[i].Accept(this);
                AddInstruction(OpCode.MOVE, "R0", $"{i * 4}(R1)");
            }
            AddInstruction(OpCode.MOVE, "R0", "R1");
        }

        public void Visit(ArrayLiteralNode node)
        {
            int count = node.Elements.Count;
            // 布局: [count, e0, e1, …]（与 Visit(IndexAccessNode) 的 +4 一致）
            int allocSize = (count + 1) * 4;
            AddInstruction(OpCode.MOVE, "R0", $"#{allocSize}");
            AddInstruction(OpCode.SYSCALL, "#40");      // R0 = 块地址
            // ⚠ 原来是 `MOVE R1, R0` 之后就指望 R1 一直有效 —— 可 R1 会被元素表达式
            //   （尤其函数调用）改掉；更致命的是下面每一句写元素都写成了
            //   `MOVE R0, {off}(R1)`：`MOVE dest, src` 的 **dest 在前**，那是**从数组读**进 R0，
            //   元素值一个都没写进去（数组恒为 0），最后 `MOVE R0, R1` 还把上一个元素值当指针返回。
            //   **"操作数写反"族**（与 Go 0015、Swift 0011 同病）。
            //   现在基址常驻栈顶（元素表达式可能调函数，任何寄存器都靠不住）。
            AddInstruction(OpCode.PUSH, "R0");          // 基址入栈（此后栈顶恒为基址）
            AddInstruction(OpCode.MOVE, "R0", $"#{count}");
            AddInstruction(OpCode.POP, "R1");           // R1 = 基址
            AddInstruction(OpCode.PUSH, "R1");          // 立刻放回
            AddInstruction(OpCode.MOVE, "(R1)", "R0");  // [base] = count

            for (int i = 0; i < node.Elements.Count; i++)
            {
                node.Elements[i].Accept(this);          // R0 = 元素值
                AddInstruction(OpCode.POP, "R1");       // R1 = 基址
                AddInstruction(OpCode.PUSH, "R1");      // 放回
                int offset = (i + 1) * 4;
                AddInstruction(OpCode.MOVE, $"{offset}(R1)", "R0");   // [base+off] = 元素值
            }
            AddInstruction(OpCode.POP, "R0");           // 返回值 = 基址
        }

        public void Visit(IndexAccessNode node)
        {
            // 元素 i 在 `base + i*4 + 4`（跳过 [count, e0, …] 的头）。
            // ⚠ 原来用的是两个**全局暂存标签** `arrLabel`/`idxLabel`，而四句存取的**方向全写反了**
            //   （`MOVE R0, arrLabel` 是 **LEA**、`MOVE R0, idxLabel` 也是 LEA）⇒ 算出来的基址与下标
            //   当场被覆盖，最后从 `&__ia_idx` 之类的地方取值（野地址）。
            //   连标签暂存这条路本身也不该走：它是**全局的**，递归/重入时会被内层下标冲掉。
            //   改成纯 PUSH/POP（与 Go/Kotlin 同一套写法，天然嵌套安全）。
            node.Target.Accept(this);                   // R0 = 基址
            AddInstruction(OpCode.PUSH, "R0");
            node.Index.Accept(this);                    // R0 = 下标
            AddInstruction(OpCode.POP, "R1");           // R1 = 基址
            AddInstruction(OpCode.SHL, "R0", "#2");     // R0 = idx*4
            AddInstruction(OpCode.ADD, "R0", "#4");     // 跳过数组头
            AddInstruction(OpCode.ADD, "R0", "R1");     // R0 = 元素地址
            AddInstruction(OpCode.MOVE, "R0", "(R0)");  // R0 = 元素值（load）
        }

        public void Visit(MemberAccessNode node)
        {
            // clone() support: allocate new memory and copy bytes from source
            if (node.Member == "clone" && node.Arguments.Count == 0)
            {
                node.Target?.Accept(this);  // R0 = pointer to source struct/tuple
                string structName = "";
                if (node.Target is IdentifierNode idTarget && _variableStructTypes.TryGetValue(idTarget.Name, out string vt))
                    structName = vt;
                // Determine size from struct fields
                int fieldCount = 0;
                if (!string.IsNullOrEmpty(structName) && _structFields.TryGetValue(structName, out var fields))
                    fieldCount = fields.Count;
                else
                    fieldCount = 1; // default: at least 1 field
                int allocSize = (fieldCount + 1) * 4; // +1 for field count header
                AddInstruction(OpCode.PUSH, "R0");  // save source pointer
                AddInstruction(OpCode.MOVE, "R0", $"#{allocSize}");
                AddInstruction(OpCode.SYSCALL, "#40");  // alloc → R0 = dest pointer
                AddInstruction(OpCode.MOVE, "R2", "R0"); // R2 = dest
                AddInstruction(OpCode.POP, "R1");      // R1 = source pointer
                // Copy bytes: use word-by-word copy
                int words = allocSize / 4;
                for (int i = 0; i < words; i++)
                {
                    int byteOffset = i * 4;
                    AddInstruction(OpCode.MOVE, "R0", $"{byteOffset}(R1)");
                    AddInstruction(OpCode.MOVE, "R0", $"{byteOffset}(R2)");
                }
                AddInstruction(OpCode.MOVE, "R0", "R2");  // return dest pointer
                return;
            }

            // Method call: instance.method(args...)
            if (node.Arguments.Count > 0)
            {
                // Determine struct type from target
                string structName = "";
                if (node.Target is IdentifierNode id && _variableStructTypes.TryGetValue(id.Name, out string varStructType))
                    structName = varStructType;

                // Push self (target)
                node.Target?.Accept(this);
                AddInstruction(OpCode.PUSH, "R0");

                // Push args right-to-left
                for (int i = node.Arguments.Count - 1; i >= 0; i--)
                {
                    node.Arguments[i].Accept(this);
                    AddInstruction(OpCode.PUSH, "R0");
                }

                string methodLabel;
                if (!string.IsNullOrEmpty(structName))
                    methodLabel = $"method_{structName}_{node.Member}";
                else
                    methodLabel = $"method_{node.Member}_{node.Arguments.Count}";

                AddInstruction(OpCode.CALL, methodLabel);
                int totalArgs = node.Arguments.Count + 1;
                AddInstruction(OpCode.ADD, "R13", $"#{totalArgs * 4}");
            }
            else if (node.Member == "len" && node.Arguments.Count == 0)
            {
                EmitCompareToBool(() => { node.Target.Accept(this); AddInstruction(OpCode.MOVE, "R0", "(R0)"); }, OpCode.JE);
            }
            else if (node.Member == "capacity" && node.Arguments.Count == 0)
            {
                node.Target.Accept(this);
                AddInstruction(OpCode.MOVE, "R0", "#0");
            }
            else if (node.Member == "push" && node.Arguments.Count == 1)
            {
                node.Target.Accept(this);
                AddInstruction(OpCode.MOVE, "R1", "R0");
                AddInstruction(OpCode.MOVE, "R2", "(R1)");
                AddInstruction(OpCode.SHL, "R2", "#2");
                AddInstruction(OpCode.ADD, "R2", "#4");
                AddInstruction(OpCode.ADD, "R2", "R1");
                node.Arguments[0].Accept(this);
                AddInstruction(OpCode.MOVE, "R0", "(R2)");
                AddInstruction(OpCode.MOVE, "R0", "(R1)");
                AddInstruction(OpCode.ADD, "R0", "#1");
                AddInstruction(OpCode.MOVE, "R0", "(R1)");
            }
            else if (node.Member == "pop" && node.Arguments.Count == 0)
            {
                node.Target.Accept(this);
                AddInstruction(OpCode.MOVE, "R1", "R0");
                AddInstruction(OpCode.MOVE, "R0", "(R1)");
                AddInstruction(OpCode.SUB, "R0", "#1");
                AddInstruction(OpCode.MOVE, "R0", "(R1)");
                AddInstruction(OpCode.SHL, "R0", "#2");
                AddInstruction(OpCode.ADD, "R0", "#4");
                AddInstruction(OpCode.ADD, "R0", "R1");
                AddInstruction(OpCode.MOVE, "R0", "(R0)");
            }
            else if (node.Member == "contains" && node.Arguments.Count == 1)
            {
                string containsLabel = NewLabel("contains_found");
                string endLabel = NewLabel("contains_end");
                node.Target.Accept(this);
                AddInstruction(OpCode.PUSH, "R0");
                AddInstruction(OpCode.MOVE, "R1", "(R0)"); // R1 = len
                AddInstruction(OpCode.MOVE, "R0", "#0");  // R0 = i
                AddLabel(NewLabel("contains_loop"));
                // Use a different approach - let's build it properly
                // For now, simplified: linear search
                AddInstruction(OpCode.MOVE, "R3", "(R13)");
                // Simplified: always return false for now
                AddInstruction(OpCode.ADD, "R13", "#4");
                AddInstruction(OpCode.MOVE, "R0", "#0");
                AddLabel(endLabel);
            }
            else if (node.Arguments.Count == 0)
            {
                node.Target.Accept(this);
                string structName = "";
                if (node.Target is IdentifierNode id && _variableStructTypes.TryGetValue(id.Name, out string varStructType))
                {
                    structName = varStructType;
                }
                if (!string.IsNullOrEmpty(structName) && _structFields.TryGetValue(structName, out var fields))
                {
                    int fieldIndex = fields.FindIndex(f => f.Name == node.Member);
                    if (fieldIndex >= 0)
                    {
                        int offset = fieldIndex * 4;
                        if (offset > 0)
                            AddInstruction(OpCode.ADD, "R0", $"#{offset}");
                        AddInstruction(OpCode.MOVE, "R0", "(R0)");
                    }
                }
            }
            else
            {
                node.Target.Accept(this);
                AddInstruction(OpCode.PUSH, "R0");
                for (int i = node.Arguments.Count - 1; i >= 0; i--)
                {
                    node.Arguments[i].Accept(this);
                    AddInstruction(OpCode.PUSH, "R0");
                }
                string structName = "";
                if (node.Target is IdentifierNode id2 && _variableStructTypes.TryGetValue(id2.Name, out string varType2))
                {
                    structName = varType2;
                }
                string methodLabel = $"method_{structName}_{node.Member}";
                AddInstruction(OpCode.CALL, methodLabel);
                int totalArgs = node.Arguments.Count + 1;
                AddInstruction(OpCode.ADD, "R13", $"#{totalArgs * 4}");
            }
        }

        public void Visit(StructLiteralNode node)
        {
            // 结构体字面量: 分配内存并填充字段
            int fieldCount = node.Fields.Count;
            int allocSize = (fieldCount + 1) * 4;
            AddInstruction(OpCode.MOVE, "R0", $"#{allocSize}");
            AddInstruction(OpCode.SYSCALL, "#40");
            AddInstruction(OpCode.MOVE, "R1", "R0");
            AddInstruction(OpCode.MOVE, "R0", $"#{fieldCount}");
            AddInstruction(OpCode.MOVE, "R0", $"(R1)");
            
            for (int i = 0; i < fieldCount; i++)
            {
                node.Fields[i].Value.Accept(this);
                int offset = (i + 1) * 4;
                AddInstruction(OpCode.MOVE, "R0", $"{offset}(R1)");
            }
            AddInstruction(OpCode.MOVE, "R0", "R1");
        }
    }
}
