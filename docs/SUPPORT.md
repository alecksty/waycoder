# 都来码 · 支持 / Dolaima · Support

> 这份是 **App Store 元数据里「支持 URL」指向的那一页**，所以放在公开可达的地方，
> 并且中英各写一遍（审核员读英文那份）。
> 元数据里填的地址：
> `https://github.com/alecksty/waycoder/blob/master/docs/SUPPORT.md`

---

## 中文

都来码（Dolaima）是一台装在手机里的编程工作台：自带代码编辑器、22 门语言的编译器与虚拟机，
以及一个能读写你项目文件的 AI 编程智能体。

### 一、第一次打开要做的两件事

1. **配置模型 API Key**（不配就用不了「对话」）
   「设置 → 模型 → 选服务商 → 填 Key」。
   ⚠ 都来码**不自带**模型额度：Key 是你自己向服务商（DeepSeek / OpenAI / 通义 / 智谱 …）申请的，
   费用由你与那家服务商直接结算，我们不代收、不经手。
   填入的 Key 只存在本机（App 私有目录），**不会回传给我们**。

2. **认一下工作区**
   文件都放在沙箱工作区里（「文件」页就是它）。开箱自带一批示例程序，
   「命令行」页敲 `vml run examples/c/demo_std.c` 就能跑第一个程序。

### 二、常见问题

**Q：对话发不出消息 / 提示未配置 Key？**
A：去「设置 → 模型」把 Key 填上。填完回首页，那行应显示「已配置 Key · 服务商 …」。
若仍不行，检查 Key 是否过期、余额是否用完（这类报错由服务商返回，都来码只负责转述）。

**Q：AI 回答很慢或超时？**
A：都来码自身不中转请求，速度取决于你所选服务商当时的状况。
换一个更快的模型（「设置 → 模型」里可切换）通常最有效。

**Q：免费版能做什么？全能版呢？**
A：免费版：编辑器 + 编译运行 **C 语言与 VML 汇编**（无优化器）。
「全能版」（一次性内购，非订阅）：解锁 **22 门语言**的编译器与**优化器**。
在「设置 → 全能版」里购买或恢复。

**Q：换了手机，怎么恢复已购买的全能版？**
A：「设置 → 全能版 → 恢复购买」（同一 Apple ID 即可，不额外收费）。
重装 App 同样走这一步。

**Q：我的代码和数据存在哪？怎么删？**
A：全部在本机 App 私有目录里（工作区文件也在其中），**我们不收集、不上传**。
删除 App 即全部删除。唯一离开本机的数据是：你在「对话」里主动发出的内容，
以及你主动执行「检查更新 / 同步」时的请求 —— 前者直接发给你**自己配置的**模型服务商。

**Q：编译报错怎么反馈？**
A：在「命令行」页或编辑器里执行编译，把**完整报错**（含语言、文件名、行号）贴到 Issue。

**Q：能不能加某门语言 / 某个功能？**
A：到 Issue 里提，说明使用场景即可。

### 三、联系我们

- 问题与漏洞报告：<https://github.com/alecksty/waycoder/issues>
- 邮箱（也可以用这个）：<alecksty@163.com>

我们通常会在几个工作日内回复。**报编译错误时请附上语言、文件名、行号与完整报错**，
这样能省一个来回。

---

## English

Dolaima is a programming workbench that lives on your phone: a code editor, compilers plus a
virtual machine for 22 languages, and an AI programming agent that can read and edit the files
in your project.

### 1. Two things to do on first launch

1. **Configure a model API key** (without one, the chat tab does nothing)
   Go to *Settings → Model*, pick a provider, paste your key.
   ⚠ Dolaima does **not** include model credits. The key is yours, obtained from your own
   provider (DeepSeek, OpenAI, …); usage is billed by that provider directly. We never
   resell or proxy it. Your key is stored on-device only and is never sent to us.

2. **Meet the workspace**
   All files live in a sandboxed workspace (that is the *Files* tab). Sample programs ship with
   the app — on the *Command* tab, run `vml run examples/c/demo_std.c` to try one.

### 2. FAQ

**Q: The chat tab won't send / it says no key is configured.**
A: Open *Settings → Model* and enter your key. The home screen should then read
"Key configured · provider …". If it still fails, check whether the key expired or the account
is out of credit — such errors come from your provider; Dolaima only relays them.

**Q: The AI is slow, or requests time out.**
A: Dolaima does not proxy requests; speed is whatever your provider gives you at that moment.
Switching to a faster model under *Settings → Model* is usually the fix.

**Q: What does the free version include? What about Full?**
A: Free: the editor, plus compiling and running **C and VML assembly** (no optimizer).
*Full* (a one-time in-app purchase, not a subscription) unlocks the compilers and the
**optimizer** for all **22 languages**. Buy or restore it under *Settings → Full*.

**Q: I changed phones — how do I restore my purchase?**
A: *Settings → Full → Restore purchase* (same Apple ID; you are not charged twice).
Reinstalling the app works the same way.

**Q: Where is my data, and how do I delete it?**
A: Everything stays in the app's private on-device storage (including your workspace files).
We do not collect or upload it. Deleting the app deletes all of it. The only data that ever
leaves your device is what you actively send in a chat, which goes **directly to the model
provider you configured**, plus update/sync requests you explicitly trigger.

**Q: How do I report a compile error?**
A: Compile from the *Command* tab or the editor, then paste the **full** error (language, file,
line number) into an issue.

**Q: Can you add language X / feature Y?**
A: Open an issue and describe your use case.

### 3. Contact

- Questions and bug reports: <https://github.com/alecksty/waycoder/issues>
- Email (also fine): <alecksty@163.com>

We usually reply within a few business days. When reporting a compile error, please include the
language, file name, line number and the **full** error text — it saves a round trip.
