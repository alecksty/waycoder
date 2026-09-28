# Ladder Diagram (IEC 61131-3 subset) compiler

**Path**: `VMLPrepares/LadderCompiler/`
**Completeness**: ~98% | 🟢 production ready
**Standard library**: `Lib/ladder/`

## Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Contacts: NO (normally open) / NC (normally closed) / POS (positive edge) / NEG (negative edge)
- ✅ Coils: OUT (output) / SET (set) / RST (reset)
- ✅ Timers: TON (on-delay) / TOF (off-delay) / TP (pulse) — SYSCALL #53 real-time clock
- ✅ Counters: CTU (up) / CTD (down) / CTUD (up/down) — with overflow protection
- ✅ Comparison: EQ/NE/GT/GE/LT/LE
- ✅ Arithmetic: ADD/SUB/MUL/DIV/MOD
- ✅ POKE/PEEK memory operations (MMIO)
- ✅ Shared builtin function library (builtins.vml)


## Compile modes

### MCU mode (default, `--mode mcu`)
MCU mode is tuned for microcontroller / bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that are incompatible with an operating system.

**Skipped** (no code is generated for these constructs):
- none (ladder languages are MCU compatible by nature)

**Kept** (the low level is provided by the BIOS):
- Timers (TON/TOF/TP) — using SYSCALL #53 GetTick
- Counters (CTU/CTD/CTUD) — 16-bit overflow protection
- POKE/PEEK memory-mapped I/O (MMIO)
- I/O memory mapping (contacts/coils)
- Basic arithmetic / comparison / control flow

### OS mode (`--mode os`, reserved)
OS mode targets environments with an operating system (embedded Linux, an RTOS and so on); it will support the full set of language features (file system, threads, async, exceptions, reflection and so on).

### RAM level
- `--ram k`: kilobyte level (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte level (64KB~1MB, such as ARM Cortex-M, **default**)
- `--ram g`: gigabyte level (such as x86/DDR systems)
- `--stack-size <bytes>`: specify the stack size manually (by default it is derived automatically from `--ram`)

### Safe MCU coding notes
- A PLC scan cycle has to meet real-time requirements
- Timer accuracy depends on the resolution of the SYSCALL #53 clock
- Watch the 16-bit range of counters (-32768 ~ 32767)
- Floating-point arithmetic may need a soft-float library

## Usage
```bash
dotnet run --project VMLTool -- input.lad -o output.vml
```

## Tests
`Test/La/` — 0 test files (the test directory is yet to be created)
