
![alt text](images/logo.png)
# VML 工具链

> **当前版本: v1.66.67** | 🔧 35 个项目 · 0 错误 0 警告 | **22/22语言** | **翻译器 18/18 补全** | **Fuzz 26(22语言)** | **BASIC 10方言+OOP | Pascal 6方言+OOP** | **GenDev 23/23 | Pascal unit 系统 | 增量编译** | **ISA v1.10 / SYSCALL v2.7** | **vmlsys.c 共享库 | asm C/OC/C++专属** | **#import预处理 | 链接器前缀剥离** |
>
> [更新日志](CHANGELOG.md) | [开发者指南](AGENTS.md) | [完成度仪表板](COMPLETION_DASHBOARD.md) | [文档目录](docs/)

## 项目简介

VML（Virtual Machine Language）是一种虚拟机汇编语言，本项目实现了一个完整的"多前端 + 统一 IR + 多后端"编译器工具链。

### 核心组件

- **22 语言编译器** → VML 汇编 (全部 🟢 生产可用)
- **18 后端翻译器** → 目标架构汇编 (6502/Z80/8051/AVR/PIC/MSP430/PIC24/ARM-CM/X86/MIPS/RISC-V/68000/PowerPC/SPARC/JVM/.NET/Wasm)
- **VML 汇编器** — VML 文本 ↔ VMB 二进制
- **C# 运行时** — 完整 CPU 模拟 + MMIO + 50+ SYSCALL
- **C 跨平台运行时** — vmlrun, 107/107 opcodes + VMB v2 + 64位完整支持 + 自加载 EXE
- **EXE 打包** — VML → VMB → 独立可执行文件 (vmlrun + VMB 尾部追加)
- **控制台/VGA 全设备模拟器** — 运行和调试 VML 程序
- **全语言测试** — `test_start_all.ps1` 一键测试 22 语言编译运行

### 🆕 v1.66.26 新特性

**全语言 & 测试**
- **全语言编译运行测试 (22/22)**: 逐个编译打包运行 22 语言 start.*, 全部通过
- **SQLite 3 完整编译通过**: 390 万行 amalgamation → 5.4M 字符预处理 → 540K tokens → 42K VML 指令, MCU 模拟器运行成功
- **NBSDGAMES 23/23 全部通过**: battleship-mines-sudoku 等 23 个游戏

**编译器增强**
- **匿名 struct 数组声明、复杂函数指针 cast**: 支持 `void(*(*)(void*,const char*))(void)` 和未知 POSIX 类型
- **预处理器缩进 # 指令、#undef 函数式宏**: 修复 tab/空格缩进指令和函数式宏移除
- **`__LINE__`/`__FUNCTION__`/`__func__` 宏**: 动态替换、行号追踪
- **数组维度常量表达式、一元 + 运算符、typedef 增强**
- **错误恢复增强**: 级联失败从数百个 → ~15 个

**性能 & 基础设施**
- **Lexer O(n²)→O(1)**: 540K tokens 扫描速度指数级提升
- **C 运行时完善**: 64位位运算 (ANDL/ORL/XORL/NOTL/SHLL/SHRL) + OpCode 同步 107/107
- **链接器跨库引用修复**: 全局标签映射 + 自动别名
- **VMB INDIRECT 编码**: 间接寻址支持 TAG_MEM register-relative

**多语言修复**
- **C#/Java 输出修复**: Console.WriteLine/println 调用链栈帧偏移
- **Scheme print 修复**: 字符串输出动态判断
- **D asm() 修复**: 内联汇编 OpCode.ASM
- **Go Lexer 同步优化**: GetLine() O(1) + ReadIdentifier/ReadNumber Substring

### 22 种语言编译器

