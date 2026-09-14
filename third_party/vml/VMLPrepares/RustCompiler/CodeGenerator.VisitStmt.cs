using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(IfStatementNode node)
        {
            Sta!.EmitIf(
                () => node.Condition.Accept(this),
                () => node.ThenBlock.Accept(this),
                node.ElseBlock != null ? () => node.ElseBlock.Accept(this) : null);
        }

        public void Visit(WhileStatementNode node)
        {
            Sta!.EmitWhile(
                () => node.Condition.Accept(this),
                () => node.Body.Accept(this));
        }
        
        public void Visit(ForStatementNode node)
        {
            _variableOffset -= 4;
            _variables[node.VariableName] = _variableOffset;
            RecordVarInScope(node.VariableName);

            if (node.RangeEnd == null)
            {
                // 集合迭代: for x in collection { body }
                string forStartLabel = NewLabel("for_start");
                string forEndLabel = NewLabel("for_end");
                string idxLabel = NewLabel("__for_idx");
                string arrLabel = NewLabel("__for_arr");
                dataSection[idxLabel] = 0;
                dataSection[arrLabel] = 0;

                node.RangeStart.Accept(this);
                AddInstruction(OpCode.MOVE, "R0", arrLabel);
                AddInstruction(OpCode.MOVE, "R0", "#0");
                AddInstruction(OpCode.MOVE, "R0", idxLabel);

                Sta!.PushLoopLabels(forEndLabel, forStartLabel);
                AddLabel(forStartLabel);
                AddInstruction(OpCode.MOVE, "R1", arrLabel);
                AddInstruction(OpCode.MOVE, "R2", "(R1)");
                AddInstruction(OpCode.MOVE, "R0", idxLabel);
                AddInstruction(OpCode.CMP, "R0", "R2");
                AddInstruction(OpCode.JGE, forEndLabel);

                // 加载元素: arr[len + index*4 + 4]
                AddInstruction(OpCode.MOVE, "R1", arrLabel);
                AddInstruction(OpCode.MOVE, "R0", idxLabel);
                AddInstruction(OpCode.SHL, "R0", "#2");
                AddInstruction(OpCode.ADD, "R0", "#4");
                AddInstruction(OpCode.ADD, "R0", "R1");
                AddInstruction(OpCode.MOVE, "R0", "(R0)");
                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");

                node.Body.Accept(this);

                AddInstruction(OpCode.MOVE, "R0", idxLabel);
                AddInstruction(OpCode.ADD, "R0", "#1");
                AddInstruction(OpCode.MOVE, "R0", idxLabel);
                AddInstruction(OpCode.JMP, forStartLabel);
                AddLabel(forEndLabel);
                Sta!.PopLoopLabels();
            }
            else
            {
                // 范围迭代: for x in start..end { body }
                string forStartLabel = NewLabel("for_start");
                string forEndLabel = NewLabel("for_end");

                node.RangeStart.Accept(this);
                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");

                Sta!.PushLoopLabels(forEndLabel, forStartLabel);
                AddLabel(forStartLabel);

                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                node.RangeEnd.Accept(this);
                AddInstruction(OpCode.MOVE, "R1", "R0");
                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                AddInstruction(OpCode.CMP, "R0", "R1");
                if (node.Inclusive)
                    AddInstruction(OpCode.JG, forEndLabel);
                else
                    AddInstruction(OpCode.JGE, forEndLabel);

                node.Body.Accept(this);

                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                AddInstruction(OpCode.ADD, "R0", "#1");
                AddInstruction(OpCode.MOVE, "R0", $"{_variableOffset}(R12)");
                AddInstruction(OpCode.JMP, forStartLabel);
                AddLabel(forEndLabel);
                Sta!.PopLoopLabels();
            }

            _variables.Remove(node.VariableName);
            _variableOffset += 4;
        }
        
        public void Visit(ReturnStatementNode node)
        {
            if (node.Value != null)
            {
                node.Value.Accept(this);
                // 返回值在R0中
            }
            
            EmitEpilogue();
        }
        
        public void Visit(ExpressionStatementNode node)
        {
            node.Expression.Accept(this);
            // 表达式值在R0中，可以忽略
        }
        
        public void Visit(BlockNode node)
        {
            EnterScope();
            
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }
            
            ExitScope();
        }
        
        public void Visit(CallExpressionNode node)
        {
            // 内置函数处理
            if (node.FunctionName == "Vec_new" && node.Arguments.Count == 0)
            {
                // Vec::new() → alloc(4), store len=0
                AddInstruction(OpCode.MOVE, "R0", "#4");
                AddInstruction(OpCode.SYSCALL, "#40");
                AddInstruction(OpCode.MOVE, "R1", "R0");
                AddInstruction(OpCode.MOVE, "R0", "#0");
                AddInstruction(OpCode.MOVE, "R0", "(R1)");
                AddInstruction(OpCode.MOVE, "R0", "R1");
                return;
            }
            if (node.FunctionName == "String_new" && node.Arguments.Count == 0)
            {
                // String::new() → alloc(4), store 0
                AddInstruction(OpCode.MOVE, "R0", "#4");
                AddInstruction(OpCode.SYSCALL, "#40");
                AddInstruction(OpCode.MOVE, "R1", "R0");
                AddInstruction(OpCode.MOVE, "R0", "#0");
                AddInstruction(OpCode.MOVE, "R0", "(R1)");
                AddInstruction(OpCode.MOVE, "R0", "R1");
                return;
            }
            if (node.FunctionName == "String_from" && node.Arguments.Count == 1)
            {
                // String::from("hello") → alloc string copy
                node.Arguments[0].Accept(this);
                // R0 = pointer to string literal
                // Simplified: just return the pointer
                return;
            }
            if (node.FunctionName == "format" && node.Arguments.Count > 0)
            {
                node.Arguments[0].Accept(this);
                return;
            }
            if (node.FunctionName.StartsWith("peek") && node.Arguments.Count == 1)
            {
                string runtimeFn = node.FunctionName;
                node.Arguments[0].Accept(this);
                AddInstruction(OpCode.CALL, runtimeFn);
                return;
            }
            if (node.FunctionName.StartsWith("poke") && node.Arguments.Count == 2)
            {
                string runtimeFn = node.FunctionName;
                node.Arguments[0].Accept(this);  // addr → R0
                AddInstruction(OpCode.PUSH, "R0");
                node.Arguments[1].Accept(this);  // val → R0
                AddInstruction(OpCode.MOVE, "R1", "R0"); // R1 = val
                AddInstruction(OpCode.POP, "R0");       // R0 = addr
                AddInstruction(OpCode.CALL, runtimeFn);
                return;
            }
            // Bit operations: bit_and, bit_or, bit_xor, bit_not, bit_shl, bit_shr
            var bitOps = new Dictionary<string, string> { {"bit_and","bit_and"}, {"bit_or","bit_or"}, {"bit_xor","bit_xor"}, {"bit_not","bit_not"}, {"bit_shl","bit_shl"}, {"bit_shr","bit_shr"} };
            if (bitOps.ContainsKey(node.FunctionName) && node.Arguments.Count >= 1)
            {
                node.Arguments[0].Accept(this);
                if (node.Arguments.Count >= 2) {
                    AddInstruction(OpCode.PUSH, "R0");
                    node.Arguments[1].Accept(this);
                    AddInstruction(OpCode.POP, "R1");
                }
                AddInstruction(OpCode.CALL, bitOps[node.FunctionName]);
                return;
            }
            if (node.FunctionName == "chipasm" && node.Arguments.Count >= 2)
            {
                // chipasm("arch", "code") — 转译必需，所有语言保留
                return;
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Rust 通过 Lib/shared/vmlsys.c 调用系统功能

            // Enum variant construction: EnumName_VariantName(args...)
            if (_enumVariants.TryGetValue(node.FunctionName, out var variantInfo))
            {
                int tag = variantInfo.index;
                if (variantInfo.payloadType != null && node.Arguments.Count == 1)
                {
                    // Data-carrying variant: allocate [tag, payload]
                    AddInstruction(OpCode.MOVE, "R0", "#8");
                    AddInstruction(OpCode.SYSCALL, "#40");
                    AddInstruction(OpCode.MOVE, "R1", "R0");
                    AddInstruction(OpCode.MOVE, "R0", $"#{tag}");
                    AddInstruction(OpCode.MOVE, "R0", "(R1)");
                    node.Arguments[0].Accept(this);
                    AddInstruction(OpCode.MOVE, "R0", "4(R1)");
                    AddInstruction(OpCode.MOVE, "R0", "R1");
                }
                else
                {
                    // Unit variant: allocate memory for tag so match can dereference
                    AddInstruction(OpCode.MOVE, "R0", "#4");
                    AddInstruction(OpCode.SYSCALL, "#40");
                    AddInstruction(OpCode.MOVE, "R1", "R0");
                    AddInstruction(OpCode.MOVE, "R0", $"#{tag}");
                    AddInstruction(OpCode.MOVE, "R0", "(R1)");
                    AddInstruction(OpCode.MOVE, "R0", "R1");
                }
                return;
            }

            // drop(x) — 显式析构：释放堆内存，清除借用/移动状态
            if (node.FunctionName == "drop" && node.Arguments.Count == 1)
            {
                if (node.Arguments[0] is IdentifierNode idArg)
                {
                    if (!IsCopyVariable(idArg.Name))
                    {
                        // 非Copy类型：释放堆内存 (SYSCALL 41 = free)
                        AddInstruction(OpCode.MOVE, "R0", $"{_variables[idArg.Name]}(R12)");
                        AddInstruction(OpCode.SYSCALL, "#41");
                    }
                    ClearMoved(idArg.Name);
                    ClearBorrowed(idArg.Name);
                    MarkMoved(idArg.Name);
                }
                else
                {
                    node.Arguments[0].Accept(this);
                }
                AddInstruction(OpCode.MOVE, "R0", "#0");
                return;
            }

            // conv 库函数首参数 C 类型: long→L0, double→D0 (按命名约定加载到正确寄存器文件)
            string? convArgType = ConvArgType(node.FunctionName);

            // 准备参数（从右到左）
            for (int i = node.Arguments.Count - 1; i >= 0; i--)
            {
                if (i == 0 && convArgType != null)
                {
                    GenerateConvArg(node.Arguments[i], convArgType);
                }
                else
                {
                    node.Arguments[i].Accept(this);
                }
                AddInstruction(OpCode.PUSH, "R0");
            }

            // 调用函数
            string funcLabel;
            if (node.FunctionName == "main")
                funcLabel = "main";
            else
            {
                // 标准库函数名映射 (编译器调用名 → stdlib 标签名)
                var stdlibMap = new Dictionary<string, string>
                {
                    { "print", "rust_print" },
                    { "println", "rust_println" },
                    { "eprintln", "rust_eprintln" },
                    { "read_line", "rust_read_line" },
                    { "str_len", "rust_str_len" },
                    { "Vec_new", "rust_vec_new" },
                    { "vec_push", "rust_vec_push" },
                    { "abs", "rust_abs" },
                    { "max", "rust_max" },
                    { "min", "rust_min" },
                    { "panic", "rust_panic" },
                    { "assert", "rust_assert" },
                    { "clear_screen", "rust_clear_screen" },
                    { "sleep", "rust_sleep" },
                    { "rand", "rust_rand" },
                };
                funcLabel = stdlibMap.TryGetValue(node.FunctionName, out var mapped)
                    ? mapped
                    : $"func_{node.FunctionName}";
            }
            AddInstruction(OpCode.CALL, funcLabel);

            // 清理参数栈
            if (node.Arguments.Count > 0)
            {
                AddInstruction(OpCode.ADD, "R13", $"#{node.Arguments.Count * 4}");
            }

            // 返回值在R0中
        }

        /// <summary>conv 库函数按命名约定确定首参数的 C 类型 (long→L0, double→D0)</summary>
        private static string? ConvArgType(string funcName)
        {
            var f = funcName.ToLower();
            if (f.StartsWith("long_") || f.StartsWith("ulong_") || f.StartsWith("longto") || f == "ltoa")
                return "long";
            if (f.StartsWith("double_") || f.StartsWith("doubleto") || f == "dtoa")
                return "double";
            return null;
        }

        /// <summary>按 C 类型加载 conv 首参数: long→MOVEL(L0), double→MOVED(D0)</summary>
        private void GenerateConvArg(ASTNode arg, string cType)
        {
            if (cType == "long" && arg is LiteralNode intLit && intLit.Type is "int" or "long")
            {
                long lv = Convert.ToInt64(intLit.Value);
                EmitLoadConstant(lv); // MOVEL 从数据段加载完整 64 位到 L0
                return;
            }
            if (cType == "double" && arg is LiteralNode flit && flit.Type == "float")
            {
                double dv = Convert.ToDouble(flit.Value);
                EmitLoadConstant(dv); // MOVED 从数据段加载 double 到 D0 (不用 float32 截断)
                return;
            }
            arg.Accept(this);
        }

    }
}
