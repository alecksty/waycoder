# C Language Compiler Specification

> **Version**: v1.1 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | C99 (ISO/IEC 9899:1999) |
| **Release year** | 1999 |
| **Completeness** | ~97% |
| **Tests** | 43+ passing (including 12 Int64) + **Lua 5.4 31/31** + cJSON/nbsdgames/parson/tiny_regex_c |
| **Milestones** | **Lua 5.4 31/31 🎉** + SQLite 255K lines (v1.65.36) |

## Overview

This compiler supports a subset of the C99 standard and compiles C source code into VML (Virtual Machine Language) assembly. It follows the classic compilation pipeline: preprocessing → lexical analysis → syntax analysis → code generation.

## Supported language features

### 1. Data types

#### Basic data types
- **Integer types**: `int`, `short`, `long`, `signed`, `unsigned`
  - Type modifiers can be combined: `unsigned int`, `signed long`, `unsigned short`, and so on
  - The default integer type is `signed int`
  - `unsigned` on its own is equivalent to `unsigned int`
  - `signed` on its own is equivalent to `signed int`
- **Character types**: `char`, `signed char`, `unsigned char`
- **Floating-point types**: `float`, `double`
- **Boolean type**: `bool` (C99 extension)
- **Void type**: `void`

#### Constant suffixes
- **Integer constant suffixes**:
  - `L` or `l`: long integer constant, e.g. `100L`
  - `U` or `u`: unsigned integer constant, e.g. `100U`
  - `UL` or `ul`: unsigned long integer constant, e.g. `100UL`
- **Floating-point constant suffixes**:
  - `F` or `f`: single-precision floating-point constant, e.g. `3.14F`
  - no suffix: double-precision floating-point constant, e.g. `3.14`
- **Hexadecimal constant suffixes**:
  - hexadecimal constant suffixes are supported: `0xFFU`, `0x7FFFFFFFL`, `0xFFFFFFFFUL`

#### Derived types
- **Pointers**: `int*`, `char*`, `void*`, and so on
- **Arrays**: one-dimensional and multi-dimensional arrays, with initialization, e.g. `int arr[10]` and `int matrix[2][3] = {{1,2,3},{4,5,6}}`
- **Structs**: global struct definitions and member access
- **Enums**: global enum definitions and use

### 2. Variable declaration and definition

#### Variable declaration
```c
int x;                    // global variable
int y = 10;               // global variable with initializer
static int z = 20;        // static variable

void func() {
    int local_var;        // local variable
    int init_var = 30;    // local variable with initializer
    static int local_static = 40;  // static local variable
}
```

#### Constants
```c
const int MAX_SIZE = 100;
const float PI = 3.14159;
```

### 3. Operators

#### Arithmetic operators
```c
+    // addition
-    // subtraction
*    // multiplication
/    // division
%    // modulo
++   // increment
--   // decrement
```

#### Relational operators
```c
==   // equal to
!=   // not equal to
<    // less than
<=   // less than or equal to
>    // greater than
>=   // greater than or equal to
```

#### Logical operators
```c
&&   // logical AND
||   // logical OR
!    // logical NOT
```

#### Bitwise operators
```c
&    // bitwise AND
|    // bitwise OR
^    // bitwise XOR
~    // bitwise NOT
<<   // left shift
>>   // right shift
```

#### Assignment operators
```c
=    // assignment
+=   // add and assign
-=   // subtract and assign
*=   // multiply and assign
/=   // divide and assign
%=   // modulo and assign
&=   // bitwise AND and assign
|=   // bitwise OR and assign
^=   // bitwise XOR and assign
<<=  // left shift and assign
>>=  // right shift and assign
```

#### Other operators
```c
&    // address-of
*    // dereference
.    // struct member access
->   // struct member access through a pointer
sizeof // size of a type
```

### 4. Type conversion and promotion

#### Implicit type conversion
- **Integer promotion**: inside expressions, `char` and `short` are automatically promoted to `int`
- **Floating-point promotion**: in mixed-type expressions, integers are automatically converted to floating point
- **Assignment conversion**: on assignment, the type of the right-hand expression is automatically converted to the type of the left-hand variable

#### Explicit type conversion (casts)
```c
int x = 10;
float y = (float)x;      // int to float
int z = (int)3.14;       // float to int
unsigned int u = (unsigned int)-10;  // signed to unsigned
```

