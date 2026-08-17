#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# 打包 WayCoder 为 Debian/Ubuntu .deb
#
# 用法:
#   ./packaging/apt/build-deb.sh <binary路径> <版本> [架构]
#   ./packaging/apt/build-deb.sh dist/linux-x64/waycoder 0.48.7 amd64
#
# 产物: waycoder_<版本>_<架构>.deb
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

BIN="${1:?用法: build-deb.sh <binary> <version> [arch]}"
VERSION="${2:?缺少版本号}"
ARCH="${3:-amd64}"

# 去掉版本号的 v 前缀（deb 版本规范不允许 v）
DEB_VERSION="${VERSION#v}"

if [[ ! -f "$BIN" ]]; then
  echo "❌ 未找到二进制: $BIN" >&2
  exit 1
fi

PKG_NAME="waycoder_${DEB_VERSION}_${ARCH}"
PKG_ROOT="$(mktemp -d)/${PKG_NAME}"
mkdir -p "$PKG_ROOT/DEBIAN" "$PKG_ROOT/usr/local/bin"

cp "$BIN" "$PKG_ROOT/usr/local/bin/waycoder"
chmod 755 "$PKG_ROOT/usr/local/bin/waycoder"

cat > "$PKG_ROOT/DEBIAN/control" <<EOF
Package: waycoder
Version: ${DEB_VERSION}
Section: devel
Priority: optional
Architecture: ${ARCH}
Maintainer: Aleckstygit <aleckstygit@outlook.com>
Installed-Size: $(du -sk "$BIN" | cut -f1)
Homepage: https://gitee.com/aleckstygit/my-coder
Description: 中文版易用编程智能体（C# .NET NativeAOT 单文件 CLI 编程 Agent）
 WayCoder（道码）是一个中文版易用编程智能体，AOT 编译为单文件，
 41 个工具 + 多 Agent 工作区 + 权限系统 + Watch 模式。
EOF

dpkg-deb --build "$PKG_ROOT" "${PKG_NAME}.deb"
rm -rf "$(dirname "$PKG_ROOT")"
echo "✅ 生成 ${PKG_NAME}.deb"
