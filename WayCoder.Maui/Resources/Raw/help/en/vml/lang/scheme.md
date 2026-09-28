# Scheme

A Lisp dialect where brackets are everything.

## How to run it on your phone

```
vml run examples/scheme/sysinfo.scm
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- Everything is an expression: `(function argument...)`
- «bold»Write games as a flat top-level program«/» (see the pitfall below)

## Examples

| File | What it shows |
|---|---|
| `catch.scm` | Catch the blocks |
| `sysinfo.scm` | Device information |

Also in this folder: `demo_std.scm`, `demo_tty.scm`, `demo_ui.scm`

## Pitfalls we hit on real devices

- ⚠ «bold»User functions cannot see top-level variables«/» (the two frame pointers step on each other).
  The fix: write all game logic at the top level, and only pull out «bold»pure functions«/» that take every parameter and touch no globals.

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/SchemeCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Scheme language compiler specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | R5RS (1998) subset |
| «bold»Publication year«/» | 1998 |
| «bold»Completeness«/» | ~93% |
| «bold»Tests«/» | 32 passing |

#### Keywords

`define` `lambda` `let` `let*` `if` `cond` `else` `begin` `and` `or` `not`
`set!` `display` `newline` `print` `peek` `poke` `asm` `chipasm`
`eq?` `equal?` `null?`

#### Overview
A subset of the Scheme (R5RS) language is supported, compiled to VML assembly. The architecture parses S-expressions and supports the core features of functional programming.

#### Supported language features

##### 1. Data types
- «bold»Integers«/»: `42`, `-1`, `0`
- «bold»Strings«/»: `"hello"` (not yet supported at expression level)
- «bold»Booleans«/»: `#t`, `#f` (supported by the Parser)
- «bold»Symbols«/»: `x`, `+`, `foo-bar`
- «bold»Lists«/»: nested S-expression structures

##### 2. Expressions
- «bold»Arithmetic«/»: `+`, `-`, `*`, `/`
- «bold»Comparison«/»: `<`, `>`, `<=`, `>=`, `=`, `eq?`, `equal?`
- «bold»Logic«/»: `and`, `or`, `not`
- «bold»Conditionals«/»: `if`, `cond`
- «bold»Sequencing«/»: `begin`

##### 3. Functions
- «bold»Function definition«/»: `(define (name args) body)`
- «bold»Anonymous functions«/»: `(lambda (args) body)`
- «bold»Function calls«/»: `(func arg1 arg2 ...)`
- «bold»Variable definition«/»: `(define name value)`
- «bold»Variable assignment«/»: `(set! var value)`

##### 4. Binding forms
- «bold»Local bindings«/»: `(let ((var val) ...) body)` — parallel binding
- «bold»Sequential bindings«/»: `(let* ((var val) ...) body)` — serial binding

##### 5. Input and output
- «bold»Print an integer«/»: `(print val)` — prints the integer and a newline
- «bold»Display«/»: `(display val)` — prints the integer without a newline
- «bold»Newline«/»: `(newline)` — prints a newline character

##### 6. Memory access
- «bold»Memory read«/»: `(peek addr)` — reads the 32-bit value at address addr
- «bold»Memory write«/»: `(poke addr val)` — writes value val to address addr

##### 7. Inline assembly
- «bold»VML assembly«/»: `(asm "instruction")` — embeds a VML instruction directly
- «bold»Chip assembly«/»: `(chipasm "arch" "code")` — chip-specific assembly

#### Compile modes

##### MCU mode (default, `--mode mcu`)
Tuned for microcontroller environments.
- «bold»Kept«/»: every implemented feature
- «bold»Skipped«/»: call/cc (not implemented)

##### OS mode (`--mode os`, reserved)
Every language feature is available.

#### Usage examples

```scheme
;; factorial
(define (fact n)
  (if (<= n 1)
      1
      (* n (fact (- n 1)))))

;; anonymous function
(define f (lambda (x) (+ x 1)))
(print (f 5))

;; let binding
(let ((x 10) (y 20))
  (print (+ x y)))

;; let* sequential binding
(let* ((x 1) (y (+ x 1)))
  (print y))

;; cond with several branches
(define (sign x)
  (cond ((< x 0) (print -1))
        ((> x 0) (print 1))
        (else (print 0))))

;; memory access
(asm "nop")
(peek 0x4000)
(poke 16384 65)
```

