# Lua

A light, quick little language: pleasant for small games, and it compiles fast.

## How to run it on your phone

```
vml run examples/lua/sysinfo.lua
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- Call `ui_*` directly
- Compiles fast, good for experimenting

## Examples

| File | What it shows |
|---|---|
| `life.lua` | Game of Life (double buffering plus eight-neighbour sum) |
| `sysinfo.lua` | Device information |

Also in this folder: `demo_std.lua`, `demo_tty.lua`, `demo_ui.lua`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/LuaCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Lua language compiler specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | Lua 5.1 subset (2006) |
| «bold»Release year«/» | 2006 |
| «bold»Completeness«/» | ~93% |
| «bold»MCU completeness«/» | ~90% |
| «bold»Tests«/» | 0 (the test directory is yet to be created) |
| «bold»Updates«/» | 2026-05-18: removed goto (a Lua 5.2 feature); corrected completeness and test counts |

#### Keywords

`and` `break` `do` `else` `elseif` `end` `false` `for` `function`
`if` `in` `local` `nil` `not` `or` `repeat` `return` `then` `true` `until` `while`

#### Overview

This compiler compiles the Lua language into VML (Virtual Machine Language) assembly. Lua is a lightweight, embeddable scripting language, widely used for game development, embedded systems and configuration scripts.

#### Language features

##### 1. Basic Lua syntax

###### Variable declaration
```lua
local x = 10          -- local variable
global_var = 20       -- global variable
y = 3.14              -- number
name = "Lua"          -- string
flag = true           -- boolean
```

###### Comments
```lua
-- single-line comment
--[[
  multi-line comment
  can span several lines
]]
```

##### 2. Data types

###### Literals
- «bold»Integer«/»: `42` (decimal)
- «bold»Hexadecimal«/»: `0xFF`, `0x3FF44000` (prefix `0x`/`0X`)
- «bold»Float«/»: `3.14`, `1.5e-2`
- «bold»String«/»: `"hello"`, `'world'`
- «bold»Boolean«/»: `true`, `false`
- «bold»Nil«/»: `nil`

###### Basic types
- «bold»nil«/»: the empty value
- «bold»boolean«/»: boolean value (true/false)
- «bold»number«/»: number (integer and float)
- «bold»string«/»: string
- «bold»table«/»: table (array and dictionary)
- «bold»function«/»: function
- «bold»userdata«/»: user data
- «bold»thread«/»: coroutine

###### Type checking
```lua
type("hello")    -- "string"
type(42)         -- "number"
type(true)       -- "boolean"
type(nil)        -- "nil"
```

##### 3. Tables

###### Array usage
```lua
local arr = {10, 20, 30, 40}
print(arr[1])     -- 10 (Lua indexes start at 1)
arr[5] = 50       -- add an element
```

###### Dictionary usage
```lua
local person = {
    name = "Alice",
    age = 25,
    city = "Beijing"
}
print(person.name)   -- "Alice"
person.job = "Engineer"
```

###### Mixed usage
```lua
local mixed = {
    "apple", "banana", "cherry",
    price = 100,
    count = 3
}
```

##### 4. Control structures

###### Conditionals
```lua
if score >= 90 then
    grade = "A"
elseif score >= 80 then
    grade = "B"
elseif score >= 70 then
    grade = "C"
else
    grade = "F"
end
```

###### Loops
```lua
-- while loop
local i = 1
while i <= 10 do
    print(i)
    i = i + 1
end

-- for loop (numeric)
for i = 1, 10 do
    print(i)
end

for i = 10, 1, -1 do
    print(i)
end

-- for loop (generic)
for k, v in pairs(table) do
    print(k, v)
end

-- repeat loop
local i = 1
repeat
    print(i)
    i = i + 1
until i > 10
```

##### 5. Functions

###### Function definition
```lua
function add(a, b)
    return a + b
end

-- anonymous function
local multiply = function(a, b)
    return a * b
end

-- multiple return values
function minmax(a, b)
    if a < b then
        return a, b
    else
        return b, a
    end
end
```

###### Function calls
```lua
local result = add(10, 20)
local x, y = minmax(5, 3)
print(multiply(4, 5))
```

###### Variadic parameters
```lua
function sum(...)
    local total = 0
    local args = {...}
    for i = 1, #args do
        total = total + args[i]
    end
    return total
end

print(sum(1, 2, 3, 4, 5))  -- 15
```

##### 6. Operators

###### Arithmetic operators
- `+` - addition
- `-` - subtraction
- `*` - multiplication
- `/` - division
- `%` - modulo
- `^` - exponentiation
- `-` - negation (unary)

###### Relational operators
- `==` - equal to
- `~=` - not equal to
- `<` - less than
- `>` - greater than
- `<=` - less than or equal
- `>=` - greater than or equal

###### Logical operators
- `and` - logical and
- `or` - logical or
- `not` - logical not

###### Other operators
- `..` - string concatenation
- `#` - length operator (string or table)

