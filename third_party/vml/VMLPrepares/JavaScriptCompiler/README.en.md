# JavaScript (ECMAScript 5) Compiler

**Path**: `VMLPrepares/JavaScriptCompiler/`
**Completeness**: ~94% | 🟢 Production ready
**Standard library**: `Lib/javascript/` (to be created)

## Features
- ✅ Full syntax analysis + code generation
- ✅ Control flow (if/while/for/switch)
- ✅ Function/procedure definitions and calls
- ✅ Standard library support
- ✅ POKE/PEEK memory operations (MMIO)
- ✅ Hexadecimal literals
- ✅ Shared built-in function library (builtins.vml)


## Compilation modes

### MCU mode (default `--mode mcu`)
MCU mode is optimized for microcontroller/bare-metal environments (Arduino/STM32/8051 etc.) and automatically skips features that are incompatible with an operating system.

**Skipped** (no code is generated for these):
- async/await, Promise, eval

**Retained** (implemented at the BIOS level):
- Closures, objects, arrays
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts map to UART

### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (such as embedded Linux, RTOS). It will then support the full set of language features (file system, threads, async, exceptions, reflection, etc.).

### RAM levels
- `--ram k`: kilobyte level (2 KB~64 KB, e.g. 8051/PIC/AVR)
- `--ram m`: megabyte level (64 KB~1 MB, e.g. ARM Cortex-M, **default**)
- `--ram g`: gigabyte level (e.g. x86/DDR systems)
- `--stack-size <bytes>`: set the stack size manually (by default it is allocated automatically from --ram)

### MCU-safe coding tips
- Limited stack space (256-4096 bytes typical), avoid deep recursion
- No deep recursion (beyond 10 levels, evaluate the stack)
- No dynamic loading (import/dofile/eval)
- Floating-point arithmetic may need a software floating-point library

## Usage
```bash
dotnet run --project VMLPrepares/JavaScriptCompiler input.Js -o output.vml
```

## Tests
`Test/Js/` — 0 test files (the test directory is to be created; use Js to avoid clashing with the Java Test/Jv)
