# Dart 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Dart 2.x / 3.x 语法子集 |
| **发布年份** | 2024 |
| **完成度** | ~97% |
| **文件扩展名** | `.dart` |
| **MCU 可用性** | 🟠 受限使用 |

## 概述

本编译器将 Dart 语言源代码编译为 VML (Virtual Machine Language) 汇编代码。编译器实现 Dart 语言的核心 C 风格语法子集 —— 变量声明、控制流、函数/方法和类定义。编译器采用经典的编译流程：预处理 -> 词法分析 -> 语法分析 -> 代码生成，并支持通过 `import` 语句链接 VML 标准库。

## 支持的语言特性

### 1. 数据类型

#### 基本数据类型

- **`int`** — 32 位有符号整数
- **`double`** — 64 位双精度浮点数
- **`String`** — 字符串字面量（双引号），编译为常量数据
- **`bool`** — 布尔类型，字面量 `true` 和 `false` 映射为整数 1 和 0
- **`var`** — 类型推断声明，变量类型由初始化表达式决定
- **`final`** — 不可变变量声明（语义上只读，运行时与 `var` 相同）
- **`void`** — 空返回类型，用于无返回值函数
- **`null`** — 空值字面量，映射为整数 0

注意：当前编译器不执行严格的类型检查。`bool`、`null` 和数值类型在运行时均映射到 VML 的 32 位寄存器值。`String` 作为数据段常量存储，通过 `MOVE R0, label` 引用其地址。

```dart
int x = 42;
double pi = 3.14159;
String msg = "Hello, Dart!";
bool flag = true;
var inferred = 100;       // 推断为 int
final locked = "fixed";   // 不可变
void emptyFunc() { }      // 无返回值
Object? nothing = null;   // null 字面量
```

#### 复合类型（有限支持）

类类型可作为变量声明类型使用，但运行时通过堆栈分配，无完整对象模型：

```dart
MyClass obj;  // 类类型变量（语法支持）
```

### 2. 变量声明和定义

变量声明遵循 `类型 名称 [= 初始化表达式];` 语法。局部变量在栈上分配，全局变量存储在 VML 数据段中。

```dart
// 局部变量（函数/方法内）
int count;
int total = 0;
String label = "start";
bool done = false;

// 全局变量（顶层作用域）
int globalCounter = 0;
double scale = 1.5;

// 类型推断
var name = "Dart";       // String
var size = 100;           // int
var ratio = 0.5;          // double

// 不可变声明
final maxSize = 1024;
final String path = "/data";
```

### 3. 运算符

#### 算术运算符

```dart
+    // 加法
-    // 减法
*    // 乘法
/    // 除法
%    // 取模
-    // 一元取负
```

#### 赋值运算符

```dart
=    // 赋值
+=   // 加后赋值
-=   // 减后赋值
*=   // 乘后赋值
/=   // 除后赋值
%=   // 取模后赋值
```

#### 比较运算符

```dart
==   // 等于
!=   // 不等于
<    // 小于
>    // 大于
<=   // 小于等于
>=   // 大于等于
```

#### 逻辑运算符

```dart
&&   // 逻辑与（短路求值）
||   // 逻辑或（短路求值）
!    // 逻辑非
```

### 4. 控制流语句

#### if-else

```dart
if (condition) {
  // 语句块
} else if (condition2) {
  // 语句块
} else {
  // 语句块
}
```

支持完整的多路分支嵌套。条件表达式为任何数值表达式，0 和 `false`/`null` 视为假，非零值视为真。

#### while 循环

```dart
while (condition) {
  // 循环体
}
```

条件在每次迭代前求值。条件为假时退出循环。

#### for 循环（C 风格）

```dart
for (int i = 0; i < 10; i++) {
  // 循环体
}

for (; ; ) {
  // 无限循环
}
```

支持 C 语言风格的三段式 `for`：初始化、条件判断、递增表达式。三个子句均可省略。等价于 `init; while (condition) { body; increment; }`。

#### return

```dart
return;            // 无返回值
return expression;  // 返回表达式值
```

`return` 语句自动生成函数尾声代码（恢复帧指针、RET 指令）。无参数 `return` 等价于 `return 0`。

### 5. 函数和方法

#### 顶层函数定义

```dart
// 函数定义语法
returnType functionName(paramType paramName, ...) {
  // 函数体
  return value;
}

// 示例
int add(int a, int b) {
  return a + b;
}

void greet(String name) {
  // 无返回值
}

double multiply(double x, double y) {
  return x * y;
}
```

#### 参数

参数列表为逗号分隔的 `类型 名称` 对。不支持命名参数和可选参数。参数在栈上传递（从右到左压栈，调用方负责清理）。

```dart
// 有参函数
int sum(int a, int b, int c) {
  return a + b + c;
}

// 无参函数
void reset() {
  // ...
}
```

#### 方法（类内）

```dart
class Calculator {
  int add(int a, int b) {
    return a + b;
  }

  int subtract(int a, int b) {
    return a - b;
  }
}
```

