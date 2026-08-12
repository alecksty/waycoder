$p = 'D:\code-agents\WayCoder\WayCoder\games\roguelike\UI\Renderer.cs'
$b = [System.IO.File]::ReadAllBytes($p)
# 目标：把 '锟斤拷' (UTF-8 字节 E9 94 9F E6 96 A4 E6 8B B7) 替换为 GBK 的 '─' (C4 FE)
$old = [byte[]](0x27, 0xE9, 0x94, 0x9F, 0xE6, 0x96, 0xA4, 0xE6, 0x8B, 0xB7, 0x27) # '锟斤拷' with quotes
$new = [byte[]](0x27, 0xC4, 0xFE, 0x27) # '─' in GBK
$idx = -1
for ($i = 0; $i -le $b.Length - $old.Length; $i++) {
    $m = $true
    for ($j = 0; $j -lt $old.Length; $j++) {
        if ($b[$i + $j] -ne $old[$j]) { $m = $false; break }
    }
    if ($m) { $idx = $i; break }
}
if ($idx -lt 0) {
    # 备用方案：搜 E9 94 9F E6 96 A4 E6 8B B7（不带引号）
    $old2 = [byte[]](0xE9, 0x94, 0x9F, 0xE6, 0x96, 0xA4, 0xE6, 0x8B, 0xB7)
    $idx2 = -1
    for ($i = 0; $i -le $b.Length - $old2.Length; $i++) {
        $m = $true
        for ($j = 0; $j -lt $old2.Length; $j++) {
            if ($b[$i + $j] -ne $old2[$j]) { $m = $false; break }
        }
        if ($m) { $idx2 = $i; break }
    }
    if ($idx2 -lt 0) { Write-Output 'SEARCH FAILED'; exit 1 }
    $new2 = [byte[]](0xC4, 0xFE)
    $out = New-Object byte[] ($b.Length - $old2.Length + $new2.Length)
    [Array]::Copy($b, 0, $out, 0, $idx2)
    [Array]::Copy($new2, 0, $out, $idx2, $new2.Length)
    [Array]::Copy($b, $idx2 + $old2.Length, $out, $idx2 + $new2.Length, $b.Length - $idx2 - $old2.Length)
    [System.IO.File]::WriteAllBytes($p, $out)
    Write-Output ('REPLACED (fallback) at ' + $idx2)
    exit 0
}
$out = New-Object byte[] ($b.Length - $old.Length + $new.Length)
[Array]::Copy($b, 0, $out, 0, $idx)
[Array]::Copy($new, 0, $out, $idx, $new.Length)
[Array]::Copy($b, $idx + $old.Length, $out, $idx + $new.Length, $b.Length - $idx - $old.Length)
[System.IO.File]::WriteAllBytes($p, $out)
Write-Output ('REPLACED at ' + $idx)
