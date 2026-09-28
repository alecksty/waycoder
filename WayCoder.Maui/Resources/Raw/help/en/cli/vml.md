# The vml command

`vml` is the built-in VML program tool. Type it on the **Shell page**.

## Subcommands

```
vml              show usage
vml test         run the built-in self-test (a few seconds; verifies the environment)
vml run <file>   compile and run
vml make <.vmk>  build from a project file; produces artifacts only, does not run
```

## `vml run` **detects the file type** for you

| The file you give it | What it does |
|---|---|
| `xxx.c` `xxx.py` `xxx.lua` ... (source in any of 22 languages) | **Compiles** it with the matching frontend compiler, links the standard library, and runs it |
| `xxx.vml` | This is **text assembly**; it is assembled and run directly |
| `xxx.vmb` | This is **binary bytecode**; it is loaded and run directly (fastest — no compilation or assembly) |

So each of the three artifact levels has its own use:

```
source .c  --compile-->  .vml  --assemble-->  .vmb
           (vml run)           (Compile on the Files page)
```

**For the same program, `.vmb` starts fastest** — especially noticeable on a phone.

## `vml make`: multi-file programs need a project file

`vml run` compiles **one** source file at a time. When there is more than one (`main.c` + `util.c` + ...),
write a `.vmk` project file that spells out the relationships, then run `vml make`:

```xml
<VMLProject Version="1">
  <Entry>src/main.c</Entry>         <!-- the one containing main -->
  <Sources>
    <File>src/util.c</File>         <!-- the other compilation units -->
  </Sources>
  <Includes><Dir>src</Dir></Includes>   <!-- equivalent to -I -->
  <Defines><Define Name="VERSION" Value="1.2"/></Defines>  <!-- equivalent to -D -->
</VMLProject>
```

```
vml make proj.vmk        # the artifact lands next to the entry by default (src/main.vml); then vml run it
```

Three things to remember:

* **It only produces artifacts; it does not run them** — run the artifact with `vml run` (keeping build and run separate is what makes batch and CI use practical);
* `<Output Format="...">` recognizes names like `bin`/`elf`/`hex`, but **only `vml` and `vmb` can be produced today**;
  asking for anything else is a **clear error**, rather than a `.vml` quietly handed back to you;
* **Elements the project file does not recognize are errors** — a typo is never silently ignored.

If you already have a Makefile, you can convert it once: `vmlcli make --import Makefile` (a **desktop** command),
and treat the `.vmk` as the source of truth from then on. The full format is in the repository's `docs/VML工程文件.md`.

## Examples

```
vml test
vml run examples/c/gomoku.c
vml run examples/python/tetris.py
vml run ~/main.vmb
vml make ~/proj.vmk
```

## If it will not run, or reports an error

1. Run `vml test` first — if it passes, the environment is fine and the problem is in your program
2. Look for a **line number** in the error and go fix that line
3. See **VML compiler → Common errors**
