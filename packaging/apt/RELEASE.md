# apt 发布（v0.69.0）

WayCoder 的 Linux 二进制通过 Debian `.deb` 包 + apt 仓库分发。

## 现状（已就绪）

`.deb` 包已生成并上传 Gitee release：

| 包 | 大小 | sha256 |
|----|------|--------|
| `waycoder_0.69.0_amd64.deb` | 31M | `3e4dc510...` |
| `waycoder_0.69.0_arm64.deb` | 29M | `c857ebff...` |

> 下载：`https://gitee.com/aleckstygit/my-coder/releases/download/v0.69.0/waycoder_0.69.0_amd64.deb`
>
> 免仓库直接安装：`sudo dpkg -i waycoder_0.69.0_amd64.deb`

## 前提（apt 仓库需要 Linux 环境）

- **Linux 机器**（Debian/Ubuntu，含 `dpkg-deb` + `reprepro`）
- **GPG 密钥**（仓库签名）
- **静态托管**（Gitee Pages 或对象存储，用于托管 `dists/` + `pool/`）

## 完整步骤（在 Linux 上执行）

### 1. 安装工具

```bash
sudo apt-get update && sudo apt-get install -y reprepro dpkg-dev gnupg
```

### 2. 生成 GPG 密钥（如无）

```bash
gpg --full-generate-key   # 记下 Key ID，如 F3A2B1C4...
```

### 3. 初始化 reprepro 仓库

```bash
mkdir -p ~/waycoder-apt/conf
cat > ~/waycoder-apt/conf/distributions <<'EOF'
Origin: WayCoder
Label: WayCoder
Codename: stable
Architectures: amd64 arm64
Components: main
Description: WayCoder 官方 apt 仓库
SignWith: F3A2B1C4
EOF
```

### 4. 入库 .deb

```bash
cd ~/waycoder-apt
reprepro -b . includedeb stable waycoder_0.69.0_amd64.deb
reprepro -b . includedeb stable waycoder_0.69.0_arm64.deb
```

### 5. 导出公钥

```bash
gpg --armor --export F3A2B1C4 > ~/waycoder-apt/waycoder.gpg
```

### 6. 发布到静态托管

把 `~/waycoder-apt` 下的 `dists/`、`pool/`、`waycoder.gpg` 发布到 Gitee Pages（或任意 HTTPS 静态托管）：

```bash
# Gitee Pages：需先在仓库「服务 → Gitee Pages」启用，实名认证后部署
# 目录结构：<pages>/dists/stable/...  <pages>/pool/main/...
```

### 7. 用户配置 sources.list

```bash
# 下载并信任公钥
sudo curl -fsSL https://<host>/waycoder.gpg -o /usr/share/keyrings/waycoder.gpg

# 添加源
echo "deb [signed-by=/usr/share/keyrings/waycoder.gpg] https://<host>/ stable main" \
  | sudo tee /etc/apt/sources.list.d/waycoder.list

sudo apt-get update && sudo apt-get install waycoder
```

## 备注

- **无签名仓库**（不推荐生产）：sources.list 用 `deb [trusted=yes] https://<host>/ stable main`
- `.deb` 打包脚本见 `build-deb.sh`（Linux 上直接可用）；本仓库的 `.deb` 已在 macOS 上通过 Python `ar` + `tarfile` 手搓生成，结构经 `ar t` 验证（`debian-binary` / `control.tar.gz` / `data.tar.gz`）。
- 未来版本：`./scripts/package.sh linux-x64 linux-arm64` 出二进制 → `build-deb.sh` 出 deb → reprepro includedeb 即可。
