# Objective-C

语法是 C 的超集，UI 接口用法与 C 完全相同。

## 在手机上怎么跑

```
vml run examples/objc/file_io.m
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 直接调用 `ui_*` 函数
- 可以写 C 风格的代码，也可以用 `@interface`

## 示例

| 文件 | 演示什么 |
|---|---|
| `file_io.m` | 读写文件 |
| `parserexpf_demo.m` | 调用共享库解析表达式 |
| `snake.m` | 贪吃蛇（**不引头文件**的写法看这份） |
| `sysinfo.m` | 设备信息 |

## 实测踩过的坑

- ⚠ «bold»这个前端解析不了 `waycoder_ui.h`«/»（会报 `expected )`）——
  «bold»别引头文件«/»，直接调用即可（`examples/objc/snake.m` 就是这么写的）。

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/ObjCCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Objective-C 语言编译器规范说明

> «bold»版本«/»：v1.1 | «bold»日期«/»：2026-07-06 | «bold»修订者«/»：深圳市探索智能科技有限公司

| 属性 | 值 |
|------|-----|
| «bold»标准«/» | Objective-C 2.0 子集 |
| «bold»年份«/» | 1984 (ObjC 2.0: 2006) |
| «bold»完成度«/» | ~99% |
| «bold»扩展名«/» | `.m`, `.mm` |
| «bold»编译器入口«/» | `ObjCCompiler.Compile()` / `ObjCCompiler.CompileFile()` |
| «bold»源文件«/» | `Lexer.cs` (145 行) / `Parser.cs` (440 行) / `CodeGenerator*.cs` (399 行) / `ASTNode.cs` (76 行) |

---

#### 一、概述

本编译器实现 Objective-C 2.0 的子集，兼容 C 语法，并扩展面向对象特性。核心设计思路是将 C 函数和 ObjC 消息发送统一编译到 VML IR，由运行时库提供 `NSObject` 等基础类的消息分发骨架。

«bold»架构分层«/»：
```
Objective-C 源码 (.m, .mm)
    → Lexer (词法分析) → Token 流
    → Parser (语法分析) → AST
    → CodeGenerator (代码生成) → VMLProgram (VML IR)
    → VMLAssembler / VMLRuntime → 执行
```

«bold»编译流水线«/»：
1. 预处理 (`Preprocessor`) — 展开 `#import`/`#include`/`#define` 及预定义宏
2. 词法分析 (`Lexer`) — 源码切割为 Token 流，识别 ObjC 特有 `@` 关键字及 C 运算符
3. 语法分析 (`Parser`) — 递归下降解析，构建 AST 节点树
4. 代码生成 (`CodeGenerator`) — 将 AST 翻译为 VML 指令序列

---

#### 二、支持的特性

##### 2.1 C 兼容类型

支持基本 C 类型系统。类型关键字由 `Lexer.IsTypeName()` 集中判定。

| 类型关键字 | 说明 | 示例 |
|-----------|------|------|
| `int` | 32 位有符号整数 | `int a = 10;` |
| `float` | 32 位浮点 | `float f = 3.14;` |
| `double` | 64 位浮点 | `double d = 2.718;` |
| `char` | 8 位字符 | `char c = 'A';` |
| `void` | 无返回值 / 空类型 | `void func() { }` |
| `short` | 16 位整数 | `short s = 100;` |
| `long` | 32 位整数（`long long` 支持 64 位） | `long l = 99999;` |
| `signed` | 有符号修饰 | `signed int x;` |
| `unsigned` | 无符号修饰 | `unsigned int u;` |
| `struct` | 结构体声明（«bold»词法器支持，语法解析器中 `struct` 可被识别为类型名，但结构体成员访问 `.`/`->` 的语义解析尚未完整实现«/»） | `struct Point p;` |
| `enum` | 枚举声明（«bold»词法器识别为关键字，语法解析器将其作为类型名识别，但枚举成员定义尚未实现«/»） | `enum Color c;` |
| `id` | ObjC 通用对象指针，等价于 `void *` | `id obj = nil;` |

«bold»类型修饰符组合规则«/»：`unsigned` / `signed` 可后接基础类型名（如 `unsigned int`），解析器递归拼接类型字符串。

##### 2.2 ObjC 关键字

所有 ObjC 关键字以 `@` 前缀，由词法器在 `@` 字符后读取字母序列并匹配。

