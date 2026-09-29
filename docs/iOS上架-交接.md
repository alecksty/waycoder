# iOS 上架 · 当前进度与待办（交接）

> **时点**：2026-09-29。这份是**某时点的状态快照**，不随代码更新。
> 目标：把 `WayCoder.Maui` 的 App Store 上架包打出来并上传。
> 上游文档：[上架AppStore](上架AppStore.md)（怎么打包上传）、
> [上架资料包](上架资料包.md)（填什么）、[上架逐屏操作](上架逐屏操作.md)（怎么点）。

## 一、进度：三道关过了两道半

| 关卡 | 状态 | 说明 |
|---|---|---|
| ① 连上 Mac | ✅ **已通** | 修掉了脚本里 Git Bash 改写 Mac 路径的 bug（见下） |
| ② Mac 的 Xcode 可用 | ✅ **已通** | `xcode-select -p` 实测指向 `/Library/Developer/CommandLineTools`（**不是**完整 Xcode）；用 `--xcode` 指到 `/Applications/Xcode.app/Contents/Developer` 绕过 |
| ③ 签名身份 | ✅ **已确认** | 实测钥匙串：**`Apple Distribution: Tanyu Shi (59QH5TEYX8)`** |
| ④ 描述文件 | ❌ **卡在这里** | Mac 上**没有 `com.tanso.dolaima` 的 App Store 描述文件** |

### ⚠ 关于两个 Team ID（实测澄清）

| Team ID | 名下有什么 | 用途 |
|---|---|---|
| **`59QH5TEYX8`** | `com.tanso.mikebao` / `com.tanso.DspMan` / `com.alecksoft.*`，以及 **Apple Distribution 证书** | **发布用这个** |
| `AF9MMXD4D3` | 只有 Apple Development 证书（个人团队） | 开发用 |

`com.tanso.dolaima` 属于 **`59QH5TEYX8`**，发布证书也在这个团队 ⇒ **描述文件必须建在这个团队下**。

## 二、你回去要做的（按顺序）

### ✅ 第 1 步已完成（签名身份已确认并填好）

`security find-identity -v -p codesigning` 实测结果里有：

```
"Apple Distribution: Tanyu Shi (59QH5TEYX8)"
```

⚠ 注意与最初填错的差异：名字是 **`Tanyu Shi`**（首字母大写）、Team 是 **`59QH5TEYX8`**
（不是 `AF9MMXD4D3` —— 那是个人团队的开发证书）。已更正进本地配置。

### ❌ 第 2 步（当前唯一卡点）：造一个 `com.tanso.dolaima` 的 App Store 描述文件

实测 Mac 上 `~/Library/MobileDevice/Provisioning Profiles/` 里**没有**这个 App ID 的任何描述文件
（只有 `com.tanso.mikebao` / `com.tanso.DspMan` / `com.alecksoft.*` 与几个通配符开发证书）。

在开发者后台（**团队选 `59QH5TEYX8`**）：

1. **Identifiers** → 确认/新建 App ID：类型 **App**（explicit，不能用通配符），
   Bundle ID 逐字 **`com.tanso.dolaima`**
2. **Profiles** → `+` → **Distribution → App Store** → 选上面那个 App ID →
   选证书 **`Apple Distribution: Tanyu Shi (59QH5TEYX8)`** → 起个名字（如 `Dolaima AppStore`）→ Generate
3. **下载** `.mobileprovision` 并**双击安装到那台 Mac**
   （⚠ 必须在 Mac 上安装 —— 构建时要能读到它）
4. 把**描述文件的真实名字**告诉我（或在本地配置里填 `WAYCODER_IOS_PROFILE`）

> 也可以走 Xcode：Settings → Accounts → 选中 `59QH5TEYX8` 团队 → **Download Manual Profiles**，
> 但它只能下载**已存在**的；第 2 步的创建仍需在后台做。

### 第 3 步：跑预检

