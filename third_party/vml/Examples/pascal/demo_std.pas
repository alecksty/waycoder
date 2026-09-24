{ demo_std.pas —— Pascal 第一层：**标准输入输出**（std）

  这一层只用 Pascal 自己的 `write` / `writeln`，不碰 Crt / Graph / 宿主 `ui_*` ——
  所以它在任何后端上都是同一份行为，也是四份 demo 里唯一有"逐字节确定性输出"
  判据的一份（另外三份只能验"编得过、跑得完、不挂死"）。

  跑法（桌面 vmlcli）：
    dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/pascal/demo_std.pas

  期望输出（逐字节）：
    === Pascal 标准输出 demo ===
    字符串: 你好，世界
    整数: 42
    计算: 7 * 6 = 42
    整除: 17 div 5 = 3
    取余: 17 mod 5 = 2
    循环求和: 1..10 = 55
    阶乘: 10! = 3628800
    字符宽度: length('abcd') = 4
    === done ===

  ◆ 两条本前端的写法要求（实测出来的，不是风格偏好）

    ① **`+` 不是字符串拼接**。`writeln('a' + 'b')` 打出的是 **195**
       —— 前端把两个字符串的**地址**当整数相加了。最小复现（两行的期望分别是
       "ab" 与 "xyzw"）：
         writeln('a' + 'b');            实际打出 195
         t := 'xy' + 'zw'; writeln(t);  实际打出空行
       所以本文件里**一处字符串拼接都没有**：要拼就把几个值分别写进同一个
       `writeln` 实参表（`writeln('a', b, 'c')` 那条路是好的）。

    ⚠ 写本文件时的一个小坑记在这儿：**Pascal 的花括号注释不嵌套** ——
      本段散文里若出现一个左花括号再出现一个右花括号，**那个右花括号会提前
      收掉整段注释**，后面的散文全被当成代码（实测报「未知字符: ：」）。
      所以下面提到花括号一律用文字说，不写真符号。

    ② `Str(42, s)`（标准 Pascal 的整数转串）**本前端没有** —— 走到「未定义的函数
       'Str'」。要在绘图窗口里显示数字得自己写整转串（本目录的 `demo_ui.pas`
       干脆不显示数字，用形状与固定标签表达状态，与 `sokoban.f90` 同一取舍）。
}

program DemoStd;

var
  i, sum, fact: integer;

begin
  writeln('=== Pascal 标准输出 demo ===');

  { ① 字符串字面量（含中文 —— 源码按 UTF-8 存，词法器直通） }
  writeln('字符串: 你好，世界');

  { ② 整数 }
  writeln('整数: ', 42);

  { ③ 整数运算 }
  writeln('计算: 7 * 6 = ', 7 * 6);

  { ④ 整除与取余：Pascal 用 `div` / `mod`（不是 `\` / `%`） }
  writeln('整除: 17 div 5 = ', 17 div 5);
  writeln('取余: 17 mod 5 = ', 17 mod 5);

  { ⑤ 循环求和 1..10 }
  sum := 0;
  for i := 1 to 10 do
    sum := sum + i;
  writeln('循环求和: 1..10 = ', sum);

  { ⑥ 循环求阶乘 10! }
  fact := 1;
  for i := 1 to 10 do
    fact := fact * i;
  writeln('阶乘: 10! = ', fact);

  { ⑦ 字符串长度（`length` 是语言内建的，走的是 VML 字符串体系） }
  writeln('字符宽度: length(''abcd'') = ', length('abcd'));

  writeln('=== done ===');
end.
