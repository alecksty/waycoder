# .NET 翻译器

**目标架构**: .NET CIL (Common Intermediate Language)
**位宽**: 32-bit (栈机)
**完成度**: ~96% (80/83 操作码)
**代码行数**: ~980

## 支持的指令

核心 VML 操作码翻译为 .NET CIL，包括:
- **数据搬运**: LOAD, STORE, MOVE (映射到 ldc.i4/ldloc/stloc)
- **算术逻辑**: ADD, SUB, MUL, DIV, MOD, AND, OR, XOR, NOT (add/sub/mul/div/rem/and/or/xor/not)
- **控制流**: CMP+JMP/JZ/JNZ (beq/bne/br)
- **调用**: CALL (call), RET (ret)

## 汇编语法

- **语法风格**: ILAsm 语法, `.assembly`, `.method`, `.class`
- **注释前缀**: `//`

## 缺失操作码

ALLOC/FREE(BIOS管理), 部分64-bit/浮点操作码通过软件库实现

## 特殊说明

- .NET CIL 是栈机架构
- 继承自 `TranslatorVM` 基类
