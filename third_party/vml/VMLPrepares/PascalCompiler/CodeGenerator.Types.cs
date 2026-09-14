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

        private int EvaluateConstantExpr(ExpressionNode expr)
        {
            if (expr is LiteralNode lit)
                return Convert.ToInt32(lit.Value);
            return 0;
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
