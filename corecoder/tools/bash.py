"""带安全检查的 Shell 命令执行。

Claude Code 的 BashTool 有 1,143 行。这是提炼后的版本：
- 带截断的输出捕获（保留头尾）
- 超时支持
- 危险命令检测
- 工作目录跟踪（cd 感知）
"""

import os
import re
import subprocess
import threading

from .base import Tool

# 跨命令跟踪 cwd（Claude Code 也是这样做的）。线程本地存储，
# 这样当智能体并行执行工具时，两个 bash 调用不会在共享全局变量上
# 产生竞态。每个工作线程携带自己的 cwd。详见文章 05。
_local = threading.local()

# 可能破坏文件系统或泄露密钥的危险模式
_DANGEROUS_PATTERNS = [
    # 针对根目录/家目录的递归删除（force 标志可选）
    (r"\brm\s+(-\w*)?-r\w*\s+(/|~|\$HOME)", "对家目录/根目录的递归删除"),
    # 同时包含递归（-r/-R）和强制（-f）标志，任意顺序或间距
    (r"\brm\b(?=(?:.*\s)?-\w*[rR])(?=(?:.*\s)?-\w*f)", "强制递归删除"),
    # 同上，但使用长格式标志
    (r"\brm\b.*--recursive\b.*--force\b|\brm\b.*--force\b.*--recursive\b", "强制递归删除"),
    (r"\bmkfs\b", "格式化文件系统"),
    (r"\bdd\s+.*of=/dev/", "原始磁盘写入"),
    (r">\s*/dev/sd[a-z]", "覆盖块设备"),
    (r"\bchmod\s+(-R\s+)?777\s+/", "对根目录 chmod 777"),
    (r":\(\)\s*\{.*:\|:.*\}", "fork 炸弹"),
    (r"\bcurl\b.*\|\s*(sudo\s+)?(ba)?sh\b", "curl 管道到 shell"),
    (r"\bwget\b.*\|\s*(sudo\s+)?(ba)?sh\b", "wget 管道到 shell"),
]


class BashTool(Tool):
    name = "bash"
    description = (
        "执行 Shell 命令。返回 stdout、stderr 和退出码。"
        "用于运行测试、安装包、git 操作等。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "command": {
                "type": "string",
                "description": "要运行的 Shell 命令",
            },
            "timeout": {
                "type": "integer",
                "description": "超时时间，单位秒（默认 120）",
            },
        },
        "required": ["command"],
    }

    def execute(self, command: str, timeout: int = 120) -> str:
        # 安全检查
        warning = _check_dangerous(command)
        if warning:
            return f"⚠ 已阻止：{warning}\n命令：{command}\n如有意执行，请修改命令使其更具体。"

        # 使用当前线程自己的跟踪工作目录
        cwd = getattr(_local, "cwd", None) or os.getcwd()

        try:
            proc = subprocess.run(
                command,
                shell=True,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=timeout,
                cwd=cwd,
            )

            # 跟踪 cd 命令，使下一条命令在正确的位置运行
            if proc.returncode == 0:
                _update_cwd(command, cwd)
            out = proc.stdout
            if proc.stderr:
                out += f"\n[stderr]\n{proc.stderr}"
            if proc.returncode != 0:
                out += f"\n[退出码：{proc.returncode}]"
            # 保留头尾以保留最有用的信息
            if len(out) > 15_000:
                out = (
                    out[:6000]
                    + f"\n\n... 已截断（共 {len(out)} 字符）...\n\n"
                    + out[-3000:]
                )
            return out.strip() or "（无输出）"
        except subprocess.TimeoutExpired:
            return f"错误：在 {timeout} 秒后超时"
        except Exception as e:
            return f"运行命令时出错：{e}"


def _check_dangerous(cmd: str) -> str | None:
    """如果命令看起来具有破坏性，返回警告字符串；否则返回 None。"""
    for pattern, reason in _DANGEROUS_PATTERNS:
        if re.search(pattern, cmd):
            return reason
    return None


def _update_cwd(command: str, current_cwd: str):
    """跟踪 cd 命令导致的目录变更，按线程隔离。"""
    # 遍历 && 链中的每个 cd，将相对目标路径基于前一个 cd
    # 到达的目录（而非原始 cwd）进行解析，
    # 这样 `cd a && cd b` 最终会停在 a/b
    running = current_cwd
    changed = False
    for part in command.split("&&"):
        part = part.strip()
        if part.startswith("cd "):
            target = part[3:].strip().strip("'\"")
            if target:
                new_dir = os.path.normpath(os.path.join(running, os.path.expanduser(target)))
                if os.path.isdir(new_dir):
                    running = new_dir
                    changed = True
    if changed:
        _local.cwd = running
