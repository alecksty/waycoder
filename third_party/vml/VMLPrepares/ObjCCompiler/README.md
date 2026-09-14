# Objective-C 编译器

**路径**: `VMLPrepares/ObjCCompiler/`
**完成度**: ~99% | 🟢 生产可用
**标准库**: `Lib/objc/`
**依赖**: CCompiler (ObjC 是 C 的超集)

## 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ @interface/@implementation/@end 类定义
- ✅ 消息表达式 [obj msg: param]
- ✅ C 语法完全兼容 (if/else/while/for/do-while/switch/case/break/continue)
- ✅ 指针 Type* / 位运算 / 三元运算符
- ✅ 方法声明 (returnType)method:(paramType)param
- ✅ 15 测试 0 失败
- ✅ @property/@synthesize
- ⚠️ @protocol/category — 未实现
- ⚠️ Block/ARC — MCU 跳过
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include/#import)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
ObjC 汇编为目标保留 C 兼容子集。

**跳过**:
- Block、ARC、Protocol

**保留**:
- @interface/@implementation 基础 OOP
- C 完整语法
- 消息发送 (编译为函数调用)

### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

## 使用
```bash
dotnet run --project VMLTool -- input.m -o output.vml
```

## 测试
- `VMLTests/NewCompilerTests.cs` — 15 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/objc/` — 示例程序 (start/factorial/file_io/info)
