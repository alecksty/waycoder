# VMLAssembler 子工程说明

> **写法**：本文件的例子用**规范写法** —— 寄存器一律带 `@`（`@R0`、`[@R12-8]`）。
> 裸名（`MOVE R0, [R12-8]`）**仍然能读**（为了老文本兼容），但新写的请带 `@`。
> ⚠ v0.96.328 起多一条：**不带 `@` 的寄存器形 token 由"本文件有没有同名标签"裁定** ——
> 有同名标签 ⇒ 它是**标签**（所以变量可以叫 `f1`/`d1`/`l1`/`R1`），没有才是寄存器。
> 详见 [VML 汇编语言规范](VML_ASSEMBLY_SPEC.md) 的「寄存器」一节规则 (d)。
> 原因（`call f1` 会被读成 `call R1`）与新旧对照见
> [`VML-README.md`](../VML-README.md) 的「汇编语法：寄存器一律带 `@` 标记」与
> [`VML_ASSEMBLY_SPEC.md`](VML_ASSEMBLY_SPEC.md) 的「寄存器」一节。

## 功能描述
VMLAssembler 是一个 VML (Virtual Machine Language) 汇编器，用于将 VML 汇编代码解析和汇编成 VML 程序对象（VMLProgram）。VMLAsembler 是 VML 工具链的核心组件，连接前端编译器与运行时/翻译器。

## 目录结构
```
VMLAssembler/
├── Instruction.cs           # 指令定义
├── OpCode.cs                # 操作码定义（指令集唯一权威定义）
├── Operand.cs               # 操作数定义
├── OperandType.cs           # 操作数类型定义
├── VMLAssembler.cs          # 汇编器主类
├── VMLAssembler.csproj      # 项目文件
├── VMLProgram.cs            # VML 程序对象
├── VML_ASSEMBLY_SPEC.md     # VML 汇编语言规范
└── README.md                # 本文件
```

## 核心功能
1. **词法分析**：解析 VML 汇编代码的词法结构
2. **语法分析**：解析指令、操作数和标签
3. **伪指令处理**：`.entry`/`.stack`/`.vectors`/`.data`/`.text`/`.string`/`.wstring`/`.ustring`/`.word`/`.byte`/`.halfword`/`.dword`/`.const`/`.include`/`.macro`/`.chipasm`/`.SPEED`/`.align`/`.equ`/`.if-.elif-.else-.endif`/`global`
4. **代码生成**：生成 VMLProgram 对象（指令列表 + 数据段 + 标签表 + 重定位表）
5. **数据处理**：处理数据段定义（含三层字符串体系：`.string` UTF-8 / `.wstring` UTF-16LE / `.ustring` UTF-32LE）
6. **标签处理**：处理标签和符号，支持前向/后向引用

## 支持的指令集

VMLAssembler 支持 100+ 个操作码（详见 [OpCode.cs](OpCode.cs) 和 [VML_ASSEMBLY_SPEC.md](VML_ASSEMBLY_SPEC.md)）：

| 类别 | 主要指令 |
|------|---------|
| 数据传送 | MOVE, MOVEH, MOVEB, MOVEF, MOVED, MOVEL, PUSH/POP 系列, PUSHL/POPL |
| 符号扩展 | SEXTB, SEXTH |
| 条件移动 | CMOVZ, CMOVNZ |
| 算术运算 | ADD, SUB, MUL, DIV, MOD, INC, DEC, NEG |
| 64位算术 | ADDL, SUBL, MULL, DIVL, MODL, NEGL, CMPL |
| 位运算 | AND, OR, XOR, NOT, SHL, SHR, SHLV, SHRV, ZERO |
| 64位位运算 | ANDL, ORL, XORL, NOTL, SHLL, SHRL |
| 比较/跳转 | CMP, TEST, JMP, JZ/JNZ, JE/JNE, JG/JL, JGE/JLE |
| 调用 | CALL, RET, ENTER, LEAVE |
| 浮点 | FADD, FSUB, FMUL, FDIV, FCMP, FNEG, FPUSH/FPOP, I2F, F2I, F2D, D2F |
| 双精度 | DADD, DSUB, DMUL, DDIV, DCMP, DNEG, DPUSH/DPOP, I2D, D2I |
| 64位转换 | I2L, L2I, F2L, L2F, D2L, L2D |
| 系统/中断 | SYSCALL, CLI, STI, INT, IRET, THROW, CATCH, ENDCATCH |
| 调试/伪指令 | BREAK, DUMP, TRACE, ASM, CHIPASM, LABEL |
| 其他 | NOP, HALT, CLC, STC |

