using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        private void ProcessTypeDeclarations(List<DeclarationNode> declarations)
        {
            foreach (var decl in declarations)
            {
                if (decl is TypeDeclarationNode typeDecl)
                {
                    definedTypeAliases[typeDecl.Name.ToUpper()] = typeDecl.Type;
                    if (typeDecl.Type is RecordTypeNode recordType)
                    {
                        definedRecordTypes[typeDecl.Name.ToUpper()] = recordType;
                        ComputeRecordLayout(typeDecl.Name.ToUpper(), recordType);
                    }
                }
            }
        }

        private void ComputeRecordLayout(string recordTypeName, RecordTypeNode recordType)
        {
            if (recordsBeingComputed.Contains(recordTypeName))
                throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"递归record类型不允许: {recordTypeName} (请使用 ^指针 间接引用)");
            recordsBeingComputed.Add(recordTypeName);

            var fieldLayout = new Dictionary<string, (int offset, string type)>();
            int offset = 0;

            foreach (var field in recordType.Fields)
            {
                int fieldSlots = GetVariableSlots(field.Type);
                if (fieldSlots < 1) fieldSlots = 1;
                fieldLayout[field.Name.ToUpper()] = (offset, GetTypeName(field.Type));
                offset += fieldSlots;
            }

            recordFieldLayouts[recordTypeName] = fieldLayout;
            recordsBeingComputed.Remove(recordTypeName);
        }

        private string FindRecordTypeName(TypeNode typeNode)
        {
            if (typeNode is SimpleTypeNode simple)
            {
                string name = simple.TypeName.ToUpper();
                if (definedRecordTypes.ContainsKey(name))
                    return name;
            }
            return null;
        }

        private int GetVariableSlots(TypeNode typeNode)
        {
            typeNode = ResolveTypeAlias(typeNode);
            if (typeNode is RecordTypeNode recordType)
            {
                int total = 0;
                foreach (var field in recordType.Fields)
                {
                    int slots = GetVariableSlots(field.Type);
                    total += Math.Max(1, slots);
                }
                return total;
            }
            if (typeNode is ArrayTypeNode arrayType)
            {
                if (arrayType.IsDynamic)
                    return 1;
                int lower = EvaluateConstantExpr(arrayType.LowerBound);
                int upper = EvaluateConstantExpr(arrayType.UpperBound);
                int count = upper - lower + 1;
                return count * Math.Max(1, GetVariableSlots(arrayType.ElementType));
            }
            if (typeNode is PointerTypeNode || typeNode is ProcedureTypeNode)
                return 1;
            if (typeNode is SubrangeTypeNode)
                return 1;
            if (typeNode is SetTypeNode setType)
            {
                // 集合类型用位图表示
                //
                // ⚠ **`set of char` 走的是"基础类型"那一支，没有上下界** ——
                //   此前 `EvaluateConstantExpr(null)` 两次都返回 0，于是
                //   `bitsNeeded = 1`、整个集合只占 **1 个字**。
                //   而 `in` 的取值是 `[基址 + (元素值/32)*4]`：元素 'b'(98) 落在
                //   **第 3 个字**上，1 个字的集合根本装不下 —— 读出去就是别人的内存。
                //   表现是「`'b' in s` 时对时错、取决于相邻数据段里恰好是什么」，
                //   也正是**集合赋值**那条块复制只搬 1 个字的成因。
                //   Turbo Pascal 的集合上限是 256 个元素，所以基础类型一律按 256 位算。
                if (setType.BaseType != null)
                {
                    string baseName = setType.BaseType is SimpleTypeNode simpleSet ? simpleSet.TypeName.ToUpper() : "";
                    int bitsForBase = baseName == "BOOLEAN" ? 2 : 256;   // char/byte/word/… 都是满 256
                    return (bitsForBase + 31) / 32;
                }

                // 计算需要的位数：upperBound - lowerBound + 1
                int lower = EvaluateConstantExpr(setType.LowerBound);
                int upper = EvaluateConstantExpr(setType.UpperBound);
                int bitsNeeded = upper - lower + 1;
                // 计算需要的4字节单元数（向上取整到最近的4字节）
                return (bitsNeeded + 31) / 32;
            }
            if (typeNode is FileTypeNode fileType)
            {
                // 文件类型需要多个slot来存储文件句柄和状态信息
                // 文件句柄(4字节) + 文件模式(4字节) + 缓冲区指针(4字节) + 错误状态(4字节)
                return 4; // 4个slot用于文件操作
            }
            return 1; // 基本类型占1个slot
        }

        /// <summary>
        /// 编译期常量求值（数组边界、集合位图都用它）。
        ///
        /// <para>
        /// 此前**只认字面量**，`UnaryOpNode`/`BinaryOpNode` 一律返回 0 ——
        /// 于是 `array[1..N]`（N 是 `const` 表达式）会被算成 1 个元素、
        /// `[1..6]` 会被算成 `[1..0]`。这两种"算得出但不报错"的静默错最麻烦，
        /// 所以这里把一元/二元算术补上（C 前端那条 `ConstFold` 是同一个用途，
        /// 但那是另一个程序的另一个类，这里就地补齐即可）。
        /// </para>
        /// <para>
        /// 认不出来的一律返回 0（**保持既有语义**：调用方多处依赖"给不出就 0"）。
        /// </para>
        /// </summary>
        private int EvaluateConstantExpr(ExpressionNode expr)
        {
            switch (expr)
            {
                case LiteralNode lit:
                    return Convert.ToInt32(lit.Value);
                case UnaryOpNode unary:
                {
                    int v = EvaluateConstantExpr(unary.Operand);
                    return unary.Operator switch
                    {
                        TokenType.MINUS => -v,
                        TokenType.PLUS => v,
                        TokenType.NOT => ~v,
                        _ => 0,
                    };
                }
                case BinaryOpNode binary:
                {
                    int a = EvaluateConstantExpr(binary.Left);
                    int b = EvaluateConstantExpr(binary.Right);
                    return binary.Operator switch
                    {
                        TokenType.PLUS => unchecked(a + b),
                        TokenType.MINUS => unchecked(a - b),
                        TokenType.STAR => unchecked(a * b),
                        TokenType.DIV => b != 0 ? a / b : 0,
                        TokenType.MOD => b != 0 ? a % b : 0,
                        _ => 0,
                    };
                }
                default:
                    return 0;
            }
        }

        private TypeNode ResolveTypeAlias(TypeNode typeNode)
        {
            if (typeNode is SimpleTypeNode simple)
            {
                string name = simple.TypeName.ToUpper();
                if (definedTypeAliases.ContainsKey(name))
                    return definedTypeAliases[name];
            }
            return typeNode;
        }
    }
}
