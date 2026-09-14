# VML 编译器共同规范

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

> 所有 22 种语言编译器必须共同遵守的规范。新增编译器或修改现有编译器时必须参照本文档。

---

## 一、编译输出规范

### 1.1 源码注释（必须）

所有编译器**默认**将源码内容以 `;` 注释形式嵌入到生成的 VML 汇编中。

- 格式：`; <行号>: <源码内容>`
- 注释放在该源码对应生成的第一条 VML 指令之前
- 同一源码行生成多条指令时，注释只出现在第一个指令前
- 空行、纯注释行不输出注释
- 通过 `--no-source-comment` 开关禁用

实现方式：
```csharp
// 在 Compile() 中设置
result.SourceLines = source.Split('\n');
// CodeGeneratorBase.BuildProgram() 自动从 CompilerOptionsContext 读取 SourceComment
```

### 1.2 函数级注释（必须）

每个函数/方法/子程序在生成的 VML 中必须包含结构化注释块，格式如下：

```vml
; -------------------------------------------
; source   : int add(int a, int b)
; function : add
; param   : int a
; param   : int b
; return   : int
; --------------------------------------------
```

规则：
- 分隔线为 44 个减号
- `source` 行：完整的函数声明/签名（语言原生语法）
- `function` 行：函数名
- `param` 行：每个参数一行，格式为 `类型 参数名`
- `return` 行：返回值类型（无返回值的写 `void` 或语言等价的空返回）
- 无参数的函数不输出 `param` 行
- 注释通过 `new Instruction(OpCode.NOP, [], 0, "; ...")` 实现

### 1.3 标准库引用

- 使用 `.include` 伪指令引用标准库，不内联库代码
- 格式：`.linked  "Lib/<Language>/stdlib.vml"`

---

## 二、MCU/OS 双目标模式

### 2.1 默认 MCU 模式

- 所有编译器默认 `--mode mcu`
- MCU 模式跳过：async/await、Thread、goroutine、try/catch/throw、reflection、动态加载
- BIOS 保留：malloc/free、fopen/fread、POKE/PEEK

### 2.2 MCU 模式检查

```csharp
if (CompilerOptionsContext.Current.IsMCU)
{
    // 跳过 OS 依赖特性
}
```

### 2.3 MCU 跳过行为

遇到不支持的语法时：
- 编译时跳过（不生成代码），并输出警告
- **禁止**抛出异常或编译失败
- 警告信息格式：`警告: <特性名> 在 MCU 模式下不可用，已跳过`

---

## 三、寄存器约定

| 寄存器 | 用途 | 说明 |
|:------:|:-----|:-----|
| R0 | 累加器 / 返回值 | 函数返回值，CCv2 arg0 |
| R1-R3 | CCv2 arg1-arg3 | 参数寄存器 |
| R4-R11 | 临时寄存器 | caller-saved |
| R12 | BP (基址指针) | callee-saved，栈帧基址 |
| R13 | SP (栈指针) | 向下增长 |
| R14 | LR (链接寄存器) | callee-saved |
| R15 | RA (返回地址) | callee-saved |
| F0 | 浮点返回值 | — |
| F0-F15 | 浮点寄存器 | 32位单精度 |
| D0-D7 | 双精度寄存器 | 64位 |
| L0-L7 | 长整数寄存器 | 64位 |

---

## 四、调用约定

| 语言 | 默认约定 | 参数传递 | 栈清理 |
|:-----|:---------|:---------|:------|
| C / C++ | cdecl | 全部从右到左压栈 | 调用者 |
| BASIC | CCv2 | 前4参数 R0-R3，其余压栈 | 调用者 |
| Pascal | pascal | 从左到右压栈 | 被调用者 |
| 其他语言 | stdcall | 全部从右到左压栈 | 被调用者 |

---

## 五、OpCode 同步规则

- **`VMLAssembler/OpCode.cs`** 是 OpCode 唯一权威定义，**禁止修改枚举值、排序、注释块结构**
- 新增指令只能在现有间隙（40-42, 59-61, 83-85, 89-99）中按顺序添加
- **不得调整已有成员的顺序或值**
- `VMLFast/VMLRuntimeC/include/vml_opcodes.h` 必须与 C# OpCode.cs 值一一对应

