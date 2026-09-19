# Lua

轻快的小语言，写小游戏很舒服，编译也快。

## 在手机上怎么跑

```
vml run examples/lua/life.lua
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 直接调用 `ui_*`
- 编译快，适合反复试

## 示例

| 文件 | 演示什么 |
|---|---|
| `life.lua` | 生命游戏（双缓冲 + 八邻域求和） |
| `sysinfo.lua` | 设备信息 |

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/LuaCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Lua 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Lua 5.1 子集 (2006) |
| **发布年份** | 2006 |
| **完成度** | ~93% |
| **MCU完成度** | ~90% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 移除 goto（Lua 5.2特性）；修正完成度和测试数 |

#### 关键字

`and` `break` `do` `else` `elseif` `end` `false` `for` `function`
`if` `in` `local` `nil` `not` `or` `repeat` `return` `then` `true` `until` `while`

#### 概述

本编译器实现 Lua 语言到 VML (Virtual Machine Language) 汇编的编译。Lua 是一种轻量级、可嵌入的脚本语言，广泛用于游戏开发、嵌入式系统和配置脚本。

#### 语言特性

##### 1. Lua 基本语法

###### 变量声明
```lua
local x = 10          -- 局部变量
global_var = 20       -- 全局变量
y = 3.14              -- 数字
name = "Lua"          -- 字符串
flag = true           -- 布尔值
```

###### 注释
```lua
-- 单行注释
--[[
  多行注释
  可以跨越多行
]]
```

##### 2. 数据类型

###### 字面量
- **整数**: `42`（十进制）
- **十六进制**: `0xFF`, `0x3FF44000`（`0x`/`0X` 前缀）
- **浮点数**: `3.14`, `1.5e-2`
- **字符串**: `"hello"`, `'world'`
- **布尔**: `true`, `false`
- **空**: `nil`

###### 基本类型
- **nil**: 空值
- **boolean**: 布尔值（true/false）
- **number**: 数字（整数和浮点数）
- **string**: 字符串
- **table**: 表（数组和字典）
- **function**: 函数
- **userdata**: 用户数据
- **thread**: 协程

###### 类型检查
```lua
type("hello")    -- "string"
type(42)         -- "number"
type(true)       -- "boolean"
type(nil)        -- "nil"
```

##### 3. 表（Table）

###### 数组用法
```lua
local arr = {10, 20, 30, 40}
print(arr[1])     -- 10（Lua索引从1开始）
arr[5] = 50       -- 添加元素
```

###### 字典用法
```lua
local person = {
    name = "Alice",
    age = 25,
    city = "Beijing"
}
print(person.name)   -- "Alice"
person.job = "Engineer"
```

###### 混合用法
```lua
local mixed = {
    "apple", "banana", "cherry",
    price = 100,
    count = 3
}
```

##### 4. 控制结构

###### 条件语句
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

###### 循环语句
```lua
-- while 循环
local i = 1
while i <= 10 do
    print(i)
    i = i + 1
end

-- for 循环（数值）
for i = 1, 10 do
    print(i)
end

for i = 10, 1, -1 do
    print(i)
end

-- for 循环（泛型）
for k, v in pairs(table) do
    print(k, v)
end

-- repeat 循环
local i = 1
repeat
    print(i)
    i = i + 1
until i > 10
```

##### 5. 函数

###### 函数定义
```lua
function add(a, b)
    return a + b
end

-- 匿名函数
local multiply = function(a, b)
    return a * b
end

-- 多返回值
function minmax(a, b)
    if a < b then
        return a, b
    else
        return b, a
    end
