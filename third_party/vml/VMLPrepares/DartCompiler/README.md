# Dart 编译器

**路径**: `VMLPrepares/DartCompiler/`
**完成度**: ~97% | 🟢 生产可用
**标准库**: `Lib/dart/`

## 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ 控制流 (if/else/while/for/do-while/break/continue)
- ✅ 变量声明 (var/final/const/类型注解)
- ✅ 函数: 返回类型/参数/递归
- ✅ Class: 类定义/成员/方法
- ✅ 运算符: ++/--/三元/位运算/短路逻辑
- ✅ 复合赋值 (+= -= *= /= %=)
- ✅ import 语句
- ✅ 30 测试 0 失败
- ⚠️ async/await/Future — MCU 跳过
- ⚠️ mixin/extension — 未实现
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化。

**跳过**（MCU 不支持）:
- async/await、Future、Stream
- mixin

**保留**:
- 基本类型运算、控制流、函数调用
- print 映射到 UART

### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

## 使用
```bash
dotnet run --project VMLTool -- input.dart -o output.vml
```

## 测试
- `VMLTests/NewCompilerTests.cs` — 30 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/dart/` — 示例程序 (start/factorial/file_io/info)
