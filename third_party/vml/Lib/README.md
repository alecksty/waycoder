# Lib — VML 标准库

## 脚本

| 脚本 | 平台 | 用途 |
|---|---|---|---|
| `publish_tools.sh` / `publish_tools.ps1` | macOS+Linux / Windows | 编译发布 VMLTool / GenLib / GenDyn / GenDev 到 `tools/bin/<rid>/` |
| `build_libs.sh` | macOS / Linux | 从 C 源码编译所有 `.vml` 库文件，生成各语言 `builtin.vml` / `stdlib.vml` / `vmllib.vml` |
| `MakeDevice.sh` / `MakeDevice.ps1` | macOS+Linux / Windows | 遍历 Devices/ XML 为 22 种语言生成 MCU 设备头文件（内部调用 DeviceCodeGenerator） |
| `build_static.sh` / `build_static.ps1` | macOS+Linux / Windows | 用户 C 源码 → `.vml` + 22 语言静态绑定（内部调用 VMLTool C 编译器） |
| `build_dynamic.sh` / `build_dynamic.ps1` | macOS+Linux / Windows | 用户 C 源码 → `.dylib`/`.dll` + 22 语言 FFI 绑定（内部调用 GenDyn 工具） |
| `cleanup.sh` | macOS / Linux | 删除所有编译产物 `.vml`（保留手写的 `Lib/vml/Bios/`、`Lib/vml/Device/` 等） |

### build_libs.sh 用法

```bash
./build_libs.sh             # 全部编译（shared + C 标准库 + 各语言聚合文件）
./build_libs.sh --shared    # 仅编译共享库模块 (shared/src/*.c → shared/*.vml)
./build_libs.sh --c         # 仅编译 C 标准库 (c/*.c → c/*.vml)
./build_libs.sh --lang      # 仅生成各语言 builtin/stdlib/vmllib
```

### build_static 用法 — 用户 C 源码 → VML 静态绑定

```bash
./build_static.sh myui.c                    # 编译+生成所有 22 语言绑定
./build_static.sh myui.c -o libgfx          # 指定输出库名
./build_static.sh myui.c --lang basic,pascal # 仅生成指定语言
```

生成文件：`shared/<name>.vml` + `c/<name>.h` + 16 语言 `<lang>/ext/<name>.<ext>`

### build_dynamic 用法 — 用户 C 源码 → 动态库 FFI 绑定

```bash
./build_dynamic.sh myui.c                    # 编译 .c → .dylib + 全部 FFI 绑定
./build_dynamic.sh myui.c --lang-only        # 仅生成 22 语言头文件
./build_dynamic.sh myui.h -l libmyui.dylib   # 从已有头文件+动态库生成
```

内部调用 `GenDyn` 工具，生成：C 胶水代码 (`<lib>_lib.c`) + VML 封装 (`<lib>_lib.vml`) + 22 语言 FFI 头文件。

## 相关 .NET 工具

| 工具 | 项目路径 | 用途 |
|---|---|---|
| **GenLib** | `tools/GenLib/` | VML 标准共享库管理：扫描 `Lib/shared/src/*.c`，编译 C→VML，生成 22 语言绑定 |
| **GenDyn** | `tools/GenDyn/` | 外部动态库 FFI 绑定：解析 `.dylib`/`.so`/`.dll` 导出符号，生成 C 胶水代码 + VML 封装 + 22 语言 FFI 头文件 |

```bash
# GenLib — 管理 VML 标准共享库
dotnet run --project tools/GenLib -- -s            # 扫描 Lib/shared/src/*.c 中所有导出函数
dotnet run --project tools/GenLib -- -g -l python  # 生成 Python 绑定文件
dotnet run --project tools/GenLib -- -b            # 增量编译过期 .vml
dotnet run --project tools/GenLib -- -A            # 一键全流程

# GenDyn — 外部动态库 FFI 绑定
dotnet run --project tools/GenDyn -- -s            # 扫描动态库导出符号
dotnet run --project tools/GenDyn -- -A -n mylib   # 一键全流程
dotnet run --project tools/GenDyn -- -g -l python -n mylib  # 仅生成 Python 绑定
```

## 三层库架构

```
builtin.vml   — 编译器自动链接，必备运行时
stdlib.vml    — 常用标准库（手动 .linked  "stdlib.vml"）
vmllib.vml    — 完整共享库（手动 .linked  "vmllib.vml"）
```

