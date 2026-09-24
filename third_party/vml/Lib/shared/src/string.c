#param lib("crosslang")

// VML Shared String Library
//
// ⚠ `crosslang` 是给**各语言的 string shim** 用的，不是 string.c 自己调用：
//   `Lib/<lang>/string.vml` 里 `CALL str_len` / `CALL str_cmp` 写的是**裸名**，
//   真身在 `crosslang.c`；而用户程序自己从不直接调用它们 ⇒ 自动链接（按 FuncMap 路由）
//   不会把 crosslang 拉进来 ⇒ 链接期报 `error: 未定义的函数 'str_len'`，
//   这门语言的**每个**程序都编不过（与程序内容无关）。
//   同 `math.c` 顶部的 `math64`/`statistics` 那两条，理由与判据一字相同。

__stdcall int strlen(const char* s) {
    int n = 0;
    while (s[n]) n++;
    return n;
}

__stdcall int strcmp(const char* a, const char* b) {
    while (*a && *a == *b) { a++; b++; }
    // 显式转 int 避免 MOVEB 截断 (C 编译器 char 运算类型提升待完善)
    int ca = (int)(signed char)*a;
    int cb = (int)(signed char)*b;
    return ca - cb;
}

/* ── 忽略大小写的比较（cmatrix 一个程序就引用了 16 次）──
 * ⚠ 它本该在 `<strings.h>` 里，但那个头本仓根本没有，而老程序**常常
 * 直接就用**（cmatrix 只在 AUTHORS 里提过 strings.h）—— 所以实现放这里、
 * 声明也进 `string.h`，让它"顺手就有"。
 *
 * ⚠ 自己内联 ASCII 小写转换而**不调 `tolower`**：那会引入对 ctype 模块的
 * 依赖，而 string 是"默认最小集"里最底层的一个（见 `Lib/c/string.h` 头部
 * "默认最小集已包含 string+memory"），不该反过来依赖别人。
 * 只处理 ASCII —— 与 C 标准里这两个函数"行为依赖 locale"的规定一致
 * （本平台只有 C locale）。
 *
 * 返回值沿用 `strcmp` 的符号约定（负/零/正），**不要求恰好差 1**。 */
static int _vml_lower_ascii(int c) {
    if (c >= 'A' && c <= 'Z') return c + 32;
    return c;
}

__stdcall int strcasecmp(const char* a, const char* b) {
    while (*a && _vml_lower_ascii((int)(signed char)*a) == _vml_lower_ascii((int)(signed char)*b)) {
        a++; b++;
    }
    return _vml_lower_ascii((int)(signed char)*a) - _vml_lower_ascii((int)(signed char)*b);
}

__stdcall int strncasecmp(const char* a, const char* b, int n) {
    while (n > 0 && *a &&
           _vml_lower_ascii((int)(signed char)*a) == _vml_lower_ascii((int)(signed char)*b)) {
        a++; b++; n--;
    }
    if (n == 0) return 0;   /* 前 n 个字符全等 */
    return _vml_lower_ascii((int)(signed char)*a) - _vml_lower_ascii((int)(signed char)*b);
}

char* strcpy(char* dst, const char* src) {
    char* p = dst;
    while (*src) { *p = *src; p++; src++; }
    *p = 0;
    return dst;
}

char* strcat(char* dst, const char* src) {
    char* p = dst;
    while (*p) p++;
    while (*src) { *p = *src; p++; src++; }
    *p = 0;
    return dst;
}

__stdcall int strncmp(const char* a, const char* b, int n) {
    for (int i = 0; i < n; i++) {
        if (a[i] != b[i]) {
            int ca = (int)(signed char)a[i];
            int cb = (int)(signed char)b[i];
            return ca - cb;
        }
        if (a[i] == 0) return 0;
    }
    return 0;
}

const char* strchr(const char* s, int c) {
    while (*s) {
        if (*s == (char)c) return s;
        s++;
    }
    return 0;
}

char* strncpy(char* dst, const char* src, int n) {
    char* p = dst;
    int i = 0;
    while (i < n && src[i] != 0) {
        p[i] = src[i];
        i++;
    }
    for (; i < n; i++) p[i] = 0;
    return dst;
}

const char* strstr(const char* haystack, const char* needle) {
    if (*needle == 0) return haystack;
    while (*haystack) {
        const char* h = haystack;
        const char* n = needle;
        while (*h && *n && *h == *n) { h++; n++; }
        if (*n == 0) return haystack;
        haystack++;
    }
    return 0;
}

// Reverse string: writes reversed src into dst, returns length
__stdcall int strrev(char* dst, const char* src) {
    int len = strlen(src);
    for (int i = 0; i < len; i++)
        dst[i] = src[len - 1 - i];
    dst[len] = 0;
    return len;
}

