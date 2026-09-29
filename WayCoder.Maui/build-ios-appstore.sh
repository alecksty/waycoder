#!/usr/bin/env bash
# 打 **App Store 上架用的 .ipa**（不是模拟器 .app —— 那个是 build-ios.sh 的活）。
#
# 用法（macOS 上，本机有 Xcode + 签名）：
#   ./build-ios-appstore.sh \
#     --key "Apple Distribution: Your Name (TEAMID)" \
#     --profile "WayCoder AppStore"
#
# 也可以用环境变量（CI 上更顺手，命令行不留明文）：
#   WAYCODER_IOS_SIGN_KEY="Apple Distribution: … (TEAMID)" \
#   WAYCODER_IOS_PROFILE="WayCoder AppStore" \
#   ./build-ios-appstore.sh
#
# Windows 上跑（Visual Studio「Pair to Mac」，需要同一网段有台开着 Xcode 的 Mac；
# 建议先让 Visual Studio 与那台 Mac 配对成功一次 —— SSH 密钥与 Mac 侧 dotnet 由它铺好）：
#   ./build-ios-appstore.sh --key "…" --profile "…" \
#     --mac 192.168.1.23 --mac-user aleck --mac-pass '…'
#
# ## 产物
#   bin/Release/net10.0-ios<pick>/ios-arm64/publish/<AppName>.ipa
#   —— 直接拿去上传（见文末提示）。
#   ⚠ 在 Windows 上用 --mac 远程构建时，这个 .ipa 会**拷回 Windows 本机**
#     （官方文档明确写了这一点）⇒「编译在 Mac、上传在 Windows」成立，
#     本机就能一条龙跑完，不是必须坐到 Mac 前面。
#
# ## 四条已经踩过的坑（与 build-ios.sh 同源，别重复踩）
#
# 0) ⚠ **Windows + Git Bash 会改写以 `/` 开头的参数值**（本条只在 `--mac` 远程构建时踩到，
#    但踩了就是"连不上 Mac/远程错误"这种看不出真身的现象）：
#    `-p:_DotNetRootRemoteDirectory=/Users/…` 被改写成 `C:/Program Files/Git/Users/…`。
#    对策是在调 dotnet 时带 `MSYS_NO_PATHCONV=1`（脚本里已加，**别删**）；
#    同族：属性名是 `ServerTcpPort`，写成 `TcpPort` 会被静默忽略。
#
# 1) **TFM 必须带 SDK 版本后缀**：csproj 里是 net10.0-ios$(AppleSdkVersion)，
#    默认 AppleSdkVersion=27.0 ⇒ 真 TFM 是 net10.0-ios27.0。写裸 net10.0-ios
#    会报 NETSDK1005「资产文件没有 net10.0-ios 的目标」—— 完全看不出是 TFM 拼错。
#    换 Xcode：--sdk 26.5（等价于 -p:AppleSdkVersion=26.5）。
#
# 2) **必须用装了 maui 工作负载的那个 dotnet**。本机若有两套（如 homebrew 的
#    /opt/homebrew/bin/dotnet 与官方 /usr/local/share/dotnet/dotnet），用错了会在
#    打 iOS 时报 NETSDK1147「必须安装以下工作负载: maui-android」（报的是 android，
#    因为还原会评估全部 TFM）。换法：WAYCODER_DOTNET=/path/to/dotnet ./build-ios-appstore.sh。
#
# 3) **报错像"环境坏了"先清 obj/bin**（秒级），别急着重装工作负载（分钟级）：
#    obj/ 里可能残留别的 SDK 版本编出来的中间产物。用 --clean。
#
# ## 签名是"外部输入"，不该写进仓库
# CodesignKey / CodesignProvision 与具体机器、具体开发者账号绑定，写进 csproj 会让
# 别人 clone 下来就编不过（也把团队信息散进版本库）。所以本脚本只从参数/环境变量取，
# 一个字都不落磁盘。
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE"

