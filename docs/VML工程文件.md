# VML 工程文件（`.vmk`）

> 一个 XML 描述"**这一个** VML 程序怎么编"。配合 `vml make xxxx.vmk` 使用，
> 并支持**从现成的 Makefile 一次性导入**。

---

## 一、为什么要有它

VML 本来只有「把一个源文件丢给编译器」这一种用法：

```
vml run examples/c/tetris.c
```

这件事在兼容性战役里已经实实在在撞到过（`docs/老程序兼容性.md` 第九节）：

> `cmatrix` 报的 `VERSION` **不是库缺失** —— 它是 Makefile 里 `-DVERSION="..."` 传进来的。
> 老程序的编译方式通常是 `gcc -DVERSION=\"1.2\" -DHAVE_CONFIG_H ...`，
> 而我们是"把源文件丢给 `vml run`"，**没有构建系统** ⇒ 这类宏全缺。

当时的应急修法是 **`VMLTOOL_DEFINE` 环境变量**。它能用，但形态不对：
**一个项目的构建描述不该待在环境变量里** —— 换台机器、换个终端、CI 里跑都要重新
export，而且没地方记「入口是哪个文件、头在哪、要链哪些库」。

`.vmk` 把它扶正。

---

## 二、格式

```xml
<?xml version="1.0" encoding="utf-8"?>
<VMLProject Version="1">
  <Name>tetris</Name>
  <Entry>src/tetris.c</Entry>          <!-- 必填：唯一入口 -->
  <Output>build/tetris.vml</Output>    <!-- 选填，默认与 Entry 同目录同名 .vml -->

  <Includes>                           <!-- 对应 -I -->
    <Dir>src</Dir>
  </Includes>

  <Defines>                            <!-- 对应 -D -->
    <Define Name="VERSION" Value="1.2"/>
    <Define Name="DEBUG"/>             <!-- 只有名字 ⇒ 取 1，与 -DDEBUG 同义 -->
  </Defines>

  <Libs>                               <!-- 选填；额外要挂的库模块 -->
    <Lib>stdio</Lib>
  </Libs>
</VMLProject>
```

| 元素 | 必填 | 说明 |
|---|---|---|
| `<Name>` | 否 | 只用于显示；缺省取入口文件名 |
| `<Entry>` | **是** | **唯一入口**源文件（含 `main` 的那个），相对本文件所在目录 |
| `<Output>` | 否 | 产物路径；缺省 = 入口换 `.vml` 后缀 |
| `<Includes><Dir>` | 否 | 追加的头文件搜索路径（`-I`）。**排在** VML 内置 `Lib` 之前，与 gcc 的 `-I` 语义一致 |
| `<Defines><Define>` | 否 | 宏（`-D`）。没有 `Value` 属性时取 `1` |
| `<Libs><Lib>` | 否 | 额外要挂的库模块名 |

**只有这六个顶层元素。** 多一个不认识的，`vml make` 会**报错**（见第五节）。

### 产物类型与格式

`<Output>` 上还有两个属性，**都能省**（省了就是 `exe` / `vml`，老行为）：

```xml
<Output Kind="exe" Format="vml">build/tetris.vml</Output>
```

| 属性 | 取值 | 默认 |
|---|---|---|
| `Kind` | `exe`（可执行程序）/ `lib`（库） | `exe` |
| `Format` | 见下表 | `vml` |

**`Kind="lib"`** 落到实现上是 `VmlProgram.IsLibrary` —— 影响**死代码消除与链接**：
库不删"没人调用"的函数（它本来就是给别人调的），入口也不要求有 `main`。

**格式**目前**已经能出**两种，其余是**预留的名字**：

| 状态 | 格式 | 说明 |
|---|---|---|
| ✅ 已实现 | `vml` | VML 汇编文本（默认），可读、可 diff、可再汇编 |
| ✅ 已实现 | `vmb` | VML 字节码，装载更快 |
| ⬜ 预留 | `bin` `rom` `elf` `hex` `s19` `exe` `dll` `class` | 转译后端在 `VMLTranslators/` 里已有，但**还没接到这条路上** |

⚠ **预留的名字现在写进 `.vmk` 不会报"拼错了"，但 `make` 会报「还没做」。**
这两句是**故意分开**的：前者是排期问题（名字对、功能没做），后者多半是拼错。
混成一句会让用户去猜是哪一种。

⚠ **绝不静默退回 `.vml`**：写了 `Format="hex"` 却拿到一个 `.vml` 文件，
用户会以为"转译完了"，而手上是个**根本烧不进芯片**的文件。

> 为什么现在就把名字留出来：`.vmk` 是枢纽格式，一旦发出去就有别人的工程文件在用。
> 等真做转译时再改字段名，等于让所有既有 `.vmk` 失效 —— 留位置的成本是零，
> 改格式的成本是所有用户。

### 跨平台

`.vmk` 是**跨平台**的：Windows 上生成的工程文件要能在 macOS / 安卓上打开。

* 写出去时路径分隔符一律 **正斜杠**（`src/main.c`，不是 `src\main.c`）；
* 读进来时**两种都认** —— 手写的 `.vmk` 带反斜杠也照样能编。

> 为什么必须统一：`Path.Combine("base", "src\\main.c")` 在 Unix 上得到的是
> **一个名字里带反斜杠的文件** —— 不报错，只是"文件不存在"，最难查的那种。

---

## 三、命令

