# Rust language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | Rust 2018 subset (1.31+) |
| **Release year** | 2018 |
| **Completeness** | ~93% |
| **MCU completeness** | ~90% |
| **Tests** | 0 (the test directory is yet to be created) |
| **Updates** | 2026-05-18: corrected the target standard version (to include 2018+ features such as async/await/dyn), the completeness and the test counts |

## Keywords

`as` `break` `const` `continue` `crate` `else` `enum` `extern` `false` `fn`
`for` `if` `impl` `in` `let` `loop` `match` `mod` `move` `mut` `pub` `ref`
`return` `self` `Self` `static` `struct` `super` `trait` `true` `type` `unsafe`
`use` `where` `while` `async` `await` `dyn`

## Overview

This compiler compiles a subset of the Rust language into VML (Virtual Machine Language) assembly. Rust is a systems programming language that emphasizes memory safety, concurrency and zero-cost abstractions.

## Language features

### 1. Basic Rust syntax

#### Variable declaration
```rust
let x = 10;              // immutable variable
let mut y = 20;          // mutable variable
const MAX_SIZE: i32 = 100; // constant
static COUNTER: i32 = 0;   // static variable
```

#### Type annotations
```rust
let x: i32 = 10;
let y: f64 = 3.14;
let name: &str = "Rust";
let flag: bool = true;
```

### 2. Data types

#### Literals
- **Integer**: `42`, `-10` (decimal)
- **Hexadecimal**: `0xFF`, `0x3FF44000` (prefix `0x`/`0X`)
- **Float**: `3.14`, `1.0`
- **Boolean**: `true`, `false`
- **Character**: `'A'`
- **String**: `"hello"`

#### Scalar types
- **Integers**: `i8`, `i16`, `i32`, `i64`, `i128`, `isize`
- **Unsigned integers**: `u8`, `u16`, `u32`, `u64`, `u128`, `usize`
- **Floats**: `f32`, `f64`
- **Boolean**: `bool`
- **Character**: `char` (a Unicode scalar value)

#### Compound types
- **Tuple**: `(i32, f64, char)`
- **Array**: `[i32; 5]`
- **Slice**: `&[i32]`
- **String slice**: `&str`
- **String**: `String`

### 3. Control flow

#### Conditionals
```rust
let number = 7;

if number < 5 {
    println!("less than 5");
} else if number < 10 {
    println!("less than 10");
} else {
    println!("10 or greater");
}

// if expression
let result = if number > 0 { "positive" } else { "non-positive" };
```

#### Loops
```rust
// loop (an infinite loop)
let mut count = 0;
loop {
    count += 1;
    if count == 10 {
        break;
    }
}

// while loop
let mut number = 3;
while number != 0 {
    println!("{}!", number);
    number -= 1;
}

// for loop
for i in 1..=5 {
    println!("{}", i);
}

let arr = [10, 20, 30, 40];
for element in arr.iter() {
    println!("{}", element);
}
```

#### match expressions
```rust
let value = 42;

match value {
    1 => println!("one"),
    2 | 3 => println!("two or three"),
    4..=10 => println!("four through ten"),
    _ => println!("something else"),
}

// pattern matching
let pair = (0, -2);
match pair {
    (0, y) => println!("x is 0, y = {}", y),
    (x, 0) => println!("x = {}, y is 0", x),
    _ => println!("neither is 0"),
}
```

### 4. Functions

#### Function definition
```rust
fn add(x: i32, y: i32) -> i32 {
    x + y  // implicit return (no semicolon)
}

// explicit return
fn subtract(x: i32, y: i32) -> i32 {
    return x - y;
}

// function with no return value
fn print_hello() {
    println!("Hello!");
}
```

#### Function parameters
```rust
// value parameter
fn by_value(mut x: i32) {
    x += 1;
    println!("Inside: {}", x);
}

// reference parameter
fn by_reference(x: &mut i32) {
    *x += 1;
    println!("Inside: {}", x);
}

// slice parameter
fn print_slice(slice: &[i32]) {
    for &item in slice {
        print!("{} ", item);
    }
    println!();
}
```

