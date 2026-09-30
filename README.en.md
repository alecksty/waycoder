<div align="center">

![alt text](image/app.png)

# Dolaima (都来码)

**Chinese-language coding agent, Vibe Coding Agent CLI**

[简体中文](README.md) | **English**

*Multi-model + 49 tools + Watch mode + single file + multi-agent*

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT-native-blue)](https://learn.microsoft.com/dotnet/core/deploying/native-aot)

</div>

> 📖 **User Manual**: [docs/使用手册.md](docs/使用手册.md) — quick start, command reference, keybindings, configuration, Watch mode, FAQ
> ⬆️ **Install & Upgrade**: [docs/安装与升级.md](docs/安装与升级.md) — direct download / winget / brew / apt + built-in auto-update
> 🔌 **Plugin System**: [docs/插件系统.md](docs/插件系统.md) — compile-time C# plugins that contribute tools and slash commands
> 📚 **All Docs**: [docs/README.md](docs/README.md) — the full categorized index (user-facing / VML platform / architecture / planning / release & store submission / handover)

## Rename Notes

This project started out as **CoreCoder** and, because it clashed with an existing trademark / product name, **has been renamed twice**:

| When | Old name | New name |
|---|---|---|
| v0.16.3 | CoreCoder | **WayCoder (道码)** |
| **v0.96.569** | WayCoder / 道码 | **Dolaima (都来码)** (avoiding a trademark clash with Korea's Waycoder co., ltd. before the global release) |

⚠ **Only the "brand name users see" changed** — every technical name in the code is untouched (touching them would ripple through paths and configurations across the whole repository):

- Code namespaces, repository directories and project names are still `WayCoder` / `WayCoder.Maui`
- **The executable is still `waycoder.exe`** (the command examples still say `waycoder`)
- The environment-variable prefix is still `WAYCODER_*`
- The App Store bundle id is `com.tanso.dolaima`

In other words: **the `waycoder` command and the `WayCoder/` path in this documentation stay exactly as they are**; only "what this product is called" became **Dolaima (都来码)**.

## What Is This

Dolaima is a Chinese-language, multi-agent, economy-minded coding agent. It takes the strengths of tools such as Claude Code, OpenCode, Crush, Codex and Cursor and combines them into one agent. The entire codebase is built in C# .NET 10 Native AOT, and it includes permission confirmation, Git integration, web fetching, LSP code navigation, a memory system, background tasks, code review, Watch mode and many other capabilities. Copy it to any Windows machine and run it directly — no .NET runtime installation required.

## Run It Once

```bash

# TUI edition
WayCoder

# Web edition
WayCoder --web

# CLI edition
WayCoder --cli

# GUI edition
WayCoder --gui

# Watch mode (watches AI! comments and triggers the agent automatically)
WayCoder --watch

# Tiny mode (local small models / fewer tokens; only enabled by an explicit --tiny, as in --tiny 8k)
WayCoder --tiny
WayCoder --tiny 8k

# Economy mode (keeps a normal context window; --economy [on|auto|off|extreme], on by default)
WayCoder --economy          # on: trimmed prompt + earlier compaction + output ceiling
WayCoder --economy auto     # auto: thresholds scale with task complexity (save on simple jobs, keep quality on complex ones)
WayCoder --economy extreme  # extreme: minimal prompt (265 characters) + 7 core tools (read/write/edit/bash/web_search/fetch/ask), 90% fewer tokens per turn

WayCoder --permit tiny      # pure-chat work mode (Chat): no tools at all + no system prompt, ~0 tokens per turn

WayCoder --mode plan         # read-only planning mode (Plan): read-only whitelisted tools + trimmed planning prompt, produces a plan for approval

WayCoder --mode chat         # pure-chat work mode (Chat): same as --permit tiny; an explicit --mode wins
# Economy-mode tool trimming (Build tier only; off = full set, the higher it goes the more it trims):
#   Off=49 → Auto=37 (drops bash-replaceable) → On=32 (drops more search/edit redundancy) → Extreme=7 (core set) → pure-chat Chat=0
# Priority preference (auto only): /config EconomyPriority quality|balanced|cost (default quality)
# Measured (writing a 10,000-line Snake, minimax-m3, 5-tool whitelist): auto 8.0M tokens → extreme 4.2M tokens (-48%), roughly half the cost
# Configuration precedence: config.json is the authority and environment variables do not override it (switch modes with --economy or by editing the configuration)

# Model management (import / capability check / connection test)
WayCoder --model import alllocal          # import every local model (Ollama /api/tags + LM Studio + CC Switch)
WayCoder --model import allonline         # import every online model list (9 endpoints including OpenRouter/Groq/SiliconFlow/DeepSeek/OpenAI)
WayCoder --model import all               # local + online, everything
WayCoder --model import online openrouter # a specific online source
WayCoder --model check [connect]          # check model capabilities: think / tools / vision / API format / context / key
WayCoder --model report                   # test the reachability of every connect and produce an available/failed report
WayCoder --model free                     # scan for free models (zen -free / openrouter :free) and write the working ones to free.json
WayCoder --model restore                  # restore the model from before /free switched to a free one (across sessions)
WayCoder --connect test                   # reachability test for every connect (endpoint first)
# Free models: --model free writes the usable free connects to ~/.waycoder/free.json (incremental; a model that does not reply within 5s is skipped);
# /free switches from that cache (no arguments = picker / N = switch directly / restore = back to a paid model) instead of rescanning every time; an empty cache tells you to run --model free first
# Model capability flags (SupportsThinking / SupportsTools / SupportsVision) live on the model entry in provider/*.json;
# LLM requests are gated on them: no schema is sent when tools are unsupported, and no reasoning when thinking is unsupported; files without the fields are inferred
# Online imports such as OpenRouter display a short name (openai/gpt-5.4 → gpt-5.4) while calls still use the full id

# Auto-update (check and self-replace; GitHub Releases first, Gitee as fallback)
WayCoder --update

# Batch task engine (many repositories in parallel, worktree isolation)
WayCoder --batch batch.json                       # read tasks from a JSON manifest
WayCoder --batch-repo https://x/r1 --batch-repo https://x/r2 --batch-task "fix the login bug"

# JSON output mode (IDE / script bridge; stdout carries exactly one structured JSON object)
WayCoder --json -p "fix a bug"

# Browser Web UI (three panes: session list + chat + info panel; Markdown rendering + permission-mode switching)
WayCoder --web          # default port 9527
WayCoder --web 9000     # a specific port

# Run the self-test (6232 checks; only Debug builds include --test)
WayCoder  --test
```

Give it a model and a key and it will run. It speaks the OpenAI-compatible API by default: on first launch (when `~/.waycoder/config.json` does not exist yet) it reads the `.env` in the project root and **imports it into config.json for good**, and from then on config.json is the single authority (editing `.env` no longer has any effect — only deleting config.json brings the bootstrap back):

| Provider | Example environment variables |
|---|---|
| DeepSeek (default `deepseek-v4-flash`) | `WAYCODER_API_KEY=sk-...` |
| OpenAI | `WAYCODER_MODEL=gpt-5.5` `WAYCODER_API_KEY=sk-...` |
| Local Ollama | `OPENAI_BASE_URL=http://localhost:11434/v1` `WAYCODER_MODEL=qwen2.5-coder` |

## Architecture

```
WayCoder/
├── Program.cs         entry + CLI + REPL (ANSI full-screen TUI)
├── Agent/             agent core (20 files)
│   ├── Agent.cs           main loop (Stop Hook + WorkReporter + 10-phase pipeline)
│   ├── AgentSlot.cs       multi-agent workspace (F1-F10 slot switching + background parallelism)
│   ├── LLM.cs             LLM client (streaming + progressive timeout retry + per-task cost tracking)
│   ├── ContextManager.cs  Crush-style context management (token tracking + auto-summary + progress events)
│   ├── SystemPrompt.cs    system prompt (15 structured blocks)
│   ├── WorkModeManager.cs work modes (Build/Plan/Chat)
│   └── FallbackLLM.cs / BackgroundTask.cs / WorkReporter.cs / TaskProgress.cs
├── Memory/            memory and sessions (9 files: StructuredMemory + MEMORY.md index / MemoryRetrieval / SessionManager / ProjectKnowledge)
├── Config/            configuration (24 files: Global.cs, the global ~/.waycoder/config.json authority / Config.Schema, 110 entries / ConnectionConfig / ModelCatalog / ModelCli)
├── Infra/             infrastructure (86 files: BashGuard / FileTracker / SandboxManager / HooksManager / UpdateChecker / DrawEngine drawing + image codecs + Logging/)
├── Git/               Git integration (8 files: GitRunner / GitCore / PackFile / RepoMapGenerator / WorktreeIsolation)
├── Watch/             Watch mode + ReviewMode
├── Sql/               hand-rolled SQL engine (SqlEngine.cs)
├── Skills/            skills + permissions (SkillsManager / PermissionManager / AutoModeClassifier / builtin/)
├── Test/              test/debug/demo code (SelfTest: 40 partial files + Benchmark/Keypad/TuiAudit/TuiDemo, 6232 checks in all)
├── Batch/             batch task engine (BatchSpec manifest model + BatchRunner multi-repo parallelism / worktree isolation)
├── Plugins/           compile-time plugin system (IPlugin SDK + PluginRegistry + [ModuleInitializer] auto-registration)
├── Tools/             49 tools
│   ├── BashTool.cs    GitTool.cs    LspTool.cs
│   ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
│   ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
│   ├── EditFileTool.cs AgentTool.cs  WebSearchTool.cs
│   ├── GlobTool.cs    GrepTool.cs    GitPRTool.cs
│   ├── PsTool.cs      KillTool.cs    LsTool.cs
│   ├── MkdirTool.cs   RmTool.cs      CdTool.cs
│   ├── FindReplaceTool.cs CpTool.cs  MvTool.cs
│   ├── DiffTool.cs    TreeTool.cs    WcTool.cs
│   ├── StatTool.cs    PwdTool.cs     SkillTool.cs
│   ├── DocTool.cs     KbTool.cs      TestTool.cs
│   ├── DownloadTool.cs JobOutputTool.cs JobKillTool.cs
│   ├── NotebookEditTool.cs MultiEditTool.cs
│   ├── AskUserQuestionTool.cs ScreenshotTool.cs
│   ├── ViewImageTool.cs
│   ├── TranscribeAudioTool.cs
│   ├── DrawTool.cs drawing (text DSL → SVG/PNG, zero reflection)
│   └── ImageConvertTool.cs / ConvertEncodingTool.cs / SqliteTool.cs / SymbolsTool.cs
└── UI/                 five front ends
    ├── TUI/            terminal UI (220 files)
    │   ├── Base/           control base (TuiBase→TuiControl→TuiView / TuiManager / TuiScreen / TuiWindow / InputManager / scroll math)
    │   ├── Controls/       core control library (45 files: TuiButton / TuiListView / TuiDynamicBar / TuiKeybindHelp / TuiMarkdown …)
    │   ├── Custom/         custom controls + dialogs (15 files: ModelPicker / FilePicker / CommandPalette / DiffPreview / UxHelper …)
    │   ├── Screens/        full-screen views (ChatScreen / MarkupChatScreen / SettingsScreen / EditorScreen …)
    │   ├── Edit/           terminal source editor (EditorCore / Syntax highlighting / DiagnosticManager lint diagnostics)
    │   ├── Renderers/      tool output renderers (7 files)
    │   └── Raw/            `.tui` declarative layouts
    ├── Shared/         cross-front-end shared pure logic (MarkdownRenderer / UnifiedDiff / AnsiHelper / AnsiColors / terminal buffers and ANSI / Vml* protocol and host interfaces)
    ├── WEB/            Web front end (WebServer / WebChat / www assets)
    ├── CLI/            CLI front end (Arguments registration / Commands slash commands)
    └── GUI/            Avalonia GUI placeholder (reserved for extension)
```

## 49 Tools

| Tool | Purpose |
|---|---|
| `bash` | Run shell commands, track cwd, detect dangerous commands, support background execution |
| `read_file` | Read files showing line numbers, offsets and line limits; supports PDF/Markdown |
| `write_file` | Create/overwrite files (creates directories automatically, diff-preview confirmation) |
| `edit_file` | Exact-match find and replace, outputs a diff, supports replace_all |
| `multiedit` | Batch editing — several replacements in one operation |
| `glob` | File pattern matching sorted by modification time, filters ignored files automatically |
| `grep` | Regex content search, supports literal_text, filters ignored files automatically |
| `agent` | Spawn sub-agents (isolated context, recursion forbidden; the tasks array runs in parallel) |
| `git` | Git operations (status/log/diff/commit/branch) |
| `fetch` | Web fetching with HTML sanitizing + Markdown extraction |
| `lsp` | LSP code navigation (go-to-def, references, hover, symbols), 14 languages |
| `symbols` | Global reverse symbol index — locate a symbol definition in one shot (file:line) |
| `memory` | Read/write project memory (structured .waycoder/memory/ format, supports read/write/search/delete/share) |
| `todo` | Structured task list |
| `struct_todo` | Enhanced todo (priority, dependencies, status tracking) |
| `lint` | Static code checks for 25+ languages |
| `web_search` | Web search (through DuckDuckGo) |
| `git_pr` | Create/push/link a Git PR |
| `ps` | Process list (pure C#) |
| `kill` | Terminate processes, protecting system processes |
| `ls` | Directory listing with recursion/filtering/depth |
| `mkdir` | Create directories recursively |
| `rm` | Safe file/directory deletion |
| `cd` | Change the working directory |
| `cp` | Copy files/directories |
| `mv` | Move/rename files |
| `diff` | Line-by-line file difference |
| `tree` | ASCII directory tree generation |
| `wc` | Line/word/character/byte counts |
| `stat` | File/directory metadata |
| `find_replace` | Cross-file regex find and replace |
| `pwd` | Print the working directory |
| `skill` | Load a SKILL.md skill's full content on demand (name + description are in the system prompt) |
| `doc` | Look up the latest library/framework documentation (search + fetch) for current APIs and usage |
| `download` | HTTP GET a file to disk (safety checks, 500MB maximum) |
| `notebook_edit` | Jupyter Notebook (.ipynb) editing (replace/insert/delete cell) |
| `export_chat` | Conversation export (Markdown / JSON / HTML) |
| `job_output` | Read the output of a background bash job |
| `job_kill` | Terminate a background bash job |
| `ask_user_question` | Ask the user for confirmation (single/multi choice + text input) |
| `screenshot` | Screenshot (terminal text / desktop PNG + OCR) |
| `view_image` | View a local image and attach it to the next request for a vision model to read |
| `transcribe` | Transcribe an audio file to text (Whisper-compatible API), completing multimodal audio input |
| `draw` | Draw with text instructions (transforms/new shapes/strokes/gradients/textures/clipping/icon templates, 20+ commands), output as SVG vector or PNG bitmap |
| `convert_image` | Convert between image formats (PNG/JPG/BMP); input detected by magic number, output chosen by extension |
| `convert_encoding` | Convert file encodings (GB18030/GBK/Big5/Shift-JIS and so on, UTF-8 by default) |
| `sqlite` | Run SQL against a local SQLite database (through the system `sqlite3` command line, which must be installed) |
| `kb` | Search the global programming knowledge base (`~/.waycoder/kb/`: pitfalls/fixes/habits), supports `search` / `diagnose` |
| `test` | Run a test command and parse the results (pass/fail counts, locating failed cases; dotnet test / pytest / npm test / cargo test / go test) |

## REPL Commands

```
/model <name>    switch model / open the model picker (Ctrl+M)
/compact         manually compact the context
/tokens          show token usage and cost estimate (including per-task spend)
/stats           show session statistics (token/PromptCache/LLM metrics)
/recent          show files changed in this session (alias /diff)
/plan            plan mode — plan first, then execute
/init            analyze the project and generate AGENT.md (/init claude generates CLAUDE.md)
/mcp             show MCP server status / reconnect
/kb (/mind)      self-learning knowledge base (mine = distill / save = remember / diagnose = diagnose / path = learning path / profile [json] = profile / retro = retrospective / review = spaced repetition / weak = statistics; see [docs/知识库.md](docs/知识库.md))
/teach           teaching mode (on/off = explain + quiz / assess = closed-loop evaluation / status = progress)
/doctor          post-release system self-check (/doctor fix applies safe repairs)
/git             Git operations (status/log/diff/commit/branch)
/versions        file edit version history (/undo <file> [n] steps back edit by edit)
/checkpoints     list checkpoints (/checkpoints prune [N] to clean up)
/undo            revert (<file> [n] edit-level / [id] [file] checkpoint-level)
/perm off|project|network-off|hard  switch the sandbox boundary (independent of permissions)
/permit ack|auto|smart|yolo  switch permission mode (independent of the boundary)
/edit            built-in source editor (one EditorCore shared by three front ends: TUI / Web `✏ Editor` / GUI `--gui [file]`)
/mode build|plan|chat  switch work mode (Shift+Tab)
/cd [path]            show/set the current slot's working directory (independent per slot)
/update [check|now]  check / auto-upgrade to the latest version
/auto            smart tiered confirmation
/watch           toggle Watch mode
/session         session management (list/save/load/resume)
/join            continue from a Claude/Codex/OpenCode/Crush/Aider/Gemini session (chat + todo + git)
/export          export conversation history
/history         search conversation history
/settings        graphical settings UI
/menu            feature menu (jump straight to the model/settings/session/Diff screens + common commands; equivalent to Ctrl+U)
/theme           switch theme
quit / exit      quit (normal Ctrl+C saves and exits / emergency Ctrl+Q)
```

## Multi-Agent Workspace

**F1-F10** switch between 10 independent agent slots with a single key, each with its own screen (its own chat history, input draft and status bar), none interfering with the others. **Slots also run in parallel in the background** — while a task runs on F1 you can switch straight to F2 and start a new one; output from background slots is buffered and shown in full when you switch back. Ten digits on the left of the status bar show each slot's state in real time:

| Display | Meaning |
|---|---|
| White background | The screen currently shown |
| Grey | Idle |
| Green | Working |
| Yellow | Waiting for permission confirmation |
| Red | Error |

> Hotkeys while running: `Esc` interrupts the current slot's agent, `Ctrl+Z` pauses gracefully (shuts down after the current batch finishes)
> Rebound hotkeys: help `Ctrl+H`, panel `Ctrl+B`, settings `Ctrl+T`, quit `Ctrl+C` (emergency `Ctrl+Q`), permissions `Ctrl+P`, economy `Ctrl+E`,
> menu `Ctrl+U`, switch connection `Ctrl+N`, theme `Ctrl+W`, search history `Ctrl+F`, swap large/small model `Ctrl+O`
> (all **two-key** combinations; `Ctrl+Shift+letter` is either grabbed by the terminal or loses its modifier on Windows, so they have all been retired)

## TUI Layout Preview

While writing `.tui` declarative layouts you can preview them live:

- **Terminal preview**: `waycoder --tui-preview <file.tui>` / `--tui-watch <file.tui>` (refreshes on save; like `--test`, these are `WAYCODER_TEST`-only development-build arguments and are absent from Release builds)
- **WPF graphical preview**: `dotnet run --project WayCoder.Preview -- <file.tui>`
  - Monospaced, cell-by-cell rendering on a pure black background, content centered, automatic refresh on save
  - **Zoom**: slider / zoom in and out buttons / **mouse wheel (Ctrl+wheel)**
  - **Grid toggle**: show/hide the cell grid lines (to see boundaries while designing); seamless background by default (same colors merged + pixels rounded)
  - **Screen size simulation**: quick picks from 80x25 to 240x72 plus row/column adjustment, to see how the layout adapts
  - **Recent files**: the path combo box remembers the last 10 opened
  - Elements carry the `InDesign`/`SimulatedScreen` environment properties, which are true in preview mode

## Key Design Decisions

- **Systematic pipeline**: complex tasks automatically go through 10 phases (investigate → analyse → plan → break down → delegate → execute → debug → review → commit → summarize)
- **edit_file uses unique-substring matching**, not line numbers — safe and reviewable
- **Three-stage context compaction**: 50% trim → 70% LLM summary → 90% hard fold, with real-time progress events
- **Progressive timeout retry**: each LLM timeout is longer than the last (1x→1.5x→2x→3x→4x→6x→8x), up to 5 retries
- **Parallel sub-agents**: the tasks array supports concurrency and returns aggregate results; recursion is prevented by not giving sub-agents the agent tool
- **Multi-agent workspace**: F1-F10 switch between 10 independent session slots, the status bar shows their working state in real time, and slots support task queues
- **Truly parallel sessions**: slot tasks run on background threads without blocking the main loop, so you can switch freely while they run; active slots stream in real time and inactive slots buffer their output, with switching and routing sharing the slot lock so no tokens are lost (comparable to Claude Code's multiple windows)
- **Hook system**: 8 events (PreToolUse/PostToolUse/Stop/PreCompact and more) with a structured JSON output protocol
- **Dynamic status bar**: live agent state / tool execution / compaction progress with a Braille spinner
- **Per-task cost tracking**: prompt/completion tokens and cost are counted per conversation turn
- **Skill system**: the standard SKILL.md format; the system prompt carries only the name + description, and the `skill` tool loads the full content on demand
- **Quality checks on automatic Git commits**: a conventional-commit prefix is enforced, a non-compliant message is retried once, and there is a default fallback message
- **Worktree isolation**: bash detects worktree paths automatically and switches cwd
- **AOT compilation: hand-written JSON serialization**, no reflection
- **Permission system**: bash/write/edit/agent confirm inline by default and `/permit yolo` skips confirmation; `/perm full-auto` enables a restricted sandbox separately
- **Dual-model architecture**: a large model for complex work and a small model for compaction/summarizing, split automatically to save money
- **Model fallback chain**: `/connect chain <c1> <c2> ...` configures a list of connects and falls back along it on failure (model + key + baseUrl switch together and may cross providers); **off by default**, turn it on with `/connect chain on`
- **One built-in editor across three front ends**: the same `EditorCore` model is available in the TUI (`/edit`, full screen) / Web (`✏ Editor`, a transparent textarea that keeps the Chinese IME working) / GUI (Avalonia `EditorWindow`) — syntax highlighting, line numbers, undo, find, and a lint summary after saving; `waycoder --gui [file]` opens the editor directly
- **Watch mode**: file watching + AI! comment parsing → thread-safe queue → automatic REPL execution
- **Structured memory**: multiple `.waycoder/memory/*.md` files with frontmatter plus a MEMORY.md index, searchable across sessions
- **Diff preview**: `/config DiffPreview true` enables per-hunk confirmation before writing a file, skipped automatically in non-interactive mode
- **Bash safeguards**: 87 forbidden commands + 70 safe-listed ones, with every command in a pipeline checked independently
- **Plan approval gate**: in `Plan` mode (Shift+Tab) the model does not start executing once it has produced a plan; an approval box appears in place — approve and it switches back to `Build` mode and continues, reject and it stops (comparable to Claude Code Plan Mode)
- **Project initialization `/init`**: scans the project and generates a Chinese AGENT.md (the default; `/init claude` generates a CLAUDE.md compatible with Claude Code; language/framework/build-tool detection plus build/test/lint command probing); the next launch injects it into the system prompt automatically through `ProjectContext.LoadInstructions`
- **MCP state management `/mcp`**: a structured state model (Connecting/Connected/Failed) plus hot reconnect; `/mcp` shows server state and `/mcp reload [name]` reconnects, comparable to Claude Code's /mcp
- **MCP resources/prompts**: `resources/list` + `resources/read` are registered as an `mcp__<server>__resources` reader tool, and `prompts/list` + `prompts/get` register every template as an `mcp__<server>__prompt__<name>` tool, comparable to Claude Code MCP resources/prompts
- **Built-in auto-update**: `/update` checks, `/update now`/`--update` self-replaces; the version check goes through **GitHub Releases** (the release channel) and falls back to Gitee on failure (overridable by environment variable); on Windows it drops a `.new` file plus `upgrade.bat` and replaces and restarts after exit, while on Unix an atomic rename covers the running binary (comparable to Claude Code `claude update`)
- **Distribution channels**: **code on Gitee, releases on GitHub** — the source repository is on Gitee (private), while Release assets / the winget manifest / the brew formula / the apt repository all point at GitHub; release binaries are built locally by `scripts/release.sh` (the repository uses no CI). `packaging/` provides the winget manifest / Homebrew formula / apt `.deb` packaging scripts; see [docs/安装与升级.md](docs/安装与升级.md)
- **Multimodal (image + audio)**: `view_image` attaches a local image so a vision model can "look at it", and `transcribe` turns audio into text (Whisper-compatible API) — covering both image and audio multimodal input, comparable to Codex CLI / Gemini CLI
- **Batch task engine**: `--batch`/`--batch-repo` process many repositories in parallel; each task is `git clone`d to its own copy and run by a child process in `-p` one-shot mode (worktree isolation), with an aggregate report + exit code, comparable to Cursor batch fixes / Aider multi-repo scripts
- **Compile-time plugin system**: the `IPlugin` SDK — drop a `.cs` file into `WayCoder/Plugins/` with `[ModuleInitializer]` auto-registration and it contributes tools (`ITool`) and slash commands (`ISlashCommand`); reflection-free under AOT and shipped with the single-file exe, see [docs/插件系统.md](docs/插件系统.md)
- **JSON output mode (IDE bridge)**: `--json -p "fix a bug"` runs the agent silently in one-shot mode and stdout carries exactly one structured JSON object (schema/success/answer/error/model/usage/cost_usd/duration_ms/changed_files) for VS Code extensions, CI scripts and external tools to `JsonNode.Parse` directly, with no ANSI animation to strip; the pure `JsonResult.Build` function makes self-testing easy (comparable to Claude Code `--output-format json`)

## Contributing / License

Shenzhen Tanso Intelligent Technology Co., Ltd.

---

Author [施探宇(aleck)](https://gitee.com/aleckstygit), Shenzhen, China
