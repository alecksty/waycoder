# Rust 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Rust 2018 子集 (1.31+) |
| **发布年份** | 2018 |
| **完成度** | ~93% |
| **MCU完成度** | ~90% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正目标标准版本（含async/await/dyn等2018+特性）、完成度和测试数 |

## 关键字

`as` `break` `const` `continue` `crate` `else` `enum` `extern` `false` `fn`
`for` `if` `impl` `in` `let` `loop` `match` `mod` `move` `mut` `pub` `ref`
`return` `self` `Self` `static` `struct` `super` `trait` `true` `type` `unsafe`
`use` `where` `while` `async` `await` `dyn`

## 概述

本编译器实现 Rust 语言子集到 VML (Virtual Machine Language) 汇编的编译。Rust 是一种系统编程语言，强调内存安全、并发性和零成本抽象。

## 语言特性

### 1. Rust 基本语法

#### 变量声明
```rust
let x = 10;              // 不可变变量
let mut y = 20;          // 可变变量
const MAX_SIZE: i32 = 100; // 常量
static COUNTER: i32 = 0;   // 静态变量
```

#### 类型注解
```rust
let x: i32 = 10;
let y: f64 = 3.14;
let name: &str = "Rust";
let flag: bool = true;
```

### 2. 数据类型

#### 字面量
- **整数**: `42`, `-10`（十进制）
- **十六进制**: `0xFF`, `0x3FF44000`（`0x`/`0X` 前缀）
- **浮点数**: `3.14`, `1.0`
- **布尔**: `true`, `false`
- **字符**: `'A'`
- **字符串**: `"hello"`

#### 标量类型
- **整数**: `i8`, `i16`, `i32`, `i64`, `i128`, `isize`
- **无符号整数**: `u8`, `u16`, `u32`, `u64`, `u128`, `usize`
- **浮点数**: `f32`, `f64`
- **布尔**: `bool`
- **字符**: `char` (Unicode标量值)

#### 复合类型
- **元组**: `(i32, f64, char)`
- **数组**: `[i32; 5]`
- **切片**: `&[i32]`
- **字符串切片**: `&str`
- **字符串**: `String`

### 3. 控制流

#### 条件语句
```rust
let number = 7;

if number < 5 {
    println!("less than 5");
} else if number < 10 {
    println!("less than 10");
} else {
    println!("10 or greater");
}

// if表达式
let result = if number > 0 { "positive" } else { "non-positive" };
```

#### 循环语句
```rust
// loop循环（无限循环）
let mut count = 0;
loop {
    count += 1;
    if count == 10 {
        break;
    }
}

// while循环
let mut number = 3;
while number != 0 {
    println!("{}!", number);
    number -= 1;
}

// for循环
for i in 1..=5 {
    println!("{}", i);
}

let arr = [10, 20, 30, 40];
for element in arr.iter() {
    println!("{}", element);
}
```

#### match表达式
```rust
let value = 42;

match value {
    1 => println!("one"),
    2 | 3 => println!("two or three"),
    4..=10 => println!("four through ten"),
    _ => println!("something else"),
}

// 模式匹配
let pair = (0, -2);
match pair {
    (0, y) => println!("x is 0, y = {}", y),
    (x, 0) => println!("x = {}, y is 0", x),
    _ => println!("neither is 0"),
}
```

### 4. 函数

#### 函数定义
```rust
fn add(x: i32, y: i32) -> i32 {
    x + y  // 隐式返回（无分号）
}

// 显式返回
fn subtract(x: i32, y: i32) -> i32 {
    return x - y;
}

// 无返回值函数
fn print_hello() {
    println!("Hello!");
}
```

#### 函数参数
```rust
// 值参数
fn by_value(mut x: i32) {
    x += 1;
    println!("Inside: {}", x);
}

// 引用参数
fn by_reference(x: &mut i32) {
    *x += 1;
    println!("Inside: {}", x);
}

// 切片参数
fn print_slice(slice: &[i32]) {
    for &item in slice {
        print!("{} ", item);
    }
    println!();
}
```

### 5. 所有权系统

#### 所有权规则
```rust
let s1 = String::from("hello");
let s2 = s1;  // s1的所有权移动到s2
// println!("{}", s1);  // 错误！s1不再有效

// 克隆（深拷贝）
let s3 = s2.clone();
println!("s2 = {}, s3 = {}", s2, s3);

// 引用（借用）
let len = calculate_length(&s3);
println!("Length of '{}' is {}", s3, len);
```

#### 引用规则
```rust
fn calculate_length(s: &String) -> usize {
    s.len()
}

// 可变引用
fn change(s: &mut String) {
    s.push_str(", world");
}

let mut s = String::from("hello");
change(&mut s);
println!("{}", s);  // "hello, world"
```

