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

        private Function ParseFunction(string returnType, string name, bool isInterrupt = false, TokenType conventionToken = TokenType.EOF)
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
            return new Function(name, returnType, parameters, body, name == "main", false, isVariadic, isInterrupt, convention);
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

            var block = new Block();
            while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
            {
                var stmt = ParseStatement();
                block.Statements.Add(stmt);
            }

            // 容错: 块未正常关闭时跳过直到 RBRACE
            if (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
            {
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                    Advance();
            }
            if (Current().Type == TokenType.RBRACE) Advance();
            return block;
        }

    }
}
