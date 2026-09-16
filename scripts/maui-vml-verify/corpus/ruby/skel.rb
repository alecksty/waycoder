# skel.rb —— VML 骨架程序（Ruby）。
#
# 习惯用法抄自 Examples/ruby/stringutil/main.rb（def ... end）与 Examples/ruby/file_io.rb
# （顶层直接写语句）。
#
# ⚠ file_io.rb 里的 asm("CALL shared_file_test") 其实**不存在**这个东西：RubyCompiler 没有
#   asm 节点，它被当成普通方法调用，编成 CALL func_asm。所以这里不照抄 asm。
# ⚠ 本前端把**不认识的函数名**编成 CALL func_<名>（CodeGenerator.Expressions.cs:189,193），
#   而实参是**从左到右**压栈（同文件 :169），与库包装读 [R12+12] 的约定相反。
#   AST 里也没有下标表达式（没有 IndexExpr 节点）⇒ a[i] 既读不到也写不进。
#   这三条都照最自然的写法写、不做规避 —— 它们正是要测的东西。
# 打印：print("...") 走 print_str（不换行）；puts(s) 走 print_int + 换行（CompilerBase/
#   CodeGeneratorBase.cs:594-599）。两者合起来正好是 "SKEL-SUM=14" 一行。
def plus1(x)
  return x + 1
end

a = [1, 2, 3, 4]
s = 0
for i in 0..3
  a[i] = plus1(a[i])
  s = s + a[i]
end

ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
ui_present()
print("SKEL-SUM=")
puts(s)
