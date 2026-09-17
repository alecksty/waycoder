(* 栈漂移探针（Ladder）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。

   照 corpus/ladder/skel.ld 的既有写法（PRINT_STR / PRINT_INT / PRINT_CHAR，均不补换行）。

   ⚠ 这条**预期就是红的**，而且红的原因不是调用约定 —— corpus/ladder/skel.ld 的文件头已记死两条：
     ① 所有调用点硬加 `LADDER_` 前缀（CodeGenerator.Networks.cs:222/279），而链接器只剥
        func_/word_/method_/var_ 四种（LibraryLinker.cs:162-182），**不含 LADDER_** ⇒ ipow
        永远解析不到 lib_math_ipow；
     ② BEGIN 块里的裸调用语句会被解析器**静默丢弃**（Parser.cs:139-152 只认 `名 := …`），
        连报错都没有。
   写出来是为了把「到底是丢还是报错」变成一条可复现的记录，不是指望它过。
   对「能不能做游戏」而言，Ladder 目前**没有可用的外部调用路径** —— 这是它进 20 种名单的硬阻塞。 *)

PROGRAM drift
VAR
    i : INT;
    s : INT;
END_VAR

BEGIN
    s := 0;
    FOR i := 1 TO 6 DO
        s := s + ipow(2, i);
    END_FOR;
    PRINT_STR "DRIFT=";
    PRINT_INT s;
    PRINT_CHAR 10;
END_PROGRAM
