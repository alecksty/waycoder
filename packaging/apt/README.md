# apt 分发

WayCoder 的 Linux 二进制打包为 Debian `.deb`，通过 apt 仓库分发。

## 打包

```bash
./scripts/package.sh linux-x64 linux-arm64          # 先产出二进制
./packaging/apt/build-deb.sh dist/linux-x64/waycoder 0.48.7 amd64
./packaging/apt/build-deb.sh dist/linux-arm64/waycoder 0.48.7 arm64
```

## 建 apt 仓库（reprepro / aptly）

```bash
# 初始化仓库
mkdir -p repo/conf
cat > repo/conf/distributions <<'EOF'
Origin: WayCoder
Label: WayCoder
Codename: stable
Architectures: amd64 arm64
Components: main
Description: WayCoder 官方 apt 仓库
EOF
reprepro -b repo includedeb stable waycoder_0.48.7_amd64.deb
reprepro -b repo includedeb stable waycoder_0.48.7_arm64.deb
```

把 `repo/` 发布到静态托管（如 Gitee Pages / GitHub Pages / 对象存储），用户配置：

```bash
echo "deb [signed-by=/usr/share/keyrings/waycoder.gpg] https://<host>/repo stable main" \
  | sudo tee /etc/apt/sources.list.d/waycoder.list
sudo apt update && sudo apt install waycoder
```

> 仓库需 GPG 签名（`reprepro` 用 `SignWith` 配置）；无签名仓库可用 `[trusted=yes]`（不推荐生产）。
