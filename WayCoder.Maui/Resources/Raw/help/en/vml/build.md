# Build and run

## The fastest route: the Shell

```
vml run examples/c/tetris.c        # source  -> run it directly
vml run ~/main.vml                 # text assembly -> run it
vml run ~/main.vmb                 # bytecode -> run it (fastest)
```

`vml run` **detects the extension for you**, so you never have to think about which path it takes.

## Tap through the Files page

**Files** page -> tap a file name -> pick from the menu:

- **Compile** -> produces the next artifact down the chain (`.c` -> `.vml`, `.vml` -> `.vmb`)
- **Run** -> runs it directly

The artifact appears in the **same directory**, under the same base name. If a file of that name is
already there, you are asked to overwrite / rename / cancel.

## How long a compile takes on a phone

- **C is the slowest**: a program that pulls in the standard library takes **one to two minutes** to
  compile in the front end and link (on a phone it really is on the order of a minute)
- Scripting languages such as Python / Lua / BASIC are much faster (a few seconds)
- **Loading a `.vmb` is nearly instant** - so if you run the same program again and again, compiling
  to `.vmb` first and then running it saves the most time

> Do not assume it has frozen while compiling: the input cell shows `⋯`, and the `~>` prompt comes
> back when it is done.

## Can a stuck compile lock me out?

No. Front end compilation is synchronous and **has no cancellation entry point at all**, so the host
adds a **watchdog**: if it is still not done after 180 seconds, control comes back to you (that
compile thread itself cannot be aborted - the point is just to stop the UI from hanging).

## Why `.vml` can still stand on its own

- **It can be read**: it is text, so you can see for yourself what instructions the compiler produced
- **It can be edited**: hand-written assembly is just as valid (`vml test` runs a piece of built-in assembly)
- **It works as an intermediate artifact**: compile `.c` to `.vml` once, then assemble to `.vmb` over
  and over without going through the front end again

## Compiler options (Settings → Compile)

Under **Settings → Compile** you can tune a few compiler behavior options. **A change takes effect
on the next compile.**

| Option | Default | Notes |
|---|---|---|
| Optimization level | Off | **Four levels**: Off / Light / Medium / Max (see below) |
| Compiler warnings | Off | Once on, compile-time warnings are shown (Basic = `-Wall`, More = `-Wextra`) |
| Treat warnings as errors | Off | Any warning counts as a failed compile (`-Werror`) |
| Debug output | Off | Turns on the diagnostic branches inside the front end, plus the warning count at the end of a compile |
| Floating point (float/double) | Hardware | Set it to Off = floating-point code **fails to compile outright** |
| 64-bit integers (long/int64) | Hardware | Same as above |

### The four optimization levels

| Level | What it does | Measured (a 46-byte hello.c) |
|---|---|---|
| **Off** | Does not delete a single instruction | 69637 instructions / 2.0 MB |
| **Light** | Clears out filler code that is unreachable after a jump | 69637 / 2.0 MB (basically unchanged) |
| **Medium** | Adds **dead code elimination**: removes library functions nobody calls | **28 / 3.1 KB** |
| **Max** | Also removes redundant jumps, clears the data segment, drops `.linked` declarations | **27 / 670 bytes** |

> **This is what "turning optimization on really makes it smaller" means**: a hello world drops from
> **2.0 MB to 670 bytes**, because it linked the whole standard library to begin with, and 99% of it
> was functions that nothing ever calls.
>
> **Games are not this dramatic** (they already use a lot of the library): Tetris 74754 -> 6282
> instructions, Gomoku 72921 -> 3544, Gorillas (BASIC) 103437 -> 29863.
>
> **Optimization does not change what a program does** - the output of the examples in all 22
> languages is **byte-for-byte identical** across the four levels (there is a test watching this).
> Its rule is "only delete what is certain to be unused": for indirect jumps, function addresses
> taken, interrupt vector tables and other cases that cannot be seen fully at compile time, it
> **prefers to keep rather than delete**.

> **Unused things get dropped**: global variables, constants, strings and whole library data
> segments (the sine table, the palette and so on) that nothing references never make it into the
> output. This is not specific to one level - data segment cleanup is always in effect; from Medium
> up, dropping library functions removes even more of it along the way.

> **"Off" reports an error, it does not degrade gracefully** - it is there to confirm whether a
> program uses floating point or 64-bit integers at all, not to let a program quietly get slower.
> Soft floating point has no corresponding library on this platform, so that level is not offered.

> These options **do not affect the default behavior**: leave them alone (optimization = Off) and
> what you compile is **byte-for-byte identical** to before.
