# Forth 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | ANSI Forth 子集 (1994) |
| **发布年份** | 1994 |
| **完成度** | ~95% |
| **MCU完成度** | ~93% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正核心词集状态描述、完成度和测试数 |

## 关键字（核心词）

**栈操作**: `DUP` `DROP` `SWAP` `OVER` `ROT` `?DUP` `NIP` `TUCK`
**算术**: `+` `-` `*` `/` `MOD` `/MOD` `1+` `1-` `2*` `2/` `ABS` `NEGATE`
**比较**: `=` `<>` `<` `>` `<=` `>=` `0=` `0<>` `0<` `0>`
**逻辑**: `AND` `OR` `XOR` `NOT` `TRUE` `FALSE`
**控制流**: `IF` `ELSE` `THEN` `BEGIN` `AGAIN` `UNTIL` `WHILE` `REPEAT` `CASE` `ENDCASE` `OF` `ENDOF`
**循环**: `DO` `LOOP` `+LOOP` `I` `J` `LEAVE` `UNLOOP` `EXIT` `RECURSE`
**定义**: `:` `;` `CONSTANT` `VARIABLE` `CREATE` `DOES>` `DEFER` `IS` `VALUE` `TO`
**内存**: `@` `!` `C@` `C!` `ALLOT` `CELLS` `HERE`
**I/O**: `."` `EMIT` `CR` `SPACE` `TYPE` `KEY`
**其他**: `DEPTH` `IMMEDIATE` `POSTPONE` `[']` `[CHAR]` `LITERAL`

## 概述

本编译器实现 Forth 语言到 VML (Virtual Machine Language) 汇编的编译。Forth 是一种栈式、可扩展的编程语言，广泛用于嵌入式系统和实时控制。

## 语言特性

### 1. Forth 核心概念

#### 栈操作
- **数据栈**: 用于参数传递和临时存储
- **返回栈**: 用于控制流和循环
- **字典**: 存储词（函数）定义

#### 词（Word）定义
```
: SQUARE ( n -- n^2 ) DUP * ;
: FACTORIAL ( n -- n! ) 
    DUP 1 > IF 
        DUP 1 - RECURSE * 
    ELSE 
        DROP 1 
    THEN ;
```

### 2. 基本栈操作词

#### 数据栈操作
- `DUP` - 复制栈顶元素
- `DROP` - 丢弃栈顶元素
- `SWAP` - 交换栈顶两个元素
- `OVER` - 复制栈顶第二个元素
- `ROT` - 旋转栈顶三个元素
- `?DUP` - 条件复制（栈顶非零时复制）

#### 算术运算
- `+` - 加法
- `-` - 减法
- `*` - 乘法
- `/` - 除法
- `MOD` - 取模
- `/MOD` - 除法和取模
- `1+` - 加1
- `1-` - 减1
- `2*` - 乘以2
- `2/` - 除以2

#### 比较运算
- `=` - 等于
- `<>` - 不等于
- `<` - 小于
- `>` - 大于
- `<=` - 小于等于
- `>=` - 大于等于
- `0=` - 等于零
- `0<>` - 不等于零
- `0<` - 小于零
- `0>` - 大于零

#### 逻辑运算
- `AND` - 按位与
- `OR` - 按位或
- `XOR` - 按位异或
- `NOT` - 按位非（或逻辑非）
- `INVERT` - 按位取反

### 3. 控制结构

#### 条件判断
```
: ABS ( n -- |n| )
    DUP 0< IF NEGATE THEN ;
    
: SIGN ( n -- )
    DUP 0> IF ." Positive " 
    ELSE DUP 0< IF ." Negative " 
    ELSE ." Zero " THEN THEN DROP ;
```

#### 循环结构
```
: COUNTDOWN ( n -- )
    BEGIN DUP . 1- DUP 0< UNTIL DROP ;
    
: TIMES ( n -- )
    0 DO I . LOOP ;
    
: 10STARS
    10 0 DO ." *" LOOP ;
```

#### 无限循环
```
: INFINITE-LOOP
    BEGIN ." Hello " AGAIN ;
```

### 4. 内存操作

#### 变量和常量
```
VARIABLE COUNTER
COUNTER @   ( 获取值 )
10 COUNTER !  ( 设置值 )

100 CONSTANT MAX-SIZE
MAX-SIZE .   ( 输出100 )
```

#### 数组和缓冲区
```
CREATE BUFFER 100 ALLOT
BUFFER 10 + C@   ( 读取字节 )
65 BUFFER 10 + C!  ( 写入字节 )
```

#### 字符串操作
```
: .STRING ( addr len -- )
    OVER + SWAP DO I C@ EMIT LOOP ;
    
S" Hello" TYPE   ( 输出字符串 )
```

### 5. 输入输出

#### 字符输入输出
```
KEY ( -- char )   ; 读取一个字符
EMIT ( char -- )  ; 输出一个字符
```

#### 数字输入输出
```
. ( n -- )        ; 输出有符号数
U. ( u -- )       ; 输出无符号数
.HEX ( n -- )     ; 十六进制输出
```

#### 字符串输出
```
." text"          ; 编译时字符串
TYPE ( addr len -- ) ; 输出字符串
CR                ; 换行
SPACE             ; 输出空格
SPACES ( n -- )   ; 输出n个空格
```

### 6. 字典和编译

#### 词定义
```
: DOUBLE ( n -- 2n ) 2 * ;
: SQUARE ( n -- n^2 ) DUP * ;
: CUBE ( n -- n^3 ) DUP DUP * * ;
```

