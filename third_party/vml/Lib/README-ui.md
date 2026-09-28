# 手机端 UI syscall（对话框 / 窗体绘图 / 输入）

> 这份接口**只在手机 App（WayCoder）里可用** —— 由宿主实现（`WayCoder.Maui/Services/VmlUiCalls.cs`），
> 桌面 `vmltool` 不认识这些号（会走内置 switch → 报未知 syscall）。
> 号段的**唯一事实源**在 `WayCoder/UI/Shared/VmlUiProtocol.cs` 的常量注释里，本文与之同步。

## 为什么是 500–599

运行时现用最大号是 402（`VMLRuntime/SyscallNumber.cs`）。宿主 syscall 处理器在**内置 switch 之前**
被调用（`VMLRuntime.Syscall.cs:23`），所以这个号段完全由宿主解释 —— **运行时一行不用改**。
手机端跑的是 mcu 模式（privilegeLevel > 0），dispatch 顶部还有一道白名单门
（`UserAllowedSyscalls.Contains`），宿主启动时会 `UserAllowed.Add(500..599)` 把门打开；
**这正是"处理器注册了却永远不被调用"的唯一原因**，自己接一套新号段时别漏。

## 号段表

参数写在 R0、R1…；字符串是**内存里的 NUL 结尾 UTF-8 字节**；颜色是 **0xAARRGGBB**。
"选项块" = 若干 `\0` 分隔的字符串（末尾再补一个 `\0`）。

### 对话框

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 500 | `DLG_MSG` | R0=标题* R1=正文* R2=样式(0信息/1警告/2错误/3询问) | 0=确定/是，1=否/取消 |
| 501 | `DLG_SELECT` | R0=标题* R1=提示* R2=选项块* R3=选项数 R4=默认项 | 选中下标，取消 -1 |
| 502 | `DLG_MULTI` | 同 501 | 位掩码，取消 -1 |
| 503 | `DLG_INPUT` | R0=标题* R1=提示* R2=缓冲* R3=容量 | 写入长度，取消 -1 |

### 窗体与绘图（保留模式）

程序先把图元一条条追加进窗口场景，宿主按帧渲染 —— VM 只追加、UI 只渲染，互不阻塞。

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 520 | `WIN_OPEN` | R0=标题* R1=宽 R2=高 | 句柄(1)，失败 -1 |
| 570 | `WIN_OPEN_EX` | R0=标题* R1=宽 R2=高 R3=可旋转 R4=要手柄 | 句柄(1)，失败 -1 |
| 582 | `WIN_OPEN_PC` | R0=标题* R1=宽 R2=高 R3=方向声明 R4=要屏幕键盘 | 句柄(1)，失败 -1 |
| 521 | `WIN_CLOSE` | R0=句柄 | 0 |
| 522 | `DRAW_CLEAR` | R0=颜色 | 0 |
| 523 | `DRAW_PIXEL` | x y 颜色 | 0 |
| 524 | `DRAW_LINE` | x1 y1 x2 y2 颜色 线宽 | 0 |
| 525 | `DRAW_RECT` | x y w h 颜色 填充(0/1) 线宽 圆角半径 | 0 |
| 526 | `DRAW_CIRCLE` | cx cy r 颜色 填充 线宽 | 0 |
| 527 | `DRAW_ELLIPSE` | cx cy rx ry 颜色 填充 线宽 | 0 |
| 528 | `DRAW_TEXT` | x y 文本* 颜色 字号 锚点(0左/1中/2右) | 0 |
| 529 | `DRAW_ICON` | x y 图标名* 尺寸 颜色 | 0 |
| 530 | `DRAW_IMAGE` | x y 路径* w h | 0 |
| 531 | `DRAW_PRESENT` | — | 0（帧边界标记；宿主定时器也会刷） |
| 586 | `SET_VALIGN` | R0=x R1=y | 0（记下"下一行文字"的落点，`SET_FONT` 的竖向搭档） |
| 583 | `FLOOD_FILL` | R0=x R1=y R2=色 R3=边界色 | 落笔的矩形条数（0 = 没填） |
| 584 | `GET_IMAGE` | R0=x R1=y R2=w R3=h | 图像句柄（≥1），失败 0 |
| 585 | `PUT_IMAGE` | R0=x R1=y R2=句柄 R3=模式(0=COPY/1=XOR) | 1 成功、0 失败 |
| 587 | `GET_PIXEL` | R0=x R1=y | `0xRRGGBB`，越界 -1 |
| 588 | `SCREENSHOT` | R0=相对路径* | 写入字节数，失败 -1 |
| 589 | `BLOCK` | R0=op（见 `VmlBlockOp`） | 随 op；**认不出的 op 返回 -1** |
| 594 | `FREE_IMAGE` | R0=句柄（0=全部） | 0（宿主级图像表不随场景释放，靠它手动收回） |
| 595 | `GFX_STATE` | R0=op（蒙版/图层，见 `VmlGfxOp`） | 随 op |

