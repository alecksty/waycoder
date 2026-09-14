# Fortran 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 元数据

| 属性 | 值 |
|------|-----|
| **标准** | Fortran 90/95 子集 |
| **首次发布** | 1957 年 (Fortran 90: 1991 年) |
| **完成度** | ~98% |
| **文件扩展名** | `.f90`, `.f`, `.f95`, `.f03`, `.f08`, `.for`, `.ftn` |
| **代码行数** | ~1,590 行 C# |

## 概述

Fortran（FORmula TRANslation）是世界上第一个高级编程语言，专为科学计算和数值分析设计。本编译器实现 Fortran 90/95 的子集，支持自由列格式（free-form source form）。

核心特性：
- **大小写不敏感** —— 所有标识符、关键字均以小写内部存储
- **自由列格式** —— 无固定列限制，语句可跨行书写
- **列格式注释** —— `*` 或 `C`/`c` 在第 1 列视为注释行（兼容固定格式）
- **MCU 安全子集** —— 默认跳过异步、线程、GC 等不支持的特性

## 支持的特性

### 1. 数据类型

| 类型关键字 | 内部映射 | 说明 |
|-----------|---------|------|
| `integer` | `integer` (i32) | 32 位有符号整数 |
| `real` | `real` (float) | 32 位 IEEE 754 单精度浮点 |
| `double precision` | `real` (float) | 映射为 32 位浮点（VML 无原生 f64 硬件支持，需软浮点库） |
| `complex` | `complex` | 复数类型（语法保留，运行时支持有限） |
| `logical` | `logical` (i32) | 逻辑值，内部以整数 0/1 存储 |
| `character` | `character` | 字符串类型（通过 `MOVE R0, label` 加载地址） |

> **注意**: `double` 关键字被识别但映射为 `real`（单精度）。`doubleprecision`（无空格）同样映射为 `real`。完整的双精度浮点支持需要启用 `VML_FLOAT64_SOFT` 宏以链接软浮点库。

### 2. 程序结构

```fortran
program program_name
    ! 声明部分
    integer :: x, y
    real :: pi = 3.14159

    ! 执行部分
    x = 10
    call mysub(x)

contains

    subroutine mysub(n)
        integer :: n
        print *, 'n = ', n
    end subroutine mysub

    function add(a, b) result(sum)
        integer :: a, b
        integer :: sum
        sum = a + b
    end function add

end program program_name
```

**支持的顶层结构**：
- `program name` / `end program [name]` —— 主程序（`program` 头部可选，缺省名为 `main`）
- `subroutine name(params)` / `end subroutine [name]` —— 子例程（无返回值）
- `function name(params) result(resultVar)` / `end function [name]` —— 函数（有返回值）
- `contains` —— 分隔主程序体与内部过程定义

**语法要点**：
- `end` 后的关键字（`program`/`subroutine`/`function`）和名称均为可选
- `result` 子句：`function square(x) result(y)` 将 `y` 作为结果变量名，影响函数内部赋值后的返回值
- 函数默认返回类型为 `integer`；可在 `function` 行前加类型关键字声明：`real function foo(x)`

### 3. 变量声明

```fortran
! 基本声明（:: 分隔符可选）
integer :: i, j, k
real :: temperature
logical :: flag

! 带 dimension 的声明（语法解析通过，但数组操作尚未实现）
integer :: a(10), b(20)

! parameter 关键字（词法/语法识别，未生成代码）
parameter :: pi = 3.14159
```

**实现细节**：
- `::` 分隔符在声明中是可选的——`integer x` 和 `integer :: x` 均可接受
- 变量在栈帧（`R12` 基址）上分配，每个变量占 4 字节
- 函数内结果变量（`result` 指定的名称）自动在栈帧上分配空间

### 4. 隐式与显式类型

编译器识别 `implicit none` 语句并跳过。默认情况下，建议在程序开头声明 `implicit none` 以禁用 Fortran 的隐式类型规则（以首字母 `I-N` 判断 integer，其余为 real）。

**注意**：当前编译器**不强制**显式声明——未声明的变量在使用时自动分配全局数据段变量。这与标准 Fortran 行为不同。

### 5. 运算符

