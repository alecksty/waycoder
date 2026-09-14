# JVM 翻译器

**目标架构**: JVM (Java Virtual Machine)
**位宽**: 32-bit (栈机)
**完成度**: ~96% (80/83 操作码)
**代码行数**: ~986

## 支持的指令

核心 VML 操作码翻译为 JVM 字节码，包括:
- **数据搬运**: LOAD, STORE, MOVE (映射到 iconst/bipush/iload/istore)
- **算术逻辑**: ADD, SUB, MUL, DIV, MOD, AND, OR, XOR, NOT (iadd/isub/imul/idiv/irem/iand/ior/ixor)
- **控制流**: CMP+JMP/JZ/JNZ (ifeq/ifne/if_icmpeq/goto)
- **调用**: CALL (invokevirtual/invokestatic), RET (return)
- **栈操作**: PUSH, POP

## 汇编语法

- **语法风格**: Jasmin 语法, `.class`, `.method`, `.end method`
- **注释前缀**: `;`

## 缺失操作码

ALLOC/FREE(BIOS管理), 部分64-bit/浮点操作码通过软件库实现

## 特殊说明

- JVM 是栈机架构，与寄存器架构翻译逻辑不同
- 继承自 `TranslatorVM` 基类
