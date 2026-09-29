# 上架 App Store

WayCoder 手机端（`WayCoder.Maui`）的 iOS / Mac Catalyst 上架流程。
**本文只讲"怎么把包送进 App Store"**；移动端功能本身见 [移动端编辑器待实现](移动端编辑器待实现.md)。

> ⚠ 一句话前提：**Xcode 那条工具链得有一台 Mac 在**（签名 / `codesign` / 归档都跑在它上面），
> 但**发起方可以是 Windows**：Visual Studio 或 `dotnet` CLI 走
> [Pair to Mac](https://learn.microsoft.com/dotnet/maui/ios/pair-to-mac) 把活派给 Mac，
> **产出的 `.ipa` 会拷回 Windows 本机** ⇒ 在这台开发机上就能一条龙跑完。
> 真正做不到的只有一件事：**手边一台 Mac 都没有**时没法打包，也没法上传。

## 一、硬门槛（这三样没有就动不了）

| 要件 | 说明 |
|---|---|
| Apple Developer Program 会员 | 个人 / 公司均可，$99/年。**企业版（Enterprise）进不了 App Store**，那是内部分发用的 |
| Distribution 证书 | 属于你的**团队**，用来证明"这个包是你发的"。在开发者后台 Certificates 里建 |
| App Store 描述文件 | 绑定「App ID + Distribution 证书」，决定了这个包**能装到哪**（App Store 就是"哪都能装"） |

另外还需要 **App Store Connect 里的 App 记录**（App 的身份证 + 所有上架资料）。

证书与描述文件**不必非在网页后台建**：在 Windows 的 Visual Studio 里
（`Tools > Options > Xamarin > Apple Accounts`）也能创建 iOS Distribution 证书，
创建/下载的描述文件会**自动导出到配对的那台 Mac**。哪边顺手用哪边。

已有事实（不用再定）：

- Bundle ID = `com.tanso.dolaima`（即 csproj 的 `<ApplicationId>`）。在开发者后台
  建 App ID 时必须**逐字**是这个（Explicit 类型，不能用通配符）。
- 显示名：英文取 `<ApplicationTitle>`，中文取 `Platforms/iOS/Resources/zh-Hans.lproj/InfoPlist.strings`（"道码"）。
- 出口合规：`Platforms/iOS/Info.plist` 已声明 `ITSAppUsesNonExemptEncryption = false`
  （只用系统 TLS ⇒ 属豁免）。**缺这个键，每次上传都会被后台标"缺少出口合规信息"**，测试和提交都走不动。
- 隐私清单：`Platforms/iOS/Resources/PrivacyInfo.xcprivacy` 已按 MAUI 最低要求内置。

## 二、版本号从哪来（不要手改）

唯一真源是 `WayCoder/Config/Global.cs` 的 `Global.Version`（形如 `v0.96.559`）。
csproj 用正则把它推导成两个苹果要的字段：

- `ApplicationDisplayVersion` = `CFBundleShortVersionString`（用户看到的 0.96.559）
- `ApplicationVersion` = `CFBundleVersion`（构建号，单调递增；算法 `major*1000000 + minor*10000 + patch`）

⇒ **改版本只改 Global.cs**。改了别处（或漏改）会出现"设置里显示 A、App Store 显示 B"的自相矛盾，
而且构建全绿、只有人眼能发现。csproj 里有 `VerifyAppVersionParsed` 守卫，解析失败会当场报错。

⚠ 传给 App Store Connect 的 `CFBundleVersion` 必须**比上一次上传的大**，同一个值会被直接拒收。

## 三、打包 .ipa

**先跑预检。** 它把那些"报错看不出真身"的失败提前问出来 —— 版本号能否被 csproj 解析、
`CFBundleVersion` 与 msbuild 推导是否一致、maui/ios 工作负载、iOS SDK pack 要求的
**Xcode 版本**、Mac 的 58181 可达性与 SSH、签名信息形状、隐私政策 URL 能不能打开：

```bash
bash WayCoder.Maui/preflight-ios-appstore.sh --mac <ip> --mac-user <名字>
# 签名从环境变量读（见 WayCoder.Maui/ios-sign.local.sh.example）；预检故意不接受 Mac 密码
```

预检全绿再打包：

```bash
cd WayCoder.Maui

# macOS 上（本机有 Xcode + 证书）
./build-ios-appstore.sh \
  --key "Apple Distribution: Your Name (TEAMID)" \
  --profile "WayCoder AppStore"

# Windows 上（需要同网段一台开着 Xcode 的 Mac）
./build-ios-appstore.sh --key "…" --profile "…" \
  --mac 192.168.1.23 --mac-user aleck --mac-pass '…'
```

`--key` 从 `security find-identity -v -p codesigning` 抄；`--profile` 是后台 Profiles 里
**App Store 类型**那个的名字（不是 Development / Ad Hoc）。
也可以用环境变量 `WAYCODER_IOS_SIGN_KEY` / `WAYCODER_IOS_PROFILE`（CI 上更顺手，命令行不留明文）。

产物：`bin/Release/net10.0-ios<SDK>/ios-arm64/publish/*.ipa`

脚本头部注释里记着三条已经踩过的坑（TFM 必须带 SDK 版本后缀、必须用装了 maui 工作负载的
那个 dotnet、报错像"环境坏了"先 `--clean`），**动手前先读那段**。
`build-ios.sh`（模拟器 .app）是另一条路，两者别混。

### Windows 远程构建（`--mac`）要注意几点

- **走的是 Visual Studio 的 Pair to Mac 协议**：Mac 侧要预置对应版本的 dotnet
  （VS 2026 在 `~/Library/Caches/maui/PairToMac/SDKs/dotnet/`，VS 2022 在
  `…/Xamarin/XMA/SDKs/dotnet/`；脚本默认取前者，可用 `WAYCODER_MAC_DOTNET_ROOT` 覆盖）。
  最省事的做法是装一次 Visual Studio 并让它与 Mac 配对成功，那会顺带铺好 SSH 密钥与 Mac 侧 dotnet。
- ⚠ **`RuntimeIdentifier` 必须显式给**：官方文档写明，Windows 上远程构建若不指定，
  会**照 Windows 机器的架构来**（这个值得在连上 Mac 之前定下来，那时还问不到 Mac 的架构）。
  脚本已固定 `ios-arm64`。
- **`.ipa` 落在 Windows 本机**（`bin\Release\net10.0-ios<SDK>\ios-arm64\publish\`）⇒
  「编译在 Mac、上传在 Windows」成立。
- 脚本是 `.sh`：Windows 上要用 Git Bash / WSL 跑（或照脚本里的命令手敲 `dotnet publish`）。

## 四、上传

三条路，按你人在哪台机器上选：

1. **Transporter.app（仅 macOS）**（Apple 官方 GUI，最稳）：`open -a Transporter`，把 `.ipa` 拖进去 → 交付。
2. **命令行（仅 macOS）**（用 App Store Connect API Key，**别用 Apple ID 密码**）：

   ```bash
   xcrun altool --upload-app -f path/to/App.ipa -t ios \
     --apiKey <KEY_ID> --apiIssuer <ISSUER_ID>
   ```

   API Key 在 App Store Connect → 用户与访问 → 集成 里生成（`.p8` 只能下一次）。

3. **在 Windows 上一路传到商店：Visual Studio 的 Archive Manager**
   （`发布…` → 归档完成 → **Distribute…** → **App Store** → 选签名/描述文件 →
   **Upload to Store** → 填 Apple ID + **App 专用密码**）。VS 会先校验包再上传，
   等价于让配对的那台 Mac 替你跑上传。

   ⚠ 反过来也要知道：**Windows 上没有 `xcrun altool`**（它属于 Xcode，只在 macOS），
   所以「纯命令行」上传在 Windows 上走不通 —— 去 Mac 上跑、或用 App Store Connect API 的
   工具链（fastlane 等）、或走上面这条 Visual Studio 的路。

上传后在 **TestFlight** 里等处理完（几分钟；首次可能更久），再回 App Store 页填资料提交审核。

## 五、提交前要补的资料

- **截图**：6.7" / 6.5" / 5.5" 三档 iPhone；本项目 `UIDeviceFamily` 含 iPad（`2`）⇒ **还要 iPad 截图**，
  否则提交会被拦。
- **App 隐私问卷**：声明"收集哪些数据"。道码本身不上报，但会**把代码/提示词发给用户自己配的
  LLM 服务商**，这一点要在问卷与隐私政策里讲清楚。
- **隐私政策 URL**（必填）。
- **年龄分级 / 类别**：Mac 端已在 `Platforms/MacCatalyst/Info.plist` 里写
  `LSApplicationCategoryType = public.app-category.lifestyle`。
- **审核备注**：写清楚"App 需要用户自备 LLM API Key 才能用"，并**提供一个可用的测试 Key**
  （否则审核员看到的是一个空壳，按 4.2 判"功能不足"）。

## 六、道码特有的审核风险（提前想好怎么答）

- **2.5.2 可执行代码**：App 内置 VML 虚拟机 + 22 个前端编译器，能跑用户自己写的代码。
  这是**随包分发的解释器**（不是运行时下载代码），通常可接受（同类先例：Pythonista、a-Shell），
  但要有准备——审核员可能就这一条提问。**不要**做"从服务器下载可执行代码"的功能，那会被直接拒。
- **4.2 最低功能性**：见上条"审核备注"，一定要给测试账号/Key。
- **5.1.1 数据收集**：LLM 传输链路要说清。
- 摄像头 / 相册 / 麦克风用途字符串已就绪（`Platforms/iOS/Info.plist`），审核不会因为缺用途说明被拒。

## 七、Mac App Store（另一条线）

Mac Catalyst 目标已在 `TargetFrameworks` 里（`net10.0-maccatalyst$(AppleSdkVersion)`），
`Platforms/MacCatalyst/Entitlements.plist` 已开 **App Sandbox + network.client**（两者都是
上 Mac App Store 的硬性要求）。

```bash
dotnet publish -f net10.0-maccatalyst27.0 -c Release -p:ArchiveOnBuild=true \
  -p:CodesignKey="Apple Distribution: … (TEAMID)" \
  -p:CodesignProvision="WayCoder Mac AppStore"
```

- ⚠ **Mac App Store 不接受只有 `maccatalyst-arm64` 的包** —— 要么两个架构都给，要么只给 `maccatalyst-x64`
  （csproj 里那段 Note 就是记这个）。
- 沙箱下若还要读写用户指定的任意目录，得补 `com.apple.security.files.user-selected.*` 权限，
  否则会出现"能选文件但读不到内容"。
- 具体命令与导出格式以官方文档为准：
  [Publish a Mac Catalyst app for the Mac App Store](https://learn.microsoft.com/dotnet/maui/mac-catalyst/deployment/publish-app-store)。
- `Platforms/MacCatalyst/Info.plist` 里那份 `UIApplicationSceneManifest` 与 iOS 那份是**成对**的，
  改一处必须改另一处（那边注释里写了：漏一处 macOS 版会 SIGTRAP 秒退）。

## 八、检查清单

- [ ] **预检脚本全绿**：`bash WayCoder.Maui/preflight-ios-appstore.sh --mac <ip> --mac-user <名字>`
- [ ] `WayCoder/Config/Global.cs` 的版本号已升，且 `CFBundleVersion` 大于上次上传值
- [ ] 开发者后台：App ID（`com.tanso.dolaima`）、Distribution 证书、App Store 描述文件三件套齐
- [ ] App Store Connect 里 App 记录已建，Bundle ID 选的是上面那个
- [ ] （在 Windows 上跑的话）已与一台装着 Xcode 的 Mac 配对成功（Pair to Mac）
- [ ] `build-ios-appstore.sh` 出了 `.ipa`（不是模拟器 `.app`）
- [ ] 上传成功，TestFlight 处理完成
- [ ] 截图（含 iPad）、隐私问卷、**隐私政策 URL**、年龄分级、类别已填
      —— 逐屏操作见 [上架逐屏操作](上架逐屏操作.md)
- [ ] 审核备注写了"需自备 API Key"并给了可用测试 Key
- [ ] **内购真机验收十条已过**（[上架逐屏操作](上架逐屏操作.md) §七）——
      注意上架 `.ipa` 装不上真机，要另用开发签名包
- [ ] 提交审核

## 九、常见失败对照

| 现象 | 真身 |
|---|---|
| `NETSDK1005: 资产文件…没有"net10.0-ios"的目标` | TFM 漏了 SDK 版本后缀，应写 `net10.0-ios27.0` |
| 打 iOS 却报 `NETSDK1147: 需要 maui-android` | 用错 dotnet（那套没装 maui 工作负载） |
| 归档报一堆静态注册器/环境怪错 | `obj/` 残留别的 SDK 版本产物，先 `--clean` |
| 上传后后台标"缺少出口合规信息" | `ITSAppUsesNonExemptEncryption` 没进包（见本文第一节） |
| 上传被拒：版本号重复/回退 | `CFBundleVersion` 没递增（改 `Global.cs`） |
| 提交被拦"缺少 iPad 截图" | `UIDeviceFamily` 含 iPad，只传了 iPhone 截图 |
| 远程构建出来的包架构不对 | Windows 上远程构建没显式给 `RuntimeIdentifier`（脚本已固定 `ios-arm64`） |
| 构建要求 Xcode 27.0 而 Mac 上是 26.x | 本机 SDK pack 目录名 `27.0.10417-xcode27.0` 的后缀就是硬要求。两端要么都 27.0，要么都降到 26.5（`-p:AppleSdkVersion=26.5`） |
| 连不上 Mac / 找不到 dotnet | 58181 不通（没在 VS 里配对过）或 Mac 侧 dotnet 路径不对（VS 2026 是 `maui/PairToMac`，VS 2022 是 `Xamarin/XMA`）。预检 G3 组会把这两条分开报 |
| 传上去后台说隐私政策 URL 打不开 | 那个 URL 指向 GitHub，而按本仓惯例代码只推 Gitee ⇒ 改了 `docs/PRIVACY.md` 要**单独推到 github 远程**。预检的 `--privacy-url` 会在打包前就报出来 |
