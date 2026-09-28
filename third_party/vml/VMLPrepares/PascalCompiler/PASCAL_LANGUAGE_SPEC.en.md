# Pascal language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | Turbo Pascal 7.0 (default) + 5 dialects |
| **CLI option** | `--pascaltype turbo\|delphi\|freepascal\|iso\|ucsd\|oberon` |
| **Release year** | 1992 (Turbo Pascal) |
| **Completeness** | ~92% |
| **MCU completeness** | ~90% |
| **Tests** | 0 (the test directory is yet to be created) |
| **Updates** | 2026-05-18: resolved the contradictions in the break/continue/pointer status; updated the test count |

## Keywords

`program` `unit` `uses` `var` `const` `type` `begin` `end` `procedure` `function`
`if` `then` `else` `while` `do` `for` `to` `downto` `repeat` `until`
`case` `of` `break` `continue` `exit` `goto` `label`
`array` `record` `set` `file` `text` `string` `integer` `real` `char` `boolean`
`true` `false` `nil` `and` `or` `not` `xor` `div` `mod` `in` `shl` `shr`
`with` `asm` `absolute` `external` `forward` `inline` `interrupt`

## Overview

This compiler compiles the Pascal language (compatible with a Turbo Pascal 7.0 subset) into VML (Virtual Machine Language) assembly. It supports Pascal's basic syntax, control flow, data types, procedures and functions, and the Crt console unit.

## Supported language features

### 1. Data types

#### Literals
- **Integer**: `42` (decimal)
- **Hexadecimal**: `$FF`, `$40013804` (the `$` prefix, Pascal style)
- **Real**: `3.14`, `1.5e-2`
- **Character**: `'A'`
- **String**: `'Hello'`, `"Hello"`
- **Boolean**: `true`, `false`

#### Basic data types
- **Integer type**: `integer` — 32-bit signed integer (fully implemented)
- **Real type**: `real` — floating point (implemented with MOVEF, FADD/FSUB/FMUL/FDIV, comparison)
- **Character type**: `char` — a single character (implemented with MOVEB)
- **Boolean type**: `boolean` — a boolean value (implemented as 0/1 produced by comparison)
- **String type**: `string` — string literals (implemented with SYSCALL 4/2)

#### Enumeration types
```pascal
type
  TDir = (Up, Down, Left, Right);
  TColor = (Red, Green, Blue);
```
Enumeration values compile to integer constants (increasing from 0).

#### Derived types
- **Arrays**: one-dimensional, with a declared bound, such as `array[1..10] of integer`
  - Runtime bounds checking is supported (an out-of-range index raises a runtime error)
  - Field access on record elements is supported: `snake[i].x`
- **Records**: `record` type definitions, with field access (the dot operator)
  - Fields may be basic types, strings or arrays (including multidimensional ones)
- **Sets**: the `set of` type, with union/intersection/difference/membership testing (implemented with a bitmap)
- **Files**: the `file of <type>` and `text` types

### 2. Program structure

#### Program declaration
```pascal
program HelloWorld;
uses Crt;  // use the Crt unit (parsed but ignored)
var
    x: integer;
begin
    x := 10;
    writeln('Hello, World!');
end.
```

#### Variable declarations
```pascal
var
    counter: integer;
    pi: real;
    ch: char;
    flag: boolean;
    arr: array[1..10] of integer;
    r: record
        x, y: integer;
    end;
```

#### Constant declarations
```pascal
const
    MAX_SIZE = 100;
    PI = 3.14159;
    MESSAGE = 'Hello';
```

#### Type definitions
```pascal
type
    TColor = (Red, Green, Blue);
    TRange = 1..100;
    TPoint = record
        x, y: integer;
    end;
    TMatrix = array[1..10] of array[1..10] of integer;
```

### 3. Control structures

#### Conditionals
```pascal
if x > 0 then
    writeln('Positive')
else if x = 0 then
    writeln('Zero')
else
    writeln('Negative');
```

#### Loop statements
```pascal
// while loop
while i <= 10 do
begin
    writeln(i);
    i := i + 1;
end;

// for loop
for i := 1 to 10 do
    writeln(i);

for i := 10 downto 1 do
    writeln(i);

// repeat loop
repeat
    writeln(i);
    i := i - 1;
until i = 0;
```

