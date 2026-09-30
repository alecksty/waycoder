# 隐私与安全 / Privacy & Security

本文档说明 **都来码（Dolaima）** 会把哪些数据发送到外部、发送给谁、以及在本机保存了什么。

⚠ **它是两个地方共同指向的页面**：Windows 侧 winget 清单的 `PrivacyUrl`，
以及 **App Store 的「隐私政策 URL」**（`docs/上架资料包.md` §六）—— 改这一页两处都跟着变。

This document describes what data **Dolaima** sends externally, to whom, and what it stores
locally. It is the page referenced by the `PrivacyUrl` field of the winget manifest **and by the
App Store listing's Privacy Policy URL**.

---

## English summary (for reviewers)

1. **No telemetry.** The project operates no server of its own. It collects and transmits
   no usage data, no crash reports and no analytics.
2. **Data goes only to the AI provider *you* configure.** Your prompts, the conversation
   history, and the contents of files or command output that tools read, are sent over
   HTTPS to the LLM provider whose base URL **you** set in the configuration. There is no
   intermediary server run by this project.
3. **API keys are stored in plain text (unencrypted)** in `~/.waycoder/api_keys.json`.
   See §3.
4. **A few tools contact third parties** that you (or the model) point them at:
   `web_search` (DuckDuckGo / Bing), `fetch` and `download` (arbitrary URLs),
   `transcribe` (a Whisper-compatible endpoint), and any MCP servers you configure. See §2.
5. **The agent runs with your user privileges** — it can execute shell commands and modify
   files in your working directory. Permission modes and a sandbox are provided to bound
   this; see §5.

---

## 1. 发往 AI 服务商的数据

都来码是一个编程智能体，它的工作方式就是把上下文交给大模型。以下内容会通过 HTTP(S)
发往**你在配置里指定的** LLM 服务商（`baseUrl` + `apiKey` 都由你设定，可以是 OpenAI、
Anthropic、DeepSeek、OpenRouter、本地 Ollama 等任意兼容服务）：

| 内容 | 说明 |
|---|---|
| 你的输入 | 你在终端/Web/GUI/手机端输入的提示词与后续对话 |
| 对话历史 | 本次会话（含压缩摘要）中累积的上下文 |
| 读取到的文件内容 | `read_file` / `grep` / `glob` 等工具读到的**文件正文** |
| 命令输出 | `bash` 等工具执行后返回的 stdout/stderr |
| 工具结果 | 各工具（含 MCP 工具）的返回值 |
| 图片 / 音频 | 仅当你使用 `view_image` / `screenshot` / `transcribe` 时，对应的图像或音频文件 |
| 系统提示词 | 由程序生成，含项目结构、Git 状态、记忆等（可用 `--economy` / `--tiny` 精简） |

⚠ **智能体会自行决定读哪些文件**。如果工作目录里有敏感文件（密钥、证书、`.env`），
模型可能读到并把内容发往服务商。请把敏感文件放进忽略规则（`FileIgnoreManager` 支持的
忽略文件），或不要在含敏感数据的目录里运行。

**发送给谁完全由你的配置决定** —— 本项目不做任何转发、代理或二次上传。

## 2. 其他对外网络请求

除 LLM 服务商外，以下功能会发起对外请求，均**只在你（或模型）实际调用该工具时**发生：

| 功能 | 目标 | 发送内容 |
|---|---|---|
| `web_search` | DuckDuckGo、Bing | 搜索关键词 |
| `fetch` | 你或模型指定的 URL | HTTP 请求（无附加数据） |
| `download` | 同上 | 同上 |
| `transcribe` | 你配置的 Whisper 兼容端点 | 待转写的音频文件 |
| MCP 服务器 | 你在 `.waycoder/mcp_servers.json` 里配置的服务器 | 由该 MCP 协议决定 |
| 自动升级检查 | Gitee Releases、GitHub Releases | 仅一次 `GET`，不含任何用户数据或标识 |
| 模型目录 / 连接测试 | 你配置的服务商端点 | 列表与连通性探测请求 |

## 3. 本地保存的数据（含明文密钥）

