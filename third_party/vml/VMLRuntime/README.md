# VMLRuntime 子工程说明

## 功能描述
VMLRuntime 是一个虚拟机运行时，用于执行 VML (Virtual Machine Language) 程序，管理内存和寄存器，处理指令执行等。

## 目录结构
```
VMLRuntime/
├── VM.cs              # 虚拟机高级接口
├── VMConfig.cs        # 虚拟机配置
├── VMLRuntime.cs      # 虚拟机运行时核心
└── VMLRuntime.csproj  # 项目文件
```

## 核心功能
1. **指令执行**：执行 VML 指令，包括算术、逻辑、分支、内存操作等
2. **内存管理**：管理虚拟机内存，包括分配、释放和访问
3. **寄存器管理**：管理 16 个通用寄存器和标志寄存器
4. **栈管理**：管理栈操作，包括 PUSH、POP 等
5. **VGA 显示**：管理 VGA 显存和显示
6. **系统调用**：处理系统调用，如输入输出
7. **配置管理**：支持 XML 格式的配置文件

## 指令集支持
- **算术指令**：ADD, SUB, MUL, DIV, MOD, INC, DEC, NEG
- **逻辑指令**：AND, OR, XOR, NOT, SHL, SHR
- **内存指令**：MOVE, MOVEH, MOVEB
- **栈指令**：PUSH, POP
- **分支指令**：JMP, JZ, JNZ, JE, JNE, JL, JLE, JG, JGE
- **调用指令**：CALL, RET, RET_VAL
- **系统指令**：SYSCALL, HALT, NOP
- **内存管理**：ALLOC, FREE

## 配置管理
- **XML 配置**：支持 XML 格式的配置文件
- **十六进制支持**：支持 0x 开头的十六进制值
- **VGA 配置**：支持配置 VGA 显示参数

## 待改进的点
1. **指令集**：扩展支持更多指令
2. **内存管理**：改进内存分配和释放算法
3. **性能优化**：提高指令执行速度
4. **错误处理**：增强错误处理和异常处理
5. **调试工具**：添加调试工具和内存查看器
6. **多线程**：支持多线程执行
7. **安全**：增强内存安全和指令验证

## 示例用法
```csharp
// 创建虚拟机实例
var config = VMConfig.LoadFromFile("VMConfig.xml");
var vm = new VMLRuntime(config.MemorySize, config);

// 加载 VML 程序
var assembler = new VMLAssembler();
var program = assembler.Assemble(vmlCode);
vm.LoadProgram(program);

// 运行程序
vm.Run();

// 单步执行
vm.Step();

// 获取 VGA 显示内容
byte[] vgaMemory = vm.GetVGAMemory();
```

## 栈指针同步
- R13 寄存器作为栈指针
- 栈指针初始值为 `memorySize - 4`
- 在 PUSH、POP、MOVE、ADD、SUB 指令中同步 sp 与 R13
- 确保栈操作的安全性和正确性

## 内存访问边界检查
- 在 SetMemory 和 GetMemory 方法中验证地址有效性
- 抛出内存访问越界异常
- 确保内存操作的安全性
