# D 编译器

**路径**: `VMLPrepares/DCompiler/`
**完成度**: ~97% | 🟢 生产可用
**标准库**: `Lib/d/`

## 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ 模块系统: module/import
- ✅ 类/结构体/接口/枚举: class/struct/interface/enum
- ✅ C 风格类型系统 (int/float/double/bool/string/char/void)
- ✅ 控制流 (if/else/while/for/do-while/foreach/break/continue)
- ✅ 运算符: ++/--/位运算/三元运算符
- ✅ 32-bit signed int, 32-bit float, 64-bit double, 8-bit char
- ✅ 36 测试 0 失败
- ⚠️ 模板元编程 — 未实现
- ⚠️ contract/invariant — 未实现
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化。

**跳过**（MCU 不支持）:
- 模板元编程 (编译期展开过大)
- contract/invariant

**保留**:
- 基本 OOP (class/struct/interface)
- 模块导入
- 完整运算符支持

### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

## 使用
```bash
dotnet run --project VMLTool -- input.d -o output.vml
```

## 测试
- `VMLTests/NewCompilerTests.cs` — 36 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/d/` — 示例程序 (start/factorial/file_io/info)
- `Examples/benchmark/` — bench_int_d.d / bench_float_d.d
