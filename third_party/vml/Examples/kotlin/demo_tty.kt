// demo_tty.kt —— **第 2 层：彩色命令行**（`tty_*`）
//
// ⚠ **新程序用 `tty_*`，老程序才用 `conio` / `Crt`**（用户定的分层标准：
//   「新版程序，文字模式用 tty 库，图像模式用 ui 库；旧版老程序才用 conio、graphics 等库」）。
//   `tty_*` 只有字符、没有任何图形功能；要画图用 BGI 或 `ui_*`。
//   坐标 **1 起算**、颜色 **0–15 索引色**。
//
// ## ⚠⚠ 本份**当前跑不起来** —— 链不上 `tty.vml`（两件事，都在这份例程之外）
//
//   ① **`Libs=` 里没有 `tty.vml`** —— 这一条**仍然成立**，但只解释得了
//      **Java / C# / Swift** 那三份（它们要显式 `--lib Lib/shared/tty.vml` 才跑）。
//      ⚠ **Kotlin 这两份不是这个原因** —— 见下。
//
//   ② **`--lib` 与 `Libs=` 对Kotlin（本前端）根本不生效** —— 真因**不是**「拿不到 `--lib`」：
//      `--lib` 的文件与语言 `Libs=` 的文件进的是同一个 `libraryPaths`
//      （`scripts/vmlcli/Program.cs`），而 `CompilerHelper.CompileFileStandard` 那次链接
//      **确实**带着这份清单（`LinkStandardLibrary(prog, langName, allLibPaths)`）。
//      真正卡住的是**更早的一步**：
//      `KotlinCompiler.cs:35` 在 `Compile` 里先自己调了一次
//      `CompilerHelper.LinkStandardLibrary(prog, "kotlin", null)` —— **库清单是 null**。
//      那一次只认 `builtins.vml`（自动）+ `SharedPrefixMap` 自动探测到的模块
//      （`ui_` → vmlui，所以 `demo_ui` 能跑），于是 `tty_*` / `initgraph` 当场被判成
//      「未定义的函数」**硬错误抛出**，后面那次「带着真库清单」的链接**根本没机会跑**。
//      ⚠ **别用 C 风格 `external` 声明去绕**（`external int tty_init(int,int,int);`）：
//        Kotlin 解析器只认 `external` 这个关键字本身（`Parser.cs:32` 置 `_pendingExternal`），
//        声明剩下的 token 被 `else Advance()` 逐个丢弃、**标志留着**，于是**紧跟着的第一个
//        `fun`** 被当成外部函数编译（`CodeGenerator.cs:105` 的 `if (fn.IsExternal) continue;`
//        ⇒ 不产标签、丢函数体）。症状是该 `fun` 报「未定义的函数」；而**若被吃掉的正好是
//        `main`，程序会编译通过、运行「成功」、然后什么都不做**（静默空跑）。
//        最小复现：`external int tty_init(int w,int h,int c);` + `fun foo(): Int { return 42 }`
//        + `fun main(){ print(foo()) }` ⇒ `error: 未定义的函数 'foo'`。
//      ⇒ 对照：`JavaCompiler.cs:37` / `CSharpCompiler.cs:38` **不做**这次预链接
//        （注释写着「LinkStandardLibrary 由调用方统一处理」），所以同一份 `--lib` 给它们就有效。
//      最小复现（2026-09-24，两步都跑过）：
//
//          // K2.java（同形，对照用）
//          class K2 { static native int tty_init(int w,int h,int c);
//                     public static void main(String[] a){ tty_init(80,25,1); } }
//          $ vmlcli Examples/java/K2.java
//            → ✔ 编译完成（java，59338 条指令）      // 不需要 --lib
//
//      ⚠ 与「`LIB.vml` 有没有挂上」**无关**：`tty.vml` 自提交 9b5e1aa7 起已在 22 门语言的
//        `Libs=` 里（本条初版写成「只有 vmlui.vml」，已过期）。挂上了也一样编不过 ——
//        卡点在前面那次预链接上，**不动共享代码修不了**。
// **修法（都动共享代码，本份例程没有代改）**：把 `tty.vml` 加进 `vmltool.config.xml`
//   里 kotlin / dart 那两行 `Libs=`；或者照 Pascal 的 `AutoLinkUnit`（`PascalCompiler.cs:358`）
//   给这两个前端也做一层"源码里用了就自动链"。
//
// ## 本文件的状态：**写法已验证、运行未验证**
//
//   · 版面与调用串和 `Examples/kotlin/demo_ui.kt`（已跑通）同一套，
//     与 `Examples/java/demo_tty.java`（已跑通出画面）**逐句同构** ⇒ 库侧用法是对的。
//   · Kotlin 这边能编到**只剩 `tty_*` 未定义**为止（没有别的语法/调用错误）。
//   ⚠ 也就是说：**它现在编不过，但它不是空壳** —— 配置补齐后应当直接可用。
//     在那之前请不要把它当成"已验证能跑"的例程。
//
// ## ⚠ UTF-8 的字节预算
//
// `conio.c` 的 `putch()` 把**字节**当**列**计数（到 80 折行），一个汉字/框线字符 3 字节
// ⇒ 某段连续输出跨过第 80 列时折行落在字符中间，宿主按字节重组 UTF-8 的校验随即失败
// 并**粘性**切成单字节老编码 ⇒ 之后整份输出全乱。版面遵守「起始列 + 3×字符数 ≤ 80」。
// 细节与最小复现见 `Examples/c/demo_tty.c` 文件头。

