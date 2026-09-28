{ out.pas —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
  out.pas -- the VML cross-language "output" probe (exactly three lines, see run-langs.sh)
  写法照 corpus/pas/skel.* —— 共享库同时提供 println_str / println_int。
  Written like corpus/pas/skel.* -- the shared library provides both println_str and println_int.
  ⚠ Pascal 不认 `//` 注释（第一版就写成了 `//`，编译期报「第1行13列未知字符」）
  ⚠ Pascal does not accept `//` comments (the first version wrote `//`, and compilation reported
  "unknown character at line 1, column 13"). }
program p;
begin
  WriteLn('OUT-STR=abc');
  Write('OUT-INT=');
  WriteLn(42);
  WriteLn('OUT-PUN=hello, world');
end.
