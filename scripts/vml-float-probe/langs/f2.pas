{ f2.pas —— float/double/int64 运算 }
procedure println_str(s: PChar);
procedure println_int(v: integer);
var
  a, b: real;
  d1, d2: double;
  big, add, mul, pos: int64;
begin
  a := 3.14; b := 2.0;
  d1 := 3.14; d2 := 2.0;
  big := 3000000000; add := 1000000000;
  mul := 123456789; pos := 4294967296;
  println_str('F-MUL='); println_int(trunc(a * b * 100.0));
  println_str('D-MUL='); println_int(trunc(d1 * d2 * 100.0));
  println_str('F-NEG='); println_int(trunc(-0.5 * 100.0));
  println_str('D-NEG='); println_int(trunc(-0.5 * 100.0));
  println_str('L-ADD='); println_int(trunc((big + add) / 1000000000.0));
  println_str('L-MUL='); println_int(trunc((mul * 1000) / 1000000000.0));
  println_str('L-NEG='); println_int(trunc((0 - pos) / 1000000000.0));
end.