#### Type promotion rules
1. If either operand is `float`, the result is `float`
2. If either operand is `double`, the result is `double`
3. Otherwise integer promotion applies and the result type is chosen by type rank
4. Type ranks (low to high):
   - `char` / `unsigned char`
   - `short` / `unsigned short`
   - `int` / `unsigned int`
   - `long` / `unsigned long`

### 5. Control-flow statements

#### Conditional statements
```c
// if-else
if (condition) {
    // statement block
} else if (condition2) {
    // statement block
} else {
    // statement block
}

// ternary operator
int result = condition ? value1 : value2;
```

#### Loops
```c
// while loop
while (condition) {
    // loop body
}

// do-while loop
do {
    // loop body
} while (condition);

// for loop
for (int i = 0; i < 10; i++) {
    // loop body
}
```

#### Jump statements
```c
break;      // leave the loop or switch
continue;   // continue with the next iteration
return;     // return a value from the function
goto label; // jump to a label
```

#### switch statement
```c
switch (expression) {
    case constant1:
        // statement block
        break;
    case constant2:
        // statement block
        break;
    default:
        // statement block
        break;
}
```

### 6. Functions

#### Function definition
```c
// function declaration
int add(int a, int b);

// function definition
int add(int a, int b) {
    return a + b;
}

// function with no return value
void print_message(const char* msg) {
    // function body
}

// variadic function (limited support)
int printf(const char* format, ...);
```

#### Function calls
```c
int result = add(10, 20);
print_message("Hello, World!");
```

#### Recursive functions
```c
int factorial(int n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}
```

#### Interrupt service routines (interrupt)
The `interrupt` keyword declares a function as an interrupt service routine (ISR). The compiler saves and restores all general-purpose registers automatically and returns with `IRET` instead of `RET`.

```c
// interrupt service routine: saves all registers, returns with IRET
interrupt void timer_isr(void) {
    // interrupt handling code
    // the compiler generates: PUSH R0..R11, PUSH R12, PUSH R15
    // restore: POP R15, POP R12, POP R0..R11, IRET
}

// interrupt can be combined with static
interrupt static void keyboard_isr(void) {
    // an ISR visible only inside this file
}
```

**Notes**:
- An ISR cannot take parameters (it must be `void`)
- Keep ISRs short (interrupt handling time is limited)
- The compiler manages saving and restoring all registers automatically
- The function's vector address must be registered in VML assembly through the `.vectors` directive

### 7. Preprocessor directives

#### File inclusion
```c
#include "vmlib.h"    // user header
#include <stdio.h>    // system header (limited support)
```

#### Macro definitions
```c
#define MAX_SIZE 100
#define MIN(a, b) ((a) < (b) ? (a) : (b))

#ifdef DEBUG
    #define DEBUG_PRINT(msg) printf("DEBUG: %s\n", msg)
#else
    #define DEBUG_PRINT(msg)
#endif
```

#### Conditional compilation
```c
#if defined(WIN32)
    // Windows-specific code
#elif defined(LINUX)
    // Linux-specific code
#else
    // code for other platforms
#endif
```

### 8. Structs and enums

#### Structs
```c
// struct definition
struct Point {
    int x;
    int y;
};

// struct variable declarations
struct Point p1;
struct Point p2 = {10, 20};

// struct member access
p1.x = 5;
p1.y = 15;

// struct pointer
struct Point* ptr = &p1;
ptr->x = 100;
```

#### Enums
```c
// enum definition
enum Color {
    RED,
    GREEN,
    BLUE
};

// enum variable
enum Color c = RED;
```

### 9. Pointers and arrays

#### Pointers
```c
int x = 10;
int* ptr = &x;      // take the address
int y = *ptr;       // dereference

// pointer arithmetic
int arr[10];
int* p = arr;
p++;                // move the pointer forward
p--;                // move the pointer backward
```

#### Arrays
```c
// array declaration and initialization
int arr1[10];
int arr2[5] = {1, 2, 3, 4, 5};
int arr3[] = {1, 2, 3};  // size inferred automatically

// partial initialization (unspecified elements become 0)
int arr4[10] = {1, 2, 3};  // arr4[0]=1, arr4[1]=2, arr4[2]=3, arr4[3..9]=0

// array access
arr1[0] = 100;
int value = arr2[2];

// multi-dimensional arrays
int matrix[3][3] = {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
};

// partial initialization of a multi-dimensional array
int matrix2[2][3] = {
    {1, 2},     // the third column becomes 0
    {4, 5, 6}
};

// nested array initialization
int cube[2][2][2] = {
    {
        {1, 2},
        {3, 4}
    },
    {
        {5, 6},
        {7, 8}
    }
};
```

