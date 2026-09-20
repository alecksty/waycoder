using System;
using System.Collections.Generic;
using System.Text;
using CompilerBase;
using VMLAssembler;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        public override VmlProgram GenerateCode()
        {
            var program = _program!;

            // 进入全局作用域
            EnterScope();

            // 生成代码
            program.Accept(this);

            // 如果没有main函数，添加默认入口点
            if (!labels.ContainsKey("main"))
            {
                AddLabel("main");
                EmitExit();
            }

            // 检查dataSection中是否有null键
            foreach (var key in dataSection.Keys)
            {
                if (key == null)
                {
                    throw new CodeGenerationException("数据段里含 null 键");
                }
            }

            return BuildProgram("main");
        }
        
        public void Visit(ProgramNode node)
        {
            foreach (var statement in node.Statements)
            {
                SetCurrentSource(statement);
                statement.Accept(this);
            }
        }
        
        public void Visit(FunctionNode node)
        {
            // 为函数创建标签
            string funcLabel = node.Name == "main" ? "main" : $"func_{node.Name}";

            AddLabel(funcLabel);

            // 添加函数注释
            Emit(OpCode.NOP, new List<Operand>(), "; -------------------------------------------");
            // 生成源函数声明
            var sourceDecl = $"fn {node.Name}(";
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                sourceDecl += $"{node.Parameters[i].Name}: {node.Parameters[i].Type}";
                if (i < node.Parameters.Count - 1)
                    sourceDecl += ", ";
            }
            sourceDecl += ")";
            if (!string.IsNullOrEmpty(node.ReturnType) && node.ReturnType != "void" && node.ReturnType != "()")
                sourceDecl += $" -> {node.ReturnType}";
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; source   : {sourceDecl}"));
            // 生成函数名注释
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; function : {node.Name}"));
            // 生成参数注释
            foreach (var param in node.Parameters)
            {
                instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; param   : {param.Type} {param.Name}"));
            }
            // 生成返回类型注释
            var retTypeStr = string.IsNullOrEmpty(node.ReturnType) || node.ReturnType == "void" || node.ReturnType == "()" ? "()" : node.ReturnType;
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, $"; return   : {retTypeStr}"));
            instructions.Add(new Instruction(OpCode.NOP, new List<Operand>(), instructions.Count, "; --------------------------------------------"));
            _firstLocalVarName = "";
            _hasMatch = false;
            
            // 保存当前作用域
            EnterScope();
            
            // 设置栈帧
            EmitPrologue();
            
            // 为参数分配空间
            int paramOffset = 12; // R12+12开始是参数（跳过PUSH R15+PUSH R12+CALL返回地址共12字节）
            foreach (var param in node.Parameters)
            {
                _variables[param.Name] = paramOffset;
                RecordVarInScope(param.Name);
                paramOffset += 4; // 每个参数4字节
            }
            
            int startVarOffset = _variableOffset;
            int insertPos = instructions.Count; // 在 prologue 之后、body 之前插入 SUB

            node.Body.Accept(this);

            int varSpace = startVarOffset - _variableOffset;
            if (varSpace > 0)
            {
                instructions.Insert(insertPos, new Instruction(OpCode.SUB,
                    new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 13),
                        new Operand(OperandType.REGISTER, 13),
                        new Operand(OperandType.IMMEDIATE, varSpace)
                    }, insertPos));
                var keys = labels.Keys.ToList();
                foreach (var k in keys)
                    if (labels[k] >= insertPos)
                        labels[k]++;
            }

            // main 函数自动退出
            if (node.Name == "main" && !string.IsNullOrEmpty(_firstLocalVarName))
            {
                bool hasReturnType = !string.IsNullOrEmpty(node.ReturnType)
                    && node.ReturnType != "void" && node.ReturnType != "()";

                if (hasReturnType)
                {
                    // 表达式返回值已在 R0 中（as cast 已处理类型转换），直接退出
                }
                else
                {
                    // 无返回类型 — 加载第一个局部变量的值
                    int exitVarOffset = _hasMatch ? _variableOffset : (startVarOffset - 4);
                    bool isFloat = _rustVarTypes.TryGetValue(_firstLocalVarName, out RustType ft)
                        && ft == RustType.Float;
                    if (isFloat)
                    {
                        AddInstruction(OpCode.MOVEF, "R0", $"{exitVarOffset}(R12)");
                        AddInstruction(OpCode.F2I, "R0", "R0");
                    }
                    else
                    {
                        AddInstruction(OpCode.MOVE, "R0", $"{exitVarOffset}(R12)");
                    }
                }
                EmitExit();
            }
            
            // 如果没有return语句，添加默认返回
            EmitEpilogue();
            
            // 退出作用域
            ExitScope();
        }
        
        /// <summary>
        /// 从表达式推断类型
        /// </summary>
        private string InferTypeFromExpression(ASTNode expression)
        {
            if (expression is LiteralNode literal)
            {
                return literal.Type;
            }
            else if (expression is IdentifierNode identifier)
            {
                // 如果是枚举变体标识符
                if (_enumVariants.ContainsKey(identifier.Name))
                {
                    return "enum";
                }
                // 如果是变量引用，返回变量类型
                if (_variableTypes.TryGetValue(identifier.Name, out string type))
                {
                    return type;
                }
                return "int"; // 默认
            }
            else if (expression is StructLiteralNode)
            {
                return "struct";
            }
            else if (expression is TupleExprNode)
            {
                return "tuple";
            }
            else if (expression is BinaryOperationNode binary)
            {
                // as 类型转换 — 返回目标类型
                if (binary.Operator == "as" && binary.Right is LiteralNode typeLit && typeLit.Type == "type")
                {
                    return typeLit.Value?.ToString()?.ToLower() ?? "int";
                }

                // 对于二元运算，返回操作数类型
                string leftType = InferTypeFromExpression(binary.Left);
                string rightType = InferTypeFromExpression(binary.Right);

                // 如果有一个是 f64，结果就是 f64
                if (leftType == "f64" || rightType == "f64")
                {
                    return "f64";
                }
                // 如果有一个是浮点数，结果就是浮点数 (字面量用 "float"，变量类型用 "f32")
                if (leftType == "float" || leftType == "f32" || rightType == "float" || rightType == "f32")
                {
                    return "float";
                }
                // 如果有一个是 i64，结果就是 i64
                if (leftType == "i64" || rightType == "i64")
                {
                    return "i64";
                }
                return "int"; // 默认
            }
            else if (expression is UnaryOperationNode unary)
            {
                // 一元运算，返回操作数类型
                return InferTypeFromExpression(unary.Operand);
            }
            else if (expression is CallExpressionNode callExpr)
            {
                // 通过命名约定识别 _to_str 函数返回字符串 (v1.66.53)
                if (IsStringReturningFunc(callExpr.FunctionName))
                    return "string";
                return "int";
            }

            return "int"; // 默认类型
        }
        
        /// <summary>
        /// 将字符串类型转换为RustType
        /// </summary>
        private RustType GetRustTypeFromString(string typeStr)
        {
            return typeStr switch
            {
                "int" or "i32" => RustType.Int,
                "i64" => RustType.I64,
                "float" or "f32" => RustType.Float,
                "f64" => RustType.F64,
                "bool" => RustType.Bool,
                "char" => RustType.Char,
                "string" => RustType.String,
                "struct" => RustType.Struct,
                "enum" => RustType.Enum,
                "tuple" => RustType.Tuple,
                "array" => RustType.Array,
                "reference" => RustType.Reference,
                _ => RustType.Int // 默认整数类型
            };
        }
        
        /// <summary>
        /// 根据Rust数据类型获取加载指令
        /// </summary>
        // RustType -> (byteSize, isFloat, isDouble, isLong)
        private static (int, bool, bool, bool) TypeInfo(RustType t) => t switch
        {
            RustType.Float => (4, true, false, false),
            RustType.F64 => (8, false, true, false),
            RustType.I64 => (8, false, false, true),  // 64 位整数用 long 路径 (MOVEL/ADDL)
            RustType.Bool or RustType.Char => (1, false, false, false),
            _ => (4, false, false, false),
        };

        /// <summary>
        /// Convert Rust type string (e.g. "i8", "u16", "f32", "f64", "*i8") to RustType for instruction selection.
        /// </summary>
        private static RustType RustTypeFromString(string typeName)
        {
            string t = typeName.Trim().ToLower();
            if (t.Contains("*")) t = t.Replace("*", "").Trim();
            return t switch
            {
                "i8" or "u8" or "bool" or "char" => RustType.Bool, // 1-byte
                "i16" or "u16" => RustType.Int, // 2-byte (maps to LOADH via TypeInfo size)
                "f32" => RustType.Float,
                "f64" => RustType.F64,
                "i64" => RustType.I64,
                _ => RustType.Int,
            };
        }

        /// <summary>
        /// Get (byteSize, isFloat, isDouble, isLong) from a Rust type string.
        /// </summary>
        private static (int, bool, bool, bool) TypeInfoFromString(string typeName)
        {
            string t = typeName.Trim().ToLower().Replace("*", "").Trim();
            return t switch
            {
                "i8" or "u8" or "bool" or "char" => (1, false, false, false),
                "i16" or "u16" => (2, false, false, false),
                "f32" => (4, true, false, false),
                "f64" => (8, false, true, false),
                "i64" => (8, false, false, true),  // 64 位整数用 long 路径 (MOVEL/ADDL)
                _ => (4, false, false, false),
            };
        }

        private new OpCode GetLoadInstruction(RustType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectLoadOp(s, f, d, l);
        }

        private new OpCode GetStoreInstruction(RustType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectStoreUnifiedOp(s, f, d, l);
        }

        private new OpCode GetMoveInstruction(RustType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectMoveOp(s, f, d, l);
        }

        private new OpCode GetPushInstruction(RustType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectPushOp(s, f, d, l);
        }

        private new OpCode GetPopInstruction(RustType type)
        {
            var (s, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectPopOp(s, f, d, l);
        }

        private new OpCode GetArithmeticInstruction(string op, RustType type)
        {
            var (_, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectArithmeticOp(op, f, d, l);
        }

        private new OpCode GetCompareInstruction(RustType type)
        {
            var (_, f, d, l) = TypeInfo(type);
            return ExpressionManager.SelectCompareOp(f, d, l);
        }

        private void GenerateTypeConversion(RustType fromType, RustType toType)
            => EmitTypeConversion(fromType, toType);
        
        /// <summary>
        /// 生成带目标类型的表达式代码
        /// </summary>
        private void GenerateExpressionWithType(ASTNode expr, RustType targetType)
        {
            // 生成表达式值到R0
            expr.Accept(this);
            
            // 推断表达式类型
            string exprTypeStr = InferTypeFromExpression(expr);
            RustType exprType = GetRustTypeFromString(exprTypeStr);
            
            // 进行类型转换
            GenerateTypeConversion(exprType, targetType);
        }
        
    }
}
