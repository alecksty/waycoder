#!/usr/bin/env bash
# 打 App Store 包**之前**的预检：把那些"报错看不出真身"的失败，提前问出来。
#
# 用法：
#   ./preflight-ios-appstore.sh [--sdk 27.0] [--key "Apple Distribution: … (TEAMID)"]
#                               [--profile "WayCoder AppStore"] [--mac <ip>] [--mac-user <名字>]
#                               [--last-build <n>] [--privacy-url <url>] [--strict]
#
# 与 build-ios-appstore.sh 配套：预检过了再跑那个。两者共用同一套环境变量名与 --key/--profile。
#
# ## 为什么是脚本不是文档清单
# 这些判据绝大多数是**机器可判**的，而它们的失败形态全都是"报错看不出真身"：
# TFM 拼错报的是 NETSDK1005、用错 dotnet 打 iOS 却报 maui-android。写在文档里必然腐烂
# —— 本仓已经吃过这个亏（build-ios-appstore.sh 头部那三条"已踩过的坑"就是补记）。
#
# ## 与仓库的隔离（三条）
# 1. **不落盘任何密钥**，不回显签名串与 Mac 密码；**故意不接受 Mac 密码参数**
#    （build-ios-appstore.sh 有 --mac-pass，这里没有）—— 预检要么走已铺好的 SSH 密钥，
#    要么留给人工核对。与 build-ios-appstore.sh:41-44「签名是外部输入，一个字都不落磁盘」一致。
# 2. **只读**：读文件、`dotnet msbuild -getProperty`（只求值不出产物）、只读网络请求。
#    不写状态文件（那会变成第二个版本真源 —— csproj 注释里骂过"同一份事实两处拷贝"）。
# 3. 放在 WayCoder.Maui/ 而不是 scripts/：它得 cd 到这里才能 `dotnet msbuild WayCoder.Maui.csproj`。
#
# 退出码：0 = 全过（--strict 下 WARN 也算不过）；1 = 有 FAIL；2 = 参数错。
#
# ⚠ 本文件必须是 **LF**（.gitattributes: `*.sh text eol=lf`）。CRLF 会让 `set -euo pipefail`
#   报"无效的选项名"——那是行尾不可见的字节，最难查。
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
cd "$HERE"

SDK_VER="${AppleSdkVersion:-27.0}"
KEY="${WAYCODER_IOS_SIGN_KEY:-}"
PROFILE="${WAYCODER_IOS_PROFILE:-}"
MAC_HOST=""
MAC_USER=""
LAST_BUILD=""
PRIVACY_URL="https://github.com/alecksty/waycoder/blob/master/docs/PRIVACY.md"
STRICT=0
MAC_DOTNET_ROOT="${WAYCODER_MAC_DOTNET_ROOT:-}"

usage() { sed -n '2,30p' "$0"; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --sdk)         SDK_VER="${2:-}"; shift 2 ;;
    --key)         KEY="${2:-}"; shift 2 ;;
    --profile)     PROFILE="${2:-}"; shift 2 ;;
    --mac)         MAC_HOST="${2:-}"; shift 2 ;;
    --mac-user)    MAC_USER="${2:-}"; shift 2 ;;
    --last-build)  LAST_BUILD="${2:-}"; shift 2 ;;
    --privacy-url) PRIVACY_URL="${2:-}"; shift 2 ;;
    --strict)      STRICT=1; shift ;;
    -h|--help)     usage; exit 0 ;;
    *) echo "未知参数: $1" >&2; exit 2 ;;
  esac
done

FAILS=0
WARNS=0
ok()   { printf '  \033[32m✓\033[0m %s\n' "$1"; }
bad()  { printf '  \033[31m✗\033[0m %s\n' "$1"; FAILS=$((FAILS+1)); }
warn() { printf '  \033[33m!\033[0m %s\n' "$1"; WARNS=$((WARNS+1)); }
grp()  { printf '\n\033[1m%s\033[0m\n' "$1"; }

