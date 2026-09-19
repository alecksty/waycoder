# D

C 风格的语法，写起来比 C 宽松一些。

## 在手机上怎么跑

```
vml run examples/d/catch.d
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 入口 `void main()`
- 直接调用 `ui_*`
- 与 C 一样注意数组越界

## 示例

| 文件 | 演示什么 |
|---|---|
| `catch.d` | 接方块 |
| `sysinfo.d` | 设备信息 |

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/DCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### D 语言编译器规范说明

> «bold»版本«/»：v1.0 | «bold»日期«/»：2026-07-06 | «bold»修订者«/»：深圳市探索智能科技有限公司

#### 元数据

| 属性 | 值 |
|------|-----|
| «bold»语言«/» | D |
| «bold»标准«/» | D 2.0 子集 |
| «bold»年份«/» | 2001 年首次发布 |
| «bold»完成度«/» | ~97% |
| «bold»源文件扩展名«/» | `.d` |
| «bold»文件编码«/» | UTF-8 (默认) |

#### 概述

D 是一门系统编程语言，采用 C-like 语法与模块化设计。本编译器实现的是 D 2.0 规范的功能子集，面向 VML 虚拟机的 MCU 安全子集环境运行。编译流程为：

```
source.d → [预处理器] → [Lexer] → [Parser] → [CodeGenerator] → VML汇编 (.vml)
```

##### 编译器入口

- 主编译器类: `DCompiler.DCompiler`
- 插件注册: `DCompilerPlugin` (实现 `CompilerPluginExBase`)
- 独立运行: `CompilerProgram.MainAsync()` — 使用 `SimpleCompilerProgram` 包装

##### 调用约定

采用类 C 调用约定：参数从右到左压栈，调用者负责栈清理。R0 寄存器存放返回值（整数/布尔/指针），浮点返回值使用 F0 寄存器。

---

#### 支持的特性

##### 1. 数据类型

| 类型 | 关键字 | VML IR 宽度 | 说明 |
|------|--------|-------------|------|
| 空类型 | `void` | — | 函数无返回值 |
| 整数 | `int` | 32-bit signed | 支持十进制和十六进制 (`0x`) 字面量 |
| 单精度浮点 | `float` | 32-bit | IEEE 754 单精度 |
| 双精度浮点 | `double` | 64-bit | 当前降级为 float 存储 |
| 布尔 | `bool` | 32-bit | `true` = 1, `false` = 0 |
| 字符串 | `string` | 指针 (32-bit) | 不可变字符数组，字面量存入 data section |
| 字符 | `char` | 8-bit | 单引号字面量，存储为 int |
| 类 | `class` | 引用类型 | 成员通过 R12-0 (this) 访问 |
| 结构体 | `struct` | 值类型 | 语法上支持，解析与 class 相同 |
| 接口 | `interface` | 占位 | 解析跳过内部声明 |
| 枚举 | `enum` | 32-bit | 支持显式赋值 `= expr` |

«bold»字面量语法«/»:
- 整数: `42`, `0x2A`, `0xFF`
- 浮点: `3.14`, `0.5`
- 布尔: `true`, `false`
- 空: `null`
- 字符串: `"hello world"` (双引号，支持转义字符)
- 字符: `'a'`, `'\n'` (单引号，支持转义字符)
- this 引用: `this` (指向当前对象，存储在 R12-0)

##### 2. 模块系统

```d
module mymodule;                    // 声明当前模块名
import std.stdio;                   // 导入模块
import foo.bar.baz;                 // 点分导入路径
```

`import` 语句仅支持静态导入语法，解析时提取模块名用于库链接。`module` 声明语法上接受但不产生实际语义作用。

##### 3. 变量声明

```d
int x = 10;                         // 声明 + 初始化
float pi;                           // 声明（默认初始化 0）
string name = "hello";              // 字符串变量
bool flag = true;                   // 布尔变量
char c = 'A';                       // 字符变量
ClassName obj;                      // 类类型变量（默认初始化为 0/null）
```

«bold»语义«/»:
- 局部变量分配在栈帧中 (R12 相对偏移，每次分配 4 字节)
- 全局变量分配在 data section 中 (`var_name` 标签)
- 不指定初始化器时，默认初始化为 0
- for 循环初始化段内支持声明新变量

##### 4. 运算符

###### 4.1 算术运算符

| 运算符 | 操作 | 示例 |
|--------|------|------|
| `+` | 加法 | `a + b` |
| `-` | 減法 | `a - b` |
| `*` | 乘法 | `a * b` |
| `/` | 除法 | `a / b` |
| `%` | 取模 | `a % b` |
| `^^` | 幂运算 | `a ^^ b` |

注意: `^^` 幂运算在 VML 层面不支持原生幂指令，编译器将 `^^` 降级为乘法 `*` 作为近似实现。

###### 4.2 比较运算符

| 运算符 | 操作 | 说明 |
|--------|------|------|
| `==` | 等于 | 返回 1/0 |
| `!=` | 不等于 | 返回 1/0 |
| `<` | 小于 | 返回 1/0 |
| `>` | 大于 | 返回 1/0 |
| `<=` | 小于等于 | 返回 1/0 |
| `>=` | 大于等于 | 返回 1/0 |

比较运算返回整数值：1 为 true，0 为 false。

###### 4.3 逻辑运算符

| 运算符 | 操作 | 说明 |
|--------|------|------|
| `&&` | 逻辑与 | CMP + JE 短路求值 |
| `\|\|` | 逻辑或 | 短路求值 |
| `!` | 逻辑非 | 一元取反 |

逻辑运算使用短路求值策略：`&&` 在左操作数为假时跳过右操作数；`||` 在左操作数为真时跳过右操作数。

###### 4.4 赋值运算符

| 运算符 | 操作 |
|--------|------|
| `=` | 直接赋值 |
| `+=` | 加法赋值 |
| `-=` | 減法赋值 |
| `*=` | 乘法赋值 |
| `/=` | 除法赋值 |

复合赋值运算符 (`+=`, `-=`, `*=`, `/=`) 在内部展开为 `a = a op value` 形式：
```d
a += b;    // 等价于 a = a + b;
a -= 5;    // 等价于 a = a - 5;
a *= 2;    // 等价于 a = a * 2;
a /= 3;    // 等价于 a = a / 3;
```

##### 5. 控制流

###### 5.1 if / else 语句

```d
if (condition) {
    // then body
}

