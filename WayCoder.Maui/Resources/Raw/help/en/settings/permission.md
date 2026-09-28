# Permissions and work modes

These are two **different** axes; do not mix them up:

## The confirmation axis: when to interrupt you

Switch it on the home page or in the menu:

| Mode | Behavior |
|---|---|
| **Ask** | Asks about every dangerous operation |
| **Auto** | Still asks before changing files |
| **SmartAuto** | Only asks about dangerous operations |
| **Yolo** | Asks nothing; just goes ahead |

> To start with, prefer Ask or SmartAuto; once you are used to it, Yolo (fast, but keep an eye on it).

## The behavior axis: what work it should do

| Mode | What it does |
|---|---|
| **Build** | The full toolset: read, write, and run (the default) |
| **Plan** | Read-only: it gives you a plan first and waits for your approval before acting |
| **Chat** | Pure conversation, with no tools at all |

**Plan mode** suits "let me see how it intends to do this": when it finishes writing the plan an approval
box appears, and only when you tap approve does it switch back to Build mode and continue.

## Sandbox

Paths outside the working directory are out of reach by default. To let it work somewhere else, `cd` there first or import the file into the workspace.

## What permission confirmation looks like

On the phone a box appears listing **what it wants to do this time** (which file to change, which command to run).
If it is changing files, you also get a **per-hunk diff** — read it carefully before allowing.

You can choose "allow once" or "allow all" (the latter stops asking about that category for the current session).