#### 文字属性（状态式）/ 渐变 / 路径 / 样式

「状态式」= 先设一次属性、之后连画很多行（菜单、计分板）；一次性写法见上面的 `DRAW_TEXT`。
两者都留着，不是重复。

⚠ `DRAW_TEXT_EX`(581) 与 `DRAW_TEXT`(528) **寄存器排布不同**：老号 R6 是样式位，
新号 R6 是**竖对齐**、R7 才是样式位。这是它走新号的原因（老程序只传前六个，
第七只寄存器里是它上一句留下的值 ⇒ 加参数 = 让老程序行为变成未定义的），
但**两条包装函数的参数序要照着各自那行写**，别把一个抄到另一个上。

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 532 | `SET_FONT` | R0=字号 R1=颜色 R2=对齐 R3=粗体 R4=斜体 | 0 |
| 533 | `TEXT` | R0=x R1=y R2=文本*（用 `SET_FONT` 设的属性） | 0 |
| 581 | `DRAW_TEXT_EX` | R0=x R1=y R2=文本* R3=颜色 R4=字号 R5=横锚点 **R6=竖对齐** **R7=样式位** | 0 |
| 534 | `GRADIENT` | R0=id R1=类型 R2..=几何与色标 | 0（定义一条渐变，之后用 `@id` 引用） |
| 535 | `DRAW_PATH` | R0=SVG 路径串* R1..=样式 | 0（`M`/`L`/`Z` + 曲线 + 圆弧，见 `DrawPath.cs`） |
| 536 | `DRAW_POLYGON` | R0=点数组* R1=点数 R2..=颜色/描边/渐变 | 0 |
| 537 | `DRAW_POLYLINE` | 同 536 | 0 |
| 538 | `DRAW_RECT_GRAD` | R0=x R1=y R2=w R3=h R4=渐变 id … | 0 |
| 539 | `DRAW_CIRCLE_GRAD` | R0=cx R1=cy R2=r R3=渐变 id … | 0 |
| 574 | `DRAW_SHAPE` | R0=形状码（`VmlShape`）R1.. | 0（形状码是契约，只能末尾追加） |
| 575 | `BRUSH` | R0=op（见 `VmlBrushOp`） | 刷子句柄；**窗口态，`ui_clear` 不清** |
| 576 | `SET_STYLE` | R0=画笔刷子 R1=文字刷子 | 0（本批只支持**纯色**画笔/文字，渐变会警告并退回起始色） |

**`SCREENSHOT`（#588）把画布存成 PNG 文件**（不含屏幕手柄与标题栏）。
**空串 = 自动命名** `shot/<窗口标题>_<日期>_<时间>.png`（如 `shot/五子棋_20260924_102801.png`）；
自己给路径也行（相对沙箱，无扩展名补 `.png`，子目录自动建），含 `..` 或盘符的直接失败。
实现走**共享光栅器**，四端同一份代码、不需要任何权限；
要在运行中读像素给自己用，走 `GET_IMAGE` / `GET_PIXEL`。

