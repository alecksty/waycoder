# Go

Close to desktop Go; the snake example is a complete game.

## How to run it on your phone

```
vml run examples/go/sysinfo.go
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- The entry point is `package main` plus `func main()`
- Declare variables with `:=` as usual
- Arrays and loops work normally (this path has been fixed up fairly thoroughly)

## Examples

| File | What it shows |
|---|---|
| `snake.go` | Snake |
| `sysinfo.go` | Device information |

Also in this folder: `demo_std.go`, `demo_tty.go`, `demo_ui.go`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/GoCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Go Language Compiler Specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | Go 1.0 subset (2012) |
| «bold»Release year«/» | 2012 |
| «bold»Completeness«/» | ~90% |
| «bold»MCU completeness«/» | ~88% |
| «bold»Tests«/» | 21 passing |
| «bold»Updates«/» | 2026-05-18: corrected the test count (21) and the completeness figure; clarified what is implemented and what is still to do |

#### Keywords

`break` `case` `chan` `const` `continue` `default` `defer` `else` `fallthrough`
`for` `func` `go` `goto` `if` `import` `interface` `map` `package` `range`
`return` `select` `struct` `switch` `type` `var`

#### Overview

This compiler implements a subset of Go and compiles it to VML (Virtual Machine Language) assembly. Go is a statically typed, compiled language that emphasizes simplicity, concurrency and high performance.

#### Language features

##### 1. Basic Go syntax

###### Variable declaration
```go
var x int = 10          // explicit type declaration
var y = 20              // type inference
z := 30                 // short variable declaration
const MAX_SIZE = 100    // constant
```

###### Package declaration
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

##### 2. Data types

###### Basic types
- «bold»Integers«/»: `int`, `int8`, `int16`, `int32`, `int64`
- «bold»Unsigned integers«/»: `uint`, `uint8`, `uint16`, `uint32`, `uint64`, `uintptr`
- «bold»Floating point«/»: `float32`, `float64`
- «bold»Complex numbers«/»: `complex64`, `complex128`
- «bold»Boolean«/»: `bool`
- «bold»String«/»: `string`
- «bold»Byte«/»: `byte` (an alias for uint8)
- «bold»Rune«/»: `rune` (an alias for int32, a Unicode code point)

###### Composite types
- «bold»Array«/»: `[5]int`
- «bold»Slice«/»: `[]int`
- «bold»Map«/»: `map[string]int`
- «bold»Struct«/»: `struct`
- «bold»Pointer«/»: `*int`
- «bold»Function«/»: `func(int, int) int`
- «bold»Interface«/»: `interface`
- «bold»Channel«/»: `chan int`

##### 3. Control flow

###### Conditional statements
```go
if x > 0 {
    fmt.Println("Positive")
} else if x == 0 {
    fmt.Println("Zero")
} else {
    fmt.Println("Negative")
}

// if with an initializer statement
if value, err := compute(); err != nil {
    fmt.Println("Error:", err)
} else {
    fmt.Println("Result:", value)
}
```

###### Loops
```go
// for loop (works like while)
i := 0
for i < 10 {
    fmt.Println(i)
    i++
}

// traditional for loop
for i := 0; i < 10; i++ {
    fmt.Println(i)
}

// infinite loop
for {
    // need a break to leave
    break
}

// range loop
arr := []int{10, 20, 30}
for index, value := range arr {
    fmt.Printf("arr[%d] = %d\n", index, value)
}
```

###### switch statement
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

// switch with no expression (replaces an if-else chain)
switch {
case x < 0:
    fmt.Println("Negative")
case x == 0:
    fmt.Println("Zero")
default:
    fmt.Println("Positive")
}
```

##### 4. Functions

###### Function definition
```go
func add(x int, y int) int {
    return x + y
}

// shorthand parameter types
func multiply(x, y int) int {
    return x * y
}

// multiple return values
func swap(x, y int) (int, int) {
    return y, x
}

// named return values
func divide(dividend, divisor float64) (result float64, err error) {
    if divisor == 0 {
        err = fmt.Errorf("division by zero")
        return
    }
    result = dividend / divisor
    return
}
```

###### Functions as values
```go
// function type
type Operation func(int, int) int

func apply(op Operation, a, b int) int {
    return op(a, b)
}

// anonymous function
add := func(a, b int) int {
    return a + b
}

result := apply(add, 10, 5)
```

###### Variadic functions
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

##### 5. Structs and methods

###### Struct definition
```go
type Point struct {
    X, Y float64
}

type Circle struct {
    Center Point
    Radius float64
}
```

###### Method definition
```go
// value receiver
func (p Point) DistanceFromOrigin() float64 {
    return math.Sqrt(p.X*p.X + p.Y*p.Y)
}

// pointer receiver (can modify the struct)
func (p *Point) Scale(factor float64) {
    p.X *= factor
    p.Y *= factor
}

// usage
p := Point{3, 4}
fmt.Println(p.DistanceFromOrigin())  // 5
p.Scale(2)
fmt.Println(p)  // {6, 8}
```

##### 6. Interfaces

