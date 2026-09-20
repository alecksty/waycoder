#!/usr/bin/env bash
# 打包 Android Release APK（自签名）。
#
# 用法：
#   ./build-apk.sh              # 出 APK
#   ./build-apk.sh -t:Install   # 顺便装到已连接的设备/模拟器（adb）
#
# 产物：bin/Release/net10.0-android/publish/com.tanso.waycoder-Signed.apk
#
# 关于签名：项目里没有正式 keystore，用本目录下自签的 waycoder.keystore
# （alias / 两个密码都是 waycoder，有效期 10 年）。这是**自用/内部分发**的签名，
# 上架应用商店需要换成正式 keystore 并妥善保管。keystore 已在 .gitignore 里排除。
#
# 注意 `-p:AndroidPackageFormat=apk`：.NET Android 的 Release 默认产出 .aab（商店用），
# 不加这个参数拿到的是 bundle 而不是能直接安装的 APK。
set -euo pipefail
# 脚本目录**只解析一次**并且**转成绝对路径**。
# ⚠ 从前这里 `cd "$(dirname "$0")"`，下面又用一次 `$(dirname "$0")` —— 第二处的 `$0`
#   是**相对**路径（`WayCoder.Maui/build-apk.sh`），而此刻 cwd 已经变成 `WayCoder.Maui` 了，
#   `cd WayCoder.Maui` 于是找不到目录。文档写的是 `./build-apk.sh`（`$0` = `./build-apk.sh`，
#   `dirname` = `.`，两次都对）—— 但从仓库根敲 `bash WayCoder.Maui/build-apk.sh` 就会在
#   第 39 行炸掉，而报错长这样：
#       build-apk.sh: 行 39: /../scripts/make-vml-lib.sh: No such file or directory
#   看不出是路径解析的问题。实测踩过一次。
HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE"

# Android 工具链：优先用已设的环境变量，否则回退到本机 SDK 安装位置（与 csproj 里的探测一致）
export JAVA_HOME="${JAVA_HOME:-$HOME/Library/Android/jdk}"

KS="$PWD/waycoder.keystore"
if [[ ! -f "$KS" ]]; then
  echo "缺少签名文件 $KS —— 先生成："
  echo "  \"$JAVA_HOME/bin/keytool\" -genkeypair -v -keystore waycoder.keystore \\"
  echo "    -alias waycoder -keyalg RSA -keysize 2048 -validity 10000 \\"
  echo "    -storepass waycoder -keypass waycoder \\"
  echo "    -dname \"CN=WayCoder, OU=Dev, O=WayCoder, L=Shenzhen, ST=Guangdong, C=CN\""
  exit 1
fi

# ⚠ **签名指纹必须与 `keystore.sha256` 一致** —— 否则这个包**打得出来、装不上去**。
#
# `waycoder.keystore` 是私钥、不进 git（对），但代价是**每台机器都可能各自生成一份同名文件**，
# 于是同一份代码在两台机器上打出来的包**签名不同** ⇒ 手机上
# `INSTALL_FAILED_UPDATE_INCOMPATIBLE: signatures do not match`，
# 而那个报错**看不出是"两台机器两张证书"**，唯一的"官方"补救是 `adb uninstall`
# （会删掉用户手机上的 API Key 与会话）。实测踩过（2026-09-20）：
#   Mac 的 keystore 生成于 9-13，Windows 的生成于 9-14，手机装的是 Windows 那份
#   ⇒ Mac 这边怎么打都装不上去。
#
# 指纹是**公开信息**（印在 APK 签名里，谁都能看到），所以可以进仓库；私钥不行。
# 这个分工正是 `keystore.sha256` 存在的意义。
EXPECTED="$HERE/keystore.sha256"
if [[ -f "$EXPECTED" ]]; then
  want=$(grep -E '^[0-9A-Fa-f:]{95}$' "$EXPECTED" | head -1 | tr 'a-f' 'A-F')
  got=$("$JAVA_HOME/bin/keytool" -list -v -keystore "$KS" -storepass waycoder -alias waycoder 2>/dev/null \
        | grep -m1 'SHA256: *' | sed 's/.*SHA256: *//' | tr -d ' ' | tr 'a-f' 'A-F')
  if [[ -z "$got" ]]; then
    echo "✘ 读不出 $KS 的证书指纹（口令/别名不对？）—— 中止，别打出一个装不上去的包。"
    exit 1
  fi
  if [[ "$got" != "$want" ]]; then
    echo "✘ 签名证书指纹对不上 —— **这个包装不上任何用统一钥匙的手机**。"
    echo "    期望: $want"
    echo "    实得: $got"
    echo "  原因: waycoder.keystore 是私钥、不在 git 里，每台机器可能各自生成一份。"
    echo "  修法: 把统一的那份 waycoder.keystore 覆盖到 $KS（离线传，别走 git；"
    echo "        细节见 WayCoder.Maui/keystore.sha256 的说明）。"
    exit 1
  fi
  echo "✔ 签名指纹匹配（${want:0:14}…）"
fi

# ⚠ **先重新生成内置标准库资产** —— `Resources/Raw/vml_lib.zip` 是**签入仓库的生成物**，
#    `Lib/` 一有改动（新增 / 改名 / 删文件）它就过期，而过期的后果**只在手机上现形**：
#    桌面跑 VML 直接读 `third_party/vml/Lib/`，**根本不走「zip → APK 资产 → 设备解压」这条链**
#    ⇒ 桌面全绿、手机上是坏的。
#    实测踩过（2026-09-17，补丁 0033 新增 `Lib/lua/luatable.vml` 之后直接打包）：
#    手机上 Lua 一路报 `未找到标签: lua_table_get`，而桌面同一条语料 PASS。
#    这一步保证「打出来的包」与「当前工作树的 `Lib/`」一致。
"$HERE/../scripts/make-vml-lib.sh"

dotnet publish -f net10.0-android -c Release \
  -p:AndroidPackageFormat=apk \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$KS" \
  -p:AndroidSigningKeyAlias=waycoder \
  -p:AndroidSigningKeyPass=waycoder \
  -p:AndroidSigningStorePass=waycoder \
  "$@"

echo
echo "APK 产物："
ls -lh bin/Release/net10.0-android/publish/*.apk 2>/dev/null || true
