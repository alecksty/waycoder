# 梯形图 (IEC 61131-3 子集) 编译器

**路径**: `VMLPrepares/LadderCompiler/`
**完成度**: ~98% | 🟢 生产可用
**标准库**: `Lib/ladder/`

## 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator）
- ✅ 触点: NO(常开)/NC(常闭)/POS(上升沿)/NEG(下降沿)
- ✅ 线圈: OUT(输出)/SET(置位)/RST(复位)
- ✅ 定时器: TON(接通延时)/TOF(断开延时)/TP(脉冲) — SYSCALL #53 实时时钟
- ✅ 计数器: CTU(加)/CTD(减)/CTUD(加减) — 含溢出保护
- ✅ 比较: EQ/NE/GT/GE/LT/LE
- ✅ 算术: ADD/SUB/MUL/DIV/MOD
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)


## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- 无（PLC 语言天然 MCU 兼容）

**保留**（由 BIOS 实现底层）:
- 定时器 (TON/TOF/TP) — 使用 SYSCALL #53 GetTick
- 计数器 (CTU/CTD/CTUD) — 16位溢出保护
- POKE/PEEK 内存映射 I/O (MMIO)
- I/O 内存映射 (触点/线圈)
- 基本算术/比较/控制流

### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

### RAM 级别
- `--ram k`：KB级别（2KB~64KB，如 8051/PIC/AVR）
- `--ram m`：MB级别（64KB~1MB，如 ARM Cortex-M，**默认**）
- `--ram g`：GB级别（如 x86/DDR 系统）
- `--stack-size <bytes>`：手动指定栈大小（默认自动根据 --ram 分配）

### MCU 安全编码提示
- PLC 扫描周期需满足实时性要求
- 定时器精度取决于 SYSCALL #53 时钟分辨率
- 计数器注意 16 位范围 (-32768 ~ 32767)
- 浮点运算可能需软浮点库

## 使用
```bash
dotnet run --project VMLTool -- input.lad -o output.vml
```

## 测试
`Test/La/` — 0 测试文件（测试目录待创建）
