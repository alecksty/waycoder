# C# 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | C# 5.0 子集 (ECMA-334:2012) |
| **发布年份** | 2012 |
| **完成度** | ~92% |
| **MCU完成度** | ~90% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正目标标准版本、完成度和测试数 |

## 关键字

`abstract` `as` `base` `bool` `break` `byte` `case` `catch` `char` `checked` `class`
`const` `continue` `decimal` `default` `delegate` `do` `double` `else` `enum` `event`
`explicit` `extern` `false` `finally` `fixed` `float` `for` `foreach` `goto` `if`
`implicit` `in` `int` `interface` `internal` `is` `lock` `long` `namespace` `new`
`null` `object` `operator` `out` `override` `params` `private` `protected` `public`
`readonly` `ref` `return` `sbyte` `sealed` `short` `sizeof` `stackalloc` `static`
`string` `struct` `switch` `this` `throw` `true` `try` `typeof` `uint` `ulong`
`unchecked` `unsafe` `ushort` `using` `virtual` `void` `volatile` `while`

## 概述
支持 C# 语言子集，编译为 VML 汇编代码。采用静态编译版架构。文档描述的目标特性含 C# 2.0~5.0+ 语法（LINQ/async/await/switch表达式等），实际实现以基本语法为主，高级特性待实现。

## 支持的语言特性

### 1. 数据类型
- **基本类型**: `int`, `double`, `float`, `bool`, `string`, `char`, `decimal`
- **引用类型**: `object`, 类, 接口, 数组, 委托
- **值类型**: 结构体, 枚举
- **泛型**: `List<T>`, `Dictionary<K,V>`

### 2. 变量和常量
```csharp
int number = 10;
const double PI = 3.14159;
string text = "Hello";
var inferred = 42;  // 类型推断
int? nullable = null; // 可空类型
```

### 3. 控制流
```csharp
// if-else
if (condition) {
    Console.WriteLine("true");
} else {
    Console.WriteLine("false");
}

// switch表达式
string result = value switch {
    1 => "One",
    2 => "Two",
    _ => "Other"
};

// 循环
for (int i = 0; i < 10; i++) { }
foreach (var item in collection) { }
while (condition) { }
do { } while (condition);
```

### 4. 方法和属性
```csharp
public int Add(int a, int b) {
    return a + b;
}

// 属性
public string Name {
    get => _name;
    set => _name = value;
}

// 自动属性
public int Age { get; set; }

// Lambda表达式
Func<int, int> square = x => x * x;
```

### 5. 类和接口
```csharp
public class Person {
    public string Name { get; set; }
    public int Age { get; set; }
    
    public Person(string name, int age) {
        Name = name;
        Age = age;
    }
    
    public virtual void Introduce() {
        Console.WriteLine($"I'm {Name}, {Age} years old");
    }
}

public interface IDrawable {
    void Draw();
}
```

### 6. 异常处理
```csharp
try {
    int result = Divide(10, 0);
} catch (DivideByZeroException ex) {
    Console.WriteLine($"除零错误: {ex.Message}");
} catch (Exception ex) {
    Console.WriteLine($"其他错误: {ex.Message}");
} finally {
    Console.WriteLine("清理资源");
}

// 自定义异常
public class CustomException : Exception {
    public int ErrorCode { get; }
    
    public CustomException(string message, int errorCode) 
        : base(message) {
        ErrorCode = errorCode;
    }
}
```

### 7. LINQ和集合
```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var evenNumbers = numbers.Where(n => n % 2 == 0);
var doubled = numbers.Select(n => n * 2);
var sum = numbers.Sum();
var average = numbers.Average();

// 字典
var dict = new Dictionary<string, int> {
    ["apple"] = 1,
    ["banana"] = 2
};
```

### 8. 异步编程
```csharp
public async Task<string> FetchDataAsync() {
    using var client = new HttpClient();
    return await client.GetStringAsync("https://api.example.com");
}

// 使用async/await
async Task ProcessData() {
    try {
        string data = await FetchDataAsync();
        Console.WriteLine(data);
    } catch (HttpRequestException ex) {
        Console.WriteLine($"网络错误: {ex.Message}");
    }
}
```

## 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (float) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (double) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (long/ulong) 处理模式 |

### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `float` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `float` 声明时报告编译错误。

### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `double` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `double` 声明时报告编译错误。

### 64位整数 (int64)

本语言中的 64 位整数类型 `long` / `ulong` 按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数类型，遇到 `long` / `ulong` 声明时报告编译错误。

### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## 编译器状态
- **完成度**: ~92%
- **标准库**: Lib/csharp/ (C# 标准库，待创建)
- **Lexer**: 内嵌于 CSharpCompiler.cs
- **Parser**: Parser.cs
- **CodeGenerator**: CodeGenerator.cs

## 编译使用
```bash
dotnet run --project VMLTool -- input.cs -o output.vml
# 或插件模式:
vmltool input.cs -o output.vml
```
---

## 🆕 字符串类型 (v1.65.19)

该语言编译器默认使用 `.wstring` (UTF-16LE) 作为内部字符串存储。

| 模式 | 默认编码 | VML 伪指令 | SYSCALL 输出 |
|:-----|:--------|:----------|:------------|
| MCU (默认) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**预定义宏**: `VML_WSTRING` — OS 模式下自动定义，MCU 模式未定义
**输出函数**: OS 模式自动使用 `shared_print_wstr` (UTF-16LE→UTF-8 自动转换)

```c
// 用户代码可通过宏判断编码
#ifdef VML_WSTRING
  // 默认字符串为 wstring (UTF-16LE)
#else
  // 默认字符串为 UTF-8
#endif
```
