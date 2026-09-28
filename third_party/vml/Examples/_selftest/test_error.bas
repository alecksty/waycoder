' test_error.bas —— VML 跨语言「诊断」判据（**故意编不过**）
' test_error.bas -- VML cross-language "diagnostics" probe (**deliberately does not compile**)
'
' ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
' ⚠ This is a **deliberate diagnostic case**; it takes part in no "compile-cleanliness" check
'   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
'   (the byte-for-byte comparison in `vml-out-probe` runs against the out.* files in this same directory -- do not mix this file into it).
' 用途：验证前端是否报出 error / warning、诊断是否带
' Purpose: verify whether the frontend reports error / warning, whether a diagnostic carries
'   `文件:行:列: level: 消息 [诊断码]` 四要素、以及编辑器能否据此画气泡。
'   the four fields `file:line:col: level: message [diag-code]`, and whether the editor can draw a squiggle from it.
'
' 期望产出：1 个 error（未声明的变量）+ 1 个 warning（CIRCLE 画弧未实现）。
' Expected output: 1 error (undeclared variable) + 1 warning (CIRCLE arc drawing is not implemented).
' QBasic 默认「未声明即隐式全局」（值为 0）⇒ 那种引用**只警告不报错**；
' QBasic defaults to "undeclared means implicit global" (value 0) => such a reference **only warns, it does not error**;
' 写上 `OPTION EXPLICIT` 才升级成错误 —— 本文件要的是**错误**，所以写了它。
' only `OPTION EXPLICIT` upgrades it to an error -- this file wants the **error**, so it is there.
' 注意：**不能**写 `DIM x AS INTEGER` —— 在 OPTION EXPLICIT 下 DIM 自己会被
' Note: you **cannot** write `DIM x AS INTEGER` -- under OPTION EXPLICIT the DIM itself gets
' 误报成「未声明的变量」（前端缺陷，与本用例无关），那会把判据搅浑。
' misreported as an "undeclared variable" (a frontend defect, unrelated to this case), which would muddy the probe.
OPTION EXPLICIT

PRINT "hi"                        ' 正常语句，不该报任何东西
' a normal statement; it should report nothing
CIRCLE (100, 100), 50, 0, 3.14    ' warning: 起始/结束角（画弧）未实现，落回整圆
' warning: start/end angle (arc drawing) is not implemented, it falls back to a full circle
PRINT undefined_thing             ' error: 未声明的变量（就是它让编译失败）
' error: undeclared variable (this is the one that makes the compile fail)