#### 什么时候用 `WIN_OPEN_EX` / `WIN_OPEN_PC`

- **`ui_win_open(t,w,h)`**（`#520`）：老接口，一个字的声明都不用给。
  跟随旋转但**坐标系不动**（宿主等比缩放着显示）—— 老程序走这条。
- **`ui_win_open_ex(t,w,h,rot,pad)`**（`#570`）：要**声明**转屏策略与要不要手柄区。
  `rot` 三选一（`VML_WIN_PORTRAIT`/`ROTATABLE`/`LANDSCAPE`），`pad` 用
  `VML_WIN_NEED_GAMEPAD`/`NO_GAMEPAD`。⚠ 只有 `ROTATABLE` 那一档会**换坐标系**
  （程序得按 `WINDOWRESIZE` 重排版）。
- **`ui_win_open_pc(t,w,h,rot,kbd)`**（`#582`）：**电脑屏窗口**，给老程序用。
  坐标系**固定**为 `(w,h)` 永不重排、触摸**只当鼠标**、第 5 个参数是**屏幕键盘**
  （`VML_WIN_NEED_KEYBOARD`/`NO_KEYBOARD`）。这里的 `rot` **没有 `ROTATABLE` 那一档**，
  只决定锁不锁方向。画图照旧走 `ui_*`。

### 输入（统一消息队列）

键盘、鼠标、触摸、定时器、窗口事件**进同一个队列**，程序统一按「取消息 → 分派」写循环。

