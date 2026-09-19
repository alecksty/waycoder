# C

«bold»最完整的一条路«/»：绘图、输入、音效、存档、手柄全都用得上。仓库里那几个完整的游戏（俄罗斯方块、五子棋、吃豆人）都是 C 写的。

如果你不确定用哪门语言 —— 用 C。

## 在手机上怎么跑

```
vml run examples/c/draw_prims.c
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 引 `#include <waycoder_ui.h>` 就有全部接口声明
- 颜色一律 `0xAARRGGBB`（alpha 在最前）
- 数组别越界（报「内存错误」十有八九是它）

## 示例

| 文件 | 演示什么 |
|---|---|
| `draw_prims.c` | 图元体检：每种绘图指令各画一格 |
| `gomoku.c` | 五子棋（触摸落子、人机对战、只竖屏 + 不要手柄区） |
| `mario.c` | 横版跳跃 |
| `pacman.c` | 吃豆人（网格地图 + 追击 AI） |
| `starfall.c` | 竖版弹幕 |
| `sysinfo.c` | 调 `ui_call_json("sysinfo")` 打印设备信息 |
| `tetris.c` | 俄罗斯方块（计分、等级、重开、音效） |

同目录还有：`c.bat`

## 实测踩过的坑

- «bold»编译最慢«/»：一份带标准库的程序，手机上要«bold»一两分钟«/»（Python/Lua 那类只要几秒）。
  同一份反复跑的话，先编成 `.vmb` 再跑，快很多。
- 全局数组的初始化器里带«bold»负数«/»时注意（历史缺陷，已修；写方向表这类东西记得测一下）。

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/CCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### C 语言编译器规范说明

> «bold»版本«/»：v1.1 | «bold»日期«/»：2026-07-06 | «bold»修订者«/»：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| «bold»目标标准«/» | C99 (ISO/IEC 9899:1999) |
| «bold»发布年份«/» | 1999 |
| «bold»完成度«/» | ~97% |
| «bold»测试«/» | 43+ 通过 (含 12 Int64) + «bold»Lua 5.4 31/31«/» + cJSON/nbsdgames/parson/tiny_regex_c |
| «bold»里程碑«/» | «bold»Lua 5.4 31/31 🎉«/» + SQLite 255K行 (v1.65.36) |

#### 概述

本编译器支持 C99 标准的子集，将 C 语言源代码编译为 VML (Virtual Machine Language) 汇编代码。编译器采用经典的编译流程：预处理 → 词法分析 → 语法分析 → 代码生成。

#### 支持的语言特性

##### 1. 数据类型

###### 基本数据类型
- «bold»整数类型«/»: `int`, `short`, `long`, `signed`, `unsigned`
  - 支持类型修饰符组合：`unsigned int`, `signed long`, `unsigned short`等
  - 默认整数类型为`signed int`
  - `unsigned`单独使用等价于`unsigned int`
  - `signed`单独使用等价于`signed int`
- «bold»字符类型«/»: `char`, `signed char`, `unsigned char`
- «bold»浮点类型«/»: `float`, `double`
- «bold»布尔类型«/»: `bool` (C99 扩展)
- «bold»空类型«/»: `void`

###### 常量后缀
- «bold»整数常量后缀«/»:
  - `L` 或 `l`: 长整型常量，如 `100L`
  - `U` 或 `u`: 无符号整型常量，如 `100U`
  - `UL` 或 `ul`: 无符号长整型常量，如 `100UL`
- «bold»浮点常量后缀«/»:
  - `F` 或 `f`: 单精度浮点常量，如 `3.14F`
  - 无后缀: 双精度浮点常量，如 `3.14`
- «bold»十六进制常量后缀«/»:
  - 支持十六进制常量后缀：`0xFFU`, `0x7FFFFFFFL`, `0xFFFFFFFFUL`

