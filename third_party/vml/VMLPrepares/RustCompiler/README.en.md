# Rust compiler

**Path**: `VMLPrepares/RustCompiler/`
**Completeness**: ~93% | 🟢 production ready
**Standard library**: `Lib/rust/`

## Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/while/for/loop/match)
- ✅ Function (fn) definition and calls
- ⚠️ struct/enum/trait — partially supported
- ⚠️ Ownership/borrowing — basic parsing
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)


## Compilation modes

### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments (Arduino/STM32/8051 and so on), and automatically skips features that are incompatible with an operating system.

**Skipped** (no code is generated for this syntax):
- async/await

**Kept** (implemented underneath by the BIOS):
- Box/Vec/String (heap), ownership/borrowing
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to the UART
- Raw pointers (with restrictions)

### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (such as embedded Linux or an RTOS), and will support every language feature (file system, threads, asynchronous I/O, exceptions, reflection and so on).

### RAM levels
- `--ram k`: kilobyte level (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte level (64KB~1MB, such as ARM Cortex-M, **default**)
- `--ram g`: gigabyte level (such as x86/DDR systems)
- `--stack-size <bytes>`: specify the stack size by hand (by default it is allocated automatically from `--ram`)

### MCU-safe coding tips
- Limited stack space (256-4096 bytes is typical), so avoid deep recursion
- Deep recursion is not allowed (beyond 10 levels the stack must be evaluated)
- Dynamic loading (import/dofile/eval) is not allowed
- Floating-point arithmetic may require the soft-float library

## Usage
```bash
dotnet run --project VMLPrepares/RustCompiler input.Ru -o output.vml
```

## Tests
`Test/Ru/` — 0 test files (the test directory is yet to be created)
