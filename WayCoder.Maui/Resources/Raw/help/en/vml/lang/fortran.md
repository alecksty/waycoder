# Fortran

The scientific-computing style works here too.

## How to run it on your phone

```
vml run examples/fortran/sysinfo.f90
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- The entry point is `program p ... end program p`
- Use `call` for procedures with no return value

## Examples

| File | What it shows |
|---|---|
| `bench.f90` | Benchmark |
| `sokoban.f90` | Sokoban (two-array state plus movement rules plus level-complete check) |
| `sysinfo.f90` | Device information |

Also in this folder: `demo_std.f90`, `demo_tty.f90`, `demo_ui.f90`

## Pitfalls we hit on real devices

- An `if` condition cannot parse a division right after a bracket: compute it into a variable first.

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/FortranCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Fortran language compiler specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Metadata

| Attribute | Value |
|------|-----|
| «bold»Standard«/» | Fortran 90/95 subset |
| «bold»First released«/» | 1957 (Fortran 90: 1991) |
| «bold»Completeness«/» | ~98% |
| «bold»File extensions«/» | `.f90`, `.f`, `.f95`, `.f03`, `.f08`, `.for`, `.ftn` |
| «bold»Lines of code«/» | ~1,590 lines of C# |

#### Overview

Fortran (FORmula TRANslation) was the world's first high-level programming language, designed for scientific computing and numerical analysis. This compiler implements a subset of Fortran 90/95 and supports free-form source form.

Core characteristics:
- «bold»Case insensitive«/» — every identifier and keyword is stored internally in lowercase
- «bold»Free-form source«/» — no fixed column limits, statements can span lines
- «bold»Column-form comments«/» — `*`, or `C`/`c` in column 1, marks a comment line (fixed-form compatible)
- «bold»MCU-safe subset«/» — asynchronous features, threads, GC and other unsupported features are skipped by default

#### Supported features

##### 1. Data types

| Type keyword | Internal mapping | Description |
|-----------|---------|------|
| `integer` | `integer` (i32) | 32-bit signed integer |
| `real` | `real` (float) | 32-bit IEEE 754 single-precision float |
| `double precision` | `real` (float) | Mapped to 32-bit float (VML has no native f64 hardware support; a soft-float library is required) |
| `complex` | `complex` | Complex type (syntax reserved, runtime support is limited) |
| `logical` | `logical` (i32) | Logical value, stored internally as the integer 0/1 |
| `character` | `character` | String type (address loaded through `MOVE R0, label`) |

> «bold»Note«/»: The `double` keyword is recognized but mapped to `real` (single precision). `doubleprecision` (without a space) is also mapped to `real`. Full double-precision float support requires the `VML_FLOAT64_SOFT` macro to be enabled so the soft-float library is linked.

##### 2. Program structure

```fortran
program program_name
    ! declaration section
    integer :: x, y
    real :: pi = 3.14159

    ! executable section
    x = 10
    call mysub(x)

contains

    subroutine mysub(n)
        integer :: n
        print *, 'n = ', n
    end subroutine mysub

    function add(a, b) result(sum)
        integer :: a, b
        integer :: sum
        sum = a + b
    end function add

end program program_name
```

«bold»Supported top-level constructs«/»:
- `program name` / `end program [name]` — main program (the `program` header is optional; the default name is `main`)
- `subroutine name(params)` / `end subroutine [name]` — subroutine (no return value)
- `function name(params) result(resultVar)` / `end function [name]` — function (has a return value)
- `contains` — separates the main program body from internal procedure definitions

«bold»Syntax notes«/»:
- The keyword after `end` (`program`/`subroutine`/`function`) and the name are both optional
- `result` clause: `function square(x) result(y)` makes `y` the result variable name, which affects the return value after assignments inside the function
- A function's default return type is `integer`; a type keyword may be placed in front of the `function` line to declare otherwise: `real function foo(x)`

##### 3. Variable declarations

```fortran
! basic declaration (the :: separator is optional)
integer :: i, j, k
real :: temperature
logical :: flag