// Convert string to uppercase (in-place or to dst)
__stdcall void str_toupper(char* dst, const char* src) {
    while (*src) {
        char c = *src++;
        if (c >= 'a' && c <= 'z') c -= 32;
        *dst++ = c;
    }
    *dst = 0;
}

// Convert string to lowercase (in-place or to dst)
__stdcall void str_tolower(char* dst, const char* src) {
    while (*src) {
        char c = *src++;
        if (c >= 'A' && c <= 'Z') c += 32;
        *dst++ = c;
    }
    *dst = 0;
}

// Repeat string n times into dst, returns total length written
__stdcall int str_repeat(char* dst, const char* src, int n) {
    int len = strlen(src);
    int total = 0;
    for (int i = 0; i < n; i++) {
        for (int j = 0; j < len; j++)
            dst[total++] = src[j];
    }
    dst[total] = 0;
    return total;
}

// Check if string s contains substring sub
__stdcall int str_contains(const char* s, const char* sub) {
    return strstr(s, sub) != 0 ? 1 : 0;
}

// Trim leading and trailing whitespace (space, tab, CR, LF)
__stdcall char* str_trim(char* dst, const char* src) {
    // Skip leading whitespace
    while (*src == ' ' || *src == '\t' || *src == '\r' || *src == '\n') src++;
    // Copy to dst
    char* start = dst;
    char* last_non_ws = dst;
    while (*src) {
        *dst = *src;
        if (*src != ' ' && *src != '\t' && *src != '\r' && *src != '\n')
            last_non_ws = dst;
        dst++; src++;
    }
    *(last_non_ws + 1) = '\0';
    return start;
}

// Extract substring: dst gets src[pos..pos+count-1]
__stdcall char* str_substr(char* dst, const char* src, int pos, int count) {
    int len = strlen(src);
    if (pos < 0) pos = 0;
    if (pos >= len) { *dst = '\0'; return dst; }
    if (pos + count > len) count = len - pos;
    char* start = dst;
    src += pos;
    while (count-- > 0 && *src) *dst++ = *src++;
    *dst = '\0';
    return start;
}

// Find first occurrence of char c in string s, return index or -1
__stdcall int str_indexof(const char* s, int c) {
    int i = 0;
    while (s[i]) {
        if (s[i] == c) return i;
        i++;
    }
    return -1;
}

// Split string by delimiter, return number of parts (max 32)
// Allocates new memory for each part; parts[] receives pointers
__stdcall int str_split(const char* s, int delim, char** parts, int maxParts) {
    int count = 0;
    const char* start = s;
    const char* p = s;
    while (*p && count < maxParts) {
        if (*p == delim) {
            int len = p - start;
            if (len > 0 || count > 0) { // include empty parts except leading
                parts[count] = (char*)vml_alloc(len + 1);
                strncpy(parts[count], start, len);
                parts[count][len] = '\0';
                count++;
            }
            start = p + 1;
        }
        p++;
    }
    // Last part
    if (*start && count < maxParts) {
        int len = strlen(start);
        parts[count] = (char*)vml_alloc(len + 1);
        strcpy(parts[count], start);
        count++;
    }
    return count;
}

// Pad string on left side
__stdcall char* str_padstart(char* dst, const char* src, int totalLen, int padChar) {
    int len = strlen(src);
    if (len >= totalLen) { strcpy(dst, src); return dst; }
    int padCount = totalLen - len;
    char* start = dst;
    while (padCount-- > 0) *dst++ = (char)padChar;
    strcpy(dst, src);
    return start;
}

// Pad string on right side
__stdcall char* str_padend(char* dst, const char* src, int totalLen, int padChar) {
    int len = strlen(src);
    strcpy(dst, src);
    if (len >= totalLen) return dst;
    dst += len;
    int padCount = totalLen - len;
    while (padCount-- > 0) *dst++ = (char)padChar;
    *dst = '\0';
    return dst - len; // return original start
}

// charAt(s, index): 返回字符串指定位置的字符 (0-based), 越界返回0
__stdcall int str_charat(const char* s, int index) {
    int i = 0;
    while (s[i] && i < index) i++;
    return s[i];  // returns 0 if index out of bounds
}

// startsWith(str, prefix): 检查 str 是否以 prefix 开头, 返回 1/0
__stdcall int str_startswith(const char* str, const char* prefix) {
    while (*prefix) {
        if (*str != *prefix) return 0;
        str++; prefix++;
    }
    return 1;
}

// endsWith(str, suffix): 检查 str 是否以 suffix 结尾, 返回 1/0
__stdcall int str_endswith(const char* str, const char* suffix) {
    int str_len = strlen(str);
    int suf_len = strlen(suffix);
    if (suf_len > str_len) return 0;
    return strcmp(str + str_len - suf_len, suffix) == 0 ? 1 : 0;
}