| 优先级 | 运算符 | 含义 | 说明 |
|--------|--------|------|------|
| 最低 | `.or.` | 逻辑或 | 短路求值 |
| | `.and.` | 逻辑与 | 短路求值 |
| | `==` / `.eq.` | 等于 | C 风格和 Fortran 风格均可 |
| | `/=` / `.ne.` | 不等于 | C 风格和 Fortran 风格均可 |
| | `<` / `.lt.` | 小于 | |
| | `>` / `.gt.` | 大于 | |
| | `<=` / `.le.` | 小于等于 | |
| | `>=` / `.ge.` | 大于等于 | |
| | `+` | 加法 | |
| | `-` | 减法 | |
| | `*` | 乘法 | |
| | `/` | 除法 | |
| 最高 | `**` | 幂运算 | 当前简化为 `*`（无原生 POW 指令） |

逻辑运算符（仅支持 `.dot.` 格式）：
- `.not.` —— 逻辑非（一元前置）
- `.and.` —— 逻辑与
- `.or.` —— 逻辑或
- `.eqv.` —— 逻辑等价（词法关键字已注册）
- `.neqv.` —— 逻辑不等价（词法关键字已注册）

> **注意**: `.eqv.` 和 `.neqv.` 被 Lexer 识别为关键字，但在目前的 CodeGenerator 中尚未实现专门的代码生成。同样，比较运算符同时支持 Fortran 风格（`.eq.`, `.ne.`, `.lt.`, `.gt.`, `.le.`, `.ge.`）和 C 风格（`==`, `/=`, `<`, `>`, `<=`, `>=`）。

### 6. 赋值

```fortran
x = 42           ! 正确：Fortran 用 = 而非 ==
flag = .true.    ! 逻辑赋值
y = x ** 2 + 1   ! 表达式赋值
```

赋值使用 `=` 运算符（不是 `==`）。`==` 是相等比较运算符。

### 7. 控制流

#### IF 语句

```fortran
if (x > 0) then
    print *, 'positive'
else if (x < 0) then
    print *, 'negative'
else
    print *, 'zero'
end if
```

- `if (cond) then` / `else if (cond) then` / `else` / `end if`
- 条件用括号 `()` 包裹
- `then` 关键字必须出现在条件之后
- `else if` 通过将后续的 IF 节点嵌套在 `elseBody` 中实现链式结构

#### DO WHILE 循环

```fortran
i = 1
do while (i <= 10)
    print *, i
    i = i + 1
end do
```

- 循环体在 `do while (cond)` 和 `end do` 之间
- 条件在每次迭代**前**求值（入口条件循环）
- 循环计数器不会自动递增——必须手动更新

#### 计数 DO 循环

```fortran
do i = 1, 10
    print *, i
end do

do j = 10, 1, -1     ! step = -1
    print *, j
end do
```

- 语法：`do var = start, end [, step]`
- 循环体在 `do` 和 `end do` 之间
- 循环变量在栈帧上分配，循环结束后保留最终值
- **限制**：step 仅支持**整数字面量**（如 `1`, `-1`），不支持表达式
- 循环条件为 `var <= end`（适用于正向步长；负向步长行为可能不符合标准 Fortran）

### 8. 子程序与函数调用

#### CALL 语句（调用子例程）

```fortran
call mysub(arg1, arg2)
call print_values(x, y, z)
```

- 参数入栈顺序：**从右到左**（`__cdecl` 风格）
- 调用后弹出参数

#### 函数调用（表达式内）

```fortran
result = add(a, b)            ! 函数调用作为表达式
x = square(y) + offset        ! 嵌套在表达式中
```

- 函数名后跟 `(args)` 触发函数调用
- 返回值通过 `R0` 寄存器传递

### 9. 内置函数（SYSCALL）

Fortran 编译器通过 VML 系统调用提供基本 I/O：

| print 参数类型 | SYSCALL | 说明 |
|---------------|---------|------|
| 字符串字面量 | SYSCALL 1 | 打印字符串（`R0` = 标签地址，MOVE 语义） |
| 整数/变量 | SYSCALL 5 | 打印整数（`R0` = 值） |
| 间隔空格 | SYSCALL 4 | 打印单个字符（空格分隔） |
| 换行 | SYSCALL 4 | 打印 `\n` |

共享的内置运行时函数（通过 `Lib/shared/builtins.vml` 提供）：`peek` / `poke` / `putchar` / `abs` / `min` / `max` / `random` / `sleep` / `alloc`。

