# Scheme

Lisp 方言，括号就是一切。

## 在手机上怎么跑

```
vml run examples/scheme/catch.scm
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 一切皆表达式：`(函数 参数…)`
- «bold»游戏请写成扁平顶层程序«/»（见下面那条坑）

## 示例

| 文件 | 演示什么 |
|---|---|
| `catch.scm` | 接方块 |
| `sysinfo.scm` | 设备信息 |

## 实测踩过的坑

- ⚠ «bold»用户函数看不见顶层变量«/»（两边的帧指针会互相踩）。
  正解：游戏逻辑全写在顶层，只把「参数全部传进去、不碰全局」的«bold»纯函数«/»抽出去。

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/SchemeCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Scheme 语言编译器规范说明

> «bold»版本«/»：v1.0 | «bold»日期«/»：2026-07-06 | «bold»修订者«/»：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| «bold»目标标准«/» | R5RS (1998) 子集 |
| «bold»发布年份«/» | 1998 |
| «bold»完成度«/» | ~93% |
| «bold»测试«/» | 32 通过 |

#### 关键字

`define` `lambda` `let` `let*` `if` `cond` `else` `begin` `and` `or` `not`
`set!` `display` `newline` `print` `peek` `poke` `asm` `chipasm`
`eq?` `equal?` `null?`

#### 概述
支持 Scheme (R5RS) 语言子集，编译为 VML 汇编代码。采用 S-表达式解析架构，支持函数式编程核心特性。

#### 支持的语言特性

##### 1. 数据类型
- «bold»整数«/»: `42`, `-1`, `0`
- «bold»字符串«/»: `"hello"` (表达式级暂不支持)
- «bold»布尔«/»: `#t`, `#f` (由 Parser 支持)
- «bold»符号«/»: `x`, `+`, `foo-bar`
- «bold»列表«/»: S-表达式嵌套结构

##### 2. 表达式
- «bold»算术运算«/»: `+`, `-`, `*`, `/`
- «bold»比较运算«/»: `<`, `>`, `<=`, `>=`, `=`, `eq?`, `equal?`
- «bold»逻辑运算«/»: `and`, `or`, `not`
- «bold»条件«/»: `if`, `cond`
- «bold»顺序«/»: `begin`

##### 3. 函数
- «bold»函数定义«/»: `(define (name args) body)`
- «bold»匿名函数«/»: `(lambda (args) body)`
- «bold»函数调用«/»: `(func arg1 arg2 ...)`
- «bold»变量定义«/»: `(define name value)`
- «bold»变量赋值«/»: `(set! var value)`

##### 4. 绑定结构
- «bold»局部绑定«/»: `(let ((var val) ...) body)` — 并行绑定
- «bold»顺序绑定«/»: `(let* ((var val) ...) body)` — 串行绑定

##### 5. 输入输出
- «bold»打印整数«/»: `(print val)` — 输出整数并换行
- «bold»显示«/»: `(display val)` — 输出整数不换行
- «bold»换行«/»: `(newline)` — 输出换行符

##### 6. 内存操作
- «bold»内存读取«/»: `(peek addr)` — 读取地址 addr 处的 32 位值
- «bold»内存写入«/»: `(poke addr val)` — 写入值 val 到地址 addr

##### 7. 内联汇编
- «bold»VML 汇编«/»: `(asm "instruction")` — 直接嵌入 VML 指令
- «bold»芯片汇编«/»: `(chipasm "arch" "code")` — 芯片特定汇编

#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
针对单片机环境优化。
- «bold»保留«/»: 全部已实现特性
- «bold»跳过«/»: call/cc（未实现）

##### OS 模式（`--mode os`，预留）
全部语言特性可用。

#### 使用示例

