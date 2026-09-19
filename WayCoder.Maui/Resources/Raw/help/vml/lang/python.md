# Python

**写起来最快的一门**。语法几乎就是桌面 Python，改一行跑一次很舒服。

## 在手机上怎么跑

```
vml run examples/python/sysinfo.py
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 直接调用 `ui_*`，不需要声明
- 编译快（几秒），适合反复改
- **可变网格（棋盘、地图）用共享库的整数网格**，不要用 Python 列表（见下面那条坑）

## 示例

| 文件 | 演示什么 |
|---|---|
| `sysinfo.py` | 设备信息 |
| `tetris.py` | 俄罗斯方块 |

## 实测踩过的坑

- ⚠ **列表「写不生效」**：`b[i] = v` 之后读回来还是 0（嵌套列表也错）。
  棋盘这类请改用共享库的整数网格：
  ```python
  ui_gclear()
  ui_gset(3, 1)      # 第 3 格
  v = ui_gget(3)
  ```
  共 256 个格子，够放 10×20 的棋盘。

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/PythonCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Python 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Python 3.0 (2008) |
| **发布年份** | 2008 |
| **完成度** | ~90% |
| **更新** | 2026-05-18: list/dict/tuple/set 全部实现堆分配 |
| **测试** | 6 通过 |

> 版本 1.0 | 2026-04-19 | 编译器路径: VMLPrepares/PythonCompiler/

---

#### 1. 目标与范围

将 Python 子集编译为 VML 汇编，运行在 VML 虚拟机上。
VML 是寄存器+栈混合架构，16 个通用寄存器（R0–R15），支持整数和浮点运算。

**不实现**: class、async/await、装饰器、类型注解、列表推导、生成器运行时

---

#### 2. 保留字

| 保留字 | 语义 | Parser | CodeGen |
|--------|------|--------|---------|
| False/True/None | 字面量 | ✅ | ✅→0/1/0 |
| and/or/not | 逻辑运算 | ✅ 短路 | ✅ 跳转链 |
| if/elif/else | 条件 | ✅ | ✅ 条件跳转 |
| while/for | 循环 | ✅ 含else | ✅ 循环+else |
| break/continue | 循环控制 | ✅ | ✅ JMP |
| def | 函数定义 | ✅ | ✅ 栈帧约定 |
| return | 返回 | ✅ | ✅ epilogue |
| pass | 空语句 | ✅ | ✅ NOP |
| lambda | 匿名函数 | ✅ | ⚠️ 桩 |
| assert | 断言 | ✅ | ⚠️ 桩 |
| del | 删除 | ✅ | ⚠️ 桩 |
| global/nonlocal | 作用域 | ✅ | ⚠️ 桩 |
| import/from | 模块导入 | ✅ | ⚠️ 桩 |
| raise | 抛异常 | ✅ | ⚠️ 桩 |
| try/except/finally | 异常 | ✅ | ⚠️ 桩 |
| with | 上下文 | ✅ | ⚠️ 桩 |
| yield | 生成器 | ✅ | ⚠️ 桩 |
| match/case | 模式匹配 | ✅ 常量模式 | ⚠️ 桩(已生成跳转) |
| in/is | 成员/身份 | ✅ 比较 | ⚠️ 桩(→0) |
| as | 别名 | ✅ | — |
| as | 除法 | ✅ | ✅ // |
| _ | 通配符 | ✅ | ⚠️ →0 |

---

#### 3. 数据类型

##### 3.1 字面量

| 类型 | 语法 | Lexer Token | CodeGen |
|------|------|-------------|---------|
| 十进制整数 | 42, 1_000 | INTEGER | LOAD RR0 #42 |
| 十六进制 | 0xFF | INTEGER | LOAD RR0 #255 |
| 八进制 | 0o77 | INTEGER | LOAD RR0 #63 |
| 二进制 | 0b1010 | INTEGER | LOAD RR0 #10 |
| 浮点数 | 3.14, 1e10 | FLOAT | LOADF RR0 #3.14 |
| 字符串 | "hello", 'world' | STRING | dataSection + LOAD RR0 [addr] |
| 三引号 | """multi""" | STRING | dataSection |
| 布尔 | True/False | TRUE/FALSE | LOAD RR0 #1/#0 |
| None | None | NONE | LOAD RR0 #0 |
| 列表 | [1, 2, 3] | LBRACKET | ✅ 堆分配 [长度+元素] |
| 字典 | {k:v} | LBRACE | ✅ 堆分配 [对数+键值对] |
| 元组 | (1, 2) | LPAREN | ✅ 堆分配 [长度+元素] |
| 集合 | {1, 2, 3} | LBRACE | ✅ 堆分配 [长度+元素] |

**字典 vs 集合歧义**: `{expr}` 为集合，`{expr:expr}` 为字典，`{}` 为空字典。

##### 3.2 类型兼容

VML 无动态类型，运行时所有值为整数（int）或浮点（float）。bool→int，None→0。
字符串为特殊数据段引用（地址），暂不支持运行时字符串操作。

---

#### 4. 运算符优先级（从低到高）

```
Level 1  lambda x: expr                    # 最松，右结合
Level 2  x if cond else y                 # 三元
Level 3  or                                # 短路
Level 4  and                               # 短路
Level 5  not x                             # 一元
Level 6  in | is | < <= > >= != ==         # 比较（链式：a<b<c → (a<b)&(b<c)）
Level 7  |                                 # 按位或
Level 8  ^                                 # 按位异或
Level 9  &                                 # 按位与
Level 10 << >>                             # 移位
Level 11 + -                               # 加减
Level 12 * / // %                          # 乘除模整除
Level 13 -x +x ~x                          # 一元
Level 14 **                                # 幂（右结合）
Level 15 await x                           # 一元（暂不实现运行时）
Level 16 x[i] x.attr f(args)              # 下标/属性/调用（最高）
```

##### 运算符的 VML 映射

| 运算符 | VML 指令 | 备注 |
|--------|----------|------|
| + | ADD RR0 RR0 RR1 | |
| - | SUB RR0 RR0 RR1 | |
| * | MUL RR0 RR0 RR1 | |
| / | DIV RR0 RR0 RR1 | 浮点 |
| // | DIV RR0 RR0 RR1 + 取整 | 整除 |
| % | MOD RR0 RR0 RR1 | |
| ** | 循环 MUL | 暂简化 |
| & | AND RR0 RR0 RR1 | |
| \| | OR RR0 RR0 RR1 | |
| ^ | XOR RR0 RR0 RR1 | |
| ~ | NOT RR0 RR0 | |
| << | SHL RR0 RR0 RR1 | |
| >> | SHR RR0 RR0 RR1 | |
| -x (neg) | SUB RR0 #0 RR0 | |
| == | CMP + JE/JNE | |
| != | CMP + JNE/JE | |
| < | CMP + JL | |
| <= | CMP + JLE | |
| > | CMP + JG | |
| >= | CMP + JGE | |
| and | 短路跳转链 | A为0则跳过B |
| or | 短路跳转链 | A非0则跳过B |
| not | CMP RR0 #0 + 取反 | |

---

#### 5. 语句与 VML 语义映射

##### 5.1 函数定义

```python
def foo(a, b, c):
    return a + b + c
