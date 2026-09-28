# Ladder Diagram language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Standards

| Field | Value |
|:-----|:----|
| **Target standard** | IEC 61131-3 subset (2003) |
| **Publication year** | 2003 |
| **Completeness** | ~98% |
| **MCU completeness** | ~97% |
| **Tests** | 0 (the test directory is yet to be created) |
| **Updated** | 2026-05-18: timers reimplemented on the SYSCALL #53 real-time clock; code generator status corrected |

## Keywords (instructions)

**Contacts**: `NO` (normally open) `NC` (normally closed) `POS` (positive edge) `NEG` (negative edge)
**Coils**: `OUT` (output) `SET` (set) `RST` (reset)
**Timers**: `TON` (on-delay) `TOF` (off-delay) `TP` (pulse)
**Counters**: `CTU` (up counter) `CTD` (down counter) `CTUD` (up/down counter)
**Comparison**: `EQ` `NE` `GT` `GE` `LT` `LE`
**Arithmetic**: `ADD` `SUB` `MUL` `DIV` `MOD`
**Program control**: `JMP` `LBL` `RET` `END`
**Other**: `MOVE` `SEL` `MUX` `LIMIT`

## Overview

This compiler compiles the IEC 61131-3 Ladder Diagram language into VML (Virtual Machine Language) assembly. Ladder Diagram is a graphical programming language widely used in industrial control, for programming programmable logic controllers (PLCs).

## Language features

### 1. Basic ladder elements

#### Contacts
- **Normally open contact**: ─┤ ├─
- **Normally closed contact**: ─┤/├─
- **Positive edge contact**: ─┤P├─
- **Negative edge contact**: ─┤N├─

#### Coils
- **Output coil**: ─( )─
- **Set coil**: ─(S)─
- **Reset coil**: ─(R)─
- **Latch coil**: ─(L)─

#### Function blocks
- **Timers**: TON (on-delay), TOF (off-delay), TP (pulse)
- **Counters**: CTU (up counter), CTD (down counter), CTUD (up/down counter)
- **Comparators**: equal, not equal, greater than, less than and so on
- **Math operations**: add, subtract, multiply, divide and so on

### 2. Data types (IEC 61131-3)

#### Elementary data types
- **BOOL**: boolean value (1 bit)
- **BYTE**: unsigned 8-bit integer
- **WORD**: unsigned 16-bit integer
- **DWORD**: unsigned 32-bit integer
- **INT**: signed 16-bit integer
- **DINT**: signed 32-bit integer
- **REAL**: 32-bit floating point
- **STRING**: string
- **TIME**: time type

#### Derived data types
- **Arrays**: `ARRAY[1..10] OF INT`
- **Structures**:
```iec
TYPE MotorControl :
STRUCT
    StartButton : BOOL;
    StopButton : BOOL;
    MotorRun : BOOL;
    Speed : INT;
END_STRUCT
END_TYPE
```

### 3. Program structure

#### Program declaration
```iec
PROGRAM MotorControl
VAR
    StartButton AT %IX0.0 : BOOL;
    StopButton AT %IX0.1 : BOOL;
    MotorRun AT %QX0.0 : BOOL;
    Timer1 : TON;
END_VAR
```

#### Variable declaration sections
- **VAR**: local variables
- **VAR_INPUT**: input variables
- **VAR_OUTPUT**: output variables
- **VAR_IN_OUT**: input/output variables
- **VAR_GLOBAL**: global variables
- **VAR_TEMP**: temporary variables

#### I/O mapping
- **Input**: `%IX0.0` (byte 0, bit 0)
- **Output**: `%QX0.0` (byte 0, bit 0)
- **Memory**: `%MW0` (word 0)
- **Holding register**: `%MD0` (double word 0)

### 4. Ladder rung structure

#### Simple rung
```
     Start    Stop     Motor
     ─┤ ├─────┤/├──────( )─
       I0.0    I0.1      Q0.0
```

