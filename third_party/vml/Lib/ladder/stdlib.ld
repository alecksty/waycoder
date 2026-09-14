# 梯形图标准库 - 基于IEC 61131-3标准

# 基本逻辑函数
LD X0  # 常开触点
LDI X1 # 常闭触点
OUT Y0 # 线圈输出

# 定时器函数
TON T0 K100  # 接通延时定时器
TOF T1 K50   # 断开延时定时器
TP T2 K20    # 脉冲定时器

# 计数器函数
CTU C0 K10   # 加计数器
CTD C1 K5    # 减计数器
CTUD C2 K20  # 加减计数器

# 比较函数
CMP = M0 D0 D1  # 等于比较
CMP > M1 D2 D3  # 大于比较
CMP < M2 D4 D5  # 小于比较
CMP >= M3 D6 D7 # 大于等于比较
CMP <= M4 D8 D9 # 小于等于比较
CMP <> M5 D10 D11 # 不等于比较

# 数学函数
ADD D12 D13 D14  # 加法
SUB D15 D16 D17  # 减法
MUL D18 D19 D20  # 乘法
DIV D21 D22 D23  # 除法

# 位操作函数
AND M6 M7 M8     # 位与
OR M9 M10 M11    # 位或
XOR M12 M13 M14  # 位异或
NOT M15 M16      # 位非

# 移位函数
SHL D24 K3 D25   # 左移
SHR D26 K2 D27   # 右移
ROL D28 K1 D29   # 循环左移
ROR D30 K1 D31   # 循环右移

# 转换函数
INT_TO_REAL D32 D33  # 整数转实数
REAL_TO_INT D34 D35  # 实数转整数
BCD_TO_INT D36 D37   # BCD转整数
INT_TO_BCD D38 D39   # 整数转BCD

# 特殊功能
MOV D40 D41          # 数据传送
JMP L0               # 跳转
CALL F0              # 子程序调用
RET                  # 子程序返回
END                  # 程序结束

# I/O 函数
[GETCHAR]
SYSCALL 5
RET

# 内存函数
[MEMCMP]
SYSCALL 13
RET

[MALLOC]
SYSCALL 40
RET

[FREE]
SYSCALL 41
RET

# 系统函数
[GET_DATETIME]
SYSCALL 54
RET

[exit]
SYSCALL 3
RET