###### Interface definition
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

###### Using interfaces
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

###### The empty interface
```go
func printValue(v interface{}) {
    fmt.Printf("Value: %v, Type: %T\n", v, v)
}

printValue(42)        // int
printValue("hello")   // string
printValue(3.14)      // float64
```

##### 7. Concurrency

###### Goroutine
```go
func sayHello() {
    fmt.Println("Hello from goroutine")
}

func main() {
    go sayHello()  // start a goroutine
    time.Sleep(100 * time.Millisecond)
}
```

###### Channels
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
    
    // start 3 workers
    for w := 1; w <= 3; w++ {
        go worker(w, jobs, results)
    }
    
    // send work
    for j := 1; j <= 5; j++ {
        jobs <- j
    }
    close(jobs)
    
    // collect results
    for r := 1; r <= 5; r++ {
        <-results
    }
}
```

###### select statement
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

##### 8. Error handling

###### Error types
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

###### Error-handling patterns
```go
// handling errors through multiple return values
result, err := riskyOperation(-5)
if err != nil {
    fmt.Println("Error:", err)
    return
}
fmt.Println("Result:", result)

// defer and error recovery
func safeOperation() (err error) {
    defer func() {
        if r := recover(); r != nil {
            err = fmt.Errorf("recovered from panic: %v", r)
        }
    }()
    
    // code that may panic
    panic("something went wrong")
    return nil
}
```

##### 9. Packages and modules

###### Package definition
```go
// mathutil/mathutil.go
package mathutil

// exported function (starts with a capital letter)
func Add(a, b int) int {
    return a + b
}

func Subtract(a, b int) int {
    return a - b
}

// private function (starts with a lower-case letter)
func internalHelper() {
    // ...
}
```

###### Using a package
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

##### 10. Standard library

###### The fmt package
```go
fmt.Print("Hello")           // print without a newline
fmt.Println("World")         // print and add a newline
fmt.Printf("Value: %d\n", 42) // formatted output
fmt.Sprintf("Result: %d", 100) // return a string
```

###### The strings package
```go
strings.Contains("hello", "he")   // true
strings.ToUpper("hello")          // "HELLO"
strings.ToLower("HELLO")          // "hello"
strings.Split("a,b,c", ",")       // ["a", "b", "c"]
strings.Join([]string{"a", "b"}, "-") // "a-b"
```

###### The strconv package
```go
strconv.Atoi("42")           // 42, nil
strconv.Itoa(42)             // "42"
strconv.ParseFloat("3.14", 64) // 3.14, nil
strconv.FormatFloat(3.14, 'f', 2, 64) // "3.14"
```

###### The os package
```go
os.Getenv("PATH")            // read an environment variable
os.Args                      // command-line arguments
os.Create("file.txt")        // create a file
os.Open("file.txt")          // open a file
os.Remove("file.txt")        // delete a file
```

##### 11. VML code-generation conventions

###### Goroutine implementation
A Go goroutine is implemented in VML as a lightweight thread:
```
Goroutine structure:
offset 0:  state (running, ready, blocked)
offset 4:  stack pointer
offset 8:  program counter
offset 12: local variable area
```

###### Channel implementation
```vml
; channel structure
LABEL make_chan
    ; allocate the channel buffer
    SYSCALL #150, size  ; allocate channel memory
    
    ; initialize the channel
    MOVE [channel+0], #0    ; sender count
    MOVE [channel+4], #0    ; receiver count
    MOVE [channel+8], size  ; buffer size
    MOVE [channel+12], #0   ; send index
    MOVE [channel+16], #0   ; receive index
    
    ; return the channel pointer
    MOVE R0, channel
    RET
