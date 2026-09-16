(* skel.ld —— Ladder 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
   取材 Examples/ladder/motor_control/main.ld（BEGIN / END_PROGRAM）与
        Examples/ladder/file_io.ld（(* … *) 注释）；前端实现 VMLPrepares/LadderCompiler/。
   本文件语法可信度**较低**：Ladder 的 ST 部分我只在源码里核对过，没有可抄的例子。

   故意踩的已知缺陷（跑不过=产品缺陷，不是语料写错）：
    ① **外部调用基本不可能**：所有调用点都硬加前缀（CodeGenerator.Networks.cs:222/279：
       名字大写再拼 `LADDER_`），而链接器只剥 func_/word_/method_/var_ 四种前缀
       （LibraryLinker.cs:162-182），不含 LADDER_ ⇒ `ui_rect` 永远解析不到
       lib_vmlui_ui_rect。而且 BEGIN 块里的裸调用语句会被解析器**静默丢弃**
       （Parser.cs:139-152 的 Identifier 分支走 ParseStStatement，只认 `名 := …`），
       连报错都没有 —— 这里就按最自然的形式写出来，看它到底是丢还是报错。
    ② 数组下标**只能是常量**：ExpressionToStorageName 只接受字面量（Networks.cs:53-70），
       变量下标读会回退成 `MOVE R0, #0`、写会凭空造一个新变量（a_i）⇒ 循环里的 `a[i]`
       根本不成立，这是第二个死点。
    ③ FOR i := 0 TO 3 DO … END_FOR 本身是好的（Networks.cs:414-450，含上界）。
    ④ 输出只有裸语句 PRINT_STR / PRINT_INT / PRINT_CHAR（Networks.cs:245-262），
       且**不补换行** ⇒ 换行要自己 PRINT_CHAR 10。
    ⑤ 颜色写 -65536（= 0xFFFF0000）。 *)

FUNCTION inc : INT
VAR_INPUT
    x : INT;
END_VAR
    inc := x + 1;
END_FUNCTION

PROGRAM skel
VAR
    a : ARRAY[0..3] OF INT := [1,2,3,4];
    i : INT;
    s : INT;
END_VAR

BEGIN
    s := 0;
    FOR i := 0 TO 3 DO
        a[i] := inc(a[i]);
        s := s + a[i];
    END_FOR;
    PRINT_STR "SKEL-SUM=";
    PRINT_INT s;
    PRINT_CHAR 10;
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
    ui_present();
END_PROGRAM
