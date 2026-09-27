{ f2.pas —— float/double/int64 运算 }
Var
  a, b: Real;
  d1, d2: Double;
  big, add, pos: Int64;
  den: Double;
Begin
  a := 3.14; b := 2.0;
  d1 := 3.14; d2 := 2.0;
  big := 3000000000; add := 1000000000;
  pos := 4294967296;
  WriteLn('F-MUL=');  WriteLn(Trunc(a * b * 100.0));
  WriteLn('D-MUL=');  WriteLn(Trunc(d1 * d2 * 100.0));
  WriteLn('F-NEG=');  WriteLn(Trunc(-0.5 * 100.0));
  WriteLn('D-NEG=');  WriteLn(Trunc(-0.5 * 100.0));
  { ⚠ 除数用 **Double 变量**而不是实数字面量：本前端的实数字面量是 `Real`（32 位），
    `Int64 / Real` 按单精度算，5e9 这个量级一舍入结果就不对了（实测 0）。
    写成 `den := 1e9` 走的是 `Int64 → Double`（**L2D**）+ 双精度除 ——
    这也正是"在 Pascal 里表达我要双精度"的正常写法。 }
  den := 1000000000.0;
  WriteLn('L-ADD=');  WriteLn(Trunc((big + add) / den));
  WriteLn('L-MUL=');  WriteLn(Trunc(((5000000000 / 5) * 2) / den));
  WriteLn('L-NEG=');  WriteLn(Trunc((0 - pos) / den));
End.
