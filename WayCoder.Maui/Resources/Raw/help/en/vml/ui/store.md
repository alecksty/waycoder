# Local save data

For small pieces of data such as the high score.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_store_get(char* key, char* buf, int cap)`
Reads a value: it is **written into the buffer you supply** and the length is returned (-1 if there is no such key).
Note: it does **not** "return that integer" - you get a string back, so convert it yourself if you need a number.
```c
char buf[16];
if (ui_store_get("high", buf, 16) > 0) best = atoi(buf);
```
### `ui_store_set(char* key, char* value)`
Stores a value. **The value is a string too** - to store a number, convert it to a string yourself first (there is no sprintf available here).
Keys get a prefix automatically, so they will not collide with the app's own settings.
```c
ui_store_set("high", "1200");
```
### `ui_store_del(char* key)`
Deletes one saved entry (for example "clear the high score"). Returns 0 on success / -1 on failure (invalid key).
```c
ui_store_del("high");
```
