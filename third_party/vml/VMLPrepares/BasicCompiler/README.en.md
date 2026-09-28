# BASIC compiler

**Path**: `VMLPrepares/BasicCompiler/` | **Version**: v1.66.33
**Completeness**: 10 dialects, ≥90% each | 🟢 production ready (graphics/sound/files already work on phones)
⚠ **The test numbers (78 / Lang_BASIC, `VMLTests/`, `test/basic_examples/`) belong to the upstream repository** —
  this repository (the mobile copy after the fork) does **not** have the `VMLTests/` or `test/` directories, so those paths and numbers cannot be reproduced here.
  The check for this repository is `scripts/vml-basic-probe/` (54 cases + 5 `fileio.sh` cases).
**Standard library**: `Lib/basic/`

## Multiple dialect support

The `--basictype` option selects one of 10 BASIC dialects:

| Dialect | CLI value | Family | Completeness |
|------|--------|------|:--:|
| QBasic (default) | `qbasic` | Microsoft | 92% |
| GW-BASIC | `gwbasic` | Microsoft | 90% |
| VisualBasic 6 | `visualbasic` | Microsoft | 90% |
| TurboBasic | `turbobasic` | Borland | 90% |
| PowerBASIC | `powerbasic` | Borland | 90% |
| FreeBasic | `freebasic` | open source | 90% |
| PureBasic | `purebasic` | commercial | 90% |
| TrueBasic | `truebasic` | ANSI/ISO | 90% |
| ChipBasic | `chipbasic` | MCU | 90% |
| MiniBasic | `minibasic` | educational | 90% |

```bash
vmltool -x basic --basictype freebasic test.bas -o test.vml
vmltool -x basic --basictype gwbasic test.bas -o test.vml
```

## Keyword implementation

⚠ **These numbers did not match the code for a long time** (this README said 81/99, the same file further down said 116+, and the SPEC said 116/141).
Recalculated from measurements in v0.96.506: **140 keywords in the lexer table, 171 `TokenType`s**;
the 99-item table in `ALL_KEYWORDS.md` measures **89 implemented / 10 unimplemented**.
**`ALL_KEYWORDS.md` is the authority** — it marks the measured status of every entry.

### Fully implemented
- ✅ Control flow: IF/THEN/ELSE/ELSEIF/SELECT CASE/FOR/NEXT/WHILE/WEND/DO/LOOP/GOTO/GOSUB
- ✅ Subroutines: SUB/END SUB/FUNCTION/CALL/DECLARE/BYVAL/BYREF/PROCEDURE
- ✅ Variables: DIM/LET/CONST/SWAP/ERASE/REDIM/SHARED/COMMON/STATIC/LOCAL/GLOBAL
- ✅ I/O: PRINT/INPUT/OPEN/CLOSE/WRITE/LPRINT/PRINT USING/FREEFILE/LINE INPUT #
  (⚠ the file family only truly worked from v0.96.503/504: before that the OPEN handle was never stored at all,
    `PRINT #`/`INPUT #` used a **device** number rather than a **file** number, and `FREEFILE` was only a lexer entry.
    All of them now go through the sandbox-aware `#110-113`; the check is `scripts/vml-basic-probe/fileio.sh`)
- ⚠️ OOP (FreeBasic): CLASS/METHOD/ENUM ✅; **CONSTRUCTOR only goes as far as parsing, DESTRUCTOR has no code**
- ✅ Maths: ABS/SGN/SQR/SIN/COS/TAN/INT/FIX/RND/RANDOMIZE
- ✅ Strings: LEN/CHR$/ASC/STR$/VAL/LEFT$/RIGHT$/MID$
- ⚠️ MCU: CHIPASM/ASM/BLOAD/BSAVE — **`ASM` has been removed from the lexer table** (the comment says "C/ObjC/C++ only");
  `BLOAD`/`BSAVE` do generate GPIO calls but are **missing a runtime library**, and phones have no GPIO ⇒ unusable in practice
- ✅ Other: REM/DATA/READ/RESTORE/ON ERROR/RESUME/SLEEP/SYSTEM/BEEP/SOUND

### Partially implemented
- ⚠️ PROPERTY, CONSTRUCTOR (the framework is ready, still to be finished)

### Not implemented (needs a type system)
- ❌ PTR, CAST, EXTENDS, OPERATOR, INTERFACE

## Dialect-specific features

| Feature | Dialects | Description |
|------|------|------|
| CLASS/OOP | FreeBasic | class/constructor/destructor/method/enum |
| GPIO | ChipBasic | PINMODE/DIGITALWRITE/DIGITALREAD |
| GLOBAL | PureBasic | maps to SHARED |
| PROCEDURE | PureBasic | maps to SUB |
| ENUM value assignment | FreeBasic | compile-time constants (RED+GREEN=30) |
| NEW | PureBasic | maps to EmitAlloc heap allocation |
| Private/Public | VisualBasic | modifier recognition |

## File structure

```
BasicCompiler/
├── ASTNode.cs                 # AST nodes (measured: 171 TokenTypes)
├── Lexer.cs                   # lexing (measured: 140 keywords)
├── Parser.cs / Parser.*.cs    # parsing (10 dialects supported)
├── CodeGenerator.cs / *.cs    # code generation (TypedCodeGen<BasicType>)
├── BasicCompilerPlugin.cs     # plugin entry point + compilation pipeline
├── TokenType.cs               # the Token type enum
├── BASIC_LANGUAGE_SPEC.md     # language specification v2.1
├── ALL_KEYWORDS.md            # 99 keywords x 10 dialects
├── UNIMPLEMENTED_FEATURES.md  # list of unimplemented features
└── README.md                  # this file
```

## Usage

```bash
# QBasic (default)
vmltool input.bas -o output.vml

# pick a dialect
vmltool -x basic --basictype freebasic input.bas -o output.vml
vmltool -x basic --basictype visualbasic input.bas -o output.vml

# compile + run
vmltool input.bas -o output.vml && vmltool -r output.vml
```

## Tests

- **78 unit tests** (`VMLTests/Lang_BASIC.cs`): dialect keywords + TYPE/CLASS + ENUM
- **22 dialect tests**: dialect-specific keywords for 7 dialects + cross-dialect compatibility
- **14 FreeBasic examples** (`test/basic_examples/freebasic/`): class/constructor/method/pointers and more
- **QBasic games**: gorillas.bas, nibbles.bas

```bash
# run all BASIC tests
dotnet test VMLTests/VMLTests.csproj --filter "ClassName~Lang_BASIC"

# run the modern-feature tests
dotnet test VMLTests/VMLTests.csproj --filter "ClassName~Lang_ModernFeatures"
```
