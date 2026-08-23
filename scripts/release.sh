#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 一键发布（半自动，bash 版）
#
# 流程（6 步）：
#   1. 编译全部 6 平台包（复用 scripts/package.sh）
#   2. 计算全部产物 SHA256
#   3. winget：生成新版本 manifest 目录 + 填 sha256 + 本地 winget validate
#   4. brew  ：更新 formula 版本号 + sha256
#   5. apt   ：打 .deb（仅 Linux 本机执行，否则打印待执行命令）
#   6. 打印「提交到各服务器」的精确命令，人工确认后逐条执行
#
# 用法:
#   ./scripts/release.sh                 # 全流程
#   ./scripts/release.sh --skip-build    # 已有 dist 产物，跳过编译
#
# 依赖: dotnet 10、bash 4+、GNU coreutils（Git Bash / Linux 均满足）；
#       winget 校验可选（未装则跳过）
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
SRC_DIR="$REPO_DIR/WayCoder"
DIST="$REPO_DIR/dist"
PACKAGING="$REPO_DIR/packaging"

SKIP_BUILD=0
[[ "${1:-}" == "--skip-build" ]] && SKIP_BUILD=1

# ── 版本号（带 v / 不带 v 两种形式）──
VERSION="$(sed -n 's/.*Version[[:space:]]*=[[:space:]]*"\(v[0-9.]*\)".*/\1/p' "$SRC_DIR/Config/Global.cs" | head -1)"
VERSION="${VERSION:-v$(date +%Y%m%d)}"
VER="${VERSION#v}"

c()   { printf '\033[36m%s\033[0m\n' "$*"; }
ok()  { printf '\033[32m%s\033[0m\n' "$*"; }
warn(){ printf '\033[33m%s\033[0m\n' "$*"; }

# ── SHA256 计算（sha256sum → shasum → powershell 兜底）──
sha256() {
  local f="$1"
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$f" | awk '{print $1}'
  elif command -v shasum >/dev/null 2>&1; then
    shasum -a 256 "$f" | awk '{print $1}'
  else
    powershell.exe -NoProfile -Command \
      "(Get-FileHash -LiteralPath '$(cygpath -w "$f" 2>/dev/null || echo "$f")' -Algorithm SHA256).Hash.ToLower()" \
      2>/dev/null | tr -d '\r'
  fi
}

c "═══════════════════════════════════════════════"
c "  WayCoder 一键发布 ($VERSION)"
c "═══════════════════════════════════════════════"

# ═══ 1. 编译 ═══
if [[ "$SKIP_BUILD" == 1 ]]; then
  warn "▶ 跳过编译（--skip-build）"
else
  c "▶ 1/6 编译全部平台包…"
  bash "$REPO_DIR/scripts/package.sh"
fi

# ═══ 2. SHA256 ═══
c "▶ 2/6 计算 SHA256…"
declare -A SHA
for RID in win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64; do
  ext=tar.gz; [[ "$RID" == win-* ]] && ext=zip
  a="$DIST/waycoder-$VERSION-$RID.$ext"
  [[ -f "$a" ]] || { echo "❌ 缺少产物: $a" >&2; exit 1; }
  SHA[$RID]="$(sha256 "$a")"
  printf '  %-12s %s\n' "$RID" "${SHA[$RID]}"
done

# ═══ 3. winget ═══
c "▶ 3/6 生成 winget manifest…"
WINGET_BASE="$PACKAGING/winget/manifests/a/Aleckstygit/WayCoder"
LATEST_VER="$(ls -1 "$WINGET_BASE" | sort -V | tail -1)"
NEW_DIR="$WINGET_BASE/$VER"

if [[ "$LATEST_VER" == "$VER" ]]; then
  warn "  winget manifest $VER 已存在，跳过生成（如需重生成请先删除 $NEW_DIR）"
else
  rm -rf "$NEW_DIR"
  cp -r "$WINGET_BASE/$LATEST_VER" "$NEW_DIR"
  # 版本号全局替换（PackageVersion + InstallerUrl 里的 vX.Y.Z 一并覆盖）
  # 点号转义为字面点（sed BRE 中 . 是通配符）；sed -i '' 兼容 GNU/BSD（BSD 需显式空备份后缀）
  LATEST_VER_RE="${LATEST_VER//./\\.}"
  for f in "$NEW_DIR"/*.yaml; do
    sed -i '' "s/$LATEST_VER_RE/$VER/g" "$f"
  done
  # 填 sha256（按 Architecture 范围替换上一版真实 sha256，顺序：x64 → arm64）
  INST="$NEW_DIR/Aleckstygit.WayCoder.installer.yaml"
  sed -i '' "/Architecture: x64/,/InstallerSha256:/s/InstallerSha256:.*/InstallerSha256: ${SHA[win-x64]}/" "$INST"
  sed -i '' "/Architecture: arm64/,/InstallerSha256:/s/InstallerSha256:.*/InstallerSha256: ${SHA[win-arm64]}/" "$INST"
  ok "  已生成 $NEW_DIR"
fi

