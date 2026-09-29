# iOS 上架 · 当前进度与待办（交接）

> **时点**：2026-09-29。这份是**某时点的状态快照**，不随代码更新。
> 目标：把 `WayCoder.Maui` 的 App Store 上架包打出来并上传。
> 上游文档：[上架AppStore](上架AppStore.md)（怎么打包上传）、
> [上架资料包](上架资料包.md)（填什么）、[上架逐屏操作](上架逐屏操作.md)（怎么点）。

## 〇、**已经打出 `.ipa` 了**（本版结论）

`WayCoder.Maui.ipa`（64 MB）已生成并拷回 Windows：
`WayCoder.Maui/bin/Release/net10.0-ios27.0/ios-arm64/publish/WayCoder.Maui.ipa`

核验通过：标识符 `com.tanso.dolaima` / 团队 `59QH5TEYX8` / 内嵌描述文件
`com-tanso-dolaima-app` / **`get-task-allow=false`（App Store 发布型）**。

**用的是「在 Mac 上本机构建」这条路，不是 Windows 远程构建**（原因见第五节）。
可复现的步骤见第六节。

## 一、进度：全部打通

| 关卡 | 状态 | 说明 |
|---|---|---|
| ① 连上 Mac | ✅ | SSH 可达（**SSH 密钥没铺也可用密码**；见第六节） |
| ② Mac 的 Xcode 可用 | ✅ | `xcode-select -p` 实测指向 `/Library/Developer/CommandLineTools`（**不是**完整 Xcode）；用 `-p:XcodeLocation=/Applications/Xcode.app/Contents/Developer` 绕过，**不必改 Mac** |
| ③ 签名身份 | ✅ | 实测钥匙串：**`Apple Distribution: Tanyu Shi (59QH5TEYX8)`** |
| ④ 描述文件 | ✅ | `com-tanso-dolaima-app` 已安装到 Mac |
| ⑤ 打出 `.ipa` | ✅ | 见第〇节 |

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

## 四、剩下的事

`.ipa` 已有 ⇒ 只剩**上传与提交**：

1. 上传（[上架AppStore](上架AppStore.md) §四）——Transporter / `xcrun altool` / VS 的 Archive Manager
2. App Store Connect 阶段 0~2（协议/银行/税务、建 App、建内购）——[上架逐屏操作](上架逐屏操作.md)
3. **内购真机验收十条**（同上 §七）——⚠ 上架 `.ipa` 装不上真机，要另出**开发签名**包
4. 阶段 3~4：元数据、截图、提交

已备好、不用再折腾的：**审核用 API Key**（DeepSeek，实测可用）存在本地 gitignored 的
`WayCoder.Maui/ios-review.local.md` —— ⚠ 审核通过后记得吊销。

## 五、为什么不用 Windows 远程构建（实测结论）

`build-ios-appstore.sh --mac` 那条路**撞的是 SDK 自己的缺陷**，不是配置问题。链路是通的
（SayHello 连上、Mac 上真的开始编译、IL 裁剪都跑完），最后败在：

```
System.ArgumentNullException: Value cannot be null. (Parameter 'path2')
   at System.IO.Path.Combine(String path1, String path2, String path3)
   at Xamarin.Messaging.Build.ExecuteTaskMessageHandler.ExecuteAsync
```

读公开源码 `dotnet/macios` 的 `ExecuteTaskMessageHandler.cs` 定位到确切那一行：

```csharp
var buildDirectory = Path.Combine (MessagingContext.BuildsPath, message.AppName, message.SessionId);
```

**`message.AppName` 是 null** —— 由 **Windows 侧客户端**填充、而命令行这条路没填
（SayHello 参数里 `DotNetRuntimePath=` 与 `VisualStudioProcessId=` 也是空的；SDK 只消费这两个属性、
从不定义它们 ⇒ 那是 VS 才会给的）。日志里那条**字面未替换的
`execute-task/{AppName}/…` 话题**正是同一件事的表现。