end
```

###### 函数调用
```lua
local result = add(10, 20)
local x, y = minmax(5, 3)
print(multiply(4, 5))
```

###### 可变参数
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

##### 6. 运算符

###### 算术运算符
- `+` - 加法
- `-` - 减法  
- `*` - 乘法
- `/` - 除法
- `%` - 取模
- `^` - 幂运算
- `-` - 负号（一元）

###### 关系运算符
- `==` - 等于
- `~=` - 不等于
- `<` - 小于
- `>` - 大于
- `<=` - 小于等于
- `>=` - 大于等于

###### 逻辑运算符
- `and` - 逻辑与
- `or` - 逻辑或
- `not` - 逻辑非

###### 其他运算符
- `..` - 字符串连接
- `#` - 长度运算符（字符串或表）

##### 7. 标准库函数

###### 基本函数
```lua
print("Hello")           -- 输出
type(x)                  -- 类型检查
tostring(123)            -- 转换为字符串
tonumber("456")          -- 转换为数字
```

###### 数学库
```lua
math.abs(-10)            -- 绝对值
math.floor(3.7)          -- 向下取整
math.ceil(3.2)           -- 向上取整
math.sqrt(16)            -- 平方根
math.sin(math.pi/2)      -- 正弦
math.random()            -- 随机数
math.max(1, 5, 3)        -- 最大值
math.min(1, 5, 3)        -- 最小值
```

###### 字符串库
```lua
string.len("hello")      -- 长度
string.sub("hello", 2, 4) -- 子串
string.upper("hello")    -- 大写
string.lower("HELLO")    -- 小写
string.reverse("abc")    -- 反转
string.rep("x", 5)       -- 重复
string.format("%d", 42)  -- 格式化
```

###### 表库
```lua
table.insert(t, 1, "x")  -- 插入
table.remove(t, 1)       -- 删除
table.concat(t, ", ")    -- 连接
table.sort(t)            -- 排序
```

##### 8. 协程（Coroutine）

```lua
-- 创建协程
co = coroutine.create(function()
    for i = 1, 3 do
        print("coroutine", i)
        coroutine.yield()
    end
end)

-- 执行协程
coroutine.resume(co)
coroutine.resume(co)
coroutine.resume(co)
```

##### 9. 错误处理

```lua
-- pcall 保护调用
local success, result = pcall(function()
    error("something went wrong")
end)

if not success then
    print("Error:", result)
end

-- xpcall 带错误处理函数
xpcall(function()
    error("test")
end, function(err)
    print("Error handler:", err)
end)
```

##### 10. 模块系统

###### 模块定义
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

###### 模块使用
```lua
local mymodule = require("mymodule")
print(mymodule.add(10, 5))
print(mymodule.subtract(10, 5))
```

##### 11. VML 代码生成约定

###### 表实现
Lua 表使用 VML 内存结构实现：
```
表结构:
偏移0: 类型标记 (0x01 = 表)
偏移4: 数组部分大小
偏移8: 哈希部分大小
偏移12: 数组数据指针
偏移16: 哈希数据指针
```

###### 函数调用约定
```lua
function add(a, b) return a + b end

VML代码:
LABEL add
    ; 保存帧指针
    PUSH R14
    MOVE R14, R13
    
    ; 访问参数: a在[R14+12], b在[R14+8]
    MOVE R0, [R14+12]   ; a
    MOVE R1, [R14+8]    ; b
    ADD R0, R0, R1      ; a + b
    
    ; 返回值在R0中
    POP R14
    RET
```

###### 闭包实现
```lua
function makeCounter()
    local count = 0
    return function()
        count = count + 1
        return count
    end
end

VML实现使用闭包结构存储upvalue（count变量）。
```

##### 12. 示例程序

###### 阶乘计算
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

###### 斐波那契数列
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