### 5. The ownership system

#### Ownership rules
```rust
let s1 = String::from("hello");
let s2 = s1;  // s1's ownership moves to s2
// println!("{}", s1);  // error! s1 is no longer valid

// clone (a deep copy)
let s3 = s2.clone();
println!("s2 = {}, s3 = {}", s2, s3);

// references (borrowing)
let len = calculate_length(&s3);
println!("Length of '{}' is {}", s3, len);
```

#### Reference rules
```rust
fn calculate_length(s: &String) -> usize {
    s.len()
}

// mutable reference
fn change(s: &mut String) {
    s.push_str(", world");
}

let mut s = String::from("hello");
change(&mut s);
println!("{}", s);  // "hello, world"
```

### 6. Structs and enums

#### Structs
```rust
struct Point {
    x: i32,
    y: i32,
}

impl Point {
    // associated function (a static method)
    fn new(x: i32, y: i32) -> Point {
        Point { x, y }
    }
    
    // method
    fn distance_from_origin(&self) -> f64 {
        ((self.x * self.x + self.y * self.y) as f64).sqrt()
    }
}

let p = Point::new(3, 4);
println!("Distance: {}", p.distance_from_origin());
```

#### Enums
```rust
enum IpAddr {
    V4(u8, u8, u8, u8),
    V6(String),
}

let home = IpAddr::V4(127, 0, 0, 1);
let loopback = IpAddr::V6(String::from("::1"));

// the Option enum
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

### 7. Error handling

#### The Result type
```rust
use std::fs::File;
use std::io::Error;

fn read_file(path: &str) -> Result<String, Error> {
    let mut file = File::open(path)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    Ok(contents)
}

// handling a Result
match read_file("data.txt") {
    Ok(contents) => println!("File contents: {}", contents),
    Err(e) => println!("Error reading file: {}", e),
}

// unwrap and expect
let contents = read_file("data.txt").unwrap();  // panics on error
let contents = read_file("data.txt").expect("Failed to read file");
```

#### panic and recovery
```rust
fn risky_operation(x: i32) -> i32 {
    if x < 0 {
        panic!("Negative value not allowed");
    }
    x * 2
}

// catch_unwind (an unstable API)
let result = std::panic::catch_unwind(|| {
    risky_operation(-5)
});
```

### 8. Module system

#### Module definition
```rust
// src/lib.rs or a module file
pub mod math {
    pub fn add(x: i32, y: i32) -> i32 {
        x + y
    }
    
    pub fn subtract(x: i32, y: i32) -> i32 {
        x - y
    }
    
    // private function
    fn internal_helper() {
        // ...
    }
}
```

#### Using modules
```rust
// using an absolute path
use crate::math::add;

// using a relative path
use self::math::subtract;

// renaming
use std::fmt::Result as FmtResult;

fn main() {
    let sum = add(10, 5);
    let diff = subtract(10, 5);
    println!("Sum: {}, Difference: {}", sum, diff);
}
```

### 9. Traits

#### Trait definition
```rust
trait Shape {
    fn area(&self) -> f64;
    fn perimeter(&self) -> f64;
    
    // default implementation
    fn description(&self) -> String {
        String::from("A shape")
    }
}
```

#### Trait implementation
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

#### Trait bounds
```rust
fn print_area<T: Shape>(shape: &T) {
    println!("Area: {}", shape.area());
}

// using a where clause
fn compare_shapes<T, U>(shape1: &T, shape2: &U) 
where
    T: Shape,
    U: Shape,
{
    println!("Shape1 area: {}", shape1.area());
    println!("Shape2 area: {}", shape2.area());
}
```

### 10. Generics

#### Generic functions
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

#### Generic structs
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

// implementing methods for a specific type
impl Point<f64> {
    fn distance_from_origin(&self) -> f64 {
        (self.x.powi(2) + self.y.powi(2)).sqrt()
    }
}
```

### 11. VML code generation conventions

#### Implementing the ownership system
Rust's ownership system is implemented in VML with reference counting:
```
String structure:
offset 0: reference count
offset 4: capacity
offset 8: length
offset 12: data pointer
```

