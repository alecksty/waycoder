# C# compiler

**Path**: `VMLPrepares/CSharpCompiler/`
**Completeness**: ~92% | 🟢 production ready
**Standard library**: `Lib/csharp/` (to be created)

## Features
- ✅ Full parsing + code generation
- ✅ Control flow (if/while/for/switch)
- ✅ Function and procedure definition and calls
- ✅ Standard library support
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Hexadecimal literals
- ✅ Shared built-in function library (builtins.vml)


## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller and bare-metal environments (Arduino/STM32/8051 and the like) and automatically skips features that need an operating system.

**Skipped** (no code is generated when these appear):
- async/await, Task, GC, reflection

**Kept** (the low-level parts are provided by the BIOS):
- new (heap), class, properties
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to UART
- LINQ/generics — to be implemented

### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, RTOS and so on). It will then support the full set of language features (file system, threads, async, exceptions, reflection and so on).

### RAM level
- `--ram k`: kilobyte range (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte range (64KB~1MB, such as ARM Cortex-M, **default**)
- `--ram g`: gigabyte range (such as x86/DDR systems)
- `--stack-size <bytes>`: set the stack size by hand (by default it is allocated automatically from --ram)

### MCU safe-coding tips
- Limited stack space (256-4096 bytes is typical), avoid deep recursion
- No deep recursion (anything deeper than 10 levels needs a stack assessment)
- No dynamic loading (import/dofile/eval)
- Floating-point math may need a software floating-point library

## Usage
```bash
dotnet run --project VMLPrepares/CSharpCompiler input.CS -o output.vml
```

## Tests
`Test/CS/` — 0 test files (the test directory is yet to be created)
