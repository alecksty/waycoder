# All-purpose calls

Two strings in, one JSON string out.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_call_json(char* fn, char* args_json, char* out_buf, int cap)`
**Two strings in, one JSON string out** - this is the route for querying device information and reaching the host's odds and ends, so no syscall number has to be spent on every small feature. The result is written into the buffer you supply.
Note: **do not use this for performance-sensitive calls** (it passes through JSON twice plus one memory copy); drawing and input still go through their dedicated calls.
```c
char buf[512];
ui_call_json("sysinfo", "", buf, 512);
puts(buf);   /* {"ok":true,"result":{...}} */
```
### `ui_call_json_at(int i)`
Returns byte i of the last result.
```c
char c = (char)ui_call_json_at(i);
```
### `ui_call_json_len(void)`
How long the result of the last `ui_call_json_s` was.
```c
int n = ui_call_json_len();
```
### `ui_call_json_print(void)`
Prints the **whole** result of the last `ui_call_json_s` to stdout (with a trailing newline) - the easiest way to debug, and what the per-language self-tests use to print the result verbatim. Note: it takes no arguments - it prints the previous result rather than making another call with arguments.
```c
ui_call_json_s("version", "");
ui_call_json_print();
```
### `ui_call_json_s(char* fn, char* args_json)`
The same, but **without a buffer** (for languages that cannot get a pointer); pair it with `_len` / `_at` to read the result.
```c
int n = ui_call_json_s("version", "");
for (int i = 0; i < n; i++) putchar(ui_call_json_at(i));
```