###### 2.2.1 核心定义关键字

| 关键字 | 用途 | 解析情况 |
|--------|------|----------|
| `@interface` | 声明类接口（实例变量 + 方法声明） | «bold»完整解析«/» — 支持类名、父类、实例变量块、方法声明列表 |
| `@implementation` | 实现类方法 | «bold»完整解析«/» — 支持方法体实现 |
| `@end` | 终止 `@interface` / `@implementation` 块 | «bold»完整解析«/» |
| `@protocol` | 协议声明（«bold»词法器识别，语法解析器未实现解析逻辑«/»） | 仅 Token 可用 |
| `@class` | 前向声明类名 | «bold»语法解析器识别并跳过«/»（不生成代码，无语义效果） |
| `@property` | 属性声明（«bold»词法器识别，语法解析器未实现解析逻辑«/»） | 仅 Token 可用 |
| `@synthesize` | 属性存取器合成（«bold»词法器识别，语法解析器未实现解析逻辑«/»） | 仅 Token 可用 |
| `@dynamic` | 属性存取器运行时提供（«bold»词法器识别，语法解析器未实现解析逻辑«/»） | 仅 Token 可用 |
| `@selector` | 选择器字面量（«bold»词法器识别，语法解析器未实现解析逻辑«/»） | 仅 Token 可用 |

«bold»`@interface` 语法«/»：
```objc
@interface MyClass : SuperClass {
    // 实例变量（仅支持类型+名称，不支持指针 * 语法）
    int count;
    float value;
    id delegate;
}
// 方法声明
- (void)doSomething;
- (int)add:(int)a to:(int)b;
@end
```

«bold»`@implementation` 语法«/»：
```objc
@implementation MyClass

// 方法实现
- (void)doSomething {
    count = count + 1;
    return;
}

// C 函数也可混合在 @implementation 中
int helperFunc(int x) {
    return x * 2;
}

@end
```

###### 2.2.2 消息表达式

支持 Objective-C 经典方括号消息发送语法：

```objc
// 基本消息发送
[receiver methodName]

// 带参数的消息发送（参数与选择器标签交织）
[obj setX:10 y:20]

// 无参数消息
[array count]

// 嵌套调用（接收者可为另一消息表达式的结果）
[[self delegate] update]
```

«bold»代码生成«/»：消息发送编译为 `CALL objc_<method>` 指令，参数和接收者依次 `PUSH` 到栈上。运行时需提供命名规则为 `objc_<selectorName>` 的函数实现消息分发。

###### 2.2.3 NSString 字面量

支持 `@"string"` 语法创建 ObjC 字符串常量：

```objc
id greeting = @"Hello, World!";
```

«bold»内部实现«/»：词法器将 `@"..."` 识别为 `ObjCString` Token（值为 `@"...内容..."`），代码生成器将其作为字符串字面量处理，存入数据段并通过 `MOVE R0, label`（LEA 语义）加载地址。

###### 2.2.4 特殊值

| 值 | 类型 | 编译为 |
|----|------|--------|
| `nil` | 空对象指针 | `MOVE R0, 0` |
| `YES` | 布尔真 | `MOVE R0, 1` |
| `NO` | 布尔假 | `MOVE R0, 0` |
| `self` | 当前对象引用（方法内隐式参数） | 栈帧偏移 0（R12 基址） |
| `super` | 父类引用（«bold»词法器识别，语义未实现«/»） | 仅 Token 可用 |

##### 2.3 方法声明

实例方法（`-`）和类方法（`+`）均支持：

```objc
// 实例方法 — 无参数
- (int)getCount {
    return count;
}

// 实例方法 — 一个参数
- (void)setCount:(int)newCount {
    count = newCount;
}

// 实例方法 — 多个参数（交错选择器标签）
- (int)add:(int)a to:(int)b {
    return a + b;
}

// 类方法（+）
+ (id)sharedInstance {
    return self;
}
```

«bold»方法名编码«/»：选择器中的冒号被替换为下划线。例如 `add:to:` 编译为标签 `objc_add_to_`。

##### 2.4 运算符

###### 2.4.1 完整支持的运算符（语法解析器 + 代码生成器均实现）