### 10. Storage classes and extension keywords

#### Automatic variables (auto)
```c
void func() {
    auto int x = 10;  // auto can be omitted
    int y = 20;       // auto by default
}
```

#### Register variables (register)
```c
void func() {
    register int counter = 0;  // hint the compiler to use a register
}
```

#### Static variables (static)
```c
static int global_static = 100;  // file scope

void func() {
    static int local_static = 0;  // function scope, keeps its value
    local_static++;
}
```

#### External variables (extern)
```c
extern int external_var;  // declare an external variable

void func() {
    external_var = 100;   // use the external variable
}
```

#### Interrupt service routines (interrupt)
```c
interrupt void isr_name(void) {
    // interrupt handling code
    // the compiler saves R0-R11 automatically and returns with IRET
}
```
See "Interrupt service routines" in section 6, "Functions", for details.

## Compiler limitations

### Unsupported features
1. **Bit fields**: struct bit fields are not supported
2. **Complex pointer arithmetic**: pointer arithmetic is limited to simple addition and subtraction
3. **Function pointers**: the declaration syntax is supported (v1.65.35), but runtime calls are limited
4. **Variable-length arrays (VLA)**: C99 variable-length arrays are not supported
5. **Complex type qualifiers**: `restrict`, `_Atomic` and similar are not supported
6. **The `unsigned` modifier**: `unsigned int` / `unsigned char` are treated as `int` / `char` (VML has no notion of unsigned)

### v1.65.35 milestone: compiling SQLite
- **Parsing**: the whole SQLite amalgamation parses (255,636 lines, ~475K tokens)
- **Compiling**: both the SQLite stub and the full version compile and run
- **Runtime verification**: sqlite3_open / CREATE TABLE / INSERT / SELECT / sqlite3_close all pass
- **Type-system improvements**: anonymous struct members, double-pointer types, function-pointer declarations

### VML extension keywords
On top of standard C99 this compiler adds the following extension keywords:

| Keyword | Purpose | Description |
|--------|------|------|
| `interrupt` / `__interrupt` | function declaration | declares an interrupt service routine; registers are saved automatically and the routine returns with IRET |
| `__chipasm__` | inline assembly block | inserts multiple lines of VML assembly |
| `asm` | inline assembly | inserts a single line of VML assembly |
| `__stdcall` | calling convention | stdcall convention (arguments pushed right to left, callee cleans up) |
| `__fastcall` | calling convention | fastcall convention (the first 2 arguments are passed in registers) |
| `__cdecl` | calling convention | cdecl convention (arguments pushed right to left, caller cleans up) |
| `__builtin_va_start` | varargs | start of a variadic function |
| `__builtin_va_arg` | varargs | access a variadic argument |
| `__builtin_va_end` | varargs | end of variadic access |
| `__builtin_va_copy` | varargs | copy variadic state |

### Partially supported features
1. **Preprocessor**: supports `#include`, `#define`, `#undef` and conditional compilation (`#if` / `#ifdef` / `#ifndef` / `#else` / `#elif` / `#endif`)
2. **Standard library**: supports `stdio.h`, `stdlib.h`, `string.h`, `math.h`, `conio.h`, `graphics.h`
3. **Floating point**: basic floating-point operations (`+ - * /`, comparisons, casts)
4. **Structs**: basic structs, struct member pointers (`char *buf`), nested structs
5. **Unions**: the union type is supported
6. **Array initialization**: the full one-dimensional and nested multi-dimensional initialization syntax
7. **Inline assembly**: `asm("instruction")` single-line and `__chipasm__ { }` multi-line syntax
8. **Multi-dimensional arrays**: declaration, access and nested initialization
9. **Calling conventions**: `__stdcall` / `__fastcall` / `__cdecl`
10. **Suffixes**: the constant suffixes (`L`, `U`, `F`) are only fully supported in some contexts

## Compilation pipeline

### 1. Preprocessing
- Handle `#include` directives
- Expand macro definitions
- Handle conditional compilation
- Strip comments

### 2. Lexical analysis
- Turn the source into a token stream
- Recognize keywords, identifiers, constants, operators and so on

### 3. Syntax analysis
- Build an abstract syntax tree (AST)
- Check syntactic correctness
- Build the symbol table