// 在第 (x,y) 处用指定前景/背景打一串
fun say(x: Int, y: Int, fg: Int, bg: Int, s: String) {
    tty_print_at(x, y, s, fg, bg)
}

fun main() {
    var i = 0

    // ── 1. 初始化 + 清屏 ──
    tty_init(0, 0, 0)
    tty_cls()

    // ── 2. 标题栏：亮黄字 + 蓝底，铺满第 1 行 ──
    tty_color(14, 1)
    tty_goto(1, 1)
    tty_puts("                                                                                ")
    tty_goto(3, 1)
    tty_puts("WayCoder  tty_* demo (Kotlin)  --  color / cursor / box / int")

    tty_color(8, 0)
    tty_goto(3, 2)
    tty_puts("tty.h: tty_init / tty_cls / tty_color / tty_goto / tty_box / tty_put_int")

    // ── 3. ASCII 面板（61 列，安全）──
    tty_color(7, 0)
    tty_box(2, 3, 62, 12, 0)               // style 0 = ASCII(`+ - |`)

    // 面板里的内容：每行一种前景色
    say(4, 4,  7,  0, "color  7  lightgray    普通正文")
    say(4, 5,  11, 0, "color 11  lightcyan    次要信息")
    say(4, 6,  10, 0, "color 10  lightgreen   ok / 成功")
    say(4, 7,  14, 0, "color 14  yellow       状态栏高亮")
    say(4, 8,  12, 0, "color 12  lightred     错误 / 警告")
    say(4, 9,  13, 0, "color 13  lightmagenta 强调")

    // 反白一行（"选中项"的长相）：黑字白底
    say(4, 11, 0, 7, "  > Open     (reversed: fg=0 bg=7)                              ")

    // ── 4. UTF-8 单线框（19 格宽 × 3 字节 = 57，从第 4 列起 ⇒ 61 字节，安全）──
    tty_color(11, 0)
    tty_box(4, 14, 22, 19, 1)              // style 1 = ┌ ─ │ ┐ └ ┘ 单线框

    say(6, 16, 14, 0, "中文也")
    say(6, 17, 10, 0, "没问题")

    // ── 5. 右侧说明（ASCII，列 26 起）──
    say(26, 14, 11, 0, "tty_box(..., style=1)")
    say(26, 15, 7,  0, "  = UTF-8 single-line box")
    say(26, 17, 11, 0, "tty_box(..., style=0)")
    say(26, 18, 7,  0, "  = ASCII box (used above)")

    // ── 6. 16 色色带 ──
    i = 0
    while (i < 16) {
        tty_color(0, i)
        tty_goto(3 + i * 4, 21)
        tty_puts("    ")
        i = i + 1
    }
    tty_color(7, 0)
    tty_goto(3, 22)
    tty_puts("index 0..15: black blue green cyan red magenta brown gray / +8 = bright")

    // ── 7. 数字与光标 ──
    tty_color(11, 0)
    tty_goto(3, 24)
    tty_puts("tty_put_int: ")
    tty_color(14, 0)
    tty_put_int(2026)
    tty_color(11, 0)
    tty_puts(" / ")
    tty_color(14, 0)
    tty_put_int(-42)
    tty_color(11, 0)
    tty_puts("   cursor=")
    tty_put_int(tty_wherex())
    tty_puts(",")
    tty_put_int(tty_wherey())
    tty_puts("   screen=")
    tty_put_int(tty_width())
    tty_puts("x")
    tty_put_int(tty_height())

    // ── 8. 状态栏 + 收尾 ──
    tty_color(0, 3)
    tty_goto(1, 24)
    tty_puts(" tty_* demo done -- no key wait, exits by itself                              ")
    tty_color(8, 0)
    tty_goto(3, 25)
    tty_puts("demo_tty (Kotlin) 结束 —— 画完即退出")

    tty_color(7, 0)
}
