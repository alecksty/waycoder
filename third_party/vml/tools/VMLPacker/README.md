# VMLPacker

VML 程序打包工具 — 将 `.vml` / `.vmb` 文件打包为跨平台独立可执行文件。

## 快速开始

### 1. 命令行打包

```bash
cd ~/Desktop/source/vm/vml
./Scripts/pack_vml.sh -i <输入文件> -o <输出名> -r <运行时>
```

**参数说明：**

| 参数 | 说明 | 示例 |
|------|------|------|
| `-i` | 输入的 VML/VMB 文件 | `test/vml/test_basic.vml` |
| `-o` | 输出可执行文件名 | `myapp` |
| `-r` | 目标运行时 | `osx-x64`、`win-x64`、`linux-x64`、`all` |

**示例：**

```bash
# macOS x64
./Scripts/pack_vml.sh -i myprogram.vml -o myapp -r osx-x64

# Windows x64
./Scripts/pack_vml.sh -i myprogram.vml -o myapp -r win-x64

# Linux x64
./Scripts/pack_vml.sh -i myprogram.vml -o myapp -r linux-x64

# 全平台打包（生成三个可执行文件）
./Scripts/pack_vml.sh -i myprogram.vml -o myapp -r all
```

### 2. PowerShell 打包（Windows）

```powershell
cd ~/Desktop/source/vm/vml
.\Scripts\PackVml.ps1 -InputFile "myprogram.vml" -OutputName "myapp" -Runtime "win-x64"
```

### 3. 手动编译

```bash
cd VMLPacker
dotnet build -c Release
dotnet publish -c Release -r <runtime> --self-contained -o ../publish/<runtime>
```

## 输出

打包结果位于 `publish/<runtime>/` 目录：

```
publish/osx-x64/
├── myapp           # macOS x64 可执行文件 (~37MB)
publish/win-x64/
├── myapp.exe       # Windows x64 可执行文件
publish/linux-x64/
├── myapp          # Linux x64 可执行文件
```

## 依赖

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- VMLAssembler（随项目自带）
- VMLRuntime（随项目自带）

## 项目结构

```
VMLPacker/
├── VMLPacker.csproj   # 项目文件
├── Program.cs        # 入口：嵌入 VML + 执行
└── README.md        # 本文档
```

## 工作原理

1. 将输入的 `.vml` / `.vmb` 文件作为嵌入式资源编译进程序
2. 运行时读取嵌入式资源为 `VmlProgram`
3. 调用 `VmlRuntime` 解释执行

程序入口仅为约 30 行代码，实际 VML 执行完全复用原有的 VMLRuntime，无重复代码。