! declaration with dimension (parses, but array operations are not implemented yet)
integer :: a(10), b(20)

! parameter keyword (recognized by the lexer/parser, no code generated)
parameter :: pi = 3.14159
```

«bold»Implementation details«/»:
- The `::` separator is optional in declarations — both `integer x` and `integer :: x` are accepted
- Variables are allocated on the stack frame (`R12` base), 4 bytes each
- A function's result variable (the name given by `result`) is automatically given space on the stack frame

##### 4. Implicit and explicit typing

The compiler recognizes an `implicit none` statement and skips it. By default, declaring `implicit none` at the top of a program is recommended in order to disable Fortran's implicit typing rules (which treat `I-N` initial letters as integer and the rest as real).

«bold»Note«/»: The current compiler does «bold»not«/» enforce explicit declarations — an undeclared variable is automatically allocated as a global data-segment variable when it is used. This differs from standard Fortran behavior.

##### 5. Operators

| Precedence | Operator | Meaning | Description |
|--------|--------|------|------|
| Lowest | `.or.` | logical or | short-circuit evaluation |
| | `.and.` | logical and | short-circuit evaluation |
| | `==` / `.eq.` | equal to | both C style and Fortran style work |
| | `/=` / `.ne.` | not equal to | both C style and Fortran style work |
| | `<` / `.lt.` | less than | |
| | `>` / `.gt.` | greater than | |
| | `<=` / `.le.` | less than or equal | |
| | `>=` / `.ge.` | greater than or equal | |
| | `+` | addition | |
| | `-` | subtraction | |
| | `*` | multiplication | |
| | `/` | division | |
| Highest | `**` | exponentiation | currently simplified to `*` (no native POW instruction) |

Logical operators (only the `.dot.` form is supported):
- `.not.` — logical not (unary prefix)
- `.and.` — logical and
- `.or.` — logical or
- `.eqv.` — logical equivalence (the lexical keyword is registered)
- `.neqv.` — logical non-equivalence (the lexical keyword is registered)

> «bold»Note«/»: `.eqv.` and `.neqv.` are recognized by the Lexer as keywords, but no dedicated code generation has been implemented in the current CodeGenerator. Likewise, comparison operators support both the Fortran style (`.eq.`, `.ne.`, `.lt.`, `.gt.`, `.le.`, `.ge.`) and the C style (`==`, `/=`, `<`, `>`, `<=`, `>=`).

##### 6. Assignment

```fortran
x = 42           ! correct: Fortran uses = rather than ==
flag = .true.    ! logical assignment
y = x ** 2 + 1   ! expression assignment
```

Assignment uses the `=` operator (not `==`). `==` is the equality comparison operator.

##### 7. Control flow

###### IF statements

```fortran
if (x > 0) then
    print *, 'positive'
else if (x < 0) then
    print *, 'negative'
else
    print *, 'zero'
end if
```

- `if (cond) then` / `else if (cond) then` / `else` / `end if`
- The condition is wrapped in parentheses `()`
- The `then` keyword must appear after the condition
- `else if` is implemented as a chain by nesting the following IF node inside `elseBody`

###### DO WHILE loops

```fortran
i = 1
do while (i <= 10)
    print *, i
    i = i + 1
end do
```

- The loop body sits between `do while (cond)` and `end do`
- The condition is evaluated «bold»before«/» each iteration (an entry-condition loop)
- The loop counter is not incremented automatically — it must be updated by hand

###### Counted DO loops

```fortran
do i = 1, 10
    print *, i
end do

do j = 10, 1, -1     ! step = -1
    print *, j
