# Scheme (R5RS subset) compiler

**Path**: `VMLPrepares/SchemeCompiler/`
**Completeness**: ~93% | 🟢 production ready
**Standard library**: `Lib/scheme/` (to be created)

## Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGen, ~1800 lines)
- ✅ S-expression parsing
- ✅ define / lambda / function definition and calls
- ✅ if / cond / and / or conditionals
- ✅ Basic types: integers / floats / booleans / strings / symbols
- ✅ Arithmetic: + - * / quotient remainder
- ✅ Comparison: = < > <= >=
- ✅ Lists: cons/car/cdr/list/null?/pair?
- ✅ Higher-order functions: apply/map
- ✅ GC marking (interface reserved)
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)

## Missing features
- ✅ Tail-call optimization (TCO)
- ❌ call/cc (call-with-current-continuation)
- ❌ Macro system (define-syntax/syntax-rules)
- ❌ Vectors (vector)
- ❌ Full character/string operations
- ❌ Full numeric tower (complex/rational)
- ❌ Standard library (Lib/scheme/)

## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller and bare-metal environments (Arduino/STM32/8051 and the like) and automatically skips features that need an operating system.

**Skipped** (no code is generated when these appear):
- call/cc (needs stack capture), dynamic eval

**Kept** (the low-level parts are provided by the BIOS):
- cons (heap allocation), GC marking
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- display/write mapped to SYSCALL

### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, RTOS and so on). It will then support the full set of language features (file system, threads, async, exceptions, reflection and so on).

## Usage
```bash
dotnet run --project VMLTool -- input.scm -o output.vml
```

## Tests
Tests are embedded in the compiler source as verification
