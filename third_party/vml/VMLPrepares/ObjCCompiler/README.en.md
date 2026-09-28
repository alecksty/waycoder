# Objective-C Compiler

**Path**: `VMLPrepares/ObjCCompiler/`
**Completeness**: ~99% | 🟢 Production ready
**Standard library**: `Lib/objc/`
**Depends on**: CCompiler (ObjC is a superset of C)

## Features
- ✅ Lexer + parser + code generator (Lexer/Parser/CodeGenerator)
- ✅ @interface/@implementation/@end class definitions
- ✅ Message expressions [obj msg: param]
- ✅ Full C syntax compatibility (if/else/while/for/do-while/switch/case/break/continue)
- ✅ Pointers Type* / bitwise operators / ternary operator
- ✅ Method declarations (returnType)method:(paramType)param
- ✅ 15 tests, 0 failures
- ✅ @property/@synthesize
- ⚠️ @protocol/category — not implemented
- ⚠️ Block/ARC — skipped on MCU
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include/#import)

## Compilation modes

### MCU mode (default `--mode mcu`)
ObjC assembles to the C-compatible subset retained for the target.

**Skipped**:
- Block, ARC, Protocol

**Retained**:
- @interface/@implementation basic OOP
- Full C syntax
- Message sending (compiled to function calls)

### OS mode (`--mode os`)
OS mode supports the full set of language features.

### RAM levels
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

## Usage
```bash
dotnet run --project VMLTool -- input.m -o output.vml
```

## Tests
- `VMLTests/NewCompilerTests.cs` — 15 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full-pipeline tests
- `Examples/objc/` — example programs (start/factorial/file_io/info)