```

VML 栈帧约定：
```
调用方: PUSH c, PUSH b, PUSH a  (右到左)
       CALL foo                  (压返回地址)
被调用方 prologue: PUSH R15, PUSH R12, MOVE R12 R13
被调用方 epilogue: MOVE R13 R12, POP R12, POP R15, RET
栈布局 (R12=帧指针):
  arg0  ← R12+12
  arg1  ← R12+8
  arg2  ← R12+4
  ret   ← R12+0  (隐式)
  saved R15 ← R12-4
  saved R12 ← R12-8
  local0 ← R12-12
  local1 ← R12-16
```

##### 5.2 if/elif/else

```python
if a > 0:
    x = 1
elif a == 0:
    x = 0
else:
    x = -1
```

VML:
```
    LOAD RR0 [R12-a]
    CMP RR0 #0
    JLE elif_0
    LOAD RR0 #1
    STORE [R12-x] RR0
    JMP endif_0
elif_0:
    LOAD RR0 [R12-a]
    CMP RR0 #0
    JNE else_0
    LOAD RR0 #0
    STORE [R12-x] RR0
    JMP endif_0
else_0:
    LOAD RR0 #-1
    STORE [R12-x] RR0
endif_0:
```

##### 5.3 while 循环

```python
while i < n:
    i = i + 1
