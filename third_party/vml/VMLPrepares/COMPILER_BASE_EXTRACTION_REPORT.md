# 编译器基类提取分析报告

> 报告日期: 2026-06-08  
> 状态: **Phase 1-3 全部完成** — v1.65.32  
> 52 提交 · ~1,970 行消除 · 258 bug 修复

---

## 1. 已完成的基类提取

### 新建 CompilerBase 文件

| 文件 | 行数 | 功能 |
|------|:---:|------|
| `ParserBase.cs` | 75 | 泛型 Parser 基类 — Peek/Advance/Match/Check/Expect |
| `AstNodes.cs` | 102 | 14 个跨语言共享 AST 节点 |
| `TypedCodeGen.cs` | 80 | 类型敏感指令选择中间基类 |

### CompilerBase 增强

| 文件 | 新增方法 |
|------|---------|
| `LexerBase.cs` | `Error()`, `ReadNumber()` virtual + 5 虚方法 |
| `LexerHelper.cs` | `IsHexDigit()`, `IsOctalDigit()`, `IsBinaryDigit()` |
| `CompilerHelper.cs` | `CreatePredefinedMacros()`, `PreprocessSource()` |
| `CodeGeneratorBase.cs` | `InitSimpleCompiler()`, `AddStringCached()`, `WStr()`, `EmitExit()` |

### 编译器迁移统计

| 项 | 编译器数 |
|----|:-------:|
| EmitExit (手动 SYSCALL 3 → 基类) | 14 |
| placeLabel → AddLabel | 12 |
| ParserBase 迁移 | 7 |
| StatementManager 迁移 | 6 |
| TypedCodeGen 迁移 | 2 |
| 零 labels 编译器 | 15/22 |
| LexerHelper.IsHexDigit 全覆盖 | 22 |
| Lexer 使用基类方法 | JavaScript 重构完成 |

### 转译器后端增强

| 文件 | 功能 |
|------|------|
| `TranslatorPlugin.cs` | 泛型 Plugin 模板 — 消除 18 个重复文件 (376 行) |
| `BaseTranslator.cs` | `TryTranslateCommonOpcode()`, `NormalizeAndTranslate()`, `EmitCLC()`, `EmitSTC()`, `EscapeString()` |
| `Translator16bit.cs` | `GetOp()` — MSP430+PIC24 共享 |
| `TranslatorVM.cs` | `SanitizeLabel()` — DotNET+JVM 共享 |
| `Translator32bit.cs` | `UseImmediateHash` — x86+MIPS+RISC-V 共享 |

---

## 2. 已创建的新基类

| 基类 | 继承自 | 使用者 |
|------|--------|--------|
| `ParserBase<TToken, TTokenType>` | — | Dart, R, ObjC, Ruby, D, Fortran, Kotlin |
| `AstNode` | — | 所有编译器可迁移 |
| `TypedCodeGen<TTypeEnum>` | `CodeGeneratorBase` | Lua, Python |
| `TranslatorPlugin<T>` | `BaseTranslatorPlugin` | 全部 18 转译器 |

---

## 3. 已修正的 Bug

| Bug | 数量 |
|-----|:---:|
| LABEL 使用 OperandType.IMMEDIATE → LABEL | 258 |
| Lua pow 循环 LABEL 操作数类型 | 3 |
| Kotlin Lexer 仅支持 \n 转义 | 1 |
| EmitCode 覆盖绕过 TryTranslateCommonOpcode | 11 |

---

## 4. 原"不做"项的状态更新

| 原排除项 | 当前状态 |
|----------|---------|
| Parser 骨架提取 | ✅ ParserBase<TToken,TTokenType> 已创建，7 编译器迁移 |
| LexerBase 抽象类 | ✅ LexerBase 已增强：Error(), ReadEscape(), ReadNumber() virtual |
| CodeGenerator 基类扩展 | ✅ TypedCodeGen, placeLabel→AddLabel, EmitExit, InitSimpleCompiler |
| 14 个 Plugin 标准化 | ✅ TranslatorPlugin\<T\> 降至 1 行/插件 (376 行消除) |

---

## 5. 剩余工作

| 项 | 复杂度 | 说明 |
|----|:----:|------|
| 15 Parser → ParserBase | 中 | 语义差异 (Peek=当前/EOF-based IsAtEnd) |
| Python/CSharp/Java labels | 高 | visitor 模式 / 位置标记 |
| 8-bit 浮点分发统一 | 高 | 内联优化不可移动 |
| JavaScript Array 内联方法 | 高 | ~400 行可提取到共享库 |

---

*报告最后更新: 2026-06-08*