### 6. 结构体和枚举

#### 结构体
```rust
struct Point {
    x: i32,
    y: i32,
}

impl Point {
    // 关联函数（静态方法）
    fn new(x: i32, y: i32) -> Point {
        Point { x, y }
    }
    
    // 方法
    fn distance_from_origin(&self) -> f64 {
        ((self.x * self.x + self.y * self.y) as f64).sqrt()
    }
}

let p = Point::new(3, 4);
println!("Distance: {}", p.distance_from_origin());
```

#### 枚举
```rust
enum IpAddr {
    V4(u8, u8, u8, u8),
    V6(String),
}

let home = IpAddr::V4(127, 0, 0, 1);
let loopback = IpAddr::V6(String::from("::1"));

// Option枚举
fn divide(numerator: f64, denominator: f64) -> Option<f64> {
    if denominator == 0.0 {
        None
    } else {
        Some(numerator / denominator)
    }
}

match divide(10.0, 2.0) {
    Some(result) => println!("Result: {}", result),
    None => println!("Cannot divide by zero"),
}
```

### 7. 错误处理

#### Result类型
```rust
use std::fs::File;
use std::io::Error;

fn read_file(path: &str) -> Result<String, Error> {
    let mut file = File::open(path)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    Ok(contents)
}

// 处理Result
match read_file("data.txt") {
    Ok(contents) => println!("File contents: {}", contents),
    Err(e) => println!("Error reading file: {}", e),
}

// unwrap和expect
let contents = read_file("data.txt").unwrap();  // 出错时panic
let contents = read_file("data.txt").expect("Failed to read file");
```

#### panic和恢复
```rust
fn risky_operation(x: i32) -> i32 {
    if x < 0 {
        panic!("Negative value not allowed");
    }
    x * 2
}

// catch_unwind（不稳定的API）
let result = std::panic::catch_unwind(|| {
    risky_operation(-5)
});
```

### 8. 模块系统

#### 模块定义
```rust
// src/lib.rs 或模块文件
pub mod math {
    pub fn add(x: i32, y: i32) -> i32 {
        x + y
    }
    
    pub fn subtract(x: i32, y: i32) -> i32 {
        x - y
    }
    
    // 私有函数
    fn internal_helper() {
        // ...
    }
}
```

#### 模块使用
```rust
// 使用绝对路径
use crate::math::add;

// 使用相对路径
use self::math::subtract;

// 重命名
use std::fmt::Result as FmtResult;

fn main() {
    let sum = add(10, 5);
    let diff = subtract(10, 5);
    println!("Sum: {}, Difference: {}", sum, diff);
}
```

### 9. 特征（Trait）

#### 特征定义
```rust
trait Shape {
    fn area(&self) -> f64;
    fn perimeter(&self) -> f64;
    
    // 默认实现
    fn description(&self) -> String {
        String::from("A shape")
    }
}
```

#### 特征实现
```rust
struct Circle {
    radius: f64,
}

impl Shape for Circle {
    fn area(&self) -> f64 {
        std::f64::consts::PI * self.radius * self.radius
    }
    
    fn perimeter(&self) -> f64 {
        2.0 * std::f64::consts::PI * self.radius
    }
    
    fn description(&self) -> String {
        format!("A circle with radius {}", self.radius)
    }
}

struct Rectangle {
    width: f64,
    height: f64,
}

impl Shape for Rectangle {
    fn area(&self) -> f64 {
        self.width * self.height
    }
    
    fn perimeter(&self) -> f64 {
        2.0 * (self.width + self.height)
    }
}
```

#### 特征约束
```rust
fn print_area<T: Shape>(shape: &T) {
    println!("Area: {}", shape.area());
}

// 使用where子句
fn compare_shapes<T, U>(shape1: &T, shape2: &U) 
where
    T: Shape,
    U: Shape,
{
    println!("Shape1 area: {}", shape1.area());
    println!("Shape2 area: {}", shape2.area());
}
```

### 10. 泛型

#### 泛型函数
```rust
fn largest<T: PartialOrd>(list: &[T]) -> &T {
    let mut largest = &list[0];
    
    for item in list {
        if item > largest {
            largest = item;
        }
    }
    
    largest
}

let numbers = vec![34, 50, 25, 100, 65];
let result = largest(&numbers);
println!("The largest number is {}", result);
```

#### 泛型结构体
```rust
struct Point<T> {
    x: T,
    y: T,
}

impl<T> Point<T> {
    fn x(&self) -> &T {
        &self.x
    }
}

// 为特定类型实现方法
impl Point<f64> {
    fn distance_from_origin(&self) -> f64 {
        (self.x.powi(2) + self.y.powi(2)).sqrt()
    }
}
```