###### 派生类型
- «bold»指针«/»: `int*`, `char*`, `void*` 等
- «bold»数组«/»: 一维和多维数组，支持初始化，如 `int arr[10]` 和 `int matrix[2][3] = {{1,2,3},{4,5,6}}`
- «bold»结构体«/»: 全局结构体定义和成员访问
- «bold»枚举«/»: 全局枚举定义和使用

##### 2. 变量声明和定义

###### 变量声明
```c
int x;                    // 全局变量
int y = 10;               // 带初始化的全局变量
static int z = 20;        // 静态变量

void func() {
    int local_var;        // 局部变量
    int init_var = 30;    // 带初始化的局部变量
    static int static_local = 40;  // 静态局部变量
}
```

###### 常量
```c
const int MAX_SIZE = 100;
const float PI = 3.14159;
```

##### 3. 运算符

###### 算术运算符
```c
+    // 加法
-    // 减法
*    // 乘法
/    // 除法
%    // 取模
++   // 自增
--   // 自减
```

###### 关系运算符
```c
==   // 等于
!=   // 不等于
<    // 小于
<=   // 小于等于
>    // 大于
>=   // 大于等于
```

###### 逻辑运算符
```c
&&   // 逻辑与
||   // 逻辑或
!    // 逻辑非
```

###### 位运算符
```c
&    // 按位与
|    // 按位或
^    // 按位异或
~    // 按位取反
<<   // 左移
>>   // 右移
```

###### 赋值运算符
```c
=    // 赋值
+=   // 加后赋值
-=   // 减后赋值
*=   // 乘后赋值
/=   // 除后赋值
%=   // 取模后赋值
&=   // 按位与后赋值
|=   // 按位或后赋值
^=   // 按位异或后赋值
<<=  // 左移后赋值
>>=  // 右移后赋值
```

###### 其他运算符
```c
&    // 取地址
*    // 解引用
.    // 结构体成员访问
->   // 指针结构体成员访问
sizeof // 获取类型大小
```

##### 4. 类型转换和提升

###### 隐式类型转换
- «bold»整数提升«/»: 在表达式中，`char`和`short`类型自动提升为`int`
- «bold»浮点提升«/»: 在混合类型表达式中，整数自动转换为浮点数
- «bold»赋值转换«/»: 赋值时右侧表达式类型自动转换为左侧变量类型

###### 显式类型转换（强制转换）
```c
int x = 10;
float y = (float)x;      // 整数转浮点
int z = (int)3.14;       // 浮点转整数
unsigned int u = (unsigned int)-10;  // 有符号转无符号
```

###### 类型提升规则
1. 如果任一操作数是`float`，结果为`float`
2. 如果任一操作数是`double`，结果为`double`
3. 否则进行整数提升，按类型等级选择结果类型
4. 类型等级（从低到高）:
   - `char` / `unsigned char`
   - `short` / `unsigned short`
   - `int` / `unsigned int`
   - `long` / `unsigned long`

##### 5. 控制流语句

###### 条件语句
```c
// if-else
if (condition) {
    // 语句块
} else if (condition2) {
    // 语句块
} else {
    // 语句块
}

// 三元运算符
int result = condition ? value1 : value2;
```

###### 循环语句
```c
// while 循环
while (condition) {
    // 循环体
}

// do-while 循环
do {
    // 循环体
} while (condition);

// for 循环
for (int i = 0; i < 10; i++) {
    // 循环体
}
```

###### 跳转语句
```c
break;      // 跳出循环或switch
continue;   // 继续下一次循环
return;     // 返回函数值
goto label; // 跳转到标签
```

###### switch 语句
```c
switch (expression) {
    case constant1:
        // 语句块
        break;
    case constant2:
        // 语句块
        break;
    default:
        // 语句块
        break;
}
```

##### 6. 函数

###### 函数定义
```c
// 函数声明
int add(int a, int b);

// 函数定义
int add(int a, int b) {
    return a + b;
}

// 无返回值函数
void print_message(const char* msg) {
    // 函数体
}

// 可变参数函数（有限支持）
int printf(const char* format, ...);
```

###### 函数调用
```c
int result = add(10, 20);
print_message("Hello, World!");
```