if (condition) {
    // then body
} else {
    // else body
}

if (x > 0) {
    return 1;
} else if (x < 0) {
    return -1;
} else {
    return 0;
}
```

条件求值后与 0 比较，为假时跳转到 else 标签或 end 标签。

###### 5.2 while 语句

```d
while (condition) {
    // loop body
}
```

###### 5.3 do-while 语句

```d
do {
    // loop body
} while (condition);
```

实现方式: do-while 在编译时展开为 `{ body; while (cond) { body } }`。

###### 5.4 for 语句 (C 风格)

```d
for (init; condition; increment) {
    // loop body
}

// 示例
for (int i = 0; i < 10; i = i + 1) {
    x = x + i;
}
```

三子句均为可选项：
- `init`: 可为变量声明或表达式，执行一次
- `condition`: 在每次迭代前求值，为假时退出循环
- `increment`: 在每次迭代体执行后求值

###### 5.5 break / continue

`break` 和 `continue` 关键字当前未实现。在 Lexer 的关键字表中不存在这两个关键字，因此会被当作普通标识符处理。

###### 5.6 return 语句

```d
return;              // void 返回
return expression;   // 带值返回
```

返回值存入 R0 寄存器，恢复 R12 后执行 RET。

##### 6. 函数

```d
// 完整声明格式
int add(int a, int b) {
    return a + b;
}

// void 返回类型
void greet(string name) {
    // ...
}

// 无参数
int getAnswer() {
    return 42;
}
```

«bold»语法«/»: `returnType functionName(paramType paramName, ...) { body }`

«bold»内部实现«/»:
- 函数标签: `func_functionName`
- 类方法标签: `class_ClassName_methodName`
- 序言: PUSH R15, PUSH R12, MOVE R12 R13 (保存帧指针和返回地址，建立新栈帧)
- 参数通过栈传递，在栈帧内偏移 +4 字节处访问第一个参数
- 结尾: POP R12, RET (恢复帧指针，返回调用者)
- 函数定义在主流程中被 JMP 跳过，只有被显式调用时才会执行

##### 7. 类与结构体

###### 7.1 class

```d
class Animal {
    void speak() {
        // member function
    }

