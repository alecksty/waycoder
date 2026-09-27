{ f.pas —— 浮点：常量、运算、负数、十六进制 }
{ ⚠ 形态与 `nat.nohdr.pas` 一致：**不声明外部过程**（无体的 procedure 声明不是合法
  Pascal，解析器会在末尾报「期望 ';'」，而错因在开头），直接用内建 `WriteLn`。 }
Var
  a, b, h, d: Real;
Begin
  a := 3.14;
  b := 2.0;
  h := $10;
  d := -0.5;
  WriteLn('F-MUL=');
  WriteLn(Trunc(a * b * 100.0));
  WriteLn('F-NEG=');
  WriteLn(Trunc(d * 100.0));
  WriteLn('F-HEX=');
  WriteLn(Trunc(h * 100.0));
End.