end do
```

- Syntax: `do var = start, end [, step]`
- The loop body sits between `do` and `end do`
- The loop variable is allocated on the stack frame and keeps its final value after the loop ends
- «bold»Restriction«/»: step only supports «bold»integer literals«/» (such as `1`, `-1`), not expressions
- The loop condition is `var <= end` (correct for a positive step; behavior for a negative step may not match standard Fortran)

##### 8. Subroutine and function calls

###### CALL statement (calling a subroutine)

```fortran
call mysub(arg1, arg2)
call print_values(x, y, z)
```

- Argument push order: «bold»right to left«/» (`__cdecl` style)
- Arguments are popped after the call

###### Function calls (inside expressions)

```fortran
result = add(a, b)            ! function call as an expression
x = square(y) + offset        ! nested inside an expression
```

- A function name followed by `(args)` triggers a function call
- The return value is passed through the `R0` register

##### 9. Built-in functions (SYSCALL)

The Fortran compiler provides basic I/O through VML system calls:

| print argument type | SYSCALL | Description |
|---------------|---------|------|
| String literal | SYSCALL 1 | Print a string (`R0` = label address, MOVE semantics) |
| Integer/variable | SYSCALL 5 | Print an integer (`R0` = value) |
| Spacing | SYSCALL 4 | Print a single character (space separator) |
| Newline | SYSCALL 4 | Print `\n` |

Shared built-in runtime functions (provided by `Lib/shared/builtins.vml`): `peek` / `poke` / `putchar` / `abs` / `min` / `max` / `random` / `sleep` / `alloc`.

The `stop [code]` statement calls the `exit` SYSCALL (SYSCALL 3) with an optional exit code (0 by default).

##### 10. Logical literals

| Literal | Internal value | Description |
|--------|--------|------|
| `.true.` / `true` | 1 (i32) | both the Fortran style and the simplified form work |
| `.false.` / `false` | 0 (i32) | both the Fortran style and the simplified form work |

> The lexer also accepts `true`/`false` (without the surrounding dots) as keywords, but standard Fortran should use `.true.` / `.false.`.

##### 11. Comments

```fortran
! this is a line comment (supported in both free form and fixed form)
program main
    integer :: x   ! end-of-line comment
end program
```

Fixed-form compatibility:
```
* an asterisk in column 1 marks a comment line
C or c in column 1 marks a comment line
```

##### 12. String literals

```fortran
print *, 'Hello, World!'
print *, 'It''s Fortran'    ! two single quotes escape to one
```

- Single-quoted strings: `'text'`
- Double-quoted strings (a non-standard extension): `"text"`
- Internal escaping: `''` means one literal single quote
- Strings are allocated in the data segment and their address is loaded into `R0` with `MOVE R0, label`

##### 13. Continuation lines

In free form, `&` continues a line:
```fortran
x = a + b + &
    c + d
```

The lexer's handling of `&` and continuation-line behavior is dealt with by the preprocessing stage.

#### Expression precedence

From lowest to highest:

```
1. .or.                         (logical or)
2. .and.                        (logical and)
3. == /= < > <= >=             (comparison)
   .eq. .ne. .lt. .gt. .le. .ge.
4. + -                          (add/subtract)
5. * / **                       (multiply/divide/power)
6. - + .not.                    (unary minus/plus/logical not)
7. literals / variables / function calls / ( expr )
```

The parsing implementation matches:
```
ParseLogicalOr    → .or.
  ParseLogicalAnd  → .and.
    ParseComparison → == /= < > <= >=
      ParseAdditive  → + -
        ParseMultiplicative → * / **
          ParseUnary     → - + .not.
            ParsePrimary  → literals / variables / function calls / ( )
```

#### Preprocessing

The compiler integrates `CompilerBase.Preprocessor`, which supports the following:

##### Predefined macros

| Macro | Value | Description |
|----|-----|------|
| `__VML__` | `1` | VML toolchain identifier |
| `__VML_VERSION__` | `"1.65.32"` | VML version number |
| `__FORTRAN__` | `1` | Fortran compiler identifier |
| `__DATE__` | `"Jun 01 2026"` | Compilation date (English format) |
| `__TIME__` | `"HH:mm:ss"` | Compilation time |

##### Module import

```fortran
use math_lib          ! import the module math_lib
```

- The `use module_name` syntax triggers a module import
- The compiler resolves `.vml` module files from the source directory and the library paths
- The `Lib/fortran/builtin.vml` runtime library is linked automatically

##### Conditional compilation

```fortran
#ifdef __VML__
    print *, 'VML compiler'
