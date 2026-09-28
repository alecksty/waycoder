# vml 命令

`vml` 是内置的 VML 程序工具，在**命令行页**敲。

## 子命令

```
vml              看用法
vml test         跑内置的自检程序（几秒钟，验证环境是否正常）
vml run <文件>    编译并运行
vml make <.vmk>  按**工程文件**编译，只出产物、不运行
```

## `vml run` 会**自动识别**文件类型

| 你给的文件 | 它怎么处理 |
|---|---|
| `xxx.c` `xxx.py` `xxx.lua` …（22 种语言的源码） | 用对应前端**编译**，再链接标准库、运行 |
| `xxx.vml` | 这是**文本汇编**，直接汇编后运行 |
| `xxx.vmb` | 这是**二进制字节码**，直接装载运行（最快，跳过编译与汇编） |

所以三级产物各有各的用法：

```
源码 .c  ──编译──▶  .vml  ──汇编──▶  .vmb
         (vml run)        (文件页「VML 编译」)
```

**同一个程序，`.vmb` 启动最快** —— 手机上尤其明显。

## `vml make`：多文件程序要写工程文件

`vml run` 一次只编**一个**源文件。文件不止一个时（`main.c` + `util.c` + …），
写一个 `.vmk` 工程文件把关系说明白，再 `vml make`：

```xml
<VMLProject Version="1">
  <Entry>src/main.c</Entry>         <!-- 含 main 的那个 -->
  <Sources>
    <File>src/util.c</File>         <!-- 其余编译单元 -->
  </Sources>
  <Includes><Dir>src</Dir></Includes>   <!-- 相当于 -I -->
  <Defines><Define Name="VERSION" Value="1.2"/></Defines>  <!-- 相当于 -D -->
</VMLProject>
```

```
vml make proj.vmk        # 产物默认落在入口旁边（src/main.vml），再 vml run 它
```

三条要记住的：

* **只出产物、不运行** —— 要跑再 `vml run` 那个产物（构建与运行分开，批量/CI 才好用）；
* `<Output Format="…">` 认得出 `bin`/`elf`/`hex` 这些名字，但**目前只做得出 `vml` 和 `vmb`**，
  写了别的会**明确报错**，不会偷偷给你一个 `.vml`；
* 工程文件里**不认识的元素会报错** —— 打错字不会静默忽略。

手头有 Makefile 的话，可以让它转一次：`vmlcli make --import Makefile`（**桌面端**命令），
之后以 `.vmk` 为准。完整格式见仓库的 `docs/VML工程文件.md`。

## 例子

```
vml test
vml run examples/c/gomoku.c
vml run examples/python/tetris.py
vml run ~/main.vmb
vml make ~/proj.vmk
```

## 跑不动 / 报错怎么办

1. 先 `vml test` —— 它过了说明环境没问题，问题在你的程序
2. 看报错里有没有**行号**，回去改那一行
3. 见「VML 编译器 → 常见错误」
