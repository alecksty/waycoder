// out.pas —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/pas/skel.* —— 共享库同时提供 println_str / println_int。
program p;
begin
  WriteLn('OUT-STR=abc');
  Write('OUT-INT=');
  WriteLn(42);
  WriteLn('OUT-PUN=hello, world');
end.
