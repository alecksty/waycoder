# Common errors

## "File not found"

The path is wrong. Two things to watch:

- The workspace root is `~`, e.g. `~/examples/c/tetris.c`
- After `cd` on the Shell page, a relative path is relative to the **current directory**

## "Label not found: xxx"

The linker cannot find that function. Common causes:

1. **The function is not in the standard library** - see the interface list under "UI development"
2. **A typo in the function name**
3. **The function is declared after it is called** (missing forward declaration)

## "Memory error (PC=...)" plus a pile of registers

The program ran off the rails: it touched memory it should not have touched. Check in this order:

1. **Array out of bounds** (the most common by far) - check the index range
2. **A pointer or address computed wrong**
3. **The wrong number of arguments to a function** (too many or too few both misalign the stack)

In the report, `PC=00000047` is the address of the instruction that faulted; the line below it,
`MOVE R0, @1 ...`, is **the instruction that faulted**, and something like `address=FFFFFFFC` is the
address it touched.

## "Compilation timed out"

The program is too large, or the compiler got stuck in its own code. See the watchdog note in
"Build and run".

## The program runs but the window is black

- `ui_present()` is missing - every frame has to be committed once
- Or the program exited **right after opening the window, before drawing its first frame**: draw the
  first frame immediately after `ui_win_open`
- Check whether the color passed to `ui_clear()` is fully transparent (an alpha of 0, as in `0x00......`)

## Touch does nothing

- Coordinates are **canvas coordinates**, not screen coordinates (the canvas is centered with its
  aspect ratio kept, so there can be black bars)
- A tap that lands on a black bar is dropped (that is intentional)
- Check whether `ui_msg_clear()` was called to drop what was left over from the previous round

## Still not right

1. Run `vml test` first to confirm the environment is healthy
2. Shrink the program to the smallest case (keep only the lines that misbehave) and run it again
3. Ask the AI: "look at examples/c/xxx.c and tell me why it reports a memory error"
