{ Gfx —— QBASIC 风格图形扩展的 **Pascal 侧声明**

  ## 现状（先读这一节，别照着这份声明就去写程序）

  ⚠ **这是一份「声明有了、实现还没有」的单元。**

  · 这些名字**在 `Lib/` 里没有任何实现**（`Lib/shared/src/*.c` 里既没有 `Gfx*`
    也没有它对应的 `gfx_*`；`Lib/shared/graph.vml` 那份走的是 `gfx_*` + 模拟 VGA 显存，
    而那条路的 58 处 `call gfx_*` **全是未解析标签**，见 `docs/老程序兼容性.md`）。
  · 也就是说：`uses Gfx` 本身能编过，但一旦**真的调用**其中任何一个，
    链接期会报「未定义的函数 'GfxLine'」这类硬错误。**这是有意的** ——
    响亮的失败好过静默画不出东西。

  ## 为什么要保留这份声明（而不是删掉）

  它标出了「Pascal 侧想要的图形接口形状」。真正能用的是另一条路：
  `uses Graph` → `Lib/pascal/graph.pas` → `graph.pas` 里 `AutoLinkUnit` 链上 `bgi.vml`
  → `Lib/c/graphics.h`（纯头文件的 BGI 垫层，45 个函数）→ 宿主 `ui_*` 图元。
  **手机上能画的图形走的是那一条**，`GfxLine`/`GfxCircle`/`GfxPrint` 这些名字
  与 BGI 的 `Line`/`Circle`/`OutTextXY` 基本一一对应 —— 将来要落实这一族，
  正确的做法是**在这份声明下面写转发到 BGI 的一行实现**，而不是另起一套绘图原语
  （本仓铁律：一处实现；两份实现漂移的症状是"同一程序 C 画得对、Pascal 画得不对"）。

  ## 它此前连单元外壳都没有

  ⚠ 这个文件从前**只有下面这堆 `procedure`/`function` 头，没有 `unit …; interface … end.`**
  ⇒ 它根本不是一个合法的 Pascal 单元：`uses Gfx` 会在**库文件自己**身上报语法错
  （实测 `期望 'begin'`），而错误位置指向一个跟用户源码毫无关系的文件。
  现已补上外壳 —— 上面说的"链接期硬错误"就是从这一版开始的、可见的行为。

  ⚠ `implementation` 段**必须**是空的：一旦写上函数体，本单元会自己生成同名标签，
  主程序里的 `CALL` 会解析到这个本地版本而不是库里的实现（与 `graph.pas`/`crt.pas` 同一约定）。 }

unit Gfx;

interface

{ 基本 VGA }
procedure VgaClear;
procedure VgaPutchar(x, y: integer; c: char; color: integer);
procedure VgaPuts(x, y: integer; s: string; color: integer);

{ 屏幕模式与信息 }
function GfxScreen(mode: integer): integer;
function GfxWidth: integer;
function GfxHeight: integer;
function GfxDepth: integer;

{ 调色板 }
procedure GfxPalette(idx, r, g, b: integer);
function GfxPaletteGet(idx: integer): integer;

{ 像素操作 }
procedure GfxPset(x, y, color: integer);
function GfxPoint(x, y: integer): integer;
procedure GfxCls;
procedure GfxClsColor(color: integer);

{ 绘图原语 }
procedure GfxLine(x1, y1, x2, y2, color: integer);
procedure GfxRect(x1, y1, x2, y2, color: integer);
procedure GfxRectFill(x1, y1, x2, y2, color: integer);
procedure GfxCircle(cx, cy, r, color: integer);
procedure GfxCircleFill(cx, cy, r, color: integer);
procedure GfxArc(cx, cy, r, sa, ea, color: integer);
procedure GfxSector(cx, cy, r, sa, ea, color: integer);

{ 文字 }
procedure GfxPrint(x, y: integer; text: string; color: integer);
procedure GfxPrintScale(x, y: integer; text: string; color, scale: integer);

{ 填充 }
procedure GfxFloodFill(x, y, fc, bc: integer);

{ 高级图形 (SYSCALL) }
function GfxScreenshot: integer;
function GfxPutImage(x, y, w, h: integer; var data): integer;
function GfxGetImage(x, y, w, h: integer; var buffer): integer;
function GfxViewport(x1, y1, x2, y2: integer): integer;

implementation

{ 空 —— 实现尚未落地，理由见文件头「现状」一节。 }

end.