#### Floating-point and 64-bit compile modes

The VML toolchain controls how floating-point values and 64-bit integers are handled through three compile options:

| Option | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | How 32-bit floating point (flonum) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | How 64-bit floating point is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | How 64-bit integers are handled |

##### 32-bit floating point (float32)

The `flonum` (inexact floating-point number) in Scheme is handled in one of the following modes when compiling to VML:

- «bold»`hard` mode (default)«/»: uses the VML native floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, computing directly through the sixteen floating-point registers F0-F15. Best performance; suited to target platforms that have floating-point hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software simulation library `softfloat.c`, simulating floating-point math through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms that have no floating-point hardware.
- «bold»`none` mode«/»: disables all 32-bit floating-point arithmetic.

##### 64-bit floating point (double)

Double-precision floating-point arithmetic in Scheme is compiled in one of the following modes:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software simulation library `softdouble.c`, simulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform, MCU included.
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, computing through the eight double-precision registers D0-D7.
- «bold»`none` mode«/»: disables double-precision floating-point arithmetic.

##### 64-bit integers (int64)

Large integers in Scheme (`bignum`) that go beyond the 32-bit range are handled in one of the following modes:

- «bold»`soft` mode (default)«/»: uses the double-register software simulation library `softint64.c`, simulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- «bold»`hard` mode«/»: reserved; a future VML version will support the native 64-bit integer instructions.
- «bold»`none` mode«/»: degrades to 32-bit integers.

##### Software simulation libraries

The software simulation libraries above all live in the `Lib/shared/` directory. They are written in C and compiled to VML by the C compiler, and every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point simulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point simulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer double-register simulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### Compiling

```bash
# compile Scheme to VML
dotnet run --project VMLPrepares/SchemeCompiler input.scm -o output.vml

# run it
dotnet run --project VMLEmulators/ConsoleEmulator -- -r output.vml
```

#### Tests

```bash
# run the Scheme tests
dotnet test VMLTests/VMLTests.csproj --filter "SchemeTests"
```

---

#### 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

«bold»MCU mode«/» (default): strings are output as UTF-8 (`.string`)
«bold»OS mode«/»: the encoding can be tested with the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h); each language compiler can use them as needed.

## Compiler README

### Scheme (R5RS subset) compiler

«bold»Path«/»: `VMLPrepares/SchemeCompiler/`
«bold»Completeness«/»: ~93% | 🟢 production ready
«bold»Standard library«/»: `Lib/scheme/` (to be created)

#### Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGen, ~1800 lines)
- ✅ S-expression parsing
- ✅ define / lambda / function definition and calls
- ✅ if / cond / and / or conditionals
- ✅ Basic types: integers / floats / booleans / strings / symbols
- ✅ Arithmetic: + - * / quotient remainder
- ✅ Comparison: = < > <= >=
- ✅ Lists: cons/car/cdr/list/null?/pair?
- ✅ Higher-order functions: apply/map
- ✅ GC marking (interface reserved)
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)

#### Missing features
- ✅ Tail-call optimization (TCO)
- ❌ call/cc (call-with-current-continuation)
- ❌ Macro system (define-syntax/syntax-rules)
- ❌ Vectors (vector)
- ❌ Full character/string operations
- ❌ Full numeric tower (complex/rational)
- ❌ Standard library (Lib/scheme/)

#### Compile modes

##### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller and bare-metal environments (Arduino/STM32/8051 and the like) and automatically skips features that need an operating system.

«bold»Skipped«/» (no code is generated when these appear):
- call/cc (needs stack capture), dynamic eval

«bold»Kept«/» (the low-level parts are provided by the BIOS):
- cons (heap allocation), GC marking
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- display/write mapped to SYSCALL

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, RTOS and so on). It will then support the full set of language features (file system, threads, async, exceptions, reflection and so on).

#### Usage
```bash
dotnet run --project VMLTool -- input.scm -o output.vml
```

#### Tests
Tests are embedded in the compiler source as verification
