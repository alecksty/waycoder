' `PALETTE idx, color` 两参形式的**最小复现** —— 判据是三个色块的实际颜色。
'
' 这是 `GORILLAS.BAS` 的 `SetScreen` 的缩减版（它用 EGA 64 色号 1/46/44/54/7/4/3/63）：
' 三条 `PALETTE` 里**只有颜色号恰好小于 16 的那条**对，其余静默串到 `color & 15` 那一项上。
'
' 详细机制见 `palette.sh` 的文件头（以及 CHANGELOG v0.96.407 的"仍未解决"段）。
'
' ⚠ 三个色块的坐标与 `palette.sh` 里的判据一一对应：改这里要同步改那边。
'    (50,30)  ← PALETTE 0, 1
'    (50,90)  ← PALETTE 1, 46
'    (50,150) ← PALETTE 3, 54
SCREEN 9
CLS
PALETTE 0, 1
PALETTE 1, 46
PALETTE 3, 54

LINE (0, 0)-(639, 349), 0, BF
LINE (10, 10)-(100, 60), 1, BF
LINE (10, 70)-(100, 120), 3, BF
