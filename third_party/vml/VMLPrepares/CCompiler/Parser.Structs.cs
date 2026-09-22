using CompilerBase;
#nullable disable // auto-generated

using System.Collections.Generic;

namespace CCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {

        /// <summary>
        /// Peek ahead to determine if the current 'struct' token starts a struct definition
        /// (anonymous struct, named struct, or forward declaration), as opposed to a variable
        /// or function declaration that uses a struct type.
        /// Does not consume any tokens.
        /// </summary>
        private bool IsStructDefinition()
        {
            // Current token is 'struct'; peek at the next token (Peek(1))
            // struct { ... }  → definition
            if (Peek(1).Type == TokenType.LBRACE) return true;
            // struct Name { ... }  or  struct Name;  → definition
            if (Peek(1).Type == TokenType.IDENTIFIER && (Peek(2).Type == TokenType.LBRACE || Peek(2).Type == TokenType.SEMICOLON))
                return true;
            // struct Name var/func/* → variable/function declaration (not a definition)
            return false;
        }

        private bool IsUnionDefinition()
        {
            if (Peek(1).Type == TokenType.LBRACE) return true;
            if (Peek(1).Type == TokenType.IDENTIFIER && (Peek(2).Type == TokenType.LBRACE || Peek(2).Type == TokenType.SEMICOLON))
                return true;
            return false;
        }

        private bool ParseStruct()
        {
            Advance(); // struct

            // 前向声明: struct Name; → 注册结构体标签名，跳过
            if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.SEMICOLON)
            {
                string tagName = Advance().Value.ToString(); // struct tag name
                Advance(); // ;
                // 注册为已知结构体类型
                if (!program.Structs.ContainsKey(tagName))
                    program.Structs[tagName] = new StructDecl(tagName);
                return true;
            }

            // 检查是struct定义还是struct变量声明
            if (Peek(0).Type == TokenType.LBRACE)
            {
                // struct定义：struct { int x; int y; }; (匿名struct)
                Expect(TokenType.LBRACE);

                var structDecl = new StructDecl("");
                int offset = 0;

                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    // 结构体成员
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();

                    // 如果是 struct/union 成员类型，消费标签名
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            typeName += " " + Advance().Value.ToString();
                    }

                    // 处理类型修饰符
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 消费 typedef 类型名 (如 sqlite3_module)
                    if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        typeName += " " + Advance().Value.ToString();
                    // 处理后置 const/volatile: sqlite3_mutex_methods const *
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        typeName += " " + Advance().Value.ToString();

                    // 处理指针 (*)
                    int pointerLevel = 0;
                    while (Current().Type == TokenType.STAR)
                    {
                        pointerLevel++;
                        Advance();
                    }

