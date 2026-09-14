\ VML 全类型转换库 — Forth 包装器 (v1.66.44)
\ 用法: include conv.fth

: int_to_str ( n -- c-addr )   \ 整数 → 字符串地址
    int_to_str ;
: str_to_int ( c-addr -- n )   \ 字符串 → 整数
    str_to_int ;
: float_to_str ( f -- c-addr ) \ 浮点 → 字符串
    float_to_str ;
: str_to_float ( c-addr -- f ) \ 字符串 → 浮点
    str_to_float ;
: double_to_str ( d -- c-addr ) \ 双精度 → 字符串
    double_to_str ;
: str_to_double ( c-addr -- d ) \ 字符串 → 双精度
    str_to_double ;
: long_to_str ( l -- c-addr )  \ 64位 → 字符串
    long_to_str ;
: str_to_long ( c-addr -- l )  \ 字符串 → 64位
    str_to_long ;
: bool_to_str ( flag -- c-addr ) \ 布尔 → 字符串
    bool_to_str ;
: str_to_bool ( c-addr -- flag ) \ 字符串 → 布尔
    str_to_bool ;
: char_to_str ( c -- c-addr )  \ 字符 → 字符串
    char_to_str ;
: str_to_char ( c-addr -- c )  \ 字符串 → 字符
    str_to_char ;
