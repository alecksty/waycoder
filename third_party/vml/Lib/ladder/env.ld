# VML 环境变量扩展库 — Ladder (OS 模式)
# 需显式 include env.ld

[GET_ENV]
SYSCALL 360
RET

[SET_ENV]
SYSCALL 361
RET

[GET_ARGS]
SYSCALL 362
RET