#### case statement
```pascal
case grade of
    'A': writeln('Excellent');
    'B', 'C': writeln('Pass');
    'D': writeln('Poor');
    'F': writeln('Fail');
otherwise
    writeln('Invalid');
end;
```

### 4. Procedures and functions

#### Procedure declaration
```pascal
procedure PrintMessage(msg: string);
begin
    writeln(msg);
end;
```

#### Function declaration
```pascal
function Add(a, b: integer): integer;
begin
    Add := a + b;  // the result is returned by assigning to the function name
end;
```

#### Parameter passing
- **Value parameters**: the default way of passing
- **Variable parameters**: use the `var` keyword; the callee may modify the caller's variable
```pascal
procedure Swap(var x, y: integer);
var
    temp: integer;
begin
    temp := x;
    x := y;
    y := temp;
end;
```

#### forward declarations
```pascal
procedure Foo(x: integer); forward;

procedure Bar;
begin
    Foo(10);
end;

procedure Foo(x: integer);
begin
    writeln(x);
end;
```

### 5. Operators

#### Arithmetic operators
- `+` — addition
- `-` — subtraction
- `*` — multiplication
- `/` — real division
- `div` — integer division
- `mod` — modulo

#### Relational operators
- `=` — equal
- `<>` — not equal
- `<` — less than
- `<=` — less than or equal
- `>` — greater than
- `>=` — greater than or equal

#### Logical operators
- `and` — logical AND
- `or` — logical OR
- `not` — logical NOT

#### Set operators
- `+` — union
- `*` — intersection
- `-` — difference
- `in` — membership test

### 6. Input and output

#### Standard output
```pascal
write('Hello');         // print without a newline
writeln('World');       // print and then a newline
writeln('Value: ', x);  // print several arguments
writeln(x:5);           // formatted output (width 5)
```

#### Standard input
```pascal
readln(x);              // read an integer
readln(ch);             // read a character
readln(str);            // read a string
```

#### File operations
```pascal
var
    f: text;
begin
    assign(f, 'data.txt');
    reset(f);           // open the file for reading
    rewrite(f);         // open the file for writing
    append(f);          // append to the file
    readln(f, x);       // read from the file
    writeln(f, x);      // write to the file
    close(f);           // close the file
end;
```

### 7. Standard library units

#### Crt unit
```pascal
uses Crt;

begin
    ClrScr;             // clear the screen
    GotoXY(10, 5);      // move the cursor
    WhereX; WhereY;     // read the cursor position
    TextColor(Red);     // set the text colour
    TextBackground(Blue); // set the background colour
    Delay(1000);        // wait 1 second
    Sound(440);         // start a tone
    NoSound;            // stop the tone
    if KeyPressed then  // check for a keypress
        ch := ReadKey;  // read a key
    NormVideo;          // restore the default video attributes
    HighVideo; LowVideo; // high/low intensity
    InsLine; DelLine;   // insert/delete a line
    Window(x1, y1, x2, y2); // define a window
end;
```

#### System unit (built in)
- `Abs(x)` — absolute value
- `Sqr(x)` — square
- `Sqrt(x)` — square root
- `Sin(x)`, `Cos(x)`, `Arctan(x)` — trigonometric functions
- `Exp(x)`, `Ln(x)` — exponential and logarithm
- `Round(x)`, `Trunc(x)` — rounding
- `Random`, `Randomize` — random numbers
- `Length(s)` — string length
- `Copy(s, start, count)` — substring
- `Concat(s1, s2)` — string concatenation
- `Pos(substr, s)` — find the position of a substring
- `Halt` — terminate the program

### 8. Special syntax features

#### Compound statements
```pascal
begin
    statement1;
    statement2;
end;
```

#### Comments
```pascal
// single-line comment
{ multi-line comment }
(* another kind of multi-line comment *)
```

#### Labels and goto
```pascal
label 100;
begin
    if x < 0 then goto 100;
    writeln('Positive');
    100: writeln('Done');
end;
```

### 9. VML code-generation conventions