#### 立即词
```
: IMMEDIATE
    LATEST @ DUP C@ 128 OR SWAP C! ;
    
: [COMPILE] ' CFA , ; IMMEDIATE
```

#### 编译控制
```
[    ; 进入解释模式
]    ; 进入编译模式
COMPILE, ( xt -- ) ; 编译执行令牌
LITERAL ( n -- )   ; 编译字面量
```

### 7. 错误处理

#### 异常处理
```
CATCH ( xt -- n | 0 )
THROW ( n -- )
ABORT" message"
```

#### 栈检查
```
?STACK ( -- )   ; 检查栈溢出
DEPTH ( -- n )  ; 返回栈深度
```

### 8. VML 代码生成约定

#### 栈实现
Forth 数据栈使用 VML 内存区域实现：
```
栈指针: R13 (SP)
栈基址: 0x9000
栈大小: 1024字节
```

#### 栈操作映射
```
DUP  -> MOVE R0, [R13]  ; 读取栈顶
      -> PUSH R0        ; 压回栈顶
      
DROP -> ADD R13, #4     ; 移动栈指针
      
SWAP -> MOVE R0, [R13]      ; 栈顶
      -> MOVE R1, [R13+4]   ; 次栈顶
      -> MOVE [R13], R1
      -> MOVE [R13+4], R0
```

#### 词调用约定
```
: SQUARE DUP * ;
VML代码:
LABEL SQUARE
    ; DUP 实现
    MOVE R0, [R13]
    PUSH R0
    
    ; * 实现
    POP R0        ; 第二个操作数
    MOVE R1, [R13] ; 第一个操作数
    MUL R0, R1, R0
    MOVE [R13], R0 ; 结果存回栈顶
    RET
```

### 9. 示例程序

#### 阶乘计算
```
: FACTORIAL ( n -- n! )
    DUP 1 > IF
        DUP 1 - RECURSE *
    ELSE
        DROP 1
    THEN ;
    
5 FACTORIAL .   ( 输出120 )
```

#### 斐波那契数列
```
: FIBONACCI ( n -- fib(n) )
    DUP 2 < IF DROP 1 EXIT THEN
    DUP 1 - RECURSE
    SWAP 2 - RECURSE + ;
    
10 FIBONACCI .   ( 输出55 )
```

#### 简单计算器
```
: CALCULATOR
    BEGIN
        CR ." Enter operation (+, -, *, /, q to quit): "
        KEY DUP EMIT CR
        DUP [CHAR] q = IF DROP EXIT THEN
        
        ." Enter first number: " PAD 20 ACCEPT >NUMBER 2DROP DROP
        ." Enter second number: " PAD 20 ACCEPT >NUMBER 2DROP DROP
        
        SWAP
        CASE
            [CHAR] + OF + ENDOF
            [CHAR] - OF - ENDOF  
            [CHAR] * OF * ENDOF
            [CHAR] / OF / ENDOF
        ENDCASE
        
        ." Result: " . CR
    AGAIN ;
```

### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (FLOAT) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (DOUBLE) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数处理模式 |

#### 32位浮点 (float32)

Forth 中的 `FLOAT` 栈操作（32位单精度浮点）按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点运算。

#### 64位浮点 (double)

Forth 中的双精度栈项（`2CONSTANT` / `2VARIABLE` 等双单元操作）按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。
- **`none` 模式**: 禁用双精度扩展。

#### 64位整数 (int64)

64 位整数在 Forth 中以双单元形式存在（`2*` / `D+` / `DNEGATE` 等双字操作）。VML 编译时按以下模式处理：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位双字扩展。

#### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

### 10. 编译限制

#### 当前实现状态
- **词法分析器**: 完整实现，支持 Forth 词（word）和符号
- **语法分析器**: 基本实现，支持冒号定义和控制结构
- **代码生成器**: 基本实现，栈操作映射到 VML 寄存器/内存操作
- **标准库**: `Lib/forth/` 目录

#### 已实现的核心词集（MCU 模式可用）
1. 栈操作词: DUP, DROP, SWAP, OVER, ROT ✅
2. 算术词: +, -, *, /, MOD ✅
3. 比较词: =, <, >, 0=, 0< ✅
4. 内存词: @, !, C@, C! ✅
5. 控制词: IF, THEN, ELSE, BEGIN, UNTIL, DO, LOOP ✅
6. I/O词: ., EMIT, KEY, CR ✅

#### 待实现
- DOES>, DEFER, IS, VALUE 等高级定义词
- CATCH/THROW 异常处理
- 浮点栈操作 (FLOAT 词集)

### 11. 与VML运行时集成

Forth 程序通过系统调用与 VML 运行时交互：

- **SYSCALL 4**: 输出字符（EMIT）
- **SYSCALL 5**: 输入字符（KEY）
- **SYSCALL 6**: 输出整数（.）
- **SYSCALL 7**: 输入整数（数字输入）
- **SYSCALL 120**: 分配内存（ALLOT）
- **SYSCALL 121**: 释放内存

Forth 的简洁性和栈式架构使其非常适合嵌入式系统和资源受限环境，通过 VML 编译器可以在多种硬件平台上运行 Forth 程序。
---

## 🆕 字符串编码 (v1.65.19)

该语言编译器通过共享库 (Lib/shared/) 间接使用 VML 字符串体系。

| 伪指令 | 宽度 | 编码 | C 类型 |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU 模式** (默认): 字符串输出为 UTF-8 (`.string`)
**OS 模式**: 可通过 `VML_WSTRING` 宏判断编码

共享库已提供宽字符串转换函数 (wchar.h/uchar.h)，各语言编译器可按需使用。
