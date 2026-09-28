# C#

Reads like desktop C#; the entry point is always a class named `P`.

## How to run it on your phone

```
vml run examples/csharp/sysinfo.cs
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- The entry point is `class P { static void Main() }`
- `System.Console.WriteLine` works directly
- Keep state in `static` fields or arrays

## Examples

| File | What it shows |
|---|---|
| `file_io.cs` | Reading and writing files |
| `racer.cs` | Racing |
| `snake.cs` | Snake |
| `sysinfo.cs` | Device information |

Also in this folder: `demo_std.cs`, `demo_tty.cs`, `demo_ui.cs`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/CSharpCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### C# language compiler specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | C# 5.0 subset (ECMA-334:2012) |
| «bold»Publication year«/» | 2012 |
| «bold»Completeness«/» | ~92% |
| «bold»MCU completeness«/» | ~90% |
| «bold»Tests«/» | 0 (test directory yet to be created) |
| «bold»Updates«/» | 2026-05-18: corrected the target standard version, completeness and test counts |

#### Keywords

`abstract` `as` `base` `bool` `break` `byte` `case` `catch` `char` `checked` `class`
`const` `continue` `decimal` `default` `delegate` `do` `double` `else` `enum` `event`
`explicit` `extern` `false` `finally` `fixed` `float` `for` `foreach` `goto` `if`
`implicit` `in` `int` `interface` `internal` `is` `lock` `long` `namespace` `new`
`null` `object` `operator` `out` `override` `params` `private` `protected` `public`
`readonly` `ref` `return` `sbyte` `sealed` `short` `sizeof` `stackalloc` `static`
`string` `struct` `switch` `this` `throw` `true` `try` `typeof` `uint` `ulong`
`unchecked` `unsafe` `ushort` `using` `virtual` `void` `volatile` `while`

#### Overview
A subset of the C# language is supported, compiled to VML assembly. The architecture is statically compiled. The target features described in this document include C# 2.0~5.0+ syntax (LINQ, async/await, switch expressions and so on); the actual implementation is centred on basic syntax, with the advanced features yet to be implemented.

#### Supported language features

##### 1. Data types
- «bold»Basic types«/»: `int`, `double`, `float`, `bool`, `string`, `char`, `decimal`
- «bold»Reference types«/»: `object`, classes, interfaces, arrays, delegates
- «bold»Value types«/»: structs, enums
- «bold»Generics«/»: `List<T>`, `Dictionary<K,V>`

##### 2. Variables and constants
```csharp
int number = 10;
const double PI = 3.14159;
string text = "Hello";
var inferred = 42;  // type inference
int? nullable = null; // nullable type
```

##### 3. Control flow
```csharp
// if-else
if (condition) {
    Console.WriteLine("true");
} else {
    Console.WriteLine("false");
}

// switch expression
string result = value switch {
    1 => "One",
    2 => "Two",
    _ => "Other"
};

// loops
for (int i = 0; i < 10; i++) { }
foreach (var item in collection) { }
while (condition) { }
do { } while (condition);
```

##### 4. Methods and properties
```csharp
public int Add(int a, int b) {
    return a + b;
}

// properties
public string Name {
    get => _name;
    set => _name = value;
}

// auto-property
public int Age { get; set; }

// lambda expression
Func<int, int> square = x => x * x;
```

##### 5. Classes and interfaces
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

##### 6. Exception handling
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

// custom exception
public class CustomException : Exception {
    public int ErrorCode { get; }
    
    public CustomException(string message, int errorCode) 
        : base(message) {
        ErrorCode = errorCode;
    }
}
```

##### 7. LINQ and collections
```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var evenNumbers = numbers.Where(n => n % 2 == 0);
var doubled = numbers.Select(n => n * 2);
var sum = numbers.Sum();
var average = numbers.Average();

// dictionary
var dict = new Dictionary<string, int> {
    ["apple"] = 1,
    ["banana"] = 2
};
```

##### 8. Asynchronous programming
```csharp
public async Task<string> FetchDataAsync() {
    using var client = new HttpClient();
    return await client.GetStringAsync("https://api.example.com");
}

// using async/await
async Task ProcessData() {
    try {
        string data = await FetchDataAsync();
        Console.WriteLine(data);
    } catch (HttpRequestException ex) {
        Console.WriteLine($"网络错误: {ex.Message}");
    }
}
```

#### Floating-point and 64-bit compile modes

