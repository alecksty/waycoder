# VMLTranslators 子工程说明

> **v1.66.19** | 18 后端翻译器 | 基类合并完成

## 功能描述
VMLTranslators 是一个代码转译器集合，用于将 VML (Virtual Machine Language) 指令转译为不同架构的汇编代码。

## 基类架构
```
BaseTranslator                 # TryTranslateCommonOpcode, NormalizeAndTranslate, EmitCLC/EmitSTC
├── Translator8bit              # HexPrefix=$, FloatLibLabel, EmitCall
├── Translator16bit             # GetOp, AddMnemonic/SubMnemonic/MovMnemonic
├── Translator32bit             # EmitBinary32, EmitBinFloat, EmitCondBranch, UseImmediateHash
└── TranslatorVM                # SanitizeLabel, EmitStackBinary, MethodDeclOpen/Close
BaseTranslatorPlugin            # 插件适配器基类
TranslatorPlugin&lt;T&gt;            # 泛型插件模板 (18插件→1行/插件)
```

## 目录结构
```
VMLTranslators/
├── BaseTranslator.cs           # 转译器基类
├── BaseTranslatorPlugin.cs     # 插件适配器
├── TranslatorPlugin.cs         # 泛型插件模板 (v1.66.5)
├── TranslatedProgram.cs        # 转译结果类
├── TranslatorFactory.cs        # 转译器工厂
├── VMLTranslators.csproj       # 项目文件
├── 8-bit/                      # 8位架构转译器
│   ├── 6502/
│   │   ├── Translator6502.cs   # 6502 架构转译器
│   │   ├── Reverse/            # 反向转译器（6502 → VML）
│   │   └── Docs/               # 6502 相关文档
│   ├── Z80/
│   │   ├── TranslatorZ80.cs    # Z80 架构转译器
│   │   ├── Reverse/            # 反向转译器（Z80 → VML）
│   │   └── Docs/               # Z80 相关文档
│   └── 8051/
│       ├── Translator8051.cs   # 8051 架构转译器
│       ├── Reverse/            # 反向转译器（8051 → VML）
│       └── Docs/               # 8051 相关文档
└── 32-bit/                     # 32位架构转译器
    ├── ARM-CM/
    │   ├── TranslatorARMCM.cs  # ARM Cortex-M 架构转译器
    │   ├── Reverse/            # 反向转译器（ARM-CM → VML）
    │   └── Docs/               # ARM-CM 相关文档
    ├── MIPS/
    │   ├── TranslatorMIPS.cs   # MIPS 架构转译器
    │   ├── Reverse/            # 反向转译器（MIPS → VML）
    │   └── Docs/               # MIPS 相关文档
    ├── RISC-V/
    │   ├── TranslatorRISCV.cs  # RISC-V 架构转译器
    │   ├── Reverse/            # 反向转译器（RISC-V → VML）
    │   └── Docs/               # RISC-V 相关文档
    ├── x86/
    │   ├── TranslatorX86.cs    # x86 架构转译器
    │   ├── Reverse/            # 反向转译器（x86 → VML）
    │   └── Docs/               # x86 相关文档
    └── 68000/
        ├── Translator68000.cs  # Motorola 68000 架构转译器
        ├── Reverse/            # 反向转译器（68000 → VML）
        └── Docs/               # 68000 相关文档
```

## 核心功能
1. **多架构支持**：支持多种 CPU 架构的代码转译
2. **统一接口**：提供统一的转译接口
3. **可扩展性**：易于添加新的架构支持
4. **代码生成**：生成目标架构的汇编代码

## 当前支持的架构

### 8位架构
- **6502**：8位微处理器，用于早期计算机和游戏机（Apple II, Commodore 64, NES）
- **Z80**：8位微处理器，用于早期计算机和游戏机（ZX Spectrum, MSX, Game Boy）
- **8051**：8位微控制器，广泛应用于嵌入式系统

### 32位架构
- **ARM M0**：ARM Cortex-M0 32位微控制器，低功耗嵌入式应用
- **MIPS**：32位/64位 RISC 架构，用于路由器、游戏机等
- **RISC-V**：开源 RISC 架构，现代嵌入式系统和服务器
- **x86**：Intel/AMD 32位架构，个人计算机和服务器
- **68000**：Motorola 16/32位混合架构，用于经典计算机（Amiga, Atari ST, Macintosh）

## 未来规划

### 近期目标
1. **反向转译器**：为每种架构开发反向转译器（汇编 → VML）
2. **文档完善**：为每种架构编写详细的使用文档和示例
3. **测试套件**：为每种架构添加完整的测试用例

### 中期目标
1. **架构扩展**：添加更多架构支持，如 ARM M4、PowerPC、AVR 等
2. **优化改进**：优化生成的代码，提高性能和代码大小
3. **工具集成**：集成到VMLToolchain工具链中

### 长期目标
1. **交叉编译**：支持跨架构编译和链接
2. **调试支持**：添加调试信息和符号支持
3. **标准库**：为每种架构提供标准库支持

## 示例用法
```csharp
// 创建 VML 程序
var vmlProgram = new VMLProgram();
// 添加指令...

// 创建转译器
var translator = TranslatorFactory.CreateTranslator("x86", vmlProgram);

// 转译代码
var translatedProgram = translator.Translate();

// 输出转译结果
Console.WriteLine(translatedProgram.Code);
```

## 转译流程
1. **初始化**：创建转译器实例
2. **转译**：调用 Translate() 方法
3. **生成头部**：EmitHeader()
4. **生成数据段**：EmitDataSection()
5. **生成代码**：EmitCode()，调用 TranslateInstruction() 处理每条指令
6. **生成尾部**：EmitFooter()
7. **返回结果**：返回 TranslatedProgram 对象
