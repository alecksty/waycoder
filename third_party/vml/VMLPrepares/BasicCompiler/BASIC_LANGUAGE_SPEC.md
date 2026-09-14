# BASIC 语言编译器规范说明

> **版本**：v2.1 | **日期**：2026-08-01 | **修订者**：深圳市探索智能科技有限公司
> **更新**: v1.66.33 — 全方言 ≥90% + CLASS/OOP + GPIO + 116 个关键字

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | QBasic (默认) + 6种方言, 全部 ≥90% 完成度 |
| **CLI 选项** | `--basictype qbasic\|turbobasic\|freebasic\|truebasic\|purebasic\|chipbasic\|minibasic` |
| **完成度** | 92% (QBasic), 90% (全部方言) |
| **测试** | 63 通过 (Lang_BASIC) + 22 方言测试 |
| **Token 总数** | 116 个关键字, 141 个 TokenType |

---

## 多方言关键字集合

BASIC 编译器通过 `--basictype` 选项支持 7 种方言。下方列出每种方言的关键字差异。

### 1. QBasic (默认) — `__QBASIC__`

Microsoft QBasic/QuickBASIC 兼容。**所有后续方言以此为基础**。

**独有关键字** (14):
`PSET`, `CIRCLE`, `PAINT`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `SCREEN`, `LPRINT`, `PRINT USING`, `FREEFILE`, `ON ERROR`, `RESUME`, `DEF FN`

**适用场景**: DOS 时代 QBasic 程序、QuickBASIC 游戏 (GORILLAS/NIBBLES)

---

### 2. TurboBasic — `__TURBOBASIC__`

Borland Turbo Basic 兼容。

**QBasic 基础上新增** (6):
`EXIT`, `DO`, `LOOP`, `UNTIL`, `CASE ELSE`, `FUNCTION`

**移除** (与 QBasic 差异) (8):
`PSET`, `CIRCLE`, `PAINT`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `SCREEN`

**适用场景**: 科学计算、结构化 BASIC 程序

---

### 3. FreeBasic — `__FREEBASIC__`

开源跨平台 BASIC 编译器兼容。

**QBasic 基础上新增** (12):
`PTR`, `CAST`, `CPTR`, `ANY`, `EXTENDS`, `OPERATOR`, `PROPERTY`, `ENUM`, `NAMESPACE`, `USING`, `DESTRUCTOR`, `CONSTRUCTOR`

**移除** (与 QBasic 差异) (6):
`LPRINT`, `PRINT USING`, `PSET`, `CIRCLE`, `DRAW`, `PALETTE`

**适用场景**: 现代开源 BASIC 项目、跨平台开发

---

### 4. TrueBasic — `__TRUEBASIC__`

ANSI/ISO 标准 BASIC。

**QBasic 基础上新增** (4):
`MAT`, `ZER`, `CON`, `SOUND`

**移除** (与 QBasic 差异) (12):
`SELECT`, `CASE`, `IS`, `ELSEIF`, `DO`, `LOOP`, `EXIT`, `GOSUB`, `PEEK`, `POKE`, `LPRINT`, `ON ERROR`

**适用场景**: 教育、标准兼容程序

---

### 5. PureBasic — `__PUREBASIC__`

PureBasic 兼容。

**QBasic 基础上新增** (8):
`PROCEDURE`, `ENDPROCEDURE`, `PROTECTED`, `GLOBAL`, `THREADED`, `INTERFACE`, `ENDINTERFACE`, `NEW`

**移除** (与 QBasic 差异) (10):
`GOSUB`, `RETURN`, `LPRINT`, `PSET`, `CIRCLE`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `DEF FN`

**适用场景**: 游戏开发、GUI 应用

---

### 6. ChipBasic — `__CHIPBASIC__`

MCU 优化方言。

**QBasic 基础上新增** (3):
`PINMODE`, `DIGITALWRITE`, `DIGITALREAD`