The VML toolchain controls how floating-point values and 64-bit integers are handled through three compile options:

| Option | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | How 32-bit floating point (float) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | How 64-bit floating point (double) is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | How 64-bit integers (long/ulong) are handled |

##### 32-bit floating point (float32)

The 32-bit single-precision floating-point type `float` in this language is compiled in one of the following modes:

- «bold»`hard` mode (default)«/»: uses the VML native floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, computing directly through the sixteen floating-point registers F0-F15. Best performance; suited to target platforms that have floating-point hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software simulation library `softfloat.c`, simulating floating-point math through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms that have no floating-point hardware.
- «bold»`none` mode«/»: disables all 32-bit floating-point types; a `float` declaration is reported as a compile error.

##### 64-bit floating point (double)

The 64-bit double-precision floating-point type `double` in this language is compiled in one of the following modes:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software simulation library `softdouble.c`, simulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform, MCU included.
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, computing through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- «bold»`none` mode«/»: disables all 64-bit floating-point types; a `double` declaration is reported as a compile error.

##### 64-bit integers (int64)

The 64-bit integer types `long` / `ulong` in this language are compiled in one of the following modes:

- «bold»`soft` mode (default)«/»: uses the double-register software simulation library `softint64.c`, simulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- «bold»`hard` mode«/»: reserved; a future VML version will support the native 64-bit integer instructions.
- «bold»`none` mode«/»: disables the 64-bit integer types; a `long` / `ulong` declaration is reported as a compile error.

##### Software simulation libraries

The software simulation libraries above all live in the `Lib/shared/` directory. They are written in C and compiled to VML by the C compiler, and every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point simulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point simulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer double-register simulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### Compiler status
- «bold»Completeness«/»: ~92%
- «bold»Standard library«/»: Lib/csharp/ (the C# standard library, to be created)
- «bold»Lexer«/»: embedded in CSharpCompiler.cs
- «bold»Parser«/»: Parser.cs
- «bold»CodeGenerator«/»: CodeGenerator.cs

#### Compile and use
```bash
dotnet run --project VMLTool -- input.cs -o output.vml
# or plugin mode:
vmltool input.cs -o output.vml
```
---

#### 🆕 String type (v1.65.19)

This language compiler stores strings internally as `.wstring` (UTF-16LE) by default.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

«bold»Predefined macro«/»: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
«bold»Output function«/»: OS mode automatically uses `shared_print_wstr` (converts UTF-16LE to UTF-8 automatically)

```c
// user code can test the encoding with the macro
#ifdef VML_WSTRING
  // the default string is wstring (UTF-16LE)
#else
  // the default string is UTF-8
#endif
```

## Compiler README

### C# compiler

«bold»Path«/»: `VMLPrepares/CSharpCompiler/`
«bold»Completeness«/»: ~92% | 🟢 production ready
«bold»Standard library«/»: `Lib/csharp/` (to be created)

#### Features
- ✅ Full parsing + code generation
- ✅ Control flow (if/while/for/switch)
- ✅ Function and procedure definition and calls
- ✅ Standard library support
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Hexadecimal literals
- ✅ Shared built-in function library (builtins.vml)


#### Compile modes

##### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller and bare-metal environments (Arduino/STM32/8051 and the like) and automatically skips features that need an operating system.

«bold»Skipped«/» (no code is generated when these appear):
- async/await, Task, GC, reflection

«bold»Kept«/» (the low-level parts are provided by the BIOS):
- new (heap), class, properties
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to UART
- LINQ/generics — to be implemented

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, RTOS and so on). It will then support the full set of language features (file system, threads, async, exceptions, reflection and so on).

##### RAM level
- `--ram k`: kilobyte range (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte range (64KB~1MB, such as ARM Cortex-M, «bold»default«/»)
- `--ram g`: gigabyte range (such as x86/DDR systems)
- `--stack-size <bytes>`: set the stack size by hand (by default it is allocated automatically from --ram)

##### MCU safe-coding tips
- Limited stack space (256-4096 bytes is typical), avoid deep recursion
- No deep recursion (anything deeper than 10 levels needs a stack assessment)
- No dynamic loading (import/dofile/eval)
- Floating-point math may need a software floating-point library

#### Usage
```bash
dotnet run --project VMLPrepares/CSharpCompiler input.CS -o output.vml
```

#### Tests
`Test/CS/` — 0 test files (the test directory is yet to be created)
