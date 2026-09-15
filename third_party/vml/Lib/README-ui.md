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

### 输入（统一消息队列）

键盘、鼠标、触摸、定时器、窗口事件**进同一个队列**，程序统一按「取消息 → 分派」写循环。

| 号 | 名称 | 参数 | 返回 |
|---|---|---|---|
| 560 | `MSG_POLL` | R0=消息缓冲地址 | 消息类型，无消息 0 |
| 561 | `MSG_WAIT` | R0=缓冲地址 R1=超时毫秒(0=无限) | 消息类型，超时 0 |
| 562 | `MSG_COUNT` | — | 待处理条数 |
| 563 | `TIMER_SET` | R0=间隔毫秒 R1=用户标记 | 定时器 id |
| 564 | `TIMER_KILL` | R0=id | 0 |
| 565 | `WIN_CLOSED` | R0=句柄 | 1=用户关了窗口（程序应退出主循环） |

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
wait:
        LEA R0, msg
        MOVE R1, #0            ; 0 = 无限等
        SYSCALL #561
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

## 各语言的封装状态

| 语言 | 状态 |
|---|---|
| VML 汇编 | ✅ 上面的示例可直接跑 |
| C | ⏳ 待补。C 前端支持 `asm("SYSCALL #N")`，但**参数怎么进 R0…Rn** 有不止一种写法（语料里 `${var}` 占位符与"靠前一条语句留下的 R0"两种并存），需真机实验确认后再定头文件写法 —— 不确认就发出去，只会让人拿到一个"编译过了但参数全是垃圾"的头文件。 |
| 其它前端 | 未做 |
