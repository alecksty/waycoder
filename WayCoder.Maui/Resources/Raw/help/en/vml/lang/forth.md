# Forth

A stack language; writing in it is a completely different way of thinking.

## How to run it on your phone

```
vml run examples/forth/sysinfo.fth
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- Push strings with `S" ..."`; the function comes after
- Argument order is push order

## Examples

| File | What it shows |
|---|---|
| `parserexp_demo.fs` | Calling a shared-library expression parser |
| `sysinfo.fth` | Device information |

Also in this folder: `demo_std.fth`, `demo_tty.fth`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/ForthCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Forth language compiler specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | ANSI Forth subset (1994) |
| «bold»Release year«/» | 1994 |
| «bold»Completeness«/» | ~95% |
| «bold»MCU completeness«/» | ~93% |
| «bold»Tests«/» | 0 (the test directory is yet to be created) |
| «bold»Updates«/» | 2026-05-18: corrected the description of the core word-set status, the completeness figures and the test count |

#### Keywords (core words)

«bold»Stack operations«/»: `DUP` `DROP` `SWAP` `OVER` `ROT` `?DUP` `NIP` `TUCK`
«bold»Arithmetic«/»: `+` `-` `*` `/` `MOD` `/MOD` `1+` `1-` `2*` `2/` `ABS` `NEGATE`
«bold»Comparison«/»: `=` `<>` `<` `>` `<=` `>=` `0=` `0<>` `0<` `0>`
«bold»Logic«/»: `AND` `OR` `XOR` `NOT` `TRUE` `FALSE`
«bold»Control flow«/»: `IF` `ELSE` `THEN` `BEGIN` `AGAIN` `UNTIL` `WHILE` `REPEAT` `CASE` `ENDCASE` `OF` `ENDOF`
«bold»Loops«/»: `DO` `LOOP` `+LOOP` `I` `J` `LEAVE` `UNLOOP` `EXIT` `RECURSE`
«bold»Definitions«/»: `:` `;` `CONSTANT` `VARIABLE` `CREATE` `DOES>` `DEFER` `IS` `VALUE` `TO`
«bold»Memory«/»: `@` `!` `C@` `C!` `ALLOT` `CELLS` `HERE`
«bold»I/O«/»: `."` `EMIT` `CR` `SPACE` `TYPE` `KEY`
«bold»Other«/»: `DEPTH` `IMMEDIATE` `POSTPONE` `[']` `[CHAR]` `LITERAL`

#### Overview

This compiler compiles the Forth language into VML (Virtual Machine Language) assembly. Forth is a stack-based, extensible programming language that is widely used in embedded systems and real-time control.

#### Language features

##### 1. Core Forth concepts

###### Stack operations
- «bold»Data stack«/»: used for passing parameters and for temporary storage
- «bold»Return stack«/»: used for control flow and loops
- «bold»Dictionary«/»: stores word (function) definitions

###### Word definitions
```
: SQUARE ( n -- n^2 ) DUP * ;
: FACTORIAL ( n -- n! ) 
    DUP 1 > IF 
        DUP 1 - RECURSE * 
    ELSE 
        DROP 1 
    THEN ;
```

##### 2. Basic stack words

###### Data stack operations
- `DUP` - duplicate the top element
- `DROP` - discard the top element
- `SWAP` - exchange the top two elements
- `OVER` - copy the second element from the top
- `ROT` - rotate the top three elements
- `?DUP` - conditional duplicate (copy only when the top is non-zero)

###### Arithmetic
- `+` - addition
- `-` - subtraction
- `*` - multiplication
- `/` - division
- `MOD` - modulo
- `/MOD` - division and modulo
- `1+` - add 1
- `1-` - subtract 1
- `2*` - multiply by 2
- `2/` - divide by 2

###### Comparison
- `=` - equal
- `<>` - not equal
- `<` - less than
- `>` - greater than
- `<=` - less than or equal
- `>=` - greater than or equal
- `0=` - equal to zero
- `0<>` - not equal to zero
- `0<` - less than zero
- `0>` - greater than zero

###### Logic
- `AND` - bitwise AND
- `OR` - bitwise OR
- `XOR` - bitwise exclusive OR
- `NOT` - bitwise NOT (or logical NOT)
- `INVERT` - bitwise complement

##### 3. Control structures

###### Conditionals
```
: ABS ( n -- |n| )
    DUP 0< IF NEGATE THEN ;
    
: SIGN ( n -- )
    DUP 0> IF ." Positive " 
    ELSE DUP 0< IF ." Negative " 
    ELSE ." Zero " THEN THEN DROP ;
```

###### Loops
```
: COUNTDOWN ( n -- )
    BEGIN DUP . 1- DUP 0< UNTIL DROP ;
    
: TIMES ( n -- )
    0 DO I . LOOP ;
    
: 10STARS
    10 0 DO ." *" LOOP ;
```

###### Infinite loops
```
: INFINITE-LOOP
    BEGIN ." Hello " AGAIN ;
```

##### 4. Memory operations

