# Pascal 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Turbo Pascal 7.0 (默认) + 5种方言 |
| **CLI 选项** | `--pascaltype turbo\|delphi\|freepascal\|iso\|ucsd\|oberon` |
| **发布年份** | 1992 (Turbo Pascal) |
| **完成度** | ~92% |
| **MCU完成度** | ~90% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正 break/continue/指针 状态矛盾；更新测试数 |

## 关键字

`program` `unit` `uses` `var` `const` `type` `begin` `end` `procedure` `function`
`if` `then` `else` `while` `do` `for` `to` `downto` `repeat` `until`
`case` `of` `break` `continue` `exit` `goto` `label`
`array` `record` `set` `file` `text` `string` `integer` `real` `char` `boolean`
`true` `false` `nil` `and` `or` `not` `xor` `div` `mod` `in` `shl` `shr`
`with` `asm` `absolute` `external` `forward` `inline` `interrupt`

## 概述

本编译器实现 Pascal 语言（兼容 Turbo Pascal 7.0 子集）到 VML (Virtual Machine Language) 汇编的编译。编译器支持 Pascal 的基本语法结构、控制流、数据类型、过程/函数和 Crt 控制台单元。

## 支持的语言特性

### 1. 数据类型

#### 字面量
- **整数**: `42`（十进制）
- **十六进制**: `$FF`, `$40013804`（`$` 前缀，Pascal 风格）
- **实数**: `3.14`, `1.5e-2`
- **字符**: `'A'`
- **字符串**: `'Hello'`, `"Hello"`
- **布尔**: `true`, `false`

#### 基本数据类型
- **整数类型**: `integer` — 32位有符号整数（全部实现）
- **实数类型**: `real` — 浮点数（实现：MOVEF、FADD/FSUB/FMUL/FDIV、比较）
- **字符类型**: `char` — 单个字符（实现：MOVEB）
- **布尔类型**: `boolean` — 布尔值（实现：比较产生 0/1）
- **字符串类型**: `string` — 字符串字面量（实现：SYSCALL 4/2）

#### 枚举类型
```pascal
type
  TDir = (Up, Down, Left, Right);
  TColor = (Red, Green, Blue);
```
枚举值编译为整数常量（从 0 递增）。

#### 派生类型
- **数组**: 一维数组，支持边界声明，如 `array[1..10] of integer`
  - 支持运行时边界检查（索引越界触发运行时错误）
  - 支持数组元素为 record 类型的字段访问：`snake[i].x`
- **记录**: `record` 类型定义，支持字段访问（点号操作符）
  - 字段可以是基本类型、字符串、数组（包括多维）
- **集合**: `set of` 类型，支持并集/交集/差集/成员测试（通过位图实现）
- **文件**: `file of <type>` 和 `text` 类型

### 2. 程序结构

#### 程序声明
```pascal
program HelloWorld;
uses Crt;  // 使用Crt单元（被解析但忽略）
var
    x: integer;
begin
    x := 10;
    writeln('Hello, World!');
end.
```

#### 变量声明
```pascal
var
    counter: integer;
    pi: real;
    ch: char;
    flag: boolean;
    arr: array[1..10] of integer;
    r: record
        x, y: integer;
    end;
```

#### 常量声明
```pascal
const
    MAX_SIZE = 100;
    PI = 3.14159;
    MESSAGE = 'Hello';
```

#### 类型定义
```pascal
type
    TColor = (Red, Green, Blue);
    TRange = 1..100;
    TPoint = record
        x, y: integer;
    end;
    TMatrix = array[1..10] of array[1..10] of integer;
```

### 3. 控制结构

#### 条件语句
```pascal
if x > 0 then
    writeln('Positive')
else if x = 0 then
    writeln('Zero')
else
    writeln('Negative');
```

