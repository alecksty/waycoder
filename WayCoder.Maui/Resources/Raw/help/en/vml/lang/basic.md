# BASIC

The old-fashioned style: imperative, one line after another.

## How to run it on your phone

```
vml run examples/basic/sysinfo.bas
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- Every `ui_*` you use needs a `NATIVE FUNCTION` / `NATIVE SUB` declaration
- Call parameterless procedures «bold»bare«/» (no brackets needed)
- «bold»You do not have to copy the screenful of `NATIVE` declarations by hand«/» - write one line, `'$INCLUDE: 'waycoder_ui.bi'`, and the whole `ui_*` table is pulled in (see `examples/basic/waycoder_ui.bi`)
- `SCREEN`/`CLS`/`PSET`/`LINE`/`CIRCLE`/`PAINT`/`COLOR`/`PALETTE`/`LOCATE`/`DRAW`/`GET`/`PUT`/`POINT(x,y)`/`WIDTH` «bold»work directly on the phone«/» (the compiler translates them to the host's `ui_*` shapes by default); `SCREEN n` only decides how big the window is
- `PRINT` in graphics mode lands «bold»in the window«/» (not the console); box-drawing characters render correctly
- Floats work: `PRINT 3.14` prints `3.14`, `PRINT 7 / 2` prints `3.5` (`\` is integer division), and integers do not print a trailing `.0` (`PRINT 2.0` prints `2`)
- Files work too: `OPEN`/`CLOSE`/`PRINT #`/`INPUT #`/`LINE INPUT #`/`FREEFILE`, with paths inside your own workspace (going outside is refused)

## Examples

| File | What it shows |
|---|---|
| `_tty.bas` | Not a standalone program: the shim that rebuilds a monospaced character grid on top of `ui_*` |
| `blackjack.bas` | Blackjack (polling message loop plus vibration) |
| `demo_bgi.bas` | A demonstration of the graphics statements, one by one |
| `demo_tty.bas` | Text mode (ANSI color) |
| `demo_ui.bas` | The smallest skeleton: open a window, draw, message loop |
| `gfx_demo.bas` | The same as above |
| `gfx_modes.bas` | Resolution of each `SCREEN` mode |
| `gorilla.bas` | Gorillas throwing bananas (a complete game plus sound) |
| `gorilla_pro.bas` | The same, with gradients, paths and tiles added |
| `hammurabi.bas` | Hammurabi (turn-based management) |
| `hurkle.bas` | An old-style typewriter text program (with `_tty.bas` inlined) |
| `lander.bas` | Lunar lander |
| `nibbles.bas` | Snake |
| `sysinfo.bas` | Device information |
| `tetris.bas` | Tetris (grid plus gamepad) |
| `whack.bas` | Whack-a-mole (random numbers plus timers plus vibration) |

Also in this folder: `CREDITS.md`, `demo_std.bas`, `donkey.bas`, `waycoder_ui.bi`

## Pitfalls we hit on real devices

- A parameter cannot be named `on` (it is a keyword).
- «bold»`INPUT` in `FOR INPUT` is a keyword«/», not an ordinary identifier - other mode names (`OUTPUT`/`APPEND`) can be written freely.
- String concatenation works, but «bold»only a limited number of strings can be live at once on one line«/» (a rotating buffer of 8); beyond that they overwrite each other.
- «bold»The return-value slot of `FUNCTION` is an integer«/» - to return a float you have to work around it (use `SUB` with an output parameter).

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/BasicCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### BASIC language compiler specification

> «bold»Version«/»: v2.1 | «bold»Date«/»: 2026-08-01 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.
> «bold»Updates«/»: v1.66.33 — all dialects ≥90% + CLASS/OOP + GPIO + 140 keywords (measured, v0.96.506)

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | QBasic (default) + 9 dialects (10 `BasicDialect` members in total, see `VMLPlugins/CompilerOptions.cs`), all at ≥90% completeness |
| «bold»CLI option«/» | `--basictype qbasic\|turbobasic\|freebasic\|truebasic\|purebasic\|chipbasic\|minibasic` |
| «bold»Completeness«/» | 92% (QBasic), 90% (every dialect) |
| «bold»Tests«/» | 63 passing (Lang_BASIC) + 22 dialect tests |
| «bold»Total tokens«/» | 140 keywords (measured, v0.96.506), 171 TokenTypes (measured, v0.96.506) |

---

#### Multi-dialect keyword sets

The BASIC compiler supports 10 dialects through the `--basictype` option. Each dialect's keyword differences are listed below.

##### 1. QBasic (default) — `__QBASIC__`

Compatible with Microsoft QBasic/QuickBASIC. «bold»Every later dialect builds on this one«/».

«bold»Unique keywords«/» (14):
`PSET`, `CIRCLE`, `PAINT`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `SCREEN`, `LPRINT`, `PRINT USING`, `FREEFILE`, `ON ERROR`, `RESUME`, `DEF FN`

«bold»Typical use«/»: QBasic programs from the DOS era, QuickBASIC games (GORILLAS/NIBBLES)

---

##### 2. TurboBasic — `__TURBOBASIC__`

Compatible with Borland Turbo Basic.

«bold»Added on top of QBasic«/» (6):
`EXIT`, `DO`, `LOOP`, `UNTIL`, `CASE ELSE`, `FUNCTION`

«bold»Removed«/» (difference from QBasic) (8):
`PSET`, `CIRCLE`, `PAINT`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `SCREEN`

«bold»Typical use«/»: scientific computing, structured BASIC programs

---

##### 3. FreeBasic — `__FREEBASIC__`

Compatible with the open-source cross-platform BASIC compiler.

«bold»Added on top of QBasic«/» (12):
`PTR`, `CAST`, `CPTR`, `ANY`, `EXTENDS`, `OPERATOR`, `PROPERTY`, `ENUM`, `NAMESPACE`, `USING`, `DESTRUCTOR`, `CONSTRUCTOR`

«bold»Removed«/» (difference from QBasic) (6):
`LPRINT`, `PRINT USING`, `PSET`, `CIRCLE`, `DRAW`, `PALETTE`

«bold»Typical use«/»: modern open-source BASIC projects, cross-platform development

---

##### 4. TrueBasic — `__TRUEBASIC__`

ANSI/ISO standard BASIC.

«bold»Added on top of QBasic«/» (4):
`MAT`, `ZER`, `CON`, `SOUND`

«bold»Removed«/» (difference from QBasic) (12):
`SELECT`, `CASE`, `IS`, `ELSEIF`, `DO`, `LOOP`, `EXIT`, `GOSUB`, `PEEK`, `POKE`, `LPRINT`, `ON ERROR`

«bold»Typical use«/»: education, standards-compliant programs

---

##### 5. PureBasic — `__PUREBASIC__`

Compatible with PureBasic.

«bold»Added on top of QBasic«/» (8):
`PROCEDURE`, `ENDPROCEDURE`, `PROTECTED`, `GLOBAL`, `THREADED`, `INTERFACE`, `ENDINTERFACE`, `NEW`

«bold»Removed«/» (difference from QBasic) (10):
`GOSUB`, `RETURN`, `LPRINT`, `PSET`, `CIRCLE`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `DEF FN`

«bold»Typical use«/»: game development, GUI applications

---

##### 6. ChipBasic — `__CHIPBASIC__`

An MCU-optimised dialect.

«bold»Added on top of QBasic«/» (3):
`PINMODE`, `DIGITALWRITE`, `DIGITALREAD`

«bold»Removed«/» (difference from QBasic) (15):
`SCREEN`, `PSET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `LPRINT`, `PRINT USING`, `FREEFILE`, `ON ERROR`, `RESUME`, `CHIPASM`

«bold»Typical use«/»: embedded MCUs (Arduino/STM32), IoT devices

---

##### 7. MiniBasic — `__MINIBASIC__`

The smallest subset — for teaching and resource-constrained environments.

«bold»Added on top of QBasic«/» (0):
None.

«bold»Removed«/» (difference from QBasic) (20):
`TYPE`, `DIM`, `REDIM`, `ERASE`, `SWAP`, `SHARED`, `COMMON`, `SELECT`, `CASE`, `DO`, `LOOP`, `EXIT`, `GOSUB`, `GOTO`, `CALL`, `LPRINT`, `PRINT USING`, `PSET`, `CIRCLE`, `DRAW`

«bold»Retained keywords«/» (15 only):
`PRINT`, `INPUT`, `IF`, `THEN`, `ELSE`, `FOR`, `NEXT`, `WHILE`, `WEND`, `LET`, `REM`, `DIM` (simplified), `END`, `GOTO` (simplified), `GOSUB` (simplified)

«bold»Typical use«/»: teaching, microcontroller bootstrapping

---

##### Dialect keyword comparison

| Category | QBasic | TurboBasic | FreeBasic | TrueBasic | PureBasic | ChipBasic | MiniBasic |
|------|:------:|:----------:|:---------:|:---------:|:---------:|:---------:|:---------:|
| Total keywords | ~70 | ~68 | ~76 | ~62 | ~68 | ~57 | ~15 |
| Completeness | 92% | 90% | 90% | 90% | 90% | 90% | 90% |
| Graphics | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| OOP | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| MCU GPIO | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Line numbers | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | optional |
| GOSUB | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | simplified |

---

#### Full QBasic keyword list (by category)

##### Control flow (14)
`IF`, `THEN`, `ELSE`, `ELSEIF`, `END`, `END IF`, `SELECT`, `CASE`, `IS`, `TO`, `FOR`, `STEP`, `NEXT`, `WHILE`, `WEND`, `DO`, `LOOP`, `UNTIL`, `EXIT`, `GOTO`, `GOSUB`, `RETURN`

##### Subroutines/functions (7)
`SUB`, `FUNCTION`, `CALL`, `BYVAL`, `BYREF`, `DECLARE`, `DEF FN`

##### Variables/data (12)
`LET`, `DIM`, `SWAP`, `ERASE`, `REDIM`, `PRESERVE`, `LOCAL`, `STATIC`, `SHARED`, `COMMON`, `CONST`, `TYPE`

##### I/O (8)
`PRINT`, `INPUT`, `LPRINT`, `PRINT USING`, `OPEN`, `CLOSE`, `AS`, `FREEFILE`

##### Graphics (17)
`SCREEN`, `PSET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`, `COLOR`, `LOCATE`, `CLS`, `VIEW`, `WINDOW`, `PALETTE`, `GET`, `PUT`, `WIDTH`, `BEEP`

##### Functions (20+)
`ABS`, `SGN`, `SQR`, `INT`, `FIX`, `SIN`, `COS`, `TAN`, `EXP`, `LOG`, `ATN`, `RND`, `RANDOMIZE`, `TIMER`, `DATE$`, `TIME$`, `PEEK`, `POKE`, `INKEY$`, `KBGETCH`, `KBHIT`, `MOUSEGETX`, `MOUSEGETY`, `MOUSELEFT`, `MOUSERIGHT`, `LEN`, `CHR$`, `ASC`, `STR$`, `VAL`, `LEFT$`, `RIGHT$`, `MID$`, `exit`

##### Other (9)
`REM`, `DATA`, `READ`, `RESTORE`, `SLEEP`, `SYSTEM`, `ON ERROR`, `RESUME`, `CHIPASM`, `ASM`

#### Full operator list (highest precedence first)

| Precedence | Operator | Token | Description |
|:------:|--------|:-----:|------|
| 1 | `^` | EXPONENT | exponentiation |
| 2 | `-` (unary) | MINUS | negation |
| 3 | `*` `/` | MULTIPLY, DIVIDE | multiply, divide |
| 4 | `+` `-` | PLUS, MINUS | add, subtract |
| 5 | `=` `<` `>` `<=` `>=` `<>` | EQUALS, LESS, GREATER, LESS_EQUAL, GREATER_EQUAL, NOT_EQUAL | relational |
| 6 | `NOT` | NOT | logical NOT |
| 7 | `AND` | AND | logical AND |
| 8 | `OR` | OR | logical OR |

#### Separators

| Symbol | Token | Purpose |
|:----:|:-----:|------|
| `,` | COMMA | separates arguments / columns |
| `;` | SEMICOLON | PRINT without a newline |
| `:` | COLON | separates multiple statements |
| `.` | DOT | member access |
| `"` | QUOTE | string delimiter |
| `(` `)` | LPAREN, RPAREN | function/array parentheses |
| `#` | HASH | file number prefix

#### 3. Statement reference

##### 3.1 Basic I/O

```basic
' PRINT — write to the console
PRINT "Hello"       ' a string
PRINT 42            ' a number
PRINT A; B;         ' a semicolon suppresses the newline
PRINT A, B          ' a comma moves to the next column
PRINT               ' a blank line

' INPUT — keyboard input
INPUT "prompt: ", X
INPUT A, B, C
INPUT A$

' LPRINT — printer output
LPRINT "Hello"
```

«bold»PRINT output target rules«/»

Where PRINT writes is decided by the `SCREEN` mode:

| Mode | Console output | Memory output | Description |
|:-----|:---------:|:-------:|------|
| «bold»Default (no SCREEN)«/» | ✅ | ❌ | console terminal only |
| «bold»SCREEN 0 (text mode)«/» | ✅ | ✅ `0xB8000` | console + VGA text video memory at the same time |
| «bold»SCREEN N>0 (graphics mode)«/» | ❌ | ✅ VGA frame buffer | renders to the VGA graphics frame buffer only; nothing goes to the console |

The details:
- «bold»SYSCALL #4«/»: pure console output, no VGA involved. Handles the BEL beep and UTF-8 decoding
- «bold»Text-mode VGA«/»: handled by the extension library `vga_text_putchar` (Lib/shared/vga_text.vml), which writes to `0xB8000` text video memory and manages the cursor. The BASIC compiler links this library by default and calls it before every SYSCALL #4
- «bold»Graphics-mode VGA«/»: the compiler emits an `EmitGfxPrintChar()` call for each PRINT character, rendering pixels from an 8x8 bitmap font into the VGA graphics frame buffer
- Each of the two checks the SCREEN mode (0x6FF0) on its own and runs only when its own mode is active, so they complement each other
- The cursor position set by «bold»LOCATE«/» is valid in both text and graphics mode (shared through `0x6FF4`/`0x6FF8`)

##### 3.2 Variable operations

```basic
' LET (may be omitted)
LET A = 42
B = A + 10

' SWAP — exchange two variables
SWAP A, B

' ERASE — clear an array
ERASE ArrayName

' DIM — array declaration
DIM A(10)           ' one dimension
DIM B(5, 5)         ' two dimensions
DIM C(3, 3, 3)      ' three dimensions
DIM S$(10)          ' an array of strings
```

##### 3.3 Control flow

```basic
' IF
IF condition THEN
    ...
ELSEIF condition THEN
    ...
ELSE
    ...
END IF

' single-line IF
IF X > 10 THEN PRINT "Big" ELSE PRINT "Small"

' FOR
FOR I = 1 TO 10 STEP 2
    PRINT I
NEXT I

' WHILE
WHILE A > 0
    A = A - 1
WEND

' DO
DO
    EXIT DO
LOOP WHILE A > 0

DO WHILE A > 0
    EXIT DO
LOOP

DO
LOOP UNTIL A > 0

' SELECT CASE
SELECT CASE X
    CASE 1
        PRINT "One"
    CASE 2 TO 5
        PRINT "Two to Five"
    CASE IS > 10
        PRINT "Big"
    CASE ELSE
        PRINT "Other"
END SELECT

' GOTO / GOSUB
GOTO label
GOSUB subroutine
RETURN

' EXIT
EXIT FOR
EXIT DO
EXIT WHILE
EXIT SUB
EXIT FUNCTION
```

##### 3.4 Subroutines and functions

```basic
SUB MySub(param1, BYREF param2)
    LOCALVAR = param1
    param2 = 99
END SUB

FUNCTION Add(a, b)
    Add = a + b
END FUNCTION

CALL MySub(x, y)
result = Add(5, 3)
```

##### 3.5 Graphics (standard QBASIC syntax)

```basic
SCREEN 9              ' set the graphics mode
WIDTH 80, 25          ' set the text size
COLOR 7, 0            ' foreground colour, background colour
CLS                   ' clear the screen
LOCATE 10, 20         ' position the cursor

PSET (100, 150), 4    ' draw a point (x, y), colour index
LINE (0,0)-(100,100), 2                    ' draw a line
LINE (0,0)-(100,100), 2, B                 ' a rectangle outline
LINE (0,0)-(100,100), 2, BF                ' a filled rectangle
CIRCLE (160,120), 50, 3                    ' draw a circle
PAINT (160,120), 4, 3                      ' fill

' graphics string macro (DRAW)
DRAW "U10 R20 D10 L20"                     ' a draw string
```

«bold»How SCREEN mode affects PRINT«/»
The `SCREEN` statement switches the display mode, and PRINT writes to the console.


##### 3.6 VML graphics extensions

```basic
VGAPUTPIXEL x, y, r, g, b      ' draw a point in 24-bit colour
VGADRAWLINE x1,y1,x2,y2,r,g,b   ' draw a coloured line
VGADRAWCIRCLE x,y,radius,r,g,b  ' draw a coloured circle
VGAFILLCIRCLE x,y,radius,r,g,b  ' draw a filled coloured circle
VGADRAWRECT x,y,w,h,r,g,b       ' draw a coloured rectangle outline
' strings
LEN(s$), CHR$(n), ASC(s$), STR$(n), VAL(s$)
LEFT$(s$, n), RIGHT$(s$, n), MID$(s$, start, len)

' random
RND(n)
RANDOMIZE [TIMER]

' memory
PEEK(addr)
POKE addr, value

' time
TIMER, DATE$, TIME$

' detection
KBGETCH(), KBHIT()
MOUSEGETX(), MOUSEGETY(), MOUSELEFT(), MOUSERIGHT()
VGAGETPIXEL(x,y), VGAGETWIDTH(), VGAGETHEIGHT()
```

##### 3.7 File operations

```basic
OPEN "file.txt" FOR INPUT AS #1
OPEN "file.txt" FOR OUTPUT AS #2
INPUT #1, X
PRINT #2, "Hello"
CLOSE #1
```

##### 3.9 Audio

```basic
' PLAY — play a music string
PLAY "C D E F G A B"
PLAY "O3 L4 CDEFGAB"

' SOUND — frequency / duration
SOUND 440, 18         ' frequency (Hz), duration (1/18 s)

' BEEP — the system beep
BEEP
```

##### 3.10 Error handling

```basic
' ON ERROR GOTO — runtime error handling
ON ERROR GOTO ErrorHandler
    ... normal code ...
    EXIT SUB

ErrorHandler:
    PRINT "错误: "; ERR
    RESUME NEXT
```

##### 3.11 Data types and structures

```basic
' CONST — constant definitions
CONST PI = 3.14159
CONST MAX_ROWS = 25

' TYPE / END TYPE — user-defined types
TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE

' using a user-defined type
DIM P AS Point
P.X = 10
P.Y = 20

' DATA/READ/RESTORE — embedded data
DATA 1, 2, 3, 4, 5
FOR I = 1 TO 5
    READ X
    PRINT X
NEXT
RESTORE

' DEF FN — a single-line user-defined function
DEF FNAdd(a, b) = a + b
PRINT FNAdd(3, 5)

' COMMON — variables shared across modules
COMMON SHARED Score, Lives

' REDIM/PRESERVE — resize a dynamic array
DIM A(10)
REDIM A(20)
REDIM PRESERVE A(30)    ' keep the existing data
```

##### 3.12 Other

```basic
SLEEP [seconds]     ' pause
SWAP A, B           ' exchange two variables
ERASE ArrayName     ' clear an array
SYSTEM              ' exit the program
exit(code)          ' exit and return code to the system (SYSCALL 3)
END                 ' end the program
DEFINT A-C          ' type declaration (compatibility)
DEFSNG D-F
DEFSTR S

' OPTION BASE — the starting array index
OPTION BASE 0       ' arrays start at 0 (default)
OPTION BASE 1       ' arrays start at 1

' INKEY$ — non-blocking key check
K$ = INKEY$
IF K$ <> "" THEN PRINT "Key: "; K$

' DECLARE — forward declaration of a subroutine
DECLARE SUB MySub(x)
DECLARE FUNCTION MyFunc(x, y)

' LOCAL / STATIC / SHARED / COMMON
SUB MyProc()
    LOCAL temp        ' a local variable
    STATIC count      ' a static variable (persists)
    SHARED globalVar  ' a shared variable
END SUB
COMMON SHARED Score, Lives  ' shared across modules
```

##### 3.13 Calling conventions and inline assembly

```basic
' the __stdcall calling convention — all arguments go on the stack, the callee cleans up
SUB MyFunc STDCALL(a, b)
    MyFunc = a + b
END SUB

' ASM — inline VML assembly
ASM "MOVE R0, #42"
ASM "SYSCALL #4"

' CHIPASM — inline assembly for the target architecture (6502/Z80/8051 and so on)
CHIPASM "LDA #$42"
CHIPASM "STA $0400"
```

##### 3.14 Advanced graphics (GET/PUT/PALETTE/VIEW/WINDOW)

```basic
' PALETTE — set a palette entry (SCREEN 13 mode)
PALETTE 0, 0, 0, 0         ' index 0 = black
PALETTE 1, 63, 0, 0        ' index 1 = red
PALETTE 2, 0, 63, 0        ' index 2 = green

' GET — save a screen region into an array
DIM buffer(200)
CIRCLE (50,50), 10, 2
GET (40,40)-(60,60), buffer

' PUT — restore a screen region from an array
PUT (100,100), buffer       ' overwrite directly
PUT (100,100), buffer, 1    ' XOR mode

' VIEW — set the graphics viewport
VIEW (10,10)-(200,100)       ' the viewport region
VIEW (10,10)-(200,100), 2, 4 ' with a coloured border

' WINDOW — set the logical coordinate system
WINDOW (0,0)-(1,1)           ' logical coordinates 0-1
CIRCLE (0.5,0.5), 0.3       ' draw in logical coordinates
```

##### 3.15 PRINT USING — formatted output

```basic
PRINT USING "###.##"; 12.345     ' prints: 12.35
PRINT USING "####"; 42            ' prints:   42
PRINT USING "$$###.##"; 12.3     ' prints: $ 12.30
```

#### 4. Differences from standard QBASIC

##### Unsupported features
- `SHELL` / `CHAIN` / `RUN` — invoking external programs (not supported on a bare-metal system)
- `LINE INPUT` — line input (INPUT can be used instead)
- `FIELD` / `LSET` / `RSET` — random-file field operations

##### Extensions
- Full support for Chinese identifiers and strings
- VGA 24-bit colour graphics extensions (VGAPUTPIXEL and more)
- Device description file support (devices/*.json)
- Multi-platform emulation (PC, Apple II, C64 and more)
- `POKE` / `PEEK` memory-mapped I/O
- The `__stdcall` calling convention, which lets functions interoperate across languages
- «bold»PRINT's output target switches automatically with the SCREEN mode«/»: in graphics mode it renders to the VGA frame buffer only, with nothing on the console (matching how a real VGA card behaves under QBASIC)

#### Floating point and 64-bit compile modes

The VML toolchain controls its strategy for floating point and 64-bit integers through three compile options:

| Option | Allowed values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | how 32-bit floating point (SINGLE/!) is handled |
| `--float64` | `hard` / `soft` / `none` | `soft` | how 64-bit floating point (DOUBLE/#) is handled |
| `--int64` | `hard` / `soft` / `none` | `soft` | how 64-bit integers are handled |

##### 32-bit floating point (float32)

The 32-bit single-precision types `SINGLE` / `!` in this language compile in the following modes:

- «bold»`hard` mode (default)«/»: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG` and computes directly through the sixteen floating-point registers F0-F15. Best performance; suited to targets that have floating-point hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point arithmetic through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU targets without floating-point hardware.
- «bold»`none` mode«/»: disables all 32-bit floating-point types; a floating-point declaration is then reported as a compile error.

##### 64-bit floating point (double)

The 64-bit double-precision types `DOUBLE` / `#` in this language compile in the following modes:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform (MCU included).
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG` and computes through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- «bold»`none` mode«/»: disables all 64-bit floating-point types; a floating-point declaration is then reported as a compile error.

##### 64-bit integers (int64)

64-bit integer types are supported in this language by the VML compilation layer. The BASIC standard does not define a matching explicit type, but the compiler leaves open the possibility of supporting one through the `--int64` extension.

- «bold»`soft` mode (default)«/»: uses the double-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- «bold»`hard` mode«/»: reserved; a future VML version will support native 64-bit integer instructions.
- «bold»`none` mode«/»: disables the 64-bit integer extensions.

##### Software emulation libraries

All of the software emulation libraries above live in `Lib/shared/`, are written in C and are compiled to VML by the C compiler, so every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point emulation of 32-bit floating point | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 software emulation of 64-bit double precision | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | double-register emulation of 64-bit integers | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### 5. Hardware interface

##### Memory-mapped I/O
| Address | Purpose |
|------|------|
| 0x60 | keyboard data port |
| 0x64 | keyboard status port |
| 0x300-0x304 | mouse X/Y/buttons |

##### System calls
| Number | Function |
|------|------|
| 1 | output a string |
| 4 | output a character |
| 5 | input a character |
| 7 | input an integer |
| 100-104 | device management |
| 110-114 | file operations |

##### Device management (syscall 100-104)
```
100: open a device     R0=device name address -> R0=handle
101: close a device    R0=handle
102: read a device     R0=handle, R1=buffer, R2=length
103: write a device    R0=handle, R1=data, R2=length
104: control a device  R0=handle, R1=command, R2=data, R3=length
```

#### 6. Compiling

```bash
# compile
dotnet run --project VMLPrepares/BasicCompiler input.bas -o output.vml

# run (character mode)
dotnet run --project VMLEmulators/ConsoleEmulator -- --run output.vml

# run (VGA mode)

# run (GUI graphics mode)
dotnet run --project VMLEmulators/FullDevicesEmulator
```

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

---

#### v1.66.33 improvements (2026-08-01)

##### Every dialect at 90%+ completeness
- «bold»PureBasic«/»: GLOBAL → SHARED, PROCEDURE → SUB (Lexer mapping)
- «bold»ChipBasic«/»: PINMODE/DIGITALWRITE/DIGITALREAD → GpioStatement
- «bold»FreeBasic«/»: ENUM compiles; PTR/CAST/EXTENDS/OPERATOR are recognised
- «bold»TrueBasic«/»: MAT/ZER/CON are recognised

##### CLASS/OOP support
- The CLASS keyword maps to TYPE_KW and is parsed uniformly
- Class declaration: CLASS name ... END CLASS (fields + methods)
- Constructor: a CONSTRUCTOR block
- Method: METHOD name(params) ... END METHOD
- Property: PROPERTY (the token is registered)
- Destructor: DESTRUCTOR (the token is registered)

##### Lexer
- «bold»140 keywords, 171 `TokenType`s«/» (measured, v0.96.506 — this used to say 116 / 141)
- Supports the dialect-specific keywords of all 10 dialects
- Keywords are case-insensitive (LexerBase.ReadIdentifier)

##### Preprocessor
- Supports the `#param lib("name")` / `#param path("dir")` directives
- Supports `#include`, `#define`, `#undef`, `#if`/`#else`/`#endif`
- Defines a macro automatically for each dialect: `__QBASIC__`, `__FREEBASIC__` and so on

##### Test coverage
- «bold»63 unit tests«/» (Lang_BASIC.cs)
- 22 multi-dialect tests (7 dialects x dialect-specific keywords)
- 5 TYPE/CLASS structure tests
- FreeBasic example programs (14 test files)

##### Known limitations
- CLASS method bodies are not yet linked to their call sites (Phase 3)
- EXTENDS/INTERFACE semantics are yet to be implemented
- GPIO statements compile but generate no actual call
- PTR/CAST pointer semantics are yet to be implemented

The shared libraries already provide wide-string conversion functions (wchar.h/uchar.h), which each language compiler can use as needed.

## Compiler README

### BASIC compiler

«bold»Path«/»: `VMLPrepares/BasicCompiler/` | «bold»Version«/»: v1.66.33
«bold»Completeness«/»: 10 dialects, ≥90% each | 🟢 production ready (graphics/sound/files already work on phones)
⚠ «bold»The test numbers (78 / Lang_BASIC, `VMLTests/`, `test/basic_examples/`) belong to the upstream repository«/» —
  this repository (the mobile copy after the fork) does «bold»not«/» have the `VMLTests/` or `test/` directories, so those paths and numbers cannot be reproduced here.
  The check for this repository is `scripts/vml-basic-probe/` (54 cases + 5 `fileio.sh` cases).
«bold»Standard library«/»: `Lib/basic/`

#### Multiple dialect support

The `--basictype` option selects one of 10 BASIC dialects:

| Dialect | CLI value | Family | Completeness |
|------|--------|------|:--:|
| QBasic (default) | `qbasic` | Microsoft | 92% |
| GW-BASIC | `gwbasic` | Microsoft | 90% |
| VisualBasic 6 | `visualbasic` | Microsoft | 90% |
| TurboBasic | `turbobasic` | Borland | 90% |
| PowerBASIC | `powerbasic` | Borland | 90% |
| FreeBasic | `freebasic` | open source | 90% |
| PureBasic | `purebasic` | commercial | 90% |
| TrueBasic | `truebasic` | ANSI/ISO | 90% |
| ChipBasic | `chipbasic` | MCU | 90% |
| MiniBasic | `minibasic` | educational | 90% |

```bash
vmltool -x basic --basictype freebasic test.bas -o test.vml
vmltool -x basic --basictype gwbasic test.bas -o test.vml
```

#### Keyword implementation

⚠ «bold»These numbers did not match the code for a long time«/» (this README said 81/99, the same file further down said 116+, and the SPEC said 116/141).
Recalculated from measurements in v0.96.506: «bold»140 keywords in the lexer table, 171 `TokenType`s«/»;
the 99-item table in `ALL_KEYWORDS.md` measures «bold»89 implemented / 10 unimplemented«/».
«bold»`ALL_KEYWORDS.md` is the authority«/» — it marks the measured status of every entry.

##### Fully implemented
- ✅ Control flow: IF/THEN/ELSE/ELSEIF/SELECT CASE/FOR/NEXT/WHILE/WEND/DO/LOOP/GOTO/GOSUB
- ✅ Subroutines: SUB/END SUB/FUNCTION/CALL/DECLARE/BYVAL/BYREF/PROCEDURE
- ✅ Variables: DIM/LET/CONST/SWAP/ERASE/REDIM/SHARED/COMMON/STATIC/LOCAL/GLOBAL
- ✅ I/O: PRINT/INPUT/OPEN/CLOSE/WRITE/LPRINT/PRINT USING/FREEFILE/LINE INPUT #
  (⚠ the file family only truly worked from v0.96.503/504: before that the OPEN handle was never stored at all,
    `PRINT #`/`INPUT #` used a «bold»device«/» number rather than a «bold»file«/» number, and `FREEFILE` was only a lexer entry.
    All of them now go through the sandbox-aware `#110-113`; the check is `scripts/vml-basic-probe/fileio.sh`)
- ⚠️ OOP (FreeBasic): CLASS/METHOD/ENUM ✅; «bold»CONSTRUCTOR only goes as far as parsing, DESTRUCTOR has no code«/»
- ✅ Maths: ABS/SGN/SQR/SIN/COS/TAN/INT/FIX/RND/RANDOMIZE
- ✅ Strings: LEN/CHR$/ASC/STR$/VAL/LEFT$/RIGHT$/MID$
- ⚠️ MCU: CHIPASM/ASM/BLOAD/BSAVE — «bold»`ASM` has been removed from the lexer table«/» (the comment says "C/ObjC/C++ only");
  `BLOAD`/`BSAVE` do generate GPIO calls but are «bold»missing a runtime library«/», and phones have no GPIO ⇒ unusable in practice
- ✅ Other: REM/DATA/READ/RESTORE/ON ERROR/RESUME/SLEEP/SYSTEM/BEEP/SOUND

##### Partially implemented
- ⚠️ PROPERTY, CONSTRUCTOR (the framework is ready, still to be finished)

##### Not implemented (needs a type system)
- ❌ PTR, CAST, EXTENDS, OPERATOR, INTERFACE

#### Dialect-specific features

| Feature | Dialects | Description |
|------|------|------|
| CLASS/OOP | FreeBasic | class/constructor/destructor/method/enum |
| GPIO | ChipBasic | PINMODE/DIGITALWRITE/DIGITALREAD |
| GLOBAL | PureBasic | maps to SHARED |
| PROCEDURE | PureBasic | maps to SUB |
| ENUM value assignment | FreeBasic | compile-time constants (RED+GREEN=30) |
| NEW | PureBasic | maps to EmitAlloc heap allocation |
| Private/Public | VisualBasic | modifier recognition |

#### File structure

```
BasicCompiler/
├── ASTNode.cs                 # AST nodes (measured: 171 TokenTypes)
├── Lexer.cs                   # lexing (measured: 140 keywords)
├── Parser.cs / Parser.*.cs    # parsing (10 dialects supported)
├── CodeGenerator.cs / *.cs    # code generation (TypedCodeGen<BasicType>)
├── BasicCompilerPlugin.cs     # plugin entry point + compilation pipeline
├── TokenType.cs               # the Token type enum
├── BASIC_LANGUAGE_SPEC.md     # language specification v2.1
├── ALL_KEYWORDS.md            # 99 keywords x 10 dialects
├── UNIMPLEMENTED_FEATURES.md  # list of unimplemented features
└── README.md                  # this file
```

#### Usage

```bash
# QBasic (default)
vmltool input.bas -o output.vml

# pick a dialect
vmltool -x basic --basictype freebasic input.bas -o output.vml
vmltool -x basic --basictype visualbasic input.bas -o output.vml

# compile + run
vmltool input.bas -o output.vml && vmltool -r output.vml
```

#### Tests

- «bold»78 unit tests«/» (`VMLTests/Lang_BASIC.cs`): dialect keywords + TYPE/CLASS + ENUM
- «bold»22 dialect tests«/»: dialect-specific keywords for 7 dialects + cross-dialect compatibility
- «bold»14 FreeBasic examples«/» (`test/basic_examples/freebasic/`): class/constructor/method/pointers and more
- «bold»QBasic games«/»: gorillas.bas, nibbles.bas

```bash
# run all BASIC tests
dotnet test VMLTests/VMLTests.csproj --filter "ClassName~Lang_BASIC"

# run the modern-feature tests
dotnet test VMLTests/VMLTests.csproj --filter "ClassName~Lang_ModernFeatures"
```
