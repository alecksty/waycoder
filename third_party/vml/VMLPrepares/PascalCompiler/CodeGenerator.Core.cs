using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        private void CollectArrayBounds(ArrayTypeNode arr, List<(int, int)> bounds)
        {
            bounds.Add((EvaluateConstantExpr(arr.LowerBound), EvaluateConstantExpr(arr.UpperBound)));
            if (arr.ElementType is ArrayTypeNode nested)
                CollectArrayBounds(nested, bounds);
        }

        private ASTNode astNode;
        private Dictionary<string, List<(int lower, int upper)>> arrayBounds;
        
        // 栈帧管理
        private Dictionary<string, int> globalVarOffsets = new(); // 全局变量偏移
        private Dictionary<string, int> localVarOffsets = new(); // 局部变量偏移(BP相对)
        private Dictionary<string, string> globalVarTypes = new(); // 全局变量类型
        private Dictionary<string, string> localVarTypes = new(); // 局部变量类型
        private Dictionary<string, TypeNode> globalVarDeclarations = new(); // 全局变量声明
        private Dictionary<string, TypeNode> localVarDeclarations = new(); // 局部变量声明
        private Dictionary<string, TypeNode> definedTypeAliases = new(); // 类型别名
        private HashSet<string> dynamicArrayNames = new(); // 动态数组变量
        private Dictionary<string, RecordTypeNode> definedRecordTypes = new(); // 已定义的record类型
        private Dictionary<string, Dictionary<string, (int offset, string type)>> recordFieldLayouts = new(); // record字段布局
        private Dictionary<string, string> variableRecordTypes = new(); // 变量对应的record类型名
        private HashSet<string> constNames = new(); // 常量名称(在dataSection中)
        private HashSet<string> recordsBeingComputed = new(); // 正在计算布局的record,防递归
        private int currentLocalSize = 0; // 当前函数局部变量大小(4字节单元)
        private int currentParamSize = 0; // 当前函数参数大小
        private bool isInSubprogram = false; // 是否在过程/函数中
        private HashSet<string> varParameters = new(); // var参数(引用传递)
        private Dictionary<string, int> paramOffsets = new(); // 参数偏移(BP正方向)
        private Stack<string> subprogramExitLabels = new(); // 子程序 exit 标签栈
        private HashSet<string> functionNames = new(); // 函数名集合,用于识别无括号函数调用
        public static Dictionary<string, string> ExternalFuncTypes = new(); // 外部函数返回类型 (v1.66.46)

        public CodeGenerator(ProgramNode ast) : base()
        {
            this.astNode = ast;
            arrayBounds = new Dictionary<string, List<(int lower, int upper)>>();
            InitSimpleCompiler();
        }

        public CodeGenerator(UnitNode ast) : base()
        {
            this.astNode = ast;
            arrayBounds = new Dictionary<string, List<(int lower, int upper)>>();
            InitSimpleCompiler();
        }

        public override VmlProgram GenerateCode()
        {
            if (astNode is ProgramNode programNode)
                return GenerateProgramCode(programNode);
            else if (astNode is UnitNode unitNode)
                return GenerateUnitCode(unitNode);
            throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, "不支持的AST节点类型");
        }

        private VmlProgram GenerateProgramCode(ProgramNode ast)
        {
            // 处理类型声明(记录record类型)
            ProcessTypeDeclarations(ast.Declarations);

            // 处理常量声明
            foreach (var decl in ast.Declarations)
            {
                if (decl is ConstDeclarationNode constDecl)
                {
                    // 处理数组常量初始化: const arr: array[1..5] of integer = (1, 2, 3, 4, 5);
                    if (constDecl.ArrayValues != null && constDecl.ArrayValues.Count > 0)
                    {
                        int[] values = new int[constDecl.ArrayValues.Count];
                        for (int i = 0; i < constDecl.ArrayValues.Count; i++)
                        {
                            if (constDecl.ArrayValues[i] is LiteralNode lit)
                                values[i] = Convert.ToInt32(lit.Value);
                            else
                                values[i] = 0;
                        }
                        dataSection[constDecl.Name] = values;
                        constNames.Add(constDecl.Name);
                    }
                    else if (constDecl.Value is LiteralNode literal)
                    {
                        if (literal.Type == TokenType.REAL_LITERAL)
                        {
                            constants[constDecl.Name] = Convert.ToDouble(literal.Value);
                        }
                        else if (literal.Type == TokenType.INTEGER_LITERAL)
                        {
                            // 整数常量直接放入数据段
                            dataSection[constDecl.Name] = Convert.ToInt32(literal.Value);
                            constNames.Add(constDecl.Name);
                        }
                        else if (literal.Type == TokenType.BOOLEAN)
                        {
                            // 布尔常量直接放入数据段
                            dataSection[constDecl.Name] = (bool)literal.Value ? 1 : 0;
                            constNames.Add(constDecl.Name);
                        }
                        else if (literal.Type == TokenType.STRING_LITERAL)
                        {
                            dataSection[constDecl.Name] = literal.Value.ToString();
                            constNames.Add(constDecl.Name);
                        }
                        else
                        {
                            dataSection[constDecl.Name] = Convert.ToInt32(literal.Value);
                            constNames.Add(constDecl.Name);
                        }
                    }
                }
            }

            // 初始化全局变量（必须在生成子程序之前，以便子程序能引用全局变量类型）
            foreach (var decl in ast.Declarations)
            {
                if (decl is VarDeclarationNode varDecl)
                {
                    if (!dataSection.ContainsKey(varDecl.Name))
                    {
                        AllocateVariable(varDecl);
                    }
                }
            }

            // 生成所有子程序(过程/函数)
            foreach (var subprogram in ast.Subprograms)
            {
                GenerateSubprogram(subprogram);
            }

            // 添加主程序入口标签
            AddLabel("main");

            // 生成主程序代码（主程序变量已在上面初始化）
            // 生成主程序代码
            GenerateBlock(ast.Block);

            // 加载最后一个纯字母变量值到 R0 作为退出码（避免计数器变量如 i/j/k 被选为退出码）
            string lastVar = null;
            foreach (var kv in dataSection.OrderBy(kv => kv.Key))
                if (kv.Key.All(char.IsLetter) && !functionNames.Contains(kv.Key.ToLower()))
                    lastVar = kv.Key;
            if (lastVar != null)
                AddInstruction(OpCode.MOVE, Reg(0), Mem(lastVar));
            EmitExit();

            return BuildProgram("main");
        }

        private VmlProgram GenerateUnitCode(UnitNode ast)
        {
            // 处理interface中的类型声明
            ProcessTypeDeclarations(ast.InterfaceDeclarations);
            ProcessTypeDeclarations(ast.ImplementationDeclarations);

            // interface常量
            foreach (var decl in ast.InterfaceDeclarations)
            {
                if (decl is ConstDeclarationNode constDecl && constDecl.Value is LiteralNode literal)
                {
                    dataSection[constDecl.Name] = Convert.ToInt32(literal.Value);
                    constNames.Add(constDecl.Name);
                }
            }

            // interface变量
            foreach (var decl in ast.InterfaceDeclarations)
            {
                if (decl is VarDeclarationNode varDecl && !dataSection.ContainsKey(varDecl.Name))
                    AllocateVariable(varDecl);
            }
            foreach (var decl in ast.ImplementationDeclarations)
            {
                if (decl is VarDeclarationNode varDecl && !dataSection.ContainsKey(varDecl.Name))
                    AllocateVariable(varDecl);
            }

            // 生成interface子程序
            foreach (var sub in ast.InterfaceSubprograms)
                GenerateSubprogram(sub);
            // 生成implementation子程序
            foreach (var sub in ast.ImplementationSubprograms)
                GenerateSubprogram(sub);

            // 初始化代码
            if (ast.InitializationBlock != null)
            {
                AddLabel($"unit_{ast.Name}_init");
                GenerateBlock(ast.InitializationBlock);
                AddInstruction(OpCode.RET);
            }

            Vars?.LogStats();
            var vmlProgram = new VmlProgram(instructions, labels, dataSection, constants);
            vmlProgram.EntryPoint = $"unit_{ast.Name}_init";

            // 导出 interface 符号 (v1.66.32+: unit 系统)
            foreach (var sub in ast.InterfaceSubprograms)
                vmlProgram.Exports[sub.Name] = sub.Name;
            foreach (var decl in ast.InterfaceDeclarations)
            {
                if (decl is VarDeclarationNode varDecl && dataSection.ContainsKey(varDecl.Name))
                    vmlProgram.Exports[varDecl.Name] = varDecl.Name;
                if (decl is ConstDeclarationNode constDecl && !vmlProgram.Constants.ContainsKey(constDecl.Name))
                    vmlProgram.Exports[constDecl.Name] = constDecl.Name;
            }

            return vmlProgram;
        }

        private void AllocateVariable(VarDeclarationNode varDecl)
        {
            int varSlots = GetVariableSlots(varDecl.Type);
            TypeNode resolvedType = ResolveTypeAlias(varDecl.Type);
            if (resolvedType is ArrayTypeNode arrType)
            {
                if (arrType.IsDynamic)
                    dynamicArrayNames.Add(varDecl.Name);
                else
                {
                    var bounds = new List<(int, int)>();
                    CollectArrayBounds(arrType, bounds);
                    arrayBounds[varDecl.Name] = bounds;
                }
            }
            if (varSlots <= 1)
            {
                dataSection[varDecl.Name] = 0;
            }
            else
            {
                int[] slots = new int[varSlots];
                dataSection[varDecl.Name] = slots;
            }
            globalVarOffsets[varDecl.Name] = dataSection.Count - 1;
            globalVarTypes[varDecl.Name] = GetTypeName(resolvedType);
            globalVarDeclarations[varDecl.Name] = resolvedType;

            if (varDecl.Type is RecordTypeNode recordType)
            {
                string recordTypeName = FindRecordTypeName(recordType);
                if (recordTypeName != null)
                    variableRecordTypes[varDecl.Name] = recordTypeName;
            }
            else if (varDecl.Type is SimpleTypeNode simpleType)
            {
                if (definedRecordTypes.ContainsKey(simpleType.TypeName.ToUpper()))
                    variableRecordTypes[varDecl.Name] = simpleType.TypeName.ToUpper();
            }
            else if (varDecl.Type is ArrayTypeNode arrayType && arrayType.ElementType is SimpleTypeNode elemSimple)
            {
                if (definedRecordTypes.ContainsKey(elemSimple.TypeName.ToUpper()))
                    variableRecordTypes[varDecl.Name] = elemSimple.TypeName.ToUpper();
            }
        }
    }
}
