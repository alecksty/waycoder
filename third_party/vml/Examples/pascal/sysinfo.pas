{ sysinfo.pas: CALLJSON(#573) self-test - call sysinfo and print the JSON result.
  NOTE: Pascal lexer only accepts ASCII inside comments (full-width punctuation
  and em-dashes are rejected as "unknown character"). Keep this block ASCII. }
program p;
begin
  ui_call_json_s('sysinfo', '');
  ui_call_json_print();
end.
