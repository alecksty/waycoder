$paths = @(
    "$env:USERPROFILE\.claude\settings.json",
    "$env:APPDATA\claude\settings.json",
    "$env:LOCALAPPDATA\claude\settings.json"
)
foreach ($p in $paths) {
    $exists = Test-Path $p
    $size = if ($exists) { (Get-Item $p).Length } else { 0 }
    Write-Host "$p  =>  exists=$exists  size=$size"
}
