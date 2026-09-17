{ out.pas —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
  写法照 corpus/pas/skel.* —— 共享库同时提供 println_str / println_int。
  ⚠ Pascal 不认 `//` 注释（第一版就写成了 `//`，编译期报「第1行13列未知字符」）}
program p;
begin
  WriteLn('OUT-STR=abc');
  Write('OUT-INT=');
  WriteLn(42);
  WriteLn('OUT-PUN=hello, world');
end.
