// demo_bgi.swift —— **传统图形接口**示范（BGI / Borland Graphics Interface，Swift）
//
// 四层示范的第三层。这一层与第四层（`ui_*`）的差别是**两个年代的两套假设**：
//   · **固定分辨率**：开一次 `VGAHI` 就是 640×480，程序里到处写死坐标；
//   · **索引色**：`setcolor(4)` 说的是「红」，不是 RGB `0x000004` —— 全套 16 个色号。
//
// ◆ 实现在哪
//
// C/C++ 走的是 `Lib/c/graphics.h`（一份**头文件里带函数体**的 BGI 垫层，
// `#include <graphics.h>` 就能用）。**其它语言 include 不了 C 头文件**，走另一份实现：
// **`Lib/shared/bgi.vml`**（由 `Lib/shared/src/bgi.c` 编译而来，**函数名与 C 版逐个相同**，
// 46 个函数：`initgraph`/`setcolor`/`line`/`circle`/`outtextxy`/`bar`/`pieslice`…）。
//
// ◆ ⚠ 本份**必须带 `--lib` 才能进链接**（这是本语言当前唯一的入口）
//
//   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll \
//       third_party/vml/Examples/swift/demo_bgi.swift \
//       --lib third_party/vml/Lib/shared/bgi.vml --screen 640x480 --frames /tmp/bgi_frames/
//
//   实测（本轮，`Lib/shared/bgi.vml` 由并行的同事新加）：
//     · `--lib Lib/shared/bgi.vml`  ⇒ **跑通出图**：640×480、16 个不同颜色、
//       `setcolor(4)` 出 `#AA0000`（索引 4 = 红，正是 BGI 调色板）✓
//     · `#param lib("bgi")`        ⇒ **不生效**（22 个函数全报「未定义的函数」）——
//       Swift 这条路的 `#param lib` 不进链接清单（实测 `成功链接: bgi.vml` 零次）。
//   ⇒ 手机上要跑这一份，得把 `bgi.vml` 加进 `vmltool.config.xml` 里
//     `<Language Name="swift" Libs="…">`（**那是共享配置，本份例程没有代改**）。
//
//   ⚠ 同一个 `--lib` 对 Rust / Go / D / ObjC **都不生效**（实测全报「未定义的函数」），
//     根因是那四门的前端在 `Compile` 里先自做一次 `LinkStandardLibrary(prog, lang, null)`、
//     把 `SharedPrefixMap` 之外的库函数当**未解析**抛错，外层那条带 `--lib` 的链接再也跑不到
//     （9 门语言实测，与"内部有没有这一步"100% 相关，见交付报告）。
//
// ◆ 看画面：用 `--frames 目录`
//
// `closegraph()` 之后场景就空了（与 `ui_win_close()` 同一机制），所以 `--frame` 单个文件
// 那一刀可能取不到东西；`--frames` 是**每次呈现当场拍快照**，照常出图。
//
// 跑法：命令行页输入  vml run examples/swift/demo_bgi.swift
//       （手机端需先按上面那条把 bgi.vml 挂进配置）

// ── ① 开图形模式：DETECT(0) ⇒ 由库选驱动/模式，本平台是 VGA 640×480 ──
var gd = 0
var gm = 0
initgraph(gd, gm, "")

// ── ② 背景 + 清屏（`cleardevice` 刷成当前背景色）────────────────
setbkcolor(0)
cleardevice()

// ── ③ 固定分辨率：整屏坐标写死成 640×480 ───────────────────────
//    外框贴着屏幕边（MAXCOLORS 那套索引色，14 = 黄）
setcolor(14)
rectangle(0, 0, 639, 479)
setcolor(7)
rectangle(8, 8, 631, 471)

// ── ④ 索引色：16 个色号各描一条竖线（这一屏就是"索引色"的判据）──
var i = 0
while i < 16 {
    setcolor(i)
    line(24 + i * 38, 30, 24 + i * 38, 110)
    i = i + 1
}
setcolor(15)
outtextxy(24, 118, "0..15  index colors  (setcolor)")

// ── ⑤ 基本图元：线 / 矩形 / 圆 / 椭圆 ──────────────────────────
setcolor(4)
line(24, 150, 616, 150)
line(24, 150, 24, 250)
line(616, 150, 616, 250)

setcolor(11)
rectangle(60, 170, 240, 240)
setcolor(10)
rectangle(80, 185, 220, 225)

setcolor(12)
circle(340, 205, 46)
setcolor(13)
circle(340, 205, 26)

setcolor(9)
ellipse(470, 205, 0, 360, 70, 40)

// ── ⑥ 填充：setfillstyle(SOLID_FILL, 色) + bar ──────────────────
setfillstyle(1, 2)
bar(60, 270, 200, 340)
setfillstyle(1, 5)
bar(220, 270, 360, 340)
setfillstyle(1, 6)
bar(380, 270, 520, 340)

// ── ⑦ 文字：outtextxy（写死坐标 = 固定分辨率那一套）─────────────
setcolor(15)
outtextxy(60, 352, "line / rectangle / circle / ellipse / bar")
setcolor(14)
outtextxy(60, 372, "VGAHI 640x480  16-color indexed palette")

// ── ⑧ 收尾：呈现 → 关图形模式 → 正常结束 ───────────────────────
ui_present()
closegraph()