###### 递归函数
```c
int factorial(int n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}
```

###### 中断服务函数 (interrupt)
`interrupt` 关键字声明一个函数为中断服务程序（ISR）。编译器自动保存/恢复全部通用寄存器，并使用 `IRET` 而非 `RET` 返回。

```c
// 中断服务函数：保存全部寄存器，IRET 返回
interrupt void timer_isr(void) {
    // 中断处理代码
    // 编译器自动生成：PUSH R0..R11、PUSH R12、PUSH R15
    // 恢复：POP R15、POP R12、POP R0..R11、IRET
}

// 可通过 interrupt + static 组合使用
interrupt static void keyboard_isr(void) {
    // 仅本文件可见的 ISR
}
```

«bold»注意事项«/»:
- ISR 不能有参数（必须为 `void`）
- ISR 应尽量简短（中断处理时间有限）
- 编译器自动管理全部寄存器的保存和恢复
- 函数向量地址需要通过 `.vectors` 伪指令在 VML 汇编中注册

##### 7. 预处理器指令

###### 文件包含
```c
#include "vmlib.h"    // 用户头文件
#include <stdio.h>    // 系统头文件（有限支持）
```

###### 宏定义
```c
#define MAX_SIZE 100
#define MIN(a, b) ((a) < (b) ? (a) : (b))

#ifdef DEBUG
    #define DEBUG_PRINT(msg) printf("DEBUG: %s\n", msg)
#else
    #define DEBUG_PRINT(msg)
#endif
```

###### 条件编译
```c
#if defined(WIN32)
    // Windows 特定代码
#elif defined(LINUX)
    // Linux 特定代码
#else
    // 其他平台代码
#endif
```

##### 8. 结构体和枚举

###### 结构体
```c
// 结构体定义
struct Point {
    int x;
    int y;
};

// 结构体变量声明
struct Point p1;
struct Point p2 = {10, 20};

// 结构体成员访问
p1.x = 5;
p1.y = 15;

// 结构体指针
struct Point* ptr = &p1;
ptr->x = 100;
```

###### 枚举
```c
// 枚举定义
enum Color {
    RED,
    GREEN,
    BLUE
};

// 枚举变量
enum Color c = RED;
```

##### 9. 指针和数组

###### 指针
```c
int x = 10;
int* ptr = &x;      // 取地址
int y = *ptr;       // 解引用

// 指针运算
int arr[10];
int* p = arr;
p++;                // 指针前进
p--;                // 指针后退
```

###### 数组
```c
// 数组声明和初始化
int arr1[10];
int arr2[5] = {1, 2, 3, 4, 5};
int arr3[] = {1, 2, 3};  // 自动推断大小

// 部分初始化（未指定元素自动为0）
int arr4[10] = {1, 2, 3};  // arr4[0]=1, arr4[1]=2, arr4[2]=3, arr4[3..9]=0

// 数组访问
arr1[0] = 100;
int value = arr2[2];

// 多维数组
int matrix[3][3] = {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
};

// 多维数组部分初始化
int matrix2[2][3] = {
    {1, 2},     // 第三列自动为0
    {4, 5, 6}
};

// 嵌套数组初始化
int cube[2][2][2] = {
    {
        {1, 2},
        {3, 4}
    },
    {
        {5, 6},
        {7, 8}
    }
};
```

##### 10. 存储类别与扩展关键字

###### 自动变量 (auto)
```c
void func() {
    auto int x = 10;  // auto 可省略
    int y = 20;       // 默认就是 auto
}
```

###### 寄存器变量 (register)
```c
void func() {
    register int counter = 0;  // 建议编译器使用寄存器
}
```

###### 静态变量 (static)
```c
static int global_static = 100;  // 文件作用域

void func() {
    static int local_static = 0;  // 函数作用域，保持值
    local_static++;
}
```

###### 外部变量 (extern)
```c
extern int external_var;  // 声明外部变量

void func() {
    external_var = 100;   // 使用外部变量
}
```

