# Swift

写法自然，示例里那个飞机大战是完整可玩的。

## 在手机上怎么跑

```
vml run examples/swift/file_io.swift
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 直接调用 `ui_*`，不需要声明
- 注意返回值要显式接住

## 示例

| 文件 | 演示什么 |
|---|---|
| `file_io.swift` | 读写文件 |
| `plane.swift` | 飞机空战 |
| `snake.swift` | 贪吃蛇 |
| `sysinfo.swift` | 设备信息 |

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/SwiftCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Swift 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Swift 3.0 子集 (2016) |
| **发布年份** | 2016 |
| **完成度** | ~90% |
| **MCU完成度** | ~88% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正目标标准版本（含guard/defer/错误处理等Swift 2.0+特性）、完成度和测试数 |

#### 关键字

`associatedtype` `break` `case` `catch` `class` `continue` `default` `defer` `deinit` `do`
`else` `enum` `extension` `fallthrough` `false` `fileprivate` `for` `func` `guard` `if`
`import` `in` `init` `inout` `internal` `is` `let` `nil` `open` `operator` `precedencegroup`
`private` `protocol` `public` `repeat` `return` `self` `Self` `static` `struct` `subscript`
`super` `switch` `throw` `throws` `true` `try` `typealias` `var` `where` `while`

#### 概述
支持 Swift 语言子集，编译为 VML 汇编代码。采用静态编译版架构。文档描述的目标特性含 Swift 2.0~3.0 语法（guard/defer/do-catch/protocol extension），实际实现以基本语法为主。

#### 支持的语言特性

##### 1. 数据类型
- **基本类型**: `Int`, `Double`, `Float`, `Bool`, `String`, `Character`
- **可选类型**: `Optional<T>`, `Int?`, `String?`
- **集合类型**: `Array<T>`, `Dictionary<K,V>`, `Set<T>`
- **元组**: `(Int, String)`, 命名元组 `(x: Int, y: Int)`

##### 2. 变量和常量
```swift
let constant = 10          // 常量
var variable = "Hello"     // 变量
var optional: String?      // 可选类型
var array = [1, 2, 3]      // 数组
var dict = ["key": "value"] // 字典
```

##### 3. 控制流
```swift
// if-else
if condition {
    print("true")
} else {
    print("false")
}

// guard语句
guard let value = optional else {
    return
}

// switch
switch value {
case 1: print("One")
case 2...5: print("2 to 5")
default: print("Other")
}

// 循环
for i in 0..<10 { }
while condition { }
repeat { } while condition
```

##### 4. 函数
```swift
func greet(name: String) -> String {
    return "Hello, \(name)"
}

// 参数标签
func calculate(for x: Int, and y: Int) -> Int {
    return x + y
}

// 闭包
let closure = { (x: Int) -> Int in
    return x * 2
}
```

##### 5. 类和结构体
```swift
class Person {
    var name: String
    var age: Int
    
    init(name: String, age: Int) {
        self.name = name
        self.age = age
    }
    
    func introduce() {
        print("I'm \(name), \(age) years old")
    }
}

struct Point {
    var x: Int
    var y: Int
}
```

##### 6. 协议和扩展
```swift
protocol Drawable {
    func draw()
}

extension Int {
    var squared: Int {
        return self * self
    }
}
```

##### 7. 错误处理
```swift
enum NetworkError: Error {
    case timeout
    case serverError(code: Int)
}

func fetchData() throws -> Data {
    throw NetworkError.timeout
}

do {
    let data = try fetchData()
} catch NetworkError.timeout {
    print("Timeout")
} catch {
    print("Other error")
}
```

#### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (Float) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (Double) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (Int64) 处理模式 |

##### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `Float` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `Float` 声明时报告编译错误。

##### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `Double` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `Double` 声明时报告编译错误。

##### 64位整数 (int64)

本语言中的 64 位整数类型 `Int64` 按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数类型，遇到 `Int64` 声明时报告编译错误。

##### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### 编译器状态
- **完成度**: ~90%
- **标准库**: Lib/swift/ (Swift 标准库，待创建)
- **Lexer**: Lexer.cs
- **Parser**: Parser.cs
- **CodeGenerator**: CodeGenerator.cs

#### 编译使用
```bash
dotnet run --project VMLTool -- input.swift -o output.vml
# 或插件模式:
vmltool input.swift -o output.vml
```
---

#### 🆕 字符串类型 (v1.65.19)

该语言编译器默认使用 `.wstring` (UTF-16LE) 作为内部字符串存储。

| 模式 | 默认编码 | VML 伪指令 | SYSCALL 输出 |
|:-----|:--------|:----------|:------------|
| MCU (默认) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**预定义宏**: `VML_WSTRING` — OS 模式下自动定义，MCU 模式未定义
**输出函数**: OS 模式自动使用 `shared_print_wstr` (UTF-16LE→UTF-8 自动转换)

```c
// 用户代码可通过宏判断编码
#ifdef VML_WSTRING
  // 默认字符串为 wstring (UTF-16LE)
#else
  // 默认字符串为 UTF-8
#endif
```

## 编译器 README

### Swift 3.0 子集 编译器

**路径**: `VMLPrepares/SwiftCompiler/`
**完成度**: ~90% | 🟢 生产可用
**标准库**: `Lib/swift/`（待创建）

#### 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator）
- ✅ 控制流 (if/else/for/while/repeat-while/switch)
- ✅ 函数/闭包定义和调用
- ⚠️ class/struct/enum — 部分支持
- ⚠️ protocol/extension — 部分支持
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- async/await、Actor

**保留**（由 BIOS 实现底层）:
- class/struct、closure
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART

##### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

##### RAM 级别
- `--ram k`：KB级别（2KB~64KB，如 8051/PIC/AVR）
- `--ram m`：MB级别（64KB~1MB，如 ARM Cortex-M，**默认**）
- `--ram g`：GB级别（如 x86/DDR 系统）
- `--stack-size <bytes>`：手动指定栈大小（默认自动根据 --ram 分配）

##### MCU 安全编码提示
- 有限栈空间（256-4096 字节典型），避免深度递归
- 禁止深度递归（>10层需评估栈）
- 禁止动态加载（import/dofile/eval）
- 浮点运算可能需软浮点库

#### 使用
```bash
dotnet run --project VMLTool -- input.swift -o output.vml
```

#### 测试
`Test/Sw/` — 0 测试文件（测试目录待创建）
