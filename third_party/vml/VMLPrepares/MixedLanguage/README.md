# MixedLanguage — 混合编程系统

## 功能描述
混合编程系统允许在同一个 VML 程序中跨语言调用函数。目前支持 22 种语言之间的互相调用，每种语言通过一个适配器（Adapter）封装，统一调用约定。

## 目录结构
```
MixedLanguage/
├── MixedLanguage.csproj     # 库项目
├── MixedLanguageAdapter.cs  # 混合语言适配器基类
├── MixedLanguageManager.cs  # 混合语言管理器
├── Adapters/                # 各语言适配器实现
│   ├── CAdapter.cs
│   ├── BasicAdapter.cs
│   ├── PascalAdapter.cs
│   ├── PythonAdapter.cs
│   ├── LuaAdapter.cs
│   ├── ForthAdapter.cs
│   ├── GoAdapter.cs
│   ├── RustAdapter.cs
│   ├── JavaAdapter.cs
│   └── JavaScriptAdapter.cs
├── Demo.csproj              # 演示项目
├── Demo.cs                  # 混合编程演示
└── README.md                # 本文档
```

## 支持的语言适配器
| 语言 | 适配器 | 状态 |
|------|--------|------|
| C | CAdapter | ✅ |
| BASIC | BasicAdapter | ✅ |
| Pascal | PascalAdapter | ✅ |
| Python | PythonAdapter | ✅ |
| Lua | LuaAdapter | ✅ |
| Forth | ForthAdapter | ✅ |
| Go | GoAdapter | ✅ |
| Rust | RustAdapter | ✅ |
| Java | JavaAdapter | ✅ |
| JavaScript | JavaScriptAdapter | ✅ |

## 工作原理
1. 每种语言实现 `ILanguageAdapter` 接口
2. 适配器负责将跨语言调用转换为统一的 VML 调用约定
3. 参数通过 VML 寄存器 (R0-R3) 传递，多余参数压栈
4. 返回值通过 R0 传递
5. 管理器负责协调多语言模块之间的符号引用

## 使用方式
```csharp
var manager = new MixedLanguageManager();
manager.RegisterAdapter("c", new CAdapter());
manager.RegisterAdapter("go", new GoAdapter());

var result = manager.CallFunction("c", "calculate", 42);
var output = manager.CallFunction("go", "formatResult", result);
```