#### Stack frame layout
```
caller: PUSH the arguments (left to right)
        CALL the function name

callee prologue:
    ENTER local_size     ; save R14, R12 -= local_size

stack layout (R14 = frame pointer):
    argument 1        <- R14+8
    argument 2        <- R14+12
    return address    <- R14+4  (implicit)
    saved R14         <- R14+0  (saved by the ENTER instruction)
    local variable 1  <- R14-4
    local variable 2  <- R14-8
    ...

callee epilogue:
    LEAVE               ; R12 = R14, POP R14
    RET                 ; return
```

#### Variable access
- **Global variables**: loaded from the data segment (a data-segment label)
- **Local variables**: accessed at a negative stack-frame offset, `[R14 - offset*4]`
- **Parameters**: accessed at a positive stack-frame offset, `[R14 + 8 + paramIndex*4]`
- **var parameters**: the parameter is an address and must be dereferenced

#### Function return values
- The return value is set by assigning to the function name
- The return value is stored in a local variable
- On return it is loaded into the R0 register

### 10. Current implementation status

| Feature | Status |
|------|------|
| Program structure (`program ...; ... end.`) | ✅ implemented |
| Comments (`{ }`, `(* *)`, `//`) | ✅ implemented |
| `uses` clause | ✅ implemented (parsed and skipped) |
| **Data types** | |
| `integer` (32-bit signed) | ✅ implemented |
| `real` (floating point) | ✅ implemented |
| `boolean` | ✅ implemented |
| `char` | ✅ implemented |
| `string` | ✅ implemented |
| Enumeration types `(A, B, C)` | ✅ implemented |
| Arrays `array[low..high] of type` | ✅ implemented |
| Records `record ... end` | ✅ implemented |
| Sets `set of type` | ✅ implemented |
| Files `file of type` / `text` | ✅ implemented |
| **Control flow** | |
| `if/then/else` | ✅ implemented |
| `while/do` | ✅ implemented |
| `for to/downto do` | ✅ implemented |
| `repeat/until` | ✅ implemented |
| `case/of/otherwise/end` | ✅ implemented |
| `break` / `continue` | ✅ implemented | loop control, nested loops supported
| **Procedures and functions** | |
| `procedure` (value parameters) | ✅ implemented |
| `function` (return value) | ✅ implemented |
| `var` parameters (pass by reference) | ✅ implemented |
| `forward` declarations | ✅ implemented |
| Recursion | ✅ implemented |
| **Operators** | |
| Arithmetic `+ - * / div mod` | ✅ implemented |
| Relational `= <> < <= > >=` | ✅ implemented |
| Logical `and or not` | ✅ implemented |
| Set `in + * -` | ✅ implemented |
| **Input and output** | |
| `write`, `writeln` | ✅ implemented |
| `readln`, `read` | ✅ implemented |
| Formatted output `x:5` | ✅ implemented |
| File I/O | ✅ implemented |
| **Built-in functions** | |
| `abs`, `sqr`, `chr`, `ord` | ✅ implemented |
| `pred`, `succ`, `odd` | ✅ implemented |
| `round`, `trunc` | ✅ implemented |
| `random`, `randomize` | ✅ implemented |
| `sqrt`, `sin`, `cos`, `exp`, `ln` | ✅ implemented |
| `length`, `concat`, `copy`, `pos` | ✅ implemented |
| **Crt unit** | |
| `ClrScr`, `GotoXY`, `WhereX/Y` | ✅ implemented |
| `TextColor`, `TextBackground` | ✅ implemented |
| `Delay`, `Sound`, `NoSound` | ✅ implemented |
| `KeyPressed`, `ReadKey` | ✅ implemented |
| `Window`, `NormVideo`, `HighVideo`, `LowVideo` | ✅ implemented |
| `InsLine`, `DelLine`, `CursorOn`, `CursorOff` | ✅ implemented |
| `with` statement | ⚠️ parsed only | shorthand access to record fields
| Multidimensional arrays | ✅ implemented | matrix[i][j] fully supported
| Pointers | ✅ implemented | ^type declarations, new/dispose, p^ dereference

### 11. Example programs

