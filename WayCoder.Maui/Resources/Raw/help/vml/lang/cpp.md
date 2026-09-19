# C++

与 C 同一套接口，可以写类。适合把游戏逻辑整理成对象。

## 在手机上怎么跑

```
vml run examples/cpp/snake.cpp
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 和 C 一样引 `waycoder_ui.h`
- 支持类与一部分模板；**不要依赖完整的 STL**（标准库是 VML 自己的实现）
- 全局对象的构造时机与 C++ 标准不一定一致 —— 简单起见把状态放在 `main` 里

## 示例

| 文件 | 演示什么 |
|---|---|
| `snake.cpp` | 贪吃蛇 |
| `sysinfo.cpp` | 设备信息 |

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/CppCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### C++ 语言编译器规范说明

> **版本**：v1.1 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | C++11 子集 (ISO/IEC 14882:2011) |
| **发布年份** | 2011 |
| **完成度** | ~95% |
| **MCU完成度** | ~93% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正目标标准为C++11（含nullptr/constexpr/override）；修正完成度和测试数 |

#### 关键字

C 子集全部关键字 + C++ 扩展:
`class` `new` `delete` `this` `virtual` `override` `public` `private` `protected`
`namespace` `using` `template` `typename` `try` `catch` `throw` `typeid` `dynamic_cast`
`static_cast` `const_cast` `reinterpret_cast` `bool` `true` `false` `nullptr`
`friend` `operator` `explicit` `mutable` `inline` `constexpr`

#### 支持的功能 (C++11 子集)

##### 基础特性
- [x] 基本类型: int/char/float/double/bool/void/指针/引用/const
- [x] 控制流: if/else/while/for/do-while/switch/case/break/continue/goto
- [x] 函数: 定义/重载/默认参数/传值/传引用/递归
- [x] 运算符: 全部算术/比较/逻辑/位运算/三目/自增自减/sizeof

##### 面向对象
- [x] 类: 成员/方法/访问控制(public/private/protected)
- [x] 继承: 单继承/多继承/虚继承
- [x] 多态: 虚函数/vtable/动态绑定
- [x] 构造函数/析构函数: 默认/带参/拷贝/初始化列表
- [x] new/delete: 动态内存分配/释放

##### 高级特性
- [x] 模板: 函数模板/类模板
- [x] 异常: try/catch/throw (映射到 VML THROW/CATCH)
- [x] 命名空间: namespace/using
- [x] const 成员函数
- [ ] RTTI: typeid/dynamic_cast (MCU 跳过)

##### 标准库
- [x] new/delete
- [x] iostream (基本)
- [ ] 完整 STL (部分支持)

#### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (float) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (double) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (long long) 处理模式 |

##### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `float` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `float` 声明时报告编译错误。

##### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `double` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `double` 声明时报告编译错误。

##### 64位整数 (int64)

本语言中的 64 位整数类型 `long long` 按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 使用 VML 原生 64 位整数指令 `MOVEL`/`ADDL`/`SUBL`/`MULL`/`DIVL`/`MODL`/`NEGL`/`CMPL`/`ANDL`/`ORL`/`XORL`/`NOTL`/`SHLL`/`SHRL`，通过 L0-L7 八个长整数寄存器运算。v1.65.197+ 起可用。
- **`none` 模式**: 禁用 64 位整数类型，遇到 `long long` 声明时报告编译错误。

##### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### 精度说明
- VML 目标平台为 MCU，不支持完整 STL 和 RTTI
- 关键词表含 C++11 特性 (nullptr/constexpr/override/friend)，但 template/STL 仅语法跳过不生成代码

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

---

#### 🆕 预处理器指令 (v1.65.19)

C++ 编译器继承 C 编译器的全部预处理器功能：

| 指令 | 说明 |
|:-----|:-----|
| `#param lib("xxx")` | 自动链接指定库 |
| `#param path("xxx")` | 添加 include 路径 |
| `#param prefix("xxx")` | 函数名前缀（多语言库） |

配合 `-D VML_PREFIX=xxx` 命令行参数，同一 C/C++ 源码可为不同语言生成不同前缀的库。
详见 [C_LANGUAGE_SPEC.md](../CCompiler/C_LANGUAGE_SPEC.md)。

## 编译器 README

### C++11 编译器

**路径**: `VMLPrepares/CppCompiler/`
**完成度**: ~95% | 🟢 生产可用
**标准库**: `Lib/cpp/`

#### 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator）
- ✅ 控制流 (if/while/for/switch)
- ✅ 函数/运算符重载/friend
- ✅ 类/继承/虚函数/vtable
- ⚠️ 模板 — 解析跳过不生成代码
- ⚠️ Lambda — 基本支持
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 命名空间/转型
- ✅ 共享内置函数库 (builtins.vml)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- throw/catch、typeid、dynamic_cast

**保留**（由 BIOS 实现底层）:
- new/delete(堆)、iostream、STL容器
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART
- 裸指针（有限制）

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
dotnet run --project VMLPrepares/CppCompiler input.Cp -o output.vml
```

#### 测试
`Test/Cp/` — 0 测试文件（测试目录待创建）