##### 7. Standard library functions

###### Basic functions
```lua
print("Hello")           -- output
type(x)                  -- type check
tostring(123)            -- convert to string
tonumber("456")          -- convert to number
```

###### Math library
```lua
math.abs(-10)            -- absolute value
math.floor(3.7)          -- round down
math.ceil(3.2)           -- round up
math.sqrt(16)            -- square root
math.sin(math.pi/2)      -- sine
math.random()            -- random number
math.max(1, 5, 3)        -- maximum
math.min(1, 5, 3)        -- minimum
```

###### String library
```lua
string.len("hello")      -- length
string.sub("hello", 2, 4) -- substring
string.upper("hello")    -- uppercase
string.lower("HELLO")    -- lowercase
string.reverse("abc")    -- reverse
string.rep("x", 5)       -- repeat
string.format("%d", 42)  -- format
```

###### Table library
```lua
table.insert(t, 1, "x")  -- insert
table.remove(t, 1)       -- remove
table.concat(t, ", ")    -- concatenate
table.sort(t)            -- sort
```

##### 8. Coroutines

```lua
-- create a coroutine
co = coroutine.create(function()
    for i = 1, 3 do
        print("coroutine", i)
        coroutine.yield()
    end
end)

-- run the coroutine
coroutine.resume(co)
coroutine.resume(co)
coroutine.resume(co)
```

##### 9. Error handling

```lua
-- protected call with pcall
local success, result = pcall(function()
    error("something went wrong")
end)

if not success then
    print("Error:", result)
end

-- xpcall with an error handler function
xpcall(function()
    error("test")
end, function(err)
    print("Error handler:", err)
end)
```

##### 10. Module system

###### Module definition
```lua
-- mymodule.lua
local M = {}

function M.add(a, b)
    return a + b
end

function M.subtract(a, b)
    return a - b
end

return M
```

###### Using a module
```lua
local mymodule = require("mymodule")
print(mymodule.add(10, 5))
print(mymodule.subtract(10, 5))
```

##### 11. VML code generation conventions

###### Table implementation
Lua tables are implemented with a VML memory structure:
```
table structure:
offset 0: type tag (0x01 = table)
offset 4: array part size
offset 8: hash part size
offset 12: array data pointer
offset 16: hash data pointer
```

###### Function calling convention
```lua
function add(a, b) return a + b end

VML code:
LABEL add
    ; save the frame pointer
    PUSH R14
    MOVE R14, R13
    
    ; access the arguments: a at [R14+12], b at [R14+8]
    MOVE R0, [R14+12]   ; a
    MOVE R1, [R14+8]    ; b
    ADD R0, R0, R1      ; a + b
    
    ; the return value is in R0
    POP R14
    RET
```

###### Closure implementation
```lua
function makeCounter()
    local count = 0
    return function()
        count = count + 1
        return count
    end
end

The VML implementation uses a closure structure to store the upvalue (the count variable).
```

##### 12. Example programs

###### Factorial
```lua
function factorial(n)
    if n <= 1 then
        return 1
    else
        return n * factorial(n - 1)
    end
end

print("5! =", factorial(5))  -- 120
```

###### Fibonacci sequence
```lua
function fibonacci(n)
    if n <= 2 then
        return 1
    else
        return fibonacci(n-1) + fibonacci(n-2)
    end
end

for i = 1, 10 do
    print(i, fibonacci(i))
end
```

###### Simple game logic
```lua
-- game character definitions
local player = {
    name = "Hero",
    health = 100,
    attack = 20,
    defense = 10
}

local enemy = {
    name = "Monster",
    health = 50,
    attack = 15,
    defense = 5
}

-- combat function
function attack(attacker, defender)
    local damage = attacker.attack - defender.defense
    if damage < 0 then damage = 1 end
    defender.health = defender.health - damage
    print(attacker.name .. " attacks " .. defender.name .. " for " .. damage .. " damage")
    return defender.health <= 0
end

-- combat loop
while true do
    if attack(player, enemy) then
        print("Player wins!")
        break
    end
    
    if attack(enemy, player) then
        print("Enemy wins!")
        break
    end
end
```

##### Floating-point and 64-bit compilation modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compilation flags:

| Flag | Possible values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit float handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit float (number) handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer handling mode |

###### 32-bit float (float32)

Every number in Lua is of the `number` type (64-bit float by default). 32-bit float arithmetic is used for internal intermediate results:

- «bold»`hard` mode (default)«/»: uses the VML native float instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly through the sixteen float registers F0-F15.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating float arithmetic through the `__vml_float_add/sub/mul/div/neg/abs/cmp` functions and so on.
- «bold»`none` mode«/»: floating-point arithmetic is disabled.

###### 64-bit float (double)

