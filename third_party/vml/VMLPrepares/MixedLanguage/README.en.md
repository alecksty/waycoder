# MixedLanguage — mixed-language programming system

## Description
The mixed-language programming system allows functions to be called across languages inside a single VML program. It currently supports calls between any of the 22 languages; each language is wrapped by an adapter and shares one unified calling convention.

## Directory structure
```
MixedLanguage/
├── MixedLanguage.csproj     # library project
├── MixedLanguageAdapter.cs  # mixed-language adapter base class
├── MixedLanguageManager.cs  # mixed-language manager
├── Adapters/                # per-language adapter implementations
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
├── Demo.csproj              # demo project
├── Demo.cs                  # mixed-language demo
└── README.md                # this document
```

## Supported language adapters
| Language | Adapter | Status |
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

## How it works
1. Every language implements the `ILanguageAdapter` interface
2. The adapter converts a cross-language call into the unified VML calling convention
3. Arguments are passed through the VML registers (R0-R3); extra arguments are pushed onto the stack
4. The return value is passed through R0
5. The manager coordinates symbol references between the multi-language modules

## Usage
```csharp
var manager = new MixedLanguageManager();
manager.RegisterAdapter("c", new CAdapter());
manager.RegisterAdapter("go", new GoAdapter());

var result = manager.CallFunction("c", "calculate", 42);
var output = manager.CallFunction("go", "formatResult", result);
```