| 类别 | 运算符 | 说明 |
|------|--------|------|
| 算术 | `+` `-` `*` `/` `%` | 加减乘除取模 |
| 比较 | `==` `!=` `<` `>` `<=` `>=` | 等于/不等于/大小比较 |
| 赋值 | `=` `+=` `-=` `*=` `/=` `%=` | 赋值与复合赋值 |
| 一元 | `-` `!` `++` `--` | 负号/逻辑非/自增/自减 |
| 解引用 | `*` | 指针解引用（«bold»词法+语法解析支持，代码生成仅做寄存器间接加载«/»） |
| 取地址 | `&` | 取地址（«bold»词法+语法解析支持，代码生成仅做寄存器间接存储«/»） |

###### 2.4.2 仅词法器支持（语法解析器未实现语义）

| 运算符 | Token 类型 | 说明 |
|--------|-----------|------|
| `&&` `\|\|` | `And`, `Or` | 逻辑与/或（«bold»短路求值未实现«/»） |
| `&` `\|` `^` `~` | `Amp`, `Pipe`, `Caret`, `Tilde` | 位运算与/或/异或/取反 |
| `<<` `>>` | `LShift`, `RShift` | 左移/右移 |
| `->` | `Arrow` | 结构体指针成员访问 |
| `? :` | `Question`, `Colon` | 三元条件运算符 |
| `...` | `Ellipsis` | 可变参数（语法解析器识别但无语义） |

##### 2.5 C 函数声明

```objc
// 标准函数声明
int add(int a, int b) {
    return a + b;
}

// 无参数函数
void sayHello() {
    return;
}

// 多参数函数
float average(float a, float b, float c) {
    return (a + b + c) / 3.0;
}

// 前向声明（函数体以分号替代）
int factorial(int n);
```

«bold»调用约定«/»：参数通过 `PUSH` 入栈，`CALL func_<name>` 调用，`RET` 返回，返回值在 R0。

«bold»函数重载«/»：不支持。函数名在全程序中必须唯一。

##### 2.6 控制流

###### 2.6.1 if / else

```objc
if (x > 0) {
    return 1;
} else if (x == 0) {
    return 0;
} else {
    return -1;
}
```

«bold»实现«/»：条件求值后 `CMP R0, 0` + `JE elseLabel`，支持链式 `else if`。

###### 2.6.2 while

```objc
while (i < 10) {
    i = i + 1;
}
```

«bold»实现«/»：循环头标签 + 条件 `JE` 跳转出口 + 循环尾无条件 `JMP` 回循环头。

###### 2.6.3 for（C 风格）

```objc
for (int i = 0; i < 10; i = i + 1) {
    total = total + i;
}
```

«bold»实现«/»：初始化在循环外执行，条件在每次迭代前求值，更新在循环体后执行。

###### 2.6.4 当前不支持的控制流（词法器已识别关键字）

以下 C 控制流关键字已由词法器定义 Token，但语法解析器未实现解析逻辑：

- `switch` / `case` / `default` — switch 多分支选择
- `do` ... `while` — do-while 循环
- `break` / `continue` — 循环中断/继续
- `goto` — 无条件跳转

##### 2.7 注释

```objc
// 单行注释

/* 
   多行注释
   支持跨行
*/
```

两种注释均在词法分析阶段直接丢弃，不进入 Token 流。

##### 2.8 预处理

编译器支持 `#import`、`#include` 和 `#define` 预处理指令。`#` 开头的行由词法器识别为 `Hash` Token，语法解析器调用 `SkipPreprocessor()` 跳过至换行。实际预处理逻辑在 `ObjCCompiler.Compile()` 入口处由 `Preprocessor` 类处理：

```objc
#import <Foundation/Foundation.h>
#include "common.h"
#define MAX_SIZE 100
```

---

#### 三、表达式优先级

递归下降解析器的函数调用链定义了以下优先级（从低到高）：

| 优先级 | 运算符 | 结合性 | 解析函数 |
|--------|--------|--------|----------|
| 1 (最低) | `=` `+=` `-=` `*=` `/=` `%=` | 右结合 | `ParseAssignment()` |
| 2 | `==` `!=` `<` `>` `<=` `>=` | 左结合 | `ParseComparison()` |
| 3 | `+` `-` | 左结合 | `ParseAdditive()` |
| 4 | `*` `/` `%` | 左结合 | `ParseMultiplicative()` |
| 5 | `-` `!` `*` `&` `++` `--`（前缀） | 右结合 | `ParseUnary()` |
| 6 (最高) | 字面量、变量、函数调用、消息发送、括号 | — | `ParsePrimary()` |

