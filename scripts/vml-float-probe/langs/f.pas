{ f.pas —— 浮点：常量、运算、负数、十六进制 }
procedure println_str(s: PChar);
procedure println_int(v: integer);
var
  a, b, h, d: real;
begin
  a := 3.14;
  b := 2.0;
  h := $10;
  d := -0.5;
  println_str('F-MUL=');
  println_int(trunc(a * b * 100.0));
  println_str('F-NEG=');
  println_int(trunc(d * 100.0));
  println_str('F-HEX=');
  println_int(trunc(h * 100.0));
end.
