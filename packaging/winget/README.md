# winget 分发

WayCoder 的 Windows 便携版（`waycoder.exe` 单文件）通过 winget 分发。

## 提交到 winget-pkgs

1. 用 `scripts/package.ps1 win-x64 win-arm64` 打包出两个 zip。
2. 计算 sha256：
   ```powershell
   Get-FileHash .\dist\waycoder-v0.48.7-win-x64.zip -Algorithm SHA256
   ```
3. 替换 `manifests/.../Aleckstygit.WayCoder.installer.yaml` 里的 `REPLACE_WITH_SHA256`。
4. fork `microsoft/winget-pkgs`，按 `manifests/a/Aleckstygit/WayCoder/0.48.7/` 目录结构提交 PR。

## 本地测试（免提交）

```powershell
winget install --manifest .\packaging\winget\manifests\a\Aleckstygit\WayCoder\0.48.7\
```

## 校验

```powershell
winget validate .\packaging\winget\manifests\a\Aleckstygit\WayCoder\0.48.7\
```
