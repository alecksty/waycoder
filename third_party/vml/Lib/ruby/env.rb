⚠ SYSCALL 审计（2026-09-27）：本文件用到的号（130/131，环境变量）在本 VM 里**从未实现** ——
全仓只有本文件在用它们，宿主与 VM 都不认 ⇒ **调了什么都不会发生**（静默 no-op）。
要真用得走库函数那条路（如文件操作用 `Lib/shared/file.vml` 的 file_*），别再照抄这里的写法。
# VML 环境变量扩展库 — Ruby (OS 模式)
# 需显式 import env

def get_env(name)
    asm("SYSCALL 130")
end

def set_env(name, value)
    asm("SYSCALL 131")
end