    int getAge() {
        return this.age;   // this 关键字号
    }
}
```

- `class` 解析为 `ClassDeclNode`
- 成员函数按声明顺序生成
- `this` 关键字通过 R12-0 访问当前对象指针
- 不支持继承（extends 关键字未实现）
- 不支持访问控制（public/private/protected 未实现）
- 不支持构造/析构函数

###### 7.2 struct

```d
struct Point {
    int x;
    int y;
}
```

语法上与 class 相同，共用 `ClassDeclNode` AST 节点。区别仅在语义层面（值类型 vs 引用类型），当前实现不做区分。

###### 7.3 interface

```d
interface Drawable {
    void draw();
}
```

`interface` 声明进行解析，跳过内部声明体，返回占位 AST 节点。接口方法不生成代码。

###### 7.4 enum

```d
enum Color {
    Red,           // 隐式值 0
    Green,         // 隐式值 1
    Blue = 5,      // 显式赋值
    Yellow         // 隐式值 6
}
```

- 枚举成员名由 `Identifier` 识别
- 支持 `= expression` 显式赋值
- 成员之间以逗号分隔（结尾逗号可选）
- 枚举声明体以分号结束 (`};`)

##### 8. 注释

| 类型 | 语法 | 说明 |
|------|------|------|
| 单行注释 | `// text` | 从 `//` 到行尾 |
| 块注释 | `/* text */` | 多行，不可嵌套 |
| 嵌套块注释 | `/+ text +/` | D 语言原生嵌套注释，当前未专门支持 |

编译器内部的 `LexerBase.SkipBlockComment` 处理 `/* ... */`，`SkipLineComment` 处理 `// ...`。`/+ ... +/` 嵌套注释语法当前未在 Lexer 中实现。

«bold»注意«/»: 注释 token 在 Tokenize 阶段产生但不参与语法分析 (被 `SkipComments()` 跳过)。

##### 9. 字符串字面量

```d
"hello world"                       // 普通字符串
"line1\nline2"                      // 含转义字符
"quote: \"inside\""                 // 转义双引号
```

- 双引号界定
- 内部转义字符由 `LexerBase.ReadStringLiteral` 处理（支持 `\n`, `\t`, `\\`, `\"`, `\0` 等标准 C 转义序列）
- 字符串字面量在 data section 中分配，通过 `MOVE R0, label` 加载地址
- 相同内容的字符串被缓存去重

##### 10. 字符字面量

```d
'a'                                 // 字符 'a'
'\n'                                // 换行符
'\t'                                // 制表符
'\\'                                // 反斜杠
```

- 单引号界定
- 解析为整数值（字符的 ASCII/Unicode 码点）
- 与字符串字面量共用 `ReadStringLiteral` 方法处理

##### 11. 表达式语法

```
Expression := Assignment
Assignment := LogicalOr ('=' | '+=' | '-=' | '*=' | '/=') Assignment | LogicalOr
LogicalOr  := LogicalAnd ('||' LogicalAnd)*
LogicalAnd := Comparison ('&&' Comparison)*
Comparison := Additive (('==' | '!=' | '<' | '>' | '<=' | '>=') Additive)*
Additive   := Multiplicative (('+' | '-') Multiplicative)*
Multiplicative := Unary (('*' | '/' | '%' | '^^') Unary)*
Unary      := ('-' | '!') Unary | Postfix
Postfix    := Primary ('(' Arguments? ')')*
Primary    := Integer | Float | String | Char | true | false | null
            | this | Identifier | '(' Expression ')'
```

##### 12. 表达式优先级

按优先级从低到高排列：

| 优先级 | 运算符 | 结合性 |
|--------|--------|--------|
| 1 (最低) | `=` `+=` `-=` `*=` `/=` | 右结合 |
| 2 | `\|\|` | 左结合 |
| 3 | `&&` | 左结合 |
| 4 | `==` `!=` `<` `>` `<=` `>=` | 左结合 |
| 5 | `+` `-` | 左结合 |
| 6 | `*` `/` `%` `^^` | 左结合 |
| 7 | `-` (一元) `!` (一元) | 右结合 |
| 8 (最高) | `()` (函数调用) | 左结合 |

##### 13. 预处理器支持

编译器集成了 C 风格预处理器 (`Preprocessor`)，在词法分析前执行：

###### 预定义宏

| 宏名 | 值 | 说明 |
|------|-----|------|
| `__VML__` | `1` | VML 目标平台标识 |
| `__VML_VERSION__` | `"1.65.32"` | VML 工具链版本 |
| `__D__` | `1` | D 语言源文件标识 |
| `__DATE__` | `"MMM dd yyyy"` | 编译日期 |
| `__TIME__` | `"HH:mm:ss"` | 编译时间 |

###### 预处理指令

