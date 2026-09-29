#!/usr/bin/env bash
# iOS 两个脚本（`preflight-ios-appstore.sh` / `build-ios-appstore.sh`）**共用的事实**。
#
# ## 为什么要有这个文件
#
# 这两个脚本是一对（**预检通过 → 打包**），而对下面三件事它们必须给出**同一个答案**，
# 否则就会出现"预检绿灯、打包失败"这种最费时间的形态：
#
#   ① **Apple SDK 版本** —— 它同时决定 TFM（`net10.0-ios<ver>`）与本机要哪个 SDK pack
#   ② **用哪套 dotnet** —— 用错那套会在打 iOS 时报 NETSDK1147，而且**报的是 android**
#      （因为还原会评估全部 TFM），完全看不出真身
#   ③ **Mac 侧 dotnet 的根目录** —— 远程构建要 `-p:_DotNetRootRemoteDirectory`
#
# 本仓头号坑就是「**同一规则两处实现、只修了其中一处**」，这三项各写一遍必然漂。
# 写死两份时实测已经漂过一次：预检按 `WAYCODER_DOTNET → PATH` 解析，而打包脚本是
# `WAYCODER_DOTNET → /usr/local/share/dotnet → PATH` —— 在 Mac 上（homebrew 与官方
# 两套并存）两者会指向不同的 dotnet。
#
# ## 用法
#
#   HERE="$(cd "$(dirname "$0")" && pwd)"; cd "$HERE"
#   source "$HERE/ios-build-common.sh"
#   SDK_VER="$(ios_sdk_version)"
#   DOTNET="$(ios_resolve_dotnet)"
#   ROOT="$(ios_mac_dotnet_root "$MAC_USER")"

# ① Apple SDK 版本：与 csproj 的 `<AppleSdkVersion>` 默认值同源（都是 27.0）。
#    可用环境变量或 `-p:AppleSdkVersion=` 覆盖；**改了它就要一并覆盖 csproj 的那个属性**，
#    否则 TargetFrameworks 里的 `net10.0-ios$(AppleSdkVersion)` 与实际 `-f` 对不上 ⇒ NETSDK1005。
ios_sdk_version() { echo "${AppleSdkVersion:-27.0}"; }

# ② 用哪套 dotnet：显式指定 > 官方安装位 > PATH。
#    ⚠ 为什么**优先官方安装位**：本机常有两套（homebrew 的 `/opt/homebrew/bin/dotnet`
#      与官方的 `/usr/local/share/dotnet/dotnet`），而 maui 工作负载通常只装在官方那套。
#      PATH 上那套 `workload list` 看着有 maui、真构建却报 NETSDK1147。
#    ⚠ Windows 上没有 `/usr/local/share/dotnet`，会自动落到 PATH 上的 dotnet（正确）。
ios_resolve_dotnet() {
  if [[ -n "${WAYCODER_DOTNET:-}" ]]; then echo "$WAYCODER_DOTNET"; return; fi
  if [[ -x /usr/local/share/dotnet/dotnet ]]; then echo /usr/local/share/dotnet/dotnet; return; fi
  echo dotnet
}

# ③ Mac 侧 dotnet 的根目录：VS 2026 铺到 `maui/PairToMac`，VS 2022 铺到 `Xamarin/XMA`。
#    路径写错只会报"连不上/找不到 dotnet"，看不出是路径问题。
#    参数：Mac 登录用户名。
ios_mac_dotnet_root() {
  echo "${WAYCODER_MAC_DOTNET_ROOT:-/Users/$1/Library/Caches/maui/PairToMac/SDKs/dotnet/}"
}
