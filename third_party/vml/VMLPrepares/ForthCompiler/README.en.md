# Forth (ANSI Forth subset) compiler

**Path**: `VMLPrepares/ForthCompiler/`
**Completeness**: ~95% | 🟢 production ready
**Standard library**: `Lib/forth/`

## Features
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


## Compile modes

### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that do not fit such a system.

**Skipped** (no code is generated when these appear):
- None (Forth started out on embedded systems, so it is MCU-compatible by nature)

**Kept** (implemented at the low level by the BIOS):
- ALLOT (heap), HERE
- POKE/PEEK memory-mapped I/O (MMIO)
- Stack operations, control flow, word definitions
- printf/puts mapped onto the UART

### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, an RTOS, and so on). It will support the full set of language features (file system, threads, async, exceptions, reflection, and so on).

### RAM level
- `--ram k`: kilobyte level (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte level (64KB~1MB, such as ARM Cortex-M, **default**)
- `--ram g`: gigabyte level (such as an x86/DDR system)
- `--stack-size <bytes>`: set the stack size by hand (by default it is allocated automatically from `--ram`)

### MCU safe-coding tips
- Stack space is limited (typically 256-4096 bytes), so avoid deep recursion
- The Forth parameter stack and return stack both need generous headroom
- Dynamic loading is not allowed
- Floating-point work may need a software floating-point library

## Usage
```bash
dotnet run --project VMLTool -- input.fs -o output.vml
```

## Tests
`Test/Fo/` — 0 test files (the test directory is yet to be created)