| 语言 | 状态 | 编译器 | 标准库 | 方言 |
|------|:----:|--------|:------:|------|
| C (C99) | 🟢 生产可用 | CCompiler | Lib/c/ (34 文件) + SQLite | — |
| BASIC | 🟢 生产可用 | BasicCompiler | Lib/basic/ | 10 方言 (QBasic/TurboBasic/FreeBasic/TrueBasic/PureBasic/ChipBasic/MiniBasic/GW-BASIC/PowerBASIC/VisualBasic) |
| Pascal | 🟢 生产可用 | PascalCompiler | Lib/pascal/ | 6 方言 (Turbo Pascal/Delphi/FreePascal/ISO/UCSD/Oberon) |
| Python | 🟢 生产可用 | PythonCompiler | Lib/python/ | — |
| Lua | 🟢 生产可用 | LuaCompiler | Lib/lua/ | — |
| Forth | 🟢 生产可用 | ForthCompiler | Lib/forth/ | — |
| Rust | 🟢 生产可用 | RustCompiler | Lib/rust/ | — |
| Go | 🟢 生产可用 | GoCompiler | Lib/go/ | — |
| Ladder | 🟢 生产可用 | LadderCompiler | Lib/ladder/ | — |
| C# | 🟢 生产可用 | CSharpCompiler | Lib/csharp/ | — |
| Java | 🟢 生产可用 | JavaCompiler | Lib/java/ | — |
| JavaScript | 🟢 生产可用 | JavaScriptCompiler | Lib/javascript/ | — |
| Swift | 🟢 生产可用 | SwiftCompiler | Lib/swift/ | — |
| C++98 | 🟢 生产可用 | CppCompiler | Lib/cpp/ | — |
| Kotlin | 🟢 生产可用 | KotlinCompiler | Lib/kotlin/ | — |
| Scheme | 🟢 生产可用 | SchemeCompiler | Lib/scheme/ | — |
| Ruby | 🟢 生产可用 | RubyCompiler | Lib/ruby/ | — |
| Dart | 🟢 生产可用 | DartCompiler | Lib/dart/ | — |
| ObjC | 🟢 生产可用 | ObjCCompiler | Lib/objc/ | — |
| R | 🟢 生产可用 | RCompiler | Lib/r/ | — |
| D | 🟢 生产可用 | DCompiler | Lib/d/ | — |
| Fortran | 🟢 生产可用 | FortranCompiler | Lib/fortran/ | — |

> **全部 22/22 🟢 生产可用** | 预处理: 20/22 | CLI -D/-U: 20/22 | 每语言 35 个 .vml 库文件
>
> **方言系统**: BASIC 10 方言 + Pascal 6 方言，全部 ≥90% 完成度。通过 `--basictype` / `--pascaltype` CLI 选项切换，每方言自动注入预定义宏 (`__QBASIC__` / `__FREEBASIC__` / `__TURBOPASCAL__` 等)。详见 [BASIC 方言规范](VMLPrepares/BasicCompiler/BASIC_LANGUAGE_SPEC.md) / [Pascal 关键字清单](VMLPrepares/PascalCompiler/ALL_KEYWORDS.md)。

### 方言系统

VML 的 BASIC 和 Pascal 编译器支持多种历史方言，通过 CLI 选项切换：

**BASIC — 10 方言** (`--basictype`)

| 方言 | 宏定义 | 完成度 | 关键字 | 特色 |
|------|--------|:------:|:------:|------|
| **QBasic** (默认) | `__QBASIC__` | 92% | ~70 | 图形/游戏 (GORILLAS/NIBBLES) |
| **TurboBasic** | `__TURBOBASIC__` | 90% | ~68 | 结构化编程 (DO/LOOP/FUNCTION) |
| **FreeBasic** | `__FREEBASIC__` | 90% | ~76 | CLASS/OOP/ENUM/PTR/CAST |
| **TrueBasic** | `__TRUEBASIC__` | 90% | ~62 | ANSI/ISO 标准, MAT/ZER/CON |
| **PureBasic** | `__PUREBASIC__` | 90% | ~68 | PROCEDURE/INTERFACE/NEW |
| **ChipBasic** | `__CHIPBASIC__` | 90% | ~57 | MCU GPIO (PINMODE/DIGITALWRITE/DIGITALREAD) |
| **MiniBasic** | `__MINIBASIC__` | 90% | ~15 | 教学最小子集 |
| **GW-BASIC** | `__GWBASIC__` | 90% | ~70 | BLOAD/BSAVE/DEF FN/行号 |
| **PowerBASIC** | `__POWERBASIC__` | 90% | ~70 | REGISTER/FASTPROC/THREADED |
| **VisualBasic** | `__VISUALBASIC__` | 90% | ~75 | Private/Public/With/Optional/ParamArray |