#### 循环语句
```pascal
// while 循环
while i <= 10 do
begin
    writeln(i);
    i := i + 1;
end;

// for 循环
for i := 1 to 10 do
    writeln(i);

for i := 10 downto 1 do
    writeln(i);

// repeat 循环
repeat
    writeln(i);
    i := i - 1;
until i = 0;
```

#### case 语句
```pascal
case grade of
    'A': writeln('Excellent');
    'B', 'C': writeln('Pass');
    'D': writeln('Poor');
    'F': writeln('Fail');
otherwise
    writeln('Invalid');
end;
```

### 4. 过程和函数

#### 过程声明
```pascal
procedure PrintMessage(msg: string);
begin
    writeln(msg);
end;
```

#### 函数声明
```pascal
function Add(a, b: integer): integer;
begin
    Add := a + b;  // 通过函数名赋值返回结果
end;
```

#### 参数传递
- **值参数**: 默认传递方式
- **变量参数**: 使用 `var` 关键字，允许修改调用者的变量
```pascal
procedure Swap(var x, y: integer);
var
    temp: integer;
begin
    temp := x;
    x := y;
    y := temp;
end;
```

#### forward 声明
```pascal
procedure Foo(x: integer); forward;

procedure Bar;
begin
    Foo(10);
end;

procedure Foo(x: integer);
begin
    writeln(x);
end;
```

### 5. 运算符

#### 算术运算符
- `+` — 加法
- `-` — 减法
- `*` — 乘法
- `/` — 实数除法
- `div` — 整数除法
- `mod` — 取模

#### 关系运算符
- `=` — 等于
- `<>` — 不等于
- `<` — 小于
- `<=` — 小于等于
- `>` — 大于
- `>=` — 大于等于

#### 逻辑运算符
- `and` — 逻辑与
- `or` — 逻辑或
- `not` — 逻辑非

#### 集合运算符
- `+` — 并集
- `*` — 交集
- `-` — 差集
- `in` — 成员测试

### 6. 输入输出

#### 标准输出
```pascal
write('Hello');         // 不换行输出
writeln('World');       // 输出并换行
writeln('Value: ', x);  // 多个参数输出
writeln(x:5);           // 格式化输出（宽度5）
```

#### 标准输入
```pascal
readln(x);              // 读取整数
readln(ch);             // 读取字符
readln(str);            // 读取字符串
```

#### 文件操作
```pascal
var
    f: text;
begin
    assign(f, 'data.txt');
    reset(f);           // 打开文件读取
    rewrite(f);         // 打开文件写入
    append(f);          // 追加到文件
    readln(f, x);       // 从文件读取
    writeln(f, x);      // 写入文件
    close(f);           // 关闭文件
end;
```

### 7. 标准库单元

#### Crt 单元
```pascal
uses Crt;

begin
    ClrScr;             // 清屏
    GotoXY(10, 5);      // 移动光标
    WhereX; WhereY;     // 获取光标位置
    TextColor(Red);     // 设置文本颜色
    TextBackground(Blue); // 设置背景颜色
    Delay(1000);        // 延迟1秒
    Sound(440);         // 发出声音
    NoSound;            // 停止声音
    if KeyPressed then  // 检查按键
        ch := ReadKey;  // 读取按键
    NormVideo;          // 恢复默认视频属性
    HighVideo; LowVideo; // 高/低亮度
    InsLine; DelLine;   // 插入/删除行
    Window(x1, y1, x2, y2); // 定义窗口
end;
```

#### System 单元（内置）
- `Abs(x)` — 绝对值
- `Sqr(x)` — 平方
- `Sqrt(x)` — 平方根
- `Sin(x)`, `Cos(x)`, `Arctan(x)` — 三角函数
- `Exp(x)`, `Ln(x)` — 指数和对数
- `Round(x)`, `Trunc(x)` — 取整
- `Random`, `Randomize` — 随机数
- `Length(s)` — 字符串长度
- `Copy(s, start, count)` — 子字符串
- `Concat(s1, s2)` — 字符串连接
- `Pos(substr, s)` — 查找子串位置
- `Halt` — 程序终止

