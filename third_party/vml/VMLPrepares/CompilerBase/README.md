# CompilerBase — 编译器公共基础库

> **v1.66.19** | 22 种语言编译器共享

## 目录结构
```
CompilerBase/
├── LexerBase.cs              # 词法分析器基类 (Peek/Advance/ReadEscape/Error)
├── LexerHelper.cs            # 词法分析辅助 (IsHexDigit/IsOctalDigit/IsBinaryDigit/IsChineseChar)
├── ParserBase.cs             # 泛型语法分析器基类 (Peek/Advance/Match/Check/Expect)
├── CodeGeneratorBase.cs      # 代码生成器基类 (Emit/AddLabel/EmitExit/EmitPrologue)
├── CLikeCodegen.cs           # C/Go 风格代码生成器
├── OopCodeGenerator.cs       # OOP 代码生成器 (Java/C#/Swift/Dart/Kotlin/Ruby)
├── TypedCodeGen.cs           # 类型敏感指令选择中间基类
├── ExpressionManager.cs      # 表达式代码生成管理器
├── StatementManager.cs       # 控制流语句管理器 (EmitIf/EmitWhile/EmitFor)
├── VarMemManager.cs          # 变量内存分配管理器
├── RegisterManager.cs        # 寄存器分配管理器
├── AstNodes.cs               # 跨语言共享 AST 节点
├── Preprocessor.cs           # C 预处理器
├── CompilerHelper.cs         # 编译器通用辅助 (InjectDefines/PreprocessSource/CreatePredefinedMacros)
├── CompilerBase.cs           # 编译器插件抽象基类
├── CompilerPluginBase.cs     # 实例类架构插件基类
├── CompilerPluginExBase.cs   # 静态类架构插件基类
├── CompilerConfig.cs         # 统一编译配置
├── CompilerException.cs      # 异常类型
├── CommonTokenType.cs        # 通用 Token 类型枚举
├── ExpVar.cs                 # 表达式变量类型系统
├── CompilerProgramBase.cs    # CLI 程序入口模板
└── README.md                 # 本文档
```

## 被引用于
全部 22 种语言编译器: C, Cpp, Basic, Pascal, Python, Lua, Forth, Rust, Go, Ladder, Java, JavaScript, Swift, CSharp, Kotlin, Scheme, Ruby, Dart, ObjC, R, D, Fortran