SDK_VER="${AppleSdkVersion:-27.0}"
CLEAN=0
KEY="${WAYCODER_IOS_SIGN_KEY:-}"
PROFILE="${WAYCODER_IOS_PROFILE:-}"
MAC_HOST=""
MAC_USER=""
MAC_PASS=""

usage() { sed -n '2,40p' "$0"; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --key)      KEY="${2:-}"; shift 2 ;;
    --profile)  PROFILE="${2:-}"; shift 2 ;;
    --mac)      MAC_HOST="${2:-}"; shift 2 ;;
    --mac-user) MAC_USER="${2:-}"; shift 2 ;;
    --mac-pass) MAC_PASS="${2:-}"; shift 2 ;;
    --sdk)      SDK_VER="${2:-}"; shift 2 ;;
    --clean)    CLEAN=1; shift ;;
    -h|--help)  usage; exit 0 ;;
    *) echo "未知参数: $1" >&2; exit 2 ;;
  esac
done

if [[ -z "$KEY" || -z "$PROFILE" ]]; then
  cat >&2 <<'MSG'
缺少签名信息。上架包必须用 Distribution 证书 + App Store 描述文件签名，
两者都不在仓库里（见脚本头部说明），得显式传进来：

    ./build-ios-appstore.sh --key "Apple Distribution: 你的名字 (TEAMID)" --profile "WayCoder AppStore"

这两个值从哪来：
  · Signing identity —— 钥匙串里 security find-identity -v -p codesigning 的输出，
    形如 "Apple Distribution: Your Name (ABCDE12345)"；
  · Provisioning profile —— Apple 开发者后台 Profiles 里那个 App Store 类型的名字
    （不是 Development、也不是 Ad Hoc）。
两者必须同属一个 Team，且描述文件绑的 App ID 要匹配 com.tanso.dolaima。
MSG
  exit 2
fi

if [[ "$CLEAN" == 1 ]]; then
  echo "▸ 清 obj/bin（net10.0-ios）…"
  rm -rf obj/Debug/net10.0-ios* obj/Release/net10.0-ios* \
         bin/Debug/net10.0-ios* bin/Release/net10.0-ios*
fi

TFM="net10.0-ios${SDK_VER}"
RID="ios-arm64"

# 与 build-ios.sh / build-apk.sh 同一条：优先官方 dotnet（装了 maui 工作负载的那套）。
DOTNET="${WAYCODER_DOTNET:-}"
if [[ -z "$DOTNET" ]]; then
  if [[ -x /usr/local/share/dotnet/dotnet ]]; then DOTNET=/usr/local/share/dotnet/dotnet; else DOTNET=dotnet; fi
fi

PUBLISH_ARGS=(-f "$TFM" -c Release
              -p:ArchiveOnBuild=true
              -p:RuntimeIdentifier="$RID"
              -p:CodesignKey="$KEY"
              -p:CodesignProvision="$PROFILE")

if [[ -n "$MAC_HOST" ]]; then
  # Windows → Mac 远程构建（Visual Studio「Pair to Mac」那套协议，dotnet CLI 同样支持）。
  # ⚠ _DotNetRootRemoteDirectory 在 Mac 侧要预置：VS 2026 用 maui/PairToMac，
  #   VS 2022 用 Xamarin/XMA —— 路径不同，写错只会报"连不上/找不到 dotnet"。
  ROOT="${WAYCODER_MAC_DOTNET_ROOT:-/Users/${MAC_USER}/Library/Caches/maui/PairToMac/SDKs/dotnet/}"
  # ⚠ 属性名是 **ServerTcpPort**，不是 `TcpPort` —— 后者是 SDK 内部的日志回显名
  #   （`Xamarin.Messaging.targets:103` 那句 `TcpPort=$(ServerTcpPort)` 是 Message 文本）。
  #   传 `TcpPort` 会被当成无名属性静默忽略：实测构建日志里那行回显是空的 `TcpPort=`，
  #   而真属性 (`:123 ServerTcpPort="$(ServerTcpPort)"`) 一直没被设上。
  PUBLISH_ARGS+=(-p:ServerAddress="$MAC_HOST" -p:ServerUser="$MAC_USER"
                 -p:ServerPassword="$MAC_PASS" -p:ServerTcpPort=58181
                 -p:_DotNetRootRemoteDirectory="$ROOT")
  echo "▸ 远程构建：$MAC_USER@$MAC_HOST（Mac 侧 dotnet：$ROOT）"