Lua's `number` type is natively a 64-bit float (configurable to 32-bit), which VML handles as follows when compiling:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through the `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` functions and so on. Compatible with every platform (MCU included).
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating through the eight double-precision registers D0-D7.
- «bold»`none` mode«/»: floating-point arithmetic is disabled.

###### 64-bit integer (int64)

The VML compilation layer supports a 64-bit integer extension for large-integer arithmetic:

- «bold»`soft` mode (default)«/»: uses the dual-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through the `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` functions and so on.
- «bold»`hard` mode«/»: reserved; a future VML version will support native 64-bit integer instructions.
- «bold»`none` mode«/»: degrades to 32-bit integers.

###### Software emulation libraries

The software emulation libraries above all live in the `Lib/shared/` directory, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit float emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit float emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer dual-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

##### 13. Compilation limitations

###### Current implementation status
- «bold»Lexer«/»: fully implemented, supports Lua 5.1 keywords and symbols
- «bold»Parser«/»: fully implemented, supports Lua statements, expressions and functions
- «bold»Code generator«/»: basically implemented, generates VML instructions
- «bold»Standard library«/»: the `Lib/lua/` directory; basic functions are available

###### Implemented
- ✅ Variable declaration (local/global)
- ✅ Control flow: if/elseif/else/while/for/repeat-until
- ✅ Functions: definition / multiple return values / variadic / anonymous functions
- ✅ Table: basic array and dictionary operations
- ✅ Standard library: print/type/tostring/tonumber

###### Core features yet to be implemented
1. Closures and upvalue capture
2. Coroutines — skipped in MCU mode
3. Metatables and metamethods
4. Garbage collection (a simplified version)
5. Complete standard library: every math/string/table function

###### Technical challenges
1. «bold»Dynamic typing«/»: Lua is a dynamically typed language, while VML is a statically typed register machine
2. «bold»Table implementation«/»: an efficient mixed array and hash-table structure is needed
3. «bold»Closures«/»: functions can capture outer variables (upvalues)
4. «bold»GC«/»: garbage collection (a simplified reference-counting version)

##### 14. Integration with the VML runtime

Lua programs interact with the VML runtime through system calls:

- «bold»SYSCALL 4«/»: output a character (used by print)
- «bold»SYSCALL 5«/»: input a character
- «bold»SYSCALL 6«/»: output an integer
- «bold»SYSCALL 7«/»: input an integer
- «bold»SYSCALL 130«/»: allocate memory (used by tables)
- «bold»SYSCALL 131«/»: free memory
- «bold»SYSCALL 132«/»: garbage collection

Lua's simplicity and embeddability make it an ideal choice for game scripts, configuration files and embedded systems. Through the VML compiler, Lua programs can run on many hardware platforms.
---

#### 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

«bold»MCU mode«/» (default): strings are output as UTF-8 (`.string`)
«bold»OS mode«/»: the encoding can be determined through the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h), which each language compiler can use as needed.

## Compiler README

### Lua 5.1 compiler

«bold»Path«/»: `VMLPrepares/LuaCompiler/`
«bold»Completeness«/»: ~93% | 🟢 production ready
«bold»Standard library«/»: `Lib/lua/`

#### Features
- ✅ Parsing + code generation (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/elseif/else/while/for/repeat-until)
- ✅ Functions: definition / multiple return values / variadic / anonymous functions
- ✅ Table: basic array and dictionary operations
- ⚠️ Closures/upvalues — partially supported
- ⚠️ Standard library: basic print/type/math functions
- ✅ POKE/PEEK memory access (MMIO)
- ✅ Shared built-in function library (builtins.vml)


#### Compilation modes

##### MCU mode (default `--mode mcu`)
MCU mode is tuned for microcontrollers and bare-metal environments (Arduino/STM32/8051 and so on), and automatically skips features that are incompatible with an operating system.

«bold»Skipped«/» (no code is generated for this syntax):
- coroutine, dofile/require

«bold»Kept«/» (implemented underneath by the BIOS):
- table, metatable
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts mapped to the UART

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (such as embedded Linux or an RTOS), and will support every language feature (file system, threads, asynchronous I/O, exceptions, reflection and so on).

##### RAM levels
- `--ram k`: kilobyte level (2KB~64KB, such as 8051/PIC/AVR)
- `--ram m`: megabyte level (64KB~1MB, such as ARM Cortex-M, «bold»default«/»)
- `--ram g`: gigabyte level (such as x86/DDR systems)
- `--stack-size <bytes>`: specify the stack size by hand (by default it is allocated automatically from `--ram`)

##### MCU-safe coding tips
- Limited stack space (256-4096 bytes is typical), so avoid deep recursion
- Deep recursion is not allowed (beyond 10 levels the stack must be evaluated)
- Dynamic loading (dofile/eval) is not allowed
- Floating-point arithmetic may require the soft-float library

#### Usage
```bash
dotnet run --project VMLTool -- input.lua -o output.vml
```

#### Tests
`Test/Lu/` — 0 test files (the test directory is yet to be created)
