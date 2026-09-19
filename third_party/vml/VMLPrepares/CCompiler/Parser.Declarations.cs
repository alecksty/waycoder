using CompilerBase;
#nullable disable // auto-generated code, null safety not applicable
using System.Collections.Generic;

namespace CCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
{
        /// <summary>
        /// 解析程序
        /// </summary>
        /// <returns>解析后的程序节点</returns>
        /// <exception cref="Exception"></exception>
        /// <summary>预注入常见类型名到 TypeDefs（sizeof 等操作符需要查询）</summary>
        private static void PreInjectTypeDefs(Program program)
        {
            // 预定义类型表应为空——所有类型应从源代码的typedef/struct声明中获取
            // sizeof等操作符在代码生成阶段解析类型, 不需要预注入
        }

        public Program Parse()
        {
            var program = new Program();
            this.program = program;

            // 预注入常见类型（sizeof 等操作符需要在解析时就能查询 TypeDefs）
            PreInjectTypeDefs(program);

            int maxIterations = tokens.Count * 5 + 10000; // safety: 5 passes per token
            int safetyCounter = 0;

            while (Current().Type != TokenType.EOF)
            {
                if (++safetyCounter > maxIterations)
                    throw Error($"Parser: 超过最大迭代次数 ({maxIterations}), 可能死循环");
                try {
                // 处理预处理器指令
                if (Current().Type == TokenType.INCLUDE  ||
                    Current().Type == TokenType.DEFINE   ||
                    Current().Type == TokenType.IF_PRE   ||
                    Current().Type == TokenType.ELSE_PRE ||
                    Current().Type == TokenType.ENDIF    ||
                    Current().Type == TokenType.IFDEF    ||
                    Current().Type == TokenType.IFNDEF)
                {
                    ParsePreprocessorDirective();
                }
                // 处理enum — 枚举定义
                else if (Current().Type == TokenType.ENUM)
                {
                    ParseEnum();
                }
                // struct/union definitions (NOT struct-typed declarations)
                else if (Current().Type == TokenType.STRUCT && IsStructDefinition())
                {
                    ParseStruct();
                }
                else if (Current().Type == TokenType.UNION && IsUnionDefinition())
                {
                    ParseUnion();
                }
                // 函数或全局变量声明，支持extern/interrupt关键字
                else if (Current().Type == TokenType.INTERRUPT)
                {
                    _pendingInterrupt = true;
                    Advance();
                }
                else if (Current().Type == TokenType.STDCALL || Current().Type == TokenType.FASTCALL || Current().Type == TokenType.CDECL)
                {
                    _pendingConvention = Current().Type;
                    Advance();
                }
                else if (Current().Type == TokenType.INT     || Current().Type == TokenType.VOID  ||
                         Current().Type == TokenType.CHAR    || Current().Type == TokenType.FLOAT ||
                         Current().Type == TokenType.DOUBLE  || Current().Type == TokenType.BOOL   ||
                         Current().Type == TokenType.SHORT   || Current().Type == TokenType.LONG     ||
                         Current().Type == TokenType.SIGNED  || Current().Type == TokenType.UNSIGNED ||
                         Current().Type == TokenType.CONST   || Current().Type == TokenType.VOLATILE ||
                         Current().Type == TokenType.AUTO    || Current().Type == TokenType.REGISTER ||
                         Current().Type == TokenType.STATIC  || Current().Type == TokenType.EXTERN   ||
                         Current().Type == TokenType.INLINE  || Current().Type == TokenType.TYPEDEF ||
                         Current().Type == TokenType.STRUCT  || Current().Type == TokenType.UNION   ||
                         Current().Type == TokenType.SIZE_T   || Current().Type == TokenType.SSIZE_T    ||
                         Current().Type == TokenType.PTRDIFF_T ||
                         Current().Type == TokenType.INT8     || Current().Type == TokenType.UINT8      ||
                         Current().Type == TokenType.INT16    || Current().Type == TokenType.UINT16     ||
                         Current().Type == TokenType.INT32    || Current().Type == TokenType.UINT32     ||
                         Current().Type == TokenType.INT64    || Current().Type == TokenType.UINT64     ||
                         Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T  ||
                         Current().Type == TokenType.WCHAR_T  || Current().Type == TokenType.CHAR32_T   ||
                         Current().Type == TokenType.IDENTIFIER)
                {
                    // 处理存储类说明符 (支持多个连续关键字: static inline, extern static 等)
                    string storageClass = null;
                    while (Current().Type == TokenType.AUTO     ||
                           Current().Type == TokenType.REGISTER ||
                           Current().Type == TokenType.STATIC   ||
                           Current().Type == TokenType.EXTERN   ||
                           Current().Type == TokenType.INLINE   ||
                           Current().Type == TokenType.TYPEDEF)
                    {
                        if (storageClass == null)
                            storageClass = Current().Value.ToString();
                        Advance(); // 跳过存储类说明符
                    }

                    // 处理 typedef struct/union { ... } Name;
                    if (storageClass == "typedef" && Current().Type == TokenType.STRUCT)
                    {
                        ParseToplevelTypedefStruct();
                        continue;
                    }
                    if (storageClass == "typedef" && Current().Type == TokenType.UNION)
                    {
                        ParseToplevelTypedefUnion();
                        continue;
                    }
                    if (storageClass == "typedef" && Current().Type == TokenType.ENUM)
                    {
                        ParseToplevelTypedefEnum();
                        continue;
                    }

                    if (storageClass == null)
                    {
                        if (Current().Type                == TokenType.IDENTIFIER
                            && Current().Value.ToString() == "extern")
                        {
                            storageClass = "extern";
                            Advance(); // 跳过extern关键字
                        }
                    }

                    // 处理类型修饰符（如 const, volatile）
                    string typeName = "";
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                    {
                        typeName += Current().Value.ToString() + " ";
                        Advance();
                    }

                    // 处理基本类型（可能由多个token组成，如 "unsigned int"）
                    var parsedType = ParseTypeSpecifiers();
                    if (!string.IsNullOrEmpty(parsedType))
                    {
                        typeName += parsedType;
                    }
                    else
                    {
                        // 如果没有解析到类型说明符，尝试读取一个标识符作为类型名
                        // wchar_t/char32_t 也可以是变量名/typedef 目标名
                        if (Current().Type == TokenType.IDENTIFIER ||
                            Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T)
                        {
                            // 检查是否是隐式 int 函数声明（标识符后跟 (）
                            if (Peek(1).Type == TokenType.LPAREN)
                            {
                                // 这是一个隐式 int 返回类型的函数声明
                                typeName = "int";
                                var funcName = Advance().Value.ToString();
                                Match(TokenType.LPAREN); // 跳过 (
                                var func = ParseFunction(typeName, funcName, _pendingInterrupt, _pendingConvention);
                                _pendingInterrupt = false;
                                _pendingConvention = TokenType.EOF;
                                program.Functions.Add(func);
                                continue;
                            }
                            else
                            {
                                typeName += Advance().Value.ToString();
                            }
                        }
                        else
                        {
                            Error("期望类型说明符");
                        }
                    }

                    // 处理后置 const/volatile: sqlite3_mutex_methods const *
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                    {
                        typeName += " " + Advance().Value.ToString();
                    }
                    // 处理未知类型修饰符: static LONG SQLITE_WIN32_VOLATILE → 连续IDENTIFIER归入typeName
                    while (Current().Type == TokenType.IDENTIFIER &&
                           !program.TypeDefs.ContainsKey(Current().Value.ToString()) &&
                           Peek(1).Type == TokenType.IDENTIFIER)
                    {
                        typeName += " " + Advance().Value.ToString();
                    }
                    // 先处理指针类型（在类型名称后可能有*）
                    while (Match(TokenType.STAR))
                        typeName += "*";
                    // 处理指针后的 const: char * const
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                    {
                        typeName += " " + Advance().Value.ToString();
                    }

                    // 处理函数指针: void* (*name)(params), 支持嵌套 void (*(*name)(p))(ret)
                    bool isFuncPtr = Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR;
                    if (isFuncPtr)
                    {
                        int fpInnerCount = 0;  // count of inner (* groups (excl. outer)
                        Advance(); // skip outer (
                        // 处理多层 * 指针: void (*(*name)(...)) — 递归跳过 (* 前缀
                        while (Current().Type == TokenType.STAR)
                        {
                            Match(TokenType.STAR);
                            typeName += "*";
                            // 嵌套函数指针: (* 后又跟 ( — count inner opens
                            if (Current().Type == TokenType.LPAREN)
                            {
                                Advance(); // skip inner (
                                fpInnerCount++;
                                continue;  // loop to handle the next *
                            }
                        }
                        // 现在期望标识符 (函数指针变量名)
                        if (Current().Type == TokenType.IDENTIFIER ||
                            Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 ||
                            Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 ||
                            Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 ||
                            Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 ||
                            Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T ||
                            Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T ||
                            Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                        {
                            var name = Advance().Value.ToString();
                            // 处理函数指针数组: int(*fa[N])() — 在标识符后、右括号前解析 [N]
                            bool isArray = false;
                            int? arraySize = null;
                            while (Current().Type == TokenType.LBRACKET)
                            {
                                isArray = true;
                                Advance(); // skip [
                                if (Current().Type == TokenType.RBRACKET)
                                {
                                    // 空维度: int(*fa[])()
                                    Advance(); // skip ]
                                }
                                else
                                {
                                    var dimExpr = ParseExpression();
                                    // TryConstDim（不是 TryConstInt）：函数指针数组的维度
                                    // 同样不能是负数/溢出（见 Parser.cs 的注释）
                                    if (TryConstDim(dimExpr, out var dimVal))
                                        arraySize = dimVal;
                                    Expect(TokenType.RBRACKET);
                                }
                            }
                            // 关闭内层 (*name) 的括号 (只关闭 name 之前的嵌套)
                            // close inner (*name) groups + the outer (*name) paren
                            int rpToClose = fpInnerCount + 1; // +1 for outer (*name)
                            while (Current().Type == TokenType.RPAREN && rpToClose > 0)
                            { Advance(); rpToClose--; }
                            // 函数参数列表
                            if (Match(TokenType.LPAREN))
                            {
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LPAREN) depth++;
                                    else if (Current().Type == TokenType.RPAREN) depth--;
                                    if (depth > 0) Advance();
                                }
                                Advance();
                            }
                            // 消费剩余的右括号（如 void (*(*name)(params)) 中外层闭括号）
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN)
                            { Advance(); rpToClose--; }
                            // 嵌套函数指针返回类型: void (*(*name)(params))(void)
                            if (Current().Type == TokenType.LPAREN)
                            {
                                Advance();
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LPAREN) depth++;
                                    else if (Current().Type == TokenType.RPAREN) depth--;
                                    if (depth > 0) Advance();
                                }
                                Advance();
                                typeName += "(*)";
                            }
                            // typedef 函数指针: typedef int (*name)(params) -> TypeDefs[name] = "int*"
                            if (storageClass == "typedef")
                            {
                                program.TypeDefs[name] = typeName;
                            }
                            // 函数指针函数定义: void (*func(params))(ret) { ... }
                            if (Current().Type == TokenType.LBRACE)
                            {
                                // ParseBlock() consumes { ... } body
                                Block body = ParseBlock();
                                var func = new Function(name, typeName, new List<Parameter>(), body, name == "main");
                                program.Functions.Add(func);
                                continue;
                            }
                            // 注册函数指针变量（非 typedef / 非函数定义时）
                            VariableDecl? fpVar = null;
                            if (storageClass != "typedef")
                            {
                                fpVar = new VariableDecl(name, typeName, null);
                                if (isArray)
                                {
                                    fpVar.IsArray = true;
                                    fpVar.ArraySize = arraySize;
                                }
                                program.Variables.Add(fpVar);
                            }
                            // 处理初始化和后续声明
                            if (Match(TokenType.ASSIGN))
                            {
                                if (Current().Type == TokenType.LBRACE)
                                {
                                    var init = ParseInitializerList();
                                    if (fpVar != null) fpVar.Initializer = init;
                                }
                                else
                                {
                                    var init = ParseAssignment();
                                    if (fpVar != null) fpVar.Initializer = init;
                                }
                            }
                            while (Match(TokenType.COMMA)) { /* multi-decl */ Advance(); }
                            Expect(TokenType.SEMICOLON);
                            continue;
                        }
                        // Not a func ptr pattern: continue to identifier check
                    }