«bold»注意«/»：
- `&&` 和 `||` 尚未纳入表达式解析链，不可在表达式中使用
- 位运算 `&` `|` `^` `<<` `>>` 仅在词法层面识别
- 三元运算符 `? :` 未实现

---

#### 四、预定义宏

编译器在启动时自动定义以下宏（见 `ObjCCompiler.PredefinedMacros`）：

| 宏名 | 值 | 说明 |
|------|-----|------|
| `__VML__` | `1` | 标识 VML 编译环境 |
| `__VML_VERSION__` | `"1.65.32"` | VML 工具链版本号 |
| `__OBJC__` | `1` | 标识 Objective-C 编译器 |
| `__DATE__` | `"Jun 02 2026"`（编译时日期） | 编译日期（`MMM dd yyyy` 格式） |
| `__TIME__` | `"15:30:45"`（编译时时间） | 编译时间（`HH:mm:ss` 格式） |

这些宏在传递给 `Preprocessor` 时作为 `PredefinedMacros` 字典注入，优先于源码中的 `#define` 生效。

---

#### 五、AST 节点类型

| 节点类 | 用途 | 关键字段 |
|--------|------|----------|
| `ProgramNode` | 顶层程序根节点 | `Statements: List<ASTNode>` |
| `FuncDeclNode` | C 函数声明/定义 | `Name`, `ReturnType`, `Parameters`, `Body` |
| `VarDeclNode` | 变量声明 | `Type`, `Name`, `Init` |
| `ObjCInterfaceNode` | `@interface` 声明 | `Name`, `SuperClass`, `Members` |
| `ObjCImplNode` | `@implementation` 定义 | `Name`, `Methods` |
| `ObjCMethodNode` | ObjC 方法 | `Name`（含冒号）, `ReturnType`, `Parameters`, `Body` |
| `ReturnNode` | return 语句 | `Value`（可为 null） |
| `IfNode` | if / else 语句 | `Condition`, `ThenBody`, `ElseBody` |
| `WhileNode` | while 循环 | `Condition`, `Body` |
| `ForNode` | for 循环 | `Init`, `Condition`, `Update`, `Body` |
| `LiteralNode` | 字面量 | `Value`（int / float / string） |
| `VarNode` | 变量引用 | `Name` |
| `AssignNode` | 简单赋值 `=` | `Name`, `Value` |
| `BinaryNode` | 二元表达式 | `Left`, `Op`, `Right` |
| `UnaryNode` | 一元表达式 | `Op`, `Operand` |
| `CallNode` | C 函数调用 | `Name`, `Arguments` |
| `MsgSendNode` | ObjC 消息发送 | `Receiver`, `Method`（含冒号）, `Arguments` |

---

#### 六、代码生成细节

##### 6.1 寄存器约定

与 VML 内部统一标准一致：

| 寄存器 | 用途 |
|--------|------|
| R0 | 累加器 / 函数返回值 / 第一参数 |
| R1-R3 | 函数参数 |
| R4-R11 | 通用寄存器（调用者保存） |
| R12 | 帧指针 (BP)（被调用者保存） |
| R13 | 栈指针 (SP) |
| R14 | 链接寄存器 (LR)（被调用者保存） |
| R15 | 返回地址 (RA)（由 CALL/RET 自动管理） |
| F0-F15 | 单精度浮点寄存器（调用者保存） |
| D0-D7 | 双精度浮点寄存器（调用者保存） |
| L0-L7 | 64位长整数寄存器（调用者保存） |

##### 6.2 函数/方法命名规则

| 类型 | 标签前缀 | 示例 |
|------|----------|------|
| C 函数 | `func_` | `int main()` → `func_main` |
| ObjC 方法 | `objc_` | `- (void)doIt:(int)x` → `objc_doIt_` |

函数体前生成 `JMP skip_<label>` 指令以跳过函数体（防止执行流误入），函数体以 `RET` 结尾。

##### 6.3 变量存储

- «bold»局部变量«/»：栈帧内分配，通过 `symbolTable` 字典将变量名映射到 `R12-{offset}` 的栈偏移量。首次引用时自动分配 4 字节空间
- «bold»全局变量«/»：存储在数据段 (`dataSection`)，通过 `var_<name>` 标签寻址
- «bold»字符串常量«/»：存储在数据段，生成 `str_<GUID>` 标签，通过 `MOVE R0, label` 加载地址

