# WayCoder (道码) — Agent Guide

## Project Identity

WayCoder is an AI coding assistant CLI for terminals, written in C# targeting **.NET 10** with **NativeAOT** compilation. It produces a single binary (`corecoder`) with zero runtime dependencies. The UI is built on **Spectre.Console** for rich terminal rendering.

## Essential Commands

```bash
# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release

# Run (Debug)
dotnet run

# Run with prompt
dotnet run -- -p "fix the bug in foo.cs"

# Run self-tests (built-in, no framework)
dotnet run -c Release -- --test

# Publish NativeAOT single-file binary
dotnet publish -c Release

# Format / lint
dotnet format
```

There is **no external test framework**. Self-tests live in `SelfTest.cs` and are invoked via `--test` / `-t`. No `Makefile`, no CI configs in the repo.

## Architecture

### Core Loop (Agent.cs)

```
User Input → Agent.ChatAsync()
  → LLM.ChatAsync() (OpenAI-compatible streaming)
    → If tool calls: ExecuteToolAsync() for each
      → PermissionManager.CheckAsync() (dangerous tools)
      → HooksManager.RunPreToolUseAsync()
      → tool.ExecuteAsync()
      → AppendLintFeedbackAsync() (auto lint on write_file/edit_file)
      → AppendTestFeedbackAsync() (auto test on source file changes)
      → HooksManager.RunPostToolUseAsync()
    → If text response: return to user
  → CompressWithSmallModel() (context compression)
  → Repeat until no tool calls or max rounds
```

### Key Files and Their Roles

| File | Role |
|------|------|
| `Program.cs` | CLI entry, REPL loop, one-shot mode, pipe mode |
| `Agent.cs` | Agent loop: LLM ↔ tool execution, lint/test feedback, auto-commit |
| `LLM.cs` | OpenAI-compatible HTTP client, SSE streaming, tool call parsing, pricing, `JsonHelper` |
| `Config.cs` | Environment variable loading, `.env` file, `WAYCODER_*` / `CORECODER_*` prefixes |
| `SystemPrompt.cs` | Generates system prompt with project context, memory, repo map, tools |
| `ContextManager.cs` | 3-layer compression: snip → summarize → hard collapse |
| `SessionManager.cs` | Session JSON persistence to `~/.corecoder/sessions/` |
| `HooksManager.cs` | Pre/Post tool-use hooks via shell scripts in `.corecoder/hooks/` |
| `PermissionManager.cs` | Dangerous tool confirmation: Ask / Auto / Yolo modes |
| `SandboxManager.cs` | Process sandbox: env sanitization, directory escape detection, system write blocking |
| `RepoMapGenerator.cs` | ASCII tree + symbol extraction for LLM context |
| `ProjectContext.cs` | Detects project type, language, framework, reads instruction files |
| `McpManager.cs` | MCP client (stdio/HTTP), tool discovery, lives in `Tools/McpClient.cs` |
| `MemoryStore.cs` | Legacy memory in `.corecoder/memory.md` (migration source) |
| `StructuredMemory.cs` | Structured memory in `.corecoder/memory/*.md` with MEMORY.md index |
| `CustomCommands.cs` | Slash commands from `.corecoder/commands/*.md` |
| `CheckpointManager.cs` | Git stash / file backup snapshots |
| `WatchMode.cs` | File watcher for `AI!` / `AI?` annotations |
| `ReviewMode.cs` | Code review via `git diff` |
| `FallbackLLM.cs` | Model fallback chain on failure |
| `PromptCache.cs` | System prompt hash tracking for cost estimation |
| `DebugLog.cs` | Debug logging to `logs/` directory |
| `SelfTest.cs` | Built-in self-test framework |
| `Edit/Editor.cs` | Terminal source code editor with syntax highlighting |
| `Edit/DiagnosticManager.cs` | LSP diagnostic integration |
| `Edit/Syntax.cs` | Syntax highlighting definitions |
| `UI/ScreenManager.cs` | Full-screen REPL buffer manager |
| `UI/TuiHelper.cs` | CJK-aware width calculation, Spectre markup helpers |
| `UI/TuiColors.cs` | Color constants using Spectre styles |
| `UI/TuiBox.cs`, `TuiInput.cs`, `TuiList.cs`, `TuiTable.cs`, `TuiPrompt.cs`, `TuiProgress.cs`, `TuiBanner.cs` | Reusable UI widgets |
| `UI/SettingsPage.cs` | Interactive settings page (Ctrl+O) |

### Tools (30+ in `Tools/`)

All implement `ITool` (`Name`, `Description`, `Parameters`, `ExecuteAsync`, `Schema`). Registered in `ToolRegistry.BuiltinTools`. MCP tools are auto-discovered and added to `AllTools`.

