{ skel.pas —— VML 骨架程序（Pascal）。

  习惯用法抄自 Examples/pascal/stm32/f103/stm32f103_spi.pas（program + begin/end.，
  裸调库函数不写声明）；array / for-to / function 见 docs/前端游戏能力评估.md §4.2'
  （patch 0013 修掉 L0 标签与数组地址方向后，Pascal 四关全过、心跳也验过）。
  颜色写负数十进制：本前端没有一处 $ 或 0x 的现成示例，不冒这个险。
  函数名故意不叫 inc —— Pascal 标准过程里就有 Inc()。
  WriteLn 支持多实参、实参之间不插分隔符、末尾补一次换行 ⇒ 直接拼出一行。 }
program Skel;

var
  a: array[0..3] of integer;
  i: integer;
  s: integer;

function plus1(x: integer): integer;
begin
  plus1 := x + 1;
end;

begin
  a[0] := 1;
  a[1] := 2;
  a[2] := 3;
  a[3] := 4;
  s := 0;
  for i := 0 to 3 do
  begin
    a[i] := plus1(a[i]);
    s := s + a[i];
  end;
  ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
  ui_present();
  WriteLn('SKEL-SUM=', s);
end.
