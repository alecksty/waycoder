# Go

写法与桌面 Go 接近，示例里的贪吃蛇是完整游戏。

## 在手机上怎么跑

```
vml run examples/go/snake.go
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 入口 `package main` + `func main()`
- 直接用 `:=` 声明变量
- 数组与循环都正常（这一路修得比较完整）

## 示例

| 文件 | 演示什么 |
|---|---|
| `snake.go` | 贪吃蛇 |
| `sysinfo.go` | 设备信息 |

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/GoCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Go 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Go 1.0 子集 (2012) |
| **发布年份** | 2012 |
| **完成度** | ~90% |
| **MCU完成度** | ~88% |
| **测试** | 21 通过 |
| **更新** | 2026-05-18: 修正测试数（21个）、完成度；明确实现/待实现状态 |

#### 关键字

`break` `case` `chan` `const` `continue` `default` `defer` `else` `fallthrough`
`for` `func` `go` `goto` `if` `import` `interface` `map` `package` `range`
`return` `select` `struct` `switch` `type` `var`

#### 概述

本编译器实现 Go 语言子集到 VML (Virtual Machine Language) 汇编的编译。Go 是一种静态类型、编译型语言，强调简洁性、并发性和高性能。

#### 语言特性

##### 1. Go 基本语法

###### 变量声明
```go
var x int = 10          // 显式类型声明
var y = 20              // 类型推断
z := 30                 // 短变量声明
const MAX_SIZE = 100    // 常量
```

###### 包声明
```go
package main

import (
    "fmt"
    "math"
)

func main() {
    fmt.Println("Hello, Go!")
}
```

##### 2. 数据类型

###### 基本类型
- **整数**: `int`, `int8`, `int16`, `int32`, `int64`
- **无符号整数**: `uint`, `uint8`, `uint16`, `uint32`, `uint64`, `uintptr`
- **浮点数**: `float32`, `float64`
- **复数**: `complex64`, `complex128`
- **布尔**: `bool`
- **字符串**: `string`
- **字节**: `byte` (uint8的别名)
- **符文**: `rune` (int32的别名，表示Unicode码点)

###### 复合类型
- **数组**: `[5]int`
- **切片**: `[]int`
- **映射**: `map[string]int`
- **结构体**: `struct`
- **指针**: `*int`
- **函数**: `func(int, int) int`
- **接口**: `interface`
- **通道**: `chan int`

##### 3. 控制流

###### 条件语句
```go
if x > 0 {
    fmt.Println("Positive")
} else if x == 0 {
    fmt.Println("Zero")
} else {
    fmt.Println("Negative")
}

// 带初始化语句的if
if value, err := compute(); err != nil {
    fmt.Println("Error:", err)
} else {
    fmt.Println("Result:", value)
}
```

###### 循环语句
```go
// for循环（类似while）
i := 0
for i < 10 {
    fmt.Println(i)
    i++
}

// 传统for循环
for i := 0; i < 10; i++ {
    fmt.Println(i)
}

// 无限循环
for {
    // 需要break退出
    break
}

// range循环
arr := []int{10, 20, 30}
for index, value := range arr {
    fmt.Printf("arr[%d] = %d\n", index, value)
}
```

###### switch语句
```go
switch day := 3; day {
case 1:
    fmt.Println("Monday")
case 2:
    fmt.Println("Tuesday")
case 3:
    fmt.Println("Wednesday")
default:
    fmt.Println("Other day")
}

// 无表达式的switch（替代if-else链）
switch {
case x < 0:
    fmt.Println("Negative")
case x == 0:
    fmt.Println("Zero")
default:
    fmt.Println("Positive")
}
```

##### 4. 函数

###### 函数定义
```go
func add(x int, y int) int {
    return x + y
}

// 参数类型简写
func multiply(x, y int) int {
    return x * y
}

// 多返回值
func swap(x, y int) (int, int) {
    return y, x
}

// 命名返回值
func divide(dividend, divisor float64) (result float64, err error) {
    if divisor == 0 {
        err = fmt.Errorf("division by zero")
        return
    }
    result = dividend / divisor
    return
}
```

###### 函数作为值
```go
// 函数类型
type Operation func(int, int) int

func apply(op Operation, a, b int) int {
    return op(a, b)
}

// 匿名函数
add := func(a, b int) int {
    return a + b
}

result := apply(add, 10, 5)
```

###### 可变参数函数
```go
func sum(numbers ...int) int {
    total := 0
    for _, num := range numbers {
        total += num
    }
    return total
}

