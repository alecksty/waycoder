{ nat.legacy.pas —— 「**老 Turbo Pascal 写法**」的兼容性判据。

  它不属于 `out.pas` 那条（共享库 println 能不能用），也不是 `nat.pas` 那条（这门语言自己的
  标准输出函数）—— 它压的是**语法/语义层面**的一组写法，每一条都是 `Examples/pascal/`
  那 112 份老程序里真实出现过的形态，而且**每一条都曾经让前端编不过或编错**：

    ① `#$FF`          —— 十六进制字符常量（`g7iles_mario.pas` 一份就用了 2209 处）
    ② `#nn` 紧邻拼接  —— `#65#66` / `'x'#33` 是**同一个**字面量（原前端每段各吐一个 token）
    ③ 字符串里的 `\`  —— Pascal **没有**反斜杠转义，`'d:\turbo\tp\'` 就是这几个字符
    ④ `#nn` 当 case 标签且**写在行首** —— 不能当成 C 的 `#` 预处理指令整行丢掉
    ⑤ `['a'..'z']`    —— 集合字面量的**区间成员** + `in`
    ⑥ `['a','b','c']` —— 集合字面量要**真编出一张位图**，不是把元素值 OR 起来当值用
    ⑦ `set of char` 变量 —— 位图必须是 **8 个 4 字节字**（256 位），不是 1 个
    ⑧ `G[i,j]`        —— 多维下标（含行主序步长与基址）
    ⑨ `Char(n)` / `Integer(c)` —— 类型转换

  ⚠ 期望值在 `nat.legacy.pas.expect` 里；改本文件必须两边一起改。
  ⚠ 注释只能用花括号（Pascal 不认双斜杠）—— 第一版写错注释前缀会把**编译失败**伪装成
     "跑完输出为空"，那是这套探针踩过的最贵的坑。
  ⚠ 上面几行里**不能出现花括号字符本身**：词法器碰到第一个右花括号就结束注释，
     正文会变成代码、报一句指向别处的「未知字符」。（本文件第一次就是这么挂的。） }
Var
  Ch: Char;
  N, I, J: Integer;
  G: Array[0..3,0..3] of Integer;
  S: Set of Char;
Begin
  { ① + ② }
  WriteLn('A1=', #$41, #66, 'C', 'D'#69);
  { ③ }
  WriteLn('A2=', 'd:\turbo\tp\');
  { ④ }
  Ch := #75;
  Case Ch Of
    #75: WriteLn('A3=K');
    #80: WriteLn('A3=P');
  End;
  { ⑤ }
  Ch := 'q';
  If Ch In ['a'..'z'] Then WriteLn('A4=lower') Else WriteLn('A4=other');
  Ch := 'Q';
  If Ch In ['a'..'z'] Then WriteLn('A4=lower') Else WriteLn('A4=other');
  { ⑥ }
  Ch := 'b';
  If Ch In ['a','b','c'] Then WriteLn('A5=in') Else WriteLn('A5=out');
  Ch := 'z';
  If Ch In ['a','b','c'] Then WriteLn('A5=in') Else WriteLn('A5=out');
  { ⑦ }
  S := ['a'..'c'];
  Ch := 'c';
  If Ch In S Then WriteLn('A6=in') Else WriteLn('A6=out');
  Ch := 'd';
  If Ch In S Then WriteLn('A6=in') Else WriteLn('A6=out');
  { ⑧ }
  For I := 0 To 3 Do
    For J := 0 To 3 Do
      G[I,J] := I * 10 + J;
  WriteLn('A7=', G[0,0], ',', G[1,2], ',', G[3,3]);
  { ⑨ }
  N := 66;
  WriteLn('A8=', Char(N), Integer('Z'));
End.