###### 中断服务函数 (interrupt)
```c
interrupt void isr_name(void) {
    // 中断处理代码
    // 编译器自动保存 R0-R11，使用 IRET 返回
}
```
详见第 6 节「函数」中的「中断服务函数」。

#### 编译器限制

##### 不支持的特性
1. «bold»位域 (bit fields)«/»: 不支持结构体位域
2. «bold»复杂指针运算«/»: 指针算术仅限于简单加减
3. «bold»函数指针«/»: 声明语法已支持 (v1.65.35)，运行时调用有限
4. «bold»变长数组 (VLA)«/»: 不支持 C99 的变长数组
5. «bold»复杂类型限定符«/»: 不支持 `restrict`, `_Atomic` 等
6. «bold»`unsigned` 修饰符«/»: `unsigned int` / `unsigned char` 当作 `int`/`char` 处理（VML 无符号概念）

##### v1.65.35 里程碑: SQLite 编译
- «bold»解析能力«/»: 完成 SQLite amalgamation 全文件解析 (255,636 行, ~475K tokens)
- «bold»编译通过«/»: SQLite stub + 完整版本均可编译运行
- «bold»运行时验证«/»: sqlite3_open / CREATE TABLE / INSERT / SELECT / sqlite3_close 全部通过
- «bold»类型系统增强«/»: 匿名 struct 成员、双星号指针、函数指针声明

##### VML 扩展关键字
本编译器在标准 C99 基础上增加了以下扩展关键字：

| 关键字 | 用途 | 说明 |
|--------|------|------|
| `interrupt` / `__interrupt` | 函数声明 | 声明中断服务函数，自动保存寄存器、IRET 返回 |
| `__chipasm__` | 内联汇编块 | 插入多行 VML 汇编指令 |
| `asm` | 内联汇编 | 插入单行 VML 汇编指令 |
| `__stdcall` | 调用约定 | stdcall 调用约定（参数从右到左压栈，被调用者清理） |
| `__fastcall` | 调用约定 | fastcall 调用约定（前2个参数通过寄存器传递） |
| `__cdecl` | 调用约定 | cdecl 调用约定（参数从右到左压栈，调用者清理） |
| `__builtin_va_start` | 变参 | 变参函数起始 |
| `__builtin_va_arg` | 变参 | 访问变参 |
| `__builtin_va_end` | 变参 | 结束变参 |
| `__builtin_va_copy` | 变参 | 拷贝变参 |

##### 部分支持的特性
1. «bold»预处理器«/»: 支持 `#include`、`#define`、`#undef`、条件编译（`#if`/`#ifdef`/`#ifndef`/`#else`/`#elif`/`#endif`）
2. «bold»标准库«/»: 支持 `stdio.h`、`stdlib.h`、`string.h`、`math.h`、`conio.h`、`graphics.h`
3. «bold»浮点运算«/»: 支持基本浮点运算（`+ - * /`、比较、类型转换）
4. «bold»结构体«/»: 支持基本结构体，支持结构体成员指针（`char *buf`）、嵌套结构体
5. «bold»联合体«/»: 支持 union 类型
6. «bold»数组初始化«/»: 完整支持一维和多维嵌套初始化语法
7. «bold»内联汇编«/»: 支持 `asm("instruction")` 单行和 `__chipasm__ { }` 多行语法
8. «bold»多维数组«/»: 支持声明、访问和嵌套初始化的完整多维数组
9. «bold»调用约定«/»: 支持 `__stdcall` / `__fastcall` / `__cdecl` 三种调用约定
10. «bold»指示字«/»: 常量后缀（`L`、`U`、`F`）仅在部分上下文中完全支持

#### 编译流程

##### 1. 预处理阶段
- 处理 `#include` 指令
- 展开宏定义
- 处理条件编译
- 移除注释

##### 2. 词法分析阶段
- 将源代码转换为令牌流
- 识别关键字、标识符、常量、运算符等

##### 3. 语法分析阶段
- 构建抽象语法树 (AST)
- 检查语法正确性
- 建立符号表

