# test_error.py —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
# test_error.py -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
#
# 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
# Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
# 预期：1 个错误、**0 个警告**（详见下）。
# Expected: 1 error, **0 warnings** (see below).
# 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
# Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
#
# ⚠ python 前端**不检查未声明的变量**（查不到局部/全局表就静默发一个 0）⇒「未定义
# ⚠ The python frontend **does not check undeclared variables** (when the local/global table has no entry it silently emits a 0) => the "undefined
#   变量」这条探针在本语言产不出诊断。错误改由**调用未声明的函数**触发：编译期不报，
#   variable" probe cannot produce a diagnostic in this language. The error is instead triggered by **calling an undeclared function**: not reported at compile time,
#   链接器报 `未定义的函数 'undefined_func'（引用 1 次）`，位置是 `文件:行`（**无列**）。
#   the linker reports `undefined function 'undefined_func' (referenced 1 time)`, positioned at `file:line` (**no column**).
# ⚠ `unused_var` 是刻意留的「未使用变量」探针；python 前端不报未使用变量
# ⚠ `unused_var` is a deliberately kept "unused variable" probe; the python frontend does not report unused variables
#   （WarnUnused 只有 CCompiler 在调），故本文件只产 1 条错误。MCU 那两条
#   (WarnUnused is called only by CCompiler), so this file produces only 1 error. The two MCU
#   「yield/await 被忽略」警告走 WarningEmitter、被 WarningLevel 门控（默认 0）
#   "yield/await ignored" warnings go through WarningEmitter, gated by WarningLevel (default 0)
#   ⇒ vmlcli 与手机端都不开启，实测全静默。
#   => neither vmlcli nor the mobile client enables it, and measurement shows they are entirely silent.
# ⚠ 注释**一律独占一行**，不写行尾注释：`#` 是 python 注释符，而 VML 这一层跑的是
# ⚠ Comments **always occupy a line of their own**; no trailing comments are used: `#` is python's comment marker, while this layer of VML runs the
#   C 风格预处理器 —— 行尾的 `#` 会让它丢掉行号映射，诊断变成 `<input>:6`
#   C-style preprocessor -- a trailing `#` makes it lose the line mapping, and the diagnostic becomes `<input>:6`
#   （文件名丢失 + 行号退化成预处理后的行号，指到别处）。把注释挪到上一行即恢复。
#   (the filename is lost + the line number degrades to the post-preprocessing line number and points elsewhere). Moving the comment to the previous line restores it.
unused_var = 5
total = 0
x = total + 1
total = x
print(total)
undefined_func(1)