### 4. Code generation
- Walk the AST and emit VML instructions
- Allocate storage for variables
- Generate function-call code
- Optimize the generated code

## Examples

### Simple program
```c
#include "vmlib.h"

int main() {
    vga_clear();
    vga_puts("Hello, C Compiler!");
    return 0;
}
```

### Computing a factorial
```c
#include "vmlib.h"

int factorial(int n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}

int main() {
    int result = factorial(5);
    
    vga_clear();
    vga_puts("Factorial of 5 is: ");
    // you still need to convert the number to a string to display it
    return 0;
}
```

### Array operations
```c
#include "vmlib.h"

int main() {
    int arr[5] = {1, 2, 3, 4, 5};
    int sum = 0;
    
    for (int i = 0; i < 5; i++) {
        sum += arr[i];
    }
    
    vga_clear();
    vga_puts("Sum of array: ");
    // display the result
    return 0;
}
```

### Multi-dimensional array initialization
```c
#include "vmlib.h"

int main() {
    // two-dimensional array initialization
    int matrix[3][3] = {
        {1, 2, 3},
        {4, 5, 6},
        {7, 8, 9}
    };
    
    // sum the matrix diagonal
    int diag_sum = 0;
    for (int i = 0; i < 3; i++) {
        diag_sum += matrix[i][i];
    }
    
    vga_clear();
    vga_puts("Diagonal sum: ");
    // display the result
    return 0;
}
```

### Complex array test
```c
#include "vmlib.h"

// exercise the various ways to initialize an array
int main() {
    // automatic size inference
    int auto_arr[] = {10, 20, 30, 40, 50};
    
    // partial initialization
    int partial[10] = {1, 2, 3};  // the last 7 elements are 0
    
    // multi-dimensional array
    int cube[2][2][2] = {
        {{1, 2}, {3, 4}},
        {{5, 6}, {7, 8}}
    };
    
    vga_clear();
    vga_puts("Array tests completed");
    return 0;
}
```

## Compiler options

### Command-line arguments
```bash
# basic usage
dotnet run --project VMLPrepares\\CCompiler input.c -o output.vml

# specify the library path
dotnet run --project VMLPrepares\\CCompiler -I Lib\c input.c -o output.vml

# preprocess only
dotnet run --project VMLPrepares\\CCompiler -E input.c

# specify the library path through an environment variable
$env:VML_C_LIB="Lib\c"
dotnet run --project VMLPrepares\\CCompiler input.c -o output.vml
```

### Environment variables
- `VML_C_LIB`: search path for C library files

## Error handling

The compiler reports the following kinds of errors:

### Syntax errors
- Missing semicolons, unbalanced parentheses, and so on
- The error location and the error message

### Semantic errors
- Undeclared identifiers
- Type mismatches
- Duplicate definitions

### Preprocessor errors
- Header file not found
- Macro definition errors

## C99 standard conformance

This compiler aims at full C99 conformance. About 97% of the core C99 feature set is implemented today:

### Core features implemented
1. **Basic syntax**: variable declarations, expressions, control structures
2. **Data types**: `int`, `char`, `float`, `double`, `void`, pointers, arrays, structs, enums
3. **Control flow**: `if-else`, `while`, `do-while`, `for`, `switch`, `break`, `continue`, `return`, `goto`
4. **Functions**: definitions, declarations, calls, recursion, parameter passing, implicit int return type
5. **Preprocessor**: `#include`, `#define`, `#ifdef`, `#ifndef`, `#if`, `#else`, `#elif`, `#endif`
6. **Arrays**: one-dimensional and multi-dimensional declaration, nested initialization, element access
7. **Structs**: global struct definitions, variable declarations, member access, struct member pointers
8. **Unions**: the union type is supported
9. **Comma expressions**: the comma operator is supported
10. **Comments**: both `/* */` and `//` styles

### VML extension features
1. **Interrupt service routines**: the `interrupt` keyword declares an ISR; all registers are saved automatically and it returns with IRET
2. **Inline assembly**: `asm("instruction")` single-line and `__chipasm__ { }` multi-line VML instructions
3. **Calling conventions**: `__stdcall` / `__fastcall` / `__cdecl` for cross-language interop
4. **Cross-language interop**: `POKE` / `PEEK` memory-mapped I/O (MMIO)

