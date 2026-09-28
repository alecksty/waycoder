# C++11 compiler

**Path**: `VMLPrepares/CppCompiler/`
**Completeness**: ~95% | 🟢 production ready
**Standard library**: `Lib/cpp/`

## Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/while/for/switch)
- ✅ Functions / operator overloading / friend
- ✅ Classes / inheritance / virtual functions / vtable
- ⚠️ Templates — parsed and skipped, no code generated
- ⚠️ Lambda — basic support
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Namespaces / casts
- ✅ Shared built-in function library (builtins.vml)


## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller and bare-metal environments (Arduino/STM32/8051 and the like) and automatically skips features that need an operating system.

**Skipped** (no code is generated when these appear):
- throw/catch, typeid, dynamic_cast

**Kept** (the low-level parts are provided by the BIOS):
- new/delete (heap), iostream, STL containers
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to UART
- Raw pointers (with restrictions)

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
dotnet run --project VMLPrepares/CppCompiler input.Cp -o output.vml
```

## Tests
`Test/Cp/` — 0 test files (the test directory is yet to be created)
