using System.Collections.Generic;
using CompilerBase;

namespace ObjCCompiler;

public class Parser : ParserBase<Token, TokenType>
{
    protected override TokenType GetTokenType(Token token) => token.Type;

    protected override Token Expect(TokenType t, string msg) => base.Expect(t, $"ObjC parse error: {msg} (got {Cur.Type} '{Cur.Value}' at line {Cur.Line})");

    /// <summary>typedef 类型别名映射 (aliasName → actualType)</summary>
    private Dictionary<string, string> _typedefs = new();
    /// <summary>Enum constant values (enumMemberName → integer value)</summary>
    private Dictionary<string, int> _enumValues = new();
    /// <summary>Struct definitions (structName → StructDef with field offsets)</summary>
    internal Dictionary<string, StructDef> _structDefs = new();

    public Parser(List<Token> tokens) : base(tokens) { }

    public ProgramNode Parse()
    {
        var prog = new ProgramNode();
        while (!IsAtEnd)
        {
            SkipNewlines();
            if (Check(TokenType.EOF)) break;
            prog.Statements.Add(ParseTopLevel());
        }
        return prog;
    }

    private ASTNode ParseTopLevel()
    {
        if (Check(TokenType.Hash)) { SkipPreprocessor(); return ParseTopLevel(); }

        // ObjC @interface
        if (Check(TokenType.Interface)) return ParseInterface();
        // ObjC @implementation
        if (Check(TokenType.Implementation)) return ParseImplementation();
        // ObjC @class forward declaration
        if (Check(TokenType.Class)) { Advance(); Expect(TokenType.Identifier, "expected class name"); Match(TokenType.Semicolon); return ParseTopLevel(); }
        // ObjC @protocol
        if (Check(TokenType.Protocol)) return ParseProtocol();

        // C function/typedef/variable declaration
        if (IsTypeName(Cur.Type) || Cur.Type == TokenType.Void || Cur.Type == TokenType.Typedef)
        {
            return ParseFunctionOrDecl();
        }

        return ParseStatement();
    }

    private void SkipPreprocessor()
    {
        while (!Check(TokenType.Newline) && !Check(TokenType.EOF)) Advance();
        if (Check(TokenType.Newline)) Advance();
    }

    private bool IsTypeName(TokenType t) => t is TokenType.Int or TokenType.Char or TokenType.Float or TokenType.Double or TokenType.Short or TokenType.Long or TokenType.Void or TokenType.Struct or TokenType.Union or TokenType.Enum or TokenType.IdType or TokenType.Static or TokenType.Const or TokenType.Extern or TokenType.Native or TokenType.Unsigned or TokenType.Signed
        || (t == TokenType.Identifier && _typedefs.ContainsKey(Cur.Value));

    private ASTNode ParseFunctionOrDecl()
    {
        // enum 声明: enum Name { member1, member2 = val, ... };
        if (Check(TokenType.Enum))
        {
            int enumLine = Cur.Line, enumCol = Cur.Column;
            ParseTypeName(); // parses body and stores constants in _enumValues
            // Skip optional variable declarations after enum body
            while (Check(TokenType.Identifier) || Check(TokenType.Star))
                Advance();
            Match(TokenType.Semicolon);
            return new BlockNode([], enumLine, enumCol); // Constants registered in _enumValues
        }

        // struct 定义: struct Name { fields } [vars];
        if (Check(TokenType.Struct))
        {
            string stType = ParseTypeName();
            while (Check(TokenType.Identifier) || Check(TokenType.Star))
                Advance();
            Match(TokenType.Semicolon);
            return new BlockNode([], Cur.Line, Cur.Column);
        }

        // union 定义: union Name { fields };
        if (Check(TokenType.Union))
        {
            string utType = ParseTypeName();
            while (Check(TokenType.Identifier) || Check(TokenType.Star))
                Advance();
            Match(TokenType.Semicolon);
            return new BlockNode([], Cur.Line, Cur.Column);
        }

        // typedef 声明: typedef existingType [*] aliasName;
        if (Check(TokenType.Typedef))
        {
            Advance(); // skip typedef
            string actualType = ParseTypeName();
            while (Match(TokenType.Star)) actualType += "*";
            string aliasName = Expect(TokenType.Identifier, "expected typedef alias name").Value;
            _typedefs[aliasName] = actualType;
            Match(TokenType.Semicolon);
            return new VarDeclNode("typedef", aliasName, null, Cur.Line, Cur.Column);
        }

        string returnType = ParseTypeName();
        int l = Cur.Line, c = Cur.Column;
        // 指针类型: type *name
        while (Match(TokenType.Star)) returnType += "*";
        string name = Expect(TokenType.Identifier, "expected function name").Value;

        // 全局变量声明: int x = 0; 或 int x, y = 0;
        if (Check(TokenType.Assign) || Check(TokenType.Semicolon) || Check(TokenType.Comma) || Check(TokenType.LBracket))
        {
            ASTNode? init = null;
            int arraySize = 0;
            // 数组: int arr[10] = {1,2,3};
            if (Match(TokenType.LBracket))
            {
                var sizeNode = ParseExpression();
                if (sizeNode is LiteralNode lit)
                {
                    if (lit.Value is int s) arraySize = s;
                    else if (lit.Value is float f) arraySize = (int)f;
                    else if (lit.Value is double d) arraySize = (int)d;
                    else if (lit.Value is string str && int.TryParse(str, out int ps)) arraySize = ps;
                    else if (lit.Value is long lv) arraySize = (int)lv;
                }
                Expect(TokenType.RBracket, "expected ]");
            }
            if (Match(TokenType.Assign))
                init = ParseExpression();
            // 跳过逗号分隔的多变量声明: int a = 1, b = 2, c;
            while (Match(TokenType.Comma))
            {
                Expect(TokenType.Identifier, "expected variable name");
                if (Match(TokenType.Assign))
                    ParseExpression();
            }
            Match(TokenType.Semicolon);
            return new VarDeclNode(returnType, name, init, l, c, arraySize);
        }

        Expect(TokenType.LParen, "expected (");
        var parms = new List<(string type, string name)>();
        // () 或 (void) 空参数列表
        if (Check(TokenType.RParen))
        {
            Advance(); // skip )
        }
        else if (Match(TokenType.Void) && Match(TokenType.RParen))
        {
            // (void) — both tokens consumed by Match
        }
        else
        {
            do
            {
                string pt = ParseTypeName();
                while (Match(TokenType.Star)) pt += "*";
                // C 允许省略参数名: int f(int, char)
                string pn = Check(TokenType.Identifier) ? Advance().Value : "_";
                parms.Add((pt, pn));
            } while (Match(TokenType.Comma));
            Expect(TokenType.RParen, "expected )");
        }

        if (Match(TokenType.Semicolon)) // forward declaration
            return new VarDeclNode("void", "_forward_" + name, null, l, c);

        var body = ParseBlock();
        return new FuncDeclNode(name, returnType, parms, body, l, c);
    }

