(* L2：**块体内**的裸打印语句（缺陷②）

   修复前的症状：`ParseStStatement()` 不认 `IsPrintStatement()`（**只有顶层认**）
   ⇒ `PRINT_STR "…";` 走标识符分支、读完 `PRINT_STR` 撞不上 `:=`，
   一路落到末尾的「跳过无法识别的语句」——**整条语句静默消失**（不报错、不生成代码）。
   实测 `IF 1 = 1 THEN PRINT_STR "在块里"; END_IF; PRINT_STR "块外面";`
   只打印「块外面」。

   ⚠ 同一条账里记着的老话「`IF … END_IF` 的块体不执行」**是错的**：
   **赋值一直是跑的**（`S := 42` 之后打 S 得 42），丢的只有**裸打印这一类**。
   本用例把三类块体一次占满：裸打印 / 赋值 / 嵌套 IF。

   期望输出：`in-block` `42` `nested` `outside` `ALL PASS`
*)
PROGRAM L2
VAR S : INT; END_VAR
BEGIN
  IF 1 = 1 THEN PRINT_STR "in-block" END_IF;
  PRINT_CHAR 10;

  IF 1 = 1 THEN S := 42 END_IF;
  PRINT_INT S;
  PRINT_CHAR 10;

  IF 1 = 1 THEN IF 2 = 2 THEN PRINT_STR "nested" END_IF END_IF;
  PRINT_CHAR 10;

  PRINT_STR "outside";
  PRINT_CHAR 10;
  PRINT_STR "ALL PASS";
  PRINT_CHAR 10;
END_PROGRAM