#### Snake
```pascal
program SnakeGame;
uses Crt;
const
  WIDTH = 40; HEIGHT = 20; MAX_SNAKE = 400;
type
  TDir = (Up, Down, Left, Right);
  TPoint = record x, y: integer; end;
var
  snake: array[1..MAX_SNAKE] of TPoint;
  len: integer; dir: TDir;
  food: TPoint; score: integer;
  gameover: boolean;
begin
  ClrScr;
  // ... the game loop
end.
```

#### A simple example
```pascal
program Simple;
var
    i, sum: integer;
begin
    sum := 0;
    for i := 1 to 10 do
        sum := sum + i;
    writeln('Sum: ', sum);
end.
```

#### Array example
```pascal
program ArrayTest;
var
    arr: array[1..5] of integer;
    i: integer;
begin
    for i := 1 to 5 do
        arr[i] := i * 10;
    for i := 1 to 5 do
        writeln('arr[', i, '] = ', arr[i]);
end.
```

#### Function example
```pascal
program FunctionTest;
function Factorial(n: integer): integer;
begin
    if n <= 1 then Factorial := 1
    else Factorial := n * Factorial(n - 1);
end;
begin
    writeln('5! = ', Factorial(5));
end.
```

### Floating point and 64-bit compile modes

The VML toolchain controls its strategy for floating point and 64-bit integers through three compile options:

| Option | Allowed values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | how 32-bit floating point (real) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | how 64-bit floating point (double) is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | how 64-bit integers (Int64) are handled |

#### 32-bit floating point (float32)

The `real` type in this language (32-bit single precision) compiles in the following modes:

- **`hard` mode (default)**: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG` and computes directly through the sixteen floating-point registers F0-F15. Best performance; suited to targets that have floating-point hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point arithmetic through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU targets without floating-point hardware.
- **`none` mode**: disables all 32-bit floating-point types; a `real` declaration is then reported as a compile error.

#### 64-bit floating point (double)

The `double` type in this language (64-bit double precision) compiles in the following modes:

- **`soft` mode (default)**: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform (MCU included).
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG` and computes through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- **`none` mode**: disables all 64-bit floating-point types; a `double` declaration is then reported as a compile error.

#### 64-bit integers (int64)

The `Int64` / `LongInt` types in this language compile in the following modes:

- **`soft` mode (default)**: uses the double-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- **`hard` mode**: reserved; a future VML version will support native 64-bit integer instructions.
- **`none` mode**: disables 64-bit integer types and reports an error when one is encountered.

#### Software emulation libraries

All of the software emulation libraries above live in `Lib/shared/`, are written in C and are compiled to VML by the C compiler, so every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point emulation of 32-bit floating point | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 software emulation of 64-bit double precision | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | double-register emulation of 64-bit integers | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

### 12. Compilation limits

1. **Multidimensional arrays**: the `array[1..3, 1..3]` syntax is not supported; use `array[1..3] of array[1..3]` instead
2. **`with` statements**: parsed only, no code is generated
3. **VML integers**: all integers are 32-bit signed
4. **Array initialization**: compile-time initialisation of array constants is not supported
5. **Strings**: dynamic string operations are not supported (there is no heap allocation)
6. **Generics**: features Turbo Pascal does not have are not supported

### 13. Integration with the VML runtime

Pascal programs interact with the VML runtime through the following mechanisms:

- **SYSCALL 2**: string input (readln on a string)
- **SYSCALL 4**: output a character
- **SYSCALL 5**: keyboard input (ReadKey)
- **SYSCALL 6**: output an integer
- **SYSCALL 7**: integer input (readln on an integer)
- **SYSCALL 100-104**: file operations
- **Memory-mapped I/O**: video memory at 0xB8000 (GotoXY/ClrScr), the cursor position at 0x6FF4, and so on

The standard library (`stdlib.vml`) provides the VML assembly implementations of the Crt unit, the maths functions, string operations and file I/O.

---

## 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared libraries (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU mode** (default): strings are emitted as UTF-8 (`.string`)
**OS mode**: the encoding can be tested with the `VML_WSTRING` macro

The shared libraries already provide wide-string conversion functions (wchar.h/uchar.h), which each language compiler can use as needed.