else:
    result = -1
```

VML:
```
while_0:
    LOAD RR0 [R12-i]
    CMP RR0 [R12-n]
    JGE endwhile_0
    # body: i = i + 1
    LOAD RR0 [R12-i]
    PUSH RR0
    LOAD RR0 #1
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    STORE [R12-i] RR0
    JMP while_0
endwhile_0:
    # else branch (执行条件: while 条件为 False 且未被 break 跳出)
    LOAD RR0 #-1
    STORE [R12-result] RR0
```

**while-else 语义**: else 在循环正常结束（条件为假）时执行；被 break 跳出时不执行。
实现方式：break 跳转到 endwhile 之后。

##### 5.4 for 循环

```python
for i in range(n):
    x = i * 2
```

VML:
```
    LOAD RR0 #0           # i = 0
    STORE [R12-i] RR0
for_0:
    LOAD RR0 [R12-i]
    CMP RR0 [R12-n]
    JGE endfor_0
    # body: x = i * 2
    LOAD RR0 [R12-i]
    PUSH RR0
    LOAD RR0 #2
    MOVE RR1 RR0
    POP RR0
    MUL RR0 RR0 RR1
    STORE [R12-x] RR0
    # increment
    LOAD RR0 [R12-i]
    PUSH RR0
    LOAD RR0 #1
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    STORE [R12-i] RR0
    JMP for_0
endfor_0:
```

##### 5.5 match/case

```python
match n:
    case 1:
        print(100)
    case 2:
        print(200)
    case _:
        print(999)
```

VML:
```
    LOAD RR0 [R12-n]
    PUSH RR0
    LOAD RR0 #1
    CMP RR0 RR1
    JE case_0_end
    LOAD RR0 #100
    PUSH RR0
    SYSCALL #4
    ADD RR13 RR13 #4
    JMP match_end
case_0_end:
    LOAD RR0 #2
    CMP RR0 RR1
    JE case_1_end
    LOAD RR0 #200
    PUSH RR0
    SYSCALL #4
    ADD RR13 RR13 #4
    JMP match_end
case_1_end:
    # _ wildcard: always matches
    LOAD RR0 #999
    PUSH RR0
    SYSCALL #4
    ADD RR13 RR13 #4
    JMP match_end
match_end:
    POP RR0
```

**语义**: 将匹配值压栈，每个 case 比较；_ 通配符无条件匹配。匹配成功执行 body 后 JMP match_end。

##### 5.6 lambda

```python
f = lambda x, y: x + y
```

VML:
```
lambda_f_0:
    PUSH R15, PUSH R12, MOVE R12 R13
    # x = R12+12, y = R12+8
    LOAD RR0 [R12+12]
    PUSH RR0
    LOAD RR0 [R12+8]
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    MOVE RR13 R12, POP R12, POP R15, RET
# f 指向 lambda_f_0 的地址
```

##### 5.7 assert

```python
assert x > 0, "x must be positive"
```

VML:
```
    # evaluate x > 0 → bool in RR0
    CMP RR0 #0
    JNE assert_ok_0
    # failure: print message, halt
    LOAD RR0 #-1
    SYSCALL #99          # 异常退出