##### 4. 代码生成阶段
- 遍历 AST 生成 VML 指令
- 分配变量存储空间
- 生成函数调用代码
- 优化生成的代码

#### 使用示例

##### 简单程序
```c
#include "vmlib.h"

int main() {
    vga_clear();
    vga_puts("Hello, C Compiler!");
    return 0;
}
```

##### 计算阶乘
```c
#include "vmlib.h"

int factorial(int n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}

int main() {
    int result = factorial(5);
    
    vga_clear();
    vga_puts("Factorial of 5 is: ");
    // 需要将数字转换为字符串显示
    return 0;
}
```

##### 数组操作
```c
#include "vmlib.h"

int main() {
    int arr[5] = {1, 2, 3, 4, 5};
    int sum = 0;
    
    for (int i = 0; i < 5; i++) {
        sum += arr[i];
    }
    
    vga_clear();
    vga_puts("Sum of array: ");
    // 显示结果
    return 0;
}
```

##### 多维数组初始化
```c
#include "vmlib.h"

int main() {
    // 二维数组初始化
    int matrix[3][3] = {
        {1, 2, 3},
        {4, 5, 6},
        {7, 8, 9}
    };
    
    // 计算矩阵对角线之和
    int diag_sum = 0;
    for (int i = 0; i < 3; i++) {
        diag_sum += matrix[i][i];
    }
    
    vga_clear();
    vga_puts("Diagonal sum: ");
    // 显示结果
    return 0;
}
```

##### 复杂数组测试
```c
#include "vmlib.h"

// 测试各种数组初始化方式
int main() {
    // 自动大小推断
    int auto_arr[] = {10, 20, 30, 40, 50};
    
    // 部分初始化
    int partial[10] = {1, 2, 3};  // 后7个元素为0
    
    // 多维数组
    int cube[2][2][2] = {
        {{1, 2}, {3, 4}},
        {{5, 6}, {7, 8}}
    };
    
    vga_clear();
    vga_puts("Array tests completed");
    return 0;
}
```

#### 编译器选项

##### 命令行参数
```bash
# 基本用法
dotnet run --project VMLPrepares\\CCompiler input.c -o output.vml

# 指定库路径
dotnet run --project VMLPrepares\\CCompiler -I Lib\c input.c -o output.vml

# 只进行预处理
dotnet run --project VMLPrepares\\CCompiler -E input.c

# 使用环境变量指定库路径
$env:VML_C_LIB="Lib\c"
dotnet run --project VMLPrepares\\CCompiler input.c -o output.vml
```

##### 环境变量
- `VML_C_LIB`: 指定 C 语言库文件搜索路径

#### 错误处理

编译器会报告以下类型的错误：

##### 语法错误
- 缺少分号、括号不匹配等
- 错误位置和错误信息

##### 语义错误
- 未声明的标识符
- 类型不匹配
- 重复定义

##### 预处理错误
- 找不到头文件
- 宏定义错误

#### C99 标准兼容性

本编译器目标是实现完整的 C99 标准兼容性。当前已实现约 97% 的 C99 核心功能：

##### 已实现的核心功能
1. «bold»基本语法«/»: 变量声明、表达式、控制结构
2. «bold»数据类型«/»: `int`, `char`, `float`, `double`, `void`, 指针、数组、结构体、枚举
3. «bold»控制流«/»: `if-else`, `while`, `do-while`, `for`, `switch`, `break`, `continue`, `return`, `goto`
4. «bold»函数«/»: 函数定义、声明、调用、递归、参数传递、隐式 int 返回类型
5. «bold»预处理器«/»: `#include`, `#define`, `#ifdef`, `#ifndef`, `#if`, `#else`, `#elif`, `#endif`
6. «bold»数组«/»: 一维和多维数组声明、嵌套初始化、元素访问
7. «bold»结构体«/»: 全局结构体定义、变量声明、成员访问、结构体成员指针
8. «bold»联合体«/»: 支持 union 类型
9. «bold»逗号表达式«/»: 支持逗号运算符
10. «bold»注释«/»: 支持 `/* */` 和 `//` 风格注释