###### Variables and constants
```
VARIABLE COUNTER
COUNTER @   ( read the value )
10 COUNTER !  ( write the value )

100 CONSTANT MAX-SIZE
MAX-SIZE .   ( prints 100 )
```

###### Arrays and buffers
```
CREATE BUFFER 100 ALLOT
BUFFER 10 + C@   ( read a byte )
65 BUFFER 10 + C!  ( write a byte )
```

###### String operations
```
: .STRING ( addr len -- )
    OVER + SWAP DO I C@ EMIT LOOP ;
    
S" Hello" TYPE   ( print the string )
```

##### 5. Input and output

###### Character input and output
```
KEY ( -- char )   ; read one character
EMIT ( char -- )  ; print one character
```

###### Number input and output
```
. ( n -- )        ; print a signed number
U. ( u -- )       ; print an unsigned number
.HEX ( n -- )     ; print in hexadecimal
```

###### String output
```
." text"          ; a string compiled into the word
TYPE ( addr len -- ) ; print a string
CR                ; newline
SPACE             ; print a space
SPACES ( n -- )   ; print n spaces
```

##### 6. Dictionary and compilation

###### Word definitions
```
: DOUBLE ( n -- 2n ) 2 * ;
: SQUARE ( n -- n^2 ) DUP * ;
: CUBE ( n -- n^3 ) DUP DUP * * ;
```

###### Immediate words
```
: IMMEDIATE
    LATEST @ DUP C@ 128 OR SWAP C! ;
    
: [COMPILE] ' CFA , ; IMMEDIATE
```

###### Compilation control
```
[    ; enter interpretation mode
]    ; enter compilation mode
COMPILE, ( xt -- ) ; compile an execution token
LITERAL ( n -- )   ; compile a literal
```

##### 7. Error handling

###### Exception handling
```
CATCH ( xt -- n | 0 )
THROW ( n -- )
ABORT" message"
```

###### Stack checks
```
?STACK ( -- )   ; check for stack overflow
DEPTH ( -- n )  ; return the stack depth
```

##### 8. VML code-generation conventions

###### Stack implementation
The Forth data stack is implemented using a region of VML memory:
```
stack pointer: R13 (SP)
stack base: 0x9000
stack size: 1024 bytes
```

###### Stack operation mapping
```
DUP  -> MOVE R0, [R13]  ; read the top of stack
      -> PUSH R0        ; push it back
      
DROP -> ADD R13, #4     ; move the stack pointer
      
SWAP -> MOVE R0, [R13]      ; top of stack
      -> MOVE R1, [R13+4]   ; second from top
      -> MOVE [R13], R1
      -> MOVE [R13+4], R0
```

###### Word calling convention
```
: SQUARE DUP * ;
VML code:
LABEL SQUARE
    ; DUP implementation
    MOVE R0, [R13]
    PUSH R0
    
    ; * implementation
    POP R0        ; second operand
    MOVE R1, [R13] ; first operand
    MUL R0, R1, R0
    MOVE [R13], R0 ; store the result back on top of stack
    RET
```

##### 9. Example programs

###### Factorial
```
: FACTORIAL ( n -- n! )
    DUP 1 > IF
        DUP 1 - RECURSE *
    ELSE
        DROP 1
    THEN ;
    
5 FACTORIAL .   ( prints 120 )
```

###### Fibonacci sequence
```
: FIBONACCI ( n -- fib(n) )
    DUP 2 < IF DROP 1 EXIT THEN
    DUP 1 - RECURSE
    SWAP 2 - RECURSE + ;
    
10 FIBONACCI .   ( prints 55 )
```

###### A simple calculator
```
: CALCULATOR
    BEGIN
        CR ." Enter operation (+, -, *, /, q to quit): "
        KEY DUP EMIT CR
        DUP [CHAR] q = IF DROP EXIT THEN
        
        ." Enter first number: " PAD 20 ACCEPT >NUMBER 2DROP DROP
        ." Enter second number: " PAD 20 ACCEPT >NUMBER 2DROP DROP
        
        SWAP
        CASE
            [CHAR] + OF + ENDOF
            [CHAR] - OF - ENDOF  
            [CHAR] * OF * ENDOF
            [CHAR] / OF / ENDOF
        ENDCASE
        
        ." Result: " . CR
    AGAIN ;
```

##### Floating point and 64-bit compile modes

The VML toolchain controls its strategy for floating point and 64-bit integers through three compile options:

| Option | Allowed values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | how 32-bit floating point (FLOAT) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | how 64-bit floating point (DOUBLE) is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | how 64-bit integers are handled |

###### 32-bit floating point (float32)

`FLOAT` stack operations in Forth (32-bit single precision) compile in the following modes:

- «bold»`hard` mode (default)«/»: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG` and computes directly through the sixteen floating-point registers F0-F15. Best performance; suited to targets that have floating-point hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point arithmetic through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU targets without floating-point hardware.
- «bold»`none` mode«/»: disables all 32-bit floating-point arithmetic.

###### 64-bit floating point (double)

Double-cell stack items in Forth (`2CONSTANT` / `2VARIABLE` and the other two-cell operations) compile in the following modes:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform (MCU included).
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG` and computes through the eight double-precision registers D0-D7.
- «bold»`none` mode«/»: disables the double-precision extensions.

