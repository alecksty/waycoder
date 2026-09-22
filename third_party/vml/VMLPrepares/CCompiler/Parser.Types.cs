using CompilerBase;
#nullable disable // auto-generated code, null safety not applicable
using System.Collections.Generic;

namespace CCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
{

        private bool ParseEnum()
        {
            Advance(); // enum

            // 检查是枚举定义还是枚举变量声明
            if (Peek(0).Type == TokenType.LBRACE)
            {
                // 枚举定义：enum { RED, GREEN, BLUE }; (匿名枚举)
                Expect(TokenType.LBRACE);

                var enumValues = new Dictionary<string, int>();
                var nextValue  = 0;

                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    string enumMemberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    if (Match(TokenType.ASSIGN))
                    {
                        // 走**常量表达式**折叠（此前只认裸字面量 ⇒ `A = BASE + 1` 静默沿用上一个成员的值）
                        nextValue = EvalEnumMemberValue(ParseConditional(), enumValues);
                    }
                    enumValues[enumMemberName] = nextValue;
                    nextValue++;

                    if (!Match(TokenType.COMMA))
                    {
                        break;
                    }
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);

                // 存储枚举常量到 Program（匿名枚举，合并防止覆盖）
                if (program.EnumConstants.ContainsKey(""))
                {
                    foreach (var kvp in enumValues)
                        program.EnumConstants[""][kvp.Key] = kvp.Value;
                }
                else
                {
                    program.EnumConstants[""] = enumValues;
                }
                return true;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                // 枚举定义：enum Color { RED, GREEN, BLUE };
                string enumName = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);

                Dictionary<string, int> enumValues = new Dictionary<string, int>();
                int                     nextValue  = 0;

                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    string enumMemberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    if (Match(TokenType.ASSIGN))
                    {
                        // 走**常量表达式**折叠（此前只认裸字面量 ⇒ `A = BASE + 1` 静默沿用上一个成员的值）
                        nextValue = EvalEnumMemberValue(ParseConditional(), enumValues);
                    }
                    enumValues[enumMemberName] = nextValue;
                    nextValue++;