###### 简单游戏逻辑
```lua
-- 游戏角色定义
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

-- 战斗函数
function attack(attacker, defender)
    local damage = attacker.attack - defender.defense
    if damage < 0 then damage = 1 end
    defender.health = defender.health - damage
    print(attacker.name .. " attacks " .. defender.name .. " for " .. damage .. " damage")
    return defender.health <= 0
end

-- 战斗循环
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

##### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (number) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数处理模式 |

###### 32位浮点 (float32)

Lua 中所有数字为 `number` 类型（默认 64 位浮点）。32 位浮点运算用于内部中间结果处理：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。
- **`none` 模式**: 禁用浮点运算。

###### 64位浮点 (double)

Lua 的 `number` 类型原生为 64 位浮点（可配置为 32 位），VML 编译时按以下模式处理：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。
- **`none` 模式**: 禁用浮点运算。

###### 64位整数 (int64)

VML 编译层支持 64 位整数扩展，用于处理大整数运算：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 降级为 32 位整数。

###### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

##### 13. 编译限制

###### 当前实现状态
- **词法分析器**: 完整实现，支持 Lua 5.1 关键字和符号
- **语法分析器**: 完整实现，支持 Lua 语句、表达式、函数
- **代码生成器**: 基本实现，生成 VML 指令
- **标准库**: `Lib/lua/` 目录，基本函数可用

###### 已实现
- ✅ 变量声明 (local/global)
- ✅ 控制流: if/elseif/else/while/for/repeat-until
- ✅ 函数: 定义/多返回值/可变参数/匿名函数
- ✅ 表 (Table): 数组和字典基本操作
- ✅ 标准库: print/type/tostring/tonumber

###### 核心功能待实现
1. 闭包和 upvalue 捕获
2. 协程 (coroutine) — MCU 模式跳过
3. 元表 (metatable) 和元方法
4. 垃圾回收机制（简化版）
5. 完整标准库: math/string/table 全部函数

###### 技术挑战
1. **动态类型**: Lua 是动态类型语言，VML 是静态类型寄存器机器
2. **表实现**: 需要高效的数组和哈希表混合结构
3. **闭包**: 函数可以捕获外部变量 (upvalue)
4. **GC**: 垃圾回收机制（简化版引用计数）

##### 14. 与VML运行时集成

Lua 程序通过系统调用与 VML 运行时交互：

- **SYSCALL 4**: 输出字符（用于print）
- **SYSCALL 5**: 输入字符
- **SYSCALL 6**: 输出整数
- **SYSCALL 7**: 输入整数
- **SYSCALL 130**: 分配内存（用于表）
- **SYSCALL 131**: 释放内存
- **SYSCALL 132**: 垃圾回收

Lua 的简洁性和可嵌入性使其成为游戏脚本、配置文件和嵌入式系统的理想选择。通过 VML 编译器，Lua 程序可以在多种硬件平台上运行。
---

#### 🆕 字符串编码 (v1.65.19)

该语言编译器通过共享库 (Lib/shared/) 间接使用 VML 字符串体系。

| 伪指令 | 宽度 | 编码 | C 类型 |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU 模式** (默认): 字符串输出为 UTF-8 (`.string`)
**OS 模式**: 可通过 `VML_WSTRING` 宏判断编码

共享库已提供宽字符串转换函数 (wchar.h/uchar.h)，各语言编译器可按需使用。

## 编译器 README

### Lua 5.1 编译器

**路径**: `VMLPrepares/LuaCompiler/`
**完成度**: ~93% | 🟢 生产可用
**标准库**: `Lib/lua/`

#### 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator）
- ✅ 控制流 (if/elseif/else/while/for/repeat-until)
- ✅ 函数: 定义/多返回值/可变参数/匿名函数
- ✅ Table: 数组和字典基本操作
- ⚠️ 闭包/upvalue — 部分支持
- ⚠️ 标准库: print/type/math 基本函数
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- coroutine、dofile/require

**保留**（由 BIOS 实现底层）:
- table、metatable
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- printf/puts 映射到 UART

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
- 禁止动态加载（dofile/eval）
- 浮点运算可能需软浮点库

#### 使用
```bash
dotnet run --project VMLTool -- input.lua -o output.vml
```

#### 测试
`Test/Lu/` — 0 测试文件（测试目录待创建）
