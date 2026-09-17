// 栈漂移探针（Dart）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
// 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
//
// `external` 是 Dart 前端唯一能产出**裸标签** CALL 的形式（CodeGenerator.Expressions.cs:141），
// 照 corpus/dart/skel.dart 的既有写法。print 不加分隔符、不补换行 ⇒ 显式补 "\n"。
//
// ⚠ corpus/dart/skel.dart 记「前端不分配栈帧（GenerateVarDecl 只写 [R12-N] 不 SUB R13），
//    带嵌套 push 的表达式会踩坏局部变量」—— 循环里调库恰好是那条路的典型受害者，
//    这条探针就是冲它去的（若成立，累加器或循环游标会被踩花 ⇒ 不等于 126）。
external int ipow(int base, int exp);

void main() {
  int s = 0;
  for (int i = 1; i <= 6; i = i + 1) {
    s = s + ipow(2, i);
  }
  print("DRIFT=", s, "\n");
}