```bash
# 从 Makefile 一次性导入，生成 proj.vmk
vmlcli make --import Makefile

# 按工程文件编译，产物写到 <Output>
vmlcli make proj.vmk
```

手机端命令行页同一套说法：`vml make proj.vmk`。

⚠ `--import` 只收注册表里认得的构建文件（现在只有 Makefile 一族）。
喂一个不认的文件名会**明确报错**，不会静默生成一个空工程。

### 顺带：`-D` / `-I` 也补上了

命令行直接编一个文件时也能喂宏与头文件路径（`.vmk` 的 `<Defines>`/`<Includes>`
在实现上就是复用的这条通路）：

```bash
vmlcli -D VERSION=1.2 -D DEBUG -I src -I ../inc examples/c/tetris.c
```

⚠ **`-D` 走的是 `SetConfig("defines", …)`，不是 `VMLTOOL_DEFINE` 环境变量。**
后者在多数前端里是**静态快照**（`PredefinedMacros` 是 `static readonly`，
只读一次），同一个进程里换宏不生效；只有 C 因为每次构造都重读才碰巧是对的。
两套行为不一致，所以 `.vmk` 不建立在"碰巧"上。

---

## 四、Makefile 导入

**只认手写的那种**：

```make
CC      = gcc
CFLAGS  = -O2 -Wall -DVERSION=\"1.2\" -Iinclude
LIBS    = -lm
SRCS    = src/main.c
OBJS    = $(SRCS:.c=.o)

prog: $(OBJS)
	$(CC) $(CFLAGS) -o prog $(OBJS) $(LIBS)
```

认得的写法：`SRCS` / `SOURCES` / `OBJS` / `OBJECTS`（`.o` 反推 `.c`）、
规则依赖、命令行里直接写的 `xxx.c`；`-D` / `-I` 从 `CFLAGS` / `CPPFLAGS` / 命令行里扫。

### ⚠ 多个源文件**会报出来**，不会静默取第一个

**VML 没有跨编译单元的链接。** `CompilerProgramBase` 的多文件模式明确拒绝 `-o`，
它只是把每个 `.c` **各编成同名的 `.vml`**，没有符号合并。所以导入器只能取一个入口，
而"静默取第一个"的后果是**编过了、少了半个程序** —— 最坏的那种失败。

导入时会明确告诉你是哪一个被取了、其余哪些不会进来。

### ⚠ 两种引号的语义**相反**

| Makefile 里写的 | gcc 实际收到 | `.vmk` 里的值 |
|---|---|---|
| `-DVERSION=\"1.2\"` | `-DVERSION="1.2"` | `"1.2"`（**带引号**，老程序拿它当字符串字面量用） |
| `-DFOO="bar"` | `-DFOO=bar`（shell 吃掉了引号） | `bar`（**不带引号**） |

### 明确**不认**（报错说明原因，不糊弄）

* **纯转发式** —— `all: ; $(MAKE) -C sub`。真正的构建描述在子目录里，导出来只会是空清单；
* **IDE 自动生成的 `.mk`** —— 用了 `-include` 递归拉依赖的（CubeIDE 的 `sources.mk` 那种），
  那是机器生成的结构，不是手写的构建描述。

### 导入是**一次性**的

生成的 `.vmk` 顶部会写明是由哪个文件导入的。**此后以 `.vmk` 为准**，Makefile 不再参与构建。

> 为什么不"每次构建去读 Makefile"：两处真源必然漂移，而漂移的症状是
> **"改了 Makefile 却不生效"** —— 两边看着都对，最难查。

---

## 五、加一种新格式

`.vmk` 的定位是**枢纽格式**：各种老项目的构建描述一次性转进来。所以"加一种格式"
是**加一个类 + 注册一行**，CLI 与手机端都不用动：

```csharp
// WayCoder/UI/Shared/VmlProjectImport.cs
public static readonly IReadOnlyList<IProjectImporter> All = new IProjectImporter[]
{
    new MakefileImporter(),
    new CMakeImporter(),      // ← 加这一行
};
```

每个导入器实现三样：`Name`、`CanHandle(文件名)`、`Import(路径, 入口提示)`。

**每个导入器都必须遵守同一条铁律：认不出的东西要报出来，不猜、不静默跳过。**
老构建系统里塞得下任何东西，导入器要做的不是"尽量多认"，而是**让用户知道
哪些没被带过来**。

---

## 六、落点

| 文件 | 是什么 |
|---|---|
| `WayCoder/UI/Shared/VmlProject.cs` | `.vmk` 模型 + 解析 + 序列化 |
| `WayCoder/UI/Shared/VmlProjectImport.cs` | `IProjectImporter` + 注册表 |
| `WayCoder/UI/Shared/VmlMakefileImporter.cs` | Makefile 导入器 |
| `WayCoder/Test/SelfTest.Chunk32.cs` | 判据（32 条） |
| `scripts/vmlcli/Program.cs` | `make` 子命令 + `-D`/`-I` |

⚠ 这三个文件住在 **`UI/Shared/`** 而不是 `third_party/vml/VMLTool/`，是**有原因的**：
桌面（`WayCoder`）**不引用** VMLTool（那会把 18 个后端翻译器一起拖进来），
而 `UI/Shared` 是**桌面自测 / `scripts/vmlcli` / 手机端三方都能编到**的唯一目录
（`vmlcli` 用 `<Compile Include="../../WayCoder/UI/Shared/…">` 直接编同一份源码）。
放 VMLTool 里就只有 CLI 用得上，另外两边各得再抄一份 —— 本仓头号坑。