```

###### Interface implementation
A Go interface is implemented with a virtual table in VML:
```
Interface structure:
offset 0: type pointer
offset 4: value pointer
offset 8: method table pointer
```

##### 12. Example programs

###### Computing a factorial
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

###### A concurrent web server
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

###### A simple cache
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

##### Floating-point and 64-bit compile modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compile parameters:

| Parameter | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit floating point (float32) handling |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit floating point (float64) handling |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integers (int64/uint64) handling |

###### 32-bit floating point (float32)

The 32-bit single-precision type `float32` compiles as follows:

- «bold»`hard` mode (default)«/»: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly on the sixteen floating-point registers F0-F15. Best performance; suited to targets with floating-point hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point operations through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms with no floating-point hardware.
- «bold»`none` mode«/»: disables all 32-bit floating-point types; a `float32` declaration is reported as a compile error.

###### 64-bit floating point (double)

The 64-bit double-precision type `float64` compiles as follows:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision emulation library `softdouble.c`, through `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` and similar. Works on every platform, MCU included.
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating on the eight double-precision registers D0-D7. Requires the target platform to support 64-bit operations.
- «bold»`none` mode«/»: disables all 64-bit floating-point types; a `float64` declaration is reported as a compile error.

###### 64-bit integers (int64)

The 64-bit integer types `int64` / `uint64` compile as follows:

- «bold»`soft` mode (default)«/»: uses the two-register software emulation library `softint64.c`, through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- «bold»`hard` mode«/»: reserved; a future VML version will support native 64-bit integer instructions.
- «bold»`none` mode«/»: disables the 64-bit integer types; an `int64` / `uint64` declaration is reported as a compile error.

###### Software emulation libraries

These emulation libraries all live in `Lib/shared/`, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer two-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

##### 13. Compilation limits

###### Current implementation status
- «bold»Lexer«/»: fully implemented, covering every Go keyword and symbol
- «bold»Parser«/»: fully implemented, covering Go statements, expressions, functions and the type system
- «bold»Code generator«/»: fully implemented, emitting VML instructions
- «bold»Standard library«/»: present in the `Lib/go/` directory

###### Features implemented
- ✅ Variable declarations (var/:=), constants (const)
- ✅ Control flow: if/else/for/switch/range
- ✅ Functions: definitions/multiple return values/named return values/anonymous functions
- ✅ Structs and methods (value/pointer receivers)
- ✅ Basic types: int/float/bool/string/array/slice/map
- ✅ Interfaces (including the empty interface interface{})
- ⚠️ Package management — basic support
- ⚠️ Standard library: basic functions such as fmt.Println/Printf

###### Core features still to implement (missing in both OS and MCU modes)
1. Concurrency through goroutines and channels (chan) — needs new VML runtime SYSCALLs
2. Garbage collection
3. The select statement
4. defer/recover/panic
5. The full standard library: strings, strconv, os, time, net/http

###### Technical challenges
1. «bold»Concurrency model«/»: a lightweight implementation of goroutines and channels
2. «bold»Garbage collection«/»: automatic memory management
3. «bold»Interfaces«/»: dynamic types and virtual tables
4. «bold»Reflection«/»: runtime type information

##### 14. Integration with the VML runtime

Go programs talk to the VML runtime through system calls:

- «bold»SYSCALL 4«/»: write a character (used by fmt.Print)
- «bold»SYSCALL 6«/»: write an integer
- «bold»SYSCALL 150«/»: create a channel
- «bold»SYSCALL 151«/»: send to a channel
- «bold»SYSCALL 152«/»: receive from a channel
- «bold»SYSCALL 153«/»: start a goroutine
- «bold»SYSCALL 154«/»: garbage collection
- «bold»SYSCALL 155«/»: reflection operations

Go's concise syntax and strong concurrency support make it a good fit for network services and distributed systems. Through the VML compiler, Go programs can run on many platforms.
---

#### 🆕 String types (v1.65.19)

This language's compiler uses `.wstring` (UTF-16LE) as its internal string storage by default.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

«bold»Predefined macro«/»: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
«bold»Output function«/»: OS mode automatically uses `shared_print_wstr` (converts UTF-16LE→UTF-8 for you)

```c
// user code can test the macro to tell which encoding is in use
#ifdef VML_WSTRING
  // strings are wstring (UTF-16LE) by default
#else
  // strings are UTF-8 by default
#endif
```

## Compiler README

### Go Compiler

«bold»Path«/»: `VMLPrepares/GoCompiler/`
«bold»Completeness«/»: ~90% | 🟢 production ready
«bold»Standard library«/»: `Lib/go/`

#### Features
- ✅ Full syntax analysis + code generation
- ✅ Control flow (if/while/for/switch)
- ✅ Function/procedure definition and calls
- ✅ Standard library support
- ✅ POKE/PEEK memory operations (MMIO)
- ✅ Hexadecimal literals
- ✅ Shared built-in function library (builtins.vml)


#### Compile modes

##### MCU mode (default: `--mode mcu`)
MCU mode is tuned for single-chip and bare-metal environments (Arduino/STM32/8051 and so on) and automatically skips features that are incompatible with an operating system.

«bold»Skipped«/» (these constructs generate no code):
- goroutine, chan, select

«bold»Kept«/» (implemented on top of the BIOS):
- map (hash table), slice (heap), interface dynamic dispatch
- POKE/PEEK memory-mapped I/O (MMIO)
- basic type operations, control flow, function calls
- printf/puts mapped to the UART
- raw pointers (with restrictions)

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (embedded Linux, an RTOS, and so on). It will support the full language feature set (file system, threads, async, exceptions, reflection).

##### RAM levels
- `--ram k`: kilobyte range (2KB~64KB, e.g. 8051/PIC/AVR)
- `--ram m`: megabyte range (64KB~1MB, e.g. ARM Cortex-M, «bold»default«/»)
- `--ram g`: gigabyte range (e.g. x86/DDR systems)
- `--stack-size <bytes>`: set the stack size by hand (by default it is allocated automatically from `--ram`)

##### MCU safe-coding tips
- Stack space is limited (256-4096 bytes typically), so avoid deep recursion
- No deep recursion (>10 levels means you should evaluate the stack)
- No dynamic loading (import/dofile/eval)
- Floating point may need a software floating-point library

#### Usage
```bash
dotnet run --project VMLPrepares/GoCompiler input.Go -o output.vml
```

#### Tests
`Test/Go/` — 21 test files
