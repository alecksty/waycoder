' test_error.bas —— VML 跨语言「诊断」判据（**故意编不过**）
'
' ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
'   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
' 用途：验证前端是否报出 error / warning、诊断是否带
'   `文件:行:列: level: 消息 [诊断码]` 四要素、以及编辑器能否据此画气泡。
'
' 期望产出：1 个 error（未声明的变量）+ 1 个 warning（CIRCLE 画弧未实现）。
' QBasic 默认「未声明即隐式全局」（值为 0）⇒ 那种引用**只警告不报错**；
' 写上 `OPTION EXPLICIT` 才升级成错误 —— 本文件要的是**错误**，所以写了它。
' 注意：**不能**写 `DIM x AS INTEGER` —— 在 OPTION EXPLICIT 下 DIM 自己会被
' 误报成「未声明的变量」（前端缺陷，与本用例无关），那会把判据搅浑。
OPTION EXPLICIT

PRINT "hi"                        ' 正常语句，不该报任何东西
CIRCLE (100, 100), 50, 0, 3.14    ' warning: 起始/结束角（画弧）未实现，落回整圆
PRINT undefined_thing             ' error: 未声明的变量（就是它让编译失败）