支持标准 C 预处理指令，仅当源文件包含 `#` 字符时才激活预处理器：
- `#include "file"` / `#include <file>`
- `#define NAME value` / `#define NAME`
- `#undef NAME`
- `#if` / `#ifdef` / `#ifndef` / `#else` / `#elif` / `#endif`
- `#pragma` (传递到编译管线)
- `#error "message"`
- 行继续符 `\`

##### 14. 入口点与库链接

- 程序入口点为 `main` 标签（非 `main` 函数，而是 VML 程序起始点）
- 编译器自动链接 D 标准库 (目录 `Lib/d/`)，通过 `CompilerHelper.LinkStandardLibrary` 执行
- `import` 声明的模块自动解析为库文件路径并链接

---

#### 限制与未实现特性

以下特性属于 D 语言完整规范但目前«bold»不支持«/»：

##### 元编程

- «bold»模板 (Template)«/»: `template T(T)` — 模板元编程核心特性，未实现
- «bold»混入 (Mixin)«/»: `mixin("string")` — 编译时代码生成，未实现
- «bold»CTFE (编译时函数求值)«/»: 不支持在编译期执行函数
- «bold»约束 (Constraint)«/»: `if (is(T == int))` 模板约束，未实现

##### 契约编程

- «bold»in 契约«/»: `in { assert(x > 0); }` — 前置条件，未实现
- «bold»out 契约«/»: `out (result) { assert(result > 0); }` — 后置条件，未实现
- «bold»invariant«/»: `invariant() { ... }` — 类不变量，未实现

##### 内存管理

- «bold»GC (垃圾回收)«/»: D 的默认内存管理策略，因 MCU 安全子集限制而跳过
- «bold»new 表达式«/»: `new ClassName()` — 关键字识别但不生成分配代码
- «bold»delete«/»: 显式析构，未实现

##### 控制流

- «bold»switch / case«/»: 多分支选择，未实现
- «bold»break«/»: 循环中断，未实现 (关键字不在词法解析表中)
- «bold»continue«/»: 继续下一次迭代，未实现 (关键字不在词法解析表中)
- «bold»foreach / foreach_reverse«/»: 范围迭代，未实现
- «bold»goto«/»: 跳转语句，未实现
- «bold»scope 语句«/»: `scope(exit)`, `scope(success)`, `scope(failure)` — 作用域守卫，未实现
- «bold»with 语句«/»: `with (expr) { ... }` — 作用域缩短，未实现
- «bold»synchronized«/»: 多线程同步，未实现
- «bold»try / catch / finally«/»: 异常处理，因 MCU 安全子集限制跳过

##### 类型系统

- «bold»auto«/»: 类型推导，未实现
- «bold»const / immutable«/»: 类型修饰符，未实现
- «bold»alias«/»: 类型别名，未实现
- «bold»typeof«/»: 类型获取，未实现
- «bold»delegate / function«/»: 委托与函数指针，未实现
- «bold»enum 作为清单类型«/»: `enum E : string { ... }` — 仅支持基本整数枚举
- «bold»union«/»: 联合体，未实现

##### 复合类型

- «bold»关联数组«/»: `int[string] aa;` — 键值对容器，未实现
- «bold»动态数组«/»: `int[] arr;` — 切片与动态数组，未实现
- «bold»静态数组«/»: `int[10] arr;` — 固定大小数组，未实现
- «bold»切片操作«/»: `arr[0..$]`, `arr[1..3]` — 未实现

##### 其他

- «bold»unittest 块«/»: `unittest { ... }` — 单元测试块，未实现
- «bold»version / debug«/»: 条件编译块，未实现 (可通过预处理器 `#ifdef` 替代)
- «bold»属性 (Property)«/»: `@property T name()` — 未实现
- «bold»操作符重载«/»: `T opBinary(string op)(T rhs)` — 未实现
- «bold»UFCS (统一函数调用语法)«/»: `obj.method()` 等价 `method(obj)`，未实现
- «bold»模块构造函数/析构函数«/»: `static this()`, `static ~this()` — 未实现
- «bold»嵌套函数/闭包«/»: 函数内定义函数，未实现
- «bold»Ranges (范围)«/»: `std.range` 等惰性求值抽象，未实现
- «bold»list (链表字面量)«/»: 如 `[1, 2, 3]`，未实现

---

#### 编译选项

通过 CLI 工具 `VMLTool` 调用时支持的编译选项：

| 选项 | 含义 |
|------|------|
| `-o <file>` | 输出文件路径 |
| `-mode mcu` | MCU 安全子集模式 (默认) |
| `-mode os` | 完整 OS 模式 |
| `--std=<std>` | 指定语言标准版本 |
| `-D <macro>` | 定义预处理宏 |
| `-U <macro>` | 取消预处理宏定义 |
| `-I <path>` | 添加 include 搜索路径 |
| `-L <path>` | 添加库搜索路径 |
| `-static` | 静态链接 |
| `-shared` | 动态链接 |
| `-target <arch>` | 指定目标架构 |