```scheme
;; 阶乘
(define (fact n)
  (if (<= n 1)
      1
      (* n (fact (- n 1)))))

;; 匿名函数
(define f (lambda (x) (+ x 1)))
(print (f 5))

;; let 绑定
(let ((x 10) (y 20))
  (print (+ x y)))

;; let* 顺序绑定
(let* ((x 1) (y (+ x 1)))
  (print y))

;; cond 多分支
(define (sign x)
  (cond ((< x 0) (print -1))
        ((> x 0) (print 1))
        (else (print 0))))

;; 内存操作
(asm "nop")
(peek 0x4000)
(poke 16384 65)
```

#### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (flonum) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数处理模式 |

##### 32位浮点 (float32)

Scheme 中的 `flonum`（不精确浮点数）在 VML 编译时按以下模式处理：

- «bold»`hard` 模式（默认）«/»: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- «bold»`soft` 模式«/»: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- «bold»`none` 模式«/»: 禁用所有 32 位浮点运算。

##### 64位浮点 (double)

Scheme 中的双精度浮点运算按以下模式编译：

- «bold»`soft` 模式（默认）«/»: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- «bold»`hard` 模式«/»: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。
- «bold»`none` 模式«/»: 禁用双精度浮点运算。

##### 64位整数 (int64)

Scheme 中的大整数（`bignum`）超出 32 位范围时按以下模式处理：

- «bold»`soft` 模式（默认）«/»: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- «bold»`hard` 模式«/»: 预留，未来 VML 版本将支持原生 64 位整数指令。
- «bold»`none` 模式«/»: 降级为 32 位整数。

##### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### 编译

```bash
# 编译 Scheme 到 VML
dotnet run --project VMLPrepares/SchemeCompiler input.scm -o output.vml

# 运行
dotnet run --project VMLEmulators/ConsoleEmulator -- -r output.vml
```

#### 测试

```bash
# 运行 Scheme 测试
dotnet test VMLTests/VMLTests.csproj --filter "SchemeTests"
```

---

#### 🆕 字符串编码 (v1.65.19)

该语言编译器通过共享库 (Lib/shared/) 间接使用 VML 字符串体系。

| 伪指令 | 宽度 | 编码 | C 类型 |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

«bold»MCU 模式«/» (默认): 字符串输出为 UTF-8 (`.string`)
«bold»OS 模式«/»: 可通过 `VML_WSTRING` 宏判断编码

共享库已提供宽字符串转换函数 (wchar.h/uchar.h)，各语言编译器可按需使用。

## 编译器 README

### Scheme (R5RS 子集) 编译器

«bold»路径«/»: `VMLPrepares/SchemeCompiler/`
«bold»完成度«/»: ~93% | 🟢 生产可用
«bold»标准库«/»: `Lib/scheme/`（待创建）

#### 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGen ~1800行）
- ✅ S-表达式解析
- ✅ define / lambda / 函数定义和调用
- ✅ if / cond / and / or 条件
- ✅ 基本类型: 整数/浮点/布尔/字符串/符号
- ✅ 算术: + - * / quotient remainder
- ✅ 比较: = < > <= >=
- ✅ 列表: cons/car/cdr/list/null?/pair?
- ✅ 高阶函数: apply/map
- ✅ GC 标记 (预留接口)
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)

#### 缺失功能
- ✅ 尾递归优化 (TCO)
- ❌ call/cc (call-with-current-continuation)
- ❌ 宏系统 (define-syntax/syntax-rules)
- ❌ 向量 (vector)
- ❌ 字符/字符串完整操作
- ❌ 完整数值塔 (complex/rational)
- ❌ 标准库 (Lib/scheme/)

#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

«bold»跳过«/»（遇到这些语法不生成代码）:
- call/cc (需栈捕获)、动态eval

«bold»保留«/»（由 BIOS 实现底层）:
- cons(堆分配)、GC标记
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- display/write 映射到 SYSCALL

##### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

#### 使用
```bash
dotnet run --project VMLTool -- input.scm -o output.vml
```

#### 测试
测试内嵌在编译器源代码验证中