Classified by risk:
- **Dangerous** (require confirmation): `bash`, `write_file`, `edit_file`, `agent`, `kill`, `rm`
- **Safe** (no confirmation): `read_file`, `glob`, `grep`, `ls`, `stat`, `fetch`, `todo`, `lsp`, `memory`, `lint`, `web_search`, `git_pr`, `ps`, `mkdir`, `cd`, `find_replace`, `cp`, `mv`, `diff`, `tree`, `wc`, `pwd`, `skill`, `doc`
- `doc` performs network lookups only (read-only), so it is Safe

## NativeAOT Gotchas

The project compiles with `<PublishAot>true</PublishAot>`. This means:

- **No reflection**. `JsonHelper` in `LLM.cs` does manual JSON serialization with `StringBuilder` instead of `System.Text.Json` reflection-based serialization.
- `JsonHelper.DeepClone()` clones via serialize/deserialize round-trip.
- `JsonHelper.SerializeArgs()` manually builds JSON strings.
- All JSON handling uses `System.Text.Json.Nodes` (`JsonObject`, `JsonArray`, `JsonNode`) which is AOT-safe.
- `GlobalUsings.cs` provides: `global using System.Text.Json.Nodes;` and `global using System.Runtime.InteropServices;`

## Important Conventions

### Environment Variables
- Dual prefix support: `WAYCODER_*` (new) and `CORECODER_*` (legacy, backward compat)
- API key lookup chain: `WAYCODER_API_KEY` → `CORECODER_API_KEY` → `OPENAI_API_KEY` → `DEEPSEEK_API_KEY` → `ANTHROPIC_API_KEY` → `API_KEY`
- `.env` file loaded from cwd upward to home dir; existing env vars are **not** overwritten
- DeepSeek models auto-set `BaseUrl` to `https://api.deepseek.com` if unconfigured

### Message Handling
- Messages are **deep-cloned** before sending to LLM to avoid `JsonNode.Parent` conflicts
- Tool call IDs must be tracked via `tool_call_id` to match responses
- Interrupted tool calls are auto-filled with error messages to keep history valid

### Context Compression (3 layers)
1. **Snip** (50% threshold): Truncate tool outputs >1500 chars, keep first/last 3 lines
2. **Summarize** (70% threshold): LLM-driven summary of old messages, keep recent 8
3. **Hard Collapse** (90% threshold): Keep only last 4 messages + summary

`SafeSplit()` ensures tool messages aren't separated from their assistant messages.

### Lint/Test Feedback Loop
After `write_file` or `edit_file`:
- Lint: auto-runs `LintTool.DetectLanguage()` + `LintTool.ExecuteAsync()`, appends results to tool output
- Test: auto-detects test command from project type (dotnet test, npm test, go test, cargo test, pytest), 60s debounce, 30s timeout
- Both are best-effort; failures don't block the main flow

### Permission Modes
- `Ask` (suggest): Every dangerous tool confirmed
- `Auto` (auto-edit): First use confirmed, then auto-allowed for session
- `Yolo` (full-auto): No confirmation + bash runs in sandbox
- `bash` in sandboxed full-auto mode skips permission check entirely

### Sub-projects
- `game/` and `lottery/` are **excluded from compilation** via `<Compile Remove>` and `<None Remove>` in the csproj
- `game/Tetris.Tests/` exists but is not part of the main solution

### Instruction Files
ProjectContext loads these files from cwd upward (excluding `memory.md`):
- `CLAUDE.md`, `AGENTS.md`, `.cursorrules`
- `.claude/*.md`, `.corecoder/*.md`

### Watch Mode
- `--watch` / `-w` flag enables `WatchMode`
- Monitors file changes, scans for `AI!` (prompt) and `AI?` (question) annotations
- Aider-compatible syntax
- Ignores: `bin`, `obj`, `.git`, `node_modules`, `.corecoder`, `logs`, etc.
- Only watches specific source file extensions

### Slash Commands
- Built-in: `/help`, `/exit`, `/clear`, `/save`, `/load`, `/stats`, `/model`, `/settings`, `/memory`, `/review`, `/test`, `/checkpoint`, `/sandbox`, `/yolo`, `/debug-on`, `/debug-off`, `/editor`, `/pr`, `/diff`, `/history`
- Custom: `.corecoder/commands/*.md` with YAML frontmatter (`description` field)

### Styling
- All Chinese comments are in mixed Chinese/English
- UI uses Spectre.Console `Markup` with `[color]` tags
- `TuiHelper.Esc()` escapes user content for Spectre markup
- CJK display width is 2 columns per character (implemented in `TuiHelper.RuneWidth()`)

## Editing Guidelines

- `edit_file` tool uses exact string matching (old_string → new_string)
- Include enough context in `old_string` for unique match
- `write_file` for new files or complete rewrites only
- `JsonNode` deep-cloning pattern: `JsonNode.Parse(node.ToJsonString())`
- Always check `Tools/ITool.cs` for the tool interface contract before adding new tools
- New tools register in `ToolRegistry.BuiltinTools` list
- New config fields go in `Config.cs` with dual `WAYCODER_*` / `CORECODER_*` env var pattern