    private string ParseTypeName()
    {
        // 跳过存储类修饰符: static, const, extern, native, volatile (typedef handled at top level)
        while (Check(TokenType.Static) || Check(TokenType.Const) || Check(TokenType.Extern) || Check(TokenType.Native)) Advance();
        if (Check(TokenType.Long)) { Advance(); if (Match(TokenType.Long)) return "long long"; return "long"; }
        if (Check(TokenType.Short)) { Advance(); return "short"; }
        if (Check(TokenType.Unsigned)) { Advance(); return "unsigned " + ParseTypeName(); }
        if (Check(TokenType.Signed)) { Advance(); return ParseTypeName(); }
        if (Check(TokenType.Struct))
        {
            Advance();
            // 匿名结构体: struct { int x; int y; } — 跳过主体
            if (Check(TokenType.LBrace))
            {
                int depth = 1; Advance();
                while (depth > 0 && !Check(TokenType.EOF))
                {
                    if (Check(TokenType.LBrace)) depth++;
                    else if (Check(TokenType.RBrace)) depth--;
                    Advance();
                }
                return "struct _anon";
            }
            string s = Expect(TokenType.Identifier, "expected struct name").Value;
            // struct Name { fields } — parse body and track field layout
            if (Match(TokenType.LBrace))
            {
                var fields = new List<StructFieldDef>();
                int currentOffset = 0;
                while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
                {
                    SkipNewlines();
                    if (Check(TokenType.RBrace)) break;
                    string fieldType = ParseTypeName();
                    // 指针字段: int*
                    while (Match(TokenType.Star)) fieldType += "*";
                    string fieldName = Expect(TokenType.Identifier, "expected field name").Value;
                    // 数组字段: int arr[10]
                    int arraySize = 1;
                    if (Match(TokenType.LBracket))
                    {
                        var sizeNode = ParseExpression();
                        if (sizeNode is LiteralNode lit && lit.Value is int iv) arraySize = iv;
                        Expect(TokenType.RBracket, "expected ]");
                    }
                    Match(TokenType.Semicolon);
                    int fieldSize = (fieldType == "long" || fieldType == "double" || fieldType == "long long") ? 8 :
                                    (fieldType.Contains("*") ? 4 : 4); // pointer = 4 bytes
                    fieldSize *= arraySize;
                    fields.Add(new StructFieldDef(fieldName, fieldType, currentOffset, fieldSize));
                    currentOffset += fieldSize;
                }
                Expect(TokenType.RBrace, "expected }");
                _structDefs[s] = new StructDef(s, fields);
            }
            return "struct " + s;
        }
        if (Check(TokenType.Union))
        {
            Advance();
            // 匿名联合体: union { int x; float y; } — 跳过主体
            if (Check(TokenType.LBrace))
            {
                int depth = 1; Advance();
                while (depth > 0 && !Check(TokenType.EOF)) { if (Check(TokenType.LBrace)) depth++; else if (Check(TokenType.RBrace)) depth--; Advance(); }
                return "union _anon";
            }
            string s = Expect(TokenType.Identifier, "expected union name").Value;
            // Parse union body if present: all fields share offset 0
            if (Match(TokenType.LBrace))
            {
                var fields = new List<StructFieldDef>();
                while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
                {
                    SkipNewlines();
                    if (Check(TokenType.RBrace)) break;
                    string fieldType = ParseTypeName();
                    while (Match(TokenType.Star)) fieldType += "*";
                    string fieldName = Expect(TokenType.Identifier, "expected field name").Value;
                    Match(TokenType.Semicolon);
                    int fieldSize = (fieldType == "long" || fieldType == "double" || fieldType == "long long") ? 8 :
                                    (fieldType.Contains("*") ? 4 : 4);
                    fields.Add(new StructFieldDef(fieldName, fieldType, 0, fieldSize)); // union: all at offset 0
                }
                Expect(TokenType.RBrace, "expected }");
                _structDefs[s] = new StructDef(s, fields);
            }
            return "union " + s;
        }
        if (Check(TokenType.Enum)) {
            Advance();
            string s = Expect(TokenType.Identifier, "expected enum name").Value;
            // Parse enum body if present: enum Name { A, B = 5 }
            if (Match(TokenType.LBrace))
            {
                int autoVal = 0;
                while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
                {
                    string memberName = Expect(TokenType.Identifier, "expected enum member").Value;
                    if (Match(TokenType.Assign))
                    {
                        var valNode = ParseExpression();
                        if (valNode is LiteralNode lit)
                            autoVal = lit.Value is int iv ? iv : (lit.Value is long lv ? (int)lv : autoVal);
                    }
                    _enumValues[memberName] = autoVal;
                    autoVal++;
                    if (!Match(TokenType.Comma)) break;
                }
                Expect(TokenType.RBrace, "expected }");
            }
            return "enum " + s;
        }
        if (Check(TokenType.IdType)) {
            Advance();
            // id<Proto1, Proto2> — protocol-qualified id
            if (Match(TokenType.Lt))
            {
                var protos = new List<string>();
                do {
                    protos.Add(Expect(TokenType.Identifier, "expected protocol name").Value);
                } while (Match(TokenType.Comma));
                Expect(TokenType.Gt, "expected >");
                return "id<" + string.Join(",", protos) + ">";
            }
            return "id";
        }
        // 检查是否是 typedef 别名
        if (Check(TokenType.Identifier) && _typedefs.ContainsKey(Cur.Value))
            return _typedefs[Advance().Value];
        return Advance().Value;
    }

