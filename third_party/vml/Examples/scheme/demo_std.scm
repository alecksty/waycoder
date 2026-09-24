; demo_std.scm —— **第 1 层：标准输入输出**（Scheme 的 display / newline）
;
; 这一层就是语言自己的标准输出：往 stdout 写文本。
; 也是最基础的一层，也是**唯一有逐字节确定性判据**的一层 —— 下面的输出
; 跑两遍完全一样（不读输入、不用随机数、不看时间）。
;
; ## 判据
;
;     vmlcli Examples/scheme/demo_std.scm
;
; 期望 stdout 逐字节等于本文件末尾「期望输出」那段。
;
; ## ⚠ 本前端实测的五条限制（写 demo 时避开）
;
;   · `display` / `print` **只取第一个实参、且不补换行** ⇒ 一行要用
;     「若干次 `(display x)` + 一次 `(newline)`」拼出来。
;   · **字符串字面量里没有任何转义** —— `SchemeCompiler/Lexer.cs` 的 `ReadString`
;     是「见到 `\` 就把反斜杠跳过、把下一个字符原样收下」⇒ `"\n"` 得到的是
;     字母 `n`，`"\t"` 得到 `t`。所以本 demo 不写任何转义，
;     要换行一律用 `(newline)`。
;   · **`remainder` / `quotient` 在库里没有对应标签**（链接期报「未找到标签」）⇒
;     取模只好手算 `a - (a / b) * b`（本前端 `/` 就是整数除）。
;   · `int->string` / `string->int` 这类名字**在本构建里链不到**（实测
;     「未定义的函数 'int->string'」）⇒ 数字靠 `(display n)` 直接打。
;   · **用户函数里不能调库函数**（尾调用优化会跳过被调者的序言）⇒
;     本前端的所有调用都写在**顶层**，循环用 `do`（`let` 只在语句位受理）。
;
; 顶层的 `define` 都放在前面、`set!` 在顶层执行 —— 这条也照 catch.scm 的形态来。

(display "=== demo_std (Scheme) ===")
(newline)
(display "纯字符串一行")
(newline)

(define a 17)
(define b 25)

(display "a=")
(display a)
(display " b=")
(display b)
(newline)

(display "a+b=")
(display (+ a b))
(display " a-b=")
(display (- a b))
(display " a*b=")
(display (* a b))
(newline)

(display "a/b=")
(display (/ a b))
(display " a%b=")
(display (- a (* (/ a b) b)))
(newline)

(display "负数： ")
(display (- 0 a))
(display " ")
(display (- 0 (* a b)))
(newline)

; 循环算一个结果，证明这一层和语言本身是通的
(define total 0)
(do ((i 1 (+ i 1))) ((> i 10) 0)
    (set! total (+ total (* i i))))
(display "1^2+...+10^2 = ")
(display total)
(newline)

; 九九表的一小段（多行）
(do ((i 1 (+ i 1))) ((> i 5) 0)
    (display i)
    (display " x 7 = ")
    (display (* i 7))
    (newline))

(display "=== 完成 ===")
(newline)

; ── 期望输出（逐字节）────────────────────────────────────────────
; === demo_std (Scheme) ===
; 纯字符串一行
; a=17 b=25
; a+b=42 a-b=-8 a*b=425
; a/b=0 a%b=17
; 负数： -17 -425
; 1^2+...+10^2 = 385
; 1 x 7 = 7
; 2 x 7 = 14
; 3 x 7 = 21
; 4 x 7 = 28
; 5 x 7 = 35
; === 完成 ===
; ────────────────────────────────────────────────────────────────
