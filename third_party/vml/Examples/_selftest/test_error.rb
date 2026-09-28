# test_error.rb —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
# test_error.rb -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
#
# 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
# Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
# 预期：1 个错误、**0 个警告**（详见下）。
# Expected: 1 error, **0 warnings** (see below).
# 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
# Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
#
# ⚠ ruby 前端**不检查未声明的变量** —— 符号表查不到就静默建一个 `var_<name>` 槽并读它，
# ⚠ The ruby frontend **does not check undeclared variables** -- when the symbol table has no entry it silently creates a `var_<name>` slot and reads it,
#   所以「未定义变量」这条探针在本语言里产不出任何诊断。本文件的错误因此改由
#   so the "undefined variable" probe produces no diagnostic at all in this language. This file's error is therefore instead triggered
#   **调用未声明的函数**触发：编译期不报，靠链接器报
#   by **calling an undeclared function**: not reported at compile time, it relies on the linker reporting
#   `未定义的函数 'func_undefined_func'（引用 1 次）` —— 注意 ruby 前端给函数名加了
#   `undefined function 'func_undefined_func' (referenced 1 time)` -- note that the ruby frontend adds a
#   `func_` 前缀，报出的名字就是加过前缀的那个；位置是 `文件:行`（**无列**）。
#   `func_` prefix to function names, so the reported name is the prefixed one; positioned at `file:line` (**no column**).
# ⚠ `unused_var` 是刻意留的「未使用变量」探针。ruby 前端不报未使用变量
# ⚠ `unused_var` is a deliberately kept "unused variable" probe. The ruby frontend does not report unused variables
#   （WarnUnused 只有 CCompiler 在调），因此本文件只产 1 条错误。
#   (WarnUnused is called only by CCompiler), so this file produces only 1 error.
# ⚠ 本文件的注释**一律独占一行**，不写行尾注释：`#` 是 ruby 的注释符，而 VML 在这一层
# ⚠ This file's comments **always occupy a line of their own**; no trailing comments are used: `#` is ruby's comment marker, while VML at this layer
#   跑的是 C 风格预处理器 —— 行尾出现的 `#` 会让它丢掉行号映射，诊断于是变成
#   runs the C-style preprocessor -- a `#` appearing at the end of a line makes it lose the line mapping, and the diagnostic becomes
#   `<input>:6`（文件名丢失 + 行号退化成预处理后的行号，指到别处）。
#   `<input>:6` (the filename is lost + the line number degrades to the post-preprocessing line number and points elsewhere).
#
# 下面这一行就是本文件唯一的错误（链接期报出）：
# The line below is this file's one and only error (reported by the linker):
unused_var = 5
total = 0
x = total + 1
total = x
puts(total)
undefined_func(1)