fmt.Println(sum(1, 2, 3, 4, 5))  // 15
```

##### 5. 结构体和方法

###### 结构体定义
```go
type Point struct {
    X, Y float64
}

type Circle struct {
    Center Point
    Radius float64
}
```

###### 方法定义
```go
// 值接收者
func (p Point) DistanceFromOrigin() float64 {
    return math.Sqrt(p.X*p.X + p.Y*p.Y)
}

// 指针接收者（可以修改结构体）
func (p *Point) Scale(factor float64) {
    p.X *= factor
    p.Y *= factor
}

// 使用
p := Point{3, 4}
fmt.Println(p.DistanceFromOrigin())  // 5
p.Scale(2)
fmt.Println(p)  // {6, 8}
```

##### 6. 接口

###### 接口定义
```go
type Shape interface {
    Area() float64
    Perimeter() float64
}

type Rect struct {
    Width, Height float64
}

func (r Rect) Area() float64 {
    return r.Width * r.Height
}

func (r Rect) Perimeter() float64 {
    return 2 * (r.Width + r.Height)
}

type Circle struct {
    Radius float64
}

func (c Circle) Area() float64 {
    return math.Pi * c.Radius * c.Radius
}

func (c Circle) Perimeter() float64 {
    return 2 * math.Pi * c.Radius
}
```

###### 接口使用
```go
func printShapeInfo(s Shape) {
    fmt.Printf("Area: %.2f, Perimeter: %.2f\n", s.Area(), s.Perimeter())
}

func main() {
    shapes := []Shape{
        Rect{Width: 3, Height: 4},
        Circle{Radius: 5},
    }
    
    for _, shape := range shapes {
        printShapeInfo(shape)
    }
}
```

###### 空接口
```go
func printValue(v interface{}) {
    fmt.Printf("Value: %v, Type: %T\n", v, v)
}

printValue(42)        // int
printValue("hello")   // string
printValue(3.14)      // float64
```

##### 7. 并发

###### Goroutine
```go
func sayHello() {
    fmt.Println("Hello from goroutine")
}

func main() {
    go sayHello()  // 启动goroutine
    time.Sleep(100 * time.Millisecond)
}
```

###### 通道（Channel）
```go
func worker(id int, jobs <-chan int, results chan<- int) {
    for job := range jobs {
        fmt.Printf("Worker %d processing job %d\n", id, job)
        time.Sleep(time.Second)
        results <- job * 2
    }
}

func main() {
    jobs := make(chan int, 100)
    results := make(chan int, 100)
    
    // 启动3个worker
    for w := 1; w <= 3; w++ {
        go worker(w, jobs, results)
    }
    
    // 发送工作
    for j := 1; j <= 5; j++ {
        jobs <- j
    }
    close(jobs)
    
    // 收集结果
    for r := 1; r <= 5; r++ {
        <-results
    }
}
```

###### select语句
```go
func main() {
    ch1 := make(chan string)
    ch2 := make(chan string)
    
    go func() {
        time.Sleep(1 * time.Second)
        ch1 <- "from ch1"
    }()
    
    go func() {
        time.Sleep(2 * time.Second)
        ch2 <- "from ch2"
    }()
    
    for i := 0; i < 2; i++ {
        select {
        case msg1 := <-ch1:
            fmt.Println(msg1)
        case msg2 := <-ch2:
            fmt.Println(msg2)
        case <-time.After(3 * time.Second):
            fmt.Println("timeout")
        }
    }
}
```

##### 8. 错误处理

###### 错误类型
```go
type MyError struct {
    Code    int
    Message string
}

func (e *MyError) Error() string {
    return fmt.Sprintf("Error %d: %s", e.Code, e.Message)
}

func riskyOperation(x int) (int, error) {
    if x < 0 {
        return 0, &MyError{Code: 400, Message: "negative value"}
    }
    return x * 2, nil
}
```

###### 错误处理模式
```go
// 多返回值错误处理
result, err := riskyOperation(-5)
if err != nil {
    fmt.Println("Error:", err)
    return
}
fmt.Println("Result:", result)

// defer和错误恢复
func safeOperation() (err error) {
    defer func() {
        if r := recover(); r != nil {
            err = fmt.Errorf("recovered from panic: %v", r)
        }
    }()
    
    // 可能panic的代码
    panic("something went wrong")
    return nil
}
```

##### 9. 包和模块

###### 包定义
```go
// mathutil/mathutil.go
package mathutil

// 导出函数（大写字母开头）
func Add(a, b int) int {
    return a + b
}

