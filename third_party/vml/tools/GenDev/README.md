# GenDev — VML 设备代码生成器

根据外部硬件描述文件（XML 格式）自动生成 22 种语言的设备访问头文件和 VML 设备定义代码。

## 快速开始

```bash
# 从 XML 生成 C 头文件
dotnet run --project tools/GenDev -- -i device.xml -o device.h -l c

# 生成 BASIC 头文件
dotnet run --project tools/GenDev -- -i device.xml -o device.bas -l basic

# 生成所有 22 种语言的头文件
dotnet run --project tools/GenDev -- -i device.xml -a -d output/
```

## CLI

```
GenDev [options] <input-file>
```

| 短名 | 长名 | 说明 |
|------|------|------|
| `-i` | `--input` | 输入设备描述 XML 文件 |
| `-o` | `--output` | 输出文件路径 |
| `-d` | `--output-dir` | 输出目录（与 `-a` 配合使用） |
| `-l` | `--language` | 目标语言（默认: c） |
| `-a` | `--all-languages` | 生成全部 22 种语言 |
| `-h` | `--help` | 显示帮助 |
| `-v` | `--version` | 显示版本号 |

输入文件也可作为位置参数（第一个非标志参数）。

## 支持的 22 种语言

| 语言 | `-l` 键 | 输出扩展名 |
|------|---------|-----------|
| C | `c` | `.h` |
| C++ | `cpp` / `c++` | `.hpp` |
| BASIC | `basic` | `.bas` |
| Pascal | `pascal` | `.pas` |
| Python | `python` / `py` | `.py` |
| Lua | `lua` | `.lua` |
| Forth | `forth` / `fs` | `.fth` |
| Rust | `rust` | `.rs` |
| Go | `go` | `.go` |
| Ladder | `ladder` / `ld` | `.ld` |
| Java | `java` | `.java` |
| JavaScript | `javascript` / `js` | `.js` |
| C# | `csharp` / `cs` | `.cs` |
| Swift | `swift` | `.swift` |
| Kotlin | `kotlin` / `kt` | `.kt` |
| Scheme | `scheme` / `scm` | `.scm` |
| VML | `vml` | `.vml` |
| Ruby | `ruby` | `.rb` |
| Dart | `dart` | `.dart` |
| Objective-C | `objc` | `.h` |
| R | `r` | `.r` |
| D | `d` | `.d` |
| Fortran | `fortran` | `.f90` |

## 项目结构

```
tools/GenDev/
├── GenDev.csproj          # 项目文件
├── Program.cs             # 入口 + 参数解析 + 编排
├── Models/
│   └── DeviceModel.cs     # XML 设备模式
└── Generators/
    ├── ICodeGenerator.cs  # 生成器接口
    ├── CodeGeneratorHelper.cs
    ├── CCodeGenerator.cs
    ├── BasicCodeGenerator.cs
    ├── PascalCodeGenerator.cs
    ├── VMLCodeGenerator.cs
    ├── ...（共 22 个生成器）
    ├── RubyCodeGenerator.cs
    ├── DartCodeGenerator.cs
    ├── ObjCCodeGenerator.cs
    ├── RCodeGenerator.cs
    ├── DCodeGenerator.cs
    └── FortranCodeGenerator.cs
```

## 输入 XML 格式

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Device>
  <Metadata>
    <Name>ATmega328P</Name>
    <Description>8-bit AVR Microcontroller</Description>
    ...
  </Metadata>
  <CPU>
    <Architecture>AVR</Architecture>
    <Bits>8</Bits>
    <Registers>
      <Register name="PORTA" address="0x0022" size="1" access="RW" />
      ...
    </Registers>
  </CPU>
  <Memory>
    <Segment name="FLASH" type="flash" start="0x0000" end="0x7FFF" />
    <Segment name="SRAM" type="ram" start="0x0100" end="0x08FF" />
  </Memory>
  <Peripherals>
    <Peripheral name="USART0" type="serial" base="0x00C0">
      <Register name="UDR0" address="0x00C6" size="1" access="RW" />
    </Peripheral>
  </Peripherals>
  <Interrupts>
    <Interrupt vector="1" name="INT0" description="External Interrupt 0" />
  </Interrupts>
  <Pins>
    <Pin number="1" name="PC6" type="I/O" />
  </Pins>
</Device>
```

## 依赖

- .NET 10.0 SDK
- YamlDotNet (NuGet)