**移除** (与 QBasic 差异) (15):
`SCREEN`, `PSET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`, `VIEW`, `WINDOW`, `PALETTE`, `LPRINT`, `PRINT USING`, `FREEFILE`, `ON ERROR`, `RESUME`, `CHIPASM`

**适用场景**: 嵌入式 MCU (Arduino/STM32)、IoT 设备

---

### 7. MiniBasic — `__MINIBASIC__`

最小子集 — 教学/资源受限环境。

**QBasic 基础上新增** (0):
无新增。

**移除** (与 QBasic 差异) (20):
`TYPE`, `DIM`, `REDIM`, `ERASE`, `SWAP`, `SHARED`, `COMMON`, `SELECT`, `CASE`, `DO`, `LOOP`, `EXIT`, `GOSUB`, `GOTO`, `CALL`, `LPRINT`, `PRINT USING`, `PSET`, `CIRCLE`, `DRAW`

**保留关键字** (仅 15):
`PRINT`, `INPUT`, `IF`, `THEN`, `ELSE`, `FOR`, `NEXT`, `WHILE`, `WEND`, `LET`, `REM`, `DIM`(简化), `END`, `GOTO`(简化), `GOSUB`(简化)

**适用场景**: 教学、微控制器引导程序

---

### 方言关键字对比总表

| 类别 | QBasic | TurboBasic | FreeBasic | TrueBasic | PureBasic | ChipBasic | MiniBasic |
|------|:------:|:----------:|:---------:|:---------:|:---------:|:---------:|:---------:|
| 关键字总数 | ~70 | ~68 | ~76 | ~62 | ~68 | ~57 | ~15 |
| 完成度 | 92% | 90% | 90% | 90% | 90% | 90% | 90% |
| 图形 | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| OOP | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| MCU GPIO | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| 行号 | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 可选 |
| GOSUB | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | 简化 |

---

## QBasic 关键字完整列表 (按类别)

### 控制流 (14)
`IF`, `THEN`, `ELSE`, `ELSEIF`, `END`, `END IF`, `SELECT`, `CASE`, `IS`, `TO`, `FOR`, `STEP`, `NEXT`, `WHILE`, `WEND`, `DO`, `LOOP`, `UNTIL`, `EXIT`, `GOTO`, `GOSUB`, `RETURN`

### 子程序/函数 (7)
`SUB`, `FUNCTION`, `CALL`, `BYVAL`, `BYREF`, `DECLARE`, `DEF FN`

### 变量/数据 (12)
`LET`, `DIM`, `SWAP`, `ERASE`, `REDIM`, `PRESERVE`, `LOCAL`, `STATIC`, `SHARED`, `COMMON`, `CONST`, `TYPE`

### I/O (8)
`PRINT`, `INPUT`, `LPRINT`, `PRINT USING`, `OPEN`, `CLOSE`, `AS`, `FREEFILE`

### 图形 (17)
`SCREEN`, `PSET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`, `COLOR`, `LOCATE`, `CLS`, `VIEW`, `WINDOW`, `PALETTE`, `GET`, `PUT`, `WIDTH`, `BEEP`

### 函数 (20+)
`ABS`, `SGN`, `SQR`, `INT`, `FIX`, `SIN`, `COS`, `TAN`, `EXP`, `LOG`, `ATN`, `RND`, `RANDOMIZE`, `TIMER`, `DATE$`, `TIME$`, `PEEK`, `POKE`, `INKEY$`, `KBGETCH`, `KBHIT`, `MOUSEGETX`, `MOUSEGETY`, `MOUSELEFT`, `MOUSERIGHT`, `LEN`, `CHR$`, `ASC`, `STR$`, `VAL`, `LEFT$`, `RIGHT$`, `MID$`, `exit`

### 其它 (9)
`REM`, `DATA`, `READ`, `RESTORE`, `SLEEP`, `SYSTEM`, `ON ERROR`, `RESUME`, `CHIPASM`, `ASM`

## 运算符完整列表 (优先级从高到低)

