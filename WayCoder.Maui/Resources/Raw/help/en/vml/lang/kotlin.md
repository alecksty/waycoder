# Kotlin

The same approach as Java, with shorter syntax.

## How to run it on your phone

```
vml run examples/kotlin/sysinfo.kt
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- The entry point is `fun main()`
- Call `ui_*` directly, as in Java
- «bold»Write state arrays inside `main`«/» (see the pitfall below)

## Examples

| File | What it shows |
|---|---|
| `catch.kt` | Catch the blocks |
| `sysinfo.kt` | Device information |

Also in this folder: `demo_std.kt`, `demo_tty.kt`, `demo_ui.kt`

## Pitfalls we hit on real devices

- ⚠ «bold»A file-level (top-level) `arrayOf` reads back as 0«/» - keep state arrays inside `main`.
- ⚠ `step` is a reserved word in this front end; do not use it as a variable name.

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/KotlinCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Kotlin Language Compiler Specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | Kotlin 1.0+ (minimal subset) |
| «bold»Release year«/» | 2016 |
| «bold»Completeness«/» | ~93% |
| «bold»MCU completeness«/» | ~91% |
| «bold»Tests«/» | 29 passing |
| «bold»Updates«/» | 2026-05-21: data class toString/equals implemented; sealed-class exhaustiveness checks upgraded to compile errors; when-expression return values confirmed implemented |

#### Overview

This compiler supports a subset of the Kotlin language and compiles Kotlin source into VML assembly. It uses a three-stage Lexer → Parser → CodeGenerator architecture, about 1200 lines of C#.

#### Supported language features

##### 1. Data types and literals
- ✅ Integers: `42`, `-10`
- ✅ Floating point: `3.14`
- ✅ Boolean: `true`, `false`
- ✅ Strings: `"hello"` (supports the `\n` escape)
- ✅ String templates: `"Hello, $name!"`, `"${expr}"` (inlined during code generation)
- ✅ Type annotations: `: Int`, `: String` and so on (parsed only, no type checking is performed)

##### 2. Variable declarations
- ✅ `val` — immutable variable
- ✅ `var` — mutable variable
- ✅ `val x: Int = 42` — with a type annotation (parsed, then the type is ignored)

##### 3. Functions
- ✅ the `fun` keyword declares a function
- ✅ parameter lists (parameter names + optional type annotations)
- ✅ return type annotations (parsed, then ignored)
- ✅ the `return` statement
- ✅ Lambda expressions: `{ x, y -> x + y }`
- ✅ shorthand function bodies: `fun add(a: Int, b: Int) = a + b`

##### 4. Control flow
- ✅ `if`/`else` — conditional branches
- ✅ `while` — pre-condition loop
- ✅ `for (i in start..end)` — range loop (both parsing and code generation)
- ✅ `when (x) { 1 -> ...; 2 -> ...; else -> ... }` — multi-way branching (including in ranges and is type checks)
- ✅ Comparison chains: `a < b`, `x in 1..10`, `x is Int`
- ✅ Compound assignment: `+=`, `-=`, `*=`, `/=` (both parsing and code generation)

##### 5. Classes
- ✅ `class ClassName(val prop1: Type, var prop2: Type)` — primary-constructor class
- ✅ `data class` — data class (toString/equals generated automatically)
- ✅ `ClassName(args)` — constructor call (heap allocation, SYSCALL #40)
- ✅ Member methods: `fun ClassName.method(params) { body }`
- ✅ Member access: `obj.property`, `obj.method(args)`

##### 6. Built-in functions
- ✅ `println(expr)` — print an integer plus a newline (SYSCALL #6 + #4)
- ✅ `print(expr)` — print an integer without a newline (SYSCALL #6)
- ✅ `println("string")` — print a string (SYSCALL #4)

##### 7. Operators
- ✅ Arithmetic: `+`, `-`, `*`, `/`, `%`
- ✅ Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`
- ✅ Logical: `&&`, `||`, `!`
- ✅ Assignment: `=`, `+=`, `-=`, `*=`, `/=`
- ✅ the `..` range operator (used by for and in)
- ✅ `in` membership checks / `is` type checks
- ✅ Unary: `-`, `!`

#### Code generation (VML)

