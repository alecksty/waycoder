# WebAssembly 翻译器

**目标架构**: WebAssembly (Wasm)
**位宽**: 32-bit (栈机)
**完成度**: ~95% (79/83 操作码)
**代码行数**: ~850

## 支持的指令

核心 VML 操作码翻译为 WebAssembly 文本格式 (WAT)，包括:
- **算术逻辑**: i32.add/i32.sub/i32.mul/i32.div_s/i32.rem_s/i32.and/i32.or/i32.xor
- **内存**: i32.load/i32.store
- **控制流**: if/else/end/block/loop/br/br_if
- **调用**: call/call_indirect

## 汇编语法

- **语法风格**: WAT (WebAssembly Text Format), `(module`, `(func`, `(export`
- **注释前缀**: `;;`

## 缺失操作码

ALLOC/FREE(BIOS管理), 部分64-bit/浮点操作码通过软件库实现

## 特殊说明

- Wasm 是栈机架构
- 继承自 `TranslatorVM` 基类
