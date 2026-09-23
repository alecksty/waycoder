' KNOWN-RED（已确认、尚未修）：**`AND`/`OR` 连接两个浮点比较时结果错**。
'
' 两个比较各自是对的（`IF B# >= A#` 与 `IF B# <= A# + 2` 单独写都成立），
' 但用 `AND` 连起来之后整体判成假 —— 因为 `AND`/`OR` 是靠 **CPU 标志位**串起来的，
' 而第一个 `DCMP` 置的标志在求第二个比较数时**被覆盖**了
' （浮点比较走 `DCMP`，跳转读 `zf/sf/cf`；两次比较之间没有"把结果落成整数"这一步）。
'
' 实测原型：GORILLA.BAS 的 `PlotShot`
'   `IF (x# >= ScrWidth - Scl(10)) OR (x# <= 3) OR (y# >= ScrHeight - 3) THEN OnScreen = FALSE`
' —— 三个浮点比较一 `OR`，香蕉**第一步就判成"出屏"**、整个飞行只剩一帧
' （实测探针读到的 `OnS=0`，而 x#/y# 其实都在屏内）。
'
' 修法方向（未做）：`AND`/`OR` 的每个操作数都要**先物化成一个整数**（0 / -1）再组合，
' 而不是让两次比较共用一组标志位。修好那天把 KNOWN-RED 摘掉即可。
' EXPECT: A=1|B=1|C=1
A# = 0
B# = 0
IF B# >= A# THEN PRINT "A=1"
IF B# <= A# + 2 THEN PRINT "B=1"
IF B# >= A# AND B# <= A# + 2 THEN PRINT "C=1"
END
