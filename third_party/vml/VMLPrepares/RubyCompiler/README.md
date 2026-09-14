# Ruby 编译器

**路径**: `VMLPrepares/RubyCompiler/`
**完成度**: ~96% | 🟢 生产可用
**标准库**: `Lib/ruby/`

## 功能
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

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化，自动跳过不兼容操作系统的特性。

**跳过**（MCU 不支持）:
- require 动态加载、yield/block

**保留**（BIOS 实现底层）:
- 基本类型运算、控制流、函数调用
- print/puts 映射到 UART

### OS 模式（`--mode os`）
OS 模式支持全部语言特性（文件系统、多线程等）。

### RAM 级别
- `--ram k`：KB级别（2KB~64KB）
- `--ram m`：MB级别（64KB~1MB，默认）
- `--ram g`：GB级别

## 使用
```bash
dotnet run --project VMLTool -- input.rb -o output.vml
```

## 测试
- `VMLTests/NewCompilerTests.cs` — 34 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/ruby/` — 示例程序 (start/factorial/file_io/info)