| 路径 | 内容 |
|---|---|
| `~/.waycoder/api_keys.json` | **各服务商的 API Key，明文存储、未加密** |
| `~/.waycoder/config.json` | 全局配置（**可能包含密钥**，取决于你如何配置） |
| `~/.waycoder/connections.json` / `providers.json` | 连接与供应商定义（地址、服务商名） |
| `~/.waycoder/sessions/` | 会话记录（按槽位分目录） |
| `~/.waycoder/kb/` | 知识库 |
| `<项目>/.waycoder/memory/` | 结构化记忆（`*.md` + `MEMORY.md` 索引） |
| `<项目>/.waycoder/` | 项目级配置、MCP 服务器清单等 |

⚠ **密钥是明文的**：任何能读到你用户目录的进程/人都能读到它。请依赖操作系统层面的
文件权限与磁盘加密来保护该文件；不要在共享账号或多用户机器上存放生产密钥。
`.env` 文件（若存在）同样可能含密钥，且它只是首次启动的引导来源。

**本项目不会把这些数据上传到自己的服务器**（因为根本没有这样的服务器）。

## 4. 没有遥测

代码库中不存在遥测、统计上报、崩溃上报或广告 SDK。唯一的"上报"是**你主动触发的**
自动升级检查——它只读取版本号，不发送任何本机信息。

（工具目录里出现的 `sentry` 是一条**可选**的 MCP 服务器条目，需你自己配置 token 并主动
启用，与本程序自身的任何上报行为无关。）

## 5. 本机权限与边界

智能体以**你的用户身份**运行，因此它有权执行 shell 命令、修改文件、访问网络。为约束这一点，
程序提供：

- **权限确认**（Ask / Auto / SmartAuto / YOLO）——决定哪些工具调用需要你逐次确认
- **沙箱边界**——限定可写目录与网络访问范围
- **工作模式**——Plan 模式只读（白名单工具）、Chat 模式零工具
- **工具白名单/黑名单**——`WAYCODER_ALLOWED_TOOLS` / `WAYCODER_DISABLED_TOOLS`
- **Bash 安全防护**——`BashGuard` 拦截危险命令

⚠ 默认配置下 `bash`/写文件/编辑文件会**逐次询问**；`/perm yolo` 会跳过确认
（危险：等于把本机交给模型）。请按你的风险偏好选择。

## 6. 如何减少外发

- 用 `--economy` / `--tiny` 精简系统提示词与上下文
- 在敏感目录跑之前先设置忽略规则，或改用 Plan 模式（只读）
- 把服务商指向**本地模型**（如 Ollama）：数据不出本机
- 定期清理 `~/.waycoder/sessions/` 与项目 `.waycoder/memory/`

## 7. 移动端补充（iOS / Android）

上面的说明对桌面端与手机端同样成立，手机上有几处不同：

- **数据存在哪**：默认在 App 私有目录里（不是 `~/.waycoder/`）。Android 上若你授予
  「所有文件访问」，配置与工作区会改放到外部存储的 `waycoder/` 目录，方便你用别的 App
  打开自己写的代码；iOS 上始终在私有目录内。
- **App 权限**：相机会在「拍照」、相册在「选图」、麦克风在「语音输入」时**按需申请**，
  拒绝不影响其它功能。
- **内购**：手机版的「全能版」是一次性买断，付款由 Apple 处理 —— 我们只收到
  "这个 Apple ID 买过没有"的凭证，拿不到姓名、卡号或账单地址。
- **手机版不包含**：自动升级（App Store 不允许应用自更新）、命令行安装与 winget。

手机版 App 内另有一份同样口径的隐私政策（**设置 → 关于 → 使用说明与隐私**），离线可看。

## 8. 儿童与年龄

本软件面向具备编程基础的开发者，**不面向 13 岁以下的儿童**，也不会刻意收集他们的信息。
移动端（App Store 版）的年龄分级为 **13+**；未满 18 岁的使用者请在监护人同意下使用，
并在填写模型服务商 API Key 之前确认已获得允许（费用由账号持有人承担）。

## 9. 联系

- 问题与漏洞报告：<https://github.com/alecksty/waycoder/issues>
- 邮箱（隐私疑问、下架/删除数据请求）：<alecksty@163.com>

*Contact: <alecksty@163.com> — English is fine.*

---

_最后更新：2026-09-28_
