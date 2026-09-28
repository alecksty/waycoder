# Command-line arguments

Your program's own switches, such as `-l`.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_arg(int i, char* buf, int cap)`
Copies argument i into buf and returns its length (-1 if out of range). `argv[0]` is the program name, so the first argument the user gives is `ui_arg(1,...)`.
```c
char buf[64];
if (ui_arg(1, buf, sizeof(buf)) > 0) { /* a switch such as -l was used */ }
```
### `ui_argc(void)`
The number of arguments (including the program name, always at least 1).
Note: programs with `int main(int argc, char **argv)` do **not** need these (the entry frame hands them over directly) - these two are for `int main(void)` programs and for non-C language bindings.
```c
int n = ui_argc();   /* phone: vml run prog.c -l => n = 2 */
```