### Key features still missing (planned)
1. **Type system**: full support for the `unsigned` type
2. **Standard library**: finish `stdio.h` file operations and all of `math.h`
3. **Advanced features**: function pointers, bit fields
4. **Complex pointer arithmetic**: more complete pointer arithmetic and casts
5. **Wide characters**: `wchar_t` and the related functions

### Differences from standard C
1. **Memory model**: the memory model of the VML virtual machine
2. **Standard library**: the VML runtime library instead of the standard C library
3. **System calls**: I/O goes through the VML runtime
4. **Integer size**: VML's 32-bit integers
5. **Floating-point precision**: VML's floating-point representation

## Floating-point and 64-bit compile modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compile parameters:

| Parameter | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit floating point (float) handling |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit floating point (double) handling |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integers (long long) handling |

### 32-bit floating point (float32)

The 32-bit single-precision type `float` compiles as follows:

- **`hard` mode (default)**: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly on the sixteen floating-point registers F0-F15. Best performance; suited to targets with floating-point hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point operations through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms with no floating-point hardware.
- **`none` mode**: disables all 32-bit floating-point types; a `float` declaration is reported as a compile error.

### 64-bit floating point (double)

The 64-bit double-precision type `double` compiles as follows:

- **`soft` mode (default)**: uses the IEEE 754 double-precision emulation library `softdouble.c`, through `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` and similar. Works on every platform, MCU included.
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating on the eight double-precision registers D0-D7. Requires the target platform to support 64-bit operations.
- **`none` mode**: disables all 64-bit floating-point types; a `double` declaration is reported as a compile error.

### 64-bit integers (int64)

The 64-bit integer type `long long` compiles as follows:

- **`soft` mode (default)**: uses the two-register software emulation library `softint64.c`, through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- **`hard` mode**: uses the native VML 64-bit integer instructions `MOVEL`/`ADDL`/`SUBL`/`MULL`/`DIVL`/`MODL`/`NEGL`/`CMPL`/`ANDL`/`ORL`/`XORL`/`NOTL`/`SHLL`/`SHRL`, operating on the eight long-integer registers L0-L7. Available since v1.65.197+.
- **`none` mode**: disables the 64-bit integer type; a `long long` declaration is reported as a compile error.

### Software emulation libraries

These emulation libraries all live in `Lib/shared/`, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer two-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## Performance considerations

1. **Code size**: the generated VML code is larger than native code
2. **Execution speed**: it runs inside the VML virtual machine, which is slower than native code
3. **Memory use**: it uses the memory space of the VML virtual machine
4. **Optimization level**: currently basic, which produces fairly direct code

## Extensibility

The compiler is designed around an extensible architecture:

1. **Adding new syntax**: the lexer and parser can be extended
2. **Optimization passes**: code-optimization stages can be added
3. **Target platforms**: support for other target architectures can be added
4. **Language features**: more C features can be added incrementally

## Related files

- `CCompiler.cs`: the main compiler class
- `Lexer.cs`: the lexer
- `Parser.cs`: the parser
- `CodeGenerator.cs`: the code generator
- `Preprocessor.cs`: the preprocessor
- `Program.cs`: the command-line interface
- `README.md`: the project readme

---

## 🆕 String types (v1.65.19)

The C compiler supports string types in three widths:

| Literal | Type | Width | VML directive | Terminator |
|:-------|:-----|:----:|:----------|:------|
| `"str"` | `char*` | 8-bit | `.string` | `0x00` |
| `L"str"` | `wchar_t*` | 16-bit | `.wstring` | `0x0000` |
| `U"str"` | `char32_t*` | 32-bit | `.ustring` | `0x00000000` |

```c
char     *s1 = "Hello";           // .string  (UTF-8)
wchar_t  *s2 = L"你好";           // .wstring (UTF-16LE, BMP only)
char32_t *s3 = U"🎉";             // .ustring (UTF-32LE, full Unicode)

wchar_t  c1 = L'字';              // 16-bit wide character
char32_t c2 = U'🎉';              // 32-bit Unicode character
```

**Predefined macro**: `VML_WSTRING` — defined as 1 in OS mode, undefined in MCU mode
**Standard headers**: `<wchar.h>` / `<uchar.h>` automatically link `Lib/shared/wchar.vml` / `uchar.vml`
**Output**: `shared_print_wstr(wstr)` — converts UTF-16LE→UTF-8 and calls SYSCALL #1

---

## 🆕 Extended preprocessor directives (v1.65.19)

### `#param` — in-source configuration directives