```bash
# ⚠ 在**仓库根目录**执行（即含 WayCoder/ 与 WayCoder.Maui/ 的那一层）。
#   注意仓库根本身就叫 WayCoder ⇒ 别写 `cd WayCoder`，那会进到主工程子目录里去。
source WayCoder.Maui/ios-sign.local.sh
bash WayCoder.Maui/preflight-ios-appstore.sh \
  --mac "$WAYCODER_MAC_HOST" --mac-user "$WAYCODER_MAC_USER"
```

目标：**0 项失败**。这一步会把"版本号 / 工作负载 / SDK pack / Mac 可达性 / 签名占位值"
一次性问清，省得打到一半才发现。

### 第 4 步：打 `.ipa`

```bash
cd WayCoder.Maui
source ios-sign.local.sh
bash build-ios-appstore.sh \
  --mac "$WAYCODER_MAC_HOST" --mac-user "$WAYCODER_MAC_USER" --mac-pass "$WAYCODER_MAC_PASS" \
  --xcode "$WAYCODER_IOS_XCODE"
```

产物：`bin/Release/net10.0-ios27.0/ios-arm64/publish/*.ipa`（**会拷回 Windows 本机**）。

第一次失败很正常 —— 把**完整日志**（不是 VS 那个一句话对话框）发出来即可，
命令行这条路的报错是完整的。

### 第 5 步：上传与提交

`.ipa` 出来后按 [上架AppStore](上架AppStore.md) §四 上传，再按
[上架逐屏操作](上架逐屏操作.md) 走 App Store Connect 的四个阶段。

⚠ **内购真机验收十条**（[上架逐屏操作](上架逐屏操作.md) §七）用的**不是**这个上架包 ——
App Store 类型 `.ipa` 装不上真机，要另出开发签名包。

## 三、这一轮已经修掉的东西（不用再查）

`build-ios-appstore.sh` 的 `--mac` 远程构建路径**自引入起从未跑通过**，两个真因都已修：

| # | 真因 | 现象 | 修法 |
|---|---|---|---|
| 1 | **Git Bash 改写以 `/` 开头的参数值** | `-p:_DotNetRootRemoteDirectory=/Users/…` 被送成 `C:/Program Files/Git/Users/…` ⇒ 连不上，VS 只报一句笼统的"远程错误" | 调 dotnet 时带 `MSYS_NO_PATHCONV=1` |
| 2 | **属性名写成 `TcpPort`** | SDK 认的是 `ServerTcpPort`（`TcpPort` 只是它内部的日志回显名）⇒ 被静默忽略 | 改用 `ServerTcpPort` |

顺带纠正两条**我自己写错的判据**：

- 预检原来把「58181 不可达」当 FAIL —— **错**。SDK 自己会打
  `warning: 当生成未在 Visual Studio 内运行时忽略服务器 TCP 端口` ⇒ 命令行构建**纯走 SSH**，
  现在以 **22** 为判据
- 预检原来会把模板占位值当"形状正确"放过（`Apple Distribution: 你的名字 (TEAMID)`
  恰好匹配证书形状）⇒ 现在认占位值并直接 FAIL

## 四、剩下的关卡与已知风险

- **Xcode 版本**：本机 SDK pack 是 `27.0.10417-xcode27.0`，硬要求 **Xcode 27.0**。
  已确认 Mac 上装的是 27 ⇒ 这条不阻塞。
- **`xcode-select`**：**故意没改**。用 `--xcode` 绕过更稳（改它要 sudo，系统更新还可能再次搞乱）。
  若想彻底治好，在 Mac 上跑 `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer`。
- **描述文件名未知** —— 这是第 4 步之前**必须**确定的，见第 2 步。
- **审核用 API Key 已备好**（DeepSeek，实测可用），存在本地 gitignored 的
  `WayCoder.Maui/ios-review.local.md`。⚠ 审核通过后记得吊销。
