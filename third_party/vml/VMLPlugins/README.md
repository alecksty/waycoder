# VMLPlugins 子工程说明

## 功能描述
VMLTool 的插件框架，提供编译器前端和翻译器后端的插件接口与加载机制。所有 22 种语言的编译器及 16 种目标架构的翻译器均通过此框架以插件形式集成。

## 目录结构
```
VMLPlugins/
├── PluginManager.cs         # 插件加载与管理器
├── Interfaces/
│   ├── IFrontendCompiler.cs    # 编译器前端接口
│   └── IBackendTranslator.cs   # 翻译器后端接口
├── CompilerPluginAttribute.cs  # 编译器插件标记
├── VMLPlugins.csproj      # 项目文件
└── README.md                   # 本文档
```

## 核心接口

### IFrontendCompiler
编译器前端插件必须实现的接口:
- `Name` — 编译器名称（如 "C", "BASIC"）
- `Description` — 编译器描述
- `SupportedExtensions` — 支持的文件扩展名列表
- `CompileFile(string path)` — 编译源文件为 VML 程序
- `CompileCode(string source)` — 编译源代码字符串

### IBackendTranslator
翻译器后端插件必须实现的接口:
- `TargetArchitecture` — 目标架构名称（如 "x86", "6502"）
- `Description` — 翻译器描述
- `Translate(VmlProgram program)` — 将 VML 程序翻译为目标架构汇编

## 插件加载机制
1. 启动时扫描 `Plugins/` 目录及程序集所在目录
2. 通过反射查找标记了 `[CompilerPlugin]` 或 `[BackendPlugin]` 特性的类
3. 自动注册到插件管理器
4. 支持运行时按名称或文件扩展名查找编译器/翻译器

## 使用方式
```csharp
// 获取编译器
var compiler = PluginManager.GetFrontendCompiler("c");
var program = compiler.CompileFile("test.c");

// 获取翻译器
var translator = PluginManager.GetBackendTranslator("x86");
var result = translator.Translate(program);

// 自动检测编译器
var detected = PluginManager.GetCompilerByFileName("test.c");
```