                    // struct/union 变量带内联初始化器: struct Name { ... } var;
                    if (Current().Type == TokenType.LBRACE && (typeName.Contains("struct") || typeName.Contains("union")))
                    {
                        bool inlineIsUnion = typeName.Contains("union");
                        Advance(); // {
                        // 解析结构体/联合体成员并构建 StructDecl
                        var inlineStructDecl = new StructDecl("");
                        int inlineOffset = 0;
                        int inlineMaxSize = 0;
                        while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                        {
                            Token memType = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE,
                                TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED,
                                TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION,
                                TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64,
                                TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64,
                                TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T,
                                TokenType.PTRDIFF_T, TokenType.BOOL);
                            string memTypeName = memType.Value.ToString();
                            if (memType.Type == TokenType.STRUCT || memType.Type == TokenType.UNION)
                            { if (Current().Type == TokenType.IDENTIFIER) memTypeName += " " + Advance().Value.ToString(); }
                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED ||
                                   Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST ||
                                   Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR ||
                                   Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT ||
                                   Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT ||
                                   Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT ||
                                   Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 ||
                                   Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 ||
                                   Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 ||
                                   Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 ||
                                   Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T ||
                                   Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T ||
                                   Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T ||
                                   Current().Type == TokenType.BOOL)
                            { string m = Advance().Value.ToString(); memTypeName += " " + m;
                              if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER)
                                  memTypeName += " " + Advance().Value.ToString(); }
                            if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                                memTypeName += " " + Advance().Value.ToString();
                            while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                                memTypeName += " " + Advance().Value.ToString();
                            // 处理指针
                            int memPtrLevel = 0;
                            while (Current().Type == TokenType.STAR) { memPtrLevel++; Advance(); }
                            string effMemType = memTypeName + new string('*', memPtrLevel);
                            // 跳过嵌套匿名 struct/union (简单 bracket-count)
                            if (Current().Type == TokenType.LBRACE && (memType.Type == TokenType.STRUCT || memType.Type == TokenType.UNION))
                            {
                                Advance(); int nd=1; while(nd>0&&Current().Type!=TokenType.EOF)
                                { if(Current().Type==TokenType.LBRACE)nd++; else if(Current().Type==TokenType.RBRACE){nd--;if(nd==0)break;}Advance(); }
                                Expect(TokenType.RBRACE); while(Current().Type==TokenType.STAR) Advance();
                                if(Current().Type==TokenType.IDENTIFIER) Advance();
                                Expect(TokenType.SEMICOLON); continue;
                            }
                            do {
                                string memName; bool memBf=false;
                                if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                { Advance(); Match(TokenType.STAR); effMemType+="*"; memName=Expect(TokenType.IDENTIFIER).Value.ToString();
                                  Expect(TokenType.RPAREN);
                                  if (Match(TokenType.LPAREN)) { int d=1; while(d>0&&Current().Type!=TokenType.EOF)
                                  { if(Current().Type==TokenType.LPAREN)d++; else if(Current().Type==TokenType.RPAREN){d--;if(d==0){Advance();break;}}Advance(); } } }
                                else if (Current().Type == TokenType.COLON)
                                { memName=""; memBf=true; Advance(); if(Current().Type==TokenType.NUMBER) Advance();
                                  else while(Current().Type!=TokenType.COMMA&&Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF) Advance(); }
                                else { memName = Expect(TokenType.IDENTIFIER).Value.ToString(); }
                                if (!memBf && Current().Type == TokenType.COLON)
                                { memBf=true; Advance(); if(Current().Type==TokenType.NUMBER) Advance();
                                  else while(Current().Type!=TokenType.COMMA&&Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF) Advance(); }
                                bool mArr=false; int? mArrSz=null; List<int?> mDims=new List<int?>();
                                while (Match(TokenType.LBRACKET)) { mArr=true;
                                  if(Current().Type==TokenType.NUMBER&&Peek(1).Type==TokenType.RBRACKET)
                                  { int dim=Convert.ToInt32(Current().Value); if(mArrSz==null)mArrSz=dim; else mArrSz*=dim; mDims.Add(dim); Advance(); }
                                  else { mDims.Add(null); int d2=1; while(d2>0&&Current().Type!=TokenType.EOF)
                                  { if(Current().Type==TokenType.LBRACKET)d2++; else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance(); }}
                                  Expect(TokenType.RBRACKET); }
                                var im = new StructMember(memName, effMemType, mArr, mArrSz) { Offset = inlineIsUnion ? inlineOffset : inlineOffset, Dimensions = mDims, IsBitfield = memBf };
                                inlineStructDecl.Members.Add(im);
                                int ms = GetMemberSize(effMemType, mArr, mArrSz);
                                if (inlineIsUnion) { if (ms > inlineMaxSize) inlineMaxSize = ms; } else { inlineOffset += ms; }
                            } while (Match(TokenType.COMMA));
                            Expect(TokenType.SEMICOLON);
                        }
                        Expect(TokenType.RBRACE);
                        if (inlineIsUnion) inlineOffset = inlineMaxSize;
                        // 处理指针: struct { ... } *name;
                        while (Current().Type == TokenType.STAR) { typeName += "*"; Advance(); }
                        // 变量名
                        string inlineVarName = null;
                        if (Current().Type == TokenType.IDENTIFIER)
                        {
                            inlineVarName = Advance().Value.ToString();
                        }
                        // 生成匿名 struct/union 的标签名
                        string anonTag = inlineVarName != null ? $"_anon_{inlineVarName}" : "_anon_struct";
                        if (program.Structs.ContainsKey(anonTag))
                        {
                            int ctr = 0;
                            while (program.Structs.ContainsKey($"{anonTag}_{ctr}")) ctr++;
                            anonTag = $"{anonTag}_{ctr}";
                        }
                        inlineStructDecl.Name = anonTag;
                        program.Structs[anonTag] = inlineStructDecl;
                        string structTypeRef = (inlineIsUnion ? "union " : "struct ") + anonTag;
                        // 处理数组维度: name[10], name[]
                        bool isArr = false; int? arrSize = null; List<int?> arrDims = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArr = true;
                            if (Current().Type == TokenType.RBRACKET)
                            { arrDims.Add(null); Advance(); }
                            else
                            {
                                // 解析数组大小
                                if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                                {
                                    int dim = Convert.ToInt32(Current().Value); arrDims.Add(dim);
                                    if (arrSize == null) arrSize = dim; else arrSize *= dim; Advance();
                                }
                                else { arrDims.Add(null); int d2=1; while(d2>0&&Current().Type!=TokenType.EOF)
                                { if(Current().Type==TokenType.LBRACKET)d2++; else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance(); }}
                                Expect(TokenType.RBRACKET);
                            }
                        }
                        // 注册变量
                        if (inlineVarName != null)
                        {
                            var inlineVar = new VariableDecl(inlineVarName, structTypeRef, null)
                            {
                                IsArray = isArr,
                                ArraySize = arrSize,
                                Dimensions = arrDims
                            };
                            if (storageClass == "static") inlineVar.IsStatic = true;
                            // 处理初始化器: = { ... }
                            if (Match(TokenType.ASSIGN))
                            {
                                if (Current().Type == TokenType.LBRACE)
                                {
                                    var initList = ParseInitializerList();
                                    inlineVar.Initializer = initList;
                                }
                                else
                                {
                                    var initExpr = ParseAssignment();
                                    inlineVar.Initializer = initExpr;
                                }
                            }
                            program.Variables.Add(inlineVar);
                        }
                        Expect(TokenType.SEMICOLON);
                        continue;
                    }

                    if (Current().Type == TokenType.LPAREN ||
                        Current().Type == TokenType.IDENTIFIER ||
                        Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 ||
                        Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 ||
                        Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 ||
                        Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 ||
                        Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T ||
                        Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T ||
                        Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        // 支持 type (name)(params) 语法（Lua API: extern int (name)(params)）
                        string name;
                        Token nameTok;
                        if (Current().Type == TokenType.LPAREN)
                        {
                            Advance(); // (
                            nameTok = Advance(); // function name
                            name = nameTok.Value.ToString();
                            Expect(TokenType.RPAREN);
                        }
                        else
                        {
                            nameTok = Advance();
                            name = nameTok.Value.ToString();
                        }
                        // 函数名 token 的行列 —— 供 `Function.Line/Column` 用
                        // （未使用函数的警告要指到**声明处**，见 `WarnUnused`）。
                        int funcLine = nameTok.Line, funcCol = nameTok.Column;

                        // 函数
                        if (Match(TokenType.LPAREN))
                        {
                            // 检查是否是函数声明（后面跟着分号）还是函数定义（后面跟着大括号）
                            var isDeclaration = false;
                            var savePos       = pos;

                            // 向前看，检查是声明还是定义（跟踪括号嵌套深度）
                            var tempPos = pos;
                            int declParenDepth = 1;
                            while (tempPos < tokens.Count && declParenDepth > 0)
                            {
                                var tt = tokens[tempPos].Type;
                                if (tt == TokenType.LPAREN) declParenDepth++;
                                else if (tt == TokenType.RPAREN) declParenDepth--;
                                else if (declParenDepth == 1 && (tt == TokenType.SEMICOLON || tt == TokenType.LBRACE))
                                    break;
                                tempPos++;
                            }
                            if (declParenDepth == 0)
                            {
                                while (tempPos < tokens.Count
                                       && tokens[tempPos].Type != TokenType.SEMICOLON
                                       && tokens[tempPos].Type != TokenType.LBRACE)
                                    tempPos++;
                            }
                            if (tempPos                 < tokens.Count
                                && tokens[tempPos].Type == TokenType.SEMICOLON)
                            {
                                isDeclaration = true;
                            }

                            if (isDeclaration)
                            {
                                // 处理函数声明，只解析参数列表，不解析函数体
                                List<Parameter> parameters = new List<Parameter>();
                                bool isVariadic = false;
                                if (!Match(TokenType.RPAREN))
                                {
                                    while (true)
                                    {
                                        if (Current().Type == TokenType.CONST  || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.INT || Current().Type == TokenType.VOID ||
                                            Current().Type == TokenType.CHAR   || Current().Type == TokenType.FLOAT    ||
                                            Current().Type == TokenType.DOUBLE ||
                                            Current().Type == TokenType.SHORT  || Current().Type == TokenType.LONG     ||
                                            Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED ||
                                            Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION   ||
                                            Current().Type == TokenType.IDENTIFIER ||
                                            Current().Type == TokenType.INT8  || Current().Type == TokenType.INT16    ||
                                            Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64    ||
                                            Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16   ||
                                            Current().Type == TokenType.UINT32|| Current().Type == TokenType.UINT64   ||
                                            Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T ||
                                            Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T ||
                                            Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                                        {
                                            // 检查是否为空参数列表 (void)
                                            string preCheckedType = null;
                                            if (Current().Type == TokenType.VOID)
                                            {
                                                Advance(); // 跳过 void
                                                preCheckedType = "void";
                                                // 检查是否立即是右括号
                                                if (Current().Type == TokenType.RPAREN)
                                                {
                                                    break;
                                                }
                                                // 如果不是右括号，说明 void 是参数类型的一部分，继续解析
                                            }

                                            string paramTypeName = preCheckedType ?? "";

                                            // 处理 const/volatile 修饰符
                                            while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                                            {
                                                paramTypeName += Current().Value.ToString() + " ";
                                                Advance();
                                            }

                                            // 函数指针参数: 必须在 Advance 之前检查，因为 void(*name) 中 ( 会先出现
                                            if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                            {
                                                // 先确定基础类型名: void(*name) → base type is void
                                                string fpBaseType = paramTypeName;
                                                Advance(); Match(TokenType.STAR);
                                                fpBaseType += "(*)";
                                                // 保存函数指针变量名: int(*f)(int) → f
                                                string fpName = Current().Type == TokenType.IDENTIFIER ? Advance().Value.ToString() : "";
                                                // 函数指针数组: int(*fa[2])(int)
                                                while (Match(TokenType.LBRACKET)) {
                                                    while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                                                    Expect(TokenType.RBRACKET);
                                                    fpBaseType += "[]";
                                                }
                                                Expect(TokenType.RPAREN);
                                                Expect(TokenType.LPAREN);
                                                int d = 1; while (d > 0 && Current().Type != TokenType.EOF)
                                                { if (Current().Type == TokenType.LPAREN) d++; else if (Current().Type == TokenType.RPAREN) d--; if (d > 0) Advance(); }
                                                if (Current().Type == TokenType.RPAREN) Advance();
                                                while (Match(TokenType.LBRACKET)) {
                                                    while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                                                    Expect(TokenType.RBRACKET);
                                                }
                                                parameters.Add(new Parameter(fpName, fpBaseType));
                                                if (!Match(TokenType.COMMA)) break;
                                                continue;
                                            }

                                            // 处理基本类型
                                            Token paramTypeToken = Advance();
                                            paramTypeName += paramTypeToken.Value.ToString();

                                            // For struct/union param types, consume the tag name
                                            if (paramTypeToken.Type == TokenType.STRUCT || paramTypeToken.Type == TokenType.UNION)
                                            {
                                                if (Current().Type == TokenType.IDENTIFIER)
                                                    paramTypeName += " " + Advance().Value.ToString();
                                            }

                                            // 处理其他类型修饰符 (包括 const/volatile 在类型名之后: char const)
                                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SHORT ||
                                                   Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED ||
                                                   Current().Type == TokenType.CHAR || Current().Type == TokenType.INT ||
                                                   Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE ||
                                                   Current().Type == TokenType.VOID ||
                                                   Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                                            {
                                                paramTypeName += " " + Advance().Value.ToString();
                                            }

                                            // 检查是否为指针类型
                                            while (Match(TokenType.STAR))
                                            {
                                                paramTypeName += "*";
                                                // 指针后的 const/volatile: const T * const name
                                                while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                                                {
                                                    paramTypeName += " " + Current().Value.ToString();
                                                    Advance();
                                                }
                                            }

                                            // 函数指针参数: int (*callback)(void*,int,char**)
                                            if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                            {
                                                Advance(); Match(TokenType.STAR);
                                                paramTypeName += "(*)";
                                                if (Current().Type == TokenType.IDENTIFIER) Advance();
                                                Expect(TokenType.RPAREN);
                                                Expect(TokenType.LPAREN);
                                                int d = 1; while (d > 0 && Current().Type != TokenType.EOF)
                                                { if (Current().Type == TokenType.LPAREN) d++; else if (Current().Type == TokenType.RPAREN) d--; if (d > 0) Advance(); }
                                                if (Current().Type == TokenType.RPAREN) Advance();
                                            }
                                            // 处理数组参数: char *argv[] → char **argv
                                            while (Match(TokenType.LBRACKET))
                                            {
                                                paramTypeName += "*"; // [] 等价于 * (函数参数上下文)
                                                if (!Match(TokenType.RBRACKET))
                                                {
                                                    // 跳过数组大小表达式
                                                    int depth = 1;
                                                    while (depth > 0 && Current().Type != TokenType.EOF)
                                                    {
                                                        if (Current().Type == TokenType.LBRACKET) depth++;
                                                        else if (Current().Type == TokenType.RBRACKET) depth--;
                                                        if (depth > 0) Advance();
                                                    }
                                                    Advance(); // 消费 ]
                                                }
                                            }

                                            string paramName = Current().Type == TokenType.IDENTIFIER ?
                                                Advance().Value.ToString() : "";
                                            // 处理参数名后的数组括号: type name[N] → type* name
                                            while (Match(TokenType.LBRACKET))
                                            {
                                                paramTypeName += "*";
                                                if (!Match(TokenType.RBRACKET))
                                                {
                                                    // 跳过数组大小表达式
                                                    int depth = 1;
                                                    while (depth > 0 && Current().Type != TokenType.EOF)
                                                    {
                                                        if (Current().Type == TokenType.LBRACKET) depth++;
                                                        else if (Current().Type == TokenType.RBRACKET) depth--;
                                                        if (depth > 0) Advance();
                                                    }
                                                    Advance(); // 消费 ]
                                                }
                                            }
                                            parameters.Add(new Parameter(paramName, paramTypeName));
                                        }
                                        else if (Current().Type == TokenType.VOID)
                                        {
                                            Advance(); // void 参数
                                            break;
                                        }
                                        else if (Current().Type == TokenType.ELLIPSIS)
                                        {
                                            Advance(); // 可变参数
                                            isVariadic = true;
                                            break;
                                        }

                                        if (!Match(TokenType.COMMA))
                                        {
                                            break;
                                        }
                                    }
                                    Expect(TokenType.RPAREN);
                                }
                                // 跳过分号
                                Expect(TokenType.SEMICOLON);

                                // 如果已有同名定义, 跳过声明 (定义优先)
                                if (program.Functions.Any(f => f.Name == name && !f.IsDeclaration))
                                {
                                    // 定义已存在, 忽略声明 — 但仍需重置 pending 标志
                                    _pendingInterrupt = false;
                                    _pendingConvention = TokenType.EOF;
                                }
                                else if (!program.Functions.Any(f => f.Name == name))
                                {
                                    // 将函数声明添加到 program.Functions 中
                                    var convention = _pendingConvention switch
                                    {
                                        TokenType.STDCALL => CallingConvention.Stdcall,
                                        TokenType.FASTCALL => CallingConvention.Fastcall,
                                        TokenType.CDECL => CallingConvention.Cdecl,
                                        _ => CallingConvention.Cdecl
                                    };
                                    var func = new Function(name, typeName, parameters, null, false, true, isVariadic, _pendingInterrupt, convention);
                                    _pendingInterrupt = false;
                                    _pendingConvention = TokenType.EOF;
                                    program.Functions.Add(func);
                                }
                            }
                            else
                            {
                                // 处理普通函数定义
                                // `static` 带过去（`storageClass` 是本方法开头那圈存储类说明符扫描留下的）
                                var func = ParseFunction(typeName, name, _pendingInterrupt, _pendingConvention,
                                    storageClass == "static");
                                func.Line = funcLine; func.Column = funcCol;
                                _pendingInterrupt = false;
                                _pendingConvention = TokenType.EOF;
                                // 检查是否已存在同名函数：定义替换声明，重复定义则覆盖
                                var existingDef = program.Functions
                                    .FirstOrDefault(f => f.Name == func.Name && !f.IsDeclaration);
                                var existingDecl = program.Functions
                                    .FirstOrDefault(f => f.Name == func.Name && f.IsDeclaration);

                                if (existingDef != null)
                                {
                                    // 已有定义: 新定义覆盖旧定义
                                    Console.Error.WriteLine($"[WARN] 函数 {func.Name} 重复定义, 使用新定义替换");
                                    program.Functions.Remove(existingDef);
                                }
                                else if (existingDecl != null)
                                {
                                    // 之前只有声明: 移除声明, 添加定义
                                    program.Functions.Remove(existingDecl);
                                }
                                program.Functions.Add(func);
                            }
                        }
                        else
                        {
                            if (storageClass == "typedef")
                            {
                                // typedef 语句
                                // 类型名称已经在typeName中（包括指针*）
                                var typedefTypeName = typeName;

                                // 获取类型别名
                                var aliasName = name;

                                // 处理数组维度 (typedef int arr[10];)
                                while (Match(TokenType.LBRACKET))
                                {
                                    if (Current().Type == TokenType.NUMBER)
                                        typedefTypeName += "[" + Advance().Value.ToString() + "]";
                                    else
                                        typedefTypeName += "[]";
                                    Expect(TokenType.RBRACKET);
                                }
                                // 添加类型定义到程序
                                program.TypeDefs[aliasName] = typedefTypeName;
                                Expect(TokenType.SEMICOLON);
                            }
                            else
                            {
                                // 全局变量（支持逗号分隔的多个变量）
                                VariableDecl var = ParseVariableDecl(typeName, name);
                                program.Variables.Add(var);
                                while (Match(TokenType.COMMA))
                                {
                                    name = Expect(TokenType.IDENTIFIER).Value.ToString();
                                    var = ParseVariableDecl(typeName, name);
                                    program.Variables.Add(var);
                                }
                                Expect(TokenType.SEMICOLON);
                            }
                        }
                    }
                    else
                    {
                        // 容错: 跳过无法识别的声明（如残留的 typedef 名称+分号）
                        Console.Error.WriteLine($"[SKIP] 跳过无法识别的声明: typeName={typeName?.ToString() ?? "null"}, token={Current().Type}");
                        // 如果是 typedef 场景，注册为哑类型以打断级联失败
                        if (storageClass == "typedef" && !string.IsNullOrEmpty(typeName) && Current().Type == TokenType.SEMICOLON)
                        {
                            // typedef 解析失败，尝试提取别名（typeName 的最后一部分）
                            string[] parts = typeName.Split(' ');
                            string alias = parts[parts.Length - 1];
                            if (!program.TypeDefs.ContainsKey(alias))
                                program.TypeDefs[alias] = "int"; // 哑类型
                        }
                        while (Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                            Advance();
                        if (Current().Type == TokenType.SEMICOLON) Advance();
                    }
                }
                else if (Current().Type == TokenType.STAR)
                {
                    // 可能是多行注释残留: ** ... */ — 跳过直到 */
                    while (Current().Type != TokenType.EOF)
                    {
                        if (Current().Type == TokenType.STAR && Peek(1).Type == TokenType.SLASH)
                        {
                            Advance(); Advance(); // 跳过 */
                            break;
                        }
                        Advance();
                    }
                }
                else if (Current().Type == TokenType.STAR)
                {
                    // 可能是多行注释残留: ** ... */ — 跳过直到 */
                    while (Current().Type != TokenType.EOF)
                    {
                        if (Current().Type == TokenType.STAR && Peek(1).Type == TokenType.SLASH)
                        { Advance(); Advance(); break; }
                        Advance();
                    }
                }
                else
                {
                    // 容错模式：跳过无法识别的token继续解析
                    Console.Error.WriteLine($"[SKIP] 跳过无法识别的顶层token: {Current().Type} at line {Current().OriginalLine}");
                    Advance();
                }
                } catch (ParseException ex) when (ex.Code == ErrorCode.Parser_UnexpectedToken) {
                    // 明确的"本编译器不支持…"要**硬失败**，不能走下面的容错恢复（v0.96.183 / patches/0008）。
                    // 恢复机制是为"看起来像笔误或残留"的输入准备的；而"不支持却编得过"会**静默产出错代码**
                    // —— 复合字面量那次就是：地址没被算出来、编译通过不报错、程序画不出东西，
                    // 只有把汇编打出来才看得见。这类错误必须终止编译，而不是记一行然后继续。
                    // ⚠ 因此本错误码被约定为"不可恢复"：以后新增用法前，先确认它真该终止编译。
                    throw;
                } catch (System.Exception ex) {
                    Console.Error.WriteLine($"[RECOVER] 顶层解析异常恢复: {ex.Message}, 行{Current().OriginalLine}, brace深度={_braceDepth}");
                    // 恢复策略: 跳过至当前失败构造结束, 然后跳到下一个有效声明
                    int recoverBraceDepth = _braceDepth;
                    int targetDepth = _braceDepth > 1 ? 1 : 0;
                    while (Current().Type != TokenType.EOF)
                    {
                        if (Current().Type == TokenType.LBRACE) recoverBraceDepth++;
                        else if (Current().Type == TokenType.RBRACE)
                        {
                            recoverBraceDepth--;
                            if (recoverBraceDepth <= targetDepth) { Advance(); break; }
                        }
                        else if (recoverBraceDepth == 0 && Current().Type == TokenType.SEMICOLON)
                        { Advance(); break; }
                        Advance();
                    }
                    // 后恢复清理: 跳过泄漏的函数体代码直到下一个有效声明关键字
                    if (targetDepth > 0) SkipToNextTopLevelDecl();
                }
            }

            return program;
        }

        /// <summary>恢复辅助: 跳过泄漏的函数体代码, 直到下一个顶层声明关键字</summary>
        private void SkipToNextTopLevelDecl()
        {
            int skipBrace = 0;
            while (Current().Type != TokenType.EOF)
            {
                // 遇到顶层声明关键字时停止
                if (skipBrace == 0 && (Current().Type == TokenType.INT || Current().Type == TokenType.VOID ||
                    Current().Type == TokenType.CHAR || Current().Type == TokenType.STATIC ||
                    Current().Type == TokenType.CONST || Current().Type == TokenType.STRUCT ||
                    Current().Type == TokenType.EXTERN || Current().Type == TokenType.TYPEDEF ||
                    Current().Type == TokenType.ENUM || Current().Type == TokenType.SIGNED ||
                    Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.SHORT ||
                    Current().Type == TokenType.LONG || Current().Type == TokenType.DOUBLE ||
                    Current().Type == TokenType.FLOAT || Current().Type == TokenType.BOOL))
                    return;
                if (Current().Type == TokenType.LBRACE) skipBrace++;
                else if (Current().Type == TokenType.RBRACE)
                {
                    if (skipBrace == 0) return; // 顶层}, 停止
                    skipBrace--;
                }
                Advance();
            }
        }

        private void ParsePreprocessorDirective()
        {
            Token directiveToken = Current();
            Advance(); // 跳过指令

            switch (directiveToken.Type)
            {
                case TokenType.INCLUDE:
                    // 处理 #include
                    if (Current().Type == TokenType.STRING)
                    {
                        Advance(); // 跳过文件名
                    }
                    else if (Current().Type == TokenType.LT)
                    {
                        Advance(); // 跳过 <
                        while (Current().Type != TokenType.GT && Current().Type != TokenType.EOF)
                        {
                            Advance(); // 跳过文件名
                        }
                        if (Current().Type == TokenType.GT)
                        {
                            Advance(); // 跳过 >
                        }
                    }
                    break;
                case TokenType.DEFINE:
                    // 处理 #define
                    if (Current().Type == TokenType.IDENTIFIER)
                    {
                        Advance(); // 跳过宏名
                        // 跳过宏值
                        while (Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF)
                        {
                            Advance();
                        }
                    }
                    break;
                case TokenType.IF_PRE:
                case TokenType.IFDEF:
                case TokenType.IFNDEF:
                    // 处理条件指令
                    while (Current().Type != TokenType.ELSE_PRE && Current().Type != TokenType.ENDIF && Current().Type != TokenType.EOF)
                    {
                        Advance();
                    }
                    break;
                case TokenType.ELSE_PRE:
                    // 处理 #else
                    while (Current().Type != TokenType.ENDIF && Current().Type != TokenType.EOF)
                    {
                        Advance();
                    }
                    break;
                case TokenType.ENDIF:
                    // 处理 #endif
                    break;
            }

            // 跳过行尾
            while (Current().Type                != TokenType.EOF
                   && Current().Value.ToString() != "\n")
            {
                Advance();
            }
            if (Current().Type != TokenType.EOF)
            {
                Advance(); // 跳过换行符
            }
        }
    }
}
