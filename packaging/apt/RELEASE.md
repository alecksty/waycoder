# apt 发布

WayCoder 的 Linux 二进制通过 Debian `.deb` 包 + apt 仓库分发。

## 现状（v0.96.36 起）

**apt 仓库已上线 GitHub Pages**，`apt-get install waycoder` 可用：

- 仓库 URL：`https://alecksty.github.io/waycoder/`（`alecksty/waycoder` 仓库的 Pages，源指向 gh-pages 分支）
- 内容：v0.96.36 amd64 + arm64（`pool/main/w/waycoder/`）
- 签名：GPG `E7481814D92CD89A`（空口令，自动签名），`gpg --verify` Good
- 结构：`dists/stable/{Release,InRelease,Release.gpg}` + `main/binary-{amd64,arm64}/Packages(.gz)` + `pool/` + `waycoder.gpg`

用户安装（见 `docs/安装与升级.md`）：

```bash
sudo mkdir -p /etc/apt/keyrings
sudo curl -fsSL https://alecksty.github.io/waycoder/waycoder.gpg -o /etc/apt/keyrings/waycoder.gpg
echo "deb [signed-by=/etc/apt/keyrings/waycoder.gpg] https://alecksty.github.io/waycoder stable main" \
  | sudo tee /etc/apt/sources.list.d/waycoder.list
sudo apt update && sudo apt install waycoder
```

## 发布新版本流程

```bash
# 1. 打包 .deb（WSL 有 dpkg-deb）
./scripts/package.sh linux-x64 linux-arm64
./packaging/apt/build-deb.sh dist/linux-x64/waycoder <VER> amd64
./packaging/apt/build-deb.sh dist/linux-arm64/waycoder <VER> arm64

# 2. 在 WSL 持久路径重建仓库（/tmp 会被清）
#    git worktree add ~/aptw gh-pages → pool 拷入新 deb → 重建 Packages/Release → 签名
#    具体步骤见下方「一键构建仓库」+ 记忆 release-v09636-wsl-linux-aot

# 3. 推 gh-pages 分支（--force，部署分支）→ GitHub Pages 自动更新（.nojekyll 已加，免重建）
# 4. 切 Pages 源后若未重建：向 gh-pages 推提交触发
```

## 一键构建仓库（Linux/WSL，无需 root）

```bash
# 前提：gpg + apt-ftparchive（Ubuntu WSL 自带）
# 1) 签名密钥（已存在，无需重复生成）：
#    gpg --batch --pinentry-mode loopback --passphrase '' \
#        --quick-generate-key "Aleckstygit (WayCoder apt repo) <aleckstygit@outlook.com>" rsa2048 sign 0

# 2) 打包 .deb
./scripts/package.sh linux-x64 linux-arm64
./packaging/apt/build-deb.sh dist/linux-x64/waycoder <VER> amd64
./packaging/apt/build-deb.sh dist/linux-arm64/waycoder <VER> arm64

# 3) 重建仓库（关键命令，避免踩坑）：
#    - apt-ftparchive generate 的 BinDirectory 同目录多块后者覆盖、且 Arch 只标注不过滤
#      → 必须用 apt-ftparchive packages pool/ 出全量 + awk 按 Architecture 拆两个 Packages
#    - apt-ftparchive release -c apt-ftparchive.conf dists/stable > Release
#    - 签名（--yes 覆盖旧文件，空口令）：
#      gpg --batch --pinentry-mode loopback --passphrase '' --default-key E7481814D92CD89A \
#          --clearsign -o InRelease Release
#      gpg --batch --pinentry-mode loopback --passphrase '' --default-key E7481814D92CD89A \
#          -abs -o Release.gpg Release
#    - waycoder.gpg 必须是二进制 keyring（gpg --dearmor），ASCII 装甲会报 NO_PUBKEY

# 4) 部署：把 dists/ pool/ waycoder.gpg 提交推 alecksty/waycoder 的 gh-pages 分支（--force）
```

## 历史

| 版本 | 状态 |
|------|------|
| v0.96.36 | apt 仓库上线 GitHub Pages（amd64+arm64）|
| v0.96.7 / v0.96.8 | 曾推 Gitee `apt-pages` 分支等 Gitee Pages——Gitee Pages 2026 停服，路线废弃 |

## 备注

- **Gitee Pages 已停服**（2026），apt 仓库走 GitHub Pages；国内需能访问 github.com
- `waycoder.gpg` 必须是二进制 keyring（`gpg --dearmor`），apt `signed-by=` 不认 ASCII 装甲
- 无签名仓库（不推荐）：sources.list 用 `deb [trusted=yes] https://<host>/ stable main`