| Kotlin syntax | VML instruction |
|:-----------|:---------|
| val/var declarations | STORE into an R12-relative local variable frame |
| Arithmetic operations | ADD SUB MUL DIV MOD |
| Conditionals if/else | JZ/JNE/JMP + LABEL |
| while/for loops | LABEL + comparison + JZ/JMP |
| when expressions | PUSH the subject; compare branch conditions; JNE to the next; on a match JMP to end |
| println(x) | MOVE R0, x; SYSCALL #6; MOVE R0, #10; SYSCALL #4 |
| Class construction | SYSCALL #40 heap allocation; STORE the properties; RET the return address |
| data class | toString/equals generated automatically |
| Function calls | PUSH R15, PUSH R12, MOVE R12 R13 (prologue), caller cleans up R13 |
| return | MOVE R13 R12, POP R12, POP R15, RET (epilogue) |
| Lambda | PUSH R15/R14, MOVE R14 R13, closure body, POP R14, POP R15, RET |

#### Missing features

##### Advanced language features
- ❌ Coroutines
- ✅ Sealed classes — parsing, exhaustive when checks
- ❌ Extension functions
- ❌ Null safety (`?`, `!!`, `?:`)
- ❌ Smart casts
- ✅ when used as a returned expression

##### Standard library
- ❌ the standard library directory `Lib/kotlin/` (to be created)
- ❌ `readLine()` — input
- ❌ String functions (length, substring and so on)
- ❌ Collection classes (List, Map, Set)

#### Floating-point and 64-bit compile modes

The same three-parameter control as every VML compiler:

| Parameter | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard`/`soft`/`none` | `hard` | 32-bit floating point (Float) |
| `--float64` | `hard`/`soft`/`none` | `soft` | 64-bit floating point (Double) |
| `--int64` | `hard`/`soft`/`none` | `soft` | 64-bit integers (Long) |

#### How to compile

```bash
# compile directly
dotnet run --project VMLTool -- input.kt -o output.vml

# run
dotnet run --project VMLEmulators/ConsoleEmulator -- -r output.vml
```

#### Completeness assessment

| Component | Completeness |
|:----|:------:|
| Lexical analysis | ~98% (full token set, including the data keyword) |
| Syntax analysis | ~95% (fun/val/var/if/while/for/when/class/data class/is/in/lambda/range/sealed/when exhaustiveness) |
| Code generation | ~93% (expressions/control flow/strings/classes/data class/lambda/inline assembly/SafeCall/Elvis/generic erasure) |
| Standard library | ~50% (the basic functions in Lib/kotlin/ are already inlined) |
| «bold»Overall«/» | «bold»~93%«/» |

---

#### 🆕 String types (v1.65.19)

This language's compiler uses `.wstring` (UTF-16LE) as its internal string storage by default.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

«bold»Predefined macro«/»: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
«bold»Output function«/»: OS mode automatically uses `shared_print_wstr` (converts UTF-16LE→UTF-8 for you)

```c
// user code can test the macro to tell which encoding is in use
#ifdef VML_WSTRING
  // strings are wstring (UTF-16LE) by default
#else
  // strings are UTF-8 by default
#endif
```

## Compiler README

### Kotlin 1.0 Compiler

«bold»Path«/»: `VMLPrepares/KotlinCompiler/`
«bold»Completeness«/»: ~93% | 🟢 production ready
«bold»Standard library«/»: `Lib/kotlin/` (to be created)

#### Features
- ✅ Syntax analysis + code generation (Lexer/Parser/CodeGenerator, ~1200 lines)
- ✅ Variables: val/var + type annotations
- ✅ Control flow: if/else/while/for-in/when
- ✅ when expressions: value matching / in range checks / is type checks
- ✅ Functions: fun definitions / lambdas / single-expression bodies
- ✅ class: primary constructors / member methods / property access
- ✅ data class: toString/equals generated automatically
- ✅ Operators: arithmetic/comparison/logical/compound assignment/.. ranges/in/is
- ✅ Built-in functions: println/print (strings and integers)
- ✅ POKE/PEEK memory operations (MMIO)
- ✅ Shared built-in function library (builtins.vml)

#### Missing features
- ❌ Coroutines
- ❌ Null safety (?, !!, ?:)
- ❌ Extension functions
- ✅ Sealed classes — parsing + exhaustive when checks
- ❌ Standard library (Lib/kotlin/)

#### Compile modes

##### MCU mode (default: `--mode mcu`)
MCU mode is tuned for single-chip and bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that are incompatible with an operating system.

«bold»Skipped«/» (these constructs generate no code):
- coroutine

«bold»Kept«/» (implemented on top of the BIOS):
- class (heap allocation through SYSCALL #40), data class
- POKE/PEEK memory-mapped I/O (MMIO)
- basic type operations, control flow, function calls
- println/print mapped to SYSCALLs

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, an RTOS, and so on). It will support the full language feature set (file system, threads, async, exceptions, reflection).

#### Usage
```bash
dotnet run --project VMLTool -- input.kt -o output.vml
```

#### Tests
29 tests passing (embedded in the compiler source as verification)
