# Swift language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | Swift 3.0 subset (2016) |
| **Release year** | 2016 |
| **Completeness** | ~90% |
| **MCU completeness** | ~88% |
| **Tests** | 0 (the test directory is yet to be created) |
| **Updates** | 2026-05-18: corrected the target standard version (to include Swift 2.0+ features such as guard/defer/error handling), the completeness and the test counts |

## Keywords

`associatedtype` `break` `case` `catch` `class` `continue` `default` `defer` `deinit` `do`
`else` `enum` `extension` `fallthrough` `false` `fileprivate` `for` `func` `guard` `if`
`import` `in` `init` `inout` `internal` `is` `let` `nil` `open` `operator` `precedencegroup`
`private` `protocol` `public` `repeat` `return` `self` `Self` `static` `struct` `subscript`
`super` `switch` `throw` `throws` `true` `try` `typealias` `var` `where` `while`

## Overview
A subset of the Swift language is supported and compiled into VML assembly code. It uses a statically compiled architecture. The target features described in this document include Swift 2.0~3.0 syntax (guard/defer/do-catch/protocol extension), while the actual implementation is mainly basic syntax.

## Supported language features

### 1. Data types
- **Basic types**: `Int`, `Double`, `Float`, `Bool`, `String`, `Character`
- **Optional types**: `Optional<T>`, `Int?`, `String?`
- **Collection types**: `Array<T>`, `Dictionary<K,V>`, `Set<T>`
- **Tuples**: `(Int, String)`, named tuples `(x: Int, y: Int)`

### 2. Variables and constants
```swift
let constant = 10          // constant
var variable = "Hello"     // variable
var optional: String?      // optional type
var array = [1, 2, 3]      // array
var dict = ["key": "value"] // dictionary
```

### 3. Control flow
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

### 4. Functions
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

### 5. Classes and structs
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

### 6. Protocols and extensions
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

### 7. Error handling
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

## Floating-point and 64-bit compilation modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compilation flags:

| Flag | Possible values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit float (Float) handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit float (Double) handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer (Int64) handling mode |

### 32-bit float (float32)

The 32-bit single-precision float type `Float` in this language compiles as follows:

- **`hard` mode (default)**: uses the VML native float instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly through the sixteen float registers F0-F15. Best performance; suited to target platforms with float hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating float arithmetic through the `__vml_float_add/sub/mul/div/neg/abs/cmp` functions and so on. Suited to MCU platforms without float hardware.
- **`none` mode**: disables every 32-bit float type; a compile error is reported when a `Float` declaration is met.

### 64-bit float (double)

The 64-bit double-precision float type `Double` in this language compiles as follows:

- **`soft` mode (default)**: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through the `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` functions and so on. Compatible with every platform (MCU included).
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- **`none` mode**: disables every 64-bit float type; a compile error is reported when a `Double` declaration is met.

### 64-bit integer (int64)

The 64-bit integer type `Int64` in this language compiles as follows:

- **`soft` mode (default)**: uses the dual-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through the `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` functions and so on.
- **`hard` mode**: reserved; a future VML version will support native 64-bit integer instructions.
- **`none` mode**: disables the 64-bit integer type; a compile error is reported when an `Int64` declaration is met.

### Software emulation libraries

The software emulation libraries above all live in the `Lib/shared/` directory, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit float emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit float emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer dual-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## Compiler status
- **Completeness**: ~90%
- **Standard library**: Lib/swift/ (the Swift standard library, yet to be created)
- **Lexer**: Lexer.cs
- **Parser**: Parser.cs
- **CodeGenerator**: CodeGenerator.cs

## Compiling and using it
```bash
dotnet run --project VMLTool -- input.swift -o output.vml
# or plugin mode:
vmltool input.swift -o output.vml
```
---

## 🆕 String type (v1.65.19)

This language compiler uses `.wstring` (UTF-16LE) by default as its internal string storage.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**Predefined macro**: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
**Output function**: OS mode automatically uses `shared_print_wstr` (automatic UTF-16LE→UTF-8 conversion)

```c
// user code can determine the encoding through the macro
#ifdef VML_WSTRING
  // the default string is a wstring (UTF-16LE)
#else
  // the default string is UTF-8
#endif
```
