using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VMLAssembler;
using CompilerBase;

namespace LuaCompiler
{
    public partial class CodeGenerator : TypedCodeGen<LuaType>
    {
        private void GenerateFunctionDefinition(FunctionDefinitionNode node)
        {
            string afterFuncLabel = $"func_end_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            Sta!.EmitJump(afterFuncLabel);

            // 函数级注释
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");
            var sourceDecl = $"function {node.Name}(";
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                sourceDecl += node.Parameters[i];
                if (i < node.Parameters.Count - 1) sourceDecl += ",";
            }
            sourceDecl += ")";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {node.Name}"));
            foreach (var param in node.Parameters)
            {
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param   : {param}"));
            }
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; return   : ..."));
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");

            // 函数标签
            string funcLabel = $"func_{SanitizeFunctionName(node.Name)}";
            AddLabel(funcLabel);
            
            // 函数序言: 保存帧指针
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 12) },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 12),
                    new Operand(OperandType.REGISTER, 13)
                }, instructions.Count));
            
            // 保存参数到栈帧(CCv2: 第一个参数在R0)
            // 注意: [R12+0]是保存的R12值，参数从[R12-4]开始
            nextStackOffset = 4; // 跳过[R12+0](保存的帧指针)
            for (int pi = 0; pi < node.Parameters.Count; pi++)
            {
                string pname = node.Parameters[pi];
                if (!symbolTable.ContainsKey(pname))
                {
                    symbolTable[pname] = nextStackOffset;
                    nextStackOffset += 4;
                }
                // ⚠ **每个形参都从栈上取**（2026-09-17 调用约定统一后改的）。
                //
                // 先前：调用方把第 i 个实参放 **R{i}**（`MOVE R{i}, [R13-(i+1)*4]`）、
                // 这里对应地从 R{pi} 取，两边自洽但只在「前 4 个 + 只有 Lua 自己调」时成立
                //（且更早只实现了 pi == 0 ⇒ 第二个及以后的形参读到的是未初始化内容）。
                // 现在调用点真压栈、其它语言与库也都从栈读 ⇒ 这里统一成 `[R12+12+4i]`，
                // 与 C 编译出来的函数同一口径，**第 4 个之后的形参也一并支持**。
                // 取到 R0 再存回帧内槽（VML 没有内存到内存的搬移）。
                {
                    int off = symbolTable[pname];
                    instructions.Add(new Instruction(OpCode.MOVE,
                        new List<Operand> {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.MEMORY, $"R12+{12 + pi * 4}")
                        }, instructions.Count));
                    instructions.Add(new Instruction(OpCode.MOVE,
                        new List<Operand> {
                            new Operand(OperandType.MEMORY, $"R12-{off}"),
                            new Operand(OperandType.REGISTER, 0)
                        }, instructions.Count));
                }
            }
            // 分配栈空间给参数 —— 实际预留的是**整帧**（参数 + 局部变量），
            // 局部变量是边生成边分配的、此刻还不知道有多少，故先占位、生成完回填。
            // 不预留的后果与 main 相同：局部变量落在 SP 之下，被 PUSH/CALL 写花。
            int framePatchIndex = instructions.Count;
            instructions.Add(new Instruction(OpCode.SUB,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, 0)
                }, framePatchIndex));

            // 生成函数体
            foreach (var stmt in node.Body)
            {
                GenerateStatement(stmt);
            }

            // 回填帧大小（生成函数体时又把 [R12-4] 之类的临时槽用了一遍，所以这一步不能省）
            instructions[framePatchIndex] = new Instruction(OpCode.SUB,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, ComputeFrameSize())
                }, framePatchIndex);

            // 函数尾声: 恢复帧指针并返回
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.REGISTER, 12)
                }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 12) },
                instructions.Count));
            Sta!.EmitReturn();

            AddLabel(afterFuncLabel);
        }
        
        private void GenerateIfStatement(IfStatementNode node)
        {
            var branches = new List<(System.Action, System.Action)>();
            foreach (var (condition, body) in node.Conditions)
            {
                var c = condition;
                var b = body;
                branches.Add((() => GenerateExpression(c), () =>
                {
                    foreach (var stmt in b) GenerateStatement(stmt);
                }));
            }

            Sta!.EmitIfChain(branches,
                node.ElseBody != null && node.ElseBody.Count > 0
                    ? () => { foreach (var stmt in node.ElseBody) GenerateStatement(stmt); }
                    : null);
        }
        
        private void GenerateWhileStatement(WhileStatementNode node)
        {
            Sta!.EmitWhile(
                () => GenerateExpression(node.Condition),
                () => { foreach (var stmt in node.Body) GenerateStatement(stmt); });
        }

        private void GenerateRepeatStatement(RepeatStatementNode node)
        {
            Sta!.EmitDoWhile(
                () => { foreach (var stmt in node.Body) GenerateStatement(stmt); },
                () => GenerateExpression(node.Condition));
        }
        
        private void GenerateForStatement(ForStatementNode node)
        {
            string startLabel = $"for_start_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string endLabel = $"for_end_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            Sta!.PushLoopLabels(endLabel, null);

            // 初始化变量
            GenerateExpression(node.Start);
            string varLabel = $"var_{node.Variable}";
            // **循环变量存在哪儿必须和循环体看到的那个位置一致**：
            // 循环体里的 `i` 是按 symbolTable 解析的（局部变量 → [R12-off]），
            // 而这里原先无条件写数据段的全局 `var_i` ⇒ 「先 local i 再 for i = …」的写法下
            // 循环推进的是全局、循环体读的是局部，两者永不相等（语料 skel.lua 里 i 恒为 0 就是这么来的）。
            // 该名字已经是局部变量就写它那一格；否则维持原行为（写全局，循环体也解析到同一个全局）。
            bool varIsLocal = symbolTable.TryGetValue(node.Variable, out int varOffset);
            if (!varIsLocal)
            {
                // ⚠ **循环变量必须有自己的局部槽**（2026-09-17 修）。
                //
                // 原先没预声明 `local i` 时回退到数据段的全局 `var_i`，而那条路是坏的：
                // 循环头发的是 `move R1 var_i`（把标签当**地址**取），循环体读 `i` 发的却是
                // `move R0 [var_i]`（按地址**取值**）—— 同一个标签两种解读方式。
                // 于是 R1 拿到一个远大于上界的地址 ⇒ `cmp/jg` 立刻跳出，**循环体一次都不执行**。
                // 实测：`for i = 1, 4 do s = s + a[i] end` 得 0；
                //       前面补一句 `local i = 0` 就对（得 10）—— 因为那时走的是局部槽这条一致的路。
                // 语料 `corpus/lua/skel.lua` 恰好写了 `local i = 0`，所以这条路径一直没被覆盖。
                //
                // 现在无条件分配一格，两种写法走同一条路（`[R12-off]`）。
                varOffset = nextStackOffset;
                symbolTable[node.Variable] = varOffset;
                nextStackOffset += 4;
                varIsLocal = true;
            }
            Operand VarStoreTarget() => new Operand(OperandType.MEMORY, $"R12-{varOffset}");

            // 存储初始值到变量（GenerateExpression结果在R0中）
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    VarStoreTarget(),
                    new Operand(OperandType.REGISTER, 0)
                },
                instructions.Count));

            // 循环开始标签
            AddLabel(startLabel);

            // 检查循环条件
            // 加载当前值
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 1),
                    VarStoreTarget()
                },
                instructions.Count));

            // 加载结束值
            GenerateExpression(node.End);
            
            // 比较当前值 <= 结束值
            instructions.Add(new Instruction(OpCode.CMP, 
                new List<Operand> { 
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }, 
                instructions.Count));
            instructions.Add(new Instruction(OpCode.JG, 
                new List<Operand> { 
                    new Operand(OperandType.LABEL, endLabel)
                }, 
                instructions.Count));
            
            // 保存R1（循环计数器）并在循环体后恢复
            instructions.Add(new Instruction(OpCode.PUSH, 
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, 
                instructions.Count));
            
            foreach (var stmt in node.Body)
            {
                GenerateStatement(stmt);
            }
            
            instructions.Add(new Instruction(OpCode.POP, 
                new List<Operand> { new Operand(OperandType.REGISTER, 1) }, 
                instructions.Count));
            
            // 增加步长
            if (node.Step != null)
            {
                GenerateExpression(node.Step);
                instructions.Add(new Instruction(OpCode.ADD, 
                    new List<Operand> { 
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 0)
                    }, 
                    instructions.Count));
            }
            else
            {
                // 默认步长为1
                instructions.Add(new Instruction(OpCode.ADD, 
                    new List<Operand> { 
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.IMMEDIATE, 1)
                    }, 
                    instructions.Count));
            }
            
            // 保存新值
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    VarStoreTarget(),
                    new Operand(OperandType.REGISTER, 1)
                },
                instructions.Count));

            // 跳回循环开始
            Sta!.EmitJump(startLabel);

            // 循环结束标签
            AddLabel(endLabel);
            Sta!.PopLoopLabels();
        }

        private void GenerateForInStatement(ForInStatementNode node)
        {
            string uid = Guid.NewGuid().ToString("N").Substring(0, 8);
            string loopLabel = $"forin_{uid}";
            string endLabel = $"forin_end_{uid}";
            Sta!.PushLoopLabels(endLabel, null);

            // 计算迭代器表达式（如 pairs(t)），结果在 R0
            GenerateExpression(node.IteratorExpr);

            // 保存迭代器状态到局部变量
            string valVar = node.Variables.Count > 1 ? node.Variables[1] : node.Variables[0];
            string idxVar = node.Variables[0];

            // 初始化 key = nil (0)
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0)]));

            // 循环开始
            AddLabel(loopLabel);

            // 调用迭代器 next(state, key) → key, val
            // For simplicity, iterate from 1..N using a counter for ipairs-like behavior
            instructions.Add(new Instruction(OpCode.ADD,
                [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1)]));

            // 尝试从表地址 + key*4 加载值
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));

            // 检查是否为 nil (0)
            instructions.Add(new Instruction(OpCode.CMP,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.JE,
                [new Operand(OperandType.LABEL, endLabel)]));

            // 赋值到循环变量
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1)])); // key
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0)])); // val

            // 循环体
            foreach (var stmt in node.Body)
                GenerateStatement(stmt);

            // 跳回
            Sta!.EmitJump(loopLabel);

            // 结束
            AddLabel(endLabel);
            Sta!.PopLoopLabels();
        }

        private void GenerateReturnStatement(ReturnStatementNode node)
        {
            if (node.Values != null && node.Values.Count > 0)
            {
                // 生成返回值到R0
                GenerateExpression(node.Values[0]);
            }
            else
            {
                // 默认返回0
                instructions.Add(new Instruction(OpCode.MOVE, 
                    new List<Operand> { 
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 0)
                    }, 
                    instructions.Count));
            }
            
            // 恢复帧指针并返回
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.REGISTER, 12)
                }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 12) },
                instructions.Count));
            Sta!.EmitReturn();
        }
        
        private static readonly Dictionary<string, string> LuaBuiltinFunctions = new()
        {
            { "print", "lua_print" },
            { "type", "lua_type" },
            { "tonumber", "lua_tonumber" },
            { "tostring", "lua_tostring" },
            { "pairs", "lua_pairs" },
            { "ipairs", "lua_ipairs" },
            { "next", "lua_next" },
            { "clear", "lua_clear" },
            { "sleep", "lua_sleep" },
            { "io.read", "io_read" },
            { "io.write", "io_write" },
            { "io.open", "io_open" },
            { "io.close", "io_close" },
            { "io.seek", "io_seek" },
            { "os.exit", "os_exit" },
            { "os.time", "os_time" },
            { "string.len", "string_len" },
            { "string.sub", "string_sub" },
            { "string.find", "string_find" },
            { "string.upper", "string_upper" },
            { "string.lower", "string_lower" },
            { "string.reverse", "string_reverse" },
            { "string.rep", "string_rep" },
            { "string.char", "string_char" },
            { "string.byte", "string_byte" },
            { "table.insert", "table_insert" },
            { "table.remove", "table_remove" },
            { "table.concat", "table_concat" },
            { "table.sort", "table_sort" },
            { "math.abs", "math_abs" },
            { "math.max", "math_max" },
            { "math.min", "math_min" },
            { "math.floor", "math_floor" },
            { "math.ceil", "math_ceil" },
            { "math.sqrt", "math_sqrt" },
            { "math.pow", "math_pow" },
            { "math.exp", "math_exp" },
            { "math.log", "math_log" },
            { "math.sin", "math_sin" },
            { "math.cos", "math_cos" },
            { "math.tan", "math_tan" },
            { "math.asin", "math_asin" },
            { "math.acos", "math_acos" },
            { "math.atan", "math_atan" },
            { "math.random", "math_random" },
            { "math.randomseed", "math_randomseed" },
            { "math.pi", "math_pi" },
            { "string.format", "string_format" },
            { "string.match", "string_match" },
            { "string.gmatch", "string_gmatch" },
            { "string.gsub", "string_gsub" },
            { "require", "lua_require" },
            { "module", "lua_module" },
            { "package.path", "lua_package_path" },
            { "setmetatable", "lua_setmetatable" },
            { "getmetatable", "lua_getmetatable" },
            { "coroutine.create", "coroutine_create" },
            { "coroutine.resume", "coroutine_resume" },
            { "coroutine.yield", "coroutine_yield" },
            // MCU bit operations
            { "peek", "peek" },
            { "poke", "poke" },
            { "bit_and", "bit_and" },
            { "bit_or", "bit_or" },
            { "bit_xor", "bit_xor" },
            { "bit_not", "bit_not" },
            { "bit_shl", "bit_shl" },
            { "bit_shr", "bit_shr" },
            { "coroutine.status", "coroutine_status" },
            { "coroutine.wrap", "coroutine_wrap" },
            { "file.read", "file_read" },
            { "file.write", "file_write" },
            { "file.seek", "file_seek" },
        };

        private string SanitizeFunctionName(string name)
        {
            return name.Replace('.', '_').Replace(':', '_');
        }

        private bool TryResolveFunctionName(ASTNode function, out string name)
        {
            if (function is IdentifierNode identifier)
            {
                name = identifier.Name;
                return true;
            }

            if (function is TableAccessNode tableAccess &&
                tableAccess.Key is ConstantNode key && key.Type == "string" && key.Value is string keyName &&
                TryResolveFunctionName(tableAccess.Table, out string tableName))
            {
                name = $"{tableName}.{keyName}";
                return true;
            }

            name = string.Empty;
            return false;
        }

        private void GenerateFunctionExpression(FunctionExpressionNode node)
        {
            GenerateFunctionDefinition(new FunctionDefinitionNode(node.GeneratedName, node.Parameters, node.Body, true, node.Line, node.Column));
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0)
                },
                instructions.Count));
        }

        private void GenerateFunctionCall(FunctionCallNode node)
        {
            // 提前检查内置函数（避免生成参数代码）
            if (TryResolveFunctionName(node.Function, out string funcName))
            {
                if (funcName == "chipasm" && node.Arguments.Count >= 2)
                    return;
                // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Lua 通过 Lib/shared/vmlsys.c 调用系统功能
            }

            // 生成参数：**真·右到左压栈**（2026-09-17 调用约定统一后改的）
            //
            // 原先那句「保存参数到栈」只有一半：`MOVE [R13-(i+1)*4], R0` 是往 SP **下方**写，
            // **R13 全程不动** —— 它不是压栈，而是把实参写进「被调方建帧后马上会覆盖」的区域；
            // 紧跟着再把实参镜像进 R0-R3，指望被调方从**寄存器**取参（旧寄存器 ABI）。
            // 统一之后实参一律在栈上、被调方也从栈读，这半套就不成立了：
            // 实测 `scripts/vml-abi-probe/langs/abi.lua` 的 `ipow(2,3)` 得 **0**。
            //
            // 现在：右到左真压栈，调用点后面由调用方清（每个实参 4 字节）。
            // 镜像那一段**删掉**了 —— 该做的事现在由 `Lib/{lang}` 的包装器统一做一次，
            // 不再是每个前端各自记得做。
            if (node.Arguments != null)
            {
                for (int i = node.Arguments.Count - 1; i >= 0; i--)
                {
                    GenerateExpression(node.Arguments[i]);
                    instructions.Add(new Instruction(OpCode.PUSH,
                        new List<Operand> { new Operand(OperandType.REGISTER, 0) },
                        instructions.Count));
                }
            }
            
            // 调用函数
            if (TryResolveFunctionName(node.Function, out string functionName))
            {
                if (functionName == "print" && node.Arguments.Count >= 1)
                {
                    // MCU print: iterate arguments, output via SYSCALL / library (v1.66.41: add float support)
                    for (int i = 0; i < node.Arguments.Count; i++)
                    {
                        var arg = node.Arguments[i];
                        GenerateExpression(arg); // R0 = value/address
                        // String literals → SYSCALL #1 (OutputString)
                        if (arg is ConstantNode cn && cn.Type == "string")
                            instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 1)]));
                        // Function returning string → SYSCALL #1
                        else if (arg is FunctionCallNode fcn && TryResolveFunctionName(fcn.Function, out var retName) && IsStringReturningFunc(retName))
                            instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 1)]));
                        // Float → vml_print_float
                        // ⚠ 必须要求**真的带小数**：词法器把数字字面量一律存成 double，
                        // 只看 `Value is double` 的话 `print(5)` 也会走这条 ⇒ SYSCALL #8 把
                        // 整数位型当浮点数解释，打出 0（实测 `print(5)` → 0，而 `print(2+3)` → 5）。
                        else if (arg is ConstantNode cnf && cnf.Type == "number"
                                 && cnf.Value is double dv && dv != Math.Floor(dv))
                            EmitPrintFloat();
                        else
                        {
                            instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                            instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "lua_print1")])); // print single value (int/char/bool)
                        }
                    }
                    // newline
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10)]));
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 4)])); // putchar '\n'
                    return;
                }
                if (functionName == "string.len" && node.Arguments.Count >= 1)
                {
                    EmitStrLen(() => GenerateExpression(node.Arguments[0]));
                    return;
                }
                if (functionName == "string.byte" && node.Arguments.Count >= 1)
                {
                    // CALL shared_str_charat(str, index) — __stdcall: PUSH right-to-left
                    if (node.Arguments.Count >= 2)
                        GenerateExpression(node.Arguments[1]);  // R0 = index (1-based)
                    else
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)])); // 1-based → 0-based
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // PUSH index (2nd param)
                    GenerateExpression(node.Arguments[0]);  // R0 = string ptr
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // PUSH str (1st param)
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "str_charat")]));
                    return;
                }
                if (functionName == "tonumber")
                {
                    // CALL shared_atoi — 字符串→整数
                    if (node.Arguments.Count >= 1) {
                        GenerateExpression(node.Arguments[0]); // R0 = string ptr
                        instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "atoi")]));
                        instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]));
                    }
                    return;
                }
                if (functionName == "tostring")
                {
                    // CALL shared_itoa — 整数→字符串
                    // C栈传参(右→左): PUSH buffer → PUSH value → CALL
                    if (node.Arguments.Count >= 1) {
                        GenerateExpression(node.Arguments[0]); // R0 = value
                        instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // save value
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 12)]));
                        instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 40)])); // malloc 12 → buffer
                        instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // PUSH buffer (右参)
                        // value is still on stack at [R13+4]
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R13+4")])); // R0 = value
                        instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R13+4"), new Operand(OperandType.REGISTER, 0)])); // move value to [R13+4]
                        instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "itoa")]));
                        instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 8)])); // clean 2 params
                    }
                    return;
                }
                // 数学库函数
                if (functionName == "math.abs" && node.Arguments.Count >= 1) {
                    // CALL shared_abs (__stdcall: PUSH arg, callee cleans stack)
                    GenerateExpression(node.Arguments[0]);  // R0 = value
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "abs")]));
                    return;
                }
                if (functionName == "math.pow" && node.Arguments.Count >= 2) {
                    // CALL shared_ipow(base, exp) — C栈传参 (右→左)
                    GenerateExpression(node.Arguments[1]); // R0 = exp
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]); // R0 = base
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "ipow")]));
                    return;
                }
                if (functionName == "math.floor" && node.Arguments.Count >= 1) {
                    GenerateExpression(node.Arguments[0]); // floor is a no-op for integers
                    return;
                }
                if (functionName == "math.ceil" && node.Arguments.Count >= 1) {
                    GenerateExpression(node.Arguments[0]); // ceil is a no-op for integers
                    return;
                }
                if (functionName == "math.sqrt" && node.Arguments.Count >= 1) {
                    // CALL shared_isqrt — 整数平方根
                    GenerateExpression(node.Arguments[0]); // R0 = n
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "isqrt")]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)]));
                    return;
                }
                if (functionName == "math.min" && node.Arguments.Count >= 2) {
                    // CALL shared_min (__stdcall: PUSH b, PUSH a)
                    GenerateExpression(node.Arguments[1]);  // R0 = b
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]);  // R0 = a
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "min")]));
                    return;
                }
                if (functionName == "math.max" && node.Arguments.Count >= 2) {
                    // CALL shared_max (__stdcall: PUSH b, PUSH a)
                    GenerateExpression(node.Arguments[1]);  // R0 = b
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]);  // R0 = a
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "max")]));
                    return;
                }
                if (functionName.StartsWith("peek") && node.Arguments.Count >= 1)
                {
                    string runtimeFn = "vml_" + functionName;
                    GenerateExpression(node.Arguments[0]);  // addr
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                    return;
                }
                if (functionName.StartsWith("poke") && node.Arguments.Count >= 2)
                {
                    string runtimeFn = "vml_" + functionName;
                    GenerateExpression(node.Arguments[0]);  // addr
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
                    GenerateExpression(node.Arguments[1]);  // val
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                    return;
                }
                if (functionName == "chipasm" && node.Arguments.Count >= 2)
                {
                    // chipasm("arch", "code") — emit CHIPASM instruction
                    return;
                }
                // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Lua 通过 Lib/shared/vmlsys.c 调用系统功能
                if (functionName == "dofile" && node.Arguments.Count >= 1)
                {
                    // dofile("filename.lua") — compile-time file inclusion
                    // Returns the result of the last statement in the included file
                    if (node.Arguments[0] is ConstantNode dofileCn && dofileCn.Type == "string" && dofileCn.Value is string dofileName)
                    {
                        try
                        {
                            string basePath = Path.GetFullPath(SourceDirectory ?? ".");
                            string fullPath = Path.GetFullPath(Path.Combine(basePath, dofileName));
                            if (File.Exists(fullPath))
                            {
                                var included = LuaCompiler.CompileFile(fullPath, autoLinkStdLib: false);
                                MergeProgram(included);
                            }
                        }
                        catch { }
                    }
                    return;
                }
                if (functionName == "pcall" && node.Arguments.Count >= 1)
                {
                    // pcall(f, ...) — protected call
                    if (!VMLPlugins.CompilerOptionsContext.Current.IsMCU)
                    {
                        string catchLabel = $"pcall_catch_{labelCounter}";
                        string endLabel = $"pcall_end_{labelCounter++}";
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)])); // R0 = true (success)
                        instructions.Add(new Instruction(OpCode.CATCH, [new Operand(OperandType.LABEL, catchLabel)]));
                        // Call the function: evaluate f, push args, CALL
                        GenerateExpression(node.Arguments[0]); // R0 = function
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        for (int ai = 1; ai < node.Arguments.Count; ai++)
                        {
                            GenerateExpression(node.Arguments[ai]);
                            instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        }
                        // Pop function address from stack and call
                        instructions.Add(new Instruction(OpCode.POP, [Reg(1)])); // R1 = func addr
                        instructions.Add(new Instruction(OpCode.CALL, [Reg(1)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, (node.Arguments.Count - 1) * 4)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));
                        labels[catchLabel] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)])); // R0 = false (error)
                        instructions.Add(new Instruction(OpCode.ENDCATCH, []));
                        labels[endLabel] = instructions.Count;
                    }
                    else
                    {
                        // MCU: just call directly, no protection
                        GenerateExpression(node.Arguments[0]);
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        for (int ai = 1; ai < node.Arguments.Count; ai++)
                        {
                            GenerateExpression(node.Arguments[ai]);
                            instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        }
                        instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
                        instructions.Add(new Instruction(OpCode.CALL, [Reg(1)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, (node.Arguments.Count - 1) * 4)]));
                    }
                    return;
                }
                if (functionName == "loadfile" && node.Arguments.Count >= 1)
                {
                    // loadfile("filename.lua") — compile-time load, returns a placeholder
                    // For now, returns nil (0) — full implementation requires runtime compilation
                    instructions.Add(new Instruction(OpCode.MOVE,
                        new List<Operand> {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, 0)
                        }, instructions.Count));
                    return;
                }
                // table.sort(arr) — inline bubble sort (MCU-compatible, no GC/threads needed)
                if (functionName == "table.sort" && node.Arguments.Count >= 1)
                {
                    int tsLbl = _nextMathLabel++;
                    string tsOuter = $"tsort_o_{tsLbl}", tsInner = $"tsort_i_{tsLbl}";
                    string tsDone = $"tsort_d_{tsLbl}", tsNoSwap = $"tsort_ns_{tsLbl}";
                    // R0 = table ptr (saved to stack), R1 = count
                    GenerateExpression(node.Arguments[0]);  // R0 = table ptr
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0)])); // R4 = table ptr
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R4")])); // R1 = count
                    // R2 = outer i, R3 = limit = count-1
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1)])); // R3 = count-1
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0)])); // i = 0
                    AddLabel(tsOuter);
                    instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3)]));
                    instructions.Add(new(OpCode.JGE, [new Operand(OperandType.LABEL, tsDone)]));
                    // R5 = j = 0, R6 = inner limit = count-1-i
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 3)]));
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 2)])); // R6 = count-1-i
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0)])); // j = 0
                    AddLabel(tsInner);
                    instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 6)]));
                    instructions.Add(new(OpCode.JGE, [new Operand(OperandType.LABEL, $"{tsOuter}_nx")]));
                    // R7 = &table + 8 + j*8  (offset past count, then skip key to value)
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 5)]));
                    instructions.Add(new(OpCode.SHL, [new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 3)])); // j*8
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 8)])); // +8
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 4)])); // +table
                    // R8 = val[j], R9 = val[j+1]
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 8), new Operand(OperandType.MEMORY, "R7")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 9), new Operand(OperandType.MEMORY, "R7+8")]));
                    instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 9)]));
                    instructions.Add(new(OpCode.JLE, [new Operand(OperandType.LABEL, tsNoSwap)]));
                    // swap: *(R7) = R9, *(R7+8) = R8
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R7"), new Operand(OperandType.REGISTER, 9)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R7+8"), new Operand(OperandType.REGISTER, 8)]));
                    AddLabel(tsNoSwap);
                    instructions.Add(new(OpCode.INC, [new Operand(OperandType.REGISTER, 5)])); // j++
                    instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, tsInner)]));
                    AddLabel($"{tsOuter}_nx");
                    instructions.Add(new(OpCode.INC, [new Operand(OperandType.REGISTER, 2)])); // i++
                    instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, tsOuter)]));
                    AddLabel(tsDone);
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)])); // return table
                    return;
                }
                // setmetatable(t, mt) — inline: store mt at [t - 4] (metatable slot)
                if (functionName == "setmetatable" && node.Arguments.Count >= 2)
                {
                    GenerateExpression(node.Arguments[1]); // R0 = mt
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]); // R0 = t
                    instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // R1 = mt
                    instructions.Add(new(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R0-4")]));
                    // Return t (R0 already holds t)
                    return;
                }
                // getmetatable(t) — inline: load from [t - 4]
                if (functionName == "getmetatable" && node.Arguments.Count >= 1)
                {
                    GenerateExpression(node.Arguments[0]); // R0 = t
                    instructions.Add(new(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0-4")]));
                    return;
                }
                // string.reverse/upper/lower/rep → CALL shared C string library (栈缓冲区, MCU兼容)
                if ((functionName == "string.reverse" || functionName == "string.upper" || functionName == "string.lower") && node.Arguments.Count >= 1)
                {
                    string sharedFn = functionName == "string.reverse" ? "strrev" : functionName == "string.upper" ? "str_toupper" : "str_tolower";
                    GenerateExpression(node.Arguments[0]); // R0 = src
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // src on C stack
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 256)])); // alloc dst[256]
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)])); // R1 = dst
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)])); // dst on C stack
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, sharedFn)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)])); // return dst
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 264)])); // clean stack
                    return;
                }
                if (functionName == "string.sub" && node.Arguments.Count >= 2)
                {
                    // shared_str_substr(dst, src, pos, count): pos = 1-based → 0-based
                    GenerateExpression(node.Arguments[2]); // R0 = count (or nil → use big number)
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // count
                    GenerateExpression(node.Arguments[1]); // R0 = pos (1-based)
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)])); // 1-based→0-based
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // pos
                    GenerateExpression(node.Arguments[0]); // R0 = src
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // src
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 256)])); // alloc dst
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)])); // R1 = dst
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)])); // dst
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "str_substr")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 272)])); // clean
                    return;
                }
                if (functionName == "string.char" && node.Arguments.Count >= 1)
                {
                    // Generate single char: just the ASCII code → store in 1-byte buffer
                    GenerateExpression(node.Arguments[0]); // R0 = char code
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 4)])); // 4-byte buffer
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)]));
                    instructions.Add(new(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1+1"), new Operand(OperandType.REGISTER, 0)])); // null terminator
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    return;
                }
                if (functionName == "table.insert" && node.Arguments.Count >= 2)
                {
                    // CALL arr_push(table, value)
                    GenerateExpression(node.Arguments[1]); // R0 = value
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]); // R0 = table
                    instructions.Add(new(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // R1 = value
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "arr_push")]));
                    return;
                }
                if (functionName == "table.remove" && node.Arguments.Count >= 1)
                {
                    // CALL arr_pop(table) → R0 = removed value
                    GenerateExpression(node.Arguments[0]); // R0 = table
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "arr_pop")]));
                    return;
                }
                if (functionName == "math.gcd" && node.Arguments.Count >= 2)
                {
                    GenerateExpression(node.Arguments[1]);
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]);
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "gcd")]));
                    return;
                }
                if (functionName == "math.lcm" && node.Arguments.Count >= 2)
                {
                    GenerateExpression(node.Arguments[1]);
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]);
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "lcm")]));
                    return;
                }
                if (functionName == "string.format" && node.Arguments.Count >= 2)
                {
                    // 基础支持: string.format("%d", n) → CALL shared_itoa
                    // string.format("%s", s) → 直接返回字符串
                    GenerateExpression(node.Arguments[1]); // R0 = arg
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 32)])); // alloc buf
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)])); // R1 = buf
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)])); // buf
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "itoa")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 40)])); // clean
                    return;
                }
                if (functionName == "io.write" && node.Arguments.Count >= 1)
                {
                    // MCU io.write: output string via SYSCALL 1
                    GenerateExpression(node.Arguments[0]); // R0 = string
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 1)])); // OutputString
                    return;
                }
                if (functionName == "io.read" && node.Arguments.Count >= 0)
                {
                    // MCU io.read: read line via SYSCALL 2 → R0 = buffer address
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 256)])); // alloc buffer
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 13)]));
                    instructions.Add(new(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 2)])); // InputString
                    return;
                }
                if (functionName == "os.time" && node.Arguments.Count >= 0)
                {
                    EmitGetTick();
                    return;
                }
                if (functionName == "os.exit" && node.Arguments.Count >= 1)
                {
                    GenerateExpression(node.Arguments[0]); // R0 = exit code
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "exit")]));
                    return;
                }
                if (functionName == "string.find" && node.Arguments.Count >= 2)
                {
                    // CALL shared_strstr(haystack, needle) → R0 = pointer to first match or 0
                    GenerateExpression(node.Arguments[1]); // R0 = needle
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(node.Arguments[0]); // R0 = haystack
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "strstr")]));
                    // Convert pointer to 1-based index or nil=0
                    string sfFound = $"sf_{Guid.NewGuid():N}"[..6];
                    string sfEnd = $"sf_{Guid.NewGuid():N}"[..6];
                    instructions.Add(new(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new(OpCode.JE, [new Operand(OperandType.LABEL, sfFound)]));
                    AddLabel(sfFound);
                    return;
                }
                if (functionName == "string.rep" && node.Arguments.Count >= 2)
                {
                    // shared_str_repeat(dst, src, n) → R0 = len
                    GenerateExpression(node.Arguments[1]); // R0 = n
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // n on stack
                    GenerateExpression(node.Arguments[0]); // R0 = src
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // src
                    instructions.Add(new(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 256)])); // alloc dst
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 13)])); // R1 = dst
                    instructions.Add(new(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)])); // dst
                    instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "str_repeat")]));
                    instructions.Add(new(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 268)])); // clean stack (12 args + 256 buf)
                    return;
                }
                string funcLabel;
                if (LuaBuiltinFunctions.TryGetValue(functionName, out string mappedLabel))
                {
                    funcLabel = mappedLabel;
                }
                else
                {
                    funcLabel = $"func_{SanitizeFunctionName(functionName)}";
                }
                instructions.Add(new Instruction(OpCode.CALL,
                    new List<Operand> {
                        new Operand(OperandType.LABEL, funcLabel)
                    },
                    instructions.Count));

                // 调用方清栈（原先没有 —— 那时实参既没真压、也没人清）
                int callArgs = node.Arguments?.Count ?? 0;
                if (callArgs > 0)
                {
                    instructions.Add(new Instruction(OpCode.ADD,
                        new List<Operand> {
                            new Operand(OperandType.REGISTER, 13),
                            new Operand(OperandType.IMMEDIATE, callArgs * 4)
                        },
                        instructions.Count));
                }
            }
            else
            {
                // 其他函数表达式暂不支持
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, "不支持的函数调用表达式");
            }
        }
        
        private void GenerateTableConstructor(TableConstructorNode node)
        {
            // 格式: [capacity(4), metatable(4), count(4), key1(4), val1(4), key2(4), val2(4), ...]
            // 返回的指针 p = raw + 8（指向 count）⇒ 相对布局与旧版完全一致：
            //   [p-8]=capacity（新加，只有 lua_table_set 读）、[p-4]=metatable（前端 `[R1-4]` 读的仍是它）、
            //   [p]=count、[p+4+i*8]=key_i、[p+8+i*8]=value_i。
            // **为什么要有 capacity**：Lua 的表是**动态**的（`t[#t+1] = v`、`a[新键] = v`），
            // 而 lua_table_set 拿不到「重新分配并把新指针写回调用方变量」的机会（调用点忽略返回值）
            // ⇒ 只能在建表时多留余量、由 set 在尾部追加。没有余量时写新键只能静默丢弃。
            // ⚠ 余量是**有限**的：写满 count == capacity 之后新键只能丢弃（见 lua_table_set）。
            // 真正的动态增长要「重新分配 + 把新指针写回调用方的变量」，而调用点忽略返回值，
            // 前端也没为 `a[i] = v` 的 `a` 保留左值信息 —— 那是另一个改动。
            int fieldCount = node.Fields.Count;
            int capacity = fieldCount + Math.Max(8, fieldCount / 2 + 4);
            int totalSlots = capacity * 2 + 3; // capacity + metatable + count + capacity*(key+val)
            int bytes = totalSlots * 4;

            // 分配内存
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, bytes.ToString())
                },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.SYSCALL,
                new List<Operand> { new Operand(OperandType.IMMEDIATE, 40) },
                instructions.Count));
            // R2 = raw ptr (capacity at [R2+0], metatable at [R2+4], count at [R2+8], ...)
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                },
                instructions.Count));

            // 存储 capacity at [R2 + 0]
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, capacity.ToString())
                },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, 0)]));

            // 存储 metatable = nil (0) at [R2 + 4]
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.MEMORY, "R2+4"), new Operand(OperandType.REGISTER, 0)]));

            // 存储元素数量 at [R2 + 8]
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, node.Fields.Count.ToString())
                },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE,
                [new Operand(OperandType.MEMORY, "R2+8"), new Operand(OperandType.REGISTER, 0)]));

            // 存储 key-value 对
            // ⚠ 基址**必须存到栈上**：下面每个字段的求值都可能冲掉 R2
            //（嵌套表构造器自己也拿 R2 当基址；函数调用、表访问也都会用 R1–R3）。
            // 只在 R2 里攥着基址的话，`{{1,2},{3,4}}` 这种嵌套字面量会把**内层表当外层写**，
            // 整张表全是坏的（实测 `local grid = {{1,2},{3,4}}` 之后 `grid[1][1]` 直接崩）。
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 2) },
                instructions.Count));
            int slotIndex = 0;
            foreach (var field in node.Fields)
            {
                int baseOffset = 12 + slotIndex * 8; // capacity(4)+metatable(4)+count(4) + slot*(key+val)

                // 存储 key
                if (field.Key != null) GenerateExpression(field.Key);
                else AddRI(OpCode.MOVE, 0, slotIndex + 1);
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]));
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.MEMORY, $"R1+{baseOffset}"), new Operand(OperandType.REGISTER, 0)]));

                // 存储 value
                GenerateExpression(field.Value);
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R13")]));
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.MEMORY, $"R1+{baseOffset + 4}"), new Operand(OperandType.REGISTER, 0)]));

                slotIndex++;
            }
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 2) },
                instructions.Count));

            // Return table pointer = raw ptr + 8 (skip capacity + metatable, points to count)
            // 相对布局与旧版一致：调用方看到的 [p-4]=metatable、[p]=count 都没变
            instructions.Add(new Instruction(OpCode.ADD,
                [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8)]));
        }

        private void GenerateTableAccess(TableAccessNode node)
        {
            // 表指针**必须存栈上**，不能只放在 R1 里求值 key —— 求值表达式会拿 R1/R2 当临时寄存器
            // （`GenerateBinaryOperation` 就是），`t[i + 1]` 这种写法下 R1 会变成 `i` 的值，
            // 传给 lua_table_get 的就是个野指针（实测 R1=1 ⇒ 读 [1-4] = FFFFFFFC 越界崩溃）。
            GenerateExpression(node.Table);
            instructions.Add(new Instruction(OpCode.PUSH,
                new List<Operand> { new Operand(OperandType.REGISTER, 0) },
                instructions.Count));
            GenerateExpression(node.Key);
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                },
                instructions.Count));
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 1) },
                instructions.Count)); // R1 = 表指针
            instructions.Add(new Instruction(OpCode.CALL,
                new List<Operand> { new Operand(OperandType.LABEL, "lua_table_get") },
                instructions.Count));
            // __index metatable fallback: if result is nil, check metatable
            string mtEnd = NewLabel();
            instructions.Add(new(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, mtEnd)])); // found, skip
            // Load metatable from [R1-4] (table pointer - 4)
            instructions.Add(new(OpCode.MOVE, [Reg(3), new Operand(OperandType.MEMORY, "R1-4")])); // R3 = metatable
            instructions.Add(new(OpCode.JZ, [Reg(3), new Operand(OperandType.LABEL, mtEnd)])); // no metatable, return nil
            // Look up __index in metatable: lua_table_get(metatable, "__index")
            instructions.Add(new(OpCode.PUSH, [Reg(1)])); // save table
            instructions.Add(new(OpCode.PUSH, [Reg(2)])); // save key
            string idxStrLabel = AddStringCached("__index");
            instructions.Add(new(OpCode.MOVE, [Reg(1), Reg(3)])); // R1 = metatable
            instructions.Add(new(OpCode.MOVE, [Reg(2), new Operand(OperandType.LABEL, idxStrLabel)])); // R2 = "__index"
            instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "lua_table_get")]));
            string mtNoIndex = NewLabel();
            instructions.Add(new(OpCode.JZ, [Reg(0), new Operand(OperandType.LABEL, mtNoIndex)])); // no __index
            // __index found, use it as fallback table: lua_table_get(__index, key)
            instructions.Add(new(OpCode.MOVE, [Reg(1), Reg(0)])); // R1 = __index table
            instructions.Add(new(OpCode.POP, [Reg(2)])); // R2 = key
            instructions.Add(new(OpCode.CALL, [new Operand(OperandType.LABEL, "lua_table_get")]));
            instructions.Add(new(OpCode.ADD, [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 4)])); // pop saved table
            instructions.Add(new(OpCode.JMP, [new Operand(OperandType.LABEL, mtEnd)]));
            // Cleanup for no-__index case: pop saved regs, return nil
            AddLabel(mtNoIndex);
            instructions.Add(new(OpCode.ADD, [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 8)])); // pop key + table
            AddRI(OpCode.MOVE, 0, 0); // return nil
            AddLabel(mtEnd);
        }

        /// <summary>
        /// 将另一个 VmlProgram 的指令、标签和数据段合并到当前程序中
        /// 用于 dofile 编译时文件包含
        /// </summary>
        private void MergeProgram(VmlProgram other)
        {
            if (other == null || other.Instructions == null || other.Instructions.Count == 0)
                return;

            int baseIndex = instructions.Count;
            string labelPrefix = $"dofile_{Guid.NewGuid().ToString("N").Substring(0, 8)}_";

            // 1. 合并数据段（避免键名冲突，跳过已存在的键）
            if (other.DataSection != null)
            {
                foreach (var kvp in other.DataSection)
                {
                    if (!dataSection.ContainsKey(kvp.Key))
                        dataSection[kvp.Key] = kvp.Value;
                }
            }

            // 2. 合并常量段
            if (other.Constants != null)
            {
                foreach (var kvp in other.Constants)
                {
                    if (!constants.ContainsKey(kvp.Key))
                        constants[kvp.Key] = kvp.Value;
                }
            }

            // 3. 合并标签（加上偏移量，并重命名以避免冲突）
            var labelMap = new Dictionary<string, string>();
            if (other.Labels != null)
            {
                foreach (var kvp in other.Labels)
                {
                    // 跳过 main 和入口标签
                    if (kvp.Key == "main" || kvp.Key == other.EntryPoint)
                        continue;
                    string newName = labelPrefix + kvp.Key;
                    labels[newName] = baseIndex + kvp.Value;
                    labelMap[kvp.Key] = newName;
                }
            }

            // 4. 合并指令（重写其中的标签引用）
            foreach (var instr in other.Instructions)
            {
                // 跳过旧入口标签（main）
                if (instr.Opcode == OpCode.LABEL && instr.Operands.Count > 0)
                {
                    object? labelVal = instr.Operands[0].Value;
                    if (labelVal is string lblStr && (lblStr == "main" || lblStr == other.EntryPoint))
                        continue; // 跳过旧入口标签，不重复添加
                }

                var newInstr = new Instruction(instr.Opcode,
                    new List<Operand>(instr.Operands.Select(op =>
                    {
                        // 重写 LABEL 类型的操作数（跳转目标）
                        if (op.Type == OperandType.LABEL && op.Value is string lblName)
                        {
                            // 检查是否是用户定义的标签（通过 labelMap）
                            if (labelMap.TryGetValue(lblName, out string mappedName))
                                return new Operand(OperandType.LABEL, mappedName);
                        }
                        return new Operand(op.Type, op.Value);
                    })),
                    instructions.Count);

                instructions.Add(newInstr);
            }
        }
    }
}

