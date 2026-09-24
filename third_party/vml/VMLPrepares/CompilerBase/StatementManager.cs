using VMLAssembler;
using System.Collections.Generic;

namespace CompilerBase
{
    /// <summary>
    /// 统一控制语句管理 — EmitIf/EmitWhile/EmitFor/EmitDoWhile + break/continue 标签栈
    /// 所有编译器通过 StatementManager 生成控制流指令，消除手工 CMP+JZ+JMP 重复代码
    /// </summary>
    public class StatementManager
    {
        private readonly List<Instruction> _instructions;
        private readonly Dictionary<string, int> _labels;
        private readonly System.Func<string> _newLabel;

        // (breakLabel, continueLabel?) — break 跳到 breakLabel, continue 跳到 continueLabel
        // 用 List 而非 Stack，支持从新到旧遍历（switch 嵌套在循环中时 continue 需跳过 switch）
        private readonly List<(string Break, string? Continue)> _loopStack = new();

        public StatementManager(
            List<Instruction> instructions,
            Dictionary<string, int> labels,
            System.Func<string> newLabel)
        {
            _instructions = instructions;
            _labels = labels;
            _newLabel = newLabel;
        }

        // ====== If/Else ======

        /// <summary>
        /// emitCondition → CMP R0,#0 → JZ elseLabel → emitThen → JMP endLabel → elseLabel: emitElse? → endLabel:
        /// </summary>
        public void EmitIf(System.Action emitCondition, System.Action emitThen, System.Action? emitElse = null)
        {
            string elseLabel = _newLabel();
            string endLabel = _newLabel();

            emitCondition();
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
            _instructions.Add(new Instruction(OpCode.JZ, [Lbl(elseLabel)]));
            emitThen();

            if (emitElse != null)
            {
                _instructions.Add(new Instruction(OpCode.JMP, [Lbl(endLabel)]));
                _labels[elseLabel] = _instructions.Count;
                emitElse();
            }
            else
            {
                _labels[elseLabel] = _instructions.Count;
            }
            _labels[endLabel] = _instructions.Count;
        }

        /// <summary>
        /// EmitIfChain — 生成 if/elif/else 链，消除手工标签管理
        /// branches: [(condition, body), ...] — 第一个是 if，后续是 elif
        /// emitElse: 可选的最终 else 块
        /// 生成: cond0→CMP→JZ next0→body0→JMP end→next0: cond1→CMP→JZ next1→body1→JMP end→...→else?→end:
        /// </summary>
        public void EmitIfChain(
            List<(System.Action EmitCondition, System.Action EmitBody)> branches,
            System.Action? emitElse = null)
        {
            if (branches.Count == 0)
            {
                emitElse?.Invoke();
                return;
            }

            string endLabel = _newLabel();

            for (int i = 0; i < branches.Count; i++)
            {
                var (emitCondition, emitBody) = branches[i];
                bool hasNext = i < branches.Count - 1 || emitElse != null;
                string nextLabel = hasNext ? _newLabel() : endLabel;

                emitCondition();
                _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
                _instructions.Add(new Instruction(OpCode.JZ, [Lbl(nextLabel)]));
                emitBody();
                _instructions.Add(new Instruction(OpCode.JMP, [Lbl(endLabel)]));

                if (hasNext)
                    _labels[nextLabel] = _instructions.Count;
            }

            if (emitElse != null)
                emitElse();

            _labels[endLabel] = _instructions.Count;
        }

        // ====== While 循环 ======

