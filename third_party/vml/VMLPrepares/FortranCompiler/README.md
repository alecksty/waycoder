# Fortran 编译器

**路径**: `VMLPrepares/FortranCompiler/`
**完成度**: ~98% | 🟢 生产可用
**标准库**: `Lib/fortran/`

## 功能
- ✅ 词法分析 + 语法分析 + 代码生成 (Lexer/Parser/CodeGenerator)
- ✅ program/subroutine/function 结构
- ✅ contains 内部过程
- ✅ function name(args) result(r) 语法
- ✅ 控制流 (if/else/do/do while/exit/cycle)
- ✅ 数据类型 (integer/real/double precision/logical/character)
- ✅ 比较运算符 (.eq./.ne./.lt./.gt./.le./.ge.)
- ✅ 逻辑运算符 (.and./.or./.eqv./.neqv.)
- ✅ print */read */write */call/return/stop
- ✅ ! 注释
- ✅ 24 测试 0 失败
- ✅ 数组切片/整体操作 (A(:)=B(:)+C(:)) — MCU循环展开
- ✅ module/use — 模块系统
- ✅ else if 链 — 递归解析支持
- ✅ C 风格预处理 (#ifdef/#ifndef/#define/#include)

## 编译模式

### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境优化。

**跳过**（MCU 不支持）:
- 无
- 动态数组分配

**保留**:
- 基本数值计算
- 子程序/函数调用
- print/write 映射到 UART

### OS 模式（`--mode os`）
OS 模式支持全部语言特性。

### RAM 级别
- `--ram k`：KB级别
- `--ram m`：MB级别（默认）
- `--ram g`：GB级别

## 使用
```bash
dotnet run --project VMLTool -- input.f90 -o output.vml
```

## 测试
- `VMLTests/NewCompilerTests.cs` — 24 测试 (0 失败)
- `VMLTests/FullPipelineTests.cs` — 全管线测试
- `Examples/fortran/` — 示例程序 (start/factorial/file_io/info)