#### Parallel branch
```
     Button1     Lamp1
     ─┤ ├────┬───( )─
            │
     Button2│
     ─┤ ├────┘
```

#### Series branch
```
     Start     Timer1.PT := T#5s
     ─┤ ├──────[TON]─────
       I0.0     Timer1
                IN  Q
                PT  ET
```

#### Function block call
```
     Start     MotorCtrl
     ─┤ ├──────[FB]─────
                EN  ENO
               Start Run
               Stop  Fault
               Speed ActualSpeed
```

### 5. Timers and counters

#### Timer (TON - on-delay)
```iec
VAR
    Timer1 : TON;
    TimeValue : TIME := T#5s;
END_VAR

// ladder representation
     Start     Timer1
     ─┤ ├──────[TON]─────
                IN  Q
               PT  ET
```

**Function**: when IN is TRUE the timer starts; once the PT time has elapsed Q becomes TRUE. ET shows the elapsed time.

#### Counter (CTU - up counter)
```iec
VAR
    Counter1 : CTU;
    PresetValue : INT := 10;
END_VAR

// ladder representation
     Pulse     Counter1
     ─┤P├──────[CTU]─────
                CU  Q
               PV  CV
                R
```

**Function**: counts on the positive edge of CU; Q is TRUE when CV >= PV; the R signal resets the counter.

### 6. Instruction List (IL) support

The ladder compiler also supports the Instruction List text format:

```iec
LD StartButton
ANDN StopButton
OUT MotorRun

LD MotorRun
TON Timer1, T#5s
```

### 7. Structured Text (ST) support

Structured Text can be embedded in a ladder diagram:

```iec
IF StartButton AND NOT StopButton THEN
    MotorRun := TRUE;
    Timer1(IN:=TRUE, PT:=T#5s);
END_IF;
```

### 8. Standard functions

#### Bit operation functions
- **AND**, **OR**, **XOR**, **NOT** - logical operations
- **SHL**, **SHR** - shift operations
- **ROL**, **ROR** - rotate operations

#### Math functions
- **ADD**, **SUB**, **MUL**, **DIV** - arithmetic operations
- **MOD** - modulo
- **ABS** - absolute value
- **SQRT** - square root
- **LN**, **EXP** - logarithm and exponent
- **SIN**, **COS**, **TAN** - trigonometric functions

#### Comparison functions
- **EQ** (=), **NE** (<>), **GT** (>), **GE** (>=), **LT** (<), **LE** (<=)

#### Type conversion functions
- Type conversions such as **BOOL_TO_INT**, **INT_TO_REAL**
- **TRUNC**, **ROUND** - rounding functions

#### String functions
- **CONCAT** - string concatenation
- **LEFT**, **RIGHT**, **MID** - substrings
- **LEN** - string length
- **FIND** - find a substring

### 9. VML code generation conventions

#### Bit operation mapping
```
Ladder contact: ─┤ ├─
VML code:       MOVE R0, [I0.0_address]
                CMP R0, #1
                JNE skip_branch

Ladder coil:    ─( )─
VML code:       MOVE R0, #1
                MOVE [Q0.0_address], R0
```

#### Timer implementation (using the SYSCALL #53 GetTick real-time clock)

TON (on-delay timer):
- On detecting a positive edge on the IN input, record startTime = SYSCALL #53 (GetTick)
- ET = GetTick() - startTime
- When ET >= PT the Q output is set to TRUE
- When IN is FALSE, reset ET = 0, Q = FALSE

TOF (off-delay timer):
- On detecting a negative edge on the IN input, record startTime
- ET = GetTick() - startTime
- Reset when IN goes back to TRUE

TP (pulse timer):
- A positive edge on the IN input triggers the pulse
- ET = GetTick() - startTime
- The Q output stays TRUE for the duration of the pulse

CTUD (up/down counter):
- On a positive edge of CU with CV < 32767: CV++
- On a positive edge of CD with CV > -32768: CV--
- R resets CV = 0, LD loads CV = PV
- QU: TRUE when CV >= PV; QD: TRUE when CV <= 0