方法在内部编译为前缀函数：`ClassName_methodName`。例如 `Calculator.add` 编译为函数 `func_Calculator_add`。

#### 函数调用

```dart
int result = add(10, 20);
greet("World");
double area = multiply(3.0, 4.0);
```

递归函数调用完全支持。

### 6. 类

支持基本的类定义，包含方法和字段声明：

```dart
class Point {
  int x;
  int y;

  void move(int dx, int dy) {
    x += dx;
    y += dy;
  }

  int getX() {
    return x;
  }
}
```

**注意事项**：
- 类成员字段仅语法解析，不生成运行时对象布局
- 方法以 `func_ClassName_methodName` 形式编译为独立函数
- 不支持继承 (`extends`)、混入 (`with`)、接口实现 (`implements`)
- 不支持构造函数、`this` 关键字、`new` 关键字
- 类字段通过函数作用域变量 / 全局数据段模拟

### 7. 注释

```dart
// 单行注释

/*
   多行块注释
   支持跨行
*/
```

两种注释风格与 Dart 标准一致。注释在词法分析阶段被移除。

### 8. 导入库

Dart 编译器通过预处理阶段解析 `import` 语句，链接 VML 标准库：

```dart
import 'stdio.vml';         // 导入 VML 库文件
import 'dart:math';         // Dart 风格导入（自动去除 dart: 前缀）
import 'package:core';      // package 风格导入（自动去除 package: 前缀）
```

`import` 语句仅用于链接 VML 运行时库，不实现完整的 Dart 模块系统。编译器会去除非 VML 前缀（`dart:`、`package:`）并尝试解析库路径。

## 表达式优先级表

表达式按优先级从低到高排列：

| 优先级 | 运算符 | 结合性 | 说明 |
|:------:|:-------|:------:|:-----|
| 1 | `=` `+=` `-=` `*=` `/=` `%=` | 右 | 赋值与复合赋值 |
| 2 | `\|\|` | 左 | 逻辑或（短路求值） |
| 3 | `&&` | 左 | 逻辑与（短路求值） |
| 4 | `==` `!=` | 左 | 相等性比较 |
| 5 | `<` `>` `<=` `>=` | 左 | 关系比较 |
| 6 | `+` `-` | 左 | 加法和减法 |
| 7 | `*` `/` `%` | 左 | 乘法、除法、取模 |
| 8 | `-` `!` | 右 | 一元取负、逻辑非 |
| 9 | `()` | — | 函数调用、分组括号 |

### 优先级示例

```dart
// 算术优先级
int a = 2 + 3 * 4;       // 2 + (3 * 4) = 14
int b = (2 + 3) * 4;     // 5 * 4 = 20

// 比较优先级
bool c = a < b && b > 0; // (a < b) && (b > 0)

// 赋值优先级（右结合）
int x = y = 10;          // x = (y = 10)
```

## 预定义宏

编译器在预处理阶段提供以下内置宏：

| 宏名 | 值 | 说明 |
|:-----|:---|:-----|
| `__VML__` | `"1"` | 标识 VML 工具链编译环境 |
| `__VML_VERSION__` | `"1.65.32"` | VML 工具链版本号 |
| `__DART__` | `"1"` | 标识 Dart 语言编译器 |
| `__DATE__` | `"Jun 02 2026"` | 编译时的日期（动态生成） |
| `__TIME__` | `"HH:mm:ss"` | 编译时的时间（动态生成） |

用法示例：

```dart
#if __VML__
// VML 平台特定代码
#endif

#if __DART__
// Dart 编译器扩展代码
#endif
```

## 编译流程

### 1. 预处理阶段
- 如果源代码包含 `#` 预处理器指令，调用 `Preprocessor` 展开宏、处理条件编译
- 移除注释（`//` 和 `/* */` 在词法分析阶段处理）

### 2. 词法分析阶段
- 将源代码转换为令牌流
- 识别关键字、标识符、数字常量、字符串字面量、运算符和分隔符
- 关键字：`class` `void` `int` `double` `String` `bool` `var` `final` `if` `else` `for` `while` `return` `true` `false` `null`

### 3. 语法分析阶段
- 构建抽象语法树 (AST)
- 解析类型定义、变量声明、控制流、表达式和函数定义
- 使用递归下降解析（优先级爬升法处理表达式）

### 4. 代码生成阶段
- 遍历 AST 生成 VML 指令序列
- 栈帧管理：`R12` 为帧指针 (BP)，`R13` 为栈指针 (SP)，`R15` 为返回地址 (RA)
- 函数返回值通过 `R0` 寄存器
- 类方法编译为带类名前缀的独立函数

## 已知限制和不支持的特性

Dart 编译器当前处于🟢 生产可用，仅支持 Dart 语言的核心子集。以下标准 Dart 特性目前不支持：

### 不支持的特性

