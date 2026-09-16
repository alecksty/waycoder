// skel.dart —— Dart 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
//
// 取材 Examples/dart/fibonacci/main.dart（`int f(int)` + `void main()`）；
// 前端实现 VMLPrepares/DartCompiler/。
//
// 故意踩的已知缺陷（跑不过=产品缺陷，不是语料写错）：
//  ① Dart 前端**没有下标表达式**：词法器有 LBracket，但解析器没有 postfix `[` 分支
//     （只有 Parser.cs:873 那个「跳过方括号字面量」的容错）⇒ `a[i]` 退化成裸 `a`；
//     `a[i] = x` 的左值也被 Parser.cs:561-564 丢弃 ⇒ 数组读写全废。
//  ② `external` 是 Dart 前端唯一能产出**裸标签** CALL 的形式
//     （CodeGenerator.Expressions.cs:141；asm 已移除）—— ui_rect 只有这条路能解析到
//     lib_vmlui_ui_rect。故此处声明形式与标准 Dart 有别：这是前端提供的 extern 写法。
//  ③ 词法器是十进制专用（Lexer.cs:128），不认 0x ⇒ 颜色写 -65536（= 0xFFFF0000）。
//  ④ print 不加分隔符、不补换行，故显式补 "\n"。
//  ⑤ 前端不分配栈帧（GenerateVarDecl 只写 [R12-N] 不 SUB R13），带嵌套 push 的表达式
//     会踩坏局部变量 —— 这是「Dart 循环/函数挂住」的机制性嫌疑点。

external int ui_rect(int x, int y, int w, int h, int c, int f, int lw, int r);
external int ui_present();

int inc(int x) { return x + 1; }

void main() {
  List<int> a = [1, 2, 3, 4];
  int s = 0;
  for (int i = 0; i <= 3; i = i + 1) {
    a[i] = inc(a[i]);
    s = s + a[i];
  }
  print("SKEL-SUM=", s, "\n");
  ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
  ui_present();
}
