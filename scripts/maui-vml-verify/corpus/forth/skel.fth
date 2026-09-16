\ skel.fs —— Forth 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
\
\ 取材 Examples/forth/parserexp_demo.fs（`: DEMO … ;` + 顶层顺序执行/末尾自动调用）
\ 与共享库的 `."` / `.` / `CR` 输出词；前端实现 VMLPrepares/ForthCompiler/。
\
\ ⚠ 扩展名：Forth 前端只认 `.fth` / `.forth`（ForthCompilerPlugin.cs:15），
\   而本骨架按调用方约定命名成 `.fs` —— **跑之前先改名成 skel.fth**，
\   否则宿主 GetCompilerByFileName 会直接报「认不出这个扩展名」。
\
\ 故意踩的已知缺陷（跑不过=产品缺陷，不是语料写错）：
\  ① `@` 是**空操作**：CodeGenerator.Operations.cs:562-570 弹出地址后发的是 `MOVE R0, R0`
\     （自赋值）⇒ 读到的是地址本身而不是元素值；`C@` 同病。`!`（:545-560）方向是对的，
\     所以「写」能进、「读」出不来 —— 数组读是这里第一个死点。
\  ② `ui_rect` 会被编成 `CALL word_ui_rect`，靠链接器剥 `word_` 前缀解析到
\     lib_vmlui_ui_rect（LibraryLinker.cs:162-182）；但调用点会多压一个保存 R15 的槽
\     （CodeGenerator.Words.cs:310-330），第 2 个往后的实参整体错位一格。
\  ③ `$FFFF0000` 在 Words.cs:17 会 Int32 溢出 ⇒ 颜色写 -65536。
\  ④ Forth 实参要**逆序**压栈（最后压的 = C 的第 1 个参数）。

: inc 1+ ;

CREATE a 16 ALLOT
  1 a      !
  2 a  4 + !
  3 a  8 + !
  4 a 12 + !

0                      \ s：累加器留在数据栈上，循环里始终平衡
4 0 DO
  a I 4 * +            \ a[i] 的地址
  DUP @ inc            \ 读出来 +1（读受 ① 影响）
  SWAP !               \ 写回
  a I 4 * + @          \ 再读一次（同样受 ① 影响）
  +                    \ s = s + a[i]
LOOP

." SKEL-SUM=" . CR
ui_rect 10 10 50 50 -65536 1 0 0
ui_present