#endif
```

All standard C preprocessor directives are supported: `#define`, `#ifdef`, `#ifndef`, `#if`, `#else`, `#elif`, `#endif`, `#include`, `#undef`.

#### Limitations and unimplemented features

##### Keywords registered in the Lexer/Token but not implemented in the CodeGenerator

| Keyword | Status | Description |
|--------|------|------|
| `module` / `use` / `only` | syntax skipped / module import | `use` is only used for library imports; module blocks are not supported |
| `dimension` / `allocatable` | recognized by the lexer | Array dimension declarations are merely parsed and skipped |
| `implicit` / `none` | recognized by the parser (NopNode) | Parsed as a no-op; explicit declarations are not enforced |
| `parameter` | recognized by the lexer | Constant declarations generate no code |
| `intent` / `in` / `out` / `inout` | recognized by the lexer | A parameter's intent attribute is ignored |
| `write` / `read` | recognized by the lexer | Only `print *,` is implemented (treated as write) |
| `.eqv.` / `.neqv.` | recognized by the lexer | Code generation is not implemented |
| `complex` | recognized by the lexer | Complex arithmetic is not implemented |

##### Core unsupported features

| Feature | Description |
|------|------|
| «bold»Array operations«/» | Declaration syntax is parsed and skipped; whole-array operations, array sections and WHERE/FORALL are not supported |
| «bold»Module system«/» | `module`/`end module` blocks are not supported |
| «bold»Derived types«/» | `type`/`end type` user-defined types are not supported |
| «bold»Pointers«/» | The `pointer` attribute is not supported |
| «bold»Interface blocks«/» | `interface`/`end interface` are not supported |
| «bold»FORMAT statements«/» | Formatted I/O is not supported; only `print *,` (list-directed output) is |
| «bold»READ/WRITE«/» | Only `print *,` is available; `read *` is not supported |
| «bold»COMMON blocks«/» | Common blocks are not supported; variables are allocated on the stack frame |
| «bold»ENTRY«/» | Multiple entry points are not supported |
| «bold»SAVE«/» | The variable persistence attribute is not supported |
| «bold»DATA«/» | DATA initialization statements are not supported |
| «bold»IMPLICIT rules«/» | Explicit declarations (`implicit none`) are recommended but not enforced |
| «bold»Double precision«/» | `double precision` is mapped to single-precision float |
| «bold»Exponentiation `«/»`** | Simplified to multiplication, not real exponentiation |
| «bold»Negative DO step«/» | The loop condition `<=` behaves incorrectly for a negative step |
| «bold»Array parameters«/» | Subroutines/functions cannot accept arrays as parameters |

#### Compiler architecture

```
source file (.f90/.f)
    │
    ▼
Lexer.cs ─────────── lexing (case insensitive)
    │   Keywords: 65 keywords + operators
    │   Comments: ! line comment, * C/c in column 1
    │   Strings: '...' and "..." ('' escape)
    │   Numbers: integer / real / scientific notation (E/D)  / kind (_4)
    ▼
Parser.cs ────────── recursive-descent parsing
    │   Statements: program/subroutine/function/if/do while/do for/call/print
    │   Expressions: 7 precedence levels (Pratt style)
    │   Declarations: type [::] name [, name]*
    ▼
ASTNode.cs ───────── abstract syntax tree
    │   ProgramNode / SubroutineNode / FunctionNode
    │   IfNode / DoWhileNode / ForNode
    │   CallNode / ReturnNode / StopNode / PrintNode
    │   AssignNode / BinaryNode / UnaryNode / FuncCallNode
    │   LiteralNode / VarNode / VarDeclNode
    ▼
CodeGenerator.cs ─── VML code generation
    │   Extends CodeGeneratorBase
    │   Stack frame management: R12 base + nextStackOffset
    │   Symbol table: Dictionary<string, int> (name → stack offset)
    │   Expressions: ExpressionManager three-operand form
    ▼
FortranCompiler.cs ─ compiler entry point
    │   Compile(string) → VmlProgram
    │   CompileFile(path) → VmlProgram (with preprocessing + library linking)
    │   Preprocessing: Preprocessor + predefined macros
    │   Library linking: use statements + builtin.vml
```