##### 6.4 消息发送代码生成

```objc
[obj setX:10 y:20]
```

编译为以下 VML 指令序列：
```asm
; 参数入栈（逆序先不必要，所有参数按序 PUSH）
MOVE R0, 10
PUSH R0
MOVE R0, 20
PUSH R0
; 接收者入栈
MOVE R0, [R12-{obj_offset}]
PUSH R0
; 调用方法
CALL objc_setX_y_
```

##### 6.5 `self` 处理

在 ObjC 方法体内，`self` 被自动加入符号表，偏移为 0（即帧指针 R12 指向的栈帧顶部）。方法体可使用 `self` 作为普通变量引用。

##### 6.6 标准库链接

编译文件时 (`CompileFile`)，自动：
1. 从 `#import` / `#include` 指令提取依赖库名
2. 按 `objc` 语言类型在 `Lib/` 目录下解析库文件
3. 调用 `LibraryLinker` 链接外部符号

`extractImports` 正则同时匹配 `#import` 和 `#include`，支持尖括号 `<>` 和双引号 `""` 两种引用形式。

---

#### 七、已知限制

| 限制项 | 详细说明 |
|--------|----------|
| «bold»@property 合成«/» | 词法器识别 `@property`/`@synthesize`/`@dynamic`，但语法解析器完全未实现存取器自动合成逻辑。属性需手工编写 getter/setter 方法 |
| «bold»@protocol 协议«/» | 词法器识别 `@protocol` Token，语法解析器未实现协议声明、采纳或一致性检查。类型系统不支持 `id<Protocol>` 语法 |
| «bold»Category 分类«/» | 完全未实现。不支持 `@interface ClassName (CategoryName)` 扩展语法 |
| «bold»Block / Closure«/» | 完全未实现。不支持 `^` block 语法及闭包捕获 |
| «bold»ARC 内存管理«/» | 不支持自动引用计数，无 `strong`/`weak`/`unsafe_unretained` 等所有权修饰符。需手工管理对象生命周期 |
| «bold»属性修饰符«/» | `@property (nonatomic, strong)` 等属性修饰符语法完全未解析 |
| «bold»消息转发«/» | `forwardInvocation:` / `methodSignatureForSelector:` 机制未实现 |
| «bold»KVC/KVO«/» | 键值编码和键值观察机制未实现 |
| «bold»异常处理«/» | `@try` `@catch` `@finally` `@throw` 完全未实现（MCU 模式跳过） |
| «bold»struct/enum/union 定义«/» | 词法器识别关键字，但结构体成员访问 `.`/`->` 的解析与代码生成未实现 |
| «bold»逻辑运算符 && / \|\|«/» | Token 已定义，但未纳入表达式解析链，无法使用短路逻辑求值 |
| «bold»位运算符«/» | `&` `\|` `^` `~` `<<` `>>` Token 已定义，但表达式解析器仅支持 `&` 和 `*` 作为一元取地址/解引用，不支持二元位运算 |
| «bold»三元运算符«/» | `? :` Token 已定义，但语法解析器未实现条件表达式 |
| «bold»switch/case«/» | Token 已定义，语法解析器未实现多分支选择 |
| «bold»break/continue«/» | Token 已定义，语法解析器未实现循环控制 |
| «bold»do-while«/» | Token 已定义，语法解析器未实现 |
| «bold»goto / 标签«/» | Token 已定义，语法解析器未实现 |
| «bold»类型转换«/» | 不支持 C 风格显式类型转换 `(type)expr` |
| «bold»数组 / 指针运算«/» | 不支持数组下标 `[]` 和指针算术 |
| «bold»对象分配«/» | 无 `alloc`/`init` 模式或 `+new` 的编译时支持，需依赖运行时手工提供 |
| «bold»super 语义«/» | `super` 关键字仅 Token 可用，无方法查找语义 |
| «bold»多文件编译«/» | 每次调用编译单个 `.m` 文件，不支持链接时跨文件类层级解析 |

---

#### 八、语法参考（EBNF 摘要）

