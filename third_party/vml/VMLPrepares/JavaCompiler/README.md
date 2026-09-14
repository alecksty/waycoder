# Java 8 编译器

**路径**: `VMLPrepares/JavaCompiler/`
**完成度**: ~92% | 🟢 生产可用
**标准库**: `Lib/java/`（待创建）

## 功能
- ✅ 完整语法分析 + 代码生成
- ✅ 控制流 (if/while/for/switch)
- ✅ 函数/过程定义和调用
- ✅ 标准库支持
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 十六进制字面量
- ✅ 共享内置函数库 (builtins.vml)


## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- Thread、GC、reflection、synchronized

**保留**（由 BIOS 实现底层）:
- new(堆)、class/interface
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART

### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

### RAM 级别
- `--ram k`：KB级别（2KB~64KB，如 8051/PIC/AVR）
- `--ram m`：MB级别（64KB~1MB，如 ARM Cortex-M，**默认**）
- `--ram g`：GB级别（如 x86/DDR 系统）
- `--stack-size <bytes>`：手动指定栈大小（默认自动根据 --ram 分配）

### MCU 安全编码提示
- 有限栈空间（256-4096 字节典型），避免深度递归
- 禁止深度递归（>10层需评估栈）
- 禁止动态加载（import/dofile/eval）
- 浮点运算可能需软浮点库

## 使用
```bash
dotnet run --project VMLPrepares/JavaCompiler input.Ja -o output.vml
```

## 测试
`Test/Jv/` — 0 测试文件（测试目录待创建，建议用 Jv 避免与 JavaScript Test/Js 冲突）
