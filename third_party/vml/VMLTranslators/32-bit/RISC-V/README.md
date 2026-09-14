# RISC-V 翻译器

**目标架构**: RISC-V (SiFive HiFive1)  
**位宽**: 32-bit  
**完成度**: 96% (80/83 操作码)  
**代码行数**: ~566  

## 支持的指令

核心 VML 操作码翻译为 RISC-V 汇编，包括:

- **数据搬运**: LOAD, STORE, MOVE, PUSH, POP, LEA
- **算术逻辑**: ADD, SUB, MUL, DIV, MOD, AND, OR, XOR, NOT
- **位移**: SHL, SHR, SHLV, SHRV
- **控制流**: CMP, JMP, JZ/JNZ, JE/JNE, JG/JL, JGE/JLE
- **调用**: CALL, RET, ENTER, LEAVE, SYSCALL
- **中断**: CLI, STI, INT, IRET
- **浮点**: FADD, FSUB, FMUL, FDIV, FCMP, FPUSH, FPOP
- **双精度**: DADD, DSUB, DMUL, DDIV, DCMP
- **长整数**: ADDL, SUBL, MULL, DIVL

## 通用指令助记符

`add/addi/sub/lw/sw/jal/beq/bne/ecall`

## 汇编语法

- **语法风格**: GAS, , 
- **注释前缀**: `#`

## 缺失操作码

ALLOC/FREE

## 特殊说明

- ALLOC/FREE 由 BIOS 层管理，不在翻译器层级处理
- 翻译器继承自 `Translator32-bit` 基类
- 寄存器映射和调用约定见 [调用约定规范](../../../docs/CALLING_CONVENTIONS.md)