# ── dotnet：优先 WAYCODER_DOTNET（Windows 上脚本内置的 /usr/local/share/dotnet 是 macOS 路径）──
DOTNET="${WAYCODER_DOTNET:-dotnet}"
if ! command -v "$DOTNET" >/dev/null 2>&1 && [[ ! -x "$DOTNET" ]]; then
  echo "✗ 找不到 dotnet（WAYCODER_DOTNET=$DOTNET）" >&2; exit 2
fi

# ── packs 目录：Windows 走 DOTNET_ROOT / 常见安装位，macOS 走 /usr/local/share/dotnet ──
PACKS=""
for c in "${DOTNET_ROOT:-}" "/c/Program Files/dotnet" "/usr/local/share/dotnet" \
         "$(dirname "$(command -v "$DOTNET" 2>/dev/null || echo /nonexistent)")"; do
  [[ -n "$c" && -d "$c/packs" ]] && { PACKS="$c/packs"; break; }
done

# ════════════════════════════════════════════════════════════════════
grp "G1 版本号（唯一真源 WayCoder/Config/Global.cs）"

GLOBAL_CS="$REPO/WayCoder/Config/Global.cs"
if [[ ! -f "$GLOBAL_CS" ]]; then
  bad "找不到 $GLOBAL_CS"
  VER_MAJOR=""; VER_MINOR=""; VER_PATCH=""
else
  # 与 csproj 里那三条正则**同口径**（改了 csproj 就要改这里，反之亦然）
  VER_MAJOR="$(sed -nE 's/.*Version[[:space:]]*=[[:space:]]*"v([0-9]+)\..*/\1/p' "$GLOBAL_CS" | head -1)"
  VER_MINOR="$(sed -nE 's/.*Version[[:space:]]*=[[:space:]]*"v[0-9]+\.([0-9]+)\..*/\1/p' "$GLOBAL_CS" | head -1)"
  VER_PATCH="$(sed -nE 's/.*Version[[:space:]]*=[[:space:]]*"v[0-9.]*\.([0-9]+)".*/\1/p' "$GLOBAL_CS" | head -1)"
fi

if [[ -z "$VER_MAJOR" || -z "$VER_MINOR" || -z "$VER_PATCH" ]]; then
  bad "版本号解析失败（major='$VER_MAJOR' minor='$VER_MINOR' patch='$VER_PATCH'）—— csproj 的 VerifyAppVersionParsed 也会因此报错"
  BUNDLE_VER=""
else
  ok "解析出 v$VER_MAJOR.$VER_MINOR.$VER_PATCH"
  BUNDLE_VER=$(( VER_MAJOR * 1000000 + VER_MINOR * 10000 + VER_PATCH ))
  ok "CFBundleShortVersionString = $VER_MAJOR.$VER_MINOR.$VER_PATCH"
  ok "CFBundleVersion           = $BUNDLE_VER"

  # 与 msbuild 真算一遍交叉验证 —— 手算和 csproj 的 Add/Multiply 表达式万一对不上，
  # 后果是"设置里显示 A、App Store 显示 B"（构建全绿，只有人眼能发现）。
  MS_VER="$("$DOTNET" msbuild WayCoder.Maui.csproj -getProperty:ApplicationVersion -nologo 2>/dev/null | tr -d '\r\n' || true)"
  MS_DISP="$("$DOTNET" msbuild WayCoder.Maui.csproj -getProperty:ApplicationDisplayVersion -nologo 2>/dev/null | tr -d '\r\n' || true)"
  if [[ -z "$MS_VER" ]]; then
    warn "读不到 msbuild 的 ApplicationVersion（还原没做或 csproj 有问题？）"
  elif [[ "$MS_VER" == "$BUNDLE_VER" && "$MS_DISP" == "$VER_MAJOR.$VER_MINOR.$VER_PATCH" ]]; then
    ok "与 msbuild 的推导一致（ApplicationVersion=$MS_VER, DisplayVersion=$MS_DISP）"
  else
    bad "与 msbuild 不一致：csproj 算出 $MS_VER / $MS_DISP，本脚本算出 $BUNDLE_VER / $VER_MAJOR.$VER_MINOR.$VER_PATCH"
  fi

  if [[ -n "$LAST_BUILD" ]]; then
    if (( BUNDLE_VER > LAST_BUILD )); then
      ok "CFBundleVersion $BUNDLE_VER > 上次上传的 $LAST_BUILD"
    else
      bad "CFBundleVersion $BUNDLE_VER 未大于上次上传的 $LAST_BUILD —— 同一个值会被 App Store 直接拒收"
    fi
  else
    warn "没给 --last-build，无法自动核对单调性。请去 App Store Connect / TestFlight 看上一版构建号，确认 $BUNDLE_VER 更大"
  fi
