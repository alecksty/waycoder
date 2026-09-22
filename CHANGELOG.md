## v0.96.378 — 图形老程序兼容性：种子填充的**算法核心**（三个 syscall 的第一个地基）

用户要「继续完善图形老程序兼容性」，并点了做法：

> 图像块的读写做个 syscall 来实现 / 包括 floodfill 也做个 syscall 来做

### 一、先把语料扩大，拿到**真实**的缺口清单

上一版只压了 6 个程序。这一版又取了 8 个（`day2/3/4`、`makeline`、`test`、
`userInput` 系列）：

    ✅ day2 / day3 / day4 / makeline / test —— 编译通过
    ❌ userInput —— 未定义的函数 'textheight'
    ❌（上一版剩下的）floodfill(10 次) + getimage/putimage/imagesize/XOR_PUT

⇒ 缺口收成**三族**：① 种子填充、② 图像块读写、③ 文字度量（`textwidth`/`textheight`）。

### 二、这一版做掉①的**算法核心**：`UI/Shared/FloodFill.cs`

**⚠ 关键设计决定：返回「水平游程」而不是逐像素。**

消费方是**保留模式**的场景（`VmlScene` 只有图元、没有像素缓冲）。逐像素输出意味着
一次填充要往场景里塞**几十万个** `ui_pixel` 图元 —— 光想想就知道不行。
而扫描线填充**天生**按行产出连续段：**一段 = 一个矩形(x, y, w, 1)**。

    **算法形状要和消费方对齐** —— 这也是这里选扫描线、不选简单四连通递归的原因。

BGI 语义照抄不改：`floodfill(x, y, border)` 遇到 `border` 色就停、四连通填。
⚠ 种子点**本身在边界色上**时什么都不做（老程序「点在线上」是常见写法），**不是错误**。

判据 7 条（`TestChunk32`），都是纯逻辑、桌面直接测：
框内 4 像素填满 / 游程数 = 2（不是逐像素）/ 越界种子不炸 /
**整行连通 ⇒ 每行一段 40 宽（不是 120 段 1 宽）** ← 这条直接钉住"落得到场景上"。

### 三、查清的两件事（决定了后面怎么接）

* **两端都有光栅化器可用**：`WayCoder.Maui` 编的是 `../WayCoder/**/*.cs`（含 `Infra/`）
  ⇒ `DrawRunner` / `RasterImage` / `PngEncoder` 在桌面与手机**都能调到**；
  `IVmlHost` 已经有 `OpenWindow(VmlScene)`（宿主负责渲染），
  再加一个"光栅化一块区域"的能力是与现有一致的落点。
* `VmlScene` **没有任何像素/栅格状态**（纯保留模式图元）—— 所以读回确实要新机制，
  不能从现有结构里"顺手"拿到。

自测 **6469/6469**（新增 7 条）。

**下一步**：583/584/585 三个号（`FloodFill` / `GetImage` / `PutImage`）+ 宿主光栅化钩子
+ C 侧包装 + `graphics.h` 接线。

## v0.96.377 — ⚠ **多文件程序**（`.vmk` 的真正目的）+ 手机端接线 + 优化开关

用户点破了 `.vmk` 的**真正目的**：

> 加 vmk 的原因是为了适配老程序的 makefile 文件，**老程序很多都是多文件的**，
> vml 编译器没办法只编译一个文件来运行。

**我上一版把这件事判错了。** 我把 `.vmk` 做成单入口，理由是"VML 没有跨编译单元的链接"
（`CompilerProgramBase` 的多文件模式明确拒绝 `-o`），并让导入器**报出**多文件而不是解决它。
**实测证明多文件做得到** —— 走的是**库那条路**。

### 一、多文件怎么做到的

`LinkLibraries` 合并库时会**重映射数据标签**（`LibraryLinker.LinkSingleLibrary` 的
`labelMapping`），两个编译单元的同名 `static` 因此互不干扰。所以"多文件"是**两步**：

| 步骤 | 做法 | 产物 |
|---|---|---|
| ① 每个附加编译单元 → **目标文件** | `autoLinkStdLib: false` + **库清单为空** + `IsLibrary = true` | 实测 **20~28 条指令** |
| ② 目标文件链进 `<Entry>` | 并进 `libraryPaths` | 49070 条（单文件 49030） |

⚠ **少了①的代价不只是"大"，是结果错**：两个文件直接链 —— 98080 条（正好两倍），
而且 `helper(20)` 该得 **43**、实得 **102944**（同名函数被两份实现各定义一次，
链接器的重映射指到了错的那份）。

⚠ **目标文件那次编译必须给空库清单**：把 `libraryPaths` 传进去的话，**即使
`autoLinkStdLib: false` 也仍然会把库内联进来**（实测同一个 `util.c`：空清单 28 条，
带清单 39916 条）。

### 二、`.vmk` 加了 `<Sources>`

```xml
<Entry>src/main.c</Entry>        <!-- 含 main 的那个 -->
<Sources>
  <File>src/util.c</File>
</Sources>
<ObjDir>.vmk-obj</ObjDir>        <!-- 选填，中间产物放哪 -->
```

Makefile 导入器同步改：多源文件**收进 `<Sources>`**（不再是"只取一个"的警告）。
⚠ 两个文件都含 `main` 时**必须报出来**（挑错入口 = 编过了、少了半个程序）。

### 三、手机端接线

`MauiVml.MakeProject` + `ShellPage` 的 `vml make` 子命令，与桌面**共用同一条
`BuildProgram`**（不另写编译链）。三个 `.vmk` 文件住在 `UI/Shared/`，
所以手机端**自动就编得到** —— 这一版只补"接一条命令"。

### 四、优化开关 `-O0/-O1/-O2/-Os`

在此之前这条链**从来没跑过优化**：`vmltool.config.xml` 里那个 `OptimizationLevel`
只被**上游 CLI** 读，`vmlcli` 与手机端都不读它 ⇒ 恒为 0。汇编器里的
`OptimizationPipeline` / `LoopOptimizationPass` / `DataFlowAnalysisPass`
一直是"写了但没被调用"。

现在 `-O1` 起会跑流水线，**开关列表逐字照抄上游**（那几个 pass 被显式关掉并注明
"实验性""有标签损坏 bug"—— 那是踩过的坑，不是保守，**别顺手打开**）。

⚠ **如实说**：接上之后实测一个 49070 条指令的程序，`-O1`/`-O2` 后**仍是 49070**
—— 因为唯一开着的是 **NOP 消除**，而这个程序没有 NOP 可消。
**开关是通的，优化器还很空。**

⚠ `-O` 的位置是 `make -O2 proj.vmk`（子命令在前）。写成 `-O2 make …` 会把 `make`
当成源文件路径。这一条我自己踩过一次：三档对比跑出来的"结果一致"其实是
**三次都跑了同一个旧产物**（前三次调用全部解析失败打了用法）。

### 五、顺带补的 `--lib`

`--lib <路径.vml>` 把额外编译单元链进来（多文件的底层机制，也让上一条能被手工验证）。
⚠ 只能给**文件**不能给目录 —— 给目录会让 `ConvertLibraryPathsToIncludes` 把该目录下
每个 `.vml` 都挂上再全量链接（实测同一个 hello.c：给目录 93423 条指令、
给库文件 36261 条）。

### 六、验收

    ✅ 多文件端到端：make → 目标文件 20 条 + 入口链成 49070 条 → **跑出 helper=43|seed=100**
       （两个编译单元的**同名 static 互不干扰**，这是整条路能成立的关键证据）
    ✅ 反证：不带 --lib 编 main.c ⇒ 报「未定义的函数 'helper'」
    ✅ 反证：-O3 ⇒ 报「未知选项」
    ✅ 导入器：两个 main ⇒ 报出来

自测 **6462/6462**（Chunk32 的判据按新语义改过：多文件不再是"必须是警告"）。

## v0.96.376 — VML 工程文件 `.vmk` + `vml make` + Makefile 导入 + `-D`/`-I`

VML 此前只有「把一个源文件丢给编译器」这一种用法。这件事在兼容性战役里已经撞到过
（`docs/老程序兼容性.md`）：`cmatrix` 报的 `VERSION` **不是库缺失**，是 Makefile 里
`-DVERSION="..."` 传进来的 —— 而"把源文件丢给 `vml run`"没有构建系统，这类宏全缺，
**补头文件补不出来**。当时的应急修法是 `VMLTOOL_DEFINE` 环境变量：能用，
但**一个项目的构建描述不该待在环境变量里**。

### 一、先补 `-D` / `-I`（`.vmk` 的前提）

```
vmlcli -D VERSION=1.2 -D DEBUG -I src -I ../inc examples/c/tetris.c
```

两条通路**落点完全不同**，这是最容易做错的地方：

| 选项 | 怎么生效 |
|---|---|
| `-I <dir>` | 加进 `CompileFileWithIncludes` 的 `includePaths` 参数 |
| `-D <名>[=<值>]` | **`cb.SetConfig("defines", list)`**（上游 `Program.Compile.cs:127-149` 那条） |

⚠ **`-D` 不能拿 `VMLTOOL_DEFINE` 凑合**：多数前端的 `PredefinedMacros` 是
`static readonly` + `new(BasePredefinedMacros())`，**静态初始化只跑一次** ⇒
环境变量在"第一次用到该编译器"时就被快照、之后改了不生效；只有 C 因为每次构造都重读
才碰巧是对的。**两套行为不一致，不能建立在"碰巧"上。**

### 二、`.vmk`：只放**项目级**的东西

```xml
<VMLProject Version="1">
  <Name>tetris</Name>
  <Entry>src/tetris.c</Entry>
  <Output Kind="exe" Format="vml">build/tetris.vml</Output>
  <Includes><Dir>src</Dir></Includes>
  <Defines><Define Name="VERSION" Value="1.2"/><Define Name="DEBUG"/></Defines>
</VMLProject>
```

**不放编译器选项**（`Mode`/`Float64`/`GcSections`…）—— 那些归 `vmltool.config.xml`。
同一个值能写两处正是本仓最反复踩的坑。

### 三、⚠ 一条查出来的硬约束，决定了整个格式的形状

**VML 没有跨编译单元的链接。** `CompilerProgramBase` 的多文件模式明确拒绝 `-o`，
只是把每个 `.c` **各编成同名的 `.vml`**，没有符号合并；`CompileFileWithIncludes`
的签名也是单入口的。

⇒ **`<Entry>` 是单数**。写成 `SRCS` 列表会**许下一个做不到的承诺**。
多文件靠 `#include`（老 C 的单 TU 写法）。

### 四、Makefile 导入：一次性转换

`vmlcli make --import Makefile` 读一遍、生成 `.vmk`，**此后以 `.vmk` 为准**。
（不选"每次构建动态读"：两处真源必然漂移，症状是"改了 Makefile 却不生效"。）

**四条硬约定**：

① **多 `SRCS` 必须报出来** —— 静默取第一个的后果是"编过了、少了半个程序"，
   正是本仓最怕的失败形状。报的时候连"要它们进来只能改成 `#include`"一起说；
② **两种引号语义相反**：`-DVERSION=\"1.2\"`（转义）⇒ 值是**带引号**的 `"1.2"`
   （老程序拿它当字符串字面量用）；`-DFOO="bar"`（裸引号）⇒ 值是 `bar`（shell 吃掉了）。
   处理顺序反了会得到光秃秃的 `1.2`，编译时报错位置离 `-D` 十万八千里；
③ **拒绝而不是糊弄**：纯转发式（`$(MAKE) -C sub`）与 IDE 生成的 `.mk`
   （`-include` 递归依赖）—— **报错说明原因**，不导出空清单；
④ 变量替换只做**一层**（`$(VAR)`、`$(SRCS:.c=.o)`）。

### 五、为"以后加更多格式"定的形状

用户点明 `.vmk` 是**枢纽格式**：以后会有更多构建格式导入进来（CMake/MSBuild/…），
也要能出多种产物。所以这一版先把**扩展点**留出来：

* **导入侧**：`IProjectImporter` + `ProjectImporters` 注册表 ——
  加一种格式 = **加一个类 + 注册一行**，CLI 与手机端都不用动；
* **产物侧**：`<Output Kind="exe|lib" Format="…">`。
  已实现 `vml` / `vmb`；`bin`/`rom`/`elf`/`hex`/`s19`/`exe`/`dll`/`class` 是**预留的名字**
  （转译后端在 `VMLTranslators/` 里已有，还没接过来）。
  ⚠ 认得出名字但没实现的 ⇒ 报**「还没做」**；压根不认识的 ⇒ 报**「不认得」**——
  两句**故意分开**（前者是排期、后者多半是拼错）。**绝不静默退回 `.vml`**
  （用户会以为转译完了，手上却是个烧不进芯片的文件）。

### 六、⚠ 落点：三个文件放 `UI/Shared/`，不是 `third_party/vml/VMLTool/`

桌面（`WayCoder`）**不引用** VMLTool（那会把 18 个后端翻译器一起拖进来），
而 `UI/Shared` 是**桌面自测 / `scripts/vmlcli` / 手机端三方都能编到**的唯一目录
（`vmlcli` 用 `<Compile Include="../../WayCoder/UI/Shared/…">` 直接编同一份源码）。
放 VMLTool 里就只有 CLI 用得上，另外两边各得再抄一份 —— 本仓头号坑。

顺带：跨平台路径不再自己写一份，改调 `PathText.Normalize`（同目录、带自测）。

### 七、顺带确认两条**已经过期**的结论

* **`NormalizeLongConstants` 那个绕行可以退休了** —— 它是给"`ToVmbBytes()` 数据段
  不认 `long`"打的补丁，而缺口**已在 `VMLProgram.cs:1247` 修掉**（注释还留着
  "原来这里直接抛"）。桌面 `make` 出 `.vmb` 时**刻意不带它**，等于每次都在反证那个修复；
* **「`.vmb` 读回来这一半还断着」也不成立了** —— 实测编译器产物（49031 条指令）
  写出的 `.vmb` 能装载并**正确运行**（打出 `VERSION=1.2` / `ver.h=ok`）。

顺带补了 `vmlcli` 的 **`.vmb` 直通**（`MauiVml.Run` 一直是认的，桌面不认 ——
桌面/手机流水线的又一处缺口，与之前补的 `.vml` 同源）。

### 八、验收

桌面端到端 + **四条反证**（闸门要能响）：

    ✅ make --import → .vmk；make → .vml
    ✅ 产物与直接编译（`-D`/`-I` 同一组参数）**逐字节相同**
    ✅ 多源文件 ⇒ 报「只取了含 main 的那个，其余不会进来」
    ✅ 转发式 Makefile ⇒ 拒绝并说明原因
    ✅ .vmk 元素名拼错 ⇒ 报错（不是静默忽略）
    ✅ 入口文件不存在 ⇒ 报错
    ✅ Format="hex" ⇒ 报「还没做」；Format="vml2" ⇒ 报「不认得」

自测 **6460/6460**（新增 `TestChunk32` 41 条）；C 探针 41/0/4（已知红不变）。

## v0.96.375 — 拿 6 个真程序压 BGI 垫层，撞出**两条静默的前端缺陷**

用户要「下载一些用 graphics 库的老程序，让它在手机上跑起来」。BGI → `ui_*` 的转接
**早就实现了**（`Lib/c/graphics.h`，373 行，把 BGI 的函数名原样接到 `ui_*`，
`initgraph` 落到**电脑屏窗口**）。所以这一轮的价值全在**拿真程序压它**：

从 `ullaskunder3/Solution-to-graphics.h` 取了 6 个（饼图 / 柱状图 / 笑脸 / 小屋 / 同心圆 /
图形函数集），**全部编译失败** —— 而失败的原因**两条都不在 BGI 垫层里**。

### 一、⚠ `#include<graphics.h>` 写成没有空格 ⇒ **整行被静默丢掉**

    #include<graphics.h>                      ← 这一行整个消失（不报错、不警告）
    int main(){ int gd = DETECT, gm; … }      ← 于是 DETECT 未声明

报的是「未声明的变量 'DETECT'」，位置指着 `main` 里那一行 ——
**没有任何线索指向 include**，会去查 DETECT 怎么没定义。

根因在 `ProcessDirective`：指令名是按**空白**切的，不是按**标识符**扫的。

    "#include<graphics.h>".Substring(1).Trim().Split(new[]{' ','\t'}, 2)
      ⇒ ["include<graphics.h>"]      ← 没有空白 ⇒ 整行一段
    dir = "include<graphics.h>"      ⇒ 匹配不上任何 case ⇒ 丢掉

同一族的 `#if(x)` / `#define(x,y)` 一起中招。**22 门语言全走这条路**，
而且**两条预处理器实现**（`CompilerBase/` 与 `CCompiler/`）都得改 —— 两处都改了。

⚠ **为什么一直没暴露**：`#include<stdio.h>` 无空格看不出问题（stdio 本来就被自动提供，
丢一行 include 也照样能编），**把 bug 盖住了**。所以判据
`cases/46-include-no-space.c` **刻意不用 stdio**，改用 `graphics.h`（它不在自动提供的集合里）。

⚠ 而实测 **6 个下载来的程序全都写的是 `#include<graphics.h>`** —— 老代码里这个写法很常见。

### 二、C++ 前端不认**省略返回类型**的函数定义（`main() { … }`）

`smile.cpp` 开头就是 `main()`（Turbo C 时代遍地都是）。C 前端**本来认**
（`Parser.Declarations.cs` 的隐式 int 分支），C++ 前端不认 —— 报
「期望 IDENTIFIER，实际得到 LPAREN ('(')」，位置指着 `main` 后面那个 `(`，
**看不出是"少写了返回类型"**。

根因：`ParseDeclarationCore` 认出 `IDENT (` 之后调 `ParseFunction()` 时
**没把已经读到的名字传下去** ⇒ 它自己去 `ParseType()`（把 `main` 当类型名吃掉）
再 `Expect(IDENTIFIER)` 撞上 `(`。修法是照 C 前端的语义取 **C89 的「隐式 int」**，
并把光标停在 `(` 之后直接进函数体（`Match(LPAREN)` 已经吃过 `(`，再回退会让它要第二次）。

### 三、补齐 `graphics.h` 缺的东西（真程序压出来的缺口）

| 补什么 | 为什么 |
|---|---|
| `NULL` | 老程序普遍只 `#include <graphics.h>` 就拿它当空指针用（Turbo C 时代由 BGI 头间接带进来） |
| `DEFAULT_FONT`…`BOLD_FONT` 11 个字体常量 | `settextstyle(SANS_SERIF_FONT, HORIZ_DIR, 2)` 极常见，不收常量整份源码一个字都编不过 |
| `arc` / `pieslice` / `sector` | 饼图靠它们（语料里 `pieslice` 用了 3 次、`arc` 2 次）。走**另一套**刷子接口 `ui_set_fill`/`ui_set_pen` + `ui_draw_pie`，因为颜色当参数的老接口画不了扇形。⚠ `ui_set_fill(0)` = 不填充（`arc` 的轮廓靠它），且**画完必须复位**（刷子是全局状态，不复位会串给后面的图元） |
| `initwindow(w,h,title)` | WinBGIm 的入口，语义就是"开一个指定大小的图形窗口" |

### 结果

    ✅ 6 个里 4 个编译干净：barChart / Concentric / pie / smile
    ❌ 剩下 2 个卡在**同一族能力**上：floodfill(10 次) + getimage/putimage/imagesize/XOR_PUT
       —— 都是**像素读回 / 位图块**。场景是保留模式的，要做得上宿主侧光栅化读回，
       那是**架构决定**不是补个垫层（`graphics.h` 的注释里本来就写着这几个不做）。

自测 **6419/6419**；C 探针 **41/0/4**（已知红不变）+ 新增 `46-include-no-space.c`。

## v0.96.374 — 新指令的**汇编级单元测试**（号段 113–125）+ vmlcli 直通 `.vml`

今天往 ISA 里加了 **13 条无符号指令**（`ZEXTL` / `DIVU` / `MODU` / `SHRU` /
`DIVUL` / `MODUL` / `SHRUL` / `CMPU` / `CMPUL` / `JA` / `JB` / `JAE` / `JBE`，
号段 113–125）。`scripts/check-asm-doc.sh` 管住了"文档得提一嘴"，
但**语义一条判据都没有** —— 这一版补上。

### 一、`scripts/vml-asm-probe/`（新）—— 用例是**手写的 `.vml`**

与 `vml-c-probe` 的分工：那边的用例是 **C 程序**，压的是"前端 + 汇编器 + 运行时"
**整条链**，拿到红不容易判断是哪一层坏的。这一套的用例是**裸汇编**，中间没有前端 ——
**一条红就只能是汇编器/VM**，定位一步到位。

（前端那边"`unsigned` 会不会正确选到 `DIVU`"是**另一条判据**，两边都要有，别互相顶替。）

| 用例 | 覆盖 |
|---|---|
| `01-unsigned-div-mod.vml` | `DIVU` `MODU` |
| `02-unsigned-shift.vml` | `SHRU` |
| `03-unsigned-cmp-jump.vml` | `CMPU` `JA` `JB` `JAE` `JBE` |
| `04-zextl.vml` | `ZEXTL` |
| `05-unsigned-64.vml` | `DIVUL` `MODUL` `SHRUL` `CMPUL` |

**挑值的原则：挑有符号与无符号结果不同的**。挑 `100/7` 那种两边同值的，
把 `DIVU` 接回 `DIV` 也照样绿，判据就是白写的。例如：

* `DIVU(0xFFFFFFFF, 2)` 无符号 = 2147483647 / 有符号 = **0**
* `SHRU(0xFFFFFFFF, 31)` 逻辑 = 1 / 算术 = **-1**
* `CMPU(1, 0xFFFFFFFF)` 无符号 1 < 大数 **真** / 有符号 1 < -1 **假**（结论正好相反）
* `MODUL(全1, 10)` 无符号 = 5 / 有符号 = **-1**

⚠ 64 位那三条**必须挪够位数才有判别力**：`SHRUL` 挪 1 位时逻辑与算术的**低 32 位
恰好相同**（符号位还在高半截里）—— 这条是被实测逼出来的，第一版差点写错。

### 二、反证：把 `DIVU` 改回有符号，闸门当场响

按本仓规矩验证"闸门真的有牙"：临时把 `ExecuteDivU` 改成 `ia / (int)b`，重跑 ——

    实得 [0 0 8 0 14 2]     期望 [2147483647 0 8 429496729 14 2]

两个除法格**精确地**变成有符号的结果（-1/2 = 0），而 `MODU` 那几格不受影响
（只动了一个方法）⇒ 判据既灵敏又能定位。随后已还原。

### 三、⚠ 写用例时踩的坑：**标志位是全局的，只能活到下一句**

第一版把四条跳转串在一次 `CMPU` 后面，中间用 `ADD R8 #1` 记"跳了没有"——
结果 `JBE` 那一格**永远是 0**。根因**不在实现**：`ADD` 自己也会置标志，
它把 `CMPU` 刚写的 `zf` 冲掉了。而 `JA`/`JB` 不受影响是因为它们只读 `uf`，`ADD` 不碰它 ——
于是"三条绿、一条红"，看着像 `JBE` 有问题。

⇒ **每条跳转前面都要重新 `CMPU` 一次**。这不是啰嗦：跳转本来就该在"刚比较完"的
状态下评，中间隔着别的指令去考它，考的是别的东西。这条已写进用例注释。

### 四、配套：vmlcli 现在直通 `.vml`

跑裸汇编的前提是 CLI 得认它 —— 而 `scripts/vmlcli` **之前不认**（只认那 22 个前端
扩展名，`foo.vml` 报"认不出这个扩展名"），**手机端的 `MauiVml.Run` 却一直是认的**。
这是"桌面/手机流水线等价"目标下的一个**缺口**，不是顺手改进：补上它，
两边才对得上。

派发规则照抄 `MauiVml.Run`（上游 `VMLTool/Program.Compile.cs:34` 也是这么分的），
实现也**必须是同一份** —— `new VmlAssembler().Assemble(text)`，
**不解析 `.include`、不链标准库**（不是 C 那条 `AssembleWithIncludes` + `LinkLibraries`）。

## v0.96.373 — 取文件名必须**正反斜杠都认**（一条只在 macOS/Linux 上红的自测）

拉取最新代码后跑自测：**6399/6400**，红的是这一条 ——

    ❌ 那条警告锚在 #include 那一行（6 行）

它在 Windows 上**全绿**，在 macOS/Linux 上**必红**。根因不在那条用例，在
`VmlDiagnostics.FileNameOf` 用了 **`Path.GetFileName`**。

### 一、`Path.GetFileName` 按**当前平台**的分隔符切

Windows 上 `\` 与 `/` 都算，**Unix 上只有 `/` 算**。而这里处理的是**编译器吐出来的路径**，
形态由**产出它的那台机器**决定，不由我们运行在哪台机器决定：

    Path.GetFileName(@"D:\proj\main.cpp")
      Windows → "main.cpp"
      macOS   → "D:\proj\main.cpp"   ← 原样返回

于是 `IsSameFile` 判成"这是**别的文件**的诊断" ⇒ **行锚丢掉**（`Line = 0`，
编辑器里不再画箭头）。气泡本身照常出现，所以从界面上看只是"位置没了"，
**根本联想不到分隔符**。

⚠ 同一组用例里另外两条反斜杠的用例**互相抵消**看不出来（两边都是反斜杠形态，
`FileNameOf` 对两边返回同一个字符串 ⇒ 判等成立），出问题的正是
「**一边反斜杠、一边相对名**」那一条。这也是它藏得住的原因。

### 二、收敛到 `UI/Shared/PathText`（新）

这条规则在仓里已经散着写过三处，其中一处（`ToolDisplay`）是对的、另两处各写一半：

| 处 | 原来 | 现在 |
|---|---|---|
| `VmlDiagnostics.FileNameOf` | `Path.GetFileName` ← **错** | `PathText.FileNameOf` |
| `ToolDisplay.ShortPath` | 自己写 `Math.Max(LastIndexOf('/'), LastIndexOf('\\'))` | 同上（那份写法是对的，收敛进来） |
| `FileIgnoreManager.IsIgnored` | `Split('/')`（先 `Replace` 归一化） | `PathText.Segments` |
| `SessionManager.NormalizeSessionId` | 自己 `Replace('\\','/').Split('/')` | 不动（那份本来就对，且带安全语义） |

`PathText` 放 **`UI/Shared/`**（MAUI 也编译那一份，见本仓跨端纯逻辑的既定要求），
提供三件事：`Normalize` / `FileNameOf` / `Segments` / `IsAbsoluteShaped`。

⚠ **判据是"这个路径从哪来"，不是"哪个 API 更保险"**：本进程自己
`Directory.GetFiles` 出来的路径用 `System.IO.Path` 是对的（分隔符一定与平台一致），
不必为了统一去改那些地方 —— 那个类注释里写清楚了这条分界。

### 三、`TestChunk31`：把规则钉住（+19 条判据）

两层都要：

* **本组**直接钉 `PathText` 自己的契约（两种分隔符、空段、绝对路径形态、以分隔符结尾不返空串）；
* **端到端**再钉一次（`VmlDiagnostics.Parse` 的 Windows 形态路径仍锚在第 6 行）。

只钉低层的话，"调用点又绕回 `Path.GetFileName`"抓不到；只钉端到端的话，出了红
也定位不到是哪条规则错。

⚠ 这一组**必须同时在 macOS/Linux 与 Windows 上跑**才有意义 —— 反斜杠那几条在
Windows 上**用 `Path.GetFileName` 也能过**（那边 `\` 本就是分隔符），
所以"在 Windows 上全绿"证明不了任何事。

自测 **6400/6400**。

## v0.96.372 — **命令行参数**：三层落地（宿主喂 / VM 两个新号 / C 绑定）

勘察时的事实很干脆：**只有 C/C++ 前端认识 `argv`**（`grep -rn argv VMLPrepares/*/` 只命中
这两门），其余 20 门一个字都没有；而运行时的 `CommandLineArgs` 一直是空的 ——
**没有任何宿主往里放过东西**。参数这条路上"能传"与"怎么取"两头都断着。
完整设计见 `docs/命令行参数.md`。

### 一、VM：两个新号（+ 白名单）

| 号 | 名字 | 调用 | 返回 |
|---|---|---|---|
| **62** | `ArgCount` | 无参 | R0 = 参数个数（**含 `argv[0]`**，恒 ≥ 1） |
| **63** | `ArgGet` | R0=序号, R1=缓冲区, R2=容量 | R0 = **写入字节数**（不含 NUL）；越界/缓冲区无效 = **-1** |

按铁律「新能力一律走新号」（不给 `#54`/`#59` 加参数）；两条都登记进 `UserAllowed`
（漏了会 `Permission denied`，表现为"取到的参数全是垃圾"）。**与特权级无关** ——
参数是 ABI/环境的事，不是内核能力。

**唯一真源**：`_argvStrings` 在 `LoadProgram` 里铺好，**入口帧与 `#62`/`#63` 都读它**；
宿主一个参数都不给时也补一个 `argv[0]`（C 保证 `argc >= 1`，老程序常拿它打用法）。

⚠ **设计时改过一次**：`#63` 原打算"返回字符串地址"（与 `#59 GetInfo` 同形），
真动手时改成**写进调用方缓冲区** —— 返回地址会把**拷贝逻辑摊到 22 门语言的绑定里**
（22 份拷贝循环 + 各自处理越界/截断），而缓冲区形式是**运行时一处实现、绑定都只是一行
syscall**，且与现成的 `ui_store_get`/`ui_call_json` 同一套（宿主本来就没有能"交还"的堆）。
本仓头号坑正是"同一规则两处实现"，这里从一开始就避开。

### 二、宿主：`vmlcli --arg`

`vmlcli prog.c --arg -l --arg foo` —— **可重复**、**刻意不做引号解析**（本仓踩过
"按空白切分、路径带空格就断成两截"，见 `ShellCommandRegistry.Split`）：要带空格的参数
就整段当一个值给。`argv[0]` 由我们补成**源文件名**（C 语义：`argv[0]` 是程序名）。

### 三、C 绑定：`ui_argc()` / `ui_arg(i, buf, cap)`

`Lib/shared/src/vmlui.c` + `Lib/c/waycoder_ui.h`，GenLib 重生成（108 编译 / 0 跳过，
diff 只落这两处 + `Lib/shared/vmlui.vml`，**没有外溢到其它语言的绑定**）。
给 `int main(void)` 的程序用（有 `main(int argc, char **argv)` 的不需要，入口帧直接给），
也是其余 21 门语言绑定照抄的模板。返回写入长度；越界 -1；容量不足**截断并补 NUL**。

### 四、判据

- runner 新增 `// ARGS:`（照 `// STDIN:` 的写法，锚行首、按空白切分）；
- **`cases/45-argv-pass.c`**：**两条路取到同一份**（`ui_argc()` 与 `argc` 相等、
  逐个 `ui_arg` 与 `argv[i]` 用 `strcmp` 比）+ `argv[0]` 是程序名 + 越界 `-1` +
  容量不足截断补 NUL。
  ⚠ 第一版把"截断"期望写成 `-l`（2 字节）在 cap=4 下会返回 3 —— 实际上**截不到**，
  修的是**期望**不是代码。
- 全量 C 探针 **39 通过 / 2 失败 / 4 已知红**（失败仍是既有的 29/30）；`vml-abi-probe` 7/7；
  `Lib/` 重生成零漂移（除本次新增的 `ui_arg*`）。

### 五、同批：`vml_lib.zip` 重建

`Lib/` 变了就必须重跑 `scripts/make-vml-lib.sh` —— 脚本自身报了一句要紧的话：
**签入的 `vml_lib.zip` 与仓库 `Lib/` 原本已经不一致**（这段时间手机上跑的是旧标准库，
而桌面不读这个包 ⇒ 桌面看不出来）。已一并重建并随本版提交。

### 仍未做（下一步）

- 手机命令行页的 `vml run prog.c -l foo` 透传（多出来的 token 当参数）；
- **22 门语言的绑定**（`sys.argv` / `os.Args` / `ARGV` / `process.argv` / `arg` 表 /
  `ParamStr(i)` / `COMMAND$`…），按老程序里的常见度，先做 **Python / Lua / JS**。

---

## v0.96.371 — 老程序兼容：`sl` **出火车** —— 入口帧 / 栈堆 / 结构体数组 连着八处

`sl` 从"画不出火车"到"**全程动画**"（42KB 输出、含烟圈），中间是**八处各自独立**的缺陷。
它们的共同点：**编译全绿、跑起来才错**，而且大多表现为"读到一段字符串正文当地址用"
（崩在 `0x20202020` 这类地址上 —— 那正好是四个空格）。

### 一、装载侧：`object[]` 里的标签**要延后解析**（元素恒 NULL）

数据段是按**字典遍历序**逐个 `AllocateMemory` 并在**循环末尾**才登记的，而 `object[]`
元素指向的标签（指针表里的字符串）完全可能排在**后面**才登记 ⇒ 边遍历边解析就读到 0。
单个 `LabelRef`（`T *p = &x;`）本来就有"延后补填"，**数组元素这条路没有**。
指纹：**同一个形状放函数里就坏、放顶层就好**（顶层那份恰好顺序凑巧）。

### 二、死代码消除：`DataRefs` 少了两种形态

判"谁被引用"时只认 `object[]` 里的**字符串**，于是
（a）文本往返一趟之后元素变成 `LabelRef`、（b）单个 `LabelRef` 值
—— 两种都会被当"没人引用"删掉。症状极隐蔽：数据段里 `.word L_x` **照旧原样输出**，
只是 `L_x` 退化成 `.text` 里的一个**空标签**（"数据都在"、内容是空的）。
用户侧后果：**手机「VML 编译」出来的 `.vml` 跑起来指针全 NULL**（那条路正是
`ToString()` → 存盘 → 独立汇编运行）。

### 三、局部声明的数组维度是**第二套判据**（表达式维度 ⇒ 初始化器整个丢掉）

`Parser.Expressions` 用 `TryConstDim` 折常量，而 `Parser.Statements`（局部声明）
只把「**NUMBER 紧跟 `]`**」当编译期维度、其余全当 VLA —— 于是 `[H + 1]`、`[2 * 5]`
被判成运行时维度，顺着 VLA 那条路走下去，而 `GenerateVariableDecl` 的 VLA 分支
**第一句就 return**（"VLA 不支持初始化器"）⇒ 初始化器被丢掉、只剩预扫描留下的
`.word 0`：编译不报错、元素全读到 0。同一个表达式写在**顶层**或**非 static 局部**上
却正常 —— 「同一个表达式换个位置就坏」正是**两套判据**的指纹。已统一到 `TryConstDim`。

### 四、入口**没给 `main` 搭帧**（`argc` 读出 8 亿）

C 的约定里 `call` 把**返回地址**压栈、参数落在它**之上**（`[R12+12]`/`[R12+16]`……），
而运行时那段是 `if (privilegeLevel == 0 && CommandLineArgs.Count > 0)`：
`privilegeLevel` **默认 1** ⇒ **整段从来没执行过**；就算执行也只在有参数时压、且
**少压了返回地址槽**。实测 `sl` 的 `argc` = **897988541**（0x358637BD，某个浮点常量
`1e-6` 的位模式）⇒ `for (i = 1; i < argc; ++i)` 一进循环就崩。
现在**无条件**搭三格：argv、argc、返回地址（RA 给 **0** —— `ExecuteRet` 把 `<= 0`
当"从入口返回"⇒ 结束程序、退出码取 R0，正是 `main` 正常 `return` 的语义）。

### 五、栈顶落在**堆**里（数据段一大就撞上）

栈从 `sp` 往下长、堆从 `DataBase` 往上长，而 `sp` 取自 `config.StackSize` = **64K**
（`.stack 0` 就是 C 前端默认发的），运行时**从不检查堆有没有越过它**。
实测 `sl`：`heapTop=67342 > sp=65536` ⇒ `main` 的参数槽（0x10004）正好落在**堆的数据**上
（缺陷四之所以表现为"读到某个浮点常量"，就是它读到了堆里的浮点常量）。
现在 `if (sp <= memoryAllocPtr) sp = memory.Length - 4`。

### 六、结构体数组的数据段按 **4 字节/元素**开

`dataSection[name] = new int[N]`：对 `int` 恰好对，对结构体数组就**只有 1/4 大** ——
`struct smokes S[8]`（元素 16 字节）只开 32 字节，写 `S[7].y` 就落到**紧邻的数据**上。
最小复现：`for (i=0;i<3;++i) S[i].y = i*10;` 之后第一个 `printf` 打出**制表符**
（把后面的格式串踩了）。新增 `AllocArrayData` 按真实元素宽度开；
⚠ `char`/`short` **仍保持 4 字节/元素**（既有行为，`Examples/` 里一批 `char buf[N]`
靠它偷余量，缩到真实宽度会把"静默容忍"变成"真越界"）。

### 七、函数内定义的**结构体类型没登记** ⇒ 元素步长成了 4

局部声明那条路遇到 `struct 标签 { … }` 时**只按 `depth` 把成员体跳过去**，
于是类型表里查不到它：`S[i]` 的步长回落到 **4**（应 16）⇒ `S[0].kind` 压在被当作
`S[1].ptrn` 的那 4 字节上，读出来的字段是**别的元素的值**（最小复现：`S[1]` 打出
`400 400 400 400`）。现在真解析成员表（复用 `ParseAnonStructBody`）并登记，
匿名的合成标签、变量类型串一并改成 `struct <标签>`。

### 八、`char *p = "字面量"` 的全局声明存的是**正文**不是地址

`dataSection[name] = strLiteral.Value` 不分数组/指针：数组要的正是正文，
**指针要的是地址** ⇒ `static char *msg = "INTACT";` 里躺着 `0x41544E49`（"INTA"）
当指针用 ⇒ 崩在取址那一步。现在按 `*` 分路（指针那条先给正文分配标签、变量记 `LabelRef`，
与 `T *p = &x;` 同一条通路）。

### 判据（用户要求：**每修一处都要有常驻判据**）

| 缺陷 | 判据 |
|---|---|
| ① 装载延后解析 | `cases/34` 的 `STATIC=1`/`SD=3` |
| ② `DataRefs` 两种形态 | `cases/34` 的 **`EXPECT-VML: "sa"`**（产物文本必须有那段正文）、`cases/44` 的 `MSG=[INTACT]` |
| ③ 维度判据统一 | `cases/34` 的 `MACRO=1`/`EXPR=1` |
| ④⑤ 入口帧 + 栈堆 | `cases/43`（`ARGC=1`/`A0=1`/`ANULL=1`/`BIG=1`/`LOOP=1`；带 70000 字节数据触发堆越界） |
| ⑥⑦⑧ 结构体数组 | `cases/44`（文件域对照 `GO` + 函数内联 `SI` + 匿名体 `AN` + 邻居字符串 `MSG`） |

- **新增 `// EXPECT-VML: <子串>` 判据**（`run.sh`）：唯一一条**不跑程序、只看产物**的断言 ——
  "只被别的数据引用的东西被死代码消除删掉"这件事**运行路径跑一百遍也照不出来**
  （`.word L_x` 照旧输出、只是 `L_x` 变成空标签）。已用"把修复退回去"反证过它会红。
  ⚠ 断言要用**只有 static 表才有的字面量**（`"sa"`）：非 static 那两条字符串是被
  **指令**引用的（`MOVE R0 L_x` 后存栈），死代码消除删不到它，拿它断言**修前修后都绿**。
- `cases/34` 由 KNOWN-RED **转绿**并重写（去掉 KNOWN-RED 标记、补 `SD`/`EXPECT-VML`）。
- 全量 C 探针 **38 通过 / 2 失败 / 4 已知红**（两个失败仍是既有的 29/30）；
  `vml-abi-probe` 7/7；**`Lib/` 全量重生成零漂移**（108 编译/0 跳过 ⇒ 这批修复不改库产物）；
  `sl` 全程动画（160KB 输出、0 崩溃）、`tty-clock` 照常出钟。
  ⚠ 体检脚本给 tty-clock 的 5 秒超时**不够**（光编译就 4.8 秒）—— 别把它当成回归。

> ⚠ 本版仍**没有**重打 APK（与 v0.96.368/369/370 同一批）。
> **改动面**：`VMLPrepares/CCompiler/{Parser.Statements,Parser.Types,CodeGenerator.Functions,CodeGenerator.Expressions.Types}`、
> `VMLAssembler/VMLProgram.cs`、`VMLRuntime/VMLRuntime.cs`、`scripts/vml-c-probe/{run.sh,cases/34,43,44}`。

---

## v0.96.370 — `char`/`short` 数组：**数据段按真实宽度打包**（存储与下标终于同源）

`char t[300] = {…}` 的 `t[100]` 读出 **25** —— 100/4。原因是数据段给**每个**数组元素都发一个
`.word`（4 字节），而下标的步长按元素真实宽度走（`char`=1、`short`=2）。两边只在 `int`（4）上
重合，所以 **`int`/`bool` 一直是对的、`char`/`short` 才露**。`sizeof` 报的一直是真实宽度
（`sizeof(char[3]) == 3`）⇒ 分配与访问自相矛盾，**不是"故意按 4 字节模型"**。

受害者是**字节表**：`Lib/shared/src/graphics.c` 的
`static const unsigned char font8x16[1520]`（字模点阵）按 `font8x16[char_offset + row]` 取值 ——
今天读到的是**第 k 个字节**，所以字模是**乱的**；顺带它一直占着 6080 字节（应为 1520）。

### 一、修法：五层都补上"1/2 字节"这一档，而宽度**只有一个真源**

| 层 | 改动 |
|---|---|
| C 前端 | 新增 `ArrayElemSize(type)`（**唯一真源**）+ `BuildArrayData`；**存储与下标共用它** |
| 文本 | `VmlProgram.ToString` 发 `.byte` / `.halfword`（等值数组压成 `.byte[N] v`） |
| 汇编器 | `.byte`/`.halfword` **按指令名**折成 `byte[]`/`short[]`；紧凑写法与多值写法都认宽度 |
| VMB | 新 tag `0x41`（紧凑字节载荷）/`0x42`（int16）；**必须排在 `IList` 那支之前** |
| 装载 | `byte[]`/`short[]` 按真实宽度 `AllocateMemory` 与铺字节 |

⚠ **三条判据纪律（都踩过）**：

1. **宽度必须实测，不能按类型名推断**：`GetTypeSize` 说 `Bool` = 1，而**实际步长是 4**
   （本编译器的 bool 是 **4 字节模型**：`sizeof(bool[4]) == 16`、`&b[1]-&b == 4`，两边自洽）。
   照 `GetTypeSize` 打包会把 bool 数组弄坏 ⇒ `ArrayElemSize` 里 bool 显式写 4，
   并由 `cases/39` 的 bool 断言钉住。（数值来自 `.scratch/_stride.c` 的 `&a[1]-&a` 实测。）
2. **只打包"带初始化器"的数组**：没有初始化器的 `char buf[256]`（`Examples/c/` 里一批字符串暂存）
   今天已经是"存储 4N 字节 / 按字节访问"自洽的，把存储缩到 N 会把"偷用那 4 倍余量"从
   **静默容忍**变成**真越界**，而收益是零 —— 不动。
3. **判据里的期望值自己也会错**：`cases/39` 我第一版把 `bs[299]` 期望成 39，而那张表只写了
   **296 个初值**、数组声明的是 **300** ⇒ 后 4 个应**零填充**。`B295=39|B299=0` 才对 ——
   **修的是期望、不是代码**（顺带把"初值少于声明尺寸"这条路径也钉住了）。

### 二、同批修好：**浮点数组整表读到 0**（三角函数的零表）

`float` 数组的初始化器里是 `double` 字面量，落文本成 `.word 0.0174…`（一个十进制），
而装载侧 `ResolveDataElement` **不认 `double`** ⇒ 每个元素都 `return 0`。
受害者是 `Lib/shared/src/math.c` 的 `static const float sin_table[361]` ——
**三角函数表一直是零表**：`sin_deg` / `cos_deg` / `tan_deg` 全返回 0，编译链接全绿、算出来是 0。

修法**只动一处**：元素存成**位模式的 int**（4 字节槽正好装一个 `float`），
而读取端本来就用 `MOVEF` 解释那 4 字节（`ResolveDataElement` 对 `float` 也走
`SingleToInt32Bits`）⇒ 位模式原样过去就对了，五层一层都不用改。
判据 `cases/41`（实测 `sin_deg(30)==0.5`、`sin_deg(90)==1`、`cos_deg(60)==0.5`，
修前全是 0）。

⚠ `char`/`short` 那条与这条是**两件不同的事**（一个是"宽度与步长不一致"、
一个是"浮点字面量的类型在数据段里丢了"），所以分开压两条判据，别混。
`double` 数组**没修**（元素 8 字节，而数据段的 `object[]` 通道按 4 字节/元素写死，
要单独一条 8 字节路径）—— 已按 KNOWN-RED 钉在 `cases/42`；全语料**零处**用到它。

### 三、顺带收掉调查中发现的既有静默坑

- `HandleDataDirective` 的**多值写法**（`.byte 1,2,3`）存成 `List<object>` ——
  既不是 `int[]` 也不是 `object[]` ⇒ 文本层写出
  `x: .word System.Collections.Generic.List\`1[System.Object]`、装载侧把 `ToString()` 的结果
  **当字符串写进内存**：编译不报错、跑起来数据全错。现在按指令名折成 `byte[]`/`short[]`。
- `.vmb` 读回的数组同样是 `List<object>` ⇒ 走 `.vmb` 路径进来的数组全落进装载侧最后一个
  `else`（当字符串写）。读回改成 `object[]`。

### 四、判据

- `cases/39-array-elem-width.c`：**KNOWN-RED 转绿**（1 维 / 2 维 / int 对照 / 300 项字节表 +
  零填充）。扩了 300 项那张表是因为"小表可能错得刚好看不出"。
- `scripts/vml-vmb-check`：语料补 `byte[]`/`short[]`，并把 `Bits()` 补上 `byte`/`short`
  —— 原先它们会掉进 `ToString()` 那条，比的是**数值的 ASCII 写法**而不是字节，宽度写错也照样绿。
- 全量 C 探针 **35 通过 / 2 失败 / 5 已知红**（`cases/39` 与 `cases/41` 双双转绿、
  `cases/42` 新增为 KNOWN-RED）；`vml-abi-probe` 7/7；
- **`Lib/` 全量重生成**（`touch Lib/shared/src/*.c` + `GenLib -b`，108 编译/0 跳过）：
  diff 只落在两个模块 —— `math.vml`（`sin_table` 变成位模式）与
  `graphics.vml`（字模 **1520 个 `.word` → 1520 个 `.byte`**，6080 字节缩到 1520）；
- `Examples/c/` 里 7 个用 `char buf[N]` 暂存的（tetris/mario/pacman/calc/plane/starfall/
  draw_colors）逐个编译+运行通过（"不打包无初始化器数组"那个决定站住了）。

> ⚠ 本版仍**没有**重打 APK（与 v0.96.368/369 同一批）。
> **改动面**：`VMLPrepares/CCompiler/`（4 文件）、`VMLAssembler/{VMLAssembler,VMLProgram}.cs`、
> `VMLRuntime/VMLRuntime.cs`、`scripts/vml-vmb-check/Program.cs`、重生成的 `Lib/`。

---

## v0.96.369 — `localtime` 不是"本地"时间：**新增 `#61 GetUtcOffset`**

`tty-clock` 出钟之后（v0.96.368）还有个刺眼的错：**小时差整整 8 小时**（本地 19:11 显示成 11:11），
而分/秒/日期**全对** —— 因为 UTC+8 是**整小时**，只有小时那一项被推掉。这个形态最容易看成
"程序自己算错了"，实际是库里根本没用时区。

### 一、根因：`_ts_to_tm` 是纯 UTC 换算，而 `localtime` 直接转调它

`Lib/shared/src/util.c` 的 `_ts_to_tm` 是把 Unix 时间戳拆成 `struct tm` 的整数算法
（`civil_from_days`），**输出的是 UTC**；而 `gmtime` 当时写的是"本平台无时区，两者相同"
（`return localtime(t);`）⇒ `localtime` **就是** `gmtime`。
`#54 GetDateTime` 给的又确实是**与地区无关**的时间戳（本来就该如此），
所以整条链上没有任何一处知道"本地在哪"。

### 二、修法：不动 `#54`，新增 `#61`

本仓规矩「**新能力一律走新号**」（老号加参数 = 静默的未定义行为：宿主从寄存器读，
而老程序后面那几只寄存器里是它自己上一句留下的值）。于是：

- `#61 GetUtcOffset` → `R0` = 本地时区偏移（秒，东为正）；
- `localtime` 加它、`gmtime` **不加**（两者必须分开，否则"用哪个都一样"把这号的意义抹掉）；
- ⚠ **必须登记进 `SyscallConstants.UserAllowed`** —— 用户态只放行本表的号，
  漏了会打 `Permission denied: syscall 61 requires kernel mode` 并把 R0 置成错误码；
- ⚠ 偏移**必须取整到分钟**：两次读时钟之间会跨毫秒，直取 `.TotalSeconds` 实测得到 **28799**
  （不是 28800）⇒ `localtime` 整体偏 1 秒，而"整小时"的假设全都还成立、**极难看出**
  （第一次的判据里 `tm_sec` 少 1 就是这么来的）；
- 宿主的 `Now - UtcNow` 而不是 `TimeZoneInfo.Local`：前者只依赖平台给的本地时间，
  后者要走时区数据库（`InvariantGlobalization=true` 的宿主上会退化成 UTC ⇒ "修了没生效"）。

宿主实现只有一处（`VMLRuntime.Syscall.cs`），所以桌面 / 手机 / GUI **一起生效**；
库侧经 `util.vml` 对**全部 22 门语言**生效。

### 三、判据 `cases/40-localtime-offset.c`：两个**互相独立**的来源

① `gmtime` 是纯 UTC ⇒ 对一个手算核对过的时间戳（`1790075508` = 2026-09-22 11:11:48 UTC）
断言它的七个字段（与宿主时区无关）；
② **`#56 GetTimeString` 给的是宿主自己的本地时间**（`DateTime.Now`，"HH:mm:ss"）——
拿它的前两位和 `localtime(NULL)` 的小时比。一条走"宿主格式化"、一条走"库的整数算法"，
任何一边坏都会露 —— **这正是"钟面小时差 8 小时"的形状**。
另有 `E`（契约：`localtime(t) == gmtime(t + 偏移)`，逐字段）与 `OFFOK`（偏移是整刻钟、在 ±14 小时内）。

⚠ **两条踩过的判据纪律**：

- `gmtime`/`localtime` **返回同一个静态缓冲**（标准 C 行为）⇒ 想同时比两个结果，
  必须每次调用后**立刻把字段抄进局部变量**。留两个指针会让"逐字段相等"**恒成立** ——
  **判据看起来全绿、其实什么都没测**（本条的第一版就栽在这里）。
- **反向验过**：把 `util.c` 里的偏移去掉，`E=0|HOK=0` —— 正是原 bug 的形状。

验收：`tty-clock` 现在显示**本地**时间（实测本地 19:32:59 ⇒ 钟面 **19:33**），日期行正常。

**判据**：全量 C 探针 **33 通过 / 2 失败 / 5 已知红**（两个失败仍是既有的 29/30）；
`vml-abi-probe` **7/7**；`GenLib -b`/`-A` 重生成与工作区**零差异**。

> ⚠ 本版仍**没有**重打 APK（与 v0.96.368 同一个批次）。
> **改动面**：`VMLRuntime/{SyscallNumber.cs,VMLRuntime.Syscall.cs}`、
> `Lib/shared/src/util.c` + 重生成的 `Lib/shared/util.vml`、`scripts/vml-c-probe/cases/40`、
> `docs/老程序兼容性.md`。

---

## v0.96.368 — `tty-clock` **出钟了**：四层各修一遍（匿名嵌套结构体 / `T a[][N]` / `wbkgdset` / `%F`）

上一版把根因定位到"匿名嵌套结构体当字段被整个丢掉"但**没有修**。这一版修了它，
并一路修到**钟真的画出来**。四层各自独立、**每一层都能单独让画面全空** ——
这正是它难查的原因：修好一层，现象只是从"全空"变成"还是不对"，看着像没修。

### 一、匿名嵌套结构体当成员：**登记成员**，而不是"拍平 + 把名字丢掉"

⚠ **上一版记的位置要更正**。`ttyclock_t` 是 `typedef struct { … }`，走的是
`Parser.Types.cs` 的 `ParseToplevelTypedefStruct`（**不是** `Parser.Structs.cs` 里
`struct 名字 { … }` 那条）。上一版指到的是"**嵌套**匿名"那条跳过分支 —— 那是同族缺陷的深处一层。
定位手段：在 7 个候选分支各挂一个"条件尾部"标记（`&& MarkAnon("x")`，返回 true 不改变语义），
跑一次就看出只有 `Parser.Types:397` 命中，共 3 次（`option`/`geo`/`date`）。

真身：`struct { … } option;` 的内层成员被拍平进外层成员表（偏移按**外层绝对值**算），
而中间的 `option` **从没登记** ⇒ `ttyclock.option.color` 找不到 `option`、基址从 0 起算。
（C 里 `struct { … } option;` 的成员**本就不提升**；只有 C11 的 `struct { … };` 才提升 ——
这两条路必须分开，混了就是"改好一种、弄坏另一种"。）

这段逻辑原先在 **7 处**解析循环里各抄了一遍，**每一处都漏了同一件事**。这一版收成两份共享助手：

- `ParseAnonStructBody(isUnion)` —— 正文**一律按相对偏移**累积
  （这是"相对谁归一化"只做一次的地方）；
- `AttachAnonStructMember(...)` —— 有名字 ⇒ 合成类型名（`_anon_option`）注册成一张独立类型，
  外层只加**一条**成员（偏移 = anonBase）；没名字 ⇒ C11 提升，成员各自偏移 + anonBase。

⚠ **两个分支各自把 base 加一次，不能都加**（"既登记成员、又保留拍平"= 偏移翻倍）。
顺带把嵌套匿名提成**递归**（原来整段跳过 ⇒ 更深一层同样丢成员）。

判据 `cases/36-anon-struct-member.c`（25 条：往返值 + 相对偏移 + C11 提升 + union 成员）。
**反向验过**：把"登记成员"那句关掉，立刻红成 `TAG=44|O1=0|O2=0` —— 正是 bug 的签名。

### 二、`T a[][N]` 第一维省略：数据段只写出**第一行**

`const bool number[][15] = {{…} × 10}`（tty-clock 的数字点阵）编出来的数据段**只有第一行**，
`number[1][k]` 读到的是**隔壁变量**（实测读到字符串区）⇒ 表盘只画得出一个数字。

根因是 `ArraySize == null` **同时**被当成"还没算"和"某一维未知"：
`ParseVariableDecl` 在循环里累乘维度，而 `[]` 那一支**只往 `Dimensions` 加 null、不碰 `ArraySize`**
⇒ `T a[][3]` 是"先 `[]`（还是 null）再见 `3`（判成'还没算'）" ⇒ **ArraySize = 3**（不是整块的元素数）。
**显式写全的 `T a[3][3]` 一直是对的** —— 又是"两种写法只坏一种"。

修法：总元素数用**局部累加**，`[]`/VLA 置 `sizeUnknown`，循环结束才写回（未知就给 null，
让生成器从初始化器推）。判据 `cases/37-mdim-infer-first.c`（int / bool / 字符串表 + 显式维度对照）。

查这条的路径值得记：**先把汇编打出来**（`--vml`）——`.data` 里 `ob:`/`oc:`/`oi:` 各只有 3 个 `.word`、
而 `fb:`（显式维度）有 9 个，一眼就分出了"是数据段少了"还是"是下标算错了"（后者是对的）。

### 三、`wbkgdset` 的背景属性：老程序"用空格 + 底色画图"全靠它

`tty-clock` 的数字**没有字形**：

```c
wbkgdset(win, COLOR_PAIR(1));      // 这一格的底色
mvwaddch(win, y, x, ' ');          // 画一个"没有字形"的色块
```

而 `wbkgdset` 此前是 `(void)w; (void)ch; return 0;`（注释还写着"背景字符：忽略"）
⇒ 每格都按默认属性落位、`refresh` 只发 `[37m[40m` ⇒ **表盘整个是黑的**（不是"颜色不好看"，
是一个字都没有）。链接、运行、`waddch` 的返回值**全都正常**。

**两条一起坏才看不出来**：

① `wbkgdset` 不记属性 —— 新增 `WINDOW.bg` + `sc_write_attr`（低 8 位修饰位留给 `attron`，
   颜色对号由背景给），`waddch`/`waddstr`/`mvwaddch`/`mvwaddstr`/`wprintw`/`mvwprintw`
   **全部改道**经过它（漏一个就是"这个调用点没颜色"）；
② 即便记了，`sc_sgr` 也认不出**默认色** —— 它拿 `pair_fg[pair] >= 0` 当"这个颜色对登记过没有"，
   而 `use_default_colors()` 之后 `-1` 是**合法颜色值**（"用终端默认色"）。
   `init_pair(1, -1, COLOR_GREEN)`（表盘数字正是"默认前景 + 绿底"）会被判成"没登记"、
   整块退回白字黑底 ⇒ 新增 `pair_set[]`，`-1` 发 `39`/`49`。

判据 `cases/38-curses-bkgd.c`：`wbkgdset` 唯一的作用面就是**发出去的 SGR**（本实现没有 `inch()`），
所以这条必须看字节流。为把期望串压到可写，三行都只碰**一行 80 格**（A：只改属性不让行变脏 ⇒
`refresh` 只发属性段；B：79 个 `waddch` + 第 80 格仍是默认，两段 SGR 的交界正好落在列 80；
C：**反例** —— 背景为 0 不许凭空注入颜色）。

### 四、`strftime` 缺 `%F`/`%T`/`%R`

tty-clock 的默认日期格式就是 `"%F"`（`strncpy(option.format, "%F", …)`），
缺了**不报错、只把 `%F` 原样打出来** —— 钟面下面那一行显示成字面的 `%F`。

### 五、结果，与仍未解决的三条

`tty-clock` **出钟了**。验收方式：跑 25 秒、把原始 stdout 回放成帧
（`sc_ch` 是字节缓冲，数字是"空格 + 底色"⇒ 必须按**背景色**回放才看得见），
取"墨迹最多"的那一帧：四个数字的位图逐格与 `number[]` 对上
（`1 1 : 0 9`，72 格绿底），日期行正常渲染（字面 `%F` 出现 **0** 次）。

仍未解决（都已量清、各自独立，**都留了判据或记录**）：

1. **`localtime` 返回 UTC** —— `util.c` 的 `_ts_to_tm` 是纯 UTC 换算（注释就写着"本平台无时区"）
   ⇒ 钟面小时差 8 小时（分/秒/日期都对，因为 UTC+8 是整小时）。
   修它要新增一个"本地时区偏移"的 syscall —— 按本仓规矩（**新能力一律走新号**，
   不动老号语义），另开一轮。
2. **`char`/`short` 数组的元素宽度两边不一致** —— 数据段给每个元素发一个 `.word`（4 字节），
   而下标步长按真实宽度走（1/2）；`int`/`bool` 恰好两边都是 4 才一直看不出来。
   1 维、2 维都中。已按 **KNOWN-RED** 钉住：`cases/39-array-elem-width.c`。
   修法要先定"数据段数组元素的宽度"这条规则（改它要重跑全部 22 门语言的判据），故不塞在本轮。
3. 一处**既有**问题（不是本轮引入 —— 撤掉本轮全部改动后现象一字不差）：
   program 侧 `指针->字段` 的成员偏移不对（实测 `stdscr->cols` 读出来是 `attr` 的值、
   经 `wbkgdset` 写的 `bg` 从 program 侧读回恒 0）。库侧自洽，所以不影响 tty-clock。

**判据**：全量 C 探针 **32 通过 / 2 失败 / 5 已知红**
（两个失败是既有的 29/30；已知红 = 既有的 12/27/28/34 + 本轮新增的 39）；
`GenLib -b` 重生成与工作区**零差异**（0 编译 / 108 跳过 ⇒ 没有"改了源码忘了重生成"）；
**`vml-abi-probe` 7/7 通过**（调用约定无回归）。

> ⚠ 本版**没有**重打 APK：手机上 `tty-clock` 要等 `localtime` 那条也解决了一起上。
> **改动面**：`third_party/vml/VMLPrepares/CCompiler/`（4 文件）、
> `third_party/vml/Lib/shared/src/{curses.c,util.c}` + `Lib/c/curses.h` + 重生成的
> `Lib/shared/{curses.vml,util.vml}`、`scripts/vml-c-probe/cases/36-39`。

---

## v0.96.367 — `tty-clock` 画面空：根因是**匿名嵌套结构体当字段时被解析器整个丢掉**

### 一、根因（已定位到行，**尚未修**）

`Parser.Structs.cs:322-329` 的「嵌套匿名 struct/union」分支：

```csharp
if (Current().Type == LBRACE && (innerType.Type == STRUCT || UNION))
{
    Advance(); …跳到匹配的 RBRACE…
    Expect(RBRACE); while (STAR) Advance();
    if (Current().Type == IDENTIFIER) Advance();   // ← 把成员名（option / geo）吃掉
    Expect(SEMICOLON); continue;                    // ← 既没加进 Members，也没推进 offset
}
```

`ttyclock_t` 里 `option` / `geo` / `date` **三个全是**这种写法 ⇒ 它们既不是外层结构体的成员、
`offset` 也没往前走 ⇒ **该结构体在它们之后的所有字段读写全落在偏移 0 上**。

### 二、现象逐条对上（都是量出来的）

| 观测（探针） | 解释 |
|---|---|
| `&bg - &ttyclock = 12` ✅ | `bg` 在第一个匿名嵌套**之前**，偏移正常 |
| `&option.color/-delay/-nsdelay`、`&geo` **全 = 0** | 那三个成员根本没登记 ⇒ `GetMemberOffset` 恒 0 |
| 写 `option.color=2` 读回 **1** | 读写都落在 offset 0；后写的 `option.date=true` 把那里写成 1 |
| `option.nsdelay = 0` 之后整组归零 | 8 字节的零又砸在 offset 0 上 |
| `tty-clock` 屏上 **4000 个空格** | 那些写把 offset 0（= `running`）清成 0 ⇒ `while(ttyclock.running)` **一次都不进** ⇒ 只剩 `init()` 两次 refresh 的空格 |
| `probe14`（手抄的简化结构体）**不复现** | 手抄的那份没有"匿名嵌套结构体当字段" ⇒ 差的就是这一点 |

**为什么值钱**：`struct { … } option;` 是老 C 里极常见的写法；一旦中招，
**该结构体在它之后的所有字段读写全错**，而且**不报错**。

### 三、修法（两步，缺一不可）与判据

1. **解析器**：那个 `continue` 分支要真的登记 `StructMember`（名 = `option`，大小 = 内层结构体的大小）并推进 `offset`；
2. **成员查找**：`s.option.color` 要能算成 `offset(option) + offset(color)`（递归登记内层类型，或把内层成员拍平成带点名的条目）。

**判据**：探针 `probe16` 的 `OC/OD/ON/OG` 从 0 变成非 0 且互不相同 → `probe15` 的 `P1..P4` 全 201
→ `tty-clock` 出钟。（两个探针都用 tty-clock 自己的 `ttyclock.h`，是 GPL 样本，只留在
`.scratch` 里跑，**不进 `Examples/`**。）

### 四、同批（**未验证**）：`curses.h` 补了 `typedef void SCREEN;`

真 ncurses 有这个不透明类型、我们没有；`tty-clock` 拿它当结构体字段的类型用。
**它没有解决这个问题**（补完偏移量还是 0），只是顺手把"类型缺失"这个隐患补上 —— 留与否待定。

---

## v0.96.366 — 老程序兼容：**窗口版 curses API 少了一整族声明**（`tty-clock` 画面空的第一个根因）

### 一、修好：`w*` / `mvw*` 那一族「有实现、没声明」

`tty-clock`（689 行）通篇只用**窗口版**函数（`mvwaddstr` / `mvwprintw` / `mvwaddch` /
`werase` / `box` / `wattron`）。实测（grep `Lib/`）：这一族的**实现全都在**
`shared/src/curses.c` 里，而 `Lib/c/curses.h` **一条声明都没有**。后果分两种：

- 非变参那几个（`mvwaddstr`/`mvwaddch`/`box`/`werase`）：**照样能画**；
- **变参那两个（`wprintw` / `mvwprintw`）把参数传错 ⇒ 程序在 `refresh()` 之前就崩**
  ⇒ 屏上一个字都没有（实测：`tty-clock` 整屏 4000 个空格）。

**与 `usleep` 那条的区别**：那次是**没实现**（编译期报「未定义的函数」，一眼看得到）；
这次是**实现了、没声明** —— **不报错**，只是画不出来，而"屏上什么都没有"看起来最像
"这个程序不兼容"。修法：把这一族原型写进 `Lib/c/curses.h`。

判据 `scripts/vml-c-probe/cases/35-window-api.c`：红灯长在**变参那一句之后**（修前走不到 `M4`）。
全量 C 探针 **29 通过 / 2 失败 / 4 已知红**（两个失败是既有的 29/30，**无回归**）。

### 二、`tty-clock` **仍未画出钟** —— 第二个拦路虎已定位到"哪个变量丢了"

插桩（单参数打包）读到：`main` 里 `option.color = 2` 写完立刻读仍是 **0**；
`init()` 里 `running = true` 读完是 1 ✅，可 `init()` 返回后 `main` 读同一个变量是 **0** ✗
（而 `init()` 里写的 `bg = -1`，`main` 读得到 ✅）⇒ `while(ttyclock.running)` 一次都没进，
整屏只剩 `init()` 那两次 refresh 的空格 —— 与实测的 4000 个空格吻合。

**已排除**：同形状的小结构体（嵌套 + `memset` + 逐字段写读）**全对**；
照 `ttyclock_t` 布局（`bool`/指针/`char[100]`/`int`/**`long`**）的探针**字段偏移也对** ——
但那个探针**顺带暴露另一件事**：`delay == 1L` 为假 ⇒ **64 位比较不对**（另立）。

### 三、两条方法论（这轮踩出来的）

- **插桩的 `printf` 每个标记只带一个实参**：4~5 个 `%d` 会在**第一个转换符处被截断**
  （`[D] setup colo`），把正在量的东西整段遮住。打包成一个整数即可。
- **别用 heredoc 里的反斜杠写 C 代码**：`\n` 会被外层吃掉，插进去的是真换行，
  生成跨行字符串字面量（文件写得出、编译报错，白跑两轮）——要生成代码就写成脚本文件。

> ⚠ 本版**没有**重打 APK：`tty-clock` 还没画出钟，等下一个拦路虎也解决了一起上手机。

---

## v0.96.365 — `matrix_rain.c` 放错树了：**它从来没进过包，手机上自然没有**

用户点的名：「新增的正确的老程序要加入手机的例程」。

**根因是个位置问题**，不是漏了打包：`scripts/make-vml-lib.sh` 打的是
**`third_party/vml/Examples/`**（它 `cd "$VML"` 之后再 `find Examples …`），
而我当初把自研的 `matrix_rain.c` 写在了**仓库根**的 `Examples/c/` 下 ——
于是桌面看目录一切正常、`git` 里也躺着一份，**唯独进不了 `vml_lib.zip`**。

⇒ 已 `git mv` 到 `third_party/vml/Examples/c/matrix_rain.c`（与 `nyancat.c` 并排），
**删掉根目录那棵 `Examples/` 树**（它是个诱饵：两份同名文件、只有一份会被打包，
留着下次还会踩）。

**为什么单独立一条**：这条链上有**两个各自成立的判据**，任何一个不对都表现为
"手机上少个文件"，而两边的现场完全不同：

| 判据 | 谁在管 | 这次的症状 |
|---|---|---|
| 文件在不在**被打包的那棵树**里 | `make-vml-lib.sh` 的 `find Examples` | ← **这次就是它**（文件在，但不在那棵树里） |
| 内容变了要不要**重新解包到手机** | `EnsureExamples()` 比 `Global.Version` | 只比版本、**不看内容指纹** ⇒ **改示例必须同时升版本号** |

所以本次**一并**：升 `Global.Version` → v0.96.365、重跑 `make-vml-lib.sh`（zip 与
`vml_lib.hash` 一起更新）、重打 APK。判据：`unzip -l` 里出现
`Examples/c/matrix_rain.c`（3929 字节）、`Examples/c/old/` 仍是 16 条。

---

## v0.96.364 — 老程序兼容：**二维指针数组的元素多解了一层引用**（修好 `sl` 三个缺陷里的第一个）

`sl`「能跑但画面空」这一条，靠**在 `sl.c` 的副本里插桩**（把循环变量、字符串指针、
每一步的返回值 `printf` 出来）收成了**三个互不相干**的缺陷。本版修好第一个，
后两个连同复现判据一起落进探针套件。

### 一、修好：`char *tab[2][3]` 的元素读成了一层引用之外的东西

```c
static char *bs[2][3] = {{"a1","a2","a3"},{"b1","b2","b3"}};
bs[0][0]        /* 得到 97（'a'）—— 那是 "a1" 的第一个字节，不是那个指针 */
printf("%s", bs[0][0]);   /* 空串 */
```

**根因**：数组的下标**走维度**，只有指针的下标才**解引用**。而
`InferExpressionType` 里那条"多级下标要按级数继续解引用"是照 `T **p` 写的 ——
`char *bs[2][3]` 被当成指针的指针多解一层 ⇒ `bs[i][j]` 判成 `char`（1 字节读）。

**两处一起改，缺一不可**：`InferExpressionType` 的解引用层数改成
"**下标数 − 数组维度数**"；`GenerateArrayAddress` 里那段"多级下标 + 元素是指针"的
特判加一道"数组自己还走不走得完"的闸门（不然后者把基址取成"元素里那个指针"、
再多读一层 —— 实测取出 `0x61003161`，正是 `"a1"` 的头四个字节 `a 1 \0 a`，随后内存错误）。

判"维度数"的**唯一判据**是新加的 `DeclaredArrayDims`。⚠ 类型串**不足以**判维度：
`ParseVariableDecl` 把维度记在 `Dimensions` 上、不写进类型串（`char *bs[2][3]` 的 `Type`
只有 `"char *"`）—— 只看类型串会判成 0 维（= 指针），白改一轮。

**为什么值钱**：老程序的**字符串表**几乎都是二维的（`sl` 的整车图形、菜单、字模、`argv`）。

### 二、另外两个缺陷（已定位、**未修**，都有 KNOWN-RED 判据）

| | 症状 | 已知线索 |
|---|---|---|
| ② 函数内 `static` 数组**读取**丢下标计算 | `static char *s[2]={"aa","bb"};` 读 `s[0]` 得 **0**；数据段**是对的**（`f__s: .word L_…`），毛病在**读**：产物里 `push 基址` 之后**没有** `mul / pop基址 / add / 取值` 那一段，直接把下标当值推出去 | 非 static 同一份正常 ⇒ 差别在 static 那条路 |
| ③ 维度是**表达式/宏**时初始化器全丢 | `static char *m[D51HEIGHT]={…}` / `[D51HEIGHT+1]` → 产物只剩 `.word 0`（= 预扫描写的默认值）；字面量 `[2]` 正常 | **怀疑**维度折不成常量时被登记成 VLA，而 VLA 那条路第一句就 `return`（跳过初始化器）—— 待查实。`sl` 的 `d51[D51PATTERNS][D51HEIGHT+1]` 正是这一档 |

### 三、判据

- `cases/33-mdim-pointer-array.c` **绿灯**（修好的那个）
- `cases/34-static-local-array.c` **KNOWN-RED**（②③，修好那天自己变绿）
- 全量 C 探针：**28 通过 / 2 失败 / 4 已知红**（两个失败是既有的 29/30，**无回归**）
- `scripts/check-vml-patches.sh` ①：`Lib/` 与新前端**逐字节一致**（1916 个文件）
  ⇒ 这次前端改动**不动**已生成的标准库，不必重生成、不必重打 APK

> ⚠ **`sl` 仍未画出火车** —— 它同时踩了 ②③。本版只是把三个缺陷拆干净、修掉第一个。

---

## v0.96.363 — 编辑器「运行」面板换成**自绘命令行网格**（与命令行页同一块控件）

用户点的名：「编辑器页面下面的命令行小窗，控件是旧的，不是最新的自绘命令行控件，需要换成最新的控件」。

### 一、旧的是什么、代价在哪

输出小窗原先是一个 `ScrollView` + `Label`。那正是「显示命令行输出」最不该用的组合，三条代价
（本仓为每一条都交过一次学费）：

| 问题 | 后果 |
|---|---|
| 平台的排版器**折叠连续空格** | 全屏程序靠空格撑出来的列全散（`+---+` 的边框与中间几行对不上） |
| 裸 ANSI **原样漏进文本** | 颜色一个没有，还多出一串转义字符 |
| 大输出拆成上万个 Span | 滚动卡死（移动端编辑器那轮踩过） |

顺带一条：编辑器跑 VML 走的是 `markup: false`（给 AI 那条路的默认档），所以上面第二条是**必然**发生的。

### 二、换成了什么

`TerminalGrid` —— 命令行页那块自绘画布（**同一块控件**，不是平行实现）：

- **列宽自己算**（半角 0.5em / 全角 1em），每段按**自己那一列的起始位置**定位 ⇒ 误差不跨段累积；
- **`«»` 标记上色**（走 `AnsiMarkup`），底色按格铺满整段（含空格）—— 老程序整屏都这么画；
- **滚动条、惯性、光标都自己画**（外面**不套** `ScrollView`：套着时双指的第二根手指会被滚动容器截走，捏合不触发）；
- **全屏程序按网格渲染**，连程序的光标位置也画（`ESC[?25l` 关了就跟着不画）。

配套三处：

1. **`MauiVml.Run(..., markup: true)`** —— 按命令行页那一档来：ANSI 翻标记、stderr 整段套红、
   控制字符（`\r`/`\t`/`\b`）在转标记**之前**解释掉（否则进度条五帧挤成一行、表格列全歪）。
2. **流式**（`ShellStream`）—— `top`/`sl`/`cmatrix` 那一类**不退出就一直画**，整段缓冲的话
   屏幕上要等到程序结束或超时才出现东西。装/摘钩子、`_chunkScheduled` 那道闸门、收尾先排空队列
   再 `Finish()`，全部照命令行页那一套。
3. **折行**（`ShellWrap`）—— 列数按面板实际宽度算（`ColumnsForWidth`，同一份规则）；
   画面行**不折**（折一下整幅画就斜切）。捏合缩字号，**分两拍**：手势进行中只换字号、
   松手（含被系统打断）才重折一次 —— 触摸 120~240Hz，每一拍都重折等于把 UI 线程占满。

### 三、同批修掉的两个隐患（换控件才浮出来的）

- **一次只跑一件事**：原来连点两次「运行」都能跑起来，而 `MauiVml.OnOutputChunk` 是**静态单槽**
  ⇒ 第二趟把钩子指向新流，第一趟的输出投给一个已经换掉的缓冲（画面互相踩）。现在
  `_runActive || _compileCts != null` 直接挡掉并提示。
- **摘钩子前先确认挂着的还是自己的**：`StopPanelStream` 不再无条件 `= null` —— 命令行页用的是
  同一个静态槽（两页各自的闸门只挡得住自己），无条件清会把对方正在用的钩子一并摘掉
  （那边表现为"画面停住不再刷新"）。

### 四、与命令行页**有意**不同的三点

面板本来就没有那些概念：没有尺寸档（固定 80×25 / 横向固定 / 自适应）⇒ 列数一律按面板宽度算；
没有回滚行数的设置项 ⇒ 用 `PanelScrollback`（2000 行）兜底；网格尺寸沿用宿主的
`MauiVml.TermRows/TermCols`（同一个来源），不另立一个 —— 否则同一段输出在两个页面会被判成
不同形状的画面。

> ⚠ **待真机验证**。本次只做到「两端 TFM 编译通过（Windows 桌面 + Android）」——
> 而这是个**观感**改动，编译绿灯证明不了任何东西。

---

## v0.96.362 — `gets`/`usleep`/`mvcur` 三个真缺口 + `sl` 从编不过到能跑；「编译非确定性」结案

### 一、`gets` 修好 —— 根因是 `modules.json` 里 `console` 漏了**自己的实现体**

```json
"console": { "Includes": ["io.vml","printf.vml","scanf.vml","readline.vml"] }  ← 漏了 console.vml
```
`Lib/c/console.vml` 于是**只链依赖、不链实现体**，而 `gets` 只在 `shared/console.vml` 里定义
⇒ 链接期报「未定义的函数 'gets'」。**影响面远不止 `gets`**：凡只在 `shared/src/console.c`
里实现的函数（`input_float`、`print_str_no_nl_impl`…）此前对 C 程序**全都不可用**。
另修签名撞车：零参 `gets(void)` 改名 `read_string`，新增标准 `gets(char* s)`
（走 `SYSCALL #14` 拿 EOF、去 `
`、忽略 `
`）。新增判据 `32-gets.c`。

### 二、`usleep` / `mvcur`（老程序高频，此前**完全没有**）

| 函数 | 要点 |
|---|---|
| `usleep` | 走 `SYSCALL 52`（**内核单位是毫秒** ⇒ `/1000`），**不足 1ms 至少睡 1ms** —— 否则 `usleep(500)` 退化成忙等、手机上白烧电。声明进 `Lib/c/unistd.h` + `#param lib("util")` + 映射表 — **四处缺一不可** |
| `mvcur` | **立即**挪**物理光标**（复用 `sc_cup`），不碰屏幕影子、不改 `sc_cy/sc_cx`（与 ncurses 契约一致）。声明进 `curses.h` |

### 三、`sl` 从"整条编不过"到"能跑"（**画面仍空，下一项**）

失败真因**不是编译器**：样本目录 `cat2/sl.c` **漏了它的头文件 `sl.h`**，
而 `C51PATTERNS`/`C51FUNNEL`/`LOGOLENGTH` 这些**数组尺寸宏定义在 `sl.h` 里**
⇒ 29 个"未声明的变量"。补上后一次全消；`sl` 现在编译成功（55390 条指令）并跑完，
但**画面是空的**（去 ANSI 后只剩被超时掐断的提示）—— 与当初 `tty-clock`"画面空"同类。

### 四、「编译的非确定性」结案：**不是编译器缺陷**

用 `--vml` 落盘逐字节比：`sl.c` 连编 6 次 **md5 全同**；`cmatrix.c` 连编 4 次
md5 各异但**字节数全同**，两两 diff **只有一处** —— 是 `cmatrix.c:175` 的
`__TIME__`/`__DATE__`（它要显示时间，编进产物**正当**）。`sl.c` 里 0 处 ⇒ 故逐字节相同。
⇒ **编译器是确定的**；当初的观察是"样本漏头文件"+"时间宏"两件事被读成了非确定性。

### 五、其余

· `Examples/c/matrix_rain.c`：自研字符矩阵雨（`cmatrix` 是 GPL-3.0 **不能进包**，
  按 gorilla.bas 那条路"只参考玩法、代码自己写"）；
· `GenLib` 加护栏：`BuildShared` 覆盖前比出「原有但新内容里没有」的 `.linked` 并告警
  （上岗第一次就抓出第二处 `util.vml`）；
· 规矩定案：**`#param lib("库")` 写进头文件**（用了这个头就链、不 include 就不链；
  `.linked` 自动去重）—— 见 `third_party/vml/FORK.md`；
· ISA 文档补上 13 条无符号指令 + `scripts/check-asm-doc.sh` 机械护栏（**反证过会响**）。

**判据**：C 探针 **27 通过 / 2 失败 / 3 已知红**（= 基线 + `32-gets`）、
abi-probe 全绿、`check-vml-patches.sh` 判据①②③ 全绿。

## v0.96.361 — `#param lib("库")` 的归宿是**头文件**：一次全量重生成抹掉手写 `.linked` 的根因

用户定了一条规矩，这一版就是照它把一处**静默失效**修到底：

> `#param lib("库")` **放在头文件里**；用了这个头文件就连这个库，不引用就不连。
> 另外 **`.linked` 列表会自动去重**。

### 一、症状与根因（跨了几周才浮出来）

`Lib/shared/curses.vml` 上被**手写**了四行 `.linked`（`conio`/`printf`/`math`/`util`），
而 `kbhit` 的唯一来源就是 `.linked "conio.vml"`。一次**全量重生成**把生成物按源码重写
⇒ 那四行**静默消失** ⇒ 表现为**编译 `20-curses-api.c` 时报「未找到标签: kbhit」**，
而根因在几周前的一次 `GenLib` —— 这类"症状与根因隔得极远"的故障最难查。

`Lib/shared/src/util.vml` 上有**同型的第二处**（`conio`/`printf` 两条）。

### 二、正解：声明搬进头文件（不是搬进 `.c`，更不是手改生成物）

`.linked` 是**编译器产物**：源码里 `#param lib("x")` ⇒ 生成物里 `.linked "x.vml"`
（`Lib/shared/src/array.c` 前三行就是范例，这也是为什么另外 70 个共享模块重生成后**逐字节相同**）。
所以两处都改成：

* `Lib/c/curses.h` 补 `#param lib("conio"/"printf"/"util"/"math")` —— **C 程序 include 了
  `<curses.h>` 就连这四个库**（`.linked` 去重，与 `conio.h` 里那条重复无害）；
* `Lib/shared/src/curses.c` / `util.c` **恢复原样** —— 实现依赖不该由
  「C 程序 include 了什么头」决定。

⚠ `#param` 是**按行**解析的：那一行后面**不能挂跨行的块注释**（续行不再算注释，
全角括号会让词法器报「未知字符」—— 本次自己踩过，注释只能单独成块）。

### 三、给 GenLib 加一道护栏（把静默变成一行警告）

`BuildShared` 覆盖已有 `.vml` 前，先比出**原有但新内容里没有**的 `.linked`，当场打警告。
第一次真正上岗就抓出了上面那第二处 —— 护栏本身是有效的，不是装饰。
（提交后"旧文件==新内容"，警告自动消失，不会长期报红。）

另：`PRUNED.txt` 里 `Lib/c/{conio,curses,graphics}.h` 三条**已过期**（那三份头被
「老程序兼容性」重写过，不再是 PC 内存映射那一路）⇒ 移出清单，判据②转绿。

### 四、`Lib/` 全量重生成（72 个生成物）

`check-vml-patches.sh` 判据① 从 54 处不一致 → **`生成物 1916 个，与重生成结果逐字节相同`**；
判据② 转绿。**判据**：C 探针 `26 通过 / 2 失败 / 3 已知红`（与基线逐条相同，
含 `19-curses.c` ✅ / `20-curses-api.c` ✅）、abi-probe 全绿。

⚠ **同批发现（不在本版修复范围）**：`out-probe` 为 **12/31**，19 条失败**全是非 C 语言**
（`out.py`/`nat.go`/`nat.rb`…）。已用对照证死**这是 HEAD 里就有的**（把 `Lib/` 换成 HEAD
版本重跑同样 12/31 同样 19 条）—— 根源是**22 个前端共用的 `CompilerBase`**
（无符号/`WidenType` 那批改动），而接线只做到了 C。**另外 21 门语言待接。**

## v0.96.360 — 无符号语义从 ISA 到前端全线补齐：13 条新指令 + 三处「把符号性丢掉」的真身

用户问「32 位和 64 位是不是还缺少一些必要 VML 指令」，查下来答案是**分层的**：
64 位那一整套（85–106）**三层全齐**（ISA 有、VM 逐条实现、汇编器 `Enum.TryParse`
泛化认名），而且**前端本来就在发** `ADDL`/`MULL`/`SHLL` —— 实测 64 位加/减/乘
**是对的**。真正缺的是**无符号**这一整类：ISA 里**没有任何无符号表示**，
`/` `%` 比较 `>>` 全按有符号算（`4000000000u % 10` 得 **-6**，应 0）。

### 一、新增 13 条无符号指令（号段 113–125，用户批准）

    ZEXTL 零扩展 int32→long64      DIVU/MODU 无符号 32 位除法/取模
    SHRU 逻辑右移 32 位            DIVUL/MODUL/SHRUL 无符号 64 位版
    CMPU/CMPUL 无符号比较          JA/JB/JAE/JBE 无符号条件跳转

号段取 113–125：128+ 是伪指令块（LABEL/BREAK/DUMP…）。汇编器零改动。
**隔离验证 11/11**（`.scratch/uinsn2.c`，每条新指令配一条老指令做对照）。

⚠ **一条设计要点：`cf` 不能拿来当无符号借位** —— `JG`/`JGE` 把它当**溢出位**用
（`JG = !zf && sf == cf`），而 `CMP` 刻意不传 carry 使 `cf` 恒 false。改成真借位会让
有符号跳转全错（`3 > 5` 时 `sf` 与借位同时为真 ⇒ 也会跳）。所以无符号走**独立标志
`uf`**，`CMPU`/`CMPUL` **刻意不碰 `sf`/`cf`**，两组互不干扰。

### 二、前端接线：三处「把符号性丢掉」的真身（不是"没写"）

`ExprType`/`ExpType` 早就带着 `U8..U64`，共享层的 `ByteSize`/`IsLong` 也都认无符号
—— 缺的是三处丢符号性的代码：

| 位置 | 原来的写法 | 后果 |
|---|---|---|
| `CTypeToExpType`（**唯一入口**） | `Char or UnsignedChar => I8`、`Int or UnsignedInt => I32` **写在同一个 case 里** | 共享层所有 `IsUnsigned()` 恒假 |
| `WidenType`（**所有前端共用**） | `U64 → I64`、**其余一律 `I32`** | `unsigned >> 4` 被并成 `I32` ⇒ 发**算术** `SHR` |
| C 前端强制转换的 `SelectConversionOp` 调用点 | 没传源的符号性 | `(long)4000000000u` 走 `I2L` 得 -294967296 |

`WidenType` 改成 **C 的整数提升规则**：等级不同取等级高的（`U64` 胜 `I32`、
`I64` 胜 `U32`）、等级相同**无符号赢**。

接线全收在共享层的**单一**选择器里：`SelectArithmeticOp(+isUnsigned)`、
**新收的** `SelectBitOp(op, isLong, isUnsigned)`（把此前类型盲、只会发 32 位 `SHL`
的那个与只看字长的合并成两维）、`SelectUnsignedCompareOp(isLong)`、
`SelectConversionOp(+fromUnsigned)`。

⚠ **顺带查清一个长期困惑**：C 前端里 `GenerateComparison`/`GetArithmeticInstruction`/
`EmitCompare`/`EmitBinary` **四个都是死代码**（零调用点）—— 算术与比较**全部**走
`ExpressionManager`。这正是第一版接线"一处也没生效"的原因。

**端到端判据 11/11**（`.scratch/uns.c`，普通 C）：`div=400000000`、`mod=0`、
`shr=250000000`、七条 `cmp_*`（**端到端验了 `CMPU`/`CMPUL`/`JA/JB/JAE/JBE`**，
这几条因需要标签无法用内联汇编隔离测）、`widen=400000000`。
**C 探针套件 26/2/3 与基线逐条相同（零回归）。**

### 三、顺带修掉的三条真缺陷

1. **`INT64`/`FLOAT64` 默认档统一为 `Hard`**（用户批准）。原来**两处默认值不一致**：
   `CompilerOptions` 是 `Soft/Soft` 而 `CompilerConfig` 是 `Hard/Hard`（22 个前端
   全走后者）。CLI `--float64`/`--int64` 的**兜底**也从 `Soft` 改成 `Hard` ——
   此前 `--int64 hart`（打错一个字母）会**静默切到另一个模式**，而那个模式在本平台
   没有库支持（`softint64.vml`/`softdouble.vml` 都没链）。依据：手机 SoC 全是 64 位，
   `ADDL`/`MULL` 与 32 位指令同价，**软模拟是纯亏**。
2. **`L` 后缀的整数字面量超 int 时被兜成 `double`** —— `long long a = 5000000000LL;`
   实测读到 **0**（`3000000000LL` 读到上一个字面量的残留值）。真身：`int.Parse` 溢出
   抛异常、被 `catch` 兜成 `double.Parse`。改法**保住既有行为**（能装进 int 的仍按
   原 int32 返回），只有装不下才给 `long`。
3. **`printf("%d", INT_MIN)` 打出 `-./,),(-*,(`** —— 那串字符恰好是 `'0' + 负余数`，
   定位在 `_printf_itoa` 的 `uv % base`：`uv` 是 `unsigned int` 却被编成**有符号**取模。
   上层修的是编译器，**但 `Lib/*.vml` 是编译产物** —— 必须重新生成才吃得到。
   ⚠ 由此推出一条机制：`GenLib -b` 按**时间戳**判定过期，**得先 `touch` 源文件**
   （否则报「0 编译, 108 跳过」而什么都没重编）。

### 四、仍未结

- **全量重生成 `Lib/`**（28 个模块）：把上述编译器修复吃满。⚠ 试过一次并**引入回归**
  （`20-curses-api.c` 运行期 `未找到标签: kbhit`，已按纪律撤回）。已知边界：
  `kbhit` 只由 `conio.c` 定义、`conio.vml` 未重生成、`curses.vml` 里的 `call kbhit`
  两行**新旧逐字相同** ⇒ 是"链接集合被改动"，机制待查。
- **64 位值进变参只占 1 槽** ⇒ 即便值算对了，`printf("%ld")` 也只看得到低半。
  要动调用约定（调用方推两槽 + printf 读两槽）。
- **十六进制 64 位常量截断**（`0x12A05F200LL` 丢高半）—— 是**刻意的**，源码里写着理由
  （token 的 Value 只承载 32 位），要改得动那条约定。
- **22 个前端里只接了 C**。

---

## v0.96.359 — 老程序兼容性第二轮：BGI 图形层、`getchar` 的 EOF、以及「一行不改就能编能跑」的 16 个判据

上一轮（v0.96.358）修的是「明明有、到不了程序手里」。这一轮把标准提到用户定的那条线 ——
**老程序尽量一行不改就能编译运行** —— 于是**先立判据**（16 个按类型分的老程序），
再逐个把挡路的缺口补掉。**16/16 编译+链接通过，非交互的全部实跑通过。**

### 一、`Lib/c/graphics.h` —— BGI 图形层：老图形程序 `initgraph()` 一行不改

DOS 时代的老图形程序几乎都这么开场：

```c
int gd = DETECT, gm;
initgraph(&gd, &gm, "");
line(0, 0, 100, 100);
circle(200, 200, 50);
getch();
closegraph();
```

这一套（BGI / Borland Graphics Interface）现在**原样能编、能画** ——
`initgraph` / `closegraph` / `cleardevice` / `setcolor` / `setbkcolor` / `setfillstyle` /
`setlinestyle` / `settextstyle` / `settextjustify` / `line` / `moveto` / `lineto` / `linerel` /
`rectangle` / `bar` / `bar3d` / `circle` / `ellipse` / `fillellipse` / `drawpoly` / `fillpoly` /
`putpixel` / `outtext` / `outtextxy` / `getmaxx` / `getmaxy` / `delay` / `kbhit` / `getch` /
`getmouse`，外加 DETECT/VGA/EGA/CGA 与 16 色的常量名。

**三条设计要点**：

1. **与「显存/BGI 一整套不做」那条定案不矛盾，是澄清** —— 定案反对的是**直接操作内存**
   （往 `0xA0000` 直写、靠 BIOS 中断设模式、`DEF SEG`）；这里做的是**转接**：
   **函数名与语义原样保留，落笔换成 `ui_*`**。要改的是"怎么画"，不改的是"程序怎么写"。
2. **落 `ui_win_open_pc`（电脑屏窗口）**：固定坐标系、不随旋转重排 —— 老程序按 640×480
   排的版，换空间就画到框外。
3. ⚠ **颜色是调色板索引，不是 RGB**。`setcolor(4)` 是「红」，不是 `0x000004` —
   不做这层翻译，老程序画面会**整片黑**（索引 0..15 当 RGB 用几乎全黑）。
   头文件里带 CGA/VGA 标准 16 色表，`_bgi_rgb()` 是唯一换算口。

⚠ **弹窗标题用 `__FILE__`**：老程序 `initgraph(&gd,&gm,"")` 不设标题，拿源文件名当标题最好认。
而 `__FILE__` **不能写在头文件里**（那是文本替换，写在那儿会展开成 `graphics.h` 自己），
所以 `initgraph` 是一个宏，在**调用点**展开、把调用者的 `__FILE__` 传进去 ——
实测标题正确显示成 `old_gfx_bgi.c`。

### 二、`getchar()` 收不到 `EOF` —— 老 C **最标准那句**读循环收不了尾

```c
while ((c = getchar()) != EOF) { ... }     /* 老程序里到处都是 */
```

这一句在本平台**永远不结束**：`getchar` 一直返回值、从不给 `EOF`，
程序一直转到宿主超时被掐（`VM execution cancelled`）。

**真根因不在宿主"不给 EOF"，而在它从没被问到。** `io.c` 的 `getchar` 写的是
`return asm("SYSCALL #5, ${1}");`，注释还写着"R0 必须显式给 1" ——
而 asm 模板里的 `${...}` 是**变量替换**（`${ch}` → 该变量所在的寄存器），
**`${1}` 不是变量名，于是什么都不生成**。实测生成出来的函数体里 `syscall` 前面
**一行都没有**（`git show HEAD:.../io.vml` 的 `getchar` 可查）。
后果是 `getchar` 一直走**非阻塞**分支（`bool blocking = registers[0] == 1;` 为假）：
输入一空就返回 `0` —— **"读到的字符是 0"看起来就像"没有 EOF 这个信号"**，
而宿主里那条 `InputExhausted` 分支**根本走不到**。

⚠ 那句注释是**代码从未实现过的意图**，而且**不报错**。`conio.c` 里
`asm("SYSCALL #5, ${0}")` 与"R0=0 ⇒ 非阻塞"是同一个错法（靠 R0 的**残留值**
碰巧非阻塞）。**凡是靠残留寄存器碰巧正确的东西，症状都是时序相关、最难查的那一类。**

**修法（三处，缺一不可）**：
- `io.c` 的 `getchar` 改**字面指令** `asm("MOVE R0 #1")`（照 `builtins.c` 的写法）
  + 走**新号 `SYSCALL #14`**；
- `#14` = 与 `#5` **同语义**，唯一区别是"输入源耗尽时给什么"：`#5` 给**空行**
  （`0x0A`，那是给 `conio.getch()` 的单键读用的），`#14` 给 **`EOF`(-1)**；
- 新号必须登记进 `SyscallConstants.UserAllowed` —— 用户态只放行本表，漏了会打
  `Permission denied: syscall 14 requires kernel mode` 并把 R0 置成错误码
  （表现同样是"读到的字符全是垃圾"）。

⚠ **不能直接改 `#5` 去给 EOF**：`Lib/shared/src/readline.c` 的 `read_line` 靠
`c == '\n'` 收尾，`#5` 一给 -1 它就再也不会返回。老号语义一字不动，EOF 由新号承载
（房规：**新能力一律走新号**）。

**顺带理清 `IConsoleIO.InputExhausted` 的契约**：它现在有**两个需求相反的消费者**，
靠"谁在问"区分 —— `#14` 把 `true` 当 EOF、`#5` 把 `true` 当空行。
于是"**压根没有输入源**"必须报 `true`（报 `false` 会让阻塞读**空转到超时**，
而那时**没有任何输入会到来**，正确语义就是 EOF）。桌面 `vmlcli` 照此改；
手机端**两个源都没有才报 `true`**（它有真的交互终端，`_readLine`/`_readKey` 任一在就不算耗尽）。

**判据**（`.scratch/stdin_min.c`，就是那句原样的读循环）：`--stdin 'ab'` → `[97][98][10]` 后
`N=3` 就停；`--stdin 'ab\n'` → `N=4`；《老程序兼容性.md》第九节「已解决③」记全过程。

### 三、`fflush` 缺失 —— 老程序进度条的标准写法直接编不过

```c
printf("...%d%%", p); fflush(stdout);      /* 老进度条几乎都这么写 */
```

报 `<input>:145: error: 未定义的函数 'fflush'（引用 2 次）`。缺的是**两处登记**，缺一不可：
① 前端的「函数名 → 模块」表（`CompilerBase/CompilerHelper.cs`，`["puts"]="io"` 那一行附近）；
② `Lib/shared/io.vml` 里的标签（由 `Lib/shared/src/io.c` 经 **GenLib `-b`** 生成）。

⚠ 排查时踩过一个**生成物路径**的坑：`vmlcli --rebuild-lib` 会把产物写在**源文件旁边**
（`Lib/shared/src/io.vml`），而规范位置是**上一级**（`Lib/shared/io.vml`）——
于是"重建了却没生效"。**重新生成库一律走 `GenLib -b`**，它按 `modules.json` 的约定路径走。

实现本身是**语义正确的空实现**：本平台输出**无缓冲**（`putchar` 直接 `SYSCALL #4` 落笔），
所以 `fflush` 不需要真的刷，但**必须存在**。

### 四、`sizeof(<数组>)` 返回的是**元素大小**而不是总大小

老程序那句 `memset(a, 0, sizeof(a))` 全靠它 —— 返回元素大小的话，等于只清了头几个字节。
矩阵实测（修前）：全局 `char[8/100/10000]` → 一律 `4`；局部 `char[8/100]` → 一律 `1`。

根因：`sizeof` 处理走的是 `GetTypeSize(InferExpressionType(...))` ——
而 `InferExpressionType` 对数组名给出的是**元素类型**。
修法是声明时另记一份 `arrayElemSize`（数组名 → 元素大小），
`sizeof` 时用 `元素数 × 元素大小`，只有**非数组**才回落到 `InferExpressionType`。
修后矩阵全对，`Examples/c/old/old_std_primes.c` 里那句**原样的** `sizeof(flags)` 也能用了。

### 五、扩展关键字与宏引擎的字面量缺陷（用户定的两条原则）

用户定的原则：**「难兼容的关键字按透明的空白处理」**、**「只管大多数，变态的复杂宏放弃」**。

- `Lib/c/vml_compat.h`（**空宏**，由 `stdio.h`/`stdlib.h` 包含 ⇒ 所有代码都拿得到）：
  `__attribute__(x)` / `__declspec(x)` / `__extension__` / `__inline__` / `__restrict` /
  `restrict` / `far` / `near` / `huge`。
  ⚠ **刻意不做**（记录在头文件里）：`_AX`/`asm`/`__emit__`/显存/`interrupt`/`_pascal` ——
  那是**直接操作汇编与内存**，属于用户定的「不支持，或改掉才能支持」那一档。
- 顺带修掉一个**挡住宏方案**的引擎缺陷：**类函数宏会吞掉字符串字面量**
  （`#define ATTR(x)` + `"ATTR(keep)"` → `[]`），而类对象宏是尊重字面量的。
  根因是 `Preprocessor.Expressions.cs` 里四处处理字面量的地方**有两处漏了**；
  抽成单一判据 `BuildCodeMask`（一个地方说了算）后一并修好。
  原先那条 `AttributeStrip.cs` 路子（175 行）随之**删掉** —— 宏方案更短、也更贴用户要的形态。

### 六、16 个判据示例 + 打包支持第 3 层

`Examples/c/old/`：**std 5**（`calc`/`guess`/`hanoi`/`primes`/`wc`）、
**tty 5**（`ansi`/`box`/`progress`/`conio`/`curses`）、**graphic 6**（`lines`/`shapes`/`palette`/
`plot`/`anim`/`bgi`）。每一个都按老程序的原貌写（不用 C99 之后的语法、变量在函数开头声明、
颜色写调色板索引），**判据就是"它能不能一行不改地编过、跑出东西"**。

⚠ **打包脚本原来只收 `Examples/` 的 1~2 层** ⇒ `Examples/c/old/*.c` 这种
「按类型分目录的老程序」**静默不进包** —— 而桌面直接读仓库、完全看不出来
（与 `vml_lib.zip` 漂移是同一种"只在手机上才暴露"的形态）。已扩到**第 3 层**（zip 与
Python 两条分支都改），并写进 CLAUDE.md 的打包规则。

**图形示例有个坑**：`ui_present()` 之后**立刻** `ui_win_close()` 的话，
快照还没拍就被关了 ⇒ 「场景是空的」。正确收尾是
`ui_present(); while (!ui_win_closed()) ui_wait(&msg, 0); ui_win_close();`。

### 七、仍未修（记在台账里，等下一轮）

- **`%ld` / `%lu` 打印 0**（`long` 打印全废）—— 实测 `printf("%ld", 42L)` 也是 `0`，
  而存储是对的（`(int)big` = `1234567`）。根因在 `Lib/shared/src/printf.c` 的
  64 位数字转换循环**依赖 64 位移位**（`rem << 1`），而本前端的 64 位移位不发 `L` 变体。
  ⚠ 试过改 `SelectBitwiseOp` 让移位发 `SHLL`/`SHRL`，结果**更糟且换了个错法**
  （`1<<4/8/16/32` 全变成常量 2），已回退 —— 这条**要单独一轮**认真做。
- `feof`/`putc`/`tolower`/`difftime` 等仍有缺实现；UDP 的 `ProtocolType` 硬编码成 `Tcp`；
  没有 BSD/POSIX socket 名字（`socket()`/`connect()`/`gethostbyname()` 那一套）。

---

## v0.96.358 — 老程序兼容性体检第一轮：两个「明明有、到不了程序手里」的缺陷、一个会伪造大回归的脚手架坑，以及「难兼容关键字按透明处理」这条原则

拿真程序跑了一轮兼容性体检（`sl` / `tty-clock` / `nyancat` / `kilo` / `cmatrix`，
外加 NetBSD 那批老游戏）。**5 个程序修前 0 个跑通**，但卡点分四类、性质完全不同 ——
这一版修掉其中最值钱的两个、外加脚手架自己那个。

### 一、`//` 出现在**宏体**里就被当成行注释（C 前端）

`#define M "a//b"` 再使用 M ⇒ `词法错误：未结束的字符串`；
**同一个字符串直接写进代码反而没事**。

真实受害程序 `sl`（mtoyoda/sl，295 行）整个编不过 —— `sl.h` 里
`#define LWHL22 "//// \\_/      \\_/    "` 正是这一形态。
`docs/老程序兼容性.md` 原先记的「偶尔词法错误（未结）」，实测是**确定性失败**（连跑 4 次全挂）。

真身：`CompilerBase/Preprocessor.Directives.cs` 的 `StripComments` 从宏体里剥 `//` / `/*`。
那句注释「C99 翻译阶段 3：注释在宏展开前删除」**本身没错，错在漏了同一阶段里字面量已经被识别**
⇒ `"a//b"` 被截成 `"a`，宏展开后就是未结束的字符串。同一个根因还会顺带报出
「未知字符：`\`」「未声明的变量 'X'」等**一串长得完全不同的错** —— 这正是它长期没被认出来的原因。

⚠ 顺带查明：**C 有两个 `Preprocessor`** —— `CCompiler/Preprocessor.cs` 是整份拷贝，
只有独立 CLI（`CCompiler/Program.cs`）走它；库入口（vmlcli/VMLTool 走的 `CCompiler.Compile`）
走 `CompilerBase`。**只有后者削宏体注释** ⇒ 两个入口行为本来就不一致，改动必须落在 `CompilerBase` 那个。

### 二、`convert64` 没进链接清单 —— `atol` / `ltoa` / `dtoa` / `atod` 对 C 一律不可用

`tty-clock`（689 行）报 `未定义的函数 'atol'（引用 6 次）`。但它的
**头文件声明、映射表条目、`.vml` 标签三者其实都在** —— 缺的是**链接清单**：
`Lib/c/builtin.vml` 的 `.linked` 里没有 `convert64.vml`（有 `convert`、`printf`、`io`……）。
而 `conv.c` 里 `atol` **只是一句前置声明**（注释写明"依赖 convert64.vml"）
⇒ 库函数一旦调 `atol`，永远是只有调用点、没有定义。

⚠ **决定链接的是前端产出的 `.linked`，不是 `SharedPrefixMap` 那张表** ——
`CompilerHelper.cs` 里那段注释早已写明"往下面这张表里加条目加了不生效"（Forth 的
`parserexp` 试过）。所以修的是 GenLib 里一张**写死的模块清单**，不是映射表。

⚠ 这与 `console` 是**同一形态**：清单上方那段注释记着上一次 —— `console` 漏了 ⇒
C# 的 `PrintlnStr` 只有调用点、运行期抛「未找到标签: PrintlnStr」。`convert64` 就是下一个。

**实际价值**：`tty-clock` 修后**跑起来了** —— 退出码 0、5164 字节 ANSI 输出
（`ESC[2J` 清屏 + 光标归位 + 逐行上色，真在画表盘）。

### 三、体检脚手架自己会**伪造一次大回归**

`scripts/vml-c-probe/run.sh` 里 `errf="$(mktemp -t vmlprobe)"` —— `-t <模板>` 是
BSD/macOS 写法，GNU coreutils 要求模板里至少 3 个 X ⇒ Windows(Git Bash)/Linux 上直接
`mktemp: too few X's in template 'vmlprobe'`，errf 为空 ⇒ **每条用例都报「实得 」
而没有任何一条真正跑过**。修复前报 `通过 0 / 失败 27`，看着像满屏回归，其实是脚手架没起来。
改成本目录其他脚本早就在用的**裸 `mktemp`**。

⚠ 这类"跑不起来"比跑红更糟：它会让人照着假回归去查代码。

### 四、体检结果矩阵（下一轮的排期依据）

| 程序 | 类别 | 修前 | 修后 | 归属 |
|---|---|---|---|---|
| `sl` | 彩色 tty | ✘ 词法错误 | ✘ 缺 `usleep`/`mvcur` | **缺实现** |
| `tty-clock` | 彩色 tty | ✘ 缺 `atol` | ✅ **跑通** | 已修（见二） |
| `nyancat` | 彩色 tty | ✘ | 缺 11 个函数 + `setjmp.h` | 缺实现 + 缺头 |
| `kilo` | 彩色 tty | ✘ | `__attribute__((unused))` 不解析 | **前端缺陷** |
| `cmatrix` | 彩色 tty | ✘ | `VERSION`（来自 Makefile `-D`） | 需要宏注入入口 |
| NetBSD 老游戏 | 命令行 | ✘ | 缺 `sys/cdefs.h`/`err.h`/`pathnames.h` | 环境缺口 |
| 老图形程序 | 图形 | 未测 | 无 `graphics.h`（BGI 定案不做） | 按设计 |

⚠ **区分两种"未定义函数"很值钱**：`atol` 是**有实现、没链接**（改一行清单），
而 `putc` / `feof` / `fflush` / `tolower` / `difftime` 是**真缺实现**
（`.vml` 标签与 C 实现体都没有）。判据是分别查 `.vml` 标签与 C 实现体在不在 ——
别一律当成"缺库"，那会把一行清单的活估成"实现 6 个函数"。

### 五、位置难兼容的扩展关键字：**用空宏抹掉**（`Lib/c/vml_compat.h`）+ 修掉挡住它的那个宏引擎缺陷

`kilo`（antirez/kilo，约 1270 行）卡在
`void handleSigWinCh(int unused __attribute__((unused)))`，报
`语法错误：期望 RPAREN，但得到 IDENTIFIER`，整个程序编不过。
而 GCC 属性在真实老程序里极常见（`unused`/`noreturn`/`packed`/`format` …），不是边角写法。

**为什么不在解析器里认**：属性注解能出现在**十来种语法位置**（形参、声明前、函数定义前、
结构体成员后、`typedef` 里……），逐个位置去认等于把**同一条规则实现十来遍** ——
而"同一规则两处实现必然漂移"是本仓头号坑。

**做法**：新增 `Lib/c/vml_compat.h`，用**空宏**抹掉这一族 —— 真实工具链也是这么办的
（GCC 的 `<sys/cdefs.h>` 就用空宏抹掉别家编译器的 `__attribute__`，好让 libc 头通用），
并由 **`stdio.h` / `stdlib.h` 带进来**（`#include <vml_compat.h>`）。

⚠ **为什么偏偏放在这两个头里**：真实程序**几乎都会包含它们**；而本前端对
**源文件里没有 `#` 的文件会整段跳过预处理**（`CCompiler.hasPreprocessor`），宏就无从生效 ——
依赖这条包含关系，比给每个文件注入一遍省事得多。
代价是「既不含 std 头、又用这些关键字」的源文件不被覆盖；按定案**只管大多数**，不为它做特判。

| 形态 | 关键字 | 来源 |
|---|---|---|
| 带括号 | `__attribute__(x)` / `__declspec(x)` | GCC / MSVC |
| 裸词 | `__extension__` / `__inline__` / `__inline` | GCC |
| 裸词 | `__restrict` / `__restrict__` / `restrict` | GCC / **C99 标准** |
| 裸词 | `far` / `near` / `huge` | **Borland / Turbo C**（内存模型限定符） |

⚠ **边界写在 `vml_compat.h` 里，判据是「丢掉不改变可观察行为」，不是「看起来是一族」**：
Borland 那套里**只有内存模型限定符能丢**；下面这一族**故意不在头里**，并写明是
「**有意不支持**、不是待实现」：`_AX`/`_BX`（寄存器伪变量 —— 它们就是读写寄存器本身）、
`asm`/`__asm__`、`__emit__`、显存直写（`0xB8000`/`0xA0000`）、`bios.h`/`int86`、`interrupt`、
`_pascal`/`_fastcall`（**不同的调用约定**，丢了会静默编出错误的调用序列）。
它们**恰恰就是**那个可观察行为，抹掉只会让程序**看起来能跑、实际静默编错**，比编不过更糟。

—— 用户 2026-09-22 定案：

> 直接操作汇编、直接操作内存的老程序**就不支持**，或者**修改掉才能支持**。

同源定案见 [`docs/老程序兼容性.md`](docs/老程序兼容性.md) 的 C 档（与 `gfx_*`/显存/BGI 那条一致）。

#### 挡在路上的那个缺陷：函数式宏会啃**字符串字面量**（已修）

要用空宏，先得过这一关：`#define ATTR(x)` 配 `printf("%s", "ATTR(keep)")` **输出 `[]`** ——
字符串里的内容被展开掉了。**对象宏那条路反而认得字面量**（对照：`#define FOO 42` 配
`"FOO stays literal"` 输出正确）。

真身：`CompilerBase/Preprocessor.Expressions.cs` 里**四处**都要处理字面量、**两份漏了** ——
`ReplaceOutsideLiterals`（对象式宏替换）与 `SplitMacroArgs`（实参切分）认得，
`FindMatchingParen`（配对括号）与 `ExpandFunctionMacros`（函数式宏找名/替换）**不认**。
后果不止"字符串被啃"：`F(")")` 会在字符串里那个 `)` 上**提前收尾**，
把后面的实参与语句一并吞掉。**影响面**：任何函数式宏 × 字符串里出现同名文字
（`#define MAX(a,b)` 会把 `"MAX(x,y)"` 啃掉）。

修法：判据收成**一处** `BuildCodeMask(line)`（标出哪些下标在"代码区"），四处都查它；
行被改写后判据作废、下次迭代重建。**不再各写一份状态机**。

**实际价值**：`kilo` 的语法错误清零 —— 新卡点是 `INPCK`/`ISTRIP`/`OPOST`/`CS8`
（**termios 常量**未定义），从「前端缺陷」降级为「缺常量」，正是上面那个便宜得多的一类。

### 验证

- 最小复现：`#define M "a//b"` 现输出 `[a//b]`；逐字节等同 `LWHL22` 的输出 `[//// \_/      \_/    ]`
- C 探针 31 例 **26 通过 / 2 失败 / 3 已知红**，与**暂存改动后重跑的基线逐项相同**
  （那 2 个失败是非 ASCII 文本比对 —— `┌───┐` 与中文诊断在 GBK 控制台下必然对不上，属环境）
- 自测 **通过 6394 / 失败 0**
- `sl` 的词法错误清零 —— 从「前端缺陷」降级为「缺 2 个库」
- 扩展关键字 **12 条判例全过**（`attr1` 形参→F=1、`attr2` 声明前→G=2、`__inline__`→A1=2、
  `__restrict`→A2=5、`restrict`→R=5、`__restrict__`→R2=5、Turbo C `far`+`near`+`huge`→TC=6、
  `__extension__`+`__declspec`→H=7，以及三条宏×字面量判例）
- 宏引擎：`printf("%s", "ATTR(keep)")` 输出 `[ATTR(keep)]`、`"far __attribute__((x)) literal"`
  原样保住（修前分别输出 `[]` 与 `"far  literal"`）
- 净删 175 行（`AttributeStrip` 那 256 行换成 `vml_compat.h` 的几十行 `#define`）

⚠ **顺带排出一条既有隐患**：重打库包时 `make-vml-lib.sh` 自己报出
「签入的 `vml_lib.zip` 与仓库 `Lib/` **原本就不一致**」—— 即**这段时间手机上跑的是旧标准库**，
而桌面看不出来（桌面不读这个包，走的是仓库里的 `Lib/`）。本次已重跑并提交 zip + hash。
⚠ 记住这条纪律：**改了 `Lib/` 就必须重跑 `make-vml-lib.sh`** —— 设备侧 `EnsureLibExtracted()`
是按**内容指纹**决定要不要重新解压的（`LibVersion` 只是写给人看的），
所以"改了 Lib 却没重打包"在桌面上永远测不出来。
- ⚠ 记一笔：`dotnet run -- --test` **只在 Debug 存在**（`#if WAYCODER_TEST`，见 csproj），
  `-c Release` 会报「未知选项: --test」

## v0.96.357 — 电脑屏窗口的键盘：屏幕上那 81 个键 + 外接物理键盘

老程序的输入就是键盘。上一版把电脑屏窗口（第三种窗口）接通之后，`pcscreen.c`
那行 `KEY 0` **永远不动** —— 手机上没有键盘可用。这一版补上两条路。

### 一、屏幕键盘（81 个键，PC 布局）

`PcKeyboardRows` —— **一处数据**（`DrawWindowPage.xaml.cs`），七行：

    Esc  F1..F12
    ` 1..0 - = ⌫
    Tab q..p [ ] \
    Ctrl a..l ; ' Enter
    Shift z..m , . / Shift
    Ctrl Alt 空格 Alt ← ↑ ↓ →
    Ins Del Home End PgUp PgDn

标签照 PC 键盘的样子，**值一律照 Win32 虚拟键码**（`VmlKeys` 是唯一真源）——
程序里写 `key == VML_KEY_F1`，在手机上按屏幕键与在 PC 上按真 F1 必须得到同一个数，
否则同一份老程序要写两套判断。`VmlKeys` 为此补齐了 `Tab/Ctrl/Alt/PgUp/PgDn/End/Home/
Insert/Delete/F1/F12` + `F(n)` 与一整套 `Oem*` 标点。

`Pressed`/`Released`（不是 `Clicked`）—— 与手柄同一条语义：按住不放要能连发，
而 `Clicked` 只在抬手时触发一次。同样照抄了手柄那条**滑键补丁**：手指从 A 滑到 B 时
Android 只发 B 的 `Pressed`、A 的 `Released` 永远不来，不补一条 `KeyUp` 的话程序
以为两个键同时按着 —— 对 `Ctrl`/`Shift` 这类修饰键尤其致命（之后每个键都成组合键）。

### 二、外接物理键盘（Android）

走 **`Activity.DispatchKeyEvent`**，不是"给某个输入框挂 `KeyPress`"。

命令行页那条老路（`ShellPage.HookHardwareKeyboard`）靠的是一个 `EditText` 有焦点；
而**绘图窗口页根本没有输入框** —— 它是一整块自绘画布。硬塞一个不可见的 `Entry`
进去只为吃键，会把 IME、抽取式编辑、输入法工具条一并带进这个页面（编辑器与绘图页
都在这上面栽过）。键事件本来就是 Activity 级的，在源头上接，谁都不用假装是个输入框。

`Services/HardwareKeys`：Android 键码 → Win32 虚拟键码，**只吃认得出的键**
（返回键/音量键/Home 键一律放行 —— 吃掉返回键用户就退不出这个窗口了）。

⚠ 这是与 `ShellPage.AnsiForAndroidKey` **并列的第二张 Android 键表，不是重复**：
那边翻成**终端 ANSI 序列**（给 curses/conio 程序读），这边翻成 **Win32 虚拟键码**
（给收 `VML_MSG_KEYDOWN` 的程序读），两套目标编码没有可共享的部分。

⚠ `Keycode.Home` 是**安卓的 Home 键**（回桌面那个），键盘 Home 是 `Keycode.MoveHome`。
写错了程序永远收不到 Home，现象却是"按 Home 回到桌面"——完全不像键表的问题。

⚠ 标点照**物理键**归位（`!` 归 `VK_1`、`{` 归 `VK_OEM_4`），因为 Win32 程序查的是
"物理键 + Shift 状态"而不是那个字符本身（`VK_SHIFT` 会单独发出去）。

### 三、⚠ 键盘**占自己的一行**，不盖在画布上（用户当场纠正）

第一版做成了浮层（`Grid.Row="0"` + `VerticalOptions="End"`），理由是"不动
`ApplyOrientation` 的行列定义 + 画布尺寸不变"。**那是错的**：浮层盖住了画面底部，
而老程序的状态行、命令行、提示语恰恰都画在底部 —— 真机上 `pcscreen.c` 的
`KEY`/`MOUSE` 两行**当场看不见**。

用户一句话点破：**键盘盖住了显示屏幕**。画面被遮住是用户直接看得见的功能缺失，
而"多摆一个控件进 `ApplyOrientation`"只是实现上麻烦一点 —— 拿前者换后者不划算。

现在与手柄区同一种做法：**chrome 让画布变小，而不是压在画布上面**。
那一行是 `Auto` ⇒ 收起时高度塌成 0（只留 26dp 的开关），画布立刻吃回来。
横竖屏两个分支都摆了（竖屏 row 3 / 横屏 row 2，都横跨三列 —— 它在自己的行上，
不会碰到左右手柄区）。

### 四、模拟器验收：`pcscreen_run.py`

新增四条判据（前面那条"触摸⇒鼠标"的链继续全绿）：

1. **屏幕键盘是 PC 布局** —— 从 UI 树里认 `Esc`/`Tab`/`Ctrl`/`Shift`/`Enter`/`空格`/`←`/`PgUp`；
2. **键盘不盖画面** —— 画面下沿（青框包围盒 `y2`）必须**在键盘上沿之上**
   （最上面那排键的 `y1`）。两个值都是量出来的，不靠肉眼 ——
   实测 `画面下沿 y=1202 < 键盘上沿 y=1467`。**这条正是用户提的那个要求**，
   浮层那版必然反过来，而画面上看着"只是底部被压了一点"，很容易被当成没关系；
3. **点字母键 ⇒ 窗口不关**（说明没把所有键都当成 Esc）；
4. **点 Esc ⇒ 程序退出**（点击 → 按钮 → `PostKeyDown` → 消息队列 → `ui_wait`
   → 程序手里的键码**就是 27** → `break` 出主循环）。

判据 3 单独看有个漏洞：**页面自己**把某个键当"返回"也会关窗，现象一模一样。
所以又加了一条**只有程序才能产生的**证据 —— `pcscreen.c` 每收到一个 `KEYDOWN`
都会把 `lastKey` 写进画面左下角那行 `KEY <n>` 并重画；画布上的文字读不出（不是 UI
节点），但**像素变了**看得出来。逐点采样那段矩形做签名，两帧一比即可。

⚠ 写这条判据时自己踩了**两次**：

* 第一版按"`ui_text` 的 y=416 那一行"把框收得很紧，结果框里**只有网格线、
  一个字都没有**，两次签名相同 ⇒ 把"没框住"读成了"没重画"。框取大一点不会误判
  （网格线两帧完全一致，准星在画面中心够不到那里）。
* 第二版点了 `a` 去比 —— 而**判据 2 已经点过一次 `a`**，再点同一个键 `lastKey`
  不变、画面当然不变 ⇒ 又把好的功能判成红的。改点一个还没按过的 `z`。
  **判据之间会互相影响，调顺序时要把这种耦合想一遍。**

### 五、配套

`WayCoder.Maui/Resources/Raw/vml_lib.zip` 重建；`docs/VML宿主接口.md` 补
"电脑屏窗口的键盘"一节（两条路 + 为什么都走消息不走 `getch()` + 为什么占一行）；
`docs/老程序兼容性.md` 的三种窗口表更新成"已做"。

## v0.96.356 — 电脑屏演示程序 + 写它时撞出的一个前端缺陷

### 一、`Examples/c/pcscreen.c`：电脑屏窗口的**体检程序**

不是给用户用的示例，是**体检程序**（与 `draw_prims.c` 同一性质）：它刻意只画
**固定分辨率**的东西，好让真机截图能逐项核对。该看到什么写在文件头：

1. 一圈边框正好贴着窗口**四边**（分辨率没被换过、没被裁）；
2. 格线 40 点一格 ⇒ 数得出 **16×12 格**（不是自适应后的别的数字）；
3. 底部两行显示**最后按下的键**与**鼠标位置 + 点击数**；
4. **触摸**：手指按在哪儿，十字准星画在哪儿（坐标是 640×480 那套）；
5. **没有手柄区**；
6. Esc 或返回箭头退出。

⚠ 它只监听 `MOUSEDOWN`/`MOUSEMOVE` —— 若"触摸只当鼠标"那条没生效，
十字准星会**一动不动**，这就是最直接的判据。

### 二、⚠ 写它的时候撞出一个**独立的前端缺陷**（与第三种窗口无关）

    static void helper(char* b) { b[0] = 'x'; }
    int main() { { char t[4]; helper(t); } return 0; }

    warning: 定义了但从未使用的函数 'helper'    ← 说没用到
    error:   未定义的函数 'helper'（引用 1 次）  ← 说引用了

**两条诊断自相矛盾**，而源码上看不出任何问题。

根因：`FindUsedFunctionsInStatement` 逐种语句分发，处理了
if / while / do / for / switch / return / 表达式语句 / 变量声明 / 标签 / 内联汇编 ——
**唯独没有"裸复合语句"（`Block`）那一支**。

因果是**级联**的、不是"少收集一个函数"那么简单：

    块里的调用没被看到 ⇒ 函数被判「定义了但从未使用」⇒ **从 AST 剔除**
      ⇒ 调用点解析不到 ⇒ 报「未定义的函数」

**触发条件是"块里同时有变量声明和调用"**（缺一个都不触发），而"在块里声明临时量再算"
是最普通的写法 —— 小样本上很容易碰不到。判据
`scripts/vml-c-probe/cases/31-nested-block-call.c`（一层块 + 一个 if 块当对照）。

### 三、顺带重建了 `vml_lib.zip`

`make-vml-lib.sh` 提示**签入的那份与仓库 `Lib/` 本来就不一致**（这段时间改了不少库文件）。
桌面跑 VML 直接读 `Lib/`、**根本不走 zip 这条链** ⇒ 桌面全绿而手机上跑的是旧库。
一并重建并提交。

### 四、页面接上第一支：触摸按 `Kind` 只发鼠标

`DrawWindowPage.PostTouch` 里那两对消息（Touch\* 与 Mouse\*）现在**按窗口种类分支**：
图形窗口两对都发（老行为一字不改），电脑屏**只发鼠标** —— 老程序处理的是鼠标，
同时再收一对触摸会把一次点击算成**两次输入**。判据收在 `VmlUi.SuppressTouch` 一处。

### 五、模拟器实测（`scripts/maui-vml-verify/pcscreen_run.py`）

判据全在**像素**上，不靠肉眼 —— 画布上的文字读不出（不是 UI 节点），而色块一行断言就够：

```
✅ 电脑屏窗口已打开（判据：命令行页让位 —— 电脑屏没有手柄可看）
✅ 屏幕上没有手柄键/折叠条
✅ 看得到青色外框（程序画的 640×480 边框）  首个像素 @(71,909)
✅ 点之前没有十字准星
✅ 点一下 ⇒ 十字准星出现（触摸当鼠标生效）  首个像素 @(540,1173)
✅ 没有触摸消息泄漏（右上角报警块未亮）
✅ 准星落在点的位置附近  准星 @(540,1198)，点的是 (540,1200)   ← 差 2 像素
```

截图核对：16×12 格线（40 点一格）、`MOUSE 320,295 #1`、`KEY 0`、准星跟着第二次点击移动。

⚠ **"抑制触摸"那条是专门为它设计的报警块**：程序在收到触摸消息时会在右上角亮一块红。
不这么做就**看不出来** —— 准星照旧跟着手指走（鼠标那对在发），只有那一块会亮。

⚠ 两个踩到的坑都记进脚本注释了：
① `find_color` 返回的是**第一个匹配像素的坐标**（或 None），不是像素列表
（第一版按列表用 `len()`，拿到的是坐标元组的长度 2，读起来像"只有 2 个像素"）；
② 模拟器上颜色是**精确值**（`#40C0FF` 就是 `#40C0FF`），没有真机那个 ~0.925 的色域缩放
—— 容差给大是给真机留的。

### 六、判据

c-probe **28/0/3**（新增 31 号那条）；模拟器验收**全部通过**；
`vml_lib.zip` 重建（`pcscreen.c` 要进包才到得了设备）。

## v0.96.355 — 第三种窗口：显示变换（双指缩放 / 单指平移）

用户定的那一条：「**默认等比缩放看全貌，双指放大后 1:1 平移细看**」。
这一版把它做成一个**纯类**（`VmlViewTransform`），页面还没接 —— 但换算本身已经钉死了。

### 一、为什么单独抽一个纯类

这套换算有**两个消费方**：画布落笔（`canvas.Translate/Scale`）与**触摸反算**
（视图坐标 → 场景坐标）。两边各算一遍就是本仓头号坑，而且它们一旦漂移，
症状是**点哪儿偏哪儿、还偏得不单调**（缩放比越大偏得越多）—— 从现象几乎反推不出来。

抽出来之后它是纯 `double` 运算，**桌面自测能逐条钉**。

### 二、内部只存三个量

`Scale`（一个场景单位 = 屏幕上几个点）、`OffsetX`/`OffsetY`（场景原点落在视口的哪里）。
用户调的缩放比是 `Zoom`，`Scale = 基准缩放 × Zoom`。

- `Fit` 取 `min(视口宽/场景宽, 视口高/场景高)` —— **只缩一个维度必然把另一个切掉**
  （这条在画布那侧踩过一次，注释里记着）
- `ZoomAt(锚点, 倍数)`：**锚点底下的场景内容保持不动** —— 那是双指捏合手感的本体
- `Clamp`：装得下的方向**居中**（不能平移，否则整幅被推走、露出回不来的空白）；
  装不下则限制成**内容始终盖满视口**

### 三、两条容易漏的语义（都钉了判据）

1. **`zoom = 1` 时平移无效** —— 直接由 `Clamp` 的"装得下就居中"推出来，
   不需要在 `Pan` 里额外写一句判断（多写一处就是第二份规则）；
2. **换视口保持 `Zoom`** —— 程序转屏 / 收起键盘换了视口时，
   用户刚放大到看细节的那一档**不该被扔掉**。所以 `Fit` 是"重算基准"而不是"重置"。

### 四、判据

`SelfTest.Chunk30` 增到 **43 条**，新增 19 条覆盖：
居中与基准缩放、**`ToScene(ToView(p))` 往返**（"点哪儿偏哪儿"的直接护栏）、
图外返回 null、**锚点不动**、放大后平移两头都不露白、缩放上下限钳位、
复位、换视口保持缩放比。

全套：桌面自测 **6399 通过 / 1 失败**（既有那条）。

### 五、仍未做

页面接上去（`SceneCanvas` 的落笔 + `ToScene` 的命中测试）、
触摸按 `Kind` 只发鼠标、屏幕键盘面板。这三件都要**真机验**。

## v0.96.354 — 第三种窗口（电脑屏）：C 侧包装落地，端到端打通

上一批做了协议与宿主，这一批把 **C 侧**接上 —— 加上它 `#582` 才真的能被程序用上。

### 一、`ui_win_open_pc`（C 包装 + 头文件宏）

    int ui_win_open_pc(char* title, int w, int h, int rotatable, int keyboard);

照 `ui_win_open_ex` 的写法（走包装函数，**不用内联汇编直连多参数 syscall** ——
那条路的 `${}` 展开会覆盖参数，是本仓记过的坑）。

头文件补了 `VML_WIN_NEED_KEYBOARD(1)` / `VML_WIN_NO_KEYBOARD(0)`，
并写明**第 5 个参数是 keyboard 不是 gamepad** —— 位置与 `ui_win_open_ex` 对称、语义不同，
这正是最容易照抄错的地方。

⚠ `rotatable` 在电脑屏这里**没有"支持旋转"那一档**，它只决定**锁不锁方向**
（坐标永远固定）。这一点在两处注释里都写了。

`Lib/shared/vmlui.vml` 由 GenLib 重生成（**生成物，不手改**）；
函数数 162 → 164，多出来的两个标签正是新函数（与既有函数同形）。

### 二、桌面 CLI 的日志补上种类 —— 这是新号在桌面的**唯一**判据

    [vml-host] 开窗："老程序" 640×480（种类=PcScreen，转屏=Legacy，手柄=不要，键盘=要）

⚠ 桌面没有真窗口，**电脑屏窗口与图形窗口在桌面上看起来完全一样**（都是"开窗 + 场景"）——
不打这一笔，`Kind`/`NeedKeyboard` 错了在桌面**一点都看不出来**，
要等到真机上才发现"开的不是那种窗"。这一条是 Plan 阶段点名要的。

### 三、端到端验到了什么

编译 + 链接 + 运行一个用 `ui_win_open_pc` 的 C 程序，从日志读出：

- 标题、尺寸（640×480）都按程序说的来；
- **转屏=Legacy** —— 这正是"坐标系固定、永不重排"那条规则生效的证据
  （程序传的是 `VML_WIN_ROTATABLE`，若照 `#570` 读会变成 `Follow` ⇒ 内容会被宿主改空间裁掉）；
- 手柄=不要、键盘=要 —— 与 `#570` 的 R4 语义**分得开**。

⚠ 中途踩了一次「`Unknown syscall: 582`」—— 改完 `VmlHostRuntime` **忘了重建 `vmlcli`**。
桌面脚手架是独立工程，改了共享层要 `dotnet build scripts/vmlcli` 才会生效。

### 四、判据

全套与基线逐条相同：c-probe **27/0/3**、out 31/31、abi 7/7、diag 61/0/0、
桌面自测 **6380/1**（既有那条）、`Lib/` 一致性 29 条零新增。

### 五、仍未做（下一批）

页面按 `Kind` 分支（无手柄 / 固定坐标系 / 触摸只发鼠标）、显示变换
（双指缩放、单指平移）、屏幕键盘面板。

## v0.96.353 — 第三种窗口（电脑屏）：协议层与宿主层

三种窗口的分工（用户 2026-09-22 定）：**主屏是命令行**（已有，实时化已完成），
**副屏才需要弹窗**，弹窗两种 —— 图形窗口（已有）与**电脑屏窗口**（本批开始做）。
这一批只做**协议 + 宿主**（全是可自测的纯逻辑）；页面行为、显示变换、屏幕键盘在后面几批。

### 一、`WIN_OPEN_PC`（#582）—— 为什么要新号

    R0=标题* R1=宽 R2=高 R3=方向声明 R4=要屏幕键盘 → 句柄，失败 -1

三种窗口要分开，是因为它们的**实现根本不在同一层**：
图形 = 开页 + 场景；电脑屏 = **固定坐标系** + **不同输入** + 键盘。
用一个号承担三者，等于把"分类"永久写进跨语言契约，以后加第四种又要动所有人。

⚠ **也没给 `WIN_OPEN_EX` 的 R3 再加一档** —— R3 是**转屏声明**，加一档就是
给同一只寄存器第二语义，"一个值两个含义"正是本仓反复踩的那类。

**种类由号决定，不是参数**：`VmlUi.KindOfWinOpen(syscall)`，认不出的号一律当
`Graphic` —— 那是"能跑"的那一档，认错时退回老行为而不是撞进一个没写过的模式。

### 二、三个字段承载全部差别（`VmlScene` 上）

| 字段 | 电脑屏 | 图形窗口 |
|---|---|---|
| `Kind` | `PcScreen` | `Graphic`（默认，**老窗口一字不改**） |
| `NeedGamepad` | **false**（输入是键盘 + 鼠标） | 默认 true |
| `NeedKeyboard` | R4 决定（默认 true） | **false**（新字段，老路不受影响） |

⚠ **`Rotation` 取不到 `Follow`**：电脑屏的坐标系**固定**为程序声明的 `w×h`，
永不重排 —— 老程序按那个分辨率排的版，换空间就会画到框外。
所以 R3 在这里只决定**锁不锁方向**，"不锁"落到 `Legacy`（跟随旋转但坐标系不动）。
R4 的含义也与 `#570` 不同（那里是手柄，这里是键盘）：**位置对称、语义不同，宿主分开读**。

### 三、⚠ 顺带补了一个既有遗漏

`DrawTextEx = 581` 早就定义了，却**一直没进 `VmlUi.AllNumbers`** ——
正是那段注释警告的"护栏形同虚设"（清单漏了号，查重查不出来，
症状是**一个功能静默变成另一个功能**）。加 582 时把它一起补上了。

### 四、`IVmlHost` 一个方法都没加

种类挂在 `VmlScene` 上，所以开窗走的还是原来那条 `OpenWindow(scene)` ——
`MauiVmlHost` / `CliVmlHost` / `FakeVmlHost` **三处实现都不用动**
（省掉了最容易漏的那块：第三个是自测用的，漏了虽然编不过，但改起来最费事）。

### 五、判据

新增 `SelfTest.Chunk30` **24 条**，分三层：

1. **契约值**：枚举值只能末尾追加（`Graphic=0` / `PcScreen=1`）、默认 `Kind=Graphic`、
   581/582 都在 `AllNumbers` 里、582 在保留号段内且不撞号；
2. **解码与谓词**：`KindOfWinOpen` 三种号 + 宽容、`SuppressTouch` 两侧、
   `NeedKeyboard` 默认关；
3. **端到端**：真走一遍 `#582` 的分派（用 Chunk28 那个假宿主），断言场景上落下来的
   `Kind`/尺寸兜底/`NeedKeyboard`/**R3=1 落到 Legacy 而非 Follow**，外加老号的回归。

桌面自测 **6380 通过 / 1 失败**（唯一那条是既有的 `#include` 锚行）、桌面与 MAUI 编译均 0 错误。

### 六、仍未做（下一批）

C 侧包装 `ui_win_open_pc` + 头文件 + GenLib 重生成；页面按 `Kind` 分支
（无手柄 / 固定坐标系 / 触摸只发鼠标）；显示变换（双指缩放、单指平移）；
屏幕键盘面板。

## v0.96.352 — 命令行窗实时化（下）：键盘逐键直通

上一版把**输出**那条链打通（边跑边出画面）。这一版补上**输入** ——
两者缺一，交互式程序仍然是"看得见、动不了"。
（原来那条路是**敲满一行按回车**才交出去，于是 `top` 连 `q` 都退不出来。）

### 一、两路输入**天然分开**，不需要程序声明

    · 软键盘   → `StdinEntry.TextChanged`（它只产生**文字**）
    · 外接键盘 → Android `View.KeyPress`（它产生**键码**，方向键/Esc/F1-F12 才有）

按 `ReadChar` 还是 `ReadString` **自动选路**：VM 调哪个读接口，就决定了要哪种输入。
`MauiVml.Run(..., readKey:)` 给了就逐键、没给就退回"攒一行再逐字符吐"的老路。

### 二、按键统一成**字符流**，特殊键按终端老规矩翻 ANSI

方向键 = `\x1b[A`、Esc = `\x1b`、F1-F4 = `\x1bOP..S`、F5-F12 = `\x1b[15~`…
**取值照 xterm 的老约定**，不自己发明编号 —— ncurses 那套程序本来就认它，
自己造一套等于让它们解不出来。好处是软键盘、外接键盘、程序侧**只有一种表示**。

### 三、`KeyQueue`（新增，纯逻辑、可自测）

跨线程交接：**UI 线程投、VM 线程阻塞取**。UI 那半在 MAUI 里测不到，
这一半必须测到 —— 出问题时的症状是"按了没反应"或"程序一直挂着"，两种都极难从现象反推。

`Clear()` **会唤醒等待者并给 NUL**：不唤醒的话它会一直挂着，
表现是"退出后进程还在"。

### 四、⚠ 两个必须知道的边界

1. **软键盘产生不了 Esc / 方向键 / F 键** —— 那些只有**外接键盘**才有。
   所以 `vim` 这类要按 Esc 退出的程序，**手机软键盘上退不出来**；
   要么接个蓝牙键盘，要么等下一期的"电脑屏窗口"配一排屏幕按键
   （那正是第三种窗口要解决的问题之一）。
2. **只有 Android 接了外接键盘**（`View.KeyPress`，照 `EditorPage` 的现成先例）。
   **iOS / MacCatalyst / Windows 还没接** —— 那几端目前只有软键盘那条路。
   这里明说，免得当成"已经支持了"。

### 五、判据

- `SelfTest.Chunk29` 增加到 **30 条**（新增 `KeyQueue` 的 6 条：FIFO、TryTake 不阻塞、
  **阻塞等待能被投递唤醒**、**Clear 不留悬挂线程**）
- 桌面自测 **6356 通过 / 1 失败**（唯一那条是既有的 `#include` 锚行）
- MAUI（Android）编译 **0 错误**
- ⚠ **键盘这条路必须真机验**（软键盘/外接键盘在模拟器上行为不同，本仓的
  `EditorPage` 那几条坑全都是真机才暴露的）：按一下是不是立刻进程序、
  `top` 能不能按 `q` 退出、接蓝牙键盘后方向键/Esc/F 键对不对

## v0.96.351 — 命令行窗实时化（上）：输出边跑边出，不再整段缓冲

讨论「syscall 弹窗做三种窗口」（图形窗口 / 命令行窗口 / 老程序专用"电脑屏"）时，
探索发现**对"PC / Linux / 苹果老程序大量可用"这个目标，拦路虎不是缺一种窗口**：

    CaptureIo 只往 StringBuilder 里攒（MauiVml.cs）
      → RunProgram 跑完才判一次（ScreenOutput.LooksFullScreen(整段)）
      → ShellPage.RunWithPromptAsync 是 var bodyText = (await body()).TrimEnd()

⇒ **程序跑完才一次性刷出画面**。而 PC/Linux/老 Mac 程序**绝大多数是终端程序**
（`top`/`vim`/`mc`/`cmatrix`），全是"不退出就一直画" —— 这一类**一个都跑不起来**。

这一版把输出那条链打通（键盘逐键直通在下一版）。

### 一、`ScreenOutput.Scan`：把"截断 ⇒ 下不了结论"取出来

原来 `LooksFullScreen` 把**两种**情况混成同一个 `false`：「确定不是全屏」与
「序列被截断、还下不了结论」。流式判定照它走，**块边界正好落在 `ESC[` 中间时
就会把全屏程序永久误判成线性**（那一屏从此散成文本，不可逆）。

新增 `Scan(raw, out bool truncated)`，`LooksFullScreen` 变成它的薄包装 ——
**行为一字未变**，只是把原来两处 `return false` 的**原因**取了出来。

### 二、`ShellStream`（新增，纯逻辑）：探测 → 线性 / 网格

- **探测期攒着不出**，判定了再一次性决定怎么呈现 ⇒ **不需要"已 append 的文本怎么撤回"**那套逻辑
- 定线性的判据分两档：**一个 ESC 都没有 ⇒ 当场定**（没有 ESC 就不可能在做光标定位）；
  有 ESC 但没见到光标定位 ⇒ 等 1KB（`ls --color` 那种也带 ESC，而全屏程序的开场只有几十字节）
- 硬上限 8KB，防止"永远下不了结论"把输出全扣住
- 结尾那半截 ANSI 序列**留着等下一段拼齐** —— 交给 `ToMarkup` 会多打一个字面量，
  交给 `FrameBuffer.Apply` 更糟（半截序列会被当成正文吃进网格）

复用的现成件：`ScreenOutput`、`FrameBuffer`、`AnsiMarkup`、`ShellControls` —— **一行没重写**。

### 三、输出泵：**不用定时器**

`CaptureIo` 加增量出口（靠 `_sb.Length` 算增量）+ 节流（80ms / 16KB）。

⚠ **刷新的时机全在 VM 线程上**：定时器线程读、VM 线程写同一个 `StringBuilder`
就是数据竞争。写成"**有新增才判节流**"—— 程序不写就没有可刷的，天然不需要定时器。

⚠ **读输入之前强制刷**：程序打印完提示符就阻塞等键，那一屏若还在节流窗口里没出去，
用户看到的和"卡死了"一样 —— 而那是**最需要看见画面的一刻**。

⚠ 收尾再刷一次，否则末尾没换行的那一截永远看不见。

### 四、网格是**替换**那一块，不是追加

`ApplyStreamRender` 是**全线唯一**决定"追加还是替换"的地方。
`_gridRows` 与既有的 `_cursorBaseLine` 共用一个基准（不留两份）；
回滚裁剪会把块开头的行丢掉、`_cursorBaseLine` 跟着前移，**但 `_gridRows` 得跟着缩** ——
不缩的话下一帧 `RemoveRange` 会多删几行正文。

⚠ **流式只开在"VML 运行"那条路**（`RunWithPromptAsync(..., stream: true)`），
**编译**那条虽然也是 `markupResult:true` 但不开 —— 它产出的是"✔ 已生成 x.vml"这种短消息，
流式对它毫无意义，却要多担一份"输出被交出去两遍"的风险。

### 五、诊断尾巴单独补

正文边跑边出去了，但**诊断是跑完才知道的**（`diag`/`diagErr` 在 `vm.Run` 之后的
`finally` 里才读得到）⇒ 新增 `MauiVml.LastDiagnostics`，收尾时补在后面。
不这么做的话，流式一开 `内存错误(PC=…)` 那套报错就**全丢了** ——
而"程序崩了、用户只看到没有输出"正是这个平台反复修过的故障。
（顺带把两处手拼诊断的代码收成 `BuildDiagnostics` 一处。）

### 六、判据

- 新增 `SelfTest.Chunk29`：**24 条**断言，覆盖 `Scan` 的 truncated 契约、
  状态机（探测→线性 / 探测→网格 / 截断时不下结论）、分块不丢字、网格第二帧是替换
- 桌面自测 **6350 通过 / 1 失败**（唯一那条是既有的 `#include` 锚行）
- MAUI（Android）编译 **0 错误**
- ⚠ 写用例时**自己写错过一条期望值**（先 `Feed("正文")` 的话那一段当场就吐出去了，
  `Finish` 之后再断言等于什么都没测）—— 自测当场红了，已改成真正经过
  "探测期攒着 + 尾巴半截"的场景

### 七、仍未做

- **键盘逐键直通**（`ReadChar` 改吃按键队列 + 逐键输入模式）—— 下一版
- **第三种窗口（电脑屏）**：`WIN_OPEN_PC` 新号、固定分辨率、触摸→鼠标、
  双指缩放/平移、软键盘 + 物理键盘。语义已定，见计划文件
- ⚠ 顺带记一条 Plan 阶段查到的既有遗漏：`DrawTextEx = 581` **没进** `VmlUi.AllNumbers`
  （`VmlUiProtocol.cs:137` vs `:643-663`），正是那段注释警告的"抢号护栏形同虚设"。

## v0.96.350 — `signal()` 返回 `-102`：同一函数两份实现，赢的那份永远不可能成功

接着上一轮子 Agent 审计的第四条。这条一句话概括：**老程序装 Ctrl+C 处理函数，
拿回来一个既不是 0 也不是 `SIG_ERR` 的第三种返回值**。

### 一、症状

    r = signal(SIGINT, on_int);   /* 修前 -102，修后 0 */

`Lib/c/signal.h` 的承诺是白纸黑字的「**装处理函数一律返回成功**」，
`util.c` 的实现也是 `return 0`。**但 `signal` 这个名字在本库里有两份实现**：

    util.c :  int signal(int sig, void* handler) { return 0; }          ← 头文件要的是这份
    os.c   :  int signal(int signum, void* handler)
                  { return asm("SYSCALL #350"); }                        ← 实际链上的是这份

而 `SYSCALL #350` 对**用户程序恒被拒**（`case 350` 先过 `PrivilegeDenied`；
即便放行，下一句也是 `NOT_SUPPORTED`）⇒ **这份永远不可能返回 0**。

### 二、为什么这个返回值最难缠

老程序里两种写法都常见：

    if (signal(SIGINT, on_int) == SIG_ERR) { perror("signal"); exit(1); }
    if (signal(SIGINT, on_int) != 0)       { 致命错误 }

`-102 ≠ SIG_ERR(-1)` ⇒ **前一半程序静默通过、后一半莫名退出**，
而两者的源码上完全看不出差别。

### 三、修法：**删掉多的那一份**，不是"把两份改成一样"

`Lib/c/signal.h:26` 写的是 **`#param lib("util")`** —— 头文件的本意就是 `util` 那份；
`os` 模块列它才是错的。所以：

1. 删 `os.c` 里的 `signal`（留一段说明为什么删、以及它为什么永远不可能成功）
2. 从 `Lib/modules.json` 的 `os.Functions` 里去掉 `"signal"`
   （那张表驱动 GenLib 的包装/绑定生成，不移除会留下"虚构条目"）
3. 重新生成 `Lib/shared/os.vml`

⚠ 这是「**同一份数据两个实现必然漂移**」的又一例，而且**谁赢取决于链接顺序**
（`globalLabelMapping` 按模块名建表）—— 症状会随链接进去的库不同而变，最难复现。

### 四、判据

- `26-signal-shadowed.c` **全绿**（此前 `A/B/C` 三条红）：
  `A=0|B=0|C=0|D=0|E=0|F=2,28,20`
- 全套与基线逐条相同：c-probe **27/0/3**、out 31/31、abi 27/29、diag 61/0/0、basic 23/0/4

## v0.96.349 — 内置函数少给参数会把编译器**崩掉**（一行 .NET 异常文本）

接着上一轮子 Agent 审计的最后一条。这条一句话概括：**用户写错一个字，
看到的是一句 .NET 的异常文本，没有文件、没有行号、没有诊断码**。

### 一、症状

    int main(){ (void)getenv(); }

    ✘ 编译失败（c）：⚠️ 编译失败：Index was out of range. Must be non-negative
      and less than the size of the collection. (Parameter 'index')

`getenv`/`setenv`/`PEEK`/`POKE` 是**前端直接生成指令**的（不走普通调用那条路），
实现里直接取 `Args[0]`/`Args[1]` —— **没有 `Args.Count` 守卫**。
同文件里 `exit` 有守卫 ⇒ 这四条是漏的。

### 二、修法：**报可读的诊断**，不是"让它别崩就行"

编译失败是**对的**（少参数确实编不出来），错的是**失败的方式**。
新增诊断码 `CodeGen_ArgCountMismatch = 1318` + `CodeGeneratorBase.ReportArgCount`：

    /tmp/m20/args.c:4:5: error: 'getenv' 需要 1 个参数，这里只给了 0 个 [CodeGen_ArgCountMismatch]
      | 它是由编译器直接生成指令的内置函数，参数个数必须在编译期就定下来 —— 少一个就没法生成。

⚠ **一次要把该报的都报出来**（`Diags` 是收集起来最后一起抛的）：
一个程序里同时缺 `getenv`/`POKE`/`setenv` 的参数，三条一起出现 ——
"遇到第一个错就停"会让用户改一个编一次、来回好几轮。

### 三、判据套件补了一条**以前没有过**的约定：`// EXPECT-COMPILE-ERROR:`

「**编译器该报错却崩了 / 静默通过**」这一类此前**一条判据都没有**，
而它们恰恰最难发现 —— 编译期就结束，跑不到任何输出，于是被当成"正常的编译失败"吞掉。
新约定断言 **stderr 里出现给定子串**，且**可以写多条**（每条都必须出现）。
`cases/30-builtin-argcount.c` 用它压了 4 条断言。

### 四、判据

- 新增 `30-builtin-argcount.c`（4 条断言全过）
- 全套与基线逐条相同：c-probe **26/0/4**、out 31/31、abi 27/29、diag 61/0/0、basic 23/0/4
- ⚠ 仍开着的一条：**普通库函数与用户函数一概不做参数个数检查**（实测 `strlen()`
  少传照样编过、用户函数 `f(1)` 对 `f(int,int)` 也编过）。要统一得先有"声明表"，
  是另一件事 —— 这一版只保证**内置函数这条路不再是崩溃**。

## v0.96.348 — `ioctl(TIOCGWINSZ)` 返回 0 却一个字节不写：头文件与实现的 ABI 对不上

接着上一轮子 Agent 审计的第三条。这条一句话概括：**老程序问"屏幕多大"，
拿到的是 0×0，而且它看不出任何异常**。

### 一、症状：**返回 0（成功）**，缓冲区原封不动

```
    printf("
R=%d", ioctl(0, TIOCGWINSZ, &ws));   /* R=0 */
    printf("
I=%d,%d", ws.ws_row, ws.ws_col);     /* I=0,0  ← 没被碰 */
```

而 `ioctl(0, TIOCGWINSZ, …)` 正是老程序（**包括 ncurses 自己**）拿屏宽排版的
**唯一**入口 —— 答不出来就按 80×25 猜、或者画到屏外。

### 二、根因：头文件写 `unsigned long`、实现写 `int`

    Lib/c/sys/ioctl.h         int ioctl(int fd, **unsigned long** request, void *arg);
    Lib/shared/src/util.c     __stdcall int ioctl(int fd, **int** request, void* arg)

本平台的调用约定是「4 字节对齐，**只有 64 位参数占 2 个位置**」⇒

    调用方（按 `unsigned long` 编）压 **2 个槽**：`[0x0000][0x5413]`
    被调方（按 `int` 编）读 **1 个槽**：`request = 0x5413` ✓，`arg = 高位槽 = 0` ✗
    ⇒ `if (request == TIOCGWINSZ && arg)` 的**后半个条件为假** ⇒ 整个分支跳过

### 三、⚠ 修法只能改头文件（试过另一边，没用）

先试的是"把**实现**改成 `unsigned long` 去对齐 POSIX 头" —— **没用**
（`I` 仍然是 `0,0`）。把两侧的汇编都打出来看，**槽位偏移其实都是对的**
（调用方 `[R13+12]`、被调用方 `[R12+24]`）⇒ 卡在更深的 **64 位参数传递**上。
所以这一版两边统一成 `int`，并在两处都写明「改一边就要改另一边」。

### 四、判据

- `24-termios-ioctl.c` **全绿**（此前 `I=0,0|J=0` 两条红）：
  `A=1|B=1|C=1|D=0|E=0|F=0|G=1|H=0|I=25,80|J=2000|K=0|L=7`
- 全套与基线逐条相同：c-probe **25/0/4**、out 31/31、abi 27/29、diag 61/0/0、
  basic 23/0/4、`Lib/` 一致性零新增。

### 五、仍开着的一条（同一族，未修）

`lib` 里 **64 位参数的传递**本身还没查清（上面第三节那个"改实现没用"就是它的表现）。
`vml-abi-probe` 目前 27/29，两条已知红是 Forth 和 D 的，与这条不是一回事 ——
**先把现象记在这里**：`unsigned long` 形参在库函数上对不齐，而同一份代码在
调用方看偏移是对的。真要动它，得先有一条最小判据把"64 位实参跨调用边界"单独钉住。

## v0.96.347 — DOS 程序的框线全变 `�`：输出侧的编码判定

接着上一轮子 Agent 审计报的那条「非 UTF-8 字节被解成 U+FFFD」。这条一句话概括就是
**「DOS 时代的框线画不出来」**，而它对老程序兼容的覆盖面比剩下几条都大。

### 一、症状：不是"没颜色"，是**画面上占位都不对**

老程序画框线写的是**裸字节**（`0xC4`=`─`、`0xDA`=`┌`、`0xB3`=`│`）。它们不是 UTF-8，
却**正好落在 UTF-8 的首字节区间**（`0xC0`–`0xDF` 两字节、`0xE0`–`0xEF` 三字节）
⇒ 解码器把**后面那个字节**当成续字节一起吃掉，两个字节换一个 U+FFFD。

实测（`putchar(0xC4)` ×3）：

    修前： efbfbd efbfbd efbfbd      ← 三个 �
    修后： e29480 e29480 e29480      ← ─ ─ ─

### 二、光"先试 UTF-8"不够 —— CP437 里有**合法的 UTF-8 子串**

第一版改法是「验不过就**只吐第一个字节**」（不再吞），`───` 立刻对了。
但 `┌───┐`（`DA C4 C4 C4 BF`）仍然错：

    DA C4 → 不合法 ⇒ `┌` ✓
    C4 C4 → 不合法 ⇒ `─` ✓
    C4 BF → **合法 UTF-8**（U+013F `Ŀ`）⇒ 吐出一个 `Ŀ` ✗

双线框更糟：`╔══╗`（`C9 CD CD BB`）的 `CD BB` 同样合法。
**光看字节流分不开这两件事** —— 这是信息层面的歧义。

### 三、判据：**第一个不合法字节 ⇒ 整个程序按单字节老编码，粘住不回头**

同一次运行里，一旦出现不合法字节，此后所有高位字节都按**一个字节一个字符**走，
单字节那一个查 `Runtime/Device/Cp437.cs` 的表翻成 Unicode。

⚠ **代价（有意接受）**：一个程序只能是一种编码 ⇒ "写中文 + 画 CP437 框线"的**混写程序**
在这里画不出来。但那种程序实际上不存在（要么是 UTF-8 的、要么是 DOS 时代的），
而**框线画不出来**是老程序兼容线上真实存在的那一类。

⚠ **UTF-8 那一侧不受影响**：合法序列照旧走多字节路径（中文一字不变），
而且 `20-curses-api.c` 那条的 refresh 输出里就带着 `A中`，它绿着即证明这条路没被碰到 ——
**两条判据缺一不可**，因为同一个程序进不了两种模式。

### 四、判据

- 新增 `29-cp437-output.c`：`B1=┌───┐|B2=╔══╗|B3=│x│|B4=░█π÷|A=42`。
  其中 `B1`/`B2` 就是上面那两处"合法 UTF-8 子串"的回归判据，
  `B3` 压"高位字节不能吃掉后面那个字节"（原症状的指纹）。
- 全套与基线逐条相同：c-probe **24/0/5**、out 31/31、abi 27/29、diag 61/0/0、
  basic 23/0/4、examples-build 99/100、自测 6325/1。

## v0.96.346 — `char**` 的双下标读成 4 个字节（`argv`/`getopt`/字符串表的共同底座）

接着 v0.96.345 往下查 `cases/23`。这一版把 **`char**` 的两条路径**都修了 ——
它们**互不相干**，症状都是"读出来是 4 个字节拼成的数"，而 `%s` 打印**看着正常**
（`%s` 走地址、不经过元素类型推断）⇒ 只有 `%d`/`%c` 把字符当数读才露馅。

### 一、多级下标要按级数解引用

解析器把 `p[i][j]` 收成**一个** `ArrayAccess` + `Indices=[i,j]`，**不是嵌套节点**
（只有 `(p[i])[j]` 这种带括号的才分两层 —— 这正好解释了"加一层括号就对"）。

而 `InferExpressionType` 那条分支只看 `Array` 是不是标识符，
**看不到"外层下标"**：只按"元素是指针"返回 `arrType` ⇒ `pp[0][0]` 被判成 `CharPtr`，
最后一跳按 4 字节读。实测 `167789121` = `0x0A004241`，正是 `"AB\0\n"` 四个字节。

⚠ **地址那侧一直是对的**（步长先 4 后 1），只有取值宽度错 —— 两边不一致才是这条的特征。

### 二、`*p` 的返回类型

`ExprType` 这一档**表达不了"指针的指针"**（`StringToExprType` 把 `char **` 也归成 `CharPtr`）。
照 `case CharPtr: return Char` 走，`*p` 被判成 `char` ⇒ 按字节加载，
`**pp` 读出来是 **0**。判据回到**声明原文的星号数**（`ElementIsPointer`，
那个函数的注释本就写着"三处共用"—— 这次是第四处）。

**顺带修好了库里的一处真缺陷**：`Lib/shared/string.vml` 重生成后
`str_split` 里的 `parts[count][len] = '\0';` 从 **4 字节写**变成**字节写**
（`move` → `moveb`）。`Lib/` 一致性脚本的不一致清单**零新增**。

### 三、`getopt` 全绿：修好 ② 之后才显形的第二处断点

`23` 绿了之后 `22-getopt.c` 仍然红 —— 整条链上有**两处独立断点叠着**，
所以"改一处没变好"当时并**不能**说明那一处没修对。

第二处是：**`optarg`/`optind`/`optopt` 没有任何地方定义**。
`util.c` 原先只写三行 `extern`，而"`extern` 声明不占数据段槽位"修好之后它们就**悬空**了
⇒ 使用者写进去的 `optind` 与 `getopt` 读到的**不是同一块内存**
（实测 `optind = 1` 之后第一次 `getopt` 直接返回 `'b'`、`optind` 读出来是 **98**）。

已在 `util.c` 里**真正定义**（`optind` 按 POSIX 初值给 **1**），并把
`Lib/c/unistd.h` 里那几行**定义**（`int optind;`）改成 `extern` —— 否则每个
include 了它的使用者都会生成一份槽位、把库里那份遮住，**那正是 `stdscr` 当初的形态**。

⚠ 这条也说明：**改完前端要让 `Lib/` 跟着重生成**，否则库里还跑着旧前端编出来的代码。
`Lib/shared/util.vml` 重生成后 `getopt` 里的 `argv[optind][0]` 才变成字节读。

### 四、⚠ 试过又撤回了：同名函数被库符号静默劫持

`cases/23` 原本把一个形参函数叫 `peek`，而 `builtins` 库里**也有** `peek`
⇒ `call peek` 被链接器改写成 `call lib_builtins_peek`，**用户那个函数从头到尾没人调用**。
（症状极具误导性：函数体汇编**逐字正确**、参数读取也对 —— 因为**调用根本没进去**。）

试的修法是「主程序自己定义的名字不许被改写」，**但撤回了**：
`VmlProgram.Labels` **不是可靠的真源**（只有十几个条目、地址还是前端留的陈旧占位 ——
实测 `peek = 0`，和一堆字符串标签挤在地址 0 上），而按名字一刀切会**打断 native 声明**：
JS 前端的 `native function println_str(s) {}` 是 `AddLabel(name)` 之后直接 `return`，
留一个**空标签**、本来就该让库接管 —— 一刀切之后整个 JavaScript 的输出全废
（崩在 `PUSH @R0`，SP=FFFFFFFC，一个字都没打出来）。

**所以：这条要修，得先有一个"这个标签是不是真定义"的可信判据**，目前没有，别在链接器里按形状猜。
已立 **`cases/28-name-shadowed-by-lib.c`（KNOWN-RED）** 把复现钉住。
`cases/23` 里的函数已改名为 `argfirst`（不冲突），它才测得到本来要测的形参形态。

### 五、判据

- `23-charpp-subscript.c` **全绿**：`A=65|B=65|C=66|D=65|E=65|F=65|G=67`
- `22-getopt.c` **全绿**（此前整条红）：`A=a,2|B=b,VAL,4|C=-1,4|D=-1,4|E=?,2|F=?,3|G=-1,1`
- 新增 `28-name-shadowed-by-lib.c`（KNOWN-RED：期望 `A=12345|B=12352`，实得 `A=0|B=48`）
- 全套与基线逐条相同：c-probe 22/0/6、out 31/31、abi 27/29、diag 61/0/0、basic 23/0/4、
  `Lib/` 重生成一致性 51 条零新增

### 六、子 Agent 审计的其余缺口（未修，已定位）

- `ioctl(0,TIOCGWINSZ,&ws)`：头文件写 `unsigned long request`、实现是 `int request`
  ⇒ 64 位参数占 2 个槽，`arg` 读到高位槽 = 0。**排版取终端尺寸的唯一入口**，且返回 0 读不出异常
- `signal()` 恒返回 `-102`（`SYSCALL_PERMISSION_DENIED`）：`util.c`（`return 0`）与
  `os.c`（`SYSCALL #350`）各一份，前端路由到了 `os`
- `graph` 模块一调就崩（`SP=FFFFFFFC`）、`graphics.h`/`gfx.h` 被 `PRUNED.txt` 剪掉且
  `InitGraph` 不在前端映射表里 —— 三层独立全断
- 非 UTF-8 字节被 `VmConsoleDevice` 按 UTF-8 解码 ⇒ **CP437 框线字符到不了屏幕**（DOS 程序全变 `�`）
- `getenv()/setenv()/PEEK()/POKE()` 少参数 ⇒ 编译器抛未处理异常（缺 `Args.Count` 守卫）

## v0.96.345 — `initscr()` 返回 80 的真根因：**结构体全局只分到 1 个字**

上一版把 `stdscr` 查到「`initscr()` 在调用方拿到 **80**」这一步，判据是 `20-curses-api.c`
的 `S5=80`（80 正好是 `SCR_COLS`，即 `sc_win` 的第二个字段）。
这一版把它查到底 —— **根因不在调用方，也不在链接器，而在数据段的分配**。

### 一、★ 根因：多字类型按「1 个字」分配

`Lib/shared/curses.c` 里 `WINDOW` 有 8 个 `int` 字段，但链接产物里：

    lib_curses_sc_win: .word 0                        ← sc_win，**只 1 个字**
    lib_curses_stdscr: .word lib_curses_sc_win        ← 紧挨着
    lib_curses_sc_ch:  .word[2000] 0                  ← 再挨着

`sc_win.cols` 落在 `sc_win+4` —— **正好是 `stdscr` 那个槽位**。
于是 `sc_init()` 里的 `sc_win.cols = 80;` 把 `stdscr` 写成了 **80**：
`initscr()` 返回的是"`stdscr` 槽里的值"，而那个值被人改过。

`CodeGenerator.Functions.cs` 里结构体分支要求类型串**以 `struct ` 开头**，
而 `WINDOW` 是**匿名 struct 的 typedef**（`typedef struct {…} WINDOW;`）⇒ 进不去；
没有初始化器时更是直接落到 `else → 0`（一个 word）。

**修法**：新增一条「非数组 + `GetTypeSizeFromString(类型) > 4` ⇒ 按真实大小分配」，
位置**必须排在三个标量初始化器之后** —— 详见下面第三节。

### 二、同一条链路上另外两处（都在链接器）

1. **数据段标签被 `+ baseOffset`**：`LibraryLinker` 合并库标签时一律
   `kvp.Value + baseOffset`（= 已合并的**指令数**）。可 `Labels` 这一张表里混着两种地址 ——
   代码标签的值是**指令下标**，数据标签的值是**数据地址**。一律加偏移的后果是
   数据标签被算成一个指向**代码中间**的假地址。
2. **主程序对库数据符号的裸名引用没人改写**：`extern WINDOW *stdscr;` 在用户程序里
   求值求到 **0**。数据标签的地址链接期还没定 ⇒ 造不出裸名别名；
   正解是把引用**改成带前缀的名字**（`lib_curses_stdscr` 在合并后的 `.data` 里有定义）。

### 三、⚠ 我自己在这次改动里引入的一个回归，以及它是怎么被抓到的

第一版把「多字类型」那条分支放在了**最前面**，于是
`double PI64_D = 3.141592653589793;` 被它截走、按"零初始化"铺了两个空字
⇒ **字面量值整个丢掉**（`Lib/shared/math64.vml`：`.dword 3.14159…` → 两个空 `.word`）。

**自测一条都没红**（6325 通过 / 1 失败，与基线逐条相同），
是 `scripts/check-vml-patches.sh` 抓到的 —— 它比对「`Lib/` == f(源码, 前端, GenLib)」。
⇒ **改数据段分配一定要跑那个脚本**，它在自测的盲区里。

修好后的判据：该脚本的不一致清单**回到基线 51 条、零新增**。

### 四、判据

- `20-curses-api.c`：**27 条全绿**（此前 17 条）。`S`/`T`/`U` 现在读到 `25,80 / 0,0 / -1,-1`。
  `S4` 从"写死地址 5215"改成**关系判据** `(int)stdscr != (int)&stdscr`
  —— 地址随编译变化，写死既非"独立推导"也抓不到下次回归。
- 其余全套逐条与基线相同：out-probe 31/31、abi-probe 27/29（`drift.fth`/`drift.ld` 两条已知）、
  diag-probe 61/0/0、basic-probe 23/0/4（已用 `git stash` 跑基线逐条对照过）、
  examples-build 99/100（`forth/parserexp_demo.fs` 缺库函数，前端报的、与本次无关）、
  桌面自测 6325 通过 / 1 失败。

### 五、子 Agent 审计（同一批，只读、未改生产代码）

新增 `cases/21`–`27` 七个用例（`cprintf` / `getopt` / `char**` 双下标 / `termios`+`ioctl` /
`sleep`+`delay` / `signal` / `graph`），并把「头文件声明 vs 实际实现」做了端到端对账
（判据是**真编一次**看链接报不报错，不是搜源码）：106 个函数"声明了但没实现"。
其中 `char**` 双下标（`argv`/`getopt`/字符串表的共同底座）是下一批的头号目标。

## v0.96.344 — `stdscr` 的真根因：**C 前端不认 `extern`** + 取址初始化的四段链路

接着上一版查「`stdscr` 在使用方恒为 NULL」。这一版把它**查到底**了 ——
根因**不在链接、也不在运行时，而在前端**。

### 一、★ 根因：`extern` 被当成了定义

`curses.h` 里写的是 `extern WINDOW *stdscr;`（**声明**），
而链接产物里**同时**出现两个符号：

    77:  stdscr: .word 0                              ← 主程序自己生成的
    126: lib_curses_stdscr: .word lib_curses_sc_win    ← 库里真正的定义

- `ASTNode` 只有 `IsStatic`、**没有 `IsExtern`**
- `Parser.Statements.cs` 读取存储类的分支**只认 `STATIC`**
  （`TokenType.EXTERN` 与词法表项一直都在，只是从没人读）

⇒ `extern` 被当普通定义，**每个 include 了该头的使用者都在自己的数据段里
生成一个 `.word 0`**，把库里那份**真定义遮住** ⇒ 使用者读到的恒为 NULL。

**影响面远超 curses**：所有用 `extern` 声明跨模块全局变量的 C 代码。
（之所以一路没撞上，是因为要**同名符号同时存在于两个模块**才会暴露 ——
而 `stdscr` 恰好是"库里定义 + 头里 extern + 到处引用"的典型。）

探针实测（`VML_TRACE_DATA=1`，已删）：
`[DATA] key=lib_curses_stdscr ref=lib_curses_sc_win found=1 val=18768`
—— 库那边的**值一直是对的**，问题纯粹是「读到了另一个同名符号」。

### 二、同一条链路上的四段独立缺失（都已修）

为了让「取址初始化」（`T *p = &x;`）能真正工作，**四段都得在**，缺一段就断：

| 段 | 问题 | 修法 |
|---|---|---|
| 生成 | 初始化器分支没有「取址」这一支 ⇒ 落到 default 的 `.word 0` | 加分支 |
| 类型 | 不能用 `string` 顶替 —— 会落成 `.string "sc_win"`（一段**内容**等于标签名的字符数据，不是地址） | 新增 `LabelRef` |
| 解析 | `VmlAssembler` 里 `.word <非数字>` **什么都不做**（槽位直接丢失） | `ParseWordValue` ⇒ `LabelRef` |
| 运行时 | `ResolveDataElement` 与单值分派都不认识 `LabelRef` ⇒ 当 0 | 各加一支 |
| 链接 | 只改 key 不改值 ⇒ 运行时按旧名查标签表永远查不到 | `RemapLabelRefs` |

外加**延迟解析**：数据段是按 `DataSection` **遍历序**逐个分配内存的，
跨槽位的 `LabelRef` 可能在目标登记**之前**就被解析 ⇒ 查不到就记下来、循环后补填
（`DataSection` 是字典，**不该依赖遍历序**）。

⚠ 中途**撞过一次回归并回退**：一度把「裸标识符 = 标签引用」当通用判据
（改在 `ParseValue` 那一层），结果 `char *rows[] = {"abc","def"}` 的元素**全变 NULL** ——
说明那个判据**太宽**。`LabelRef` 的价值正在于它**明确知道自己是标签**，
不必靠名字形状猜。

### 三、`extern` 支持：**判据先行，一次到位**

第一版按「看起来对」改了三处（AST 字段 / 解析器 / 生成过滤），**产物却没变**。
**没有继续猜第四处**，而是按上一版写下的规矩**先打判据**：

    [EXT] name=stdscr IsExtern=False used=True inUsedVars=True

`IsExtern=False` ⇒ 问题**在解析侧**，不在生成侧。顺着查下去：

- **`Parser.Statements.cs`** 那条分支读的是 `storageClassToken`，认 `EXTERN` ✓
- 但 `curses.h` 的顶层声明走的是**另一条路** —— `Parser.Declarations.cs`，
  它把存储类读进了**局部字符串 `storageClass`**，而**从来没用它**
  （只用于 typedef 判断）⇒ `extern` 在那条路上**丢了**

补上之后 `IsExtern=True`、`inUsedVars=False` ✓，但立刻冒出**新错**：
`未声明的变量 'stdscr'` —— 因为代码生成查的是 `dataSection`，而 extern 变量
**已经不往里放了**。

⇒ 还需要**登记 extern 变量名**（`externVariables`），让代码生成仍能识别它们：
生成的仍是 `MEMORY(name)`（标签引用），由链接器解析到库里那份定义
（链接器会给库标签造裸别名）。三处查表点一并加上。

**结果**（20 号用例）：

    之前： S2=0    S4=0        （stdscr 恒为 NULL）
    现在： S2=1    S4=5207     （真实地址）

⚠ **中途一个自己造的坑**：用脚本批量替换 `dataSection.ContainsKey(x)` 时，
表达式被写成 `a && b || externVariables.Contains(...)` —— **`&&` 优先级高于 `||`**，
语义整个变了。已加括号。**脚本改代码必须回读**（本仓的老规矩）。

**剩下的**：`S`（`getmaxyx(w,…)` 读 `w->rows`）与 `S3`（`w != stdscr`）——
那是**另一个问题**：`initscr()` 的返回值与使用者读到的 `stdscr`
**不是同一个对象**，与 `extern` 无关。

**判据**：`vml-c-probe` **18 通过 / 0 失败 / 2 已知红**。

## v0.96.343 — C 前端两个真 bug（逗号表达式 / 取址初始化器）+ 给新接口补判据

用户提的一条：「**其实可以把新加的函数接口全部验证一遍再使用**」—— 照做写了
`scripts/vml-c-probe/cases/20-curses-api.c`（给 `curses.h` 这一批新接口每个至少一条判据），
**首跑就红了，而且是两个真 bug**。这一版把它们修掉。

### 一、★ 逗号表达式丢了左边的副作用（C 前端）

`Parser.Expressions.cs` 里原来是 `expr = right;` —— **把左边整个丢弃**，
于是左边的赋值/函数调用**根本不生成代码**。而"值"是对的（本来就该取右边），
所以症状极隐蔽。

实测 `curses.h` 的 `getmaxyx(w,y,x)` 展开是 `((y)=(w)->rows, (x)=(w)->cols)` ——
**标准头里这种宏遍地都是**（`getbegyx`/`getparyx` 同款）。丢弃左边之后 `y` 拿不到值，
调用方读到的还是自己栈上的哨兵值（实测 `-9`），而 `x` 恰好是对的 ——
**「同一句话里一个对一个错」正是这类缺陷的指纹**。

修法：新增 `CommaExpr` 节点，代码生成**先求左边（值丢弃）再求右边**；
`InferExpressionType` 同步取右边的类型。

### 二、★ 全局指针的取址初始化器没生成代码 + `LabelRef` 四段链路

`WINDOW *stdscr = &sc_win;` 的初始化器是 `UnaryOp("&", …)`，而
`CodeGenerator.Functions.cs` 的初始化器分支只认数组/数字/字符串，
**落到 default 的「未初始化 ⇒ 0」** —— 生成的正是一个 `.word 0`
⇒ **`stdscr` 在每个使用者那边都是 NULL**。

而 `stdscr` 是老程序最常用的全局对象：`cmatrix` 的节拍器
`timeout(0); wgetch(stdscr);` 里，`wgetch` 是 `win = w ? w : stdscr`，
NULL 让 `win->nodelay` 那条判断被跳过 ⇒ **非阻塞读键静默退化成阻塞**
⇒ 整类动画程序一帧都画不出来。

这是一条**四段链路**，四段都补上了：

| 段 | 问题 | 修法 |
|---|---|---|
| 生成 | 没有「取址」分支 ⇒ `.word 0` | 加分支 |
| 类型 | 不能用 `string` 顶替（会落成 `.string "sc_win"` —— 一段**内容**等于标签名的字符数据，不是地址） | 新增 `LabelRef` |
| 解析 | `VmlAssembler` 里 `.word <非数字>` **什么都不做**（槽位直接丢失） | `ParseWordValue` ⇒ `LabelRef` |
| 运行时 | `ResolveDataElement` 与单值分派都不认识 `LabelRef` ⇒ 当 0 | 各加一支 |
| 链接 | 只改 key 不改值 ⇒ 运行时按旧名 `sc_win` 查标签表永远查不到 | `RemapLabelRefs` |

⚠ **中途撞过一次回归并回退**：一度把「裸标识符 = 标签引用」当通用判据（在
`ParseValue` 那一层），结果 `char *rows[] = {"abc","def"}` 的元素**全变 NULL** ——
说明那个判据**太宽**。`LabelRef` 的价值正在于它**明确知道自己是标签**，
不必靠名字形状猜（这正是回退后在正确那层重做的原因）。

### 三、UTF-8 多字节序列被拆散（`curses.c`）

两个根因都是「字节落位」写错（**与「程序支不支持 UTF-8」无关**）：
- `addch` 里那条「UTF-8 续字节（0x80–0xBF）不推进光标」**放错了层** ——
  它改的是 `addch` 的**公开语义**（ncurses 契约：一次 `addch` = 一个字符 = 一列）。
  于是 `addch(0xB8)` 与随后的 `addch(0xAD)` 落在**同一个 `sc_cx`** 上，`B8` 被 `AD` 覆盖。
- 重构时 `sc_putwchar` 又让三个字节写**同一个格子**，而 `sc_ch` 是**字节**数组
  （`refresh` 就是"把这一行的 N 个字节原样发出去"）⇒ 一个字节 = 一个位置才自洽。

修法：抽出 `sc_cell_byte`（写一格，不推进）与 `sc_advance`（推进一列）两个原语，
`addch` 恢复「一次调用 = 一列」，`sc_putwchar` 逐字节走 `addch`。
判据：`tri.c`/`w0.c`/`wz.c` 从 `EF BF BD` → **`E4 B8 AD`**。

### 四、新判据 20 号：`curses.h` 这一批接口

19 条判据，**17 条已绿**。原则写在文件头：
**每个接口至少一条判据，且期望值独立推导** ——
前面十几轮反复迷路的根因就是「没有基线，只能从现象反推」，
而反推又反复被自己的探针/判据偏移误导。

### 仍为已知红

- **20 号的 `S`/`S2`**：`stdscr` 在**使用方**仍读到 NULL（链路的运行时那一环 ——
  链接后的文本已经正确到 `lib_curses_stdscr: .word lib_curses_sc_win`）
- `12-sscanf`（既有）

**判据**：`vml-c-probe` **18 通过 / 0 失败 / 2 已知红**。

## v0.96.342 — `dos.h` + `curses.h`/`ncurses.h` + 「让真实老程序一行不改跑起来」的环境补齐

三个头文件（`conio.h` 之后的第 2、3 名）加一批**环境缺口**。压这一版的判据不是自写探针，
而是**一个真实的老程序**：`tty-clock`（689 行，纯 curses）——**源码一个字符都没改**，
编译并运行通过。缺的东西全在**平台侧**补上，这才是"老程序不用改"的正确形态。

### 一、`dos.h`（含在下一节之前）

### 二、★ `curses.h` + `ncurses.h`（合计 83,712 个 C 文件）

常用子集、**单窗口 `stdscr`**（`newwin` 返回 `stdscr` 而不是 NULL —— 让"顺手 newwin 一下"
的老程序能跑）。与 `conio.h` **三处语义相反**，头文件里列了表：

| | `conio.h` | `curses.h` |
|---|---|---|
| 坐标原点 | **1 起** | **0 起** |
| 何时上屏 | 写完**立即** | **`refresh()` 才** |
| 颜色 | DOS 色序（要映射表） | **就是 ANSI 色序**（直接 +30/40，**套映射表反而错**） |

`ncurses.h` 只是转发 —— 两个名字本来就同一套 API，各写一份声明必然漂移。

### 三、★ 拿 tty-clock 压出来的「环境缺口」清单（**程序一行没改**）

真老程序假定 libc 就该有的东西，VML 这边一样都没有。补的全是这类：

- **头**：`ncurses.h`、`locale.h`、`sys/stat.h`、`sys/select.h`
- **缺的常量**：`LC_TIME`、`A_BLINK`、`KEY_UP/DOWN/RESIZE/…`、`STDIN_FILENO`、`SIGSEGV/SIGPIPE/…`
- **只有声明没有定义**：`stdin`/`stdout`/`stderr`（`stdio.h` 里是 `extern`）⇒ 链接期缺符号
- **缺的函数**：`localtime`/`gmtime`/`time`/`strftime`（自己写的日期算法，无时区）、
  `fprintf`、`strerror`、`getopt`、`strdup`、`atexit`、`nanosleep`、`sigaction`、
  `select`/`pselect`/`FD_*`、`stat`/`S_ISCHR`
- **curses 的宽字符变体**：`wattron`/`wattroff`/`wbkgdset`/`waddch`/`wgetch`/`box`/
  `wborder`/`newterm`/`set_term`/`use_default_colors`/`clearok`/`mvwin`/`wresize`/…

⚠ **`curses.h` 要带出 `stdio.h`** —— 真 ncurses 就是这么做的，而 tty-clock **自己并不**
include `<stdio.h>`，却用 `stderr`。这条不照抄，第一轮编译就报"未声明的变量 'stderr'"。

⚠ **`sigaction`/`select` 一律返回"成功"**：本平台没有信号、没有 fd 多路复用，
返回 -1 会让不少老程序当成致命错误直接退出；返回成功它们就正常往下走。

### 四、★ 两处实现上的坑（都是"照抄另一处"抄错的）

1. **属性缓冲得用 `int`，不能照抄 `conio.c` 的 `char`**：conio 的属性是 DOS 那套 **8 位**
   （高 4 位背景 + 低 4 位前景），一个 `char` 正好；而 curses 的属性是 **16 位**
   （低 8 位修饰位 + 高 8 位颜色对号 `COLOR_PAIR(n) = n << 8`）—— 用 `char` 存会把颜色对号
   **整段截掉**，整屏退回"白字黑底"而且**不报错**（实测：`init_pair(1, BLACK, CYAN)`
   之后标题栏打出来还是 `[37m[40m`）。
2. **`getch` 只能有一份定义**：curses 的 `getch` 与 conio 的**语义完全相同**，
   直接用 conio 那份（`curses.vml` 写 `.linked "conio.vml"`）—— 再写一个就是
   "谁赢不确定"（本仓头号坑）。
3. **`refresh()` 改成只重发脏行**：整屏 25×80 是两千多个字符，判定/调试都没法看；
   只画一行再 refresh 就只有 ~90 个字符。顺带让"每秒刷一次"的动画程序少发一大截。
   ⚠ 首次必须整屏（终端那边可能还是空白）。

### 验证

- 探针 `scripts/vml-c-probe/run.sh`：**19 通过 / 0 失败 / 1 已知红**
  （新增 `18-dos.c`、`19-curses.c`；后者特意**不调 `refresh()`** —— 用 `getcury/getcurx`
  读回缓冲光标来钉"坐标 0 起"这条语义）。
- `Examples/c/dos_demo.c`（时钟 + 进度条 + 五声音阶，音效需真机）、
  `Examples/c/curses_demo.c`（标题栏/边框/反白菜单/8 色色块）桌面实跑通过。
- **`tty-clock` 一行不改**：`✔ 编译完成（55435 条指令）✔ 运行完成`。
  ⚠ 画面仍是空的，正在查（怀疑 `newwin` 退化后它画到了我给的行列之外）。
- ⚠ **手机端要重装 APK** 才能验到这些（Lib 变了）。

### 五、`dos.h` 详情 + 实测纠正「`#55`/`#56` 是字符串指针，不是打包整数」

`conio.h` 之后的第二个头文件。按同一条规矩排的：含 `#include <dos.h>` 的 C 文件
**49,792** 个，紧跟 conio 排第 2，而且它与 conio **天然配套**（DOS 文本程序几乎都是
`gotoxy` 画界面 + `delay` 控节奏 + `sound` 出音效 一起用）。

### 一、只做**能映射到本平台**的，x86 专属的一律不做（写在明处）

| 做 | 底层 |
|---|---|
| `delay` / `sleep` | `SYSCALL #52` |
| `sound` / `nosound` | `SYSCALL #57` / `#542` |
| `getdate` / `gettime` | `SYSCALL #55` / `#56` |
| `DosVersion` | 恒 `0x0700`（老程序拿它判分支） |

**不做**：`geninterrupt` / `int86` / `intdos` / `keep` / `setvect` / `getvect` /
`dosexterr` / `union REGS` —— 全是 **x86 软中断与 TSR**，本平台没有中断向量表这回事。
判据写在头文件里（照 `conio.h` 那条「有限兼容，写在明处」的做法）。

### 二、★ 实测纠正：`#55`/`#56` 返回的是**字符串指针**，不是打包整数

`Lib/shared/src/dos.c` 里那版 `GetDate`/`GetTime`（Turbo Pascal 风格）把返回值当
**打包整数**做位段解析（`>>16` / `>>8` / `&0xFF`）。实测（今天 2026-09-21）：

```c
char* d = (char*)asm("SYSCALL #55");   ⇒  "2026-09-21"
char* t = (char*)asm("SYSCALL #56");   ⇒  "23:00:44"
```

它给的是**字符串的地址**——按整数解析等于把地址当日期。两个函数已改成按字符串解析
（`atoi` 从各字段的固定偏移取）。**上一版我在注释里写的"格式未实测"现在有答案了。**

### 三、★ `sound()` 与 DOS 的语义差别（最容易踩的一条）

DOS 的 `sound(f)` 是「**开始**持续发声，一直响到 `nosound()`」——状态是**保持**的。
本平台的音效原语是「响**固定时长**」（`#57` 的第二个参数，宿主侧 `ToneMaxMs = 5000`）。
这里取近似：`sound(f)` = 起一个**最长（5 秒）** 的音，`nosound()` = 立刻掐掉。

对老程序的**实际影响很小**：它们几乎都是 `sound(440); delay(100); nosound();`
这种"响一下就关"——**只要在 5 秒内 `nosound()`，听感与 DOS 完全一致**。
只有"`sound()` 之后一直不关"的写法会在 5 秒后自己停。

### 四、音效**桌面验不了**——如实说，别把"没崩"当"音效对了"

`sound`/`nosound` 走 `#57`/`#542`，而桌面宿主 `CliVmlHost.Tone()` **只写一行日志、
不出声**，`StopAudio()` 只打一行（这正是 `#57` 当初"调了没反应"的**桌面镜像**）。
所以探针里只验"调用不崩"，**听感必须真机**。

配套给了 `Examples/c/dos_demo.c`：画真实时钟 + 进度条（`delay` 看得见）+
**五声音阶**（`sound`/`nosound` 听得见）。跑法 `vml run examples/c/dos_demo.c`。

### 验证

- 新判据 `scripts/vml-c-probe/cases/18-dos.c`：`delay` 真的等了（拿 `get_tick()` 夹着量）、
  日期时间的字段**落在合理区间**（返回值丢了会得 0、解析错位会掉出区间、结构体布局
  对不上会字段串位 —— 三种真实故障全都接得住）。判据故意"松"（不判具体值），
  因为日期每次跑都不一样；但松得**够用**。
- `scripts/vml-c-probe/run.sh`：**17 通过 / 0 失败 / 1 已知红**。
- `Examples/c/dos_demo.c` 桌面实跑通过（日期 `2026-9-21`、`DosVersion` 1792、
  进度条 0→100%、音阶五档）。

## v0.96.341 — 补 conio 判据时挖出的三条「不报错、只是没反应」

本轮从"继续测 `conio`"开始。判据是补上了，但更有价值的是**顺出来的三个缺陷** ——
它们的共同形态是：**编译全绿、程序照跑，只是某个值悄悄不对**，而观感就是"程序没反应"。

### 一、★ 库函数从 syscall 取返回值：**写法错一种，返回值整个丢**（64 处 / 10 个模块）

查 `conio` 的键盘为什么收不到按键，一路查到 `getchar()` **恒返回 0 / 垃圾值**。
指令级 trace 把链条钉死了（`VMLTRACE=1` 逐条打 `pc/opcode/operands/寄存器`）：

```
11331  SYSCALL  #5         R0=0     ← 执行前
11332  MOVE     [mem],@R0  R0=65    ← 运行时确实把 65 写进了 R0
11333  MOVE     @R0,[mem]  R0=65    ← 执行前还是 65
11334  JMP      ...        R0=0     ← ✗ 变 0 了
```

真身是**汇编里的存读不同源**：`move [@R12-4] @R0` 存、`move @R0 [@R12-8]` 读 ——
`return c` 读的那个槽**从来没有被写过**。对应到 C 源码，就是这一行写法：

```c
int c; asm("SYSCALL #5"); return c;   /* ✗ c 是未初始化的垃圾 */
```

**库里其实有三种写法，只有第一种是坏的** —— 这是本轮最花时间、也最值钱的发现，
因为**按"文件名 + grep"去清会把能工作的代码一起改坏**：

| | 写法 | 状态 |
|---|---|---|
| ① | `int x; asm("SYSCALL N"); return x;` | **坏**（返回值丢） |
| ② | `return asm("SYSCALL N");` | 好 —— 规则白纸黑字写在 `vmlui.c` 头部，**那边一直是对的** |
| ③ | `asm("MOVE [_vml_result_int], R0"); return _vml_result_int;` | 好（显式搬运，`vmlsys.c`/`graphics.c`/`network.c`/`crt.c` 那 61 处**没动**） |

- 清掉 ①**64 处**：`os.c`(29) / `file.c`(7) / `basiclib.c`(5) / `console.c`(5) / `sysinfo.c`(5) /
  `device.c`(4) / `builtins.c`(3) / `io.c`(2) / `builtins_kotlin.c`(2) / `dos.c`(2)。
- **判据先行**：先写 `vml-c-probe/cases/17-syscall-ret.c`（拿 `vmlui.c` 的 ②/③ 当**对照组**，
  判据取"两种写法必须给同一个答案"），**看着它红**才动手；改完变绿。
- ⚠ **重复定义会让"改了一处"等于没改**：`random` 在 `builtins.c` 和 `sysinfo.c` **各有一份**，
  实测链接器选的是 `lib_sysinfo_random` —— 只修 `builtins.c` 那份，判据照样红。

### 二、★ 用户敲 `wsad`，只有 `w` 进得了程序：宿主 `ReadChar()` 一次消费一整行

`IConsoleIO.ReadChar()` 的契约是"读取**一个字符**"，而两端宿主（`vmlcli` 与手机端
`MauiVml.CaptureIo`）都写成了"读一整行、返回 `line[0]`"—— **其余字符直接丢掉**。
放大到使用者那一层就是：`conio` 的 `getch()` 是拿 `getchar()` **循环攒一行**进键盘缓冲的，
宿主一次只给一个字符 ⇒ **玩家敲的方向键只有第一下生效**，而且不报错。

改成**逐字符吐**（行末补 `'\n'` 当"按了回车"，正是 `getch()` 循环等的收尾符）。两端同步改，
手机端语义与桌面**逐条对齐**（这是 `vmlcli` 存在的意义）。

### 三、★ `dos.c` 编译不过：`STORE` 不是有效指令 + 变量从未声明

`asm("STORE R0, _vml_result_int")` —— 指令名写错，且 `_vml_result_int` 在本文件里
**根本没声明**。它是 `GenLib -b` 整条流水线上**唯一的红点**（每次都在这里报 2 个错）。
顺带清掉了同文件里的调试残留：`*year = 2026; *month = 8;` 会把上面刚算好的值**覆盖掉**。

### 四、conio 判据补齐（此前只有坐标模型一条）

- `13-conio.c`：判据行被库画的字符**粘在同一行**（`menuP2=14,5`），而行首锚定的 grep
  匹配不到 ⇒ 改用例打印前先换行。
- `14-conio-color.c`：DOS 16 色 → ANSI SGR **逐条**。期望值按 CGA 色序 vs ANSI 标准色序
  **独立推导**（不抄实现那张表）—— 那张表手工错过位，`textbackground(CYAN)` 会打成黄底。
- `15-conio-lines.c`：`clreol`/`delline`/`insline`/滚屏。这三个走影子缓冲**整行重发**，
  没有任何别的观测手段（全是 `void`，返回值这条路本来也走不通）；期望串里的 80 格空格
  是**真契约**，用脚本生成保证数量准确。
- `16-conio-key.c`：敲 `wsad` ⇒ 四次 `getch` 依次拿到 119/115/97/100，第五次是空行的 13。

### 五、探针装置三个"假结果"源（都会让人读到**看起来合理**的错结论）

1. **`timeout` 不是通用命令**（GNU coreutils；macOS 自带没有）⇒ 每个用例都是
   `command not found`、判据"实得为空"，看上去像全红，而真去查用例又都是对的。
2. **`2>&1` 把编译日志混进判据** —— `vmlcli` 的契约是 `stdout=程序输出 / stderr=编译日志`。
3. **`grep -o 'STDIN: .*'` 先命中了用例注释里那句示例**，而那段说明文字的**前四个字符恰好也是
   `wsad`** ⇒ 前四条判据照样绿，只有第五条露馅。**判据装置自己骗人比代码出错更难查**，
   已锚行首（`^// STDIN:`）。

### 验证

- `scripts/vml-c-probe/run.sh`：**16 通过 / 0 失败 / 1 已知红**（本轮起点是 11 / 0 / 2）。
- **顺带收益**：`alloc()` 修好后，既有的 KNOWN-RED **`07-heap-str` 自己转绿**
  （它坏的正是"分配到的地址是垃圾"）；`12-sscanf` 是剩下那条，与本轮无关。
- `GenLib -b` 流水线**零失败**（此前 `dos.c` 每次报 2 个错）。
- MAUI `net10.0-android` 编译 0 错误。⚠ **手机端的 `CaptureIo` 修复要重装 APK 才验得到**，
  桌面侧已验、两端语义已对齐。
- 留档（未动生成器）：`GenLib` 重生成会**丢掉 `Lib/shared/*.vml` 的 `.linked` 头**，
  本轮用"备份头部 → 重生成 → 插回"绕开；`check-vml-patches.sh` 的判据①可能因此对不上。

## v0.96.340 — 命令行页：**触摸全废与画布不裁剪**两个真根因 + 菜单键 / 光标 / 双向滚动条

用户报「好像卡死了」。查下来**不是死机** —— 按钮、输入框一切正常，是**输出区那块画布
对触摸完全没有反应**。两个互相独立的缺陷，加上这一轮点名的几项能力。

### 一、★ 触摸全废：画布上**挂任何手势识别器**都会让 `Start/Drag/EndInteraction` 集体失效

命令行页当初为了"点一下聚焦输入框"，给画布挂了一个 `TapGestureRecognizer`。
实测出来的硬约束是：**只要给这个 `GraphicsView` 挂上任何手势识别器，平台那层的触摸就被
手势系统接走，画布自己那套 `StartInteraction`/`DragInteraction`/`EndInteraction`
一个都不再触发** —— 于是滑不动、也捏不动，而界面别处一切正常（用户看到的"卡死"）。

- 反证很干净：编辑器画布 `CodeCanvasView` **一个手势识别器都没挂**，它一直是好的。
- 改法：删掉它，"点一下"改成画布自己在 `EndInteraction` 里**按位移判**（编辑器同款），
  对外暴露 `Tapped` / `PinchScaled` / `PinchEnded` 三个事件。
- 捏合基准**懒设在 `OnTouchDrag` 里**（照编辑器抄的那一条）：平台不一定在第二根手指落下时
  再发一次 `StartInteraction`，只靠它设基准的话 `_pinchStartDist` 恒为 0、每一拍都被早退吃掉。

### 二、★ 画布不裁剪：滚出上边的内容压在顶栏按钮上

平台**不会**替我们裁 —— Android 只在 `ViewGroup` 那层裁"子视图超出父容器"的部分，
不管子视图自己往外画。往上滚之后画面就盖在「自适应 / 横向固定 / 固定 80×25 / 菜单」那一排上。
加 `ClipRectangle(0,0,w,h)`，顺带**只画可见行**（输出几千行时省掉整趟无谓解析）。

### 三、★ 缩放后横向滚不动：页面把**画布视图本身**撑到和内容一样宽

`RenderOutput` 里还留着"给 Label 一个显式宽度"那句（`OutputGrid.WidthRequest = 内容宽`）——
那是输出区还套在 `ScrollView` 里的做法。画布接管滚动之后**意思完全反了**：
`Width >= 内容宽` ⇒ `maxX = 内容宽 - 视口宽 = 0` ⇒ 横向不需要滚、滚动条判据恒为假。
内容被**父容器**裁在屏幕外，而画布自认为全都看得见（用户报的「缩放后横向滚动条没出来、滚不回去」）。

- 正解：画布宽度**永远是视口宽度**（布局给的），内容尺寸另有 `_contentW/_contentH`，
  超出部分由画布**自己裁剪 + 自己滚**。
- 连带清掉一整段死代码：`ContentWidth`/`ContentHeight`/`NewTextLabel`/`GroupByNoWrap`
  与那组滚动条拖拽字段（都是 `ScrollView` 时代的遗留，页面这边已无人读）。

### 四、菜单键（原「清屏」）· 字号 6~96 · 双向滚动条可拖 · 光标

- **顶栏「清屏」→「菜单」**：加大字号 / 减小字号 / 重置字号 / 复制整屏输出 / 清空屏幕。
  加一项不必再加一个按钮；「复制整屏输出」还是**非它不可**的一项 —— 输出区是自绘画布、
  没有文本选择，不给入口用户就**拿不走**看到的输出。
- **字号范围 6~96**（用户定的）：`FontChoices` 铺满全程、下半段密上半段疏
  （小字号差 1 磅就是"多塞一列"，大字号看不出来）。`ShellWrap.FontSizeForColumns`
  的钳位跟着改，自测的期望值一并更新。
- **滚动条自己画、也能自己拖**：几何只有一份（`VerticalThumbRect`/`HorizontalThumbRect`），
  画 / 命中判定 / 拖动换算三处同源；可见条宽 3、**命中热区外扩 22**（手指按不住 3 像素）；
  拖动中那条画成激活色。**两个轴都是"内容超了才出现"**（用户定的原话）。
- **光标**：`ESC[?25h/l`（DECTCEM）以前是**整类被忽略**的（`param[0]=='?'` 一律 continue），
  现在认 25（并可串写 `?25;1049l`），`FrameBuffer` 暴露 `CursorRow/CursorCol/CursorVisible`，
  经 `MauiVml.LastCursor` 旁路到画布并画一个半透明方块。程序关掉就不画。

### 五、屏幕模式决定落点（用户定的语义）

- **固定屏幕**（行列钉死）= 老显示器 ⇒ 贴**屏幕左上角**；
- **行不固定**（滚屏）= 真终端 ⇒ 停在**最后一屏**；
- 内容比窗口小时两者自然重合：**横向左对齐、纵向顶部对齐**（`Math.Max(0,…)` 与
  `_scrollX=0` 在装得下时都算 0，不必为它写特判）。
- 切模式、改字号、新输出之后一律走同一个 `AlignOutput()`。

### 六、惯性（"物理反馈"）· 字号落盘的第三条路

- 用户点名的「**滚动少了物理反馈**」：手指一松画面就定住。惯性**照抄编辑器**那组常量与算法
  （`VelocityWindowMs=100` / `MinFlingVelocity=40` / `FlingStopVelocity=60` /
  `FlingLaunchGain=2.6` / `FlingFriction=0.98`）—— 那组数是实测调过两轮的，另凑一套只会两边手感不同。
  松手速度取**最近 100ms 的位移 ÷ 时间**（不是"总位移 × 系数"：轻扫位移小 ⇒ 估出≈0 ⇒ 几乎不滑）。
- 顺手修掉一处**漂移**：捏合的收尾写在 `PinchEnded` 上，而那个事件**不保证一定来**
  （手势被别处接走 / 页面被切走）。实测漂过一次 —— 屏幕字号 96、存储里还是上一次的 6.75，
  于是"自适应 N 列"按存储算出来是 **96 列**（而字大得离谱）。两条修：
  ① 标签改读**页面自己的 `_fontSize`**（屏幕上多大是它说了算，存储只管"下次进来用多少"）；
  ② **`OnDisappearing` 也落盘** —— "一次性收尾挂在结束事件上"的第三条路就是"离开这一页"。

### 验证

- 模拟器：画布尺寸从 `0×0` 变 `384×711`；单指拖动滚动、到边界钳住；点一下聚焦输入框、
  软键盘弹出；conio 画面四边框 / 反白菜单项 / 16 色条全部对齐且**不再压顶栏**。
- 自测 **6320 项全绿**（含新增的 DECTCEM 六条：默认显示 / `?25l` 关 / `?25h` 开 /
  串写要逐个认 / 别的私有模式不许误伤）。
- **双指缩放由用户真机确认可用**（字号 27 = 非档位值，只可能来自连续手势）。
- 真机上 `adb` 的两种多点注入都不可用（`input` 是单指；`sendevent` 被 SELinux 拦），
  所以**捏合之外的触摸行为靠模拟器 + 用户真机确认**。

## v0.96.337 — 老 TTY 程序画面空白**查到根**：C 前端多级指针下标展平 + 画面行不许折行

用户真机报的「老程序兼容性问题」。两件事、两层，**都不是渲染坏了** ——
网格、配色、`Label` 三层从头到尾都是好的，坏的是**程序自己读到的数据**。

### 一、★ 真根因：`char **` 的多级下标被展平成一个偏移（C 前端）

`nyan_show(char ** fr)` 里的 `fr[y][x]` 编译成 `*(fr + (y+x)*4)` —— 两个下标
**先相加、再乘一次步长**。而 `fr[y]` 指向的串与 `fr` 的偏移毫无关系，展平在语义上
就是错的。实测症状：三帧只发出三个 ESC，着色字符一个没读到（`Lib` 与探针都验过）。

- 判据收成**一份** `ElementIsPointer`（`T *x[]` 与 `T **x` ⇒ 元素就是指针），
  三处共用：元素类型、步长、以及**多级下标时基址取地址还是取值**。
- 配套三处同源修正：`x[i][j]` 的类型要**再解一层**（拿 `fr[i]` 的 `CharPtr` 定步长会
  按 4 走 ⇒ `rows[1][1]` 直接读到串尾之外，打印成空）；指针元素步长 = 4；
  全局**指针变量**当下标基址时要取它的**值**（`g_rows[1]`），不是它的槽地址。
- 探针 `vml-c-probe/02-ptr-ptr.c` 从全红转全绿：`Q1=abc|Q2=e|Q3=def|Q4=c|Q5=def`。

⚠ **连带**：`Lib/shared/cli.vml`、`Lib/shared/string.vml` 一直是用**坏的前端**生成的
（`str_split(char** parts)` 写指针用 `moveb`，只写最低一个字节）。已重跑 `GenLib -A`
落盘、重打 `vml_lib.zip`（否则手机上跑的仍是旧标准库），`check-vml-patches.sh`
判据①（`Lib == f(源码, 前端, GenLib)`）随之变绿。

### 二、画面被折行折散了（命令行页呈现）

用户原话：「有彩色了，但有点乱」—— 彩虹与猫的身体都出来了，形状却是剪开的。

- 全屏程序的每一行就是屏幕上的一行，而 `DisplayText` 对**所有**行按列折行 ⇒
  一行变两行、后面整体下移。新增 `ShellWrap.IsPictureLine`（可见字符全是空格
  **且**有 `«…»` 标记）：**画面行不折**，并把 `Label` 撑到画面那么宽，超出就横向滚
  （「显示不全出滚动条」本来就是定好的规矩）。
- 同源的一条：Markdown 的**缩进代码块**会把"左边一片空白 + 一串底色标记"的网格行
  整行吃掉 —— 代码块是逐字文本、**不解 `«»`**，标记载体会原样打到屏幕上。
  缩进代码块那条分支现在跳过带真标记的行；反方向（真的 `    int x = 1;`）仍照旧判成代码块。

### 三、判据

自测 **6314 / 0**（新增 12 条：网格过 Markdown 解析必须是一个段落 + 零代码块、
画面行 vs 文本行、可见宽度）。四套桌面探针（c / abi / basic / shared-lib）全过。
模拟器实测：**固定 80×25** 下 nyancat 形状完整（馅饼 + 糖粒 + 彩虹 + 猫脸）。

### 四、如实记：还剩什么

- `sprintf` 的 `%` 转换多吐一个格式字符（`vml-c-probe/08`，KNOWN-RED）
- 堆指针过 `%s` 打成 `(null)`（`vml-c-probe/07`，KNOWN-RED）
- **curses 库还没有** ⇒ `sl` / `cmatrix` 那类暂时编不了
- ~~自适应模式给全屏程序的终端尺寸是屏幕推导出的列数~~ —— **已修**（见下面第五节）

### 五、同批：`sprintf` **不写结尾符**（老程序"先拼串再输出"全都会被喂残渣）

`vsnprintf` 返回的是**长度**（不含结尾符），而 C 的 `sprintf` 契约是"写一个以 NUL 结尾的串"。
实现里却是 `return vsnprintf(...)` —— **一个结尾符都不写** ⇒ 目标缓冲区后面残留什么，
串就"长"成什么。

- **实测形态**：`strcpy(buf,"abcd"); sprintf(buf,"%d-%d",3,7)` 得到 **`3-7d`**（长度 4 而非 3）
  —— `d` 正是上一句 `strcpy` 留在 `buf[3]` 的残渣。
- **最坑的地方是它"换个上下文就自己变绿"**：单独写同一句是对的（缓冲区刚分配、后面本来就是 0）
  ⇒ 孤立用例永远抓不到。`scripts/vml-c-probe/08-sprintf.c` 因此**刻意带着前置字符串操作**
  （这一点写在那条用例的注释里，是它没被洗掉的原因）。
- 影响面：状态栏、日志、以及**任何兼容层里的 `cprintf`**（先拼串再输出）都会多尾巴，
  且多出来的内容**取决于上一次往该缓冲区写过什么** —— 典型的"编得过、跑起来才错"。
- 判据：`vml-c-probe` 从 **7 通过 / 3 已知红** 变成 **9 通过 / 1 已知红**
  （剩下那条是堆指针过 `%s` 打成 `(null)`）。

### 六、自适应档不再播报推导出来的终端尺寸（用户提的「按建议来」）

给全屏程序的**终端尺寸**改播 0 = 未知 ⇒ 退回经典 80×25（行同理）。老程序写死 80 列、
根本不会自适应，告诉它"你的终端只有 50 列"，它照样一行吐 80 个字符 ⇒ **它自己的输出
就在网格里折了**（画面斜切）。用户显式选固定档时照播不误 —— 那正是"我就要这个尺寸"。
模拟器实测：**自适应 50 列档下 nyancat 的形状与固定 80×25 档完全一致**。

### 七、变参改走标准 `va_list`（把上面那条规矩**落地**）+ 补 `snprintf` + 新发现 `sscanf` 写不回

`printf` / `sprintf` 原先用 `int *stack_args = (int*)&fmt + 1;` **自己算形参地址**取变参 ——
正是上一条规矩禁掉的形态。改成 `va_start` / `va_arg`（前端内建实现，`stdarg.h` → `__builtin_va_*`），
变参先读进显式的 `int args[]` 再交给 `vsnprintf`。`scanf.c` 一直是这么做的，本次把 `printf`
家族追上。同批把"数格式串里有几个消耗实参的转换符"**收成一份** `format_arg_count`
（两份的后果是数错一个 ⇒ 变参表**整体错位一格**，且没有编译期提示）。

- **`snprintf` 此前只有声明没有实现**（`stdio.h:55` 声明、全 `Lib/` 无定义）⇒ 一用就链不上，
  而它是"安全拼串"最常用的那个。已补，返回值按 C99。
- **新发现（KNOWN-RED）**：`sscanf` **一个变量都不写回**（`v` 仍初值、`w` 仍空串、返回 0）。
  全仓此前对 `sscanf` **零覆盖**，所以是个一直在的缺陷（与本次改动无关 —— 同文件的
  `printf`/`sprintf`/`snprintf` 五条全对，而它们共用同一条判据）。影响面：INI、`key=value`、
  成绩单、存档那类"解析一行"的写法，**静默不写**比报错更难查。
- 判据：`vml-c-probe` 新增 `11-printf-args.c`（全绿）与 `12-sscanf.c`（KNOWN-RED），
  套件 **10 通过 / 0 失败 / 2 已知红**。

**同批定案（用户）**：「**自接读写地址的代码不要**」—— 已写进 ROADMAP 第零节当硬规矩。
同一处的 `printf` 家族用 `int *stack_args = (int*)&fmt + 1;` 取变参正是这个形态
（注释里记着它已经因此读偏过一次）。正解是让前端把变参**按显式数组**交给库函数，
而不是库函数自己拿形参地址算偏移 —— 排进兼容层的下一步。


## v0.96.336 — 网格记下 **256 色 / 真彩**（画面空白的**第一层**根因）+ 文字竖对齐（新号 #581）

### 一、`FrameBuffer.ApplySgr` 把 256 色与真彩**整个跳过**

代码里明写着「38;5;N / 48;5;N / 38;2;R;G;B —— 跳过后续参数，不做精确记录」⇒
一切用它们作画的程序在网格里**一个颜色都没有**。而这类程序"画"的正是
**带背景色的空格**：底色一丢只剩空格，转成标记后**整段被 TrimEnd 掉** ⇒ 屏幕全白。
（`tty_legacy.c` 那条看着正常，是因为**它那段里有文字**，底色丢了也还有东西。）

- 认两种形状并把 **256 色当场换算成真彩**（表示法收成一种，下游四条不必再认第二种编码）
- `DumpAnsi` 写回时真彩必须走 `38;2;r;g;b` 这个形状（把 `0x1000000|rgb` 当裸码写出去
  是**另一个意思**）；**16 色原样不动**（不许被改写成真彩，否则老程序配色会漂）
- 中途先怀疑错了方向（以为移动端渲染丢空格背景，加了 NBSP 那条）—— 那一条**留着**
  （对"纯空格行"确实有用），但它**不是**根因，加完画面照样空白

### 二、文字增加**竖对齐**（新号 #581）

用户实测报的：程序写"横中"时**横向居中了、纵向却顶着 y**（摆在方框正中看着偏上）。

- **为什么要新号而不是给 #528 加参数**：老程序只传前六个参数，**第七只寄存器里是它
  自己上一句留下的值**，宿主无从判断"这是不是真给了" ⇒ 加参数会让跑得好好的老程序
  变成未定义行为（本仓记过多次的铁律）。
- 协议 `DrawTextEx = 581`：`x y 文本* 颜色 字号 横锚点 竖对齐(0顶/1中/2底) 样式位`。
  ⚠ 寄存器排布与老号**不同**（老号 R6 是样式位），两条包装函数的参数序别互相照抄。
- 偏移**只一份**（`DrawParse.TextVOffset`，SVG / 光栅 / 矢量三条后端共用）；
  `VAnchor` 默认 `top` = **老行为**，所以这是**纯增量**，老程序一个像素不变。
- DSL 用 `vtop`/`vcenter`/`vbottom`，**刻意不与横锚点的 `middle` 重名**。

### 三、真机结论（**部分修复，如实记**）

修复后模拟器上出现了一个**深蓝色块**，颜色与预言值精确相符（`48;5;17` → `rgb(0,0,95)`）
—— 修复前这里是全白，所以"颜色没被记下"这条**确认修好**。但整幅**只画出一小格** ⇒
还有一层没解决。**那一层在 v0.96.337 里才查到**（是 C 前端读错数据，不是渲染）。

自测 **6302 / 0**（新增 4 条，含一条反方向的「16 色不许被改写」）。

## v0.96.335 — 命令行页三档尺寸 + 全屏输出网格 + 两轴滚动条

### 一、尺寸模式从两档改**三档**（都不固定 / 横向固定 / 都固定）

原先只有"自动 vs 固定"，把**横向固定**这一档漏掉了 —— 它不是可有可无的中间态：
老 TTY 程序按 80 列排表格 ⇒ **列必须钉死**，而手机屏幕高度各家不同 ⇒
**行没必要跟着钉死**（钉死了输出区上下留白）。用户点名的原话是
「都固定，或者横向固定，或者都不固定」。

- 模式是**一等公民**，生效列行由它推导（`ShellSize.EffectiveCols/Rows`），
  各轴已选值另存 —— 不这样的话会出现"模式说自适应、值却是 80"这种自相矛盾的状态。
- 纯逻辑下沉到 `UI/Shared/Terminal/ShellSize.cs`：四端同源 + 桌面自测 8 条
  （模式→生效列行、老配置迁移、循环切换、文案互不相同）。

### 二、全屏输出按**网格**渲染，而不是折行堆叠

`nyancat` / `sl` / `nethack` / 一切 curses 程序是"一屏一屏画"的，折行与回滚缓存
对它们**都是错的**（折行把画面搅烂、堆叠起来就是几十屏残影）。

- 判据 `UI/Shared/Terminal/ScreenOutput.LooksFullScreen`（纯逻辑）：只认**光标定位 /
  擦屏类** CSI（`H f A B C D G d s u` 与 `J`）。⚠ **刻意不认 `ESC[K`**（擦到行尾）——
  它是 `
` 进度条的常客，判成全屏会把普通构建输出也拽进网格；私有模式同样不认。
- 分流点在 `MauiVml.RunProgram`：命中就建 `FrameBuffer(rows, cols)`（现成的终端模拟器）
  再 `DumpAnsi()` 转标记。终端尺寸由命令行页播报（0 = 未知 ⇒ 退回 80×25；
  做成静态的依据与 `OnProgress` 相同：**VML 执行是排他的**）。

### 三、滚动条与"老显示器"语义

- 两轴**一条判据**（内容装得下就不显示）。修了视口**没扣 `ScrollView` 自身内边距**
  （横 28 / 竖 8）—— 拿外框尺寸当视口时，内容被切掉一截而滚动条不出现。
- 固定高度 = **老显示器**：屏幕上那 N 行就是全部，滚过了的就没了（用户点名）。
  显示层只取**折行之后**的最后 N 行（按逻辑行裁会漏：一条长命令折成 5 行）。

## v0.96.332 续 — 同版本内续做的两批（汇编器 `;` 截断 / BASIC 文本控制台垫层）

### 一、汇编器两处「同一规则多份实现」

- **`.string "a;b"` 被当行内注释截断**：三处剥注释里只有 `ProcessSingleLine` 是
  **引号感知**的，`ParseLine` / `ParseData` 是裸 `IndexOf(";")`。影响面是**整类 ANSI
  转义**（`1;31` / `38;5;208` / `38;2;r;g;b` 全是分号分隔）⇒ 256 色与真彩**静默失效**。
  已收成唯一一份 `StripLineComment`（`;` 与 `//`、单双引号各自成对跟踪）。
- **`; N:` 源码行号捕获是死代码**：它挂在剥注释之后，而整行注释剥完就早退 ⇒
  `Instruction.SourceLine` 恒为 -1。挪到剥注释之前。

### 二、C 前端：指针表 `char *rows[]` 的**三处独立缺陷**

修完 `rows[0]` 从 `(null)` 变 `abc`：① `RemoveUnusedData` 只按**指令**的操作数算可达，
而指针表里的字符串**只被别的数据项引用**（`.word L_x`），指令里一次都不出现 ⇒
整条被删（已补"数据段内部引用"的传递闭包，走到不动点）；② `FlattenArrayInitializer`
把字符串**内容**直接塞进数组、序列化成 `.word abc`（把内容当标签名写，而那个标签
不存在）⇒ 改成给字符串分配数据段标签；③ `char *rows[]` 的元素类型被判成 `char`
（`StringToExprType` 先剥 `[]` 再看 `*`）⇒ 下标按**字节**读写，读指针只读到最低一个字节。

### 三、BASIC 老程序的**文本控制台垫层** + 机械转换器

给 1970~80 年代那批"打字机式"老程序（`PRINT` 顺序打字 + `INPUT` 读一行）做的一层可移植
文本控制台（`Examples/basic/_tty.bas`）+ 转换脚本 `scripts/basic-port.py`
（`PRINT`→`ttyP`/`ttyPn`/`ttyPc`、`INPUT`→`ttyAsk`、`RND`、`CLS`，并注入垫层）。

垫层改用 `ui_*` 那几个号，因为老程序的两条输出模型在本平台**都不成立**（实测）：
`PRINT` 走 stdout 而窗口化运行时 stdout 被宿主重定向进内存缓冲 ⇒ 窗口里看不见；
`LOCATE`/`COLOR`/`CLS` 写的是 `0xB8000` 那段 VGA 文本显存，宿主把它压成 1×1。

## v0.96.332 — 移植 NIBBLES（**五个 QBasic 经典全部落地**）+ 补上一条常量折叠的漏洞

### 一、移植 NIBBLES / 贪吃蛇（`Examples/basic/nibbles.bas`）

自检 **14 条全对**：坐标往返、四个方向增量、不许反向、关卡需求与速度曲线、
边界墙、空格数、普通/奖励计分、一路撞墙必死。

**手机上怎么玩照的是仓库自己踩过的坑**：**不自己画手柄** —— 绘图窗口底部本来就有一排
屏幕手柄，程序只该收按键消息。自绘那套的代价是三重：占画面高度、几何要在"画"与"命中"
两处各算一遍、每个游戏还各画一套风格（`tetris` 那里已经删过一次自绘手柄）。
所以这里用 `ui_win_open_ex(..., 0, 1)` **要系统手柄区**、方向键走按键消息；
另外补一个**滑动改向**给纯触摸的场合。

### 二、★ 补上一条常量折叠的漏洞（v0.96.331 那修的**漏洞本身**）

上一版把「用 CONST 当数组维度」修成"解析期查常量表"，但**只收字面量** ⇒
`CONST GCELLS = GW * GH` 这种**引用其它常量**的写法进不了表。后果很隐蔽：

- `DIM bd(GCELLS)` 的尺寸**是对的**（解析期查得到表里的 GW/GH 自己折的）；
- 而**代码生成期**用的是它自己那份常量表（从 `ConstStatement.Value` 建的），
  `GCELLS` 不在里面 ⇒ `WHILE i < GCELLS` 读成 0 ⇒ **循环一次都不跑**。
  实测：自检里"空格数"打出 0、棋子右下角不是墙。

**修法**：加一个小常量折叠器（`字面量` / `已登记的常量名` / `+ - * /`），
并且**折叠成功就把 AST 节点就地换成字面量** —— 这样解析期与代码生成期**同源**，
不用让代码生成也实现一遍折叠（那又会变成"同一规则两处实现"）。

### 三、新发现一条缺陷（**已确认、未修**，登记为 KNOWN-RED）

🔴 **字符串函数的返回值共用 `_buf1`**：`STR$(1) + "/" + STR$(2)` 实测打出 **`1/1`**
（应 `1/2`）—— `STR$`/`LEFT$`/`MID$`/`CHR$`/`UCASE$`… **全都返回同一个 `_buf1`**，
同一个表达式里两次调用必然互相覆盖（谁后写谁赢）。

与已修的「拼接结果共用 `_buf3`」是**同一家族**，但修法不同：拼接那边前端能给**每个拼接点**
分配槽位（`basic_concat_slot`），而这些是**库函数**、调用点信息进不来。可行路径要么给它们
一个**轮转池**（常见写法 2~3 个就不会撞），要么照 concat 的形状让前端传槽位。
NIBBLES 的进度显示原本就是被这条打花的（`0/5` 显示成 `0/0`）。

**判据管理**：`cases/28-strfnbuf.bas` 标记 `KNOWN-RED`，`run.sh` 新增一档
**"已知红"** 单独统计、**不计入通过/失败** —— 否则「N/N 全绿」这个信号就被一条
已知项永久污染（`vml-abi-probe` 同款处置）。留它在套件里的价值是**钉住症状**，
修好那天它会自己变绿。

### 四、判据

| 判据 | 结果 |
|---|---|
| `scripts/vml-basic-probe` | **23 通过 / 0 失败 / 1 已知红** ✓ |
| `vml-diag-probe/examples-build` | **91 / 92** ✓（基线 85/86 + 6 个新示例，失败项仍是 `forth/parserexp_demo.fs`）|
| `vml-out-probe` 跨语言输出 | **31 / 31** ✓ |

### 五、五个 QBasic 经典总览（`Examples/basic/`）

| 游戏 | 自检 | 看点 |
|---|---|---|
| `donkey.bas` 跑车躲驴（1981） | — | 最小，用来跑通"新游戏骨架" |
| `hammurabi.bas` 汉谟拉比（1968） | 10/10 | 回合制分阶段，`SELECT CASE` + 字符串拼报告 |
| `lander.bas` 登月（1969） | 12/12 | 连续物理（重力/推力/燃料）+ 三条着陆判据 |
| `blackjack.bas` 21 点 | 16/16 | A 的两面性、比大小五种情形、洗牌无重复 |
| `nibbles.bas` 贪吃蛇 | 14/14 | 关卡制 + 走系统手柄 |

五个都是**照玩法规则重写的**（原版 GORILLAS/NIBBLES 是 `Copyright (C) Microsoft 1990`、
1970s 那批出处更乱，而 `Examples/` 会随 APK 分发）。
**手机适配的通行做法**：原版打字输入的一律改成触摸条；"按住不放"必须用**按下+抬起
两个事件**维护状态（只认"按下"的话拇指按住时没有后续事件，推进只生效一帧）。

## v0.96.331 — BASIC 前端：再修 2 条（`EXIT` 在 SUB 里失效 / CONST 当数组维度）+ 移植 21 点

接着 v0.96.330 那轮修。这一批的两条都是**写游戏时被逼出来的** —— 共同的形态还是那句
「**SUB 体才是重灾区**」，而且两条都**不报错**。

### 一、★ `EXIT WHILE` / `EXIT FOR` 在 SUB/FUNCTION 里失效

**根因**：`GenerateSubWhileStatement` **从来没有调 `Sta.PushLoopLabels`** ——
紧挨着的 `GenerateSubDoLoopStatement` 一直有，就 WHILE 这条漏了。于是 `EmitBreak`
见循环栈为空 ⇒ **一条指令都不发、静默空操作**（顶层 WHILE 一直是对的）。

**症状分两极，都很能骗人**：

| 循环形态 | 表现 |
|---|---|
| **唯一的出口就是那句 EXIT**（`WHILE sum > 21`） | **死循环** —— BLACKJACK 的 `handValue` 实测卡死在自检第一处调用上、整轮超时 |
| 条件也会自己结束的 | **多跑完剩下的圈数** —— 最小复现 `f(4)` 应返回 5，实测 **100** |

⚠ **这条我误判过一轮，记下来**：先是看到"自检只打了标题就卡住"，推断成
「`EXIT WHILE` 支持得不好」；做了最小复现却发现**顶层完全正常**（`W=5`、`F=4`），
于是又反过来怀疑是自己写错、把绕法留在了文件里。**直到把"顶层能过、SUB 里不过"
这个差异本身当成线索**，才定位到漏登记循环标签。
**教训：最小复现如果"没复现"，先别下结论说"没问题" —— 换个上下文再试一次**
（这次是"顶层 vs SUB"这个维度）。

### 二、★ 用 CONST 当数组维度不生效

**根因**：解析 `DIM a(N)` 时，标识符维度被写死成 `lowerBound = 1` **占位**，
注释还写着"实际大小在代码生成阶段算"—— 而**那个阶段根本没人算** ⇒ 只分到 2 格。

**这条会静默写穿数组**：LANDER 实测循环变量一路跑到 `c=760`（= `sh` 的值）、
把地形数组**和邻居变量一起写花**，画面表现是"地形画到天上去了"。
修法：解析器立一张 `_constValues`（只收字面量 CONST），`DIM` 的维度查它；
查不到就退回占位行为（不更糟）。顺带把 LANDER 改回惯用写法 `DIM terr(COLS)`。

### 三、更正一条**说错了**的判断

v0.96.330 / 上一批汇报里写的「**无参 `FUNCTION` 返回 0**」**是错的**：
`FUNCTION three() AS INTEGER` 用**带括号**调用（`three()`）本来就是好的（`A=7`）。
真正不同的是**裸名调用**（`PRINT three`）会当成**变量**读、得 0。
⇒ BLACKJACK 里那两处"绕法"（标志位替 `EXIT WHILE`、占位形参替 `deal()`）**全都不必要**，
已改回惯用写法并复验自检 16/16。

### 四、移植 BLACKJACK / 21 点

`Examples/basic/blackjack.bas` —— 自检 **16/16**：牌编码往返、**A 的两面性**
（A+K=21 / A+9=20 / A+K+K=21 降成 1 / A+K+5=16 仍按 11）、爆牌、庄家 17 停、
比大小五种情形、洗牌后前 10 张无重复。渲染也确认过（筹码/赌注/点数、庄家与「你」的牌区、
−/+/发牌三键）。

**写它时撞到「数组形参不支持」**，反而逼出了一个更好的形状：两个数组合成一个、
**用偏移区分**（玩家 `hand(0..11)` / 庄家 `hand(12..23)`）—— 既避开了数组形参，
又不用把"算点数"这条规则写两份。

### 五、判据

| 判据 | 结果 |
|---|---|
| `scripts/vml-basic-probe`（新增 `26-exitloop` / `27-constdim`） | **23 / 23** ✓ |
| `vml-diag-probe/examples-build` | **90 / 91** ✓（失败项仍是 `forth/parserexp_demo.fs`）|
| `vml-out-probe` 跨语言输出 | **31 / 31** ✓ |

### 遗留（已写进 `FRONTEND_DEFECTS.md` 的 BASIC 一节，含最小复现与"为什么这轮不做"）

- 🟡 **数组形参不支持**：`FUNCTION f(a(12) AS INTEGER)` 里 `a(0)` 被当成**函数调用**
  去链接，报 `未定义的函数 'func_a'`。根治要**解析器 + 代码生成 + 调用约定**三处联动
  （调用点传数组基址、被调方按基址索引），是一次 ABI 改动 —— **评估后本轮不做**。
- 🟡 **裸名调用无参 FUNCTION 不触发调用**（见第三节）。
- ⚪ `DIM x AS INTEGER` 一律报「隐式声明的变量」警告（值对、纯噪音，但会淹掉真警告）。

## v0.96.330 — BASIC 前端：立**语言特性判据**（21 例）+ 修 6 条缺陷 + 移植 3 个 QBasic 经典

用户要做的是「把更多 QBasic 经典程序改成手机版」；做的过程中发现**真正挡路的是这门语言本身** ——
`gorilla.bas` 头部记了一串"必须绕开"的写法（数组不能用、SUB 里不能套括号、不能写 `AND`…），
于是先立判据、再逐条修，最后才写游戏。

### 一、★ 建 BASIC 语言特性判据（`scripts/vml-basic-probe/`，**21 例**）

这是这门语言**第一条**判据 —— 此前只有"跨语言输出"与"调用约定"两套，压的都是别的轴。

形态：每个用例一段最小复现，判据是**打印出来的值**，`cases/*.bas` 里带 `' EXPECT:` 行自动比对。
**顶层 vs SUB 内各来一遍**：历史经验是「顶层基本是好的、SUB 体才是重灾区」，只写顶层的判据会漏一半。

**顺带一个结论**：`gorilla.bas` 头部那份清单**已经全部过期** ——
数组（含动态下标 / 二维 / SUB 内）、`\` 与 `MOD`、带括号的子表达式、CONST 参与算术、`AND`/`OR`、
字符串拼接、`STR$`、SUB 的字符串形参、`SIN`/`COS`（×10000 定标）、`FOR…NEXT`、`SELECT CASE`、
`DATA`/`READ`/`RESTORE`、用户 `FUNCTION`、`ELSEIF`、`GOTO` 标签 —— **现在全对**。
那些是 v0.96.3xx 期间的实测记录，当时的缺陷后来都修掉了。

### 二、修掉 6 条缺陷（每条都定位到根因，且都进了判据）

| 缺陷 | 根因 | 判据用例 |
|---|---|---|
| **`FOR` 负步长一次都不执行** | 主程序与 SUB **两套** FOR 实现都无条件发 `JG`（`i > end` 退出），只对正步长成立 | `20-step` |
| **`CHR$` 打出的永远是同一个字符** | PRINT 路径把整个 `CHR$(n)` 当表达式求值 → `basic_chr` 返回的是**字符串指针**，紧接着 `SYSCALL #4` 把这个**指针**当**字符码**打出去 | `21-chr` |
| **`SELECT CASE` 顶层：任何分支体都不执行** | 本文件的 `Expect` 是 `new` 出来的**只断言、不消费**，而调用点写的是「跳过 CASE」⇒ `CASE` 没被吃掉，第一轮 `ParseCaseBlock` 把**测试表达式本身**当成条件 ⇒ 多出一个「条件=测试值、体为空」的块，而它**永远匹配** | `22-select` |
| **`DATA`/`READ` 读回 0** | 两处叠加：① 分派处把 `DATA` **整条丢弃**（真正正确的 `ParseDataStatement` **零调用点**、是死代码）② 链上 **4 处 `MOVE` 方向写反**（dest-first） | `23-data` `14-data` |
| **字符串临时量共用一个缓冲区** | 所有拼接结果都写库里那块唯一的 `_buf3` ⇒ `a$="AAA"+STR$(1)` 紧跟 `b$="BBB"+STR$(2)` 之后**两个变量都等于 `BBB2`** | `09-strbuf` |
| **`SELECT CASE` 在 SUB/FUNCTION 里整支漏掉** | `GenerateSubStatement` 的 if/else 链里**根本没有 `SelectCaseStatement` 这一支** —— 落下去**悄无声息、不报错不警告**，函数编译得过也能调，就是所有分支体都不执行、返回值恒 0 | `25-scinfunc` |

两条最值得记的**形态**：

- **`SELECT CASE` 那两条是一对**：顶层那条的根因是「断言函数不消费」，SUB 那条的根因是「分派漏了一支」。
  两条都不报错、都只是"少执行一段代码" —— 正是本仓最怕的那类。
- **字符串缓冲区**：修法是库侧开 16 槽缓冲池 + 前端给**每个拼接点**分配固定槽位
  （代码生成对每个 AST 节点只跑一次 ⇒ 同一处代码永远写同一槽，不同表达式点天然错开）。
  槽位数是**跨语言契约**（`basiclib.c` 的 `CONCAT_SLOTS` ↔ `GenerateStringConcat` 里同一个数），
  两边对不上也不崩：库侧把越界槽位夹回 0，最坏退化成从前的行为。

### 三、移植 3 个 QBasic 经典（都带**开机自检**，桌面就能跑）

| 游戏 | 自检 | 备注 |
|---|---|---|
| `Examples/basic/donkey.bas` 跑车躲驴（1981） | ✅ | 三个游戏里最小的，用来把"新游戏的骨架"跑通 |
| `Examples/basic/hammurabi.bas` 汉谟拉比（1968） | **10/10** | 回合制：买地/卖地/喂粮/播种分阶段，`SELECT CASE` 分派 + 字符串拼报告 |
| `Examples/basic/lander.bas` 登月（1969） | **12/12** | 连续物理：重力/推力/燃料 + 三条着陆判据（含边界） |

三个都是**照玩法规则重写的**（结构/变量名/注释/绘制都是自己的）—— 原版 GORILLAS/NIBBLES 是
`Copyright (C) Microsoft Corporation 1990`、1970s 那批出处更乱，而 `Examples/` 会随 APK 分发。

手机适配的通行做法：**原版打字输入的一律改成触摸条**（`−`/`+`/确定 或 长按点火），
**按住不放要用「按下+抬起两个事件」维护状态**（只认"按下"的话拇指按住时没有后续事件，
推进只生效一帧 —— tetris 的连发定时器踩过同一个坑）。

### 四、判据

| 判据 | 结果 | 与基线 |
|---|---|---|
| `scripts/vml-basic-probe`（新增 21 例） | **21 / 21** | 新增 |
| `vml-diag-probe/examples-build` | **89 / 90** | 同（85/86 + 新增 4 个示例；失败项仍是 `forth/parserexp_demo.fs`）|
| `vml-out-probe` 跨语言输出 | **31 / 31** | 同 |

### 遗留（都是本轮**实测出来、尚未修**的，判据已留最小复现）

- **用 CONST 当数组维度不生效**：`DIM a(N) AS INTEGER`（N 是 CONST）之后 `a(4)` 恒为 0；
  换成字面量 `DIM a(5)` 就正常。**这正是 LANDER 地形错乱的根因**（原写 `DIM terr(COLS)`，
  让循环变量一路跑到 `c=760`（= `sh`）**写穿数组**）。LANDER 已临时改字面量维度并注明
  「改一处要改两处」，根因待修。
- **无参 `FUNCTION` 返回 0**：`FUNCTION three AS INTEGER / three = 7` 之后 `PRINT three` 打 0。
  带参数的 `FUNCTION` 正常（1 参、2 参都验过）。

## v0.96.329 — 猴子扔香蕉：补上**真正的拆楼**（用户报「炸了建筑，炸完又还原了」）

### 症状与定性

用户真机试玩报「猴子炸了建筑，炸完又还原了」。**先定性**：这不是 v0.96.328 那轮
`@` 改动引起的 —— 两条实测证据：

1. `gorilla.bas` 的编译产物里**一条"短寄存器形标签"都没有**（只有 BASIC 前端生成的
   `L70320002` 这类越界形内部标签，它们本来就是标签）⇒ 新加的「标签集裁定寄存器形名字」
   那条规则对这个程序**不产生任何作用**；
2. **源码里没有持久化的破损状态** —— `groof` 只存每栋楼的**楼顶高度**，`stepFlight`
   命中时只做三件事：响一声、震一下、把状态切到 `st = 2`，**楼体数据一个字节没动**。

而爆炸是 `st = 2` 时在楼**上面**叠画两个扩散的圆（`boomT` 0→8），8 拍后 `resolveShot()`
切走；偏偏 `drawScene` **每帧**都 `ui_clear` + 把每栋楼**整栋**重画 ⇒ 圆一消失，
楼就"完好如初"。**原版 GORILLAS 之所以看着会留缺口，是因为它用背景缓冲、只重画移动物体**
（静态的楼被炸过就不再重画）—— 这份移植每帧全量重画，就把那层"假缺口"抹平了。

### 改动（`Examples/basic/gorilla.bas`）

**弹坑是一份状态，不是一层特效**：

- **存法**沿用本仓"没有可靠数组"的惯例（文件头缺陷 ③）：`ui_gget/ui_gset` 的整数网格，
  每坑 3 格（x/y/r），**环形复用**（满了盖最老的）。宁可老坑消失，也不能让"满了之后的新伤害
  不生效"（那就成了"打不动了"）。
  ⚠ 网格一共 **256** 格，而 **200..206 是俄罗斯方块的方块掩码**（`vmlui.c` 的 `MASK_AT`，
  跨语言约定）⇒ 弹坑表从 **208** 起、16 坑 ×3 到 **255 正好塞满**。越界是**静默丢弃**
  （`ui_gset` 不报错），表现是"后面的坑不生效"，很难查 —— 已在代码里写明这条算式。
- **画**：在 `drawScene` 里画完**所有**楼之后、地面之前，逐坑 `ui_circle(..., C_SKY, ...)`。
  位置很讲究：画在楼前面会被下一栋楼盖住（缺口本来就可能横跨两栋交界），画在地面之后会啃掉地面。
- **能打穿**：`stepFlight` 撞楼判定里，若落点正落在**已炸出的缺口**内，那是空的 ⇒ 继续飞。
  这就是"打几个洞之后能打进去"那件事。
  判定用**方形盒**（与猿命中盒同一个理由：SUB 里大整数乘法不可靠），且盒子取圆的
  **内接**正方形（半径 ×7/10）——取外接则香蕉能从缺口**四角**穿过去，那看起来就是穿墙；
  取内接最多是"炸点比看到的洞口低一点点"，方向是对的。
- **换局清空**：`newCity()` 里 `clearHoles()`。城市都重排了，旧坑的位置毫无意义
  （不清的话上一局的洞会以天空色的圆出现在新楼上，像贴了几块补丁）。
- 打**猿**不留坑、打在**地平线**上不留坑（那儿本来就没有楼）。

### 判据

`simCheck()`（文件自带的**固定城市 + 脚本弹道**无头自检）里新增 ⑥⑦⑧ 三段，
桌面 `vmlcli` 就能跑（这一版也修掉了自检里两处**判据本身**的毛病）：

| 判据 | 实测 |
|---|---|
| ④ 打**猿** → `hitBld` | **0** ✓（不该留坑） |
| ⑥ 打**楼** → `hitBld` / `nHole` | **1 / 1** ✓ |
| ⑥ 坑 x/y/r == ⑤ 的落点 | `153 361 22` **逐字相同** ✓ |
| ⑦ 同角度**再打一发** → 新落点 y | **383 > 361** ✓（从缺口穿过去了，又炸深一层），`nHole → 2` ✓ |
| ⑧ `clearHoles()` 之后 `nHole` | **0** ✓ |

⚠ 写这段判据时**自己踩了两个坑**，都写进注释了：① `hitBld` 每发都在 `stepFlight` 开头清零，
攒到最后一起打印拿到的是**最后一发**的值，必须**当场打印**；② `ui_gget(ghole)` 读的是
**第一个**坑，前面 ①② 也各留了一个坑 ⇒ 判 ⑤ 之前必须先 `clearHoles()`。

**绘制**这一环用 `vmlcli --frames` 抓真帧验过（不要"看代码觉得对"就算了）：只打一枪、不结束，
逐帧取像素做差分 —— 「首帧是楼体、后来变成天空色」的像素共 1645 个，**最大连通块 1378 像素**
（成团，不是散点），末帧可以直接看到**右数第二栋楼的顶部被炸掉一个圆缺口**，且打完几百帧后仍在 ✓。

## v0.96.328 — 寄存器形名字的归属改由**标签集**裁定（`f1`/`d1`/`l1` 可以当变量名了）+ 三方补齐 `@` + 全库重生成

### 一、★ 修掉「变量名 / 函数名撞寄存器形」的最后一族

v0.96.327 修好了**函数名**（`call f1` 不再变 `call R1`），但**全局变量**还坏着 ——
用户报「变量名用 Rn/Ln/Fn/Dn 要能正常使用」，实测复现如下（判据是输出值）：

| 判据（`Examples/c/regname.c` 全覆盖） | 修前 | 修后 |
|---|---|---|
| 函数名 `r1/f1/d1/l1` | `10` ✓ | `10` |
| **全局标量** `r1/f1/d1/l1` | **`IndexOutOfRangeException`** | `10` |
| 全局数组 `f1/l2` | `6` ✓ | `6` |
| **大写** `R1/F1/D1/L1` | `0887766`（`R1` 读成 0） | `99887766` |
| 越界形 `r99/f20/d9/l9` 当变量 | —— | `1357` |

**根因是实测出来的，不是推的**：C 前端为全局标量产出 `[f1]`（**标签名**）→ 序列化器
按**形状**判成寄存器引用、写成 `[@f1]` → 汇编器读回成寄存器 F1；`d1` 变 D1、`l1` 变 L1
（= 运行时编号 17 / 25）⇒ `registers[17]` 直接越界。
生成的程序里那三行就是证据：`move @R0 @1` / `@17` / `@25`（`L1→25 = n+24` 正是 D/L 的编号）。

**新规则（一条，取代"按形状猜"）**：

> 一个**不带 `@`** 的寄存器形 token（`f1` / `d2` / `R1` / `r100`）——
> **本文件定义了同名标签 ⇒ 它是标签**；否则 ⇒ 寄存器（越界则报错）。

- **为什么必须推迟裁定**：标签可以定义在使用点**之后**（前向引用是常态），解析到那一行时
  标签集还不全 ⇒ 只登记形状，整份文件解析完再定（`VMLAssembler.ResolveRegisterShapedNames`）。
- **与两条老规则各归其位**：(a) 标签位置（CALL/JMP/Jcc）**只能是标签**；(b) 带 `@` 的
  **只能是寄存器**；这条只管**两者都没说**的。
- **`@` 永远优先**：`[@R0]` 就是寄存器，与有没有同名标签无关。
- **代价（有意接受）**：真定义了名叫 `R0` 的标签时，`[R0]` 会变成标签引用 —— 要寄存器间接
  写 `[@R0]`。而**编译器产出的寄存器引用一律带 `@`**，所以这条代价只落在手写汇编上；
  而 `f1`/`d1`/`l1` 当变量名远比 `R0` 常见，两害相权取此。

### 二、运行时也认 `@`，并且按**同一条规则**裁定

`VMLRuntime.Memory.cs` 是运行时唯一按文本读寄存器的地方，而它有两处缺口：

1. **只认裸名**：`ParseMemoryString` 只做 `Trim('[',']')`，串里带 `@`（`@R12-8`、AT&T 的
   `8(@R12)`、`[@R14-0]` 里层）就整串落空、**静默降级成标签** ⇒ 改为统一走
   `RegisterSyntax.StripMarker`（与汇编器共用同一判据）。
2. **同一处形状歧义**：`[R1]` 走 `StartsWith("R")` → 寄存器间接 ⇒ 读到的是**寄存器**的内容
   （实测打印 `0` 而不是 `99`）。而 `F1`/`D1`/`L1` 不以 `R` 打头、反而落进标签分支 ——
   **同一份源码里一个对一个错**，正是"按形状判归属"这类缺陷的指纹。
   ⇒ 把 `_program.Labels` 传进去，按**与汇编器同一条规则**裁定（`IsLabel`），
   AT&T / `R1+4` / `R1-4` 三条分支一并覆盖。
   （`PreDecodeMemoryOperands` 跑在 `labelAddresses` 填充**之前**，所以用 `_program.Labels`。）

### 三、C 编译器补齐 `@`

`GenerateAsmStatement` 的 `${var}` 展开是 C 前端**唯一自己拼寄存器文本**的地方，而且它
**绕过序列化器**（直接进 `asm "..."` 文本，见 `AddInstruction(OpCode.ASM, Imm(code))`）
⇒ (c) 规则管不到它。改成 `@R{n}`（`searchStart` 相应 +3）。

⚠ **动手前先查过**：633 处 `asm()` 里 78 处含 `${}`，其中 **16 处是「`${}` 与裸寄存器同行」**
（`asm("MOVEF F0, [${p}]")`）—— 在**旧**规则下把 `${}` 写成 `@R0`，规则 (b) 会把这个 `F0`
判成标签、直接编坏。而按新规则（一），`F0` 无同名标签 ⇒ 仍是寄存器 ✓。**顺序不能反**。

### 四、GenLib 生成器改 `@` + 全量重生成

`tools/GenLib/Program.cs` 里用**字面量**拼的寄存器共 13 处（`push R15` / `move R12 R13` /
`PUSH R{regIdx}` / `sub R13 #4` / `move @R13 @R12` …），都不走序列化器 ⇒ 全改 `@` 形态；
三份生成绑定（Python `asm("R0")` / Rust `asm!("MOVE {0}, R0")`）一并改。

然后按 `FORK.md` 的流程：`touch Lib/shared/src/*.c` → `GenLib -b`（105 编译）→ `GenLib -A`
⇒ **`Lib/` 下 1587 个文件重新生成**，转发包装现在长这样：

```
LABEL c_newline
    push @R15
    push @R12
    move @R12 @R13
    CALL newline
    move @R13 @R12
```

### 五、汇编 / 链接期的异常必须接住（两处，同一件事）

新增的「寄存器名越界」是**用户可见的诊断**，不接住就变成手机上「点了没反应」：

- `MauiVml.BuildProgram`：`AssembleWithIncludes` 此前**没有任何 try/catch**（474 行那个只包
  前端编译）⇒ 补上，走与前端编译失败同一条 `Fail` 出口；
- `scripts/vmlcli`：链接包了 try、汇编没包 ⇒ 合成一处（**它自己的注释里就记着这个缺口**：
  "`MauiVml` …那两步也没包 try，一并记着"）。

### 六、判据

| 判据 | 结果 | 与基线 |
|---|---|---|
| `Examples/c/regname.c`（新增，12 项寄存器形命名） | 合计 `831` ✅ | 新增 |
| `vml-out-probe` 跨语言输出 | **31 / 31** | 同 |
| `vml-abi-probe` 跨语言 ABI | 27 / 29 | 同（红的是 `drift.fth` / `drift.ld`，README 里列为已知） |
| `vml-diag-probe/examples-build` | **85 / 86** | 同（红的仍是 `forth/parserexp_demo.fs`） |
| 桌面自测 | 6231 / 6232 | 同（那条失败在 `VmlDiagnostics`，本次**未碰**该文件） |
| 越界反面判据 | `R99` 无同名标签 ⇒ 报「寄存器名越界：R99（地址 4）…」 | 新增 |

### 遗留（有意不做）

- **633 处手写 `asm()` 仍是旧方言** —— 读兼容、照旧工作（与 v0.96.327 同一条）。
- **`Lib/` 下仍有旧方言的 `.vml`**，两类，都不影响运行（裸名读兼容）：
  ① 47 个**手写**模块（各路 `ustring.vml` / `wstring.vml` 等，GenLib 不产出、改不动）；
  ② 88 个 **GenLib 陈旧产物**（`Lib/<lang>/printx.vml` 等 22×4 —— 当前生成器里已经没有
     `printx` 这个模块了，所以 `-A` 不会重写它们）。**要清得先确认没人链接它们。**

## v0.96.327 — VML 汇编语言变更：寄存器一律 `@` 开头 + 全仓文档矛盾普查

### 一、★ 汇编语言变更：寄存器一律带 `@` 标记

**一句话**：`move @R0, @R1`、`push @R12`、内存操作数里也一样（`[@R12-8]`）。
**裸名照旧能读**（老 `.vml`、手写汇编、`Lib/` 里 633 处 `asm()` **一个字节都不用改**），
但**编译器生成的一律带 `@`**。

**起因是一个静默缺陷**：汇编器判寄存器 = 「前缀 ∈ {R,F,D,L} + 后面 `int.TryParse` 能过」，
**大小写不敏感、且 `R` 连上界都不查**。于是用户起的短名字撞进寄存器：

| 名字 | 被读成 | 修前实测 |
|---|---|---|
| `f1`/`f2` | `F1`/`F2` → R1/R2 | `print_int(f1()); print_int(f2())` 打出 **`77`**（应为 `7 11`） |
| `r100` | `R100` | `IndexOutOfRangeException` |
| `l1` | `L1` → **R25** | `IndexOutOfRangeException` |
| `d1` | `D1` → **R17** | `IndexOutOfRangeException` |

**22 门语言全中**（前端产的符号名都是裸名），只是 `Examples/` 里一直没人这么命名才没暴露。
修后 `711` ✓。四 bank 语料 `r1`/`l1`/`d1`/`f1` 由「崩」变 **`1234`** ✓。

**为什么不是在源头给符号改名**（一度考虑过 `f1 → var_f1`）：
① 那要穿透 **22 个前端**的每一处符号出口，漏一类就换个形状继续坏（本仓头号坑）；
② **上下文消歧根本不成立** —— `call f1` 能靠"这个位置要的是标签"判，但标签**被当值用**时
（`move R0, f1` 取地址）与寄存器 `F1` **真的分不开**。

**为什么 `@` 方案代价小得不成比例**：**寄存器名只活在 `.vml` 的文本格式里** ——
内存里是 `Operand(REGISTER, n)` 与 `MEMORY("R12-8")`。
⇒ 只改汇编器的**解析**与**序列化**两处，**22 个前端与运行时一个字节都没改**（实测确认，见下）。

**三条规则，缺一不可**：
| | 规则 | 覆盖 |
|---|---|---|
| (a) | **标签位置**（`CALL`/`JMP`/各类条件跳转/`CATCH`/`LABEL`）的操作数**必是标签** | `call f1` |
| (b) | 一条指令里出现**任一** `@` 标记的寄存器 ⇒ 其余"字母+纯数字"的裸 token 当标签 | `move @R0, f1` |
| (c) | **序列化时给所有寄存器写 `@`**（含内存操作数串里的 `R12-8` → `@R12-8`） | 让 (b) 永远生效 |

⚠ **(c) 单独不够**：流水线**会重新读文本**（前端 → `.vml` → 汇编器再解析 → 链接器），
生成出来的 `move @R0, f1` 读回去时 `f1` **照样会被当成寄存器**。(b)(c) 是一对。

⚠ **一条指令里不要混用两种方言**：(b) 说"有 `@` ⇒ 其余**裸** token 是标签"，
所以 `move [@R1+8] R0` 里的 `R0` 会被当成**标签**。要么整条都标，要么整条都不标。

⚠ **`@` 的旧含义变了**：`@R0` 以前是「以 R0 为地址的**间接寻址**」，现在是**寄存器本身**。
间接寻址写 **`[@R0]`**（`[R0]` 是旧的裸名写法、仍可读）。库里唯一一处旧用法
（`builtins.c` 的 `peek`/`poke`/`peekb`/`pokeb`）已迁移。

**判据全绿**：用户复现 `77 → 711`；`out-probe` **31/31**、`abi-probe` **7/7**、
`examples-build` **85/86**、桌面自测 **6232/6233**、`vmlcli-verify` 全过、
`check-vml-patches` **✓**（`Lib/` 与重生成逐字节相同）。**真回归 0 条。**

**"零前端改动、零运行时改动"是实测结论**：中途一度加过运行时的 `@` 剥除，按边界**把它撤回**
之后 `[@R1+8]` 形态**仍然全绿** —— 剥除发生在汇编器的括号分支。
幂等性也验了：`Assemble(ToString()).ToString()` 与上一趟**逐字相同**；
7479 处 `[@R12+12]` 喂回去，内存里是 **195 种裸名、带 `@` 的 0 种**。

**反证两个方向都做了**：只留 (a) ⇒ `call f1` 修好、但 `MOVE @R0 f1` 取地址仍坏（(b)/(c) 承重）；
关掉 (a) ⇒ 用户复现回到 `77`（(a) 承重）。

### 二、全仓文档矛盾普查：21 条承重事实，**16 条对不上**

**方法**：不是"通读找矛盾"（查不全），而是先列**承重事实**（多份文档重复出现、改一处就得改多处），
再逐条 grep 横向比对；**"哪个对"一律以代码为准**，不让文档之间投票。
**历史记录（CHANGELOG 旧条目、带版本号的里程碑行）不改写** —— 那是当时的正确记录。

| 事实 | 文档各说各的 | 实际 |
|---|---|---|
| 工具数 | 46 / 44 / 39 / 47（**四种**） | **49** |
| 自测规模 | 4106 / 4597 / 4620 / 5083 / 5122 / 5841 | **6232** |
| BashGuard | 「70+ 禁止 + 47 安全」 | **87 / 70** |
| 经济模式 | 「三态」 | **四态**（漏了 `extreme`） |
| 配置项数 | 67 | **110** |
| 目录名 | `.corecoder/` | `.waycoder/` |
| 槽位切换 | 「运行时禁止」vs「运行中也能切」 | **能切** |
| 权限确认 | 「三行黄底」 | 那是**死代码**，现役是输入框下方的选择栏 |
| 边界轴 | 「与确认轴纠缠，待解耦」 | **已解耦** |
| 模型回退链 | 「失败自动尝试备选」 | **默认关**，链由 `/connect chain` 配 |

工具名还有硬错：README 写 `multi_edit`/`export`/`ask_user`，实际是
`multiedit`/`export_chat`/`ask_user_question`。`--GUI` 应为 `--gui`（大小写敏感，写错的会报"未知选项"退出 1）。
（**本来就是对上的**：syscall 号段表、22 门语言清单、调用约定 —— 这三条写得最好。）

### 三、普查顺带查出的**代码**问题（未改代码，仅记录）

1. ★ **`sqlite` 工具根本没有只读守卫** —— README 一直写「只读 SQL 查询（手搓 SQL 引擎）」，
   **两头都不对**：实际是起 `sqlite3` 子进程、**没有任何只读限制（可跑 DELETE/UPDATE）**；
   而那个"手搓引擎"（`Sql/SqlEngine.cs`）**全仓零调用点**，是死代码。
2. `--economy` 的 `--help` 文本漏了 `extreme`（用户敲 `--help` 直接看得到的错）。
3. `UI/Shared/ToolDisplay.cs` 有个**死键** `["multi_edit"]`（不存在这个工具名）⇒ `multiedit` 拿不到缩写名。
4. `Config/Global.cs` 注释仍写「先试 `.waycoder/`，回退 `.corecoder/`」，而回退早已不存在。
5. `Program.cs` / `ModeArgs.cs` 的 XML 注释与实现**相反**（`--permission-mode` 的两个分支都只设确认轴）。

### 四、纠正本轮的三个认知（查出来的，不是猜的）

1. **`F20`/`D9` 在顶层操作数位置并不是寄存器**（`ParseOperand` 有范围检查）——
   但 **`IsRegisterName`（只用于 `[...]` 里）没有范围检查**。
   ⇒ **"什么算寄存器"这条规则在汇编器里有两份、口径不同** —— 同类地雷，本次未动，已记录。
2. **手写 `asm()` 是 633 处**（不是 543）：`vmlsys.c` 118、`vmlui.c` 90、`crt.c` 86、`graphics.c` 73…
3. **`l1`/`d1` 崩的真因是运行时的寄存器堆只有 16 个**（`registers = new int[16]`），
   而 D/L bank 映射到 16–31 —— **裸名 `L3` 今天同样崩**，与本次改动无关；本次只保证它们**不再被误读**。

### 遗留（有意不做）

- **633 处手写 `asm()` 仍是旧方言** —— 读兼容、照旧工作。迁移属**另一件事**（高风险机械编辑），
  单独一刀。理由：与语法变更捆在一起，出了回归就分不清是谁的。
- **`Lib/<lang>/*.vml` 的转发包装**（GenLib 手写文本、不走序列化器）仍是裸名 ✓。
- **括号里的"寄存器形标签"**（`[f1]`）仍按老规则读成寄存器 —— 规则 (b) 管的是裸 token；
  全量扫描 **0 处**命中，属理论角落。
- `VmlProgram.ParseOperand`（另一套简易解析器）不认识 `@` —— 但它的唯一调用者
  `IncrementalCache` **零调用点**，是死代码；谁复活它必须先补 (a)/(b) 与 `@`。

## v0.96.326 — 猴子在手机上跑通 + BASIC 前端 17 条缺陷 + 桌面端补齐 syscall + 通用宿主调用口

本版量很大，四批工作交织（互相碰过同一批文件），逐条记清归属。

### 一、`gorilla.bas` 在模拟器上跑通，并打完一整局（玩家二 3:0）

**画面是逐点比色验的，不是截图看的**（`scripts/maui-vml-verify/gorilla_pixels.py`，22/22）：

| 图元 | 期望 | 实得 |
|---|---|---|
| 角度条绿 `#4ADE80` | 半条 | 填充 **0.496**（瞄准 45/90） |
| 力度条橙 `#FFB020` | 七成 | 填充 **0.699**（力度 70/100） |
| 发射键红 `#D8443C` | 两端各留 14 单位 | 左右留白**都精确等于 14**（源码常量就是 14） |

触摸：拖角度条 → **0.741**（目标 0.75）、拖力度条 → **0.687**（目标 0.70）、互不误伤；
按发射 → 香蕉 → 落点爆炸 → **回合换边**；打中 → **比分圆点由 off 变蓝**。

★ **`gorilla.bas` 一个字都没改。** 上设备后暴露的三个问题**全在宿主与工具链里** ——
游戏源码画出的东西与它自己的常量逐项吻合。那段"桌面自检"不是白写的。

### 二、★ 一个影响**所有** VML 程序的宿主缺陷：安卓上手指拖动根本不发事件

`DrawWindowPage` 用 `PointerGestureRecognizer.PointerMoved` 收拖动 ——
而它在安卓上是 **hover 语义，手指拖动时一次都不发**。
实测形状："3 秒长 swipe，每 250ms 采一次，绿色填充**一个像素都没动**；
**同位置改成单击立刻生效**" ⇒ 按下好的、抬起好的、**只有移动丢了**。
改用 `GraphicsView` 的 `StartInteraction`/`DragInteraction`/`EndInteraction`；
改完同一条 swipe：`0.086 → 0.149 → 0.542 → 0.886`，跟着手指走。
**任何需要拖动的 VML 程序在此之前于手机上都拖不动** —— 不只是猴子的事。

配套修 `driver.py` 一处**破坏性误判**：程序一开窗/弹框，`uiautomator` 只 dump 焦点窗口，
输入框整个不在树里、`entry_text()` 返回 `None`，而判据写的是 `== ""`
⇒ **一个跑得好好的程序被记成"命令没送进去"、然后被强行冷启动重来**。

### 三、BASIC 前端：12 条报告缺陷 + 5 条顺带挖出的，共 17 条

**根因不在"两套语句生成器"，而在更窄的一处**：`GenerateSubExpression` 的 `BinaryExpression`
分支是 `GenerateExpression` 那条的**残缺副本** —— 缺 `\`/`MOD`/`^`（**且没有 default，
静默什么都不发**），操作数寄存器硬编码 R1/R2 且不保护左值（`MOVE reg,R1` 在 `reg==2` 时
变成 `OP R2,R2`，自己跟自己算）。**12 条里 6 条出自这一处**，修完一起消失。

修前 → 修后：`SQR(16)` 0→**4**；`SIN(30)` 0→**5000**；SUB 体 `100\2` 0→**50**；
SUB 体 `1+(2*3)` 6→**7**；SUB 体 CONST 比较恒真→**假**；`DIM a(10)`/`a(3)=42` 链接失败→**通过**；
字符串拼接空→**xy**；`DIM x AS STRING` 1024→**hello**；SUB 字符串形参垃圾→**hello**。

**顺带挖出 5 条不在清单里的**，两条很重：
- **`NEXT i` 的循环变量没被吃掉** ⇒ 紧跟的 `i` 被当独立语句，链接器把它接到同名全局标签，
  `NEXT i` 变成**递归调用**：`FOR i=0 TO 3` 打出 `1 2 3 0 1 2 3 …` 直到内存不足。
  **`scripts/basic-tests/` 的 t2/t5/t9/t10/t11 五条全栽在这一个根因上**（t1–t11 现在 11/11）。
- **C 前端空语句 NRE**（`for(…);` 编成 `ExpressionStatement(null)`，读 `node.Line` 就炸）
  ⇒ **`basiclib.vml` 根本重生成不出来**（`uchar/ustring/wchar/wstring` 五个同因）
  —— 库源码与签入产物早就对不上，只是没人看得见。

`_sin_lookup` 的每段基点抄成了**该段上界**的值、且 `basic_sin` 把角度当弧度（全族统一改成角度制）。

### 四、C++ 前端：`long*` / `short*` 参数解析不了

`CppCompiler/Parser.Declarations.cs` 的 `ParseType()` 里 `long`/`short` 两支**提前 `return`**，
函数末尾吃 `*` 的循环永远走不到 ⇒ `long* v` 解析成 `"long"`、`*` 留给调用方撞上 `Expect(IDENTIFIER)`。
**只有 `long`/`short` 中招**（`int*`/`float*`/`double*` 都落到末行、本来就是对的）——
所以它看起来像"随机某个类型不行"。**C 前端本来就是好的**（各种位置试过，零失败）。

⚠ 刻意**没有**改成 fall-through：那会让 `long int` 变成类型串 `"long int"`，
而 C++ 后端判位宽是 `Contains("int")`，4 字节会**悄悄变 8 字节**。

### 五、通用宿主调用口 `callwith*`（号 577–580）

`callwithint8` / `callwithfloat8` / `callwithlong4` / `callwithdouble4`：按**数字 id** 调宿主函数，
入参走寄存器、返回值覆盖第 0 个寄存器，**省掉 JSON 编解码**（与 CALLJSON #573 并列的"带类型快通道"）。

★ **动手前先查清了寄存器组，这个要求值回票价**：

| 组 | 存储 | 名字 |
|---|---|---|
| 整数 | `int[32]` | R0–R31 |
| 浮点 | `float[16]` | F0–F15（**与 R0–R15 同号**，值在另一个数组） |
| 双精度 | `double[8]` | D0–D7 |
| 长整数 | `long[8]` | L0–L7 |

⇒ **4 个 long 走 L0–L3、4 个 double 走 D0–D3**。我原以为的"R0–R7 每两个槽拼一个"是**错的**，
而且错得隐蔽：`ISystemCallHandler.HandleSyscall` 只给宿主 `int[] registers`，
而 `SetLongValue`/`SetDoubleValue` **只把低 32 位镜像进那个数组** —— 照那个数组配对，每个 64 位值只能拿到半截。

错误码全不崩、各带一条可读日志（未注册 `-6` / 类型不符 `-2` / 负数号 `-2` / 实现体抛异常 `-9`）。

### 六、桌面端补齐 syscall：**不靠窗口也能在桌面上跑游戏**

新增共享宿主层 `WayCoder/UI/Shared/VmlHostRuntime.cs`：`IVmlHost`（20 个成员）只装**两个平台
真的不一样**的事，`VmlHostRuntime`（1016 行）装**全部逻辑**（整张 `switch` 表、坐标/字号钳位、
消息队列语义、定时器暂停、刷子状态机、`Str/StrBlock`、CALLJSON 两段式退让、`#57` 截获）。
手机 `VmlUiCalls.cs` **1042 → 434 行**（只剩薄壳），桌面新增 `CliVmlHost.cs`。

**关键决定：桌面没有窗口，画面照样建 `VmlScene` → 渲 PNG 落盘**，
出图走仓库**现成的** `DrawRunner.Parse` + `ToPng`（与手机端 `VmlTool.TryExportFrame` 同一条）
—— **只有这样，桌面上渲出来的像素与手机上才是同一个引擎的产物**，逐像素判据才可能在桌面上做。
输入靠 `--input` 脚本按墙钟投消息（走与手机 UI 线程**同一个** `VmlHostRuntime.Post`）。

成果：`draw_colors.c` 从"跑满 20s 超时被杀、什么都不出"变成 **3337ms 跑完并出图**（15 格全部
`#3C6EB4`、渐变左 `#EF000F` → 右 `#0D00F1`）；`tetris.c` 12 帧、分数 98；`gomoku.c` 两次点击
**黑子真的落在点的位置上、AI 回了两个白子**。新增可复跑套件 `scripts/vmlcli-verify/`（20 项）。

**这一刀抓出 3 个真缺陷，第一个手机上也跑同一份代码**：
1. **`VmlMessageQueue` 信号量的账不对**（`VmlUiProtocol.cs`，**共享、手机同一份**）：
   `Post` 只在计数为 0 时 `Release()`（把信号量当唤醒开关），而 `TryRead` **不消费许可**
   ⇒ 队列取空后计数还留着 ⇒ 下一次 `Read` **空唤醒、当场返回 null**。
   症状：连读两条消息再装 60ms 定时器，定时器明明投了消息，`ui_wait(msg, 2000)` 立刻返回 0。
2. **宿主诊断日志被当成"程序输出"并进 stdout** ⇒ 污染 `stdout = 程序输出` 这条契约
   （`vml-out-probe` 那套逐字节判据靠它）。
3. **`--timeout` 管不住无限等待**：VM 的超时在**指令循环**里查，而 `ui_wait(msg,0)` 让 VM 线程
   阻塞在宿主 syscall 里 —— 实测 `--timeout 30` 跑了 5 分钟还在。

### 七、判据

| 套件 | 结果 |
|---|---|
| 桌面自测 | **6232 / 6233**（唯一失败 `VmlDiagnostics` 那条**预先存在**，在未触碰的文件里） |
| `vml-out-probe` | **31 / 31** |
| `vml-abi-probe` | **7 / 7** |
| `vml-diag-probe/examples-build` | **85 / 86**（唯一失败 `forth/parserexp_demo.fs`，**预先存在**） |
| `vmlcli-verify`（新增） | 全过 |
| 22 语言 sysinfo（模拟器 v0.96.325 实测） | 21 门 JSON **逐字节相同** + `ladder` 豁免 |

### 遗留（已诊断未修）

1. **`CCompiler.FindUsedFunctionsInExpression` 不认 `CastExpr`** ⇒ `(int)g(a)` 这类调用对可达性
   分析不可见、函数被当"没人用"**静默不生成**、链接期才报未定义。
   最小复现：`int g(long* v){return v[0];} int main(){long a[2]; a[0]=1; return (int)g(a);}`
2. **`forth/parserexp_demo.fs` 的真源**：`Lib/shared/src/ctype.c` **第一行 `#param lib("parserexpf")`**
   （ctype 与 parserexp 毫无关系，显然是误抄）让 `ctype.vml` 带上 `.linked "parserexpf.vml"`。
   但删掉它也不能让例子过（那需要往 GenLib 生成的自动链接清单里加 `parserexp.vml`，影响 22 门）。
3. **汇编器寄存器名冲突**：`f1`/`d2`/`l3` 这类小写函数名被当成寄存器，`call f2` 链接期变 `call R2`。
   实测 `print_int(f1());print_int(f2())` 打 `77`（应 `7 11`）。影响全部 22 门语言。
4. **`asm("SYSCALL #N")` 的返回值写进硬编码 `R12-4`**（局部量在 `R12-8`）⇒
   `Lib/shared/src/{file,network}.c` 里所有 `int r; asm("SYSCALL #N"); return r;`
   返回未初始化栈内容（`fopen`/`fwrite`/`net_send`/`get_tick` 全返回 0，**副作用是真的**）。
   `vmlsys.c` 早绕开了（写**不带 `#`** 的 `SYSCALL`）。
5. **手机端共享宿主层改动只到编译通过**，未上设备验证。

### 补记（同日收口）：判据升级 + 手机端未验 + 猴子移植的路线判定

**① 22 语言 sysinfo 的判据从「有 `"ok":true`」升级成「逐字节相同」**，重跑结果：
**21 门全部逐字节相同（差异 0）**。`ladder` 不是缺陷 —— 它是 PLC 梯形图前端，
**没有字符串实参的函数调用**，那份 `sysinfo.ld` 本身只证明模块还能链接（文件里就写着）。

**② 手机端仍未验证，而且有一个必须先说清的事实**：`make-vml-lib.sh` 这次重打包时明确警告
「签入的 `vml_lib.zip` 与仓库 `Lib/` **原本不一致**」—— 也就是说**这段时间手机上跑的是旧标准库**
（桌面看不出来，因为桌面不读这个包）。新包已随本版重打并提交，但 **APK 还没重打、没装**
⇒ 上述所有修复（含 `printf %f`）**目前都还在仓库里，没上手机**。

**③ 猴子（GORILLA.BAS）移植的路线判定**：官方原件已取得并**按 MD5 验真**
（29,434 字节 / 1135 行，`3651562e0a058e661e38a1e9e82afadb`，
版权头 `Copyright (C) Microsoft Corporation 1990`）。
两条结论已写进 `CLAUDE.md`：
- **QBasic 原生图形语句在手机上跑不了** —— 那套 `SCREEN/LINE/CIRCLE/PAINT` 的代码生成是往
  **固定 DOS 内存地址**写的 DOS 帧缓冲模型，不是手机那扇窗口；手机上画图只能走 **`ui_*` 共享库**。
- **原件不能随包发**（`Examples/` 会被打进 `vml_lib.zip` 随 APK 分发）。
  用户选定：**只参考玩法、代码自己写**。

**④ 同一家族里仍未修的**（别让 21/21 读起来像"全都好了"）：
`Examples/objc/parserexpf_demo.m` 打 `Float: 0.0`（ObjC「函数返回 double」整条链断的）、
`Examples/forth/parserexp_demo.fs` 编译失败（**预先存在**）、
`drift.fth` = 8（应 126，Forth R15 蹦床语义）、`vsscanf` 无浮点转换。

## v0.96.325 — 仪表在空转：macOS 上每次调用白等满超时（套件 20 分钟 → 1 分钟）+ 三门语言的输出缺陷

### 一、`run_with_timeout` 在 macOS 上让**每一次调用都等满超时**（最值钱的一条）

判据套件（`vml-out-probe` / `vml-abi-probe` / `vml-diag-probe`，共 111 项）**跑了二十几分钟**，
而本仓记录的"健康时长"是 **3.5 分钟**。查下来不是编译器变慢，是**仪表在空转**：

POSIX 分支里那个看门狗 `( sleep N; kill -9 $pid ) &` **继承了调用方的 stdout 管道**。
`kill "$watcher"` 杀的是那个**子 shell**，而 `sleep` 是它的**孩子**（杀父不杀子）⇒
那个 `sleep` 一直活到点，管道写端一直开着 ⇒ 调用点那句
`out="$( … run_with_timeout … | tr -d '\0' )"` 里的 `tr` **要等所有写端关闭才见 EOF**
⇒ 每次调用实打实地等满超时。

**实测**：`run_with_timeout 20 echo hi` 耗时 **20.013s**；给看门狗的 stdio 接上 `/dev/null` 之后 **0.004s**。

**为什么长期没被发现**：Linux 上有 GNU `timeout`（`TIMEOUT_BIN=timeout`），根本走不到这条
POSIX 分支；**只有 macOS 会中** —— 而 macOS 正是本仓的开发机。症状是"整套例程变慢"，
看起来像编译器的问题，实际每次调用都在**空转**。

修完之后：`vml-out-probe` **31/31、耗时 59s**（预算 600s）；`vml-abi-probe` 7/7、**9s**。

### 二、BASIC：`NATIVE` 声明**把后面的整个程序吞掉**，症状是「零输出、零报错」

`NATIVE FUNCTION f() AS INTEGER` 这种**外部符号声明**本来没有"体"，但解析器照旧进体循环、
一路吃到 `END FUNCTION`。不写 `END` 时它就吃到 **EOF** ⇒ **该声明后面整个程序都被当成了它的体**；
而 NATIVE 的体不生成代码（`CodeGenerator.Sub.cs` 开头就 `return`）⇒ 主程序一条语句都不剩
⇒ 编出一份「完全合法、什么也不做」的程序：**退出码 0、屏幕上没有一个字**。
实测 `Examples/basic/sysinfo.bas`：`statements=1 FunctionDeclaration=1`。

判据改成「是不是 NATIVE」而不是「有没有 END」：NATIVE 就是外部符号声明，写不写 `END` 都不该吃后面的语句；
紧跟的 `END SUB/FUNCTION` 仍旧吃掉（`whack.bas`/`tetris.bas` 等既有例子的空体写法向后兼容）。
新增探针 `scripts/vml-out-probe/langs/nat.bas` 把**两种写法**都钉住（少测一条，另一条坏了看不出来）。

### 三、`printf("%f")` 打出 `0.0`：`%f` 与 double 的槽位规则不一致

`Examples/objc/parserexpf_demo.m` 打出 `Float: 0.0`。根因是**同一根线上两套规则**：
前端按 C 把 `1.5` 当 **double（2 槽）**，而 `printf` 库按自己那套「`%f` = float32（1 槽）」去读
⇒ 只取到低 32 位（`1.5` 的低字恰好是 `0x00000000`）⇒ `0.0`。
而 `printf("%f", floatVar)` 反而对（1 槽，位模式正好落在低字）—— **对错取决于实参的静态类型**。

按 **C 语义**对齐：`%f/%e/%E/%g/%G` 一律读**一个 double（2 槽）**（printf 里 `%f` 与 `%lf` 同义）；
并在**调用点**补上 C 的**可变参数默认提升**（`float → double`，C 前后端各一处），
所以 `printf("%f", 1.23)` 与 `printf("%f", floatVar)` 现在都对。

⚠ 三条踩出来的：
- `readDouble` 里 `((unsigned long long)hi << 32) | lo` **不工作** —— 实测两半都成了 hi
  （`0x3FF800003FF80000`）。`Lib/` 里 64 位函数一律「lo/hi 两个 int 分开传」正是绕它。
  改成**让两半在内存里自然相邻**再按 double 读（零 64 位算术），`pair={0,0x3FF80000}` ⇒ `d*1000==1500` ✓。
- **scanf 不能照搬**：它收的是**指针**，C 规定 `%f` 要 `float*`、`%lf` 要 `double*`（与 printf 相反）。
- **ObjC 前端另有独立的缺陷**（本轮只诊断未修）：连 `double d = parserexpf(...)` 都拿到 `16777216`，
  即「函数返回 double」这条链本身断的，不是压参的问题。已在下方"遗留"记录。

### 四、Forth：`S"` 没跳分隔空格 ⇒ 字符串永远多一个前导空格

`S" sysinfo"` 编成 `" sysinfo"`，宿主按名字查表**永远命不中**，报出来的却是
「未实现该函数：」（名字后面带一个看不见的前导空格）。
`."` 在 v0.96.208 那轮已经补上了这一句，**`S"` 漏了** —— 本仓反复出现的
「同一规则两处实现、只修了其中一处」，这是又一例。

例子那侧同时改了栈序：`S" ..."` 压的是 **(地址, 长度) 两个单元**，而 `ui_call_json_s(char*,char*)`
只要一个指针 ⇒ 长度必须 `DROP`；且**最后压的 = C 的第 1 个实参** ⇒ args 先压、fn 后压。

### 五、**撤回**：Forth 调用点的 R15 蹦床改动

诊断阶段发现 `CodeGenerator.Words.cs` 的蹦床 `POP R1 / PUSH R15 / PUSH R1` 把保存槽
**插进了实参区** ⇒ 被调方 `[R12+16]` 读到 R15、第 2 个实参起整体错位
（`drift.fth` 期望 `DRIFT=126`、实测 `8`）。按「只有本地词才需要保存 R15」改掉之后 ——
**`drift.fth` 挂死了**（22 秒超时），而**强制保留保存槽则恢复正常（`DRIFT=8`）**。

**所以那个改动不安全，已整条撤回**（`git checkout` 两个文件）。上一条的 `S"` 词法修复
与例子栈序修复**单独就足以让 forth sysinfo 输出正确**（与 C 版逐字一致），所以不欠这笔。
`drift.fth` 的 `DRIFT=8` 是**已知、已文档化的缺陷**（那条探针存在的意义就是量它），
正解要先把 R15 在词体与库调用之间的保存语义弄清楚，再动。

### 六、判据现状

| 套件 | 结果 |
|---|---|
| `vml-out-probe`（31 条，含新增 `nat.bas`） | **31/31**（59s） |
| `vml-abi-probe` | **7/7**（9s） |
| `vml-diag-probe/examples-build`（83 例） | 82/83 —— 唯一失败 `forth/parserexp_demo.fs` **预先存在**（forth 语言库只有 `vmlui.vml`，`word_parserexp` 本就解析不到，自 v0.96.153 并入起如此） |
| 22 语言 sysinfo（桌面） | **21/22** —— 差的 `ladder` **不是缺陷**：它是 PLC 梯形图前端，**没有字符串实参的函数调用**，那份 `sysinfo.ld` 只证明模块还能链接（文件里就写着） |

### 遗留（已诊断，未修）

1. **ObjC 前端「函数返回 double」整条链**：`double d = parserexpf("...")` 得 `16777216`，
   `(double)parserexpf(...)` 得 `-2147483647`。不是压参问题。
2. **Forth 蹦床与 R15 语义**（见 §五）。
3. **`vsscanf` 没有浮点转换**：switch 里只有 `d/i/x/X/s/c`，`scanf("%f", &x)` 静默不赋值。

## v0.96.324 — 护栏按「健康时长」定尺寸：全量预算 30 分 → 10 分、加连续超时闸、进度与耗时可见

**起因是用户的一句批评**：v0.96.323 刚把护栏加上，但全量预算给的是 **1800 秒（30 分钟）**，
而本仓桌面全套例程健康时长约 **3.5 分钟** —— 10 倍余量意味着**真出问题也要等半小时它才说话**，
那种护栏等于没有。**等待时间本身就是成本**，护栏的尺寸必须对着健康时长定，不是对着"绝对不会误报"定。

### 一、全量预算：1800 → **600 秒**（≈ 健康时长的 3 倍）

慢机器不假红，真卡住十分钟内一定停。

### 二、新增「连续超时闸」（`PROBE_TRIP_LIMIT`，默认 3，退出码 **4**）

单看某一条超时说明不了什么（可能就那一个例程坏）；而**连着 3 条都超时是另一种结论** ——
不是某个用例的问题，是**编译器/环境整体挂了**，这时候把剩下 80 条各等一遍毫无意义。

⚠ 它与预算是**两个判据，回答两个不同问题**（"等够了没" vs "是不是全坏了"），
混成一个会让报错说不清是哪一种 —— 这与退出码 3（预算用尽）≠ 1（有用例失败）是同一条思路。

### 三、进度输出 + 耗时可见

- 全量例程现在每一条都打一行 `[ 67/ 83] objc/sysinfo.m`。
  从前**跑完才输出**，中途一个字节都没有 —— 「在跑」和「卡住」在屏幕上完全一样
  （用户为此问过两次「卡是没有」，我自己也把它当成挂住过）。**长任务必须有可见的状态边界。**
- 四个套件的汇总行都带上 `耗时 Ns（预算 Ns）`，**"跑了多久"从日志里读得出来**，不靠人回忆。
- 加进度时顺手把 `Examples/` 的**排除规则收成一处**（原来在循环体里，我在上面又写了一份 ——
  两份过滤器必然漂移，本仓头号坑）。

### 四、两处归因修正（"归因错了就等于报错不对"）

- **预算把时限夹小时，报的是"实际等了多久"**，不是配置里那个数：
  时限被夹到 1 秒时用例确实会超时，但那时报「120 秒没返回」是假话（它只等了 1 秒），
  会把排查引向"编译器卡死"这个错方向。
- 同上：夹小时会**明确说是全量预算夹的**，而不是"本用例卡死"。

### 五、两条踩坑（都是本仓记过的老坑）

1. **双引号里的反引号被 bash 当命令替换执行** —— 报错文案里写了 `` `PROBE_BUDGET` ``，
   运行时打出 `PROBE_BUDGET: command not found` 并把这行输出嵌进提示里。
   `run-langs.sh` 头部正好有这条警告（"别用带反引号的 heredoc 写它"），我还是踩了。改用「」。
2. **`RUN_TIMEOUT_USED` 设在子 shell 里回不来** —— 调用点是
   `out="$( … run_with_timeout … )"`，那是**命令替换的子 shell**，
   在里面赋的变量传不回父 shell（`set -u` 下直接 `unbound variable`）。
   改成 `budget_clamp`：由调用方**在父 shell 里**先把时限算出来。

### 判据（都在改完之后实测）

| 判据 | 结果 |
|---|---|
| 全量预算闸 | `PROBE_BUDGET=5` ⇒ 跑完第 1 条即报「全量预算用尽」、退出码 **3** |
| 连续超时闸 | `EX_TIMEOUT=1` ⇒ 连超 3 条即报「整体挂了」、退出码 **4** |
| 单次超时判 FAIL（不是"通过"） | `EX_TIMEOUT=1` ⇒ 83/83 判成超时 |
| 例程全量 | **82 / 1**（唯一失败仍是 `forth/parserexp_demo.fs`，即 v0.96.323 记的那个 Forth `#param lib` 链接层缺陷） |
| diag-probe / out-probe / abi-probe | 61/61 · 30/30 · 7/7 |

---
## v0.96.323 — 防卡死补齐到「编译 + 测试」两侧 ＋ Examples 基线失败 3→1

**一、`Examples/` 从 80/3 到 82/1。** `_selftest/out.<ext>` 与
`scripts/vml-out-probe/langs/out.<ext>` **是同一份语料的两个副本**（`_selftest/README.md`
自己写着「判据在 run-langs.sh」），而其中 **4 份漂了**：`f90` / `ld` / `js` / `pas`。
probe 那份是**硬化过**的（带着血汗注释），`_selftest` 那份是没硬化的旧副本 ——
其中 `out.js` 最典型：缺 `native function` 声明 ⇒ **编得过、跑起来一个字都不输出**
（编译期检查照不出来，所以它一直是"绿"的）。按本仓优先级（**编译器 > 例程**）改例程：
把 4 份换成 probe 的硬化版，两份副本现已**逐字节相同**。
剩下 1 个 `forth/parserexp_demo.fs` 见下（不是例程的错）。

**二、Forth：链接期的「未定义函数」被误报成「内部错误」。** 上一段把两个 `catch` 从
「换成空程序返回」改成上抛时，漏了 `UnresolvedSymbolException` —— 它落进兜底，
被包成 `<input>: 内部错误: …`，用户看到的是「编译器坏了」而不是「我调了个不存在的函数」。
本仓对这个口径有明文（`CompilerHelper.CompileWithDiagnostics` 里那条注释）。
修法：两处 `catch` 收成一个 `Classify(ex)`（返回 `null` = 原样上抛），四档一个地方判。

**三、新查出一条缺陷（未修，已入账本）**：Forth 里**任何** `#param lib(默认集之外的模块)`
都会让 `print_int` 这类本该由包装器提供的函数变成**未定义** ——
`#param lib("base64")` + `42 . CR` 就能复现；`math` 不坏只因为它**本来就在默认集里**。
这正是 `parserexp_demo.fs` 编不过的原因：在 Forth 里**声明依赖**这条路本身是坏的。
**已证伪的假设也记下了**（怀疑前端提前用残缺清单链了一次 —— 删掉那个分支症状一字不变），
免得下一个人重走；账本里写清了下一步该在哪儿打点。

**四、防卡死补齐（用户要求「编译和测试都要有防卡死机制」）。** 此前只有**编译**那一侧
有机制（`ProgressGuard` 运行期兜底 + `Check(EOF)` 源头修复 + 手机端 180 秒看门狗 +
`HangProbe` 截断判据）。**测试**那一侧的缺口是真的：

- **`examples-build.sh` 完全没有超时**（它自己注释里写着「别用 timeout」，理由是
  macOS 没有它会 command not found ⇒ 假绿 —— 那条**半对**：它诊断对了病，方子开错了，
  没有超时的下场是**整个套件挂死**）。而且它的判定是**看输出里有没有"编译失败"字样** ⇒
  被杀掉时输出为空，会被读成**通过**（假绿的老病根）。
  现在：超时按**退出码**单独判 FAIL；`timeout` 缺失时走 POSIX 手写兜底，两条路都真会超时。
- **`check-vml-patches.sh` 没有超时** —— 它要跑 GenLib 重编 100 个模块，卡住就是永久挂住。
- **桌面自测（6133 条）没有看门狗** —— 一条 Check 卡在死循环里，`--test` 永不返回，
  输出停在最后一条 ✅ 上，**看不出卡在哪一条**。新增看门狗（后台线程，180 秒无任何测试项
  完成就打印「最后完成的一项 ⇒ 卡死的是它后面那一项」并以退出码 2 结束），
  阈值可用 `WAYCODER_SELFTEST_WATCHDOG_SEC` 压低、`WAYCODER_SELFTEST_HANG=1` 注入空转 ——
  **给了它一个能自己验收的入口**（实测注入后 5 秒报出、退出码 2）。
- **三份重复的超时实现收成一个** `scripts/lib/portable-timeout.sh`；
  原本「都没有 timeout 就**不加外壳**」的那两处（`diag-probe` / `out-probe`）也改成真有兜底 ——
  vmlcli 的 `--timeout` 只管 **VM 层跑不完的程序**，**前端编译阶段卡死它管不着**。

---
---
## v0.96.322 — 「块必须闭合」立成判据（探针四档 22/22）＋ 防挂死的截断探针

> 用户定的规矩：**所有语言的块必须闭合，不闭合的代码必须报错**。
> 这一轮把这句话变成一条**能跑的判据**，然后按它把 22 门全过一遍 ——
> 查出 **4 门静默接受未闭合块**、**1 门报错但归因错了**，**1 门的样本是我自己写错的**。

### 一、立判据：`DiagProbe` 新增第四档「块未闭合」

前三档（未定义标识符 / 语法错误 / 头文件里的错）问的是「**报得准不准**」，
这一档问的是「**报不报**」—— 与用户那句要求一字对应。

- **22 门各一份样本，恰好只差最后一行闭合符**。为证明"只差这一处"，
  闭合版**逐门实测都能编过**（22/22）⇒ 本档一旦红，红的必然是"未闭合没报错"。
- `Expected = 0` = **不判行**（新增约定，与既有的 `ExpectedColumn = 0` 同一套）。
  锚在"开块那一行"还是锚在 EOF，各门习惯不同且都说得通（GCC 报 `end of input`，
  本仓 C 修完锚在开括号）—— 硬钉一个期望行号等于把一种随手选的约定变成判据。

### 二、判据查出的六门（修前 / 修后）

| 门 | 修前行为 | 真身 |
|---|---|---|
| **C** | `int main() {` + 几行 ⇒ **编译成功、退出码 0** | `ParseBlock` 的「容错: 块未正常关闭」是段**死代码**（判据恒假），EOF 那一支**一声不吭**返回 |
| **Scheme** | 四种畸形全静默收下：未闭合 `(`、多余的 `)`、裸 `'`、`'` 后无表达式 | 表达式兜底是无条件的 `new SSym(Advance().Value)` ⇒ `(display "a"))` 里多出的 `)` 被当成名叫 `")"` 的符号 |
| **Forth** | 报了错**却仍编译成功**（`: sq` 缺 `;` ⇒ 44516 条指令、退出码 0） | ① `ParseProgram` 把解析异常 `Console.WriteLine` 一句就 `break` **吞掉**；② 外层 `catch` 把编译错误换成 `CreateErrorProgram` 的空程序 ⇒ `Compile` **永不失败** |
| **Fortran** | `program p` 不写 `end program` ⇒ 编译成功 | 顶层循环**只在 `end program` 或 EOF 退出**，EOF 那一支没有判据 |
| **BASIC** | `IF ... THEN`（块式）不写 `END IF` ⇒ 编译成功 | 那句 `// Consume END IF if present` —— "if present" 就是"没有也不管" |
| **Pascal** | **报错了，但报的是「解析未收敛（编译器内部缺陷，请报告给开发者）」** | `ParseBlock` 的循环**没有 EOF 出口** ⇒ 真·空转，全靠运行期兜底 `ProgressGuard` 兜住 |

**Pascal 那门最值得记**：兜底**兜住了不挂死，但把"用户少写一个 `end.`"说成了"编译器坏了"** ——
位置是对的、归因是错的，用户会照着那句话去提 bug。**兜底不是诊断**；现在报
`2:1: error: 块未闭合（缺少 'end'）`（锚在 `begin`）。

**另一门是我自己写错的**：`ld`（ladder）第一版样本写的是 `PRINT_INT 1`（裸打印语句）
—— 探针报 NOERR，看着像"ladder 不收块"，其实**是样本里根本没有块**：
`END_PROGRAM` 在本方言**是可选**的（三份随包例程一份都没写），裸语句模式本来只有打印语句。
真正的块是 `IF/END_IF`、`WHILE/END_WHILE` —— 实测**都会报错**。
**先怀疑用例，再怀疑实现**（本仓 `undef-fn.go` 写成 Rust 语法、ladder `END_PROGRAM` 两笔前科）。

### 三、`HangProbe`：把「不许挂死」也立成判据

`--truncate` 从「运行期兜底」升级为**判据**：把 `Examples/<语言>/` 的**真实例程按行截断**
成几十份不完整的源码，逐份编译，**判据只有一条 —— 必须返回**。

- 档位：`HANG`（超时未返回）/ `CRASH`（未捕获异常）算失败；`ERR`/`PASS` 不算
  （截断点落在完整语句之后本就该通过；**`PASS` 不是"静默错编"的判据**，那要另开一档）。
- **`--selftest` 自证判据能响**：拿死循环 / 抛异常 / `CompilationException` / 正常返回
  四个行为已知的假编译入口喂它，要求各判出对的档 —— 本仓铁律「不响的自测比没有更糟」。
- 基准**按"结构点"挑，不按行数**：第一版写「行数 8~120 里最大的」，结果 **22 门里 14 门判 NOBASE**
  （例程是两极分布的：1~10 行的演示，或 120~870 行的整程序）—— 行数既不是"有没有结构"的判据、
  也不是"切得动切不动"的判据。改后**探针自身问题 0 门**。
- **一门一次进程**：挂住的那条线程停不下来（.NET 没有安全的中止线程），
  留着它空转可能占住编译器里的静态锁、让后续每一刀**假红成"挂住"** —— 那就再也分不清
  "真挂"与"被带累"了。

**实测**：22 门 × 1220 刀，**HANG 0 / CRASH 0**。
C 那一门从 `ERR=8 / PASS=112` 变成 `ERR=107 / PASS=13` —— `ParseBlock` 的修复在截断探针上直接现形。

**试过并否决的一条路**（记下来免得下次再走）：先写了一个「静态扫不推进的循环」的脚本，
结论是**它抓不到它要抓的那个 bug** —— Dart 那个循环体里**有** `Advance()`，
坏的是条件里 `Check(EOF)` 永假。而且它要么报 0 条（把 `ParseStatement()` 当推进），
要么报 40 条误报（递归下降的正常写法）。**一个永远不响或永远乱响的检查等于没有**，已删。

### 四、`Lib/` 重生成：结清一笔"签入产物落后于前端"的债

`scripts/check-vml-patches.sh` 抓出 **14 个 `Lib/shared/*.vml` 与重生成结果不一致**。
先判"是我这次改的还是本来就有的"：**回退我的 C 前端改动后差异照旧** ⇒ 债来自
**已提交的源码行注释那批工作**（前端比签入的 `Lib/` 新）。

14 个文件的差异**逐行核过，全是 `; 行号: 源码` 注释**（40 行新增 / 1 行改动）：
for / case / 多行表达式这些原本没注释的语句现在有了 —— 正是"报错位置准确"那条的收益。
**没有一处语义变化。**

⚠ **`GenLib -A` 单独跑是假绿的**：`-b` 是「`.vml` 比 `.c` 旧才重编」，而签入的 `.vml`
比源新 ⇒ 整批跳过、一个都没重生成、比对自然全过（那条脚本注释里记的就是这个形态）。
正解是先 `touch Lib/shared/src/*.c` 再跑。**按本仓优先级，这是"共享库适配 C 编译器"**。

### 五、手机端跑的是旧标准库

重打 `vml_lib.zip` 时脚本报：**签入的包与仓库 `Lib/` 原本就不一致**
⇒ 这段时间里**手机上跑的是旧标准库**（桌面看不出来，因为桌面不读这个包）。
`vml_lib.zip` + `vml_lib.hash` 已一起重建提交；版本号同步升到 v0.96.322，
`EnsureExamples()` 的版本闸门会让设备重新解包。

### 判据（全部实测）

| 判据 | 结果 |
|---|---|
| `DiagProbe` 四档 | **22 / 22 / 22 / 22** |
| `vml-diag-probe`（CLI 层，61 条） | 61 / 61 |
| `vml-out-probe`（28 语言输出逐字节） | 30 / 30 |
| `vml-abi-probe`（调用约定） | 7 / 7 |
| `HangProbe`（22 门 × 1220 刀） | HANG 0 / CRASH 0 / 探针自身问题 0 |
| `Examples/` 全量编译 | 80 / 3 —— **失败集与基线逐字相同** |
| 桌面自测 | 6133 / 0 |
| `check-vml-patches.sh` | 三条全绿（`Lib` 1884 个产物逐字节相同） |

### 仍未做（如实）

- **`Examples/` 那 3 个失败仍是基线原样**（`_selftest/out.f90`、`_selftest/out.ld`、
  `forth/parserexp_demo.fs`）。按本仓优先级（**编译器 > 例程**）它们该在例程/库那一层解决，
  但**得先判清是哪一层的错**，不能为了绿灯去放宽编译器：
  `out.f90` 写的是 `print "..."` 而 Fortran 要 `print *, "..."`（**例程错**）；
  另两个待判。
- **「静默错编」只清了"未闭合"这一类**。探针的 `PASS` 档不构成判据
  （截断到完整语句本就该通过），要查完整类得为每门写"这段源码**本该**报错"的样本。
- **`ld` / `fth` 的截断覆盖薄**（5 刀 / 9 刀）：这两门的例程要么只有 1~2 行、
  要么是扁平的词序列，切不出更多形态。要加覆盖得先加多行例程（**例程适配编译器**，不是反过来）。

---

## v0.96.321 — 22 门前端的报错位置全部校准（探针三档 22/22）＋ 防编译器卡死

> 承接 v0.96.319 结尾那句「【语法错误】档 3 PASS / 8 行不对 / 6 无位置 / 5 没检出 —— **未修**」
> 与 v0.96.320 的「错误两分」。这一轮把**剩下每一门**的位置问题清完（31 个提交，一门一提交），
> 并顺手挖出、修掉一条**跨 8 门、104 处的潜伏死循环**。

### 一、判据先行：`DiagProbe` 三档从「半红」到全绿

| 档 | 会话开始时 | 现在 |
|---|---|---|
| 【未定义标识符】 | 22 / 22 | **22 / 22** |
| 【语法错误】 | 3 报得对 / 8 行不对 / 6 无位置 / 5 没报错 / 4 **崩溃** | **22 / 22** |
| 【头文件里的错】 | 19 / 22 | **22 / 22** |

`vml-diag-probe` 61/61、`vml-out-probe` 30/30、`vml-abi-probe` 7/7、桌面自测 6134/0、
`Examples` 79/3 且**零超时**（3 个失败与基线相同）—— 全程与基线逐字对齐。

### 二、病因分五类，不是「位置算错了」一句话

**① 解析器直接崩（cs / go / pas / scm）** —— 修前报的是
`内部错误: Object reference not set…`，**一句位置都没有**。四门同一形态：解析器接受
「缺操作数的表达式」、编出畸形 AST（右子节点为 `null`），解析期一句不报、**到代码生成才 NRE**。

**② 静默错编（cpp / java / dart / c）** —— 比崩更糟：**编得过、跑得动、结果是错的**。
- `cpp`：兜底 `return new IntLiteral { Value = 0 }` ⇒ `int c = a + ;` 编成 `a + 0`
- `java` / `dart`：同样静默造 0，**还顺手把外层的 `;` 吃掉** ⇒ 再级联出一条「期望 ';'」假错
- `c`：`Compile(string)` 的**成功路径不看诊断包** ⇒ 收进去的错被整个丢掉
  （`CompileWithDiagnostics` 里早写着「成功路径也要看 bag」，C 这条手写路径漏了）

**③ 错误被吞进没人读的地方（swift / bas）**
- `swift`：`Parse()` 把异常写进 `hasError`/`errorMessage` 两个字段然后返回空程序 ——
  那两个字段**全仓只有写、没有读**
- `bas`：`default: return null` 静默递上去 ＋ **自维护游标**（`current`）让位置永远报 1:1

**④ 位置锚错（py / lua / rb / r / kt / ld）** —— 报在「撞上的**下一个** token」上：
`x = 1 +` 的下一个 token 在**下一行**，于是报到下一行去。

**⑤ 根本没接上统一出口（go / pas / forth / js / rs / swift）** —— 自己拼位置、或用
**两参/三参构造**（`line = 0`）⇒ 位置要么形状不对、要么压根没有。

### 三、新接缝：**位置取「缺口在哪」，文案取「看到了什么」**

统一到 `ParserBase` 的三处，逐语言只提供**它自己才知道的东西**（token 分类）：

| 接缝 | 作用 |
|---|---|
| `ErrorAt` / `GccErrorAt` | 锚定版报错（抛 / 收集），位置取**指定 token** |
| `ResolveAnchor` | 锚定类位置计算的**唯一**实现（与既有位置映射同一套） |
| `GapAnchor()` | **该不该锚**的判据：撞上的须是**收尾符**、上一个**不能是语句分隔**、两者**要跨行** |
| `CurrentToken` | 自维护游标的前端（cs / bas）覆写它报位置 |

`GapAnchor` 的三条判据缺一不可 —— 我第一版「一律锚上一个」把 `class P {` 判成了语法错误
（3 条 cs 用例全变成 `1:1 遇到 Class 'class'`）；撞上的若是普通 token，**它自己就是问题**。

### 四、链接期那一半（js / scm / fth）

这三门只经**链接器**报「未定义的函数」，而 `ReportUnresolved` 手上是**拼接后**的行号 +
硬编码 `<input>` —— 正是上一轮在解析器/代码生成器上修掉、链接期漏掉的那一半。

映射表**跟着程序对象走**（`VmlProgram.SourceLineMap`），实现搬到 `VMLAssembler`
（依赖方向 `CompilerBase → VMLAssembler` 单向，链接器够不着编译器那边的表）。
**盖章处选在 `BuildProgram`** 而不是编译出口 —— 有的门（JS）在**自己的编译委托内部**就调了链接，
盖在出口是「先链接、后盖章」（第一版就这么漏掉 `js` 的）。
**不进 `.vml` 文本格式**：文件链（编译→写 .vml→汇编→链接）行为逐字不变。

### 五、词法错误也收成一套形状

`LexerBase.Error` 此前是**两套**：映射指向别的文件才用 `文件:行:列:`，否则退回
`…第N行M列：…` —— 那套「我们自己看得懂」的形状**宿主认不出来**，于是**词法错误在编辑器里
一行都锚不到**，而解析错误锚得到。现在统一。另修：`go` / `python` 的词法错误**一个位置都没有**
（三参构造 `line = 0`；Go 那处还算了两行源码行 + 插入符却**从没用上**），
`dart`/`fortran`/`lua`/`pascal`/`ruby` 各自手拼的位置一并收敛。

### 六、防卡死（用户要求：「要防止编译器编译卡死或者死循环」）

**真因不在 Dart，在共用基类**：

```csharp
protected bool Check(TTokenType type) =>
    !IsAtEnd && EqualityComparer<TTokenType>.Default.Equals(GetTokenType(Cur), type);
//   ^^^^^^^^^ 这个短路让 Check(EOF) 永远返回 false
```

于是 `while (!Check(RBrace) && !Check(EOF))` 在**跑到输入末尾**时两个条件同时为假
⇒ 等于 `while (true)`，而循环体里的 `Advance()` 到末尾也不再推进 ⇒ **死循环**。
最小触发：`void main() {` 一个文件就让编译器**永不返回**。
机械扫过：**104 处 / 8 门**，`Match(...EOF)` 另 2 处 —— 语料里全是完整文件，所以一直没露头。

**修法代价为零**：22 门的词法器**全部**在末尾追加 EOF 哨兵（逐个核过）⇒
`Check(EOF)` 该为真、其余类型该为假，与从前**只差 EOF 这一格**。
顺带修掉第二条：`class A {` / `void main() { x` 这类**体没闭合**的输入此前是**静默编过**。

**另加与缺陷无关的兜底 `ProgressGuard`**：**「位置长时间不动 ⇒ 判定死循环」**，
抛一条带位置的错而不是挂死；解析器 `Cur` 与词法器 `Peek` 两处热路径各挂一个。
判据选「位置不动」而非「总步数超限」是刻意的 —— 后者对**大文件必然误报**，
前者在合法输入上不可能发生（两次推进之间只读常数次），所以阈值给到百万级仍然精确。

### 判据

- `void main() {` / `void main() {`↵`  var s = "abc`：**挂死（124）→ 报错退出（1）**
- DiagProbe 三档 **22/22**；`vml-diag-probe` 61/61、`out-probe` 30/30、`abi-probe` 7/7
- 桌面自测 **6134/0**；`Examples` **79/3 且零超时**（与基线相同）
- 三类机械扫全部归零：两参/三参构造（不带位置）、词法器自拼位置、`Error(...)` 忘 `throw`
  或绕开基类的 `Error` 覆写

### 工具两处

- **DiagProbe 加 `--stack`**：`CRASH` 那一档正文常是一句 `Object reference not set…`，
  而真因与调用栈**在 `InnerException` 里**（`CompilerHelper` 把它包成
  `CompilationException("<file>: 内部错误: <msg>", ex)`）—— 只看 `ex.ToString()` 得到的是**包装层**的栈。
- **`vml-abi-probe` 的 `$TMPDIR` 在 `set -u` 下假红**（Git Bash 无此变量 ⇒ 7 条全 FAIL）⇒ `${TMPDIR:-/tmp}`。
- **`DiagProbe` 的 fth 语法档样本漏了 `PostLinkLang`** —— 量到的是「探针没接链接」而不是前端行为，
  长期显示 `NOERR`。同类「用例写错了」本仓有前例（`undef-fn.go` 当年写成了 Rust 语法）。

### 仍未做（如实）

- **探针只覆盖三类错误**（未定义标识符 / 语法错误 / 头文件里的错）。词法错误、代码生成错误、
  链接警告的位置虽已统一形状，但**没有独立的探针档**钉住 —— 下次动这几层时没有自动判据。
- **「静默错编」只清了探针样本那一类**（缺操作数 / 兜底造 0）。没做过系统扫描：
  还有哪些「认不出就静默当 0 / 静默丢掉一段」的分支。这一类的危害不低于崩溃（编得过、结果是错的）。
- **Dart 那条挂死只修了触发它的那一个循环**。`Check(EOF)` 的 104 处现在**行为正确**了，
  但「循环体不推进」这种写法本身没有静态检查 —— 靠 `ProgressGuard` 在**运行期**兜住
  （它会报错，不会挂死；但报出来的是「解析未收敛」，不是「第几行写错了」）。

---
## v0.96.320 — 编译器崩溃修复 ＋ 错误处理两分（能继续的多报、不能继续的停）

> 承接 v0.96.319 结尾那句「【语法错误】档 3 PASS / 8 行不对 / 6 无位置 / 5 没检出 —— **未修**」。
> 这一轮把它落地，并按用户定的原则重做了错误处理：**错误分两类，能继续的要尽量多报，
> 不能继续的才停**。中间两个提交（`cb063ca3` 报错中文化、`2edac8e0` 头文件行号接线）当时按
> 「检查点」直接落的、没走日志，一并补在第一节。

### 一、头文件行号接线（21 门）＋ 报错中文化（部分）

**缺的不是管道，是投递。** 上一轮把三个消费端都建好了（`LexerBase.GccError/Error`、
`ParserBase.ResolveDiagnosticPosition`、`CodeGeneratorBase.DiagFile/DiagLine`），而
`Preprocessor.LineMap` 算出来后被 21 门**逐门丢掉**（`source = pp.Process();` 之后再没人提它）。
四处共享改动把投递接上：`Preprocessor` 登记「产物引用 → 映射表」→ 词法器凭**引用相等**认领
→ 解析器兜底取生效表 → `DiagPosition()` 把游标行换回原文件。**20 门前端一行代码没改**。

引用相等是安全性关键：21 门都把 `Process()` 的返回值原样传给 `new Lexer(source)` ⇒
「这次没预处理」绝不会捡到上次编译的陈表（那会报出另一份文件的行）。

顺带修掉 C 的同族形态：它只带 `ASTNode.OriginalLine`、不带 `OriginalFile` ⇒ 报「主文件名 +
头文件行号」，正是要修的那个 bug 的另一种形态。

报错中文化：22 门前端 + `CompilerBase` 里面向用户的英文消息已转中文（**还剩 Ruby 词法器 2 条**）。

**仍未做**：`js`/`scm`/`fth` 三门只能经链接器报「未定义的函数」，而
`LibraryLinker.ReportUnresolved` 手上是**拼接后**行号 + 硬编码 `<input>`。要修得把
「文件 + 原行」一路带进 VML 文本格式（产品链是 编译→写 `.vml`→汇编→链接）。

### 二、四门前端在语法错误上**直接崩**（cs / go / pas / scm）

【语法错误】档报出来的第 4 个「无位置」其实不是「没位置」，是**编译器自己崩了**：

| 门 | 修之前用户看到的 |
|---|---|
| `cs` `go` `pas` | `<input>: 内部错误: Object reference not set to an instance of an object.` |
| `scm` | `<input>: 内部错误: Index was out of range.` |

四门是**同一个形态**：解析器接受了「缺操作数的表达式」、编出畸形 AST（`BinaryExpression`
右子节点为 `null`，或 `(+ 1)` 这种参数少一个的列表），解析期一句错都不报，
**到代码生成才 NRE**，被 `CompilerHelper` 包成一句没有位置的「内部错误」。

修法不是给代码生成加 null 判据（那是把症状按下去），而是**在解析器认出问题的那一刻报出来**：

- cs / go：新增 `RequiredOperand()`，包在**每一个操作数位**上（cs 16 处、go 14 处，
  含二元/一元/三元/解引用/取地址）
- scm：`GenCall` 链首一处**元数检查**（覆盖下面所有按下标取参的分支）
- pas：`ParseFactor` 认不出表达式时不再返回 `null`

### 三、错误分两类：**能继续的多报，不能继续的才停**

用户原话：**「错误有两种，一种不影响往下编译，一种是完全无法继续下去，前面一种可以报多个错误，
后面一种报错就编译停止了。尽量多报错误」**。

落到代码上是一个新接缝 `ParserBase.Collect(ex)` —— 把**已经算好位置**的 `ParseException`
**原样收进诊断**（`Code`/`File`/`Line`/`Column`/`BareMessage` 五个字段都在异常上），
配合**异常过滤器**用：

```csharp
catch (ParseException ex) when (Collect(ex)) { /* 只负责恢复：跳到同步点继续 */ }
```

没有诊断收集器时 `Collect` 返回 `false` ⇒ 自动退回抛出，**绝不凭空吞掉**。

⚠ **「恢复」与「吞掉」是两件事，本仓在这上面栽过两次**：

- cs `Parse()` 是**无过滤的裸 `catch`** —— 把带位置的 `ParseException` 与内部异常一视同仁地吃掉、
  连一行日志都没有，然后「跳过当前语句、继续编」。用户零错误提示，程序照编出来。
- go `ParseProgram`/`ParseBlock` 把异常打到 **stdout**（用户根本看不到）再恢复。

两处现在都改成「**收进诊断 + 恢复**」：错报了、编译整体照样失败，而一次能报出尽可能多的错。
恢复还都补了**推进保证**（`_pos` 一个都没动就至少吃掉当前 token）—— 否则出错点正好落在
同步点上时，外层 `while` 会拿同一个 token 原地打转。

### 四、`Error(...)` 忘了 `throw` ＝ **构造完直接丢掉**（Pascal 7 处）

Pascal 的 `Error` 覆写是 `=> new ParseException(…)` —— **返回异常、自己不抛**。而 7 处调用点写的是

```csharp
Error("期望表达式");   // ← 构造了一个 ParseException，然后丢掉
return null;
```

错**一个字都不会出现**，返回的 `null` 一路流到代码生成再崩。全仓机械扫过一遍：
`CCompiler` 的 `Error` 体内自带 `throw`（写法不同但安全）、词法器的 `Error` 是 `void` 且内部抛，
**只有 Pascal 这 7 处是真的丢**，已全部改掉。

### 五、位置：Go / Pascal 接回统一出口

两门各有一条**绕开基类**的报错实现，各少两件事（没有 `文件:行:列: error:` 前缀 ⇒ 编辑器锚不到行；
没走 `ResolveDiagnosticPosition` ⇒ **不查 `#include` 行号映射**）：

- go：删除 `override ParseException Error(...)`，改用基类（`Token` 实现了 `ITokenPosition`，
  取到的行列与原来手写的**完全同源**，删掉只补上前缀与映射，位置一个字不变）
- cs：基类 `ResolveDiagnosticPosition` 读的是基类游标 `Cur`，而 C# 前端**自己维护 `position`**、
  把 `Peek`/`Advance`/`Check`/`IsAtEnd` 全 `new` 掉了 ⇒ 基类 `_pos` 从不移动、**每条语法错误都报
  1:1**。新增基类接缝 `CurrentToken`（默认 `Cur`），自维护游标的前端覆写它即可 ——
  **不做「两个游标同步」**（那正是本仓反复踩的「同一件事两处实现」）

### 六、Pascal `CompileFile` 没接诊断收集器 ⇒ 整个前端退化成「只报一条」

`GccError` 见 `Diagnostics == null` **只能抛**（它没有地方可收）⇒ 文件里后面的错全部看不到。
实测一份有两处独立错的文件只报出第一条。`CompileFile` 是 Pascal 手写的一条流水线
（不像其余 17 门走 `CompileFileStandard` → `Compile` → `CompileWithDiagnostics`），
接上收集器后两处都报出来了。

### 七、工具两处

- **DiagProbe 加 `--stack`**：`CRASH` 那一档正文往往就是一句 `Object reference not set…`、
  四个字都没有信息量。而真因与调用栈**在 `InnerException` 里**（`CompilerHelper` 把它包成
  `CompilationException("<file>: 内部错误: <msg>", ex)`）——只看 `ex.ToString()` 得到的是
  **包装层**的栈（就在包装点上），对定位毫无用处。这一条实测被骗过一轮。
- **`vml-abi-probe` 的 `$TMPDIR` 在 `set -u` 下假红**：Git Bash 里没这个变量 ⇒
  `unbound variable` ⇒ **7 条全部 FAIL**，看上去像「整个调用约定塌了」。改成 `${TMPDIR:-/tmp}`。
  （这类假红最坏：它训练人去忽略红灯。）

### 判据

| 判据 | 结果 |
|---|---|
| DiagProbe【语法错误】 | PASS **3 → 7**、崩溃 **4 → 0**、无位置 **6 → 2** |
| DiagProbe【未定义标识符】/【头文件里的错】 | 22/22、19/22 —— 与基线**逐字相同** |
| **多报错实测**（一份文件放多处独立错） | cs **3/3**、go **2/2**、pas **2/2**、scm **3/3**（cs 修前是「编译成功」） |
| 桌面自测 | 6134 / 0（与基线同） |
| `vml-diag-probe` / `vml-out-probe` / `vml-abi-probe` | 61/61、30/30、7/7 |
| `vml-diag-probe/examples-build.sh` | 80/3 —— **用 baseline DLL 复跑过，同样 80/3、同样那 3 个文件**（fortran/ladder/forth，本轮没碰，属既有红灯） |

三档的判定沿用探针的六档口径：`NOPOS`（报了错但没行号）与 `NOERR`（该报错没报）**各自单列**，
不并进 FAIL —— 「跑通了」既不是「位置对」也不是「位置错」，是另一种故障。

### 仍未做（如实）

- 【语法错误】档余下：**8 门行不对**（`java kt dart ld py rb lua r` —— 错报在**下一个 token**
  的行上，`1 +` 结尾时报的是下一行的 `NEWLINE`/`EOF`）、**`rs`/`js` 格式不符**（位置其实是对的，
  只是不走 `文件:行:列:` 那个形状）、**5 门静默接受**（`c cpp swift bas fth` 把 `int c = a + ;`
  整份编过 —— 静默错编，按严重度不亚于崩溃）
- **另外 18 门的「两类错误」没系统过一遍**：本轮只改了测试面里的 4 门 + 抽出的共用接缝
- 另有 **C×2 / Forth / Ladder** 三处 `new Parser(...)` 没接诊断收集器

---
## v0.96.319 — 诊断气泡画布化 ＋ 配色定稿 ＋ 报错位置准确性

> v0.96.316~318 是这一轮的中间真机验证版（装在手机上逐轮调观感用的），未单独记日志。

### 一、诊断气泡从「叠加控件」改成「画布绘制层」

用户原话：**「显示气泡也是绘制层，不用做成单独控件，画在代码上层即可，箭头对准错误位置」**。
原实现是叠在编辑器上的一层控件，于是**气泡与它指的那行代码是两套坐标系** —— 一滚动就得追着
同步，字号一变就得整层重排。改成一体的画布绘制之后：

- **形状是一体路径**（不是「圆角矩形 + 另画的三角尾巴」）：左上角不收圆角、直接收成**尖**，
  尖端落在锚点（错误那一格的左缘 × 该行下缘）上。用尾巴的话，错误在第 1 列时锚点已在正文最左、
  **没有向左伸的余地**，要么尾巴被裁掉、要么气泡右移而尖又对不准。
- **气泡只往右下展开，不做任何避让**（用户明确要求）。左侧画不下的问题由「尖在左上角」这一条
  天然解决。
- **宽度固定（默认 32 字符，可在设置里改）、高度随内容**；四周内边距 = 半个字号。
- **所有几何都由字号推导**（尖 0.9×0.6、圆角 0.6、✕ 1.25 字号…）⇒ 捏合缩放时气泡与代码**一起**
  变大变小。
- **行不可见就不画**（跟随行的气泡同理），超宽不裁剪、靠滚动看。

### 二、同一位置多个诊断：按严重度分组上下排

同一个 (行, 列) 上有多个诊断时合成一处、**按严重度分组**各出一个气泡上下摞：**错误合并错误、
警告合并警告**，错误在上。组内多条走编号列表 `1. …` `2. …`，单条走 `第 N 行：…`。

⚠ 顺带修掉一个**从没被触发的几何错**：`BubblePath` 里 `y0 = tipY + tipH` 是**无条件**加的，
而调用方的堆叠循环写的是 `y += bodyH + (withTip ? tipH : BubbleStackGap)` —— 两边都以为
「没有尖就不加」，**只有路径在加**。后果是叠在下面的气泡**多让出一整个尖高**（13 号字 7.8px），
而它本意只要一道缝；并且无尖时路径从 `MoveTo(x0, y0)` 直接拉横线，**左上角是直角**。
现在 `y0 = withTip ? tipY + tipH : tipY`、无尖分支左上角也走圆角，`BubbleStackGap` 下限抬到 2px
（仍随字号缩放）。**命中矩形本来也跟着偏了**（比画出来的形状高一个尖高），一并同源对齐。

### 三、✕ 的语义：从「删诊断」改成「收起气泡」

原来点 ✕ 走的是 `DiagnosticManager.Dismiss` —— **不可逆地删掉那条诊断**，波浪线和错误列表一起
消失。用户要的是「关掉气泡之后只剩波浪线」。现在 ✕ = **收起成小圆点**：气泡收起来，波浪线留着，
**起点画一个小圆点**（错误红 / 警告黄），点圆点能把气泡再打开。整个编辑器另有总开关（全开 / 全关 /
单条），关闭态就是「只剩波浪线 + 小圆点」。

### 四、配色定稿（含一处我自己算错的数）

| | 白天 | 夜间 |
|---|---|---|
| 错误 | `#F19A9D` | `#C4382F` |
| 警告 | `#FACE87` | `#996000` |
| 提示 | `#8DCDAE` | `#17794A` |
| 文字 | 一律 `#1A1A1A` | 一律 `#F2F2F2` |

三条规矩：**色相 = 严重度**、**白天偏亮 / 夜间偏暗**、**字色统一跟随主题**
（日间深字、夜间浅字 —— 这是用户纠正的，原先按每个气泡自己的底色各定各的，导致同一个夜间主题里
红气泡浅字、黄气泡深字）。

⚠ **夜间警告底 `#996000` 这个值是踩了我算错的一个数才定下来的**：我先给了 `#A36600` 并声称
「4.7 : 1 达标」—— 那是拿**纯白**当夜间字色算的，而真实字色是 `#F2F2F2`（近白），实得 4.21:1，
**没过 4.5**。是落地时被核算出来的。**对比度要拿真实字色算，不能用纯白近似。**

### 五、警告一直不显示：成功分支把诊断表清空了

用户真机报**「警告编译没效果，只有错误有效」**。`MauiVml.CompileForEditor` 在**编译成功时也把
警告带回来了**（`compileWarnings`），而 `EditorPage.RunCurrentFileAsync` 的成功分支写着
`DiagnosticManager.Inject(_relPath, [])` —— **写死空表**，那份警告从来没被用过。有错时走失败分支
注入真表（所以错误看得见）、只有警告时走这一支把表清空。改成 `Inject(_relPath, diags)` 后，
气泡、波浪线、错误列表（本来三档都渲染）一起出现。

### 六、报错位置：三条根因

用户报「**我现在打开的文件报 112 行错误，但是这里报错是不对的**」。查下来 112 行是一句无害的
文档注释，而真正的错在 `Lib/c/time.h` 里。三条独立根因：

1. **C++ 前端不认 `typedef struct <标签> <别名>;`**（`ParseTypedef` 只处理带 `{…}` 的两种形态，
   把结构体标签当成别名吃掉，再撞上真正的别名 ⇒ `Expected SEMICOLON but got IDENTIFIER ('tm_t')`）。
   ⚠ 光吃下语法不够 —— **别名必须真的登记**：实测 `pt_t p; p.x=3; p.y=4;` 会打出 `4,4`
   （成员落到同一地址、**编得过、不报错、结果是错的**），这条逼出了结构体定义原先是**整个被丢掉**的。
2. **报错行号用的是「预处理拼接后」的行号**：预处理器把头文件内容拼进同一个流，而报错走的是
   `Cur.Line`。那张「拼接行 → 原文件行」的 `lineMap` **一直在收集、从来没人读**。现在规则收在
   `CompilerHelper.MapOriginalLine` **一处**（词法器/解析器都转调它），并修了映射表自身在
   「无宏定义快速路径」与 include 尾随内容上的**漏记**（整表会错位一行）。
3. **编辑器把文件路径丢了**：`VmlDiagnostics` 解析 `文件:行:列:` 时 `Groups[1]` 从没被用过 ⇒
   凡是**别的文件**来的诊断都被按行号硬贴到用户正在看的文件上。现在 `Diagnostic` 带来源文件，
   **不属于当前文件的诊断不给行锚**（`Line=0`、文件名进消息正文）。

修完同一份 `game17.cpp` 的报错：`第 112 行：Expected SEMICOLON …` →
`game17.cpp:64:43: error: 未声明的变量 'STD_OUTPUT_HANDLE'`（第 64 行正是出错的代码）。

### 七、缺头文件：从「完全静默」改成警告

`game17.cpp` 是 Win32 程序（`Windows.h` / `conio.h`），VML 里没有这些头文件，而预处理器按 GCC 的
「可选头文件」策略**静默跳过**、一个字不说 ⇒ 用户看到的是一串下游的「未声明的变量」，永远推不出
真正的原因。现在缺头文件**报一条警告**（不是错误，不把老程序弄挂）：

```
game17.cpp:6: warning: 找不到头文件 "Windows.h" —— 它不在 include 搜索路径里…
game17.cpp:64:43: error: 未声明的变量 'STD_OUTPUT_HANDLE' [CodeGen_UndefinedVariable]
```

出厂语料（22 门示例 + 三个探针）实测噪声 **0 个文件**。

### 八、22 门前端「报错位置」探针（新工具）

`third_party/vml/tools/DiagProbe/` —— 每门语言一份**最小样本**、在**已知行**埋一个错，进程内调该
前端的 `Compile(source)`，抽出行列与期望值对比：

```bash
dotnet run --project third_party/vml/tools/DiagProbe        # 22 门，约 6 秒；退出码 0 = 全 PASS
```

判据**五档**，`NOPOS`（报了错但无行号）与 `NOERR`（该报错没报）**各自单列、不并进 FAIL** ——
本仓写过「冒烟不许冒充 PASS」，不能把「跑通了」当成「位置对」。

⚠ 探针自己踩过两个坑（已写进注释）：**`VML_HOME` 必须显式设**，否则找不到 `Lib/` 会**静默跳过
链接**，把 6 门动态语言 + Forth 全误报成「前端静默接受」—— **「漏报」和「探针坏了」长得一模一样**；
`Compile(string)` **不等于** `CompileFile`（c/cpp/ladder/forth 的 `Compile(string)` 不调标准库链接，
而「未定义函数」只有链接器看得见）。

### 判据

- 桌面自测 6115 → **6134**（0 红）；新增断言覆盖：本文件诊断锚行 / 别文件诊断不锚行、`<input>`
  占位符、Windows 反斜杠路径、warning·note 级别不受影响、不传参数等于老行为、typedef 三形态、
  头文件报错指到头文件
- `WayCoder` / `WayCoder.Maui`（windows）/ `WayCoder.Maui`（android）**三目标 0 错误**
- `scripts/vml-out-probe` 30/30、`scripts/vml-diag-probe` 61/61、`scripts/vml-abi-probe` 7/7
- 两条关键判据都做过**反证能红**：把 `lineMap` 改回 `null` ⇒ 头文件报错立刻指回用户文件；
  把「编译成功注入空表」改回去 ⇒ 警告消失的断言立刻红

---

## v0.96.315 — 标题文本走行内解析 ＋ 行内代码补底色

真机复验 v0.96.314 时用户报「缺少 `text` 这种格式块的渲染」。截图一看：帮助文档「绘图」页
的标题是 `### \`ui_brush_radial(int color_a, …)\`` 形式，屏上**赫然显示成带一对反引号的大字**。

### 根因：**四端的标题渲染都把 `h.Text` 原样放**，没过行内解析

正文的段落 / 列表 / 表格单元格**早就走行内解析了**，只有标题漏了 ——
所以现象很迷惑：「同一页正文里的 `` `code` `` 正常，标题里的不正常」。四端一起修：

| 端 | 之前 |
|---|---|
| MAUI `MarkdownPreview.RenderHeading` | `Text = h.Text` |
| MAUI `MarkupToFormattedString` MdHeading | `new Span { Text = h.Text }` |
| GUI `MarkdownBlocks.Heading` | `new Run(h.Text)` |
| TUI `TuiMarkdown.RenderHeading` | `(h.Text, color, 0)` |

修完**顺手全仓扫了一遍**同类形态（「原样放 markdown 文本」），确认已无遗漏。

### 行内代码补底色（对齐桌面）

用户指出桌面上反引号内容是「圆角矩形背景 + 换个颜色」。查了实际定义 —— Web 的 `.md-inline`：

```css
background: var(--panel2); border: 1px solid var(--border);
border-radius: 5px; padding: 0 5px; font-family: ui-monospace, …;
```

MAUI 的 `Span` / Avalonia 的 `Inline` **只能给方形底色、没有圆角**（不像 CSS 能给行内框
`border-radius`）⇒ 做到能力上限（底色 + 等宽），圆角那份差距是**平台限制**，已写进注释。
TUI 按约定不动。

### 判据

- 桌面自测 6096 → **6097**（0 红）；新钉两条：标题里的行内代码不出字面反引号、内容仍保留
  （TUI 是四端里**唯一能被桌面自测覆盖**的，所以判据钉在那里）
- `WayCoder.Maui` / `WayCoder.Gui` 编译 0 错误；真机复验见下

---

## v0.96.314 — Markdown：四端向共享 AST 收敛 ＋ 补 20 项 CommonMark

起因是「有些格式渲染不对」。查下来根因不是漏了某几个语法，而是**四套互不相干的实现**：
`UI/Shared/MarkdownRenderer.cs` 的块级 AST **只有 TUI 在用**；MAUI 只复用了它的**行内**那半；
GUI 零复用（自成一套）；Web 是独立的 JS。于是同一段 md 在终端、手机、浏览器里长得都不一样。

本版把三端 C# 渲染面**都接到共享 AST**，再在共享层一次性补语法 —— 补一个语法四端同时生效。

### 一、共享解析引擎补 16 项（`38cab848`）

| 语法 | 之前 |
|---|---|
| `#####`/`######` 标题、`## x ##` 关闭式井号 | 字面显示（只认到 `####`） |
| **Setext** `标题\n===` / `---` | **主动渲染错**：拆成「段落 + 分割线」 |
| `+` 列表 / `~~~` 围栏 / 4 空格缩进代码块 | 字面 |
| **嵌套引用 `>>`** | 渲染成 `│ > 内层`；现改成**容器块**（内部递归解析，可嵌代码块/列表） |
| 表格 `:--` `:-:` `--:` 对齐 | 忽略（只有 Web 支持） |
| `__加粗__` `_斜体_`（含「词内下划线不触发」） | 全不认 |
| `\*` 转义、`&amp;` `&#39;` 实体 | 字面 |
| `<https://x>` 自动链接 / 裸 URL | 字面 |
| `[文](url "标题")` | title 被当成 URL 的一部分显示 |
| ``` ``a`b`` ``` 反引号数量可变 | 不支持 |
| `![图](url)` | 漏出一个孤立 `!` |
| `~~删除~~` | 发成 `2`(淡化)，只是变淡 ⇒ 改发 `9`(真删除线) |

### 二、MAUI 聊天 + 预览接 AST（`06f7a8a3`）

聊天端此前**按行扫描、只认围栏与表格**，其余整段并进一个行内段 ⇒ 标题/列表/引用/分割线/
任务项**全部字面显示**（手机上一串 `- `，终端上却是真列表）。预览页是**另一套独立的行扫描器**，
段落只断「空行 / `|` / ``` / `#`」⇒ 紧跟段落的列表/引用/分割线全被吸进段落。

顺带删掉随之失效的 `MarkdownTable.cs` 与三个 helper（改完立即 grep 确认零引用）。

**真机验证**（v0.96.313，手机「文件 → README.md → 预览」）：H1/H2 按级别放大、真项目符号 `•`、
表格画出格线且**单元格内行内代码着色**、分隔行正确消失。

### 三、GUI 接 AST（`c65f4f1b` 块级 / `40ef8b83` 行内）

块级同因（段落吞块、标题只到 3 级、嵌套列表拍平、引用必须 `"> "` 带空格、表格单元格纯文本）。
行内是**自建的第四份口径**，`«»` 标签表最小 ⇒ `«fg:#rrggbb»` 真彩色与 `«strike»`
**字面泄漏**（TUI/MAUI/Web 都认十六进制，只有 GUI 不认）；`**`/`` ` `` 还是开关式的，
正文里落单的反引号被**直接吞掉**。现整段改走共享 `ParseInline`，颜色复用既有的
`SyntaxBrushMap.ForFg`；文件 308 → 69 行，`RenderTo`（本就零调用方）等死代码一并清掉。

### 四、Web 单独补齐（`b9b7b213`）

Web 是**唯一无法共享**的一端（JS 对 C#），缺口单独补、判据单独钉：

- `MARKUP_STYLES` 只有 11 个键，补 black/gray/purple/faint/i/u/bright/strike/s/blink/reverse/invert
  （⚠ blink/reverse **不能给空串** —— 查表用真值判断，空串会被当「未知标签」再泄漏一次）
- 行内代码反引号数量可变；链接允许 **title** 与**相对路径**；补 `***粗斜***` / `__` / `_` / `~~`
- **表格吞行**：`lines[i].includes('|')` 把表格后的含竖线正文吃进 tbody ⇒ 加空行守卫
- **实体二次转义**：改为**转义之前**解码（解码后必经 `escapeHtml`，故不引入 XSS）

### 五、真机复验修掉一处回归（`0891ec4a`）

用户真机报「表格的文字行变高了，文字靠上，好像每个格子都多了个空行」。
根因不在表格 —— 是 `RenderSegments` 的**块间换行**：重写时让每个块**自带尾换行**，
而这个方法**同时被当行内渲染器用**（`MarkdownPreview.AppendPlain` → `Convert` 渲染单元格），
于是每个格子末尾多一个 `\n`。原始实现在这里就是「只在行间插、末尾不插」，重写时漏掉了。

修法：抽出 `RenderBlocks`（只在块**之间**插换行），块自己不带尾换行；表格末行同理。
引用块那个 `TrimTrailingBar` 补丁随之**不再需要**（它当初就是为了擦掉末尾换行留下的悬空竖条）。

真机前后对比（同一文件、同一路径）：表格三行总高 ≈395 → **≈265** 屏幕像素，
摊到 3 行恰好是**每行少一个行高**；列表项之间的大空隙也一并消失。

### 判据

- 桌面自测 **6072 → 6095**（+23 条，全绿）
- `node scripts/_check_web_markdown.cjs` **19/19**（新增；沿用既有脚手架的 `slice()` 手法，
  把 app.js 里**真实的**函数抠出来跑 —— 浏览器里肉眼看不出「哪个标签泄漏了」）
- `WayCoder` / `WayCoder.Gui` / `WayCoder.Maui` 三工程编译 **0 错误**
- 真机：手机「文件 → .md → 预览」渲染正确

---

## v0.96.313 — 手机端 VML 物理剪枝 ＋ `check-vml-patches.sh` 判据换成分家后的模型

分家（v0.96.212）之后该收的两个尾：**产物层分离了、仓库层没有**，以及**常驻判据还停在分家前**。

### 一、`Lib/` 物理剪枝：2653 个文件 / 约 15 MB

「手机端专用」此前只落在**产物**上 —— `scripts/make-vml-lib.sh` 打包时用 `-x` 排掉 8 个
PC/DOS 模块，文件本身还躺在树里；而整个 `Lib/` 会被打进 `vml_lib.zip`**下发到手机解压**。
现在把手机端用不到的**文件**直接删掉，并落成一份可复核的声明清单 `third_party/vml/PRUNED.txt`：

| 组 | 内容 | 量 |
|---|---|---|
| ① | `Lib/*/Device/**`（MCU 与古董机的外设/寄存器定义：ATmega / STM32 / RP2040 / ESP32 / ZX Spectrum / NES / Sega / Apple II / IBM-PC…，由 `tools/GenDev` 生成）、`Lib/vml/{Device,Bios}/**`（23 个模拟 BIOS 的启动汇编） | 12.7 MB |
| ② | `Lib/*/ext/**` + `Lib/dynamic/**`（桌面 GPU/GUI 动态库绑定 imgui / opencv / opengl / skia，由 `tools/GenDyn` 生成） | 0.5 MB |
| ③ | `Lib/shared/backup/**`（陈旧副本）、PC VGA 字库、`Lib/pascal/vga.pas`、PC 专有头、宿主机构建脚本 | 2.2 MB |

`Lib/` **5201 → 2548 个文件**；`vml_lib.zip` **6.49 MB → 2.48 MB**（5282 → 2532 个条目）。

**剪枝前先证「零引用」**：保留模块对这四组没有一条 `.linked`、`vmltool.config.xml` 里没有、
`Lib/shared/src/*.c` 里没有 `#param`。**`Lib/c/vmlib.h` 有意保留** —— 它是
`Lib/c/vmdevice.h` 的 `#include` 目标，删了会留悬空 include。

⚠ **桌面端读的是同一份 `Lib/`**（不经过 zip→APK→解压那条链）⇒ 这些能力在桌面 VML 里
一并没有了。这符合「手机端专用」的定性；`git checkout` 随时能拿回来。已写进 FORK.md。

### 二、`GenLib -A`：签入的 `Lib/shared/*.vml` 出自旧版 GenLib

跑常驻判据时抓出来的：**82 个 `Lib/shared/*.vml` 与当前生成器的产出逐字节不同**，差异
**全部是新生成的多出 `; <行号>: <源码>` 注释**（`Lib/shared/printf.vml` 另有 5 行 `.linked`
指向 `uscanf/wprintf/wscanf/wchar/uchar`）。即签入那批是**更早版本的 GenLib** 产的。

按 CLAUDE.md ㉕ 早写下的「下一步」处理 —— 关键是**先 `touch Lib/shared/src/*.c`**：
`GenLib -b` 的增量判据是「`.vml` 比 `.c` 旧才重编」，而签入的 `.vml` 比 `.c` 新，直接跑会
**整批跳过、什么都不变**。touch 之后 **4702 行插入、0 行删除**，纯增量。

### 三、`check-vml-patches.sh` 判据重写

**它在改动前就是红的**：干净的 v0.96.312 上 **131 个 ✘、退出码 1** —— 长期当「绿的」看
是危险的（红得太久就没人看了）。拆开看，49 个来自**已退役的前提**：

| 判据 | 红 | 性质 |
|---|---|---|
| ① 七个目录 == vendor+补丁 | 2（`VMLAssembler` / `VMLPrepares`） | 分家后改了源码，按 FORK.md 不再补补丁 ⇒ 前提失效 |
| ②b 手工侧 == vendor+补丁 | 47（`util.vml`×22 / `syscall.vml`×22 / `modules.json` / `waycoder_ui.h`） | 同上 |
| ②a 生成物 == 重生成 | 82 | **真陈旧**（见上） |

① / ②b 验的是「rsync 会不会把我们的改动冲掉」，而 `sync.sh` 已随分家删除。现在换成三条
**分家后仍然有意义**的：

| 判据 | 验什么 | 失败意味着 |
|---|---|---|
| ① | `Lib/` 生成物 == 用本仓 GenLib 重生成的结果 | 改了源码/前端/生成器却没重生成 |
| ② | 剪枝清单 == 磁盘现实（两个方向） | 清单在说谎，或发生了**清单外的意外删除** |
| ③ | GenLib 不产出的手工文件已被 git 跟踪 | 新写了手工文件却忘了 `git add`（丢了没法重生成） |

**参考系从「vendor 提交」换成「当前 git 索引」** —— 分家后树本身就是真源。
（文件名里的 `patches` 已成历史遗留；保留旧名是为了不动 FORK.md / docs 里的一堆引用。）

### 判据

`scripts/check-vml-patches.sh` **全绿**（改动前 131 ✘）。桌面自测 **6072 / 6072**
（与改动前同数，含 `[VML 前端编译器清单]` 的上游漂移护栏 —— `VMLTool/` 未删，护栏仍生效）。

---

## v0.96.312 — 文字槽收渐变（C 层够得到）＋ 真机验证

v0.96.310/311 把文字渐变做进了引擎与两条离线后端，但 **VML 程序还够不到** ——
`VmlScene` 的文字槽对渐变刷子**退化成起始色**（`SolidTokenFor` 那句"该槽只支持纯色"）。

现在文字槽与填充/画笔一样收刷子，`ui_set_text_brush(gradient刷子)` + `ui_draw_text` 即可。
**按「兼容性优先」**，两处都是加法而不是换参数：

- `AddText` 加**重载**（`string colorToken` / 原来的 `uint color`）——
  `AddText(…, uint, …)` 是 `VmlScene` 的公开 API（`AddIcon` 在用），换成 `string` 是破坏性改动；
- `AddTextCurrent` 走 token；`TextBrushColor` 这个"只认纯色"的老读取口**保留**，
  拿到渐变时给它的起始色（向后兼容），**不再告警**（那句"该槽只支持纯色"已经不成立）；
- `SolidTokenFor` 随之**没有调用方**，删掉。

### 真机读数（`emulator-5554`，`draw_brush.c` 新增第 6 行）

| 格 | 偏红 | 偏蓝 |
|---|---|---|
| 列0 渐变文字 `WWWWWW`（红→蓝刷子） | 1043 | 1007 |
| 列1 对照 纯色 `RED` | 1070 | **0** |

对照组是关键：**纯色文字只可能有一个色相**，所以"两端都有"这条判据真的能分辨对错
（少了它，一条恒真的判据也能过）。这是第三次用"对照组 + 两端色相"这套判据
（渐变描边、渐变文字各一次），它比"看截图觉得像"可靠得多。

### 判据

桌面自测 **6074 → 6078（+4，0 红）**：文字槽端到端（刷子→DSL→解析回渐变）、
发射的是刷子引用而不是退化色、以及两条**兼容回归**
（没设文字刷子时仍跟随 `FontProperties` 的颜色；`AddText(uint)` 重载没被 string 版顶掉）。
真机 **2/2**。

---

## v0.96.311 — 文字渐变的 SVG 导出；并按「兼容性优先」改正两处形状

### SVG：文字渐变必须**单独发一份** def

形状用的是 `objectBoundingBox`（坐标相对**几何盒**归一化），而文字在 SVG 里那个盒
由渲染器**按字形墨迹**算 —— 与我们 `TextBlockBox`（行高 × 行数 + 最长行宽）必然不等，
PNG 与 SVG 的渐变位置会差一截。

所以文字那份改成 `gradientUnits="userSpaceOnUse"`，坐标按 `TextBlockBox` 算成绝对值，
**按图元各发一份**（两个文字图元盒不同 ⇒ 不能共用）。径向半径按 SVG 规范对
`objectBoundingBox` 的定义换算（`r` 是归一化对角线的比例 `sqrt(w²+h²)/√2`），
不换的话非正方盒上的圆会比形状那份扁。

- 文字宽度与盒收成 `DrawParse.MeasureLineWidth` / `TextLongestLine` / `TextBox` **一处** ——
  光栅采样器、SVG 定义、自测三处都用它（各算各的就是"文字盒三份实现"）。
- 形状那份 def **一个字没动**（仍是不带 `gradientUnits` 的 `objectBoundingBox`），
  有回归守卫钉着。

### 用户提的「优先考虑兼容性，其次考虑能否实现」—— 正好点中两处

**① `IVectorTarget.FillShape` 加可选参数 = 对实现方是破坏性改动。**
接口方法加参数，**所有**实现方（含本仓之外的插件）都必须跟着改 —— 而"能画渐变描边"
这件事只有实现方在意。改成**新增一个带默认实现的重载**：

```csharp
void FillShape(subpaths, fill, gradient, evenOdd, box) => FillShape(subpaths, fill, gradient, evenOdd);
```

不关心这个矩形的实现方**一个字都不用改**，最坏也只是渐变位置退化成原样。
同样的形状也用在 `Render` / `DrawString` / `FillGlyphAa` / `Canvas.DrawText` 上
（加的是**可选参数**，调用方无感）。

**② 文字专用 id 必须避开用户已用的渐变名。**
DSL 里 `gradient tg0 linear …` 是合法的，撞上就是**一个 SVG 里两个同名 id**、
`url(#tg0)` 指哪个由渲染器说了算 —— 这类"平时没事、别人起个名就坏"的隐患不值得留。
现在按 `doc.Gradients` 里已有的名字避让，并有断言钉住
（用户占了 `tg0` ⇒ 文字那份排到 `tg1` 且真的引用 `tg1`）。

> 取舍说明：`ClipId`（image 的裁剪 id）那边**是老代码、同样的隐患**，这次**没动** ——
> 它不影响正确性（只是理论上的命名冲突），而改它会动到已验证过的 image 路径。
> 按"兼容性优先"记在这里，不顺手改。

### 判据

桌面自测 **6066 → 6074（+8，0 红）**：userSpaceOnUse、文字 fill 引用专用 id、
x1/x2 落在文字盒左右缘、两个图元各拿一个 id 且跨度不同、id 避让、
形状那份不带 `gradientUnits` 的回归守卫。

⚠ **这一轮我自己写错了三条断言**（都是当场发现并改对的，但值得记一笔）：
① `Matches(...).Count == 2` 只数了个数、根本没比较两个值是否不同；
② `!Contains("tg1")` 与前半句自相矛盾（`tg1` 正是避让后该用的那个）；
③ 更早那条"5×7 回退"根本没走到 5×7（像素数与 TrueType 一模一样才露的馅）。
**断言写错和实现写错一样会让人以为验过了** —— 判据要么能分辨对错，要么别写。

### 还没做

- **真机验证**（文字渐变在手机上靠矢量回退到光栅；v0.96.308 起回退会重出这一帧，
  但这一版还没上模拟器）。
- 5×7 点阵那条路仍无自测覆盖。

---

## v0.96.310 — 文字渐变（光栅那条路）

用户提的「文字也可以使用渐变，可以画文字渐变测试」。

### 落在哪一层

**字形填充是扫描线，落笔点只有一处**（`TrueTypeFont.FillGlyphAa` 里那句 `BlendPixel`）
—— 所以不必给光栅器再写一套，把"按坐标问颜色"接进去就够了：
`Render` / `DrawString` / `FillGlyphAa` 各加一个可选的 `sample` 委托，
`Canvas.DrawText`（5×7 那条回退路）同样加一个。旧调用方一个字不用改。

**归一化按整个文本块，不是逐个字形** —— 逐字形会让每个字都自己红→蓝，一眼看去是"花的"。
盒由 `DrawGeo.TextBlockBox` 给（**唯一真源**，SVG 那一半将来要用同一个盒）。

⚠ **这是第二套几何真源**（第一套是各 `DrawGeo.*` 的形状点集）。之所以必须另立一个：
文字没有"点集"，它的范围是"行高 × 行数 + 最长行宽"，得按字体度量算。

### 矢量后端：如实标记画不了

平台的文字 API（`ICanvas.DrawString`）**只吃一个纯色**，没有"字形 → 路径"的入口。
渐变描边当初看着也"做不了"，但描边本质是填充多边形、能轮廓化；**文字不行** ——
要拿到字形轮廓得走平台专有 API，正是这个后端刻意避开的那类东西。

所以 `DrawVector.Text` 遇到渐变就 `MarkUnsupported("text-gradient")` ⇒ 宿主整窗回退光栅，
由光栅那条路画（它现在支持）。v0.96.308 起**回退会重出这一帧**，所以静态程序也不会停在残缺帧上。

### 判据（桌面自测 6057 → 6066，+9，0 红）

判据是「**红像素和蓝像素都存在**」，因为这条路径有两种失败形态且都不报错：
① 渐变没接上 ⇒ 回退成 `f.Fill`（黑）；② 盒算错 ⇒ 整行落在渐变的一端、全是同色。
外加**对照组**：纯色文字必须"只有红、没有蓝"——少了它，一条恒真的判据也能过。

另加 `TextBlockBox` 的三条（middle 退半宽 / end 退全宽 / 三行高度）与矢量回退的**反证**
（纯色文字不该被误伤，否则所有文字都会把整窗拖回光栅）。

### ⚠ 一条**测不到**的东西，写在这里免得被读成"验过了"

自测里那条"未知字体族名"的断言**测的不是 5×7 点阵那条路**。
露馅的是**像素数与 TrueType 那条一模一样**（690/680 对 690/680）——
`TrueTypeFont.Resolve` 对不认识的族名有一个"候选里随便挑一个能加载的"兜底，
所以 `no-such-font-xyz` 照样拿到真字体。5×7 只在"一个字体都加载不出来"时才走，
DSL 层造不出那个环境 ⇒ **那条路目前没有自测覆盖**（代码支持，没验过）。

### 还没做的

- **SVG 导出**：文字渐变在 SVG 里仍是纯色。它必须配 `gradientUnits="userSpaceOnUse"`
  —— 形状用的 `objectBoundingBox` 在文字上由渲染器按**字形墨迹**算盒，
  与我们这个"行高 × 行数 + 最长行宽"必然不等，PNG 与 SVG 的渐变位置会差一截。
  而且不能与形状共用同一份 `<defs>`（单位不同）⇒ 要为文字单独发一份。
- **真机验证**：这一版只过了桌面自测（6066/0）与两端编译，**还没上模拟器**。

---

## v0.96.309 — 渐变描边的矢量后端：**轮廓化填掉，不再回退光栅**

v0.96.306 那一版把渐变描边在矢量后端**如实标记"画不了"**，靠整窗回退光栅兜底
（v0.96.308 又修了"回退不重出这一帧"）。画面是对的，但**代价是整窗掉出 GPU 路径** ——
手机上那正是 v0.96.180 花了力气才拿到的 92~107fps。

其实**做得到**：平台画布没有 `SetStrokePaint`（只有 `SetFillPaint`），
但描边本质上就是一个**填充多边形** —— 把折线展成一组多边形再填即可。

### 几何抽成唯一真源：`DrawGeo.StrokePieces`

"线宽 w 的描边 = 一组粗线四边形（+ square 端帽的外延 + round 端帽的圆）"
这套分解从前**只长在光栅路径里**（`StrokePolylineBrushed` 的局部函数）。
矢量要用就得再写一遍 —— 那正是本仓头号坑「同一规则两处实现」，
症状是"同一份 DSL，导出 PNG 与手机上看到的不一样"，而且只测一条后端照不出来。

现在两条后端从 `DrawGeo.StrokePieces` 拿同一份。**光栅那条是纯粹的搬移**：
改完自测 6053/0 一条不变，而渐变描边那几条断言本来就是逐像素卡的
（左端必须是**纯**起始色、中点必须是插值、内部必须是填充色）——
它们没红，就说明搬移是逐像素等价的。

### 矢量侧两条硬约束

**① 刷子矩形必须传原几何的盒**（`DrawVector.BoundsOf`）。
`FillShape` 默认取"这组子路径自己的外接矩形"，而描边轮廓比几何**胖出 `width/2`** ——
拿轮廓盒归一化，渐变会整体偏半个线宽，**肉眼看不出来**。
为此给 `IVectorTarget.FillShape` 加了可选的 `box`（填充传 null，那时两者本来就相等）。
语义与 SVG 的 `objectBoundingBox` 一致：它取的也是**几何**的盒，不含描边。

**② 必须非零环绕**。这些块之间是**并集**（接头处、圆帽与杆之间都重叠），
奇偶规则会把每一处重叠都挖成洞。能这么写是因为 `StrokePieces` 产出的每块**绕向一致** ——
自测里逐块算鞋带和钉住了这一条（`各块绕向一致`）；绕向一乱，症状是接头处出现黑洞。

**一次 `FillShape` 装下全部子路径**，不是逐块调：一条 64 段的椭圆描边加上圆帽能到两百块，
逐块调就是每帧两百次建路径。

### 真机读数（`emulator-5554`，逐像素）

与 v0.96.308 那版（光栅回退）**逐点一致**，说明两条后端画的是同一个东西：

| 采样 | 矢量轮廓化 | 光栅回退版 |
|---|---|---|
| 矩形左边框 | `(230,45,35)` | `(230,44,35)` |
| 矩形右边框 | `(32,74,233)` | `(30,73,234)` |
| 矩形上/下边框 | `(130,59,135)` | `(130,59,134)` |
| 矩形内部 | `(56,102,167)` | `(56,102,167)` |

圆环径向扫描：`d=105..116` 全是实心描边、`117` 是抗锯齿边、`118` 起才是底色 ——
**接缝处没有洞**（这就是非零环绕那条判据在真机上的落点）。

### 退化盒的边界（已知，只影响轴对齐直线上的**径向**渐变）

水平线的几何盒**高度恒为 0**、竖直线宽度恒为 0。光栅侧有 `spanX <= 0 ? 0 : …` 的守卫
（退化成沿该轴不变化），平台侧由 `SetFillPaint(paint, rect)` 自己映射 ——
两边在**退化维**上的行为不保证一致。线性渐变不受影响（`draw_brush.c` 第 5 行的直线
就是水平线，几何盒高度 0，真机读数正确）；**径向渐变铺在轴对齐直线上**是唯一可能分叉的组合，
目前没有用例，也没有花力气去对齐 —— 记在这里，别当成已验证。

### 判据

桌面自测 **6053 → 6057（+4，0 红）**：矢量走的是填充而不是回退、
刷子矩形是原几何盒（`y 10..10` 而不是轮廓的 `7..13`）、各块绕向一致、round 端帽进了同一批子路径；
外加**反证**：纯色描边仍走 `StrokePolyline`（不误入轮廓化路径）。

---

## v0.96.308 — 真机验出来的：**矢量后端回退只翻标志位 ⇒ 静态程序永远停在残缺帧上**

上一条（v0.96.306）说渐变描边"矢量后端这一版如实标记画不了 ⇒ 整窗回退光栅，
**画面完全正确，只是慢**"。**那句话是错的** —— 真机上整片消失。

### 现象（`Examples/c/draw_brush.c` 第 5 行三格，模拟器逐像素）

| 格 | 期望 | 真机实测 |
|---|---|---|
| 圆（**只有**渐变描边、不填充） | 红→蓝圆环 | **整格空白** |
| 矩形（填充纯色 + 边框渐变） | 蓝底 + 红蓝边框 | **只剩蓝底，没有边框** |
| 直线（渐变描边） | 红→蓝 | **不见** |
| 同帧的椭圆**填充**渐变（对照组） | 红→蓝 | ✔ 正常 |

"矢量支持的都在、矢量不支持的整片没有" —— 每一处观察都指向同一个方向。

### 根因：回退只影响**下一帧**，而静态程序没有下一帧

```csharp
_canvas.UseVector = false;   // 从前这里写着「当前这帧已经画好了，不重绘（免得闪成空白）」
```

那句话对**逐帧重画的游戏**成立（下一拍自然补上），对**画完一帧就 `ui_wait()` 挂着的程序**
是错的 —— 屏上会**永久**停在"矢量画不出来的那一块是空的"。

改法：回退时**强制重出这一帧**。两处配套，缺一不可：

1. `_forceRasterRender` 让渲染那一拍**跳过版本守卫** —— 回退是"同一份内容换条路再画一遍"，
   版本号当然没变，按版本判会直接返回；
2. 重出时**现拍 `BuildDsl()`**，不能走 `TakePresentedDsl()` —— 那份快照上一拍已经被取走
   （`Take` 会清），再取是 `null`，整帧从 `if (dsl == null) return;` 溜掉。

不闪空白：光栅那条路是**后台算好再换**（旧帧一直贴在屏上），
这里只是让**正确的帧**替换掉**残缺的帧**。

### 修好之后的真机读数（`emulator-5554`，逐像素）

| 采样 | 实测 |
|---|---|
| 圆环左 / 中 / 右 | `(224,32,32)` / `(128,32,96)` / `(32,64,224)` |
| 矩形左边框 / 右边框 | `(230,44,35)` / `(30,73,234)` |
| 矩形**上/下**边框 | `(130,59,134)` —— 红蓝**中点**（渐变沿 x，所以上下边框在中点呈紫） |
| 矩形内部 | `(56,102,167)` ≈ `#3C6EB4`（= 填充色 C，**没被描边吃掉**） |

「边框一个渐变、填充一个渐变」在真机上成立。

### 顺带两条装置/脚本的坑（都不是产品问题，但会让"验过了"变成假的）

- **`driver.py` 的键码表没有 `_`** —— 安卓没有下划线键（它是 shift+减号）。
  从前那里直接 `raise ValueError`（响，好过静默丢字符），但后果是
  **带下划线的例子一条都跑不了**，而语料里到处是下划线（`draw_brush.c` / `file_io.c`…）。
  实测表现是"命令没送进输入框"，看不出是键码表的缺口。
  现在：键码送得进就走键码，**送不进就退回 `input text`**（空格写 `%s`、整条套单引号
  交给设备 shell 剥）。退路安全的前提是 `submit` 每条都**回读校验**。
- **`window_open()` 对 `VML_WIN_NO_GAMEPAD` 的程序永远返回 false** ——
  它的判据是绘图页那几个手柄按钮的文字（SELECT/START/收起手柄），
  而声明了「不要手柄区」的程序一个都没有。于是**已经跑起来的程序被记成"窗口没开"**。
  （仓库里记过这条；这次又踩到，因为 `draw_brush.c` 正是 NO_GAMEPAD。）

### 判据

桌面自测 **6053/0**（不变 —— 这条缺陷在 MAUI 页面里，桌面自测碰不到，
**这正是它必须上真机才能发现的原因**）。
真机：模拟器 `emulator-5554` 跑 `vml run examples/c/draw_brush.c`，上表逐像素读数。

---

## v0.96.307 — 补出 vml_lib.zip：v0.96.305 改了头文件却没重出包

`v0.96.305`（画笔箭头）给 `Lib/c/waycoder_ui.h` 加了 `VML_ARROW_*` 三个宏，
但**没有重跑 `make-vml-lib.sh`** —— 包最后停在 `v0.96.303`。

后果不是「少个宏」那么轻：手机上的 C 程序是**对着包里解出来的头文件**编译的，
所以任何写 `VML_ARROW_NONE` 的程序在真机上**根本编不过**，而桌面上一切正常
（桌面不读这个包，直接读 `third_party/vml/Lib/`）。

本刀给 `draw_brush.c` 新增的渐变画笔格正好用了 `VML_ARROW_NONE` —— 算是那条
`make-vml-lib.sh` 陈旧警告（v0.96.299 加的）第一次真实命中。

同批给 `draw_brush.c` 补**第 5 行：渐变画笔**（网格从 4 行改 5 行），
并把 `build-apk.sh` 里「脚本目录解析两次」的坑修掉（从仓库根敲
`bash WayCoder.Maui/build-apk.sh` 会在第 39 行炸，而报错看不出是路径解析的问题）。

---

## v0.96.306 — 描边（画笔）渐变：边框一个渐变、填充一个渐变

用户提的「线条是画笔，画笔是刷子和线条宽度……**边框一个渐变，填充一个渐变**」。
上一批做完了填充刷子，这一批把**描边**也变成刷子。

### 改动落在三层

**① 解析**（`DrawCommands.cs`）

| 写法 | 从前 | 现在 |
|---|---|---|
| `rect … @g` | 填充渐变 | **不变**（兼容红线） |
| `rect … @a @b 3` | 第二个 `@` **覆盖**第一个（= 被静默丢弃） | 第二个 = **描边**刷子 |
| `line/polyline/arrow … @g` | 整个 token **被丢弃** | = **描边**刷子（这几条只有一个颜色位） |
| `path … @g` | 填充渐变 | **不变**（`path` 的老特例，也是红线） |
| `path … stroke @g` | `stroke` 被丢弃、`@g` 当填充 | `stroke` 是关键字，**描边**刷子 |

四条新槽位占的全是「今天被静默丢弃的 token 形态」，老程序的解析结果**逐字段不变**。

**② 光栅**（`DrawCanvas.cs` 新增 `StrokePolylineBrushed`）

与既有的 `StrokePolyline`/`DrawLine` **逐段同构** —— 同一套粗线四边形、同一套线帽规则
（butt 直切 / square 外延 / round 两端补圆），连"段间不做圆角接合"这个既有行为都照搬，
唯一差别是每像素的颜色按**几何局部坐标**从刷子里取。

**没有**先拼一个整轮廓多边形再填：拼轮廓在凹多边形（星形）上会自交，
奇偶规则填出来的接合处会**漏洞**；"逐段四边形取并集"正是既有实现的定义，照搬它天然等价。

> **归一化盒这一处最容易错**：扫描范围是"被线宽撑大的轮廓盒"，而渐变归一化必须用
> **原几何**的盒 —— 拿轮廓盒去归一化，渐变会整体偏半个线宽，**肉眼看不出来**。
> 为此给 `FillTransformed` 加了可选的 `norm` 参数（默认 null = 两者相同，既有调用点零影响）。
> 自测的判据卡在"起点必须是**纯**起始色"（R≥248）而不是"偏红" ——
> 盒错了正好会得到 R≈235，写成"偏红"两边都过，等于没测。

虚线也顺手修了：从前 `DrawLineDashed` 拿到的是**世界**坐标，缩放/旋转会改变虚线节奏；
渐变这条路在**局部**空间切段。

**③ SVG**：四条只描边的指令（line / arrow / polyline / path）从前各自硬写
`ColorUtil.ToHex(f.Stroke)` —— 给它们配渐变描边会**光栅对、SVG 画成黑的**。
抽出 `DrawParse.StrokeAttr` 一处供四个 `EmitSvg` 共用（本仓头号坑形态，改一处漏三处）。

### 矢量后端这一版**如实标记"画不了"**

平台画布没有 `SetStrokePaint`（只有 `SetFillPaint`）。但描边本质上是个填充多边形，
所以它**做得到** —— 只是要先按 `DrawGeo` 那套把折线外扩成轮廓再填，这一刀还没做。
现在 `DrawVector.Stroke` 遇到渐变描边就 `MarkUnsupported("stroke-gradient")`，
宿主据此把**整个窗口**回退到光栅后端：**画面完全正确，只是慢**。
宁可慢，也不能默默画成黑色的实心块。

### 画笔槽也收渐变了

`VmlScene.SetStyle(Pen, …)` 之前对渐变刷子**退化成起始色并告警**，现在直接用刷子 token。
配套删掉了 `_penFallback` 这个只剩补位的字段。

⚠ 这同时意味着 **`Lib/` 侧不用改** —— `ui_set_pen` 收的是句柄，
`ui_brush_linear` 造的句柄一直能传进画笔槽，从前只是被退化掉。

### 判据

桌面自测 **6029 → 6053（+24，0 红）**。其中最要紧的四条：

- `起点是纯色 ⇒ 归一化盒用的是几何盒而非轮廓盒`（上面那条）；
- `SVG: 描边渐变不漏进填充槽`；
- `矢量: 渐变描边 → MarkUnsupported` **+ 反证纯色描边不误触发整窗回退**
  （少了反证，一条"所有描边都回退"的错实现也能过）；
- `path 兼容：裸 @id 仍是填充（红线）`。

### 顺手修掉一条**死分支**（本刀写测试时撞出来的）

`AddShape(VmlShape.Rect, …)` 的分流判据写的是 `a4 > 0`（**高 > 0**，恒真）
⇒ `rect` 那条分支**永远走不到**，`ui_draw_rect` 一律发 `roundrect`。
判据本意是**半径 > 0**（`a5`）。

改掉之后半径 0 走 `rect`、半径 > 0 走 `roundrect`，与两条 DSL 指令各自的
`ParseStyle` 起点（4 / 5）也就对上了。**几何上两者等价**（不是"改了个颜色"那种）：
`DrawGeo.RoundRect` 把 `r` 钳到 ≥0，r=0 时退化成"四个角点各重复 13 次"的退化多边形，
四条实边正是矩形四边 —— 扫描线判据对左右两边各交叉一次、偶数规则判定正确。
自测补了**两条分支各一条**断言（只测一条的话，改回死代码也照样绿）。

⚠ **写这批断言时有两条是我自己期望值写错了**（不是实现错），都已修正并就地写明原因
（同一个坑当天踩了两次 —— `FirstOrDefault` 拿到 null 就当成“实现错”，其实是**判据**写错了）：
`DrawRunner.Parse` 的渐变解析循环是 `if (doc.Gradients.Count > 0)` **整段判**的 ——
一条渐变都没定义时它整个跳过，那时 `GradientRef` 会原样留着（既有行为）；
以及裸 `rect` 的默认填充是**不透明黑** `#FF000000` 而不是透明。

---

## v0.96.305 — 画笔的箭头落地（末端箭头）

`ui_set_pen` 的第 5 个参数（箭头）之前是"**收了不用**" —— 现在接上了：
画笔带箭头时，`ui_draw_line` 改发 DSL 的 **`arrow`** 指令（主干 + 两条箭头边），
几何在 `ArrowCommand.Head` 里、三条后端共用，不必新写一份。

**这是一次「一个绘制调用两种形态」**：`arrow` 在 DSL 里是**另一条指令**，
所以选指令名这件事放在 `VmlScene` 里（一行三元），而不是让 `line` 自己长出箭头参数。

⚠ **本批只做末端箭头，且只对直线生效**：
- `VML_ARROW_START` / `BOTH` 现在与 `END` **同义** —— `arrow` 的几何就是"从起点指向终点"，
  起端/两端要让它支持反向（三条后端都要动）；
- **折线与路径的箭头还没做**（各自的几何支持）。

新增 `VmlArrow` / `VmlCap` 两个跨语言契约常量类（与 C 头文件的 `VML_ARROW_*` /
`VML_CAP_*` 一一对应），并补两条断言钉住"带箭头发 arrow、不带发 line"。

判据：桌面自测 **6029/0**（+2）。

---

## v0.96.304 — 刷子模型的**参数防护自测**：把"最多画不出来，绝不崩"钉住

用户提的「运行时要有参数检查和防护，防止被搞死」。新的三个号是"一个号 + 操作码"，
程序传进来的**种类 / 槽位 / 形状码全是裸整数**，写错一个就是未定义行为 ——
而防护代码写的时候**没有自测钉住**（只有代码，没有判据）。

补了 18 条断言（桌面自测 6009 → **6027**），只断言两类，因为这两类才是"崩"的来源：

| 类别 | 断言 |
|---|---|
| **不抛** | 未知/负形状码 → false；未知样式槽 → false；越界句柄 → null（绝不能拿它去索引刷子表） |
| **越界真的被挡住** | 坐标超 ±100 万 → 丢弃；星形角数钳 `[2,4096]`；正多边形边数钳 `≥3`；旋转角归一到 `[0,360)`；刷子表满 128 后返回 0 |
| **不静默** | 渐变刷子给画笔槽 → 受理但退化成起始色（`#FFFF0000`），不是悄悄画错 |
| **不误伤** | 同色刷子复用同一句柄（循环里反复造不撑爆表） |

第 2 类刻意写死："没崩就算过"是自测里最坏的一种 —— 静默画错也是错。

判据：桌面自测 **6027/0**。

---

## v0.96.303 — 刷子模型的模拟器全量体检（`Examples/c/draw_brush.c`）

给刷子模型配一个端到端例子：**样式成了状态之后，出错的形态也变了** ——
老接口是"一条调用带齐颜色与开关"，新接口是"先设刷子、再画形状，绘制调用不带颜色"。
所以「状态没设上」「设错了槽」「切形状时状态丢了」这三种都表现为
"画出来了，但不是你要的颜色"，只看代码看不出来。

12 格逐格取格心像素，模拟器实测：

```
0 矩形   #3C6EB4 ✔   1 圆角矩形 #3C6EB4 ✔   2 圆     #3C6EB4 ✔
3 椭圆   #3C6EB4 ✔   4 多边形   #3C6EB4 ✔   6 星形   #3C6EB4 ✔
7 正多边形 #3C6EB4 ✔  10 心形    #3C6EB4 ✔
8 圆环：环上 #3C6EB4，**中心 #000000**（奇偶规则挖洞，中间必须是空的）✔
11 椭圆渐变：左 #F2322D（红）→ 右 #334DEC（蓝）—— 是真的渐变，不是纯色 ✔
```

### 又一个"被版本升级冲掉"的坑（这次是我自己踩的）

第一次跑报「找不到文件：drawbrush.c」—— 因为**升版本会让 `EnsureExamples()`
重新解压 `examples/`**，而重解压会清掉 `c/` 目录，把我刚 `adb push` 进去的副本也清掉。
顺序必须是：**先让应用跑一次（完成重解压）→ 再 push → 再运行**。
（这与 `CLAUDE.md` 里记的「改 `Examples/` 只要不升版本就不会重解压」是同一件事的两面。）

判据：`vmlcli` 编译 40202 条指令；模拟器 12 格逐字节对上。

---

## v0.96.302 — 绘图接口收成「一个号 + 操作码」：所有形状、所有样式都走刷子

用户的两条指正决定了这一版的形态：

> 「不一定需要增加很多 syscall 的 id，因为**全部被库封装了，一个 id 也能封装多个版本**」
> 「基本上库兼容就够了，库如果实在无法兼容的，就改游戏源码来支持库」

### 为什么不再"一个功能一个号"

VML 程序**不直接调 syscall** —— 它调 `Lib/` 里的 `ui_*` 包装（22 门语言各有 GenLib
生成的绑定）。**号是给库用的，不是给程序作者用的**，所以一个号完全可以靠"操作码"
承担多个版本。于是原方案里那 20 个号收成了 **3 个**：

| 号 | 名称 | 入参 |
|---|---|---|
| **574** | `DRAW_SHAPE` | R0=形状码（14 个）R1..R7=七个通用槽 |
| **575** | `BRUSH` | R0=种类（纯色/线性/径向/按名字）R1..R6=参数 → 句柄 |
| **576** | `SET_STYLE` | R0=槽位（填充/画笔/文字）R1=刷子或颜色 R2..R5=线宽/线帽/虚线/箭头 |

好处是实打实的：**加形状再也不用占号**（加形状码 + 一行 C 包装）；**参数个数可控**
（通用槽固定 7 个，绕开"星形要 9 个参数"这个全仓没有先例的形态 —— 现有内联汇编
最多 8 个实参）；宿主侧只有**一个** switch 分支。
代价：形状码成了跨语言契约，**发布后只能末尾追加**。

### 一次补齐了「形状 / 样式」两边的缺口

- **椭圆渐变**（`VML_SHAPE_ELLIPSE_GRAD`）：534–539 那批是**一个个补上去的**而不是设计出来的 ——
  矩形和圆各有 `ui_*_grad`、多边形/路径靠"多一个 grad 参数"，**椭圆从头到尾没有入口**
  （宿主 `AddEllipse` 反倒一直支持 `fillGradient`）。手机端跑渐变普查时才发现的。
- **星形 / 正多边形 / 圆环 / 扇形 / 心形**：DSL 与引擎里**早就有**（`DrawGeo.*`），
  但 C 侧完全够不到。现在各是一个形状码 + 一个 `ui_draw_*`。
- **样式三槽**：填充 / 画笔（刷子+线宽+线帽+虚线+箭头）/ 文字。
  「颜色 = 只有一个色标的刷子」—— 所以 `ui_set_fill(0xFF2A3346)` **直接传颜色也合法**
  （句柄是小整数 1..128，颜色 ≥ 0x01000000，两者值域不重叠，不需要魔法位）。

### 兼容

**旧 `ui_*` 的号、入参、语义一个都没动**；旧程序不改一个字照跑。
新的 3 个号是纯新增；`DRAW_SHAPE` 的样式来自 `SET_STYLE`，与老调用各走各的。

### ⚠ 本批还没做的（宿主会**记一次警告**再退化，不静默）

**画笔与文字刷子只支持纯色** —— 渐变描边 / 渐变文字要等引擎侧做出来（DSL 与三条后端都要动：
光栅要能按像素采样描边、矢量后端要把描边轮廓化、SVG 要放开 `FillStrokeAttrs` 的闸）。
给渐变句柄时退回该渐变的起始色并记警告。

### 判据

桌面自测 **6009/0**；`GenLib -b` 报 `vmlui.c -> vmlui.vml ... OK`；
`python3 scripts/make-ui-help.py` 生成 12 页 / **80 个接口**（硬闸：不补说明就报错退出）。

---

## v0.96.301 — `Lib/shared/src/vmlui.c` 其实**编译不过**，被增量构建掩盖了

### 怎么发现的

给 `vmlui.c` 加一个接口，改动了它的 mtime ⇒ `GenLib -b`（**增量**：`.vml` 比 `.c` 新就跳过）
这次没跳，于是打出：

```
vmlui.c -> vmlui.vml ... 失败: <input>:331:5: error: 未声明的变量 'MASK_AT' [CodeGen_UndefinedVariable]
完成: 0 编译, 105 跳过
```

**`MASK_AT` 全仓只被使用、从来没有定义过**（`git log -S "define MASK_AT"` 一次都没命中）。

### 它以前是怎么"跑"起来的

前端对未声明标识符**静默按 0 算**（这正是后来 P6 加未定义变量检查要治的病），
于是 `ui_gset(MASK_AT + 0, …)` 等价于写第 0 格 —— 7 种方块的掩码落在 **0..6**，
**正好压在前 7 个棋盘格上**：棋盘写 0..6 就把掩码冲掉，之后 `ui_piece_cell` 返回垃圾。

### 应该是什么值：**200**

两条独立证据：

- `vmlui.c` 自己的注释写着「256 个 int 够放 10×20 的棋盘（**200**）+ 7 种方块的 4×4 位掩码（7）」；
- `Examples/python/tetris.py` 里写死了 `BOARD_AT = 0` / **`MASK_AT = 200`** —— 那是**跨语言约定**，
  两边必须对得上（Python 那门自己写掩码，C 这门走 `ui_piece_init`）。

### 修法

`#define MASK_AT 200`（附上这段来历）。重编后 `ui_piece_init` 生成
`move R0 #200` —— 掩码从"压着棋盘"变成"接在棋盘后面"。

⚠ **这是行为变更**：任何"掩码被棋盘写坏"的程序从此正常（`ui_piece_cell` 的早期格子）。
C 的 `Examples/c/tetris.c` 实测照常编译、链接、运行（44 172 条指令，与改前一致）。

### 教训

**增量构建 + 静默错误 = 两层掩盖**：源码早就编译不过，而"没改它就不重编"让这件事
一直看不见。同类信号还有一处 —— `scripts/make-vml-lib.sh` 末尾那条"签入的 zip 与仓库
`Lib/` 不一致"的告警，这次也是它先响的（v0.96.296 加的那条）。

判据：`GenLib -b` 报 `vmlui.c -> vmlui.vml ... OK`；`Examples/c/tetris.c` 在 `vmlcli` 上照跑。

---

### ① 三条指令各写了一份逐字相同的样式循环

`line` / `arrow` / `polyline` 解析尾部样式段（线帽 / 虚线 / 颜色 / 线宽）的那 8 行，
**三份逐字相同** —— 本仓头号坑「同一规则四处实现」的标准形态。已收口成
`DrawParse.ParseStrokeStyle(a, start, f)` 一处。

不改语义只改形状的活，最容易"顺手"改出问题，所以配了一道**兼容性主闸**：
把每条指令解析出来的 `Stroke / StrokeWidth / LineCap / Dashed / Fill / GradientRef /
FontFamily / Args` 全部打成一条字符串钉住（10 条断言）。

**并且用「改动前的实现」反证过这道闸真的会响**：把 `DrawCommands.cs` 临时还原到改动前再跑 ——

```
line 默认 ✅  line 颜色+线宽 ✅  line 线帽+虚线 ✅  arrow 颜色 ✅
polyline 颜色+线宽+线帽 ✅  rect 双色 ✅  rect 渐变填充 ✅  text 常规 ✅
text @id → 渐变引用 ❌      text @id 不再污染字体族 ❌
通过: 6007  失败: 2
```

**8 条兼容断言在旧实现上全过**（证明收口确实零变化），**2 条新断言在旧实现上失败**
（证明它们不是空断言）。恢复改动后 6009/0。

### ② `text 10 20 "hi" 24 @g1` 里的 `@g1` 被当成了**字体族名**

`TextCommand.Parse` 的兜底是 `f.FontFamily = s; // 其余裸词视为字体族名` ——
而 `@g1` 既不是颜色也不是数字、不是锚点也不是粗斜体，正好落进兜底。后果有两层：

1. 渐变当然没生效（填充保持默认黑）；
2. **更直接的**：`FontFamily` 被污染成 `@g1`，`TrueTypeFont.Resolve("@g1")` 找不到
   就**静默回退到 5×7 点阵字体** —— 写这行的人会看到文字突然变成另一种字体，
   而完全想不到是那个 `@id` 干的。

已加一支 `@` 判断，排在兜底**之前**。与 `polyline` 的 `@id` 被丢是同一族问题（静默丢失）。

### 还没做的

- **`polyline` 的 `grad` 参数仍然是死的**。它语义上就是"描边刷子"，而描边刷子要等
  引擎侧的可采样描边落地（`DrawFigure.StrokeGradientRef` + 光栅/矢量两条落笔路径）——
  **解析了却没人用**等于把一个半成品塞进代码，所以刻意与渲染同批做，不在这里先占位。
- `text` 的渐变引用现在**能解析进字段**了，但文字渲染还没接（同批做）。

判据：桌面自测 **6009/0**（新增 10 条；并用改动前实现反证过其中 8 条不变、2 条真修复）。

---

### 为什么先做这一条

要给绘图接口加一批新号（`docs/VML宿主接口.md` 的刷子模型，574–599）。
动工前先把**号本身**的护栏立起来 —— 因为抢号的症状不是报错：

```
switch (n) { case 573: …; case 573: …; }   // 后写的那个是永远不可达的代码，只给一条 CS0162 警告
```

先写的赢，后加的**静默变成另一个功能的语义**。而 syscall 号是**在 `VmlUi` 里一个个
`public const int Xxx = 5xx;` 加上去的**，AOT 禁反射 ⇒ 运行时枚举不出来 ⇒
没有清单就只能靠人眼比对。

### 做了什么

- **新增 `VmlUi.AllNumbers`**（47 个号，只给自测查重用，无运行时消费方）。
  ⚠ 新增一个号必须同时加进这张清单 —— 忘了不会让程序出错，只会让这道网漏掉新号，注释里写明了。
- **新增 `SelfTest.Chunk26.cs`**：清单非空 / **无重复** / 都在 500–599（`Handles` 认领）/
  都在 `ReservedRange` 里（否则运行时会以「权限不足」拒掉），外加一条**反证**
  （往清单里塞一个重复号，断言查重抓得到 —— 「不响的自测比没有更糟」）。
- **删掉 4 条手写的号段比对**（`CallJson ≠ MsgWaitEx ≠ WinOpenEx ≠ ScrOrient` 这类，
  散在 `SelfTest.Chunk10.cs` 里）。它们正是本仓头号坑的形态：**平行表** ——
  每加一个号就手抄一遍比对名单，没被抄进去的那几个永远查不到。现在一次性查全部号。

### 顺带清掉一张已经过期的规划表

`docs/VML宿主接口.md` §3 原本写着 `570 SENSOR_READ` / `571 CLIPBOARD_SET` /
`572 SHARE_TEXT` / `573 TOAST` / `574 FRAME_TIME`，而 **570–573 早已被
`WIN_OPEN_EX` / `MSG_POLL_EX` / `MSG_WAIT_EX` / `CALLJSON` 实际占用** ——
规划表没跟着划掉，于是它成了一张"看着还有号、其实已经没了"的假清单（再照着它加号就是撞号）。
改法：**规划表只放"还没占号"的**，号一旦实现就写进 §2 全表、那一行删掉；
Toast / 剪贴板 / 系统分享这类改成走 `ui_call_json`（#573 存在的理由就是不占号）。
P1 那 5 个号仍然空着，照原样保留。

判据：桌面自测 **5999/0**（新增 5 条、删掉 4 条手写比对）。

---

用户报的「计算器的按钮颜色还是不对」。v0.96.257 记过一条**未验证**的待办
（"代码与汇编逐条核对过，但没能在真机上验到"），这版在模拟器上把它量到底了。

### 怎么量的（装置与判据）

不是靠眼睛看，是**取原始帧缓冲逐点采样**（`adb exec-out screencap` 裸 RGBA，
绕开本机没有 PIL 的麻烦）：把计算器那屏拉下来，横扫/竖扫若干条线，做**游程编码**，
与源码里的期望值逐条比。

第一眼就推翻了先前所有猜测：

| | 期望 | 实测 |
|---|---|---|
| 数字键 `KEY_NUM 0xFF2A3346` | `#2A3346` | `#2A333E`…`#232732`（**随 y 平滑变暗**） |
| 运算符 `KEY_OP 0xFF7A5A28` | `#7A5A28` | 同上 |
| 等号 `KEY_EQ 0xFF2E7DD1` | `#2E7DD1` | 同上 |
| 功能键 `KEY_FN 0xFF9E4630` | `#9E4630` | 同上 |

**四档配色全被抹平成同一个色**，而且那个色还在**平滑变化** —— 说明它不是"某个键画错了"，
是**这一层之上还盖着别的东西**。

### 三轮最小复现，把范围一步步收紧

| 探针 | 内容 | 结果 |
|---|---|---|
| `probe2` | 十条已知色的实心横带，**不碰任何渐变** | **十条全部逐字节精确** ⇒ `ui_rect` 本身没问题 |
| `probe4` | 把程序自己算的坐标用 `ui_dlg_msg` 打出来 | `h*3=324` 等**全部正确** ⇒ 不是 C 前端算错，是"画的那一侧丢的" |
| `probe5` | 六条**同色**横带，只在中间插一次 `ui_rect_grad` | 渐变**之前**两条 `#2A3346` ✓；**之后**四条画成了 **红→蓝的渐变**（刷子 `g` 的原色）✗ |

`probe5` 那条横扫黑白分明：后面四条带的颜色在 **x 280..795 之间红→蓝、两边被 clamp 成纯色**——
那个区间**正好是那次 `ui_rect_grad` 的刷子矩形**。⇒ **分毫不差地"用的是上一次那个渐变刷子"。**

### 根因（逐层读 MAUI 源码确认，`Microsoft.Maui.Graphics` 10.0.20）

```
PlatformCanvas.SetFillPaint(paint, rect)   渐变 → CurrentState.SetFillPaintShader(shader)
                                                  ↑ 挂在 Android Paint 对象上；开头会清掉上一个
PlatformCanvas.FillColor  { set }          只写 _fillColor，**不碰 shader**
CurrentState.FillPaintWithAlpha            SetARGB(颜色) 之后**直接返回那个 Paint**
FillPath → _canvas.DrawPath(path, FillPaintWithAlpha)
```
**Android 的 `Paint` 里 shader 优先级高于颜色** ⇒ 只要这一帧画过任何一个渐变，
**之后所有 `FillColor = …` 都是空操作**，统统被那个旧渐变接管；落在刷子矩形内的部分
显示渐变，落在外的按 `TileMode.Clamp` 取端点色。

计算器的画法刚好踩满：`draw()` 先画背景径向渐变（`ui_rect_grad` "bg"），
`draw_display()` 再画一块**玻璃面板**（`ui_rect_grad` "glass"：0x33FFFFFF → 0x11FFFFFF 的竖向渐变），
**然后才轮到二十个按键**。按键全在面板包围盒**下方** ⇒ 被 clamp 到 `0x11FFFFFF`，
等价于"给底图叠 7% 白"—— 于是四档配色全被冲掉，只剩下背景那层径向渐变透出来，
屏幕上看着就是"所有按键一个色、还随位置变"。

### 修法

`MauiVectorTarget.FillShape` 的**纯色分支**改成走 `SetFillPaint(new SolidPaint(color), rect)` ——
`SetFillPaint` 是**唯一**会清 shader 的入口。并把这层规矩收成一个入口：

- 新增 `MauiVectorTarget.FillSolid(canvas, argb, rect)`，`DrawWindowFrame` 里两处直接用
  `ICanvas` 铺底的地方（视图背景、场景底色）一并改走它；
- 类注释里写清「渐变的余荫」整条机制 + 实测症状，**规矩一句话：这条路上填色一律经过 `SetFillPaint`**。

纯色那条**复用同一个 `SolidPaint` 实例**改 `.Color`（平台对它就是一句
`FillColor = paint.Color` 读完即弃）—— 这条路每帧走几百次，而这个后端当初就是为了
消掉每帧的垃圾才做的。

### 判据

| | 修前 | 修后 |
|---|---|---|
| `probe5` 六条同色横带 | 后四条 = 红→蓝渐变 | **六条全部 `#2A3346`** |
| `calc.c` 四档按键色 | 全部抹平 | 数字 `#2A3346` / 功能 `#9E4630` / 运算符 `#7A5A28` / 等号 `#2E7DD1` 逐点精确 |

### 这条为什么自测照不出来

桌面的「记录型落笔面」测的是**我们的映射**（哪条指令画到哪个落笔方法、带什么颜色），
而这条错在**平台实现**里 —— 映射全对，是平台把颜色忽略了。
两个后端（光栅/矢量）在桌面上走的是光栅那条，**矢量后端只在手机上跑**。
⇒ 这类"只有真机看得见"的缺陷，判据只能是**在真机上取像素**，没有第二条路。

### 新增 `Examples/c/draw_colors.c`：颜色体检（把"取像素"这一步固定下来）

上面那套量法是临时搭的，这版把它变成一个**随包发的例子**，与 `draw_prims.c` 分工：

| | 管什么 |
|---|---|
| `draw_prims.c` | **形状**（圆角圆不圆、折线开不开口、路径挖不挖洞） |
| `draw_colors.c` | **颜色**（同一条指令画出来的像素，是不是你给的那个色） |

这两件事会**各自独立地坏**：这次就是形状/文字/布局全对，只有颜色被整体抹平。

做法：15 格，每个能上色的调用各占一格，**全部取同一个颜色 `0xFF3C6EB4`**
（r/g/b 三通道互不相同 —— 通道序写反、被别的刷子接管、alpha 被吃掉，都会让它变成另一个色）。
判据是**逐格取格心像素 == `#3C6EB4`**，不靠眼睛。

其中第 11~13 格是**回归格**：它们排在"用过一次渐变"之后，必须仍是 `#3C6EB4` ——
修这条之前，它们画出来是**上一次那个渐变的颜色**。

设备上实测（模拟器 `emulator-5554`，v0.96.298，逐格取格心像素）：

```
0  实心矩形      #3C6EB4 ✔      1  圆角矩形      #3C6EB4 ✔
2  实心圆        #3C6EB4 ✔      3  实心椭圆      #3C6EB4 ✔
4  实心多边形    #3C6EB4 ✔      5  粗折线(边上)  #3C6EB4 ✔
6  粗直线        #3C6EB4 ✔      7  路径填充      #3C6EB4 ✔
8  路径描边(边上)#3C6EB4 ✔      9  空心矩形描边  #3C6EB4 ✔
10 线性渐变      左 #F80007 → 右 #0800F7  ✔（对照格，本来就该是渐变）
11 回归格        #3C6EB4 ✔      12 回归格        #3C6EB4 ✔
13 回归格        #3C6EB4 ✔      14 收尾纯色圆    #3C6EB4 ✔
15 文字笔画      #3C6EB4 ✔
```

**写这个例子时自己踩的一个坑**（顺手记进源码注释）：`ui_path` 的参数序是
**`stroke, width, fill, grad, cap, dash`** —— "描边色"排在"填充色"**前面**，
与 `ui_rect` 那种"颜色在前、开关在后"不一样。写反了**不报错**，只是把空心轮廓
画成了实心三角（第一版就是这么错的，亲眼在截图上看到才发现）。

---

## v0.96.296 — 重建 `vml_lib.zip`：手机端标准库**落后了 37 个版本**

### 起因

打 APK 前重跑 `scripts/make-vml-lib.sh`，顺手把新旧两个 zip 解开来逐文件比了一遍：

```
diff -rq 旧zip/ 新zip/  →  421 个文件内容不同，0 个新增、0 个删除
```

`git log` 一查，**上一个进包的 zip 停在 v0.96.258**。也就是说 **v0.96.259 ~ v0.96.295
这 37 个版本里所有对 `Lib/` 的改动，一个都没到过手机上**。

### 为什么这么久没人发现

`EnsureLibExtracted()` 的解压判据是**内容指纹**（`vml_lib.hash`），而指纹是
**跟着 zip 一起算的** —— zip 没重建，指纹自然没变，设备上那份就一直是老的。
判据本身是对的（正是它让"改内容就重解压"成立），**漏的是"忘了重建 zip"这一步**。
它不报错、不警告：桌面 `vmlcli` 读的是 `third_party/vml/Lib/` 的**当前**文件，
只有手机上跑的是**包内那份**。⇒ **桌面全绿、手机上是三个月前的库**。

（这条与本仓库记过两次的「平行表」同源：同一份数据两处存放 —— 磁盘上的 `Lib/`
与包内的 `Lib/` —— 靠人记得重建来保持同步。**打包产物要进版本控制，就得有一步
会提醒你重建它的东西**；本轮先把这个事实记进 CHANGELOG 与脚本注释。）

### 这 37 个版本里错过的内容（挑影响大的）

| 改动 | 版本 |
|---|---|
| 包装器改从栈帧读实参（**总根源修复**）+ Lua 真压栈 | v0.96.262 |
| `ParseFunctions` 先剥注释（清掉虚构函数）+ 撞名保险 | v0.96.265 |
| **P1 库侧清源**：`modules.json` 59 个虚构名、22 个 `syscall.vml` 陈旧件 | v0.96.267 |
| 汇编器 `; N:` 源码行号注释进产物 | v0.96.290+ |
| 调用约定统一（**两套栈清理约定并存**的根因修复） | 分家后重生成 |

最重的是**库侧清源**那一条：包里 22 份 `syscall.vml` 是**孤儿陈旧件**
（生成器源码里早已没有 `syscall` 模块），而 `graphics`/`browser_gfx` 里
**59 个 `CALL Sector` 式死包装器**曾让 4 门语言报「未解析标签 49~60 个」——
手机上跑的是没清过的版本。

### 判据

| | 之前（v0.96.258 的包） | 之后 |
|---|---|---|
| zip 内 `Lib/` 与仓库 `Lib/` | **421 个文件不一致** | 逐文件一致 |
| `Lib/basic/util.vml` | 还带着 `BASIC_ROOT` 跳板 | 已随重生成移除 |
| 设备侧 | 指纹没变 ⇒ **不重新解压** | 指纹变 ⇒ 自动重解压 |

重打 APK 时已一并验证：`vml_lib.hash` 指纹 `2d12a2cc…`，95 个示例随包。

---

## v0.96.295 — P5：让 C 解析器**真的报**语法错（从 0 条到多条）

### 先纠正计划的前提

计划里写的是「语法错只报第一个」。实测**根本不是** —— 是**一个都不报**：

| 输入 | 改之前 |
|---|---|
| `int a = 1 return a;`（缺分号） | ✔ 编过 |
| `int a = ;` | ✔ 编过 |
| `{ return 0;`（未闭合大括号） | ✔ 编过 |
| `switch(1){ case: return 1; }` | ✔ 编过 |

### 三层「吞点」，逐个查出来

**① `CCompiler.CompileFile` 根本没接诊断袋。**
`Lexer.Diagnostics` 与 `Parser.Diagnostics` 都留在 null（另一条入口 `Compile` 从 v0.96.269 起就接了，
`vmlcli` 走的偏偏是这条没接的）⇒ 词法/语法错**无处可去**。已接上，并让它在 `Parse()` 之后
一次性抛（与代码生成那半的 `BuildProgram` 同口径）。

**② 两个容错点只往 stderr 写一行。**
顶层 `[SKIP] 跳过无法识别的顶层token` 与 `[RECOVER] 顶层解析异常恢复` ——
**手机上 stderr 是看不见的流**，用户看到的是"编译成功"，代码却少了一整段。
改成报进 `Diagnostics`（**不抛**，解析照常恢复并往下走 ⇒ 一次能报多条）。
实测：`Examples/c|cpp|objc` 全部**零触发** ⇒ 升级成错误不会误伤正常代码。

**③ `ParsePrimary` 把「缺失的表达式」静默当成 0。**
兜底分支写着「遇到未预期的 token 返回空节点并跳过」，于是 `int a = ;` 里的 `;` 被跳过、
返回 `NumberLiteral(0)` ⇒ **静默变成 `int a = 0`**。已加一条诊断（同样零误报）。

### 「多报」的真正拦路虎：位置恒为 0

修完上面三处还是**只报 1 条**。查下来是**两个叠在一起**的坑：

- `ParserBase.GetTokenLine/GetTokenColumn` 是 `=> 0` 的虚方法，**C 没覆写** ⇒ 位置恒为 0；
- 而 `DiagnosticBag` 按「码+文件+行+列+消息」**去重** ⇒ 位置全一样的多条错误**并成一条**。

覆写时又踩到第二层：**C 的解析器用自己的 `pos`/`tokens`，不是基类的 `_pos`/`_tokens`**
⇒ 基类那个 `Cur` 恒指**第 0 个 token**，覆写里拿到的 `token` 恰恰是走错的那个。
所以覆写必须**绕开传进来的 token、改用 C 自己的 `Current()`**。

判据（同一行三处 `int x = ;`）：

```
/tmp/_p5d.c:2:13: error: 表达式缺失或多余（遇到 ';'） [Parser_SyntaxError]
/tmp/_p5d.c:4:13: error: 表达式缺失或多余（遇到 ';'） [Parser_SyntaxError]
```

行号与列号都与源码对得上（`    int a = ;` 的 `;` 在第 13 列）。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `int a = ;` | 编过 | **报错** |
| 顶层乱 token `)))` | 编过（只写 stderr） | **报错** |
| `Examples/c` 误报 | — | **0** |
| `diag-probe` / `out-probe` / `examples-build` | 60/0/0 · 29/29 · 78/3 | 60/0/0 · 29/29 · 78/3 |

### 还没做的（P5 未完）

`{ return 0;`（未闭合大括号）**仍然编过** —— 那是**另一条路**（块解析的收尾），
与上面三处无关，记账待查。同类"多处语法错"目前能报 2/3（第三条被恢复逻辑吃掉），
恢复的**粒度**（按语句边界而不是按大括号）才是计划里 P5 原定的活。

---

## v0.96.294 — 行列号铺开第四批：8 门一次补齐 → **15/16 静态语言**

这一批是最大的：**C# / C++ / Go / Java / JS / Kotlin / Swift** 七门的 `ASTNode` 是**空基类**
（一个位置字段都没有），加上**盖戳**与**接线**，一门要动三处。

### 三步走的模板（与 C 那次完全一致）

1. **给基类补字段**：`Line`（预处理后，索引 `SourceLines`）/ `OriginalLine`（原文件，给人看）/
   `Column`。C# 那门原本 `public abstract class ASTNode {}` 是个纯空壳。
2. **解析器盖戳**：7 门都有**单一 `ParseStatement()`** ⇒ 改名 + 包一层，
   记下 `Cur` 的行列、解析完盖到节点上（`Line == 0` 才盖，不覆盖子解析器更精确的位置）。
   ⚠ 中途脚本把 `sig`（已含 `{`）又拼了一个 `{`，七门一起 CS1513 —— 逐文件回读时才发现。
3. **代码生成接线**：6 门有集中的 `GenerateStatement`/`GenerateStmt`，Kotlin 那门叫
   `GenerateNode`（**名字不同，得单独找**）。

### 结果

| | |
|---|---|
| **有行列号（15）** | `c` `cpp` `cs` `d` `dart` `f90` `go` `java` `kt` `ld` `m` `pas` `rs` `swift` （+ 动态 6 门走 `dyn-global`） |
| **仍无（2）** | `fth`（词调用不走语句入口）、`bas`（报错点在 `GetOrCreateVariable`） |

### ⚠ 已知不准确的一处：`cs = 1:1`

C# 报的是 `1:1`，而用例里 `nosuch` 在第 4 行 —— 说明**内层语句的 `Line` 仍是 0**，
`CurrentSourceLine` 停在类声明那一句（第 1 行）。已确认 `ParseBlock` 确实走 `ParseStatement`，
所以是 C# 前端里**另有一条局部声明的解析路径**绕过了它。**记账待查**，
其余 14 门的行号都与源码对得上（`go=3:2`、`java=4:5`、`kt=3:5`、`swift=3:5`、
`cpp=1:25`、`c=1:29`…）。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |
| 桌面自测 | 5998/0 | 5998/0 |
| MAUI Android 构建 | — | 0 错误 |

---

## v0.96.293 — 行列号铺开第三批：Ladder（**7/22**）

Ladder 的情况与 Rust 相反：**解析器那半边是齐的**（`LadderCompiler/Parser*.cs` 里有 38 处
`Line =` 赋值），缺的一直只是**代码生成侧读取**。

它是逐节点 `Visit` 重载（没有集中的语句分发），所以挂点选在**语句列表的遍历处** ——
ST 语句（`program.StStatements` / `func.StStatements`）是全部语句到达代码生成的公共通道，
一共 **3 处**。挂到每个 `Visit(XxxNode)` 里要改十几处，将来新增节点类型还容易漏。

判据：`undef-var.ld` → `<input>:1:11: error: 未声明的变量 'nosuch'`
（`PRINT_INT nosuch` 里 `nosuch` 正好在第 11 列，与源码对得上）。

### 进度：**7/22**

| | 语言 |
|---|---|
| **有行列号（7）** | `c` `d` `dart` `f90` `ld` `m` `pas` `rs` |
| 剩余 | `bas` `cpp` `cs` `fth` `go` `java` `kt` `py` `swift` + 动态 6 门 |

⚠ 顺带查清 **Python 与 Rust/Ladder 的情况不同**：它的 `ASTNode` 是
**构造函数注入**位置（`protected ASTNode(type, line, column)`，属性只读）——
解析器那半边本来就有值，缺的同样只是代码生成侧读取（它有 ~20 处语句遍历点，得逐处挂）。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |

---

## v0.96.292 — 行列号铺开第二批：Rust；并量清了「还差哪些」

### Rust：解析器那半边才是缺的

上一版给 Rust 加了代码生成侧的挂点（`Visit(ProgramNode)` / `Visit(BlockNode)` 两处遍历），
**但它是空转的** —— 实测 Rust 报错依然没有行号。查下来：

> `ASTNode` 里 `Line`/`Column` **字段早就有，解析器从不赋值** ⇒ 恒为 0
> ⇒ `if (node.Line > 0) …` 永远不成立。

**这不是 Rust 一家的问题**，是全仓的普遍状态（`grep 'Line\s*=' <lang>/Parser*.cs`：
Rust 0 处、Python 0 处、D 0 处、Lua 0 处 …… 字段是摆设）。
所以真正要补的是**解析器侧**，与 C 那次同一套路：

```
private ASTNode ParseStatement()          // 新：记下 Cur 的行列，解析完盖到节点上
{
    int line = Cur.Line, col = Cur.Column;
    var node = ParseStatementCore();
    if (node != null && node.Line == 0) { node.Line = line; node.Column = col; }
    return node;
}
private ASTNode ParseStatementCore() { …原来的全部 return… }
```

判据：`undef-var.rs` → `<path>:3:5: error: 未声明的变量 'nosuch'`（第 3 行第 5 列，
`let b = a + nosuch;` 里 `let` 的位置，与源码对得上）。

### 量清了全景：**6/22 已带行列号**

不做模糊表述，直接逐门量「报错里有没有 `:行:列:`」：

| | 语言 |
|---|---|
| **有位置（6）** | `c` `d` `dart` `f90` `m` `pas` |
| **无位置（10）** | `bas` `cpp` `cs` `fth` `go` `java` `kt` `ld` `rs`→已修 `swift` |
| 动态语言 6 门 | `js` `lua` `py` `r` `rb` `scm` —— 走 `dyn-global` 组，不在此列 |

⚠ 中途一次**测量方法本身错了**：我先去 `.vml` 产物里数 `; N:` 注释，得到"22 门全 0"——
但 `--vml` 写的是**汇编之后**的 program，那份的 `SourceLines` 是 null，本来就不该有注释。
看到"全 0"差点当成"全军覆没"，换成量**端到端报错里的位置**才对上。

### 接下来的活（一张清单）

剩下的门分三类，做法都已经验证过：

1. **解析器有单一 `ParseStatement()`**（Rust 已做，Python/Ladder 等大概率同形）——
   改名 + 包一层，与 C/Rust 完全一致。
2. **AST 连位置字段都没有**（C# / C++ / Go / Java / JS / Kotlin / Scheme / Swift）——
   先给基类补 `Line`/`Column`，再做第 1 步。
3. **报错不走语句入口**的几门（`bas` 的 `GetOrCreateVariable`、`fth` 的词调用、
   `ld` 的 `LoadVariable`）—— 要看它们各自的路径，可能需要额外的挂点。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |

---

## v0.96.291 — 行列号铺开第一批：10 门补上**列**

用户定了「全部需要修复」，这一版开铺。先做收益最高、风险最低的一批 ——
**已经在设 `CurrentSourceLine`、只是没设列的 10 门**（它们本来就有集中的语句入口）：

| | |
|---|---|
| 同形一句话改（7 门） | D / Forth / Fortran / Lua / ObjC / R / Ruby |
| 形状不同、单独改（3 门） | Dart / Pascal / Basic（Basic 有两处语句循环） |

改法是把 `if (node.Line > 0) CurrentSourceLine = node.Line;`
扩成 `{ CurrentSourceLine = node.Line; CurrentSourceColumn = node.Column; }` ——
**一处一处改、改完逐文件 `grep` 回读确认**（本仓铁律：脚本改完文件必须回读，
`replace_all` 误伤与 CRLF 静默失配都栽过）。

实测生效：`pas` → `<input>:3:10:`、`c` → `<input>:1:29:`。

### 普查出来的全景（这一版之后还剩什么）

| 状态 | 门数 | 语言 |
|---|---|---|
| 行 + 列都有 | 1 | C |
| 只有行、**列刚补上** | 10 | D / Forth / Fortran / Lua / ObjC / R / Ruby / Dart / Pascal / Basic |
| 有 `Line`/`Column` 字段但**没接** | 3 | Ladder / Python / Rust |
| **AST 连位置字段都没有** | 8 | C# / C++ / Go / Java / JS / Kotlin / Scheme / Swift |

后两类是接下来的活。第三类（Ladder/Python/Rust）按 C 的路子接上即可；
第四类要先给基类补 `Line`/`Column`/`OriginalLine`，再在语句入口盖 ——
与 C 同一套做法（`Parser.ParseStatement()` 改名 + 包一层，一处覆盖全部 return）。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |

---

## v0.96.290 — 未使用局部变量 + **两个行号的分工**（修 v0.96.289 引入的回归）

v0.96.289 加上源行注释之后 `nat.c` / `gomoku.c` / `draw_prims.c` 全挂了，
这一版把它查清并修好，同时补上「未使用局部变量」那半。

### 回归的真身：**两个行号被当成一个用了**

v0.96.289 让 C 的语句带上行号，于是产物里第一次出现了 `; N: <源码行原文>` 注释。
两个后果：

**(a) 注释引错了原文。** `VmlProgram.ToString()` 取的是 `SourceLines[N - 1]`，
而 `SourceLines` = **预处理后**的文本；我却把 `OriginalLine`（原文件行号）填进了 `Instruction.SourceLine`
⇒ **索引错位**，引出来的是 `stdio.h` 里的注释文字。

**(b) 引出来的原文里含 `#include`，把宿主的产物自检打成假失败。**
`LooksLikeVml` 的反判据是 `text.Contains("#include")` —— 而 C 程序第一行往往就是
`#include <stdio.h>`，被注释原样引进去 ⇒ **所有带 #include 的程序全部"前端编译没有产出 VML 汇编"**。

**修法：两个行号各归各位，不能互相替代。**

| | 谁用 | 为什么 |
|---|---|---|
| `ASTNode.Line`（预处理后） | 索引 `SourceLines`、生成 `; N:` 注释 | 必须与 `SourceLines` **同一套索引** |
| `ASTNode.OriginalLine`（原文件） | **报给用户/编辑器** | `#include` 一展开就整体推后（gomoku 差 **138 行**） |

`CodeGeneratorBase` 相应加 `CurrentSourceOriginalLine` 与 `DiagLine`（优先原文件行号，
拿不到才退回），诊断一律取 `DiagLine`，而 `Instruction.SourceLine` 仍取 `CurrentSourceLine`。

`LooksLikeVml` 的反判据也一并收紧成「**行首** + **跳过 `;` 注释行**」——
原来那句 `Contains` 把"注释里引用的源码"当成了"预处理器没展开"。

⚠ **排查这笔回归的方法值得记**：先用 `git stash` 确认是不是本次改动，再二分；
中途我犯过一次错 —— `stash pop` 之后**忘了重建**，拿旧二进制跑出一串"都能编过"的假结论，
白绕一圈。**改完代码一定要重建再测。**

### 未使用局部变量（用户要的另一半）

判据：「声明过、但整个函数体里**一次都没被引用**」——比"只写不读"更保守：
后者要区分"死存储"和"有意写但不读"，误报风险大；而"从头到尾没出现过"没有任何争议。
（这也正是 C 编译器自己的分界：`-Wunused-variable` 管的就是这个，
"写过却没读"是另一档 `-Wunused-but-set-variable`，本版不做。）

两个挂钩点：

| 干什么 | 挂在哪 | 为什么是这儿 |
|---|---|---|
| 记「声明了哪些」 | `CountLocalVariablesEx` | 局部量登记的**唯一入口**（帧大小与偏移都在那儿算） |
| 记「用到过哪些」 | `FormatVarOffset` + 数组基址那条 | 读/写/取地址都要拿变量的内存位置，都从这儿过 |

**踩到的坑**：`int never_used = 5;` 的**写初值**也走一次变量访问 ⇒ 每个带初值的局部量
都被当成"用过了"，**整个检查形同虚设**。在语句分派处理 `VariableDecl` 之后把这一次擦掉
（声明之后再被写会重新记账，所以 `x = 1;` 在函数体里出现仍然算使用）。

**判据**：`Examples/c/*` 上一共报出 6 条，逐条核过**全是真的**（
`gomoku.c` 的 `x0/y0/x1/y1` 全文件只出现在声明处、`pacman.c` 的 `new_game()` 里 `i` 在该函数内一次没用过、
`plane.c:270` 的 `hit` 同理），行号都指到**原文件**的声明行。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `vml-out-probe` | **28/29**（回归） | **29/29** |
| `examples-build` | **76/5**（回归） | **78/3** |
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-abi-probe` | 7/7 | 7/7 |
| 桌面自测 | 5998/0 | 5998/0 |
| MAUI Android 构建 | — | 0 错误 |

---

## v0.96.289 — C 的 AST 补上位置信息：报错终于有**行列号**了

### 一句注释挂了很久

```
// ⚠ **C 这边暂时设不了 `CurrentSourceLine`** —— 它的 `ASTNode` 是个**空基类**，
//   一个位置字段都没有（`Token` 上倒是有 `Line`/`Column`，但解析器没往 AST 上带）。
```

这是全仓**唯一**一门在 `GenerateStatement` 里设不了行号的语言（Dart/Basic 等早就有 `node.Line`），
代价是三件事一起做不了：报错给不出行列号、警告指不到声明处、编辑器气泡没有锚点。
**杠杆最大的一处**，这一版补上。

### 填法：一处覆盖 36 个 return

`Parser.ParseStatement()` 是**单一入口**（36 个 `return` 全在一个方法里）。
所以不改那 36 处，而是**改名 + 包一层**：

```csharp
private ASTNode ParseStatement()          // 新：记下起始 token 的行列
{
    int line = Current().Line, col = Current().Column;
    var node = ParseStatementCore();
    if (node != null && node.Line == 0) { node.Line = line; node.Column = col; }
    return node;
}
private ASTNode ParseStatementCore() { ...原来的 36 个 return... }
```

用 `Line == 0` 才盖 ⇒ 子解析器自己填过的**更精确**的位置不会被外层冲掉。

⚠ **只盖语句/声明级别**，不盖表达式节点 —— 那要改几百处构造点，而三件要办的事
（报错行列号 / 警告指到声明处 / 气泡锚点）**都只需要语句粒度**。

### 顺带补上「列」

`ReportUndefined` 里列是**硬编码 0** 的。加 `CodeGeneratorBase.CurrentSourceColumn`
与 `CurrentSourceLine` 配对，C 在语句入口一起设。

**`WarnUnused` 那处刻意不用 `CurrentSourceColumn`**：它通常在生成之后的清理段调用，
游标早就不在声明处了，那一列会指到毫不相干的位置上 —— **比没有列更糟**。
所以显式传 `line` 时列也一并传，不传就写 0。

### 判据

| 位置 | 改之前 | 改之后 |
|---|---|---|
| C 未声明变量（`_multi.c` 第 3~6 行） | `<input>: error: …`（无位置） | `<input>:3:5:` / `:4:5:` / `:5:5:` / `:6:5:` |
| 未使用函数（`static` 在第 5 行第 12 列） | `<input>: warning: …` | `<input>:5:12: warning: …` |

两者都与源码对得上（`int a = ...` 缩进 4 空格 ⇒ 列 5；`static int never_used` 的 `n` ⇒ 列 12）。

| | 之前 | 之后 |
|---|---|---|
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `vml-abi-probe` | 7/7 | 7/7 |
| `examples-build` | 78/3 | 78/3 |
| 桌面自测 | 5998/0 | 5998/0 |

### 现在还剩什么

`P5 语法错误恢复` 的前提被实测推翻了（C 解析器**不报**语法错，不是"只报一个"）——
那是"解析器够不够严"的设计取舍，要另定。`未使用局部变量` 那半：
位置信息这一版已经就位，缺的只是"本函数引用过哪些名字"的记录集。

---

## v0.96.288 — 把编译期警告接到手机上：`MauiVml` 只接了运行期的 stderr

### 症状：功能做了，用户永远看不到

`MauiVml` 有一段**很讲究**的处理 —— 把 `Console.Out/Error` 临时接到 `StringWriter`，
因为「运行时的诊断输出走 `System.Console`，而手机上那是一个看不见的流」
（`VMLRuntime` 的内存错误 / 标签错误 / 寄存器 dump 全走 stderr）。

但那段捕获**只套在 `vm.Run(ct)` 外面**。前端编译是在**另一处**、更早发生的：

```csharp
var compile = Task.Run(() => ex.CompileFileWithIncludes(...), ct);
...
vmlText = compile.Result;      // ← 这期间没人接 stderr
```

⇒ v0.96.286 起新增的**编译期警告**（未使用符号那类）在手机上**全部落进虚空**。
不是"报错"，是"做了但没人看得见" —— 这正是本仓库反复记的那一类。

### 修法：同一个闸门、同一套理由

编译那段也包进 `lock (ConsoleRedirectGate)` + `Console.SetError(errSink)`，
收完并把内容**并进结果**（不丢）。取 `Severity.Warning` 的那些塞进 `BuildProgram` 的
返回值 —— 那是 `CompileForEditor` 用来画气泡的列表，`VmlDiagnostics` 早就把 GCC 风格认全了，
所以这一改**直接落在用户要的「给 IDE 报警告提示用」上**。

⚠ 代价说清楚：这把锁要**持有一两分钟**（编译本身就那么久），而运行期那段只持有几十毫秒。
可以接受是因为 VML 工具是 Exclusive、`ShellPage` 另有 `_busy` 闸门，正常不会与运行期的捕获并发；
真并发时的表现是"等一会儿"，而不是输出串台。

### 判据

| | 之前 | 之后 |
|---|---|---|
| MAUI Android 构建 | — | **0 错误** |
| 桌面自测 | 5998/0 | 5998/0 |
| `diag-probe` | 60/0/0 | 60/0/0 |
| `examples-build` | 78/3 | 78/3 |

⚠ **只对 `CompileForEditor` 那条路生效** —— `CompileAndRun` / `CompileToVml` 都把 Diags
丢掉了（它们只要"能不能跑"）。命令行页要显示警告是另一件事，没做。

---

## v0.96.287 — 未使用符号警告（C）：定义了却没人用的 static 函数

用户的诉求：「那些定义了，却没有使用的局部变量或者函数（**外部访问不了的**），
要出警告，可以给 IDE 报警告提示用」。

### 先说 P5 的一盆冷水：C 解析器**几乎不报语法错**

按计划去量「一次多报语法错」，结果实测是**根本不报**：

| 输入 | 结果 |
|---|---|
| `int a = 1 return a;`（缺分号） | ✔ 编译通过 |
| `int a = ;` | ✔ 编译通过 |
| `{ return 0;`（未闭合大括号） | ✔ 编译通过 |
| `switch(1){ case: return 1; }` | ✔ 编译通过 |

⇒ P5 的前提（"语法错只报第一个"）**跟实测不符**，真问题是「解析器够不够严」。
这属于**设计取舍**（收紧会让现在能编的宽松写法反过来编不过，比如 `Examples` 里那些），
不该我一个人拍板 —— 记账待定。

### 未使用函数：两处判据，都是被实测打回来的

C 前端在 `CodeGenerator.Functions.cs` 里有一趟**可达性分析**（从 `main` + 中断函数出发
BFS 调用图），不可达的函数**直接从 AST 剔除** —— 此前完全静默，
用户写了个函数、以为它在，产物里一个字都没有（实测 `int unused_fn(...)` 在 `.vml` 里出现 **0 次**）。

判据写在剔除点，但**两条都是踩过坑才补上的**：

| 判据 | 不加会怎样 |
|---|---|
| ① **有函数体**（`Body` 非空） | 第一版没带 ⇒ `Examples/c/*` 每个文件刷出 **40~58 条**：那时 `ast.Functions` 里已塞满 `#include` 带进来的**库函数声明**（`waycoder_ui.h` 那几百个），它们当然"没被本文件调用"。**声明 ≠ 定义** |
| ② **`static`**（用户随后补的「只报外部无法访问的」） | 非 static 有**外部链接**，别的翻译单元随时可能调它，"本文件没调"说明不了什么 |

⚠ 判据②需要 `static` 这个信息，而**解析器把它丢了** —— 存储类说明符读进局部变量
`storageClass` 就扔了，`Function` 上根本没这个字段 ⇒ 这条判据以前**没法表达**。
已补 `Function.IsStatic` 并接线。

判据（`/tmp` 造的最小用例）：

```c
static int unused_static(int x){ return x*2; }   // ← 报
int  exported_fn(int x){ return x+1; }           // ← 不报（外部可访问）
int  main(void){ return exported_fn(1); }
```

```
<input>: warning: 定义了但从未使用的函数 'unused_static' [CodeGen_UnusedFunction]
✔ 编译完成
```

**`Examples/c/*` 与 `Examples/cpp/*` 全部零警告**（两条判据合起来正好把误报收干净）。

### 顺带：警告终于有出口

`Diags.AddWarning` 在 v0.96.286 之前是**零调用点**，而 `BuildProgram` 只抛错误、
警告收集了从不上报 —— 等于没有。这一版起它按 GCC 风格 `file:line:col: warning: …`
打到 stderr，宿主侧 `VmlDiagnostics` 的裸 `warning:` 规则正好接上（编辑器里就是黄色气泡）。
新增两个错误码 `CodeGen_UnusedFunction` / `CodeGen_UnusedVariable`（1316/1317）
与共享助手 `WarnUnused(...)`。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `diag-probe` | 60/0/0 | 60/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |
| C 例子的未使用警告 | — | **0 条** |

**还差局部变量那一半**（用户要的是"局部变量或者函数"）：需要一个"本函数里引用过哪些名字"
的记录集，与声明集对比，并排除形参 —— 比函数那半多几处挂钩点（读 / 写 / 取地址 / 数组基址），
留作下一步。

---

## v0.96.286 — 未声明变量收尾：**16/16**（C++ / Rust / Ladder / Pascal + Fortran / Basic 的语义闸门）

### 普通 4 门

| 语言 | 落空分支 | 形态 |
|---|---|---|
| C++ | `CodeGenerator.Expressions.cs` 的 `case IdentExpr` 末位 | 静默发 `MOVE R0, var_x`，连槽都不建 |
| Rust | `CodeGenerator.Expressions.cs` 读标识符的 else | 同上（裸名，值取决于汇编器/内存残值） |
| Ladder | `CodeGenerator.Core.cs` 的 `LoadVariable` | 静默 `MOVE R0, #0` |
| Pascal | `CodeGenerator.Expressions.cs` 的第四个 else | 顺手建初值 0 的全局槽 |

Pascal 那处**保留原来那句建槽**、没有改成 `EmitUndefinedFallback + return`：它嵌在四层
`if/else` 里，提前 return 会跳过后面收尾的指令生成，而"建个 0 槽照旧往下走"与旧行为
**逐字相同**、零结构风险（编译反正会因为那条诊断失败）。

Ladder 另有一处**逐字同形**的分支（`Elements.cs` 的 `LoadVariableOrValue`）**没动**：
它的入参是"变量名**或字面量**"（先 `int.TryParse` 再查表），落到 else 的还可能是它认不出的
其它字面量形态，直接报错有误伤风险，而**没有任何用例能区分这两种情况**。

### Fortran / Basic：先补「指令语义」，再谈报错

这两门**不能一刀切** —— 用户原话「少数语言不用声明，根据语言特性来定」：

| | 默认语义 | 该报错的开关 | 现状 |
|---|---|---|---|
| Fortran | 隐式类型（i-n 为 integer、其余 real） | `implicit none` | 解析成 `NopNode` **直接丢**，语义完全没生效 |
| QBasic | 未声明即隐式全局（值 0） | `OPTION EXPLICIT` | 只认 `OPTION BASE`，`EXPLICIT` 被**静默吞掉** |

⇒ 两条指令都补上了语义：解析器落一个**每文件**的标志位（不是 `ImplicitDeclarationAllowed`
那种编译期常量 —— 同一门语言的不同文件可以不同），代码生成据此决定
**报错**（写了开关）还是**只警告**（没写）。

⚠ 为了"只警告"这一半能成立，顺带**给警告开了出口**：`Diags.AddWarning` 此前是
**零调用点**，而 `BuildProgram` 只抛错误、警告收集了从不上报 —— 等于没有。
现在 `BuildProgram` 把警告按 GCC 风格 `file:line:col: warning: …` 打到 stderr，
宿主侧 `VmlDiagnostics` 本来就认这个形状（v0.96.283 新加的裸 `warning:` 规则正好接上）。

### Basic 的另一个坑：判据不能写在调用点

Basic 有**四五个**位置调用 `GetOrCreateVariable`，其中 `CodeGenerator.Expressions.cs`
那条是**无条件**的。第一版把"报错还是警告"写在了 `CodeGenerator.Sub.cs` 的 else 里，
实测 **`PRINT nosuch` 根本不走那条路**（`OPTION EXPLICIT` 形同虚设）。
⇒ 判据收进 `GetOrCreateVariable` 一处 —— 那是全前端**唯一**「没见过就造一个」的出口
（本仓头号坑就是"同一规则两处实现"，这次是被实测逼出来的）。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `undef-var` | 10/16 | **16/16** ✅ |
| `dyn-global` | 6/6 | 6/6 |
| `undef-fn` / `link-clean` | 16/16 / 22/22 | 16/16 / 22/22 |
| `diag-probe` 合计 | 54/12 | **60/0/0** |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3（3 条为已记录在案的） |
| 桌面自测 | 5998/0 | 5998/0 |

**P6（逐门未定义变量）16 门静态语言全部完成**；6 门动态语言由 `dyn-global` 反向护栏钉住。

---

## v0.96.285 — 未声明变量：再修 7 门（ObjC / Go / Swift / Dart / C# / D / Java）→ **10/16**

### 三路并行侦察的结论：形态只有两种，基类早就备好了工具

13 个前端**全都（间接）继承 `CodeGeneratorBase`** —— 它上面现成摆着
`ReportUndefined(name, code, kind)`（收集、不抛）与 `EmitUndefinedFallback()`（发 `MOVE R0,#0`），
但 grep 全仓**一门都没调过**。读变量的落空分支只有两种形状：

| 形态 | 语言 | 特征 |
|---|---|---|
| **顺手建个初值 0 的全局槽** | ObjC / Swift / Dart / C# / D / Java / Pascal / Fortran / Basic | `if (!dataSection.ContainsKey("var_x")) dataSection["var_x"] = 0;` |
| **假定存在全局符号，直接 load 名字** | Go / C++ / Rust | 连槽都不建，引用一个可能根本不存在的标签（连"确定的 0"都不是） |

修法统一：把落空分支换成 `ReportUndefined(...)` + `EmitUndefinedFallback()` + `return`。
落空分支里发的占位值本来就等价于"读个 0"，所以**除了多一条诊断，生成代码语义不变**。

### 一门一个坑：Java 的静态字段被解析器丢了

`Examples/java/catch.java` 立刻报了 `未声明的变量 'A'` —— **误报**。
查下来 `static int[] A = new int[10];` 的 `FieldDecl` 被解析器收进了 `ClassDecl.Fields`，
而 `GenerateClass` **只遍历构造函数与方法**（`grep '\.Fields' CodeGenerator*.cs` 零命中）
—— 字段从来没被代码生成消费过。

此前之所以没暴露，是因为 `GenerateVariable` 那条兜底**把它蒙对了**：数组基址要的正是**地址**，
而那条发的恰好是 `LABEL`。兜底一升级成硬报错，误报就来了。

⇒ 在 `GenerateClass` 开头把字段名登记进 `dataSection`。**生成出来的代码与登记之前逐字相同**
（标签一直是 `var_<名字>`，值同样是 0），只是把"这个存在"提前告诉符号表。

⚠ 顺带记下**没动**的一条：Java 那句用的是 `OperandType.LABEL`，而本 VM 里 LABEL 的语义是
**取标签地址**、不是取值 —— `CSharpCompiler/CodeGenerator.cs:368-375` 有段注释专门记过这个坑
（"v0.96.185 前这里是 LABEL…读出来是地址（几千）"），C# 已改成 `MEMORY`，Java 至今没改。
它只影响**真正声明过的全局变量**的读取，而 `Examples/java/` 三个例子都只有 `static native`
方法、没有静态字段可读 ⇒ **没有用例能验证这次改动**，按「没验证就不改」留作待办。

### `dyn-global` 组补上了（豁免的反向护栏）

6 门动态语言（js/lua/py/r/rb/scm）各一条「赋值给未声明的名字 + 打印它」，
`.expect` 期望输出 `7`。组里加了**输出比对** —— 只判"编得过"是不够的：
「隐式全局被静默当成 0、值丢了」同样编得过、也不崩，那正是本组要防的"看起来没坏"。

⚠ 写用例时踩到两处语法：**Lua 必须显式写 `main()` 调用**（`out.lua` 末尾就有，
漏了不报错、只是什么都不输出）；`dyn-global.*.expect` 会被 `dyn-global.*` 的 glob
当成用例 —— 与前两组同一个坑，已排除。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `undef-var` | 3/16 | **10/16** |
| `dyn-global` | 组不存在 | **6/6** |
| `undef-fn` / `link-clean` | 16/16 / 22/22 | 16/16 / 22/22 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |

剩 6 门：`cpp` `rs`（"假定存在全局符号"形态）、`pas` `ld`（多调用点）、
以及两门**有语言语义例外**的 `f90` `bas` —— Fortran 要 `implicit none`、
QBasic 要 `OPTION EXPLICIT` 才该报，而这两个指令**目前都只被解析、没有被强制执行**
（`implicit none` 解析成 `NopNode` 丢弃；`OPTION EXPLICIT` 的 `EXPLICIT` 根本不被识别）。
这两门得先补上指令语义，不能一刀切。

---

## v0.96.284 — 未声明变量（P6 第一门：C）；新建 `undef-var` 判据组量出 **2/16** 的基线

### 判据先行：`undef-var` 组

`undef-fn`（未定义**函数**）那条线已经全绿，但用户的原话是「没有声明的**变量或者函数**」——
变量这一半一直没判据。新建 `cases/undef-var.<ext>` × 16（静态语言），
语义与 `undef-fn` **完全同形**（编译必须失败 + 点名符号），所以 run.sh 里是
**同一个组函数跑两遍**（`group_undefined <前缀>`），不是抄一份 —— 本仓头号坑就是「同一规则两处实现」。

**基线 2/16**（只有 `fth` 和 `kt` 报）。用例统一写成**读**一个未声明的变量
（`int a = 1; return a + nosuch;`）—— 因为「写」那一侧好几门语言本来就报，
只测写会把问题盖住。

### C：五条路径里只有「读」这一条漏着

C 的 `GenerateExpression` 在标识符落空时有五条出口：取地址 / 左值 / 数组元素 / 下标
**早就在报错**，只有**求值（读）**那条写着：

```csharp
if (Options.DebugMode) Console.Error.WriteLine($"警告: 未定义的变量…");
instructions.Add(new Instruction(MOVE, R0, IMMEDIATE 0));   // 静默按 0 继续
```

⇒ `int a = 1; return a + nosuch;` 编得过、运行期静静算出个错答案。
改成走同一处的 `NoteUndefinedVariable`（它发的占位值**就是**那条 `MOVE R0, #0`
⇒ 除多一条诊断外，生成出来的代码一字不差，不会级联出假错）。

### 它当场抓出一个真 bug

`Examples/c/mario.c` 立刻编不过：`<input>: error: 未声明的变量 'COL_WARN'`。

查下来 `mario.c` 的调色板里**从来没有 `COL_WARN`**（19 个 `COL_*` 一个不缺，就少了它），
而第 686 行拿它当「游戏结束」那行字的颜色 —— 以前静默按 0，画出来是**透明色**
（那行字等于没上色）。已按 `tetris.c` 的同一个值 `0xFFF87171` 补上定义。

这正是这条检查该有的样子：**用户没报的 bug，编译器先报**。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `undef-var` | 组不存在 | **3/16**（c / fth / kt） |
| `undef-fn` | 16/16 | 16/16 |
| `link-clean` | 22/22 | 22/22 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3（`c/mario.c` 修好后回到 78） |

⚠ `run.sh all` 现在会**非零退出** —— `undef-var` 那 13 条红的就是 P6 的工作队列，
不是回归。README 里已写明。

---

## v0.96.283 — 报错带行号（P3 前端半边）+ 编辑器不再把 N 条错误并成 1 条

### ① 行号链路：汇编器那半边早就有，**上游一直没喂它**

`Instruction.SourceLine` → `; N:` 注释 → 汇编器读回，这条链 v0.96.269 就铺好了。
但实测**前端产物里一条 `; N:` 都没有** —— 因为 `InstrList.Add` 是从
`CodeGeneratorBase.CurrentSourceLine` 抄行号的，而**全仓只有 Dart 和 Basic 两门在设它**。

给「语句生成有**单一入口**」的 8 门各加一句（`if (node.Line > 0) CurrentSourceLine = node.Line;`）：

| | |
|---|---|
| 加的 | Lua / Pascal / Ruby / R / Fortran / D / ObjC / Forth |
| 判据 | `> 0` —— 行号是 1-based，Line 没填的节点是 0，置 0 会把上一句的行号**冲掉** |
| 实测生效 | **d / lua / m / r / rb**（Pascal 填了 Line 但调用不走这个入口，另查） |

**产物体积没变**：注释只在**源码行变化时**发一条（`if (sl != lastSourceLine)`），
条数被源文件行数封顶、不随指令数膨胀；而且汇编器建的 program 没有 `SourceLines`，
所以 `--vml` 写出的**最终产物逐字节不变**（`ToString` 那边要 `SourceLines != null` 才发注释）。

顺带钉掉一个**潜在崩溃**：`VmlProgram.ToString` 的条件是 `SourceLine >= 0`，
而下一句是 `SourceLines[sl - 1]` ⇒ `sl == 0` 时 `SourceLines[-1]` 直接抛 IndexOutOfRange
（右边界 `sl - 1 < Length` 对 `-1` 恒真，拦不住）。改成 `>= 1`。

### ② 编辑器把 N 条错误并成 1 条 —— 「一次多报」在 UI 上原来是失效的

`LibraryLinker.ReportUnresolved` 一次会把**所有**未解析的名字列出来，形状是**混排**的：
有源码行号的写成 `<input>:12: error: …`，取不到行号的只有 `error: …`。

而 `VmlDiagnostics.Parse` 是「三条规则按序尝试、命中即停 + `return list`」——
`TryGcc` 只收**同一种形状**的匹配 ⇒ 用户看到的从 N 条掉到 1 条；
一条都没带位置时更彻底：三条规则全不命中，退化成 `FirstLine(text)` **一条**。
CLI 上 N 行照打、手机上 1 条，两端观感对不上，正是这一环。

改法：带位置的照旧（三条规则仍互斥），**再单独扫一遍无位置的裸错误行补进来**
（`^[ \t]*(error|warning|错误|警告)[:：]`，行首锚定 ⇒ 天然不会与带位置的那些重复计数）。
`提示:` 行**不进气泡**（它是提示语不是错误，进了就是每条错误后面跟一个噪声泡）。

### ③ `VmlDiagnostics` 从 MAUI 工程挪到 `UI/Shared/` —— 顺带让自测**重新跑得起来**

它是**纯逻辑**（文本进、诊断出），却长在 `WayCoder.Maui/Services/` 下，
而 MAUI 工程不进桌面自测 ⇒ **一个用例都碰不到**。按本仓对跨端纯逻辑的既定要求挪进
`UI/Shared/`（`Diagnostic` 来自 `UI/TUI/Edit/`，MAUI 本来就重新包含了那个目录），
新增 `SelfTest.Chunk25`：多错多气泡、混排、GCC 带列不重复计数、中文/英文尾缀、
空输入、warning 级别、噪声行不进气泡 —— 22 条。

**挪完才发现自测根本跑不起来**（`dotnet run -- --test` 直接 CS1593 编不过）：

```csharp
tdsl.Any((c, i) => …)   // Any 没有带下标的替身（那是 Select / Where 的）
```

只在 `WAYCODER_TEST` 下编译 ⇒ **`dotnet build`（Release）全绿、自测编不过**，
这个断点一直没人看见。改成 `Select((c, i) => …).Any(x => x)`。

修完立刻暴露出**第二条**：`防护: 越界坐标的图元整个丢弃` 断言 `DSL 行数 == 3`，
实测 4。真因是用例自己写错了参数位 —— `AddImage(x, y, path, w, h)` 的第 4 个是**宽**，
而**宽按既定语义是"钳制"不是"丢弃"**（同一批的 `防护: 超大尺寸钳到窗口` 正靠这个语义通过，
两条用例对同一件事的要求正好相反）。把越界的那个值挪回第 1 位（坐标）。

⇒ 桌面自测 **5998 / 0 失败**（此前是"编不过"，不是"全绿"）。

### 判据

| | 之前 | 之后 |
|---|---|---|
| 桌面自测 | **编不过**（CS1593） | **5998 / 0** |
| `vml-diag-probe` | 38/0/0 | 38/0/0 |
| `vml-out-probe` | 29/29 | 29/29 |
| `examples-build` | 78/3 | 78/3 |
| MAUI Android 构建 | — | 0 错误 |

剩的 3 条 examples 是已记录在案的（Fortran 格式化 print / Ladder 需 BEGIN / Forth `parserexp`）。

### 还差的一半（P3 未完）

**C 是最大的一块**：它的 `ASTNode` 是**空基类**（41 个节点类全无位置字段），
要从 parser 一路铺上去，不是加一行的事。同样没有 `Line` 的还有
C# / C++ / Go / Java / JS / Kotlin / Scheme / Swift。
Python 与 Rust 的 `ASTNode` **有** `Line`，但它们的语句生成没有单一入口，得逐个找。

---

## v0.96.282 — 三处「调了个不存在的函数」被静默放过（其中一个牵出 BASIC 块 IF 的大洞）

起因是 `vml-diag-probe` 里 `undef-fn` 组的 3 条红灯。逐条查下来**两条是假红、一条是真红**，
但真红那条往下挖，挖出的是 BASIC 一个更严重的问题。

### ① Go 那条是**用例写错了**（不是缺陷）

`cases/undef-fn.go` 里写的是 `fn main() { nosuch(1); }` —— **`fn` 不是 Go 语法**（那是 Rust/Swift）。
改用 `func main()` 一试，Go 前端**本来就报错**：

```
error: 未定义的函数 'nosuch'（引用 1 次）
```

⇒ 用例文件已订正。README 里那个「13/16」是被这条坏用例压低的，Go 那格一直是好的。

### ② JavaScript：`nosuch(1)` 生成的是**指向不存在变量的间接调用**

JS 的 `GenerateCall` 在「既不是 `func_x` 也不是 `x`」时**无条件**走「从 `var_x` 取函数地址」
那条分支，于是 `nosuch(1)` 编出来是：

```asm
        move R1 var_nosuch    ; ← 这个标签根本不存在，汇编期静默变 0
        move R0 @1            ; ← 从地址 0 读
        call R0               ; ← 调到 0 去
```

链接器**看不见**它 —— `ReportUnresolved` 只扫 `CALL`/`JMP`/`J*` 的标签操作数，
而这里的标签挂在 `MOVE` 上。于是「调了个不存在的函数」一路静默通过编译。

**判据本来就不是「找不到 `func_x`」而是「`var_x` 到底有没有」**（后者只出现在
`var f = foo; f()` 这种写法里）。改成：两者都不是时**直接 `CALL` 这个裸名**，
由链接器裁决 —— 名字对（库函数）就链上，名字错就报「未定义的函数」。
前端**不自己查**「是不是库函数」：它手里没有那张表，自己维护一张必然与链接器漂移。

### ③ Basic：裸调用没声明过的名字，**整条语句凭空消失** —— 顺带挖出块 IF 的大洞

`nosuch(1)`（不带 `CALL` 关键字）在解析器里落到 `ParseLetStatement()`，
而**没有 `=` 的 `ParseLetStatement` 返回 `null`**，语句层对 `null` 是静默跳过
⇒ 源码写了、汇编里一个字都没有。（v0.96.204 修过同类：`ui_win_open` 是
`declaredFunctions` 里的一员，那次只补上了「已声明」这一半。）

改法与 JS 同构：**名字后面不是 `=` 就一律当裸调用解析**，裁决权交给链接器。
赋值仍走 `LET` 解析（`x = 1` 左边也可能是与函数同名的变量）。

**但改完 `tetris.bas` 仍然报 `func_ty` / `func_sh`** —— 二分定位到
`ELSEIF ty > sh * 3 \ 4 THEN` 这一行。查下去发现两件事：

**(a) `ParseFunctionDeclaration` 从来没解析过返回类型 `AS <类型>`。**
参数表读完直接进「函数体」循环，于是类型名留在 token 流里当成函数体的第一个语句。
（`INTEGER` 在词法表里是 `IDENTIFIER` —— 只有首字母大写的 `Integer` 才映射到 `VB_INTEGER`。）
以前它落进 `ParseLetStatement` 被静默丢掉，表面上「没事」；现在变成 `CALL func_integer`，
`NATIVE FUNCTION f() AS INTEGER` 后面**跟任何语句都编译不过**。
已在参数表之后补上 `AS <类型>` 的解析，顺带把 `AS STRING` 接上 `IsStringFunction`
（此前只有名字带 `$` 后缀才能标记字符串返回值）。

**(b) 块式 IF 体只有第一条语句是条件执行的。** 这是本轮最重的一条：

```basic
IF a = 1 THEN
  x = 5
  y = 6
END IF
```

`a = 0` 时输出 **`0 6`**（应为 `0 0`）—— `THEN` 之后无论换不换行都只 `ParseStatement()`
收**一条**，其余全被拍平成无条件执行的兄弟语句。连带 `ELSEIF` 也废了：它只有
**紧邻**体语句时才被链状 `while` 看见，多语句体的 `ELSEIF` 落在外面
⇒ 被 `default:` 逐 token 跳过 ⇒ **整条分支变成死代码**。

修法：用 **token 行号**区分（词法里没有换行 token）—— `THEN`/`ELSE` 之后**换行 = 块式**，
新增 `ParseBlockBody()` 把语句一直收到 `ELSEIF`/`ELSE`/`END IF`；同一行则是单行 IF。
带一条防死循环兜底（解析器没推进就手工推进一格）。

验证：8 条语义用例全对（多语句体真假、多语句 ELSEIF 真假、块 ELSE、单行 IF、
单行 IF/ELSE、冒号序列 IF），嵌套块 IF 三种组合全对。

### ④ 顺带查清：`SharedPrefixMap`（auto-detect 自动链接表）在 vmlcli/MauiVml 链上是**死代码**

上一版留下的「补 `["parserexp"]` 不生效」这回查到底了。
实测方法：在 `AutoDetectSharedLibs` 入口插一句**无条件** `Console.Error.WriteLine`，
重新构建（已确认插桩字符串进了 `CompilerBase.dll`），跑 C 和 Forth 各一个 ——
**一次都没打印**。这个方法在这条链上根本没被调用。

真正决定「哪些库被链进来」的是**前端产出的 `.linked` 指令**：
GenLib 生成的 `Lib/<lang>/builtin.vml` / `builtins.vml` 写出 `.linked "x.vml"`，
汇编器解析成路径，外层再 `LinkLibraries` 链上
（`vmlcli/Program.cs` 第 ④ 步那条注释说的就是这件事）。

已在 `SharedPrefixMap` 上方写明这一点，免得后来人再往里加条目。
⚠ **本轮没有动 `parserexp`** —— 它要生效得改 `modules.json` 的 `Core` 标志，
而那是**全局**的（会进所有 22 门的 `builtin.vml`），属于另一件事，不在本次范围。

### ⑤ 撤销 v0.96.281 的 Forth 改动 —— 它切断的是链接器**既有**的一条机制

查 ④ 的过程中发现上一版改错了方向。`LibraryLinker.cs:180-183` **本来就会剥掉
`word_` 前缀再重试**：

```csharp
// 情况1b: 剥离语言前缀 (word_, func_, method_, var_) 后重试
// (如 Forth 的 word_str_to_int → str_to_int → lib_conv_str_to_int)
foreach (string knownPrefix in new[] { "word_", "func_", "method_", "var_" })
```

所以 `word_ui_call_json_s` → 剥前缀 → `ui_call_json_s`（在已链接的 `shared/vmlui.vml` 里）能解析，
`Examples/forth/sysinfo.fth` 一直是靠这条路跑通的。v0.96.281 把它换成 `forth_` ——
那是**链接器不认识的前缀**（不在剥离清单里），等于把这条机制绕过去了。

判据（回退前后各测一遍）：

| | v0.96.281 之前 | v0.96.281 | 本版（已回退） |
|---|---|---|---|
| `forth/sysinfo.fth` | ✔ | ✘ `forth_ui_call_json_s` | ✔ |
| `forth/parserexp_demo.fs` | ✘ `word_parserexp` | ✘ `forth_parserexp` | ✘ `word_parserexp` |

⇒ 已把 `VMLPrepares/ForthCompiler/` 整体回退到 v0.96.281 之前（`WordCallLabel` / `_definedWords`
全部移除），并在本版说明里记下**为什么**。`parserexp_demo.fs` 回到它原本就不通过的状态
（根因是 ④，与本条无关）。

**教训**：「某个前缀在库里找不到」的第一反应不该是**换一个前缀**，
而该先问「链接器/加载器对这个前缀有没有既定处理」—— 换前缀是在**绕开**一条已经工作的机制，
症状只会从一个地方挪到另一个地方。

### 判据

| | 之前 | 之后 |
|---|---|---|
| `vml-diag-probe`（undef-fn + link-clean） | 13/16、22/22 | **38/0/0** |
| `vml-out-probe` | 29/29 | 29/29 |
| `vml-abi-probe` | 7/7 | 7/7 |
| `examples-build` | 77/5 | **78/3** |

`examples-build` 剩的 3 条都是**已记录在案**的：`_selftest/out.f90`（Fortran 格式化 print 限制）、
`_selftest/out.ld`（Ladder 需 BEGIN）、`forth/parserexp_demo.fs`（上面 ④）。
另把 `javascript/file_io.js` 补进排除清单 —— 它与已排除的六个 `file_io.*` **是同一类**
（`asm("CALL shared_file_test")`），此前靠 ② 那条静默缺陷假绿通过。

---

## v0.96.281 — Forth 词调用：分清「本文件定义」与「库词」（命名约定核对的第一份结果）

### 核对结果：`word_` 是一张**必然漂移**的手工清单

| | |
|---|---|
| Forth 前端发 CALL | 统一 **`word_<名字>`** |
| GenLib 为 forth 生成的**主标签** | **`forth_<名字>`** |
| `word_` 别名 | 手工列了 **6 个模块**（conv/string/math/io/convert/printf） |
| `modules.json` 里的模块总数 | **74** |

⇒ 任何用到 `word_` 调用、而模块不在这 6 个里的 Forth 程序都会编不过。
`parserexp_demo.fs` 只是恰好撞上的那一个。

### 改的这一半（已验证）

按「**本文件定义的用 `word_`、库词用 `forth_`**」修 —— 与 Fortran 的 `sub_` 那条**同形**：
在 `GenerateCode` 开头预扫描 `ast.Statements`/`ast.Words` 里 `WordDefinition` 的名字
填进 `_definedWords`，两处调用点统一走 `WordCallLabel()`。

判据：调用名从 `word_parserexp` 变成了 **`forth_parserexp`** —— 那正是
`Lib/forth/parserexp.vml` 里**真实存在**的标签名（`LABEL forth_parserexp`）。
`out-probe` 29/29 全绿。

### ⚠ 但还有第二半，**没修**

改完仍然编不过：`error: 未定义的函数 'forth_parserexp'` —— 名字对了，
但 **`parserexp` 这个模块压根没被 auto-link 拉进来**。

试过补 `["parserexp"] = "parserexp"` 映射，**不生效**，已按「没修好就先撤」撤回。

⇒ **auto-link 映射表是第三张手工清单**，而且是同一个病。今天三条同族问题
（`ui_` 缺映射、Fortran 的 `sub_`、Forth 的 `word_`）背后是**三张各自维护的命名表**：
各前端的调用前缀、GenLib 的 `naming.json`、链接器的 auto-link 映射。
**该做的是一次把这三张对齐**（`modules.json` 有 74 个模块，靠人手维护的别名/映射必然漏），
而不是继续逐个撞。

## v0.96.280 — Forth 补登记 `.fs` 扩展名；并记下它后面藏着的那条真问题

### 改的是一行

`ForthCompilerPlugin.SupportedExtensions` 原先只有 `.fth,.forth`，而 **`.fs` 是 gforth 的默认扩展名**
（`gforth foo.fs`），也是最常见的一种 ⇒ `Examples/forth/parserexp_demo.fs` 被 CLI 判成
「认不出这个扩展名」而拒绝编译。**不是语言的问题，是派发表少一条。**

### 但补上之后，真问题立刻浮出来了

```
error: 未定义的函数 'word_parserexp'（引用 3 次）
```

`parserexp` 是 `Lib/forth/parserexp.vml` 里的一个词，而那个文件里定义的是
**`LABEL forth_parserexp`**（GenLib 给 Forth 用的前缀是 `forth_`），
前端发出的却是 **`word_parserexp`**（Forth 编译器自己的前缀是 `word_`）。

⇒ 又是一处**两边前缀约定不一致**，而且这个模块**也没被 auto-link 拉进来**
（映射表里没有 `parserexp` 这一条）。

**这是今天遇到的第三次同族问题**（前两次：`ui_` 缺映射、Fortran 的 `sub_` 前缀）。
它们的共同形状是：**前端发 CALL 时用的名字，和库里实际定义的名字，靠两套各自维护的
命名约定去对**，任何一边漏一条就变成"未定义的函数"。

**未修**：判断哪个前缀才是"对的"要看 Forth 前端与 GenLib 各自怎么命名词
（`word_` 还是 `forth_`），需要一次专门的核对 —— 不能照着症状在映射表里加一条了事
（前两次已经证明那样只会绕过问题）。

### 判据

`.fs` 现在能派发到 Forth 前端（错误从"认不出扩展名"变成了上面那条真实的链接错误）。
`examples-build.sh` 仍是 79 / 3 —— 因为这一条把问题**推进了**、还没修完。

## v0.96.279 — Fortran 调用点的 `sub_` 前缀：**P2 的误报清零**

### 现象

`Examples/fortran/sysinfo.f90` 调 `call ui_call_json_s('sysinfo','')`，P2 之后报
`error: 未定义的函数 'sub_ui_call_json_s'`。

### 真身

Fortran 的**定义端**一律编成 `sub_<名字>`（`GenerateSubroutine`），而**调用点也无条件加前缀**
（`GenerateCall` 里写死 `$"sub_{fname}"`）。自己人之间调用没问题 —— **但库函数是裸名**
（`Lib/shared/vmlui.vml` 里就叫 `ui_call_json_s`）⇒ 编出 `CALL sub_ui_call_json_s`，那个标签永远不存在。

### 修法：判据用现成的预扫描

`GenerateCode` 开头**已经**有一次预扫描把本文件定义的所有子程序/函数收进 `_functionTable`
（含 module 里的）—— 直接用它：

```csharp
bool isUserDefined = _functionTable.ContainsKey(fname.ToLowerInvariant());
string funcLabel = isUserDefined ? $"sub_{fname}" : fname;
```

名字转小写与定义端一致（定义处用的是 `node.Name.ToLowerInvariant()`）。

### 之前试过、**不生效**的两条（记下来免得重走）

1. 补 `["sub_ui_"] = "vmlui"` 映射 —— 不生效；
2. 把 `sub_` 加进链接器的「已知内部前缀」表（让候选人被剥成 `ui_call_json_s` 去匹配）—— **也不生效**。

真身比"缺一条映射"深一层：**剥离前缀只用于「决定链哪个模块」，不改写 CALL 目标**。
链进来也没用，因为那条 `CALL` 的字面量就是错的。**问题在前端，不在链接器。**

### 结果：P2 的 9 个误报**全部清零**

`examples-build.sh`：**通过 79 / 失败 3**（起点是 12）。剩下 3 个**都不是 P2 误报**：

| 失败项 | 性质 |
|---|---|
| `_selftest/out.f90` | Fortran 不支持格式化 `print '(A,I0)'` —— 既有语言限制 |
| `_selftest/out.ld` | Ladder 要 `BEGIN` —— 探针文件不是 Ladder 语法 |
| `forth/parserexp_demo.fs` | `.fs` 扩展名没注册 |

`out-probe` 29/29、`abi-probe` 7/7 全绿。

## v0.96.278 — R 的 `break` / `next` 指向一个**从来没落过的标签**（P2 误报第 2 个）

### 现象

```r
i <- 0
while (i < 10) { i <- i + 1; if (i == 5) { break } }
print(i)
```
P2 之后编译报 `error: 未定义的函数 'wend_0'`。

### 真身

`GenerateWhile` 把 `currentBreakLabel = $"wend_{n}"` / `currentNextLabel = $"while_{n}"` 设好，
`GenerateBreak`/`GenerateNext` 也确实发 `JMP wend_N` —— 但**全 R 前端没有任何一处 `LABEL wend_N`**。
两个标签只被赋值、从来没被落点。

对照：`repeat` 是**对的**（`AddLabel(startLabel)` + `AddLabel(endLabel)`），
`for` 也是对的（`currentBreakLabel = endLabel` 且那个 `endLabel` 落了）。**只有 `while` 漏了。**

`StatementManager` 里那套 `_loopStack`（`EmitWhile` 自己会 push）在这条路上用不上 ——
它**根本没有 `EmitBreak`/`EmitContinue`**，R 走的是自己这条 `currentBreakLabel` 通路。

### 修法

按 R 的语义落点：`next` 跳回**循环体开头**（回去重新判条件），`break` 跳**循环之后**。
用同文件已有的 `AddLabel` 写法（与 `repeat` 一致 —— 同一件事两种写法正是本仓的坑）。

### 一条方法论教训（我在这条上绕了两圈）

第一轮 grep `wend_` **什么都没找到**，我据此写下「还没找到引用方」。
**那个结论是错的**：标签名是 `$"wend_{labelCounter++}"` **插值拼**出来的，
字面量 `"wend_"` 只出现在**赋值那一行**；而"读"它的 `GenerateBreak` 里写的是**变量名**
（`currentBreakLabel`），我第二次 grep 又加了过滤条件把这几行排掉了。

⇒ **"grep 没命中"不等于"不存在"**。换个搜法（按 AST 节点名 `BreakNode` 找生成函数）一眼就看到了。

### 判据

`i` 停在 **5** ✓；`Examples/r/catch.r` 编译通过；`out-probe` 29/29。

## v0.96.277 — 运行面板的关闭键：贴右 + 与栏融为一体

用户两条：① 关闭键**靠右对齐**（位置好找）；② **去掉外框和底色**，与栏融为一体。

- **右对齐**：那一行原本是 `HorizontalStackLayout` —— Stack **没有**"把最后一个推到最右"
  这种能力，所以换成 `Grid` + 一列 `*` 空档（`Auto,Auto,Auto,*,Auto`）。
  `■ 停止` 仍在 ✕ 左边（只是它平时不可见）。
  ⚠ 三个按钮都要**显式写 `Grid.Column`** —— 不写就全挤在第 0 列叠在一起。
- **去外框**：`BorderWidth="0"` **必须显式写**。外框**不是来自 `PanelChip`**（那里底色本来
  就是 `Transparent`），而是来自 App 的**全局 Button 隐式样式**（`Styles.xaml` 的
  `BorderWidth=1` + `MinimumHeight/WidthRequest=44`）—— 局部样式压不住它。
  `VerticalOptions="Center"` 同理：不写会被拉满整行高度。

## v0.96.276 — 编辑器诊断气泡：按字数折行 + **永远摆在行下方**

用户定的两条规矩：
1. 「默认气泡是一行字，如果一行超过 32 字符就换行，不用考虑是否超出屏幕，反正屏幕可以滑动，
   32 这个标准可以在设置里面设定，最少 16，最大 1024」
2. 「气泡位置要永远在指示行下方，因为在上方第一行就超出了，看不见了」

### ① 折行：**字数定死，宽度由它推**（原来是反的）

原先气泡宽度是 `min(300, 画布宽−16)` —— **宽度由屏幕定**，每行几个字再由宽度反推
（`宽 / 字号×0.62`）⇒ "一行多少字"随屏幕与字号漂移。
现在反过来：**字数由设置定死**，宽度 = 字数 × 半角字宽 + 内边距。

这与"不看屏幕"是同一件事的两面：只要宽度还受画布约束，"每行 N 字"就随时可能被挤掉。

**折行自己做**（`WrapByColumns`），不交给 `Label`：MAUI 的折行是**按控件宽度**算的，
而用户要的是**按字符数**。两者只在字宽均匀时才等价，而气泡里常混中英文 ——
交给平台折，换行位置会随字号/字体/取整各处漂移，"每行 32 字"这条规矩根本立不住。
配套 `LineBreakMode = NoWrap`（否则我们算好的换行会被平台按宽度重排）。

- 宽度真源用 `AnsiString.CharWidth`（全仓唯一那份），**按码点遍历**（`EnumerateRunes`）——
  emoji/扩展 B 是代理对，逐 `char` 走会拆开它们，而且 `CharWidth` 本来就只收 `Rune`。
- 超过 `BubbleMaxLines` 在这里截断加省略号：`NoWrap` 之后 `MaxLines` 管不住我们插入的 `\n`，
  不截的话文字会溢出边框。

### ② 位置：**永远在行下方**（原先是上方）

原先摆上方，理由是"盖住已经读过的那一行、不挡接着要读的"。**但那个理由只在错误行不在屏幕顶端时成立** ——
行一靠近顶端，`anchorY − 气泡高` 就把整条顶到可视区之外，**第一行直接看不见**，
而第一行正是"第几行、什么错"。现在 `by = anchorY + 行高 + 间距`，尾巴改为**尖朝上**。

### ③ 设置项

`MauiEditorStore.BubbleChars`：默认 32、范围 16–1024、落盘 `editor.json`、纯函数 `ClampBubbleChars` 一处夹取。
设置页「编辑器」卡片加一个数字输入框，**失焦与回车都走同一个处理器**，且**夹取后回写**成真实值 ——
不回写的话用户输入 `5` 之后框里留着 5、实际生效 16，两边对不上，而"设置没生效"这种印象最难查。

⚠ 缺键时用**默认值**而不是 0（老版本升上来的 `editor.json` 没有这个键，
写成裸 `GetNumber` 会得到 0、再被夹成 16 ⇒ 界面"自己变窄了"而用户根本没改过这项）。

## v0.96.275 — Examples 判据转正一半：12 → 5；记两条**没修成**的

### 修好的（已验证）

- **`lua_table_` → `luatable`**：`Lib/lua/luatable.vml` 定义着 `lua_table_set`/`lua_table_get`，
  而映射表一条都没有 ⇒ `Examples/lua/life.lua` 编不过。补上即通。
- **`Examples/*/file_io.*` 从判据里显式排除**（csharp/java/ruby/r/swift/objc 六个）：
  它们是 **SharedLib 演示存根**，内容是 `asm("CALL shared_file_test")` + `asm("LOAD R0 #100")`
  + `asm("SYSCALL 3")`。**两个理由都不该修成能编**：
  ① 新版**只在 C 类语言保留内嵌汇编**，其余语言取消（改为"只能调 C 写好的库"）
     ⇒ `asm("…")` 在这些语言上本来就不是支持的能力了；
  ② 被调的 `shared_file_test` **在整个仓库里没有任何地方定义**。
  **留着当红灯只会训练人去忽略红灯** —— 排除比让它常年红着诚实。

⇒ `examples-build.sh`：**通过 77 / 失败 5**（原 12）。

### 两条**没修成、已撤回**的（记下来免得下次重走）

**① Fortran 的 `sub_ui_call_json_s`** —— 补 `["sub_ui_"] = "vmlui"` **不生效**；
换成正解（把 `sub_` 加进「已知内部前缀」表让候选人被剥成 `ui_call_json_s`）**也不生效**。

真身比"映射缺一条"深一层：**剥离前缀只用于「决定链哪个模块」，不改写 CALL 目标**。
Fortran 发的是 `CALL sub_ui_call_json_s`，而 `shared/vmlui.vml` 定义的是 `ui_call_json_s`
—— 名字对不上。所以就算把 vmlui 链进来，那条 CALL 依然解析不了。
⇒ 要修得动 **Fortran 的调用命名**或**给 Fortran 侧加别名**，是另一块活。

两条改动**都撤回了** —— 按本仓的规矩「**没修好就先撤，别留一个说不清效果的改动**」
（`EndsWith("_itoa")` 那次教训）。

### 剩余 5 个

| 失败项 | 性质 |
|---|---|
| `fortran/sysinfo.f90` | 上面那条，**未修** |
| `r/catch.r`（`wend_56`） | 名字带序号 ⇒ 代码生成器拼的标签，另一类，**未查** |
| `forth/parserexp_demo.fs` | `.fs` 扩展名没注册（前端扩展名表缺一条） |
| `_selftest/out.f90` / `out.ld` | 既有语言限制（Fortran 不支持格式化 `print` / Ladder 缺 `BEGIN`），**不是缺陷** |

### 回归

`out-probe` 29/29 全绿。

## v0.96.274 — 补 `lua_table_` 映射；订正上一版的数字

### 订正

v0.96.273 里我写「挖出 7 个 P2 误报」——**实际是 9 个**。权威数字来自加了排除项之后的完整跑：

```
通过 76 / 失败 12
失败：_selftest/out.f90 _selftest/out.ld csharp/file_io.cs forth/parserexp_demo.fs
      fortran/sysinfo.f90 java/file_io.java lua/life.lua objc/file_io.m
      r/catch.r r/file_io.r ruby/file_io.rb swift/file_io.swift
```

12 减去 3 个非 P2 项（`out.f90` 是 Fortran 不支持格式化 `print`、
`out.ld` 缺 `BEGIN`、`parserexp_demo.fs` 是扩展名没注册）⇒ **P2 误报 9 个**。

### 修掉第一个：`lua_table_` → `luatable`

`Lib/lua/luatable.vml` 里定义着 `lua_table_set` / `lua_table_get`，而 auto-link 映射表里
**一条 `lua_table_` 都没有** ⇒ `Examples/lua/life.lua` 编不过。补上，已验证通过。

### 剩下 8 个的形状（**还没修**，已查过一部分）

| 例子 | 未定义 | 初查结论 |
|---|---|---|
| `csharp/file_io.cs`、`swift/file_io.swift` | `asm` | **库里查不到这个名字** ⇒ 可能不是映射缺口，是例子依赖了某个没被链进来的模块，或前端把关键字/内建编成了 CALL —— 待查 |
| `java/file_io.java`、`ruby/file_io.rb`、`r/file_io.r` | `method_asm` / `func_asm` | 同上，只是各语言的前缀不同 |
| `fortran/sysinfo.f90` | `sub_ui_call_json_s` | **Fortran 的前缀是 `sub_`** ⇒ 光加 `["ui_"]` 不够，要按前缀再补一条 |
| `r/catch.r` | `wend_56` | 库里有 `wend`？带后缀 `_56` 说明是**代码生成器按标签序号拼出来的**，另一类问题 |
| `objc/file_io.m` | `shared_file_test` | `SharedLib/` 那个例子的助手函数，可能本就该由例子自己提供 |

⇒ **P2 的硬错误在这几门语言上对真实例子仍不安全**。修完之前，`examples-build.sh` 就是这条的红灯。

## v0.96.273 — 补一层语料：`Examples/` 全量编译检查（**当场挖出 7 个 P2 误报**）

### 为什么补这一层

`vml-out-probe` 是**每门语言一条十几行的最小程序**。把「未定义函数」从警告升成编译期硬错误
（v0.96.268）之后，那套 29/29 全绿、`link-clean` 22/22 全绿，看着是"零误伤" ——
**可它只在那一层成立**。换成真实例子，当场炸出一批：

```
csharp/file_io.cs     未定义的函数 'asm'（引用 3 次）
java/file_io.java     未定义的函数 'method_asm'（引用 3 次）
ruby/file_io.rb       未定义的函数 'func_asm'（引用 3 次）
r/file_io.r           未定义的函数 'func_asm'（引用 3 次）
r/catch.r             未定义的函数 'wend_56'（引用 2 次）
swift/file_io.swift   未定义的函数 'asm'（引用 3 次）
lua/life.lua          未定义的函数 'lua_table_set'（8 次）/ 'lua_table_get'（12 次）
objc/file_io.m        未定义的函数 'shared_file_test'（引用 1 次）
fortran/sysinfo.f90   未定义的函数 'sub_ui_call_json_s'（引用 1 次）
```

全是同一族：**库函数名在 auto-link 映射表里缺一条**（`asm` 系、`lua_table_*`、
`sub_ui_*`（Fortran 的前缀版本，单纯加 `ui_` 不够）、`wend_*`、`shared_*`）。
`Examples/kotlin` 那个 `ui_*` 只是先冒出来的一个。

⇒ **语料只铺一层，结论就只在那一层成立。**

### 新增 `scripts/vml-diag-probe/examples-build.sh`

把 `Examples/<语言>/*` 每个源文件编译一遍，只判"能不能编过"（不跑 —— 很多是交互式游戏）。
排除非源码（`.md`/`.h`/`.json`/脚本）与 **`.vml`**（那是**已经编好的汇编**，不是源码，
喂给前端只会得到"认不出扩展名"，第一版就是这么误报了两个）。

### ⚠ 写这个脚本时踩的坑：**`timeout` 会给出"全通过"的假绿**

第一版用了 `timeout 60 dotnet …`，而 **macOS 没有 `timeout`**（GNU coreutils 的）⇒
整条命令 `command not found` ⇒ 输出为空 ⇒ **每个例子都被算成"通过"**，
报出「通过 90 / 失败 0」。去掉 `timeout` 重跑才看到 **14 个失败**。

这与 `vml-out-probe/run-langs.sh` 头部记的那个坑**同源但方向相反**：
那里是"全报错"（28 条一起 FAIL），这里是**"全通过"** ——
后者更危险，因为它给的是**假的信心**。两个脚本的注释里都钉住了这一条。

## v0.96.272 — Kotlin 未声明变量 + **修掉 P2 的一处误报**（P6 第二门）

### ① Kotlin：五条分支全落空 = 未声明（顺带修掉"读回残值"）

`case VarRef` 四条分支（`__when_val__` / 局部槽 / 顶层属性 / `this` 的字段）全落空时，
此前**什么都不发**、直接掉到 `break` —— 比另外 15 门的"建个初值 0 的槽"更隐蔽：
**R0 保留上一条指令的残值**，读出来的是**上一次运算的结果**，
看着像个"有时对有时不对"的随机 bug。现在补第五支：报「未声明的变量」+ 发 `MOVE R0,#0`。

实测一个文件里写错两个名字，**一次报两条**，而且带**真实文件名**。

### ② ⚠ P2（未定义函数硬错误）漏掉的一处误报 —— 被例子抓到，不是被语料抓到

`Examples/kotlin/sysinfo.kt` / `catch.kt` 在 P2 之后**编不过了**：

```
error: 未定义的函数 'ui_call_json_s'（引用 1 次）
error: 未定义的函数 'ui_call_json_print'（引用 1 次）
```

真身：`ui_*`（绘图/窗口那一套宿主接口，实现在 `Lib/shared/vmlui.c`）**不在 auto-link 映射表里**
⇒ 任何**直接**引用它们的程序都链不到 `vmlui`：

- **C 侥幸躲过** —— `waycoder_ui.h` 里有一句显式的链接指令；
- **Kotlin 躲不过**（例子直接调 `ui_call_json_s`）；
- 而 P2 之前这条只是"链接期警告"、**运行到才崩**，所以谁都没发现；
  P2 把它升成**编译期硬错误**之后，这两个例子当场编不过。

补一条 `["ui_"] = "vmlui"` 前缀映射即可。

**教训**：我的验证语料是 `vml-out-probe`（22 门各一条最小程序），它**没覆盖 `Examples/`** ——
所以"用户档 22 门全 0"这个结论只在那套语料上成立。**升硬错误这种改动，语料要多铺一层。**

### 回归

`out-probe` 29/29、`abi-probe` 7/7、`Examples/c/*.c` 九个例子、`Examples/kotlin/*.kt`、
`drift.kt` 全部编译通过。

## v0.96.271 — P6 第一门：C 的未声明标识符改成**一次全报**

### 改了什么

C 前端此前有 **5 处** `throw new CodeGenerationException(CodeGen_UndefinedVariable, ...)`
（赋值左值、取址、数组下标、复合赋值目标…），**遇到第一个就抛** ⇒ 用户一次只看到一个。
现在统一走 `NoteUndefinedVariable()`：**记一条诊断 + 发个 `MOVE R0,#0` 占位 + 继续生成**，
最后在 `CodeGeneratorBase.BuildProgram` 一次性抛出（P4 建好的收口）。

```c
int main(void) { int a; a=1; b=2; c=3; return nosuch(a); }
```

| | 之前 | 现在 |
|---|---|---|
| 报错 | `未定义的变量: b`（1 条） | `未声明的变量 'b'` + `'c'`（**2 条**） |

那个占位值不是随便糊的：不发的话后面的代码会拿上一条指令留在 R0 里的残值继续算，
很容易级联出一串**假**错误把真问题淹掉。

顺带统一了文案与错误码（`未定义的变量: x` → `未声明的变量 'x'`，与基类 `ReportUndefined` 同源），
并给基类加了三个原语：`Diags`（生成器自持诊断袋）、`ReportUndefined`、
`EmitUndefinedFallback`、`ImplicitDeclarationAllowed`（动态语言的豁免开关，**一个虚拟属性**
而不是在 16 处散写 `if (lang != "python")`）。

### 一个**估算错了**的地方，如实记下来

方案里写的是「18 门前端补 `CurrentSourceLine`，每门一句 `CurrentSourceLine = node.Line`」——
**对 C 不成立**：它的 `ASTNode` 是个**空基类**，一个位置字段都没有
（`Token` 上倒是有 `Line`/`Column`，但解析器没往 AST 上带）。
所以「C 的报错带行号」需要先给 C 的 AST 补位置信息并在各构造点填上，是**独立的一块活**。

**取不到就不显示**，不编造：`CompilerError.LocationString` 在 `Line <= 0` 时退化成
`file: error: …`（GCC 里"位置未知"的标准写法）。别写成 `file:-1:0:` ——
编辑器的位置解析器对 `line <= 0` 是**直接跳过**的，结果是**几条错误被合成一个气泡**，
正好把"一次多报"毁掉。

### 回归

`out-probe` 29/29 全绿。

## v0.96.270 — 诊断管道打通：**一次多报**真的生效了（P4）

用户要求：「要尽量一次多报些错误，现在运行就报一个错误」。

### 实测对比

```lua
print("abc          -- 未终止的字符串
```

| | 之前 | 现在 |
|---|---|---|
| Lua | **静默编译通过**（收集到的错误被扔掉） | `编译失败`，**一次两条** |

现在的输出：
```
<input>:2:1: error: 未终止的字符串字面量，缺少闭合引号 '"' [Lexer_UnterminatedString]
.../unterminated.lua:0:0: error: 期望 ')'
2 error(s) generated.
```

**两条**是"词法阶段收集到的" + "语法阶段抛出来的"**并成一份** —— 而以前这两条是**二选一**，
而且成功路径干脆两条都不要。

### 四处改动

**① `DiagnosticBag` 补四样**（`CompilerBase/DiagnosticBag.cs`）
- **去重键**（`(code, file, line, col, message)`）：同一条诊断被两条路径重复添加是**常态**
  （C 前端四个 `throw` 点、将来"收集并继续"之后同一句被扫到两次），不去重的话
  50 条上限会被同一条消息瞬间吃光、真正其它的错反而被挤掉。
- **`TooManyErrors` 标志**：原先到上限只是插一条哨兵然后静默 `return` ——
  调用方**分不出**「刚好 50 条」和「还有 200 条没报」，而"被截断了"恰恰是用户必须知道的。
  `FormatAll` 现在会补一句「（错误太多，只报了前 N 条）」。
- **`FirstErrorCode`**：`CompilationException` 需要一个 code。
- **`Merge(bag)`**：把异常路径那一条并进同一份输出。

**② `CompileWithDiagnostics` 补三处**（`CompilerHelper`）
1. **成功路径也看 bag** —— 就是它修掉了上面那个"未终止字符串静默编译通过"的既有 bug；
2. `catch (ParseException)` / `catch (CodeGenerationException)`：把抛出来的那条
   **并进 bag 再抛**（原来是 `HasErrors ? FormatAll() : 单条` 的**二选一**，
   于是"之前收集到的"在"有抛出"时反而不见了）。

**③ `CodeGeneratorBase.BuildProgram` 收口** —— **杠杆最大的一处**：
它是全部 22 门在 `GenerateCode()` 末尾都会调的，且对 `Compile`/`CompileFile`/
`CompileFileWithIncludes` **三条入口全部生效**，不用碰任何一门语言的入口代码。
配合新加的**生成器自持**诊断袋 `Diags`（22 个 `CompileFile` 入口没有一个会把 bag 传进 codegen，
外部注入要改 22 个文件；自持则**改零个入口**）——
P6 的逐门未定义变量检查只要往 `Diags` 里写，收口是现成的。

⚠ 踩到一个命名坑：`DiagnosticBag` 不能写成 `CompilerBase.DiagnosticBag` ——
那个命名空间里正好有一个**叫 `CompilerBase` 的类**，限定名会被解析到它上面（CS0426）。

### 回归

`out-probe` 29/29、`abi-probe` 7/7、`link-clean` 22/22 —— 全绿。

## v0.96.269 — 诊断带上**源码行号**（P3：汇编器半边）

用户要求：「所有错误尽量按照标准输出行列号，可以用来在 IDE 标注错误位置」。

### 这条链路查下来是**断在最上游**的

| 环节 | 状态 |
|---|---|
| `VmlProgram.ToString` 写 `; 12: <源码行>` | ✅ 逻辑早就有（`:509`，要求 `SourceLine >= 0`） |
| `CodeGeneratorBase` 的指令列表在 `Add` 时按 `CurrentSourceLine` 设 `SourceLine` | ✅ 逻辑早就有（`:25-26`） |
| **前端把 `CurrentSourceLine` 设上** | ❌ **全仓只有 4 处**（Dart 1、Basic 3）⇒ 其余 18 门 `SourceLine` 恒为 -1 |
| **汇编器把 `; N:` 读回来** | ❌ 此前把所有 `;` 行整个 `continue` 掉 ⇒ `Instruction.SourceLine` 永远是 -1 |
| 链接器报错带行号 | ❌ 只能给名字 |

**这一版补的是倒数第二环**（汇编器），它不补的话，将来前端把行号设上了也接不上。

### 改了什么

- `VMLAssembler` 主解析循环：认 `^\s*;\s*(\d+):` 这种注释，把行号记下来赋给随后那条指令的
  `SourceLine`。只认这一个形态 —— `; ----`、`; source : …`、`; 参数说明` 都不匹配，不会被误当行号。
- `LibraryLinker.ReportUnresolved`：用户档的每条错误改成 **GCC 风格的 `文件:行: error: 消息`**

```
<input>:3: error: 未定义的函数 'nosuch'（引用 1 次）
```

文件名是占位符 `<input>`（链接器看不到源文件名，`VmlProgram` 没有这个字段）；宿主知道真实路径。
**这个格式正是 `VmlMaui/Services/VmlDiagnostics` 已经在解析的那种** —— 它有 4 条正则、
且是**遍历全部匹配**的 ⇒ 一次多报在 UI 上才真的会变成多个气泡。

### 剩下的一环（并入逐门前端的工作）

**18 门前端要把 `CurrentSourceLine` 设上** —— 每门一句（在语句生成入口写 `node.Line`），
与 P6 的「未定义变量」是同一次逐门改动，合并做。在它完成之前，报错会退化成"只报名字"（不显示行号），
**不会报错的行号是错的** —— 取不到就不显示，不编造。

## v0.96.268 — 未定义函数升级为**编译期硬错误**（P2：一处机制覆盖 22 门）

用户要求：「没有声明的变量或者函数，编译就应该报错，不然我现在明明有无效标识，
非要等到运行才报错」。

### 修之前是什么样

```c
return nosuch(1);     /* 编译通过 → 运行到那一条 CALL 才抛
                         KeyNotFoundException: 未找到标签: nosuch
                         —— 而且是**一次只报一个** */
```

### 改法

`LibraryLinker.ReportUnresolved` 里那条**用户档**从 `Console.Error.WriteLine` 改成
`throw new UnresolvedSymbolException(...)`，异常文本**一次列出所有**没定义的名字：

```
错误: 有 2 个函数**没有定义**（也没有在任何库里找到）：
  nosuch（引用 1 次）
  other（引用 3 次）
提示: 检查函数名拼写；库函数要在源码里 #include 对应头文件，或确认该模块在语言库里存在。
```

**为什么现在才敢升档**：这条以前只能当警告 —— 库里还有一批历史遗留的死包装器
（目标函数真实、只是没被 auto-link 拉进来），混在一起报就永远升不了档。
P1 把两档分开之后实测**用户档 22 门全是 0**，前提这才满足。

### 顺带把"报错形态"收了

新加的这个异常会穿过三条不同的路径，逐个收拾了：

| 路径 | 之前 | 现在 |
|---|---|---|
| `CompilerPluginBase` / `CompilerHelper` 的兜底 `catch (Exception)` | 被标成 `internal error:` —— 一个拼错的函数名报成"内部错误"，用户完全不知道该改哪里 | 走 `CodeGen_UndefinedFunction`，且**排在兜底 catch 之前** |
| `CompilerPluginExBase.CompileFileWithIncludes`（Pascal 等注入委托的语言） | 裸 `Unhandled exception` + 堆栈 | 统一翻成 `CompilationException`（放在**委托调用这一层**，不是让 22 个前端各改一遍） |
| `scripts/vmlcli` 的编译步与**链接步** | 顶层只接 `CliArgumentException` ⇒ 任何编译错误都是裸堆栈 | 两处各包 try → `⚠️ 编译失败：<消息>`。**链接步那一处特别要紧**：Pascal/Forth/Ladder/Basic **不在自己的 `CompileFileWithIncludes` 里链接**，是在 CLI 这一步做的 |

### 判据

`scripts/vml-diag-probe/`（P0 建的那套）加了 `undef-fn` 组：**13/16 通过**。

三门红着的是**待办信号，不是回归**：
- **Go / BASIC 把整句调用丢掉了** —— 生成的汇编里连 `nosuch` 都没有（`grep -c nosuch` = 0）。
  这比"发一个不存在的标签"更隐蔽：后者至少在链接期冒出来。属逐门前端的工作（P6+）。
- JS 待查。

回归：`out-probe` 29/29、`abi-probe` 7/7、`diag-probe link-clean` 22/22 全绿
（**硬错误没有误伤任何一个正常程序** —— 这是这次最关键的一条验证）。

## v0.96.267 — 编译期未定义标识符（P0+P1）：判据骨架 + 库侧清源

用户要求：「大部分语言，没有声明的变量或者函数，编译就应该报错……而且要尽量一次多报些错误」。
分阶段做，这一版是 **P0（判据骨架）+ P1（库侧清源）**。

### P0：新建 `scripts/vml-diag-probe/`

与 `vml-out-probe` **极性相反**的一套判据（那套是"必须编译成功且输出正确"，这套是"必须编译失败"）。
分开是刻意的 —— `run-langs.sh` 自己的注释里就记着当年把「编译期抛异常」和「输出不对」
混成一档的教训。

**三档判定**：`PASS`（非 0 退出 + 点名符号）/ `FAIL`（编译过了）/ **`CRASH` 单列**
—— 最后这档正是"修复没生效、只是换个地方崩"的伪装形态（今天的行为就是
「编译通过 → 运行期 `KeyNotFoundException: 未找到标签`」，只能判"没编过"的话它会冒充 PASS）。

### P1：`Lib/modules.json` 里 56 个**虚构函数名**

正常程序编译时链接器会报一堆「未解析标签」（bas 60 / cs 49 / ld 49 / pas 49）。
追下去真身不在生成器、在**输入**：`modules.json` 里有一批函数名在 `Lib/shared/src/*.c`
里根本不存在（`Sector` / `Arrays` / `CMD_BUF` / `Font` / `Manipulation` …），
而 `GenModules` 对 `funcMap` 查不到的名字走的是**编造签名的兜底分支** ⇒ 生成出
`LABEL c_Sector` + `CALL Sector` 这种死包装器。

**三处一起改**：
- `tools/GenLib/Program.cs`：查不到的函数**不再编造签名**，改为跳过 + 累计报告
  （「宁可不生成也不静默生成一个死包装器」）；别名段**同样跳过**（否则 `LABEL func_Arrays / JMP c_Arrays`
  会让未解析换个更隐蔽的形式继续存在）；整模块无 LABEL 时删掉已生成的**孤儿产物**
  ⚠ 判据是「既没有 LABEL、**也没有 `.linked`**」—— 第一版只看 LABEL，把 `util`/`syscall`
  这两个**聚合模块**也删了，而 `Lib/<lang>/builtin.vml:29` 正写着 `.linked "util.vml"`。
- `Lib/modules.json`：删掉那 56 个条目（用 GenLib 自己的报告当唯一真源，不另写一份判据）；
  `syscall`/`util` 的 `Functions` 清空但**模块本身保留**（它们是 re-export 共享模块的聚合壳）。
- `VMLAssembler/LibraryLinker.cs`：`ReportUnresolved` 加**两档分档**（`LinkLibraries` 入口记
  `userEnd` 索引边界）：用户代码里的未解析 = 真的写错函数名（**将来升硬错误**）；
  库代码里的 = 历史遗留死包装器（另立账）。

**为什么用索引边界而不是名字前缀**：链接器会**主动给库标签造裸别名**，同一个符号两种写法；
模块名自带下划线（`lib_printf__printf_itoa`）——「按名字猜来源」本仓已经付过一次代价
（`EndsWith("_itoa")` 那起 `printf` 事故）。可达性分析则会漏报用户代码里的死分支，
且对没有 `main` 的程序（Pascal 单元、GenLib 编共享库）整个失效。

### 结果

- **用户档 22 门全部为 0** —— 这是「未定义函数升级成编译期硬错误」的前提，已满足。
- 库档从 60/49/49/49 降到 **33/32/32/32**（只剩 bas/cs/ld/pas 四门）。
  降不下去的那部分是**目标真实、只是没被 auto-link 拉进来**的死包装器
  （`Lib/pascal/console.vml` 的 `PASCAL_PRINT_LONG` 调 `call print_long`，而 `io64` 没链上），
  归 Tier 2 待办。
- 判据：`vml-diag-probe link-clean` 22/22、`vml-out-probe` 29/29、`vml-abi-probe` 7/7 全绿
  （462 个库文件被重新生成，无回归）。

### 顺手记一条盲区

`check-vml-patches.sh` 的两条判据**都抓不到孤儿产物**：判据①只比对"补丁该产出的文件"、
判据②a 只比对"GenLib 写过的文件" —— 一个**不再被生成器写、却还躺在盘上**的文件，
两边都不覆盖。这正是"陈旧产物"长出来的方式，已写进生成器注释。

## v0.96.266 — 移动端：辅助输入条的**弹出 / 收回**动画

用户要求「打开是从图标位置图标大小弹出到打开位置和正常大小，关闭动画就反过来」。

### 做法

`AssistPopStart()` 算出**同一组起手状态**（位置 + 缩放），开与关共用：
- **位置**：缩完的条子**中心**压在工具栏那颗图标（`AssistBtn`）的中心上 ——
  先各自换算到页面坐标再作差，不能拿两者的 `X/Y` 直接减（它们不在同一个坐标系里）。
- **大小**：按**面积**折算成等比缩放 `sqrt(图标面积 / 条子面积)`。这条子又长又扁 ——
  按宽度比会小成一根线，按高度比反倒比原图还大，只有面积比既保住长宽比又确实"跟图标差不多大"。
- **钳进画布范围**：图标在工具条**最右一格**，照它居中会顶出屏幕右缘；
  纵向更是整颗都在条子容器的上方。钳完起手位落在"工具条正下方那一角"，
  仍看得出是从那颗按钮出来的，又不会被画到没有画布的地方。

开：`CubicOut` 从起手位弹到停靠位；关：`CubicIn` **倒着放一遍**，演完才真隐藏。

### 三条必须记住的

① **停靠位要单独存（`_assistRestX/Y`），不能拿 `TranslationX/Y` 当它** ——
   动画期间那两个字段是"路过"的值，下一次打开就从半路上起飞、位置一路漂。
   `ClampAssistIntoView` 里统一同步（拖动每帧都过那里）。

② **不能一律挂在 `SizeChanged` 上等**：`SizeChanged` 只在尺寸**变了**才发，
   第二次打开尺寸没变 ⇒ 永远等不到、条子停在 `Opacity=0` 上等于没开。
   所以只有"还没量过"（`Width <= 0`）才等，已量到就当场起动画。

③ **打开那条路上不能调 `AssistTouch()`** —— 它内含 `RestoreAssistScale()`，
   会把 `Scale/Opacity` 立刻动画到 1/1，与刚起头的弹出动画抢同一个属性。
   拆出一个只管计时器的 `RestartAssistIdle()`。

关闭演到一半又被打开时（`_assistHiding` 被清）就不隐藏；拖动起手时若正在收回则整个手势不接。

## v0.96.265 — Scheme：用户函数看不见顶层变量（台账最后一条 🔴）

顶层绑定按 `R12 + (12 - off)` 寻址，而 `R12` **在函数里是那个函数自己的帧指针**
（序言 `push R15; push R12; move R12 R13`）⇒ 同一个偏移读出来是该函数帧里的某个槽，两边互相踩。
`(define g 0)(define (w1)(set! g 5))(w1)` 直接崩在野地址上。

**修法**：入口时把 `R12` 存进数据段 `__scheme_top_fp`，凡是要碰**顶层绑定**的地方都改用它当基址。
`main` 全程只 `sub R13`、不动 `R12` ⇒ 顶层处两者相等，同一套代码在顶层也成立。

⚠ **修的时候又踩了本仓的老毛病**：`set!` 在**三个地方**各写了一遍
（顶层 / 表达式与函数体 / 另一条运行时分支），**只改了第一处** ⇒ 函数体里的 `(set! g …)`
照旧写 `[R12+off]`。症状是**函数永不返回**（VM 超时），汇编里一眼能看到两套基址混在一起。
三处已收敛成 `EmitStoreVar` 一份。

### 修完之后「六种形态各用一遍」才发现：三条老问题**一直被它掩盖**

改动前整份探针**崩在野地址上**，后面几条从来没跑到过 ——「骨架全绿只证明这条路径没坏」。
- **函数体多形式**：`(define (multi x) (set! g x) (+ x 1))` 返回 9（set! 的值），应 10。
  只生成 `l.Items[2]`（第一个形式）。与 `let`/`let*`/`letrec` **同一族** ——
  那三处上一轮已修成循环，**唯独函数体这一处漏了**。**已修**。
- **命名 let**（得 61，应 10）与 **N 元算术** `(+ a b c d e)`（得 3，应 15）：**未修**，已记进台账。
  后者不是「多局部量」的问题 —— `GenCall` 只把**恰好两个操作数**的 `+ - * /` 特判成一条指令，
  三个以上落到通用路径去调一个不存在五参的 `+` 库函数；Scheme 的 `+` 是变参的。

判据：`(define g 10)(define (bump n)(set! g (+ g n)) g)(print (bump 5))(print g)` → `1515`；
ABI 判据 7/7、跨语言输出判据 29/29 全绿。

## v0.96.264 — Pascal 的 `//` 行注释；以及台账里四条「已过时」条目的复核（台账第七批）

### 改代码的只有一条：Pascal `//` 行注释

词法器只认 `{ }` 与 `(* *)`，`//` 被原样吐成两个 `/` 交给语法分析 ⇒ 报的是**莫名其妙的语法错**。
而现代 Pascal（Delphi / Free Pascal —— 本前端的目标就是它们）都认 `//`。
已修：`//` 跳到行尾即止、不跨行。

### 复核后确认「已经好了」的四条（台账陈旧）

| 条目 | 复核结果 |
|---|---|
| Fortran `if` 条件里「紧跟括号的除法」 | `if ((a / b) > 3) then` 正常，编出 `DIV-OK` |
| Pascal 注释里只能写 ASCII | `{ 中文注释 —— 破折号、逗号 }` 正常编译 |
| Python 列表「写不生效」 | `b[1] = 7; print(b[1])` 打出 `7` |
| `Lib/` 两套栈清理约定并存 | **已消解**：703 处旧约定收尾**全在 `Lib/shared/backup/`**，而该目录无人引用 |

### 复查时顺手核实的两条

- **第二份实现（`stdio_funcs`）**：坏实现已删，各语言的 `stdio_funcs.vml` 现在是**转发壳**
  （`.linked "../shared/printf.vml"` + `../shared/scanf.vml`，只有 3 行）。
  `d`/`objc` 的 `stdio.h` 里那句 `#param lib("stdio_funcs")` 还在，但链到的是壳 ⇒ 无副作用。
  实测 ObjC 的 `printf("%d")` → 42、`sprintf("%s")` → abc。
- **`LibraryLinker` 后缀匹配劫持**：仍是 🟡（有一条未验证的"取最长匹配"改动被撤回过，
  留着不动是对的）。

⚠ 台账里两次出现同一个教训：**「当时是坏的」不能当证据** ——
Ruby 的 `def`、Rust 的跨行数组、Fortran 的除法、Pascal 的中文注释，四条都是中间某次前端改动
顺带带好的，**没人回头复测**，于是注释和台账一起陈旧了两三个版本。

## v0.96.263 — Objective-C 前端：`#include <waycoder_ui.h>` 报 `expected )`（台账第六批）

`Lib/c/waycoder_ui.h` 里有**两个形参叫 `id`**（`ui_timer_kill(int id)`、`ui_gradient(char* id, …)`），
而 **`id` 在 Objective-C 里是保留的*类型名*** ⇒ ObjC 前端在形参位置读到 `IdType 'id'` 就报
`expected )`。台账里那条「别引头文件、直接调用」的绕过就是这么来的。

两个形参改名（`timerId` / `gradId`）。**原型里的形参名对 C 没有任何语义、改名零风险**，
而这是 22 门语言共用的那一份头文件 —— 让它对 ObjC 也能 `#include` 才合理。

判据：`#include <waycoder_ui.h>` + `ui_clear(...)`/`ui_rect(...)` 编译通过；
`Examples/objc/*.m` 四个例子与 `drift.m` 全部照旧。

⚠ 顺带实测到一条**没记过、也还没修**的：ObjC 前端**不认 `0x` 十六进制字面量** ——
`ui_clear(0xFF000000)` 报 `expected ) (got Identifier 'xFF000000')`（被切成 `0` + `xFF000000` 两个 token）。
现阶段颜色按**负数十进制**写。已记进台账。

## v0.96.262 — BASIC 前端：保留字当形参名时的**假崩溃**（台账第五批）

台账那条「形参名不能叫 `on`」只写了"不能"，没写症状。查下来是两层：

1. 形参检查 `if (Peek().Type != TokenType.IDENTIFIER) return null;` —— 而 `return null` 在调用方
   只表示「这条语句没解析出来」⇒ **整条声明被静默丢掉**，词法位置却停在形参列表**中间**，
   后面的 token 全被当成顶层语句继续解析 ⇒ 运行期报 **`内存不足，无法分配!`**。
2. 就算声明在，**引用侧也认不出**（`on` 词法是 `TokenType.ON`，表达式解析器只在
   `TokenType.IDENTIFIER` 上建节点）⇒ `add = on + 1` 里的 `on` 读成 0。

**修掉第 1 层**：判据从「必须是 IDENTIFIER」改成「这个 token 的文本能不能当名字」。
⚠ 一开始写成「一律报错」，**立刻打掉了本来能用的写法** —— `Examples/basic/sysinfo.bas` 的
`NATIVE FUNCTION ui_call_json_s(fn AS STRING, …)` 里 `fn` 就是 `TokenType.FN`，
而它 NATIVE 无函数体、用不到形参 ⇒ 以前丢掉声明也照样跑。
**先别急着把"静默"改成"报错"，先数一遍有多少正常写法在靠那个静默。**

**第 2 层没修（有意）**：根治要让表达式解析器在每个 IDENTIFIER 判据处也认「文本与已声明名字
相同的关键字」，而解析器根本没有变量名表 —— 影响面远超收益，且 `ON`/`FN` 本来就是保留字。
现状：**不再崩溃、不再报假错，但读出来是 0**，已如实写进台账。

判据：`Examples/basic/*.bas` 三个例子全部编译通过（`sysinfo.bas` 是这次差点打掉的回归）。

## v0.96.261 — Rust 前端：`println!` 的**下标实参**打出字面量 `[expr]`（台账第四批）

台账那条写的是「数组字面量**必须写在一行**，跨行的 `];` 会报非法 token」——
**复测已经能编了**（那条陈旧了）。但顺着它试了一眼输出，抓到另一个：

```rust
let xs = [1, 2, 3];
println!("{}", xs[0]);        // → 打出字面量 "[expr]"   ✗
println!("{}", xs[1] + 10);   // → 12                    ✔
```

`println!` 的格式实参是**按 AST 节点类型**分派的（字面量 / 标识符 / 二元运算 / 函数调用），
**没有下标这一条** ⇒ 掉进兜底分支，那分支往数据段塞一个字面量 `"[expr]"` 就打出来了。
**同一个表达式放进二元运算里反而是对的** —— 只测那一种形态永远照不出来。

- 补 `IndexAccessNode` 分支（`Visit` 收尾把元素值留在 R0，接着打即可）；
- 兜底分支**改成报错**，不再打占位符。本文件上面那条整数字面量的分支早就写着
  「宁可报错也不静默丢 —— 静默丢正是这个 bug 藏了这么久的原因」，这条兜底属于同一族。

判据：`Examples/rust/*.rs` 与 `drift.rs` 全部照旧编译通过；`println!("{}", xs[0])` 打出 `1`。
跨语言输出判据 29/29、ABI 判据 7/7 全绿。

## v0.96.260 — Kotlin 前端：顶层属性、`step` 软关键字与循环步长、`println(变量)`（台账第三批）

### ① 顶层属性读回是 0 —— 根因**不在数组**，比台账写的宽得多

台账原文是「文件级的 `arrayOf` 读回是 0」，绕过写的是「状态数组写在 `main` 内部」。
实际 `val n = 5` 也一样读回 **0**。两处凑成：

- `Parser.Parse()` 只认 `external`/`fun`/`data`/`class`/`interface`/`sealed`/`object`，
  **没有一条分支认 `val`/`var`** ⇒ 兜底的 `else Advance()` 把声明**一个 token 一个 token
  地静默吃掉**（连报错都没有）；
- 就算声明在，`VarRef` 也只在 `_varOffsets`（函数局部表、每进一个函数就 Clear）里查，
  查不到就**一条指令都不生成** ⇒ R0 留着上一步的残值。

改成：解析器认顶层 `val`/`var`；顶层属性放**数据段**，由 `main` 开头初始化一次；
`VarRef`/`AssignStmt` 各补一条全局分支。

### ② `step` / `until` / `downTo` 是**软关键字**，却被当硬关键字用

`var step = 5` 报 `Expected variable name ... got KEYWORD 'step'`。
三者只在 `for (i in a..b step c)` 这个位置有意义 ⇒ 移出词法关键字表，
`for` 那三处改成按文本比对。

⚠ **改完顺手验 `for..step` 才发现步长压根没生效**：`ForStmt.Step` 解析出来了，
**代码生成从没读过它**，增量写死 ±1 —— `for (i in 0..10 step 2)` 静默打出十一个数。
同批修掉（step 进循环前求值一次存槽位）。

### ③ 新发现：`println(变量)` 打出的是地址

与 Ruby 的 `puts(变量)` 同一族：两条实现（`print_str` 收地址 / `print_int` 收数值）、
值无类型标记、原判据只看字面量与返回串的函数 ⇒ `val s = "abc"; println(s)` 打出 1032。
按初始值登记 `_stringVars` / `_stringGlobals`。
⚠ **形参仍做不到**：解析器把形参的**类型标注整个丢掉了**（只存名字），
`fun f(s: String) { println(s) }` 还是打地址 —— 已记进台账，要修得先留着形参类型。

### 判据

`scripts/vml-out-probe/langs/nat.kt` 扩成：三个值放**顶层属性**、并用
`for (i in 0..12 step 2)` 累加出 **42**（步长不生效时是 78）。
跨语言输出判据 29/29、ABI 判据 7/7 全绿。

## v0.96.259 — Ruby 前端：`&&` / `||`、`puts(变量)` 打出地址（台账第二批）

### ① `def` 那条**是陈旧的**，复测已经好了

台账里挂着「`def` 报 `Unexpected token: End` —— 一个函数都写不了」，
`Examples/ruby/catch.rb` 顶上那段注释也照着它写「所以本份例程不用任何方法」。
**2026-09-19 六种形态全部复测通过**（无参/1 参/2 参、带括号与不带括号的调用、`return`、
以及台账点名的 `def resetGame … end`）。中间那次「调用约定统一」应该把它一起带好了，
**但没人回头复测**，于是注释和台账一起陈旧了两个版本。
⇒ 教训写进台账了：**改了前端要回头把标着 🔴 的条目重跑一遍**。

### ② 词法器完全不认 `&&` / `||`（真，已修）

`&` 和 `|` 在 `Lexer.Tokenize` 里**连 case 都没有** ⇒ 一写就 `Unexpected char: &`。
而解析器那边**本来就认** `TokenType.And`/`Or`（`and`/`or` 关键字走的就是它），
缺的只是词法这一层。现在 `&&` → `And`、`||` → `Or`，代码生成里与 `and`/`or` 并列。
单写的 `& | ^`（Ruby 的位运算）**仍然没做** —— 给的是明确的报错文案，不是静默算错。

### ③ 新发现：`puts(变量)` 打出的是**地址**

```ruby
s = "VAR"
puts(s)      # → 1024（地址）   ✗
puts("LIT")  # → LIT            ✔
```
`puts`/`print` 在 VML 里有两条完全不同的实现（`print_str` 收地址、`print_int` 收数值），
值本身**没有类型标记**，只能编译期判。原判据只看「字符串字面量 / 名字像返回串的函数」，
**变量一律不算** ⇒ 走整数那条把地址打了出来。

改法：按「赋值来源」记一笔（`_stringVars`），并在**预扫描**里记下
「哪个函数在第几个实参位上收到过字符串」，据此标记形参；认不准时**保守取整数**
（认错成字符串会拿小整数当地址去解引用，更糟）。
⚠ **同一形参在调用点类型不一致时仍做不到**（`f(7)` 与 `f("LIT")` 并存是合法 Ruby）——
现在的规则是「所有已知调用点一致才认字符串」，混合时退回原口径，**保证不比改前更差**。

### 判据

新增 `scripts/vml-out-probe/langs/nat.rb`：走 Ruby **自己的** `puts`（与 `out.rb` 的共享库
那条是两套实现），且刻意把值放进**变量**、并带一个 `&&` —— 一条探针同时钉住 ② 和 ③。
**反证过**：把变量判据临时关掉，这条立刻报 `1024 / OUT-INT=42 / 1036`。
跨语言输出判据 29/29、ABI 判据 7/7 全绿。

## v0.96.258 — C 前端：`&形参` 取址、`printf` 的 `%` 转换（前端缺陷台账第一批）

按 `third_party/vml/FRONTEND_DEFECTS.md` 逐条清。**这一版是 C 一门。**

### ① `&形参` 得到的是裸 `R12`（台账里挂了两个版本、当时只观察到现象）

台账原条目写的是「通过指针形参写回会生成坏地址」，并列了四种猜测 —— **四条全不对**。
缩小之后真身只有一条：**取形参的地址**。

```c
void par(int v) { int* p = &v; … }     /* 实测 *p = 65528（应为 42），写回也完全不生效 */
```

- **取局部地址 / 取全局地址 / 通过指针形参写回**（`wrp(&x)`）**全都是好的** ——
  「指针写回一律坏」这个结论是错的，坏的是「取形参地址」这一个操作。
- 真身：C 前端里「取地址」有**两份实现**。`GenerateUnaryOp` 的 `case "&"` 手写的那份算的是
  `offset = stackFrameSize - 变量偏移` 再 `SUB`，而 `stackFrameSize` **只被赋过一次 0、再没更新过**
  （编译器自己会报 CS0414）。局部变量偏移是负的 ⇒ 减负数得正数 ⇒ 碰巧 SUB 对；
  形参偏移是正的 ⇒ 减出负数 ⇒ 那个 `if (offset > 0)` 不成立 ⇒ **一句不加**。
  而 `GenerateAddressOf` 用的 `FormatVarOffset` 本就按**带符号偏移**来 ——
  **同一规则两处实现、只对了一半**。
- **改法**：`case "&"` 整个改成转调 `GenerateAddressOf`（收敛成一份），删掉那个死字段。

### ② `printf` / `sprintf` 的 `%` 转换

```c
sprintf(b, "ab%dcd", 9);   /* → "ab" + 地址的十进制 + "cd"  ✗ */
sprintf(b, "%s", "abc");   /* → "%s"（把格式串自己打了出来）  ✗ */
printf("%d\n", 42);        /* → 42  ✔ —— 纯属巧合 */
```
`printf.c` 取变参表写的是 `(int*)(&fmt + 4)`：`&fmt` 是 `const char**`，`+4` 按 4 字节缩放成 **+16**
⇒ 读到 `fmt` 之后的**第 4 个**槽。`printf` 只有一个形参、变参紧跟其后，**碰巧落对**；
`sprintf` 多一个 `buf` 就整个读偏，`args[0]` 读到的正是 `fmt` 自己。
改成 `(int*)&fmt + 1`（按 `int` 步长加一格，与形参个数无关）。

### 判据（都反证过）

- 新增 `scripts/vml-abi-probe/probes/p7_param_addr.c`：读形参地址 + 写回形参地址 +
  取局部/全局地址三组一起钉。**把旧实现放回去，这条立刻报 `ABI-FAIL 取形参地址读回不对` 并挂死**。
- ABI 判据 7/7、跨语言输出判据 28/28 全绿。

### 顺手修掉的两个**工具链**坑

- **`scripts/vml-out-probe/run-langs.sh` 在 macOS 上一直是 0/28**：脚本里用了 `timeout`，
  那是 GNU coreutils 的命令、macOS 默认没有 ⇒ 整条命令行 `command not found` ⇒ 抓到空输出
  ⇒ **28 条探针一起报 FAIL，看上去像"所有语言都坏了"**（手工单跑 `out.c` 却三行全对）。
  已改成「有 `timeout` 用它、其次 `gtimeout`、都没有就不加外壳」。
  **判据脚本坏了比没有更糟 —— 它会指挥你去修错的东西。**
- **重生成 `Lib/` 必须用 GenLib，不能用 `vmlcli --rebuild-lib`**：后者不设
  `CompilerOptionsContext` ⇒ 默认 Soft 模式 ⇒ 64 位模块退化成库调用
  （实测 `printf.vml` 的 `movel` 从 47 掉到 1）。GenLib 显式 `Int64Mode.Hard + Float64Mode.Hard`，
  且会跳过 mtime 比 `.c` 新的 `.vml`（只想重建一个模块就把其余 `.vml` `touch` 一下）。

## v0.96.257 — 真机修掉计算器的五个缺陷 + syscall 参数防护 + 两处 TabBar

这一版几乎全是**在真机上用眼睛看出来的**：界面全对、程序不崩，但就是不对。
五个缺陷分散在**三个不同的层**（例子代码 / 矢量后端 / C 前端），根因各不相同。

### ① 计算器（`Examples/c/calc.c`）——五个缺陷

| 现象 | 根因 | 层 |
|---|---|---|
| 按键一个都画不出来 | `keyRect` 从"指针形参写回"改成"全局量返回"时**调用点没跟着改**，多传的 4 个实参被前端**静默丢弃**，`x/y/w/h` 一路未初始化 | 例子 |
| 文字整体右移（列间距只剩一半） | 矢量后端用「`[x, 画布右边]` 矩形 + 对齐」近似锚点 ⇒ `Center` 居中的是那个矩形的中点而不是 `x` | **矢量后端** |
| 显示屏一直空白 | `fmt()` 的输出没进缓冲区；改成直接写全局量后正常 | 例子 |
| 第 5 行多出一个空按钮 | 空位键虽跳过文字却仍画了矩形，而它在跨格的 `0` 键**之后**绘制、正好盖住右半 | 例子 |
| **按键点了完全没反应** | `int i, m[4], t;` ——**标量与数组合并声明**，局部数组分不到槽位，`m[1]/m[2]` 的读取指令**整个不生成** | **C 前端** |

最后一条最隐蔽：**编译一声不响**（对比之下，"未定义的数组"是会崩编译器的），
现象只是"按键无反应"，而界面绘制一切正常，很难往"输入"上想。
`plane.c` 写的是 `int m[4];` 单独声明，所以同样的代码在那边是好的。

### ② 飞机空战"打一会自己闪退" —— 漏了 `ui_clear()`

`plane.c` 全文没有 `ui_clear`。所有绘制都往宿主**同一张图元表**追加，渐变铺满全屏只是
"盖上去"、不是重置 ⇒ 每帧涨约 200 条、30fps 下每秒 6000 条、只增不减，几十秒后撑爆。
`docs/VML游戏开发指南.md` 与 `starfall.c` 都记录过这个坑；扫描后**只有 calc.c 和 plane.c 漏了**。

### ③ syscall 参数防护（你要的"防止异常值把系统搞崩"）

收口放在**共享层 `VmlScene`**（所有端共用，且桌面可自测），策略是「**让异常参数最多画不出来，绝不崩**」：

- **坐标越界（±100 万外）→ 整个图元丢弃**（屏幕外的东西本来也看不见，丢掉还省内存）
- 尺寸 / 半径 / 线宽 / 字号 → 负数钳 0、超大钳到窗口
- **图元数封顶 12000** —— 这是 ② 那类"漏 `ui_clear`"的**宿主侧兜底**
- 文本按**码点**截断（不切碎代理对）；`path` 串里的**换行**抹掉（DSL 按行解析，一个 `\n` 能伪造出整条指令）
- 文字锚点矩形抽成纯逻辑 `VmlUi.TextAnchorBox` 放共享层，自测能锁住（bug ①-2 就是它）

### ④ 两处 TabBar 恒不显示

编辑器页与 VML 绘图窗口都是整页占满的页面，底部一条切页栏既没用又占高度。
⚠ 原来**是有条件显隐的**（游戏窗口只在横屏隐藏、编辑器跟全屏开关走），
不是"没做" —— 这次只是把两个"设 true"的分支拿掉。XAML 上的 `Shell.TabBarIsVisible`
对 push 出来的页面**不生效**（实机验过），必须写在 code-behind。

### ⑤ 按钮布局与配色

- **横竖间距统一**：原来水平是 `2×bgap`、垂直是 `1×bgap`（右边看着总空一截），
  现在两个方向共用一个缝
- **按功能分区的四档配色**：数字（深蓝灰）/ 运算符（暗金）/ 功能 `C ± %`（砖红）/ `=`（亮蓝）
  —— 色相拉开才分得清，同一色系只差明度在手机上几乎看不出区别

### ⚠ 未验证 / 待办

- **配色**：代码与汇编逐条核对过（`fn_of` 的短路求值、`f == 1` 比较、`col = KEY_FN` 赋值都正确），
  但**没能在真机上验到**（几次截图看到的都是 App 恢复的上一次运行的窗口）。
  另外 `keyFn` 数组查表 → `fn_of` 函数这一改，**证据并不成立**，真因待查。
- **按下 / 抬起的视觉反馈**：代码里有（换高亮色 + 下沉 2px），但诊断块显示 TOUCHDOWN 分支
  没有执行，而点击又能正常输入数字 —— 两件事对不上，怀疑是**我的 adb 模拟输入不可靠**
  （已证实 `input swipe` 不产生 TOUCHDOWN）。需真手指确认。

## v0.96.253 — 建 `third_party/vml/FRONTEND_DEFECTS.md`：前端缺陷台账

用户：「写代码过程中，收集各种编译器缺陷到文件，后面我会全部补上缺陷或者修复错误」。

把写例子时一条条踩出来的问题汇总成一份台账，**每条都写清三件事**：
现象 / **最小复现** / **判据**（怎么确认它真的坏了，而不是我代码写错）。
判据最重要 —— "我以为的边界"和"真的边界"差的往往就是这些。

- **状态分三档**：🔴 未修 · 🟡 已修（注明补丁号/版本） · ⚪ 限制（不是 bug，是没做）
- **已修的不删** —— 后来的人需要知道这里踩过坑
- ⚠ **只观察到、还没缩小的缺陷，状态就照实写**（比如本次发现的「指针形参写回」），
  别把"猜测的原因"写成结论 —— 那会误导下一个来修的人

收进去的（按语言分组）：C 6 条 / Ruby 2 / Kotlin 2 / Rust 2 / Fortran 1 / Pascal 2 /
BASIC 2 / Scheme 2 / Python 1 / JavaScript 1 / ObjC 1 / Ladder 1，外加
「全局」一节 3 条（`Lib/` 两套栈清理约定、`LibraryLinker` 后缀劫持、
"第二份实现"的查法）。

末尾附「写新例子时的防御性写法」——从这些坑倒推出来的 5 条，新写例子照着写能避开绝大多数。

## v0.96.252 — 两个新例子（飞机空战 / 计算器）+ 修 UI 文档的错误说明

### ① 写例子时发现：我上一版发的 UI 文档**有几处是错的**

`vml/ui/*.md` 里签名行是从头文件抓的（没错），但**描述和例子是我按印象写的**，
好几处与真签名不符。逐条核对后修正 20 条，典型几个：

| 我原先写的 | 头文件实际是 |
|---|---|
| `ui_polygon(pts,n,fill开关,stroke…)` | `fill`/`stroke` 是**颜色**，`width` 是线宽 |
| `ui_gradient(…)` 返回 int id | `ui_gradient("id", radial, 色A, 色B, a1..a4)` —— 传**字符串 id**、返回 void |
| `ui_rand()` 再 `% n` | `ui_rand(n)` → 0..n-1 |
| `ui_store_get("k")` 返回整数 | `ui_store_get(key, buf, cap)` —— 写进你的缓冲区 |
| `ui_poll(m, 0)` | `ui_poll(m)` —— **没有 timeout 参数** |
| `ui_call_json_print("fn","")` | 无参：它打印**上一次** `ui_call_json_s` 的结果 |

**并给生成器补了一道闸**：例子里的调用**参数个数必须与头文件签名一致**，不一致直接报错退出。
补上之后当场又抓出 4 处（`ui_poll` 多一个参数、`ui_circle_grad` 多两个、
`ui_call_json_print` 多两个、`ui_poll_ex` 多一个）—— 说明这道闸是有效的。
措辞对不对仍要靠人读头文件，但"照着例子抄会编译不过"这类错，现在跑一次生成器就红。

### ② `Examples/c/plane.c` —— 《长空》竖版飞机空战

**只用手指**：`ui_win_open_ex(..., VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD)`，
手柄区连同折叠条都不显示、画布吃满整屏；拖动即移动（阻尼跟随），自动开火。

画面：天空/海面渐变、三层云视差、飞机是 `ui_polygon` 拼的机身+主翼+尾翼
（**转弯时整机倾斜**）、螺旋桨按 tick 转、枪口焰与爆炸用「同色多层递减 alpha」叠出光晕、
粒子爆炸、HUD。桌面 CLI 验：**46337 条指令、零错误**。

### ③ `Examples/c/calc.c` —— 《算盘》计算器

径向渐变底 + 玻璃面板显示屏（算式小字在上、结果大字在下、右对齐）、
圆角按键分三色（数字/功能/等号）、**按下有高亮与下沉反馈**、手指点按命中。
数字用**千分之一定点**（内部 ×100 的整数），不用 `sprintf`/`%f`。

**写这个例子时挖出一个前端缺陷**：`void f(int* x) { *x = …; }` 这种
**"通过指针形参写回"**在这条 C 前端上会生成坏地址 —— 实测
`内存错误(PC=000002AB): MOVE @1, R0 — 地址=FFFFFFF1`，
而把同样的逻辑改成写全局量就正常。已把这条写进 `calc.c` 的注释里（`plane.c` 没这种写法所以没踩到）。

## v0.96.251 (2026-09-19) — 工作区多了一个 `help/` 目录：说明文档可以直接翻阅

用户：「在工作目录也需要建个 help 目录，和 examples 一样，但是只放各种说明文档，方便用户自己查阅」。

### 一个源，两个落地点

说明文档的**唯一源**仍然是 `WayCoder.Maui/Resources/Raw/help/**`（App 内「使用说明」读的就是它）。
新增的只是它的第二个落地点：

```
Resources/Raw/help/**  ──make-vml-lib.sh──▶  vml_lib.zip 里的 Help/
                                                      │
                                          EnsureHelp() 解包
                                                      ▼
                                        工作区 help/   ← 用户在文件页里直接点开看
```

**不存在"两份要对着改"**：两个位置都出自同一份源（`make-lang-help.py` /
`make-ui-help.py` 生成的也是那一份）。

- `scripts/make-vml-lib.sh`：把 help 树**经暂存目录**打进 zip 的 `Help/` 前缀下
  （Info-ZIP 只能按"当前目录的相对路径"存档，直接加会写成 `Resources/Raw/help/...`，
  与期望布局对不上）。两条打包路径（`zip` / Python zipfile）都覆盖。
- `MauiBootstrap.EnsureHelp()`：与 `EnsureExamples()` 同一个 zip、同一个版本闸门，
  解到 `WorkspaceDir/help/`。**整棵替换**（说明文档是一整套，留一半旧的比不给更糟）。
  解不出来只记日志、不拦启动。

⚠ **改了说明文档必须升版本** —— 闸门就是 `Global.Version`（与 examples 同一个坑）。

真机验收：`/storage/emulated/0/waycoder/workspace/help/` 下 48 篇 markdown，
按 `quickstart / vml / vml/lang / vml/ui / editor / cli / files / settings` 分层。

## v0.96.250 (2026-09-19) — 「UI 开发」补齐全部 60 个接口，并按类别分级

用户：「ui 开发要把所有函数用法列出来举例说明，现在只有基础的」，随后补一句
「如果怕页面太大，可以也分级」。

### 从「基础的 5 个」到「全部 60 个」

原先那页只有开窗/绘图/文字的入门几个，而宿主接口其实有 **60 个**（头文件 `Lib/c/waycoder_ui.h`）。
新增 `scripts/make-ui-help.py`：

- **签名从头文件抓**（权威来源），这里只写「一句用法 + 一个例子」；
- **两个方向都对账**：头文件里有而说明表没写的、说明表有而头文件没有的，**直接报错退出**
  —— 加了新接口不补文档 = 生成器跑不过去，而不是"文档悄悄少了几个函数"。

### 分级：一页 60 个太长 ⇒ 按类别拆成 11 个子页

`vml/ui` 变成索引（最小程序 + 四条骨架 + 各类别的一览表），
每个类别一页：`vml/ui/{window,messages,timer,draw,text,piece,grid,dialog,feel,store,json}`。

用的是 v0.96.244 做的 **`help:` 链接跳转** —— 加一类只是加一个 markdown 文件，不动代码。
自测里那条「正文里 `help:` 链接的目标都存在」+「没有放了却没人看得到的 .md」把两边钉住。

内容按**实测经验**写，不是抄头文件：`ui_gclear/set/get`（有的前端数组写不回来）、
音效单通道（用音高表达好坏）、`ui_wait(msg, 0)` 是「一直等」不是「不阻塞」、
转屏只有 `VML_WIN_ROTATABLE` 那一档才换坐标系、`ui_call_json` 别用在每帧调用的地方。

真机复验：关于 → VML 编译器 → UI 开发 → 点类别进子页。

## v0.96.249 (2026-09-19) — Markdown 的「垂直节奏」：段落不再糊成一坨

用户：「总觉得 markdown 的渲染显示效果比电脑端差很多，好像内容在一坨，不同段落之间没分开」。

### 真身：所有块只有**均一的 4px 间距**

`VerticalStackLayout { Spacing = 4 }` —— 标题、正文、列表、代码块之间全是 4px。
看着"都隔开一点"，但没有**垂直节奏**。桌面阅读器（GitHub / VS Code 预览 / Typora）用的是：

| | 间距 |
|---|---|
| 标题**上方** | 一大档（h1 26 / h2 22 / h3+ 18）—— **这才是"分段"的唯一手段** |
| 标题下方 | 10 / 7 |
| 段落之间 | 各 7（合起来 14，比行距明显大一档） |
| 列表项之间 | 6，整块上下 7 / 10 |
| 代码块 / 表格 | 上下 6 / 10 |

改成 `Spacing = 0` + **每个块自己带 Margin**：块间距变成"按块类型决定"，而不是一律 4。
字号也分了档（23 / 19 / 16.5），行高 1.35 → **1.55**。

### 列表：悬挂缩进 + 修双标记

原先把 `• ` 拼进正文再整段渲染，两个后果：

- **折行顶到最左**：长条目的第二行回到行首，与上一级标题齐平 —— 分不清"同一项的第二行"还是"新的一项"。
  现在标记单独占一列（Grid `Auto,*`），文字列对齐，与桌面渲染器一致。
- **有序列表双标记**：`1. 用了…` 被拼成 `• 1. 用了…`。拆标记的逻辑收进 `SplitBullet` 一处：
  无序给 `•`、**有序直接用它自己的序号**。

真机复验（「常见错误」页）：章节靠留白切开、段落分明、`1./2./3.` 编号正常、折行对齐到文字列。

## v0.96.247 (2026-09-19) — 表格画出格线、整格可点

用户：「表格能否做出画一圈格子的表格，现在的表格有点小，点击不方便」。

### 格线：靠"留缝"，不靠描边

MAUI 的 `Border` **只能四边一起描边**（没有单独画某一边的 API）⇒ 一格一个 Border 拼起来，
相邻处会叠成 2px、外圈 1px，粗细不一。改用**留缝**：

```
Grid.BackgroundColor = 线色
Grid.RowSpacing = Grid.ColumnSpacing = 1     ← 露出来的就是 1px 格线
Grid.Padding = 1                             ← 外圈那一圈
每格 Border 铺自己的底色盖住中间
```

格线宽度只由 spacing 决定，不会有"某条线偏粗"。

### 点击：手势挂到**整格**

原先只有链接那几个字有手势，手指要精准落在文字上（用户说"点击不方便"）。
现在每格 `Padding = 12,12`（约 44dp 高，接近触摸目标下限），手势挂在格子上。

⚠ **整格可点的格子，里面那个 Span 的手势必须去掉** —— Span 手势会挂上
`LinkMovementMethod`，它**把触摸整个吃掉**（而它自己的 Span 手势在 Android 上又不触发）
⇒ 事件传不到外层那个真正管用的 Border，表现为"点了没反应"。
真机上试出来的：同样的格子，只把 Span 手势留着就点不动。

### 顺带修掉一个刚引入的 bug

改成"整表统一循环"之后，markdown 的**分隔行** `|---|---|` 被当成数据渲染出来了
（屏幕上多一行 `--- | ---`）。原先是靠"数据行从下标 2 开始"绕开的；
判据改成"每格只由 `-`/`:`/空白组成"，比写死下标稳。

真机复验：格线完整、行高约 44dp、点表格里的 C 进得去 C 语言页。

## v0.96.244 (2026-09-19) — 说明页点不动：`CollectionView` 没写 `SelectionMode` + 链接跳转

用户报「二级再点不会跳转3级了」。查下来**不是跳转逻辑的问题**：

### ① 真身：`CollectionView` 默认 `SelectionMode = None`，选中事件**一次都不触发**

`HelpListPage.xaml` 的列表没写 `SelectionMode` —— 于是 `SelectionChanged` 从注册那天起
就没响过，二级、三级**都**点不动。**症状是"点了没反应"**：不报错、不崩、列表看着完全正常。
（仓库里其余几处 `CollectionView` 都显式写了 `Single`，就本页漏了。）
→ 补 `SelectionMode="Single"`，并加了一条**机械护栏**自测：
扫 `Pages/*.xaml`，凡绑了 `SelectionChanged` 的 `<CollectionView>` 标签必须同时有
`SelectionMode` —— 这类"不报错的坏"只能靠结构检查兜住。

⚠ 同一个坑在 `ChatPage.xaml` 的 `/` 命令建议列表里**也存在**（点击填入同样不生效），一并修了。

**教训**：我一开始去改"跳转逻辑"（给容器节点加 `Children` 处理），而那段代码**压根没机会执行**。
先确认"事件到底有没有触发"，再改事件里做的事。

### ② 层级改由**正文里的链接**决定（用户提的方向）

原先层级写死在 C# 目录表里（`Topic.Children`）——每加一层都要动代码、动列表页，
而且"哪些节点是目录、哪些有正文"判断漏一处就是"点下去什么也不发生"。
现在正文里写 `[C 语言](help:vml/lang/c)` 就跳到那一篇：**多少级都行，加页面只写 markdown**。
`Children` 那套已删除（两套层级机制并存 = 本仓库头号坑）。
页面标题也改成**以正文自己写的 `# 一级标题` 为准**（链接目标不一定在目录表里）。

**坑**：Span 级的 `GestureRecognizers` 在 Android 上**点了没反应**（链接画得对、长按也不行）。
改成"整块就是一个链接时，手势直接挂 `Label`" —— 表格里那一列正是这种形态。实测通过。

### ③ 表格 / 列表单元格改走与段落同一条渲染路径

原先表格单元格是裸 `Label { Text = … }`，正文里能用的东西一进表格就变字面量：
`[C](help:...)` 原样显示、`**粗体**` 带星号。现在单元格与列表项都走 `BuildParagraph`。
另外生成器把上游文档（以及本仓自己写的那几段）里的 `**粗体**` 转成 `«bold»` 标记 ——
渲染端只认 `«»`，不解析 markdown 的 `**`（同样跳过围栏代码块）。

自测 5966 全绿。真机复验：关于 → VML 编译器 → 22 种语言 → 点表格里的 C → 进 C 语言页。

## v0.96.239 (2026-09-19) — 使用说明补到三级（22 种语言各一份）+ Markdown 日夜配色 + 命令行页彩色

用户提的三件事，都在「手机上看得清、看得全」这条线上。

### ① 22 种语言，每个一份说明 —— **从 VML 源码生成，不手写**

用户：「每个语言的文档刚好 vml 源码的各个编译器下面有」。
查下来确实：`third_party/vml/VMLPrepares/<X>Compiler/` 下每个都有
`README.md`（支持什么、怎么编）+ `<语言>_LANGUAGE_SPEC.md`（语言规范：语法/类型/标准库）。

**手写一遍 = 把它们抄错一遍，而且上游一改就漂。** 新增 `scripts/make-lang-help.py`：

- 上游两份文档**原样嵌入**，标题统一**降两级**；
- ⚠ 降级**必须跳过围栏代码块** —— C 代码里满是 `#include`/`#define`，不跳的话整段代码散架；
- 前面只补上游没有的那一小段：**怎么在手机上跑**、**这个目录里有哪些现成例子**、**实测踩过的坑**
  （例：Ruby 的 `def` 会报 `Unexpected token: End`，这一路**一个函数都写不了**；Fortran 的 `if`
  里解析不了「紧跟括号的除法」；Kotlin 文件级 `arrayOf` 读回 0）；
- 示例清单**从目录实扫**，不手抄文件名。

`AskUserQuestion` 式的"目录↔文件"自测也跟着加强：原先只查一层、且
`Check(..., seen.Add(...) || true)` 是**恒为真的空断言**；现在递归进 `Children`，
并补上「主题 id 不重复」的真判据。

`vml/languages` 成了纯容器（点开是下级列表），它自己那份 `languages.md` 就永远点不到了 ⇒
把里面的「一览表」并进 `vml/index.md`，删掉那个孤儿文件。

### ② Markdown 两套配色 —— 白天主题下整篇看不见

`MarkdownPreview.TextColor(isDark, …)` **完全没用 `isDark`**，恒返回亮灰 `#E0E0E0`。
夜间主题下正常，白天主题下就是**白底白字** —— 说明页整篇不可见（编辑器「预览」同一个 bug）。

改成 `Ink(isDark, 暗RGB, 亮RGB)` 显式两套，7 处取色全部给两天套值。
（代码高亮的字色本来就有两套 —— `MarkupToFormattedString.ResolveFg` 里有
`ForLightBackground` 做浅底翻新；漏的只有本文件自己画的标题/表格/分割线/代码块底色。）

### ③ 表格与代码块横向滚动

外层只有竖向 ScrollView，宽内容右边就被吃掉。两种内容**不能同一种解法**：

- **表格**折行会把列对错（那就不是表格了）⇒ 套横向 `ScrollView`，列宽 `Auto`；
- **代码块**同理折行会毁掉缩进对齐 ⇒ 也套横向 `ScrollView`（`LineBreakMode.NoWrap` 保留）。

### ④ 命令行页终于有颜色了（`AnsiMarkup`）

外部命令的输出带的是**终端控制字节**，而移动端富文本这条路原先**直接 `StripAnsi` 丢掉**：
`ls --color` 的目录蓝、`git status` 的红绿全没了。结果不是"没颜色"而是"看不出重点"。

新增 `UI/Shared/AnsiMarkup.cs`：裸 ANSI → `«»` 中间格式（**有限子集**，
不是终端模拟器）。支持 SGR `0/1/2/3/4/9`、`30-37`/`90-97`、`40-47`/`100-107`、
`38;5;N`/`48;5;N`、`38;2;r;g;b`；**吃掉**光标/擦除/OSC 等一切非 SGR 序列；
**刻意不支持**反白（它要交换前背景，而本项目取色是跟着日/夜主题走的，"交换"在主题适配之后语义就变了）。

配套两条：

- **`MauiVml.Run` 加 `markup` 开关**（默认 false = 老行为）。AI 走的 `vml` 工具**必须是不传的那一支**
  —— 颜色标记混进工具结果只会污染模型看到的内容；命令行页传 `true`，并且
  **两个流分开接**（原来并进同一个 sink），**stderr 整段套红** ——
  `VML 错误`/寄存器 dump 走 stderr，编译报错也套红，用户要的就是"一眼看出出事了"。
- 顺带修掉一个**全仓存在的老 bug**：`AnsiHelper.Esc` 把 `«` 变成 `««` 之后
  **没有任何地方把它变回来**，于是"转义过"的文本在屏幕上显示成两个书名号。
  是「ANSI→标记」那条**往返判据**（转换后渲染出来的可见文字必须与 `StripAnsi` 逐字相同）
  把它逼出来的。还原逻辑抽成 `TryReadEscapedBook`，`ParseInline` 与 `ParseMarkupOnly`
  **两个解析循环共用一份**（它们各有一套扫描循环，规则写两份必然漂移）。

自测 5962 全绿（新增 ANSI 一组 15 条，判据是往返而非"看起来对"）。

## v0.96.237 (2026-09-19) — `.vmb` 终于能跑了：VMB 编解码四处读写不对称

用户报「vmb 现在无法运行，要是识别二进制格式，运行 vmb 肯定速度快」。
`.vml` 是文本汇编（每次运行都要现场汇编一遍），`.vmb` 是它的二进制形态（装载即执行）。
链路本身早就写好了（`vml build x.c` → `.vml`，`vml build x.vml` → `.vmb`，`vml run x.vmb` → 装载），
**坏的只有编解码**：编译器产出的 `.vmb` 自己读不回来。

### 判据先行：往返 + **逐条比对操作数**

```
.vml（44370 条指令）→ ToVmbBytes() → .vmb（867452 字节）→ FromVmbBytes() → 逐条比对
```

⚠ **判据不能只看"指令条数对得上"** —— 解码错位时条数常常照样对得上。
第一版就是只比条数，于是 `[R12+200]` 单独一条也"✔ 通过"，白绕一圈。
改成逐条比对操作数之后，边界一次就量准了。

### 四处不对称（前三处是这次新发现的）

| # | 现象 | 真身 |
|---|---|---|
| ① | **`.vmb` 装载抛 `Unknown operand type tag: 0x00`**（本次跑不了的根因） | 写侧把**间接寻址**编码成 `MEMORY(0x03)` + bit31 置位的 uint，读侧只能靠"peek 后面 4 字节的最高位"去猜它和**寄存器相对寻址**（子模式 0x02）；而子模式 0x02 的第 4 个字节**正是偏移量的最低字节** ⇒ 偏移 ≥ 128 的 `[R12+200]` 被当成间接寻址读掉 4 字节（实际 6 字节），后面整条字节流错位。**实测边界：偏移 8/127 往返一致，128/200/384 全错。** |
| ② | `[R12-24]` 读回来变成 `[R12+24]` | 符号位是**约定值**（写侧 0=正、1=负），读侧却写成 `sign >= 0 ? "+" : "-"` ⇒ 无论正负都取 `+`。**局部变量与参数全部指错**，而且不抛异常、只让程序行为诡异地不对 |
| ③ | 数据段里有数组就抛 `Unknown data type tag: 0x40` | 写侧会写 `0x40`（int/float/double 数组），读侧**没有这一支** |
| ④ | 数据段有 64 位常量就抛 `Unsupported data type: System.Int64` | 写侧不认 `long` —— 而 64 位常量在 C 标准库里到处都是（hello 级程序的数据段 160 项里 7 项是 Int64）。按 double 那一路编码（tag 0x10 + 8 字节小端）：**装载侧对 tag 0x10 是逐字节搬运、不按数值语义解释**，所以位模式原样保留、等价。顺带补上**列表元素**的 `long` —— 原来那一支会**静默写成 0**（比抛异常难查得多） |

**①的修法**：给间接寻址**一个自己的 tag（0x05）**，读侧删掉那段 peek。
一个 type tag 换掉一次"猜"，是这个格式里最划算的一处修改。
（旧的 `.vmb` 不再可读 —— 但 `.vmb` 在此之前**根本装载不了**，没有需要兼容的存量。）

### 验证

```
examples/c/sysinfo.c → vml build → .vml → vml build → .vmb → vml run
两条路（直接跑 .c / 跑 .vmb）打印的 JSON 逐字符一致
```

### 常驻检查：`scripts/vml-vmb-check`（穷举往返）

用户提的：「可以把 vml 所有编码列举一遍，然后编译成 vmb，再解码回去，对比 vml 源码是不是一样的就知道了」。
—— 比"拿俄罗斯方块跑一遍"硬得多，**而且这次抓到的四处里有两处正是抽查漏网的**
（偏移 ≥128 的寄存器相对寻址、负偏移符号位）。

```bash
dotnet run --project scripts/vml-vmb-check
```

把**每个 opcode** × **每种操作数编码形式**（寄存器 / 各宽度立即数 / 绝对与寄存器相对内存 /
标签 / 间接寻址）装进一个 `VmlProgram` → `ToVmbBytes()` → `FromVmbBytes()` → **逐条比对操作数**，
数据段**逐字节比位模式**。通过打印 `✔ 全量往返一致`，失败逐条列出并返回 1。

结果：**297 条指令 / 1902 字节 / 读回 297 条 → 全量一致**；真程序那条链
（`examples/c/tetris.c` → 44370 条指令 → 867452 字节）也做到了
**完全一致 43398 / 等价写法不同 972 / 真不同 0**（那 972 条是 `@R0` ≡ `[R0+0]`
这类**编码完全相同**的写法差异）。

判据本身也踩了两个坑，都写进 `scripts/vml-vmb-check/README.md` 了：
**只比条数是无效判据**（错位时条数照样对得上，第一版就被骗过去）；
**伪指令不写进 VMB**（`label`/`break`/… 由写侧过滤，穷举时要跳过，否则"少了 6 条"是误报）。

---

## v0.96.236 (2026-09-19) — `sysinfo`（系统信息）+ 21 种语言的 CALLJSON 自测

用户要的：「callwithjson 可以做个 GetSystemInfo，返回系统的 cpu、uuid、os 等一些系统性信息，
给每个语言写个测试」。

### ① 注册 `sysinfo`

```c
char buf[512];
ui_call_json("sysinfo", "", buf, 512);
```

返回（真机实测）：

```json
{"ok":true,"result":{"app":"WayCoder","version":"v0.96.236","platform":"android","os":"Android",
 "osVersion":"16","deviceModel":"sdk_gphone64_arm64","deviceName":"sdk_gphone64_arm64",
 "manufacturer":"Google","arch":"arm64","cpuCount":4,"memoryMb":382,
 "deviceId":"d99d9f3c…","screen":{"w":411,"h":914,"density":2.625,"canvasW":395,"canvasH":652},
 "orientation":0}}
```

函数名用 **`sysinfo`** 而不是 `GetSystemInfo`：已有的三个是 `echo`/`version`/`screen`，
小写单词是这套 JSON 函数名的既定风格（跨语言契约，改名要同步 22 端）。

⚠ **`deviceId` 是"本机安装实例的随机 id"，不是硬件序列号** —— 首次用时生成、存进 Preferences。
手机上拿硬件 id 要么要权限（ANDROID_ID 在新版本已按签名隔离）、要么根本拿不到（IMEI 早就不让读），
而且那类标识属于"设备指纹"，拿来做普通功能是过度收集。随机 id 够用来区分两次安装，清应用数据即换新。
**别拿它当设备指纹使** —— 这条写进代码注释了。
`canvasW/H` 与 `SCR_W`/`SCR_H` **同源**（调的就是同一个函数），免得出现"JSON 报的尺寸和 syscall 报的不一样"。

### ② 共享库加 `ui_call_json_print()`

让 22 种语言各自写一遍"逐字节取 + 单字符输出"的循环，就是 22 份同一段代码（本仓库头号坑）。
加一个 `ui_call_json_print()`（内部循环 `ui_call_json_at` + `putchar`）之后，
每份自测都只剩两行：

```c
ui_call_json_s("sysinfo", "");
ui_call_json_print();
```

### ③ 21 种语言各一份 `Examples/<语言>/sysinfo.<ext>`

按各语言已有示例的写法逐个写（Java 要 `static native`、Dart 要 `external`、BASIC 要 `NATIVE` 声明，
其余大多直接裸调由前端按库符号解析）。**桌面逐个编译过 21/21**，设备上跑了 C 与 Python 两份，
输出**逐字符一致**。

途中撞到两个语言各自的小坑（都写进文件注释了）：

| 语言 | 坑 |
|---|---|
| ObjC | **解析不了 `waycoder_ui.h`**（`expected ) (got IdType 'id'`，报在头文件里）⇒ 只能像 `snake.m` 那样直接裸调、不引头文件 |
| Pascal | 注释只认 ASCII：`//` 不是注释（要用 `{ }`），注释里的中文破折号/中文逗号都会报「未知字符」/「无效的字符代码」 |

**Ladder 只放了个占位**：它是 PLC 风格前端，没有"带字符串参数的函数调用"这种语法
（仓库里原有的 `ladder/file_io.ld` 本身就是一个 NOP 测试）。文件里写明了这一点。

---

## v0.96.235 (2026-09-19) — 编辑器手感三件：自动缩进 / 括号配对高亮 / 查找替换（+ 纯逻辑下沉到可自测层）

按 `docs/编辑器到IDE-差距清单.md` 的第一梯队开工，只做移动端。

### ① 自动缩进（回车断行时继承缩进）

原来回车只是「在光标处劈开」，新行从第 0 列开始 —— 写嵌套代码每按一次回车都要自己再敲一遍空格。
现在新行继承左半段的前导空白；左半段以 `{` `(` `[` 收尾时再多一级。

| 输入 | 新行缩进 |
|---|---|
| `    foo();` + 回车 | `    `（继承） |
| `    if (x) {` + 回车 | `        `（继承 + 一级） |
| `\tif (x) {` + 回车 | `\t\t`（**本来用 tab 就加 tab**，不换成空格） |
| `foo();` + 回车 | 空（顶层不加级） |

⚠ **刻意不做"语言感知"**：Python 的 `:`、Ruby 的 `do`、Lua 的 `then` 各要一套规则，
而缩进错了比不缩进更烦（用户得手工退回去）。宁少勿错。

顺带把右半段的**前导空白去掉**：那多半是光标后面的分隔空格，留着就等于在新行缩进上又叠一层
（用户看到的是"越缩越深"）。

**设备验证**：`int main(void) {` 行尾按 Go → 7 行变 8 行、光标到 L4、新行内容正好 `    ` ✓

### ② 括号配对高亮

光标贴着 `(` `[` `{` 时，把**与它配对的那一个**也涂上底色（琥珀 30%，与选中蓝/错误红都分得开）。
判据与所有编辑器一致：先看光标**左边**那个字符，不是括号才看**正下方**那个。
跨行用**深度计数**找配对（`f(g(x))` 里"找下一个同款字符"会指错）。

⚠ **配对逻辑不放在画布里**：画布只拿得到"当前可见的那几行"，配对要跨行扫描 ——
放那儿会出现"滚动一下配对就变了"这种最难查的现象。宿主算好两个位置传进去，画布只负责画。
⚠ **配对括号只涂两个字符格、不铺整行**：铺整行会在光标停在一个括号旁边时多出一条横贯整屏的色带。
⚠ **不跳过字符串与注释**（那要真正的词法分析，而这是每敲一个键都要跑的路径）——
代价是 `puts("(")` 这种偶尔标错一次；它只影响高亮画在哪两个格子、不改任何文本。
扫描有 2 万字符上限：宁可不标，也不为一个高亮把输入卡住。

**设备验证**：截图里 L3 的 `{` 与 L6 的 `}` 上各有一块琥珀高亮，且整行底色只在光标行 ✓

### ③ 查找替换（原来是"只有查找"）

☰ 菜单加了「🔁 替换…」：问查找内容 → 问替换为 → 选「替换全部 / 只替换下一个」。
**替换全部压成一个 `EditOp`**（行区间替换天生支持），所以一次撤销就回去了 ——
分成每行一步的话，用户改错一次要按几十下，那是"不敢用"的典型。行数上限 2000，超了请用户缩小范围。
大文件是只读打开的（没有 `_editable`），那时**明说不能替换**而不是静默什么都不做。

**设备验证**：菜单里能看到「🔁 替换…」✓（完整流程需人工点两次对话框）

### ④ 顺带：纯逻辑下沉到 `UI/Shared/TextEditAssist.cs`

自动缩进、括号配对、自动配对的**判据**都从 `EditorPage`（3414 行的 MAUI 文件）挪到了
`WayCoder/UI/Shared/TextEditAssist.cs`。三条理由：

1. **可自测** —— 留在 `EditorPage` 里就没有任何测试碰得到（那个工程不进桌面自测），
   而这些都是"边界一多、肉眼看不出来"的东西（全空白行、`{` 后带空格、嵌套三层、扫描上限刚好卡住）。
   落地即有 **19 条自测**（总数 5943），并且**立刻抓出我自己写错的两条期望值**。
2. **四端共享** —— 桌面 TUI / GUI / Web 的编辑器将来要同样的行为，不必再写一份。
3. **纯逻辑不依赖平台** —— 它只吃字符串、吐字符串/坐标。

### ⑤ 自动配对（已实现，**未设备验证**）

敲 `(` `[` `{` 补右半边、光标停中间；敲的正好是已在那儿的右括号时**跨过去**（否则会变成 `())`）。
判据（`AutoCloseFor`）有自测；但**写入推到下一拍**（`Dispatcher.Dispatch`）——
⚠ 绝不在这条 `TextChanged` 回调栈里回写 `LineEditor.Text`：那会打断 IME 的组合态
（拼音没上屏时 `TextChanged` 就触发了）。判断"刚插进去的是哪个字符"也**必须能证伪**：
`text.Remove(col-1,1) == 上一次的文本` 才算数，只比长度会在粘贴/自动更正/整词上屏时认错。

⚠ **真机只验到"没崩、编辑照常"**：`adb shell input text` 发不出 `(`（被 shell 吃掉），
中文输入法的组合态下会不会误判，**需要人工在手机上敲一次**再算数。

---

## v0.96.234 (2026-09-19) — 全能接口 `CALLJSON`：加功能不用占号

用户提的：「加一个全能接口，参数就是两个字符串（函数名 + 参数 JSON），返回值也是 JSON 字符串，
这样以后可以扩展任意功能，做那些不要求性能的接口，几乎通用」。

### 一个号换掉"加功能"的三件套

在此之前，加一个能力要：**占一个 syscall 号 + 写一对 C 包装 + 重生成 22 种语言的绑定**。
对查版本、报屏幕这类**不要求性能**的功能来说太重了。现在：

```c
char buf[256];
ui_call_json("screen", "", buf, 256);          /* {"ok":true,"result":{"w":395,…}} */
ui_call_json("echo", "{\"n\":7}", buf, 256);    /* 参数原样回来（管线自检 / 调试口） */
```

**宿主侧加一个能力 = 注册一行**：

```csharp
VmlJsonApi.Register("screen", _ => JNode.Object().Set("w", area.Width).Set("h", area.Height));
```

| 号 | 名称 | 入参 | 返回 |
|---|---|---|---|
| **573** | **`CALLJSON`** | R0=函数名\* R1=参数 JSON\* R2=输出缓冲 R3=缓冲容量 | 写入字节数，失败 -1 |

### 五个设计点

1. **结果写进调用方给的缓冲区** —— VM 里没有宿主能"交还"的堆。与 `DLG_INPUT` 返回文本同一套。
   拿不到缓冲区指针的前端（Python / BASIC / Lua…）用 `ui_call_json_s()` + `ui_call_json_len()`
   + `ui_call_json_at(i)` 三个薄封装，连 `char*` 都不用解引用。
2. **信封固定**：成功 `{"ok":true,"result":…}`、失败 `{"ok":false,"error":"…"}` ——
   这是跨语言契约（22 个前端的程序都按它判断成败），自测逐条钉住。
3. **实现抛异常不打挂 VM**：异常在 `VmlJsonApi.Invoke` 里接住、翻成 `ok:false` ——
   与宿主 syscall 处理器"出错记日志并回失败码"那条约定一致。
4. **参数不是合法 JSON 当场报错**，不把半个对象交给实现去猜 ——
   "参数看着像但解析成了别的"是这里最难查的一类。
5. **缓冲区装不下**时写回一段"结果太长（需要 N 字节）"的信封并返回 -1 ——
   不让程序拿到一段被截断的、解析不出来的 JSON 还以为是自己的问题。

### 复用而不是重写

JSON 解析/序列化直接用仓库里现成的 **`Infra/JsonLib.cs`**（手写、AOT 安全、无反射，
`JNode`/`Json.Parse`/`Json.Serialize`）—— 本来就有，没有为这个接口再造一个。
（"动手写新助手之前先 grep 有没有现成的"，本仓库头号坑。）

### 已注册

| 函数 | 结果 |
|---|---|
| `echo` | 参数原样返回（管线自检 + 程序调试口） |
| `version` | `{app, cn, version, platform}` |
| `screen` | `{w, h, orientation, landscape}` —— 与 `SCR_W`/`SCR_H`/`SCR_ORIENT` **同源**（就调那几个函数），免得出现"JSON 报的尺寸和 syscall 报的不一样"这种最难查的分叉 |

⚠ **性能敏感的调用不要走这里**：一次要过两趟 JSON 再穿一次内存拷贝。
绘图、输入这类每帧/每个事件都发生的调用仍然走专用号。

自测 5924 全绿（新增 7 条）。

---

## v0.96.233 (2026-09-19) — 开窗声明（转屏三档 + 要不要手柄）、读消息的「保留位」、宿主支持运行期改场景尺寸

这一版的四件事都来自用户在真机上提的同一条线索：「**横竖出问题**」。
先把症状定位准，再一层层往下挖 —— 最后发现**不只是缺一个接口**。

### ① 开窗时把两个声明一起给出来：`ui_win_open_ex`（syscall #570）

```c
ui_win_open_ex("五子棋", w, h, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
```

| 参数 | 取值 | 含义 |
|---|---|---|
| R3 转屏 | `VML_WIN_PORTRAIT`(0) | **只支持竖屏**（棋盘类）：屏幕锁在竖屏，怎么转都不动 |
| | `VML_WIN_ROTATABLE`(1) | **支持旋转**（默认）：两种排版都写好了 ⇒ 视口一变宿主就换坐标系并通知 |
| | `VML_WIN_LANDSCAPE`(2) | **只支持横屏**（赛车 / 横版过关） |
| R4 手柄 | `VML_WIN_NO_GAMEPAD`(0) | **整块手柄区连同折叠条一起不显示**，画布吃满整屏 |
| | `VML_WIN_NEED_GAMEPAD`(1) | 显示（默认） |

两条都在**开窗之前**生效 ⇒ 程序按 `SCR_W/H` 排的版一开始就是对的，
不会"先按小画布排一次、再收到 resize 重排"。五子棋已改成 `PORTRAIT + NO_GAMEPAD`：
棋盘本来就是竖着看的，而它全程用触摸落子，留一整块手柄区等于白吃一百多像素棋盘高度。

**为什么不直接给 `ui_win_open` 加两个参数**：宿主是从 `registers[3]/[4]` 读的，
而只传 3 个参数的老程序那两只寄存器里是**它自己上一句留下的值**（可能是个指针、可能是个计数），
宿主无从判断"这是不是真给了"。所以新能力一律走新号 —— 与 `ui_msg_clear`(#568) 当初一致。
**老接口一个字的语义都没动。**

### ② 读消息加「保留位」：`ui_poll_ex` / `ui_wait_ex`（#571 / #572）

`VML_MSG_KEEP` = 只**看**队头那一条，队列里一个都不少（下一次读到的还是它，直到明确消费）；
`VML_MSG_CONSUME`(0) 与老接口同义。给"先看一眼再决定谁来处理"用。
⚠ 保留模式**别当循环条件** —— 它永远返回同一条。
队列侧只有一处实现（`TryRead(keep)`），`TryTake`/`Take` 变成它的薄包装 ——
分成 `TryTake`/`TryPeek` 两份的话，信号量那套"投递—唤醒"迟早只修一边。

### ③ 宿主支持**运行期改场景尺寸**（转屏后坐标系跟着换）

老程序转屏后画面会变小：场景尺寸在 `ui_win_open` 之后就固定了，宿主只能把整幅等比缩着显示。
现在**声明了 `VML_WIN_ROTATABLE`** 的程序，视口一变宿主就把新的可用绘图区整个给到场景，
并先发 `WINDOWORIENT` 再发 `WINDOWRESIZE` —— 程序按新尺寸重排版即可，不必拆窗重建（那条路会闪一下）。

⚠ **只给声明过的程序换，是故意的**：不处理 `WINDOWRESIZE` 的程序被换了空间之后会继续按老坐标画，
空间变小 ⇒ **内容被裁掉一大截**（比"等比缩小"更糟）。所以 `WindowRotation` 是**四档**
（Legacy / Follow / PortraitOnly / LandscapeOnly）—— "老程序"必须能和"声明了跟随旋转的程序"分开。
换了尺寸会作废已渲染那一帧（程序可能正卡在 `ui_wait` 上，不会自己重画）。

### ④ 两处「中间态被当成真值」——都是靠真机日志抓出来的

| 症状 | 真身 | 修法 |
|---|---|---|
| 横屏重开游戏，窗口开成 `544x46`、画面是花的 | `MeasuredViewport` 在 `OnSizeAllocated`（**布局期**回调）里记账，转屏时被夹在中间态量到 `545x46` | 上报搬进 40ms 定时器 |
| 转回竖屏后场景变成 `411x780`（真实画布只有 525） | **定时器只保证"不在布局回调内部"，不保证布局已经结束** —— 转屏要连走几趟布局，中间那趟量到 780 | **连续两拍读到同一个值才算数**（中间态最多活一拍就被顶掉） |
| 竖屏打完一局退出、转横屏再开一局，画面还是竖屏尺寸 | `MeasuredViewport` **跨方向陈旧**（转屏期间没有绘图页在跑，没人更新它） | `ViewportMatchesOrientation`：**形状与当前方向一致才算数**，否则退回分方向的估算 |
| 横屏第一局的估算区是 `898×149` 这种又宽又扁的 | `AvailableArea` 一律扣竖屏那套固定占用 | 横屏扣的是**宽度**（手柄在左右两侧）：`LandscapeSideChromeDp` / `LandscapeChromeHeightDp` |
| 画布下沿被「收起手柄」压住 | `FitSize` 用 `CanvasHost` 尺寸，而 `GraphicsView` 自己有 `Margin="8"` | `CanvasBox()` 扣掉 `CanvasView.Margin`（从控件读，不写死） |

### ⑤ 顺带：屏幕方向接口 `ui_orientation()`(#569) 与 `WINDOWORIENT`(#12) 消息

查询是给"开窗前就要决定怎么排版"用的，消息是给"跑着的时候方向变了"用的（**先发方向、后发尺寸**）。
判定规则只有 `VmlUi.OrientationOf` 一处实现 —— 查询与消息必须同源，
否则"查到的"和"收到的"会在某个边界上不一致，那是最难查的一类。
⚠ **别拿 `ui_scr_w() > ui_scr_h()` 去推方向**：那两个数报的是**绘图区**，
会随宿主排版（手柄收起/展开）变，而方向是设备本身的属性。

### 验证（模拟器 Android 16，1080×2400 @2.625）

```
横屏：page=914.3x327.2 host=396.4x301.0 req=374.9x285.0 land=True
竖屏：page=411.4x726.5 host=411.4x525.3 req=395.4x300.6 land=False
收起手柄（横屏）：host=850.5x301.0
声明 ROTATABLE 转屏：scene 跟着变；老接口对照：scene 不动（老行为保持）
A orient=LANDSCAPE / A raw=1 / A wh=WIDE；转屏时 C msg-orient=… → C msg-resize（先方向后尺寸）
```

自测 5917 全绿（新增 20 条）。

---

## v0.96.229 (2026-09-18) — 诊断三色分主题两套（每一档都过 AA）；删掉两处临时探针

### ① 诊断色：一档定死一个色，却要压在**四种底**上

`ErrorWave` / `WarnWave` / `InfoWave` 原来各是一个写死的常量，可它们要同时出现在
**浅色代码底 `#FFFFFF`、浅色面板底 `#F0F0F3`、深色代码底 `#121214`、深色面板底 `#1A1A1E`** 上。
算下来在浅色面板上：

| | 原值 | 浅色面板对比度 |
|---|---|---|
| 警告 <c>WarnWave</c> | `#F5A524` | **1.79** |
| 提示 <c>InfoWave</c> | `#30A46C` | 2.78 |
| 错误 <c>ErrorWave</c> | `#E5484D` | 3.44 |

AA 正文门槛是 4.5 —— 也就是用户报的「日间主题下错误列表看不见」。
深色那边错误色压在深色面板上也只有 **4.43**，同样不达标。

改成**分主题两套**，取值是算出来的（括号内是「面板底 / 代码底」实测对比度，**每一档都 ≥4.5**）：

| | 浅色 | 深色 |
|---|---|---|
| 错误 | `#C4382F` (4.67 / 5.31) | `#FF7B72` (6.88 / 7.42) |
| 警告 | `#8A5A00` (5.21 / 5.93) | `#F5A524` (8.50 / 9.17) |
| 提示 | `#17794A` (4.77 / 5.43) | `#30A46C` (5.50 / 5.93) |

**做成属性、不做成「让调用方各挑一份」**：这三档色被**错误列表 / 错误气泡 / 行下波浪线**
三处共用，让每处自己选「亮版还是暗版」就是三份判据，迟早漏一处（本仓库的头号坑）。

⚠ **琥珀色在近白底上想达标，只能压成深琥珀/棕** —— 这是它的宿命，不是调色失误。
别为了「看着更黄」调回去，那就又回到 1.79 了。这条写进代码注释了。

### ② 删掉两处临时探针（`WCAS` / `WCBK`）

v0.96.226 为查「拖动乱晃」与「擦除键没反应」挂的 logcat 探针，现在结论都已落地成代码注释，删掉：
· `TraceAssist`（4 个调用点 + 方法本体）· `BackspaceAwareEditText.Hook.Trace`（2 个调用 + 方法本体）。
顺带把 3 处注释里对这两个 tag 的引用改成「当时挂了个 logcat 探针量的」——
免得后来的人去找一个**已经不存在的 tag**。源码里 `WCAS|WCBK|TraceAssist` 现在零命中。

---

## v0.96.228 (2026-09-18) — 全屏浮层跟随主题 + 两个按钮粘成一个面板；以及一个「注释自杀」的坑

### ① 全屏那两个按钮：日间主题下**图标是看不见的**

用户报「全屏时的按钮颜色没有跟随系统」。根因不显然 —— 图标是 SVG 里**烘死**的 `#777777` 灰
（与工具栏同一批），而底色是写死的 `#99000000`（60% 黑）。底色叠在**代码底**上，
于是两种主题下它的实际颜色完全不同：

| | 叠加后实际底色 | 灰图标对比度 |
|---|---|---|
| 日间（白代码底） | `#666666` | **1.28** ← 等于看不见 |
| 夜间（近黑代码底） | `#070708` | 4.50 ✓ |

所以「夜间正常、日间看不见」不是错觉。底色改成跟随主题（浅 `#E9E9EE` / 深原值），
量过：浅色底上图标 3.70、深色底 4.50，两端都过图形元素的门槛（3.0）。

### ② 两个按钮**粘成一个面板**

用户：「全屏时 2 个按钮应该做圆角粘在一起，像一个 panel 上面的两个按钮」。
原来它们是两个各自 44×44 的方块、中间隔着 8pt。现在底色与圆角挪到外层一个 `Border` 上、
两个按钮自己透明、中间一道细分隔线把两格分开。

分隔线的可见性跟着「运行」那格走（非源码文件时那格是隐藏的，留着一条孤零零的竖线像坏了）——
同步放在 `ApplyActionButton` 一处，那本来就是「这一格长什么样」的唯一出口。

### ③ 一个连踩两次的坑：**XAML 注释里写了注释结束符**

改完之后构建报：

```
error MAUIX2002: No accessible property ... found for "Children"
```

**没有行号**；换成旧版 `XamlC` 编译器（`-p:MauiXamlInflator=XamlC`）也只有 `(-1,-1)`。

真因是我在注释里多写了一个注释结束符 —— 注释在那里就闭合了，
**后面几行说明成了 `Grid` 的裸文本**。而报出来的「`Children` 找不到」完全指不到病因。

更讽刺的是：**修的时候我又在注释里写「多写一个结束符会出问题」**，那个字面量本身
把注释闭合了第二次。同一个坑连踩两次。

定位手段（可复用）：**二分注释掉整块是没用的** —— 散落的文字本来就在块外。
真正管用的是写个扫描器：逐行跟踪「当前是否在注释里」，把**注释外、又不以 `<` 开头**的行打出来，
一眼就看到那两行说明。另一个教训：中途用 Python 改这个文件时把行尾转成了 LF
（`open(p, encoding=...)` 会把 CRLF 读成 LF，也就是把整个文件的行尾全换掉）——要靠 **byte 级备份**还原。

> **规矩：XAML 注释里绝不能出现注释结束符本身，哪怕只是想举例说明它。**

---

## v0.96.227 (2026-09-18) — 面板 Tab 的白字、设置里「存储」与「编辑器」拆开

v0.96.226 装到真机后立刻报出来的两条。第一条是同一种毛病的**第二次露面**：
代码里写死的颜色把 XAML 上的主题绑定盖掉了。

### ① 日间主题下，面板 Tab 白底白字 —— 而且**只有选中的那一格**

用户的原话：「编辑器下方的输出和错误列表，如果是焦点状态，在日间主题，看不见了」。
根因一行：

```csharp
PanelTabErrors.TextColor = errs ? Colors.White : Color.FromArgb("#999999");   // 写死
```

`SwitchPanelTab` 把选中态的颜色**写死成 `Colors.White`**，正好盖掉 XAML 里挂的 `AppThemeBinding`；
日间主题下面板底色是近白 ⇒ 选中的那一格直接消失。
**未选中的 `#999999` 在白底上还看得见 ⇒ 现象是「时有时无」**，很容易被当成偶发。

改法两条：颜色按 `IsDarkTheme` 取（与气泡、辅助条弹表**同一口径**）；
并且把那两个 Tab 在 XAML 上的 `TextColor` **删掉** —— 选中态是**运行期状态**，就该只由一处决定，
两处都给等于「谁生效看执行顺序」。顺带在 `OnAppThemeChanged` 里补了重刷（换主题时选中态也要跟着变，
这正是 v0.96.226 刚修过的那类问题）。

**教训**：给控件在 XAML 挂了主题绑定之后，**页面代码里就不要再给它赋颜色** ——
赋值会静默胜出，而且只在运行到那一行之后才胜出。

### ② 设置里「存储」与「编辑器」拆开

用户：「设置里面的存储和编辑器要分开，不要合并到一起，编辑器以后还要增加很多设置的」。

原来首页那颗卡片叫「存储与编辑器」，点进去一页里塞了**三块**：存储 / VML 游戏画面 / 编辑器。
现在：首页拆成 **🗂 存储** 与 **📝 编辑器** 两张卡片（各有摘要），
编辑器独立成组 `GrpEditor`，路由 id `editor`（`settingsgroup?group=editor`）。

拆的时候顺手消掉一处**即将出现的平行表**：编码/换行的候选标签原本写在设置页里，
而首页摘要也要显示同样的标签 —— 两处各写一遍迟早漂。已挪到
`MauiEditorStore.SaveEncodingOptions` / `SaveNewlineOptions` + `NameOf()`，**下拉与摘要共用同一份**。

### ③ 量出来但**这次没动**的一条

诊断色压在浅色面板 `#F0F0F3` 上的对比度都不够（AA 正文门槛 4.5），
而错误列表那些行的文字用的就是它们：

| | 色值 | 对比度 |
|---|---|---|
| `ErrorWave` | `#E5484D` | 3.44 |
| `WarnWave` | `#F5A524` | **1.79** |
| `InfoWave` | `#30A46C` | 2.78 |

这三个色号被**错误列表 / 错误气泡 / 行下波浪线**三处共用，改一处三处都跟着走。
没动的原因是它要连带影响 v0.96.226 里 ⑦ 那套「按 HSL 保饱和度」的取色路径，
想单独作为一条改（用户未表态）。

---

## v0.96.226 (2026-09-18) — 辅助输入条：拖动乱晃查到底、处处可拖、跟随系统主题

这一批全落在移动端编辑器上。起点是用户的三句话：「辅助输入条拖动时还是乱晃，这个一直没解决」、
「只有关闭按钮位置不可拖动，其他地方都应该能拖动」、「代码文字色彩要考虑不同颜色主题下面的效果」。
过程里有**两个「以为是手感问题、其实是机制问题」**的东西被量到了底 —— 两条都不是调参数能解决的。

### ① 拖动乱晃 —— 不是手感，是 `PanGestureRecognizer` 上报的**两条流**

用户给了一句决定性的对照：**选区手柄拖动不会乱晃**。于是先量后猜（logcat tag `WCAS`）：

- 手指快甩那次：`TotalX` 是**两条各自平滑、相差约 50 DIP 的轨迹在交替**，两条都在正确跟手；
- 再用 `adb` 注入**匀速慢划（1200ms、纯程序、没有人手）**：**照样 ±39 来回** —— 与速度、与人手都无关。

成因是「两个各记各的起点的监听器」，而 `PanUpdatedEventArgs` 只有 `TotalX/TotalY`、
**没有任何字段能把它们区分开** ⇒ 这条路修不动。

换到用户指的那条路上：**`GraphicsView` 亲手接原始触摸**（`Start/Drag/EndInteraction`）——
它给的是**绝对坐标**（不是「相对某个起点」），天生免疫这类问题；画布的选区手柄走的正是这条路。

坐标换算有一条必须写对：拖动面**自己会跟着条子动**，所以拿到的是「手指相对条子」的位置。
位置公式是 `T += (t − t₀)`（每帧补一次当前偏移误差），
写成 `T = T₀ + (t − t₀)` 会**卡住不动**，写成纯增量 `T += (t − t_prev)` 会退化成「动一帧停一帧」的半速抖动。

新增 `Controls/AssistDragSurface.cs`：**期望尺寸恒为 0** 的 GraphicsView。
第一版直接用裸 `GraphicsView` + `Fill`，它把期望尺寸报成「可用空间的全部」，把整条 Border 撑成**近全屏**
（用户报「辅助输入条几乎全屏了」）。教训是「测量」与「排布」是两件事：
测量只决定**父容器**长多大，排布才决定自己占多大。

### ② 处处可拖 —— 按钮**从来就吞掉了触摸**

用户要「只有关闭按钮位置不可拖动，其他地方都应该能拖动」。实测（从「关键字」按钮起划）：**pan 事件 0 个** ——
Button 是 `clickable`，Android 的分发在子元素消费到就 break，底下那层拖动面永远收不到。
也就是说**按钮上从来就拖不动**，能拖的只有 ⣿ 那一小块（这也解释了用户为什么会猜「是不是拖动区太小」）。

做法：六个格子全部 `InputTransparent`，触摸穿到拖动面；**点击改由拖动面按坐标派发**
（`AssistHitTargets` 的矩形直接取各按钮的 `Bounds`，与按钮自己的 `Clicked` 调**同一个 `AssistDo*`**，
不存两份实现）。加 8 DIP 的位移门槛区分「点」与「拖」—— 不看门槛的话，手指按上按钮那一瞬间的抖动
就会被当成拖动，按钮再也点不准。`✕` 是**唯一整次手势都不拖**的一格。

### ③ 不依赖输入法的退格键 ⌫

用户自己找到了真因：**部分输入法的退格只作用于它自己的联想词库、从不回调编辑框**
（症状是「候选区随退格在变、文本一个字不动」）。那种情况下 `InputConnection` 这条路**根本够不着** ——
它不调用我们，拦也没得拦。所以浮条上给了一个应用自己的 ⌫：有选区删选区 → 行中删一个**码点**
（emoji / CJK 扩展 B 是两个 `char`，按码元删会劈成半个）→ 行首与上一行合行。
平台那条拦截**保留**，它管的是另一批会正常回调的输入法。

### ④ 弹表三处

| 症状 | 真身 |
|---|---|
| 往上弹时离浮条老远 | 高度**写死 220**（注释还写着「与 ScrollView 的上限一致」，而那个上限早改成了 165），真实高度取决于**排了几行** ⇒ 改用 `IView.Measure` 实量 |
| 符号按钮偏高、运算符正常 | `FlexLayout.AlignContent` **默认 `Stretch`**，会把**行**拉伸去填满容器：符号 22 项行少、每行被拉高；运算符 31 项行多、本就接近自然高度 ⇒ 加 `AlignContent="Start"` |
| 「缩到 3/4」看着没变小 | 只改了宽。全局隐式样式的 `MinimumHeightRequest=44` 没被覆盖，高度一点没动 |

### ⑤ 关键字高频优先

用户：「关键字应该分类，首页是高频关键字，那样才会输入快，现在要去找，输入麻烦」。
原来按字典序排，`auto`/`break`/`byte` 挤在前面，找个 `while` 得扫一遍。
加一份**跨语言共用的高频优先级表**（控制流在最前），它**只用来筛选和排序各语言自己的词表，
不是第二份关键字表**（那种平行表迟早和 `Syntax` 漂开）；高频项**底色更实一档** ——
不靠位置暗示，因为几十个格子排下来「前几个」和「后面几个」看着是一样的。
⚠ 匹配必须**不区分大小写**：词表按各语言书写惯例存（BASIC/Fortran 大写），
区分大小写的话 BASIC 的 `IF` 一个都排不到前面。

### ⑥ 跟随系统主题 —— `SetDark` 早就写好了，**从来没有人调用它**

用户：「改了系统白天黑夜，编辑器内容不变色，要退出文件重开才变色」。
真身：画布的 `_isDark` 只在 `SetDocument`（打开文件）时传进来一次，且**行缓存里每个段的颜色是建的时候就烘进去的**。
`CodeCanvasView.SetDark()` 连「必须连行缓存一起清」都写对了，但唯一的调用者是……没有。
`OnAppThemeChanged` 里当时只重建了气泡，注释还写着「画布不在这次范围内」。

同批把**选区操作条（复制/粘贴那条）与运行面板**也改成跟随主题（原来底色/文字全是写死的深色），
按钮收成 `SelChip`/`PanelChip` 两个样式 —— 原来颜色在十来个按钮上各写一遍，改主题要改十处。

### ⑦ 浅色主题的语法配色 —— 第一版**做错了**

用户先报「白天主题下标识符偏淡，旁边行号却正常」：标识符用的是 `Syntax.Identifier = 253`
（xterm 亮灰 `#dadada`），那是照**深色底**挑的（One Dark 系），白底上几乎和纸一样白；行号走另一套 `GutterFg` 所以正常。

第一版按 **RGB 等比缩放**去压暗 —— 等比会把**色差的绝对值一起缩小**，于是发灰，
用户的原话是「所有的颜色都变淡了」。正解是在 **HSL 里只动 L、原样保留色相与饱和度**：
颜色还是那个颜色，只是变深。再加一道按 **WCAG 相对亮度**的兜底（二分降 L）——
因为 **HSL 的 L 不是感知亮度**，青/绿在同样的 L 下亮得多，不兜底的话运算符（青）只有 3.20。

改完实测（算出来的，不是估的）：12 个色号在白底上**全部 ≥4.61**（AA 门槛 4.5），
**饱和度一格没掉**（`#FF8787` 100%→100%、`#D787D7` 50%→50%）。
顺带发现 `Syntax.cs` 里「关键字 `#c678dd`」那句注释是**陈旧的** —— 按真源
（`AnsiTty.Xterm256ToRgb`，`Level(v) = v==0 ? 0 : 55+v*40`）实际出来是 `#D787D7`。

### ⑧ 零碎

- 符号表补 `/` 与 `\`（路径、注释、转义都要用，而手机符号键盘上它们藏在很后面）。
- `⌫` 挪到 `✕` 左边（左侧是抓握/拖动区，退格杵在那儿时「想拖条子」很容易顺手删掉一个字）。
- `⌫` 与 `✕` **单独收小并去掉外框**：App 全局 `Button` 隐式样式设了 `BorderWidth=1` 与
  `MinimumHeight/WidthRequest=44`，只改 `Padding`/`FontSize` 压不下去，而 **44×44 就是它们的实际可点范围**（误触的来源）。
- 判「再点一次同一个分类就收起」原来用**列表引用相等**，而关键字那一路每次现排一个新 List
  ⇒ 那条规则对它从来没生效过。改成比**分类标签**。
- 拖动/退格的诊断日志（logcat tag `WCAS` / `WCBK`）**有意保留**：只在手势发生时打 2~3 行，
  下次这条线出问题一条 logcat 就能定位。

---

## v0.96.217 (2026-09-18) — 手机编辑器：能跑代码、能删干净、报错画在出错那一格上

用户一口气提了这一批，都落在移动端编辑器上。核心是两件：「写代码不能跑」与「删除是坏的」，
其余是配套。**真机跑通之后又按实测补了三处**（见 ⑦）。

### ① 「运行」按钮 —— 那一格按钮**按文件类型变形**

工具栏第 5 格原来只对 Markdown 有用（非 md 点了只弹「仅 Markdown 文件支持预览」），
源码文件等于摆了个空占位。现在：**Markdown → 👁 预览 / 源码 → ▶ 运行 / 其它 → 整格隐藏**。

判据只有一份 `ActionForCurrentFile()`，**工具栏、☰ 菜单、全屏浮层三处都问它** ——
本仓库的头号坑就是「同一规则两处实现」，而这里最容易出现的就是「工具栏点得动、全屏点不动」。

**全屏也给了运行按钮**（用户要求「写了代码就运行一下」）：全屏时工具栏整条被藏起来，
原先只剩右上角一个还原按钮，现在是「▶ + 还原」并排。

### ② 删除是坏的 —— 三处，其中两处是**平台层面的**

| 症状 | 真身 |
|---|---|
| 行首退格删不掉换行 | `Entry` 是**单行控件**，光标在第 0 列时按退格文本不变 ⇒ `TextChanged` 不触发 ⇒ 什么都不发生 |
| 选中文字后删除无效 | 选区是**画布级**的，平台那层输入框根本不知道有选区 |
| 浏览态退格无反应 | 没在编辑任何一行时输入框是隐藏且未聚焦的 ⇒ 按键没有落点 |

- **跨行退格/Delete**：新增 `BackspaceAwareEditText`，覆写 `OnCreateInputConnection` 包一层
  `InputConnectionWrapper`，**三条路都拦**（`deleteSurroundingTextInCodePoints` / `deleteSurroundingText` /
  `sendKeyEvent(KEYCODE_DEL)`）—— 软键盘的退格**根本不走 `KeyPress`**，这是原来收不到它的原因。
  平台视图按 `StyleId` 分流：非编辑器那一支返回**原类型**，App 里另外那些输入框一个字节都不变
  （`NoSelectionToolbar` 那次「改了所有 Entry」的事故不能重演）。
  门控是 `SelectionStart == 0 && before > 0 && !composing`；光标在第 0 列时平台的
  `deleteSurroundingText` 本来就删不掉东西 ⇒ **就算判断错了也不会比现在更糟**。
- **选区删除**：选区条加「删除」。它与上面那条 InputConnection 路径**共用同一个
  `DeleteSelectionCore()`** —— 两边各写一份的话，跨行/单行的边界处理迟早只改对一边。
- **浏览态**：不做「按一下退格就自动进编辑态」（那是用户没要求的模式切换），
  改成把「没反应」变成「说清为什么没反应」。

**顺带挖出一个更严重的**：打字的编辑**根本没进撤销历史**。`OnLineEditorTextChanged` 是逐键
直接写模型的、不压栈，等 `CommitEditingLine` 才补压 —— 而它的「旧值」是从**已被改写过的模型**
里读的，于是 `oldText == newText` 恒成立、函数在第一道判断就提前返回。修法是进编辑态时
把那一行的**原始内容**记进 `_editLineStart`（旧值只有在那一刻才拿得到）。多行粘贴与回车
两处「先替换、后读旧值」的同型写法一并改正。

### ③ 全角标点自动转半角 —— **避开字符串与注释**

中文输入法打出的 `，。；：（）` 在代码里直接是语法错误。现在源码编辑时自动转半角，
**但中文字符串与注释里的标点原样保留**（`printf("你好，世界")` 里的「，」不该被动）。

- 映射表是 `Infra/FullWidthText.cs`：`U+FF01..FF5E` 整段减 `0xFEE0`（含全角字母数字），
  `。、""''〜` 单独映射。**刻意排除**全角空格 `U+3000`（转掉是改坏内容）、`「」《》【】`、
  `—…·`（没有 ASCII 对应物，猜一个等于静默产出错代码）。
- 保护区间由 `Syntax.ProtectedSpans` 给（复用语法高亮的 tokenizer，颜色即类别）。
- **映射严格 1 字符换 1 字符 ⇒ 长度不变 ⇒ 光标下标永不需要换算** —— 这条性质撑着
  「只把归一化后的文本写进模型与画布、**不回写**平台输入框」（回写会打断中文输入法的组合态）。
  四个写回点（打字/提交/回车/粘贴）收口到一个 `CurrentEditedText()`。
- 局限写在明处：逐行 tokenize 判不出跨行块注释与三引号字符串，故加了一条保守兜底
  （受保护区间占半行以上就整行不动）。

### ④ 编译报错气泡 —— **浮在代码上，箭头指向出错那一格**

带箭头、右上角 ✕、可同时多个（**堆叠不重叠**）、**错误红 / 警告黄 / 其它绿**。

**没有另建数据源**：编辑器里本来就有一套完整的诊断基础设施（`Diagnostic` 模型带行列、
三档严重度、行下波浪线渲染），而 `WayCoder.Maui.csproj` 单独放行了 `UI/TUI/Edit/`，
所以它在移动端是**真实现**、只是从来没有数据喂它。这版只给它加了一个外部注入入口，
于是**气泡与波浪线读同一份数据** —— 关掉一条气泡，那一行的波浪线一起消失。

- 箭头定位走画布**那把唯一的尺子**（`TryGetCellAnchor` → `GutterWidth + TextLeftPad − 横向滚动
  + MeasurePrefixWidth`），与光标、波浪线起点**逐像素同源**，不会「箭头指这儿、波浪线画那儿」。
- 排布是一个算法三个消费者（气泡之间、气泡与全屏浮层、气泡与选区条共用同一张避让表）。
- 调试构建里有「🧪 注入测试诊断」，让气泡的观感/堆叠/避让/关闭**不必等一分钟的编译**就能验收。

### ⑤ 报错显示在编辑器，运行却交给命令行页 —— **只编一次**

气泡要求在编辑器里就地报错，而运行链路（stdin 交互、可中断、绘图窗口、崩溃 dump）都在命令行页。
两边各编一次的话，手机上就是**两个一分钟**（前端编译实测一分多钟）。

解法：编辑器编完之后把**已链接的自包含汇编文本**随作业一起带过去（`PendingVmlJob.PrebuiltVml`），
命令行页只付一次**汇编**（秒级）。走的是既有的 `MauiVml.Run(source:)` 那条路 ——
零新增执行路径、零新增 VM 管线。交接本体也提成了 `ShellPage.HandOff`（**唯一实现**，
文件页与编辑器共用），不再是两份。

### ⑥ 状态栏诊断计数 + 保存编码/换行

- 状态栏加「❌ 警告：n个，错误：m个」（有提示档时补一段）。**放在前面** —— 这行本来就长，
  排到末尾先被挤出屏幕，而「有几个错」正是扫一眼就想要的信息。
- 设置里加**保存源码时的编码**（保持原样 / UTF-8 / UTF-8-BOM / UTF-16LE / OEM）与
  **换行**（保持原样 / LF+CR / LF）。**只对源码生效**：Markdown、纯文本、数据文件一律沿用原样。
  两个默认值都是**「保持原样」** —— 默认设成某个具体编码等于「打开一个 GBK 老文件、随手保存
  就被悄悄转成 UTF-8」，那是改动了用户没打算改的东西。

### ⑦ 跑通之后按真机实测补的三处

**这批东西只有上手机才会暴露，桌面全绿一个都照不出来。**

- **运行改成「编辑器内就地跑 + 底部面板」**（用户要求）。原先成功后要跳到命令行页，
  用户的原话是「编辑器运行程序时可以在底部开个小窗，显示 shell 的内容，2 个 tab，
  一个显示错误列表，一个显示 shell 输出」。
  查下来这条路是通的：绘图窗口（`DrawWindowPage`）是 Shell 路由、由 `MauiBootstrap` 打开，
  `VmlUiCalls` 也不引用 `ShellPage` —— **宿主能力全是进程级的，谁发起运行都一样**；
  而执行路径用的还是同一个 `MauiVml.Run`，不是第二份实现。编好的产物直接喂给它，不重复编译。
  新增的只有 stdin 输入行（与命令行页同一语义）与一个「■ 停止」（**连等输入时的阻塞也要一起放行** ——
  只取消 token 是没用的，那时 VM 线程正阻塞在 `readLine` 里，看不到 token）。
- **气泡的箭头原来是个菱形**。我用了「旋转 45° 的方块」想让它露出一半当三角 —— 但尾巴是
  竖排里**相邻的独立元素**，谁也没裁它，整块都看得见。改成真正的三角形（`Polygon` 三个顶点）。
- **气泡右边缘超出屏幕、文字被切掉**。根因是我对 MAUI `Grid` 的假设错了：**Grid 会把没填满
  单元格的子元素居中**，不是放在 (0,0)；气泡宽度通常小于屏宽，于是它先被居中、我们算好的
  `TranslationX` 再叠上去，整体右移了半个余量（实测算出来该在 89dp、实际落在 135dp）。
  修法是显式 `HorizontalOptions=Start` —— `SelectionBar` 当年也是为这个写的，不是巧合。
  顺带把宽度算式收严：原来那个 `Math.Max(160, …)` 的**固定下限**在窄屏上会反过来把宽度撑到
  比可用宽度还大，改成「上限 = 画布宽 − 16」才真正保证出不了屏。
- **气泡文字跟代码连成一体**（用户要求「和文字一般大、一起放大缩小、一起滚动」）。
  文字大小直接取编辑器字号（原来写死 12pt，与正文 10.7pt 对不上），尾巴尺寸按字号等比缩放；
  捏合改字号时**整体重建**气泡 —— 它的字号、每行字数、行数估算、尾巴尺寸全都是从字号算出来的，
  逐项改属性不如重建可靠（没有气泡时是极便宜的早退）。滚动跟随本来就有（`ViewChanged` 重排）。
- **气泡的「语义」按用户的口径重做：它是代码的一部分，不是浮层**。原话是「和代码的坐标绑死，
  就像代码的一部分，文字也是一样大的，一起缩放一起滚动的，只不过是在绘制在代码上层」。
  按这条把三处「为了迁就视口而改变位置」的行为全删了：
  **不翻面**（原来「下方放不下就翻到上方」—— 同一个错误在不同滚动位置会显示在代码的不同侧）、
  **不做视口避让**（被工具栏/面板/选区条盖住就盖住，那些是**界面外壳**，本来就画在代码层之上）、
  **收起前先占位**（原来滚出视口就 `continue`，那个位子一空，排在后面的气泡整体挪窝 ——
  滚动时看着就是在跳）。现在**位置先全部算完、再决定画不画**，位置只由代码坐标决定。
  另外按用户要求把气泡从「错误行下方」改到**上方**：盖住的是已经读过的那行，
  而读代码是从上往下的。
- **气泡文字色跟随系统主题**：`EditorTypography` 按本文件 `Xxx / XxxDark` 的既有惯例加了一对；
  主题判据收成一处 `IsDarkTheme`（原来气泡色、画布 `dark`、Markdown 预览的 `isDark`
  是各写一遍的同一句判断）；并挂了 `RequestedThemeChanged` 即时重建 —— 颜色是建的时候取的，
  不重建的话开着编辑器切主题会一直用旧色。亮色档给**深字**（橙黄底 `#F5A524` 配白字对比度只有约 1.9）。

### ⑧ 浮动代码辅助输入条（新控件）

手机上打字，括号分号要切符号键盘、关键字要一个字一个字敲。这条浮条把最常用的那批摊在手边：
`[TAB][符号][运算符][关键字][⣿拖动区][✕]`。

- **可自由拖动**：`PanGestureRecognizer` **只挂在拖动区那一小块上** —— 挂整条的话
  「想点按钮却把条子拖跑了」；松手钳进可视区（拖出屏幕就再也点不到了，而它没有别的入口）。
- **10 秒不碰自动缩小半透明**（用户指定）：`IDispatcherTimer` 单次触发 → `Scale 0.72 / Opacity 0.45`；
  **任何交互都把计时重新起表**。
- **点分类弹表，上/下方**：弹表紧贴浮条上方，放不下就翻到下方；内容 `ScrollView` + `FlexLayout(Wrap)`
  自动折行，关键字多也不怕。
- **点内容插入**：与「粘贴到光标处」**同一条路** —— 没在编辑就先落到光标那一行，再写进 `Entry`，
  于是全角归一化等既有规则一并生效。
- **关键字/运算符按识别到的语言自动填充**：关键字直接问 `Syntax.ForFile(...).Keywords`
  （与语法高亮同一份词表，**没另立一张表**）。
- **分类不是非此即彼的，分不清就两边都加**（用户定的规则）：`?` 单独是标点、`(a)?b:c` 里是三元
  运算符；`!` 单独像标点、`!x` 里是逻辑非；`<` `>` 既是泛型尖括号又是比较运算符；`|` 既作分隔
  又作按位或。这些在两张表里**都保留** —— 硬归一边，总有一种写法在表里找不到。

### ⑨ 补上 10 门 VML 语言的关键字表（顺带修好它们的高亮）

辅助输入条本来就是按语言取词表的，但 `Syntax.ForFile` 只认约 40 个扩展名，而 VML 支持 **22 门**
语言 —— `.lua` `.pas` `.bas` `.r` `.d` `.f90` `.dart` `.m` `.forth` `.ladder` 这些**它压根不认识**，
落到 `Plain()`（词表为空）⇒ 显示的是通用兜底表，**对 Lua/Pascal/Fortran 就是错的表**。

补法是给 `Syntax` 加这 10 门语言的词表并让 `ForFile` 认它们的扩展名。**不是新开一份平行表**：
高亮层本来就是自己一套手写词表（C#/Python 那些同样与 VML 的 lexer 各存一份），这里只是按既有设计补齐。
顺带修好两件事：这些文件**从「整片不上色」变成有语法高亮**；`IsSourceFile()` 也认它们了 ⇒
全角转半角、保存编码/换行这些「只对源码生效」的设置对它们一并生效。

⚠ 如实记一条限制：关键字匹配是**区分大小写**的（`Keywords.Contains(word)`，ordinal）。
词表按各语言书写惯例存（BASIC/Forth 大写、Fortran 小写），用户按另一种大小写打字时高亮不命中 ——
既有匹配机制的限制，不影响辅助输入条（它插入的是表里的原样文本）。

### ⑩ 编译体验

- **不再为一行字闪一整屏**（用户反馈「体验不好」）：编译进度改到**底部面板的输出里滚**，
  代码全程看得见。只在缓存放一行 `⏳ 正在编译…` 作为即时反馈（`OnProgress` 最早也要等解压
  标准库那步才开口，那之前可能已经过去几秒，屏幕上不动会让人以为没点上）。
- **编译也能停**：停止键原来只作用于运行，而**编译比运行慢得多**（手机上要一分多钟），
  现在它同时取消编译与运行，谁在跑停谁。
- **编译期间锁编辑、保留滚动**（用户要求）：这期间改了代码，结果回来时气泡与错误列表指向的
  就是**已经不对的行号**。锁的是编辑不是滚动 —— 等的时候还能翻代码看。

---

## v0.96.214 (2026-09-18) — 手机端 AI 能**看见**自己写的游戏 + 聊天页「发了没反应」的真身是**滚屏**

起因是用户那句「让手机 AI 写个历险游戏，不能卡死闪退」。真机一测，两件事都不是它看起来的样子：
「卡死」大半是**看不见**，「写游戏」缺的是**AI 没有眼睛**。

### ① 「发了没反应 / 卡死」= 聊天列表**永久停止跟随**（不是 AI 卡住）

实测：发一句 `hello`，AI **3.2 秒**就回了（`你好 👋 有什么需要我做的？`），token 从
273,124 涨到 282,988 —— **一切正常**。但屏幕上纹丝不动，新内容全落在视口下方，
只靠一个 `↓` 按钮提示"下面还有"。

真身是 `ChatPage.ScrollToEnd()` 开头那道门：

```csharp
if (Messages.Count > 0 && _isNearBottom)   // 只有"接近底部"才跟随
```

这道门本意没错（上翻历史时别把人拽回去），**但它没有回程**：`_isNearBottom` 只在
`Scrolled` 事件与点 `↓` 按钮时被写，一旦因上翻变成 false，**之后任何新消息都不再触发滚动**
⇒ **一次上翻 = 永久停止跟随**。连自己发的消息也滚不动 —— 发送处那句注释写着
「保证刚发的消息可见」，而它调的正是被门挡住的 `ScrollToEnd()`。

**修法**：新增 `ForceScrollToEnd()`（无视门控 + 重置标志），用在**用户主动发送**的两个点上。
「用户上翻时不打断」的语义对自动跟随仍然保留。

### ② AI 看得见画面了：`vml` 工具跑完自动导一帧 PNG

原先 `vml` 工具**只回控制台文本**，而游戏是画出来的 —— **「程序没崩」不等于「画对了」**：
东西画在哪、颜色对不对、有没有该出现却没出现的，文本里一个字都没有。

链路（每一环都核过判据）：
`ui_present` → `VmlScene.PresentedDsl` → 【新】`VmlUiCalls.TryGetPresentedDsl()` →
`DrawRunner.Parse` → `ToPng` → 写 `vml_frame.png` → 提示 AI 用 `view_image` 打开。

- **用 `PresentedDsl` 而不是实时图元**：后者可能拍到画到一半的半成品场景
  （窗口渲染那条链正是为此加的 `PresentVersion`）。
- **`view_image` / vision 注入在 MAUI 端本就完整可用、没被裁**；用户配的模型
  `deepseek-v4-flash-vision-exp` 也落在 `ResolveSupportsVision` 的家族子串表里，
  不会被静默丢弃。
- 已知限制：程序**自己调 `ui_win_close`** 会立即清空场景，那一帧导不出来（多数游戏是被
  超时/返回键结束的，场景仍在）。

### ③ 导出开关 + 最大边长（用户要求：能关、能限）

`VmlExportFrame`（默认**开**）、`VmlFrameMaxSide`（默认 **1024**，超了等比缩小），
桌面设置页（Schema）与手机设置页（`SettingsGroupPage`）都有入口。

**性能上是零开销设计**：先比 `doc.Width/Height` 再决定要不要缩 —— VML 窗口常规
320×480（程序在 `ui_show_window` 里指定），**正常路径连一次解码都不会做**；
只有真超限才走「解码 → 最近邻缩放 → 重编码」。满盘场景整条导出约 70~110ms，
且**每次工具调用只做一次**（不是每帧），相对同一次调用里的编译（几秒~一分多钟）
与运行（≤60s）可忽略。

### ④ ⚠ `make-vml-lib.sh` 的 `zip -x` 位置写反 ⇒ **从 Mac 打 APK 必然失败**

```bash
zip -q -r -X "$TMP" "${ZIP_EX[@]}" Lib vmltool.config.xml   # ✗
```

Info-ZIP 把 `-x` 之后**所有不以 `-` 开头的参数**一律当成排除模式 ⇒ `Lib` 与
`vmltool.config.xml` 这两个**本该打包的目标**被当成排除项，一个文件都选不中，报
`zip error: Invalid command arguments (nothing to select from)`。而 `build-apk.sh`
第 39 行先调它、又是 `set -e` ⇒ **一步都走不到 `dotnet publish`**。
（`MOBILE_EXCLUDE` 为空时 `-x` 根本不出现，所以这个 bug 只在加了排除项后才暴露。）
**修法**：`-x` 必须跟在要打包的路径**之后**。

### ⑤ 顺带查清：手机上的包是**另一台机器的密钥**签的

`adb install -r` 报 `INSTALL_FAILED_UPDATE_INCOMPATIBLE`。对比证书：**DN 完全相同**
（`CN=WayCoder, OU=Dev, O=WayCoder, L=Shenzhen, ST=Guangdong, C=CN`）**但 SHA-256 不同**
（手机 `7598f13e…` / 本机 `14ab383e…`）—— 同一个 DN 生成了两把密钥。
`build-apk.sh` 注释里写着 keystore 被 `.gitignore` 排除，所以它**不随仓库同步**。
⚠ **此时千万别顺手 `adb uninstall`**：先确认数据位置 —— 本机实测用户的
`api_keys.json` / `config.json` / `sessions/` / `memory/` **全在外部存储**
（`/storage/emulated/0/waycoder/config/.waycoder/`），卸载重装不影响，只丢 app 私有目录的
界面设置与 vml 库解压缓存。

## v0.96.213 (2026-09-18) — macOS(MacCatalyst) 与 iOS 端 VML 游戏跑通 + 音效验证；**Xcode 27 把 Simulator.app 换成了 DeviceHub.app**

这一版**没有改任何生产代码** —— 产出是「两端跑通并验证音效」这个结论，外加三条**下次一定会再撞上**
的环境事实。排查用的探针加了又撤，工作区回到干净状态（`git diff` 为空）。

### ① 两端都跑通了，能玩完整的一局

- **macOS（MacCatalyst）**：`WayCoder.app`（`-f net10.0-maccatalyst27.0`）。五子棋跑完整局 ——
  棋盘/黑白棋子/落子标记/「电脑赢了。再来一局？」对话框/屏幕手柄/「▲ 收起手柄」折叠条全正常；
  另跑了贪吃蛇、吃豆人。
- **iOS**：iPhone 17 Pro 模拟器（iOS 26.5，`-f net10.0-ios27.0`）。五子棋同上，中文渲染与手柄区一致。
- 两端 `VmlAudio` 都走 `#elif IOS || MACCATALYST` 那一支，日志实测 `分支=APPLE` + `engineRunning=True`。

### ② 「游戏正常、就是没声音」的真因**不在代码，在增量构建**

链路本身是通的。加探针重新构建后，日志立刻变成：

```
[VmlAudio] Tone hz=880 ms=25 分支=APPLE
[VmlAudio] ToneCore 已调度 hz=880 ms=25 frames=1102 engineRunning=True
```

**探针不改变任何逻辑**，两次构建唯一的差别就是「重新构建了一次」⇒ 先前那次是**增量构建没把
`VmlAudio` 的新代码编进去**。**下次遇到「改了没生效 / 编译全绿但行为是旧的」，先 `-t:Rebuild`
或清 `obj/` 再谈其他。**

排查途中还踩了一个**自己造的坑**：`dotnet msbuild -getProperty:DefineConstants` 的输出里**只有**
`__MACCATALYST__` 而没有 `MACCATALYST`，据此差点判定「符号没定义、静默走了空实现分支」。
**那个属性不等于编译器实际收到的全部符号，别拿它当判据** —— 要判就用 `#if` 探针实测
（本次正是靠探针才把结论从「编译期」拨回「构建缓存」）。

### ③ ⚠ Xcode 27 把 `Simulator.app` 换成了 `DeviceHub.app`（环境变更）

全盘 `find` + `mdfind`（按名字、按 `com.apple.iphonesimulator`）都找不到模拟器 ——
**这不是装坏了，是 Xcode 27（2026-06）的有意变更**：

| | Xcode 26 及以前 | Xcode 27 |
|---|---|---|
| 应用 | `Xcode.app/Contents/Developer/Applications/Simulator.app` | `Xcode.app/Contents/Applications/DeviceHub.app` |
| bundle id | `com.apple.iphonesimulator` | `com.apple.dt.Devices` |

**后果**：`open -a Simulator`、按旧路径 `open`、以及 `xcodebuild -downloadComponent Simulator`
（**该组件类型根本不存在**，合法值如 `MetalToolchain`）全部失败。**驱动模拟器要 `open`
那个 `Contents/Applications/DeviceHub.app`**；`xcrun simctl` 一侧一切照旧（boot/install/launch/
io screenshot 都正常，只是它**不提供触控命令**）。
另：升级 Xcode 时旧 Simulator 进程可能变成**孤儿**（bundle 已被删、进程还活着，`ps` 里有路径
但那个文件不存在）—— 别被它误导去找那个路径。

### ④ MAUI 的 Apple TFM 名字里带着 SDK 版本

`-f net10.0-maccatalyst` 会报 `NETSDK1005`（assets 里没这个目标）。真名是
**`net10.0-maccatalyst27.0`** / **`net10.0-ios27.0`** —— csproj 的 `AppleSdkVersion` 被拼进了 TFM。
查法：`obj/project.assets.json` 的 `targets` 键，或 `dotnet msbuild <proj> -p:TargetFramework=… -getProperty:DefineConstants`。

## v0.96.212 (2026-09-17) — 吃豆人「按键有反应、人不动」的根因在 **C 前端**：初始化器里的负数被编成 0

用户真机上「手机版 WayCoder 用 VML 的 C 写游戏，写着写着不行了」这条线，最后落在
**一个与游戏无关的编译器缺陷**上。这一版把它连同吃豆人自己的两处缺陷一起清掉，
按用户的规矩重生成全库、跑回归网，并交付一个能玩的《吃豆人》。

### ① 根因：**全局数组初始化里的负数被静默编成 0**（正数全对）

```c
int A[4] = {  0,  1,  0, -1 };   /* 程序实际读到  0  1  0  0 */
int B[3] = { -5,  7, -9 };       /* 程序实际读到  0  7  0    */
int C[4] = { 10, -20, 30, 40 };  /* 程序实际读到 10  0 30 40 */
```

**正数一个不差、只有负数错** ⇒ 光看源码矩阵永远看不出来。

**真身是兜底分支吃掉了整类节点**：摊平初始化器的两条路
（全局 `FlattenArrayInitializer`、`static` 局部 `FlattenArrayInitValues`）只认
`NumberLiteral`/`CharLiteral`/`StringLiteral`/`Identifier`，**其余一律 `result.Add(0)`**；
而 `-3` 根本不是 `NumberLiteral`，是 `UnaryOp("-", NumberLiteral(3))`
（`Parser.Expressions.cs` 的一元分支）⇒ **负数整类变 0**。同类还有 `1+2`、`~0`、`1<<3`。

**影响面**：所有用「负数常量数组」的程序，**方向表首当其冲** —— 吃豆人的

```c
int DX[4] = { 0, 1, 0, -1 };   /* 编成了 {0,1,0,0} */
int DY[4] = { -1, 0, 1, 0 };   /* 编成了 {0,0,1,0} */
```

⇒ **只剩「右」「下」两个方向存在**。用户看到的正是「按键有反应（`pwant` 确实在变）、
但人不动」—— 那个方向的速度是 0。

**修法**：新增 `VMLPrepares/CCompiler/ConstFold.cs` —— 一元/二元**常量折叠**
（`-1`、`1+2`、`~0`、`1<<3`、比较与逻辑），两处摊平函数接上。整数运算走 **`unchecked`**
（= C 的 int 回绕语义，`int x = 0xFFFFFFFF` 就是 -1）；**与 `Parser.TryConstInt` 刻意不同**
—— 那个用于**数组维度**必须 `checked` 报溢出（patches/0018），两处需求相反，
是两份实现而非重复。

**闸门 + 反证**：`vml-out-probe` 的 `nat.c` 里 `OUT-INT` **不写字面量 42**，而由
`int BASE[2] = { 45, -3 }` 加出来 —— 负数一丢就变 45。**已把闸门改回去反证过**：
`nat.c FAIL OUT-INT=45` ✓。**不响的自测比没有更糟**，所以这一步不能省。

### ② 吃豆人自己的两处缺陷（症状同样是「人不动」，但各自独立）

**位置模型**：`near_center` 错了两轮 —— ① 判据写成「坐标是 `TILE` 的整数倍」，
那是**格线**，而吃豆人停在**格心**；② 改对判据却仍「一拍直接走 `spd` 像素」，
于是 `col_of()` 一越过格线就报下一格、「前面是墙」随即在**离格心半格**处把人钉死
（实测 `pdir` 恒 1、`pxp` 恒 **316**，而那一格的格心是 **325**）。

正解：**逐像素推进 + 精确格心判定**。从格心出发、每次只走 1px ⇒ **必然精确经过格心**，
与速度无关（旧写法在 `spd` 不整除 `TILE` 的关卡还会累积漂移）；鬼用同一套走法。
**「判定、吸附、停下」必须是同一个位置** —— 三件事分家就会出现这种最容易被误判成
「按键没收到」的症状。

**难度**：自动试玩（贪心走向最近的豆子）量出 4000 拍**死 42 次**（约 4 秒一条命）。
补上原作就有的**追击/游走交替**（每 300 拍切一次，游走时各回自己的角）⇒
死亡率 0.0105 → **0.0073 拍⁻¹**（约 −30%）。

### ③ 库全量重生成 + 回归网（用户定的规矩）

「改过编译器 ⇒ 所有库必须重新生成 ⇒ 生成完必须跑共享库单元测试」：

- `genlib -b`：**106 编译 / 0 失败**
- HEAD 里的库产物比编译器现在的输出**旧**（`sub R13 #28 → #32` 是「栈帧向上对齐 8 字节」
  那次、`.linked "string.vml"` 是聚合清单那次）—— 那两次改完编译器**都没重生成库**，
  这一版一并补上，**124 个文件**
- `test_shared/run.sh` → **3 PASS / 0 FAIL**（上一版 2 PASS / 1 FAIL）
- `scripts/vml-out-probe/run-langs.sh` → **28 / 28**（上一版 27/28）

### ④ 交付《吃豆人》（`Examples/c/pacman.c`）

19×21 迷宫、193 颗豆、4 只鬼（带惊吓/吃鬼）、3 条命、过关升级、音效震动、
分数与最高分存档；布局**随可用区域自适应**（`TILE = min(横, 竖)`，两个维度都管，
不会被切）。**自测是「跑一遍才算数」的那两类**：

- **泛洪验证**：从出生点 BFS，断言每颗豆子都走得到 —— `豆子总数=193 可达=193 全部可达 OK`、
  `鬼出生点不可达数=0`（布局死角只有跑一遍才暴露，看矩阵看不出来）
- **行为验证**：跑 400 拍断言「人真的在动、豆子真的在少」；四方向全过
  （(9,19)→(17,19)→(17,1)→(11,1)→(11,4)）

### ⑤ 移动端一批体验修复（同版进包）

- **设置页分级**（用户选的两级导航）：`SettingsPage` 改为入口页（六行 + 实时值摘要），
  原内容移到 `SettingsGroupPage`（按 `?group=` 只显示命中那一组）
- **聊天页顶部两个图标大小不一** → 改对等的 `ImageButton`；输入框左侧 `+` 按钮
  `VerticalOptions` 由 `End` 改 `Center`
- **命令行 tab 图标** `⌨️` 在 Android 上渲染成方块 → 换 `💻`（tab 图标须取 U+1F300+ emoji 段）
- **编辑器**：全屏时**连底下 TAB 栏一起隐藏**（原只收 NavBar）；行号栏右侧留白减半；
  两条滚动条往边缘再靠、**交叉处互相让位**、**5 秒无触摸自动隐藏**（一次性 timer，
  不留常驻定时器）
- **绘图窗口去掉 `ScrollView`**（用户：「手指一拖有时就滑动了」）——
  缩放本来就由 `FitCanvas` 按视口算，ScrollView 只会把触摸抢走

### ⑥ 示例解压闸门**只判版本号** —— 改示例必须同时改版本

打了两版 APK 之后才发现新示例到不了手机：`MauiBootstrap.EnsureExamples()` 的闸门是

```csharp
File.ReadAllText(marker).Trim() == WayCoder.Global.Version
```

**只判版本、不看内容指纹**（与 `MauiVml.EnsureLibExtracted` 用 `vml_lib.hash` 判内容
**不是一回事**）⇒ 版本号不变就**不会重新解压**。包里明明有 `Examples/c/pacman.c`
（`make-vml-lib.sh` 的输出逐条列了），手机上 `examples/c/` 里就是没有，
而 `adb shell ls` 一看「确实没有」极易误判成「打包漏了」。
**改示例 = 改版本号**，两件事要一起做 —— 已写进 CLAUDE.md 的「示例进包规则」③。

### 结果

| 项 | 状态 |
|---|---|
| 负数常量复现 | `{0,1,0,-1}` → `0 1 0 -1` ✓（修复前 `0 1 0 0`） |
| 输出探针 | **28 / 28 全绿**（且反证过闸门会响） |
| `test_shared/` | **3 PASS / 0 FAIL** |
| 共享库 | 已用最新编译器全量重生成（106/106 干净） |
| 吃豆人 | 泛洪 193/193 可达；四方向与 400 拍行为验证全过 |
| 真机 | 已装 **v0.96.212**（`com.tanso.waycoder`）；⚠ **画面观感未在真机验证** |

### 文档

- 新增 `docs/VML游戏开发指南.md`：骨架 / 接口清单 / 三条硬约束 / 坑表 /
  **§6.5「走格子：一像素一判，只在格心做判定」**（把这次两次错法连同现场数据写清楚）/
  调试手法（**「源码看着对、跑起来不对」时把程序实际读到的数据打出来**）
- 手机端 SKILL.md 同步更新（`~/.waycoder/skills/vml-game/SKILL.md`）
- CLAUDE.md 新增 ㉗ 条；⑱ 条（`third_party/vml` 与上游分家）补上 `FORK.md` 的指引

## v0.96.211 (2026-09-17) — 输出探针走到 **28 / 28 全绿**；用修好的编译器把共享库**全量重生成**

承接上一版，把最后三条清掉，并走完用户定的那条规矩：
**「改过编译器 ⇒ 所有库必须重新生成 ⇒ 生成完必须跑共享库单元测试」**。

### ① 括号嵌套解析失败 —— **两个独立的判据各漏一格**（4 个库源因此一直编不出完整产物）

`(v * (s))` 这类形状解析失败，而且走 **`[RECOVER]` 静默路径**（把异常吞成一行日志继续编），
卡着 4 个库源：`color.c:62`、`fixed64.c:38`、`signal.c:84`、`graphics.c`。

**怎么定位的**（这一步是转折点）：8 格真值表先把形状收窄成「括号组内部、二元运算符的
**右**操作数又是括号组」，与括号里的内容无关；然后在 `ParseUnary` 的 LPAREN 分支与
"括号表达式"回落下**各插一行日志** —— 对照用例 `(v * s)` **两行都打**，
失败用例 `(v * (s))` **只打第一行** ⇒ 病在**转换判定块内部**，由此排除了
`ParseMultiplicative` / `ParseExpression` / `ParsePrimary` / 二元循环右操作数四处。

两个根因（都在 `Parser.Expressions.cs` 那段"类型转换 vs 括号表达式"消歧里）：

| # | 位置 | 漏了什么 |
|---|---|---|
| ① | `(identifier *)` 判定（注释写着"`*` 后面是数字或标识符则是乘法"） | **漏了 `LPAREN`** —— `*` 后跟括号表达式同样是乘法操作数。于是被判成"未知类型标识符" ⇒ 当转换处理 ⇒ 吃掉 `v` 再要求 `)`，撞上内层 `(` 抛「期望 RPAREN，但是 LPAREN」——**错误位置正好指向内层括号** |
| ② | `(identifier)` 那张"不是转换"的运算符清单 | **漏了 `RPAREN`** —— `(s)` 后面紧跟 `)` 时被判成类型转换 |

⚠ **这两个坑互相掩盖**：修完 ① 之后 `(v * (s + 1))`、`(v * (1))` 才好；而此前它们也失败，
**导致我早前把 ② 那条判据证伪得太早**。教训很具体：**一次只修一半时，另一半的失败会把结论带偏**
—— 这次是靠"修完 ① 立刻重跑整张真值表"才把 ② 逼出来。

**验证**：8 格真值表全过；**4 个库源全部不再出现 `[RECOVER]`**（color 1141 / fixed64 381 /
signal 1129 / graphics 4702 条指令）。

### ② `console` 不在聚合清单里 —— C# 的 `Console.Write/WriteLine` 全解析不到

`out.cs` 报 `未找到标签: PrintlnStr`（最后在**运行期**抛，编译链接都过）。查下来：

- C# 前端把 `Console.Write/WriteLine` 编成 **PascalCase 标签**
  （`CSharpCompiler/CodeGenerator.cs` 的 `memberLabels`：`PrintlnStr`/`PrintStr`/`PrintlnInt`/`PrintInt`），
  并注明「标签定义在 `Lib/csharp/console.vml`」
- 而 `GenLib` 生成 `builtin.vml`（**编译器自动链接**的那个聚合文件）时的模块清单里
  **没有 `console`** ⇒ 那个模块**永远不会被链进来** ⇒ 程序里只有调用点、没有定义
  （实测 `out.cs` 链的 43~51 个模块里**始终没有任何 console 模块**）

**改法**（按用户早先那句"GenLib 有问题就改 GenLib"）：`GenLib/Program.cs` 那份清单里补上
`console`，再 `genlib -a` 重生成 **22 个聚合文件**。
⚠ 我中途还往映射表里补了那 4 条 `PrintlnStr→console` 映射 —— **那不是修复**
（补完 `out.cs` 仍然失败），真正起作用的是聚合清单；那 4 条保留但已在提交里标明。

⇒ **输出探针 28 / 28 全绿。**

### ③ 用修好的编译器**全量重新生成共享库**

`touch Lib/shared/src/*.c` + `genlib -b` → **完成: 106 编译 / 0 跳过 / 失败 0**
（对比修复前那次全量：4 条 `[RECOVER]` 解析错误）⇒ **68 个产物改动**。

跑完用户要求的回归网：`test_shared/` 2 PASS / 1 FAIL、探针 27/28 —— **与重生成前逐条一致 ⇒ 无回归**。

⚠ 两条操作要点：**`genlib -b` 是增量构建**（`.vml` 比 `.c` 新就跳过），
所以"全量"必须先 `touch`；而它的汇总行 `完成: N 编译, M 跳过` **极易被误读成
"N 成功 / M 失败"** —— 本会话就误读过两次。另外 **GenLib 必须重建**（它进程内调
`CCompiler`，只重建 `vmlcli` 不算）。

### ④ 栈帧向上对齐到 8 字节 —— `ltoa` 曾编出 25 字节的奇数帧（**修对一半**）

最小复现：`char b[8]; ltoa(5L, b);` → `MOVEL @13, R0 写内存失败: 内存越界`。
而**用户自定义的 `long` 形参函数完全正常** ⇒ 差别只在这个库函数身上。

**真因**：`convert64.c` 的 `ltoa` 里有一句 `for (...) { char t = dst[j]; ... }` ——
**块内的 1 字节局部量**。而 `VarMemManager.LocalFrameSize`（`= -_localBottom`）按局部量的
**实际大小**累加、**不做对齐** ⇒ 帧算成奇数、实测编出 `sub R13 #25` ⇒ **SP 不在 4/8 字节
边界上** ⇒ 紧随的 `movel`（8 字节访问）地址算错。旁边 `atol` 是 `sub R13 #28`（对齐的）
所以没事 —— **同一文件里两个函数，只有带 `char` 局部量的那个崩**。

**改法**：发 `sub R13 #N` 时把 `stackAlloc` 向上对齐到 8（放在这处而非改 `LocalFrameSize`，
后者还被别处用来算偏移）。**改完 `R0` 由垃圾值（`499602D2`）变成正确的 `5`**
⇒ 对齐这一步确实是真问题；**但 `MOVEL @13` 仍然崩** ⇒ 还有第二层没解决，如实记着。
回归网：探针 **28/28 无回归**（这改动影响所有函数的帧大小，必过）。

### ⑤ 顺带

- **更正一条自己的误诊**：先前报的"`itoa_hex` 栈损坏"是错的 —— `itoa_hex` 单独调完全正常
  （实测 `2 [FF]`），崩溃其实来自我 `test_shared/convert.c` 末尾的 `ltoa` 那行。
  当时顺着"最后一条 FAIL 停在 `itoa_hex` 上"归因，而那是修 `char h[]="..."` 之前的状态，
  它的 FAIL 把真正的崩溃点挡住了。**崩溃点 ≠ 出错点**，得用最小复现去逼。
- **移动端库排掉 PC/DOS/单片机**（用户要求）：`crt`/`dos`/`vga_text`/`conio`（DOS 与 PC 文本控制台）、
  `graphics`/`graph`（BGI 绘图）、`browser_gfx`（浏览器 canvas 专用）、`gpio`（单片机引脚）。
  每个模块有**两份**（`shared/X.vml` 实现 + `<lang>/X.vml` 转发 shim），**两份都排**；
  zip 与 Python 两条打包路径都改。重建后 **5101 条目、排除清单零残留**。
  ⚠ `device`/`device64` **没排**（名字像硬件层但可能是 VM 自己的设备抽象，排错会让绘图/音效失灵）。
- **`test_shared/` 铺到 3 个用例**：`vsnprintf`（格式化族）、`string`（字符串族，约 40 条断言）、
  `convert`（转换族，约 50 条断言）。判据是**自己算一遍再和库的返回比**、
  **同时查返回长度和逐字节内容**、**不经过 stdio**。写用例时踩到三处自己的 bug
  （两个字面量写在同一条语句里**不共享地址** ⇒ 指针相减得垃圾；`str_charat` 越界
  返回 0 是**源码写明的契约**不是我猜的 -1）—— 都记进用例注释了：**定期望前先读被测函数的注释/实现**。
- **`atoi` 全库三份实现去重**：`convert.c`（跳空白+认 +/-）、`util.c`（**不跳空白、不认 +**）、
  `builtins.c`。模块映射指向 `convert`，但 `util` 也被链进来，而**重名标签按链接顺序后者覆盖**
  ⇒ 实测生效的是**最弱的那份**（`atoi("+7")=0`、`atoi("   42")=0`）。删掉两处冗余。

### 结果

| 项 | 状态 |
|---|---|
| **输出探针** | **28 / 28 全绿**（本会话从 17 一路走到全绿） |
| `test_shared/` | 2 PASS / 1 FAIL（= ④ 那条 `ltoa` 的第二层） |
| 共享库 | 已用最新编译器全量重生成（106/106 干净） |
| 移动端库 | 已排 PC/DOS/单片机 7 类，5101 条目 |

## v0.96.210 (2026-09-17) — 输出探针 17 → **28 / 28 全绿**：三条真前端缺陷 + 一套判别顺序

承接上一版，把剩下的 4 条逐门清掉。**这一版最有价值的不是修好的那几条，
而是把"是探针坏了还是语言坏了"这个判断做成了可复用的顺序** —— 17 条里
**只有 3 条**最后被证明是语言的问题，其余全是探针自己写错，而它们**伪装得一模一样**。

### ① 三条真的前端缺陷（都不是探针）

| 语言 | 现象 | 真因 |
|---|---|---|
| **Rust** | `println!("OUT-INT={}", 42)` 打出 `OUT-INT=`，**整数整个丢了** | 解析器产出 `Type = "int"`（`Parser.Expressions.cs:284`），而 CodeGenerator 判的是 `literal.Type == "integer"` —— **两个字符串对不上** ⇒ 整数分支永远不进；而这个 if/else 链**没有兜底 `else`** ⇒ 静默产空。字符串/浮点/字符/布尔的类型名恰好都对得上，**所以只有整数被丢**。旁边那句注释还写着「忽略多余的参数（Rust 的 println! 会忽略）」——**这个前提是错的**：`{}` 是占位符，实参要**代入** |
| **Python** | `print("OUT-INT=", 42)` 打出 ` 1036` | 实参读取位置**整个反了**：实参从右往左压栈 ⇒ arg_i 在 `[R13 + 4*i]`，而代码算的是 `(Args.Count - 1 - i) * 4`。**`Args.Count == 1` 时 `(1-1-0)*4 == 0` 恰好对** ⇒ 单实参一直正常，只有多实参炸。把 42 当**字符串指针**打、把字符串指针当**整数**打 |
| **Forth** | 每一行多一个**前导空格** | `Lexer.cs` 的 `TokenizeDotString` 只跳了 `.` 和 `"` —— 而 Forth 的 `."` 是「词 + **空格分隔符** + 文本」，那个空格**不属于字符串内容**（`." abc"` 打出的是 `abc`） |

三条**都靠"看生成的汇编"抓出来**（`--vml` 导出）：
- Rust：汇编里**从来没有那次 `print_int`**（只有 `print_str` + `newline`）
- Python：汇编里**两个实参的读槽是对调的**（`4(R13)` 读到 42 却走 `syscall #1`）
- Forth：原始字节 ` OUT-STR=abc\n OUT-INT=42\n...` —— **每行都以空格开头**

⚠ **三者的共同点是编译链接全绿。**

### ② 判别顺序（这才是 17 → 28 真正靠的东西）

写进 `run-langs.sh` 文件头：

```
① 看 stderr —— 编译期抛异常 ⇒ 先修探针（ERR 档），别去看输出
② 编译过了但零输出/输出怪 ⇒ 看生成的汇编（--vml 导出）
③ 只有当汇编里那一处调用/取值确实错了，才轮到怀疑语言
```

配一个判据上的关键区分：**"分隔语义不同"要给 `.expect`，"该有的空格却错了"才是缺陷。**
Go 的 `println` / Python 的 `print(a,b)` 在操作数间插空格 —— 那是**正确**语义（配 `.expect`）；
Forth 的 `."` 多一个**前导**空格 —— 标准里不该有，所以是缺陷。两者长得几乎一样。

### ③ 剩下的探针问题（本轮清掉）

- **`nat.kt`**：`println("OUT-INT=", 42)` ⇒ `Expected ')' at 4:23, got Comma`
  —— Kotlin 的 `println` **只吃一个实参**。拆成 `print` + `println`。
- **`out.f90`**：`print '(A,I0)', ...` ⇒ `expected * after print (got StringLiteral)`
  —— 本前端**只支持表控输出 `print *`**。改成表控后实测 `OUT-INT= 42`（项间插空格），
  那正是 **Fortran list-directed 的正确语义** ⇒ 配 `out.f90.expect`。
- **runner 自身**：`*.expect` 被 glob 当成了探针去编译（已排除）。

### ④ 事故：我把 runner 的头部注释写坏了

用 `python3 -c "...反引号..."` 改文件时，**反引号被 bash 当成命令替换**，
把命令的输出塞进了注释（那行成了 `# · Fortran 前端**只支持表控输出 Î޷¨³õʼ»¯É豸 PRN**`，
`Î޷¨³õʼ»¯É豸` 是 `无法初始化设备 PRN` 的 GBK 糊码），另一行还丢了 `#`。
已修回并在文件头记下：**别用带反引号的 shell heredoc 改它；改完必须回读**。

⚠ 这是本会话**第三次**踩"脚本写文件没落地/写坏"（前两次：`nat.c`/`nat.cpp` 的 `\n`
转义修复没落地、四个探针的 heredoc 重写没落地 —— 后者害我误报过"nat.kt 已改"）。
CLAUDE.md ⑰ 早就记着「脚本改完必须 grep 回读，别信自己的打印」，三次都是事后核对才发现。

### 结果

`scripts/vml-out-probe`：**17 → 28 / 28 全绿**，编译失败 0。

## v0.96.209 (2026-09-17) — 逐门语言校准输出探针：17 → 24 / 28，其中一条是真前端缺陷

上一版把 C/C++ 的 printf 修通之后，接着逐门检查「各语言自己的标准格式化输出」。
这一段**几乎全是探针自己的问题**，而每一类都长着同一张脸：**编译链接全绿**。

### ① runner 最大的盲区：把「编译失败」报成了「输出为空」

探针一直是 `2>/dev/null`，于是 **5 个编译期就抛异常的探针被报成"输出为空"**，
读起来像"这门语言的打印坏了"。现在 stderr 单独接住，**编译失败单列一档 `ERR`**：

```
通过 24 / 输出不符 2 / **编译失败 2**（共 28 条）
⚠ 编译期就抛异常的探针（先修探针/前端，还没轮到看输出）：out.f90 nat.kt
```

**"修编译器"和"修探针"从此不会混在一起。** 这与 `nat.c` 那次「把 stdio 的毛病
看成 codegen 的毛病」是同一个毛病：**判定链上先分清是哪一段坏了**。

### ② 三类「看着像语言坏了、其实是探针」

| 形态 | 伪装成 | 判据 |
|---|---|---|
| 注释前缀写错 | "运行了没输出" | stderr 单接 → `ERR` 档 |
| 拿别的语言的换行/分隔语义套这一门 | "输出格式错" | `<探针>.expect` |
| 不声明 native ⇒ 间接 `CALL R0` | "JS 前端零输出" | **看生成的汇编** |

- **注释前缀**：第一轮给 Python/R/Ruby/Scheme/Lua 修过 `//`，**漏了三个** ——
  `out.ld`（Ladder 不认 `//` 且要求第 1 行 `BEGIN`）、`out.pas`（Pascal 报"第1行13列未知字符"）、
  `nat.py`（Python 不认）。三个都是**编译期就抛**，修完都 PASS。
- **分隔语义**：Go 的 `println`、Python 的 `print(a,b)` 在操作数间**插一个空格**
  （`OUT-INT= 42` 才是它们的**正确**输出），Kotlin 的 `println` **只吃一个实参**。
  统一成一行是错的 ⇒ 新增 `<探针>.expect` 覆盖默认期望。
- **`out.js`**：编译运行全绿（**87224 条指令 / 5.6 秒**）却零输出 —— 看汇编一眼可辨：
  ```asm
  move R1 var_println_str     ; 把**变量**装进 R1
  move R0 @1                  ; 从变量槽读一个值
  call R0                     ; **间接调用**，变量未定义 ⇒ 跳野地址
  ```
  而同一函数里的 `print("OUT-INT=")` 被正确解析成 `call lib_console_print_str`。
  `corpus/javascript/skel.js` 里**逐字记着**这条：本前端对不认识的函数名会先找 `func_<名>`，
  都没有就把名字当变量 ⇒ 修法是先声明 `native function ... {}`。

### ③ 一条真的前端缺陷：`println!` 的整数格式实参被静默丢掉（Rust）

`out.rs` 的 `println!("OUT-INT={}", 42)` 打出 `OUT-INT=` —— **整数整个丢了**。
生成的汇编里三次 `println!` **从来没有 `print_int`**（只有 `print_str` + `newline`），
而 `{}` 确实被当成占位符吃掉了（输出里没有 `{`）—— 只是**代入实参那一步什么都没产出**。

**真因是类型名字符串对不上**：

| 出处 | 整数类型名 |
|---|---|
| `Parser.Expressions.cs:284`（`"long"` 在 :285） | `"int"` |
| `CodeGenerator.FormatHelpers.cs` 判的 | `literal.Type == "integer"` |

`"int"` ≠ `"integer"` ⇒ 整数分支**永远不进**，而这个 if/else 链**没有兜底 `else`**
⇒ **静默产空、实参凭空消失**。字符串/浮点/字符/布尔的类型名恰好都对得上
（`"string"`/`"float"`/`"char"`/`"bool"`），**所以只有整数被丢**。
旁边那句注释还写着「如果还有未使用的参数，忽略（**Rust 的 println! 会忽略多余的参数**）」——
**这个前提是错的**：`{}` 是占位符，实参要**代入**。

改法：判据补 `"int"`/`"long"`，且解析失败时**抛异常而不是静默丢**。

⚠ **这与本仓刚修的 C++ 前端 `printf` 是同一族**（`ce.Arguments.Count > 0` 只取第 0 个实参）：
**格式串旁挂的实参被静默丢掉，而编译链接全绿。** ⇒ 判据必须落在**生成的汇编**上
（"有没有那次 `print_int` 调用"），不能停在"编译通过了"或"跑完没输出"。

### 结果

`scripts/vml-out-probe`：**17 → 24 / 28**（`nat.c` / `nat.cpp` / `out.ld` / `out.pas` /
`out.js` / `nat.kt` / `out.rs` 七条转 PASS）。剩下 4 条：`out.f90`/`nat.kt` 编译失败、
`out.fth` 第二行多一个前导空格、`nat.py` 多实参 `print` 打出 ` 1036`。

## v0.96.208 (2026-09-17) — printf 为什么一个字都不输出：两份实现 + 一个后缀匹配

接着 v0.96.207 往下查「各语言自己的标准输出函数」。C 的 `printf` **一个字都不输出**
（但**执行继续**，后面的 `puts` 照常）。查到最底下是**两个互不相干的缺陷叠在一起**，
外加一条把两者都藏了很久的东西。

### ① `#include <stdio.h>` 会把 C 的 printf 导到**第二份实现**上

`Lib/c/stdio.h` 里有一句 `#param lib("stdio_funcs")`，把 `c/stdio_funcs.vml` 拉进链接。
那份文件（41763 字节、**连 `.c` 源都没有**）里**只有 printf 家族** ——
`printf`/`sprintf`/`snprintf`/`shared_vsnprintf`/`_vformat_buf`/`_put*`，
**一个独有的 stdio 函数都没有**。名字叫 stdio_funcs，内容纯粹是 printf 的第二份实现，
而且它是坏的。

**隔离证据**（同一份 C 代码，唯一差别是那个 include）：

```
不带 stdio.h → call lib_printf_printf        → 三行全对 ✓
带   stdio.h → call lib_stdio_funcs_printf   → 一字全无 ✗
```

C 前端里**没有任何 printf 特殊处理**（已确认），差别纯粹来自链接进哪个模块 ——
`globalLabelMapping["printf"]` 被后链接的 `stdio_funcs` 覆盖了。

**这正是用户最初那句「为啥 printf 有 2 份实现？只要一份就行」的真身。**
按「相同的函数只留一份」删掉：`stdio.h` 去掉那句 `#param`、删 `c/stdio_funcs.vml`、
**d/dart/fortran/objc/r/ruby 六门语言也在链它** ⇒ 改为链 `../shared/printf.vml`。

### ② 链接器用**纯后缀匹配**找"CALL 目标"，把 `_printf_itoa` 认成了 `itoa`

`LibraryLinker` 的「情况2」（已解析到包装器 → 找更好的实现体）：

```csharp
if (target.EndsWith("_" + bareName) && target.StartsWith("lib_") && ...)
    operand.Value = bestImpl;      // 把 CALL 换成"更好的实现体"
```

`shared/src/printf.c` 的 static 助手叫 `_printf_itoa`，链接后是 `lib_printf__printf_itoa`
（前缀 `lib_<库文件名>_`）—— 它 **`EndsWith("_itoa")`** ⇒ 被当成「itoa 的包装器」，
**整个调用被重定向到 `convert.c` 的 `itoa(int value, char* dst)`**：
一个**参数顺序完全相反**的函数。`itoa(42, tmp)` 把 42 当目标地址去写 ⇒
`%d` 返回垃圾长度、`tmp` 没被填 ⇒ **printf 的所有 `%` 转换静默失效**，
而字面量正常（那条路只经过 `emit`，不经过它）。

**四条证据合拢**，第三条最说明问题 —— 源码里留着一行前人的补丁：

```c
// 32-bit itoa (base 2-16) — 前缀 _printf_ 避免与其他库冲突
```

**这个前缀就是为了躲开 `itoa` 冲突而加的**，而 `EndsWith` 把那个规避手段整个架空了。

诊断路径也值得记：先在 `_printf_itoa` **内部**插桩 → **一个字都不打**，
而在调用它的 `vsnprintf` 里插桩正常打印 ⇒ **调用根本没进那个函数**；
再插桩读数 `len=5 val=42 t0=0 t1=0` ⇒ 实参是对的、缓冲区没被填。
**"改对一处"不等于修好** —— 中途我改过一版"取最长匹配"，没修好，因为
`globalLabelMapping` 里只有 `itoa` 这个键、没有更长的 `_printf_itoa`；
那一版是**未经验证的链接器行为变更**，已撤回，没留在树里。

**正解**是判据带上**模块边界**：target 要**恰好**是 `lib_<模块>_<裸名>`
（模块名 = 库文件 basename，与 `libPrefix` 同源）。`lib_builtins_itoa` 照旧重定向，
`lib_printf__printf_itoa` 需要模块名 `printf__printf`（不存在）⇒ 不再误伤。
顺带把「break 在字典遍历顺序第一个匹配」改成取最长匹配 —— 原实现的结果
**取决于 Dictionary 的枚举顺序**，本身就是不确定行为。

### ③ `%%` 被当成"需要实参的转换"，没有实参时被静默吃掉

`vsnprintf` 里「读取参数值」那段排在 `%%` 分派**之前**：

```c
} else if (ai < nargs) { val = args[ai]; ai++; }
else { fmt++; continue; }        // ← 没有实参就静默跳过这个转换
```

`%%` 是唯一**不消耗实参**的转换 ⇒ `printf("100%%")` 会走 `fmt++; continue`：
吃掉第二个 `%`、**一个字不产出、pos 也不增、零报错**。实测 `sprintf(b,"4-sp=%%d",42)`
打出 `4-sp=d`。改法是把 `%%` 提到「读取实参」之前（一行）。

### 结果

| | 修前 | 修后 |
|---|---|---|
| `vsnprintf(b,"N=%d",args,1)` | `len=7`、缓冲区 `N=` 后跟一个 0 | **`len=4 buf=[N=42]`** |
| `vsnprintf(b,"P=%%",args,0)` | `len=2 buf=[P=]` | **`len=3 buf=[P=%]`** |
| `sprintf(b,"4-sp=%%d",42)` | `4-sp=d` | **`4-sp=%d`** |
| `nat.c`（带 stdio.h 的 C printf） | **一个字都没有** | **三行全对** |
| `nat.cpp` | `OUT-INT=` 后面什么都没有 | **三行全对** |

`scripts/vml-out-probe`：**17 → 19 / 28**（`nat.c` 与 `nat.cpp` 转 PASS），**无回归**
（`out.d`/`out.dart`/`out.fortran`/`out.objc`/`out.r`/`out.ruby` 六门全通过，
那正是改用 `shared/printf.vml` 的六门）。链接日志里「最终修复重定向数」由 101 → 65。

### 顺带清掉的死代码，以及**两个被推翻的模型**

- **`c/*.c` 不是"第二份实现"** —— 读了 GenLib 本体（`tools/GenLib/Program.cs:92`）：
  它的输入是 **`Lib/shared/src/*.c`**，`<lang>/*.c` 是 shared 重构之前的**遗留死源码**，
  既不进链接产物也不参与 shim 生成。已删（11 个）。
  **而且它们是地雷**：`build_libs.sh` 的 Phase 3 是 `c/*.c → c/<name>.vml` 无差别遍历，
  跑一次就会把 GenLib 的 shim 覆盖掉。删完 `c/` 下已是零个 `.c`。
- **撤回「29 模块各装载两次 ⇒ 定义两遍」** —— 第二次装载的是 **GenLib shim**，
  它**转发**而不重实现。据此报的「71 组两份都可达」跟着不成立。
- **撤回「指针形参被当成数组」** —— 复现写错了（`int rd0(int p){return p[0];}` 在 C 里
  本身非法），用正确的 `int *p` / `const int *p` 重测全部正确。
  ⇒ **撤掉 `c/string.*` 的删除**：支撑它的模型不成立，而 `c/string.vml` 是 shim、
  导出的 102 个 `c_*`/`func_*` 别名是 `shared/string.vml` **没有**的，
  探针全绿只说明探针没用到它们、**证明不了无害**。

### GenLib vs build_libs：谁该留（实测）

让 GenLib 重新生成 `c/` 的 74 个模块 → 与签入的**零差异**（幂等、权威）。
`build_libs.sh`/`.ps1` 重复实现了 GenLib 的 `-b`/`-m`/`-a` 三个阶段（且是更旧的语义、
缺 `-g`/`-n`），除历史 CHANGELOG 外**没有任何代码/CI/文档引用** ⇒ 真正的冗余是它。
⚠ 但 GenLib 的**产物里有陈旧件**：那 6 个 `<lang>/stdio.vml` 顶着 `Auto-generated`
却链着 GenLib 源码里早已不存在的 `stdio_funcs`（更早版本的输出）。**下一步：对全部
语言重跑一次 GenLib，判据是 `git status` 应几乎无改动，凡有改动处就是一处陈旧产物。**

### 新建设施：`third_party/vml/test_shared/`

起点是用户的一句判断：**「单元测试估计也只能测有返回值的，没返回值的判不了好坏，
只能判有没有」**。这条对了一半，落进规矩里 —— 判据不是「有没有返回值」，
而是**「有没有可观测的副作用」**。扫了 959 个导出函数：

| 类 | 数量 | 占比 | 判据 |
|---|---|---|---|
| A 有返回值 | 638 | 66% | 直接比返回值 |
| B void + 指针形参 | 178 | 18% | **从被写穿的内存断言**（`sprintf`/`strcpy`/`memcpy` 全在这类 —— 缓冲区就是那个"返回值"） |
| C void + 往 stdout 写 | 32 | 3% | 断言**程序自己的 stdout 字节** |
| D void + 无指针形参 | 111 | 11% | 大半还能救：`graph.*` 走**场景图元可数**、`crt.CRT_*` 断言写出的字符序列 |

⇒ **89% 可以真正判好坏**。真·冒烟（`SMOKE`）**只证明没崩/没挂/没超时**，
runner 把它**单列一档、不计入 PASS** 并单独打警告和名单 ——
**不许拿"没崩"冒充"正确"**：111 个都写成"跑通了就算过"的话，报告会显示 100% 绿而质量是零。

首个用例 `test_shared/vsnprintf.c`（30+ 断言，覆盖 `%d` 含负数 / `%%` / 字面量 /
`%s` / `%c` / `%x` / 宽度 / 左对齐 / 补零）实测 PASS。**每一格都同时查返回长度和逐字节内容**
—— 只查长度会漏掉「长度对、内容写了个 0」这种本次故障的原始形态。
（写它的时候我自己先踩了两次实参错位，5 条 FAIL 全是用例的问题不是库的问题。）


## v0.96.207 (2026-09-17) — printf 有 4 份实现：同一个函数只留一份

起点是用户的一句追问：**「为啥 printf 有 2 份实现？只要一份就行」「相同的函数只要保留一份，多的删掉」**。
查下去发现不是一个 printf 的事 —— 全库 **109 组函数被定义了两遍**。

### ① `printf` 到底有几份：三份源码 + 一条前端特例

| 位置 | 状态 |
|---|---|
| `shared/src/printf.c:300` → `shared/printf.vml` | **活的**（全库唯一被链的 printf） |
| `c/printf.vml`（159 条指令的 shim） | **活的**，但**不是实现** —— 它把 shared 的 `static` 助手导出成 `c_emit`/`func_emit` 这类跨模块名 |
| `c/stdio.c:91` | **不可达**：模块映射表里没有 `stdio` 键，也没有任何 `.vml` 链它 |
| `c/vmlib.c:111` | **不可达**：VGA 时代的单体库，谁都没链 |
| `c/src/printf.c:308` | **不可达**，而且是个**地雷** |
| `CppCompiler/CodeGenerator.Expressions.cs` | 前端硬编码 `printf→print_str` 捷径（v0.96.206 已收窄） |

`c/src/printf.c` 的地雷值得单说：`build_libs.sh` 的 Phase 3 是
`c/src/*.c → c/<name>.vml`，而 `c/src/` 里**只有它一个文件** ⇒ 谁跑一次构建脚本，
它就把那个**能工作的 shim（3146 字节）覆盖成自己的编译产物（7915 字节）**，
然后 `c/builtin.vml` 的 `.linked "printf.vml"` 拿到的是另一份东西。
一个从没被链接过的 359 行源码，唯一的作用就是等着毁掉旁边那个能跑的文件。

**删**：`c/src/printf.c`、`c/stdio.c`、`c/stdio.vml`、`c/vmlib.c`、`c/vmlib.vml`（补丁 0040）。
删前逐个验过不可达（模块映射表 + 编译日志 + `.linked` 全图），删后 `pf2.c` 探针
输出**逐字节相同**。

### ② 模块映射表里同一个键写了两遍 —— 而且后写的那条是**错的**

`CompilerHelper.cs` 的「函数名 → 模块」表（C# 集合初始化器，**后写覆盖先写**）：

```csharp
["printf"] = "printf", ["printf"] = "printf",        // 同行写两遍
["sprintf"] = "printf", ["snprintf"] = "printf",     // 隔两行又来一遍
["ltoa"] = "convert64", ["dtoa"] = "convert64", ["atol"] = "convert64", ["atod"] = "convert64",
["ltoa"] = "convert64", ["dtoa"] = "convert64", ["atol"] = "convert64", ["atod"] = "convert64",
["sleep"] = "builtins", ...  ["sleep"] = "time", ["get_tick"] = "time",   // ← 这条是错的
```

最后一条不是冗余而是**实打实的 bug**：`sleep` / `get_tick` 的**唯一实现**在
`shared/src/builtins.c`（`SYSCALL #52` / `#53`），`time` 模块里**根本没有这两个函数** ——
而「后写覆盖先写」意味着实际生效的正是这条错的，编译器会去链一个不含它们的模块
（平时被 `builtins` 恰好也在链上掩盖住了）。这正是「改了一处没生效」的温床。

清了 **24 个纯冗余条目**（判据：**键和值都相同**才删，保留最先出现的那个），
两处「同键不同值」的冲突单独报出来人工判断。行尾保住 CRLF（`git ls-files --eol` = `w/crlf`），
diff 规模 12 删 2 改 —— 不是整文件。

### ③ 没修的（如实记下）

- **`%` 转换产出零个字符**：`printf("D-noarg\n")` 一个字不打（但**执行继续**，
  后面的 `puts` 照常），而 `sprintf(b,"d=[%d]",42)` 打出 `d=[` 之后直接崩 ——
  `MOVEB R0, @0 写内存失败: 地址 34313536`。C 源码是对的，`vsnprintf` 返回 `pos` 也对，
  所以病在**代码生成**这一段长 if/else-if 链上；根因与 CLAUDE.md ⑨ 记的
  「`Lib/` 里两套栈清理约定并存」同源（`pop` 取的临时值被漂移的栈指针读错），
  正解是**用当前前端把 `Lib/` 整个重新生成**（上游仓库的事）。
- 因此 C++ 前端那条 `printf→print_str` 捷径**暂时保留** —— 它遮住了 1 实参的情形，
  删掉它会让 C++ 的 printf 立刻退到和 C 一样（什么都不打）。等 ③ 修好再删，顺序不能反。
- `c/stdio.h` / `c/vmlib.h` 是**用户代码 `#include` 的公开头**，不是重复实现，保留。

## v0.96.205 (2026-09-17) — 弹框把游戏的表停了 / 拒绝终于能退出（真机实测四连报）

用户在手机上逐门试游戏，报回来四条。**头两条是同一个根因，而且 20 份例程全中招**。

### ① 弹框期间定时器一直在跑 ⇒ 积压消息把「新一局」瞬间抽干

`ui_dlg_msg` / `ui_dlg_select` / `ui_dlg_multi` / `ui_dlg_input` 都是**阻塞**的
（VM 线程上同步等用户回答），而 `ui_timer_set` 是**重复**定时器 —— 弹框挂多久，
队列里就积压多少条 `Timer` 消息。程序一恢复就把这些积压**瞬间抽干**：

- **打地鼠**：`left = 90` 刚重置，几十条积压一次抽到 0 ⇒ 新一局立刻又「时间到」，
  **弹框再也关不掉**（实测：连点两次「允许」，框都还在）。
- **接方块 / 俄罗斯方块**：积压的每一拍都推进一步物理 ⇒ 球/方块瞬移出界、立刻又结束。

用户报的是「时间太短、打不着」。真机日志是最干净的证据 —— `#500` 在
`14:24:21 / :22 / :23` **每秒一次**连弹三次。

**修法在宿主一处**（`VmlUiCalls.WithTimersPaused`）：四个模态调用前后把定时器全部
`Change(Timeout.Infinite, Timeout.Infinite)` / 按原间隔恢复。语义上也更对 ——
程序在弹框期间**根本没在跑**，它的时钟本来就不该走。
**一处顶二十份例程**：那种「每份例程自己 `ui_timer_kill` + `ui_timer_set`」的修法，
漏一个就是同一个 bug 再来一次。

### ② 对话框的「拒绝/允许」没区别，游戏还很难退出

`ui_dlg_msg` 一直**有返回值**（`0`=是 / `1`=否，C 头文件里就是 `int`），
但 20 份例程**全部声明成 void 把结果丢了** —— 文案写着「（选「否」退出）」，
代码里从来没实现过。`gomoku.c` 是唯一做对的（`finish()` 拿了返回值）。

修法：宿主让 `WinClose()` 也置位「窗口已关」（**幂等**，收尾再关一次直接返回），
于是各语言例程只需一行 `if (ui_dlg_msg(...) != 0) { ui_win_close(); ... }` ——
主循环的 `while (ui_win_closed() == 0)` 自然退出，**不必每份再加一个退出标志位**。
改了 C / C++ / ObjC / C# / D / Dart / Go / Java / JavaScript / Kotlin / Pascal /
R / Ruby / Rust / Scheme / Swift / BASIC 共 17 份（`c/tetris.c` 只有开场提示框，无此项）。

### ③ 打地鼠：加时间 + 5 次没打到才结束 + 认 A 键

按用户要求：倒计时 `30 → 90` 秒，**没敲中 5 次**这一局就结束（画 5 个机会点，
打空一个暗一个）。另外动作键从「只认回车 13」改成 **13 / 65(A) / 32(空格) 都认** ——
手机屏幕手柄把 `START` 映射成回车、`A` 映射成字母键，玩家自然会去按那个圆形的 A。

### ④ 仍未解决（都在查）

- **`Examples/basic/tetris.bas` 在手机上编译卡住**：桌面 1.9 秒编完、跑得正常，
  真机日志停在「菜单选择…→ VML 运行」之后再无任何输出（连 `win_open` 都没有）。
- **`Examples/python/tetris.py` 启动黑屏**：真机 `VmlRuntimeException: 整数除零`；
  桌面是 `内存错误(PC=00000314)`（`clear_lines` 里）。两者都没修完。
- **`Examples/csharp/snake.cs` 运行崩**（`R12(BP)` 变垃圾值）—— 既有问题。

> **教训**：这三条都是「构建全绿 + 桌面跑得动 + 20 门语言里只坏一门」，
> 与 v0.96.203/204 同一个形状 —— **只有真机逐门试才照得出来**。

---

## v0.96.204 (2026-09-17) — BASIC 裸调函数被静默丢弃（两份游戏都不弹绘图窗口）

**用户真机实测报的**：「basic 语言有问题，没有弹界面，只弹了对话框」。

### 根因：裸调用**函数**的语句根本没被解析成调用

`Parser.Core.cs` 的标识符语句分支只认 `declaredSubs`：

```csharp
// Check for implicit SUB call (without CALL keyword) — QBasic allows bare sub name calls
if (declaredSubs.Contains(token.Value)) return ParseImplicitCallStatement();
return ParseLetStatement();          // ← ui_win_open 落到这里
```

`ui_win_open` 声明的是 `NATIVE FUNCTION`（进 `declaredFunctions`），**不在 `declaredSubs` 里**
⇒ 落到 `ParseLetStatement()`，而这一行没有 `=` ⇒ **静默丢掉**。
两份游戏都是这个形状：`whack.bas:81` 写 `ui_win_open "打地鼠", w, h`、
`tetris.bas:310` 写 `ui_win_open("俄罗斯方块", ui_scr_w(), ui_scr_h())`。

**症状为什么是「只弹对话框」**：`ui_dlg_msg` 走的是 `NATIVE SUB`（裸调用正常），
`ui_clear`/`ui_rect`/`ui_text`/`ui_present` 也都是 SUB —— **全都没问题**；
唯独 `ui_win_open` 是唯一「有返回值 + 当语句裸调」的那个 ⇒ 窗口从没被打开，
绘制全画在一块不存在的画布上，而对话框照弹。

**汇编层面的证据**：行标从 `line_80` 直接跳到 `line_82`，**`line_81` 整个不存在**；
`grep 'call .*ui_win_open'` 数出 **0**（库里只有定义、没有调用）。

### 修法（两处，缺一不可）

① **解析器**认「声明的函数名 + 后一个 token 不是 `=`」⇒ 当隐式调用语句
（排除赋值：`x = ...` 的左边也可能是与函数同名的变量）。
② **代码生成**：`GenerateCallStatement` 此前**只查 `subMap`**（函数在 `funcMap`），
`subDecl == null` ⇒ 标签被编成 `sub_ui_win_open`（永远解析不到）。
现在两种声明都查，判据与表达式路径 `GenerateSubFunctionCall` 对齐：
**native 用裸名、否则 `sub_`/`func_` 前缀**；BYREF 判据同样对两种声明成立。

### 验证

| 判据 | 修前 | 修后 |
|---|---|---|
| 探针 `ui_win_open` 的 call 数（语句式 + 赋值式两处） | 1（赋值那处） | **2** ✓ |
| 探针的行标 | 缺 `line_15`/`line_16` | 14 个行标齐全 ✓ |
| `Examples/basic/whack.bas` 的 `ui_win_open` call 数 | 0 | **1** ✓ |
| `Examples/basic/tetris.bas` | 0 | **1** ✓ |

回归：`drift.bas` **DRIFT=126**、`skel.bas` **SKEL-SUM=14**、
跨语言判据 **27/29**（失败的 `fth`/`ld` 是既有的剩余 2 种）、
两份 BASIC 游戏跑满超时**零内存错**。补丁 `patches/0038-basic-bare-function-call.patch`。

> **教训**：「能编译、能跑、连对话框都弹了」不等于这条路径通了 ——
> 前面 20 门语言都正常，唯独 BASIC 是「**函数当语句用**」这个形状没被覆盖。
> 与 v0.96.203 的 Scheme 同一个道理：**骨架全绿只能证明骨架走过的那条路没坏**。

---

## v0.96.203 (2026-09-17) — Scheme 前端六条修复（接方块上手机）+ 全 22 语言游戏打包

**起因**：给 Scheme 补一份游戏例程。写不出来的原因不是「Scheme 不适合做游戏」，
而是这个前端**六处各自独立的内存/调用缺陷** —— 骨架 `skel.scm` 一条都照不出来，
因为它的函数只有一个参数、循环全在顶层、且从不从函数里调库。

### ① `main` 不占帧 ⇒ 顶层变量只能放 3 个

顶层绑定按 `R12 + (12 - off)` 寻址（`off = 4, 8, 12 …`），也就是
`R12+8 / R12+4 / R12+0` 之后**继续往下**到 `R12-4 / R12-8 …` —— 而 `main` 此前不 `sub R13`，
`R13 == R12`，**压栈正好写在那一片**。

| 判据 | 修前 | 修后 |
|---|---|---|
| 12 个顶层变量求和（应 **78**） | 24 | 78 ✓ |
| 5 个变量夹几次调用（应 **15**） | 65536 | 15 ✓ |

修法：`GenerateCode` 里给 `main` 加帧占位、按**峰值**回填。

### ② ≥2 个形参的函数读错实参

形参槽原先按 `varOff = fnParams.Count; … = --varOff * 4` 递减分配（4、0、-4…），
**只在单参数时**恰好等于正确的 `-4*0`；两个参数起整体错开一槽（读到返回地址槽）。
命名 `let` 那处写的是 `nlParams.Count - 1`（0、-4…），则**只在两个参数时**恰好对。

| 判据 | 修前 | 修后 |
|---|---|---|
| `(define (pick a b) b)` 调 `(pick 11 22)` | 11 | 22 ✓ |
| `(define (add2 a b) (+ a b))` 调 `(add2 3 4)` | 26 | 7 ✓ |
| 三参数 `(add3 1 2 3)` | 31 | 6 ✓ |

修法：两处统一成 **`off_i = -4i`**（`arg_i` 位于 `R12+12+4i`：GenCall 逆序压栈 ⇒
`arg0` 在最顶，`call` 压返回地址，序言压 R15/R12 —— 共 3 槽 = 12 字节）。

### ③ 从用户函数里调库函数必崩 —— 尾位置一律编成 `jmp <名>_body`

`GenCall` 见 `tailPos` 就走 `GenTailRecursive`：把实参搬进**当前帧**的形参槽、释放当前帧、
`jmp {targetFunc}_body`。那是 Scheme 自己的**尾调用优化**，成立的前提是
「被调者与调用者共用同一个帧」—— 对**自递归**成立，对**任何别的函数**都是错的：
跳进 `_body` 等于**跳过被调者的序言**（`push R15; push R12; move R12 R13`）。

症状：顶层直接调库函数完全正常（`_currentFunc == null`，本来就不走这条路），
**从用户函数里调**就连零参数的 `ui_present` 都崩。

修法：判据收紧成 `tailPos && op == _currentFunc`（`_currentFunc` 早就在记当前函数名）。

### ④ 函数体 / `let` body 只编译第一个形式

`(define (f) a b c)` 只编 `a`；命名 `let` 只编 `l.Items[3]`；
语句位的 `let` / `let*` / `letrec` 只编 `l.Items[2]` —— **第二个形式起全部静默丢掉**。
实测 `(let lp ((a 1)(b 2)(c 3)) (display "NL3=") (display (+ a b c)) (newline))`
只打印出 `NL3=`，后面的 display/newline 一个字都没编出来。

修法：四处统一成 `begin` 的口径（前面的按值丢弃、最后一个留在尾位置）。

### ⑤ 命名 `let` **完全不占帧**

生成的 `__nl_N` 里一条 `sub R13` 都没有，而它体内的 `let`/`do` 局部量照样按
`R12+(12-off)` 往下写 ⇒ **直接写进压栈区**、被实参压栈冲掉。
只有「函数体里没有局部量」的命名 `let`（如骨架）才看不出来。

### ⑥ 帧大小按**生成结束时的** `varOff` 算

`let` / `do` 收尾会把 `varOff` 还原（`savedVarOff`），于是
`(define (f) (let ((a 1) …二十个…)) …))` 生成完 `varOff` 回到 4、帧被算成 64 字节，
而局部量已经写到 `R12-80` —— **踩进调用方的帧**。

修法：`varOff` 改成属性，赋值时顺带记 `_peakVarOff`；帧按峰值算。
20 个局部量 + 循环判据实测 **213** ✓。

> **六条的共同教训**：`skel.scm` 全绿只能证明「这一条路径没坏」。
> 骨架的最小性（单参数、循环在顶层、不调库）恰好绕开了全部六条。

### ⑦ 新例程 `Examples/scheme/catch.scm`（接方块）

写的时候还钉住一条**没法修、只能绕**的约束：**用户函数看不见顶层变量** ——
二者都按 `R12 + (12 - off)` 寻址，而函数里的 `R12` 是它自己的帧指针。

```scheme
(define g 0)
(define (w1) (set! g 5))
(w1)            ; ⇒ 踩坏保存的 R12（R12(BP) 变成 8），紧接着 [R12-48] 越界
```

⇒ 游戏写成**扁平顶层程序**：循环、状态、判定全在顶层，只把「参数全传、不碰全局」的
纯函数抽出去（例程里的 `clamp` 就是三参数的，顺带吃到 ② 那条修复）。

### ⑧ 全 22 语言游戏例程打包上手机

`scripts/make-vml-lib.sh` 重打（示例 45 份，含 `Examples/scheme/catch.scm`），
APK 重签重建。至此 **20 门语言各有游戏例程**（22 门里只剩 Forth / Ladder 两门跑不了）：

| 语言 | 例程 | 语言 | 例程 |
|---|---|---|---|
| C | `c/tetris.c` `c/gomoku.c` | ObjC | `objc/snake.m` |
| C++ | `cpp/snake.cpp` | Pascal | `pascal/catch.pas` |
| C# | `csharp/snake.cs` `csharp/racer.cs` | Python | `python/tetris.py` |
| Java | `java/catch.java` | R | `r/catch.r` |
| JavaScript | `javascript/catch.js` | Ruby | `ruby/catch.rb` |
| Kotlin | `kotlin/catch.kt` | Rust | `rust/breakout.rs` |
| Go | `go/snake.go` | Swift | `swift/snake.swift` `swift/plane.swift` |
| Dart | `dart/catch.dart` | BASIC | `basic/tetris.bas` `basic/whack.bas` |
| D | `d/catch.d` | Fortran | `fortran/sokoban.f90` |
| Lua | `lua/life.lua` | Scheme | `scheme/catch.scm` |

**验证**：跨语言调用约定判据 **27/29**（失败的 `fth`/`ld` 是既有的「剩余 2 种语言」）、
`skel-scheme` **SKEL-SUM=14**、`drift.scm` **DRIFT=126**、
Scheme 自测 13/13（`nt`/`pa`/`pb`/`pc`/`do2`/`fnst`/`manylets`/`nlstmt`/`nlstmt1`/
`letm`/`letstarm`/`topn`/`topc`）。
补丁 `patches/0037-scheme-frame-params-and-calls.patch`。

---

## v0.96.202 (2026-09-17) — Lua 的 `for` 循环变量必须先 `local`（跨语言判据 11→13 绿）

上一版之后又清掉两条，其中一条是**纯 Lua 前端缺陷**、与调用约定无关。

### ① `ParseFunctions` 把**注释**当成了函数签名

正则 `RET NAME(params)` 看不出注释与代码的区别，于是 `array64.c` 第 4 行的

```c
// VML Shared Array64 Library — 64-bit Integer Arrays (long* with long indices)
```

被读成「返回 `Integer`、函数名 `Arrays`、参数 `long* with long indices`」⇒ `funcMap` 与
`Lib/modules.json` 里多出一批**不存在的函数**，每个还生成一个 `LABEL x … CALL x` 的
**自调用死包装器**（与 v0.96.201 修的撞名自调用同一个机制）。

修法：`StripComments`（把注释换成**等长空白**，保住偏移与行号）之后正则再匹配。
`Arrays` / `Manipulation` / `CRC` 从此不再出现。
⚠ `modules.json` 是手工配置，里面仍留着约 50 条历史虚构条目（`graphics` 模块下的 `R0`/`PUSH` 之类），
是死包装器且已被下面的保险兜住，**本次不清**。

### ② 撞名保险：包装标签与 C 符号名同名时**自动改名**

原本想给 java/javascript 补 `PrimaryPrefix` 了事，但 csharp 立刻报出**两例真实撞名**：
`GetDate` / `GetTime`（`dos.c` 里函数名本身就是 PascalCase，camelCase/PascalCase 之后还是它自己）。
改成在 `GenModules` 里兜底：`label == funcName` 时包装标签改 `{lang}_{原标签}`，
别名（`func_xxx` / `Dos_xxx` …）指向它，包装器里的 `CALL {funcName}` 于是解析到共享实现而非自己。
原来是「撞名就告警」，现在是被**处理**。

### ③ Lua 的 `for` 循环变量必须有自己的局部槽（`drift.lua` 由 `0` 转 `126`）

```lua
function main()
    local a = {1, 2, 3, 4}
    local s = 0
    for i = 1, 4 do  s = s + a[i]  end
    print(s)          -- 0    ✗ 循环体一次都没执行
end
```

**只差一句 `local i = 0`** 就对（得 10）。读生成的 VML 看到根因：没预声明时循环变量落在
数据段的全局 `var_i`，而那条路对**同一个标签有两种解读** ——

```asm
for_start:  move R1 var_i     ; 把标签当**地址**取 → R1 是个远大于 4 的地址
            cmp R1 R0
            jg  for_end       ; 立刻跳出，一次都不执行
循环体:     move R0 [var_i]    ; 按地址**取值**
```

修法：`GenerateForStatement` 里循环变量**无条件分配一格局部槽**，两种写法从此走同一条 `[R12-off]`。

> 语料 `corpus/lua/skel.lua` 恰好写了 `local i = 0`，所以 22 语言骨架一直绿 ——
> **这条路径从来没被覆盖**。又一个「骨架全绿 ≠ 该语言没问题」。

### ④ 新增 `abi.m`：把 ObjC 的嫌疑从「实参顺序」摘干净

`drift.m` 一直是红的，但新增的 `abi.m`（同样的两参调用、**不套循环**）实测 `ABI=8` **通过**
⇒ ObjC 的实参传递没问题。真正的病定位到「**外部库调用在循环体里会让循环提前退出**」：

| 程序 | 实测 | 应得 |
|---|---|---|
| 循环里不调库 `s = s + i` | `21` ✓ | 21（跑满 6 轮） |
| 循环里调库、**常量**实参 `s = s + ipow(2,3)` | **`16`** ✗ | 48（= 8×**2** ⇒ 只跑 2 轮） |
| 循环里调库、变量实参 `s = s + ipow(2,i)` | **`2059`** ✗ | 126 |

骨架 `corpus/objc/skel.m` 没事，是因为它循环里调的是 `inc`（**本地函数**，走另一条分支）。
**未修**，落点已写进交接文档。

### 判据

| | 本版开始 | 结束 |
|---|---|---|
| 跨语言判据 | 11 绿 / 2 红 | **13 绿 / 1 红**（新增 `abi.m`，分母 13→14） |
| C 判据 / 22 语言骨架 / `check-vml-patches.sh` | — | 6/6 · 22/22 · 全绿 |

唯一红的 `drift.m` 未修。**APK 仍未重打**。交接说明见 `docs/VML调用约定统一-交接.md`。

---

## v0.96.201 (2026-09-17) — 单词名库函数的包装器**自调用**（跨语言判据 10→11 绿）

### ① `abi.js` 由「崩（SP 归零）」转 `ABI=8`

现象是调用 `ipow(2,3)` 直接把栈跑穿。根因在 `Lib/naming.json`：

| | LabelStyle | PrimaryPrefix | 单词名（`ipow`/`pow`/`abs`/`sqrt`）的标签 |
|---|---|---|---|
| 其余 19 种语言 | snake_lower 等 | `c_` / `python_` / `ruby_` … | 带前缀，**不与 C 符号名同名** |
| java / csharp / javascript | PascalCase / camelCase | **空** | camelCase 走完**还是它自己** ⇒ 与 C 符号名同名 |

于是包装器生成成了自调用：

```asm
LABEL ipow
    push R1 / push R0
    call ipow        ← 自己调自己，无限递归 → SP 归零
```

修法是补前缀：javascript → `js_`、java → `java_`（与其余语言做法一致）。

> ⚠ **java 是同一个病**，此前没崩只是**链接器那次"碰巧"选了共享实现**。
> 同一份 `LABEL ipow / CALL ipow`，链接顺序换个方向就发作 —— 这种运气不能留。
> csharp 的 `LabelStyle` 是 `PascalCase_method`（首字母也大写），单词名会变成 `Ipow` ≠ `ipow`，
> 所以它没有这一类撞名（它的告警是下面 ③ 那批虚构条目）。

### ② 护栏：真实函数撞名就告警

`GenLib.GenModules` 新增一条 —— 生成的标签等于 C 符号名**且该函数确实存在于共享库**时打到 stderr。
只在真实冲突时触发（虚构条目报出来只会误导），把「以后新增语言选了个会撞名的命名风格」挡在生成阶段。

### ③ 护栏顺带挖出的一条**独立缺陷**（本次只告警、未修）

加完护栏，csharp 一次报出 `Arrays` / `Manipulation` / `CRC` / `GetDate` / `GetTime` …… 一查全是
**虚构条目**：`GenLib.ParseFunctions` 的正则没先剥注释，于是 `array64.c` 第 4 行的

```c
// VML Shared Array64 Library — 64-bit Integer Arrays (long* with long indices)
```

被读成「返回 `Integer`、函数名 `Arrays`、参数 `long* with long indices`」。
后果：`funcMap` 与 `Lib/modules.json` 里多出一批不存在的函数，每个还生成一个
`LABEL x … CALL x` 的**自调用死包装器** —— 与 ① 同一个机制，将来撞上真标签就是静默劫持。

修法（下一步）：`ParseFunctions` 先剥 `//` 与 `/* */` 再匹配，并清掉 `modules.json` 里已收进来的
虚构条目。成因已写进 `scripts/vml-abi-probe/README.md`。

### 判据

| | 本轮开始 | 结束 |
|---|---|---|
| 跨语言判据 | 10 绿 / 3 红 | **11 绿 / 2 红** |
| C 判据 / 22 语言骨架 / `check-vml-patches.sh` | — | 6/6 · 22/22 · 全绿 |

剩两项是**独立的既存缺陷**：`drift.lua`（`ipow` 在循环外对、进 `for` 循环变 0）、
`drift.m`（ObjC 既有多参缺陷）。`vml_lib.zip` 随 `Lib/` 重打；**APK 仍未重打**。

---

## v0.96.200 (2026-09-17) — VML 调用约定统一**推平到其余前端**（跨语言判据 7→10 绿）

上一版把约定落到了 `Lib/`，但**其余前端仍是旧形态** —— 22 语言骨架全绿照不出来。
本版把它们逐个推平，并在过程中挖到了**「单参调用看着都对、多参才露馅」的总根源**。

### ① 判据先行：22 语言骨架全绿**证明不了**调用约定

骨架里的库调用都是单参（或参数不参与判据）。新立一套 `scripts/vml-abi-probe/run-langs.sh`（13 条）：

- `abi.<ext>` —— 调 **`ipow(2,3)`** 并打印，判据 `ABI=8`。
  选它是因为**非交换**：正确 8、实参反序 9、第二个实参读成 0 得 1。
- `drift.<ext>` —— 反复调用库函数后累加/循环变量必须一字未动（「压了不清」的栈漂移要攒几次才踩到）。

| | 本轮开始 | 结束 |
|---|---|---|
| 跨语言判据 | 7 绿 / 6 红 | **10 绿 / 3 红** |
| C 判据（`run.sh`） | 6/6 | 6/6 |
| 22 语言骨架 | 22/22 | 22/22 |

### ② 修掉的四个前端

| 前端 | 改前 | 改后 | 改了什么 |
|---|---|---|---|
| **C++** | `9` | **`8`** | extern/stdcall/fastcall/cdecl **四条分流合成一条**（右到左 + 一律调用方清栈）。⚠ 顺带修掉一处**从来没生效过**的镜像：原 cdecl 分支那句 `if (i > 0) MOVE Ri, R0` 写在**压栈循环里面**，i=3 装好 R3 之后还会被 i=2/1/0 的实参求值冲掉。被调方同样收口成单一形参布局 + 裸 `pop R15`（原先声明 `__stdcall` 的函数会自己弹栈，而调用方也清一次 ⇒ 净漏） |
| **Java** | `1` | **`8`** | 「第 1 个实参放 R0、其余从右到左压栈」（CCv2 寄存器约定）改成**全部走栈**。它与自己的被调方是自洽的，但与其它所有语言和库都不兼容 —— 库里 C 编译出来的函数从 `[R12+12+4i]` 取参，R0 里那个它根本看不到 |
| **Ruby** | `1` | **`8`** | 压栈方向改右到左（接收者仍压在实参之后 ⇒ 落在最低地址 = `self` 的位置，正好对） |
| **Lua** | `8`→崩 | **`8`** | 「保存参数到栈」是 `MOVE [R13-(i+1)*4], R0` —— 往 SP **下方**写、**R13 全程不动**，再镜像进 R0-R3 指望被调方从寄存器取。改成真压 + 调用方清；被调方也改成每个形参从 `[R12+12+4i]` 取（原先第 4 个之后根本不支持） |

### ③ **总根源**：`Lib/{lang}/**` 的包装器一直在用**寄存器**收参数

```asm
ruby_ipow:
    push R1        ; ← 第 2 个参数从 R1 取
    push R0        ; ← 第 1 个从 R0
    call lib_math_ipow
```

`GenLib.EmitPushParam` 发的是 `PUSH R{i}` —— 旧寄存器 ABI，而被调方从栈上读，
于是拿到 R0/R1 里的残留（实测 `ipow(2,3)` 得 `1` = `ipow(x,0)`，因为 R1 恰为 0）。

**这就解释了「为什么单参调用看着都对、多参才露馅」**：单参时 R0 恰好等于刚求值完的那个实参；
C 前端能跑，只因为它在调用点额外做了 R0-R3 镜像（v0.96.199）。

改成包装器**从自己的栈帧读实参**（自带 `push R15 / push R12 / move R12 R13` 序言 → `[R12+12+4i]`）、
转发前把 `arg0..arg3` 镜像进 R0-R3（喂实现体里那 543 处 `asm("SYSCALL #6")`）、尾声拆帧裸 `ret`。
包装器从此只认「实参在栈上、右到左」，**不再关心调用方是谁** —— 镜像也从
「每个前端都要记得做」变成「包装器做一次」。`Lib/` 随之整体重生成（1628 个文件），
`vml_lib.zip` 重打。

### ④ 还剩 3 项 —— 都是**独立的既存缺陷**，不是本次回归

- `abi.js`：`Lib/javascript/math.vml` 里 `LABEL ipow … CALL ipow` 是**自调用** ⇒ 无限递归。
  根因是 `naming.json` 给 javascript / java 的 `PrimaryPrefix` 为空，**单词名函数**的主标签
  与 C 符号名撞车（`pow`/`abs`/`sqrt` 同理）。重生成前后都在。
- `drift.lua`：`ipow` 在循环**外**是对的（常量实参、`local` 变量实参实测都得 8），
  **进 `for` 循环就变 0** —— 与实参传递无关的另一个问题。
- `drift.m`：ObjC 既有多参缺陷（重生成前基线也是 2059；栈漂移那部分已修）。

三项连同实测值记在 `scripts/vml-abi-probe/README.md` 当活单。
另：桌面端自测 5859/5859、`check-vml-patches.sh` 全绿；**APK 仍未重打**。

---

## v0.96.199 (2026-09-17) — VML 调用约定统一**落到 `Lib/`**（判据 6/6 + 22 语言全绿）

**用户拍板的方向**：全部实参右到左压栈、一个都不走寄存器、被调方清栈
（= 真·微软 stdcall 的传参形态），把 VML 内部「至少三种约定并存」一次结清。
方案与全部实测记录见 `docs/VML调用约定统一.md`。

上一轮只改了 **C 前端**（判据仍 4/6）；本轮把 `Lib/` 就地重生成，并把由此**新暴露**的
一批回归修掉。同时发现两条此前没记下的东西（② 与 ③）。

### ① `Lib/` 就地重生成 —— 分家之后第一条命令

```bash
cd third_party/vml
touch Lib/shared/src/*.c      # ⚠ GenLib -b 是增量的，不顶时间戳会「0 编译, N 跳过」
dotnet run --project tools/GenLib -- -A -r .
```

| 判据 | 重生成前 | 重生成后 |
|---|---|---|
| `scripts/vml-abi-probe/run.sh` | 4/6（p3/p5 红） | **6/6** |
| 22 语言骨架（`SKEL-SUM=14`） | 22/22（带病） | **22/22** |
| `print_*` 漂移探针（Python） | T2/T4/T5 全 0 | **T2=50 T4=60**，漂移本体消失 |
| 桌面自测 | — | **5859 / 5859** |

变更 `Lib/` 1621 个文件（约 4.2 MB 文本 diff），而 **`Lib/shared/src/*.c` 一字未改**
—— 整个 diff 都是生成物。手机端资产 `vml_lib.zip` 随之重打（内容指纹更新，
app 侧据此重新解压）。

### ② 纠正一条误判：掉 csharp/forth **不是**「生成工具链口径」

上一版记的是「只重生成 `console.vml` ⇒ 22 语言掉到 20，原因是标签格式
（`L94240004`→`L_94240004`）与 `.linked` 抬头丢失」。**这段结论是错的。**
真因是**只重生成了一层**：`Lib/shared/` 变新了，而 `Lib/{lang}/` 的包装器还是旧的 ——

```asm
LABEL PrintlnInt
    PUSH R0            ; ← 为「被调方会弹掉一个参数槽」而多压的一格
    CALL println_int
    RET                ; ← 它指望 println_int 把那一格弹掉
```

被调方改成裸 `ret` 之后，`RET` 弹掉的就是自己刚压的参数 ⇒ 跳飞。**两层一起重生成，
csharp 当场恢复**；标签格式与 `.linked` 从来不是问题。

### ③ ⚠ 重生成引入的回归：各前端「为被调方清栈写的补偿」变成了净漏栈

被调方不再弹参，而好几个前端里还留着「压了实参指望对方弹掉」的补偿 ⇒ **每次调用净漏 4~8 字节**。
用「换 Lib 前后跑同一份程序」对照出来的：

| 探针 | 重生成前 | 重生成后 | 修完 |
|---|---|---|---|
| Fortran `2 ** i` 累加 | `126` ✓ | `66` ✗ | **126** ✓ |
| D `2 ^^ i` 累加 | `126` ✓ | `66` ✗ | **126** ✓ |
| Ruby `2 ** i` 累加 | `65528` ✗ | — | **126** ✓ |
| R 三次 print 后进循环 | `7` ✓ | `7` ✓ | 7 ✓（补的是净漏 4 字节） |
| C# 循环里打 20 次 | `190` ✓ | `190` ✓ | 190 ✓（补的是**同一前端内两条路约定矛盾**） |

前两条是**本次引入的回归**，必须修。改动落在 7 个前端（D `^^` / Fortran `**` / Ruby `**` /
R print·cat / Basic `GenerateLibraryCall` / C# `GenerateConsoleWriteLine` /
ObjC print_* 与外部函数分支），统一成「压了 N 格 → CALL → 补 `ADD R13 #N`」。

### ④ 22 语言骨架全绿**证明不了**调用约定 —— 判据进了仓库

骨架里的库调用都是单参（或参数不参与判据），而**实参反序要两个以上实参才露馅**，
**栈漂移要攒几次才踩到关键变量**。立了一条 `ipow(2,3)` 探针（非交换：正确 8、反序 9），
立刻照出文档第 2 步「其余 19 个前端各自查一遍」**只做了 C 与 Forth**：

| 探针 | 实测 | 指向 |
|---|---|---|
| `abi.cpp` | `9` | C++ 有**自己的一套**调用生成（与 C 是两套代码），extern/stdcall 左→右压栈，且 R0-R3 镜像写在**压栈循环内**（会被后续实参求值冲掉） |
| `abi.java` / `abi.rb` | `1` | 「第 1 个实参进 R0、其余压栈」——第 2 个实参到不了被调方（`ipow(2,0)`） |
| `abi.js` | 无输出 | 左→右压栈；且**同一前端内** `super`/`new` 两处却是右→左 |
| `drift.lua` | `0` | 「压栈」不动 R13，实参只进 R0-R3 ⇒ 被调方读不到，第 5 个起静默丢弃 |
| `drift.m` | `2059` | ObjC 既有多参缺陷（重生成前基线也是 2059，**非本次回归**） |

新增 `scripts/vml-abi-probe/run-langs.sh`（13 条：`abi.*` 查实参顺序、`drift.*` 查栈漂移），
现状 **7 绿 / 6 红** —— 红的全是**其余前端还没统一**，逐条记在 README 里当活单。
（跨语言那一层的完整审计结论也写进了 `docs/VML调用约定统一.md` 末节。）

### ⑤ `check-vml-patches.sh`：`Lib/` 改按「可重生成」验

`Lib/` 已完全是生成物，再往补丁里塞 4 MB 快照没有意义（而且每重生成一次就要再塞一份）。
改成两段：

1. **生成物**：复制 `Lib/` 到临时目录 → 非 C 源文件 mtime 拨到 1970 → 跑 `GenLib -A`
   → 凡 mtime 变新的（1933 个）逐个 `cmp` 与工作区比，**必须逐字节相同**。
   （这比「vendor+补丁」更强：证明的是「`Lib/` == f(源码, 前端, GenLib)」，不是「碰巧没人动过」。）
   ⚠ **只拨非 C 源**：把 `.c` 一起拨老会让 `-b` 判定「不比源旧」而整批跳过 —— 原型阶段假绿过一次。
2. **手工部分**（GenLib 不产出的 3289 个：手写 `.vml` / `json` / 脚本 / `Device/`…）：
   仍按「vendor + 补丁 == 工作区」验 —— 它们才是 `rsync --delete` 真会吃掉的东西。

负向验证过三次（改生成物 / 改手工文件 / 新增手工文件），三次都报红。
新增 `patches/0034-unified-calling-convention.patch`（10 个前端文件）。

### ⑥ 仍未完成

- **其余前端的调用点**：上面 6 条红项（C++ / Java / Ruby / JavaScript 的实参方向、
  Lua 的假压栈、ObjC 的既有多参缺陷）。C++ 那条最典型 —— 它与 C 是**两套独立实现**，
  正是本仓反复出现的「同一件事两处实现」。
- **APK 未重打**：`vml_lib.zip` 已更新，但不重装 APK，手机上跑的还是旧约定的库。

---

## v0.96.198 (2026-09-17) — VML 22 种语言全部达到「能写游戏」（真机 22/22）

**用户目标**：「至少 20 种语言可以用来写游戏，供大家在手机上调试运行程序」——**达成**。

判据不是「编译过了」，而是每种语言跑通一条骨架（**循环 + 数组 + 函数 + 一次游戏 API 调用**），
stdout 恰好 `SKEL-SUM=14`。基线 **7** 种（c / csharp / go / javascript / objc / pascal / basic），
本轮修好另外 **15** 种，**桌面 CLI 与真机（Android 模拟器）逐字一致，22/22 零分歧**。

### ① 两个效率前提 —— 没有它们这件事根本做不动

| | |
|---|---|
| **桌面 VML CLI** | `.scratch/vmlcli`，与手机端**逐字等价**（c/python/lua/java/forth 5/5 实测，连 forth 的前导空格都对上）。迭代从「打 APK + 装模拟器 ≈ 2 分钟」压到「≈ 1 秒」|
| **22 前端横向体检** | 先静读 22 个前端把缺陷定位到 `文件:行号`，再派并行修复；而不是一个个试 |

> ⚠ **必须用 vendored 代码**：上游仓库没有我们 `patches/` 里的修复。同一份 python 语料，
> vendored 能编过、上游 CLI 在编译期崩 `Index was outside the bounds of array`。

### ② 缺陷归成五族 —— 不是「某门语言难」，是同一个错误反复出现在不同前端

| 族 | 表现 | 中招 |
|---|---|---|
| 赋值目标是下标时**整段没有代码生成** | 算完右值就 `return` | Go · Python · Java · Kotlin · Rust · Ruby · D · Dart |
| **操作数写反 / 地址当值** | `MOVE R0, R0` 自赋值、把 store 写成 load | Forth · Scheme · Java · Swift · Ladder · D · Rust |
| 解析器**没有下标语法** | `a[i] = v` 根本解析不过 | Ruby · Ladder · D · Dart · Kotlin · Rust |
| **符号表换对象**致基类 `_varOffsets` 悬空 | 定义过函数后顶层变量被分配两次 | **R 与 Ruby 各写一份、都漏了**（R 那份有修复注释，Ruby 没有）|
| 前端**不分配栈帧** | `R12 == R13`，局部变量槽全在 SP 之下，任何 `push`/`call` 都写花 | R · Lua · Ruby · Dart · D |

**一条必须记住的判据**：`+4` 数组头**只有用了 `AllocateVmlArray`（`[count, e0, …]`）的语言才需要**；
**R / Fortran / Pascal / D 是自建扁布局、读写两侧同式，没有 `+4` 是对的** —— 补上去反而造出「读的对、写的差一格」。

**几条被实测纠正的先前判断**（记下来免得重犯）：
- Forth 的 `65536` **不是**「把地址当值返回」的佐证（那是巧合），真因是 `print_*` 蹦床吃掉数据栈
- Ladder **不是**「不该用游戏骨架验收」—— 它有完整的 IEC 61131-3 ST 子集，缺陷是实现问题
- Dart 的大头**不是**「少个 postfix」，是 `List<int>` 声明整条被丢弃 + 字面量元素一个都没解析
- Fortran 语料里 `print *, 'A=', s` 打出多余空格是**正确的列表输出语义**，不是缺陷

### ③ 改动固化为 `third_party/vml/patches/0019`–`0033`（15 条）

python / forth / scheme / java / kotlin / rust / swift / cpp / fortran / ruby / ladder / d / dart / r / lua。
`scripts/check-vml-patches.sh` **全绿**：33 条补丁依序打到 vendor 提交上，8 个同步目录 + `vmltool.config.xml` + `VERSION` **逐字节一致** ⇒ 跑 `sync.sh` 不会再丢。

> Lua 那条新增了 `Lib/lua/luatable.vml`（前端调的 `lua_table_get/set` 全仓没有定义）。
> ⚠ **新增文件要 `git add -N` 才进得了 `git diff`** —— 生成补丁时踩过：漏了它，同步一次 Lua 就废。

### ④ 真机验收抓出「桌面等价性」的边界（**这条最有价值**）

第一轮真机是 **21 PASS / 1 LINK_FAIL**：`skel-lua` 报 `未找到标签: lua_table_get`，而**桌面同一条 PASS**。

根因**不在前端**，在打包：APK 内置的 `WayCoder.Maui/Resources/Raw/vml_lib.zip` 是**签入仓库的生成物**，
早于补丁 0033，里面没有 `luatable.vml`。而**桌面跑 VML 直接读 `third_party/vml/Lib/`，根本不走
「zip → APK 资产 → 设备解压」这条链** ⇒ 桌面全绿、手机上是坏的。

**如果只信桌面结果就发布，Lua 在手机上就是坏的，而且我们不会知道。**

### ⑤ 三个打包缺陷修复（① 是 ④ 的根因，也是后续重构的前置条件）

| | 问题 | 修法 |
|---|---|---|
| ① | `scripts/make-vml-lib.sh` 调**裸 `python`**，macOS 上只有 `python3` ⇒ 报 127；**失败点恰在「zip 已移到位、指纹还没算」之间** ⇒ 留下「新 zip + 旧指纹」，而设备解压判据是**指纹** ⇒ **永远不重新解压、修复静默不生效** | 解析 `PYTHON`（优先 `python3`）+ **两个都先写临时文件、最后一起移入**（指纹失败则两个都不动）|
| ② | `build-apk.sh` **不会重新生成 `vml_lib.zip`** ⇒ `Lib/` 一有改动，打出的包就过期 | 打包前先调 `scripts/make-vml-lib.sh` |
| ③ | `verify.py` 的 `COMPILE_FAIL` 关键字含 `VML 标准库`，而它是进度行 `⏳ 正在解压 VML 标准库…` 的子串 ⇒ **库指纹一变、之后第一条必被误判**（实测 `skel-c` 明明打出了 `SKEL-SUM=14` 却被判 COMPILE_FAIL）| 判据剔除进度行（原先 `COMPILE_FAIL` 用未清洗的 `output`、`LINK_FAIL` 用 `stripped`，同函数两套口径）|

### ⑥ 发现 VML 内部**三种调用约定并存**，并定案统一到微软 stdcall

排查 `print_*` 栈漂移时发现：`CCompiler` 的寄存器路径是**右到左**压栈（注释明写），
而同一个前端的 `case CallingConvention.Stdcall:` 分支是**左到右**（注释也明写）——
**两条注释直接互相矛盾**；`Lib` 里 `vmlui` 家族从 `[R12+12]` 起读（右到左、全从栈读），
而 `builtins` 家族值在 `R0` 却**还要求栈上留一个槽**。

底层事实（三条互证）：`VMLRuntime.Instructions.cs:452` 的 `sp -= 4` ⇒ 栈向下生长
⇒ **arg0 在最低地址 = 最后压入 = 右到左**，且 `[R12+12]` 恒为 arg0。

**定案（用户拍板）**：全部参数**右到左压栈、一个都不走寄存器** + **被调方清栈** + **用当前前端重新生成整个 `Lib/`**。
方案见 `docs/VML调用约定统一.md`（含实施顺序、回归资产、六条已知坑）。
> 收益之一是消掉 **`print_*` 栈漂移**：现在任何打了两次以上 `print` 的程序局部变量就会读成 0
> （实测 Swift / Kotlin / Go / C / R / Python 全部中招；`ui_*` 家族不漂，所以游戏主循环不受影响）。

### 文档

- 新增 `docs/VML调用约定统一.md`（重构方案，含 `file:line` 证据）
- `docs/前端游戏能力评估.md` 加**时效横幅** —— 它的「不适合」档位表已失效（那 6 种现在都通），根因分析仍有效

## v0.96.197 (2026-09-17) — C/BASIC 前端缺陷修复 + 补丁机制三处失修 + 设备端验收装置

本轮以**排查与修缺陷**为主，附一套可重复的设备端验收装置。

### ① 补丁机制的三处「静默失效」（比单个缺陷更值得记）

`scripts/check-vml-patches.sh` **在改动前就是红的**，跑不到逐目录比对那一步：

| 问题 | 后果 |
|---|---|
| `patches/0004` **带 CRLF**（正文 111 行 + 头部 21 行，头部还是 `\r\r\n`）| 严格 `git apply` **永远打不上** ⇒ 复核脚本跑到它就中断，**后面全都等于没验** |
| `patches/0007` **漏了 6 个 BasicCompiler 文件（约 140 行）** | 补丁头明写「除 0001–0006 之外全部源码改动」，那段却一个字都没有 ⇒ **跑一次 `sync.sh` 就会静默冲掉** |
| 复核脚本没排除 `.DS_Store` | macOS 上必然误报 |

三者都是「**看起来在保护、实际没保护**」的形态 —— 本仓反复出现的那类：**自证一致性不等于可应用性**。

### ② C 前端两处

- **常量折叠无溢出/负数护栏**：`int a[65536*65536]` 回绕成 0 ⇒ **编译通过、数组零个元素、此后 a[i] 全部静默越界**；`int a[4294967295]` 更糟 —— 顶层容错吞掉异常，结果是**「编译完成: 0 条指令」、退出码 0**，失败被伪装成成功。回归验证：8 个既有 C 文件产物**逐字节不变**。
- **枚举「只认裸字面量」有 4 份实现**：`enum{A=BASE+1,B,C}` 实测编出 **0/1/2**（应为 11/12/13）。四处统一走同一套常量折叠，并允许引用已定义的枚举成员。

### ③ BASIC 前端：全局变量区「改了一半」的回归 + SUB 内 FOR

- **全局段清零循环起址算错**（`EmitStaticAddr` 现成没用上）。⚠ 实测更正：`ADD R1,[R1]` 在本 VM 里**是空操作**，起址恒为绝对 `0x5000` —— 属**潜伏**缺陷，真正的错是「基址被丢掉」。另发现 `0x6FD4`（静态基址槽）**全仓从未被写入** ⇒ 「动态静态区」是半成品。
- **READ / INPUT / 记录字段 / SUB 内 IO 共 13 处仍写旧地址**（比清单多 1：BYREF 实参取全局地址）。实测：`DIM x: INPUT x: PRINT x` 从 `0` → **`7`**；`SWAP a,b` 从 `1 2` → **`2 1`**；记录字段跨帧从 `0` → **`42`**。
- **`EmitLoadVar` 丢类型化 load**（写类型化、读不类型化）。
- **SUB 内 FOR 永不推进**：两处 MOVE 操作数顺序反了（dest-first 写成 src-first ⇒ 是**读**）。t10 从「10 秒跑 10 亿条指令」→ **16777 条、0.69 秒、输出正确**。
- 顺带更正两条误导性注释。**t9 随之从「死循环」变成「跑完但值错」**（数组仍在主帧寻址）= 下一道墙，**不是回归**。

### ④ 设备端验收装置（`scripts/maui-vml-verify.sh`）

- 授 `MANAGE_EXTERNAL_STORAGE` ⇒ workspace 落到 `/sdcard/waycoder/workspace`，**`adb push` 直达**（先前 `base64 + $IFS` 那套绕法整个不需要了）
- 逐条跑 `vml run <文件>`、**每条跑完即落盘**、可中断续跑、出 `report.tsv`
- 踩过并写进注释的坑：`adb shell` 的引号是「哪一层在解析」；**`input text` 时好时坏**（改键码注入）；**回车不触发提交**（MAUI 的 `Entry.Completed` 挂 `EditorAction`，由输入法发出，无输入法则裸 `KEYCODE_ENTER` 落不到）⇒ 改点「运行」按钮；聚焦后光标停在行首会把文本堆在开头
- **第一份基线只跑完 4 条**（`vml test` / C / C# / Go 通过，C++ 编译失败，Swift 超时）。**这个数字不足以支撑任何决策，如实标注。**

### ⑤ iOS / MacCatalyst 构建修复

- 裸 `net10.0-ios` 会落到工作负载清单默认版本（26.5），而**那个 SDK 硬性检查 Xcode 版本**，本机 Xcode 27.0 ⇒ 构建 7 秒即失败。改为显式钉 `net10.0-ios27.0`，留成可覆盖属性 `-p:AppleSdkVersion=26.5`。
- MacCatalyst `SupportedOSPlatformVersion` 15.0 → **17.0**（SDK 27.0 起抬了下限）。
- ⚠ **仍未通过**：卡在 `actool` 报 `You have not agreed to the Xcode license agreements`（需 `sudo xcodebuild -license accept`）。注意 `xcodebuild -version` **不检查**许可、`git`/`actool` **检查** —— 别用一个工具的可用性去推断另一个。

### ⑥ 两条调研结论（避免走错路）

- **`Lib/` 不要「全删再全量重生成」**：上游生成管线跑得通，但产物**不是现在这份 `Lib/`** —— 会坏 1904 个设备头文件（寄存器地址 `0x00000020` → `+20`，根因 `GenDev/GeneratorBase.cs:225`）、27~83 个 `Lib/shared/*.vml` 栈帧尺寸变、**43 个 `Lib/<lang>/{ustring,wstring}.vml` 永久消失**（无生成器，占全库 LOAD/STORE 的 94.6%）。
- **ABI 现状盘点**：22 种语言实测 **10 种**能跑通自己的函数调用；现状不是「两套约定并行」，是「**一个有基准的体系（C cdecl）+ 若干离群者 + 若干自身调用都不通的半成品**」。另：`stdcall` 与 **Pascal 约定是两个不同的东西**（stdcall = 右→左 + 被调方清栈；Pascal = 左→右 + 被调方清栈），上游产物实现的是前者。



## v0.96.196 (2026-09-16) — 真机验收：Go / Kotlin 在手机上正常；发布后又抓出飞机空战一处"游戏自己停了"
这一版**只动源码与文档，没有重新出包** —— 手机上装的仍是 0.96.195 的 APK，
而它**已经含这三个游戏的最新版本**（示例包在出包前重打过，手机上三个新文件与仓库 **MD5 逐个一致**，已核）。

### 真机验收（用户实测）

* **Go 贪吃蛇在真机上运行正常** ⇒ 这一门连同 patch 0014 / 0015 / 0017 三处前端修复可以结案。
* **Kotlin 也正常** —— ⚠ 这与报告 §4.2C 的结论表面冲突：那里写着 Kotlin「数组路径四处缺陷已修，
  但**数组读仍未解决**（`arrayOf(10,20,40,80)` 只对下标 3）」。两者不一定矛盾
  （"Kotlin 能编能跑" ≠ "数组读对"），但**也可能是那轮判定错了** ⇒ 下一轮动 Kotlin 之前先核实
  "跑的是哪个程序、有没有读数组"。**这条不先澄清就开修，很可能白修一遍。**

### 发布后又抓出的一处（飞机空战）

敌机槽位从 4 改成 6 之后，**"敌机 × 本机 / 敌机出底"那一圈忘了跟着从 `4` 改成 `foeSlots()`**
⇒ 第 5、6 号槽的敌机**永不回收、也永不扣命**，槽位占满之后**再也没有敌机出现** ——
画面还在动（星空在滚），分数不涨，**静默地"游戏自己停了"**。

修完实测（同一脚本 500 拍）：命数 3→2→1→0 → 重开，共 3 局、最高 40 分；
末帧图元数 21（改前 31 —— 泄漏的槽位没了）。

> **教训**：`SLOTS = 6` 这种常量定下来之后，要 **grep 一遍所有遍历它的循环**。
> 它和 v0.96.183 那条"只加一句报错是不够的"是同一类：**改了一处、另一处还写着旧常量**，
> 而且症状是"少做一件事"而不是"做错一件事"，更不容易发现。

### 下一轮（按性价比）

1. **Kotlin 数组读**（先按上面那条核实，别照旧结论直接开修）；
2. **Go 剩的两条**：字符串拼接编出来是空串；函数名撞标准库被静默链到 `lib_util_sum`；
3. 其余语言：Python / Ruby / Dart / R 的循环或函数会挂住；JavaScript 的标识符解析 + 正序压栈；
   Lua / BASIC / Forth / Scheme / Fortran / ObjC 各自的硬伤（见报告第 4 节）。

## v0.96.195 (2026-09-16) — 三门语言的游戏齐了：新增 Swift 飞机空战 / C# 赛车 / Go 贪吃蛇

用户要的是"**先给那三个写游戏**"（Swift / C# / Go），并且**都要收进 `Examples/` 传到手机**。
三个游戏都写完、都**逐像素量过**、也**量过可玩性**。过程中又挖出 Go 前端两条、Swift 前端一条。

### 三个新游戏（都已验证）

| 文件 | 玩法 | 实测判据 |
|---|---|---|
| `Examples/swift/plane.swift` | 飞机空战：自动开火、左右跑位拦敌机、漏 3 架结束 | 本机 406px，方向键 x±16 / y±10；500 拍 3 局、最高 30 分 |
| `Examples/csharp/racer.cs` | 赛车：三车道、左右换道、躲过 +10 分并提速 | 车 62×111、换道 ±86px、虚线 y 在 40/58 循环、撞车重开 |
| `Examples/go/snake.go` | 贪吃蛇：转向 / 暂停 / 重开 / 撞墙结束 | 头 248px（1 格）/ 身 496（2 格）/ 食物 248（1 格）|

### ⚠ 三个游戏的第一版**都能"跑"但不能"玩"** —— 这只有量了几百拍才看得出来

* **Swift 飞机**：子弹沿本机那一列上行，本机不动就是一道"子弹墙"，那一列的敌机在顶端就被打掉
  ⇒ **500 拍一次都没输过**，也几乎得不了分。改成"**放跑一架扣一条命**（共 3 条）"才成为游戏。
* **C# 赛车**：敌车每 5 拍刷一辆，而穿过整条路要 98 拍 ⇒ 玩家在"任何一辆车跑到路底之前"就被撞死，
  **分数永远是 0**。改成 `26-速度` 拍刷一辆 + 速度 8 起。
* **Go 贪吃蛇**：第一版**压根编不过**（见下）。

**像素判据只能证明"画对了"，证明不了"能玩"** —— 三个都得跑起来量几百拍。

### Go 前端又两条（patch 0017 + 0015 增补）

**① 控制语句头部里的复合字面量 —— 一条 bug 假扮成四条"语法限制"。**
`for i < A[1] {` 里那个 `{` 被 `ParseExpressionSuffix` 当成 `A[1]` 的**复合字面量**，
**整个循环体被当元素列表吃掉**；报错却是 `Expected RBRACE, but got IF`（位置指向被吞掉的
循环体第一条语句）。第一轮因此误判成"循环体里 `if` 必须排在赋值之前""不能嵌套循环 / `if`"
"函数不能带参数"，把整份代码改成了扁平风格 + 全局状态数组。修法是 Go 规范本来就有的规矩：
**控制语句头部（`if`/`for`/`switch` 的条件、`for` 的 init/post）里禁止复合字面量**。
修完实测**嵌套循环、嵌套 `if`、循环体里 `:=`、先赋值再 `if`、带参带返回值** 一次全过。

**② 形参只搬了第一个 —— "操作数写反"+ 少算一个槽位。**
函数序言写着"第一个参数在 R0 里"，只搬了 `[R14-4]` 一处 ⇒ **除末参外全是垃圾**。
两处错叠在一起：偏移少算了 **CALL 自己压入的返回地址**那 4 字节（应为 `[R14+8+4*i]`），
且那一支**操作数写反**（`MOVE [栈槽], R0` 是把 R0 存回调用方的实参槽）。
修完三参函数逐个精确：`rep(3,5,7)` → 30/50/70 ✓。

**还剩两条真的限制**（都写进了 `snake.go` 的文件头）：**字符串拼接编出来是空串**；
**函数名撞标准库会被静默链到库函数**（自定义 `func sum` → `lib_util_sum`）。

### Swift 前端一条（patch 0011 增补）

**第 5 个参数起全是第 1 个参数的值** —— 写飞机空战时 8 参的 `overlap(...)` **恒判相交**
（敌机一冒头就被打掉）。同样是两处错叠加：偏移写成 `8 + (pi - 4) * 4`
（而调用方是**全部实参都压栈**的，"只有第 5 个起在栈上"是错的），**且操作数写反**。
修完 `show(1,2,3,4, 200,150,7,9)` 的 bx/by/bw/bh 从"全是 1"变成 **200/150/70/90** 逐个精确。
**"操作数写反"族第 9、10 次。**

### 顺带

* `vmlhost` 的 `--sim` 支持**按键脚本**（`k37` = 左方向键）—— 没有它，"方向键能不能动"
  只能靠猜；现在三个游戏的方向键都是脚本喂进去、逐帧量出来的。
* 补丁表更新：0011 / 0015 重算，新增 **0017**（Go 解析器）；
  `scripts/check-vml-patches.sh` **17 条全绿**。
* 报告 `docs/前端游戏能力评估.md`：§0 结论改成"Swift / C# / Go 都已经能玩"，
  §4.2'' / §4.5 补两条根因，§8 换成三个游戏的验证表 + "能跑≠能玩"那条教训。

## v0.96.194 (2026-09-16) — Kotlin 数组路径四处缺陷已修，**但数组读仍未解决**（如实交代）

Kotlin 与 Pascal 症状同族，但它的标签走基类 `L_{n}`（安全）⇒ 真因另找。读汇编看到
**四处各自独立、且都在汇编层面自证是错的**缺陷（patch 0016）：

| # | 原来的写法 | 为什么一定是错的 | 改成 |
|---|---|---|---|
| ① | `MOVE R0, [R1+off]`（写数组字面量的元素）| **dest 在前** ⇒ 这是**从数组读**，元素值根本没写进去 | `MOVE [R1+off], R0` |
| ② | 下标一律按**字符串那套**（`addr+idx`、1 字节、不跳数组头）| 布局是 `[count, e0, …]`，元素 i 在 `base + i*4 + 4` | 数组走 `idx*4+4`，并用标记区分字符串 |
| ③ | "load byte" 是 `MOVEB R0, R0` | **自赋值、空操作** ⇒ 地址被当成元素值 —— **"地址当值"族第七次** | `MOVE(B) R0, [R0]` |
| ④ | `arrayOf` 只把 count 放进 R0 就 CALL | 被调用方 `array_alloc` **按 C 约定读 `[R12+12]`** ⇒ 分配尺寸与返回指针都是垃圾 | 先 `PUSH R0` |

### ⚠ 仍未解决 —— 不拿它冒充修好

修完四处后读数**只对下标 3**（`arrayOf(10,20,40,80)`，把读到的值当 x 坐标画）：

| | `a[0]` | `a[1]` | `a[2]` | `a[3]` |
|---|---|---|---|---|
| 实测 | 0 | 0 | 0 | **80** ✓ |
| 期望 | 10 | 20 | 40 | 80 |

而且读数**会随元素的值变**（换成 `arrayOf(2,4,6,8)` 时 `a[0]` 读到 4、`a[1]` 读到野值）
⇒ 不是"固定偏一格"，更像基址/偏移里混进了一个**数据相关**的量。**未定位。**

按本仓库的规矩：**四处改动每一处都在汇编层面自证是错的、且实测无回归**，
所以留下来（比原来更接近正确）；但**不宣称 Kotlin 能写游戏**。
报告 §4.2C 记了入口：用"值即坐标"的画法逐下标对。

### 回归

`fib(n)` 那个例子（递归 + 函数 + 条件）**实测无回归**：`fib(10)` 画出来宽 **55** ✓。
`check-vml-patches.sh` **16 条全绿**。

## v0.96.193 (2026-09-16) — Go 的数组补齐（三处 + 一处栈帧）→ **四关 + 游戏心跳全过**

patch 0014 修掉 `L{n}` 标签洞之后 Go 的死循环好了，但数组还是坏的：实测 `a[2]`→8、`a[5]`→20、
`a[7]`→28，**全是 `idx*4`**。读汇编看到**三处都缺，不在一个地方**：

| # | 缺什么 | 修法 |
|---|---|---|
| ① | `var a [8]int` **不分配** —— 只按 `GetTypeSize`（默认 4）留一个槽、从不初始化 | 用基类现成的 `AllocateVmlArray`（布局 `[count, e0, …]`）建块，把**块地址**存进变量槽 |
| ② | `a[i] = v` **整段没有代码生成** —— 赋值目标是下标时谁都不认（`a[3]=5` 只编出一句 `move R0 #5`）| `GenerateAssignment` 补 `IndexExpr` 分支 |
| ③ | 读取结尾是 **`MOVE R0, R0`（空操作）**，且偏移**少跳 VML 数组头** | `MOVE R0, [R0]`；补 `+4`。字符串索引分支同族 |

③ 与 Swift 的 patch 0011 ③ 同一个病（**操作数写反**）—— **"地址当值"族第六次**。

### 更隐蔽的一处：栈帧算少了 = 踩内存

`CalculateLocalSpace` **只看块里的顶层语句**，`for i := 0; …` 里的 `i` 与循环体内声明的变量
都没算进去 ⇒ 帧开小了，局部变量落到保留区之外，而表达式求值的 `push`/`pop` 正好写在那里
⇒ **每次求值都把循环变量冲掉**。症状极具迷惑性（差别只在"循环体里读了一次数组"）：

```
for i := 0; i < 5; i++ { s = s + a[3] }   → 只累加 2 次（10 而不是 25）
for i := 0; i < 5; i++ { s = s + 1 }      → 又是对的（50）
```

现在递归进 if / for / switch 统计。**少算 ≠ 只浪费空间，少算就是踩内存**，故宁可多算。
同批还修了 `GenerateLocalVarDecl` 里 `Mem($"R14-{-localVarOffset + typeSize}")` **多一个负号**
（`R14--4` 被 `ResolveRegOffsetString` 解析成 `R14 + 4`，往调用方写）
⇒ **`var x int = 5` 的初值从来没生效**。

### 验证（画布 200×200，全部逐像素量）

| 探针 | 结果 |
|---|---|
| `a[3] = 5; a[3]*5` | **25** ✓（改前 60 = 12×5）|
| `a[1]=3; a[4]=7; a[7]=9` 各画一条 | **30 / 70 / 90** ✓ 三个互不串 |
| `for i<5 { s = s + a[3] }` | **25** ✓（改前 10）|
| `for i<5 { s = s + 1 }` | **50** ✓ |
| **游戏心跳**（开窗 → 定时器 → `ui_wait_msg` → 累加 → 重画 → `ui_present`）| **4 帧**，末帧绿条 **60 = 3×20** ✓ |

⇒ **Go 四关全过、游戏心跳也验过** —— 与 Swift / C# 同一条线（实际游戏尚未写）。

### 补丁与回归

新增 **0015**（Go 数组 + 栈帧）；0014 追加补记。`check-vml-patches.sh` **15 条全绿**；
`Examples/` 25 个示例全部编译通过；Swift 与 C# 贪吃蛇仍 316/632/316；C 俄罗斯方块仍正常出图。

## v0.96.192 (2026-09-16) — `L0` 不是标签、是长整数寄存器（Pascal 数组通了，Go 死循环好了）；**并撤回我上一版做错的那次"复核"**

### 一、真因：`$"L{labelCounter}"` 生成的前 8 个标签叫 `L0`…`L7`

`VMLAssembler.ParseOperand` 里 **`L0`–`L7` 就是长整数寄存器**（R24–R31）：

```csharp
if (operandStr.StartsWith("L", ...) && int.TryParse(...) && regNum >= 0 && regNum <= 7)
    return new Operand(OperandType.REGISTER, regNum + 24);
```

⇒ **`jge L0` 编译出来是"跳到寄存器"**，而 `ExecuteJmp` 只处理 `LABEL`/`IMMEDIATE`
⇒ **静默什么都不做**、直接往下走。四个前端**自己拼**标签名时都写成了 `$"L{n}"`：
Pascal（越界检查两条分支都不跳 ⇒ 落进"越界→退出" ⇒ 一帧不出）、
Go（死循环被 vmtimeout 杀）、Forth、C（**C 一直没暴露是运气** —— 它另有 `L80190001`
形态的大数字标签，`regNum <= 7` 不成立就落回标签分支；而这个 `GenerateLabel()` 只在 3 处用）。
基类 `CompilerBase.NewLabel()` 生成的是 `L_{n}`（下划线，`int.TryParse("_0")` 失败）本就安全 ——
这四条只是没跟着基类写。**只在"标签编号 < 8"时冒头**，所以极难复现。

### 二、撤回 v0.96.191 那次"复核"（我错了）

上一版我写"报告初版那条 `jl R30` / `jge R24` 在当前树上不成立"，**是错的**：
我当时 grep 的是 `j[..] R<数字>`，**没认出 `L0`–`L7` 本身就是寄存器的拼法**。
报告初版的**方向完全正确**，只是它把 `jge L0` 写成了 `jge R24`。已在报告里以"本节替换掉
v0.96.191 那一版，撤回"的形式更正，并写清机理。

**还记一条无效实验**：定位过程中我手写汇编用 `L0` 当标签、量出来"跳了"，于是以为跳转没问题 ——
实际那个探针里**跳与不跳都落到同一句** `move R0 #10`，量出来都是 10。
**判据必须让两种结果落在不同的地方**，否则它只是在确认自己。
真正定案的是"把生成物里的标签整体改名（`L0`→`ZZ0`）再跑，立刻出帧"。

### 三、结果

| 语言 | 变化 |
|---|---|
| Pascal | ✅ `a[3] := 5; s := a[3]` 画宽 `s*10` 的矩形，**逐像素量出 50**。四关全过（游戏心跳未验，**不宣称能写游戏**）|
| Go | 死循环修好（出帧、循环正确：`for i<5 {s=s+1}` ⇒ 宽 50）；**但数组读回的是字节偏移**：`a[2]`→8、`a[5]`→20、`a[7]`→28，全是 `idx*4`。与 Swift patch 0011 ③ 同族（**"地址当值"第六次**），**尚未修** |

顺带修掉 Pascal 数组地址方向（`base − (i−下界)×4` → `+`；数组块在数据段里是向上排的）。
⚠ 这一处**不是**"一帧不出"的原因（改完症状不变），是先落进来的独立修正。

### 四、补丁与回归

新增 **0013**（Pascal：标签 + 地址方向）、**0014**（Go/Forth/C：标签名）。
`check-vml-patches.sh` **14 条全绿**；`Examples/` 25 个示例全部编译通过；
**C 俄罗斯方块仍正常出图**（我动了 C 的 `GenerateLabel`，必须验）；
Swift 与 C# 贪吃蛇回归仍是 316/632/316。

**这一轮最该记住的**：报告里"某条证据不成立"这种结论，**必须先确认自己找的位置对**——
`L0` 与 `R24` 是同一个寄存器，只按一种拼法 grep 就会得出相反结论。

## v0.96.191 (2026-09-16) — Pascal 数组地址方向修了（但那不是全部）；**并更正评估报告里两条不再成立的证据**

### 一、Pascal：数组元素地址算反了方向（patch 0013）

`CodeGenerator.Statements.cs` 里数组地址写的是 `base − (i − 下界) × 4`，
而汇编器对数组块的布局是**向上**的（`labels[label] = currentAddress` 指向块的**第一个字**）
⇒ 下标 = 下界时侥幸对（偏移 0），**下标 ≥ 下界+1 就写到数组"前面"**，
把数据段段首之前的内存踩了。已改成 `ADD`。

二分判据（出不出帧）：不声明数组 ✔；只声明不碰 ✔（宽 50）；写 `a[1]/a[2]/a[3]` ✘；
写+读 ✘ ⇒ 是**写**这一步跑飞。

⚠ **改完症状不变（还是无帧）** ⇒ 这条路径上还有**第二个**缺陷，**尚未定位**。
已排除：`cmp/jge/jle` 语义、上下界检查条件本身（按汇编逐条核过）、
`move R0 <数组名>` 是不是 LEA（手写汇编走同一条链不报错）。
**报告里如实写着"Pascal 仍不能写游戏"，没有拿这一处冒充满分。**

### 二、更正评估报告的两条证据（这条比上面那条更重要）

`docs/前端游戏能力评估.md` 初版把 Go / Pascal / Kotlin 归因于「条件跳转编成了 `jl R30` /
`jge R24`，而 R24–R31 是长整数寄存器、从没人赋值 ⇒ 静默不跳」。**复核：当前树上不成立** ——

```
grep -rnE "^\s+(jl|jle|jg|jge|je|jne|jz|jnz) R[0-9]+" <各前端产出的 .vml>   → 零命中
```

Go（带循环）、Pascal（数组越界检查）产出的都是**标签跳转**；而 `SetFlags` 里 **CMP 不带 carry**，
`cf` 恒 false ⇒ `JL/JLE/JGE` 退化成 `SF` / `zf||SF` / `!SF`，**有符号语义是对的**。
初版那段 `jl R30` 的出处已不可考。

**症状仍然成立**（2026-09-16 重新实测）：Go 的探针跑成死循环被 `--vmtimeout` 杀掉（无帧）；
Pascal 任何一次数组访问都不出帧。⇒ 真因**待重新定位**，报告里已注明"别再照那段旧汇编去找"。

### 三、这一轮的方法论

两门语言（Swift v0.96.189 / C# v0.96.190）能跑通，共同点是**先把生成的汇编倒出来读** ——
Swift 上换写法试绕了四轮，改成读汇编一步就中。
**报告里没有复核过的证据，这次也被抓出两条** —— 判据要能重跑，才算判据。

## v0.96.190 (2026-09-16) — C# 前端四条缺陷 → C# 贪吃蛇跑通（第二个不用 C 写的手机游戏）

C# 是本项目的主语言，之前评估报告里它卡在"带参方法参数串槽"。真拆下去发现是**四条**、
而且根子只有两类：**操作数顺序写反**（三条）与**字段初始化没生成**（一条）。
全部落在 `patch 0010`（已改名为 `0010-csharp-frontend-fixes`，覆盖 `CodeGenerator.cs`
+ `ASTNode.cs` + `Parser.Expressions.cs`）。

**① 带参方法的实参读反了**（`CodeGenerator.cs` 序言）
"把实参读进 R0"那句写成了 `MOVE [R12+12+4i], R0` —— 而 `MOVE dest, src` 的 dest 在前，
那是**把 R0 存进调用方的实参槽**。局部变量于是拿到"调用那一刻 R0 恰好留着的值"
（= 调用点最后求值的那个实参）。症状极具误导性：`a1(x)` 侥幸对（调用点最后一句就是
`move R0 #10`，正好是 x）、`a2(x,y)` 的 y 变成 x ⇒ **两个矩形完全重叠**（所以"只看着一个"）、
`a3(x,y,c)` 的 c 变成 x ⇒ 颜色近黑、在黑底上看不见。

**② 读数组元素永远读到数组头**
`ADD Rd, Rs` 的 2 操作数形式是 `Rd = Rd + Rs`，而 `SHL [#2, R0]` / `ADD [#4, R0]`
**把立即数当 dest ⇒ 结果被丢弃**（VM 里写的是 `if (dest.Type == OperandType.REGISTER)`），
接着 `ADD [R0, R1]` 又把和写进了 R0，而下一句读的是 `[R1]`。写入路径同样三处、
外加"保存值"那句 `MOVE R0, R2` 也是反的。两条路径一起改成基类
`EmitArrayElementOffset` 那套（寄存器在前 + 3 操作数）。

**③ `a` 指向的不是数组块**
数组字面量结尾 `MOVE R0, LABEL ptrLabel` —— **LABEL 是 LEA（取标签地址）**，
不是"取标签里的值"；`.Length` 那处一样。于是 `a[i]` 的基址偏到"指针槽自己"去了。
**这与 patch 0010 原本修的"全局变量被读成标签地址"是同一个病**（本仓库第五次遇到 `LABEL`
与 `MEMORY` 混用）。

**④ `static int[] sx = new int[N];` 从来没分配过**
类字段在 `ClassDeclaration` 里一律 `RegisterStaticField(...); continue;` —— 数组块的生成
整段被跳过；而 `new int[N]` 的**长度在解析阶段就被丢掉**（返回一个空数组字面量），
所以不是"少分配"，是"没分配 + 基址是 0"，之后每次 `sx[k]` 都在地址 0 附近读写。
配套还有一处结构坑：入口 `main` **就是** `Main` 的标签，在它之前发的指令全是死代码
（与 Swift 的 patch 0011 ① 同类）⇒ 字段数组的初始化改为**在 `Main` 体内发**。
长度现在支持**字面量与常量标识符**（`new int[MAXLEN]`），折不出来则**硬报错**。

**实测判据**（都逐像素量，不看"编译过了"）：
`a1/a2/a3/a4` 四个矩形的 bbox = (10,10,20,20)/(10,30,20,20)/(10,50,20,20)/(10,70,30,40) 全对；
`a[2]=7; if (a[2]==7)`、`.Length`、局部/静态数组各一组全对；
`Examples/csharp/snake.cs` 蛇头 316 像素（1 格）/ 蛇身 632（2 格）/ 食物 316（1 格）。

`Examples/` 下 25 个示例改动后全部照常编译通过；Swift 贪吃蛇回归仍是 316/632/316；
`check-vml-patches.sh` 12 条补丁全绿。

## v0.96.189 (2026-09-16) — 括号表达式恒等于 0（Swift/C#/JavaScript 三家同一个洞）→ Swift 贪吃蛇跑通

`GenerateExpression` 的 switch 里**没有 `ParenthesizedExpression` 分支**，落进 `default:`
被静默编成 `move R0 #0` ⇒ **`(任意表达式)` 恒等于 0**，而且**编译全绿**。
三家前端的解析器都建了这个 AST 节点（`Parser.cs` 里 `new ParenthesizedExpression(expr)`），
代码生成都漏接了 —— 只有 Java 接上了。

**症状有多误导**：贪吃蛇里 `A[16 + (k) * 2]` 六次写入**全落到同一个槽**（因为下标恒为
`16 + 0*2 = 16`），换成纯常量下标 `A[16]…A[21]` 就全对 ⇒ 看起来像"算式下标不能在多条语句里复用"。
同一个洞还让棋盘格 `(c + r) % 2 == 0` 从来没交替过。

**判据不是看现象猜，是读生成的汇编**（`vmlhost asm`）：修前 `(k)` 处就是 `move R0 #0`。
这个手段一步定位，比前面几轮"换个写法试试"快得多。

**改动**（patch 0011 扩充 / 0010 扩充 / 新增 0012）：
1. 补 `case ParenthesizedExpression paren:` —— 递归生成内层表达式；
2. 类型推断同步补一条（括号不改变类型）；
3. `default:` 从"静默发 0"改为**抛 `CodeGenerationException`** ——
   这类"编译全绿、跑起来错"的故障最该编不过（与 `DrawCommand.Vector` 定成必需成员同一个理由）。

回归：`Examples/` 下 25 个示例改动后全部照常编译通过；`check-vml-patches.sh` 12 条补丁全绿。

**Swift 贪吃蛇跑通**（`Examples/swift/snake.swift`，逐像素量）：蛇头 316 像素（1 格）、
蛇身 632（2 格）、食物 316（1 格）—— 三个数都是精确整格；棋盘格图元数 180 = 20×18 的一半，
说明交替也对了。**这是第一门不用 C 写出来的手机游戏。**

C# 贪吃蛇仍画不出蛇 —— 卡在**带参方法的参数串槽**（`drawCell(sx[k], sy[k], HEAD_C)` 三个参数），
这是它与 Swift 之间还剩的那一条，下一步修它。

## v0.96.188 (2026-09-16) — 修 Swift 读数组读回**字节偏移**（影响面最大的一条）

`GenerateIndexAccess` 最后一句写的是 `MOVE [Mem("R1")], R0` —— 而 `MOVE dest, src` 的 dest 在前，
那是"把 R0 **存**进 R1 指向的地址"，可 R0 此刻装的正是**字节偏移** ⇒ **读数组读回来的是偏移**。
改成 `MOVE R0, [R1]`。

**证据（解 PNG 逐像素量宽度，不看"编译过了"）**：

| 写法 | 读到 | 画出来 | 期望 |
|---|---|---|---|
| `A[2]`（常量下标）| 12 = 2*4+4 | 96 像素 | 24 |
| `A[i]`（变量下标）| 12 | 96 | 24 |
| `A[16+k*2]`（算术下标）| 92（被屏幕截断）| 385 | 32 |
| `A[3]`（动态写后读）| 16 = 3*4+4 | 128 | 56 |

三个数**全对得上偏移公式**。这是"**地址当值**"那一族 —— 与 C# 的 LABEL/MEMORY（patch 0010）、
BASIC 的 `MOVE reg, R2`（patch 0007）同一个毛病，本仓库第四次遇到。修完四项全对（24/24/32/56）。

**顺带一条方法论**：诊断时我量了**第一帧**（诊断画在第二次 `ui_present`、落在 `f001`），
于是得出"三个诊断全 0"的假结论 —— **先确认量的是哪一帧，再下结论**。

**贪吃蛇现状**：能画出窗口 / 棋盘 / HUD / 食物 / **一节蛇身**（应当 3 节）。而同一份状态在
`main` 里量是对的（`A[1]*20 = 60`、`A[14]/10 = 39`，都逐像素量过）⇒ 状态没写错，
是 `draw()` 里那段循环没跑满，**未定位**。


## v0.96.187 (2026-09-16) — 修 Swift 第二条阻塞点：函数的局部变量漏进全局（patch 0011 合并）

Swift 的两条阻塞点都修掉了（同一个 `CodeGenerator.cs`，合成一条补丁）。

### 一、现象与根因

```swift
func count5() -> Int { var i = 0; var s = 0
                       while i < 5 { s = s + 1; i = i + 1 }
                       return s }        // 实测返回 3，而不是 5
```

两条都不对：函数序言**只给参数**在栈帧上留位子、函数体里的局部变量一个都不留；
而 `GenerateVariableDecl` **无条件写数据段**（读的那几处虽然先查 `_localVarOffsets`，
但那儿根本没有局部变量的名字）⇒ **所有局部变量都变成"全局"**，不同函数的同名局部互相踩
（游戏里 `draw` / `step` / `occupied` 三处都有 `var_i`，画面立刻烂）。

### 二、改法

- 序言里**递归预扫描函数体**（block / if / while / for / do-while / switch），
  给每个局部变量留 4 字节（`ReserveLocals`；不进嵌套函数，它有自己的帧）。
- `GenerateVariableDecl` **栈帧优先**：查到就存 `R14±offset`，查不到才回退数据段
  （那说明是模块级声明，本来就该是全局）。

### 三、一条验证上的教训

改完先**用眼睛估**方块宽度，估成 24 像素、以为没修好 —— 后来**解 PNG 逐像素量**才确认是
**40 = 5×8** ✓（改之前是 24 = 3×8）。**宽度/尺寸这类东西不该靠眼睛**，这已经是本会话
第三次栽在"目测"上了。

### 四、Swift 的第三条（只定位到形态）

`A[f(i)]`（**下标位置里调函数**）编出来不对：贪吃蛇的蛇身用 `A[segx(k)]` 时 0 像素，
而食物用常量下标 `A[4]` 正常。示例里已把下标算术**内联**（`A[16 + k*2]`）绕开。
这条要不要修、动态下标本身可靠到什么程度，还没测准（第一次测的测量脚本太粗）。


## v0.96.186 (2026-09-16) — 修 Swift 第一条阻塞点：全局初始化成了死代码（patch 0011）

按"**先修问题最少的语言**"排下来，第一门是 Swift（只差两点）。这一版修掉第一点。

### 一、现象与根因

```swift
var A = [0, 0, 0, 0, 0, 0, 0, 0]
func writer() { A[1] = 3 }
func reader() -> Int { return A[1] }      // 读回来不是 3
```

生成的程序头是 `.entry main`，而 `main` 那段本该是"顶层语句"：

```
.text
        move R0 arr_data_0      ← 全局数组指针的初始化
        move [var_A] R0
        syscall #3              ← 紧跟着就退出
writer:  …
main:    …                     ← 入口却在这里
```

`Generate()` 的设计是"**没有 `func main` 时**入口就是顶层那段"；一旦程序自己定义了 `main`，
顶层那段**永远不执行** —— 而全局变量/数组的初始化恰恰就在里面 ⇒ `var_A` 恒为 0 ⇒
所有 `A[i]` 读写都落在地址 4/8/… 那片**低内存**上。同一函数内读写偏移一致所以"看着正常"，
跨函数立刻露馅。

### 二、改法与一处容易只修一半的地方

**不搬指令、只挪标签**：块首补 `main` 标签、块尾 EXIT 前插 `call __user_main`，
执行顺序变成「全局初始化 → 用户 main → 退出」。

⚠ **序列化器认的是指令流里的 LABEL 伪指令，不是编译器那张 `labels` 表** ——
只改表的话函数身上那个 `main` 标签还在流里、入口照样指向函数。第一版就是这么白改了一次，
**是"出图一看"发现的**（又一次印证：构建全绿证明不了任何事）。

### 三、Swift 的第二条（未修，已精确定位）

`func count5() -> Int { var i = 0; var s = 0; while i < 5 { s = s + 1; i = i + 1 } return s }`
实测返回 **3** 而不是 5 —— 数据段里能看到 `var_i` / `var_k` / `var_tries` / `var_c` / `var_r`
这些**函数局部**：**局部变量漏进了全局数据段，不同函数的同名局部互相踩**（游戏里
`draw`/`step`/`occupied` 三处都有 `var_i`，画面立刻烂）。**这条修完，Swift 就能写游戏了**
（贪吃蛇已写好、只等它）。


## v0.96.185 (2026-09-16) — 22 个前端逐个评估「能不能不用 C 写游戏」+ 修掉两处静默缺陷

一句话：为了"不用 C 再写几个游戏"，把 22 个前端逐个过筛（编译出图 / 循环 / 数组 / 函数 /
取址 / 定时器收消息），**只有 Swift 干净通过**，其余各自差一处修复；顺带修掉两个真实缺陷
（都是"编译全绿、跑起来不对"）。完整报告见 **`docs/前端游戏能力评估.md`**。

### 一、评估方法与矩阵（判据可量）

四关 + 两关手工：A 编译出图（帧里 1 个图元）· B 循环（5 个）· C 数组写读（5 个）·
D 带参函数（5 个）· E 取址 · F 定时器+收消息+重画。跑无头宿主（与手机同一条渲染链）。

| 档 | 语言 |
|---|---|
| 现在就够 | **Swift**（C/C++ 是基准）|
| 修一处 | **C#**（带参方法参数串槽）· **Pascal/Kotlin**（数组路径跳转跳寄存器）|
| 有兜底 | Python / Ruby / Dart / R（循环或函数挂住；数组读回 0，可用宿主侧网格绕过）|
| 不适合 | Go · JavaScript · Lua · BASIC · Forth · Scheme · Fortran · ObjC · Rust · D |

逐条根因（都带汇编证据）在报告里。三条最典型的：**Go** 把条件跳转编成 `jl R30`（跳**寄存器**，
而 R30/R31 是 L0–L7 长整寄存器、**整份程序里从没被赋值**；VM 的 `ExecuteJmp` 只认标签与立即数
⇒ 分支静默什么都不做 ⇒ 死循环）；**JavaScript** 把未知函数名当变量（`var_ui_rect` + 间接
`call R0`）且实参**正序压栈**（包装读的是逆序）；**Pascal** 同 Go，且落在**数组越界检查**上 ⇒
"越界就退出"那条分支永远成立、程序连窗都没开就退了。

### 二、修掉的两个缺陷

- **补丁 0009：`vmltool.config.xml` 给全部语言挂 `vmlui.vml`**。原来只有 c/basic/python 有，
  其余 19 种 `Libs=""` ⇒ 非 C 语言写 `ui_rect(...)` **运行期**报 `未找到标签`。
  ⚠ 顺带查出**更严重的一条**：给 c/basic/python 挂 vmlui 那三处**本来就是我们本地的改动、
  从来没进补丁**，而这个文件由 `sync.sh` 用 `cp` 覆盖 ⇒ **跑一次同步，C 的游戏示例会全部编不过**。
  同时给 `scripts/check-vml-patches.sh` 补上这个文件与 `VERSION` —— 判据漏一个文件，
  那道防线对它就等于不存在。
- **补丁 0010：C# 前端读全局变量读成了地址**。`const int N = 5;` 实测画出 **1026** 个方块而不是 5：
  数据段读取用了 `OperandType.LABEL`（那是"标签的**地址**"，C 前端正用它物化指针），
  改成 `MEMORY`（两读一写）；另外类字段此前被当成栈上局部变量 ⇒ **初值整个丢掉**，
  现补 `RegisterStaticField`（登记数据段 + 折字面量初值）。

### 三、游戏：写出来了，但**还没到能玩**（诚实交代）

`Examples/swift/snake.swift` 与 `Examples/csharp/snake.cs` 都能编译、能出图
（窗口尺寸、棋盘网格、HUD 文字都出来了），但蛇身/分数画不出来：
Swift 卡在"跨函数读写全局数组读回来不对"，C# 卡在"带参方法的参数串槽"。
**不是写不出来，而是这台 VM 上目前没有一门前端能同时满足"带参方法 + 跨函数状态 + 数组读写"。**
与其交一个"编译全绿、跑起来不对"的游戏（这一轮已经栽过三次），不如把判据和根因留清楚：
报告第 7 节列了按性价比排序的下一步，其中**任意修好一处**，贪吃蛇以及后面的赛车/飞机空战/象棋
就都是纯逻辑活了。


## v0.96.184 (2026-09-16) — examples 按语言分目录（手机上也分）

一句话：`examples/` 从「十来种语言堆在一个目录里、只能靠文件名猜语言」改成
**`examples/<语言>/<文件>`**；仓库里那几处没按语言归位的也一并归位。

### 一、手机上原来是平铺的

包里的结构本来就是 `Examples/<语言>/<文件>`，**是解压那一步把它拍平了**
（`Path.GetFileName(e.FullName)`），于是手机上 `examples/` 底下是 22 个混在一起的
文件：`tetris.c` / `tetris.py` / `tetris.bas` / `file_io.cs|java|js|m|r|rb|swift` /
`parserexp_demo.fs` … 看目录看不出哪门是哪门。

### 二、改法

- `MauiBootstrap.EnsureExamples()` **保持包里的层形**解包（`examples/<语言>/<文件>`），
  写盘前按需建子目录。
- **清空必须精确**（这条比解压本身更要紧）：只清「包里有的那些」——
  ① 顶层散文件（v0.96.184 之前平铺的就是它们，不清就成了同一份示例两份：`tetris.c` + `c/tetris.c`）；
  ② 我们管理的语言子目录（内容要与当前版本一致、不能累加）。
  **用户自己在 `examples/` 下建的目录不碰** —— 那是他的文件。
- **旧手机怎么迁移**：不用写迁移代码。标记文件 `.unpacked` 里存的是**版本号**，
  版本一变就会重解压一次 —— 这正是那个标记当初设计成存版本号而不是"文件在不在"的理由。

### 三、路径变了（一并改掉的地方）

`vml run examples/tetris.c` → **`vml run examples/c/tetris.c`**。
跟着改的：`Examples/README.md` 的开头与「再加一个游戏」那节、`CLAUDE.md` 的两处命令、
`MauiBootstrap` 与 `SandboxFsService` 里举例用的注释路径。

### 四、仓库端也归位了

- `benchmark/fortran/main.f90` → **`fortran/bench.f90`**、`benchmark/javascript/main.js` →
  **`javascript/bench.js`**（同一个基准程序的两种语言版本）。**同时改了名**：
  `benchmark` 那个按用途分的目录没了，名字得把"这是个基准"带上，否则 `fortran/main.f90`
  和 `fortran/math/main.f90` 摆在一起看不出区别。
- 根目录那个 `c.bat`（清 `*.dat`/`*.vml` 的小工具，没人引用）→ **`c/c.bat`**，
  和它服务的语言放在一起。
- **保留不动的**：`mix/stm32f4_blink`（本就是**混合**语言工程：C + 汇编 + C# 脚本 + 整套
  STM32 工程文件，归不进任何单一语言）、以及 `Curl/` `OpenCV/` `Zlib/` `SharedLib/`
  （**库**的调用示例，不是语言）。它们是"各自目录"，只是分的维度不是语言。


## v0.96.183 (2026-09-16) — 「支持不了的语法」改成硬失败（补丁 0008）

一句话：上一版记在案的那条"复合字面量应当报错"落地了 —— 而且不是"多打一行日志"，是**终止编译**。

### 一、上一版写的是"应当报错"，可前端本来会把它吞掉

前端有一条**顶层解析异常恢复**：`catch (System.Exception)` → 打一行 `[RECOVER] …` →
跳到下一个声明**继续编**，退出码还是 0。这条容错对"笔误/残留 token"这类输入是有用的，
但它把"本编译器不支持…"一起吞了 ⇒ 用户拿到的仍是一个**能跑、却少了一段**的程序。
**所以光加一句 `Error(...)` 不够，得让这个错误码"不可恢复"。**

### 二、改法（两条，都在 `patches/0008-c-compound-literal-hard-fail.patch`）

① 解析到 `(类型[]){…}` 就 `Error(...)`：带行列号、源行与插入符，并**直接告诉用户怎么改**
   （改用具名局部数组再传）。
② 顶层那个 catch 前面加一条 `when (ex.Code == ErrorCode.Parser_UnexpectedToken)` + `throw;`
   —— 把该错误码约定为**不可恢复**。这个码在本仓库**只有这一处用**（改前 grep 确认过），
   所以不会误伤原有的容错路径；注释里把这个约定写明了，免得以后有人拿它做可恢复的错误。

### 三、为什么是报错，而不是把复合字面量实现出来

让它成立得在**表达式中间**开一个帧内临时槽，而"临时槽与局部变量撞车"正是 0002 修过的
那类事故（asm 结果的暂存槽撞上第一个局部变量）。做对需要一遍真正的临时分配 ——
不该在这个位置随手加一个。**报错让用户立刻知道怎么绕，比默默编坏便宜得多。**

### 四、配套：硬失败的异常必须变成"用户看得见的一句话"

前端一旦硬失败，异常会穿到 `MauiVml`；而那里 `compile.Result` 只接了
`OperationCanceledException` ⇒ 异常落在 `Task.Run` 里就是**未观察的任务异常**，
手机上表现为"点了没反应"（本仓库踩过一次同型，见 CLAUDE.md 移动端十三期 ⑨b）。
现在 `BuildProgram` 补了一条通用 catch，转成 `⚠️ 编译失败：<带行列号的原文>`。

### 五、补丁账

0008 与 0003 **同文件、不同 hunk** —— 按 hunk 拆的。0003 的语义是"数组维度常量折叠"，
这次是"不支持的语法要硬失败"，**两件事**；混在一起会让上游把两件事当一件 review。
拆完 `scripts/check-vml-patches.sh` **全绿**（八条补丁依序打上 + 八个 synced 目录逐字节一致）。


## v0.96.182 (2026-09-16) — 真机图元体检抓出三层缺陷：修好宿主那层，画面纹丝不动

一句话：v0.96.180 的矢量后端在**构建全绿、桌面自测 5841 项全绿、Windows 桌面也验过**的情况下，
装上手机逐格体检（rect / roundrect / circle / ellipse / line / polygon / polyline / path 挖洞 /
curve / text / grad 线性 / grad 径向）才发现**两项画不出来**：`polygon`、`polyline` 整格空白，
线性渐变渲染成**纯红**。而这三处缺陷**分别落在三个不同的层上** —— 宿主处理器、C 前端代码生成、
平台坐标口径 —— 修好第一层之后画面**一点没变**，这一条比缺陷本身更值得记。

### 一、为什么自测和桌面验证都放过了它们

自测验的是「DSL 文档 → 落笔指令」的**映射**（记录型落笔面），桌面验的是同一条光栅路径；
这两处却错在更外侧：

- **参数错位在宿主处理器里**（`VmlUiCalls`，MAUI 侧，桌面构建根本编不到它）——
  记录型落笔面看到的是一次**完全正常的填充调用**，必然通过；
- **渐变错在平台坐标口径**：几何、`@id` 引用、解析全对，只有"交给平台的数"是错的；
- **多边形那个还多了第三层**：坏在**前端代码生成**（复合字面量的地址没算出来），
  于是"宿主读对了参数"照样画不出东西 —— 这一层连真机日志都不报错，只能把汇编打出来看。

⇒ **「每类图元在真机上看一眼」这一步不能省。** 构建通过 + 自测通过 + 桌面通过，三件事加起来
仍然证明不了「手机上画出来是对的」。而这次还多一条：**改对一处不等于修好** ——
宿主参数改对之后画面纹丝不动，正是那句"应当继续怀疑，别把没复现当成已修复"。

### 二、`polygon` / `polyline` 空白：两个原因**叠在一起**，修完头一个还是空的

**因一：宿主把参数按"看起来像"读，没按 C 头文件读。** C 侧签名是

    ui_polygon (int* pts, int count, int fill色, int stroke色, int width, char* grad)
    ui_polyline(int* pts, int count, int stroke色, int width, char* grad)

而宿主处理器把 `r[2]` 当成"填充开关"、`r[3]` 当成颜色（大概是照
`ui_rect(x,y,w,h,色,填充,宽度,圆角)` 的形状类推的）。后果：

- `ui_polygon(pts, 3, 绿, 0, 0, "")` ⇒ 颜色取到 `r[3] = 0`（全透明）而"填充"为真 ⇒ **什么都不画**；
- `ui_polyline(pts, 4, 黄, 3, "")` ⇒ 把**线宽 3** 当成了颜色，又把 **`grad` 指针**当成了线宽。

**因二（真正的拦路虎）：程序用了 C99 复合字面量 `(int[]){92,215,…}`，本前端不支持、而且不报错。**
它的地址**根本没被算出来**，编出来的代码把上一个寄存器的值（正好是"点数 3"）当成指针推下去 ⇒
宿主去地址 3 读坐标、越界就地停 ⇒ 依旧是空白。桌面上把汇编打出来一看就明白：

    具名数组：  move R0 R12 / sub R0 #28 / push R0      ← 算出数组地址 ✓
    复合字面量：move R0 #3    /                push R0   ← 把「点数」又推了一遍 ✗

根因在前端：`Parser.Expressions.cs` **认得**这个语法（`if (Current().Type == LBRACE) return ParseInitializerList();`
注释就写着 "compound literal"），但 `ArrayInitializer` 的代码生成**只挂在变量声明的初始化上**
（`varDecl.Initializer is ArrayInitializer`），表达式位置没有能产出地址的实现。
⇒ 同一句 `ui_polygon(...)`，**换个写法就从"画不出"变成"画得对"**，而编译器一言不发。

修法：宿主改为按头文件读（并覆盖"只填不描"这个最常见写法：颜色取"给了的那个"）；
示例一律改用**具名局部数组**。⚠ 两条**已知限制**记在案：
`ui_polygon` 同时给填充色与描边色时**只填不描**（场景接口 `AddPolygon` 只有一个颜色位，DSL 支持两个）；
复合字面量将来应当**报错**而不是静默编坏（"响亮地停"），那是上游前端的活。

### 三、渐变：我把"相对谁归一化"这个前提搞错了，绕了两圈才回来

三个版本，前两个都是错的，而**错法不同**：

1. **绝对场景坐标给平台** —— `g.X1 * SceneWidth` 这种。平台只认 `0..1`，一百多的数被当成
   "远在形状之外" ⇒ 整块落在 `t≈0` ⇒ **渲染成纯红**（"渐变没生效"的典型样子）。
2. **"修"成：场景归一化 → 绝对 → 再按包围盒归一化** —— 数对了、渐变也真出来了，但是**一段切片**
   （实测紫→蓝）：场景级的渐变铺到小方块上，"这块的左红右蓝"变成了"整个场景渐变的一小段"。
   **换算的方向是对的，前提（相对谁归一化）是错的** —— 而这版我一度当成正解写进了日志。
3. **正解：原样透传。** 我方 `Gradient` 的坐标本来就是**相对「这张形状自己的包围盒」**的 `0..1`，
   而 MAUI 的 `LinearGradientPaint`/`RadialGradientPaint` 要的**正好也是**「相对刷子矩形的 `0..1`」
   （文档：*relative coordinates from (0,0) to (1,1)*；径向默认 center `(0.5,0.5)`/radius `0.5`）
   ⇒ 两者同源，一个数都不用改。

**"相对谁归一化"有三处现成真源，答案一直摆在那儿**：光栅侧 `FillTransformed` 的
`nx = (lx - minX) / spanX`（局部包围盒）、SVG 导出用 `objectBoundingBox`（`EmitGradient` 注释逐字如此）、
**以及自测里一条早就钉住这条语义的用例**（`SelfTest.Chunk22`：矩形左端偏红、右端偏蓝）。
教训：**先问"这个数相对谁归一化"，再动手算** —— 光栅侧一行除法就写着答案，我先按想象算了两轮。
（`SelfTest.Chunk23` 里那句"由落笔面按场景尺寸换算"的注释就是这条错误前提，已改正。）

### 四、留下的判据：`Examples/c/draw_prims.c`（图元体检）+ 一份图纸两条后端对着看

体检程序 = 12 个格子、每格判据写在注释里、**肉眼可判**（多边形必须是实心三角、折线必须开口、
挖洞那个**中间必须是空的**、曲线必须是弧线不是直线、线性渐变**左红右蓝**），手机上
`vml run examples/draw_prims.c` 就能复跑。已经进了 `Examples/`（不在 `sync.sh` 的覆盖范围内，
放这儿不会被上游同步冲掉）：这类"整条链路都对、只有平台上画出来不对"的缺陷，
**只有肉眼或逐像素比对能抓住**，所以这份可复跑的判据比那两行修复本身更值钱。

**另一件更管用的工具：同一份图纸分别喂给两条后端。** 桌面的最小宿主加了
`vmlhost dsl <图纸.dsl> <出图.png>`（把一份 DSL 直接走**光栅**出图），手机上是**矢量**，
两边并排一看就知道"是不是同一个形状" —— 渐变那两轮弯路**只有这么比才看得出来**：
光栅版是"左红右蓝铺满整块"，我第一版是纯红、第二版是紫→蓝的一小段，都"看着挺像渐变"。
**"看着对"和"跟另一条实现一致"是两件事**，有这个对照就不用靠想象。


体检程序 = 12 个格子、每格判据写在注释里、**肉眼可判**（多边形必须是实心三角、折线必须开口、
挖洞那个**中间必须是空的**、曲线必须是弧线不是直线、线性渐变**左红右蓝**），手机上
`vml run examples/draw_prims.c` 就能复跑。已经进了 `Examples/`（不在 `sync.sh` 的覆盖范围内，
放这儿不会被上游同步冲掉）：这类"整条链路都对、只有平台上画出来不对"的缺陷，
**只有肉眼或逐像素比对能抓住**，所以这份可复跑的判据比那两行修复本身更值钱。

体检范围限于 **C 包装真能画到的那些入口**：星形 / 扇形 / 心形 / 环**没有 C 包装**
（DSL 里有、VML 程序够不到）⇒ 它们的矢量实现只由桌面的映射自测覆盖，真机验不到，记在案。

## v0.96.181 (2026-09-16) — 补丁机制修好：加兜底补丁 + 常驻复核脚本（此前一次同步会丢 3000+ 行）

一句话：`sync.sh` 的 rsync 列表**包含 `Lib`**（还带 `--delete`），而 `patches/` 只覆盖了
`VMLRuntime`/`VMLPrepares`/`VMLAssembler` 里"我们记得的那几处" ⇒ **跑一次同步，
`Lib/` 与几个前端的改动就会没**。这一版把补丁机制本身修对，并留下一条能跑的判据。

### 一、先做错了一版，又被自己的复核纠正

第一版 0007 只取了三个提交里对 `Lib/` 的改动（我以为那就是全部）。做了**反向检查**
（`git apply --check --reverse`）之后才发现：把 0007 卸下来，`Lib/` 与 vendor 状态**还差 3023 行**
—— 共享 UI 调用库那一整块（`vmlui.vml` 2248 行 + `Lib/shared/src/vmlui.c` 345 行）根本没进去。
同一轮审计还查出 `VMLPrepares/` 下 9 个文件（BasicCompiler ×7、CompilerBase/Preprocessor、
PythonCompiler ×1，即 v0.96.164/165/169 打通"其它语言调用 C 共享库"那批修复）也没有补丁。

**教训：补丁的完整性不能靠"我记得改过哪些"来枚举 —— 那必然漏。** 改成机械求差：
`git diff <vendor>..HEAD -- <rsync 覆盖的八个路径>`，再按内容（不是文件名）算覆盖。

### 二、修好之后

- **0007 换成兜底补丁** `0007-rest-local-adaptations.patch`（13 文件 / 3636 行）：
  不做人工挑选，直接取 vendor 基准的机械差。
- **0001 重新生成**：它的上下文已经漂了 —— 实测严格 `git apply` **打不上**（GNU patch 也要 fuzz 2）。
  而 `sync.sh` 用的正是严格 `git apply` ⇒ **真同步时它会失败并中断**。现按 vendor 基准确认。
- **0002–0004 也重新生成**：复核报出 `CCompiler/CodeGenerator.Functions.cs` 与「vendor+补丁」不一致
  —— 0002 虽然提到这个文件，却只包了"那次修复"的几处，而该文件自 vendor 以来还有别的改动；
  0007 又因为"文件已被覆盖"把它排除了 ⇒ **两头都没兜住**。**判据按文件名算是不够的。**
- 补丁 0001 原本**没有说明文字**（其余几条都有），一并补上。

### 三、常驻判据：`scripts/check-vml-patches.sh`

把补丁依次打到 **vendor 那次提交**的树上（临时工作树，不碰主工作区），
再与当前工作区**逐目录逐字节**比对（跳过 csproj —— 那类由 `sync.sh` 的 python 步骤机械施加）。
本仓库有那次 vendor 提交、此后没再跑过同步，所以**不需要上游副本**就能验这条判据。

现状：**7 条全部严格可打，八个目录全部一致** ✅

### 四、仍然欠着的

补丁只是"上游还没有这些改动时，本地同步不会把它们冲掉"。**真正的解法是把它们提给上游
VML 仓库**（新 syscall 的语义、汇编器语法、前端修复本来就该在那边定义）——
上游收了之后对应补丁会自动走 `sync.sh` 的"已在（跳过）"分支。欠账清单：
`0005`、`0006`、`0007`（外加 0001–0004 里属于上游该修的那部分）。

## v0.96.180 (2026-09-16) — 绘图「直接写屏」落地：每帧 80ms → 1ms，帧率 26 → 92~107fps

用户问：「绘图做成直接写屏是不是更快？」并定了做法：**先做安卓，后面需要支持所有平台**。

### 一、为什么值得做（先把账算清楚，别凭感觉）

真机实测（小米 13 / Android 16，同一支 60 矩形探针）原来每帧：

```
后台：解析 0.8 + 光栅化 25~30 + PNG 编码   = 26ms
UI  ：PNG 解码 52~56 + 贴图 0.2            = 54ms
每帧新建一张 333KB 位图 ⇒ 25fps ≈ 10MB/s 垃圾，10 秒里 GC 跑了 355 次
```

**PNG 那一段是纯粹的浪费**：图刚编出来就立刻解回去上屏。帧率当时并没有被它卡住
（25.6fps 顶在 40ms 节拍上），所以收益不在帧率，而在 **CPU / 耗电 / 发热**，以及那 355 次
GC 停顿 —— 「抖动」里属于平台的那一半正是它。

### 二、做法：加第三条画法，而不是"另起一套"

`DrawCommand` 本来就是「一个指令类、两个画法」（`Rasterize` 光栅 / `EmitSvg` 矢量导出），
这次加第三个 **`Vector`**，并且是**必需成员而不是默认空实现** —— 漏一个指令就会静默少画东西，
那种错只有上手机才看得出；**编不过反而是最省事的提醒**。

- `IVectorTarget`（落笔面）+ `MauiVectorTarget`（MAUI `ICanvas` 实现）；
- 16 个指令的矢量画法在 `DrawCommands.Vector.cs`，**几何全部取自同一个 `DrawGeo`**
  （星形/正多边形/椭圆/环/圆角矩形/扇形/心形）—— 与 SVG 那条路同源，一行都没重写；
- 变换**先落到点上**（与光栅侧同一个 `Canvas.TransformPoints`）⇒ 平台画布不再做变换，
  两条后端的坐标语义天然一致；
- 绘窗矢量模式下每帧只做「拼 DSL + 解析」→ 挂文档 → 重绘：**光栅化、PNG 编码、PNG 解码、
  贴图那一整段全没了**。

**为什么不做成"安卓专用"**：落笔面走 MAUI 的 `ICanvas`，**一套代码两端跑** ——
按用户要求先在安卓上验证，iOS/桌面不用重写、开开关即可（`DrawWindowPage.UseVectorBackend`）。
反面做法"保留光栅化、只把像素缓冲交给平台位图"要给 Android/iOS 各写一份
（`Bitmap.CreateFromPixels` / `CGBitmapContext`），正是本仓最忌讳的「同一规则两处实现」。

**安全网**：矢量画不了的图元（目前是"带旋转/错切的贴图"）会 `MarkUnsupported` ⇒
**整个窗口回退光栅后端** —— 宁可慢，也别默默少画东西。

### 三、真机对照（同一支探针、同一条件）

| | 光栅后端 | 矢量后端 |
|---|---|---|
| 后台（解析+光栅+PNG） | 26ms | **0.8ms** |
| UI（解码+贴图） | 54ms | **0.0ms** |
| 实际帧率 | 25.6 fps | **92~107 fps** |
| GC | 355 次 / 10 秒 | 295 次 / **40 秒** |

俄罗斯方块照常跑（图元 88~112、每帧 0.9ms），**整屏截图逐项核对无误**：棋盘圆角、格线、
方块高光、右侧「下一个」、分数/最高/消行/等级（含中文与配色）都与光栅版一致，
抗锯齿由平台做、比原来的 3× 超采样更好。

**跨端验证到哪一步（用户定的"先做安卓、后面支持所有平台"）**：

| 平台 | 构建 | 运行/观感 |
|---|---|---|
| Android | ✅ | ✅ 真机整屏核对（上表数据即出自此） |
| Windows | ✅ 零警告 | ✅ **跑起来核对过**：俄罗斯方块棋盘/格线/方块高光/右侧面板与中文文字全部正确（桌面无 adb，用 UI Automation 驱动：选页签 → `ValuePattern` 填命令 → `Invoke` 运行 → 截图） |
| iOS / MacCatalyst | ✅ 我加的文件零错误（该目标下唯一报错是既有的 `VmlAudio.cs` 缺 `AVAudioPCMBuffer`，与本次无关） | ⏳ **未验证**（需 macOS）——开关 `DrawWindowPage.UseVectorBackend` 可一键回退光栅 |

也就是说：**矢量后端默认在所有平台开启**，代码本来就是一套（MAUI `ICanvas`）；
iOS 只差在 Mac 上看一眼观感，万一有问题把开关关掉即可回到已验证的光栅路径。

### 四、自测 15 条（桌面上就能验的那一层）

矢量后端最终落在平台画布上，桌面没这东西 —— 所以用**记录型落笔面**（只记不画）断言
「文档 → 落笔」的映射：矩形 4 点/颜色原样、星形 2n 点、变换落在点上、透明填充跳过、
渐变原样下传、多行文字行距 1.3、`path` 多子路径奇偶挖洞、贴图无文件 ⇒ `MarkUnsupported`，
外加**表驱动**一条：16 个内置指令**每个都要能画出东西**（新增指令忘写矢量画法时会红）。
全量：**5840 通过 / 0 失败**。

### 五、一处失误（记下来）

第一次上真机时日志显示**还在走光栅** —— 查下来是我用脚本改构造函数那段接线时
**CRLF 没匹配上、替换静默失败**，而我只看了自己打印的 "ok" 就当成落地了
（`UseVector` 默认 false ⇒ 全部走老路）。已改用 Edit 工具补上并 `grep` 回读确认。
**脚本改完文件要回读确认，别信自己的打印** —— 与"补丁基准取错提交"是同一类。

### 六、遗留

新瓶颈是**每帧的 DSL 字符串 + 解析**（0.8ms + 相应分配，GC 295 次/40 秒主要来自它）。
再往下要省就得让矢量后端直接吃场景图元、跳过"拼文本再解析"这一趟 —— 但那条路会动到
SVG/光栅共用的中间表示，留到有明确需求时再说。

## v0.96.179 (2026-09-16) — 绘窗「半成品帧」的真正根因：快照拍晚了；外加真机分段计时

承接 v0.96.178（用户报「俄罗斯方块有时抖动闪烁」）。上一版把出图判据从 `Version` 改成
`PresentVersion`，把**出图次数**从"每个图元一次"降到"每帧一次"——但**没消除竞态**。

### 一、真正的根因：`ui_present()` 只是插了个旗，快照没人拍

`ui_present()` 以前只把标记置上，真正 `BuildDsl()` 拍快照发生在**下一次定时器醒来时**
（最多 40ms 后）。而那 40ms 里 VM 早就开始画下一帧了——**先 `ui_clear()` 把棋盘抹干净、
再一格一格重画**。于是拍到的仍是半成品，贴上去还是闪。

现在 `VmlScene.Present()` **当刻就把这一帧定下来**（`PresentedDsl`），渲染只负责取走；
那一刻的场景正是程序刚画完的，之后隔多久渲染都不影响。没调 present 的老程序仍走
"这一拍静下来了"那条路（那条路上程序本来就没在画）。

**验证判据换成了日志里的图元数**：修好之后探针每帧恒为 **68**（60 矩形 + 7 刻度 + 1 条 = 完整一帧），
而不是修之前的 5/17/28/29/34/37/47/51/59 那样参差。这条比截图数像素干净得多。

### 二、真机分段计时（新增，`adb logcat -s WCVML`）

绘窗每 30 帧打一行，把一帧拆成**两半**（此前桌面基准只覆盖后台那半）：

```
每帧：DSL 0.0 / 解析 0.8 / 光栅+PNG 25–30 = 后台 26ms｜解码 52–56 / 贴图 0.2 = UI 54ms
      ｜图元 68｜PNG 1KB｜合计 80ms｜**实际 25.6 fps**（30 帧 / 1172ms）
```

两条结论都和我先前的判断相反，一并更正：

1. **帧率不是被绘图卡的**。实际 25.5–26.3 fps 极稳，正好是 40ms 检查节拍的倒数 ⇒
   管线跟得上，**要更高帧率先动节拍，不是动绘图方式**。
2. **UI 侧那 54ms 是最大的单项开销，而且来源不是"解码慢"**：10 秒里观察到 **355 次 GC**
   （多的时候 140ms 内 4 次）、Java 堆 19MB→30MB。每帧新建一张位图 + PNG 字节 ⇒
   25fps × 333KB ≈ **10MB/s 垃圾**，GC 连轴转 —— 这才是"抖动"里属于平台的那一半。
   **PNG 编解码这一段是纯粹的浪费**：图刚编出来就立刻解回去上屏。

### 三、两次"量错了"的更正（都在本文里留痕）

- **探针写错**：第一版帧率探针用 `ui_wait(msg, 0)` 想连续出帧，量出"1 fps" —— 而
  `ui_wait` 的 `timeout=0` 是「**无限等**」不是「不阻塞」（宿主 `Take(0) → Wait(Infinite)`），
  它每轮都在等下一秒的定时器。改用 `ui_poll` 才有意义。该语义已写进 `docs/VML宿主接口.md`
  （含"事件驱动用 `ui_wait`、连续动画用 `ui_poll`"两种主循环形状）。
- **仪表读错时机**：图元数原先在 UI 回调里读 `scene.FigureCount`，那时后台已经光栅完一整趟、
  VM 又画过好几轮 —— 读到的是"此刻"而不是"这一帧"，害我一度以为快照没修好。
  改成**从这一帧的 DSL 里数**（数换行减 2 行表头）。

**教训**：`实际 fps` 与 `单帧耗时` 必须同时报 —— 单帧 10ms 也可能因为节拍只出 2 帧/秒；
反过来"某个分段很慢"也可能是被污染的量（读的时机不对、或把 GC 记在了自己头上）。

## v0.96.178 (2026-09-16) — 俄罗斯方块「有时抖动闪烁」：出图判据用错了标记；顺带修两处数组往返

用户原话：「俄罗斯方块有时抖动闪烁」。真机复现环境：小米 13 / Android 16 / v0.96.177。

### 一、闪烁的机制：把「内容变了」当成了「一帧画完了」

窗口出图的判据是 `VmlScene.Version`，而那个数**每个图元都 +1** —— 一帧要画上百个图元，
于是"变了"上百次。40ms 的定时器撞上哪一次，就把**当时那一刻**的场景贴上去，而那一刻
多半是**画到一半**的：`ui_clear()` 刚把棋盘清干净、棋子还没画出来。真机上光栅化 + PNG 编码
要上百毫秒 ⇒ 空棋盘在屏上停留到肉眼可见 —— 就是「有时闪烁」的「有时」（取决于重画跨度
与节拍相位）。

**程序其实一直在说"这帧画完了"**：`waycoder_ui.h` 的用法示例每帧末尾都调 `ui_present()`，
`socket 531` 号也一直在协议里（`DrawPresent`），两个游戏也都调了 —— **只是宿主收到之后
把它当成了又一次"内容变化"**（`case DrawPresent: TouchScene()`），渲染侧完全没用它。

修法：场景加 **`PresentVersion`（只由 `ui_present()` 递增）**，窗口按它出图：

- 调过 `ui_present()` 的程序 ⇒ 每 present 一次出图一次，**贴上去的必然是完整帧**；
- 没调过的老程序 ⇒ 退到「**这一拍内容没再变**」（画完再说，晚一拍），而不是"一变就出图"；
- 首帧（`Attach` 里那次同步渲染）不受此限：那时程序可能一个图元都还没画，等一拍就"一闪而过"。

**「内容变了」与「一帧画完了」是两件事**，拿前者当后者用就会贴出半成品。

### 二、抖动：画布每帧被重新拟合了一次尺寸

`ShowFrame` 每帧都调 `FitCanvas`，而它按视口比例算出的尺寸**带小数**；视口测量只要有一点
浮动（ScrollView 内容变化、滚动条出现/消失），算出来就跟着变 ⇒ 画布**每帧微调尺寸**，
整块棋盘跟着缩放/位移。连拍实测抓到过两张比满格小 1% 的画面（`73876 → 73166` 像素），
就是它。修法：尺寸**只在还没有尺寸时兜底设一次**（首帧在 `Attach` 里同步出图，那时页面
还没布局），之后一律交给 `OnSizeAllocated` —— 那里才是"视口真的变了"的判据（带 0.5dp 容差）。

### 三、验证：确定性探针 + 真机连拍

桌面基准量不出这类问题（它静态地量一帧的耗时，看不见"贴的是哪一帧"），所以写了个探针程序
（`.scratch/probe_frame.c`，也推到手机工作区跑了）：**一帧分两步画** —— 先 `ui_clear()` 铺底色、
再画 100 个方块，两帧配色刻意不同（蓝底绿块 / 红底黄块）。于是连拍里只有三种可能：
完整 A 帧、完整 B 帧、**半成品帧**（底色换了、方块不足）。

- 真机连拍 **45 张全部是完整帧**，零半成品（探针每 300ms 换一帧、连拍覆盖约 50 次换帧；
  修复前那种"上百毫秒的半成品"本该被大量抓到）；
- CPU（同一条命令、同一起始状态，局的进行中各采 10 次 × 2s）：**26.6% → 22.1%**
  （少掉的正是那些贴不出去的部分帧的光栅化 + 编码）；
- 探针的两档像素数（`73876 / 73876`）在第二节的改动后不再出现 1% 的漂移。

### 四、顺带：两处数组往返的缺陷（code-review 查出 + 我自查）

1. **`object[]` 等值数组被误压成一行**（v0.96.177 引入的回归）：`.word[N]` 的默认值只收整数，
   而压行的判据只看"值相同"⇒ `object[]{"ok","ok","ok"}` 往返之后**只剩一个字符串**。
   前端确实会产出等值字符串数组（`GenerateStaticArrayInit` 与全局初始化器都会）。
   **只有全等才会中招**（有一个不同就退回逐元素写），所以拿 `int` 数组去测永远测不出来 ——
   现已连类型一起判（只压 4 字节整数）。
2. **`.int[N]` / `.long[N]` 别名够不到**：调度的判据是一串 `Contains(".word")`，
   别名的正则写在 `TryParseCompactData` 里但**根本没机会被调用**（等于死代码）。
   现已在调度处补 `.int[` / `.long[`；且**不能用 `StartsWith`** —— 带标签的 `p: .int[3] 7`
   开头是标签，实测就栽在这上面（报"未知指令：.INT[3]"）。

## v0.96.177 (2026-09-16) — VML 批量数据 `.word[N]`；顺手挖出「数组初值静默错位」的汇编器缺陷

用户原话：「VML汇编和运行时需要支持批量内存分配，类型[数量]:默认值，这样的语法，不然
int a[1024]，编译成 word 1000行代码，很长一堆。」

### 一、`int a[1024]` 从 1071 行变成 1 行

问题在**"落成文本"这一步**：数据在内存里本来就是压缩的（编译器放进去的是 `int[1024]`），
只有序列化成 `.vml` 时才被摊平成 1024 行 `.word 0`。加一条批量写法，两侧一起改：

- **语法**（`VmlAssembler.TryParseCompactData`）：`buf: .word[1024] 0`，默认值可省（= 0），
  冒号写法 `.word[1024]:0` 也收（用户说的「类型[数量]:默认值」形态）；别名 `.int[N]`/`.long[N]`/
  `.dword[N]` 等价。判据很紧：**整行就是这一条**（前面可带 `label:`），认不出就交回原来的
  逐元素路径 —— 这条逻辑**不能猜**，猜错会把普通 `.word 5` 解析成别的东西。数量钳在 1<<20
  （`[999999999]` 会在一行里直接分配 4GB）。
- **序列化**（`VmlProgram.ToString`）：只在**元素全相同**时才压成一行 —— 不等值的仍逐元素写，
  那才是真的每个都不一样。

实测：`int a[1024]; int b[3]; int c[3]={7,8,9};` 改前 1071 行 → **改后 47 行**
（`a: .word[1024] 0` 一行）。**只做了 4 字节字**：`.byte[N]`/`.half[N]` 没做 —— VMB 数据段对
数组是"每个元素一个带 tag 的槽"，直接塞 `byte[]` 会被写成 4 字节（静默错），要做得再动一处编码。

### 二、验证时撞见的真问题：一行 `.word` 被**处理了两遍**

按本仓的验收标准（"直接编译运行" vs "存成 .vml 再独立汇编运行" 逐字节相同，A==C）验证批量
语法时，A==C 是**通过**的，但探针程序里 `c[2]` 读出来是 8 而不是 9。**A==C 只证一致、不证正确**，
于是往下查，查出一个与本次改动无关的既有缺陷：

    int c[3] = {7, 8, 9};      /* 数据段实际写出 6 个字：7, 7, 8, 8, 9, 9 */

**每个元素都翻倍**（`c[0]==7` ✓，`c[1]` 读到 7 ✗，`c[2]` 读到 8 ✗）。最小复现与 C 前端无关：

    .data
    p1:
        .word 7
        .word 8
        .word 9

`Assemble` 之后 `DataSection["p1"]` 是 `int[6] {7,7,8,8,9,9}`。

**根因**：`VmlAssembler.ParseData` 里是一条 `if / else if` 链（`.word` → `.string` → … → `.const`），
而 `.data` 那一条**是新起的 `if` 而不是 `else if`** —— **链就断在这里**，后面 `.dword`/`.byte`/
`.word` 那几条 `else if` 于是挂到了 `.data` 分支上。于是一行 `.word 7` 先被 `.word` 专用分支加
一次，又被 `HandleDataDirective` 加一次。**修法只有一个词**（`if` → `else if`）。

**为什么一直没被发现**：零初值数组（`int board[15][15];`）翻倍只是白占一倍内存，下标仍按各自
基址寻址 ⇒ 棋盘类程序看着完全正常；有初值的才露馅，而"每个元素翻倍"很容易被当成"C 前端数组
初始化没实现"。**这次就先走错了方向**：去 C 前端找了一轮（AST 里 `ArraySize=3`、3 个元素，
全是对的），回到汇编器才看见。定位方法是**把同一段文本直接喂给 `Assemble` 单测** ——
前端根本不在这条路上，把链路分层之后就一眼可辨。

**这次还差点被自己的调试骗了**：往汇编器里插的打印有一半"没出现"，据此推出的"这条分支没跑"
结论全是假的 —— 真因是探针工程把 `VMLAssembler.dll` **拷进了自己的 bin**，`--no-build` 跑的
是旧副本。**改完 `third_party` 里的库，探针工程一起重编**（这跟"构建产物被旧副本/运行实例锁住"
是同一类坑，本仓已踩过多次）。

### 三、上游补丁与兼容性

两处改动都在 `third_party/vml`（rsync 覆盖区），按本仓规矩做成补丁，`sync.sh` 的【B】段加了
⑤ ⑥ 两条：

- `patches/0005-assembler-compact-word-array.patch` —— 批量数据（**新语法**）。
  ⚠ **旧版汇编器读不懂 `.word[N]`**（会把 `[1024] 0` 当值解析、静默变 0）⇒ 解析侧与序列化侧
  必须**同步升级**，回灌上游时发版说明要写清楚。**这两处要提给上游 VML 仓库。**
- `patches/0006-assembler-word-handled-twice.patch` —— 上面那条缺陷修复（**纯 bug**，上游本该收）。

拆成两个补丁而不是一个：一个是新能力（有兼容性风险，要同步升级），一个是纯修复，混在一起
review 时分不清哪条是风险。**验证方式是"把两个补丁依次打到 HEAD 上，结果与工作区逐字节相同"**。

### 四、验收

- 最小复现：三行 `.data` 喂 `Assemble`，`p1` 由 `int[6]{7,7,8,8,9,9}` 变为 `int[3]{7,8,9}`；
- 端到端：`int a[1024]; int b[3]; int c[3]={7,8,9};` 逐项判定（**判定绕过 stdio**，
  每条结论发一个频率），7 项全对，且 A==C 逐字节相同；
- 回归：`Examples/c/tetris.c`（含脚本陪玩整局）与 `Examples/c/gomoku.c` 的 A==C 仍逐字节相同。

## v0.96.176 (2026-09-16) — 绘图接口完善（渐变刷子/路径/曲线）+ 绘图性能基准与优化（2.1×）

用户原话：「完善手机的绘图接口，比如加入渐变刷子，加入路径功能，加入曲线功能，等完善绘图」，
随后追加：「写个 benchmark，专门测试绘图性能，优化绘图性能」。

### 一、先把"本来就有的能力"接出来，而不是另造一套

动手前先把这条链摸了一遍，结论是**大半能力在绘图 DSL 里早就有了**（桌面 `draw` 工具一直在用），
**缺的只是 VML 侧的入口**：`gradient` 定义与 `@id` 引用、`path`（SVG 语法）、`polygon`/`polyline`、
`star`/`pie`/`ring`、`translate`/`rotate`/`scale`/`push`/`pop`、线帽与虚线、抗锯齿。
所以这一版**没有新造图元**，只加了 6 个 syscall 把它们接出来（534–539，号段里唯一空着的一段）：
`GRADIENT` / `DRAW_PATH` / `DRAW_POLYGON` / `DRAW_POLYLINE` / `DRAW_RECT_GRAD` / `DRAW_CIRCLE_GRAD`。

### 二、真正的缺口：曲线被静默丢掉

`path` 这条指令一直在，但光栅化那侧只认 `M`/`L`/`Z`（原注释：「手搓光栅化器不支持任意 SVG
path 曲线，退化为解析 M/L 直线段」），而 `EmitSvg` 把整条 `d` 原样交给矢量后端 ——
**同一份 DSL 导出 PNG 与导出 SVG 图形不一样**，属于"只在导出位图时才发现"的坑。

新增 `Infra/DrawPath.cs`（纯数学、无 IO ⇒ 可自测、四端共用）：完整解析
`M/m L/l H/h V/v C/c S/s Q/q T/t A/a Z/z`，曲线按**控制多边形长度**自适应分段，
圆弧走 SVG 规范 F.6.5 的端点参数化→中心参数化换算（含"半径不够大时按规范放大"）。
展平之后填充与描边直接复用现成的 `FillTransformed` / `StrokePolyline`（自带变换与渐变采样），
不必给光栅器再加一套曲线求值。`path` 同时支持了 `fill <色|@渐变>` 与**奇偶规则挖洞**。

### 三、绘图性能：先立基准，再按数据优化

新增 `--bench` 的「绘图（手机 VML 窗口每帧）」类别（**复用既有 `Benchmark` 骨架**，
不另写一套报告），场景取自真程序（俄罗斯方块满盘 / 五子棋满盘 / 路径曲线渐变），
并把每帧拆成 **图纸→DSL / DSL→文档 / 光栅化+PNG** 三段 —— 只报一个"每帧 X 毫秒"没法指导优化。

**首轮实测立刻指认了瓶颈**：前两段各 1–2ms，**光栅化+PNG 是 145–225ms**（4–6fps）。
再拆一层发现：**抗锯齿占了其中 87～89%**（俄罗斯方块 222ms 里 194ms 是它，关掉只要 29ms）——
固定 3× 超采样在手机全屏上就是"把 183 万像素画一遍再缩回来"。

**优化：超采样倍率按画布面积自适应**（`DrawRunner.ChooseSupersample`，预算 120 万像素）：
小画布（图标、缩略图）仍拿满 3×，手机全屏降到 2×，超大画布退到 1×。
代价是 `O(W·H·s²)` 而画质收益是固定的观感改善、不随画布变大而变大 —— 这正是该自适应的理由。

| 场景 | 优化前 | 优化后 |
|---|---|---|
| 俄罗斯方块（满盘） | 225ms | **108ms** |
| 五子棋（满盘） | 145ms | **71ms** |
| 路径/曲线/渐变 | 177ms | **87ms** |

**诚实的差距**：目标仍是 33ms（30fps），当前满盘最坏情形 71–108ms（9–14fps），
抗锯齿仍占 71–75%。基准的阈值因此设成"当前实测的两倍上下"——它的作用是**抓回归**，
不是假装已经达标。下一步可做的是把每图元的填充从"逐像素委托"改成扫线快路径。

### 四、验证

自测 5122 → **5826** 条全绿。新增 `SelfTest.Chunk22`：路径展平（含隐含重复、相对命令、
贝塞尔/圆弧终点精确性、半径放大、多子路径、坏输入不炸）+ **端到端出图逐像素**
（曲线确实画到了、填充是填充色、奇偶挖洞、渐变两端颜色不同）+ 超采样倍率的单调性。
6 个新 wrapper 的参数编组也在桌面脚手架里逐条验过（含 8 参数的 `ui_gradient` 与点数组）。

### 五、仍欠人工

`Lib/shared/vmlui.vml` 与 `Lib/c/waycoder_ui.h` 里这一批（含此前的 `ui_beep` 等 5 个）
**都要提给上游 VML 仓库** —— `Lib/` 由 `sync.sh` 同步，只改本地下次就没了。

---

## v0.96.175 (2026-09-16) — 查到「同一段代码有时对有时错」的根：**栈清理约定**在库里是分裂的

上一版留了四条"已复现未定因"的现象。这一版把最要紧那条（局部数组像是被后续库调用踩坏）
往下挖，挖到的不是"数组被踩"，而是**栈指针被库函数弄漂了** —— 而且它是**两个约定混在一个库里**。

### 现象与最小复现

```c
ui_beep(10000 + strlen(loc), 1);   /* 期望 10005，实测 6 */
```

`.scratch/dup_a.c`。注意触发形状：**实参里嵌一次函数调用**。
对照 `.scratch/drift.c`：把 `strlen` 单独调用（不在实参里）四次，之后的**纯字面量**调用一切正常
—— 所以它不是"调了就坏"，而是"漂了之后，谁用 `pop` 取临时值谁遭殃"。

### 根因：同一个库里两套 epilogue

函数收尾只有两种写法，`Lib/` 里**两种都有**：

| 约定 | 收尾 | 谁清参数 | `Lib/` 里函数数 |
|---|---|---|---|
| 调用方清（C 前端生成的就是这种） | `move R13 R12; pop R12; pop R15; ret` | 调用方 `add R13 #4/#8` | **446** |
| **被调用方清** | `…; pop R15; move R1 @13; add R13 #8; push R1; ret` | 被调用方自己 `add R13 #8` | **764** |

而 C 前端**每条调用后都自己 `add R13 #N`**（它按"调用方清"生成）。于是每调一次那 764 个
函数之一，**栈指针就多释放一次**（`strlen` 一次净多 4 字节）。漂了之后：

- 实参槽是**按当前 SP 读**的（`move R0 [R13+0]` / `move R1 [R13+4]`）⇒ 嵌套调用夹在中间时，
  后面那句 `pop R1` 弹出的是**错位的值**（dup_a 里 `10000 + strlen(loc)` 的 10000 就没了，
  最终传给 `ui_beep` 的是别的东西）；
- 数组下标的临时值也是 `pop` 取的（`push R0 … pop R10; add R0 R10`）⇒ 下标算错 ⇒ "内容不对"。

这就解释了这一整类症状：**判据本身没错，错的是它读到的数**。也解释了为什么
"同一对判据前两次对、后两次错"（`.scratch/strt4.c`）——漂移量固定，程序在不同位置恰好
落在"还能忍"与"忍不了"的两侧。

### 这一版做了什么 / 没做什么

**只做了诊断，没有改代码。** 因为正确修法不是"把 764 处 epilogue 统一改掉"：
库函数之间也互相调用，A 调 B 时 A 是否清参数取决于 B 是哪种约定 —— 一刀切会改坏另一半。
正解是**用当前的前端把 `Lib/` 重新生成一遍**（让全库统一到"调用方清"），那是上游 VML 仓库
的事（`Lib/` 由 `sync.sh` 同步，本地改会被冲掉）。

⚠ 顺带更正上一版的一句：`clob2.c` 逐条验过，**没有任何单个库调用会踩坏调用方的局部数组**
（掩码恒为"一个都没坏"），所以"局部数组被踩坏"这个说法不准确 —— 真正被弄坏的是**栈指针**。

### 对写手机 C 程序的实际影响（当下）

- 只用 `waycoder_ui.h` 那些包装函数（它们是"调用方清"、与前端一致）的程序**不受影响** ——
  `examples/tetris.c`、`gomoku.c` 都属于这一类，实测正常；
- 一旦用到 `Lib/` 里那 764 个函数（`strlen`/`strcmp`/`printf`/`atoi`/`memcpy` … 大量常用函数），
  **只要这中间还夹着别的调用或数组下标运算**，就可能静默算错。这多半也是
  v0.96.174 里那两条"`puts` 输出串行 / `printf` 崩溃"的真身。

### 复现清单（都在 `.scratch/`）

| 文件 | 看什么 |
|---|---|
| `dup_a.c` | 实参里嵌调用 → 频率 `6`（应 `10005`） |
| `drift.c` | 只单独调 `strlen` → 之后的纯字面量调用**仍正常**（约束了影响面） |
| `strt4.c` | 同一对判据前两次对、后两次错 |
| `clob2.c` | 没有任何单个调用踩坏局部数组（更正上一版的说法） |

---

## v0.96.174 (2026-09-16) — 修 C 前端「全局 char/short 数组按 32 位读写」（第 4 个本地补丁）

起因是 v0.96.173 给游戏加存档时撞上的「**长度对、内容不对**」：`ui_store_get` 明明写进了 5 个字节、
长度也返回 5，读回来却是别的值。这一版把它查到根、修掉，并把当时那条**错误结论**更正掉。

### 真因：`InferExpressionType` 的数组下标分支只查局部变量

`char g[8];` 这样**全局**的数组，元素访问是按 **32 位**做的：

```c
char g[8];
int main() { g[0]='A'; if (g[0]=='A') … }   /* 判成不等：读回来是相邻几字节拼的 32 位数 */
```

`CodeGenerator.Statements.cs` 的 `InferExpressionType(ArrayAccess)` 只查 `variableTypes`
（**只装局部变量**），全局数组查不到就 `return ExprType.Int` ⇒ `GetLoadInstruction(Int)` 给出
`MOVE`（32 位），`char` 的存储宽度被整个忽略。**局部数组一直是对的**（`variableTypes` 里有），
所以这个坑只在全局数组上冒头；`short` 同理（应 `MOVEH`）。写也是 32 位，所以
`char g[8]` 的 `g[7]` 会**越界**写 3 个字节。

修法是把"先查局部 `variableTypes`、再回退 `ast.Variables` 里的全局声明"这个口诀**收成一个
`GetVarExprType`** —— 原先只有"取标识符值"那条路写了它，数组下标那条路没写，
又是一次「助手已存在但调用点绕过」。认不出的类型（自定义 struct 之类）回退成 `Int`，
保持原语义不变。

**落地方式遵循本仓库既有的约定**：不改上游源码（`sync.sh` 会 rsync 覆盖），而是补成
`patches/0004-c-global-array-elem-type.patch` 并写进 `sync.sh` 的【B】清单；
复现用例 `.scratch/vmlhost/tests/globchar.c`。

### 更正：v0.96.173 里那条结论是错的

上一版写着「**局部数组的地址传给函数是错的**（`strcpy(局部,…)` 读出来是空）」，并据此给
`tetris.c` 定了"缓冲区必须放全局"的规矩。**这条规矩并不存在** —— 局部数组传参一直是好的。

误判的来源是**判定方法**：那一版拿 `puts` 的输出当判据，而本环境的 `puts` 在**字符串字面量
较多**的程序里输出会**串行/重复**（同一个字符串打出两种结果）。把一个 stdio 的毛病看成了
codegen 的毛病。改成**不经过 stdio 的判据**（每条结论用一个 `ui_beep` 频率报出来、宿主原样打印，
顺带发现 `ui_dlg_msg` 能当"宿主从内存里读到的字符串"的现成探针）之后，六个格子一次就量清了：

| | 下标读 | 传参写后读回 |
|---|---|---|
| **局部** `char[]` / `int[]` | ✓ | ✓ |
| **全局** `char[]` | ✗ → ✅已修 | ✗ → ✅已修 |

补丁前后各跑一遍，`1002 1003 1004`（对）与 `2002 2003 2004`（错）干净对调。
**凡是"猜编译器"的结论，先在判定链上把 stdio 摘出去。**

### 仍未查清的四条（与本次改动无关，都留了可复现的现场）

查完真因之后顺手把「桌面验证为什么这么难判」也盘了一遍，四条**确定可复现**、但还没查到根因的：

| # | 现象 | 复现 | 备注 |
|---|---|---|---|
| 1 | **调用方的局部数组会被后续库调用踩坏** | `.scratch/strt4.c`：同一对判据，**前两次对、后面两次错**（中间只夹了 `strlen`/`strcmp` 调用） | 最要紧的一条 —— 顺序相关、确定性复现 |
| 2 | **带三元表达式的实参可能传错** | `.scratch/clobber.c` 最后两条：`ui_beep(ok ? 1008 : 2008, 1)` 打出来是 `freq=1`（**第二个实参的值**） | 与 patch 0002 修的「参数寄存器早装载」同族，像是那条修复的漏网 |
| 3 | `puts` 输出串行/重复 | 早先的 `ptr.c` / `chartest.c`；但**单独 20 个 `puts("mNN")` 完全正常**（`.scratch/lits.c`）⇒ 触发条件不在"字面量多"，还得再缩 | 判定时请绕开 `puts` |
| 4 | 桌面脚手架里 `printf` 的格式化路径会崩 | 默认样例程序（`MOVEB @2, R0`，地址是垃圾值） | 只在脚手架观察到 |

**这四条都还没有结论，不要当成已诊断。** 记在这里的价值是：① 写桌面验证程序时知道该绕开什么
（`puts`/`printf` 别当判据，用「一个结论一个频率」那种不经过 stdio 的判定）；② 下次有人接着查
时，现场是现成的。

⚠ 也提醒一句关于**已验证到什么程度**：本仓的 A==C 判据证明的是「两条执行路径逐字节一致」，
**不证明逻辑正确** —— 上面第 1 条就是"两边一样地错"也能通过比对。真要看逻辑对不对，
得靠出帧看画面（gomoku 的棋盘、tetris 的面板都是这么看的）。

---

## v0.96.173 (2026-09-16) — 俄罗斯方块用系统手柄 + 接上音效/震动/最高分；手柄改真按下/抬起

用户原话：「俄罗斯方块本来系统有游戏按键，自己右画了一套，多此一举，不需要，使用系统的屏幕下方的
游戏按键即可，然后加上新的游戏接口，比如声音效果，震动效果等」。

### 游戏示例：`Examples/c/tetris.c` 重写（-140 行布局代码）+ `gomoku.c` 补音效与胜负弹框

- **删掉自绘手柄**：上一版在窗口里画了一整套十字键 + 旋转/直落/暂停/重开，靠触摸命中去判按键。
  代价是实打实的：占掉约 140px 窗口高度（棋盘矮一截）、几何要在「画」与「命中判定」两处各算一遍
  （改个间距就"看着在键上、点下去没反应"）、每个游戏各画一套风格互不相同。
  现在**只认按键消息**，触摸一概不处理 —— 窗口就是一块显示区，整个高度归棋盘。
- **操作**：`← →` 左右（按住连发）、`↓` 加速下落（按住连发）、`↑ / A / X` 旋转、
  `空格 / B / Y` 直落、`START` 重开、`SELECT` 暂停、返回箭头退出。键码就是 `VmlKeys` 那套
  Win32 虚拟键值，接物理键盘同样能玩。
- **接上手感接口**：旋转一声短促干音、自然落地一声闷响、直落低频闷响 + 轻震、消行按行数升高音调
  （1→880 / 2→1046 / 3→1318 / 4→1568 Hz）并震一下、升级盖过消行音、结束时下行长音 + 长震。
  `ui_keep_on(1)` 开局常亮、退出时关掉。**最高分**走 `ui_store_*` 存档，面板里多一行「最高」。
- **音效只用单音、不连发琶音**：`AUDIO_TONE` 是单通道的，来一个新音就把上一个停掉 ——
  连发一串只有最后一个听得见，等于白写。所以改成"用频率高低表达好坏"。
- **长按连发自带三道刹车**（`rptLeft`/`rptStuck`）：**不能把"一定会收到 KeyUp"当成前提** ——
  手指划出按键范围、系统吃掉 CANCEL、页面被切走都可能让 KeyUp 永远不来，而 `ui_timer_set`
  是**重复**定时器，连发一旦跑起来就会一直跑（表现是"方块自己一直往左移"）。所以：换键即接管、
  按了没动两次就停、总拍数上限 40（约 5 秒，横穿棋盘只要 10 拍）。**写游戏时凡是重复定时器
  都要有这么一道安全网**。

### 手柄：`Clicked` → `Pressed`/`Released`（`DrawWindowPage`）

`Clicked` 是抬手才触发一次，**按住不放没有任何后续事件**，于是"按住 ← 连续左移"根本做不出来
（程序只收到一次 `KeyDown`）。改成 `Pressed` 发 `KeyDown`、`Released` 发 `KeyUp`，程序就能自己拿
定时器做连发（DAS）—— 这也是所有手柄的语义。另外手指从一个键滑到另一个键时 Android 只发新键的
`Pressed`、旧键的 `Released` 会丢，所以按下新键前先替旧键补一条 `KeyUp`（否则程序以为两个键同时按着）。

### 桌面验证：`.scratch/vmlround` 加了「脚本化陪玩」

新增 `--play` / `--lines` / `--best N` 三种驱动，用一段脚本化的手柄输入把游戏整条主循环跑通
（按键 → 消息 → 动作 → 音效/震动/存档），并**逐字节比对**「直接编译运行」与「存成 .vml 再独立
汇编运行」两条路的输出。跑通了：旋转、连发（含"抬手之后不该再动"）、自然落地、直落、消行
（专造的五块 O 拼满两行 ⇒ 1046Hz + 28ms 震动）、游戏结束、START 重开、SELECT 暂停/继续、
退出、最高分读（预置 1234 ⇒ 不覆盖；预置 7 ⇒ 覆盖成 381）。

顺带钉住三条**脚手架/前端的既有事实**（写进 [docs/VML宿主接口.md §8](docs/VML宿主接口.md)）：
VM 的 `#50` Random 是**时间播种**的（同一程序两遍方块序列不同，比对必须先钉死随机数）；
`stdio_funcs.vml` 解析不到（`ResolveLibPath` 只搜 `Lib/shared` 与 `Lib`，不搜 `Lib/c`）；
**桌面脚手架里 `printf` 的格式化路径会崩而 `puts` 正常**。

⚠ 这一版还写了一句**后来被推翻的结论**（原文：「局部数组地址传参错、全局 `char` 数组下标读成
32 位，两条既有缺陷」）—— 前者是误判（真因只有一个，见 v0.96.174），当时是拿 `puts` 当判据才看歪的。
原文保留在此以便对照，**不要照它写代码**。

### 五子棋：赢了没提示（真 bug）+ 音效（用户报的）

用户原话：「五子棋游戏也可以加点音效，关键是**五子棋赢了输了，都没看到输赢的提示框，
只是棋盘清空了，重新开始了**！」

- **胜负必须弹框**：原来只在棋盘下面写了一行 15px 的小字「你赢了！点任意处再来一局」——
  玩家盯着的是棋盘，那行字在下方、不闪不动，**等于没交代**；再点一下棋盘就直接清空重开，
  看上去就是"莫名其妙从头开始了"。现在四种结束方式（人胜 / 电脑胜 / 平局 / 无处可下）
  **汇到同一处收尾**：先画完终局棋盘，再出声 + `ui_dlg_msg` 问「再来一局？」——
  选「否」就退出窗口，而不是默默重开。
- **音效**：人落子 880Hz、电脑落子 620Hz（一耳朵分得出是谁下的）、赢 1320Hz 长亮音 + 轻震、
  输 260Hz 低闷长音 + 长震、平局 500Hz、重开 900Hz。赢/输两条刻意做得**音高差别极大** ——
  合成音单通道、一次只发一个音，只能用音高表达情绪（同 `tetris.c`）。
- **顺带修掉一个布局 bug**：`avail = sh - 34; if (avail > sw) avail = sw;` 把宽高两个方向的
  约束**压成一个数取小者**。手机上 `sh≈744 / sw≈395` ⇒ avail 被压成 395，于是格子按宽度算完
  之后**棋盘被居中在"顶部那 395px"里**，屏幕下面空掉一大半、状态文字浮在屏幕中间。
  改成宽高各自约束、再在整块画布里居中。

### 绘图窗口：内容超出绘图区就被切掉（用户报的）

用户原话：「那个俄罗斯方块窗口内容超出绘图区了，能否弄小点点，不要让下面键盘区挡住」。

两处根因，各自修掉：

1. **`FitCanvas` 只按宽度缩放**（`宽 = 视口宽，高 = 宽 × 场景高宽比`）——
   宽度那一维装得下，**高度完全没管**，场景一高就被外面的 `ScrollView` 截在可视区外。
   游戏要滚动才看得全，等于已经不能玩了。改成**两维取小**
   （`min(视口宽/场景宽, 视口高/场景高)`）：无论程序按什么尺寸开窗，整幅场景一定完整可见，
   "被切掉一半"从结构上不可能出现。估算准的时候缩放比恰好是 1，与原来完全一致。
   `OnSizeAllocated` 里的重排判据也一并从「宽度变没变」改成「`FitSize` 算出来的两个数变没变」
   —— 视口变矮时宽度可能没变、高度却变了，只比宽度会漏掉这次重排。
2. **可用绘图区的估算少算了 92dp**：`AvailableArea` 的固定占用原来是 170，只算了导航栏 + 方向键，
   **漏了绘图窗口页自己的折叠条（26dp）与画布留白（16dp）**，于是**每次会话里第一个** VML 窗口
   会比真实视口高约 90dp（真实视口要等页面布局完才量得到，`SCREEN_W/H` 在那之前只能估算）。
   提到 262，并提为具名常量 `VmlUi.DefaultChromeHeightDp`（自测里那个写死的 170 也改成引用它
   —— 平行表正是本仓库头号坑）。

### 仍需人工做的一件事

`Lib/shared/vmlui.vml` 里那五个包装函数（`ui_beep`/`ui_vibrate`/`ui_keep_on`/`ui_store_set`/
`ui_store_get`）**必须提给上游 VML 仓库** —— `Lib/` 是 `sync.sh` 从上游 rsync 同步下来的，
只改本地的话下次同步就没了。

---

## v0.96.172 (2026-09-16) — VML 手感接口（音效/震动/最高分/常亮）；编译看门狗 + 随时按返回终止；应用名「道码 / WayCoder」与包名 com.tanso

这一版两件事：**给 VML 游戏补手感**，以及**把"卡住出不来"的出口补上**。接口清单与设计约束
存档在 [docs/VML宿主接口.md](docs/VML宿主接口.md)，以后加接口先改那份。

### VML 手感接口

| 号 | 接口 | 说明 |
|---|---|---|
| **`#57`** | `SPEAKER_BEEP(freq, ms)` | **VM 内置的**蜂鸣 —— 宿主**截住它**接到真实音频 |
| 541/542/543 | `AUDIO_PLAY/STOP/VOLUME` | BGM（工作区里的音频文件）与整体音量 |
| 545/546 | `VIBRATE(ms, 强度)` / `VIBRATE_PATTERN(模式*)` | 单次与节奏震动 |
| 550/551/552 | `STORE_SET/GET/DEL(键, 值)` | 最高分、关卡进度、设置（键加 `vml.` 前缀与 App 自己的 Preferences 隔离） |
| 553 | `SCREEN_KEEP_ON(0/1)` | 玩游戏时别熄屏 |

**两个刻意的决定**：

- **音效走「接通 `#57`」而不是新增 `AUDIO_TONE(540)`**。运行时的 `#57` 本来就有，但实现是交给
  `VmSpeakerDevice` —— 那个设备**只把样本记进内存/写 WAV 给测试用，不发出任何声音**，手机上跑就是
  "调了没反应"。与其再加一个平行的音效接口（同一件事两处实现，本仓库头号坑），不如把这一个接通：
  VM 的文档、示例、任何语言的前端都已经认这个号，接通一次全都活了。宿主处理器**在内置 switch 之前**
  被调用，所以截得住 —— 这是该设计允许的用法。⚠ **只截这一个内置号**，`Handles()` 仍只认 500–599。
- **没做「随机数」和「取时间」接口** —— 运行时已经有了：`#50` Random、`#53` GetTick、
  `#54` GetDateTime、`#55`/`#56` 日期时间串，`ui_rand` 就是包着 `#50`。先查有没有现成的，
  只不过这次的"仓"是 VM 的内置 syscall 表。

音效是**现场合成**的（按频率+时长+波形算 PCM 交 `AudioTrack`/`AVAudioEngine`），游戏不用带任何音频
素材、不涉版权；合成时加了 3ms 淡入淡出，否则方波头尾会有"咔"的爆音。**Android 与 iOS 各写了
一份实现**（平台 API 完全不同，抽一层接口也只是把 `#if` 挪个地方），参数钳位、包络、单通道语义两边一致。
`VIBRATE` 在清单里加了权限（**normal 级，装上即生效**；漏了声明的表现是"调了没反应"，而且 `Vibrate`
对无权限是**静默失败**）。

### 卡住出不来 —— 三个出口

1. **编译看门狗**：前端编译是同步的、且 `IFrontendCompiler` 上**没有任何取消入口**，
   某些源码会让编译器自己陷进去，而这条链上**从前没有任何出口** —— 界面永远停在"正在编译…"，
   连「强制停止」都按不动（那个 token 只作用于运行阶段）。现在把编译丢到独立线程上、主线程**带超时地等**
   （180 秒；手机上一份 C 程序实测要一分多钟，值必须明显大于合法耗时，否则误杀正常程序）。
   ⚠ 说清代价：超时后**那个编译线程还在跑**（.NET 没法中止线程），这只是把控制权还给用户，不是杀掉编译。
2. **随时按返回终止**：取消源从实例字段改成**静态**（`ShellPage.CancelRunningVml`），
   于是**游戏窗口**也够得着它 —— 退出窗口 = 先发 `WindowClose`（优雅收场）+
   **1.5 秒守望**：到点还在跑就直接取消 token。只发消息不兜底的话，一个不理会该消息的程序
   会一直烧着 CPU 活在后台，而用户以为"我已经退出游戏了"。静态是安全的：VML 执行本就是排他的。
3. 编译也能被停：`vml build` 现在同样装取消源（停的是"等待"，不是那个编译线程）。

### 应用身份

- **显示名按系统语言走**：中文 → **道码**、英文 → **WayCoder**。走 Android 字符串资源
  （`values/strings.xml` + `values-zh/strings.xml`）+ 清单里的 `android:label="@string/app_name"` ——
  csproj 的 `ApplicationTitle` 是**单个写死的字面量**，没法按语言变，只能当兜底。
  iOS 用 `zh-Hans.lproj/InfoPlist.strings` 覆盖 `CFBundleDisplayName`。
  APK 里实测：默认 `WayCoder`、`zh` / `zh-CN` → `道码`。
- **包名 `com.companyname.waycoder.maui` → `com.tanso.waycoder`**（公司：深圳市探索智能科技有限公司 / tanso）。
  ⚠ **改包名 = 换一个 App**：新包不会覆盖旧包（两个图标并存），旧包的**私有目录**（Preferences 存的
  最高分/设置、解压出来的 vml 库）不会跟过来；工作区与 config 在外部存储不受影响。keystore 没变，
  旧包随时可卸。
- 关于页加「作者：施探宇」，csproj 补 `Authors`/`Product`/`Copyright`。

### 真机挖出来的两个 bug（构建全绿、只有上手机才暴露）

1. **`SCREEN_KEEP_ON` 抛 `Only the original thread that created a view hierarchy can touch its views`**：
   `DeviceDisplay.KeepScreenOn` 在 Android 上最终调 `Window.AddFlags(FLAG_KEEP_SCREEN_ON)` ——
   那是**动 View 层级**，从 VM 线程调必炸。宿主处理器里**所有分支都得按这条尺子过一遍**：
   只碰数据的（Preferences / Vibrator / AudioTrack）可以直接调，**凡是碰 View 的一律 marshal 回主线程**。
2. **`~>` 提示符打出完整路径**：`Abbreviate` 里用 `ToRelative` 判"是不是根"，而 `ToRelative`
   要求 `根 + 分隔符` 严格前缀 ⇒ **根自己**返回 null，原来靠一条等值分支单独兜 ——
   结果"`vml build ~/examples/x.c` 缩写对了、提示符却打全路径"（同一个函数、两条分支只对一边）。
   改成**统一按前缀判断**，并在"缩写失败"时记一行日志把两个根都打出来（否则只能靠猜，
   实测为这个多跑了一轮装机）。

### 验证

Android 构建 0 错误；桌面自测 **5796 通过 / 0 失败**（新增 15 项覆盖钳位/键清洗/号段约束）。
真机（Xiaomi 13）逐项验过：文件页配色、`VML 编译`产出 `tetris.vml`(1.1 MB) 与 `tetris.vmb`(826 KB)、
`vml build` 的进度提示与收尾提示符、`feel_test.vml` 跑出「蜂鸣+震动+常亮 → 读最高分(无) →
写 777 → 读回 777」、APK 里的中英文应用名。
**iOS 只到"写完了实现、没编译过"** —— 本机（Windows）编不了 iOS，需要 Mac 验。

### 文件页：能编译的源码都能编辑，图标说"能干什么"

- **所有 VML 能编译的源码都算「源码」** —— 判据直接问 `MauiVml.CanCompile`，**不另抄一份扩展名表**。
  原先 `.lua`/`.pas`/`.bas`/`.r`/`.m`/`.ld`/`.d`/`.f90` 这些（VML 前端认、我们那张表没列）在文件页里
  **连「打开」都没有**，而它们恰恰是最该能改的文件。现在它们的菜单是「打开 / VML 编译 / VML 运行」。
- **图标按"能干什么"分**：能 VML 编译的源码 **📐**（要编译一下才能跑）、`.vml`/`.vmb`（直接就能跑）**🚀**、
  其余照旧（源码/文本 📝、图片 🖼、音频 🎵、视频 🎬、未知 📄）。

## v0.96.171 (2026-09-16) — 手机端文件页接上 VML：编译/运行两个入口 + 文件名分色；命令行页状态可辨；VML 窗口去重标题、手柄可收起

用户的方向是「**在文件管理界面直接操作**，比在命令行页敲命令简单好用多了」—— 这一版把 VML
这条链整个搬到文件页的菜单上，执行过程与结果仍落在命令行页（那里本来就有交互输入、回滚缓冲、
可中断的运行）。**全程真机验证**（Xiaomi 13，逐项截图 + 逐像素量色）。

### 文件页

- **文件名按「在 VML 这条线上是什么」分色**：能编译的源文件（`.c`/`.py`/`.rs`…）**绿**、
  `.vml` **橙**、`.vmb` **红**；**其余文件（含 README.md 这类 VML 编不了的源码）一律不变色** ——
  颜色只用来标记"这个能编 / 能跑"，满屏彩色反而看不出重点。
  「能编译的扩展名」直接问上游那 22 个编译器的注册表（`GetAllFrontendCompilers().SupportedExtensions`），
  **不另列一张表**（上游加一门语言这里自动跟上）。判据收在 `SandboxFsService.DetectVmlRole`
  （四档 `None/Compilable/Assembly/Binary`），配色值在 `Colors.xaml`（亮/暗成对）。
- **菜单按角色给入口**：源文件 →「VML 编译」（产出 `main.vml`）+「VML 运行」；`.vml` →
  「VML 编译」（产出 `main.vmb`）+「VML 运行」；`.vmb` → 只有「VML 运行」（已是终态）。
  产物名走唯一那份规则 `MauiVml.NextArtifact`，与上游 CLI 的默认产物同名同形
  （`vmltool main.c` 写的就是 `main.vml`）。
- **同名产物已存在时问「覆盖 / 重命名 / 取消」三选** —— 不是「确定要覆盖吗」两选：产物正好是
  用户可以手改的文件，静默覆盖不可逆；只给两选则想保旧产物的人只能退出去改名再回来。

### 命令行页

- **运行时报错要看得见**：VML 运行时的报错（`内存错误(PC=…)`/`标签错误`/`未预期崩溃` + 16 个寄存器
  dump）全部走 `Console.Error`、`Permission denied: syscall N` 与 `VM execution cancelled` 走
  `Console.Out`，而手机上那是**一个看不见的流** —— 这正是"程序明明崩了、用户只看到没有输出"的根因。
  现在运行期间把两个流接到内存里并进返回值（`Console.SetOut/SetError` 是进程级的，用静态锁串行化；
  收进来的内容**并进输出**而不是丢掉）。真机验过：`标签错误…未找到标签: ocv_init_done` + 寄存器 dump
  完整显示在输出区。
- **运行中不许直接离开**：按返回先问「是否强制停止？」——「继续运行」就留在本页；「强制停止」走
  `VmRuntime.Run(ct)`（主循环**每条指令**查一次 token，实测死循环 207ms 内停住），等这次运行
  **彻底收干净**再重发一次返回。只拦 VML 运行：普通 shell 命令没有中断入口，弹一个停不掉的
  "强制停止"是骗人。
- **静默等待有状态了**：解压标准库（39 MB / 5200+ 个文件）与前端编译各要好几秒、屏幕上一个字不变，
  和卡死没区别 ⇒ `MauiVml.OnProgress` 钩子打出「⏳ 正在解压 VML 标准库…」「⏳ 正在编译 xxx.c…」
  与完成提示。钩子只在本页发起的运行期间装着，聊天那边跑 VML 不会往这页冒提示。
- **「运行中 / 已结束」的边界**（用户报"摸不着头脑"）：加了 `路径>` 提示符 —— 起点写一行
  `~/examples> vml run gomoku.c`，终点再写一行空的 `~/examples>` 表示"等下一个命令"；
  运行中输入框前面那一格显示 `⋯`。三个入口（手敲、文件页运行、文件页编译）共用
  `RunWithPromptAsync` 一个外壳，免得谁漏掉收尾提示符。
- **路径缩写成 `~/…`**（工作区根 = `~`）：`SandboxFsService.Abbreviate`，
  手机上的 `/storage/emulated/0/waycoder/workspace/examples/gomoku.c` 缩成 `~/examples/gomoku.c`；
  顶栏那行 `cwd:` 去掉（和提示符说同一件事）。

### VML 窗口

- **去掉重复标题**：Shell 标题栏已经显示了 `scene.Title`，页面里那个自绘 HeaderLabel 写着同一句话，
  屏幕上就是上下两个一模一样的标题 —— 删掉页面里那个，标题只留 `Page.Title` 一处。
- **手柄区可收起**：画布与手柄之间加一条折叠条（左右细线 + 中间「▲ 收起手柄」），点一下整块隐藏，
  画布立刻多出那约 150dp（隐藏 `Auto` 行自然塌成 0）。箭头旁带一句话说明，免得"▲ 是收起还是展开"
  要靠点一次才知道。

### 内置标准库（Lib）：不再白解压

- **解压位置从 `Global.Home/vml` 改到 App 私有目录（`AppDataDirectory/vml`）**。`Global.Home` 在
  Android 上会随「所有文件访问」权限在**私有目录 ↔ `sdcard/waycoder/config` 之间跳**，而标记文件
  跟着 Home 走 ⇒ 在系统设置里授权/撤销一次，新位置没有标记，**39 MB / 5245 个文件白解压一遍**。
  App 私有目录不随任何权限变化。老位置里已有一份且内容对得上的用户**就地接着用**，不为搬家白解压。
  顺带对齐了本仓库自己的移动端铁律第 3 条（路径一律走 `FileSystem.Current.AppDataDirectory`）。
- **判据从「哈希整个 6 MB zip」换成随包的 `vml_lib.hash`**（`scripts/make-vml-lib.sh` 生成）：
  读几十字节而不是 6 MB；而且它按「条目名 + 长度 + 内容」算、**不看时间戳**，所以"同样的内容重新
  打个包"指纹不变、不会白解压。哈希缺失时退回原行为，不会更糟。

### 真机上挖出来的两个坑（这才是这一轮的主要产出）

**① `Shell.Current.GoToAsync("//shell")` 在真机上直接抛 `ArgumentOutOfRangeException`** ——
用户看到的是点「VML 运行」/「VML 编译」弹「无法打开命令行页」。原因：Shell 的绝对路由串要一路穿过
`TabBar → Tab → ShellContent` 三层，而 `AppShell.xaml` 里那几个 `<Tab>` **没有显式 `Route`**
（MAUI 自动生成的），`//<ShellContent 的 Route>` 这种写法在这里解析不到 —— 报的还不是"路由不存在"，
是路由解析器内部越界，**光看错误信息根本猜不到**。改成**直接指定 Shell 的当前项**
（`ShellPage.SwitchToShellTab`：找到 `Route == "shell"` 的 ShellContent，把三层依次设为当前）——
这就是点 Tab 时系统做的事，不经过任何路由字符串。修完真机验证：文件页点「VML 运行」直接切到命令行页
并开始跑。

**② `async void` 里的异常会被静默吞掉**。`OnSelectionChanged` 是 `async void`（事件签名定死），
里面任何一处抛出都是进程级未处理异常；而 MAUI 在几条路径上还会先吞掉，现场只剩应用自己
`FirstChance` 日志里一行 `ArgumentOutOfRangeException` —— **连是哪一步炸的都看不出来**。
现在整个处理器兜住并把**堆栈**落进错误日志（`sdcard/waycoder/config/logs/`，adb 直接可读），
还给用户一句人话。同理，文件页递过来的作业是 `Dispatcher.Dispatch(() => _ = RunPendingVmlJobAsync(job))`
起的，那个 `_ = ` 把 Task 丢掉了 —— 一个例子编译时抛 `未找到标签: asm` 就变成"未观察的任务异常"，
屏幕上什么都没有；现在由 `RunWithPromptAsync` 统一兜住并打印。

### 顺带查清的三件事（都记进 CLAUDE.md 了）

1. **`prog.ToString()` 的产物能不能独立跑**：能 —— 但**必须"编完就存、不先跑"**。同一个 `VmlProgram`
   先 `Run` 过再 `ToString`，产物再汇编出来**不等价**（实测一个打印 3 行的程序变成只输出一个换行）。
   产品里 `CompileToVml` 与 `CompileAndRun` 各建各的程序，天然是对的；这条规矩钉在
   `.scratch/vmlround` 里，免得将来有人图省事把两条路合成"编一次、又能跑又能存"。
2. **上游 VMB 编码器不认 `long`**（`Unsupported data type: System.Int64`）—— 而 64 位常量在
   C 标准库里到处都是（hello 级程序的数据段 160 项里 7 项是 Int64）。宿主侧按位换成 `double` 绕开
   （编码/解码/装载三处全是 8 字节搬运，位模式不变），**没有动 `third_party/vml`**。
3. ⚠ **`.vml → .vmb` 这条链上游还断着**：手写的小汇编 `.vml` 编成 `.vmb` 装载运行**输出逐字符一致**；
   但编译器产物（35k 条指令那种）写出的 `.vmb` **自己读不回来**（`InvalidDataException: Unknown
   operand type tag: 0x00`，在**代码段**的 operand 编码上，与上面那个数据段的缺口无关）。
   这条得在上游 VML 仓库补，宿主侧绕不过去。

### 验证

Android 构建 0 错误；桌面自测 **5778 通过 / 0 失败**；真机逐项验过：配色（逐像素量色）、
文件页 →「VML 运行」切页并开跑、`tetris.c`/`gomoku.c` 编译后弹出游戏窗口、运行时报错与
解压/编译状态提示、强制停止、`~>` 提示符与收尾提示符。

## v0.96.170 (2026-09-15) — **C 版俄罗斯方块**；修掉它顶出来的三个 bug；示例打包两处修复

用户决定"先用 C 实现，以后再去修 BASIC"，于是 `Examples/c/tetris.c`：10×20 棋盘、7 种方块、
消行计分升级、下一块预览、长按连发、手柄（圆角方块方向键 + 旋转/直落 + 小尺寸暂停/重开）、
暂停与结束遮罩。全部用现成的 window/draw/msg/timer syscall，布局按实测绘图区自适应。

顺手把共享库补齐：`ui_rand(n)` / `ui_tick()` —— **只有 C 能直接调 syscall**（`#50`/`#53`），
其余前端自己搓不出随机数，放共享库里各语言共用一份。

### 一路顶出来的三个 bug（这才是这一版的主要产出）

做这个游戏的过程**不是"照着写"**，而是不断撞到"编得过、跑起来才错"。三个都修了：

**① 中文渲染成豆腐块** —— `TrueTypeFont.Resolve("sans-serif")` 挑错字体。
`DrawFigure.FontFamily` 的默认值就是 `"sans-serif"`，也就是**所有没写族名的文字**（`draw` 工具、
VML 的 `ui_text`）都走这条路；而它去文件名里找 `sansserif` 必然落空，掉进"随便挑一个能加载的"，
本机挑中的是一个只覆盖少量字形的**试用水印字体**（`HYZhongHeiTi-197`）—— 能解析、能加载，
但 `中` 落回 `.notdef` ⇒ 屏幕上每个汉字都是一个空心方框。**"能加载"不等于"有中文字形"**：
现在通用族名按空族名处理（走首选表 + 中文优先），且**猜字体时必须真有中文字形**，
全都画不出才退回第一个能加载的。

**② 一套"场景→DSL→PNG"的绘图管线，每帧要 4 秒。** 手机端 VML 绘图窗口每帧都走
`VmlScene → DSL → DrawRunner.ToPng`（画布 395×744、**强制 3× 超采样**）。桌面上量出
**每帧 3693ms**，游戏直接不可用。分段量下来两个热点：

| 热点 | 原因 | 修法 | 实测 |
|---|---|---|---|
| 字形光栅化 | 每像素 4×4 采样、**每个采样点把所有轮廓边跑一遍** ⇒ 一个 51px 汉字约 620 万次内层运算 | 扫描线：**同一条子扫描线上所有采样点的 y 相同** ⇒ 每条子扫描线只求一次交点，再用后缀和 + 单调游标回答该行所有采样点（判据与原来逐点等价） | 12 个汉字串 **7169ms → 145ms** |
| 形状填充 | 变换不是恒等就退到 `FillTransformed` 的**逐像素布尔判定**（圆角矩形按 40 点多边形判），而抗锯齿恰恰给每个图元挂了 `Scale(3,3)` | 等比缩放 + 平移时形状缩完还是同一个形状 ⇒ 直接乘倍率走整数扫描线路径 | 120 个圆角矩形 **1115ms → 186ms** |

合计 **120 图形 + 12 段文字从 8640ms → 239ms（36 倍）**；俄罗斯方块整帧 3693ms → 约 300ms。
**逐像素验证过**：字形那一处新旧输出**完全一致（0 个像素不同）**，矩形路径也完全一致，
圆角矩形在 3× 下有 0.8% 的边缘像素有细微差异（两种都合法的光栅化），肉眼无差别。
自测 5778 项全绿。

**③ C 前端不折常量表达式 ⇒ 数组静默只分配 1 个元素。** `int b[BW * BH]`（宏展开成 `10 * 20`）
被判成"运行时维度"进 `VlaDimensions`，编译期尺寸留空，于是**只生成一个 `.word`** ——
之后所有 `b[i]` 都写到别的变量上：编译不报错、跑起来数据全乱。`Parser.Expressions.cs` 里
那句注释"支持常量表达式如 MAXPATHSIZE + 8"说的就是这个意图，但那条路一直没实现。
补了 `TryConstInt` 常量折叠（字面量 / 一元 ±!~ / 常量算术与位运算），固化为
`patches/0003-c-const-array-dim.patch`。

### 无头模拟器：改完先在这里看画面，别为了看一眼效果重打 APK

桌面 `vmlhost` 现在能：`--sim "x,y;t;…"` 脚本化触摸与**重力节拍**、
`--frames <目录>` 把每一帧走**和手机同一条渲染链**出 PNG（可直接看图验收排版）、
`--trace` 打每一次 syscall（定位"卡在哪一条"）、`bench` 量出图成本、`font` 做字体自检
（枚举候选、量推进量、出样张）、`--screen W,H` 换可用绘图区。
俄罗斯方块与五子棋都是这么验的 —— 其中包括**按手机实测的 395×578**（不是屏幕的 744）
把两个游戏的排版都过了一遍，棋盘/信息面板/手柄都在可视区内。

> 这轮的所有结论都来自**量**而不是读代码：`bench` 量出 3.7 秒、`font` 量出"能加载但 `中`→0"、
> 逐像素比对新旧光栅器。同一段链路上"看着对"和"真的是对的"差得很远。

### 示例（游戏）打包：两处真机上才看得出来的问题

用户要求"五子棋、俄罗斯方块都进 Examples，以后会补更多游戏"，于是把这条链从头到尾验了一遍，
**两处都是"桌面看目录一切正常、只有装上手机才发现"**：

**① 示例从来没进过 APK。** `MauiBootstrap.EnsureExamples()` 从 `vml_lib.zip` 里取
`Examples/` 开头的条目解到 `<工作区>/examples/`，而 `scripts/make-vml-lib.sh` 只打了
`Lib` 与 `vmltool.config.xml` —— 一个示例条目都没有，手机上 `/examples` 是空的。

**② 改完第一版又矫枉过正：把上游整棵 `Examples/` 树递归打了进去。** 128 个条目里 107 个是
上游挂着的 stb / stm32 整套工程（pngsuite、EWARM 工程、`.exe`…），而手机端是**平铺**解包的
（丢掉目录）⇒ 上千个文件糊进 `examples/` 一个目录、还有 5 组重名互相覆盖。
现在只收 `Examples/README.md`（第 1 层）与 `Examples/<语言>/<文件>`（第 2 层）——第 2 层
正好等于"平铺后还能一一对应"的那一层，也就是示例的约定存放位置。23 个条目。
**⇒ 以后加示例/游戏，直接放 `Examples/<语言>/` 下，别建子目录**（子目录不会进包）。

顺带：`make-vml-lib.sh` 用 `zip`，而这台机器（Windows Git Bash）根本没有 `zip`、脚本直接失败
—— 加了 Python `zipfile` 回退（固定时间戳，保持"同样内容产出同样字节"）。脚本还会把顶层条目
与示例清单打出来。

`Examples/README.md` 写成了"游戏合集"索引：能玩的（五子棋 / 俄罗斯方块）、**暂时跑不了的**
（`basic/tetris.bas`、`python/tetris.py`，各自卡在哪个前端缺陷上）、「再加一个游戏」的六步清单
（含最容易漏的两条：先问 `ui_scr_w/h` 再开窗、光把文件放进 `Examples/` 不会自动到手机上）。

### 真机验证（Redmi 2211133C，覆盖安装 0.96.158 → 0.96.170，数据未丢）

用仓库 `waycoder.keystore` 签名（`adb install -r` 直接过，无签名冲突），启动后：

- `/storage/emulated/0/waycoder/workspace/examples/` 里 **21 个示例 + README 齐全**；
- 命令行敲 `vml run examples/tetris.c` —— **游戏窗口出得来**：棋盘、下一个、分数/消行/等级
  全部正常，方块与配色正确；
- 输入走 **App 自带的手柄**（方向键 + SELECT/START + X/Y/A/B → Win32 虚拟键），
  游戏的键盘分支认这些键，**在手机上直接能玩**。

> ⚠ 由此暴露一件待定的事：App 自带手柄之后，游戏自己在画布底部画的那个手柄在手机上
> **落在可视区外**（要滚动才看得到）。桌面/模拟器没有手柄，那个还留着；手机上是否去掉，
> 等用户定。

## v0.96.169 (2026-09-15) — **修好 BASIC 的跨帧变量**（模块级变量 / DIM SHARED 现在 SUB 里可读可写）

按 v0.96.168 里那份设计实施完毕。**语料 t4/t7/t8 全部转绿**：

| 用例 | 修前 | 修后 |
|---|---|---|
| `t4` SUB 读模块级变量 | `sub=0` | **`sub=10`** ✓ |
| `t7` `DIM SHARED` 跨 SUB | `shared=0` | **`shared=77`** ✓ |
| `t8` SUB 写模块级变量 | `counter=0` | **`counter=10`** ✓ |

### 做法

给全局变量划一块**静态区全局段**（`STATIC_GLOBALS_OFFSET = 0x5000`，追加在现有硬编码分区
**之后**，不动 DATA/文件句柄/调色板/声音那几块，免得重新编号引新错；`STATIC_TOTAL_SIZE`
0x5000 → 0x7000），读写一律经 `EmitStaticAddr` 就地算绝对地址再间接寻址，主程序与 SUB
指向同一块内存。`currentSubName == null` 时创建的变量即为全局。启动时把该段清 0。

顺带把 FOR 循环计数器、`GenerateSubLetStatement` 的兜底分支等**六处**仍在按 `R12+索引×4`
直接寻址的站点一并收进 helper —— 它们各自形状不同，是这次最花时间的部分。

### 三个既有缺陷（**与本改动无关**，已用基线证明并记入语料）

修完跑 `tetris.bas` 仍卡死，二分出 `t10`（**SUB 内局部 FOR 循环**死循环）、
`t11`（**SUB 内局部数组**赋值读回 0）、`t9`（FUNCTION+SUB 组合），再用 `git stash`
暂存全部改动、重编"改动前"的编译器复验 —— **三者表现一模一样** ⇒ 是既有缺陷。
这一步很值：不然会把三个老 bug 记成自己的回归，方向全错。

> 影响：`Examples/basic/tetris.bas` 的 `draw_board`/`clear_lines` 全是 SUB 内局部 FOR 循环，
> 撞上 t10 就卡死，所以 **BASIC 版俄罗斯方块暂时搁置，先用 C 实现**（用户决定），
> BASIC 这几个缺陷后续单独修。

另外把 `\` 整除与数组读写（v0.96.166 修的两处）也补进语料回归，现在 `run.sh` 共 11 项。

## v0.96.168 (2026-09-15) — BASIC 回归语料（前端原本没有），并查清跨帧变量的真实状况

### 补了 `scripts/basic-tests/`（7 个用例 + 跑分脚本）

`Examples/basic/` 下那两个 `.bas` 内容是 `404: Not Found`（坏掉的下载残留），`Lib/basic/`
又全是 `asm("SYSCALL ...")` 那种死绑定 ⇒ **BASIC 前端实际上没有可用的回归语料**。
没有语料就没法安全地改它，所以先补：标量/算术、数组、SUB 传参、FOR/WHILE、跨语言调共享库，
外加两个"跨帧变量"的目标用例。基线记录在该目录的 README 表格里。

### 查清了④的真实范围 —— 比原先判断的更大

原先以为"模块级变量只是被 SUB 当成了局部变量"，按那条思路加了守卫（v0.96.167）之后
**实测仍是 0**，于是继续挖，得到两个结论：

1. `VarMemRef` 对模块级变量返回的是 **`R12+偏移`** —— 相对**当前帧**。进 SUB 后 R12 是子帧，
   所以模块级变量对任何 SUB 都不可达；
2. 更要紧的是：**`DIM SHARED` 也读不到**。实测 `DIM SHARED g` + 模块级 `g = 77`，
   在 SUB 里读 `g` 得到 **0**。

也就是说，BASIC 前端**不存在任何可用的跨帧变量机制**（模块级、`DIM SHARED` 都不行）。
这不是"一条寻址规则要改"，而是**跨帧变量的存储与寻址压根没实现**。

> 这一点直接改变了修法判断：`StaticBase + n` 那条"共享变量"路径同样是裸数字地址
> （`StaticBase` 只是个返回常量 `0x6FD4` 的属性），照它改等于**照着一个同样坏掉的参考实现去修**。
> 要真修，得先**设计**全局变量区（布局、与 `STATIC_DATA_OFFSET`/文件句柄区/调色板区这些
> 硬编码分区的边界、初始化、主程序与 SUB 两侧一致的寻址），而不是找一处补一行 ——
> 这需要有语料兜底地单独做一轮；语料这一轮已补上。

### 本轮仍已交付且实测过

`\` 整除（`100\20` → 5）、数组读写（`arr(2)` → 7）、跨语言调 C 共享库（BASIC/Python
8 参数全对、`ui_scr_w()` → 395）。BASIC 回归 7 项里只差跨帧变量那 2 项。

## v0.96.166 (2026-09-15) — 修 BASIC 前端两处硬伤（整除 / 数组），并定位到第三、第四处

用户报「俄罗斯方块在 BASIC 上跑不起来」，一路查到前端。**两处已修并实测通过**：

### ① `\`（整除）**根本没有实现** —— 静默丢掉整个右半表达式

词法里**没有 `\` 这一支**，它落进"未知字符"分支发 ERROR token，表达式解析走到那儿就停 ⇒
`c = 100 \ 20` 得到的 `c` 是 **100**（`\ 20` 整段被丢）。而按 `\` 排版的代码
（拿格宽算坐标那种）分母会变成 0，踩运行时"整数除零"。

修法：加 `TokenType.INT_DIVIDE` + 词法一支 + 乘法级优先级 + 代码生成走 `/` 同一条除法路径。
**实测 `100 \ 20` → 5** ✓

### ② 数组：读、写**各反了一次**

`GenerateArrayAccess` / `GenerateArrayAssignment` 两处的收尾都写反了：

- 写：`MOVE R3, R0` —— 把**地址**写回 R3，值**根本没存进数组**；
- 读：`MOVE reg, R2` —— 把**地址本身**当成了元素值。

叠在一起的现象极具迷惑性：`arr(2) = 7` 之后读 `arr(2)` 得到 **65556**（一个栈地址），
看着像"数组全是野值"，其实写压根没生效。

修法：写用寄存器间接寻址 `MOVE @R0, R3`，读用 `MOVE reg, @R2`。**实测 `arr(2)` → 7** ✓

> 定位过程本身有个坑记一笔：改完代码后**我的桌面宿主用了陈旧的 BasicCompiler.dll**，
> 重编没用 —— 现象是"同样的输出"。`rm -rf bin obj` 全量重建后才看到真实结果。
> 差点因此把已经修好的两处判成"没修对"。

### ③ 未修：`INT()` 是**根本没实现的 builtin**

它解析成库函数 `basic_int`，而全仓 `Lib/` 下**搜不到这个标签**（不是没链接，是没定义）。
本轮的俄罗斯方块不需要它（`\` 修好后就不用了），但它是个该补的洞。

### ④ 未修（当前卡住 tetris.bas 的那一个）：**SUB 里读不到模块级变量**

实测：模块级 `DIM BW` / `BW = 10`，在 **SUB 里** `PRINT BW` 得到 **0**。
根因在 `CodeGenerator.Sub.cs`：SUB 内遇到不认识的标识符会**自动登记成局部变量**
（未初始化即 0），从不回查模块级变量。于是 `cell = (sh - 70) \ BH` 的分母成了 0。

这不是"再补一行"能解决的 —— 要让 SUB 解析标识符时**先查模块级变量表**并按其（全局）地址取用。
绕法（把量都当参数传进 SUB，或干脆不写 SUB 全部内联）能跑，但那是在给前端缺陷打补丁、
会把示例写坏，所以 `tetris.bas` 保持"写好了但暂不绕"的状态，文件顶部写明卡点。

## v0.96.165 (2026-09-15) — BASIC 也接上共享库；定位 BASIC/Python 前端的短板

跨语言调用这一层**已在 BASIC 上复验通过**（与 Python 同样）：`NATIVE SUB/FUNCTION` 声明 +
空体 ⇒ 调**裸标签**（不带 `NATIVE` 会被加上 `sub_`/`func_` 前缀，链接期"未找到标签"）。
实测 `ui_rect(10,20,30,40,255,1,2,3)` **八个参数全对**、`ui_scr_w()` → 395 ✓。
共享库给 basic 的 `Libs` 也挂上了 `vmlui.vml`。

另给共享库加了**方块几何**接口（`ui_piece_init` / `ui_piece_cell`）—— 位运算与旋转公式放在 C 里，
各前端只负责"拿坐标 → 画"，因为 BASIC 只有 AND/OR（没有移位取位）、Python 的列表又不可靠。

### 实测出来的前端短板（这是"俄罗斯方块用高级语言写"真正的拦路虎）

**BASIC**：
- `\`（整除）**不生效** —— `c = 100 \ 20` 得到的 `c` 是 **100**（`\ b` 整段被丢），
  于是按 `\ BH` 排版直接踩运行时"整数除零"；
- `INT()` 解析成库函数 `basic_int`，而它**没有链进来**（未找到标签）；
- **数组不可靠** —— `DIM arr(5)` 之后 `arr(2) = 7`，读回来是 **65556**（野地址）；
- `DIM` 必须排在赋值**之前**（否则那个赋值不落到整型变量上）。

**Python**（上一版已记）：列表**能读不能写**（`b[i]=v` 读回 0）、嵌套列表索引错、
深层 `while/if` 嵌套报"意外的 token: NEWLINE"。

### 因此

`Examples/basic/tetris.bas` 与 `Examples/python/tetris.py` **都已写出但暂不能跑**，
两份文件顶部都写明了**卡在哪、怎么绕、已验证到哪一步** —— 它们现在的价值是
"记下正确的跨语言写法 + 记下前端缺陷清单"，等前端补齐即可直接跑。共享库那一层已经就绪。

> 用户提到的 `public static native callfunc()` 在仓库里**不存在**（已全仓搜索）。
> 若指的是要引入一套统一的外部调用入口，那是个新设计，需要先定接口再动手。

## v0.96.164 (2026-09-15) — **打通「其它语言调用 C 共享库」**（三处编译器修复）

在这之前，非 C 前端调 C 共享库是**不通的**，而且不通得毫无提示。三处一起修才有意义：

### ① C 被调方用 R0–R3 **覆盖**栈上的实参（关键）

序言里那段「把 R0-R3 存回 `[R12+偏移]`」是给 **CCv2**（`CallingConvention.C`，前 4 个参数走寄存器）
用的，但它原来对 **cdecl 也生效**（`regParamCount` 的 `_ => 4` 兜底把 cdecl 也算进去了）。
于是被调方**用寄存器里的残留盖掉调用方压进来的真参数**。

C→C 一直"看着是对的"，因为 C 的 caller 顺手也装了 R0-R3；**只有别的语言调过来才暴露** ——
Python/BASIC 等前端是**只压栈**的（`VisitCall` 末尾统一 `ADD R13, n*4` 清栈，正是 cdecl 的
"调用者清理"），寄存器里根本没有实参。实测：

```
ui_rect(10,20,30,40,255,1,2,3)  →  R0=10 ✓  R1/R2/R3 **全是 0** ✗  R4-R7 却是对的
```

第 5 个参数起才走栈，所以症状精确地是「**四个参数以内的库函数，只有第一个参数生效**」。
改成按约定门控：`CallingConvention.C/Fastcall => 4`，其余（cdecl/stdcall/pascal）**=> 0**。
修后同样的调用 **8 个参数全对**。

### ② Python 前端**静默丢弃**对库函数的调用

`PythonCompiler/CodeGenerator.Expressions.cs` 的 `VisitCall` 只对**本编译单元内见过的标签**
发 `CALL`，不在表里的一律落进"内置函数"分支、**什么都不发**。而链进来的库
（`vmltool.config.xml` 里各语言的 `Libs`，例如 `vmlui.vml`）标签不在编译期的 `labels` 里 ⇒
`ui_win_open(...)` 编出来**一条 CALL 都没有**：程序照跑、界面上什么都没有、也看不出哪儿错了。

改成**认不出来就按标签 CALL** —— 名字拼错会在**链接期**报「未找到标签」，而不是凭空消失
（与 CLI 那条「有错即报错，绝不静默忽略」同一原则）。

### ③ 预处理：光秃秃的 `#` 行**打挂整个编译**

`Preprocessor.cs` 对 `#` 开头行走指令解析，`dirParts` 在"只有 `#`"时是**空数组**，
下一行 `dirParts[0]` 直接 IndexOutOfRange **未捕获异常**。而 `#` 单独成行在 Python/BASIC/shell
里都是**合法注释**（实测：Python 源里写一行 `#` 就让编译器崩）。空数组当"不是指令"处理。

### 实测结果（桌面宿主 + 寄存器探针）

```
Python:  ui_scr_w() → 395 ✓        ui_set_font(18,3,0xFFFFFFFF,2) → R0..R3 = 18,3,-1,2 ✓
         ui_rect(10,20,30,40,255,1,2,3) → R0..R7 全对 ✓
C 回归： 五子棋照常跑（#532/#533 都在用）；6 份真实 C 库源码（含 7541 行的 graphics.c）编译通过；
         全部校准用例（含返回值 23130）通过
```

> **仍未完成**：Python 版俄罗斯方块（`Examples/python/tetris.py`）已写、跨语言调用这一层已通，
> 但还卡在 **Python 前端自身的语法/语义短板**上（实测：列表**能读不能写** —— `b[i]=v` 之后
> 读回来还是 0；嵌套列表索引错；`while/if` 深度嵌套时报"意外的 token: NEWLINE"）。
> 程序里已绕开列表（棋盘改用共享库的整数网格 `ui_gset/ui_gget`），剩下的解析问题待单独处理。

## v0.96.163 (2026-09-15) — UI syscall 提为**共享调用库**（各语言共用一份实现）

原先 UI 接口的实现写在 `Lib/c/waycoder_ui.h` 里（头文件自带实现），**只有 C 能用** ——
因为汇编级 syscall 只有 C/ObjC/C++ 能直接调（`asm()`），其余前端只能**按标签调 C 函数**。

现在实现挪到 **`Lib/shared/src/vmlui.c`** → 编成 **`Lib/shared/vmlui.vml`**，
由 `vmltool.config.xml` 挂到各语言的 `Libs`（C: `builtins+crt+vmlui`，Python: `vmlui`）。
`Lib/c/waycoder_ui.h` 只剩声明与常量，`#include` 它不会让程序变大。
**C 与其它前端从此共用同一份实现**（早先那样各写一遍正是本仓库排第一的坑）。

顺带加了一组**面向不支持指针的语言**的取消息接口（`ui_wait_msg` / `ui_poll_msg` /
`ui_msg_type` / `ui_msg_a` / `ui_msg_b`）—— C 里可以 `int msg[4]` 直接拿到地址，
Python/BASIC 拿不到，也没有按 int 下标读内存的手段。

C 侧回归：五子棋改走"声明头 + 链接共享库"后重编重跑，行为与之前一致（`#532/#533` 都在用）。

> ### ⚠ 未完成：Python 版俄罗斯方块 —— 卡在 Python 前端，不在共享库
>
> 共享库这一层已经就绪，但 **Python 前端调不到它**：
> `PythonCompiler/CodeGenerator.Expressions.cs:195` 的 `VisitCall` 是
> `else if (labels.ContainsKey(callTarget)) Emit(CALL)` —— **只对本编译单元内见过的标签发 CALL**，
> 不在表里的一律落进"内置函数"分支、**静默丢弃**。链进来的库（`vmlui.vml`）标签不在 `labels` 里，
> 于是 `ui_win_open(...)` 编出来**一条 CALL 都没有**（实测：生成物里搜不到 `ui_` 也搜不到 `CALL`）。
> 这正是 `Lib/python/*.py` 全用 `asm("CALL …")` 写的原因，而 **`asm` 在 Python 里是空操作**
> （同文件 154 行注释：「asm() 仅限 C/ObjC/C++」）⇒ 那批绑定整体是死的。
>
> 另有两处 Python 前端短板（都实测过）：**列表不可靠**（`b.append(i*2)` 之后 `b[3]` 读出
> 6649202 这种堆地址、嵌套列表索引全错），以及上面那个"未知函数静默丢弃"。
>
> **要往下走有两条路**：① 改 Python 前端的调用解析——未知函数**回退成按标签 CALL**
> （而不是丢弃），这同时能让 `Lib/python/*.py` 那批绑定活过来；② 俄罗斯方块改用 C 写。
> 两条都能做，但都是另一轮的事，先在此记明。

## v0.96.162 (2026-09-15) — 随包示例程序（经典小游戏）解到工作区

示例源码（`Examples/` 下的经典小程序）随 `vml_lib.zip` 一起打进 APK，首次启动解到
**工作区** `<workspace>/examples/` —— 手机端的工作目录就是工作区，放这儿用户开箱即可
`vml run examples/gomoku.c`（放 `Global.Home` 下的话还得先 `cd` 出去、还会被沙箱拦），
在「文件」页里也看得见。

两个细节：① 解包标记里存**版本号**而不是只看"文件在不在" —— 示例集随版本增删，
只判存在的话老用户永远看不到新示例、还留着一堆已删掉的旧文件；② 重解时**先清空目录**，
保证内容与当前版本一致而不是累加。

> 顺带记一条**未做**的事：用户要的「俄罗斯方块用 VML 的 Python 写」目前**做不了** ——
> Python 前端不支持内联 `asm()`（`PythonCompiler/CodeGenerator.Expressions.cs:154` 明确写着
> "asm() 仅限 C/ObjC/C++，其他语言通过 `Lib/c/vmlsys.c` 调用"），而那座桥自己也坏着：
> `Lib/shared/src/vmlsys.c` 里 `void vml_print_int(int n) { asm("SYSCALL 6"); }`
> **根本没把 n 放进 R0**（与 C 侧那个直写变量名的坏写法同源，134 个函数都是这个样子）。
> 要让它能跑，得先补一座**汇编级**的桥（`Lib/*/vmlsys.vml` 那种形态，各语言共用），
> 再在 Python 里调它。

## v0.96.161 (2026-09-15) — 绘图窗口渲染链路重做（闪烁/中文/圆角）+ 文字属性接口 + 手柄区重做

用户连着报的五件事，逐条按根因改，全部在模拟器上实测过。

### ① 闪烁：**换 `Image.Source` 这件事本身**就是根因

先做的"每条图元渲染一次 → 改成每帧一次"只减轻、没消除（用户复测："比之前好一点，但还闪"）。
真根因是渲染结果经 **PNG 编码 → `Image.Source`** 贴上去：换 Source 会**先卸旧图再解码新图**，
中间必然空一拍，那就是看得见的闪。**次数再少也还是有那一拍。**

改成 `GraphicsView` + `IDrawable` **就地重绘**：后台算好 `IImage` 再 `Invalidate()`，
平台双缓冲保证"旧帧一直在屏上、直到新帧画完" —— 中间不会露出背景色。

### ② 中文渲染成豆腐块：`FontFinder` 在 Android 上**一个字体都找不到**

`OperatingSystem.IsLinux()` 在 Android 上返回 **false**，于是它掉进了 Unix 分支只找
`/usr/share/fonts`（Android 上不存在）。三处一起修：

- 给 Android 单列一支，找 `/system/fonts`；
- 认 **`.ttc` 字体集合**（Android 的中日韩字体几乎都是它）—— 顺带修了 `TrueTypeFont` 的 TTC 解析：
  **不能"切掉集合头再递归"**，TTC 里每张表的偏移是相对**整个文件**的，切一刀偏移就全失效；
  正确做法是在原数组上换目录起点；
- 首选表命中不了时**先挑带中文字形的**，再退化到"任意一个"。

但 Android 的中日韩字体是 **CFF(OTF) 轮廓**，而本仓库的解析器只支持 glyf ⇒ 仍然加载不了
（实测：找到 208 个系统字体、`Resolve` 返回 null）。所以再把**随包的 Sarasa Mono SC**
（glyf + 全中文覆盖）从 APK 资产释放到 `Global.Home/fonts/` 供文件级查找，并让 `Resolve`
**在候选之间依次试加载**（挑中的解析不了就继续往后找），而不是一次失败就交白卷。

### ③ 圆角/斜线平滑：光栅器**本来就支持抗锯齿**，只是没开

`DrawDocument.Antialias` 一开即走 3× 超采样 + 盒式降采样（圆角、斜线、文字边缘一起变干净）。
由 `VmlScene.BuildDsl()` 显式发一条 `antialias` —— 放在场景层而不是页面层，
这样"出图"与"开窗显示"两条路看到的是同一份语义。

> 随之调整一条逐像素断言：原来写死 `HexAt(8, 18) == "#ffcc00"`，那是"描边正好压在第 8 列"的
> 假设，**只在没有抗锯齿时成立**；开了之后边缘按覆盖率混色（实测 69%），
> 写死坐标等于在断言"没有抗锯齿"，方向反了。改成扫一行取最黄像素、判"黄占绝对主导"。

### ④ 触摸：坐标必须换算成**场景坐标**

画布是缩放贴上去的（还可能有黑边/滚动偏移），原来直接把**视图坐标**发给程序 ⇒
程序按自己的网格算就会偏。现在由绘制体记录贴图矩形并反算，落在图外（黑边）的点击直接丢掉。

另修两个让"整块画布全黑、点也点不动"的坑：`GraphicsView` 在首帧渲染时页面还没布局、
可用宽度是 0，**"取不到就不设尺寸"会让视图零尺寸**（画不出、也点不到，而版本号已记下、
后续不会再渲染）—— 现在兜底成场景宽度，并在 `OnSizeAllocated` 里重算。

### ⑤ 文字属性接口：`SET_FONT`(532) + `TEXT`(533)

`532 SET_FONT(字号, 样式位, 颜色, 锚点)` 设一次，`533 TEXT(x, y, 文本*)` 只管画 ——
状态式与一次性的 `528 DRAW_TEXT` 并存（后者补了 R6=样式位，老调用不传即 0，不破坏兼容）。
样式位 `1=粗体 2=斜体`，DSL 侧翻成 `bold`/`italic`/`bi`。文本按 **UTF-8** 读。
C 侧：`ui_set_font()` / `ui_text_cur()` / `ui_text_styled()`，宏 `VML_FONT_BOLD` 等。

### ⑥ 手柄区重做 + `SCR_W/H` 改为**实测视口**

手柄区整体压小一档（方向键 42×38 **圆角方块**、SELECT/START 70×28 上下排开且不再压到两侧）。

`SCREEN_W/H` 原来按屏幕尺寸减一个**固定** chrome 高度估算，而真实占用随设备/排版变
（实测：估算 744dp，画布实际只有 578dp）⇒ 程序按 744 排版，**底部一百多 dp 的内容
（状态文字、计分板）全落在可视区外**，看着就像没画。现在由绘图页把**实测到的画布视口**
写回宿主，`SCREEN_W/H` 优先报它；首次运行尚无实测值才退回估算，**第二次运行起完全贴合**。

> 顺带：`examples/c/gomoku.c` 的窗口尺寸与排版尺寸**同源**（先问可用绘图区再开窗）——
> 按 395 排版却开 360 宽的窗口，棋盘右半边会被画到画布外（"屏幕右边超出"）。

## v0.96.160 (2026-09-15) — VML 的 C 前端两处错误代码生成 + 命令行页命令注册表与滚动条

### ① C 编译器：参数寄存器过早装载（**错误代码生成**）

cdecl 调用是「从右到左、边求值边 `MOVE Ri, R0`」，而**下一次实参求值会用 R0/R1 当临时寄存器**
（表达式产物里到处是 `PUSH R0 … POP R1`）⇒ 已经装好的参数被冲掉：

```c
probe(p + 3*k, q + 3*k, 3*k);   /* p=29 q=41 k=24，期望 101/113/72 */
修前：R0=101  R1=101  R2=72      ← 第二个参数变成了第一个的值
修后：R0=101  R1=113  R2=72
```

只要实参是**带运算的表达式**、且它前面还有别的实参就会中招。改法：先全部求值 + 压栈并记下
各自相对 R13 的偏移，全部压完之后**统一从栈上把 R0–R3 装回来**（读的就是自己刚压进去的值，
与后面还有没有求值无关）。五子棋的星位 `ui_circle(pad+3*cell, padY+3*cell, …)` 就是这么画歪的。

### ② C 编译器：`R12-4` 撞车（**"弹窗里字符串大多是空的"的根因**）

`asm("SYSCALL #…")` 执行完会把 R0 存回 `[R12-4]`（asm 结果暂存槽），而局部变量分配器把
**第一个声明的局部变量**也放在 `R12-4` ⇒ 任何一条 asm 执行完都把它覆盖成上一条 syscall 的返回值
（实测读到的正是 10 = 前一条「输出换行」的返回值）。C 前端现在先占住 `R12-4`，
用户局部变量从 `R12-8` 起 —— 与 `BasicCompiler` 把 `R12-4..R12-256` 预留给临时槽的处置一致。

两处都记进 `third_party/vml/patches/0002-c-codegen-fixes.patch`（`sync.sh` 会重放，不会被打回重来），
**并需回灌 VML 上游仓库**。回归：全部校准用例、结构体/递归/数组用例通过，
5 份真实 C 库源码（含 7659 行的 `graphics.c`）编译通过。

### ③ C 侧 syscall 约定实测钉死 + `Lib/c/waycoder_ui.h` 包装库

校准结论（桌面宿主 + 寄存器探针，见 `third_party/vml/Lib/README-ui.md`）：
参数只能用 **`${名}` 占位符**（按出现顺序落 R0/R1/R2…），返回值必须写成 `x = asm(...)`；
`asm("MOVE R0, s")` 这种**直写变量名**的写法拿不到值（汇编器没有局部变量的符号表）——
而 `Lib/c/stdlib.c`、`time.c` 里全是这种写法，**待回上游核查**。

新增 `Lib/c/waycoder_ui.h`（对话框/窗体/绘图/输入全部 syscall 的 C 封装，自带实现）+
`Examples/c/gomoku.c`（五子棋：人机对战、棋盘棋子全绘制、触摸定位、按可用绘图区自适应）。

### ④ 命令行页：命令注册表

命令从「页面里一串 `if (cmd == "xxx")`」改成**注册机制**（`UI/Shared/ShellCommands.cs`）：
每条命令声明 `名字 / 参数格式 / 参数个数上下界 / 说明 / 详细说明 / 执行体`，
`help` 列表、`命令 -h` 用法、参数校验**全部从同一条记录推出来**（本仓库排第一的坑是
"必须手工同步的平行表"，收成一条数据后想漏都漏不了）。新增 `/help`、`cls`、
`clear`、`vml -h`；不认识的输入仍原样交给 shell。

### ⑤ 命令行页：输出区滚动条（只在超屏时出现）

自绘细轨（`UI/Shared/ScrollBarMath.cs` 是纯几何，带断言），**内容不超屏就整条藏起来**；
可拖动、点轨道翻页。同时把"新输出无条件弹到底"改成**贴底才跟随** —— 一条命令持续吐输出时，
用户往回翻一屏再也不会被拽回底部。

> ⚠ **`ScrollView.ContentSize` 报的高度是虚的**：实测 Label 量出来 1750px 而 `ContentSize.Height`
> 报 ~3000px。按它滚就会**滚过内容**（顶部被切、底部留白），且"贴不贴底"判据永远为真、
> 每次都多滚一截，用户看到的就是"在滚但翻不动"。改用 Label 的**实测高度**后两处一起正常
> （滑块长度也按同一分母算，同一处错误连累两个地方）。

## v0.96.159 (2026-09-15) — 手机端 git 全流程打通 + VML 弹窗/绘图/输入系统调用 + 配色统一

一整轮**真机 + 模拟器实测**驱动的修复与新功能。所有结论都有实测证据（轨迹文件、日志、逐像素断言），
不是照猜改的。

### ① `cd` 是个只说好话的 no-op —— 手机端 git 因此一步都跑不了

`CdTool` 回一句「✔ 工作目录: …」，可紧接着 `pwd`/`ls`/`git` 全按旧目录解析。根因是
`CwdContext` 直接用 `AsyncLocal<string?>` 存值：**往 `AsyncLocal.Value` 赋值 = 给当前上下文换一个新值**，
而工具是**在 async 被调方里**跑的（Agent → RunToolAndRecordAsync → ExecuteToolAsync → tool.ExecuteAsync，
多工具还各起并行分支），赋值传不回 await 上游的调用方。于是 `cd` 从来没生效过，
桌面端同样受影响。

改成 **AsyncLocal 持一个「盒子」**：setter 改的是盒子内容（堆对象）而非换 AsyncLocal 值，
父子上下文共享同一个盒子 ⇒ 修改能被调用方读到。配套 `PushScope()` 给槽位 / 子智能体 / Web 页面 /
自测各开新作用域（一个盒子 = 一个逻辑智能体的 cwd），子智能体的 cd 隔离变成**结构性**的，
不再需要「记下父值再恢复」。

顺带修掉这条路暴露出的两个洞：
- **`SetDefault`（进程级默认目录）**：作用域设置与消费若不在同一条 async 流（切系统深浅色导致
  Activity 重建就是），消费方会惰性新建空盒子并回退到**进程 cwd** —— 而手机进程 cwd 是
  `waycoder/config`，不是 workspace。实测后果：Agent 在工作区外 mkdir/clone，随后每个 write/edit
  都被沙箱拒绝、整轮卡死，而 `cd` 与 `clone` 却都"成功"，极具迷惑性。
- **`cd` 拒绝越出沙箱**：以前 cd 能切到项目外、写工具却只在项目根内可写 ⇒ 半死状态。

### ② 三个 git 真缺陷（手机实测挖出）

| 缺陷 | 后果 |
|---|---|
| `git clone` 会把**现有仓库的 origin 改掉** | 写 remote 发生在联网**之前**，**克隆失败也照改**。实测：在 `way-coder` 里 clone 一个 GitHub 仓库（超时失败），该仓库 origin 已被改成对方地址，下次同步就发错地方。现在目标已是 git 仓库时直接拒绝 |
| 凭证**不按 host 区分** | 仓库里存了凭证就无条件挂到任何 URL 上 ⇒ Gitee 的 token 被当 Basic 头发给 GitHub，公开仓库也报 401；更是**把 A 站凭证交给 B 站主机**。新增 `CredentialFor(gitDir, url)`，只在目标 host 与本仓库 origin 一致时发送 |
| 一条 `git status` 能灌 135k tokens | 无提交的仓库里所有文件都是「未跟踪」，整份清单进上下文；且 4000 字符裁剪只在到 50% 阈值时才触发，这份清单会**跟着每一轮重复发送**（实测单轮任务累计 prompt 670 万）。三个列表统一封顶 200 条 + 给出总数 |

另修：轨迹 `.jsonl` 带 BOM（`Encoding.UTF8` → `Utf8NoBom`，严格解析器读不了）、
`2>/dev/null;` 被沙箱误拦（重定向正则把尾随 `;` 吃进路径）、
`file-tracker.json` 自我假警报（**第一版只修了写入路径、漏了 `Load` 恢复路径**，
手机上重测警告从 5 次涨到 15 次，补齐后降到 0）。

### ③ VML 手机端新增 UI 系统调用（号段 500–599）

VML 程序此前在手机上只能输出文本。新增三类宿主实现的 syscall，**`third_party/vml` 运行时一行未改**：

- **对话框** 500–503：消息框 / 单选 / 多选 / 文本输入（复用现成交互桥）
- **窗体与绘图** 520–531：开窗 + 点/线/矩形/圆角矩形/圆/椭圆/文字/图标/图片/清屏/提交帧
  （保留模式：VM 只往场景追加图元，宿主按帧渲染；图元语义复用现成的 `DrawCommandRegistry` 16 条指令，
  **不另写渲染器**）
- **输入** 560–565：**统一消息队列**（键盘/鼠标/触摸/定时器/窗口事件同队，16 字节定长消息），
  `MSG_POLL`/`MSG_WAIT`/`MSG_COUNT`/`TIMER_SET`/`TIMER_KILL`/`WIN_CLOSED`
- **可用绘图区** 566/567：让程序先问后开，不再自己拍尺寸被宿主缩放（⚠ 这两个**尚未跑通**，见下）

接线靠两处已核实的事实：宿主处理器**先于**内置 switch 被调用；`SyscallConstants.UserAllowed` 与运行时
内部白名单是**同一对象引用**，宿主 `Add(500..599)` 即可过 mcu 模式那道门（漏了这一步的现象是
「处理器注册了却永远不被调用」）。

绘图窗口页按**游戏机手柄**排布：左十字方向键、中 SELECT/START、右 X/Y/A/B 菱形。键码映射到
自然键盘等价键（A/B/X/Y 即字母键，START=回车、SELECT=Shift），同一份程序接物理键盘也能玩。

顺带修掉光栅层一个真 bug：`Canvas.SetPixel` 是**直接覆盖**而非 alpha 混合 ⇒ 「全透明填充」不是
不画，而是把画布**打了个洞**，显示端透出宿主背景（亮色主题下就是白底），表现为空心图形内部整片变白。
改为 source-over（不透明走原快路径，**既有渲染产物逐字节不变**）。

### ④ 移动端配色统一（去掉品牌蓝）

按钮全部改为**中性随主题**：暗色近黑底白字、亮色近白底黑字，选中态用**反色**（不需要第二个强调色）。
全局 `Button` 样式 + ChatPage 圆形按钮 + ModelPicker/ProviderModels 的切换键一起改，配色对收敛到
`MauiUi.ToggleColors`（唯一真源）。

### ⑤ Agent 轮次上限 50 → 200

50 太短：一次「clone → 改代码 → 编译调试 → 提交」就会撞上限而中途停下，需用户手动「继续」；
排查一个编译器行为问题也能花掉 30+ 轮。

### ⑥ 自测

5717 → **5744 条全绿**。新增断言里有几条是真正的护栏（去掉修复立刻变红）：
`cd` 跨 async 派发边界生效、丢失作用域时回退进程默认目录（用不流经 ExecutionContext 的线程池回调
忠实模拟）、`cd` 拒绝切出沙箱、clone 拒绝覆盖已有仓库且 origin 未被改写、凭证不跨 host、
status 列表封顶、消息队列连投两条不漏、**场景渲染后逐像素验**（空心图形内部必须是背景色 ——
这条正是抓住「透明填充被画成实心」的那一条）。

### 未完成（如实记录）

- **566/567 可用绘图区返回垃圾值**（实测 `SCR_W=39574400`）：纯计算部分有 4 条断言钉着，
  说明问题在**喂进去的设备参数**（`DeviceDisplay.MainDisplayInfo` 的单位），下一步加诊断打原值即可定位。
- **绘图区纵横比被拉伸**：`Image` 的 `AspectFit` 放在 `Orientation="Both"` 的 `ScrollView` 里不保纵横比。
- **C 侧封装库**：C 的 `asm("SYSCALL #N")` 不会把参数放进 R0..Rn，寄存器约定需真机实验确定后才好发头文件
  （这解释了「对话框字符串大多是空的、只有一个 hello」的现象 —— 汇编那条路已验证字符串完全正确）。
- 号段内 526/527/528/530/531、560–562、563/564、565 尚缺真机端到端证据（代码路径有自测覆盖）。


## v0.96.158 (2026-09-15) — Windows 桌面版编辑器：闪退 / 定位尺子 / 键盘输入

MAUI 的 Windows（WinUI 3）目标此前只做到「编译通过」，编辑器从没真跑起来过。这版把它跑通，
四个问题都是**实测定位**、不是照猜改的（诊断日志落在 `%LOCALAPPDATA%\WayCoder.Maui.Diag\diag.log`）。

### ① 打开编辑器直接闪退 —— Win2D 没有 `DrawText(IAttributedText)` 实现

反编译核对：`Microsoft.Maui.Graphics.Win2D.W2DCanvas.DrawText(IAttributedText, …)` **只有一句
`throw new NotImplementedException()`**。异常从 Win2D 的绘制回调逃出去，被 WinUI 包成 stowed
exception（事件日志 `0xC000027B`，故障模块 `Microsoft.UI.Xaml.dll`）⇒ 进程当场终止。

三处文本绘制（正文色段 / 超长行 / 行号栏）在 Windows 下改走唯一实现了的 `DrawString`：正文
**逐色段自绘**，x 用与光标/命中测试同源的 `AdvancePrefix`；y 语义补一个字号（Win2D 的
`DrawString` 把文字顶边放在 `y - FontSize`，而 `DrawText` 是左上锚定）。

### ② 汉字后面的文字错位 —— 尺子与画笔不同源

`GetStringSize` 在 Win2D 后端返回的数**既不是 0.5em 也不是 1em**：字号 14 时实测 `a=6.12`、
`中=10.61`，连比例都不对；而渲染走的 `CanvasTextLayout` 量出来是**精确的 7.00 / 14.00**。
定位（逐段绘制 x、光标 x、点击命中）全走这把错尺子 ⇒ 汉字之后越走越偏。

改用**与渲染同引擎**的 `CanvasTextLayout.GetCaretPosition(1).X` 量推进量，字体自检随之从 ❌
变 OK。同时纠正一条旧结论：Windows 上「字体回落」是**误判** —— 自检当时也用了那把错尺子，
量什么字体都返回同一个错数。

### ③ 上下键不动光标 —— 只读态页面上没有任何可聚焦元素

WinUI 里画布不可聚焦，而只读态那个「输入框代理」是隐藏的 ⇒ **按键没有任何东西收得到**。
现在只读态让它以 1px 透明形态保持可见并持有焦点，并接 `PreviewKeyDown`：↑↓/PageUp/PageDown
跨行（**保留目标列** —— 短行上夹住但不改写，走回长行能回到原来那一列）、←→/Home/End 横向移动。
只读态以前**根本没有光标图形**（只有一层 4% 白的行底色），现在画一根不闪烁的竖线。

配套三条 Windows 专属坑：

- **焦点必须等 `Loaded`**：Handler 刚建好时控件还没进可视树，此刻 `Focus()` 返回 false。
- **禁掉 `BringIntoViewRequested`**：聚焦这个 1px 代理时 WinUI 默认把它滚进视野，连带把整页
  滚一下 —— 表现就是「点一下光标闪一下就没了」。
- **只读态置 `IsReadOnly`**：能打字的输入框会招来输入法，光标浏览时候选框直接弹在正文上。

### ④ 打字完全进不去 —— 点画布把焦点抢给了 ScrollViewer

点画布进编辑态时，WinUI 会把焦点给页面里的 `ScrollViewer`，**而这步晚于我们的点击回调** ——
它覆盖掉 `BeginEditLine` 里那次同步 `Focus()`；输入框随即失焦，而 `Unfocused` 是**立刻
`CommitEditingLine()`**，编辑态于是被自己拆掉（输入框隐藏、`_editLine=-1`），此后按键全落空。

修法：失焦提交**延后一拍**（这一拍内焦点回到输入框就撤销提交），聚焦在下一拍**再要一次**。
（两处都是 `#if WINDOWS`，Android/iOS 仍是原来的即时提交。）

### ⑤ 工具栏图标不显示

`Source="icon_undo"` **少了扩展名**：Windows 上 MAUI 直接拿这个名字去 `ms-appx:///<名>` 找文件，
而 resizetizer 的实际产物是 `icon_undo.scale-100.png`（Android 的资源查找不看扩展名，所以手机
一直是好的）。7 个静态图标 + 1 处运行时赋值（`EditBtn.Source = "icon_edit" / "icon_lock"`）
一并补 `.png`。

### ⑥ Tab / Shift+Tab：**全平台一套标准**

三件事一起做，四个平台（Windows / Android / iOS / MacCatalyst）行为一致：

1. **Tab 插的是真制表符**（`\t` 这一个字符），不是 4 个空格 —— 语义上必须是真制表符：
   Python 的缩进与三引号字符串里的制表符都有意义，换成空格是**改坏代码**，不只是改显示。
   视觉上仍是 4 列（展开由绘制侧 `ExpandTabs` 按 `TabColumns`=4 负责），删除也天然只删 1 个字符。
2. **Shift+Tab 退一级缩进**：行首是制表符就删它一个，否则删最多 `TabColumns` 个前导空格；
   没有缩进可退时**原样不动**（空按一下不该改变文件）。
3. **载入待编辑行时不再 `ExpandTabs`**：原行为把行内真 tab 静默换成空格，一编辑就永久改写
   文件内容 —— 这是数据损坏，不是显示问题。

**键入口各平台不同，语义同一份代码**（`InsertIndent` / `DedentLine` / `ReplaceEditing` 放在
跨平台区域，不在任何 `#if` 里）：

| 平台 | 入口 | 为什么 |
|---|---|---|
| Windows | `TextBox.PreviewKeyDown` | 平台把 Tab 当**焦点导航**（AcceptsReturn=false 时移到下一控件），不截下来一按焦点就跑 |
| Android | `EditText` 的 `OnKeyListener`（`View.KeyPress`） | 同上；该回调**早于** View 默认处理，正好截住。软键盘没有 Tab 键，接外接键盘才用得到 |
| iOS / MacCatalyst | `UIKeyCommand` + 专用 `EntryHandler` | UIKit 里 Tab 是**命令键**（不走 `ShouldChangeCharacters`），而 key command 的 selector 必须由**控件自己的类**实现 ⇒ 只能给编辑器这一个输入框换成 `TabAwareTextField`（不动全局 Mapper） |

**验证**（Windows 实机，读存盘字节）：

| 操作 | 结果 |
|---|---|
| 行首按 Tab | 该行 tab 数 **1 → 2**（真 0x09，不是 4 空格） |
| 按一次 Shift+Tab | 该行 tab 数 **1 → 0** |
| 两次都存盘 | 文件字节确认，非仅屏幕 |

Android / iOS / MacCatalyst 三端**只做了编译验证**（0 错误），**运行未验** —— Android 需外接键盘、
iOS 需模拟器/真机接硬件键盘才能按到 Tab。

### ⑦ 移动端外观：启动背景 / 状态栏 / 编辑器菜单按钮

三条都是**系统级或布局级**的显式取值问题，MAUI 页面里的 `AppThemeBinding` 管不到：

1. **启动画面背景去紫**：`<MauiSplashScreen>` 没写 `Color` ⇒ MAUI 模板默认 `#512BD4`（那套紫）。
   补 `Color="#0E0E12"`（= 页面背景 `PageBgDark`），启动到界面无缝。
2. **状态栏配色**：原来由 `Platforms/Android/Resources/values/colors.xml` 的模板值决定
   （`colorPrimaryDark=#2B0B98` 紫）。改成**跟着明暗主题走**：
   `values/colors.xml` 浅色用 `#FFFFFF`（Android 在浅色模式给状态栏配深色图标，底色必须是浅的
   才看得见）、`values-night/colors.xml` 深色用 `#0E0E12`（配浅色图标）。`colorAccent` 取 App
   自己的 `Primary #4A6CF7`，免得控件强调色跟着变。
   ⚠ 一开始只改成深色一档，模拟器（浅色模式）里 **深色图标压黑底几乎看不见** —— 这才补的 night 资源。
3. **编辑器菜单按钮上移一行**：`☰` 原来和文件名挤在内容区第一行（方块比标题栏还高，视觉上像压在
   工具条上）。改为放进 `<ContentPage.ToolbarItems>`，即 Android 标准的**顶部动作栏**位置，
   文件名那行整行让给文件名（长路径不再被按钮挤掉）。

**验证**（Android 16 模拟器，实机截图 + 像素取样）：启动画面背景 `#0E0E12` ✓；深色模式状态栏
黑底白图标 ✓、浅色模式白底深图标 ✓；编辑器导航栏右上角出现 `☰`、文件名整行显示 ✓。
### 仓库维护

- `scripts/clean.ps1` / `clean.sh`：补上此前**漏掉**的 `bin2`/`obj2`（主 `bin/` 被运行中实例锁住
  时的旁路构建输出，实测占 89MB）、`*.apk`/`*.aab`、`__pycache__`、vscode-extension 的
  `out/`/`*.vsix`；并写死「刻意不碰」清单 —— `third_party/vml/Lib/**/*.vml` 是 2224 个
  **已跟踪**文件，vml 自带的 cleanup 会把它们删掉（那是上游语义，不是我们的）。
- 历史重写剔除了早期误提交的构建产物（`CoreCoderSharp/obj|bin` 492 个对象 + 旧 `waycoder.exe`
  + 根目录旧 `.deb`），`.git` 326MB → 150MB（余下约 95MB 是 apt 部署分支的 `.deb`，属正常内容）。
  **所有提交哈希已变** —— 克隆过的副本需重新克隆，`git pull` 会因历史重写报错。

### 仍未做（诚实标注）

- 随包的 Sarasa 字体在 Windows 上**取不到**（Win2D 把族名交给 DirectWrite 查的是**系统**字体
  集合）⇒ Windows 暂用系统 2:1 等宽字体（`NSimSun`，实测半角 7.00 / 全角 14.00 精确成立）。
  要真正用上打包字体得走 Win2D 的 `CanvasFontSet`（`W2DCanvas.Session` 是 public，可行但未做）。
- 编辑器页的**触控**路径（捏合缩放 / 长按选词 / 拖动扩选 / 选区操作条）在 Windows 上没验过。
- Android / iOS 上这些改动**只做了编译验证**（改动集中在 `#if WINDOWS` 分支，非 Windows 走的
  原路径未动），**实机未回归**。

## v0.96.157 (2026-09-15) — 补上 v0.96.156 欠的行为验证：网络真有往返、门控确按预期

v0.96.156 里我写明了「网络放行只做到编译通过、没验证实际连通」。这一版把那步补上，
**没有产品代码改动**，只有验证结论 —— 记在这里是因为上一条声称"未验证"的必须被结清。

### ① 门控逐条核对（读 R0 具体值，不是"没抛异常"）

`-102` = `SYSCALL_PERMISSION_DENIED`。宿主放行 `{330,334,335,336,337,338}` 前后：

| syscall | 不放行 | 放行 | 期望 |
|---|---|---|---|
| 330 SocketCreate | -102 | **-1** | 放行 ✅ |
| 337 SocketClose | -102 | **-2** | 放行 ✅ |
| 331 Bind / 332 Listen / 333 Accept | -102 | **-102** | 仍拒 ✅ |
| 320 Exec | -102 | **-102** | 仍拒 ✅ |
| 340 MkDir | -102 | **-102** | 仍拒 ✅ |
| 361 SetEnv | -102 | **-102** | 仍拒 ✅ |

**关键**：330/337 放行后返回的是 `-1`/`-2`（进了实现、因参数或状态失败），**不再是权限拒绝** ——
这才证明"门真的开了"。而 Exec 与绕沙箱的 OS 文件路径仍拒，等于把"我特意没放进
`HostAllowedSyscalls`"从"我相信"变成了有证据。

### ② 真实 TCP 往返（建 → 连 → 发 → 收）

本地起一个一次性 TCP 服务，VML 汇编走自己的 syscall 连上去：

| 步骤 | R0 | 含义 |
|---|---|---|
| 334 SocketConnect | **0** | `SUCCESS`，真连上 `127.0.0.1:18080` |
| 335 SocketSend | **4** | 4 字节 `PING` 发出 |
| 336 SocketRecv | **4** | **收到 4 字节** = 服务端回的 `PONG` |

### 仍未验证（诚实标注，别当已完成）

- 上面两条都是在**桌面（macOS）**、且是 **VML 汇编直接发 syscall** 的前提下得的。
  **设备端**与**经 C 编译器编出来的程序**那条路还没跑过网络。
- `recv` 只验证了**字节数**，没打印出内容（测试里 `buf` 是单字节占位）。
- 桌面 CLI 那条路没动（网络放行只在 `MauiVml` 里）—— 它是 MAUI 专属。

## v0.96.156 (2026-09-15) — 手机端 VML 放开网络（只放客户端） + 命令行页按行 scrollback

### ① 33 处写死的门控收敛成「宿主可放行」

MCU 模式的拒绝写在**两个地方**：入口白名单 `UserAllowed`，以及 **33 个 case 里各写一遍**
`if (privilegeLevel > 0) { PERMISSION_DENIED; break; }`。麻烦在于 **socket 那一批（330+）
本来就在 `UserAllowed` 里** —— 第一道门放行、第二道门拦住，所以"放开网络"绕不开那 33 处。

收敛成一个判据：

```csharp
public HashSet<int> HostAllowedSyscalls { get; } = new();
private bool PrivilegeDenied(int syscallNum)
    => privilegeLevel > 0 && !HostAllowedSyscalls.Contains(syscallNum);
```

不收敛的话「哪些 syscall 在 MCU 下可用」散在 33 个 case 里，加一条放行要改其中某一处，
**而漏改不报错** —— 只是那条永远不生效，现象是「我明明放行了却没反应」。

### ② 只放客户端那一半，另外三类每条都有理由

| | |
|---|---|
| **放行** | 330 SocketCreate / 334 SocketConnect / 335 SocketSend / 336 SocketRecv / 337 SocketClose / 338 DnsResolve |
| **不放：服务端** | 331 Bind / 332 Listen / 333 Accept —— 手机是终端设备，一段 VML 程序不该在它上面开监听端口 |
| **不放：目录与文件** | 340 MkDir / 341 Remove / 342 Rename / 343 ReadDir / 344 Stat —— ⚠ 它们走 `VMLRuntime.Syscall.OS.cs` 那条 OS 实现，**不经过 v0.96.155 设的 `FileSystemRoot` 沙箱**；放行等于把刚立起来的文件沙箱拆掉一半。要放行必须先把它们也接上沙箱 |
| **不放：决定性危险** | 320 Exec（`Process.Start` 起任意进程）、370/371 DLOpen·DLSym（FFI）、361 SetEnv |

**Exec 那条是关键**：它同样在 `UserAllowed` 里、同样有 case 门控 —— 只是**不在** `HostAllowedSyscalls`
里，所以维持封禁。这也是**不选「切 OS 模式」**的原因：`mode: "os"` 零改动就让 330+ 全通，
但它同时放开 Exec，还会激活中断/定时器子系统、并让文件 syscall 走那条绕开沙箱的 OS 实现 ——
等于把文件沙箱拆一半。

### ③ 命令行页 scrollback 改按行

原按字符数上限（12 万字符），改 `MaxScrollbackLines = 256`（**行**），超了从最老的**整行**丢。
终端里"滚出去"的单位本来就是行，按字符裁会把一行从中间劈开、留一条断头的半行。
保留半行语义：末尾没换行的那段标 `_partial`，与上一段拼接而不是另起一行。
（顺带删掉按字符切的 `TrimHead` 及其 UTF-16 代理对保护 —— 按整行丢天然安全。）

### 固件与验证状态

- patch 重新生成为 `third_party/vml/patches/0001-local-adaptations.patch`（210 行，含上述两组适配），
  已验证「反向应用 → 重放 → 幂等」三步；README 本地适配表随之补行
- 构建 0 错误。**但要如实说：本版的行为验证还欠着** ——
  · 网络放行只做到编译通过，没跑过实际 socket 连接，也没验证 331/320 确实仍被拒
  · 256 行 scrollback 的裁剪点没实测过（想用 `seq 1 300` 造超长输出，但 `uiautomator`
    回读不到 300 行长的 Label 文本）

## v0.96.155 (2026-09-15) — 命令行页接上 C 等 22 种语言：编译链路口径错 + 文件沙箱 + 交互式 stdin

承接 v0.96.153：上一版只通了纯 VML 汇编（`vml test`）。这一版把「一套工具编所有类型文件」
真正接进 App —— **实测 Android 上 `vml run hello.c` → `HELLO-C-OK`**，`t.py`/`t.lua`/`t.js`
同一份代码自动派发、各自跑通。

### ① C 编不过的真因：三处口径错，全在 `MauiVml.CompileAndRun`

| 错在哪 | 症状 |
|---|---|
| `VML_HOME` 从没设过 | 前端产物里的 `.linked "Lib/c/builtin.vml"` 是**相对路径**，链接器解析它要依次试「父库目录 → `VML_HOME` → CWD → 搜索路径」。手机上 CWD 是用户 workspace、不是 VML 根 ⇒ **整条标准库链断掉**，运行期报 `未找到标签: shared_puts`。上游 CLI 不设它是因为它"从 VML 根目录运行"，我们没有那个奢侈 |
| 把**目录**当 `LibraryPaths` 传 | 链接器会把目录下**每个** `.vml` 全量挂上：同一个 hello.c，**给目录 93423 条指令、给库文件 36261 条**（上游 35329）。上游为此专门留了注释：「仅添加已解析的库文件，不添加目录路径（避免全库链接）」 |
| 空清单传给 `LinkLibraries` | 它**首行就早退**（`if (Count == 0) return mainProgram;`），等于链接器一个字不干，且**不抛异常** —— 错误一路拖到运行期 |

修法是照抄上游口径；库清单**不自己硬编码**（那是"平行表"），调上游的 `VmlToolConfig.ResolveLibs()`
读 `vmltool.config.xml` 的 `DefaultLibs` + 各语言 `Libs`。

### ② `isAssembly` 判据写反 —— 两个症状都极具误导性

原判据 `string.IsNullOrWhiteSpace(source) || ext == ".vml"`（「没有源码就是汇编」）**正好是反的**：

- 内联汇编（`vml test`，source 非空）→ 判成**编译** → `CwdContext.Resolve(null)` 抛 `ArgumentNullException`
- `.c` 文件（source 为空）→ 判成**汇编** → **C 源码被喂给汇编器**，产出空程序、不报错 ⇒ 用户看到的是「程序正常运行，就是没输出」

两者都不是"参数报错"，而是各自跑到别的分支上，所以特别难看出来。
**修法不只是改方向，而是把派发抽出工具**：`MauiVml.Run(source, filePath, timeout, readLine)`
是派发的**唯一实现**，`VmlTool`（AI 调用）与命令行页（用户敲）共用 —— 派发是流水线的属性，不是工具的属性。

### ③ 文件沙箱（vendored 的第一处源码级适配）

`ExecuteFileOpen` 原本 `new FileStream(fileName, ...)` **按进程 CWD 解析**：手机上那不是
workspace、还常常是只读目录 ⇒ 报 `Read-only file system`。更该防的是另一半 —— 它不总是
"写不进去"，也可能是**写到了别的地方**。

给 `VmRuntime` 加 `FileSystemRoot`：相对路径解析到它下面，**越界一律 `FILE_ACCESS_DENIED`**
（包含判断按**路径段边界**，裸 `StartsWith` 会让 `/srv/proj-evil` 通过 `/srv/proj` 的检查）。
全文件搜过 `new FileStream`/`File.` —— **只有这一处接用户给的路径**（`FileRead/Write/Control`
操作已打开的句柄，不再解析路径），所以只改一行调用点。

实测：相对路径进沙箱、`../evil.txt` 与 `/etc/hosts` 都返回 `-202`、`/tmp/evil.txt` 未被创建。

改动固化成 `third_party/vml/patches/0001-file-system-root.patch`，`sync.sh` 用 `git apply` 重放、
**打不上直接退出**（本地适配不能悄悄消失）。README 的本地适配表随之分为【A】csproj 属性与【B】源码级。

### ④ 交互式 stdin：一问一答，不阻塞主循环

`CaptureIo` 持有**可阻塞的** `Func<string>`；命令行页在 VM 线程上阻塞，同时亮出「⌨ 等输入」行，
用户填一行回车 → `TaskCompletionSource` 交回值 → 程序接着跑 → 输出区回显那行（像真终端）。
`VmlTool` 另加 `stdin` 参数（AI 可预置输入，按行喂、读完给空行）。

**一条硬约束**：VM 超时是 `Run()` 起就走的**墙钟** `CancellationTokenSource`，
**等用户输入的时间也算在里面** —— 手机上一行敲几十秒很正常。故 `RunProgram` 的超时上限
从 60 提到 600。真正的解法是"等输入时暂停计时"（要动 VML 的超时实现），**未做**，注释里标了。

### ⑤ 命令行页 scrollback 改按行

原按字符数上限（12 万字符），改 `MaxScrollbackLines = 256`（**行**），超了从最老的**整行**丢 ——
终端里"滚出去"的单位本来就是行，按字符裁会把一行从中间劈开、留一条断头的半行。
半行语义保留：末尾没换行的那段标记 `_partial`，与上一段拼接而不是另起一行。

### 基础设施（顺手补的）

- `scripts/make-vml-lib.sh` —— 原先 `vml_lib.zip` 是**手工打的**；现在脚本化并补上
  **`vmltool.config.xml`**（少它整个链接阶段会被跳过）
- 库重解压改用 **zip 内容指纹**判断，不再靠人记得改版本常量
- 前端自检补「残留 `#include`」反判据（旧的只看 `.text`/`.entry`，会放行半成品）
- `VmlTool` 的 catch **带上堆栈** —— 正是它原先只回「类型: 消息」让我第一轮只能猜 AOT；
  移动端 AOT/裁剪会把异常资源键原文漏出来（`ArgumentNull_Generic Arg_ParamName_Name, path`），光看那句判断不了是哪个 API 抛的

### 两条排查记录（都很贵）

1. **「设备上 C 能编能跑但零输出」的两次误判**：先怀疑 AOT/裁剪（Release 特有），
   实际真因是上面 ② 的判据写反 + 我建的 `hello.c` 是 **0 字节** ——
   `echo$IFSaW50…` 里 `$IFSaW50…` 被 sh 当成未定义变量展开成空，`echo` 没参数、`base64 -d` 解了个空输入。
   `wc -c hello.c` = `0` 是转折点。**「$IFS 后面紧跟字母会被并进变量名」，要写 `${IFS}`。**
2. **「设备上 shell cwd 是只读」也是我搞错的** —— 那是 adb 的引号问题（重定向被 adb 自己的 shell 吃掉），
   App 的 shell 一直正常、`cwd` 就是 workspace。

## v0.96.153 (2026-09-15) — VML 并入本仓库：手机端进程内跑编译器 + 运行时

用户：「vml 要怎么做才可以在手机端 shell 里面使用？」→ 摸底后**问题性质变了**，
最终按「**源码复制进本仓库**」并入，见 `third_party/vml/README.md`。

### 关键判断：不该编成可执行文件丢进 shell，而该链进 App

VML 是**纯 C# / .NET 10**（106 个 .cs / 34K 行），OS 依赖收在极窄的接缝里
（`VMLRuntime.Syscall.OS.cs` 511 行，唯一逃生口是一处 `Process.Start`）。

进程外那条路三条全踩：① **iOS 的 `fork/exec` 被沙箱物理拒绝** —— 永远上不了 iOS；
② Android 10+ 的 W^X 让 app 私有目录里的文件不可 exec，得塞 `jniLibs` 当 `.so`；
③ 自包含 .NET 运行时几百 MB。托管代码链进来，这三条全没有。

**实测（Android 16 模拟器）**：命令行页敲 `vml test` → `Hello, VML!`。
汇编器 + 虚拟机全部在 App 进程内跑完，**没起任何进程**；APK 只涨 **212 KB**。

### 四处本地适配（`third_party/vml/sync.sh` 会在同步上游后重新施加）

vendored 副本里改了 25 个 csproj，**不改就编不过**：

| 适配 | 不改会报 |
|---|---|
| `OutputType` Exe → Library（25 个）| **NETSDK1150**：非自包含的可执行文件不能由自包含可执行文件引用 |
| 去掉 `StartupObject`（19 个）| **CS2017**：生成模块或库时无法指定 /main |
| 去掉 `RuntimeIdentifiers`（24 个）| **NETSDK1047**：它的列表里没有 `android-arm64` |
| 去掉 `PublishAot`（25 个）| AOT 发布要求 RID；我们只要它们的代码 |

其余文件**一行未改** —— 改动越少，`rsync` 覆盖式同步越干净。

第一处和第二处是**连锁**的：为了能被自包含应用引用而把 Exe 改成 Library，就撞上了 `/main` 冲突。

### NETSDK1047 的根因（值得记）

那 22 个前端编译器是纯 `net10.0` 项目，而 MAUI Android 带着
`RuntimeIdentifier=android-arm64` 的全局属性流进 `ProjectReference`。
根因是 `CCompiler.csproj` 那类文件里写死的
`<RuntimeIdentifiers>win-x64;linux-x64;osx-x64;osx-arm64</RuntimeIdentifiers>` ——
**列表里没有 android-arm64**，加上 `PublishAot=true` 又强制要 RID。
两者去掉后，`VMLTool` 可以正常引用（它传递带入 22 个编译器 + 翻译器），
构建 0 错误、23 个编译器 DLL 进产物。

### 安全边界：**永远只用 `mode:"mcu"`**

`mode:"os"`（`privilegeLevel=0`）会放开 syscall 300-376：线程/互斥量、Socket/DNS、
mkdir/stat/readdir、以及 **Exec（syscall 320 → `Process.Start`）**。
MCU 模式下它们全部返回 `SYSCALL_PERMISSION_DENIED`。

模式是**宿主侧参数、VML 程序自己改不了** ⇒ `MauiVml` 里写死 `mode: "mcu"`，
那三层 OS/FFI 就是死代码。**没有去删 `Syscall.OS.cs` / `.FFI.cs`** —— 删了只是删死代码
（1.3K 行，在 63MB 的 APK 里可忽略），却要在 `sync.sh` 里加一步「同步后删文件」，
**那是最脆的一类本地适配**（上游一改文件结构就静默失效）。

### 三条移动端须知（写进 README，别凭直觉改）

1. **`AndroidLinkMode` 默认 `SdkOnly`** = 用户程序集不参与裁剪，VML 的反射路径靠这个才活着。
   谁改成 `Full` 或把 `TrimMode` 改成 `full`，都可能打断 `PluginManager` 的 `Assembly.LoadFrom`。改之前先跑 `vml test`。
2. **AOT 开着**（`RunAOTCompilation=true`）⇒ `Reflection.Emit` 不可用，影响面只有 FFI 那一层，
   且它自带 `RuntimeFeature.IsDynamicCodeSupported` 兜底 ⇒ 表现为「FFI 不可用」而**不是崩溃**。
3. **每次运行前必须 `DeviceManager.Instance.Reset()`** —— 单例，跨运行保留状态，不重置会串 MMIO 地址。

### 未完成（如实记，别当成已做）

- **接上编译器后还没在真机复验** —— 跑通 `vml test` 的那版只引了汇编器 + 运行时
- **高级语言编译入口未接** —— 命令行页现在只有 `vml test` / `vml run <文件.vml>`（汇编）；
  跑 `hello.c` 要接 `VMLTool` 的编译流水线（`CompileFileWithIncludes → AssembleWithIncludes → LinkLibraries → ApplyExports`）。
  注意 **`Compile(string)` 那个重载不链标准库**，少了流水线会「编译过了、跑起来找不到 stdlib 函数」
- **VML 尚未注册为 AI 工具** —— 用户提的用途是「让 AI 编译验证自己写的代码」，那步还没做

## v0.96.152 (2026-09-15) — 手机版有 shell 了：命令行页面 + 把桌面的 BashTool 接回 Android

用户：「当前的手机版，有了 ai，有了 git，有了编辑器，还缺个 shell，缺个编译器。」
编译器那块用他的 `vml` 项目后续集成，**本轮只做 shell**。

### 关键实测：Android 上 `Process.Start` 是**真实现** —— 这条决定了整个方案

桌面那个 `BashTool`（498 行，含 `BashGuard` 三层拦截、取消即杀进程树、超时迁移后台、
流式增量截断）**一行都没重写** —— 它只是被 csproj 排除在外、配了个「移动端不支持」的桩。
能不能复用的唯一疑点是 `Process.Start` 在 Android 上到底可用不可用。

查证方式（不是猜）：从 Android 的 Mono 运行时包里读
`Microsoft.NETCore.App.Runtime.Mono.android-arm64/…/System.Diagnostics.Process.dll`（126KB），
里面搜到 `<ForkAndExecProcess>g____PInvoke` 与入口 `SystemNative_WaitPidExitedNoHang`
—— 走的是 System.Native PAL 的 `fork/exec`，**是真实现**（纯抛异常的桩只有十几 KB）。
它唯一不支持的 `UseShellExecute`（程序集里有那句字面量）恰好没被用到（`BashTool` 写的是 false）。
⇒ 直接复用，**不需要给 Android 另造一套 Java `Runtime.exec` 的执行器**。

### 四处硬编码的 shell 路径（本仓库排第一的那类坑）

```
BashTool.cs:182 / PersistentShell.cs:97 / BackgroundTask.cs:112 / SandboxManager.cs:253
```
四处都是 `IsWindows ? "cmd.exe" : "/bin/bash"`。桌面三平台看不出问题，
而**手机上两个都没有** —— 只有 `/system/bin/sh`（mksh）。
收敛成 `WayCoder/Infra/ShellPath.cs`（`Resolve` / `BuildArgs` / `PersistentArgs`），
每条规则都拆成「纯函数 + 无参包装」两半，于是**三条平台分支在桌面上就能全部钉住**
（自测没法假装自己是 Android）。桌面行为逐字未变，自测有断言锁住转义规则。

`PersistentArgs` 单独一支：Android 必须传空 —— `--noprofile/--norc` 是 **bash 专有**的，
喂给 mksh 会以「未知选项」启动失败。

### 平台条件编译（本仓库**首次**用「按平台条件 include 源文件」）

Android 才编真 `BashTool`，`CoreStubs.cs` 的桩包 `#if !ANDROID` —— 两边同名同命名空间，
同时参与编译就是 CS0101。

⚠ 一个坑：把 `BashTool.cs` 从 csproj 的 Exclude 里**移走**是不够的 ——
`Include="../WayCoder/**/*.cs"` 那个通配会把它收回来，和条件 include 撞成
**NETSDK1022「重复的 Compile 项」**。正确姿势是**留在 Exclude 里**、只由条件 include 放行。

三平台全部 0 错误：Android 真实现 / iOS + MacCatalyst 桩
（iOS 的 `fork/exec` 被沙箱**物理拒绝**，没有配置能绕 —— 那是终点不是待办）。

### 新增「命令行」页面（第 5 个 Tab）

输出区（等宽纯文本）+ `$` 提示符输入框 + 历史按钮 + 运行，顶栏常显 `cwd`。
刻意**不做**真 TTY：手机上既没有 pty 也没有 terminfo，套上去就掉进「转义序列要自己解、
全屏程序要自己渲染」的无底洞；要的只是「能敲命令、能看输出、能知道自己在哪个目录」。
输出区**直接就是纯文本**（不做富文本着色）—— 移动端编辑器那轮踩过 ANR，
这里是那个「超大降级形态」本身，量级再大也不用再降。

走 `ExecuteUserShellAsync`（桌面 `!` 直通那条）：**用户自己敲的**语义，
跳过面向 AI 的黑名单，保留绝对红线。

`BashGuard` 另补一组 **Android 专有高危命令**：`pm disable` 能停掉系统组件
（含桌面/输入法，停错了手机进不去界面）、`am force-stop`、`settings put`、`svc`、`wm`、`input`
—— 桌面那几张表一个都没覆盖到它们。**只管模型发起的调用**；用户在命令行页自己敲的不拦。

### 实测（Android 16 模拟器）

| 项 | 结果 |
|---|---|
| `ls -l` | ✅ 真实输出，cwd = `/storage/emulated/0/waycoder/workspace` |
| `head -2 kbtest.txt` | ✅ **中文正常，无乱码**（`ProcEncoding.Apply` 非 Windows 自动 no-op，Android 本就是 UTF-8） |
| 错误路径 | ✅ `[stderr]` + `[退出码: 127]` 都显示 |
| 沙箱边界 | ✅ `cd /sdcard` 被拦（在 workspace 外），cwd 正确保持不变 |

**实测才暴露的真 bug：安卓输入法自动首字母大写。** `cd /sdcard` 被送成 `CD /sdcard`
⇒ `exit 127: CD: not found`。终端里这条是**致命**的（每条命令都会大写），
修法是 `Keyboard="Plain"` + 关纠错/联想。这类东西在桌面上永远测不出来。

⚠ 未验干净的一条：`cd` 的**成功**分支（cwd 真的变了）没演示成功 ——
该 workspace 下没有子目录可进，而用 `mkdir T && cd T` 时 `&&` 被 `adb shell`
的转义吃掉了（测试工具问题，非产品）。

## v0.96.151 (2026-09-15) — 原生跑 macOS（MacCatalyst）+ 配好 Windows 目标；顺带在 Apple 平台验掉了字体的旧账

用户：「继续使用 macosx 来测试」「还可以生成 window 版本吧」。

### macOS：加一个 TFM 不够，还有三处平台守卫

`maui-maccatalyst` 工作负载装好后加了 `net10.0-maccatalyst`，但**构建立刻挂在 TUI 类型上** ——
三处 `#if ANDROID || IOS` 在 Catalyst 上落到 `#else`（桌面 TUI 那条路）：

- `UI/CLI/Commands/InitCommand.cs` —— `Program.RunWithUiLoop` / `ChatScreen.StartAgentMsg` / `FinishAgentMsg`
- `UI/CLI/Commands/ReviewCommand.cs` —— `ReviewMode`
- `MauiProgram.cs` —— 内层 `#elif IOS` 在 Catalyst 上留了个**空块**（透明文字那位）

三处补成 `|| MACCATALYST` 后原生跑起来（App Sandbox 开着，workspace 落在容器里，与移动端语义一致）。
`ANDROID`/`IOS` 在 `||` 链里照旧短路命中，两端的既有分支**逐字未变**。

### 一条**之前只在 Android 上验过**的结论，这次在 Apple 平台钉住了

造了 5 个**正好 1000 显示列**的测试文件（1000 数字 / 1000 拉丁 / 500 汉字 / 500 emoji / 混排），
逐个打开读状态栏的最大横向滚动 —— **五个全部报 `X0/6731`，分毫不差**。

这一条同时证明两件事：① **内嵌 Sarasa 在 Apple 平台上加载成功** ——
这正是 iOS「光标对不上位置」的根因（`CanvasFontName` 要 PostScript 名），当时只验了 Android 一支、
iOS 从没在运行时验过；字体一旦回落成**比例字体**，汉字就不可能是拉丁的**恰好 2 倍**。
② 宽度模型精确：汉字/emoji 占 2 列这条网格与字体设计天然对齐。

### 一次我自己的误判，写下来免得重犯

在 Catalyst 上用 `osascript click at` **合成点击**点画布，光标不动。
我一路排查（按钮点击有效、事件挂载无条件、`IsEnabled` 无人动过、探针确实编进程序集、
并在 `Draw` 里放一条日志证明 Console 通路正常），最后定性成「`GraphicsView` 在 Catalyst 上收不到鼠标」。
**结论是错的** —— 用户拿真实鼠标一点就正常。

教训：**合成输入驱动不了的东西，不等于那东西坏了**。判据应该是「同一个合成点击能不能驱动
同窗口的别的控件」——能，只能说明「差异存在」，**不能**说明差异在谁身上。
（这条与 v0.96.150 那条「先看图再动手」是同一类错误的两面：都是**把推断当成了实测**。）

### Windows：MAUI 只能在 Windows 上构建，但 Avalonia 那版可以

官方确认 **WinUI 3 的构建链（XAML 编译器 / MakePri / WindowsAppSDK 任务）是 Windows 专有的**，
macOS 上连评估那个 TFM 都会失败。所以：

- **MAUI 的 Windows 目标**：加了**按宿主 OS 条件**的 TFM（`IsOSPlatform('windows')` 才追加
  `net10.0-windows10.0.19041.0`），本地构建流程一个字不受影响；另把三处平台守卫补上 `|| WINDOWS`，
  并把 `EditorTypography` 的 Windows 分支指向**族名**（资产名/PostScript 名分别是 Android/iOS 专有的解析器）。
  **出包要在 Windows 机器或 CI 的 `windows-latest` 上做** —— 本机验不了，这条如实标注了。
- **Avalonia 的 `WayCoder.Gui`**：标准桌面工程，**在这台 Mac 上直接交叉发布出了 Windows 版**
  （`dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` → `waycoder-gui.exe`，113MB）。
  这是「现在就想要一个 Windows GUI」的那条路，代价是它和 MAUI 是两套 UI 代码。

顺带查过五个 `#else` 分支在 Windows 上会不会编不过：两处 `MauiBootstrap` 是 `return null/false`
的安全兜底、`GitSyncPage`/`SettingsPage` 是「当前平台不支持」的提示，**都能编过**；
真正会挂的只有 `InitCommand`/`ReviewCommand` 那类掉进 TUI 分支的。

## v0.96.150 (2026-09-14) — 软键盘挡住光标：Android 15+ 的 `adjustResize` 已经失效，改为自己接 IME inset

用户报：「点一条靠下的行，键盘刚弹出来就把光标挡住了，这时应该往上滚动露出光标。」

### 真因不是「没滚动」，是**窗口根本没被压矮**

`MainActivity` 上一直写着 `WindowSoftInputMode=AdjustResize`，注释里还专门写了「必须靠它」。
但真机 `uiautomator dump` 前后一比：**页面平台视图在键盘弹出前后都是 `(0,0)-(1080,2202)`**，
画布一点没变 —— 键盘只是盖上来，没有把布局压矮。

官方行为变更证实了这条：**Android 15（API 35）起，targetSdk ≥ 35 的应用强制 edge-to-edge，
`adjustResize` 不再缩放窗口**，它现在只负责「让应用能收到 IME inset」，剩下的要应用自己按
`WindowInsetsCompat.Type.ime()` 调整布局。所以这不是配置写错了，是那条路被系统封了。

### 改法：把「窗口被压矮」这件事自己还原出来

1. `EditorPage` 在**画布的平台视图**上挂 `ViewCompat.SetOnApplyWindowInsetsListener`
   （挂画布而不是页面：页面视图上已经有 MAUI 自己的安全区监听，覆盖它会连带弄坏页面的 inset 处理；
   画布是叶子视图，MAUI 不管它）。**inset 原样传下去、不消费** —— 它是窗口级的，
   吃掉会让别的控件一起失去内边距。
2. 拿到键盘高度后换算成**根布局的底部内边距**（`RootGrid.Padding`），于是画布真的变矮了。
   **选「压矮布局」而不是「在滚动数学里减去键盘高度」**：压矮之后画布的高度、命中测试、
   滚动边界、绘制范围全部照旧，只多一条下面的钩子；后者要同时维护「两个高度」，
   正是本仓库反复踩的「同一件事两处实现」。
3. `CodeCanvasView.OnSizeAllocated`：视口**变矮**时把光标行顶回视口（`ScrollToLine`，
   最小滚动 —— 露得全就不动），变高时只收口边界、不无端跳一下。

### 用户报的那句「刚好弹出键盘」= 触发点选错了

原来 `EnsureEditorVisibleAsync` 是「点完**等 260ms** 再滚一次」。键盘动画在 200~400ms 之间，
这个数是猜的：猜早了算的还是**旧视口**（那一行判定为可见 ⇒ 一个字都不滚，等于没做），
猜晚了用户已经看着自己被挡住 —— 正好就是「刚好弹出键盘时挡住」。
现在触发点是**真实的高度变化**，与键盘动画耗时无关，那条 `Task.Delay` 已删。

### 三条踩坑（都是「量了才知道」）

1. **`ime()` 不要再顺手扣掉导航栏**。网上通行做法是 `ime().bottom - systemBars().bottom`
   （官方文档也写了 ime「may include」导航栏），我照做了一版，**反而错了**：
   实测行号栏结束于 y=1453、状态栏 1453~1517、键盘上沿 **1517** —— `ime()` 报的就是 1517，
   本机**没**算进导航栏；扣掉 64px 后内容区被多顶上去一截，状态栏直接掉到键盘底下。
   **判断依据别靠肉眼看缩放截图**（我第一版就是这么误判的）：扫一列像素看行号栏底色
   `#F2F2F4` 在哪一行结束，就得到内容区的真实下沿。
2. **跨版本会压两遍**。`adjustResize` 只在 Android 15+ 失效；同一份 APK 装到 Android 14
   及更早的机器上那条老路照常生效，我们再补一次就是压两遍（编辑区被挤成一条缝）。
   判据**不写「系统版本 ≥ N」**（那是在猜系统行为），而是直接量：键盘弹出后页面
   还是满高 ⇒ 系统没管，我们补；已经明显矮了 ⇒ 系统管了，一个字不加。
3. 三处 Android 绑定细节：`WindowInsetsCompat.Type` 是**嵌套类型**（`var t = ...Type;`
   再 `t.Ime()` 报 CS0119，只能全限定写）；`GetInsets()` 返回**可空的** `Insets?`
   （直接点 `.Bottom` 报 CS8602，要 `?.Bottom ?? 0`）；`OnApplyWindowInsets` 的
   参数/返回在绑定里都是可空的（`null` 直接返回，否则 CS8767）。

### 验证（Android 16 模拟器，逐像素 + UI 树双读数）

| 场景 | 结果 |
|---|---|
| 点**靠下**的行（y=2000，光标落 L28） | 画布 `(0,537)-(1080,2126)` → `(0,537)-(1080,1453)`；状态栏完整可见；**L28 被滚进视口底部** |
| 点**靠上**的行（y=700，光标落 L15） | 画布同样变矮，但**首行仍是 12、一行都没滚**（L15 本来就在视口里）—— 最小滚动原则保住 |
| 返回键收起键盘 | 画布完整复位回 `(0,537)-(1080,2126)` |

内容区下沿与键盘上沿**严丝合缝**（1516 vs 1517），既没有多余空带、也没有元素被盖住。

## v0.96.149 (2026-09-14) — 查明「红色大光标」= 调试标尺（**无代码改动**）

用户报「输入框有个红色大光标」，截图一看：**是调试标尺**，不是输入框的光标。

### 那根线是什么

`CodeCanvasView` 里 `#if DEBUG` 的**调试标尺**：在「按推进量算出来的行尾」画一条竖线。
它 **1px 宽、`lineH * 3` 高、纯红**（`Colors.Red`）—— 视觉上和一根「大红光标」分不出来。
只在 `ShowDebugHud` 打开时画。

**关掉它的入口**：设置页 →「编辑器调试」开关（`SettingsPage.EditorDebugSwitch` → `MauiEditorStore.SetDebugHud`）。
关掉后红标尺与顶部那行性能 HUD（`47.6ms 峰109.7 X22/727 …`）一起消失。

它的使命已完成（把「测量与渲染差多少」从目测变成可量，前几轮定位偏差就是靠它定的性），
但**留着只会造成误解**，下轮应删掉（用 `grep -n` 定位后逐行 `sed` 删）。

### 一条要记的教训：**先看图，再动手**

这一轮我先按「输入框的光标」做完了 v0.96.148 的四项改造（1px / 钉死顶部 / 关平台光标），
**之后才去截图看**，发现用户实际看到的是标尺。那四项本身无害
（`SetCursorVisible(false)` 对原生光标实打实有效），但**解决的很可能不是用户实际看到的问题**。

「用户报的现象」与「我推断的原因」之间，**差一次截图**。这类视觉问题尤其如此 ——
下一个动作应该是取证，不是改代码。

### 另：一次差点写坏文件的操作（已还原）

删标尺那次用 python 按行号切分文件，脚本打印 `7 行 → 10 行`（2585 行的文件不可能只有 7 行），
但 `git diff` 显示 **0 deletions** —— 删除静默失败、只插进了一段注释。
`git checkout` 已还原，工作区干净。

**教训**：改核心大文件时，那种「按行号切分再拼回」的脚本，**必须校验切分结果的行数**
（`len(lines)` 与 `wc -l` 对不上就立刻中止）。这次是运气好没写坏。

## v0.96.148 (2026-09-14) — 输入框变成纯「键盘代理」：钉死顶部 + 1px + 关掉自带光标

用户实测**能看到输入框自带的光标（红色竖条）冒出来乱跳**。方案（用户定的）：
把它变成一个纯粹的「换出软键盘」代理，任何原生视觉都不要。

### 四项改造

| 项 | 做法 | 原因 |
|---|---|---|
| 位置 | **钉死在顶部**（`TranslationY = 0` / `Margin = 0`） | 跟着文字走反而有害：原生光标会冒出来，且每帧挪它要更新原生控件布局、长列表会掉帧 |
| 高度 | **固定 1px**（XAML `HeightRequest`/`MaximumHeightRequest`）| 不用 0 —— 怕平台侧算布局时报错 |
| 背景/高亮 | 平台侧设成**透明** | 去掉下划线与选中高亮 |
| **自带光标** | `EditText.SetCursorVisible(false)` | **就是那条红色竖条**；文字与光标全部由画布自绘，原生残留只会打架 |

平台侧的处理挂在 `LineEditor.HandlerChanged` 上（`Handler` 是懒创建的，构造函数里还拿不到）。

### 前置条件的修正

这条能成立，是因为用户澄清了一个事实：**那个 `Entry` 只用来换出软键盘，
自带光标一概不用、内容全透明** ⇒ 它浮在屏幕哪里都不影响观感。
所以 v0.96.146 里「每帧重定位」那套不但多余还有害，v0.96.147 已撤。

### 未验证

真机行为没验：自带光标是否真的不再冒出、软键盘是否照常弹出、滚着是否能继续打字。

## v0.96.147 (2026-09-14) — 修正 v0.96.146：那个浮动输入框根本不用管

v0.96.146 里我为「滚动时输入框会错位」做了两件事：每帧 `PositionEditor` 重定位、
滚出屏幕就 `IsVisible = false`。**两条都是多余的，第二条还有害**。

用户澄清了关键事实：**那个 `Entry` 是全透明的，只用来换出软键盘，自带光标一概不用**
⇒ 它浮在屏幕哪个位置都不影响观感，只影响「输入法候选窗浮在哪」（纯外观）。

于是：

- **撤销「每帧重定位」** —— 那正是 B 方案的掉帧代价，不必要
- **撤销「出屏就隐藏」** —— `IsVisible = false` 会让它**失焦、软键盘收起来**，
  正好破坏我们要的「滚着也能打字」
- **保留「滚动不结束编辑」** —— 这条才是真正要的

净结果：改动收敛成一条，**零每帧开销**。光标自绘跟着文字走，滚出屏幕自然看不见，
滚回来还在；输入框该在哪在哪，不影响任何视觉。

## v0.96.146 (2026-09-14) — 推翻「滚动即结束编辑」：光标跟着文字走

### 旧规矩为什么可以废掉

原来 `EditorPage` 里有一行 `ScrollingStarted += () => CommitEditingLine()` ——
**一滚动就结束编辑**。它的动机是：光标当初靠一个原生控件叠在画布上定位，
一滚就对不上，索性滚走。**现在光标是画布自绘的**，跟着文字一起画，
这个顾虑根本不存在了。

### 改法（选的是「滚动时同步移动输入框」这条）

新版规矩（用户定的）：

- **滚多远、缩多少，光标都还在它该在的位置**
- 滚出屏幕 → 自然看不见（画布只画可见行，不需要专门隐藏）
- 滚回来 → 还在
- 缩放不影响 ✓（缩放只改字号，布局重算后光标自动跟着走，这条本来就成立）

三处改动（`EditorPage.xaml.cs`）：

1. 删掉 `ScrollingStarted → CommitEditingLine`（滚动不再结束编辑）
2. `ViewChanged` 里补一句：编辑中就 `PositionEditor(_editLine + 1)` ——
   把**浮动的原生输入框**挪到编辑行的新位置。`PositionEditor` 本来就两个轴都管
   （`TranslationY` 纵向 + `Margin.Left - Canvas.ScrollX` 横向），不用改
3. `PositionEditor` 在 `LineScreenY` 返回 -1（行滚出屏幕）时**把输入框藏起来**
   —— 原来只是 `return`，输入框会**停在屏幕边上压着别的行**

**为什么必须管那个输入框**：光标自绘没问题，但编辑态还浮着一个原生 `Entry` 负责
IME / 软键盘 / 系统复制粘贴，它靠 `TranslationY` 钉在编辑行上。画布滚了它不滚，
输入的字就会落到错误位置 —— 这多半正是当初「滚动即提交」的动机。

### 代价（选 B 时已知）

滚动/缩放的每一帧都要更新一个原生控件的布局，长列表滚动时可能掉帧。待实测。

### 未验证

**真机行为没验**：滚动光标是否跟着走、出屏是否消失、滚回来是否还在，三件都要实测。

## v0.96.145 (2026-09-14) — 编辑器的滚动改成「屏幕适应光标」（最小滚动）

### 问题

点击后光标所在行/位置会**自动居中**，屏幕莫名跳一下 —— 用户的原话很准：
「应该是**屏幕适应光标，而不是光标适应屏幕**」。

### 两处根因

| 方向 | 位置 | 原因 |
|---|---|---|
| 纵向 | `EditorPage.EnsureEditorVisibleAsync` | 进编辑态时两次都传 `center: true`，把行拽到屏幕中部 |
| 横向 | `CodeCanvasView.EnsureCaretVisible` | 判定边距 `margin = 48f` ≈ 2.5 列，**离边缘不足 48px 就滚** |

### 改法（都是最小改动）

**纵向**：`ScrollToLine(oneBased, center: true)` → `ScrollToLine(oneBased)`（`center` 默认 false）
—— `EnsureVisible` 本来就是**最小滚动**（只在看不见时才挪），居中反而把「本来就在屏幕上」的行也拽走。

**横向**：`margin = 48f` → `EditorTypography.HalfWidth`（一列）。
原来那两行逻辑本身是对的（超出右边缘才滚、超出左边缘才滚），**问题只在边距太大**。
改成一列后语义正好对应用户定的规则：

- 光标格被左边挡住 → 右滚到刚好露出
- 光标格右边不够放一个字符 → 左滚一格
- **中间的字点了 → 一个字都不动**

### 没做

`GoToLineAsync`（跳到行）与查找/大纲跳转仍保留居中 —— 那类「主动跳转」需要目标周围的上下文，
跟「点一下」是两种语义。如果也想去掉，说一声。

## v0.96.144 (2026-09-14) — 「点哪儿光标去哪儿」的真根因：正在编辑的那一行，手指点的是**平台的输入框**，不是我们的画布

症状（用户原话）：「我手点的位置，和定位位置有较大偏差，**特别是靠右边的时候很明显**」，
而且偏差会跳变（同一行连着点 4 下，落点相对手指是 +3.2 / -1.8 / -1.8 / -2.3 格，非单调）。

**根因在 XAML 的声明顺序**：`EditorPage.xaml` 里 `CodeCanvasView` 声明在**前**、浮动的
`Entry`（正在编辑的那一行）声明在**后** —— MAUI 的 Grid 子元素**后声明者在上层**，
于是那个 Entry 是**盖在画布上的**。它是个真的 Android `EditText`：

* 点它的触摸被它接走，**画布根本收不到**（实测：在编辑行上点 5 下，画布自己的触摸日志
  **一条都没有**，而点别的行立刻有）；
* 「横坐标 → 字符下标」由**平台自己的排版**决定，还叠加**它内部自己的横向滚动**
  （它的滚动随光标位置变），与画布这套「逐字形推进量」的网格毫无关系；
* 两者每格只差不到 1px，但**沿行累积**，到第 80 列就是一整格；再加上它内部滚动带来的跳变，
  于是「越靠右越明显」且不单调 —— 现象与实测完全对上。

**修法**：把 `Entry` 挪到**画布之前**（= 压在画布底下）。触摸从此全归画布，点哪儿由
`CharIndexAtX` 一把尺子决定；那个 Entry 仍能被 `Focus()` 聚焦，**IME / 软键盘 / 剪贴板照常**
（它本来就只负责这三件事），而它的**系统光标、选择手柄、放大镜也被不透明的画布一并盖住**了。
配套两条：同一行分支里画布光标**直接取我们算出的列**（不回读平台值）；`SyncCaret` 轮询现在
只管**输入法改光标**（打完一个字往右挪那类），手指点位置不走它。

**顺带修掉一个自己发现的越界**：`EnsureCaretVisible` 里两式直接给 `_scrollX` 赋值、**没有边界**，
点一次行尾就永久「滚过头」（HUD 实测 `X3504/3412`，屏幕上留一段空白）；现在收口到 `ClampScroll()`。

**验证（用户手机 · 字号 37 · 真实栅距 51.0px，脚本 `scripts/_taptest.py` 闭环：点 → 截屏 → 量洋红三角）**：

| 手指 | 画布解出的列（日志） | 光标预测屏幕 x | 像素实测光标 x | 与字形格的关系 |
|---|---|---|---|---|
| 200 | 184 | 198 | 197.5 | 落在字与字之间（+0.0px） |
| 400 | 188 | 402 | 401.5 | 落在字与字之间（+0.0px） |
| 600 | 192 | 606 | 605.5 | 落在字与字之间（+3.0px） |
| 800 | 196 | 810 | 809.5* | 落在字与字之间 |
| 1000 | 200（行尾） | 966 | 966 | 行尾，文字外缘 ✓ |

\* 点完会自动把光标带进视野、视口跟着挪，截屏量到的是挪过之后的 761.5 —— 补上滚动量正好 809.5。

**打字链路也一并验了**（改动把输入框挪到了画布底下，必须确认 IME 没被挡掉）：点 x=605 → 列 8
（日志 `col=8/12`）→ 输入 `Z` → 屏幕变成 `AAAABBBBZCCCC`、光标紧跟其后落在间隔里 →
点保存 → 手机上的文件内容就是 `AAAABBBBZCCCC` ✓。


## v0.96.143 (2026-09-14) — 光标压字的最终根因（在用户手机上量出来的）

前面几版都在猜「小数号 vs 整数号」，用户一句「**和字体整数无关，整数字号也还是对不齐**」
把方向纠正了。这次直接在**用户手机上**量，一次就锁死了：

| 量到的 | 值 |
|---|---|
| 真实栅距（一行 19 个 H，相邻墨迹中点差全一致） | **51.000 px** |
| 设计值 `37 × 0.5 × 2.75` | 50.88 → 平台**取整 51** ✓ |
| HUD 报的 `_charWidth`（上一版的吸附结果） | 18.91dp = **52.00 px** ✗ |

**平台的绘制规则是：每个字形的推进量 = `round(字体设计值 × 屏幕比例)` 个设备像素。**
而 `GetStringSize` 报的是排版算出来的小数，并且**实测比设计值偏大**
（这台机器上 37 号报 18.8dp，设计值 18.5dp）。我上一版**拿这个偏大的实测值去吸附**，
就吸出了 52px ⇒ **每字多 1px** —— 到第 10 列偏 10px、第 20 列偏 20px（半格 25.5px），
光标于是落进字格里。字号越大偏得越快，也解释了为什么「24 号看着还行、别的不行」。

**修法**：反过来 —— **从设计值算、吸到设备像素网格**：
`_charWidth = round(HalfWidth × scale) / scale`（`HalfWidth = 字号 × 0.5`，本字体拉丁恰好 0.5em）。
比例用「屏幕物理宽 ÷ 控件 dp 宽」（本文件 `OnEnd` 的调试探针早就写着它**不等于** `MainDisplayInfo.Density`）。

**验证（用户手机 · 字号 37 · 真实栅距 51.0px）**：修好后 HUD 报 `w18.55` = 51.01px ✓ 与真实栅距吻合；
在 H 行点 4 个不同位置、用洋红三角定位光标 —— **4 次全部落在字与字之间**
（偏移 +0.0 / +0.5 / -0.5 / +0.0 px），压字 0 次。

同批另两条：进编辑态的**两条路**都要对齐字号（工具栏那支笔原先漏了，实测截图就是
「编辑模式开着、字号还是小数」）；吸附比例改用真实 scale 而不是密度。

## v0.96.141 (2026-09-14) — 光标修复实测通过 + 行号不再闪烁

### ① 光标修复：8 点扫描，0 次压字（这是量出来的，不是推断）

用不依赖字形留白假设的办法定格线（`H` 水平居中 ⇒ 格线 = 墨迹中点 − 半格），
在 H 行上点 8 个不同位置，逐帧量光标相位（`mod 18` 折到 ±9）：

| | 修复前（v0.96.139） | 修复后（v0.96.140） |
|---|---|---|
| 同一位置的相位 | **+8.5px（正好半格，压在字里）** | **+0.0px** |
| 8 点扫描 | —— | **全部 +0.0，压字 0 次** |

### ② 光标两端的调试三角**之前也在闪**（我自己写错了）

`DrawCaret` 开头写了 `if (!_caretOn) return;` ⇒ 闪烁的「灭」相位**整个函数提前返回，三角一起没了**
—— 而三角存在的全部意义就是「截屏时一定看得到光标」（用户实测反馈）。
现在只有那根竖线受 `_caretOn` 约束，三角恒亮。

### ③ 行号不再闪烁（用户：「不要为了提升一点速度，老是闪烁」）

v0.96.130 加的「视口在动的这一帧跳过行号数字」是**用可见的闪烁换一点时间**，
用户实测反馈「行号容易闪烁，还是一直显示比较好」。这一版把方向反过来 ——
**不是少画，而是别每帧重排版**：行号与正文共用同一套缓存排版（`GutterEntry`），
字符串高度重复（就那几十个数字），一次编译反复绘制 ⇒ 不闪、而且比以前快。

配套删掉了 `_lastDrawnFirstLine`/`_lastDrawnScrollX` 两个只服务于那次跳过的字段，
以及开头那句 `if (viewMoving) Dispatcher.Dispatch(Invalidate);` 兜底 ——
「跳过绘制」的理由没了，围着它写的东西就都是误导。

## v0.96.140 (2026-09-14) — 光标压字的真根因：尺子要与「绘制用的推进量」同格

**上一版（编辑时对齐整数号）方向不对** —— 我又量了一轮，把根因钉死了。

### 只差半步：算光标用 18.375px/字符，平台画字形用 18.000px/字符

用**不依赖任何字形留白假设**的办法定格线：`H` 在等宽字体里水平居中，
于是「格线 = 墨迹左右两端的中点 − 半格」。一行 40 个 `H`，量出来格线在 `150.5 + 18k`。

然后在同一屏里点一下 H 行、量光标中心：

| 量到的 | 值 | 说明 |
|---|---|---|
| 格线 | `150.5 + 18k` | 墨迹中点推得 |
| 光标中心 | **501** | 点击 x=500 处 |
| 最近格线 | 492.5 | 偏离 **+8.5px ≈ 半个格子** |
| 反推光标步长 | **18.375 px/字符** | `textX + 19 × 18.375 = 499.6` ✓ |
| 反推字形步长 | **18.000 px/字符** | `150.5 + 19 × 18 = 492.5` ✓ |

**所以：平台绘制时把每个字形的推进量取整到整数设备像素，而 `GetStringSize` 报的是未取整的小数。**
每字差 0.375px、沿行累积，到第 19 列就是 7px —— 光标于是画在**字符格的中间**。

⚠ **顺带纠正上一版的一个误判**：我当时按用户「24 没问题、非整数号有问题」推断「整数号就对」，
但**字号 14 也是整数号，照样差**。真正的对齐条件是
**「字号 × 0.5 × 屏幕密度」恰好落在整数上**（24 × 0.5 × 2.75 = 33.0 ✓；14 × 0.5 × 2.625 = 18.375 ✗）。
用户的观察没错，是我把条件读成了「字号是整数」。

### 修法：把「一个字形推进多少」吸附到设备像素网格

```csharp
float d = DeviceDisplay.MainDisplayInfo.Density;
lat = MathF.Round(lat * d) / d;   // 半角
wide = MathF.Round(wide * d) / d; // 全角
```

**这不是「把字号取整」**（那会毁掉无极缩放，用户明确反对）：吸附的是
**「一个字形推进多少」这个长度**，字号本身仍然连续可取，只是这个长度落在与渲染同一张网格上
——**平台画多宽，我们就按多宽算**。

### 配套：光标两头加了一对**不闪烁**的调试三角

用户提的（「为了截屏可以看到光标，你可以在光标两头做个三角形标记，标记不闪烁」）：
`DrawCaret` 在「灭」的半周期整根不画，自动化截屏因此抓不稳；
现在在光标两端画一对洋红小三角（**不受闪烁影响**，颜色刻意选语法高亮不用的洋红，
便于按颜色定位）。**只在「设置 → 编辑器 → 调试 HUD」打开时才画**，正式用户看不到。

## v0.96.139 (2026-09-14) — 编辑时把字号对齐到整数（缩放仍无极）

用户实测给出的关键线索：**「24 字号没问题，不是整数的却有问题」**。
再结合模拟器上的实测（字号 14 时排版报 18.375px、画出来是精确 18.000px），
机制就清楚了 —— **平台在绘制时把每个字形的推进量取整到整数设备像素，而我们的尺子
（`MeasureAdvances`）用的是排版报的小数**。两者只在
`字号 × 0.5 × 屏幕密度` 落到整数上时重合 ⇒ **整数号对齐、小数号沿行累积偏差**
（到第 16 个字就差 6px，光标于是压进字格里）。

两条路：跟平台的取整较劲（改宽度模型 —— 但那是「替用户决定缩放粒度」，用户明确反对），
或者**让编辑只发生在对得准的字号上**。用户自己提了后者：

> 「其实可以这样，缩放是无极缩放，但是要编辑，松手就主动四舍五入，变成最接近的那个整数不就好了？」

于是：**缩放照旧无极**（阅读/浏览时任意小数号），**只在进入编辑态时把字号落到最近整数** ——
编辑时「光标落在格线上」是硬需求，读书时不是。
落点两处：`BeginEditLine`（点一行要打字时）与 `PinchEnded`（正在编辑时捏合完松手）。
对齐时会弹一条「字号已对齐到 N（编辑时用整数号，光标才对得准）」——
否则「一点编辑文字就变大」会像 bug。

## v0.96.138 (2026-09-14) — 光标闪烁 / 光标宽度上限 / 惯性再加大

### ① 光标要闪烁（用户：「固定常亮不对」）

`DrawCaret` 只有一个「画」的分支，没有相位。加了一颗 500ms 的 `IDispatcherTimer`：
进编辑态（`EditingLine >= 0`）起、退出停；**「灭」的那半周期整根不画**。
配套一条与系统编辑器一致的行为：**光标一移动就把相位重置成「亮」并重新计时**
（`EditingCursor` 的 setter）—— 打字/移动之后应该立刻看得见光标，而不是运气不好正赶上灭的那半秒。

验证：模拟器连拍 6 张（间隔 0.35s），**3 张有光标、3 张没有** ✓。

### ② 光标「压在 P 上面」——位置本来就是对的，**问题在宽度上限**

先量位置：`i` 行（竖笔居中在格子里）上光标中心 703，格线在 704 —— **落在格线上，差 1px**。
所以位置没问题：光标**居中画在格与格的边界上**（与 `MeasurePrefixWidth` 同源），左右各伸出一半宽度；
而 `P`/`H` 这类**左竖笔紧贴格线**的字形左留白几乎为 0 ⇒ 光标一胖就把那根竖笔**整个盖住**。

v0.96.137 把宽度改成 `0.25 × 字宽`，但**上限仍夹在 2.5pt** ——
用户字号是 **24**（`0.25 × 12 = 3 > 2.5`），被上限吃满 ⇒ **等于没改**，所以他说「还是完全压在文字上面」。
现在改成 `clamp(0.2 × 字宽, 1pt, 2pt)`：字号 8 → 2.8px、14 → 3.7px、24 → 5.5px（格子 33px），
单侧伸出 2.8px，塞得进字形两侧约 4px 的留白。

### ③ 惯性再加大（用户：「滑的还不够」）

`FlingLaunchGain` 1.7 → **2.6**（第一秒位移约 39 行 → 约 60 行，滑行时长仍几乎不变）。

### ④ 顺手修的两条

- **HUD 文字超过 512 会折行**：`DrawString(text, x, y, HorizontalAlignment.Left)` 那个重载内部把边界
  写死成 512，我加分段耗时字段后整串超了 ⇒ 折成两行、第一行被顶出 18px 高的底色带
  （调性能时读不到 `w` 实测推进量）。已把拖拽诊断字段去掉、整串缩短，`w` 也改成两位小数。
- **`paint.SubpixelText = true`**：这是「不要按整数像素吸附字形」的那个标志，与用户
  「文字宽度不要取整，这样无法无极缩放」的诉求同向，所以加上。
  **但要如实说明：实测它并没有改变栅距**（模拟器字号 14 下改前改后都是精确 18.000px），
  所以它**不是**这个 bug 的成因或解法；保留是因为它对文本定位本身是更正确的设置，
  且与用户诉求同向 —— 若将来怀疑它，可以直接删掉这一行。

### 关于「有些字号是可以的」

用户这条反馈很关键，它指出了一个**真实存在、但这次没修**的机制：平台报的排版宽度
（`GetStringSize`）与它**绘制**时用的推进量并不总是相等，差值随字号/密度变化
（模拟器 420dpi、字号 14：排版报 18.375px，画出来是 18.000px）。
本次实测确认**当前这套字号下光标是落在格线上的**，所以没有按它去改宽度模型
—— 那会变成「我们替用户决定缩放粒度」，正是用户反对的。留待复现到具体字号时再处理。

## v0.96.137 (2026-09-14) — 光标不再压字（跟着字符宽度走）+ 惯性滑得更远

真机实测反馈的两条手感问题。

### ① 光标「叠在字母上」—— 位置本来就是对的，是**太粗**

用户反馈「光标好像是在字符中间，应该在字符的间隔位置」「光标在 s 字母上叠加了」。
先把现象量出来（模拟器原始帧缓冲）：反推栅距 18.4px 后，光标**中心 390.5 正好落在第 16 个
字符的边界上**（`textX + 16 × 栅距`），也就是**位置没有整格偏移** —— 它一直是对的。

真正的问题是**宽度写死 2.5pt**：这个值是按正文字号的手感定的，但格子宽度是随字号缩的 ——
字号 8 时半角格子只有 **11px**，而 2.5pt ≈ 6.9px，**占了格子大半**，左右各压到相邻字形上，
于是「在间隔里」被画成了「压在字上」。

改成跟字符宽度走：`clamp(_charWidth × 0.25, 1.2pt, 2.5pt)` —— 字号 8 → 3.3px、14 → 4.6px、
20 号以上维持 2.5pt。实测（字号 14）墨迹宽度 **8px → 6px**，而**中心 390.5 一点没变**，
两边都不再压到字形。

### ② 惯性：加大**松手初速**，不是加大摩擦

用户要的是「松手后**第一秒**滑动的距离更远，**不是持续时间变长**」。这两件事对应不同的旋钮，
一开始我调错了：把摩擦 `FlingFriction` 从 0.98 提到 0.99 —— 那确实让总距离翻倍，
但**同时也把时长拉长了**（总位移 = 初速 × dt / 行高 ÷ (1 − 摩擦)，而
时长 = ln(收手速度 / 初速) ÷ ln(摩擦)），表现就是「滑到位之后还在慢慢飘」，正是他不想要的。

正解是放大**初速**（新增 `FlingLaunchGain = 1.7`）：它只按比例放大第一秒的位移，
时长几乎不变（v0 翻倍只是把对数里的一项挪一点）。实测量级：轻扫一下第一秒约 **23 行 → 39 行**，
而滑行时长 2.37s → 2.47s（几乎不变）。摩擦已改回 0.98。

配套把「起步阈值」与「收手阈值」拆开（原先共用一个 `MinFlingVelocity`）：调滑行手感时
不该顺带改掉起步门槛，否则「轻扫一下干脆不滑了」会和「滑得更远」混在一起，下次没人分得清是哪个在起作用。

## v0.96.136 (2026-09-14) — 编辑器滚动卡顿：缓存平台排版（真机实测正文段 78ms → 21.9ms）

「滑动卡顿」这个从 v0.96.128 就挂着的老问题，这一版在**真机上量清了**并做了根治。

### 量：瓶颈到底在哪

在小米 13 上装带分段计时的构建后，滑动时 HUD 直接给出三段耗时：

| 段 | 55 行（字号 8） |
|---|---|
| 底色 | 0.4ms |
| **正文** | **78.0ms** |
| 行号栏 | 0.0ms（滚动中已跳过，v0.96.130 的优化生效） |

而 `gfxinfo` 显示 **GPU 只占 2ms**、中位帧 42ms ⇒ 瓶颈 100% 在我们的 CPU 绘制路径，且就在正文那一段
（1.42ms/行）。行号栏每个数字也是同样的 `DrawText`，却只要 0.2ms —— 差别在**每行语法 run 的数量**。

### 根因（读 MAUI 源码确认）

`PlatformCanvas.DrawText` 的实现是：
`new SpannableString(...)` → 逐 run `SetSpan` → **`new StaticLayout(...)`** → `layout.Draw` → **`Dispose()`**。
**每次调用都从头排版、画完立刻销毁，且 MAUI Graphics 里没有任何缓存缝合点**
（`AttributedText` 是纯数据、`PlatformCanvas` 只有 `_canvas`/`_shader` 两个字段、`TextLayoutUtils` 是 internal）。
一屏 55 行就是 55 次完整排版。

### 改：把编译好的排版留下来复用

- **`LineRuns` 上挂一份 `StaticLayout` 缓存**（行内容 + 字号不变就一直有效），
  每帧只剩一次 `layout.Draw`。真机实测 **78.0ms → 21.9ms（3.6 倍）**，每行 1.42ms → 0.40ms
  —— 这个数**比 MAUI 自己那条单 run 路径还快**（同一文件换 `.txt` 后缀走 Plain 高亮、每行只有 1 个 run，
  实测 25.2ms）。
- **能拿到原生画布是关键**：`Microsoft.Maui.Graphics.Platform.PlatformCanvas.Canvas` 是 **public** 的
  （`get => _canvas;`）⇒ 在**同一张画布、同一个 z 位置**上画，外层「底色 → 正文 → 行号栏 → 手柄 → HUD」
  的顺序一点没动，不需要自定义 handler、不需要改造渲染管线。
- **paint 与 span 只能自己复刻**（`CurrentState.FontPaint` 是 protected、`TextLayoutUtils` 与
  `AttributedTextExtensions` 是 internal，而本仓禁用反射）：按 MAUI 源码逐字复刻
  `new TextPaint() + SetARGB(1,0,0,0) + AntiAlias + SetTypeface(font.ToTypeface()) + TextSize = 字号`，
  span 只做 `ForegroundColorSpan`。`ScaleX` 恒为 1（只被 `canvas.Scale()` 改写，我们从不调）
  ⇒ `TextSize` 就等于字号，与测量路径同源。
- **失效条件多了一条，这是本版最容易漏的地方**：行缓存原先「与字号无关、调字号不必清」，
  挂了排版之后**必须清** —— `ResetTypography` 现在走 `ClearLineCache()`。
  释放只有 `DisposeLine()` 一个出口，**四个丢弃点**（FIFO 淘汰 / `InvalidateLine` / `InvalidateAll` /
  换字号）全部走它，漏一个就是原生对象泄漏。
- **安全网**：`CanCacheLayout` 只认「只有颜色」的 run —— 出现字体名/粗体/斜体/下划线/背景/上下标/
  删除线/列表就整行回退到平台原路。将来谁往 run 上加了别的属性而忘了同步，是**变慢**而不是**静默丢样式**。

### 验：渲染逐像素等价

替换渲染路径最怕「看着差不多、其实差一点」（这个仓在光标对齐上已经栽过八轮），所以做了 A/B 逐像素比对：
模拟器上同一文件、同字号、同滚动位置（强制停止后重新打开，位置确定），
**优化前 / 优化后两版各取一张原始帧缓冲**（`screencap` 裸 RGBA，绕开 PNG 解码）：

> **259 万像素里 581 个不同（0.0224%），且全部落在 y 50..77 的 Android 状态栏（时钟/图标）。
> 编辑器正文、行号栏、工具栏、底部状态栏、导航栏 —— 0 像素差异。**

即：自己复刻的 paint / span / layout 与 MAUI 原路径**逐像素一致**，对齐关系没有任何变化。

另：Android 与 iOS 均 0 错误编译，桌面自测 5685 通过。

### 遗留

- **冷帧仍是 108ms**（换字号 / 刚打开文件时那一帧要重建一屏的排版）。滚动稳态已解决，这一次性卡顿还在。
- **稳态帧 ~22–29ms**，还不是 60fps：剩下的钱花在 `layout.Draw` 本身与帧内其他部分（画布底色、裁剪、
  滚动条、HUD）。行号栏的 55 次 `DrawText` 也可以同样缓存，是下一步最直接的收益点。
- 有真机数据支撑的掉帧率（gfxinfo 中位帧 42ms → 38ms、掉帧 30% → 22%）**是在更苛刻的字号下测的**
  （之前 23.8 号、现在 8 号，屏上行数 21 → 55），所以这个改善是被低估的。

## v0.96.135 (2026-09-14) — 上一版自查发现的 10 处问题（代码审查逐条核实后修复）

对这一版**整段 diff**（v0.96.128~134，约 900 行）跑了一遍独立的代码审查，10 条全部核实成立。
这一版修掉其中**确实是 bug** 的 9 条（第 10 条是优化项，记在下面待办）。

### ① 【最严重】平台的选中浮层被装到了**全 App 每一个 Entry** 上

`EntryHandler.Mapper` 是**全局静态**的，我在 `MauiProgram` 里无条件 `AppendToMapping`，
于是聊天输入框、API Key、仓库地址…（共 18 处 Entry）**全都被禁掉了系统的复制/粘贴工具条**，
而它们**只有**系统那一套粘贴入口（`Clipboard.*` 在 EditorPage 之外没有出现）。

而且**语义还写反了**：`onCreateActionMode` 返回 `true` 是「**创建**这个模式」而不是「已消费」。
所以实际效果是「工具条照弹、每个菜单项都被 `onActionItemClicked` 吃掉」——
一条**点不动的**工具条，比不弹还糟。

修法：`onCreateActionMode` 返回 **`false`**（真正不创建），并**按 StyleId 限定作用范围**
（同文件上面的 `TransparentText` 就是判 StyleId 的，照它写）。

### ② 行号数字滚完不回来（v0.96.130 引入的可见回归）

`DrawGutter` 在「本帧视口在动」时跳过数字，但**没有保证之后还会再画一帧**。
实测：惯性滚动的**最后一帧恰好「还在动」**，之后无人再触发绘制 ⇒ 行号栏永久停在一条
没有数字的灰边上，要等某个无关事件（点击、进编辑）恰好重画才回来。

先后试了两版「停止时补一帧」都不成立 —— 因为惯性计时器**在被节流掉的那些 tick 里仍在移动位姿**，
补的那一帧量出来「还在动」，于是又跳过一次、然后再也没有下一帧。
**正解是在 Draw 里：本帧还在动就 `Dispatcher.Dispatch(Invalidate)` 再排一帧**，
下一帧位姿没再变就自然收敛。验证：惯性停下后不再触碰屏幕，行号自动回来（134~140）。

### ③ 缩放的比例是**连乘**的（v0.96.130 引入）

`ResetTypography` 用 `_scrollFontSize` 当基准换算横向偏移，却**没有把基准更新成新字号**。
捏合期间每接受一档就调一次本函数 ⇒ 比例变成 ∏(sᵢ/s₀) 而不是 s/s₀，缩放几下就被
`ClampScroll` 甩到行尾，缩回去也回不来 —— 正是这一版想修的那类 bug。

### ④ 字号变了但行宽缓存没清

`_lineWidths` 存的是**与字号相关**的点宽，`ResetTypography` 只清了 `_charWidthMeasured`，
于是缩放后 `MaxScrollX` 还是旧值：长行的尾巴滚不到（缩小时又能滚进一片空白）。

### ⑤ 选区操作条在滚动时不会跟着走

`ViewChanged` 只在手势结束/点击时发，而滚动路径只调 `ThrottledInvalidate()` ⇒
页面那个「视口一变就重算条子位置」的修复（v0.96.134 ②）**从未被触发**，
条子停在原地「乱飘」，「滚出视口就隐藏」的判据也从未求值。
修法：`ThrottledInvalidate` 同时发 `ViewChanged`（同一道 16ms 闸门，触摸 240Hz 也扛得住）。

### ⑥ 长按路径上「提交正在编辑的行」是死代码

改写长按分支后 `LineLongPressed` 一个触发点都没有了。后果：正在编辑的那一行只活在浮动
输入框里，而 `GetSelectedText` 读 `_doc.GetLine()` ⇒ **长按后复制到的是编辑前的旧文本**，
屏幕上却是新的。修法：长按定时器里**先**发 `LineLongPressed`（页面据此收尾）**再**选词。

### ⑦ 粘贴传了可能为 -1 的行号

`OnSelPasteClicked` 算好了夹到 ≥1 的 `line`，却把 `Canvas.CaretLine`（可能是 -1）传给了
`BeginEditLine` ⇒ `_editLine` 变成 -2，随后 TextChanged/Commit 都因 `_editLine < 0` 提前返回，
**粘贴的文字被静默丢掉**（输入框透明，屏幕上什么异常都看不到）。

### ⑧ `WordClass` 的第 2 类（CJK）**永远不可达**

`char.IsLetterOrDigit('中')` 是 **true**（汉字是 Unicode 字母类 Lo），而它写在 CJK 判断**前面** ⇒
长按 `value中文名` 会把整串当成一个词，与注释/更新日志写的「CJK 单独一类」不符。
修法：**CJK 先判**。

### ⑨ 超大只读文件上「全选+复制」会得到一份几乎全空的文本

只读大文件的内容不在内存里（`IndexedTextSource` 对 LRU 窗口外的行返回 null），
而新路径没有 `CopyAllAsync` 的预取与 200 万字符上限 ⇒ 拼出「行数对、内容几乎全空」的假文本，
还报「已复制 N 字符」。修法：加上限、拿不到的行**如实标记截断**并提示用户，不再假装复制全了。

### 未修（记在待办）

- `#if ANDROID`（而非 `#if DEBUG`）的排版自检在 Release 首帧会跑约 2000 次平台文本测量；
- `MeasurePrefixWidth` 每次调用都分配一个 `int[行长+1]` 的映射数组（在每帧路径上）。

两条都是「量级不大但白做」的优化项，等真机性能数字出来一起看。

### ⑩ 模拟器验收时发现的两条（同版补记）

**a) 版本号有**两个**真源，已经漂了 8 个版本。** —— 这是典型的「手工同步的平行表」：
`scripts/release.sh:32` 的 `VERSION` 是从 **`WayCoder/Config/Global.cs` 的 `Global.Version`**
里 sed 出来的，而 Android 包的版本来自 `WayCoder.Maui.csproj` 的 `ApplicationDisplayVersion`
—— **同一份事实的两处拷贝**。v0.96.128~134 连续 8 版只改了 csproj（因为这几版都在改 MAUI 侧），
于是 Android 包是 0.96.135、而首页/关于页/TUI 标题栏读 `Global.Version` 显示 **v0.96.127**、
桌面端打出来的包也会是 v0.96.127 —— 而且**构建全绿**，只有装上手机用眼睛看才发现。
修法：**真源只留 `Global.Version`**，csproj 用 MSBuild 属性函数从 Global.cs 正则解析
（`System.IO.File::ReadAllText` + `Regex::Match`），`ApplicationVersion`（Android versionCode）
一并推导为 `major*1000000+minor*10000+patch`；解析失败在 `BeforeTargets="Build;Publish"` 的
Target 里**报可读错误**（否则会静默产出空版本号的包）。
⚠ 位宽那条踩了一下：最初写的是 `minor*100+patch`，而 patch 已经到 135 > 100 ⇒ 0.96.135 算成
**9735**，**高于** 0.97.0 的 9700 —— 一次 minor 升级会让 versionCode 变小，Android 直接拒绝覆盖安装。

**b) 状态栏「已选 398」看着像选了 398 个字符。** —— `SelectionChangedRange` 给的是**行区间**，
紧挨着的「光标 L398」用的就是 `L` 前缀，这里却没加。改成「已选 L398 / L398-405」，
与左边同格式（这个数不改成字符数是有意的：跨几百行的选区要逐行读一遍，在每帧路径上不划算）。

模拟器实测（`emulator-5554`，Android API 36）：惯性甩动停下后**不再触碰屏幕**，行号栏自动补回
398~427（原先要等某个无关事件重绘）；编辑器长按只弹**我们自己的**操作条、无平台工具条；
聊天输入框长按**仍然**弹系统的 剪切/复制/粘贴/分享（证明 StyleId 限定生效，没有误伤其余 18 个 Entry）；
首页与 `dumpsys package` 的版本号均为 0.96.135。

## v0.96.134 (2026-09-14) — 选区手势收尾：打断时清干净 / 操作条跟着视口走

### ① 手势被系统打断时，本手势的状态没清干净

`CancelInteraction` 原先只清 `_dragging/_longPress/_dragBar`。这一版新增的 `_dragHandle`
与 `_selecting` **没被清** ⇒ 手势一旦被系统取消（来电、切走 App、父容器截走触摸），
**之后任何一次拖动都会继续去挪那个手柄**，或者变成「扩选」而不是滚动。

这类标志「粘住」的症状是「莫名其妙开始选东西」，而且**只在被打断过之后才复现** ——
最难查的一类。现在把本手势的每一个状态都清掉（含停掉长按定时器、复位捏合状态）。

### ② 操作条会浮到工具栏上去

内容区**不裁剪子控件**（MAUI 容器默认不裁剪），所以选区滚出视口之后，操作条还留在原处，
浮到工具栏/状态栏上面。现在：选区滚出视口就收起条子，并且**视口一变就重算条子位置**
（`ViewChanged` 也接上）—— 否则滚动时条子停在原地，而选区已经滚走了，看着像在乱飘。

### ③ 删掉写而不读的 `_longPress`

长按改成定时器判定之后它就没有读者了（只剩几处赋值）。留着会让人误以为它还有语义。

## v0.96.133 (2026-09-14) — 选区手柄也自己做（选区定下来之后还能拖）

### 为什么

上一版把自己的选区和操作条做出来了，但**选区一旦定下来就只能重新长按再来一次** ——
「把左端再往左挪一格」这种最常见的微调够不到。手柄补的就是这一环。

### 做了什么

- 选区两端各画一个手柄：**外圈白环 + 实心蓝**，压在深色/浅色正文上都看得见（白环就是为这个加的）。
- **画在哪与点哪算命中共用一份计算**（`HandlePositions`）。各算一次就会出现
  「看到的圆点」和「抓得住的圆点」错开 —— 滚动条那边踩过一模一样的坑（滑块几何必须与绘制同源）。
- 触摸热区**远大于**视觉半径（22 vs 6.5）—— 手指点不中一个 6.5pt 的圆点。
- 按在手柄上 ⇒ 这一手势**归手柄**（不滚动、不再判长按）；拖动只改那一端的 (行, 列)。
- 选区「谁在前」的归一化抽成 `NormalizedSelection()`，**底色铺陈与手柄位置共用** ——
  两处各写一遍的话，手柄就会落在高亮的旁边一格里。

### 验证（模拟器，Pixel 6 / API 36）

长按选中 `一二三四五六七八九十` ⇒ 两个手柄正好落在选区起止的**字符格左缘**（与高亮边界重合）；
把左手柄往左拖一格 ⇒ 选区**恰好扩进一个全角字符** `：`，高亮与两个手柄同步更新。

## v0.96.132 (2026-09-14) — 选中 / 复制 / 粘贴**全部自己做**（不再用平台的选中 UI）

### 为什么要自己来

平台那套选区 UI（长按弹出的 复制/粘贴/全选 工具条 + 两个水滴形选择手柄）**所有坐标都是
按它自己那层输入框算的**。而编辑器的正文是**自绘**的 —— 两层只要差一点点，就表现成
「选中的位置和看到的位置对不上」。与其一直去追平它的坐标系，不如**把它请出去**：

> **少一个平台参与，就少一处坐标系不一致。**

### 做了什么

1. **选区从「行级」改成「字符级」**：两个端点各是「行 + 行内码元下标」
   （原先那套的行号端点只够「选整行」，粒度太粗）。
2. **长按 = 选词**（连续的同类字符算一个词：字母数字下划线一类、CJK 一类；落在空白处选整行）；
   **长按之后拖动 = 扩选**（另一端固定，活动端跟手指）。
   ⚠ 长按必须在**手指还按着**的时候就判定 ⇒ 加了 500ms 单次定时器。
   只在抬手时按「耗时 ≥ 500ms」判断的话，**永远做不出「长按选中再拖着扩选」**这个标准手势
   —— 抬手就是手势结束，没得拖了。
3. **选区底色自己画**：按**字符跨度**铺（起点/终点都取字符格子的左边缘，与光标同源
   `MeasurePrefixWidth`），所以选区边界与文字边界永远对得上；空行/行尾给一个字符宽的最小可见段。
4. **自己的操作条**：复制 / 全选 / 粘贴（只读文件时隐藏）/ ✕，摆在选区上方（顶部放不下就摆下方）。
5. **关掉平台的选中浮层**：`CustomSelectionActionModeCallback` 的三个回调一律返回 true
   （Android 官方的关闭方式），于是平台那套工具条不再出现。
6. 剪贴板**仍走系统** —— 那是**数据**通道，不是 UI。平台自己会弹一个「已复制」预览（Android 13+ 的
   剪贴板特性），那是系统给所有 App 的反馈，不属于「平台的选中 UI」。

### 验证（模拟器，Pixel 6 / API 36）

- 长按行 10 的 `x` ⇒ 高亮**恰好一个字形宽**、状态栏 `光标 L10 · 已选 10`、**无系统手柄与浮窗**；
- 点「复制」⇒ 剪贴板拿到 `x`（系统剪贴板预览条显示的就是它）、选区自动收起、操作条隐藏；
- 只读文件下「粘贴」按钮**不出现**（不给一个按不动的按钮）。

## v0.96.131 (2026-09-14) — 光标定位收口：tab 规则合一 + emoji 单独实测 + 系统光标彻底隐掉

### ① tab 的推进规则曾经有**两套**，中文后面跟 tab 就差一格

- `ExpandTabs`（画什么）按**字符数**推进 tab stop —— CJK 也只算 1 格；
- `MeasureColumns`（点哪儿）按**显示格**推进 —— CJK 算 2 格。

于是「画出来的宽度」与「点击算出来的位置」在**tab 前面有中文/emoji** 时差一格，
表现就是「点 tab 后面那段，光标落错位置」。现在统一按**显示格**（半角 1 格、全角 2 格），
一个 tab 在视觉上永远补齐到下一个 4 的倍数格 —— 也就是「tab 相当于 4 个空格」的字面意思。

关键是**展开与「原串下标 → 展开后下标」的映射在同一次遍历里算出来**
（新增 `TextEditorMath.ExpandTabsWithMap`）：画布画的是展开串，而光标/点击按原串下标定位，
拿 `Map[i]` 去切展开串，两边就永远同源，tab 规则只有那一处实现。

自测钉住的不变量：**原串第 i 个字符之前的列数 == 展开串前 `Map[i]` 个字符的列数**
（混排 + 多个 tab + emoji 一起上）。

### ② emoji 的推进量与 CJK **不一样**（实测：中=14.00 / 😀=17.00）

`AnsiString.CharWidth` 把 emoji 与 CJK 都判成 2 列，但**打包的 Sarasa 没有 emoji 字形** ——
平台是用**回落字体**（Noto Color Emoji）画的，推进量并不等于 Sarasa 的全角宽。
一律按 `_wideCharWidth` 算 ⇒ **每个 emoji 差 3pt**，一行几个 emoji 光标就偏一截 ——
这正是「光标有时还是不对」里那个「有时」：只在含 emoji 的行上错。

改法分两档：**确定在打包字体里**的 CJK/全角区间直接走实测全角宽（快路径、不测量）；
其余宽字符（emoji / 杂项符号等）**逐个实测并缓存**（按码点，改字号清空），
拿到的就是平台真正在用的那个数。

### ③ 系统的插入光标**彻底**隐掉

`setCursorVisible(false)` 只在 `HandlerChanged` 里设一次是不够的 ——
平台在获得焦点 / 重新布局时会按自己的规则把它置回 true。所以：
焦点事件里重申一次，并且**把光标画笔换成全透明**（`setTextCursorDrawable` + API 29 守卫）；
选区底色也交给画布画（`setHighlightColor` 透明），免得系统再叠一层与自绘的错开一点。

> 方向已经定下：**光标/选择/复制粘贴全部自己做**，平台只留 IME 与剪贴板数据
> （见下一条待办）。少一个平台参与，就少一处「它的坐标和我们的不一样」。

### 验证（模拟器 + 真机同款自检）

```
[排版自检] OK 逐字累加 vs 平台排版 最大偏差=0.00px 探针行=4 字号=14.0 中=14.00 😀=17.00
```

探针行**特意挑带 emoji / 带 tab / 带中文的**（只拿纯 ASCII 行验等于没验到那三条分支），
字号 8/12/13/16/28/30 全跑一遍 —— 最大偏差 0.00px。

## v0.96.130 (2026-09-14) — 编辑器三修：缩放不再被拽回最左 / 选择与复制粘贴位置 / 滚动中省一半绘制

### ① 缩放时视口被拽回最左边

**`ResetTypography()` 里有一句 `_scrollX = 0;`** —— 字号一变就把横向偏移清零。
用户实测「明明滚到了行中间，一缩放就跳回最左边」。

它的本意是「横向偏移是像素，字号变了含义就变了」，但**清零不是解法、换算是**：
推进量与字号成正比（平台的逐字形取整只是零头），所以「原来停在左边第几列」在新字号下的偏移
≈ 旧偏移 × (新字号 ÷ 旧字号)。纵向（`_firstLine` 是行号，与字号无关）本来就不动。
新增 `_scrollFontSize` 记住 `_scrollX` 是对着哪个字号算的，换算后写回。

### ② 选择手柄 / 复制粘贴浮层位置不对

浮动的那层 `Entry` 只按**行号栏宽度**做左边距，没算上**正文左内边距**与**横向滚动**：

```csharp
LineEditor.Margin = new Thickness(Canvas.GutterWidthPx, 0, 0, 0);              // 修前
LineEditor.Margin = new Thickness(GutterWidthPx + TextLeftPad - ScrollX, 0, 0, 0);  // 修后
```

而画布正文的原点是 `行号栏 + 正文左内边距 − 横向滚动`（`CodeCanvasView` 里的 `textX`）。

这一处必须逐项对齐，因为 **Entry 的文字与光标是透明的，但「光标/选择手柄/复制粘贴浮层」
是系统按输入框自己的内部坐标画的** —— 我们只是让它看不见，没让它不存在。
输入框原点与画布差多少，那些系统浮层就偏多少（横向一滚差出整个滚动量）。

同时把 EditText 的**左右内边距清零**（`SetPadding(0, top, 0, bottom)`）：
输入框的文字原点是「外边距 + 内边距」，内边距留着就再多偏一个内边距的量。

### ③ 滚动中不画行号数字（小字号省一半绘制）

每可见行要两次平台文本绘制（正文一次、行号一次），而 `DrawText` 每次都要新建 `StaticLayout`
（`ICanvas` 没有缓存入口）。字号 8 时一屏 50 多行，行号栏就是其中一半，且滚动中数字本来也看不清。

判据是「**本帧视口位姿与上帧是否相同**」（首个可见行 + 横向偏移），不依赖手势状态机 ——
拖拽 / 惯性 / 程序滚动三条路都自动覆盖；停下后位姿不再变，下一帧数字就回来。
**底色照画、只跳数字**，否则滚动时左边缘会露出一条与正文同色的空白，看着像界面在抖。

## v0.96.129 (2026-09-14) — 编辑器定位改用**平台实测推进量**（撤销「偶数号」限制，缩放恢复平滑）

### 上一版为什么只允许偶数号

v0.96.128 之后「整行一次绘制」依赖一条等式：**平台排版 == 我们的网格**。而测出来的平台行为是：

> **Android 把每个字形的推进量取整到整数**，我们的网格用的是精确的 0.5em。

于是在**半列宽是整数**（即字号为偶数）时两边逐字相等，半列宽是 x.5 时平台每个半角字形多算 0.5。
当年正好在追「光标对不上位置」，就把字号限制成偶数档（实测偶数号整行偏差 0.00px，
**奇数 13 号同行差 53.5px** —— 一帧里那 107 列的行）。

代价立刻显出来了：**捏合每档 2 磅，手感发跳**（「只能整数缩放，反而像卡顿」）。

### 换立场：位置不再由「列 × 半列宽」算，直接累加平台实测推进量

- `CodeCanvasView.MeasureAdvances()`：量半角（`"0"`）与全角（`"中"`）各一个字符的
  `GetStringSize` 宽度 —— **量出来的就是渲染在用的那把尺子**，不用设计值推算。
- `MeasurePrefixWidth`（字符下标 → x）与 `CharIndexAtX`（x → 字符下标）改成**逐字形累加 / 反查**，
  两者互为逆、共用同一套推进量。Tab 仍按 tab stop 推进（与 `ExpandTabs` 同语义）。
- 半角/全角判据仍走 `AnsiString.CharWidth`（全仓唯一真源），不另立一套。

**偶数限制随之撤销**：`FontSize` 连续可取，捏合的浮点值直接用，菜单步长 1。
「列」这个概念只留给 tab stop 与状态栏显示，位置一律由推进量决定。

### 不变量自检证明这条等式对任何字号都成立

`CodeCanvasView` 里的一次性自检（`logcat -s WCFONT`）拿「逐字累加」与「平台整段排版」对比：

```
[排版自检] OK 逐字累加 vs 平台排版 最大偏差=0.00px
```

字号 8 / 12 / **13** / 16 / 28 / 30，前缀 30 / 60 / 120 码元 —— **包括被旧结论判死的奇数 13 号**。
（`GetStringSize` 走 `PlatformStringSizeService`，是无界排版取 `GetLineWidth(i)` 的真实浮点宽，
不是被取整的假值；早期把它误当成「折行后的宽度」，绕了一段弯路。）

### 顺带

- **横屏不再弹「全屏输入法」**：Android 的抽取式编辑（extract mode）会整屏盖住输入区。
  `Entry`/`Editor` 两个 handler 都加 `flagNoExtractUi` + `flagNoFullscreen`，
  并且**用 `|=` 而不是赋值**（MAUI 拿 `ImeOptions` 表达 `ReturnType`，赋值会把 Done 覆盖掉）。
- **字号范围 8–96**（6 号实测已看不出单词形状）、步长 1。
- **状态栏在「光标 L…」右侧显示字号**（`· 字号13.5`），捏合时能看着数字调。
- 横向滚动上限、行号栏宽度也收编到同一把尺子。

## v0.96.128 (2026-09-14) — 修移动端编辑器「滑动/缩放卡顿」的真身：字体被压缩进 APK，每帧解压 25MB

### 症状

滑动/缩放时每帧约 **250ms（≈4fps）**，手感是「一顿一顿」。`dumpsys gfxinfo` 对照：
95th 250ms / 99th 450ms / janky **4.10%**。

### 怎么定位的

在 `Draw` 里塞分段计时打 logcat（底色 / `canvas.Font` / `canvas.FontSize` / 行底 / 正文 / 行号栏），
靠**排除法**逼近：

| 观察 | 结论 |
|---|---|
| `canvas.Font = X` 耗时 **0.0ms** | 设字体本身不要钱 |
| 紧跟的 `canvas.FontSize = X` 耗 **~110ms** | 钱花在这一句上 |
| **同一个字号再设一遍只有 0.0ms** | 是**一次性**开销（字体族解析），不是 `setTextSize` |
| 正文那一段（几百次 `DrawText`）只有 7~50ms | 画字本身不慢 |

去读 MAUI 源码，`PlatformCanvasState.FontPaint` 的 getter 会在 `_typefaceInvalid` 时调
`Microsoft.Maui.Graphics.Platform.FontExtensions.ToTypeface()` —— **那条路没有任何缓存**：

```csharp
var id = context.Resources.GetIdentifier(font.Name, "font", context.PackageName);
if (!TryLoadTypefaceFromAsset(font.Name, out typeface)) { context.Assets.List(""); }
typeface = Typeface.CreateFromAsset(Aapplication.Context.Assets, filename);   // ← 每次重新解析
```

而我们的资产是 **25.5MB 的 CJK 字体**，且**在 APK 里是 Deflate 压缩的**（`unzip -v`：
`Defl:N 25541448 → 12818382`）⇒ `CreateFromAsset` 每次都要**把 25.5MB 解压一遍**。
一帧解压**两次**（行号栏前一次、正文前一次）≈ 220ms，正好是整帧的开销。

### 修法

```xml
<AndroidStoreUncompressedFileExtensions>.ttf;.otf</AndroidStoreUncompressedFileExtensions>
```

资产改成 `Stored` 打包（验证过 APK 里是 `Stored ... 0%`），Android 可直接 mmap，解析代价随之崩掉。
**不压缩打包是这一版的关键，别删那一行** —— 删了不报错，只是慢，而且只在真机滚动时才看得出来。

同批一起做的（都属于「一帧里少做无谓的事」）：

- 行号栏**不再重复设 `canvas.Font`**（`CanvasFont` 是 static readonly，正文前已设过；而任何一次
  `Font` 写入都会让字体族解析作废、下次重解析）。
- **整行一次 `DrawText`**：原先按语法段逐段画（一行十几次调用），现改成一行一次、语法色是同一串里的多个 run。
- 行缓存从「缓存整行 `AttributedText`」扩成**整行的绘制对象**（分词/切 run/取色一次性建好，滚动时零重算）。
- 删掉每帧一次的 `GetStringSize("0")`（结果赋给了一个**从未被读取**的局部变量）。

### 结果

| 分段 | 修前 | 修后 |
|---|---|---|
| 字体族解析 | **112.8ms** | **0.3ms** |
| 行号栏 | **115.3ms** | **2.4ms** |
| 整帧 | **~250ms（4fps）** | **~31–56ms** |

### 还没解决的（诚实记录）

**小字号（≤10）滑动仍偏卡**：`framestats` 拆开是 `布局 0.1ms / 绘制 51.9ms / GPU 5.6ms`
—— 卡在我们的绘制路径。每可见行要两次平台文本绘制（正文 + 行号），而 `DrawText` 每次都要新建
`StaticLayout`（`ICanvas` 没有缓存入口）。字号 8 时一屏约 2 倍于字号 14 的行数，于是**行数把它吃回去了**
（每行成本 1.8ms → 0.96ms，但行数 24 → 54）。
下一步在做与不做之间有取舍：**滑动中先不画行号，停下再补**（约省一半），需用户拍板。

## v0.96.127 (2026-09-14) — 修 iOS 光标对不上位置（字体名写错，静默回落）

### 症状与根因

iOS 上光标与文字对不上位置。根因是 **`CanvasFontName` 的 iOS 分支写错了字体名**
⇒ `UIFont.FromName` 返回 null ⇒ **静默回落成系统比例字体** ⇒ 渲染按比例走、
而定位按「汉字 = 2 列」的网格走，两者必然错开。

**这个字体有三个互不相同的名字，之前只对了一个**（都是从 TTF 的 `name` 表读出来的）：

| 用途 | 正确值 | 之前写的 |
|---|---|---|
| 家族名（nameID 1/16） | `Sarasa Mono SC` | — |
| **PostScript 名（nameID 6）** | **`Sarasa-Mono-SC-Regular`** | ❌ `SarasaMonoSC-Regular` |
| Android 资产文件名 | `SarasaMonoSC-Regular.ttf` | ✅ 对的 |

MAUI 在 iOS 上三条路**都要 PostScript 名**（已核对 `Graphics/Platforms/MaciOS/FontExtensions.cs`）：

```csharp
public static CGFont ToCGFont(this IFont font) => CGFont.CreateWithFontName(font.Name);
public static CTFont ToCTFont(this IFont font, ...) => new CTFont(font.Name, ...);
PlatformFont.FromName(font?.Name ?? DefaultFontName, ...)   // UIFont.FromName
```

### 修法

`EditorTypography.CanvasFontName` 的 iOS 分支改成 PostScript 名 `Sarasa-Mono-SC-Regular`，
并把三个名字的对应关系写进文档注释（**别再互相顶替**）。

### 加了跨平台字体自检

`CodeCanvasView.Draw` 里一次性检查（`#if DEBUG`）：量半角/全角的实宽与
`字号÷2`/`字号` 比，不符就打出

```
[字体自检] ❌ 字体回落了！检查 CanvasFontName a=… 中=… 名=…
```

**这类错误平台不抛异常**，只有主动量才知道 —— 和 GUI 侧 `GuiFonts.Verify()` 是同一套纪律。
iOS 上不便反复迭代，这道自检是必要的。

### 验证状态

Android / iOS 两个目标 **0 错误**。
**iOS 实机行为尚未复验**（模拟器触控自动化还不稳），待办见下。

## v0.96.126 (2026-09-14) — GUI 编辑器换内嵌等宽字体（改造·第三步）

### 为什么必须换字体：它不是审美问题，是网格的地基

上一步把编辑器列宽定成 `FontSize / 2`（**0.5em**），而 GUI 原先五处都写死
`"Menlo,Consolas,monospace"` —— **Menlo 这类拉丁等宽字体的步进是 0.6em**，与网格对不上。
「汉字 = 2 列」要精确成立，字体必须满足「**拉丁 0.5em、汉字 1em**」，而这恰好是
为 CJK 设计的等宽字体（Sarasa / Cascadia 那一路）才有的性质，拉丁等宽字体里没有。

### 做了什么

- **内嵌 Sarasa Mono SC**：csproj 用 `AvaloniaResource` **链接** `WayCoder.Maui/Resources/Fonts/`
  那份（仓库里不存第二份 24MB 二进制）
- 新增 `WayCoder.Gui/GuiFonts.cs`：`Mono` / `MonoTypeface` 收成一处
  （原先 `EditorView` / `GuiInteraction` / `ToolDetailWindow` / `MarkdownInlines` /
  `MarkdownBlocks` **五处各写各的字面量** —— 正是仓库里「平行表」那个坑）
- 五处调用点改指向它

### 字体性质是**从 TTF 里读出来的，不是猜的**

直接解析字体的 `cmap`（format 12 子表）+ `hmtx`：

| 字符 | 步进 |
|---|---|
| 拉丁 `a` / `W` / `0` | **0.5000 em** |
| 汉字 `中` / `文` | **1.0000 em** |
| 全角标点 `，` / `！` | **1.0000 em** |
| 日文 `あ` | 1.0000 em |
| emoji | **无字形**（靠系统 fallback） |

内部族名也从 `name` 表（nameID 1）读出 = `Sarasa Mono SC`，**没有靠猜** ——
avares URI 里族名写错是**静默回落**，症状与我们要修的 bug 一模一样。

### 加了自检，把静默失败变响

`GuiFonts.Verify()`（静态构造里跑一次，写 stderr）：量半角/全角实宽与
`字号÷2`/`字号` 比。启动实测：

```
[字体自检] OK  内嵌 Sarasa 已加载：a=6.50（期望 6.50） 中=13.00（期望 13.00）
```

**既证明没回落，也证明「0.5em / 1em」在 Avalonia 的渲染下精确成立** —— 网格地基坐实了。
（同 MAUI 那边红标尺的道理：这类事情只有拿到数才算数，构建通过证明不了任何东西。）

## v0.96.125 (2026-09-14) — GUI 编辑器换成网格定位（改造·第二步）

### 删掉「第二把尺子」

`EditorView` 原先自带一套字宽模型：`CharWidth(char)` 对**每个字符**单独建一个
`FormattedText` 取 `Width`，`VisualToCol` 按它累加、`TextWidth` 对前缀建串再量。
一条 1029 字符的行，**光光标定位每帧就要建上千个 FormattedText** —— 这是本轮最大的开销。

现在三处全部改走共享网格（`TextEditorMath` + `AnsiString.CharWidth`）：

- `TextWidth(line, col)` → `ColumnsToX(MeasureColumns(...))`，纯算术，不再 new 任何文本布局
- `VisualToCol(line, x)` → `ColumnToCharIndex(line, XToColumn(x, HalfWidth))`
- 新增 `HalfWidth = FontSize / 2` 常量（字体设计值，不用实测值）
- `CharWidth(char)` 整个删除

### 修掉三个按 `char` 遍历留下的 bug

`ExpandLine` 重写（改按 Rune 走）：

1. **tab 按列位展开** —— 原来一律当 4 个空格，列位 2 上的 tab 该补 2 格却补了 4 格，
   于是**显示串与列号模型对不上**，横滚和点击都会偏
2. **代理对不再被当成两个字符** —— emoji 原先被算成 2 列（两个 char 各 1 列），
   恰好凑对，但中间态会算错；现在按 Rune 走
3. 顺带产出**起始列与列数**，供渲染侧做同单位裁剪

### 修掉横向裁剪的单位 bug

`Render` 里原本是 `if (start + len <= (int)_hScroll) continue;` —— 拿 **span 的字符下标**
去比 **`_hScroll` 的像素值**，两个单位。横滚之后该跳的不跳、该画的不画。
现在两边都换算成**列号**再比。

### 验证

`dotnet build WayCoder.Gui` → **0 错误 0 警告**。

### 还没做

**行缓存**（每行的 token 段 + `FormattedText` 复用）是计划里性能收益的另一半 ——
现在每帧仍对每个可见行重跑 `Tokenize` + `new FormattedText`。
抓手已找好：`EditorCore.LastChange (Start, End)?` **已存在但 GUI 完全没用**。

## v0.96.124 (2026-09-14) — 网格换算下沉到共享层（GUI 编辑器改造·第一步）

GUI（Avalonia）编辑器要重做渲染层，第一步先把**定位真源**统一掉 —— 否则又会变成
「GUI 抄一份、MAUI 抄一份」，正是这个仓库里排第一的坑（同一规则两处实现）。

### 做了什么

- `WayCoder/Infra/TextEditorMath.cs` 新增四个方法，**GUI 与 MAUI 共用**：
  `MeasureColumns` / `ColumnsToX` / `XToColumn` / `ColumnToCharIndex`
  （半角 1 列、全角 2 列、tab 补到 4 的整数倍；列宽由调用方给 `halfWidth`）
- 宽字符判定默认走**全仓唯一真源** `AnsiString.CharWidth`（可用 `widthOf` 参数注入覆盖，
  保持该文件「字宽由调用方注入」的原设计）
- `CodeCanvasView` 删掉本地那四份，改为调用共享版；顺带删掉已无人调用的 `RuneWidthApprox`
- 清掉因删除而悬空的两处 `<see cref>` 与一段过时文档

### 收益：这层现在能被桌面自测覆盖

`SelfTest.Chunk21.cs` 的 `[编辑器数学]` 段补 12 条断言：
半角/全角/中英混排/emoji 算 2 列/下标落在代理对中间不劈开/tab 补 4/列↔x/
**x→连续列刻意不取整**/**全角字前半归它之前、后半归它之后**/末尾夹到行尾/零宽字符不占格。

`dotnet run -- --test` → **5692 通过 / 0 失败**；MAUI（Android）与 GUI 均 0 错误。

> 这几条断言正是 MAUI 那八轮里靠真机截图才量出来的结论（中点判定、emoji 列宽），
> 现在变成纯函数断言了 —— 下沉的主要意义就在这里。

## v0.96.123 (2026-09-14) — iOS 在模拟器跑通 + 触控自动化打通（**无代码改动**）

本轮只做验证，代码一行未改。两条经验值得留档，都是下次会再踩的。

### 一、iOS「启动即崩」的根因是 `obj/` 残留，**不是工具链损坏**

现象是模拟器上启动即崩于：

```
Microsoft.iOS: The static registrar map for Microsoft.iOS is invalid.
It was built using a runtime with hash bf48bb9d..., but the current runtime
was built with hash ac895e19...
→ System.ArgumentNullException at UIWindow.set_RootViewController
```

此前判断为「iOS 工作负载/运行时包不同步（环境问题）」，**这个判断是错的**。
真因是 **`obj/` 里残留着别的 SDK 版本编出来的中间产物**。清掉重编即可：

```bash
rm -rf obj/Debug/net10.0-ios bin/Debug/net10.0-ios
dotnet build WayCoder.Maui.csproj -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64
# → 0 错误；模拟器上道码 v0.96.122 首页正常渲染，进程常驻，无崩溃报告
```

**教训**：遇到「注册器哈希不匹配」这类**看起来像环境损坏**的报错，先清 `obj` 再怀疑工作负载 ——
清 `obj` 是秒级的，`dotnet workload repair` 是分钟级的，且多半治不了这个。

### 二、iOS 模拟器触控自动化：AppleScript + 坐标标定

`xcrun simctl` 没有触控命令。可用 `osascript` 合成点击（**需要「辅助功能」权限**：

`tell application "System Events" to click at {x, y}` —— 未授权时报 `-25204`）。

**iPhone 17 = 402×874 pt @3x（1206×2622 px）**，Simulator 窗口内容区在窗口内**居中**：

```
屏幕坐标 = 窗口原点 + ((窗口宽 − 402)/2, (窗口高 − 874)/2) + (设备px ÷ 3)
```

实测标定成立（点 (330,934) 正确进了「文件」页）。脚本见 `/tmp/iostap.sh`。

**验证落点的现成手段**：`click at` 会**返回被点中的那个 UI 元素** ——
返回 `... of application process Terminal` 就说明坐标错到本会话窗口上了，
返回 Simulator 的按钮名就说明点对了。不用截图猜。

### 三、iOS 沙箱里的测试文件怎么放

iOS app 有自己的沙箱，Android 的 `/sdcard/waycoder/workspace` 看不到：

```bash
C=$(xcrun simctl get_app_container <udid> com.companyname.waycoder.maui data)
cp long1k.txt noemoji.txt "$C/Library/workspace/"
```

### 未完成

- **点击不稳定**：同一坐标 (330,934) 第一次进了文件页，重试就点不动。
  可能是 App 重启后首页滚动位置变化，或 Simulator 抢焦点 —— **没查清楚，不能当能用**
- **iOS 编辑器未验证**：那两处一行没验过的地方仍在（`CanvasFontName` 的 PostScript 名分支、
  `HalfWidth = FontSize × 0.5` 这个网格前提在 iOS 上是否成立）

## v0.96.122 (2026-09-14) — 编辑器：钳住浮动 Entry 的高度（选区高亮/手柄错位）

### 现象

编辑模式下长按选中文本时：**选区高亮是一条跨 3 行的矩形、两个选择手柄落在编辑行下方两行**。

文字与光标都是画布画的（浮动 `Entry` 只做 IME/系统复制粘贴，文字透明），
所以平时看不出异常 —— **只有高亮块和手柄会暴露 Entry 的真实边界，而它有 3 个行高那么高**。

### 根因

`Pages/EditorPage.xaml.cs` 四处设了 `LineEditor.HeightRequest = EditorTypography.LineHeight;`（18），
但 **`HeightRequest` 只是「请求」，不是上限**：Entry 在 `VerticalOptions="Start"` 下会按内容
自然高度撑开（13pt 加 EditText 默认内边距实测约 54dp ≈ 3 个行高）。

### 修法

四处各补一行 `LineEditor.MaximumHeightRequest = EditorTypography.LineHeight;` ——
**这才是真正的钳制**。文字与光标都由画布画，钳掉不影响观感，只让高亮/手柄回到正确位置。

### 验证

| | 高亮 | 手柄位置 |
|---|---|---|
| 修复前 | 跨 2~4 行的矩形 | 第 4 行（编辑行下方两行）|
| 修复后 | 第 2 行单行横带 | 第 2 行（编辑行）✓ |

### 复制粘贴测试结论（本轮一并做的）

**工作正常**：只读模式长按 → `复制此行/选择行范围/全选并复制`，复制后 toast 预览内容；
编辑模式长按浮动 Entry → 系统菜单 `Translate/Cut/Copy/Paste`；Paste 正确**替换**原选中内容；
拖动选择手柄能正确扩展选区。

**放大镜未出现**：拖手柄时 Android 该浮出文本放大镜，实测没有。但放大镜是**系统提供**的，
应用侧没有实现代码；`adb shell input swipe` 是合成的粗略手势，**不排除是注入方式的问题而非真缺陷** ——
需要真机手拖复核，本条未定性。

## v0.96.121 (2026-09-14) — 修掉 iOS 构建被诊断探针弄坏的问题

**iOS 目标一直是编译不过的，而且是我自己弄坏的**：v0.96.116 加的宽度自检探针
（`AltMeasure` / `AltMeasureOne`）套在 `#if DEBUG` 里，却**没有 `#if ANDROID`** ——
里面全是 `Android.*` 与 Android-only 的 `FontExtensions.ToTypeface`，
于是 `net10.0-ios` 下 15 个编译错误。

- 两个对照测量方法**删除**（一次性的 A/B 实验，结论已经落到 v0.96.117 的修复里）
- `LogWidthProbe` 收进 `#if DEBUG && ANDROID`（它输出走 `Android.Util.Log`）
- 探针里那批针对「全角标点压缩」的逐字符项也删了，只留
  「整串 vs 单字之和」这一条 —— 将来换字体/升 MAUI 时一眼能看出字有没有被吞

**教训**：给 MAUI 加 `#if DEBUG` 的诊断代码时，别忘了平台守卫 ——
桌面构建全绿看不出来（同 `CoreStubs.cs` 那条），只有真去编 iOS 才现形。

### iOS 编译结果

`dotnet build WayCoder.Maui.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64`
→ **0 错误**。

### iOS 运行：未跑通，但原因是工具链而非代码

模拟器上启动即崩，日志：

```
Microsoft.iOS: The static registrar map for Microsoft.iOS is invalid.
It was built using a runtime with hash bf48bb9d..., but the current runtime
was built with hash ac895e19...
→ System.ArgumentNullException at UIWindow.set_RootViewController
```

静态注册器与运行时的哈希对不上 —— 属 **iOS 工作负载/运行时包不同步**的环境问题，
与本轮改动无关（代码编译通过即为止损点）。待修：清理 `obj`/`bin` 后
`dotnet workload repair` 或重装对应 runtime pack。

## v0.96.120 (2026-09-14) — 编辑器：清掉平台布局死代码（-255 行）+ 中点判定修正

### 删掉的东西（净 -255 行，1671 → ~1416）

网格模型落地后，这一整套都不再有调用点，之前只是被 `if (true) return -1;` 停着：

- **平台布局引擎整块**（`EnsureAndroidLayout` / `CharIndexAtXPlatform` /
  `PrefixWidthPlatform` / `_androidLayout` / `_androidPaint` / `PlatformDensity`）
- **宽度实测簇**（`AdvanceOf` / `CacheRuneWidths` / `RuneWidth` / `_runeWidths` /
  `AdvanceSampleCount` / `WholeMeasureMaxChars`）
- **二分前缀定位**（`CharIndexAtXByPrefix`）与 `CharIndexAtX` 里的逐字累加兜底
- `InvalidatePlatformLayout` 及其调用

`CharIndexAtX` 现在只剩一行：`ColumnToCharIndex(line, XToColumn(xInLine))`。

### 顺带修掉一个我自己引入的回归

`XToColumn` 原先**四舍五入到最近列**，于是「格子内部靠右的一点」被推到下一格的边界上 ——
点 `！`（11–13 列）的右半边会算成「下一个字符之前」，**点哪儿都往后跳一格**。

改成：`XToColumn` 返回**连续列位置（不取整）**，由 `ColumnToCharIndex` 做中点判定
（前半归它、后半归它后面）。这也正是文件里那句注释原本就写着、而旧实现并没做到的语义。

全角字符因此不会被劈开：整格 2 列，中点在第 1.5 列，左半边一律归到它之前。

### 复验（1K 行 `long1k.txt`）

| 点击 | xInLine | 列位置 | 命中 |
|---|---|---|---|
| x300 | 79 | 12.15 | `i8`（`！` 中点 12，右侧 → 其后）|
| x600 | 193 | 29.69 | `i20`（😀 中点 29，右侧 → 其后）|
| x900 | 307 | 47.23 | `i32`（`3` 中点 2.5，左侧 → 其前）|

网格行宽 `W11134` 不变。插入复验：点 x600（= `i20`）输入 `X` → 落在 **😀 之后**，
与报出下标一致；emoji 未被劈开，整行仍对齐。

## v0.96.119 (2026-09-14) — 编辑器：emoji 列宽修正 + 1K 行端到端验证

### 修掉一个真问题：本地宽字符表把 emoji 判成 1 列

`CodeCanvasView.RuneWidthApprox` 是一张**手写的**宽字符表，里面没有 0x1F000 段 ——
于是 emoji 被判成 **1 列**，而全仓唯一的宽度真源 `AnsiString.CharWidth` 判 **2 列**
（`cp is >= 0x1F000 and <= 0x1FAFF`）。

1029 字符的测试行里有 **114 个 emoji**，不修的话光这一项就累计偏 **114 列**。
现在 `RuneWidthApprox` 直接委托 `AnsiString.CharWidth` —— 又一处「同一规则两处实现、
只改了一处」，这次摊在宽度表上。

### 1K 行端到端验证（`long1k.txt` = 1029 字符，重复的 `aB3中文，。！😀`）

**网格行宽**：`W11134` —— 与独立算出的 `1713 列 × 6.5 = 11134.5` **完全吻合**
（114 个重复单元 × 15 列 + 尾部 `aB3` 3 列）。

**定位**（三个点全部精确命中）：

| 点击 | 报出 | 校验 |
|---|---|---|
| `x79` | `i7` | 79/6.5=12.15 列 → `！`（11–13 列）✓ |
| `x193` | `i18` | 193/6.5=29.69 → 第 2 单元第 14.69 列 → 😀（13–15），UTF-16 = 1×10+8 ✓ |
| `x307` | `i32` | 307/6.5=47.23 → 第 4 单元第 2 列 → `3`，UTF-16 = 3×10+2 ✓ |

坐标换算也自洽：屏幕 300px = 114dp = 17.5 列。

**插入**：点 `x600`（= `i18`）输入 `X` → 落在 `！` 与 `😀` **之间**，emoji 未被劈开，
插入后整行仍对齐。

**删除**：两次退格依次删掉 `X`、再删掉它前面的 `！`，位置准确，emoji 完好。

**⇒ 「1K 字符混排 + 行尾定位 + 插入删除」这条链路通了。**

## v0.96.118 (2026-09-14) — 编辑器：定位改用**自建网格模型**（尺子只有一把）

v0.96.117 把偏差从 24.5px 压到 1.5px，但立场仍然是「让测量的数追上渲染的位置」——
**两把尺子**。本轮换立场：**位置一律由我们自己算，不看字体度量。**

### 做了什么

- 新增网格接口（纯计算，不碰平台）：
  `MeasureColumns(line, charIndex)`（半角 1 列 / 全角 2 列 / tab 补到 4 的整数倍）、
  `ColumnsToX(cols)`、`XToColumn(x)`、`ColumnToCharIndex(line, col)`
- `MeasurePrefixWidth` 改为 `ColumnsToX(MeasureColumns(...))` —— **不再调 `GetStringSize`**
- 新增**自建的绘制接口** `DrawGridRuns`：一行不再整行交给平台排版，而是**逐段（语法 token）
  按网格列定位绘制**，每段起点强行 = 该段起始字符的列号 × 列宽。
  段直接复用语法上色已有的 token run，不额外增加分词开销

这样即使某处字形与列宽有差，**误差也不跨段累积** —— 每段都被重新按回网格。
「一路修测量」只能把偏差压小，压不掉「两把尺子」这件事本身。

### 一个反直觉的读数，值得记下来

状态栏 `W110`（网格算的）与 `G114`（`GetStringSize`）差 4dp。**网格是对的**：

- Sarasa 拉丁真值 = 0.5em = **6.5dp**（像素实测 `B→3 = 17px = 6.48dp`）
- `GetStringSize` 报 114 是 Android 把每字推进量**取整**（6.5→7）后的产物，
  7 个拉丁正好多 3.5dp

⇒ **别再拿 `GetStringSize` 的行宽当基准**，它系统性偏大。

### 验证

点击实测（行 `aB3中文，。！|END`，网格下 `D` 在 104–110.5、`文` 在 32.5–45.5）：

- 点 `x44`（`文` 的格子）→ **`i4`** ✓
- 点 `x109`（`D` 的格子）→ **`i11`** ✓

### 遗留

`EnsureAndroidLayout` / `CharIndexAtXPlatform` / `PrefixWidthPlatform` 这套平台布局探测
代码在网格模型下已全部作废（此前就已被 `if (true) return -1;` 停用），
连同 `AdvanceOf` / `CacheRuneWidths` / `_runeWidths` 一起待清理。**本轮没删** ——
一次只动一件事，先让功能换过去。

## v0.96.117 (2026-09-14) — 编辑器：点击偏移**已修复**（测量与渲染同源）

v0.96.116 定位到「全角标点紧跟全角标点，测量宽度掉一半」，但不知道该怎么修。
本轮修好了，**根因是字体，不是算法**。

### 根因

`BuildAttributed` 给每个 run 写了 `[TextAttribute.FontName]`。MAUI 会把它变成 Android 的
`TypefaceSpan(族名)` —— **那个 API 只认系统字体族名、没有 asset 重载**
（`dotnet/maui` 的 `Graphics/Platforms/Android/Text/AttributedTextExtensions.cs`）。
我们的资产名喂进去解析不到，于是**静默回落成平台默认的比例字体**：

- 渲染用比例字体 ⇒ 中文与拉丁的宽度比不再是 2:1
- 测量（`GetStringSize`）走的是 `FontExtensions.ToTypeface`，它有 `CreateFromAsset` 分支，
  **能加载打包字体**
- 两条路量的是**两个不同的字体**，越往右越偏

外加系统 `monospace` 自己的毛病：全角标点紧跟全角标点时会掉一半宽
（`[中文，。！]=52` 而非 65，`[，，]=19.5` 而非 26）。**Sarasa 没有这个毛病**（实测 65 / 26）。

### 修法（三处，都很小）

1. **run 上不写 `FontName`** —— 布局就回落用 `FontPaint` 的字体，而那正是 `canvas.Font`
   （`EditorTypography.CanvasFont`），走 `CreateFromAsset` 分支，**打包字体在这里能加载**
2. **`CanvasFontName` 改成资产名**（Android `SarasaMonoSC-Regular.ttf` / iOS PostScript 名），
   `FontFamilyName` 保持 Controls 侧的别名 `SarasaMonoSC`
3. **列宽用字体的设计值**（`FontSize × 0.5`）而非实测值 —— Android 把行宽**取整**
   （13pt 时拉丁真值 6.5 报成 7），照实测值定位每个拉丁字符多算 0.5pt

Sarasa Mono 的拉丁推进量**恰好 0.5em**、汉字**恰好 1em**，所以「汉字 = 2 列」的网格
与字体设计天然对齐 —— 那 24MB 字体从「用不上的包袱」变成了方案的一部分。

### 验证（红标尺 + 截图取墨迹列，非目测）

| | 标尺 x | 墨迹右端 | 偏差 |
|---|---|---|---|
| 修复前 | 389.5 | 414 | **24.5 px** |
| 修复后 | 404.5 | 406 | **1.5 px**（≈0.6dp）|

点击实测（行 `aB3中文，。！|END`，12 字）：

- 点 `D`（107–114dp）→ `i11` ✓
- 点 `文`（34–47dp）→ `i4` ✓（修复前会算成 `i3`，**差一个字符**）

### 顺带证实的两件事

- `[altOne:，，]=26.00`（不带 span → 用画布字体 → 对）vs `[alt:，，]=19.50`
  （带 `TypefaceSpan(资产名)` → 回落 → 错）—— **这组对照直接坐实了「那行 FontName 是元凶」**
- 压缩是**恒定且线性**的（`@26=130`、`@34.125=170`、`@52=260` 全是 65 的整数倍）
  ⇒ v0.96.115 里「按密度补正」那条路彻底作废

### 保留的诊断工具（都挂在 `ShowDebugHud` 下，默认关）

- **红标尺**：在测量出来的行尾画竖线。它的价值是**把「偏了多少」从目测变成可量** ——
  这次就是靠它（配合截图取墨迹列）定位并验证的
- **`LogWidthProbe`**：一次性把逐字符实测宽度、对照构造、线性性检查打到 logcat（tag `WCW`）

## v0.96.116 (2026-09-14) — 编辑器：点击偏移收窄到「全角标点测量」（**仍未修复**）

v0.96.115 说「测量与渲染差 19%」，那是个**读数不准的判断**。本轮把测量搬进
App 内部（`CodeCanvasView.LogWidthProbe`，logcat tag `WCW`），拿到的是确定值：

```
fs=13 density=2.625
[a]=8.00 [中]=13.00 [W]=8.00 [|]=8.00          ← 逐字准：等宽成立，CJK=13dp
[中]@34.125 = 34.00                            ← 线性
[aB3|END]=56.00     = 7 × 8        ✓
[中文，。！]=52.00   = 4 × 13       ✗  ← 5 个字，只量出 4 个
```

**逐字测量是准的**（`中` 量到 13dp，像素实测渲染也是 13dp —— 两边对得上），
**错在整串**：`。`、`！` 这类全角标点**紧跟在另一个全角标点后面**时，宽度从 13 掉到 6.5：

```
[，]=13  [。]=13  [！]=13        单字都准
[中文，]=39                      13×3 ✓
[中文，。]=45.5                  13×3 + 6.5   ✗
[，，]=19.5  [。。]=19.5  [！！]=19.5          13 + 6.5   ✗
[中中]=26                        13×2 ✓（汉字之间正常）
```

⇒ `aB3中文，。！|END` 因此少算一个字宽（108 而非 121），这就是点击越往右越偏的来源。

### 本轮排除掉的（都是拿读数证伪的）

- **构造器差异**：`CreateLayoutForSpannedString`（废弃构造器，渲染走的那条）与
  `CreateLayout`（Builder，测量走的那条）**结果逐项相同** —— `[alt:中文，。！]=52.00`
- **`TypefaceSpan` 的锅**：不加 span 也一样（`[altOne:，，]=19.50`）
- **字号不对**：压缩**恒定且完全线性**（`@26=104`、`@34.125=136`、`@52=208` 全是 52 的整数倍）
  ⇒ **换字号治不好它**，v0.96.115 里「按密度补正」那条路也一并作废
- **密度、字体没打进 APK、逐字累加算法**（v0.96.115 已排除，本轮维持）

### 仍未解释的矛盾

渲染出来每个全角字符的步进**看着是均匀的**（中→文→，→。→！四步都在 31~39px 之间，
均值 ≈34px = 13dp），而**任何**我能构造出来的布局都给出「压缩后」的位置。
但我的像素估计精度只有 ±5%（一个 34px 的格子 ±3px），**而偏差本身就是 6~9% 量级** ——
**再往下抠已经超出这套测量手段的分辨力**，所以本轮到此为止，不再拿目测下结论。

### 下一步（已经很清楚）

**不要再比「谁更接近像素」，要换成同源**：自建 `StaticLayout`（按
`AttributedTextExtensions.AsSpannableString` + `CreateLayoutForSpannedString` 逐行复刻），
**绘制与命中都用同一个 layout 对象**，位置一律走 `Layout.GetPrimaryHorizontal(charIndex)`、
命中走 `GetOffsetForHorizontal(line, x)` —— 这正是 AOSP `TextView` 自己的做法，
测量与渲染**同源是构造保证的**，不必再解释「为什么两个数不一样」。
`ICanvas` 侧取平台画布的口子也有了：`ScalingCanvas.ParentCanvas` 与
`PlatformCanvas.Canvas` 都是 public。

### 新增：逐字符宽度探针

`CodeCanvasView.LogWidthProbe`（`#if DEBUG`，真机 logcat 过滤 `WCW`）：一次性打印
逐字符实测宽度、对照构造（`AltMeasure`/`AltMeasureOne`）与线性性检查。
**这类问题只有把数字打出来才问得下去** —— 屏幕上肉眼比不出来（见上）。

## v0.96.115 (2026-09-14) — 编辑器：点击偏移根因定位（**未修复**）+ 调试标尺

### 根因（已确认）

点击定位随非 ASCII 字符累积偏移。根因是**测量的宽度与渲染的宽度不是同一把尺子**，
红标尺实测差约 **19%**。三层证据：

1. **纯 ASCII 行准确、混排行偏移** ⇒ 偏的是非 ASCII 字符，不是「累积误差」（早先那个判断是错的）
2. **MAUI 在 Android 上渲染 `AttributedText` 只能走 `TypefaceSpan(系统族名)`**
   （见 `dotnet/maui` 的 `Graphics/Platforms/Android/Text/AttributedTextExtensions.cs`）——
   它内部是 `Typeface.Create(family, style)`，**只认系统字体族名、没有 asset 重载**，
   MAUI 也没留传 `Typeface` 的口子。⇒ **打包字体在这个 API 下根本用不上**
3. **两条测量路径（自建 `StaticLayout`、MAUI 的 `GetStringSize`）结果一致**，
   但都与渲染差约 19% —— 所以不是「哪条路径没生效」，是两条都不同源

### 排除掉的假设（都是拿读数证伪的，不是猜的）

- **密度换算**：平台密度 2.62 与「屏幕物理宽 ÷ 控件 dp 宽」的 2.62 完全一致
- **字体没打进 APK**：字体确实在 `assets/SarasaMonoSC-Regular.ttf`（与 OpenSans 并列）
- **字母宽度累加算法**：换成整段前缀测量后 sum/whole 已是 1.00

### 新增工具：调试标尺（`#if DEBUG`）

在「测量出来的行尾」画一条红色竖线。**它不依赖目测** —— 线的位置由程序算、
行尾由渲染产生，两者之差就是真实偏差。此前十几轮靠截图数格子得出的
「偏了 1 个字符 / 2 个字符」等结论**都不可靠**（目测误差比偏差本身还大），
这个工具就是为了替掉那种判断方式。**后续修这个问题都应该先看这条线。**

### 当前怀疑（尚未验证）

19% ≈ 字号差一档。渲染用的是 `CurrentState.FontPaint`，其 `TextSize` 来自
`PlatformCanvasState.ScaledFontSize`；若它在我们把 `canvas.FontSize` 设为 13（dp）
之后又按密度缩了一次，渲染字号就会比测量用的大，差值比例正好对得上。
下一步：读 `PlatformCanvasState.cs` 看 `ScaledFontSize` 的定义。

### 附带说明

内置的 Sarasa Mono SC（24MB）目前在 **Android 上用不上**（见根因 2），先保留，
等「自绘渲染」方案启用 —— 那需要绕开 `AttributedText`，自己建
`SpannableString` + `StaticLayout` 并**同时用于绘制与测量**，才谈得上真正同源。


## v0.96.114 (2026-09-13) — 编辑器：滚动条 + 双指缩放 + 宽度链路重构

### 一、滚动条（自绘）

内容超出视口才出现；静止时细（2.5pt）而淡，**按住或拖动时变粗（6pt）变浓**；横向、纵向各一条，
都能手动拖拽改变显示位置。几何由 `VerticalThumb`/`HorizontalThumb` 产出，
**绘制与命中测试共用同一份** —— 各算一份的话「看到的滑块」和「点得中的滑块」会错位。

三个坑：

- **画在 `Height` 底部会被状态栏压住**。画布的 `Height` 一直算到内容区底边，而底部紧挨着的就是
  状态栏那一行；留白 3pt 时滚动条正好被盖在下面，表现为「加了滚动条却看不见、也点不中」⇒ 留白改 16pt。
- **起点不能贴屏幕左缘**。那是系统的边缘返回手势区，滑块停在最左时手指按上去会被系统截走
  （实测「拖滚动条直接退出了编辑器」）⇒ 横向轨道从行号栏右侧开始。
- **几何尺寸必须与绘制同源**。`VisualElement.Height` 在绘制之外读到的值与绘制时用的不一定相同。

### 二、双指缩放字号

`PinchZoomed` → 改字号（9~28，钳制并取整）→ 重测字宽与行高 → 存盘，与菜单里的加大/缩小共用
同一套收尾。手势由触摸事件的多点信息驱动（`GraphicsView` 已转发平台触摸，不另叠 `PinchGestureRecognizer`）。

### 三、「字符位置 → 横坐标」收成一个真源

原先 5 处各写各的（自绘光标、错误波浪线、横向滚动上限、编辑行可见保证、点击换算），且都按
「字符数 × 单字宽」估 —— 中文宽度不是 ASCII 的整数倍（手机上实测约 **1.75 倍**，不到 2 倍），
估算在中英混排行里越往右偏得越多。

现在统一走 `MeasurePrefixWidth`，并**改为「整段前缀一次测量」而非逐字符累加**：

> 实测 1K 字符中英 emoji 混排行：逐字累加 **9771** vs 整行一次测量 **10512** —— 差 **7%**。
> 「每个字符的 advance 之和」并不等于字体的实际排布。累加值偏小 ⇒ 横向滚动上限偏小
> ⇒ 拖到最右也到不了行尾、点击位置越往右偏得越多。

`CharIndexAtX`（点击 → 字符下标）随之改为**二分前缀查找**（按码点边界对齐，不切代理对）。
超长行（> 8192 字符）仍退回累加，免得为一条 4MB 的行分配整段前缀字符串。

顺带修掉测量里的固定余量：直接拿 `GetStringSize("0")` 当字宽，那个值含一份**与字数无关的平台余量**，
逐字符累加 1000 次等于把它放大 1000 倍。改用「n 个字 − 1 个字」消掉常量项。

### 四、两条踩坑

- **`Color.FromArgb("#000000AA")` 是全透明的**。MAUI 按 `#AARRGGBB` 解析（alpha 在前），
  写成习惯的 `#RRGGBBAA` 会让「黑色半透明」变成 alpha=0x00。调试 HUD 因此一直「看不见」
  ——背景透明，白字又画在白底上。本仓库第二次踩这个坑（v0.96.113 记过一次）。
- **调试读数只在 DEBUG 构建里**。状态栏探针与 HUD 探针都会在每次绘制时量一遍整行，
  1K 字符的行并不便宜，用 `#if DEBUG` 收敛。

### 已知未解决

**非 ASCII 字符的测量宽度与渲染占位仍不一致**：纯 ASCII 行点击精确，而中英/emoji 混排行
（22 字符）点击偏约 1 个字符，偏差随非 ASCII 字符数累积 —— 指向 `GetStringSize` 的测量路径与
`DrawText(AttributedText)` 的渲染路径，对 `monospace` 缺字形的字符（中文、emoji）**回落到不同字体**。
下一步：给编辑器打包一个含中文的等宽字体（Sarasa Mono SC，中文恰好 1em = 2 列），从根上消除 fallback。

**v1 未做**：跨行退格/删除、>2000 万行块索引、单行 >4MB 截断、iOS 实机验证。


## v0.96.113 (2026-09-13) — 移动端编辑器重做：自绘 + 单行编辑（100MB 可打开）

原来的编辑器是「透明 `<Editor>` + 垫底高亮 `<Label>`」，**整条链路都是全量处理**：打开走
`File.ReadAllBytes` + 全量解码；每次击键对**全文**重新高亮、`Split('\n')`、逐 rune 算宽、
拼行号串；撤销栈存**全文快照**（最多 200 份）；打开前还要白白全量读一遍做「二进制检测」
（那段其实是死代码）。结果稍大的文件就卡，100MB 必然 OOM。

本版把它整个换掉：**虚拟化自绘 + 单行编辑**。

### 一、数据层：永不把整份读进内存

`WayCoder/Infra/LargeTextFile.cs`：

| 类型 | 用途 |
|---|---|
| `TextSourceFactory.OpenAsync` | 头部 4KB 采样判编码 → 按大小选来源 |
| `EditableLines` | ≤ 只读阈值（默认 2MB）：内存行表，**可编辑** |
| `IndexedTextSource` | 更大：字节级行索引 + 按行解码 + LRU 行缓存，**只读** |
| `MemoryTextSource` | 空文件与 UTF-16/32（换行非单字节，索引路径不成立） |

**实测（本机 82MB / 180 万行代码文件）**：打开 **35ms**，内存增量 **14MB**（远小于文件本身）。

关键性质：**按 `0x0A` 扫字节切行对所有目标编码都安全** —— UTF-8 多字节序列、GB18030 的双字节
与四字节、Big5 / Shift-JIS / EUC-KR 的续字节范围**都不含 0x0A**。所以每行的字节区间本身就是
一个完整编码序列，**按行解码不需要跨块状态机**。这条由自测钉住（否则整个数据层都建立在猜测上）。

编码探测改成**采样**：`Decoder.Convert(flush: false)` 允许尾部半个多字节序列，不会因为采样
把字符切一半就误判成 GB18030（全量试解会）。

### 二、渲染层：只画可见的几十行

`WayCoder.Maui/Controls/CodeCanvasView.cs`（`GraphicsView` + `IDrawable`）：虚拟滚动、行号栏、
语法彩色高亮（含**运算符与括号标点**）、错误波浪线、选择高亮、超长行窗口化。每帧代价与文件
大小无关。

几个刻意的选择（都踩过或查到实证）：

- 文本与行号都用 `DrawText(IAttributedText, …)` 而**非** `DrawString`：Android 的 `DrawString`
  每次调用都新建一个 `StaticLayout`，且 y 语义与 iOS 差一整个字号。
- 触摸走 `GraphicsView` 自带的 Start/Drag/EndInteraction，**不叠** `PanGestureRecognizer`
  （平台已经把同一批触摸转发了，再叠一层等于同一手势被两套代码处理）。
- 滚动自己做，**不用** `ScrollView` 包巨型画布：250 万行 × 18pt 的内容高度远超 View 尺寸上限，
  而且 Android 滚动时子 View 不重绘。
- 行高固定、不算平台行高：「第 N 行 → y」必须能精确算出来。

### 三、单行编辑：显示层只有一套

所有行自绘，**只有光标所在行在编辑时**有一个原生 `Entry` —— 但它**文字透明**，只负责
IME 拼音组合、软键盘、系统复制粘贴菜单；**文字与光标都由画布画**。

一开始不是这么做的（让 `Entry` 显示那一行、画布跳过它），结果用户实测「编辑行错位」——
两层各按自己的规则算位置（画布用「行顶 + 基线补偿」，`Entry` 用它自己的内边距），本来就对不齐。
改成单层显示后从根上消除了这类错位。

**光标也收进自绘**：`Entry` 的文字**和光标**都透明/隐藏（Android `SetCursorVisible(false)`），
光标改由画布画（深色底纯白、浅色底纯黑，2.5pt）。理由同上一段：系统光标的位置取决于平台
自己的内边距与行内对齐（Android 单行 `EditText` 默认垂直居中），算不出也就对不齐，实测表现
是「光标在光标行的下方乱飘」。另外 Android 的系统光标跟随主题色（Material 紫），在深色代码
背景上很不显眼。

**点击定位到字符**：点击只定位到行是不够的 —— 带上行内横坐标，经
`TextEditorMath.VisualColToSourceIndex` 换算成字符下标（**先换成视觉列再换字符下标**，
CJK 占两列这件事才算得进去；直接按字符数算，中文行会偏出好几格）。实测：点行中部落在
`=` 之后、点中文行中部落在两个汉字之间，插入与退格都落在正确位置。

**滑动即结束编辑**：编辑态下浮着一个输入框，一滚动它就和自绘的行对不上；而滑动本身就是
「我要浏览，不是在打字」。所以一开始拖动（移动超过 8pt，单击不算）就先提交这一行再滚。

**字符宽必须实测**：行号栏宽度、点击→字符下标、光标位置、超长行窗口化全依赖它。
曾经只在调试 HUD 里测，而 HUD 默认关闭 ⇒ 从没测过，点击定位就总是偏几个字符。

**滑行惯性**：原先的速度是拿「按下到松手的总位移 × 4」估的 —— 既不是速度也不是任何有意义
的量：轻轻一甩位移小 ⇒ 估出的速度接近 0 ⇒ 几乎不滑，而按住拖很远再松手反而窜出去。
改成「最近 100ms 内的位移 ÷ 时间」，摩擦 0.98（滑行约 50 倍单帧位移）。

### 四、踩到的坑（都是真机才暴露的）

1. **`AttributedTextRun` 范围越界直接崩**：空行时 `Syntax.Tokenize("")` 返回的是一个空格 token，
   而文本长度是 0，于是 run `(0,1)` 越界 —— Android 会把它喂给 `SpannableString.setSpan`，
   抛 `IndexOutOfBoundsException`。现在 run 范围强制夹在文本长度内，空行干脆不调 `DrawText`。
2. **`Color.FromArgb` 的 8 位十六进制是 `#AARRGGBB`（alpha 在前）**，不是 `#RRGGBBAA`。
   写成后者，「白色 6%」会变成 **alpha=FF 的不透明黄色**（当前行顶出一条刺眼黄条），
   「黑色 6%」变成全透明等于没画。
3. **高亮条与文字必须用同一个 y**：文字落笔点带了基线补偿，条少了这个偏移就整体偏上一截。
4. **`DrawText` 的 y 落在基线上**（不是行顶）：第 1 行会被画到画布上方看不见，后面各行因为
   行高 18 > 字号 13 而看不出来。统一加一个 `TextBaselineOffset` 补偿。
5. **惯性速度不能用「总位移」估**：原来写的是 `-(总位移) × 4`，既不是速度也不是任何有意义的量 ——
   轻轻一甩位移小 ⇒ 估出的速度接近 0 ⇒ 几乎不滑，而按住拖很远再松手反而窜出去。改成「最近
   100ms 内的位移 ÷ 时间」，摩擦系数从 0.94 调到 0.98（滑行距离约 50 倍单帧位移）。
6. **`DiagnosticManager` 在移动端没有数据源** —— 它依赖 `LintTool`，而 MAUI 里那是桩。
   所以**语法错误波浪线目前不会显示**：绘制代码在，喂给它的数据是空的。要做实需补一个
   移动端的轻量诊断（如未闭合括号/引号）。

### 五、其他

- 打开大文件时显示「文件打开中…（正在建立行索引）」遮罩：100MB 要顺序扫一遍，没有反馈
  用户会以为是「没点到」或者「卡死了」。
- 保存改**原子写**（`.tmp` + `File.Move`）并保留原编码与换行风格；旧实现是 `File.WriteAllText`
  直接覆盖，写一半崩掉原文件也没了。
- `FilesPage` 打开前的全量预检换成 `ProbeText`（只读头部 8KB）。
- `AndroidManifest`/`MainActivity` 加 `WindowSoftInputMode = AdjustResize`（否则软键盘
  可能选 `adjustPan` 整窗上推，滚动偏移与屏幕 y 的换算全错）与 `ConfigChanges.Keyboard`。

自测 **5679 通过 / 0 失败**（新增 `[大文件索引]` `[大文件编码]` `[编辑器数学]` `[编辑历史]`
`[可编辑行集合]` `[大文件冒烟]` 六节）。

**v1 明确未做**：跨行退格/删除（需平台 `InputConnectionWrapper`）、>2000 万行的块索引、
单行超过 4MB 的部分（截断显示）、设置页里的编辑器入口（配置存储已就绪，默认 2MB）。

## v0.96.112 (2026-09-13) — 思考折叠 + 聊天区行数上限

本版两块：**① 思考折叠** —— 推理正文不再留在聊天流里，定稿只留一行「💭 已思考 N 秒」，
点开看全文；**② 聊天区行数上限** —— `MaxChatLines`（默认 500）。

收益不在「少显示几行」，而在**推理正文彻底不进渲染层**：一段 50K 字符的推理 ≈ 上千行，
留在 `TuiListView` 里就是每次布局、滚动、重解析都要付的钱。

23 文件（新增 6）；自测 **5617 通过 / 0 失败**（**+47 条护栏**）。

### 一、思考折叠：正文不进渲染层

对齐 Web（一行胶囊 + 点开浮层）与 MAUI（`ChatRole.Thinking` 泡泡 + 详情页）：

```
  你    修一下这个 bug
  💭 思考中 3s          ← 思考中正文实时滚动可见（观感不变）
        用户想要的是…先看 ReadFile 再改…
  💭 已思考 12 秒        ← 定稿：正文行整项移除，点它 / Alt+T 看全文
  💡 Edit(main.c)
  道码  已修复。
```

- **解析规则单源是 Web 的 `handleToken`**，C# 版落在 `UI/Shared/ThinkStreamParser.cs`：
  `«dim»` 开、块内新开标记**逐层配对**（LLM 超长时注入的 `«orange3»…«/»` 不该结束思考）、
  **块外的 `«/»` 必须原样进正文**（它是所有 «» 标记的统一结束符，剥掉会让渲染器失配）。
- **正文去哪了**：只留内存（`ChatMsg.Reasoning`，50K 尾部窗口，与 Web/MAUI 一致），**不落盘**；
  点那一行或按 `Alt+T` 打开 `ThinkDetail` 窗口看全文（剥掉 `«»` 标记后按窗口宽折行）。
- **槽位路径同构**：`AgentSlot` 缓冲侧走同一套分流，切回槽位时重建为折叠态 ——
  否则正文里会躺着裸 `«dim»/«/»`。切走时进行中的思考就地定稿，免得切回来是一条永远「思考中」的死行。

### 二、聊天区行数上限

新增 `Config.MaxChatLines`（默认 500，设置页 / `/config` 均可改）：总行数超限时丢最旧显示项，
一次删到 **80% 低水位**（滞回 —— 逐条删的话每次都要重算整表坐标，代价比不裁还高）。

按**行数**而非条数：条数上限 1000 对「一条消息顶几百行」无感。流式进行中跳过裁剪
（那时最后一项就是流式项，删到只剩它等于清空历史）。Agent 会话与会话文件不受影响，只裁显示层。

### 三、验证

新增 `Test/scripts/chat_perf.txt`（`--keypad` 驱动真实 TUI）：填满 → 继续追加 → 上下翻页 → 思考折叠全流程。

| 场景 | 实测 |
|---|---|
| 满行数下追加 200 条 | 13ms |
| 满行数下翻页 400 次 | 5ms |
| 满行数下滚动 + 渲染 50 帧 | 6ms |
| 稳态帧写出字节 | 39B（只落动态栏与光标行） |

`Alt+T` 已进键表（`Ctrl+N`/`Ctrl+O` 分别被换 connect / 交换大小模型占用）。

### 四、移动端打包

MAUI 的 `ApplicationDisplayVersion` 同步到 **0.96.112**（此前停在 0.96.40，与桌面版脱节）；
新增 `WayCoder.Maui/build-apk.sh` 固化 Release APK 的签名参数（`.NET Android` 的 Release 默认
产出 `.aab`，不加 `-p:AndroidPackageFormat=apk` 拿不到能直接安装的 APK）。自签名 keystore
已加入 `.gitignore`，升级包必须用同一个签名，否则只能卸载重装。

## v0.96.111 (2026-09-13) — 行内问答 + 语法高亮重做 + 工具行/动态栏/状态栏

本版三块：**① 行内问答**（CLI/TUI 的确认类交互全部去弹窗）、**② 语法高亮重做**（token 类别
6 → 10、新增 Windows 批处理、配色对标 One Dark / Crush、工具输出按文件后缀上色、围栏容错）、
**③ TUI 细节**（工具行统一格式与折行、动态栏右对齐 + 子智能体数、状态栏路径居中 + 槽位方括号）。

26 文件；自测 **5480 通过 / 0 失败**（**+27 条护栏**）。

### 一、行内问答：CLI/TUI 不再弹窗

对标 Claude Code 的 permission prompt，把确认类交互从「居中模态弹框」改成「输入框下方的
文字选择栏」——❯ 箭头指示、选中行黄底、每项一行说明：

```
╭──────────────────────────────────────────────────────────────╮
│ ▶ 权限    范围                                                │
│     读取              只读文件                                │
│     写入              修改文件                                │
│     其他（自行输入）  输入自定义答案                           │
│ ❯  跳过此题          不作答，直接下一题                        │
╰──────────────────────────────────────────────────────────────╯
  ↑↓ 选择 · Enter 确认 · Esc 取消 · Y/N/A 单键 · 多选 Space 勾选 · ←→/Tab 翻页
```

- **位置**：输入框**下方**（`InputBotBorder` 与 `ModelInfoRow` 之间）。不放上方是因为那里与
  `/ @ ! #` 前缀提示共用 `InputArea.KeyHook`、且是「Enter 回填输入框」的语义，放一起必打架。
- **不阻塞主循环**：`UxHelper.RunInlineChoiceOnScreen` 走 `RenderWait(win: null)` ——
  后台 Agent 线程只 `Sleep` 等事件，渲染与键路由仍归常驻主循环（顺带绕开 `win.Screen == null`
  那条过早返回的判据）。
- **键位**：`↑↓/Home/End` 移动、`Enter` 确认、`Esc` 拒绝、`Y/N/A` 单键、`1-9` 直选。

**四种形态**由「题目数 × MultiSelect × 是否显示标签页」组合而成，不再各写一套：

| 形态 | 触发 |
|---|---|
| 多选一 | 单题 + 单选（权限确认） |
| 多选多 | `Space` 勾选，`[x]`/`[ ]` 前缀 |
| 横向多页 | 多题 + `showTabs` → 页头 `▶ 权限  范围`，`←→`/`Tab` 翻页 |
| 分步骤 | 多题 + 无标签 → 页头 `步骤 1/2 · 标题`，`Enter` 逐步推进 |

已改行内的入口：权限确认、计划审批、粘贴确认、通用确认、退出确认、设置页 select 项
（就地展开，不再 `TuiDialog.Select`）。`ask_user_question` 在 TUI 下也改成一次问完所有题目。

**Web / GUI / MAUI 仍走弹框**（有意为之，不是没做）：分界点就是
`TuiManager.Instance.ActiveScreen is ChatScreen` —— Web/GUI 不 `PushScreen`、MAUI 的
`TuiManager` 桩 `ActiveScreen` 恒 null，三端一律落到 `UxHelper.WebInteraction` 桥。

### 二、代码配色对标 One Dark / Crush

`Syntax` 从标准 16 色（青/绿/黄/品红）换成 **256 色**的柔和中间调：关键字紫 `#c678dd`、
字符串柔绿 `#98c379`、注释暗灰 `#5c6370`、JSON 键粉红、数字橙、标签蓝。常量改为**语义名**
（`Keyword`/`Str`/`Comment`/…），旧颜色名保留为别名，调色只动一处。

顺带修一个跨端老问题：MAUI 的 `ColorForToken` 只认 16 色 + 真彩，**256 色一律 fallback
成默认色**（代码高亮全灰），此前是「用到哪个色往表里补哪个」必然漏 —— 改成 xterm 256
调色板算法（6×6×6 立方 + 24 级灰阶）全覆盖，四个端从此一致。

**覆盖面补强**：光换配色不够 —— 聊天区「看着没高亮」的真正来源是**语言识别失败**，
两个口子都堵上了：

- **别名表**补 `c#` / `jsx` / `python3` / `console` / `htm` / `hpp` / `cc` 等模型常写、
  但原表里没有的标签（`​```c#` 此前直接落到 `Plain()`，因为它既不在 `ByLanguage` 表里、
  `ForFile("c#")` 又取不到扩展名）。显式写 `​```text` / `log` / `output` 的仍保持纯文本。
- **无标签时改走「通用关键词表」而不是 `Plain`**：`Plain` 的关键字表是**空的** ⇒
  整块只剩字符串/注释/数字有色、关键字一片白，看起来就是「根本没高亮」——
  而 AI 回复的代码块**经常不带语言标签**，保守的 `Syntax.Detect` 又认不出小块片段。
  通用表收各语言共有的编程词（`public`/`class`/`return`/`def`/`func`/`const`…），
  刻意**剔掉** `in` / `is` / `as` / `and` / `or` / `not` / `from` / `do` / `end` 这类英文常用词，
  散文、日志、表格因此基本不会被误上色。

**工具输出也上色（write / edit 贴出的代码）**：这类代码**没有语言标注**，但文件路径一定有 ——
改按 `Syntax.ForFile(filePath)` 的**扩展名**定语言，比内容启发式准得多。`ContentDiffFormatter`
原先把每行整体包成 `«bright green»` / `«bright red»`（代码因此没法再上语法色），现在改为
「**行号与 +/- 标记保持 diff 语义色 + 代码部分逐 token 上真彩**」——真彩写法 `«fg:#rrggbb»`
四端都认，256 色→hex 的换算收在新增的 `AnsiTty.Xterm256ToRgb/ToHex`（MAUI 原先自己写了一份
`FromXterm256`，改走同一个实现）。上下文行保持整体灰（未改动的行不该抢眼）；行内含 `«»`
字面量时整行不上色（标记语法没有转义机制）。

> 这正好解释了「**diff 弹窗颜色是对的、聊天列表里的不对**」：弹窗（`DiffPreview`）本来就在用
> `Syntax` 上色，而聊天列表这条路径（`ContentDiffFormatter`）没接上。

**diff 展示与围栏识别的三处修正**：

- **上下文行也上语法色**：此前只有 `+`/`-` 行的代码按语法上色、上下文行整行灰 ——
  现在三种行都是「行号与标记用 diff 语义色 + 代码按语法上色」，区别只在底色。
- **`+`/`-` 行加底色**：暗绿 `#0e2a17` / 暗红 `#2c1417`（对标 Claude Code 与 Crush），
  扫读时一眼分得清增删。«» 的写法是「外层开 `bg`、内层各段 `fg` 自己开合、行尾关 `bg`」——
  标记按栈配对，顺序写反颜色就串。
- **容错识别「反引号写少了」的围栏**：模型经常把 ``` 写成**一个或两个**，形态是
  「独占一行的 `语言名」…「独占一行的 `」。按标准 markdown 这既不是围栏（要 3 个）、
  又不是行内代码（行内代码不能跨行）—— 于是整块代码当普通文本渲染。判据抽成
  `UI/Shared/CodeFence`（TUI 与 MAUI 共用，此前两处各写一套 `TrimStart().StartsWith("```")`），
  收得很紧：只认「整行只有反引号 + 语言名」且语言名形如标识符 ——
  文本里独立成行的 `` `foo` `` 不会被误吞。

  实测出现过的形态是 **1 个 / 3 个 / 4 个**反引号，而且**开闭数量还可能不一致**（4 开 3 闭）。
  闭栏判定因此放宽为「标准形态满 3 个就算闭合」—— 按标准 markdown 的「闭栏不少于开栏」，
  4 开 3 闭会找不到闭合，把后面**所有正文**都吞进代码块；容错形态（1-2 个）仍要求个数不少于开栏，
  否则行内代码行会把块提前闭合。

  > **真正的坑不在围栏判据，在段落累积**：它的终止条件里硬编码了 `StartsWith("```")`，
  > 于是「先一句说明、再贴代码」（**模型最常就是这么写的**）时围栏被当段落续行吃掉；
  > 反倒是「首行即围栏」的形态一直正常 —— 这就是为什么第一版断言（内容首行就是围栏）
  > 全绿、而 keypad 实测里同样的代码却渲染成了普通文本。

### 三、动态栏：子智能体数 + 三段重新分配

新增 `AgentTool.ActiveSubAgents`（`Interlocked` 计数），动态栏右段以 `🤖N` 显示正在跑的子智能体数。
计数包在 `RunSubAgentWithRetryAsync` 的**整个重试周期**外 —— 重试期间仍是同一个子智能体在跑，
放在方法内层会让数字中途归零再涨、闪一下。

显示位置在 `📊` 上下文占比**之前**：靠后的项有 `HasRoom` 宽度保护，放前面能保证「正在并行干活」
这个信息不被挤掉。**仅 >0 时占位** —— 多数时候没有子智能体，不该在右段留一块空白。

> 这条的断言差点写成帧快照：活跃屏幕上内容变化走**段级直写**（直接写终端、同步更新段缓存，
> 于是紧随的 `OnRender` 认为「无需重写」），`LastCleanFrame` 里根本没它 —— 只能从产出侧验
> （`BuildRightItems` 改 `internal` 直接断言）。**测渲染管线时先想清楚「这段内容走的是直写还是帧渲染」**。

**三段宽度重新分配**：左段（状态文字）从 `1/3` 压到 `1/5`，中段（工具命令）吃掉**剩余全部宽度**，
右段（`📊⚡🔤¥`）改为**右对齐**贴右边缘。此前三段各占 1/3 —— 中段放的是 `bash` 命令这类长文本，
固定 ~29 列根本不够；而右段内容宽度不定，左起排会在右边留一块空白。右对齐的实现是
「先按优先级从左起排 → 再整体右移贴边」，这样原有的丢项优先级（放不下时丢靠后的）保持不变。

几何计算抽成 `ComputeGeometry`，`OnRender`（整行/段渲染）与 `RenderDirect`（直写）**共用一份** ——
此前两处各硬算 `Width/3`、`Width*2/3`，改一处忘另一处就会「直写位置与整行渲染位置对不上」，
表现为内容串位／闪烁。

**底部状态栏的路径改为居中**：此前紧跟左侧槽位条（`1 2 3 … 10`）左对齐，路径一长就显得挤。
现在在「槽位条之后」到「右侧 Token 之前」这段区间里居中；区间不足时沿用原有的**保尾部**截断
（路径要保住项目名 `…/my-coder`，不能保开头的 `/Users/…`）。

**槽位指示改样式**：从「纯数字 + 白底标当前」改成 **`[N]` 方括号框住当前槽位** ——
白底/纯颜色在浅色主题与色盲下都不够明确，括号是**字形层面**的区分，任何配色下都认得出
（状态色仍保留：绿=工作 / 黄=等权限 / 红=出错）。第 10 个槽位显示成 **`0`** 而不是 `10`：
个位等宽，槽位条不会因个位/两位混排而参差，切槽位时后面的内容也不左右抖
（`…8 9 0` 也正好是数字行的常规写法，0 对应 F10）。

```
 [1] 2 3 4 5 6 7 8 9 0        📁 ~/Desktop/source/mycoder/my-coder/WayCoder
```

**工具行统一格式**：此前 7 个渲染器各写一套「emoji + 小写名 + 参数」（`✏️ edit x` / `📝 write x` /
`💻 bash x` / `📖 read x` / `🔍 …` / `🤖 agent x` / `⚙ …`）—— 图标不统一、大小写也不一，
在聊天流里一眼扫不出「这是工具调用」。现在统一成：

```
💡 Edit(a.cs)
+public class A { }
```

- 图标固定 `💡`；名称去 `_file` 后缀再 snake→Pascal（`edit_file`→`Edit`、`multi_edit`→`MultiEdit`），
  **加粗染橙**；参数降为灰色并用括号括起 —— 与下面的内容行拉开层次
- **工具行与内容行分开**：内容此前经 `AppendToLast` 追加到**工具标题那条消息**上，多行输出的首行
  会紧贴标题（`🔧 Edit(a.cs)  +public class A…`）。现在标题之后的首个输出块**另起一条消息**
  （`_pendingToolBody` 懒创建 —— 无输出的工具不会白多一个空气泡）

**语法高亮补全 token 类别（6 类 → 10 类）**：

| 类别 | 色值 | 说明 |
|---|---|---|
| 运算符 | 青 `#5fafaf` | `= == != < > + - * / && \|\| =>`，连续同类合并成一段 |
| 括号标点 | 蓝灰 `#afafaf` | `() [] {} , ; :` |
| 字符字面量 | 深绿 `#87af87` | `'a'` —— 与字符串 `#98c379` 同色系但分得开 |
| 函数名 | 蓝 `#61afef` | 后跟 `(` 的标识符 |
| 类型名 | 黄褐 `#d7af87` | 首字母大写的标识符（类 / 结构 / 常量） |
| 变量 | 柔红 `#d78787` | `$VAR` / `${VAR}` / `%VAR%` |
| 标识符 | 亮灰 `#dadada` | 变量 / 字段 / 参数名 —— **不用 `Default(0)`** |

**标识符只挑两类上色**（函数、类型名），其余给**明确的亮灰** —— 全上色会整屏都是彩的、反而没重点；
但也不能用 `Default(0)`（= 终端默认前景）：**暗色终端的默认前景本身就偏暗**，会和注释
（`#5c6370` 暗灰）糊成一片，用户实测「标识符和注释一样、分不出代码与注释」。
现在注释 241 / 标识符 253，相差 12 级灰阶，暗背景下对比明显。

**新增两种批处理语言**：

- **Windows 批处理**（`.bat` / `.cmd`）：注释是 `REM` / 行首 `::`，变量是 `%VAR%`，
  关键字含 `if/else/for/goto/call/set/setlocal/equ/neq/lss/geq…` ——
  与 Shell 的 `#` 注释 / `$VAR` 是**两套语法**，不能共用一个定义（`echo 100%` 里的百分号
  会被当成变量头，已在 `Tokenize` 里按语言分开判定）
- **Linux Shell 关键字表扩充**：47 词 → 约 110 词（补上 `until/select/coproc/shift/trap/alias/
  test/printf/read/ulimit/umask/rsync/tar` 等），并支持 `$VAR` / `${VAR}`

**bash 工具行的参数按 shell 语法上色**：`💡 Bash(dotnet build -c Release | grep -i error)` 里
选项、引号、`$VAR`、管道都认得出；其他工具的 brief（路径 / 描述）仍是统一灰。

**语法高亮补运算符与括号标点**：此前只认关键字 / 字符串 / 注释 / 数字 / XML 标签 / JSON 键 ——
现在运算符（`= == != < > + - * / && || =>`，**连续同类合并成一段**）与括号标点
（`() [] {} , ; :`）也各有其色：运算符青 `#5fafaf`（256 色 73）、括号标点蓝灰 `#afafaf`（145）。

**纯文本（`Plain`）不开符号着色**（`HighlightSymbols = false`）—— 否则散文里的破折号、括号、
冒号会整篇变色，比不上色还难看；通用兜底表（无语言标签的代码块）仍开。

> **Web 端有一套独立的 JS 高亮**（`app.js` 的 `highlightCode` + `style.css` 的 `--tok-*`），
> 与主工程 `Syntax` 是两套实现（跨语言没法共享代码）。它此前只有 `tok-kw/str/num/fn/com`，
> 同样缺运算符与括号 —— 一并补上 `tok-op` / `tok-paren`，色值向主工程对齐。
> **改一边记得改另一边**：这正是本仓库反复出现的「同一规则两处实现」，而跨语言这层无法靠共享代码消除。

**聊天裁剪的两个问题**（用户报告「消息多了卡、大部分内容丢失」）：

- **裁剪顺带删了 `ChatMessages`** —— 那是**槽位的消息列表**（会话保存与切槽位重放的来源），
  删了就等于「聊久了历史真的没了」。方法注释写着「只裁显示层（ChatMessages/ChatList）」
  是自相矛盾的；两者本就不是一一对应（`ChatList` 还含工具气泡，`ChatMessages` 只有
  user/assistant/system）。现在只丢显示项。
- **token 估算每次 `AddMessage` 都全量遍历** —— 1000 条长消息下就是每轮十几 MB 的字符串扫，
  O(n²) 累积。改为每 32 条估一次，精度足够、开销降两个数量级。

**侧栏：内容一变就整块重绘**：侧栏是「**一个分区高度变化 → 后面所有分区整体位移**」的典型，
而 `TuiSidePanel.OnRender` 此前只逐行写、**没有整块清空** —— 内容变少时旧行留在屏上，
后面的分区也停在原处（用户实测「侧边栏有内容改变，之后就布局乱了」）。
现在渲染开头先把整个面板用背景色擦一遍再画，配合调用方的标脏即等于整块重绘。

**粗体是粘性的 —— 段尾必须显式关**：`SGR 1`（粗体）与 `SGR 22`（取消粗体）是一对，
只发前者会**一直生效到段尾之后** —— 工具行 `«bold»«orange»Edit«/»«/»` 后面那段参数、
乃至下一行都跟着变粗（用户实测「工具参数字体好像也被加粗了」）。
`TuiMarkdown` 的段渲染现在跟踪粗体状态：粗体段一结束就发 `AnsiTty.SgrNoBold`，行尾再兜一次底。
（代码块等非粗体段同样受益。）

**工具消息四处调整**：

- **括号里只给值、不带 `key=`**：`edit(file_path=main.c)` → `edit(main.c)` ——
  参数名对用户没有信息量（他知道自己刚让 AI 做什么），还会把本就不宽的一行挤满；
  多参数时值里的路径也一并缩写
- **参数长度不再限制**：`ToolDisplay.Brief` 原默认截 60 字符，现在默认**不截断** ——
  交给我们自己的折行（见下），bash 命令、长路径不会再被切掉尾巴
- **工具名加粗终于生效**：`«bold»«orange»Edit«/»«/»` 里样式码 `1` 被内层的 `«orange»`
  **覆盖**掉了（中间格式的段模型是 `(Text, Fg, Bg)` 三元组，**没有独立的样式通道**，
  解析栈只存 Fg/Bg）。修法是把粗体编进颜色高位 `AnsiTty.BoldFlag = 0x2000000`：
  `«bold»` 置该位、颜色码保留该位，`FgCode`/`FgBgCode` 见到就先发 `SGR 1` 再发颜色 ——
  这样不用改动所有段的消费方（TUI/MAUI/Web 各自的渲染器）。MAUI 侧同步剥位 + 设 `FontAttributes.Bold`
- **bash 工具行的参数按 shell 语法上色**（见上）

**工具参数改为折行完整显示**（原来是按宽截断 + 省略号）：bash 命令、文件路径截掉尾巴就看不全了。
现在超宽时在 `FormatHeader` 里折行，续行缩进对齐到 `(` 之后：

```
  💡 Read(src/很长的中文目录名/AnotherLongPath/文件.cs --option value
          --another-option-here)
```

折行有三个要点：**在 «» 标记之外折**（`«grey»` 必须整段保留，按显示宽硬切会切出字面量）、
**每行各自闭合**（plainText 路径是逐行解析 «» 的，跨行标记对不上）、
**按显示宽度折且不切断宽字符**（CJK/emoji 宽 2）。另外优先在空格 / 路径分隔符处断行，
但回退距离超过行宽 1/3 就不回退 —— 否则一个长单词后面跟着空格会把上一行折得只剩几个字符。

**keypad 实测又抓到两个**（为它加了 `TOOL:` / `TOOLOUT:` 两条指令，此前 keypad 驱动不了工具行）：

- **bash 的标题行把 «» 标记原样打了出来**：
  `│ 🔧 «bold»«yellow»Bash«/»«/»«grey»(dotnet build -c Release)«/»` ——
  `AddToolProgress` 给标题行也设了 `ShellBlock = toolName == "bash"`，而 ShellBlock 的渲染路径
  **故意不解码 «»**（bash 输出是 OS 原文，那里的 `«`/`»` 只是普通字符）。标题是我们自己生成的
  标记，不该走那条路 —— **竖线 gutter 只属于内容行**（由 `_pendingToolShell` 决定）。
- **缩进差 2 列**：标题 label 里手写了 `"  "` 前缀，而 `AddMessage(indent: 1)` 还会再加一次，
  于是标题 4 列、内容 2 列，看着对不齐。手写前缀去掉，缩进统一由 `indent` 加。

> 这两条自测都测不到：前者的触发条件是「bash 工具」（自测里用的是 edit_file），
> 后者要看**画面**才知道差 2 列。**「格式对不对」这类问题，keypad 逐帧看比断言快得多。**

**顺带补了 Web 端的橙色**：`app.js` 的 `MARKUP_STYLES` 此前只登记了 `orange3` 没有 `orange` ——
`«orange»` 在浏览器里会**静默失色**（色名不认识就返回空样式，不报错）。这类「色名写错 / 某端色表
没登记 → 整段无色」只能靠**解出来的色值**发现，已加断言钉住（名称解析为 `AnsiColors.Orange`、
参数解析为 `BrightBlack`）。

**代码块 / diff 的底色铺满整行**：此前底色只裹住文字，右侧留一段断口 —— diff 的红绿底尤其明显，
看着像没画完。现在在 `RenderMessage` 出口统一把**有底色的行**补空格到渲染宽度（竞品的代码块与
diff 底色都是铺满整行的）。取行内**第一个非零背景色**作为该行底色，已超宽的行不补（长行本就会折行）。
两条出口 —— 工具输出的纯文本路径与 markdown 代码块路径 —— 都收口到同一个 `FillRowBackgrounds`。

### 四、六项交互差距修复

① **「仅本次允许」被误记**：`SmartAuto` 的 Cautious 分支只看了 `allowed`（bool）、没看结果码，
   于是 0（仅本次）和 1（全部允许）**都会被写进 `AutoAllowed`** —— 与选项文案正好相反。
   `ShowConfirmDialog` 改返回 `(bool, int)`，只有 `code == 1` 才记账。

② **问卷「其他（自行输入）」**：模型的选项未必覆盖用户想法。选中后收起选项栏、**复用输入框**
   （键位照常流进输入框，只在 Enter/Esc 上拦），结果用 `SurveyResult(Picks, Others)` 回传。
   踩坑：Enter 会被 `HandleSpecial` 的「发送消息」先截走（输入态时选项栏已收起、
   `InlineChoiceVisible` 为假）—— 表现是「打完自定义答案一按回车，答案当成聊天消息发了出去」。

③ **权限确认给文件级 diff**：`edit_file`/`write_file` 改走 `UnifiedDiff.Generate`
   （读原文件 → 应用替换 → 统一 diff），取代原来各截 80 字符的 `-old/+new`。
   读不到文件/替换不生效时退回子串预览。这条是**四端共享**的：Web/GUI/MAUI 的弹框也一并受益。

④ **栏内常驻快捷键提示行**：`shortcutRow` 在行内栏可见时切为选择栏键位，隐藏时还原 ——
   否则键位只活在 Ctrl+H 帮助面板里，第一次遇到权限确认的人不知道能按 Y/N/A。

⑤ **计划审批三态**：`批准并自动接受编辑` / `批准，但每次编辑都问我` / `继续规划`。
   前者置 `PermissionManager.AllowEditsThisSession`，**只放开** `edit_file`/`write_file`/`multi_edit`
   —— bash、rm、kill 照旧逐次确认（对齐竞品 auto-accept edits 的语义）。

⑥ **问卷「跳过此题」**：单选页末尾追加（多选页空选本身就是跳过，不重复占行），该题结果为空列表。

### 四、keypad 新增 `INLINE:` 指令

`Test/Keypad.cs` 加 `INLINE:perm / permdanger / survey / step`：刻意直接调
`ChatScreen.ShowInlineChoice/Survey` 而**不**走 UxHelper 的 `RunInline*` —— 后者会
`RenderWait` 阻塞到用户作答，脚本再也走不到后面的 `SNAP`。配 `Test/scripts/inline_perm.txt`
与 `inline_survey.txt` 逐帧截图 + 结果码回放。

**它抓出了 3 个自测断言覆盖不到的渲染缺陷**（都是逐帧看画面才暴露的）：

- `TuiPromptBar` 的填充右界是 `Width-3`（不含右框内侧列）→ 旧帧画在那里的 `─` 没人覆盖，
  框内右下永久留半截横线
- 页头行只写了文本，**没按普通内容行渲染边框 + 整行填充** → 动态栏的 spinner 和 `⚡ 0%` 粘在页头行上
- 换页改变 `Height` → 下方兄弟控件整体位移，但增量渲染只重绘「自己标脏」的控件
  → 上边框被上一帧的页头文本啃出豁口 `╭─── ─ ───── ─ ───╮`。改走 `TuiManager.RequestFullRefresh()`

另修一处行为不一致：`1-9` 数字键原先只在问卷里有效（帮助面板却写了「直接选中第 N 项」）；
问卷多选页的数字键原本会直接提交（等于永远只能勾中一项），改为「切换勾选」。

> 工作区 CRLF 提醒：本次用 python 批量改的几个文件把 CRLF 写成了 LF，`git diff --stat`
> 一度虚高到 6537/5123 行。按 index 行尾（`git ls-files --eol` 的 `i/crlf w/lf`）逐文件
> 归一化后回到 1539/125。**任何批量改文件的脚本之后都要核对 `git diff --numstat`**。

## v0.96.110 (2026-09-12) — 长任务完成后卡死（冻结现场采集与 GC 线程暂停互锁）

2 文件；自测 **5453 通过 / 0 失败**（**+3 条护栏**）。

### 问题

TUI 下跑 Agent 多轮长任务，**完成后界面完全无响应**（按键、鼠标全失效），复现率很高。

实测现场（v0.96.109，StarGo 项目，进程卡住 41 分钟、CPU 99.6%）：

- 主线程 **4202/4202 个采样全部**落在 .NET GC 的 `task_threads` / `thread_get_state` /
  `thread_get_register_pointer_values` 循环里 —— 全仓 grep 确认**项目没调这些 API**，
  是运行时的「暂停所有线程」逻辑
- 九个线程（含 `waycoder-input-pump`、TUI 心跳、线程池）在 5 秒采样里**全部停在同一位置**
- 日志只留下一句：`主循环冻结 3087ms，最后活动: Render —— 现场已落盘: ` ← **路径是空的**

### 修法

① **移除「自己采样自己」**：`FreezeCapture` 在冻结时会执行 `/usr/bin/sample <自己的PID> 1000`
   抓原生栈，而 sample 的工作方式就是**暂停整个进程来采样** —— 与 .NET GC「暂停所有线程」
   正面冲突。链条：长任务后 `Render` 阶段分配大 → 触发 GC → 看门狗判定冻结并抓现场 →
   两个「暂停全部线程」互等 → 进程再也回不来。
   改为写入**手动采样指引**（在另一个终端跑 `sample <pid>`，不涉及进程内自暂停），
   并删除 `CaptureNativeStackAsync` 以免日后被误用。

② **落盘改同步**：原实现用 `Task.Run` 异步写（注释理由：慢盘不拖住心跳线程），但**卡死时
   线程池本身已被挂起** ⇒ 那个 Task 永不执行，于是「最需要现场的时候反而没有现场」
   （即上面那个空路径）。冻结 dump 是低频事件（>3s 才触发），同步写代价可接受；清理仍放后台。

## v0.96.109 (2026-09-12) — TUI 打字乱码（控制台输入代码页不是 UTF-8）

自测 **5444 通过 / 0 失败**（较上版 **+7 条护栏**）。**真机已复验**：中文 Windows（控制台代码页 936）
重新编译后在 TUI 里打中文正常 —— 说明终端认 `SetConsoleCP(65001)`，第 2 条回退解码留在兜底位。

### 症状与根因

中文 Windows 上在 TUI 里打中文，输入框里全是乱码（U+FFFD 或别的字符）。

根因是**解码侧与控制台输入代码页不一致**：`WindowsCharSource` 按 UTF-8 解码字节流（状态化拼多字节），
而 conhost 在 VT 输入模式下**用 `GetConsoleCP()` 编码非 ASCII 按键字节** —— 中文系统该值是 **936(GBK)**
（实测本机 `chcp` = 936）。`Program.Main` 只设了 `Console.OutputEncoding = UTF8`，
它调的是 `SetConsoleOutputCP`，**输入侧从来没被改过** ⇒「界面文字正常、自己打的字乱码」并存。

这也解释了为什么 v0.96.74 之前没这问题：那时 Windows 走 `Console.ReadKey`，拿的是输入记录里的
UTF-16 字符、不经过字节编码；改成统一字节流之后才暴露。

### 修法

1. **切输入代码页**（根因）：`WinConsoleMode.Enable` 在开 VT 输入的同时把控制台**输入**码页切到
   UTF-8（`SetConsoleCP(65001)`），`Disable` 还原 —— 与既有的输入模式一样是控制台全局设置、退出必还原；
   幂等分支与 `ReapplyConsoleMode` 每次进界面重施（裸 `!` 跑 `chcp 936` 会把输入页改回去）。
2. **按原页回退解码**（兜底）：切不过去时 `WindowsCharSource` 按**原码页**的字节规则切分再解码
   （DBCS = 前导字节 + 1 尾字节，`IsDBCSLeadByteEx`），而不是按 UTF-8 的字节数猜。
   这里有一条实测教训：**「先试 UTF-8、不合法再换页」是错的** ——
   ① GBK 前导字节落在 UTF-8 的 3 字节区间（0xE0-0xEF）时会白等一个永远不来的第三字节（最后一个字卡住不出）；
   ② 大量 GBK 双字节**恰好是合法 UTF-8**（`一` = D2 BB → U+04BB），先试 UTF-8 会把它们静静地解成别的字符。
   **输入字节的编码是控制台的属性，不是逐串猜出来的。**
3. 两条都兜不住就丢字节，绝不把 U+FFFD 塞进输入框；切不过去时记一行 `DebugLog` 便于排查。

新增护栏（`SelfTest.Chunk19.cs`）：GBK 三段前导字节（0x81-BF / C0-DF / E0-EF）整串还原、
形似合法 UTF-8 的 GBK 双字节按原页解、无回退时不留 U+FFFD、UTF-8 输入页下中文与 emoji 代理对不受影响。

## v0.96.108 (2026-09-12) — 思考胶囊各段独立 · 不再发空气泡

自测 **5437 通过 / 0 失败**（较上版 **+11 条护栏**）。

### 一、思考气泡「点不开」「连在一起」（用户实测：老的思考气泡都很难点开，工具气泡基本正常）

同一处显示上叠了两个独立缺陷：

1. **点击回调捕获了可变的 `think` 变量** —— 所有思考胶囊共用这一个变量。思考一结束
   `think` 被置空，此时点任何老胶囊 ⇒ 回调读到 `null` ⇒ 抛错、浮层打不开；若已有新思考在
   进行，点老胶囊又会打开**最新那一块**的内容。用户看到的就是「各段连在一起、老的点不开」。
   修法：创建胶囊时闭包捕获**自己的块对象**（`const block = …; () => showThinkDetail(block)`）——
   工具组本来就这么写，所以「工具气泡基本正常」。
2. **见到 `«/»` 就关思考块** —— 而它是**所有** `«»` 标记的结束符：推理里嵌的
   `«orange3»… 思考内容过长…«/»`（LLM 超长时注入）会把思考块就地关掉，其后的推理漏进正文段、
   还多出一个裸 `«/»`。修法：思考块按**层数**配对（`«dim»` 算第 1 层，块内嵌套标记各占一层，
   减到 0 才收尾）——Web `handleToken` 与 GUI `AppendToken` 同规则。
3. 附带修一处错位：推理前那一口换行（LLM 发 `"\n«dim»"`）过去会先建一个空正文泡排在胶囊
   **前面**（DOM 先建段、后插胶囊），顺序颠倒 —— 现在丢弃。

### 二、空气泡（用户实测：很多空泡泡，没有任何内容；只有空格或者不可见字符的泡泡，不发）

来源：LLM 每段正文开头送一口换行（思考结束那一下就是 `"«/»\n"`），渲染端光凭「来了一个
token」就建泡 —— 「模型想完直接调工具」（这一轮没有正文）于是在工具行前留下一个空泡。

- 判据收成**一份**：新增 `UI/Shared/VisibleText.HasVisible`（空白 + 零宽空格/连接词、BOM、
  软连字符、词连接符、谚文填充符、变体选择符、控制字符…一律不算内容；.NET 的
  `char.IsWhiteSpace` **不认**其中大半，必须显式列出）；Web 端 `app.js` 有孪生实现
  `isBlankText`，两侧同一张表、自测都钉住。
- 三端落地：Web `segAppend` 不因空白建泡 + `endSeg` 收尾时把「渲染完仍无可见文字」的泡
  **撤掉**；GUI `AppendToken` 同判据 + `FinalizeStreaming` 撤掉「发送时预建、这一轮没出正文」
  的空泡；MAUI token 回调同判据（仅编译验证）。
- 顺带：`/` 命令无输出时不再发空泡，改为回执 `✔ 已执行 /xxx`。

诊断脚本 `scripts/_check_web_collapse.cjs` 扩到 **37 条**断言（含「老胶囊开老内容」「嵌套标记
不关思考块」「不可见字符不建泡」），并把 DOM 桩的 `innerHTML`/`textContent` 语义补正
（渲染过的元素要读得出渲染后的文字，否则空泡检测会误判）。

## v0.96.107 (2026-09-12) — Web 不再卡死 · 思考/工具折叠 · 键位避让系统键 · 工具输出统一命令行格式

10 提交 / 76 文件；自测 **5422 通过 / 0 失败**（较上版 **+93 条护栏**）。

### 一、Web 界面卡死（用户实测：卡住一会，久了浏览器弹「页面无响应」）

**先说结论：不是编码层、也不是渲染解析层，是 DOM 层。** 先用数据排除误判——
把浏览器纯函数抠到 node 里实测（`scripts/_bench_web_render.cjs`）：ANSI 解码 4.9 万字符 **3.7ms**、
markdown 渲染 12 万字符 **30ms**、字符串拼接可忽略。都便宜得不像元凶。

真凶是每个流式 token 的两行代码：`el.textContent += s`（**重建整块文本节点**）紧随
`scroll()`（读 `scrollHeight` → **强制同步重排整页**）。把**真实函数**跑在最小 DOM 桩上计数
（`scripts/_bench_web_stream.cjs`）：

| 场景 | 整块文本重建 | 强制重排 |
|---|---|---|
| 2000 token / 5 万字符（旧 → 新） | 2000 → **1** | 2000 → **1** |
| 工具输出 400 chunk（旧 → 新） | 400 → **0** | 400 → **0** |

修法三条：流式追加改走文本节点 `appendData`（只追加新片段）；滚动合帧（rAF，且只在用户
已在底部时跟随，上翻看历史不会被拽回）；历史重放分帧（每帧 15 条）。

另外两处「像卡死」的坑一并收口：**服务端 `ask`/`diff` 超时后前端浮层永远挂着**
（服务端已按默认继续跑了，页面却点不动任何东西）→ 超时广播 `ask_closed`，前端收掉模态并提示。

### 二、思考与工具折叠（Web / GUI 默认开，对齐 MAUI）

- 💭 思考：一行 `思考中 Ns` → 定稿 `已思考 N 秒`，正文只进内存
- 🔧 工具：一行 `工具调用:N 次`；分组边界照 MAUI（出现新思考块或新正文段 → 下个工具**新开一组**）
- 点开才看详情：Web 弹模态浮层（可滚动 + 搜索，思考进行中每秒刷新）、GUI 开独立详情窗；
  输出按项预算均分（120k ÷ 项数，clamp 2k–30k），对齐 MAUI 的 `ShareFor`，避免先到先得饿死后序输出
- 顺带把正文**按工具边界分段**（AI1 / 工具组 / AI2 交错），单气泡体积受控
- **GUI 同时修掉两处卡死**：工具输出不再每个 chunk 新建一条气泡（以前一次大输出 = 成百上千条
  气泡 + 成百次强制滚动）；滚动加「自动跟底」闸门
- TUI 复核后**不动**：它本来就是「每 delta 只标脏 + 渲染帧统一 flush + 内容级脏」，
  外加单条上限与 auto 档 20 行折叠（用户已定：TUI 维持 detailed/auto/concise 三档）

### 三、外部工具输出统一按命令行格式（UTF-8 / 等宽 / 保换行 / 解码 ANSI）

Web 端实测：`!命令` 显示正常，bash 工具气泡却是**一坨乱码**。根因同样不在编码层——
四条进程启动路径（agent 的 bash、`!` 直通、沙箱、持久 shell）本来就都调 `ProcEncoding.Apply`，
C# 侧给到各端的是同一个正确 UTF-8 串；差别在**前端渲染**：`!` 走 `ansiToHtml`，
工具输出走 `markupToHtml` ⇒ ① ESC 序列被当正文印出；② shell 输出里的 `#`/`- `/`|`
被当 markdown 渲染成标题/列表/表格，大字号标题 + 折叠的连续空格把等宽列对齐全毁。

判据做成**工具自己声明**（`ITool.RawOutput`，默认接口成员）：11 个「输出是外部进程原始字节」
的工具声明 `true`（bash/git/git_pr/sqlite/ps/kill/test/lint/lsp/screenshot/job_output），
`ToolRegistry.IsRawOutput(name)` 供按名查询，Web 事件带 `raw` 标记、前端据此分派 ——
**前端不再自备工具名单**（那种平行表改一处漏一处）。移动端与 GUI 新增共享
`AnsiHelper.StripAnsi`（含 CSI/OSC/截断序列处理）：不做上色但**绝不显示乱码**。

### 四、工具行显示缩到最短（只改显示，不改参数）

`edit_file file_path=C:\a\b\c\d\main.c, old_string=…` → **`edit(main.c)`**。

新增共享 `ToolDisplay`（一处实现、四端共用）：名字缩写 `read_file→read` / `write_file→write` /
`edit_file→edit`（另含 `multi_edit`/`notebook_edit→edit`、`find_replace→replace`）；
路径取最短（绝对路径只留文件名、长相对路径留末两段）；参数摘要**只取路径主参**，
丢掉 `old_string`/`new_string` 这类噪声。真实参数照旧走 `ToolCall.Arguments`，
轨迹日志仍记全量，按真实名的能力判断（`IsRawOutput`）不受影响。

### 五、键位避让系统键与三键组合

**三键组合清零**（`Ctrl+Shift+字母` 在 Windows 上按不到：Windows Terminal 抢走 `Ctrl+Shift+P`
开它自己的命令面板；即便不被抢，VT 字节流也拿不到 Shift 修饰键）：命令面板 → `Ctrl+U`、
换 connect → `Ctrl+N`、主题 → `Ctrl+W`（轮转改 `/theme next`）；编辑器同批：
`Ctrl+Shift+O`→`Ctrl+O`、`Ctrl+Shift+F`→`Ctrl+W`、取消 `Ctrl+Shift+K`（`Ctrl+X` 无选区即整行剪切）。

**不占用系统剪切键与输入框自己的编辑键**：`Ctrl+X` 交换大小模型 → `Ctrl+O`、
`Ctrl+Y` 搜索历史 → `Ctrl+F`、取消 `Ctrl+K` 切模式别名（让回「删到行尾」）。

**`Alt+字母/数字/符号` 修好可用**：`ESC + 字符` 这条路此前把字符退回 pending、ESC 单独成键 ——
按 `Alt+T` 会「**中断 Agent** + 往输入框打一个 t」，Alt 组合键全部不可用；现在带 Alt 修饰键返回。

### 六、`dotnet run` 无参启动起不来

`launchSettings.json` 的 `commandLineArgs` 是 `"\r\n\r\n"` —— 早年删掉 `--tui-demo` 时留下的尾巴，
退化成一段纯空白；`dotnet run` 把它当**一个参数**传进来，而 v0.96.85 的严格校验把「裸位置参数」
改成硬报错 ⇒ 本仓库常规入口直接起不来。删残留 + `Parse` 忽略纯空白参数（它来自工具而非人手），
顺带把报错里的参数真身转义成一行（原来含换行的参数会把提示冲散，看不出是哪个参数）。

### 七、其它

- 全仓按键提示与说明文档同步（顺手修掉三处本来就写错的提示：`/help` 尾部的
  「Ctrl+E 编辑器」「Ctrl+R 搜索」，以及键表里从没绑定过的 `Ctrl+T / O`）
- 三个开发用诊断脚本入库：`scripts/_bench_web_render.cjs`、`_bench_web_stream.cjs`（DOM 操作计数）、
  `_check_web_collapse.cjs`（拿真实 app.js 函数在 DOM 桩上跑完整一轮折叠流程，14 项断言）
- `.gitignore` 补 `bin2/`/`obj2/` 与本地重定向日志

## v0.96.106 (2026-09-12) — 界面分发固化 · Anthropic 原生兼容 · 重复代码提炼

25 提交 / 75 文件；自测 **5329 通过 / 0 失败**（较上版 **+67 条护栏**）。
（diff 显示 `+10535 / −9464`，其中约一万行是**测试文件拆分**的搬迁，净新增约 500 行。）

### 一、启动界面分发收成单一判据

`Program.cs` 里一串 if 决定「无参数 / `-p` / `-p1` / `--web` / `--cli` / `--tui` 各进哪个界面」，
**没有任何测试钉住** —— 判据写错就会静默换界面而自测全绿。现收敛为
`UI/CLI/Arguments/StartupRouter.Decide(...)`：

| 命令 | 界面 |
|---|---|
| 无参数 | 全屏 TUI |
| `-p "任务"` | **CLI 纯文本**（与 `--cli` 同源、无 spinner 动画，输出可直接重定向 / 被 CI 解析） |
| `-p1`~`-p0` | TUI（槽位任务） |
| `--web`/`--cli`/`--tui` | **指定优先** |

顺带修掉两处**提示词被静默丢弃**：`--cli -p` 现在照常执行；`--web -p` 在 stderr 说明
「web 是浏览器界面，提示词不在终端执行」。

### 二、Anthropic 原生协议

**3 处必 400 的缺陷**（先加断言全红、再修）：

- **角色未交替**：一轮发多个工具时，每条 tool 消息各生成一条 user 消息 → 出现连续 user，
  Anthropic 直接拒（roles must alternate）。改为同角色合并进同一条消息，`tool_result` 因此
  自然归到一条 user 里（也满足「必须在 content 最前」）
- **空 text 块**：assistant 只带 tool_calls、content 为空串时补了 `text:""`，Anthropic 拒收
  （must be non-empty）→ 空块丢弃
- **thinking 硬约束**：开启 extended thinking 时 `temperature` 必须为 1、`budget_tokens` 须落在
  `[1024, max_tokens)` —— 原先两者都会发出非法值

**两处根因**（比上面三条更值钱）：

- `ResolveApiFormat` / `ResolveModelCallConstraints` / `ResolveProviderTemperature` 都只用
  「模型 + 地址」**精确匹配**。用户在 `providers.json` 新配网关（带 `apiFormat` /
  `supportsThinking`）却没导入模型时，这些设置**全部读不到** ⇒ 协议静默降级成 openai
  （请求打到 `/v1/chat/completions`）、能力回退「按模型名推断」（claude → 支持思考）。
  新增 `ResolveProviderFor`（精确匹配落空后**按 baseUrl 反查注册表占用者**），三处共用。
- LLM 的 HttpClient **没有 User-Agent** ⇒ opencode-zen 前置的 Cloudflare 直接 403（error code 1010）。
  实测同一端点：空 UA → 403，`WayCoder/0.96.105` → 200。

真机验证：`aihubmix-anthropic`（**thinking 正常渲染**）、`opencode-zen-anthropic`（该网关拒绝
thinking，已在 provider 配置里关掉）走 `/v1/messages` 原生格式通过。

### 三、连接解析：`select <命名连接名>`

`--connect select default` 被当成**裸模型名**，静默建出 `providerId/modelId="default"` 的垃圾
connect，并把主模型换成一个**根本不存在的模型**。`ApplySpec` 增加「命名连接名优先」判据
（放在 `ApplySpec` 而非 `select` 分支，让 `/connect <名>` 快捷形式等所有入口一起受益）。

### 四、重复代码提炼：14 处收敛为单一真源

机械扫描（跨文件 8 行窗口）出 34 组候选，按「**是否已在产生风险**」逐组核实。其中 **5 处
不只是重复，而是已在产生用户可见问题**：

1. **`mcp_servers.json` 三份读改写** → `Tools/McpConfigStore.cs`：去重口径相反（一处忽略大小写、
   一处区分）⇒ 同一份配置两边判定不同、导入写进重复条目；且三处都是
   `File.WriteAllText(..., Encoding.UTF8)` —— **非原子写 + 凭空带 BOM**
   （jq / `python json.load` 不容忍，与当初 NotebookEditTool 写坏 `.ipynb` 是同一类）
2. **16 个进程启动点各判各的编码** → `ProcEncoding.IsConsoleWrapperName` + `ApplyIfConsoleWrapper`：
   **8 处该处理 OEM 的漏了**（自更新跑 `cmd.exe`、LintTool 跑 `npx`、McpTransport 起 npx 型 server…）。
   判据是反直觉的：**「所有启动点都调 Apply」是错的** —— cmd 系包装器才套 OEM，原生程序
   （git/dotnet/gcc/语言服务器）输出 UTF-8，套上**反而**乱码
3. **MCP 状态图标 5 套**（文档里记的是 3 套）→ `McpStatusIcon`：连接状态汇总那处用 ASCII `✓✗?`，
   与其余三处的 `✅⏳❌` 不一致 —— 同一个状态在 `/mcp` 显示 ✅、在汇总里显示 ✓
4. **Claude Code 会话解析两份 + 文本提取三份** → `Infra/ClaudeSessionParser.cs`：且已语义分化
   （空 content 旧版返回 `""` 会让会话标题变空）
5. **`KillTool`/`PsTool` 的 `BuildPsi` 逐字相同**（连「本工具此前漏了 Apply」这个修复都各做一遍）
   → `ProcUtil.BuildPsi`

其余收敛：锁冲突提示文案（7 处两种，其中 4 处**根本不告诉用户怎么办**）、
`/model import` 与 `/provider import` 的源解析（逐字相同）、视觉列换算
（`AnsiHelper.VisualColToCharIndex`）、diff 区间合并（`UnifiedDiff.MergeRanges`）、会话转录构建、
列表导航键表（`TuiListNav`）、待办列表前置（`TodoStore.LoadFiltered`）、GUI 供应商扫描
（`ProviderScanner`）、审计工具复用按键测试的 ANSI 模拟器。

跨端工具清单（桌面 vs MAUI）**刻意分开**、无法合并，改为加护栏：断言
「桌面 − MAUI == 已知进程类集合」，漂移即红。

### 五、大文件拆分

| 文件 | 前 | 后 |
|---|---|---|
| `Test/SelfTest.Helpers.cs` | 5968 行 / 122 方法 | **7 个文件**（Doc / Regression / Infra / Web / Context / Model + 保留） |
| `Test/SelfTest.Chunk8.cs` | 2747 行（单方法 27 个 Section） | **3 个文件**（Chunk8 / Chunk8Dialog / Chunk8Ui） |

均为纯移动（`SelfTest` 本就是 `partial class`），自测逐项一致。拆分中发现一处跨段依赖
（`cols`/`rows` 定义在段 2、被段 3 使用），已让段 3 自带定义。

### 六、code-review 6 条 findings（全部处理）

- **尺寸保底丢失**：删掉 `TuiAudit` 手写解析时，它开头的 `rows/cols = Math.Max(…,1)` 一起没了，
  而 `FrameBuffer` 不钳制尺寸 ⇒ 0 尺寸会让 `Math.Clamp(min>max)` 抛异常，三条调用路径都无
  `try/catch` ⇒ **整个 `--test` / `--tui-audit` 中断**（而不是报一条 ❌）。已将钳制收口到构造函数
- **`EraseLine(mode==1)` 满行越界**：`to = _curC` 不设限，而写字符路径 `_curC += w` 从不钳到
  `_cols-1` ⇒ 满行 + `\x1b[1K` 越界。接上模拟器后这条路径从「不可达」变「可达」
- **我加的三条断言对新旧实现都通过**（钉不住重构）→ 换成 5 条能区分的语义断言；顺带查出
  一处**真实语义缺陷**：写字符落在宽字符延续格时没有打断该宽字符（新旧两版都给 `"中X"`，
  而真实终端给 `" X"`）
- **`FrameBuffer` 下沉到 `UI/Shared/Terminal/`**：原先嵌在 `Test/` 里，而 `Test/**` 被 Release
  构建与 MAUI 双双排除 ⇒「唯一的 ANSI 屏幕模拟器」在发布版和移动端**根本不存在**
- 补「生产 `FrameSnapshot` / 测试 `FrameBuffer` 两套解析器」的**对照用例**（此前无任何用例比较
  两者）；`FrameSnapshot.Parse` 的完全委托待 Windows 侧验证（它在 WPF 预览的渲染路径上）
- 删掉因解析器移除而死掉的 `using`

## v0.96.105 (2026-09-11) — 换盘建项目时个人技能「消失」（FindSkillDirs 边界失效）

3 文件，**+49 / −12 行**；自测 **+4 条护栏**。

### 问题

`SkillsManager.FindSkillDirs` 从 cwd 逐级向上收集 `{ .waycoder, .corecoder, .claude, .cursor }/skills`，
边界是 `dir == Global.Home` —— 而 **`Global.Home` 是用户主目录**（`SpecialFolder.UserProfile`）。

只要 **cwd 不在 home 之下**（**D 盘建项目、C 盘放用户目录**，很常见），这个边界**永不触发**：
循环直落盘根，`home` 那一级从来没被访问过 ⇒ 用户自己的 `~/.claude/skills`、
`~/.waycoder/skills` **静默不加载**。

**用户看到的现象**：在 C 盘建的项目里个人技能都在，换到 D 盘建项目，「我的技能全没了」。

而 `Load()` 的文档注释写的恰恰是「从当前目录向上查找到 home 目录」——
**文档说的和代码做的是两件事**。

另外这条边界还有个更隐蔽的问题：`Global.Home` 会被 `HomeOverride` 改成临时目录
（自测/嵌入式场景），那时它**根本不在 cwd 的祖先链上**，等不到相等 ⇒ 上溯无界。

### 修法（两条一起上，缺一不可）

1. **个人级技能目录无条件纳入**：`Global.Home` 不在祖先链上时**显式补到链尾**。
   补在链尾是刻意的 —— 下面的 `Reverse()` 会让它排在最前 = 最先加载 = 优先级最低，
   与「home 恰好在祖先链顶端」时的相对位置一致，**「本地目录覆盖通用目录」的语义不变**。
2. **上溯边界锚在 `ProjectContext.UserProfileDir`**（**不随 `HomeOverride` 变化**，
   已从 `private` 改 `internal`）与盘根，两个都兜 —— 与 `ProjectContext.FindProjectRoot`
   同一处置（那里的注释早就写着「home 与 UserProfileDir 两个边界都兜」）。

### 验证

新增 4 条断言。**自测环境恰好就是「cwd 与 home 不同盘」这个场景**（cwd 是仓库目录，
`Global.HomeOverride` 指向临时目录），所以这组用例在旧实现下必然红。
**证伪过** —— 把「用户级兜底」那一步短路掉，其中 3 条立刻变红、第 4 条（前提校验）仍绿。
`--test` **5235 / 5235**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

### 一条**有意不修**的（如实记录）

自测临时目录若恰好落在**真实用户主目录之下**（Windows 上 `Path.GetTempPath()` 就是
`C:\Users\<你>\AppData\Local\Temp\...`），上溯经过 profile 那一级仍会收进开发机真实的
`~/.claude/skills` —— 这是「用户在自己 home 下跑」的**正常语义**，不是缺陷。
要消除只能把测试目录迁到 profile 之外，**不该往生产代码里塞测试专用的特例**。
原断言保持环境无关（断言创建的两个技能被发现 + 无重名）即为此。

## v0.96.104 (2026-09-11) — Doctor 日志预览的 UTF-16 切片（截断点落在 emoji 上出 U+FFFD）

2 文件，**+32 / −4 行**；自测 **+4 条护栏**（含一条端到端）。

### 问题

CLAUDE.md 有一条既有铁律：**「字符串截断必须按码点（Rune），禁止用 `[..N]` 任意索引切片」**
—— emoji / CJK 扩展 B 是 UTF-16 代理对（2 个 `char`），切半会产出 U+FFFD。
`DoctorEngine` 的「最近日志含 N 条 ERROR」示例预览**绕过了它**：

```csharp
if (preview?.Length > 160) preview = preview[..160] + "…";   // UTF-16 切片
```

这是**日志行**——本仓库自己的日志就含中文与 emoji（`✅ 创建 worktree …` 之类），
截断点落在代理对中间就把 emoji 切成半个，用户看到 ``。

同目录的 `ContextBridge` 早就走 `ContextManager.TruncateWithEllipsis`（Rune 安全），
只有这里漏了 —— 又是本仓库最常见的那个形态：**助手已存在，调用点绕过去了**。

### 修法

改走 `ContextManager.TruncateWithEllipsis(sample.Trim(), 160)`；
`CheckErrorLogs` 从 `private` 改 `internal`（注明「供自测喂临时 cwd」）以便端到端覆盖。

### 验证

新增 4 条断言，其中一条是**端到端**：真在临时目录造一份 `logs/error_*.log`
（偏移刻意设计成 preview 下标 159 是 emoji 的高位代理），跑 `CheckErrorLogs`，
断言**最终提示文案**里没有孤立代理 —— 这样才证伪得到调用点本身，而不只是测到 helper。
**证伪过**：改回 `[..160]`，那条端到端断言立刻变红，另外三条（helper 级）仍绿，
正说明「只测 helper 测不出调用点有没有绕过」。
`--test` **5231 / 5231**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

### 顺带核实（两处**不是** bug，未改）

- `TuiRichEditor.TruncateByVw`：局部变量名叫 `bytePos`，但累加的是
  `rune.Utf16SequenceLength` —— 是 UTF-16 索引不是字节偏移，`Substring(0, bytePos)` **正确**。
  名字误导，但改它只是纯改名，没动。
- `Git/WorktreeIsolation.SanitizeName`：先按 `[^a-zA-Z0-9一-鿿_-]` 过滤，emoji 已被滤掉，
  剩下的全是 BMP 字符 ⇒ 后面的 `safe[..20]` 天然切不出代理对，安全。

## v0.96.103 (2026-09-11) — `notebook_edit` 给用户的 .ipynb 加 BOM（Jupyter 之后打不开）

7 文件，**+48 / −8 行**；自测 **+2 条护栏**。

### 1. `notebook_edit` 写回时凭空加 BOM（数据完整性）

`NotebookEditTool.WriteNotebook` 用 `File.WriteAllText(path, json, Encoding.UTF8)` 写用户的 notebook。
**`Encoding.UTF8` 是带 BOM 的编码**（.NET 的静态实例 `encoderShouldEmitUTF8Identifier: true`），
于是编辑一个**原本没有 BOM** 的 `.ipynb`，写回后就有了 `EF BB BF`。

而 Jupyter / nbformat 读 notebook 走的是 `open(path, encoding='utf-8')` + `json.load`，
**对 BOM 不容忍** —— 实测直接抛：

```
JSONDecodeError: Unexpected UTF-8 BOM (decode using utf-8-sig): line 1 column 1 (char 0)
```

即「用 WayCoder 改一下 notebook，Jupyter 就打不开了」。

**改法**：走 `Global.WriteAllTextPreserveBom` —— 编辑路径的标准本就是它
（`EditFileTool` / `MultiEditTool` 都走），只有这里漏了，且**方向是错的**：
preserve-bom 是「原文件有 BOM 就保留、没有就不加」，而 `Encoding.UTF8` 是「无条件加」。

### 2. 顺带：6 处配置/状态文件补齐原子写

`Global.WriteAllTextAtomic` 已有 12 个调用点，但同目录、同角色的几个文件仍在裸写：

| 文件 | 说明 |
|---|---|
| `Config/ModelCatalog.Providers.cs` | `providers.json` —— 与同目录的 `config.json`/`connections.json`（早已原子写）同级同为权威配置 |
| `Config/AgentSlotConfig.cs` | 槽位配置 |
| `Config/ThemeConfig.cs` | 主题配置 |
| `Memory/StructuredMemory.cs` ×2 | **智能体自己的记忆正文 + `MEMORY.md` 索引** |
| `Memory/KbIndex.cs` | 知识库条目正文 —— 同文件的 `StatePath`（:984）早就原子写，条目正文却漏了；同一份知识库一个原子一个不原子 |

这 6 处的写法与 `WriteAllTextAtomic` **逐字节等价**（都是 UTF-8 无 BOM），
所以换过去不改变任何读方的结果，只是把「崩溃/磁盘满留下半截文件 ⇒ 整份配置读不回来」
这个窗口关掉。

**未改动、如实记录**：`McpClient.cs` / `McpCache.cs` / `ImportHelper.cs` / `Program.Commands.cs`
写这几处时显式传了 `Encoding.UTF8`（**带 BOM**，与上面第 1 条同一类问题），且仍是裸写。
它们是另一批（含异步写，需要单独一个异步原子写入口 + 确认各读方容忍无 BOM），本版**没动**。

### 验证

新增 2 条断言：无 BOM 的 `.ipynb` 编辑后**仍无** BOM、带 BOM 的编辑后**保留** BOM。
**证伪过** —— 把 `WriteNotebook` 改回 `Encoding.UTF8`，第一条立刻变红、第二条仍绿
（`Encoding.UTF8` 无条件加 BOM，所以「保留」那条测不出问题），正是预期。
`--test` **5227 / 5227**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.102 (2026-09-11) — `ps` / `kill` / 抓屏的 cmd 启动点漏了解码（中文系统必乱码）

3 工具改动，**+92 / −23 行**；自测 **+3 条护栏**。

### 问题

CLAUDE.md 有一条既有铁律：**「新建任何启动 cmd/bash 子进程的代码都要调 `ProcEncoding.Apply`」**
—— cmd.exe 及其子命令向**重定向管道**写的是系统 OEM 代码页字节（中文系统 GBK），
按 UTF-8 解码必乱码。这条铁律有三处**漏了**，且都是「智能体直接读它的输出」的工具：

| 位置 | 子进程 | 实测输出 |
|---|---|---|
| `Tools/PsTool.cs` | `cmd.exe /c tasklist` | `tasklist /NH` 有 **350 字节非 ASCII**（`微信开发者工具.exe` 等），UTF-8 解码在偏移 27928 处失败 |
| `Tools/KillTool.cs` | `cmd.exe /c taskkill` | `错误: 没有找到进程 "999999"。` —— 非法 UTF-8 |
| `Tools/ScreenshotTool.cs` | `powershell -Command` | `中文测试 abc` —— 非法 UTF-8 |

后果不是「显示难看」：`ps` 的中文进程名解成乱码后，智能体**照着拼进 `kill` 必然失败**；
`kill` 的成败提示解成乱码后，**它读不出杀成功还是杀失败**。

**三条都是实测确认的**（不是按代码推断）：直接抓子进程 stdout 字节，
`b.decode('utf-8')` 全部抛 `UnicodeDecodeError`，`b.decode('gbk')` 全部还原成正确中文。

### 修法

三处各补 `ProcEncoding.Apply(psi)`。**没有**塞进 `ProcUtil.RunAsync` 一刀切 ——
它同时还服务 `git`（UTF-8）、`sqlite3`（UTF-8）、`dotnet build`/`ruff`/`npx`（UTF-8）、
`gdb`（UTF-8），套上 OEM 解码会把它们的输出解成乱码。**判据是「子进程是不是 Windows 控制台工具」，
不是「有没有重定向」**：

- `PsTool` / `KillTool` 的 psi 装配提成 `internal static BuildPsi(fileName, args)`（工具本就只在 Windows 走 cmd.exe）
- `ScreenshotTool.RunProcess` 加 `oemDecode` 参数，**默认 false** —— 只有 Windows 抓屏那条
  powershell 路径传 true，`tesseract` 保持 UTF-8（它输出的正是中文 OCR 文本，套 OEM 解码反而毁掉）

### 验证

新增 3 条断言：两条断言 `BuildPsi` 的 `StandardOutputEncoding == ProcEncoding.OemEncoding`，
一条**端到端真跑 cmd 回显中文**（`/c echo 中文测试ABC`）断言解出原字且不含 `�`。
**证伪过** —— 摘掉 `PsTool.BuildPsi` 里的 `Apply`，其中两条立刻变红。
`--test` **5225 / 5225**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.101 (2026-09-11) — 重复代码清理（C 级第十批）：模态窗关闭协议单出口

9 文件改动，**+194 / −142 行**；自测 **+16 条护栏**。

### 关模态窗的 53 处手写样板 → `TuiWindow.Close`

关一个模态窗永远要按序做三件事：

1. 写 `Result` —— 调用方读它取值
2. 跑调用方回调（`onResult`/`onConfirm`/`onCancel`…）
3. 触发 `OnClosed` —— 驱动窗口出栈、唤醒渲染等待循环

`TuiDialog` 的 8 个构建器把这三行**手抄了 47 处**（另加 `TuiMenu` 3、`TuiKeybindHelp`/`DiffPreview`/`TuiMarkup` 各 1），
漏写任一行都没有任何编译期提示：

- 漏 `OnClosed` ⇒ **窗口永不关闭**，渲染等待循环等不到事件，调用方一直挂着
- 漏 `Result` ⇒ 调用方读到默认值（`object?` 的 −1、`int?` 的 null），把「取消」读成「确认」
- 回调跑在 `Result` 之前 ⇒ 被回调唤醒的调用方读到上一轮的旧值

现在只有 `TuiWindow.Close(result, callback)` 一个出口；`Close()` 是它的无结果版
（查找替换框按「查找下一个」关窗但无「返回值」这回事）。`UxHelper.FinishModal`
（5 个 Picker 的收敛点）也改为委托它 —— 全仓「关模态窗」再无第二条路径。

**核查结论（与评审假设不同，如实记录）**：评审据「39 处 `Result` vs 46 处 `OnClosed`」
推断有 7 处「半成品 ⇒ 死锁」。逐行核对后 **39 处全部配对正确**，多出的 8 处
是查找替换框**另一套合法协议**（只走回调、不经 `Result` 交付），不是 bug。
本批收的是「未来漏写的风险」，不是修一个已存在的死锁。

**顺带查实一条真事实**：消息框（Info/Success/Warn/Error）**没注册 Esc 快捷键**，
由 `TuiScreen.OnKey` 的模态兜底直接触发 `OnClosed` 关窗（不写 `Result`，保持 −1）。
这条只有在「走屏幕真实路径」的测试里才测得到 —— 直接跑快捷键体会抛 KeyNotFound。
已在 `TuiScreen` 那处补注释说明为什么它**故意**不走 `Close()`。

**验证**：新增 16 条断言（5 条快捷键路径断言 `Result`/回调/`OnClosed` 三件齐活 +
11 个构建器逐个走 `ShowWindow + screen.OnKey(Esc)` 屏幕路径断言真关窗），
并**证伪过** —— 摘掉 `Close` 里的 `OnClosed?.Invoke()`，21 条里 19 条立刻变红，
唯一两条仍绿的是走屏幕兜底的消息框，正是预期。
`--test` **5222 / 5222**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.100 (2026-09-11) — 重复代码清理（C 级第九批）：滚动状态机同源 + 列表视口复位

4 文件改动 + 1 新增，**+56 / −172 行**；自测 **+6 条护栏**。

### 1. 滚动状态机 2 份 → `TuiScrollable`（新基类）

`TuiScrollView`（内容自适应高度的滚动视图）与 `TuiListView`（逐项滚动的列表视图）
各持一份 `ScrollOffset` / `ContentHeight` / `IsAutoScrollToEnd` / 弃用别名 `AutoScroll`
与 `ScrollUp` / `ScrollDown` / `ScrollToTop` / `ScrollToBottom` ——
**四段方法体逐字节相同**，连「已在顶部/底部则 no-op（防闪屏）」的判据与注释都一致。

差异只有一处：无参调用的默认步长（列表一格滚轮 3 行、滚动视图 1 行），
提成 `<see cref="ScrollStepLines"/>` 虚属性（`TuiListView` 覆写为 3）。

**为什么值得收**：这几个方法不是纯 getter，而是「跟底状态 + 边界 no-op + 标脏」三件事
交织的状态机 —— `ScrollDown` 到边界要把 `IsAutoScrollToEnd` 置回 `true`（内容再增长时继续跟底），
非边界置 `false`（用户手动滚上去了就别再自动跟底）。漏一处就是「滚一下就永久失去自动跟底」
或「手动上翻一页又被拽回底部」，而两份各自演化时**没有任何编译期提示**。

### 2. 顺带修掉 `TuiListView` 缺 `OnResize` 复位

缩放 / 截图终端后视口变高，旧的 `ScrollOffset` 可能已越过新的 `ContentHeight - Height`，
**偏移越界后列表停在空白区**。此前只有 `TuiScrollView` 覆写 `OnResize` 做了钳制，列表视图没有。
判据收进 `TuiScrollable.OnResize`（`base.OnResize` = 布局 + 递归子控件，再加 `ClampScroll()`），
两类同时具备。

### 3. 树标脏两份 → `TuiView.SetTreeDirty`

`TuiListView` 私有的 `SetTreeDirty` 递归与 `TuiView.MarkDirtyTree` 是**同一趟遍历**，
唯一区别是前者只置 `IsDirty`、后者额外叫醒帧闸门（供渲染帧内调用，避免多排一帧空渲染）。
现在共用 `TuiView.SetTreeDirty(节点)` 一个静态遍历，`MarkDirtyTree` / `MarkDirtyTreeQuiet` /
`MarkItemContentDirty` 三条路径都走它。

**验证**：新增 6 条断言（两类同源 + 步长 3/1 + 视口变高后 offset 钳回），
并**证伪过**——摘掉 `ClampScroll()` 后两条「视口变高后 offset 被钳回」立刻变红；
`TuiScrollView` 那条先因「内容增长自动跟底」顺带调位而**假绿**，已改成先 `Layout()` 固化
`ContentHeight` 再 `ScrollUp(1)` 退出跟底，确保测的是钳制本身而非跟底副作用。
`--test` **5206 / 5206**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.99 (2026-09-11) — 重复代码清理（C 级第八批）：内联滚动条落笔

5 文件改动，**+54 / −40 行**；自测 **+8 条护栏**。

### 内联滚动条 4 份 → `TuiScrollMath.Paint`

`TuiList`（列表选单）、`TuiMenu`（滚动菜单）、`TuiTableList`（表格列表）、
`DiffPreview`（diff 预览右侧）各写一遍「`Bar()` 取几何 → 逐行判滑块 → 写目标列」，
四份**连字符字面量 `"█"` / `"│"` 都逐字相同**，只有两处正当差异：

| 调用点 | 目标列 | 颜色来源 |
|---|---|---|
| `TuiList` | `absX + Width` | `fg: 2`（裸样式码） |
| `TuiMenu` | `absX + ContentWidth` | `AnsiTty.StyleDim` |
| `TuiTableList` | `absX + Width - 1` | 主题 `SeekBarThumbFg` / `SeparatorFg` |
| `DiffPreview` | `absX + contentW` | `AnsiTty.SgrDim` 包裹 |

目标列与颜色正是各控件的语义，故留作参数；几何仍由 `Bar()` 提供，
新增的 `TuiScrollMath.Paint` 只负责落笔，字符提成 `ThumbChar` / `TrackChar` 两个常量。

**顺带修掉一条潜在崩溃**：`Bar()` 在 `total <= vis` 时滑块长度会超过视口，
`(long)(vis - thumb)` 变负会让 `Math.Clamp` 以 min > max 抛 `ArgumentException`。
四处调用点此前**各判一次**同一个条件（`total > vis`），属于「四份守卫，漏一处即崩」——
判据收进 `Paint` 内部，新增调用点不必再记得判。

**行为不变**：`TuiList` 的 `fg: 2` 与 `AnsiTty.StyleDim` 同为 SGR 2 淡化
（`RenderBuffer` 把 1..9 当样式码，8 是 conceal 不是 dim，见 v0.96.90 的 `AnsiTty.StyleDim`）；
`DiffPreview` 改用 `RenderBuffer` 后逐字符末尾由 `SgrReset` 变 `StyleOffSeq(2)` = `ESC[22m`，
只关淡化不清色，视觉一致且不再冲掉底色。

**验证**：新增 8 条断言把「逐行画面 == `Bar()` 几何」钉死（顶部/中间/底部三档 + 界线档 +
`height=0`），并**证伪过**——临时摘掉 `total <= visible` 守卫，2 条断言立刻变红。
`--test` **5199 / 5199**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。
全仓 `? "█" : "│"` 已无残留。

## v0.96.98 (2026-09-11) — 重复代码清理（C 级第七批）：日志文件句柄生命周期

2 文件改动 + 1 新增，**+16 / −145 行**。

### `RotatingFileWriter`（新）

`FileLogSink` 与 `JsonLogSink` 各写一遍 `EnsureOpen` / `Flush` / `Dispose` 样板，
连 `StreamWriter` 的构造都逐字相同（`new FileStream(path, Append, Write, FileShare.Read)`
+ `UTF8Encoding(false)`）。其中 `FileLogSink` 内部**同一个构造表达式还写了两遍**
（首次打开 + 按大小轮转后重开）。

差异全部提成构造参数，语义仍由各 sink 自己决定：

- `FileLogSink`：`rotateByDate: _rotateByDate, maxFileSizeBytes: _maxFileSizeBytes,
  flushEveryWrite: !_buffered`（保留「按大小 + 按日期」双策略与 `_rotateSeq`）
- `JsonLogSink`：`.jsonl`，只按日期、逐条 flush、不按大小轮转

**为什么这类重复值得收**（已写进类注释）：样板漏改一处**没有任何编译期提示** ——
漏 `Flush` 会丢日志，漏 `FileShare.Read` 会把文件**独占锁死**（外部连 `tail` 都打不开）。
现在句柄打开只有一处。

**验证**：`--test` **5191 / 5191**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.97 (2026-09-11) — 重复代码清理（C 级第六批）：相对时间阶梯 / 模型文件移除

6 文件，**+72 / −65 行**；自测 **+7 条护栏**。

### 1. 相对时间格式化 3 份 → `UiText.RelativeTime`

`SessionPicker`（TUI 侧边栏会话区）、`MauiSessions`（移动端会话列表）、
`CheckpointManager`（检查点列表）各写一遍阶梯。**移动端那份已经漂移**：漏了「N 周前」分支
⇒ **10 天前在手机上显示「10 天前」、在桌面显示「1 周前」**，同一个时间两处说法不同。

收口到 `UiText.RelativeTime(now, past, withSeconds = false)`：秒级档做成可选
（检查点列表要「N 秒前」，会话列表不要），其余唯一。

**两处行为变化**（都往更完整的一侧统一）：

- 移动端会话列表：7~30 天从「N 天前」变成「N 周前」；
- 检查点列表：新增「N 周前」与「超 30 天显示日期」（此前超 7 天一律「N 天前」，30 天前就显示「30 天前」）。

新增 7 条护栏，其中一条专门锁「10 天 → 1 周前」—— 正是移动端此前缺的那档。

### 2. `ModelCatalog.RemoveCustomByProvider` 里的两份 14 行

「读文件 → 按 providerId 过滤 → 删空则删文件否则 `SaveCustom`」对「新分类文件」与
「旧 models.json」各写一遍（目前一致，但改一处必忘另一处）→ 抽
`RemoveProviderFromFile(path, providerId)`，方法体从 44 行缩到 7 行。

**验证**：`--test` **5191 / 5191**（v0.96.96 为 5184）；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.96 (2026-09-11) — 重复代码清理（C 级第五批）：ImportHelper 的 MCP 导入与摘要文案

1 文件，**+59 / −68 行**。

### 1. Cursor / Cline 的 MCP 导入各写一遍（约 35 行同构）

抽 `ParseMcpServers(servers, sourceLabel)`，两个 `Import*McpAsync` 缩成
「定位文件 → 解析 → 调它 → 写盘」四步。

**修掉一处漂移**：Cline 那侧此前**只打印 command、不打印 args**，用户在导入报告里
看不到实际参数（Cursor 那侧打印）。现在两条路径共用一份，报告格式一致。

### 2. 五处同一串摘要插值

`$"{X.Count} 个: {string.Join(", ", names.Take(5))}{(names.Count > 5 ? "…" : "")}"`
在 Claude 插件 / OpenCode MCP / OpenCode 插件 / Cursor MCP / Cline MCP 各写一遍 ——
抽 `SummarizeNames(names)`。

顺带消掉一个隐患：原写法里 `X.Count` 与 `names.Count` 是**两个独立来源**，
而 `names` 恰恰是从 `X` 派生的（五处当前都相等）——「N 个」与实际列出的条目一旦不一致就是 bug。

**验证**：`--test` **5184 / 5184**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.95 (2026-09-11) — 重复代码清理（C 级第四批）：Maui Markup 三份高亮 / 目录上溯四处

6 文件改动 + 1 新增，**+56 / −120 行**。

### 1. Maui `Markup/`：表格解析与代码高亮各写了多份

- **`MarkdownTable.cs`（新）**：`IsTableSeparator` 与单元格切分在两处**逐字相同**
  （一边叫 `SplitCells`、一边叫 `ParseTableRow`），外加第三份 `IsSeparatorRow`。
  两处「目前一致」，但表格判定分家会让预览页与聊天流对同一段 markdown 给出不同结果。
- **`MarkupToFormattedString.AppendCodeLines`（新）**：代码块逐行 Tokenize 上色的循环写了**三遍**。
  合并时发现三处**并不等价**：预览页那版不用等宽字体、且是「每行都补换行、最后再删掉」
  （合并版天然不在末尾补）；聊天流那一版**缺「超大代码块降级纯文本」护栏** —— 而那道护栏
  正是为防 `>100k` 字符主线程长时间分词（移动端 ANR）而加的。现在三处共用一份，护栏全覆盖。

### 2. 目录上溯循环 4 处 → `Global.WalkUpDirectories`

`FindExistingConfigDir`、`FindConfigFileInTree`、`ImportHelper.FindInTree`、`McpCache.FindCacheFile`
各写了一遍同一个「逐级上溯 + `parent == dir` 盘根终止」。其中 `McpCache.FindCacheFile`
其实就是 `Global.FindConfigFileInTree` —— 它下面那句注释
「FindConfigFileInTree defined in Global.cs (shared with McpClient)」正说明作者知道，只是没去调。

新助手的文档特意记下那条教训：**`Path.GetDirectoryName` 对盘根返回 null**，终止要同时兜住
`parent == dir` 与 `parent == null`；并指向 `SkillsManager.FindSkillDirs` 那个
「边界只在 cwd 位于 home 之下时才成立、实际会爬到盘根」的例子 ——
**写这类循环前先想清楚边界在什么条件下才会触发**。

**验证**：`--test` **5184 / 5184**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.94 (2026-09-11) — 重复代码清理（C 级第三批）：工具错误文案 + 自测框架失败行可见性

8 文件，+38 / −8 行。附带定性并修掉了一个「失败 +1 却看不到是哪条」的自测框架缺陷，
以及由它暴露出的一个既有 bug。

### 1. `ToolErrors` 收口（输出文案逐字不变）

`cp` / `ls` / `mv` / `rm` / `wc` 五个工具的 catch 各自手拼
`错误：{op}: {异常类型}: {Message}`，而既有的 `ToolErrors.Error(op, ex)` 产出的前缀顺序**相反**
（`{op}错误：…`）—— 所以不能直接替换调用。新增保持文案逐字不变的重载
`ToolErrors.ErrorOpPrefix(op, ex)`，五处改调。

`错误：` 前缀是 Agent 识别「工具失败而非模型问题」的稳定标记，手拼五份，将来任一处拼错就会破坏它。
（`GitTool` 那份是 `错误：Git: …`，操作名大小写不同，未并入。）

### 2. 自测框架：失败行会被 `Console.SetOut` 捕获区吞掉

此前偶发出现过「汇总里 `失败: 1`，但输出里**没有任何 ❌ 行**」—— 排查时完全无从下手。
根因：`SelfTest.Report` 走 `Console.WriteLine`，而不少测试用
`Console.SetOut(StringWriter)` 捕获输出（渲染 / 对话框 / 编码类），**期间若有 Check 失败，
❌ 就落进那个 StringWriter 被丢掉**，只涨计数、看不到是哪一条。

修法：套件入口（`RunWithFilter`）捕获真实 stdout，`Report` 在**当前 `Console.Out` 已不是它**时
把失败行**额外直写真实 stdout**。已用「把一条位于 SetOut 捕获区内的断言临时置为失败」实测反证：
修复前只看到 `失败: 1`，修复后正常打出 `❌ 分区刷新：改右段后…`。

这条修复的直接价值：下一个偶发失败会自己报上名来 —— 下节两条就是这么找到的。

### 3. 由它暴露的问题

**(a) `SkillsManager.FindSkillDirs` 的终止边界失效（既有 bug）**

它从 cwd 向上收集 `.waycoder/skills` / `.claude/skills` 等目录，终止条件是
`dir == Global.Home`；但**只要 cwd 不在 `Global.Home` 之下，这个边界永不触发**，
循环会一路爬到盘根（`.claude` 那条本意是 Claude Code 兼容，被扫到是预期内的，
但「爬到盘根」不是）。实测：自测在临时目录里做技能发现时，会把开发机真实的
`~/.claude/skills`（6+ 个技能）一并算进来，于是「恰好发现 2 个技能」这条断言在环境里
该目录有内容后**稳定失败**，且与代码改动无关。

这与 CLAUDE.md 已记录的 `ProjectContext.FindProjectRoot` 是**同一类坑**
（「边界判定必须先于标志检测」）。

本版**未改技能发现语义**（对 `~/.claude/skills` 的兼容可能是有意的）——只把该断言改为环境无关：
断言「本测试创建的两个技能确实被发现」+「无重名」。**是否让向上查找真正止步于 home，留待决策。**

**(b) 两条环境/顺序相关的偶发**

技能那条（已定性）与 `ApiKeyStore 导入: json 为空时 env 补入`（连跑两次未复现）。
在框架修复前，两者都表现为「失败 +1、无从定位」。

**验证**：`--test` **5184 / 5184**（连跑 2 次）；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.93 (2026-09-11) — 重复代码清理（C 级第二批）：BOM 表 / 鼠标命中

4 文件，+45 / −35 行。两条都是「同一规则两份实现、只修了其中一处」。

### 1. BOM 表：`Detect` 与 `Decode` 各写一遍，只有 `Decode` 修了 UTF-32

`TextEncoding` 里同一套「BOM 字节序列 → 前缀长度」的表写了两遍：一遍用
`Utf8Bom`/`Utf16LeBom`/`Utf16BeBom` 常量比较，一遍用行内字面量再写一次。
漂移后果是**可复现的**：`Decode` 有 UTF-32 分支、`Detect` **没有**，而 UTF-32 LE 的 BOM 是
`FF FE 00 00` —— 在 `Detect` 里会被 UTF-16 LE 的 `FF FE` 抢先命中，余下字节按 UTF-16 解出
一堆 NUL / 乱码。于是**同一个 UTF-32 文件，经 `Decode` 打开正常、经 `Detect` 打开是乱码**。

抽成唯一实现 `MatchBom(ReadOnlySpan<byte>)`（返回前缀长度 + 显示名 + 写回用 Encoding），
两条路径共用。**顺序敏感**（UTF-32 的 4 字节 BOM 必须先于 UTF-16 的 2 字节判定）这件事现在只在
一处表达。新增 2 条护栏：`Detect` 认 UTF-32 LE、且与 `Decode` 对同一输入的结论一致。

### 2. 鼠标命中绕过 `MouseInBounds`

`TuiRichEditor.OnMouse` 与 `DiffPreview.DiffView.OnMouse` 手写
`GetAbsoluteX/Y` + 四则边界判断，而共享入口 `TuiControl.MouseInBounds`（基于渲染缓存
`HitAbsX/HitAbsY`）已经有 13 处在用 —— 它的注释明确写着引入理由就是修
「弹窗内点击错位：`GetAbsoluteX` 沿链累加漏窗口 X/Y」。这两处**恰好都在弹窗内**
（`TuiRichEditor` 在 `editor.tui` 的 Dialog 里、`DiffView` 被插进 `TuiWindow` 的 body），
正是那个坑的命中场景。改走 `MouseInBounds`，并把它返回的**相对坐标**直接用于后续的
点击定位换算（与命中判断同源，比旧式 `ev.MouseX - absX` 更准）。

**验证**：`--test` **5184 / 5184**（v0.96.92 为 5182）；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

## v0.96.92 (2026-09-11) — 重复代码清理（C 级第一批）：HTTP 重定向 / 模态对话框样板 / 写文件两步

7 文件改动 + 2 新增，**+67 / −247 行**。

### 1. `SsgfRedirect`：三份「跟随重定向 + 每跳 SSRF 校验」

`FetchTool` / `DownloadTool` / `DocTool` 各写一份（约 40 行/份，连 doc 注释都几乎逐字相同），
且已经漂移：**重定向预算是 5 / 10 / 5，无理由地不同** —— 同一句 `"重定向次数过多"` 之下，
download 能吞 10 跳、fetch 只能 5 跳。现在预算必须显式传、差异摆在调用点上（download 传 10 并注明
理由：下载链接经 CDN/镜像跳转往往比普通抓取多几跳）。

DocTool 那条「不查 DNS」保留为显式参数 `checkDns: false` —— 它有明确理由：最终连接由
`SsgfGuard.CreateSafeHandler` 的 ConnectCallback 原子完成「解析→校验→连接」，这里再 CheckDns
会二次解析、重开 DNS 重绑定窗口。

### 2. `UxHelper` 六个私有对话框收口到 `RunModalDialog`

同文件 40 行外就有 `RunModalDialog<TResult>`（注释写着「各 Picker 此前重复约 8 份……收敛到此单点」），
而 `ShowInputDialog` / `ShowSecretDialog` / `ShowSelectDialog` / `ShowMultiSelectDialog` /
`ShowAskDialog` / `ShowConfirmDialog` **六个都没用它**，各自手写
「result + ManualResetEventSlim + try/catch + ShowWindow + RenderWait」13~25 行，−146 行。

**⚠ 迁移时最容易踩的坑（已在代码注释里写明）**：`ShowConfirmDialog` 取
`RunModalDialog<int?>` 而**不是** `int`。`RunModalDialog<TResult>` 是无约束泛型，
`TResult?` 对值类型**不退化为 `Nullable<T>`** —— 用 `int` 的话「回调未触发」（超时 / 构建抛异常）
会静默退化成 `default(int) = 0 = **允许**`，**权限弹窗在异常路径上就从「拒绝」翻成「允许」**，
而调用点文本一字未变。取 `int?` 才能表达空态，末尾 `?? 2` 保住原来那句「默认拒绝」。

### 3. `WritePipeline`：写文件流水线里零风险的两步

四个工具（`EditFileTool` / `MultiEditTool` / `WriteFileTool` / `DownloadTool`）各写了一遍
「guard → lock → try/finally → 先读后改 → diff 确认 → CRLF → record → lint」，其中两段逐字重复：

- **`ConfirmDiff`**：逐 hunk 确认。三处的守卫条件写法已经分叉
  （`cfg.DiffPreview && CanConfirmInline && !Yolo` vs 先判 `cfg.DiffPreview` 再内层分 Yolo）——
  当前语义等价，但改一处另两处不会跟着改，而 `CanConfirmInline` 那条注释特意写了
  「勿裸判重定向」的教训。现在收敛成一份。
- **`RestoreCrlf`**：CRLF 行尾恢复（两处连注释都逐字相同）。类注释写明调用时机要求：
  必须在生成 diff / 记录变更**之后**（那时内容都还是 LF，否则逐行比较会把整文件误判为改动）。

**没有抽整个流水线**（`FileWriteScope` 那种 `using` 式 guard+lock）：四处的异常路径语义并不相同
（如 `DownloadTool` 取消时要先删半成品文件再 rethrow），收益不抵重构风险 —— 已在类注释里写明。

### 4. Braille 帧集

`TuiAnimatedText` 自己声明的 `SpinnerFrames` 与 `AgentStatusResolver.SpinnerFrames`
（注释自称「经典 10 帧 Braille……四端共用」）逐字相同，改为引用唯一真源 ——
帧集改名/增减时四端本该同步，抄一份就多一个「改一处漏一处、各端动画不同步」的入口。

**验证**：`--test` **5182 / 5182**；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

**C 级仍未做**：`TuiListView : TuiScrollView`（滚动状态与方法重复约 50 行，且 `TuiListView`
缺 `OnResize` 重钳偏移 —— resize 后可能越界）、`TuiDialog` 关闭协议 46 处
（`win.Result =` 39 处 vs `OnClosed` 46 处 ⇒ 7 处只做一半，漏的那半边会让窗口不关 / 事件不置位 → 卡死）、
内联滚动条 4 份、写文件流水线剩下的 guard+lock、日志 sink 日期轮转 3 份、
BOM 表 2 份（`Detect` 缺 UTF-32 分支 ⇒ UTF-32LE 的 `FF FE 00 00` 会被当 UTF-16LE 解出乱码）、
`ImportHelper` 的 Cursor/Cline 两份、Maui `Markup/` 三份高亮循环、鼠标命中绕过 `MouseInBounds`
（`TuiRichEditor`/`DiffPreview` 恰好在弹窗内 → 命中 `HitAbsX` 记录的那个「点击错位」老坑）、
目录上溯循环 3 份、`ToolErrors` 被 6 处手拼。

## v0.96.91 (2026-09-11) — 重复代码清理（下）：模型解析 / 模型切换 / diff 引擎 / 跨端文案 / key 判定

接 v0.96.90（A 级 7 条真 bug + 三处单一真源收口），本版做完 B 级剩下 5 条。
27 文件，**+211 / −499 行**（净删 288 行）。

### 1. 模型地址 / 服务商判定（B1）

同一概念「这个模型该连哪个地址 / 这个地址属于哪个服务商」有 8 处内联实现。收口到
`ModelCatalog.BaseUrlOf` / `ResolveBaseUrl`：

- `ConnectionConfig.ResolveBaseUrl`、`ApiKeyStore.Set`、`ModelCli.AddModel`、
  `ModelCatalog.Import`、`AgentSlotConfig`（两处）、`Program.Repl`（Ctrl+X 交换）共 7 处改走助手。
- **实质变化**：`BaseUrlOf` 有大小写不敏感兜底（注释解释了为什么 —— providers.json 手写混合大小写 key），
  而内联的 `Providers.TryGetValue(pid, ...)` **没有** ⇒ 传 `"DeepSeek"` 时助手命中、内联落空。
  现在全链路一致。
- `ConnectionConfig.InferProviderFromBaseUrl`（未命中返回 `"custom"`）与
  `ModelCatalog.FindProviderByBaseUrl`（未命中返回 null）是同一逻辑两份实现，前者改为
  `FindProviderByBaseUrl(baseUrl) ?? "custom"`。

### 2. 模型切换收尾（B2）

`Agent.ApplyRuntimeModel` 的文档自称「CLI/TUI/GUI/Web/MAUI 各端切换模型的公共收尾」，
但 TUI 五处全绕过，且每处各自补一句 `SmallModel = …`、取法四样（局部变量 / `cfg.SmallModel` /
`_llm.SmallModel` / **干脆不设**）。

- 签名扩为 `ApplyRuntimeModel(modelId, smallModelId, apiKey, baseUrl)`：`smallModelId` 为 null
  表示「本次不动小模型」，各端策略显式传，不再是「补了这处忘那处」。
- **修掉真 bug**：运行时回退链那条**只改大模型** ⇒ 跨服务商回退后小模型仍指向旧服务商，
  压缩请求打到旧网关。
- 删掉 `Program.ApplyModel` —— 与 `ModelPicker.Apply` 逐字重复 40 行，且**漏了「运行时生效」尾段**，
  逼得 `CycleModel` 在下面自己又补一遍。现在统一走 `ModelPicker.Apply`。

### 3. unified diff 引擎（B6）

`EditFileTool` 与 `MultiEditTool` 各有一份逐字拷贝的私有 diff 生成器（后者函数名就叫
`EditFileTool_GenerateDiff`、注释写「复用 EditFileTool 逻辑」，**实际是拷贝不是调用**），
而公共实现 `DiffPreview.GenerateUnifiedDiff` 就在旁边。工具那两份
**只找「首个差异行 → 末尾差异行」、只支持单块改动、无 `@@` 头** ⇒ 同一文件两处相隔较远的
小改动会被呈现成「删掉中间全部 + 重新加」，误导模型以为整段被重写。

引擎下沉到 **`UI/Shared/UnifiedDiff.cs`**（`Build`/`Render`/`Generate`）：
`UI/Shared/` 是桌面 / Gui / **MAUI** 三边都编译的位置 —— 留在 `UI/TUI/Custom/DiffPreview.cs` 里的话，
MAUI 排除了 `UI/TUI/**`，`Tools/**` 在 MAUI 构建下根本引用不到（这正是当初各抄一份的成因）。
`DiffPreview.BuildHunks` / `GenerateUnifiedDiff` 改为适配本引擎（删掉它的
`ComputeLineEdits` + `GroupIntoHunks` + 生成主体，−152 行），`ApplyAccepted` 保持原样未动。
两个工具改调 `UnifiedDiff.Generate`。新增 2 条自测锁住「两处远距离改动 → 2 个 hunk」
与「未改动的中间段被省略、既不算删除也不重复添加」。

### 4. 跨端权限 / 经济文案（B7）

同一个枚举 5 种叫法（`Ask` / `必问` / `Ask（每次确认）` / `问答ACK` / `YOLO (上帝模式)`），
散在 `PermissionManager` / `AutoCommand` / `WebChat` / `GuiCommands` / Maui 五处各写一遍 switch；
`UiText`（建来就是为消重）**零生产调用点**。

- `UiText` 增 `PermNameZh`（必问/自动/智能/畅通）、`PermDesc`、`PermLabel`、`PermFull`、
  `PermCompact`（必问ASK…）与 `EconomyShortName`（省钱/自动/极致/关闭）；六处改调。
- 颜色/emoji 留在各自端（那是呈现选择，不是文案）。
- Gui 的下拉与 Maui 的标签数组改为**从枚举派生**（`Enum.GetValues` 顺序即 `(int)` 索引，
  `SelectedIndex` 依赖它），不再手维护平行数组 —— 加档位不会再只改一端。

### 5. key 判定（B8）

Web 的 `ProviderHasKey` 与 `SerializeState` 里的内联判据都**只查 ApiKeyStore + 全局 ApiKey、
不查环境变量**，而同文件的模型列表走 `ApiKeyStore.HasKeyFor`（查环境变量）
⇒ **同一个页面上「模型列表说有 key、供应商列表说没 key」**。两处改走 `HasKeyFor`。
另删掉 `ModelPicker` 私有的 `ProviderEnvVar` 表 + `ModelHasKey`（1844 字符，与 `ApiKeyStore`
逐行等价，两边注释互相点名「对齐对方」——作者已知重复而选择保留）。

### 6. 用户可见的变化

1. **工具输出的 diff 变成多 hunk 且带 `@@` 头**（更正确；`--json` 桥与 Web 面板的 diff 呈现同变）。
2. **权限/经济措辞统一**为「必问 / 自动 / 智能 / 畅通」与「省钱 / 自动 / 极致 / 关闭」——
   各端此前互不相同，现在同一套。
3. **跨服务商回退后小模型会跟着换**（此前不换，是个 bug）。
4. Web 上「供应商是否已配置 key」现在会认环境变量（此前不认）。

**验证**：`--test` **5182 / 5182**（v0.96.90 为 5180）；桌面 / Gui / MAUI Android 三工程构建均 0 错误。

**仍未做**：C 级 20 余条 —— `TuiListView : TuiScrollView`（滚动状态与方法重复，且 `TuiListView`
缺 `OnResize` 重钳）、`UxHelper` 六个私有对话框收口到同文件的 `RunModalDialog`
（含「超时默认 `2`=拒绝 vs 默认 null」这个会把权限弹窗**从拒绝翻成允许**的坑）、
工具写文件流水线 4 份（guard→lock→finally→diff→CRLF→record→lint）、
三份 `SendWithRedirectAsync`（**重定向预算 5 / 10 / 5** 无理由地不同）、
`TuiDialog` 关闭协议 46 处、内联滚动条 4 份（含 `fg: 8` = conceal 已单独修）、
日志 sink 日期轮转 3 份、BOM 表 2 份（`Detect` 缺 UTF-32 分支）、
`ImportHelper` Cursor/Cline 两份、Maui `Markup/` 内三份高亮循环、
鼠标命中绕过 `MouseInBounds`、Braille 帧集 3 份、目录上溯循环 3 份、`ToolErrors` 被 6 处手拼。

## v0.96.90 (2026-09-11) — 全仓重复代码清理：7 条已致 bug 的重复 + 三处单一真源收口

用 6 路并行区域审查 + 一份机械检测（479 个非测试文件、8 行窗口、跨文件重复 409 组）过了一遍
全仓 137K 行 C#，合并去重后 33 条。**元结论：这个仓库不缺抽象，缺的是「采用」** —— 多数条目
不是「没人写过这个助手」，而是助手就在旁边、调用点绕过去了（`GitRunner` 自称「所有 git 调用都应
通过此类」被 4 处绕过；`PathSafety.Guard` 被 6 处绕过；`UiText` 建来就是为消重，零生产调用点；
`RunModalDialog` 的注释写着「收敛约 8 份」，而同文件 40 行外有 6 个私有方法没用它）。

本版做完「重复已造成真 bug」的 7 条 + 三处单一真源收口。

### 1. 七条重复已经变成真 bug

- **`PktLine` 短帧崩溃（Git，可打挂 clone）**：`ReadTolerant` 只判 `len <= 1` 就放行，而 protocol v2
  的 response-end-pkt 是 `0002` ⇒ `new byte[len - 4]` 得到 `new byte[-2]` 抛 **OverflowException**，
  错误信息完全指不到 pkt-line。抽出唯一实现 `ReadFrame`，非数据帧判据改为 `len < 4`
  （合法数据帧最小 4 字节 = 空 payload）。
- **沙箱 cd 逃逸（安全）**：`CheckWritable` 与 `CheckDirectoryEscape` 是同一策略两份实现，前者走
  `PathSafety.ResolveSymlinks`、后者只有 `Path.GetFullPath`（只折叠 `./..` 不跟随链接）⇒
  **「项目内 symlink 指向项目外」可绕过 cd 检查**，而同策略在 `CheckWritable` 拦得住。
  两者还共有「前缀比较不是路径段边界」的缺陷（`/proj-evil` 通过 `/proj`）。抽出
  `ContainmentReason` + `IsUnder`，两条链共用，两个缺陷一并修掉。
- **四个工具没有沙箱边界（安全）**：`rm` / `cp`(dest) / `mv`(两端) / `convert_encoding`(dst) /
  `find_replace` 手写 `CheckSensitive` 绕过 `PathSafety.Guard`，**因而漏掉 `SandboxManager.CheckWritable`**
  ⇒ 项目写边界下这些命令能写到 `AllowedDirectory` 之外，而 `write_file`/`edit_file` 不能
  （CLAUDE.md 的「边界轴」此前执行率 4/8）。全部改走 `Guard`，并把 `FindReplaceTool` 那句已经
  漂移的文案（少了「安全策略：」一截）收敛回单一来源。
  注：`cp`/`convert_encoding` 的 **src** 保留只查敏感路径 —— 读语义与 `read_file` 一致，
  沙箱管的是「能写到哪」。
- **`todo` / `struct_todo` 双向漂移（两个工具写同一份 `todos.json`）**：新增 `Tools/TodoStore.cs`
  （读写锁 + `Global.WriteAllTextAtomic` + 状态词表 + 依赖图操作），两个工具都接过去
  （`TodoTool` −161 行、`StructTodoTool` −134 行）。修掉三条：① **写盘耐久性**——`todo` 走原子写 +
  锁，`struct_todo` 是裸 `File.WriteAllText`、全文无锁，可撕裂对方写出的文件；② **状态词表分裂**——
  `struct_todo` 没有 `cancelled`，`todo` 标的它会被判「无效状态」；③ **文案在骗模型**——
  `struct_todo` 依赖缺失时返回「任务已创建但依赖无效」，而代码在 `todos.Add` 之前就 return 了，
  任务并没有被创建。顺带补上 `Global.MaxTodos`（此前 `struct_todo` 是无上限的那个口子）。
- **手机端「测试 Key」误报「Key 无效」**：`LLM.ResolveApiEndpoint` 是 private，Maui 手抄了一份
  不完整的、**漏了 `/v1beta/openai` 特例** ⇒ Gemini 被拼成 `.../openai/v1/chat/completions`（404），
  而真实对话是好的。改为 public，Maui 转调。
- **菜单滚动条与分隔线隐形**：`TuiMenu` 用 `fg: 8` 表达「暗」，但 `RenderBuffer.Write` 把 1..9 当
  **样式码**（不是颜色码）⇒ `ESC[8m` = **SGR 8 conceal（隐藏字符）**，而 dim 是 SGR 2。
  新增 `AnsiTty.StyleDim`（= 2）并替换三处。
- **主题的 user/system 配色是死键**：`TuiListItem` 三张表 + `TuiMarkdown.FgForRole` 四份平行实现，
  都把 user/system 硬编码成亮白，而主题 6 个变体共 24 处给 `ChatUserFg`/`ChatSystemFg`/`Icon*` 赋了值
  —— **没有任何渲染器读它们**。漂移还有：`tool` 在 `TuiMarkdown` 取 `ChatToolFg`、在 `TuiListItem`
  落 `_ =>` 取 `ControlFg` ⇒ 同一条工具消息的正文与角色名颜色不同源；`agent` 在两处映射也不同。
  新增 `UI/TUI/ChatRoleStyle.cs` 作唯一真源，两处改调。

### 2. `GitRunner` 收口（含一条每次都走的死锁路径）

`GitRunner` 的类注释写着「消除项目中 8 处重复的 `Process.Start("git")` 模式。所有 git 调用都应通过
此类」，但仍有 4 处自己拼：`Agent/SystemPrompt.cs`（两处）、`Infra/ContextBridge.cs`、
`Infra/ProjectInitAnalyzer.cs`。漂移后果不是风格问题：

- `SystemPrompt.RunGitCommand` 超时后**仍无界 `stdoutTask.GetAwaiter().GetResult()`** ——
  而 `ProcUtil.AwaitReadWithTimeoutAsync` 正是为「孙进程继承管道 → `ReadToEndAsync` 永不 EOF」
  而建；这条路径**每次构建系统提示词都要走**。
- `ProjectInitAnalyzer` 少了 `RedirectStandardInput`（该字段的注释点明它是「防与 TUI 主循环抢
  控制台 stdin → ReadKey 永久阻塞」的护栏）。
- `ContextBridge.RunGit` 只有 `WaitForExit()`，没有统一超时与 `KillTree` 兜底。

四处全部转调（保留各自「失败返回空串」的原语义）。顺带把 `GitRunner` 内部两份逐字重复的
`BuildStartInfo` 合并到一个 `BaseStartInfo` 底座上 —— 否则那道 stdin 护栏改一处漏一处。
**现在全仓已无手写的 git 进程。**

### 3. `FileWalker`：三份递归遍历收口

`GrepTool` / `WcTool` / `FindReplaceTool` 各写一份「逐目录 try/catch + 深度上限 + 跳过垃圾目录」，
且三张跳过表**互不相同、也都不等于** `FileIgnoreManager` 的权威表（25 条）。后果是
**「grep 查不到、find_replace 却改得到」**这类自相矛盾（grep 扫 `bin`/`obj`，find_replace 扫
`dist`/`build`），以及三者都不跳 `target`/`vendor`/`packages`/`.next`/`coverage`。

新增 `Tools/FileWalker.cs`：跳过判断以权威表为**底座**，各工具用 `extraSkipDirs` **显式**追加
自己的噪音项（grep 的 `dist`/`build`）——「默认一致、允许显式追加」，不再允许整张表另写一份。
顺带修掉 `ProjectInitializer.GlobAny` 的无预算 `EnumerateFiles(..., AllDirectories)` 与
`rel.StartsWith("bin")` 前缀匹配（漏 `src/bin`、误伤 `binary…`）—— 那正是 CLAUDE.md 记的
「home 下 12~36s」那类无界递归。

### 4. 构建 / 测试命令探测收口

同一套「marker 文件 → 命令」优先级链有两份实现，且**同一个仓库给出两种答案**：
`dotnet test` vs `dotnet test --nologo -v q`、`npm test` vs `npm test --silent`、
`pytest` vs `python -m pytest -q`、`cargo test` vs `cargo test -q`；npm 构建一侧无条件
`npm install && npm run build`、另一侧仅当声明了 `build` 脚本。⇒ **`/init` 写进 AGENT.md 的命令
与 Agent 自检实际执行的命令各说各话**。

收口到 `ProjectInitializer.DetectTestCommand(root, userOverride)` / `DetectBuildCommand(root)`，
`Agent.Feedback` 转调（用户覆盖 `Config.TestCommand` 仍在 Agent 侧取、作为最高优先级传入）。
统一取**静默形式**，npm 构建要求显式声明 `build` 脚本。

### 5. ⚠ 用户可见的行为变化

1. **沙箱边界补全（第 1 节）会挡住操作**：项目写边界（移动端 / `isProjectWrite`）下，
   `rm`/`cp`/`mv`/`convert_encoding`/`find_replace` 现在拒绝项目根之外的路径。桌面默认模式
   **不受影响** —— `CheckWritable` 首行就 `!IsProjectWrite → return null`。
2. **user/system 消息颜色现在跟随主题**（第 1 节最后一条）：此前硬编码亮白，6 个主题变体里那些
   配色从未生效。这是主题作者的原意，但确实是可见变化。想改回固定亮白：把 `TuiTheme` 的
   `ChatUserFg`/`ChatSystemFg`/`IconUserFg`/`IconSystemFg` 设成 `AnsiColors.BrightWhite` 即可。
3. **菜单滚动条/分隔线从隐形变可见**（修 bug 的直接结果）。
4. **`wc` / `find_replace` / `GlobAny` 的扫描范围变化**：现在会跳过权威表里的
   `target`/`out`/`vendor`/`packages`/`.next`/`coverage` 等。在这些目录下跑 `wc` 结果会变。
5. **`struct_todo` 语义补齐**：认 `cancelled`、有 `MaxTodos` 上限、依赖缺失时文案不再声称已创建
   （行为仍是拒绝创建，只是文案与实现一致了）。
6. **6 条既有自测断言按新语义更新**：它们锁的正是那两套矛盾答案中的旧一套。这不是「改测试凑绿」
   —— 命令串是本版有意统一的，另新增一条「npm 无 build 脚本 → 无构建命令」。

### 6. 自测护栏（+6 条，共 5180）

`struct_todo` 此前**没有任何测试**。新增 5 条锁住本轮修的三条漂移：认 `cancelled`、
依赖缺失拒绝创建且不说假话、跨工具共用同一存储（`struct_todo` 写 → `todo` 读得到 →
`todo clear` 清得掉）。另加 1 条 npm 构建语义。

**验证**：`--test` **5180 / 5180**（v0.96.89 为 5174）；桌面与 MAUI Android 构建均 0 错误。

**本版未做（33 条中剩 20 条，按价值排序）**：① 模型地址/服务商判定 8 处内联（大小写兜底失效、
未命中返回 null vs `"custom"` 不一）；② 模型切换收尾 `Agent.ApplyRuntimeModel` 被 TUI 五处绕过，
其中运行时回退那条干脆不设 `SmallModel`（跨服务商回退后小模型压缩请求打到旧网关）+ 写槽位
`SlotConfig` 的 40 行两份；③ diff 生成器两份逐字拷贝且**只支持单块改动、无 `@@` 头**（同文件两处
相隔较远的小改动会被呈现成「删掉中间全部 + 重新加」，误导模型）；④ 权限/经济文案 6 套（同一个 Ask
有 5 种叫法，`UiText` 零生产调用点）；⑤ Web 的 key 判定不查环境变量，与同文件另一处自相矛盾；
⑥ C 级 20 余条，其中 `TuiListView : TuiScrollView`、`UxHelper` 六个对话框收口到 `RunModalDialog`
（含「超时默认拒绝 vs 默认 null」这个会把权限弹窗从拒绝翻成允许的坑）、工具写文件流水线 4 份、
三份 `SendWithRedirectAsync`（重定向预算 5/10/5）收益最大。

## v0.96.89 (2026-09-11) — v0.96.88 的收尾：判据只换了一半 + 两轮 code-review 修复

上一版把「能不能开全屏界面」的判据从 `Console.IsInputRedirected` 换成了
`ConsoleDevice.CanUseFullScreen`，但**代码里其它地方仍把 `IsInputRedirected` 当作「非交互」**。
这一版把那批漏网的判据全部收口；第 7 节是本轮改动**自身**被 code-review 复核后修掉的问题。

### 1. 读键泵线程的闸门漏了（致命）

**问题**：能开界面 ≠ 会读键。`InputManager.EnsurePumpStarted()` 与泵循环内的守卫仍以
`Console.IsInputRedirected` 早退，而泵线程是字符源**唯一**的读者。于是 `waycoder < NUL`
（或任何喂空管道的启动器）现在会进 TUI、建好 `WindowsCharSource(CONIN$)`、翻掉控制台模式 ——
然后 `ReadInput()` 永远只返回 Timeout：**键盘、Esc、Ctrl+P/M、Tab、F1-F10、鼠标全部无效**，
已在跑的读线程反把用户按键吞进无人消费的队列；挂在同一线程上的心跳（spinner 动画 /
冻结看门狗 / CPU 采样）一起停摆。只有 Ctrl+C 能逃出去。

- **新增纯逻辑判据** `ConsoleDevice.HasKeyboard(stdin被重定向, 是否来自控制台设备)`
  = `!stdin被重定向 || 来自设备`；`InputManager.Init()` 按**实际建成的源**算出 `_hasKeySource`
  （建源失败退回 Unix 源时按 Unix 语义重算），泵启动闸门与循环内守卫都改看它。

### 2. 命令行槽位任务：三条独立缺陷

`RunReplAsync` 是 `_pendingSlotQueues` 的**唯一**消费者，它需要画布；v0.96.88 的守卫把
「stdout 被重定向」也一并拒了 ⇒ `waycoder -p1 "修复 bug" > run.log` 把任务投进队列后
直接打印「没有可用的控制台」并返回 1，**任务从未执行**。而无界面路径本身还差三件事：

- **没有画布 ≠ 没有工作**：抽出 `RunSlotQueuesHeadlessAsync()` 把队列跑完再退。
- **每槽位身份绑定漏了**：`-p4 "记住：本仓库用 xunit"` 会经 `StructuredMemory.SlotMemoryDir`
  把记忆写进 **slot_0**、并把 F4 的提示词拿去注入 F1 的记忆；`Agent.AgentId` 停在默认 `"main"`，
  使 `_agent_id` 工具注入、FileLockManager 跨槽位冲突归属、`LLM.DrainImages(AgentId)` 全按
  "main" 记账。新增 `EnsureSlotAgent(int)` 把三处槽位入口（SwitchAgentSlot / StartSlotTask /
  队列投递）共有的绑定收成一份。
- **恒返回 0 且不落盘**：`ProcessTextInput` 吞异常只打一行错误，方法末尾 `return 0` ⇒
  key 过期时「打一行失败、退出码 0、`sessions/slotN/` 空着」，违反 CLI 铁律①（有错即报错退出，
  绝不静默忽略）。现在 `ProcessTextInput` 返回成败，任一条失败即退 1，跑完 `AutoSaveSession()`。
- **`--json` 契约被破坏**：Main 的 jsonMode 分支只覆盖 `prompt != null`，`-p1`~`-p0` 时
  prompt 为 null ⇒ 会往 stdout 吐解码后的 ANSI 文本，`waycoder --json -p1 "…" | jq -r .answer`
  拿到不可解析的内容。现在 `--json` + 槽位任务**不进 TUI**，改由
  `RunOneJsonCoreAsync`（从 `RunOnceJsonAsync` 抽出，两条路径共用）每条任务输出一行
  `JsonResult`；进度提示走 stderr，stdout 只有一个 JSON 对象。

顺带把 TUI 内的投递循环与无界面路径合并到 `RunSlotQueuesAsync`（此前 headless 手抄了一份，
上面「身份绑定漏了」正是抄丢的），以后改投递语义只有一处。

### 3. `Console.In.ReadToEnd()` 阻塞 → 有界等待

v0.96.88 的标题是「无参数即可进 TUI」，但「被别的程序拉起」这条场景根本走不到新代码：
父进程给了管道却既不写也不关（Node/Electron `spawn` 的默认 stdio、IDE 集成、双击启动器）
会让 `Main` 永远卡在读 stdin 上 —— 没有窗口、没有提示、也没有退出码。

- 改为 `ReadRedirectedPrompt()`：判据是「**读到第一个字节** 或 **读到 EOF**，谁先到算谁」
  （`WaitHandle.WaitAny`）——先到 EOF（`< NUL`、`< 空文件`、`: | waycoder`）**立即返回**，不白等；
  先到首字节就确认在用管道喂提示词，**此后不再设限**一路阻塞到 EOF，慢生产方（分批 echo、
  慢 `curl`、脚本 sleep 后才写）的内容不会被截断；两者都没到（5 秒）才认定不是喂提示词的管道，
  放弃并**在 stderr 说明**，不再静默。
- **时限无条件生效**，不看「有没有界面可回退」：Unix 上 stdin 被重定向时 `CanUseFullScreen()`
  恒 false（`/dev/tty` 没进 raw mode），若因「回退不了就死等」跳过时限，就正好保留了这次要
  消灭的 Unix 挂死；放弃后由调用方走它自己的路（有画布→开界面，没画布→报错 + 退出码 1）。
- 读放在后台线程、时限加在**等待侧**：`Console.OpenStandardInput()` 不是 overlapped 句柄，
  `ReadAsync(..., token)` 取消不了已经发出的同步读。两个事件**故意不 Dispose** ——
  放弃后那条线程可能仍在跑，对已释放的 `ManualResetEventSlim` 调 `Set()` 会抛，
  而异常出在线程的 finally 里就是未捕获异常 → 进程直接挂（比它要解决的问题更严重）。
- 解码仍按 `Console.InputEncoding` + BOM 探测，与原来 `Console.In`（StreamReader 同一套参数）一致。

### 4. 控制台模式与句柄生命周期

- **Exit→Enter 往返不再丢 raw 模式**：输入裸 `!` 跑一条 shell 命令会 `Exit()`（把 CONIN$ 模式
  还原成行缓冲 + 回显）再 `Enter()`，而施加模式原本只发生在一次性的 `Init()` 里 ⇒ 往返之后
  按键要按回车才到、回显叠在 TUI 自绘上。现在 `TuiManager.Enter()` 每次调
  `InputManager.ReapplyConsoleMode()`（用 `Init` 记下的**设备句柄**；stdin 被重定向时
  无参 `WinConsoleMode.Enable()` 判重定向直接返回 false，够不到 CONIN$）。
- **探测句柄不再泄漏**：`CanUseFullScreen()` 原来把四个实参一次求值，`hasConsoleDevice` 那一项
  要真开一个 CONIN$ 句柄且从不释放 —— 每次 Windows 启动都漏一个（stdout 被重定向时根本用不上）。
  改为逐项短路 + `HasConsoleDevice()` 探测后立即 Dispose。
- **Dispose 顺序**：`InputManager.Dispose()` 先还原控制台模式再关字符源 —— `WinConsoleMode`
  记下的句柄就是这条流，顺序反了会 `SetConsoleMode` 到已关闭（甚至已被系统复用）的句柄上，
  恢复在 try/catch 里静默失败，终端被留在 raw 状态。
- **进程终结兜底**：`WinConsoleMode` 静态构造注册 `ProcessExit` 钩子还原模式 —— TUI 因异常或
  直接退出没走到 `TuiManager.Exit` 时，不至于把用户的 cmd/PowerShell 留在无回显状态。
  （被强杀 `taskkill` / 崩溃无解，靠下次启动的清残留逻辑缓解。）
- **`Dispose()` 里未守卫的 `Console.CursorVisible`**（本轮自测暴露的既有 bug）：`Init()` 有
  「非交互环境没有控制台句柄」的守卫，`Dispose()` 没有 ⇒ 在 stdout 被重定向的进程里调用会抛
  `IOException: 句柄无效`，**掀掉整个自测套件**（实测到过）。补上同样的守卫。

### 5. DiffPreview 逐 hunk 确认被静默降级（四处）

`EditFileTool` / `WriteFileTool` / `MultiEditTool`（两处）用
`cfg.DiffPreview && !Console.IsInputRedirected && !Console.IsOutputRedirected` 判「是否弹窗」，
把新的「TUI + 管道」环境判成非交互 ⇒ 开了 `/config DiffPreview true` 又经脚本/启动器启动的用户，
在非 YOLO 模式下编辑被**直接落盘、无逐 hunk 确认**，且毫无提示。

- 新增 `UxHelper.CanConfirmInline`（TUI 界面 / 交互式终端），四处统一改走它；
  `AskUserQuestionTool.CanAskUser` 也改为复用它（同一判据单一来源，防两处漂移）。
- ⚠ **它是 `WayCoder.Maui/CoreStubs.cs` 里同名桩类的一部分**：MAUI 工程排除了
  `../WayCoder/UI/TUI/**`（真 UxHelper 进不来）却编译 `Tools/**`，所以真类每加一个被 Tools
  用到的成员，桩里都要同步补一个 —— 漏了就**只在 MAUI 上 CS0117 编译失败**（桌面构建全绿，看不出来）。

### 6. 动态栏：遮挡期间整行重写 + 命名歧义

`SetContent()` 在 `CanDirectWrite()` 为假时仍调 `MarkDirty()`，而覆写版的 `MarkDirty()`
会置 `_rowInvalidated` ⇒ 恰好在注释所说「只补段」的那些帧强制整行重写。模态对话框在场时
agent 正在流式输出，每个 token / 费用 / 上下文变化都整行重刷底色，把遮罩画在本行上的像素抹掉
（「模态遮罩上打亮行」），也就是上一版声称修掉的那条。

- 拆成两个**名字即意图**的方法：`MarkContentDirty()`（内容变了，只进渲染帧、走段级补写）
  与 `MarkRowInvalidated()`（框架侧 `MarkDirty`/`Invalidate`：无法确定本行是否被浮层擦过，
  整行重写）。`SetContent` 走前者。此前两者只差一个 `base.` 前缀，读代码的人很容易「简化」掉
  —— 那正是上面那个闪烁 bug 的成因，且调用点文本一字未变、难以察觉。
- 内容不会丢：`OnRender` 每帧从当前状态重算三段文本，遮挡解除后的首帧会整行重写一次补回基线。

### 7. 本轮改动被 code-review 复核后修掉的问题

- **MAUI 编译中断**（本轮引入）：第五节那个 `CanConfirmInline` 忘了同步桩类 →
  Android/iOS 工程 5 处 CS0117 编译失败。已补桩并实跑
  `dotnet build WayCoder.Maui/WayCoder.Maui.csproj -f net10.0-android` 验证。
- **空管道白等**：`firstByte` 只在读到字节时置位、EOF 不置位，而主线程先只看 `firstByte`
  ⇒ `< NUL` / `< 空文件` / 立刻关闭的管道每次白等满一个时限（被替换的 `ReadToEnd()` 是立即返回的）。
  已改 `WaitAny(firstByte, finished)`。
- **慢生产方被静默丢弃**：首字节 2 秒后才到的提示词会被当噪声丢掉、任务不执行且毫无交代。
  已放宽到 5 秒 + 放弃时打印说明（见第三节）。
- **Unix 挂死没修**：`canFallBackToTui` 在 Unix 恒 false，原来的「有条件时限」等于没时限。
  已改无条件（见第三节）。
- **无界面路径的三条缺陷**（身份绑定 / 退出码 / `--json`）与**投递循环重复**：见第二节。

### 8. 自测护栏（新增 19 条）

- **读键闸门**：3 条纯谓词 + **2 条驱动真实泵启动路径**（`SetSourceForTest` 接缝 +
  `IsPumpRunningForTest`）。后者是关键 —— 自测进程自己就是重定向 stdin，纯谓词全绿也证明不了
  闸门真的看了 `_hasKeySource`。**已用「把闸门改回 `Console.IsInputRedirected`」实测反证**：
  3 条谓词依旧全绿、2 条驱动用例立刻红。
- **`--json` + 槽位任务**、**markup 屏直写登记**、**PopScreen 名单归还**、**遮挡期间段级补写
  而非整行重写**（此前「内容变化」与「显式标脏」两条相反契约没有任何测试区分，两边都只断言
  `IsDirty`，两种调用都为真）。

**验证**：`--test` **5174 / 5174 全绿**（v0.96.88 为 5163）；MAUI Android 构建 0 错误；
`echo "…" | waycoder --json` 的管道路径、`waycoder < NUL` 的无画布报错路径（退出码 1）、
`--json -p1`（stdout 恰好一行 JSON、退出码 1、提示走 stderr）、`sleep 3 | waycoder`（等满 EOF）
均实跑确认。

**已知未修**：① `TuiStatusBar`/`TuiTitleBar` 仍无条件整行重绘金色渐变（同源闪因，未收口到
机制层）；② `TuiScreen.Render` 的 dirty-rect 擦除落在 pass 1 之后，被擦的控件不一定被重绘；
③ 读键闸门的「真机字节路径」仍需 `--keypad` 的 `RAWKEY:` 手工复验（`waycoder < NUL` 后按键
是否真的生效）；④ `EnsureSlotAgent` 的每槽位绑定只有静态检查与「三处合并成一份」保证，
没有自动化用例（自测进程里 `_llm` 未初始化，难以直接驱动槽位 Agent 创建）；
⑤ 一次 `--test` 曾出现 `失败: 1` 但无 ❌ 行输出、其后连跑 6 次全绿，**未能复现**，
疑似既有的计时敏感用例（Retry jitter / 项目检测 < 3s）偶发。

## v0.96.88 (2026-09-11) — 无参数即可进 TUI（stdin 被重定向时改读控制台设备）+ 动态栏直写登记的致命漏登记

### 1. 不带参数启动全屏界面

**问题**：`waycoder`（无参数）在「stdin 被重定向」的环境里**静默退出**——零输出、退出码 0，
用户完全看不出发生了什么。实测 `waycoder < /dev/null` 就是这个结果。

**根因**：`RunReplAsync` 用 `Console.IsInputRedirected` 当「非交互」的判据，直接切到管道模式
逐行读 stdin；而 stdin 早已被 `Main` 的 `ReadToEnd` 读过（有内容就成了一次性提示词），
所以这条分支实际只剩「stdin 是空的」一种情况 —— 读不到任何一行，然后安静退出。
**「stdin 被重定向」不等于「没有终端」**：被别的程序拉起、脚本调用、双击启动器、
`waycoder < 文件` 都可能让 stdin 是管道，而进程仍挂着可用的控制台。

- **判据换成「有画布 + 拿得到键盘」**：`ConsoleDevice.CanUseFullScreen(stdin被重定向, stdout被重定向,
  能否开控制台设备, 是否Windows)`（纯逻辑，可自测）。stdout 被重定向 = 没有画布 → 不开；
  stdin 被重定向但 Windows 上能开 `CONIN$` → **照常开 TUI**。
- **读键改从控制台设备取**：`ConsoleDevice.OpenInput()` 在 stdin 被重定向时返回 `CONIN$` 流，
  `InputManager` 用它构造 `WindowsCharSource`，并把该句柄交给 `WinConsoleMode.Enable(handle)`
  ——stdin 是管道时 `GetStdHandle(STD_INPUT)` 拿到的是管道、拿不到控制台模式，必须作用在 `CONIN$` 上。
- **确实没有控制台**（CI / 服务 / 输出也被重定向）时打印三行说明 + **退出码 1**，不再静默。
  （`RunReplAsync` 改为返回 `Task<int>` 并由 `Main` 透传 —— `Environment.ExitCode` 会被
  `Main` 末尾的 `return 0` 盖掉，这个坑踩过一次。）
- **删掉已不可达的 `RunPipeModeAsync`**（逐行读 stdin）：它唯一的入口就是上面那条分支，
  而那种输入早被 `Main` 抢先读成 prompt 走一次性执行。`echo "任务" | waycoder` 行为不变。
- Unix 上仍不算这条路：`Console.ReadKey` 的 raw mode 绑在 stdin 上，单独打开 `/dev/tty`
  没进 raw mode（按键要等回车才到），所以「Unix + stdin 被重定向」仍旧报错退出而不是假开界面。

### 2. 动态栏直写登记的致命漏登记（code-review 关键发现）

**上一版（v0.96.87）的分区刷新在用户实际跑的界面上完全没生效。** 直写 spinner 与段级增量都靠
`owner` 门控（owner 必须是当前活跃屏幕），而 `RegisterDirectWrite` 只写着手写版 `ChatScreen`
的 `BuildLayout` 里 —— 默认界面却是**标记版 `MarkupChatScreen`**（`Program.Repl.cs` 里
`new MarkupChatScreen()`，手写版只是 chat.tui 加载失败的兜底），它覆写 `BuildLayout` 且不调 `base`。
于是默认界面 `_owner` 恒为 null → `CanDirectWrite()` 恒 false → `wholeRow` 恒 true →
**整行每帧被重写**，正是「空闲时整行闪烁」本身。验证也一并失真：`--keypad` 与自测建的是
手写版 `ChatScreen`，量到的「39 字节/帧」并不代表用户那道界面。

**修法：登记收到框架侧** —— `TuiScreen.RegisterDirectWriters()` 遍历控件树认领所有动态栏，
由 `TuiManager.PushScreen/PopScreen` 在 `Activate()` 之后调用（标记版在 `BuildLayout` 里才建树，
必须等 Activate 之后）。新增屏幕不必再记得手写这一句。

### 3. 同轮 code-review 的其余修复

- **`OnRender` 补过的段没刷新段缓存** → 紧随其后的 `RenderDirect`（同一帧内 `TuiManager.Render`
  写完帧就调 `RenderAllDirect`）会把同一段**再写一遍**。补上三处缓存赋值，v0.96.87 声称的
  「同帧不重复写」才成立。
- **`wholeRow` 丢了几何项**：上一版用整行签名时签名含 `absX/absY/Width`，改成标志位后丢了。
  输入区从 1 行长到 3 行（或在压缩进度行出现时）动态栏会整体挪一行，而内容可能一字未变 →
  段比对得出「无需重画」→ 老行留旧像素、新行只有 spinner。现在位移也触发整行重写。
- **遮挡期间不再强制整行**：原 `_rowInvalidated = !canDirect` 让「有对话框时」每帧整行重写
  （等于原缺陷对对话框场景复现），且模态遮罩只在全屏帧重画 → 会在暗底上打出一条亮行。
  改为**遮挡解除后的首帧**整行重写一次（遮挡期间仍按区段补，内容不丢）。
- **覆写 `Invalidate()`**：`MarkDirtyInRect`、`TuiScreen.MarkDirty`（`RootView.IsDirty = true`）、
  主题切换等入口绕过 `MarkDirty` 直接置 `IsDirty`，漏掉它们会让「已经脏了但段没变」写出零字节。

**验证**：`--test` **5163 / 5163 全绿**；新增护栏 12 条 —— 分区刷新 3 条（位移整行重写、
补过的段不重复写）+ 直写登记 3 条（未登记/登记/销毁摘除）+ 启动判据 5 条 + 无控制台退出码。

**已知未修（code-review 另有报告，留待下轮）**：① `_sectionModuleMap` 有 106 个 Section 没有映射，
`/test <模块>` 会「不跑它们却报全部通过」；② `TuiScreen.Render` 的 dirty-rect 擦除在 pass 1 之后执行，
被擦的控件不会被重绘（`ClampOverlayToContent` 允许高对话框压到动态栏行）；③ `TuiStatusBar`/
`TuiTitleBar` 仍无条件整行重绘金色渐变（同源闪因，未收口到机制层）。

## v0.96.87 (2026-09-11) — 动态栏改为「分区域刷新」：空闲时整行闪烁修复

**问题**：空闲时动态栏整行持续闪烁。内容是静止的（「空闲 / 上下文%、CPU%、token、花费」都不变），
唯一该动的是左边的 spinner —— 但整行每秒被重写约 20 次。

**根因**：`TuiDynamicBar.OnRender` 无条件把整行写进帧缓冲（底色 + spinner + 左/中/右三段）。
而增量渲染里叶子重绘的判据是 `child.IsDirty || parentDirty` —— 只要**别的控件**重绘时把本栏
当作父容器脏顺带带进来（`parentDirty` 为真），整行就会被重写一遍。整行重写一次 = 闪一次，
于是「什么都没变」的空闲态成了持续闪烁。

**修复：动态栏按区段刷新，各段时机不同**（spinner 每帧转 / 左段只在状态变时 / 中段只在工具变时 /
右段 📊⚡🔤¥ 在思考与流式期间持续跳变）。谁变写谁，谁都不带着别人重画：

- **`OnRender` 两条路径**。整行重写只留给三种情形：**显式 `MarkDirty`**（无法确定本行是否被浮层/
  窗口擦过）、**全屏重绘/切屏**（`IsIncrementalUpdate == false`，清屏后段缓存坐标已失效）、
  **直写不可用**（被遮挡 / 本栏不在活跃屏幕，帧内容必须自洽完整）。其余增量帧一律走**只补变化段**
  —— 未变的段一个字节都不写；内容一字未变时整帧零写入。
- **`MarkDirty` 覆写为「整行待重写」标志**（而不是只作废某个缓存）：`MarkDirtyInRect`、窗口关闭后
  的补绘都走这条路，只补变化段会让「被擦过但内容未变」的列留在错误状态。被遮挡期间该标志保持置位
  ⇒ **遮挡解除后的首帧也必定整行重写一次**。
- **`RenderDirect` 与 `OnRender` 共用同一份段写入器与段缓存**（`WriteSegment` / `WriteRightSegment` /
  `_lastLeft`/`_lastMiddle`/`_lastRight`）：`OnRender` 补过的段会刷新缓存，紧随其后的 `RenderDirect`
  查到「无段可补」只写 spinner —— **同一帧不会重复写**。
- **右段签名须含颜色与绝对列**：📊 跨阈值是「绿→黄」同文本换色、CPU% 从 `9%` 变 `100%` 会把后续
  项整体推移 —— 只比文本拼接会漏掉这两类变化，留下错色/错位。
- 这就是「**只要动画控件刷新，其余没变的内容不刷新**」的落点：防闪靠「没变的不重绘」，
  而不是「变了的晚点重绘」。

**顺带修掉两个真 bug**：

- `SyncDynamicBar` 每帧给 `DynamicBar.ContextPercent` **赋值两次**（先 `_contextPercent`
  再被真实值覆盖）。动态栏内容属性是「值变了就算内容变化」，同帧两次赋值 = 两次变化，
  直写不可用时就是逐帧整行重绘。改为**每个属性每帧只赋值一次**（真实值优先，取不到回退已知值）。
- `/test all` 被误报「未知模块: all」：`ModuleToSections("all")` 与「未知模块」共用 `null` 返回值，
  而可用列表里恰恰写着 `all`。`RunModule` 先分流 `all` 再判未知。

**验证**：`--keypad` 帧巡检（`FILL:30 / STATUS / DIRTY / FRAMES:8`）稳态每帧 **39 字节、只碰
第 17（动态栏 spinner）与第 19（输入区光标）行**；新增 4 条「分区刷新」护栏
（未变零写入 / 显式标脏整行重写 / **只改右段则只补右段** / 活跃屏幕前置），并把此前未登记的
`[TuiDynamicBar …]`、`[项目根解析边界]` 两段补进模块表 —— **5153 / 5153 全绿**。

## v0.96.86 (2026-09-11) — `--model/--base-url/--api-key` 成为真正的「本次启动强制连接」（不落盘）

**问题**：这三个参数本就存在、组合也生效（实测 `--base-url` 确实压过模型目录默认地址），
但语义不干净 —— 它们会经 `ApplyModelChoice → SetActiveConnect` **连带写出三个文件**
（`connections.json` + `config.json` + **`.env`**），与 `--model` 自身「本次会话，不持久化」的
说明矛盾；`--api-key` 还会在该服务商原本无 key 时**永久写入 `api_keys.json`**。
一次「换套参数试一下」变成了不可逆的配置变更。

- **新增 `Global.PersistDisabled`**：只改内存、不写盘。闸门设在三个写盘出口上 ——
  `ConnectionConfig.Save()`、`Config.SaveToConfigJson()`、`Config.SaveToEnvFile()`
  （与 `--offline` 的 `OfflineMode` 一样属于「窄区间开关」，由 Program 在应用这几个参数的
  极小闭区间内置位并 `finally` 还原）。
- **`Program.cs`** 把三者收成一个「强制连接」块：给了哪几项就覆盖哪几项，全程 `PersistDisabled`；
  `--api-key` 不再落盘（要永久保存请用 `/model key <供应商> <key>`）。
  内存状态照常更新（运行时镜像一致，压缩用的小模型/回退链读到的都是新连接），只是不写文件。
- 顺带把这个块里的**隐式顺序依赖**写明：`base-url` 必须在 `ApplyModelChoice` **之后**落到
  `_config` 上，否则会被 connect 推导出的地址覆盖 —— 以后别把这几个赋值散出去。
- **验证（端到端）**：用 `--model gpt-4o-mini --base-url http://127.0.0.1:9 --api-key sk-...`
  启动（死端口，零 token），实测 ① 请求确实打到 `127.0.0.1:9`；②
  `api_keys.json` / `connections.json` / `config.json` / 仓库 `.env` **四个文件哈希全部未变**。
- 自测新增 2 项「闸门双向」护栏（关时确实不写、还原后确实写），**5149 / 5149 全绿**。
- **注意**：`ExitCode` 之外，`PersistDisabled` 也不要用 `!= null`/布尔单独判断"是否出错"——
  它的语义是「这次启动改了就改」，与正常/异常退出无关。

## v0.96.85 (2026-09-11) — 命令行参数有错即报错退出（不再静默忽略后照常启动）

- **旧行为有多危险**：`CliArgRegistry.Parse` 对未知参数是 `continue` —— `waycoder --modle plan`
  这种**拼错旗标**会被悄悄吞掉，程序照常以默认配置进交互界面，用户以为参数生效了；
  `waycoder "写个 hello"`（漏 `-p`）同理，提示词被静默丢弃。裸位置参数本就不在用法
  （`waycoder [选项]`）里，也没有任何代码读原始 `args`。
- **新行为**（全部 `✘` 报错到 stderr + 退出码 1，**不启动**）：
  - 未知选项（含 `--modle` 这类拼写错误）
  - `--key=value` 形式但选项名未知
  - 位置参数（漏 `-p`）——附带提示 `提示词要用 -p 显式指定：waycoder -p "..."`）
  - 必需值缺失（如 `--prompt` 后面没东西）
  - 统一附一行 `查看全部选项：waycoder -h`
- **注意区分**：`ExitCode` 非 null 有两种语义 —— 「解析出错」（返回 1）与「终结型动作已处理、
  正常退出」（如 `--model list` 的 `OnMatch` 返回 0）。二者都靠 `Program.Main` 的
  `if (exitCode.HasValue) return exitCode.Value;` 退出，不要用 `ExitCode != null` 当错误判据。
- **跨模块风险已排除**：批量子进程（`BatchRunner.SpawnSelf`）以
  `-p <任务> -y --model/--base-url/--api-key/--max-budget-usd` 启动自身，这些名字全部已注册；
  且 `-p` 是 `ValueCount=1`（盲取值，不看是否以 `-` 开头），任务文本以 `-` 开头也能正确吃进。
  已加断言把这一组参数锁住 —— 否则严格校验漏认任何一个，批量任务会整批起不来。
- **验证**：实测 5 类非法参数均 `exit=1` 且打印对应错误；正对照 `--model list` / `--model=list` /
  `-h` / `--permit yolo --test` 均 `exit=0` 正常。自测新增 7 项护栏，**5147 / 5147 全绿**。
- **未覆盖**：GUI 版（`WayCoder.Gui`）参数面只有 `--edit`，其余交给 Avalonia 自己的启动器解析，
  本次未动 —— 需要的话可另做。

## v0.96.84 (2026-09-11) — GUI 输入卡下方显示当前工作目录（对齐 Web #cwd-bar）

- 输入卡下方新增一行 **`📁 <当前工作目录>`**，呈现对齐 Web 的 `#cwd-bar`：
  居中、12px、暗色（`DimTextBrush`）、单行不换行 + `CharacterEllipsis` 省略、`ToolTip` 提示「当前工作目录」、
  `MaxWidth` 与输入卡同为 940 保证左右对齐。
- **取值的依据**：GUI 的 `/cd`（`GuiCommands`）是只读信息命令、不切换目录，
  `CoreStubs.GetSlots()` 返回空数组（GUI 没有 AgentSlot 体系），`GuiBootstrap` 又是以
  `Directory.GetCurrentDirectory()` 设 `SandboxManager.AllowedDirectory` —— 因此 GUI 的工作目录
  就是进程启动目录，与 `/cd` 报告值和沙箱根**同源**，不会出现三处不一致。
- 复用 core 的 `PathStatus.FormatCwd`（主目录前缀折叠为 `~`）——与 TUI 状态栏同一套呈现，不另写一份。
- **验证**：`dotnet build -t:Compile WayCoder.Gui` 0 警告 0 错误（XAML 结构已由编译器校验）。

## v0.96.83 (2026-09-11) — GUI 聊天输入框聚焦不变黑、无焦点外框（只留光标）

- **现象**：GUI 输入框获得焦点时整块变黑并出现焦点边框。
- **根因**：控件上设的 `Background="Transparent"` / `BorderThickness="0"` 压不住 Fluent 的焦点态 ——
  焦点视觉是**带伪类的样式触发**（`:focus` / `:pointerover`，优先级高于本地值），且作用在
  **模板内部的 Border** 上（根本不在同一个元素）。两处各堵一半，所以本地值形同虚设。
- **修法**（只作用于 `ChatInputBox`，不影响其它输入框）：
  - `App.axaml` 新增 `local|ChatInputBox` 及其 `:focus` / `:pointerover` 态的样式，控件层
    `Background=Transparent` + `BorderThickness=0`（与主题同优先级、声明在后 → 胜出）；
  - 同样三条再以 `/template/ Border` 选择器覆盖模板内部的 Border；
  - `MainWindow.axaml` 给输入框加 `FocusAdorner="{x:Null}"`（去掉焦点装饰外框）+
    `CaretBrush="{DynamicResource CaretBrush}"`（保留光标，用主题色）。
- 效果：聚焦时输入框保持与背景同色、无边框、无外框，只有光标。
- **验证**：`dotnet build -t:Compile WayCoder.Gui` 0 警告 0 错误（GUI 当时正在运行，
  bin 复制被 .NET Host 占用，故只做了编译验证；视觉效果待重启后确认）。

## v0.96.82 (2026-09-11) — 修复 GUI 聊天输入回车不发送（只能点按钮）

- **现象**：GUI 版在输入框按回车没反应，必须点「发送」按钮。
- **根因**：`MainWindow.axaml` 上挂的是普通（冒泡）`KeyDown` 处理器，而 `TextBox`
  （`AcceptsReturn="True"`）自己的**类处理器**会先一步消费 Enter —— 插入换行并置 `Handled=true`，
  挂在同一元素上的普通处理器随即被**直接跳过**，`SendAsync` 永远走不到。
- **修法**：不在 XAML 挂处理器，改为新增 `ChatInputBox : TextBox` **覆写 `OnKeyDown`**
  —— 我就是那个类处理器，Enter 的处理顺序不存在歧义：
  - Enter（无 Shift）→ 触发 `SendRequested` → 宿主调 `SendAsync`，`return` 不调 `base`（否则发送后输入框会留一个空行）
  - Shift+Enter → 放行给 `base.OnKeyDown`，由 TextBox 正常插入换行
  - 覆写 `StyleKeyOverride => typeof(TextBox)`：Avalonia 按 StyleKey 查 `ControlTheme`，
    子类默认用自己的类型作 key 会找不到主题、输入框退化成无边框无光标的裸控件。
- 顺带修正占位符文案：原写「Ctrl+Enter 发送」与实际行为（Enter 发送）不符，改为
  「输入消息…（Enter 发送 · Shift+Enter 换行）」，与 Web/TUI 端一致。
- **验证**：`dotnet build WayCoder.Gui` 0 警告 0 错误。（GUI 无 headless 测试基建，
  行为待实机确认 —— 但「原处理器被跳过」这一现象本身即证明 Enter 是在 `OnKeyDown` 被消费的，
  覆写点正确。）

## v0.96.81 (2026-09-11) — 动态栏段级刷新：只重写变化的段，不再整行重画

承接 v0.96.79 的「动态栏内容变化才刷新」，本轮把粒度做细到**段**：此前任一段的值一变就整条重绘
（约 1/3 屏宽），而思考与流式期间 token/花费数字持续跳变 ⇒ 每秒数十次整条重画。

- **动态栏拆成 4 个区域**：spinner / 左段(状态) / 中段(工具) / 右段(📊 上下文 · ⚡ CPU · 🔤 token · ¥ 花费)。
- **内容与布局只有一份实现**：新增 `BuildLeftSegment` / `BuildMiddleSegment` / `BuildRightItems`，
  `OnRender`（整行渲染）与 `RenderDirect`（段级直写）共用，杜绝两处漂移。右段连**绝对列**一起产出
  —— 各分支列步进不同（进度条后留 4 列），只返回文本还原不了布局。
- **`OnRender` 记录各段「已写到屏幕的内容」**，`RenderDirect` 每帧只重写与记录不同的段；
  段级直写按段宽补空格（文本变短时刷掉旧字符），右段收尾补白到右端，防残留旧数字。
- **内容变化不再整行标脏**：`SetContent` 优先交给段级直写；**仅在直写不可用时**
  （被对话框遮挡 / 本栏不在活跃屏幕）才退回 `MarkDirty` 整行重绘兜底 —— 内容任何情况下都不丢，
  遮挡解除时屏幕重绘会重新写入并刷新段记录（用户要求的「遮挡消失后要刷新」由此闭合）。
- **自测新增 2 项契约护栏**：①内容未变时左/中/右段都不重写（且 spinner 仍在写）；
  ②只改右段时只重写右段、左/中段不动。
- **验证**：同一脚本在改造前后渲染输出逐字符一致（归一化时间戳 / spinner 帧 / 版本号后**完全相同**）；
  流式帧收益保持（342 B、仅 14-15 行）；空闲帧仍 39 B（只写 spinner + 输入区光标）。

## v0.96.80 (2026-09-11) — 修复思考中聊天区闪烁：流式追加不再整视口擦除重绘

v0.96.79 定位到的根因本轮修掉：**思考时每个流式 token 都把整个聊天视口擦掉重画一次**，
表现为「模型一转、聊天区闪一下」。桌面自测 5138 / 5138 全绿。

- **`TuiListView` 新增「内容级脏」窄路径** `MarkItemContentDirty(index)`：只擦**该条目自己的行区间**
  + 末项底边以下的空档，不再擦整个视口。整视口擦除是给「滚动 / 条目增删 / 多项位移」的保险，
  按流式频率（每渲染帧一次）做就是闪烁之源。
- **`ChatScreen.FlushStreamingLayout` 分流**：仅当「**只有最后一条**正文在变」（流式追加的常态，
  其后没有条目需要跟着位移）才走窄路径；多项变化 / 中间项变高会把后续条目挤下去，仍走
  `MarkTreeDirty()` 全量，否则未标脏的条目不重绘会留错位残影。
- **两道安全阀**：
  - 窄路径仍**整棵子树标脏**（`SetTreeDirty`）——`TuiView` 的 `parentDirty` 只向下传播一层，
    只标容器的话标题等叶子不会重画。实测过：漏标的直接后果是流式消息的「● 智能体」标题行被擦成空白。
  - 若**滚动偏移本帧变化**（流式触发自动滚到底），可视条目整体位移 → 自动退回全量擦除。
- **实测收益**（`--keypad` 逐帧字节数 / 重绘行）：

  | 流式帧 | 修复前 | 修复后 |
  |---|---|---|
  | 首帧（全量，正常） | 8870 B | 8870 B |
  | 第 2 帧 | 1794 B，重绘行 **2-15**（整片聊天区） | **342 B，重绘行 14-15** |
  | 第 3 帧 | 1797 B，同上 | **345 B，同上** |

- **正确性验证**：同一脚本在修复前后各跑一遍，把渲染帧文本逐字符对比 —— 归一化时间戳后除
  spinner 动画帧（本就随动画变化）外**完全一致**，确认性能收益未换来视觉回归。
- **动态栏遮挡恢复**：实测「对话框在场 → Esc 关闭」三态，动态栏内容均完整、无残留
  （对话框受 `OverlayBottom` 约束本就不覆盖动态栏）。

## v0.96.79 (2026-09-10) — 动态栏与聊天区解耦（去掉动画定时标脏）+ 帧范围诊断工具

### 已修：动态栏动画不再按节拍拖拽整屏重绘

- **`TuiDynamicBar` 内容属性改为按值标脏**：`Status` / `LeftText` / `ToolText` / `TokenDisplay` /
  `CostDisplay` / `ContextPercent` / `CpuPercent` / `ProgressPercent` / `ProgressLabel` 此前都是
  **普通自动属性**（赋值不标脏），只能靠 `ChatScreen.SyncDynamicBar` 里「每 250ms 无条件 `MarkDirty()`」
  的定时器硬刷 —— 那等于按 spinner 的动画节拍把整屏反复拖进渲染路径。
- **删除该定时器**：spinner 动画本就走 `TuiDynamicBar.RenderDirect` 直写终端（不依赖脏标记，
  空闲态照常转），定时器纯属冗余。改后「动态栏何时重绘」完全由**内容是否变化**决定：
  内容不变 → 不标脏 → 不重绘；实时数字（token/花费/上下文）仍随值变化即时更新。

### 根因定位（聊天区闪烁，已于 v0.96.80 修复）

思考中聊天滚动区持续闪烁，**与动态栏无关**，根因在流式追加路径：

```
LLM.cs:725        推理 token 走 onToken 回调（与正文同一条流）
  → AppendToken → AppendToLast → QueueStreamLayout()
  → 渲染帧 FlushStreamingLayout() → ChatList.MarkTreeDirty()      ChatScreen.cs:1032
  → TuiListView.OnRender 整视口「擦除 + 重绘」                     TuiListView.cs:263-282
```

即**每个推理 token 都会把整个聊天视口擦掉重画一次**（`TuiListView` 一脏就 `Fill` 满视口再逐项重绘），
推理持续期间即为连续闪烁。`ChatScreen.cs:1029` 的注释也确认了这一取舍（「必须整棵子树标脏，
未变消息才不会被擦掉」）。方案（跳过整视口擦除、只重绘变动行）已评估，改动落在共享列表控件上
且需真机目视确认，**待确认后再动**。

### 工具：渲染帧范围诊断

- **`--keypad` 新增 `FRAMES:<n>`**：连续渲染 n 帧，逐帧报告**写出的字节数 + 光标定位到的行 + 文本**。
  这是判断「区域是否联动刷新」的直读工具：健康帧应只有动画行与光标行、约 40 字节。
- 另加 `STATUS:<名>`（强制动态栏状态）与 `DIRTY`（只标脏不渲染）。
- 新增脚本 `Test/scripts/frames.txt`（含判读要点），替代临时诊断件。
- 自测新增 4 项护栏锁定「同值赋值不标脏 / 变值标脏」契约（**5138 / 5138 全绿**）。

## v0.96.78 (2026-09-10) — 修复 VT 字节流丢键：退格无法擦除、Ctrl 组合键与 F1-F4 失效

v0.96.74 把 Windows 读键从 `Console.ReadKey()` 换成 VT 字节流（为了收 SGR 鼠标），但字节流层
**丢失了修饰键信息**，而几处「按字节还原按键」的映射没补齐——表现为退格擦不掉输入、Ctrl 组合键
静默失效、F1-F4 槽位键失效。桌面自测 **5134 / 5134 全绿**，0 警告 0 错误。

### 根因：字节流 → ConsoleKeyInfo 的还原有三处缺口

- **Backspace 收不到**：VT 输入下终端对 Backspace 发的是 **DEL(0x7F)** 而非 BS(0x08)，
  而 `MapToConsoleKey` 只映射了 `'\b'` → `Key=NoName`。编辑控件都按 `Key==ConsoleKey.Backspace`
  分支判（`TuiEditBase`/`TuiChatInput`），于是**退格无法回退擦除**。
- **Ctrl+字母全失效**：终端未协商 Kitty 协议（conhost/旧终端忽略 `CSI >1u`）时，Ctrl 组合以
  **控制字节 0x01..0x1A** 到达，`Modifiers` 恒为 false；而 `Program.Repl` 判的是
  `Modifiers.HasFlag(Control)` → Ctrl+P/E/M/B/S 全部静默失效。
- **F1-F4 失效**：xterm / Windows Terminal 对 F1-F4 用 **SS3 形态 `ESC O P/Q/R/S`**（序列里没有
  `[`），`TryParseEscapeSequence` 只认 `[` → 落进「Alt+字符」分支，F1-F4 退化成 Alt+O/P/Q/R，
  **F1-F10 槽位切换键整排失效**（v0.96.74 只补了 CSI 形态，漏了 SS3）。

### 修复

- **`WindowsCharSource.ToConsoleKeyInfo(char)` 成为唯一实现**（`TryReadKey` 与
  `InputManager.ToConsoleKeyInfo` 共用——此前两处各写一份，两处一起漏）：0x7F → Backspace
  （KeyChar 归一为 `'\b'`，与 Kitty 路径一致）；0x01..0x1A → Ctrl+字母（还原 Key 与 Control 修饰键）。
- **歧义码位保持既有语义不动**：0x08(BS)/0x09(Tab)/0x0A(LF)/0x0D(CR)/0x1B(ESC)——它们在 Unix 上
  与 Ctrl+H/I/J/M/[ 同码，代码库既有约定见 `TuiKeybindHelp`，不在字节流层重新分配。
- **新增 `InputManager.MapSs3Key`**（纯函数，便于单测）：SS3 终止字节 → F1-F4 / 方向键 / Home/End；
  未识别返回 null（不吞按键）。`TryParseEscapeSequence` 在读到 `ESC O` 时走这条。
- 删除因此变为死代码的 `InputManager.ToConsoleKey`。

### 测试

- **`--keypad` 新增 `RAWKEY:<hex>` 指令**：按**终端原始字节**喂键（`字节→WindowsCharSource→
  ConsoleKeyInfo→OnKey`）。此前的 `KEY:`/`INJECT` 直接注入 `ConsoleKeyInfo`，**绕过了字节映射层**，
  结构性无法复现本次这类问题（这正是它漏网的原因）。仅覆盖单字节键，方向/功能键序列仍用 `KEY:Up`。
- 新增脚本 `Test/scripts/allkeys.txt`：全键盘巡检，覆盖 打字基线 → KEY/RAWKEY 两条退格路径 →
  BS 变体 → 连退到空 → Ctrl 组合（用 Ctrl+A 全选 + 输入验证修饰键真的到了编辑控件）→ 歧义码位。
- 自测新增 12 项护栏（含**直接喂 0x7F 走真实 `WindowsCharSource` 的字节级断言**，即在真机路径上复现）。

## v0.96.77 (2026-09-10) — 自测离线化 + 项目根解析性能修复（106s → 35s，5122 全绿）

自测全程不再触网、不再产生 token 费用，也不再对 gitee 执行真实 push；同时修掉一个「非项目目录下
系统提示词构建卡 12~36 秒」的生产性能缺陷。桌面自测 **5122 / 5122 全绿**（`WAYCODER_STRESS` 未开），
0 警告 0 错误，**全量耗时 106~121s → 35s**。

### 性能：真凶不是 Generate，是 DetectProject

此前自测里 4 个「15~18s 慢测试」都被记为 `SystemPrompt.Generate`，实测 `Generate` 只要 0.65~0.9s。

- **根因**：`ProjectContext.FindProjectRoot()` 先查项目标志、**后**查主目录边界。用户主目录下有
  一个 `package.json`（很常见）时，home 就被当成项目根 → `DetectLanguages` 递归遍历整个 home
  （几十万文件）。实测 `DetectProject` 在非项目目录 **12~36s**，在仓库内只要 88ms。
  **这是生产 bug**——非项目目录下每次构建系统提示词都要吃这个开销。
- **修复**：边界判定（`Global.Home` / 用户主目录 / 盘根）**前移**到标志检测之前；新增
  `UserProfileDir` 让护栏在 `HomeOverride` 下（自测 / MAUI）依然生效；`WalkFiles` 增加
  `MaxDirsPerScan = 2000` 目录预算兜底——无论根选成什么，扫描都不会失控。
- **效果**：`SystemPrompt.Generate` 14s → **0.17s**；`DetectProject`（非项目目录）36s → **5.9ms**。

### 离线硬护栏（`Global.OfflineMode`）

自测 / CI 期间置位、`finally` 还原，生产路径恒为 false：

- **`LLM`** 在**真正发包处**拒绝非本机端点（只拦发送，`Endpoint` 等纯展示路径不受影响）→
  跑测试**不可能产生 token 费用**
- **`Config.Env.FindEnvFile`** 离线模式不发现 `.env` → 真实密钥不再被导入测试进程
  （此前临时 home 不在 cwd 祖先链上，`FindEnvFile` 的「上溯到 home 为止」护栏失效，会一路走到
  盘根命中仓库根 `.env`）
- **`ModelCli.ProbeEndpointAsync`** 跳过外部端点探测

### 消除两处真实外部副作用

- **`TestList` / `POST /models/scan` 曾带着真实 API key 探测真实服务商**（实测
  `api.inferera.com`、`api.deepseek.com` 均返回 200）。新增 `ProbeBaseUrlOverride` 测试接缝，
  `TestWebFull` 外壳把探测整体重定向到本地 mock —— 覆盖率不减，4004ms → **1.2ms**。
- **`git_pr push` / `url` 曾在仓库根 CWD 下真的对 gitee 执行 `git push`**（仓库 `.env` 里还存着
  `GITEE_TOKEN`）。改到【临时仓库 + 本地裸远端】，仍走完整真实代码路径，但零外网、零远端副作用。

### 修复环境依赖的失败测试

- **`bash 大输出不死锁`**：原命令用 Unix 专有的 `yes`/`head`，而 Windows 的 BashTool 走
  `cmd.exe` → 输出恒为空 → 从 PowerShell 跑必红、从 Git Bash 跑才绿。改平台自适应命令。
- **`lint C# 项目不崩溃`**：无参调用把 CWD（仓库根）当目标，真的对 WayCoder 自身跑
  `dotnet build`（写仓库 `obj/bin`、可能访问 NuGet、与在途构建抢文件锁）。改到临时最小项目。

### 自测工程化

- 逐条测试打印**时间标签** `[x.x ms]`，末尾排行扩到 Section 前 15 / 测试项前 20，**全量明细落盘 CSV**
  （`%TEMP%\waycoder-selftest-timing.csv`，带 UTC 时间戳便于优化前后对比）
- `ApiKeyStore.ClearCache()` 前后各一次，同进程 `/test` 不会污染 REPL 的真实密钥
- 新增回归护栏：非项目目录下项目根不得落在主目录 + 检测 < 3s

## v0.96.76 (2026-09-10) — 核心纯逻辑上移 + 自测护栏扩面（5083 → 5127）

对近期修复补自动化回归护栏，并把 MAUI 里的纯逻辑上移 core 使其可被主自测覆盖。桌面自测 5127 / 5127，MAUI Android 编译 0 错误。

- **TUI 输入链护栏**：`ParseCsiFuncKey` 提 internal + Chunk19 断言裸 CSI 光标键（`A-D/H/F`）、`1~..6~`（Home/Insert/Delete/End/PgUp/PgDn）、`15~`=F5、`Ctrl+Left` 修饰键——**不得退化成裸 ESC**（finding#1 回归护栏）；`MapToConsoleKey` 字母/数字/空格/ESC/CJK 断言
- **纯逻辑上移 core（可测）**：
  - `WayCoder/UiText.cs`：PermName / EconomyName / FormatK / IsSessionBodyRole（`MauiUi`、`MauiSessions.FromNodes` 委托它）
  - `WayCoder/PageRouteMap.cs`：`/topage` 页面名（含中文别名）→ Shell 路由映射 + Usage（`MauiCommands` 调用它）
- **既有核心逻辑补断言**：`SlashMatcher`（四端共用唯一实现，主名/别名/带参/大小写/不匹配）、`ContextManager.EstimateText`（ASCII 0.25 / CJK 1.5 每字、emoji 代理对安全、单调不减）
- **护栏累计**：TUI UTF-8 状态化、CSI 功能键映射、跨端文本、路由映射、命令匹配、token 估算

## v0.96.75 (2026-09-10) — MAUI 死成员清理 + WindowsCharSource UTF-8 自测护栏

v0.96.74 后的收尾：清理 MAUI 抽屉独立页化残留的死成员，并为 Windows 输入链的 UTF-8 状态化解码补自测（可注入流）。Android Debug 编译 0 错误；桌面自测 5089 / 5089。

- **MAUI 清理残留死成员**（`ChatPage`）：CommandRow / EconomyName / ColorKey / ShowTasksAsync（抽屉独立页后各仅剩定义无引用）删除，净删 42 行
- **WindowsCharSource 可注入构造**：加 `Stream` 注入构造（生产无参 = Console 标准输入），解码逻辑不再锁死真实控制台，可单测
- **UTF-8 状态化解码自测（回归护栏）**：新增 `SelfTest.Chunk19`——emoji 代理对高位/低位完整返回、跨读边界拆包（先喂前导字节后喂续字节）不丢字节/不出 U+FFFD、RS(0x1E) 分隔符跳过、ASCII 直通；主自测 5083 → **5089** 全过（finding #3 若复发先红）

## v0.96.74 (2026-09-09) — TUI 鼠标 Windows 支持（统一字符源）+ 卡死修复（2026-09-10 code-review 修复后，待实机复验）

> 原始 WIP 记录：本轮把 TUI 输入读键链路重构为统一字符源（Windows 用 VT 字节流、macOS/Linux 用 Console.ReadKey），
> 让 Windows 也能收到 SGR 鼠标序列 —— **已实机验证字节流可达、`ParseSgrMouse` 解析成功**。鼠标乱码与 motion 泛滥两处曾未解决。

**2026-09-10 code-review 修复（Windows 输入链，桌面自测 5083/5083 通过）**：
- **ESC 塌缩**：Windows VT 输入下方向/Home/End/Delete 等以 `ESC[A-D/H/F`、`1~..6~` 到达但解析返回 null → 泵退化成裸 ESC **取消在跑 agent / 丢聊天草稿**。`ParseCsiFuncKey` 现显式映射（含修饰键）
- **回显 / 行缓冲**：`WinConsoleMode.Enable` 追加清 `ENABLE_LINE_INPUT|ENABLE_ECHO_INPUT`——原生字节流前提完整，消除 conhost 按键回显叠加到 TUI 自绘（重复/鬼影字符，**疑「鼠标乱码」真因之一**）与行缓冲等 Enter 成批到达
- **UTF-8 多字节损坏**：`WindowsCharSource.TryReadChar` 改状态化（跨 64B 读边界缓存续字节，不再丢首字节/出 U+FFFD）+ 代理对高位先返、低位缓存（emoji/CJK 粘贴不乱码）
- **粘贴误确认**：`ReadConfirmKey` 鼠标点按须落在确认提示行跨度内才 Y/N，任意位置点击不再误自动确认/取消
- **Unix 点击定位降级**：`Console.CursorTop/Left` 缓存不可靠 → Unix 输入区点击定位跳过（避免光标落错位），Windows VT 路径保留
- **映射收敛**：char→ConsoleKey 收敛为 `WindowsCharSource.MapToConsoleKey` 单实现（删死代码 `ToKeyEvent`）

**仍需 Windows 真机复验**：① 屏幕/输入框鼠标乱码（`^[[<64;95;48M`）在回显/行缓冲修复后是否消除；② `?1002h` motion 泛滥是否仍存在；③ macOS 鼠标实机行为。

- **输入通道统一字符源重构（核心）**：新增 `UI/TUI/Base/CharSource.cs` —— `ICharSource` 抽象 + `WindowsCharSource`（`Console.OpenStandardInput` 读 VT 字节流）+ `UnixCharSource`（macOS/Linux 保 `Console.ReadKey`）；`InputManager.PumpKeys`/`TryParseEscapeSequence`/`TryParseCsiFunctionKey`/`ReadPasteContent` 读键全部收敛到统一源，Windows 用字节流收 SGR 鼠标。macOS/Linux 路径与原逻辑等价（零回归）
  - **Windows VT 输入启用**：新增 `UI/Shared/Terminal/WinConsoleMode.cs` —— `GetStdHandle`/`GetConsoleMode`/`SetConsoleMode` P/Invoke（`ENABLE_VIRTUAL_TERMINAL_INPUT` + 关 `ENABLE_QUICK_EDIT_MODE`），`TuiManager.Enter/Exit` 接线；非 Windows/重定向 no-op，macOS/Linux 不碰 kernel32（AOT 可移植）
  - **跳过 Windows Terminal 的孤立 RS 分隔符**（`\x1e`，SGR 鼠标序列后的记录分隔，macOS 无此字节）
  - **方向键/功能键保留**：`ICharSource.TryReadKey` 返回完整 `ConsoleKeyInfo`（`Key=UpArrow` 等不丢），macOS/Linux 方向键回归已测
- **Windows 原始通道两件套（探针实证）**：仅改字节流不够，还须开 `ENABLE_VIRTUAL_TERMINAL_INPUT` 终端才把 VT 序列交付为字节——`--mouse-probe` 用真实 `WindowsCharSource` 实机验证字节流可达 + `ParseSgrMouse` 解析正确
- **统一鼠标消费补齐**：`TuiChatInput` 输入区鼠标点击定位光标（复用 `ScreenToHard`）+ `ReadConfirmKey` 左键=确认 Y/右键=取消 N；`Terminal.cs` 加 `Tty.CursorTop/CursorLeft`（IO 异常防护）；`TuiMouseTest.cs` 更新过期"不支持列表"注释（各 Picker 已走 RenderWait 路由鼠标）
- **卡死修复（前述根因）**：异步 REPL 主循环 await 后线程迁移，`TuiScreen.IsUiThread` 比对构造线程快照失效 → `RenderWait` 误判后台线程只空转 = 卡死；改由 `TuiManager.UiLoopThreadId`（`Render`/`ReadInput` 记录实时循环线程）+ `RenderWait` ownLoop 期间 `SetActivity` 刷新看门狗。**此部分已修**
- **已知未解决（供后续）**：① 鼠标事件屏幕乱码，`^[[<64;95;48M` 来源未定位（不在泵线程日志，疑第二条读取路径）；② `?1002h` 在 Windows Terminal 触发 motion 泛滥（每秒几百 `MouseMotion`）；③ macOS 实机鼠标未验证（理论等价）
- **自测**：桌面 Debug `dotnet run -- --test` 通过 5076 / 5076，失败 0（含 Lint、方向键、TuiMouse）；`--tui-mouse` 76 项全过

## v0.96.73 (2026-09-09) — MAUI 清理抽屉死代码（净删 355 行）+ 自测基线

会话历史/侧栏自 v0.96.65 改 Shell 独立页后，抽屉浮层整套代码（XAML DrawerLayer/左右抽屉、ChatPage 开合动画/边缘 Pan/scrim/Populate 面板/NewSession 链等）一直是未调用死代码。本轮删除并记录桌面自测基线。Android Debug 编译 0 错误，模拟器启动正常。

- **抽屉死代码清理**：删除 ChatPage.xaml DrawerLayer/DrawerScrim/LeftDrawer/RightDrawer/LeftBody/RightBody 整块（52 行）+ ChatPage 抽屉相关字段与 18 个方法（303 行）：开合动画（Open/Close/CloseDrawersAsync）、边缘 Pan（OnLeft/RightEdgePan）、scrim 关闭、Build/Populate 左右面板、RefreshSessionList、NewSession/NewSessionAsync/ManageSessions 链、NavThen、OnNewSessionClicked/OnDrawerScrimTapped、OnSizeAllocated 抽屉宽覆盖；清理残留 `RefreshSessionList()` 调用
- **文档同步**：CLAUDE.md 更新 v0.96.65 条目"抽屉旧 UI/方法暂留为未调用死代码待清理"为"v0.96.73 已清理"
- **自测基线**：桌面 Debug `dotnet run -- --test` 通过 5083 / 5083，失败 0——Android 网络 handler 恢复改动（v0.96.72）在桌面 core 无回归

## v0.96.72 (2026-09-08) — Android 恢复系统代理 + UI 重复代码提炼（MauiUi 共享助手）

修复 review 遗留的 Android 网络能力问题，并把移动端多处重复的取值/渲染 helper 收敛为单一来源。Android Debug 编译 0 错误。

- **Android 恢复系统网络能力（review finding）**：`LLM.CreateHttpClient` / `TranscribeAudioTool` / `WebSearchTool` 撤销强制 `SocketsHttpHandler`、恢复系统默认 handler（Android=AndroidMessageHandler）——保留 Wi‑Fi/系统代理、VPN、network-security-config cleartext、用户安装 CA（自建网关场景回归修复）。前提：网络调用不在主线程——agent 已由 AgentService `Task.Run` 后台执行，UI 入口（聊天页录音转录）补 `Task.Run` 移后台，避免主线程 Java 流 NetworkOnMainThreadException
- **`MauiUi` 共享助手（DRY）**：`WayCoder.Maui/Services/MauiUi.cs` 收敛各页重复——主题色取值 `Res/ResOrNull`、深色判断 `IsDark`、确认权限名 `PermName`、经济模式名 `EconomyName`、千分位 `FormatK`、当前模型文本 `ModelText`；ChatPage（含死代码薄委托）/CommandPanelPage/SessionHistoryPage/ToolCallsDetailPage 全部改用
- **载入渲染合一**：`EnsureSessionAsync` 与 `SwitchToSessionAsync` 的「FromNodes→富文本→AddMessage→滚底」循环收敛为共享 `AppendHistoryNodes`，并清掉不再用的局部 `isDark`

## v0.96.71 (2026-09-08) — MAUI 移动端 code-review 二轮 5 项修复（会话持久化/队列/详情页预算）

承接 v0.96.70 增量会话保存模型（`_sessionRaw`/`_appAddCount`）的 code-review 复核，修复 5 项会话持久化与队列问题。Android Debug 编译 0 错误。

- **流式中离页丢回复（回归）**：`ChatPage.OnDisappearing` 仅在非运行（`!_agent.IsRunning`）时保存——运行中离页由轮末 finally 统一落盘，避免中途保存重置 `_appAddCount` 基线后 finally 误走 `SaveRaw`(基线) 把已冻结的回复丢掉
- **切换后队列无消费者卡死**：移除 `ProcessQueueAsync` 会话守卫 break（切换路径已 `DrainSendQueue` 清队），旧轮 wind-down 期间新输入的排队消息不再永久卡「排队中…」（发送键还显示 ↑ 无法停止）
- **`/clear` 复活**：`OnClearChat` 先 `AwaitActiveRoundEndAsync`（防在跑轮 finally 再写盘）再 `MauiSessions.Delete` 盘上文件 + 清内存状态；空消息下重启不再从 `.json` 复活已清除会话
- **被丢弃排队消息以假 user 轮入库**：`DrainSendQueue` 改为从 `Messages` 直接移除排队占位气泡（而非只改文本留 `Role=User`），不再持久化「❌已停止」伪消息、不再注入 LLM context
- **详情页渲染预算头偏**：`ToolCallsDetailPage` 150k 预算由先到先得改为**按工具均分**（`ShareFor(count)`），流式 `UpdateTool` 与 `Build` 都按各自配额截断，后序/最终工具不再被前面大输出饿成「已省略」

## v0.96.70 (2026-09-08) — MAUI 移动端 code-review 复核三连修（会话切换/详情页健壮性）

承接 v0.96.69 的会话切换/详情页改动，经三轮 code review 复核修复发现问题项：重点是「僵尸轮只挡 finally 没挡流式回调」这一根因，以及会话切换/保存的数据正确性。Android Debug 编译 0 错误。

- **会话切换丢弃排队消息**（`ChatPage.AwaitActiveRoundEndAsync`）：取消在途轮**之前**同步清空 `_sendQueue`（`StopCurrent`→`AwaitActiveRoundEndAsync` 回归）——否则旧会话排队消息被 `ProcessQueueAsync` 续体取走执行并写入新会话；`ProcessQueueAsync` 另按发起会话守卫，队列不再跨会话执行
- **会话 LLM 上下文隔离**（`EnsureContextSeeded` + `_contextSeeded`）：每会话首条消息才把当前会话历史注入 `Agent.Messages`；`NewSessionAsync` 直接 `Agent.Reset()`；旧会话历史不再污染新/其它会话
- **切换屏障门控 + 超时**：`AwaitActiveRoundEndAsync` 改以 `_activeRound`（`done.Task`）门控（原 `IsRunning` 先翻 false 留下残余写窗口）+ 10s 超时兜底（被取消的工具不理会 token 也不永久挂起切换）+ TCS 局部化（`ReferenceEquals` 防旧轮误清接替的新轮）
- **僵尸轮彻底隔离**（`StaleRound()` 局部函数）：统一判定「会话已切走 **或** `_activeRound` 已被新轮接替」，流式回调（`onToken`/`onTool`/`onToolOutput`）与收尾 `finally` 两侧都据此放弃——超时后旧轮残余 token/工具/输出不再写入新会话，也不再清掉接替新轮的 `_cts`/`_activeCts`（修复仅 finally 守卫生效太晚、且切回同会话开新轮时被误清的问题）
- **共享会话回写不丢数据**（`SaveCurrentSession` + `MauiSessions.SaveRaw`/`ToNode`）：以盘载入原始节点为基底合并新消息；改用递增计数 `_appAddCount` + `TakeLast`，对 `PruneMessages` 队首 `RemoveAt(0)` 稳健（条数边界在裁剪下失效会让新消息被丢）；合并后把结果升为新基线（`_sessionRaw=merged`），已保存的旧历史即使被后续裁剪也不回丢；`/clear` 同时重置会话状态 + `Reset()` 清空 LLM 上下文
- **工具组流式新工具渲染**（`ChatMessage.ToolCalls` 改 `ObservableCollection`）：详情页订阅 `CollectionChanged`，流式中追加的工具即时加卡渲染（`List<T>` 不触发变更事件，此前新工具漏渲染）
- **详情页共享预算**：改按「预算 − 其它工具已实际渲染 rune 总量」动态收缩每工具允许额，全页总量恒 ≤150k（原每工具 `UsedBefore` 为 Build 时快照，无共享总量，流式可压超）；流式只增量重绘该工具；`LastRendered` 相同则跳过重渲染；超预算空卡补「（输出过长，已省略详情）」
- **详情页重入/节流**：`OnAppearing` 重入先解除订阅 + 清 Body（防占位叠加/订阅泄漏）；节流回调改全量 reconcile（未变经 `LastRendered` 跳过），防并发窗口内其它工具的 Detail 被丢

## v0.96.69 (2026-09-08) — MAUI 移动端 code-review 修复（会话切换屏障 + 首跑模式 + 详情页健壮性）

对移动端会话/抽屉/详情页做代码审查并修复 8 项确认问题。Android Debug 编译 0 错误。

- **会话切换竞态屏障**（`ChatPage`）：新增 `_activeRound` 完成信号 + `AwaitActiveRoundEndAsync`——`NewSessionAsync`/`SwitchToSessionAsync` 先取消并**等旧轮 finally 彻底结束**（旧 `_currentSessionId` 下 FreezeSeg + 落盘、残余回调跑完）再清空/切 id，杜绝「长回答流式中切会话 → 旧轮残余写入新会话 / 源会话丢尾」
- **首跑工作模式生效**（`AgentService.EnsureAgent`）：建 Agent 后应用全局 `WorkModeManager.CurrentMode`，不再默认 Build 吞掉用户预设的 计划/聊天 模式
- **回调异常不外溢**（`AgentService`）：onToken/onTool/onToolOutput 在 `BeginInvokeOnMainThread` 内 try/catch 记日志，改道主线程后异常不再脱离 agent 控制流、在主线程未捕获崩溃
- **桌面会话互读不串扰**（`MauiSessions.FromNodes`）：只取 user/assistant 正文，跳过 role=tool/system——桌面会话不再显示成假 AI 气泡，回写不破坏共享 schema
- **OnAppearing 性能**：`Messages.Count>0` 提前返回移至 `Exists`（全目录扫描）之前，返回详情页不再整目录重解析
- **富文本节流状态重置**：`FreezeSeg` 段切换时清 `_lastFormattedLen/_lastFormatRecompute`，短段不再继承长段节流（新段开头被压制不渲染）
- **ToolCallsDetailPage 健壮性**：Detail 变更节流实时重绘（打开跟随流式输出）+ 全页 150k 字符渲染预算防多工具巨输出 ANR（按码点截断）；ToolCallsDetailPage/ReasoningDetailPage 等打开期间不再一次快照
- **edge-pan 核对**：审查所称 26dp 边缘热区在当前代码已不存在（v0.96.65 移除），该 finding 不成立

## v0.96.68 (2026-09-07) — 多端命令统一代码审查修复（SlashMatcher 提炼 + 5 项）

承接 v0.96.67 的多端命令统一，经 code review 校对并修复 5 项：命令别名约定、Web /interrupt、并发隔离、匹配逻辑去重、实例缓存。主工程自测 5076 通过，ci 门禁通过。

- **SlashMatcher 提炼**（`WayCoder/SlashMatcher.cs`）：主工程 `SlashCommandRegistry.TryExtractArgs` / GUI `CoreStubs.ExtractArgs` / Web `ExtractWebArgs` 三处相同的命令匹配逻辑合并为单一实现（四项目共编、各自 resolve 本项目 `ISlashCommand`），消除复制漂移。
- **命令别名加前导斜杠**：WebCommands（`/clear` `/diff` `/permissions` `/恢复模型`）与 GuiCommands（`/hist` `/repro` `/connect`）的无斜杠别名补前导 `'/'`，对齐 CLI 约定——原裸别名与输入（以 `/` 开头）永不匹配，导致 Web `/clear` 等落为普通消息的回归。
- **Web `/interrupt` `/stop` 恢复**：重构时移除的命令补回 ack（真实中断副作用仍在 WebChat 路由层）。
- **Web 命令上下文 AsyncLocal 隔离**：`WebCommandContext.Agent/Slot` 由静态可变改 `AsyncLocal`，避免多浏览器并发 `/command` 串槽。
- **Web 命令集静态缓存**：`WebCommands.All()` 由生成器改为一次性构建缓存，免除每 `/command` 请求重建实例。

## v0.96.67 (2026-09-07) — 多端斜杠命令收敛到单一 SlashCommandRegistry（GUI/Web/MAUI 补齐）

此前斜杠命令四端分裂：CLI/TUI 走主工程 `SlashCommandRegistry`，MAUI 复用注册表+端命令覆盖，GUI 是空 stub+独立 switch(11条)，Web 是独立 switch(17条)——同一命令三处各写一份、语义不一（MAUI `/settings` 被桌面同名命令吞掉即由此来）。本轮收敛到「单一命令真源 + 端命令同名覆盖」，消除手写 switch 分叉、统一命名，并按端补齐命令。三端编译 0 错误，主工程自测 5076 通过。

- **统一机制**：`SlashCommandRegistry` 新增公共 `ApplyEndCommands`（与已注册命令同名则覆盖、否则追加），`RegisterAll` 插件循环改用它（DRY）；TUI `ChatScreen` 的 `AddMessage/AddSystemMsg/AddUserMsg/ClearChat` 标 `virtual`，允许端桥 override 收集/重定向，不改命令签名。四端处理 `/xxx` 统一经注册表 `Match`，端命令经注入同名覆盖主工程命令。
- **GUI（原空 stub）**：`CoreStubs` 的 `SlashCommand`（`Matches` 真实化）/`SlashCommandRegistry`（可注入/匹配）由空变为可用；新增 `GuiCommands` 端命令子集（经 `GuiContext.MainWindow` 调主窗口 internal 方法），`TryHandleCommand` 改注册表 `Match`。命令名补齐至与 CLI/Web 一致：`/perm` 语义修正为沙箱边界（原错位为权限）、补 `/permit`（权限）、`/todos`→`/todo`(别名 `/todos`)，新增 `/session` `/stats` `/mcp` `/free` `/free-restore` `/recent` `/diff` `/history` 等数据/界面命令；`/edit` `/config` 映射编辑器/设置窗口。
- **Web**：新增 `WebCommands` 端命令子集 + `WebChatScreen`（收集 `AddSystemMsg` 文本）；`HandleCommand` 保留 `(bool,string)` 签名（自测兼容）但内部改经注册表分发，未命中再兜底主工程命令（多数用 `AddSystemMsg`，经桥可出文本；TUI 界面命令捕获后回退为未处理）。合并 `WebChat.Commands.cs` 原 switch 分叉。前端本地 `/theme` `/settings` `/model` `/provider` 与路由层 `/interrupt` `/review` `/model`（需实例槽位副作用）保留。
- **MAUI**：导航命令从 10 个分散 PageNav + `/open` 收敛为单个 `/topage <page>`（参数映射 home/chat/files/settings/sessions/panel/modelpicker/providers/gitsync/about/editor），减少命令占用、避免与桌面同名冲突。
- **保留/特有**：Web 前端本地命令与 `/interrupt`（需要实例副作用）、MAUI 移动端页、GUI `/slots`。GUI/Web 对 TUI 面板类命令（`/git` `/checkpoint` `/undo` `/versions` 等）提供端化说明引导，完整端面板为后续增量。

## v0.96.66 (2026-09-07) — MAUI 移动端：斜杠命令打开全部界面 + /help 界面导航分组

聊天输入斜杠命令即可直达移动端各界面（此前只能靠按钮/页面导航），并把界面导航命令在 /help 中单独分组置顶。Android Debug 编译 0 错误，模拟器验证 /files /sessions /help 分组。

- **页面导航斜杠命令**（`WayCoder.Maui/Services/MauiCommands.cs`，经 `CoreStubs.PluginRegistry.CollectCommands` 注入 `SlashCommandRegistry`）：Tab 切换 `/home` `/chat` `/files` `/settings`（`//` 绝对路由）；独立页 `/sessions`（会话历史）、`/panel`（侧栏命令）、`/modelpicker`（模型选择）、`/providers`（供应商/模型）、`/gitsync`（代码同步）、`/about`（关于）；主命令 `/open <页面>`（支持全部页面别名，无参列出可用项）
- **命名规避桌面冲突**：桌面既有 `/model` `/session` `/about` 等仍走桌面语义，另起 `/modelpicker` `/sessions` 等不冲突名
- **/help 分组**：`HelpCommand` 把界面导航命令（描述以「打开」开头 / `/open`）从总表抽出，顶部「📱 打开界面」单独成组（手机首屏即见导航命令）；桌面无此类命令，分组为空不影响原布局
- 界面导航命令不进四端共享 `CommandBar.Favorites`（避免污染桌面建议栏）

## v0.96.65 (2026-09-07) — MAUI 移动端：会话历史/侧栏改独立页（弃抽屉浮层，根治布局类 bug）

v0.96.63/64 的左右抽屉浮层在 MAUI Android 上反复出现布局问题（首开过窄、抽屉打开时 CollectionView 内容不渲染导致「聊天空白」——Padding/Margin 让位两种方式均复现，覆盖又盖住左对齐气泡文字）。本轮**弃用抽屉浮层，会话历史与侧栏改为 Shell 独立页**：聊天页始终全宽、内容永不丢失。Android Debug 编译 0 错误，模拟器全链路验证（入口/会话页/侧栏页/模式循环/新建会话）。

- **会话历史独立页 `SessionHistoryPage`**：左上 `≡` Shell push 进入——返回栏 + 会话卡片列表（首句摘要/相对时间/条数，当前会话高亮）+「＋ 新会话」；点选/新建经 `ChatPage.PendingOpenSessionId` 桥接，返回聊天页 `OnAppearing` 消费并 `SwitchToSessionAsync` 切换/新建
- **侧栏命令独立页 `CommandPanelPage`**：右上 `☰` Shell push 进入——模型横幅（点按选模型）+ 命令区（供应商/模型、代码同步、任务管理）+ 模式区值行（工作模式/确认权限/经济模式点按循环即时刷新，读全局 `WorkModeManager.CurrentMode`/`PermissionManager.CurrentMode`/`cfg.EconomyMode`）+ 关于
- **聊天页保持全宽**：移除 DrawerLayer 覆盖层/左右边缘 Pan 热区/抽屉让位逻辑；聊天列表不再被任何浮层覆盖或让位，彻底消除「空白/过窄/首开错」整类问题
- 遗留抽屉相关 UI/方法暂保留为未调用死代码（后续清理，不影响功能）

## v0.96.64 (2026-09-06) — MAUI 移动端：思考独立泡泡（计时）+ 工具组按思考/正文交错 + 抽屉浮层化 + 模式/权限即时切换

承接 v0.96.63 的聊天重构：思考从「AI 气泡顶部入口」升级为**独立思考泡泡**（与工具同构：一行「已思考 N 秒」，点开看全文），工具分组判据扩展为思考/正文任一间断；抽屉改为真正的浮层（收窄 + 淡遮罩 + 置顶），并修复右侧栏「切换不了权限/模式」的显示回退 bug。Android Debug 编译 0 错误，真机验证各交互。

- **思考独立泡泡**：新增 `ChatRole.Thinking` + 思考消息模板（一行小圆角胶囊「已思考 N 秒」，斜体，点开 `ReasoningDetailPage`）；`ChatMessage.ThinkingSeconds` 记录耗时；AI 气泡上的旧「💭 查看思考」内嵌入口移除（思考统一走独立泡泡）
- **思考计时与展示**：`RunOneMessageAsync` 推理改为**每思考块独立泡泡**——首个推理字符惰性建泡（空推理不发泡），标题随思考实时计秒（`思考中 Ns` → 结束 `已思考 N 秒`），推理全文结束时落 `bubble.Reasoning`（取消/异常 `FinishThink` 兜底）；`ReasoningDetailPage` 顶部副标显示「已思考 N 秒」
- **工具组按思考/正文都交错**：分组间断判据从「仅正文说话」扩为「新思考块或正文段任一出现」(`interruptSinceTool`)——工具与思考、与对话严格按时间交错（💭思考 → 工具组 → 正文 → 思考 → 工具组…），同一思考块内连续工具仍合一组不切碎
- **抽屉浮层化**：抽屉宽度改为**每次布局按屏宽重算**（首次 `OnSizeAllocated` 的 width 可能非最终屏宽，一次性锁死会把抽屉顶到上限过宽）→ 屏宽 ~62%（上限 300dp、下限 200dp），真机约 223dp；遮罩 `#99000000`(60%) 减淡为 `#33000000`(20%)；`DrawerLayer ZIndex=10` 确保浮在 CollectionView 之上——抽屉打开时聊天列表在遮罩下清晰可见，不再被盖没/像全屏页
- **模式/权限即时切换修复**：右侧栏「工作模式 / 确认权限」值与顶栏 ModeBar 空 agent 分支原读 `AgentService.GetStatus()`（agent 未创建时返回 null → fallback 成写死「建造/Ask」），循环切换内部状态但 UI 永不更新，观感「切不了」；改为**直接读全局 `WorkModeManager.CurrentMode` / `PermissionManager.CurrentMode`**（`PermName` 文案与 GetStatus 一致），点按后经面板重建即时刷新；经济模式行本就直读 `cfg.EconomyMode` 正常

## v0.96.63 (2026-09-06) — MAUI 移动端：多会话历史 + 左右抽屉 + 工具分组/推理子页 + Android 网络与输入框修复

移动端聊天体验大改：多会话历史（复用桌面 SessionManager 格式，左抽屉管理）、工具调用按正文间断分组、思考过程默认隐藏改子页查看、正文与工具组按时间交错呈现，另修 Android 主线程网络（NetworkOnMainThreadException）与输入框底线。Android Debug 编译 0 错误，真机验证抽屉/交错时序/入口导航。

- **多会话历史（`WayCoder.Maui/Services/MauiSessions.cs`）**：复用桌面 `SessionManager` file-per-session JSON（`Global.Home/.waycoder/sessions/*.json`，slot=-1 全局、桌面可互读）；只存 User/Assistant 正文 RawText（不含思考/工具结果）；当前会话 id 存 Preferences，重启回最后打开的会话；首启自动迁移旧单会话 `maui_session.txt`；每轮结束/退出自动落盘
- **左右抽屉（`ChatPage`）**：左上 `≡` 滑出**会话历史左抽屉**（固定头「＋ 新会话」+ 可滚动会话卡片：首句摘要/相对时间/条数，当前项高亮，点击 `SwitchToSessionAsync` 切换——正在跑先停），右上 `☰` 滑出**侧边栏右抽屉**（模型横幅 + 命令区：供应商/模型、代码同步、任务管理 + 模式区值行：工作模式/确认权限/经济模式点按循环即时刷新）；DrawerLayer 覆盖层 scrim 半透明遮罩点击关闭、左右消息区边缘 Pan 开合、动画防重入 + 同屏单侧（`TranslateToAsync`/`FadeToAsync` 新 API 去 obsolete）
- **工具调用分组 + 详情子页**：`ChatMessage.ToolCalls`（`ToolCallItem` 名称/摘要/输出流式累积）；工具按「正文间断」合并成组，聊天流只留一行 `🔧 工具调用:N 次`，点开 `ToolCallsDetailPage` 逐个工具分区展示（名称/参数摘要/输出详情语法高亮，按 `file_path=` 推语言）
- **思考过程默认隐藏 + 子页**：推理不再污染聊天流——AI 气泡顶部灰字「💭 查看思考」入口，点开 `ReasoningDetailPage` 整页滚动渲染（`«»` 富文本）；思考只累积到 `reasoningSb`、不流式写 UI、最终挂本轮第一正文段
- **正文/工具按时间交错**：AI 正文按工具边界切「段」（每段独立气泡），工具组插在段间 → AI1/工具1/AI2/工具2…；段惰性创建（收到正文 token 才有，无正文不发空气泡），工具到来先 `FreezeSeg` 冻结当前段；每轮独立 `_toolGroup`（防上轮遗留组并首工具）；工具输出/思考不计入正文间断
- **Android 网络修复（NetworkOnMainThreadException）**：`LLM.CreateHttpClient` / `TranscribeAudioTool` / `WebSearchTool` 在 Android 强制纯托管 `SocketsHttpHandler`——默认 handler 由 .NET Android 解析为 Java `HttpURLConnection`（`AndroidMessageHandler`），主线程触碰 Java 网络流抛异常中止对话（实测堆栈终止于 `InputStream.Close`）；桌面默认 `HttpClientHandler` 本质等同无回归；`AgentService.ChatAsync` 改 `Task.Run` 放线程池（agent 工具执行/token 估算等同步工作不卡 UI 主线程）+ 回调 `MainThread.BeginInvokeOnMainThread` 泵回渲染
- **Android 输入框去底线**：`MauiProgram` 给 `EntryHandler` 补 `RemoveUnderline`（原只覆盖 `Editor`）——聊天输入框与各设置单行输入去除 Android 原生底部横线（iOS 无，观感统一）

## v0.96.62 (2026-09-06) — 手写 QR 编解码库（去 ZXing，0 NuGet 依赖）+ 二维码缩小

手写标准 QR（ISO 18004）编码与解码库，移除 ZXing.Net——项目 **0 NuGet 依赖**。自测 5062 全过，编译 0 警告。

- **QrCodec 共享基础**：GF(256) 对数/反对数、EC 块参数/交错布局、8 掩码 pattern、格式信息 BCH(15,5)+`TryDecodeFormatInfo`、版本容量/对齐坐标（encode+decode 共用）
- **QrEncoder**（encode，`Infra/QrCodec.cs`+`QrEncoder.cs`）：版本 1-40 自动、字节模式、RS 纠错、finder/timing/alignment/格式+版本信息布局、8 掩码惩罚择优；`/sync-qr` 去 ZXing
- **QrDecoder**（decode，`Infra/QrDecoder.cs`）：Otsu 二值化 → finder 1:1:3:1:1 定位 + 连通聚类 → 仿射网格采样 → 格式解析 → 位流逆解码 → RS 纠错（Berlekamp-Massey/Chien/Forney）→ 字节模式 UTF-8；`Decode(rgba/RasterImage/PngFile/矩阵)`，供 PNG 与相机帧
- **二维码缩小**：ASCII 半块字符（宽高减半 + 2 模块安静区）；PNG scale 10→5 + 4 模块白边（~205px）
- **可扫验证**：encode 17 样本临时 ZXing 全回读；decode 闭环 encode→绘制→decode 回读（L/M/Q/H、中文/emoji/300B、v1-v13 边界、8 掩码、几何容差、真实 PNG），RS 直接损坏恢复
- 自测 +79（encode 46 + decode 33）：5062 全过，编译 0 警告
- 已知限制（后续增强）：decode 极端透视/反光/90° 旋转；相机 MAUI UI 接入为独立后续

## v0.96.61 (2026-09-06) — GUI/Web 模型栏单当前模型（小模型入口并入弹窗 Tab）

GUI 与 Web 模型栏从「并列大/小两个模型」收敛为**只显示当前生效模型**，与 TUI/MAUI 一致；小模型切换经模型选择弹窗内「大模型|小模型」Tab。自测 4983 全过，GUI + 主 build 0 警告。

- **GUI**：删并列 `SmallModelBtn`；主按钮 `CurrentModelBtn` 只显示当前生效模型（通道前缀 `大/自由/回滚模型:(供应商)model`）；`ModelWindow` 新增「🤖 大模型 | 🔧 小模型」分段 Tab（`_smallMode` 可变 + `SetMode` 预选当前模型，切小经 `ApplySmallModel` / 切大经 `ApplyModelChoice`）
- **Web**：删模型栏 `small-model-btn`/`small-model-label`；主标签单当前模型（回滚/自由/大前缀判定保留）；模型弹窗加 `model-mode` 大/小 Tab（保存/设 key 自动适配 `pendingMode`）；`.model-mode` 样式
- **MAUI**：核验已单当前 + 前缀，无需改——四端（TUI/GUI/Web/MAUI）模型栏统一单当前模型
- 自测 4983 全过，GUI + 主 build 0 警告 0 错误，keypad exit 0

## v0.96.60 (2026-09-06) — 连接状态收敛 connections.json state（config 停用模型字段）

模型/连接状态由 config.json 扁平字段迁至 connections.json 顶层 `state` 单一权威，消除双源不一致；回退与 free 切换不再覆盖主模型锚点；模型栏跨端统一为当前模型 + 通道前缀。自测 4983 全过，主项目 + GUI + MAUI 编译 0 错误。

- **connections.json `state` 权威**：`connect_mode`（big/free/rollback）+ `default/small/free/rollback_connect`（`providerId:modelId`，含 `*_base_url` 自定义网关覆盖）；老文件无 state 自动迁移生成并落盘升级
- **config.json 停用模型字段**：`Model/Provider/BaseUrl/SmallModel/SmallProvider/FallbackChain/freePrev*` Load 不读 Save 不写（保留镜像）；`Config.Instance.*` 经 `SyncToConfig` 从 state 按 connect_mode 解析填充；`FallbackChain` 转发 connections `fallbackChain[]`
- **回退/free 不覆盖主模型锚点**：`SetActiveModel(free)` 只设 free_connect + connect_mode（首入快照 rollback_connect），default_connect 仅用户主动换主时变；回退链运行回退纯内存不落盘
- **绕过点收敛**：`/config set Model` 直写同步命名连接（state↔connections 双向一致）；`Config.FallbackChain` setter 走 `SetFallbackChainFromSpec`（目录模型自动注册 connect）；`--model connect` → `SetDefaultBaseUrl`（写 default_base_url 锚点）；会话内存加载 `SessionModelMirror` 守卫防泄漏持久（显式模型写清标志）
- **模型栏跨端统一**：只显示当前生效模型 + 通道前缀 `大模型/自由模型/回滚模型:(供应商)model`（TUI 状态栏、GUI/Web 大小按钮各带前缀、MAUI、CLI 横幅/StatusLeft）
- **/model reset**：清空 uniform + F1-F10 全部槽位设置回 UseGlobal 默认（等效删 agent_slots.json）
- **修复**：MAUI CoreStubs 补 `OnOpenCommandPalette`（v0.96.59 /menu 拉取后 MAUI 编译断）；补交此前漏 add 的 `ConnectionConfig.State.cs`
- 自测 4983 全过（+47 state/显示/绕过断言），主项目 + GUI + MAUI 编译 0 错误，keypad exit 0

## v0.96.59 (2026-09-05) — 快捷键跨平台适配 + 功能菜单 + /menu

让快捷键在 Win/Linux/Mac 三端可靠，并把命令面板练成"调出大多数界面"的功能菜单，新增 `/menu` 打字直达。自测 4928 全过，主项目编译 0 警告 0 错误。

- **快捷键跨平台适配**（终端生态约束）：
  - **`Ctrl+Z` 优雅暂停**：Unix/Linux/Mac 默认把 `Ctrl+Z` 当 SIGTSTP **挂起整个进程**——注册 `PosixSignal.SIGTSTP` 处理（`ctx.Cancel=true` 取消挂起 + 设 `PauseRequested`），转成真正的优雅暂停；Windows 无此信号仍走 ReadKey
  - **剪贴板**：补文档「复制 `Ctrl+Insert`（`Ctrl+C` 被系统键占用为退出故不做复制）；Mac 无 Insert 用终端原生复制；粘贴 `Ctrl+V` 全平台可用」
  - 使用手册「⚠ 平台差异」扩为完整跨平台表（`Ctrl+C`=信号/退出、`Ctrl+M`·`H`≡回车·退格→`/model` `/help`、`Ctrl+S`=XOFF、`F1-F10` Mac 需 `Fn`）
- **功能菜单**（命令面板扩充）：`Ctrl+Shift+P` 从「7 动作 + 23 斜杠」扩为「⚡ 界面」组（模型/设置/会话/Diff/推理/搜索/帮助/同步二维码/侧栏/供应商 直达，带快捷键）+「🗂 命令」组（~40 条斜杠命令），VS Code / Claude Code 式模糊搜索 + 分组 + 快捷键提示
- **`/menu` 命令**（别名 `/palette`）：打字即调出功能菜单，等价 `Ctrl+Shift+P`；经 `ChatScreen.OnOpenCommandPalette` 回调打开，已注册并收录进菜单自身命令组
- **文档**：使用手册 + README 命令速查补 `/menu`；CLAUDE.md 加「快捷键跨平台铁律」（禁信号/控制码键 + 斜杠兜底）

## v0.96.58 (2026-09-05) — 快捷键统一（一义）+ 去重别名

统一全部快捷键：同一键不再因「聊天主循环 / Agent 运行循环」双焦点而变义，轴向层一键一义，并去掉重复别名键（每个动作只留一个键）。源码、软件提示、说明文档三处对齐，键位表唯一事实源 = `TuiKeybindHelp.Groups`。自测 4928 全过，主项目编译 0 警告 0 错误。

- **快捷键一键一义（轴向层）**：`Ctrl+P`=权限模式循环、`Ctrl+E`=经济模式循环、`Ctrl+Q`=紧急退出、`Ctrl+X`=交换大小模型——全模式一致，不再因弹窗/运行循环改变含义。删除 `ChatScreen.HandleGlobalShortcut` 中冲突的 `Ctrl+E`(编辑器)/`Ctrl+P`(建议条)/`Ctrl+Q`(退出确认) 绑定；编辑器改走 `/edit`，输入建议条改走输入 `/`·`!`·`#`·`@` 前缀自动弹出
- **去重快捷键别名**（同一动作留主键）：模型选择删 `Alt+P`（留 `Ctrl+M`）；打开设置删 `Ctrl+O`（留 `Ctrl+T`）；刷新/重绘删 `F5`（与 `Ctrl+L` 相同）；编辑器查找/替换删 `Ctrl+H`（与 `Ctrl+F` 重复，且 Unix 下是回退键）；编辑器重做删 `Ctrl+Shift+Z`（留 `Ctrl+Y`）；输入区换行文档行删 `Ctrl+Enter`（留 `Shift+Enter`）
- **键位说明对齐代码**：修正 `Ctrl+R`=生成同步二维码、补 `Ctrl+Y`=搜索历史、`Ctrl+Shift+F1/F2`=主题；帮助面板 / 底部快捷键行 / 使用手册 / 模式体系 / ROADMAP 同步；编辑器屏状态栏补全键位提示；`CommandPalette` 对话历史搜索 `Ctrl+R`→`Ctrl+Y`

## v0.96.57 (2026-09-05) — TUI 提示框残留修复 + 全库重复代码提炼收尾

修复「拉取 v0.96.56 后首次自测」暴露的两个 TUI 缺陷，并完成一批安全、行为保持的重复代码提炼（统一度收尾）。自测 4928 全过，主项目编译 0 警告 0 错误。

- **修复：拉取 v0.96.56 后编译失败**——重构提交 63e4644 引用了从未入库的 `SearchableListPicker.WireSearchInput`（作者强推时漏掉新增文件），导致远程 HEAD 编译不过。按重构前 FilePicker 的内联接线补回宿主类：搜索框样式统一 + 导航键转发列表 + Enter 确认 + onExtraKey 优先裁决
- **修复：TUI 提示框开合吞聊天内容**——侧栏可见时输入 `/` 命令，提示框消失后聊天区/侧栏残留（实测更严重：聊天消息被擦成永久空白）。根因：`ShowPromptBar`/`HidePromptBar` 只标脏 ChatList 容器（`MarkDirty`），而 `TuiListView.OnRender` 在容器脏时先整视口擦成空白，子项因 `parentDirty=false` 不重画 → 消息消失。改 `MarkTreeDirty` 标脏整棵子树（与侧栏无关，无侧栏同样复现）
- **全库重复代码提炼**（7 类收敛到单一真源，行为保持/修复）：
  - `CwdContext.Resolve/Root`：cwd 相对路径解析 28+11 处收敛
  - `ToolErrors.Error(op, ex)`：工具错误文案 22+5 处收敛
  - `GitBin.ReadInt32/ReadInt64BE`：Git 大端读取收敛
  - `AnsiHelper.PadRightByWidth`：就地补白收敛
  - `AnsiHelper.CharVisualWidth`：Tab/单字符宽 3 处收敛
  - `AnsiHelper.TruncateByWidth`：3 份手写截断委托归一（修省略号宽 off-by-one：2→1）
  - `AnsiString.Strip`：StripAnsi 3 份实现收敛（TuiTable 修漏剥 OSC）
  - `TuiScrollMath.Clamp`：滚动钳制 5 处收敛

## v0.96.56 (2026-09-05) — TUI 优化专项四批（清理 / 性能 / 结构 / 去重）

- **快速清理**：删 `ChatScreen.Input` 15 个零引用转发方法（内联到唯一调用点，−105 行）；shell 命令清单单源 `ShellCommandHints`（默认栏保持原 4 常用）；`ChatScreen.Dialogs/Input` 5 处模态样板走新增 `RunModalDialogOnScreen`（UI 线程直执 / 后台 `PostToUI`）；注释错位修复
- **性能**：流式 Markdown 重解析**按帧合并**——`AppendContent` 只追加+标脏，渲染帧 `FlushStreamingLayout` 统一重解析+高度重算+滚底，消除「每个流式 token 全量重解析累计正文」；`chat-item.tui` 模板文本 LRU 缓存（64）+ `TuiListItem.ResizeContent` resize 复用模板控件树（免重复文件 IO + XML 解析）
- **结构债**：双布局接线单源 `WireStaticInputHooks` + 快捷键文案 `ShortcutRowText` 单常量（手写版补 Tab 补全、标记版补 Ctrl+Shift+M，漂移统一）；`Tty.Capabilities` 平台判定单点（`SupportsButtonDrag`/`SupportsKittyKeyboard`/`EnableMouseForTerminal`，InputManager/TuiManager 不再散写 `IsAppleTerminal`）；17 个控件标注「仅演示用，发货未实例化」+ 控件使用矩阵
- **Picker 去重**：`SearchableListPicker.WireSearchInput` 收敛搜索 KeyHook（ReasoningPicker/FilePicker 全接入）+ `UxHelper.FinishModal` 单源 5 Picker Finish；SessionPicker/CommandPalette/ModelPicker 自绘高亮/自定义导航/ClassifyKey 边界差异保留（共性/差异表见 commit）
- 自测 4935 全过，keypad 全系列 exit 0，主项目编译 0 警告 0 错误

## v0.96.55 (2026-09-05) — TUI 鼠标点击命中修复 + 提炼简化

- **鼠标点击命中修复**：弹窗内点击错位——窗口内容控件命中坐标沿 Parent 链累加漏掉窗口偏移（TuiWindow 非控件、RootView 不设 Parent），渲染显式传 win.X/Y 而命中用实时链 → 弹窗内点击偏窗口位置（居中窗口上/左差约窗口偏移）。修复：命中基准改用最近渲染绝对坐标（`_lastAbsX/Y`，`HitAbsX/Y` 兼容入口，未渲染回退实时链）——覆盖 `TuiControl.MouseInBounds`/`HitTest`、`TuiButton`、`TuiList`、`TuiListView`、`TuiView.HitTest`。主界面（根在 0,0）不受影响
- **跨终端鼠标启用序列健壮化**：`MouseEnable` 去 `?1015h`（legacy UTF-8 鼠标干扰 SGR，部分终端据此不发 `\x1b[<...` 序列致鼠标无反应）、`?1003h` 全运动 flood 改 `?1002h` 按键运动；`MouseDisable` 对称
- **TuiDialog 窗口模态化**：此前所有 TuiDialog 对话框 Modal=false，叠在已有模态（如 ModelPicker）之上时 TopModal 仍指下层 → 上层对话框鼠标被下层遮罩吞掉
- **已知遗留**：二层确认框（TuiDialog 弹于 ModelPicker 等之上）按钮鼠标仍无响应（HitTest 未路由到内容层），待专项
- **TUI 提炼简化**（行为不变，净 -37 行）：`UxHelper.RunModalDialog<T>` 收敛 7 个 Picker（ModelPicker/SessionPicker/FilePicker/ProviderPicker/ReasoningPicker/CommandPalette）的「result+evt+ShowWindow+RenderWait」样板；`TuiScrollMath.Wheel` 收敛 TuiList/TuiTableList/TuiTreeView/DiffPreview 滚轮 ±3 clamp；`TuiControl.ContainsMouse` 收敛 TuiButton/TuiList/TuiListView 命中 inside 判定（基于渲染缓存命中基准）
- 自测 4935 全过，主项目编译 0 警告 0 错误

## v0.96.54 (2026-09-04) — TUI 统一输入源（泵线程单读者 + 时钟并入）+ 输入框失焦兜底

「卡死→输入失灵」根治：控制台读取全上单泵线程，主循环永不阻塞；界面刷新严格在主线程；动画心跳并入泵线程（纯诊断不渲染）。主项目编译 0 警告 0 错误，`--test ui` 1304 全过（+3 回归）。

- **统一输入源（单读者）**：`InputManager` 把所有 `Console.KeyAvailable`/`ReadKey`/转义解析（SGR 鼠标/bracketed paste/Kitty/xterm）搬上单泵线程 `PumpKeys`（子线程），`ReadInput` 变纯出队——修「双读者竞态：两线程都 KeyAvailable→一个 ReadKey 永久阻塞 = 整机冻结（spinner 停 + 任意按键无效）」；`_pendingKeys` 改 `ConcurrentQueue`（泵写、主循环读）
- **统一时钟**：删除独立 `TuiAnimTicker` 线程，动画心跳经 `Input.SetHeartbeat(HeartbeatTick)` 并入泵线程；`HeartbeatTick` 只做诊断（冻结看门狗/丰富条/CPU 采样/模型兜底同步），**不渲染**——界面刷新严格在主线程（`Render()` 恒持 `_renderLock`，动画 `RenderAllDirect` 在 Render 内）
- **聊天输入框失焦兜底**：`ChatScreen.HandleInputEditing` 顶部 `if (!InputArea.Focused) InputArea.Focused = true`——修「任务后输入框失灵：能 Ctrl+M/全局快捷键却打不了字」（`TuiEditBase.OnKey` 在 `!IsEnabled || !Focused` 时吞掉全部字面键，而 `InputArea.Focused` 只在模态开/关时由 `TuiScreen._savedRootFocus` 保存/恢复，恢复链一断就停失焦）
- **`TuiChatInput`（Plan 模式）输入迁移**：由裸 `Tty.ReadKey()` 改走共享 `TuiManager.Instance.Input.ReadInput()` 出队，补齐唯一的双读者漏网；Ctrl+V 剪贴板兜底保留
- **`UxHelper.RenderWait` 关窗守卫**：窗口已关闭（`win.Screen == null`，ESC/OnClosed 关窗但未置位 evt）即返回——修「`/provider` 执行一次后，下次 `/` 命令消息进列表但命令不执行」：ProviderPicker 未像 ModelPicker 那样 `RegisterShortcut(Esc)` 置位 evt，ESC 关窗后 `RenderWait(timeout=0)` 永久卡住主循环，后续命令永远解不出队
- **首次无 API key 也允许启动**：去掉「API 密钥未设置！」红框+退出（`return 1`），改为温和提示并直接进入软件——经 `/model`（选模型时输入 key）/`/provider`（设Key）/`/model keys set <供应商> <key>` 在软件内补设；本地模型（ollama / baseUrl 含 localhost/127.0.0.1）本就免 key
- **模型栏供应商误报修复**：`AgentSlotConfig.ResolveBaseUrl` 全局场景目录无该「id+网关」精确条目时改用全局配置网关（权威），不再落进 `Find(modelId)`「内置官方优先」——修「模型栏显示 `(DeepSeek)mimo-v2.5`」（DeepSeek 无此模型）：选中自定义网关模型（如 `mimo-v2.5@api.ambient.xyz`）被误报成内置 deepseek，且与实际请求端点不一致
- **providerId 生成去前端网关前缀**：`ModelCatalog.NormalizeProviderId` / `ExtractHost` 生成服务商 id 时剥 `api-` / `api-inference.` / `inference.` / `ai.` / `ai-` / `www.` / `www-` 前端前缀（`ai.deepseek.com`→`deepseek`、`www-openai.com`→`openai`、`api-inference.deepseek.com`→`deepseek`、`inference.siliconflow.com`→`siliconflow`）——同一网关的子域/`www`/`api-inference` 变体归并成同一服务商 id，避免自定义中转被拆成多个 id
- **表格缩行「花屏」修复**：`TuiTableList.OnRender` 写数据行后补齐表格 Height 区域内的剩余空行（用列表底色覆盖）——修「模型对话框『清空全部模型』后花屏」：总行数 < Height 时前帧旧行内容残留在未重绘区域（TuiView 渲染子控件不先清区域），缩行/清空目录即残留
- **侧栏 LSP 区改为活动才显示**：不再一直列静态 `SupportedServers` 服务列表，改读 `LspTool.ActiveSessions`（运行中会话）——无活动显示「(无活动会话)」占位（对齐 Web 面板「无活动会话」），有活动列出命令/状态/根目录；侧栏指纹同步改用 ActiveSessions.Count，随活动变化即时刷新
- **模型栏供应商显示：匹配不到就显示 `(?)`**：新增 `ModelCatalog.ResolveConfidentProvider(modelId, baseUrl)`——只在目录按 id+baseUrl 精确命中、或 baseUrl 可反推出已知服务商时返回；两者皆不中返回 null，模型栏 / 动态栏显示 `(?)model`，不回退 `Config.Provider` 乱猜。修「模型栏显示 `(DeepSeek)mimo-v2.5`」（DeepSeek 无此模型）：自定义网关模型被误报成别的服务商
- **TUI `/help` 修复**：改 CJK 感知对齐（`AnsiString.DisplayWidth`，中文/emoji 双宽），左列用「短名(Name+别名)」、右列描述，按名称排序——修「`/help` 显示混乱」：此前 `{name,-36}` 按字符数填充在含中文/别名时错位，且左列用超长 `Usage` 导致各行长短失控（参考 `--help` 的 DisplayWidth 对齐 + 干净列式）
- **回归测试 +5**：`SelfTest.Chunk4`「模型栏」2 项（自定义网关全局模型 ResolveBaseUrl 用全局网关 / ResolveLargeProvider 回落全局 provider）+ `SelfTest.Chunk8`「输入失焦兜底」3 项 +「RenderWait 守卫」2 项

## v0.96.53 (2026-09-04) — 模型提问永不拦截（沟通≠权限）+ bash shellBlock 全链路修复（code-review 6 项）

修复 YOLO 模式大模型提问被静默自答（确认轴误划归类到沟通），并整体收尾 shellBlock「│ 」竖线 gutter 特性的 6 项 code-review 发现。自测全过，主项目 + GUI 编译 0 警告 0 错误。

- **模型询问用户 = 沟通，永不拦截**：`ask_user_question` 与权限模式彻底解耦——判据改「可交互性」（TUI / Web 交互桥 / 交互式终端 → 弹框；真正无用户可应答 → 明确返回「无法询问，请自行决定」），YOLO/Ask/Auto/SmartAuto 一律不弹权限框、不静默自答。此前 YOLO 下大模型提问被「自动选第一个选项/留空」代替用户做决定；已加 2 条 YOLO 回归测试锁定
- **ShellBlock 持久化到 ChatMsg**：`ChatMsg` 新增 `ShellBlock`，`AddToolProgress` 写入，`AgentSlot.RestoreTo` / `CaptureChatItems` 重放传参——切槽位 / 后台 / 调宽后 bash 竖线 gutter 不丢（此前只存在临时 `TuiListItem.Body`，重放时静默丢失）
- **`!` shell 直通带 gutter**：`!cmd` 输出与 agent bash 工具路径一致（`AddMessage shellBlock:true`）
- **标题截断预留 gutter**：bash 命令头截断 `Width-4`→`Width-6`，长命令不再被右侧 `│ ` 挤掉 2 列
- **shellBlock 保留 diff/语法染色**：合并进单一内容行渲染，bash 跑 `git diff` / `dotnet build` 不再丢红绿背景；bash 原始输出仍不解码 WayCoder「«»」标记（reviewer 特别锁定为正确行为）
- **shellBlock 提前到构造器**：`TuiListItem` 构造参数 `isShellBlock` / `isError` 在 `BuildContent` 前生效，删掉后置二次重解析（修首次解析白费 + 每次 bash 双次解析）
- **渲染循环去重**：shell/plain 合并进 `AddContentLine` 辅助，弃死变量 `defaultFg`、不再重复求值
- **活跃槽位内容同步**：`AppendToLast` 把流式 tool/system 输出同步回 `ChatMsg.Content`（assistant 由 `AppendToken` 预先同步，按角色跳过避免重复累加）——切槽位后完整 bash 块不再只剩进度 label
- **回归测试 +12**：YOLO 下仍弹窗 ×2、shell 块 diff 红绿背景、shell 块不解码「«»」、非 shell 仍解码「«»」等

## v0.96.52 (2026-09-03) — 会话恢复网关 6 项修复（code-review 补丁）

修复 v0.96.51「会话恢复携带网关」的 6 项 code-review 发现，会话恢复/保存的网关一致性逻辑集中到核心。自测 4905 全过（+7 回归），主项目 + GUI 编译 0 警告 0 错误。

- **恢复模型后同步上下文窗口**：`LoadSessionById` 设模型后调 `UpdateContextWindow`——跨窗口模型加载会话后压缩阈值不再按旧模型预算（对齐 ApplyModel/ApplyRuntimeModel），防超窗对话直接 400
- **恢复网关写回 Config 镜像**：加载会话同步 `Config.Model/Provider/BaseUrl`（仅内存不落盘）+ 刷新头部——头部/模型对话框读 Config，防用户信头部确认后 ApplyModel 重新解析官方端点丢弃恢复的网关
- **保存集中化**：新增 `Agent.SaveSession`——GUI/TUI/Web/CLI 六处保存统一走它写 `model/provider/base_url` 元数据；TUI 退出/崩溃/手动 `/session save` 不再用无参调用覆写剥掉 GUI 存的网关（此前四端共享会话文件、TUI 退出即破坏 GUI 的恢复元数据）
- **provider 推导修正**：新增 `ModelCatalog.ResolveProviderForModel`（目录 model+baseUrl 精确匹配 > baseUrl 注册表反查 > 模型默认），替代「保存回退全局 `Config.Provider`」——修复跨槽位最后选择的 provider 误存给本会话
- **恢复核对 key 绑定网关**：`ApiKeyStore.GetBaseUrl(provider)` 优先（key 与其地址一致）——provider 中途换网关后，旧网关 + 新 key → 401；绑定为空才用会话保存网关
- **空 model 遗留会话兼容**：`LoadSession` 空/缺 model 仍返回消息（Model=""，调用方 `IsNullOrEmpty` 跳过模型赋值）——修复旧格式会话被整体判 null 导致对话静默丢失的回归（Program.resume / `/session load` 已加 guard）
- **回归测试 +7**：会话元数据落盘 / 空 model 兼容 / provider 推导（精确匹配、模型默认、无法判定 null）

## v0.96.51 (2026-09-03) — 会话恢复携带网关（provider/base_url，向后兼容）

会话记录新增可选 `provider`/`base_url`；GUI 显式加载会话时按保存时的网关重配 LLM endpoint——修「model id 配错网关」导致发错服务器/鉴权失败。自测 4891 全过，主项目 + GUI 编译 0 警告 0 错误。

- `SessionManager.SaveSession` 追加可选 `providerId`/`baseUrl`，JSON 写 `provider`/`base_url` 字段（旧会话缺字段 → 回退当前网关，向后兼容）
- 新增 `SessionRecord` + `LoadSessionDetailed`（`LoadSession` 签名不变，主项目/自测零改动，仍返回 `(messages, model)` 元组）
- GUI `SaveAllSessions` 存 model+baseUrl+provider（provider 从「模型+网关」推导，未命中回退全局配置）；`LoadSessionById` 恢复时 `Reconfigure(key, baseUrl)` 重配 endpoint
- 自测：4891 全过 / 0 失败（会话 Save/Load 用例含在内）

## v0.96.50 (2026-09-03) — 模型切换网关收敛（对齐 Web：优先 provider 注册表地址）

`ApplyModel` 改经 `ConnectionConfig.ResolveBaseUrl(effProviderId) ?? info.DefaultBaseUrl` 解析网关（与 `WebChat` 一致）——此前 GUI 用模型目录 `DefaultBaseUrl` 覆盖，导致走代理/中转网关的用户选中模型后被静默打回官方端点（密钥对不上 → 鉴权失败/发错服务器）。GUI 编译 0 警告 0 错误。

- **`ApplyModel` 网关优先级**：provider 注册表地址（用户经「供应商→改地址」自定义的代理网关）优先，provider 未注册时回退模型目录默认地址；不再用目录默认地址覆盖自定义网关
- 与 Web 端 `WebChat` 的 `ResolveBaseUrl` 判定统一，两侧行为一致

## v0.96.49 (2026-09-03) — 二次代码审查修复（GUI 状态/路由 bug 7 项）

第二轮代码审查（针对 v0.96.48 增量）挖出的 GUI 运行时/状态 bug，修复 7 项：槽位排队重入、推理标记复位、`«»` 中间格式色号补齐、`/perm` 下拉同步、模型切换头部刷新、转录落槽、经济模式下拉越界。GUI 编译 0 警告 0 错误。

- **槽位排队重入**：`TrySendNextPending` 顶部加 `if (_cts[slot] != null) return;` —— 切回忙碌槽不再重复执行，修「⏳ 排队中」气泡永久孤儿 + 队列顺序错乱
- **推理标记复位**：`SendAsync finally` 里 `_inReasoning[slot] = false;` —— 中途停止未收到 `«/»` 时，下条回复不再误入推理气泡
- **`«»` 中间格式契约补齐**：GUI `MarkupColor` 补共享契约色 `purple`/`gray`/`black`（对齐 `MarkdownRenderer`：purple→35、gray→90）—— 修 `«gray»`/`«black»`/`«purple»` 字面泄漏给用户
- **`/perm` 下拉同步**：改按 `(int)PermissionManager.CurrentMode` 回填（下拉 0..3 与枚举同序）—— 修 `/perm smartauto` 等别名改模式后下拉不更新（旧数组按字面匹配 `smartauto` 会漏）
- **模型切换头部刷新**：`ApplyModel` 的 `UpdateHeader()` 移到 `ApplyModelChoice` 之后 —— 修切模型后顶栏仍显示旧模型
- **转录落错槽**：`HandleUpload` 捕获 `slot = _activeSlot`、await 后不再读全局 —— 修转录秒级耗时期间切槽把结果写进错误会话
- **经济模式下拉越界**：补第 4 项「极致」—— 修 `EconomyMode.Extreme` 时 3 项下拉越界、不显示实际模式

## v0.96.48 (2026-09-03) — GUI 重构 + 配色体系 + 代码审查修复 10 项

GUI 端大改：配色真源收敛到 `App.axaml` ThemeDictionaries（深/浅各一套，切主题经 `RequestedThemeVariant`），代码取色统一走 `GuiColors` 桥；主窗口拆 4 个 partial；设置/供应商/模型窗口转 axaml；新增 `UiKit` 共享控件库。代码审查发现的 10 项 GUI 回归全部修复。GUI 编译 0 警告 0 错误（Debug/Release/发布全过）。

- **配色体系重构**：删除 `Style/ResourcesChinese/Black/White.axaml`，颜色真源集中到 `App.axaml` ThemeDictionaries（Dark/Light 键同名）；新增 `GuiColors.cs`（`TryGetResource` 读当前变体 + 缺失深色兜底 + `Invalidate` 清缓存）；`.cs` 逻辑不再写 `Color.Parse("#…")` 字面量，经 `GuiColors` 统一取色
- **主窗口 partial 拆分**：`MainWindow.cs` 拆出 `MainWindow.Chat.cs`（交互/发送/角色化消息）/`MainWindow.Commands.cs`（斜杠命令/主题/模型/权限）/`MainWindow.Session.cs`（会话）；删旧 `MainWindow.axaml.cs`
- **窗口转 axaml**：SettingsWindow / ProviderWindow / ModelWindow 由 `.cs` 脚手架改为 `.axaml` + code-behind，ProviderWindow 补确认/改名/改地址对话框
- **UiKit 共享控件库**：`UiKit.cs` 统一按钮（`ButtonVariant` 变体取色）+ 通用对话框（确认/输入/单选/多选/三选一/`NewDialog`），消除各窗口重复的 `MakeButton`/`new Window{…}` 样板
- **审查修复 10 项**：
  1. 权限下拉中文标签→`ComboBoxItem.Tag` 英文标识符，`Perm_SelectionChanged` 只认 `ask/auto/smartauto/yolo`（修 `/perm` 与启动把非 Ask 模式静默重置为 Ask）
  2. `NewDialog` 默认 height=0 走 `SizeToContent.Height` 按内容自适应（修确认/输入/三选一对话框被压至近零高）
  3. `PromptPathAsync` 占位符只作灰色提示、文本框留空（修新建/另存为被预填 `src/foo.cs` 静默错存）
  4. `MessageBubble` 移除强制 Stretch、恢复角色对齐 + MaxWidth（用户右对齐，工具/助手 640/560 折行上限）
  5. `UiKit.MakeButton` 资源键 `Panel2Bg`→`Panel2BgBrush`、Ghost 用 `Brushes.Transparent`（修按钮背景回退默认灰/实心）
  6. 新增 `App.ThemeChanged` 事件——编辑器画刷切主题后重解析（修 EditorView 持旧主题快照）
  7. `Theme_Click` 改为全槽气泡重渲染 + 槽位按钮重洗色（修非活跃槽气泡/10 个槽位按钮留旧主题色）
  8. `SyntaxBrushMap.ForFg` TrueColor 绿通道 `(ansi & 0xFF00) >> 8`（修绿/红错位）
  9. 槽位按钮第 10 槽标签改 `F10`（修显示 `F0`）
  10. 新增 `EditorWindow.OpenFor` 去重复用（修点文件累积 N 个独立编辑器窗口）

## v0.96.47 (2026-09-03) — 跨端重复逻辑收敛（批A/B/C：合并到核心，行为不变）

纯内部重构收尾：把散落在四端（CLI/TUI/Web/GUI/MAUI）的重复逻辑收拢到核心类，消除「复刻版」「双扫描」式复制，TuiTreeView 补上 v0.96.46 未完成的基类接入。自测 4898 全过，四端编译 0 警告 0 错误。

- **批A 纯重复收拢**：新增 `Infra/HtmlText.cs`（4 套 StripHtml 归并）；`Global.BackupFile`；原子写 `WriteAllTextAtomic` 再收 3 处；5 处外部工具路径收敛到 `ImportHelper` 常量
- **批B 跨端文案/格式统一**：GUI StatusText → `ModelPicker.StatusText`；Provider「地址被占用」文案 → `ModelCatalog.DuplicateUrlMessage`（九处统一措辞）；Whisper `IsTranscribeError` 共享到 `TranscribeAudioTool`；上下文窗口显示统一到 `Global.FormatContext`（四端 8 处，InvariantCulture 固定小数点）
- **批C ResolveBaseUrl 家族**：Web 与 MAUI 各自「复刻」的内联解析收敛为 `ModelCatalog.ResolveBaseUrl`（注册表默认 > 模型目录默认 > 全局），ModelCli 探测薄包装同步（复用 `BaseUrlOf` 子查询）；槽位（`AgentSlotConfig`）与连接（`ConnectionConfig`）两层不同抽象语义保留
- **批C MAUI 双扫描**：ModelManagerPage 与 SettingsPage 各自的「收集 provider + 3s GET 首页」扫描合并到核心 `ModelCli.ResolveScanTargets` + `ProbeEndpointAsync`（真异步 /models 探测、4s 超时 + 取消、`ConfigureAwait(false)` 供 UI 层），同步 `ProbeEndpoint` 委托异步版；MAUI 扫描从「首页可达」升级为「/models 接口可用」，与 `--model test` 同一判定标准
- **批C Provider CRUD 样板**：`RegisterProviderResult`/`UpdateProviderUrlResult` 返回失败文案（= `DuplicateUrlMessage` 占用者，不含端上前缀），bool 原方法委托结果方法，消除各处「注册失败后反查 `FindProviderByBaseUrl` 拼文案」的九处三行样板
- **批C TuiTreeView 基类接入**：v0.96.46 保留的树控件补接 `TuiListControl`——删 `_scrollOffset`/`PageMove`/内联 MoveUp·Down（约 40 行），`SelectedNode ↔ SelectedIndex` 双向桥接保持公开 API（越界/空表/折叠隐藏 → null/-1），Home/End/Page 走基类骨架，渲染选中判定改行索引；TuiMenu PageUp/Down 裸数学收敛 `TuiScrollMath.PageMove`（分隔线环绕语义在 `MoveSelection` 保留，未改 TuiView 继承结构与 MenuState 状态对象）
- **保留不合并**（抽象层不同或交互形态差异）：槽位/连接层 ResolveBaseUrl、RemoveProvider 各端确认 UI、TuiMenu 的 TuiView 继承线、模型导入的 URL/ID 规范化差异

## v0.96.46 (2026-09-03) — Tui 列表/滚动控件去重（纯函数 + 公共基类，行为不变）

纯 UI 内部重构：收敛 Tui 列表/滚动控件重复的滚动数学与选中/导航样板，无用户可见功能变化。自测 4898 全过，编译 0 警告 0 错误。

- **`TuiScrollMath` 纯函数提取**：新增 `UI/TUI/Base/TuiScrollMath.cs`（`EnsureVisible` 两段式可见钳制 / `Bar` 滚动条滑块几何 / `PageMove` 页滚动），统一 5 个控件（TuiList/TuiTableList/TuiMenu/TuiTreeView/DiffPreview）各自内联的同一公式
- **`TuiListControl` 数据列表基类**：新增 `UI/TUI/Base/TuiListControl.cs`——选中/滚动状态 + `ItemCount`/`VisibleRows`/`IsSelectable` 虚钩子 + `EnsureSelectedVisible` + `MoveUp/Down/Home/End/Page` 键盘导航
  - **TuiList** 完整接入（删重复字段 + 5 导航键收敛，−46 行）
  - **TuiTableList** 接入（组头跳过经 `IsSelectable` 覆写、SelectNext/Prev 收敛到基类；Home/End/Page 因回调时机差异保留子类，−34 行）
- **保留（结构差异大，不强行接入基类）**：TuiTreeView（树节点↔index 映射 + 展开折叠）、TuiMenu（分隔线环绕 + 数字快捷键 + TuiView 继承线）

## v0.96.45 (2026-09-03) — 大文件拆分 + 提取重复代码（纯重构，行为不变）

本版为纯内部重构：拆分超 1300 行的大文件、把散落的重复样板收拢到公共工具类，无用户可见功能变化。自测 4898 全过，三端编译 0 警告 0 错误。

- **大文件 partial 拆分**：
  - `Config/Config.cs`（1388→412 行）拆出 `Config.Schema.cs`（schema 定义表+查询）、`Config.Storage.cs`（config.json/.env 存储）、`Config.Env.cs`（环境变量辅助）
  - `Config/ModelCatalog.cs`（1471→397 行）拆出 `ModelCatalog.BuiltIn.cs`（内置目录）、`ModelCatalog.Query.cs`（查询解析）、`ModelCatalog.Import.cs`（多格式导入）
  - `Config/ModelCli.cs`（1320→366 行）拆出 `ModelCli.Free.cs`/`Keys.cs`/`Test.cs`/`Import.cs`
- **提取重复代码（批1 零语义工具收拢）**：`Global.cs` 新增 `WriteAllTextAtomic`（原子写）/`EnsureDir`/`TodayStamp`/`NowStamp`/`LogStamp`/`CleanupOldFiles`/`EnforceMaxFiles`；`TextEncoding.IsBinaryContent`；`ChatScreen.ConfirmPaste`。替换散落样板：原子写 9 处、建目录 30 处、时间戳 7 处、retention 清理 6 处
- **提取重复代码（批2 统一助手）**：`ProviderDisplayName` 内联 15 处（含删 MAUI 本地辅助）、新增 `BaseUrlOf` 统一 4 处、`NormalizeBaseUrl` 改 public 统一 12 处、`FormatCtx` 复用、`ModelPicker.StatusText(ScanStatus)` 提取
- **提取重复代码（批3）**：`Agent.ApplyRuntimeModel` 提取——CLI/Web 5 端「重配模型三连」收敛
- **GUI 按钮图标/文案微调**：编辑器/设置/新建会话/清空按钮 emoji 化 + Style 主题资源文件

## v0.96.44 (2026-09-02) — GUI 主窗口重排（头部按钮统一主题化 + 布局重构）

- **GUI 主窗口头部重排**：主题/模型/编辑器/设置按钮统一改用主题动态资源着色（`TextBrush`/`BorderBrush`，切主题即时一致），跨行格式化；**隐藏主窗口「模型选择」按钮**（`IsVisible=False`）
- **侧栏/会话区/状态栏/输入区 XAML 重构**：布局标签统一缩进与换行风格
- **`.cs` 清理**：槽位切换/会话保存等 for 循环统一补花括号 + `var` 声明
- GUI 编译 0 警告 0 错误

## v0.96.43 (2026-09-02) — models.dev 导入修复（官方供应商不被内置跳过 + api=None 回退官方端点）

- **models.dev 导入不再按内置 id 跳过**：此前 `ImportOnline` 跳过所有「内置 id 同名」模型——models.dev 的 deepseek 官方模型（`deepseek-v4-pro`/`deepseek-v4-flash`/`deepseek-v4-flash-vision-exp`）全被跳过，官方供应商在导入结果里缺失（只剩网关托管的 deepseek 模型）；openrouter 等网关托管的同名 deepseek 模型也被误跳
  - 现在在线导入数据**刷新内置**（同 id + 同 baseUrl 由 `AddCustomRange`/`All` 合并覆盖为 models.dev 数据，价格/上下文取官方最新）；网关托管同名模型（不同 baseUrl = 不同服务商）正常导入到对应网关
  - 仅跳过**无端点模型**（baseUrl 空 = 无法使用，`RegisterImportProviders` 也不会注册）
- **models.dev `api=None` 回退官方端点**：部分官方供应商在 models.dev 无 `api` 字段（openai/anthropic 等），`ImportModelsDev` 回退内置/已注册供应商的官方端点（anthropic → `api.anthropic.com`），不再以空 baseUrl 导入不可用模型
- **实测**：重新导入 models.dev 后 deepseek 供应商显示 3 个官方模型（`baseUrl=api.deepseek.com`，ctx=1M）；anthropic 14 模型带官方端点；openrouter/nano-gpt 等网关模型正常；归并幂等 0 移动；自测 105 通过

## v0.96.42 (2026-09-02) — 模型归并迁移（修复 0 模型供应商）+ 导入多源/进度 + 模型对话框优化

- **模型归并迁移（修复 0 模型供应商）**：新增 `ModelCatalog.ReconcileModels` —— 把自定义模型从「别名 providerId」按 **base_url 归并到已注册供应商**（如 `bailian` 0→73、`qiniucloud` 0→81、`wafer-ai` 0→3、`fireworks` 0→17）。别名来源 = 多源导入按来源声明名/URL host 推导的 id 与注册表脱节
  - **安全**：备份先行（`.bak` 时间戳）+ 先写目标后删源（防崩溃丢模型）+ 幂等可重跑；**字段归一化**（`wafer.ai`→`wafer-ai`）改写存储字段而非误删；**真别名**仅当已存在 canonical 条目才重复跳过（移除别名源，别名 id 彻底消失）；空别名文件自动删除
  - **导入防复发**：`AddCustom`/`AddCustomRange` 持久化边界 `NormalizeToRegisteredOwner`（按注册表 base_url 归一化 providerId）+ `RegisterImportProviders` 导入自愈——正常导入不再产生 0 模型供应商
  - **命令面**：`/provider reconcile [--dry-run]`（TUI）、`--provider reconcile`（**默认预览**，`apply`/`run` 才执行，防误操作）、Web `POST /provider/reconcile`（dryRun 参数）
  - **实测**：用户数据 0 模型供应商 **35 → 8**（剩余为真正无模型的本地/内置供应商）；归并 5 移动 / 3133 重复合并 / 零数据丢失；~500 个 `.bak` 备份可回滚
- **导入多源统一（所有端）**：Web 在线导入**修复**（`app.js` 重复 onclick handler 覆盖导致只剩 opencode）→ 恢复 **10 源多选框**（本地 9 源原有多选）；TUI `/model import`、`/provider import` 支持 `online [源名...]`（空格/逗号多源）/ `allonline`；CLI `--model import online <源>`；GUI/MAUI 原已多源
- **在线导入进度显示（所有端）**：后端 `ImportOnline` 4 段 onProgress（🔍 拉取→🔄 解析→写入→✅ 完成）——**Web** 经 SSE `import-progress` 实时广播到发起页（`model-scan-status` 实时更新）、**GUI** 经 Dispatcher 实时上屏、**TUI/CLI** 原本已逐步显示——告别「导入看着像卡死」
- **模型对话框**：Web 模型选择框加宽 **880→1000px** + 价格列 **90→140px** + **去【取消】按钮**（标题 × / 点遮罩仍可关）；GUI 模型列表**组内按模型 ID 排序**（对齐 Web/TUI「供应商 ID 分组 + 模型 ID 排序」）
- **自测**：4890 全过（新增「模型库归并」18 项断言：迁移/重复跳过/幂等/无归属/无URL/dry-run/防复发/字段归一化回归）；三端编译 0 警告 0 错误

## v0.96.41 (2026-09-02) — 服务商名称显示五端统一 + Web 模型框加宽/连接切换修复

- **服务商名称显示统一（TUI/GUI/Web/MAUI/CLI 五端）**：新增 `ModelCatalog.ProviderDisplayName`（大小写不敏感，providers.json 注册显示名优先），模型栏/分组头/厂商列统一显示注册显示名（`(AIHubMix)deepseek-v4-flash` 而非 `(aihubmix)…`），`/provider rename` 后各端刷新即时生效
  - **模型归属修复（同 id 多服务商）**：`ResolveBaseUrl`/`ResolveSlotProvider` 按「实际生效模型 + 网关 (id, baseUrl)」精确定位服务商——选 AIHubMix 网关的 `deepseek-v4-flash`/`coding-glm-5.1-free` 不再误显示/误配 DeepSeek 官方；`ResolveBaseUrl` 全局场景返回实时 providers.json 注册表地址（覆盖优先），槽位 connect 场景走 connect 指定网关
  - **ModelPicker 回退链去重键修正**：connect 行去重改用 `ModelKey(providerId, modelId)`，消除回退链模型重复显示
  - **`Find(id, baseUrl)` 尾斜杠规范化**：与 `ResolveBaseUrl` 对齐，带尾斜杠的网关也能正确归属
  - **TUI/GUI/Web 模型栏、启动横幅/状态栏/回退提示全部转显示名**（此前遗漏 `Program.Repl` 直写 StatusLeft、切会话路径等）
- **Web 端**：
  - **模型选择框加宽**：440px→880px（8 列信息不截断），厂商/价格列放大（对标服务商弹窗 880px）
  - **切换连接修复**：`ApplyModel`/`/settings`/`/model/save` 切换模型时 baseUrl 未显式传则用 provider 注册表地址（此前 `Find(modelId)` 内置官方优先会把选 AIHubMix 的模型误切到官方网关——「显示切了、连接没切」）；`/settings` 小模型切换补重配 `agent.LlmClient.SmallModel`
  - **`/model list`、`/free` 表格供应商列**、Key 弹窗/槽位 tooltip/服务商管理弹窗操作文案改显示名（新增 `provNameById` 前端 map）
- **GUI/MAUI 显示统一**：GUI 模型列表厂商列（快照→实时显示名）、Key 弹窗、ProviderWindow 操作文案；MAUI 首页模型标签补服务商、聊天模型栏 `(provider)model`、vision 提示、导入/保存提示
- **CLI 显示统一**：`--model`/`/provider`/`/connect` 当前模型概览、切换确认消息、模型列表分组头、free 扫描括号全部显示名（`ModelCli`/`ProviderCommand`/`ConnectionCommand`/`ModelArgs`/`ConnectionConfig`/`FreeCommand`）
- **自测**：4879 全过（新增显示名大小写/Trim/归属/网关注册表地址断言）；三端编译 0 警告 0 错误

## v0.96.40 (2026-09-01) — config.json 权威源 + TUI `!cmd` 直通/中文乱码/输入框增强 + TUI 卡死修复

- **配置加载重构**：`~/.waycoder/config.json` 成为**唯一权威源**，默认不再从环境变量（含 `.env`）读取配置——消除 shell 里残留 `WAYCODER_MODEL` / `WAYCODER_API_KEY` 等对已配置行为的意外干扰（`Config.CreateInstance` 改为按「config.json 是否存在」分支）
  - **普通启动**：仅加载 config.json + api_keys.json，环境变量（含 `.env` 文件）完全不读取；API Key 只从 api_keys.json 解析
  - **首次启动**（无 config.json）：从 `.env` + 环境变量读取并**导入固化到 config.json**；API Key 经 `ApiKeyStore.ImportFromEnvironment` 导入 api_keys.json（只补空不覆盖，通用 `WAYCODER_API_KEY` 导入当前服务商条目）；已有 `.env` 精简为 5 项引导配置，全新安装不凭空创建
  - 删除 config.json 即回到首次启动状态（可重新导入环境变量）；删除 `MigrateToConfigJsonIfNeeded`，逻辑折叠进首次启动分支
  - 运行时开关（`WAYCODER_WEB_PORT` / `WAYCODER_TRAJECTORY` / 代理变量等）仍读真实 OS 环境变量，不受影响；`GITEE_TOKEN` 等若只存在 `.env` 里则普通启动不再加载（需移到系统环境变量）
- **TUI `!cmd` shell 直通**：输入 `!命令` 直接在操作系统 shell 执行（Windows=`cmd.exe`，macOS/Linux=`/bin/bash`），**输出加到聊天内容持久显示**（tool 角色纯文本防 markdown 误渲染，过长截取最后 500 行 `TailLines`）；裸 `!` 保留弹提示；新增 `BashTool.ExecuteUserShellAsync` —— 跳过 Agent 黑名单（curl/npm/sudo/apt 不再拦），仅保留绝对红线（不可逆系统破坏），红色边框即危险提示（`ProcessUserInput` 由 `== "!"` 改 `StartsWith("!")` 分发，`RunShellOnceAsync` → `RunShellCommandAsync` 支持命令参数）
  - **`!cmd` 后台执行（防 TUI 卡死）**：主循环冻结看门狗曾反复报「PendingSubmissions 冻结 ~3s」= `!cmd` 内联 `await` 子进程阻塞整个 REPL 主循环（慢命令期间屏幕冻结、输入/Ctrl+C 无响应）；改为 `Task.Run` 后台执行 + `PostToUI` 投递结果，先投「⏳ 执行」提示再追加输出——慢命令期间循环保持响应（spinner 仍转、可继续输入/打断）
  - **红线按平台分列**：`DangerousPatterns` / `RedLinePatterns` 同时含 bash 语法（`rm -rf /`、fork 炸弹、`dd`/`mkfs`/`chmod 777 /`）与 **Windows cmd 语法**（`del /s`、`rd /s`、`format c:`、`reg delete`）——两平台命令列表不同，各自拦截，防 `!` 直通在 Windows 上绕过红线
- **`!cmd` 执行后黑屏修复**：退出 TUI 执行命令再 `Enter()` 重进备用屏后 `Render()` 读到残留的 `_needsFullRefresh=false` 走「无脏」路径不重绘 → 黑屏；`TuiManager.Enter()` 现在强制 `_needsFullRefresh=true` + `IsDirty=true`，重进即全刷新重绘（裸 `!` / Plan 模式重进同样受益）
- **TUI 输入框前缀变色**：输入以 `/`、`!`、`@`、`#` 开头的消息时，输入区上下两条横线变色——`!`=红（危险 shell）、`/`=青（斜杠命令）、`@`=品红（提及）、`#`=灰（注释）、其余=默认分隔线色（`ChatScreen.InputBorderColorFor` 纯逻辑 + `SyncInputBorderColor` 每帧同步，.tui 标记版同生效）
- **TUI 输入框自动换行 + 动态高度**：`TuiTextArea.MaxColumnWidth` 设为终端宽——超宽内容自动折行成多逻辑行；输入区高度动态 1~5 视觉行（`inputH = Clamp(Lines.Count, 1, 5)`），超过 5 行向上滚动（`EnsureCursorVisible` 每帧钳制滚动）；新增 `MaxLength` 上限（聊天输入 32K 字符，键入/粘贴/赋值 Rune 安全截断，`TruncateRunes` 防代理对切半）
- **Windows 中文输出乱码修复**：实测 `chcp 65001` 只改控制台代码页、**不改重定向管道字节**——cmd 内建 echo/dir 对管道始终写 OEM 代码页（中文系统 GBK），「强制 UTF-8」不可行；正确做法是 `ProcEncoding.Apply` 按系统 OEM 代码页解码（GBK→Unicode），`!cmd`/agent bash/后台任务/持久 shell/测试输出/剪贴板全部覆盖（TestTool/ClipboardHelper 补上原本缺失的 OEM 解码）；`StripBom` 去 BOM；剪贴板 PowerShell 显式 `[Console]::OutputEncoding=UTF8` 强制 UTF-8（PS 可强制，cmd 不可）
- **启动横幅居中修复**：`ChatScreen.Activate()` 末尾立即 `ComputeLayout+ApplyDynamicSizes`——否则 `RestoreTo` 回放欢迎消息时 `ChatList.Width` 还是 chat.tui 初始布局值（含可见侧栏的 `TW-30`），横幅按错误宽度居中、宽度不足被钳到左对齐，开侧边栏触发重排后才居中；现在启动即正确居中（`.tui` 标记版默认界面）
- **TUI 用户/系统消息去彩色**：用户消息与系统消息正文/角色标签/图标统一固定**亮辉白**（`AnsiColors.BrightWhite`）——告别默认主题的绿用户+黄系统（太花）；助手消息保持青色、工具输出保持暗淡；显式 `«color»` 标记（如 API Key 黄色警告）作为重点提示保留
- **自测**：4860 全过（新增 `!cmd` 中文无乱码端到端断言 + Activate 后 ChatList.Width 就绪断言）

## v0.96.39 (2026-08-31) — 文档更新（apt 仓库上线 + 安装说明）

- **apt 仓库文档更新**：`packaging/apt/RELEASE.md` 重写为 apt 仓库已上线 GitHub Pages（v0.96.36 amd64/arm64，`apt install waycoder` 可用）——旧文档停留在「Gitee Pages 待上线」已过时；`docs/安装与升级.md` apt 段改为可用的安装命令（含二进制 keyring 注意）
- **自测**：4836 全过

## v0.96.38 (2026-08-31) — Tiny 模式工具精简（小窗口模型最小工具集）

- **Tiny 模式工具精简**：`--tiny` 只保留最小工具集（`read_file / write_file / edit_file / bash / glob / grep / ls / tree / todo / ask_user_question`），系统提示词工具定义从 ~10K 精简到 ~3.3K tokens，16K 小窗口可用空间翻倍（`Agent.Tools.cs` 新增 `TinyCoreTools` + `FilterTools` 裁剪）
- **自测**：4836 全过

## v0.96.37 (2026-08-31) — LLM 驱动 /init + system 消息 markdown 渲染

- **LLM 驱动 /init（对标 Crush / Claude Code）**：不再生成带空占位符的静态模板，改为程序化收集代码库上下文（项目检测 / 常用命令 / 仓库地图 / 已有规则 / README / Git 状态）→ 单次 LLM 调用生成真实、非显然、渐进披露的 `AGENT.md`（`/init claude` 生成 CLAUDE.md）；无 LLM 或调用失败自动降级静态模板
  - 新增 `Infra/ProjectInitAnalyzer.cs`（上下文收集 + 提示词模板 + 降级决策，纯逻辑可自测）
  - 内容标准：只写非显然知识（坑 / 隐式约定 / 意外标志）、绝不虚构、命令准确、架构基于仓库地图、合并已有规则、不输出占位符
  - 流式推屏（`RunWithUiLoop` + `PostToUI`）+ 180s 超时兜底；修复 `ProgramContext.LLM` 全局未挂载（顺带激活 /tokens /stats）
- **system 消息 markdown 渲染修复**：`/model list` 等命令输出含 `**bold**` / `` `code` `` 却当纯文本显示——`ChatScreen` 不再强制 system 消息纯文本（tool / banner 保持原样），连续 system/tool 消息合并续接逻辑保留
- **winget manifest 0.96.36 入库**（GitHub release URL 方案，验证全过）
- **自测**：4836 全过（新增 init-llm 16 断言 + system markdown 2 断言）；三端编译 0 警告 0 错误

## v0.96.36 (2026-08-31) — 四端 promptbar（输入框上方常用命令提示栏）

- **四端统一 promptbar**：输入框上方常驻显示精选常用命令（`/help /model /provider /review /reset /tokens /session /perm /mcp /theme`），提示可输入的命令；Web/GUI/MAUI 点击命令直接填入输入框（光标跟末尾）
  - 新增共享 `CommandBar.Favorites`（`UI/Shared/`，TUI/MAUI/GUI 共用）；Web 前端维护同款数组
  - TUI：`ChatScreen` 输入区上方新增常驻提示行（`_promptRow`，`«dim»` 暗淡样式，代码版 + chat.tui 标记版同步，`ComputeLayout` 高度计入）
  - Web：`#composer` 内 input 上方加 `#promptbar` chip 行（点击填入输入框）
  - GUI：`ComposerHost` InputBox 上方加 `PromptBar` WrapPanel chips（点击填入）
  - MAUI：输入栏 Entry 上方加 `PromptBar` chip 行（点击填入）
- **自测**：4824 全过（新增 CommandBar 2 断言 + chat.tui 子节点数更新）；三端编译 0 警告 0 错误

## v0.96.35 (2026-08-31) — 四端补充 /review 代码审查指令

- **新增 `/review` 代码审查命令（四端）**：审查修改过的代码，多维度分析（正确性/安全性/性能/可维护性/测试覆盖），结果流式显示
  - 主工程：新增 `ReviewCommand`（`/review`、别名 `/审查`/`/rv`），复用 `ReviewMode.BuildReviewPrompt`（git diff + 未跟踪文件预览）；作为普通消息投递 Agent 后台执行（`ChatScreen.EnqueueSubmission`，忙时排队），注册到 `SlashCommandRegistry`
  - Web：`/command` 路由层特判 `/review`（生成审查 prompt 投递 `StartSlotTask`）+ `/help` 帮助文本
  - GUI：`TryHandleCommand` 加 `/review`（生成 prompt 走 `SendAsync` 发送）+ `/help` 文本
  - MAUI：`ChatScreen` stub 加 `EnqueueSubmission` 桥接（`OnEnqueueSubmission` → ChatPage 发送）；`ReviewCommand` 条件编译——MAUI 无 git（`GitRunner`/`ReviewMode` 被排除）用修改文件列表的简化审查
- **自测**：4822 全过（新增 `/review` 注册断言）；三端编译 0 警告 0 错误

## v0.96.34 (2026-08-31) — 四端 Provider 管理对话框补齐（Web/GUI 新增）+ 多重价格显示 + TUI ProviderPicker 修复

- **Web 补服务商管理对话框**（此前无独立 provider 管理）：
  - 后端新增 `/provider/*` 路由组（`GET /provider` 列表 / add / rename / url / delete / key / key/remove / test），复用 `ModelCatalog.Providers` CRUD + `ApiKeyStore` + `ModelCli.TestList`
  - 前端模型弹窗加「🗂 服务商」按钮 + `/provider` 斜杠命令打开服务商弹窗：列出供应商（图标/key 状态/模型数/连通态/地址）+ 设Key/清Key/测试/添加/改名/改地址/删除
- **GUI 补服务商管理对话框**（此前只有模型级设/清 key）：新增 `ProviderWindow`（全代码构建，仿 ModelWindow）+ `/provider` 斜杠命令入口；列出全部供应商 + 设Key/清Key/测试连通/添加/改名/改地址/删除
- **多重价格统一显示（四端）**：新增共享 `ModelPrice.Format`（`UI/Shared/`，纯函数）——输入价/输出价 + 闲时价（`$1.2/$4.5` 或 `忙$1.2/$4.5 闲$0.9/$3.0`），免费显示 Free
  - TUI ModelPicker：价格列改用共享格式（此前只显示输入价），对话框加宽（价格列 6→18，MinW 62→74）
  - Web `formatPrice`：补输出价 + 闲时价（此前只显示输入 + 闲时输入）
  - GUI ModelWindow：价格列改用共享格式（此前只显示输入价）
  - MAUI ProviderModelsPage / ModelPickerPage：价格子标题改用共享格式（此前只显示输入/输出，无闲时）
- **TUI ProviderPicker 修复「有按钮无功能」**：8 个按钮补 `shortcut` 快捷键（K/X/T/A/R/U/D/Q，此前无快捷键，表格占焦点时键盘无法触发）；补「Enter 选中行弹操作菜单」（设Key/清Key/改名/改地址/删除）
- **自测**：4821 全过（新增 ModelPrice 6 断言、providerpicker 按钮快捷键 8 断言、SerializeProviders 2 断言）；三端编译 0 警告 0 错误

## v0.96.33 (2026-08-30) — 四端动态状态栏 + 忙时消息排队统一 + Web 模型选中按供应商精确定位

- **四端统一动态状态栏**（TUI/Web/GUI/MAUI）：Agent 运行时状态实时显示 + Braille 等待动画
  - 新增共享状态模型 `WayCoder/UI/Shared/AgentStatusBar.cs`（三端共同编译）：`AgentStatus` 枚举 + `AgentStatusResolver.Resolve` 纯函数解析器 + 统一 10 帧 Braille 字符集
  - 状态覆盖：思考中… / 使用工具中 {工具}… / 压缩上下文中… / 等待确认中… / 等待用户回复中… / 等待子代理完成中… / 任务完成 ✓（瞬态 2.5s）/ 空闲（隐藏）/ 计划模式 🧠 / 错误
  - **Agent 最小信号补充**：新增 `Agent.IsBusy`（思考中判定）；「等待用户 / 等待子代理」自动从 `CurrentToolName`（`ask_user_question` / `agent`）推断，零侵入；`PermissionManager.PendingPermissionTool` 可轮询权限等待
  - **TUI**：修「思考中」死分支（原 `AgentBusy` 从未置 true，思考中实际不可达）；优先级链统一换 `AgentStatusResolver`；补等待用户/子代理/完成三态
  - **MAUI**：私有 `AgentUiState` 换共享 `AgentStatus`；`onTool` 按工具名分派等待用户/子代理；`finally` 置「任务完成 ✓」瞬态回落
  - **Web**：后端新增 `status` SSE 事件（`WebSlot.LastStatus` 去重防刷屏）+ 前端消息区与输入框之间状态栏（空闲隐藏、完成 2.5s 回落）
  - **GUI**：中栏 Grid 插状态行（聊天与输入框之间）+ 100ms 定时器 Braille 动画；补订阅压缩/权限事件（此前未订阅）
- **四端忙时消息排队统一**：Agent 忙时发消息进队列，空闲自动取下一个执行；**忙时消息绝不吞**
  - **Web 修忙时吞消息**（根因）：前端 `send()` 在 `isBusy` 时直接 return，忙时回车消息静默丢失——改为回车忙时照常发送（后端 `PendingInputs` 排队），发送按钮忙时=停止（⏹）
  - **单按钮语义统一**（Web/GUI/手机端 1 个按钮）：空闲=发送，忙时=停止；**回车 / 键盘发送**忙时进队列
  - **GUI**：移除独立 StopButton；排队消息立即上屏+「⏳ 排队中…」标记（`_pendingInputs` 改存消息气泡引用，执行时复用更新「发送中」）；上限常量 `MaxPendingInput`→`MaxPendingSubmissions`；修「槽位切换放回队尾无触发点」卡死（`SwitchSlot` 触发消费）
  - **MAUI**：移除独立 StopBtn；SendBtn 忙时=停止；Editor 换 Entry（`ReturnType=Send` + `Completed`）实现虚拟键盘「发送」忙时排队
  - 排队消息立即上屏+「⏳ 排队中…」，轮到执行改「📤 发送中…」，队列满丢最旧标「❌ 已丢弃」
- **Web 模型选择按 (providerId, modelId) 精确定位**：同 id 跨供应商（minimax 多网关）不误勾
  - 大/小勾、选中高亮、`confirmModel`/设 key/清 key/保存模型全部改 `findModel(id, providerId)` 精确查找（原 `modelMap[selectedModelId]` 同 id 取最后一条会应用错供应商）
  - `saveKey` 暂选应用补传 `providerId`（原只发裸 modelId 会选错网关）
- **Web 输入栏调整**：加宽至 960px 一行放下；去图标 + 精简文案（大:/小:/省钱/权限/模式）；权限选项中文化（询问/自动/智能/畅通）；工作模式下拉（建造/计划/聊天）；模型栏显示 `(服务商)模型`（同 id 跨供应商一眼分清）；槽位悬停 title 带服务商
- **Web 补工作模式切换**：新增 `POST /mode`（改绑定槽位 `Agent.WorkMode` + 同步全局镜像）；`SerializeState` 槽位项输出 `workMode`/`providerId`
- **移动端 ProviderModelsPage 选中勾按供应商精确匹配**：`Marker` 加当前大小模型实际供应商判断（同 id 跨供应商不误勾）
- **API Key 与 baseUrl 绑定**：`ApiKeyStore.KeyEntry` 增加 `BaseUrl` 字段——key 与其调用地址绑定，同名供应商/网关间 key 不混用（Web 请求取 key 绑定的 baseUrl 优先）
- **ModelCatalog.ClearAll 一并清空 providers.json**（`ClearProviders`）：清空后重新导入时供应商注册表不留旧数据干扰
- **自测**：4805 全过（新增 13 个 `AgentStatusResolver` 纯函数断言：状态映射/优先级/StatusKey/帧集）；三端编译 0 警告 0 错误

## v0.96.32 (2026-08-30) — iOS 版首次跑通模拟器 + 手机端模型列表编辑模式

- **iOS 版首次编译 + 模拟器运行验证**：Xcode 26.6 + iOS 26.5 模拟器（iPhone 17 Pro），`dotnet build -f net10.0-ios -t:Run` 构建部署成功、app 正常启动渲染（WayCoder 双平台 Android/iOS 均验证通过）。编辑器 icon_* 图标资源缺失为非致命 warning（`IIOImageSource` 找不到 `Library/icon_*`，待后续补资源）
- **手机端模型列表页编辑模式**（`ProviderModelsPage`）：
  - 顶部新增**模式按钮**（显示当前模式），点击弹菜单「编辑模式 / 选择模式」一键切换
  - **选择模式**（默认）：点模型直接设为当前大/小模型（原行为，右上角大/小切换保留）
  - **编辑模式**：点模型弹编辑菜单——「设为当前大模型 / 设为当前小模型 / 改名 / 删除 / 改地址」
    - **改名**：保留原模型全部属性（价格/上下文/地址等）只改显示名（`AddCustom` 同 key 覆盖 + `MergeModel`）
    - **删除**：`RemoveCustom`（内置模型不可删，删除失败提示）
    - **改地址**：`UpdateProviderUrl` 改供应商默认 base_url（作用于该供应商全部模型）
  - 模式按钮高亮区分（编辑紫 / 选择蓝），提示文案随模式切换
- **API Key 合法性校验**（全端统一）：
  - 新增 `ApiKeyStore.IsValidApiKey`：只允许**英文字母数字 + `+-_.` 逗号**；`$`（Unix 环境变量）、`%`（Windows 环境变量）均判非法——**环境变量引用（`$VAR` / `${VAR}` / `%VAR%`）不是真实 Key**
  - `ApiKeyStore.Set` 拒绝环境变量引用存储——**修复导入误判**（SettingsPage 从 Claude Code/OpenCode 配置文件导入时把 `$VAR` 当真实 Key 存入，已改为跳过并提示）
  - 手动输入点全加即时校验：手机「设Key」、设置页大/小模型 Key 输入框与保存、`--model key` CLI、Web `/key`——非法字符/环境变量引用拒绝并提示合法字符集
- **导入供应商 ID 规范化**：新增 `ModelCatalog.NormalizeProviderId`——去掉 `api-` 前缀和 `-ai`/`-com` 后缀（`api-openai-ai.com` → `openai`），保证名称/图标/去重判断正确。应用到 `ResolveProviderId`（在线/多源导入统一解析）、手机手动导入供应商、配置文件导入；影响名称判断的 host 派生 id 同步修正（`api.neuralreseller.com` → `neuralreseller`）
- **编译警告清零（三端 0 警告）**：
  - 主工程：`PackFile` CS0675 位运算符号扩展（`| (uint)(c & 0x7F)`）
  - GUI：9 个 CS8602 空引用——Avalonia `TextBlock.Inlines` 属性 nullable，`tb.Inlines!.Add` 统一断言
  - MAUI：真实修复 3 处——`TappedEventArgs?`（null 传入）、`PickPhotoAsync` → `PickPhotosAsync().FirstOrDefault()`（CS0618 过时）、`custom.DefaultBaseUrl!`（CS8620 nullable 元组）；其余 128 个为共享桌面源码的注释/平台噪音（CS1570/CS1587 XML 注释格式、CA1416/CA1422 Android Console API、CA2255 ModuleInitializer）→ MAUI csproj `NoWarn` 消除（主工程/GUI 不报，仅 MAUI 编译共享代码时解析）
- **自测**：`dotnet build WayCoder.Maui -f net10.0-android` 0 错误；Android 模拟器部署运行验证（编辑模式按钮→弹菜单→编辑菜单实测通过）；4793 全过（新增供应商 ID 规范化断言）

## v0.96.31 (2026-08-30) — 全系统隐患审查修复（3 个并行代理扫描 + P0 崩溃 + 20 处隐患）

- **审查方式**：3 个并行审查代理分别扫描「并发/稳定性」「资源/无限增长遗漏」「正确性/安全」，逐端核查出 40+ 项发现，去重后按严重度 P0-P3 修复
- **P0 崩溃（v0.96.30 引入）**：`ChatPage.AddMessage` 无限自递归 → StackOverflow——`replace_all` 替换 `Messages.Add(` 时把方法体自己的 `Messages.Add(m)` 也替换成了 `AddMessage(m)`，任何消息添加即崩（MAUI 聊天全挂）。已改回 + 验证无其他误伤
- **P1 数据丢失/内存爆/卡死**：
  - `AudioRecorder` 按路径 ordinal 排序删录音 → 跨位数边界/Android 重启后把**刚录完的新文件**当最旧删除（转录必失败）→ 改按文件写入时间排序
  - `WebChat.WriteClient` 同步 `Wait(5s)` 卡 Agent 线程——浏览器后台标签页暂停读 SSE 时每次 token 卡 5s/客户端、多慢客户端串行「死机」→ 改 fire-and-forget 异步写（`WriteGate` SemaphoreSlim 串行 + 超时剔除），HandleSseAsync 初始回放/释放同步改用 WriteGate
  - `GrepTool` 结果上限核验：硬编码 200 实为**总匹配上限**（非误报），提为 `Global.MaxGrepResultLines` 常量
  - `ModelCatalog` 批量护栏按「导入条数」误算净新增（9990 条导入 20 条纯更新被误拒）→ 改按净新增 key 计；单条 `AddCustom` 绕过护栏 → 补上限检查
  - MAUI `_sendQueue` 丢最旧后 UI 残留「排队中…」永不纠正 → 被丢消息改标「已丢弃」
  - `MauiSessionStore` 2MB 截尾切半长度前缀流 → 恢复乱码 → 改按**完整消息**从最旧丢弃
- **P2 并发/一致性（10 项）**：
  - `WebChat` 换模型与排队重启竞态：新增 `WebSlot.Quiescing` 暂停标志——先置位+清空排队，再 Interrupt/Reconfigure，`TryStartNextPending` 见暂停跳过启动，消除 Reconfigure 与在途 ChatAsync 并发
  - `TuiManager` 心跳：`_lastRenderTicks` 打点移入 `_renderLock` 内 + 心跳直写前 `TryEnter`，杜绝长渲染中途按旧坐标写屏
  - `FreezeCapture` 同步落盘阻塞心跳（冻结时唯一活信号被慢盘拖住）→ 落盘/清理改异步 Task.Run
  - `TodoTool` 无锁读改写（GUI 2s 定时器 + 多槽位 Agent 并发）→ `_fileLock` 串行 + 原子写（.tmp + move）
  - `ScreenshotTool` 默认截图目录无限累积 → `Global.MaxScreenshotsKeep`(20) 删最旧
  - Web `/upload` 图片临时文件消费后不删（音频转录即删、图片漏了）→ `CleanupOldUploads` 每次上传清 10 分钟前 upload-*
  - GUI `AppendToolOutput` 只留头 2000 丢尾部错误 → 保头保尾（对齐 TUI Snip）
  - GUI 队列满丢弃静默吞消息 → 补「⚠️ 排队已满」提示（MAUI 已在 P1 补）
  - MAUI `PruneMessages` 每次 Add 全量 rune 估算卡 UI → token 裁剪降频到每 8 次
  - `SubAgentAudit` 滚动文件无保留上限 → 滚动时清 N 天前 subagents_*.log
- **P3 低危（10 项）**：
  - Rune 截断防切半代理对：`Syntax.cs` `[..4000]` → `TruncateByRunes`、`GetCommonPrefix` 停在代理对中间回退码元边界、app.js `slice(-N)` → `tailCodePoints` 按完整码元取尾
  - Web 满员回滚误删合法绑定（ResolveSlot 尚未调用却 Remove 旧绑定）→ 删除回滚；`BindClientSlot` 补 `MaxClientSlotEntries` 上限
  - Web 前端 `#messages` DOM 只增不减 → `pruneMessagesDom` 丢最旧（300 条）
  - `LLM._reasoningBuffer` 单响应完整累积 → 超显示上限 2 倍才滚动保留尾部（正常思考统计保留）
  - GUI 槽位切换时 A 槽排队消息污染 B 槽输入框 → `TrySendNextPending` 非活跃槽放回队尾
  - MAUI「停止」只停当前轮、排队继续跑 → 停止时清空队列标「已停止」
  - `ErrorLog` 单日滚动文件无数量上限 → `MaxRolledLogFiles`(10) 超限删最旧
  - `/fileref` 前端丢后端 error 细节 → 优先展示 `res.error`
- **已核查排除（非隐患）**：GrepTool 200 实为总匹配上限、CpuMonitor 锁安全、WebChat 锁序无死锁、AgentSlot.Sync 无锁环、渲染原子性、全部 Global 常量真生效、AsyncLocal 无泄漏、XSS/路径穿越防线
- **自测**：4792 全过；`dotnet build -c Release` / `WayCoder.Gui` / `WayCoder.Maui -f net10.0-android` 三端 0 错误

## v0.96.30 (2026-08-30) — 所有端防无限增长补齐（Web/GUI/MAUI）+ MAUI 编译断链修复

- **逐端核查结论**：此前 v0.96.28/29 的 21+ 个 Global 上限中，12 个核心限制（上下文 >300 条硬门、Bash 流式、后台任务、文件集合、日志/轨迹/会话/版本保留等）对 CLI/TUI/Web/GUI/MAUI **全部共享生效**；但 6 个 TUI 专属限制的同型结构在 Web/GUI/MAUI **完全裸奔**。本次按「复用 Global 常量 + 平移 TUI 已验证的 `CapMessageContent`（保留尾部窗口+标记）与 `PruneChatHistory`（按条数/token 丢最旧）模式」逐端补齐
- **新增 Global 编译期常量（7 个）**：`MaxPendingInput`(100，Web/GUI 输入队列)、`MaxClientSlotEntries`(64，Web 非 SSE 客户端槽位绑定)、`MaxQueuedImages`(8，LLM 待注入图片/agentId)、`MaxTodos`(500)、`MaxImportedModels`(10000，模型库导入护栏)、`MaxMauiSessionBytes`(2MB)、`MaxAudioRecordings`(20)
- **Web 端**：`WebSlot.PendingInputs` 入队判满丢最旧（`MaxPendingInput=100` 定义了从未真正生效，脚本持续 POST /chat 可灌满）；`ResolveSlot` 绑定上限 64——只 POST /chat 不建 SSE 连接的 clientId 原本永久占用字典+槽位（10 槽会被占光）；浏览器 `app.js` 单条消息 `textContent +=` 加 50k 尾部窗口截断（`appendCapped`，对齐 TUI `CapMessageContent`）
- **GUI 端**：`AppendToken` 的 `StringBuilder.Append` 加 50k 截断；`_messages[slot]` 只 Add 不裁剪（与 Agent 压缩脱钩，千轮会话内存线性涨 + 渲染 O(n²)）→ 按 `MaxChatMessages` 丢最旧并同步移除气泡；`_pendingInputs` 加 100 上限；`EditFileTool.ChangedFileStats`（ConcurrentDictionary 只写不删）随 `ChangedFiles` 超限淘汰同步清空（共享，所有端生效）
- **MAUI 端**：`Messages`（ObservableCollection）只 Clear 不裁剪 → 封装统一 `AddMessage` + `PruneMessages`（按 `MaxChatMessages` 条数 + `ContextManager.EstimateText` 累计 token 双裁剪）；`contentSb`/`reasoningSb` 流式追加加 50k 截断；`ToolDetail +=` 加 50k 上限停止追加；`_sendQueue` 加 50 上限；`MauiSessionStore` 只存最近 N 条 + 文件超 2MB 截尾重写；`AudioRecorder` 录音完成后清理最旧 `rec-*.m4a`（保留 20，防 workspace 磁盘无限涨）
- **共享护栏**：`LLM.QueueImage` 每 agentId 上限 8 张（连续加图不发消息累积 base64 内存）；`TodoTool.Create` 满 500 拒绝；`ModelCatalog.AddCustomRange` 总库超 10000 拒绝该批（一次 models.dev 导入 7000+ 无护栏）
- **修复**：`WayCoder/demo/`（未跟踪演示目录）的 `obj/**` 被 GUI/MAUI 的 `../WayCoder/**/*.cs` glob 误包含导致 AssemblyInfo 重复 → 两 csproj Exclude `demo/**`；MAUI 编译断链（v0.96.28/29 遗留，`DiagCommand` 引用 `TuiManager.UiLoopActivity`、`ConnectionCommand` 引用 `ChatScreen.RefreshModelStatus` 在 MAUI stub 缺失）→ `CoreStubs` 补两个 no-op 桩
- **自测**：4791 全过；`dotnet build -c Release` / `WayCoder.Gui` / `WayCoder.Maui -f net10.0-android` 三端均 0 错误

## v0.96.29 (2026-08-30) — 思考内容滚动显示 + 模型显示统一同步

- **思考内容滚动显示（保留最后 100 行）**：单条消息截断从「保留头尾」改为「保留尾部窗口 + 滚动标记」——超 `MaxSingleMessageChars` 后每次追加滚动更新，旧内容被挤出、**始终显示最新内容**（思考/工具输出滚动可见），而非只显示最先的 100 行；涉及 `ChatScreen.CapMessageContent`、`TuiListItem.AppendContent`、`AgentSlot.CapSingleMessage`。LLM 层 `MaxReasoningDisplayChars` 提到 50k（与单条上限一致），让 reasoning 完整流式到显示层由尾部窗口接管；截断提示改「思考内容过长，显示窗口受限」
- **模型显示统一同步（active Connect 唯一事实源）**：
  - 动态栏 `StatusLeft` 从裸 model 改为 `(provider)model`（`ConnectionConfig.FormatModel`），与模型栏格式统一——启动/回退/Ctrl+M/会话切换各处对齐
  - 新增 `ChatScreen.RefreshModelStatus()` 统一刷新入口；修复 `/connect`（ApplySpec）、`/connect use`（UseConnection）、`Ctrl+Shift+M`（CycleConnect）切换后**漏刷新动态栏**（此前只 `AddSystemMsg` 标脏 → 显示旧模型）的问题
  - 心跳线程 5s 兜底同步：比较 active connect 快照（Provider|Model|SmallProvider|SmallModel），变了才刷新（防每 5s 全屏闪烁），即使某切换路径漏调用也能自动纠正
- **自测**：4791 全过；`dotnet build -c Release` 0 错误

## v0.96.28 (2026-08-30) — 死机现场自动采集 + CPU/动态栏实时监控 + 全部无限增长点限制

- **死机现场自动采集（`--debug-dump` 开关）**：新增 `FreezeCapture` 三合一自诊断——① 阶段环形缓冲（黑匣子）：主循环每阶段转换打点入环（~100 条/s，4096 容量 ≈ 40s 历史），看门狗每秒记一条 Agent/上下文「丰富条」；② 每分钟定时 dump：心跳线程把黑匣子 + 当前状态同步写入 `logs/freeze_*.txt`（用户需求：死机前最近一次快照即现场）；③ 冻结时同步强制 dump：看门狗检测主循环冻结 >3s → 同步落盘完整现场（黑匣子尾部 + 槽位/Agent/token/上下文 + macOS `sample` 抓 native 栈异步追加）。`File.WriteAllText` 同步强制落盘（不依赖 ErrorLog 5s 缓冲，进程被强杀不丢）；`Monitor.TryEnter` 防主线程持锁卡死采集线程；保留最新 20 个 dump 自动清理。**默认关闭**，`waycoder --debug-dump` 显式开启（排查死机时用，平时零开销）
- **CPU 占用检测**：新增 `CpuMonitor`（缓存 `Process.GetCurrentProcess()` + `TotalProcessorTime` 差值算占用%，AOT 安全跨平台），心跳线程每 5s 采样；动态栏右侧显示 `⚡CPU%`（绿/黄/红三色）；超 70% 且开启 `--debug-dump` 时输出资源占用 dump；CPU 值进 FreezeCapture LiveState 供冻结现场分析
- **动态栏实时监控扩展**：右侧常驻显示「上下文占比 `📊` + CPU `⚡` + token 消耗 `🔤大X 小Y` + 花费 `¥`」，每帧从 `Agent.LlmClient`/`ContextManager` 读实时值（getter-only 廉价计算，流式/工具执行时 token 数实时变化）；上下文占比改用真实 `LastPromptTokens/MaxTokens`（比估算值准）；右段宽度保护（窄终端丢最靠后的信息）
- **大模型思考内容显示截断**：reasoning（`reasoning_content`/`thinking_delta`）超 `MaxReasoningDisplayChars`（4000 字符 ≈ 100 行）停止显示并提示「思考内容过长已截断」，防止无限制思考把聊天列表撑爆；`_reasoningBuffer` 仍完整累积（保 ReasoningTokens 统计/调试日志）
- **TUI 光标错位修复**：心跳线程 `RenderAllDirect()` 直写 spinner 用 `CursorPos` 移动光标但漏恢复——主循环被堵 >150ms 时心跳接管直写后光标停在任意 spinner 位置「到处乱跑」；补 `ActiveScreen?.EmitCursor()` 恢复到输入框（与主渲染循环一致）
- **全部无限增长点限制（集中到 `Global` 编译期常量，用户统一调整）**：全面排查并修复 16+ 处字符串/列表/磁盘无限增长——
  - **内存/显示层**：BashTool 流式 `outBuilder` 执行期间增量截断（原结束才截断，长命令执行期撑爆）；后台任务 `BgTask.AppendOutput` 滚动保留头尾；单条流式消息 `Content += delta` 加 `MaxSingleMessageChars` 截断+标记（ChatScreen/TuiListItem/AgentSlot）；AgentSlot 非活跃槽位缓冲加条数裁剪；`TuiScreen._uiQueue` 超 `MaxUiQueue` 丢最旧；`PendingSubmissions`/跨槽位 `PendingMessages`/Watch 提示队列加排队上限；`ChangedFiles`/`_allSessionFiles`/`_modifiedFiles` 文件集合超限清空；Agent 消息条数硬门（>300 强制压缩）
  - **磁盘层**：Trajectory 旧轨迹文件保留 100 个/删 30 天；ErrorLog 单文件 10MB 滚动 + 启动清理 30 天前；DebugLog 会话日志保留 7 天；SubAgentAudit 单文件 10MB 滚动；SessionManager 保留 200 个会话/删 30 天；FileVersionStore 补上 `MaxTotal` 全局总量淘汰（原只每文件限制）
- **`--tui -p` 提示词投递槽位 0 走全屏 REPL**：`--tui` 显式且带 `-p` 时提示词加入 `_pendingSlotQueues[0]` 强制走 `RunReplAsync`（复用槽位自动投递机制），而非 RunOnceAsync 一次性 spinner 模式——便于观察任务完成阶段的 TUI 渲染/死机；`--json` 组合保持 RunOnceJsonAsync
- **自测**：4791 全过（新增 6 项：无限增长限制、CPU 检测、单条消息截断等）；`dotnet build -c Release` 0 错误

## v0.96.27 (2026-08-29) — TUI 窗口树重构：子窗口永远在父之上 + 关窗刷新替代快照回填

- **窗口树结构**：`TuiWindow` 新增 `Children`/`Parent`/`Screen`/同级 ZOrder 计数，`TuiScreen.Windows` 收窄为根窗口列表——子窗口挂在父窗口下，永远渲染在父之上；一个窗口可弹多个子窗口，Z-order 仅在同级子窗口之间有意义；`TuiWindow.AddChildWindow` 显式弹子窗
- **只能弹子窗口（自动父级）**：模态窗口在场时，新对话框自动成为当前顶层模态的子窗口（回调里 `ShowWindow` 自动挂父，生产调用点零改动）；`ShowToast`/UxHelper 通知恒为根窗口（不被模态关闭递归带走）；`AddRootWindow` 供「替换型对话框」显式根挂——修复 EditorScreen 文件选择器「输入路径」被父选择框关闭递归误杀的隐患
- **先序渲染 + 粘性 forceFull（层级刷新规则）**：渲染改先序 DFS（父先画、子后画），子窗口从结构上永远盖在父窗口之上，「父窗口整窗重绘把子对话框刷没」根除；任一窗口整窗重绘后，其子窗口与后续更高兄弟强制整窗重绘（递归传播，否则增量脏控件补画救不回边框/背景/内容）
- **关父递归关子**：`CloseWindow` 先递归关闭全部子窗口再关自身，焦点回退到渲染序末位存活窗口（自然满足「回父」/「回同级次高层」）；`CloseAllModals` 循环关栈顶递归清子树
- **去掉关窗快照回填**：`BackgroundSnapshot`/`CaptureBackground`/`_pendingSnapshots` 机制废弃（回填老是花屏），关窗一律脏区清除 + 背景控件重绘，有残留窗口/关闭带子树时全量重绘兜底；`FrameSnapshot` 类保留（WayCoder.Preview WPF 渲染器共享源码依赖），仅废弃 TUI 内关窗回填用法
- **输入/鼠标/焦点树化**：Esc 关最深模态（`TopModal` 树下降取最深层模态）、Tab/方向键走焦点叶子窗口、鼠标非模态按「最上优先」反向渲染序命中、`SetCursorOwner` 空窗判定改全树计数、OnCreate/OnDestroy/OnResize 递归子树 + 逐层钳制回内容区、焦点转移补标脏边框色
- **工具催促节制**：`LooksLikeTask` 判定「没任务不催」——纯聊天/提问（问号/疑问词/问候）不再被误判为停滞而注入「请立即调用 write_file/bash」；真任务也等**思考 10 轮以上**才渐进催促（未到阈值只注入「继续」，不打断长链思考）
- **任务完成死机防御（调查中，看门狗待复现确认）**：① 窗口树防环——`AddWindow` 挂载前摘除旧位置（重复 ShowWindow 变移动窗口），渲染/键路由/关闭的树遍历加深度守卫 `MaxTreeDepth=64`（树环从栈溢出死机变为可记录异常）；② 子进程 stdin 重定向补漏——`ContextBridge.RunGit`/`DesktopNotifier`/`UpdateChecker`/`TuiChatInput` 均不共享主控台 stdin（防子进程抢读控制台 → 主循环 `KeyAvailable→ReadKey` 竞态永久阻塞）；③ 完成时 `EstimateTokens(SnapshotMessages)` 移出 UI 线程（任务上下文大时不再卡死 UI）；④ **主循环冻结看门狗**——主循环每阶段记 `UiLoopActivity`，心跳线程发现停滞 >3s 记「主循环冻结 Xms，最后活动: Y」，死机复现后直接定位卡在哪个阶段
- **子进程输出编码修复**：`ProcEncoding` 统一把 `cmd.exe` 输出按系统 OEM 代码页（中文系统 GBK）解码——`Process.StandardOutput/ErrorEncoding` 默认跟 UTF-8 导致 GBK 字节误解码成乱码；接入 BashTool/BackgroundTask/PersistentShell/SandboxManager
- **手机端清理**：删除 MAUI 调试/测试代码——`autotest.flag` 自动写文件自测钩子、`nativemem.flag` 1GB 原生内存读写实验、`#if DEBUG` 日志；保留设置页「测试 API Key」用户功能（配置连通性检测）
- **自测**：4785 全过（新增层级刷新/同级子窗口 Z/关父递归关子/多级弹窗树断言/任务判定，适配键位作用域；移除关窗快照断言与 `[FrameSnapshot]` 回填引用）；Maui Android 构建 0 警告 0 错误

## v0.96.26 (2026-08-29) — 符号反向索引 + 手搓 SQL 引擎 + 编码转换 + git pull/push

- **全局符号反向索引**：`RepoMapGenerator` 新增符号名 → {文件:行} 反向索引（复用 SymbolPatterns 提取行号，2 分钟 TTL 缓存），`symbols` 工具一次定位符号定义；注册桌面+移动 ToolRegistry、标记 Safe、9 项自测
- **手搓精简 SQL 引擎**：`WayCoder/Sql/SqlEngine.cs` 纯 C# 只读 SQL 查询引擎（替换 Microsoft.Data.Sqlite 摆脱原生依赖），`sqlite` 工具查询本地 SQLite 库
- **编码转换工具**：`convert_encoding`（`TextEncoding.cs` + `ConvertEncodingTool.cs`）文件编码互转，支持 GB18030/GBK/Big5/Shift-JIS 等绝大部分编码，默认 UTF-8；编辑器四端（TUI/Web/GUI/移动）自动识别编码 + 保存保真
- **git pull/push（移动端纯 C# 传输协议）**：`PackFile.cs` pkt-line + packfile 编解码（zlib + delta）、`GitRemote.cs` smart HTTP 协议（fetch/push/clone + remote/凭证配置）、`GitCore.cs` 可达遍历 + checkout + remote config；桌面端 GitCommand 白名单放开 pull/push/fetch/remote/clone；凭证脱敏不进日志/commit
- **移动端后台切换卡死修复**：App 切后台（来电/Home/锁屏）时 `CancelActive` 取消在途流式请求，避免后台 SSE 连接被系统挂起导致切回 UI 卡「思考中」
- **项目检测预算 bug 修复**：`WalkFiles` 加 pattern 参数，.csproj/.sln 用 `EnumerateFiles(pattern)` 只枚举匹配项，避免通用扫描被 dist/packaging 占满预算漏检项目类型
- **卫生项**：winget manifest 补 0.96.25（brew 已对齐）、brew formula 修「体」字乱码 + 删多余 end、codes/ 加 .gitignore
- **自测**：4788 通过 / 4 失败（4 项为 pre-existing 环境依赖失败，v0.96.25 基线同样 4 失败；本次新增 66 项全过）

## v0.96.25 (2026-08-28) — 控件基类提炼 + 渲染器命名空间重构 + 编译警告清零

- **编译警告清零**：csproj 修复 WebAssets.Generated.cs 重复编译（CS2002——Windows 下 glob Identity 是反斜杠路径，Target 里 Remove 用正斜杠不匹配）；PackFile 位运算符号扩展显式强转（CS0675×3）；KbIndex null 引用修复（CS8601/8602）；自测回调 content! 断言（CS8604×3）；清理全局已声明的冗余 using——`dotnet build` 0 警告 0 错误
- **控件基类提炼**：新增 `TuiDisplayControl`（CanFocus=false，收纳 15 个纯展示控件）+ `TuiBorderedControl`（BorderStyle/BorderColor/GetBorderChars 统一 4 个边框控件）；TuiPanel 删除自写的不完整边框 switch 改委托 AnsiHelper（补全 Solid/Dotted/Dashed/Slash/Triangle 等新样式）；TuiRect/TuiLine 的 Style 属性统一改名 BorderStyle
- **渲染器命名空间重构**：`WayCoder.UI.Tui.ToolRenderers` → `WayCoder.UI.TUI.Renderers` 统一命名空间；TuiMarkdown 从 Custom/ 移到 UI/TUI/；编辑器/对话框/列表等控件格式化统一（换行/括号风格）
- **细节调整**：toast 队列上限 5→10；列表 Home 键直接跳到底部；模型信息栏精简（工作模式→模式、经济模式→经济、大模型→大、小模型→小）；快捷键栏去掉「Tab 补全」提示
- **自测**：4720 全过

## v0.96.24 (2026-08-28) — TUI 动画心跳：主渲染循环被堵时 spinner 仍持续转

- **需求**：卡死时动画控件也停——动画帧由主渲染循环驱动，循环被堵（ReadKey 被抢 / 锁 / 长任务）Render 不再调用，spinner 冻结
- **实现**：`TuiManager` 独立动画心跳线程（后台，~120ms/帧）——主循环最近 150ms 内 Render 过则跳过（避免双写+光标跳动），超过则接管直写 spinner 帧；`RenderDirect` 内部门控活跃屏 + 无浮层，安全
- **效果**：只要 screen 可见（活跃屏且无对话框覆盖），spinner 就一直转，UI 看起来是活的；不取 `_renderLock`（否则主循环堵在 Render 里心跳也停）
- **配套**：`TuiDynamicBar.RenderAllDirect` 快照迭代（防 DirectWriters 并发增删抛异常）
- **自测**：4720 全过

## v0.96.23 (2026-08-28) — TUI 卡死修复：后台线程全部桥接 UI 线程 + RenderWait 单帧防御

- **TUI 卡死根因（模型管理对话框）**：在线导入/扫描/连通性测试的进度回调和完成刷新在 `Task.Run` 后台线程**直接改控件树**（`help.Text`/`MarkDirty`/`RefreshParent` 重建表格行），与渲染线程并发读写 → 渲染读到半改态抛异常 → `RenderWait` 异常逃逸、`evt` 永不置位 → 对话框永久卡死
- **修复**：ModelPicker（在线导入 onProgress、扫描完成、本地导入完成）与 ProviderPicker（连通性测试完成）全部改 `screen.PostToUI` 桥接回 UI 线程执行；后台线程只投递不碰控件
- **RenderWait 单帧 try/catch**：渲染/读键一帧异常不再逃逸（否则窗口栈残留 → 永久冻结），下一帧照常重绘
- **自测**：4720 全过

## v0.96.22 (2026-08-28) — provider 数据库两条铁律：不允许重复地址 + 同供应商不允许重复模型

- **地址唯一铁律**：`RegisterProvider`/`UpdateProviderUrl` 拒绝「地址已被其它供应商占用」（同地址 = 同供应商）；CLI `/provider add`、TUI `/provider`、移动端导入供应商均弹出明确拒绝提示
- **自动修复**：新增 `DeduplicateProviders`——加载 providers.json 后自动归并共享同一地址的重复供应商（内置优先、id 字典序靠前者胜出，被合并者的模型改挂到保留供应商）；`--model clean` 也触发
- **内置合并**：bailian（百炼）与 opencode 旧别名均并入对应规范供应商（qwen / opencode-zen），内置注册表不再有重复地址
- **模型唯一铁律**：`AddCustom` 同 providerId+id 二次写入自动合并；`All` 按规范化 baseUrl（去尾部斜杠）覆盖内置，消除「同供应商同模型因地址变体重复」
- **自测**：4720 全过；Maui Android 编译通过

## v0.96.21 (2026-08-28) — 供应商管理对话框（全端）+ TUI 导入异步 + 模型选择框闪烁/省略号修复

- **TUI `/provider` 供应商管理对话框**（新 `ProviderPicker`）：列出全部供应商（🔑有Key/⚠️无Key/🌿本地 · 显示名 · Key状态 · **模型数量** · 连通性 · 地址），按钮：设Key / 清Key / 测试 / 添加 / 改名 / 改地址 / 删除；改动即落盘 providers.json
- **移动端供应商 CRUD**：供应商卡片点开菜单「管理模型 / 设Key / 清Key / 改名 / 改地址 / 删除」；「＋导入供应商」正确注册 providers.json
- **Web 端 models.dev 导入**：在线导入源列表新增 models.dev；导入改后台线程异步（不再阻塞请求线程）
- **TUI 导入全部异步 + 进度**：在线导入（models.dev 4.4MB/7000 模型）改 Task.Run 后台执行 + 底部进度显示（拉取→解析含 KB→完成），不再卡死 UI；报告含「X 供应商 / Y 模型」
- **模型选择框闪烁修复**：箭头导航去掉 `screen.MarkDirty()`（它设 RootView.IsDirty 强制整屏重绘）→ 只增量重绘选中行，标题栏/外框不再闪
- **省略号 U+2026 宽度修复**：等宽终端按 1 列渲染（曾按 EA Ambiguous 算 2 列 → 供应商对话框等表格后列错位）；测试同步更新
- **供应商注册表**：新增 `RenameProvider`/`UpdateProviderUrl`（保留其他字段落盘）
- **供应商唯一 id 由 base_url 决定**：新增统一入口 `ResolveProviderId(baseUrl, fallbackPid)`——已知网关 host→规范 id（deepseek/openai…，opencode 按路径分 Go/Zen），未知 host→按 host 派生稳定 id（同地址必同 id，跨来源也能合并去重），无地址才回退来源 pid；models.dev 导入补齐 base_url 归类（此前直接用 pid），OpenCode/OpenClaw/Crush/Claude/Codex 导入全部统一走此入口
- **自测**：4711 全过

## v0.96.20 (2026-08-28) — 聊天浮动滚底 + 压缩进度进状态区 + models.dev 导入 + 内置模型丰富 + 文件页编辑模式

- **聊天浮动「滚到底」按钮**：手动上翻离开底部 → 右下角 ↓ 浮动按钮，点它滚到底并恢复自动滚动后隐藏
- **上下文压缩进度进状态区（所有端）**：核心 `CompressWithSmallModel` 不再把压缩进度喂进 token 流（源头修复），只走 `ContextManager.CompressProgress` 事件——桌面动态栏 / Web 状态区 / 移动状态栏固定位置显示，聊天区保持干净；移动端 `ChatPage` 订阅事件显示「🔄 压缩中 [Lx/3]…」，并过滤压缩 token 不进内容
- **models.dev 导入（所有端）**：`ModelCatalog.ImportModelsDev` 解析 models.dev `api.json`（200+ 服务商、7000+ 模型、价格/上下文/思考/工具），加入 `ModelCli.OnlineSources` → 桌面/Web/移动「在线导入」多出 models.dev 源；模型 id 自动去服务商前缀
- **内置目录丰富**：更新过时上下文/价格（gpt-5.5/5.4 → 1.05M、claude-sonnet-5 → 1M 等），新增 18 个新模型（GPT-5.5 Pro、GPT-5.6、o3 Pro、Claude Fable 5、Kimi K3/K2.7 Code/K2.6、Grok 4.6、Gemini 3.1 Pro、GLM-5.3/5/4.7、Qwen3.7 Max 等）
- **导入去重：有价格覆盖没价格**：`MergeModel` 同供应商同模型有价格者胜（多源导入后价格最全）
- **connectId 去重键规范化**：`ModelKey` 两侧 `NormalizeId`（小写 + 去空格 + 空格转横线），同供应商同模型不因导入来源大小写/空格差异重复；`RemoveCustom` 兼容旧格式
- **文件页编辑模式**：工具栏「✏️ 编辑/✔ 完成」开关，编辑模式下点任意目录/文件弹「重命名 / 删除 / 创建 ZIP 压缩包」；`SandboxFsService.CreateZip`（ZipArchive 打包，AOT 安全）；点击机制恢复 CollectionView SelectionChanged（TapGesture 在 Android 不可靠）
- **自测**：新增 models.dev 导入解析 / 去重价格覆盖 / connectId 归一化测试，共 4703 全过

## v0.96.19 (2026-08-28) — 移动端聊天卡死修复 + 发送队列（忙时排队，输入永不卡）

移动端聊天在 agent 长任务/长思考时界面卡死（输入、点击无响应）。两个根因一并修掉：

1. **思考流刷爆主线程**：`aiMsg.Reasoning` 每个思考 token 都 set → 每次触发绑定重渲染。长思考流（DeepSeek 几千字思考）每 token 刷一遍 UI → 主线程被淹没。改为与正文一致节流（≥300 字符 或 ≥120ms 才更新，最终补全全文）。
2. **发送键 = 停止键**：运行中发送键变「■停止」，忙时无法发新消息。改为**发送始终可用**（忙时入队），新增独立红色 ■ 停止键（运行中才显示）。

- **发送队列**：agent 忙时发送的消息立即可见并标「⏳ 排队中…」，忙完自动取下一条（「📤 发送中…」→ 完成），多条按序串行处理；输入框永不因忙禁用
- **`ChatPage` 重构**：`OnSendClicked` 拆为 `ProcessQueueAsync`（队列串行循环）+ `RunOneMessageAsync`（单轮对话）+ `OnStopClicked`（独立停止）
- **思考节流**：`ShouldRecomputeReasoning`（同 Formatted 的 300 字符/120ms 策略），`finally` 补齐最终思考全文
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误

## v0.96.18 (2026-08-28) — 移动端 git 拉取大仓库闪退修复（下载/解码/大对象三路 OOM）

移动端 git 同步（克隆/拉取）大仓库时闪退，三个独立 OOM 根因逐一修掉（实测 VML.git 4 万+ 对象、pack 数百 MB、**单对象 593MB**）：

1. **下载阶段 OOM**：`UploadPackAsync` 把整个响应包读进内存（`ms.ToArray()` + side-band 副本），pack 数百 MB 时单次 ~284MB 分配直接超安卓堆上限（256MB）→ `OutOfMemoryError` SIGABRT 闪退。改为 **side-band 流式解码写入临时文件**，内存上界降到流缓冲区。
2. **解码阶段 OOM**：`PackFileReader` 把全部解压对象堆进内存字典，解到约 3 万对象即内存不足被杀。改为 **单遍解码 + 有字节上限的 LRU base 缓存**：对象解出即回调写盘、delta base 只经 24MB LRU 暂存，miss 从盘上已写入的 loose 对象按 sha 读回。
3. **单对象超大 OOM**：VML 有 **单文件 593MB** 的对象，`new byte[size]` 托管分配（哪怕 `largeHeap` 512MB）都装不下 → **内容改走原生内存**（`NativeMemory.Alloc`，不占托管堆，实测 1GB 原生分配读写全通过、托管堆仅 4MB）。SHA1 用 `IncrementalHash` 流式、写盘用 `ZLibStream` 原生流式，全程不物化托管 `byte[]`。

- **`DownloadPackToFileAsync`**：protocol v2 fetch 响应逐帧 `PktLine.ReadTolerant` 解码，channel-1 pack 字节直接写临时文件（每 MB 报一次进度，UI 不刷屏）；`finally` 清理临时文件
- **`PackFileReader.ReadFile`**：从临时文件分块解码（`InflateAt` 块不够大自动加倍重读），单对象压缩数据读取上限 64MB；`ReadObjectCount` 从文件头读对象数供进度显示
- **大对象原生路径**（`Read`/`ReadFile`）：>16MB 对象经 `NativeMemory.Alloc` 分配内容缓冲、`DecompressInto` 直接解入原生 Span，`ObjectShaNative` 流式算 sha、`WriteLooseObjectNative` 原生流式写 loose，`onObject` 收 null 内容（已写盘）；压缩数据也从文件读原生缓冲（`InflateAtInto`）不占托管堆
- **流式哈希/写盘**：`GitCore.WriteObject`/`ReadObject`/`PackFileReader.ObjectSha` 改用 `IncrementalHash` + 精确大小分配，消除 `header+content` 大数组与 `MemoryStream` 翻倍物化
- **`Inflater` 改造**：核心改 span 输出（`DecompressInto` 写入调用方缓冲，托管/原生皆可），`BitReader` 改 `ref struct`（原生输入支持，注意必须 `ref` 传参否则位置不推进——曾致死循环）
- **`BitReader.ReadBytes` 修复**：越界抛 `EndOfStreamException`（与 `SkipBytes` 一致）而非 `Array.Copy` 的 `ArgumentException`
- **`PackFileReader.Read` 内存有界化**：删除「预扫描 + 全量保留 base」，改 `BaseCache`（LRU 按字节上限淘汰）
- **`android:largeHeap="true"`**：托管堆上限提至 512MB+（小对象/中对象的保底）
- **自测**：新增 Inflater.Skip / 回调 API / ofs-delta / ReadFile / **大对象原生路径** 测试，共 4692 全过
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误

## v0.96.17 (2026-08-27) — 代码同步卡死修复 + 远程分支完整识别

修复点选历史仓库时界面卡死：`ListRemoteBranches`（ls-refs）原为同步网络调用跑在 UI 线程，慢网络/不可达时阻塞界面。改为后台线程拉取远程分支（本地分支立即显示、远程到了再合并），并用独立 5s 短超时 client。

- **代码同步页分支列表**：本地分支 + 远程分支（`origin/xxx` 前缀，ls-refs `refs/heads/*`），way-coder 完整识别 5 个分支（apt-pages/hasee/mac/master/test）
- **拉取远程新分支**：选 `origin/xxx` 拉取时自动建本地分支 + 切 HEAD + 检出（FetchCoreAsync 对本地不存在的分支建 ref）
- **卡死修复**：远程分支列表后台线程拉取，UI 永不阻塞；网络不可达 5s 内静默返回
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误

## v0.96.16 (2026-08-27) — 移动端 git 全链路修复 + 外部存储 + 设置双分组 + 代码同步增强

移动端 git 同步彻底打通（克隆/拉取/推送全链路真机验证），大项目（way-coder 12184 对象）端到端跑通。

- **git 同步重大修复**：
  - `GitRemote` 改 **protocol v2 fetch**：Gitee 仅响应 v2（v1 请求返回空 → 「upload-pack 无 packfile」），补 `Git-Protocol: version=2` 头 + v2 请求帧（`command=fetch` + `0001` 定界 + want/have/done）
  - **side-band-64k 解码**：channel1=pack 数据、channel2=进度，解码后定位 PACK 魔数
  - **Inflater 两个潜伏 bug**：HuffmanDecoder 构造器越界（`_symbols[nextCode]` 用规范码值索引 288 长数组越界 → 改位置索引填充）；码位位序颠倒（`code |= bit << (len-1)` LSB 构建与规范 first 不匹配 → 改 `code = (code << 1) | bit` MSB 优先）
  - **BuildTree 树排序修复**：git 要求目录按 `name/` 与文件统一排序（旧实现文件全在前目录全在后 → 含子目录仓库 push 被服务端 `treeNotSorted` 拒）。修复后大仓库 push 成功（`unpack ok`）
  - **远程分支**：`ListRemoteBranches`（ls-refs）+ 拉取远程新分支自动建本地分支
  - **克隆/拉取进度显示**：下载 pack MB → 解码对象 i/N → 检出文件 i/N，大仓库不误判卡死
- **外部存储**：workspace 优先 `sdcard/waycoder/workspace`（卸载重装代码不丢），config 落 `sdcard/waycoder/config`，未授「所有文件访问」自动回退 App 私有目录
- **设置页双分组**：大模型/小模型各自独立「服务商/模型/地址/API-Key/测试按钮/保存状态」；🔍 测试 API Key 按钮（填好立即测，有效自动保存、无效报错）；保存后立即刷新「已保存」状态
- **界面**：首页移除模式/权限按钮（已有 ☰ 菜单）；聊天顶部模式/权限上移行1右侧（行2 只留 todo/上下文/用量/花费）
- **代码同步页增强**：仓库地址历史（拉取成功自动记录，最多 50 个，下拉选择/输入两用）、分支选择器、进度显示
- **自测**：新增 git 包解码测试（Inflater 往返/side-band/tree 排序），共 4677 全过
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误

## v0.96.15 (2026-08-27) — 供应商/模型管理 + 模型选择页 + 文件类型路由 + 多源导入

移动端模型体系大升级：新增供应商/模型管理页（图标区分本地/有Key/无Key、点供应商右滑模型列表、点模型设大/小模型、显示上下文/价格、免费模型绿色、大✓/小✓ 双选中勾）、TUI ModelPicker 移植的模型选择页（分组+搜索+大/小切换）、Web 版同款多源导入（内置/Claude Code/Codex/OpenCode/Crush/OpenClaw/自定义）、文件列表按类型路由（源码→编辑器，图片/音频/视频/未知→系统应用打开）、聊天代码块等宽字体。

- **供应商/模型管理页**（`ModelManagerPage`）：
  - 供应商卡片图标：本地 → 🌿；有 Key → 🔑；无 Key 非本地 → ⚠️ 警告
  - 点供应商 → 右侧滑入该供应商模型列表（`ProviderModelsPage`）
  - 模型列表显示上下文大小 + 价格（`32k 上下文 · $0.50/$1.50 MTok`），免费（0 价）模型绿色
  - 右上角 **大/小切换**：选大 → 点模型设大模型；选小 → 设小模型（`isLarge` 由切换决定）
  - **双选中勾**：`大✓` / `小✓` 分开显示（大小模型可能是同一个，不靠合并图标区分）
  - 📡 扫描全部供应商连通性（HTTP GET 默认地址，3s 超时）
  - 多源导入：Web 版同款来源选择器——内置模型目录 / Claude Code / Codex / OpenCode / Crush / OpenClaw / 自定义添加（文件选择器 → 正则启发式解析模型/Key/baseUrl → `ModelCatalog.AddCustom` 导入）
- **模型选择页**（`ModelPickerPage`，TUI ModelPicker 移植）：🔍 实时搜索过滤 + 大/小模型切换 + 按供应商分组列表，点选即应用（连接层统一入口）
- **文件类型路由**（`SandboxFsService.DetectCategory`）：源码/文本扩展名 → 编辑器打开；图片/音频/视频/未知 → 系统应用打开（`Launcher`），列表按类型显示图标（📝/🖼/🎵/🎬/📄）
- **聊天代码块等宽字体**：`MarkupToFormattedString.MonoFont`（Courier New）——```代码块/diff 输出对齐、模仿命令行（原为比例字体）
- **设置页模型工具**：🆓 免费模型（0 价按服务商分组，点选即切换）+ 📡 扫描连接 + 📥 导入模型（配置文件启发式/内置目录）
- **入口**：聊天右上角 ☰ 菜单 + 首页加「🗂 供应商/模型」；模型条点击改开模型选择页
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误

## v0.96.14 (2026-08-27) — 关于页使用说明 + 会话恢复空气泡修复

v0.96.13 发布后的两处修正：关于页不再显示过长的更新日志改为使用说明；修复「继续会话」恢复多行内容显示空气泡的解析 bug。

- **关于页使用说明**：`AboutPage` 从读内嵌 CHANGELOG.md（日志太长）改为内嵌简洁使用说明（对话/语音图片/文件/编辑器/菜单/会话/设置 各板块），移除 `Resources/Raw/CHANGELOG.md` 内嵌资源
- **会话恢复空气泡修复**：根因 `MauiSessionStore.Load` 用 `File.ReadAllLines` 按 `\n` 分行读长度前缀格式——AI 回复含多行换行时字段被拆散、RawText 截断为空 → 继续会话显示空气泡；改为整串索引解析（`ReadAllText` + 按长度前缀精确读 `len` 字符，含换行字段正确往返）
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误；手机实测会话恢复内容正常

## v0.96.13 (2026-08-27) — 移动端三修：编辑器修复 + 会话持久化 + 右上角菜单 + SVG 图标工具栏

在 v0.96.12 基础上修复编辑器内容只显示首行的严重 bug，补齐编辑器只读保护/横向滚动条、会话持久化（继续会话/新的会话）、右上角功能菜单、SVG 图标工具栏等体验项。

- **编辑器「只显示首行」修复**：根因是 `HighlightLayer` 高亮 Label 的 `LineBreakMode="NoWrap"` 让 FormattedString 里的 `\n` 不换行——内容并成一行只有首行可见（行号 Label 未设 NoWrap 所以正常分行）。移除 NoWrap + `UpdateHighlight` 按最大行宽（CJK 双宽）显式设 `WidthRequest` 约束测量宽度，`\n` 正常分行 → 全高度内容与行号对齐、可滚动
- **编辑器只读默认**：打开文件默认只读（`CodeEditor.IsReadOnly`），点「✎ 编辑」才解锁编辑（🔒 锁定图标切换），只读时禁用撤销/重做/保存
- **编辑器横向滚动条**：`EditText.HorizontalScrollBarEnabled` + `SetHorizontallyScrolling(true)` 不换行；行号在独立列，横向滚动不覆盖行号
- **SVG 图标工具栏**：撤销/重做/查找/大纲/预览/编辑/保存 7 个按钮改为 `ImageButton` + SVG 图标（`Resources/Images/icons/`，MAUI MauiImage 编译内嵌），语义 Hint 保留可访问性
- **会话持久化**：新增 `Services/MauiSessionStore.cs`——聊天的对话正文（User/Assistant 的 RawText）落盘 `Global.Home/maui_session.txt`（AOT 手写长度前缀格式，无反射）；进入聊天页检测到上次会话弹「继续会话 / 新的会话」，可恢复；**不保存思考过程与工具调用返回结果**（太多会撑爆会话）；每轮对话结束 / 离开页面自动保存
- **右上角菜单键 ☰**：聊天顶部状态条右侧菜单，集中缺失功能——🧠 模型选择（连接层统一入口）/ ⚙ 模式切换（Build→Plan→Chat 循环，同步 Agent）/ 🔐 权限切换（Ask→Auto→SmartAuto→Yolo）/ 📋 会话管理（继续/新的会话）/ 📌 任务管理（todo 列表）
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误；手机实测全部通过

## v0.96.12 (2026-08-27) — 移动端二轮：代码高亮 + 编辑器增强 + 动态状态栏 + 图标 + 多种体验修复

移动端（`WayCoder.Maui`）体验大升级：聊天/编辑器代码片段语法高亮（修复「write/edit diff 裸显示 `«bright green»` 字面标签」bug）、编辑器透明叠加高亮 + 行号 + 不换行横向滚动 + markdown 预览（表格渲染）、输入框上方动态状态栏（思考/执行工具/等待确认多态 + Braille 旋转动画）、任务完成摘要（用时/token/费用）、首页模式/权限一键切换、应用图标换 app.png（四角透明）、文件列表「用外部应用打开」（HTML→浏览器）、聊天区 markdown 表格等宽对齐渲染、流式智能自动滚动、折叠按钮去方形外框 + 折叠项互斥，并修复滚动大聊天卡死 ANR。

- **代码片段语法高亮**（聊天 + 编辑器）：
  - 新增 `Markup/ToolOutputFormatter.cs`：工具输出按优先级渲染——含 `«»` 标记 → `MarkupToFormattedString.Convert` 解码（修复 write/edit diff 在手机上裸显示 `«bright green»` 等字面标签的 bug）；diff 检测（`---/+++/@@` 首行）→ `+` 绿 / `-` 红 / `@@` 青；代码 → `Syntax.ForFile(file_path)` / `Syntax.Detect` 逐行 Tokenize 上色
  - `MarkupToFormattedString.Convert` 支持 ```lang 围栏代码块（块内 Syntax 高亮）+ markdown 表格块（列宽补齐 + Courier New 等宽 + 表头加粗）——聊天区 AI 回复的表格不再是「一团乱」
  - `ChatMessage.ToolDetailFormatted` 惰性渲染（展开才计算）；onTool 解析 `file_path=` 推断语言
  - **ANR 修复**：相邻同色 Span 合并（大代码块不再拆出上万 Span 卡主线程）+ 超大内容降级纯文本 + 流式富文本节流（≥300 字符/120ms 才重算，finally 补齐最终）
- **编辑器增强**（`EditorPage`）：
  - 语法高亮「透明叠加」：透明文字 Editor（`EditorHandler` mapper，StyleId 精确作用，Android 光标 `TextCursorDrawable` 保留）+ 垫底高亮 Label
  - 左侧行号栏（与代码同 FontFamily/FontSize 对齐，横向固定不随代码滚动）
  - 不自动换行 + 横向滚动：`EditText.SetHorizontallyScrolling(true)` + `SetOnScrollChangeListener` → 高亮 Label `TranslationX` 平移同步（对齐桌面格式）
  - markdown 文件「预览」模式（`Markup/MarkdownPreview.cs`）：标题/表格（Grid 渲染）/代码块/列表/段落，源码⇄预览双向切换
- **输入框上方动态状态栏**（对齐桌面 `TuiDynamicBar`）：`IDispatcherTimer` 100ms 旋转 Braille 图标；多状态——思考中（token 流式）/ 🔧 执行工具 {名}（onTool）/ 等待确认中（`PermissionManager.PermissionPromptStarted`）/ 空闲隐藏
- **任务完成摘要**：每轮对话结束在聊天区追加 `⏱ 用时 · 🪙 prompt+completion · 💰 费用`（`LLM.Task*` + Stopwatch；用户主动停止/无消耗跳过）
- **首页模式/权限切换**：一键循环切换工作模式（建造→计划→聊天，同步 Agent.WorkMode）与确认轴权限（Ask→Auto→SmartAuto→Yolo）
- **应用图标**：`app.png`（1024x1024，四角透明，不设 Color 背景避免透明角染黑）作 MauiIcon/MauiSplashScreen；首页「道码」上方与聊天空态的麻将 🀄 替换为图标
- **文件列表「用外部应用打开」**：`Services/FileOpenService.cs` + Android FileProvider（`file_paths.xml` 只暴露 workspace）+ `Launcher.OpenAsync`——HTML→浏览器等
- **聊天体验修复**：流式自动滚动（`FollowStreamScroll` 150ms 节流 + `_isNearBottom` 智能守卫，滚到底自动跟随、滚上去停止）；折叠按钮去方形外框（补 `StrokeThickness="0"`）；折叠项全局互斥（展开一个收起其余）
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误；手机实测（Xiaomi 13 Pro）全部功能通过

## v0.96.11 (2026-08-27) — MCP Http/Sse 移动端接入 + git 工具注册修复 + /import /doctor 转真实现

继续补齐移动端与桌面端功能差距：git 工具（纯 C#）在移动端已实现但漏注册进工具表（模型无法自主调用）——本次修复；MCP 从「移动端不支持」降级桩升级为 Http/Sse 传输真实现；/import、/doctor 两个命令从降级桩转真实现。

- **git 工具移动端注册修复**：`WayCoder.Maui/ToolRegistry.cs` 漏 `new GitTool()`——模型看不到 git 工具，无法在编码任务中自主 pull/push/切分支。已补注册（与 `/git` 命令一致走纯 C# `GitCore`）
- **MCP Http/Sse 移动端接入**：移除 csproj Exclude 的 McpClient/McpTransport/McpTools/McpCache/ClaudeMcp 五文件 + 删除 CoreStubs 降级桩，`/mcp` 升级为真实现——Http（Streamable）/ Sse（HTTP+SSE 双端点）传输在移动端可用，`DetectTransport` 自动识别；**stdio 传输运行时降级**（`OperatingSystem.IsIOS()/IsAndroid()` 时标记 Failed 并提示改用 http/sse，因 iOS 禁 `Process.Start`）；`MauiBootstrap` 启动时 `McpManager.Init()`，`McpCache` 同步加载缓存工具，Agent 懒建时 `ToolRegistry.AllTools` 已含 MCP 工具
- **移动端 ToolRegistry 合并 MCP**：`AllTools` 从「恒等于内置」改为「内置 + `McpManager.GetDiscoveredToolsSnapshot()` + 插件」，加缓存 + `InvalidateAllToolsCache` 失效逻辑（对齐桌面端）
- **/import 转真实现**：移除 `Infra/ImportHelper.cs` Exclude + 删桩，从 Claude Code / OpenCode / Cursor / Cline 导入模型/API、MCP 服务器、项目上下文（CLAUDE.md）、会话（JSONL）、权限规则
- **/doctor 转真实现**：移除 `Infra/DoctorEngine.cs` Exclude + 删桩，配置完整性/API Key/错误日志/文件锁/检查点/MCP/Hook/临时文件全量自检（纯文件/环境检查，无进程依赖）
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误

## v0.96.10 (2026-08-27) — git 纯 C# 远程操作（pull/push）+ 分支管理 + 凭证双模式 + 移动端思考/工具折叠

移动端（无 git 进程）此前只能本地闭环（init/add/commit/status/diff/log），无法与服务器交换代码、无法分支管理。本次补齐纯 C# git 传输协议 + 分支管理，支持账号密码 / token 双认证；同时移动端聊天区思考过程与工具输出改为折叠显示。

- **纯 C# git 传输协议**（`Git/PackFile.cs` + `GitRemote.cs`）：git smart HTTP v1（pkt-line 帧 + `info/refs` advertisement + `git-upload-pack`/`git-receive-pack`），packfile 编解码——自实现 zlib/deflate inflate（RFC 1950/1951，精确返回消耗字节以定位 packfile 对象边界，`ZLibStream` 4KB 预读会破坏边界）、`ofs-delta`/`ref-delta` delta 应用、非 delta 全量 push 打包；iOS 禁 `Process.Start` 也能拉取/推送
- **pull/push/fetch/clone/remote**：`/git pull`、`/git push`、`/git fetch`、`/git clone <url>`、`/git remote add/set-url` 全打通——pull 解码 packfile → 写 loose objects → 更新 refs → checkout 工作区；push 从本地 HEAD 沿 parent/tree/blob 收集可达对象打包上传（远端 refs 作边界剪枝）
- **凭证双模式**（`/git credential`）：账号密码 `credential <user> <pass>` 与 token `credential --token <user> <token>`，统一 HTTP Basic `base64(user:secret)`（Gitee/GitHub 均接受）；存 `.git/config` `[credential]` 段、remote url 不含凭证（`remote -v` 不泄露）、展示一律脱敏、不进日志/commit
- **分支管理**（`GitBranch.cs`）：`/git branch`（列表/创建/`-d` 删除）、`/git checkout <branch>`（切换，含旧分支独有文件清理）与 `checkout -b`、`/git merge <branch>`（fast-forward，分叉时提示手动处理）、`/git diff <b1> <b2>`（两提交 tree 对比 unified diff）
- **桌面端放开白名单**：`GitCommand` 加 pull/push/fetch/remote/clone/branch/checkout/merge（桌面走系统 git 透传）
- **移动端思考/工具折叠**：AI 思考过程流式实时显示、结束后折叠成「💭 思考过程」条（点击展开）；工具调用显示参数摘要 + 「▸ 输出详情」折叠条；消息列表 `KeepScrollOffset` + 智能滚动（近底部才自动滚到底，否则保持位置可往上翻）
- **修复 commit 消息引号**：`commit -m "msg"` 首尾引号剥离（此前显示 `"msg`）
- **编译**：桌面 0 错误；移动端 `net10.0-android` 0 错误；纯 C# git 分支管理端到端自测通过

## v0.96.9 (2026-08-26) — MAUI 移动端界面完善：设置页 + 首页 + 4 Tab + 主题 + 崩溃修复

v0.96.8 新增的 MAUI 手机版此前处于 M1-M4 雏形（无设置页无法配 API Key、MainPage 模板占位、配色模板默认），本次补齐界面并修复三个 Android 运行时崩溃。

- **新增设置页**（`Pages/SettingsPage`）：服务商下拉 → 联动模型下拉 → BaseUrl → API Key → MaxTokens/Temperature/经济模式。保存复用主工程配置链路（`ApiKeyStore.Set` 存密钥、`ConnectionConfig.ApplyModelChoice` 切模型、`Config.SaveToEnvFile` 持久化、`AgentService.Reset` 重建 Agent），不再需要手写配置
- **首页欢迎页**：MainPage 模板占位（dotnet_bot/"Click me"）→ 真实首页（版本号 + 当前模型/Key 状态卡 + 开始对话/配置入口）
- **AppShell 4 Tab**：首页/对话/文件/设置 + emoji 图标（此前仅对话/文件两 Tab 无图标）
- **全局主题品牌化**：Colors/Styles 定义 WayCoder 品牌主色 `#4A6CF7` + 语义色（气泡/卡片/输入框/弱化文字），Button 圆角 12、TabBar 品牌配色，深/浅双主题
- **聊天页美化**：气泡改语义色、AI 消息加卡片背景、输入栏圆角容器、顶部模型快捷切换条（点模型名 ActionSheet 切换）、未配置 Key 引导跳设置页
- **文件页补齐**：文件项 ActionSheet（打开/重命名/删除）+ 确认对话框，`SandboxFsService` 补 `Rename`/`Delete`
- **AgentService 单槽位静态共享**：改配置后 `Reset()` 对全部实例全局生效
- **崩溃修复**：
  - `AppThemeBinding` 作为资源 + `StaticResource` 赋颜色属性在 Android 运行时抛 `JavaProxyThrowable` → 改 MAUI 官方模式（Color 分 Light/Dark 成对 + 使用处 `AppThemeBinding` 引用）
  - `ApiKeyStore`/`Config.SaveMinimalDotEnv` 用裸 `Environment.SpecialFolder.UserProfile`，Android 上解析成根 `/` → 写 `~/.waycoder` 报 `access to the path '/' is denied` → 统一走 `Global.Home`（移动端重定向 App 私有目录）
  - Android 进程 cwd 为 `/`，所有 `Directory.GetCurrentDirectory()` 派生路径（`SyncConfigJsonToLocal` 等）落根 → `MauiBootstrap` 启动时把 cwd 锚到 App 私有目录
- **编译**：`dotnet build WayCoder.Maui -f net10.0-android` 0 错误；主项目桌面 CLI 0 错误

## v0.96.8 (2026-08-26) — 新增 MAUI 移动端第五前端（Android + iOS）+ 实时录音

第五个前端 `WayCoder.Maui`（Android + iOS）——「完全脱离电脑」的手机独立编程智能体：Agent 核心直接编译进 App，离线可用（仅 LLM API 需网络），自带 API Key/配置，手机本地跑真正的编程智能体而非「遥控桌面后端」。

- **共享源码编译、零主工程侵入**：csproj 用 `<Compile Include="../WayCoder/**/*.cs" Exclude="...">` 复用主工程 Agent 核心（与 `WayCoder.Gui` 同款），Exclude 掉 CLI/TUI/Web/Batch/Watch/进程类工具/MCP/多槽位，加回编辑器核心与 `ContentDiffFormatter`；唯一改动是 `Tools/CwdContext.cs` 抽离 `AsyncLocal<string?>`（替代 `BashTool.CurrentCwd`，51 处引用替换），MAUI 才能干净排除 BashTool 而文件工具相对路径解析不崩
- **`CoreStubs` 桩降级**：10 个进程工具（bash/git/git_pr/lsp/lint/ps/kill/test/sqlite/screenshot）+ CLI/TUI 类型空实现——进程工具在移动端「存在但不可用」，调用得「移动端不支持」清晰提示而非崩溃（iOS 禁 `Process.Start`）
- **本地配置/会话/记忆落 App 私有目录**：`Global.HomeOverride = FileSystem.Current.AppDataDirectory`
- **沙箱**：`SandboxManager.SetLevel("project")` + `AllowedDirectory=workspace`，写工具越界自动拦截
- **四个 Tab（手机屏幕优先）**：对话 / 文件 / 编辑器 / 设置；`AppShell` 底部 TabBar，触控目标 ≥44pt
- **对话流式**：`Agent.ChatAsync` 纯回调 + `«»` 中间格式 → MAUI `FormattedString`（颜色同源 `TuiColors`），token 流式上屏 + 停止
- **内置编辑器**：`EditorCore` 纯数据模型绑定 MAUI `Editor`，文件/大纲跳转/保存/撤销重做
- **交互桥 + 权限确认**：`MauiWebInteraction` 实现 `UxHelper.IWebInteraction` 5 方法，权限确认/计划审批/AskUserQuestion 弹 MAUI 原生对话框（`.NET 10` 新 API `DisplayAlertAsync`/`DisplayActionSheetAsync`，旧 `DisplayAlert` 已过时）
- **实时录音**：🎤 按钮——Android `MediaRecorder` / iOS `AVAudioRecorder` 手搓平台原生（单文件 `#if` 条件编译，不引第三方录音库），落沙箱 m4a（AAC）后复用 `TranscribeAudioTool` 转录填输入框；也支持选已有音频文件转录
- **拍照/选图看图**：📷 按钮——`MediaPicker` 拍照/相册 → `view_image` vision 队列（`ModelCatalog.ResolveSupportsVision` 门控），下轮消息自动带上图片
- **打包**：Android 签名 APK 33MB、iOS Release 编译通过（iOS 真机需开发者证书，MVP 模拟器验证）；主工程自测 4661 项 0 回归

## v0.96.7 (2026-08-26) — GUI 修复：控件溢出换行 + 浅色主题文字可见

GUI（Avalonia）两处显示缺陷：长内容控件溢出屏幕、切换浅色主题后文字仍是深色浅色导致看不见。

- **表格长单元格换行**：`MarkdownBlocks.Table` 列宽由 `GridLength.Auto`（内容全宽，撑破气泡/屏幕）改为 `Star` 均分——长单元格在列内换行，不再溢出
- **代码块防溢出**：`CodeBlock` 外层 Border 加 `ClipToBounds`，超长无空格 token（URL/长字符串）不破版
- **右侧面板长文本换行**：`Panels.Text` 的 `wrap` 模式加 `MaxWidth`——水平 `Row`（StackPanel）测量给无限宽导致 `Wrap` 失效，强制限宽让任务标题/文件名/MCP 名/LSP 命令换行
- **浅色主题文字可见**：`MarkdownInlines.RenderInline`/`ApplyStyles` 正文色此前硬编码深色主题浅色 `#e6e8ee`，切浅色后仍浅色导致白底白字看不见——改为接受 `defaultFg`/`dimFg`（`MarkdownBlocks` 传入动态 `TextBrush`/`DimTextBrush`），正文/行内代码/dim 文字随主题变色
- **非活跃槽位气泡刷新**：`RebuildMessages` 对已存在的 `View` 调 `Render()` 重建 block，切主题后切回其他槽位文字色也正确
- **GUI 编译修复**：`CoreStubs` 补 `Program.GetSlots()` 桩（主项目多槽位 cwd 新增引用，GUI 无 REPL 返回空数组），修复 GUI 项目既有编译错误
- **编译**：`dotnet build WayCoder.Gui` 0 错误

## v0.96.6 (2026-08-26) — 聊天代码片段语法高亮 + 代码块去线框

工具贴出的代码片段此前在聊天区统一颜色，现按语法着色；同时代码块去掉 `┌─┐` 线框，观感更简洁。

- **代码片段语法高亮**：read/write/edit/diff 等工具输出的源码，此前纯文本统一色——`RenderMessage` 纯文本分支对无语言标签内容用 `Syntax.Detect` 启发式探测语言（`using System`/`def`+`import` 等）逐行着色，与 assistant 代码块一致
- **无语言标签按文件扩展名判断**：`GetSyntax` 对语言标签为文件名（`test.cs`/`foo.py`/`main.rs`）但规范名不认识的场景，回退 `Syntax.ForFile` 按扩展名匹配
- **diff 红绿高亮**：纯文本 diff 输出（`---`/`+++`/`@@` 开头，新增 `IsDiffOutput` 检测）`+` 行绿背景（`rgb 0,45,0`）、`-` 行红背景（`rgb 45,0,0`），文件头 `+++`/`---` 行除外，token 前景仍按语法着色——对齐 `RenderCodeBlock` 的 `isDiff` 行为
- **代码块去线框**：顶部 `┌ lang ──┐` 边框改为仅语言标签 + 空格，底部 `────` 边框改为空行
- **自测 4661 通过 / 0 失败**

## v0.96.5 (2026-08-26) — 修复三个列表输出对齐

三个 `--xxx list` CLI 列表的输出对齐问题，均因列宽/标记宽度不一致导致数据错位到下一列。

- **`--model list` 第一列加宽**：模型名列宽写死 28 字符，长模型名（如 `deepseek-v4-flash-vision-exp`、`deepseek-chat-v3-0324`）溢出到第二列——改为按当前所有模型短名最大长度动态定宽（`PadRight`，+2 余量，下限 20）
- **`--provider list` 钥匙列宽统一**：`🔑`（emoji 2 列）与 `—`（em dash 1 列）显示宽度不一致，有/无 key 的行第二列起整体错位 1 列——keyMark 统一占 2 列（`—` 后补空格）。`/provider list` 同款问题一并修复
- **`--connect list` 分列对齐**：原把 `name / providerId / modelId` 挤在一行——拆成 connect 名 / providerId / modelId 三列，前两列按最大长度动态定宽，key 状态（🔑/⚠）作行尾标记

## v0.96.4 (2026-08-25) — 槽位独立工作目录 + 多模态 vision 门控统一

- **槽位独立工作目录**：F1-F10 每个槽位拥有独立工作目录——Agent 在槽位内 `cd` 后目录被持久化，下次任务从该目录起步、互不影响。新增 `/cd [路径]` 命令查看/设置当前槽位目录（支持 `~` 展开）；状态栏路径栏按槽位显示。补齐此前 `AsyncLocal` 虽已隔离 cwd、但缺「每槽位初始目录」与「跨任务持久化」的缺口
- **多模态 vision 门控统一**：
  - 删除死代码 `LLM.ModelSupportsVision`（已无调用点），统一到 `ModelCatalog.ResolveSupportsVision`（模型声明 > 厂商声明 > 家族推断）
  - 修复 `InferSupportsVision` 回归盲点：`Contains("vl")` 泛化漏掉 `glm-4v`（不含 `vl` 子串），补显式匹配
  - 修复门控错模型：`view_image` 工具与 Web 上传用全局 `Config.Instance.Model` 判 vision，但切换模型走 `ConnectionConfig` 不更新 `Config.Model`，槽位独立模型/回退链下误判——改为 `Agent.ExecuteToolAsync` 注入 `_model`/`_base_url`（当前 Agent 实际生效模型），`view_image`/Web 端优先用注入值、兜底 Config
  - 补 `BuildImageMessage` 的 bmp MIME（Web 端已支持 bmp 上传）
- **自测 4661 通过 / 0 失败**（新增 15 项 vision 门控 + 2 项 view_image 注入 + 4 项槽位 cwd）

## v0.96.3 (2026-08-25) — 工作模式 CLI 参数补齐 + 系统提示词惰性生成

三模式（建造/计划/聊天）从「仅交互可切换」补齐为 **CLI 可直接启动**，同时修复构造期按 Build 抢先生成系统提示词被丢弃的浪费。

- **新增 `--mode <build|plan|chat>`**：CLI 直接指定工作模式（行为轴）——build=全工具+完整提示词 / plan=只读白名单+精简计划提示词 / chat=0 工具 0 提示词。与 `/mode` 斜杠命令同源解析；`--permit tiny/chat` 聊天别名仍兼容，显式 `--mode` 时覆盖。填补此前只有 Chat（经 `--permit`）能 CLI 启动、Plan 无 CLI 入口的缺口
- **系统提示词惰性生成（修复浪费）**：`Agent` 构造时实例 `WorkMode` 默认 Build，会抢先执行 `SystemPrompt.Generate`（RepoMap 生成/项目检测/记忆检索等昂贵工作），随后被调用方按全局模式同步并 `ReapplyToolFilter` 整体丢弃。改为 `EnsureSystemPrompt()` 惰性生成——仅首次发送请求时按当时模式/工具集生成并缓存，`ReapplyToolFilter` 只失效缓存。Chat 模式全程不生成任何提示词
- **自测**：新增 16 项断言覆盖「惰性 + 三模式切换请求组合」（Chat 0 工具无 system 注入且不触发生成 / Plan 只读白名单+计划提示词注入 / Build 全量工具+提示词随模式失效重建），`--test system` 模块 724 通过 / 0 失败
- **端到端实测**（`--debug` 抓真实请求）：`--mode chat` → `Messages:1, Tools:0` 仅 user 消息、无「系统提示词已生成」；`--mode plan` → 23 只读工具 + 1962 字符计划提示词、模型实际调用只读 bash；`--mode build` → 30 工具（economy 精简）+ 6947 字符完整提示词

## v0.96.2 (2026-08-25) — 修复源文件被强加 UTF-8 BOM

双代理并行测试（五子棋网页版 + 俄罗斯方块 Python 版）发现：生成 tetris.py 带 UTF-8 BOM（`EF BB BF`），Python 3 严格解析器会拒绝。定位：`EditFileTool`/`MultiEditTool`/`FindReplaceTool`/`EditorCore.Save`/`Web 编辑器 SaveEditorFile` 直接用 `Encoding.UTF8`（.NET 静态实例带 BOM）。

- **新增 `Global.WriteAllTextPreserveBom`**：原文件带 BOM 才保留，否则写无 BOM UTF-8；5 处写文件点统一接入
- `WriteFileTool` 默认本就是无 BOM（`UTF8Encoding(false)`），未受影响
- 自测新增 `[文件编码 BOM]` 节（新写无 BOM / 原带 BOM 保留 / EditorCore 保存不新增 BOM），**4623 通过 / 0 失败**
- 端到端实测：Web 编辑器保存 .py → 文件直接以 `p` 开头（无 BOM）✓

## v0.96.1 (2026-08-25) — 质量加固：CI 自测门禁 + 编辑器补齐

- **CI 自测门禁**：新增 `.github/workflows/ci.yml`（任意分支 push/PR 触发）——主工程构建 + **WayCoder.Gui 构建**（Avalonia 独立工程，此前 CI 从不编译）+ **4620 项自测门禁**（非零退出即失败，防回归）
- **LspTool 锁加超时**：`_sessionLock.WaitAsync()`/`Wait()` 无超时 → 统一 3s 超时回退（防跨槽位死锁挂起）
- **Web 编辑器补替换**：`Ctrl+F`/`Ctrl+H` 查找替换条（上一个/下一个/替换当前/全部替换），替换为前端本地做、保存后落盘；浏览器原生 Ctrl+F 只查不换的问题解决
- **GUI 编辑器补齐**：tab 展开 4 空格渲染（不改缓冲，token 映射到展开串）+ **横向滚动**（Shift+滚轮/触控板横滑）+ 光标/选区/点击定位对齐展开宽度
- 自测 **4620 通过 / 0 失败**；GUI 带 tab 文件实测存活

## v0.96.0 (2026-08-25) — 内置编辑器三端实现（TUI / Web / GUI-Avalonia）+ 修复 lint 全坏 bug

内置编辑器从 TUI 独占升级为**三端可用**——共享同一 `EditorCore` 纯数据模型（已零终端依赖），各端只写视图层。顺带挖出并修复了一个**潜伏的 lint 全坏 bug**（所有语言的保存后 lint 从未真正产出过诊断）。

- **三端编辑器（核心）**：
  - **共享模型增强**：`EditorCore` 加 `SelectionAnchor` 属性（GUI 画选区矩形）+ **CRLF 行尾保真**（加载探测 `\r\n`，保存不再静默转 LF）
  - **TUI 端**（已有，完善）：保存后 lint 完成弹「已保存 · N 错误 M 警告」摘要 Toast
  - **Web 端**（新增）：`✏ 编辑器` 按钮 + 全屏编辑器（透明 textarea 叠 pre，保中文 IME）+ 文件树懒加载 + 行号 gutter + Ctrl+S 保存 + lint 标记轮询 + 改动文件面板可点开；后端 `GET /editor/list|file|diags` + `POST /editor/save` 四路由（`ResolveWithinRoot` 守卫，保存放宽 8MB）
  - **GUI 端**（新增，Avalonia 原生窗口）：自定义 `EditorView` 控件绑 `EditorCore`（语法高亮 + 行号 + 诊断 gutter + 光标 + 选区 + 中文 IME）、`EditorWindow`（保存/打开/新建/查找/关闭确认）、顶栏「✏ 编辑器」按钮 + 修改文件卡可点开；`waycoder --gui [文件]` 或 `--gui --edit <文件>` 直接打开编辑器
- **修复：lint 全链路（三端共享收益）**：
  - `LintTool.RunProcess` 读**未启动的占位 Process** 的 `ExitCode` → 抛「无法运行」→ **所有语言 lint 静默失效**（潜伏 bug，TUI 端同样受影响）。改用 ProcUtil 返回的 ExitCode
  - cs 分支从 `dotnet build <单文件>`（MSB1003 不匹配）改为**构建包含的项目**（向上找 .csproj）
  - 输出截断 4000 → 20000 字符（项目级构建量大，目标文件错误行被截掉）
  - `ParseLintOutput` ⚠-跳过收窄为精确匹配（内建 linter 失败提示也以 ⚠ 开头，不该整体跳过）
  - `SafeResolveWithinRoot` 相对路径**基于 root 而非 cwd** 解析（web 编辑器保存相对路径时 root≠cwd）
- **自测 4620 通过 / 0 失败**（新增 `[EditorCore 选区锚点]`/`[EditorCore 行尾]`/`[Syntax ANSI 契约]`/`[Web 编辑器路径]` 四节 23 项）

## v0.95.0 (2026-08-25) — 环境变量精简（对齐竞品，只留引导级）+ 文档大更新

环境变量从 **107 个**砍到 **14 个**——对齐 Claude Code / Codex（人家只有 `API_KEY`/`BASE_URL`/`MODEL` 几个），配置一律以 `~/.waycoder/config.json` 为权威源。同时补齐 v0.88→v0.94 六版本滞后的五份文档。

- **环境变量精简（核心）**：
  - Schema 驱动机制：107 个 P() 行中 **93 个 envVar 置 null** = 仅走 config.json（`/config <Key>` 或设置界面写入），保留 14 个引导级
  - **保留 14 个**：服务商引导（Provider/BaseUrl/ApiKey/Model/SmallModel/SmallProvider）、经济模式（Economy）、鼠标（Mouse）、预算上限（MaxBudgetUsd）、工具白/黑名单（AllowedTools/DisabledTools）、Whisper 三项（独立服务凭据）
  - 内部调参项全部入 config：超时/压缩阈值/沙箱边界/教学模式/向量嵌入/Diff 预览/主题/回退链等
  - `ConfigProp.EnvVar` 改可空 `string?`；`Env()` 补 null 保护；`/config get` 显示「来源: 仅 config.json」区分保留项
  - .env 迁移不变：`SaveMinimalDotEnv` 只重写 5 项引导（服务商/地址/API_KEY/经济模式/鼠标），旧 WAYCODER_* 行自动清理
- **文档大更新**（补齐 v0.87.28-v0.94.0 全部新功能）：
  - `docs/使用手册.md`：命令表补 `/kb /teach /versions /checkpoints prune /undo 编辑级`；权限章节换 SandboxMode 四值；配置表标「仅 config.json」
  - `README.md`：命令/功能亮点/架构树刷新；版本徽章 v0.95.0
  - `docs/知识库.md`：补学习路径/画像导出/教学闭环/向量检索
  - `docs/模式体系.md`：边界轴换 SandboxMode 四值语义
  - `ROADMAP.md`：版本头 + 已完成清单 + 自测数 4597
- **代码侧同步**：Benchmark `/config` 图改「配置键」列；测试断言改「仅 config」

### ✅ 验证
- 完整自测 **4597 通过 / 0 失败**
- `--config get` 实测：`MaxTokens` → 「来源: 仅 config.json」；`Model` → 「环境变量: WAYCODER_MODEL」

## v0.94.0 (2026-08-25) — 教学模式打磨（测验闭环入知识库）

把 `/teach` 做成"最会教你的 AI"——打通「学→测→记→复习」闭环：教学问答 → 评判掌握 → 更新知识库 gap 权重（掌握降、未掌握升+进复习）。

- **TeachBlock 教学法强化**：分步讲解（先结论后原理）、错误归因、类比/比喻、反问追问、测验后给出每题掌握评价并提示 `/teach assess` 记录
- **KbIndex 教学闭环**（核心）：
  - `SetGapWeight`/`AdjustGapWeight`：掌握降权（−0.3，下限 0.3）、未掌握加权（+0.5，上限 5.0）——补齐此前**"掌握降权路径不存在"**的缺口
  - `AssessTranscript`/`ParseAssessment`：教学问答 → 小模型评判掌握/未掌握主题（严格 JSON，可注入 summarize 测试）
  - `ApplyAssessment`：主题匹配 gap 条目 → 掌握降权、未掌握提权 + 进复习轮换
- **`/teach assess`**：评估本次教学会话 → 更新知识库，报告「已掌握 X 项 / 待复习 Y 项」
- **`/teach status`**：教学进度（按权重分组：✅ 基本掌握 / 🔴 待复习 / ○ 学习中）
- 弱项自动进 `/kb review` 间隔重复轮换

### ✅ 验证
- 完整自测 **4597 通过 / 0 失败**（新增 `[教学模式]` 节 7 项：权重方法/解析/评估应用/复习集成/教学法），假 summarize 不依赖真实 LLM

## v0.93.0 (2026-08-25) — 学习路径推荐 + 画像可视化（增强长处）

短板收官后转增强长处——把学习型智能体做深到不可复制，直接服务「提高技能」目标：

- **`/kb path` 学习路径推荐**：从欠缺知识（gap）清单 + 薄弱标签 + ErrorLog 信号 → 小模型合成 3-7 步「接下来该学什么」进阶路线（主题/为什么/实践/自测）。每步写成 `kind=gap + source=path` 条目：
  - **自动接入 `/kb review`** 间隔重复检验掌握（未掌握自动提权重）——零改动，现有 PickNextDue/BoostGaps 天然支持
  - 内容用 `**现象**/**根因**/**修复**/**教训**` 标记 → `QuizQuestion`/`QuizAnswer` 出真实问答
  - LLM 不可用/失败 → 降级用 gap 清单生成基础路径（不失败）
- **`/kb profile json` 画像可视化导出**：SkillProfile → JSON（schema 1.0：total_entries/total_commits/kb_kinds/weak_tags/error_signals/git_commit_types），供 GUI/外部可视化；`/kb profile` 保留终端文本
- **CLI 对齐**：`--kb path` / `--kb profile json`；`/kb` 帮助 + KbArg SubCommands 更新

### ✅ 验证
- 完整自测 **4590 通过 / 0 失败**（新增 `[学习路径]` 节 8 项：解析/生成/降级/复习集成/ProfileToJson），注入假 summarize 不依赖真实 LLM
- 真实 `--kb profile json` 输出完整 JSON（4 条目 / 200 提交 / 错误信号）

## v0.92.0 (2026-08-25) — VS Code 扩展（waycoder-vscode，短板 #4 完成）

`--json` 桥接已就绪；本版本做正式 VS Code 扩展，把学习型智能体带到 IDE。基于 `--web` SSE 流式协议（已核实）。

- **`vscode-extension/`（TypeScript，独立目录）**：
  - **流式对话面板**：webview 聊天 UI（消息列表 + 输入 + 发送/中断），逐 token 渲染
  - **命令**：`WayCoder: 打开对话`（Ctrl+Alt+W）/ `解释选中代码` / `修复选中代码` / `中断`
  - **server.ts**：spawn `waycoder --web <空闲端口>`（`WAYCODER_WEB_NO_OPEN=1`，cwd=工作区），解析端口行、停用 kill
  - **relay.ts**：`POST /chat` + `GET /events` SSE 消费（token/tool/tool_output/done/failed/interrupted）
  - **配置**：`waycoder.path` / `waycoder.port` / `waycoder.model`
  - `--web` 启动失败提示确认安装（可配置 path）
- **ROADMAP**：VS Code 扩展 P1 → MVP 已交付；docs/使用手册补扩展节
- 补齐短板 #4 完成——**短板清单四项全部交付**（#1 沙箱 ✅ #2 checkpoint ✅ #3 语义检索 ✅ #4 VS Code 扩展 ✅）

### ✅ 验证
- `npm install && npm run compile`（tsc 零错误），产物 `out/`
- 打包就绪（`npm run package` → .vsix）
- 手动验证需 VS Code + waycoder 在 PATH（见 vscode-extension/README.md）

## v0.91.0 (2026-08-25) — 语义代码检索（向量嵌入接线，对标 Cursor @codebase）

探查重大发现：向量嵌入基建（`EmbeddingStore` 余弦/混合检索 + `LLM.GetEmbeddingAsync`）早已建好但**从未接入生产**——`ProjectKnowledge.Query` 一直跑纯 TF-IDF。本版本把向量检索接进代码块检索，有嵌入用语义混合，无嵌入/API 失败回退 TF-IDF。

- **`CodeEmbeddingCache`（新）**：代码块向量缓存 `.waycoder/code-embeddings.json`（JNode 原子写）。块键 = `Title + 内容哈希前缀`——内容变键变天然失效、没变复用；`Prune` 清理孤儿键防膨胀
- **`EmbeddingStore.SearchRelevantHybrid(MemoryDocument)`（新）**：返回与 `SemanticMemory.SearchRelevant` 相同元组（调用方零改动）。0.7×余弦 + 0.3×TF-IDF 混合；无向量块纯 TF-IDF；后台 fire-and-forget 补向量（下轮即命中）；embedder 可注入供测试
- **接线**：`ProjectKnowledge.QueryAsync`（向量优先，失败回退 `Query` TF-IDF）；`Agent.ChatAsyncCore` 每轮检索走 `QueryAsync`（async 一行改动）
- **激活死配置**：`EmbeddingDimensions>0` 时在 `/v1/embeddings` 请求传 `dimensions`
- **默认关**：`WAYCODER_EMBEDDING=0` 保持零网络零向量依赖，开启后逐轮语义增强

### ✅ 验证
- 完整自测 **4582 通过 / 0 失败**（新增 `[语义代码检索]` 节 8 项：缓存键稳定/往返/Prune、混合排序、API 失败回退、QueryAsync 关闭走 TF-IDF）——全部注入假 embedder，不依赖真实 API
- 补齐短板 #3 完成（#1 沙箱 ✅ #2 checkpoint ✅ #3 语义检索 ✅；剩 #4 VS Code 扩展）

## v0.90.0 (2026-08-25) — Checkpoint 补短板（编辑级版本 / 保留策略 / 还原测试）

探查确认 CheckpointManager 全文件快照 + `/undo` 字节还原已存在（每轮开始打点）。补齐真正缺口（对标 Claude Code checkpoint）：

- **编辑级文件版本（`FileVersionStore`）**：每次写文件前记录旧内容 → `/undo <文件> [n]` 逐编辑回退（撤销最后一次编辑）。`/versions <文件>` 列版本历史。与轮级整树快照互补
- **保留策略**：`WAYCODER_CHECKPOINT_MAX`（默认 50），创建超上限自动删最旧 ckpt 目录；`/checkpoints prune [N]` 手动清理
- **还原路径测试补齐**（此前因风险跳过）：临时目录内验证 `UndoAsync` FileBackup **字节还原**；`FileVersionStore` 记录/还原/去重/保留全测
- **顺带修复 CheckpointManager 既有 bug**：非 git 目录下 `git diff --name-only` 的错误输出（"warning: Not a git repository"，不含 fatal）被当成变更文件塞进检查点——现在用 `IsGitError` 识别 fatal/warning/usage
- **FindReplaceTool 追踪补齐**：写入后补 `RecordChange` + `FileTracker.RecordWrite`（此前漏记，自动 commit 精准暂存收不到它）

### ✅ 验证
- 完整自测 **4574 通过 / 0 失败**（新增 `[编辑级文件版本]` 节 9 项 + 还原测试 3 项）
- 还原测试：非 git 目录建文件 → 检查点 → 改坏 → `/undo` → 内容还原 ✅

## v0.89.0 (2026-08-25) — 沙箱边界解耦（边界轴独立于确认轴）

CLAUDE.md 待办落地：边界轴（可写范围/网络）与确认轴（Ask/Auto/SmartAuto/Yolo）彻底解耦，对齐 Codex sandbox_mode / Claude 权限分层。**`--yolo` 现在只跳过确认，不解除边界**——可叠加 `hard` 边界（仅项目内写 + 无网络）。

- **边界模型 `SandboxMode { Off / ProjectWrite / NetworkOff / Hard }`**（`WAYCODER_SANDBOX_MODE`，配置持久化）：Off 无边界 / ProjectWrite 仅项目根写 / NetworkOff 禁网络 / Hard 最严。旧 `/perm` 值向后兼容映射（suggest→off、auto-edit→project、full-auto→hard）
- **删除 4 处硬编码耦合**：`Program.cs` `yoloMode` 不再同时设沙箱；`acceptEdits`→权限 Auto（不再映射边界）；`bypassPermissions`→仅 Yolo；删 `yolo/god→full-auto` 别名；BashTool 沙箱检查基于边界模式而非权限
- **真实边界强制（3 层）**：
  1. `Agent.ExecuteToolAsync` 唯一门控：网络关拦 fetch/web_search/download/doc/transcribe/git；项目写限校验写工具路径
  2. `BashTool.Execute` 顶部网络关（yolo 下也生效，localhost 例外）；复用此前**死配置** `AllowNetwork`
  3. 文件工具防御纵深：write/edit/multiedit/find_replace 加 `CheckWritable`（顺带补 multiedit 缺失的 PathSafety）
- **Web `/perm` 语义修复**：`/perm`=沙箱边界、新增 `/permit`=权限（CLI/Web 对齐；Web 权限下拉改走 /permit）
- **BashGuard** 保持硬层（yolo 可跳过属确认层管控意图）；新边界 yolo 下依然生效

### ✅ 验证
- 完整自测 **4562 通过 / 0 失败**（新增 `[沙箱边界]` 节 19 项：模式映射/解耦/网络关/项目写限/yolo 不解除边界）
- 解耦回归：`/permit yolo` 不再改沙箱；`/perm hard` 不再改权限

## v0.88.0 (2026-08-25) — 学习型智能体（错误诊断 / 技能画像 / 教学模式 / 会话复盘）

把「学习」做成智能体内生能力，竞品均无：

- **① 即时错误诊断**：报错/测试失败时自动召回「同类错误上次怎么修的」——`KbIndex.DiagnoseError` 检索知识库（TF-IDF）+ git 历史 fix/refactor 提交（无词面相关回退最近 fix 记录，永不空）。自动注入 `Agent.Tools` 错误自恢复处 + `Agent.Feedback` 自动测试失败处；`kb` 工具加 `diagnose` action；`/kb diagnose <报错>` / `--kb diagnose`
- **② 技能画像**：`/kb profile` 聚合四维——知识库分类分布（ASCII 条形图）、薄弱标签、ErrorLog 信号、git 提交类型计数（`ParseGitLog` conventional 前缀纯函数）。`/kb weak` 的完整版
- **③ 教学模式**：`/teach on|off` / `WAYCODER_TEACH_MODE`——SystemPrompt 注入 `<teach_mode>` 块，AI 逐处解释为什么 + 引用知识库经验 + 完成后 3 问测验（显式覆盖「极简输出/不叙述」规则）；全量/Economy 提示词均生效
- **④ 会话复盘**：`/kb retro` 用会话记录经小模型提炼 1-5 条经验入知识库（`ParseLessons` 纯函数）；`WAYCODER_RETRO_ON_EXIT` 开启后退出 `AutoSaveSession` 自动复盘（默认关）
- **清理**：删除误置于源码树的 CommLib/PortLib（.NET 4.8 老库 36k 行，含反射/System.Management 等 AOT 障碍，默认 glob 卷入还导致 CS0579；经评估整库移植不值——底层即标准 Socket/SerialPort，真需要串口/TCP 调试工具时原生重写更优）；移除对应 csproj 排除与 gitignore 条目

### ✅ 验证
- 完整自测 **4543 通过 / 0 失败**（新增诊断/画像/教学/复盘 11 项），无崩溃
- 真实 `--kb diagnose "build 失败"` 召回知识库 + git 修复史；`--kb profile` 输出画像（git feat 27/fix 24 + ErrorLog 信号）

## v0.87.30 (2026-08-25) — 自主学习编程知识库（/kb + /mind）

面向「提高编程技能与经验」：把工作痕迹提炼成经验条目，全局保存（`~/.waycoder/kb/`，跨项目积累个人编程经验），间隔重复自测强化记忆，薄弱点统计指导学习方向。条目支持**文字 / 代码片段 / Markdown / 链接**（纯 frontmatter md 文件，图片/音频留待后续）。

- **`/kb` 统一命令**（`/mind` 为别名，自动提炼 + 手动记忆合二为一）：
  - `/kb mine [N]`：扫描 git 历史（`git log`+`git show`），经**小模型**把提交归纳为四类经验 JSON（`mistake` 容易犯的错误 / `bugfix` 复杂 bug 修复 / `habit` 个人使用习惯 / `gap` 欠缺知识），`gaps[]` 自动沉淀欠缺清单；LLM 不可用时降级为基础条目（不失败）
  - `/kb save [类别] <内容>`：手动记住（自动带日期上下文，类别自动识别/可显式指定，含 `code` 代码片段）
  - `/kb update <关键词> <新内容>`：更新最匹配条目；`/kb forget <内容>`：删除最匹配条目
  - `/kb search`/`/kb find <内容>`：TF-IDF 查找
  - `/kb review`：**间隔重复自测**——展示「现象+根因」提问，揭示「修复+教训」，掌握 → 间隔 1→3→7→14→30 天递增，未掌握 → 重置 1 天 + 关联 gap 权重提升（薄弱信号）
  - `/kb weak`：欠缺知识清单（gap 按权重）+ 薄弱标签（mistake/bugfix 聚合）+ ErrorLog 错误信号（按 source 计数）
  - `/kb list`：列出全部条目
- **运行时检索 `kb` 工具**：聊天中 Agent 遇到**不熟悉的术语或疑似 bug** 时可调用 `kb` 工具在全局知识库 TF-IDF 检索相关经验（`KbTool`，已注册 ToolRegistry）
- **运行时不打扰注入**：`SystemPrompt` 启动时 `KbIndex.GetRelevant(query)` 把相关经验并入记忆区块（TF-IDF 复用 `SemanticMemory`）
- **文档**：新增 [docs/知识库.md](docs/知识库.md)，README 命令表补 `/kb`
- **纯函数可测**：BuildEntry（LLM JSON 解析）/ SaveManual / Search / UpdateBestMatch / DeleteBestMatch / 复习调度 / weak 统计 / ErrorLog 信号 / kb 工具，全部离屏单测覆盖

### ✅ 验证
- 完整自测 **4529 通过 / 0 失败**（新增 `[知识库经验]` 节 30 项），无崩溃
- `--kb mine/list/review/weak` CLI 实测：真实 git 历史 → `~/.waycoder/kb/` 条目生成、weak 输出真实 ErrorLog 信号（ApiKeyStore×136/LLM×101）、review 学习模式正常
- 全局隔离：`Global.HomeOverride` 使自测不污染真实 `~/.waycoder/kb/`

## v0.87.29 (2026-08-25) — 全控件鼠标支持 + SGR 解析层测试闭环

让「凡能获得焦点的控件都支持鼠标」成真——此前仅 8 个控件实现 `OnMouse`，`TuiComboBox`/`TuiRadioGroup`/`TuiTreeView`/`TuiTableList`/`TuiTabs`/`TuiInput`/`TuiTextArea`/`TuiPromptBar` 均可聚焦却只能键盘操作；且真实终端字节→事件的 SGR 解析层零测试覆盖。

- **8 控件新增 `OnMouse`**（点击=聚焦+交互，滚轮=滚动）：
  - `TuiComboBox`：折叠点击展开；展开点下拉行选中+自动折叠；点外部不消费；**覆写 `HitTest`** 展开时命中区扩展到下拉底部（基类只认 Height=1，容器路由点不到下拉行）
  - `TuiRadioGroup`：点选项选中；`TuiTreeView`：点行选中、点展开符列（▼/▶ 2 列）切换展开、滚轮滚动；`TuiTableList`：点数据行选中、组头行跳过、滚轮；`TuiTabs`：点标签切换；`TuiPromptBar`：点条目选中+激活、滚轮
  - `TuiInput`/`TuiTextArea`：点击定位光标（新增 `TuiEditBase.VisualToCharCol` 视觉列→字符列，Tab/CJK/emoji 宽度感知）
- **修复点击聚焦缺口**：`TuiList`/`TuiListView` 点击后置 `Focused=true`（此前点击不聚焦，后续方向键路由不到——`TuiView.OnKey` 只派发聚焦子控件）；`TuiRichEditor` 保留既有 `OnFocusRequested` 宿主机制不改
- **SGR 解析纯函数化**：`InputManager.TryParseEscapeSequence` 的解析逻辑抽出 `ParseSgrMouse(string)`（`<` 后内容如 `"0;10;5M"`），首次有离屏单测锁定「1-based→0-based 坐标、code→左右键/滚轮/motion」映射（注：注释称 motion 为 32/33/34/35，代码实际认 35/36/39——以代码为准并已注明）
- **`TuiMouseTest` 34 → 76 断言**：新增 TestSgrParse（9 条）+ 8 控件测试（下拉框 5 / 单选 3 / 树 7 / 表格 5 / 标签页 3 / 输入框 3 / 多行框 2 / 提示栏 2）+ 列表点击聚焦补强

### ✅ 验证
- 编译 0 错误
- 完整自测 **4501 通过 / 0 失败**（TuiMouse 76 条全绿，较上版 +42），无崩溃
- `--tui-mouse` 独立运行 76/76 通过；`--test ui` 含 `[TuiMouse]` 节

## v0.87.28 (2026-08-25) — TUI 鼠标支持离屏测试（--tui-mouse）

补「鼠标支持可验证」短板——此前鼠标路由只在真实终端手动点、无自动化回归。新增 `--tui-mouse` 命令，离屏模拟点击/滚轮/悬停/拖拽，逐项报告每个控件与界面的 OnMouse 是否正确响应：

- **`TuiMouseTest`（34 断言）**：`OnMouse(InputEvent)` 是纯函数式接口（绝对坐标进 → 消费与否 + 状态变），不依赖真实终端/鼠标硬件，故可完全离屏自动化、无需交互。固定 `Tty.SizeOverride = (100, 40)` 保证窗口拖拽/缩放 clamp 稳定。
- **覆盖 11 控件 + 3 分发层**：按钮（命中/区域外/悬停进出）、列表（滚轮/点击选中/多选勾选取消）、复选框、滑动条、滚动条（跳转/拖拽/释放）、懒列表（滚轮/激活）、富编辑器（滚轮/点击定位光标）、按钮组（委托子按钮）、行内权限块（允许/已解决不响应）、视图命中路由、窗口拖拽、屏幕模态遮罩拦截。
- **已知不支持鼠标的界面如实上报**：`ReportUnsupported()` 列出 5 个全屏 ANSI 对话框（ModelPicker/SessionPicker/ReasoningPicker/CommandPalette/FilePicker）——内部虽用 TuiButton 等控件，但输入层走 `Console.ReadKey` 阻塞循环、不经 OnMouse 分发，仅键盘可用。
- **SelfTest 集成**：`[TuiMouse]` section（模块 ui）复用 `CollectChecks()`，`/test ui`、`/test all` 均覆盖。

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4465 通过 / 0 失败（新增 34 项：TuiMouse 12 控件/分发层测试）

## v0.87.27 (2026-08-25) — 补竞品短板五连（上下文/验证门/审计/护栏/状态栏）

针对 Claude Code / Codex 的痛点，一次性补五块短板：

- **上下文水位可视化 + 压缩预告/回看**：`ContextManager` 新增 `CompactionWarning`/`CompactionOccurred` 事件与有界 `CompactionHistory`（最多 32 条），压缩前弹预告（即将把前 N 条合并为摘要）、压缩后弹「📦 已压缩 X→Y 条 · A→B tokens」，补足竞品「压缩不可见」的盲区。
- **修完必验证闭环门**：新配置 `VerifyBeforeDone`（默认开，`WAYCODER_VERIFY_BEFORE_DONE`）——本轮改过源码却从未跑过测试时，收尾前强制验一次（测试命令优先、无测试退 `dotnet build`/`npm run build`/`go build`/`cargo build`），失败则注入摘要继续修，防「假修好了」。设置界面补 `toggle` 类型开关。
- **子智能体明文审计日志**：每次子智能体任务把「提示词 + 授予工具集 + 结果 + 耗时」追加到 `.waycoder/audit/subagents.log`（gitignore）+ 内存历史（`SubAgentAudit`），对标 Claude Code 的 subagent transcript，`WAYCODER_SUBAGENT_AUDIT=0` 可关。
- **任务漂移护栏加强**：`BuildGoalGuard` 纯逻辑提示构建器——`<current_goal>` 注入改为逐级加强（6 轮轻提示 → 12 轮强警告「很可能已跑偏」）+ 注入已触碰文件清单（自审是否改到无关文件）。
- **状态栏显示工作目录 + git 分支**：底部状态栏中间新增 `📁 ~/path · ⎇ branch`（`PathStatus` 探测分支，支持 worktree/子模块 `.git` 文件、detached HEAD；home 展开为 `~`；超长保尾截断），补齐「不知道自己在哪个目录/分支」的痛点。

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4431 通过 / 0 失败（新增 47 项：压缩审计 8 + 验证门 8 + 审计 6 + 护栏 11 + 路径 6 + 校验 schema toggle 修正）

## v0.87.26 (2026-08-25) — MCP 目录补国内通讯 + 国内搜索

按用户要求补齐两块：

- **通讯**：微信（`weixin-mcp`，扫码登录即用，无需公众号）、QQ（`qq-mcp`，经 HTTP API 向 QQ 群发消息，需 `QQ_API_URL` + `QQ_TOKEN`，uvx 启动）
- **搜索（国内）**：百度（`baidu-search-mcp`，免费无需 key，中文搜索）、SearXNG（`searxng-mcp`，自托管元搜索，可接国内实例，需 `SEARXNG_SERVER_URL`）

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测通过（新增 8 项：weixin/qq/baidu/searxng 命中 + env 占位 + 通讯分类含微信/QQ）

## v0.87.25 (2026-08-25) — MCP 生态目录 47→83（达 80+ 目标）

内置目录一次扩充到 **83 个**，覆盖 Claude Code 热门生态九成方向。新增 36 个服务器，包名均经 `npm view` 逐条核实存在：

- **搜索**：Serper（Google 搜索 API）
- **数据库**：Snowflake（数据仓库）、DuckDB、ClickHouse、Typesense、Pinecone（向量库）
- **云平台**（新分类）：AWS、Google Cloud、Firebase、DigitalOcean
- **通讯**（新分类）：Discord、Telegram、WhatsApp、Twilio
- **协作/办公**：Gmail、Google Calendar、Shopify、HubSpot、Salesforce、Zendesk、Mailchimp、Trello、ClickUp
- **开发**：Blender（3D）、Kubernetes、ScreenshotOne（截图）、Midscene（AI 测试）、Magic（AI 前端）、Composio、OpenRouter、PostHog
- **服务**：Weather、Spotify、Zapier、n8n、Datadog

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4384 通过 / 0 失败（新增 21 项：服务器命中 + 云/通讯分类 + env 占位）

## v0.87.24 (2026-08-25) — MCP 生态目录 40→47（向量库/云沙箱/浏览器云/邮件）

- **向量数据库四件套**：Chroma、Qdrant、Elasticsearch、Weaviate——补齐当前最热的 RAG/检索 MCP 需求（`chromadb-mcp`、`mcp-server-qdrant`、`@elastic/mcp-server-elasticsearch`、`mcp-server-weaviate`，均带 `${VAR}` 环境变量占位）
- **云沙箱 E2B**：`@e2b/mcp-server` 隔离容器执行代码（对标 Codex 沙箱的云侧补齐，需 `E2B_API_KEY`）
- **浏览器云 Browserbase**：`@browserbasehq/mcp` 云端浏览器自动化（需 API Key + Project）
- **邮件 Resend**：`resend-mcp` 程序化发信（需 `RESEND_API_KEY`）
- 所有新增包名均经 `npm view` 逐条核实存在，非凭记忆编造

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4363 通过 / 0 失败（新增 9 项：7 服务器命中 + qdrant/e2b env 占位）

## v0.87.23 (2026-08-25) — 共用 Claude Code 的 MCP 配置

- **零配置复用 Claude Code MCP**：新增 `ClaudeMcp` 转换器，自动读取 Claude Code 已配好的 MCP 服务器并复用到 WayCoder——三处配置源（项目级 `.mcp.json`、user 级 `~/.claude.json` 顶层 `mcpServers`、project 级 `~/.claude.json` 的 `projects.<cwd>.mcpServers`），`type` 字段自动映射为 WayCoder 的 `transport`（stdio/http/sse），command/args/env/url/headers 全量透传
- **合并策略**：WayCoder 自己的 `mcp_servers.json` 优先，Claude Code 服务器同名（忽略大小写）去重后追加；`McpServerInfo`/`McpServerState` 新增 `Source` 字段（waycoder/claude）
- **来源标记**：`/mcp` 命令、TUI 侧栏 MCP 区、CLI `--mcp`、Web 版 `/mcp` 对 Claude 来源服务器标注 `〔Claude〕`
- **开关**：`WAYCODER_CLAUDE_MCP=0` 环境变量关闭共用（默认开）

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4354 通过 / 0 失败（新增 10 项 ClaudeMcp 断言：type→transport 映射 + 字段透传 + LoadServers 读 user 级 `.claude.json`）

## v0.87.22 (2026-08-25) — MCP 生态目录扩充 + uvx 启动方式

- **MCP 内置目录 35→40**：新增「部署」分类（Netlify），补充搜索（Perplexity、DuckDuckGo）、开发（Figma、Chrome DevTools），包名均经官方文档/npm 核实
- **引入 uvx 启动方式**：DuckDuckGo 走 `uvx duckduckgo-mcp-server`（Python），`McpCatalog` stdio 目录从「仅 npx」扩展为「npx/uvx/docker 任意 command」，`ToServerNode` 通用透传启动命令
- **竞品短板对标**：Claude Code 800+ 生态 vs WayCoder 40 个精选目录，覆盖文件/版本控制/浏览器/搜索/数据库/记忆/开发/协作/服务/部署十大类

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4344 通过 / 0 失败（新增 7 项 MCP 目录断言：5 服务器命中 + 部署分类 + uvx 启动）

## v0.87.21 (2026-08-25) — 代码重构（拆大文件/合重复代码）+ --model import 进度输出

- **拆分 4 个大文件**（纯物理移动/partial，零行为改动）：`BuiltinArgs.cs`（1204 行）按关注点拆 7 文件（`ModelArgs`/`ModeArgs`/`BatchArgs`/`DebugArgs`/`UtilityArgs`/`McpCli`/`CachePurger`）；`McpClient.cs`（1467 行）拆 `McpClient`/`McpTransport`/`McpTools` 3 文件；`LLM.cs` 移出 `JsonHelper` 到 `Infra/JsonHelper.cs`；`ModelCatalog.cs`（1645 行）拆 3 个 partial（核心 / 文件 IO / 供应商）
- **合并重复代码**（聚焦「同一函数被复制」的明确重复）：`FormatSize` 8 处 → `Infra/FormatUtil.cs`（统一含 GB 档）；「保尾截断」7 处 → `ContextManager.TruncateKeepHeadTail`；「Rune 截断+省略号」约 10 处 → `ContextManager.TruncateWithEllipsis`；`StripJsonComments` 2 处 + JSON 转义 2 份 → `Json.StripComments` / `Json.EscapeString`
- **`--model import` 进度输出**：`ModelCli` 导入系列新增 `onProgress` 回调（CLI 走 `Console.WriteLine`、REPL 走 `screen.AddSystemMsg`），导入 / 探测 / 拉取模型列表实时打印，不再干等

### ✅ 验证
- 编译 0 错误（1 个既有警告 `ProviderCommand.cs:119` CS8602，非本次引入）
- 完整自测 4337 通过 / 0 失败
- `--model import opencode` / `alllocal` 实测打印进度后正常导入

## v0.87.20 (2026-08-24) — 模式轴解耦 + 工具风险统一 + MCP 目录扩充 + 自测/代理健壮性

- **边界/确认轴解耦**（Codex 双轴对齐）：`SandboxManager.SetLevel` 不再联动 `PermissionManager`；`/perm` 只管理沙箱边界（suggest/auto-edit/full-auto），`/permit` 只管理确认模式（ack/auto/smart/yolo）；`--yolo`、`--permission-mode bypassPermissions` 等组合入口显式同时设置两轴
- **`--permission-mode` 补齐**：`plan`→行为轴 Plan、`acceptEdits`→边界轴 auto-edit、`bypassPermissions`→full-auto + Yolo、`default` 不覆盖持久化配置、未知值警告
- **`ToolSafetyRegistry` 单一风险数据源**：PermissionManager 确认名单与 AutoModeClassifier 三级分类统一从注册表读取；补齐 git/git_pr/sqlite/job_kill/test 等高风险工具确认路径；未知插件/MCP 工具默认按高风险处理
- **通用白名单 `AllowedTools` 接入决策链**：Build 档与 Build/Yolo 白名单合并，`DisabledTools` 仍最后剔除
- **LLM 代理遵守 `NO_PROXY`**：`localhost`/`127.0.0.1` 自动绕过代理，修复本地 Ollama/LM Studio 与自测 mock 被代理劫持
- **自测全局目录隔离**：`Global.HomeOverride` 支持测试/嵌入场景重定向 home，完整自测不再写真实用户 `.waycoder`
- **MCP 生态目录 29→34**：新增 GitLab、Redis、MySQL、PDF、AWS Bedrock KB RAG、Google Drive，包名/参数经 npm registry 核实，带 `${VAR}` 环境变量占位
- **发行后自检 `/doctor`**：只读检查 config/api_keys/.env/MCP/hooks/临时文件等，`/doctor fix` 执行安全修复
- **Ctrl+E 经济切换完善**：切换后立即刷新 Build 工具集并持久化到 `.env`
- **README/使用手册/模式体系文档同步**：`/perm` 与 `/permit` 语义分开，竞品差异复核与路线图更新

### ✅ 验证
- 新增自测：NO_PROXY 回环/域名匹配、沙箱与确认解耦、permission-mode 映射、ToolSafetyRegistry 一致性、MCP 34 个目录与 env 占位、Doctor 自检隔离
- 完整自测 4328 通过 / 3 失败（剩余为本机 git 进程解析与 bash 大输出管道环境相关）
- 编译 0 错误（2 个既有警告：WebAssets.Generated.cs 重复、ProviderCommand.cs 空引用，非本次引入）

## v0.87.19 (2026-08-24) — API key 解析机制加固 + Keypad 无头修复 + 脚本跨平台

- **API key 解析机制加固**（`ApiKeyStore`/`Program`/`Config`）：**api_keys.json 为权威源，默认优先使用**；环境变量 key 仅在 json 该服务商为空时才导入/使用（`ImportFromEnvironment`/`ImportFromKnownSources`/`--api-key`/自动导入全部加「只补空不覆盖」守卫）；`--api-key` CLI 仅本次会话生效，json 已有 key 不落盘覆盖；**只有手动 `--model key <供应商> <key>` 才允许覆盖**；新增 `ApiKeyStore.EnvKey()` 读取辅助。修复「env/CLI 一换就覆盖 api_keys.json」导致密钥污染、切换模型连不上
- **Keypad 无头自测修复**（`InputManager.Init`）：输出/输入重定向时跳过 Console 终端模式设置，修复 `--keypad` 脚本（用 `INJECT` 喂键的阻塞式选择器如 ModelPicker）在管道/后台环境下未处理异常退出 127
- **发布脚本跨平台**：`package.sh` Git Bash 无 `zip` 时用 PowerShell `Compress-Archive` 兜底（cygpath 转 Windows 路径）；`release.sh` 的 `sed -i ''` 改 GNU/BSD 兼容 `sedi`；`merge_webassets.py` 重配置 UTF-8 输出，修 Windows runner UnicodeEncodeError
- **csproj 排除杂散项目**：`Hello/`、`Hello.Tests/` 独立演示项目（net8.0+xunit 误置于源码树）排除出主项目编译（CS0579/CS0246）

### ✅ 验证
- 新增自测：EnvKey 读取/无 env 返回 null/已有 json key 不被 env 覆盖/json 为空时 env 补入，全部通过
- 自测 4279 通过 / 0 失败
- 编译 0 错误

## v0.87.18 (2026-08-24) — API key 有效期 + aihubmix/openrouter 内置模型 + 启动界面精简

- **API key 有效期字段**（`ApiKeyStore`）：每条 key 支持 `expiry`（永久 / 截止日期），`~/.waycoder/api_keys.json` 向后兼容（无 expiry=永久）；`--model key <供应商> <key> [有效期]` 保存带有效期、`--model key expiry <供应商> <有效期>` 改有效期；`--model key` 列表显示有效期并标 `⚠`（已过期 / 临期 ≤7 天），末尾汇总过期数
- **aihubmix / openrouter 内置常用模型**（`ModelCatalog`）：开箱即用无需先导入——aihubmix 内置 6 个（deepseek-v4-pro/flash、coding-kimi-k3、coding-minimax-m3-free、glm-5.2、gemini-2.5-flash），openrouter 内置 5 个（openrouter/free、north-mini-code:free、deepseek-v3、gemini-2.5-flash、claude-sonnet-4-5）；价格/上下文取真实目录
- **aihubmix 默认端点改 `api.inferera.com/v1`**：官网 `aihubmix.com` 常被墙，按回退规则切到可达端点，免费模型实测可用
- **启动界面精简**：聊天区首条对话不再注入快捷键表（太占地方），需要时 `/help` 弹出控件化面板；槽位欢迎改为简短提示
- **自测修复**：`/mcp add` 缺目录创建 bug（祖先 `.waycoder` 命中时写失败）、reasoning_effort 测试对齐 `SupportsThinking` 门控、服务商端点测试用 `BuiltinProviders` 快照隔离本地覆盖
- **安全**：清除公开仓库历史中泄露的 DeepSeek API key（`git filter-repo` 重写全历史 + 强推分支/标签），此 key 仍须在 DeepSeek 平台轮换

### ✅ 验证
- 新增自测：有效期往返/旧格式兼容/展示文本/SetExpiry、清空内置断言改「仅内置源被移除」，全部通过
- 自测 4275 通过 / 0 失败
- 编译 0 错误（2 个既有警告：WebAssets.Generated.cs 重复、ProviderCommand.cs 空引用，非本次引入）

## v0.87.17 (2026-08-24) — 子智能体健壮性三连：失败重试 / 分批调度 / MCP 目录扩充

- **子智能体失败自动重试**（`AgentTool`）：新增 `SubAgentRetryCount` 配置（默认 1，可 0–5），子任务返回「子智能体错误」时自动把「换一种方法重试」提示追加到任务文本重跑，最多重试 N 次——LLM 偶发抽风（错误工具选择、中途异常）时给第二次机会，不再一次失败就报废
- **超大规模分批调度**（`AgentTool`）：废除旧「超过并行数即报错」硬限制，`tasks` 数组改为**批内并行、批间串行**（每批最多 `SubAgentMaxParallel` 个并发，超出自动分批流水线化）；新增 `SubAgentMaxTotalTasks` 硬上限（默认 100）防 LLM 生成海量任务失控；依赖图每层同样按批串行，扇形展开层不再一次性火并
- **MCP 生态目录扩充**（`McpCatalog`）：内置服务器从 18 扩到 **29** 个，新增搜索（firecrawl/tavily/exa）、数据库（mongodb/neo4j）、协作（notion/linear/atlassian）、服务（stripe/supabase/cloudflare）四类，均带 `${VAR}` 环境变量占位，`/mcp list`/`/mcp add` 直接可用

### ✅ 验证
- 新增自测：`IsSubAgentFailure` 失败判定、`ComputeBatches` 分批切分（10/4、7/3、batchSize≤0 归一等）、`SubAgentRetryCount`/`SubAgentMaxTotalTasks` 配置注册、MCP 目录 24+ 与新增服务器 env 占位，全部通过
- 自测 4268 通过 / 1 失败（1 失败为预先存在的「无约束模型原样发 medium」断言，非本次引入）
- 编译 0 错误（1 个既有警告 ProviderCommand.cs，非本次引入）

## v0.87.16 (2026-08-24) — 竞品短板补齐三连：代码语义检索 / 依赖编排 / MCP 生态目录

- **代码级语义检索**（`CodeKnowledge`）：扫描项目源码提取「符号 + 文档注释」分块（类/函数/方法，纯文本启发式零反射零正则），复用 `SemanticMemory` TF-IDF 检索，把与任务最相关的代码段注入系统提示词——从「只能 grep 精确匹配」升级到「语义召回代码」
- **子智能体依赖编排**（`AgentTool`）：`tasks` 元素支持对象 `{id, description, depends_on}`，DAG 拓扑分层调度（Kahn 算法，每层并行层间串行）+ 环/自依赖/缺依赖校验 + 依赖输出注入后续任务，突破纯并行一次性火并的限制，支持「先分析→再实现」流水线式子任务
- **MCP 生态目录**（`McpCatalog`）：内置 18 个社区常用 MCP 服务器（文件/版本控制/浏览器/搜索/数据库/记忆/开发/服务），`/mcp list [关键词]` 浏览、`/mcp add <name>` 一键写入 `.waycoder/mcp_servers.json` 并热重连；stdio env 补 `${VAR}` 环境变量展开（与 headers 对齐）

### ✅ 验证
- 自测 4247 通过 / 1 失败（1 失败为预先存在的「无约束模型原样发 medium」断言，非本次引入）
- 编译 0 错误（1 个既有警告 ProviderCommand.cs，非本次引入）

## v0.87.15 (2026-08-24) — 修复 /join gemini 会话定位 + 聊天角色统一

- **修复 `/join gemini` 目录命名与 cwd 恢复 bug**（`ContextBridge`）：
  - 项目目录名从错误的 `sha256(cwd)` 改为 Gemini CLI 实际算法 `slugify(basename)`（小写 + 非 `[a-z0-9]` 转 `-` + 折叠连续 `-` + 去首尾，空则 `project`），逐字复现
  - metadata 的 cwd 恢复从错误的 `directories` 字段（实际不存在）改为读 `.project_root` 标记文件 + `~/.gemini/projects.json` 的 `{cwd:slug}` 映射
  - 补齐 `~/.gemini/history/` baseDir（原只扫 `tmp/`，历史会话漏掉）
  - 三路定位：① `projects.json` 精确映射 → ② `slugify` 兜底 → ③ 全量枚举读 `.project_root` 相关性匹配（应对 slug 碰撞后缀）
  - 删除死代码 `Sha256Hex` 及 `System.Security.Cryptography` 引用
- **聊天角色统一**：流式回复占位消息的 `Role` 从 `"agent"` 收敛为标准 `"assistant"`（`ChatScreen.StartAgentMsg` / `AgentSlot.BufferedStartStream` / `BufferedAppendToken`），消除同一 Agent 回复「agent / assistant」双标识混用导致的「两个智能体」；界面只显示一个「智能体」

### ✅ 验证
- 自测 4216 通过 / 1 失败（1 失败为预先存在的「无约束模型原样发 medium」断言，非本次引入）
- 编译 0 错误（1 个既有警告 ProviderCommand.cs，非本次引入）

## v0.87.14 (2026-08-24) — 竞品痛点九连击：可回滚 / 护栏 / 测试驱动 / RAG / 复现 / 可视化 / 离线 / 中文优化

针对竞品痛点补齐 9 项能力（对应 `/todo` 中 #12–#20）：

1. **改坏可回滚**：每次轮对话首次写文件前自动文件快照（不污染 git stash，`~/.waycoder/checkpoints/`），`/timeline` 树状时间线 + `/undo <id> [文件]` 回退整点或单文件；`AutoCheckpoint` 配置开关（默认开）
2. **目标护栏**：每轮把用户任务注入 `<current_goal>`，≥10 轮仍无进展时注入 `<goal_check>` 偏离拉回提示，防止智能体跑偏
3. **成本护栏**：预算到 80%（`BudgetWarnPercent` 可调）即时预警、超预算即止（沿用既有 `--max-budget-usd`）；`/stats` 仪表盘加 10 段预算进度条
4. **测试驱动修复**：`TestCommand` 配置指定测试命令优先于自动探测；本轮内测试失败 → 收尾前「硬绿判定」再跑一次，仍失败则强制继续修复不放行结束（`_turnTestFailed`/`_hardGreenGateDone` 双状态防死循环）
5. **项目知识库 RAG**：`ProjectKnowledge` 摄入 README/AGENT.md/CLAUDE.md/docs/*.md，按标题分块（rune 安全），复用 `SemanticMemory` TF-IDF 检索，把最相关片段注入 `<project_knowledge>`（mtime 指纹缓存，零网络零向量）
6. **可复现脚本导出**：`/repro` 从会话历史提取 bash 命令 + 写文件路径，生成 `repro_*.sh`（`set -euo pipefail`）
7. **可视化**：DrawTool 加 `flowchart` 语义指令（Mermaid 风格 `A[开始]-->B{判断}-->C((结束))`，自动分层布局，节点 `[方]/(圆角)/{菱形}/((圆))` + 连线 `-->/-.->/==>/---`）；`line/arrow/polyline` 支持 `dash` 虚线；`text` 支持 `\n` 多行
8. **内网/离线部署**：`UpdateEnabled` 关闭自动升级（`/update`/`--update` 门控）；`OllamaNumCtx` 显式注入 Ollama `options.num_ctx` 上下文窗口
9. **中文 + 国内模型深度优化**：`ProviderInfo.Temperature` 厂商级温度覆盖（per-provider 参数）；`LLM.LocalizeError` 网络错误中文本地化（超时/连接拒绝/DNS 等映射为可读中文）

### ✅ 验证
- 新增自测：虚线（line/arrow/polyline 解析+SVG+PNG）、多行文字（tspan 拆分）、flowchart（8 图元结构 + SVG/PNG + 错误路径），全部通过
- 自测 4215 通过 / 1 失败（1 失败为预先存在的「无约束模型原样发 medium」断言，非本次引入）
- 编译 0 错误（1 个既有警告 ProviderCommand.cs，非本次引入）

## v0.87.13 (2026-08-24) — /join 新增 Gemini CLI 支持

- **`/join gemini`**：读取 `~/.gemini/tmp/<sha256(项目路径)>/chats/session-*.jsonl`（JSONL：首行 metadata + 后续 message 记录）——解析 `user`/`gemini` 消息与 `functionCall`/`functionResponse` 工具调用，标题取 `summary` 或第一条用户消息，cwd 从 `directories` 恢复
- 定位用**双通道**：① 从 cwd 向上遍历祖先目录算 SHA256 精确定位项目子目录；② 兜底枚举所有 `tmp/*/chats`，靠 metadata `directories` 做相关性匹配（应对 symlink/大小写等 hash 差异）
- `/join` 候选来源扩到 6 个（Claude/Codex/OpenCode/Crush/Aider/**Gemini CLI**）

### ✅ 验证
- 构造 gemini 格式样例 JSONL 端到端：FindSessions 命中、标题/cwd 正确、用户↔助手对话 + `[工具调用]`/`[工具结果]` 完整还原进交接文档
- 格式基于 gemini-cli 0.56.0 bundle 源码反推（`partToString`/`isMessageRecord`/`projectHash=sha256(cwd)`），**未经真实 gemini 会话验证**（需 Google OAuth 登录）
- 编译 0 错误（1 个既有警告 ProviderCommand.cs，非本次引入）

## v0.87.12 (2026-08-23) — /join 新增 Aider 支持

- **`/join aider`**：读取 `.aider.chat.history.md`（纯 Markdown）——从 cwd 向上找项目根历史文件，解析 `USER:`/`ASSISTANT:`/`TOOL:` 段落（多行消息合并），标题取第一条用户消息
- `/join` 候选来源从 4 个扩到 5 个（Claude/Codex/OpenCode/Crush/**Aider**），无候选提示同步补 Aider 路径

### ✅ 验证
- 样例 `.aider.chat.history.md` 端到端：FindSessions 命中 1 个、标题/cwd 正确、用户↔助手多行对话完整还原进交接文档

## v0.87.11 (2026-08-23) — /join 跨工具会话桥接 + 手搓 SQLite 只读解析器

- **`/join`（`/接手` `/续跑` `/handoff`）**：从 Claude Code / Codex / OpenCode / Crush 会话「接着跑」——读取竞品会话的**聊天内容 + todo 清单 + 当前 git 状态**组装成交接文档注入当前 Agent，换工具无缝续跑
  - `/join` 或 `/join list` 列出匹配当前 cwd 的候选会话（来源 + 更新时间 + 标题）
  - `/join claude|codex|opencode|crush` 直接接手该工具最新会话
  - `/join <序号>` 接手列表第 N 个
  - **已有聊天记录则提示覆盖**：当前会话已存在 user 消息时，先弹确认框（「导入会叠加在现有对话之后」），取消则不做任何改动
- **手搓 `SqliteReader`**（`Infra/SqliteReader.cs`）：零依赖、AOT 安全的 SQLite 只读解析器，按 fileformat2 手写——文件头 → `sqlite_master` 定位表 → B-tree 遍历（leaf/interior + 右侧指针 + cell pointer array）→ overflow 页链式读取 → record 解码 + 全类型还原（int/double/text/blob/null），列名从 CREATE TABLE 解析
- **`ContextBridge`**（`Infra/ContextBridge.cs`）：跨工具会话桥接——`FindSessions(cwd)` 按目录匹配扫描四种竞品存储（Claude `~/.claude/projects/*/*.jsonl`、Codex `~/.codex/sessions/`、OpenCode `~/.local/share/opencode/opencode.db`、Crush `<项目>/.crush/crush.db`），`BuildHandoffDoc` 组装 Markdown 交接文档（会话标题/更新时间/工作目录/对话记录/todo/git 状态）
- **`ImportHelper` 修正 Claude 会话路径**：`~/.claude/sessions/*.json` → `~/.claude/projects/*/*.jsonl`（新版 Claude Code 实际存储位置）

### ✅ 验证
- **真实数据核验**：opencode.db 读 session 57 / message 1845 / part 9250 行，行数与 `sqlite3` 一致；大消息数据 SHA256 与 Python `sqlite3` 模块逐字节一致
- **修复 6 字节整数（serialType 5）解码 bug**：漏 `buf[pos+1]` + 移位错（`<<32` 应 `<<40`），导致毫秒时间戳（2³²~2⁴⁸ 区间）全读错成 1970 年——修复后 opencode/crush 时间戳正确显示 2026-08-09
- 当前项目实测找到 30 个候选会话（claude 14 / codex 1 / opencode 14 / crush 1），交接文档正确含真实聊天内容 + 工具调用
- 编译 0 错误（1 个既有警告 ProviderCommand.cs，非本次引入）

## v0.87.10 (2026-08-23) — 新增 /exit /quit 退出指令 + /resume 恢复会话指令

- **`/exit`（/quit / /退出）**：退出 WayCoder（`Program.RequestExit` 设退出标志，走 REPL 正常清理路径：保存会话 + 退出全屏）
- **`/resume`（/continue / /恢复）**：恢复上次自动保存的会话（原 REPL 硬编码改为注册指令，`/help` 可见；无自动会话时友好提示）
- 两个命令注册为 SlashCommand，自动出现在 `/help` 命令表格

### ✅ 验证
- keypad 实测：`/exit` 提示「再见，正在保存并退出…」、`/resume` 无会话时提示「没有可恢复的会话」
- 自测 4196+ 通过（1 失败为预先存在的「无约束模型原样发 medium」断言）

## v0.87.9 (2026-08-23) — 模型不支持工具不催促 + 聊天模式零注入验证

- **模型不支持工具不催促**：`SupportsTools=false`（如 Ollama gemma2 等本地小模型）时，跳过全部工具催促——推理催促 / 首轮分析催促 / 口述代码追问 / 中途停滞催促（做不到的事不催，避免每轮收到「请立即调用 write_file」的错误催促）
- **聊天模式（Chat）零注入实证**：`--permit chat` 一次性模式 prompt 仅 **20 tokens**（纯用户消息），对比 Build 模式 20809 tokens（系统提示词 + RepoMap + Git + 记忆 + 工具 schema）——纯聊天确实 0 注入 0 催促（首条回复即完成，不自动续写/追问）

### ✅ 验证
- Chat 模式 `--permit chat --json -p` 回复正常、prompt 20 tokens、无任何注入
- 自测 4196+ 通过（1 失败为预先存在的「无约束模型原样发 medium」断言）

## v0.87.8 (2026-08-23) — 动态栏橙色 + 控件越界裁剪 + diff 行 RGB 背景

- **动态栏文字改橙色**（RGB 255,180,0，同对话框渐变起始色），Error 状态保留红
- **RenderBuffer.Write 系统级越界裁剪**：行越界丢弃、列越界截断——所有控件内容不得溢出屏幕（修复动态栏文字等超宽时终端自动换行把内容泄漏到下方横线/相邻行）
- **聊天 diff 代码块**：`+` 行黑带绿背景（rgb 0,45,0）、`-` 行黑带红背景（rgb 45,0,0），RGB 精确控色；只在明确 diff 块（```diff / 首行 diff--git/+++/---/@@）生效，避免普通代码 +/- 行误染
- 自测加 RenderBuffer 越界行/列裁剪断言

### ✅ 验证
- keypad RAW 实测：动态栏文字含 `38;2;255;180;0` 橙色前景；diff 块 `+` 行 `48;2;0;45;0`、`-` 行 `48;2;45;0;0`
- 自测 4196+ 通过（1 失败为预先存在的「无约束模型原样发 medium」断言）

## v0.87.7 (2026-08-23) — /free 支持参数（N 直接切换 / restore 还原收费）

- **`/free` 无参数** → 弹框选择（原行为）
- **`/free N`（1~可用数）** → 直接切换第 N 个免费模型（读 free.json 缓存，记住切换前模型）
- **`/free restore`** → 直接还原收费模型（等同 `/free-restore`，跨会话）
- 未知参数 / 序号越界 → 友好提示并回退弹框
- 使用手册 / README 同步 `/free [N|restore]` 用法

### ✅ 验证
- keypad 实测：`/free 2` 切换 laguna-s-2.1-free ✓、`/free restore` 还原 deepseek-v4-flash ✓、`/free` 无参弹框 ✓

## v0.87.6 (2026-08-23) — 补日志文档 + free 弹框切换回归

- 补记 v0.87.4 / v0.87.5 更新日志（此前已提交代码但日志滞后）
- 使用手册 / README 补 `/free`、`/free-restore`、free.json 缓存与 `--model free/restore` 说明
- 新增 `Test/scripts/free_switch.txt`：`/free` 弹框方向键选择 + Enter 切换的按键回归

### ✅ 验证
- 自测 4194+ 通过（1 失败为预先存在的「无约束模型原样发 medium」断言，与本次无关）

## v0.87.5 (2026-08-23) — /free 读 free.json 缓存不重扫 + 修复弹窗未弹出 + 扫描不误判 chat/think

- **free.json 缓存**：`--model free` 扫描生成的可用免费 connect 列表写入 `~/.waycoder/free.json`（增量持久化：逐模型扫到可用立即写，中途中断也能用已扫到的）；`/free`（TUI + Web）直接读缓存弹窗，**不再每次扫描**；free.json 空/不存在 → 提示先跑 `--model free`
- **修复 `/free` 弹窗没弹出来**：FreeCommand 漏了 `screen.ShowWindow`（TuiDialog.Select 构建了窗口但没显示）
- **扫描默认每模型 5s** 没回复即跳过（`--model free <秒>` 覆盖）
- **不误判 chat/think**：`ProbeChat` 判可用加 `ReasoningTokens > 0`（think 模型内容在 reasoning_content 不再误判「空回复」）；LLM 400 回退加**第三级去 `reasoning_effort`**（chat-only 模型被误发 thinking 参数自动降级）；LLM 加 `TimeoutSeconds`（探测固定单次超时，不渐进加长）

### ✅ 验证
- 实测 `--model free`：28 个免费模型 → **4 个可用**（hy3-free / laguna-s-2.1-free / nemotron-3-ultra-free / nemotron-3.5-lightning-free）已写入 free.json
- `/free` 弹框 → 方向键选择 → Enter 切换 laguna-s-2.1-free → 对话正常 → `/free-restore` 恢复 deepseek-v4-flash → 对话正常
- 自测 4194+ 通过（1 失败同 v0.87.6）

## v0.87.4 (2026-08-23) — /free-restore 三端持久化 + 设置弹框渲染/读写修复 + 对话框不自动关闭

- **`/free` + `/free-restore` 三端**：切换免费模型前记住当前模型（`PreviousModel` 持久化到 config.json，跨会话可还原）；TUI 菜单 / Web `/free-restore` / CLI `--model restore` 统一走 `RememberCurrentModel` / `RestorePrevious`；CLI `--model name` 切换前自动记住
- **修复设置界面弹框输入花屏**（两层渲染 bug）：
  - `TuiScrollView` 增量模式无条件重绘全部子项并覆盖模态弹框 → 只画脏子项 + 渲染后清脏
  - 弹框脏判断 `win.RootView.IsDirty` 不覆盖子控件（MarkDirty 只标叶子不冒泡）→ 改 `HasDirtyDescendant` 递归 + RenderWindow 整窗重绘时内容区全量
- **修复设置界面 45 项配置读写缺失**（推理深度/Whisper/工具白名单/经济模式/沙箱/回退链/Watch 等）：`GetValue`/`SetValue` default 走 Schema Getter/Setter 自动补全
- **修复对话框开着自动关闭**：用户主动选择器（ModelPicker/SessionPicker/FilePicker/ReasoningPicker/CommandPalette/键盘帮助/DiffPreview）`RenderWait` 超时改 0 无限等；LLM 询问（Ask/Secret/Select）保留超时默认 cancel；TUI 权限对话框本就不超时
- **Keypad 测试设施**：`INJECT`/`MODEL` 脚本化驱动阻塞式选择器 + `InputManager.InjectKey` + ModelPicker `forceReadKeys` + `FOCUS` 查活跃屏幕 + `RAW` 原始 ANSI 调试

### ✅ 验证
- keypad 脚本实测：设置界面 text/secret/number 弹框输入完整、select 弹框选择后值正确保存（推理深度 low）、ModelPicker 打开/导航/Enter 确认/Esc 取消
- 自测 4194+ 通过（1 失败同 v0.87.6）

## v0.87.3 (2026-08-23) — report/free 实时进度 + 可选单模型超时参数

- **实时进度（stderr）**：`--model report` / `--model free` 测试时逐条输出「正在扫描 [provider] 第 n/N 个（模型）...」，不再干等
- **可选超时参数**：`--model report <秒>` / `--model free <秒>`（如 `--model free 10`，默认 60s，范围 1-600s）
- 进度走 stderr 不污染报告主输出

### ✅ 验证
- 自测 4106+ 通过（0 失败）；`--model free 10` 实测进度逐条显示

## v0.87.2 (2026-08-23) — `--help` 补全今日新增功能

- `--model` 帮助补齐：`check` / `report` / `free` / `clean` / `import alllocal|allonline|all|online <源>`
- 更新 `key` 描述（永不自动删除、删除需确认）、`list`（OpenRouter 短名）

### ✅ 验证
- `waycoder --help` 完整显示所有模型管理子命令

## v0.87.1 (2026-08-23) — API key 永不自动删除（删除需询问确认）

- **`RemoveProvider` 不再连带删除 API key**——clean / provider 清理可以删 provider 注册与模型，但 key 一律保留
- **无效 key 删除需询问**：探测到 401/403 无效 key 时交互确认 `[y/N]`，同意才删；非交互（管道/一次性模式）默认保留
- **显式删 key**：`--model key rm <provider>`（唯一删除路径）
- 修复：clean 误删 opencode-go key、重新导入丢能力字段（glm-5.3 补回 `ReasoningEffortAllowed`）

### ✅ 验证
- 自测 4106+ 通过（0 失败）；glm-5.3 恢复可用

## v0.87.0 (2026-08-23) — `--model report / free / clean` 模型管理命令 + 本地模型按地址判断

- **`--model report`**：测试所有 connect 的连通性，生成报告（✅ 可用 / ❌ 失败原因 / ⏭ 无 key）
- **`--model free`**：测试模型库所有 free 模型（opencode zen `-free` + openrouter `:free`），列出可用的
- **`--model clean`**（统一）：清理无效服务商 + 合并重复模型 + 删无效 connect
  - **修复误删 bug**：clean 只在 **baseUrl 无效**（空/非 http(s)）时删服务商——探测失败/无 key/临时网络都不是删除理由（此前误删 opencode-go/zen/openrouter 模型）
  - 遍历只做标记、结束后统一删除（不能边遍历边删）
- **本地模型按地址判断**：`ModelCatalog.IsLocalUrl`（localhost/127.0.0.1 才算本地）——Ollama 也有云端（ollama.com），是否免 key 看 baseUrl 而非 providerId
- 实测 `--model free`：**9 个可用**（zen：laguna/nemotron-3-ultra/nemotron-3.5；openrouter：nemotron-3 系/laguna/nemotron-nano）

### ✅ 验证
- 自测 4106+ 通过（0 失败）

## v0.86.1 (2026-08-23) — README 补模型管理命令文档

- README 增加「模型管理」命令段：`--model import alllocal/allonline/all`、`--model check [connect]`、`--connect test`
- 说明模型能力特性（SupportsThinking/Tools/Vision）存储与按能力门控、OpenRouter 短名显示

### ✅ 验证
- 纯文档更新，无代码改动

## v0.86.0 (2026-08-23) — 模型能力特性显式标识（SupportsThinking / SupportsTools / SupportsVision）+ `--model check`

- **ModelInfo / ProviderInfo 加能力字段**：`SupportsThinking` / `SupportsTools` / `SupportsVision`（bool?，null=未声明→厂商/家族推断）；三级合并「模型 > 厂商 > 推断」
- **LLM 请求按能力门控**：
  - `SupportsTools=false`（如 Ollama gemma2）→ 不发 tools（400 回退降级为安全网）
  - `SupportsThinking=false`（本地模型）→ 一律不发 `reasoning_effort`（替代 `"none"` hack）
  - Anthropic `thinking` 块 / Gemini `thinkingConfig`：支持思考的模型才开
- **视觉判定元数据化**：`ModelSupportsVision` 硬编码迁移为 `ResolveSupportsVision`（补齐 o4-mini/llama-4/gemma3 盲点），Agent/ViewImage/WebChat 三处改查元数据
- **`--model check [connect]` 新命令**：测试当前/指定连接模型的能力特性（格式 / think / tools / vision / 温度精度 / 上下文 / key）
- 本地模型导入：`ImportLocalServices` 设 `SupportsThinking=false`（删 `"none"` hack）；SupportsTools 留推断（gemma2:2b→false，qwen3→true）

### ✅ 验证
- 自测 4106+ 通过（0 失败）；`--model check` 实测：gemma2:2b（思考❌工具❌）、glm-5.3（思考✅工具✅）、qwen3.5-4b（思考❌工具✅）

## v0.85.4 (2026-08-23) — 本地模型兼容：Ollama/LM Studio 支持 + LLM 400 两级回退（去 stream_options / 去 tools）

- **本地服务模型（ollama/lmstudio/local）导入时设 `ReasoningEffortAllowed="none"`**：Ollama 等不支持 thinking，全局 `reasoning_effort` 会导致 400——`none` 使任何全局值都跳过（不发）
- **LLM 400 两级回退**：① 去 `stream_options`（OpenAI 扩展）② 再去 `tools`（Ollama gemma2 等不支持工具调用的模型）——退化纯文本回复，不再 400 中断
- 验证：Ollama gemma2:2b ✅ 输出 ok（1.9s）；LM Studio qwen3.5-4b 联通（thinking 模型默认思考 content 空，需关 thinking）

### ✅ 验证
- 自测 4106+ 通过（0 失败）

## v0.85.3 (2026-08-23) — 模型列表显示短名（OpenRouter 路由前缀去除）

- **OpenRouter 等在线导入模型 id 带厂商路由前缀**（`openai/gpt-3.5-turbo`），列表显示太长
- 新增 `ModelCatalog.ShortDisplayName`：**显示时去前缀**（`gpt-3.5-turbo`），**调用仍用完整 id**（路由需要）
- 应用：`--model list`、ModelPicker（Ctrl+M）表格、新导入模型 `DisplayName` 直接存短名
- 验证：`--model list` 显示 `gpt-3.5-turbo`（原 `openai/gpt-3.5-turbo`）、`mythomax-l2-13b`（原 `gryphe/mythomax-l2-13b`）

### ✅ 验证
- 自测 4106+ 通过（0 失败）

## v0.85.2 (2026-08-23) — CLI 模型导入增强：`--model import alllocal / allonline / all` + 在线指定源

- **组合命令**：
  - `--model import alllocal` — 导入全部本地模型（Ollama /api/tags + LM Studio /v1/models + CC Switch）
  - `--model import allonline`（或 `online`）— 导入全部在线端点模型列表（OpenCode/OpenRouter/Groq/SiliconFlow/Together/DeepSeek/OpenAI/Moonshot 共 9 个）
  - `--model import all`（或 `auto`）— 本地 + 在线全部
  - `--model import online <源名>` — 在线指定源
- 新增 `ModelCli.ImportOnlineAll`（在线批量拉取 /models 写全局库，无 key 也拉模型、使用前设 key）
- 验证：`import alllocal` 导入 LM Studio embed 模型；`import online opencode` 拉取 muse-spark/mimo/hy3/nemotron 等

### ✅ 验证
- 自测 4106+ 通过（0 失败）

## v0.85.1 (2026-08-23) — 环境变量 key 自动导入补全（connect add 路径）+ providers.json 字段继承内置默认

- **key 自动导入补到 `connect add`**：`AutoImportKeyFromEnv` 抽取为公共方法，`ApplyModelChoice`（选模型）与 CLI/TUI `/connect add`（建 connect）共用——新建 connect 时无 key 自动从官方环境变量复制 + `IsPlausibleApiKey` 校验
- **修复 providers.json 覆盖内置丢失元数据**：`LoadOrCreateProvidersJson` 字段缺失时继承内置默认——旧 providers.json（只有 name/base_url）会把内置 `ApiKeyEnvVar`/`ApiFormat`/`CommonModels` 等覆盖为 null，导致 key 自动导入失效、ApiFormat 解析回退 openai
- 验证：删 minimax key → `MINIMAX_API_KEY` 环境变量 + `--connect add` → ✅ 自动导入 + 提示

### ✅ 验证
- 自测 4106+ 通过（0 失败）

## v0.85.0 (2026-08-23) — 非 OpenAI 格式兼容（Anthropic 原生 + Gemini 原生）+ 模型/厂商参数约束 + 环境变量 key 自动导入

### 非 OpenAI 原生 API 兼容（ProviderInfo.ApiFormat）
- **Anthropic 原生**（`POST /v1/messages`）：`x-api-key` + `anthropic-version` 头；`system` 提取到顶层；`content` 块数组；`max_tokens` 必填；SSE 解析 `message_start/content_block_delta/message_stop`；工具 `tool_use` + `input_json_delta` 累积
- **Gemini 原生**（`POST /v1beta/models/{model}:streamGenerateContent?alt=sse`）：URL 嵌模型名；`x-goog-api-key`；`contents/parts` + `generationConfig`；SSE 解析 `candidates[0].content.parts` + `usageMetadata`；工具 `functionCall`（无 id 合成）
- 多模态按格式转换（Anthropic image/source + Gemini inlineData）
- 内置 `anthropic`/`google` 改走原生端点（gemini baseUrl 改回原生根）；`ResolveApiFormat` 每请求反查，FallbackLLM 跨格式回退自动适配

### 模型/厂商调用参数约束（此前提交未发版）
- `ModelInfo.ReasoningEffortAllowed`（允许集）+ `TemperaturePrecision`；`ProviderInfo` 同字段为厂商级默认，两级解析「模型级 > 厂商级 > 全局」
- `reasoning_effort` 值恒来自全局，越界 → 跳过字段（glm-5.3 medium → 不 400）；temperature 精度按模型级联
- `ReasoningPicker` 按模型允许集过滤级别

### 内置厂商官方元数据 + 环境变量 key 自动导入
- 24 个内置厂商补 `ApiKeyEnvVar`（官方环境变量名）/`ModelsEndpoint`（模型列表接口）/`CommonModels`（官方常用模型）
- 首次选择模型无 key 时自动从官方环境变量复制到 api_keys.json，`IsPlausibleApiKey` 校验防误复制（URL/空白/占位拒绝）

### ✅ 验证
- 自测 4106+ 通过（0 失败）：新增 `TestModelParams`（约束纯函数/往返/集成）+ `TestApiFormat`（Anthropic/Gemini 请求体头/SSE 解析/工具/usage 全断言）

## v0.84.1 (2026-08-23) — 修复 temperature 序列化 bug（glm-5.3 严格网关）+ waycoder 5000 行 C++ 能力检验

- **修复 `LLM.cs` temperature 序列化**：float 0.1f 经 JSON `"R"` 格式输出 `0.10000000149011612`，被 opencode.ai/zen/go 网关的 glm-5.3（限制小数点 2 位）以 HTTP 400 拒绝；改为发送前 `(double)Math.Clamp + Math.Round(2)`
- **真实场景验证**：waycoder 写 5000 行 C++ 程序时暴露（glm-5.3 首轮直接 400），修复后 `--json` 回复正常
- **waycoder 能力检验**：TinyL 脚本语言解释器 **5,584 行 C++**（lexer/parser/AST/求值器/GC/标准库/REPL，25 个内置函数 + switch + 位运算），g++ C++17 编译 0 错误，**25/25 测试通过**，中文输出/闭包/类继承/异常全部可运行
- 过程中还发现：glm-5.3 强制思考，`ReasoningEffort` 只接受 low/high/max（medium → 400），已调低配置

### ✅ 验证
- waycoder 独立验证编译 + 25 测试全过；`--json -p` 请求正常返回

## v0.84.0 (2026-08-22) — 模型配置三层重构：connect / provider / connection + 命令按域分类 + 回退链开关

**模型配置重构**：connect = {providerId, modelId} 命名注册表；provider = {name, baseUrl, apikey} 逻辑一体（物理分文件）；connection = 大 connect + 小 connect 一起切换；回退链 = 一串 connect。

- **三层数据模型落地**：`ConnectionConfig` 重写为分类存储（`connections.json` 分 connects / connections / fallbackChain 区）；`ApplyModelChoice` / `SetActiveConnect` 统一「切换模型 = 切换 connect」入口，ModelPicker / ModelCli / Web / GUI / CLI 全部路由到它；旧配置自动迁移（旧 connections.json、扁平 Config 字段、FallbackChain 模型名 → connect 名）
- **/connect 一键切换**：`/connect <connectId | providerId.modelId | providerId/modelId | baseUrl:model | modelId>` 双分隔符解析（`TryParseSpec` 纯逻辑可自测）；`Ctrl+Shift+M` 快捷键循环切换 connect
- **命令按域分类**：`/connect` 只管 connect 层（注册表 / 命名连接 / 回退链 / 切换），`/provider` 只管 provider（list/add/rm/select/test/import + apikey），`/model` 只管模型目录；CLI 新增 `--connect`、`--provider` 补 select/key/test/import
- **模型栏 `(provider)model`**：区分同名模型不同服务商；显示「实际生效」模型（回退/切换后立即反映 + `(回退)` 标记）
- **回退链开关（默认关）**：`/connect chain on|off`；关 = 只用当前模型失败即停；回退消息明确去向 + 剩余链
- **跨 provider 大/小模型**：`WithModelOverrideAsync` 按小 connect 的 provider 重配 endpoint（finally 恢复）
- **修复**：在线导入分类错配（pname 跟随推断 provider，不再硬编码 OpenCode Go）、导入/清空白屏（connect 迁移预热到启动、渲染路径防抛）、TUI 设置屏缺 SmallProvider、Web 切小模型丢 provider/baseUrl、FallbackLLM `WriteFallback` 自递归
- **槽位 connect 化**：`SlotConfig` 增 BigConnect / SmallConnect 引用，槽位模型切换同步登记 connect

### ✅ 验证
- 主项目 + GUI `dotnet build -c Debug` 0 错误；自测 4152/4153（唯一失败为环境相关的 minimax 本地 providers.json 覆盖）

---

## v0.83.1 (2026-08-22) — TUI 跨线程安全：PostToUI 提炼到基类 + 修 6 处后台直接碰 UI/终端

**修复「本地导入卡住 + 花屏」**（ModelPicker 本地导入期间后台线程直接写控件 → 与渲染循环并发帧交错花屏）。

- **基类提炼**：`PostToUI` / `PumpUIQueue` / `_uiQueue` / `_uiThreadId` 从 ChatScreen 上移到 `TuiScreen` 基类——所有屏幕共用「后台线程投递、UI 线程 PumpUIQueue 消费」机制；`UxHelper.RenderWait` 消费改为 `screen?.PumpUIQueue()`（不再限 ChatScreen）
- **修复 6 处后台直接碰控件树/终端**：
  - `ModelPicker.RunImport`：后台线程写 help.Text/MarkDirty → `PostToUI`（**修复本地导入花屏**，导入完成表格即时刷新）
  - `TodoTool.RefreshSidebar`：agent 线程调 RefreshSidePanel → `PostToUI`
  - `ShowPermissionDialog` / `ShowPlanApproval`：agent 线程 ShowWindow（改窗口栈）→ `PostToUI`
  - `UpdateChecker` 后台续延：线程池线程 AddSystemMsg → `PostToUI`
  - `ShowToast` 自动关闭：后台 ContinueWith CloseWindow → `PostToUI`
  - `FallbackLLM`：agent 线程 9 处 Console.Error 写终端 → `WriteFallback`（TUI 活跃时抑制，仅非 TUI 输出）
- **审计确认安全**：Agent 流式回调（Route/onToken 已 PostToUI）、TuiToastQueue（数据锁）、EditorCore 脏标记、WatchMode（队列轮询）、日志系统（默认未启用）均不涉及后台直接控 UI

### ✅ 验证
- 主项目 + GUI `dotnet build` 0 错误；自测 4122/4123（唯一失败为环境相关的 minimax 本地 providers.json 覆盖）

---

## v0.83.0 (2026-08-22) — 模式体系 v3：行为轴三档（Chat/Plan/Build）落地

- **工作模式三档**（行为轴）：删 Review 与 Auto，改为 `🔨 Build（建造）/ 🧠 Plan（规划）/ 💬 Chat（聊天）`；Shift+Tab / Ctrl+K 循环 `Build→Plan→Chat`
  - **Chat 聊天**：0 工具 + 0 系统提示词，每轮只剩 user/assistant 消息（token≈0）；不注入 system 消息、不传工具 schema、首条回复即完成（不被「请立即调用工具」误催）
  - **Plan 规划**：只读白名单 `PlanReadOnlyTools`（read_file/glob/grep/ls/tree/stat/wc/pwd/diff/doc/web_search/fetch/lsp/lint/skill/memory/transcribe/view_image/ask_user_question/job_output/ps/todo/**bash**，bash 运行时按 `BashGuard.IsSafeReadOnly` 门控只读命令）+ 精简提示词 `GeneratePlan`（~460 字符，完整版 27K）——schema 层硬过滤，写工具不进 schema
  - **Build 建造**：工具与提示词受经济模式管理（TrimToolsForEconomy + SystemPrompt 档位），现状不变
- **TINY 并入 Chat**：`PermissionManager.Mode` 删除 TINY（只剩 Ask/Auto/SmartAuto/Yolo）；`--permit tiny/chat`、`/perm tiny/chat`、Web/GUI `/perm` 路由到 Chat 工作模式
- **权限 AUTO 改必问**：去掉「首次确认后会话记忆」，只读工具放行、危险/修改操作逐次确认（≈Ask）；`AutoAllowed` 保留给 SmartAuto Cautious
- **修复 P0-1**：`/mode`、`/permit`、Ctrl+P、计划审批门批准等入口统一刷新工具集+提示词（`ModeChanged` 处理器 + `Program.RefreshActiveSlotTools` + `WireSlotWorkMode` 回调）
- **修复 ReapplyToolFilter 缺陷**：Agent 保存 `_allTools` 原始集，重滤从全集进行——经济精简删掉的工具（如 grep）在切 Plan 时恢复
- `--sysprompt-size` 新增 Chat（0/0）与 Plan（23 工具 + GeneratePlan）对照行
- docs/模式体系.md v3 重写、使用手册/CLAUDE.md/README 同步；自测新增 Plan 白名单 / Chat 0 工具 / 权限四档 / Auto 改必问断言（4122/4123 通过）

### ✅ 验证
- 全项目 `dotnet build -c Debug` 0 错误（主项目 + GUI）；自测 4122/4123（唯一失败为环境相关的 minimax 本地 providers.json 覆盖，与本次无关）；`--sysprompt-size` 输出 Chat/Plan 行尺寸正确

---

## v0.82.3 (2026-08-22) — 模式体系 v2：参考竞品（Claude Code / Codex / Crush / Aider）四轴正交划分

- **四正交轴模型**（对齐 Codex 双轴 `sandbox_mode × approval_policy` + Claude Code `plan` mode 语义）：

  | 轴 | 决定什么 | WayCoder 映射 | 竞品参照 |
  |---|---|---|---|
  | 确认轴 | 何时打断确认 | 权限模式 Ask/Auto/SmartAuto/Yolo | Codex approval_policy |
  | 边界轴 | 能碰什么（可写/网络） | 沙箱 SandboxManager | Codex sandbox_mode |
  | 行为轴 | 干什么活 + 工具集 | 工作模式 Build/Plan/Review | Claude plan（只读规划） |
  | 省钱轴 | 花多少 token | 经济模式（提示词+压缩+输出） | Aider/Crush（模型选择+压缩） |

- **竞品共识：省钱不删工具**——四大竞品（Claude/Aider/Crush/Codex）省钱全靠模型选择 + 上下文压缩 + 提示词精简，无一家靠删工具；WayCoder 经济模式含 `TrimToolsForEconomy` 属偏离共识特色，文档标注（§6-P1-⑤），建议移出或显式化
- **Codex 双轴启示**：沙箱（边界）与确认（approval）应解耦——WayCoder 现状 `full-auto→Yolo` 联动待拆（§6-P0-②）
- **TINY 纯聊天**：重新定义——从权限枚举降级为「组合预设」（边界=read-only + 行为=聊天），非确认轴职责
- **docs/模式体系.md v2** 全重写（新增 §0 竞品对标 + 四轴模型 + 修订决策链/命名对照/问题清单）；CLAUDE.md 摘要更新

### ✅ 验证
- 竞品设计来自官方文档调研（code.claude.com / openai/codex docs / Aider 文档）；文档代码引用沿用 v0.82.2 已核对结果；纯文档改动

## v0.82.2 (2026-08-22) — 模式职责划分文档化：权限/工作/经济三正交维度理清边界

- **新增 [docs/模式体系.md](docs/模式体系.md)**：三分钟看懂三类模式——权限管「要不要确认」、工作管「工具有没有 + 干什么活」、经济管「省多少 token」
- **职责边界表**：每维度「归它管 / 明确不归它管 / 现状违规点」（TINY 禁工具+清提示词、YOLO 换白名单、经济悄悄删工具为三处越界）
- **单一决策链**：工具有没有 = 白名单 > 工作模式 AllowList > 黑名单 > 全量；确认只看权限；省钱只看经济
- **命名对照表**：消除同名异义（Auto×5：工作/权限/权限Smart/经济/沙箱；权限 TINY vs 窗口 `--tiny`）
- **已知问题清单 P0/P1/P2**（本次不修，标注影响 + 修复方向）：ReapplyToolFilter 仅 Shift+Tab 刷新、YOLO 受经济裁剪、WorkMode.Auto 名不副实、`--permission-mode plan` 静默忽略、Review schema 浪费、危险工具数据集 5 处不一致、AllowedTools 死代码等
- **CLAUDE.md 新增「模式体系（三分钟版）」摘要节**，指向 docs/模式体系.md

### ✅ 验证
- 文档中代码引用与源码逐项核对（枚举/FilterTools/SystemPrompt 分派/ReapplyToolFilter 调用点全部吻合）；纯文档改动，无需编译

## v0.82.1 (2026-08-22) — 纯聊天 TINY 模式纳入省钱体系对照（无工具 + 无提示词）

- **纯聊天模式 = 权限极简 TINY**（`--permit tiny` / `/permit tiny`）：`FilterTools` 直接 `return []`（无任何工具）+ `Agent` 系统提示词置空（不调 `SystemPrompt.Generate`）——每轮只剩用户/助手消息，**工具 0 + 提示词 0，token 开销≈0**
- **`--sysprompt-size` 增加纯聊天对照行**：完整省钱谱系一览（Off ≈17,229 → Auto ≈15,833 → On ≈7,487 → Extreme ≈1,714 → **TINY 纯聊天 ≈0**）
- 与省钱工具精简正交：TINY 是权限维度（禁用全部工具），EconomyMode 是经济维度（按档位去重复精简）

### ✅ 验证
- 自测 4106 通过（0 失败）；Release 构建通过

## v0.82.0 (2026-08-22) — 省钱模式工具精简：关=全量，开=去重复，开得越大越精简

- **省钱模式按档位精简工具集**（设计原则：省钱关 = 全量 46 工具；开 = 去掉重复（bash 可替代）；开的越大，工具越精简）：

  | 档位 | 工具数 | 精简逻辑 |
  |---|---|---|
  | Off 关闭 | 46 | 全量 |
  | Auto 自动 | 34 | 去掉 12 个 bash 基础命令可替代的重复工具（cd/pwd/ls/mkdir/cp/mv/rm/tree/wc/stat/ps/kill） |
  | On 开启 | 29 | 再去掉 5 个搜索/编辑冗余（glob/grep/diff/find_replace/multiedit） |
  | Extreme 极致 | 7 | 只留读改写 + 执行 + 联网 + 问用户核心（read/write/edit/bash/web_search/fetch/ask_user_question） |

- **白名单优先**：配置了显式白名单（WAYCODER_ALLOWED_TOOLS / Plan/Build/YOLO 工具集）时完全按白名单执行，省钱精简不干扰；黑名单始终叠加
- **实测每轮 token 开销**（`--sysprompt-size` 完整矩阵）：Off ≈17,260 → Auto ≈15,864（↓8%）→ On ≈7,487（↓57%）→ Extreme ≈1,714（**↓90%**）
- **Extreme 定稿 7 工具**：读改写（read_file/write_file/edit_file）+ 执行（bash）+ 联网查/下载（web_search/fetch）+ 问用户（ask_user_question）——完整能力最小集，写代码可自测、可 git
- **`--sysprompt-size` 升级**：从 normal/extreme 两行扩展为完整矩阵（4 提示词档 × 各档工具数 + 白名单对照），一行看清提示词量 + 工具 schema 双维度
- 新增自测 `TestEconomyToolTrim`：断言工具数 46>34>29>7 单调递减 + 各档精确数量 + Extreme 核心集一致性

### ✅ 验证
- 自测 4106 通过（0 失败）；Release 构建通过

## v0.81.13 (2026-08-22) — 省钱模式实测验证：extreme 比 auto 省一半 token

- **实测对比**（写一万行贪吃蛇游戏，minimax-m3，白名单 5 工具）：

  | 指标 | auto | extreme | 下降 |
  |---|---|---|---|
  | 总 token | 8,012,888 | 4,168,891 | ↓48% |
  | 输入 | 7,902,458 | 4,071,813 | ↓48% |
  | 耗时 | 37.1 min | 26 min | ↓30% |
  | 代码行 | 10,035 | 10,353 | 相当 |

- 花费估算（minimax-m3 长上下文价）：extreme ≈ $2.7（¥19）/ auto ≈ $5.0（¥36），**约省一半**
- 省钱来源：extreme 最小提示词（265 字符）+ 工具白名单 → 每轮固定开销大幅下降、上下文更小处理更快
- 配置优先级：config.json 为权威源，环境变量不覆盖；测试 extreme 走 CLI `--economy extreme`

### ✅ 验证
- 自测 4106 通过（0 失败）；GUI Release 构建通过

## v0.81.12 (2026-08-22) — Extreme 省钱模式保留白名单工具（可干活）

- **修复 Extreme 禁用所有工具**：`FilterTools` 此前 `EconomyMode.Extreme` 时 `return []`
  （极致省钱 = 纯聊天，写不了代码）。改为 Extreme 只最小化提示词（GenerateExtreme），
  工具保留白名单/黑名单过滤（`WAYCODER_ALLOWED_TOOLS` 等）——极致省钱仍可干活
- 仅权限 TINY（纯聊天）禁用所有工具
- 配置优先级确认：config.json 为权威源，环境变量不覆盖（测试 extreme 走 CLI `--economy` 或改 config）
- 验证：`--economy extreme` + 白名单(bash/read_file) 成功写文件（token 68K 小任务）

### ✅ 验证
- 自测 4106 通过（0 失败）；GUI Release 构建通过

## v0.81.11 (2026-08-22) — 排队状态显示在动态栏 + 省钱模式验证命令

- **排队状态显示在动态栏**：Agent 忙碌且有待处理指令时，动态栏显示 `⏳排队N`
  （N=队列中待执行指令数，随任务完成递减），不弹聊天区——主循环每轮按队列实际待处理数
  更新 `SetQueuedCount`，`SyncDynamicBar` 排队优先分支渲染；排队数是背景状态走动态栏，
  聊天区保持干净
- **`--sysprompt-size` 命令**：对比各模式 SystemPrompt + 工具 schema 大小（省钱模式效果验证）
  ——测试结果：normal 全工具 ≈17K tok → **extreme + 白名单5工具 ≈1.1K tok（↓15 倍）**

### ✅ 验证
- 自测 4106 通过（0 失败）；GUI Release 构建通过
- 按键脚本：10 任务顺序执行（连接数递增 2s/个），动态栏排队数 9→8→5→2→1 递减

## v0.81.10 (2026-08-22) — 输入排队机制（三端）：Agent 忙碌时不打断，排队等批次完成后自动取指令

- **TUI**：主循环处理提交队列改用 `TryPeek`——Agent 忙时普通对话留在队列不取走
  （斜杠命令即时处理），等当前槽位批次完成后继续取；`StartSlotTask` 忙时不再拒绝，
  改为入队 + 提示「⏳ 指令已排队，当前批次完成后自动执行」
- **Web**：`WebSlot.PendingInputs` 待处理队列——忙时输入入队，Agent 批次完成
  （finally 置 IsBusy=false）后 `TryStartNextPending` 自动取下一个执行
- **GUI**：每槽位 `_pendingInputs` 队列——`SendAsync` 忙时入队 + 提示，
  批次完成 finally 后 `TrySendNextPending` 取下一个自动执行
- 此前行为：Agent 忙时提交被拒绝（TUI/Web 提示等待、GUI 直接丢弃输入）

### ✅ 验证
- 自测 4106 通过（0 失败）；主项目 Debug + GUI Release 构建通过

## v0.81.9 (2026-08-21) — 状态栏大/小模型用量+花费 + 压缩状态动态栏 + 快捷键栏居中

- **状态栏右侧显示大/小模型上下文用量 + 累计花费**：LLM 分模型统计 token
  （`EffectiveModel==SmallModel` 即压缩等小模型调用计入小模型，否则计入大模型）；
  `UpdateTokenDisplayFull` 改为 `📊 大:{L} · 小:{S} · ¥{cost} · {latency}s`
- **快捷键栏文字居中**：模式栏下方快捷键行居中显示（合并切换 + 基础操作快捷键）
- **压缩上下文状态只在动态栏显示**：移除 Agent 压缩时注入聊天流的
  `⏳ 上下文压缩中...` / `🔄 上下文已自动压缩` 消息——压缩是背景状态不是对话内容，
  动态栏（CompressProgress → TuiDynamicBar 压缩进度）已承载显示，聊天区保持干净
- 自测：StatusRight 含大/小模型用量断言；chat.tui shortcutRow 居中断言

### ✅ 验证
- 自测 4106 通过（0 失败）；GUI Release 构建通过

## v0.81.8 (2026-08-21) — 模式栏/快捷键行居中 + Ctrl+X 完整交换

- **模式栏/快捷键行居中**：TuiMarkup 的 `align` 属性此前只对 `TuiLabel` 生效，
  `TuiSmartLabel`（chat.tui 的 modelInfoRow/shortcutRow）未应用 → 靠左显示；
  现在 Label 与 SmartLabel 都支持 align，模式栏/快捷键行按 `align="center"` 居中
- **修复 align 解析卡死**：`Attr("align")` 对缺失属性返回 null，无条件 `Enum.TryParse(null)`
  抛 ArgumentNullException 使标记加载中断（TuiAudit 卡死）——判空 + 仅 Label/SmartLabel 处理
- **Ctrl+X 交换大/小模型整套**：模型 id + 服务商 + 网关 + KEY 一起交换
  （key 是服务商级，交换服务商后自动取 store 对应 key），且运行时真正生效——
  更新当前槽位 Agent 的 LLM 实例 + 上下文窗口（此前只改配置/模型栏，「仅仅交换了显示」，
  RunSlotAgentAsync 用的是 agent.LlmClient 旧实例）
- 自测：chat.tui modelInfoRow/shortcutRow 居中断言、子节点数适配

### ✅ 验证
- 自测 4105 通过（0 失败）；GUI Release 构建通过

## v0.81.7 (2026-08-21) — 模式栏下方独立快捷键行 + 状态栏回归干净

- **模式栏（模型信息行）下方插入独立快捷键行**：只显示切换快捷键
  `Shift+Tab 模式 · Ctrl+P 权限 · Ctrl+E 经济 · Ctrl+X 换模型`（TuiSmartLabel 淡灰渲染），
  可见性跟随模式栏；chat.tui 与代码版 ChatScreen 两套布局都加
- **状态栏回归干净**：切换快捷键从状态栏 HintText 移除，恢复基础操作提示
  （Enter 发送 · ↑↓ 历史 · Tab 补全 · F1-F10 槽位 · Ctrl+H 帮助）
- 布局动态计算：模式栏区域占行 = 模式栏 + 快捷键行 + 分隔空行（按实际控件存在与否算，chat.tui 版无 spacer）
- 自测：chat.tui 子节点数/新 shortcutRow 断言适配

### ✅ 验证
- 自测 4103 通过（0 失败）；GUI Release 构建通过；TuiAudit 帧确认快捷键行/模型栏彩色/状态栏提示均渲染

## v0.81.6 (2026-08-21) — 模型栏切换残留修复 + 状态栏显示切换快捷键

- **修复模型栏切换残留**：TuiSmartLabel 渲染前先清整行（RenderBuffer.Fill 背景空格铺满）——
  切换后文本变短时旧字符残留在最右侧（此前只覆盖新文本不清理旧内容）
- **状态栏显示切换快捷键**：底部状态栏 HintText 优先展示 `Shift+Tab 模式 · Ctrl+P 权限 ·
  Ctrl+E 经济 · Ctrl+X 换模型`（右侧 busy/token 会挤占尾部，重要提示放前；完整清单走 Ctrl+H 帮助面板）

### ✅ 验证
- 自测 4102 通过（0 失败）；GUI Release 构建通过

## v0.81.5 (2026-08-21) — 模型栏彩色显示 + 状态切换快捷键 + TuiSmartLabel

- **模型信息行（状态栏上方）彩色加亮**：新增 `TuiSmartLabel` 控件（支持 `«tag»…«/»` 分段着色，
  颜色真源 MarkdownParser），模型栏改用它——标签暗、值亮/彩（权限/工作模式/经济模式按模式着色、
  大/小模型名加粗白色），不再整行灰暗看不清；下方插入空行与状态栏分隔
- **修复模型栏切换后不刷新**：`SetModelInfoRow` 文本更新时未 `MarkDirty`，增量渲染不重绘该行，
  切换模型后状态栏上方一直显示旧模型（标签只改 Text 不标脏）
- **状态切换快捷键（TUI）**：Ctrl+P 循环权限模式（问答→自动→智能→畅通→极简）、
  Ctrl+E 循环经济模式（关闭→自动→开启→极致）、Ctrl+X 交换当前槽位大/小模型；
  工作模式切换沿用 Shift+Tab / Ctrl+K
- **TuiMarkup 新增 SmartLabel 标签**：chat.tui 的 modelInfoRow 改用 `<SmartLabel>`
- 自测：ParseMarkupOnly 分段着色断言、chat.tui modelInfoRow 断言适配

### ✅ 验证
- 自测 4102 通过（0 失败）；GUI Release 构建通过

## v0.81.4 (2026-08-21) — 多选框列表溢出修复 + 在线导入不因 key 阻塞

- **多选框列表溢出下边框修复**：`TuiDialog.Select`/`MultiSelect` 把列表高度改成可见项数（≤12）后，**窗口高度未重算**——选项多（如在线导入 8 源）时列表底部跑出下边框。修复：
  - 改高列表后按内容重算窗口高度（`FitWindowToContent`）
  - 可见项数受屏幕可用高度钳制（`min(项数, 屏幕行-4/-6)`），小终端不超屏
- **在线导入不再被 API Key 阻塞**：`ImportOnline` 此前无 key 直接拒绝（`未找到 xxx 的 API Key`）——但 opencode go/zen 等端点 `/models` **公开**（HTTP 200 无需 key）。改为：
  - 无 key 也尝试拉取（不带 Authorization 头）；401/403 才提示需要/无效 key
  - 无 key 导入成功时结果标注「未配置 API Key，导入的模型需设 key 后使用」
- 自测：MultiSelect 8 项高度适配断言（列表高度/窗口容纳/不超屏）、OnlineSources 源存在断言

### ✅ 验证
- 自测 4099 通过（0 失败）；GUI Release 构建通过；opencode go/zen `/models` 无 key 实测 HTTP 200

## v0.81.3 (2026-08-21) — 设置对话框排查 + YOLO diff 自动放行显示对比

- **设置界面输入框改单行**：text/number/secret 设置项编辑改用 `TuiDialog.InputLine`（单行输入框，回车=确定）——设置值（模型名/超时秒数/密钥等）都是单行文本，多行输入框不适配
- **YOLO diff 自动放行 + 聊天区显示源码对比**：
  - `DiffPreview.Show` 入口加 YOLO 放行——畅通模式下写文件不弹 diff 确认窗（此前 Web/TUI 仍会弹）
  - 自动放行时**把 diff 渲染进工具输出**，聊天区气泡显示源码对比差异（红删绿增）——write_file/edit_file/multiedit 三工具、TUI/Web/GUI 三端统一（`RenderAsMarkup` 生成 `«red»/«green»` 中间格式标记，各端渲染器解析）
- **设置对话框构建巡检**：遍历配置 schema 92 个设置项，逐个构建+渲染其编辑对话框（select→Select / text/number/secret→InputLine），断言不崩溃、窗口不超屏——抓「某设置项弹框才炸」类问题
- 自测断言：YOLO 下 DiffPreview 直接接受全部变更、RenderAsMarkup 含红删绿增标记

### ✅ 验证
- 自测 4094 通过（0 失败）；GUI Release 构建通过

## v0.81.2 (2026-08-21) — 终端字符宽度实测探针 + 静态宽度表修复

- **终端宽度实测探针 `--width-probe [目录]`**：用「`+字符*` + CPR 光标位置查询」测量字符在当前终端字体下的真实显示列宽——
  - 无参：实测内置代表字符集（ProbeSet），与静态宽度表 `AnsiString.CharWidth` 逐项比对，输出 ✅/❌ 判定表
  - 带目录：扫描该目录源码中出现的**全部非 ASCII 字符**（排除 bin/obj/.git，按首次出现去重），逐个实测，列出不一致项 + 按宽度的字符分布——「检查整个程序所有字符宽度」
  - Windows 走 `Console.CursorLeft`，Unix/macOS/Linux 走 CPR + libc termios
- **`TerminalRawMode`**：libc termios P/Invoke（`cfmakeraw` 设 raw + `poll`/`read` 直连 fd0 读原始字节）——`Console.ReadKey` 会把 CPR 回复的 `\x1b` 当 escape 前缀吞掉、`Console.OpenStandardInput` 与手动 raw 不兼容，均不可用，实测踩坑后改用 libc 原始读取
- **修静态宽度表**：漏覆盖的 emoji 由 1 列改 2 列——媒体控制/时钟 emoji `⏩⏪⏫⏬⏭⏮⏯⏰⏱⏲⏳`（23E9-23F3，全 Emoji_Presentation）、杂项符号与箭头 `⬛⬜⭐⭕⬆⬇⬅`（2B00-2BFF，EA=W/Emoji_Presentation）
- **自测新增 [终端实测宽度] 段**：源码字符扫描纯逻辑测试（始终执行）+ 终端实测校准（真实 TTY 才跑）
- 静态审计：交叉比对源码 1774 个非 ASCII 字符，定位并修复静态表遗漏（⏰⏳⭐ 等 EA=W 误判 1 列）

### ✅ 验证
- 假终端（pty + CPR 模拟）全链路验证通过：协议、raw 读取、解析、报告正常
- 自测 4089 通过（0 失败）；主项目 Release 构建通过；GUI Release 构建通过；AOT publish 验证中

## v0.81.1 (2026-08-21) — Web diff 窗口增强 + YOLO 无问答阻止 + 回车不停止任务

- **Web diff 窗口**：加宽（`min(94vw, 980px)`）+ 显示行号（旧/新行号列，对齐等宽字体）；内容超屏时**底部按钮固定可见**（吸底，不随内容滚出屏幕）
- **YOLO（畅通）无任何问答阻止**：`ask_user_question` 在 YOLO 下自动选第一个选项 / 文本留空，不弹框——Web / TUI / GUI 三端统一
- **Web 回车不再停止任务**：回车只发送；停止仅由 ⏹ 按钮触发（此前任务运行时回车会调 /interrupt 停止）
- **修自测**：搜索过滤列序对齐（状态列位置）、ProviderGroupName 连字符规范化断言

### ✅ 验证
- 自测 4085 通过（0 失败）；GUI 构建通过

## v0.81.0 (2026-08-21) — 提示栏改造 + Diff 深色配色 + 宽度统一 + 模型选择器交互 + 自测计时

### 提示栏（命令补全）改造
- **只显示命令名**：斜杠命令补全不再显示冗长子参数（`[all|tui]`/`<name> [key]` 等），描述统一按对齐列排布
- **收起不残留**：修复提示栏收起后聊天区花屏（`TuiListView` 未在自身脏时重绘背景）——显示/隐藏都标脏聊天列表强制整区重绘
- **行数溢出修复**：提示栏条目少时不再把空行/底边框画到控件下方（盖住动态栏/分隔线）
- **发送即消失**：提交消息时提示栏必定隐藏；侧栏可见时提示栏只做聊天列表一样宽（收起只需刷新聊天区）

### Diff 对话框
- **代码背景默认黑**；删除行黑带一点点红、添加行黑带一点点绿（TrueColor 深色调，替代过饱和 256 色）
- **行号前缀**：删除行白带一点点红、添加行白带一点点绿
- **打钩不错位**：`✓` 宽度修正为 1 列（此前落进 0x2600-0x27BF 宽区间被算成 2）+ 前缀按宽度函数补齐

### 字符串宽度统一（全仓唯一真源）
- `AnsiString.CharWidth` 显式标定常用符号：`✓✗✔✘` 按 1 列、箭头/几何图形/圆点/`⌘` 1 列、`⏱` 2 列（emoji 展示）
- **设置分类图标对齐**：`🎙⚙⏱` 加 VS16（U+FE0F）强制 emoji 2 列，修复「语音/参数/超时」与其他分类宽度不一致、文字错位

### 模型选择器
- **Enter = 确认 + 保存关闭**：返回当前选中的 `providerId + modelId`（原来 Enter 只应用不关闭、保存按钮返回 null）；空格仍为「预览不关」
- **无 key 模型**：Enter 先弹 API Key 输入框，保存后自动应用并关闭
- **勾选唯一**：大/小模型 ✓ 按 (id, 网关) 匹配，修复同 id 多服务商（如 deepseek-v4-pro 分属 DeepSeek 与 OpenCode）出现 2 个 ✓
- **清空修复**：清空后重新读模型列表（表格真正清空）+ 窗口整棵 Invalidate 重绘（按钮/背景不被清掉）

### 自测计时 + 最慢项优化
- **计时报告**：每条 Check/Section 计时，跑完输出「最慢 Section / 最慢测试项」Top 榜，便于定位慢测试
- **性能优化**：web_search 测试改本地 mock（15.6s→毫秒级）、连通性探测并发化 + 网络失败不再试第二 URL（14.5s→4s）、LLM 重试退避基准可调、后台任务轮询替代固定 Sleep（1.5s→0.1s）

### 设置界面
- **状态栏提示文字居中**
- **切换分类底部残留修复**：`TuiScrollView` 内容变更时整区填充背景再重绘（仿 TuiListView），新类别条目少时旧内容不再残留

### 动画图标光标
- 动态栏 spinner / 动画文本直写屏幕前先 `CursorHide`，动画图标旁不再闪烁光标（EmitCursor 把光标恢复到输入区）

### ✅ 验证
- Release 编译 0 错误；`--test ui` 自测 1150 通过 0 失败（含新增宽度/提示栏/模型选择器相关断言）

## v0.80.1 (2026-08-21) — 模型唯一性按 baseUrl（地址不同=不同服务商）+ 内置 BaseUrl 修复

- **模型唯一键改 (id, baseUrl)**：同 id 不同网关地址的模型都保留，不再被后加载覆盖——如 `deepseek-v4-pro` 分属内置 DeepSeek（api.deepseek.com）与 OpenCode Go（opencode.ai/zen/go/v1）两组，可分别选择走各自网关
- **分组点选**：TUI ModelPicker 按服务商分组，选择时保存所选模型的 `DefaultBaseUrl` + `ProviderId` 到槽位/配置，请求走对应网关
- **新增 `Find(id, baseUrl)`**：按地址精确查模型；`Find(id)` 内置官方优先（兜底默认）
- **内置 BaseUrl 修复**：gemini 内置地址补 `/v1beta/openai`（OpenAI 兼容端点）；`LLM.ResolveApiEndpoint` 对 `/openai` 结尾去 `/v1` 前缀，避免 `v1/v1` 重复
- **导入去重按 (id, baseUrl)**；跳过内置仅当同 id 同地址

### ✅ 验证
- Release 编译 0 错误；config 自测 86 通过；system 自测 542 通过

## v0.80.0 (2026-08-21) — 全局 config.json 配置架构 + --help 全面格式化 + 槽位模型修复

### 配置架构：全局 config.json
- **`~/.waycoder/config.json` 成为配置权威源**：保存全部配置（Key 格式），优先级 config.json > .env > 环境变量
- **.env 精简为 5 项基本引导配置**：服务商 / 地址 / API_KEY / 经济模式 / 鼠标（无 config.json 时兜底）
- **首次启动自动迁移**：无 config.json 且存在 .env 时自动生成并精简 .env
- **每次启动本地备份**：config.json 有更新则同步一份到项目 `.waycoder/config.json`（先验证文件正常才备份）

### 槽位模型不一致修复
- **回退链用槽位实际模型**：`BuildFallbackChain` 参数化，不再用全局 .env 模型——修复「状态栏 deepseek-v4-pro 却显示 mimo-v2.5 失败」
- **`GetSlotLlm` 复用防污染**（回退残留模型不再污染下次请求）+ 新增 `AgentSlotConfig.ResolveEffectiveModel` 统一槽位解析（槽位优先 → .env 回退 → 写回）

### --help 全面格式化
- **线框横幅**：┌─┐│ 框住应用名 + 公司名 + 版本号（居中/右对齐）
- **分类分组**：`---< 模型 >` 分隔线 + 标题上方空行
- **三列对齐**：短名 / 长名 / 说明列（CJK 宽度感知）
- **别名与子命令纵向列出**（暗灰），`-r, --resume` / `-c, --continue` 成组
- **可选值 `[x]` / 必填值 `<x>`**，示例注释纵向对齐
- **清除 "Claude Code" 品牌字样**

### 其他
- 设置界面不再显示环境变量名（config.json 权威后大部分 WAYCODER_* 作废）
- 导入配置写入 config.json + .env 5 项
- Global 区分公司名 / 作者

### ✅ 验证
- Release 编译 0 错误；config 模块自测 86 通过（0 失败）；system 模块 537 通过（5 项 [项目检测] 环境性失败，与基线一致非回归）

## v0.79.95 (2026-08-21) — 导入 key 过滤 $变量伪 key

- `ImportFromKnownSources` 从配置文件（Claude Code / Codex / Cursor / OpenCode）导入 key 时，跳过 `$VAR` / `${VAR}` 形式的环境变量引用伪 key（非真实字面 key）
- 自测 4073 通过（0 失败）

## v0.79.94 (2026-08-21) — 本地导入不再自动同步 API Key

- **本地导入只导入模型**：API Key 仅由 `~/.waycoder/api_keys.json` + 环境变量决定，不再从 Claude Code / Codex 等导入来源文件自动同步 key（避免「导入本地模型后模型列表冒出 OPENAI 等无关 key」）
- Web / TUI / GUI 三处本地导入逻辑统一移除 `ImportFromKnownSources` 自动调用；模型列表的 🔑 只反映已保存 key 与环境变量

### ✅ 验证
- 自测 4071 通过（0 失败）

## v0.79.93 (2026-08-21) — GUI 模型窗口对齐 Web/TUI

- **GUI 模型窗口按钮对齐**：本地导入 / 在线导入分离；本地导入弹来源勾选（内置模型 / Claude Code / Codex / OpenCode / Crush / OpenClaw，默认全选）；在线导入可选 OpenCode Go / Zen（不同地址）；新增「清空」按钮（清空内置目录 + 自定义模型，可重新导入）
- **GUI 模型列表加状态列**：每模型显示 无key / 连通 / 欠费 / 不通 / 未测，与 Web/TUI 状态列一致（仅显示不落盘，扫描结果推导）
- **修复 GUI 编译**：排除 `SnakeGame/` 误置目录（主项目 v0.79.90 已排除，GUI csproj 未同步）+ 补 `Program.InAgentRenderLoop` 占位
- GUI 走 Avalonia 多选/单选/确认对话框（对齐 Web 勾选交互）

### ✅ 验证
- 主项目自测 4071 通过（0 失败）；GUI 构建通过

## v0.79.92 (2026-08-21) — OpenCode 分 Go/Zen 服务商 + providers.json 服务商数据库

- **OpenCode 分两个服务商**：`opencode-go`（`https://opencode.ai/zen/go/v1`，订阅制）与 `opencode-zen`（`https://opencode.ai/zen/v1`，按量付费），各自保留地址
- **在线导入可选服务商**：Web 与 TUI 在线导入时弹框选择 OpenCode Go / Zen，用对应地址拉取模型并导入；导入的模型按实际地址归类（zen/go/v1 → opencode-go，zen/v1 → opencode-zen）
- **providers.json 服务商数据库**：首次运行生成 `~/.waycoder/providers.json`（服务商 id / name / base_url），用户可编辑扩展服务商；代码从它加载并按 base_url 识别服务商

### ✅ 验证
- 自测 4071 通过（0 失败）

## v0.79.91 (2026-08-21) — 模型管理大升级 + Diff 预览简化 + Web 端多项修复

### 模型选择对话框
- **搜索过滤修复**：Refresh 改数据后标脏窗口根视图——此前增量渲染只画脏控件，表格被跳过，输入过滤词列表「不动」
- **状态列**：每模型显示 连通 / 无key / 欠费(402) / key无效 / 不通 / 未测，仅显示不落盘（扫描结果推导）；组头聚合状态（💸欠费等）
- **数据行去列间竖线**（表头保留），列边界清爽；两行按钮加空行 + flex 等宽铺满（窄窗口不溢出）
- **清空按钮**：清空全部模型（内置目录标记隐藏 + 删除自定义模型），可清空后重新导入；内置可经「本地导入→内置模型」恢复
- **本地/在线导入分离**：本地导入弹勾选框选择来源（内置 / Claude Code / Codex / OpenCode / Crush / OpenClaw，默认全选、空格勾选），并同步导入已知软件 API Key
- **导入按服务地址归类**：`InferProviderFromBaseUrl`——opencode 网关提供的 deepseek-v4-flash 归 opencode（而非 deepseek）；本地导入同样适用
- **嵌套对话框 ESC 返回时刷新父级**底部与整体（RefreshParent）

### Diff 预览
- **Y 立即生效**：接受当前 hunk 并自动前进、状态栏计数、已接受行符号位打 ✓（此前按 Y 无反馈，误以为要确认两遍）
- **按钮快捷键直接触发**：移除重复的窗口级 RegisterShortcut，Y/N/A/Q 即按钮快捷键
- **简化**：移除冗余状态提示行与 Enter 提交；A 直接全接受；焦点在按钮上按 Enter 触发按钮（不被窗口快捷键抢走）

### 渲染 / 控件
- **新增 TuiSpace 空白占位控件**（布局留白，不渲染不聚焦）
- **聊天代码块 4 反引号围栏修复**：````js 语言标签不再残留多余反引号（此前显示「`js」）
- **聊天输入框代码语法高亮**：`Syntax.Detect` 内容启发式检测语言，多色渲染粘贴/输入的代码

### Web 端
- 模型对话框**状态文字单独一行**（不再挤在按钮行显示太窄）
- **源码气泡 flex 收缩修复**：overflow 元素不被压成一小条（`flex-shrink:0`）
- 导入按钮改名：**本地导入 / 在线导入**；本地导入来源勾选 + 清空按钮

### ✅ 验证
- 自测 4070 通过（0 失败）

## v0.79.90 (2026-08-20) — 修复 SnakeGame 误置源码树致编译失败

- **修复**：`WayCoder/SnakeGame/`（独立控制台游戏，自带 `.csproj` + `Main`）误置于本源码树内，其 `obj/Debug`、`obj/Release` 生成的 `AssemblyInfo.cs` 被主项目默认 `**/*.cs` glob 编入 → `CS0579` 特性重复，Release/Debug 均编译失败。已从主项目编译中排除（`Compile/None Remove="SnakeGame/**/*.cs"`），SnakeGame 仍可 `dotnet run --project WayCoder/SnakeGame` 独立运行

### ✅ 验证
- Release / Debug 构建 0 错误通过

## v0.79.89 (2026-08-20) — 聊天显示裁剪 + 代码预览封顶

- **聊天显示裁剪**：超过 `MaxChatMessages`（默认 1000，范围 100~10000）自动丢弃最旧消息——仅裁显示层（ChatMessages/ChatList），Agent 的 Messages 与会话文件不受影响（恢复会话时按 Agent 消息重建），万级消息下列表不再越滚越慢
- **代码块预览封顶**：超过 `MaxCodePreviewLines`（默认 500，范围 10~1000）保留头尾（60%/尾）、中间折叠「省略 N 行」标记
- **设置可改**：两项均加入设置界面（`Ctrl+T` → 系统分类）与环境变量 `WAYCODER_MAX_CHAT_MESSAGES` / `WAYCODER_MAX_CODE_LINES`
- **压力测试**（`WAYCODER_STRESS=1 waycoder --test`）：1 万条混合消息 + 500 行代码块 + 5000 字符超长文本（25s 无崩溃）、10 槽并发写入隔离、Web 30 并发 GET 无 5xx；非法格式（未闭合代码块/畸形表格/ANSI/NUL/emoji 代理对/超长单行）渲染+滚动不崩溃

### ✅ 验证
- 自测 3995 通过（0 真实失败）

## v0.79.88 (2026-08-20) — 修复长消息折叠致无法滚屏

> 含远程同步的修复：在 home 目录启动 TUI 卡死（RepoMap/项目扫描限制）、macOS Terminal.app 鼠标协议兼容（降级基础鼠标 + 禁用 Kitty 键盘协议）。

- **根因**：`MarkdownParser` 段落构建用空格连接多行（`paraSb.Append(' ')`）→ 多行消息的换行丢失，被当作一个长段落折行 → **行数塌缩**（60 行 → 14 行）→ 条目高度不足 → 内容超过屏幕时**滚不动/像卡住**（用户反馈「聊天贴的代码超过屏幕卡住无法滚屏」）
- **修复**：
  - 段落构建改为保留换行（`Append('\n')`）
  - `TuiMarkdown.RenderParagraph` 逐行处理（每行各自解析内联 + 折行）
  - `WrapText` 保留原始换行（先按 `\n` 拆行再各自折行）
- **新增自测**：多行纯文本 60 行 → 渲染 60 行、条目高度随正文增长、滚屏后帧变化；代码块 62 行

### ✅ 验证
- 自测 3977 通过（0 真实失败）

## v0.79.85 (2026-08-20) — 能力验证：10K 行游戏自主开发 + 自主修复

**稳定性/自主能力里程碑验证**：

- **WayCoder 自主开发 10,428 行网页游戏**（`/d/code-agents/game-test`，22 文件，类吸血鬼幸存者）：
  - 全程 ~40 分钟运行，**无卡死、无异常**（错误日志 0 条），验证本轮全部稳定性修复（进程管道超时/锁/渲染兜底/并发）
  - 模块化结构：combat/ui/entities/ai/levels/items/skills/shop/weather/bestiary/elites/tutorial/stats/save…
  - 自主写 `smoke-test.js` 并运行通过（SMOKE TEST PASSED）
- **WayCoder 自主诊断并修复游戏 bug**（「点不进去」= 主菜单点击/按键失效）：
  - 根因：`engine.js` 输入边沿检测时序错误——浏览器事件先于 rAF 帧执行，`prevDown` 快照已同步，`mousePressed()/keyPressed()` 恒 false
  - 修复：改事件驱动「按下标志 + 消费即清除 + 帧末防残留」，移除失效快照
  - 验证：事件时序模拟 + `smoke-test.js` PASS，无回归
- 闭环验证了「写码 → 用户发现 bug → 自主诊断修复」的完整自愈能力

### ✅ 本轮会话累计（v0.79.77→85）
- 卡死/崩溃排查修复（进程管道超时、锁超时、渲染兜底）
- 并发竞态（McpManager 列表、/loop /plan IsBusy、子智能体 cd）
- 资源泄漏/孤儿进程/静默失败
- 重复代码提炼合并（FileLock/截断/错误前缀/渲染器/ProcRunner）
- 提示注入防护、Web 竞态、行尾根治（.gitattributes）、AllTools 缓存

## v0.79.84 (2026-08-20) — 优化批次：行尾根治 + 缓存 + 注入防护 + Web 竞态

- **`.gitattributes` 根治 CRLF 行尾噪音**：`* text=auto`（.bat/.ps1/.cmd 保持 CRLF），此后编辑工具写 LF 不再产生整文件行尾 diff
- **`AllTools` 缓存**：Agent 构造/10 槽位/子智能体频繁访问，改为缓存（MCP 工具变更 + 插件注册/卸载时失效）
- **提示注入防护**：新增 `PromptInjection` 检测器，read_file 读取内容含「忽略之前指令/你现在是…」等注入模式时附加安全警告（把内容当数据处理）
- **Web 换模型/存 key 竞态修复**：WebSlot 加 `RunningTask`，/model 与 /model 命令在 Reconfigure 前 `WaitForSlotIdleAsync` 等退场 ChatAsync 收尾（此前中断后立即改配置可能发「新 key+旧端点」）

### ✅ 验证
- 自测 3970 通过（0 真实失败）

## v0.79.83 (2026-08-20) — ProcRunner 统一进程运行

- **新增 `ProcUtil.RunAsync(psi, timeoutMs, ct)`**：统一「启动进程 + 并发读 stdout/stderr + 等退出（带超时）+ 超时杀进程树 + 读取兜底」，消除各工具重复样板与三种超时语义差异
- **应用到 5 个简单工具**：TestTool / SqliteTool / PsTool / KillTool / LintTool（各自保留自定义超时文案）；复杂工具（BashTool 流式/后台/沙箱、GitRunner、BatchRunner、BackgroundTask）保留定制逻辑（已做超时修复）

### ✅ 验证
- 自测 3970 通过（0 真实失败）

## v0.79.82 (2026-08-20) — 错误前缀统一 + 渲染器着色合并

- **工具错误前缀统一为「错误：」开头**：`cp/Git/ls/mv/rm/wc` 6 个工具此前返回 `"cp 错误：…"`（工具名开头），`ToolResultClassifier.IsError` 只认 `错误/Error/❌/失败` 前缀 → 这些失败被**误判为成功**（Agent 不注入重试提示）。统一后分类器全部识别
- **渲染器错误着色合并**：`AnsiTty` 新增 `ErrorBlock`（白字红底），6 个工具渲染器的 `FgBg(37,41)` 错误块与 `Fg(33)` 黄色「用户拒绝」分支统一走 `ErrorBlock`/`Warn`

### ✅ 验证
- 自测 3970 通过（0 真实失败）

## v0.79.81 (2026-08-20) — 重复代码提炼合并 + 管道超时全覆盖

按重复代码审查报告合并（消除约 15 处样板 + 统一语义）：

- **`FileLockManager.TryAcquireOrError`**：合并 4 处逐字相同的「文件被锁定」检查块（EditFile/MultiEdit/NotebookEdit/WriteFile）
- **`ContextManager.TruncateWithNotice`**：合并 4 处「截断于 N 字符」标记（OfficeExtractor×3 + LegacyOffice），统一 rune 安全截断 + 文案
- **进程读取超时全覆盖**：把 `ProcUtil.AwaitReadWithTimeoutAsync`（守护子进程继承管道防挂起）补到剩余 9 处裸 `await stdoutTask/stderrTask`（KillTool/LintTool/PsTool/SqliteTool/TestTool/Agent.Feedback/BackgroundTask×2/HooksManager）——消除同类挂起隐患在 Lint/进程/测试/hook 路径的遗漏

### ✅ 验证
- 自测 3970 通过（0 真实失败）
- 稳定性验证：WayCoder 自主写 10,428 行网页游戏（22 文件），全程 ~40 分钟无卡死无异常，自测 PASS

## v0.79.80 (2026-08-20) — 并发细节 + 资源泄漏 三轮修复

- **`ContextManager.IsCompressing` 静态 bool → 计数器**：多 Agent 并行压缩时先完成的会把指示提前清掉（UI「压缩中」消失）。改为 `static int` 计数，最后一个压缩结束才广播 `CompressFinished`
- **`Agent.Feedback` 自动测试防抖改实例级**：静态 `_lastTestRun`/`_lastTestProject` 会让多槽位/多子智能体并行写文件时互相抑制测试反馈（一个跑过抑制另一个）。改 per-Agent
- **`Agent.Commit` 临时文件 finally 清理**：git commit 抛异常时不再残留 %TEMP% 消息文件
- **`BashTool` 沙箱 `sandboxCts` 改 `using`**：沙箱违规路径不再泄漏 CancellationTokenSource

### ✅ 验证
- 自测 3970 通过（0 真实失败）

## v0.79.79 (2026-08-20) — 静默失败/孤儿进程/超时 二轮修复

- **`ApiKeyStore` 保存失败可见**：`Save`/`Set` 改返回 bool + 记 ErrorLog，此前静默吞 → 用户以为存了重启后 Key 丢失。ModelPicker 保存 Key 失败时提示
- **致命错误保存会话如实报告**：`Agent` 全部模型失败时 SaveSession 失败不再声称「会话已保存」，改提示检查磁盘/权限
- **`CheckpointManager` git 命令无超时**：`/checkpoint`、`/undo` 在卡住仓库/网络盘会永久阻塞 → `WaitForExitAsync` 加 15s 超时 + 进程树杀 + 读取 5s 兜底
- **退出清理孤儿进程**：新增 `ProcessExit` 处理器 → `PersistentShellManager.ShutdownAll()` + `BackgroundTaskManager.ShutdownAll()`（此前应用退出后 cmd/bash 及其内长命令残留）
- **`StatTool` 目录枚举失败误报「0 个文件」**（权限问题）→ 改为报告错误
- `BackgroundTaskManager` 新增 `ShutdownAll`

### ✅ 验证
- 自测 3970 通过（0 真实失败）

## v0.79.78 (2026-08-20) — 并发/资源/正确性批量修复

双子代理并发审查 + 资源审查结果修复（分三类）：

**并发崩溃/竞态**
- **`McpManager.DiscoveredTools` 无锁并发改写**（最确定崩溃）：多 MCP 服务器 fire-and-forget 并行发现时并发 `RemoveAll/Add` 普通 `List`，`ToolRegistry.AllTools` / `McpCache.Save` 枚举会抛 `Collection was modified`。加 `_toolsLock` + 快照方法，变更统一走 `MutateTools`
- **`/loop`、`/plan` 绕过 IsBusy**：当前槽位 Agent 运行中直接启动第二个 `ChatAsync` → 同一 Agent/LLM 并发（推理缓冲/模型覆盖竞态、输出错乱）。加 IsBusy 守卫拒绝并提示
- **子智能体 `cd` 泄漏回父 cwd**：`BashTool.CurrentCwd` 是 static AsyncLocal，子智能体 cd 沿同一 async 上下文回传污染父相对路径。执行前保存父值、finally 恢复

**进程读取永久挂起（HIGH）**
- **守护子进程继承管道 → `ReadToEndAsync` 永不 EOF**：`nohup node &` 等孙进程持有 stdout 写端，主进程退出后读取无超时永久阻塞（BashTool×2 / GitRunner / BatchRunner / BackgroundTask）。新增 `Infra/ProcUtil.AwaitReadWithTimeoutAsync`，统一加 5s 读取超时
- **`TuiChatInput.ReadClipboard` UI 线程同步 `ReadToEnd` 无超时**（剪贴板工具被锁 → TUI 渲染线程永久卡死）。改并发读 + WaitForExit(2s) + 读完成 2s 兜底

**安全/静默失败**
- **HooksManager fail-open**：hook 启动失败返回 `(0,"")` 放行危险工具（与「不能误放行」相悖）。改返回 `(2,"")` = 强阻止（仅 PreToolUse 读 block 决策）
- **`FindReplaceTool` 单文件失败静默吞** → Agent 误以为全部成功。改为报告失败文件
- **`LegacyOffice.Truncate` 按 char 硬切** → CJK 代理对切出 U+FFFD。改用 `TruncateByRunes`

### ✅ 验证
- 自测 3970 通过（0 真实失败）

## v0.79.77 (2026-08-20) — 卡死/崩溃排查修复

排查全部锁/阻塞异步/死循环/网络调用，修复 3 处真实卡死点（其余已验证安全）：

- **`LspTool.ActiveSessions` 锁无超时**（Web 面板/侧栏刷新走 UI 线程）——LSP 握手最坏持锁 10s，此前 UI 线程 `_sessionLock.Wait()` 会被卡住 → 界面卡死。加 `Wait(100)` 超时 + 缓存快照（超时返回缓存，下次刷新再读）
- **`PersistentShell` 锁无超时**（`Dispose`/`ShutdownAll` 退出路径）——会话命令可能跑分钟级，退出被 `_lock.Wait()` 卡死。加 2s 超时，超时跳过
- **主循环 `mgr.Render()` 无异常兜底**——某控件 `OnRender` 偶发异常会直接崩（虽有自动存会话兜底，但属非预期退出）。包 try/catch：记日志 + `RequestFullRefresh` 全刷重试

### ✅ 验证（其余扫描点均安全）
- GitRunner/SystemPrompt 进程读取：并发读 + 超时杀进程（防管道死锁）✅
- `while(true)` 循环：LLM 流（300s 正文超时）/tar 解压/目录上溯 均有退出条件 ✅
- HTTP 阻塞调用（ModelCatalog 2s / ModelCli 4s）均有 HttpClient 超时 ✅
- 自测 3970 通过（0 真实失败）

## v0.79.76 (2026-08-20) — 鼠标支持默认开启 + 开关进设置

- **鼠标支持默认开启**（此前默认关闭、仅 `WAYCODER_MOUSE=1` 门控，导致点击无效）——SGR 鼠标（点击/滚动/移动）现在开箱即用
- **开关进设置文件**：`Config.MouseEnabled`（默认 true）+ 设置界面「鼠标支持」项（`WAYCODER_MOUSE` 环境变量 / `.env` 均可改，设置页可关）
- `TuiManager.MouseEnabled` 改为读 `Config.Instance`，启动时 `EnableMouse()` 按配置决定

### ✅ 验证
- 自测 3971 通过（新增鼠标默认开启/设置项 2 项）
- Keypad 鼠标冒烟：点击聊天区/中部/标题栏均正常路由不崩

## v0.79.75 (2026-08-20) — 修复 Ctrl+Shift+P 拦截 + Keypad 快捷键验证脚本

- **修复 Ctrl+Shift+P 被 Ctrl+P 建议条拦截**：`HandleGlobalShortcut` 的 `if (ctrl)` 块不区分 shift，`case ConsoleKey.P` 会把 Ctrl+Shift+P 当 Ctrl+P 处理 → 打开建议条而非命令面板。现将 Ctrl+Shift+P 检查移到 `if (ctrl)` 块之前，命令面板正常打开
- **命令面板走回调**：ChatScreen 新增 `OnOpenCommandPalette`（Program.Repl 接线、Keypad 可绑定标记），与其它快捷键回调一致
- **Keypad 快捷键验证脚本** `Test/keypad_keybinds.txt`：`waycoder --keypad keypad_keybinds.txt` 一键验证全部回调绑定快捷键 —— `Ctrl+S/D/M/G/H` + 新增 `Alt+P`（模型）+ `Ctrl+Shift+P`（命令面板）均触发对应回调标记；导航/模式/刷新/槽位/退出确认以 SNAP 帧核对
- 说明：`Ctrl+R` 弹阻塞输入框（UxHelper.Ask）无法脚本驱动，由单元自测覆盖

### ✅ 验证
- Keypad 脚本：7 个回调绑定键全部触发标记
- 自测 3969 通过（新增命令面板 10 项）

## v0.79.74 (2026-08-20) — 快捷键对齐 Claude Code / OpenCode（命令面板）

- **`Ctrl+Shift+P` 命令面板**（对齐 Claude Code quickOpen / OpenCode）——`CommandPalette` 此前已实现但从未接线，现接上并精选最常用命令（对标竞品，避免刷屏）：
  - ⚡ 常用动作：模型选择 / 快捷键帮助 / 设置 / 会话列表 / Diff 预览 / 推理深度 / 搜索历史
  - 🗂 常用斜杠命令：`/help` `/reset` `/compact` `/model` `/settings` `/undo` `/diff` `/edit` `/recent` `/session` `/mcp` `/init` `/update` `/stats` `/tokens` `/config` `/provider` `/search` `/git` `/todo` `/theme` `/mode`
- **`Alt+P` 模型选择**（对齐 Claude Code `meta+p`，与 `Ctrl+M` 等价）
- 已对齐：`Esc` 中断、`Ctrl+L` 重绘、`Ctrl+R` 搜索历史；冲突键（`Ctrl+D`=diff、`Ctrl+T`=设置）保留 WayCoder 语义并在文档注明
- 帮助面板 + 使用手册登记新键位

### ✅ 验证
- 自测 3969 通过（新增命令面板 10 项）

## v0.79.73 (2026-08-20) — CLI 参数对齐 Claude Code / OpenCode

- **命令参数对齐竞品（迁移用户无需重新学习）**，全部为新增别名、不动现有参数：
  - `--print`（CC）→ 等同 `-p/--prompt`；`--dangerously-skip-permissions`（CC）→ 等同 `--yolo`
  - `--output-format <text|json|stream-json>`（CC）/ `--format <default|json>`（OC）→ json/stream-json 等同 `--json`
  - `--permission-mode <default|acceptEdits|plan|bypassPermissions>`（CC）→ bypassPermissions 等同 `--yolo`
  - `--allowedTools` / `--disallowedTools`（CC）→ 工具白/黑名单（等同 WAYCODER_ALLOWED/DISABLED_TOOLS）
  - `--system-prompt` / `--append-system-prompt`（CC）→ 追加到系统提示词（WayCoder 结构化基础提示，统一为追加）
  - `--session <id>`（OC）/ `--resume-session-id`（CC）→ 等同 `--resume <id>`
  - 已对齐：`-c/--continue`、`-r/--resume`、`-m/--model <id>`、`-v/--version`、`-h/--help`、`--init`
- 自测补 14 项（别名定义 + 解析行为）；修复 HasKeyFor 测试对用户真实 api_keys.json（含 openai key）的脆弱依赖

### ✅ 验证
- 自测 3958 通过

## v0.79.72 (2026-08-20) — Web 亮色主题对比度修复

- **Web 亮色主题太亮、线框看不清**：`www/style.css` 亮色主题（`/theme` 切换）调暗背景（`--bg:#f5f6f8→#dde1e9`）、面板改为柔和近白（`--panel:#f6f7fa`）、边框加深（`--border:#e2e5ec→#bcc4d2`）使对话框/区域线框清晰，对话框阴影加强（`0 6px 24px`）让浮层突出；重新生成 `WebAssets.Generated.cs`

### ✅ 验证
- 构建通过

## v0.79.71 (2026-08-20) — 未知模型上下文兜底改 128K

- **未知模型上下文窗口兜底 1M → 128K**：`WAYCODER_MAX_CONTEXT`（`Config.MaxContextTokens`）默认从 1048576 改为 131072，未知/未收录模型（含 `ContextWindow=0` 的自定义导入模型）按 128K 处理，避免高估窗口导致请求超限
- **token 显示上限改用 agent 真实窗口**：右下角「当前/上限」不再显示配置兜底值，改用 `agent.Context.MaxTokens`（切换模型后同步显示真实窗口）

### ✅ 验证
- 自测 3944 通过

## v0.79.70 (2026-08-20) — 修复设置页切模型不更新上下文窗口

- **修复：设置页/空格预览切换模型后上下文窗口不更新**——`ModelPicker.Apply` 此前只改 `cfg.Model`，设置界面（`SettingsScreen` 直接调 `ModelPicker.Show()`，走不到 `/model` 命令的 `UpdateContextWindow` 后处理）切模型后窗口残留旧模型值、压缩阈值跟着错，直到重启或走 `/model` 命令
  - 现集中到 `Apply`：全局（slot -1/-2）切换立即更新当前 LLM 的 `Model`/`SmallModel`，大模型切换同步 `UpdateContextWindow(ModelCatalog.ResolveContextWindow(cfg.Model, MaxContextTokens))`，与 `/model` 命令后处理对齐——设置页、空格预览、`/model` 全部路径统一生效

### ✅ 验证
- 自测 3944 通过

## v0.79.69 (2026-08-20) — 写文件内容聊天区展示 + Crush 模型导入

- **写文件内容聊天区内联展示**（对标 Claude Code `FileEditToolUpdatedMessage` 内联 diff）——write_file/edit_file/multiedit 完成后，把写入内容以 diff 格式（行号 + `+`/`-` 标记 + 颜色）直接内联展示在聊天区，占满聊天宽度、无弹窗：
  - 新增 `ContentDiffFormatter`（纯函数，`«»` 中间格式）：`write_file` 全量新增（头行 `path · N 行` + 每行 `行号 +内容` 绿色）；`edit_file`/`multiedit` 变更 diff（`+N/-M` 头行 + hunk 头青色 + `+`绿/`-`红/上下文灰）；CRLF 归一化、超 2000 行截断防刷屏
  - `Agent.ExecuteToolAsync` 写盘成功后读回内容，经既有 `onToolOutput` 单次注入聊天区 —— **仅展示、LLM 上下文保持摘要不膨胀**（显示与模型解耦）；非 TUI/一次性模式自动跳过
  - 新增开关 `WAYCODER_WRITE_CONTENT_VIEW`（`Config.WriteContentView`，默认开，SettingsScreen 同步）
- **删除 Tiny 测试代码**（`TestTinyMode`/`TestTinyWindow`，tiny 模式已作废）
- **支持导入 Crush 模型数据**——`--model import crush` 现可读取 `%LOCALAPPDATA%\crush\crush.json` + `providers.json`（Catwalk 内置目录，40 provider / 1506 模型），Unix `~/.config/crush/` 兜底：
  - `ImportCrush` 支持 providers.json 数组格式 + crush.json providers 对象格式，读 `api_endpoint`/`base_url`
  - `ParseModelNode` 支持 Crush snake_case 字段（`cost_per_1m_in/out`、`context_window`、`default_max_tokens`）
  - 使用手册导入路径说明同步
- **清理误归属模型数据**：移除 `~/.waycoder/provider/opencode.json` 中误归属为 opencode 的内置 deepseek 模型（`deepseek-v4-pro/flash`）——修复切换模型错误路由到 opencode baseUrl + 2 项自测失败

### ✅ 验证
- 自测 3944 通过（新增 ContentDiff 14 项 + Crush 导入 4 项；0 真实失败，框架既有 1 项 Console 重定向计数怪癖不计）

## v0.79.68 (2026-08-20) — 界面打磨批次

- **侧边栏空分区误导截断**：配额 0 时不再显示"… 还有 N 条"（Todo 0/0 却显示"还有 1 条"），只留标题
- **提示栏建议 `/clear` → `/reset`**（/clear 命令不存在）；**提示栏高度随条目数**（此前恒 10 行，1 条建议也占 10 行推挤输入区）
- **主题切换反馈**：TuiToastQueue（死代码从不渲染）改用 `ShowToast`
- **压缩进度条**移到布局预留行（TH-3），不再覆盖模型信息行
- **chat.tui 删除 modelInfoSpacer**（ComputeLayout 未预留导致标记版状态栏被顶出），自测断言同步
- **对比度修复**：快捷键面板列表白字压灰底→黑字；状态栏灰字压金渐变→黑字；Tab 补全 Bg=7 反白→BgWhite 白底黑字
- **动态栏 spinner 统一黄色**（OnRender 与 DirectWrite 两套口径）；输入区分隔线统一细线（代码版 ━→─，对齐标记版）

### ✅ 验证
- 自测 3952 通过

## v0.79.67 (2026-08-20) — Table 表头竖线 + ModelPicker 空格应用/回车确认

- **TuiTableList 表头加分割竖线**：`FormatHeaderTitles`/`FormatRow` 列间加 `│`（非最后列 width-1 + 竖线，最后列全宽，总宽与分隔线/数据行对齐）
- **ModelPicker 交互**：
  - **空格**：应用选中模型但保持对话框打开（`CommitNoClose`，可连续试多个模型）
  - **回车**：应用选中并关闭（原有 Commit）
  - **`✓ 保存` 按钮**：点击 = 应用选中并关闭（`Wire btnSave → Commit`）
  - `TuiTableList` 新增 `OnSpace` 回调（空格可定制，默认仍激活选中）

### ✅ 验证
- 自测 3952 通过；预览确认表头竖线

## v0.79.66 (2026-08-20) — 输入区高度自适应 + 内部剪贴板兜底

- **对话输入区**：启动默认 1 行；`Ctrl+Enter` 换行（TuiTextArea override HandleCtrlKey）自动增高（高度=行数，最大 5 行）；删除键合并行减行（至少 1 行）；超 5 行滚动显示最新内容
- **复制/粘贴/剪切**：CLI 环境（Keypad 测试/SSH）无 GUI 剪贴板会话时系统剪贴板读到残留 —— `TuiEditBase`/`EditorCore`/`ChatScreen.PasteAsync` 统一加**内部剪贴板兜底**（复制/剪切写内部，粘贴优先内部），保证复制→粘贴一致
- **鼠标切换焦点验证**：编辑器点击编辑区切换焦点（打字进编辑器）、设置界面点击分类切换、聊天点击输入区定位 —— 均正常

### ✅ 验证
- 自测 3952 通过；Keypad 测试：启动 1 行/3 行增高/删除减行/7 行滚动、编辑器与输入框复制粘贴剪切、编辑器点击切焦点

## v0.79.65 (2026-08-20) — Editor 只读模式 + 命令文档补全

- **Editor 只读模式**——`/edit <文件> --readonly`（或 `-r`）只读查看，禁止修改文件：
  - `EditorCore.ReadOnly` 属性，输入/删除/换行/删词/删行/剪切等**全部编辑方法开头拒绝**
  - `TuiRichEditor`/`EditorScreen` 转发只读；标题显示 `[只读]`、状态栏提示"只读查看"、Ctrl+S 弹"只读模式，无法保存"
  - **Plan 模式默认只读**——`/edit` 与 `Ctrl+E` 打开编辑器时，若当前为 Plan 工作模式自动只读（只读分析）
  - 验证：打字/删除/保存全被拒，文件内容不变；Plan 下 editor 自动 `[只读]`
- **命令文档补全**——使用手册速查表补上 `/mcp`（MCP 管理）、`/init`（项目初始化）、`/update`（自动升级）三个已注册但文档缺失的命令

### ✅ 验证
- 自测 3952 通过；只读模式 Keypad 测试（打字/删除/保存拒绝）通过

## v0.79.64 (2026-08-20) — 长命令不阻塞界面 + Release 不含 test 指令

- **`/test all` 卡死修复**——自测模块（50s+）此前同步在 UI 线程跑，主循环 `await` 期间无法读键 → 界面完全卡死。新增 `Program.RunWithUiLoop`（后台任务 + UI 渲染循环），TestCommand 自测改用后台跑，UI 线程保持渲染 + 读键（可打字排队、对话框路由），完成后结果回写聊天
- **编译开关：Release 不含 test 系列指令**——csproj Debug 定义 `WAYCODER_TEST`、Release 排除 `Test/` 目录；`/test` 命令（TestCommand + 注册）、`--test`/`--keypad`/`--tui-demo`/`--tui-preview`/`--tui-audit`/`--bench`/`--limits` 等测试入口全部 `#if WAYCODER_TEST` 包裹，Release 版本不编译、运行被忽略
- **Keypad 新增 `COMMAND` 命令**——后台执行斜杠命令 + 8s 超时检测，排查哪些命令卡住界面；实测 `/test` 系为模块耗时（修复后后台跑不卡），其余命令全部正常

### ✅ 验证
- 自测 3952 通过；Debug `/test` 正常、Release 无 test 入口

## v0.79.63 (2026-08-20) — 对话框渐变随主题统一切换（默认金黄）

- **新增 `TuiTheme.DialogGradient` 主题属性**（对话框统一渐变，默认金黄=橙黄渐变），各主题覆盖自身风格色：
  Ocean 青蓝 / Forest 绿 / Sunset 橙红 / Monochrome 灰 / Retro 琥珀 —— 切主题（`/theme`）时对话框渐变自动跟随
- **所有对话框改用 `DialogGradient`**：TuiDialog 系（confirm/input/select/perm 等）、自定义对话框（SessionPicker/FilePicker/CommandPalette/ReasoningPicker/DiffPreview）、标记 `gradient="warning"`（ModelPicker 等）
- **保留语义色**：Info/Success/Warn/Error 边框（青/绿/黄/红）区分消息类型；按钮渐变（橙黄）暂保持品牌色

### ✅ 验证
- 自测 3952 通过

## v0.79.62 (2026-08-20) — 对话框风格统一 + Keypad 对话框容量/长度测试

- **对话框风格统一为橙黄渐变**——SessionPicker / FilePicker / CommandPalette / ReasoningPicker 原用青蓝/紫粉渐变，统一为 `GradOrangeYellow`（与 TuiDialog 系一致）；按钮统一「主操作橙黄 + 危险红橙」（SessionPicker openBtn 青蓝→橙黄，delBtn 保留红橙）
- **Keypad 新增**：
  - `DIAGDIALOG` — 遍历常用对话框 × 内容长度（1/10/50/100/500/1000 字），验证长内容不崩溃、窗口正常、折行/截断/滚动条正确（实测 36/36 通过）
  - `DIAGSELECT` — 实测单选/多选容量（12/50/100/500/1000 项）：可见 ≤12 项，总数无上限、滚动到末项正常（实测 10/10 通过）
  - `DIALOG:name:N` — 打开指定内容长度（`测`×N）的对话框
- 保留语义色：Info/Success/Warn/Error 边框（青/绿/黄/红）区分消息类型；ModelPicker 全屏无边框

### ✅ 验证
- 自测 3952 通过；设置巡检 85/85；对话框长度 36/36、列表容量 10/10

## v0.79.61 (2026-08-20) — 鼠标支持补全 + Keypad 鼠标指令

- **`TuiList` 加 OnMouse**——此前无鼠标处理，设置界面分类列表等点击无效（只能键盘导航）。现支持：滚轮滚动、左键点击选中（单选触发 `OnSelect`，多选勾选切换）
- **EditorScreen 修复 editorW 初始宽**——`BuildLayout` 假设两侧面板都开（`TW - 2*(leftW+1)` = 38），面板隐藏时 editor 只占 38 列；Render 的 `SyncPanelLayout` 改宽后 HBox 布局不重算（Width 仍 38）→ `HBox.HitTest` 拒绝 x>38，**editor 右侧整片鼠标点击无效**。按实际面板状态算 editorW
- **Keypad 鼠标指令**：
  - `MOUSE:x,y` — 模拟鼠标左键点击（0-based 屏幕坐标），路由当前屏幕
  - `SCROLL:up|down` — 模拟滚轮（带指针坐标；TuiListView/TuiList 按坐标命中才滚动）
- **排查验证**：聊天界面（点击定位/滚轮滚动）、对话框（点按钮）、设置界面（点分类）、editor（点定位光标）鼠标功能均正常

### ✅ 验证
- 自测 3952 通过；设置巡检 85/85；editor 点击全区域定位正常

## v0.79.60 (2026-08-20) — 布局隐藏控件修复 + Keypad 全量对话框/编辑器测试

- **HBox/VBox 布局跳过 `Visible=false` 控件**——隐藏控件不占位、flex 不参与分配。修复 **editor 面板关闭残留**（ToggleLeftPanel/RightPanel 只翻转标志，面板仍占位导致 editor 不扩展覆盖、左侧残留旧像素）；这是通用布局修复，影响所有面板/隐藏控件场景
- **EditorScreen**：面板开关后 `SyncPanelLayout()` + `RootView.Layout()` 重新布局（editor 扩展覆盖）
- **Keypad 工具扩充**：
  - `EDITOR:<文件>` — 直接打开带文件编辑器（绕过无文件时的文件选择框）
  - `TREE` 输出 Visible/Focused 状态（定位布局/焦点问题）
  - 绑定 ChatScreen 快捷键回调（记录触发，验证 Ctrl+D/Ctrl+S/Ctrl+M 等按键处理）
  - `DIALOG` 支持全部 TuiDialog 对话框（info/confirm3/inputline/secret/ask/perm 等补全）
- **排查验证**：editor 全部功能（编辑/光标/删除/面板/查找/退出）、聊天界面全部快捷键、13 个对话框按键均正常无 bug

### ✅ 验证
- 自测 3952 通过；editor 面板开关残影消除；设置巡检 85/85；对话框按键全部正常

## v0.79.59 (2026-08-20) — 设置界面修复 + Keypad 排查工具

- **设置界面：左侧分类 ↑↓ 上下移动修复**——`SettingsScreen.OnKey` 的 ↑↓/PgUp/PgDn/Home/End 无条件调 `NavigateItem`（移动右侧项），不路由给左侧 `_catList`；现焦点在左侧时导航键路由列表（`MoveCategoryList`），并手动比较 `SelectedIndex` 刷新右侧（`TuiList.OnSelect` 只在空格激活时触发）
- **设置界面：右侧详情被挤出屏幕修复**——`TuiMarkup` 创建 `<Separator vertical="true">` 用默认构造（宽 60），垂直分隔线占满 HBox 剩余宽度，把 detailPanel 推到屏外；现垂直 Separator 强制 `Width=1`
- **Keypad 排查工具新增**（`--keypad 脚本.txt`）：
  - `SETTINGSALL` — 自动巡检设置界面全部设置项（逐项 Enter 弹编辑框 + 验证窗口 Title + 关闭），本次实测 9 分类 87 项全部通过
  - `TREE` — 输出当前屏幕控件树（每控件 X/Y/W/H/绝对坐标），定位布局错位
  - `SHOT:<文件.png>` — 截屏当前帧为 PNG（TrueTypeFont 矢量渲染，CJK 可读）
  - `WAYCODER_KEYPAD_SIZE=WxH` 环境变量 — 控制帧尺寸，排查不同终端宽度布局

### ✅ 验证
- 自测 3952 通过；Keypad 巡检设置界面 85/85 项弹框正常（2 项 ModelPicker 由自测覆盖）

## v0.79.58 (2026-08-19) — 界面修复批次：Agent 忙时可输入排队 + diff 交互/美化

- **Agent 忙时可输入排队**：`RunAgentWithRenderLoop` 改用共享 InputManager，普通键路由给 ChatScreen——Agent 执行中输入框可编辑，Enter 提交进 `PendingSubmissions` 队列，Agent 空闲后由主循环逐个处理（排队等响应，界面不卡死）
- **diff 对话框交互修复**（三层根因）：DirectWrite 直写覆盖窗口（门控：非活跃屏幕/栈顶有窗口跳过）；Agent 场景鼠标点击无人处理（RunAgentWithRenderLoop 补鼠标路由 + RenderWait 补鼠标）；双线程并发渲染（`InAgentRenderLoop` 标志 + TuiManager.Render 加锁，RenderWait `readKeys:false` 只等待交给外层）
- **权限/计划对话框统一共享 InputManager**：`ChatScreen.RenderWait` 委托 `UxHelper.RenderWait`——此前裸 `Console.ReadKey` 与主循环双读竞态（按键时灵时不灵）、粘贴前导 `\x1b` 被当 Esc 静默拒绝
- **DirectWrite 光标错位修复**：无脏 + 脏渲染路径末尾 `EmitCursor()` 恢复光标（spinner 直写不再把输入区光标拉到动态栏）
- **diff 美化**：split 模式右面板背景色填充到面板右边界（左右对齐）；有色背景改**深色调**（深红 #800000 / 深绿 #008700 / 深青 #005f5f）+ 亮前景，替代刺眼的 ANSI 亮色 41/42/46（`FgBg`→`FgBgCode` 支持 256 色）
- **提示栏**：行背景填充移到图标前（图标不再被擦掉）；窄容器负空格崩溃 `Math.Max(0,…)`
- **压缩进度条**：`CursorPos0` 修正 1/0-based 错位（画到错误行被覆盖，实为不可见）
- **ComboBox 折叠残影**：`CloseDropdown` 标脏窗口根视图重绘
- **DiffCommand**：支持 `/diff <关键词>` 文件名过滤 + 一次预览上限 10 个
- TuiMenu 可空警告、TuiDynamicBar 过期注释清理

### ✅ 验证
- 自测 3930 通过，编译 0 警告

## v0.79.57 (2026-08-19) — UI 线程模型根治 + /diff 命令 + 侧边栏会话列表

- **UI 线程模型根治并发崩溃**：`ChatScreen` 加 UI 消息队列（`PostToUI` 线程判定直接执行/入队 + `PumpUIQueue` 消费）——后台 Agent 回调（Route / RunAgentWithRenderLoop 的 onToken/onTool/onToolOutput）、权限确认、上下文压缩进度、工作模式变更全部改为**只投递消息**，控件树被 UI 线程独占；`TuiView` 高频遍历（FindFocused/OnRender/OnKey 等）改 `for` + 重读 Count 防御。修复 `Collection was modified` 崩溃（后台线程与 UI 线程 `FindFocused` 遍历 Children 竞态）
- **动态栏 spinner DirectWrite 直写**：`FrameMs` 250ms、spinner 常驻旋转（空闲灰/活跃彩）、各状态统一黄色；直写终端不依赖整条重绘；**门控**（非活跃屏幕 / 栈顶有窗口跳过直写）——修复 diff 对话框打开时 spinner 画到窗口上导致按钮/按键「无效」的假象
- **`/diff` 命令 + Ctrl+D 快捷键**：新建 `DiffCommand` 遍历修改文件逐个弹 `DiffPreview`（旧内容取 `git show HEAD`，非 git/新文件回退全新增）；`/recent` 拆分回纯文件列表；帮助面板补 Ctrl+D
- **侧边栏「⚡ 会话」区改历史会话列表**：`SessionManager.ListSessions` 按槽位取最近 5 条（名称·相对时间·消息数），当前会话 ✓ 高亮，500ms 缓存避免每帧读盘
- **侧边栏弹窗时暂停刷新**：`SyncSidePanel` / `ApplyDynamicSizes` 加 `FocusedWindow` 判断，对话框在场不重建分区不标脏
- **费用显示**：累计费用优先回退本轮费用；模型无定价表时显示 `¥-` 占位
- **侧边栏 section 上下间隔**：`Overhead` 2→3（上间隔+标题+下间隔），对应测试同步更新

### ✅ 验证
- 自测 3930 通过（新增 /diff 命令注册测试 3 项），编译 0 错误

## v0.79.56 (2026-08-19) — 侧边栏优化：标题横线到边 + section 间隔 + 灰色文字

- **标题行**：`名称(num)` + 横线到边（少一格，长度随内容区宽，数据变动不错位）；删除固定 20 长度分隔线
- **section 之间空一行**（呼吸间隔）
- **文字改灰色**（标题/内容 BrightBlack，不再刺眼）

### ✅ 验证
- 自测 3928 通过，编译 0 错误，tty 聊天屏正常

## v0.79.55 (2026-08-19) — TUI 界面微调（模式行呼吸感 + 输入区细线）

- **模式行与状态栏之间空一行**（`modelInfoSpacer`），底部更透气
- **输入框上下分隔线改细线**（`━` → `─`），粗线改细线更精致

### ✅ 验证
- 自测 3928 通过（chat.tui 子节点断言 11→12）

## v0.79.54 (2026-08-19) — 修复 TuiDynamicBar 动画帧数组越界崩溃

**根因**：`CurrentFrame` 先 `(int)` 强转再取模——`毫秒数/500` 约 1.28e11 远超 int.MaxValue，强转溢出为负数索引 → `IndexOutOfRangeException` 崩溃（v0.79.51 动态栏改动引入）。

**修复**：先对帧数取模（long）再强转 int（结果 0-7 安全）。

### ✅ 验证
- 自测 3928 通过，编译 0 错误，tty 聊天屏渲染正常

## v0.79.53 (2026-08-19) — 修复对话框粘贴关闭/花屏（RenderWait 共享 InputManager）

**根因**：`UxHelper.RenderWait`（ModelPicker/DiffPreview 等阻塞对话框）用裸 `Console.ReadKey`，不解析 bracketed paste——粘贴的 `\x1b[200~` 被当 Esc 关闭对话框、内容逐字符乱入花屏。

**修复**：
- `TuiManager.Input` 持有**共享 InputManager**（主循环 + RenderWait 复用，统一 bracketed paste/CSI 解析）
- `RenderWait` 改用 `InputManager.ReadInput`：Key→控件、**Paste→HandleBracketedPaste**（路由对话框焦点输入控件）、Resize→重绘
- `RunReplAsync` 改用共享 InputManager

### ✅ 验证
- 自测 3928 通过，编译 0 错误，tty 聊天屏正常

## v0.79.52 (2026-08-19) — 修复对话框输入框粘贴花屏（bracketed paste 路由）

**bug**：api-key 等输入对话框（TuiDialog.InputLine/Input）粘贴花屏——bracketed paste 事件固定插入主聊天输入框，对话框焦点输入控件收不到内容。

**修复**：
- `TuiEditBase.PasteFromExternal`（public 粘贴入口）
- `ChatScreen.HandleBracketedPaste`：模态对话框打开时，粘贴内容路由到对话框**焦点输入控件**（TuiInput/TuiTextArea），否则才走主输入框

### ✅ 验证
- 自测 3928 通过（TuiTextArea 粘贴/撤销测试全绿）

## v0.79.51 (2026-08-19) — 模型信息行 + 状态栏/动态栏精简 + 修 diff 对话框按键

### 模型信息行（输入区下方，灰字）

新增常驻信息行显示当前配置，字段用 `·` 分隔、不再用尖括号，模式切换后自动刷新：

`工作模式:计划模式 · 经济模式:自动 · 大模型:deepseek-v4-pro · 小模型:deepseek-v4-flash`

- 大/小模型按活跃槽位解析（`AgentSlotConfig.ResolveLargeModel/SmallModel`，槽位继承 F1、回退全局）
- 每帧读 `WorkModeManager.CurrentMode` / `Config.EconomyMode`，内容变了才重绘（模式切换下一帧自动更新）
- 布局：`chat.tui` 加 `<Label id="modelInfoRow">`（代码版 `ChatScreen` 同步），灰字（`dim`）不抢眼

### 动态栏精简

- **不再显示模型**（与模型信息行重复），模型信息统一由模型信息行承担
- **去掉分段竖线 `│`**，靠间距区分左/中/右段
- **上方空一行**与聊天列表分隔（布局加 spacer，代码版 chatH 同步 -1）
- **动画预留字符位**：最前一个字符永远占位（活跃=spinner / 空闲=空格），状态切换不左右跳
- **动画节流 500ms**：`FrameMs` 150→500，活跃态每 500ms 标一次脏——修复"思考态无状态变化 → spinner 冻住不动"，
  又不逐帧整条重绘卡顿

### 状态栏精简

去掉**动画图标（心跳）/ 工作模式 / 经济模式**三个区域——它们已由动态栏 spinner 与模型信息行承担，不再重复。
状态栏保留：槽位指示条（F1-F10）、中间提示文本、右侧 Agent 状态/Token。

### 修 diff 对话框按键无效

根因：`DiffPreview.ShowFullScreen` 的 `UxHelper.RenderWait`（代理线程）与主循环（REPL / `RunAgentWithRenderLoop`）
**双线程同时 `Console.ReadKey`** 抢同一控制台输入，Y/N/A/Q/Enter 按了没反应。
修法：`RenderWait` 加 `readKeys` 参数，diff 对话框传 `false`——按键完全交给外层循环经「键位作用域闸」路由到窗口
（窗口 `RegisterShortcut` 优先命中，测试 `dscr.OnKey` 逐键路由已覆盖）。

### 模型价格校准

按 OpenRouter 在线目录快照校准 `ModelCatalog` 部分模型价格（Claude Opus 5 / Sonnet 5、DeepSeek V4 Pro、
Gemini 2.5 Flash、Qwen Plus、Kimi K2.5 等），并删除仓库根目录的价格提取临时脚本 `extract_prices.py`。

### ✅ 验证

- 自测 3917 通过、0 失败（6 条失败全为本机环境：Tiny 自动探测目录、LSP 项目根、deepseek 供应商被
  `~/.waycoder/provider/opencode.json` 改写，非代码回归）
- 新增断言：模型信息行可见/含四字段/无尖括号/`·`分隔/模式切换刷新、chat.tui 子节点=11、
  状态栏不再重复模式名
- 主项目编译 0 错误

## v0.79.50 (2026-08-19) — GUI 启动引导 + 消息时间线修复 + 侧栏实时化

### GUI：独立进程补齐启动初始化（GuiBootstrap）

GUI 是独立进程、有自己的 `Program.Main`，csproj 排除了主项目 `Program*.cs`，此前从不执行 CLI 的启动初始化
（错误日志/异常钩子/MCP/检查点全靠主项目 `Program.Main`）。新增 `GuiBootstrap` 补齐「进程级、与 UI 无关」的部分，对齐 `Program.Main` 序列：

- 错误日志最先（`ErrorLog.Initialize`）+ 全局未处理异常 / 未观察任务异常钩子——崩溃时经 `GuiBootstrap.OnCrashSave`
  尽力保存各槽位会话（`MainWindow` 构造时挂 `SaveAllSessions`）
- 主题预设 / 沙箱 / 提示词缓存 / 自定义命令 / Hook（`RunSessionStart`）/ MCP / 检查点，逐项失败只记日志不阻断启动
- 窗口关闭走 `GuiBootstrap.Shutdown()` 补 `RunSessionEnd`（对齐 CLI 退出流程）；内部幂等可重入

### GUI 消息时间线：工具消息 / 推理段另起气泡

聊天气泡的时间线此前会错位——工具消息之后的回复正文写回工具消息「之前」的旧气泡，视觉上
「对话全堆在上面、工具消息全堆在下面」。修法：

- `FinalizeStreaming` 由「封最后一条流式气泡」改为「封全部流式气泡」——推理 + 正文可能同时开着，只封最后一条封不干净
- `AppendTool`/`AppendToolOutput` 先封口正文气泡，工具消息之后的回复另起一条（对齐 TUI `onTool→FinishAgentMsg`）
- `GetOrCreateStreamingMsg` 必须同时判角色：只看 `Streaming` 会把正文写进还开着的推理气泡
- 推理段 `«/»` 收尾即封口推理气泡，正文另起一条

### 侧栏实时化：Ctrl+B 立即可见 + 每帧指纹同步

侧栏此前是「摆设」：`Ctrl+B` 只翻标记位，侧栏叶子不标脏，增量渲染跳过，得等手动改一次终端尺寸才
「突然」冒出来；数据也只开侧栏那一刻刷一次。重构：

- `ToggleSidePanel()`：走完整 resize 路径（重排布局 + 按新宽重灌消息）+ 整屏刷新，开关立即生效
- `SyncSidePanel()` + `SidePanelStamp()`：每帧数据指纹比对（模型/模式/槽位忙闲/上下文%/当前工具/Git 分支/
  改动文件数/MCP/LSP/Todo），变了才重建分区并标脏，避免逐帧白烧 GC
- 品牌区「🏷 道码」换成「⚡ 会话」实时区；Todo/文件/MCP 列表不再 `Take(15)` 预截断
- `TuiSidePanel` 删除恒为 0 的死代码 `ScrollOffset`，新增纯函数 `AllocateHeights` 按实际可用高度给各分区配额，
  超出的折成「… 还有 N 条」

### 顺带

- 修 `ThemeConfig` 静态初始化顺序：`Instance = Load()` 挪到 `Presets` 字典之后，避免加载预设时字典未初始化
- `TuiScreen.AddWindow` 兜底聚焦：窗口内没有控件带焦点时 `win.FocusNext()` 聚焦首个可聚焦控件，
  修「树形视图演示建了控件却没人 `Focused`」的键盘失灵
- 删除仓库根目录三个与产品无关的 Python 示例/压力脚本（2048/贪吃蛇），保持仓库纯净

### ✅ 验证

- 自测 3910 通过、0 失败（另有 4 条因本机 `~/.waycoder/provider/opencode.json` 自定义模型改写 deepseek 供应商而失败，属本地配置，非代码回归）
- 主项目 + GUI 编译 0 错误

## v0.79.49 (2026-08-19) — 启动快捷键简版 + Diff 对话框按钮化 + 工具参数加长 + 标题栏不闪

### 启动快捷键显示简版

启动欢迎 / 槽位首条消息不再列全量快捷键（40 条太挤），只显示常用键：
`F1-F10` 槽位、`Esc` 中断、`Ctrl+Z` 暂停、`Ctrl+C` 退出、`Ctrl+S` 会话、`Enter` 发送、
`Ctrl+V` 粘贴、`↑↓` 历史/滚动、`Shift+Tab` 切模式、`Ctrl+M / /model` 模型、`PgUp/PgDn` 翻页、
`Ctrl+E` 编辑器、`Ctrl+B` 侧栏、`Ctrl+H` 帮助。完整版按 `Ctrl+H` / `F1` 打开速查面板。
`TuiKeybindHelp.StartupKeys` 是筛选集，与全量表单一事实源（`Groups`），加新键默认只进完整版。

### Diff 对话框：缩小 + 底部按键

- 尺寸不再逼近全屏：宽 `3/4` 屏、高 `70%` 屏（各留边距），小终端还能再小
- 底部加四个渐变按钮（接受/跳过/全部接受/取消），Tab 切焦点 + 空格执行，
  与 `Y/N/A/Q` 快捷键走同一动作 —— 按钮是快捷键的图形化替身
- 修 `/loop` 等走 `RunAgentWithRenderLoop` 的路径：它在栈顶有窗口时仍会把
  `Y/N/A/Q` 当无用键吃掉（只认 Esc/Ctrl+Z/Q），现在与 REPL 主循环同规矩——
  有窗口先给窗口（键位作用域闸）

### 工具消息参数加长

`onTool` 回调原先把参数摘要硬砍 57 字符，bash 命令/文件路径一眼看不全。
改为完整传给 `AddToolProgress`，由它按聊天区宽度截取（宽度减 4 列，带省略号）。
动态栏那份仍截短（一行小空间）。

### 标题栏：商标恒定 + 不闪烁

- 左上角恒为商标名（`Global.AppFullName`）—— 之前 `TitleBar.Title = StatusLeft`，
  模型一换/loop 计数一变就把商标顶掉（「商标变模型名」）
- 修闪烁：工具开始/结束、权限等待、压缩进度、加聊天消息都只刷各自控件
  （动态栏 / 聊天列表），不再把整棵根标脏。根一脏，标题栏作为叶子被
  `parentDirty` 拉着整行重绘，金色渐变重画一次就是一次闪。工具一多就连续闪。

自测 3899 → 3910 通过、0 失败（另有 4 条因本机 `~/.waycoder/provider/opencode.json`
自定义模型把 deepseek 供应商改写为 opencode 而失败，属本地配置，非代码回归）。

## v0.79.48 (2026-08-19) — 修「输入对话框改屏幕尺寸，按钮不见了」

两个叠加的根因：

### 1. 窗口高度在 resize 时不重算（按钮被裁在窗口外）

输入类对话框（`Input`/`InputLine`/`Secret`）的提示文本在窄屏会折成更多行 → 内容变高。
但 `ApplyContentWidth` 注册的 resize 处理器只重算宽度，**高度留在构建时那个值**，
内容比内容区高一行 → 底部按钮落在窗口外 =「按钮不见了」。

改法：`ApplyContentWidth` 新增 `afterResize` 钩子，三个输入对话框传
`() => FitWindowToContent(win, fitWidth: false)`，窄屏重折行后高度跟着内容重算。

### 2. resize 后窗口子树没被标记重绘（按钮渲染被增量跳过）

`TuiWindow.OnResize` 原先 `RootView.MarkDirty()` —— `parentDirty` 只向下传播一层，
套在 `HBox` 里的按钮（父容器不脏、自身也不脏）被增量渲染的
`child.IsDirty || parentDirty` 过滤跳过。resize 改变了所有控件的位置/尺寸，
任何一层都可能要重画 → 改成递归 `RootView.Invalidate()`。

自测加 8 条输入对话框 resize 断言（真渲染一帧，断言缩窄终端后确定/取消按钮仍在帧里、
窗口高度确实变高）。3864 → 3899 通过、0 失败。

## v0.79.47 (2026-08-19) — 修「输入框/按钮按了键不刷新」：状态变了必须标脏

增量渲染只画脏控件（`TuiView.OnRender` 里 `child.IsDirty || parentDirty`），
所以**任何改变绘制结果却没标脏的状态变更 = 界面不刷新**。此前漏标脏的地方：

| 漏的地方 | 表现 |
|---|---|
| 单行 `TuiInput` 的光标移动 / 选择 / 撤销重做 | 按 ←→/Home/End/Shift+方向/Ctrl+Z，选中高亮与滚动位不变 |
| 多行 `TuiTextArea` 的左右移动 / Home / End / 选择 | 同上（上下移动和翻页原先有单独补的 `MarkDirty`） |
| `TuiControl.Focused` 直接赋值 | Tab 之外的换焦点路径（鼠标点选、`ClearFocus`、code-behind）按钮高亮不变 |
| `TuiLabel.Text` / `TuiButton.Text` 外部赋值 | code-behind 改状态回显（「扫描中…」、「→小模型」）看不见变化 |
| `TuiInput.Text/CursorPos`、`TuiTextArea.Lines/CursorRow/CursorCol/ScrollRow` 外部赋值 | 预填/清空/滚动不显示 |

只有改文本的原语会走 `NotifyChanged`（内含 `MarkDirty`），其余全靠各处自觉——自觉不了。改法：

- `TuiControl` 新增 `SetDirty(ref field, value)`：值真变了才写入并标脏。上表那些属性全部改走它
- `TuiEditBase.OnKey` 处理完按键统一标脏（**单行和多行共用这一条路径**），
  省得每个编辑原语各记一次；没处理的键不标脏，增量渲染该省的还是省
- `TuiControl.MarkDirty` 顺带叫醒 `TuiManager.IsDirty` 帧闸门——控件脏了却没有帧，等于没脏

自测加了 23 条脏标记断言 + 4 条**端到端**断言（真渲染一帧，断言新内容确实写进了增量帧、
没变的控件确实没被重画）。3864 → 3891 通过、0 失败。

## v0.79.46 (2026-08-19) — 按钮渐变成系统默认风格 + .tui 可设渐变 + 模型框 Ctrl 加速键

### 按钮渐变：默认风格放主题，标记只管覆盖

按钮的渐变开关和配色不再由各处 code-behind 挨个调 `ApplyButtonGradient`，改成**控件默认值 + 系统级主题项**：

- `TuiTheme` 新增 `ButtonGradientByDefault`（默认 true）与 `ButtonGradient`（默认橙→黄 `BtnOrangeYellow`）
- `TuiButton` 的 `GradientBg`/`GradientBgStart`/`GradientBgEnd` 都改成空值回落到主题，**显式设过的不受主题改动影响**
- **标记不写特征就跟默认走**；要覆盖才写

`.tui` 新增渐变属性（窗口边框与按钮底共用一套解析 `TuiMarkup.ParseGradient`）：

| 写法 | 效果 |
|---|---|
| 不写 | 跟控件/主题默认 |
| `gradient="false"` | 关成扁平 |
| `gradient="btnCyanBlue"` | 开 + 取语义渐变（随主题） |
| `gradientStart="#102030" gradientEnd="#405060"` | 显式给色（写了色即隐含开） |

语义名两族：`cyanBlue`/`greenCyan`/`orangeYellow`/`redOrange`/`purplePink`/`titleBar` 给边框，
`btn*` 那族比边框亮 30% 给按钮底；另有 `info`/`success`/`warning`/`danger` 别名，与 `fg`/`bg` 的语义色对齐。
语义名与显式色同写时显式色赢。

模型对话框由此瘦身：窗口边框改 `gradient="warning"` 写在 `modelpicker.tui` 里，code-behind 不再写死 RGB；
十个按钮一个属性都不用写，直接吃默认金色渐变（之前是一排扁平黑底，像堵黑墙）。

### 模型对话框

- 搜索框与表格之间加一行空行——两个都是黑底控件，贴在一起分不出边界
- `Ctrl+字母` 加速键回归（按钮仍在，键只是快捷方式）：`^T` 大小模型 `^G` 全部槽位 `^S` 扫描 `^R` 导入
  `^O` 在线 `^P` 设Key `^L` 清Key `^N` 添加 `^U` 编辑 `^D` 删除。
  裸字母一律留给过滤，`Ctrl+A/C/X/V/Z/Y/E/K` 留给搜索框编辑（`KeyHook` 跑在 `TuiEditBase.HandleCtrlKey` 前面，占了就等于把全选/复制/粘贴/撤销抢走）

自测 3826 → 3864 通过、0 失败。

## v0.79.45 (2026-08-19) — 键位两级作用域：系统键只剩 Ctrl+C，其余全归窗口

规范：**系统键**任何时候最高优先级（目前只有 `Ctrl+C`）；**窗口键**只在所属窗口是栈顶时生效，
弹子窗口即被屏蔽，子窗口关闭后随焦点回到父窗口而恢复。由此子窗口与父窗口用同一个键不冲突。

窗口层本来就是对的（`TuiScreen.OnKey` 模态独占后直接 return、`CloseWindow` 把焦点还给栈里上一个窗口），
**破口在 REPL 主循环**：`Program.Repl.cs` 有 6 组标着「系统级」的先判分支，排在 `mgr.OnKey` 之前无条件执行。

平时看不出来，因为对话框多半用 `RenderWait` 阻塞主循环。但 `PermissionManager` 的权限确认框走
**Agent 后台线程**的 `RenderWait`，只阻塞后台线程，主 REPL 循环仍在读键——两个循环抢同一个控制台缓冲区，
REPL 抢到就截胡：

| 对话框开着时按下 | 之前的后果 |
|---|---|
| `F1`–`F10` | 底下换槽位，且先 `while (...) PopScreen()` **把屏幕栈拆掉** |
| `Ctrl+K` / `Shift+Tab` | 切工作模式 |
| `Ctrl+Q` | 整个进程 PanicExit |
| `Esc` / `Ctrl+Z`（槽位忙时） | 直接 Cancel Agent，而不是关对话框 |

改法：

- 新增 `UI/TUI/Base/TuiKeyScope.cs` —— `IsSystemKey` 是「哪些键能穿透对话框」的唯一事实源，
  目前只认 `Ctrl+C`（它本就走 OS 的 `CancelKeyPress`，任何线程都能触发，这里只是把事实写进代码）
- REPL 主循环加**一道总闸**：栈顶有窗口时，非系统键一律 `mgr.OnKey` 下发。一处生效，
  下面 5 组分支自动全部降级为窗口键，不用逐条改。顺带把 `Shift+Tab` 这种独立事件类型
  补成真的 `ConsoleKeyInfo` 再下发（原先 `KeyInfo` 可能是空的）
- 修次要穿透路径：`AddWindow` 里非模态浮层（Toast）不再抢走模态对话框的焦点，
  `TuiScreen.OnKey` 改按「栈顶模态优先」路由而非认 `FocusedWindow`——
  否则 Toast 叠在模态上时键会漏到根视图（表现为对话框开着还能往输入框打字）

按用户裁定：`Ctrl+Q` 降级为窗口键（严格按规范，`Ctrl+C` 仍是万能退路）；
`Esc`/`Ctrl+Z` 在对话框开着时归对话框（权限框正是靠 `Esc` 表示拒绝）。

未做：REPL 的 F1-F10 与 `ChatScreen.HandleGlobalShortcut` 里那份是重复实现（前者多做 Agent 侧绑定），
合并要给 ChatScreen 接回调，属独立重构；加闸后无窗口时行为不变，已在代码里标注。

自测 3803 → 3818，新增 15 条：系统键白名单、F 键被对话框屏蔽/关闭后恢复、
父子窗口同键各归其主、非模态浮层不抢模态的键与焦点。

## v0.79.44 (2026-08-19) — 模型对话框：功能改按钮，字母键还给过滤

用户报的「输入字符串过滤功能没有」，根因不是过滤没实现，是**首字符被快捷键吞了**：
`S`/`I`/`O`/`L`/`K`/`A` 和数字在「搜索框为空」时被当动作键，于是想打 `openai`、
`siliconflow`、`4o` 时，第一个字符触发的是扫描/导入/设 Key，压根进不了搜索框。

改法不是换几个快捷键，而是**把功能全做成按钮**：`Tab` 切焦点、空格/Enter 执行。
框架本来就支持（`TuiScreen` 的 Tab 焦点遍历 + `TuiButton` 的空格激活），
之前用不上只是因为 ModelPicker 把 `Tab` 注册成了「切大/小模型」的快捷键，把遍历挡了。

- 10 个按钮分两行：`→小/大模型`｜`全部槽位`｜`扫描`｜`导入`｜`在线` / `设Key`｜`清Key`｜`添加`｜`编辑`｜`删除`
- `ClassifyKey` 从 13 个动作缩到 4 个（Nav / Commit / Slot / None），**裸键一律落回搜索框**
- 按钮不注册字母快捷键 —— `TuiWindow.OnKey` 的快捷键匹配会按 `KeyChar` 回退到大写键，
  注册了照样抢字符；自测里钉死这条
- 保留 `F1`–`F10` 选槽位（F 键打不出字符，安全）

同一轮修的其余四条外观问题：

| 问题 | 根因 | 修法 |
|---|---|---|
| 分组横线背景格格不入 | `TuiTableList` 组头传 `bg: 0`（透明），透出对话框灰底，而数据行是 `ListBg` 黑底 | 组头/列头/分隔线统一走行底色，只用前景色区分 |
| 窗口不是金色边框 | `.tui` 写的 `borderColor="brightblack" gradient="false"` | 换权限框同款 `GradOrangeYellow` 渐变 |
| 标题左对齐 | `TuiScreen.RenderWindow` **只在渐变分支居中标题**，非渐变分支硬写在 `X+1` | 上一条换成渐变边框后自动居中 |
| 表格内容全挤在左边 | 列宽声明合计 54，控件宽约 76，右侧空 22 列 | 新增 `TuiTableList.StretchColumns`，按声明比例放大铺满，余数补给最宽列 |
| 底部提示左对齐 | 标签没设对齐 | `.tui` 加 `align="center"`（`TuiLabel.TextAlign` 与标记解析本来就有） |

自测 3758 → 3803。

## v0.79.43 (2026-08-19) — 终端缩放时对话框内容跟着重算，不再只动外框

`TuiWindow` 早就留了 `OnResizeContent` 钩子，注释白纸黑字写着「由 TuiDialog 工厂方法设置」——
但翻遍代码，**注册它的只有自测**（`SelfTest.Chunk9.cs` 十几处），没有一个真实对话框用过。
于是对话框的尺寸全是构建时按当时屏宽算死的，缩放终端只有外框跟着动，里面的标签还按老宽度折行。

两个家族分别接上：

| 家族 | 对话框 | 之前 resize 的表现 |
|---|---|---|
| `FillMsgBox` 系 | info / success / warn / error / confirm / confirm3 / permission | 外框变，消息不重折行 |
| `ApplyContentWidth` 系 | input / inputline / secret / select / multiselect / ask | `XScale=0` 之后**外框都不动** |

- 新增 `FitAndBindResize`：定尺寸和注册重算是同一件事，避免下次再有人只做前一半
- `ApplyContentWidth` 加 `applyWidth` 回调参数——「把内容宽刷到控件上」这个动作本身
  就是 resize 处理器，注册进 `OnResizeContent` 即可，调用点只需把控件查找提前
- `ask` 的窗口高度也挪进回调：窄屏下消息折行变多，高度不跟着长会把选项列表挤出去
- `Tty.SizeOverride`（仅自测用）：布局代码遍地直接读 `Tty.Cols`/`Rows`，
  不给钩子就没法断言「缩放后有没有重算」——真实控制台尺寸自测改不动
- 自测 3748 → 3758：把终端从 160 列缩到 60 列，断言窗口变窄、标签宽度跟着缩、
  **消息行数变多**（真的重新折行了，不只是框变小）

## v0.79.42 (2026-08-19) — 权限框宽度自适应 + 清掉残留占位符 + 按钮渐变底还原

### 第一行那个左对齐的 `…`

`permission.tui` 里 `msgBox` 有个设计态占位标签 `<Label text="…" />`。别的对话框走 `FillMsgBox`，
里面有 `msgBox.Children.Clear()`（`TuiDialog.cs:140`/`:487`/`:756` 三处都清），
唯独 `Permission` 是自己 `foreach + Add`，**漏了 Clear** —— 占位符就永远留在第一行。

### 内容很窄，框却很宽

`Permission` 的宽度来自 `ContentW(WideXScale, 4)` —— 屏宽 × 0.75，**压根不看消息内容**，
一行短消息也撑出一个大宽框。改成和 Info/Confirm 同一条自适应路径（`FillMsgBox` + `FitWindowToContent`），
宽度取「消息最宽行」与「按钮行宽」的较大者：短消息下按钮行成为约束项，
120 列终端上由 ~90 列缩到 46 列。一次改动同时修掉上面那个 `…`。

### 按钮渐变底还原

v0.79.39 把按钮统一成黑底白字，彩色渐变（青蓝/绿青/橙黄/红橙）好看得多，还原。
但不能直接回退——渐变分支用的是 `t.ButtonFg`，而那次为了黑底扁平按钮把它从黑改成了白，
**白字压在橙黄渐变上几乎看不见**。所以拆成两个字段：

| 渲染路径 | 前景 | 背景 |
|---|---|---|
| 扁平按钮 | `ButtonFg` = 白 | `ButtonBg` = 黑底 |
| 渐变按钮 | `ButtonGradientFg` = 黑（新增） | RGB 渐变（亮） |

- 自测 3743 → 3748：25 条按钮断言由「黑底白字」翻成「渐变底且 RGB ≥0x1000000」
  （低于这个值 `TuiButton` 会悄悄回退成扁平分支），加 3 条权限框宽度/占位符，
  2 条锁住两个前景字段不许合并

## v0.79.41 (2026-08-19) — 修好 Shift+Tab（Windows）+ 状态栏显示模式名 + 砍掉重复品牌

### Shift+Tab 在 Windows 上是漏判，不是没法修

`InputManager` 只认 Unix 终端发的 `ESC[Z`（`InputManager.cs:176`），而 Windows 的 `Console.ReadKey`
给的是 `ConsoleKey.Tab` + Shift 修饰键，永远不可能有 `ESC[Z`——所以 Windows 上按 Shift+Tab 毫无反应。
补上修饰键判断即可，两平台都通了：

- 模式切换现在认三个入口：`InputType.ShiftTab`（Unix）、`Tab`+Shift（Windows）、
  **`Ctrl+K`**（两平台通用别名，新增）
- 判定抽成纯函数 `InputEvent.IsModeSwitchKey` —— REPL 主循环是个读真实按键的 async 大方法，没法自测，
  而这个条件已经写错过一次。抽出来后 9 条断言锁住，含两条最要命的反向：
  **裸 `Tab` 必须放行**（否则路径补全没了）、**裸 `k` 必须放行**（否则打字就切模式）
- 帮助表的平台差异脚注从 3 条缩到 2 条——`Shift+Tab` 那条删掉，剩下的 `Ctrl+M`/`Ctrl+H`
  是 Unix 终端层的硬冲突（≡Enter / ≡Backspace），真的无解，继续给 `/model`、`/help` 兜底

### 状态栏只画了个 🔨，没人认得出那是「建造模式」

- `TuiStatusBar` 模式指示由 `Emojis[mode]` 改 `WorkModeManager.Format(mode)`，显示 `🔨 建造`
- 顺手按模式着色：建造=青 / 计划=黄 / 审查=亮青 / 自动=绿，非建造态一眼能看出「现在不写文件」

### 品牌名在主界面上有三份

`slot0.StatusLeft` 启动时被赋成 `Global.AppFullName`，而它同时喂给顶栏标题、动态栏左段、动态栏右段、
侧栏会话区——于是「WayCoder 道码·通用编程智能体」在一屏里出现 3 次，其中两次还在**同一根动态栏**上。

- `TuiDynamicBar` 右段空闲时重画 `LeftText` 的分支删掉（注释写着「显示模型名」，可 `LeftText` 就是左段那份）
- `slot0.StatusLeft` 启动值由品牌名改 `_config.Model`——`/model` 切换后本来就会写成模型名，启动态跟着一致
- 品牌保留在顶栏标题 + 聊天流欢迎横幅两处，各司其职

- 自测 3728 → 3734：4 条锁模式中文名（渲染后 StripAnsi 断言），2 条锁 Ctrl+K 别名与脚注收缩

## v0.79.40 (2026-08-19) — 快捷键表改成实话：11 条对齐实现 + 平台差异脚注

`Ctrl+H` 帮助面板和启动首条消息共用一张表，但那张表是照着「打算做成什么样」写的，不是照着代码写的。
逐条核对实现后发现 11 条不符，其中 `Ctrl+C` 一条最误导——表里写「中断」，实际是**退出**。
这次只改表和手册，**不动任何行为**（改键的方案另议）。

- **改对的**：`Ctrl+C` 中断 → 退出（先存会话）；`Ctrl+↑↓` 输入历史 → 聊天滚动 3 行；
  `Tab` 切换焦点 → 路径补全 / 插 4 空格；`Ctrl+P` 命令面板 → 输入建议条；
  `Home/End` 跳列表首尾 → 输入区行首/行尾；`Ctrl+O` 打开文件 → 打开设置（同 `Ctrl+T`）
- **补上的**：`Esc` 中断当前 Agent、`Ctrl+Z` 优雅暂停——两个最常用的键此前一个字没写
- **拆开的**：裸 `↑↓` 一个键两种行为，按输入区空不空分流，拆进「编辑」（输入历史）和「导航」（聊天滚动）两组
- **删掉的**：`Ctrl+Shift+F1/F2` 切主题——REPL 的 F 键分支不看修饰键，这两个组合实际是切槽位 1/2
- **平台差异脚注**：输入走 `Console.ReadKey`，Windows 拿虚拟键码、Unix 拿字节流，
  而 `Ctrl+M≡0x0D≡Enter`、`Ctrl+H≡0x08≡Backspace`、`Shift+Tab≡ESC[Z`——
  于是两边坏的是**不同的**键：Unix 下 `Ctrl+M`/`Ctrl+H` 失效，Windows 下 `Shift+Tab` 失效，
  各自的兜底命令（`/model`、`/help`、`/mode`）一并写进脚注
- `docs/使用手册.md` 两张快捷键表同步重写，加「⚠ 平台差异」矩阵
- 自测 3719 → 3728：9 条锁住这次的更正，表再被改回去会红

## v0.79.39 (2026-08-19) — 对话框配色统一：灰底黑字 + 控件黑底白字 + 选中反色

主题里本来就写着「灰底黑字」（`WindowBg=PanelGrey` / `DialogFg=Black`），但各对话框自己又覆盖了一层，
于是权限框黄底、模型选择器黑底、设置页蓝标题栏、按钮四套彩色渐变，看着不像一套东西。这次把覆盖全撤掉：

| 层 | 配色 |
|---|---|
| 对话框 | 灰底（`PanelGrey`）黑字 |
| 输入框 / 按钮 | 黑底白字，聚焦反色（白底黑字） |
| 列表 / 树 / 表格 | 黑底白字，选中行反色（白底黑字） |

- **撤掉的写死值**：`permission.tui` 黄底黄标题 → 主题灰底（黄边框留着，那是「权限确认」的语义信号）；
  `modelpicker.tui` `bg="black"` → 主题灰底；`settings.tui` 标题栏 `bg="44"`（蓝）→ 灰；
  各对话框提示文字 `fg="8"`/`fg="brightblack"`/`fg="4"` → `black`
- **按钮不再用渐变底**：`ApplyButtonGradient` 改成统一设黑底白字 + 聚焦反色。
  每个对话框一套渐变（青蓝/绿青/橙黄/红橙）压在灰底上太花；按钮和输入框一样是可操作控件，跟着走同一套
- **选中色 青 → 反色**：`ListSelBg`/`TreeViewSelBg` 由 `BgCyan` 改 `BgWhite`（`ListSelFg` 本来就是黑）
- **树补底色**：新增 `TreeViewBg`。此前树的未选中行是透明的（`Bg > 0 ? Bg : 0`），放进灰底对话框会漏灰底；
  同时改成整块控件区先铺底，节点不满一屏时底部不再露出灰底
- **命令面板 / 会话管理器**未选中行同理由透明改黑底；会话管理器「当前会话」标记色 `Blue` → `BrightCyan`（蓝压黑底看不见）
- **浅色 / 高对比度主题补 `ButtonFg`**：基类默认由黑字改白字，这两个主题只覆盖了 `ButtonBg`，
  不补就会白字白底（`--theme-verify` 实测 1.00:1）。高对比度顺手把灰底按钮改白底黑字（2.17:1 → 21:1）
- 自测 3675 → 3719：主题 8 条 + 每个对话框「灰底 + 按钮黑底白字」逐个校验

## v0.79.38 (2026-08-19) — `/test dialog` 对话框巡检

在运行中的会话里把对话框挨个弹一遍，肉眼核对排版/按钮/快捷键——之前 `/test` 只有 `perm`/`toast`/`menu` 三个，
要看全套得退出去开 `--tui-demo`。

- `/test dialog` —— 21 个对话框依次弹出，**关掉一个自动弹下一个**，每个的结果（选了什么/取消）回写聊天流
- `/test dialog <名字>` —— 只弹一个：`info` `success` `warn` `error` `confirm` `confirm3`
  `input` `inputline` `secret` `select` `multiselect` `ask` `askmulti` `perm` `toast` `menu`
  `model` `session` `reasoning` `palette` `file`
- 串链靠 `win.OnClosed`（Esc 走 `TuiScreen.OnKey` 也会触发，且 `ShowWindow` 保留调用方设的回调），无需状态机；
  全屏 ANSI 选择器自带 `RenderWait` 泵、串不进回调链，放在链前顺序跑
- 自测补 14 条：每个窗口式对话框都必须能构建（`LoadDialog` 找不到 `.tui` 里的 id 会直接抛，
  光靠人工弹窗得弹到那个才发现）

## v0.79.37 (2026-08-19) — 模型对话框底部快捷键提示被裁掉 + 权限按钮改灰底

- **模型选择对话框看不到快捷键提示**：`ModelPicker.BuildWindow` 手算 `listH = winH - 5`，
  但内容区只有 `winH - 2` 行，而固定行是 4 行（搜索/槽位/帮助/帮助2）——总高比内容区多 1 行，
  最后一行帮助（`Ctrl+A添加 Del删除 Ctrl+E编辑…`）被下边框裁掉。
  改由 `modelpicker.tui` 给表格标 `flex="1"`，剩余高度交 `TuiVBox` 分配，code-behind 不再手算行数
- **权限对话框按钮底色 cyan → grey**：cyan 在 Campbell 等主流终端配色里是 `#3A96DD` 的蓝，
  压在黄底对话框上刺眼；聚焦态仍是白底黑字，对比度不变
- 自测补 16 条断言：模型选择器在 h=16/20/28/40 四档高度下三行提示均在内容区内 + 表格 flex 高度正确；
  权限三按钮底色为灰

## v0.79.36 (2026-08-19) — «» 中间格式支持真彩：`«fg:#rrggbb»` / `«bg:#rrggbb»`

命名色枚举不完，且此前**背景色根本没有语法**（只有内联代码那处硬写数字，还写错成残缺的 48）。补上带参颜色标签：

| 写法 | 含义 |
|---|---|
| `«fg:#rrggbb»` / `«#rrggbb»` / `«#rgb»` | 真彩前景（`#rgb` 缩写等价 `#rrggbb`） |
| `«bg:#rrggbb»` / `«bg:#rgb»` | 真彩背景 |
| `«bg:red»` / `«bg:cyan»` | 命名背景（标准 16 色前景码自动 +10；256 色码不偏移） |

- 解析集中在 `MarkdownParser.TryMapTag(tag, out code, out isBg)`，**三端同一套语法**：
  TUI（`ParseInline`/`ParseMarkupOnly`）、CLI（`SpectreToAnsi` 新增 `ExpandColorTags` 前置扫描，
  因为 Replace 链枚举不到带参标签）、Web（`app.js` 新增 `markupTokenStyle`）
- 真彩码沿用既有约定 `AnsiTty.RgbCode` = `0x1000000 | rgb`，`FgCode`/`BgCode` 已能展开成 `38;2;` / `48;2;`
- **前景与背景各自独立入栈**：`«bg:#000080»底«fg:#f00»红«/»还底«/»` 中内层 `«/»` 只弹前景，背景留着
- 大小写不敏感；非法写法（`#gg0000`、`#ff00`）不认，标签原样输出暴露笔误
- Web 侧十六进制走严格白名单正则再拼进 `style` 属性，不给注入留口子

### ✅ 验证
- 自测 3608 通过 0 失败（新增 15 条：语法解析 7 条、前景/背景栈 4 条、CLI 解码 3 条、未知标签 1 条）

## v0.79.35 (2026-08-19) — 首屏快捷键表改成左对齐两列表格 + 修内联代码「亮绿底」+ 纯文本漏解码 «»

**快捷键表（`TuiKeybindHelp.GetHelpText`）**：原先是空格拼的自由排版，落到首屏还被前一条居中的
system 消息带偏成参差居中。改为「键 │ 说明」两列表格：
- 键列按 `AnsiHelper.DisplayWidth` 补齐（CJK 安全），各分类块共用同一列宽 + `─` 分隔线，整体一张表
- `ChatScreen.AddMessage` 的 `centered` 参数改 `bool?`：`null` 才继承前一条同角色消息的对齐，
  显式传 `false` 强制左对齐——此前无条件继承，表格类内容一律被前一条居中消息带歪
- 键名保持亮色，其余（标题/分类/分隔线/竖线/说明）走 `«grey»`，用中间格式而非裸 ANSI
- 删除作废的 `Ctrl+Shift+F1/F2`（主题选择/轮换）两行，主题仍可走 `/theme` 与设置界面

**修「亮绿底色」**：`MarkdownParser.ParseInline` 给内联代码 `` `路径` `` 配的背景码写的是 **48**，
注释还标着「深色背景」——但 48 是 SGR 的**扩展背景色引导码**，必须跟 `5;n` 或 `2;r;g;b`，
裸发 `ESC[48m` 是残缺序列，各终端解释不一（Windows Terminal 渲染成亮绿底），
于是聊天里每个路径/命令都糊一片刺眼绿。改为不加底色（`defaultBg`），只保留黄色文字。

**修纯文本漏解码 «»**：`TuiMarkdown.RenderMessage` 的 plainText 分支（system/tool 消息）
把每行原样输出，`«grey»` 直接印成字面量。新增 `MarkdownParser.ParseMarkupOnly`——
**只解码 `«tag»`/`«/»`，其余字符一律原样保留**：不能对这条路径套完整内联解析，
因为 shell 输出里的反引号、`**`、`#` 是数据不是格式，会被当 Markdown 吃掉。

### ✅ 验证
- 自测 3593 通过 0 失败（新增 12 条：表格形态/列对齐/左对齐/颜色标记归属、48 背景码、纯文本解码 4 条）
- 编译 0 错误（构建输出改指 `bin/verify/` 以免占用正在运行的 waycoder.exe）

## v0.79.34 (2026-08-19) — 修 CLI/一次性模式漏解码 «» 中间格式（终端显示出转义标记）

**症状**：CLI 模式下推理内容显示成字面的 `«dim»…«/»`，而不是暗色效果。

**根因**：`LLM.cs:520` 在推理段首尾经 `onToken` 注入 `«dim»`/`«/»` 中间格式标记，各端须自行解码——
TUI 走 `MarkdownParser.MapMarkupTag`、Web 走 `markupToHtml`、GUI 剥离标记分流到推理气泡，
**唯独两条 CLI 路径直接 `Console.Write(tok)` 裸打印**：
- `Program.Repl.cs:66`（`--cli` 交互模式）
- `Program.Output.cs:156`（`-p` 一次性 / 管道模式）

**修复**：两处均改为 `Console.Write(SpectreToAnsi(tok))`（与同文件 `MarkupLine` 同源解码器，
无状态纯替换，标记由 LLM 整体一次发出不会被切片）。`SpectreToAnsi` 开为 `internal` 便于自测断言。

**顺带修一条脆弱断言**：`grep 空路径不崩溃`（`SelfTest.Chunk1.cs`）要求结果含「错误」或「未找到」，
但空路径语义就是搜 cwd，在 WayCoder 源码目录里搜 `test` 必然命中——它测的其实是「当前目录里有什么」。
改用新增的 `TryToolCall` 助手，只验「不抛异常且返回非空」，回归其注释本意。

### ✅ 验证
- 自测 3581 通过 0 失败（新增 3 条 `SpectreToAnsi` 解码断言）
- 编译 0 错误

## v0.79.33 (2026-08-19) — 设计态样本数据 `{InDesign '…'}` + 自测按省钱档位分叉（修极致档压缩失效）

**`.tui` 设计态数据标记**：任意属性值可写 `{InDesign '样本'}`——设计/预览态取引号内内容，真实运行取空串：
- `TuiMarkup.ResolveDesign` 在 `Attr` 统一解析，**所有属性通用**（items/text/columns…），不必给每个控件开后门
- 引号可省（读到 `}` 为止）、一属性可多处、`{InDesignMode}` 同前缀占位符不误伤、引号未闭合原样保留（暴露笔误）
- 7 个界面就地声明样本：`chat.tui`（聊天气泡）、`commandpalette`/`keybindhelp`/`sessionpicker`（列表项）、`modelpicker`（表格行）、`reasoningpicker`/`settings`（选单项）
- 预览器（`--tui-preview` / WPF）直接看到数据，真实 UI 一行不漏；样本仍由 code-behind 覆盖（`ClearRows`/`Items=`）

**预览器支持资源名**：WPF 预览与 `--tui-preview` 一致——路径或资源名（`dialogs/modelpicker.tui`）皆可，下拉框列出全部可用资源，命中文件系统 `Raw/` 才热刷新。

**自测按省钱档位分叉**（用户 `.env` 开 `WAYCODER_ECONOMY=on` 时曾有 24 条假失败）：
- 新增 `PromptWithMode(档位)` 测试辅助：完整版断言自带 `Off` 基线，不再拿「当前环境」当完整版
- `[系统提示词]` 段按生效档位分叉断言：`Off/Auto`=完整提示词 + 15 条规则；`On`=含工作目录/先读后改、砍流水线、比完整版短；`Extreme`=极简标识、比精简版更短
- 补反向断言：省 token 模式不注入技能段 / 不注入仓库地图

**修复：极致档上下文压缩完全失效**（本次分档跑测暴露）：
- `ContextManager.ResolveRatio` 被百分比阈值与字符数阈值两种量纲复用，`Extreme` 分支却写死 `Math.Max(2000, …)`——把压缩线抬到 `MaxTokens × 20`，三层压缩永不触发
- 下限改为调用方按量纲显式传入（`extremeFloor`）：百分比不设下限，字符数传 2000

### ✅ 验证
- 四档全绿：`off` 3576 / `on` 3578 / `auto` 3576 / `extreme` 3577，**失败均为 0**（修复前 extreme 4 失败、on 24 失败）
- 编译 0 错误 0 警告；`--tui-preview dialogs/commandpalette.tui` 实测样本行正常显示

## v0.79.32 (2026-08-19) — TUI 预览支持 Screen 根 + 数据控件完整显示

- `TuiPreview.Run` 对 **Screen 根**（showcase/chat/main）用 `RenderOnlyScreen` 渲染（此前只窗口化渲染，数据控件不显示）
- 实测 showcase 预览：**List/DataList/Tree/TableList/ComboBox/RadioGroup** 数据全部显示
  - List `items`、DataList `items`+cell 模板、Tree `items` 路径、TableList `columns`+`items`
- 模板声明 items 即可预览数据；自定义项数据（cell 模板/占位符）同样显示

### ✅ 验证
- 自测 3569 通过，编译 0 错误

## v0.79.31 (2026-08-19) — TUI 预览支持内嵌资源名（预览任意对话框/新属性）

- `TuiPreview.Run`（`--tui-preview`）：非文件路径时按**内嵌资源名**加载（`dialogs/modelpicker.tui` / `chat.tui` / `menu.tui` 等），预览任意内嵌界面
- title 占位符用文件名填充；新属性（`bg`/`titleFg`/`focusedBg`/列等）自动在预览显示
- 实测：权限对话框（黄底黑字按钮）、模型对话框（搜索 + 表格列 + 黑背景）预览正常

### ✅ 验证
- 自测 3569 通过，编译 0 错误

## v0.79.30 (2026-08-19) — 权限对话框样式移入 .tui（少写代码）

「属性特征尽量放资源文件」推广：权限对话框（黄底黑字）固定样式移入 `permission.tui`：
- **TuiMarkup 扩展**：Window 支持 `titleFg`/`titleBg`，控件支持 `focusedFg`/`focusedBg`
- `permission.tui` 声明：`bg="yellow"` `titleFg="black"` `titleBg="yellow"` + 按钮 `fg/bg/focusedFg/focusedBg`
- `TuiDialog.Permission` 删除硬编码颜色，只留 Flex/事件/消息内容

### ✅ 验证
- 自测 3569 通过，编译 0 错误，tty 聊天屏正常

## v0.79.29 (2026-08-19) — 模型对话框布局/颜色移入 .tui

「布局写标记、颜色写标记」：模型对话框的静态样式全部移到 `modelpicker.tui` 声明：
- **背景/边框**：`bg="black"` + `borderColor="brightblack"` + `gradient="false"`（TuiMarkup 新增 Window `bg` 属性支持）
- **搜索框**：`fg="white" bg="black"`
- **表格列**：`columns="🔑:2,模型:24,厂商:11,窗口:6,价格:7,大:2,小:2"`（列定义移模板）

`ModelPicker` code-behind 删除硬编码颜色/列，只保留动态数据/事件。

### ✅ 验证
- 自测 3569 通过（TuiMarkup WinBg/模板测试全绿），编译 0 错误，tty 聊天屏正常

## v0.79.28 (2026-08-19) — 模型对话框帮助提示两行 + 搜索过滤增强

- **帮助提示分两行**显示（help + help2）：全部按键提示完整可见（↑↓/Enter/Tab/S/I/O/K/L + Ctrl+A/Del/Ctrl+E）；状态提示时第二行清空
- **搜索过滤增强**：按名称/厂商/**供应商ID**（ProviderId）过滤模型

### ✅ 验证
- 编译 0 错误，自测 3569 通过，tty 聊天屏正常

## v0.79.27 (2026-08-19) — 模型对话框 key 设置/修改增强

- **K / Ctrl+K** 设置/修改选中模型的 API Key（预填当前值，可改；custom 模型也可设，仅 local 除外）
- **L / Ctrl+L** 清除 Key（回退全局 key）
- 设置/清除后**重配当前 Agent**（`Reconfigure` 新 key + baseUrl，运行时生效无需重启）

### ✅ 验证
- 自测 3569 通过，编译 0 错误，tty 聊天屏正常

## v0.79.26 (2026-08-19) — 模型对话框黑色背景 + 增删改保存

- **黑色背景**：模型对话框 WinBg 改黑色，去掉橙黄渐变边框（暗色边框）
- **增删改快捷键**：
  - `Ctrl+A` 添加自定义模型（弹输入：模型名|ProviderId|BaseUrl）
  - `Delete` 删除选中自定义模型（确认；内置不可删）
  - `Ctrl+E` 编辑自定义模型（预填当前值，改后覆盖保存）
  - `Enter` 应用/保存（已有）
- 帮助行更新全部快捷键

### ✅ 验证
- 自测 3569 通过（模型库增删改断言全绿），编译 0 错误

## v0.79.25 (2026-08-19) — 快捷键表输出到启动首条对话（每个槽位）

- `TuiKeybindHelp.GetHelpText()` 把快捷键表转为纯文本
- **槽位 0 启动欢迎**后追加快捷键表
- **每个槽位首次激活**（F2-F10 HasWelcome）首条对话含完整快捷键表
- 自测新增 4 断言（分组/槽位/帮助/模型快捷键）

### ✅ 验证
- 编译 0 错误，自测 3569 通过

## v0.79.24 (2026-08-19) — TUI 系统常用快捷键补齐 + 帮助面板修正

- **新增**：Ctrl+L 全屏强制重绘、F5 刷新/重绘、Ctrl+Home/End 聊天滚动
- **修正帮助面板与实际一致**：
  - Ctrl+S 会话列表（原误标"保存"）
  - Ctrl+G 推理深度（原误标"文件选择"）
  - Ctrl+R 搜索历史（原误标"推理深度"）
  - Ctrl+M /model 打开模型对话框（注明 Kitty 终端 + /model 命令兜底）
  - 补 Ctrl+E 编辑器 / Ctrl+T/O 设置 / Ctrl+B 侧栏 / Ctrl+Shift+F1/F2 主题
  - 移除不存在的 Ctrl+D Diff

### ✅ 验证
- 编译 0 错误，自测 3565 通过

## v0.79.23 (2026-08-19) — 修复 TUI 模型对话框按不出 + 输入 key

- **原因**：Ctrl+M 在普通终端映射为 `\r`=Enter（Kitty 协议终端才可区分），模型对话框无法触发
- **修复**：`/model`（无参）弹 `ModelPicker` 对话框（任何终端可用）；选择无 key 模型 → `UxHelper.Secret` 输入 API Key → 应用 + 运行时重配
- `ModelPicker.Apply` 改 public 供命令复用

### ✅ 验证
- 编译 0 错误，自测 3565 通过

## v0.79.22 (2026-08-19) — 弹出菜单 .tui 模板化（TUI 全界面资源化完成）

- 新增 `menu.tui` 声明弹出菜单窗口结构（Dialog + VBox）
- `TuiMenu.Show` 从模板加载窗口 + 覆盖动态属性（尺寸/定位/模态/边框），`MenuView` 保留动态项渲染（滚动/快捷键/分隔线）
- 至此 TUI 全部窗口类界面均 .tui 资源化：聊天屏(chat.tui)、聊天项(chat-item.tui)、14 对话框(dialogs/*)、菜单(menu.tui)、设置/编辑器/选择器

### ✅ 验证
- 自测 3565 通过（TuiMenu 测试全绿），tty 下聊天屏正常（无模板错误）

## v0.79.21 (2026-08-19) — 聊天列表项 .tui 模板化

- 新增 `chat-item.tui` 声明式布局（Header: icon/role/time + Markdown body）
- `TuiListItem.BuildContent` 从模板加载 + 填充（角色名/图标/时间/markdown），不再代码手搭
- 续接/嵌套消息隐藏模板 header 行；主题切换更新 icon/role 色
- 布局写标记、逻辑写 code-behind 架构延伸至消息项

### ✅ 验证
- 编译 0 错误，自测 3565 通过，tty 下聊天屏正常（无模板错误）

## v0.79.20 (2026-08-19) — TUI 界面全面 .tui 资源化（标记版聊天屏翻默认）

TUI 全界面检查 + 资源化：
- **聊天主屏翻 .tui 默认**：`MarkupChatScreen`（chat.tui 声明式布局）为默认界面，手写 `ChatScreen` 仅作 chat.tui 加载失败的异常兜底
- 全部 14 个对话框工厂（Info/Success/Warn/Error/Confirm/Confirm3/Input/InputLine/FindReplace/Secret/Select/MultiSelect/Ask/Permission）均走 `dialogs/*.tui`
- 设置/编辑器/命令面板/文件选择/模型选择/会话选择/推理选择/快捷键帮助等界面均用 .tui 资源

### ✅ 验证
- tty 模拟下 Markup 全屏正常存活；非交互（stdin 重定向）走管道模式正常退出
- 自测 3565 通过

## v0.79.19 (2026-08-19) — 设置复位默认值（三端）

- `SettingDef` 新增 `Default`（透传 schema 默认值），`/settings` 序列化含 `default`
- **GUI**：每项「↺ 默认」按钮 + 分组「♻ 全部复位默认」
- **Web**：每项「↺ 默认」按钮 + 分组「♻ 全部复位默认」
- **TUI**：R 键复位当前项 + Ctrl+R 全部复位分组

### ✅ 验证
- 自测 3565 通过；主项目 + GUI 编译 0 错误

## v0.79.18 (2026-08-19) — 三端设置同步「💰 计费」分组

- 修复 `EconomyPriority` 在 SettingSchema 的重复定义（旧「⚙ 参数」残留与新「💰 计费」冲突）→ 删除旧定义，计费分组完整
- 三端（TUI/Web/GUI）均为 Schema 驱动，新分组自动出现：
  - **Web** `/settings` 实测返回「💰 计费」10 项（模式/优先级/6 阈值/正常裁剪/Tiny 窗口）
  - **GUI** 设置窗口 `SettingSchema`+`GroupBy` 自动含新分组（number/select 控件）
  - **TUI** `SettingsScreen` 分类分组 + number/select 编辑自动支持

### ✅ 验证
- 自测 3565 通过；主项目 + GUI 编译 0 错误

## v0.79.17 (2026-08-19) — 计费设置独立分组 + 各模式阈值全部可调

设置界面新增 **「💰 计费」分组**，集中管理所有模式的计费/上下文阈值，均可在设置界面调整：

**省钱模式（Extreme 已含）**：
- 省 Token 模式（off/auto/on/extreme）、自动优先级（quality/balanced/cost）
- 裁剪阈值 %、摘要阈值 %、硬折叠阈值 %、工具输出裁剪字符、单次输出上限、复杂任务判定轮数（常量 → 可配置属性）

**正常模式**：
- 工具输出裁剪字符（`SnipCharsNormal` 4000，常量 → 可配置属性）

**Tiny 模式**：
- Tiny 窗口（`TinyWindow` 4096，新增 schema）

配套：`WAYCODER_ECONOMY_SNIP_RATIO`/`_SUMMARIZE_RATIO`/`_COLLAPSE_RATIO`/`_SNIP_CHARS`/`_MAX_TOKENS`/`_COMPLEX_ROUNDS`、`WAYCODER_SNIP_CHARS_NORMAL`、`WAYCODER_TINY_WINDOW` 环境变量。

### ✅ 验证
- 自测 3565 通过；主项目 + GUI 编译 0 错误

## v0.79.16 (2026-08-19) — 省钱模式新增「极致」档（提示词尽量不注入、上下文尽量少给）

`EconomyMode` 新增 `Extreme`（极致）档，比 On 更激进：

- **提示词尽量不注入**：`SystemPrompt.GenerateExtreme` 极简版（仅工具名 + 核心规则，砍环境/项目上下文/完整工具描述）
- **上下文尽量少给**：
  - 压缩阈值比 economy 再收紧 20%（更早压缩）
  - 工具输出裁剪减半（`EconomySnipChars/2`）
  - `max_tokens` 减半（`EconomyMaxTokens/2` = 4096）
- 配置：`WAYCODER_ECONOMY=extreme` / `--economy extreme`，设置界面新增档位；状态栏 🔥 图标

### ✅ 验证
- 自测 3565 通过；主项目 + GUI 编译 0 错误

## v0.79.15 (2026-08-19) — 新增 --tui / --cli 界面参数

允许显式进入两种界面：
- **`--tui`**：强制 TUI 全屏界面（默认即 TUI，显式指定更明确）
- **`--cli`**：强制 CLI 文本界面（非全屏，`»` 提示符逐行交互，Agent 纯文本回复，`exit`/`quit` 退出）

实现：`TuiArg`/`CliModeArg` 参数 + `RunCliReplAsync`（复用管道模式 `ProcessTextInput` 纯文本处理）。`--cli` 与 `--tui` 同传时 TUI 优先。

### ✅ 验证
- `--cli` 实测：提示符 + 输入 + 退出；`--help` 显示两参数
- 自测 3565 通过

## v0.79.14 (2026-08-19) — TUI 管道模式：echo "任务" | waycoder 自动执行

承接 v0.79.13 非交互崩溃修复，补齐管道 stdin 完整语义：
- `echo "任务" | waycoder` 逐行读 stdin 交给 Agent，纯文本输出（不启动全屏 TUI）
- 支持 `onToken` 流式回复 + `onTool` 工具调用行 + 异常兜底
- EOF 自动退出；交互终端行为不变

### ✅ 验证
- 管道输入实测：读 stdin → 显示输入 → Agent 执行 → 退出（LLM 401 为本地无 key，非代码问题）
- 自测 3565 通过

## v0.79.13 (2026-08-19) — 修复 TUI 非交互崩溃（管道/重定向/CI）

4 端验证发现：`echo "x" | waycoder`、CI 后台等非交互场景，`InputManager.ReadInput` 的 `Console.KeyAvailable` 在输入重定向下抛 `InvalidOperationException` 崩溃。

- **修复**：`Console.IsInputRedirected` 检测 → 返回 `Timeout` 事件空转，不再崩溃
- 真实交互终端行为不变；管道 stdin 完整读入语义为后续增强

### ✅ 验证
- 自测 3565 通过；TUI 非交互后台启动不再崩溃
- 4 端编译运行确认：CLI/TUI/Web/GUI 全部正常

## v0.79.12 (2026-08-19) — GUI 聊天推理内容独立气泡（对齐 Web reasoning）

GUI 聊天内容格式对齐：推理内容（`«dim»…«/»`）从"混在正文淡色"升级为**独立 reasoning 气泡**（对齐 Web `.msg.reasoning`）：
- `ChatRole.Reasoning` 新角色 + `MessageBubble` 淡色小字气泡
- `AppendToken` 按 `«dim»`/`«/»` 标记分流：推理内容进独立气泡，正文排除
- 每槽位独立推理状态（`_inReasoning`），`/reset` 重置

至此 GUI 聊天格式对齐 Web：气泡角色（user右/assistant左/工具/系统/**推理淡色**）+ Markdown 完整渲染（表格/代码高亮/链接）+ 工具输出代码块

### ✅ 验证
- GUI 编译 0 错误，启动运行正常

## v0.79.11 (2026-08-19) — GUI 附件上传 + 斜杠命令（对齐 Web）

### 📎 附件上传（对齐 Web /upload）
- GUI「📎」按钮接 `OpenFileDialog`（图片/音频）
- 图片 → `LLM.QueueImage`（入 vision 队列，槽位 AgentId 隔离）
- 音频 → `TranscribeAudioTool` 后台转录 → 结果入聊天流
- GUI 槽位 Agent 设唯一 `AgentId`（`gui-slot-N`，防多槽位图片串扰）

### ⌨️ 斜杠命令（GUI 侧实现，对齐 Web /command）
`/help` `/model` `/settings` `/theme` `/reset` `/todos` `/tokens` `/perm <mode>` `/slots`；未知命令回退普通消息

### ✅ 验证
- GUI 编译 0 错误，启动运行正常
- 剩余对话框全对齐：文件选择/上传、命令面板、密码/密钥输入（ModelWindow 已有掩码）、多行输入（AskAsync 已支持）

## v0.79.10 (2026-08-19) — GUI 系统通知对齐（UxHelper.Info/Success/Warn/Error）

GUI 版 `UxHelper.Info/Success/Warn/Error` 此前回退 Console（GUI 无控制台 → 通知丢失）。修复：
- `UxHelper` 新增可注入 `OnNotify` 委托（level/title/message）
- GUI `MainWindow` 注入 `OnNotify` → 通知显示到当前槽位聊天流（带 ℹ/✓/⚠/✘ 图标）
- TUI 消息框 / Console 兜底保留

### ✅ 验证
- GUI + 主项目编译 0 错误，GUI 启动正常

## v0.79.9 (2026-08-19) — GUI 权限/选择对话框对齐：命令着色 + 单选/多选 checkbox

### 🔧 权限确认对话框（对齐 TUI InlinePermission）
- **工具图标标题**：bash→🔧、write→📝、edit→✏️、read→📄
- **命令着色**：bash 命令绿色 / 文件路径青色（对齐 TUI 参数着色），等宽可选中
- 保留允许/全部允许/拒绝三键 + 超时

### ☑️ 单选/多选对话框（对齐 Web/TUI）
- **多选**：CheckBox 列表（对齐 Web 多选 checkbox，勾选明确）
- **单选**：ListBox 列表
- 标题带 emoji（📋 单选 / ☑ 多选）

### ✅ 验证
- GUI 编译 0 错误，启动运行正常
- 三端对话框能力对齐：权限（TUI 行内 Y/A/D / Web 弹窗 / GUI 模态+着色）、单选/多选（TUI 列表 / Web checkbox / GUI checkbox）

## v0.79.8 (2026-08-19) — GUI 设置对话框 Schema 驱动（对齐 TUI/Web）

GUI 版设置对话框从手写 4 项（API Key/Temperature/MaxTokens/自动提交）升级为 **Schema 驱动**（对齐 TUI SettingsPage / Web 设置抽屉）：

- **全量设置项**：`Config.SettingSchema()` 动态生成所有配置（模型/API/界面/安全/上下文…按分类组织）
- **分类导航**：左侧分类列表 + 右侧当前分类设置项
- **各类型控件**：toggle→CheckBox、select→ComboBox、secret→密码框、number/text→输入框
- **逐项保存**：读控件值 → `TrySetPropValue` → `SaveToEnvFile`，保存后刷新模型目录/省钱/权限下拉/右侧面板

### ✅ 验证
- GUI 编译 0 错误，启动运行正常
- 三端设置能力对齐：TUI（Tab 分类）/ Web（设置抽屉）/ GUI（分类列表 + 全量 Schema）

## v0.79.7 (2026-08-19) — GUI diff 逐 hunk 确认（对齐 TUI/Web）

GUI 版 `GuiInteraction.DiffConfirmAsync` 从"整文件接受/拒绝"升级为**逐 hunk 确认**，三端 diff 能力对齐：

- **逐 hunk 勾选**：每个 hunk 独立显示（header + 等宽着色行，+绿/-红），CheckBox 勾选接受任意子集
- **底部操作**：✔ 全部接受 / ✘ 全部拒绝 / 取消 / 💾 应用所选
- **返回语义**：全勾→AcceptAll、全不勾→RejectAll、部分→Partial + AcceptedHunks（与 TUI `DiffPreview.Show`/Web ask-modal 一致）

### ✅ 验证
- GUI 编译 0 错误，启动运行正常
- 三端 diff 能力对齐：TUI（全屏 y/n/q）/ Web（ask-modal checkbox）/ GUI（本窗口 checkbox）

## v0.79.6 (2026-08-19) — TUI 模型对话框完善：供应商分组 + 扫描/导入/OpenCode/设置key

参考 Web 版模型弹窗完善 TUI 版 `ModelPicker`：

### 🗂️ 供应商分组
- **分组显示**：模型列表按供应商分组，组头行显示供应商名 + 连通状态（✅/❌）
- `TuiTableList` 新增**组头行支持**：`AddGroupHeader`（独立淡色样式、不可选中、导航跳过），`NextSelectable` 定位首个数据行

### ⌨️ 操作快捷键
- **S 扫描连通性**：后台 `Task.Run` 跑 `ModelCli.TestList`，结果 `lock` 保护，组头实时显示 ✅/❌
- **I 自动导入**：后台 `ModelCli.Import`（本地配置）
- **O OpenCode 在线导入**：后台拉取 `https://opencode.ai/zen/go/v1/models` + 批量写入，完成后刷新列表
- **K 设置 key**：弹输入框 `TuiDialog.InputLine` 保存 `ApiKeyStore`
- **L 清除 key**
- 帮助行更新全部快捷键提示

### ✅ 验证
- 主项目 + GUI 编译 0 错误
- 自测 3561→3565（+4 组头断言：标记/导航跳过/NextSelectable）

## v0.79.5 (2026-08-19) — Web 前端审查修复：忙碌状态上报 + system 事件 + 槽位切换竞态

对 Web 前端 app.js（2500 行）做专项审查，修复多槽位并行的状态缺陷。

### 🔴 忙碌状态丢失（P1）
- **切到后台忙碌槽位发送 → 消息丢失 + 按钮卡死**：`switchSlot` 无条件复位发送按钮，但服务端状态不含 busy，前端也不监听 `system` 事件 → 向运行中的槽位发送被拒但无反馈，`isBusy` 永真
- **修复**：后端 `SerializeState` 各槽位新增 `busy` 标志（`SlotBusyFlags` 取自 `WebSlot.IsBusy`）；前端切槽位不再无条件复位、`state` 处理器按活跃槽位忙碌同步发送/停止态；新增 SSE `system` 事件监听（槽位忙碌提示可见）

### 🟠 时序/状态缺陷（P2）
- `/chat` fetch 失败后 `isBusy` 永久卡死 → 错误分支复位并提示
- 快速连续切换槽位时陈旧 `/slot` 响应覆盖当前视图 → 响应加 `currentSlot` 守卫丢弃
- 模型栏显示全局默认模型而非槽位实际模型（`/sessions/load` 可覆盖槽位模型）→ 用 `state.slots[activeSlot].model`
- SSE 断线无重同步 → `onerror` 提示（重连后 `state` 处理器按槽位 busy 复位）

### 🟡 细节（P3）
- 连续工具输出合并到同一块 → `tool` 事件清空上一块
- `saveKey` fetch 无错误反馈 → 加 `.catch` 提示
- `hasKey` 死代码等记录待清理

### ✅ 验证
- 主项目编译 0 错误，`/state` 返回 `slots[].busy`，前端含 system/onerror 监听
- 自测 3561 通过

## v0.79.4 (2026-08-19) — 二次审查修复：模型迁移防数据丢失 + 文件并发原子化 + GUI 气泡/挂起修复

对 v0.79.2/v0.79.3 新增代码（模型存储重构 + GUI 版）做专项审查，修复一批 P0/P1/P2 问题。

### 🔴 数据安全（P0）
- **模型迁移误删旧数据**：`MigrateLegacyModels` 原逻辑「provider/ 目录有文件即删 models.json」——用户先执行过 `--model add`（写分类文件但未触发迁移）后旧模型永久丢失。改为 **`.migrated` 标记文件**判断 + 全部分类文件**写成功后才删旧文件**，任何失败保留现场

### 🟠 模型存储并发/原子性（P1）
- **文件读改写加锁**：`AddCustom`/`RemoveCustom`/`RemoveCustomByProvider` 纳入统一锁（防 Web 并发导入/删除 read-modify-write 竞争丢模型）
- **原子写**：`SaveCustom` 改临时文件 + `File.Move` 原子替换（防崩溃/磁盘满留截断文件覆盖全量）
- **批量导入**：新增 `AddCustomRange`（同分类合并一次写），opencode 在线导入 + `ModelCli.Import` 改用——23 个模型从 23 次磁盘写减为几次
- **ReadFile 加日志**：损坏模型文件不再静默消失（记录 `ErrorLog.Warning`）

### 🖥️ GUI 版（P1）
- **空气泡修复**：`MessageBubble` 构造不渲染导致用户/系统/工具/会话恢复历史全部空白——构造即渲染；主题切换显式重渲染所有气泡（block 文字色构建时固化）
- **对话框超时**：`GuiInteraction` 全部对话框加 `WaitWithTimeout`（超时返回拒绝/取消，防 Agent 无人点击无限挂起）
- **异常防护**：`RenameSession` 包 try/catch（I/O 异常不再崩应用）
- **UI 线程**：`ChatAsync` 包 `Task.Run` 隔离（LLM 流解析/同步工具不再卡 UI 线程）
- **ModelWindow `_busy`**：改 volatile（防快速连点扫描/导入并发执行）

### ✅ 验证
- 主项目 + GUI 编译 0 错误 0 告警
- 自测 3561 通过

## v0.79.3 (2026-08-18) — GUI 版全面对齐 Web：三栏布局 + 气泡聊天 + 右侧面板 + 模型弹窗 + 主题 + 完整渲染

把 Avalonia GUI 版从单列 MVP 升级为与 Web 版同等的完善界面。

### 🖥️ 三栏布局 + 主题
- **三栏布局**：左（槽位 F1-F10 + 历史会话列表）/ 中（气泡聊天 + composer）/ 右（5 数据面板）；顶栏含 logo + 主题切换 + 模型按钮
- **深/浅主题切换**：`App.ToggleTheme` + 动态资源绑定，控件取色走主题资源键；`GuiTheme` 配置持久化（`Config` 新增）

### 💬 消息气泡化 + 完整渲染
- **消息气泡**：`ChatMessage`/`MessageBubble` 结构化消息，user 右 / assistant 左 / 工具 / 系统 分角色，流式合帧（根治全量重渲染 O(n²)）
- **块级 Markdown `MarkdownBlocks`**：气泡内部多 block 容器，支持段落/标题/引用/列表/分隔线/**表格（Grid）**/代码块
- **代码语法高亮 `SimpleHighlight`**：注释/字符串/数字/关键字着色（对齐 Web tok 配色）
- **Markdown**：粗体/行内代码/链接（品牌蓝下划线）

### 📊 右侧 5 数据面板
- 任务 / Token费用 / 修改文件 / MCP / LSP 卡片，`DispatcherTimer` 2s 刷新，数据直连主项目静态类（TodoTool/EditFileTool/McpManager/LspTool）

### 🧠 模型弹窗 + Composer + 会话管理
- **模型选择对话框 `ModelWindow`**：搜索 + 供应商分组 7 列表格 + 扫描/自动导入/OpenCode 在线/设置 key/保存/切换
- **Composer 工具栏**：大/小模型按钮 + 💰省钱 + 🛡交互模式 + 发送箭头（busy 变 ⏹ 停止）；输入自动增高 + Enter 发送/Shift+Enter 换行 + 多槽位输入草稿
- **历史会话列表**：预览/元信息 + 加载/重命名/删除/新建/清空（按槽位隔离）

### ✅ 验证
- GUI 项目编译 0 错误，运行正常（每阶段实测）
- 主项目编译 0 错误，自测 3561 通过

## v0.79.2 (2026-08-18) — Web 输入框美化 + 模型按供应商分类存储 + opencode 在线导入

### 🎨 Web 前端输入区美化（对标主流 AI 聊天页）
- **Composer 卡片**：输入框改为圆角卡片容器（22px 圆角 + 聚焦光晕 + 内阴影），textarea 透明化融入
- **工具栏内嵌**：大模型/小模型/省钱/交互模式移入输入框内部（回形针与发送箭头之间），整行居中；省钱加 💰 钱袋图标
- **发送按钮**：纸飞机 → 向上箭头（描边风格）；空输入禁用态、忙碌变红色停止按钮
- 输入框高度可调（28px → 56px）、去掉底部提示横线与分隔线、整体上移 1.5cm

### 🗂️ 模型按供应商分类存储
- 自定义模型库从单文件 `models.json` 改为 `~/.waycoder/provider/{供应商}.json` 分类分文件：
  - 本地模型（local/custom/ollama/lmstudio 等）→ `locals.json`
  - 其余按 providerId 命名（minimax.json / moonshot.json / zhipu.json / openai.json…）
  - 旧 `models.json` 自动迁移到分类文件后删除；删空分类文件自动清理
- `ModelCatalog.ProviderGroupName` 供应商分组（非法字符剥离防路径穿越）

### 🌐 OpenCode 在线导入
- 模型弹窗新增「🌐 OpenCode 在线」按钮，拉取 `https://opencode.ai/zen/go/v1/models`（OpenAI 兼容格式）
- **baseUrl 保留 opencode 网关**（模型经 `zen/go/v1` 访问，chat/completions 端点已验证）
- **按模型 id 前缀推断真实供应商分类**（`InferProviderFromId`）：minimax-*→minimax、kimi-*→moonshot、glm-*→zhipu、qwen-*→qwen、deepseek-*→deepseek、gpt-*/o*→openai、claude-*→anthropic、gemini-*→google、grok-*→xai、hy*→hunyuan 等
- 实测导入 23 个模型，自动分类到各供应商文件

### 🐛 Web 层健壮性修复
- **slowloris 读超时**：HttpServer 读请求阶段 10s 超时（防慢连接占满 32 连接槽）
- **PendingImages 跨槽位串扰**：静态单队列 → 按 agentId 分队列，Web 槽位 Agent 设唯一 AgentId
- **SSE 断连脏路径**：正常断连补 `Closed.TrySetResult()`；`WebSlot.Agent` 加 volatile

### ✅ 验证
- 自测 3507→3561 全绿（+54 断言）
- Web 服务 HTTP 200，opencode 在线导入实测通过

## v0.79.1 (2026-08-18) — 安全审计：白名单绕过 + 解析器崩溃护栏 + 并发/SSRF/路径防护

对全系统做 4 路并行安全审查（TUI 渲染 / Agent 并发 / 工具安全 / 手搓编解码），修复一批安全漏洞与健壮性问题。

### 🔒 安全（P0）
- **BashGuard 只读白名单绕过**：`find -exec/-delete`、`env <cmd>`、`git config` 被当作只读自动放行，提示注入可零确认执行任意命令。移除 `env`/`git config`，`find` 加 `-exec/-execdir/-delete/-ok/-okdir` 危险 flag 逐 token 拦截
- **路径防护 `PathSafety`（新增）**：拦截 SSH 密钥 / shell 配置 / 云凭据 / 系统凭据路径（`id_rsa`/`.bashrc`/`.ssh/`/`.pem`/`/etc/passwd` 等），接入 read_file / write_file / edit_file / rm
- **SSRF DNS 重绑定**：`SsgfGuard.CreateSafeHandler()` 用 `ConnectCallback` 原子「解析+校验+连接」，消除 CheckDns 校验后 HttpClient 再解析的 TOCTOU

### 🛡️ 崩溃 / DoS 护栏（P0/P1）
- **PdfParser** 内容流数组递归无深度护栏（StackOverflow）→ 加 128 层护栏；FlateDecode zip bomb → 64MB 解压上限；xref `count` 未钳制 → 钳制到文件大小
- **CfbParser** DIFAT 链 `numDifatSectors` 未校验（OOM）→ 钳制 + 循环内 `fatSectors` 上限
- **JpegCodec** 分量采样数组 ~800MB → 总采样点 ≤ 3×MaxPixels
- **find_replace** 正则无超时（灾难性回溯卡死主循环）→ 对齐 GrepTool 加 `RegexTimeoutSec`

### 🧵 并发（P1）
- **Messages 锁纪律**：主循环/压缩层无锁枚举与 Web 请求线程加锁写并发 → 4 处改快照/锁（压缩、FullMessages、RepairOrphanedToolPairs、DetectAndBreakLoop）

### 🖥️ TUI / 其他
- 输入/选择类对话框窗口宽不自适应（模板默认 30 列裁剪）→ `ApplyContentWidth`；Permission 对话框按钮被裁剪 → 高度公式修正
- `TuiAnimatedText.FrameIndex` 恒 0 bug + `DirectWriters` 静态列表泄漏
- `EnvScrubber` 凭据名覆盖扩展（`MYSQL_PWD`/`GOOGLE_APPLICATION_CREDENTIALS`/`DOCKER_AUTH` 等）
- `FileLockManager` 大小写不敏感系统锁绕过（字典 `OrdinalIgnoreCase` comparer）
- GUI 项目 WebAssets 重复包含（CS2002）

### ✅ 验证
- 自测 3507→3539 全绿（+32 断言）
- 主项目 + GUI 项目 0 警告 0 错误

## v0.79.0 (2026-08-18) — 本地模型（LM Studio / Ollama）集成可用性修复

用本地模型（LM Studio `qwen3.5-9b`）实测跑通编程任务时发现并修复 BaseUrl 端点归一化问题，补齐本地模型使用文档。

### 🔧 端点归一化（本地模型连接）
- `LLM.ResolveApiEndpoint`：BaseUrl 约定不含 `/v1`（自动追加 `/v1/chat/completions` 或 `/v1/embeddings`）；**兼容用户误传 `http://host:port/v1`**（剥离尾部 `/v1` 后再追加，避免 `/v1/v1/chat/completions` 400）。chat/completions ×2 + embeddings 三处统一走该方法
- **LM Studio 实测**：`qwen3.5-9b` 完整跑通「读文件 → 定位 bug → 修复 → 运行验证 → 汇报」agent 循环（~103s），自动修复了冒泡排序按名字而非年龄排序的 bug
- **上下文窗口坑**：完整模式请求（12K system prompt + 46 工具 ≈ 8695 token）超出 LM Studio 给模型配置的默认窗口（≈8192）会被直接拒绝（2s 0 token）→ **`--economy on` 精简 prompt 即可用**；或在 LM Studio 里调大 Context Length 用完整模式

### ✅ 验证
- 自测 3495→3498 全绿（+3 端点解析断言：无 /v1 / 误传 /v1 / 尾部斜杠）
- LM Studio `--base-url http://localhost:1234` 与 `http://localhost:1234/v1` 两种写法均实测通过

## v0.78.0 (2026-08-18) — 全部界面 `.tui` 标记化 + 资源内嵌 exe

把剩余的命令式对话框（TuiDialog.* 工厂）全部迁移到 `.tui` 声明式标记资源，并把标记资源统一收拢到 `UI/TUI/Raw/` 且嵌入程序集——「布局写标记、交互写 code-behind」架构闭环，单文件 exe 内可直接读资源。

### 📦 标记资源位置与内嵌
- **`tuidemo/` → `WayCoder/UI/TUI/Raw/`**：全部 `.tui` 资源（chat/colors/main/showcase + dialogs/*）移入项目内 `UI/TUI/Raw/`（git 历史保留）
- **`EmbeddedResource` 嵌入 exe**：`Raw/**/*.tui` 以逻辑名 `WayCoder.UI.TUI.Raw.<path>` 嵌入程序集，AOT 单文件 exe 内无需外部文件即可读
- `TuiMarkupPaths` 三路定位：`{BaseDir}/Raw/` 发布复制 → 向上找 `Raw/` / `UI/TUI/Raw/` / `WayCoder/UI/TUI/Raw/`（开发/预览热刷新）→ 嵌入资源兜底（已从仓库外目录实测通过）
- 新增 `TuiMarkup.LoadResource(name)` / `LoadResource(name, vars)`；全部窗口型界面（8 选择器 + 聊天屏）改走资源加载
- `WayCoder.Preview` 同步把 `Raw/**/*.tui` 复制到输出（`--colors` 等文件系统定位可用）

### 🖥 全部对话框迁移到 `.tui`（TuiDialog.* 工厂）
| 对话框 | 模板 |
|---|---|
| Info / Success / Warn / Error | `dialogs/info.tui`（共用，边框色/标题注入） |
| Confirm / Confirm3 | `dialogs/confirm.tui` / `dialogs/confirm3.tui` |
| Input / InputLine / Secret | `dialogs/input.tui` / `dialogs/inputline.tui` / `dialogs/secret.tui` |
| Select / MultiSelect | `dialogs/select.tui` / `dialogs/multiselect.tui` |
| Ask（LLM 提问） | `dialogs/ask.tui`（单选隐藏确定按钮） |
| Permission（权限确认） | `dialogs/permission.tui`（黄底黑字 + Y/N/A） |
| FindReplace（编辑器） | `dialogs/findreplace.tui` |
| **EditorScreen（终端编辑器）** | `editor.tui`（标题栏/文件列表/大纲/状态栏标记化，`TuiRichEditor` 编辑控件 code 注入 mainHBox） |

- **`/init` 默认生成 AGENT.md**（`ProjectInitializer.GenerateAgentMd` + `InitCommand` 写 AGENT.md；`/init claude` 生成 CLAUDE.md 兼容 Claude Code）；`ProjectContext.LoadInstructions` 指令加载新增识别 `AGENT.md`
- 模板支持 `{title}` 占位符注入；`msgBox` 容器由 code-behind 填折行消息标签（预览态显示占位）
- `Input` 标记新增 `password` 属性（Secret 掩码输入）
- `TuiView` 新增 `InsertAt(index, child)`（设置 Parent + 触发 OnCreate，供 EditorScreen 在 mainHBox 指定位置注入编辑控件）
- 预览占位注入守卫：`PopulateDesignPlaceholders` 仅对未定义 center 的标题栏（chat.tui）注入聊天示例数据，editor.tui 自带 `center="📝 编辑器"` 不被覆盖
- 全部沿用「`LoadResource` → `Find(id)` → 接线」模式，**交互/尺寸/事件逻辑零改动**

### 🐛 渲染修复
- **宽字符半格**：`○`/`▸` 等半宽符号被当宽字符只画半格 → `TuiGridPanel` 对宽字符做水平拉伸到恰好 2 格（真正的双宽字形几乎不变）
- **文字特征无效**：`FrameSnapshot` 丢弃 SGR 样式码 → 新增 `StyleAt` 捕获粗体/斜体/下划线/淡色，WPF 预览用 Typeface/TextDecorations/半透明还原
- **表格蓝/黑块**：`TuiTableList` cell 背景只设包装 VBox，内层 Label 仍继承灰底 → 递归 `SetCellBg` 传播行背景
- **聊天记录亮绿背景刺眼**：edit diff 新增/删除行由「白字绿/红底」改为「亮绿/亮红前景」去背景
- **预览对话框塌缩**：picker 类对话框模板缺显式 `height`，WPF 预览默认高 10 行裁剪内容 → 补默认高度（modelpicker/filepicker/commandpalette/keybindhelp/diffpreview/findreplace，真实 app code-behind 覆盖不受影响）
- **编辑器预览状态栏不见**：`editor.tui` mainHBox 加 `flex="1"` 撑满剩余高度，状态栏落到底部；真实 app 靠 flex 自适应 resize
- 修复 `TableList cell 颜色` 测试断言（背景传播后 SGR 合并码 `36;40m`）

### ✅ 验证
- 主项目自测 3493 全绿（含全部对话框构造 + 渲染 + 自适应尺寸断言 + EditorScreen 无头渲染冒烟）
- 嵌入资源路径实测：从仓库外 cwd 运行 `--test ui` 895/896 通过（唯一失败为依赖 cwd 的 Lint 定位测试）
- 预览 `--selftest` chat.tui / permission.tui / editor.tui 通过；构建 0 错误

## v0.77.0 (2026-08-18) — 独立 WPF `.tui` 预览程序 + InDesign 环境特性

新增独立 WPF 预览程序 `WayCoder.Preview/`：图形化渲染 `.tui` 声明式布局，边写边看；并为 TUI 元素引入设计/模拟环境特性。

### 🖥 WPF 预览程序（`WayCoder.Preview/`）
- **复用主项目渲染管线**：共享源码编译 `TuiMarkup`/`TuiDialog`/`TuiScreen` + `FrameSnapshot`，把 `.tui` 渲染成带色字符网格后 WPF 逐格绘制（等宽字体、黑背景）
- **实时预览**：打开文件自动渲染，文件保存自动刷新（FileSystemWatcher 防抖）
- **宽字符正确渲染**：修复 CJK/emoji 占 2 格时延续格残留背景盖住右半的问题（跳过延续格 + 2 格底色扩展）
- **屏幕尺寸模拟**：分辨率快选下拉（80x25 / 100x30 / 120x36 / 128x40 / 160x48 / 200x60 / 240x72）+ 行列增减按钮，模拟不同终端尺寸看布局自适应（XScale 比例窗口、min/max 钳制、居中）
- **缩放**：长滑块（25%~400%）+ 放大/缩小按钮（25/50/100/200/400% 档位）+ **Ctrl+鼠标滚轮**
- **网格开关**：显示/隐藏单元格网格线（设计期看格子边界）
- **无缝背景**：同色背景合并为整块矩形 + 整数像素边界取整，消除默认状态下的亚像素横/竖细线（不再像"有网格"）
- **内容居中**：小于视口时居中，大于时可滚动；纯黑背景
- **最近文件**：路径框改为可编辑组合框，记住最近打开 10 个文件（持久化到 `%LocalAppData%/WayCoder.Preview/recent.txt`）
- `--selftest` 无头自检（渲染非空网格），`--dump` 诊断转储

### 🧩 TUI 环境特性（InDesign / SimulatedScreen）
- `TuiBase` 新增 `InDesign`（设计/预览模式）+ `SimulatedScreen`（true=模拟/离屏渲染，false=物理终端），由 `TuiMarkup` 环境量注入到所有加载的元素（控件 + 窗口）
- 预览程序（`--tui-preview` / WPF）下为 true，正常 REPL 为 false——元素可据此区分设计态与运行态
- `TuiMarkup.InDesign` / `TuiMarkup.SimulatedScreen` 静态环境量，加载前设置即注入

### 🐛 渲染修复
- **高分辨率对话框消失**：`TuiScreen.WriteAt` 边界检查用 `Tty.Rows`（真实控制台高度），预览模拟更大屏幕时超过真实高度的行被跳过 → 改用屏幕高度 `TH`（真实 App 中两者相等，行为不变）。这是主项目真正的 bug 修复
- `FrameSnapshot.CharAt/ColorAt` 改 `public`（供外部渲染读取格子）

### ✅ 验证
- 主项目自测 3475 全绿（新增 InDesign/SimulatedScreen 传播断言）
- 预览：80x25~240x72 全尺寸对话框正常渲染、冒烟通过、`--selftest` 通过

## v0.76.0 (2026-08-18) — TUI 所有窗口型界面 `.tui` 标记化

把全部窗口型界面（选择器/快捷键/设置/Diff 预览）迁移到声明式 `.tui` 资源架构——「布局写标记、交互写 code-behind」，与主界面（v0.75.0 标记化）统一。

### 🧩 标记系统扩展
- `TuiMarkup` 新增 `ScrollView` 标签（设置页详情面板）+ `Separator vertical` 属性
- 资源定位复用 `TuiMarkupPaths`（发布输出优先 + 向上找 `tuidemo/`）

### 🖥 8 个界面迁移（新建 `tuidemo/dialogs/*.tui`）
| 界面 | 做法 |
|---|---|
| ModelPicker / FilePicker / SessionPicker / CommandPalette / ReasoningPicker | Dialog 壳+布局全标记化；搜索/表格列/列表项/键盘拦截/落盘 code-behind |
| TuiKeybindHelp | Window 壳标记化，列表项 code 填充 |
| SettingsScreen | Screen 壳标记化，schema 详情/高亮 code |
| DiffPreview | Dialog 壳标记化，DiffView 自定义控件 code 注入 |

- 全部沿用「`LoadFile` → `Find(id)` → 接线」模式，**交互逻辑零改动**（搜索/键盘/槽位/落盘保留）
- 编辑缓冲（TuiRichEditor）/TuiMenu/InlinePermission/Toast 保持 code（本质 code 驱动，标记化收益低）

### 🧹 清理
- 删除死代码 `DialogFrame.cs`（TuiWindow 渐变边框落地后无调用点）

### ✅ 验证
- 自测 3459→3475 全绿（新增 16 项：8 个 `.tui` 加载 + 关键 id 断言）
- `--tui-preview` 各对话框渲染正确；构建 0 错误

## v0.75.0 (2026-08-18) — 标记界面重构：`.tui` 声明式布局 + `--tui-chat`

主聊天界面从 C# 硬编码布局迁移到声明式 `.tui` 资源文件——「布局写标记、交互写 code-behind」，向"写资源文件+交互代码就能实现界面、还能预览"架构推进。

### 🧩 标记系统扩展（TuiMarkup）
- 新增 4 个标签：`ListView`（autoScroll/itemSpacing）、`DynamicBar`、`PromptBar`（maxVisible/itemHeight/separatorColor）、`SidePanel`（borderWidth/borderColor/panelVisible）
- 补齐属性：`TitleBar.gitBranch`、`TextArea.placeholder/showLineNumbers`、`Markdown.role/plainText/isError`、`Separator.lineChar/lineColor`、通用 `floating`
- 新增 `TuiMarkupPaths`：标记资源定位（发布输出优先 + 向上找 `tuidemo/`）；`TuiMarkupDemo` 复用之

### 🖥 标记版主界面（MarkupChatScreen）
- 新建 `tuidemo/chat.tui`：声明完整聊天布局（标题栏/聊天列表/侧栏/建议面板/提示栏/动态栏/分隔线/输入区/状态栏 9 子视图）
- 新建 `MarkupChatScreen : ChatScreen`：覆写 `BuildLayout` 从标记加载、`Find(id)` 赋给基类属性，**复用全部交互逻辑**（槽位/会话/流式/侧栏/输入/对话框）
- `ChatScreen.BuildLayout` 改 `protected virtual`、子视图属性改 `protected set`；新增 `BuildLayoutPreservesChatItems` + `RebuildChatItems` resize 钩子（复用 ChatList 时按新宽重建项内容，不重灌）
- `ComputeLayout`/`ApplyDynamicSizes` 抽出，Render 与 OnResize 共用

### 🚪 入口
- `--tui-chat` 参数：强制用标记版界面启动（测试入口）
- `WAYCODER_MARKUP_UI` 配置开关：控制默认用新/旧界面（默认关闭，测试通过后翻默认）
- `WayCoder.csproj` 把 `tuidemo/**/*.tui` 复制到发布输出

### ✅ 验证
- 自测 3426→3459 全绿（新增 30 项：4 新标签解析、属性补齐、chat.tui 完整加载、MarkupChatScreen 无头渲染冒烟）
- `--tui-preview tuidemo/chat.tui` 可渲染；构建 0 错误

## v0.74.0 (2026-08-18) — TUI 对话框背景快照还原 + 标题栏分隔线移除

对话框关闭后背景恢复改为「快照贴回」，从根上消除残留条带；所有窗口/面板的标题栏下方分隔线移除。

### 🪟 对话框背景快照 + 贴回
- **新增 `FrameSnapshot`**：模态对话框显示前截取覆盖区域背景（逐格 字符+前景色+背景色），关闭后整块贴回还原
- **根治残留条带**：原关闭路径是「默认背景清屏 + 裁剪重绘」，底色不是默认色或重绘漏掉干净控件时会残留颜色不一致的长条；快照含真实底色，贴回后完全还原
- **三种兜底**：① 贴回在根视图重绘之前 → 动画/实时内容随后以最新帧覆盖；② 多层窗口关闭仍走整屏重绘；③ 终端 resize 时快照作废，回退重绘
- 颜色感知 ANSI 解析（16/256/TrueColor、宽字符），贴回按行同色合并

### 📏 标题栏下方分隔线移除
- `TuiWindow`/`TuiPanel` 标题嵌在上边框行，其下 `├───┤` 分隔线移除，内容直接从标题行下开始
- `ContentTop`/`ContentHeight` 几何统一（带边框 Y+1 / Height-2），全量渲染与增量渲染路径一致（修复此前「有的窗口不画线但留色差长条」）
- `TuiDialog`/`TuiMenu` 高度公式同步去掉分隔线占行

### 🔧 重构与调整
- `TuiWindow.Border` → `BorderStyle` 属性改名（全库），与 `GetBorderChars` 元组解耦、语义更明确
- `TuiDialog` 消息框尺寸调整：`MinDialogW 24→12`、`MinDialogH 7→3`，消息框固定高度 7 行（内容区 5 行）
- `TuiView` 新增 `GetMaxWidth()` / `GetTotalHeight()` 子控件尺寸辅助
- Tiny 模式改为仅显式启用（移除「模型窗口 <128K 自动进入」逻辑）+ 删除 `.env.example`

### ✅ 验证
- 自测 3426 项全绿（新增 12 项 FrameSnapshot 单元测试 + 13 项「模态窗口已截取快照」集成断言 + UI Lint）
- 构建 0 错误

## v0.73.0 (2026-08-18) — 声明式 TUI 自定义单元格（DataList/TreeView/TableList cell 模板）

三个列表控件支持用 `.tui` 文件片段做自定义单元格模板，配合 `{key}` 占位符数据绑定，向「声明式布局 + code-behind」架构靠拢。

### 🧩 占位符绑定 + LoadCell
- **`TuiMarkup.Load` 支持 `vars` 参数**：`{key}` 占位符替换（AsyncLocal 并发安全），文本/颜色属性通用，同一模板可并发渲染不同行数据
- **`LoadCell` 叶子根修复**：根为叶子控件（Label/Button 等）时自动包装进 `TuiVBox`，此前强转 `(TuiView)` 抛 InvalidCastException 致 cell 模板从未真正生效、每次走 catch 兜底截断
- 渲染前 `OnResize` 触发布局，避免子控件堆叠

### 📋 三列表控件 cell 模板
- **TuiDataList**（新增）：每行用 cell 模板渲染（`{text}/{index}/{key}` 占位符）；标记 `<DataList items cell height selected/>`
- **TuiTreeView**：`CellMarkup` 接入，`items="文档>概览,文档>入门"` 路径语法自动建树（中间节点 `IsExpanded=true`）+ `cell` 属性（`{text}/{icon}/{depth}` 占位符）
- **TuiTableList**：`CellMarkup` 每列一个 cell（宽度=列宽，`{value}/{colN}/{text}/{index}` 占位符）；`ClampCellWidths` 递归约束子控件宽度 ≤ 列宽，防 `DrawLine` 直接写屏不裁剪导致串列；选中反白先画整行背景，cell 透明处透出
- 标记注册：`<TableList columns="名称:10,型号:14" items="a,b|c,d" cell showHeader selected/>`

### ✅ 验证
- 自测 3397→3412 全绿（新增 TableList/TreeView cell 渲染与颜色断言）
- 构建 0 警告 0 错误；`tuidemo/showcase.tui` 新增「⑥ 自定义单元格」预览区（DataList `▸` 前缀 / TreeView `📄` 树线 / TableList 全绿加粗）

## v0.72.0 (2026-08-18) — GUI 图形界面上线（Avalonia 12）

对标 Web 版落地原生图形界面，`waycoder --gui` 启动。

### 🖥 GUI 架构
- **独立 JIT 进程**：`WayCoder.Gui` 项目（Avalonia 12.1.1），与主程序 NativeAOT 单文件分离——Avalonia 依赖反射无法 AOT，故 GUI 走独立 JIT 进程，主程序 AOT 不变
- **核心抽库（shared-source）**：GUI 项目 `<Compile Include>` 复用主项目核心源码（Agent/LLM/Tools/Infra/Memory），排除 CLI 入口/命令层/测试/插件/斜杠命令；`CoreStubs.cs` 占位桩使核心在 GUI 进程编译通过
- **`--gui` 参数**：主程序定位并拉起独立 GUI 进程（dev 走 dotnet dll / publish 走可执行）

### 🖥 GUI 功能
- 聊天区 + 输入框 + 发送/停止（流式 onToken 回填，Ctrl+Enter 发送）
- 模型下拉切换（ModelCatalog.All）
- F1-F10 多槽位（独立 Agent + 各自历史 + 独立中断令牌）
- **完整交互桥**（GuiInteraction 注入 UxHelper.WebInteraction）：权限确认（允许/全部允许/拒绝）、文本提问、单选/多选、diff 预览确认（接受/拒绝全部）
- **Markdown 富文本渲染**：代码围栏/标题/列表/引用/`«color»…«/»` 颜色标记/粗体/行内代码，颜色同源 AnsiColors（三端观感一致）
- **会话持久化**：退出保存 `_auto`/`_auto_slotN`，启动恢复
- 工具输出显示（代码块 + 按码点截断）

## v0.71.34 (2026-08-18) — 图片/记忆/外围 + 全库六路并行审计 18 项确定性 bug

本轮完成 #275 图片编解码/记忆/外围 bug + 六路并行全库审计（核心 LLM/Agent、记忆/会话、文件工具、网络/进程工具、UI 基础/编辑器、UI 控件）修复 18 项确定性 bug。

### 🖼 图片编解码 + 记忆/外围
- **JpegCodec/PngEncoder/BmpCodec 编码尺寸守卫**：宽高乘积溢出 int、JPEG 宽高 >65535 被 `(ushort)` 静默截断
- **WatchMode 块注释吞行注释**：`/* ... */ // AI! x` 的 `// AI!` 被吞，块注释结束后继续解析行尾
- **StructuredMemory**：`ParseFrontmatter` 未闭合/无尾随换行时正文污染；`SetShared` 只翻标志不移文件致 push/pull 路径不一致；`ListAll` 槽位/共享优先级与 `Get` 反转
- **BatchRunner git clone 注入**：`"` 字符串拼接可注入 `--upload-pack`/`--config`，改 `GitRunner.RunArgsAsync` 参数列表逐参传递
- **ProjectContext 前缀匹配误伤**：`StartsWith("bin")` 误伤 `bin-tools/`、`objc/`、`.gitignore`，改按路径段精确匹配

### 🔧 文件/进程工具
- **MultiEditTool CRLF 不归一化**：多行编辑在 CRLF 文件上永远匹配失败；同时 diff/RecordChange 行尾不一致
- **EditFileTool CRLF diff 错乱**：LF content vs CRLF newContent 逐行比较把整文件误报为改动
- **TodoTool 持久化路径**忽略 cd 跟踪，与 struct_todo 读写不同位置
- **PsTool `top+1` 溢出**：无上限时 `top == int.MaxValue` 溢出为负
- **MvTool/CpTool 深目录静默丢数据**：`CopyDirectory` 深度 >64 静默 return，mv 随后删源致文件丢失
- **GitPRTool `2>&1` 字面参数**：无 shell 下 `2>&1` 成为 git 参数，push/远程检测失效
- **LintTool Java 单文件永远 gradle**：`FindProjectFile` 回退返回 target 致 `File.Exists` 恒真，maven/javac 不可达

### 🛡 安全
- **BashGuard 前缀/子 shell 绕过**：`env sudo apt`、`(sudo apt)` 只查首 token 绕过黑名单，加 pass-through 包装命令跳过 + 子 shell 括号剥离

### 🧠 核心 LLM/Agent
- **LLM 正文超时被误判为取消**：正文 300s 超时抛 OCE，`FallbackLLM` 的 `ex is not OCE` 过滤器当作用户取消放行，回退链失效；改判 `ct.IsCancellationRequested`
- **DetectAndBreakLoop 绕过 MessagesLock**：直接 `messages.Add` 与 `SnapshotMessages` 并发抛异常，改用持锁 `AddMessage`
- **Architect 计划失败回滚删错消息**：`RemoveMessageAt(Count-1)` 删的是 hook 注入上下文而非用户消息
- **Agent.Feedback 自动测试 `2>&1` 字面参数**：无 shell 下成为子进程参数

### 🎨 UI
- **TuiInputHistory Escape/Unescape 顺序颠倒**：`C:\new` 重启后历史被破坏（`\n` 误还原成换行），改单遍扫描
- **TuiScrollView 裁剪区覆盖父容器**：`ClipTop/ClipBottom` 未与父裁剪取交集，嵌套滚动视图越界绘制
- **RenderBuffer.WriteWrap 死循环**：`indentCol > maxCol` 时 `avail` 恒 ≤0 永不推进
- **TuiTextArea 自动换行代理对切半**：硬断点落在 emoji 代理对中间切出 U+FFFD，加 `SafeBreakCol` 回退

### 🧪 测试
- 新增回归测试 15 项（图片编码守卫/frontmatter 边界/Watch 块注释/ProjectContext 路径段），自测 3363 → 3388 全绿

## v0.71.33 (2026-08-18) — 绘图/工具/编辑器/Infra 确定性 bug + 对话框内容自适应 + Show 接口

本轮继续确定性 bug 修复，并落地对话框「内容自适应宽高」与仅绘制 `Show` 接口。

### 🛡 绘图引擎（DoS/OOM）
- **DrawImage 无界循环**：w/h 过大（如 2e9）致数十亿次无效迭代，钳制到画布内
- **直线跨度溢出死循环**：跨度 > int.MaxValue/2 时 `2*err` 溢出为负、收敛永不满足，提前拦截
- **圆非整数半径 NaN**：dy=±r0 超半径时 `r²-dy²` 为负开方出 NaN，钳制为 0
- **star/regular 顶点数无上限**：> 4096 拒绝（此前可传 2e9 致内存/时间爆炸）
- **TryNum 拒绝 NaN/Infinity** 注入几何计算 + **DrawTool grid 上限 256×256**

### ✏ 编辑器 EditorCore
- 单行删除撤销未还原行号 → 连续撤销跨行时光标停在错行
- 替换串/替换结果含换行拒绝（破坏「一行一条目」不变量，撤销 DeleteBlockAt 越界崩溃）
- OutlineExtractor 命名组误跳过起始于第 0 列的名称组（改判 Group 0 而非 Index 0）

### 🛠 工具
- **FindReplaceTool**：负值 maxFiles/maxPerFile 钳制；花括号 glob `*.{md,txt}` 展开（.NET GetFiles 不认花括号静默 0 匹配）+ 去重
- **DiffTool**：context 钳制 0..10000 防 `i±context` 整数溢出
- **MultiEditTool**：首编辑计入成功数；additions/removal 钳制 ≥0 防「+3/--3」双负号
- **TreeTool**：截断标记修正（恰等于 max 误报上限）；先剔除隐藏项再判 isLast
- **ExportTool / StructTodoTool**：相对路径基于被跟踪 cd 工作目录而非进程启动目录

### 🧱 基础设施
- **ErrorLog 并发丢行**：Flush/Write 在锁外并发 AppendAllLines 交错丢行 + 日期轮转竞态，加独立追加锁
- **FileIgnoreManager `**` 零目录匹配**：`a/**/b` 无法匹配 `a/b`，新 GlobToRegex 支持零目录
- **SnippetStore 路径穿越**：name 含 `../` 写出 snippets 目录之外 + 空名/纯点兜底 unnamed
- **FileTracker LRU**：重读热点文件反复淘汰无关条目致追踪集收缩，先判 ContainsKey
- **Logger**：指标订阅者抛异常中断日志 worker，加 try/catch
- **RasterImage.SampleGrid**：cols*rows / cx*Width 整数溢出防护

### 🎨 UI 配色修正
- **AnsiColors**：补 Orange=208 / Orange3=172 真实色值（此前 orange 错用 33 黄）；窗口默认底改灰底 PanelGrey、输入/列表默认黑底
- **对话框正文**：新增 DialogFg（灰底黑字，黑底主题覆盖为亮色），DynamicBar/List/TableList 背景改用黑底

### 🖼 对话框内容自适应 + 仅绘制 Show 接口
- **`TuiDialog.Show(win, x, y)`**：仅绘制对话框到 ANSI 字符串（不进入输入循环、不响应按键），x/y 可指定坐标或居中，供抓屏调试
- **`--dialog-show` 调试命令**：渲染 1~6 行消息 + 确认框 + 指定位置示例
- **消息/确认框宽高自适应**：`AutoSizeMessageDialog` 按消息最宽行与按钮行宽算宽、按折行数算高；`Info/Success/Warn/Error/Confirm/Confirm3` 全部改用，修复 4 行以上消息裁掉按钮、第 5 行起折叠不可见
- **按钮不再撑满**：确认框按钮去掉 Flex=1，用自然宽度居中

### 🧪 测试
- 新增回归测试 10 项（消息/确认框自适应宽高 + Show 帧），自测 3363 → 3373 全绿

## v0.71.32 (2026-08-17) — 全库确定性 bug 修复 + v0.71.31 重构回归修复

本轮为 4 路并行全库审计（工具 / UI / Agent 核心 / 基础设施+Web）的结果落地，共修复 16 项确定性 bug，并补齐 v0.71.31 大重构引入的自测回归。

### 🔧 核心 Agent / LLM
- **Architect 双模型 `ModelOverride` 泄漏**：每轮 `ChatAsync` 开始前复位 `ModelOverride`，此前第二轮起计划生成分支静默失效，且费用按小模型低估
- **`/compact` 压缩快照副本**：改传活 `Messages` 列表，此前就地 Clear/Add 只作用于副本、真实上下文一条不少
- **`_fastMode` 永不复位**：改为每轮直接赋值，快速模式只影响当轮、不再污染后续消息
- **`LlmMaxRetries` 语义错位**：总尝试次数改为「重试次数 + 1」，默认 5 次重试现在真正走 1x→6x 超时倍率（此前只到 4x）

### 🛠 工具
- **MvTool 数据丢失**：源与目标同路径（如 `mv file.txt .`）且 overwrite=true 会先删源，加同路径拦截
- **NotebookEditTool `cell_index` 溢出**：long/double 强转 int 截断为负误插开头，改用 `ToolArgs.GetInt` 钳制
- **ScreenshotTool**：删除未钳制的本地 `GetInt`，region 参数统一走 `ToolArgs.GetInt`
- **WcTool 字符数**：改按码点（Rune）计数，emoji/CJK 扩展 B 不再翻倍

### 🎨 UI
- **压缩进度条写错变量**：`ChatScreen` 进度条写 `barText` 而非恒空的 `StatusText`
- **TuiScrollView 渲染未传裁剪区**：子控件渲染补传 Clip 参数，滚动时内容不再溢出视口
- **TuiScrollView 布局未递归**：嵌套视图子控件高度未计算，补递归 `Layout()`
- **非容器子控件 `Parent` 指向自身**：`Parent` 类型放宽为 `TuiControl?`，TuiButtonGroup/TuiTabs 子控件坐标链含组自身偏移（此前鼠标命中错位）
- **ControlRenderer.DrawBarLine**：`filled` 防御钳制，防 `new string(char, 负计数)` 抛异常

### 🧱 基础设施 / Web
- **TrueTypeFont 静态缓存线程安全**：加锁串行化解析，多槽位并行渲染字体不再破坏 Dictionary 内部状态
- **HooksManager 正则 ReDoS**：matcher 加 250ms 超时，灾难性回溯回退精确匹配
- **WebChat SSE 写阻塞**：已剔除客户端跳过写 + 观察被放弃写任务异常，慢客户端不再逐 token 拖死 Agent 流式线程

### ✅ v0.71.31 重构回归
- TuiMarkdown 快速回退路径：单段落无特殊字符时不再返回不折行，长段落正确按宽度折行
- TuiWindow.ContentTop 与渲染器内容起始行统一为 Y+2（标题嵌上边框 + 分隔线）
- TuiScreen 清理两处 drawTitle 死代码
- 对话框标题位置 / CdTool 测试断言更新

### 🧪 测试
- 新增 v0.71.32 回归测试 4 项（mv 源=目标拦截 / wc 码点计数 / 非容器子控件 Parent 指向自身），自测 3342 → 3346 全绿

## v0.71.31 (2026-08-17) — UI 命名空间重构 + 新布局引擎 + 行尾统一

本轮为大型结构重构：UI 代码从顶层 `WayCoder` 命名空间收拢到 `WayCoder.UI.*` 分层，引入 HBox/VBox 弹性布局引擎，并统一全仓库行尾为 CRLF。

### 🔧 重构

**UI 命名空间分层**
- 所有 UI 代码由顶层 `WayCoder` 收拢到 `WayCoder.UI.*`：`UI/Shared`（颜色/文本工具）、`UI/TUI/Base`（基础控件与屏幕管理）、`UI/TUI/Controls`（控件库）、`UI/TUI/Custom`（对话框/选择器）、`UI/TUI/Renderers`（工具输出渲染）、`UI/TUI/Screens`（屏幕）、`UI/TUI/Edit`（编辑器）
- `TuiColors` → `AnsiColors`、`TuiHelper` → `AnsiHelper`：类名与 ANSI 职责对齐
- `UI/TUI/ToolRenderers/` → `UI/TUI/Renderers/`：7 个工具渲染器随目录重命名

**新增布局引擎**
- `TuiHBox`（水平弹性容器）+ `TuiVBox`（垂直弹性容器）：子控件按 `Flex` 权重分配剩余空间，`Spacing` 控间距，浮动控件不参与流式布局
- `TuiScrollView`（滚动容器）、`EHAlign`/`EVAlign`（水平/垂直对齐枚举）
- 控件新增 `Flex`/`Floating`/`Margin` 属性支撑流式布局

**提示条目模型**
- `PromptItem` + `EPromptKind`（Command/File/Shell…）拆分输入提示条目类型

**移除遗留源码**
- 删除 `QBasic/` 独立 QBasic 解释器/IDE 源码（未纳入主解决方案）

**行尾统一**
- 全仓库约 277 个文件 LF → CRLF（.cs/.md/.yaml/.sh 等），消除跨平台 diff 噪声

**打包与工具**
- winget 清单新增 0.71.29、更新 0.71.4 / 0.69.0 历史清单，`packaging/`、`scripts/` 同步
- 新增 `check/deepseek-harness/`（DeepSeek 测试 harness）、`WayCoder/snake.py`（tkinter 贪吃蛇示例）

## v0.71.30 (2026-08-17) — 四路确定性修复：CLI 队列/路径遍历 + 时间单位/整数溢出 + LLM/工具/记忆边界 + Web 作用域

延续四路并行审计（Program/批量、Infra、工具/LLM、Web）后人工验证，本轮修 **19 项**确定性缺陷，覆盖 CLI 参数累积、路径遍历、时间单位错配、整数溢出、LRU 淘汰缺失、单字记忆召回、Web 多标签页作用域等。

### 🐛 修复

**CLI/批量/命令层（7 项）**
- **`-pN` 多值参数二次追加丢首值**：`CliArgRegistry` 解析 `AllowMultiple` 参数（如 `-p1 "A" -p1 "B"`）时，第二次出现走覆盖分支而非追加，只剩 `[B]`。已存在时改 `AddRange` 追加
- **`-pN` 同槽位排队任务竞态丢弃**：`StartSlotTask` 是 fire-and-forget，循环里多个任务同槽位排队时未等待前一任务完成即派发下一任务。派发后 `await` 槽位任务句柄，保证顺序执行
- **`BatchSpec.DisplayName` 路径遍历**：直接用未清洗的 `Name`/`Repo` 拼 `jobs/<名>_<随机>` 目录名，`Name="../x"` 可逃逸工作目录。改 `SanitizeName` 清洗
- **`ReviewMode` 报告截断后长度虚低**：输出截断后才取 `output.Length`，报告「已输出 N 字符」实际是截断后的值。截断前捕获原始长度
- **`BackgroundTask` 同款截断长度虚低**：与 ReviewMode 相同缺陷，报告用截断后的 `content.Length`。截断前捕获原始长度
- **`ProjectContext` HEAD 分支解析错误**：`ref: refs/heads/xxx` 用 `LastIndexOf('/')` 前只取首个 `/` 后片段，带前缀的 ref 分支名错乱。正确剥 `refs/heads/` 前缀
- **`WorkReporter` 转义引号误判**：解析 bash 命令参数引号时未统计引号前的反斜杠个数，`\"` 前奇数个反斜杠时应视为转义而非引号闭合。补反斜杠奇偶计数

**基础设施层（5 项）**
- **`PersistentShell` 空闲超时单位错配**：`TimeSpan.FromMinutes(5).Ticks`（100 纳秒 = 3e9）与毫秒时间戳比较，5 分钟回收实际约 34.7 天。改毫秒常量 `5L*60*1000`
- **`DrawCanvas.FillRoundRect` 圆角越界**：圆角半径 `r` 未钳制到 `min(w,h)/2`，超界时圆角矩形顶点越界绘制。统一 `Math.Clamp` 到半宽/半高
- **`DrawCanvas` Bresenham 坐标 int 溢出**：`Math.Abs(x1-x0)` 用 int 减法，大坐标（超 2^31）溢出为负。改 long 计算 + 超 `int.MaxValue` 直接 return
- **`UpdateChecker` 版本号数字溢出**：`int.Parse(digits)` 对超长版本段（如 `99999999999`）抛 `OverflowException` 崩溃。改 `long.TryParse` + `Math.Min` 钳制
- **`FileTracker.RecordWrite` 无 LRU 淘汰**：`RecordRead` 有淘汰逻辑而 `RecordWrite` 缺失，`Tracked` 无限增长。补与 `RecordRead` 一致的淘汰块

**LLM/工具/记忆层（3 项）**
- **`LLM` 重试次数为 0 丢兜底**：`maxRetries > 0 ? maxRetries : Math.Max(1, ...)` 在显式传 `0` 时正确走兜底，但旧实现 `0` 直接生效导致不重试。对齐 `Math.Max(1, Config.LlmMaxRetries)` 兜底
- **`NotebookEditTool.InsertCell` 索引溢出**：`(int)(afterIndex + 1)` 在 `afterIndex` 大值时溢出为负。改 `Math.Clamp((long)afterIndex + 1, 0, cells.Count)`
- **记忆单字查询静默失效**：`SemanticMemory.GetRelevantContext` / `StructuredMemory.GetRelevantContext` 对单字 CJK 查询（如「汉」）与多字记忆的 bigram 无交集 → TF-IDF 得 0 分直接返回空，相关记忆注入系统提示词静默失效。补子串兜底（与 `StructuredMemory.Search` 对齐），只在大分无结果时触发，不影响 bigram 精度

**Web 层（4 项）**
- **前端 `?client=` 拼接重复 `?`**：`cq()` 无条件拼 `?client=`，`/upload?kind=xx` 等已含 `?` 的 URL 会拼成 `?kind=xx?client=xx`，clientId 丢失导致页面槽位错绑。改 `indexOf('?')` 判断用 `&`/`?`
- **SSE 全量回退残留 clientId 映射**：`/events` 全量回退（非流式）时未从 `_clientSlot` 移除该 client，后续请求沿用脏映射。回退前清理
- **`WebChat.ParseClientQuery` 非法转义崩溃**：`Uri.UnescapeDataString` 对非法百分号转义抛 `UriFormatException`。改 `SafeUnescape` 包裹 try/catch
- **`WebChat.Interaction` 上传同款转义崩溃**：`HandleUpload` 用裸 `Uri.UnescapeDataString`。改 `SafeUnescape`

### ✅ 测试

新增 `TestV0730Deterministic`（5 项断言：AllowMultiple 多值累积、BatchSpec 路径清洗、ProjectContext HEAD 分支解析、单字查询子串兜底命中、FillRoundRect 圆角钳制）。测试总数 3327 → 3336。

## v0.71.29 (2026-08-17) — 四路审计收尾：工具参数钳制 + 时间单位/溢出 + UI 负计数崩溃 + 上下文/LLM 边界

四路并行审计（Agent 核心/上下文会话/Tools/UI 编辑器）后人工验证，本轮修 **23 项**确定性缺陷，横跨工具层、UI 渲染、上下文压缩、LLM 客户端四层。

### 🐛 修复

**工具层（9 项）**
- **整数参数静默截断**：`ToolArgs.GetInt` 用 unchecked `(int)l`/`(int)d`，超 `int.MaxValue` 的 long（如 `limit=3000000000`）截断为负值、`double.NaN` 得 `int.MinValue`，全工具整数参数结果错误。改 `Math.Clamp` 钳制 + 非有限值回退默认
- **LSP 空闲会话永不回收**：`LastUsedTicks`（毫秒）与 `TimeSpan.FromMinutes(5).Ticks`（100 纳秒=3e9）比较，5 分钟回收实际约 34.7 天，空闲 language server 进程长期泄漏。改毫秒常量 `5L*60*1000`
- **`bash` 超时整数溢出**：`Task.Delay(timeout * 1000)` 在 timeout ≥ 2.1M 秒溢出为负/回绕。改 `TimeSpan.FromMilliseconds(Math.Clamp((long)timeout*1000, 0, int.MaxValue))`
- **`kill`/`web_search` 参数解析**：`Convert.ToInt32` 对超范围 long 抛 `OverflowException`、对 double 银行家舍入、对非数字串抛 `FormatException`。统一改 `ToolArgs.GetInt`
- **`read_file` Markdown 行数 off-by-one**：`text.Split('\n')` 未去末尾空元素，`行 X-Y / N` 多报 1（对齐 `ReadTextFile` 的去尾空逻辑）
- **`multiedit` 创建文件行数虚增**：`CountLines = Split('\n').Length`，`"a\nb\n"` 报 3 行实际 2 行。改 `TrimEnd('\n')` 后计数
- **`tree` 末条可见项误判**：`isLast` 在 `continue` 跳过隐藏项前计算，目录末尾是隐藏项时输出 `├──` 而非 `└──`。先 `RemoveAll` 隐藏项再算
- **`find_replace` 匹配文件数虚增**：用 `sb.ToString().Split("###").Length-1` 数子串统计，内容含 `###` 时虚增。改独立计数器

**UI 层（5 项）**
- **`new string(char, 负计数)` 崩溃**：`TuiPromptBar.WriteBorder`/选中填充、`TuiScreen.RenderWindow` 边框绘制用 `width-2`/`win.Width-2` 无钳制，宽度 <2 时抛 `ArgumentOutOfRangeException`。统一 `Math.Max(0, …)`
- **编辑器跳列切半代理对（数据损坏）**：`EditorCore.JumpToLineCol` 列钳制到 `char` 长度但不做代理对修正，列落 emoji/CJK 扩展 B 中间后 `InsertText` 把代理对切成两半、保存时编码回退 U+FFFD 破坏文件。补向右对齐修正
- **`TuiHelper.TruncateByWidth` 窄宽超宽**：省略号（宽 2）在 `maxWidth=1` 时放不下仍返回 `"…"`（宽 2 超 1）。退化无省略号截断
- **`BoxBuffer` 省略号预留 off-by-one**：`TruncateByVW(text, maxLen-2)+"…"` 在 `maxLen≤2` 时超宽。抽 `TruncateWithEllipsis` helper 处理窄格

**上下文/记忆层（4 项）**
- **第 2 层摘要死条件**：`messages.Count > 10` 闸门与 `SummarizeOldAsync` 的 `keepRecent=20` 矛盾，11-20 条时外层进但内层立即 return false（先报「正在摘要」再空转）。对齐 `> 20`
- **`ContextManager` 构造除零**：`maxTokens≤0` 未防御（`UpdateMaxTokens` 有），阈值全 0 + `ReportProgress` 除零得 NaN 污染进度条。回退默认窗口
- **记忆 description 换行截断**：`WriteFile` 不转义 description 换行，读回只保留第一行丢内容。`ReplaceLineEndings(" ")`
- **MEMORY.md 槽位断链**：`RebuildIndex` 统一写 `memory/{name}.md`，槽位记忆实际在 `slot_N/` 子目录。改 `Path.GetRelativePath` 按实际 FilePath 生成

**Agent/LLM 层（5 项）**
- **`GetEmbeddingAsync` 泄漏响应**：`CallWithRetryAsync` 返回的 `HttpResponseMessage` 未 `using`，每次嵌入调用泄漏连接。补 `using var`
- **费用按错模型计价**：`TaskCost`/`EstimatedCost` 用 `Model` 而非 `EffectiveModel`，Architect 模式切 `SmallModel` 后费用仍按大模型算，预算决策错误。改 `EffectiveModel`
- **`Retry-After` 远未来日期溢出**：`(int)TotalMilliseconds` 对 >24.8 天溢出为负，回退默认退避（graceful 但错）。先钳制到 maxWaitMs 再转 int
- **`_effectiveMaxRounds` 可 0/负**：`Config.MaxRounds≤0` 时循环体一次不跑，输出「已达 0 轮上限」。下限 1
- **`SystemPrompt` git 命令死锁**：先同步 `ReadToEnd()` stdout 再等退出，stderr 写满 4KB 缓冲时进程阻塞、stdout 永不关闭永久卡死。改并发读双流 + 超时 kill

### ✅ 测试

新增 `TestV0729BoundsAndRunes`（7 项断言：超范围 long 钳制/NaN 回退/负 long 保留、跳列代理对向右对齐 + 插入不切半、1 列截断不超宽、MaxTokens≤0 回退）。测试总数 3320 → 3327。

## v0.71.28 (2026-08-17) — 图片编解码损坏输入越界读 + Config/记忆/命令 6 项确定性修复

延续 bug 修复循环，本轮修**图片编解码损坏输入越界读**（JPEG/BMP 解析恶意/损坏字节流时缺边界校验，`IndexOutOfRangeException` 而非干净 `FormatException`）+ 6 处确定性缺陷。

### 🐛 修复

- **JPEG 解码越界读**（5 处）：`JpegCodec.Decode` 对损坏输入读段内容前不校验边界，越界读抛 `IndexOutOfRangeException`（未拦截的异常类型，会在工具层外暴露）：
  - SOF0 段长度=2（空 payload）：先读 height/width/nComp 再校验 → 越界读 `data[pos+5]`
  - SOF0 分量数 nComp=2：旧的 `nComp<1||nComp>4` 放行，后续 `comps[2]` 越界；改 `nComp==2||nComp<1||nComp>4` 拒绝（仅支持 1/3/4 分量）
  - DQT/DHT 段截断：连读 64 字节量化表 / 16 字节 bits + vals 前不校验 `p+65`/`p+16`/`p+total` 是否越过 `end` → 越界读
  - SOS 段截断：按 Ns 循环读分量条目前不校验 `pos+1+2*n+3` → 越界读
- **BMP 解码越界读**：`BmpCodec.Decode` 读 `dataOffset` 后不校验其合法性，负值/超出文件长度 → 越界读。补 `dataOffset < 0 || dataOffset >= data.Length` 抛 `FormatException`
- **TrueTypeFont BE16/BE32 越界读**：`BE16/BE32` 只校验上界不校验下界，负偏移越界读。补 `off >= 0` 下界判定
- **Config 环境变量非法值崩溃**：`WAYCODER_MAX_TOKENS=abc` 等非法值直接抛异常导致启动崩溃。env setter 包裹 try/catch，非法值忽略保留默认值
- **Config 默认模型名不一致**：`Model`/`SmallModel` 的 `DefaultStr` 为 `"deepseek-chat"` 但属性默认值为 `"deepseek-v4-flash"`，文档与代码不一致。统一为 `"deepseek-v4-flash"`
- **StructuredMemory.Delete 槽位/共享双目录查找缺失**：删除时只查槽位目录，共享记忆删除失败。对齐 `Get` 的双目录回退查找
- **SharedMemoryManager 更新检测恒空**：拉取后用 mtime 对比判定「更新」，但 `Get` 读的是检出后文件、`UpdatedAt` 恒等于新 mtime，比较恒 false。改检出前后内容对比
- **ModelCatalog HttpClient 响应未释放**：`client.PostAsync` 结果未 `using` 释放 `HttpResponseMessage`
- **DebugCommand 别名自相矛盾**：`/debug-on` 为主名、`/debug-off` 为别名，但两者都走同一 `ExecuteAsync`（toggle 同一逻辑）。拆分为 `DebugOnCommand`/`DebugOffCommand` 两个独立命令
- **SessionCommand 分页溢出**：`(page-1)*limit` 无上界，`--page 2147483647` 溢出为负。改 `Math.Min((long)(page-1)*limit, int.MaxValue)`

### ✅ 测试

新增 `TestV0728CodecBounds`（4 项断言：截断 SOF0/SOS/DQT 抛 `FormatException`、BMP 负 dataOffset 抛 `FormatException`，均断言干净拒绝而非越界异常）。测试总数 3316 → 3320。

## v0.71.27 (2026-08-17) — 截断按码点补齐 7 处 + 目录递归深度上限补齐 3 处

延续审计，本轮收尾两类此前批次未扫全的残留：**截断不按码点硬切代理对**（7 处）+ **递归目录遍历无深度上限**（3 处）。

### 🐛 修复

- **截断按码点补齐**（7 处 `[..N]` 硬切代理对，违反 CLAUDE.md「字符串截断必须按码点」规范）：内容在 emoji/CJK 扩展 B 代理对中间被切半产生 U+FFFD。全部改走 `ContextManager.TruncateByRunes`：
  - `TestTool` 提取失败用例截断 `line[..200]`（喂给 LLM 的测试失败信息，乱码会误导模型）
  - `Agent.Loop` 循环检测快照截断 `output[..outputSnipLen]`
  - `ChatScreen.Dialogs` 权限详情 `argsDetail[..800]` / 计划审批 `planDetail[..600]`（用户可见乱码）
  - `ChatScreen.Input` 粘贴预览 `text[..200]`×2 / 历史标题 `item.Title[..17]`
- **递归目录遍历深度上限补齐**（3 处，对齐 v0.71.26 的 WcTool/GrepTool/FindReplaceTool）：`CpTool.CopyDirectory` / `MvTool.CopyDirectory` / `RepoMapGenerator.CollectEntries` 递归复制/遍历目录无深度上限，深目录树或非 POSIX 平台（Windows junction 环）会无限递归 → StackOverflow 崩溃进程。补 `depth` 参数（>64 层停止）。（POSIX 上符号链接环由内核 ELOOP 提前抛 IOException 拦截，此处深度上限是跨平台 + 深目录树的兜底防御）

### ✅ 测试

本轮为机械替换（截断改走已测的 `TruncateByRunes`）与防御性深度上限，未新增断言；测试总数保持 3316，全绿。

## v0.71.26 (2026-08-17) — 符号链接环崩溃 + cd 相对路径系统性失效 + TUI 鼠标/缓存/网格 5 项

延续 bug 修复循环，本轮修**两个系统性缺陷**（符号链接环无限递归、cd 后相对路径仍基于进程 cwd）+ 5 处 TUI 确定性 bug。

### 🐛 修复

- **符号链接环 → StackOverflow 崩溃进程**（3 处递归目录遍历无深度上限）：`WcTool.CollectFiles` / `GrepTool.WalkRecursive` / `FindReplaceTool.CollectFiles` 遇 `ln -s . loop` 自引用环时无限递归直到栈溢出，直接把整个 agent 进程拖崩。补 `depth` 参数（>64 层停止）+ 既有数量上限提前返回
- **`cd` 后相对路径仍基于进程 cwd**（系统性，22 个文件）：`CdTool`/`bash` 切换 `BashTool.CurrentCwd` 后，后续文件操作工具的相对路径仍用 `Path.GetFullPath(x)` 单参数（= 进程启动目录）而非被跟踪工作目录，导致 `cd` 到别处后 `read_file`/`ls`/`tree`/`write` 等相对路径全部落到错误位置。统一改 `Path.GetFullPath(x, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory())`，覆盖 ReadFile/Ls/Tree/FindReplace/MultiEdit/ViewImage/Lint/Download/Transcribe/Screenshot/Lsp/Wc 等读、写、lint、下载、抓屏工具
- **`TuiButton` 点击未命中边界检查**：`MouseLeft` 分支直接 `OnClick`，无 `inside` 命中判定——点击按钮外区域也会误触按钮。补边界检查，未命中返回 false
- **`TuiMarkdown` 渲染缓存 key 缺 Role/IsPlainText**：同一实例复用改 `Role`（assistant→tool 等）或 `IsPlainText` 后，`_parsed` 命中旧缓存返回错误配色/纯文本渲染。补 `_lastRole`/`_lastPlain` 纳入缓存判定
- **`TuiScrollbar` 拖拽移动分支死代码**：`if (MouseLeft && !MouseRelease)` 按下分支先于拖拽分支 return true，拖动事件被按下分支吞掉，`OnScroll` 回调永不触发。重构为先判 `_dragging` 拖拽移动、再判按下，且按下时也回调一次
- **`TuiGrid` 星号轨分配溢出 totalSpace**：`Math.Max(1, remaining*weight/starTotal)` 强制每颗星轨至少 1px，小剩余空间 + 多星轨时前面各轨和超过 remaining、最后一轨拿到负值再被抬到 1，尺寸总和超出容器。改向下取整 + 最后一轨吸收余量，固定轨才保证最小 1px
- **`TuiComboBox` Backspace 按码元硬切代理对**：`_searchText = _searchText[..^1]` 删最后 1 个 `char`，emoji/CJK 扩展 B 代理对被切半留下 U+FFFD。改判代理对删 2 个码元

### ✅ 测试

新增 `TestV0726SymlinkCdAndUi`（6 项断言：GrepTool 遇符号链接环不崩溃 / `read_file`+`ls` 相对路径基于 CurrentCwd / TuiGrid 多星轨小空间不溢出且无负尺寸）。`ResolveSizes` 改 `internal` 供纯逻辑测试。测试总数 3310 → 3316。

## v0.71.25 (2026-08-17) — 工具整数参数系统性失效 + 图片编解码边界 + 渲染乱码

Explore 代理扫 `Tools/` 文件/命令类 + `Program/Infra/Batch/Skills` 后人工验证，本轮修一个**系统性**缺陷（所有工具整数参数因 `long` vs `int` 类型不匹配而静默失效）加 7 处边界/损坏/乱码。

### 🐛 修复

- **所有工具整数参数静默失效**（系统性）：`LLM.ParseJsonNumber` 把 JSON 整数统一解析为 `long`，而 19 处参数解析用 `x is int` 判断，`long` 不匹配导致 `timeout`/`max`/`depth`/`context`/`offset`/`limit`/`top`/`line`/`character` 等**全部回退默认值**（如 `bash` 传 `timeout:2` 实际仍跑 120s、`ls` 传 `max:10` 仍输出 100 条）。新增 `ToolArgs.GetInt` 统一兼容 int/long/double/string 四种来源，替换 BashTool/DiffTool/FindReplaceTool/DownloadTool/FetchTool/LsTool/PsTool/LspTool/ReadFileTool/TestTool/TreeTool 共 19 处
- **`MultiEditTool` 对 LLM 调用完全失效**：`ParseEdits` 只判 `editsObj is JNode`，但 LLM 参数经 `JNodeToObject` 把 JSON 数组转成 `List<object?>`，`is JNode` 恒 false → 永远返回「至少需要一个编辑操作」，工具不可用。补 `IEnumerable` 分支解析 `Dictionary<string,object?>`（对齐 `AskUserQuestionTool`）
- **`DrawCommands` path 光栅化首点坐标丢失**：`ParsePathSegments` 的 `cx` 初始为 0（非 NaN），首个数字被误当 y 与 x=0 配对，`M 10 20` 画成 `(0,10)`、整条线偏移。改 `cx = double.NaN` 初始化
- **`PngDecoder` chunk 长度整数溢出绕过越界检查**：`len` 为 4 字节大端，可取 `0x7FFFFFFF`，`off + len` 用 int 相加溢出为负、`> data.Length` 恒 false，随后负索引 `BE32` 抛越界异常（而非 `FormatException`）。改用 `(long)off + len` 比较
- **`BmpCodec` 32 位 BI_RGB 把保留字节当 alpha**：32 位 BI_RGB 第 4 字节是保留位（XRGB，常为 0），读作 alpha 会让绝大多数 32 位 BMP 解码后 alpha=0、图像全透明。改为 alpha 固定 255
- **`/history` 预览 `Substring` 硬切代理对**：`idx-40` / `+120` 码元窗口可能落在 emoji/CJK 扩展 B 代理对中间，渲染成 U+FFFD。提取 `BuildHistoryPreview` 并做 `IsLowSurrogate` 边界对齐
- **`/jobs` 命令列表 `[..57]` 硬切代理对**：后台命令第 57 码元跨代理对时显示 U+FFFD。改走 `ContextManager.TruncateByRunes`

### ✅ 测试

新增 `TestV0725DrawAndCodec`（6 项断言：path 首点坐标 / PngDecoder 溢出抛 FormatException / Bmp32 alpha=255 / 历史预览不切半）+ `TestV0725ToolArgsAndEdit`（7 项断言：ToolArgs 四种类型取数 / MultiEditTool 端到端编辑生效）。测试总数 3297 → 3310。

## v0.71.24 (2026-08-17) — 回退链端点 + 边界/显示 6 项修复

继续上一批 Explore 代理发现的遗留候选，本轮修 6 个中优先级（端点错发 / 边界异常 / 逻辑误判 / 显示损坏 / 轻微泄漏）。

### 🐛 修复

- **`FallbackLLM` 回退链对部分模型用错 BaseUrl**：`ResolveKeyAndUrl` 只取模型自身 `DefaultBaseUrl`，`qwen-turbo`/`glm-4-flash` 等未配 `DefaultBaseUrl` 的模型不回落供应商级 `Providers[providerId].DefaultBaseUrl`，回退时把 DASHSCOPE/ZHIPU 的 key 发到 `api.openai.com`。改为 `info?.DefaultBaseUrl ?? Providers[provider]`（空字符串的 local/custom 仍保持 null，与 `WebChat` 既有逻辑一致）
- **`FetchTool` 负数 `max_chars` 抛异常**：`Math.Min(mi, 100_000)` 只钳上限不钳下限，负数时 `TruncateByRunes(text, 负数)` 返回空 → `"".LastIndexOf(' ') = -1` → `-1 > 负数*3/4` 为真 → `text[..(-1)]` 抛 `ArgumentOutOfRangeException`，被吞成「抓取错误」。改 `Math.Clamp(mi, 1, 100_000)`
- **`GitPRTool` 默认分支子串误判**：`branches.Contains("main")` 对 `git branch` 输出做裸子串匹配，存在 `feature/maintenance` 等含 "main" 的分支名时误判默认分支为 main。改按行拆分、`TrimStart('*')` 后精确匹配分支名
- **`Syntax.Tokenize` 代理对切半**：逐 `char` 兜底分支把 emoji/CJK 扩展 B 切成两个孤立代理 token，终端渲染成 U+FFFD。改为 `IsHighSurrogate && IsLowSurrogate` 成对作为一个 token、`i += 2`
- **`Agent` 计划摘要 `plan[..160]` 代理对切半**：审批框标题摘要按码元硬切，改走 `ContextManager.TruncateByRunes`
- **`LLM` 成功路径未释放 `HttpResponseMessage`**：`response` 只在 400 回退分支 `Dispose`，成功路径靠 GC 回收。改 `using var response`（400 分支仍先 Dispose 旧响应再重试，最终响应由 using 统一释放）

### ✅ 测试

新增 `TestV0724SyntaxSurrogate`（2 项断言）：`Syntax.Tokenize("a😀b")` 的 emoji 成对 token、无孤立代理项。测试总数 3295 → 3297。

## v0.71.23 (2026-08-17) — 安全 + 数据损坏 + 死锁 4 项高优先级修复

Explore 代理扫 `Agent/`（LLM/Fallback/Agent/AgentSlot）+ `Tools/` 网络/外部类 + `Edit/`/`Watch/`/`Memory/` 三层后人工验证，本轮先修 4 个最高优先级（安全绕过 / 文件内容损坏 / 误判超时）。

### 🐛 修复

- **`GitTool` 危险操作拦截可被参数顺序绕过**：`BlockedPatterns` 用整串 `command.Contains("push --force")` 子串匹配，把 flag 挪到分支名/远端名之后（`push origin main --force`、`reset HEAD --hard`、`clean src/ -f`）即绕过拦截，真实执行 force push / hard reset / clean -f 等破坏性操作。改为 `HasBlockedGitOperation` 按 token 精确匹配（子命令大小写不敏感 + flag 区分大小写），顺带修正 `branch -d`（普通删除）被 `OrdinalIgnoreCase` 误拦、并放行 `push --force-with-lease`（更安全变体）
- **`DocTool` fetch 模式 SSRF 重定向绕过**：`_client` 设 `AllowAutoRedirect = true`，`FetchDocAsync` 只对初始 URL 做 `SsgfGuard.CheckUrl/CheckDns`，HttpClient 静默跟随 30x 后跳转目标（如内网 `127.0.0.1`、云元数据 `169.254.169.254`）不再校验。改为 fetch 模式用独立的 `AllowAutoRedirect=false` client + 手动跟随并对每一跳重跑 SSRF 校验（对标 `FetchTool.SendWithRedirectAsync`）
- **`EditorCore.MoveCursor` 上下移动不防代理对切半**：代理对修正被 `if (dx != 0)` 包住，仅左右移动生效。上下移动（`dx==0`）落到 emoji/CJK 扩展 B 代理对中间后，`Backspace`/插入把 emoji 切成两半，保存时 `Encoding.UTF8` 替换回退成 U+FFFD，**文件内容被永久破坏**。改为左右/上下移动后统一修正（`dx==0` 默认回退到码点边界）
- **`Agent` 自动测试 stdout/stderr 顺序读导致管道死锁**：先 `ReadToEndAsync` 读完 stdout 再读 stderr，子进程向 stderr 写满管道缓冲（约 4KB+，`dotnet test` 的编译警告/弃用提示极易达到）时会阻塞在写 stderr、永不退出，stdout 读任务永不完成 → 误判超时并 `Kill` 本应通过的测试。改为 stdout/stderr 并发读（`Task.WhenAll`）后再等待超时

### ✅ 测试

新增 `TestV0723SafetyAndCursor`（13 项断言）：`HasBlockedGitOperation` 的 flag 后置绕过 / force-with-lease 放行 / branch -d 放行 / 正常命令放行；`MoveCursor` 上下移动落到 emoji 代理对中间时 Cx 回退到码点边界。测试总数 3282 → 3295。

## v0.71.22 (2026-08-17) — 日志/预览截断走码点边界（代理对不切半）

继续清扫上一轮遗留的 UTF-16 原始切片候选（Explore 代理标记、人工确认）。本轮修复 3 处「显示/日志层」截断在代理对（emoji / CJK 扩展 B）中间切半、产出 U+FFFD 乱码的问题——均属展示层，不影响数据完整性，但会让预览/日志里出现乱码。

### 🐛 修复

- **`FindReplaceTool` 匹配上下文窗口切半代理对**：`content.Substring(match.Index - 30, match.Length + 60)` 的起止边界直接按 UTF-16 码元算，当「前后各 30 字符」的窗口边界恰好落在 emoji / 扩展 B 汉字的代理对中间时，预览行首/行尾出现孤立代理（U+FFFD）。改为起止边界向码点对齐（`start` 落在低位代理则回退到高位、`end` 落在低位代理则前伸纳入）
- **`ErrorLog.ToolError` 参数截断切半**：`content`/`old_string`/`new_string` 用 `v[..Math.Min(100, len)]` 硬切 100 码元，代理对在边界处被劈开。改走 `ContextManager.TruncateByRunes(..., 100)`（码点安全，且顺带消除旧代码 `v?.ToString()?[..]` 对 null 时拼出孤立 `"..."` 的怪异语义）
- **`ErrorLog` 堆栈截断 + `HooksManager` hook 输出截断**：`ex.StackTrace[..500]`、`output[..Math.Min(200)]` 同样按码元硬切，改走 `TruncateByRunes`（堆栈/调试输出理论上几乎全是 ASCII，属防御性对齐，与上两处一致化）

### ✅ 测试

新增 `TestV0722RuneSafeContext`（4 项断言）：`find_replace` 预览上下文窗口边界落在 emoji 代理对中间时不切半、emoji 完整保留；`ErrorLog.ToolError` 的 `content` 截断到 100 码点不切半、emoji 完整保留。测试总数 3278 → 3282。

## v0.71.21 (2026-08-17) — FileTracker 正确性 + ReadFile limit 边界

继续清扫文件追踪与读取工具。Explore 代理扫 `Tools/` 文件操作类 + `Infra/FileTracker` 后人工验证，本轮修复 3 个可复现问题。

### 🐛 修复

- **`FileTracker.RecordWrite` 不更新 `LastReadTimes`**：Agent 自己写文件（write_file/edit_file 写后调 `RecordWrite`）只更新了 `Tracked` 哈希，`LastReadTimes` 仍停留在初次 `RecordRead` 时刻。后续再次编辑时 `ValidatePreEdit` 用 `fileModTime > lastRead + 1s` 判定，把 Agent 自己的写入误报为「自上次读取后被外部修改」（read→write→edit 连续操作必触发）。补上一行 `LastReadTimes[absPath] = DateTime.UtcNow`，写入后即视为「当前内容已知」
- **`FileTracker` LRU 淘汰实为 FIFO**：`Tracked.Keys.FirstOrDefault()` 淘汰的是 `Dictionary` 枚举序「最早插入」的键——而覆盖已存在键不改变枚举顺序，热点文件照样被先清掉。改为遍历 `LastReadTimes` 淘汰「最久未读取」的条目，并顺带清理 `LastReadTimes`（旧代码只删 `Tracked`，泄漏读取时间）
- **`ReadFileTool` `limit<=0` 未钳制**：`offset` 有 `Math.Max(0, offset-1)`、`tail` 有 `Math.Max(0, tli)` 兜底，唯独 `limit` 直接透传。`limit=0` 或负数时 `Take(limit)` 返回空序列，输出空 `<file>` 块 + 误导性的「还有更多行」提示。改为 `Math.Max(1, li)` 钳制

### ✅ 测试

新增 `TestV0721FileTrackerRead`（2 项断言）：`read_file` 传 `limit=0` 仍读到首行；`RecordWrite` 后（未先 read）`ValidatePreEdit` 返回 null（不再误报「尚未读取」）。测试总数 3276 → 3278。

## v0.71.20 (2026-08-17) — Infra 层确定性 bug 修复（Hooks 前缀碰撞 + FileIgnore 未转义 [ + RetryPolicy 边界）

Explore 代理系统扫 `Infra/`（BashGuard/FileTracker/RetryPolicy/LruCache/HooksManager/FileIgnoreManager/ErrorLog/IdGenerator）后人工验证，本轮修复 4 个可复现问题。BashGuard 的 `rm`/`mv`/`cp` 未列入禁用集合经核实为**有意设计**（走 PermissionManager 确认层，非禁用层），不修。

### 🐛 修复

- **`HooksManager` session hook 前缀碰撞**：hook ID 格式为 `"{eventType}_{guid}"`，`RunEventAsync` 用裸 `kv.Key.StartsWith(eventName)` 判断事件归属，导致 `"PostToolUseFailure_xxx".StartsWith("PostToolUse")` 为真——失败专属 hook 在成功路径上被误触发。改为 `eventName + "_"` 前缀 + `Ordinal` 精确匹配
- **`FileIgnoreManager` 未转义 `[`/`]` 生成非法正则**：`GlobSegmentToRegex` 转义了 `. + ( ) ^ $ { } | \` 却漏了 `[`/`]`，`.gitignore` 出现不成对方括号（如 `foo[`、`[abc`）时 `new Regex` 抛 `ArgumentException`（`Match` 无 try/catch，向上传播到文件过滤/搜索工具）。补上 `[`/`]` 转义为字面量
- **`RetryPolicy` `MaxRetries` 负数不执行 action**：`for (attempt = 0; attempt <= cfg.MaxRetries; attempt++)` 在负数时条件立即为 false，action 一次都不执行却落到「不可达终点」抛误导性的 `InvalidOperationException`。改为 `maxRetries = Math.Max(0, cfg.MaxRetries)` 钳制
- **`RetryPolicy` `NoRetryExceptions` null 无保护**：`if (NoRetryExceptions.Contains(...))` 直接调用，而 `RetryableExceptions` 有 `is { Count: > 0 }` 保护。调用方显式置 null 时 `ShouldRetry` 抛 NRE（且位于异常过滤器内会逃出 catch）。改为 `?.Contains(typeName) == true`

### ✅ 测试

新增 `TestV0720InfraDeterministic`（6 项断言）：PostToolUse 不误触发 PostToolUseFailure hook / PostToolUseFailure 正确触发 / 含未闭合 `[` 的 .gitignore 不崩溃且字面匹配 / MaxRetries 负数钳制为 0 执行一次 / NoRetryExceptions null 不抛 NRE。测试总数 3270 → 3276。

## v0.71.19 (2026-08-17) — 语义记忆扩展 B 区汉字召回修复（Tokenize 代理对）

`SemanticMemory.IsCJK` 只覆盖 BMP 内 CJK 区间，`Tokenize` 按 `char`（UTF-16 码元）迭代，导致 CJK 扩展 B 区汉字（U+20000–U+2A6DF，UTF-16 代理对，如 𠮷、𩸽 等常见于人名/地名）落入 `i++` 被**静默丢弃**——既不参与 bigram 也不成为单 token，含扩展 B 的记忆按扩展 B 关键词查询时召回失败。属召回率缺陷而非崩溃/数据损坏。

### 🐛 修复

- **`SemanticMemory.Tokenize` 扩展 B 代理对处理**：在跳过空白/标点后、CJK 判断前，识别 `高代理项+低代理项` 并解出完整码点；若落在扩展 B 区间则作为**单个 token**加入（扩展 B 罕见字按单字索引、无需 bigram），其余代理对（emoji 等）成对跳过——同时避免旧代码把 emoji 逐 `char` 丢弃时的低效迭代。BMP 基本区汉字 bigram 逻辑不变。

### ✅ 测试

新增 `TestV0719CjkExtB`（5 项断言）：`Tokenize("𠮷野家")` 保留扩展 B 字「𠮷」为单 token + BMP「野家」bigram 正常 + 无孤立代理项；emoji 代理对成对跳过不产生 token；`SearchRelevant` 用扩展 B 查询命中含扩展 B 的记忆。测试总数 3265 → 3270。

## v0.71.18 (2026-08-17) — 共享记忆按名查找修复（StructuredMemory.Get 双目录回退）

记忆系统的共享记忆在 `ListAll()`/`Search()` 里能被加载（`ListAll` 遍历 `SharedMemoryDir` + `SlotMemoryDir` 两个目录），但 `Get(name)` 只经 `NameToPath` 解析到槽位独立目录（`MemoryDir => SlotMemoryDir`），导致共享记忆（如 `SharedMemoryManager.PullSharedAsync` 拉取的团队记忆、或 `GetRelevantContext` 里 `[[wiki-link]]` 交叉引用指向的共享记忆）**按名查不到，返回 null**。而 `Update`/`SetShared`/`GetRelevantContext` 均先 `Get(name)`，于是团队共享记忆无法被更新、无法展开交叉引用描述。

### 🐛 修复

- **`StructuredMemory.Get` 共享目录回退**：查找顺序改为「槽位独立目录优先 → 共享目录回退」，与 `ListAll` 双目录行为一致。抽出 `NameToPathIn(dir, name)` 辅助方法，`NameToPath`（供 Create/Delete 写槽位）委派给 `NameToPathIn(MemoryDir, ...)`，`Get` 额外回退 `NameToPathIn(SharedMemoryDir, ...)`
- **同名冲突槽位优先**：个人槽位记忆可覆盖同名共享记忆（个人覆盖团队，符合直觉）；`Get` 找到共享记忆后 `Update`/`SetShared` 会经 `existing.FilePath` 回写共享文件，行为正确

### ✅ 测试

新增 `TestV0718SharedMemoryGet`（4 项断言）：临时目录隔离 cwd + `CurrentSlotIndex=7`，直接向 `SharedMemoryDir` 根目录写一个共享记忆文件（模拟 `PullSharedAsync` 拉取），断言 `Get` 按名查到共享记忆、`ListAll` 双目录均列出、同名冲突时槽位优先。测试总数 3261 → 3265。

## v0.71.17 (2026-08-17) — UI 层确定性 bug 修复（撤销栈方向 + 菜单空序列 + BoxBuffer 负宽 + WrapLine 码点）

继续清扫 UI 层确定性 bug。Explore 代理系统扫 `UI/TUI/`（编辑器/控件/共享缓冲）后人工验证，本轮修复 4 个可复现问题。

### 🐛 修复

- **`EditorCore.TrimBottom` 撤销栈修剪方向反**：`Stack<T>.ToArray()` 返回栈顶在前（`arr[0]`=最新、`arr[^1]`=最旧），原循环 `i = arr.Length-1 .. arr.Length-max` 保留的是**最旧**的 max 条、丢弃最新若干条——编辑超过 `MaxUndo=100` 后第 101 次编辑被立即丢弃、撤销历史整体错位。改为 `i = max-1 .. 0` 保留最新 max 条，并提为 `internal` 便于自测
- **`TuiMenu` 全分隔线菜单空序列崩溃**：`.Max(i => DisplayWidth(i))` 只检查 `items.Count > 0` 未检查过滤后是否为空，列表全为 `""`/`"---"` 时 `Max()` 抛 `InvalidOperationException`。改为 `.Select(...).DefaultIfEmpty(10).Max()`
- **`BoxBuffer.Fill` 负宽度负参异常**：`new string(ch, ContentWidth)` 未钳制 `Width-2`，带边框且 `Width<2` 时抛 `ArgumentOutOfRangeException`（同文件 `Render` 已有 `Math.Max(0, ...)` 保护）。补上钳制
- **`TuiHelper.WrapLine` 首字符 emoji 切半**：`FindBreakIndex` 返回 0（首字符宽度即超 `maxWidth`）时原兜底 `breakIdx=1` 按 UTF-16 码元切半代理对，`maxWidth=1` 且首字符为 emoji/扩展区汉字时产出孤立代理项。改为取第一个完整码点的字符长度

### ✅ 测试

新增 `TestV0717RuneSafeWrap`（3 项）+ `TestV0717UiDeterministic`（3 项）：WrapText 首字符 emoji 不切半 / CJK 不越界 / 英文折行正常；TrimBottom 保留最新 2 条 / 全分隔线菜单不崩溃 / 窄边框 BoxBuffer.Fill 不抛。测试总数 3255 → 3261。

## v0.71.16 (2026-08-17) — 数据路径 UTF-16 切片代理对修复（6 处）

继续清扫「按 UTF-16 码元任意切片」这一 CLAUDE.md 明文禁止的确定性数据损坏源。系统性审查发现 6 处仍用 `str[..N]` 截断**发往 LLM/系统提示词/落盘**的数据，当截断点落在 emoji/扩展区汉字（代理对）中间时会切出孤立代理项，UTF-8 编码后成为 U+FFFD 替换符混入提示词与文件内容。统一改走 `ContextManager.TruncateByRunes`（按码点截断）。

### 🐛 数据路径

- **`Agent.Feedback.cs`（2 处）**：lint 结果 `lintResult[..1500]` 与自动测试失败输出 `fullOutput[..2000]` 注入工具结果前改按码点截断——测试输出常含 ✔/✘/❌ 等 emoji，切半后污染自动修复闭环的上下文
- **`Memory/SemanticMemory.cs`**：检索记忆摘要 `snippet[..300]` 注入系统提示词前改按码点截断
- **`Memory/EmbeddingStore.cs`**：embedding 输入 `text[..8000]` 改按码点截断，避免切半文本生成错误向量（原静默 catch 吞掉）
- **`Infra/OfficeExtractor.cs`（3 处）**：DOCX/XLSX/PPTX 提取结果 `result[..maxChars]` 改按码点截断（对比 `FetchTool` 已正确用 `TruncateByRunes`），文档内 emoji/扩展区汉字不再损坏
- **`Agent.Commit.cs`**：LLM 生成的提交信息 `msg[..72]` 改按码点截断，避免 `feat: add 🎉` 之类消息落成带 `�` 的 commit

### ✅ 测试

新增 `TestV0716RuneSafeTruncation`（3 项断言）：构造最小 DOCX（段落含 emoji），`maxChars=3` 恰好落在代理对中间，断言提取结果无孤立代理项、emoji 完整保留、截断说明正常追加。测试总数 3252 → 3255。

## v0.71.15 (2026-08-17) — 上下文压缩界面指示（Web + TUI 动画）

上下文压缩此前在 Web 版只有聊天流里的一行 `🔄 [1/3] ...` 文本、TUI 版动态栏的进度条在压缩结束后残留陈旧标签，用户感知弱。本轮补齐两端「压缩中」的界面指示：动画 + 完成后消失。

### ✨ 界面

- **Web 版压缩指示条**：`WebAssets.cs` 在聊天区顶部新增浮动胶囊指示条——旋转 spinner + 阶段文案（裁剪工具输出 / 正在摘要旧对话 / 紧急压缩）+ 进度条，`@keyframes cspin` 旋转动画 + 进度条宽度过渡，收到 `done` 后淡出消失（`opacity` 过渡 + `translateY` 上浮）
- **Web 版事件链路**：`ContextManager` 新增静态 `CompressFinished` 事件（`MaybeCompressAsync` 的 `finally` 触发，无论是否实际压缩）；`WebChat.Start` 订阅 `CompressProgress`/`CompressFinished`，经 `AsyncLocal<int> _currentSlot` 把进度按槽位路由 `BroadcastTo(slot, "compress", ...)`，新增纯函数 `SerializeCompress(layer, label, percent, done)`；前端 `compress` SSE 监听 → `showCompress()`
- **TUI 版修复残留标签**：`ChatScreen.SyncDynamicBar` 压缩完成清理 `ProgressPercent` 时同步清空 `ProgressLabel`，避免压缩结束后动态栏右段残留 `[L3] 压缩完成` 覆盖常驻上下文占用 `%`（原 `OnCompressProgress` 只在 `IsCompressing` 时写标签，结束时无事件清空）

### ✅ 测试

新增 `TestV0715CompressIndicator`（8 项断言）：`SerializeCompress` 纯函数载荷（done/layer/label/percent 透传）+ `CompressFinished` 事件触发 + `IsCompressing` 复位 + 极小上下文不压缩。测试总数 3244 → 3252。

## v0.71.14 (2026-08-17) — Retry-After 头解析负数回退

继续清扫 LLM 客户端边界。本轮修复一个 429 限流重试的确定性边界 bug：`Retry-After` 响应头为负数时，退避延迟计算为负，`Task.Delay` 抛异常。

### 🐛 LLM 重试

- **`ParseRetryAfter` 负数秒未回退**：`LLM.cs` 解析纯数字 `Retry-After` 头时直接 `(long)seconds * 1000` 再 `Math.Min`，负数会得到负延迟——调用方 `Task.Delay(负)` 抛 `ArgumentOutOfRangeException`（`Retry-After: -1` 更会让 `Task.Delay(-1000)` 直接抛、而非「无限等待」语义），整次 429 重试链被异常打断而非回退默认退避；改为 `seconds < 0` 时返回 `null` 走默认指数退避，与 HTTP-date 分支的 `delay > 0` 判断保持一致

### ✅ 测试

新增 `TestV0714RetryAfter`（8 项断言）：正整数→正延迟、`0`→立即重试、负数/`-1`→回退 null、无头→null、非数字→null、过去时间→null、未来时间→正延迟。`ParseRetryAfter` 由 `private` 提为 `internal` 便于自测。测试总数 3236 → 3244。

## v0.71.13 (2026-08-17) — 槽位 Cts 清理顺序竞态修复

继续清扫多 Agent 并发场景的数据竞态。本轮修复一个确定性竞态：后台槽位任务结束时，`IsBusy=false` 与 `Cts` 原子摘除的顺序错误，会让新任务的取消令牌被旧任务误释放。

### 🐛 并发

- **`StartSlotTask` finally 清理顺序竞态**：`Program.Repl.cs` 后台任务 finally 原先把 `slot.IsBusy = false` 写在 `Interlocked.Exchange(ref slot.Cts, null)?.Dispose()` **之前**。二者之间 UI 线程读到 `IsBusy == false` 会启动新任务写入新的 `Cts`，此时旧任务的 `Interlocked.Exchange` 会把新任务的 `Cts` 摘走并 Dispose——新任务从此无法被 Esc 中断（Esc 读到 null 即 no-op）。改为先摘除 `Cts` 再置 `IsBusy=false`，使「检查 IsBusy 启动新任务」必然发生在旧任务 Cts 清理完成之后，杜绝误释放

### ✅ 测试

新增 `TestV0713CtsLifecycle`（6 项断言）：`AgentSlot.Cts`/`IsBusy` 独立字段语义 + `Interlocked.Exchange` 原子摘除（并发摘除恰好一个取到非 null、字段归 null、取到者是原实例）。测试总数 3230 → 3236。

## v0.71.12 (2026-08-17) — Agent.Messages 线程安全（锁内读写 + 快照读）

继续清扫多 Agent 并发场景的数据竞态。本轮聚焦 `Agent.Messages`（`List<JNode>`）的并发访问：主循环线程流式追加消息，与 Web 序列化 / 退出自动保存 / 会话命令 / 历史命令等外部线程的遍历并存，非线程安全的 `List` 在「遍历中并发 Add」时抛 `InvalidOperationException`。

### 🐛 并发

- **`Agent.Messages` 非线程安全访问**：`Agent.cs` 把 `Messages` 从公开字段改为 `_messages` 后备字段 + 属性，新增 `MessagesLock` + 封装方法 `AddMessage`/`RemoveMessageAt`/`InsertMessage`/`SnapshotMessages`/`ReplaceMessages`/`ClearMessages`（锁内读写，快照 `ToList` 供外部只读遍历）
- **内部写点统一走锁**：`Agent.cs`/`Agent.Loop.cs`/`Agent.Tools.cs` 的 `Messages.Add/Insert/RemoveAt/Clear` 全部替换为封装方法
- **外部写点改封装**：`Program.cs`（恢复会话）/`Program.Repl.cs`（`/resume`）/`Program.Commands.cs`（`/session`、新建会话）/`WebChat.Commands.cs`（`/reset`、`/session load`）/`WebChat.cs`（`/sessions/new|load`、`/fileref`）/`SessionCommand.cs`（load/resume）的 `Clear+AddRange` 全部合并为 `ReplaceMessages`、`Clear` 改 `ClearMessages`、`Add` 改 `AddMessage`
- **外部读点改快照**：`WebChat.Serialization.cs`（`SerializeHistory`/`HasHistory`）、`Program.Repl.cs`（退出自动保存/崩溃保存/紧急退出/token 估算）、`Program.Commands.cs`（`/history` 索引遍历、`/loop` 尾消息）、`WebChat.Commands.cs`（`/session save`）、`SessionCommand.cs`（save）、`HistoryCommand`/`ExportCommand`/`CompactCommand`/`StatsCommand`/`AgentTool.BuildParentContext` 全部改为 `SnapshotMessages()` 锁内快照，杜绝遍历中并发 Add 抛异常

### ✅ 测试

新增 `TestV0712MessagesThreadSafety`（7 项断言）：封装方法功能正确性（追加/快照副本/插入/删除/整体替换/清空）+ 并发压力（写线程 `AddMessage` ×5000 与读线程 `SnapshotMessages` ×5000 并发不抛异常）。测试总数 3223 → 3230。

## v0.71.11 (2026-08-17) — 并发安全 4 项（锁一致性/volatile/定时器/原子累加）

继续清扫多 Agent 并发场景的数据竞态，修复 4 个确定性并发 bug。

### 🐛 并发

- **`_allSessionFiles` 锁不一致**：`Agent.Tools.cs` 里 `_allSessionFiles.Add(path)` 在 `lock (_modifiedFiles)` 内，而 `Agent.cs:620` 读取时用 `lock (_allSessionFiles)` —— 两个不同锁对象无法互斥，文件清单读写竞态；改为 `_allSessionFiles` 的写入也单独用 `lock (_allSessionFiles)`
- **`_activeSlot` 非 volatile**：`Program.cs` 的 `_activeSlot` 被主线程写、后台槽位/命令线程读（`ActiveSlotIndex`），非 volatile 存在可见性风险；改为 `volatile int`
- **`WatchMode` 定时器 Dispose 竞态**：`Stop()`/`Dispose()` 在锁外操作 `_debounceTimer`，与 `OnFileChanged` 的锁内访问不互斥，Stop 后回调仍可能重建 Timer 泄漏；`Stop()` 的 Timer 清理移入 `lock (_lock)`，删掉 `Dispose()` 里冗余的锁外 `Dispose()`，`_disposed` 改为 `volatile bool`
- **`FallbackLLM.TotalSpent` 非原子累加**：`TotalSpent += cost` 是读-改-写，多槽位 Agent 并发回退时丢增量、预算判断失真；引入 `_stateLock` + `AddSpent`/`BudgetExceeded` 辅助，累加与预算检查原子化，`Reset()` 同锁置零

### ✅ 测试

新增 `TestV0711Concurrency`（3 项断言）：`FallbackLLM` 并发累加 10000×0.5 无丢失、Reset 归零、`WatchMode` 重复 Stop/Dispose 幂等不抛异常。测试总数 3220 → 3223。

## v0.71.10 (2026-08-17) — 输入控件代理对安全（光标移动/删除）

继续清扫编辑原语层 UTF-16 代理对拆半问题：输入控件的光标移动与字符删除仍逐 `char` 操作，emoji/CJK 扩展 B 会拆半成 U+FFFD。

### 🐛 输入控件

- **`TuiInput` 光标移动拆半代理对**：`MoveCursorLeft`/`MoveCursorRight` 逐 `char` 前进/后退，光标会落在代理对中间；改为检测 `char.IsHighSurrogate(Text[i-1]) && char.IsLowSurrogate(Text[i])` 跳过中间码元
- **`TuiInput` 删除拆半代理对**：`DeleteCharBefore`/`DeleteCharAfter` 固定删 1 个 `char`，退格/Delete 只删半个 emoji；改为按代理对边界算 `delLen`（1 或 2）
- **`TuiTextArea` 光标移动拆半代理对**：`MoveCursorCol` 在 `Math.Clamp` 后仍可能落在代理对中间；改为检测边界后 `newCol += delta > 0 ? 1 : -1` 跳过
- **`TuiTextArea` 删除拆半代理对**：`DeleteCharBefore`/`DeleteCharAfter` 固定删 1 个 `char`；改为按代理对边界算 `delLen`
- **`TuiChatInput` 光标移动/删除拆半代理对**：`MoveLeft`/`MoveRight`/`Backspace`/`DeleteFwd` 同样逐 `char` 操作；改为 `char.IsHighSurrogate`/`char.IsLowSurrogate` 边界检测（`private static` 原语，与 `EditorCore.MoveCursor` 已正确的代理对跳过模式一致）

### ✅ 测试

新增 `TestV0710EditPrimitives`（8 项断言）：TuiInput/TuiTextArea 左右移动跳过代理对中间、退格/Delete 整删 emoji 不产生 `�`。测试总数 3212 → 3220。

## v0.71.9 (2026-08-17) — 全仓 UTF-16 代理对截断清扫 + ANSI/JPEG/边框 确定性修复

继续清扫全仓字符串截断与 UI 渲染边界，修复约 30 个确定性 bug，补齐单元测试。

### 🐛 UTF-16 代理对截断（续）

任意索引 `[..N]`/`[^N..]`/`Substring` 切片在 emoji/CJK 扩展 B 处拆半代理对产生 U+FFFD。本轮把 Program 层（Commands/Repl/Output）、Watch 模式、文件操作工具（ReadFile/EditFile/MultiEdit/Lint/FindReplace/Agent）、记忆与会话（StructuredMemory/SessionManager/MemoryRetrieval/ProjectContext）等约 23 处切片统一改为 `ContextManager.TruncateByRunes`/`TruncateTailByRunes`。

### 🐛 LSP 客户端

- **JSON-RPC `Content-Length` 按字符数而非字节数**：`ReadResponse` 逐字符读正文，多字节 UTF-8（中文/emoji）内容长度错位导致粘包/丢包；改为从 `BaseStream` 按字节读头 + 正文，`Encoding.UTF8.GetString` 解码
- **`initialized` 通知后误读响应**：`initialized` 是单向通知、无响应体，却 `await ReadResponse` 阻塞到超时；移除该次读取
- **参数拼接 + 跨平台路径**：`string.Join(" ", args)` 遇含空格路径被拆散，改用 `ArgumentList` 逐参数；`file://` URI 改用 `new Uri(Path.GetFullPath()).AbsoluteUri` 跨平台

### 🐛 渲染 / 终端

- **`AnsiString.Strip`/`TruncateByWidth` CSI 终止符只认 `m/H/J/K`**：`\x1b[?25l`（隐藏光标）等序列未在 `m/H/J/K` 终止，把后续真实文本一并吞掉；改为按 CSI 最终字节区间 0x40–0x7E 判定，并跳过 `ESC[` 引入符
- **`BoxBuffer` 负宽度崩溃**：`new string(h[0], Width - 2)` 在 `Width < 2` 时抛 `ArgumentOutOfRangeException`；改为 `Math.Max(0, Width - 2)`
- **`BoxBuffer` 省略号 off-by-one**：`TruncateByVW(text, maxLen-1) + "…"` 未预留「…」两列，改为 `maxLen-2`
- **双省略号**：`TuiHelper.TruncateByWidth` 已自带省略号，`TuiToastQueue`/`TuiDynamicBar` 又 `+ "…"` 产生「……」；去掉多余省略号
- **省略号未预留宽度**：`InlinePermission`/`SessionPicker`/`DiffPreview` 私有 `TruncateByVW` 追加「…」但不预留两列，改为先判 `DisplayWidth` 快速返回 + 循环预留 2 列
- **`TuiRichEditor` 宽度判据 `>127`**：重音字符（é/ñ）被判为宽字符，改用 `TuiHelper.DisplayWidth`（委托 `AnsiString.CharWidth` 唯一真源）

### 🐛 图像解码 / 配置 / 杂项

- **`JpegCodec` DHT 表越界**：`dcTables`/`acTables` 长度 4，但 DHT 表 id 字段 0–15，构造表 id≥4 的 JPEG 触发 `IndexOutOfRangeException`；扩到 16
- **`--max-requeue` 配置丢失**：命令行参数在 `_config = Config.FromEnv()` 之前写入 `_config` 字段、随后被单例覆盖；改为先解析为局部变量、在 `FromEnv()` 后落回 `Config.Instance`
- **`LLM` Retry-After 整数溢出**：`seconds * 1000` 用 `int` 相乘溢出，改为 `long` 中间量后钳制
- **`ProjectContext` `.git` 仅识别目录**：worktree/submodule 的 `.git` 是文件而非目录，漏检版本库；同时识别 `File.Exists`

## v0.71.8 (2026-08-17) — 工具/TUI/UTF-16 代理对 26 项确定性修复

系统性审查工具、TUI 控件、基础设施与全仓字符串截断，修复 26 个确定性 bug，补齐单元测试。

### 🐛 工具（Tools）

- **`StructTodoTool` 解除阻塞污染标题**：每次解锁都在 `Title` 后追加 `[解除阻塞: id]`，反复累加；移除污染行，并为 `TodoItem` 补 `Description` 字段（读/写与 `TodoTool` schema 对齐）
- **`TodoTool` 状态硬编码**：列出时 `状态=pending` 写死，改为 `状态={todo.Status}` 反映真实状态
- **`FetchTool` 截断长度谎报**：截断后消息引用已截断的 `text.Length`，改为截断前记录 `originalLen` 再引用
- **`DocTool` 缓存非线程安全**：`Dictionary` 缓存多 Agent 并发读写竞态，改为 `ConcurrentDictionary`

### 🐛 TUI 控件 / UI

- **`TuiProgress` 负宽度越界**：`barW < 0` 时 `Math.Clamp(filled, 0, barW)` 抛异常，先钳 `barW = 0`
- **`TuiControl` 字符推进按 UTF-16 码元**：`charIdx++` 遇 emoji 拆半，改为 `charIdx += rune.Utf16SequenceLength`
- **`AnsiString.TruncateByWidth` 拆半代理对**：逐 `char` 拼接切半 emoji → U+FFFD，改为按 `Rune` 拼接 + 码元补齐
- **`BoxBuffer` 宽度判定与真源分叉**：`VW`/`TruncateByVW` 用 `r.Value > 127` 判宽，与 `AnsiString.CharWidth` 真源不一致，统一委托真源
- **`TuiScrollbar` 拖拽坐标未换算**：`ev.MouseY` 直接当相对坐标，缺 `GetAbsoluteY()` 偏移
- **`TuiMarkdown` 彩虹段拆半代理对**：`BuildRainbowSegments` 逐 `char` 遍历，改为 `EnumerateRunes`
- **`TuiToastQueue` 跨线程可见性**：`_current` 静态字段非 volatile，Toast 在 UI/后台线程间可能读到旧值，加 `volatile`
- **`TuiComboBox` 过滤后索引错乱**：渲染/Home/End 用未过滤索引，过滤后选中错位，新增 `ActiveIndices` 统一
- **`TuiTable` 单元格溢出错位**：超宽单元格不截断破坏表格对齐，新增按列宽截断（含 ANSI/Spectre 标记感知的 `TruncateMarkup`）

### 🐛 基础设施（Infra）

- **`Logger.FlushAll` 锁重入死锁**：外层 `lock(_lock)` 内再进 `FlushLocked` 触发 `LockRecursionException`，去掉外层锁
- **`LogMetrics` 缩容残留写指针**：`RingCapacity` 缩小只删元素不重置 `_ringIndex`，后续 `Record` 越界写，补 `_ringIndex = 0`
- **`BmpCodec` int.MinValue 溢出**：`Math.Abs((int)height)` 遇 `int.MinValue` 溢出/回绕，改为 `long` 取绝对值 + 超界抛 `FormatException`
- **`TrueTypeFont` 拆半代理对**：`Measure`/`DrawString` 逐 `char` 取字形，改为 `EnumerateRunes`
- **`IdGenerator` 取模偏差**：`_rng.GetInt32` 实例调用误用（静态方法）+ 模运算取模偏差，改为 `RandomNumberGenerator.GetInt32(len)`

### 🐛 UTF-16 代理对截断（全仓清扫）

任意索引处 `[..N]`/`[^N..]` 切片在 emoji/CJK 扩展 B 处拆半代理对，产生 U+FFFD。新增 `ContextManager.TruncateTailByRunes`，并把约 20 处切片（Tree/Export/Fetch/Doc/Git/Ps/Bash/GitPR/Memory/AskUserQuestion/NotebookEdit/TranscribeAudio/Trajectory/BackgroundTask/WorkReporter 等）改为按码点截断。

### 🐛 共享记忆

- **`SharedMemoryManager` 快照目录与检出目标不一致**：拉取前快照用 `MemoryDir`（槽位子目录）、检出目标却是共享目录，导致新增文件误判为「更新」；快照统一用 `SharedMemoryDir`

## v0.71.7 (2026-08-17) — 基础设施/文件工具/会话/批处理/编辑器 21 项修复

系统性审查基础设施、文件操作工具、会话/辅助模式、批处理与 TUI 编辑器，修复 21 个确定性 bug，补齐单元测试。

### 🐛 基础设施（Infra）

- **`.gitignore` 目录规则产生 `//` 双斜杠**：`FileIgnoreManager.BuildRegex` 对 `logs/` 这类尾斜杠目录规则先拼接再补 `/`，正则出现 `//`，导致 `logs/` 规则既匹配不到目录本身、也匹配不到目录内容；拆分「锚定/中间目录段/尾斜杠后缀」逐段拼接修复，`logs/output.txt`、`logs/deep/file.txt` 现在正确命中且不误伤 `catalog.txt`
- **Hook exit 2 + JSON 无 decision 不阻断**：`HooksManager.ParseHookOutput` 在 JSON 分支下若 `exitCode == 2` 且 `Decision` 为空，未落回 block；补上 `Continue=false + Decision="block"`，同时不覆盖显式 `approve`/`deny`

### 🐛 文件操作工具

- **`RmTool` 非 Windows 空 `SpecialFolder` 前缀**：`Environment.GetFolderPath(Windows/System)` 在非 Windows 返回 `""`，`ProtectedPaths` 空串 `StartsWith` 恒真 → 一切路径都被判为受保护；跳过空路径
- **`GlobTool` `*.*` 漏掉无扩展名文件**：`Directory.GetFiles(root, "*.*")` 在 Unix 不匹配无点文件，改为 `"*"`
- **`FindReplaceTool` 替换串被当正则替换模式**：`regex.Replace(content, replacement)` 会把 `$1`/`${name}` 解释为捕获组引用，改用 `MatchEvaluator` 字面替换
- **`EditFileTool`/`MultiEditTool` 三处**：① `Encoding.UTF8`（`throwOnInvalidBytes:false`）静默吞非法字节，改为 `new UTF8Encoding(false,true)` 严格校验；② 写回时 `Replace("\n","\r\n")` 在已有 CRLF 时产生 `\r\r\n`，改为先归一化 LF 再转 CRLF；③ CRLF 文件编辑保持换行符
- **`CpTool`/`MvTool` 目录复制/移动进自身子树**：目标落在源目录内部时 `Directory.Move`/递归复制无限循环，加前缀检测提前拒绝
- **`ReadFileTool` 尾随换行行数虚增**：`text.Split('\n')` 对 `"a\nb\n"` 产生 3 元素、行数报 3 实为 2；去掉末尾空元素
- **`GrepTool` 单目录不可访问导致整树漏搜**：`Directory.GetFiles(..., AllDirectories)` 一个异常整棵放弃，改为逐目录递归、每目录独立 try/catch

### 🐛 会话/辅助模式（Watch / Fallback / Session）

- **`WatchMode` AI? 注释 off-by-one**：`trimmed[2..]` 漏掉 `AI?` 第 3 字符 `?`，指令前缀残留问号
- **`WatchMode` 忽略目录按绝对路径段匹配误伤**：用绝对路径 `dir` 逐段比对，祖先目录名（如 `/Users/x/target/...`）被误判为忽略目录；改为相对监视根的路径段
- **`WatchMode` 块注释在不适配语言误判**：`.py` 等文件里含 `/*` 的字符串/URL 被当 C 块注释吞掉后续行；`/* */`、`<!-- -->` 检测改为仅适用语言开启
- **`FallbackLLM.MaxBudget` setter 死代码**：setter 写私有静态 `_maxBudget`、读走 `Config`，`Config.FallbackMaxBudget = value` 永不生效；setter 直接落到 `Config`
- **`SessionManager.ListSessions` 跨目录分页错序**：先按目录取 `Skip/Take` 再拼接，跨 `sessions/` 与旧目录时排序/去重失效；改为全量收集 → 按 `saved_at` 降序 → `seen` 去重 → 统一分页

### 🐛 批处理 / 编辑器

- **`BatchRunner.CloneRepo` 无超时**：同步 `GitRunner.Run` 的 `WaitForExit` 无超时，`git clone` 因网络/认证问题永久挂起卡死整个批任务；改为 `CloneRepoAsync` + `RunAsync` 可取消，超时杀进程树
- **`DiagnosticManager` 并发访问非线程安全字典**：后台 lint 写、UI 线程读共享 `Dictionary`，改为 `ConcurrentDictionary`
- **`DiagnosticManager` exit 0 时丢弃 warning**：`StartsWith("✅")` 把「检查通过」整体跳过，但 stderr 里的 warning 会拼进 `combined`；改为只跳过「无法运行 linter」，继续解析 warning
- **`DiagnosticManager` PHP 严重级按整段输出判 warning**：`output.Contains("warning")` 会让一条 error 之外的无关 warning 把 error 也标成 warning；改为按每条匹配的 `error|warning` 前缀判定
- **`DiagnosticManager` Rust 错误定位按索引配对错位**：`note`/`help` 注解也带 `-->`，`errMatches[i]` 配 `locMatches[i]` 会让后续错误错位到注解位置；改为取该 error 之后最近的 `-->`
- **`EditorCore` 光标/删除切半 emoji 代理对**：`Backspace`/`Delete`/`MoveCursor` 按单 `char` 操作会在 emoji/CJK 扩展 B 中间切断；改为代理对感知，删除/移动整码点
- **`Syntax` 高亮忽略字符串转义**：`line.IndexOf('"', i+1)` 把 `\"` 当结束引号提前截断；新增 `FindStringEnd` 跳过反斜杠转义

## v0.71.6 (2026-08-17) — 上下文压缩/LLM 流式/Web 并发 11 项修复

系统性审查 `ContextManager`/`LLM`/`WebChat`，修复 11 个确定性 bug：上下文压缩省略行 off-by-one、UTF-16 切片切半代理对、LLM 400 回退死代码、流式工具调用重复触发、Web 版断连泄漏/槽位竞态。

### 🐛 上下文压缩（ContextManager）

- **裁剪省略行 off-by-one**：`SnipToolOutputs` 的 `lastWritten` 初值 `-2` 导致每条被裁剪输出开头多一句虚假的「省略 1 行」，改为 `-1`
- **UTF-16 切片切半代理对**：`flat[..20000]`/`text[..1000]`/`trimmed[..200]` 按码元切片会在 emoji/扩展区汉字（代理对）中间切断，经 JSON 编码后变成 U+FFFD 污染发往 LLM 的文本；新增 `TruncateByRunes` 按码点截断，与 `EstimateTokensText` 的 rune 感知一致
- **裁剪完成提示语义**：进度消息把「剩余容量」当「节省量」上报（`-(MaxTokens - current)`），改为上报实际节省量（裁剪前 − 裁剪后）

### 🐛 LLM 流式解析（LLM）

- **400 回退死代码**：`catch (HttpRequestException) when (StatusCode == BadRequest)` 永不命中——`CallWithRetryAsync` 对 4xx 返回响应而非抛异常，导致不支持 `stream_options.include_usage` 的端点返回 400 时被当成 SSE 流解析、静默返回空响应。改为在返回后检查状态码、400 则去掉 `stream_options` 重试一次（并顺带清理未使用的 `request` 变量）
- **流式工具调用去重**：`onToolCall` 在参数形成完整 JSON 后每次 delta 都会触发，无「该 index 已触发」守卫；新增 `firedToolCalls` 去重，防止同一工具调用被重复执行
- **日志预览切半代理对**：`ParseArgs` 失败日志的 `json[..200]` 改为 `TruncateForLog` 按码点截断

### 🐛 Web 并发（WebChat）

- **SSE 断连无检测**：服务端从不读流，`Closed` 仅在写失败时置位，客户端关标签页后若无后续广播，连接永久阻塞在 `Closed.Task` 上泄漏线程/连接槽位；改为 `Task.WhenAny` 同时监听底层流 EOF
- **`_clientSlot` 永不清理**：客户端断开只移除 `_clients` 不清 `_clientSlot`，字典无界增长 + 旧 clientId 永久占用槽位，反复刷新后新客户端回退槽位 0 串扰；断开时按「无其他连接复用该 clientId」条件清理
- **Interrupt 启动窗口丢失**：`IsBusy=true` 与 `slot.Cts` 赋值非原子，窗口内到达的中断被 `Exchange` 取到 null 丢弃；`Interrupt` 与 `StartSlotTask` 改为共享 `StartLock`
- **EnsureSlot 异常卡死槽位**：`IsBusy=true` 先于 `EnsureSlot` 且无回滚，`EnsureSlot` 抛异常时槽位永久 busy；改为异常时广播失败且不置 `IsBusy`
- **EnsureSlot 无锁双建**：多路由并发首建产生双 Agent 相互覆盖；加 `AgentLock` double-checked locking
- **BindClientSlot 不校验占用**：两个页面可绑到同一槽位互看对方对话；改为校验占用、被占用时拒绝并报错

## v0.71.5 (2026-08-17) — 提问多选布尔解析修复 + 聊天角色标识中文化

修复 `ask_user_question` 多选参数在 JNode 路径下永远解析为 false 的问题，并把聊天/导出里的角色标识（User/Assistant/System/Tool）统一改为中文。

### 🐛 修复

- **`AskUserQuestionTool` 多选布尔解析**：`ParseOneQuestion(JNode)` 用 `AsString() == "true"` 判断 `multiSelect`，但 JSON 布尔值（`"multiSelect": true`）的 `AsString()` 返回 null（`JKind.Bool` ≠ `JKind.String`），导致多选永远解析为单选。改为优先 `AsBool()`、兜底字符串 `"true"`（对齐 `MultiEditTool` 的处理）

### ✨ 聊天角色标识中文化

- **TUI 聊天角色标签**：`TuiListItem` 的角色头从 `You`/`Assistant`/`System`/`Tool` 改为 `用户`/`智能体`/`系统`/`工具`
- **对话导出角色名**：`ExportTool` 的 Markdown/HTML 导出标题中的英文 role（`user`/`assistant`/`system`/`tool`）统一改为中文

## v0.71.4 (2026-08-17) — 并发竞态修复 + LLM 提问对话框 + diff 滚动

修复多槽位/并行子智能体下的几处竞态，新增 LLM 提问对话框（`ask_user_question`）与 diff 对话框代码区滚动，并清理冗余文档。

### 🐛 并发竞态修复

- **AgentTool 独立实例**：每个 Agent 构造时持有独立的 `AgentTool` 实例（不再共享单例），避免 `ParentAgent` 被后构造的子智能体覆写（AgentId 继承失效、花费归并到错误实例、跨槽位重绑竞态）
- **WebChat 双启动锁**：`StartSlotTask` 加 `StartLock` 串行化 check-then-act，杜绝同槽位两个并发请求同时通过 `IsBusy` 检查导致双 Agent 并发 + 第二个 CTS 覆盖第一个（泄漏）
- **Esc 中断 CTS 原子摘除**：`Program.Repl` 中断槽位改用 `Interlocked.Exchange` 摘除 CTS，与后台 `finally` 的 `Dispose` 对齐，消除「读到非空 → 后台 Dispose → Cancel 抛 ObjectDisposedException」竞态
- **LLM token/请求计数原子累加**：`TotalPromptTokens`/`TotalCompletionTokens`/`TotalRequests` 改 `Interlocked` 累加，并行子智能体（`Task.WhenAll`）并发归并到同一父实例不丢增量；新增 `AddUsage` 供自测/归并原子注入

### 🛡️ 健壮性

- **鼠标输入默认关闭**：`TuiManager.MouseEnabled` 默认 false（鼠标定位尚未调好），设 `WAYCODER_MOUSE=1` 即可重新启用；`InputManager`/`TuiManager.Enter`/`Program.Repl` 统一按此开关
- **WrapText 省略号修复**：超行截断时正确预留省略号宽度并在末行补「…」，不再出现省略号被截断或吞掉最后一行
- **权限确认改模态弹框**：`ShowInlinePermission` 迁移为 `ShowPermissionDialog`（`TuiDialog.Permission` 模态框，Y=允许 A=全允 N/Esc=拒绝），替代旧行内权限块，详情超 800 字符自动截断

### ✨ LLM 提问对话框 + diff 滚动

- **`TuiDialog.Ask`**：标题独占一行（粗体）→ 消息正文（1~5 行，超出省略号）→ 选项列表（单选 ▶ / 多选 ☑，最多 9 行可滚动）→ 底部按钮，高度按内容精确计算；`UxHelper.Ask` 统一 TUI 弹框 / 非 TUI 编号菜单回退，`AskUserQuestionTool` 接入
- **diff 对话框代码区滚动**：`DiffPreview` 的代码对比区支持鼠标滚轮（每格 3 行）+ 右侧滚动条，内容超出屏幕时可滚动查看

### 🧹 文档清理

- 删除 `AGENTS.md`（旧 Agent Guide，已由 `CLAUDE.md` 取代）与误提交的临时文件

## v0.71.3 (2026-08-16) — TUI 会话记录按槽位隔离

把 Web 版「会话记录按槽位隔离」同步到终端 TUI：每个槽位（F1-F10）各自保存/加载/列出/删除自己的会话记录，互不串扰。

### ✨ 会话记录按槽位隔离（TUI）

- **会话管理器按当前槽位**：`SessionPicker.Show` 增加 `slot` 参数，`ListSessions`/`RenameSession` 按当前槽位作用；Ctrl+S 打开时只看到/操作本槽位会话
- **`/session` 命令按当前槽位**：`SessionCommand` 的 `list`/`save`/`load`/`resume` 全部加 `Program.ActiveSlotIndex`，各槽位独立保存/加载/恢复
- **退出自动保存按槽位**：`AutoSaveSession`/`AutoSaveException`/`PanicExit` 从 `_auto`/`_auto_slotN` 后缀改为 `SaveSession(..., "_auto", slot)`，物理隔离到 `sessions/slot{N}/` 子目录
- **当前会话 ID per-slot 化**：`_currentSessionId` 单值改为 `_currentSessionIds[10]` 数组，每槽位各自标记当前会话（Ctrl+S 切换/删除只影响本槽位）
- **切换会话只改本槽位模型**：Ctrl+S 切换会话从改全局 `_config.Model`/`_llm.Model` 改为 `_agent.LlmClient.Model`，不再污染其他槽位的默认模型
- **恢复向后兼容**：`TryRestoreSession`、`--resume`、`-c`、`/session resume` 恢复 `_auto` 时「槽位 0 优先，回退全局目录」，旧版本存全局的会话仍可恢复

### 🧪 自测

- 新增 `SessionSlot` 断言：`_auto` 存槽位 2 可恢复、跨槽位（0/3）不可见

## v0.71.2 (2026-08-16) — Web 会话记录按槽位隔离

每个浏览器页面（= 一个槽位 = 一个「虚拟用户 + 智能体」）拥有独立的会话记录：单端口下各页面只看到、保存、加载、删除自己槽位的会话，互不串扰。

### ✨ 会话记录按槽位隔离

- **`SessionManager` 增加槽位维度**：`SaveSession`/`LoadSession`/`ListSessions`/`DeleteSession`/`RenameSession`/`DeleteAllSessions` 均新增可选 `slot` 参数（默认 -1 = 全局共享，终端 TUI 沿用旧行为）；传 0-9 时记录写入 `~/.waycoder/sessions/slot{N}/` 子目录，各槽位物理隔离
- **槽位隔离模式不回退旧目录**：`LoadSession`/`ListSessions` 在 slot≥0 时只读该槽位子目录，不扫描全局目录或 `.corecoder` 旧目录，杜绝跨槽位读取
- **`DeleteAllSessions(slot)` 只清空该槽位**：清空按钮按当前页面槽位作用，不影响其他页面的会话记录
- **Web 层贯穿槽位**：`HandleCommand`/`WebSessionText` 增加 `slot` 参数；`/session save|load|list`、`/sessions`（GET 列表）、`/sessions/load|delete|rename|clear` 全部按当前客户端绑定的槽位作用
- **会话广播按槽位路由**：`BroadcastAll("sessions")` 改为 `BroadcastTo(slot, "sessions", SerializeSessions(slot))`，会话列表 SSE 事件只发给本槽位页面；前端 `fetchSessions`/删除/重命名/清空请求统一走 `?client=` 标识槽位

### 🧪 自测

- 新增 `SessionSlot` 断言组：槽位 0/1 各自只列自己会话、跨槽位加载返回 null、同槽位加载命中、`SerializeSessions(0|1)` 各自隔离、`DeleteAllSessions(0)` 只清空槽位 0 而槽位 1 保留

## v0.71.1 (2026-08-16) — Web 停止按钮按页面隔离 + 发送按钮改版

Web 版停止按钮修复为「每个浏览器页面只作用于自己的 agent」：后端从单活动槽位 + 单取消令牌 + 单顺序循环，改为每页面（SSE 客户端）绑定一个槽位、各槽位独立并发执行；发送按钮改为圆形 + 纸飞机图标。

### 🐛 停止按钮按页面隔离（根因修复）

- **客户端槽位绑定**：前端生成随机 `clientId`，所有请求走 `?client=<id>`（含 `/events` SSE 连接）；后端 `ResolveSlot` 把新客户端分配到空闲槽位（0-9）并复用
- **槽位分配跳过已绑定槽位**：`ResolveSlot` 分配空闲槽位时同时排除「已被其他客户端绑定」的槽位（不能只看 `Agent`/`IsBusy`——新客户端分配后不会立刻建 Agent，否则多个页面被分到同一个槽位互相干扰）
- **切换槽位同步 SSE 客户端槽位**：`BindClientSlot` 除更新 `_clientSlot` 字典外，同步改写该页面已建 `SseClient.SlotIndex`，否则切换后 `BroadcastTo(新槽位)` 匹配不到它 → 收不到 token/停止态、停止按钮失效
- **每槽位独立执行**：`WebSlot{Agent,IsBusy,Cts}` + `StartSlotTask` 后台 `Task.Run` 并发跑 `ChatAsync`（镜像终端 `Program.Repl.cs` 槽位模型），删掉全局 `_activeSlot`/`_roundCts`/`_input`/`MainLoopAsync`
- **作用域广播**：`BroadcastTo(slot)` 只写绑定该槽位的客户端；`BroadcastAll` 保留给全局事件（sessions）；`BroadcastStateForAll` 按各客户端自己的槽位刷新 state
- **交互桥 `ask` 按槽位路由**：`AsyncLocal<int> _currentSlot`（AOT 安全，项目已用于 bash cwd 跟踪）在 `StartSlotTask` 内 set，`WaitAnswerAsync` 据此只把提问发给发起该轮任务的页面
- **`OnSse` 委托加 `HttpRequest`**：SSE 连接建立时能读到 query 里的 `client` 标识

### ✨ 槽位切换保留状态

- **输入草稿按槽位记忆**：前端 `slotDrafts` 缓存每个槽位未发送的输入，切换槽位时保存当前、恢复目标，输入框内容互不串扰（切换时也重置 `isBusy`，避免残留旧槽位的停止态拦截发送）
- **聊天内容按槽位隔离**：每槽位独立 `Agent.Messages`，切换时从后端回放该槽位历史

### ✨ 发送按钮改版

- **圆形按钮**：`#send` 的 `border-radius` 13px → `50%`（46×46 成圆）
- **纸飞机图标**：`✈️` emoji 换成 Material「send」内联 SVG 纸飞机（`fill=currentColor` 随主题着色），忙态仍为 ⏹ 圆钮

### 🧪 自测

- 新增 `ParseClientQuery`（query 取 client）/ `PickFreeSlot`（槽位分配）纯函数断言
- 新增端点冒烟：两个 `?client=` 分配不同槽位、`/slot` 切槽后互不影响

## v0.71.0 (2026-08-16) — 安全加固 + 多槽位真并行 + 渲染一致性 + 健壮性

一轮系统性审查后的四批修复落地：Web 安全加固、修复「切换槽位后任务停止」的根因（10 个 agent 真正并行）、三端渲染一致性收敛、补齐非 bash 工具取消令牌与资源泄漏。

### 🔒 安全加固

- **XSS 修复**：`renderSuggest` 对文件名等提示框字段做 `escapeHtml` 转义，杜绝恶意文件名注入 HTML/脚本
- **`/shell` 权限确认**：Web 端执行 Shell 命令前走 `PermissionManager.CheckAsync`，非 YOLO 模式下不再无条件放行
- **`/fileref` `/filelist` 路径穿越限制**：`ResolveWithinRoot` 钳制路径于项目根目录内，越界返回「路径超出项目根目录」而非读取任意文件
- **CSRF 纵深防御**：状态变更请求（非 GET）校验 `Origin`（空 Origin 放行 curl/SSE）+ `Sec-Fetch-Site: cross-site` 兜底拦截漏带 Origin 的跨站请求

### 🐛 核心修复：多槽位真并行

- **槽位独立 LLM 克隆**（根因修复）：`GetSlotLlm` 对 `UseGlobal` 槽位返回 `_llm.Clone()` 而非共享 `_llm`，消除并发读写 `ModelOverride`/`_reasoningBuffer`/`_reasoningShown` 的竞态——修复「F1 任务跑到一半，切 F2 再切回 F1 就停了」的问题，10 个 agent 真正互不干扰并行
- **跨槽位文件锁冲突检测**：新增 `Agent.AgentId`（F1-F10）+ `ExecuteToolAsync` 注入 `_agent_id`，`FileLockManager` 按槽位归属识别跨 agent 资源锁定并报错提醒（此前始终按 "main" 判定，跨槽位冲突被误作同源续期静默吞掉）
- **WebChat 并发写安全**：`SseClient` 加 `WriteLock` 串行化 `Broadcast` 写入；`Agent.Messages` 序列化前防御性快照（`ToList`）
- **`_roundCts` 原子化**：`Stop`/`Interrupt`/`MainLoopAsync` 用 `Interlocked.Exchange`/`CompareExchange` 协调取消与释放，杜绝 dispose/cancel 竞态

### 🎨 渲染一致性（三端收敛）

- **裸标记修复**：`Program.Output.cs` 管道输出改用 `MarkupLine`，`«dim»` 标记不再裸写到终端
- **`SpectreToAnsi` 标签集补齐**：新增 `«underline»`/`«italic»`/`«strike»`/`«blue»`/`«magenta»`/`«white»` 等，与 `MapMarkupTag` 对齐；`«bold X»` 复合标签真正带粗体（此前粗体被丢弃）
- **Markdown 表格**：`SplitTableCells` 支持 `\|` 转义竖线（单元格字面 `|` 不误拆）；「先窥探再消费」避免单行 `| 文本 |` 被静默吞掉；无分隔行（表头+数据）也能解析

### 🛡️ 健壮性

- **非 bash 工具取消令牌**：`fetch`/`web_search`/`download`/`git`/`agent` 实现 `ICancellableTool`，中断时真正终止在途 HTTP 请求/子进程/子智能体，并区分「中断」（`OperationCanceledException` 向上抛）与「超时」（返回超时文案）
- **资源泄漏修复**：槽位 `Cts` 用 `Interlocked.Exchange` 原子摘除并 `Dispose`；`BackgroundTaskManager` 完成态任务保留上限 50 自动清除；spinner CTS 释放

### 🧪 自测

- 新增表格转义竖线 / 无分隔行 / 单行竖线不吞行测试
- 新增 `ICancellableTool` 接口实现断言（fetch/web_search/download/git/agent）
- 全量自测通过（3137 通过 / 0 失败）

## v0.70.0 (2026-08-16) — Web 特殊前缀输入 + 停止真中断 + 中间格式渲染

一轮 Web 交互补强：标题栏只显示智能体名、`!` Shell 与 `#` 文件引用两大前缀输入落地、停止按钮真正杀掉 bash 子进程，并确立「中间格式 → 各平台渲染」的着色架构（`«tag»…«/»` 统一表达，CLI/TUI→ANSI、Web→HTML）。

### ✨ Web 交互增强

- **标题栏只显示智能体名**：中间标签从 `智能体: 智能体N` 精简为 `智能体N`（随槽位切换更新）
- **`!` Shell 前缀**：输入 `! <命令>` 直接执行 bash 并显示输出（新增 `/shell` 路由，对标 Claude Code `!`）
- **`#` 文件引用前缀**：输入 `# <路径>` 读取文件注入当前对话上下文；`#` 后实时列出文件/目录补全（新增 `/fileref` `/filelist` 路由）
- **全角/半角归一化**：`／`→`/`、`！`→`!`、`＃`→`#`，中文输入法下前缀照常识别
- **命令提示框**：斜杠命令模糊匹配、`!` Shell 提示、`#` 文件补全，Tab/方向键选中回车确认
- **Shell 输出 tty 配色**：`ansiToHtml` 把 Shell 命令产生的裸 ANSI SGR 转 HTML span（XSS 安全转义），`.shell-output` 独立块等宽显示

### 🎨 中间格式渲染架构

- **`markupToHtml`**（Web 端）：对标后端 `SpectreToAnsi`，把 `«red»`/`«bold»`/`«dim»`/`«underline»`/`«italic»` 等中间格式转 HTML span，颜色值与 `ANSI_FG` 同源 `TuiColors`，三端观感一致
- **接入渲染管线**：`mdToHtml` 行内与 `renderToolOutput` 纯文本分支改用 `markupToHtml`，正文/表格单元格/工具输出里的 `«»` 标记正确着色
- **`/test markup`**：中间格式样例（颜色/文字特征/复合标签/表格内联/代码块原样），验证跨平台渲染；`/test ansi` 保留为「Shell 裸 ANSI 解码」测试

### 🐛 修复

- **停止按钮真中断**：`ICancellableTool` 接口 + 取消令牌贯穿 `Agent`→`BashTool`，中断时杀掉 bash 子进程并抛 `OperationCanceledException`（不吞），修复「停止后 shell 仍在跑」
- **Markdown 表格渲染**：修复转义竖线 `\|` 误拆列（`/perm [ask\|auto\|…]` 拆成 5 列）、`.md-table` 布局；分隔行对齐冒号解析（`:---` 左 / `---:` 右 / `:---:` 居中）应用 `text-align`

### 🧪 自测

- 新增「bash 取消令牌中断长命令」测试（`sleep` 被 800ms 令牌杀掉，耗时 < 5s）
- `TestUiLint` 跳过 `UI/WEB` 目录（HTTP/SSE 层不适用「禁止硬编码 Console/ANSI」约束）
- 全量自测通过（3094 通过 / 0 失败）

## v0.69.0 (2026-08-16) — Web 聊天界面完善

一轮 Web 聊天界面体验收尾：修复设置保存并发写 `.env` 导致的「保存失败」，补全代码块语法高亮、大/小模型双下拉、无 key 弹框、标题栏（智能体 + 版本号）、修改文件增删行统计，并新增 `/stats` `/recent` `/model` 斜杠命令。

### 🐛 修复

- **设置保存失败**：`Config.SaveToEnvFile()` 加 `SaveLock` 串行化读改写，修复 Web 设置面板 `Promise.all` 并发 POST 多项设置时并发写 `.env` 抛 `IOException` → 连接被服务端丢弃（无响应、`fetch` 拒绝）的竞态
- **模型对话框按钮被折叠**：`.model-card` 改 flex 三段布局（标题 + 搜索框固定、列表区 `flex:1; overflow-y:auto`、底部按钮固定），66 个模型不再把「确认/取消」顶出可视区
- **点击遮罩关闭对话框**：backdrop click-to-close（`e.target === e.currentTarget` 才关闭）

### ✨ Web 界面增强

- **大/小模型双下拉**：标题栏拆成「大模型:」+「小模型:」两个 `select`，各自 `onchange` 即时切换；小模型下拉仅列 5 个常用小模型（`deepseek-chat`/`deepseek-v4-flash`/`gpt-5.4-mini`/`gpt-4o-mini`/`deepseek-v4-pro`），下拉加宽 1.5 倍避免长名称截断
- **无 key 弹框**：切换模型时若该供应商无 API Key（且非 local/custom）→ 回退选中并弹 key 输入框，提交后写 `ApiKeyStore` + 持久化并完成切换
- **标题栏三分区**：左=APP 标题，中=当前智能体（`智能体: 智能体N` 随槽位切换更新），右=软件版本号（`Global.Version` 注入）
- **修改文件增删统计**：`EditFileTool.RecordChange` 记录 `+新增/-删除` 行数（`ChangedFileStats`），右栏文件面板显示 `+N/-M`
- **代码块语法高亮**：`highlightCode` 按语言 token 着色（`--tok-*` CSS 变量，明暗主题自适应）

### 🎛 Web 斜杠命令

- 新增 `/stats`（token/费用/请求统计）、`/recent`（最近修改文件）、`/model <名称>`（模糊匹配换模型），与终端命令对齐
- `WebServer` 响应加 `Cache-Control: no-store, no-cache, must-revalidate` 防页面缓存
- Web 模式强制开启 diff 预览（`Config.Instance.DiffPreview = true`）
- `WayCoder.csproj` 排除 `WayEngine/**/*` 构建

### 🧪 自测

- 全量自测通过（0 失败）

## v0.68.0 (2026-08-16) — UI 分类重构 + 大文件拆分

一次清偿两项工程债：UI 代码按 `UI/{Shared,CLI,TUI,GUI,WEB}` 五层分类归档（命名空间对齐目录），并把 4 个 1500+ 行的超大文件拆成 `partial class` 多文件，降低维护成本。附带修复 GitRunner 经典死锁。

### 🗂 UI 分类重构

- 目录五层归档：`UI/Shared`（Terminal/BoxBuffer/MarkdownRenderer/TuiColors/TuiHelper）、`UI/CLI`（Arguments/Commands）、`UI/TUI`（Base/Controls/Custom/Screens/Edit/ToolRenderers）、`UI/GUI`（预留）、`UI/WEB`（Web 服务器）
- 命名空间对齐目录：`WayCoder.Terminal`/`Arguments`/`Commands`/`UI.TuiBase` 等 → `WayCoder.UI.Shared.*`/`Cli.*`/`Tui.*`/`Web.*`
- 130+ 文件 `git mv` 归位，外部调用点经 `using` 同步更新

### ✂️ 大文件拆分（partial class）

- `Program.cs` 2536 行 → 4 文件（`Program` + `Repl`/`Commands`/`Output`）
- `WebChat.cs` 2272 行 → 5 文件（`WebChat` + `WebAssets` + `Serialization`/`Commands`/`Interaction`）
- `ChatScreen.cs` 2237 行 → 5 文件（`ChatScreen` + `Input`/`Dialogs` + `SlotState` + `ChatMsg`）
- `Agent.cs` 1563 行 → 5 文件（`Agent` + `Tools`/`Feedback`/`Commit`/`Loop`）
- 独立类型抽取：`WebAssets.Html`（~1000 行纯 HTML 常量）、`SlotState` 枚举、`ChatMsg` 类

### 🐛 GitRunner 死锁修复

- `Run`/`RunAsync` 并发读取 stdout/stderr，修复「同步先读 stdout、stderr 缓冲写满 → 进程阻塞 → stdout 永不 EOF」的经典死锁
- 此前脏工作树下 `--test` 因大量 git 换行警告填充 stderr 缓冲而挂起

### 🧪 自测

- `SelfTest.Chunk11`（UI Lint + TuiTableList）接入 runner（此前被遗漏未执行）
- UI Lint 白名单更新：终端 ANSI 底层原语（AnsiString/AnsiTty/RenderBuffer/Terminal）+ CLI 参数层（BuiltinArgs）+ ChatScreen.Dialogs
- 自测 13 partial 文件、3070 项全部通过（0 失败）

## v0.67.0 (2026-08-16) — Web 多模态上传（图片/音频）

补齐 Web 端多模态输入短板：输入栏新增 📎 上传按钮，图片入 vision 队列、音频转录为文字后自动发送，对标终端 `view_image` / `transcribe` 工具。

### 📎 Web 多模态上传

- 输入栏新增 📎 按钮 + 隐藏 `<input type="file" accept="image/*,audio/*">`，选中即上传
- 后端 `POST /upload?kind=image|audio`：图片走 `LLM.QueueImage`（vision 模型门控，非 vision 模型友好报错）、音频走 `TranscribeAudioTool`（Whisper 转录，成功后自动作为 user 消息发送）
- 二进制正文支持：`HttpRequest` 新增 `RawBody` 字节数组（避免 UTF-8 解码损坏图片/音频），`HttpServer` 两阶段读取（头 64KB 上限 / 正文普通 1MB、`/upload` 32MB）
- 纯函数辅助：`ParseUploadKind` / `IsImageExtension` / `SafeExtension` / `IsTranscribeError` / `ParsePath`（便于自测）
- 大小限制：图片 ≤5MB、音频 ≤25MB；扩展名白名单校验 + 安全落盘（`UploadDir` 临时目录）

### 🧪 自测

- 新增：`ParseUploadKind`/`SafeExtension`/`IsTranscribeError`/`IsImageExtension` 纯函数 + `ParseHttpRequest(byte[])` 二进制正文 + `ParsePath`
- 全量自测通过（0 失败）

## v0.66.0 (2026-08-16) — Web Diff 预览（写前逐 hunk 确认）

对标终端 `WAYCODER_DIFF_PREVIEW=1`：Web 模式下 write_file/edit_file/multi_edit 写文件前，把 diff 逐 hunk 推送到浏览器确认，不再因无 Console 而跳过。

### 🔍 Web Diff 预览

- `IWebInteraction` 新增 `DiffConfirmAsync(filePath, hunks, timeoutMs)` + `DiffConfirmResult` 结果类型
- `DiffPreview.Show` 增加 Web 分支：`UxHelper.WebInteraction != null` 时经桥弹浏览器 diff 对话框（`GetAwaiter().GetResult()` 阻塞等待，无 SynchronizationContext 死锁风险；超时/取消 → 拒绝）
- `WebChatServer` 实现 `DiffConfirmAsync` + `SerializeHunks` + `ParseDiffAnswer`（纯函数，便于自测）
- 前端 `ask` 事件新增 `kind:"diff"`：每 hunk 一个复选框（默认勾选）+ 行级红/绿/灰着色（明暗主题自适应）+ 「全部接受 / 应用选中 / 全部拒绝」三按钮
- 复用现有 `/answer` 路由回传结构化决策（`{"decision":"accept|reject|partial","accepted":[索引]}`）

### 🧪 自测

- 新增：`ParseDiffAnswer` 纯函数（accept/reject/partial/null/非法）+ `SerializeHunks` + `DiffPreview.Show` Web 分支（mock 交互桥）
- 全量自测通过（0 失败）

## v0.65.0 (2026-08-16) — Web 斜杠命令路由

Web 输入框支持斜杠命令，对标终端 REPL。未识别命令回退为普通 Agent 消息，纯 UI 命令前端直接拦截。

### ⌨ Web 斜杠命令

- 后端 `HandleCommand` 纯函数 + `POST /command` 端点，覆盖 Web 有意义命令子集
- 前端 `/` 开头输入路由到 `/command`，未识别回退 `/chat`；纯 UI 命令（`/theme`、`/settings`、`/model`）前端直接拦截
- 命令输出用 `.msg.cmd` 独立样式（accent 左边框 + 满宽 + Markdown 渲染）

### 命令清单

| 命令 | 说明 |
|---|---|
| `/help` | 命令帮助 |
| `/perm [ask\|auto\|smartauto\|yolo]` | 权限模式 |
| `/model` | 打开模型选择窗口（前端） |
| `/model list` | 列出模型 |
| `/theme` | 切换明暗主题（前端） |
| `/settings` | 打开设置（前端） |
| `/reset` | 清空当前会话 |
| `/session [list\|save\|load <id>]` | 会话管理 |
| `/tokens` | Token 统计 |
| `/mcp` | MCP 服务器状态 |
| `/todo` | 任务列表 |
| `/interrupt` | 中断当前任务 |

### 🧪 自测

- 新增 19 条：`HandleCommand` 纯函数 + `/command` 端点冒烟 + HTML 结构
- 全量自测通过（0 失败）

## v0.64.0 (2026-08-16) — Web UI 完善：Markdown 渲染 + 权限模式开关 + 模型选择窗口

在 v0.63.0 三栏改版基础上继续完善 `--web` 浏览器界面：聊天消息 Markdown 渲染、权限模式从「强制 YOLO」改为用户可选、模型选择独立窗口、设置窗口居中。全程零第三方依赖、手搓渲染器、AOT 安全。

### 📝 Markdown 渲染（手搓 XSS 安全渲染器）

- 聊天 assistant 消息从纯文本升级为 Markdown：代码块（```` ```lang ````）、行内代码、标题 `#`~`######`、无序/有序列表、引用、表格、水平线、粗体/斜体、链接
- 安全策略：**先 `escapeHtml` 转义再结构化**，链接仅允许 `http/https`（`javascript:` 直接拒绝），代码块内容不解析内部 Markdown
- 流式体验：流式期间用 `textContent` 追加（快、不闪烁），`done/interrupted/failed` 时 `finalizeAssistant()` 一次性转 Markdown；`.streaming` class 区分流式/完成态，避免 `white-space` 冲突
- 修复表格分隔行被误当数据行的 bug（`i++` → `i += 2`）

### 🛡 权限模式开关（Web 从「强制 YOLO」改为用户可选）

- 后端 `SerializeState` 新增 `permMode` 字段 + `POST /perm` 端点
- 前端顶栏新增 `🛡 Ask / ✅ Auto / 🧭 SmartAuto / ⚡ YOLO` 下拉，切到 Ask 后权限确认框真正经交互桥弹浏览器对话框（此前 Web 模式强制 YOLO，确认框永不触发）

### 🧠 模型选择窗口 + 设置居中

- 顶栏模型下拉改为独立 `model-modal`：搜索过滤 + 按供应商分组 + 上下文窗口/价格元数据 + 需 key 标记（点选自动弹 key 输入）
- 设置抽屉从贴边弹出改为**居中弹出**（`top:50%; left:50%; translate(-50%,-50%)`，缩放过渡）

### 🧪 自测

- 新增 9 条：Markdown 结构/`finalizeAssistant`/流式态样式/权限模式下拉/`/perm` 端点冒烟
- `mdToHtml` 渲染器另用 node 单独 21 条单测覆盖（代码块/标题/列表/表格/引用/XSS 转义等）
- 全量自测通过（0 失败）

## v0.63.0 (2026-08-16) — Web 聊天界面三栏改版 + 交互桥

`--web` 浏览器聊天界面从单栏升级为三栏，并修复「工具输出不滚动」与「网页版无提问对话框」两个致命问题。全程零第三方依赖、AOT 安全、跨平台、手搓 HTTP+SSE。

### 🖥 三栏布局

- **左栏 · 会话记录**：上半 F1-F10 槽位条（白底=当前、有历史=高亮、点击切换），下半历史持久化会话列表（预览 + 模型 + 时间 + 消息数，悬停删除/重命名，底部「新建会话」按钮）
- **中栏 · 聊天**：保留消息流 + 输入框 + 发送/停止
- **右栏 · 信息面板**：📋 任务（含状态色点）、💰 Token/费用（本轮 + 累计 + 速率）、🔧 修改文件、🔌 MCP 服务器、🧠 LSP 会话；自动实时刷新（SSE 事件触发 + 2 秒轮询，页面隐藏时跳过）
- **设置页两列**：左列类别导航（首个默认高亮），右列当前类别的详细设置项，点击类别切换右侧内容

### 🐛 滚动 bug 修复

- `tool_output` 此前被追加到过期的 assistant 流元素（位于 tool 卡片上方隐藏区），导致工具输出「一直在下方不上滚」。现改为**两个独立流指针**：`assistantStreamEl`（assistant 文本流）+ `toolOutputEl`（独立 `.tool-output` 等宽可折叠块），按状态机正确分离，工具输出独立成块、按到达顺序显示在底部并自动滚动

### 🆘 Web 交互桥（致命问题修复）

- 此前 Web 模式下 `ask_user_question` 工具与权限确认框走 `Console.ReadLine()` 阻塞在后台线程，浏览器用户看不到任何对话框。现抽象「交互模式」：`UxHelper.IWebInteraction` 接口 + `WebInteraction` 注入点
- `WebChatServer` 实现 `IWebInteraction`（`Start` 注入 / `Stop` 移除）：提问/确认经 SSE `ask` 事件弹浏览器对话框（select 选项按钮 / multi 复选 / text 输入 / confirm 允许+总是允许+拒绝），`POST /answer` 回填应答，`Task.WhenAny` 超时兜底
- `AskUserQuestionTool` 与 `PermissionManager` 改异步走桥；`WebInteraction == null` 时仍走原 TUI/Console 路径，终端模式零影响

### 🔌 后端新增端点与访问器

- `GET /panel` — 右栏六类数据（`SerializePanel` 纯静态函数）
- `GET /sessions`、`POST /sessions/new|load|delete|rename` — 历史会话管理（`SerializeSessions` 纯静态函数）
- `POST /answer` — Web 交互桥应答
- `LspTool.ActiveSessions` 公开访问器 + `ActiveLspInfo` record（右栏 LSP 会话展示）

### 🧪 自测

- 新增 `SerializePanel`/`SerializeSessions`/`LspTool.ActiveSessions`/交互桥/端点冒烟测试
- 全量自测通过（0 失败）

## v0.62.2 (2026-08-16) — P0-P4 健壮性与安全加固

对全仓库做系统性安全审计与压力测试，修复命令注入、RCE、权限绕过、SSRF、资源泄漏、整数溢出、OOM、并发竞态与 Web 资源滥用等一批硬伤。全程零反射、AOT 安全、跨平台、零新依赖。

### 🔒 安全加固（P0-P2）

- **P0 `test` 工具 RCE**：`test` 工具经 shell 执行命令却绕过权限确认与 `BashGuard` 黑名单（`/perm yolo` 之外仍可 `test "curl ...|sh"`）。现加入 `PermissionManager.DangerousTools` + `TestTool.ExecuteAsync` 前置 `BashGuard.CheckBanned`
- **P1 `git` 命令注入**：`git -c alias.x='!cmd'` / `core.pager` / `core.sshCommand` 可使 git 内部经 shell 执行任意命令，完全绕过 `BashGuard`。新增 `GitTool.HasDangerousGitArgs` 拦截 `-c/--config/--config-env/--upload-pack/--receive-pack/--exec`（含 `-c=` 前缀）
- **P1 `/checkpoint` 命令注入**：`description` 未经清洗拼进 `git stash push -m "..."` 经 shell 执行，`/checkpoint x"; rm -rf ~; #` 可注入。新增 `SanitizeCheckpointLabel` 清除 shell 元字符
- **P1 `cp`/`mv`/`find_replace` 权限绕过**：三个文件操作工具不在确认名单，Agent 可无确认覆盖/移动文件。现加入 `DangerousTools`
- **P2 `doc` 工具 SSRF**：`action=fetch` 的 URL 未做 SSRF 校验，可诱导访问云元数据（169.254.169.254）与内网服务。现复用 `SsgfGuard.CheckUrl`/`CheckDns` 拦截
- **P2 `RasterImage` 整数溢出**：`width * height * 4` 按 int 溢出为负绕过长度检查 → 改 `long` + 超 2GB 拒绝；`AnsiString.TruncateByWidth` 悬空 ESC 序列 `text[i..(j+1)]` 越界 → 终止符钳制
- **P2 `LspTool.ReadResponse` 挂起**：同步阻塞读 + 未使用的超时 token，服务器不响应时永久挂起。现异步读 + token 超时 + EOF 保护 + `Content-Length` 长度上限（防恶意巨大缓冲区分配）
- **P2 `CheckpointManager` stderr 死锁**：只读 stdout，命令大量写 stderr 时管道缓冲区写满阻塞子进程。现并行排空 stdout/stderr
- **P2 `ErrorLog` 锁内 IO + `_dirty` 不复位**：缓冲区满时在 `lock` 内 `File.AppendAllLines` 阻塞其他线程；抽出 `AppendToFile` 锁外刷盘并修正 `_dirty` 复位

### 🛡 健壮性加固（P1：OOM / 死循环 / 栈溢出防护）

- **不可信尺寸字段护栏**：`PdfParser`（深层嵌套数组深度护栏）、`PngDecoder`（负数 chunk 长度）、`BmpCodec`/`JpegCodec`、`OfficeExtractor`（zip bomb 解压上限）、`CfbParser`（流 Size 校验）对图片宽高、CFB 流尺寸等不可信字段加防御护栏
- **绘图引擎护栏**：`DrawCanvas` 防 NaN/Inf 半径、超大半径钳制到画布对角线、退化椭圆除零；`DrawEngine` 画布像素数上限 25MP（`canvas W H` 超大尺寸防 OOM）+ 超大画布自动跳过 3× 超采样

### 🔀 并发与资源安全（P3）

- **`ModelOverride` 竞态**：`Agent.WithModelOverrideAsync` 用 try/finally 保证临时切换的小模型恢复，异常不再把 `ModelOverride` 永久污染导致后续请求静默降级
- **线程安全集合**：新增 `ThreadSafeStringSet` 替代无锁 `HashSet`（`EditFileTool.ChangedFiles` / `PermissionManager.AutoAllowed`）；`FileTracker` 7 处、`BackgroundTask.Output`、`LruCache`（读写锁 + 回调锁外调用防 `NoRecursion`）加锁；`FileLockManager` 过期强占改 `TryUpdate` CAS 原子更新
- **响应体 / 进程泄漏**：`LLM` 5xx/429 重试前 `Dispose` 响应体、`doc`/`fetch` 响应体 `using`、`LspTool` 进程 Kill 后 `Dispose`（`KillAndDispose`）

### 🌐 跨平台与 Web（P4 / P4-2）

- **`CrossPlatform` 统一运行器**：shell（`cmd.exe` vs `/bin/bash`）+ python（`python` vs `python3`）按平台选择；`HooksManager`/`LintTool` 的 `.py` 脚本改用 `CrossPlatform.PythonExecutable`，消除硬编码导致的跨平台失效
- **Web 资源上限 + XSS**：`WebServer` 请求正文 1MB 上限（413）、连接数 32 上限（`SemaphoreSlim`）；`WebChat` SSE 客户端 16 / 待处理输入队列 100 上限（429）；`HtmlEscape` 转义工具名/参数防 XSS

### 🧪 自测

- 新增 P1/P3/P4/P4-2/P0-P2 各批次共 **164** 项测试（`TestP1Hardening`/`TestP3Concurrency`/`TestCrossPlatform`/`TestP4WebResource`/`TestP0P2Hardening`）：命令注入拦截、权限名单、SSRF、整数溢出、画布护栏、线程安全集合、Web 资源上限、HTML 转义、跨平台运行器等
- 总计 **2965** 项自测全部通过（0 失败）

## v0.62.1 (2026-08-16) — 老式 Office/WPS 提取器真实文件修复

对着 WPS 自带真实模板文件端到端验证后，修复 `LegacyOffice` 三个提取器对真实文件的解析缺陷（此前单元测试夹具与解析器共享同一套错误假设，2795 项自测全绿但真实文件仍解析失败）。

### 🐛 修复

- **DOC FIB 布局 off-by-4**：`cslw` 偏移应为 `34 + csw*2`（原误为 34）、`fibRgLwOff` 应为 `36 + csw*2`（原误为 32 + csw*2）、漏读 `cbRgFcLcb` 2 字节，导致真实 `.doc` 报「无效 DOC：FIB 截断」。修复后 `secdoctemplate.doc`/`Austere.doc` 正确提取中文正文
- **XLS 空白表格 dump 元数据**：BIFF8 空白表格（空 SST）此前退化到 UTF-16 扫描，把字体名/数字格式/表名当正文输出；现按 BOF 版本（≥0x0500）判定现代格式，无文本直接返回「无文本内容」
- **XLS 加密检测**：新增 `FILEPASS`（0x002F）记录检测，密码保护文件返回「已加密」而非「无文本内容」
- **PPT 分层嵌套解析**：PowerPoint Document 流是分层容器结构（容器 `recVer=0xF`），文本 atom 嵌在容器内部，此前平铺扫描跳过容器内所有子记录导致「PPT 无文本内容」；现递归下降进容器，`newfile.dps` 正确提取母版文本
- **PPT 加密检测**：按 `Current User` 流 `CurrentUserAtom.headerToken` 高 16 位（0xF3D1）判定标准加密

### 🧪 自测

- 新增 6 项测试：XLS 空白表格不 dump 元数据、XLS 加密文件（FILEPASS）、PPT 嵌套容器文本、PPT 加密分支、端到端 CFB 加密检测（headerToken）、端到端未加密正常提取
- 总计 **2801** 项自测全部通过（0 失败）

## v0.62.0 (2026-08-16) — 老式二进制 Office / WPS 文档读取

补齐 `.doc/.xls/.ppt` 老式二进制 Office 文档与 WPS 老后缀 `.wps/.et/.dps` 的文本读取。这些格式本质都是 CFB（Compound File Binary / OLE2）复合文档，此前 `read_file` 只能读 docx/xlsx/pptx，遇到二进制 Office 会报「无法识别」。全程零第三方依赖、零反射、跨平台，延续「手搓」原则。

### ✨ 新增

- **手搓 CFB 解析器 `CfbParser`**（`WayCoder/Infra/CfbParser.cs`）：按扇区 + FAT + DIFAT + 目录组织解析复合文档，支持常规扇区链（512/4096 字节）与 mini 扇区链（<4096 小流），按名取流（`GetStream`/`HasStream`/`StreamNames`）；FAT 链遍历带 100 万次防御上限，损坏输入返回 `null` 不崩
- **老式格式文本提取器 `LegacyOffice`**（`WayCoder/Infra/LegacyOffice.cs`）：
  - **二进制 DOC**：FIB 解析（wIdent/flags/csw/cslw → fibRgLw/fibRgFcLcb）+ piece table 提取文本。正确实现 MS-DOC 规范——`fcClx` 指向**表流**（0Table/1Table 由 `fWhichTblStm` 选择）、`Pcd.fc` 指向 **WordDocument 流**（bit31 为保留位 r1，bit30 为 fCompressed）、压缩文本字节偏移 = `fc/2`（非压缩 = `fc`）、cp1252 单字节映射（0x80–0x9F → € ‚ ƒ „ … 等）
  - **二进制 XLS**（BIFF8）：SST 共享字符串表（`0x00FC`）+ LABEL 内联标签（`0x0204`）+ LABELSST（`0x00FD`）+ STRING/RSTRING；BIFF 字符串 flags（fHighByte/fExtSt/fRichSt）解析
  - **二进制 PPT**：RecordHeader 遍历 + `TextCharsAtom`（0x0FA0 UTF-16）/ `TextBytesAtom`（0x0FA8 ANSI）文本 atom
  - **RTF 剥离**：控制字（`\word`/`\wordN`）、控制符号（`\'hh`/`\~`/`\_`/`\-`）、`\*` 跳过目标组、`\uN` Unicode 转义
  - **容器识别**：按文件头魔数区分 CFB / ZIP / RTF / HTML / 纯文本，扩展名不可靠时仍正确路由
- **`read_file` 分发**：`.doc/.wps` → DOC、`.xls/.et` → XLS、`.ppt/.dps` → PPT（WPS 老后缀与 Office 老后缀共用同一套解析器）

### 🧪 自测

- 新增 22 项测试（`TestWps`）：CFB 解析 round-trip（小流走 mini 链、大流走常规扇区、未知名返回 null）、容器识别（CFB/ZIP/RTF/HTML/纯文本）、二进制 DOC 提取（Hello World + 中文 + **压缩文本折半定位**）、XLS（SST + LABEL）、PPT、RTF 剥离、端到端 `.wps → read_file`
- 测试夹具 `BuildCfb`/`BuildDocWordStream`/`BuildDocTableStream`/`BuildXlsWorkbook`/`BuildPptStream` 手搓构造最小合法 CFB/BIFF/PPT 结构，piece table 按规范落在表流
- 总计 **2795** 项自测全部通过（0 失败）

## v0.61.0 (2026-08-16) — Web 界面完整化（对标 DeepSeek Harness）

把 `--web` 浏览器聊天界面从「单 Agent + 固定模型」扩展为完整的 Web UI，对标 DeepSeek Harness 的 `dsh web`（黑白主题、圆角、换模型、输 key、设置、多槽位）。全程延续零第三方依赖 + AOT 安全 + 跨平台 + 手搓原则。

### ✨ 新增

- **黑白双主题 + 圆角风格**：`data-theme="dark"|"light"` 两套 CSS 变量，`localStorage` 持久化，默认深色；全局圆角（消息卡片 14px / 按钮 10px / 输入框 14px）
- **模型下拉**（按 provider 分组）：`GET /models` 返回 `ModelCatalog.All`（含 `hasKey` 字段），`POST /model` 换当前槽位模型
- **输入 API Key**：`POST /key` 按供应商存 `ApiKeyStore`（`~/.waycoder/api_keys.json`），换到无 key 供应商时前端弹 key 输入框；`secret` 类型设置项返回 masked 不泄露明文
- **设置面板**：`GET /settings` 返回 `Config.SettingSchema()` 按 Category 分组（text/number/select/secret/toggle 对应控件），`POST /settings` 走 `TrySetPropValue` + `SaveToEnvFile`，右滑抽屉交互
- **槽位切换（F1-F10）**：`POST /slot` 切换多 Agent 工作区槽位，每槽位独立 LLM + 历史（惰性创建 `EnsureSlot`），顶栏胶囊指示条（当前=高亮、有历史=实线）
- **LLM 运行时重配置**：`LLM.Reconfigure(apiKey, baseUrl)` 让 `ApiKey`/`BaseUrl` 改为 `{ get; private set; }`，换供应商无需重建 Agent、不丢对话历史；`ApplyModel` 换模型流程（模型目录 Url 优先 + `UpdateContextWindow` + 持久化）

### 🧪 自测

- 新增 25 项测试（`TestWebFull`）：`LLM.Reconfigure`（key/baseUrl/Endpoint/Model）、`SerializeModels`（分组 + hasKey）、`SerializeSettings`（分组 + secret 字段）、`SerializeState`/`SerializeHistory`、`ApplyModel` 非法模型报错、`ProviderHasKey`（local/custom 无需 key）、端点冒烟（`GET /models` `/state` `/settings`、`POST /slot` `/model` `/settings` 成功与错误分支）
- 端到端原生进程验证：10 个端点全部正常（换模型/换 key/设值/槽位切换返回 `{"ok":true}`，非法输入返回结构化错误），`.env` 验证后恢复
- 总计 **2774** 项自测全部通过（0 失败）

## v0.60.0 (2026-08-15) — 浏览器聊天界面（--web）

对标 deepseek-harness 的 `--web`，新增本地 HTTP 服务 + 浏览器聊天界面：`waycoder --web [端口]` 启动服务并自动打开浏览器，在网页里与 Agent 流式对话，摆脱终端环境限制（远程服务器、无 TTY 场景），获得更友好的 Markdown 渲染、流式输出与工具调用可视化。全程零新依赖、零反射，符合「跨平台 + 手搓」原则。

### ✨ 新增

- **手搓 HTTP 服务端 `HttpServer`**（`WayCoder/Web/WebServer.cs`，纯 BCL，AOT 安全）：`TcpListener` 监听 `127.0.0.1:<端口>`（默认 9527，`WAYCODER_WEB_PORT` / `--web 端口` 覆盖，仅回环不暴露公网）；HTTP/1.1 请求解析（请求行 + 头 + `Content-Length` 正文）；SSE 长连接（`text/event-stream`，`event:`/`data:` + `\n\n`）。纯函数 `ParseHttpRequest`/`SseEvent`/`FindHeaderEnd`/`ParseContentLength` 便于自测
- **Agent 桥接 `WebChatServer`**（`WayCoder/Web/WebChat.cs`）：把 `Agent.ChatAsync` 的三个流式回调（onToken/onTool/onToolOutput）转 SSE 事件广播（token/tool/tool_output/done/interrupted/failed）；`ConcurrentQueue` 输入队列 + 可重置 `CancellationTokenSource` 支持 `/interrupt` 中断；`/history` 回放 `Agent.Messages` 中 user/assistant 消息
- **内嵌前端**（单 `const string Html`，无构建、无外部 CDN、离线可用）：深色主题聊天界面，`EventSource('/events')` 收流式 token、`fetch POST /chat` 发消息、`fetch POST /interrupt` 中断，工具调用卡片 + 流式追加 + 自动滚动
- **CLI 接入 `WebArg`**：`BuiltinArgs.RegisterAll()` 注册 `--web`（`ValueCount -1` 可选端口）；`Program.RunWebAsync` 强制 YOLO（web 无终端弹权限框）→ 启动服务 → `OpenBrowser`（macOS `open`/Windows `start`/Linux `xdg-open`，`WAYCODER_WEB_NO_OPEN=1` 禁用）→ Ctrl+C 优雅退出自动保存会话
- **保持单文件发布**：HTML 内嵌为 `const string`，无外部文件、无新依赖，AOT 单文件 exe 不变

### 🧪 自测

- 新增 Web 17 项测试：`ParseHttpRequest`（GET/POST/查询串/畸形请求不崩）、`SseEvent` 格式化、`FindHeaderEnd`/`ParseContentLength`、HTML 含关键标记（EventSource//chat//interrupt）、端到端 `HttpClient` GET `/` 冒烟
- 本地原生二进制端到端验证：`--web 9999` → `GET /` 返回 HTML、`GET /history` 返回 `[]`、`POST /interrupt` 返回 `ok`、YOLO 权限切换与 URL 打印正确
- 总计 **2749** 项自测全部通过（0 失败）

## v0.59.0 (2026-08-15) — 手搓 PDF 解析器替代 PdfPig

彻底移除最后一个重依赖第三方库 PdfPig，手写纯 BCL 的 PDF 文本提取器，消除其编译警告与不可修复/安全泄漏隐患，符合「优先开源、无开源则手搓」的第三方库选型原则。

### ✨ 新增

- **手搓 PDF 解析器 `PdfParser`**（`WayCoder/Infra/PdfParser.cs`，约 900 行纯 BCL，AOT 安全、零反射、零依赖）：
  - 文件结构解析：`%PDF` 头校验 → `startxref` 定位 → xref 表 / xref 流（PDF 1.5+）双路 + `/Prev` 增量链回溯 → 间接对象（字典/数组/名字/字面字符串/十六进制字符串/数字/引用/流）递归解析，带对象缓存与 `<<` 字典→`stream` 流内联识别
  - 流解压：`FlateDecode`（复用 `ZLibStream`）+ `ASCIIHexDecode` + `ASCII85Decode` + `Filter` 数组链式过滤，无 filter 原样返回
  - 页面树遍历：Catalog → Pages → Kids 递归收集，`/Count` 校验
  - 内容流文本提取：`BT`/`ET` 文本块 + `Tj`/`TJ`/`'`/`"` 显示文本 + `Tf` 字体切换 + `Td`/`TD`/`T*`/`Tm` 换行判定（负位移/绝对 y 下降），`TJ` 大负 gap 插空格；**内容流用独立静态字节级解析器**（与文件结构解析解耦）
  - 字体编码：`/ToUnicode` CMap（bfchar 单码 + bfrange 范围）> Type0 `/Identity-H`（UTF-16BE）> 简单字体 `/WinAnsiEncoding`（CP1252）+ `/Differences` 字形映射 + Latin-1 近似 + UTF-16BE/LE BOM 自动识别；`/Info /Title` 元数据解码
- **`PdfExtractor` 重写**：公开 API 完全不变（`Extract`/`GetMeta`/`PdfExtractResult`/`PdfPageContent`/`PdfMeta`/`ToMarkdown`），内部改用 `PdfParser`；空行压缩逻辑保留；损坏/加密/不支持结构（object stream）优雅报错不崩溃
- **移除 PdfPig 依赖**：`WayCoder.csproj` 删除 `PackageReference Include="PdfPig"`，编译警告随之消失

### 🧪 自测

- 新增 PdfParser 19 项测试：最小 PDF 构造（xref 表 + 页树 + 内容流 + 标题）→ 页数/标题/两行文本提取；非 PDF/空数据返回 null；`PdfExtractor` 公共 API（Extract/GetMeta/ToMarkdown/不存在文件/损坏文件）错误分支
- 本地用 `cupsfilter` 生成的真实 PDF（含中文）端到端验证：文本完整提取（68 字符无乱码）
- 总计 **2732** 项自测全部通过（0 失败）

## v0.58.1 (2026-08-15) — 运行轨迹记录 + OpenClaw 竞品分析

对标 OpenClaw 的 trajectory JSONL 回放，新增运行轨迹记录器，把每次 Agent 运行的完整过程落盘为版本化 JSONL 事件流，为调试 agent 行为、评估模型质量、复现 bug 提供可观测基石。

### ✨ 新增

- **运行轨迹 `Trajectory`**：版本化 JSONL 事件流（`traceSchema`/`schemaVersion`/`runId`/`sessionId`/`type`/`ts`/`seq`/`data`），四类事件——`run_start`/`llm_turn`（每轮 token+内容长度+工具数+推理长度）/`tool_call`（工具名+入参/结果摘要+成败+耗时）/`run_end`（轮次+累计 token 汇总）；落盘 `.waycoder/trajectory/<runId>.jsonl`（已被 `.gitignore` 覆盖）；`WAYCODER_TRAJECTORY=0` 关闭；纯手搓 JSONL 追加（`File.AppendAllText` + lock + Interlocked 序列号），AOT 安全、零依赖
- **`ChatAsync` 薄包装重构**：主循环抽为 `ChatAsyncCore`，外层 try/finally 统一落 `run_end`——无论正常完成/异常/取消/提前返回都不漏（轨迹记录失败静默降级，不影响主流程）
- **竞品分析文档** `docs/openclaw-analysis.md`：OpenClaw 架构对比 + 4 个可借鉴点（轨迹回放/上下文降级原因码/工具声明式元数据/安全自审计），轨迹回放已落地

### 🧪 自测

- 新增 Trajectory 21 项测试：截断纯函数（头尾保留/标记/极小 maxChars）、Enabled 标志、JSONL 事件流落盘/读回（事件类型顺序、schema 字段、run_end 汇总、tool_call 成败）
- 总计 **2713** 项自测全部通过（0 失败）

## v0.58.0 (2026-08-15) — 对标 deepseek-harness：持久 shell + 环境清理 + 进程树终止 + 调度器

对照 deepseek-harness 源码逐项借鉴，补齐一批执行层的健壮性能力：`bash` 支持 `session_id` 持久 shell 会话（跨命令共享 cwd/env/shell 状态）；子进程启动前清理凭据形状的环境变量（防密钥经 env 泄漏）；全部子进程终止统一走 `entireProcessTree` 进程树终止（父进程被杀子进程一并清理）；`RetryPolicy` 增加对称 jitter（±10%）打破多客户端同时重试的惊群；工具调用改为按 `ExecutionMode`（Parallel/Exclusive）分批并行调度（批内有界并发 4 + 独占串行 + 按模型声明顺序提交）；并新增 `ToolResultClassifier` 统一区分「真实错误 vs 用户取消/安全阻止」，自恢复提示只对真实错误注入。

### 🚀 增强

- **`bash` 持久 shell 会话**：新增 `session_id` 参数，复用同一 shell 进程，`export`/`alias`/`cd` 跨命令生效；唯一 GUID marker 界定输出边界 + 回读退出码；进程崩溃/超时自动重建，空闲 5 分钟自动回收（沙箱模式不支持）
- **环境变量清理**：`EnvScrubber` 在子进程启动前移除 KEY/PASSWORD/SECRET/TOKEN 形状及 `WAYCODER_*` 环境变量，防止密钥经 `env`/输出泄漏（对标 harness `scrubbedParentEnv`）
- **进程树终止对齐**：`HooksManager`/`LspTool`/`LintTool`/`McpClient`/`Agent` 自动测试等全部子进程 `Kill()` 统一改为 `Kill(entireProcessTree: true)`，父进程被杀时子进程一并终止
- **`RetryPolicy` 对称 jitter**：新增 `JitterRatio`（默认 0.1），重试延迟在 ±10% 内随机抖动；`ComputeJitteredDelay` 纯逻辑可自测（对标 harness jitterRatio）
- **工具调用并行调度**：`ITool.ExecutionMode`（Parallel/Exclusive）+ `ToolCallScheduler.Partition` 把一轮工具调用切分为「并行批 + 独占批」，批内有界并发（`MaxParallelism=4`）、批间串行，结果按模型声明顺序回填；bash/write_file/edit_file/agent/lsp/rm 等 14 个有副作用工具标注为 Exclusive
- **统一工具错误格式**：`ToolResultClassifier` 统一识别「错误/Error/❌/失败/运行命令时出错」等真实错误前缀，与「用户取消/Hook 阻止/沙箱阻止/危险命令阻止」等中止类区分——只有真实错误才注入「修正参数后重试」自恢复提示

### 🧪 自测

- 新增持久 shell 命令包装/cwd/env/退出码、环境变量敏感名判定与清理、进程树终止（父杀子随）、jitter 上下限/禁用、调度器分批、14 个工具 ExecutionMode 标注、错误分类器 17 项等测试
- 总计 **2692** 项自测全部通过（0 失败）

## v0.57.0 (2026-08-15) — 工具完善：新增 sqlite/test 工具 + 6 个工具增强 + LSP 会话缓存

响应「所有工具还有什么欠缺」的系统性排查，补齐工具短板：新增 `sqlite` 查询、`test` 测试运行两个工具（内置工具数 44→46）；`fetch` 支持 HTTP 方法/headers/body、`web_search` 增加 Bing 备用引擎与节流、`read_file` 支持 tail/二进制识别/JSON/INI 结构化、`write_file` 支持 append 与编码、网络工具接入 `RetryPolicy`；并给 `lsp` 加会话缓存复用，避免每次导航都重启服务器 + 重新初始化。

### ✨ 新增

- **`sqlite` 工具**（第 45 个）：通过系统 `sqlite3` 命令行执行 SQL（SELECT/INSERT/UPDATE/DELETE），`-header -column` 列式输出；零依赖、跨平台、AOT 安全，未安装时给出 macOS/Linux/Windows 各平台安装提示
- **`test` 工具**（第 46 个）：封装「跑测试 → 统计通过/失败 → 定位失败用例」闭环，支持 dotnet test / pytest / npm test / cargo test / go test 等，自动解析 pass/fail 计数并提取 FAILED/Error 失败用例行

### 🚀 增强

- **`fetch`**：支持 HTTP 方法（GET/POST/PUT/DELETE/PATCH/HEAD/OPTIONS）、自定义 headers、请求 body；接入 `RetryPolicy` 网络重试
- **`web_search`**：DuckDuckGo 失败自动回退 Bing 解析；2 秒最小间隔节流防封
- **`read_file`**：新增 `tail` 读取末尾 N 行；二进制内容识别（NUL 字节 + 严格 UTF-8 校验）；`.json` 美化输出、`.ini/.cfg/.conf` 结构化解析
- **`write_file`**：新增 `append` 追加模式；`encoding` 参数（utf8/utf8bom/ascii/utf16/utf16be/utf32，AOT 内置编码）
- **`download`**：接入 `RetryPolicy` 网络重试
- **`lsp`**：会话缓存复用——按（项目根, 命令）缓存 LSP 服务器进程，空闲 5 分钟自动回收、进程崩溃自动重建；顺带修复 didOpen 通知误读响应导致的 10 秒阻塞

### 🧪 自测

- 新增 fetch 方法/headers 解析、sqlite 查询、test 结果解析（pytest/dotnet 格式）、lsp 项目根查找与会话清理等测试
- 工具数量断言 44 → 46
- 总计 **2638** 项自测全部通过（0 失败）

## v0.56.0 (2026-08-15) — TUI 控件库统一 + 编辑器增强 + 对话框布局修复

响应「编辑器输入/刷新闪烁」「表格控件」「对话框标题栏错位」等一批 TUI 体验问题，新增 `TuiTableList` 表格列表控件并把 8 个界面迁移到统一控件库；编辑器补齐正则查找/替换、括号匹配、鼠标支持、状态栏行列信息，`TuiRichEditor` 改为逐行脏渲染消除整屏闪烁；修复确认框/权限框等 TitleBold 对话框标题栏覆盖上边框的错行 bug，并对全部 13 种对话框 + 行内权限控件（InlinePermission）建立端到端渲染自测。

### ✨ 新增

- **`TuiTableList` 控件**：表格列表（列头/列分隔线/选中高亮/滚动钳制/`ActivateSelected` 回调），8 个界面迁移到统一控件库
- **编辑器能力补齐**：
  - 正则查找/替换（捕获组 + 整词匹配 + 大小写开关）
  - 括号匹配 + 光标处词搜索
  - 鼠标点击定位光标 + 滚轮滚动
  - 状态栏显示行列/总行数/字节数
  - Tab 缩进可配置（默认 `\t`，设置里选 tab/space）
  - 退出未保存时弹保存确认对话框
- **`docs/TUI设计规范.md`**：设计令牌 + 规范文档 + 校验闸门

### 🐛 修复

- **对话框标题栏错行**：`TuiScreen` 两处 TitleBold 分支误用 1 基 `CursorPos`，导致标题覆盖上边框、与边框挤同一行；改为 0 基 `CursorPos0` 后标题独立成行、上边框为纯净渐变线（确认框/权限框等所有带标题对话框受益）
- **编辑器标点输入**：中文/全角标点无法正常输入的解析问题修复
- **`TuiRichEditor` 逐行脏渲染**：输入时只刷新光标所在行，未变化行不再整屏闪烁；滚动时全量刷新

### 🛠 重构

- **`Test/` 目录重组**：SelfTest 12 partial + Benchmark/Keypad/TuiAudit/TuiDemo 归入 `WayCoder/Test/`
- **对话框体系收敛**：移除 `DialogOverlay`/`DialogAction`，并入统一对话框管线

### 🧪 自测

- 新增全部 13 个 `TuiDialog` 工厂方法（Info/Success/Warn/Error/Confirm/Confirm3/Input/InputLine/Secret/FindReplace/Select/MultiSelect/Permission）布局 + 渲染断言（宽高 ≤ 屏 3/4、TitleBold 标记、标题独立成行）
- 新增 `InlinePermission` 行内权限控件测试（3 行黄色块渲染、危险命令忽略 A、D 展开、Y/N/A 三种结果）
- 总计 **2622** 项自测全部通过（0 失败）

## v0.55.0 (2026-08-15) — 绘图引擎增强 + 图片编解码 + JNode 手搓 JSON 迁移

响应「丰富画图功能」与「图片互转/贴图/裁剪/应用图标」需求，绘图引擎从 10 条指令扩展到 20+ 条（变换/新形状/描边/渐变/贴图/裁剪/图标模板/抗锯齿），并手搓 PNG/JPG/BMP 编解码，新增第 44 个内置工具 `convert_image` 实现格式互转。同时完成 JNode 手搓 JSON 全量迁移，彻底告别 `System.Text.Json` 反射。

### ✨ 新增

**图片编解码（零依赖、零反射、AOT 安全、跨平台）**

- **`Infra/RasterImage.cs`** — 统一像素缓冲（RGBA + 宽高 + 单像素读写/采样）
- **`Infra/BmpCodec.cs`** — BMP 编解码；**`Infra/JpegCodec.cs`** — JPEG 基线编解码
- **`Infra/PngDecoder.cs`** — 手搓 PNG 解码（灰度/索引/RGB/RGBA + 5 种行滤波）
- **`Infra/ImageLoader.cs`** — 魔数格式检测 + 编解码分发 + 按扩展名转格式
- **`Tools/ImageConvertTool.cs`** — `convert_image` 工具（第 44 个内置工具）：PNG/JPG/BMP 互转

**绘图引擎增强**

- **变换**：`translate/rotate/scale/push/pop`（`Affine` 仿射矩阵，SVG `<g transform>` + PNG 逆变换填充）
- **5 个新形状**：`star`（n 尖星）/`regular`（正 n 边形）/`ring`（圆环）/`pie`（扇形）/`heart`（心形）
- **描边**：所有填充形状支持 `stroke` 轮廓 + 线宽 + 线头 butt/round/square
- **渐变**：`gradient` 定义 + 线性/径向渐变填充（`@id` 引用，SVG `<defs>` + PNG 参数插值）
- **贴图**：`image x y w h "路径"` 把 PNG/JPG/BMP 图片贴入画布
- **裁剪**：`crop sx sy sw sh` 裁源图子矩形、`round r` 目标圆角、`rect` 直角矩形
- **图标模板**：`icon mac|ios|android|windows [颜色] [字形]` 一键生成应用图标（预设尺寸/圆角/安全区）
- **字体 + 抗锯齿**：`TrueTypeFont` 手搓 TrueType 解析 + `FontFinder` 系统字体探测（跨平台）；`antialias` 线条/字体消除锯齿

### 🛠 重构

- **JNode 手搓 JSON 迁移**：43 个工具的 `Parameters`/`Schema` 及非工具文件（Agent/Config/Infra/Memory/Batch）从 `System.Text.Json.Nodes` 全量迁移到手搓 `JNode`，移除残留 `JsonNode`/`JsonSerializer` 反射（承接 v0.53.10/11，实现「完全禁止反射」）

### 🧪 自测

- 新增图片/绘图断言（Image.Loader/Convert/Paste/Crop + Draw.Icon 等）
- 工具数量 43 → 44，总计 **2343** 项自测全部通过（0 失败）

## v0.54.0 (2026-08-15) — 新增 draw 绘图工具（文本 DSL → SVG/PNG）

响应「是否需要加个绘图工具」需求，新增第 43 个内置工具 `draw`：用文本指令画图，支持主流格式（SVG 矢量 + PNG 位图）双输出，指令可经编译期插件系统扩展。全程手搓零依赖、零反射、AOT 安全、跨平台。

### ✨ 新增

- **`Tools/DrawTool.cs` — draw 工具**：`code`（绘图指令文本）+ `format`（svg/png）+ `output`（文件路径），SVG 缺省返回内容、PNG 写文件返回路径
- **`Infra/DrawEngine.cs` — 绘图引擎核心**：`ColorUtil`（#rgb/#rrggbb/#rrggbbaa + 20 命名色）、`DrawTokenizer`（引号/逗号分词）、`DrawFigure` 图元、`IDrawCommand` 指令接口 + `DrawCommandRegistry` 注册表、`DrawDocument`/`DrawRunner`（DSL 解析 + SVG/PNG 编排）
- **`Infra/DrawCanvas.cs` — 光栅化器**：`Canvas` 像素画布（Bresenham 线 + 中点圆 + 扫描线 even-odd 多边形填充 + 圆角矩形/椭圆）+ 内置 5×7 点阵字体（ASCII 32–126，非 ASCII 实心块占位）
- **`Infra/DrawCommands.cs` — 10 条内置指令**：`rect`/`roundrect`/`circle`/`ellipse`/`line`/`arrow`/`polygon`/`polyline`/`path`/`text`（另有 `canvas` 画布），经 `[ModuleInitializer]` 自动注册
- **`Infra/PngEncoder.cs` — 手搓 PNG 编码器**：RGBA → PNG（ZLibStream DEFLATE + CRC32 + chunk 布局），对标 ScreenshotTool 的手写 PNG 解析

### 🔌 扩展

- 指令可扩展：实现 `IDrawCommand` 并 `DrawCommandRegistry.Register`，插件 `[ModuleInitializer]` 里注册即可贡献自定义绘图指令（满足「自己增加指令」）

### 🧪 自测

- 新增 `SelfTest.Chunk10.cs`（50 项）：ColorUtil 解析往返、分词器、注册表、DSL 解析（含错误分支）、SVG 标签、PNG 签名/IHDR 尺寸/IEND、光栅化像素、工具端到端
- 工具数量 42 → 43，总计 **2228** 项自测全部通过（0 失败）

## v0.53.11 (2026-08-15) — 彻底移除 JsonSerializer 反射（AgentSlotConfig / FetchTool）

承接 v0.53.10 的手搓 JSON/XML 库，把代码库中**最后一处**反射型 `JsonSerializer.Deserialize<T>`/`Serialize<T>` 全部替换为手搓 `JNode`，实现「完全禁止反射」——AOT 下不再有任何 `JsonSerializerIsReflectionDisabled` 隐患。

### 🛠 重构

- **`Config/AgentSlotConfig.cs`**：`Load`/`Save` 改用 `Json.Parse` + 新增 `SlotFromNode`/`SlotToNode` 手搓映射（键名保持 PascalCase，兼容历史 `agent_slots.json`），移除 4 处 `JsonSerializer.Deserialize<SlotConfig>`/`Serialize<SlotConfig>` 反射调用
- **`Tools/FetchTool.cs`**：`PrettyPrintJson` 改用 `Json.Parse` + `Json.Serialize(indent: true)`，移除 `JsonDocument`+`JsonSerializer` 反射路径

### ✅ 核查

- **`Tools/ScreenshotTool.cs`** 确认为跨平台（Windows PowerShell / macOS screencapture / Linux grim→import→scrot→maim 回退）+ 零反射（PNG 尺寸手搓解析 IHDR、OCR 走外部 tesseract），无需改动

### 🧪 自测

- 新增 10 项断言：`SlotConfig` 手搓往返（PascalCase 键名、字段往返、null 字段、`UseGlobal` 缺省为 true）、嵌套缩进美化
- 总计 **2178** 项自测全部通过（0 失败）

## v0.53.10 (2026-08-14) — 手搓 AOT 安全 JSON/XML 库

响应「不使用反射、自己可控」需求，新增两个零依赖、零反射的手写序列化库，替代散落各处的 `JsonNode.Parse` 手写解析与 `System.Text.Json` 反射序列化。

### ✨ 新增

- **`Infra/JsonLib.cs` — 手搓 JSON 库**（约 400 行）：
  - `JNode` DOM（Object/Array/String/Number/Bool/Null）+ 工厂/增删查/取值/深拷贝
  - `Json.Parse`/`TryParse` 递归下降解析器（手写 tokenizer），支持全部转义（含 `\uXXXX` 代理对）、严格数字语法、错误定位
  - `Json.Serialize`（紧凑 + 缩进两模式）+ `SerializeValue`（无反射，对齐 JsonHelper）
  - 数字往返保真（保留原始文本），非有限值安全回退 `null`
  - 类名 `JNode`/`JKind`/`Json` 避开 `global using System.Text.Json.Nodes` 冲突
- **`Infra/XmlLib.cs` — 手搓 XML 库**（约 400 行）：
  - `XNode` DOM（Element/Text）+ 属性保序 + 子节点/查询/InnerText
  - `Xml.Parse`/`TryParse` 手写解析器：声明/注释/DOCTYPE/CDATA/处理指令跳过、单双引号属性、预定义实体 + 数字字符引用、自闭合标签、标签匹配校验
  - `Xml.Serialize`（紧凑 + 缩进）+ 文本/属性转义

### 🧪 自测

- 新增 `TestJsonLib`（36 项）+ `TestXmlLib`（24 项）共 60 项断言：标量/对象/数组/嵌套解析、转义与代理对、非法输入拒绝、序列化往返、DOM 操作、实体/CDATA、错误分支
- 总计 2127 项自测全部通过（0 失败）

## v0.53.9 (2026-08-14) — 修复 HooksManager AOT 反射 bug

修复一个 NativeAOT 下的真实缺陷：`HooksManager.ParseHookOutput` 与 `LoadMatchers` 使用 `JsonSerializer.Deserialize<T>` 反射序列化，在 `PublishAot=true` 下会抛 `JsonSerializerIsReflectionDisabled`，导致 hook JSON 输出协议与 `hooks.json` matcher 系统在 AOT 发布版中完全失效。

### 🐛 修复

- **`ParseHookOutput`**：改用 `JsonNode.Parse` + `GetJsonString`/`GetJsonBool` 手写提取（AOT 安全），支持 `continue`/`decision`/`reason`/`systemMessage`/`additionalContext` 字段
- **`LoadMatchers`**：改用 `JsonNode.Parse` 手写解析 `matchers` 数组（matcher/events/hooks），移除 `HookMatchersWrapper` 反射类与 `using System.Text.Json`

### 🧪 自测

- 新增 `TestHooksManager`（20 项）：会话 hook 注册/注销/清空、`MatchesPattern` 通配匹配、`SnakeCase` 转换、`ParseHookOutput` 纯文本/JSON/exitCode 2 分支
- 其中 JSON 解析断言此前因反射异常而失败，现随修复通过
- 总计 2067 项自测全部通过（0 失败）

## v0.53.8 (2026-08-14) — 文件锁/文件追踪/Prompt 缓存单元测试补齐

继续代码质量维度：补齐三个核心安全/成本特性的零覆盖纯逻辑类的单元测试（39 项），均为 CLAUDE.md 强调的关键设计。

### 🧪 自测

- 新增 39 项断言覆盖三个类：
  - **`FileLockManager`**（14 项）：首次获取、同 agent 续期、异 agent 拒绝、`IsLockedByOther` 判定、Release 归属校验、过期锁强占（负 timeout 立即过期）、`ReleaseAll` 清空、`GetSummary` 空/非空、`WaitForLockAsync` 无锁成功
  - **`FileTracker`**（12 项）：未追踪/已追踪状态、外部修改 stale 检测（哈希变更）、`CheckForChanges` 检出、`RecordWrite` 更新、删除检测并移除追踪、`ValidatePreEdit` 未读取警告/已读取通过、`GetChangeWarning`、`Enabled` 短路、`Reset` 清空
  - **`PromptCache`**（13 项）：首次未命中、相同请求命中、节省 token 累计、system/tools 任一变更未命中、`HitRate` 计算、`Reset` 后未命中、`Enabled` 短路、`Summary` 命中率与 K 格式

- 总计 2047 项自测全部通过（0 失败）

## v0.53.7 (2026-08-14) — 长方法拆分 + ImportHelper 纯逻辑 + 压力测试脚本修复

代码质量维度继续推进：消除两处重复样板（`AgentTool` 并行解析三分支、`ContextManager` 三层压缩进度报告），将 `ImportHelper` 两个私有纯函数改为 `internal` 并补齐单元测试，同时修复压力测试脚本的跨平台与路径 bug。

### 🛠 重构

- **`AgentTool.ExecuteParallelAsync` 三分支合并**：`JsonArray` / `IEnumerable` / string-JSON 三处重复的「`ExtractTaskText` + 非空判断 + `Add`」样板提取为 `CollectTaskTexts` 单方法，消除重复
- **`ContextManager.MaybeCompressAsync` 进度报告合并**：三层压缩（裁剪/摘要/硬折叠）共 6 处重复的「百分比 + 进度条 + 事件」样板提取为 `ReportProgress`，行为不变
- **`ImportHelper` 纯逻辑暴露**：`StripJsonComments`（JSONC 注释剥离）与 `FormatSize`（文件大小格式化）由 `private` 改 `internal`，成为可测的零依赖纯函数

### 🧪 自测

- 新增 14 项断言覆盖 `ImportHelper`：
  - **`StripJsonComments`**（7 项）：行注释/块注释移除、注释后仍可解析、字符串内 `//` 与 `/* */` 不误删、字符串内转义引号保留
  - **`FormatSize`**（7 项）：B/KB/MB 三档 + 0/1023/1024/1MB 边界

- 总计 2008 项自测全部通过（0 失败）

### 🐛 修复

- **`scripts/stress-test.sh` 跨平台 + 路径 bug**：① AOT 产物名硬编码 `WayCoder.exe`（Windows），macOS/Linux 实为 `waycoder`，改为平台感知探测；② 编译验证 `cd "$WORK_DIR"` 后直接 `dotnet build`，但 Agent 在子目录（`MiniKanban/`）建项目导致 MSB1003，改为 `find` 定位 `.csproj`/`.sln` 所在目录后编译

### 🚀 端到端验证

- 压力测试通过：`deepseek-v4-flash` 生成 MiniKanban（9 文件 312 行），Agent 自测循环实测 `dotnet build` 0 错误 0 警告，CLI `add/list/move/delete` 全部通过，确认本轮重构无回归

## v0.53.6 (2026-08-14) — SnippetStore 可测试性重构 + 单元测试补齐

`SnippetStore` 此前 `Get`/`Search`/`List`/`Delete`/`Add` 硬编码 `DefaultDir`（`Environment.CurrentDirectory/.waycoder/snippets`），无法在隔离目录下测试——「不可测试 = 不可维护」的设计缺陷。

### 🛠 重构

- **`SnippetStore` 五个方法增加可选 `dir` 参数**：`Add`/`Search`/`List`/`Delete`/`Get` 均新增 `string? dir = null`（默认行为不变，向后兼容），内部 `EnsureLoaded(dir)` + 文件操作统一走 `dir ?? DefaultDir`，使测试可用临时目录隔离

### 🧪 自测

- 新增 9 项断言覆盖 `SnippetStore`：frontmatter 解析（name/language/tags/body）、`Add`→`Get` 往返、`Search` 多词 OR 按名称/标签命中、无命中返回空、`List` 全量、`Delete` 命中/未命中

- 总计 1994 项自测全部通过（0 失败）

## v0.53.5 (2026-08-14) — 工具层单元测试补齐（find_replace/diff/tree）

从基础设施类转向工具层：补齐三个纯 C# 实现工具的单元测试（16 项），覆盖编辑/对比/目录树核心行为。

### 🧪 自测

- 新增 16 项断言覆盖三个工具：
  - **`FindReplaceTool`**（6 项）：空 pattern 报错、预览模式不写文件、实际替换写入、无效正则回退纯文本匹配、目录不存在报错
  - **`DiffTool`**（5 项）：差异行 `-`/`+` 输出、相同文件提示、空文件提示、文件不存在错误
  - **`TreeTool`**（5 项）：树生成含子目录/文件、隐藏文件跳过、深度限制不展开、目录不存在错误

- 总计 1985 项自测全部通过（0 失败）

## v0.53.4 (2026-08-14) — 文件忽略规则 + 记忆检索补齐单元测试

继续代码质量维度改进：再补齐两个零覆盖的基础设施类 `FileIgnoreManager` 与 `MemoryRetrieval` 的单元测试（27 项）。

### 🧪 自测

- 新增 27 项断言覆盖两个类：
  - **`FileIgnoreManager`**（20 项）：`node_modules`/`dist`/`.git` 等始终忽略目录、`.pyc`/`.dll`/`.jpg` 等扩展名、`.gitignore` 规则匹配（`*.log` 任意深度、`!` 否定反转、`/rootfile.txt` 锚定、`*.tmp`）、`FilterIgnored` 批量过滤、`ShouldSkipDirectory` 隐藏/忽略目录跳过
  - **`MemoryRetrieval`**（7 项）：frontmatter 记忆加载 + `GetRelevant` 关键词匹配（英文标识符 + CJK 双字词）、无关关键词不误命中、`FormatForPrompt` 标题/类型/描述超 200 字符截断/空列表返回空

- 总计 1969 项自测全部通过（0 失败）

## v0.53.3 (2026-08-14) — 基础设施纯逻辑类补齐细粒度单元测试

代码质量维度（68/100）改进：此前三个零覆盖的基础设施纯逻辑类（`RetryPolicy` / `LruCache` / `IdGenerator`）从未被自测触达，属于「写了但没人验证」的死角。

### 🧪 自测

- 新增 `[基础设施]` 测试段，42 项断言覆盖三个类：
  - **`RetryPolicy`**（12 项）：黑名单/白名单异常过滤、首次成功不重试、失败 N 次后成功、耗尽重试、指数退避 100→200→400、无返回值版本
  - **`LruCache`**（16 项）：基本读写、容量淘汰最旧、`Get` 提升 LRU、TTL 过期、`Remove`/`Clear`/`OnEvicted` 事件、`TryGet`、命中/未命中/淘汰统计、容量 ≤0 校验
  - **`IdGenerator`**（14 项）：`NewId` 长度与去歧义字符集、100 个唯一性、`NewSlug` 格式与词数 clamp、`NewPrefixed` 前缀

### 🐛 修复

- **`RetryPolicy` 死代码**：重试循环末尾的 `throw new AggregateException` 实为不可达死代码——`catch (Exception ex) when (...)` 过滤器在最后一次尝试（`attempt == MaxRetries`）已原样放行异常向外抛出，`lastEx` 变量与 `AggregateException` 从未执行，且误导读者「耗尽重试抛 AggregateException」（实际抛最后一次原始异常，与文档一致）。移除死代码，替换为明确的「不可达终点」哨兵，行为不变

- 总计 1942 项自测全部通过（0 失败）

## v0.53.2 (2026-08-14) — 修复上下文压缩误触发（累计用量 → 最近 prompt）

端到端验证暴露的第二个缺陷：主智能体累计用量到 169 万 tokens、触发 5 次「上下文压缩」，但消息估算却只有 8 万左右——压缩根本没发生，只是在空转刷屏。

### 🐛 修复

- **压缩判断度量错误**：`ContextManager.ShouldStopAndSummarize()` 此前用 `CumulativePromptTokens + CumulativeCompletionTokens`（会话累计用量，单调递增）判断「剩余窗口是否不足」，导致上下文远未满时（真实上下文 ~12 万 vs 窗口 1M）就误触发压缩；而压缩层（`MaybeCompressAsync`）用消息估算（~8 万，远低于 50% 阈值）判断，三层压缩全部不触发，累计值也不重置，形成「每轮误触发 → 实际不压缩 → 累计继续涨 → 再误触发」的死循环
- **新增 `ContextManager.LastPromptTokens`**：`AddUsage` 里覆盖记录最近一次真实 prompt（代表当前上下文大小），`ShouldStopAndSummarize` 改用其判断——只有真实上下文真正接近窗口（剩余 ≤ buffer）才触发压缩，触发后压缩层用校准估算（≈ 真实 prompt）判断会真正执行裁剪/摘要，`ResetUsage` 同步重置

### 🧪 自测

- 新增断言 9 项（`LastPromptTokens` 记录最近一次非累加、累计超窗口但最近 prompt 小不触发、最近 prompt 接近窗口触发、大小窗口阈值、`ResetUsage` 重置），总计 1900 项全部通过（0 失败）

## v0.53.1 (2026-08-14) — 并行子智能体 tasks 数组对象元素乱码修复

端到端验证暴露的缺陷：`agent(tasks=[{"description": "..."}, ...])` 结构化传参时，对象元素被解析成 `Dictionary<string, object?>` 后直接 `ToString()`，子智能体收到 `System.Collections.Generic.Dictionary...` 乱码，3 个子智能体 2 个直接失败（只有纯字符串元素碰巧成功）。

### 🐛 修复

- **tasks 数组对象元素提取**：新增 `AgentTool.ExtractTaskText()`，对元素为对象（`JsonObject` / `Dictionary<string, object?>`）时提取 `description`/`task`/`name`/`title`/`text`/`prompt`/`instruction` 字段（对齐 schema 的 items 结构），纯字符串透传，无已知字段时兜底取第一个字符串值，杜绝类型名乱码

### 🧪 自测

- 新增断言 5 项（纯字符串透传、对象提取 description/task、JsonObject 提取、null 返回），总计 1891 项全部通过（0 失败）

## v0.53.0 (2026-08-14) — 子智能体健壮性加固（修复并行竞态 + 上下文爆炸防线）

压力测试暴露的两个「短板」从代码层根治：子智能体并行时的共享可变状态竞态、多路输出累加撑爆主智能体上下文。

### 🐛 修复

- **并行子智能体 ModelOverride 竞态**：子智能体改用独立 LLM 实例（`LLM.Clone()`），不再共享父 `LlmClient` 的小模型切换。此前并行模式下最后完成的子智能体会把 `ModelOverride` 恢复成小模型，污染主智能体后续请求降级
- **BashGuard 参数拦截语义 bug**：纯子命令禁止（如 `dotnet new`、`cargo install`）此前因 `MatchArgs` 无兜底而漏拦；`exceptFlags` 白名单（如 `pip install --user`）此前未命中时也放行。重写 `Match`/`MatchArgs`，白名单（exceptFlags=默认拦）与黑名单（flags/blockArgs=默认放）语义分离

### 🛡️ 健壮性

- 新增 `SubAgentParallelTotalMaxChars` 配置（`WAYCODER_SUBAGENT_PARALLEL_TOTAL_MAX_CHARS`，默认 15000）：并行子智能体聚合结果总限长，防止 N 个输出累加撑爆主智能体上下文（压力测试第五轮 8 路并行 3.8M tokens 的根因）
- 子智能体输出截断改「保尾」（头 70% + 尾 25%），保留末尾结论（如「Automata 7→0」），不再把关键结论截掉
- BashGuard 新增拦截 `dotnet new`（生成 csproj/Program.cs 污染多项目构建，压力测试第五轮 `MSB1011` 的根因）

### 🤖 自主性

- 新增 `SystemPrompt.SubAgentDiscipline`：子智能体纪律固化到每个子任务注入（不建 scratch/csproj、自测到通过、精简回报、不越界改模块），主智能体不必每次在 task 里重写外部铁律
- `LLM.MergeUsageFrom()`：子智能体 clone 实例的花费统计回收累加到父智能体，隔离不丢花费追踪

### 🧪 自测

- 新增断言 6 项（`dotnet new` 拦截、`dotnet build` 不误伤、纪律非空、`LLM.Clone` 独立、配置默认值等），并顺手修复 Snapshot 路径假设（兼容 cwd=仓库根或 `WayCoder/`），1879 项全部通过（0 失败）

## v0.52.0 (2026-08-14) — 子智能体 shell 权限（YOLO 放行 / 非 YOLO 提问确认）

### 🤖 多智能体

- 子智能体获得 `bash`（shell）权限：从「工具层禁令」转为「确认层管控」——移除 `SubAgentDeniedTools` 中的 `bash` 条目，shell 能力交给既有的 `PermissionManager.CheckAsync` 统一裁决
- **YOLO 模式**（`/perm yolo`）：子智能体 `bash` 直接放行，可跑 `dotnet build`/`dotnet run` 自测，不再「盲写」代码（修复压力测试中失败数从 7 暴涨到 129 的根因）
- **非 YOLO 模式**：子智能体 `bash` 属危险工具，逐条弹行内确认框「提问申请」；`ls`/`find`/`wc` 等只读命令仍由 `BashGuard.IsSafeReadOnly` 自动放行
- 新增 `PermissionManager.ConfirmLock`（`SemaphoreSlim`）串行化确认弹框：并行子智能体（`Task.WhenAll`）并发请求 shell 权限时逐个排队，消除抢键盘/渲染竞态
- 保留 `rm`/`kill`/`git` 等危险/管理类工具禁令（主智能体统一管理）

### 🔧 工具回显优化

- `Agent.FormatValue` 递归序列化集合/字典/JsonNode（而非 `ToString()` 泄漏 `System.Collections...`）
- `JsonHelper.SerializeValue` 改 `public` 供 FormatValue 复用
- `AgentTool.Description` 并发数硬编码「4 个」改为动态 `MaxParallelTasks`（配置调整后描述同步）

### 🧪 自测

- 自测数 1867→1872（上次 5 项回归）；`SubAgentDeniedTools` 断言反转（不再包含 `bash` / 子 Agent 深度 0 保留 `bash`），1872 项全部通过（0 失败）

## v0.52.0 (2026-08-14) — TUI 交互与渲染增强 + 反白配色统一 + 一键发布（master 并行线）

### 🎨 反白配色统一

- 统一「选中/光标行」反白惯例：菜单/列表选中行、单选组选中项、输入框光标行全部为「高亮底 + 黑字」，与终端反白（前景/背景互换）语义一致
- 修复 `TuiRadioGroup` 选中项前景色误用背景色代码（`SelFg=ControlFocusedBg` 经 `AnsiTty.FgCode(47)` 渲染成 256 色亮绿）的 bug：新增 `SelBg`，选中项整行「白底 + 黑字」
- `TuiTextArea` 新增 `CursorLineFg`（光标行黑字），光标移动/翻页/滚动时补 `MarkDirty`，修复光标行高亮残留

### 📝 Markdown 渲染增强

- 引用块 `>`：连续多行合并为 `MdBlockQuote`，左侧 `│` 竖线 + 缩进渲染
- 任务清单 `- [x]` / `- [ ]`：解析勾选状态，渲染 ☑（已完成·绿）/ ☐（未完成·弱化）
- 链接 `[文字](url)`：青色链接文字 + 弱化显示 URL；删除线 `~~text~~` 弱化显示

### ⌨️ 对话框 / 菜单 / 列表交互

- 所有 `TuiDialog` 对话框统一注册 `Esc` = 取消/关闭；单行输入框回车 = 确定；多选列表 Enter = 确认
- `TuiMenu`：`Space` 激活当前项；快捷键编号只计入非分隔线项（1-9 连续，分隔线不占编号）；菜单高度按终端内容区收拢；弹出菜单按调用点定位（不再居中）
- `TuiList` 单选 `Space` = 激活（等同回车）；`TuiButtonGroup` 支持上下键导航，按钮自行渲染（不依赖父 Children 树）

### 🔐 权限确认与问答统一

- `PermissionManager` 统一走 `UxHelper.Confirm`（TUI 黄底 Y/N/A 弹框 / 非 TUI 行内编号菜单），删除重复的 `ShowInlinePermission` 分支（注：本次合并保留 mac 线 `ShowInlinePermission` 行内权限块方案）
- `AskUserQuestionTool` 单选/多选/文本输入统一 `UxHelper.Select/MultiSelect/Ask`，删除约 125 行重复的 `ShowAndWait` 事件循环，并透传 `AskUserTimeoutSec` 超时

### 📎 工具消息缩进嵌套

- `tool` 消息缩进嵌套在所属 `assistant` 消息下（`indent=1`），`TuiListItem.Indent` 新增左缩进、续接无角色头；AgentSlot / Program / SessionCommand 全链路透传

### 🛠️ 工具增强

- 抓屏跨平台实现补全：Windows（PowerShell `CopyFromScreen`）/ macOS（`screencapture`）/ Linux（`grim`→`import`→`scrot`→`maim` 依次回退）
- 自动升级源切换：**Gitee 优先**（国内快）→ GitHub 回退；GitHub 仓库名更正为 `alecksty/waycoder`（配合商标更名，brew/winget/docs 同步更新）

### 🧪 TUI 测试审计工具

- `--keypad`：按键脚本回放驱动 TUI（KEY/TEXT/DELAY/SNAP/DIALOG/FOCUS/MSG/FILL），任意节点抓帧核对排版
- `--tui-audit`：对话框/控件渲染审计（输出纯文本帧，剥离 ANSI）

### 🎨 选择器统一外框 + 发布自动化

- 新增 `DialogFrame`：居中带边框外框（橙→黄渐变 + 暗化背景），统一 ModelPicker / FilePicker / SessionPicker / CommandPalette 外观
- 新增 `scripts/release.ps1` / `scripts/release.sh` 一键发布：编译 6 平台包 → 算 SHA256 → 生成 winget manifest → 更新 brew formula → apt 打包 → 打印提交命令

### 🧪 自测

- 新增反白配色 / Markdown 引用块·任务清单·链接 / 菜单快捷键·收拢 / 对话框 Esc / UxHelper 多选·确认等自测，1867 → 1909 项全部通过（0 失败）

## v0.51.0 (2026-08-14) — JSON 输出模式（--json，IDE / 脚本桥接，对标 Claude Code --output-format json）

### 🔌 IDE 桥接

- 新增 `--json`（`-j`）一次性输出模式：`waycoder --json -p "任务"`（或 `echo "任务" | waycoder --json`）静默执行 Agent，stdout 只输出一个结构化 JSON 对象，供 VS Code 扩展、CI 脚本、外部工具直接 `JsonNode.Parse` 解析——无需剥离 ANSI 动画/Spinner/权限块
- 结果字段：`schema`（版本 1.0）、`success`、`answer`（最终回答）、`error`（失败原因）、`model`、`usage{prompt_tokens/completion_tokens/total_tokens}`、`cost_usd`（模型无定价时为 null）、`duration_ms`、`changed_files[]`（本次会话修改文件清单，复用 `EditFileTool.ChangedFiles`）
- 退出码约定：0 = 成功，1 = 中断/超时/异常；与现有 `-p` 一次性模式共用同一条 Agent 执行链（`-y` 自动放行、非交互）
- `JsonResult.Build` 为纯函数构建器（输入原始值 → 输出 JsonObject），便于 AOT 自测，不依赖 `_llm`/`_agent` 静态态

### 🧪 自测

- 新增 JSON 模式自测 14 项（成功/失败结果、usage 汇总、cost null 兜底、空 changed_files、序列化可解析），1853→1867 项全部通过（0 失败）

## v0.50.0 (2026-08-14) — 编译期插件系统（IPlugin SDK，对标 Claude Code/Crush 插件扩展）

### 🔌 插件系统

- 新增 `IPlugin` 接口 + `Plugin` 抽象基类 + `PluginRegistry` 注册表——编译期插件（C# 源码随主程序一起 AOT 编译进单文件）可贡献两类扩展：
  - **工具**（`ITool`）：自动并入 `ToolRegistry.AllTools`，大模型直接通过 function calling 调用
  - **斜杠命令**（`ISlashCommand`）：自动并入 `SlashCommandRegistry`，REPL 输入 `/xxx` 触发
- 三步接入、零启动代码改动：`WayCoder/Plugins/` 目录新建 `.cs` → 继承 `Plugin` 覆写贡献项 → `[ModuleInitializer]` 自动注册（AOT 安全，无反射）
- 与已有扩展机制互补：SKILL.md（Markdown 技能）、Hooks（外部脚本）、MCP（外部服务器）之外，插件提供「原生性能、复杂逻辑、可复用工具/命令」的第四类扩展
- 注册表健壮性：同名插件（忽略大小写）覆盖不重复、`null` 注册/`null` 返回防御、按名卸载
- 完整文档见 [docs/插件系统.md](docs/插件系统.md)（含 `ITool` + `ISlashCommand` + `[ModuleInitializer]` 完整示例）

### 🧪 自测

- 新增插件系统自测 11 项（注册/收集/集成到 AllTools/同名覆盖/null 防御/卸载），1842→1853 项全部通过（0 失败）

## v0.49.0 (2026-08-14) — 批量任务引擎（多仓库并行 + worktree 隔离，对标 Cursor/Aider 多仓库批处理）

### 🚀 批量任务引擎

- 新增 `--batch` 一次性模式：多仓库并行处理，每个任务在独立克隆副本中隔离执行，跑完输出聚合报告（Markdown + 退出码），对标 Cursor 的批量修复、Aider 的多仓库脚本
- 两种用法：
  - `--batch <JSON文件|内联JSON>`：`{ "maxParallel": 4, "timeoutSec": 1800, "keepResults": false, "tasks": [{ "repo": "URL或本地路径", "task": "任务描述", "name"?, "branch"? }] }`
  - `--batch-repo <仓库> --batch-task "任务"`：快速给多个仓库跑同一个共享任务（`--batch-repo` 可重复）
- 隔离与安全：每个任务 `git clone` 到 `.waycoder/batch/jobs/<名>_<随机>` 独立副本，子进程以 `-p` 一次性模式 + `-y` 放行执行，进程级隔离 cwd 与状态；默认执行后清理副本，`--batch-keep` 保留
- 子进程复用父进程已解析的模型/BaseUrl/API Key/预算（`--model`/`--base-url`/`--api-key`/`--max-budget-usd` 显式传入），避免 clone 目录无 `.env` 丢失配置
- 并行度受控（`SemaphoreSlim`，1–16 可配，默认 4），单任务超时可配（默认 1800s），超时终止整个进程树（含 bash 子进程）
- 报告落盘 `.waycoder/batch/batch-report.md`：总计/成功/失败/总耗时 + 每个任务的摘要与错误详情

### 🧪 自测

- 新增批量任务引擎自测 26 项（JSON 解析/钳制/错误场景/名称消毒/远程判断/FromRepos/报告渲染/端到端克隆+并行+清理），1816→1842 项全部通过（0 失败）

## v0.48.9 (2026-08-14) — 多模态音频输入（transcribe 转录，对标 Codex CLI / Gemini CLI）

### 🎙️ 音频转录

- 新增 `transcribe` 工具：把本地音频文件上传到 Whisper 兼容端点（`/v1/audio/transcriptions`，multipart）转成文字，补齐多模态的「音频输入」短板——至此 WayCoder 同时支持图片（`view_image`）与音频（`transcribe`）两种多模态输入
- 支持 mp3/wav/m4a/flac/ogg/webm/aac/opus 等 19 种音频格式，自动映射 MIME 类型，25MB 大小上限
- 可选 `language`（ISO 语言代码）与 `prompt`（术语引导词）参数提高转录准确率
- 配置三件套（设置界面自动生成）：`WAYCODER_WHISPER_MODEL`（默认 whisper-1）、`WAYCODER_WHISPER_BASE_URL`（空=默认 api.openai.com）、`WAYCODER_WHISPER_API_KEY`（空=回退主 API Key）；支持 OpenAI Whisper / Groq / faster-whisper 任意兼容服务
- 归入 AutoModeClassifier「Safe」级（只读外部查询，自动放行，无需确认）

### 🧪 自测

- 新增 transcribe 自测 11 项（注册 + 格式支持 + MIME 映射 + 路径校验 + 配置默认值），内置工具 41→42，1816 项全部通过（0 失败）

## v0.48.8 (2026-08-14) — MCP 资源/提示词支持（对标 Claude Code resources/prompts）

### 🔌 MCP 能力补全

- 新增 **资源（resources）** 支持：`resources/list` 发现 + `resources/read` 读取，注册为 `mcp__<server>__resources` 工具——省略 `uri` 参数列出全部资源，传入 `uri` 读取指定资源内容（text/blob/嵌套资源统一格式化）
- 新增 **提示词模板（prompts）** 支持：`prompts/list` 发现 + `prompts/get` 调用，每个模板注册为 `mcp__<server>__prompt__<name>` 工具，参数从模板 `arguments` 数组自动生成 inputSchema
- 修复 MCP 发现结果解析 bug：`tools/list`/`resources/list`/`prompts/list` 的响应数据此前从响应顶层读取，实际位于 JSON-RPC `result` 字段下——导致工具发现一直为空（此前 `tools/call` 正确读 `result`、发现却读顶层，二者不一致）；本次统一改为 `result` 下读取
- 状态模型扩展：`McpServerState`/`McpServerInfo` 新增 `ResourceCount`/`PromptCount`，`/mcp` 命令与侧栏 MCP 区显示「N 工具 · M 资源 · K 提示词」

### 🧪 自测

- 新增 MCP 资源/提示词自测 15 项（资源工具名称/描述/参数/读取/列表 + 提示词工具名称/参数生成/调用 + `BuildParameters`/`ExtractContentText` 纯逻辑 + 状态计数），1804 项全部通过（0 失败）

## v0.48.7 (2026-08-14) — 内置自动升级 + winget/brew/apt 分发（对标 Claude Code `claude update`）

### ⬆️ 自动升级

- 新增 `UpdateChecker`：语义版本比较 + 当前平台 RID 探测 + release 资产匹配（纯逻辑与网络/文件操作分离，可确定性自测）
- 版本检查优先 **GitHub Releases**、失败回退 **Gitee Releases**（`WAYCODER_GITHUB_REPO` / `WAYCODER_GITEE_REPO` 环境变量可覆盖）
- 完整自替换：下载匹配平台的 `.tar.gz`/`.zip` → 解压单文件二进制 → 覆盖当前可执行文件；Windows 落 `.new` + `upgrade.bat` 重试脚本（退出后自动替换并重启），Unix 原子 `rename` 覆盖运行中二进制
- 极简 tar.gz 单文件解压（仅 `GZipStream` + `FileStream`，AOT 零风险，不引入 `System.Formats.Tar`）
- `/update` 命令（检查 + 更新日志详情）、`/update now`（自替换）、启动后台静默检查（有新版本才提示）
- `--update` CLI 一次性升级标志（幂等，已最新则提示后退出）

### 📦 分发渠道

- `packaging/winget/`：winget manifest（portable 类型，x64/arm64）
- `packaging/brew/`：Homebrew formula（osx-arm64 / osx-x64）
- `packaging/apt/`：`build-deb.sh` 打包脚本 + reprepro 仓库配置说明
- `.github/workflows/release.yml`：推送 `v*` 标签自动 NativeAOT 编译 4 平台并创建 Release + 上传资产
- 新增 `docs/安装与升级.md` 完整安装/升级/发布指南

### 🧪 自测

- 新增 `TestUpdateChecker` 14 项（版本比较 7 + RID 探测 2 + 资产名/URL 匹配 5），1789 项全部通过（0 失败）

## v0.48.6 (2026-08-14) — 工作模式下沉到 Agent 实例（修复混合模式并行污染）

### 🔧 实例级工作模式

- `Agent.WorkMode` 实例字段替代全局 `WorkModeManager.CurrentMode`（全局仅作 UI 镜像），每个槽位 Agent 持有自己的模式
- 修复混合模式并行污染：此前后台槽位会读到活跃槽位的模式（如 A 槽 Plan + B 槽 Build 并行时 B 槽被误判为 Plan 而阻止写文件）
- `Agent.OnWorkModeChanged` 回调携带槽位索引——后台槽位批准计划后自动切回 Build 只通知正确槽位，不再经全局 `ModeChanged` 事件污染活跃槽位
- Agent 主循环三处读取（模式提示 / 计划审批门 / 工具约束检查）+ 计划批准后切回 Build 全部改为读写实例字段
- `Program.cs` 新增 `WireSlotWorkMode` 绑定槽位时灌入模式 + 接线回调；Shift+Tab、`/mode` 命令同步更新活跃槽位 Agent 实例模式

### 🧪 自测

- 新增 `TestWorkModePerAgent` 10 项（默认 Build + 双实例独立 + 实例不影响全局 + 工具约束跟随实例 + 回调 + 审批门纯逻辑），1775 项全部通过（0 失败）

## v0.48.5 (2026-08-14) — 多会话真并行执行（对标 Claude Code 多窗口）

### 🧵 多槽位后台并行执行

- F1-F10 槽位从「切换」升级为「并行」：Agent 任务在后台线程运行，主循环不再阻塞，运行中可自由切换槽位查看/投递其他任务
- 输出按槽位路由：活跃槽位实时流式写屏（复用 ChatScreen 流式方法），非活跃槽位缓冲到槽位自身 `ChatMessages`，切换回时由 `RestoreTo` 完整展示
- `AgentSlot` 新增线程安全缓冲输出（`BufferedStartStream`/`BufferedAppendToken`/`BufferedFinishStream`/`BufferedAppendToLast`/`BufferedAddMsg`）+ 运行状态（`IsBusy`/`Cts`/`Sync` 互斥锁）
- 切换与输出路由共享槽位 `Sync` 锁，原子完成「检查活跃 + 写入」与「快照 + 改活跃槽位」，杜绝切换瞬间丢 token
- Esc 中断当前活跃槽位 Agent / Ctrl+Z 优雅暂停（空闲时正常下发），Ctrl+Q 仍为全量紧急退出
- 退出/崩溃自动保存全部非空槽位会话（`_auto` / `_auto_slotN`），后台任务的进度不丢
- 槽位内已有任务运行时拒绝重复投递，避免同槽并发冲突

### 🧪 自测

- 新增 `TestMultiSlotParallel` 12 项（槽位运行状态 + 缓冲流式输出 + 自动新建流式消息 + 追加），1765 项全部通过（0 失败）

## v0.48.4 (2026-08-14) — LSP 语言扩充 5→14

### 🔍 LSP 语言服务器扩充

- `LspTool` 从 5 种语言扩充到 **14 种**，对标 Cursor/Claude Code 的覆盖
- 新增 9 种：C/C++（`clangd`）、Java（`jdtls`）、Kotlin（`kotlin-language-server`）、Ruby（`solargraph`）、PHP（`intelephense`）、Lua（`lua-language-server`）、Bash（`bash-language-server`）、Swift（`sourcekit-lsp`）、Zig（`zls`）
- `ExtToLang` 扩展映射补齐（.c/.cpp/.h/.java/.kt/.rb/.php/.lua/.sh/.swift/.zig 等）
- `GetLanguageId` 同步补齐 LSP 语言 ID（c/cpp/java/kotlin/ruby/php/lua/shellscript/swift/zig）

### 🧪 自测

- 新增 `[LSP]` 语言覆盖 10 项（14 种语言 + 9 个服务器命令断言），1753 项全部通过（0 失败）

## v0.48.3 (2026-08-14) — /mcp 管理面板 + MCP 状态模型（对标 Claude Code /mcp）

### 🔌 `/mcp` 管理面板

- 新增 `/mcp` 命令：列出所有 MCP 服务器（名称/传输/状态/工具数），`/mcp reload [name]` 重连（省略 name 重连全部）
- `McpManager` 引入结构化状态模型：`McpServerStatus`（Connecting/Connected/Failed）+ `McpServerInfo`（不可变快照）+ `McpServerState`（内部运行时状态）
- `ReloadAsync` 重连：断开旧连接 → 移除旧工具 → 重新解析配置 → 重连 → 更新状态
- `McpManager.Servers` 暴露排序后的服务器状态快照，侧栏面板（Ctrl+B）MCP 区改用结构化显示（状态图标 + transport + 工具数）
- 连接状态由拼凑字符串改为状态机：握手失败/无工具/连接失败均记录精确状态与错误信息

### 🧪 自测

- 新增 `[MCP 状态]` 区块 12 项（枚举值 + Info 快照 + ToInfo 映射 + Reload 非空），1743 项全部通过（0 失败）

## v0.48.2 (2026-08-14) — /init 项目初始化（对标 Claude Code /init）

### 🚀 `/init` 项目初始化

- 新增 `/init [force]` 命令：扫描项目（语言/框架/构建工具/Git）→ 生成中文 `CLAUDE.md` 指导文件（对标 Claude Code /init）
- `ProjectInitializer.GenerateClaudeMd()` 纯逻辑生成：项目概述 / 常用命令 / 架构 / 开发规范 / 注意事项 五区块
- 命令检测复用 `ProjectContext.DetectProject()` 结果，补充构建/测试/lint 命令精确探测（.NET / Node / Go / Rust / Python / Makefile）
- 已存在 `CLAUDE.md` 时弹确认框（覆盖/取消），`force` 参数跳过确认
- 生成后下次启动自动注入系统提示词（复用现有 `ProjectContext.LoadInstructions()`）

### 🧪 自测

- 新增 `TestProjectInit`：生成结构 6 项 + 命令检测 11 项，1731 项全部通过（0 失败）

## v0.48.1 (2026-08-14) — MCP SSE 传输 + 竞品资源复用指南

### 🔌 MCP 三传输补齐（stdio / HTTP / SSE）

- 新增 legacy HTTP+SSE 双端点传输（`SseMcpTransport`）：GET `/sse` 事件流 + POST `/message` 发请求
- `McpTransportType` 枚举 + `DetectTransport` 自动探测（`sse` / `http` / `stdio`），配置字段 `transport` 优先
- `SseMcpTransport` 后台 SSE 读循环解析 `event:`/`data:` 行，`endpoint` 事件解析消息端点、`message` 事件按 `id` 匹配 pending 请求
- `ResolveEndpointUrl` 相对→绝对 URL 解析，空白/非法 data 安全返回 null
- 自测新增 `[MCP SSE]` 区块 8 项（DetectTransport ×4 + ResolveEndpointUrl ×4），1714 项全部通过

### 📚 竞品资源复用指南

- 新增 `docs/竞品资源复用.md`：按「协议是否开放」给竞品（Claude Code / Crush / Cursor / Aider / Cline）的插件/MCP/LSP/Skill 分级
- MCP 完全共用、LSP server 生态完全共用、SKILL 高度共用（容错 frontmatter）、插件外壳不可共用但内部资源可拆
- 附 `.waycoder/mcp_servers.json` 三 transport 配置示例 + 复用优先级建议

## v0.48.0 (2026-08-14) — 计划审批门 + 抓屏/多模态视觉 + SelfTest 拆分

### 🧠 计划审批门（对标 Claude Code Plan Mode）

- `计划` 模式（Shift+Tab）下模型产出计划后不再自动催促执行，而是就地弹出审批框
- 批准 → 自动切回 `建造` 模式继续执行；拒绝 → 停止并返回计划
- `Agent.ShouldPromptPlanApproval(mode, contentLen)` 纯逻辑判定 + `ChatScreen.ShowPlanApproval` 审批对话框（Y/N 快捷键）
- `WorkModeManager.ModeChanged` 统一同步槽位持久模式与状态栏，修复批准后状态栏仍显示"计划"的错位
- 非 TUI 环境（一次性模式/管道/测试）自动批准，不阻塞

### 📸 抓屏工具（`screenshot`）

- 新增 `screenshot` 工具：终端文本抓屏（去除 ANSI）+ 桌面 PNG 抓屏（macOS `screencapture`）+ 区域抓屏
- 桌面截图可选 OCR（检测到 tesseract 时自动提取文本）

### 👁️ 多模态视觉（`view_image`）

- 新增 `view_image` 工具：把本地图片附加到下一轮请求，让支持 vision 的模型直接"看图"
- `LLM.ModelSupportsVision` 门控：仅 gpt-4o/gpt-5/claude/gemini 等 vision 模型注入，DeepSeek 等文本模型自动跳过避免 400
- 配合 `screenshot` 实现「抓屏 → 看图修 bug」闭环

### 🧪 SelfTest 拆分

- 5879 行单文件 `SelfTest.cs` 拆为 11 个 partial 文件（`SelfTest.cs` 核心 + `Chunk1-9` + `Helpers`），单文件最大 863 行

### 🧪 自测

- 1706 项自测全部通过（0 失败）

## v0.47.12 (2026-08-13) — /pause 优雅暂停 + 主循环稳健性修复 + Windows 打包脚本

### ⏸️ 优雅暂停（Ctrl+Z）

- 新增 **Ctrl+Z 优雅暂停**：Agent 运行时按 Ctrl+Z，当前批次完成后自动「git commit → 写检查点 → 存会话」再停机，与 Esc 立即中断互补
- 只提交 Agent 自己改过的文件（`AutoCommitAsync(fallbackToGitStatus: false)`），不卷入与本任务无关的未提交改动
- 新增 `/pause` 命令（提示用法）；会话存到 `_auto`，重启后 `/resume` 可恢复

### 🐛 LLM 参数解析健壮性

- `ParseArgs` 改用 `JsonDocument` 解析（替代 `JsonNode`），容忍重复 JSON 键（后者覆盖，不再抛 `ArgumentException`）

### 🔧 主循环稳健性修复

- 修复主循环中途停滞被误判为完成而提前退出
- 任务进行中无工具调用不再误判为完成
- 自动续跑 `wasWriting` 标记与实际工具输出对齐（`已写入 / 已编辑 / 已创建`）

### 📦 Windows 打包脚本

- 新增 `scripts/package.ps1`（与 `package.sh` 功能对等，Windows 原生 PowerShell 打包）

### 🧪 自测

- 1676 项自测全部通过（0 失败）

## v0.47.11 (2026-08-13) — 输入框完善：光标定位 + 复制粘贴 + 双输入对话框 + 补全钩子 + 渲染残影修复

### 🖱️ 光标/选区错位修复

- **根因**：`GetAbsoluteX/Y()` 沿 `Parent` 链累加，无法反映窗口内容区的 `ContentLeft/ContentTop` 偏移（`RootView` 不设 `Parent`），导致对话框内输入控件光标定位错位
- **修复**：`TuiControl.Render()` 记录渲染时的绝对原点 `_lastAbsX/_lastAbsY`（在裁剪早退前设置），三个输入控件（`TuiInput`/`TuiTextArea`/`TuiRichEditor`）的 `GotoCursorPos()` 改用该坐标，窗口内光标不再跑偏/消失

### 📋 双输入对话框 + 复制粘贴

- **新增 `TuiDialog.InputLine()`**：单行输入对话框（`TuiInput`），与已有的多行 `Input()`（`TuiTextArea`）、密码 `Secret()` 并列，均带输入历史（`TuiInputHistory`）与 OK/Cancel 按钮
- **复制粘贴快捷键**：`Ctrl+C` 被全局保留为退出，故复制改用 **Ctrl+Insert**、粘贴改用 **Shift+Insert**（Linux/Win 通用），`TuiEditBase` 统一分发

### 🎣 补全输入钩子（触发提示框）

- `ChatScreen.RegisterPrefixHint(prefix, provider)` / `UnregisterPrefixHint`：允许外部注册任意前缀符号的提示项生成器，`BuildPrefixHints` 优先走钩子，`IsKnownPrefix` 统一判定内置（`/ @ ! #`）与自定义前缀

### ✨ 渲染闪烁/残影修复

- **建议面板浮层化**：`TuiControl` 新增 `Floating` 属性，`TuiVBox`/`TuiHBox` 布局跳过浮动子控件（Flex/尺寸/位置都不计入）——Tab 补全/前缀提示面板不再把输入区挤出屏幕
- **脏区补绘**：`TuiScreen.MarkDirtyRect()` + `ChatScreen` 记录建议面板上一帧矩形，移动/缩放/隐藏时补绘被遮挡的聊天内容
- **底色擦除修复**：脏区擦除先 `SGR 复位` 再填空格，`bg=0` 的空格不再残留浮层底色（如建议面板的 Bg=47）

### 🧪 自测

- 1671 项自测全部通过（0 失败）

## v0.47.10 (2026-08-13) — 一键多平台打包脚本（`scripts/package.sh` / `scripts/package.ps1`）

### 📦 多平台打包

- 新增 `scripts/package.sh`（bash）与 `scripts/package.ps1`（PowerShell/Windows 原生）：一条命令打包 6 个平台（win-x64 / win-arm64 / linux-x64 / linux-arm64 / osx-x64 / osx-arm64）
- **当前平台走 NativeAOT**（零依赖原生单文件），**其他平台走非 AOT**（自包含单文件 JIT，跨平台交叉发布）
- Windows AOT 自动把 VS Installer 目录加入 PATH（定位 vswhere.exe / MSVC 链接器）
- 每次打包前清理 `obj/bin`，避免不同 RID 之间的还原状态污染（误报 Cross-OS）
- 产物统一为 `dist/waycoder-<版本>-<RID>.zip`（Windows）或 `.tar.gz`（Linux/macOS），排除 `.pdb` 调试符号
- 版本号自动从 `Global.cs` 提取；支持命令行指定平台（如 `./scripts/package.sh win-x64 linux-x64`）

## v0.47.9 (2026-08-13) — 一键清理失效供应商（`--model prune`）

### 🧹 剪除失效供应商（`--model prune` / `/model prune`，别名 `clean`）

- 一条指令自动清理：逐一测试所有已存 API key，分三种失效情形处理
  - **仅 key 无效（401/403）**：供应商真实可达 → 只删 key、**模型保留**
  - **无法连接（超时/拒绝/写错地址）**：供应商本身不可用 → 删 key + 该供应商下所有自定义模型
  - **无端点（供应商不存在/拼错供应商）**：删 key + 该供应商下所有自定义模型
- 内置供应商/模型不删（仅删 key）；本地端点（Ollama/LM Studio）不参与
- 输出逐项报告：✅ 保留 / 🗑️ 已删除，末尾给出「删除 N 个 key + M 个自定义模型」结论
- 复用连通性探测（401/403=密钥无效、超时/拒绝=无法连接），与 `--model test` 同一套判定

## v0.47.8 (2026-08-13) — 连通性测试覆盖全部已存 key + 补充供应商端点

### 🔑 全量 key 扫描

- `--model test` 由「只测有目录模型的服务商」升级为「逐一测试**所有已存 API key** + 所有本地端点」——目录内无模型的供应商（如 Gitee / Bailian / OpenCode / MiniMax / AIHubMix）也会被扫描
- 新增供应商端点注册表：`gitee`（ai.gitee.com/v1）、`bailian`（dashscope 百炼）、`opencode`（opencode.ai/zen/v1）、`minimax`（api.minimaxi.com/v1）、`aihubmix`（aihubmix.com/v1）
- 探测结果细化：区分「密钥无效（401/403）」「端点可达但无 /models 接口（可能非 OpenAI 兼容）」「无法连接（超时/拒绝）」，避免误报
- 报告分「API Key」与「本地端点」两节，末尾给出「N/M 个端点可连接」结论

### 🧪 自测 +5

- 新增供应商注册表端点断言（gitee/bailian/opencode/minimax/aihubmix）

## v0.47.7 (2026-08-13) — 模型连通性测试 + 手动增删模型/服务商/API key

### 🔌 模型连通性测试（`--model test` / `/model test`）

- 测试所有「有 key 的服务商」+「所有本地模型」（Ollama / LM Studio / localhost）能否连上
- 按端点（服务商 + base_url）分组探测 `GET /models`：401/403=密钥无效、404 回退 `/v1/models`、超时/拒绝=无法连接
- 输出报告：每个端点 ✅/❌ + 所属模型 + 最终「N/M 个端点可连接」结论
- 有效 base_url 解析：显式 > 服务商默认 > 本地默认 `localhost:11434`

### ➕➖ 手动增删（`--model add` / `--model remove`，`/model` 同理）

- `add model <id> <供应商ID> [baseUrl]`、`add provider <供应商ID> [baseUrl]`、`add key <供应商ID> <key>`
- `remove model <id>` / `remove provider <供应商ID>` / `remove key <供应商ID>`（`remove <id>` 向后兼容删模型）
- 均写入全局模型库 / key 库并持久化；内置模型/供应商不可删

### 🧪 自测 +8

- 手动添加模型/服务商、按服务商删除、删除 key、连通性端点分组

## v0.47.6 (2026-08-13) — 模型库外置化：内置兜底 + 多来源导入（OpenCode/OpenClaw/Crush/Claude Code/Codex）

### 📚 模型库外置化（内置兜底）

- 模型目录拆为「内置精选 + 自定义库」：`~/.waycoder/models.json`（全局）优先，项目 `.waycoder/models.json`（本地）覆盖，找不到外置库时内置目录兜底——开箱即用
- `ModelCatalog.All` = 内置 + 自定义合并（自定义按 Id 覆盖内置、新增追加）；`--model list`、`/model`、`/provider`、模型选择框全部切换为合并目录

### 📥 外部模型库导入（`--model import` / `/provider import`）

- 支持 `opencode`（`~/.config/opencode/opencode.json`/`.jsonc`）、`openclaw`（`~/.openclaw/openclaw.json`）、`crush`（`~/.config/crush/config.json`）、`claude`/`claudecode`（`~/.claude/settings.json` env 中 `*_MODEL` + `BASE_URL`）、`codex`（`~/.codex/config.toml` 的 `[model_providers.*]` + `[profiles.*]`），或任意 JSON/TOML 文件路径
- `import` 无参 / `all` = 自动探测全部来源；导入内容持久化到全局 `models.json`；内置已有自动跳过；同一 Id 去重

### 🧭 /provider 命令

- `/provider`（当前服务商概览）、`/provider list`（服务商列表 + 模型数/key 状态/base-url）、`/provider <pid>`（该服务商模型列表）、`/provider apikey [set <pid> <key>]`、`/provider import [...]`

### 🐛 修复往返损坏

- 导入后写回 `models.json` 时错误复用外部格式解析器（`ParseModelNode` 会从 provider 显示名推断 providerId、硬编码 description），二次写回污染数据；新增专用 `FromJson`（精确往返）读回，`ParseModelNode` 仅用于外部导入

### 🧪 自测 +14

- Claude/Codex 导入解析、模型库序列化往返、自定义库合并/删除

## v0.47.5 (2026-08-13) — API key 统一走服务商 + 小模型服务商跟踪 + 命令行 key 自动入库

### 🔑 全局 key 库格式定版：`[{ "provider": ..., "apikey": ... }]`

- **问题**：上一版 `api_keys.json` 还是扁平 `{ "服务商": "key" }`，与「一个服务商一个 key、一个服务商多个模型」的心智不符
- **修复**：定版为数组 `[{ "provider": "deepseek", "apikey": "sk-..." }, ...]`（对标 OpenCode/Crush 多 key 全局存储），读写均按数组；兼容旧扁平格式与 OpenCode `{ pid: { key } }` 格式自动迁移
- **key 跟服务商走，不跟模型走**：`Config.ApiKey` 解析链 = 全局 JSON（按当前服务商）> 全局 JSON（按模型）> `.env WAYCODER_API_KEY` > 各家环境变量

### 🧩 大/小模型服务商独立跟踪

- **问题**：只有 `Provider` 跟踪大模型服务商，小模型切服务商后 key 解析会错
- **修复**：新增 `SmallProvider`（`WAYCODER_SMALL_PROVIDER`）字段；`--model small <id>` 子命令选中/持久化小模型并同步小模型服务商

### 🐛 修复 `.env` 污染

- **问题**：切换服务商后 `SaveToEnvFile` 会把旧服务商的 key 写成新模型的 `WAYCODER_API_KEY`（污染 `.env`，切 key 错乱）
- **修复**：`secret` 类型设置项永不写入 `.env`——key 只存全局 `api_keys.json`，`.env` 只存 `MODEL/PROVIDER/SMALL_*` 等非敏感项

### ⌨️ 命令行 key 自动入库

- `--api-key <key>` / `-k <key>` 自动保存到全局 `~/.waycoder/api_keys.json`（按当前服务商，或 `--model` 指定模型所属服务商），无需再手动 `--model key <供应商> <key>`

## v0.47.4 (2026-08-13) — 全局 JSON 多 key 存储：多服务商/模型丝滑切换，无需重输 key

### 🔑 API key 全局 JSON 回退（对标 OpenCode/Crush）

- **问题**：key 只能存 `.env`（单一 key），切换服务商/模型要手动改 `.env` 重输 key
- **修复**：`.env` 无 `WAYCODER_API_KEY` 时，按当前模型供应商自动到全局 JSON（`~/.waycoder/api_keys.json`）找 key——
  - `.env` 只存「当前模型名 + 当前 key」（单 key：`WAYCODER_MODEL` + `WAYCODER_API_KEY`）
  - `api_keys.json` 存「多服务商多 key」（`{ "deepseek": "...", "openai": "...", ... }`）
  - 切换即用：`--model name gpt-5.5` 自动匹配 openai 的 key，无需重输、无需改 .env
- **实现**：`ApiKeyStore.ForModel(modelId)` 按模型目录解析供应商再查 JSON；`Config.Instance.ApiKey` 加载链末尾追加该回退；「API 密钥未设置」报错补 `--model key <供应商> <key>` 引导

### 🧪 自测 +3

- 模型→供应商解析 deepseek/openai + `ApiKeyStore.ForModel` 可调用

## v0.47.3 (2026-08-13) — --model 模型管理：列表 / 选中 / API key / 端点，全程命令行

### 🤖 --model 子命令（对标 /model 斜杠命令）

- **问题**：模型管理（列表/选中/API key）只能在 REPL 内用 `/model`，CLI 只有一个 `--model <名称>` 会话级选择
- **修复**：`--model` 升级为贪长子命令分发器，与 `/model` 共享同一份模型目录（`ModelCli`）——
  - `waycoder --model` → 显示当前大/小模型 + BaseUrl
  - `--model list [关键词]` → 列出模型目录（按供应商分组，当前项标注）
  - `--model name <id>` → 选中并持久化（自动解析供应商 + 默认 BaseUrl + 写 .env）
  - `--model key <供应商> <key>` → 保存 API key（无参列出已存 keys，打码）
  - `--model connect <base-url>` → 设置连接端点（写 .env）
  - `--model <id>` → 快捷选中（仅本次会话，不持久化，向后兼容）
- **实现**：抽 `ModelCli` 静态助手（Current/List/Select/Connect/ListKeys/SetKey），`ModelArg` 变贪长子命令分发；`CliArg.Greedy` 语义改为「吞到下一个以 `-` 开头的旗标为止」，使 `--model gpt-5.5 -y` / `-p` 等组合仍正常解析

### 🧪 自测 +3

- `ModelCli.List` 含标题 / 过滤 deepseek / `ListKeys` 可读

## v0.47.2 (2026-08-13) — --config 命令行参数：启动即配置，无需进 REPL

### ⌨ --config 命令行配置（对标 /config 斜杠命令）

- **问题**：`/config` 只能在 REPL 内使用，脚本/批处理/远程部署场景无法在启动时读写配置
- **修复**：新增 `--config`（短名 `-C`）命令行参数，与 `/config` 共享同一份 Schema 数据源（`ConfigCli`）——
  - `waycoder --config` 或 `--config list` → 列出全部设置项（按分类，含当前值，secret 打码）
  - `--config get <key>` → 读取单项（含描述 / 环境变量 / 可选项）
  - `--config set <key> <value>` → 设置并**立即写入 .env**
  - 简写：`--config <key> <value>` = set，`--config <key>` = get
- **实现**：抽 `ConfigCli` 静态助手（List/Get/Set 返回纯文本），`ConfigCommand`（→屏幕）与 `ConfigArg`（→控制台）共用，消除重复；`CliArg` 新增 `Greedy` 标志 + 解析器贪婪吞参，支持 `--config set <key> <value>` 变长参数

### ⌨ Tiny 模式并入 test 前缀分组

- `-T` / `--tiny` → `-tt` / `--test-tiny`（保留 `--tiny` 别名），与 `-t` / `-tb` / `-tl` 统一为 `-t` 测试族前缀

## v0.47.1 (2026-08-13) — CLI 参数全部补齐短命令

### ⌨ 命令行参数补短选项（频率排序 + 同类前缀分组）

- **问题**：`--tiny` / `--economy` / `--bench` / `--limits` / `--sessions` 只有长名，无短命令
- **命名规则**：
  - 高频参数：首字母小写（`-p` prompt / `-m` model / `-h` help / `-k` api-key / `-t` test / `-e` economy / `-w` watch / `-y` yolo / `-r` resume / `-s` sessions / `-b` base-url / `-v` version / `-i` init / `-d` debug）
  - 低频冲突：首字母大写（`-B` max-budget-usd）
  - 同类测试参数：统一 `-t` 前缀分组（`-t` test / `-tb` test-benchmark / `-tl` test-limits / `-tt` test-tiny）
  - 内部开发参数：`-x` screenshot / `-u` tui-demo / `-z` theme-verify
- 使用手册 CLI 参数表同步更新

## v0.47.0 (2026-08-13) — /config 命令行配置：所有设置项无需进界面

### ⌨ /config 命令行配置（对标 Claude Code /config）

- **问题**：所有配置项只能通过 `/settings` 图形界面逐项点选，脚本/批处理/远程场景无法设置
- **修复**：新增 `/config` 命令，全部设置项（Model/SmallModel/ApiKey/BaseUrl/超时/压缩/沙箱/界面主题…）均可在命令行读写——
  - `/config` 或 `/config list` → 按分类列出全部设置项与当前值（secret 打码）
  - `/config get <key>` → 读取单项（含描述 / 环境变量 / 可选项）
  - `/config set <key> <value>` → 设置并**立即写入 .env**（`SaveToEnvFile`）
  - 简写：`/config <key> <value>` = set，`/config <key>` = get
  - key 大小写不敏感，也可用环境变量名（`/config set WAYCODER_MODEL x`）
  - select 类型校验可选项（错误时列出合法值），number 类型自动钳制（复用 Schema Setter）
  - 主题类设置（ThemePreset/ColorScheme/Border*）改后即时 `SyncTheme` 生效
- **实现**：`Config` 新增 Schema 驱动的 `FindProp`/`GetPropValue`/`TrySetPropValue`，复用同一份 `_schema` 数据源，**消除 SettingsScreen 手写 switch 的重复**；`/settings` 保留图形界面，`/config` 专注命令行
- **兼容**：语法对齐 Claude Code 的 `get`/`set`/`list`，老用户零学习成本

### 🧪 自测 +9

- 新增 `/config` 读写 API 用例：FindProp 按 Key/大小写/环境变量、GetPropValue、TrySetPropValue 成功、非法 select 拒绝、未知项拒绝

## v0.46.0 (2026-08-13) — Token 计数切真实 API 报告：校准消除系统性低估

### 📊 压缩触发切真实 API 校准（P1，对标 Crush 纯 API 报告）

- **问题**：`ContextManager.MaybeCompressAsync` 的三层压缩阈值（裁剪/摘要/折叠）用 `EstimateTokens`（CJK 感知估算）判断，但估算只统计消息内容，**漏掉 system prompt + 工具定义 + 消息元数据**（固定开销约 8–12K），导致压缩整体触发偏晚
- **修复**：用真实 API 报告的 `prompt_tokens` 校准估算——
  - `AddUsage(promptTokens, completionTokens, estimatedTokens)` 新增可选参数，计算 `固定开销 = 真实 prompt − 估算`，移动平均平滑收敛
  - 新增 `EstimateCalibratedTokens()` = 原始估算 + 固定开销（加性模型：system prompt/工具定义固定，不随内容增长，比比例模型更准）
  - `MaybeCompressAsync` 三层判断全部改用校准值，`PreCompact` hook 报告同步校准
  - 未采集到真实用量时（首轮/自测）退化为原始估算，零风险
- **收益**：压缩触发时机更准（校准前 50% 阈值实际 59% 才触发，校准后对齐真实窗口占用），少误压/漏压

### 🧪 自测 +4

- 新增 `TestTokenEstimation` 校准用例：无真实数据退化 / 含固定开销 / 开销平滑收敛 / 校准值 > 估算

## v0.45.0 (2026-08-13) — FileTracker 持久化：跨会话 stale-read 保护

### 💾 文件追踪持久化（P0-2，对标 Crush last_read_time）

- **问题**：`FileTracker` 哈希与读取时间仅存内存，程序重启后全部丢失，跨会话的 stale-read 检测与「先读后改」保护失效
- **修复**：追踪状态持久化到 `.waycoder/file-tracker.json`（纯 JSON，**零依赖、无数据库**），重启后自动恢复
  - `RecordRead` / `RecordWrite` / `CheckForChanges` / `Reset` 在状态变更后自动 `Save()`
  - `EnsureLoaded()` 惰性加载——首次使用时从磁盘读回，仅一次
  - **原子写**：先写 `.tmp` 再 `File.Move(overwrite:true)`，防止中断损坏缓存
  - 上限保护：磁盘数据超出 `MaxTracked=200` 时丢弃多余条目；损坏/不可读时静默退化为内存模式
- **体积零增长**：复用 `JsonNode` 手写序列化（与 `todos.json` 同模式），不引入 SQLite / 任何第三方库，AOT 兼容

### 🧪 自测 +5

- 新增 FileTracker 持久化往返：记录生成 JSON / 含路径与 hash 字段 / 模拟重启后仍追踪 / 检测到外部修改

## v0.44.0 (2026-08-13) — bash 前台超时自动迁移后台

### ⏱ 后台命令自动迁移（对标 Crush）

- **问题**：前台 `bash` 命令超时后直接 `Kill` 进程并返回「错误：超时」，长任务（build/test）已执行的工作白费，Agent 需重新跑
- **修复**：超时后不再杀死进程，自动转入后台继续执行并返回 `shell_id`，Agent 可用 `job_output` 轮询、`job_kill` 终止
  - **非流式路径**（`-p` 一次性模式 / benchmark）：`BackgroundTaskManager.Adopt` 接纳已运行进程 + `ReadToEndAsync` 任务
  - **流式路径**（交互 REPL 实时输出）：`BackgroundTaskManager.AdoptStreaming` 接纳逐行读取任务 + 共享输出缓冲
- **重构**：`RunTaskAsync` 提取 `WaitAndCollectAsync`（等待退出 + 收集 IO + 写状态），`Start` / `Adopt` / `AdoptStreaming` 三条路径共用，消除重复
- **沙箱模式例外**：仍直接终止，避免迁移绕过内存/CPU 资源上限
- **进程所有权**：迁移后由后台管理器负责 dispose，前台不再释放句柄（`migrated` 标志 + `finally`）

### 🧪 自测 +1

- 新增 `bash 超时自动迁移到后台`（慢命令 + 短超时 → 断言返回 `Shell ID`）

## v0.43.0 (2026-08-13) — 省 Token 模式：三态开关 + 任务复杂度自适应

### 💰 省 Token 模式（`--economy [on|auto|off]` / `WAYCODER_ECONOMY` + `WAYCODER_ECONOMY_PRIORITY`）

- **新增** `EconomyMode` 三态开关（默认 `off`），保持正常窗口不变：
  - **关（off）**：完整提示词 + 正常压缩阈值
  - **开（on）**：从四个方面综合降 token——
    - 系统提示词精简（`SystemPrompt.GenerateEconomy` 砍 RepoMap/Git/记忆/10 阶段流水线，保留完整工具描述 + 项目上下文 + 9 条核心规则）
    - 压缩更激进（snip/summarize/collapse 50/70/90 → 35/55/75）
    - 工具输出更早裁剪（4000 → 2000 字符）
    - 输出上限收紧（`max_tokens` 32768 → 8192）
  - **自动（auto）**：保持完整提示词，压缩阈值/裁剪阈值按**任务轮数复杂度**动态插值——任务越复杂（轮数越多）越少省，先保质量、再省费用
- **新增** `EconomyPriority` 优先级偏好（仅 Auto 生效，默认 `quality`）：
  - `quality` 质量优先：简单任务省、复杂任务几乎不省
  - `balanced` 均衡：始终保留一半省钱力度
  - `cost` 费用优先：尽量省，弱化复杂度影响
- **与 Tiny 的区别**：Tiny = 极简提示词 + 4K 小窗口（面向本地小模型）；Economy = 保持正常窗口，仅综合省 token（面向云端大模型省钱）
- **`reasoning_effort` 不做最小化**：对不支持推理参数的非 DeepSeek/OpenAI 模型会 400，风险大于收益；reasoning 省 token 已由「reasoning_content 不存历史」覆盖

### 🧪 自测 +26（1591 → 1617）

- 新增 `TestEconomyMode`（三态默认值 / 优先级偏好 / ResolveRatio 复杂度插值 / 提示词精简 / snip 阈值对照 / 常量）

## v0.42.0 (2026-08-13) — Tiny 模式增强：可指定窗口 + 自动探测 + 小模型自动进入

### 🐭 Tiny 模式窗口可指定 / 自动探测

- **`--tiny 8k` 指定窗口**：`TinyArg.ValueCount=-1` 支持可选值，`ModelCatalog.ParseWindowSpec` 解析 `8k`/`8192`/`4K` 等规格
- **`--tiny` 自动探测**：`ProbeModelWindow` 优先调 Ollama `/api/show` 读真实 `context_length`（解决目录对本地模型标称 128K 虚高问题），其次内置目录 `ContextWindow`，最后回退 4K
- **`<128K` 自动进入 tiny**：模型窗口低于 `TinyAutoThreshold=128_000` 时自动启用 tiny（精简提示词 + 对应窗口），本地小模型开箱即用，无需手动 `--tiny`

### 🔧 实现

- `Config.TinyWindow`（可运行时覆盖的窗口，默认 4K）+ `Config.TinyAutoThreshold=128_000`
- `ModelCatalog.ResolveTinyWindow` / `ProbeModelWindow` / `ParseWindowSpec` / `IsOllamaBaseUrl` / `QueryOllamaContextLength`（2s 超时，失败静默回退）
- `ResolveContextWindow` 在 Tiny 模式读 `Config.Instance.TinyWindow`（不再写死 4K）
- `Program` 在 base URL 解析后统一判定：显式 `--tiny` 或窗口 `<128K` 自动进入

### 🧪 自测 +17（1574 → 1591）

- 新增 `TestTinyWindow`（窗口规格解析 / 显式指定 / 自动探测目录与兜底 / ProbeModelWindow / 128K 阈值 / Ollama base url 识别）

## v0.41.0 (2026-08-13) — Tiny 模式：4K 上下文窗口也能写程序

### 🐭 Tiny 模式（`--tiny` / `WAYCODER_TINY=1`）

- **新增** `--tiny` CLI 参数 + `WAYCODER_TINY` 配置：4K 上下文窗口 + 极简系统提示词，省 token / 压力测试
- **窗口固定 4K**：`Config.TinyContextWindow=4096`，`ModelCatalog.ResolveContextWindow` 在 Tiny 模式下忽略模型窗口返回 4K
- **极简提示词** `SystemPrompt.GenerateTiny`：砍掉 RepoMap/记忆/技能/10 阶段流水线/冗长规则区块，只留身份+环境+工具+8 条核心规则，从 1 万+ token 压到 <3K 字符
- **依赖压缩 + 自动续跑**：4K 窗口下压缩更频繁、自动续跑接管，持续写程序不中断

### 🧪 自测 +8

- 新增 `TestTinyMode`（窗口常量 / 固定 4K / 提示词精简 / 核心规则保留）

## v0.40.0 (2026-08-13) — 自动续跑 + 压缩保真度 + 上下文窗口按模型切换

### 🔁 自动续跑（撞 MaxRounds 上限不再退出）

- **问题**：大任务撞 `MaxRounds=50` 就 `return` 提示「输入继续」，一次性模式（`-p`）无交互通道，进程直接退出，只能手动开新实例接力
- **修复**：撞上限且仍在写文件时，自动压缩 + 注入「继续 + 已完成文件清单」提示后重跑；`WAYCODER_MAX_REQUEUE` 控制次数（默认 3，0=关闭，上限 20）
- **提取** `InjectContinuePrompt`：压缩后自动继续与撞上限续跑共用同一注入模板

### 🧠 压缩保真度增强：无 LLM 回退摘要保留需求清单

- **问题**：无 LLM 的离线回退摘要 `ExtractKeyInfo` 只提取文件路径/命名空间/错误码，压缩后 Agent 会「忘记」还剩哪些需求没做
- **修复**：新增正则提取「需求 N：/Requirement N：/`- [ ]` 未完成勾选/TODO/待办」条目，摘要输出「待完成需求」段（去重取前 10）

### 📏 上下文窗口按模型切换（不再写死）

- **问题**：`MaxContextTokens` 全局写死 1M，切换模型不更新窗口 —— 切到 64K/128K 小窗口模型时压缩触发过晚，模型先报 `context length exceeded`
- **修复**：`ModelCatalog.ResolveContextWindow` 按模型目录 `ContextWindow` 解析窗口；`ContextManager.UpdateMaxTokens` 运行时重算三层压缩阈值；切模型入口（`/model`、ModelPicker、槽位切换、启动初始化）统一同步
- **修正**：Schema 默认串 `128000` → `1048576`（与代码默认一致）

### 🧪 自测 +22

- 新增 `TestCompressionFidelity`（30 需求压缩后保留路径/命名空间/错误码/需求清单）+ `TestContextWindowSwitch`（按模型解析 + 阈值重算 + 边界）+ `MaxAutoRequeue` 默认值

## v0.39.0 (2026-08-13) — 界面对标：`/` 补全接注册表 + 上下文用量 Gauge

### ✨ `/` 斜杠命令补全接 SlashCommandRegistry

- **问题**：`/` 补全用硬编码 14 条命令数组，新增斜杠命令不会自动出现在补全里
- **修复**：`ChatScreen.BuildPrefixHints` 的 `case '/'` 改为遍历 `SlashCommandRegistry.Commands`，`Name`/`Aliases` 参与匹配，`Usage` 作标签，`Value` 用主命令名

### ✨ 上下文用量彩色 Gauge（动态栏右段常驻）

- **新增**：`TuiDynamicBar.ContextPercent` 属性，右段常驻 `📊 {pct}%`，颜色绿(≤30%)→黄(≤70%)→红(>70%)
- **数据桥**：`ChatScreen.UpdateTokenDisplayFull` 计算 `_contextPercent`，`SyncDynamicBar()` 每帧同步；空闲态模型名与上下文% 并存

### 🧪 自测 + 修复 flaky 测试

- 新增 4 项 `SlashCommandRegistry` 测试（非空 / 含 `/help` / 含 `/model` / 数量 ≥14）
- 修复「日志包含堆栈信息」flaky 测试：`new` 出来的异常 `StackTrace` 为 null，改为 throw/catch 生成真实堆栈，不再依赖历史崩溃日志残留

## v0.38.0 (2026-08-12) — Agent 工具 tasks 数组解析 Bug 修复

### 🐛 修复：agent 工具 tasks 数组退化成字符串（16639 任务 bug）

- **问题**：调用 `agent` 工具传 `tasks` 数组时，3 个任务被拆成 16639 个「单字符任务」
- **根因**：`LLM.ParseArgs` 把 `JsonArray`/`JsonObject` 用 `ToJsonString()` 序列化成字符串，`AgentTool.ExecuteParallelAsync` 再把字符串当 `IEnumerable<char>` 逐字符遍历
- **修复**：新增 `JsonNodeToObject` 递归转换（数组→`List<object?>`、对象→`Dictionary<string, object?>`、标量→原生类型），`ParseArgs`/`TryParseCompleteJson` 两处解析入口统一走该转换

### 🐛 修复：JSON 数字类型保真（自测崩溃中断）

- **问题**：JSON 整数被解析成 `JsonElement` 或 `double`，`(long)x` 强转抛 `InvalidCastException`，导致自测进程 `AppDomain.UnhandledException` 崩溃中断
- **根因**：`JsonValue.GetValue<object>()` 返回 `JsonElement`；`TryGetValue<long>` 对整数 JSON 值也走 double 路径
- **修复**：新增 `JsonValueToNative` + `ParseJsonNumber`，基于原始文本 `long.TryParse`/`double.TryParse` 精确区分整数/小数，>2^53 的大整数也不丢精度

### 🔧 加固：AgentTool 字符串防御

- `IEnumerable` 分支加 `is not string` 守卫，杜绝字符串被逐字符遍历
- 新增 `string` 防御分支，兼容 LLM 直接返回单个字符串或旧版序列化 JSON 字符串

### 🧪 新自测（7 项）+ 修 3 项自测自身 bug

- ParseArgs 数组解析为 List、嵌套对象解析为 Dictionary、小数/负整数/大整数类型保真、混合数组保序保类型、嵌套数组递归解析
- AgentTool Schema 含 tasks 并行数组
- 修复：超时默认值测试改用 `new Config()` 测代码默认值（不再受 `.env` 600 覆盖影响）；仓库地图根路径测试支持 macOS 绝对路径

### 📊 评分

- v0.37.0: 80/100
- v0.38.0: **82/100** ✅（Agent 工具并行 bug 修复 + 数字类型保真 +2 分）

## v0.37.1 (2026-08-12) — 对话框首次显示 Bug 修复

### 🐛 修复：首次打开对话框尺寸/位置不计算 + 按钮不显示

- **问题**：启动后第一次打开对话框，窗口以默认 30×10 尺寸渲染在屏幕左上角 (0,0)，按钮全部堆叠不可见
- **根因**：`TuiScreen.AddWindow()` 只调用了 `win.OnCreate()`（生命周期），从未调用 `win.OnResize(TW, TH)`
  - `OnResize` 是唯一执行 XScale→Width、YScale→Height、WindowHAlign/WindowVAlign→居中定位、RootView.Layout()→Flex 按钮布局的地方
  - 终端 resize 事件会触发 `OnResize`，但首次创建时不会
- **修复**：`AddWindow()` 中 `win.OnCreate()` 之后添加一行 `win.OnResize(TW, TH)`
- **影响**：所有对话框（设置、模型选择、确认框、输入框等）首次打开即正确居中、比例缩放、按钮可见

## v0.37.0 (2026-08-12) — 文件先读后改保护 + Git 状态注入 + Agent 工具分层

### 🔧 改进 1：文件先读后改保护（对标 Crush last_read_time）

- **FileTracker.ValidatePreEdit**：文件写入/编辑前检查是否已先读取，防止 LLM 凭猜测编辑
- 未读取过 → 返回警告：必须先用 `read_file` 读取
- 读取后被外部修改 → 返回警告：文件已变更，需重新读取
- `FileTracker.RecordRead` 同步记录 `LastReadTimes`（时间戳字典）
- 集成工具：`WriteFileTool`（覆写已有文件时）、`EditFileTool`（编辑前）、`MultiEditTool`（编辑已有文件时）
- 新文件/不存在的文件不检查（无需先读）

### 🔧 改进 2：Git 状态注入系统提示词（对标 Crush git status）

- 系统提示词新增 `__GIT_STATUS__` 区块，每次启动自动注入当前 Git 状态
- 包含：当前分支名、工作区变更（git status --short，最多 15 项）、最近 3 次提交
- 让 LLM 感知当前 Git 上下文，减少误操作（如在不干净的工作区做提交）
- 非 Git 仓库时自动跳过（返回空字符串）

### 🔧 改进 3：Agent 工具集分层

- **子智能体工具白名单**：`ToolRegistry.SubAgentDeniedTools` — 禁止子智能体使用 bash/rm/kill/git 等危险工具
- **集中化过滤**：`ToolRegistry.GetSubAgentTools(parentTools, depth, maxDepth)` 统一管理子智能体工具集
- 子智能体仅保留安全工具：read_file、write_file、edit_file、grep、glob、ls 等读写/搜索工具 + agent（深度限制）
- 危险的 shell 命令/进程管理/Git 操作仅主智能体可用

### 🧪 新自测（20 项）

- FileTracker 先读后改：未读警告、已读通过、外部修改警告、新文件通过、Reset 清空
- Agent 工具分层：SubAgentDeniedTools 包含危险工具、子 Agent 不同深度工具集验证
- Git 状态注入：非 null、包含仓库信息

### 📊 评分

- v0.36.0: 72/100
- v0.37.0: **80/100** ✅（文件先读后改 +8 分）
- 达到可发布标准（80 分）

## v0.36.0 (2026-08-12) — 自编程稳定性修复 + 竞品对比驱动改进

### 🧪 macOS Web Desktop 自编程测试

- **WayCoder 成功自编程**：从零写出 1,251 行 / 52KB 的 macOS 风格 Web 桌面（`demo/index.html`）
- 包含菜单栏、Dock 栏、窗口系统（拖拽/缩放/动画）、6 个可交互应用（Finder/终端/计算器等）
- 过程中暴露 4 个关键缺陷，本轮全部修复

### 🐛 修复 1：ParseArgs 错误参数泄漏（12 分）

- **问题**：`ParseArgs()` JSON 解析失败时返回 `_parse_error`/`_parse_error_type`/`_raw_json_snippet` 伪参数，被当作真实工具参数传递
- **表现**：LLM 调用 `write_file(_parse_error=True, _parse_error_type=JsonReaderException, ...)`，文件路径丢失、工具调用幻觉
- **根因**：`LLM.cs` 第 729 行在解析异常时将错误标记写入参数字典，上层未清理直接传入工具
- **修复**：`ParsedToolCalls` 逻辑中检测并清除 `_parse_error*` 键，仅保留合法参数，将截断信息记录到调试日志

### 🐛 修复 2：推理（Reasoning）独占检测缺失（10 分）

- **问题**：DeepSeek V4 等模型将大量输出花在 `reasoning_content` 上（显示但不计入 `Content`），`_analysisOnlyStreak` 的 `contentLen > 100` 条件不触发
- **表现**：模型输出数千字推理内容、零工具调用，Agent 静默等待直到超时
- **根因**：推理内容被 `LLM.cs` 从 `contentParts` 剥离（设计决策：推理不存入对话历史），导致 Agent 层看不到
- **修复**：
  - `LLMResponse` 新增 `ReasoningTokens` 字段，传递推理内容长度
  - `Agent.cs` 新增检测：`ReasoningTokens > 300 && ToolCalls.Count == 0 && Content.Length < 80` → 渐进式催促
  - 与 `_analysisOnlyStreak` 共享计数器，三级递进 nudge

### 🔧 改进 3：文档细化（对标 Crush 竞品分析）

- 对 Crush（Go 版 Claude Code）v2.1.88 做了系统性 10 维竞品对比分析
- 识别出 WayCoder 的差异化优势（3 层上下文压缩、渐进式反循环检测、Schema 驱动配置）和竞品值得学习的模式（多供应商 SDK、后台命令自动迁移、基于事件总线的权限架构、多作用域配置）
- 详细对比记录在 `docs/waycoder-vs-crush-comparison.md`

### 🧪 新自测

- `ParseArgs` 错误标记清除：验证截断 JSON 不泄漏 `_parse_error` 到工具参数
- `LLMResponse.ReasoningTokens` 字段完整性：验证新字段存在且正确传递
- `_talksCodeStreak` + 推理独占检测端到端：模拟 3 轮推理独占 → 验证渐进式 nudge

### 📊 评分

- 自编程测试前 WayCoder 编程能力评分：**62/100**（初级工程师水平）
- 本轮修复后目标：**72/100**（接近中级工程师，改善了口述代码、错误参数泄漏、推理独占三大核心问题）
- 距离 80 分可发布目标的差距：**8 分**（需要进一步改进：文件读写时间戳保护、Agent 工具集分层、系统提示词动态化）

---

## v0.35.0 (2026-08-12) — Flex 弹性布局 + 窗口比例缩放 + 位置对齐 + 对话框标准化

### ✨ Flex 弹性布局系统

- **`TuiBase.Flex`**：所有 UI 元素新增 `Flex` 属性（默认 0=固定尺寸，>0=按比例分配父容器剩余空间）
- **`TuiHBox.Layout()`**：水平容器支持 Flex — Flex=0 固定宽度，Flex>0 按权重比例分配剩余空间
- **`TuiVBox.Layout()`**：垂直容器支持 Flex — 同算法垂直方向
- **后向兼容**：所有现有控件 Flex=0，行为完全不变

### ✨ 窗口比例缩放（XScale / YScale）

- **`TuiWindow.XScale`**：窗口宽度 = 终端宽度 × 比例（0=禁用，如 0.5=半屏宽）
- **`TuiWindow.YScale`**：窗口高度 = 终端高度 × 比例（0=禁用，如 0.4=40%屏高）
- **约束支持**：`MinWidth`/`MinHeight`/`MaxWidth`/`MaxHeight` 自动钳制缩放结果
- **手动拖拽清零**：鼠标拖拽缩放窗口后自动清零 XScale/YScale，切换到固定尺寸

### ✨ 窗口位置对齐（WindowHAlign / WindowVAlign）

- **`TuiWindow.WindowHAlign`**：`Left`/`Center`/`Right`/`Stretch`(不定位)，resize 时自动重算 X
- **`TuiWindow.WindowVAlign`**：`Top`/`Middle`/`Bottom`/`Stretch`(不定位)，resize 时自动重算 Y
- **`TuiWindow.ScreenMargin`**：窗口与屏幕边缘偏移（Toast 右下角 + 偏移等场景）
- **移除 `AutoCenter`**：`WindowHAlign=Center + WindowVAlign=Middle` 等效替代

### ✨ 对话框全面标准化

- **全部 11 种对话框重构**（Info/Success/Warn/Error/Confirm/Confirm3/Input/Secret/Select/MultiSelect/Permission）
- 使用 `XScale` 替代手动 `CalcMaxMsgWidth()` + clamp 宽度计算
- 使用 `WindowHAlign`/`WindowVAlign` 替代 `win.Center()`
- 按钮使用 `Flex=1` 均分替代 `NormalizeButtons()` 手动统一宽度
- **移除 `OnResizeContent` 完全重建**：框架自动处理 resize → 窗口缩放 → Flex 重分配 → 重绘
- 代码量 914 行 → 338 行（-63%）

### 🧪 测试

- Flex 布局测试 16 项（含 HBox/VBox Flex 分配、Margin/Spacing 配合、后向兼容）
- 窗口比例缩放测试 10 项（XScale/YScale、Min/Max 约束、两维同时、手动保持）
- 窗口位置对齐测试 12 项（9 种对齐组合 + AutoCenter 兼容 + ScreenMargin Offset）
- 端到端集成测试 9 项（终端 resize → Screen → Window → Flex 全链路）
- 总计 **1499 通过 / 0 失败**

---

## v0.34.2 (2026-08-12) — 对话框 Resize 刷新 + 4 项 P0 修复

### 🐛 对话框 Resize 不刷新（修复）

- **根因**：`TuiWindow.OnResize()` 不调用 `RootView.MarkDirty()`，增量渲染可能跳过窗口重绘；窗口保持创建时尺寸，缩小终端时溢出、扩大终端时不利用额外空间
- **修复 1**：`TuiWindow.OnResize()` 新增 `RootView.MarkDirty()` 强制下一帧重绘窗口
- **修复 2**：`TuiWindow.OnResize()` 新增窗口位置 clamp（防止缩小终端时溢出）
- **修复 3**：新增 `TuiWindow.OnResizeContent` 回调，`TuiDialog` 各工厂方法设置此回调以在 resize 时重建控件
- **修复 4**：全部 `TuiDialog` 方法（Info/Success/Warn/Error/Confirm/Confirm3/Input/Secret/Select/MultiSelect/Permission）均已改造，状态型对话框（Input/Secret/Select/MultiSelect）保留用户输入/选择状态
- **设计**：每次 resize 重建控件树（`CalcMaxMsgWidth()` 重新按 `Tty.Cols` 计算宽度）→ 窗口尺寸自适应新的终端尺寸

### 🐛 P0-1：孤立工具调用/结果修复（Agent.cs）

- **根因**：Agent 中断（Ctrl+C）、会话恢复或 LLM 输出截断导致 assistant tool-call 无对应 tool-result，下轮 API 拒绝请求
- **修复**：实现 `RepairOrphanedToolPairs()`（对标 Crush `filterOrphanedToolResults` + `syntheticToolResultsForOrphanedCalls`）
  - 收集所有 assistant 消息的 tool_call ID → `callIds`
  - 收集所有 tool 消息的 tool_call_id → `resultIds`
  - 无结果的 tool-call → 注入合成错误 tool-result：`[工具执行被中断] 工具 "{name}" 的调用未能完成执行...`
  - 无对应 tool-call 的 tool-result → 从 Messages 中删除
- **效果**：中断后恢复的会话不再因孤例配对而 API 报错

### 🐛 P0-2：循环检测改 per-tool 级（Agent.cs）

- **根因**：旧方案对整轮做哈希，同轮中其他工具不同会掩盖某个工具的重复调用
- **修复**：重写 `DetectAndBreakLoop()` 为 per-tool-call 级
  - 每个工具单独哈希：`tool_name + args_json + output[..2000]`
  - 滑动窗口 10，阈值 5 → 循环警告；批量检测提示"共 N 个模式重复"
- **效果**：更精准检测 write→lint-error→rewrite 等单工具重复模式

### 🐛 P0-3：编辑前 mtime 检查（Agent.cs）

- **根因**：Agent 读文件后、编辑前，文件可能被 bash 外部修改，导致基于过期内容编辑
- **修复**：`ExecuteToolAsync()` 中 edit_file/write_file 前检查 `FileTracker.GetStatus()`
  - 文件 stale → 返回警告要求先 re-read（对标 Crush edit guard）
  - 第二次调用确认后放行；成功写入自动更新 FileTracker 哈希
- **效果**：防止 Agent 基于过期文件内容做编辑决策

### 🐛 P0-4：任务系统升级 — CRUD + 依赖（TodoTool.cs）

- **根因**：旧 TodoTool 无依赖、无描述、无持久化、int ID
- **修复**：重写为对标 Crush todos + Claude Code TaskCreate 的完整 CRUD 工具
  - string ID / description 字段 / deps 依赖列表 / 5 种状态含 blocked
  - 依赖检测：blocked→in_progress 需依赖完成；完成自动解除阻塞任务
  - 持久化 `.waycoder/todos.json`；兼容旧 `Items` API（侧栏、/todo 命令、自测）
- **效果**：Agent 可规划依赖型多步骤工作，任务跨会话持久保留

---

## v0.34.1 (2026-08-12) — 稳定性修复 8 项 + 竞品分析

### 🧪 Roguelike 稳定性测试

WayCoder 编写 10,418 行 Roguelike 游戏项目（35 文件），期间发现并修复 8 个问题。

### 🐛 P0 修复：工具参数静默丢失（LLM.cs）

- **根因**：`ParseArgs()` JSON 解析失败时静默返回空字典 `{}`，工具调用参数丢失
- **修复 1**：`ParseArgs` 失败时返回 `_parse_error` 标记字典 + 原始 JSON 片段，调用方可检测
- **修复 2**：`TryParseCompleteJson` 新增 `IsJsonProbablyComplete()` 完整性预检：花括号平衡、不以逗号/冒号结尾、引号成对
- **修复 3**：流式结束后检查 `[DONE]` 标记，未收到则记录截断警告
- **修复 4**：最终解析循环中检测 `_parse_error` 标记并记录日志

### 🐛 P0 修复：系统提示词强制探索模式（SystemPrompt.cs + Agent.cs）

- **根因**：`critical_rules` 第 1 条和 `workflow` 强制"先读后改"，与用户"不要读文件"指令冲突
- **修复**：新增快速模式 — 检测用户消息中的关键词（不要读文件/不要ls/不要规划/直接用write_file），自动替换工作流和规则 1 为"直接执行"版本
- `SystemPrompt.DetectFastMode()` 关键词检测 + `StandardWorkflow/FastModeWorkflow/StandardRule1/FastModeRule1` 公开属性
- `Agent.FullMessages()` 快速模式时替换工作流文本

### 🐛 P1 修复：思考流代码丢失（LLM.cs + SystemPrompt.cs）

- **根因**：`reasoning_content` 显示但不存储，模型在思考中生成 400 行代码 → 流截断后零落盘
- **修复 1**：新增 `_reasoningBuffer` StringBuilder 旁路缓冲区，累积推理文本
- **修复 2**：流结束后检测代码特征（`;` `{` 计数 > 20），警告可能丢失代码
- **修复 3**：推理内容保存到 `DebugLog.Log("reasoning", ...)` 供调试恢复
- **修复 4**：SystemPrompt `critical_rules` 新增第 16 条：不要在思考流中生成代码

### 🐛 P1 修复：最大轮次静默退出（Agent.cs）

- **根因**：达到 `_effectiveMaxRounds` 后返回固定消息，不报告完成状态
- **修复**：检测最近 10 条消息中的 `✅ 已写入/✅ 编辑完成` 标记，区分"正在写文件时退出"和"已完成退出"，输出差异化提示

### 🐛 P1 修复：ContinuePrompt 缩小已有文件（Agent.cs）

- **根因**：压缩后的继续提示不说"不要重写"，模型重新读取后可能用更短版本覆盖
- **修复 1**：ContinuePrompt 追加"不要重写或缩小已有文件"
- **修复 2**：自动收集已创建/修改的文件清单（从 write_file/edit_file 工具结果提取）注入继续提示

### 🐛 P1 修复：过度规划无渐进催促（Agent.cs）

- **根因**：首轮分析不行动只催促一次，模型继续分析时没有逐次加强的迫使
- **修复**：新增 `_analysisOnlyStreak` 计数器，第 1 次温和催促 → 第 2 次严肃要求 → 第 3+ 次严重警告。工具调用时自动重置

### 🐛 P1 修复：FallbackLLM 静默失败（LLM.cs + FallbackLLM.cs + Agent.cs）

- **根因**：所有回退模型失败后返回纯文本错误，Agent 当成正常回复退出
- **修复 1**：`LLMResponse` 新增 `IsFatalError` 标记
- **修复 2**：`FallbackLLM` 错误响应设置 `IsFatalError = true`
- **修复 3**：`Agent` 检测到致命错误时自动调用 `SessionManager.SaveSession()` 保存会话

### 📊 竞品对比分析

对比分析 Crush（Go）和 Claude Code（TypeScript）两大竞品，输出 15 项可借鉴改进，优先级排序：
- **P0**：孤立工具调用修复、per-tool 循环检测、编辑 mtime 检查、任务 CRUD 系统
- **P1**：流式工具执行、bash 自动后台、Agent 类型注册表、摘要保 todo、Hook 扩展
- **P2**：Worktree 隔离、工具描述缩短、工具结果磁盘持久化、文件建议、花费追踪

---

## v0.34.0 (2026-08-12) — 系统化流水线 + 渐进超时重试 + Hook 系统 + 动态栏

### 🐛 聊天列表不实时刷新（修复）

- **根因**：`AppendToLast()` 在流式 token/tool 输出时更新内容但未设脏标记，`TuiManager.Render()` 的 `IsDirty` 检查跳过渲染
- **修复**：`AppendToLast()` 尾部新增 `Manager.IsDirty = true`，折叠提示同步 `MarkDirty()`
- **原理**：只需标记 Manager — TuiView 子容器总是被遍历，`ChatList.OnRender` 渲染所有可见子项无需单独标记

### 🧠 10 阶段系统化流水线（`<systematic_phases>`）

SystemPrompt 新增 `<systematic_phases>` 区块，复杂任务（3+ 文件、多步骤、新建项目）强制按 10 阶段执行：
**调查→分析→规划→拆分→分工→执行→调试→审核→提交→总结**。每个阶段内部完成，不向用户叙述过程，只交付结果。

### ⏱ 渐进超时 + 自适应重试

- **逐次加长超时**：1x→1.5x→2x→3x→4x→6x→8x 倍率，每次重试独立 `CancellationTokenSource`
- `GetTimeoutMultiplier(attempt)` 计算当前尝试的超时倍率，超出内置数组后线性递增
- 超时日志记录当前超时秒数 + 下次尝试的超时秒数，便于诊断
- 每次 HTTP 调用入口恢复原始 `_http.Timeout`，确保并发请求不受影响
- 默认重试次数：3→5，HTTP 超时上限：900s→3600s

### 🎣 Hook 系统全面升级（对标 Claude Code Hooks）

**8 种事件类型**：
- `PreToolUse` — 工具调用前（可阻止/批准/拒绝）
- `PostToolUse` — 工具调用成功后
- `PostToolUseFailure` — 工具调用失败后
- `SessionStart` — 会话启动时
- `SessionEnd` — 会话结束时
- `Stop` — Agent 完成一轮后（可注入额外上下文）
- `PreCompact` — 上下文压缩前
- `Notification` — 通知事件（权限提示等）

**结构化输出协议**：
- `HookOutput` JSON 格式：`Continue`（继续/阻止）、`Decision`（approve/block）、`Reason`、`SystemMessage`、`AdditionalContext`
- `HookMatcherConfig` 匹配器：支持管道分隔 `"bash|git"`、正则 `"^Write"`、通配符 `"*"`
- 向后兼容纯文本 stdout（非 JSON 视为 SystemMessage）

### 📊 动态状态栏（对标 Claude Code Status Line）

- **`TuiDynamicBar`** 1 行控件：左段（模型状态 + 旋转动画）、中段（当前工具/任务）、右段（上下文压缩进度条）
- **6 种状态**：Idle / Thinking / ToolRunning / Compressing / WaitingPerm / Error
- **Braille 旋转动画**：⣾⣽⣻⢿⡿⣟⣯⣷ 基于 `DateTime.UtcNow` 计算帧（不依赖定时器）
- **上下文压缩进度条**：`████░░░░ 45%` 迷你 8 字符进度条
- 订阅 `ContextManager.CompressProgress` 事件，实时层号和进度百分比

### 💰 任务级花费追踪

- `LLM.TaskPromptTokens` / `TaskCompletionTokens`：当前任务的 token 消耗（从快照点计）
- `LLM.TaskCost`：当前任务的花费估算（美元），模型在定价表中时返回
- `LLM.SnapshotTaskCost()`：Agent 每轮对话开始时调用，建立快照
- `LLM.ResetTaskCost()`：任务取消或异常时重置

### 📝 工作总结报告 + 结构化 Todo

- **`WorkReporter`**：Agent 完成一轮后自动生成结构化摘要，包含新增/修改/删除文件、关键决策、下一步
- 报告保存到 `.waycoder/reports/latest.md`，失败不影响主流程
- **`ExportTool`**：对话导出工具（Markdown / JSON / HTML），Agent 可在用户请求时调用
- **`StructTodoTool`**：增强版 Todo 工具，支持优先级、依赖关系、状态追踪

### 📟 终端协议增强

- **Bracketed Paste**：启用 `\x1b[200~...\x1b[201~` 包裹粘贴内容，`ReadPasteContent()` 安全读取
- **Kitty 键盘协议**：`AnsiTty.EnableKittyKeyboard()` 启用修饰键完整报告
- **CSI 功能键解析器**：统一处理 Bracketed Paste + Kitty + xterm 功能键序列
- 粘贴内容通过 `InputType.Paste` 事件路由，自动过滤 ANSI 转义序列

### 🗂 槽位任务队列（`-p1`~`-p0`）

- **槽位专项任务**：`-p1 "提示词"` ~ `-p9`、`-p0`=F10，同一槽位多次 `-pN` 可排队
- **共享前缀**：`-pa "前缀"` 拼到每个 `-pN` 任务前面
- 槽位任务自动强制进入 REPL 交互模式（非一次性模式）
- `BuiltinArgs` 新增 11 个 `slot-prompt-N` 参数注册

### 🧠 跨会话记忆检索

- **`MemoryRetrieval`**：跨会话记忆检索，使用 TF-IDF + 时间衰减排序
- 系统提示词生成时自动加载匹配记忆（最多 5 条），与结构化记忆合并注入
- 防抖机制：相同查询 60 秒内不重复检索

### 🔧 上下文压缩改进

- **Snip 阈值**：1500→4000 字符（保留更多工具输出信息）
- **错误行保留**：裁剪时保留编译错误、异常堆栈等关键诊断信息
- **IsCompressing 状态**：静态属性标记压缩进行中，UI 可据此显示进度
- **`CompressProgress` 事件**：每层压缩完成时触发，包含层号、消息、百分比
- **`ProgressBar()`**：8 字符迷你进度条生成器

### 🏗 基础设施新增

| 文件 | 说明 |
|------|------|
| `Agent/TaskProgress.cs` | 任务进度追踪（并发安全） |
| `Agent/WorkReporter.cs` | 工作总结报告生成器 |
| `Infra/IdGenerator.cs` | 加密安全 ID 生成 |
| `Infra/LruCache.cs` | 线程安全 LRU 缓存（支持 TTL 过期） |
| `Infra/MemoryRetrieval.cs` | 跨会话记忆检索 |
| `Infra/RetryPolicy.cs` | 智能重试策略（指数退避 + 异常过滤） |
| `Infra/SnippetStore.cs` | 代码片段管理器 |
| `Infra/Logging/` (9 文件) | 结构化日志系统（ILogSink/File/Console/JSON + 指标） |
| `Tools/ExportTool.cs` | 对话导出工具 |
| `Tools/StructTodoTool.cs` | 结构化 Todo 工具 |
| `UI/TuiControls/TuiDynamicBar.cs` | 动态状态栏控件 |
| `UI/TuiControls/TuiKeybindHelp.cs` | 键盘快捷键帮助面板 |
| `UI/TuiControls/TuiToastQueue.cs` | Toast 通知队列 |

### ⚙ 配置变更

- `LlmMaxRetries`：3→5（更多重试机会）
- `LlmHttpTimeoutSec` 上限：900→3600（1 小时，适应深度思考模型）
- `WatchExtensions` + `WatchIgnoreDirs`：新注册到设置界面（67/67 全部可配）
- `ToolTimeout` 默认：300s
- `BackgroundTaskTimeoutSec` 默认：1200s

### 📋 修改文件清单

| 文件 | 变更 |
|------|------|
| `Agent/Agent.cs` | Stop hook + WorkReporter + CompressWithSmallModel 进度回调 |
| `Agent/ContextManager.cs` | CompressProgress 事件 + Snip 4000→4000 字符 + 错误行保留 + ProgressBar |
| `Agent/LLM.cs` | 渐进超时 + 任务花费追踪 + CallWithRetryAsync 重构 |
| `Agent/SystemPrompt.cs` | `<systematic_phases>` 10 阶段流水线 + MemoryRetrieval 整合 |
| `Arguments/BuiltinArgs.cs` | 新增 11 个 `slot-prompt-N` 参数 + `prompt-all` |
| `Arguments/CliArg.cs` | `GetAll` 静态方法 |
| `Arguments/CliArgRegistry.cs` | `GetAll` 多值获取 |
| `Commands/StatsCommand.cs` | 任务花费显示 |
| `Config/Config.cs` | MaxRetries 3→5 + TimeoutSec max 900→3600 + Watch 配置注册 |
| `Config/Global.cs` | v0.33.1 → v0.34.0 |
| `Infra/HooksManager.cs` | 全面重构：8 事件 + JSON 协议 + 匹配器 + 并发安全队列 |
| `Program.cs` | 槽位任务队列 + Bracketed Paste/Kitty 键盘启用 |
| `SelfTest.cs` | +23 项测试（系统化流水线 + 渐进超时 + 花费追踪 + 配置默认值） |
| `Terminal/AnsiTty.cs` | EnableBracketedPaste + EnableKittyKeyboard |
| `Terminal/Terminal.cs` | BracketedPaste + KittyKeyboard 封装 |
| `Tools/ToolRegistry.cs` | 注册 ExportTool + StructTodoTool |
| `TuiDemo.cs` | 动态栏演示 |
| `UI/TuiBase/BoxBuffer.cs` | 清理冗余代码 |
| `UI/TuiBase/InputManager.cs` | Bracketed Paste + Kitty Keyboard + CSI 统一解析 |
| `UI/TuiScreens/ChatScreen.cs` | 动态栏集成 + 状态同步 + CompressProgress 订阅 |

### 🧪 测试

- 1407 项自测全部通过（+23 项新增）

---

## v0.33.1 (2026-08-11) — 对话框刷新修复 + CSI 功能键解析 + TuiDemo 重构

### 🐛 对话框关闭 → 背景刷新修复

三处联动修复，解决关闭模态窗口后背景残留窗口残影的问题：

- **TuiManager `_needsFullRefresh`**：`RequestFullRefresh()` 设置标志，`Render()` 检测该标志时跳过增量渲染、发送 `ClearScreen` ANSI
- **TuiScreen `MarkDirtyInRect`**：关闭窗口时递归标记被遮挡区域的控件为脏，正确处理滚动容器坐标偏移
- **TuiView/TuiListView `EffectiveScrollOffset`**：虚拟属性抽象，`MarkDirtyInRect` 用于计算滚动容器中子控件的真实屏幕坐标

### ⌨️ CSI 功能键解析器（InputManager）

- **`TryParseCsiFunctionKey(char firstChar)`**：读取完整 CSI 参数串（`\x1b[num;modP/~`），委托给解析方法
- **`ParseCsiFuncKey(string, char)`**：解析 xterm 修饰键编码（2=Shift, 3=Alt, 4=Shift+Alt, 5=Ctrl, 6=Ctrl+Shift, 7=Ctrl+Alt, 8=Ctrl+Shift+Alt），支持 `P` 终止符（F1-F4）和 `~` 终止符（F5-F12 + 旧编码 F1-F24）
- 非 SGR 鼠标的 CSI 序列现在先尝试解析为功能键，失败后才吞掉序列
- **注**：macOS 终端（Terminal.app / iTerm2）默认不发送 `Shift+Fx` 序列，此功能为支持这些序列的终端提供正确解析

### 🎮 TuiDemo 重构

- **Slash 命令**：6 个全屏对话框改为 `/m /s /r /c /f /b` 输入框命令触发，跨平台兼容
- **PendingSubmissions 消费循环**：主循环新增 `TryDequeue` 逻辑，修复 Enter 提交的消息从未被 `OnSubmit` 处理的 bug
- **欢迎消息**：更新为 Slash 命令说明 + 全屏对话框列表

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiBase/TuiManager.cs` | 修改：`_needsFullRefresh` + `RequestFullRefresh()` 公共方法 |
| `UI/TuiBase/TuiScreen.cs` | 修改：`CloseWindow` dirty rects + `MarkDirtyInRect` 递归标记 |
| `UI/TuiBase/TuiView.cs` | 修改：`EffectiveScrollOffset` 虚拟属性 (default 0) |
| `UI/TuiBase/InputManager.cs` | 修改：`TryParseCsiFunctionKey` + `ParseCsiFuncKey` + `ConsumeCsi(char)` |
| `UI/TuiControls/TuiListView.cs` | 修改：`EffectiveScrollOffset` override |
| `UI/TuiControls/TuiDialog.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/ModelPicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/SessionPicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/ReasoningPicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/CommandPalette.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/FilePicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `TuiDemo.cs` | 重构：Slash 命令 + PendingSubmissions 消费 + 欢迎消息更新 |
| `Config/Global.cs` | v0.33.0 → v0.33.1 |

### 🧪 测试
- 编译 0 错误

---

## v0.33.0 (2026-08-11) — 鼠标全面修复 + 推理深度 + 会话管理

### 🖱️ 鼠标全面修复（对标 Crush 鼠标系统）

- **启用鼠标追踪**：取消 `Tty.EnableMouse()` 的注释，终端可正常上报 SGR 鼠标事件
- **主循环路由**：鼠标事件不再被吞掉，通过 `TuiManager → TuiScreen → 控件树` 正常路由
- **窗口内控件路由**：`TuiWindow.HandleMouse` 新增子控件点击/滚动/悬停路由
- **事件冒泡**：`TuiView.HandleMouse` 支持从最深命中控件向上冒泡，确保父容器（如 TuiListView）可处理子控件未消费的事件
- **SGR 运动事件**：`InputEvent` 新增 `MouseMotion` 属性，解析 SGR 代码 35/36/39（鼠标移动追踪）
- **TuiListView 滚轮**：鼠标滚轮滚动列表（3 行/格），点击可选中列表项
- **TuiButton hover**：鼠标悬停自动高亮按钮（蓝底白字）

### 🧠 推理深度选择器（对标 Crush reasoning.go）

- **`ReasoningPicker`**：全屏 ANSI 对话框，5 级推理深度（Minimal / Low / Medium / High / Max）
- **实时搜索** / **当前级别 ✓ 标记** / **清除恢复默认**
- **`Config.ReasoningEffort`**：新增配置属性 + `WAYCODER_REASONING_EFFORT` 环境变量
- **`LLM.cs`**：请求体中自动携带 `reasoning_effort` 参数（DeepSeek V4 / OpenAI o-series）
- **快捷键**：`Ctrl+G` 打开推理深度选择器

### 📂 会话管理器（对标 Crush sessions.go）

- **`SessionPicker`**：全屏 ANSI 对话框，浏览 / 切换 / 重命名 / 删除历史会话
- **三种模式**：Normal（选择）→ Renaming（内联重命名）→ Deleting（确认删除）
- **实时搜索** / **当前会话 ✓ 标记** / **相对时间显示**
- **`SessionManager.RenameSession`**：新增重命名方法（更新 JSON 内 id + 文件重命名）
- **`SessionManager.CreateNewSessionId`**：公开的 ID 生成方法
- **快捷键**：`Ctrl+S` 打开会话管理器

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/ReasoningPicker.cs` | 新增：推理深度选择对话框 |
| `UI/TuiCust/SessionPicker.cs` | 新增：会话管理对话框 |
| `Config/Config.cs` | 修改：新增 ReasoningEffort 属性 + Schema 项 |
| `Agent/LLM.cs` | 修改：请求体添加 reasoning_effort 参数 |
| `Memory/SessionManager.cs` | 修改：新增 RenameSession / CreateNewSessionId |
| `UI/TuiBase/InputManager.cs` | 修改：启用鼠标追踪 + MouseMotion 解析 |
| `UI/TuiBase/TuiManager.cs` | 修改：Enter() 中启用鼠标 |
| `UI/TuiBase/TuiView.cs` | 修改：HandleMouse 新增事件冒泡 |
| `UI/TuiBase/TuiWindow.cs` | 修改：HandleMouse 新增子控件路由 + 滚轮处理 |
| `UI/TuiControls/TuiListView.cs` | 修改：新增鼠标滚轮/点击处理 |
| `UI/TuiControls/TuiButton.cs` | 修改：新增鼠标悬停高亮 |
| `UI/TuiScreens/ChatScreen.cs` | 修改：新增 OnOpenSessions / OnReasoningEffort 回调 + Ctrl+S/Ctrl+G |
| `Program.cs` | 修改：启用鼠标路由 + 新对话框回调 |
| `Config/Global.cs` | v0.32.2 → v0.33.0 |

### 🧪 测试
- 1348 项自测全部通过

---

## v0.32.2 (2026-08-11) — 权限/输入/聊天 三大对标

### 🛡️ 权限行内渲染（对标 Crush inline permission）

- **`InlinePermission`**：在聊天流中直接嵌入 3 行黄色背景交互确认块，无需弹模态窗口
- **Y/N/A/D 快捷键**：Y=允许 N=拒绝 A=全允 D=展开详情，直接在消息列表中响应
- **工具参数着色**：bash 命令绿色高亮、write_file/edit_file 路径青色高亮
- **展开/折叠详情**：按 D 展开完整参数，再次按 D 折叠
- **已解决状态**：确认后自动变为灰色决议标记（✅/❌）
- `PermissionManager.ShowConfirmDialog` 更新：调用新的 `ChatScreen.ShowInlinePermission(toolName, summary, detail, isDangerous)`
- `TestCommand` 权限演示适配新接口

### ✏️ TuiDialog 多行输入升级（对标 Crush textarea）

- **`TuiDialog.Input()`** 从单行 `TuiInput` 升级为 3 行高 `TuiTextArea`，支持 Ctrl+Enter 硬换行
- **输入历史**：新建 `TuiInputHistory` 静态类，按字段名记录最近 50 条输入
- **自动预填**：对话框打开时自动填充该字段最近一次输入
- **AOT 安全持久化**：简单文本格式 `field|value` 保存到 `~/.waycoder/input_history.txt`
- **全局裁剪**：500 条全局上限 + 每字段 50 条上限 + 自动去重

### 💬 聊天输入增强

- **输入历史持久化**：`ChatScreen` Enter 发送时保存到 `TuiInputHistory`，重启后恢复
- **粘贴确认**：`ChatScreen.PasteAsync()` 超长(>500字符)或多行(>3行)弹出确认对话框
- **粘贴确认（旧）**：`TuiChatInput.Paste()` 同样增加 Y/N 确认
- **斜杠命令补全**：`BuildDefaultHints` 增加 `/model set`/`/model list`/`/model import`/`/perm ask`/`/perm auto` 等子命令提示

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/InlinePermission.cs` | 新增：行内权限确认控件 |
| `UI/TuiInputHistory.cs` | 新增：输入历史管理器 + 持久化 |
| `Skills/PermissionManager.cs` | 修改：ShowConfirmDialog 使用新 InlinePermission |
| `UI/TuiScreens/ChatScreen.cs` | 修改：ShowInlinePermission 内嵌控件/粘贴确认/历史持久化/子命令提示 |
| `UI/TuiControls/TuiDialog.cs` | 修改：Input() 升级为 TuiTextArea 多行 + 历史预填 |
| `UI/TuiCust/TuiChatInput.cs` | 修改：Paste() 增加 Y/N 确认 |
| `Commands/TestCommand.cs` | 修改：权限演示适配新接口 |
| `Config/Global.cs` | v0.32.1 → v0.32.2 |

### 🧪 测试
- 1348 项自测全部通过

---

## v0.32.1 (2026-08-11) — 补齐 UI 控件与对话框

### 🎨 模型选择对话框（对标 Crush models.go）

- **`ModelPicker.Show()`**：全屏 ANSI 直写模式，21+ 模型按供应商分组（DeepSeek/OpenAI/Anthropic/Google/Qwen/Zhipu）
- **Tab 切换大/小模型**：标题栏实时显示当前选择类型，Tab 键切换并重置搜索
- **实时搜索过滤**：输入即过滤，按模型名/ID/供应商名匹配
- **键盘导航**：↑↓ 导航、Enter 确认、Esc 取消、Home/End/PgUp/PgDn
- **当前使用标记**：当前正在使用的模型带 ✓ 标记，自动定位到当前选择
- **Ctrl+M 接入**：由原来的 4 模型轮换改为打开 ModelPicker 对话框
- **Settings 接入**：设置页面的模型下拉打开 ModelPicker 而非基础 TuiList

### 🕹️ 按钮控件增强（对标 Crush button.go）

- **`TuiButton` 增强**：新增 `UnderlineIndex` 快捷键下划线、`IsSelected` 选中高亮、`IsHovered` 悬停状态、`MinWidth` 最小宽度
- **`TuiButtonGroup`**：按钮组容器，水平/垂直布局、Tab/Shift+Tab 切换、方向键导航、字母快捷键识别
- **`TuiButtonGroup.AddRange()`**：批量添加按钮，自动检测大写字母为快捷键

### 📜 TuiScrollbar 独立滚动条（对标 Crush scrollbar.go）

- **`TuiScrollbar`**：独立垂直滚动条，bar/dot/block 三种样式
- **滑块拖拽**：鼠标拖拽滑块精确定位，`OffsetFromMouse()` 坐标换算
- **鼠标滚轮**：`HandleMouse(InputEvent)` 支持 ScrollUp/ScrollDown
- **自动隐藏**：`AutoHide=true` 时内容无需滚动自动隐藏
- **键盘支持**：PgUp/PgDn/Home/End 控制滚动

### 📁 文件选择对话框（对标 Crush filepicker）

- **`FilePicker.Show()`**：全屏 ANSI 直写，目录浏览 + 文件选择
- **目录导航**：Enter 进入子目录、Backspace 返回上级、".." 快捷项
- **文件信息**：显示 📁/📄 图标、文件大小(K/M/G)、修改时间(MM-dd HH:mm)
- **搜索过滤**：输入文件名过滤，大小写不敏感
- **多级排序**：目录优先 → ".." 最前 → 字母序

### 🔍 命令面板（对标 Crush command palette）

- **`CommandPalette.Show()`**：通用命令面板框架，全屏 ANSI 直写
- **分类分组**：命令按 Category 分组，蓝色粗体类别标题分隔
- **模糊搜索**：搜索标签/类别/描述/快捷键，实时显示匹配数/总数
- **快捷键显示**：每个命令右侧黄色粗体显示快捷键

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/ModelPicker.cs` | 新增：模型选择对话框 |
| `UI/TuiControls/TuiButton.cs` | 增强：UnderlineIndex/IsSelected/IsHovered/MinWidth |
| `UI/TuiControls/TuiButtonGroup.cs` | 新增：按钮组容器 |
| `UI/TuiControls/TuiScrollbar.cs` | 新增：独立滚动条 |
| `UI/TuiCust/FilePicker.cs` | 新增：文件选择对话框 |
| `UI/TuiCust/CommandPalette.cs` | 新增：命令面板 |
| `Program.cs` | 修改：Ctrl+M 接入 ModelPicker |
| `UI/TuiScreens/SettingsScreen.cs` | 修改：模型下拉接入 ModelPicker |
| `Config/Global.cs` | v0.32.0 → v0.32.1 |

### 🧪 测试
- 1348 项自测全部通过

---

## v0.32.0 (2026-08-11) — 对标 Crush TUI 四大模式

### 🔧 ToolRenderer 接口（对标 Crush ToolMessageItem）

- **`IToolRenderer`** 接口：每种工具类型独立渲染器，`FormatHeader` + `FormatOutput`
- **`ToolRendererFactory`**：按工具名分发（含 MCP 工具名解析），支持别名注册
- **6 个具体渲染器**：
  - `BashToolRenderer`：💻 图标 + 退出码着色（绿=成功/红=失败）+ stderr 红色标记
  - `EditToolRenderer`：✏️ 图标 + diff 着色（红底删除/绿底新增/青色 hunk 头）
  - `WriteToolRenderer`：📝 图标 + 成功绿色/错误红色
  - `AgentToolRenderer`：🤖 图标 + 深度标记蓝色 + 子任务分隔线黄色
  - `ReadFileToolRenderer`：📖 图标
  - `GlobGrepToolRenderer`：🔍 图标（glob + grep 共用）
- `ChatScreen.AddToolProgress` 集成：工具调用头自动使用对应 emoji 和格式

### 🪟 Dialog Overlay 栈（对标 Crush Overlay + typed Action）

- **`DialogOverlay`**：栈式对话框管理器，Push/Pop/Clear 操作
- **按 ID 管理**：同 ID 自动替换，支持嵌套对话框（确认→文件选择→权限）
- **类型化 Action 结果**：`DialogAction.Close` / `Confirm` / `Cancel` / `Select<T>` / `Permission` / `FilePicked` / `MultiSelect<T>` / `TextInput`
- Esc 自动关闭栈顶，焦点自动恢复
- 与现有 TuiScreen/TuiWindow 系统无缝兼容

### 📜 懒渲染列表（对标 Crush List + Item 接口）

- **`ILazyItem`** 接口：`MeasureHeight(width)` 预估高度 + `IsRenderCached` 缓存标记 + `InvalidateCache()`
- **`TuiMarkdown` 实现 `ILazyItem`**：已缓存时 O(1) 高度，未缓存时按字符折行估算
- **`TuiListView.FindFirstVisibleIndex()`**：二分查找 O(log n) 定位首个可见项
- **`OnRender` 优化**：从 firstVisibleIndex 开始遍历，`childScreenY >= screenBottom` 提前终止

### 💾 渲染缓存（对标 Crush cachedMessageItem）

- **已有实现**：`TuiMarkdown._parsed` + `_lastContent` + `_lastMaxWidth` 三级缓存
- `EnsureParsed()` 仅在内容或宽度变化时重新解析
- `ILazyItem.IsRenderCached` 对外暴露缓存状态

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/ToolRenderers/IToolRenderer.cs` | 新增：接口 + 工厂（含 MCP 支持） |
| `UI/TuiCust/ToolRenderers/BashToolRenderer.cs` | 新增：bash 输出着色 |
| `UI/TuiCust/ToolRenderers/EditToolRenderer.cs` | 新增：diff 红绿着色 |
| `UI/TuiCust/ToolRenderers/WriteToolRenderer.cs` | 新增：文件创建摘要 |
| `UI/TuiCust/ToolRenderers/AgentToolRenderer.cs` | 新增：子智能体状态 |
| `UI/TuiCust/ToolRenderers/ReadFileToolRenderer.cs` | 新增：read/glob/grep 渲染 |
| `UI/TuiCust/ToolRenderers/DefaultToolRenderer.cs` | 新增：默认直通 |
| `UI/TuiCust/DialogAction.cs` | 新增：类型化 Action 结果 |
| `UI/TuiCust/DialogOverlay.cs` | 新增：栈式叠层管理器 |
| `UI/TuiControls/ILazyItem.cs` | 新增：懒渲染项接口 |
| `UI/TuiControls/TuiMarkdown.cs` | 修改：实现 ILazyItem + MeasureHeight |
| `UI/TuiControls/TuiListView.cs` | 修改：二分查找首个可见项 + 提前终止 |
| `UI/TuiScreens/ChatScreen.cs` | 修改：AddToolProgress 使用 ToolRenderer |
| `Config/Global.cs` | v0.31.11 → v0.32.0 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.11 (2026-08-11) — Diff 语法高亮

### 🎨 Diff 语法高亮

- **Token 级语法高亮**：Diff 中的代码行按语言（14 种）进行 token 级着色
- 关键字（蓝）、字符串（绿）、数字（黄）、注释（灰）等在 diff 背景上正确显示
- **Unified 模式**：`-`/`+` 行的代码在红/绿背景上保留语法颜色
- **Split 模式**：左右面板代码各自独立语法高亮
- 上下文行在蓝底（当前 hunk）上也语法高亮
- `GetSyntaxForFile()` 根据文件扩展名自动选择语法定义
- `AppendHighlightedCode()` 将 Tokenize 结果渲染到 diff 背景色上

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/DiffPreview.cs` | GetSyntaxForFile + AppendHighlightedCode + unified/split 渲染改造 |
| `Config/Global.cs` | v0.31.10 → v0.31.11 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.10 (2026-08-11) — Diff 双模式（Unified/Split）

### 📊 Diff 双模式渲染

- **Split 侧边对照**：终端宽度 ≥ 120 时自动启用，删除（左红）↔ 新增（右绿）并排对照
- **Unified 统览**：窄屏自动回退传统 unified diff
- `BuildSplitRows()`：将 diff hunk 中的删除/新增行配对为 SplitRow（LeftText/RightText + LeftKind/RightKind）
- `RenderSplitRow()`：左面板 + ` │ ` 分隔符 + 右面板渲染，带行号偏移
- 滚动计算兼容两种模式（`totalVisualLines` = splitRows.Count 或 allLines.Count）

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/DiffPreview.cs` | SplitRow 类 + BuildSplitRows/RenderSplitRow + useSplitMode 自动切换 |
| `Config/Global.cs` | v0.31.9 → v0.31.10 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.8 (2026-08-11) — 工具白名单/黑名单 + 配置完善

### 🔒 工具白名单/黑名单

- `Config.AllowedTools`：逗号分隔白名单，仅允许列表中的工具可用（空=全部允许）
- `Config.DisabledTools`：逗号分隔黑名单，禁止列表中工具（空=不禁用）
- 环境变量：`WAYCODER_ALLOWED_TOOLS` / `WAYCODER_DISABLED_TOOLS`
- 过滤发生在 Agent 构造函数（`FilterTools` 方法），对 Agent 和子 Agent 均生效
- 过滤后通过 `DebugLog.Log("tool-filter", ...)` 记录工具数量变化

### 🔧 配置完善

- Settings UI 新增"工具白名单"和"工具黑名单"两个配置项（`🔒 安全` 分类）

| 文件 | 变更 |
|------|------|
| `Config/Config.cs` | 新增 AllowedTools / DisabledTools 属性 + Settings 注册 |
| `Agent/Agent.cs` | 新增 `FilterTools()` 方法 |
| `Config/Global.cs` | v0.31.7 → v0.31.8 |

## v0.31.8-v0.31.9 (2026-08-11) — 工具白名单/黑名单 + FileTracker 集成 + 测试覆盖

### 🔒 工具白名单/黑名单

- `Config.AllowedTools`：白名单（环境变量 `WAYCODER_ALLOWED_TOOLS`）
- `Config.DisabledTools`：黑名单（环境变量 `WAYCODER_DISABLED_TOOLS`）
- Agent 构造函数中 `FilterTools()` 过滤，主 Agent 和子 Agent 均生效

### 📁 Stale-Read 文件变更检测集成

- Agent 主循环集成 `FileTracker.GetChangeWarning()`（对标 Crush 的 stale-read 保护）
- 每轮工具执行后检查已读取文件是否被外部修改
- 检测到变更时注入 tool 消息警告 LLM 重新读取过期文件
- 通过 `DebugLog.Log("file-tracker", ...)` 记录

### 🧪 测试增强

- SystemPrompt 新增 9 项结构化区块检查（`critical_rules` / `workflow` / `editing_files` / `exact_matching` / `task_completion` / `error_handling` / `testing` / `code_conventions` / 15 条规则）

| 文件 | 变更 |
|------|------|
| `Agent/Agent.cs` | FileTracker 变更检测集成 + 工具过滤 `FilterTools()` |
| `Config/Config.cs` | AllowedTools / DisabledTools 属性 + Settings UI |
| `SelfTest.cs` | SystemPrompt 区块验证（9 项新检查） |
| `Config/Global.cs` | v0.31.7 → v0.31.9 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.7 (2026-08-11) — 对标 Crush：SystemPrompt 重写 + 循环检测 + 工具描述增强

### 🧠 SystemPrompt 重写（对标 Crush coder.md.tpl）

**从 107 行扩展到 ~350 行，15 个结构化 XML 区块**

- `<critical_rules>` — 15 条硬规则（先读后改 / 自主行动 / 每次修改后测试 / 极简输出 / 精确匹配）
- `<code_references>` — `file_path:line_number` 引用规范
- `<workflow>` — 行动前（搜索/读取/检查记忆）→ 行动中（编辑/测试/修复）→ 完成前（验证/对照需求/lint）
- `<decision_making>` — 自主决策原则 + 停止条件（能查到就不问 / 绝不因任务大而停下）
- `<editing_files>` — 编辑工具使用指南（edit_file / multi_edit / write_file 选择 + 7 步编辑流程 + 常见错误）
- `<exact_matching>` — 精确匹配避坑指南（空格 vs Tab / 花括号前空格 / 注释后空格 / 编辑失败修复流程）
- `<task_completion>` — 端到端完成检查清单（行动前思考 → 完整接线所有组件 → 逐项验证原始需求）
- `<error_handling>` — 错误恢复流程（读错误 → 隔离 → 3 种方案 → 修复 → 测试）
- `<testing>` — 测试规范（具体到宽泛 / 自我验证 / lint + 类型检查）
- `<tool_usage>` — 工具使用最佳实践 + bash 非交互命令优先
- `<code_conventions>` — 先读后写 / 匹配风格 / 野心 vs 精确
- `<proactiveness>` — 自主性平衡（被要求就做完 / 不描述直接做 / 被问"如何"只解释不实现）
- `<final_answers>` — 回复详细程度分级（默认 3 行 / 复杂任务 10-15 行 / 避免废话）

**技术修复**：使用无 `$` 前缀的 `"""` 原始字符串 + `.Replace()` 注入变量，避免代码示例中 `{` 花括号导致 C# 插值解析错误

### 🔄 SHA256 循环检测（Crush 风格）

- `Agent/Agent.cs` 新增 `DetectAndBreakLoop()` 方法
- 每轮对（assistant 消息 + 工具结果）做 SHA256 哈希，8 轮窗口内相同哈希出现 3+ 次触发
- 3 级递进式反循环提示（换方法 → 重新评估 → 严重警告重置）
- 触发后清空窗口给 Agent 几轮调整时间
- 通过 `DebugLog.Log("loop", ...)` 记录检测事件

### 📝 编辑工具描述增强

- `EditFileTool.Description` 重写：强调"先读后改"、"逐字符匹配（空格、Tab、换行）"、"3-5 行上下文确保唯一"
- `EditFileTool` 参数级描述增强：`old_string` 提示"从 read_file 输出精确复制，不要凭记忆或近似猜测"
- `WriteFileTool.Description` 重写：强调"仅用于新建或整体重写 / 局部编辑用 edit_file / 覆写前先 read_file"

### 🔧 Agent 架构更新

| 文件 | 变更 |
|------|------|
| `Agent/SystemPrompt.cs` | 完全重写，107→350 行，15 个结构化区块 |
| `Agent/Agent.cs` | 新增 SHA256 循环检测（`DetectAndBreakLoop`） |
| `Tools/EditFileTool.cs` | 描述 + 参数描述大幅增强 |
| `Tools/WriteFileTool.cs` | 描述 + 参数描述增强 |
| `Config/Global.cs` | v0.31.6 → v0.31.7 |

### 🧪 测试
- 1339 项自测全部通过

### 📄 文档读取增强

**PDF 文本提取**（PdfPig 库，开源 + AOT 兼容）
- `Infra/PdfExtractor.cs`：提取 PDF 纯文本，分页返回，压缩连续空行
- `ReadFileTool` 新增 `.pdf` 处理：自动调用 PdfExtractor，支持 `offset`（起始页）/ `limit`（最大页数）
- PDF 上限 50 MB，单次最多 20 页
- `FileIgnoreManager` 移除 `.pdf` 过滤（现在 PDF 可被 glob/grep 发现）

**Markdown 结构化渲染**
- `ReadFileTool` 新增 `.md` 处理：使用 `MarkdownParser` 解析 AST
- 输出结构化：标题层级 / 代码块（带语言标注）+ 80 行截断 / 表格（30 行截断） / 列表 / 段落
- Markdown 上限 500 KB

### 🤖 模型回退链增强

**跨供应商 API Key 自动解析**
- `FallbackLLM.ResolveKeyAndUrl()`：根据 ModelCatalog 供应商自动查找对应 API Key
- 14 个供应商映射：DEEPSEEK / OPENAI / GEMINI / ANTHROPIC / DASHSCOPE / ZHIPU / ARK / MOONSHOT / MISTRAL / XAI / SILICONFLOW / GROQ / TOGETHER / OPENROUTER
- 无 Key 时优雅跳过（不崩溃）+ 提示设置环境变量

**回退链新增免费模型**
- `gemini-2.0-flash`（Google 免费层 15 RPM）
- `qwen-turbo`（阿里超低价 $0.05/$0.15）
- `glm-4-flash`（智谱低价 $0.07/$0.14）
- 新链：`deepseek-v4-flash → deepseek-v4-pro → gemini-2.0-flash → qwen-turbo → glm-4-flash → gpt-5.4-mini`

### 🧪 测试脚本
- `scripts/` 目录 7 个脚本：bench-models.sh / bench-local.sh / bench-models.ps1 / bench-quick.bat / quick-test.sh / stress-test.sh / run-all-tests.sh
- 支持云端 + Ollama 本地模型一键基准测试

### 🗂️ 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Infra/PdfExtractor.cs` | PDF 文本提取器（PdfPig，AOT 兼容） |
| `Tools/ReadFileTool.cs` | 重构：PDF + Markdown + 文本三模式 |
| `Agent/FallbackLLM.cs` | 跨供应商 Key 解析 + 优雅跳过 |
| `Config/Config.cs` | 回退链 3→6 模型 + 默认 V4 Flash |
| `Infra/FileIgnoreManager.cs` | 移除 .pdf 过滤 |
| `Program.cs` | API Key 提示增加 Gemini/DashScope |
| `SelfTest.cs` | +10 测试（PDF/MD/回退链） |
| `WayCoder.csproj` | 新增 PdfPig NuGet 依赖 |
| `.gitignore` | 添加 games/ chess-test/ |

### 🧪 测试
- 1331 项自测全部通过

## v0.31.5 (2026-08-11) — DeepSeek V4 推理修复 + Crush 上下文管理 + 安全/追踪/诊断/本地模型

### 🧠 DeepSeek V4 推理内容修复（关键 Bug）
- **reasoning_content 污染对话历史**：DeepSeek V4 的 `reasoning_content` + Ollama/qwen 的 `reasoning` 字段不再存入 `contentParts`
- `TryGetReasoningText()` 统一处理两种字段名（`reasoning_content` / `reasoning`）
- 推理内容以暗色（«dim»）实时显示给用户，但不存入对话历史，不送入下一轮 API 调用
- tool_calls 到达时自动关闭暗色样式
- 流结束后安全关闭推理标记（防御性代码）
- **根因**：推理 token 计入 `max_tokens` 预算 → V4 消耗全部预算在推理上 → 零正式输出 → 修复后 Snake2（292 行）生成成功

### 🔄 Crush 风格上下文管理
- **真实 token 追踪**：`ContextManager.AddUsage()` 累积每次 API 返回的 prompt/completion tokens
- **自动摘要触发**：`ShouldStopAndSummarize()` — 大窗口（>200K）用 20K buffer，小窗口用 20% 比例
- **Auto-Continue**：摘要后自动注入继续提示（`ContinuePromptInjected`），防止 Agent 丢失任务上下文
- **自动续写增强**：检测"口述代码"（content >300 字符 + 代码标记）→ 追问使其写文件
- **首轮停滞检测**：模型首轮只分析不调用工具 → 自动追问执行
- 新增配置：`ContextWindowLargeThreshold=200K`、`ContextWindowLargeBuffer=20K`、`ContextWindowSmallRatio=0.2`、`AutoContinueAfterSummarize=true`

### 🛡️ Bash 命令安全系统（对标 crush bannedCommands + safeCommands）
- `BashGuard` 三层防护：禁止命令名拦截 + 参数级拦截 + 安全只读白名单
- 70+ 禁止命令（网络下载/系统修改/包管理器/网络配置）
- 47+ 安全只读命令自动放行（免权限确认）
- 参数级规则：阻止 `pip install`（允许 `--user`）、`npm install -g` 等
- 管道中每个命令独立检查（`|`, `;`, `&` 分割）

### 📊 文件追踪 / Stale-Read 检测（对标 crush filetracker）
- `FileTracker`：SHA256 哈希记录 + 外部变更检测 + LRU 淘汰（200 文件）
- 集成到 `ReadFileTool`/`WriteFileTool`/`EditFileTool`/`MultiEditTool`/`BashTool`

### 🔍 LSP 诊断自动附加（对标 crush diagnostics auto-attachment）
- `DiagnosticManager.FormatForLLM()` + `TryRunLintWithTimeout()`（3s 超时）
- 编辑/创建文件后自动附加 lint 错误/警告

### 🖥️ Ollama 本地模型支持
- 新增 6 个 Ollama 模型 + 通用 BaseUrl 自动解析 + 本地模型免 API Key
- **实测结论**：`qwen3.x:4b`（thinking 内置，全部 token 耗尽）→ 不可用；`qwen2.5-coder:1.5b`（无 thinking，快但 1.5B 太小无法工具调用）→ 需 7B+ 模型

### 📝 统一错误日志系统
- `ErrorLog` 四级日志 + 按天轮转 + 内存缓冲 + 全局异常捕获 + 噪音过滤

### ⚙️ 配置变更
- **默认模型**：`deepseek-v4-flash` → `deepseek-chat`（V4 推理有缺陷）
- **SmallModel**：同改为 `deepseek-chat`
- **MaxTokens**：4096 → 32768（推理模型需要更多预算）
- **LlmHttpTimeoutSec**：60 → 300（大窗口请求慢）
- **回退链**：`deepseek-chat,deepseek-v4-flash,deepseek-v4-pro,gpt-5.4-mini`
- **超时参数集中管理**：9 个超时配置项（BackgroundTask/AutoTest/Git/Kill/Download/Hook/AskUser/Regex/Fetch）
- **SystemPrompt 规则 10/11**：复杂任务先列 todo 清单，不要输出思考过程

### 🗂️ 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Agent/LLM.cs` | reasoning_content 显示不存 + TryGetReasoningText + Endpoint + ErrorLog |
| `Agent/Agent.cs` | token 追踪 + Crush 自动摘要 + 自动续写增强 + ErrorLog |
| `Agent/ContextManager.cs` | AddUsage() + ShouldStopAndSummarize() + ResetUsage() |
| `Agent/SystemPrompt.cs` | 规则 10（todo 清单）+ 规则 11（不输出思考过程） |
| `Agent/FallbackLLM.cs` | 回退链 ErrorLog + 模型更新 |
| `Agent/BackgroundTask.cs` | 输出缓冲区增强 + ErrorLog |
| `Infra/BashGuard.cs` | Bash 命令安全防护（三层拦截 + 安全白名单） |
| `Infra/FileTracker.cs` | 文件追踪器（SHA256 哈希 + 变更检测） |
| `Infra/ErrorLog.cs` | 统一错误日志系统（四级日志 + 自动轮转 + 全局异常） |
| `Config/Config.cs` | 默认模型/MaxTokens/超时 + ContextWindow 阈值 + 超时参数集中管理 |
| `Config/ModelCatalog.cs` | 新增 6 个 Ollama 模型 + deepseek-chat 优先 + BaseUrl 修正 |
| `Config/Global.cs` | 版本号 v0.31.5 |
| `Edit/DiagnosticManager.cs` | FormatForLLM() + TryRunLintWithTimeout() |
| `Program.cs` | ModelCatalog BaseUrl 解析 + 本地模型免 API Key + ErrorLog |
| `Tools/BashTool.cs` | BashGuard 集成 + FileTracker 变更警告 + ErrorLog |
| `Tools/EditFileTool.cs` | FileTracker + LSP 诊断自动附加 |
| `Tools/WriteFileTool.cs` | FileTracker + LSP 诊断自动附加 |
| `Tools/ReadFileTool.cs` | FileTracker + 缓存诊断附加 |
| `Tools/MultiEditTool.cs` | FileTracker + LSP 诊断自动附加 |
| `SelfTest.cs` | 测试更新（默认模型 deepseek-chat） |
| `.gitignore` | 添加 games/ chess-test/ |

### 🧪 测试
- 1321+ 项自测全部通过

## v0.31.4 (2026-08-11) — 竞品对标强化：Skills 升级 + 工具全面增强 + 温度/上下文默认调整

### ✨ 新增功能

**Agent Skills 标准升级**（对标 crush agentskills.io）
- SkillDef 新增字段：`License`、`Compatibility`、`Metadata`（key:value 字典）、`Builtin`
- 名称验证：正则 `/^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$/`，最大 64 字符，必须匹配目录名
- 新增内置 skill：`waycoder-config`（WayCoder 配置指南）
- 多发现路径：新增 `.cursor/skills/` 兼容
- 系统提示词注入格式：Markdown 列表 → `<available_skills>` XML 结构
- 技能加载追踪：`SkillsManager.LoadedSkills` + `MarkLoaded()`

**桌面通知**（对标 crush notify）
- `DesktopNotifier`：Agent 完成/权限等待时终端标题闪烁 + 响铃
- Windows Toast 通知支持（PowerShell 集成）
- 配置项：`WAYCODER_ENABLE_NOTIFICATIONS`（默认关闭）
- 集成点：`PermissionManager` 等待时 + Agent 轮次完成时

**文件忽略系统**（对标 crush gitignore/.crushignore）
- `FileIgnoreManager`：加载 `.gitignore` 和 `.waycoderignore` 规则
- 通用垃圾目录/文件扩展名自动忽略
- 集成到 `GlobTool` 和 `GrepTool` 自动过滤结果

### 🚀 工具增强

**EditFileTool**（对标 crush edit）
- 新增 `replace_all` 参数：一次替换所有匹配项
- CRLF 行尾保留：编辑后保持原始换行格式

**MultiEditTool** — 批量编辑工具（对标 crush multiedit）
- 参数：`file_path` + `edits: [{old_string, new_string, replace_all?}]`
- 首个编辑 old_string 为空 → 创建新文件
- CRLF 行尾保留

**ReadFileTool** 增强（对标 crush view）
- 文件不存在时 "Did you mean?" 建议（Levenshtein 编辑距离匹配）
- UTF-8 验证
- 图片文件识别与提示（jpg/png/gif/webp 等）
- 大文件保护（>100KB 拒绝，提示分段读取）
- 行号格式化（自适应宽度对齐）

**GrepTool** 增强（对标 crush grep）
- 新增 `literal_text` 参数：自动转义正则特殊字符
- FileIgnoreManager 过滤

**FetchTool** 升级（对标 crush fetch/web_fetch）
- HTML 净化：移除 script/style/nav/footer/header/aside 等噪音元素
- 多输出格式：`text`（纯文本）/ `markdown`（结构化）
- JSON 自动美化
- 真实浏览器 User-Agent（Chrome 131 Windows）

### 🔧 配置变更

**默认温度 0.0 → 0.1**
- `LLM.cs` 构造函数默认值 + `Config.Temperature` 默认值 + Schema 默认值
- 回退链自动继承温度

**MaxContextTokens 默认 128K → 1M**
- 匹配 DeepSeek V4 的 1M 上下文窗口

### 🗂️ 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Skills/SkillsManager.cs` | 完全重写：名称验证 + XML 格式 + 内置 skill + `.cursor/skills` |
| `Skills/builtin/waycoder-config/SKILL.md` | 内置 WayCoder 配置指南 skill |
| `Infra/FileIgnoreManager.cs` | 文件忽略规则管理器（.gitignore + .waycoderignore） |
| `Infra/DesktopNotifier.cs` | 桌面通知系统（终端闪烁 + 响铃 + Windows Toast） |
| `Tools/MultiEditTool.cs` | 批量编辑工具 |
| `Tools/EditFileTool.cs` | 新增 replace_all + CRLF 保留 |
| `Tools/ReadFileTool.cs` | 增强：文件建议 + 图片检测 + UTF-8 验证 + 大文件保护 |
| `Tools/GrepTool.cs` | 新增 literal_text 模式 + FileIgnoreManager |
| `Tools/FetchTool.cs` | HTML 净化 + Markdown 格式 + JSON 美化 |
| `Tools/GlobTool.cs` | FileIgnoreManager 集成 |
| `Config/Config.cs` | 新增 DesktopNotifications 配置项 |
| `Agent/SystemPrompt.cs` | Skills XML 注入格式 |

### 🧪 测试
- 1312 项自测全部通过

## v0.31.3 (2026-08-11) — 竞品对标：后台任务工具 + Download 工具 + Bash 后台增强

### ✨ 新功能

**后台任务工具**（对标 Crush 竞品分析）
- `JobOutputTool` (`job_output`) — LLM 可读取后台运行 bash 任务的输出，支持 timeout 等待
- `JobKillTool` (`job_kill`) — LLM 可终止指定的后台 bash 任务
- `BackgroundTask.Kill()` — 新增进程终止方法（`entireProcessTree: true`）

**Download 工具**
- `DownloadTool` (`download`) — HTTP GET 下载文件到本地
  - 安全检查：拒绝 `file:///`、仅允许 http/https、最大 500MB、自动创建父目录
  - 超时：默认 60s，最大 600s
  - 权限：需要确认（危险操作）

**Bash 后台运行增强**
- `BashTool` 新增参数：`run_in_background` (bool)、`auto_background_after` (int)
- 后台模式：启动 BackgroundTask → 返回 shell_id → LLM 后续可通过 job_output/job_kill 管理

### 🔧 权限分类
- `download` 加入 PermissionManager.DangerousTools 列表
- `job_output` 加入 AutoModeClassifier.SafeTools（只读操作）
- `job_kill` 加入 AutoModeClassifier.DangerousTools（破坏性操作）

### 🗂 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Tools/JobOutputTool.cs` | 后台任务输出读取工具 |
| `Tools/JobKillTool.cs` | 后台任务终止工具 |
| `Tools/DownloadTool.cs` | HTTP 下载工具 |
| `Agent/BackgroundTask.cs` | 新增 Kill 方法 + 增强输出缓冲 |
| `Tools/BashTool.cs` | 新增后台运行参数 |
| `Tools/ToolRegistry.cs` | 工具数量 33→36 |
| `Skills/AutoModeClassifier.cs` | 新工具风险分类 |
| `Skills/PermissionManager.cs` | download 确认规则 |
| `.gitignore` | 忽略 crush/ 和 test_game/ 参考目录 |

### 🧪 测试
- 1307 项自测全部通过

## v0.31.2 (2026-08-11) — TUI 增量渲染优化

### 🐛 修复

**输入框输入时全屏闪烁**
- `TuiManager.Render()`：增量帧不再 `ClearScreen`，仅首帧/切屏/Resize 时全屏清除
- `TuiControl.MarkDirty()`：不再向 Parent 链传播脏标记，避免 RootView 脏导致所有控件重绘
- `TuiView.OnRender()`：始终遍历子视图容器以查找脏后代，但只渲染脏的叶子控件
- 效果：输入框打字时仅刷新输入区域，聊天区/状态栏等保持不变，消除闪烁

## v0.31.1 (2026-08-11) — CLI 参数注册系统 + DeepSeek V4 上下文修正

### ✨ 新功能

**📋 CLI 参数注册系统**
- 新建 `Parameters/` 目录，结构化参数定义：
  - `CliArg` 抽象基类 — 统一 Key / Names / Description / ValueCount / OnMatch 元数据
  - `CliArgRegistry` 注册表 — Register 自动重复检测（长短名冲突立即报错）、Parse 解析引擎、HelpText 自动生成
  - `BuiltinArgs` — 18 个内置参数子类，每个 5-10 行继承基类
- Program.cs Main：删除 ~50 行手动 switch，改为 15 行注册 + 解析
- `--help` 输出从注册表自动生成，排除内部/开发参数
- 支持 `--key=value` 等号格式
- 大小写敏感：`-b` (base-url) 和 `-B` (max-budget) 不冲突

**🏷 全部参数长短双名**
- 7 个参数新增强短名：`-b` (`--base-url`), `-k` (`--api-key`), `-i` (`--init`), `-d` (`--debug`), `-y` (`--yolo`), `-B` (`--max-budget-usd`)
- `--benchmark`/`--perf` 新增 `--bench` 别名
- 18 个参数全部支持短名或别名

### 🐛 修复

**DeepSeek V4 上下文窗口修正**
- `deepseek-v4-pro` / `deepseek-v4-flash` 上下文窗口：128K → 1M (1,048,576 tokens)
- 模型列表 `/model list` 显示正确：`1M ctx`

### 🔄 重构

- CLI 参数从手写 switch 重构为结构化注册表
- `BuiltinArgs.RegisterAll()` 幂等注册，重复名称自动检测报错

### 🗂️ 新增文件
| 文件 | 功能 |
|------|------|
| `Parameters/CliArg.cs` | CLI 参数抽象基类 |
| `Parameters/CliArgRegistry.cs` | 注册表 + 解析引擎 + 帮助生成 |
| `Parameters/BuiltinArgs.cs` | 18 个内置参数定义 |

### 🧪 测试
- 1306 项自测全部通过

## v0.31.0 (2026-08-11) — 统一配置系统 + 多模型管理 + 槽位独立记忆

### ✨ 新功能

**⚙ 统一配置系统**
- Config 改为单例模式 (`Config.Instance`)，全局唯一实例，支持 `Reload()`
- 新增 19 个可配置属性，均绑定环境变量 + Schema 驱动设置界面
  - 沙箱：`SandboxMaxMemoryMb`、`SandboxMaxCpuSeconds`、`SandboxAllowNetwork`
  - Agent：`MaxRounds`、`SubAgentMaxParallel`、`SubAgentOutputMaxChars`
  - LLM：`LlmHttpTimeoutSec`、`LlmMaxRetries`、`LlmConnectionTimeoutSec`、`LlmRateLimitMaxWaitSec`
  - 回退：`FallbackChain`（逗号分隔模型列表）
  - 文件锁：`FileLockTimeoutSec`
  - 上下文压缩：`ContextSnipRatio`、`ContextSummarizeRatio`、`ContextCollapseRatio`
- 9 个模块全量重构，从硬编码/分散静态字段改为 `Config.Instance` 读取
  - `SandboxManager`、`AgentTool`、`Agent`、`LLM`、`FallbackLLM`、`ContextManager`、`FileLockManager`、`BashTool`、`WatchMode`
- 双环境变量兼容：`WAYCODER_*`（新）+ `CORECODER_*`（旧）

**🤖 多模型目录与槽位独立选模型**
- `ModelCatalog` — 内置 50+ 模型，覆盖 12+ 提供商（OpenAI、Anthropic、DeepSeek、Google、Qwen、智谱、豆包、Moonshot、Mistral、xAI、Ollama 等）
- 每模型含：上下文窗口、输入/输出价格、默认 API 地址、分类标签
- 外部配置导入：OpenCode / Crush / Cline / Continue JSON 格式一键导入
- `ApiKeyStore` — 多模型 API Key 持久化（`~/.waycoder/api_keys.json`），按提供商独立存储，跨会话保留
- `AgentSlotConfig` — 10 槽位 (F1-F10) 各自独立选择大小模型
  - 统一模式：一键设置所有槽位为同一模型
  - 模型继承：未设置槽位默认使用 F1 的模型
  - API Key 三级优先：槽位直设 → ApiKeyStore → 全局 Config.ApiKey
  - BaseUrl 三级优先：槽位 → 模型默认 → 全局 Config.BaseUrl

**📟 `/model` 命令重写**
- 子命令：`list`（浏览目录）、`set`（当前槽位）、`uniform`（全槽位统一）、`import`（外部导入）、`keys`（管理 API Key）、`slot`（指定槽位）
- `#N` 快捷语法：`/model #1 deepseek-v4-pro sk-xxx` 一键配置
- 本地模型检测：`localhost:port` 自动转换为 `http://localhost:port/v1` BaseUrl，无需 API Key
- 向后兼容：`/model <id>` 快速切换大模型

**🧠 槽位独立记忆系统**
- 每个 Agent 槽位 (F1-F10) 独立记忆存储目录（`.waycoder/memory/slot_N/`）
- 共享记忆目录（`.waycoder/memory/`）所有槽位可见
- 切换槽位时记忆空间自动切换
- `StructuredMemory` 读写操作自动路由到当前槽位
- `ListAll` / `Search` / `GetRelevantContext` 合并共享 + 槽位独立记忆
- `Count` 合并计数（排除 MEMORY.md 索引文件）

### 🔄 重构

- `Config.FromEnv()` 调用点全部改为 `Config.Instance`（`BashTool` 4 处、`WatchMode` 2 处、`LintTool` 3 处、`WriteFileTool`、`EditFileTool`、`ChatScreen`、`SettingsScreen`、`TuiManager`、`SystemPrompt`）
- `SandboxManager` 静态字段改为 Config-backed getter（保留 setter 供测试）
- `AgentTool.MaxParallelTasks` 从 `const` 改为 Config 读取
- `FileLockManager.DefaultTimeout` 从硬编码改为 Config 读取
- `LLM` 超时/重试/连接超时全部从 Config 读取
- `FallbackLLM.FallbackChain` 从 Config 逗号分隔解析
- `ContextManager` 压缩比从 Config 读取
- `StructuredMemory` 目录结构重构为共享 + 槽位双层

### 🗂️ 新增文件
| 文件 | 功能 |
|------|------|
| `Config/ModelCatalog.cs` | 模型目录（50+ 内置 + 导入引擎 + 搜索） |
| `Config/ApiKeyStore.cs` | 多模型 API Key 持久化 |
| `Config/AgentSlotConfig.cs` | 10 槽位独立模型配置 + 统一模式 |

### 🧪 测试
- 1306 项自测全部通过

## v0.30.2 (2026-08-11) — Bash 流式输出增强 + 上限报告同步

### ✨ 新功能

**📟 Bash stderr 流式输出**
- 原仅 stdout 逐行流式，stderr 全缓冲到退出后一次性追加
- 现 stdout/stderr 并行异步逐行读取，stderr 行带 `[stderr]` 前缀
- UI 中通过 `IsErrorOutput` 自动标红错误行
- 管道模式（非 TUI）也新增 `onToolOutput` 回调，逐行输出到控制台

**📊 上限报告同步更新 (55→60 项)**
- 新增 5 项：TuiTextArea 最大行数、自动换行列宽、TuiEditBase 撤销栈、Tab 键行为标志、Bash 流式 stderr

## v0.30.1 (2026-08-11) — TuiEditBase 键盘引擎 + TuiTextArea 自动换行

### ✨ 新功能

**⌨ TuiTextArea 自动换行与行数限制**
- `MaxColumnWidth` 属性：文字自动折行宽度（按空格智能断词），可视区 `Width` 可小于此值实现水平滚动
- `MaxLines` 属性：最大行数限制，超出时从顶部自动裁剪旧行，同步调整光标和滚动偏移

### 🔄 重构

**🏗 TuiEditBase 统一键盘分发引擎**
- 基类新增 18 个抽象编辑原语（光标移动、文本编辑、选择管理、撤销重做、粘贴）
- 基类实现完整键盘分发：`OnKey` → `HandleCtrlKey` / `HandleShiftKey` / `HandleRegularKey`
- 子类只需实现数据模型相关的底层原语（每方法 1-5 行），不再需要重复编写键盘处理
- TuiInput：删除 ~160 行 OnKey，新增 12 个原语实现
- TuiTextArea：删除 ~175 行 OnKey + HandleCtrlKey，新增 18 个原语实现 + 多行光标移动
- TuiRichEditor：保留独立的 OnKey 覆写（委托 EditorCore），新增糖衣原语适配基类
- 消除 ~335 行重复代码

**🔀 按键冲突消除**
- `AcceptsTab` 虚属性：默认 `false`（Tab 切换焦点），TuiRichEditor 覆写为 `true`（Tab 输入缩进）
- `InsertNewLine()` 虚方法：默认提交（OnSubmit），TuiTextArea/TuiRichEditor 覆写为实际换行
- 基类 Tab 分发：`AcceptsTab ? InsertChar('\t') : return false`（交父容器切换焦点）

**🧪 自测**
- 1306 项自测全部通过（新增 6 项：MaxColumnWidth 折行、MaxLines 裁剪、Tab/Enter 按键差异）

### ✨ 新功能

**📊 系统上限报告 (`--limits`)**
- 扫描全代码库 55 项系统上限，分为 6 大类别：
  - 🤖 智能体（8 项）— 槽位数量、子Agent深度、并行上限、轮次/预算限制
  - 📨 上下文（8 项）— 三层压缩阈值、token 估算公式、会话消息列表
  - 📁 工具/文件（14 项）— Bash 截断、危险命令阻止、超时、各工具结果上限
  - 🔒 沙箱/资源（6 项）— 内存、CPU、网络、隔离深度
  - 🖥 TUI/编辑器（9 项）— 聊天消息、撤销历史、diff 预览、代码块宽度
  - ⚙ 配置/杂项（10 项）— 记忆注入、会话列表、commit 限制
- 每项标注：🔴硬阻断 / 🟡降级 / 🟢优雅 / ⚪无限制
- **⚙可配 vs 🔒硬编 区分**：标注哪些上限可在设置界面修改（8 项可配 + 环境变量名 + 设置路径）
- 输出包含源码位置（文件名:行号），方便代码审查和性能调优
- 发现 2 个潜在问题：`AgentTool.MaxDepth` 与 `Config.SubAgentMaxDepth` 不同步、沙箱 CPU 限制未实施

**💬 聊天显示风格设置**
- 三种显示模式：`detailed`（全显示）、`auto`（智能折叠 20 行）、`concise`（极简一行）
- 环境变量 `WAYCODER_CHAT_STYLE`，设置界面 → 🎨 界面 → 聊天显示风格
- 实时生效，保存设置后同步更新

### 🔄 重构 / 改进

**📝 Markup 标记符号重构**
- 清除 Spectre.Console 方括号 `[color]text[/]` 与中文 `[]` 的冲突
- 改用法语书名号 `«color»text«/»` 作为标记符号（U+00AB/00BB）
- 中文方括号不再需要双写 `[[选项]]`，直接写 `[选项]` 即可
- 涉及：`SpectreToAnsi` 解析器、37 处 `MarkupLine` 调用、`TuiHelper.Esc/StripMarkup`
- 同步更新 `CheckpointManager`、`FindReplaceTool`、`SelfTest`

**📊 性能测评新增 10K 聊天压力测试**
- 10000 条混合角色聊天消息的创建、渲染、滚动性能
- ChatMsg 创建 9ms、TuiListItem 解析 74ms、布局 258ms、滚动 <1ms

### 🐛 修复
- `AgentTool.MaxDepth` 与 `Config.SubAgentMaxDepth` 不同步问题已记录（上限报告中）

## v0.26.0 (2026-08-11) — 智能工作模式 + 跨槽位协作 + 光标修复

### ✨ 新功能

**🤖 智能 Auto Mode 分类器**
- 三级风险分级引擎：Safe（只读自动放行）→ Cautious（首次确认后记住）→ Dangerous（每次确认）
- 连续 3 次拒绝危险操作 → 自动退回 Ask 手动模式，防止误操作疲劳
- `/auto` 一键切换，别名 `/自动`

**🔨 工作模式系统（Shift+Tab 切换）**
- 四种模式：🔨Build 建造 / 🧠Plan 计划 / 🔍Review 审查 / 🤖Auto 自动
- 每个 Agent 槽位独立记忆工作模式
- **Plan 模式**：封锁 write/edit/bash/rm/git/agent，System Prompt 注入分析引导
- **Review 模式**：只读 + agent 可用，封锁写工具
- **Auto 模式**：全工具 + SmartAuto 分级确认
- 快捷键 **Shift+Tab** 循环切换，`/mode` 命令查看/直达
- 状态栏显示当前模式 emoji

**📦 自动 Git Commit 增强**
- `/autocommit` 开关命令，别名 `/自动提交`
- 精准暂存：只 `git add` AI 实际修改的文件（通过 `write_file`/`edit_file` 追踪）
- 提交正文含 `git diff --stat` 摘要
- 用户可见反馈：提交后在聊天区显示 `📦 自动提交 [N 文件]: msg`

**📨 跨槽位消息传递**
- `/send <槽位号> <消息>` —— 向其他 Agent 槽位发送消息
- `/broadcast <消息>` —— 向所有其他槽位广播
- 非活跃槽位的消息自动排队，切换回该槽位时投递
- 别名：`/发送` `/广播` `/to` `/bc`

**🧠 Architect 双模型模式**
- `/architect` 开关，大模型出计划 + 小模型执行
- 大模型不带工具，纯分析输出结构化计划

**📥 配置导入**
- `/import` 一键导入 Claude Code / OpenCode / Cursor / Cline 配置
- 支持：模型/API 配置、MCP 服务器、项目上下文、会话数据

**🗺️ Repository Map 升级**
- 新增 16 语言 import/include 引用图分析
- PageRank 风格核心文件评分 + ⭐ 标记

**💬 AskUserQuestion 用户交互工具**
- LLM 可主动弹窗向用户提问：单选（多选一）、多选（多选多）、文本输入
- 每次可问 1-4 个问题，依次模态弹窗，阻塞 Agent 等待响应
- 支持选项描述文本（如 `"React — Popular UI library"`）
- 非 TUI 模式自动回退到 Console I/O
- 分类为 Safe 工具，SmartAuto 下自动放行无需确认
- `UxHelper.RenderWait` 重构为公开方法，timeout 可配置，供工具层复用

### 🖥️ TUI 改进
- **状态栏心跳动画**：⣾⣽⣻⢿⡿⣟⣯⣷ braille 旋转，证明 UI 渲染循环存活
- **光标定位修复**：`EnsureCursorPosition()` 后备机制，增量渲染模式下光标位置不丢失
- **Shift+Tab 终端序列**：`\x1b[Z` → `InputType.ShiftTab`，模式切换
- **ProgramContext.Agent 同步**：槽位切换时自动更新，所有命令可用

### 🔄 重构
- `PermissionManager` 重构：提取 `ShowConfirmDialog()`，新增 `SmartAuto` 模式
- `SandboxManager` 新增 `smart-auto` 级别映射 + `IsSmartAuto` 属性
- `BackgroundTask` 完全异步化：`WaitForExit` → `WaitForExitAsync`，消除同步阻塞
- `AgentSlot` 新增：`WorkMode`、`PendingMessages`、`DeliverMessage()`、`FlushPendingMessages()`

### 🧪 测试
- 新增 95 项测试（总计 1299 项，从 1199 增长），全部通过
- 覆盖：AutoMode 分类器、工作模式约束/切换/事件、AutoCommit 属性/校验/清洗/EscArg、跨槽位投递/排队、光标状态、AskUserQuestion 工具 Schema/注册/安全分类

### 🗂️ 新增文件
| 文件 | 功能 |
|------|------|
| `Skills/AutoModeClassifier.cs` | 三级风险分类引擎 |
| `Agent/WorkModeManager.cs` | 四模式定义 + 约束 + Prompt |
| `Commands/AutoCommand.cs` | `/auto` 切换命令 |
| `Commands/ModeCommand.cs` | `/mode` 模式命令 |
| `Commands/AutoCommitCommand.cs` | `/autocommit` 开关 |
| `Commands/SendCommand.cs` | `/send` + `/broadcast` |
| `Commands/ArchitectCommand.cs` | `/architect` 双模型 |
| `Commands/ImportCommand.cs` | `/import` 导入 |
| `Infra/ImportHelper.cs` | 四源导入引擎 (~750行) |
| `Tools/AskUserQuestionTool.cs` | LLM 用户交互工具 (~360行) |

## v0.25.9 (2026-08-10) — TUI 控件完善 + 测试全覆盖 + Bug 修复

### ✨ 新功能
- **TuiDialog.Secret()**: 新增密码输入对话框，支持掩码回显
- **UxHelper.Secret()**: 新增密码输入辅助方法，TUI/控制台双模式适配

### 🧪 测试
- **29 个 TUI 组件测试全覆盖**: TuiButton, TuiCheckbox, TuiInput, TuiTextArea, TuiLabel, TuiIcon, TuiList, TuiListView, TuiProgress, TuiSpinner, TuiStatusBar, TuiTabs, TuiTitleBar, TuiBanner, TuiGrid, TuiWrapPanel, TuiSidePanel, TuiPromptBar, TuiDialog (11 工厂方法), TuiControl, TuiView, TuiScreen, BoxBuffer, TuiColors, TuiTheme, MarkdownRenderer, TuiTable, DiffPreview, UxHelper
- 新增 362 项测试（总计 1199 项），全部通过
- `/test ui` 可一键运行所有 UI 组件测试

### 🐛 Bug 修复
- **侧边栏背景色修复**: TuiSidePanel 默认背景从 WindowBg 改为 TerminalBg，与聊天区黑色背景统一
- **开机 Logo 间距**: 上方增加 3 行空白，视觉居中对齐
- **彩虹色偏移修复**: 横幅彩虹渐变基于视觉行号（非空行）而非绝对行索引，避免空白行消耗颜色锚点

### 🔄 重构
- Program.cs 中 `TuiInput.ReadInput()` → `TuiChatInput.ReadInput()`，统一输入入口

## v0.25.8 (2026-08-10) — 去 CoreCoder 品牌化

### 🔄 品牌重命名
为避免商标侵权，代码库中彻底移除 "CoreCoder" 品牌名称：

- **命名空间重命名**: `CoreCoderSharp` → `WayCoder`（~177 个 .cs 文件 + 项目文件 + CI）
  - `.csproj` `<RootNamespace>` / `<AssemblyName>` → `WayCoder` / `waycoder`
  - `.sln` 项目名和路径同步更新
  - CI workflows 路径更新
- **配置目录迁移**: `.corecoder/` → `.waycoder/`（~16 个文件）
  - 写操作一律用 `.waycoder/`，读操作先试新目录回退旧目录
  - `Global.cs` 新增集中式辅助方法: `WriteConfigPath` / `ReadConfigPath` / `GlobalConfigPath` / `ConfigDirSearchOrder`
  - 环境变量: 新增 `WAYCODER_*` 前缀，保留 `CORECODER_*` 兼容回退
  - 涉及文件: StructuredMemory, SessionManager, CheckpointManager, CustomCommands, ThemeConfig, Config, Program, WatchMode, SharedMemoryManager, ProjectContext, HooksManager, McpCache, McpClient, ExportCommand, MemoryTool, SkillsManager, SelfTest
- **注释与文档去 CoreCoder 化**:
  - 代码注释: Agent.cs, ContextManager.cs, SessionManager.cs, CheckpointManager.cs
  - 文档: CLAUDE.md, README.md, AGENTS.md, .gitignore
  - 二进制名: `corecoder.exe` → `waycoder.exe`（输出文件）

### ⚠️ 破坏性变更
- 新配置写入 `.waycoder/`，首次启动自动创建新目录
- 旧 `.corecoder/` 目录可安全保留或删除（读操作仍兼容）

### 🧪 测试
- 844 项自测全部通过，0 失败
- 编译 0 错误（仅 23 个预存在 nullability/AOT 警告）

## v0.25.7 (2026-08-10) — TuiBase 统一基类 + Bug 修复

### 🏗️ 架构重构
- **TuiBase.cs** (NEW): 统一 UI 元素基类，提炼所有界面通用属性和方法
  - 公共属性: `X`, `Y`, `Width`(默认10), `Height`(默认1), `Name`, `Tag`, `IsDirty`
  - 脏标记管线: `MarkDirty()`(virtual), `ClearDirty()`, `Invalidate()`
  - 生命周期: `OnCreate()`, `OnDestroy()` (virtual)
  - 输入路由: `OnKey(ConsoleKeyInfo)`(virtual→bool), `HandleMouse(InputEvent)`(virtual→bool)
  - 尺寸变化: `OnResize(int newW, int newH)`(virtual)
- **TuiControl** → 继承 TuiBase，移除重复属性，virtual → override
  - 保留 `MarkDirty()` Parent 传播逻辑
  - 保留 KeyHook 模式 + Render 管线 + 颜色系统
- **TuiWindow** → 继承 TuiBase，构造函数默认 30×10，移除重复 X/Y/W/H
- **TuiScreen** → 继承 TuiBase
  - Namespace 统一: `CoreCoderSharp.UI.TuiBase` → `CoreCoderSharp.UI`
  - `MarkDirty()` override 增强: 同步标记 Manager.IsDirty + RootView.IsDirty
- **TuiView** → 无需改动，继承链自动传递 (TuiView→TuiControl→TuiBase)
- 5 个调用方移除旧 `using CoreCoderSharp.UI.TuiBase;` 导入

### 🐛 Bug 修复
- **Bug #1**: `/plan` 命令 — PlanModeAsync 加 try-finally 包裹 Enter/Exit 备用屏
- **Bug #2**: `!` shell 命令 — RunShellOnceAsync 加 try-finally 包裹 Enter/Exit 备用屏
- **Bug #8**: Agent.AutoCommitAsync 空 `catch { }` → `catch (Exception ex) { DebugLog.Log(...) }`
- **Bug #9**: Terminal.ExitAltScreenDirect 缺少 `MouseDisable` → 退出前先禁用鼠标

### 🧪 测试
- 844 项自测全部通过，0 失败
- 编译 0 错误（仅 23 个预存在 nullability/AOT 警告）

## v0.25.6 (2026-08-10) — NotebookEdit 工具 + 工具总数 32

### 🚀 P3 新功能
- **NotebookEditTool.cs** (NEW): Jupyter Notebook (.ipynb) 编辑工具
  - `replace`: 替换指定 cell 的源代码（自动清理旧 outputs）
  - `insert`: 在指定位置后插入新 cell（支持 code/markdown/raw 类型）
  - `delete`: 删除指定 cell
  - 基于 cell 索引（0-based），兼容 .ipynb v4 格式
  - AOT 兼容: 手写 JSON 节点操作，零反射依赖
- **ToolRegistry**: 工具总数 31 → 32
- **PermissionManager**: notebook_edit 加入写工具确认列表
- **SelfTest**: 9 项 NotebookEdit 测试（replace/insert/delete/边界）

## v0.25.5 (2026-08-10) — 团队知识库共享

### 🚀 P3 新功能
- **SharedMemoryManager.cs** (NEW): 通过 git 同步 `.corecoder/memory/` 共享记忆
  - `IsGitRepo()`: 检测当前目录是否在 git 仓库中
  - `GetStatus()`: 获取本地共享记忆数 + 远程变更数 + 变更文件列表
  - `PullSharedAsync()`: 从远程拉取共享记忆（fetch + checkout 仅 memory 文件）
  - `PushSharedAsync()`: 推送本地共享记忆到远程（add + commit + push）
  - `ShareAsync()`: 标记记忆为共享 + 推送到远程
  - `Unshare()`: 取消记忆共享状态
  - 安全措施: 只操作 .corecoder/memory/*.md，不触碰其他 git 内容
- **StructuredMemory.cs**: 新增 `IsShared` / `SetShared()` / `ListShared()`
  - `shared: true` frontmatter 字段持久化
- **MemoryTool.cs**: 新增 `share` / `unshare` / `sync` 三个操作
  - `share`: 标记团队共享并推送
  - `unshare`: 取消共享
  - `sync`: 拉取（默认）或推送（sync push）远程共享记忆
- **Config.cs**: 新增 `TeamMemoryEnabled` + `TeamMemoryAutoSync` 配置项
  - 环境变量: `WAYCODER_TEAM_MEMORY` / `WAYCODER_TEAM_AUTO_SYNC`
- **Program.cs**: 启动时自动拉取远程共享记忆（需开启 TeamMemoryEnabled + TeamMemoryAutoSync）

## v0.25.4 (2026-08-10) — 核心 API 文档补全

### 📝 P2 改进
- **LLM.cs**: 8 个公开属性 + 2 个核心方法添加 XML 文档（`<param>`/`<returns>`）
- **Agent.cs**: 5 个公开属性 + 构造函数 + `ChatAsync` + `Reset` 添加 XML 文档

## v0.25.3 (2026-08-10) — 工具错误信息改进

### 🔧 P2 改进
- **26 个工具 catch 块添加异常类型**: `ex.Message` → `ex.GetType().Name: ex.Message`
  - 覆盖 Tools/ 下全部 26 处错误返回点
  - 区分 NullReferenceException / IOException / UnauthorizedAccessException 等

## v0.25.2 (2026-08-10) — 错误处理加固

### 🔧 P2 改进
- **空 catch 块添加日志**: 10+ 个静默异常捕获点增加 `DebugLog.Log()` 调用
  - `ProjectContext.cs` 6 处（指令文件/package.json/csproj/go.mod/Git/SafeGetFiles）
  - `SandboxManager.cs` 2 处（路径解析 + OperationCanceledException）
  - `RepoMapGenerator.cs` 3 处（.gitignore/目录扫描/LSP 符号提取）
  - `CheckpointManager.cs` 3 处（Git stash/Git status/变更文件）

## v0.25.1 (2026-08-10) — 代码质量修复

### 🐛 Bug 修复
- **Esc 转义方括号**: `TuiHelper.Esc` 中 `]` 被错误转义为 `[[]` 而非 `]]`，修复后 830/830 自测全过

### 🔧 P1 改进
- **SettingSchema 补全**: 新增 `DiffPreview` 和 `EmbeddingDimensions` 设置界面入口
- **PlanMode 接入**: `/plan` 命令升级使用 `PlanMode.GetPlanSystemPrompt()`（含项目上下文 + 仓库地图 + 两阶段确认）
- **MemoryStore 标记废弃**: 添加 `[Obsolete]` 属性，指向 `StructuredMemory`

## v0.25.0 (2026-08-10) — 语义记忆（P1 补全）

### 🔥 P1 语义记忆

**TF-IDF 语义搜索替换关键词匹配**
- `StructuredMemory.Search()` 和 `GetRelevantContext()` 升级为 CJK bigram + TF-IDF 评分
- `SemanticMemory` 新增 `SearchEntries(List<MemoryEntry>)` 重载，支持新结构化格式
- 旧式 `SearchRelevant(MemoryDocument)` 保持不变，向后兼容
- TF-IDF 无结果时自动兜底到原始子串匹配

**可选向量嵌入 (Embedding API)**
- 新增 `EmbeddingStore` 静态类：`.vec` 二进制向量文件 I/O + 余弦相似度 + 混合搜索
- `LLM.GetEmbeddingAsync()` 调用 `/v1/embeddings` 端点生成向量
- Hybrid 搜索：embedding 余弦相似度 ×0.7 + TF-IDF ×0.3
- 懒加载向量生成：搜索时 fire-and-forget，最多 3 并发
- 原子写入（临时文件 + 重命名）防并发冲突

**配置**
- `WAYCODER_EMBEDDING` 开关（默认 false，需 API 支持 `/v1/embeddings`）
- `WAYCODER_EMBEDDING_MODEL` 模型名（默认 `text-embedding-3-small`）
- `WAYCODER_EMBEDDING_DIMS` 维度（0=模型默认）

### 🔧 修复

- `SemanticMemory.SearchRelevant()` 时间新鲜度加权不再错误应用到零匹配文档

## v0.24.2 (2026-08-10) — 渐变增强 + 增量渲染

### 🔥 新增

**标题栏/状态栏金色渐变**
- `TuiTheme` 新增 `GradTitleBar`：暖金 `#FFD700` → 琥珀 `#FF8C00`
- `ControlRenderer` 新增 `DrawGradientBarFill` + `WriteGradientTextAt` 渐变条绘制
- `TuiTitleBar` / `TuiStatusBar` 整行金色渐变背景，黑字金底

**按钮焦点渐变差异**
- `AnsiTty` 新增 `DarkenRgb` 向黑色调暗
- `DrawButtonGradientLine` 非焦点时暗化渐变 55%，焦点保持完整亮色
- 焦点/非焦点视觉差异明显，Tab 切换即时响应

### 🔧 变更

**增量渲染（消除焦点切换闪烁）**
- `TuiView.FocusNext/FocusPrev` 标记丢失/获得焦点控件为脏
- `TuiManager` 条件全刷新：首帧/切屏/Resize → ClearScreen，增量时跳过
- `TuiScreen` 增量窗口渲染 `RenderWindowDirtyControls`：仅渲染脏控件，跳过背景/边框/遮罩
- `TuiWindow` 拖拽/缩放后 `RootView.MarkDirty()` 确保全量重绘

**边框背景修复**
- 竖边框/底角背景从 `bg=边框色` 改为 `bg=窗口底色`，不影响其他窗口外观
- Toast 文字背景跟随窗口底色，不再显示黑色

**CJK 修复**
- `WriteGradientTextAt` 改为 Rune 迭代 + `RuneWidth` 计算列偏移，修复汉字丢失

---

## v0.24.1 (2026-08-10) — 真彩渐变边框 + 按钮美化

### 🔥 新增

**真彩渐变边框**
- 对话框上下横边支持 24-bit TrueColor 渐变（青→蓝 / 绿→青 / 橙→黄 / 红→橙）
- 竖边框：左=起始色、右=终止色，纯色不断线
- `WriteGradientHLine` 逐字 Lerp 插值渲染
- `TuiTheme` 5 组渐变预设（`GradCyanBlue` / `GradGreenCyan` / `GradOrangeYellow` / `GradRedOrange` / `GradPurplePink`）

**按钮渐变背景**
- `DrawButtonGradientLine` 单次定位 + 逐字换背景色 + 末尾统一重置
- 按钮渐变独立于边框（比边框亮 30%），`TuiTheme` 4 组 `Btn*` 预设
- `TuiButton` 新增 `GradientBg` / `GradientBgStart` / `GradientBgEnd` 属性

**对话框美化**
- 边框默认改为 `Solid`（▀▄█ 半高块），粗线更显眼
- 四角改用全块 `█` 字符，防断线
- 竖边框 + 底角 bg=fg 防行间间隙
- 标题居中 + 渐变色（取 50% 位置）
- 内容文字 `TextAlign = HAlign.Center` 居中
- 按钮等宽居中（`NormalizeButtons` 统一取最宽者）
- 按钮 HBox 左右各留 1 字符间距，不贴边框

### 🔧 变更

- `AnsiTty` 新增 `RgbCode` / `DecodeRgb` / `LerpRgb` / `LightenRgb` TrueColor 工具
- `BorderChars` 支持 `HTop` / `HBottom` 独立字符
- `TuiWindow` 新增 `GradientBorder` / `GradientStart` / `GradientEnd` 属性
- `TuiScreen.RenderWindow` 渐变渲染逻辑重构

---

## v0.24.0 (2026-08-10) — 侧栏面板 + 按键架构简化

### 🔥 新增

**侧栏面板** (`UI/TuiControls/TuiSidePanel.cs`)
- 多分区同时显示：品牌、Todo、文件、MCP、LSP，无需标签切换
- `PanelSection` 数据模型：Title + Lines + Collapsed
- 左边框竖线分隔，每分区标题 + ─ 分隔线 + 内容行
- Ctrl+B 一键切换显示/隐藏

**标题栏 + 状态栏** (`UI/TuiControls/TuiTitleBar.cs`, `TuiStatusBar.cs`)
- 顶部 TitleBar：应用名 + Git 分支 + 版本号右对齐
- 底部 StatusBar：F1-F10 槽位指示（颜色区分状态）+ 提示文本 + Token
- 活跃槽位白底黑字，工作中绿色，等待权限黄色，出错红色

**输入区上下分隔线**
- InputTopBorder / InputBotBorder（━ 分隔线）包裹输入区
- BuildLayout 嵌套布局：VBox(外层) + HBox(ChatList+SidePanel 中间层)

### 🔧 变更

- **按键架构简化**：`HandleKey` → `OnKey` / `OnOwnKey` 统一模型
  - TuiControl 基类新增 `OnOwnKey`（控件自身按键），`OnKey` 负责路由
  - TuiView.OnKey 简化为：自己先处理 → 单焦点子节点路由
  - TuiWindow.HandleKey → OnKey，TuiScreen.HandleKey → OnKey + OnOwnKey
  - ChatScreen 6 个子方法合并到 OnOwnKey
  - 14 个控件 HandleKey override → OnOwnKey override
  - TuiPanel / TuiSeparator 删除 pass-through HandleKey override
- **LspTool**：新增 `SupportedServers` 公共属性供侧栏展示
- **AgentSlot**：`PanelTab ActivePanel` → `bool SidePanelVisible`
- **RenderBuffer.Write** 增强：精确 SGR 重置（SgrResetFg/SgrResetBg），避免冲掉底色
- **TitleBar/StatusBar 底色修复**：全部改用 `RenderBuffer.Write(fg, bg)` 传前景+背景色

### ❌ 移除

- `PanelTab` 枚举和标签切换系统
- 旧的 `TuiBanner.cs`、`TuiProgress.cs`（已迁移到 TuiControls 目录）
- `TuiView.OnSelfKey`（被 OnOwnKey 替代）

## v0.23.0 (2026-08-09) — TUI 控件系统增强

### 🔥 新增

**EdgeInsets 布局属性** (`UI/TuiControl.cs`)
- 新增 `EdgeInsets` 结构体（Top/Right/Bottom/Left），`Horizontal`/`Vertical` 快捷属性
- `TuiControl` 新增 `Margin` 和 `Padding` 属性（默认 0,0,0,0）
- `Padding` 自动内移渲染裁剪区 + 偏移 OnRender 原点
- VBox/HBox 布局自动计算 Margin 偏移

**Continuation 续接消息** (`UI/TuiControls/TuiListItem.cs`)
- `Continuation` 属性：同角色连续消息跳过头部（Icon + RoleLabel + TimeLabel）
- `IsPlainText` 模式：逐行渲染不走 Markdown 解析，避免系统消息行合并
- 正文 `Padding.Left = 2` 对齐标题文本

**OnClick sender 模式** (`UI/TuiControls/TuiButton.cs`)
- `OnClick` 从 `Action?` 改为 `Action<TuiButton>?`，按钮点击时传入自身引用
- 所有对话框按钮回调适配

### 🔧 变更

- **F1-F10 切换修复**：`Program.cs` 增加 `SwitchAgentSlot` 调用
- **角色图标**：所有图标从 emoji 改为 ● 纯色圆点（User=绿, Assistant=青, System=黄, Tool=灰）
- **聊天滚动**：`TuiListView` 新增 `ContentHeight` 属性，自动滚底 + PgUp/PgDn 手动滚动
- **IsAutoScrollToEnd**：`TuiScrollView` 和 `TuiListView` 重命名 `AutoScroll` → `IsAutoScrollToEnd`，旧属性标记 `[Obsolete]`
- **消息间距**：`ItemSpacing = 1`，所有消息之间自动空行

## v0.22.0 (2026-08-09) — Editor/Settings 迁移到新 TUI 架构

### 🔥 新增

**EditorCore 纯数据模型** (`Edit/EditorCore.cs`, ~280 行)
- 从旧 Editor.cs 提取纯数据层：文本缓冲区、光标、滚动、撤销栈、剪贴板、语法、诊断
- 零渲染依赖、零键盘依赖、零 TUI 依赖，可独立单测
- `InsertText/Backspace/Delete/NewLine/InsertTab` — 编辑操作
- `MoveCursor/MoveHome/MoveEnd/MovePageUp/MovePageDown/JumpToLine` — 光标导航
- `CopyLine/CutLine/PasteClipboard/DeleteLine` — 剪贴板操作
- `Undo` 完整撤销栈，`Save/SaveAsync` 文件持久化 + 异步 Lint 诊断触发
- `GetDiagnosticsAtLine/GetDiagSummary` — 诊断查询（委托 DiagnosticManager）

**TuiRichEditor 富文本编辑控件** (`UI/TuiControls/TuiRichEditor.cs`, ~277 行)
- 增强版源码编辑控件，TuiControl 子类，绑定 EditorCore 数据模型
- 行号列（右对齐 4 位）+ Gutter 诊断指示符（● 错误 / ▲ 警告 / · 无诊断）
- 语法高亮内容渲染（通过 Syntax.Tokenize），CJK 宽度感知截断
- 光标行整行高亮，诊断行背景色覆盖（错误红 41 / 警告黄 103）
- 完整键盘：↑↓←→ Home End PgUp PgDn / Backspace Delete Enter Tab
- Ctrl+Z/X/C/V/Y/G/S 组合键 → 事件回调（OnSaveRequested/OnJumpRequested/OnExitRequested）
- 自动滚动确保光标可见

**EditorScreen 编辑器屏幕** (`UI/TuiScreens/EditorScreen.cs`, ~275 行)
- 完整 TuiScreen 实现：TitleBar + TuiRichEditor + StatusBar1 + StatusBar2
- 全局键盘：Ctrl+S 保存 / Ctrl+G 跳行 / Escape 退出（脏文件三选一确认）
- 文件选择对话框：无 FilePath 时弹出 TuiDialog.Select 选择最近文件（最多 9 个）
- 动态状态栏：光标位置、行/字符统计、文件大小、语言、诊断摘要
- 保存后异步触发 Lint 诊断

**SettingsScreen 设置屏幕重写** (`UI/TuiScreens/SettingsScreen.cs`, ~360 行)
- 从旧 SettingsPage.Show() 重写为纯 TuiScreen + 控件树
- 左侧 TuiList 类别切换 + 右侧 VBox 设置项详情（TuiLabel + TuiDialog）
- select 类型 → TuiDialog.Select 下拉选择 / text/number/secret → TuiDialog.Input 输入
- Ctrl+S 保存写入 .env + Toast 通知 / Escape 退出 / ↑↓←→ Tab 导航
- 从 Config.SettingSchema() 自动生成布局，配置读写沿用原有 switch 逻辑

### 🔧 变更

- **Program.cs**: `/edit` `/edit <path>` `/settings` `/config` `Ctrl+O` → `TuiManager.PushScreen`
- **EditorCore Undo**: 修复 NewLine 撤销时删除错误行的问题（原来从末尾删除，改为从 Line+1 删除）
- **SettingsScreen.cs**: 移除旧的 EditorScreen 桩类（现在独立为 EditorScreen.cs）

### 📊 测试

- 自测：756 → 819 项（+63 项）

---

## v0.21.0 (2026-08-09) — 新增 6 个 TUI 控件

### 🔥 新增

**TuiTreeView 树形视图** (`UI/TuiControls/TuiTreeView.cs`, ~390 行)
- `TuiTreeNode` 树节点：Text/Icon/Children/Tag/Parent/IsLeaf/IsExpanded
- 递归构建可见节点列表（expand/collapse），≥200 个节点无障碍
- 树线渲染：`├─` `└─` `│` 缩进线 + `▼` `▶` 展开指示符
- 键盘全导航：↑↓ 移动焦点，←→ 展开/折叠/跳转父节点，Space 切换，Enter 激活
- 滚动：自动滚动使选中节点可见，`_scrollOffset` 偏移管理
- `ExpandToRoot()` 展开所有祖先使深层节点可见

**TuiRadioGroup 单选按钮组** (`UI/TuiControls/TuiRadioGroup.cs`, ~96 行)
- 互斥选项列表，`◉`/`○` 符号渲染选中/未选中
- 键盘：↑↓ Home End 导航，Enter/Spacebar 确认
- `OnSelectionChanged` 回调，自动装填选项数作为高度

**TuiComboBox 组合框** (`UI/TuiControls/TuiComboBox.cs`, ~177 行)
- 收起时显示选中项 + `▼`，展开时弹出下拉列表（最多 10 项可见）
- 键盘：Enter/Spacebar 展开，↑↓ Home End 在列表内导航，Enter 确认选择，Esc 收起
- `OnExpandedChanged` + `OnSelectionChanged` 双向回调
- RenderBuffer 背景填充，占位文本支持

**TuiSeekBar 滑块** (`UI/TuiControls/TuiSeekBar.cs`, ~163 行)
- `━●──` 风格滑块，比例计算滑块位置
- Value/MinValue/MaxValue/Step/LargeStep 可配置
- 键盘：←→ 步进微调，Home/End 跳边界，PgUp/PgDn 大步跳
- 可选数字标签 "50/100"，自定义字符（Thumb/TrackFilled/TrackEmpty）
- `OnValueChanged` 回调，值自动钳制

**TuiSeparator 分割线** (`UI/TuiControls/TuiSeparator.cs`, ~71 行)
- 水平/垂直两种方向，可选居中文本（`─── 标题 ───`）
- 自定义线字符和颜色

**TuiPanel 面板** (`UI/TuiControls/TuiPanel.cs`, ~139 行)
- 带边框 + 标题栏的嵌入式容器（TuiView 子类可选 12 种边框风格）
- 标题栏 `┤` 分隔线，内容区域 Padding，递归布局子控件

### ✨ 改进
- **TuiTreeView**: `MoveUp`/`MoveDown` 内部调用 `BuildFlatList()` 确保数据同步
- **TuiSeekBar**: 添加 `using CoreCoderSharp.Terminal` 修复 RenderBuffer 引用
- **TuiDemo**: F10=树形视图 F11=控件合集 F12=面板布局

### 📝 自测: 756/756

## v0.20.0 (2026-08-09) — 五层 TUI 架构全面接入 REPL

### 🔥 重大变更

**五层 TUI 架构** (`UI/TuiManager.cs` → `TuiScreen` → `TuiWindow` → `TuiView` → `TuiControl`)
- 树状场景图替代旧扁平 ScreenManager，每层职责清晰：Manager 管屏幕栈、Screen 管布局、Window 管浮层、View 管排版、Control 管交互
- 递归布局引擎（`VBox.Layout` / `HBox.Layout` 递归嵌套视图，精确 fillBg 背景色填充）
- 统一的 `MarkDirty` 脏标记 + 按需重绘，`HandleKey` 键盘事件沿树冒泡

**TUI 控件库** (`UI/TuiControls/`)
- `TuiButton` / `TuiInput` / `TuiTextArea` / `TuiLabel` / `TuiList` / `TuiListView` / `TuiMarkdown` / `TuiDialog` / `TuiGrid` / `TuiProgress` / `TuiSpinner` / `TuiBanner` / `TuiCheckbox` / `TuiTabs` / `TuiIcon`
- `TuiDialog` 静态工厂：`Info/Warn/Error/Success/Confirm/Confirm3/Select/MultiSelect/Permission/Input` 基于回调的 API，`ManualResetEventSlim` 阻塞包装器适配 REPL 同步流程
- `TuiListView` 可滚动列表 + 键盘/鼠标导航，`TuiMarkdown` 完整 Markdown 渲染，`TuiProgress` 进度条

**ChatScreen** (`UI/TuiScreens/ChatScreen.cs`)
- 完整 REPL 屏：欢迎横幅 + 聊天列表（TuiListView → TuiListItem → TuiMarkdown）+ 多行输入区（TuiTextArea）+ 状态栏（槽位/Token/Git）+ 建议面板
- 流式输出：`StartAgentMsg()` → `AppendToken()` → `FinishAgentMsg()` 实时追加
- 对话框包装器：`ShowInlinePermission()` / `ShowMenu()` / `RenderWait` 阻塞等待 + 渲染循环

**对话框增强**
- **键盘快捷键注册**：`TuiWindow.KeyShortcuts` 字典 + `RegisterShortcut(ConsoleKey, Action)` + `HandleKey` 优先快捷键拦截
- **对话框返回值**：`TuiWindow.Result` 属性，默认 -1（未选择），选择 ≥ 0
- 所有工厂方法（Permission/Confirm/Select/Input 等）自动注册 Y/N/A/Esc/Enter 快捷键并设置 Result
- **TuiScreen Esc 路由修复**：Esc 先路由到模态窗口快捷键（取消回调），未处理才关闭，防止 RenderWait 死锁

**TuiMenu 弹出菜单** (`UI/TuiControls/TuiMenu.cs`)
- 可滚动弹出菜单控件（~240 行），支持标题栏 + 窗口边框 + 滚动条指示器
- 键盘导航：↑↓ Home End PgUp/PgDn Enter Esc，1-9 数字键快速选择
- 分隔线（空字符串或 "---"），跳过键盘选中
- 位置自动 Clamp，屏幕边界不溢出
- `TuiDemo` F6=短菜单 F7=长滚动菜单(28项) F8=右键菜单

**Markdown 表格渲染**
- 完整 GFM 表格语法解析（`| cell | cell |`），Unicode 边框字符（┌┬┐├┼┤└┴┘）
- 列宽自适应内容 + 终端宽度不足时等比缩放
- **单元格内联格式**：`**加粗**` 和 `` `代码` `` 在表格单元格内正确渲染颜色
- `TuiDemo` F9=表格演示（语言对比 + 模型价格两张表）

### 🗑️ 移除
- **`UI/ScreenManager.cs`** (1458 行) — 旧全屏 TUI 管理器，功能全部迁移到新架构
- **`UI/WindowManager.cs`** (807 行) — 旧窗口管理器 + `ManagedWindow`/`UILabel`/`UIButton`/`UIInput` 旧控件体系

### ✨ 改进
- **Program.cs REPL 循环重写** — `ScreenManager.Instance` → `TuiManager.Instance` + `ChatScreen`，~100+ 处 `sm.` 调用迁移到新 API
- **PermissionManager** — `ShowInlinePermission` 改用 `ChatScreen.ShowInlinePermission()`（TuiDialog.Permission 包装器）
- **AgentSlot** — `SaveFrom`/`RestoreTo` 适配 `ChatScreen`（ChatMessages/InputArea/RecentFiles）
- **DiffPreview** — 移除无效 `ScreenManager.Instance` 引用
- **SettingsPage** — 独立全屏 Console UI，TUI 模式下临时 Exit/Enter
- **ThemeConfig** — `ApplyTo(ManagedWindow)` → `ApplyTo(TuiWindow)`
- **Editor/TuiTable/TuiBox** — `ScreenManager.Instance` 引用替换为 `TuiManager.Instance.ActiveScreen as ChatScreen`

### 📝 自测: 658/658

## v0.19.3 (2026-08-09) — 稳定性修复 + 控制台安全

### 🐛 修复
- **控制台无句柄崩溃**: `TTY.Cols`/`TTY.Rows` 属性在无真实控制台（管道/重定向/CI）时捕获 `IOException` 返回安全默认值 80×24，全局替换 8 个文件中 22 处直接 `Console.WindowWidth/Height` 调用
- **语义记忆文档拆分失败**: `ParseDocuments` 增加 `\r\n` → `\n` 规范化，修复 Windows 换行符下 `---` 分割线无法识别的问题
- **项目检测递归查找**: `DetectBuildTools`/`DetectFrameworks` 改用 `SafeGetFiles` 递归搜索 `.csproj`，修复 git 根目录下子目录项目无法检测到 dotnet/.NET SDK 的问题
- **自测参数越界崩溃**: 语义记忆测试访问 `docs[1]` 前增加 `Count >= 2` 守卫，避免拆分失败时 `ArgumentOutOfRangeException`

### 📝 自测: 705/705

## v0.19.2 (2026-08-09) — 多 Agent 工作区（F1-F10）

### 🔥 新增
- **多 Agent 工作区** (`AgentSlot.cs`): 10 个独立 Agent 槽位（F1-F10 一键切换），每个槽位拥有独立会话上下文 + 独立屏幕（聊天历史/输入草稿/状态栏/最近文件），懒创建 Agent，切换即保存/恢复 UI 状态
- **状态栏槽位指示条**: 状态栏左侧 10 个数字（1-9、0=10），底色白 = 当前显示屏幕；字色按状态——灰=空闲、绿=工作中、黄=等待权限、红=出错（LLM 全失败/超时自动标红）

### ✨ 改进
- **热键迁移**: F1/F2/F5/F10 让位给槽位切换，原功能迁移到 Ctrl 组合键——帮助 `Ctrl+H`、面板 `Ctrl+B`、设置 `Ctrl+O`、退出 `Ctrl+Q`
- **权限状态信号**: `PermissionManager` 新增 `PermissionPromptStarted`/`PermissionPromptResolved` 事件，权限确认框弹出时槽位指示条实时变黄
- Agent 运行时禁止切换槽位（提示等待），避免上下文撕裂

### 📝 自测: 704/704

## v0.19.1 (2026-08-09) — 三骨架接入 + 记忆系统升级

### 🔥 新增
- **文档查询工具接入** (`Tools/DocTool.cs`): 注册为第 31 个工具，`action='search'` 定向抓取官方文档（React/Next.js/Vue/DotNET/Rust/Go 等 30+ 库），`action='fetch'` 抓取指定页面，15 分钟会话级缓存
- **Diff 预览接入** (`UI/DiffPreview.cs`): `WAYCODER_DIFF_PREVIEW=1` 开启，`write_file`/`edit_file` 写前逐 hunk 确认（Y 接受/N 跳过/A 全接受/Q 取消），新增 `ApplyAccepted` 按接受集合重建内容；非交互模式（管道/重定向/测试）自动跳过

### ✨ 改进
- **记忆系统升级为结构化格式**: `MemoryTool` 从单文件 memory.md 切换到 `.corecoder/memory/*.md` frontmatter 多文件（read/write/search/delete 四操作，支持 name/description/type 参数）；`SystemPrompt` 记忆注入同步切换并自动迁移旧格式
- **自测隔离**: 记忆段与系统提示词段在临时目录运行，不再污染真实 `memory.md`；自测新增 Doc 校验、结构化记忆、Diff 纯函数测试

### 📝 自测: 694/694

## v0.19.0 (2026-08-09) — 技能系统 + 并行子代理 + CI

### 🔥 新增
- **技能系统** (`SkillsManager.cs` + `Tools/SkillTool.cs`): 标准 SKILL.md 格式发现与解析（`skills/<name>/SKILL.md`），SystemPrompt 注入精简技能列表，`skill` 工具按需加载完整 body 与打包文件
- **并行子代理** (`Tools/AgentTool.cs`): `tasks` 数组参数，最多 4 个并发，结果聚合返回；保留多层递归深度限制
- **GitHub Actions CI** (`.github/workflows/ci.yml`): 自动构建 + 全量自测
- **Git Worktree 隔离** (`WorktreeIsolation.cs`): 检测 worktree 路径并自动切换 bash cwd，`BashTool` 已接入
- **结构化记忆骨架** (`StructuredMemory.cs`): 对标 Claude Code frontmatter 记忆设计（未接入，预留）
- **Diff 预览骨架** (`UI/DiffPreview.cs`): 逐 Hunk 确认对话框（未接入，预留）
- **文档查询工具** (`Tools/DocTool.cs`): 搜索+抓取最新库/框架文档（未注册，预留）

### ✨ 改进
- **AutoGitCommit 质量校验**: conventional-commit 前缀强制 + 引号/代码围栏清理 + 不合格重试一次 + 兜底默认信息，杜绝乱提交

### 🔧 修复
- **SelfTest Checkpoint 段**（根因修复）: 原在仓库内执行 `git stash push` 会清空工作树且累积垃圾 stash（曾达 101 个），现隔离到临时非 git 目录执行
- **移除有风险的 Undo 测试**: FileBackup 还原路径会把备份拷回工作树，解析出错即覆盖真实文件，改为标注风险不测试

### 📝 自测: 676/676

## v0.18.5 (2026-08-09) — Plan 模式 + 稳定性修复

### 新增
- **Plan 模式** (`PlanMode.cs`): 14 个只读工具 + 规划提示词 + IsApproval 确认判断

### 修复
- **ReviewMode**: 始终列出文件名，git diff 优先
- **BashTool**: ReadToEndAsync 替代事件回调，避免管道死锁
- **CheckpointManager**: 路径 TrimStart('/') 修正 + ChangedFiles 回退
- **SelfTest**: Cwd 重置、缓存键断言修正、沙箱断言修正

### 操作
- Ctrl+C 闲时直接退，忙时中断后确认
- 自测: 656/657

## v0.18.4 (2026-08-08) — 终端抽象层 + 窗口系统 + 主题引擎

### 🔥 新增
- **终端抽象层** (`Terminal/`): TTY 屏幕控制 / RenderBuffer 零转义符渲染 / BoxChars 13种边框 / AnsiString 检测剥离 / Color 命名颜色 / AnsiText 快捷格式化
- **窗口管理器** (`UI/WindowManager.cs`): Z-order 层叠 / 裁剪渲染 / 模态对话框 / 弹出菜单 / Toast / UIControl 控件体系 (UILabel/UIButton/UIInput) / 11种边框风格 / 自定义边框
- **主题引擎** (`ThemeConfig.cs`): 6预设主题 / 自定义配色 / `~/.corecoder/theme.json` 持久化 / `/theme` 命令 / 设置页主题选择
- **Markdown 渲染**: 标题/代码块(14语言高亮)/表格/多级列表 / 内联格式
- **Diff 渲染**: 红绿背景 + 行号 + 语法高亮
- **InputManager**: 全键盘拦截 + 鼠标滚轮 + resize 即时重绘

### 🔧 修复
- **权限对话框**: `CheckAsync` 改用 `ShowInlinePermission` 行内渲染，不再卡死不显示
- **多级列表**: `MdListItem.Level` 字段，每2前导空格=1级缩进
- **减少空行**: 只在标题/代码块/表格后留空，段落间列表间不留
- **Emoji 宽度**: `cp >= 0xFEFF` → `cp == 0xFEFF`，修复所有 Emoji 被误判零宽
- **AnsiText 补全**: BoldFg/Reset/DimOn/BoldOn/FgCode/BoldFgCode/ClearLine/BorderOpen/Heading/Prompt
- **ClipboardHelper**: 跨平台剪贴板读取

### 📝 自测: 649/658

## v0.18.3 (2026-08-08) — 权限对话框行内渲染 + 表格显示修复 + Markdown 渲染缓存

### 🔧 修复
- **权限对话框卡死不显示**：`PermissionManager.CheckAsync` 从居中弹窗 `ShowMenu`（WindowManager 浮层 + 嵌套 ReadKey 循环）改为行内三行渲染 `ShowInlinePermission`，不依赖浮层机制，稳定可靠
- **表格输出导致聊天区卡死**：`TuiTable.Render()` 前缀 ANSI 哨兵 `\x1b[0m` 跳过 Markdown 重解析，逐行独立注入，添加后自动刷新屏幕
- **流式输出每帧重解析全文**：`BuildChatScreenLines` 新增按消息索引 + 内容缓存的渲染缓存，`AppendToken` / `FinishToolProgress` 精准失效，宽度变化或外部清空自动重置

### ⚡ 性能
- Markdown 渲染缓存：流式输出时只有最后一条消息每帧解析，其余消息走缓存 O(1) 命中，彻底解决大对话卡顿

## v0.18.2 (2026-08-08) — CJK/Emoji 全宽字符覆盖 + 方块光标

### 🔧 修复
- **RuneWidth 全面覆盖 CJK/全角/符号/Emoji**：
  - 修复 0x1F000-0x1F02F (麻将牌) 错误返回零宽的问题
  - 新增 0x2010-0x2027 (通用标点 — … " " ' ' ※ 等 East Asian Ambiguous)
  - 新增 0x2030-0x2043 (补充标点 ‰ ′ ″ ※ 等)
  - 新增 0x2600-0x27BF (杂项符号 + 装饰符号 ☀ ★ ❤ ➿ 等)
  - 扩展 Emoji 覆盖 0x1F000-0x1FAFF (含 Symbols & Pictographs Extended-A)

### ✨ 改进
- 光标改为大方块样式（`\x1b[2 q` DECSCUSR，对标竞品）

## v0.18.1 (2026-08-08) — 输入区分割线 + CJK 光标修复 + 聊天区简化

### ✨ 改进
- 输入区上下添加 dim 分割线（`─`），与聊天区视觉分离
- 状态栏紧跟下分割线，布局紧凑不重叠

### 🔧 修复
- CJK 中英文混合输入时光标逐渐错位（`InputHardToScreen` 换行边界反向遍历）
- 聊天区闪烁指示器移除（只有输入框需要光标/闪烁 `▊`）
- `works/**/*.cs` 编译排除，防止无关文件参与构建

### 📝 自测
- **61 项** Markdown 模块（通过全部 61 项）

## v0.18.0 (2026-08-08) — 窗口系统 + 主题引擎 + TUI 控件库 + 终端抽象层

### 🔥 重大变更

**窗口管理器** (`UI/WindowManager.cs`)
- Z-order 层叠窗口、裁剪渲染、模态对话框、弹出菜单、Toast 提示框
- UIControl 体系：UILabel / UIButton / UIInput，Tab/Shift+Tab 焦点切换
- 11 种边框风格：single/double/rounded/thick/solid/dotted/dashed/slash/triangle/ascii/custom
- 菜单满行高亮条、分隔线、滚动指示器 + 滚动条
- 窗口关闭自动还原背景

**终端抽象层** (`Terminal/`)
- `TTY` — 屏幕切换/清屏/光标/鼠标/颜色/样式快捷方式
- `RenderBuffer` — Write/Segment/Fill/SegmentBold/SegmentDim/Blink 零转义符渲染
- `BoxChars` — 13 种预设 + 自定义边框字符集
- `AnsiString` — ANSI 检测/剥离/截断/宽度计算
- `Color` — 命名颜色管理（Color.Cyan 替代数字 36）

**主题引擎** (`ThemeConfig.cs`)
- 6 个预设主题：default/ocean/forest/sunset/midnight/mono
- 自定义边框/背景/前景/选中色，持久化到 `~/.corecoder/theme.json`
- `/theme` 命令 + 设置页面主题选择
- ScreenManager 主题自动同步

**Markdown 渲染** (`UI/MarkdownRenderer.cs`, `UI/TuiMarkdown.cs`)
- 标题/段落/代码块(14 语言语法高亮)/表格/列表/内联格式

**Diff 渲染** (`UI/DiffRenderer.cs`)
- 红绿背景 + 行号 + 语法高亮

**InputManager** (`UI/InputManager.cs`)
- 全键盘拦截 + 鼠标滚轮 + resize 即时重绘

### ✨ 新增功能
- 子智能体递归：支持最多 5 层嵌套（`AgentTool.MaxDepth`）
- `/undo` 按文件精确 revert（`CheckpointManager.UndoAsync(id, filePath)`）
- TF-IDF 语义记忆（`SemanticMemory.cs`，零依赖纯 C#）
- Lint/Tool 超时可配置（`ToolTimeoutSec` / `LintTimeoutSec`）
- `/todo` 命令接入 UI
- Logo 补齐 "WAY**CODER**" 完整拼写

### 🔧 修复
- `DisplayWidth` 不计入 ANSI 转义码宽度
- 窗口裁剪 `Clip()` 修复顶框/左框被误裁
- 右框绝对定位，不再因 CJK 文字宽度错位
- 空窗口竖边始终渲染
- Damerau-Levenshtein 支持字符换位纠错
- 历史上限 while 循环修复
- 建议面板半宽+滚动+不溢出
- 滚动菜单选中项不被指示器挤出

### 📝 自测
- **636 项**（+35 新测试：主题/边框/Diff/InputManager/窗口）

## v0.17.5 (2026-08-07) — 移除 Terminal.Gui v2，恢复 AOT，三项短板清零

### 🔥 重大变更
- **移除 Terminal.Gui v2** — 回退到 ANSI 全屏 TUI（ScreenManager），Terminal.Gui v2 库体验不佳
- **恢复 AOT 编译** — `PublishAot` 重新启用，恢复零依赖单文件 exe 部署（~8MB）
- **子智能体递归** — 支持最多 5 层嵌套子智能体，深度可配置，自动移除 agent 工具防止无限递归
- **`/undo` 按文件精确 revert** — 支持 `/undo [N] <file>` 选择性恢复，`/undo -l` 列出检查点文件
- **工具/Lint 超时可配置** — 新增 `ToolTimeoutSec`（默认 120s）和 `LintTimeoutSec`（默认 60s）

### 🗑️ 移除
- 删除 `TerminalGuiRepl.cs` / `Controls.cs` / `ScrollMenu.cs`
- 删除 `--tui-v1` CLI 参数（仅保留 ANSI TUI）
- 删除 `TERMINAL_GUI` 编译常量和 Terminal.Gui NuGet 依赖
- 删除 `RunReplV2Async()` 方法

### ✨ 新增功能
- **Lint/Tool 超时可配置** — `WAYCODER_TOOL_TIMEOUT` / `WAYCODER_LINT_TIMEOUT` 环境变量
- **`/undo` 按文件恢复** — `CheckpointManager.UndoAsync(id, filePath)` + `GetCheckpointFiles()`
- **子智能体递归** — `AgentTool.MaxDepth` + `AsyncLocal<int>` 深度追踪
- **子智能体深度配置** — `WAYCODER_SUBAGENT_DEPTH`（默认 3，范围 1-5）
- **`/undo` / `/checkpoint` / `/checkpoints` 命令接入 UI** — 修复死代码问题

### 🔧 保留
- ScreenManager ANSI TUI 作为唯一交互式 REPL 实现
- 所有现有功能：侧栏面板、输入历史、彩色渲染、建议补全、弹窗菜单

## v0.17.4 (2026-08-07) — Terminal.Gui v2 默认 TUI

### 🔥 重大变更
- **Terminal.Gui v2 成为默认 TUI** — 替代手写 ANSI 转义码的 ScreenManager
  - 原 `--tui-v2` 标志改为 `--tui-v1`（回退到旧版 ANSI TUI）
  - 移除全部 `#if TERMINAL_GUI` 条件编译守卫
  - **AOT 编译暂时禁用**：Terminal.Gui v2 不支持 NativeAOT，待 v2 正式版后恢复
  - PublishAot 设为 false，仍保留单文件发布

### ✨ 新增功能
- **聊天区彩色多角色渲染** — `ChatView`（View + Label 组合）替代单色 `TextView`
  - User=亮青 `BrightCyan`、Assistant=白 `White`、System=灰 `Gray`、Tool=亮黄 `BrightYellow`、Welcome=青 `Cyan`
  - 每行独立 `ColorScheme`，手动 Y 坐标滚动
  - 流式输出实时追加到最后一行
- **侧边面板** — F2 切换 32 列右侧面板，4 个标签页：
  - 任务（Todo 列表）、文件（修改文件列表）、锁（活跃文件锁）、MCP（服务器状态）
- **输入历史导航** — ↑↓ 键浏览历史输入（单行模式），Ctrl+Enter 插入换行，Esc 清空
- **自动会话恢复** — 启动时检测 `_auto` 会话，提示 `/resume` 恢复
- **Scroll 快捷键** — PageUp/Down 滚动聊天区，Ctrl+Home/End 跳转首尾

### 🐛 修复
- 管道模式误判：`Console.IsInputRedirected` 在 bash 下始终为 true，修复为空 stdin 不触发一次性模式
- `Tab.View` 为 null 导致 NRE 崩溃（v2 API 变更：Tab 即 View）
- `Application.Top` 在 `Run()` 之前为 null 导致 NRE 崩溃
- `Colors.Base` / `Colors.TopLevel` / `Colors.Menu` 在 v2 不存在，改为视图级 `ColorScheme`
- `verify_build/` 目录混入 git 提交，已清理并加入 `.gitignore`

### 🔧 增强
- 状态栏快捷键：F1 帮助 / F2 面板 / F5 设置 / F6 编辑 / F10 退出
- 窗口标题实时显示当前模型名称
- CJK 字符在 Label 中正常显示（Terminal.Gui 内部处理）

## v0.17.3 (2026-08-07) — MCP 协议完善

### ✨ 新增功能
- **MCP 传输抽象层** — 解耦通信方式与协议层
  - 新增 `McpTransport` 抽象类：`SendRequestAsync` / `SendNotification` / `DisconnectAsync` / `IsConnected`
  - `StdioMcpTransport`：从 `McpConnection` 提取子进程管理代码（向后兼容）
  - `HttpMcpTransport`：HTTP POST + SSE 响应流解析，支持 Streamable HTTP 传输
  - `McpConnection` 重构为协议层，持有 `McpTransport` 实例委托通信
- **MCP 配置格式增强**
  - `mcp_servers.json` 新增 `transport` / `url` / `headers` 字段
  - 自动检测传输类型：有 `url` → HTTP，否则 → stdio（向后兼容）
  - `headers` 和 `url` 中的 `${VAR}` 语法自动展开为环境变量
  - `RunInit()` 模板添加 HTTP MCP 服务器注释示例
- **工具发现缓存** — 持久化 `tools/list` 结果，加速启动
  - 新增 `McpCache.cs`：SHA256 缓存键 + 24h TTL
  - `CachedMcpTool`：缓存命中时立即可用，后台异步刷新
  - 配置变更自动失效（基于命令/参数/URL 的 SHA256 哈希）
- **MCP 面板状态显示** — F2 面板 MCP 页显示服务器连接状态和工具数量
  - `McpManager.Info` 属性 + `ScreenManager` 自动读取渲染

### 🔧 增强
- MCP 客户端版本号更新为 0.17.3
- `DiscoveredTools` 支持按服务器前缀移除旧条目（缓存刷新时替换）

### 📝 自测
- 10+ 项 MCP HTTP 传输测试（传输检测、环境变量展开、headers 解析）
- 6 项 MCP 缓存测试（缓存键稳定性、规范标识符、格式验证）

## v0.17.2 (2026-08-07) — TUI 编辑器 Lint 诊断集成

### ✨ 新增功能
- **编辑器 Lint 诊断** — 保存文件时自动运行 lint 检查，行内标注错误和警告
  - 新增 `DiagnosticManager.cs`：解析 10+ 种 linter 输出格式（dotnet build / eslint / ruff / go vet / gcc / shellcheck / ruby / php / java / rust cargo）
  - 编辑器 gutter 指示器：有错误的行显示红色 `●`，有警告的行显示黄色 `▲`
  - 错误行红色背景、警告行黄色背景高亮
  - 状态栏显示错误/警告计数和当前行诊断消息
  - 配置开关：`WAYCODER_EDITOR_LINT`（默认开启）
- 新增 `ErrorBg` / `WarningBg` 颜色常量（ANSI 41 / 103）

### 📝 自测
- 10 组诊断解析测试（dotnet/eslint/ruff/go vet/gcc/shellcheck/ruby/php/java/rust）
- 3 组查询/配置/枚举测试

## v0.17.1 (2026-08-07) — 沙箱执行

### ✨ 新增功能
- **三级沙箱执行** — suggest（确认）/ auto-edit（自动编辑）/ full-auto（全自动沙箱）
  - 新增 `SandboxManager.cs`：环境变量清理、工作目录锁定、系统目录写保护、内存监控（1GB 限制）
  - bash 工具集成：沙箱模式下自动创建受保护的进程、异步内存监控
  - 权限系统联动：full-auto 模式 bash 直接放行
  - 配置开关：`WAYCODER_SANDBOX_LEVEL`（默认 suggest）

### 📝 自测
- 30 项沙箱管理测试

## v0.17.0 (2026-08-07) — Prompt 缓存 + 竞品 P0 清零

### ✨ 新增功能
- **Prompt 缓存追踪** — SHA256 本地检测系统提示词和工具定义是否重复发送
  - 新增 `PromptCache.cs`：静态追踪类，每次 LLM 请求前记录哈希值
  - `/stats` 面板展示缓存命中率、节省 Token 数和估算费用
  - 配置开关：`WAYCODER_PROMPT_CACHE`（默认开启）
  - 不影响实际 LLM 请求内容，仅在监控层面追踪
- **竞品 P0 差距全部清零** — 自动 Test 修复循环（v0.16.3 已有）+ Prompt 缓存（v0.17.0）

### 📝 文档更新
- ROADMAP.md：自动 Test 循环 ✅、Prompt 缓存 ✅、重新排布优先级
- competitor-analysis.md：P0 差距清零、功能矩阵更新、代码自愈评级提升

## v0.16.3 (2026-08-07) — 更名 WayCoder（道码）+ Watch 模式

### 🔥 重大变更
- **软件更名** — CoreCoder → **WayCoder（道码）**，因发现与现有商标名称冲突，规避侵权风险
  - 可执行文件：`corecoder.exe` → `waycoder.exe`
  - 环境变量前缀：`CORECODER_*` → `WAYCODER_*`（旧名仍兼容）
  - 配置目录：`.corecoder/` → `.waycoder/`（旧目录仍兼容）
  - 展示文本全部更新：UI 标题栏 / 帮助文本 / 测试输出 / 调试日志 / User-Agent / MCP 客户端名

### ✨ 新增功能
- **Watch 模式 (`--watch` / `-w`)** — 监听外部编辑器文件变更，检测代码注释中 `AI!` / `AI?` 标记自动触发 Agent
  - 兼容 Aider 的 AI 注释语法：`// AI!`、`# AI!`、`-- AI!`、`/* AI? */`、`<!-- AI! -->`
  - 支持 40+ 种编程语言（.cs .py .ts .js .go .rs .java .kt .swift .c .cpp .rb .php .vue .svelte 等）
  - 500ms 防抖合并快速连续修改 + 线程安全 `ConcurrentQueue` 队列
  - `/watch` REPL 命令切换 + `CORECODER_WATCH` 环境变量 + 设置界面集成
  - 3 处退出路径自动清理（F10 / quit / Ctrl+C）

## v0.16.2 (2026-08-07) — /stats 用量统计面板

### ✨ 新增功能
- **`/stats` 用量统计** — 表格面板展示模型/Token/花费/延迟/速度/消息/会话/权限等全维度用量

## v0.16.1 (2026-08-07) — 滚动修复 + /loop + /test + 界面主题

### 🐛 修复
- **滚动区域重构** — ChatScrollUp 下限 clamp + ChatScrollDown 延迟判底机制
- **ShowMenu 重写** — 长列表自动滚动 + PgUp/PgDn/Home/End 导航 + 选中项背景填充修复 + 滚动指示器
- **RenderSuggestions 越界修复** — 边框/填充改用 `chatW` 替代 `TW`，右侧面板激活时不再视觉错乱
- **ShowDialog 边距修正** — 标题/内容/提示行 fill 计算公式统一
- **箭头键聊天滚动** — 输入为空时 ↑↓ 滚动聊天区；Ctrl+↑/↓ 随时可滚动

### ✨ 新增功能
- **`/loop` 循环执行** — `/loop [最大轮次] 提示词` 重复执行 Agent 直到输出含成功标记（SUCCESS/✅/通过 等）
- **`/test` 分模块测试** — `/test all|tools|ui|git|config|memory|agent|review|mcp|system` 只跑相关模块
- **界面主题系统** — 6 套预设配色（default/ocean/forest/sunset/mono/cyberpunk）+ 4 种边框类型（rounded/single/double/bold）+ 独立边框色/强调色设置
- **SettingsPage 新增 `🎨 界面` 分类** — 配色方案/边框类型/边框颜色/强调色 四设置项，选色立即生效

### 🔧 增强
- 快捷键栏新增 `PgUp/Dn 滚动` 提示
- SelfTest 新增 Section/RunWithFilter 过滤机制
- ScreenManager 全面主题化：顶栏/分隔线/输入区/建议面板/菜单/对话框 均使用主题边框和颜色

## v0.16.0 (2026-08-07) — 20 项综合增强

### ✨ 新增功能
- **MCP 环境变量传递** — `mcp_servers.json` 支持 `env` 字段，可向 MCP 进程注入 API Key 等环境变量
- **标准输入管道模式** — `echo "prompt" | corecoder` 管道输入自动一次性模式 + yolo
- **记忆自动注入** — MemoryStore 内容自动注入系统提示词，跨会话项目知识持久化
- **Sub-Agent 增强** — 子智能体使用小模型（省钱）+ 注入父上下文（最近 6 条消息）
- **自定义提示词模板** — 扫描 `.corecoder/prompt.md` 及 `.corecoder/*.md`（排除 memory.md）自动注入
- **对话历史搜索** — `/history <关键词>` 搜索 + `Ctrl+R` 交互搜索，含上下文预览
- **Diff 预览确认增强** — write_file 显示行数/新建/覆盖 + 内容预览，edit_file 显示 +/- 对比
- **项目初始化向导** — `corecoder --init` 创建 `.corecoder/` + 模板文件（mcp_servers.json, prompt.md, memory.md）
- **命令别名** — `/c→/compact, /m→/model, /r→/reset, /h→/help, /t→/tokens, /d→/diff, /s→/save, /q→quit`
- **Token 性能统计** — 每次请求延迟 + tok/s 显示在 `/tokens` 和右下角状态栏
- **Agent 完成通知** — 耗时显示 + 终端响铃 `\a`
- **Agent 错误自恢复** — 工具报错时追加修正提示，引导 LLM 自我纠正
- **对话导出** — `/export` → `.corecoder/export_<date>.md`，Markdown 格式含角色和时间戳
- **输入历史** — `↑↓` 键浏览历史输入（单行模式），最多 200 条，去重相邻重复
- **模型热键切换** — `Ctrl+M` 循环切换大模型：deepseek-v4-flash → pro → gpt-5.4-mini → gpt-5.4
- **HTTP 代理支持** — 读取 `HTTPS_PROXY` / `HTTP_PROXY` / `ALL_PROXY` 环境变量配置代理
- **Tab 路径补全** — 输入像文件路径时 Tab 智能补全（最长公共前缀 + 候选列表）
- **Git 分支显示** — 启动时检测 `.git/HEAD`，顶栏显示当前分支名
- **最近文件列表** — `/recent` 显示最近修改文件（最多 50），按时间排序
- **快捷键完善** — `F1` 帮助，`F10` 自动保存 + 退出

### 🔧 增强
- CJK 感知 Token 估算（CJK ~1.5 tok/char，ASCII ~0.25 tok/char）
- Checkpoint 持久化加载（重启后 `/undo` 不丢失）
- Review 模式改为 git-diff-based（聚焦实际改动）
- 会话自动保存（退出时保存，启动时 `/resume` 恢复）
- 欢迎屏 ASCII Logo 注入聊天区
- 快捷键栏精简：`F1帮助 F2面板 F5设置 F6编辑 ↑↓历史 Ctrl+R搜索 Ctrl+M切模型`
- 版本号 v0.15.1 → v0.16.0

## v0.9.0 (2026-08-06) — 14 个内置工具 + 6 个新命令 + 进程管理

### ✨ 新增功能
- **仓库地图 (Repository Map)** — 自动扫描项目结构，17 种语言符号提取，ASCII 树状图
- **14 个纯 C# 内部工具** — 替代 Shell 依赖，可控缓冲区/超时/无转义问题：
  - 进程管理：`ps`（进程列表）、`kill`（终止进程，含系统进程保护）
  - 目录操作：`ls`（目录列表）、`mkdir`（创建目录）、`rm`（安全删除）、`cd`（切换目录）、`pwd`（打印路径）
  - 文件操作：`cp`（复制）、`mv`（移动）、`diff`（差异比对）、`stat`（元数据）
  - 文本统计：`wc`（行/词/字符计数）、`tree`（ASCII 目录树）、`find_replace`（跨文件查找替换）
- **6 个新 REPL 命令**：`/cost`（费用统计）、`/commit`（Git 提交）、`/doctor`（环境诊断）、`/config`（配置管理）、`/test`（运行测试）、`/status`（项目状态）
- **Git PR 工具** — `git_pr` 自动创建分支、推送、生成 GitHub/Gitee PR 链接
- **打包脚本** — `package.sh` / `package.bat` 一键 AOT 发布 + zip 打包
- **`/repomap` REPL 命令** — 手动刷新仓库地图

### 🔧 增强
- 仓库地图自动集成到系统提示词，LLM 时刻了解代码库布局
- `write_file`/`edit_file` 后自动使仓库地图缓存失效
- GitTool 异步输出读取，修复大输出死锁问题
- ITool Schema() 深克隆 Parameters，修复 "node already has a parent" 崩溃
- 工具总数：15 → 29
- 245 项自测（+49 项新工具/命令测试）

## v0.8.0 (2026-08-06) — 预算控制 + Hooks 生命周期 + MCP 协议

### ✨ 新增功能
- **硬预算上限** — `--max-budget-usd` CLI 标志 + `CORECODER_MAX_BUDGET_USD` 环境变量，超支自动停止
- **Hooks 生命周期** — PreToolUse / PostToolUse 事件，Shell 脚本处理器，退出码 2=阻止
- **MCP 协议支持** — Stdio 传输，自动发现 MCP 服务器工具，命名空间 `mcp__<server>__<tool>`
- **MCP 配置** — `.corecoder/mcp_servers.json` 配置服务器

### 🔧 增强
- `Config.MaxBudgetUsd` — 预算配置字段
- `ToolRegistry.AllTools` → 动态属性，自动合并 MCP 工具
- 166+ 项自测（+3 项预算/Hooks/MCP 测试）

## v0.7.0 (2026-08-06) — 自定义命令 + 自动 Lint 闭环 + YOLO 模式

### ✨ 新增功能
- **`--yolo` CLI 标志** — 一次性模式跳过所有权限确认，非交互环境可用
- **自动 Lint 反馈闭环** — `write_file`/`edit_file` 后自动运行 lint，错误注入 LLM 上下文
- **自定义斜杠命令** — `.corecoder/commands/*.md` 用户可扩展，支持 YAML frontmatter + `$ARGUMENTS`
- **示例命令** — `.corecoder/commands/review-code.md` 代码审查模板

### 🐛 Bug 修复
- **JsonNode Parent 冲突** — `FullMessages()` 深克隆消息，修复 "already has a parent" 崩溃

### 🔧 内部
- `CustomCommands.cs` — 自定义命令加载器
- 163+ 项自测（+3 项自定义命令测试）

## v0.6.1 (2026-08-06) — 三个新工具 + 增强语言支持

### ✨ 新增工具
- **Lint 工具** — 静态检查反馈闭环，支持 25+ 种编程语言（C#、Python、JS/TS、Go、Rust、Java、C/C++、Ruby、PHP、Swift、Kotlin、Lua、Shell、HTML/CSS、Vue、YAML、JSON、Markdown、Dart、R、SQL、Perl、Elixir、Haskell、Zig）
- **Web 搜索工具** — 通过 DuckDuckGo 进行网页搜索，无需 API 密钥
- **Checkpoint 系统** — Git stash + 文件备份双轨检查点，支持 `/checkpoint` `/undo` `/checkpoints` 命令

### 🔧 增强
- Lint 工具自动检测项目类型（通过扩展名和项目文件）
- AOT 安全的 JSON 序列化（无反射，手写 JSON 构建）
- 160 项自测全通过（+15 项新增测试）

## v0.6.0 (2026-08-06) — 纯 C# 版

### 🔥 重大变更
- **移除 Python 版** — 删除 `corecoder/`、`tests/`、`pyproject.toml` 等全部 Python 代码
- **C# 版成为唯一版本** — `CoreCoderSharp/` 是项目唯一实现
- **文档中文化** — README、CLAUDE.md、CHANGELOG 全部改为中文

## v0.5.0 (2026-08-05) — C# 重构版

### 🚀 C# 移植 (CoreCoderSharp)
- 完整移植 Python 版全部功能到 C# (.NET 10)
- AOT 原生编译为单文件 exe (7.8 MB)，无需运行时
- 12 个工具，44 项自测全通过

### ✨ 新增功能 (C# 版)
- **权限确认系统** — 危险操作前弹窗确认，支持 Ask/Auto/Yolo 三种模式
- **Git 智能集成** — git 工具 + `/git-status` `/git-log` `/git-diff` 命令
- **Web 抓取** — fetch 工具，自动提取网页纯文本
- **计划模式** — `/plan` 命令，Agent 先规划再执行
- **Todo 任务追踪** — Agent 可管理结构化任务列表
- **LSP 代码导航** — go-to-definition, find-references, hover, symbols
- **后台任务** — bash 命令后台执行，`/jobs` `/job-output` 查看
- **记忆系统** — Agent 可读写项目记忆 (`.corecoder/memory.md`)
- **流式工具执行** — LLM 边生成边执行工具，降低延迟
- **调试日志** — `--debug` 记录完整通信内容到 logs/
- **彩色 TUI** — Spectre.Console 美化界面 + ASCII Art 欢迎屏
- **项目指令加载** — 自动读取 CLAUDE.md/AGENTS.md
- **代码审查** — `/review` 命令多维度审查修改
- **模型回退** — LLM 失败时自动尝试备用模型
- **项目检测** — 自动识别语言/框架/构建工具

### 🔄 Python 版更新
- 默认模型改为 `deepseek-v4-flash`
- 新增 DeepSeek V4 Flash/Pro 定价
- CLI 自动识别 DeepSeek 模型并设置 base URL
- 所有注释和 docstring 翻译为中文

## v0.4.1 (2026-08-01)
- 添加 AGENTS.md 双语智能体指南
- 初始版本