    private List<ASTNode> ParseBlock()
    {
        Expect(TokenType.LBrace, "expected {");
        var body = new List<ASTNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.RBrace) || Check(TokenType.EOF)) break;
            body.Add(ParseStatement());
        }
        Expect(TokenType.RBrace, "expected }");
        return body;
    }

    // ObjC @interface Name : SuperClass { ivars } methods @end
    private ASTNode ParseInterface()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // @interface
        string name = Expect(TokenType.Identifier, "expected interface name").Value;
        string superClass = "NSObject";
        string? categoryName = null;

        // Category syntax: @interface ClassName (CategoryName)  or  @interface ClassName () = anonymous extension
        if (Match(TokenType.LParen))
        {
            if (!Check(TokenType.RParen))
                categoryName = Expect(TokenType.Identifier, "expected category name").Value;
            Expect(TokenType.RParen, "expected )");
        }
        else if (Match(TokenType.Colon))
            superClass = Expect(TokenType.Identifier, "expected superclass name").Value;

        var members = new List<ASTNode>();
        // Categories and class extensions may have an optional ivar block
        if (Match(TokenType.LBrace))
        {
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.RBrace)) break;
            // 跳过访问修饰符: @public, @private, @protected
            while (Check(TokenType.AtSign)) { Advance(); SkipNewlines(); }
            if (Check(TokenType.RBrace) || Check(TokenType.EOF)) break;
            // instance variable: type name;
            string ivarType = ParseTypeName();
            // 允许关键字(id, int, char等)作为变量名
            string ivarName;
            if (Check(TokenType.Identifier) || IsTypeName(Cur.Type) || Cur.Type == TokenType.IdType)
                ivarName = Advance().Value;
            else
                ivarName = Expect(TokenType.Identifier, "expected ivar name").Value;
            Match(TokenType.Semicolon);
            members.Add(new VarDeclNode(ivarType, ivarName, null, l, c));
        }
        Expect(TokenType.RBrace, "expected }");
        } // end if LBrace (optional for categories)

        // 解析 @property 声明 (在实例变量之后、方法之前)
        while (Match(TokenType.Property))
        {
            // 解析 @property (attributes) type name;
            var attrs = new List<string>();
            while (Check(TokenType.LParen))
            {
                Advance(); // skip (
                while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
                {
                    if (Check(TokenType.Identifier))
                        attrs.Add(Advance().Value);
                    else
                        Advance(); // skip , or other tokens
                }
                Expect(TokenType.RParen, "expected )");
            }
            string propType = ParseTypeName();
            string propName = Expect(TokenType.Identifier, "expected property name").Value;
            Match(TokenType.Semicolon);
            var propNode = new ObjCPropertyNode(propType, propName, l, c);
            propNode.Attributes.AddRange(attrs);
            members.Add(propNode);
        }

        // parse methods until @end
        while (!Check(TokenType.End) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.End)) break;
            // @property 也可以在方法之间声明
            if (Match(TokenType.Property))
            {
                var attrs = new List<string>();
                while (Check(TokenType.LParen))
                {
                    Advance();
                    while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
                    {
                        if (Check(TokenType.Identifier)) attrs.Add(Advance().Value);
                        else Advance();
                    }
                    Expect(TokenType.RParen, "expected )");
                }
                string propType = ParseTypeName();
                string propName = Expect(TokenType.Identifier, "expected property name").Value;
                Match(TokenType.Semicolon);
                var propNode = new ObjCPropertyNode(propType, propName, l, c);
                propNode.Attributes.AddRange(attrs);
                members.Add(propNode);
                continue;
            }
            if (Check(TokenType.Minus) || Check(TokenType.Plus))
            {
                Advance(); // - or +
                bool isNative = Match(TokenType.Native);
                // parse return type: (type) or just type
                string retType = "void";
                if (Match(TokenType.LParen))
                {
                    retType = ParseTypeName();
                    Expect(TokenType.RParen, "expected )");
                }
                else
                {
                    retType = ParseTypeName();
                }
                // method name with colons: methodName:(type)param1 name2:(type)param2
                var parts = new List<string>();
                var params2 = new List<(string type, string name)>();
                string methodName = "";
                bool first = true;
                while (!Check(TokenType.LBrace) && !Check(TokenType.Semicolon) && !Check(TokenType.EOF))
                {
                    if (first) { methodName += Expect(TokenType.Identifier, "expected method name").Value; first = false; }
                    if (Match(TokenType.Colon))
                    {
                        methodName += ":";
                        Expect(TokenType.LParen, "expected (");
                        string pType = ParseTypeName();
                        Expect(TokenType.RParen, "expected )");
                        string pName = Expect(TokenType.Identifier, "expected param name").Value;
                        params2.Add((pType, pName));
                    }
                }
                List<ASTNode> methodBody;
                if (Match(TokenType.Semicolon) || isNative)
                {
                    methodBody = new List<ASTNode>(); // just a declaration
                }
                else
                {
                    methodBody = ParseBlock();
                }
                members.Add(new ObjCMethodNode(methodName, retType, params2, methodBody, l, c, isNative));
            }
            else break;
        }
        Expect(TokenType.End, "expected @end");
        return new ObjCInterfaceNode(name, superClass, members, l, c, categoryName);
    }

    // @protocol Name <ParentProto> ... @end
    private ASTNode ParseProtocol()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // @protocol
        string name = Expect(TokenType.Identifier, "expected protocol name").Value;
        var members = new List<ASTNode>();

        // Optional parent protocols: @protocol MyProto <ParentProto>
        if (Match(TokenType.Lt))
        {
            while (!Check(TokenType.Gt) && !Check(TokenType.EOF))
            {
                Expect(TokenType.Identifier, "expected parent protocol name");
                if (!Match(TokenType.Comma)) break;
            }
            Expect(TokenType.Gt, "expected >");
        }

        // Parse protocol members (method declarations only until @end)
        while (!Check(TokenType.End) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.End)) break;
            // @optional / @required 指令 — 消费跳过
            if (Check(TokenType.AtSign))
            {
                Advance(); // @
                if (Check(TokenType.Identifier)) Advance(); // optional/required
                continue;
            }
            // 方法声明: -/+ (type)name[:param:...];
            if (Check(TokenType.Minus) || Check(TokenType.Plus))
            {
                Advance();
                bool isNative = Match(TokenType.Native);
                string retType = "void";
                if (Match(TokenType.LParen))
                { retType = ParseTypeName(); Expect(TokenType.RParen, "expected )"); }
                else { retType = ParseTypeName(); }
                var params2 = new List<(string type, string name)>();
                string methodName = "";
                bool first = true;
                while (!Check(TokenType.Semicolon) && !Check(TokenType.EOF))
                {
                    if (first) { methodName += Expect(TokenType.Identifier, "expected method name").Value; first = false; }
                    if (Match(TokenType.Colon))
                    {
                        methodName += ":";
                        Expect(TokenType.LParen, "expected (");
                        string pType = ParseTypeName();
                        Expect(TokenType.RParen, "expected )");
                        string pName = Expect(TokenType.Identifier, "expected param").Value;
                        params2.Add((pType, pName));
                    }
                    else break;
                }
                Match(TokenType.Semicolon);
                members.Add(new ObjCMethodNode(methodName, retType, params2, new List<ASTNode>(), l, c, isNative));
            }
            else if (Check(TokenType.Property))
            {
                // @property declarations in protocol
                Advance(); // @property
                var attrs = new List<string>();
                if (Match(TokenType.LParen))
                {
                    while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
                    { attrs.Add(Expect(TokenType.Identifier, "expected attribute").Value); Match(TokenType.Comma); }
                    Expect(TokenType.RParen, "expected )");
                }
                string propType = ParseTypeName();
                string propName = Expect(TokenType.Identifier, "expected property name").Value;
                Match(TokenType.Semicolon);
                var pn = new ObjCPropertyNode(propType, propName, l, c);
                foreach (var a in attrs) pn.Attributes.Add(a);
                members.Add(pn);
            }
            else { break; }
        }
        Expect(TokenType.End, "expected @end");
        return new ObjCProtocolNode(name, members, l, c);
    }

    // @implementation Name methods @end
    private ASTNode ParseImplementation()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // @implementation
        string name = Expect(TokenType.Identifier, "expected implementation name").Value;
        string? categoryName = null;

        // Category syntax: @implementation ClassName (CategoryName)  or  @implementation ClassName () = anonymous extension
        if (Match(TokenType.LParen))
        {
            if (!Check(TokenType.RParen))
                categoryName = Expect(TokenType.Identifier, "expected category name").Value;
            Expect(TokenType.RParen, "expected )");
        }

        // 可选的实例变量块: @implementation Name { ivars }
        if (Match(TokenType.LBrace))
        {
            while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
            {
                SkipNewlines();
                if (Check(TokenType.RBrace)) break;
                // 跳过 @public/@private/@protected
                while (Check(TokenType.AtSign)) { Advance(); SkipNewlines(); }
                // 跳过 ivar: type name;
                ParseTypeName();
                if (Check(TokenType.Identifier) || IsTypeName(Cur.Type) || Cur.Type == TokenType.IdType)
                    Advance();
                Match(TokenType.Semicolon);
            }
            Expect(TokenType.RBrace, "expected }");
        }

        var methods = new List<ASTNode>();
        while (!Check(TokenType.End) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.End)) break;
            // parse as C function or method
            if (Check(TokenType.Minus) || Check(TokenType.Plus))
            {
                Advance();
                bool isNative = Match(TokenType.Native);
                // parse return type: (type) or just type
                string retType = "void";
                if (Match(TokenType.LParen))
                {
                    retType = ParseTypeName();
                    Expect(TokenType.RParen, "expected )");
                }
                else
                {
                    retType = ParseTypeName();
                }
                var params2 = new List<(string type, string name)>();
                string methodName = "";
                bool first = true;
                while (!Check(TokenType.LBrace) && !Check(TokenType.EOF))
                {
                    if (first) { methodName += Expect(TokenType.Identifier, "expected method name").Value; first = false; }
                    if (Match(TokenType.Colon))
                    {
                        methodName += ":";
                        Expect(TokenType.LParen, "expected (");
                        string pType = ParseTypeName();
                        Expect(TokenType.RParen, "expected )");
                        string pName = Expect(TokenType.Identifier, "expected param").Value;
                        params2.Add((pType, pName));
                    }
                    else break;
                }
                var body = ParseBlock();
                methods.Add(new ObjCMethodNode(methodName, retType, params2, body, l, c, isNative));
            }
            else if (Match(TokenType.Synthesize))
            {
                // @synthesize propName = ivarName;
                string propName = Expect(TokenType.Identifier, "expected property name").Value;
                string ivarName = propName;
                if (Match(TokenType.Assign))
                    ivarName = Expect(TokenType.Identifier, "expected ivar name").Value;
                Match(TokenType.Semicolon);
                methods.Add(new ObjCSynthesizeNode(propName, ivarName, l, c));
            }
            else if (Match(TokenType.Dynamic))
            {
                // @dynamic propName;
                string propName = Expect(TokenType.Identifier, "expected property name").Value;
                Match(TokenType.Semicolon);
                methods.Add(new ObjCDynamicNode(propName, l, c));
            }
            else if (IsTypeName(Cur.Type) || Cur.Type == TokenType.Void)
            {
                methods.Add(ParseFunctionOrDecl());
            }
            else break;
        }
        Expect(TokenType.End, "expected @end");
        return new ObjCImplNode(name, methods, l, c, categoryName);
    }

    private ASTNode ParseStatement()
    {
        SkipNewlines();
        // C label: name:
        if (Check(TokenType.Identifier) && Peek().Type == TokenType.Colon)
        {
            string labelName = Cur.Value!;
            Advance(); Advance(); // consume name and :
            var stmt = ParseStatement();
            // Wrap as label: block (label, then statement)
            return new BlockNode([new LabelNode(labelName, Cur.Line, Cur.Column), stmt], Cur.Line, Cur.Column);
        }
        if (Check(TokenType.Return)) return ParseReturn();
        if (Check(TokenType.If)) return ParseIf();
        if (Check(TokenType.While)) return ParseWhile();
        if (Check(TokenType.For)) return ParseFor();
        if (Check(TokenType.Goto)) { Advance(); string labelName = Expect(TokenType.Identifier, "expected label name").Value; Match(TokenType.Semicolon); return new GotoNode(labelName, Cur.Line, Cur.Column); }
        if (Check(TokenType.Do)) return ParseDoWhile();
        if (Check(TokenType.Switch)) return ParseSwitch();
        if (Check(TokenType.Try)) return ParseTryCatch();
        if (Check(TokenType.Throw)) return ParseThrow();
        if (Check(TokenType.Synchronized)) return ParseSynchronized();
        if (Check(TokenType.Autoreleasepool)) return ParseAutoreleasepool();
        if (Check(TokenType.Break)) { int l = Cur.Line, c = Cur.Column; Advance(); Match(TokenType.Semicolon); return new BreakNode(l, c); }
        if (Check(TokenType.Continue)) { int l = Cur.Line, c = Cur.Column; Advance(); Match(TokenType.Semicolon); return new ContinueNode(l, c); }
        // 空语句: 单独的 ;
        if (Match(TokenType.Semicolon)) return new LiteralNode(null, Cur.Line, Cur.Column);
        // enum 定义: enum Name { A, B } — 可在函数体内
        if (Check(TokenType.Enum))
        {
            string et = ParseTypeName(); // parses body and stores in _enumValues
            // Skip optional variable declarations after enum body
            while (Check(TokenType.Identifier) || Check(TokenType.Star)) Advance();
            Match(TokenType.Semicolon);
            return new BlockNode([], Cur.Line, Cur.Column);
        }
        if (IsTypeName(Cur.Type)) return ParseVarDecl();

        // 内联汇编: asm("NOP"); (asm 是标识符形式, 非关键字, 与 C/C++ 对齐)
        if (Check(TokenType.Identifier) && Cur.Value == "asm" && Peek().Type == TokenType.LParen)
        {
            int l = Cur.Line, c = Cur.Column;
            Advance(); // asm
            Expect(TokenType.LParen, "expected ( after asm");
            string asmCode = Expect(TokenType.String, "expected string literal in asm").Value;
            Expect(TokenType.RParen, "expected ) after asm");
            Match(TokenType.Semicolon);
            return new AsmStatement(asmCode, l, c);
        }

        var expr = ParseExpression();
        Match(TokenType.Semicolon);
        return expr;
    }

    private ASTNode ParseReturn()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        ASTNode? val = Check(TokenType.Semicolon) ? null : ParseExpression();
        Match(TokenType.Semicolon);
        return new ReturnNode(val, l, c);
    }

    private ASTNode ParseIf()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected (");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected )");
        var thenBody = ParseStatementAsList();
        List<ASTNode>? elseBody = null;
        if (Match(TokenType.Else))
        {
            if (Check(TokenType.If)) elseBody = new List<ASTNode> { ParseIf() };
            else elseBody = ParseStatementAsList();
        }
        return new IfNode(cond, thenBody, elseBody, l, c);
    }

    private ASTNode ParseWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected (");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected )");
        var body = ParseStatementAsList();
        return new WhileNode(cond, body, l, c);
    }

    private ASTNode ParseFor()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance();
        Expect(TokenType.LParen, "expected (");

        // 检测快速枚举: for (type var in collection)
        if (IsTypeName(Cur.Type))
        {
            string varType = ParseTypeName();
            if (Check(TokenType.Identifier))
            {
                string varName = Expect(TokenType.Identifier, "expected variable name").Value;
                // 检查 "in" 关键字
                if (Cur.Type == TokenType.Identifier && Cur.Value == "in")
                {
                    Advance(); // consume "in"
                    var collection = ParseExpression();
                    Expect(TokenType.RParen, "expected )");
                    var foreachBody = ParseStatementAsList();
                    return new ObjCForEachNode(varType, varName, collection, foreachBody, l, c);
                }
                // 回退：不是快速枚举，而是常规 for
                // 已经消费了 type 和 name，需要当做 varDecl 处理
                ASTNode? forInit = new VarDeclNode(varType, varName, null, l, c);
                if (Match(TokenType.Assign))
                {
                    var initExpr = ParseExpression();
                    forInit = new VarDeclNode(varType, varName, initExpr, l, c);
                }
                Match(TokenType.Semicolon);
                ASTNode? forCond = Check(TokenType.Semicolon) ? null : ParseExpression();
                Match(TokenType.Semicolon);
                ASTNode? forUpdate = Check(TokenType.RParen) ? null : ParseExpression();
                Expect(TokenType.RParen, "expected )");
                var forBody = ParseStatementAsList();
                return new ForNode(forInit, forCond, forUpdate, forBody, l, c);
            }
        }

        ASTNode? init = IsTypeName(Cur.Type) ? ParseVarDecl() : (Check(TokenType.Semicolon) ? null : ParseExpression());
        Match(TokenType.Semicolon);
        ASTNode? cond = Check(TokenType.Semicolon) ? null : ParseExpression();
        Match(TokenType.Semicolon);
        ASTNode? update = Check(TokenType.RParen) ? null : ParseExpression();
        Expect(TokenType.RParen, "expected )");
        var body = ParseStatementAsList();
        return new ForNode(init, cond, update, body, l, c);
    }

    private ASTNode ParseDoWhile()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // do
        var body = ParseStatementAsList();
        Expect(TokenType.While, "expected 'while' after do body");
        Expect(TokenType.LParen, "expected (");
        var cond = ParseExpression();
        Expect(TokenType.RParen, "expected )");
        Match(TokenType.Semicolon);
        return new DoWhileNode(cond, body, l, c);
    }

    private ASTNode ParseSwitch()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // switch
        Expect(TokenType.LParen, "expected (");
        var expr = ParseExpression();
        Expect(TokenType.RParen, "expected )");
        Expect(TokenType.LBrace, "expected {");
        var cases = new List<(ASTNode? caseVal, List<ASTNode> body)>();
        List<ASTNode>? currentBody = null;
        while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
        {
            SkipNewlines();
            if (Check(TokenType.RBrace)) break;
            if (Match(TokenType.Case))
            {
                if (currentBody != null) cases.Add((null, currentBody)); // flush previous default
                var val = ParseExpression();
                Expect(TokenType.Colon, "expected :");
                currentBody = new List<ASTNode>();
                cases.Add((val, currentBody));
            }
            else if (Match(TokenType.Default))
            {
                if (currentBody != null) cases.Add((null, currentBody));
                Expect(TokenType.Colon, "expected :");
                currentBody = new List<ASTNode>();
                cases.Add((null, currentBody));
            }
            else if (currentBody != null)
            {
                currentBody.Add(ParseStatement());
            }
            else
            {
                throw Error($"Expected case or default at {Cur.Line}:{Cur.Column}");
            }
        }
        Expect(TokenType.RBrace, "expected }");
        return new SwitchNode(expr, cases, l, c);
    }

    private ASTNode ParseVarDecl()
    {
        int l = Cur.Line, c = Cur.Column;
        string type = ParseTypeName();
        // skip pointer stars
        while (Match(TokenType.Star)) type += "*";
        // Block pointer type: int(^name)(params) — (^name) before params
        bool isBlockPtr = false;
        if (Check(TokenType.LParen) && _tokens.Count > _pos + 1 &&
            _tokens[_pos + 1].Type == TokenType.Caret)
        {
            isBlockPtr = true;
            Advance(); // skip (
            Advance(); // skip ^
            type = $"{type}(^)"; // mark as block pointer type
        }
        string name = Expect(TokenType.Identifier, "expected variable name").Value;
        if (isBlockPtr)
            Expect(TokenType.RParen, "expected ) after block pointer name");
        // 跳过 block 指针的参数类型: (Type1, Type2, ...)
        if (isBlockPtr && Check(TokenType.LParen))
        {
            Advance(); // skip (
            int parenDepth = 1;
            while (parenDepth > 0 && !IsAtEnd)
            {
                if (Check(TokenType.LParen)) parenDepth++;
                else if (Check(TokenType.RParen)) parenDepth--;
                if (parenDepth > 0) Advance();
            }
            Expect(TokenType.RParen, "expected ) after block parameter types");
        }
        ASTNode? init = null;
        int arraySize = 0;
        // 数组: int arr[N]
        if (Match(TokenType.LBracket))
        {
            var sizeNode = ParseExpression();
            // 提取数组大小：支持 int, float, double, string 等类型
            if (sizeNode is LiteralNode lit)
            {
                if (lit.Value is int s) arraySize = s;
                else if (lit.Value is float f) arraySize = (int)f;
                else if (lit.Value is double d) arraySize = (int)d;
                else if (lit.Value is string str && int.TryParse(str, out int ps)) arraySize = ps;
                else if (lit.Value is long lv) arraySize = (int)lv;
            }
            Expect(TokenType.RBracket, "expected ]");
        }
        if (Match(TokenType.Assign)) init = ParseExpression();
        // 多变量声明: int a = 1, b = 2;
        // 返回 BlockNode，其中包含所有变量声明（调用者需处理）
        var decls = new List<ASTNode>();
        decls.Add(new VarDeclNode(type, name, init, l, c, arraySize));
        while (Match(TokenType.Comma))
        {
            string vname = Expect(TokenType.Identifier, "expected variable name").Value;
            ASTNode? vinit = null;
            if (Match(TokenType.Assign))
                vinit = ParseExpression();
            decls.Add(new VarDeclNode(type, vname, vinit, l, c, 0));
        }
        Match(TokenType.Semicolon);
        if (decls.Count == 1)
            return decls[0];
        return new BlockNode(decls, l, c);
    }

    private List<ASTNode> ParseStatementAsList()
    {
        if (Check(TokenType.LBrace)) return ParseBlock();
        return new List<ASTNode> { ParseStatement() };
    }

    private ASTNode ParseExpression() => ParseAssignment();

    private ASTNode ParseAssignment()
    {
        var left = ParseTernary();
        if (Check(TokenType.Assign) || Check(TokenType.PlusAssign) || Check(TokenType.MinusAssign) || Check(TokenType.MulAssign) || Check(TokenType.DivAssign))
        {
            string op = Advance().Value;
            var right = ParseAssignment();
            if (left is VarNode v && op == "=")
                return new AssignNode(v.Name, right, left.Line, left.Column);
            // 支持 *ptr = val, arr[i] = val 等非简单变量的赋值
            return new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseTernary()
    {
        var left = ParseLogicalOr();
        if (Match(TokenType.Question))
        {
            var thenExpr = ParseExpression();
            Expect(TokenType.Colon, "expected :");
            var elseExpr = ParseTernary();
            return new BinaryNode(new BinaryNode(left, "?", thenExpr, left.Line, left.Column), ":", elseExpr, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicalOr()
    {
        var left = ParseLogicalAnd();
        while (Match(TokenType.Or)) // ||
        {
            var right = ParseLogicalAnd();
            left = new BinaryNode(left, "||", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseLogicalAnd()
    {
        var left = ParseBitwiseOr();
        while (Match(TokenType.And)) // &&
        {
            var right = ParseBitwiseOr();
            left = new BinaryNode(left, "&&", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseOr()
    {
        var left = ParseBitwiseXor();
        while (Match(TokenType.Pipe))
        {
            var right = ParseBitwiseXor();
            left = new BinaryNode(left, "|", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseXor()
    {
        var left = ParseBitwiseAnd();
        while (Match(TokenType.Caret))
        {
            var right = ParseBitwiseAnd();
            left = new BinaryNode(left, "^", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseBitwiseAnd()
    {
        var left = ParseShift();
        while (Match(TokenType.Amp))
        {
            var right = ParseShift();
            left = new BinaryNode(left, "&", right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseShift()
    {
        var left = ParseComparison();
        while (Check(TokenType.LShift) || Check(TokenType.RShift))
        {
            string op = Advance().Value;
            var right = ParseComparison();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseComparison()
    {
        var left = ParseAdditive();
        while (Check(TokenType.Eq) || Check(TokenType.Neq) || Check(TokenType.Lt) || Check(TokenType.Gt) || Check(TokenType.Le) || Check(TokenType.Ge))
        {
            string op = Advance().Value;
            var right = ParseAdditive();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseAdditive()
    {
        var left = ParseMultiplicative();
        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            string op = Advance().Value;
            var right = ParseMultiplicative();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseMultiplicative()
    {
        var left = ParseUnary();
        while (Check(TokenType.Star) || Check(TokenType.Slash) || Check(TokenType.Percent))
        {
            string op = Advance().Value;
            var right = ParseUnary();
            left = new BinaryNode(left, op, right, left.Line, left.Column);
        }
        return left;
    }

    private ASTNode ParseUnary()
    {
        if (Match(TokenType.Minus)) return new UnaryNode("-", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Not)) return new UnaryNode("!", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Star)) return new UnaryNode("*", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Amp)) return new UnaryNode("&", ParseUnary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Increment)) return new UnaryNode("++", ParsePrimary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Decrement)) return new UnaryNode("--", ParsePrimary(), Cur.Line, Cur.Column);
        if (Match(TokenType.Caret)) return ParseBlockLiteral(); // ^ block literal
        return ParsePrimary();
    }

    // ObjC block literal: ^int(int x){ return x*2; } or ^{ ... }
    private ASTNode ParseBlockLiteral()
    {
        int l = Cur.Line, c = Cur.Column;
        string returnType = "void";
        var parameters = new List<(string type, string name)>();

        if (!Check(TokenType.LBrace))
        {
            // Parse return type if next token is not (
            if (!Check(TokenType.LParen))
                returnType = ParseTypeName();
            // Parse parameter list
            if (Match(TokenType.LParen))
            {
                if (!Check(TokenType.RParen))
                {
                    do {
                        string pType = ParseTypeName();
                        string pName = Expect(TokenType.Identifier, "expected param name").Value;
                        parameters.Add((pType, pName));
                    } while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected )");
            }
        }

        var body = ParseBlock();
        return new ObjCBlockNode(returnType, parameters, body, l, c);
    }

    private ASTNode ParsePrimary()
    {
        int l = Cur.Line, c = Cur.Column;

        if (Check(TokenType.Number)) { var t = Advance(); string v = t.Value; string original = v;
            // 去掉所有数字后缀: f/F/l/L/u/U
            while (v.Length > 0 && (v[^1] == 'f' || v[^1] == 'F' || v[^1] == 'l' || v[^1] == 'L' || v[^1] == 'u' || v[^1] == 'U'))
                v = v[..^1];
            if (v.Contains('.'))
            {
                // f/F 后缀 → float，否则默认 → double（符合 C/ObjC 标准）
                bool isFloat = original.EndsWith("f", System.StringComparison.OrdinalIgnoreCase);
                if (isFloat)
                    return new LiteralNode(float.Parse(v, System.Globalization.CultureInfo.InvariantCulture), l, c);
                else
                    return new LiteralNode(double.Parse(v, System.Globalization.CultureInfo.InvariantCulture), l, c);
            }
            // 64位: LL 后缀 或 值超过 int 范围
            bool isLong = original.EndsWith("ll", System.StringComparison.OrdinalIgnoreCase)
                || (original.EndsWith("l", System.StringComparison.OrdinalIgnoreCase) && !original.EndsWith("ul", System.StringComparison.OrdinalIgnoreCase));
            if (isLong || !int.TryParse(v, out _))
            {
                if (long.TryParse(v, out long lv))
                    return new LiteralNode(lv, l, c);
            }
            if (int.TryParse(v, out int iv))
                return new LiteralNode(iv, l, c);
            return new LiteralNode(0, l, c); }
        if (Check(TokenType.String)) { var t = Advance(); return new LiteralNode(t.Value, l, c); }
        if (Check(TokenType.CharLiteral)) { var t = Advance(); return new LiteralNode(t.Value.Length > 0 ? (object)(int)t.Value[0] : (object)0, l, c); }
        if (Check(TokenType.ObjCString)) { var t = Advance(); return new LiteralNode("@" + t.Value, l, c); }
        if (Match(TokenType.NilObj)) return new LiteralNode(0, l, c);
        if (Match(TokenType.YesObj)) return new LiteralNode(1, l, c);
        if (Match(TokenType.NoObj)) return new LiteralNode(0, l, c);
        if (Match(TokenType.Self)) return new VarNode("self", l, c);
        if (Match(TokenType.Super)) return new VarNode("super", l, c);
        // @selector(method:param:) — 编译为 selector 字符串常量
        if (Match(TokenType.Selector))
        {
            Expect(TokenType.LParen, "expected ( after @selector");
            string selName = "";
            while (!Check(TokenType.RParen) && !Check(TokenType.EOF))
            {
                if (Check(TokenType.Identifier))
                    selName += Advance().Value;
                if (Match(TokenType.Colon))
                    selName += ":";
                else if (!Check(TokenType.RParen))
                    Advance();
            }
            Expect(TokenType.RParen, "expected )");
            return new LiteralNode(selName, l, c);
        }

        // C 初始化列表: {1, 2, 3}
        if (Check(TokenType.LBrace))
        {
            Advance(); // {
            var elements = new List<ASTNode>();
            if (!Check(TokenType.RBrace))
            {
                do elements.Add(ParseExpression());
                while (Match(TokenType.Comma));
            }
            Expect(TokenType.RBrace, "expected }");
            // 简化: 返回第一个元素作为占位
            return elements.Count > 0 ? elements[0] : new LiteralNode(0, l, c);
        }

        // ObjC message expression: [receiver method:arg ...]
        if (Check(TokenType.LBracket))
        {
            Advance();
            // 区分: [obj msg] (消息发送) vs [1,2,3] (数组) vs 普通括号
            var receiverExpr = ParseExpression();
            // 直接闭合 → 可能是空数组或索引(不应出现在此处)
            if (Check(TokenType.RBracket))
            {
                Advance(); // consume ]
                return receiverExpr; // 作为普通表达式返回
            }
            if (Check(TokenType.Identifier) || Check(TokenType.Colon))
            {
                // message send: [receiver method:arg]
                var args = new List<ASTNode>();
                string method = "";
                while (!Check(TokenType.RBracket) && !Check(TokenType.EOF))
                {
                    if (Check(TokenType.Identifier))
                    {
                        method += Advance().Value;
                    }
                    if (Match(TokenType.Colon))
                    {
                        method += ":";
                        args.Add(ParseExpression());
                    }
                    else break;
                }
                Expect(TokenType.RBracket, "expected ]");
                return new MsgSendNode(receiverExpr, method, args, l, c);
            }
            // C 数组索引回退: 这是在 ParsePrimary 上下文，如 arr[1]
            // 此处 receiverExpr 是 arr，当前token是 ]
            // 但我们已经做了 ParseExpression，所以当作索引
            // 实际上这种情况应该由 Identifier 分支的 LBracket 处理
            // 如果到达这里，说明是 [expr] 包装表达式
            Expect(TokenType.RBracket, "expected ]");
            return receiverExpr;
        }

        // sizeof 运算符: sizeof(int) → 返回4 (MCU简化)
        if (Check(TokenType.Identifier) && Cur.Value == "sizeof")
        {
            Advance(); // sizeof
            int size = 4; // 默认32位MCU = 4字节
            if (Match(TokenType.LParen))
            {
                // 跳过类型名
                while (!Check(TokenType.RParen) && !Check(TokenType.EOF)) Advance();
                Expect(TokenType.RParen, "expected )");
            }
            return new LiteralNode(size, l, c);
        }

        if (Check(TokenType.Identifier))
        {
            string name = Advance().Value;
            if (Match(TokenType.LParen))
            {
                var args = new List<ASTNode>();
                if (!Check(TokenType.RParen))
                {
                    do args.Add(ParseExpression());
                    while (Match(TokenType.Comma));
                }
                Expect(TokenType.RParen, "expected )");
                return new CallNode(name, args, l, c);
            }
            // C struct 成员访问: obj.field
            if (Match(TokenType.Dot))
            {
                string field = Expect(TokenType.Identifier, "expected field name").Value;
                ASTNode ma = new MemberAccessNode(new VarNode(name, l, c), field, false, l, c);
                while (Match(TokenType.LBracket))
                {
                    var index = ParseExpression();
                    Expect(TokenType.RBracket, "expected ]");
                    ma = new BinaryNode(ma, "[]", index, l, c);
                }
                return ma;
            }
            // C 指针成员访问: ptr->field
            if (Match(TokenType.Arrow))
            {
                string field = Expect(TokenType.Identifier, "expected field name").Value;
                ASTNode ma = new MemberAccessNode(new VarNode(name, l, c), field, true, l, c);
                while (Match(TokenType.LBracket))
                {
                    var index = ParseExpression();
                    Expect(TokenType.RBracket, "expected ]");
                    ma = new BinaryNode(ma, "[]", index, l, c);
                }
                return ma;
            }
            // C 数组索引: arr[1] → BinaryNode(arr, "[]", index)
            ASTNode result = new VarNode(name, l, c);
            while (Match(TokenType.LBracket))
            {
                var index = ParseExpression();
                Expect(TokenType.RBracket, "expected ]");
                result = new BinaryNode(result, "[]", index, l, c);
            }
            if (result is BinaryNode) return result;
            // 后置 ++ / --
            if (Match(TokenType.Increment))
                return new UnaryNode("++", new VarNode(name, l, c), l, c);
            if (Match(TokenType.Decrement))
                return new UnaryNode("--", new VarNode(name, l, c), l, c);
            // Check for enum constant before treating as variable
            if (_enumValues.TryGetValue(name, out int enumVal))
                return new LiteralNode(enumVal, l, c);
            return new VarNode(name, l, c);
        }

        // Boxed expression: @(expr) → ObjC number/string boxing
        if (Check(TokenType.AtSign) && Peek().Type == TokenType.LParen)
        {
            int atLine = Cur.Line, atCol = Cur.Column;
            Advance(); // @
            Advance(); // (
            var boxedExpr = ParseExpression();
            Expect(TokenType.RParen, "expected ) after boxed expression");
            return new ObjCBoxedNode(boxedExpr, atLine, atCol);
        }

        // C-style cast: (type)expr — 例如 (void)x, (int)3.14
        if (Check(TokenType.LParen) && IsTypeName(Peek().Type))
        {
            int cl = Cur.Line, cc = Cur.Column;
            Advance(); // consume (
            string castType = ParseTypeName();
            Expect(TokenType.RParen, "expected )");
            var castExpr = ParsePrimary();
            return new CastNode(castType, castExpr, cl, cc);
        }

        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            Expect(TokenType.RParen, "expected )");
            return expr;
        }

        throw Error($"Unexpected token: {Cur.Type}({Cur.Value}) at {Cur.Line}:{Cur.Column}");
    }

    // @try { body } @catch (NSException *e) { catchBody } @finally { finallyBody }
    private ASTNode ParseTryCatch()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // @try
        var tryBody = ParseBlock();

        // @catch 块 (可选，可多个)
        var catches = new List<(string? excType, string? excName, List<ASTNode> body)>();
        while (Match(TokenType.Catch))
        {
            string? excType = null, excName = null;
            if (Match(TokenType.LParen))
            {
                // (NSException *e) 或 (...)
                if (!Check(TokenType.Ellipsis))
                {
                    excType = ParseTypeName();
                    while (Match(TokenType.Star)) excType += "*";
                    if (!Check(TokenType.RParen))
                        excName = Expect(TokenType.Identifier, "expected exception variable").Value;
                }
                else
                {
                    // catch-all: @catch(...) — 跳过 ...
                    while (Check(TokenType.Ellipsis)) Advance();
                }
                Expect(TokenType.RParen, "expected )");
            }
            var catchBody = ParseBlock();
            catches.Add((excType, excName, catchBody));
        }

        // @finally 块 (可选)
        List<ASTNode>? finallyBody = null;
        if (Match(TokenType.Finally))
        {
            finallyBody = ParseBlock();
        }

        return new ObjCTryNode(tryBody, catches, finallyBody, l, c);
    }

    // @throw expr;
    private ASTNode ParseThrow()
    {
        int l = Cur.Line, c = Cur.Column;
        Advance(); // @throw
        ASTNode? expr = null;
        if (!Check(TokenType.Semicolon) && !Check(TokenType.Newline))
            expr = ParseExpression();
        Match(TokenType.Semicolon);
        return new ObjCThrowNode(expr, l, c);
    }

    // @synchronized(expr) { body } — MCU 模式视为普通代码块
    private ASTNode ParseSynchronized()
    {
        Advance(); // @synchronized
        Expect(TokenType.LParen, "expected (");
        ParseExpression(); // 同步对象 (MCU模式忽略)
        Expect(TokenType.RParen, "expected )");
        var body = ParseBlock();
        // 作为匿名代码块返回
        return new ObjCSynchronizedNode(body, Cur.Line, Cur.Column);
    }

    // @autoreleasepool { body } — MCU 模式视为普通代码块
    private ASTNode ParseAutoreleasepool()
    {
        Advance(); // @autoreleasepool
        var body = ParseBlock();
        return new ObjCAutoreleasepoolNode(body, Cur.Line, Cur.Column);
    }

    private void SkipNewlines() { while (Check(TokenType.Newline)) Advance(); }
    private void SkipParens() { int d=1; Advance(); while (d>0&&!Check(TokenType.EOF)){if(Check(TokenType.LParen))d++;if(Check(TokenType.RParen))d--;Advance();} }
}
