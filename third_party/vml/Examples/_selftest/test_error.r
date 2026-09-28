# test_error.r —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
# test_error.r -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
#
# 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
# Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
# 预期：1 个错误、**0 个警告**（详见下）。
# Expected: 1 error, **0 warnings** (see below).
# 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
# Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
#
# ⚠ r 前端**不检查未声明的变量**（查不到就静默建一个 `var_<name>` 槽并读它）⇒「未定义
# ⚠ The r frontend **does not check undeclared variables** (when it cannot find one it silently creates a `var_<name>` slot and reads it) => the "undefined
#   变量」这条探针在本语言产不出诊断。错误改由**调用未声明的函数**触发：编译期不报，
#   variable" probe cannot produce a diagnostic in this language. The error is instead triggered by **calling an undeclared function**: not reported at compile time,
#   链接器报 `未定义的函数 'func_undefined_func'（引用 1 次）` —— 注意 r 前端给函数名
#   the linker reports `undefined function 'func_undefined_func' (referenced 1 time)` -- note that the r frontend adds a
#   加了 `func_` 前缀，报出的名字就是加过前缀的那个；位置是 `文件:行`（**无列**）。
#   `func_` prefix to function names, so the reported name is the prefixed one; positioned at `file:line` (**no column**).
# ⚠ `unused_var` 是刻意留的「未使用变量」探针；r 前端不报未使用变量
# ⚠ `unused_var` is a deliberately kept "unused variable" probe; the r frontend does not report unused variables
#   （WarnUnused 只有 CCompiler 在调），故本文件只产 1 条错误。
#   (WarnUnused is called only by CCompiler), so this file produces only 1 error.
# ⚠ r 前端确实有 4 处代码生成期报错（如 `break 语句不在循环内`），但它们抛的是
# ⚠ The r frontend does have 4 code-generation-time errors (such as `break statement outside a loop`), but they throw
#   CompilationException，而 CompileWithDiagnostics 对它是**原样重抛** ⇒ 只剩一句
#   CompilationException, and CompileWithDiagnostics **rethrows it as-is** => all that is left is a
#   光秃秃的消息，**没有位置、没有级别词、没有诊断码**，编辑器无法定位。
#   bare message with **no position, no level word, no diag-code**, so the editor cannot locate anything.
# ⚠ 注释**一律独占一行**，不写行尾注释：`#` 是 r 的注释符，而 VML 这一层跑的是
# ⚠ Comments **always occupy a line of their own**; no trailing comments are used: `#` is r's comment marker, while this layer of VML runs the
#   C 风格预处理器 —— 行尾的 `#` 会让它丢掉行号映射，诊断变成 `<input>:6`
#   C-style preprocessor -- a trailing `#` makes it lose the line mapping, and the diagnostic becomes `<input>:6`
#   （文件名丢失 + 行号退化成预处理后的行号，指到别处）。把注释挪到上一行即恢复。
#   (the filename is lost + the line number degrades to the post-preprocessing line number and points elsewhere). Moving the comment to the previous line restores it.
unused_var <- 5
total <- 0
x <- total + 1
total <- x
cat(total)
cat("\n")
undefined_func(1)
