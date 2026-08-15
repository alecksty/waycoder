#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 一键多平台打包
#   当前平台 → NativeAOT（零依赖原生单文件）
#   其他平台 → 非 AOT（自包含单文件 JIT，可跨平台交叉发布）
#
# 用法:
#   ./scripts/package.sh                     # 打包全部 6 个平台
#   ./scripts/package.sh win-x64 linux-x64   # 只打包指定平台
#
# 产物: dist/waycoder-<版本>-<RID>.zip (Windows) / .tar.gz (Linux/macOS)
#
# 依赖: 当前平台为 Windows 且做 AOT 时，需要 Visual Studio 2022 C++ 工具链
#       （脚本会自动把 VS Installer 目录加入 PATH 以定位 vswhere.exe）。
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
PROJ="$REPO_DIR/WayCoder/WayCoder.csproj"
SRC_DIR="$REPO_DIR/WayCoder"
DIST="$REPO_DIR/dist"

# 版本号：从 Global.cs 提取
VERSION="$(sed -n 's/.*Version[[:space:]]*=[[:space:]]*"\(v[0-9.]*\)".*/\1/p' "$SRC_DIR/Config/Global.cs" | head -1)"
VERSION="${VERSION:-v$(date +%Y%m%d)}"

# ── 当前平台检测（只有当前平台走 AOT） ──
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) HOST_OS=win ;;
  Linux*)               HOST_OS=linux ;;
  Darwin*)              HOST_OS=osx ;;
  *)                    HOST_OS=linux ;;
esac
case "$(uname -m)" in
  x86_64|amd64) HOST_ARCH=x64 ;;
  arm64|aarch64) HOST_ARCH=arm64 ;;
  *)             HOST_ARCH=x64 ;;
esac
CURRENT_RID="${HOST_OS}-${HOST_ARCH}"

# Windows AOT 需要 vswhere.exe（VS Installer）定位 MSVC 链接器，确保其在 PATH
if [[ "$HOST_OS" == "win" ]]; then
  VS_INSTALLER="/c/Program Files (x86)/Microsoft Visual Studio/Installer"
  if [[ -d "$VS_INSTALLER" ]] && [[ ":$PATH:" != *":$VS_INSTALLER:"* ]]; then
    export PATH="$VS_INSTALLER:$PATH"
  fi
fi

# ── 目标平台（默认 6 个，可命令行覆盖） ──
DEFAULT_RIDS=(win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64)
RIDS=("${@:-${DEFAULT_RIDS[@]}}")

echo "═══════════════════════════════════════════════"
echo "  WayCoder 打包 ($VERSION)"
echo "  当前平台: $CURRENT_RID → NativeAOT"
echo "  目标平台: ${RIDS[*]}"
echo "═══════════════════════════════════════════════"

mkdir -p "$DIST"

for RID in "${RIDS[@]}"; do
  if [[ "$RID" == "$CURRENT_RID" ]]; then
    AOT=true
    MODE="NativeAOT"
  else
    AOT=false
    MODE="JIT（非 AOT）"
  fi

  OUT="$DIST/$RID"
  echo ""
  echo "▶ [$RID] $MODE 编译中…"
  rm -rf "$OUT"
  # 清理 obj/bin，避免不同 RID 之间的还原状态污染（否则会误报 Cross-OS）
  rm -rf "$SRC_DIR/obj" "$SRC_DIR/bin"
  dotnet publish "$PROJ" -c Release -r "$RID" -o "$OUT" \
    --self-contained true -p:PublishAot="$AOT" -p:PublishSingleFile=true \
    --nologo -v q

  # ── 打包 ──
  if [[ "$RID" == win-* ]]; then
    ARCHIVE="$DIST/waycoder-${VERSION}-${RID}.zip"
    rm -f "$ARCHIVE"
    WIN_ARCHIVE="$(cygpath -w "$ARCHIVE" 2>/dev/null || echo "$ARCHIVE")"
    (cd "$OUT" && powershell.exe -NoProfile -Command \
      "Get-ChildItem | Where-Object { \$_.Extension -ne '.pdb' } | Compress-Archive -DestinationPath '$WIN_ARCHIVE' -Force")
    echo "  ✅ $ARCHIVE"
  else
    ARCHIVE="$DIST/waycoder-${VERSION}-${RID}.tar.gz"
    rm -f "$ARCHIVE"
    tar -czf "$ARCHIVE" -C "$OUT" --exclude='*.pdb' .
    echo "  ✅ $ARCHIVE"
  fi
done

echo ""
echo "═══════════════════════════════════════════════════"
echo "  打包完成，产物:"
# ── 生成 SHA256SUMS.txt（供自动升级校验供应链完整性，防篡改）──
if command -v sha256sum >/dev/null 2>&1; then
  ( cd "$DIST" && sha256sum waycoder-* ) > "$DIST/SHA256SUMS.txt"
else
  ( cd "$DIST" && shasum -a 256 waycoder-* ) > "$DIST/SHA256SUMS.txt"
fi
echo "  ✅ $DIST/SHA256SUMS.txt"
ls -lh "$DIST"/waycoder-* "$DIST"/SHA256SUMS.txt 2>/dev/null || ls -lh "$DIST"
echo "═══════════════════════════════════════════════════"
