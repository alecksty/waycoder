# The Shell page

On a phone this is a **real shell** (Android uses `/system/bin/sh`), so almost any command you would type on a computer works here.

## Basics

- Type a command in the input box → tap **Run** (or press Return)
- **Clear** at the top left wipes the output history
- `cd` **changes the working directory below** (the prompt, `~/examples>`, follows along)
- Paths accept the `~/` shorthand (the workspace root is `~`)

## What you can run

- Ordinary commands: `ls`, `cat`, `cp`, `mv`, `grep`, `find`...
- **The `vml` command**: compile and run VML programs (see **The vml command**)
- Your own scripts

## Running long commands

When a program runs for a long time:

- The input box shows `⋯` (meaning it is still busy)
- Tap **■** to interrupt it (the child process is terminated as well)
- The prompt `~>` reappearing means this round has finished (it is not frozen)

## How this differs from Chat

- **Chat**: you let the AI do the work — it calls tools, reads files, and edits code by itself
- **Shell**: you do it yourself; the AI is not involved

Both look at **the same workspace**, so files are shared between them.
