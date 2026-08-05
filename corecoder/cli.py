"""交互式 REPL - 面向用户的终端界面。"""

import argparse
import os
import sys

from prompt_toolkit import prompt as pt_prompt
from prompt_toolkit.history import FileHistory
from prompt_toolkit.key_binding import KeyBindings
from rich.console import Console
from rich.markdown import Markdown
from rich.panel import Panel

from . import __version__
from .agent import Agent
from .config import Config
from .llm import LLM, LiteLLM
from .session import list_sessions, load_session, save_session

console = Console()


def _parse_args():
    p = argparse.ArgumentParser(
        prog="corecoder",
        description="极简 AI 编程智能体。支持任何兼容 OpenAI 的 LLM。",
    )
    p.add_argument("-m", "--model", help="模型名称（默认：$CORECODER_MODEL 或 gpt-5.5）")
    p.add_argument("--base-url", help="API 基础 URL（默认：$OPENAI_BASE_URL）")
    p.add_argument("--api-key", help="API 密钥（默认：$OPENAI_API_KEY）")
    p.add_argument("-p", "--prompt", help="一次性提示词（非交互模式）")
    p.add_argument("-r", "--resume", metavar="ID", help="恢复已保存的会话")
    p.add_argument("-v", "--version", action="version", version=f"%(prog)s {__version__}")
    return p.parse_args()


def main():
    args = _parse_args()
    config = Config.from_env()

    # CLI 参数覆盖环境变量
    if args.model:
        config.model = args.model
    if args.base_url:
        config.base_url = args.base_url
    if args.api_key:
        config.api_key = args.api_key

    # 使用 DeepSeek 模型时，自动设置 base URL
    if config.base_url is None and config.model.startswith("deepseek"):
        config.base_url = "https://api.deepseek.com"

    if not config.api_key:
        console.print("[red bold]未找到 API 密钥。[/]")
        console.print(
            "请设置以下环境变量之一：OPENAI_API_KEY、DEEPSEEK_API_KEY 或 CORECODER_API_KEY\n"
            "\n示例：\n"
            "  # OpenAI\n"
            "  export OPENAI_API_KEY=sk-...\n"
            "\n"
            "  # DeepSeek\n"
            "  export OPENAI_API_KEY=sk-... OPENAI_BASE_URL=https://api.deepseek.com\n"
            "\n"
            "  # Ollama（本地）\n"
            "  export OPENAI_API_KEY=ollama OPENAI_BASE_URL=http://localhost:11434/v1 CORECODER_MODEL=qwen2.5-coder\n"
        )
        sys.exit(1)

    llm_cls = LiteLLM if config.provider == "litellm" else LLM
    llm = llm_cls(
        model=config.model,
        api_key=config.api_key,
        base_url=config.base_url,
        temperature=config.temperature,
        max_tokens=config.max_tokens,
    )
    agent = Agent(llm=llm, max_context_tokens=config.max_context_tokens)

    # 恢复已保存的会话
    if args.resume:
        loaded = load_session(args.resume)
        if loaded:
            agent.messages, loaded_model = loaded
            # 从已保存的会话中恢复模型，除非 CLI 覆盖
            if not args.model:
                agent.llm.model = loaded_model
                config.model = loaded_model
            console.print(f"[green]已恢复会话：{args.resume}（模型：{agent.llm.model}）[/green]")
        else:
            console.print(f"[red]会话 '{args.resume}' 未找到。[/red]")
            sys.exit(1)

    # 一次性模式
    if args.prompt:
        _run_once(agent, args.prompt)
        return

    # 交互式 REPL
    _repl(agent, config)


def _run_once(agent: Agent, prompt: str):
    """非交互模式：运行一次提示词后退出。"""
    def on_token(tok):
        print(tok, end="", flush=True)

    def on_tool(name, kwargs):
        console.print(f"\n[dim]> {name}({_brief(kwargs)})[/dim]")

    try:
        agent.chat(prompt, on_token=on_token, on_tool=on_tool)
    except KeyboardInterrupt:
        console.print("\n[yellow]已中断。[/yellow]")
        sys.exit(130)
    except Exception as e:
        console.print(f"\n[red]错误：{e}[/red]")
        sys.exit(1)
    print()


