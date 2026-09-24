// demo_std.js —— **第 1 层：标准输入输出**（JavaScript 的 console.log）
//
// 这一层就是语言自己的标准输出：往 stdout 写文本。
// 也是最基础的一层，也是**唯一有逐字节确定性判据**的一层 —— 下面的输出
// 跑两遍完全一样（不读输入、不用随机数、不看时间）。
//
// ## 判据
//
//     vmlcli Examples/javascript/demo_std.js
//
// 期望 stdout 逐字节等于本文件末尾「期望输出」那段。
//
// ## ⚠ 本前端实测的三条限制（写 demo 时避开）
//
//   · **字符串 + 数字编出来的结果是错的** —— `console.log("a=" + a)` 打出
//     `2109`（地址量级的值），`console.log("line " + i)` 打出 `1059/1060/1061`。
//     所以本 demo **一个 `"..." + n` 都不用**，靠 `console.log` 的**多实参**
//     （`console.log("a=", a)` → `a=17`，数字实参本身是对的）。
//   · `console.log` **不插分隔符**：`console.log("line", i)` 打出 `line0`。
//   · **没有 `String(n)` / `n.toString()`**（两个都报「未定义的函数」），
//     而 `int_to_str(n)` 恒返回 `0`（实测）⇒ 数字转字符串这条路是断的。
//     彩色控制台那一层因此改用**字面量光标坐标**，见 demo_tty.js。
//
// ## 本前端必须显式声明外部函数（沿用 `Examples/javascript/catch.js`）
//
// 对不认识的函数名，本前端会先找 `func_<名>`、都没有就**把名字当变量**、
// 编成「MOVE R1, var_<名>；CALL R0」⇒ 运行期跳野地址。
// `console.log` 是前端内建的，不用声明。

const A = 17;
const B = 25;

console.log("=== demo_std (JavaScript) ===");
console.log("纯字符串一行");
console.log("转义：制表\t反斜杠\\引号\"");

console.log("a=", A, " b=", B);
console.log("a+b=", A + B, " a-b=", A - B, " a*b=", A * B);
console.log("a/b=", A / B, " a%b=", A % B);
console.log("负数： ", 0 - A, " ", 0 - (A * B));

// 循环算一个结果，证明这一层和语言本身是通的
let i = 1;
let total = 0;
while (i <= 10) {
    total = total + i * i;
    i = i + 1;
}
console.log("1^2+...+10^2 = ", total);

// 九九表的一小段（多行）
i = 1;
while (i <= 5) {
    console.log(i, " x 7 = ", i * 7);
    i = i + 1;
}

console.log("=== 完成 ===");

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (JavaScript) ===
// 纯字符串一行
// 转义：制表	反斜杠\引号"
// a=17 b=25
// a+b=42 a-b=-8 a*b=425
// a/b=0 a%b=17
// 负数： -17 -425
// 1^2+...+10^2 = 385
// 1 x 7 = 7
// 2 x 7 = 14
// 3 x 7 = 21
// 4 x 7 = 28
// 5 x 7 = 35
// === 完成 ===
// ────────────────────────────────────────────────────────────────