fi

MS_SDK="$("$DOTNET" msbuild WayCoder.Maui.csproj -getProperty:AppleSdkVersion -nologo 2>/dev/null | tr -d '\r\n' || true)"
if [[ -n "$MS_SDK" && "$MS_SDK" != "$SDK_VER" ]]; then
  bad "csproj 的 AppleSdkVersion='$MS_SDK' 与本次 --sdk='$SDK_VER' 不一致（TFM 会拼错）"
else
  ok "Apple SDK 版本 = $SDK_VER（TFM = net10.0-ios$SDK_VER）"
fi

# ════════════════════════════════════════════════════════════════════
grp "G2 本机工具链"

if ! command -v "$DOTNET" >/dev/null 2>&1 && [[ ! -x "$DOTNET" ]]; then
  bad "dotnet 不可用"
else
  ok "dotnet = $("$DOTNET" --version 2>/dev/null | tr -d '\r\n')"
  WL="$("$DOTNET" workload list 2>/dev/null || true)"
  for w in maui ios; do
    if echo "$WL" | grep -qE "^[[:space:]]*$w[[:space:]]"; then ok "工作负载 $w 已装"
    else bad "缺少工作负载 $w —— 跑 dotnet workload install $w maui"; fi
  done
fi

if [[ -n "${WAYCODER_DOTNET:-}" ]]; then
  ok "WAYCODER_DOTNET 已设置（build-ios-appstore.sh 会用它）"
else
  warn "未设 WAYCODER_DOTNET —— 脚本内置回退 /usr/local/share/dotnet/dotnet 是 macOS 路径，Windows 上会走到裸 dotnet"
fi

REQUIRED_XCODE=""
if [[ -z "$PACKS" ]]; then
  bad "找不到 dotnet 的 packs 目录（试过 DOTNET_ROOT / Program Files / /usr/local/share）"
else
  PACK_DIR="$PACKS/Microsoft.iOS.Sdk.net10.0_$SDK_VER"
  if [[ ! -d "$PACK_DIR" ]]; then
    bad "缺少 SDK pack: $PACK_DIR —— 该 SDK 版本没装（这就是报 NETSDK1005 的真身）"
  else
    # 目录名形如 27.0.10417-xcode27.0 ⇒ 后缀就是**硬要求**的 Xcode 版本
    XDIR="$(find "$PACK_DIR" -maxdepth 1 -mindepth 1 -type d -name '*-xcode*' | head -1)"
    if [[ -n "$XDIR" ]]; then
      REQUIRED_XCODE="$(basename "$XDIR" | sed -nE 's/.*-xcode([0-9.]+)$/\1/p')"
      ok "iOS SDK pack 在（要求 Xcode ${REQUIRED_XCODE:-?}）"
    else
      warn "SDK pack 存在，但目录名里没有 -xcode 后缀，推不出要求的 Xcode 版本"
    fi
  fi
  # Windows 主机编 iOS 还需要这一份
  if [[ -d "$PACKS/Microsoft.iOS.Windows.Sdk.net10.0_$SDK_VER" ]]; then
    ok "Microsoft.iOS.Windows.Sdk.net10.0_$SDK_VER 在"
  else
    warn "没有 Microsoft.iOS.Windows.Sdk.net10.0_$SDK_VER（若在 Windows 上构建 iOS 会需要）"
  fi
fi

# ════════════════════════════════════════════════════════════════════
grp "G3 Mac 侧（Pair to Mac 远程构建）"