#### Counter implementation (with overflow protection)

CTU (up counter):
- Counts on a positive edge of CU
- Check CV < 32767 before incrementing CV (overflow protection)
- Q = TRUE when CV >= PV
- The R signal resets CV = 0

CTD (down counter):
- Counts on a positive edge of CD
- Check CV > -32768 before decrementing CV (underflow protection)
- Q = TRUE when CV <= 0
- The LD signal loads CV = PV

CTUD (up/down counter):
- On a positive edge of CU with CV < 32767: CV++
- On a positive edge of CD with CV > -32768: CV--
- R resets CV = 0, LD loads CV = PV
- QU: TRUE when CV >= PV; QD: TRUE when CV <= 0

### 10. I/O memory mapping

#### Input image area
```
Address range: 0x8000 - 0x80FF (256 bytes)
Purpose: stores digital input states
Access: MOVE R0, [0x8000]  ; read the first input byte
```

#### Output image area
```
Address range: 0x8100 - 0x81FF (256 bytes)
Purpose: stores digital output states
Access: MOVE [0x8100], R0  ; write the first output byte
```

#### Holding register area
```
Address range: 0x8200 - 0x83FF (512 bytes)
Purpose: stores intermediate variables, timers, counters and so on
```

#### Special registers
```
Address: 0x8400 - system time (milliseconds)
Address: 0x8404 - scan cycle
Address: 0x8408 - error code
```

### 11. Scan cycle model

A ladder program executes according to the scan cycle:

```
Start scan
├── Read physical inputs into the input image area
├── Execute the user program (ladder logic)
├── Write the output image area to the physical outputs
└── Handle system tasks (communication, diagnostics and so on)
```

VML implementation:
```vml
LABEL main_scan_cycle
    ; 1. read inputs
    CALL read_physical_inputs
    
    ; 2. execute the user program
    CALL user_program
    
    ; 3. write outputs
    CALL write_physical_outputs
    
    ; 4. update system time
    CALL update_system_timer
    
    ; 5. check the scan cycle time
    MOVE R0, [current_time]
    SUB R0, [scan_start_time]
    CMP R0, [max_scan_time]
    JLE scan_ok
    
    ; scan timeout error
    MOVE R0, #1
    MOVE [scan_timeout_error], R0
    
scan_ok:
    ; wait for the next scan cycle
    CALL delay_scan_cycle
    JMP main_scan_cycle
```

### 12. Example programs

#### Motor start/stop control
```iec
PROGRAM MotorControl
VAR
    StartButton AT %IX0.0 : BOOL;
    StopButton AT %IX0.1 : BOOL;
    MotorRun AT %QX0.0 : BOOL;
    Overload AT %IX0.2 : BOOL;
    Timer1 : TON;
END_VAR

// ladder logic
     Start    Stop    Overload   Motor
     ─┤ ├─────┤/├─────┤/├──────( )─
       I0.0    I0.1    I0.2      Q0.0
     
     Motor     Timer1
     ─┤ ├──────[TON]─────
                IN  Q
               PT  ET
              T#5s
     
     Timer1.Q    Alarm
     ─┤ ├────────( )─
                     Q0.1
```

#### Conveyor line control
```iec
PROGRAM ConveyorControl
VAR
    Sensor1 AT %IX0.0 : BOOL;
    Sensor2 AT %IX0.1 : BOOL;
    Motor1 AT %QX0.0 : BOOL;
    Motor2 AT %QX0.1 : BOOL;
    Counter1 : CTU;
    PartsCount : INT;
END_VAR

// product counting
     Sensor1    Counter1
     ─┤P├──────[CTU]─────
                CU  Q
               PV  CV
               10
                R
     
     Counter1.Q   PartsCount := Counter1.CV
     ─┤ ├─────────────────[MOV]─────
     
// conveyor control
     Sensor1    Sensor2    Motor1
     ─┤ ├───────┤/├────────( )─
     
     Sensor2           Motor2
     ─┤ ├──────────────( )─
```

