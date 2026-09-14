# R 编译器

**路径**: `VMLPrepares/RCompiler/`
**完成度**: ~96% | 🟢 生产可用
**标准库**: `Lib/r/`

## 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ 控制流 (if/else/for/while/repeat/break/next)
- ✅ 函数: function() 定义/参数/return/匿名函数
- ✅ 赋值: <- / <<- / = 运算符
- ✅ 数据结构: c() 向量 / list() / 公式 ~ 运算符
- ✅ %in% 运算符
- ✅ 科学计数法数值
- ✅ 31 测试 0 失败
- ⚠️ S3/S4 对象系统 — 未实现
- ⚠️ data.frame/matrix — 未实现
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化。

**跳过**:
- source() 动态加载
- S3/S4 对象系统
- 图形设备

**保留**:
- 基本统计运算
- 向量操作 (c/seq/rep)
- print 映射到 UART

### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

## 使用
```bash
dotnet run --project VMLTool -- input.r -o output.vml
```

## 测试
- `VMLTests/NewCompilerTests.cs` — 31 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/r/` — 示例程序 (start/factorial/file_io/info)