if [[ -z "$MAC_HOST" ]]; then
  warn "没给 --mac，跳过远程构建检查（在 Mac 上本机打包可忽略这一组）"
else
  if [[ -z "$MAC_USER" ]]; then
    bad "--mac 给了但 --mac-user 没给"
  else
    ok "目标 Mac: $MAC_USER@$MAC_HOST"
    if [[ "$MAC_HOST" == "192.168.1.23" || "$MAC_USER" == *"你的"* ]]; then
      bad "Mac 地址/用户名还是模板占位值（192.168.1.23 / 你的 Mac 用户名）—— 填成你真实的 Mac 再跑"
    fi

    # 58181：Pair to Mac 的构建代理端口。**要区分 refused 与 timeout** ——
    # 前者=Mac 上没起 broker（多半是没配对过），后者=不同网段/防火墙，处理办法完全不同。
    PORT_STATE="closed"
    if command -v powershell.exe >/dev/null 2>&1; then
      if powershell.exe -NoProfile -Command "Test-NetConnection -ComputerName '$MAC_HOST' -Port 58181 -InformationLevel Quiet" 2>/dev/null | tr -d '\r' | grep -qi true; then
        PORT_STATE="open"
      fi
    elif (exec 3<>"/dev/tcp/$MAC_HOST/58181") 2>/dev/null; then
      PORT_STATE="open"
    fi
    if [[ "$PORT_STATE" == "open" ]]; then
      ok "TCP $MAC_HOST:58181 可达"
    else
      bad "TCP $MAC_HOST:58181 不可达 —— 要么 Mac 上没起 Pair to Mac 的构建代理（先在 VS 里配对一次），要么被防火墙/网段挡住"
    fi

    # 只用密钥登录，**绝不在脚本里问密码**
    if ssh -o BatchMode=yes -o ConnectTimeout=5 "$MAC_USER@$MAC_HOST" true >/dev/null 2>&1; then
      ok "SSH 密钥登录可用"
      REMOTE_XCODE="$(ssh -o BatchMode=yes -o ConnectTimeout=5 "$MAC_USER@$MAC_HOST" 'xcodebuild -version 2>/dev/null | head -1' 2>/dev/null | tr -d '\r' || true)"
      if [[ -n "$REQUIRED_XCODE" && -n "$REMOTE_XCODE" ]]; then
        if echo "$REMOTE_XCODE" | grep -q "Xcode $REQUIRED_XCODE"; then
          ok "Mac 的 $REMOTE_XCODE（与本机 SDK pack 要求一致）"
        else
          bad "Mac 的 '$REMOTE_XCODE' 与本机 SDK pack 要求的 Xcode $REQUIRED_XCODE 不匹配 —— 版本不对远程构建会失败"
        fi
      elif [[ -n "$REMOTE_XCODE" ]]; then
        ok "Mac 的 $REMOTE_XCODE"
      else
        warn "取不到 Mac 的 Xcode 版本"
      fi
      ROOT="${MAC_DOTNET_ROOT:-/Users/$MAC_USER/Library/Caches/maui/PairToMac/SDKs/dotnet/}"
      if ssh -o BatchMode=yes -o ConnectTimeout=5 "$MAC_USER@$MAC_HOST" "test -x '$ROOT/dotnet'" >/dev/null 2>&1; then
        ok "Mac 侧 dotnet 在：$ROOT"
      else
        bad "Mac 侧没有可执行的 dotnet：$ROOT —— VS 2026 用 maui/PairToMac，VS 2022 用 Xamarin/XMA，路径不同；可用 WAYCODER_MAC_DOTNET_ROOT 覆盖"
      fi
    else
      warn "SSH 密钥登录不通（预检不提供密码）。请在那台 Mac 上手工核对：Xcode 版本、上面那条 dotnet 路径、58181 是否有进程监听"
    fi
  fi
fi

# ════════════════════════════════════════════════════════════════════
grp "G4 签名信息"