> **99 关键字清单**, 81 已实现 (82%) · CLASS/CONSTRUCTOR/METHOD 代码生成完成 (Phase 1+2) · ENUM 值代入 · GPIO 跨平台运行时库

**Pascal — 6 方言** (`--pascaltype`)

| 方言 | 宏定义 | 完成度 | 语句数 | 特色 |
|------|--------|:------:|:------:|------|
| **Turbo Pascal** (默认) | `__TURBOPASCAL__` | 92% | 72/78 | object 旧式OOP/absolute/asm |
| **Delphi** | `__DELPHI__` | 90% | 85/95 | class/constructor/property/try/for-in |
| **FreePascal** | `__FREEPASCAL__` | 92% | 87/95 | 最完整, overload/default参数 |
| **ISO Pascal** | `__ISOPASCAL__` | 92% | 48/52 | ANSI 标准最小子集 |
| **UCSD Pascal** | `__UCSDPASCAL__` | 93% | 50/54 | unit/uses 模块系统 |
| **Oberon** | `__OBERON__` | 91% | 42/46 | MODULE/IMPORT (Wirth 系) |

> **105 代码生成目标**, 87 已实现 (83%) · class/constructor/destructor · for/in 枚举语法 · record/set/file · Crt 单元

```bash
# 使用示例
vmltool input.bas --basictype freebasic -o output.vml     # FreeBasic 方言
vmltool input.pas --pascaltype delphi -o output.vml        # Delphi 方言
```

### 18 个后端翻译器

| 架构 | 字长 | 目标平台 |
|------|:----:|---------|
| 6502 | 8位 | Apple II / C64 / NES |
| Z80 | 8位 | CP/M / ZX Spectrum |
| 8051 | 8位 | STC / AT89 系列 |
| AVR | 8位 | Arduino / ATmega |
| PIC | 8位 | Microchip MCU |
| MSP430 | 16位 | TI 低功耗 MCU |
| PIC24 | 16位 | Microchip 16位 |
| ARM Cortex-M | 32位 | STM32 嵌入式 |
| x86 | 32位 | 通用 PC |
| MIPS | 32位 | 网络设备 / 教学 |
| RISC-V | 32位 | 开源 CPU |
| SPARC | 32位 | 工作站 |
| PowerPC | 32位 | 嵌入式 / 游戏机 |
| 68000 | 32位 | 复古计算 |
| JVM | 32位 | Java 平台 |
| .NET | 32位 | CLR 平台 |
| Wasm | 32位 | Web 浏览器 |

## 快速使用

```bash
# 编译（自动根据扩展名识别语言）
vmltool input.c -o program.vml                  # C → VML
vmltool input.py -o program.vml                 # Python → VML
vmltool input.java -o program.vml               # Java → VML

# 编译 + 链接共享库
vmltool input.c -o program.vml -l parserexp -L Lib/shared

# 运行
vmltool -r program.vml

# VML → HEX 烧录
vmltool -c program.vml -o firmware.hex -t arm-cm

# 构建: 0 错误 0 警告
dotnet build VMLToolchain.sln

# Shell 自动补全
source Scripts/vmltool-completion.bash    # Bash
. Scripts/vmltool-completion.ps1          # PowerShell
```

## MCU/OS 双模式 + 字符串编码

| 模式 | 默认编码 | 字符串类型 | 输出 SYSCALL |
|:-----|:--------|:----------|:------------|
| MCU (默认) | UTF-8 | `.string` | #1 |
| OS | UTF-16LE | `.wstring` | #391 |