排查中**用证据否掉了两个更"顺口"的假设**，记下来免得下次重走：

| 假设 | 反证 |
|---|---|
| Mac 上代理版本旧 | 客户端请求的就是 `18.3.99.2099`，两端版本一致 |
| 代理部署损坏 | Mac 上 `Build.dll` 与 Windows 侧 `Build.zip` **SHA256 完全一致**（45 个文件） |

⇒ **`--mac` 那条路暂时不用**。要用得等上游修；或者用 VS（VS 会设置那些属性，走的是官方支持路径）。

## 六、可复现的打包方法：在 Mac 上本机构建

**绕开整个远程消息层**，实测一次跑通。步骤（在 Windows 上就能全程驱动）：

```bash
# ① 导出源码并传到 Mac（⚠ 用 ssh 管道，scp 不走 SSH_ASKPASS 会 Connection closed）
git archive --format=tar.gz -o /tmp/wc-src.tgz HEAD          # 只含被跟踪文件，天然排除 scratch/bin/obj
MAC_PW='<Mac 密码>' SSH_ASKPASS=<echo密码的脚本> SSH_ASKPASS_REQUIRE=force \
  ssh alecksty@<Mac IP> 'cat > /tmp/wc-src.tgz' < /tmp/wc-src.tgz
ssh alecksty@<Mac IP> 'mkdir -p ~/waycoder-build && tar -xzf /tmp/wc-src.tgz -C ~/waycoder-build'

# ② ⚠ **先解锁钥匙串** —— SSH 会话下登录钥匙串是锁的，不解锁 codesign 必报
#    `errSecInternalComponent`（SDK 的报错信息自己就写了这一条）
ssh alecksty@<Mac IP> "security unlock-keychain -p '<Mac 密码>' ~/Library/Keychains/login.keychain-db"

# ③ 在 Mac 上打包（**不给 ServerAddress** ⇒ 纯本机构建）
ssh alecksty@<Mac IP> '
  cd ~/waycoder-build && /usr/local/share/dotnet/dotnet publish WayCoder.Maui/WayCoder.Maui.csproj \
    -f net10.0-ios27.0 -c Release -p:ArchiveOnBuild=true \
    -p:RuntimeIdentifier=ios-arm64 -p:PlatformTarget=AnyCPU \
    -p:CodesignKey="Apple Distribution: Tanyu Shi (59QH5TEYX8)" \
    -p:CodesignProvision="com-tanso-dolaima-app" \
    -p:XcodeLocation=/Applications/Xcode.app/Contents/Developer'

# ④ 取回产物
ssh alecksty@<Mac IP> "cat ~/waycoder-build/WayCoder.Maui/bin/Release/net10.0-ios27.0/ios-arm64/publish/WayCoder.Maui.ipa" \
  > WayCoder.Maui/bin/Release/net10.0-ios27.0/ios-arm64/publish/WayCoder.Maui.ipa
```

**这条路上用到的三个 Mac 侧事实**（都是实测，不是猜的）：

- Mac 上**有一套系统 dotnet** `/usr/local/share/dotnet/dotnet`（10.0.401），
  且装了完整的 **`maui` 工作负载**（含 `Microsoft.Maui.Controls.Runtime.ios`）——
  PairToMac 那套（`~/Library/Caches/maui/PairToMac/SDKs/dotnet`）**只有 `ios`、没有 `maui`**，用它编不了
- ⚠ **`security unlock-keychain` 是必须的一步**，不是可选的排错手段
- 传输用 `ssh 'cat > 文件'` 管道；**`scp` 在 Windows 上不走 `SSH_ASKPASS`**，会报 `Connection closed`

> 顺带：这条路的产物与远程构建**同源同签名**（核验过标识符/团队/描述文件/get-task-allow），
> 所以不是"退而求其次"，而是当前更可靠的那条。

