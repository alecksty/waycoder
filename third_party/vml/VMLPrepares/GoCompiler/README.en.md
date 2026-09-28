# Go Compiler

**Path**: `VMLPrepares/GoCompiler/`
**Completeness**: ~90% | 🟢 production ready
**Standard library**: `Lib/go/`

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
- goroutine, chan, select

**Kept** (implemented on top of the BIOS):
- map (hash table), slice (heap), interface dynamic dispatch
- POKE/PEEK memory-mapped I/O (MMIO)
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
dotnet run --project VMLPrepares/GoCompiler input.Go -o output.vml
```

## Tests
`Test/Go/` — 21 test files
