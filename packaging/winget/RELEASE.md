# winget 发布指南

都来码（Dolaima）的 Windows 便携版通过 `microsoft/winget-pkgs` 社区仓库分发。

> **分工铁律（2026-09-28 定）**：**Gitee 存代码、GitHub 发行**。
> 源码仓库在 Gitee 且是**私有**的（匿名访问实测 403），它**永远不会**把资产发给终端用户。
> ⇒ Release 资产、`InstallerUrl`、`PackageUrl`、`PublisherUrl`、`PrivacyUrl`、brew formula、
> apt 仓库**一律指向 GitHub**。清单里出现 `gitee.com` 会被自动化域名校验与人工审核同时判不合格。

## 一、manifest 结构

```
packaging/winget/manifests/a/Aleckstygit/Dolaima/<版本>/
├── Aleckstygit.Dolaima.yaml              # version
├── Aleckstygit.Dolaima.locale.zh-CN.yaml # defaultLocale（含 PublisherUrl / PrivacyUrl / Author / Copyright）
└── Aleckstygit.Dolaima.installer.yaml    # installer（GitHub URL + 真实 sha256）
```

由 `scripts/release.sh`（或 `release.ps1`）自动生成：它会从上一版目录复制、替换版本号与 sha256，
**并在第 4.5 步做一致性自检**（回读清单里的 sha，必须等于本次 `dist/` 产物哈希，对不上直接失败）。

## 二、⚠ 核心约束：清单 sha 必须等于 **GitHub 上实际托管**的那份资产

这是唯一一个会让用户「装不上」的坑，且报错隐晦。

**2026-09-30 起只有一条路**：**以本地 `dist/` 为准**。原先的「路 B：以 CI 产物为准」随
`.github/workflows/`（2026-09-30 删除）一起没了 —— 那条路要求本机能出 AOT 产物，
而本仓本来就是本机编译（`release.sh` 第 1 步就是编 6 平台），CI 那套的产物哈希与本地
清单对不上、还缺 `win-arm64`，所以它从来没被真正用过（原先的流程里 CI 产物**必须**被
本地 dist 覆盖，覆盖那一步才是真正生效的那一步）。

```bash
bash scripts/release.sh                 # 编 6 平台 + 算 sha + 生成清单
gh release create v<版本> --repo alecksty/waycoder --title "v<版本>" --notes-file CHANGELOG.md
gh release upload v<版本> --repo alecksty/waycoder --clobber \
  dist/waycoder-v<版本>-* dist/SHA256SUMS.txt
# 回验：远端 digest 必须与本地 SHA256SUMS.txt 逐项一致
gh release view v<版本> --repo alecksty/waycoder --json assets \
  --jq '.assets[] | "\(.digest|sub("sha256:";""))  \(.name)"'
```

- **别再把清单 sha 写成别的东西的哈希**：清单 sha 只对 `dist/` 负责。历史上那次
  「上传的资产与清单所记 sha 不同源」（v0.96.105）报的是
  `Installer hash does not match`，看不出真身。

## 三、本地校验与安装测试（Windows PowerShell）

```powershell
winget validate .\packaging\winget\manifests\a\Aleckstygit\Dolaima\<版本>\
winget install --manifest .\packaging\winget\manifests\a\Aleckstygit\Dolaima\<版本>\
```

## 四、提交 PR

```bash
gh repo fork microsoft/winget-pkgs --clone      # 已有 fork 则直接 clone
cd winget-pkgs
cp -r <waycoder-repo>/packaging/winget/manifests/a/Aleckstygit/Dolaima/<版本> \
      manifests/a/Aleckstygit/Dolaima/
git checkout -b Aleckstygit.Dolaima-<版本>
git add manifests/a/Aleckstygit/Dolaima/<版本>
git commit -m "New version: Aleckstygit.Dolaima version <版本>"
git push --set-upstream origin Aleckstygit.Dolaima-<版本>
gh pr create --title "New version: Aleckstygit.Dolaima version <版本>" --body "..."
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
