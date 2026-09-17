# GenLib — VML 共享库管理工具

从 `Lib/shared/src/*.c` 自动提取函数签名，编译 C → VML，为 22 种语言生成模块包装、聚合文件和源文件绑定。

## 快速开始

```bash
# 一键全流程：编译 + 模块包装 + 聚合文件 + 源文件绑定
dotnet run --project tools/GenLib -- -A

# 只编译过期的 C 源文件
dotnet run --project tools/GenLib -- -b

# 只为 Python 生成全部
dotnet run --project tools/GenLib -- -A -l python
```

## CLI

```
genlib <命令> [选项]
```

### 命令

| 短名 | 长名 | 说明 |
|------|------|------|
| `-s` | `--scan` | 扫描 `Lib/shared/src/*.c` 导出函数 |
| `-b` | `--build` | 编译 C 源码 → VML（进程内调用 CCompiler） |
| `-m` | `--modules` | 生成每种语言的模块包装 `.vml` |
| `-a` | `--aggregators` | 生成 `builtin.vml` / `stdlib.vml` / `vmllib.vml` |
| `-g` | `--bindings` | 生成源文件级绑定（`shared.go`, `shared.py` 等） |
| `-n` | `--gen-native` | **🆕 v1.66.60** 生成 native 封装源文件（22 种语言） |
| `-A` | `--all` | 一键全流程（`-b` + `-m` + `-a` + `-g`） |

### 选项

| 短名 | 长名 | 默认值 | 说明 |
|------|------|------|------|
| `-l` | `--lang` | `all` | 目标语言，`all` 表示全部 22 种 |
| `-c` | `--class` | — | **🆕** 类名/模块名（`-n` 按模块过滤函数） |
| `-o` | `--file` | 自动 | **🆕** 输出文件路径（`-n` 模式） |
| `--style` | — | `one_two` | **🆕** 命名风格（`-n` 模式），见下方说明 |
| `--prefix` | — | `false` | **🆕** 是否带语言前缀（`-n` 模式） |
| `-p` | `--package` | 空 | 包名/命名空间（仅 `-g`） |
| `-r` | `--root` | `.` | 项目根目录（需包含 `Lib/`） |
| `-h` | `--help` | — | 显示帮助 |

## Native 代码生成 (`-n/--gen-native`) 🆕 v1.66.60

为每种语言生成可直接使用的 native 封装源文件，将共享库函数声明为语言原生类型。

### 基本用法

```bash
# 为所有语言生成 Math 模块的 native 封装
dotnet run --project tools/GenLib -- -n -c Math

# 只为 C# 生成，使用 PascalCase 命名
dotnet run --project tools/GenLib -- -n -l csharp -c Math --style OneTwo

# 只为 Pascal 生成，使用 One_Two 命名风格
dotnet run --project tools/GenLib -- -n -l pascal -c Math --style One_Two
```

### 命名风格 (`--style`)

| 风格 | 示例 (`int_to_str`) | 说明 |
|------|---------------------|------|
| `one_two` | `int_to_str` | snake_case（默认） |
| `OneTwo` | `IntToStr` | PascalCase |
| `oneTwo` | `intToStr` | camelCase（方法风格） |
| `ONETWO` | `INTTOSTR` | 全大写下划线去除 |
| `One_Two` | `Int_To_Str` | Pascal_Snake |
| `ONE_TWO` | `INT_TO_STR` | SCREAMING_SNAKE |
| `onetwo` | `inttostr` | 全小写下划线去除 |

### 生成示例

**C#** (OOP + native alias):
```csharp
class Math {
    native static int Abs(int n) alias "abs";
    native static float Sqrt(float x) alias "sqrt";
}
```

**Pascal** (external 声明):
```pascal
function Abs(n: Integer): Integer; external 'abs';
function Sqrt(x: Single): Single; external 'sqrt';
```

**Java** (native 方法):
```java
class Math {
    native static int abs(int n);
    native static float sqrt(float x);
}
```

**Kotlin** (external 函数):
```kotlin
object Math {
    external fun abs(n: Int): Int
    external fun sqrt(x: Float): Float
}
```

### 类型映射

C 类型自动映射到各语言的对应类型：

