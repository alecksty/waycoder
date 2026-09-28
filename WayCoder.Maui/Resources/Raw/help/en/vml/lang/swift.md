# Swift

Natural to write, and the plane shooter example is a complete, playable game.

## How to run it on your phone

```
vml run examples/swift/sysinfo.swift
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- Call `ui_*` directly; no declaration needed
- Remember to capture return values explicitly

## Examples

| File | What it shows |
|---|---|
| `file_io.swift` | Reading and writing files |
| `plane.swift` | Plane combat |
| `snake.swift` | Snake |
| `sysinfo.swift` | Device information |

Also in this folder: `demo_std.swift`, `demo_tty.swift`, `demo_ui.swift`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/SwiftCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Swift language compiler specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | Swift 3.0 subset (2016) |
| «bold»Release year«/» | 2016 |
| «bold»Completeness«/» | ~90% |
| «bold»MCU completeness«/» | ~88% |
| «bold»Tests«/» | 0 (the test directory is yet to be created) |
| «bold»Updates«/» | 2026-05-18: corrected the target standard version (to include Swift 2.0+ features such as guard/defer/error handling), the completeness and the test counts |

#### Keywords

`associatedtype` `break` `case` `catch` `class` `continue` `default` `defer` `deinit` `do`
`else` `enum` `extension` `fallthrough` `false` `fileprivate` `for` `func` `guard` `if`
`import` `in` `init` `inout` `internal` `is` `let` `nil` `open` `operator` `precedencegroup`
`private` `protocol` `public` `repeat` `return` `self` `Self` `static` `struct` `subscript`
`super` `switch` `throw` `throws` `true` `try` `typealias` `var` `where` `while`

#### Overview
A subset of the Swift language is supported and compiled into VML assembly code. It uses a statically compiled architecture. The target features described in this document include Swift 2.0~3.0 syntax (guard/defer/do-catch/protocol extension), while the actual implementation is mainly basic syntax.

#### Supported language features

##### 1. Data types
- «bold»Basic types«/»: `Int`, `Double`, `Float`, `Bool`, `String`, `Character`
- «bold»Optional types«/»: `Optional<T>`, `Int?`, `String?`
- «bold»Collection types«/»: `Array<T>`, `Dictionary<K,V>`, `Set<T>`
- «bold»Tuples«/»: `(Int, String)`, named tuples `(x: Int, y: Int)`

##### 2. Variables and constants
```swift
let constant = 10          // constant
var variable = "Hello"     // variable
var optional: String?      // optional type
var array = [1, 2, 3]      // array
var dict = ["key": "value"] // dictionary
```

##### 3. Control flow
```swift
// if-else
if condition {
    print("true")
} else {
    print("false")
}

// guard statement
guard let value = optional else {
    return
}

// switch
switch value {
case 1: print("One")
case 2...5: print("2 to 5")
default: print("Other")
}

// loops
for i in 0..<10 { }
while condition { }
repeat { } while condition
```

##### 4. Functions
```swift
func greet(name: String) -> String {
    return "Hello, \(name)"
}

// argument labels
func calculate(for x: Int, and y: Int) -> Int {
    return x + y
}

// closures
let closure = { (x: Int) -> Int in
    return x * 2
}
```

##### 5. Classes and structs
```swift
class Person {
    var name: String
    var age: Int
    
    init(name: String, age: Int) {
        self.name = name
        self.age = age
    }
    
    func introduce() {
        print("I'm \(name), \(age) years old")
    }
}

struct Point {
    var x: Int
    var y: Int
}
```

##### 6. Protocols and extensions
```swift
protocol Drawable {
    func draw()
}

extension Int {
    var squared: Int {
        return self * self
    }
}
```

##### 7. Error handling
```swift
enum NetworkError: Error {
    case timeout
    case serverError(code: Int)
}

func fetchData() throws -> Data {
    throw NetworkError.timeout
}