##### VML 扩展特性
1. «bold»中断服务函数«/»: `interrupt` 关键字声明 ISR，自动保存全部寄存器、IRET 返回
2. «bold»内联汇编«/»: `asm("instruction")` 单行和 `__chipasm__ { }` 多行 VML 指令
3. «bold»调用约定«/»: `__stdcall` / `__fastcall` / `__cdecl` 跨语言互操作
4. «bold»跨语言互操作«/»: `POKE` / `PEEK` 内存映射 I/O (MMIO)

##### 缺失的关键功能（计划实现）
1. «bold»类型系统«/»: `unsigned` 类型完整支持
2. «bold»标准库«/»: 完成 `stdio.h` 文件操作、`math.h` 全部函数
3. «bold»高级特性«/»: 函数指针、位域
4. «bold»复杂指针运算«/»: 完善的指针算术和类型转换
5. «bold»宽字符«/»: `wchar_t` 和相关函数

##### 与标准 C 的差异
1. «bold»内存模型«/»: 使用 VML 虚拟机的内存模型
2. «bold»标准库«/»: 使用 VML 运行时库而非标准 C 库
3. «bold»系统调用«/»: 通过 VML 运行时进行 I/O 操作
4. «bold»整数大小«/»: 使用 VML 的 32 位整数
5. «bold»浮点精度«/»: 使用 VML 的浮点表示

#### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (float) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (double) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (long long) 处理模式 |

##### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `float` 按以下模式编译：

- «bold»`hard` 模式（默认）«/»: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- «bold»`soft` 模式«/»: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- «bold»`none` 模式«/»: 禁用所有 32 位浮点类型，遇到 `float` 声明时报告编译错误。

##### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `double` 按以下模式编译：

- «bold»`soft` 模式（默认）«/»: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- «bold»`hard` 模式«/»: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- «bold»`none` 模式«/»: 禁用所有 64 位浮点类型，遇到 `double` 声明时报告编译错误。

##### 64位整数 (int64)

本语言中的 64 位整数类型 `long long` 按以下模式编译：

- «bold»`soft` 模式（默认）«/»: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- «bold»`hard` 模式«/»: 使用 VML 原生 64 位整数指令 `MOVEL`/`ADDL`/`SUBL`/`MULL`/`DIVL`/`MODL`/`NEGL`/`CMPL`/`ANDL`/`ORL`/`XORL`/`NOTL`/`SHLL`/`SHRL`，通过 L0-L7 八个长整数寄存器运算。v1.65.197+ 起可用。
- «bold»`none` 模式«/»: 禁用 64 位整数类型，遇到 `long long` 声明时报告编译错误。

##### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### 性能考虑

1. «bold»代码大小«/»: 生成的 VML 代码比原生代码大
2. «bold»执行速度«/»: 在 VML 虚拟机中运行，比原生代码慢
3. «bold»内存使用«/»: 使用 VML 虚拟机的内存空间
4. «bold»优化级别«/»: 当前为基本优化，可生成较直接的代码

#### 扩展性

编译器设计为可扩展的架构：

1. «bold»添加新语法«/»: 可扩展词法和语法分析器
2. «bold»优化通道«/»: 可添加代码优化阶段
3. «bold»目标平台«/»: 可扩展支持其他目标架构
4. «bold»语言特性«/»: 可逐步添加更多 C 语言特性

#### 相关文件

- `CCompiler.cs`: 编译器主类
- `Lexer.cs`: 词法分析器
- `Parser.cs`: 语法分析器
- `CodeGenerator.cs`: 代码生成器
- `Preprocessor.cs`: 预处理器
- `Program.cs`: 命令行接口
- `README.md`: 项目说明文档

---

#### 🆕 字符串类型 (v1.65.19)

C 编译器支持三种宽度的字符串类型：