#### Function calling convention
```rust
fn add(x: i32, y: i32) -> i32 {
    x + y
}

VML code:
LABEL add
    ; save the frame pointer
    PUSH R14
    MOVE R14, R13
    
    ; access the arguments
    MOVE R0, [R14+12]   ; x
    MOVE R1, [R14+8]    ; y
    ADD R0, R0, R1      ; x + y
    
    ; the return value is in R0
    POP R14
    RET
```

#### Implementing error handling
```rust
Result<T, E> enum implementation:
value 0: Ok variant tag
value 4: Ok value (if the tag is Ok)
value 8: Err variant tag
value 12: Err value (if the tag is Err)
```

### 12. Example programs

#### Factorial
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

#### Fibonacci sequence
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

#### A small vector library
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

### Floating-point and 64-bit compilation modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compilation flags:

| Flag | Possible values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit float (f32) handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit float (f64) handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer (i64/u64) handling mode |

#### 32-bit float (float32)

The 32-bit single-precision float type `f32` in this language compiles as follows:

- **`hard` mode (default)**: uses the VML native float instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly through the sixteen float registers F0-F15. Best performance; suited to target platforms with float hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating float arithmetic through the `__vml_float_add/sub/mul/div/neg/abs/cmp` functions and so on. Suited to MCU platforms without float hardware.
- **`none` mode**: disables every 32-bit float type; a compile error is reported when an `f32` declaration is met.

#### 64-bit float (double)

The 64-bit double-precision float type `f64` in this language compiles as follows:

- **`soft` mode (default)**: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through the `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` functions and so on. Compatible with every platform (MCU included).
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit arithmetic.
- **`none` mode**: disables every 64-bit float type; a compile error is reported when an `f64` declaration is met.

#### 64-bit integer (int64)

The 64-bit integer types `i64` / `u64` in this language compile as follows:

- **`soft` mode (default)**: uses the dual-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through the `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` functions and so on.
- **`hard` mode**: reserved; a future VML version will support native 64-bit integer instructions.
- **`none` mode**: disables the 64-bit integer types; a compile error is reported when an `i64` / `u64` declaration is met.

#### Software emulation libraries

The software emulation libraries above all live in the `Lib/shared/` directory, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit float emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit float emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer dual-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

### 13. Compilation limitations

#### Current implementation status
- **Lexer**: fully implemented, supports Rust keywords and symbols
- **Parser**: fully implemented, supports statements, expressions and functions
- **Code generator**: basically implemented, generates VML instructions
- **Standard library**: the `Lib/rust/` directory exists

#### Implemented
- ✅ Variable declaration (let/mut), constants (const)
- ✅ Control flow: if/else/loop/while/for
- ✅ Functions: fn definition and calls
- ✅ Basic types: i32/f64/bool/char/str

#### Core features yet to be implemented
1. Ownership and borrow checker (a simplified version)
2. Pattern matching (match) code generation
3. struct/enum/trait/impl
4. Generic support
5. Error handling (Result/Option)
6. async/await (skipped in MCU mode)

#### Technical challenges
1. **Ownership system**: Rust's core feature, requiring static analysis (the simplified VML version does not implement a borrow checker for now)
2. **Lifetimes**: the static checking of reference validity
3. **Pattern matching**: complex pattern destructuring
4. **Traits/generics**: the monomorphization compilation strategy

### 14. Integration with the VML runtime

Rust programs interact with the VML runtime through system calls:

- **SYSCALL 4**: output a character (used by print)
- **SYSCALL 6**: output an integer
- **SYSCALL 140**: allocate memory (Box, Vec, String)
- **SYSCALL 141**: free memory
- **SYSCALL 142**: panic handling
- **SYSCALL 143**: stack unwinding

Rust's memory-safety characteristics and high performance make it suited to systems programming and embedded development. Through the VML compiler, Rust programs can run in resource-constrained environments.
---

## 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU mode** (default): strings are output as UTF-8 (`.string`)
**OS mode**: the encoding can be determined through the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h), which each language compiler can use as needed.
