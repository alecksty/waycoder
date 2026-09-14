// VML Shared CLI Library
// 简单命令行解析器 — 嵌入式交互式终端

// cli_match: 匹配命令字符串, 返回 1/0
// 支持缩写: "help" 匹配 "h", "he", "hel", "help"
__stdcall int cli_match(const char* input, const char* command) {
    while (*input && *command) {
        if (*input != *command) return 0;
        input++;
        command++;
    }
    // 如果 input 结束或 command 结束 → 匹配成功 (支持缩写)
    return (*input == 0 || *command == 0) ? 1 : 0;
}

// cli_parse_int: 从字符串解析整数参数, 返回解析的字符数
// 支持十进制和0x十六进制
__stdcall int cli_parse_int(const char* s, int* value) {
    if (s == 0 || value == 0) return 0;
    int result = 0;
    int sign = 1;
    int count = 0;
    while (*s == ' ') { s++; count++; }
    if (*s == '-') { sign = -1; s++; count++; }
    else if (*s == '+') { s++; count++; }
    if (s[0] == '0' && (s[1] == 'x' || s[1] == 'X')) {
        s += 2; count += 2;
        for (;;) {
            char c = *s;
            if (c >= '0' && c <= '9') result = result * 16 + (c - '0');
            else if (c >= 'A' && c <= 'F') result = result * 16 + (c - 'A' + 10);
            else if (c >= 'a' && c <= 'f') result = result * 16 + (c - 'a' + 10);
            else break;
            s++; count++;
        }
    } else {
        while (*s >= '0' && *s <= '9') {
            result = result * 10 + (*s - '0');
            s++; count++;
        }
    }
    *value = sign * result;
    return count;
}

// cli_next_arg: 跳过当前参数, 返回下一个参数的指针
__stdcall const char* cli_next_arg(const char* s) {
    if (s == 0) return 0;
    // 跳过前导空白
    while (*s == ' ') s++;
    // 跳过当前参数 (非空白)
    while (*s && *s != ' ') s++;
    // 跳过空白到下一参数
    while (*s == ' ') s++;
    return (*s == 0) ? 0 : s;
}

// cli_trim_line: 去除行首尾空白和换行符, 原地修改
__stdcall void cli_trim_line(char* line) {
    if (line == 0) return;
    // 跳过前导空白
    char* start = line;
    while (*start == ' ' || *start == '\t') start++;
    // 移动字符串
    if (start != line) {
        char* p = line;
        while (*start) *p++ = *start++;
        *p = 0;
    }
    // 去除尾部换行和空白
    char* end = line;
    while (*end) end++;
    while (end > line && (end[-1] == '\n' || end[-1] == '\r' || end[-1] == ' ')) {
        end--;
        *end = 0;
    }
}

// cli_tokenize: 分割字符串为 argc/argv, 最多 max_args 个
// 返回参数个数, 原地修改字符串 (插入 \0 分隔)
__stdcall int cli_tokenize(char* line, char** argv, int max_args) {
    if (line == 0 || argv == 0) return 0;
    cli_trim_line(line);
    int argc = 0;
    char* p = line;
    while (*p && argc < max_args) {
        // 跳过前导空白
        while (*p == ' ') p++;
        if (*p == 0) break;
        argv[argc++] = p;
        // 找到参数结尾
        while (*p && *p != ' ') p++;
        if (*p == ' ') { *p = 0; p++; }
    }
    return argc;
}