---

#### 完整语法参考 (EBNF)

```ebnf
Program       := { TopLevelDecl }

TopLevelDecl  := ModuleDecl | ImportDecl | FuncDef | VarDecl
               | ClassDecl | StructDecl | InterfaceDecl | EnumDecl

ModuleDecl    := "module" Identifier ";"
ImportDecl    := "import" Identifier { "." Identifier } ";"

Type          := "void" | "int" | "float" | "double" | "bool"
               | "string" | "char" | Identifier

VarDecl       := Type Identifier [ "=" Expression ] ";"

FuncDef       := Type Identifier "(" [ ParamList ] ")" Block
ParamList     := Type Identifier { "," Type Identifier }

ClassDecl     := "class" Identifier "{" { Declaration } "}" ";"
StructDecl    := "struct" Identifier "{" { Declaration } "}" ";"
InterfaceDecl := "interface" Identifier "{" { Declaration } "}" ";"
EnumDecl      := "enum" Identifier "{" EnumMember { "," EnumMember } [ "," ] "}" ";"
EnumMember    := Identifier [ "=" Expression ]

Block         := "{" { Statement } "}"
Statement     := ReturnStmt | IfStmt | WhileStmt | DoWhileStmt
               | ForStmt | VarDecl | ExpressionStmt

ReturnStmt    := "return" [ Expression ] ";"
IfStmt        := "if" "(" Expression ")" Block [ "else" (IfStmt | Block) ]
WhileStmt     := "while" "(" Expression ")" Block
DoWhileStmt   := "do" Block "while" "(" Expression ")" ";"
ForStmt       := "for" "(" [ForInit] ";" [ Expression ] ";" [ Expression ] ")" Block
ForInit       := VarDecl | Expression
ExpressionStmt:= Expression ";"

Expression    := Assignment
Assignment    := LogicalOr [ ("=" | "+=" | "-=" | "*=" | "/=") Assignment ]
LogicalOr     := LogicalAnd { "||" LogicalAnd }
LogicalAnd    := Comparison { "&&" Comparison }
Comparison    := Additive [ ("==" | "!=" | "<" | ">" | "<=" | ">=") Additive ]
Additive      := Multiplicative { ("+" | "-") Multiplicative }
Multiplicative:= Unary { ("*" | "/" | "%" | "^^") Unary }
Unary         := ["-" | "!"] Postfix
Postfix       := Primary { "(" [ Expression { "," Expression } ] ")" }

Primary       := Integer | FloatLiteral | StringLiteral | CharLiteral
               | "true" | "false" | "null" | "this"
               | Identifier
               | "(" Expression ")"

Identifier    := Letter { Letter | Digit | "_" }
Integer       := Digit { Digit } | "0x" HexDigit { HexDigit }
FloatLiteral  := Digit { Digit } "." Digit { Digit }
StringLiteral := '"' { Character } '"'
CharLiteral   := "'" Character "'"

Comment       := "//" { Character }
BlockComment  := "/*" { Character } "*/"
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

### D 编译器

«bold»路径«/»: `VMLPrepares/DCompiler/`
«bold»完成度«/»: ~97% | 🟢 生产可用
«bold»标准库«/»: `Lib/d/`

#### 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ 模块系统: module/import
- ✅ 类/结构体/接口/枚举: class/struct/interface/enum
- ✅ C 风格类型系统 (int/float/double/bool/string/char/void)
- ✅ 控制流 (if/else/while/for/do-while/foreach/break/continue)
- ✅ 运算符: ++/--/位运算/三元运算符
- ✅ 32-bit signed int, 32-bit float, 64-bit double, 8-bit char
- ✅ 36 测试 0 失败
- ⚠️ 模板元编程 — 未实现
- ⚠️ contract/invariant — 未实现
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include)

#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化。

«bold»跳过«/»（MCU 不支持）:
- 模板元编程 (编译期展开过大)
- contract/invariant

«bold»保留«/»:
- 基本 OOP (class/struct/interface)
- 模块导入
- 完整运算符支持

##### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

##### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

#### 使用
```bash
dotnet run --project VMLTool -- input.d -o output.vml
```

#### 测试
- `VMLTests/NewCompilerTests.cs` — 36 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/d/` — 示例程序 (start/factorial/file_io/info)
- `Examples/benchmark/` — bench_int_d.d / bench_float_d.d