                    // 匿名 struct/union: struct { ... } name; 或 union { ... } name;
                    if (Current().Type == TokenType.LBRACE && (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION))
                    {
                        bool anonIsUnion = typeToken.Type == TokenType.UNION;
                        Advance();
                        int anonBase = offset;
                        int anonMaxSize = 0;
                        while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                        {
                            Token innerType = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                            string innerTypeName = innerType.Value.ToString();
                            if (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION) {
                                if (Current().Type == TokenType.IDENTIFIER) innerTypeName += " " + Advance().Value.ToString();
                            }
                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                                { string m = Advance().Value.ToString(); innerTypeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) innerTypeName += " " + Advance().Value.ToString(); }
                            string effInner = innerTypeName;
                            while (Current().Type == TokenType.STAR) { effInner += "*"; Advance(); }
                            // 嵌套匿名 struct/union
                            if (Current().Type == TokenType.LBRACE && (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION))
                            {
                                Advance(); int nd=1; while(nd>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACE)nd++;else if(Current().Type==TokenType.RBRACE){nd--;if(nd==0)break;}Advance();}
                                Expect(TokenType.RBRACE); while(Current().Type==TokenType.STAR) Advance();
                                if(Current().Type==TokenType.IDENTIFIER) Advance();
                                Expect(TokenType.SEMICOLON); continue;
                            }
                            do {
                                string innerMN; bool innerBF = false;
                                if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                { Advance(); Match(TokenType.STAR); effInner += "*"; innerMN = Expect(TokenType.IDENTIFIER).Value.ToString(); Expect(TokenType.RPAREN);
                                  if (Match(TokenType.LPAREN)) { int d=1; while(d>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LPAREN)d++;else if(Current().Type==TokenType.RPAREN){d--;if(d==0){Advance();break;}}Advance();} } }
                                else if (Current().Type == TokenType.COLON)
                                { innerMN=""; innerBF=true; Advance(); if(Current().Type==TokenType.NUMBER) Advance(); else while(Current().Type!=TokenType.COMMA&&Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF) Advance(); }
                                else { innerMN = Expect(TokenType.IDENTIFIER).Value.ToString(); }
                                if (!innerBF && Current().Type == TokenType.COLON)
                                { innerBF=true; Advance(); if(Current().Type==TokenType.NUMBER) Advance(); else while(Current().Type!=TokenType.COMMA&&Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF) Advance(); }
                                bool iArr = false; int? iArrSz = null; List<int?> iDims = new List<int?>();
                                while (Match(TokenType.LBRACKET)) { iArr=true; if(Current().Type==TokenType.NUMBER&&Peek(1).Type==TokenType.RBRACKET){int dim=Convert.ToInt32(Current().Value);if(iArrSz==null)iArrSz=dim;else iArrSz*=dim;iDims.Add(dim);Advance();}else{iDims.Add(null); int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}} Expect(TokenType.RBRACKET); }
                                var im = new StructMember(innerMN, effInner, iArr, iArrSz); im.Offset = anonIsUnion ? anonBase : offset; im.Dimensions = iDims; im.IsBitfield = innerBF;
                                structDecl.Members.Add(im);
                                int ms = GetMemberSize(effInner, iArr, iArrSz);
                                if (anonIsUnion) { if (ms > anonMaxSize) anonMaxSize = ms; } else { offset += ms; }
                            } while (Match(TokenType.COMMA));
                            Expect(TokenType.SEMICOLON);
                        }
                        Expect(TokenType.RBRACE);
                        if (anonIsUnion) offset = anonBase + anonMaxSize;
                        // Handle pointers after anonymous struct: } *name; or } **name;
                        while (Current().Type == TokenType.STAR) Advance();
                        if (Current().Type == TokenType.IDENTIFIER)
                        {
                            string anonVarName = Advance().Value.ToString(); // variable name
                            // Register as global variable (top-level anonymous struct/union declaration)
                            string anonType = anonIsUnion ? "union" : "struct";
                            program.Variables.Add(new VariableDecl(anonVarName, anonType, null));
                            // Skip array brackets: name[1], name[N]
                            while (Match(TokenType.LBRACKET))
                            {
                                int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}
                                Expect(TokenType.RBRACKET);
                            }
                        }
                        Expect(TokenType.SEMICOLON);
                        continue;
                    }

                    do {
                        // 每个逗号分隔的成员可以有自己的指针: int *a, **b;
                        while (Current().Type == TokenType.STAR) { pointerLevel++; Advance(); }
                        string memberName;
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); // (
                            // 处理嵌套函数指针: void (*(*name)(p))(ret) — 收集所有 (* 前缀
                            int fpInnerCount = 0;
                            while (Current().Type == TokenType.STAR)
                            {
                                Advance(); // *
                                pointerLevel++;
                                if (Current().Type == TokenType.LPAREN)
                                {
                                    Advance(); fpInnerCount++;
                                    continue;
                                }
                            }
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                            // 关闭 name 之前的所有 (
                            int rpToClose = fpInnerCount + 1; // +1 for the outer (
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN)
                            { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN))
                            {
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LPAREN) depth++;
                                    else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } }
                                    Advance();
                                }
                            }
                            // 消费剩余右括号
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN)
                            { Advance(); rpToClose--; }
                            // 嵌套函数指针返回类型: (void)
                            if (Match(TokenType.LPAREN))
                            {
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LPAREN) depth++;
                                    else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } }
                                    Advance();
                                }
                                pointerLevel++; // return type is a pointer to function
                            }
                        }
                        else if (Current().Type == TokenType.COLON)
                        {
                            memberName = "";
                            Advance();
                            if (Current().Type == TokenType.NUMBER) Advance();
                            else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance();
                        }
                        else
                        {
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        }
                        if (Current().Type == TokenType.COLON)
                        {
                            Advance();
                            if (Current().Type == TokenType.NUMBER) Advance();
                            else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance();
                        }
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        if (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                arraySize = Convert.ToInt32(Current().Value);
                                dimensions.Add(arraySize);
                                Advance();
                            }
                            else
                            {
                                // 跳过复杂数组大小表达式: [(11+1)], [SQLITE_N_LIMIT]
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LBRACKET) depth++;
                                    else if (Current().Type == TokenType.RBRACKET) { depth--; if (depth == 0) break; }
                                    Advance();
                                }
                            }
                            Expect(TokenType.RBRACKET);
                        }

                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = offset;
                        member.Dimensions = dimensions;
                        member.PointerLevel = pointerLevel;
                        structDecl.Members.Add(member);
                        offset += pointerLevel > 0 ? 4 : GetMemberSize(typeName, isArray, arraySize);
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);
                structDecl.Size = offset;
                return true;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                string name = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);

                var structDecl = new StructDecl(name);
                int offset = 0;

                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    // 结构体成员
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();

                    // 如果是 struct/union 成员类型，消费标签名
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            typeName += " " + Advance().Value.ToString();
                    }

                    // 处理类型修饰符
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 消费 typedef 类型名 (如 sqlite3_module)
                    if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        typeName += " " + Advance().Value.ToString();
                    // 处理后置 const/volatile: sqlite3_mutex_methods const *
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        typeName += " " + Advance().Value.ToString();

                    // 处理指针 (*)
                    int pointerLevel = 0;
                    while (Current().Type == TokenType.STAR)
                    {
                        pointerLevel++;
                        Advance();
                    }

                    // 匿名 struct/union: struct { ... } name; 或 union { ... } name;
                    if (Current().Type == TokenType.LBRACE && (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION))
                    {
                        bool anonIsUnion = typeToken.Type == TokenType.UNION;
                        Advance();
                        int anonBase = offset;
                        int anonMaxSize = 0;
                        while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                        {
                            Token innerType = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                            string innerTypeName = innerType.Value.ToString();
                            if (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION) {
                                if (Current().Type == TokenType.IDENTIFIER) innerTypeName += " " + Advance().Value.ToString();
                            }
                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                                { string m = Advance().Value.ToString(); innerTypeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) innerTypeName += " " + Advance().Value.ToString(); }
                            string effInner = innerTypeName;
                            while (Current().Type == TokenType.STAR) { effInner += "*"; Advance(); }
                            // 嵌套匿名 struct/union
                            if (Current().Type == TokenType.LBRACE && (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION))
                            {
                                Advance(); int nd=1; while(nd>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACE)nd++;else if(Current().Type==TokenType.RBRACE){nd--;if(nd==0)break;}Advance();}
                                Expect(TokenType.RBRACE); while(Current().Type==TokenType.STAR) Advance();
                                if(Current().Type==TokenType.IDENTIFIER) Advance();
                                Expect(TokenType.SEMICOLON); continue;
                            }
                            do {
                                string innerMN; bool innerBF = false;
                                if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                { Advance(); Match(TokenType.STAR); effInner += "*"; innerMN = Expect(TokenType.IDENTIFIER).Value.ToString(); Expect(TokenType.RPAREN);
                                  if (Match(TokenType.LPAREN)) { int d=1; while(d>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LPAREN)d++;else if(Current().Type==TokenType.RPAREN){d--;if(d==0){Advance();break;}}Advance();} } }
                                else if (Current().Type == TokenType.COLON)
                                { innerMN=""; innerBF=true; Advance(); if(Current().Type==TokenType.NUMBER) Advance(); else while(Current().Type!=TokenType.COMMA&&Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF) Advance(); }
                                else { innerMN = Expect(TokenType.IDENTIFIER).Value.ToString(); }
                                if (!innerBF && Current().Type == TokenType.COLON)
                                { innerBF=true; Advance(); if(Current().Type==TokenType.NUMBER) Advance(); else while(Current().Type!=TokenType.COMMA&&Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF) Advance(); }
                                bool iArr = false; int? iArrSz = null; List<int?> iDims = new List<int?>();
                                while (Match(TokenType.LBRACKET)) { iArr=true; if(Current().Type==TokenType.NUMBER&&Peek(1).Type==TokenType.RBRACKET){int dim=Convert.ToInt32(Current().Value);if(iArrSz==null)iArrSz=dim;else iArrSz*=dim;iDims.Add(dim);Advance();}else{iDims.Add(null); int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}} Expect(TokenType.RBRACKET); }
                                var im = new StructMember(innerMN, effInner, iArr, iArrSz); im.Offset = anonIsUnion ? anonBase : offset; im.Dimensions = iDims; im.IsBitfield = innerBF;
                                structDecl.Members.Add(im);
                                int ms = GetMemberSize(effInner, iArr, iArrSz);
                                if (anonIsUnion) { if (ms > anonMaxSize) anonMaxSize = ms; } else { offset += ms; }
                            } while (Match(TokenType.COMMA));
                            Expect(TokenType.SEMICOLON);
                        }
                        Expect(TokenType.RBRACE);
                        if (anonIsUnion) offset = anonBase + anonMaxSize;
                        // Handle pointers after anonymous struct: } *name; or } **name;
                        while (Current().Type == TokenType.STAR) Advance();
                        if (Current().Type == TokenType.IDENTIFIER)
                        {
                            string anonVarName = Advance().Value.ToString(); // variable name
                            // Register as global variable (top-level anonymous struct/union declaration)
                            string anonType = anonIsUnion ? "union" : "struct";
                            program.Variables.Add(new VariableDecl(anonVarName, anonType, null));
                            // Skip array brackets: name[1], name[N]
                            while (Match(TokenType.LBRACKET))
                            {
                                int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}
                                Expect(TokenType.RBRACKET);
                            }
                        }
                        Expect(TokenType.SEMICOLON);
                        continue;
                    }

                    do {
                        // 每个逗号分隔的成员可以有自己的指针: int *a, **b;
                        while (Current().Type == TokenType.STAR) { pointerLevel++; Advance(); }
                        string memberName;
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); // (
                            int fpInnerCount = 0;
                            while (Current().Type == TokenType.STAR)
                            {
                                Advance(); pointerLevel++;
                                if (Current().Type == TokenType.LPAREN)
                                { Advance(); fpInnerCount++; continue; }
                            }
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                            int rpToClose = fpInnerCount + 1;
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN)
                            { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN))
                            {
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LPAREN) depth++;
                                    else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } }
                                    Advance();
                                }
                            }
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN)
                            { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN))
                            {
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LPAREN) depth++;
                                    else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } }
                                    Advance();
                                }
                                pointerLevel++;
                            }
                        }
                        else if (Current().Type == TokenType.COLON)
                        {
                            memberName = "";
                            Advance();
                            if (Current().Type == TokenType.NUMBER) Advance();
                            else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance();
                        }
                        else
                        {
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        }
                        if (Current().Type == TokenType.COLON)
                        {
                            Advance();
                            if (Current().Type == TokenType.NUMBER) Advance();
                            else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance();
                        }
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        if (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                arraySize = Convert.ToInt32(Current().Value);
                                dimensions.Add(arraySize);
                                Advance();
                            }
                            else
                            {
                                // 跳过复杂数组大小表达式: [(11+1)], [SQLITE_N_LIMIT]
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LBRACKET) depth++;
                                    else if (Current().Type == TokenType.RBRACKET) { depth--; if (depth == 0) break; }
                                    Advance();
                                }
                            }
                            Expect(TokenType.RBRACKET);
                        }

                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = offset;
                        member.Dimensions = dimensions;
                        structDecl.Members.Add(member);
                        offset += GetMemberSize(typeName, isArray, arraySize);
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);
                structDecl.Size = offset;
                program.Structs[name] = structDecl;
                return true;
            }
            else
            {
                // struct变量声明：struct Point p;
                // 解析struct类型名称
                var structTypeName = "struct";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    structTypeName += " " + Advance().Value.ToString();
                }

                // 解析变量名
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    var varName = Advance().Value.ToString();
                    var varDecl = ParseVariableDecl(structTypeName, varName);
                    program.Variables.Add(varDecl);
                    Expect(TokenType.SEMICOLON);
                }
                else
                {
                    Error("期望变量名");
                }
                return true;
            }
        }

        // ── 匿名 struct/union 当成员（`struct { … } name;`）────────────────────────────
        //
        // 这段逻辑原先在 **7 处**解析循环里各抄了一遍，而且**每一处都漏了同一件事**：
        // 内层成员被"拍平"进外层成员表（偏移按外层绝对值算），而中间那个 `name`
        // 成员**从没登记** ⇒ `s.name.field` 找不到 `name`、基址从 0 起算。
        // tty-clock 的 `ttyclock.option.color` 因此恒指到结构体开头（`running` 那一格），
        // 于是 `nsdelay = …` 一写就把 color/date 冲成 0，钟面画不出来。
        //
        // 收成两份：正文**一律按相对偏移累积**，去向再另判 ——
        // 这也是"相对谁归一化"只做一次的地方：两个分支各自把 anonBase 加回去一次，
        // 若"既登记成员、又保留拍平"就会加两遍（翻倍）。

        /// <summary>
        /// 解析匿名的 <c>{ … }</c> 成员表（调用时当前 token 是 <c>{</c>，返回时已消费 <c>}</c>）。
        /// **成员偏移一律相对本结构体（从 0 起）**，整块尺寸见 <see cref="StructDecl.Size"/>。
        /// 支援位域、数组、函数指针、以及任意深度的嵌套匿名 struct/union。
        /// </summary>
        private StructDecl ParseAnonStructBody(bool isUnion)
        {
            Expect(TokenType.LBRACE);
            var body = new StructDecl("");
            int offset = 0;
            int maxSize = 0;

            while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
            {
                var typeToken = Expect(AllTypeTokens);
                string typeName = typeToken.Value.ToString();
                bool anonKeyword = typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION;
                if (anonKeyword && Current().Type == TokenType.IDENTIFIER)
                    typeName += " " + Advance().Value.ToString();
                while (IsTypeModifier())
                {
                    string m = Advance().Value.ToString();
                    typeName += " " + m;
                    if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER)
                    {
                        typeName += " " + Advance().Value.ToString();
                        anonKeyword = true;
                    }
                }
                if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                    typeName += " " + Advance().Value.ToString();
                while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                    typeName += " " + Advance().Value.ToString();

                // 匿名 struct/union 当成员（可继续嵌套）
                if (Current().Type == TokenType.LBRACE && anonKeyword)
                {
                    bool subIsUnion = typeToken.Type == TokenType.UNION;
                    var sub = ParseAnonStructBody(subIsUnion);
                    int bytes = AttachAnonStructMember(sub, subIsUnion, body.Members, isUnion ? 0 : offset);
                    if (isUnion) { if (bytes > maxSize) maxSize = bytes; } else offset += bytes;
                    continue;   // Attach 已吃掉 `;`
                }

                int pointerLevel = 0;
                while (Current().Type == TokenType.STAR) { pointerLevel++; Advance(); }
                do
                {
                    // 每个逗号分隔的成员可以有自己的指针: int *a, b;（b 不是指针）
                    int ptr = pointerLevel;
                    while (Current().Type == TokenType.STAR) { ptr++; Advance(); }
                    string memberName;
                    bool isBitfield = false;
                    if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                    {
                        Advance(); Match(TokenType.STAR); ptr++;
                        memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        Expect(TokenType.RPAREN);
                        if (Match(TokenType.LPAREN))
                        { int d = 1; while (d > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LPAREN) d++; else if (Current().Type == TokenType.RPAREN) { d--; if (d == 0) { Advance(); break; } } Advance(); } }
                    }
                    else if (Current().Type == TokenType.COLON)
                    {
                        memberName = ""; isBitfield = true; Advance();
                        if (Current().Type == TokenType.NUMBER) Advance();
                        else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance();
                    }
                    else memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    if (!isBitfield && Current().Type == TokenType.COLON)
                    {
                        // 位宽照旧**不入 StructMember** —— 全仓没有任何一处写过 BitWidth，
                        // 只在这里写会让"匿名正文里的位域"与别处行为不一致，另开一轮统一改。
                        isBitfield = true; Advance();
                        if (Current().Type == TokenType.NUMBER) Advance();
                        else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance();
                    }
                    bool isArr = false; int? arrSize = null; var dims = new List<int?>();
                    while (Match(TokenType.LBRACKET))
                    {
                        isArr = true;
                        if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                        { int dim = Convert.ToInt32(Current().Value); if (arrSize == null) arrSize = dim; else arrSize *= dim; dims.Add(dim); Advance(); }
                        else
                        { dims.Add(null); int d2 = 1; while (d2 > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LBRACKET) d2++; else if (Current().Type == TokenType.RBRACKET) { d2--; if (d2 == 0) break; } Advance(); } }
                        Expect(TokenType.RBRACKET);
                    }
                    string effType = typeName + new string('*', ptr);
                    var member = new StructMember(memberName, effType, isArr, arrSize);
                    member.Offset = isUnion ? 0 : offset;
                    member.Dimensions = dims;
                    member.PointerLevel = ptr;
                    member.IsBitfield = isBitfield;
                    body.Members.Add(member);
                    int ms = GetMemberSize(effType, isArr, arrSize);
                    if (isUnion) { if (ms > maxSize) maxSize = ms; } else offset += ms;
                } while (Match(TokenType.COMMA));
                Expect(TokenType.SEMICOLON);
            }
            Expect(TokenType.RBRACE);
            body.Size = isUnion ? maxSize : offset;
            return body;
        }

        /// <summary>
        /// 把 <see cref="ParseAnonStructBody"/> 的结果登记进外层成员表。
        /// 进入时当前 token 在 <c>}</c> 之后；返回时已消费 <c>;</c>，返回值是该成员占用的字节数。
        ///
        /// <para>**有名字**（<c>} option;</c>）：整块登记成一张独立类型，外层只加**一条** <c>option</c>
        /// 成员、偏移 = anonBase —— 成员**不再**拍平进外层（C 里 <c>struct { … } name;</c>
        /// 的成员本就不提升）。</para>
        /// <para>**没名字**（<c>};</c>，C11 匿名成员）：把成员**提升**进外层，各自偏移 + anonBase。</para>
        /// </summary>
        private int AttachAnonStructMember(StructDecl body, bool isUnion, List<StructMember> outerMembers, int anonBase)
        {
            int pointerLevel = 0;
            while (Current().Type == TokenType.STAR) { pointerLevel++; Advance(); }

            string memberName = null;
            if (Current().Type == TokenType.IDENTIFIER) memberName = Advance().Value.ToString();

            bool isArr = false; int? arrSize = null; var dims = new List<int?>();
            while (Match(TokenType.LBRACKET))
            {
                isArr = true;
                if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                { int dim = Convert.ToInt32(Current().Value); if (arrSize == null) arrSize = dim; else arrSize *= dim; dims.Add(dim); Advance(); }
                else
                { dims.Add(null); int d2 = 1; while (d2 > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LBRACKET) d2++; else if (Current().Type == TokenType.RBRACKET) { d2--; if (d2 == 0) break; } Advance(); } }
                Expect(TokenType.RBRACKET);
            }
            Expect(TokenType.SEMICOLON);

            if (memberName != null)
            {
                string tag = MakeAnonTag(memberName);
                body.Name = tag;
                if (isUnion)
                {
                    var ud = new UnionDecl(tag);
                    ud.Members = body.Members;
                    ud.Size = body.Size;
                    program.Unions[tag] = ud;
                }
                else program.Structs[tag] = body;

                var member = new StructMember(memberName,
                    (isUnion ? "union " : "struct ") + tag + new string('*', pointerLevel), isArr, arrSize);
                member.Offset = anonBase;   // ← 基址只在这里加这一次
                member.Dimensions = dims;
                member.PointerLevel = pointerLevel;
                outerMembers.Add(member);

                if (pointerLevel > 0) return 4;
                if (isArr && arrSize.HasValue) return body.Size * arrSize.Value;
                return body.Size;
            }

            foreach (var inner in body.Members)
            {
                var promoted = new StructMember(inner.Name, inner.Type, inner.IsArray, inner.ArraySize);
                promoted.Offset = inner.Offset + anonBase;   // ← 内层偏移本就相对，这里才加基址
                promoted.Dimensions = inner.Dimensions;
                promoted.PointerLevel = inner.PointerLevel;
                promoted.IsBitfield = inner.IsBitfield;
                promoted.BitWidth = inner.BitWidth;
                promoted.BitOffset = inner.BitOffset;
                outerMembers.Add(promoted);
            }
            return body.Size;
        }

        /// <summary>给匿名 struct/union 合成一个类型名（<c>_anon_&lt;成员名&gt;</c>，重名时加序号）。</summary>
        private string MakeAnonTag(string memberName)
        {
            string tag = "_anon_" + memberName;
            if (program.Structs.ContainsKey(tag) || program.Unions.ContainsKey(tag))
            {
                int ctr = 0;
                while (program.Structs.ContainsKey(tag + "_" + ctr) || program.Unions.ContainsKey(tag + "_" + ctr)) ctr++;
                tag = tag + "_" + ctr;
            }
            return tag;
        }
        private int GetMemberSize(string typeName, bool isArray, int? arraySize)
        {
            int baseSize = GetBaseTypeSize(typeName);
            if (isArray && arraySize.HasValue)
            {
                return baseSize * arraySize.Value;
            }
            return baseSize;
        }

        private int GetBaseTypeSize(string typeName)
        {
            var t = typeName.ToLower().Trim();
            if (t == "char" || t == "signed char" || t == "unsigned char") return 1;
            if (t == "short" || t == "signed short" || t == "unsigned short" || t == "short int") return 2;
            if (t == "int" || t == "signed" || t == "unsigned" || t == "signed int" || t == "unsigned int") return 4;
            if (t == "long" || t == "signed long" || t == "unsigned long" || t == "long int") return 4;
            if (t == "float") return 4;
            if (t == "double" || t == "long double") return 8;
            if (t.Contains("*")) return 4;
            // Check struct types: "struct name" or typedef alias
            if (t.StartsWith("struct "))
            {
                string name = t.Substring(7);
                foreach (var skv in program.Structs)
                {
                    if (skv.Key.ToLower() == name && skv.Value.Size > 0)
                        return skv.Value.Size;
                }
            }
            if (t.StartsWith("union "))
            {
                string name = t.Substring(6);
                foreach (var ukv in program.Unions)
                {
                    if (ukv.Key.ToLower() == name && ukv.Value.Size > 0)
                        return ukv.Value.Size;
                }
            }
            // Check typedef alias (case-insensitive)
            foreach (var kv in program.TypeDefs)
            {
                if (kv.Key.ToLower() == t)
                {
                    if (kv.Value == "struct")
                    {
                        // Look up struct by the typedef name (case-insensitive)
                        foreach (var skv in program.Structs)
                        {
                            if (skv.Key.ToLower() == t && skv.Value.Size > 0)
                                return skv.Value.Size;
                        }
                    }
                    else if (kv.Value == "union")
                    {
                        foreach (var ukv in program.Unions)
                        {
                            if (ukv.Key.ToLower() == t && ukv.Value.Size > 0)
                                return ukv.Value.Size;
                        }
                    }
                    break;
                }
            }
            // Direct lookup by name in structs/unions (case-insensitive)
            foreach (var skv in program.Structs)
            {
                if (skv.Key.ToLower() == t && skv.Value.Size > 0)
                    return skv.Value.Size;
            }
            foreach (var ukv in program.Unions)
            {
                if (ukv.Key.ToLower() == t && ukv.Value.Size > 0)
                    return ukv.Value.Size;
            }
            return 4; // default
        }

        private bool ParseUnion()
        {
            Advance(); // union

            // 前向声明: union Name; → 注册联合体标签名，跳过
            if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.SEMICOLON)
            {
                string tagName = Advance().Value.ToString();
                Advance(); // ;
                if (!program.Unions.ContainsKey(tagName))
                    program.Unions[tagName] = new UnionDecl(tagName);
                return true;
            }

            // 检查是union定义还是union变量声明
            if (Peek(0).Type == TokenType.LBRACE)
            {
                // union定义：union { int i; float f; }; (匿名union)
                Expect(TokenType.LBRACE);

                var unionDecl = new UnionDecl("");
                int maxSize = 0;

                while (Current().Type != TokenType.RBRACE)
                {
                    // 联合体成员
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();

                    // 如果是 struct/union 成员类型，消费标签名
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            typeName += " " + Advance().Value.ToString();
                    }

                    // 处理类型修饰符
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 消费 typedef 类型名 (如 sqlite3_module)
                    if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        typeName += " " + Advance().Value.ToString();

                    // 匿名 struct/union: struct { ... } name;
                    if (Current().Type == TokenType.LBRACE && (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION))
                    {
                        Advance(); // {
                        while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                        {
                            Token innerType = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                            string innerTN = innerType.Value.ToString();
                            if (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION)
                            { if (Current().Type == TokenType.IDENTIFIER) innerTN += " " + Advance().Value.ToString(); }
                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                                innerTN += " " + Advance().Value.ToString();
                            while (Current().Type == TokenType.STAR) Advance();
                            // 嵌套匿名 struct/union
                            if (Current().Type == TokenType.LBRACE && (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION))
                            {
                                Advance(); // {
                                int nd = 1; while (nd > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LBRACE) nd++; else if (Current().Type == TokenType.RBRACE) { nd--; if (nd == 0) break; } Advance(); }
                                Expect(TokenType.RBRACE);
                                while (Current().Type == TokenType.STAR) Advance();
                                if (Current().Type == TokenType.IDENTIFIER) Advance();
                                Expect(TokenType.SEMICOLON);
                                continue;
                            }
                            do {
                                string imn = Current().Type == TokenType.COLON ? "" : Expect(TokenType.IDENTIFIER).Value.ToString();
                                if (Current().Type == TokenType.COLON) { Advance(); if (Current().Type == TokenType.NUMBER) Advance(); }
                                while (Match(TokenType.LBRACKET)) {
                                    int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}
                                    Expect(TokenType.RBRACKET);
                                }
                                int ims = GetMemberSize(innerTN, false, null);
                                if (ims > maxSize) maxSize = ims;
                            } while (Match(TokenType.COMMA));
                            Expect(TokenType.SEMICOLON);
                        }
                        Expect(TokenType.RBRACE);
                        while (Current().Type == TokenType.STAR) Advance();
                        if (Current().Type == TokenType.IDENTIFIER)
                        {
                            string anonUVarName = Advance().Value.ToString();
                            // Register anonymous union top-level variable
                            program.Variables.Add(new VariableDecl(anonUVarName, "union", null));
                        }
                        Expect(TokenType.SEMICOLON);
                        continue;
                    }

                    do {
                        while (Current().Type == TokenType.STAR) Advance();
                        string memberName;
                        if (Current().Type == TokenType.COLON) { memberName = ""; Advance(); if (Current().Type == TokenType.NUMBER) Advance(); }
                        else memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        if (Current().Type == TokenType.COLON) { Advance(); if (Current().Type == TokenType.NUMBER) Advance(); }
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        if (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                arraySize = Convert.ToInt32(Current().Value);
                                dimensions.Add(arraySize);
                                Advance();
                            }
                            else
                            {
                                // 跳过复杂数组大小表达式
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LBRACKET) depth++;
                                    else if (Current().Type == TokenType.RBRACKET) { depth--; if (depth == 0) break; }
                                    Advance();
                                }
                            }
                            Expect(TokenType.RBRACKET);
                        }

                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = 0; // union所有成员偏移都是0
                        member.Dimensions = dimensions;
                        unionDecl.Members.Add(member);
                        int memberSize = GetMemberSize(typeName, isArray, arraySize);
                        if (memberSize > maxSize) maxSize = memberSize;
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);
                unionDecl.Size = maxSize;
                return true;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                // union定义：union GlobalUnion { int i; float f; };
                string name = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);

                var unionDecl = new UnionDecl(name);
                int maxSize = 0;

                while (Current().Type != TokenType.RBRACE)
                {
                    // 联合体成员
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();

                    // 如果是 struct/union 成员类型，消费标签名
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            typeName += " " + Advance().Value.ToString();
                    }

                    // 处理类型修饰符
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 消费 typedef 类型名 (如 sqlite3_module)
                    if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        typeName += " " + Advance().Value.ToString();

                    // 匿名 struct/union: struct { ... } name;
                    if (Current().Type == TokenType.LBRACE && (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION))
                    {
                        Advance(); // {
                        while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                        {
                            Token innerType = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                            string innerTN = innerType.Value.ToString();
                            if (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION)
                            { if (Current().Type == TokenType.IDENTIFIER) innerTN += " " + Advance().Value.ToString(); }
                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.VOID || Current().Type == TokenType.STRUCT || Current().Type == TokenType.UNION || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                                innerTN += " " + Advance().Value.ToString();
                            while (Current().Type == TokenType.STAR) Advance();
                            // 嵌套匿名 struct/union
                            if (Current().Type == TokenType.LBRACE && (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION))
                            {
                                Advance(); // {
                                int nd = 1; while (nd > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LBRACE) nd++; else if (Current().Type == TokenType.RBRACE) { nd--; if (nd == 0) break; } Advance(); }
                                Expect(TokenType.RBRACE);
                                while (Current().Type == TokenType.STAR) Advance();
                                if (Current().Type == TokenType.IDENTIFIER) Advance();
                                Expect(TokenType.SEMICOLON);
                                continue;
                            }
                            do {
                                string imn = Current().Type == TokenType.COLON ? "" : Expect(TokenType.IDENTIFIER).Value.ToString();
                                if (Current().Type == TokenType.COLON) { Advance(); if (Current().Type == TokenType.NUMBER) Advance(); }
                                while (Match(TokenType.LBRACKET)) {
                                    int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}
                                    Expect(TokenType.RBRACKET);
                                }
                                int ims = GetMemberSize(innerTN, false, null);
                                if (ims > maxSize) maxSize = ims;
                            } while (Match(TokenType.COMMA));
                            Expect(TokenType.SEMICOLON);
                        }
                        Expect(TokenType.RBRACE);
                        while (Current().Type == TokenType.STAR) Advance();
                        if (Current().Type == TokenType.IDENTIFIER)
                        {
                            string anonUVarName = Advance().Value.ToString();
                            // Register anonymous union top-level variable
                            program.Variables.Add(new VariableDecl(anonUVarName, "union", null));
                        }
                        Expect(TokenType.SEMICOLON);
                        continue;
                    }

                    do {
                        while (Current().Type == TokenType.STAR) Advance();
                        string memberName;
                        if (Current().Type == TokenType.COLON) { memberName = ""; Advance(); if (Current().Type == TokenType.NUMBER) Advance(); }
                        else memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        if (Current().Type == TokenType.COLON) { Advance(); if (Current().Type == TokenType.NUMBER) Advance(); }
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        if (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET)
                            {
                                arraySize = Convert.ToInt32(Current().Value);
                                dimensions.Add(arraySize);
                                Advance();
                            }
                            else
                            {
                                // 跳过复杂数组大小表达式
                                int depth = 1;
                                while (depth > 0 && Current().Type != TokenType.EOF)
                                {
                                    if (Current().Type == TokenType.LBRACKET) depth++;
                                    else if (Current().Type == TokenType.RBRACKET) { depth--; if (depth == 0) break; }
                                    Advance();
                                }
                            }
                            Expect(TokenType.RBRACKET);
                        }

                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = 0; // union所有成员偏移都是0
                        member.Dimensions = dimensions;
                        unionDecl.Members.Add(member);
                        int memberSize = GetMemberSize(typeName, isArray, arraySize);
                        if (memberSize > maxSize) maxSize = memberSize;
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);
                unionDecl.Size = maxSize;
                program.Unions[name] = unionDecl;
                return true;
            }
            else
            {
                // union变量声明：union GlobalUnion g1;
                // 解析union类型名称
                string unionTypeName = "union";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    unionTypeName += " " + Advance().Value.ToString();
                }

                // 解析变量名
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    string       varName = Advance().Value.ToString();
                    VariableDecl varDecl = ParseVariableDecl(unionTypeName, varName);
                    program.Variables.Add(varDecl);
                    Expect(TokenType.SEMICOLON);
                }
                else
                {
                    Error("期望变量名");
                }
                return true;
            }
        }

        private Token Expect(params TokenType[] tokenTypes)
        {
            foreach (var tokenType in tokenTypes)
            {
                if (Current().Type == tokenType)
                {
                    return Advance();
                }
            }
            string expectedTypes = string.Join(", ", tokenTypes);
            throw Error($"期望 {expectedTypes}，但得到 {Current().Type.ToString()}");
        }

        private Function ParseFunction(string returnType, string name, bool isInterrupt = false, TokenType conventionToken = TokenType.EOF, bool isStatic = false)
        {
            // 参数列表
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
                        Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T ||
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

                        string typeName = preCheckedType ?? "";

                        // 处理 const/volatile 修饰符
                        while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        {
                            typeName += Current().Value.ToString() + " ";
                            Advance();
                        }

                        // 函数指针参数: 必须在 Advance 之前检查
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); Match(TokenType.STAR);
                            typeName += "(*)";
                            string pn = Current().Type == TokenType.IDENTIFIER ? Advance().Value.ToString() : "";
                            // 函数指针数组: int(*fa[2])()
                            while (Match(TokenType.LBRACKET)) {
                                while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                                Expect(TokenType.RBRACKET);
                                typeName += "[]";
                            }
                            Expect(TokenType.RPAREN);
                            if (Match(TokenType.LPAREN))
                            { int d = 1; while (d > 0 && Current().Type != TokenType.EOF)
                              { if (Current().Type == TokenType.LPAREN) d++; else if (Current().Type == TokenType.RPAREN) { d--; if (d == 0) { Advance(); break; } } Advance(); } }
                            while (Match(TokenType.LBRACKET)) {
                                while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                                Expect(TokenType.RBRACKET);
                            }
                            parameters.Add(new Parameter(pn, typeName));
                            if (!Match(TokenType.COMMA)) break;
                            continue;
                        }

                        // 处理基本类型
                        Token typeToken = Advance();
                        typeName += typeToken.Value.ToString();

                        // For struct/union param types, consume the tag name
                        if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                        {
                            if (Current().Type == TokenType.IDENTIFIER)
                                typeName += " " + Advance().Value.ToString();
                        }

                        // 处理其他类型修饰符 (含 const/volatile 后置 + typedef 类型名)
                        while (Current().Type == TokenType.LONG || Current().Type == TokenType.SHORT ||
                               Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED ||
                               Current().Type == TokenType.CHAR || Current().Type == TokenType.INT ||
                               Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE ||
                               Current().Type == TokenType.VOID || Current().Type == TokenType.BOOL ||
                               Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        {
                            typeName += " " + Advance().Value.ToString();
                        }
                        // 处理 typedef 类型名 (如 volatile u32 → "volatile u32")
                        while (Current().Type == TokenType.IDENTIFIER &&
                               program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        {
                            typeName += " " + Advance().Value.ToString();
                        }

                        // 检查是否为指针类型
                        while (Match(TokenType.STAR))
                        {
                            typeName += "*";
                            while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                                typeName += " " + Advance().Value.ToString();
                        }

                        // 处理参数名前的数组括号: int *arr[] → int **arr
                        while (Match(TokenType.LBRACKET))
                        {
                            typeName += "*";
                            if (!Match(TokenType.RBRACKET))
                            {
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
                        // 函数指针参数: int (*name)(params) 或 void *(*name)(params)
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); Match(TokenType.STAR);
                            typeName += "(*)";
                            string pn = Current().Type == TokenType.IDENTIFIER ? Advance().Value.ToString() : "";
                            // 函数指针数组: int(*fa[2])()
                            while (Match(TokenType.LBRACKET)) {
                                while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                                Expect(TokenType.RBRACKET);
                                typeName += "[]";
                            }
                            Expect(TokenType.RPAREN);
                            if (Match(TokenType.LPAREN))
                            { int d = 1; while (d > 0 && Current().Type != TokenType.EOF)
                              { if (Current().Type == TokenType.LPAREN) d++; else if (Current().Type == TokenType.RPAREN) { d--; if (d == 0) { Advance(); break; } } Advance(); } }
                            while (Match(TokenType.LBRACKET)) {
                                while (Current().Type != TokenType.RBRACKET && Current().Type != TokenType.EOF) Advance();
                                Expect(TokenType.RBRACKET);
                            }
                            parameters.Add(new Parameter(pn, typeName));
                            if (!Match(TokenType.COMMA)) break;
                            continue;
                        }
                        string paramName = Current().Type == TokenType.IDENTIFIER ?
                            Advance().Value.ToString() : "";
                        // 处理参数名后的数组括号: type name[N] → type* name
                        while (Match(TokenType.LBRACKET))
                        {
                            typeName += "*";
                            if (!Match(TokenType.RBRACKET))
                            {
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
                        parameters.Add(new Parameter(paramName, typeName));
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
            // else 分支不需要Advance()，因为Match已经调用了Advance()

            // 函数体
            Block body = ParseBlock();

            var convention = conventionToken switch
            {
                TokenType.STDCALL => CallingConvention.Stdcall,
                TokenType.FASTCALL => CallingConvention.Fastcall,
                TokenType.CDECL => CallingConvention.Cdecl,
                _ => CallingConvention.Cdecl
            };
            var fn = new Function(name, returnType, parameters, body, name == "main", false, isVariadic, isInterrupt, convention);
            // `static` —— 内部链接、外部访问不到。这是「未使用了该不该报警」的判据
            // （见 `Function.IsStatic` 的说明）。调用方从存储类说明符里带过来。
            fn.IsStatic = isStatic;
            return fn;
        }

        private Block ParseBlock()
        {
            // 容错: 如果没有 {，返回空block (可能已被其他处理器消费)
            if (Current().Type != TokenType.LBRACE)
            {
                // 跳过直到 } 或 ;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF)
                    Advance();
                if (Current().Type == TokenType.SEMICOLON || Current().Type == TokenType.RBRACE) Advance();
                return new Block();
            }
            Expect(TokenType.LBRACE);
            var open = Previous();   // 这个 `{` —— 报"块没关上"时锚在它身上（缺口就是它）

            var block = new Block();
            while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
            {
                var stmt = ParseStatement();
                block.Statements.Add(stmt);
            }

            // ⚠ 从前这里是「容错: 块未正常关闭时跳过直到 RBRACE」+ 无条件返回 ——
            //   两层都是空操作：上面的循环**只在** `}` 或 EOF 退出，所以那个
            //   `!= RBRACE && != EOF` 的判据恒假（一段死代码），而 EOF 那一支则
            //   **一声不吭地把缺了 `}` 的块原样返回**。实测 `int main() {` +
            //   任意几行 ⇒ **编译成功、退出码 0** ——
            //   「写了一半就先存一下」在 C 上表现为「静默编出一份残程序」。
            //   （cs/java/go/rust 都会报 `Expected '}'`，只有 C 漏了。）
            if (Current().Type == TokenType.RBRACE) Advance();
            else GccErrorAt("块未闭合（缺少 '}'）", open, ErrorCode.Parser_SyntaxError);
            return block;
        }

    }
}