                    if (!Match(TokenType.COMMA))
                    {
                        break;
                    }
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);

                // 存储枚举常量到 Program
                program.EnumConstants[enumName] = enumValues;
                return true;
            }
            else
            {
                // 枚举变量声明：enum Color c = GREEN;
                // 解析enum类型名称
                string enumTypeName = "enum";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    enumTypeName += " " + Advance().Value.ToString();
                }

                // 解析变量名
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    string       varName = Advance().Value.ToString();
                    VariableDecl varDecl = ParseVariableDecl(enumTypeName, varName);
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

        // 处理顶层 typedef enum { ... } Alias; 或 typedef enum Name { ... } Alias;
        // 注意：此时还没有消费 "enum" 关键字，当前token是 "enum"
        private void ParseToplevelTypedefEnum()
        {
            Advance(); // enum

            // 匿名枚举定义: typedef enum { ... } Alias;
            if (Peek(0).Type == TokenType.LBRACE)
            {
                Expect(TokenType.LBRACE);
                var enumValues = new Dictionary<string, int>();
                var nextValue  = 0;

                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    if (Match(TokenType.ASSIGN))
                    {
                        // 走**常量表达式**折叠（此前只认裸字面量 ⇒ `A = BASE + 1` 静默沿用上一个成员的值）
                        nextValue = EvalEnumMemberValue(ParseConditional(), enumValues);
                    }
                    enumValues[memberName] = nextValue;
                    nextValue++;

                    if (!Match(TokenType.COMMA))
                        break;
                }
                Expect(TokenType.RBRACE);

                string aliasName = Expect(TokenType.IDENTIFIER).Value.ToString();
                program.TypeDefs[aliasName] = "int";
                program.EnumConstants[aliasName] = enumValues;
                Expect(TokenType.SEMICOLON);
                return;
            }

            // 命名枚举定义: typedef enum Tag { ... } Alias;
            if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                string enumName = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);
                var enumValues = new Dictionary<string, int>();
                var nextValue  = 0;

                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                    if (Match(TokenType.ASSIGN))
                    {
                        // 走**常量表达式**折叠（此前只认裸字面量 ⇒ `A = BASE + 1` 静默沿用上一个成员的值）
                        nextValue = EvalEnumMemberValue(ParseConditional(), enumValues);
                    }
                    enumValues[memberName] = nextValue;
                    nextValue++;

                    if (!Match(TokenType.COMMA))
                        break;
                }
                Expect(TokenType.RBRACE);

                string aliasName = Expect(TokenType.IDENTIFIER).Value.ToString();
                program.TypeDefs[aliasName] = "int";
                program.EnumConstants[enumName] = enumValues;
                Expect(TokenType.SEMICOLON);
                return;
            }

            // typedef enum Tag Alias; (引用已有的枚举类型)
            if (Current().Type == TokenType.IDENTIFIER)
            {
                string enumTag = Advance().Value.ToString();
                string aliasName = Expect(TokenType.IDENTIFIER).Value.ToString();
                program.TypeDefs[aliasName] = "int";
                Expect(TokenType.SEMICOLON);
                return;
            }

            Error("typedef enum 语法错误");
        }

        // 处理顶层 typedef union { ... } Alias; 或 typedef union Name { ... } Alias;
        // 注意：此时还没有消费 "union" 关键字，当前token是 "union"
        private void ParseToplevelTypedefUnion()
        {
            Advance(); // union

            if (Peek(0).Type == TokenType.LBRACE)
            {
                Expect(TokenType.LBRACE);
                var unionDecl = new UnionDecl("");
                int maxSize = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();
                    // Consume struct/union tag name (e.g., "NodeKey" in "struct NodeKey { ... }")
                    if ((typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION) && Current().Type == TokenType.IDENTIFIER)
                        typeName += " " + Advance().Value.ToString();
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 处理匿名/命名 struct/union 内联定义: struct [Name] { ... } member;
                    if (Current().Type == TokenType.LBRACE && (typeName == "struct" || typeName.StartsWith("struct ") || typeName == "union" || typeName.StartsWith("union ")))
                    {
                        Advance(); // {
                        int anonDepth = 1;
                        while (anonDepth > 0 && Current().Type != TokenType.EOF)
                        {
                            if (Current().Type == TokenType.LBRACE) anonDepth++;
                            else if (Current().Type == TokenType.RBRACE) { anonDepth--; if (anonDepth == 0) break; }
                            Advance();
                        }
                        Expect(TokenType.RBRACE);
                        typeName = "int"; // fallback type
                    }
                    // 处理指针类型 (struct/union 成员)
                    while (Match(TokenType.STAR))
                        typeName += "*";
                    do {
                        string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER)
                            {
                                int dim = Convert.ToInt32(Current().Value);
                                if (arraySize == null) arraySize = dim;
                                else arraySize *= dim;
                                dimensions.Add(dim);
                                Advance();
                            }
                            else { dimensions.Add(null); }
                            Expect(TokenType.RBRACKET);
                        }
                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = 0;
                        member.Dimensions = dimensions;
                        unionDecl.Members.Add(member);
                        int memberSize = GetMemberSize(typeName, isArray, arraySize);
                        if (memberSize > maxSize) maxSize = memberSize;
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                if (Current().Type == TokenType.IDENTIFIER) {
                    string aliasName = Advance().Value.ToString();
                    unionDecl.Name = aliasName;
                    program.Unions[aliasName] = unionDecl;
                    program.TypeDefs[aliasName] = "union " + aliasName;
                }
                Expect(TokenType.SEMICOLON);
                unionDecl.Size = maxSize;
                return;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                string name = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);
                var unionDecl = new UnionDecl(name);
                int maxSize = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();
                    // Consume struct/union tag name (e.g., "NodeKey" in "struct NodeKey { ... }")
                    if ((typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION) && Current().Type == TokenType.IDENTIFIER)
                        typeName += " " + Advance().Value.ToString();
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 处理匿名/命名 struct/union 内联定义: struct [Name] { ... } member;
                    if (Current().Type == TokenType.LBRACE && (typeName == "struct" || typeName.StartsWith("struct ") || typeName == "union" || typeName.StartsWith("union ")))
                    {
                        Advance(); // {
                        int anonDepth = 1;
                        while (anonDepth > 0 && Current().Type != TokenType.EOF)
                        {
                            if (Current().Type == TokenType.LBRACE) anonDepth++;
                            else if (Current().Type == TokenType.RBRACE) { anonDepth--; if (anonDepth == 0) break; }
                            Advance();
                        }
                        Expect(TokenType.RBRACE);
                        typeName = "int"; // fallback type
                    }
                    // 处理指针类型 (struct/union 成员)
                    while (Match(TokenType.STAR))
                        typeName += "*";
                    do {
                        string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER)
                            {
                                int dim = Convert.ToInt32(Current().Value);
                                if (arraySize == null) arraySize = dim;
                                else arraySize *= dim;
                                dimensions.Add(dim);
                                Advance();
                            }
                            else { dimensions.Add(null); }
                            Expect(TokenType.RBRACKET);
                        }
                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = 0;
                        member.Dimensions = dimensions;
                        unionDecl.Members.Add(member);
                        int memberSize = GetMemberSize(typeName, isArray, arraySize);
                        if (memberSize > maxSize) maxSize = memberSize;
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                if (Current().Type == TokenType.IDENTIFIER) {
                    string aliasName = Advance().Value.ToString();
                    program.TypeDefs[aliasName] = "union " + aliasName;
                }
                Expect(TokenType.SEMICOLON);
                unionDecl.Size = maxSize;
                program.Unions[name] = unionDecl;
                return;
            }
            else
            {
                var unionTypeName = "union";
                if (Current().Type == TokenType.IDENTIFIER)
                    unionTypeName += " " + Advance().Value.ToString();
                while (Match(TokenType.STAR))
                    unionTypeName += "*";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    var aliasName = Advance().Value.ToString();
                    program.TypeDefs[aliasName] = unionTypeName;
                    while (Match(TokenType.COMMA))
                    {
                        aliasName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        program.TypeDefs[aliasName] = unionTypeName;
                    }
                    Expect(TokenType.SEMICOLON);
                }
                else
                {
                    Error("期望别名");
                }
                return;
            }
        }

        // 处理顶层 typedef struct { ... } Alias; 或 typedef struct Name { ... } Alias;
        // 注意：此时还没有消费 "struct" 关键字，当前token是 "struct"

        private void ParseToplevelTypedefStruct()
        {
            Advance(); // struct

            if (Peek(0).Type == TokenType.LBRACE)
            {
                Expect(TokenType.LBRACE);
                var structDecl = new StructDecl("");
                int offset = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 处理后置 const/volatile: sqlite3_mutex_methods const *
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        typeName += " " + Advance().Value.ToString();
                    string effectiveTypeName = typeName;
                    while (Current().Type == TokenType.STAR) { effectiveTypeName += "*"; Advance(); }
                    // 匿名 struct/union 当成员：struct { … } name;  或 C11 匿名成员 struct { … };
                    // 正文按**相对偏移**解析，去向交给 AttachAnonStructMember —— 有名字就登记成
                    // 一张独立类型（外层只加一条 `name` 成员），没名字才把成员提升进外层。
                    if (Current().Type == TokenType.LBRACE && (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION))
                    {
                        bool anonIsUnion = typeToken.Type == TokenType.UNION;
                        var anonBody = ParseAnonStructBody(anonIsUnion);
                        offset += AttachAnonStructMember(anonBody, anonIsUnion, structDecl.Members, offset);
                        continue;
                    }
                    do {
                        while (Current().Type == TokenType.STAR) { effectiveTypeName += "*"; Advance(); }
                        string memberName;
                        bool isBitfield = false; int bitWidth = 0;
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); int fpInnerCount = 0;
                            while (Current().Type == TokenType.STAR)
                            { Advance(); effectiveTypeName += "*"; if (Current().Type == TokenType.LPAREN) { Advance(); fpInnerCount++; continue; } }
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                            int rpToClose = fpInnerCount + 1;
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN) { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN)) { int depth = 1; while (depth > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LPAREN) depth++; else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } } Advance(); } }
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN) { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN)) { int depth = 1; while (depth > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LPAREN) depth++; else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } } Advance(); } }
                        }
                        else if (Current().Type == TokenType.COLON)
                        { memberName = ""; isBitfield = true; Advance(); if (Current().Type == TokenType.NUMBER) { bitWidth = Convert.ToInt32(Current().Value); Advance(); } else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance(); }
                        else
                        { memberName = Expect(TokenType.IDENTIFIER).Value.ToString(); }
                        if (!isBitfield && Current().Type == TokenType.COLON)
                        { isBitfield = true; Advance(); if (Current().Type == TokenType.NUMBER) { bitWidth = Convert.ToInt32(Current().Value); Advance(); } else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance(); }
                        bool isArray = false; int? arraySize = null; List<int?> dimensions = new List<int?>();
                        if (Match(TokenType.LBRACKET))
                        { isArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET) { arraySize = Convert.ToInt32(Current().Value); dimensions.Add(arraySize); Advance(); }
                            else { int d2 = 1; while (d2 > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LBRACKET) d2++; else if (Current().Type == TokenType.RBRACKET) { d2--; if (d2 == 0) break; } Advance(); } }
                            Expect(TokenType.RBRACKET); }
                        var member = new StructMember(memberName, effectiveTypeName, isArray, arraySize);
                        member.Offset = offset; member.Dimensions = dimensions; member.IsBitfield = isBitfield;
                        structDecl.Members.Add(member);
                        offset += GetMemberSize(effectiveTypeName, isArray, arraySize);
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                if (Current().Type == TokenType.IDENTIFIER) {
                    string aliasName = Advance().Value.ToString();
                    structDecl.Name = aliasName;
                    program.Structs[aliasName] = structDecl;
                    program.TypeDefs[aliasName] = "struct " + aliasName;
                }
                Expect(TokenType.SEMICOLON);
                structDecl.Size = offset;
                return;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                string name = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);
                var structDecl = new StructDecl(name);
                int offset = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    // 处理后置 const/volatile: sqlite3_mutex_methods const *
                    while (Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE)
                        typeName += " " + Advance().Value.ToString();
                    string effectiveTypeName = typeName;
                    while (Current().Type == TokenType.STAR) { effectiveTypeName += "*"; Advance(); }
                    // 匿名 struct/union (C11): union { ... } 或 struct { ... }
                    if (Current().Type == TokenType.LBRACE && (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION))
                    {
                        bool anonIsUnion = typeToken.Type == TokenType.UNION;
                        Advance(); // {
                        int anonBase = offset;
                        int anonMaxSize = 0;
                        while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                        {
                            Token innerType = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                            string innerTypeName = innerType.Value.ToString();
                            if (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION)
                            {
                                if (Current().Type == TokenType.IDENTIFIER)
                                    innerTypeName += " " + Advance().Value.ToString();
                            }
                            while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                                innerTypeName += " " + Advance().Value.ToString();
                            if (Current().Type == TokenType.IDENTIFIER && program.TypeDefs.ContainsKey(Current().Value.ToString()))
                                innerTypeName += " " + Advance().Value.ToString();
                            string effInnerType = innerTypeName;
                            while (Current().Type == TokenType.STAR) { effInnerType += "*"; Advance(); }
                            do {
                                while (Current().Type == TokenType.STAR) { effInnerType += "*"; Advance(); }
                                // 嵌套匿名 struct/union
                                if (Current().Type == TokenType.LBRACE && (innerType.Type == TokenType.STRUCT || innerType.Type == TokenType.UNION))
                                { Advance(); int nd=1; while(nd>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACE)nd++;else if(Current().Type==TokenType.RBRACE){nd--;if(nd==0)break;}Advance();}
                                  Expect(TokenType.RBRACE); while(Current().Type==TokenType.STAR) Advance();
                                  if(Current().Type==TokenType.IDENTIFIER) Advance(); Expect(TokenType.SEMICOLON); continue; }
                                string innerMemberName;
                                bool innerBF = false; int bitWidth = 0;
                                if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                                {
                                    Advance(); Match(TokenType.STAR);
                                    effInnerType += "*";
                                    innerMemberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                                    Expect(TokenType.RPAREN);
                                    if (Match(TokenType.LPAREN)) { int d = 1; while (d > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LPAREN) d++; else if (Current().Type == TokenType.RPAREN) { d--; if (d == 0) { Advance(); break; } } Advance(); } }
                                }
                                else if (Current().Type == TokenType.COLON)
                                { innerMemberName = ""; innerBF = true; Advance(); if (Current().Type == TokenType.NUMBER) { bitWidth = Convert.ToInt32(Current().Value); Advance(); } else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance(); }
                                else
                                { innerMemberName = Expect(TokenType.IDENTIFIER).Value.ToString(); }
                                if (!innerBF && Current().Type == TokenType.COLON)
                                { innerBF = true; Advance(); if (Current().Type == TokenType.NUMBER) { bitWidth = Convert.ToInt32(Current().Value); Advance(); } else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance(); }
                                bool isArr = false; int? arrSize = null; List<int?> dims = new List<int?>();
                                if (Match(TokenType.LBRACKET))
                                { isArr = true;
                                    if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET) { int dim = Convert.ToInt32(Current().Value); if (arrSize == null) arrSize = dim; else arrSize *= dim; dims.Add(dim); Advance(); }
                                    else { int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();} }
                                    Expect(TokenType.RBRACKET); }
                                var im = new StructMember(innerMemberName, effInnerType, isArr, arrSize);
                                im.Offset = anonIsUnion ? anonBase : offset;
                                im.Dimensions = dims; im.IsBitfield = innerBF;
                                structDecl.Members.Add(im);
                                int ms = GetMemberSize(effInnerType, isArr, arrSize);
                                if (anonIsUnion) { if (ms > anonMaxSize) anonMaxSize = ms; }
                                else { offset += ms; }
                            } while (Match(TokenType.COMMA));
                            Expect(TokenType.SEMICOLON);
                        }
                        Expect(TokenType.RBRACE);
                        if (anonIsUnion) offset = anonBase + anonMaxSize;
                        // Handle pointers after anonymous struct: } *name; or } **name;
                        while (Current().Type == TokenType.STAR) Advance();
                        if (Current().Type == TokenType.IDENTIFIER) Advance();
                        Expect(TokenType.SEMICOLON);
                        continue;
                    }
                    do {
                        while (Current().Type == TokenType.STAR) { effectiveTypeName += "*"; Advance(); }
                        string memberName;
                        bool isBitfield = false; int bitWidth = 0;
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); int fpInnerCount = 0;
                            while (Current().Type == TokenType.STAR)
                            { Advance(); effectiveTypeName += "*"; if (Current().Type == TokenType.LPAREN) { Advance(); fpInnerCount++; continue; } }
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                            int rpToClose = fpInnerCount + 1;
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN) { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN)) { int depth = 1; while (depth > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LPAREN) depth++; else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } } Advance(); } }
                            while (rpToClose > 0 && Current().Type == TokenType.RPAREN) { Advance(); rpToClose--; }
                            if (Match(TokenType.LPAREN)) { int depth = 1; while (depth > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LPAREN) depth++; else if (Current().Type == TokenType.RPAREN) { depth--; if (depth == 0) { Advance(); break; } } Advance(); } }
                        }
                        else if (Current().Type == TokenType.COLON)
                        { memberName = ""; isBitfield = true; Advance(); if (Current().Type == TokenType.NUMBER) { bitWidth = Convert.ToInt32(Current().Value); Advance(); } else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance(); }
                        else
                        { memberName = Expect(TokenType.IDENTIFIER).Value.ToString(); }
                        if (!isBitfield && Current().Type == TokenType.COLON)
                        { isBitfield = true; Advance(); if (Current().Type == TokenType.NUMBER) { bitWidth = Convert.ToInt32(Current().Value); Advance(); } else while (Current().Type != TokenType.COMMA && Current().Type != TokenType.SEMICOLON && Current().Type != TokenType.EOF) Advance(); }
                        bool isArray = false; int? arraySize = null; List<int?> dimensions = new List<int?>();
                        if (Match(TokenType.LBRACKET))
                        { isArray = true;
                            if (Current().Type == TokenType.NUMBER && Peek(1).Type == TokenType.RBRACKET) { arraySize = Convert.ToInt32(Current().Value); dimensions.Add(arraySize); Advance(); }
                            else { int d2 = 1; while (d2 > 0 && Current().Type != TokenType.EOF) { if (Current().Type == TokenType.LBRACKET) d2++; else if (Current().Type == TokenType.RBRACKET) { d2--; if (d2 == 0) break; } Advance(); } }
                            Expect(TokenType.RBRACKET); }
                        var member = new StructMember(memberName, effectiveTypeName, isArray, arraySize);
                        member.Offset = offset; member.Dimensions = dimensions; member.IsBitfield = isBitfield;
                        structDecl.Members.Add(member);
                        offset += GetMemberSize(effectiveTypeName, isArray, arraySize);
                    } while (Match(TokenType.COMMA));
                    Expect(TokenType.SEMICOLON);
                }
                Expect(TokenType.RBRACE);
                if (Current().Type == TokenType.IDENTIFIER) {
                    string aliasName = Advance().Value.ToString();
                    program.TypeDefs[aliasName] = "struct " + aliasName;
                }
                Expect(TokenType.SEMICOLON);
                structDecl.Size = offset;
                program.Structs[name] = structDecl;
                return;
            }
            else
            {
                var structTypeName = "struct";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    structTypeName += " " + Advance().Value.ToString();
                }
                while (Match(TokenType.STAR))
                {
                    structTypeName += "*";
                }
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    var aliasName = Advance().Value.ToString();
                    program.TypeDefs[aliasName] = structTypeName;
                    while (Match(TokenType.COMMA))
                    {
                        aliasName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        program.TypeDefs[aliasName] = structTypeName;
                    }
                    Expect(TokenType.SEMICOLON);
                }
                else
                {
                    Error("期望别名");
                }
                return;
            }
        }

        private VariableDecl ParseStructWithDecl()
        {
            Advance(); // struct

            if (Peek(0).Type == TokenType.LBRACE)
            {
                Expect(TokenType.LBRACE);
                var structDecl = new StructDecl("");
                int offset = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName = typeToken.Value.ToString();
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    int pointerLevel = 0;
                    while (Current().Type == TokenType.STAR) { pointerLevel++; Advance(); }
                    do {
                        string memberName;
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); // (
                            Match(TokenType.STAR); // *
                            pointerLevel++;
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                            Expect(TokenType.RPAREN);
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
                        }
                        else
                        {
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        }
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER)
                            {
                                int dim = Convert.ToInt32(Current().Value);
                                if (arraySize == null) arraySize = dim;
                                else arraySize *= dim;
                                dimensions.Add(dim);
                                Advance();
                            }
                            else { dimensions.Add(null); }
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
                // 处理 } 后的内容: tag名 或 变量声明(含数组/初始化器)
                while (Current().Type == TokenType.STAR) Advance();
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    string afterBrace = Advance().Value.ToString();
                    // 判断是结构体标签名还是变量名:
                    // 变量名后跟 [、= 或 ;（跳过数组/初始化器）
                    if (Current().Type == TokenType.LBRACKET || Current().Type == TokenType.ASSIGN)
                    {
                        while (Match(TokenType.LBRACKET))
                        {
                            int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}
                            Expect(TokenType.RBRACKET);
                        }
                        if (Match(TokenType.ASSIGN))
                        {
                            if (Match(TokenType.LBRACE))
                            {
                                int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACE)d2++;else if(Current().Type==TokenType.RBRACE){d2--;if(d2==0)break;}Advance();}
                                Expect(TokenType.RBRACE);
                            }
                            else { while(Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF)Advance(); }
                        }
                    }
                    else
                    {
                        // 结构体标签名: } Name;
                        structDecl.Name = afterBrace;
                        program.Structs[afterBrace] = structDecl;
                    }
                }
                Expect(TokenType.SEMICOLON);
                structDecl.Size = offset;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                string name = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);
                var structDecl = new StructDecl(name);
                int offset = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName = typeToken.Value.ToString();
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    int pointerLevel = 0;
                    while (Current().Type == TokenType.STAR) { pointerLevel++; Advance(); }
                    do {
                        string memberName;
                        if (Current().Type == TokenType.LPAREN && Peek(1).Type == TokenType.STAR)
                        {
                            Advance(); // (
                            Match(TokenType.STAR); // *
                            pointerLevel++;
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                            Expect(TokenType.RPAREN);
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
                        }
                        else
                        {
                            memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        }
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER)
                            {
                                int dim = Convert.ToInt32(Current().Value);
                                if (arraySize == null) arraySize = dim;
                                else arraySize *= dim;
                                dimensions.Add(dim);
                                Advance();
                            }
                            else { dimensions.Add(null); }
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
                // 处理 } 后的变量声明或额外标签名
                while (Current().Type == TokenType.STAR) Advance();
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    string afterBrace = Advance().Value.ToString();
                    // 变量名后跟 [、= 或 ;（作为结束）→ 跳过数组/初始化器
                    if (Current().Type == TokenType.LBRACKET || Current().Type == TokenType.ASSIGN)
                    {
                        while (Match(TokenType.LBRACKET))
                        {
                            int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACKET)d2++;else if(Current().Type==TokenType.RBRACKET){d2--;if(d2==0)break;}Advance();}
                            Expect(TokenType.RBRACKET);
                        }
                        if (Match(TokenType.ASSIGN))
                        {
                            if (Match(TokenType.LBRACE))
                            {
                                int d2=1; while(d2>0&&Current().Type!=TokenType.EOF){if(Current().Type==TokenType.LBRACE)d2++;else if(Current().Type==TokenType.RBRACE){d2--;if(d2==0)break;}Advance();}
                                Expect(TokenType.RBRACE);
                            }
                            else { while(Current().Type!=TokenType.SEMICOLON&&Current().Type!=TokenType.EOF)Advance(); }
                        }
                    }
                    else
                    {
                        structDecl.Name = afterBrace;
                        program.Structs[afterBrace] = structDecl;
                    }
                }
                Expect(TokenType.SEMICOLON);
                structDecl.Size = offset;
                program.Structs[name] = structDecl;
                return null;
            }
            else
            {
                string structTypeName = "struct";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    structTypeName += " " + Advance().Value.ToString();
                }
                while (Match(TokenType.STAR))
                {
                    structTypeName += "*";
                }
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    string varName = Advance().Value.ToString();
                    var varDecl = ParseVariableDecl(structTypeName, varName);
                    program.Variables.Add(varDecl);
                    while (Match(TokenType.COMMA))
                    {
                        varName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        varDecl = ParseVariableDecl(structTypeName, varName);
                        program.Variables.Add(varDecl);
                    }
                    Expect(TokenType.SEMICOLON);
                    return varDecl;
                }
                else
                {
                    throw Error("期望变量名");
                }
            }
            throw Error("结构体声明异常");
        }

        private VariableDecl ParseUnionWithDecl()
        {
            Advance(); // union

            if (Peek(0).Type == TokenType.LBRACE)
            {
                Expect(TokenType.LBRACE);
                var unionDecl = new UnionDecl("");
                int maxSize = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    do {
                        string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER)
                            {
                                int dim = Convert.ToInt32(Current().Value);
                                if (arraySize == null) arraySize = dim;
                                else arraySize *= dim;
                                dimensions.Add(dim);
                                Advance();
                            }
                            else
                            {
                                dimensions.Add(null);
                            }
                            Expect(TokenType.RBRACKET);
                        }
                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = 0;
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
                return null;
            }
            else if (Current().Type == TokenType.IDENTIFIER && Peek(1).Type == TokenType.LBRACE)
            {
                string name = Expect(TokenType.IDENTIFIER).Value.ToString();
                Expect(TokenType.LBRACE);
                var unionDecl = new UnionDecl(name);
                int maxSize = 0;
                while (Current().Type != TokenType.RBRACE && Current().Type != TokenType.EOF)
                {
                    Token  typeToken = Expect(TokenType.INT, TokenType.CHAR, TokenType.FLOAT, TokenType.DOUBLE, TokenType.SHORT, TokenType.VOID, TokenType.LONG, TokenType.SIGNED, TokenType.UNSIGNED, TokenType.CONST, TokenType.VOLATILE, TokenType.VOID, TokenType.IDENTIFIER, TokenType.STRUCT, TokenType.UNION, TokenType.INT8, TokenType.INT16, TokenType.INT32, TokenType.INT64, TokenType.UINT8, TokenType.UINT16, TokenType.UINT32, TokenType.UINT64, TokenType.INTPTR_T, TokenType.UINTPTR_T, TokenType.WCHAR_T, TokenType.CHAR32_T, TokenType.SIZE_T, TokenType.SSIZE_T, TokenType.PTRDIFF_T, TokenType.BOOL);
                    string typeName  = typeToken.Value.ToString();
                    if (typeToken.Type == TokenType.STRUCT || typeToken.Type == TokenType.UNION)
                    {
                        if (Current().Type == TokenType.IDENTIFIER)
                            { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    while (Current().Type == TokenType.LONG || Current().Type == TokenType.SIGNED || Current().Type == TokenType.UNSIGNED || Current().Type == TokenType.CONST || Current().Type == TokenType.VOLATILE || Current().Type == TokenType.VOID || Current().Type == TokenType.CHAR || Current().Type == TokenType.INT || Current().Type == TokenType.FLOAT || Current().Type == TokenType.DOUBLE || Current().Type == TokenType.SHORT || Current().Type == TokenType.INT8 || Current().Type == TokenType.INT16 || Current().Type == TokenType.INT32 || Current().Type == TokenType.INT64 || Current().Type == TokenType.UINT8 || Current().Type == TokenType.UINT16 || Current().Type == TokenType.UINT32 || Current().Type == TokenType.UINT64 || Current().Type == TokenType.INTPTR_T || Current().Type == TokenType.UINTPTR_T || Current().Type == TokenType.WCHAR_T || Current().Type == TokenType.CHAR32_T || Current().Type == TokenType.SIZE_T || Current().Type == TokenType.SSIZE_T || Current().Type == TokenType.PTRDIFF_T || Current().Type == TokenType.BOOL)
                    {
                        { string m = Advance().Value.ToString(); typeName += " " + m; if ((m == "struct" || m == "union") && Current().Type == TokenType.IDENTIFIER) typeName += " " + Advance().Value.ToString(); }
                    }
                    do {
                        string memberName = Expect(TokenType.IDENTIFIER).Value.ToString();
                        bool isArray = false;
                        int? arraySize = null;
                        List<int?> dimensions = new List<int?>();
                        while (Match(TokenType.LBRACKET))
                        {
                            isArray = true;
                            if (Current().Type == TokenType.NUMBER)
                            {
                                int dim = Convert.ToInt32(Current().Value);
                                if (arraySize == null) arraySize = dim;
                                else arraySize *= dim;
                                dimensions.Add(dim);
                                Advance();
                            }
                            else
                            {
                                dimensions.Add(null);
                            }
                            Expect(TokenType.RBRACKET);
                        }
                        var member = new StructMember(memberName, typeName, isArray, arraySize);
                        member.Offset = 0;
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
                return null;
            }
            else
            {
                string unionTypeName = "union";
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    unionTypeName += " " + Advance().Value.ToString();
                }
                while (Match(TokenType.STAR))
                {
                    unionTypeName += "*";
                }
                if (Current().Type == TokenType.IDENTIFIER)
                {
                    string varName = Advance().Value.ToString();
                    VariableDecl varDecl = ParseVariableDecl(unionTypeName, varName);
                    program.Variables.Add(varDecl);
                    Expect(TokenType.SEMICOLON);
                    return varDecl;
                }
                else
                {
                    Error("期望变量名");
                }
            }
            throw Error("联合体声明异常");
        }
    }
}  // namespace
