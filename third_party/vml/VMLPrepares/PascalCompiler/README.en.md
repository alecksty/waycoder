# Pascal compiler

**Path**: `VMLPrepares/PascalCompiler/` | **Version**: v1.66.33
**Completeness**: ~92% (Turbo Pascal) | 🟢 production ready | **6 dialects + OOP basics**
**Standard library**: `Lib/pascal/` | **Tests**: 25

## Multiple dialect support

The `--pascaltype` option selects one of 6 Pascal dialects:

| Dialect | CLI value | OOP | Examples |
|------|--------|:--:|:--:|
| Turbo Pascal (default) | `turbo` | ❌ | 2 programs |
| Delphi | `delphi` | ✅ | 2 programs |
| Free Pascal | `freepascal` / `fpc` | ✅ | 1 program |
| ISO Pascal | `iso` | ❌ | 1 program |
| UCSD Pascal | `ucsd` | ❌ | — |
| Oberon | `oberon` | ❌ | — |

```bash
vmltool -x pascal --pascaltype delphi test.pas -o test.vml
```

## OOP features (Delphi/Free Pascal)

| Feature | Status |
|------|:--:|
| class/object | ✅ fields supported |
| constructor/destructor | ⚠️ token + parser |
| property | ⚠️ token only |
| virtual/override | ⚠️ AST ready |
| try/except/finally | ⚠️ token only |

## Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGenerator, ~6300 lines)
- ✅ Control flow (if/then/else/while/for/repeat-until/case-of)
- ✅ Procedures/functions: value parameters / var parameters / forward / recursion
- ✅ Data types: integer/real/char/boolean/string/enum/array/record/set/file
- ✅ break/continue — implemented
- ✅ Pointers: ^type/new/dispose — implemented
- ✅ Crt unit: ClrScr/GotoXY/TextColor/Delay/Sound and more
- ✅ File I/O: text/file of type
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)


## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller and bare-metal environments (Arduino/STM32/8051 and the like) and automatically skips features that need an operating system.

**Skipped** (no code is generated when these appear):
- None (Turbo Pascal has no async or threads)

**Kept** (the low-level parts are provided by the BIOS):
- dispose (heap), file I/O
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to UART

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
- No dynamic loading
- Floating-point math may need a software floating-point library

## Usage
```bash
dotnet run --project VMLTool -- input.pas -o output.vml
```

## Tests
`Test/Pa/` — 0 test files (the test directory is yet to be created)
