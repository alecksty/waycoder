# winget 发布（v0.69.0）

WayCoder 的 Windows 便携版通过 `microsoft/winget-pkgs` 社区仓库分发。

## 现状（已就绪）

manifest 已生成在仓库内：

```
packaging/winget/manifests/a/Aleckstygit/WayCoder/0.69.0/
├── Aleckstygit.WayCoder.yaml              # version
├── Aleckstygit.WayCoder.locale.zh-CN.yaml # defaultLocale
└── Aleckstygit.WayCoder.installer.yaml    # installer（Gitee URL + 真实 sha256）
```

- `InstallerUrl` 指向 Gitee 可预测 release URL（`https://gitee.com/aleckstygit/my-coder/releases/download/v0.69.0/waycoder-v0.69.0-win-x64.zip` / `-win-arm64.zip`）
- `InstallerSha256` 已填真实值（`588e8c0d...` / `fcfe3e14...`）
- 域名启发式校验：`InstallerUrl`（gitee.com）与 `PackageUrl`/`PublisherUrl`（gitee.com）**一致**，通过自动化域名匹配的概率高

## 前提

- **Windows 机器**（`winget validate` / `winget install` 测试）
- **GitHub 访问**（fork + PR，若本机 `github.com` 不通需开代理）
- 已登录 `gh`（`gh auth login`）

## 步骤

### 1. 本地校验（Windows）

```powershell
winget validate .\packaging\winget\manifests\a\Aleckstygit\WayCoder\0.69.0\
```

### 2. 本地免提交安装测试（Windows）

```powershell
winget install --manifest .\packaging\winget\manifests\a\Aleckstygit\WayCoder\0.69.0\
```

### 3. fork + 提交 PR

```bash
# fork microsoft/winget-pkgs 并 clone
gh repo fork microsoft/winget-pkgs --clone
cd winget-pkgs

# 复制 manifest（按目录结构，版本号目录 0.69.0）
cp -r <waycoder-repo>/packaging/winget/manifests/a/Aleckstygit/WayCoder/0.69.0 \
      manifests/a/Aleckstygit/WayCoder/

git checkout -b aleckstygit-waycoder-0.69.0
git add manifests/a/Aleckstygit/WayCoder/0.69.0
git commit -m "New version: Aleckstygit.WayCoder version 0.69.0"
git push --set-upstream origin aleckstygit-waycoder-0.69.0

gh pr create \
  --title "New version: Aleckstygit.WayCoder version 0.69.0" \
  --body "更新 WayCoder 到 0.69.0（Web 聊天界面完善 + 多模态上传 + Diff 预览）。下载源为 Gitee Release（国内快）。"
```

### 4. PR 审核要点

- 首次提交可能需要签署 [Contributor License Agreement](https://cla.opensource.microsoft.com/microsoft/winget-pkgs)。
- 若审核者质疑 Gitee 域名：说明 WayCoder 项目主页与发布源均为 Gitee（`https://gitee.com/aleckstygit/my-coder`），域名与 `PackageUrl` 一致。
- 若坚持要求 GitHub 源：见下方「方案 B」。

## 方案 B：改用 GitHub release（需代理 + GitHub Actions）

若审核要求 `InstallerUrl` 用 GitHub 域名，需：

1. 代理访问 GitHub → 推 `v0.69.0` 标签触发 `.github/workflows/release.yml` 构建
2. 等 release 就绪后，把 `installer.yaml` 的 `InstallerUrl` 前缀改回 `https://github.com/alecksty/waycoder/releases/download/v0.69.0/`，`InstallerSha256` 换成 Actions 产物的 sha256
3. 重新走步骤 3 提交 PR

> 注：GitHub release 产物的 sha256 与本地编译产物**不同**（NativeAOT 受工具链版本影响），须用 Actions 产物重算，不可沿用本仓库当前 sha256。
