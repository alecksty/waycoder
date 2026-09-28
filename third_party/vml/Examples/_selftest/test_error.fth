\ test_error.fth —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
\ test_error.fth -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
\
\ 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
\ Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
\ 预期：1 个错误、**0 个警告**（forth 前端没有任何警告通道）。
\ Expected: 1 error, **0 warnings** (the forth frontend has no warning channel at all).
\ 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
\ Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
\
\ `undefined_word` 不在任何内置词表里，也没有 `: undefined_word ... ;` 定义，
\ `undefined_word` is not in any builtin word list, and there is no `: undefined_word ... ;` definition,
\ 前端会按惯例生成 `CALL word_undefined_word`，留到链接期报
\ the frontend will idiomatically generate `CALL word_undefined_word`, leaving it for the linker to report
\ [CodeGen_UndefinedFunction]。⚠ 这条消息由链接器手拼，**没有列号、也没有 [诊断码] 后缀**。
\ [CodeGen_UndefinedFunction]. ⚠ This message is hand-assembled by the linker and has **no column and no [diag-code] suffix**.
." forth diag probe" CR
undefined_word
." unreachable" CR
