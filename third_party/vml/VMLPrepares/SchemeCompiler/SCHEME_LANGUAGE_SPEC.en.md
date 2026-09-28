# Scheme language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | R5RS (1998) subset |
| **Publication year** | 1998 |
| **Completeness** | ~93% |
| **Tests** | 32 passing |

## Keywords

`define` `lambda` `let` `let*` `if` `cond` `else` `begin` `and` `or` `not`
`set!` `display` `newline` `print` `peek` `poke` `asm` `chipasm`
`eq?` `equal?` `null?`

## Overview
A subset of the Scheme (R5RS) language is supported, compiled to VML assembly. The architecture parses S-expressions and supports the core features of functional programming.

## Supported language features

### 1. Data types
- **Integers**: `42`, `-1`, `0`
- **Strings**: `"hello"` (not yet supported at expression level)
- **Booleans**: `#t`, `#f` (supported by the Parser)
- **Symbols**: `x`, `+`, `foo-bar`
- **Lists**: nested S-expression structures

### 2. Expressions
- **Arithmetic**: `+`, `-`, `*`, `/`
- **Comparison**: `<`, `>`, `<=`, `>=`, `=`, `eq?`, `equal?`
- **Logic**: `and`, `or`, `not`
- **Conditionals**: `if`, `cond`
- **Sequencing**: `begin`

### 3. Functions
- **Function definition**: `(define (name args) body)`
- **Anonymous functions**: `(lambda (args) body)`
- **Function calls**: `(func arg1 arg2 ...)`
- **Variable definition**: `(define name value)`
- **Variable assignment**: `(set! var value)`

### 4. Binding forms
- **Local bindings**: `(let ((var val) ...) body)` — parallel binding
- **Sequential bindings**: `(let* ((var val) ...) body)` — serial binding

### 5. Input and output
- **Print an integer**: `(print val)` — prints the integer and a newline
- **Display**: `(display val)` — prints the integer without a newline
- **Newline**: `(newline)` — prints a newline character

### 6. Memory access
- **Memory read**: `(peek addr)` — reads the 32-bit value at address addr
- **Memory write**: `(poke addr val)` — writes value val to address addr

### 7. Inline assembly
- **VML assembly**: `(asm "instruction")` — embeds a VML instruction directly
- **Chip assembly**: `(chipasm "arch" "code")` — chip-specific assembly

## Compile modes

### MCU mode (default, `--mode mcu`)
Tuned for microcontroller environments.
- **Kept**: every implemented feature
- **Skipped**: call/cc (not implemented)

### OS mode (`--mode os`, reserved)
Every language feature is available.

## Usage examples

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

## Floating-point and 64-bit compile modes

The VML toolchain controls how floating-point values and 64-bit integers are handled through three compile options:

| Option | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | How 32-bit floating point (flonum) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | How 64-bit floating point is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | How 64-bit integers are handled |

### 32-bit floating point (float32)

The `flonum` (inexact floating-point number) in Scheme is handled in one of the following modes when compiling to VML:

- **`hard` mode (default)**: uses the VML native floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, computing directly through the sixteen floating-point registers F0-F15. Best performance; suited to target platforms that have floating-point hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software simulation library `softfloat.c`, simulating floating-point math through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms that have no floating-point hardware.
- **`none` mode**: disables all 32-bit floating-point arithmetic.

### 64-bit floating point (double)

Double-precision floating-point arithmetic in Scheme is compiled in one of the following modes:

- **`soft` mode (default)**: uses the IEEE 754 double-precision software simulation library `softdouble.c`, simulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform, MCU included.
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, computing through the eight double-precision registers D0-D7.
- **`none` mode**: disables double-precision floating-point arithmetic.

### 64-bit integers (int64)

Large integers in Scheme (`bignum`) that go beyond the 32-bit range are handled in one of the following modes:

- **`soft` mode (default)**: uses the double-register software simulation library `softint64.c`, simulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- **`hard` mode**: reserved; a future VML version will support the native 64-bit integer instructions.
- **`none` mode**: degrades to 32-bit integers.

### Software simulation libraries

The software simulation libraries above all live in the `Lib/shared/` directory. They are written in C and compiled to VML by the C compiler, and every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point simulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point simulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer double-register simulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## Compiling

```bash
# compile Scheme to VML
dotnet run --project VMLPrepares/SchemeCompiler input.scm -o output.vml

# run it
dotnet run --project VMLEmulators/ConsoleEmulator -- -r output.vml
```

## Tests

```bash
# run the Scheme tests
dotnet test VMLTests/VMLTests.csproj --filter "SchemeTests"
```

---

## 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU mode** (default): strings are output as UTF-8 (`.string`)
**OS mode**: the encoding can be tested with the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h); each language compiler can use them as needed.