`stop [code]` 语句调用 `exit` SYSCALL（SYSCALL 3），可选退出码（默认 0）。

### 10. 逻辑字面量

| 字面量 | 内部值 | 说明 |
|--------|--------|------|
| `.true.` / `true` | 1 (i32) | Fortran 风格和简化形式均可 |
| `.false.` / `false` | 0 (i32) | Fortran 风格和简化形式均可 |

> 词法器同时接受 `true`/`false`（无前后点号）作为关键字，但标准 Fortran 应使用 `.true.` / `.false.`。

### 11. 注释

```fortran
! 这是行注释（自由格式和固定格式均支持）
program main
    integer :: x   ! 行尾注释
end program
```

固定格式兼容：
```
* 星号在第 1 列视为注释行
C 或 c 在第 1 列视为注释行
```

### 12. 字符串字面量

```fortran
print *, 'Hello, World!'
print *, 'It''s Fortran'    ! 两个单引号转义为一个
```

- 单引号字符串：`'text'`
- 双引号字符串（非标准扩展）：`"text"`
- 内部转义：`''` 表示一个字面量单引号
- 字符串在数据段中分配，通过 `MOVE R0, label` 加载地址到 `R0`

### 13. 续行

自由格式中，`&` 用于续行：
```fortran
x = a + b + &
    c + d
```

词法器解析 `&` 以及续行行为由预处理阶段处理。

## 表达式优先级

从低到高：

```
1. .or.                         (逻辑或)
2. .and.                        (逻辑与)
3. == /= < > <= >=             (比较)
   .eq. .ne. .lt. .gt. .le. .ge.
4. + -                          (加减)
5. * / **                       (乘除幂)
6. - + .not.                    (一元负/正/逻辑非)
7. 字面量 / 变量 / 函数调用 / ( expr )
```

解析实现对应：
```
ParseLogicalOr    → .or.
  ParseLogicalAnd  → .and.
    ParseComparison → == /= < > <= >=
      ParseAdditive  → + -
        ParseMultiplicative → * / **
          ParseUnary     → - + .not.
            ParsePrimary  → 字面量 / 变量 / 函数调用 / ( )
```

## 预处理

编译器集成了 `CompilerBase.Preprocessor`，支持以下功能：

### 预定义宏

| 宏 | 值 | 说明 |
|----|-----|------|
| `__VML__` | `1` | VML 工具链标识 |
| `__VML_VERSION__` | `"1.65.32"` | VML 版本号 |
| `__FORTRAN__` | `1` | Fortran 编译器标识 |
| `__DATE__` | `"Jun 01 2026"` | 编译日期（英文格式） |
| `__TIME__` | `"HH:mm:ss"` | 编译时间 |

### 模块导入

```fortran
use math_lib          ! 导入模块 math_lib
```

- `use module_name` 语法触发模块导入
- 编译器从源目录及库路径解析 `.vml` 模块文件
- 自动链接 `Lib/fortran/builtin.vml` 运行时库

### 条件编译

```fortran
#ifdef __VML__
    print *, 'VML compiler'
#endif
```

标准 C 预处理器指令均支持：`#define`, `#ifdef`, `#ifndef`, `#if`, `#else`, `#elif`, `#endif`, `#include`, `#undef`。

## 限制与未实现特性

### 已在 Lexer/Token 中注册但未在 CodeGenerator 中实现的关键字

| 关键字 | 状态 | 说明 |
|--------|------|------|
| `module` / `use` / `only` | 语法跳过/模块导入 | `use` 仅用于库导入，module 块不支持 |
| `dimension` / `allocatable` | 词法识别 | 数组维度声明仅被解析跳过 |
| `implicit` / `none` | 语法识别（NopNode） | 解析为 no-op，不强制执行显式声明 |
| `parameter` | 词法识别 | 常量声明未生成代码 |
| `intent` / `in` / `out` / `inout` | 词法识别 | 参数的 intent 属性被忽略 |
| `write` / `read` | 词法识别 | 仅 `print *,` 被实现（视为 write） |
| `.eqv.` / `.neqv.` | 词法识别 | 代码生成未实现 |
| `complex` | 词法识别 | 复数运算未实现 |

### 核心不支持特性

