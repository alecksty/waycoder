{ nat.nohdr.pas —— 「**省略 program 头**」的判据。

  Turbo Pascal 起，程序头就是**可选**的：源码可以直接从 uses / const / type / var / begin
  开始。写库函数、贴代码片段的老程序几乎都不写那一行 —— `Examples/pascal/` 里
  光 `avc_*.pas` 就有一整批是这个形态（语料里 19 份卡在这一条上）。

  ⚠ 这份探针**故意**没有 `program` 那一行；它旁边的 `nat.legacy.pas` 有。
    两份都在，才说明"有头/没头"两条路都通。
  ⚠ 期望值在 `nat.nohdr.pas.expect` 里 —— 注意这一份**不是**那三行 OUT-* 默认期望。
  ⚠ 注释里不能出现花括号字符本身（词法器一碰到右花括号就结束注释）。 }

Const
  Greeting = 'NOHDR-OK';

Var
  N: Integer;

Begin
  WriteLn(Greeting);
  N := 7;
  WriteLn('N=', N);
End.