⚠⚠ **这一段曾经列着 561 `MSG_WAIT` / 562 `MSG_COUNT` / 564 `TIMER_KILL` 三个号 ——
它们已经不存在了**（v0.96.483 把 560/561/562/…/596 **七个号**并成了 `MSG`(#560)
一个号 + 操作码，定时器同理）。照着老表写 `SYSCALL #561` 的程序**什么都不发生**
（宿主 switch 落到兜底、返回 -1）。**改号段整合时，调用点必须全部找出来** ——
`grep -rn 'SYSCALL #5' .` 就是那条命令（`Lib/shared/src/conio.c` 曾因此坏了三处）。

**一个号 + 操作码**：`R0` = 操作码，参数从 `R1` 起（**整体后移一格**）。

| 号 | op | 名称 | 参数 | 返回 |
|---|---|---|---|---|
| **560** | 0 | `MSG_POLL` | R1=消息缓冲地址 | 消息类型，无消息 0 |
| | 1 | `MSG_WAIT` | R1=缓冲地址 R2=超时毫秒(0=无限) | 消息类型，超时 0 |
| | 2 | `MSG_COUNT` | — | 待处理条数 |
| | 3 | `MSG_CLEAR` | — | 丢弃条数 |
| | 4 | `MSG_DROP` | R1=类别（见 `VmlMsgKind`）| 丢弃条数 |
| | 5 | `MSG_POLL_EX` | R1=缓冲 R2=保留(1=只看不取走) | 消息类型 |
| | 6 | `MSG_WAIT_EX` | R1=缓冲 R2=超时 R3=保留 | 消息类型 |
| **563** | 0 | `TIMER_SET` | R1=间隔毫秒 R2=用户标记 | 定时器 id |
| | 1 | `TIMER_KILL` | R1=id | 0 |
| 565 | — | `WIN_CLOSED` | — | 0 开着 / 1 被关掉 / 2 **从来没开过窗** |

**消息结构固定 16 字节**（4 个小端 int，任何语言都能直接读）：

```
+0  类型      +4  A        +8  B        +12 时间戳(ms)
```

| 类型 | 名称 | A | B |
|---|---|---|---|
| 1 / 2 | KeyDown / KeyUp | 键码 | 0 |
| 3 / 4 / 5 | MouseMove / Down / Up | x | y |
| 6 / 7 / 8 | TouchDown / Move / Up | x | y |
| 9 | Timer | 定时器 id | 装机时的用户标记 |
| 10 | WindowClose | 0 | 0 |
| 11 | WindowResize | 新宽 | 新高 |

键码沿用 **Win32 虚拟键值**（与 HTML `keyCode` 基本一致）：方向键 37–40、回车 13、空格 32、ESC 27
（常量见 `UI/Shared/VmlUiProtocol.cs` 的 `VmlKeys`）。

#### 轮询式查询（不占消息队列）

「此刻按住没有」这类问题**不要**做成新消息类型：16 字节的消息塞不下"手指 id + x + y"，
扩成 20 字节会**打断所有现有程序** ⇒ 走查询，零破坏。

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 556 | `TOUCH_QUERY` | R0=slot(0–9) R1=输出数组*3 | 1=按着 / 0=没按（输出 x,y,id） |
| 557 | `KEY_QUERY` | R0=虚拟键码 | 1=此刻按着 / 0 |
| 558 | `ORIENTATION_LOCK` | R0=0 竖屏 / 1 横屏 / 2 自动 | 0 |
| 559 | `IMMERSIVE` | R0=0/1（隐藏状态栏与导航栏） | 0 |

⚠ `TOUCH_QUERY` 是**轮询**而不是消息：`PostTouch` 只对 **slot 0** 投事件消息，
其余 slot 只更新查询状态 ⇒ **多指（和弦、双摇杆）只能靠轮询**，要挂定时器自己做按下/抬起边沿检测。
⚠ `KEY_QUERY` **治不了丢 KeyUp**（手指划出按键、系统吃掉 CANCEL 都会让 KeyUp 永远不来），
连发逻辑仍然要自带刹车。

#### 屏幕与窗口状态

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 566 | `SCR_W` | — | 可用绘图区宽（dp，**开窗前就能问**） |
| 567 | `SCR_H` | — | 可用绘图区高 |
| 568 | `HOST_LANG` | — | 0 = 中文 / 1 = 英文（**跟随系统语言**；C 侧 `ui_get_language()`） |
| 569 | `SCR_ORIENT` | — | 0 竖屏 / 1 横屏 |
| 553 | `SCREEN_KEEP_ON` | R0=0 关 / 1 开 | 0 |

绘图单位 = **密度无关像素（dp）**，所以 `ui_win_open(t, ui_scr_w(), ui_scr_h())` 得来的坐标系
与屏幕 1:1：既不缩放也不出界，在高分屏与低分屏上观感一致。

### 硬件与手感（一个号 + 操作码）

| 号 | 名称 | op / 参数 | 说明 |
|---|---|---|---|
| 540 | `POWER` | op0 `BATTERY`：R1=输出数组*3 | 电量% / 充电中 / — |
| | | op1 `SAVER` | 省电模式 0/1（**不在** BATTERY 的输出里） |
| 541 | `AUDIO` | `AudioOp`：播放文件 4 个 op + `NoteOn`/`NoteOff`/`AllNotesOff` | 见 `docs/VML宿主接口.md` |
| 542 | `CLIPBOARD` | op0 `SET` R1=文本* / op1 `GET` R1=缓冲 R2=容量 | 系统剪贴板 |
| 543 | `SHARE` | op0 `TEXT` / op1 `URL`，R1=内容* | 拉起系统分享面板 |
| 545 | `VIBRATE` | `VibrateOp`：震一下 / 按节奏 | 老程序的 545/546 两号已合并 |
| 550 | `STORE` | op0 `SET` / op1 `GET` / op2 `DELETE`，R1=键* | 键值存档，**键要加 `vml.` 前缀** |
| 554 | `SENSOR` | `SensorOp`：Query/Available/Rate/Calibrate；`SensorKind`：Accel/Gyro/Rotation | 传感器三件套 |

⚠ **`ui_sensor` 的 op 必须占 `R0`**：包装写成 `asm("SYSCALL #554, ${kind}, ${out}")` 会**恒定返回 -1**，
而函数看着完全正常（参数整体前移一格）。凡是"一个号 + 操作码"的，**op 永远是 `R0`、参数从 `R1` 起**。
⚠ 电量与省电是**两个 op**，不是同一个调用的两个输出 —— 漏了后者的话那一行永远不显示，且**不报错**。
⚠ 传感器**读不到 ≠ 读到 0**：先 `Available` 再读，前者要如实说"这台设备没有"。

### 通用调用口（跨语言逃生门）

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 573 | `CALL_JSON` | R0=函数名* R1=参数 JSON* R2=输出缓冲* R3=容量 | 写入长度（信封见 `VmlJsonApi`） |
| 577–580 | `CALL_WITH_INT8` / `FLOAT8` / `LONG4` / `DOUBLE4` | R0=自检族调用号 R1..R4=参数 | 随调用号（`VmlCallRegistry`） |

⚠ `CALL_JSON` 一次要过两趟 JSON + 一次内存拷贝 —— **绘图/输入这类每帧调用别走它**，专用号更快也更明确。

### 随机数与计时（游戏要用）

这两个**不在 500–599 号段**里，走的是 VM 自带的 `#50` / `#53`。放在共享库里，是因为
只有 C 能直接调 syscall，其它前端自己搓不出随机数（只能固定出子顺序）。

| 函数 | 说明 |
|---|---|
| `int ui_rand(int n)` | 0..n-1 的随机数（n ≤ 0 返回 0） |
| `int ui_tick(void)` | 自 VM 启动起的毫秒数（单调递增） |

## 最小可运行示例（VML 汇编）

开一个窗口，画一个实心圆 + 一行字，等按键，然后关窗：

```asm
.entry main
.stack 256
.vectors 0x0

.data
title:  .string "演示窗口"
hello:  .string "按方向键退出"
msg:    .string "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"   ; 16 字节消息缓冲

.text
main:
        ; WIN_OPEN(title, 320, 240)
        LEA R0, title
        MOVE R1, #320
        MOVE R2, #240
        SYSCALL #520

        ; DRAW_CLEAR(#FF101020)
        LOAD R0, #0xFF101020
        SYSCALL #522

        ; DRAW_CIRCLE(160, 120, 60, #FF00C8FF, 填充=1, 线宽=0)
        MOVE R0, #160
        MOVE R1, #120
        MOVE R2, #60
        LOAD R3, #0xFF00C8FF
        MOVE R4, #1
        MOVE R5, #0
        SYSCALL #526

        ; DRAW_TEXT(160, 20, hello, #FFFFFFFF, 16, 锚点=居中)
        MOVE R0, #160
        MOVE R1, #20
        LEA R2, hello
        LOAD R3, #0xFFFFFFFF
        MOVE R4, #16
        MOVE R5, #1
        SYSCALL #528

        ; 主循环：等一条消息，收到 WindowClose 就退出
        ; ⚠ MSG 是"一个号 + 操作码"：op 占 R0，参数从 R1 起（老写法 #561 已不存在）
wait:
        MOVE R0, #1            ; op = 1 → MSG_WAIT
        LEA R1, msg
        MOVE R2, #0            ; 0 = 无限等
        SYSCALL #560
        CMP R0, #10            ; WindowClose
        JE  done
        JMP wait

done:
        MOVE R0, #0
        SYSCALL #3             ; exit(0)
```

> ⚠ 上面用的是 VML 汇编的指令写法（`LEA`/`LOAD`/`MOVE`/`SYSCALL`/`CMP`/`JE`）。
> 具体语法以 `docs/VML_ISA_SPEC.md` 为准；本示例的价值在于**把号段、参数顺序、消息缓冲的用法
> 串成一条完整可跑的路径**。

## C 侧约定（实测校准，不是推测）

C 里调这套 syscall 的**唯一可靠**写法是 `${名}` 占位符，包装库见 **`Lib/c/waycoder_ui.h`**
（头文件自带实现，`#include` 即可用）：

```c
#include <waycoder_ui.h>

int main(void) {
    int msg[4];
    ui_win_open("演示", 360, 620);
    ui_clear(0xFF101020);
    ui_circle(160, 120, 60, 0xFF00C8FF, 1, 0);
    ui_text(160, 20, "按方向键退出", 0xFFFFFFFF, 16, 1);
    ui_present();

    while (ui_win_closed() == 0) {
        if (ui_wait(msg, 0) != VML_MSG_TOUCHDOWN) continue;
        /* msg[1]=x msg[2]=y，单位与绘图一致 */
    }
    return 0;
}
```

### 参数：`${名}` 占位符

`GenerateAsmStatement` 会把 `${名}` 按出现顺序换成 R0、R1、R2…，并在它前面补一条
`MOVE Rn, [变量槽]`：

```c
asm("SYSCALL #520, ${title}, ${w}, ${h}");   /* ✓ R0=title R1=w R2=h */
```

**不可靠的写法**（本仓库 `Lib/c/stdlib.c`、`Lib/c/time.c` 里就是这么写的，实际拿不到值）：

```c
asm("MOVE R0, s");    /* ✗ 这句文本原样进汇编器；局部变量在栈上（R12±偏移）、
                             汇编器没有它的符号表 → R0 是垃圾 */
```

`${名}` 只认**简单变量名**：`p.x`、`arr[i]`、函数名都不认，要先赋给一个变量。
全局变量也不认（它不在局部变量表里，会退化成 `R12+0`）—— 走包装函数（形参）就没这问题。

### 返回值：把 asm 当表达式用

```c
int ui_scr_w(void) { return asm("SYSCALL #566"); }     /* ✓ */
int ui_scr_w(void) { asm("SYSCALL #566"); return 0; }  /* ✗ 返回值丢了 */
```

### 校准记录

用桌面宿主 + 寄存器探针跑的最小用例（宿主在仓库根的 `.scratch/vmlhost/`，
它直接从 `third_party/vml` 的 `Lib/` 取库，不重编 VML 任何工程）：

| 用例 | 结果 |
|---|---|
| `asm("SYSCALL #510, ${s}")`，`s` 是字符串指针 | R0 → `HELLO-STRING` ✅ |
| `asm("SYSCALL #520, ${s}, ${w}, ${h}")` | R0/R1/R2 = 字符串指针/395/744 ✅ 顺序即占位符出现顺序 |
| `asm("MOVE R0, ${n}")` + `asm("SYSCALL #530")` | R0 = 12345 ✅（`${}` 在非 SYSCALL 行上也替换） |
| `r = asm("SYSCALL #540, ${n}")` | `r` = 探针回的 23130 ✅ |
| `asm("MOVE R0, s")`（直写变量名） | ❌ 拿不到值 |

## 校准过程中修掉的两个编译器 bug

这两处都在 C 前端，都属于"**编得过、跑起来才错**"，是这套 syscall 一开始"字符串传不过去"
的真正原因。修法见 `patches/0002-c-codegen-fixes.patch`（并已回灌上游 VML 仓库）。

### ① 参数寄存器过早装载（影响面更大）

cdecl 调用里，实参是从右到左"边求值边 `MOVE Ri, R0`"，而**下一次实参求值会把 R0/R1
当临时寄存器用**（表达式产物里随处可见 `PUSH R0 … POP R1`）——于是已经装好的 Ri 被冲掉。

```c
probe(p + 3 * k, q + 3 * k, 3 * k);   /* p=29 q=41 k=24 → 期望 101/113/72 */
```
修前：`R0=101 R1=101 R2=72`（第二个参数变成了第一个的值）
修后：`R0=101 R1=113 R2=72` ✅

只要实参是**带运算的表达式**、且它前面还有别的实参就会中招。改法是分两步：
先全部求值+压栈并记下各自相对 R13 的偏移，最后统一从栈上把 R0–R3 装回来。

### ② `R12-4` 撞车

`asm("SYSCALL #…")` 执行完会把 R0 存回 `[R12-4]`（asm 结果的暂存槽），
而局部变量分配器把**第一个声明的局部变量**也放在 `R12-4`。任何一条 asm 执行完
都会把它覆盖成上一条 syscall 的返回值 —— 症状正是"弹窗里的字符串大多是空的"。

修法是让 C 前端先占住 `R12-4`，用户局部变量从 `R12-8` 起（与 BasicCompiler 把
`R12-4` 到 `R12-256` 预留给临时槽的处置一致）。

> 这两处都已实测校准。**没有它们的编译器上，`waycoder_ui.h` 里的包装函数仍然可用** ——
> 那是因为包装函数只把**形参**交给 `${}`（形参在 `R12+8` 往上，不在 `R12-4`），
> 且每个函数只调一条 syscall。但**你自己的代码**如果直接用 `${局部变量}`、或写出
> 带运算的实参，就会踩到上面两条。

## 各语言的封装状态

**实现只有一份**：`Lib/shared/src/vmlui.c` → 编成 `Lib/shared/vmlui.vml`，由
`vmltool.config.xml` 挂到各语言的 `Libs` 上。各语言侧只放**声明/包装**。

| 语言 | 状态 |
|---|---|
| VML 汇编 | ✅ 上面的示例可直接跑 |
| C / C++ / ObjC | ✅ `#include <waycoder_ui.h>`（`Lib/c/`，只有声明，不增大程序）；约定与校准见上一节 |
| Python / BASIC | ✅ 同一份 `vmlui.vml`（`vmltool.config.xml` 里已挂）—— 这两个前端**不能用内联 asm**，只能按标签调 C 函数，`Lib/shared/src/vmlui.c` 就是为它们写的（另有 `ui_wait_msg`/`ui_msg_a`/`ui_gset` 这类不碰指针的接口） |
| 其它前端 | 未做（做法同上：加声明 + 确保 `vmlui.vml` 在该语言的 `Libs` 里） |

> ⚠ 本目录**已经与上游 VML 分家**（2026-09-17 起是"移动设备手机端专用"的独立分支），
> `sync.sh` 与它的 `WAYCODER_VML_FORCE_SYNC` 逃生口**都已删除** —— 不存在任何同步通道，
> 因此**不存在"改完还要回灌上游/做成补丁"这一步**，直接改就是最终状态。
> 分家原因与 35 个 csproj 的手工适配清单见 `third_party/vml/FORK.md`。

## 这份表怎么保证不烂

接口号是**手工**加进 `VmlUi` 的（AOT 禁反射 ⇒ 运行时枚举不出来），而这份文档是它的第 4 份抄本。
**抄本会漂，而且漂得没有声音** —— 本文件就曾同时犯三种错：列着 3 个**已经被合并掉的号**
（561/562/564，照它写程序会静默失效）、漏了 35 个**活着的号**、示例里还在用老号。

所以加了 [号段文档同步] 自测（`WayCoder/Test/SelfTest.Chunk26.cs`）：
**`VmlUi.AllNumbers` 里的每一个号都必须在本文件里出现**，反之本文件出现的 5xx 号也必须是活号。
漏一个就红。**它只能保证"号没漏"，保证不了"参数写得对"** ——
参数以 `VmlUiProtocol.cs` 的注释为准（那是唯一事实源），本文件负责"有哪些、叫什么、大致怎么用"。
