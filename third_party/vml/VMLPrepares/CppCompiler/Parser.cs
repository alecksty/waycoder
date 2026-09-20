using CompilerBase;
using VMLPlugins;

namespace CppCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        private int _anonCount;
        private bool _isMCU;
        private List<ClassMember> _pendingMembers = new();

        /// <summary>
        /// 「结构体别名 → 标签」表（`typedef struct tm tm_t;` ⇒ `tm_t → tm`）。
        /// 见 <see cref="ParseTypedef"/> 与 <see cref="ParseType"/>：
        /// 下游是按标签查结构定义的，不换名字的话 `tm_t` 只是个查不到定义的裸类型串。
        /// </summary>
        private readonly Dictionary<string, string> _structAliases = new();

        /// <summary>
        /// 解析过程中产生的**额外顶层声明** —— 目前只有「typedef 里那个结构体定义」。
        ///
        /// <para>
        /// `typedef struct Tag { int x; } Alias;` 会解析出一个 `ClassDecl`，而 `ParseTypedef`
        /// 只能返回**一个**节点（那是变量占位）。丢掉它的话结构体定义就**从未进过
        /// `_classes`**（`CodeGenerator.CollectDeclarations` 是从 `program.Declarations` 收的），
        /// 于是 `Alias v; v.x` 解析不出成员 —— 症状是**静默编错**（两个成员落到同一处地址，
        /// 读谁都是最后写的那个值），比报错难查得多。实测 `pt_t p; p.x=3; p.y=4;` 打出 `4,4`。
        /// </para>
        /// </summary>
        private readonly List<ASTNode> _pendingDecls = new();

        protected override TokenType GetTokenType(Token token) => token.Type;

        /// <summary>
        /// 诊断用的行号 —— **取原文件的行，不取拼接流的行**。
        ///
        /// <para>
        /// `ParserBase` 里这两个是 `=> 0` 的虚方法，不覆写就恒为 0：于是所有解析错的位置
        /// 都一样，而 `DiagnosticBag` 按「码+文件+行+列+消息」去重 ⇒ **一个文件里的多处
        /// 同类语法错会被并成一条**（与 C 前端 `Parser.Core.cs` 记的是同一个坑）。
        /// </para>
        /// </summary>
        protected override int GetTokenLine(Token token) => token.OriginalLine > 0 ? token.OriginalLine : token.Line;

        /// <summary>同上 —— 列号。</summary>
        protected override int GetTokenColumn(Token token) => token.Column;

        public Parser(List<Token> tokens, bool isMCU = true) : base(tokens) { _anonCount = 0; _isMCU = isMCU; }

        /// <summary>
        /// 把当前 token 的位置写成 <c>原文件:原行:列: </c> 前缀（GCC 形态）供 <see cref="Expect"/> 用。
        ///
        /// ⚠ 文件必须**真的写出来**：不写的话「报错在头文件里」这件事就丢了，编辑器只能把它
        ///   当成用户自己文件的错误、按行号硬贴上去（用户报的「第 112 行那个注释被标红」正是这一环）。
        /// </summary>
        private string Where(Token t)
        {
            var (file, line) = MapOriginal(t.Line);
            if (file == null) file = FileName ?? "<input>";
            return $"{file}:{line}:{Math.Max(t.Column, 0)}: ";
        }

        public Program Parse()
        {
            var program = new Program();
            while (!IsAtEnd)
            {
                var decl = ParseDeclaration();
                if (decl != null) program.Declarations.Add(decl);
                else Advance();
                // 解析这一条时顺带产出的额外顶层声明（typedef 里的结构体定义）——
                // 必须**进 program**，否则 `_classes` 里没有它（见 `_pendingDecls` 的说明）。
                if (_pendingDecls.Count > 0)
                {
                    program.Declarations.AddRange(_pendingDecls);
                    _pendingDecls.Clear();
                }
            }
            return program;
        }

        /// <summary>
        /// 顶层声明入口 —— 与 <see cref="ParseStatement"/> 同一套路，**顺手记下起点位置**
        /// （`Line`/`Column`/`OriginalLine`/`OriginalFile`）。
        ///
        /// <para>
        /// 为什么顶层也要盖：全局变量的初始化式出问题（`int g = 没声明的名字;`）报的是
        /// 「未声明的变量」——不盖位置的话它只能拿到**上一句遗留的行号**（比没有行号更糟，
        /// 用户会去改一行毫不相干的代码）。判据照旧 `Line == 0` 才盖：精确值不被外层冲掉。
        /// </para>
        /// </summary>
        private ASTNode? ParseDeclaration()
        {
            int __line = Cur.Line, __col = Cur.Column;
            var (__file, __originLine) = MapOriginal(__line);
            var __node = ParseDeclarationCore();
            if (__node != null && __node.Line == 0)
            {
                __node.Line = __line;
                __node.Column = __col;
                __node.OriginalLine = __originLine;
                __node.OriginalFile = __file;
            }
            return __node;
        }

        // Top-level declarations
        private ASTNode? ParseDeclarationCore()
        {
            // 处理调用约定属性: __cdecl, __stdcall, __fastcall
            CallingConvention? pendingConvention = null;
            if (Check(TokenType.STDCALL)) { Advance(); pendingConvention = CallingConvention.Stdcall; }
            else if (Check(TokenType.FASTCALL)) { Advance(); pendingConvention = CallingConvention.Fastcall; }
            else if (Check(TokenType.CDECL)) { Advance(); pendingConvention = CallingConvention.Cdecl; }

            ASTNode? result;
            if (Match(TokenType.TYPEDEF)) result = ParseTypedef();
            else if (Match(TokenType.NAMESPACE)) result = ParseNamespace();
            else if (Match(TokenType.USING)) result = ParseUsing();
            else if (Match(TokenType.CLASS)) result = ParseClass();
            else if (Match(TokenType.STRUCT)) result = ParseStructLike(); // struct also
            else if (Match(TokenType.ENUM)) result = ParseEnum();
            else if (Match(TokenType.UNION)) result = ParseUnion();
            else if (Match(TokenType.CONSTEXPR)) { result = ParseDeclaration(); }
            else if (Match(TokenType.STATIC_ASSERT)) { SkipTo(TokenType.SEMICOLON); if (Check(TokenType.SEMICOLON)) Advance(); result = null; }
            else if (Match(TokenType.NOEXCEPT)) { result = ParseDeclaration(); } // skip noexcept specifier
            else if (Match(TokenType.TEMPLATE)) { result = ParseTemplateDeclaration(); }
            else if (Match(TokenType.EXTERN)) { result = ParseExternBlock(); }
            else if (Match(TokenType.FRIEND)) { result = ParseFriendDeclaration(); }
            else if (IsTypeToken()) result = ParseFunctionOrVariable();
            else if (Check(TokenType.IDENTIFIER))
            {
                int save = _pos;
                string name = Cur.Value;
                Advance();
                if (Match(TokenType.LPAREN)) { _pos = save; result = ParseFunction(); }
                else if (Match(TokenType.SCOPE_RESOLVE))
                {
                    _pos = save;
                    result = ParseFunctionOrVariable();
                }
                else { _pos = save; result = ParseFunctionOrVariable(); }
            }
            else result = null;

            // Apply pending convention to function declarations
            if (pendingConvention.HasValue && result is FunctionDecl fd)
                fd.Convention = pendingConvention.Value;

            return result;
        }

        private ASTNode? ParseFriendDeclaration()
        {
            // friend class Foo; 或 friend ReturnType func(params);
            // 简化处理：解析但不生成任何特殊代码
            if (Match(TokenType.CLASS))
            {
                Expect(TokenType.IDENTIFIER);
                Expect(TokenType.SEMICOLON);
                return new VariableDecl { Type = "friend_class", Name = "_dummy" };
            }
            if (IsTypeToken())
            {
                string type = ParseType();
                string name = Expect(TokenType.IDENTIFIER).Value;
                if (Match(TokenType.LPAREN))
                {
                    // friend 函数声明
                    while (!Check(TokenType.RPAREN) && !IsAtEnd)
                    {
                        string pt = ParseType();
                        if (!Check(TokenType.RPAREN))
                            Expect(TokenType.IDENTIFIER);
                        if (!Match(TokenType.COMMA)) break;
                    }
                    Expect(TokenType.RPAREN);
                }
                Expect(TokenType.SEMICOLON);
                return new FunctionDecl { Name = name, ReturnType = type, IsMember = false };
            }
            // 跳过未知的 friend 声明
            SkipTo(TokenType.SEMICOLON);
            if (Match(TokenType.SEMICOLON)) { }
            return new VariableDecl { Type = "friend", Name = "_dummy" };
        }

        /// <summary>
        /// `typedef` 的**三种**形态（`struct` 那一支）：
        /// <code>
        /// ① typedef struct { ... } Alias;        匿名结构体 + 别名
        /// ② typedef struct Tag { ... } Alias;    结构体定义 + 别名
        /// ③ typedef struct Tag Alias;            引用**已声明**的结构体（`Lib/c/time.h:31`）
        /// </code>
        ///
        /// ⚠ **②③ 里 `struct` 后面第一个标识符是「标签」、第二个才是「别名」**。
        /// 老代码把标签当成别名吃掉、紧接着 `Expect(SEMICOLON)`，于是撞在真正的别名上 ——
        /// 真机上那句 `Expected SEMICOLON but got IDENTIFIER ('tm_t') at line 112:` 就是它
        /// （`#include &lt;time.h&gt;` 的 C++ 程序一律编不过）。②同理：它连 `}` 都撞不过去。
        ///
        /// ⚠ **光把语法吃下去不够，别名必须真的登记**：下游解析结构体成员时是按
        /// **标签**查 `_classes` 的（`CodeGenerator.ResolveClassOf` → `CleanType(type)`），
        /// 不登记的话 `tm_t *p; p-&gt;tm_sec` 仍然解析不出结构体 —— 错误只是换个地方冒出来。
        /// </summary>
        private ASTNode? ParseTypedef()
        {
            // typedef existing_type new_name;
            // typedef struct { ... } new_name;
            if (Match(TokenType.STRUCT))
            {
                if (Check(TokenType.LBRACE))
                {
                    // ① 匿名结构体 + 别名（体的名字是 `_anon_N`）
                    var anon = ParseStructLikeBody();
                    string anonAlias = Expect(TokenType.IDENTIFIER).Value;
                    if (anon != null)
                    {
                        _pendingDecls.Add(anon);                 // 定义要进 program（见 _pendingDecls）
                        RegisterStructAlias(anonAlias, anon.Name);
                    }
                    Expect(TokenType.SEMICOLON);
                    return new VariableDecl { Type = "typedef", Name = "_dummy" };
                }

                // ②③ 都以「标签」打头
                string tag = Expect(TokenType.IDENTIFIER).Value;
                if (Check(TokenType.LBRACE) || Check(TokenType.COLON))
                {
                    // ② 结构体定义 + 别名 —— 成员体走 ParseClassBodyCore（它**不吞**尾随的 `;`，
                    //    因为这里 `}` 后面跟的是别名而不是分号）
                    var body = ParseClassBodyCore(tag);
                    _pendingDecls.Add(body);                     // 定义要进 program（见 _pendingDecls）
                    string alias2 = Expect(TokenType.IDENTIFIER).Value;
                    RegisterStructAlias(alias2, tag);
                }
                else if (Check(TokenType.IDENTIFIER))
                {
                    // ③ 已声明结构体 + 别名（没有被体）
                    string alias3 = Advance().Value;
                    RegisterStructAlias(alias3, tag);
                }
                // 否则是 `typedef struct Tag;`（只有标签、没有别名）—— 读过标签即可
                Expect(TokenType.SEMICOLON);
                return new VariableDecl { Type = "typedef", Name = "_dummy" };
            }
            // typedef existing_type new_name;
            string type = ParseType();
            string name = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.SEMICOLON);
            return new VariableDecl { Type = "typedef", Name = "_dummy" };
        }

        /// <summary>
        /// 登记「结构体别名 → 标签」。`ParseType` 见到别名时换成标签，下游才查得到结构定义
        /// （成员访问 `aliased.member` 全靠这一条）。
        /// </summary>
        private void RegisterStructAlias(string alias, string? tag)
        {
            if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(tag)) return;
            _structAliases[alias] = tag;
        }

        private ASTNode? ParseStructLike()
        {
            // struct { ... } x; — anonymous struct with variable
            if (Check(TokenType.LBRACE))
            {
                var cd = ParseStructLikeBody();
                if (cd != null && !IsAtEnd && (Check(TokenType.IDENTIFIER) || Check(TokenType.STAR)))
                {
                    string varName = Expect(TokenType.IDENTIFIER).Value;
                    while (Match(TokenType.STAR)) varName += "*";
                    Expect(TokenType.SEMICOLON);
                    return new VariableDecl { Type = cd.Name, Name = varName };
                }
                return cd;
            }
            // named struct: struct Name {...} OR struct Name var; OR struct Name func(...);
            if (Check(TokenType.IDENTIFIER))
            {
                string name = Advance().Value;
                // struct Name {...} — definition
                if (Check(TokenType.LBRACE) || Check(TokenType.COLON))
                    return ParseClassBody(name);
                // struct Name; — forward declaration
                if (Check(TokenType.SEMICOLON))
                {
                    Advance();
                    return new ClassDecl { Name = name };
                }
                // struct Name var/func — variable or function using previously declared struct type
                // Push back the name so ParseFunctionOrVariable() can consume it
                _pos--;
                return ParseFunctionOrVariable();
            }
            return ParseClass(); // fallback
        }

        private ClassDecl? ParseStructLikeBody()
        {
            if (!Check(TokenType.LBRACE)) return null;
            string anonName = "_anon_" + (_anonCount++);
            Expect(TokenType.LBRACE, "StructLikeBody");
            var cd = new ClassDecl { Name = anonName, CurrentAccess = AccessSpec.Public };
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                var member = ParseClassMember(cd.CurrentAccess);
                if (member != null) { cd.Members.Add(member); cd.Members.AddRange(_pendingMembers); _pendingMembers.Clear(); }
                else Advance();
            }
            Expect(TokenType.RBRACE);
            return cd;
        }

        private ASTNode ParseNamespace()
        {
            string name = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.LBRACE, "Namespace");
            var ns = new NamespaceDecl { Name = name };
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                var d = ParseDeclaration();
                if (d != null) ns.Members.Add(d);
            }
            Expect(TokenType.RBRACE);
            return ns;
        }

        private ASTNode ParseUsing()
        {
            if (Match(TokenType.NAMESPACE))
            {
                string name = "";
                while (Check(TokenType.IDENTIFIER))
                {
                    name += Cur.Value; Advance();
                    if (Match(TokenType.SCOPE_RESOLVE)) name += "::";
                }
                Expect(TokenType.SEMICOLON);
                return new UsingDecl { NamespaceName = name };
            }
            SkipTo(TokenType.SEMICOLON);
            return null;
        }

        private ASTNode ParseClass()
        {
            string name = Expect(TokenType.IDENTIFIER).Value;
            return ParseClassBody(name);
        }

        private ClassDecl ParseClassBody(string name)
        {
            var cd = ParseClassBodyCore(name);
            Expect(TokenType.SEMICOLON);
            return cd;
        }

        /// <summary>
        /// 类/结构体的成员体 <c>{ … }</c>，**读到 `}` 为止、不吞尾随的 `;`**。
        ///
        /// 分出这一层是因为 `typedef struct Tag { … } Alias;` —— 那里 `}` 后面跟的是
        /// **别名**而不是分号，用 <see cref="ParseClassBody"/> 会在别名上撞 `Expect(SEMICOLON)`。
        /// </summary>
        private ClassDecl ParseClassBodyCore(string name)
        {
            string? baseClass = null;
            if (Match(TokenType.COLON))
            {
                if (Match(TokenType.PUBLIC, TokenType.PRIVATE, TokenType.PROTECTED)) { }
                if (Check(TokenType.IDENTIFIER)) baseClass = Cur.Value;
                SkipTo(TokenType.LBRACE);
            }
            Expect(TokenType.LBRACE, "Class");
            var cd = new ClassDecl { Name = name, BaseClass = baseClass };
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                if (Match(TokenType.PUBLIC)) { cd.CurrentAccess = AccessSpec.Public; continue; }
                if (Match(TokenType.PRIVATE)) { cd.CurrentAccess = AccessSpec.Private; continue; }
                if (Match(TokenType.PROTECTED)) { cd.CurrentAccess = AccessSpec.Protected; continue; }
                var member = ParseClassMember(cd.CurrentAccess);
                if (member != null) { cd.Members.Add(member); cd.Members.AddRange(_pendingMembers); _pendingMembers.Clear(); }
                else Advance();
            }
            Expect(TokenType.RBRACE);
            return cd;
        }

        private ClassMember? ParseClassMember(AccessSpec access, bool isVirtual = false, bool isStatic = false)
        {
            if (Match(TokenType.TEMPLATE)) { SkipTemplate(); return null; }
            if (Check(TokenType.IDENTIFIER) && Cur.Value == "virtual")
            { Advance(); return ParseClassMember(access, true, isStatic); }
            if (Check(TokenType.IDENTIFIER) && Cur.Value == "static")
            { Advance(); return ParseClassMember(access, isVirtual, true); }
            // 调用约定属性
            CallingConvention? pendingConvention = null;
            if (Check(TokenType.STDCALL)) { Advance(); pendingConvention = CallingConvention.Stdcall; }
            else if (Check(TokenType.FASTCALL)) { Advance(); pendingConvention = CallingConvention.Fastcall; }
            else if (Check(TokenType.CDECL)) { Advance(); pendingConvention = CallingConvention.Cdecl; }
            // friend 声明：跳过，返回 null（非成员声明不计入类成员）
            if (Match(TokenType.FRIEND)) { ParseFriendDeclaration(); return null; }

            if (!IsTypeToken() && !Check(TokenType.IDENTIFIER) && !Check(TokenType.TILDE)) return null;
            bool isDestructor = Match(TokenType.TILDE);
            bool isType = IsTypeToken();
            string type = "";
            bool isCtor = false;
            if (isDestructor) type = "void";
            else if (isType) type = ParseType();
            else if (Check(TokenType.IDENTIFIER))
            {
                type = Cur.Value; Advance();
            }
            string name;
            if (Match(TokenType.OPERATOR))
            {
                // 运算符重载: ReturnType operator+(params)
                name = "operator" + ParseOperatorSymbol();
                isCtor = false;
            }
            else if (Check(TokenType.LPAREN))
            {
                // Constructor: the "type" is actually the class name
                name = type;
                type = "";
                isCtor = true;
            }
            else
            {
                name = Expect(TokenType.IDENTIFIER).Value;
                isCtor = false;
            }
            if (Match(TokenType.LPAREN))
            {
                var func = new FunctionDecl { Name = name, ReturnType = type, IsMember = true, IsVirtual = isVirtual };
                if (pendingConvention.HasValue) func.Convention = pendingConvention.Value;
                if (IsVoidOnlyParamList()) Advance(); // `f(void)`
                else if (!Check(TokenType.RPAREN))
                {
                    do
                    {
                        if (Match(TokenType.ELLIPSIS)) break; // 可变参数 `...`
                        string pt = ParseType();
                        bool isRef = Match(TokenType.AMPERSAND);
                        if (isRef) pt += "&";
                        string pn = Expect(TokenType.IDENTIFIER).Value;
                        // 处理数组参数: argv[] → *argv
                        while (Match(TokenType.LBRACKET))
                        {
                            pt += "*";
                            if (!Check(TokenType.RBRACKET)) SkipTo(TokenType.RBRACKET);
                            Expect(TokenType.RBRACKET);
                        }
                    func.Parameters.Add(new Parameter { Type = pt, Name = pn, IsReference = isRef });
                    } while (Match(TokenType.COMMA));
                }
                Expect(TokenType.RPAREN);
                if (Match(TokenType.CONST)) func.IsConst = true;
                if (Match(TokenType.OVERRIDE)) func.IsOverride = true;
                // 构造函数初始化列表: : member1(val1), member2(val2)
                if (Match(TokenType.COLON))
                {
                    while (!Check(TokenType.LBRACE) && !IsAtEnd)
                    {
                        if (Check(TokenType.IDENTIFIER))
                        {
                            string memberName = Advance().Value;
                            if (Match(TokenType.LPAREN))
                            {
                                var val = ParseExpression();
                                if (Check(TokenType.RPAREN)) Advance();
                                func.InitList.Add(new InitEntry { MemberName = memberName, Value = val });
                            }
                            if (!Match(TokenType.COMMA)) break;
                        }
                        else break;
                    }
                }
                // Check for = 0 (pure virtual)
                if (Match(TokenType.ASSIGN))
                {
                    Expect(TokenType.NUMBER); // skip 0
                    Expect(TokenType.SEMICOLON);
                }
                else if (Match(TokenType.LBRACE))
                {
                    func.Body = ParseBlock();
                }
                else if (Check(TokenType.SEMICOLON))
                {
                    Expect(TokenType.SEMICOLON);
                }
                else
                {
                    // 容错: 跳过无法识别的标记到 ; 或 {
                    while (!Check(TokenType.SEMICOLON) && !Check(TokenType.LBRACE) && !IsAtEnd)
                        Advance();
                    if (Match(TokenType.LBRACE))
                        func.Body = ParseBlock();
                    else if (Check(TokenType.SEMICOLON))
                        Advance();
                }
                if (isDestructor) return new ClassMember { Access = access, Name = "~" + name, IsDestructor = true, Method = func, IsStatic = isStatic };
                if (isCtor || name == type || (isVirtual && isDestructor))
                    return new ClassMember { Access = access, Name = name, IsConstructor = true, Method = func, IsVirtual = isVirtual, IsStatic = isStatic };
                return new ClassMember { Access = access, Name = name, IsMethod = true, Method = func, IsVirtual = isVirtual, IsStatic = isStatic };
            }
            Expr? init = null;
            if (Match(TokenType.ASSIGN)) init = ParseExpression();
            
            // 处理逗号分隔的多个成员: int x, y;
            if (Match(TokenType.COMMA))
            {
                // 第一个成员正常返回, 额外成员通过字段传递给调用方
                // 这里解析额外的名称
                string nextName = Expect(TokenType.IDENTIFIER).Value;
                Expr? nextInit = null;
                if (Match(TokenType.ASSIGN)) nextInit = ParseExpression();
                _pendingMembers.Add(new ClassMember { Access = access, Type = type, Name = nextName, Initializer = nextInit, IsStatic = isStatic });
                // 可能还有更多
                while (Match(TokenType.COMMA))
                {
                    string an = Expect(TokenType.IDENTIFIER).Value;
                    Expr? ai = null;
                    if (Match(TokenType.ASSIGN)) ai = ParseExpression();
                    _pendingMembers.Add(new ClassMember { Access = access, Type = type, Name = an, Initializer = ai, IsStatic = isStatic });
                }
            }
            
            Expect(TokenType.SEMICOLON);
            return new ClassMember { Access = access, Type = type, Name = name, Initializer = init, IsStatic = isStatic };
        }

        // Simplified: parse function or variable
        private ASTNode? ParseFunctionOrVariable()
        {
            string type = ParseType();
            // 处理引用: int& name
            bool isRef = Match(TokenType.AMPERSAND);
            string name;
            if (Match(TokenType.OPERATOR))
            {
                // 运算符重载: operator+, operator-, operator* 等
                name = "operator" + ParseOperatorSymbol();
                if (Match(TokenType.LPAREN)) return ParseFunctionBody(type, name);
            }
            else if (Check(TokenType.IDENTIFIER))
                name = Advance().Value;
            else
                name = Expect(TokenType.IDENTIFIER).Value;
            if (Match(TokenType.LPAREN)) return ParseFunctionBody(type, name);
            // Parse comma-separated variables: int a, b = 5, c;
            var vars = new List<VariableDecl>();
            vars.Add(ParseVariableDeclarator(type, name, isRef));
            while (Match(TokenType.COMMA))
            {
                bool ref2 = Match(TokenType.AMPERSAND);
                string n = Expect(TokenType.IDENTIFIER).Value;
                vars.Add(ParseVariableDeclarator(type, n, ref2));
            }
            Expect(TokenType.SEMICOLON);
            return vars.Count == 1 ? vars[0] : new MultiVarDecl { Variables = vars };
        }

        private VariableDecl ParseVariableDeclarator(string type, string name, bool isReference = false)
        {
            var dimensions = new List<Expr?>();
            bool isArray = false;
            while (Match(TokenType.LBRACKET))
            {
                isArray = true;
                if (!Check(TokenType.RBRACKET)) dimensions.Add(ParseExpression());
                else dimensions.Add(null);
                Expect(TokenType.RBRACKET);
            }
            Expr? arraySize = null;
            if (isArray && dimensions.Count > 0 && dimensions[0] != null)
                arraySize = new IntLiteral { Value = ComputeTotalElements(dimensions) };
            Expr? init = null;
            if (Match(TokenType.ASSIGN))
            {
                if (Check(TokenType.LBRACE)) init = ParseInitializerList();
                else init = ParseExpression();
            }
            return new VariableDecl { Type = type, Name = name, Initializer = init, ArraySize = arraySize, IsArray = isArray, Dimensions = dimensions, IsReference = isReference };
        }

        private int ComputeTotalElements(List<Expr?> dims)
        {
            int total = 1;
            foreach (var d in dims)
            {
                if (d is IntLiteral il) total *= il.Value;
                else total *= 1;
            }
            return total;
        }

        private ASTNode ParseFunction(string? knownType = null, string? knownName = null)
        {
            string type = knownType ?? ParseType();
            string name = knownName ?? Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.LPAREN);
            return ParseFunctionBody(type, name);
        }

        /// <summary>
        /// `f(void)` —— C 风格「无参数」写法，等价于空参数列表。
        ///
        /// <para>判据收得很紧：<b>`void` 后面紧跟 `)`</b>。这样 `void f(void* p)`、
        /// `void f(void v)`、`void f(void*, int)` 仍是正常参数，一个都不受影响 ——
        /// 只有「括号里孤零零一个 void」才被当成空参数表。</para>
        ///
        /// <para>为什么必须支持：`Lib/c/stdio.h` 自己就写着 `int getchar(void);`
        /// （第 31 行）与 `FILE *tmpfile(void);`，所以任何 `#include <stdio.h>` 的 C++
        /// 程序在预处理后必定碰到这个形态 —— 不认它就等于「C++ 用不了 C 标准头」。</para>
        /// </summary>
        private bool IsVoidOnlyParamList()
            => Check(TokenType.VOID)
               && _pos + 1 < _tokens.Count
               && GetTokenType(_tokens[_pos + 1]) == TokenType.RPAREN;

        private FunctionDecl ParseFunctionBody(string type, string name)
        {
            var func = new FunctionDecl { Name = name, ReturnType = type };
            if (IsVoidOnlyParamList())
            {
                Advance(); // 吃掉 `void` —— 参数列表为空
            }
            else if (!Check(TokenType.RPAREN))
            {
                do
                {
                    // 可变参数 `...` —— 到此为止，后面不再有具名参数（C 语义）。
                    if (Match(TokenType.ELLIPSIS)) break;
                    string pt = ParseType();
                    bool isRef = Match(TokenType.AMPERSAND);
                    if (isRef) pt += "&";
                    while (Match(TokenType.RESTRICT)) { pt += " restrict"; }
                    string pn;
                    // Function pointer parameter: type (*name)(params)
                    if (Match(TokenType.LPAREN) && Match(TokenType.STAR))
                    {
                        pn = Expect(TokenType.IDENTIFIER).Value;
                        Expect(TokenType.RPAREN);
                        pt += "*";
                        // Skip the function pointer's parameter list (params)
                        if (Match(TokenType.LPAREN))
                        {
                            int depth = 1;
                            while (depth > 0 && !IsAtEnd)
                            {
                                if (Match(TokenType.LPAREN)) depth++;
                                else if (Match(TokenType.RPAREN)) depth--;
                                else Advance();
                            }
                        }
                    }
                    else
                    {
                        pn = Expect(TokenType.IDENTIFIER).Value;
                        // 处理数组参数: argv[] → *argv
                        while (Match(TokenType.LBRACKET))
                        {
                            pt += "*";
                            if (!Check(TokenType.RBRACKET)) SkipTo(TokenType.RBRACKET);
                            Expect(TokenType.RBRACKET);
                        }
                    }
                    func.Parameters.Add(new Parameter { Type = pt, Name = pn, IsReference = isRef });
                } while (Match(TokenType.COMMA));
            }
            Expect(TokenType.RPAREN);
            // 构造函数初始化列表: : member1(val1), member2(val2)
            if (Match(TokenType.COLON))
            {
                while (!Check(TokenType.LBRACE) && !IsAtEnd)
                {
                    string memberName = Expect(TokenType.IDENTIFIER).Value;
                    Expect(TokenType.LPAREN);
                    var val = ParseExpression();
                    Expect(TokenType.RPAREN);
                    func.InitList.Add(new InitEntry { MemberName = memberName, Value = val });
                    if (!Match(TokenType.COMMA)) break;
                }
            }
            if (Match(TokenType.LBRACE))
            {
                func.Body = ParseBlock();
            }
            else { Expect(TokenType.SEMICOLON); }
            return func;
        }

        private ASTNode ParseEnum()
        {
            string name = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.LBRACE, "Enum");
            var ed = new EnumDecl { Name = name };
            int val = 0;
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                string mname = Expect(TokenType.IDENTIFIER).Value;
                if (Match(TokenType.ASSIGN))
                {
                    if (GetTokenType(Cur) == TokenType.NUMBER)
                        val = int.Parse(Advance().Value);
                    else if (GetTokenType(Cur) == TokenType.MINUS)
                    {
                        Advance();
                        val = -int.Parse(Expect(TokenType.NUMBER).Value);
                    }
                }
                ed.Members.Add(new EnumMember { Name = mname, Value = val });
                val++;
                if (!Match(TokenType.COMMA) && !Check(TokenType.RBRACE))
                    break;
            }
            Expect(TokenType.RBRACE);
            Expect(TokenType.SEMICOLON);
            return ed;
        }

        private ASTNode ParseUnion()
        {
            string name = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.LBRACE, "Union");
            var ud = new UnionDecl { Name = name };
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                string mtype = ParseType();
                do
                {
                    string mname = Expect(TokenType.IDENTIFIER).Value;
                    Expr? init = null;
                    if (Match(TokenType.ASSIGN))
                        init = ParseExpression();
                    ud.Members.Add(new ClassMember
                    {
                        Type = mtype,
                        Name = mname,
                        Initializer = init,
                        Access = AccessSpec.Public
                    });
                } while (Match(TokenType.COMMA));
                Expect(TokenType.SEMICOLON);
            }
            Expect(TokenType.RBRACE);
            Expect(TokenType.SEMICOLON);
            return ud;
        }

        /// Parse template declaration: template<typename T, ...> class/struct Name { ... };
        private ASTNode? ParseTemplateDeclaration()
        {
            // Parse template parameters: <typename T, class U, ...>
            var typeParams = new List<string>();
            if (Match(TokenType.LT))
            {
                while (!Check(TokenType.GT) && !IsAtEnd)
                {
                    if (Match(TokenType.TYPENAME, TokenType.CLASS))
                    {
                        if (Check(TokenType.IDENTIFIER))
                            typeParams.Add(Advance().Value);
                    }
                    else if (Match(TokenType.IDENTIFIER))
                    {
                        typeParams.Add(Previous().Value);
                    }
                    if (!Match(TokenType.COMMA)) break;
                }
                Expect(TokenType.GT, "TemplateDeclaration: expected '>' after template parameters");
            }
            else if (Match(TokenType.TYPENAME, TokenType.CLASS))
            {
                if (Check(TokenType.IDENTIFIER))
                    typeParams.Add(Advance().Value);
            }

            // Parse the class/struct/function that follows
            if (Match(TokenType.CLASS, TokenType.STRUCT))
            {
                string name = Expect(TokenType.IDENTIFIER).Value;
                string? baseClass = null;
                if (Match(TokenType.COLON))
                {
                    if (Match(TokenType.PUBLIC, TokenType.PRIVATE, TokenType.PROTECTED)) { }
                    if (Check(TokenType.IDENTIFIER)) baseClass = Cur.Value;
                    SkipTo(TokenType.LBRACE);
                }
                Expect(TokenType.LBRACE, "TemplateClass");
                var tcd = new TemplateClassDecl { Name = name, BaseClass = baseClass };
                tcd.TypeParams.AddRange(typeParams);

                while (!Check(TokenType.RBRACE) && !IsAtEnd)
                {
                    if (Match(TokenType.PUBLIC)) { continue; }
                    if (Match(TokenType.PRIVATE)) { continue; }
                    if (Match(TokenType.PROTECTED)) { continue; }
                    var member = ParseTemplateClassMember(typeParams);
                    if (member != null) { tcd.Members.Add(member); tcd.Members.AddRange(_pendingMembers); _pendingMembers.Clear(); }
                    else Advance();
                }
                Expect(TokenType.RBRACE);
                Expect(TokenType.SEMICOLON);
                return tcd;
            }

            // Template function: template<typename T, ...> R f(params) { ... }
            if (IsTypeToken() || Check(TokenType.IDENTIFIER) || Check(TokenType.AUTO))
            {
                // 返回类型可能是模板参数 T 或 auto（自动推导）
                string returnType;
                if (Check(TokenType.AUTO))
                {
                    Advance();
                    returnType = "auto";
                }
                else if (Check(TokenType.IDENTIFIER) && typeParams.Contains(Cur.Value))
                {
                    returnType = Advance().Value; // 模板参数类型: T
                }
                else
                {
                    returnType = ParseType();
                }

                // 函数名（可能是模板参数返回类型 T 后直接跟函数名）
                string name = Expect(TokenType.IDENTIFIER).Value;
                Expect(TokenType.LPAREN, "TemplateFunction: expected '('");
                var tfd = new TemplateFunctionDecl { Name = name, ReturnType = returnType };
                tfd.TypeParams.AddRange(typeParams);

                // 解析参数列表（参数类型可能是模板参数 T）
                if (IsVoidOnlyParamList()) Advance(); // `f(void)`
                else if (!Check(TokenType.RPAREN))
                {
                    do
                    {
                        if (Match(TokenType.ELLIPSIS)) break; // 可变参数 `...`
                        string pt;
                        if (Check(TokenType.IDENTIFIER) && typeParams.Contains(Cur.Value))
                            pt = Advance().Value;
                        else if (IsTypeToken())
                            pt = ParseType();
                        else if (Check(TokenType.IDENTIFIER))
                            pt = ParseType();
                        else
                            pt = "int";
                        bool isRef = Match(TokenType.AMPERSAND);
                        if (isRef) pt += "&";
                        while (Match(TokenType.STAR)) pt += "*";
                        string pn = Expect(TokenType.IDENTIFIER).Value;
                        tfd.Parameters.Add(new Parameter { Type = pt, Name = pn, IsReference = isRef });
                    } while (Match(TokenType.COMMA));
                }
                Expect(TokenType.RPAREN, "TemplateFunction: expected ')'");

                // 函数体
                if (Match(TokenType.LBRACE))
                {
                    tfd.Body = ParseBlock();
                }
                else if (Match(TokenType.SEMICOLON))
                {
                    // 仅声明，无定义
                    return null;
                }
                else
                {
                    Expect(TokenType.LBRACE, "TemplateFunction: expected '{'");
                }
                return tfd;
            }

            return null;
        }

        /// Parse a class member within a template class, where type params like T/U are valid types
        private ClassMember? ParseTemplateClassMember(List<string> typeParams)
        {
            if (Check(TokenType.IDENTIFIER) && Cur.Value == "virtual")
            { Advance(); }
            if (Check(TokenType.IDENTIFIER) && Cur.Value == "static")
            { Advance(); }

            if (!IsTypeToken() && !IsTemplateParam(typeParams) && !Check(TokenType.TILDE) && !Check(TokenType.IDENTIFIER)) return null;

            bool isDestructor = Match(TokenType.TILDE);
            string type = "";
            if (isDestructor) type = "void";
            else if (IsTypeToken() || IsTemplateParam(typeParams)) type = ParseTypeOrTemplateParam(typeParams);
            else if (Check(TokenType.IDENTIFIER))
            {
                string id = Cur.Value;
                if (typeParams.Contains(id)) type = Advance().Value;
                else type = ParseType();
            }

            string name;
            if (Check(TokenType.LPAREN))
            {
                name = type; // Constructor
                type = "";
            }
            else
            {
                name = Expect(TokenType.IDENTIFIER).Value;
            }

            if (Match(TokenType.LPAREN))
            {
                var func = new FunctionDecl { Name = name, ReturnType = type, IsMember = true };
                if (IsVoidOnlyParamList()) Advance(); // `f(void)`
                else if (!Check(TokenType.RPAREN))
                {
                    do
                    {
                        if (Match(TokenType.ELLIPSIS)) break; // 可变参数 `...`
                        string pt;
                        if (IsTypeToken() || IsTemplateParam(typeParams))
                            pt = ParseTypeOrTemplateParam(typeParams);
                        else
                            pt = ParseType();
                        bool isRef = Match(TokenType.AMPERSAND);
                        if (isRef) pt += "&";
                        string pn = Expect(TokenType.IDENTIFIER).Value;
                        func.Parameters.Add(new Parameter { Type = pt, Name = pn, IsReference = isRef });
                    } while (Match(TokenType.COMMA));
                }
                Expect(TokenType.RPAREN);
                if (Match(TokenType.CONST)) func.IsConst = true;
                if (Match(TokenType.OVERRIDE)) func.IsOverride = true;
                // 构造函数初始化列表: : member1(val1), member2(val2)
                if (Match(TokenType.COLON))
                {
                    while (!Check(TokenType.LBRACE) && !IsAtEnd)
                    {
                        if (Check(TokenType.IDENTIFIER))
                        {
                            string memberName = Advance().Value;
                            if (Match(TokenType.LPAREN))
                            {
                                ParseExpression();
                                if (Check(TokenType.RPAREN)) Advance();
                                func.InitList.Add(new InitEntry { MemberName = memberName, Value = null! });
                            }
                            if (!Match(TokenType.COMMA)) break;
                        }
                        else break;
                    }
                }
                if (Match(TokenType.LBRACE))
                    func.Body = ParseBlock();
                else
                    Expect(TokenType.SEMICOLON);

                if (isDestructor)
                    return new ClassMember { Access = AccessSpec.Private, Name = "~" + name, IsDestructor = true, Method = func };
                return new ClassMember { Access = AccessSpec.Private, Name = name, IsMethod = true, Method = func };
            }

            // Data member
            Expr? init = null;
            if (Match(TokenType.ASSIGN)) init = ParseExpression();
            if (Match(TokenType.COMMA))
            {
                string nextName = Expect(TokenType.IDENTIFIER).Value;
                Expr? nextInit = null;
                if (Match(TokenType.ASSIGN)) nextInit = ParseExpression();
                _pendingMembers.Add(new ClassMember { Access = AccessSpec.Private, Type = type, Name = nextName, Initializer = nextInit });
                while (Match(TokenType.COMMA))
                {
                    string an = Expect(TokenType.IDENTIFIER).Value;
                    Expr? ai = null;
                    if (Match(TokenType.ASSIGN)) ai = ParseExpression();
                    _pendingMembers.Add(new ClassMember { Access = AccessSpec.Private, Type = type, Name = an, Initializer = ai });
                }
            }
            Expect(TokenType.SEMICOLON);
            return new ClassMember { Access = AccessSpec.Private, Type = type, Name = name, Initializer = init };
        }

        private bool IsTemplateParam(List<string> typeParams)
        {
            return Check(TokenType.IDENTIFIER) && typeParams.Contains(Cur.Value);
        }

        private string ParseTypeOrTemplateParam(List<string> typeParams)
        {
            if (IsTemplateParam(typeParams))
                return Advance().Value;
            return ParseType();
        }

        // Statements
        /// <summary>
        /// 语句入口 —— **顺手给每条语句盖上起始行列**（`ASTNode.Line`/`Column`）。
        ///
        /// 与 C/Rust 同一套路：这个方法是**单一入口**，包一层就覆盖了全部 return
        ///（含递归进来的嵌套语句）。用 `Line == 0` 才盖 ——
        /// 子解析器自己填过的更精确位置不会被外层冲掉。
        ///
        /// ⚠ 加这个之前 `ASTNode` 是个**空基类**，一个位置字段都没有 ⇒
        ///   代码生成侧那句 `if (node.Line > 0) …` 永远不生效，报错只能给个名字。
        /// </summary>
        public Stmt ParseStatement()
        {
            int __line = Cur.Line, __col = Cur.Column;
            // 起点同时记下**原文件**行/文件：`Line` 是拼接流的行（产物里 `; N:` 注释按它索引
            // `SourceLines`），`OriginalLine`/`OriginalFile` 才是报给用户看的（与 C 前端同分工）。
            var (__file, __originLine) = MapOriginal(__line);
            var __node = ParseStatementCore();
            if (__node != null && __node.Line == 0)
            {
                __node.Line = __line;
                __node.Column = __col;
                __node.OriginalLine = __originLine;
                __node.OriginalFile = __file;
            }
            return __node;
        }

        public Stmt ParseStatementCore()
        {
            // label: identifier followed by colon
            if (GetTokenType(Cur) == TokenType.IDENTIFIER)
            {
                int nextPos = _pos + 1;
                if (nextPos < _tokens.Count && _tokens[nextPos].Type == TokenType.COLON)
                {
                    string label = Advance().Value;
                    Expect(TokenType.COLON);
                    return new LabelStmt { Name = label };
                }
            }
            if (Match(TokenType.LBRACE)) return ParseBlock();
            if (Match(TokenType.IF)) return ParseIf();
            if (Match(TokenType.WHILE)) return ParseWhile();
            if (Match(TokenType.GOTO))
            {
                string target = Expect(TokenType.IDENTIFIER).Value;
                Expect(TokenType.SEMICOLON);
                return new GotoStmt { Target = target };
            }
            if (Match(TokenType.FOR)) return ParseFor();
            if (Match(TokenType.DO)) return ParseDoWhile();
            if (Match(TokenType.SWITCH)) return ParseSwitch();
            if (Match(TokenType.RETURN)) return ParseReturn();
            if (Match(TokenType.BREAK)) { Expect(TokenType.SEMICOLON); return new BreakStmt(); }
            if (Match(TokenType.CONTINUE)) { Expect(TokenType.SEMICOLON); return new ContinueStmt(); }
            if (Match(TokenType.TRY))
            {
                if (_isMCU) WarningEmitter.Emit("cpp", "MCU模式: try/catch异常处理被忽略（不支持异常）");
                Expect(TokenType.LBRACE, "expected '{' after try");
                var body = ParseBlock();
                var ts = new TryStmt { Body = body };
                while (Match(TokenType.CATCH))
                {
                    var cc = new CatchClause();
                    if (Match(TokenType.LPAREN))
                    {
                        if (Check(TokenType.IDENTIFIER) || IsTypeToken())
                        {
                            string ct = ParseType();
                            cc.ExceptionType = ct;
                            if (Check(TokenType.IDENTIFIER) && Cur.Value != ")")
                                cc.VariableName = Expect(TokenType.IDENTIFIER).Value;
                        }
                        Expect(TokenType.RPAREN);
                    }
                    Expect(TokenType.LBRACE, "expected '{' after catch");
                    cc.Body = ParseBlock();
                    ts.Catches.Add(cc);
                }
                return ts;
            }
            if (Match(TokenType.THROW))
            {
                if (_isMCU) WarningEmitter.Emit("cpp", "MCU模式: throw被忽略（不支持异常）");
                Expr? val = null;
                if (!Check(TokenType.SEMICOLON)) val = ParseExpression();
                Expect(TokenType.SEMICOLON);
                return new ThrowStmt { Expression = val };
            }
            if (Match(TokenType.ASM))
            {
                Expect(TokenType.LPAREN, "expected '(' after asm");
                string code = Expect(TokenType.STRING).Value;
                Expect(TokenType.RPAREN, "expected ')'");
                Expect(TokenType.SEMICOLON, "expected ';'");
                return new AsmStmt { Code = code };
            }
            // constexpr variable declaration
            if (Match(TokenType.CONSTEXPR)) { return ParseVarDeclStmt(); }
            // Structured binding: auto [x, y] = expr;
            if (Match(TokenType.AUTO) && Check(TokenType.LBRACKET))
            {
                Expect(TokenType.LBRACKET);
                var names = new List<string>();
                do { names.Add(Expect(TokenType.IDENTIFIER).Value); } while (Match(TokenType.COMMA));
                Expect(TokenType.RBRACKET);
                Expect(TokenType.ASSIGN);
                var expr = ParseExpression();
                Expect(TokenType.SEMICOLON);
                // For now, assign whole value to first variable
                if (names.Count > 0)
                {
                    var assign = new AssignExpr { Target = new IdentExpr { Name = names[0] }, Value = expr };
                    return new ExprStmt { Expression = assign };
                }
                return null;
            }
            if (IsVarDecl()) return ParseVarDeclStmt();
            return ParseExprStmt();
        }

        private bool IsVarDecl()
        {
            if (IsTypeToken()) return true;
            if (Check(TokenType.IDENTIFIER))
            {
                int save = _pos;
                Advance();
                // Handle namespace-qualified types: std::vector<int> v;
                if (Check(TokenType.SCOPE_RESOLVE))
                {
                    Advance(); // consume ::
                    if (Check(TokenType.IDENTIFIER))
                    {
                        Advance(); // consume inner identifier
                        // Skip template args if present
                        if (Check(TokenType.LT))
                        {
                            int depth = 1;
                            Advance(); // consume <
                            while (depth > 0 && !IsAtEnd)
                            {
                                if (Match(TokenType.LT)) depth++;
                                else if (Match(TokenType.GT)) depth--;
                                else if (Match(TokenType.RSHIFT)) { depth -= 2; if (depth <= 0) break; }
                                else Advance();
                            }
                        }
                        bool result = Check(TokenType.IDENTIFIER) || Check(TokenType.STAR) || Check(TokenType.AMPERSAND);
                        _pos = save;
                        return result;
                    }
                    _pos = save;
                    return false;
                }
                // Handle template type without namespace: Container<int> v;
                if (Check(TokenType.LT))
                {
                    int depth = 1;
                    Advance(); // consume <
                    while (depth > 0 && !IsAtEnd)
                    {
                        if (Match(TokenType.LT)) depth++;
                        else if (Match(TokenType.GT)) depth--;
                        else if (Match(TokenType.RSHIFT)) { depth -= 2; if (depth <= 0) break; }
                        else Advance();
                    }
                    bool result = Check(TokenType.IDENTIFIER) || Check(TokenType.STAR) || Check(TokenType.AMPERSAND);
                    _pos = save;
                    return result;
                }
                bool simpleResult = Check(TokenType.IDENTIFIER) || Check(TokenType.STAR) || Check(TokenType.AMPERSAND);
                _pos = save;
                return simpleResult;
            }
            return false;
        }

        public BlockStmt ParseBlock()
        {
            var block = new BlockStmt();
            while (!Check(TokenType.RBRACE) && !IsAtEnd)
            {
                var s = ParseStatement();
                if (s != null) block.Statements.Add(s);
            }
            Expect(TokenType.RBRACE);
            return block;
        }

        private Stmt ParseVarDeclStmt()
        {
            string type = ParseType();
            return ParseVarDeclList(type);
        }
        private void SkipTo(params TokenType[] types)
        {
            while (!Check(types) && !IsAtEnd) Advance();
        }

        // Helpers
        private new bool Match(params TokenType[] types)
        {
            foreach (var t in types)
            {
                if (GetTokenType(Cur) == t) { _pos++; return true; }
            }
            return false;
        }

        private new bool Check(params TokenType[] types) => types.Contains(GetTokenType(Cur));

        /// <summary>
        /// 期望当前 token 为指定类型，否则报错。
        ///
        /// 报文用 **GCC 形态**（<c>原文件:原行:列: error: …</c>），不用从前那句
        /// `… at line 112:`：后者既没有文件名、行号还是**拼接后**的（`#include` 一展开就整体后移），
        /// 于是用户看到的是一句「指着他文件里另一行」的报错。
        /// </summary>
        private new Token Expect(TokenType type, string msg = "")
        {
            if (GetTokenType(Cur) != type)
            {
                var detail = string.IsNullOrWhiteSpace(msg) ? "" : $" ({msg})";
                throw Error($"{Where(Cur)}error: Expected {type} but got {GetTokenType(Cur)} ('{Cur.Value}'){detail}");
            }
            return Advance();
        }

        private string ParseOperatorSymbol()
        {
            if (Match(TokenType.PLUS)) return "+";
            if (Match(TokenType.MINUS)) return "-";
            if (Match(TokenType.STAR)) return "*";
            if (Match(TokenType.SLASH)) return "/";
            if (Match(TokenType.PERCENT)) return "%";
            if (Match(TokenType.ASSIGN)) return "=";
            if (Match(TokenType.EQ)) return "==";
            if (Match(TokenType.NE)) return "!=";
            if (Match(TokenType.LT)) return "<";
            if (Match(TokenType.GT)) return ">";
            if (Match(TokenType.LE)) return "<=";
            if (Match(TokenType.GE)) return ">=";
            if (Match(TokenType.AND)) return "&&";
            if (Match(TokenType.OR)) return "||";
            if (Match(TokenType.NOT)) return "!";
            if (Match(TokenType.AMPERSAND)) return "&";
            if (Match(TokenType.PIPE)) return "|";
            if (Match(TokenType.CARET)) return "^";
            if (Match(TokenType.INCREMENT)) return "++";
            if (Match(TokenType.DECREMENT)) return "--";
            if (Match(TokenType.LSHIFT)) return "<<";
            if (Match(TokenType.RSHIFT)) return ">>";
            if (Match(TokenType.LSHIFT_ASSIGN)) return "<<=";
            if (Match(TokenType.RSHIFT_ASSIGN)) return ">>=";
            if (Match(TokenType.ADD_ASSIGN)) return "+=";
            if (Match(TokenType.SUB_ASSIGN)) return "-=";
            if (Match(TokenType.MUL_ASSIGN)) return "*=";
            if (Match(TokenType.DIV_ASSIGN)) return "/=";
            if (Match(TokenType.MOD_ASSIGN)) return "%=";
            if (Match(TokenType.AND_ASSIGN)) return "&=";
            if (Match(TokenType.OR_ASSIGN)) return "|=";
            if (Match(TokenType.XOR_ASSIGN)) return "^=";
            if (Match(TokenType.LBRACKET) && Match(TokenType.RBRACKET)) return "[]";
            if (Match(TokenType.LPAREN) && Match(TokenType.RPAREN)) return "()";
            if (Match(TokenType.COMMA)) return ",";
            if (Match(TokenType.ARROW)) return "->";
            if (Match(TokenType.NEW)) return "new";
            if (Match(TokenType.DELETE)) return "delete";
            return Advance().Value;
        }

        private new Token Advance()
        {
            var t = Cur;
            _pos++;
            return t;
        }

        private new Token Previous() => _tokens[_pos - 1];

        private new bool Match(TokenType type)
        {
            if (GetTokenType(Cur) == type) { Advance(); return true; }
            return false;
        }
    }
}
