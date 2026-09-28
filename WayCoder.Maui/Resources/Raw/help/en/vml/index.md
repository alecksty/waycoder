# What VML is

**VML = a virtual machine that runs inside your phone**, with its own standard library, its own
assembler and its own bytecode format.

A program you write (C / Python / BASIC / ... 22 languages in all) is compiled by a **front end
compiler** into the instructions this virtual machine understands, and then that is what runs it. So:

- **The same language runs here with nothing installed** - no gcc, no interpreter
- **Write it once, compile it in all 22 languages** (one and the same compile chain)
- Fast and small (a hello world is tens of thousands of instructions, a few hundred KB of bytecode)

## Why not just call the system compiler

You cannot install, or even run, a full gcc / CPython on a phone. VML moves that work to **compile
time** - compilation happens in the front end, and at run time all that is needed is a very small
virtual machine.

## Three levels of artifact

```
your source (.c)  ──front end──▶  .vml (text assembly)  ──assemble──▶  .vmb (binary bytecode)
                                                                    │
                                                              load and run
```

| Artifact | What it is | When to use it |
|---|---|---|
| Source | What you wrote | You edit this day to day |
| `.vml` | Text assembly, readable and greppable | When you want to see what the compiler made of your code |
| `.vmb` | Binary bytecode | **Load this one to run - it is the fastest** |

`vml run` understands all three and tells them apart on its own.

## It can draw UI, make sound, and vibrate

A VML program is not limited to printing text - it has a complete set of **phone UI interfaces**:

- Open a drawing window and draw rectangles / circles / paths / gradients / text
- Receive touch (**multi-touch**), key and timer messages
- Synthesize sound effects (no audio files needed) - **single tones** with `ui_beep`, **chords /
  melodies** with `ui_tone_on/off` (polyphonic, up to 32 voices at once), plus vibration and keeping
  the screen awake
- Local save data

Tetris, Gomoku, Pac-Man, Snake, a plane shooter and a **piano** have already been written against
these interfaces. They are all in `examples/`, and they all run as they are.

See "UI development" for the details.

## 22 languages, pick any one

The same set of VML interfaces is available from all 22 front ends. **Write in whichever one you
know**, and what you compile runs on the same virtual machine. Each language has its own detailed
page (its language spec plus what this front end supports) under "About -> Help -> VML compilers ->
22 languages".

## 22 languages, one example each

| Language | Examples | How to run |
|---|---|---|
| C | `examples/c/tetris.c` `gomoku.c` `pacman.c` `mario.c` `starfall.c` **`piano.c`** **`audio_test.c`** | `vml run examples/c/gomoku.c` |
| C++ | `examples/cpp/snake.cpp` | `vml run examples/cpp/snake.cpp` |
| C# | `examples/csharp/snake.cs` `racer.cs` | `vml run examples/csharp/snake.cs` |
| Objective-C | `examples/objc/snake.m` | `vml run examples/objc/snake.m` |
| Java | `examples/java/catch.java` | `vml run examples/java/catch.java` |
| Kotlin | `examples/kotlin/catch.kt` | `vml run examples/kotlin/catch.kt` |
| Swift | `examples/swift/plane.swift` `snake.swift` | `vml run examples/swift/snake.swift` |
| Go | `examples/go/snake.go` | `vml run examples/go/snake.go` |
| Rust | `examples/rust/breakout.rs` | `vml run examples/rust/breakout.rs` |
| D | `examples/d/catch.d` | `vml run examples/d/catch.d` |
| Dart | `examples/dart/catch.dart` | `vml run examples/dart/catch.dart` |
| Python | `examples/python/tetris.py` | `vml run examples/python/tetris.py` |
| JavaScript | `examples/javascript/catch.js` | `vml run examples/javascript/catch.js` |
| Lua | `examples/lua/life.lua` | `vml run examples/lua/life.lua` |
| Ruby | `examples/ruby/catch.rb` | `vml run examples/ruby/catch.rb` |
| R | `examples/r/catch.r` | `vml run examples/r/catch.r` |
| Pascal | `examples/pascal/catch.pas` | `vml run examples/pascal/catch.pas` |
| Fortran | `examples/fortran/sokoban.f90` | `vml run examples/fortran/sokoban.f90` |
| BASIC | `examples/basic/whack.bas` `tetris.bas` | `vml run examples/basic/whack.bas` |
| Forth | `examples/forth/parserexp_demo.fs` | `vml run examples/forth/parserexp_demo.fs` |
| Scheme | `examples/scheme/catch.scm` | `vml run examples/scheme/catch.scm` |
| Ladder | `examples/ladder/file_io.ld` | Compile only, see below |

