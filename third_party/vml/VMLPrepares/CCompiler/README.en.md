# C Compiler

**Path**: `VMLPrepares/CCompiler/`
**Completeness**: ~97% | 🟢 production ready
**Standard library**: `Lib/c/` (29 files)

## Features
- ✅ Full C99 standard (ISO/IEC 9899:1999)
- ✅ Pointer arithmetic, multi-dimensional arrays, struct/union
- ✅ Preprocessor (#include, #define, #ifdef)
- ✅ Inline assembly (`__chipasm__`)
- ✅ POKE/PEEK memory operations
- ✅ Complete standard library (stdio, stdlib, string, math, conio, graphics)
- ✅ Interrupt / peripheral support (interrupt)


## Compile modes

### MCU mode (default: `--mode mcu`)
MCU mode is tuned for single-chip and bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that are incompatible with an operating system.

**Skipped** (these constructs generate no code):
- None (C99 has no async, threads or reflection)

**Kept** (implemented on top of the BIOS):
- malloc/free, fopen/fread, printf %f, va_list
- POKE/PEEK memory-mapped I/O (MMIO)
- the `interrupt` keyword (interrupt service routines)
- basic type operations, control flow, function calls
- printf/puts mapped to the UART
- raw pointers (with restrictions)

### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, an RTOS, and so on). It will support the full language feature set (file system, threads, async, exceptions, reflection).

### RAM levels
- `--ram k`: kilobyte range (2KB~64KB, e.g. 8051/PIC/AVR)
- `--ram m`: megabyte range (64KB~1MB, e.g. ARM Cortex-M, **default**)
- `--ram g`: gigabyte range (e.g. x86/DDR systems)
- `--stack-size <bytes>`: set the stack size by hand (by default it is allocated automatically from `--ram`)

### MCU safe-coding tips
- Stack space is limited (256-4096 bytes typically), so avoid deep recursion
- No deep recursion (>10 levels means you should evaluate the stack)
- No dynamic loading (import/dofile/eval)
- Floating point may need a software floating-point library

## Usage
```bash
dotnet run --project VMLPrepares/CCompiler input.c -o output.vml
vmltool input.c -o output.vml
```

## Tests
`Test/c/` — 190+ test files
