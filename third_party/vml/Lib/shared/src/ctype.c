#param lib("parserexpf")

// VML Shared CType Library
// 编译: vmltool compile ctype.c -o ../ctype.vml --lang c

__stdcall int is_alpha(int c) {
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}

__stdcall int is_digit(int c) {
    return c >= '0' && c <= '9';
}

__stdcall int isalnum(int c) {
    return is_alpha(c) || is_digit(c);
}

__stdcall int isspace(int c) {
    return c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f' || c == '\v';
}

__stdcall int isupper(int c) {
    return c >= 'A' && c <= 'Z';
}

__stdcall int islower(int c) {
    return c >= 'a' && c <= 'z';
}

__stdcall int to_upper(int c) {
    if (c >= 'a' && c <= 'z') return c - 32;
    return c;
}

__stdcall int to_lower(int c) {
    if (c >= 'A' && c <= 'Z') return c + 32;
    return c;
}

__stdcall int isxdigit(int c) {
    return is_digit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}

__stdcall int ispunct(int c) {
    return (c >= '!' && c <= '/') || (c >= ':' && c <= '@') ||
           (c >= '[' && c <= '`') || (c >= '{' && c <= '~');
}

__stdcall int isprint(int c) {
    return c >= 32 && c <= 126;
}

__stdcall int iscntrl(int c) {
    return (c >= 0 && c <= 31) || c == 127;
}

// ============================================
// 共享库只导出  前缀名称
// C 标准名 (isalnum/isspace/...) → Lib/c/stdlib_shared.vml
// 各语言 idiomatic 名 → Lib/<lang>/stdlib_complete.vml
// ============================================
