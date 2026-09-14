// VML Shared ReadLine Library
// Character-by-character console input with backspace handling
// Compile: dotnet run --project VMLTool -- Lib/shared/src/readline.c -o Lib/shared/readline.vml

__stdcall int read_line(char* buf, int max_len) {
    int i = 0;
    int c;

    if (max_len <= 1) return 0;

    while (i < max_len - 1) {
        asm("SYSCALL #5");
        asm("MOVE c, R0");

        if (c == '\n') {
            *buf = 0;
            return i;
        } else {
            if (c == '\r') {
                *buf = 0;
                return i;
            } else {
                if (c == 8) {
                    if (i > 0) {
                        i = i - 1;
                        buf = buf - 1;
                    }
                } else {
                    *buf = (char)c;
                    buf = buf + 1;
                    i = i + 1;
                }
            }
        }
    }

    *buf = 0;
    return i;
}