if command -v winget >/dev/null 2>&1; then
  winget validate "$NEW_DIR" && ok "  winget validate 通过" || warn "  winget validate 有告警，请检查"
else
  warn "  未检测到 winget，跳过本地校验（可手动: winget validate \"$NEW_DIR\"）"
fi

# ═══ 4. brew ═══
c "▶ 4/6 更新 brew formula…"
FORMULA="$PACKAGING/brew/waycoder.rb"
OLD_VER="$(sed -n 's/.*version "\([0-9.]*\)".*/\1/p' "$FORMULA" | head -1)"
if [[ -z "$OLD_VER" ]]; then
  warn "  无法从 formula 提取版本号，跳过（请手动改 $FORMULA）"
else
  OLD_VER_RE="${OLD_VER//./\\.}"
  sed -i '' "s/$OLD_VER_RE/$VER/g" "$FORMULA"
  sed -i '' "/on_arm do/,/sha256 /s/sha256 \".*\"/sha256 \"${SHA[osx-arm64]}\"/" "$FORMULA"
  sed -i '' "/on_intel do/,/sha256 /s/sha256 \".*\"/sha256 \"${SHA[osx-x64]}\"/" "$FORMULA"
  ok "  已更新 $FORMULA → $VER"
fi

# ═══ 5. apt ═══
c "▶ 5/6 apt .deb…"
APT_DIR="$PACKAGING/apt"
if command -v dpkg-deb >/dev/null 2>&1; then
  bash "$APT_DIR/build-deb.sh" "$DIST/linux-x64/waycoder" "$VERSION" amd64
  bash "$APT_DIR/build-deb.sh" "$DIST/linux-arm64/waycoder" "$VERSION" arm64
  ok "  .deb 已生成"
else
  warn "  非 Linux 环境（无 dpkg-deb），跳过 .deb 打包，下方打印待执行命令"
fi

# ═══ 6. 打印提交命令 ═══
echo ""
c "═══════════════════════════════════════════════"
c "  提交到各服务器 —— 请人工确认后逐条执行"
c "═══════════════════════════════════════════════"

echo ""
c "【0. 上传发行资产】"
cat <<EOF
  ① Gitee Release（国内主渠道，waycoder --update 优先走这里）:
    https://gitee.com/aleckstygit/way-coder/releases
    上传 dist/waycoder-$VERSION-*.zip / *.tar.gz 共 6 个资产

  ② GitHub Release（海外 mirror；winget/brew 清单 URL 指向 Gitee 可预测 release URL releases/download/<tag>/<file>）:
    走 Actions： git push github $VERSION
    或手动： https://github.com/alecksty/waycoder/releases/new?tag=$VERSION
EOF

echo ""
c "【1. winget → microsoft/winget-pkgs】"
cat <<EOF
  已生成（含 sha256）: $NEW_DIR
  提交步骤（需已登录 gh + fork microsoft/winget-pkgs）:
    gh repo fork microsoft/winget-pkgs --clone
    cp -r "$NEW_DIR" "\$HOME/.../winget-pkgs/manifests/a/Aleckstygit/WayCoder/$VER"
    cd 到 fork 目录
    git add -A && git commit -m "New version: Aleckstygit.WayCoder $VER"
    git push && gh pr create --title "New version: Aleckstygit.WayCoder $VER" --body "更新到 $VER"
  本地免提交安装测试:
    winget install --manifest "$NEW_DIR"
EOF

echo ""
c "【2. brew → gitee tap aleckstygit/homebrew-waycoder】"
cat <<EOF
  已更新（含 sha256）: $FORMULA
  提交步骤:
    git clone https://gitee.com/aleckstygit/homebrew-waycoder /tmp/homebrew-waycoder
    cp "$FORMULA" /tmp/homebrew-waycoder/Formula/waycoder.rb    # 若 tap 公式在根目录则去掉 Formula/
    cd /tmp/homebrew-waycoder && git add . && git commit -m "waycoder $VER" && git push
  macOS 校验:
    brew audit --strict waycoder && brew test waycoder
EOF

echo ""
c "【3. apt → 自建 reprepro 仓库 + Pages 静态托管】"
cat <<EOF
  在 Linux 上执行（.deb 打包 + 仓库入库）:
    ./packaging/apt/build-deb.sh dist/linux-x64/waycoder $VER amd64
    ./packaging/apt/build-deb.sh dist/linux-arm64/waycoder $VER arm64

    mkdir -p repo/conf
    cat > repo/conf/distributions <<'CONF'
Origin: WayCoder
Label: WayCoder
Codename: stable
Architectures: amd64 arm64
Components: main
Description: WayCoder 官方 apt 仓库
SignWith: <你的 GPG key id>
CONF
    reprepro -b repo includedeb stable waycoder_${VER}_amd64.deb
    reprepro -b repo includedeb stable waycoder_${VER}_arm64.deb

    # 把 repo/ 发布到 Gitee/GitHub Pages（需 GPG 签名，详见 packaging/apt/README.md）
EOF

echo ""
ok "完成。产物与清单已就绪，请按上述命令逐条提交。"
