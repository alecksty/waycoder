# Ruby Compiler

**Path**: `VMLPrepares/RubyCompiler/`
**Completeness**: ~96% | 🟢 Production ready
**Standard library**: `Lib/ruby/`

## Features
- ✅ Lexer + parser + code generator (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/elsif/else/unless/while/until/for/in)
- ✅ Functions: def/return/parameter passing
- ✅ Class: class definitions/methods/instance variables
- ✅ Operators: assignment/comparison/arithmetic/unary/<=>/and/or/not
- ✅ Literals: integer/float/string/boolean/array/Symbol/`**` power/`..` range
- ✅ Compound assignment (+= -= *= /= %=)
- ✅ case/when/else branching
- ✅ 34 tests, 0 failures
- ✅ module/mixin — include is inlined
- ⚠️ yield/block/Proc — not implemented
- ✅ require library import
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include)

## Compilation modes

### MCU mode (default `--mode mcu`)
MCU mode is optimized for microcontroller/bare-metal environments and automatically skips features that are incompatible with an operating system.

**Skipped** (not supported on MCU):
- require dynamic loading, yield/block

**Retained** (implemented by the BIOS):
- Basic type arithmetic, control flow, function calls
- print/puts map to UART

### OS mode (`--mode os`)
OS mode supports the full set of language features (file system, threads, etc.).

### RAM levels
- `--ram k`: kilobyte level (2 KB~64 KB)
- `--ram m`: megabyte level (64 KB~1 MB, default)
- `--ram g`: gigabyte level

## Usage
```bash
dotnet run --project VMLTool -- input.rb -o output.vml
```

## Tests
- `VMLTests/NewCompilerTests.cs` — 34 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full-pipeline tests
- `Examples/ruby/` — example programs (start/factorial/file_io/info)
