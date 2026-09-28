# winget 发布指南

WayCoder 的 Windows 便携版通过 `microsoft/winget-pkgs` 社区仓库分发。

> **分工铁律（2026-09-28 定）**：**Gitee 存代码、GitHub 发行**。
> 源码仓库在 Gitee 且是**私有**的（匿名访问实测 403），它**永远不会**把资产发给终端用户。
> ⇒ Release 资产、`InstallerUrl`、`PackageUrl`、`PublisherUrl`、`PrivacyUrl`、brew formula、
> apt 仓库**一律指向 GitHub**。清单里出现 `gitee.com` 会被自动化域名校验与人工审核同时判不合格。

## 一、manifest 结构

```
packaging/winget/manifests/a/Aleckstygit/WayCoder/<版本>/
├── Aleckstygit.WayCoder.yaml              # version
├── Aleckstygit.WayCoder.locale.zh-CN.yaml # defaultLocale（含 PublisherUrl / PrivacyUrl / Author / Copyright）
└── Aleckstygit.WayCoder.installer.yaml    # installer（GitHub URL + 真实 sha256）
```

由 `scripts/release.sh`（或 `release.ps1`）自动生成：它会从上一版目录复制、替换版本号与 sha256，
**并在第 4.5 步做一致性自检**（回读清单里的 sha，必须等于本次 `dist/` 产物哈希，对不上直接失败）。

## 二、⚠ 核心约束：清单 sha 必须等于 **GitHub 上实际托管**的那份资产

这是唯一一个会让用户「装不上」的坑，且报错隐晦。有两条自洽的路，**选一条走到底，别混**：

### 路 A（本仓现行）：以本地 `dist/` 为准

```bash
bash scripts/release.sh                 # 编 6 平台 + 算 sha + 生成清单
git push github v<版本>                 # 推 tag → 触发 .github/workflows/release.yml
gh run watch                            # 等 CI 跑完（它会创建 release 并上传 CI 产物）
# 用本地 dist 覆盖 CI 产物，使托管资产 == 清单所记的那份
gh release upload v<版本> --repo alecksty/waycoder --clobber \
  dist/waycoder-v<版本>-* dist/SHA256SUMS.txt
# 回验：远端 digest 必须与本地 SHA256SUMS.txt 逐项一致
```

- **为什么必须覆盖**：CI 在 GitHub runner 上重新 AOT 编译，产物与本地 `dist/` **不是同一份二进制**
  （NativeAOT 受工具链版本影响），哈希必然不同 ⇒ 不覆盖就是清单 sha 与托管资产不符。
- **为什么不能只靠 CI**：`release.yml` 的 matrix 只覆盖 4 个平台（缺 `win-arm64` / `linux-arm64`），
  winget 需要 `win-arm64`；且它的产物哈希与本地清单对不上。
- 验收命令（`release.sh` 第 6 步会打印）：
  `gh release view v<版本> --json assets --jq '.assets[] | "\(.digest|sub("sha256:";""))  \(.name)"'`
  与 `dist/SHA256SUMS.txt` 逐项 diff（`SHA256SUMS.txt` 自身不在其内容里，先滤掉）。

### 路 B：以 CI 产物为准

推 tag 后**不覆盖**，改用 Actions 产物的 sha256 重算清单，再提交 PR。
适用于本机无法完成 AOT 编译的场合。

> **两条路混用 = 安装端校验失败**（v0.96.105 事故就是这么来的：上传的资产与清单所记 sha 不同源）。
> 报错是 `Installer hash does not match`，看不出是"两份产物"。

## 三、本地校验与安装测试（Windows PowerShell）

```powershell
winget validate .\packaging\winget\manifests\a\Aleckstygit\WayCoder\<版本>\
winget install --manifest .\packaging\winget\manifests\a\Aleckstygit\WayCoder\<版本>\
```

## 四、提交 PR

```bash
gh repo fork microsoft/winget-pkgs --clone      # 已有 fork 则直接 clone
cd winget-pkgs
cp -r <waycoder-repo>/packaging/winget/manifests/a/Aleckstygit/WayCoder/<版本> \
      manifests/a/Aleckstygit/WayCoder/
git checkout -b Aleckstygit.WayCoder-<版本>
git add manifests/a/Aleckstygit/WayCoder/<版本>
git commit -m "New version: Aleckstygit.WayCoder version <版本>"
git push --set-upstream origin Aleckstygit.WayCoder-<版本>
gh pr create --title "New version: Aleckstygit.WayCoder version <版本>" --body "..."
```

## 五、⚠ 审核已明确要求的两件事（2026-09-26 版主 denelon 在 PR #426613 提出）

这两条**必须在提交前就满足**，否则 PR 会一直卡在 `REVIEW_REQUIRED`：

1. **`PrivacyUrl` 必须指向公开可匿名访问的安全/隐私文档**，且内容要说明：
   - 哪些数据（提示词 / 文件正文 / 命令输出）会发往 AI 服务商、发给谁
   - **API Key 以明文存在 `~/.waycoder/api_keys.json`（未加密）**
   ⇒ 本仓文档：[`docs/PRIVACY.md`](../../docs/PRIVACY.md)，
     链接形如 `https://github.com/alecksty/waycoder/blob/master/docs/PRIVACY.md`。
2. **发布者身份必须自洽**：manifest 的 `Publisher` / `Author` / `Copyright`
   要与 **`LICENSE` 的版权人**一致。
   （历史问题：LICENSE 曾误写 `Yufeng He`，已更正为 `施探宇 (Aleckstygit)`。）

另外：清单里**所有 URL 都要能匿名打开**。私有仓库的链接（`gitee.com/aleckstygit/*`）
会被审核与自动化校验同时判失败。

## 六、当前状态

| 项 | 状态 |
|---|---|
| 已提交 PR | #433450（0.96.105）、#430722（0.96.67）、#426613（0.96.36）—— **均未合并**，包尚未进入 winget |
| 版主反馈 | 仅在 #426613（2026-09-26），要求见第五节；按要求应把修正后的清单推回**该 PR** |
| CLA | 已于 2026-09-18 签署 |
| 待办 | 满足第五节两条 + 把清单更新到当前版本，推回 #426613 重新验证；顺带关闭重复的旧 PR |
