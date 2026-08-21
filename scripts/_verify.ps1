Write-Host "=== dist files ==="
Get-ChildItem 'D:\code-agents\WayCoder\dist\waycoder-*' | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host ("{0,-50} {1} MB" -f $_.Name, $size)
}
Write-Host ""
Write-Host "=== winget manifest 0.79.76 ==="
Get-ChildItem 'D:\code-agents\WayCoder\packaging\winget\manifests\a\Aleckstygit\WayCoder\0.79.76'
Write-Host ""
Write-Host "=== brew formula (first 8 lines) ==="
Get-Content 'D:\code-agents\WayCoder\packaging\brew\waycoder.rb' | Select-Object -First 8
Write-Host ""
Write-Host "=== winget installer.yaml sha256 ==="
$inst = Get-Content 'D:\code-agents\WayCoder\packaging\winget\manifests\a\Aleckstygit\WayCoder\0.79.76\Aleckstygit.WayCoder.installer.yaml' | Out-String
Write-Host $inst
