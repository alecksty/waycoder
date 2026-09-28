# 22 种语言

同一套 VML 接口，22 个前端都能用。**你熟悉哪门就用哪门写**，编出来的东西跑在同一台虚拟机上。

## 每种语言一份说明

点进任意一门，里面有它的**语言规范**（语法/类型/标准库）、**这个前端支持什么**、
以及**实测踩过的坑** —— 后两类是直接从 VML 源码各编译器的 `README.md` /
`<语言>_LANGUAGE_SPEC.md` 带过来的，上游一改、这里重新生成就是最新的。

| 语言 | 一句话 |
|---|---|
| [C](help:vml/lang/c) | «bold»最完整的一条路«/»：绘图、输入、音效、存档、手柄全都用得上。仓库里那几个完整的游戏（俄罗斯方块、五子棋、吃豆人）都是 C 写的。 |
| [C++](help:vml/lang/cpp) | 与 C 同一套接口，可以写类。适合把游戏逻辑整理成对象。 |
| [C#](help:vml/lang/csharp) | 写得像桌面 C#，入口固定是一个叫 `P` 的类。 |
| [Objective-C](help:vml/lang/objc) | 语法是 C 的超集，UI 接口用法与 C 完全相同。 |
| [Java](help:vml/lang/java) | 写法与桌面 Java 接近，外部函数用 `native` 声明。 |
| [Kotlin](help:vml/lang/kotlin) | 与 Java 同一套写法，语法更短。 |
| [Swift](help:vml/lang/swift) | 写法自然，示例里那个飞机大战是完整可玩的。 |
| [Go](help:vml/lang/go) | 写法与桌面 Go 接近，示例里的贪吃蛇是完整游戏。 |
| [Rust](help:vml/lang/rust) | 能写，但别指望完整的标准库。 |
| [D](help:vml/lang/d) | C 风格的语法，写起来比 C 宽松一些。 |
| [Dart](help:vml/lang/dart) | 写法接近，外部函数用 `external` 声明。 |
| [Python](help:vml/lang/python) | «bold»写起来最快的一门«/»。语法几乎就是桌面 Python，改一行跑一次很舒服。 |
| [JavaScript](help:vml/lang/javascript) | 写法接近，但入口要自己调一次。 |
| [Lua](help:vml/lang/lua) | 轻快的小语言，写小游戏很舒服，编译也快。 |
| [Ruby](help:vml/lang/ruby) | 能写，但这个前端支持的特性最少 —— «bold»完全平铺着写«/»。 |
| [R](help:vml/lang/r) | 向量语言，写法与桌面 R 接近。 |
| [Pascal](help:vml/lang/pascal) | 结构清楚，写游戏状态机很合适。 |
| [Fortran](help:vml/lang/fortran) | 科学计算那套写法在这里也能用。 |
| [BASIC](help:vml/lang/basic) | 老式写法，命令式一行一行往下走。 |
| [Forth](help:vml/lang/forth) | 栈式语言，写起来完全是另一种思路。 |
| [Scheme](help:vml/lang/scheme) | Lisp 方言，括号就是一切。 |
| [Ladder](help:vml/lang/ladder) | 梯形图（PLC）风格的前端 —— 它«bold»做不了手机界面程序«/»。 |

## 先跑一个看看

```
vml run examples/c/tetris.c        # 俄罗斯方块
vml run examples/python/tetris.py  # 同一个游戏，Python 版
vml run examples/c/gomoku.c        # 五子棋
vml run examples/lua/life.lua      # 生命游戏
vml run examples/basic/whack.bas   # 打地鼠
```

每门语言目录下还有一个 `sysinfo.<扩展名>`，它调用 `ui_call_json("sysinfo", "")`
把设备信息打出来 —— **想知道这门语言能不能用，先跑它**。
