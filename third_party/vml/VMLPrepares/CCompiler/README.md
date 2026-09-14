# C 编译器

**路径**: `VMLPrepares/CCompiler/`
**完成度**: ~97% | 🟢 生产可用
**标准库**: `Lib/c/` (29 个文件)

## 功能
- ✅ C99 完整标准 (ISO/IEC 9899:1999)
- ✅ 指针算术、多维数组、struct/union
- ✅ 预处理器 (#include, #define, #ifdef)
- ✅ 内联汇编 (`__chipasm__`)
- ✅ POKE/PEEK 内存操作
- ✅ 完整标准库 (stdio, stdlib, string, math, conio, graphics)
- ✅ 中断/外设支持 (interrupt)


## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- 无（C99无异步/线程/反射）

**保留**（由 BIOS 实现底层）:
- malloc/free、fopen/fread、printf%f、va_list
- POKE/PEEK 内存映射 I/O (MMIO)
- interrupt 关键字（中断服务函数）
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART
- 裸指针（有限制）

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
dotnet run --project VMLPrepares/CCompiler input.c -o output.vml
vmltool input.c -o output.vml
```

## 测试
`Test/c/` — 190+ 测试文件
