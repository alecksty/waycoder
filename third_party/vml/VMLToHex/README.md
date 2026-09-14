# VMLToHex — VML 汇编转烧录工具

源码 / VML / 目标汇编 → **HEX / BIN / ELF / EXE / COM / S19 / JVM / .NET**

## 完整工作流

```
源文件 (.c/.pas/.go/...)
    ↓ 编译器 (自动检测22种语言)
VML 中间表示 (.vml)
    ↓ 翻译器 (18种目标架构)
目标平台汇编
    ↓ 汇编器 (内联编码)
二进制机器码
    ↓ 格式输出器
HEX / BIN / ELF / EXE / COM / S19 —> 烧录/部署
```

## 用法

### 一键编译+翻译+汇编

```bash
# C源码 → 6502 HEX
vml2hex main.c -arch 6502

# Pascal → Z80 HEX
vml2hex main.pas -arch z80 --lang pascal

# Go → ARM-CM BIN
vml2hex main.go -arch arm-cm -f bin
```

### VML → 目标汇编 → 烧录格式

```bash
# VML → 8051 HEX
vml2hex program.vml -arch 8051

# VML → x86 BIN
vml2hex program.vml -arch x86 -f bin
```

### 直接汇编 .s 文件

```bash
vml2hex output.6502.s -arch 6502
vml2hex output.z80.s -arch z80 -o firmware.hex
```

### 纯 VMB 格式转换（不翻译）

```bash
vml2hex program.vmb                   # VMB → HEX
vml2hex program.vml -f bin            # VML → BIN
vml2hex program.vmb -f dump           # 十六进制转储
```

## 参数

| 参数 | 说明 |
|------|------|
| `-arch <arch>` | 目标架构（必选，编译/翻译时） |
| `-f <format>` | 输出格式，默认 `hex` |
| `-o <file>` | 输出文件路径 |
| `-a <addr>` | 基地址（十六进制，如 `8000`） |
| `--lang <lang>` | 指定源码语言 |

## 支持的架构 (18种)

| 架构 | 说明 | 字长 |
|------|------|:----:|
| `6502` | MOS Technology 6502 | 8位 |
| `z80` | Zilog Z80 | 8位 |
| `8051` | Intel 8051 | 8位 |
| `avr` | Atmel AVR (ATmega) | 8位 |
| `pic` | Microchip PIC (mid-range) | 8位 |
| `arm-cm` | ARM Cortex-M (Thumb) | 32位 |
| `x86` | Intel x86 (32-bit) | 32位 |
| `68000` | Motorola 68000 | 32位 |
| `mips` | MIPS (32-bit) | 32位 |
| `riscv` | RISC-V (32-bit) | 32位 |
| `sparc` | SPARC (32-bit) | 32位 |
| `powerpc` | PowerPC (32-bit) | 32位 |

## 输出格式 (9种)

| 格式 | 说明 | 典型用途 |
|------|------|----------|
| `hex` | Intel HEX | 通用烧录器、编程器 |
| `bin` | 原始二进制 | 裸片烧录、固件更新 |
| `elf` | ELF64 可执行文件 | Linux 系统 |
| `exe` | DOS MZ 可执行文件 | DOS 系统 |
| `com` | CP/M COM 文件 | CP/M 系统 |
| `s19` | Motorola S-Record | 老式编程器 |
| `dump` | 十六进制转储 + ASCII | 调试、逆向 |
| `jvm` / `java` | Java .class 字节码 | JVM 运行时直接执行 |
| `dotnet` / `net` | .NET PE 程序集 | .NET 运行时直接执行 |

## 示例

```bash
# 编译C代码并转换为6502 HEX（烧录到EPROM）
vml2heat main.c -arch 6502 -a 8000 -o firmware.hex

# 编译Pascal到8051，输出Intel HEX
vml2heat main.pas -arch 8051 --lang pascal

# VML直接转ARM Cortex-M二进制
vml2heat program.vml -arch arm-cm -f bin

# 翻译后汇编为DOS EXE
vml2heat program.vml -arch x86 -f exe

# Motorola S-Record格式（老式编程器）
vml2heat program.vml -arch 68000 -f s19

# VML → JVM .class（Java 运行时执行）
vml2heat program.vml -f jvm -o VMLProgram.class

# VML → .NET PE（.NET 运行时执行）
vml2heat program.vml -f dotnet -o VMLProgram.exe
```

## 项目结构

```
VMLToHex/
├── Program.cs                    # 主入口 + CLI
├── Assemblers/
│   ├── BaseAssembler.cs          # 抽象基类
│   ├── AssemblerFactory.cs       # 工厂
│   ├── Assembler6502.cs ..       # 12个架构实现
│   └── OutputFormat.cs           # 格式输出器
├── Formatters/
│   ├── JvmClassWriter.cs         # JVM .class 生成器
│   └── DotNetPeWriter.cs         # .NET PE 生成器
└── README.md
```
