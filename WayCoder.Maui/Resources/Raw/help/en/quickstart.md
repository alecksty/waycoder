# Get your first program running in five steps

## 1. Add an API key

**Settings** at the bottom → pick a provider (DeepSeek / OpenAI / Claude / Gemini...) → paste your API key.
You can run programs from the **Shell** page without a key, but chat will not work.

> The key stays on your phone (`api_keys.json`) and is never uploaded anywhere.

## 2. Pick a model

**Settings → Model**: each row shows "provider + model name".
To save money, select a **small model** as well — context compaction and summarization get handed to it.

## 3. Say what you want, in one sentence

Type straight into the **Chat** page, for example:

- "Write a hello.c in the workspace that prints a multiplication table"
- "Slow down the falling blocks in examples/c/tetris.c"
- "Show me how examples/python/tetris.py handles key presses"

When each turn ends, **time / tokens / cost** appear under the bubble.

## 4. Run it

**Files** page → tap a file → choose **Run**.
Compiling C on a phone takes **a minute or two** — do not assume it has frozen.

Or type this into the **Shell** page:

```
vml run examples/c/tetris.c
```

## 5. Edit code

On the **Files** page, tap a file → tap **✎** in the top right to leave read-only mode → edit → **Save**.
Then tap ▶ in the toolbar to compile and run in place; errors are **drawn on the offending cell**.

---

## Where to go next

| I want to | Read |
|---|---|
| Write my own VML program | VML compiler → UI development |
| Write in another language | VML compiler → 22 languages |
| Learn the editor | Editor |
| The shell on my phone | Shell |
