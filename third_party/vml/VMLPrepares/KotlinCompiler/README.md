# Kotlin 1.0 编译器

**路径**: `VMLPrepares/KotlinCompiler/`
**完成度**: ~93% | 🟢 生产可用
**标准库**: `Lib/kotlin/`（待创建）

## 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator ~1200行）
- ✅ 变量: val/var + 类型标注
- ✅ 控制流: if/else/while/for-in/when
- ✅ when 表达式: 值匹配 / in 范围检查 / is 类型检查
- ✅ 函数: fun 定义/Lambda/单表达式体
- ✅ class: 主构造函数/成员方法/属性访问
- ✅ data class: toString/equals 自动生成
- ✅ 运算符: 算术/比较/逻辑/复合赋值/..范围/in/is
- ✅ 内置函数: println/print (字符串和整数)
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)

## 缺失功能
- ❌ 协程 (coroutine)
- ❌ 空安全 (?, !!, ?:)
- ❌ 扩展函数
- ✅ 密封类 (sealed class) — 解析 + when 穷尽性检查
- ❌ 标准库 (Lib/kotlin/)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- coroutine

**保留**（由 BIOS 实现底层）:
- class(堆分配 SYSCALL #40)、data class
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- println/print 映射到 SYSCALL

### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

## 使用
```bash
dotnet run --project VMLTool -- input.kt -o output.vml
```

## 测试
29 测试通过（内嵌在编译器源代码验证中）