| 优先级 | 运算符 | Token | 说明 |
|:------:|--------|:-----:|------|
| 1 | `^` | EXPONENT | 指数 |
| 2 | `-` (一元) | MINUS | 负号 |
| 3 | `*` `/` | MULTIPLY, DIVIDE | 乘除 |
| 4 | `+` `-` | PLUS, MINUS | 加减 |
| 5 | `=` `<` `>` `<=` `>=` `<>` | EQUALS, LESS, GREATER, LESS_EQUAL, GREATER_EQUAL, NOT_EQUAL | 关系 |
| 6 | `NOT` | NOT | 逻辑非 |
| 7 | `AND` | AND | 逻辑与 |
| 8 | `OR` | OR | 逻辑或 |

## 分隔符

| 符号 | Token | 用途 |
|:----:|:-----:|------|
| `,` | COMMA | 参数/分栏分隔 |
| `;` | SEMICOLON | PRINT 不换行 |
| `:` | COLON | 多语句分隔 |
| `.` | DOT | 成员访问 |
| `"` | QUOTE | 字符串定界 |
| `(` `)` | LPAREN, RPAREN | 函数/数组括号 |
| `#` | HASH | 文件号前缀

## 3. 语句参考

### 3.1 基本 I/O

```basic
' PRINT — 输出到控制台
PRINT "Hello"       ' 字符串
PRINT 42            ' 数字
PRINT A; B;         ' 分号不换行
PRINT A, B          ' 逗号分栏
PRINT               ' 空行

' INPUT — 键盘输入
INPUT "prompt: ", X
INPUT A, B, C
INPUT A$

' LPRINT — 打印机输出
LPRINT "Hello"
```

**PRINT 输出目标规则**

PRINT 的输出目标由 `SCREEN` 模式决定：

| 模式 | 控制台输出 | 内存输出 | 说明 |
|:-----|:---------:|:-------:|------|
| **默认（无 SCREEN）** | ✅ | ❌ | 仅输出到控制台终端 |
| **SCREEN 0（文本模式）** | ✅ | ✅ `0xB8000` | 控制台 + VGA 文本显存同时输出 |
| **SCREEN N>0（图形模式）** | ❌ | ✅ VGA 帧缓冲 | 仅渲染到 VGA 图形帧缓冲，控制台不输出 |

具体机制：
- **SYSCALL #4**：纯控制台输出，不涉及 VGA。处理 BEL 蜂鸣和 UTF-8 解码
- **文本模式 VGA**：由扩展库 `vga_text_putchar` (Lib/shared/vga_text.vml) 处理，写入 `0xB8000` 文本显存并管理光标。BASIC 编译器默认链接此库，在每次 SYSCALL #4 前调用
- **图形模式 VGA**：编译器为每个 PRINT 字符生成 `EmitGfxPrintChar()` 调用，从 8×8 位图字库渲染像素到 VGA 图形帧缓冲
- 两者各自检查 SCREEN 模式（0x6FF0），仅在各自模式激活时执行，互补工作
- **LOCATE** 设置的光标位置在文本模式和图形模式下均有效（通过 `0x6FF4`/`0x6FF8` 共享）

### 3.2 变量操作

```basic
' LET (可省略)
LET A = 42
B = A + 10

' SWAP — 变量交换
SWAP A, B

' ERASE — 清除数组
ERASE ArrayName

' DIM — 数组声明
DIM A(10)           ' 一维
DIM B(5, 5)         ' 二维
DIM C(3, 3, 3)      ' 三维
DIM S$(10)          ' 字符串数组
```

### 3.3 控制流

```basic
' IF
IF condition THEN
    ...
ELSEIF condition THEN
    ...
ELSE
    ...
END IF

' 单行 IF
IF X > 10 THEN PRINT "Big" ELSE PRINT "Small"

' FOR
FOR I = 1 TO 10 STEP 2
    PRINT I
NEXT I

' WHILE
WHILE A > 0
    A = A - 1
WEND

' DO
DO
    EXIT DO
LOOP WHILE A > 0

DO WHILE A > 0
    EXIT DO
LOOP

DO
LOOP UNTIL A > 0

' SELECT CASE
SELECT CASE X
    CASE 1
        PRINT "One"
    CASE 2 TO 5
        PRINT "Two to Five"
    CASE IS > 10
        PRINT "Big"
    CASE ELSE
        PRINT "Other"
END SELECT

' GOTO / GOSUB
GOTO label
GOSUB subroutine
RETURN

' EXIT
EXIT FOR
EXIT DO
EXIT WHILE
EXIT SUB
EXIT FUNCTION
```