| C 类型 | C# | Java | Pascal | Go | Python | Kotlin |
|--------|----|----|--------|----|--------|--------|
| `int` | `int` | `int` | `Integer` | `int32` | `int` | `Int` |
| `long` | `long` | `long` | `Int64` | `int64` | `int` | `Long` |
| `float` | `float` | `float` | `Single` | `float32` | `float` | `Float` |
| `double` | `double` | `double` | `Double` | `float64` | `float` | `Double` |
| `char` | `char` | `char` | `Char` | `byte` | `str` | `Char` |
| `void` | `void` | `void` | — | — | `None` | `Unit` |
| `const char*` | `string` | `String` | `String` | `string` | `str` | `String` |
| `bool` | `bool` | `boolean` | `Boolean` | `bool` | `bool` | `Boolean` |

### 编译器 native 关键字支持

v1.66.60 同步添加了各编译器对 native/external 声明的支持：

| 语言 | 关键字 | 状态 |
|------|--------|------|
| C/C++ | `extern` | ✅ 已支持（预存） |
| C# | `native ... alias "..."` | ✅ **v1.66.60** |
| Java | `native` | ✅ **v1.66.60** |
| Kotlin | `external` | ✅ **v1.66.60** |
| Pascal | `external` | ✅ 已支持（预存） |
| 其他 17 语言 | 函数体风格 | ✅ 无需特殊关键字 |

## 流水线步骤

### Step 1: 扫描 (`-s`)
扫描 `Lib/shared/src/*.c`，提取所有非静态导出函数及其返回类型、调用约定和源文件。

### Step 2: 构建 (`-b`)
进程内调用 `CCompiler.CompileFile()` 编译每个 `.c` 文件为 `.vml`，自动剥离 `.entry`/`.stack` 等程序级指令。仅当源文件比目标文件更新时才重编译（增量构建）。

### Step 3: 模块包装 (`-m`)
为每个核心模块生成语言特定的包装 `.vml`。根据函数的调用约定（`__cdecl`/`__stdcall`/`__fastcall`）生成正确的参数推栈顺序和堆栈清理代码。

### Step 4: 聚合文件 (`-a`)
生成三个层次化的聚合文件：

| 文件 | 包含内容 |
|------|---------|
| `builtin.vml` | builtins + device + sysinfo + math + system + console（条件编译 softfloat/softdouble/softint64） |
| `stdlib.vml` | builtin + string + time + file + printf + scanf + ctype + bitops + convert + memory + readline |
| `vmllib.vml` | stdlib + os + network + vga_text |

### Step 5: 源文件绑定 (`-g`)
为每种语言生成 `shared.{ext}` 绑定文件，声明共享库函数为该语言的 extern 函数。

### Step 6: Native 封装 (`-n`) 🆕
为每种语言生成类型安全的 native 封装源文件。OOP 语言生成 class 封装，非 OOP 语言生成函数声明。详见上方「Native 代码生成」章节。

## 模块分层：核心 vs 自定义

`modules.json` 中每个模块标记 `core: true/false`：

| 类型 | 示例 | 进入聚合文件 | 使用方式 |
|------|------|-------------|---------|
| core | math, string, time, file, console, system | 是 | `.linked  "math.vml"` 或自动链接 |
| custom | 用户自写模块 | 否 | 仅 `.linked  "xxx.vml"` |

用户自定义模块工作流：
```
1. 写 Lib/shared/src/myapi.c → shared_my_xxx() 函数
2. genlib -b               → 编译为 Lib/shared/myapi.vml
3. genlib -m               → 生成各语言的 Lib/{lang}/myapi.vml
4. 用户代码: .linked  "myapi.vml"
```

## 6 个核心模块

| 模块 | 源文件 | 主要函数 |
|------|--------|---------|
| math | math, float, util | abs, min, max, sqrt, pow, sin, cos, tan, random, clamp |
| string | string | strlen, strcmp, strcpy, strcat, strchr, strstr, strrev |
| time | sysinfo | datetime, sleep, delay, get_tick, get_date, get_time |
| file | file | fopen, fclose, fread, fwrite, fseek, ftell, fsize |
| console | io, printf, scanf, readline | putchar, getchar, print_str, print_int, printf, scanf, sprintf |
| system | builtins, device, memory | peek, poke, alloc, free, memcpy, memset, dev_open, getconfig |

