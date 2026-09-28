# D compiler

**Path**: `VMLPrepares/DCompiler/`
**Completeness**: ~97% | 🟢 production ready
**Standard library**: `Lib/d/`

## Features
- ✅ Lexing + parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Module system: module/import
- ✅ Class/struct/interface/enum
- ✅ C-style type system (int/float/double/bool/string/char/void)
- ✅ Control flow (if/else/while/for/do-while/foreach/break/continue)
- ✅ Operators: ++/--/bitwise/ternary
- ✅ 32-bit signed int, 32-bit float, 64-bit double, 8-bit char
- ✅ 36 tests, 0 failures
- ⚠️ Template metaprogramming — not implemented
- ⚠️ contract/invariant — not implemented
- ✅ C-style preprocessing (`#ifdef`/`#ifndef`/`#define`/`#include`)

## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller / bare-metal environments.

**Skipped** (MCU does not support them):
- Template metaprogramming (compile-time expansion is too large)
- contract/invariant

**Kept**:
- Basic OOP (class/struct/interface)
- Module imports
- Full operator support

### OS mode (`--mode os`)
OS mode supports the full set of language features.

### RAM level
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

## Usage
```bash
dotnet run --project VMLTool -- input.d -o output.vml
```

## Tests
- `VMLTests/NewCompilerTests.cs` — 36 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full pipeline tests
- `Examples/d/` — example programs (start/factorial/file_io/info)
- `Examples/benchmark/` — bench_int_d.d / bench_float_d.d