```
program          := { top_level }

top_level        := objc_interface
                  | objc_implementation
                  | objc_forward_class
                  | function_decl
                  | statement

objc_interface   := "@interface" IDENT [ ":" IDENT ] "{" { ivar_decl } "}" { method_decl } "@end"

objc_implementation := "@implementation" IDENT { method_impl | function_decl } "@end"

objc_forward_class := "@class" IDENT ";"

ivar_decl        := type IDENT ";"

method_decl      := ("-" | "+") type method_selector [ ";"]
method_impl      := ("-" | "+") type method_selector "{" { statement } "}"

method_selector  := IDENT [ ":" "(" type ")" IDENT ] { IDENT ":" "(" type ")" IDENT }

function_decl    := type IDENT "(" [ param_list ] ")" ( ";" | block )

param_list       := type IDENT { "," type IDENT }

block            := "{" { statement } "}"

statement        := return_stmt
                  | if_stmt
                  | while_stmt
                  | for_stmt
                  | var_decl
                  | expression ";"

return_stmt      := "return" [ expression ] ";"

if_stmt          := "if" "(" expression ")" ( block | statement ) [ "else" ( block | statement ) ]

while_stmt       := "while" "(" expression ")" ( block | statement )

for_stmt         := "for" "(" [ var_decl | expression ] ";" [ expression ] ";" [ expression ] ")" ( block | statement )

var_decl         := type IDENT [ "=" expression ] ";"

type             := "int" | "char" | "float" | "double" | "short" | "long" | "long long"
                  | "void" | "id" | "struct" IDENT | "enum" IDENT
                  | "unsigned" type | "signed" type

expression       := assignment

assignment       := comparison { ( "=" | "+=" | "-=" | "*=" | "/=" | "%=" ) assignment }

comparison       := additive { ( "==" | "!=" | "<" | ">" | "<=" | ">=" ) additive }

additive         := multiplicative { ( "+" | "-" ) multiplicative }

multiplicative   := unary { ( "*" | "/" | "%" ) unary }

unary            := ( "-" | "!" | "*" | "&" | "++" | "--" ) unary | primary

primary          := NUMBER
                  | STRING
                  | OBJC_STRING
                  | "nil" | "YES" | "NO"
                  | "[" expression method_selector_exp "]"
                  | IDENT [ "(" [ expression { "," expression } ] ")" ]
                  | "(" expression ")"
```

---

#### 九、编译命令示例

```bash
# 编译单个 .m 文件
dotnet run --project VMLTool -- input.m -o output.vml

# 编译 .mm 文件
dotnet run --project VMLTool -- app.mm -o app.vml

# 使用 ObjCCompiler 直接编译（编程接口）
ObjCCompiler.Compile("int main() { return 0; }")
ObjCCompiler.CompileFile("MyClass.m", includePaths, libraryPaths)
```

---

#### 十、版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0 | 2025-12 | 初始实现 — 词法器 + 递归下降解析器 + VML 代码生成 |
| 1.65.17+ | 2026-05 | 集成 `Preprocessor`、`__OBJC__` 预定义宏、`ExtractImports` 链接支持 |

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

### Objective-C 编译器

«bold»路径«/»: `VMLPrepares/ObjCCompiler/`
«bold»完成度«/»: ~99% | 🟢 生产可用
«bold»标准库«/»: `Lib/objc/`
«bold»依赖«/»: CCompiler (ObjC 是 C 的超集)

#### 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ @interface/@implementation/@end 类定义
- ✅ 消息表达式 [obj msg: param]
- ✅ C 语法完全兼容 (if/else/while/for/do-while/switch/case/break/continue)
- ✅ 指针 Type* / 位运算 / 三元运算符
- ✅ 方法声明 (returnType)method:(paramType)param
- ✅ 15 测试 0 失败
- ✅ @property/@synthesize
- ⚠️ @protocol/category — 未实现
- ⚠️ Block/ARC — MCU 跳过
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include/#import)

#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
ObjC 汇编为目标保留 C 兼容子集。

«bold»跳过«/»:
- Block、ARC、Protocol

«bold»保留«/»:
- @interface/@implementation 基础 OOP
- C 完整语法
- 消息发送 (编译为函数调用)

##### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

##### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

#### 使用
```bash
dotnet run --project VMLTool -- input.m -o output.vml
```

#### 测试
- `VMLTests/NewCompilerTests.cs` — 15 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/objc/` — 示例程序 (start/factorial/file_io/info)
