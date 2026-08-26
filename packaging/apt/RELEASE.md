# apt 发布（v0.96.8）

WayCoder 的 Linux 二进制通过 Debian `.deb` 包 + apt 仓库分发。

## 现状（v0.96.8）

`.deb` 已打包并上传 release：

| 包 | 架构 | 位置 |
|----|------|------|
| `waycoder_0.96.8_amd64.deb` | amd64 | GitHub + Gitee release |
| `waycoder_0.96.8_arm64.deb` | arm64 | 待构建（需 arm64 环境） |

> 免仓库直接安装（立即可用）：
> `wget https://github.com/alecksty/waycoder/releases/download/v0.96.8/waycoder_0.96.8_amd64.deb && sudo dpkg -i waycoder_0.96.8_amd64.deb`
> 国内可用 Gitee：`wget https://gitee.com/aleckstygit/way-coder/releases/download/v0.96.8/waycoder_0.96.8_amd64.deb`

**apt 仓库已重建并签名验证**（`apt-ftparchive` 手搓，无需 reprepro/sudo，见下方「一键构建脚本」）：

- 签名 GPG 密钥：`E7481814D92CD89A`（空口令，供自动签名）
- 结构：`dists/stable/{Release,InRelease,Release.gpg}` + `main/binary-{amd64,arm64}/Packages(.gz)` + `pool/main/w/waycoder/*.deb` + `waycoder.gpg`
- 内容：v0.96.7 双架构 + **v0.96.8 amd64**（已入 pool，`gpg --verify` Good）
- 部署文件已推 Gitee 分支 **`apt-pages`**（`610f945`，仅供 Gitee Pages 部署，不参与 hasee/mac/master 代码分支）

> **⚠️ 部署状态（2026-08 更新）**：**Gitee Pages 服务已停服/下架**（仓库「服务」菜单已无入口，`https://aleckstygit.gitee.io/way-coder/` 404）。因此 **apt 仓库暂无法线上托管**，当前以**免仓库 deb 直链**为准（上方命令，Gitee 直链已验证 HTTP 200 可下载）。
>
> 完整 apt 仓库（`apt-get install waycoder`）待托管点就绪后再启用：候选为 **GitHub Pages**（需 github.com 可达）或自建服务器。apt-pages 分支内容（dists/pool/waycoder.gpg）已就绪且签名 Good，届时推分支 + 托管即可。

## 一键构建 apt 仓库（Linux/WSL，无需 root）

```bash
# 前提：gpg + apt-ftparchive（Ubuntu WSL 自带）
# 1) 生成签名密钥（一次性）
gpg --batch --pinentry-mode loopback --passphrase '' \
    --quick-generate-key "Aleckstygit (WayCoder apt repo) <aleckstygit@outlook.com>" rsa2048 sign 0

# 2) 打包 .deb
cd WayCoder && ./scripts/package.sh linux-x64 linux-arm64
./packaging/apt/build-deb.sh dist/linux-x64/waycoder 0.96.7 amd64
./packaging/apt/build-deb.sh dist/linux-arm64/waycoder 0.96.7 arm64

# 3) 构建仓库（dists + pool + 签名）→ 把 dists/ pool/ waycoder.gpg 提交到 apt-pages 分支
#    核心命令：apt-ftparchive packages → Release → gpg --detach-sign / --clearsign
```

完整脚本逻辑见仓库 `apt-pages` 分支产物（`dists/`、`pool/`、`waycoder.gpg`），重新生成时用 `apt-ftparchive` 而非 reprepro（免 sudo）。

## 历史（v0.71.4）

| 包 | sha256 |
|----|--------|
| `waycoder_0.71.4_amd64.deb` | `671af5ded18a3a04910fa4abbff5ec91ddf6a9459f692c92f2f2cf530923d7d7` |
| `waycoder_0.71.4_arm64.deb` | `672f94bad2ac76d728a389835fe7e3825b726619636cb4c85b59ae36d7f6ca62` |

> 下载：`https://gitee.com/aleckstygit/my-coder/releases/download/v0.71.4/waycoder_0.71.4_amd64.deb`

## 前提

- **Linux 环境**（Debian/Ubuntu/WSL，含 `dpkg-deb`；reprepro 可选，`apt-ftparchive` 亦可）
- **GPG 密钥**（仓库签名）
- **静态托管**（Gitee Pages 或 GitHub Pages，托管 `dists/` + `pool/`）

## 备注

- **无签名仓库**（不推荐生产）：sources.list 用 `deb [trusted=yes] https://<host>/ stable main`
- `.deb` 打包脚本见 `build-deb.sh`；未来版本：`./scripts/package.sh linux-x64 linux-arm64` → `build-deb.sh` 出 deb → 重跑仓库构建 → 更新 `apt-pages` 分支
