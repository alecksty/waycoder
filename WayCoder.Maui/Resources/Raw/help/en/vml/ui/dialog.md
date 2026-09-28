# Dialogs

Messages, single choice, multiple choice, text input.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_dlg_input(char* title, char* prompt, char* buf, int cap)`
Asks for one line of text. The result is written into the buffer you supply.
```c
char buf[64];
ui_dlg_input("Rename", "New name:", buf, 64);
```
### `ui_dlg_msg(char* title, char* body, int style)`
Shows a message box (pass 0 for `style`). It **blocks until the user dismisses it** - perfect for reporting the result when a game ends.
```c
ui_dlg_msg("Game over", "Score 120", 0);
```
### `ui_dlg_multi(char* title, char* body, char* opts, int n)`
A multiple-choice dialog: `opts` is the option string, `n` the number of options. Returns how many were selected.
```c
int n = ui_dlg_multi("Settings", "Which ones?", opts, 3);
```
### `ui_dlg_select(char* title, char* body, char* opts, int n, int def)`
A single-choice dialog: `opts` is the **option string**, `n` the number of options and `def` the option selected by default. Returns the selected index (-1 = cancelled).
```c
int r = ui_dlg_select("Game over", "Play again?", opts, 2, 0);
```
