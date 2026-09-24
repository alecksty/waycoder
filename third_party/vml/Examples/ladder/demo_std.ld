(* demo_std.ld —— Ladder 第一层：**标准输入输出**（std）

   Ladder（梯形图）是 IEC 61131-3 的**工业控制**语言 —— PLC 那一套。
   它的"输出"不是控制台图形，而是四条**裸打印语句**：

       PRINT_STR   "字符串"
       PRINT_INT   整数表达式
       PRINT_FLOAT 浮点表达式
       PRINT_CHAR  字符码

   这四条是前端**专门认的**（`Parser.IsBarePrintStatement`），
   别的函数名一律会被编成 `CALL LADDER_<名字>` 而**链不上任何东西** ——
   这一点决定了 Ladder 在四层里只能到"标准输出"与"ANSI 彩色"两层，
   图形（BGI）与宿主 UI 两层**整层不可达**。理由见本目录的 `demo_tty.ld`。

   跑法（桌面 vmlcli）：
     dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/ladder/demo_std.ld

   期望输出（逐字节）：
     === Ladder 标准输出 demo ===
     字符串: 你好，世界
     整数: 42
     计算: 7 * 6 = 42
     A - B * 2 = 7
     整除: 17 / 5 = 3
     实数: 2.5
     === done ===

   ◆ 三条本前端的写法要求（实测）

     ① 程序骨架是 `PROGRAM <名> / VAR … END_VAR / BEGIN … END_PROGRAM`，
        每条语句以 `;` 结尾，注释用 `(* … *)`。
     ② 变量**必须**在 `VAR` 段声明（`R : INT;`）—— 直接写 `R := 1` 会报
        「未声明的变量 'R'」。这点与 BASIC/Pascal 不同，别下意识省掉。
     ③ ⚠ **标签（`LBL_x:`）会吃掉它后面的语句**，所以本层里**写不出循环**。
        实测最小复现（`Examples/ladder/` 下现写的，两行之差）：
          S := 7; PRINT_INT S;              ⇒ 打出 7        ✅
          S := 7; LBL_A: PRINT_INT S;       ⇒ 什么都不打    ❌
       同批还有一条：`IF <条件> THEN … END_IF` 的**块体不执行** ——
        条件写成 `S = 1`（S:=1）或 `B`（B:=TRUE）都试过，块内两条语句
        一条都没跑，块后的语句照常跑。⇒ 本文件全部是**直线代码**。

   ◆ ⚠ **两条算术缺陷**（本文件刻意绕开，都是实测）
     ① **`MOD` 只取右操作数**（不是取模）：
          17 MOD 5   ⇒ 5      （应 2）
           5 MOD 17  ⇒ 17     （应 5）
     ② **浮点的二元运算只取右操作数**：
          R := 3.0 * 100.0; PRINT_FLOAT R   ⇒ 100
          R := 100.0 * 3.0; PRINT_FLOAT R   ⇒ 3
     两条的形状一样（**右操作数活下来、左操作数与运算符一起消失**），
     而**整数的 `+ - * /` 都是对的**（`7 * 6` = 42、`17 / 5` = 3、`17 - 5 * 2` = 7）。
     ⇒ 所以本文件里：取模一律换成整除、浮点只打印**字面量**。
     （顺带一提，`PRINT_FLOAT 2.5` 打出来是 `2.5` —— Ladder 是本批五门语言里
     唯一能把浮点打对的一门：BASIC 的 PRINT 浮点按整数截断、Fortran 的 print
     打出的是浮点的**位模式**。）
*)

PROGRAM DemoStd
VAR
  A : INT;
  B : INT;
  C : INT;
END_VAR
BEGIN
  PRINT_STR "=== Ladder 标准输出 demo ===";
  PRINT_CHAR 10;

  (* ① 字符串字面量（含中文 —— 源码按 UTF-8 存，词法器直通） *)
  PRINT_STR "字符串: 你好，世界";
  PRINT_CHAR 10;

  (* ② 整数 *)
  PRINT_STR "整数: ";
  PRINT_INT 42;
  PRINT_CHAR 10;

  (* ③ 常量算术（表达式直接进打印语句）*)
  PRINT_STR "计算: 7 * 6 = ";
  PRINT_INT 7 * 6;
  PRINT_CHAR 10;

  (* ④ 变量算术 —— 先在 VAR 段声明，再赋值，再打印。
        顺带把优先级也验一下：A - B * 2 = 17 - 10 = 7 *)
  A := 17;
  B := 5;
  C := A - B * 2;
  PRINT_STR "A - B * 2 = ";
  PRINT_INT C;
  PRINT_CHAR 10;

  (* ⑤ 整除（`/` 在整数上是整除；`MOD` 是坏的，见文件头，这里不用）*)
  PRINT_STR "整除: 17 / 5 = ";
  PRINT_INT 17 / 5;
  PRINT_CHAR 10;

  (* ⑥ 浮点：只打**字面量**（浮点的二元运算也是坏的，见文件头）*)
  PRINT_STR "实数: ";
  PRINT_FLOAT 2.5;
  PRINT_CHAR 10;

  PRINT_STR "=== done ===";
  PRINT_CHAR 10;
END_PROGRAM