### 3.4 子程序与函数

```basic
SUB MySub(param1, BYREF param2)
    LOCALVAR = param1
    param2 = 99
END SUB

FUNCTION Add(a, b)
    Add = a + b
END FUNCTION

CALL MySub(x, y)
result = Add(5, 3)
```

### 3.5 图形 (标准 QBASIC 语法)

```basic
SCREEN 9              ' 设置图形模式
WIDTH 80, 25          ' 设置文本尺寸
COLOR 7, 0            ' 前景色, 背景色
CLS                   ' 清屏
LOCATE 10, 20         ' 光标定位

PSET (100, 150), 4    ' 画点 (x, y), 颜色索引
LINE (0,0)-(100,100), 2                    ' 画线
LINE (0,0)-(100,100), 2, B                 ' 矩形边框
LINE (0,0)-(100,100), 2, BF                ' 填充矩形
CIRCLE (160,120), 50, 3                    ' 画圆
PAINT (160,120), 4, 3                      ' 填充

' 图形字符串宏 (DRAW)
DRAW "U10 R20 D10 L20"                     ' 绘图字符串
```

**SCREEN 模式对 PRINT 的影响**
`SCREEN` 语句切换显示模式，PRINT 输出到控制台。


### 3.6 VML 扩展图形

```basic
VGAPUTPIXEL x, y, r, g, b      ' 24位彩色画点
VGADRAWLINE x1,y1,x2,y2,r,g,b   ' 彩色画线
VGADRAWCIRCLE x,y,radius,r,g,b  ' 彩色画圆
VGAFILLCIRCLE x,y,radius,r,g,b  ' 彩色填充圆
VGADRAWRECT x,y,w,h,r,g,b       ' 彩色矩形边框
' 字符串
LEN(s$), CHR$(n), ASC(s$), STR$(n), VAL(s$)
LEFT$(s$, n), RIGHT$(s$, n), MID$(s$, start, len)

' 随机
RND(n)
RANDOMIZE [TIMER]

' 内存
PEEK(addr)
POKE addr, value

' 时间
TIMER, DATE$, TIME$

' 检测
KBGETCH(), KBHIT()
MOUSEGETX(), MOUSEGETY(), MOUSELEFT(), MOUSERIGHT()
VGAGETPIXEL(x,y), VGAGETWIDTH(), VGAGETHEIGHT()
```

### 3.7 文件操作

```basic
OPEN "file.txt" FOR INPUT AS #1
OPEN "file.txt" FOR OUTPUT AS #2
INPUT #1, X
PRINT #2, "Hello"
CLOSE #1
```

### 3.9 音频

```basic
' PLAY — 音乐字符串播放
PLAY "C D E F G A B"
PLAY "O3 L4 CDEFGAB"

' SOUND — 频率/持续时间
SOUND 440, 18         ' 频率(Hz), 持续时间(1/18秒)

' BEEP — 系统蜂鸣
BEEP
```

### 3.10 错误处理

```basic
' ON ERROR GOTO — 运行时错误处理
ON ERROR GOTO ErrorHandler
    ... 正常代码 ...
    EXIT SUB

ErrorHandler:
    PRINT "错误: "; ERR
    RESUME NEXT
```

### 3.11 数据类型与结构