###### 64-bit integers (int64)

64-bit integers exist in Forth as double cells (the `2*` / `D+` / `DNEGATE` and other double-word operations). VML compilation handles them in the following modes:

- «bold»`soft` mode (default)«/»: uses the double-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- «bold»`hard` mode«/»: reserved; a future VML version will support native 64-bit integer instructions.
- «bold»`none` mode«/»: disables the 64-bit double-word extensions.

###### Software emulation libraries

All of the software emulation libraries above live in `Lib/shared/`, are written in C and are compiled to VML by the C compiler, so every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point emulation of 32-bit floating point | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 software emulation of 64-bit double precision | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | double-register emulation of 64-bit integers | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

##### 10. Compilation limits

###### Current implementation status
- «bold»Lexer«/»: complete, supports Forth words and symbols
- «bold»Parser«/»: basic, supports colon definitions and control structures
- «bold»Code generator«/»: basic, maps stack operations onto VML register/memory operations
- «bold»Standard library«/»: the `Lib/forth/` directory

###### Implemented core word set (usable in MCU mode)
1. Stack words: DUP, DROP, SWAP, OVER, ROT ✅
2. Arithmetic words: +, -, *, /, MOD ✅
3. Comparison words: =, <, >, 0=, 0< ✅
4. Memory words: @, !, C@, C! ✅
5. Control words: IF, THEN, ELSE, BEGIN, UNTIL, DO, LOOP ✅
6. I/O words: ., EMIT, KEY, CR ✅

###### Not yet implemented
- Advanced defining words such as DOES>, DEFER, IS, VALUE
- CATCH/THROW exception handling
- Floating-point stack operations (the FLOAT word set)

##### 11. Integration with the VML runtime

Forth programs talk to the VML runtime through system calls:

- «bold»SYSCALL 4«/»: output a character (EMIT)
- «bold»SYSCALL 5«/»: input a character (KEY)
- «bold»SYSCALL 6«/»: output an integer (.)
- «bold»SYSCALL 7«/»: input an integer (numeric input)
- «bold»SYSCALL 120«/»: allocate memory (ALLOT)
- «bold»SYSCALL 121«/»: free memory

Forth's simplicity and stack-based architecture make it a very good fit for embedded systems and resource-constrained environments, and through the VML compiler Forth programs can run on a wide range of hardware platforms.
---

#### 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared libraries (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

«bold»MCU mode«/» (default): strings are emitted as UTF-8 (`.string`)
«bold»OS mode«/»: the encoding can be tested with the `VML_WSTRING` macro

The shared libraries already provide wide-string conversion functions (wchar.h/uchar.h), which each language compiler can use as needed.

## Compiler README

### Forth (ANSI Forth subset) compiler

«bold»Path«/»: `VMLPrepares/ForthCompiler/`
«bold»Completeness«/»: ~95% | 🟢 production ready
«bold»Standard library«/»: `Lib/forth/`

#### Features
- ✅ Syntax analysis + code generation (Lexer/Parser/CodeGenerator)
- ✅ Word definition: `: word ... ;`
- ✅ Stack operations: DUP/DROP/SWAP/OVER/ROT
- ✅ Arithmetic: + - * / MOD /MOD
- ✅ Comparison: = <> < > 0= 0<
- ✅ Control: IF/ELSE/THEN/BEGIN/UNTIL/DO/LOOP
- ✅ Memory: @ ! C@ C! VARIABLE CONSTANT
- ✅ I/O: . EMIT KEY CR ." TYPE
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)


#### Compile modes

##### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that do not fit such a system.

«bold»Skipped«/» (no code is generated when these appear):
- None (Forth started out on embedded systems, so it is MCU-compatible by nature)

«bold»Kept«/» (implemented at the low level by the BIOS):
- ALLOT (heap), HERE
- POKE/PEEK memory-mapped I/O (MMIO)
- Stack operations, control flow, word definitions
- printf/puts mapped onto the UART

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, an RTOS, and so on). It will support the full set of language features (file system, threads, async, exceptions, reflection, and so on).

##### RAM level
- `--ram k`: kilobyte level (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte level (64KB~1MB, such as ARM Cortex-M, «bold»default«/»)
- `--ram g`: gigabyte level (such as an x86/DDR system)
- `--stack-size <bytes>`: set the stack size by hand (by default it is allocated automatically from `--ram`)

##### MCU safe-coding tips
- Stack space is limited (typically 256-4096 bytes), so avoid deep recursion
- The Forth parameter stack and return stack both need generous headroom
- Dynamic loading is not allowed
- Floating-point work may need a software floating-point library

#### Usage
```bash
dotnet run --project VMLTool -- input.fs -o output.vml
```

#### Tests
`Test/Fo/` — 0 test files (the test directory is yet to be created)
