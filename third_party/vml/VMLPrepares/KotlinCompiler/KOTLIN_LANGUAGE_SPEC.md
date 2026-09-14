# Kotlin 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Kotlin 1.0+ (最小子集) |
| **发布年份** | 2016 |
| **完成度** | ~93% |
| **MCU完成度** | ~91% |
| **测试** | 29 通过 |
| **更新** | 2026-05-21: data class toString/equals 实现; sealed class 穷尽性检查升级为编译错误; when 表达式返回值确认已实现 |

## 概述

本编译器支持 Kotlin 语言子集，将 Kotlin 源代码编译为 VML 汇编。编译器采用 Lexer → Parser → CodeGenerator 三阶段架构，代码规模约 1200 行 C#。

## 支持的语言特性

### 1. 数据类型和字面量
- ✅ 整数: `42`, `-10`
- ✅ 浮点: `3.14`
- ✅ 布尔: `true`, `false`
- ✅ 字符串: `"hello"` (支持 `\n` 转义)
- ✅ String 模板: `"Hello, $name!"`, `"${expr}"` (代码生成内联处理)
- ✅ 类型标注: `: Int`, `: String` 等 (仅语法解析，不执行类型检查)

### 2. 变量声明
- ✅ `val` — 不可变变量
- ✅ `var` — 可变变量
- ✅ `val x: Int = 42` — 带类型标注（解析但忽略类型）

### 3. 函数
- ✅ `fun` 关键字声明函数
- ✅ 参数列表（参数名 + 可选类型标注）
- ✅ 返回值类型标注（解析但忽略）
- ✅ `return` 返回语句
- ✅ Lambda 表达式: `{ x, y -> x + y }`
- ✅ 函数体简写: `fun add(a: Int, b: Int) = a + b`

### 4. 控制流
- ✅ `if`/`else` — 条件分支
- ✅ `while` — 前置条件循环
- ✅ `for (i in start..end)` — 范围循环（解析和代码生成均实现）
- ✅ `when (x) { 1 -> ...; 2 -> ...; else -> ... }` — 多路分支（含 in 范围、is 类型检查）
- ✅ 比较链: `a < b`, `x in 1..10`, `x is Int`
- ✅ 复合赋值: `+=`, `-=`, `*=`, `/=` (解析和代码生成均实现)

### 5. 类
- ✅ `class ClassName(val prop1: Type, var prop2: Type)` — 主构造函数类
- ✅ `data class` — 数据类（自动生成 toString/equals）
- ✅ `ClassName(args)` — 构造调用（堆分配，SYSCALL #40）
- ✅ 成员方法: `fun ClassName.method(params) { body }`
- ✅ 成员访问: `obj.property`, `obj.method(args)`

### 6. 内置函数
- ✅ `println(expr)` — 输出整数 + 换行 (SYSCALL #6 + #4)
- ✅ `print(expr)` — 输出整数不换行 (SYSCALL #6)
- ✅ `println("string")` — 输出字符串 (SYSCALL #4)

### 7. 运算符
- ✅ 算术: `+`, `-`, `*`, `/`, `%`
- ✅ 比较: `==`, `!=`, `<`, `>`, `<=`, `>=`
- ✅ 逻辑: `&&`, `||`, `!`
- ✅ 赋值: `=`, `+=`, `-=`, `*=`, `/=`
- ✅ `..` 范围运算符 (用于 for 和 in)
- ✅ `in` 成员检查 / `is` 类型检查
- ✅ 一元: `-`, `!`

## 代码生成 (VML)

| Kotlin 语法 | VML 指令 |
|:-----------|:---------|
| val/var 声明 | STORE 到 R12-relative 局部变量帧 |
| 算术运算 | ADD SUB MUL DIV MOD |
| 条件 if/else | JZ/JNE/JMP + LABEL |
| while/for 循环 | LABEL + 比较 + JZ/JMP |
| when 表达式 | PUSH 主语; 比较分支条件; JNE 下一个; 匹配则 JMP end |
| println(x) | MOVE R0, x; SYSCALL #6; MOVE R0, #10; SYSCALL #4 |
| 类构造 | SYSCALL #40 堆分配; STORE 属性; RET 返回地址 |
| data class | 自动生成 toString/equals 方法 |
| 函数调用 | PUSH R15, PUSH R12, MOVE R12 R13 (prologue), 调用者清理 R13 |
| return | MOVE R13 R12, POP R12, POP R15, RET (epilogue) |
| Lambda | PUSH R15/R14, MOVE R14 R13, 闭包体, POP R14, POP R15, RET |

## 缺失功能

### 高级语言特性
- ❌ 协程 (coroutine)
- ✅ 密封类 (sealed class) — 解析、when 穷尽性检查
- ❌ 扩展函数 (extension function)
- ❌ 空安全 (`?`, `!!`, `?:`)
- ❌ 智能类型转换 (smart cast)
- ✅ when 作为表达式返回值

### 标准库
- ❌ 标准库 `Lib/kotlin/` 目录（待创建）
- ❌ `readLine()` — 输入
- ❌ 字符串函数 (length, substring 等)
- ❌ 集合类 (List, Map, Set)

## 浮点与64位编译模式

与所有 VML 编译器相同的三参数控制：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard`/`soft`/`none` | `hard` | 32位浮点 (Float) |
| `--float64` | `hard`/`soft`/`none` | `soft` | 64位浮点 (Double) |
| `--int64` | `hard`/`soft`/`none` | `soft` | 64位整数 (Long) |

## 编译方式

```bash
# 直接编译
dotnet run --project VMLTool -- input.kt -o output.vml

# 运行
dotnet run --project VMLEmulators/ConsoleEmulator -- -r output.vml
```

## 完成度评估

| 组件 | 完成度 |
|:----|:------:|
| 词法分析 | ~98% (完整 token 集，含 data 关键字) |
| 语法分析 | ~95% (fun/val/var/if/while/for/when/class/data class/is/in/lambda/range/sealed/when穷尽性) |
| 代码生成 | ~93% (表达式/控制流/字符串/类/data class/Lambda/内联汇编/SafeCall/Elvis/泛型擦除) |
| 标准库 | ~50% (Lib/kotlin/ 基础函数已内联) |
| **总体** | **~93%** |

---

## 🆕 字符串类型 (v1.65.19)

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