def _repl(agent: Agent, config: Config):
    """交互式读取-求值-打印循环。"""
    console.print(Panel(
        f"[bold]CoreCoder[/bold] v{__version__}\n"
        f"模型：[cyan]{config.model}[/cyan]"
        + (f"  基础 URL：[dim]{config.base_url}[/dim]" if config.base_url else "")
        + "\n输入 [bold]/help[/bold] 查看命令，[bold]Ctrl+C[/bold] 取消，[bold]quit[/bold] 退出。",
        border_style="blue",
    ))

    hist_path = os.path.expanduser("~/.corecoder_history")
    history = FileHistory(hist_path)

    # Enter 提交，Esc+Enter 插入换行（用于粘贴代码块等场景）
    kb = KeyBindings()

    @kb.add("enter")
    def _submit(event):
        event.current_buffer.validate_and_handle()

    @kb.add("escape", "enter")
    def _newline(event):
        event.current_buffer.insert_text("\n")

    while True:
        try:
            user_input = pt_prompt(
                "You > ",
                history=history,
                multiline=True,
                key_bindings=kb,
                prompt_continuation="...  ",
            ).strip()
        except (EOFError, KeyboardInterrupt):
            console.print("\n再见！")
            break

        if not user_input:
            continue

        # 内置命令
        if user_input.lower() in ("quit", "exit", "/quit", "/exit"):
            break
        if user_input == "/help":
            _show_help()
            continue
        if user_input == "/reset":
            agent.reset()
            console.print("[yellow]对话已重置。[/yellow]")
            continue
        if user_input == "/tokens":
            p = agent.llm.total_prompt_tokens
            c = agent.llm.total_completion_tokens
            line = f"Token：[cyan]{p}[/cyan] 输入 + [cyan]{c}[/cyan] 输出 = [bold]{p+c}[/bold] 总计"
            cost = agent.llm.estimated_cost
            if cost is not None:
                line += f"  （约 ${cost:.4f}）"
            console.print(line)
            continue
        if user_input == "/model" or user_input.startswith("/model "):
            new_model = user_input[7:].strip() if user_input.startswith("/model ") else ""
            if new_model:
                agent.llm.model = new_model
                config.model = new_model
                console.print(f"已切换到 [cyan]{new_model}[/cyan]")
            else:
                console.print(f"当前模型：[cyan]{config.model}[/cyan]")
            continue
        if user_input == "/compact":
            from .context import estimate_tokens
            before = estimate_tokens(agent.messages)
            compressed = agent.context.maybe_compress(agent.messages, agent.llm)
            after = estimate_tokens(agent.messages)
            if compressed:
                console.print(f"[green]已压缩：{before} → {after} tokens（{len(agent.messages)} 条消息）[/green]")
            else:
                console.print(f"[dim]无需压缩（{before} tokens，{len(agent.messages)} 条消息）[/dim]")
            continue
        if user_input == "/save":
            sid = save_session(agent.messages, config.model)
            console.print(f"[green]会话已保存：{sid}[/green]")
            console.print(f"恢复命令：corecoder -r {sid}")
            continue
        if user_input == "/diff":
            from .tools.edit import _changed_files
            if not _changed_files:
                console.print("[dim]本次会话未修改任何文件。[/dim]")
            else:
                console.print(f"[bold]本次会话修改的文件（{len(_changed_files)} 个）：[/bold]")
                for f in sorted(_changed_files):
                    console.print(f"  [cyan]{f}[/cyan]")
            continue
        if user_input == "/sessions":
            sessions = list_sessions()
            if not sessions:
                console.print("[dim]没有已保存的会话。[/dim]")
            else:
                for s in sessions:
                    console.print(f"  [cyan]{s['id']}[/cyan]（{s['model']}，{s['saved_at']}）{s['preview']}")
            continue

        # 未知的 /command 不应作为提示词发送给模型
        if user_input.startswith("/"):
            console.print(f"[yellow]未知命令：{user_input.split()[0]}（输入 /help 查看帮助）[/yellow]")
            continue

        # 调用智能体
        streamed: list[str] = []

        def on_token(tok):
            streamed.append(tok)
            print(tok, end="", flush=True)

        def on_tool(name, kwargs):
            console.print(f"\n[dim]> {name}({_brief(kwargs)})[/dim]")

        try:
            response = agent.chat(user_input, on_token=on_token, on_tool=on_tool)
            if streamed:
                print()  # 流式输出后的换行
            else:
                # 响应未流式输出（在工具调用之后到达）
                console.print(Markdown(response))
        except KeyboardInterrupt:
            console.print("\n[yellow]已中断。[/yellow]")
        except Exception as e:
            console.print(f"\n[red]错误：{e}[/red]")


def _show_help():
    console.print(Panel(
        "[bold]命令：[/bold]\n"
        "  /help          显示此帮助\n"
        "  /reset         清空对话历史\n"
        "  /model         显示当前模型\n"
        "  /model <名称>  在对话中切换模型\n"
        "  /tokens        显示 token 用量\n"
        "  /compact       压缩对话上下文\n"
        "  /diff          显示本次会话修改的文件\n"
        "  /save          将会话保存到磁盘\n"
        "  /sessions      列出已保存的会话\n"
        "  quit           退出 CoreCoder\n"
        "\n"
        "[bold]输入：[/bold]\n"
        "  Enter          提交消息\n"
        "  Esc+Enter      插入换行（用于粘贴代码）",
        title="CoreCoder 帮助",
        border_style="dim",
    ))


def _brief(kwargs: dict, maxlen: int = 80) -> str:
    s = ", ".join(f"{k}={repr(v)[:40]}" for k, v in kwargs.items())
    return s[:maxlen] + ("..." if len(s) > maxlen else "")