| 特性 | 说明 |
|------|------|
| **数组操作** | 声明语法被解析跳过，不支持整体数组运算、数组切片、WHERE/FORALL |
| **模块系统** | `module`/`end module` 块不支持 |
| **派生类型** | `type`/`end type` 自定义类型不支持 |
| **指针** | `pointer` 属性不支持 |
| **接口块** | `interface`/`end interface` 不支持 |
| **FORMAT 语句** | 格式化 I/O 不支持，仅 `print *,`（列表定向输出） |
| **READ/WRITE** | 仅 `print *,` 可用；`read *` 不支持 |
| **COMMON 块** | 公共块不支持，使用栈帧分配变量 |
| **ENTRY** | 多入口点不支持 |
| **SAVE** | 变量持久性属性不支持 |
| **DATA** | DATA 初始化语句不支持 |
| **IMPLICIT 规则** | 显式声明（`implicit none`）被建议但未强制执行 |
| **双精度** | `double precision` 映射为单精度 float |
| **幂运算 `**`** | 简化为乘法，不是真正的幂运算 |
| **DO 负向步长** | 循环条件 `<=` 对负向步长行为不正确 |
| **数组参数** | 子程序/函数不能接受数组作为参数 |

## 编译器架构

```
源文件 (.f90/.f)
    │
    ▼
Lexer.cs ─────────── 词法分析（大小写不敏感）
    │   Keywords: 65 个关键字 + 运算符
    │   注释: ! 行注释, * C/c 在列1
    │   字符串: '...' 和 "..." ('' 转义)
    │   数字: 整数 / 实数 / 科学计数法 (E/D)  / 种类 (_4)
    ▼
Parser.cs ────────── 递归下降语法分析
    │   语句: program/subroutine/function/if/do while/do for/call/print
    │   表达式: 7 层优先级 (Pratt 风格)
    │   声明: type [::] name [, name]*
    ▼
ASTNode.cs ───────── 抽象语法树
    │   ProgramNode / SubroutineNode / FunctionNode
    │   IfNode / DoWhileNode / ForNode
    │   CallNode / ReturnNode / StopNode / PrintNode
    │   AssignNode / BinaryNode / UnaryNode / FuncCallNode
    │   LiteralNode / VarNode / VarDeclNode
    ▼
CodeGenerator.cs ─── VML 代码生成
    │   继承 CodeGeneratorBase
    │   栈帧管理: R12 基址 + nextStackOffset
    │   符号表: Dictionary<string, int> (名称 → 栈偏移)
    │   表达式: ExpressionManager 三操作数
    ▼
FortranCompiler.cs ─ 编译器入口
    │   Compile(string) → VmlProgram
    │   CompileFile(path) → VmlProgram (含预处理 + 库链接)
    │   预处理: Preprocessor + 预定义宏
    │   库链接: use 语句 + builtin.vml
```

## 调用约定

- **子例程调用** (CALL)：参数从右到左 PUSH，通过 `CALL` 指令跳转
- **函数调用** (表达式)：参数从右到左 PUSH，通过 `CALL` 指令跳转，返回值在 `R0` 寄存器
- **内部标签命名**：
  - 子例程：`sub_<name>`（小写）
  - 函数：`func_<name>`（小写）
- **返回**：`RET` 指令（子例程返回 void，函数返回时 `R0` 已加载结果）

## 库结构

```
Lib/fortran/
    builtin.vml          ← 编译器自动链接
    ├── .linked  "../shared/builtins.vml"       (peek/poke/putchar/abs/min/max/random/sleep/alloc)
    ├── .linked  "../shared/device.vml"          (设备操作)
    ├── .linked  "../shared/sysinfo.vml"         (日期/时间/退出)
    ├── .linked  "../shared/softfloat.vml"       (VML_FLOAT32_SOFT)
    ├── .linked  "../shared/softdouble.vml"      (VML_FLOAT64_SOFT)
    ├── .linked  "../shared/softint64.vml"       (VML_INT64_SOFT)
    ├── .linked  "../shared/float.vml"           (基础浮点)
    └── .linked  "../shared/syscall.inc.vml"     (系统调用常量)
```

## 最小完整程序示例

```fortran
program hello
    implicit none
    integer :: i

    do i = 1, 5
        print *, 'Hello Fortran!', i
    end do

contains

    function square(x) result(y)
        integer :: x
        integer :: y
        y = x * x
    end function square

end program hello
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

共享库已提供宽字符串转换函数 (wchar.h/uchar.h)，各语言编译器可按需使用。