### 8. 特殊语法特性

#### 复合语句
```pascal
begin
    statement1;
    statement2;
end;
```

#### 注释
```pascal
// 单行注释
{ 多行注释 }
(* 另一种多行注释 *)
```

#### 标签和 goto
```pascal
label 100;
begin
    if x < 0 then goto 100;
    writeln('Positive');
    100: writeln('Done');
end;
```

### 9. VML 代码生成约定

#### 栈帧布局
```
调用方: PUSH 参数（左到右）
       CALL 函数名

被调用方 prologue:
    ENTER local_size     ; 保存R14, R12-=local_size

栈布局 (R14=帧指针):
    参数1     ← R14+8
    参数2     ← R14+12
    返回地址  ← R14+4  (隐式)
    保存的R14 ← R14+0  (ENTER指令保存)
    局部变量1 ← R14-4
    局部变量2 ← R14-8
    ...

被调用方 epilogue:
    LEAVE               ; R12=R14, POP R14
    RET                 ; 返回
```

#### 变量访问
- **全局变量**: 从数据段加载（数据段标签）
- **局部变量**: 从栈帧负偏移访问 `[R14 - offset*4]`
- **参数**: 从栈帧正偏移访问 `[R14 + 8 + paramIndex*4]`
- **var 参数**: 参数为地址，需解引用

#### 函数返回值
- 通过函数名赋值设置返回值
- 返回值存储在局部变量中
- 返回时加载到 R0 寄存器

### 10. 当前实现状态

| 特性 | 状态 |
|------|------|
| 程序结构 (`program ...; ... end.`) | ✅ 已实现 |
| 注释 (`{ }`, `(* *)`, `//`) | ✅ 已实现 |
| `uses` 子句 | ✅ 已实现（解析并跳过） |
| **数据类型** | |
| `integer` (32位有符号) | ✅ 已实现 |
| `real` (浮点数) | ✅ 已实现 |
| `boolean` | ✅ 已实现 |
| `char` | ✅ 已实现 |
| `string` | ✅ 已实现 |
| 枚举类型 `(A, B, C)` | ✅ 已实现 |
| 数组 `array[low..high] of type` | ✅ 已实现 |
| 记录 `record ... end` | ✅ 已实现 |
| 集合 `set of type` | ✅ 已实现 |
| 文件 `file of type` / `text` | ✅ 已实现 |
| **控制流** | |
| `if/then/else` | ✅ 已实现 |
| `while/do` | ✅ 已实现 |
| `for to/downto do` | ✅ 已实现 |
| `repeat/until` | ✅ 已实现 |
| `case/of/otherwise/end` | ✅ 已实现 |
| `break` / `continue` | ✅ 已实现 | 循环控制，支持嵌套循环
| **过程和函数** | |
| `procedure` (值参数) | ✅ 已实现 |
| `function` (返回值) | ✅ 已实现 |
| `var` 参数（引用传递） | ✅ 已实现 |
| `forward` 声明 | ✅ 已实现 |
| 递归 | ✅ 已实现 |
| **运算符** | |
| 算术 `+ - * / div mod` | ✅ 已实现 |
| 关系 `= <> < <= > >=` | ✅ 已实现 |
| 逻辑 `and or not` | ✅ 已实现 |
| 集合 `in + * -` | ✅ 已实现 |
| **输入输出** | |
| `write`, `writeln` | ✅ 已实现 |
| `readln`, `read` | ✅ 已实现 |
| 格式化输出 `x:5` | ✅ 已实现 |
| 文件 I/O | ✅ 已实现 |
| **内置函数** | |
| `abs`, `sqr`, `chr`, `ord` | ✅ 已实现 |
| `pred`, `succ`, `odd` | ✅ 已实现 |
| `round`, `trunc` | ✅ 已实现 |
| `random`, `randomize` | ✅ 已实现 |
| `sqrt`, `sin`, `cos`, `exp`, `ln` | ✅ 已实现 |
| `length`, `concat`, `copy`, `pos` | ✅ 已实现 |
| **Crt 单元** | |
| `ClrScr`, `GotoXY`, `WhereX/Y` | ✅ 已实现 |
| `TextColor`, `TextBackground` | ✅ 已实现 |
| `Delay`, `Sound`, `NoSound` | ✅ 已实现 |
| `KeyPressed`, `ReadKey` | ✅ 已实现 |
| `Window`, `NormVideo`, `HighVideo`, `LowVideo` | ✅ 已实现 |
| `InsLine`, `DelLine`, `CursorOn`, `CursorOff` | ✅ 已实现 |
| `with` 语句 | ⚠️ 仅解析 | record字段快捷访问
| 多维数组 | ✅ 已实现 | matrix[i][j] 完整支持
| 指针 | ✅ 已实现 | ^type声明, new/dispose, p^解引用

