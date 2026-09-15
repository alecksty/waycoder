# VML（内置副本）

[VML](https://gitee.com/aleckstygit/vml) 的**精简副本**，以**源码复制**（vendoring）并入本仓库，
供手机端**在进程内**调用 —— 不是把它编成可执行文件丢进 shell 跑。

> 上游自己的 README 保留在 [`VML-README.md`](VML-README.md)（含 22 语言 / 18 后端翻译器的完整说明）。

## 来源

| | |
|---|---|
| 上游 | `https://gitee.com/aleckstygit/vml.git` |
| 版本 | **v1.66.67** |
| 提交 | `84708b69`（2026-08-18）|
| 复制时排除 | `bin/`、`obj/`、`.git*`、`*.user` |

## 保留了什么

| 目录 | 作用 |
|---|---|
| `VMLAssembler/` | 汇编器：VML 文本 → `VmlProgram` |
| `VMLRuntime/` | 虚拟机：装载 + 执行（`VmRuntime`）|
| `VMLPlugins/` | 前端编译器注册表（`PluginManager` / `IFrontendCompiler`）|
| `VMLPrepares/` | **22 种语言的前端编译器** + `CompilerBase`（25 个 csproj）|
| `VMLTool/` | **「一套工具编所有类型文件」的集成主体** —— 扩展名→语言的派发、include/library 路径、标准库链接、导出应用 |
| `VMLTranslators/` | 18 个后端翻译器 —— `VMLTool` 的**编译依赖**（不是可选装饰）|
| `VMLToHex/` | 产物格式化（hex/elf/bin…），同样是 `VMLTool` 的编译依赖 |
| `Lib/` | 标准库（高级语言编译要靠它链接，缺了编译出来的程序跑不了）|
| `Examples/` | **精选子集**：每种扩展名取最小的文件，共约 120 个 / 1.1 MB（原目录 128 MB，只留够做冒烟测试的）|

### 删掉了什么

`VMLIde/`（138 MB，Avalonia 桌面 IDE）、`VMLEmulators/`（131 MB，硬件模拟器）、
`VMLTests/` / `test/`（上游测试）、`web/` / `docs/` / `images/` / `Scripts/` / `tools/`
以及 `Examples/` 里的大文件（`sqlite3.c` 单个 8.8 MB 那种压力测试素材）。

## ⚠ 本地适配（同步上游后**必须重新施加**）

本地适配分两类，`sync.sh` 都会重新施加（`rsync` 是覆盖式的，不重放就丢）。

### 【A】csproj 属性 —— 不改会**编不过**

改了 **25 个 csproj**：

| 改动 | 不改会报什么 |
|---|---|
| `<OutputType>Exe</OutputType>` → `Library` | **NETSDK1150**：非自包含的可执行文件不能由自包含可执行文件引用 |
| 去掉 `<StartupObject>`（19 个）| **CS2017**：如果生成模块或库，则无法指定 /main |
| 去掉 `<RuntimeIdentifiers>`（24 个）| **NETSDK1047**：它的列表里没有 `android-arm64` |
| 去掉 `<PublishAot>`（25 个）| AOT 发布要求 RID；我们只要它们的代码，不做 AOT 发布 |

各编译器原本是 `Exe` 是因为它们带 `Program.cs` 可以独立当 CLI 用；我们只要它们的**代码**，
改 `Library` 后那个 `Main` 只是一个没人调用的方法，无副作用。

### 【B】源码级 —— 不改会**编得过、跑起来才出问题**

改动固化成 [`patches/`](patches/) 里的 patch，`sync.sh` 用 `git apply` 重放；
**打不上就直接退出**，不会让适配悄悄消失。

| patch | 改了什么 | 不改的后果 |
|---|---|---|
| `0001-file-system-root.patch` | 给 `VmRuntime` 加 `FileSystemRoot`；`ExecuteFileOpen` 按它解析相对路径、拒绝越界路径 | 手机上 `open("a.txt","w")` 按**进程 CWD** 解析 ⇒ 报 `Read-only file system`，或**写到别的地方**（沙箱失守） |

> 这是**唯一**一处源码级改动。之所以只改了 `ExecuteFileOpen` 一行调用点：
> `ExecuteFileRead/Write/Control` 操作的是已打开的 `FileStream` 句柄，不再解析路径。
> 全文件搜过 `new FileStream` / `File.` —— 只有这一处接用户给的路径
> （其余是固定路径的 `/proc/cpuinfo` 之类）。**将来上游新增按路径开文件的地方要一起加沙箱检查。**

## 怎么同步上游

用 [`sync.sh`](sync.sh)（它会把上面的本地适配一起重新施加）：

```bash
third_party/vml/sync.sh ~/Desktop/source/vml/vml
```

手动等价于：

```bash
UP=~/Desktop/source/vml/vml
for d in VMLAssembler VMLRuntime VMLPlugins VMLPrepares VMLTool VMLTranslators VMLToHex Lib; do
  rsync -a --delete --exclude 'bin/' --exclude 'obj/' --exclude 'Examples/' "$UP/$d/" "third_party/vml/$d/"
done
for f in VERSION LICENSE vmltool.config.xml Directory.Build.props .editorconfig; do
  cp "$UP/$f" third_party/vml/
done
# 然后重新施加本地适配（OutputType → Library、去掉 StartupObject）
```

**这是 vendoring 的固有代价**：上游的修复不会自己流过来。之所以仍选它而不是 git submodule ——
VML 的 `.git` 有 292 MB（历史里有个 593 MB 的单对象），挂成子模块会让**每个 clone WayCoder 的人**
都付这笔钱；而这些源码进本仓库后是普通文件，clone 即构建。

改动 VML 源码时**改这里就是改上游的副本** —— 如果某个修复对上游也有价值，记得回推一份。

## 待办：接上「一套工具编所有类型文件」

目前 `WayCoder.Maui.csproj` **只引用了 `VMLAssembler` + `VMLRuntime`**（够跑纯 VML 程序）。

接 `VMLTool`（= 接上 22 个前端编译器）时撞到一个**尚未解决**的构建阻塞：

> 那 22 个编译器是纯 `net10.0` 项目，而 MAUI Android 带着 `RuntimeIdentifier=android-arm64`
> 的全局属性流进 `ProjectReference` ⇒ 它们的 `project.assets.json` 里没有该 RID 的目标，
> 报 **NETSDK1047**。

**候选解法（都还没试）**：
1. 在 `third_party/vml/Directory.Build.props` 里对 `net10.0` 目标清掉 RID / 设 `SelfContained=false`
2. 给那些项目加 `<RuntimeIdentifiers>android-arm64</RuntimeIdentifiers>`
3. **把 VML 预先编成 DLL，用 `<Reference><HintPath>` 引** —— 彻底绕开 RID 传递，
   代价是多一个构建步骤

## 移动端适配须知（实测结论，别凭直觉改）

1. **不做成可执行文件**：iOS 的 `fork/exec` 被沙箱物理拒绝（永远上不了 iOS）；
   Android 10+ 的 W^X 让 app 私有目录里的文件不可 exec，得塞 `jniLibs` 当 `.so`。
   托管代码链进来这三条全没有。
2. **`AndroidLinkMode` 默认 `SdkOnly`**，即**用户程序集不参与裁剪** —— VML 的反射路径靠这个才活着。
   谁把它改成 `Full`、或把 `TrimMode` 改成 `full`，都可能打断
   `VMLPlugins.PluginManager` 的 `Assembly.LoadFrom` 路径。改之前先跑一遍 `vml test`。
3. **AOT 开着**（`RunAOTCompilation=true`）⇒ **`Reflection.Emit` 不可用**，影响面只有
   `VMLRuntime.Syscall.FFI.cs`（`DLOpen`/`NativeCallEx`）。它自带
   `RuntimeFeature.IsDynamicCodeSupported` 兜底，表现为「FFI 不可用」而**不是崩溃**。
4. **每次运行前必须 `DeviceManager.Instance.Reset()`** —— 单例，跨运行保留状态，
   不重置会让第二次运行和第一次的 MMIO 地址串掉。封装见 `WayCoder.Maui/Services/MauiVml.cs`。
5. **`Compile(string)` 那个重载不链标准库** —— 高级语言编译要走
   `CompileFileWithIncludes → AssembleWithIncludes → LinkLibraries → ApplyExports`
   那条完整流水线（就在 `VMLTool/Program.Compile.cs`）。少了它，症状是「编译过了、跑起来找不到 stdlib 函数」。