func Subtract(a, b int) int {
    return a - b
}

// 私有函数（小写字母开头）
func internalHelper() {
    // ...
}
```

###### 包使用
```go
package main

import (
    "fmt"
    "github.com/user/mathutil"
)

func main() {
    sum := mathutil.Add(10, 5)
    diff := mathutil.Subtract(10, 5)
    fmt.Printf("Sum: %d, Difference: %d\n", sum, diff)
}
```

##### 10. 标准库

###### fmt包
```go
fmt.Print("Hello")           // 不换行输出
fmt.Println("World")         // 输出并换行
fmt.Printf("Value: %d\n", 42) // 格式化输出
fmt.Sprintf("Result: %d", 100) // 返回字符串
```

###### strings包
```go
strings.Contains("hello", "he")   // true
strings.ToUpper("hello")          // "HELLO"
strings.ToLower("HELLO")          // "hello"
strings.Split("a,b,c", ",")       // ["a", "b", "c"]
strings.Join([]string{"a", "b"}, "-") // "a-b"
```

###### strconv包
```go
strconv.Atoi("42")           // 42, nil
strconv.Itoa(42)             // "42"
strconv.ParseFloat("3.14", 64) // 3.14, nil
strconv.FormatFloat(3.14, 'f', 2, 64) // "3.14"
```

###### os包
```go
os.Getenv("PATH")            // 获取环境变量
os.Args                      // 命令行参数
os.Create("file.txt")        // 创建文件
os.Open("file.txt")          // 打开文件
os.Remove("file.txt")        // 删除文件
```

##### 11. VML 代码生成约定

###### Goroutine实现
Go的goroutine在VML中通过轻量级线程实现：
```
Goroutine结构:
偏移0: 状态（运行、就绪、阻塞）
偏移4: 栈指针
偏移8: 程序计数器
偏移12: 局部变量区
```

###### 通道实现
```vml
; 通道结构
LABEL make_chan
    ; 分配通道缓冲区
    SYSCALL #150, size  ; 分配通道内存
    
    ; 初始化通道
    MOVE [channel+0], #0    ; 发送者计数
    MOVE [channel+4], #0    ; 接收者计数
    MOVE [channel+8], size  ; 缓冲区大小
    MOVE [channel+12], #0   ; 发送索引
    MOVE [channel+16], #0   ; 接收索引
    
    ; 返回通道指针
    MOVE R0, channel
    RET
```

###### 接口实现
Go接口在VML中使用虚表实现：
```
接口结构:
偏移0: 类型指针
偏移4: 值指针
偏移8: 方法表指针
```

##### 12. 示例程序

###### 阶乘计算
```go
package main

import "fmt"

func factorial(n int) int {
    if n <= 1 {
        return 1
    }
    return n * factorial(n-1)
}

func main() {
    fmt.Println("5! =", factorial(5))  // 120
}
```

###### 并发Web服务器
```go
package main

import (
    "fmt"
    "net/http"
    "time"
)

func handler(w http.ResponseWriter, r *http.Request) {
    fmt.Fprintf(w, "Hello, %s!", r.URL.Path[1:])
}

func main() {
    http.HandleFunc("/", handler)
    
    go func() {
        time.Sleep(2 * time.Second)
        fmt.Println("Server shutting down...")
    }()
    
    fmt.Println("Server starting on :8080")
    http.ListenAndServe(":8080", nil)
}
```

###### 简单缓存系统
```go
package main

import (
    "fmt"
    "sync"
    "time"
)

type Cache struct {
    mu    sync.RWMutex
    items map[string]cacheItem
}

type cacheItem struct {
    value      interface{}
    expiration time.Time
}

func NewCache() *Cache {
    return &Cache{
        items: make(map[string]cacheItem),
    }
}

func (c *Cache) Set(key string, value interface{}, ttl time.Duration) {
    c.mu.Lock()
    defer c.mu.Unlock()
    
    c.items[key] = cacheItem{
        value:      value,
        expiration: time.Now().Add(ttl),
    }
}

func (c *Cache) Get(key string) (interface{}, bool) {
    c.mu.RLock()
    defer c.mu.RUnlock()
    
    item, found := c.items[key]
    if !found || time.Now().After(item.expiration) {
        return nil, false
    }
    return item.value, true
}

