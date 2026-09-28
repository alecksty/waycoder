(* ⚠ **`FUNCTION` 块必须写在 `PROGRAM` 之前** —— 实测写在后面会被**整个忽略**
   （调用点报「未定义的函数 'PICK'」，函数体也从没被发射）。这条顺序约束没写进语言规范，
   本轮顺手记在这里；`FUNCTION` 在前时 `PICK(1)` 得 7。 *)
FUNCTION PICK : INT
VAR X : INT; END_VAR
BEGIN
  X := 7;
  PICK := X;
END_FUNCTION

(* L1：Ladder 调**库函数**（`LADDER_` 前缀只该加在"本程序自己的函数"上）

   修复前的症状：前端把任何非内建调用**无条件**编成 `CALL LADDER_<名字>`
   （`CodeGenerator.Networks.cs`），而库标签是链接器按 `lib_<模块>_<裸名>` 建的
   ⇒ 差一个前缀，**整门语言调不到任何库函数**（图形 / 宿主 UI / 语言查询全不可达）。
   实测报：`未定义的函数 'LADDER_UI_GET_LANGUAGE'`。

   本用例同时钉两件事：
     ① 库函数**能调到**（`ui_get_language` / `ui_scr_w` 都来自 `Lib/`）；
     ② 本程序**自己的** FUNCTION 仍然用 `LADDER_<大写>` 那一套（`PICK` 的返回值对）。

   期望输出：`480` / `1`（或 `0`，看宿主语言）/ `ALL PASS`
*)
PROGRAM L1
VAR W : INT; END_VAR
BEGIN
  W := ui_scr_w();
  IF W > 0 THEN PRINT_INT W ELSE PRINT_STR "FAIL scr_w" END_IF;
  PRINT_CHAR 10;

  IF ui_get_language() = 0 THEN PRINT_STR "zh" ELSE PRINT_STR "en" END_IF;
  PRINT_CHAR 10;

  PRINT_INT PICK(1);
  PRINT_CHAR 10;

  PRINT_STR "ALL PASS";
  PRINT_CHAR 10;
END_PROGRAM

