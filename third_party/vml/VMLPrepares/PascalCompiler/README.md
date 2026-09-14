# Pascal 编译器

**路径**: `VMLPrepares/PascalCompiler/` | **版本**: v1.66.33
**完成度**: ~92% (Turbo Pascal) | 🟢 生产可用 | **6 方言 + OOP 基础**
**标准库**: `Lib/pascal/` | **测试**: 25

## 多方言支持

通过 `--pascaltype` 选项支持 6 种 Pascal 方言:

| 方言 | CLI 值 | OOP | 示例 |
|------|--------|:--:|:--:|
| Turbo Pascal (默认) | `turbo` | ❌ | 2 程序 |
| Delphi | `delphi` | ✅ | 2 程序 |
| Free Pascal | `freepascal` / `fpc` | ✅ | 1 程序 |
| ISO Pascal | `iso` | ❌ | 1 程序 |
| UCSD Pascal | `ucsd` | ❌ | — |
| Oberon | `oberon` | ❌ | — |

```bash
vmltool -x pascal --pascaltype delphi test.pas -o test.vml
```

## OOP 特性 (Delphi/Free Pascal)

| 特性 | 状态 |
|------|:--:|
| class/object | ✅ 字段支持 |
| constructor/destructor | ⚠️ Token+解析 |
| property | ⚠️ Token |
| virtual/override | ⚠️ AST就绪 |
| try/except/finally | ⚠️ Token |

## 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator ~6300行）
- ✅ 控制流 (if/then/else/while/for/repeat-until/case-of)
- ✅ 过程/函数: 值参数/var参数/forward/递归
- ✅ 数据类型: integer/real/char/boolean/string/enum/array/record/set/file
- ✅ break/continue — 已实现
- ✅ 指针: ^type/new/dispose — 已实现
- ✅ Crt 单元: ClrScr/GotoXY/TextColor/Delay/Sound 等
- ✅ 文件 I/O: text/file of type
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)


## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- 无（Turbo Pascal 无异步/线程）

**保留**（由 BIOS 实现底层）:
- dispose(堆)、文件I/O
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
- 禁止动态加载
- 浮点运算可能需软浮点库

## 使用
```bash
dotnet run --project VMLTool -- input.pas -o output.vml
```

## 测试
`Test/Pa/` — 0 测试文件（测试目录待创建）
