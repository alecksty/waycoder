# R compiler

**Path**: `VMLPrepares/RCompiler/`
**Completeness**: ~96% | 🟢 production ready
**Standard library**: `Lib/r/`

## Features
- ✅ Lexing + parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/else/for/while/repeat/break/next)
- ✅ Functions: `function()` definition / parameters / return / anonymous functions
- ✅ Assignment: `<-` / `<<-` / `=` operators
- ✅ Data structures: `c()` vectors / `list()` / `~` formula operator
- ✅ `%in%` operator
- ✅ Scientific-notation numbers
- ✅ 31 tests, 0 failures
- ⚠️ S3/S4 object system — not implemented
- ⚠️ data.frame/matrix — not implemented
- ✅ C-style preprocessing (`#ifdef`/`#ifndef`/`#define`/`#include`)

## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller / bare-metal environments.

**Skipped**:
- `source()` dynamic loading
- S3/S4 object system
- Graphics devices

**Kept**:
- Basic statistic operations
- Vector operations (c/seq/rep)
- `print` mapped to UART

### OS mode (`--mode os`)
OS mode supports the full set of language features.

### RAM level
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

## Usage
```bash
dotnet run --project VMLTool -- input.r -o output.vml
```

## Tests
- `VMLTests/NewCompilerTests.cs` — 31 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full pipeline tests
- `Examples/r/` — example programs (start/factorial/file_io/info)
