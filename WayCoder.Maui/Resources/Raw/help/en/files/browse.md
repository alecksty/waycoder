# File management

The **Files** page is your workspace (the sandbox). Files the AI edits live here too.

## Importing

The top toolbar can:

- **Import project**: bring a project directory in from your computer
- **New file / New folder**

Or just ask the AI to do it:

> "Create a mygame/ directory in the workspace with a main.c inside it"

## Opening

**Tap a file name** → a menu appears:

| Menu item | What it does |
|---|---|
| Open | Go into the Editor (**read-only** by default, to prevent accidental edits) |
| Compile | Compile to the next artifact level (see below) |
| Run | Compile and run |
| Open with external app | Hand it to the system (view an image, read a PDF, play audio...) |
| Rename / Delete | Exactly what they say |

## The file name colors mean something

| Color | Meaning |
|---|---|
| **Green** | Source that VML can compile (22 languages) |
| **Orange** | `.vml` (text assembly) |
| **Red** | `.vmb` (binary bytecode) |
| No color | Everything else (README, images...) |

The colors only mark **"this one can be compiled / run"** — not "source ought to be green".
A screen full of color makes it harder to see what matters.

## What "Compile" produces

It is decided by the current file; you do not choose:

```
main.c   --->  main.vml       (source -> text assembly)
main.vml --->  main.vmb       (text assembly -> binary bytecode)
main.vmb --->  (already final; it can only be run)
```

If an artifact **with the same name already exists**, you are asked to **Overwrite / Rename / Cancel** —
the artifact may be a file you edited by hand, and silently overwriting it cannot be undone.