fi

echo "▸ 用 dotnet：$DOTNET"
echo "▸ TFM = $TFM   RID = $RID"
echo "▸ 签名：$KEY"
echo "▸ 描述文件：$PROFILE"

# 这里故意用 publish 而不是 build：ArchiveOnBuild 只在 publish 管线里归档并导出 .ipa。
# （.NET 8 起 iOS 的 publish 已默认 Release + ios-arm64，这里仍显式写全，
#   免得将来默认值变了之后行为悄悄漂移。）
#
# ⚠⚠ **MSYS_NO_PATHCONV=1 不能删**（Windows + Git Bash 上的致命坑）：
#   Git for Windows 会把「以 / 开头的参数值」当 POSIX 路径改写成 Windows 路径 ——
#   `-p:_DotNetRootRemoteDirectory=/Users/…` 会被送成
#   `C:/Program Files/Git/Users/…`，而 Mac 上根本没有这个位置 ⇒ SayHello 连不上，
#   报错只有一句笼统的"远程错误"。实测：
#     不加 → DotNetSdkPath=C:/Program Files/Git/Users/alecksty/Library/Caches/maui/PairToMac/SDKs/dotnet/
#     加了 → DotNetSdkPath=/Users/alecksty/Library/Caches/maui/PairToMac/SDKs/dotnet/
#   这正是本脚本"引入以来从未跑通过远程构建"的第一个真因。
#   其余参数（TFM / RID / 签名 / 地址）都不需要这层转换，全局关掉是安全的；
#   在 macOS/Linux 上该变量无副作用。
MSYS_NO_PATHCONV=1 "$DOTNET" publish WayCoder.Maui.csproj "${PUBLISH_ARGS[@]}"

OUT="bin/Release/${TFM}/${RID}/publish"
echo
IPA="$(find "$OUT" -maxdepth 1 -name '*.ipa' -print -quit 2>/dev/null || true)"
if [[ -z "$IPA" ]]; then
  echo "没在 $OUT 找到 .ipa —— 归档没成功。" >&2
  echo "  多数是签名环节：确认描述文件是 App Store 类型、证书是 Distribution、App ID 与 com.tanso.dolaima 一致。" >&2
  exit 1
fi

echo "✔ 产物：$HERE/$IPA"
ls -lh "$IPA" | awk '{print "  大小：" $5}'
VERSION="$("$DOTNET" msbuild WayCoder.Maui.csproj -getProperty:ApplicationDisplayVersion -nologo 2>/dev/null | tr -d '\r\n' || true)"
[[ -n "$VERSION" ]] && echo "  版本：$VERSION（ApplicationDisplayVersion，来自 WayCoder/Config/Global.cs）"
echo
cat <<'MSG'
下一步：把 .ipa 传到 App Store Connect。两条路：

  ① Transporter.app（最稳，Apple 官方 GUI）
     open -a Transporter   # 拖入上面的 .ipa → 交付

  ② 命令行（用 App Store Connect API Key，别用 Apple ID 密码）
     xcrun altool --upload-app -f "<上面的 .ipa>" -t ios \
       --apiKey <KEY_ID> --apiIssuer <ISSUER_ID>
     注：altool 这些年一直被 Apple 反复标记弃用，新机器上若它已不可用，
     走 ①（或用 fastlane 的 pilot）。

上传成功后在 App Store Connect 的 TestFlight 里等处理完（几分钟），
补完截图/隐私/分级等信息才能提交审核。完整清单见 docs/上架AppStore.md。
MSG