| 字面量 | 类型 | 宽度 | VML 伪指令 | 终止符 |
|:-------|:-----|:----:|:----------|:------|
| `"str"` | `char*` | 8-bit | `.string` | `0x00` |
| `L"str"` | `wchar_t*` | 16-bit | `.wstring` | `0x0000` |
| `U"str"` | `char32_t*` | 32-bit | `.ustring` | `0x00000000` |

```c
char     *s1 = "Hello";           // .string  (UTF-8)
wchar_t  *s2 = L"你好";           // .wstring (UTF-16LE, BMP only)
char32_t *s3 = U"🎉";             // .ustring (UTF-32LE, full Unicode)

wchar_t  c1 = L'字';              // 16-bit 宽字符
char32_t c2 = U'🎉';              // 32-bit Unicode 字符
```

«bold»预定义宏«/»: `VML_WSTRING` — OS 模式下定义为 1，MCU 模式下未定义
«bold»标准头«/»: `<wchar.h>` / `<uchar.h>` 自动链接 `Lib/shared/wchar.vml` / `uchar.vml`
«bold»输出«/»: `shared_print_wstr(wstr)` — 自动转换 UTF-16LE→UTF-8→SYSCALL #1

---

#### 🆕 预处理器扩展指令 (v1.65.19)

##### `#param` — 源文件内配置指令

| 指令 | 说明 | 示例 |
|:-----|:-----|:-----|
| `#param lib("xxx")` | 自动链接指定库文件 | `#param lib("wchar")` |
| `#param path("xxx")` | 添加 include 搜索路径 | `#param path("./inc")` |
| `#param prefix("xxx")` | 设置函数名前缀 | `#param prefix("python_")` |

##### `#param prefix` — 同一源码编译多语言库

使用 `#param prefix` 或命令行 `-D VML_PREFIX=xxx`，同一份 C 源码可为不同语言生成不同前缀的库文件：

```c
// wstring.c — 一份源码，多语言共用
#param prefix("python_")    // 为 Python 编译时取消注释这行
// #param prefix("java_")   // 为 Java 编译时取消注释这行
// #param prefix("js_")     // 为 JavaScript 编译时取消注释这行

size_t wcslen(const wchar_t *s) { ... }   // → python_wcslen
wchar_t *wcscpy(wchar_t *d, const wchar_t *s) { ... }  // → python_wcscpy
```

«bold»编译命令«/»：
```bash
# 为 Python 生成 python_wcslen, python_wcscpy ...
vmltool wstring.c -D VML_PREFIX=python_ -o python/wstring.vml

# 为 Java 生成 java_wcslen, java_wcscpy ...
vmltool wstring.c -D VML_PREFIX=java_ -o java/wstring.vml

# 为 JavaScript 生成 js_wcslen, js_wcscpy ...
vmltool wstring.c -D VML_PREFIX=js_ -o js/wstring.vml
```

«bold»前缀替换规则«/»：
- `shared_xxx` → `{prefix}xxx` (替换 shared_ 前缀)
- `vml_xxx` → `{prefix}xxx` (替换 vml_ 前缀)
- 其他函数名 → `{prefix}` + 原名 (直接添加前缀)

##### 多语言库生成脚本

```bash
#!/bin/bash
# build_all_langs.sh — 为所有语言编译 wstring 库
for lang in python java js kotlin swift go csharp; do
  vmltool wstring.c -D "VML_PREFIX=${lang}_" -o "${lang}/wstring.vml" --no-link
done
```

##### `#param lib` — 自动库链接

```c
// wstring.h 头文件中声明依赖
#param lib("wchar")     // 自动链接 wchar.vml

// 用户只需 #include "wstring.h"
// 编译器自动链接 wstring.vml + wchar.vml
```

配合 `#ifdef VML_WSTRING` 可按模式选择编码。

##### `#param path` — 额外 include 路径

```c
#param path("./include")      // 添加相对搜索路径
```

---

#### 调用约定 (v1.65.19)

C 编译器支持三种调用约定，通过函数修饰符指定：

