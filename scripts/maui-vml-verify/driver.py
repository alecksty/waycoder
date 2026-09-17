"""安卓设备端 UI 驱动 —— 把一个 MAUI 命令行页当成可控的黑盒。

## 为什么要这么绕

`WayCoder.Maui` 没有 CLI，也没有测试钩子（`autotest.flag` 那套在 v0.96.90 前就删了）。
设备上唯一能敲 `vml run <文件>` 的地方是**命令行页的输入框**。
所以本模块做三件在裸 `adb` 里很容易做错的事：

1. **把命令送进输入框**（`input text` + `keyevent 66`），并**回读校验**：
   `input text` 会静默丢字符、"整条设备命令不加双引号"会被设备上的 shell 吃掉 `>`/`|`/`;`，
   这两条以前各制造过一次误判。这里每一条命令都有"输入框里现在是不是我要的那串"的断言，
   不一致就重来，绝不让一条没送进去的命令被读成"程序没输出"。
2. **判"跑完了没有"**。命令行页把最右侧那个按钮的文字当状态灯（空闲 = `运行`，忙碌 = `…`，
   见 `ShellPage.SetBusy`）。轮询它比 `sleep 固定秒数` 可靠得多 —— 一个 C 程序编译要一两分钟，
   而 `vml test` 只要几秒，固定 sleep 不是等太久就是没等到。
3. **把输出读回来**。输出区是一个 `Label`，`uiautomator dump` 能拿到它的**全文**
   （实测 250 字符的旧输出一字不差）。取"面积最大的 TextView"= 输出区 ——
   页面上其余文字节点（提示符、cwd、按钮）都小得多，且这一条不依赖硬编码坐标。

## 坑（都踩过）

- `input text` 送进去之前**必须先点一下输入框**。点「清屏」按钮会夺走焦点，
  之后 `input text` 的字符会落到没有焦点的窗口上、`keyevent 66` 什么也不做 ——
  现象是"命令没跑、输出是上一轮的"，极容易读成"这条命令没输出"。
- 输入框为空时 `uiautomator` 报的是 **placeholder**（`输入 shell 命令…`），不是空串。
  拿它当"有内容"就会每次狂发 60 次退格。
- 软键盘（Gboard）会和注入的按键抢。`disable_ime()` 把输入法关掉之后按键直达应用，
  整条链的时序噪声小很多。
"""

import html
import re
import subprocess
import time

# 输入框为空时 uiautomator 回报的 placeholder 文案（ShellPage.xaml 里的 Placeholder）
PLACEHOLDERS = ("输入 shell 命令…", "程序在等一行输入…")

# ── 字符 → 键码 ────────────────────────────────────────────────────────
# 为什么不用 `input text`：它要穿过输入法（Gboard），实测**时好时坏** ——
# 同一串 `vml test` 有时进得去、有时输入框保持空，成功率低到没法当验收装置的地基。
# 键码注入走的是另一条路（不经过输入法），确定得多。
#
# 只覆盖**语料里真实用到的字符**；遇到没覆盖的字符直接报错，绝不静默丢字符 ——
# 静默丢字符正是"命令没送进去却被读成程序没输出"那个误判的来源。
_KEYCODES = {}
for _i in range(26):
    _KEYCODES[chr(ord("a") + _i)] = 29 + _i
    _KEYCODES[chr(ord("A") + _i)] = 29 + _i          # 大写用同一键（注入时不带 shift，落成小写）
for _i in range(10):
    _KEYCODES[chr(ord("0") + _i)] = 7 + _i
_KEYCODES.update({" ": 62, ".": 56, ",": 55, "/": 76, "-": 69, "=": 70, ":": 74, "~": 69})


def keycodes_for(text):
    bad = sorted({c for c in text if c not in _KEYCODES})
    if bad:
        raise ValueError("字符 %r 没有键码映射 —— 设备路径请只用 字母/数字/空格 . / - , = :"
                         % "".join(bad))
    return [_KEYCODES[c] for c in text]


