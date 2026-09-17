# 输出自测（`vml-out-probe`）

同一件事在 22 门语言里各写一遍：**打印三行**，逐字节比对。

```
OUT-STR=abc
OUT-INT=42
OUT-PUN=hello, world
```

第三行刻意带空格与逗号 —— 最容易暴露「字符串被截断 / 转义被吃掉 / 按空白切词」，
而骨架里那句 `SKEL-SUM=14`（无空格、不换行）恰好照不出来。

## 在手机上跑

命令行页逐条敲（`~/examples/_selftest/`）：

```
vml run examples/_selftest/out.c
vml run examples/_selftest/out.py
vml run examples/_selftest/out.rs
...
```

桌面上一键跑全套（判据在 `scripts/vml-out-probe/run-langs.sh`）：

```
scripts/vml-out-probe/run-langs.sh
```

> ⚠ 这个目录是**自测**，不是示例教程 —— 放这里只是因为它必须随包进手机。
> 语言识别靠扩展名，`make-vml-lib.sh` 按 `Examples/<子目录>/<文件>` 二层收，正好进包。
