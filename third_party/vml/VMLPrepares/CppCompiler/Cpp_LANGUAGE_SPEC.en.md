# C++ language compiler specification

> **Version**: v1.1 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | C++11 subset (ISO/IEC 14882:2011) |
| **Publication year** | 2011 |
| **Completeness** | ~95% |
| **MCU completeness** | ~93% |
| **Tests** | 0 (test directory yet to be created) |
| **Updates** | 2026-05-18: corrected the target standard to C++11 (including nullptr/constexpr/override); corrected completeness and test counts |

## Keywords

All C subset keywords plus the C++ extensions:
`class` `new` `delete` `this` `virtual` `override` `public` `private` `protected`
`namespace` `using` `template` `typename` `try` `catch` `throw` `typeid` `dynamic_cast`
`static_cast` `const_cast` `reinterpret_cast` `bool` `true` `false` `nullptr`
`friend` `operator` `explicit` `mutable` `inline` `constexpr`

## Supported features (C++11 subset)

### Basic features
- [x] Basic types: int/char/float/double/bool/void/pointers/references/const
- [x] Control flow: if/else/while/for/do-while/switch/case/break/continue/goto
- [x] Functions: definition / overloading / default arguments / by value / by reference / recursion
- [x] Operators: all arithmetic / comparison / logical / bitwise / ternary / increment and decrement / sizeof

### Object-oriented
- [x] Classes: members / methods / access control (public/private/protected)
- [x] Inheritance: single / multiple / virtual inheritance
- [x] Polymorphism: virtual functions / vtable / dynamic binding
- [x] Constructors / destructors: default / parameterized / copy / initializer list
- [x] new/delete: dynamic memory allocation and release

### Advanced features
- [x] Templates: function templates / class templates
- [x] Exceptions: try/catch/throw (mapped to VML THROW/CATCH)
- [x] Namespaces: namespace/using
- [x] const member functions
- [ ] RTTI: typeid/dynamic_cast (skipped on MCU)

### Standard library
- [x] new/delete
- [x] iostream (basic)
- [ ] Full STL (partially supported)

## Floating-point and 64-bit compile modes

The VML toolchain controls how floating-point values and 64-bit integers are handled through three compile options:

| Option | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | How 32-bit floating point (float) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | How 64-bit floating point (double) is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | How 64-bit integers (long long) are handled |

### 32-bit floating point (float32)

The 32-bit single-precision floating-point type `float` in this language is compiled in one of the following modes:

- **`hard` mode (default)**: uses the VML native floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, computing directly through the sixteen floating-point registers F0-F15. Best performance; suited to target platforms that have floating-point hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software simulation library `softfloat.c`, simulating floating-point math through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms that have no floating-point hardware.
- **`none` mode**: disables all 32-bit floating-point types; a `float` declaration is reported as a compile error.

### 64-bit floating point (double)

The 64-bit double-precision floating-point type `double` in this language is compiled in one of the following modes:

- **`soft` mode (default)**: uses the IEEE 754 double-precision software simulation library `softdouble.c`, simulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform, MCU included.
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, computing through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- **`none` mode**: disables all 64-bit floating-point types; a `double` declaration is reported as a compile error.

### 64-bit integers (int64)

The 64-bit integer type `long long` in this language is compiled in one of the following modes:

- **`soft` mode (default)**: uses the double-register software simulation library `softint64.c`, simulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- **`hard` mode**: uses the VML native 64-bit integer instructions `MOVEL`/`ADDL`/`SUBL`/`MULL`/`DIVL`/`MODL`/`NEGL`/`CMPL`/`ANDL`/`ORL`/`XORL`/`NOTL`/`SHLL`/`SHRL`, computing through the eight long-integer registers L0-L7. Available from v1.65.197+.
- **`none` mode**: disables the 64-bit integer type; a `long long` declaration is reported as a compile error.

### Software simulation libraries

The software simulation libraries above all live in the `Lib/shared/` directory. They are written in C and compiled to VML by the C compiler, and every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point simulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point simulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer double-register simulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## Precision notes
- The VML target platform is an MCU; the full STL and RTTI are not supported
- The keyword table includes C++11 features (nullptr/constexpr/override/friend), but template/STL are only parsed and skipped, with no code generated

---

## 🆕 String type (v1.65.19)

This language compiler stores strings internally as `.wstring` (UTF-16LE) by default.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**Predefined macro**: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
**Output function**: OS mode automatically uses `shared_print_wstr` (converts UTF-16LE to UTF-8 automatically)

```c
// user code can test the encoding with the macro
#ifdef VML_WSTRING
  // the default string is wstring (UTF-16LE)
#else
  // the default string is UTF-8
#endif
```

---

## 🆕 Preprocessor directives (v1.65.19)

The C++ compiler inherits every preprocessor feature of the C compiler:

| Directive | Description |
|:-----|:-----|
| `#param lib("xxx")` | automatically links the given library |
| `#param path("xxx")` | adds an include path |
| `#param prefix("xxx")` | function name prefix (multi-language libraries) |

Together with the `-D VML_PREFIX=xxx` command-line option, the same C/C++ source can generate libraries with different prefixes for different languages.
See [C_LANGUAGE_SPEC.md](../CCompiler/C_LANGUAGE_SPEC.md) for details.