## JSON 配置文件

两个配置文件位于 `Lib/`，缺失时自动生成：

### modules.json
```json
{
  "math": { "source": "math.c", "core": true, "functions": ["shared_abs", ...] },
  "string": { "source": "string.c", "core": true, "functions": ["shared_strlen", ...] },
  "demoapi": { "source": "demoapi.c", "core": false, "functions": ["shared_demo_xxx"] }
}
```

### naming.json
定义每种语言的标签命名风格（snake_lower / SCREAMING_SNAKE / PascalCase_method / kebab-case）和别名前缀。

## 生成文件

| 语言 | 绑定文件 | 模块包装 | 聚合文件 |
|------|---------|---------|---------|
| C | `Lib/c/shared_bindings.h` | `Lib/c/*.vml` | `Lib/c/builtin.vml` 等 |
| C++ | `Lib/cpp/shared_bindings.hpp` | `Lib/cpp/*.vml` | `Lib/cpp/builtin.vml` 等 |
| BASIC | `Lib/basic/shared.bas` | `Lib/basic/*.vml` | `Lib/basic/builtin.vml` 等 |
| Python | `Lib/python/shared.py` | `Lib/python/*.vml` | `Lib/python/builtin.vml` 等 |
| Go | `Lib/go/shared.go` | `Lib/go/*.vml` | `Lib/go/builtin.vml` 等 |
| Rust | `Lib/rust/shared.rs` | `Lib/rust/*.vml` | `Lib/rust/builtin.vml` 等 |
| Java | `Lib/java/shared.java` | `Lib/java/*.vml` | `Lib/java/builtin.vml` 等 |
| C# | `Lib/csharp/shared.cs` | `Lib/csharp/*.vml` | `Lib/csharp/builtin.vml` 等 |
| JavaScript | `Lib/javascript/shared.js` | `Lib/javascript/*.vml` | `Lib/javascript/builtin.vml` 等 |
| Kotlin | `Lib/kotlin/shared.kt` | `Lib/kotlin/*.vml` | `Lib/kotlin/builtin.vml` 等 |
| Lua | `Lib/lua/shared.lua` | `Lib/lua/*.vml` | `Lib/lua/builtin.vml` 等 |
| Pascal | `Lib/pascal/shared.pas` | `Lib/pascal/*.vml` | `Lib/pascal/builtin.vml` 等 |
| Scheme | `Lib/scheme/shared.scm` | `Lib/scheme/*.vml` | `Lib/scheme/builtin.vml` 等 |
| Swift | `Lib/swift/shared.swift` | `Lib/swift/*.vml` | `Lib/swift/builtin.vml` 等 |
| Forth | `Lib/forth/shared.fth` | `Lib/forth/*.vml` | `Lib/forth/builtin.vml` 等 |
| Ladder | `Lib/ladder/shared.ld` | `Lib/ladder/*.vml` | `Lib/ladder/builtin.vml` 等 |
| Ruby | `Lib/ruby/shared.rb` | `Lib/ruby/*.vml` | `Lib/ruby/builtin.vml` 等 |
| Dart | `Lib/dart/shared.dart` | `Lib/dart/*.vml` | `Lib/dart/builtin.vml` 等 |
| ObjC | `Lib/objc/shared.h` | `Lib/objc/*.vml` | `Lib/objc/builtin.vml` 等 |
| R | `Lib/r/shared.r` | `Lib/r/*.vml` | `Lib/r/builtin.vml` 等 |
| D | `Lib/d/shared.d` | `Lib/d/*.vml` | `Lib/d/builtin.vml` 等 |
| Fortran | `Lib/fortran/shared.f90` | `Lib/fortran/*.vml` | `Lib/fortran/builtin.vml` 等 |

## 命名约定

> **v1.66.55+**: 共享库已去除 `shared_`/`vml_` 前缀。函数直接使用裸名（如 `abs`、`print_str`）。

生成的绑定遵循函数原名，各语言通过命名风格（`--style`）转换为目标命名约定。

## 依赖

- .NET 10.0 SDK
- VML C 编译器（`VMLPrepares/CCompiler`，进程内 ProjectReference 调用）