| 修饰符 | 参数传递 | 栈清理 | 变参支持 |
|:-------|:--------|:------|:--:|
| `__cdecl` (默认) | 右→左压栈，R0-R3传递前4参数 | 调用者清理 | ✅ |
| `__stdcall` | 右→左压栈 | 被调用者清理 | — |
| `__fastcall` | R0-R3传递前4参数，其余压栈 | 被调用者清理 | — |

##### `__cdecl` 变参函数

```c
// 变参 printf
__cdecl void shared_printf(const char *fmt, ...) {
    char buf[512];
    int *stack_args = (int*)(&fmt + 1);  // fmt 之后是变参
    int len = shared_vsnprintf(buf, fmt, stack_args, 8);
    buf[len] = 0;
    asm("SYSCALL #1");  // 输出到控制台
}

// 调用: 参数数量不限 (最多 8 个, 受 VML 栈限制)
shared_printf("Int: %d  Hex: %x  Str: %s\n", 42, 255, (int)"Hello");

// 变参 sprintf
__cdecl int shared_sprintf(char *buf, const char *fmt, ...) {
    int *stack_args = (int*)(&fmt + 1);
    return shared_vsnprintf(buf, fmt, stack_args, 8);
}
```

«bold»变参实现原理«/»:
- `__cdecl` 参数从右向左压栈
- `fmt` 是最接近栈顶的固定参数
- `&fmt + 1` 指向栈上的第一个可变参数
- 通过栈指针可以直接读取所有变参

##### `__stdcall` 固定参数

```c
// 被调用者清理栈, Windows API 风格
__stdcall void shared_print_str(const char *str) {
    asm("SYSCALL #1");
}
```

##### `__fastcall` 快速调用

```c
// 前4参数用寄存器, 适合性能敏感场景
__fastcall int add_four(int a, int b, int c, int d) {
    return a + b + c + d;
}
```

«bold»注意«/»: `__cdecl`/`__stdcall`/`__fastcall` 修饰符用于«bold»函数定义«/»。在调用侧，编译器自动根据函数定义选择正确的调用约定，无需显式声明。

## 编译器 README

### C 编译器

«bold»路径«/»: `VMLPrepares/CCompiler/`
«bold»完成度«/»: ~97% | 🟢 生产可用
«bold»标准库«/»: `Lib/c/` (29 个文件)

#### 功能
- ✅ C99 完整标准 (ISO/IEC 9899:1999)
- ✅ 指针算术、多维数组、struct/union
- ✅ 预处理器 (#include, #define, #ifdef)
- ✅ 内联汇编 (`__chipasm__`)
- ✅ POKE/PEEK 内存操作
- ✅ 完整标准库 (stdio, stdlib, string, math, conio, graphics)
- ✅ 中断/外设支持 (interrupt)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

«bold»跳过«/»（遇到这些语法不生成代码）:
- 无（C99无异步/线程/反射）

«bold»保留«/»（由 BIOS 实现底层）:
- malloc/free、fopen/fread、printf%f、va_list
- POKE/PEEK 内存映射 I/O (MMIO)
- interrupt 关键字（中断服务函数）
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART
- 裸指针（有限制）

##### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

##### RAM 级别
- `--ram k`：KB级别（2KB~64KB，如 8051/PIC/AVR）
- `--ram m`：MB级别（64KB~1MB，如 ARM Cortex-M，«bold»默认«/»）
- `--ram g`：GB级别（如 x86/DDR 系统）
- `--stack-size <bytes>`：手动指定栈大小（默认自动根据 --ram 分配）

##### MCU 安全编码提示
- 有限栈空间（256-4096 字节典型），避免深度递归
- 禁止深度递归（>10层需评估栈）
- 禁止动态加载（import/dofile/eval）
- 浮点运算可能需软浮点库

#### 使用
```bash
dotnet run --project VMLPrepares/CCompiler input.c -o output.vml
vmltool input.c -o output.vml
```

#### 测试
`Test/c/` — 190+ 测试文件
