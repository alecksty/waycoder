using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;
using CompilerBase;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(PrintlnStatementNode node)
        {
            if (node.Arguments.Count > 0)
            {
                if (node.Arguments[0] is LiteralNode formatLiteral && formatLiteral.Type == "string")
                {
                    ProcessFormatString(formatLiteral.Value?.ToString() ?? "", node.Arguments.Skip(1).ToList());
                    EmitPrintNewline();
                }
                else
                {
                    string errorLabel = NewLabel("error") ?? throw new CodeGenerationException("error标签为null");
                    dataSection[errorLabel] = "[错误: println! 的第一个实参必须是字符串字面量]";
                    AddInstruction(OpCode.MOVE, "R0", errorLabel);
                    EmitPrintString();
                }
            }
            else
            {
                EmitPrintNewline();
            }
        }
        
        public void Visit(CompoundAssignmentNode node)
        {
            var target = WrapTargetExpr(new IdentifierNode { Name = node.VariableName });
            _expr!.EmitCompoundAssign(target, WrapExpr(node.Value), node.Operator.TrimEnd('='));
        }
        
        // 辅助方法
        private void AddInstruction(OpCode opcode, params string[] operands)
        {
            var operandList = new List<Operand>();
            foreach (var operand in operands)
            {
                // 判断操作数类型
                OperandType type;
                object value;

                if (operand.StartsWith("R") && operand.Length > 1 && char.IsDigit(operand[1]))
                {
                    // 寄存器 - 整数索引
                    type = OperandType.REGISTER;
                    value = int.Parse(operand.Substring(1));
                }
                else if (operand.StartsWith("#"))
                {
                    // 立即数
                    type = OperandType.IMMEDIATE;
                    value = int.Parse(operand.Substring(1));
                }
                else if (operand.Contains("(R"))
                {
                    // 内存引用，如 "8(R12)"
                    type = OperandType.MEMORY;
                    value = operand;
                }
                else if (labels.ContainsKey(operand) || operand.StartsWith("L") || operand.StartsWith("func_"))
                {
                    // 标签
                    type = OperandType.LABEL;
                    value = operand;
                }
                else
                {
                    // 默认作为标签处理
                    type = OperandType.LABEL;
                    value = operand;
                }

                operandList.Add(new Operand(type, value));
            }

            AddInstruction(opcode, operandList);
        }
        
        private readonly Stack<HashSet<string>> _scopeImmutBorrowedStack = new();
        private readonly Stack<HashSet<string>> _scopeMutBorrowedStack = new();
        private readonly Stack<List<string>> _scopeLocalVars = new(); // 每作用域声明的变量名

        private void EnterScope()
        {
            _scopeStack.Push(new Dictionary<string, int>(_variables));
            _scopeMovedStack.Push(new HashSet<string>(_movedVariables));
            _scopeImmutBorrowedStack.Push(new HashSet<string>(_immutBorrowed));
            _scopeMutBorrowedStack.Push(new HashSet<string>(_mutBorrowed));
            _scopeLocalVars.Push(new List<string>());
        }

        private void ExitScope()
        {
            // Auto-drop: free heap memory for non-Copy variables declared in this scope
            // Dropped in reverse declaration order (Rust semantics)
            if (_scopeLocalVars.Count > 0)
            {
                var scopeVars = _scopeLocalVars.Peek();
                for (int i = scopeVars.Count - 1; i >= 0; i--)
                {
                    string varName = scopeVars[i];
                    // Only drop if: still alive (not moved), not borrowed, non-Copy type, still in scope
                    if (!_movedVariables.Contains(varName)
                        && !_immutBorrowed.Contains(varName)
                        && !_mutBorrowed.Contains(varName)
                        && _rustVarTypes.TryGetValue(varName, out RustType rt)
                        && HasDrop(rt)
                        && _variables.ContainsKey(varName))
                    {
                        // Load pointer and free heap memory (SYSCALL 41 = free)
                        AddInstruction(OpCode.MOVE, "R0", $"{_variables[varName]}(R12)");
                        AddInstruction(OpCode.SYSCALL, "#41");
                    }
                }
                _scopeLocalVars.Pop();
            }

            if (_scopeStack.Count > 0)
            {
                _variables.Clear();
                var previousScope = _scopeStack.Pop();
                foreach (var kvp in previousScope)
                    _variables[kvp.Key] = kvp.Value;
            }
            if (_scopeMovedStack.Count > 0)
            {
                var previousMoved = _scopeMovedStack.Pop();
                _movedVariables.Clear();
                foreach (var v in previousMoved)
                    _movedVariables.Add(v);
            }
            if (_scopeImmutBorrowedStack.Count > 0)
            {
                var prev = _scopeImmutBorrowedStack.Pop();
                _immutBorrowed.Clear();
                foreach (var v in prev) _immutBorrowed.Add(v);
            }
            if (_scopeMutBorrowedStack.Count > 0)
            {
                var prev = _scopeMutBorrowedStack.Pop();
                _mutBorrowed.Clear();
                foreach (var v in prev) _mutBorrowed.Add(v);
            }
        }

        /// <summary>
        /// 判断RustType是否为Copy类型（赋值时复制而非移动）
        /// </summary>
        private bool IsCopyType(RustType type)
        {
            return type == RustType.Int || type == RustType.I64
                || type == RustType.Float || type == RustType.F64
                || type == RustType.Bool || type == RustType.Char;
        }

        /// 判断RustType是否有Drop（非Copy类型需要析构）
        private bool HasDrop(RustType type) => !IsCopyType(type);

        /// 记录变量在当前作用域中声明（用于退出时自动drop）
        private void RecordVarInScope(string varName)
        {
            if (_scopeLocalVars.Count > 0)
                _scopeLocalVars.Peek().Add(varName);
        }

        /// <summary>
        /// 判断变量是否为Copy类型
        /// </summary>
        private bool IsCopyVariable(string varName)
        {
            if (_rustVarTypes.TryGetValue(varName, out RustType t))
                return IsCopyType(t);
            // 未知类型默认为int（Copy）
            return true;
        }

        /// <summary>
        /// 标记变量已被移动
        /// </summary>
        private void MarkMoved(string varName)
        {
            _movedVariables.Add(varName);
        }

        /// <summary>
        /// 清除变量的移动状态（let重新声明时）
        /// </summary>
        private void ClearMoved(string varName)
        {
            _movedVariables.Remove(varName);
        }

        /// <summary>
        /// 标记变量已被借用（&x 或 &mut x），并检查借用规则
        /// </summary>
        private void MarkBorrowed(string varName, bool isMut = false)
        {
            if (isMut)
            {
                // &mut x: 必须没有任何借用
                if (_immutBorrowed.Contains(varName))
                    throw new CodeGenerationException(
                        $"无法把 `{varName}` 借用为可变，因为它同时被借用为不可变");
                if (_mutBorrowed.Contains(varName))
                    throw new CodeGenerationException(
                        $"无法把 `{varName}` 同时借用为可变两次");
                _mutBorrowed.Add(varName);
            }
            else
            {
                // &x: 不能同时有可变借用
                if (_mutBorrowed.Contains(varName))
                    throw new CodeGenerationException(
                        $"无法把 `{varName}` 借用为不可变，因为它同时被借用为可变");
                _immutBorrowed.Add(varName);
            }
        }

        /// <summary>
        /// 判断变量是否正在被借用（任意类型）
        /// </summary>
        private bool IsBorrowed(string varName)
        {
            return _immutBorrowed.Contains(varName) || _mutBorrowed.Contains(varName);
        }

        /// <summary>
        /// 清除变量的借用状态（let重新声明时 / 赋值时）
        /// </summary>
        private void ClearBorrowed(string varName)
        {
            _immutBorrowed.Remove(varName);
            _mutBorrowed.Remove(varName);
        }
    }
}
