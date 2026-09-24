' 被 `52-include.bas` 用 `'$INCLUDE:` 拉进来的 BASIC 头文件（`.bi`）。
' 第一件判据：这里的 CONST 必须对包含者可见。
CONST BI_FROM_HEADER = 1234
' 第二件判据：**嵌套包含**，且相对路径以**本文件所在目录**为基准
' （包含者与它同目录 ⇒ `'52-include-nested.bi'` 这一行必须解析得到）。
'$INCLUDE: '52-include-nested.bi'
