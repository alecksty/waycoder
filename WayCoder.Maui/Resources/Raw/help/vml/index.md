# VML 是什么

**VML = 一台跑在你手机里的虚拟机**，自带一套标准库、一台汇编器、一个字节码格式。

你写的程序（C / Python / BASIC / …共 22 种语言）会被**前端编译器**编成这台虚拟机认识的
指令，再由它跑起来。所以：

- **同一种语言在这里跑，不需要装任何环境** —— 不用 gcc、不用解释器
- **一次写好，22 种语言都能编**（同一条编译链）
- 速度快、体积小（一份 hello 世界几万条指令，字节码几百 KB）

## 为什么不是直接调用系统的编译器

手机上装不了、也跑不动完整的 gcc / CPython。VML 把这些工作放到了**编译期**——
编译在前端完成，运行时只需要一台很小的虚拟机。

## 三级产物

```
你的源码 (.c)  ──前端编译──▶  .vml（文本汇编）  ──汇编──▶  .vmb（二进制字节码）
                                                                  │
                                                            装载即执行
```

| 产物 | 是什么 | 什么时候用 |
|---|---|---|
| 源码 | 你写的 | 平时改这个 |
| `.vml` | 文本汇编，能直接看、能 grep | 想看看编译器把你的代码编成了什么 |
| `.vmb` | 二进制字节码 | **要跑就加载它，最快** |

`vml run` 三种都认，自动识别。

## 能画界面、能出声、能震动

VML 程序不是只能打印文字——它有一套完整的**手机界面接口**：

- 开一个绘图窗口，画矩形 / 圆 / 路径 / 渐变 / 文字
- 收触摸、按键、定时器消息
- 合成音效（不用带音频文件）、震动、屏幕常亮
- 本地存档

已经用这套接口写了俄罗斯方块、五子棋、吃豆人、贪吃蛇、飞机大战…
都在 `examples/` 里，能直接跑。

详见「UI 开发」。

## 22 种语言，随便挑一门

同一套 VML 接口，22 个前端都能用。**你熟悉哪门就用哪门写**，编出来的东西跑在同一台虚拟机上。
每种语言的详细说明（语法规范 + 这个前端支持什么）在「关于 → 使用说明 → VML 编译器 → 22 种语言」里。

## 22 种语言，各挑一个例子跑

| 语言 | 示例 | 跑法 |
|---|---|---|
| C | `examples/c/tetris.c` `gomoku.c` `pacman.c` `mario.c` `starfall.c` | `vml run examples/c/gomoku.c` |
| C++ | `examples/cpp/snake.cpp` | `vml run examples/cpp/snake.cpp` |
| C# | `examples/csharp/snake.cs` `racer.cs` | `vml run examples/csharp/snake.cs` |
| Objective-C | `examples/objc/snake.m` | `vml run examples/objc/snake.m` |
| Java | `examples/java/catch.java` | `vml run examples/java/catch.java` |
| Kotlin | `examples/kotlin/catch.kt` | `vml run examples/kotlin/catch.kt` |
| Swift | `examples/swift/plane.swift` `snake.swift` | `vml run examples/swift/snake.swift` |
| Go | `examples/go/snake.go` | `vml run examples/go/snake.go` |
| Rust | `examples/rust/breakout.rs` | `vml run examples/rust/breakout.rs` |
| D | `examples/d/catch.d` | `vml run examples/d/catch.d` |
| Dart | `examples/dart/catch.dart` | `vml run examples/dart/catch.dart` |
| Python | `examples/python/tetris.py` | `vml run examples/python/tetris.py` |
| JavaScript | `examples/javascript/catch.js` | `vml run examples/javascript/catch.js` |
| Lua | `examples/lua/life.lua` | `vml run examples/lua/life.lua` |
| Ruby | `examples/ruby/catch.rb` | `vml run examples/ruby/catch.rb` |
| R | `examples/r/catch.r` | `vml run examples/r/catch.r` |
| Pascal | `examples/pascal/catch.pas` | `vml run examples/pascal/catch.pas` |
| Fortran | `examples/fortran/sokoban.f90` | `vml run examples/fortran/sokoban.f90` |
| BASIC | `examples/basic/whack.bas` `tetris.bas` | `vml run examples/basic/whack.bas` |
| Forth | `examples/forth/parserexp_demo.fs` | `vml run examples/forth/parserexp_demo.fs` |
| Scheme | `examples/scheme/catch.scm` | `vml run examples/scheme/catch.scm` |
| Ladder | `examples/ladder/file_io.ld` | 只能编译，见下 |

