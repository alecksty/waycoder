{ 栈漂移探针（Pascal）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
  反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。

  裸调库函数不写声明（照 corpus/pascal/skel.pas 的写法）。
  WriteLn 支持多实参、实参之间不插分隔符、末尾补一次换行 ⇒ 直接拼出一行。

  ⚠ 审计记「Basic / Pascal 的 builtin 调用仍有『被调方清栈』残留」。ipow 属 math 模块，
     若它也被归进 builtin 那一族，这里会因净漏栈在几轮之后踩花循环变量 —— 正是要量的。 }
program Drift;

var
  i: integer;
  s: integer;

begin
  s := 0;
  for i := 1 to 6 do
  begin
    s := s + ipow(2, i);
  end;
  WriteLn('DRIFT=', s);
end.
