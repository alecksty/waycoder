# 22 languages

One VML interface set, usable from all 22 front ends. **Write in whichever one you know** - what
you compile runs on the same virtual machine.

## One page per language

Open any one of them and you get its **language reference** (syntax, types, standard library),
**what this front end supports**, and **the pitfalls we hit on real devices**. The last two are
taken straight from each compiler's `README.md` / `<language>_LANGUAGE_SPEC.md` in the VML source,
so regenerating this page brings them up to date.

| Language | In one line |
|---|---|
| [C](help:vml/lang/c) | «bold»The most complete path«/»: drawing, input, sound, save data and the gamepad are all available. The full games in this repo (Tetris, Gomoku, Pac-Man) are all written in C. |
| [C++](help:vml/lang/cpp) | The same interfaces as C, with classes. Good for organising game logic into objects. |
| [C#](help:vml/lang/csharp) | Reads like desktop C#; the entry point is always a class named `P`. |
| [Objective-C](help:vml/lang/objc) | The syntax is a superset of C, and the UI interfaces are used exactly as in C. |
| [Java](help:vml/lang/java) | Close to desktop Java; external functions are declared `native`. |
| [Kotlin](help:vml/lang/kotlin) | The same approach as Java, with shorter syntax. |
| [Swift](help:vml/lang/swift) | Natural to write, and the plane shooter example is a complete, playable game. |
| [Go](help:vml/lang/go) | Close to desktop Go; the snake example is a complete game. |
| [Rust](help:vml/lang/rust) | Usable, but do not expect the full standard library. |
| [D](help:vml/lang/d) | C-style syntax, a little more relaxed than C to write. |
| [Dart](help:vml/lang/dart) | Close in style; external functions are declared `external`. |
| [Python](help:vml/lang/python) | «bold»The fastest one to write.«/» The syntax is almost desktop Python, and changing a line and running again is a pleasure. |
| [JavaScript](help:vml/lang/javascript) | Close in style, but you have to call the entry point yourself. |
| [Lua](help:vml/lang/lua) | A light, quick little language: pleasant for small games, and it compiles fast. |
| [Ruby](help:vml/lang/ruby) | Usable, but this front end supports the fewest features of all: «bold»write everything flat«/». |
| [R](help:vml/lang/r) | A vector language, close to desktop R in style. |
| [Pascal](help:vml/lang/pascal) | Clearly structured; well suited to writing game state machines. |
| [Fortran](help:vml/lang/fortran) | The scientific-computing style works here too. |
| [BASIC](help:vml/lang/basic) | The old-fashioned style: imperative, one line after another. |
| [Forth](help:vml/lang/forth) | A stack language; writing in it is a completely different way of thinking. |
| [Scheme](help:vml/lang/scheme) | A Lisp dialect where brackets are everything. |
| [Ladder](help:vml/lang/ladder) | A ladder-diagram (PLC) style front end - it «bold»cannot build phone UI programs«/». |

## Run one first

```
vml run examples/c/tetris.c        # Tetris
vml run examples/python/tetris.py  # the same game, Python version
vml run examples/c/gomoku.c        # Gomoku
vml run examples/lua/life.lua      # Game of Life
vml run examples/basic/whack.bas   # Whack-a-mole
```

Every language folder also has a `sysinfo.<ext>` that calls `ui_call_json("sysinfo", "")` and
prints your device information - **if you want to know whether this language is usable, run that
first**.
