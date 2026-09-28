# Fortran compiler

**Path**: `VMLPrepares/FortranCompiler/`
**Completeness**: ~98% | 🟢 production ready
**Standard library**: `Lib/fortran/`

## Features
- ✅ Lexing + parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ program/subroutine/function structure
- ✅ contains internal procedures
- ✅ function name(args) result(r) syntax
- ✅ Control flow (if/else/do/do while/exit/cycle)
- ✅ Data types (integer/real/double precision/logical/character)
- ✅ Comparison operators (.eq./.ne./.lt./.gt./.le./.ge.)
- ✅ Logical operators (.and./.or./.eqv./.neqv.)
- ✅ print */read */write */call/return/stop
- ✅ ! comments
- ✅ 24 tests, 0 failures
- ✅ Array sections / whole-array operations (A(:)=B(:)+C(:)) — MCU loop unrolling
- ✅ module/use — module system
- ✅ else if chains — recursive parsing support
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include)

## Compilation modes

### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments.

**Skipped** (not supported by the MCU):
- None
- Dynamic array allocation

**Kept**:
- Basic numeric computation
- Subroutine/function calls
- print/write mapped to the UART

### OS mode (`--mode os`)
OS mode supports every language feature.

### RAM levels
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

## Usage
```bash
dotnet run --project VMLTool -- input.f90 -o output.vml
```

## Tests
- `VMLTests/NewCompilerTests.cs` — 24 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full pipeline tests
- `Examples/fortran/` — example programs (start/factorial/file_io/info)