do {
    let data = try fetchData()
} catch NetworkError.timeout {
    print("Timeout")
} catch {
    print("Other error")
}
```

#### Floating-point and 64-bit compilation modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compilation flags:

| Flag | Possible values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit float (Float) handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit float (Double) handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer (Int64) handling mode |

##### 32-bit float (float32)

The 32-bit single-precision float type `Float` in this language compiles as follows:

- «bold»`hard` mode (default)«/»: uses the VML native float instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly through the sixteen float registers F0-F15. Best performance; suited to target platforms with float hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating float arithmetic through the `__vml_float_add/sub/mul/div/neg/abs/cmp` functions and so on. Suited to MCU platforms without float hardware.
- «bold»`none` mode«/»: disables every 32-bit float type; a compile error is reported when a `Float` declaration is met.

##### 64-bit float (double)

The 64-bit double-precision float type `Double` in this language compiles as follows:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through the `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` functions and so on. Compatible with every platform (MCU included).
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- «bold»`none` mode«/»: disables every 64-bit float type; a compile error is reported when a `Double` declaration is met.

##### 64-bit integer (int64)

The 64-bit integer type `Int64` in this language compiles as follows:

- «bold»`soft` mode (default)«/»: uses the dual-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through the `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` functions and so on.
- «bold»`hard` mode«/»: reserved; a future VML version will support native 64-bit integer instructions.
- «bold»`none` mode«/»: disables the 64-bit integer type; a compile error is reported when an `Int64` declaration is met.

##### Software emulation libraries

The software emulation libraries above all live in the `Lib/shared/` directory, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit float emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit float emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer dual-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### Compiler status
- «bold»Completeness«/»: ~90%
- «bold»Standard library«/»: Lib/swift/ (the Swift standard library, yet to be created)
- «bold»Lexer«/»: Lexer.cs
- «bold»Parser«/»: Parser.cs
- «bold»CodeGenerator«/»: CodeGenerator.cs

#### Compiling and using it
```bash
dotnet run --project VMLTool -- input.swift -o output.vml
# or plugin mode:
vmltool input.swift -o output.vml
```
---

#### 🆕 String type (v1.65.19)

This language compiler uses `.wstring` (UTF-16LE) by default as its internal string storage.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

«bold»Predefined macro«/»: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
«bold»Output function«/»: OS mode automatically uses `shared_print_wstr` (automatic UTF-16LE→UTF-8 conversion)

```c
// user code can determine the encoding through the macro
#ifdef VML_WSTRING
  // the default string is a wstring (UTF-16LE)
#else
  // the default string is UTF-8
#endif
```

## Compiler README

### Swift 3.0 subset compiler

«bold»Path«/»: `VMLPrepares/SwiftCompiler/`
«bold»Completeness«/»: ~90% | 🟢 production ready
«bold»Standard library«/»: `Lib/swift/` (yet to be created)

#### Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/else/for/while/repeat-while/switch)
- ✅ Function/closure definition and calls
- ⚠️ class/struct/enum — partially supported
- ⚠️ protocol/extension — partially supported
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)


#### Compilation modes

##### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments (Arduino/STM32/8051 and so on), and automatically skips features that are incompatible with an operating system.

«bold»Skipped«/» (no code is generated for this syntax):
- async/await, Actor

«bold»Kept«/» (implemented underneath by the BIOS):
- class/struct, closure
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to the UART

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (such as embedded Linux or an RTOS), and will support every language feature (file system, threads, asynchronous I/O, exceptions, reflection and so on).

##### RAM levels
- `--ram k`: kilobyte level (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte level (64KB~1MB, such as ARM Cortex-M, «bold»default«/»)
- `--ram g`: gigabyte level (such as x86/DDR systems)
- `--stack-size <bytes>`: specify the stack size by hand (by default it is allocated automatically from `--ram`)

##### MCU-safe coding tips
- Limited stack space (256-4096 bytes is typical), so avoid deep recursion
- Deep recursion is not allowed (beyond 10 levels the stack must be evaluated)
- Dynamic loading (import/dofile/eval) is not allowed
- Floating-point arithmetic may require the soft-float library

#### Usage
```bash
dotnet run --project VMLTool -- input.swift -o output.vml
```

#### Tests
`Test/Sw/` — 0 test files (the test directory is yet to be created)
