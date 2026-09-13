#!/usr/bin/env bash
# 打包 Android Release APK（自签名）。
#
# 用法：
#   ./build-apk.sh              # 出 APK
#   ./build-apk.sh -t:Install   # 顺便装到已连接的设备/模拟器（adb）
#
# 产物：bin/Release/net10.0-android/publish/com.companyname.waycoder.maui-Signed.apk
#
# 关于签名：项目里没有正式 keystore，用本目录下自签的 waycoder.keystore
# （alias / 两个密码都是 waycoder，有效期 10 年）。这是**自用/内部分发**的签名，
# 上架应用商店需要换成正式 keystore 并妥善保管。keystore 已在 .gitignore 里排除。
#
# 注意 `-p:AndroidPackageFormat=apk`：.NET Android 的 Release 默认产出 .aab（商店用），
# 不加这个参数拿到的是 bundle 而不是能直接安装的 APK。
set -euo pipefail
cd "$(dirname "$0")"

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
