# Scheme (R5RS 子集) 编译器

**路径**: `VMLPrepares/SchemeCompiler/`
**完成度**: ~93% | 🟢 生产可用
**标准库**: `Lib/scheme/`（待创建）

## 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGen ~1800行）
- ✅ S-表达式解析
- ✅ define / lambda / 函数定义和调用
- ✅ if / cond / and / or 条件
- ✅ 基本类型: 整数/浮点/布尔/字符串/符号
- ✅ 算术: + - * / quotient remainder
- ✅ 比较: = < > <= >=
- ✅ 列表: cons/car/cdr/list/null?/pair?
- ✅ 高阶函数: apply/map
- ✅ GC 标记 (预留接口)
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)

## 缺失功能
- ✅ 尾递归优化 (TCO)
- ❌ call/cc (call-with-current-continuation)
- ❌ 宏系统 (define-syntax/syntax-rules)
- ❌ 向量 (vector)
- ❌ 字符/字符串完整操作
- ❌ 完整数值塔 (complex/rational)
- ❌ 标准库 (Lib/scheme/)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

**跳过**（遇到这些语法不生成代码）:
- call/cc (需栈捕获)、动态eval

**保留**（由 BIOS 实现底层）:
- cons(堆分配)、GC标记
- POKE/PEEK 内存映射 I/O (MMIO)
- 基本类型运算、控制流、函数调用
- display/write 映射到 SYSCALL

### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

## 使用
```bash
dotnet run --project VMLTool -- input.scm -o output.vml
```

## 测试
测试内嵌在编译器源代码验证中