Every language directory also has a `sysinfo.<ext>` that calls `ui_call_json("sysinfo", "")` and
prints the device information - **if you want to know whether this language works, run that first**.

## How to write in each language

### C / C++ / Objective-C - the most complete path

```c
#include <waycoder_ui.h>

int main(void) {
    int w = ui_scr_w(), h = ui_scr_h();
    ui_win_open("Demo", w, h);
    ui_clear(0xFF101020);
    ui_text(20, 40, "Hello", 0xFFFFFFFF, 20, 0);
    ui_present();
    while (ui_win_closed() == 0) { int m[4]; ui_wait(m, 0); }
    return 0;
}
```

- **The slowest to compile** (front end plus linking the standard library: a minute or two on a phone)
- Warning: Objective-C **must not `#include <waycoder_ui.h>`** (the front end cannot parse that
  header), just call the `ui_*` functions directly (see `examples/objc/snake.m`)

### C# / Java / Kotlin / Dart - you have to declare them

These front ends require external functions to be declared first:

```csharp
// C#
class P { static void Main() { ui_win_open("Demo", ui_scr_w(), ui_scr_h()); } }
```
```java
// Java
static native int ui_win_open(String t, int w, int h);
```
```dart
// Dart
external int ui_win_open(String t, int w, int h);
```

### Python / Lua / JavaScript / Ruby / R - call them directly

```python
ui_win_open("Demo", ui_scr_w(), ui_scr_h())
ui_clear(0xFF101020)
ui_present()
```

Warning: **Python lists cannot be written to** (`b[i] = v` still reads back as 0). For a mutable grid
such as a game board, use the integer grid the shared library provides:

```python
ui_gclear()
ui_gset(3, 1)      # cell 3
v = ui_gget(3)
```

### BASIC

```basic
NATIVE FUNCTION ui_win_open(t AS STRING, w AS INTEGER, h AS INTEGER) AS INTEGER
ui_win_open "Whack-a-mole", w, h
```

Warning: a parameter must not be named `on` (it is a keyword).

### Pascal

```pascal
{ comments are { }, not // }
program p;
begin
  ui_win_open('Demo', ui_scr_w(), ui_scr_h());
  ui_present();
end.
```

Warning: **comments may only contain ASCII** - Chinese punctuation (dashes, commas) is reported as an
unknown character. And if you use `//` for a comment, the whole line is treated as code.

### Forth

```forth
S" Demo" ui_scr_w ui_scr_h ui_win_open
```

### Scheme

```scheme
(ui_win_open "Demo" (ui_scr_w) (ui_scr_h))
```

Warning: **a user function cannot see top-level variables** (the two frame pointers collide). Write
games as a **flat top-level program**, and pull out only pure functions that take every parameter
explicitly and never touch globals.

### Fortran

```fortran
call ui_win_open('Demo', ui_scr_w(), ui_scr_h())
```

### Ladder - special case

It is a **PLC ladder-logic style** front end, and it has no syntax for "a function call with string
arguments", so **the whole UI set is out of reach** (`ladder/file_io.ld` in the repository is itself
an empty test). It compiles and it runs, but it cannot build a phone UI program.

## Pitfalls common to all languages

1. **Ask first, then open**: `ui_scr_w/h` -> `ui_win_open`
2. **Call `ui_present()` every frame**
3. **Call `ui_msg_clear()` before starting a new round**
4. **Timers must be killed**
5. **Do not run an array out of bounds** - nine times out of ten, "memory error" means exactly that