> **v1.65.167+**: LOAD/STORE/LEA/FLOAD/FSTORE/DLOAD/DSTORE 已彻底删除，统一使用 MOVE 系列指令。

## 支持的操作数类型
- **寄存器**：`@R0`-`@R15`、`@F0`-`@F15`、`@D0`-`@D7`、`@L0`-`@L7`
  （裸名 `R0` / `F1` / `D0` / `L0` **仍可读**，见顶部说明）
- **立即数**：`#value` 或直接数字
- **内存寻址**：`[@address]` / `[@Rn]` / `[@Rn±offset]` / `[@label±offset]`（裸名 `[R0]` 仍可读）
- **标签**：用户定义的标签（**标签不加 `@`**；`call f1` 里的 `f1` 是标签）
- **间接寻址**：`[@reg]` 或 `[@[addr]]`（`@` 在括号**里**）
- **寄存器本身（`@` 在最前面）**：`@R0` —— 注意它与旧的间接寻址写法**同形不同义**，
  旧文本里的 `@R0`（间接）请改写 `[@R0]`

## 支持的数值格式
- **十进制**：如 `123`
- **十六进制**：如 `0x123`
- **二进制**：如 `0b1010`
- **八进制**：如 `0o123`
- **浮点数**：如 `3.14`, `#2.718`
- **字符串**：如 `"Hello\n"`

## 示例用法

### C# API 调用
```csharp
// 创建汇编器
var assembler = new VMLAssembler();

// 汇编 VML 代码
string vmlCode = @"
    .data
    msg: .string ""Hello, World!\n""

    .text
    start:
        MOVE @R0, msg       ; R0 = 字符串地址
        SYSCALL #1          ; 输出字符串
        HALT
";

var program = assembler.Assemble(vmlCode);

// 使用生成的程序
var vm = new VMLRuntime.VMLRuntime();
vm.LoadProgram(program);
vm.Run();
```

### VGA 显存输出示例
```asm
.data
msg: .string "Hello, VGA!"

.text
.entry start
start:
    MOVE @R0, msg          ; R0 = 字符串地址
    MOVE @R1, #0xB8000     ; R1 = VGA 显存基址
loop:
    MOVEB @R2, [@R0]       ; R2 = 当前字符（1字节）
    CMP @R2, #0            ; 遇到 null 终止符?
    JZ end
    MOVEB [@R1], @R2       ; 写入 VGA 显存
    ADD @R0, #1            ; 下一字符
    ADD @R1, #2            ; VGA 下一个位置（字符+属性）
    JMP loop
end:
    HALT
```

## 汇编流程
1. **初始化**：创建汇编器实例，初始化段状态
2. **预处理**：展开 `.include`、`.macro` 调用
3. **第一遍**：收集所有标签地址
4. **第二遍**：逐行解析代码，处理伪指令和数据定义
5. **指令编码**：将操作码和操作数编码为内部表示
6. **生成程序**：生成 VMLProgram 对象（指令列表 + 数据段 + 标签表 + 元数据）

## 相关文档
- [VML_ASSEMBLY_SPEC.md](VML_ASSEMBLY_SPEC.md) — VML 汇编语言完整规范
- [OpCode.cs](OpCode.cs) — 操作码枚举定义（指令集唯一权威定义）
- [docs/VML_ISA_SPEC.md](../docs/VML_ISA_SPEC.md) — VML 指令集架构规范
- [docs/SYSCALL_SPEC.md](../docs/SYSCALL_SPEC.md) — 系统调用规范
- [docs/VMB_FORMAT_SPEC.md](../docs/VMB_FORMAT_SPEC.md) — VMB 二进制格式规范