if [[ -z "$KEY" || -z "$PROFILE" ]]; then
  bad "缺签名信息：--key / --profile（或 WAYCODER_IOS_SIGN_KEY / WAYCODER_IOS_PROFILE）—— 上架包必须用 Distribution 证书 + App Store 描述文件"
else
  # ⚠ 先认占位值：模板里的 "Apple Distribution: 你的名字 (TEAMID)" **形状是对的**，
  #   不加这一条就会全绿通过 —— 而真打包必然失败（这正是"假绿比红更危险"的形态）。
  #   **两条分开报**：混在一句里就看不出到底是哪个还没填。
  if [[ "$KEY" == *"你的名字"* || "$KEY" == *TEAMID* || "$KEY" == *your\ name* ]]; then
    bad "签名身份还是模板占位值 —— 要填 <Mac 上 security find-identity -v -p codesigning> 的真实输出"
  fi
  if [[ "$PROFILE" == *"你的"* || "$PROFILE" == "WayCoder AppStore" ]]; then
    bad "描述文件名还是模板默认值「$PROFILE」—— 要填后台那个 App Store 类型描述文件的真实名字（App 已改名 Dolaima，名字多半不长这样）"
  fi
  # 只回显前缀与长度，**绝不回显整串**
  KPFX="${KEY%%:*}"; [[ "$KPFX" == "$KEY" ]] && KPFX="${KEY:0:24}"
  ok "签名身份已给（前缀 '${KPFX}'，长度 ${#KEY}）"
  if [[ "$KEY" =~ ^Apple\ (Distribution|Production): ]]; then
    ok "证书类型看着是 Distribution/Production"
  else
    bad "证书不是 Apple Distribution/Production 开头 —— 开发证书签不出上架包"
  fi
  ok "描述文件已给：$PROFILE"
  case "$PROFILE" in
    *Development*|*Ad\ Hoc*|*Dev) warn "描述文件名里带 Development/Ad Hoc —— 名字不可靠，请人工确认后台那份是 **App Store** 类型" ;;
  esac
fi

# ════════════════════════════════════════════════════════════════════
grp "G5 其余发布门"

for f in Resources/Raw/help/zh/legal/privacy.md Resources/Raw/help/en/legal/privacy.md; do
  [[ -f "$f" ]] && ok "随包隐私政策在：$f" || bad "缺少随包隐私政策：$f（App 内「关于 → 使用说明与隐私」会点不开）"
done

if [[ -n "$PRIVACY_URL" ]]; then
  # 唯一能在上传前发现"元数据里的隐私政策 URL 是 404"的地方。
  # ⚠ 这个 URL 指向 GitHub；按本仓惯例代码只推 Gitee ⇒ 新写的 docs/PRIVACY.md
  #   在推到 github 远程**之前**是取不到的，这一项会红 —— 那是对的，不是误报。
  CODE="$(curl -sS -L -o /dev/null -w '%{http_code}' --max-time 15 "$PRIVACY_URL" 2>/dev/null || echo 000)"
  if [[ "$CODE" == "200" ]]; then ok "隐私政策 URL 可访问（200）"
  else bad "隐私政策 URL 返回 $CODE：$PRIVACY_URL —— 元数据里填它会被审核打回（记得把 docs/PRIVACY.md 推到 github 远程）"; fi
fi

if [[ -n "$(git -C "$REPO" status --porcelain 2>/dev/null)" ]]; then
  warn "工作区有未提交改动 —— 上架包最好对应一个干净的提交，便于复现"
else
  ok "工作区干净"
fi

# ════════════════════════════════════════════════════════════════════
printf '\n\033[1m预检结果：%d 项失败，%d 项警告\033[0m\n' "$FAILS" "$WARNS"
if (( FAILS > 0 )); then
  echo "先修掉上面标 ✗ 的再打包。"
  exit 1
fi
if (( STRICT && WARNS > 0 )); then
  echo "--strict：警告也算不过。"
  exit 1
fi
echo "可以打包了："
echo "  bash WayCoder.Maui/build-ios-appstore.sh --clean --key \"…\" --profile \"…\" \\"
echo "       --mac <ip> --mac-user <名字>"
exit 0