### Floating-point and 64-bit compile modes

The VML toolchain controls the handling strategy for floating point and 64-bit integers through three compile parameters:

| Parameter | Options | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit float (REAL) handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit float handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer handling mode |

#### 32-bit float (float32)

The `REAL` type in this language (32-bit single-precision float) is compiled in the following modes:

- **`hard` mode (default)**: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, computing directly through the sixteen floating-point registers F0-F15. Best performance; suited to target platforms with floating-point hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point operations through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms without floating-point hardware.
- **`none` mode**: disables all 32-bit float types and reports a compilation error when a `REAL` declaration is encountered.

#### 64-bit float (double)

The VML compile layer supports 64-bit double-precision floating-point arithmetic. The ladder language itself does not define the corresponding `LREAL` type, but the compiler reserves extension support for it.

- **`soft` mode (default)**: uses the IEEE 754 double-precision software emulation library `softdouble.c`.
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`.
- **`none` mode**: disables the double-precision float extension.

#### 64-bit integer (int64)

The 64-bit integer types such as `DLONG` / `LINT` in the IEC 61131-3 ladder language are compiled in the following modes:

- **`soft` mode (default)**: uses the dual-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- **`hard` mode**: reserved; a future VML version will support native 64-bit integer instructions.
- **`none` mode**: disables 64-bit integer types.

#### Software emulation libraries

All the software emulation libraries above live in the `Lib/shared/` directory, are written in C and are compiled to VML by the C compiler; every language shares them:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit float emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit float emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer dual-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

### 13. Compilation limitations and notes

#### Current implementation status
- **Lexer**: fully implemented, supports the IEC 61131-3 keywords
- **Parser**: fully implemented, supports both ladder diagram and Structured Text
- **Code generator**: basic implementation (CodeGenerator.Elements.cs), supporting contacts/coils/timers/counters
- **Timers**: TON/TOF/TP use the SYSCALL #53 (GetTick) real-time clock
- **Counters**: CTU/CTD/CTUD with overflow protection
- **Standard library**: a ladder standard library (stdlib.vml) still needs to be created

#### Features still to be implemented
1. Code generation from ladder diagram to VML instructions — ⚠️ essentially complete
2. Timer and counter function blocks — ✅ implemented (TON/TOF/TP/CTU/CTD/CTUD)
3. I/O memory mapping management
4. Scan cycle scheduling
5. Standard function library implementation (MOVE/SEL/MUX/LIMIT)

#### Technical challenges
1. **Graphics to text conversion**: the ladder diagram is a graphical language; a textual instruction list is used as the intermediate representation
2. **Real-time requirements**: PLC programs have strict real-time requirements — timers use the SYSCALL #53 millisecond clock
3. **Bit operations**: contacts/coils need efficient bit operations and edge detection
4. **Retentive variables**: variable values have to be retained across a power loss

### 14. Integration with the VML runtime

A ladder program interacts with the VML runtime in the following ways:

- **I/O access**: physical devices are accessed through memory-mapped I/O addresses
- **Timer services**: TON/TOF/TP are implemented with the system timer
- **Interrupt handling**: hardware interrupt events are supported
- **Diagnostics**: scan cycle monitoring, error handling

#### System calls
- **SYSCALL #3**: program exit
- **SYSCALL #53**: GetTick — get a millisecond timestamp (the timing base for timers)
- **I/O access**: physical devices are accessed through memory-mapped I/O addresses (contacts/coils)

### 15. Application areas

The ladder diagram compiler is mainly used in:
- Industrial automation control systems
- Building automation
- Process control
- Machinery and equipment control
- Energy management systems

By compiling ladder diagrams into VML code, traditional PLC programs can run on the VML virtual machine, enabling cross-platform porting and simulation testing of industrial control programs.
---

## 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU mode** (default): string output is UTF-8 (`.string`)
**OS mode**: the encoding can be determined through the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h) that each language compiler can use as needed.
