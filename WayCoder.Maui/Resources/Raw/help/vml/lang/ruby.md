# Ruby

能写，但这个前端支持的特性最少 —— «bold»完全平铺着写«/»。

## 在手机上怎么跑

```
vml run examples/ruby/catch.rb
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 全部逻辑写在顶层，不要包函数、不要嵌套
- 直接调用 `ui_*`

## 示例

| 文件 | 演示什么 |
|---|---|
| `catch.rb` | 接方块（完全平铺的一份，这一路写不了函数） |
| `file_io.rb` | 读写文件 |
| `sysinfo.rb` | 设备信息 |

## 实测踩过的坑

- ⚠ «bold»不认 `&&`«/»（用 `and`，或者分开写 `if`）。
- ⚠ «bold»`def` 会报 `Unexpected token: End`«/» —— 也就是说这一路«bold»一个函数都写不了«/»，
  只能平铺（`examples/ruby/catch.rb` 就是这么写的）。

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/RubyCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### Ruby 语言编译器规范说明

> «bold»版本«/»：v1.0 | «bold»日期«/»：2026-07-06 | «bold»修订者«/»：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| «bold»目标标准«/» | Ruby 1.9+ 子集 |
| «bold»发布年份«/» | 1995 (Ruby 1.9: 2007) |
| «bold»完成度«/» | ~96% |
| «bold»文件扩展名«/» | `.rb` |

#### 概述

本编译器支持 Ruby 语言的核心子集，将 Ruby 源代码编译为 VML (Virtual Machine Language) 汇编代码。编译器流程：预处理 → 词法分析 → 语法分析 → 代码生成。

#### 支持的语言特性

##### 1. 数据类型

- «bold»整数«/»: `Integer` — 整数类型 (4 字节有符号)
- «bold»浮点数«/»: `Float` — 单精度浮点数
- «bold»字符串«/»: `String` — 字符串字面量，双引号 `"..."` 或单引号 `'...'`
- «bold»符号«/»: `Symbol` — 以 `:` 开头，如 `:name`
- «bold»数组«/»: `Array` — 方括号字面量 `[1, 2, 3]`
- «bold»nil«/»: `nil` — 空值
- «bold»布尔值«/»: `true` / `false`

##### 2. 变量

```ruby
x = 10           # 局部变量
@x = 20          # 实例变量 (已识别但功能受限)
name = "hello"   # 字符串变量
```

##### 3. 运算符

###### 算术运算符
```ruby
+    # 加法
-    # 减法
*    # 乘法
/    # 除法
%    # 取模
**   # 幂运算
```

###### 复合赋值
```ruby
+=   # 加后赋值
-=   # 减后赋值
*=   # 乘后赋值
/=   # 除后赋值
```

###### 比较运算符
```ruby
==   # 等于
!=   # 不等于
<    # 小于
>    # 大于
<=   # 小于等于
>=   # 大于等于
<=>  # 飞船运算符 (比较结果 -1/0/1)
```

###### 逻辑运算符
```ruby
!    # 逻辑非 (not)
```

##### 4. 控制流

###### 条件语句
```ruby
if condition
  # then body
elsif other_condition
  # elsif body
else
  # else body
end

unless condition
  # then body (条件为假时执行)
end
```

###### 循环语句
```ruby
while condition
  # loop body
end

until condition
  # loop body (条件为假时重复)
end
```

###### for 循环
```ruby
for var in start..end
  # loop body
end
```

##### 5. 方法定义

```ruby
def method_name(param1, param2)
  # method body
  return value
end

def method_name  # 无参数方法
  # body
end
```

##### 6. 类定义

```ruby
class ClassName
  def method_name
    # method body
  end
end
```

类在内部被编译为带 `self.` 前缀的方法，`self` 存储为第一个局部变量。

##### 7. 返回值

```ruby
return value     # 带返回值
return           # 返回 nil
```

##### 8. 方法调用

```ruby
method_name(arg1, arg2)
object.method_name(arg1)
method_name       # 无参数调用
```

##### 9. 注释

```ruby
# 单行注释
```

#### 表达式优先级

| 优先级 | 类型 | 运算符 |
|:------:|:-----|:-------|
| 1 (最低) | 赋值 | `=` `+=` `-=` `*=` `/=` |
| 2 | 比较 | `==` `!=` `<` `>` `<=` `>=` `<=>` |
| 3 | 加法 | `+` `-` |
| 4 | 乘法 | `*` `/` `%` `**` |
| 5 | 一元 | `-` `!` |
| 6 (最高) | 主表达式 | 字面量、变量、函数调用、括号 |

#### 预定义宏

| 宏名 | 值 |
|:-----|:---|
| `__VML__` | `1` |
| `__VML_VERSION__` | `"1.65.32"` |
| `__RUBY__` | `1` |
| `__DATE__` | 编译日期 |
| `__TIME__` | 编译时间 |

#### 已知限制

- 不支持 `module`、`yield`、`block`、`mixin`
- 不支持异常处理 (`begin`/`rescue`/`ensure`)
- 不支持字符串插值 (`"hello #{name}"`)
- 不支持 hash 字面量 `{key => value}`
- 不支持 splat 操作符 `*args`
- 类继承和多态方法分发尚未实现

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

### Ruby 编译器

«bold»路径«/»: `VMLPrepares/RubyCompiler/`
«bold»完成度«/»: ~96% | 🟢 生产可用
«bold»标准库«/»: `Lib/ruby/`

#### 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ 控制流 (if/elsif/else/unless/while/until/for/in)
- ✅ 函数: def/return/参数传递
- ✅ Class: 类定义/方法/实例变量
- ✅ 运算符: 赋值/比较/算术/一元/<=>/and/or/not
- ✅ 字面量: 整数/浮点/字符串/布尔/数组/Symbol/** 幂/.. 范围
- ✅ 复合赋值 (+= -= *= /= %=)
- ✅ case/when/else 分支
- ✅ 34 测试 0 失败
- ✅ module/mixin — include 内联
- ⚠️ yield/block/Proc — 未实现
- ✅ require 库导入
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include)

#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化，自动跳过不兼容操作系统的特性。

«bold»跳过«/»（MCU 不支持）:
- require 动态加载、yield/block

«bold»保留«/»（BIOS 实现底层）:
- 基本类型运算、控制流、函数调用
- print/puts 映射到 UART

##### OS 模式（`--mode os`）
OS 模式支持全部语言特性（文件系统、多线程等）。

##### RAM 级别
- `--ram k`：KB级别（2KB~64KB）
- `--ram m`：MB级别（64KB~1MB，默认）
- `--ram g`：GB级别

#### 使用
```bash
dotnet run --project VMLTool -- input.rb -o output.vml
```

#### 测试
- `VMLTests/NewCompilerTests.cs` — 34 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/ruby/` — 示例程序 (start/factorial/file_io/info)