每种语言目录下还有一个 `sysinfo.<ext>`，它调用 `ui_call_json("sysinfo", "")`
把设备信息打出来 —— **想知道这门语言能不能用，先跑它**。

## 各语言怎么写

### C / C++ / Objective-C —— 最完整的一条路

```c
#include <waycoder_ui.h>

int main(void) {
    int w = ui_scr_w(), h = ui_scr_h();
    ui_win_open("演示", w, h);
    ui_clear(0xFF101020);
    ui_text(20, 40, "Hello", 0xFFFFFFFF, 20, 0);
    ui_present();
    while (ui_win_closed() == 0) { int m[4]; ui_wait(m, 0); }
    return 0;
}
```

- **编译最慢**（前端 + 链接标准库，手机上一两分钟）
- ⚠ Objective-C **不要 `#include <waycoder_ui.h>`**（前端解析不了那个头文件），
  直接调用 `ui_*` 函数即可（参考 `examples/objc/snake.m`）

### C# / Java / Kotlin / Dart —— 需要声明一下

这几个前端要求先声明外部函数：

```csharp
// C#
class P { static void Main() { ui_win_open("演示", ui_scr_w(), ui_scr_h()); } }
```
```java
// Java
static native int ui_win_open(String t, int w, int h);
```
```dart
// Dart
external int ui_win_open(String t, int w, int h);
```

### Python / Lua / JavaScript / Ruby / R —— 直接调

```python
ui_win_open("演示", ui_scr_w(), ui_scr_h())
ui_clear(0xFF101020)
ui_present()
```

⚠ **Python 的列表"写不进去"**（`b[i] = v` 之后读回来还是 0）。
棋盘这类可变网格请用共享库提供的整数网格：

```python
ui_gclear()
ui_gset(3, 1)      # 第 3 格
v = ui_gget(3)
```

### BASIC

```basic
NATIVE FUNCTION ui_win_open(t AS STRING, w AS INTEGER, h AS INTEGER) AS INTEGER
ui_win_open "打地鼠", w, h
```

⚠ 形参名不能叫 `on`（关键字）。

### Pascal

```pascal
{ 注释是 { }，不是 // }
program p;
begin
  ui_win_open('演示', ui_scr_w(), ui_scr_h());
  ui_present();
end.
```

⚠ **注释里只能写 ASCII** —— 中文标点（破折号、逗号）会报「未知字符」。
用 `//` 当注释的话整行会被当成代码。

### Forth

```forth
S" 演示" ui_scr_w ui_scr_h ui_win_open
```

### Scheme

```scheme
(ui_win_open "演示" (ui_scr_w) (ui_scr_h))
```

⚠ **用户函数看不见顶层变量**（两边的帧指针会撞）。
游戏请写成**扁平顶层程序**，只把"参数全传、不碰全局"的纯函数抽出去。

### Fortran

```fortran
call ui_win_open('演示', ui_scr_w(), ui_scr_h())
```

### Ladder —— 特殊

它是**PLC 梯形图风格**的前端，没有"带字符串参数的函数调用"这种语法，
所以 **UI 那套接口用不了**（仓库里的 `ladder/file_io.ld` 本身就是一个空测试）。
它能编译、能跑，但做不了手机界面程序。

## 共同的坑（与语言无关）

1. **先问后开**：`ui_scr_w/h` → `ui_win_open`
2. **每帧 `ui_present()`**
3. **重开一局前 `ui_msg_clear()`**
4. **定时器要 kill**
5. **数组别越界** —— 报「内存错误」十有八九是它
