# Sessions and memory

## Sessions save themselves

When a turn ends, or when you leave the page, the session is **written to disk automatically**. Next time you can:

- **Continue the last session** — carry on where you left off
- **New session** — start from scratch

> Only the **conversation text** is saved. Reasoning and tool results are not archived (they are long, and only useful at the time).

## Multiple sessions

**≡** at the top left opens your session history:

- **+ New session** at the top
- Each card shows the **first sentence** and a **relative time**
- Tap one to switch to it (if a turn is running, it stops first)

## Memory (across sessions)

The AI can write "things worth remembering" into long-term memory, and it will carry them into new sessions automatically.
For example, if you tell it "this project uses tabs, not spaces", it will note that down.

This lives in `.waycoder/memory/` in the workspace as **plain markdown you can edit yourself**.

## Slots (desktop)

The desktop version also has ten independent slots, F1–F10, which can run different tasks in parallel.
The phone version is single-session for now.