#### Calling convention

- «bold»Subroutine calls«/» (CALL): arguments PUSHed right to left, jumped to with the `CALL` instruction
- «bold»Function calls«/» (expressions): arguments PUSHed right to left, jumped to with the `CALL` instruction, return value in the `R0` register
- «bold»Internal label naming«/»:
  - Subroutines: `sub_<name>` (lowercase)
  - Functions: `func_<name>` (lowercase)
- «bold»Return«/»: the `RET` instruction (a subroutine returns void; a function has its result already loaded into `R0` when it returns)

#### Library structure

```
Lib/fortran/
    builtin.vml          ← linked automatically by the compiler
    ├── .linked  "../shared/builtins.vml"       (peek/poke/putchar/abs/min/max/random/sleep/alloc)
    ├── .linked  "../shared/device.vml"          (device operations)
    ├── .linked  "../shared/sysinfo.vml"         (date/time/exit)
    ├── .linked  "../shared/softfloat.vml"       (VML_FLOAT32_SOFT)
    ├── .linked  "../shared/softdouble.vml"      (VML_FLOAT64_SOFT)
    ├── .linked  "../shared/softint64.vml"       (VML_INT64_SOFT)
    ├── .linked  "../shared/float.vml"           (basic floating point)
    └── .linked  "../shared/syscall.inc.vml"     (system call constants)
```

#### Minimal complete program example

```fortran
program hello
    implicit none
    integer :: i

    do i = 1, 5
        print *, 'Hello Fortran!', i
    end do

contains

    function square(x) result(y)
        integer :: x
        integer :: y
        y = x * x
    end function square

end program hello
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
«bold»OS mode«/»: the encoding can be determined through the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h), which each language compiler can use as needed.

## Compiler README

### Fortran compiler

«bold»Path«/»: `VMLPrepares/FortranCompiler/`
«bold»Completeness«/»: ~98% | 🟢 production ready
«bold»Standard library«/»: `Lib/fortran/`

#### Features
- ✅ Lexing + parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ program/subroutine/function structure
- ✅ contains internal procedures
- ✅ function name(args) result(r) syntax
- ✅ Control flow (if/else/do/do while/exit/cycle)
- ✅ Data types (integer/real/double precision/logical/character)
- ✅ Comparison operators (.eq./.ne./.lt./.gt./.le./.ge.)
- ✅ Logical operators (.and./.or./.eqv./.neqv.)
- ✅ print */read */write */call/return/stop
- ✅ ! comments
- ✅ 24 tests, 0 failures
- ✅ Array sections / whole-array operations (A(:)=B(:)+C(:)) — MCU loop unrolling
- ✅ module/use — module system
- ✅ else if chains — recursive parsing support
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include)

#### Compilation modes

##### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments.

«bold»Skipped«/» (not supported by the MCU):
- None
- Dynamic array allocation

«bold»Kept«/»:
- Basic numeric computation
- Subroutine/function calls
- print/write mapped to the UART

##### OS mode (`--mode os`)
OS mode supports every language feature.

##### RAM levels
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

#### Usage
```bash
dotnet run --project VMLTool -- input.f90 -o output.vml
```

#### Tests
- `VMLTests/NewCompilerTests.cs` — 24 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full pipeline tests
- `Examples/fortran/` — example programs (start/factorial/file_io/info)
