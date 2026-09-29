# iOS 上架 · 当前进度与待办（交接）

> **时点**：2026-09-29。这份是**某时点的状态快照**，不随代码更新。
> 目标：把 `WayCoder.Maui` 的 App Store 上架包打出来并上传。
> 上游文档：[上架AppStore](上架AppStore.md)（怎么打包上传）、
> [上架资料包](上架资料包.md)（填什么）、[上架逐屏操作](上架逐屏操作.md)（怎么点）。

## 一、进度：三道关过了两道

| 关卡 | 状态 | 说明 |
|---|---|---|
| ① 连上 Mac | ✅ **已通** | 修掉了脚本里 Git Bash 改写 Mac 路径的 bug（见下） |
| ② Mac 的 Xcode 可用 | ✅ **已通** | 用 `--xcode` 指到 `/Applications/Xcode.app/Contents/Developer` 绕过；**Xcode 27 装在标准位置，只是 `xcode-select` 没指过去** |
| ③ 签名证书 | ❌ **卡在这里** | `密钥链中找不到 iOS 代码签名密钥"Apple Distribution: tanyu shi (AF9MMXD4D3)"` |
| ④ 描述文件 | ⏳ 未知 | `WAYCODER_IOS_PROFILE` 还是模板默认值，**必须换成后台那个真实名字** |

## 二、你回去要做的（按顺序）

### 第 1 步：确认签名证书在不在钥匙串里

在那台 Mac 上跑：

```bash
security find-identity -v -p codesigning
```

- **有 `Apple Distribution: …` 开头的行** ⇒ 记下**逐字**的整行（大小写要一致），进第 2 步
- **没有** ⇒ 证书只在后台建了、没装到这台 Mac。装法二选一：
  - Xcode → Settings → Accounts → 选中 Apple ID → **Manage Certificates** → `+` → **Apple Distribution**
  - 或去开发者后台 Certificates 页把那 `.cer` 下下来双击安装

装完再跑一次上面那条命令确认。

### 第 2 步：把真实值填进本地配置

编辑 `WayCoder.Maui/ios-sign.local.sh`（**已在 gitignore 里，不会进仓库**）：

```bash
# 用第 1 步抄下来的整行，逐字不改
export WAYCODER_IOS_SIGN_KEY="Apple Distribution: <证书上的名字> (AF9MMXD4D3)"

# 开发者后台 → Profiles 里那个 **App Store 类型**描述文件的名字
# ⚠ 不是 Development / Ad Hoc；且它绑的 App ID 必须是 com.tanso.dolaima
export WAYCODER_IOS_PROFILE="<描述文件的真实名字>"
```

> 该文件现在已有：`WAYCODER_MAC_HOST=192.168.16.74`、`WAYCODER_MAC_USER=alecksty`、
> `WAYCODER_MAC_PASS`（已填）、`WAYCODER_IOS_XCODE=/Applications/Xcode.app/Contents/Developer`。
> **前四项不用动**，只改签名身份与描述文件名。

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