func main() {
    cache := NewCache()
    cache.Set("name", "Alice", 5*time.Second)
    
    if value, found := cache.Get("name"); found {
        fmt.Println("Found:", value)
    }
}
```

##### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (float32) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (float64) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (int64/uint64) 处理模式 |

###### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `float32` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `float32` 声明时报告编译错误。

###### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `float64` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `float64` 声明时报告编译错误。

###### 64位整数 (int64)

本语言中的 64 位整数类型 `int64` / `uint64` 按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数类型，遇到 `int64` / `uint64` 声明时报告编译错误。

###### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

##### 13. 编译限制

###### 当前实现状态
- **词法分析器**: 完整实现，支持 Go 全部关键字和符号
- **语法分析器**: 完整实现，支持 Go 语句、表达式、函数和类型系统
- **代码生成器**: 完整实现，生成 VML 指令
- **标准库**: 存在于 `Lib/go/` 目录

###### 已实现功能
- ✅ 变量声明 (var/:=)、常量 (const)
- ✅ 控制流: if/else/for/switch/range
- ✅ 函数: 定义/多返回值/命名返回值/匿名函数
- ✅ 结构体和方法 (值/指针接收者)
- ✅ 基本类型: int/float/bool/string/array/slice/map
- ✅ 接口 (含空接口 interface{})
- ⚠️ 包管理 — 基本支持
- ⚠️ 标准库: fmt.Println/Printf 等基本函数

###### 核心功能待实现（OS / MCU 均缺）
1. Goroutine 和通道 (chan) 的并发支持 — 需 VML 运行时新 SYSCALL
2. 垃圾回收机制
3. select 语句
4. defer/recover/panic
5. 完整标准库: strings, strconv, os, time, net/http

###### 技术挑战
1. **并发模型**: Goroutine和通道的轻量级实现
2. **垃圾回收**: 自动内存管理
3. **接口**: 动态类型和虚表
4. **反射**: 运行时类型信息

##### 14. 与VML运行时集成

Go 程序通过系统调用与 VML 运行时交互：

- **SYSCALL 4**: 输出字符（用于fmt.Print）
- **SYSCALL 6**: 输出整数
- **SYSCALL 150**: 创建通道
- **SYSCALL 151**: 发送到通道
- **SYSCALL 152**: 从通道接收
- **SYSCALL 153**: 启动goroutine
- **SYSCALL 154**: 垃圾回收
- **SYSCALL 155**: 反射操作

Go 的简洁语法和强大并发支持使其适合网络服务和分布式系统。通过 VML 编译器，Go 程序可以在多种平台上运行。
---

#### 🆕 字符串类型 (v1.65.19)

该语言编译器默认使用 `.wstring` (UTF-16LE) 作为内部字符串存储。

| 模式 | 默认编码 | VML 伪指令 | SYSCALL 输出 |
|:-----|:--------|:----------|:------------|
| MCU (默认) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**预定义宏**: `VML_WSTRING` — OS 模式下自动定义，MCU 模式未定义
**输出函数**: OS 模式自动使用 `shared_print_wstr` (UTF-16LE→UTF-8 自动转换)

```c
// 用户代码可通过宏判断编码
#ifdef VML_WSTRING
  // 默认字符串为 wstring (UTF-16LE)
#else
  // 默认字符串为 UTF-8
#endif
```

## 编译器 README

### Go 编译器

**路径**: `VMLPrepares/GoCompiler/`
**完成度**: ~90% | 🟢 生产可用
**标准库**: `Lib/go/`

#### 功能
- ✅ 完整语法分析 + 代码生成
- ✅ 控制流 (if/while/for/switch)
- ✅ 函数/过程定义和调用
- ✅ 标准库支持
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 十六进制字面量
- ✅ 共享内置函数库 (builtins.vml)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- goroutine、chan、select

**保留**（由 BIOS 实现底层）:
- map(哈希表)、slice(堆)、interface动态分发
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART
- 裸指针（有限制）

##### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

##### RAM 级别
- `--ram k`：KB级别（2KB~64KB，如 8051/PIC/AVR）
- `--ram m`：MB级别（64KB~1MB，如 ARM Cortex-M，**默认**）
- `--ram g`：GB级别（如 x86/DDR 系统）
- `--stack-size <bytes>`：手动指定栈大小（默认自动根据 --ram 分配）

##### MCU 安全编码提示
- 有限栈空间（256-4096 字节典型），避免深度递归
- 禁止深度递归（>10层需评估栈）
- 禁止动态加载（import/dofile/eval）
- 浮点运算可能需软浮点库

#### 使用
```bash
dotnet run --project VMLPrepares/GoCompiler input.Go -o output.vml
```

#### 测试
`Test/Go/` — 21 测试文件
