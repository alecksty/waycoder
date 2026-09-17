# GenDyn — VML 动态库绑定生成器

扫描编译后的共享库（`.dylib`/`.so`/`.dll`），编译 C 胶水代码，生成 VML FFI 包装器和 22 种语言的调用绑定。

## 快速开始

```bash
# 一键全流程：准备 demodll 动态库
dotnet run --project tools/GenDyn -- -A -n demodll

# 只扫描动态库导出符号
dotnet run --project tools/GenDyn -- -s

# 只为 Python 生成绑定
dotnet run --project tools/GenDyn -- -g -l python -n demodll
```

## CLI

```
gendyn <命令> [选项]
```

### 命令

| 短名 | 长名 | 说明 |
|------|------|------|
| `-s` | `--scan` | 扫描动态库导出符号，更新 `dynlibs.json` |
| `-b` | `--build` | 编译 C 胶水代码为共享库 |
| `-w` | `--wrap` | 生成 VML FFI 包装器 `.vml` |
| `-g` | `--bindings` | 生成 22 种语言绑定文件 |
| `-A` | `--all` | 一键全流程（`-s` + `-b` + `-w` + `-g`） |

### 选项

| 短名 | 长名 | 默认值 | 说明 |
|------|------|------|------|
| `-n` | `--name` | 空（全部库） | 目标库名称 |
| `-H` | `--header` | 自动匹配 | C 头文件路径（用于类型信息） |
| `-l` | `--lang` | `all` | 目标语言（仅 `-g` / `-A`） |
| `-r` | `--root` | `.` | 项目根目录 |
| `-h` | `--help` | — | 显示帮助 |

## 流水线步骤

### Step 1: 扫描 (`-s`)
使用平台工具（macOS: `nm -gU`，Linux: `nm -D --defined-only`，Windows: `dumpbin /exports`）解析动态库的导出符号。如存在匹配的 `.h` 头文件，自动提取函数签名和类型信息。结果写入 `Lib/dynamic/dynlibs.json`。

### Step 2: 构建 (`-b`)
编译 C 胶水代码为共享库：
- macOS: `cc -shared -o lib{name}_glue.dylib {name}_lib.c`
- Linux: `cc -shared -o lib{name}_glue.so {name}_lib.c`
- Windows: `cl /LD /Fe:lib{name}_glue.dll {name}_lib.c`

### Step 3: 包装 (`-w`)
生成 VML 汇编包装器，包含：
- `{name}_init` — 执行 `dl_open` (SYSCALL #370) + 每个函数的 `dl_sym` (SYSCALL #371)
- 每个函数的调用包装器，使用 `NativeCallEx` (SYSCALL #376)
- `{name}_teardown` — 执行 `dl_close` (SYSCALL #372)

### Step 4: 绑定 (`-g`)
为每种语言生成 `Lib/{lang}/ext/lib{name}.{ext}` 绑定文件，包含外部函数声明和 VML 调用代码。

## 输出文件

| 文件 | 路径 |
|------|------|
| 配置 | `Lib/dynamic/dynlibs.json` |
| C 胶水源码 | `Lib/dynamic/{name}_lib.c`（用户提供） |
| VML 包装器 | `Lib/dynamic/{name}.vml` |
| C 绑定 | `Lib/c/ext/lib{name}.h` |
| C++ 绑定 | `Lib/cpp/ext/lib{name}.hpp` |
| BASIC 绑定 | `Lib/basic/ext/lib{name}.bas` |
| Python 绑定 | `Lib/python/ext/lib{name}.py` |
| Go 绑定 | `Lib/go/ext/lib{name}.go` |
| Java 绑定 | `Lib/java/ext/Lib{PascalCase}.java` |
| C# 绑定 | `Lib/csharp/ext/Lib{PascalCase}.cs` |
| ... | 全部 22 种语言 |

## dynlibs.json 配置

JSON 配置文件位于 `Lib/dynamic/dynlibs.json`，缺失时自动扫描 `Lib/dynamic/` 目录生成：

```json
{
  "demodll": {
    "Library": "libdemodll.dylib",
    "Header": "demodll.h",
    "Glue": "demodll_lib.c",
    "Functions": {
      "add": { "Return": "int", "Params": "int a, int b" },
      "fact": { "Return": "int", "Params": "int n" },
      "greet": { "Return": "string", "Params": "string name" }
    }
  }
}
```

## 调用约定

VML 包装器遵循 cdecl 约定：
- 调用者从右到左 push 参数
- 调用者负责堆栈清理（`ADD R13, #N*4`）
- 字符串和结构体参数通过指针传递
- 固定内存布局：0xE000 handle, 0xE010 func pointers, 0xE100 args, 0xE200 type descs, 0xE300 saved BP

## 与 GenLib 的文件隔离

| 库类型 | VML 文件 | 绑定文件 | JSON 配置 |
|--------|---------|---------|-----------|
| 静态库 (GenLib) | `Lib/{lang}/{module}.vml` | `Lib/{lang}/shared.{ext}` | `Lib/modules.json` |
| 动态库 (GenDyn) | `Lib/dynamic/{name}.vml` | `Lib/{lang}/ext/lib{name}.{ext}` | `Lib/dynamic/dynlibs.json` |

动态库和静态库文件路径完全隔离，不会互相覆盖。

## 依赖

- .NET 10.0 SDK
- 平台 C 编译器（`cc` / `cl`）
- 平台二进制工具（`nm` / `dumpbin`）
