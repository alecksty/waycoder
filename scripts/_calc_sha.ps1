$dist = 'D:\code-agents\WayCoder\dist'
$rids = @('win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64')
$VER = 'v0.79.76'
$Sha = @{}
foreach ($rid in $rids) {
    $ext = if ($rid -like 'win-*') { 'zip' } else { 'tar.gz' }
    $f = Join-Path $dist "waycoder-$VER-$rid.$ext"
    $h = (Get-FileHash $f -Algorithm SHA256).Hash.ToLowerInvariant()
    $Sha[$rid] = $h
    Write-Host ("{0,-12} {1}" -f $rid, $h)
}
Write-Host ''
Write-Host '=== PowerShell variables ==='
foreach ($rid in $rids) { Write-Host "`$Sha['$rid'] = '$($Sha[$rid])'" }