assert_ok_0:
```

##### 5.8 赋值与增强赋值

```python
x = 10
x += 5
```

VML:
```
    LOAD RR0 #10
    STORE [R12-x] RR0
    LOAD RR0 [R12-x]
    PUSH RR0
    LOAD RR0 #5
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    STORE [R12-x] RR0
```

##### 5.9 三元表达式

```python
result = 1 if a > 10 else 0
```

VML:
```
    LOAD RR0 [R12-a]
    CMP RR0 #10
    JG cmp_true_0
    LOAD RR0 #0
    JMP cmp_end_1
cmp_true_0:
    LOAD RR0 #1
cmp_end_1:
    STORE [R12-result] RR0
```

---

#### 6. 内置函数

| 函数 | VML | 参数 | 返回值 |
|------|-----|------|--------|
| print(x) | SYSCALL #4 | 栈上1个int | 无 |
| input() | SYSCALL #7 | 无 | RR0=整数 |
| len(x) | 桩 | — | RR0=0 |
| range(n) | for循环展开 | — | 特殊处理 |
| abs(x) | 内联 | RR0 | RR0 |
| int(x) | 内联 | RR0 | RR0 |
| str(x) | 桩 | — | RR0=0 |

---

#### 7. 缩进规则（Lexer）

- 行首空格数 > currentIndent → 生成 INDENT token，更新 currentIndent
- 行首空格数 < currentIndent → 循环生成 DEDENT token，currentIndent -= 4
- 行首无空格且 currentIndent > 0 → 补发 DEDENT 至 0
- 缩进单位固定 4 空格，tab = 4 空格
- 文件结束时补齐所有未关闭的 INDENT → DEDENT

---

#### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (float) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (int) 处理模式 |

##### 32位浮点 (float32)

Python 的 `float` 类型在 VML 编译时映射为 64 位双精度浮点。32 位浮点运算用于内部中间结果处理：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。
- **`none` 模式**: 禁用浮点运算。

##### 64位浮点 (double)

Python 的 `float` 类型原生为 IEEE 754 双精度 64 位浮点，按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。
- **`none` 模式**: 禁用浮点运算。

##### 64位整数 (int64)

Python 3 的 `int` 类型为任意精度整数。VML 编译时按以下模式处理 64 位范围内的整数：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 降级为 32 位整数。

##### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### 8. 不支持的 Python 特性

| 特性 | 原因 |
|------|------|
| class / __init__ / 继承 | 类系统复杂度 |
| async / await | 需事件循环 |
| 装饰器 @ | 语法糖优先级低 |
| 类型注解 x: int | 编译期检查暂不需要 |
| f-string | 字符串插值复杂 |
| 列表推导 [x*2 for x in ...] | 需迭代器 |
| 生成器 yield 运行时 | 需状态机 |
| with __enter__/__exit__ | 简化为空实现 |
| match 完整模式（解构/守卫） | 仅常量+通配符 |
| *args/**kwargs | 可变参数 |
| 多重赋值 a, b = 1, 2 | 解包 |
| 海象运算符 := | 暂不实现 |

---

#### 9. 变更记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-04-19 | 0.1 | 初始版本 |
| 2026-04-19 | 1.0 | 加入 VML 语义映射、栈帧约定、缩进规则、while/for-else 语义 |

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

### Python 3 编译器

**路径**: `VMLPrepares/PythonCompiler/`
**完成度**: ~90% | 🟢 生产可用
**标准库**: `Lib/python/`

#### 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator）
- ✅ 控制流 (if/while/for)
- ✅ 函数/类定义和调用
- ✅ list/dict/tuple/set — 堆分配实现
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- async、yield、import

**保留**（由 BIOS 实现底层）:
- list/dict/tuple/set(堆)、class
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
dotnet run --project VMLPrepares/PythonCompiler input.Py -o output.vml
```

#### 测试
`Test/Py/` — 0 测试文件（测试目录待创建）