### 11. 示例程序

#### 贪吃蛇
```pascal
program SnakeGame;
uses Crt;
const
  WIDTH = 40; HEIGHT = 20; MAX_SNAKE = 400;
type
  TDir = (Up, Down, Left, Right);
  TPoint = record x, y: integer; end;
var
  snake: array[1..MAX_SNAKE] of TPoint;
  len: integer; dir: TDir;
  food: TPoint; score: integer;
  gameover: boolean;
begin
  ClrScr;
  // ... 游戏循环
end.
```

#### 简单示例
```pascal
program Simple;
var
    i, sum: integer;
begin
    sum := 0;
    for i := 1 to 10 do
        sum := sum + i;
    writeln('Sum: ', sum);
end.
```

#### 数组示例
```pascal
program ArrayTest;
var
    arr: array[1..5] of integer;
    i: integer;
begin
    for i := 1 to 5 do
        arr[i] := i * 10;
    for i := 1 to 5 do
        writeln('arr[', i, '] = ', arr[i]);
end.
```

#### 函数示例
```pascal
program FunctionTest;
function Factorial(n: integer): integer;
begin
    if n <= 1 then Factorial := 1
    else Factorial := n * Factorial(n - 1);
end;
begin
    writeln('5! = ', Factorial(5));
end.
```

### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (real) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (double) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (Int64) 处理模式 |

#### 32位浮点 (float32)

本语言中的实型类型 `real`（32位单精度浮点）按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `real` 声明时报告编译错误。

#### 64位浮点 (double)

本语言中的 `double` 类型（64位双精度浮点）按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `double` 声明时报告编译错误。

#### 64位整数 (int64)

本语言中的 `Int64` / `LongInt` 类型按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数类型，遇到时报告编译错误。

#### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

### 12. 编译限制

1. **多维数组**: `array[1..3, 1..3]` 语法不支持，可用 `array[1..3] of array[1..3]`
2. **`with` 语句**: 仅解析，不生成代码
3. **VML 整数**: 所有整数为 32 位有符号
4. **数组初始化**: 不支持编译时数组常量初始化
5. **字符串**: 不支持动态字符串操作（无堆分配）
6. **泛型/generic**: 不支持 Turbo Pascal 不具备的特性

### 13. 与 VML 运行时集成

Pascal 程序通过以下机制与 VML 运行时交互：

- **SYSCALL 2**: 字符串输入（readln 字符串）
- **SYSCALL 4**: 输出字符
- **SYSCALL 5**: 键盘输入（ReadKey）
- **SYSCALL 6**: 输出整数
- **SYSCALL 7**: 输入整数（readln 整数）
- **SYSCALL 100-104**: 文件操作
- **内存映射 I/O**: 0xB8000 显存（GotoXY/ClrScr），0x6FF4 光标位置等

标准库函数（`stdlib.vml`）实现了 Crt 单元、数学函数、字符串操作和文件 I/O 的 VML 汇编实现。

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