---

## 六、语法兼容性守则

### 6.1 修改编译器，不修改测试

如果测试代码是语法正确的标准语言代码，但编译器不能正确编译，则**必须修改编译器**来适配，不允许简化测试代码。

### 6.2 编译器必须支持的语法

- 所在语言的合法语法必须被正确编译
- 不支持的特性应在 MCU 模式下输出警告并跳过
- **禁止**因编译器能力不足而要求用户修改源码

---

## 七、代码组织规范

### 7.1 目录结构

```
VMLPrepares/<Language>Compiler/
├── <Language>Compiler.cs    # 入口，实现 IFrontendCompiler
├── Lexer.cs                 # 词法分析
├── Parser.cs                # 语法分析 (或 Parser*.cs)
├── ASTNode.cs               # AST 节点定义
├── CodeGenerator.cs         # 代码生成主文件
├── CodeGenerator*.cs        # 代码生成 partial class 拆分
└── ...
```

### 7.2 基类继承

| 组件 | 基类 |
|:-----|:-----|
| Lexer | `CompilerBase.LexerBase` |
| CodeGenerator (C-like) | `CompilerBase.CLikeCodegen` |
| CodeGenerator (OOP) | `CompilerBase.OopCodegen` |
| CodeGenerator (通用) | `CompilerBase.CodeGeneratorBase` |

### 7.3 Partial Class 拆分

代码生成器超过 500 行时应拆分为多个 partial class 文件：
- `CodeGenerator.Core.cs` — 字段、构造函数、辅助方法
- `CodeGenerator.VisitExpr.cs` — 表达式访问
- `CodeGenerator.VisitStmt.cs` — 语句访问
- `CodeGenerator.Functions.cs` — 函数/方法生成

### 7.4 命名规范

- 编译器入口类：`<Language>Compiler`，实现 `IFrontendCompiler`
- Compile 方法签名：`VmlProgram Compile(string source)` 或静态方法
- 代码生成器字段使用 `_camelCase` 前缀

---

## 八、测试规范

### 8.1 测试位置

所有语言编译测试集中在 `VMLTests/CompilerTests_Lang.cs` 和 `VMLTests/CompilerTests_Structural.cs`。

### 8.2 测试命名

```
<Language>_<Feature>_<ExpectedBehavior>
```

示例：
- `C_Function_Compiles`
- `Rust_Match_Returns`

### 8.3 测试验证

- 编译测试：`Assert.NotNull(p)` 验证编译成功
- 运行时测试：`MakeRuntime()` + `LoadProgram()` + `Run()` + `Assert.Equal()`
- 编译失败测试：验证抛出异常

---

## 九、CLI 接口规范

### 9.1 统一标志

| 标志 | 说明 | 默认值 |
|:-----|:-----|:------|
| `--mode mcu\|os` | 目标模式 | `mcu` |
| `--source-comment` / `--no-source-comment` | 源码注释开关 | 开启 |
| `-o <file>` | 输出文件 | — |
| `--float32 hard\|soft\|none` | 32位浮点模式 | `hard` |
| `--float64 hard\|soft\|none` | 64位浮点模式 | `soft` |
| `--int64 hard\|soft\|none` | 64位整数模式 | `soft` |

### 9.2 帮助信息

每个编译器必须支持 `-h` / `--help` 输出使用说明。

---

## 十、禁止事项

1. **禁止**直接修改 VML 测试用例中的标准语言代码来绕过编译器 bug
2. **禁止**在 `Lib/shared/` 中使用手写 VML 汇编（应用 C 编写）
3. **禁止**使用 `NotImplementedException` 或 stub 代码
4. **禁止**调整 `OpCode.cs` 中已有枚举成员的值或顺序
5. **禁止**仅部分编译器实现通用功能（源码注释、函数注释等）
6. **禁止**编译器因 MCU 不支持的特性而崩溃（应跳过并警告）