class Driver:
    def __init__(self, serial="emulator-5554", pkg="com.tanso.waycoder", verbose=True):
        self.serial = serial
        self.pkg = pkg
        self.verbose = verbose
        self._entry_xy = None
        self._clear_xy = None
        self._tabbar_y = None

    # ── 底层 ────────────────────────────────────────────────────────────
    def sh(self, *args):
        return subprocess.run(["adb", "-s", self.serial] + [str(a) for a in args],
                              capture_output=True, text=True).stdout

    def log(self, msg):
        if self.verbose:
            print(msg, flush=True)

    # ── UI 树 ───────────────────────────────────────────────────────────
    def nodes(self):
        self.sh("shell", "uiautomator", "dump", "/sdcard/vmlverify.xml")
        raw = self.sh("shell", "cat", "/sdcard/vmlverify.xml")
        out = []
        for m in re.finditer(r"<node[^>]*?>", raw):
            s = m.group(0)

            def g(k):
                mm = re.search(k + r'="([^"]*)"', s)
                return html.unescape(mm.group(1)) if mm else ""

            b = re.match(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", g("bounds"))
            x1, y1, x2, y2 = (int(b.group(i)) for i in (1, 2, 3, 4)) if b else (0, 0, 0, 0)
            out.append(dict(text=g("text"), cls=g("class"), desc=g("content-desc"),
                            x1=x1, y1=y1, x2=x2, y2=y2,
                            focused=(g("focused") == "true")))
        return out

    @staticmethod
    def _center(n):
        return (n["x1"] + n["x2"]) // 2, (n["y1"] + n["y2"]) // 2

    def output_label(self, ns=None):
        """输出区 = 面积最大的 TextView（页面上别的文字节点都小得多）。"""
        best = None
        for n in (ns if ns is not None else self.nodes()):
            if n["cls"] != "android.widget.TextView":
                continue
            area = (n["x2"] - n["x1"]) * (n["y2"] - n["y1"])
            if best is None or area > best[0]:
                best = (area, n)
        return best[1]["text"] if best else ""

    def _button(self, ns, texts):
        for n in ns:
            if n["cls"] == "android.widget.Button" and n["text"] in texts:
                return n
        return None

    def run_button(self, ns=None):
        return self._button(ns if ns is not None else self.nodes(), ("运行", "…"))

    def is_idle(self, ns=None):
        b = self.run_button(ns)
        return b is not None and b["text"] == "运行"

    # ── 导航 / 恢复 ─────────────────────────────────────────────────────
    def on_shell_page(self, ns=None):
        """命令行页的判据是**那个「运行」按钮**。

        ⚠ 别拿「清屏」当判据：它在某些布局下 bounds 是 `[0,0][0,0]`（没被布局到），
        但节点仍在树里 —— 用它判"在命令行页"会得到 True 却点不到。
        """
        return self._button(ns if ns is not None else self.nodes(), ("运行", "…")) is not None

    def tabbar_ready(self, ns=None):
        for n in (ns if ns is not None else self.nodes()):
            if n["desc"] == "命令行":
                return True
        return False

    def goto_shell_tab(self, tries=6):
        """点底部「命令行」Tab，**反复确认真的切过去了**。

        ⚠ 点一下不代表切过去了：冷启动时 Shell 还在初始化，它稍后会把当前项设回默认
        （首页），把我们那一下点掉的。只看"点完那一刻在不在命令行页"会拿到假阳性 ——
        表现是整轮都以为自己在命令行页、实际每条命令都在首页上敲（一条都送不进去）。
        所以这里**点一次、确认一次，不成就再点**。
        """
        for _ in range(tries):
            ns = self.nodes()
            if self.on_shell_page(ns):
                return True
            for n in ns:
                if n["desc"] == "命令行":
                    self.sh("shell", "input", "tap", *self._center(n))
                    break
            time.sleep(2.5)
        return self.on_shell_page()

    def recover(self):
        """从任何意外页面回到命令行页（例如程序开了绘图窗口没关）。

        ⚠ **只在真的看见绘图窗口时才按 BACK**。别的页面上按 BACK 会**退出 App**
        （首页/对话页的返回就是退出）—— 那样整轮会在一个已经退到后台的 App 上敲命令，
        而且看起来"每条都失败得很奇怪"。
        """
        for _ in range(3):
            ns = self.nodes()
            if self.on_shell_page(ns):
                return True
            if self.window_open(ns):
                self.sh("shell", "input", "keyevent", "4")
                time.sleep(2)
                continue
            if self.goto_shell_tab():
                return True
            time.sleep(1)
        return False

    def start_app(self):
        line = self.sh("shell", "cmd", "package", "resolve-activity", "--brief",
                       "-c", "android.intent.category.LAUNCHER", self.pkg).strip().splitlines()
        comp = line[-1].strip() if line else ""
        if "/" in comp:
            self.sh("shell", "am", "start", "-n", comp)
        else:
            self.sh("shell", "monkey", "-p", self.pkg, "-c", "android.intent.category.LAUNCHER", "1")

    def restart_app(self, attempts=3):
        """冷启动到命令行页（用在整轮开始、或上一轮把界面留在了别处时）。

        ⚠ **必须先 `force-stop` 再 `am start`。**
        只 `am start` 是把**已经在前台的那个实例**拉到前台 —— 如果里面还有一个
        VML 程序在跑（超时中止没杀干净就是这种情形），命令行页会一直停在「忙碌」态：
        `CmdEntry.IsEnabled = !busy`，输入框是**禁用**的，注入的键码全部丢掉。
        表现就是接下来每一条都"命令未能送进输入框"，看着像驱动坏了、其实是一个
        前一轮的僵尸程序在挡路。
        """
        for _ in range(attempts):
            self.sh("shell", "am", "force-stop", self.pkg)
            time.sleep(2)
            self.start_app()
        # 冷启动要等 MAUI 起来。**等的是底部 Tab 栏**而不是命令行页 —— 冷启动落在「首页」，
        # 等命令行页会白等。Tab 栏一出现就点过去，**并确认真的切过去了**（见 goto_shell_tab）。
            for _ in range(30):
                time.sleep(2)
                if self.tabbar_ready() and self.goto_shell_tab():
                    return True
            # 起不来（被前一轮的 BACK 退出过 / 启动慢）—— 停掉重来一次
            self.sh("shell", "am", "force-stop", self.pkg)
            time.sleep(2)
        return False

    # ── 输入 ────────────────────────────────────────────────────────────
    def _entry(self, ns=None):
        """命令输入框 = **最靠下**的那个 EditText。

        ⚠ 原来写的是"y > 1500"，那是照着某一次 dump 的坐标硬编码的 ——
        换台设备/换次布局（键盘开关、旋屏）就找不到，症状是"命令送不进去"。
        命令行页上 EditText 只有两个（命令框、stdin 框），最下面那个就是命令框。
        """
        cands = [n for n in (ns if ns is not None else self.nodes())
                 if n["cls"] == "android.widget.EditText"]
        return max(cands, key=lambda n: n["y1"]) if cands else None

    def entry_text(self, ns=None):
        e = self._entry(ns)
        if e is None:
            return None
        return "" if e["text"] in PLACEHOLDERS else e["text"]

    def entry_text_stable(self, tries=3, gap=0.45):
        """读输入框内容，**连续两次读到同一个值才认**。

        ⚠ `uiautomator dump` 会返回**上一次的树**（它自己也要时间，键盘动画期间尤其明显）。
        只读一次就拿来判"命令送对了没有"，会在最坏的情况下**把没送进去的当成送进去了** ——
        实测过一条 `vml run x.basvml run x.bas`（命令被键入两遍、没清掉）被判成"已送达"，
        App 收到的是一条拼起来的怪路径，报 `找不到文件：…/x.basvml run x.bas`。
        这种失败最难查：看起来像 VML 的路径解析坏了，其实是采集端读了一张旧图。
        """
        prev = self.entry_text()
        for _ in range(tries):
            time.sleep(gap)
            cur = self.entry_text()
            if cur == prev:
                return cur
            prev = cur
        return prev

    def focus_entry(self, tries=4):
        """把键盘焦点送到命令输入框，**并确认它真的拿到了**。

        ⚠ 用 `uiautomator` 报的 `focused` 属性当判据，不要"点一下就当它聚焦了"。
        没聚焦的输入框上，注入的键码和文字**全部丢掉、且没有任何报错** ——
        现象是"输入框一直是空的"，很容易被读成"命令跑了但没输出"。
        这里每次点击之后都回读取证，点不中（坐标过期 / 布局变了）就重新量一遍坐标再点。
        """
        for _ in range(tries):
            e = self._entry()
            if e is None:
                # ⚠ **找不到输入框不要立刻放弃**：页面切换/键盘起落的那一两秒里，
                # dump 抓到的是一张还没有输入框的中间帧（实测 item 1 就死在这里，
                # 整条只花了 3 秒，看起来像"命令没送进去"）。
                time.sleep(1.2)
                continue
            if e["focused"]:
                return True
            self._entry_xy = self._center(e)
            self.sh("shell", "input", "tap", *self._entry_xy)
            time.sleep(0.5)
        return self.entry_text() is not None and self._entry().get("focused", False)

    def clear_output(self):
        if self._clear_xy is None:
            for n in self.nodes():
                if n["cls"] == "android.widget.Button" and n["text"] == "清屏":
                    self._clear_xy = self._center(n)
                    break
        if self._clear_xy is None:
            return False
        self.sh("shell", "input", "tap", *self._clear_xy)
        time.sleep(0.6)
        return True

    # 输入框最多 64 字符上下；一次发这么多退格足够清空，又不至于让注入拖太久。
    _MAX_CLEAR = 96

    def clear_entry(self, tries=3):
        """清空输入框。

        ⚠ 退格**只在光标左侧有字符时才删得掉**，而输入框获得焦点时光标位置没有保证
        （实测会停在行首）。这时连发退格一个字符都删不掉，而 `input text` 是**在光标处
        插入**的 —— "清空 + 重新输入"于是退化成"把新命令插到旧命令前面"，
        输入框里堆成 `cvml run skel.cccvml run skel.c…`。这个形状很有迷惑性：
        每次输入单看都像"成功打进去了"。

        所以清空前**先用一次点击把光标钉到文本末尾**：点输入框的**右端**，左对齐的短文本
        落到最后一字之后。`KEYCODE_MOVE_END` 也发，但它要穿过输入法，不保证送达（实测会丢）。
        """
        for _ in range(tries):
            if self.entry_text_stable(gap=0.3) == "":
                return True
            e = self._entry()
            if e is not None:
                self.sh("shell", "input", "tap", str(e["x2"] - 8), str((e["y1"] + e["y2"]) // 2))
                time.sleep(0.3)
            self.sh("shell", "input", "keyevent", "123")
            time.sleep(0.2)
            self.sh("shell", "input", "keyevent", *(["67"] * self._MAX_CLEAR))
            time.sleep(0.5)
        return self.entry_text() == ""

    def submit(self, cmd, tries=6):
        """把 cmd 送进输入框并回车。全程回读校验，直到输入框确实收到这串且被提交。"""
        for _ in range(tries):
            if not self.focus_entry():
                return False
            cur = self.entry_text()
            if cur is None:
                self.recover()
                continue
            if cur and not self.clear_entry():
                continue
            # ⚠ **这里不要再 focus 一次**。已经聚焦的输入框再点一下中心，之后再 `input text`
            # 实测一个字都进不去（输入框保持空）。手动逐步验证时"focus → clear → type"是通的，
            # 多插一次 focus 就变成 0/3 成功 —— 这类"多做一步反而坏"的地方只能靠逐步复现抓。
            # ⚠ 必须**整条**作为一个字符串送 `adb shell`，让设备上的 shell 去解析那对单引号：
            #   adb -s X shell "input text 'vml run a.c'"
            # 拆成 ["shell","input","text","vml run a.c"] 会被 adb 用空格拼起来再交给设备 shell，
            # 于是变成 `input text vml run a.c` —— `input` 只拿到 `vml`，命令从第一个空格就被截断。
            # 这就是"引号"那个坑的准确形状：**不是加不加引号的问题，是这一层是谁在解析**。
            try:
                self.sh("shell", "input", "keyevent", *keycodes_for(cmd))
            except ValueError as ex:
                return dict(ok=False, saw_busy=False, window=False, output="",
                            error=str(ex)) if False else False
            time.sleep(0.7)
            if self.entry_text() != cmd:
                continue
            # ⚠ **点「运行」按钮，不要发回车**（`keyevent 66`）。
            #
            # MAUI 的 `Entry.Completed` 在 Android 上是挂 `EditorAction` 触发的，
            # 而 editor action 由**输入法**发出（`ReturnType="Go"` → IME_ACTION_GO）——
            # 没有输入法时，注入的裸 KEYCODE_ENTER 落不到那条链上，`Completed` 不触发、
            # 命令就这么躺在输入框里不动。实测：同一条 `vml run skel.c`，
            # 发回车的写法"输入成功但从没跑起来"，改点按钮立刻正常。
            # 按钮走的是 `Clicked="OnRunRequested"`，与回车**同一个处理函数**，语义完全一致。
            # **两条提交路都试**：点「运行」按钮，再补一个回车。
            #
            # 单用哪一条都实测会偶发不提交（都成功过、也都失败过，取决于当时的输入法状态）：
            # 点按钮走 `Clicked="OnRunRequested"`，回车走输入法发的 EditorAction →
            # `Completed`，两条路在 MAUI 里是**两个不同的入口**，没有哪条恒稳。
            # 先点按钮（不依赖输入法），再回车兜底（按钮坐标在键盘起落时可能过期）。
            # 两条都发出去了最多是"提交两次"，而第二次会因为 `_busy` 被 `OnRunRequested`
            # 直接挡掉（`if (_busy) return;`），不会真的跑两遍。
            b = self.run_button()
            if b is not None:
                self.sh("shell", "input", "tap", *self._center(b))
                time.sleep(1.2)
                if self.entry_text_stable() == "":
                    return True
            self.sh("shell", "input", "keyevent", "66")
            time.sleep(1.2)
            if self.entry_text_stable() == "":
                return True
            # 还是没提交（偶发）—— 清掉重来，别让残留污染下一条
            self.clear_entry()
        return False

    # ── 跑一条命令 ──────────────────────────────────────────────────────
    # 宿主对话框上的按钮文案（VML 的 `ui_dlg_msg` 落到 MAUI 的 DisplayAlert 上）
    DIALOG_OK = ("允许", "确定", "好的", "继续", "是")

    def accept_host_dialog(self, ns=None):
        """遇到宿主弹的模态对话框就**点允许**。

        为什么必须有：`ui_dlg_msg` 这类调用会**阻塞 VM 线程**等用户点按钮，
        而 `examples/c/gomoku.c` 开局第一件事就是弹一个「五子棋 / 你执黑先行…」的介绍框。
        自动化跑的时候没人点它 —— 于是程序不动、绘图窗口不出现，被记成
        「没走到 ui_win_open」，**把一个完全正常的程序判成坏的**。
        （实测踩到：屏幕上就摆着「拒绝 / 允许」两个按钮，采集端在下面傻等超时。）

        ⚠ 这是**有意的放行**：装置只在"无人可问"的场景跑，权限类弹框一律点允许。
        要测拒绝路径得另外造用例，别指望这里。
        """
        for n in (ns if ns is not None else self.nodes()):
            if n["cls"] == "android.widget.Button" and n["text"] in self.DIALOG_OK:
                self.sh("shell", "input", "tap", *self._center(n))
                time.sleep(1.5)
                return n["text"]
        return None

    def window_open(self, ns=None):
        """VML 程序开的绘图窗口在不在。

        判据取绘图页那几个手柄按钮的文字（`DrawWindowPage.xaml` 的 SELECT / START / 收起手柄）——
        它们只在绘图页上出现，命令行页没有。
        """
        for n in (ns if ns is not None else self.nodes()):
            if n["text"] in ("SELECT", "START", "▲ 收起手柄"):
                return True
        return False

    def wait_idle(self, timeout, settle=1.5):
        """等命令行页回到空闲。返回 (是否等到, 是否见过忙碌, 是否开过绘图窗口)。"""
        t0 = time.time()
        time.sleep(settle)
        saw_busy = False
        saw_window = False
        polls = 0
        while time.time() - t0 < timeout:
            ns = self.nodes()
            polls += 1
            if self.accept_host_dialog(ns):
                continue
            if self.window_open(ns):
                # **开窗即达判据，立刻返回**，不要等满超时。
                #
                # 对开窗程序，"绘图页出现"就是结论，再等下去只是白等 60~300 秒
                # （程序的主循环本来就跑到用户关窗为止）。而且这一等会把整轮时间
                # 拖垮 —— 9 个游戏各等 300 秒就是 45 分钟，换不来任何新信息。
                return True, True, True
            b = self.run_button(ns)
            if b is None:
                # 页面不见了（程序开了绘图窗口）—— 记一笔，等它回来
                time.sleep(2)
                continue
            if b["text"] == "…":
                saw_busy = True
            elif saw_busy or polls >= 2:
                # ⚠ **"现在空闲"本身就是"跑完了"的充分证据**，不要再要求"必须先见过忙碌"。
                # 提交那一刻输入框就被清空了（`OnRunRequested` 里 `CmdEntry.Text = ""`
                # 在 `await` 之前），所以空闲 + 输入框已空 = 这条命令已受理并结束。
                # 要求"见过忙碌"会在**两条命令之间刚好没轮到我们采样**时永远等下去
                # （实测：一个跑完的 C 程序，输出明明已经出来了，采集端还在傻等超时）。
                return True, saw_busy, saw_window
            time.sleep(2)
        return False, saw_busy, saw_window

    def abort_running(self):
        """把还在跑的 VML 停掉并**保证下一条能跑** —— 冷启动，不做分级降级。

        原来那版是 BACK → 找「强制停止」→ 冷启动三级降级。实测**不够**：BACK 在
        「首页/对话」页上就是**退出 App**（不是返回上一页），而中止之后下一条报
        "命令未能送进输入框" 的根因正是 App 已经不在前台/不在命令行页了。
        分级降级看着"更省事"，实际是给失败留了三张不同形状的脸。
        冷启动约 20 秒，换的是**下一条一定跑得起来** —— 这个交换在几十条的批量里是赚的。
        """
        self.restart_app()
        return True

    def close_window(self):
        """关掉 VML 程序开的绘图窗口并回到命令行页。

        绘图页上按 BACK 是**安全**的（只在绘图页按），会发一条 WindowClose 消息 +
        调 `ShellPage.CancelRunningVml()`；若命令行页弹了「程序还在运行」确认框就点
        「强制停止」。都不成再冷启动。
        """
        self.sh("shell", "input", "keyevent", "4")
        time.sleep(2)
        for n in self.nodes():
            if n["cls"] == "android.widget.Button" and n["text"] in ("强制停止", "停止"):
                self.sh("shell", "input", "tap", *self._center(n))
                time.sleep(2)
                break
        if not self.goto_shell_tab():
            self.restart_app()

    def run(self, cmd, timeout=300):
        """送命令 → 等结束 → 取**这一次**的输出。返回 dict(ok, saw_busy, output, error)。

        「这一次的输出」用**增量**取，不是清屏：输出区是个只追加的缓冲
        （`ShellPage.Append` → `OutputLabel.Text = join(_lines)`），所以跑完之后
        的新文本 = 跑之前的文本 + 这一次的输出。增量取比"先清屏"少一次往返，
        也不依赖那个布局不稳的「清屏」按钮。
        万一缓冲从头裁过（`MaxScrollbackLines = 256`）增量判据失效，就直接整段返回 ——
        宁可多带上一轮的尾巴，也不要漏掉这一轮的正文。
        """
        self.recover()
        # 提交前先确认空闲：忙 = 上一个程序还活着，此时输入框是禁用的，送什么都白送
        for _ in range(3):
            if self.is_idle():
                break
            self.abort_running()
        pre = self.output_label()
        sent = self.submit(cmd)
        if not sent:
            # 命令送不进去 = 命令行页的状态不干净（无法取证的一类）。**冷启动一次再来** ——
            # 这是确定性恢复，比继续在同一张脏页面上重试有价值：继续重试只会把
            # "送不进去"重复 N 遍，最后记一条"驱动失败"，而那既不是产品的结论、也不是语料的结论。
            # 冷启动会清掉页面（输出缓冲没了），所以 pre 必须重新取，否则增量判据对不上。
            self.restart_app()
            pre = self.output_label()
            sent = self.submit(cmd)
        if not sent:
            return dict(ok=False, saw_busy=False, window=False, output="",
                        error="命令未能送进输入框（冷启动重试后仍失败）")
        done, saw_busy, saw_window = self.wait_idle(timeout)
        if saw_window:
            self.close_window()
        if not done:
            self.abort_running()
        time.sleep(0.5)
        post = self.output_label()
        delta = post[len(pre):] if post.startswith(pre) else post
        return dict(ok=done, saw_busy=saw_busy, window=saw_window, output=delta,
                    error=None if done else "等待超时（命令可能仍在运行）")

    # ── 环境准备 ────────────────────────────────────────────────────────
    def disable_ime(self):
        """关掉软键盘：注入的按键直达应用，时序噪声小很多。失败不致命。"""
        for ime in self.sh("shell", "ime", "list", "-s").split():
            ime = ime.strip()
            if ime:
                self.sh("shell", "ime", "disable", ime)

    def enable_external_storage(self):
        """授「所有文件访问」→ workspace 落到 /sdcard/waycoder/workspace，adb push 可直达。

        没这一步 workspace 在 app 私有目录里，只能靠"在命令行页里 base64 拼文件"那种
        又慢又容易出错的绕法（`input text` 吞字符 + 设备 shell 吃引号，两个坑都在那条路上）。
        """
        self.sh("shell", "appops", "set", self.pkg, "MANAGE_EXTERNAL_STORAGE", "allow")

    def push(self, local, device):
        return self.sh("push", local, device)

    # ── 页面状态快照（给报告用）─────────────────────────────────────────
    def cwd_label(self):
        for n in self.nodes():
            if n["cls"] == "android.widget.TextView" and n["text"].startswith("cwd:"):
                return n["text"]
        return ""