**三层字符串**: `.string`(8-bit char*) / `.wstring`(16-bit wchar_t*) / `.ustring`(32-bit char32_t*)

**语言适配**: Java/C#/JS/Kotlin/Swift/Python/Go 在 OS 模式自动使用 `.wstring`，MCU 模式退化为 `.string`

**预定义宏**: `VML_WSTRING` — OS 模式自动 `#define VML_WSTRING 1`

## 共享库

```
Lib/shared/ — 跨语言通用库 (每语言 Lib/<lang>/ 含相同副本)

字符串:    string  wstring  ustring         (三层字符串操作)
字符:      ctype   wchar    uchar           (字符分类+转换)
格式化:    printf  wprintf  uprintf         (格式化输出)
输入:      scanf   wscanf   uscanf          (格式化输入)
输出:      io      printx                   (I/O + 全类型打印)
表达式:    parserexp  parserexpf            (整数/浮点表达式解析)
数学:      math  float  softdouble  softfloat  softint64
系统:      os  file  network  memory  device  convert  ...
```

## 示例

```bash
# 跨语言表达式解析示例 (22 语言)
Examples/<lang>/parserexp_demo.*     # 整数表达式: parserexp("2+3*4") = 14
Examples/<lang>/parserexpf_demo.*    # 浮点表达式: parserexpf("2.5+3*1.5") = 7.0

# SQLite 编译测试 (255K行 C 源文件)
test/c/sqlite/sqlite_r0_test.c       # MCU 模式: open→CREATE→INSERT→SELECT→close
dotnet run --project VMLTool -- test/c/sqlite/sqlite_r0_test.c -o sqlite.vml

# 编译器测试 (22 语言 × 基础测试)
test/compiler_tests/<lang>/           # 每语言 Hello World + 语法测试
```

## 项目结构

```
vml/
├── VMLAssembler/              # VML 汇编器 (.wstring/.ustring 伪指令)
├── VMLTranslators/            # 18 目标架构转译器
├── VMLPrepares/               # 22 语言编译器
│   ├── CompilerBase/          # LexerBase · ParserBase · CodeGeneratorBase · TypedCodeGen<T> · CLikeCodegen · OopCodegen
│   │                          # ExpressionManager · StatementManager · VarMemManager · RegisterManager · Preprocessor
│   ├── CCompiler/             # C99 🟢 生产可用
│   ├── CppCompiler/           # C++98
│   ├── [Basic|Pascal|Python|Lua|Forth|Rust|Go|...]/  # 所有编译器
├── VMLRuntime/                # C# 虚拟机 (SYSCALL #1-394)
├── VMLFast/VMLRuntimeC/       # C 跨平台运行时 (vmlrun)
├── VMLFast/VMLPackerC/        # C 打包工具
├── VMLEmulators/              # ConsoleEmulator + FullDevicesEmulator (Avalonia)
├── VMLTool/                   # CLI 主工具 (vmltool)
├── VMLIde/                    # Avalonia IDE
├── VMLToHex/                  # HEX/ELF/BIN 烧录生成
├── VMLTests/                  # xUnit 测试 (~550 tests, 5 Trait 类别)
├── VMLPlugins/                # 插件接口 + PluginManager
├── tools/                     # GenLib / GenDyn / GenDev
├── Lib/                       # 标准库 (22 语言 × 35 文件 + shared/)
├── Examples/                  # 跨语言示例代码
├── test/compiler_tests/       # 编译器基础测试 (22 语言)
├── docs/                      # ISA/SYSCALL/ROADMAP 等文档
└── VMLToolchain.sln           # 解决方案
```

## 文档

- [指令集规范](docs/VML_ISA_SPEC.md) — ISA v1.5 (.wstring/.ustring)
- [系统调用规范](docs/SYSCALL_SPEC.md) — SYSCALL v2.2 (#391-394)
- [完成度仪表板](COMPLETION_DASHBOARD.md)
- [更新日志](CHANGELOG.md)
- [路线图](docs/ROADMAP.md)
- [开发者指南](AGENTS.md)

## 许可证

MIT License