```basic
' CONST — 常量定义
CONST PI = 3.14159
CONST MAX_ROWS = 25

' TYPE / END TYPE — 自定义类型
TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE

' 自定义类型使用
DIM P AS Point
P.X = 10
P.Y = 20

' DATA/READ/RESTORE — 内嵌数据
DATA 1, 2, 3, 4, 5
FOR I = 1 TO 5
    READ X
    PRINT X
NEXT
RESTORE

' DEF FN — 单行自定义函数
DEF FNAdd(a, b) = a + b
PRINT FNAdd(3, 5)

' COMMON — 模块共享变量
COMMON SHARED Score, Lives

' REDIM/PRESERVE — 动态数组重定义
DIM A(10)
REDIM A(20)
REDIM PRESERVE A(30)    ' 保留原有数据
```

### 3.12 其他

```basic
SLEEP [seconds]     ' 暂停
SWAP A, B           ' 变量交换
ERASE ArrayName     ' 清除数组
SYSTEM              ' 退出程序
exit(code)          ' 退出并返回code给系统 (SYSCALL 3)
END                 ' 程序结束
DEFINT A-C          ' 类型声明 (兼容)
DEFSNG D-F
DEFSTR S

' OPTION BASE — 数组下标起始
OPTION BASE 0       ' 数组从0开始 (默认)
OPTION BASE 1       ' 数组从1开始

' INKEY$ — 无阻塞按键检测
K$ = INKEY$
IF K$ <> "" THEN PRINT "Key: "; K$

' DECLARE — 前置声明子程序
DECLARE SUB MySub(x)
DECLARE FUNCTION MyFunc(x, y)

' LOCAL / STATIC / SHARED / COMMON
SUB MyProc()
    LOCAL temp        ' 局部变量
    STATIC count      ' 静态变量 (持久)
    SHARED globalVar  ' 共享变量
END SUB
COMMON SHARED Score, Lives  ' 模块共享
```

### 3.13 调用约定与内联汇编

```basic
' __stdcall 调用约定 — 参数全部栈传递，被调用者清理
SUB MyFunc STDCALL(a, b)
    MyFunc = a + b
END SUB

' ASM — 内联 VML 汇编
ASM "MOVE R0, #42"
ASM "SYSCALL #4"

' CHIPASM — 内联目标架构汇编 (6502/Z80/8051 等)
CHIPASM "LDA #$42"
CHIPASM "STA $0400"
```

### 3.14 高级图形 (GET/PUT/PALETTE/VIEW/WINDOW)

```basic
' PALETTE — 设置调色板颜色 (SCREEN 13 模式)
PALETTE 0, 0, 0, 0         ' 索引0=黑色
PALETTE 1, 63, 0, 0        ' 索引1=红色
PALETTE 2, 0, 63, 0        ' 索引2=绿色

' GET — 保存屏幕区域到数组
DIM buffer(200)
CIRCLE (50,50), 10, 2
GET (40,40)-(60,60), buffer

' PUT — 从数组恢复屏幕区域
PUT (100,100), buffer       ' 直接覆盖
PUT (100,100), buffer, 1    ' XOR 模式

' VIEW — 设置图形视口
VIEW (10,10)-(200,100)       ' 视口区域
VIEW (10,10)-(200,100), 2, 4 ' 带颜色边框

' WINDOW — 设置逻辑坐标系统
WINDOW (0,0)-(1,1)           ' 逻辑坐标 0-1
CIRCLE (0.5,0.5), 0.3       ' 在逻辑坐标中绘制
```

### 3.15 PRINT USING — 格式化输出

```basic
PRINT USING "###.##"; 12.345     ' 输出: 12.35
PRINT USING "####"; 42            ' 输出:   42
PRINT USING "$$###.##"; 12.3     ' 输出: $ 12.30
```

## 4. 与标准 QBASIC 的差异

### 不支持的特性
- `SHELL` / `CHAIN` / `RUN` — 外部程序调用（裸机环境不支持）
- `LINE INPUT` — 行输入（可替代为 INPUT）
- `FIELD` / `LSET` / `RSET` — 随机文件字段操作

