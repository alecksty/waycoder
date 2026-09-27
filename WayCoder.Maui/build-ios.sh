#!/usr/bin/env bash
# 打 iOS 版（**模拟器 .app**）—— 与 `build-apk.sh` 是一对。
#
# ## 为什么默认打模拟器版
#
# 真机 `.ipa` 要 **provisioning profile + 已注册的设备**：证书是签名用的，
# 但"能装到哪台机器"由描述文件说了算 —— 没注册的机器上的包**装不上去**，
# 而这一步只能在 Apple 开发者后台做（或让 Xcode 自动管理）。模拟器版不需要这些。
#
# ## ⚠ 最容易踩的一条：**TFM 带版本后缀**
#
# csproj 里写的是
#     <TargetFrameworks>net10.0-android;net10.0-ios$(AppleSdkVersion);…</TargetFrameworks>
# 而 `AppleSdkVersion` 默认 `27.0` ⇒ 真正的 TFM 是 **`net10.0-ios27.0`**，
# **不是 `net10.0-ios`**。写成裸的会报：
#
#     error NETSDK1005: 资产文件"…/project.assets.json"没有"net10.0-ios"的目标
#
# ⚠ 那条报错**完全看不出是 TFM 拼错**（读起来像"没还原"），实测为它白查过一轮。
# 想换 Xcode 版本不用改 csproj：`-p:AppleSdkVersion=26.5`。
#
# ## ⚠ 第二条：「看起来像环境损坏」的报错先清 obj
#
# 模拟器起不来、报 static registrar 哈希不匹配之类的，**先删 `obj/`/`bin/` 重编**
# （秒级）。那是 `obj/` 里残留着别的 SDK 版本编出来的中间产物，
# 而 `dotnet workload repair`（分钟级）多半治不了它。
#
# 用法:
#   WayCoder.Maui/build-ios.sh                # 模拟器版（默认）
#   WayCoder.Maui/build-ios.sh --clean        # 先清 obj/bin 再打
#   WayCoder.Maui/build-ios.sh --device       # 试真机版（需描述文件，多半会卡在这里）
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE"

SDK_VER="${AppleSdkVersion:-27.0}"
CLEAN=0
TARGET=sim
for a in "$@"; do
  case "$a" in
    --clean)  CLEAN=1 ;;
    --device) TARGET=device ;;
    -h|--help) sed -n '2,30p' "$0"; exit 0 ;;
    *) echo "未知参数: $a"; exit 2 ;;
  esac
done

if [[ "$CLEAN" == 1 ]]; then
  echo "▸ 清 obj/bin（net10.0-ios）…"
  rm -rf obj/Debug/net10.0-ios* obj/Release/net10.0-ios* \
         bin/Debug/net10.0-ios* bin/Release/net10.0-ios*
fi

if [[ "$TARGET" == "device" ]]; then
  RID="ios-arm64"
  echo "▸ 真机版 —— 需要 provisioning profile；没有的话这一步会失败在签名上。"
else
  RID="iossimulator-arm64"
fi
TFM="net10.0-ios${SDK_VER}"

# ⚠⚠ **必须用"装了 maui 工作负载"的那个 dotnet** —— 与 `build-apk.sh` 同一条（那边踩过，
#   这边当初漏了）：本机有两套 —— PATH 上的 `/opt/homebrew/bin/dotnet`（`dotnet workload list`
#   在 10.0.401 波段下**是空的**）与 `/usr/local/share/dotnet/dotnet`（装了 maui 全家）。
#   用错那套的报错是 `error NETSDK1147: 必须安装以下工作负载: maui-android`（**在打 iOS 时
#   报 android**，因为还原会评估全部 TFM）——完全看不出是"走错了 dotnet"，实测白查一轮。
#   想换回去：`WAYCODER_DOTNET=/path/to/dotnet ./build-ios.sh`。
DOTNET="${WAYCODER_DOTNET:-}"
if [[ -z "$DOTNET" ]]; then
  if [[ -x /usr/local/share/dotnet/dotnet ]]; then DOTNET=/usr/local/share/dotnet/dotnet; else DOTNET=dotnet; fi
fi

echo "▸ 用 dotnet：$DOTNET"
echo "▸ TFM = $TFM   RID = $RID"
"$DOTNET" build WayCoder.Maui.csproj -f "$TFM" -c Release -p:RuntimeIdentifier="$RID"

APP="bin/Release/${TFM}/${RID}/WayCoder.Maui.app"
echo
if [[ -d "$APP" ]]; then
  echo "✔ 产物：$HERE/$APP"
  du -sh "$APP" | awk '{print "  大小：" $1}'
  echo
  echo "装进模拟器并启动："
  echo "  xcrun simctl boot <UDID>            # xcrun simctl list devices available"
  echo "  xcrun simctl install <UDID> \"$APP\""
  echo "  xcrun simctl launch <UDID> com.tanso.waycoder"
  echo
  echo "⚠ Xcode 27 起 Simulator.app 没了（改用 DeviceHub.app），但 xcrun simctl 一侧照旧。"
else
  echo "✘ 没找到 $APP —— 构建没产出 .app。"
  exit 1
fi
