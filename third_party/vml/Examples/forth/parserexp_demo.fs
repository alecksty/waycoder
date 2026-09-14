\ parserexp_demo.fs — 调用共享库 parserexp 解析表达式
: DEMO
    S" 2+3*4" parserexp . CR
    S" (10-3)*2" parserexp . CR
    S" 100/4+5" parserexp . CR ;
DEMO