| Directive | Description | Example |
|:-----|:-----|:-----|
| `#param lib("xxx")` | automatically link the given library file | `#param lib("wchar")` |
| `#param path("xxx")` | add an include search path | `#param path("./inc")` |
| `#param prefix("xxx")` | set the function-name prefix | `#param prefix("python_")` |

### `#param prefix` — build a multi-language library from one source

With `#param prefix`, or the command line `-D VML_PREFIX=xxx`, one C source file can produce libraries with different prefixes for different languages:

```c
// wstring.c — one source file, shared by many languages
#param prefix("python_")    // uncomment this line when building for Python
// #param prefix("java_")   // uncomment this line when building for Java
// #param prefix("js_")     // uncomment this line when building for JavaScript

size_t wcslen(const wchar_t *s) { ... }   // → python_wcslen
wchar_t *wcscpy(wchar_t *d, const wchar_t *s) { ... }  // → python_wcscpy
```

**Build commands**:
```bash
# build python_wcslen, python_wcscpy ... for Python
vmltool wstring.c -D VML_PREFIX=python_ -o python/wstring.vml

# build java_wcslen, java_wcscpy ... for Java
vmltool wstring.c -D VML_PREFIX=java_ -o java/wstring.vml

# build js_wcslen, js_wcscpy ... for JavaScript
vmltool wstring.c -D VML_PREFIX=js_ -o js/wstring.vml
```

**Prefix replacement rules**:
- `shared_xxx` → `{prefix}xxx` (replaces the `shared_` prefix)
- `vml_xxx` → `{prefix}xxx` (replaces the `vml_` prefix)
- any other function name → `{prefix}` + the original name (the prefix is simply added)

### Multi-language library build script

```bash
#!/bin/bash
# build_all_langs.sh — build the wstring library for every language
for lang in python java js kotlin swift go csharp; do
  vmltool wstring.c -D "VML_PREFIX=${lang}_" -o "${lang}/wstring.vml" --no-link
done
```

### `#param lib` — automatic library linking

```c
// a header such as wstring.h declares its dependencies
#param lib("wchar")     // automatically links wchar.vml

// the user only needs #include "wstring.h"
// the compiler links wstring.vml + wchar.vml automatically
```

Combined with `#ifdef VML_WSTRING` you can choose the encoding per mode.

### `#param path` — extra include paths

```c
#param path("./include")      // add a relative search path
```

---

## Calling conventions (v1.65.19)

The C compiler supports three calling conventions, selected with a function modifier:

| Modifier | Argument passing | Stack cleanup | Variadic |
|:-------|:--------|:------|:--:|
| `__cdecl` (default) | pushed right to left, R0-R3 carry the first 4 arguments | caller | ✅ |
| `__stdcall` | pushed right to left | callee | — |
| `__fastcall` | R0-R3 carry the first 4 arguments, the rest are pushed | callee | — |

### `__cdecl` variadic functions

```c
// variadic printf
__cdecl void shared_printf(const char *fmt, ...) {
    char buf[512];
    int *stack_args = (int*)(&fmt + 1);  // the variadic arguments follow fmt
    int len = shared_vsnprintf(buf, fmt, stack_args, 8);
    buf[len] = 0;
    asm("SYSCALL #1");  // output to the console
}

// call: any number of arguments (up to 8, limited by the VML stack)
shared_printf("Int: %d  Hex: %x  Str: %s\n", 42, 255, (int)"Hello");

// variadic sprintf
__cdecl int shared_sprintf(char *buf, const char *fmt, ...) {
    int *stack_args = (int*)(&fmt + 1);
    return shared_vsnprintf(buf, fmt, stack_args, 8);
}
```

**How variadic arguments work**:
- `__cdecl` pushes arguments from right to left
- `fmt` is the fixed parameter closest to the top of the stack
- `&fmt + 1` points at the first variadic argument on the stack
- the stack pointer lets you read every variadic argument directly

### `__stdcall` fixed parameters

```c
// the callee cleans up the stack, Windows API style
__stdcall void shared_print_str(const char *str) {
    asm("SYSCALL #1");
}
```

### `__fastcall` fast calls

```c
// the first 4 arguments go in registers; good for performance-sensitive code
__fastcall int add_four(int a, int b, int c, int d) {
    return a + b + c + d;
}
```

**Note**: the `__cdecl` / `__stdcall` / `__fastcall` modifiers go on the **function definition**. On the calling side the compiler picks the right convention from the definition, so no explicit declaration is needed.