### 11. VML 代码生成约定

#### 所有权系统实现
Rust 的所有权系统在 VML 中通过引用计数实现：
```
String结构:
偏移0: 引用计数
偏移4: 容量
偏移8: 长度
偏移12: 数据指针
```

#### 函数调用约定
```rust
fn add(x: i32, y: i32) -> i32 {
    x + y
}

VML代码:
LABEL add
    ; 保存帧指针
    PUSH R14
    MOVE R14, R13
    
    ; 访问参数
    MOVE R0, [R14+12]   ; x
    MOVE R1, [R14+8]    ; y
    ADD R0, R0, R1      ; x + y
    
    ; 返回值在R0中
    POP R14
    RET
```

#### 错误处理实现
```rust
Result<T, E>枚举实现:
值0: Ok变体标记
值4: Ok值（如果标记为Ok）
值8: Err变体标记
值12: Err值（如果标记为Err）
```

### 12. 示例程序

#### 阶乘计算
```rust
fn factorial(n: u64) -> u64 {
    match n {
        0 | 1 => 1,
        _ => n * factorial(n - 1),
    }
}

fn main() {
    println!("5! = {}", factorial(5));  // 120
}
```

#### 斐波那契数列
```rust
fn fibonacci(n: u32) -> u64 {
    match n {
        0 => 0,
        1 => 1,
        _ => fibonacci(n - 1) + fibonacci(n - 2),
    }
}

fn main() {
    for i in 0..10 {
        println!("fib({}) = {}", i, fibonacci(i));
    }
}
```

#### 简单向量库
```rust
struct Vector3 {
    x: f64,
    y: f64,
    z: f64,
}

impl Vector3 {
    fn new(x: f64, y: f64, z: f64) -> Vector3 {
        Vector3 { x, y, z }
    }
    
    fn dot(&self, other: &Vector3) -> f64 {
        self.x * other.x + self.y * other.y + self.z * other.z
    }
    
    fn cross(&self, other: &Vector3) -> Vector3 {
        Vector3 {
            x: self.y * other.z - self.z * other.y,
            y: self.z * other.x - self.x * other.z,
            z: self.x * other.y - self.y * other.x,
        }
    }
    
    fn magnitude(&self) -> f64 {
        (self.x * self.x + self.y * self.y + self.z * self.z).sqrt()
    }
}

fn main() {
    let v1 = Vector3::new(1.0, 2.0, 3.0);
    let v2 = Vector3::new(4.0, 5.0, 6.0);
    
    println!("Dot product: {}", v1.dot(&v2));
    println!("Magnitude v1: {}", v1.magnitude());
}
```

### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (f32) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (f64) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (i64/u64) 处理模式 |

#### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `f32` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `f32` 声明时报告编译错误。

#### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `f64` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `f64` 声明时报告编译错误。

#### 64位整数 (int64)

本语言中的 64 位整数类型 `i64` / `u64` 按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数类型，遇到 `i64` / `u64` 声明时报告编译错误。

#### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

### 13. 编译限制

#### 当前实现状态
- **词法分析器**: 完整实现，支持 Rust 关键字和符号
- **语法分析器**: 完整实现，支持语句、表达式、函数
- **代码生成器**: 基本实现，生成 VML 指令
- **标准库**: `Lib/rust/` 目录存在

#### 已实现
- ✅ 变量声明 (let/mut)、常量 (const)
- ✅ 控制流: if/else/loop/while/for
- ✅ 函数: fn 定义和调用
- ✅ 基本类型: i32/f64/bool/char/str

#### 核心功能待实现
1. 所有权和借用检查器（简化版）
2. 模式匹配 (match) 代码生成
3. 结构体/enum/trait/impl
4. 泛型支持
5. 错误处理（Result/Option）
6. async/await（MCU 模式跳过）

#### 技术挑战
1. **所有权系统**: Rust 的核心特性，需要静态分析（VML 简化版暂不实现 borrow checker）
2. **生命周期**: 引用有效期的静态检查
3. **模式匹配**: 复杂的模式解构
4. **特征/泛型**: 单态化编译策略

### 14. 与VML运行时集成

Rust 程序通过系统调用与 VML 运行时交互：

- **SYSCALL 4**: 输出字符（用于print）
- **SYSCALL 6**: 输出整数
- **SYSCALL 140**: 分配内存（Box、Vec、String）
- **SYSCALL 141**: 释放内存
- **SYSCALL 142**: 恐慌处理（panic）
- **SYSCALL 143**: 栈展开（unwind）

Rust 的内存安全特性和高性能使其适合系统编程和嵌入式开发。通过 VML 编译器，Rust 程序可以在资源受限的环境中运行。
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
