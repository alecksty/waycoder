# apt 发布（v0.96.7）

WayCoder 的 Linux 二进制通过 Debian `.deb` 包 + apt 仓库分发。

## 现状（v0.96.7）

`.deb` 已打包并上传 release（**GitHub 双架构**；**Gitee 因仓库附件配额 1GB 已满仅 amd64**）：

| 包 | 架构 | 位置 |
|----|------|------|
| `waycoder_0.96.7_amd64.deb` | amd64 | GitHub + Gitee release |
| `waycoder_0.96.7_arm64.deb` | arm64 | GitHub release |

> 免仓库直接安装：
> `wget https://github.com/alecksty/waycoder/releases/download/v0.96.7/waycoder_0.96.7_amd64.deb && sudo dpkg -i waycoder_0.96.7_amd64.deb`

**apt 仓库已构建并验证**（`apt-ftparchive` 手搓，无需 reprepro/sudo，见下方「一键构建脚本」）：

- 签名 GPG 密钥：`E7481814D92CD89A`（空口令，供自动签名）
- 结构：`dists/stable/{Release,InRelease,Release.gpg}` + `main/binary-{amd64,arm64}/Packages(.gz)` + `pool/main/w/waycoder/*.deb` + `waycoder.gpg`
- 验证：`gpg --verify` → Good signature；WSL 内 `apt-get update` + `apt-get install -s waycoder` dry-run 通过
- 部署文件已推 Gitee 分支 **`apt-pages`**（仅供 Gitee Pages 部署，不参与 hasee/mac/master 代码分支）

### 上线 Gitee Pages（需人工操作一次）

1. Gitee 账号**实名认证**（Gitee 个人资料 → 实名认证）
2. 仓库 `way-coder` → 服务 → **Gitee Pages** → 部署分支选 **`apt-pages`**，目录 `/`，强制 HTTPS
3. 上线后用户配置（URL 以 Pages 实际地址为准）：

```bash
sudo curl -fsSL https://aleckstygit.gitee.io/way-coder/waycoder.gpg -o /usr/share/keyrings/waycoder.gpg
echo "deb [signed-by=/usr/share/keyrings/waycoder.gpg] https://aleckstygit.gitee.io/way-coder/ stable main" \
  | sudo tee /etc/apt/sources.list.d/waycoder.list
sudo apt-get update && sudo apt-get install waycoder
```

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
