# Providers and models

## Entering an API key

**Settings** → pick a provider → paste the key.

A set of mainstream providers is built in (DeepSeek / OpenAI / Claude / Gemini / Qwen / Kimi...),
and you can also enter a custom address (any OpenAI-compatible gateway works).

## Choosing a model

**Settings → Model** opens model management:

- Grouped **by provider**; each row shows the context length and price (free ones are green)
- The top right switches between "set as main model / set as small model"
- Selected ones show `Main✓` / `Small✓`

You can also switch quickly from the Chat page: **☰ menu → model picker**.

## Main model and small model

- **Main model**: does the real work (writing code, analysis, decisions)
- **Small model**: does the chores (context compaction, summarization)

**Selecting both saves the most money**: chores go to the cheap small model, and only real work uses the expensive one.
If you do not pick a small model, the main model does the chores itself.

## Model fallback chain (desktop)

You can configure a list of models to try in order when the main one fails. Not available on the phone yet.

## Other providers work too

As long as it is compatible with OpenAI's `/v1/chat/completions`, enter the address and key and you are set.
**Model management → Import** can also **bulk-import** from the configs of tools like Claude Code / Codex / OpenCode.