### 扩展特性
- 完整的中文标识符和字符串
- VGA 24位彩色图形扩展 (VGAPUTPIXEL 等)
- 设备描述文件支持 (devices/*.json)
- 多平台模拟 (PC, Apple II, C64 等)
- `POKE` / `PEEK` 内存映射 I/O
- `__stdcall` 调用约定，支持跨语言函数互操作
- **PRINT 输出目标根据 SCREEN 模式自动切换**：图形模式下仅渲染到 VGA 帧缓冲，控制台无输出（与 QBASIC 的物理 VGA 显卡行为一致）

## 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (SINGLE/!) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (DOUBLE/#) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数处理模式 |

### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `SINGLE` / `!` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到浮点声明时报告编译错误。

### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `DOUBLE` / `#` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到浮点声明时报告编译错误。

### 64位整数 (int64)

64 位整数类型在本语言中由 VML 编译层支持，BASIC 标准中未定义对应的显式类型，但编译器预留了通过 `--int64` 扩展支持的可能性。

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数扩展。

### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## 5. 硬件接口

### 内存映射 I/O
| 地址 | 用途 |
|------|------|
| 0x60 | 键盘数据端口 |
| 0x64 | 键盘状态端口 |
| 0x300-0x304 | 鼠标 X/Y/按钮 |

### 系统调用
| 编号 | 功能 |
|------|------|
| 1 | 输出字符串 |
| 4 | 输出字符 |
| 5 | 输入字符 |
| 7 | 输入整数 |
| 100-104 | 设备管理 |
| 110-114 | 文件操作 |

### 设备管理 (syscall 100-104)
```
100: 打开设备  R0=设备名地址 → R0=句柄
101: 关闭设备  R0=句柄
102: 读设备    R0=句柄, R1=缓冲区, R2=长度
103: 写设备    R0=句柄, R1=数据, R2=长度
104: 控制设备  R0=句柄, R1=命令, R2=数据, R3=长度
```

## 6. 编译

```bash
# 编译
dotnet run --project VMLPrepares/BasicCompiler input.bas -o output.vml

# 运行 (字符模式)
dotnet run --project VMLEmulators/ConsoleEmulator -- --run output.vml

# 运行 (VGA 模式)

# 运行 (GUI 图形模式)
dotnet run --project VMLEmulators/FullDevicesEmulator
```

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

---

## v1.66.33 改进 (2026-08-01)

### 全方言 90%+ 完成度
- **PureBasic**: GLOBAL → SHARED, PROCEDURE → SUB (Lexer 映射)
- **ChipBasic**: PINMODE/DIGITALWRITE/DIGITALREAD → GpioStatement
- **FreeBasic**: ENUM 编译通过, PTR/CAST/EXTENDS/OPERATOR 识别
- **TrueBasic**: MAT/ZER/CON 识别

### CLASS/OOP 支持
- CLASS 关键字 → 映射到 TYPE_KW 统一解析
- 类声明: CLASS name ... END CLASS (字段 + 方法)
- 构造函数: CONSTRUCTOR 块
- 方法: METHOD name(params) ... END METHOD
- 属性: PROPERTY (token 已注册)
- 析构函数: DESTRUCTOR (token 已注册)

### 词法分析器
- **116 个关键字** (141 个 TokenType)
- 支持 7 种方言的特有关键字
- 关键字大小写不敏感 (LexerBase.ReadIdentifier)

### 预处理器
- 支持 `#param lib("name")` / `#param path("dir")` 指令
- 支持 `#include`, `#define`, `#undef`, `#if`/`#else`/`#endif`
- 每个方言自动定义宏: `__QBASIC__`, `__FREEBASIC__` 等

### 测试覆盖
- **63 个单元测试** (Lang_BASIC.cs)
- 22 个多方言测试 (7 方言 × 特有关键字)
- 5 个 TYPE/CLASS 结构体测试
- FreeBasic 示例程序 (14 个测试文件)

### 已知限制
- CLASS 方法体尚未链接到调用点 (Phase 3)
- EXTENDS/INTERFACE 语义待实现
- GPIO 语句编译通过但未生成实际调用
- PTR/CAST 指针语义待实现

共享库已提供宽字符串转换函数 (wchar.h/uchar.h)，各语言编译器可按需使用。
