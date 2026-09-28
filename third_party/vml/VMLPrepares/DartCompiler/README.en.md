# Dart Compiler

**Path**: `VMLPrepares/DartCompiler/`
**Completeness**: ~97% | 🟢 Production ready
**Standard library**: `Lib/dart/`

## Features
- ✅ Lexer + parser + code generator (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/else/while/for/do-while/break/continue)
- ✅ Variable declarations (var/final/const/type annotations)
- ✅ Functions: return types/parameters/recursion
- ✅ Class: class definitions/members/methods
- ✅ Operators: ++/--/ternary/bitwise/short-circuit logic
- ✅ Compound assignment (+= -= *= /= %=)
- ✅ import statements
- ✅ 30 tests, 0 failures
- ⚠️ async/await/Future — skipped on MCU
- ⚠️ mixin/extension — not implemented
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include)

## Compilation modes

### MCU mode (default `--mode mcu`)
MCU mode is optimized for microcontroller/bare-metal environments.

**Skipped** (not supported on MCU):
- async/await, Future, Stream
- mixin

**Retained**:
- Basic type arithmetic, control flow, function calls
- print maps to UART

### OS mode (`--mode os`)
OS mode supports the full set of language features.

### RAM levels
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

## Usage
```bash
dotnet run --project VMLTool -- input.dart -o output.vml
```

## Tests
- `VMLTests/NewCompilerTests.cs` — 30 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full-pipeline tests
- `Examples/dart/` — example programs (start/factorial/file_io/info)
