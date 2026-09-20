# VMLTool 子工程说明

## 功能描述
VMLTool 是一个命令行工具，用于操作 VML (Virtual Machine Language) 程序，包括汇编、转译、编译和运行等功能。

## 目录结构
```
VMLTool/
├── Program.cs         # 程序主入口
└── VMLTool.csproj     # 项目文件
```

## 核心功能
1. **汇编**：将 VML 汇编代码汇编成 VML 程序
2. **转译**：将 VML 程序转译为目标架构的汇编代码
3. **编译**：将 C 代码编译为 VML 程序
4. **运行**：运行 VML 程序
5. **交互式 REPL**：提供交互式命令行环境

## 命令行选项
VMLTool 使用 GCC 风格的命令行标记（flag-based）格式，不再支持旧的子命令格式。

### 操作标志
- `-c`：编译源代码到 VML（也可直接指定输入文件，自动检测）
- `-S`：编译到目标汇编（需配合 `-t`）
- `-E`：仅预处理
- `-a, --assemble`：汇编 VML 代码
- `-T, --translate`：转译到目标架构
- `-k, --link`：链接多个 VML 文件
- `-r, --run`：运行 VML 程序
- `-e, --exe`：打包为独立可执行文件
- `-i, --repl`：启动交互式 REPL

### 通用选项
- `-o, --output <file>`：指定输出文件
- `-x <lang>`：指定源语言（auto=c/bas/py/lua 等）
- `-t, --target <arch>`：指定目标架构（6502/z80/x86/arm-cm/avr/mips/riscv 等）
- `-f, --format <fmt>`：输出格式（hex/elf/bin/exe/com/s19/dump）
- `-O0, -O1, -O2`：优化级别
- `-d, --dump <file>`：输出详细指令 dump
- `-K, --save-temps`：保留中间汇编文件

### GCC 兼容选项
- `-D <macro[=value]>`：定义宏
- `-U <macro>`：取消宏定义
- `-I, --include <path>`：添加 include 路径
- `-L, --library <path>`：添加库路径
- `-std=<standard>`：指定语言标准
- `-g`：调试模式
- `-Wall, -Werror`：警告选项
- `-static, -shared`：链接模式

### MCU/OS 选项
- `-m, --mode mcu|os`：目标模式（默认 MCU）
- `-mr, --ram k|m|g`：内存大小
- `-ss, --stack-size <bytes>`：栈大小
- `--soft-float, --sf`：软件浮点数
- `--no-float, --nf`：禁用浮点数
- `--int64 <mode>`：int64 模式

### 其他
- `-h, --help`：显示帮助
- `-v, -V, --version`：显示版本
- `--verbose`：详细输出
- `-P, --plugins`：列出已加载插件

## 支持的目标架构
- 6502
- 8051
- ARM-CM
- MIPS
- RISC-V
- x86
- Z80

## 待改进的点
1. **功能扩展**：添加更多命令和功能
2. **错误处理**：提供更详细的错误信息
3. **性能优化**：提高处理速度
4. **文档**：完善文档和使用说明
5. **测试**：添加更多测试用例

## 示例用法
```bash
# 编译 C 代码到 VML（根据扩展名自动检测语言）
vmltool main.c -o program.vml

# 编译 BASIC 代码
vmltool main.bas -o program.vml

# 指定语言编译
vmltool main.txt -x c -o program.vml

# 编译 + 翻译 + 汇编 + 输出 HEX（全流水线）
vmltool -c main.c -o output.hex -t arm-cm -f hex

# 从 .vml 直接构建（自动链接 BIOS + 翻译 + 汇编）
vmltool program.vml -o output.hex -t avr

# 仅编译到目标汇编（不汇编）
vmltool -c main.c -o output.s -t x86

# 预处理 C 代码
vmltool -E main.c -o main.i

# 汇编 VML 代码
vmltool -a input.vml -o output.bin

# 转译 VML 到目标架构汇编
vmltool -T input.vml -o output.asm -t riscv

# 运行 VML 程序
vmltool -r program.vml

# 打包为独立可执行文件
vmltool -e program.vml -o program.exe

# 多文件编译
vmltool main.c lib.c -o program.vml

# 启动 REPL
vmltool -i

# 显示帮助
vmltool --help
```

> **注意：** 旧的子命令格式（如 `vmltool compile input.c output.vml`）已不再支持。
> 请使用标记格式（如 `vmltool input.c -o output.vml` 或 `vmltool -c input.c -o output.vml`）。

## 交互式 REPL
在 REPL 模式下，您可以直接输入 VML 指令并立即执行：

```
VML 交互式 REPL
输入 'exit' 退出
> MOVE @R0, #42
> MOVE @R1, #0xB8000
> MOVE [@R0], @R1
> HALT
```
