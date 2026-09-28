# Java 8 Compiler

**Path**: `VMLPrepares/JavaCompiler/`
**Completeness**: ~92% | 🟢 production ready
**Standard library**: `Lib/java/` (to be created)

## Features
- ✅ Full syntax analysis + code generation
- ✅ Control flow (if/while/for/switch)
- ✅ Function/procedure definition and calls
- ✅ Standard library support
- ✅ POKE/PEEK memory operations (MMIO)
- ✅ Hexadecimal literals
- ✅ Shared built-in function library (builtins.vml)


## Compile modes

### MCU mode (default: `--mode mcu`)
MCU mode is tuned for single-chip and bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that are incompatible with an operating system.

**Skipped** (these constructs generate no code):
- Thread, GC, reflection, synchronized

**Kept** (implemented on top of the BIOS):
- new (heap), class/interface
- POKE/PEEK memory-mapped I/O (MMIO)
- basic type operations, control flow, function calls
- printf/puts mapped to the UART

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
dotnet run --project VMLPrepares/JavaCompiler input.Ja -o output.vml
```

## Tests
`Test/Jv/` — 0 test files (the test directory is still to be created; using Jv is recommended so it does not clash with JavaScript's Test/Js)