### builtin.vml — 编译器自动链接

所有语言共享核心层，按语言级别差异化：

| 层级 | 语言 | 包含内容 |
|---|---|---|
| **核心** (所有语言) | 全部 22 种 | builtins, device, sysinfo |
| **条件软浮点** (所有语言) | 全部 | softfloat / softdouble / softint64（按编译选项 `#ifdef` 自动引入）|
| **cdecl 变参** | C, C++ | printf, scanf |
| **标准扩展** | BASIC 以外的 14 种 | float, syscall.inc |
| **全量** | BASIC | string, math, io, file, ctype, bitops, convert, memory, util, readline, vga_text, graphics, browser_gfx |

### stdlib.vml — 常用标准库（手动引入）

继承 builtin，额外包含：string, math, io, file, printf, ctype, bitops, convert, memory, util, readline

### vmllib.vml — 完整共享库（手动引入）

继承 stdlib，额外包含：os, network, debug, graphics, vga_text, browser_gfx, softfloat, softint64, softdouble

## 共享库模块 (Lib/shared/)

| 模块 | 源文件 | 函数 |
|---|---|---|
| **builtins** | `shared/src/builtins.c` | PEEK, POKE, putchar, getchar, abs, min, max, random, sleep, alloc, kbhit |
| **device** | `shared/src/device.c` | 设备操作基础（端口读写、MMIO） |
| **sysinfo** | `shared/src/sysinfo.c` | 随机种子、日期时间、系统退出 |
| **float** | `shared/src/float.c` | 基础浮点操作 |
| **syscall.inc** | (手写) | 系统调用常量 (SYS\_\* / CFG\_\*) |
| **printf** | `shared/src/printf.c` | printf, sprintf, snprintf, vsnprintf（cdecl 变参） |
| **scanf** | `shared/src/scanf.c` | scanf, sscanf, vsscanf（cdecl 变参） |
| **string** | `shared/src/string.c` | strlen, strcpy, strcmp, strcat, strncpy, strncmp, strchr |
| **math** | `shared/src/math.c` | sin, cos, tan, sqrt, pow, log, exp 等 |
| **io** | `shared/src/io.c` | getchar, putchar, puts, gets |
| **file** | `shared/src/file.c` | fopen, fclose, fread, fwrite, fseek, ftell, fsize |
| **ctype** | `shared/src/ctype.c` | isalpha, isdigit, isspace, isupper, islower, toupper, tolower |
| **bitops** | `shared/src/bitops.c` | AND, OR, XOR, NOT, SHL, SHR（位运算） |
| **convert** | `shared/src/convert.c` | atoi, itoa, ftoa（类型转换） |
| **memory** | `shared/src/memory.c` | 内存管理扩展 |
| **util** | `shared/src/util.c` | 工具函数 |
| **readline** | `shared/src/readline.c` | 控制台读行 |
| **vga_text** | `shared/src/vga_text.c` | VGA 文本模式 |
| **graphics** | `shared/src/graphics.c` | VGA 绘图 |
| **browser_gfx** | `shared/browser_gfx.c` | 浏览器图形（WASM Canvas） |
| **os** | `shared/src/os.c` | OS 模式扩展（进程/线程，仅 --mode os） |
| **network** | `shared/src/network.c` | 网络函数 |
| **debug** | `shared/src/debug.c` | 调试函数 |
| **softfloat** | `shared/src/softfloat.c` | 软件 float32 模拟（Q15.16 定点） |
| **softdouble** | `shared/src/softdouble.c` | 软件 float64 模拟（IEEE 754 双精度） |
| **softint64** | `shared/src/softint64.c` | 软件 int64 模拟 |

## 编译模式

- **库文件**（无 `main` 函数）：自动识别，不链接任何库，仅编译源文件自身
- **程序文件**（有 `main` 函数）：自动链接 `builtin.vml`
- **强制不链接**：`--no-link` 参数

## #ifdef 条件编译

编译器自动传递以下宏定义，`builtin.vml` 用 `#ifdef` 按条件引入：

| 宏 | 来源 |
|---|---|
| `VML_C`, `VML_BASIC`, `VML_PASCAL`, ... | 语言类型 |
| `VML_FLOAT32_SOFT` | `-float32 soft` |
| `VML_FLOAT64_SOFT` | `-float64 soft` |
| `VML_INT64_SOFT` | `-int64 soft` |