| 特性类别 | 具体内容 | 计划优先级 |
|:---------|:---------|:----------:|
| **异步** | `async` / `await` 关键字 | 低 |
| **异步类型** | `Future`、`Stream` 类型 | 低 |
| **混入** | `mixin` 关键字、`with` 子句 | 低 |
| **泛型** | `List<int>`、`Map<K,V>` 等参数化类型（语法跳过，不生成对应代码） | 中 |
| **集合字面量** | `[1, 2, 3]` 列表字面量、`{1, 2, 3}` 集字面量、`{"a": 1}` 映射字面量 | 中 |
| **命名参数** | `function({paramName: value})` 命名参数语法 | 低 |
| **可选参数** | `[param]` 位置可选参数、默认参数值 | 低 |
| **构造函数** | 类构造函数、`this` 关键字、初始化列表 | 中 |
| **继承** | `extends`、`implements`、`super` 关键字 | 中 |
| **空安全** | `?` 后缀类型、`??` 运算符、`?.` 运算符、`late` 关键字 | 低 |
| **箭头函数** | `=> expression` 简写语法 | 低 |
| **级联** | `..` 级联操作符 | 低 |
| **枚举** | `enum` 类型定义 | 低 |
| **扩展方法** | `extension on Type` 语法 | 低 |
| **动态类型** | `dynamic` 关键字 | 低 |
| **记录** | `(int, String)` 记录类型 | 低 |
| **模式匹配** | `switch` 表达式模式匹配 | 低 |
| **字符串插值** | `"Hello $name"` 字符串插值 | 低 |
| **顶级常量** | `const` 编译时常量（`final` 仅用于变量声明） | 低 |
| **完整模块系统** | `export`、`part`、`.dart` 文件解析 | 低 |
| **库前缀** | `import '...' as prefix` 库前缀别名 | 低 |
| **类型别名** | `typedef` 函数类型别名 | 低 |

### 部分支持的特性

| 特性 | 支持程度 | 说明 |
|:-----|:---------|:-----|
| **类** | 基本 | 支持方法定义和字段语法，但不生成完整的对象运行时布局 |
| **泛型参数** | 语法级别 | `List<int>` 等泛型语法被跳过，类型解析为 `_gen` 占位符 |
| **类型系统** | 弱类型 | 不执行严格的类型检查，所有标量类型在运行时统一为 VML 的 32 位值 |
| **单精度浮点** | 有限 | `double` 字面量被解析和存储，但运行时可能经转换后使用 |
| **字符串操作** | 有限 | 字符串作为常量存储，通过地址引用，无字符串拼接或插值 |

### 与标准 Dart 的差异

1. **内存模型**：使用 VML 虚拟机的栈 + 数据段模型，无堆分配和 GC
2. **类型系统**：运行时弱类型，所有值统一为32位标量，无运行时类型信息
3. **对象模型**：类方法编译为全局函数，无虚方法表和动态分发
4. **标准库**：使用 VML 运行时库而非 Dart SDK 的 `dart:core` 等核心库
5. **模块系统**：`import` 仅用于 VML 库链接，不解析 `.dart` 源文件依赖
6. **整数大小**：`int` 为 VML 的 32 位有符号整数（标准 Dart 为任意精度整数）

## 使用示例

### 简单程序

```dart
int main() {
  int x = 10;
  int y = 20;
  int sum = x + y;
  return sum;
}
```

### 条件判断

```dart
int max(int a, int b) {
  if (a > b) {
    return a;
  } else {
    return b;
  }
}

void main() {
  int result = max(5, 10);
}
```

### 循环计算

```dart
int factorial(int n) {
  int result = 1;
  for (int i = 1; i <= n; i++) {
    result *= i;
  }
  return result;
}

void main() {
  int f = factorial(5);  // 120
}
```

### 类和方法

```dart
class Counter {
  int value;

  void increment() {
    value += 1;
  }

  void decrement() {
    value -= 1;
  }

  int getValue() {
    return value;
  }
}

void main() {
  // 使用编译后的前缀函数
  // 类字段作为全局变量存储
}
```

## 性能考虑

1. **代码大小**：生成的 VML 代码包含完整的栈帧管理逻辑（序言/尾声），代码量比手写汇编大
2. **执行速度**：在 VML 虚拟机中解释执行，比原生 Dart VM 慢多个数量级
3. **内存使用**：所有变量使用 VML 栈空间，无动态堆分配开销
4. **优化级别**：当前为基本代码生成，无中间优化通道（如常量折叠、死代码消除）

## 相关源文件

| 文件 | 说明 |
|:-----|:-----|
| `DartCompiler.cs` | 编译器主入口，预处理调度、库链接 |
| `Lexer.cs` | 词法分析器：令牌流生成 |
| `Parser.cs` | 语法分析器：递归下降 AST 构建 |
| `ASTNode.cs` | AST 节点类型定义 |
| `Token.cs` | 令牌类型枚举和令牌数据结构 |
| `CodeGenerator.cs` | 代码生成入口：栈帧管理、类处理 |
| `CodeGenerator.Expressions.cs` | 表达式代码生成：字面量、变量、二元/一元运算、函数调用 |
| `CodeGenerator.Statements.cs` | 语句代码生成：变量声明、赋值、控制流 |
| `DartCompilerPlugin.cs` | 插件接口实现 |
| `DartCompiler.csproj` | .NET 项目文件 |

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
