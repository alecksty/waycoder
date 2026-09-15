# 示例程序

手机 App 启动时会把这些示例解包到 **`<工作区>/examples/`**（`MauiBootstrap.EnsureExamples`），
在「文件」页点开就能编译运行：`vml run examples/xxx.c`。

## 可以直接玩的

| 文件 | 说明 |
|---|---|
| `c/gomoku.c` | **五子棋**（人机对战）。点棋盘落子，最后一手有记号，结束点任意处再来一局。 |
| `c/tetris.c` | **俄罗斯方块**。方向键移动、`旋转`、`直落`、`暂停`/`重开`；长按方向键可连发。 |

两个都用 `Lib/c/waycoder_ui.h` 那套接口（窗体 / 绘图 / 触摸与按键消息 / 定时器），
界面全用绘图指令画出来，**不依赖任何图片资源**；棋盘与手柄尺寸按实测可用绘图区自适应，
手机、平板、模拟器上都能铺满。

## 暂时跑不了的（**已知**，别当新问题查）

| 文件 | 卡在哪 |
|---|---|
| `basic/tetris.bas` | BASIC 前端三个既有缺陷：SUB 内局部 `FOR` 循环死循环、SUB 内局部数组赋值读回 0、FUNCTION+SUB 组合挂起（回归语料见仓库 `scripts/basic-tests/` 的 t9/t10/t11）。`draw_board` 整片都是 SUB 内局部 FOR，一个都绕不过去。 |
| `python/tetris.py` | Python 前端的列表**读可以、写不生效**（`b[i] = v` 之后读回还是 0），棋盘放不进去。 |

这两个游戏先用 C 版交付；那几处前端缺陷要单独修。

## 想在桌面看效果

桌面没有 500–599 那套 UI 号段，但仓库里的脚手架 `vmlhost`（`.scratch/vmlhost`）能无头跑：

```bash
# 脚本化触摸 + 重力节拍，并把每一帧走**和手机同一条渲染链**出成 PNG（可以直接看图验收）
dotnet run -c Release --project .scratch/vmlhost -- run Examples/c/tetris.c \
    --sim "t;t;87,671;297,639" --frames /tmp/out

vmlhost bench     # 量每帧的出图成本
vmlhost font      # 字体自检：挑中哪个字体、中英推进量、出样张
```