        /// <summary>
        /// startLabel: emitCondition → CMP R0,#0 → JZ endLabel → emitBody → JMP startLabel → endLabel:
        /// </summary>
        public void EmitWhile(System.Action emitCondition, System.Action emitBody)
        {
            string startLabel = _newLabel();
            string endLabel = _newLabel();

            _loopStack.Add((endLabel, startLabel));

            _labels[startLabel] = _instructions.Count;
            // 确保 R2 初始化为 0 (用于空字符串比较等场景)
            _instructions.Add(new Instruction(OpCode.MOVE, [R(2), Imm(0)]));
            emitCondition();
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
            _instructions.Add(new Instruction(OpCode.JZ, [Lbl(endLabel)]));
            emitBody();
            _instructions.Add(new Instruction(OpCode.JMP, [Lbl(startLabel)]));
            _labels[endLabel] = _instructions.Count;

            _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        // ====== Do-While 循环 ======

        /// <summary>
        /// startLabel: emitBody → continueLabel: emitCondition → CMP R0,#0 → JNZ startLabel
        /// </summary>
        public void EmitDoWhile(System.Action emitBody, System.Action emitCondition)
        {
            string startLabel = _newLabel();
            string continueLabel = _newLabel();

            _loopStack.Add((continueLabel, continueLabel));

            _labels[startLabel] = _instructions.Count;
            emitBody();
            _labels[continueLabel] = _instructions.Count;
            emitCondition();
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
            _instructions.Add(new Instruction(OpCode.JNZ, [Lbl(startLabel)]));

            _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        /// <summary>
        /// Pascal 的 <c>repeat 语句序列 until 条件</c> —— 与上面的 <see cref="EmitDoWhile"/>
        /// **只差一处跳转条件**，而那一处正好是反的：
        ///
        /// <para>
        /// <c>do{…}while(c)</c> 是「**c 为真**就再来一遍」⇒ `JNZ`；
        /// <c>repeat…until(c)</c> 是「**c 为假**就再来一遍」（until = 直到…为止）⇒ **`JZ`**。
        /// </para>
        ///
        /// <para>
        /// ⚠ **这条是实测出来的**：Pascal 前端原先直接把 `RepeatNode` 路由到
        /// <see cref="EmitDoWhile"/> ⇒ 语义反转，而症状**不是报错、是静默跑错**：
        /// 条件一开始为假时循环体**只跑一遍**（`i:=0; repeat i:=i+1; Writeln(i); until i>=3;`
        /// 只打出 `1`），条件一开始就为真时**死循环**。老程序里 `repeat…until` 遍地都是，
        /// 所以这是"能编过、跑起来结果不对"里最隐蔽的一类。
        /// </para>
        ///
        /// <para>
        /// `continue` 仍应跳到**条件求值处**（Pascal 的 `Continue` = 直接去做 until 判断）
        /// ⇒ 标签栈与 do-while 同形，不用另立一套。
        /// </para>
        /// </summary>
        public void EmitRepeatUntil(System.Action emitBody, System.Action emitCondition)
        {
            string startLabel = _newLabel();
            string continueLabel = _newLabel();

            _loopStack.Add((continueLabel, continueLabel));

            _labels[startLabel] = _instructions.Count;
            emitBody();
            _labels[continueLabel] = _instructions.Count;
            emitCondition();
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
            _instructions.Add(new Instruction(OpCode.JZ, [Lbl(startLabel)]));   // ← 与 do-while 唯一的不同

            _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        // ====== For 循环 ======

        /// <summary>
        /// emitInit? → startLabel: emitCondition? → CMP R0,#0 → JZ endLabel → emitBody → continueLabel: emitIncrement? → JMP startLabel → endLabel:
        /// emitCondition 为 null 时生成无限循环 (for(;;))
        /// </summary>
        public void EmitFor(System.Action? emitInit, System.Action? emitCondition, System.Action? emitIncrement, System.Action emitBody)
        {
            string startLabel = _newLabel();
            string continueLabel = _newLabel();
            string endLabel = _newLabel();

            _loopStack.Add((endLabel, continueLabel));

            emitInit?.Invoke();

            _labels[startLabel] = _instructions.Count;
            if (emitCondition != null)
            {
                emitCondition();
                _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
                _instructions.Add(new Instruction(OpCode.JZ, [Lbl(endLabel)]));
            }

            emitBody();

            _labels[continueLabel] = _instructions.Count;
            emitIncrement?.Invoke();
            _instructions.Add(new Instruction(OpCode.JMP, [Lbl(startLabel)]));
            _labels[endLabel] = _instructions.Count;

            _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        // ====== Switch / Case ======

        /// <summary>
        /// EmitSwitch — 生成 switch/case 控制流（整数 case 值）
        /// 使用 PUSH/POP 保护 switch 值，防止 case 表达式修改 R0
        /// 流程: emitSwitchValue → PUSH R0 → 每个case: POP R1, CMP R1,#val, PUSH R1, JE body
        ///       → POP R0(清理) → JMP default → case bodies → endLabel
        /// fallthrough=false 时每个 case body 末尾自动加 JMP endLabel
        /// </summary>
        public void EmitSwitch(
            System.Action emitSwitchValue,
            List<(int CaseValue, System.Action EmitBody)> cases,
            System.Action? defaultBody = null,
            bool fallthrough = false)
        {
            string endLabel = _newLabel();
            string defaultLabel = defaultBody != null ? _newLabel() : endLabel;

            _loopStack.Add((endLabel, null)); // break 跳转到 endLabel, continue 跳过 switch

            // PUSH switch 值到栈上保护
            emitSwitchValue();                                      // R0 = switch value
            _instructions.Add(new Instruction(OpCode.PUSH, [R(0)]));

            // 比较链: POP R1, CMP R1,#val, PUSH R1, JE bodyLabel
            var caseBodyLabels = new List<string>();
            for (int i = 0; i < cases.Count; i++)
            {
                var caseBodyLabel = _newLabel();
                caseBodyLabels.Add(caseBodyLabel);
                _instructions.Add(new Instruction(OpCode.POP, [R(1)]));
                _instructions.Add(new Instruction(OpCode.CMP, [R(1), Imm(cases[i].CaseValue)]));
                _instructions.Add(new Instruction(OpCode.PUSH, [R(1)]));
                _instructions.Add(new Instruction(OpCode.JE, [Lbl(caseBodyLabel)]));
            }

            // 无匹配: POP 清理栈 + JMP default
            _instructions.Add(new Instruction(OpCode.POP, [R(0)]));
            _instructions.Add(new Instruction(OpCode.JMP, [Lbl(defaultLabel)]));

            for (int i = 0; i < cases.Count; i++)
            {
                _labels[caseBodyLabels[i]] = _instructions.Count;
                cases[i].EmitBody();
                if (!fallthrough)
                    _instructions.Add(new Instruction(OpCode.JMP, [Lbl(endLabel)]));
            }

            if (defaultBody != null)
            {
                _labels[defaultLabel] = _instructions.Count;
                defaultBody();
            }

            _labels[endLabel] = _instructions.Count;
            _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        /// <summary>
        /// EmitSwitchCustom — 自定义比较逻辑的 switch（字符串 case、范围 case 等）
        /// switch 值保存在 R1 中，每个 case 前 PUSH R1 保护，emitCaseValue 后 POP R1 恢复
        /// emitCaseValue 只需把 case 值放入 R0，由本方法负责 CMP R0,R1 + JE
        /// 流程: emitSwitchValue → MOVE R1,R0 → 每个case: PUSH R1, emitCaseValue, POP R1, CMP R0,R1, JE
        ///       → JMP default → case bodies → endLabel
        /// </summary>
        public void EmitSwitchCustom(
            System.Action emitSwitchValue,
            List<System.Action> emitCaseValues,
            List<System.Action> caseBodies,
            System.Action? defaultBody = null,
            bool fallthrough = false)
        {
            string endLabel = _newLabel();
            string defaultLabel = defaultBody != null ? _newLabel() : endLabel;

            _loopStack.Add((endLabel, null));

            // switch 值保存到 R1（寄存器保护）
            emitSwitchValue();                                      // R0 = switch value
            _instructions.Add(new Instruction(OpCode.MOVE, [R(1), R(0)]));

            // 比较链: PUSH R1(保护), emitCaseValue→R0, POP R1(恢复), CMP R0,R1, JE body
            var caseBodyLabels = new List<string>();
            for (int i = 0; i < emitCaseValues.Count; i++)
            {
                var bodyLabel = _newLabel();
                caseBodyLabels.Add(bodyLabel);

                _instructions.Add(new Instruction(OpCode.PUSH, [R(1)]));
                emitCaseValues[i]();                                // R0 = case value
                _instructions.Add(new Instruction(OpCode.POP, [R(1)]));
                _instructions.Add(new Instruction(OpCode.CMP, [R(0), R(1)]));
                _instructions.Add(new Instruction(OpCode.JE, [Lbl(bodyLabel)]));
            }

            // 无匹配
            _instructions.Add(new Instruction(OpCode.JMP, [Lbl(defaultLabel)]));

            for (int i = 0; i < caseBodies.Count; i++)
            {
                _labels[caseBodyLabels[i]] = _instructions.Count;
                caseBodies[i]();
                if (!fallthrough)
                    _instructions.Add(new Instruction(OpCode.JMP, [Lbl(endLabel)]));
            }

            if (defaultBody != null)
            {
                _labels[defaultLabel] = _instructions.Count;
                defaultBody();
            }

            _labels[endLabel] = _instructions.Count;
            _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        // ====== Break / Continue ======

        public void PushLoopLabels(string breakLabel, string? continueLabel)
        {
            _loopStack.Add((breakLabel, continueLabel));
        }

        public void PopLoopLabels()
        {
            if (_loopStack.Count > 0)
                _loopStack.RemoveAt(_loopStack.Count - 1);
        }

        public (string Break, string? Continue) GetLoopLabels()
        {
            return _loopStack.Count > 0 ? _loopStack[_loopStack.Count - 1] : (null!, null);
        }

        public bool HasLoopLabels => _loopStack.Count > 0;

        /// <summary>用于检测标注语句(labeled statement)是否推入了新标签</summary>
        public int LoopStackCount => _loopStack.Count;

        /// <summary>JMP 到最内层循环/switch 的 breakLabel</summary>
        public void EmitBreak()
        {
            if (_loopStack.Count > 0)
            {
                var (breakLabel, _) = _loopStack[_loopStack.Count - 1];
                _instructions.Add(new Instruction(OpCode.JMP, [Lbl(breakLabel)]));
            }
        }

        /// <summary>JMP 到最内层循环的 continueLabel（从新到旧遍历，跳过 switch 的 null continueLabel）</summary>
        public void EmitContinue()
        {
            for (int i = _loopStack.Count - 1; i >= 0; i--)
            {
                var (_, continueLabel) = _loopStack[i];
                if (continueLabel != null)
                {
                    _instructions.Add(new Instruction(OpCode.JMP, [Lbl(continueLabel)]));
                    return;
                }
            }
        }

        // ====== 手工发射（特殊控制流场景） ======

        public string NewLabel() => _newLabel();

        public void PlaceLabel(string label)
        {
            _labels[label] = _instructions.Count;
        }

        public void EmitJump(string label)
        {
            _instructions.Add(new Instruction(OpCode.JMP, [Lbl(label)]));
        }

        /// <summary>CMP R0, #0 + JZ label</summary>
        public void EmitJumpIfFalse(string label)
        {
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
            _instructions.Add(new Instruction(OpCode.JZ, [Lbl(label)]));
        }

        /// <summary>CMP R0, #0 + JNZ label</summary>
        public void EmitJumpIfTrue(string label)
        {
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
            _instructions.Add(new Instruction(OpCode.JNZ, [Lbl(label)]));
        }

        /// <summary>CMP R0, #0 — 设标志位供手动跳转</summary>
        public void EmitCmpZero()
        {
            _instructions.Add(new Instruction(OpCode.CMP, [R(0), Imm(0)]));
        }

        public void EmitReturn()
        {
            _instructions.Add(new Instruction(OpCode.RET, []));
        }

        // ====== 断言 ======

        /// <summary>
        /// EmitAssert — 生成断言检查代码。emitCondition 将条件值放入 R0。
        /// 如果 R0==0，调用 vml_assert (SYSCALL #7) 并附带可选的失败消息标签。
        /// 所有编译器统一使用此方法，消除手工 TEST+JNZ+SYSCALL 重复代码。
        /// </summary>
        /// <param name="emitCondition">生成条件表达式，结果 0=失败 非0=通过</param>
        /// <param name="message">可选的失败消息标签（data section 中的字符串）</param>
        public void EmitAssert(System.Action emitCondition, string? message = null)
        {
            string passLabel = _newLabel();

            emitCondition();
            _instructions.Add(new Instruction(OpCode.TEST, [R(0), R(0)]));
            _instructions.Add(new Instruction(OpCode.JNZ, [Lbl(passLabel)]));

            // 失败：加载消息地址（如果有）+ SYSCALL #7 (ASSERT)
            if (message != null)
                _instructions.Add(new Instruction(OpCode.MOVE, [R(0), Lbl(message)]));
            else
                _instructions.Add(new Instruction(OpCode.MOVE, [R(0), Imm(0)]));

            _instructions.Add(new Instruction(OpCode.SYSCALL, [Imm(7)]));
            _labels[passLabel] = _instructions.Count;
        }

        /// <summary>
        /// EmitCall — 发射 CALL 到命名函数（用于调用 builtins.vml 中的共享实现）
        /// 编译器应将此用于标准库函数调用而非内联生成等价代码
        /// </summary>
        public void EmitCall(string funcName)
        {
            _instructions.Add(new Instruction(OpCode.CALL, [Lbl(funcName)]));
        }

        // ====== 寄存器/标签语法糖 ======

        private static Operand R(int reg) => new(OperandType.REGISTER, reg);
        private static Operand Imm(int value) => new(OperandType.IMMEDIATE, value);
        private static Operand Lbl(string label) => new(OperandType.LABEL, label);
    }
}
