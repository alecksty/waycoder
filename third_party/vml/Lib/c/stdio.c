/* Working printf with %d %i %u %x %X %o %p %c %s %% - no stdarg */
int puts(const char *s);
int sprintf(char *str, const char *fmt, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) {
    int pos = 0; int count = 0;
    char *p = (char*)fmt;
    int args[8];
    args[0]=a1; args[1]=a2; args[2]=a3; args[3]=a4;
    args[4]=a5; args[5]=a6; args[6]=a7; args[7]=a8;
    int ai = 0;

    while (*p) {
        if (*p == '%') {
            p++;
            if (*p == 0) break;
            if (*p == '%') { str[pos]='%'; pos++; count++; p++; continue; }
            int val = 0;
            if (ai < 8) { val = args[ai]; ai++; }
            if (*p == 'd' || *p == 'i') {
                int n = val, cnt = 0, i = 0;
                char tmp[12];
                if (n < 0) { str[pos]='-'; pos++; cnt++; n = -n; }
                if (n == 0) { str[pos]='0'; pos++; count+=cnt+1; p++; continue; }
                while (n > 0) { tmp[i] = (char)('0' + (n % 10)); i++; n = n / 10; }
                cnt += i; count += cnt;
                while (i > 0) { i--; str[pos] = tmp[i]; pos++; }
            } else if (*p == 'u') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[12];
                if (n == 0) { str[pos]='0'; pos++; count++; p++; continue; }
                while (n > 0) { tmp[i] = (char)('0' + (n % 10)); i++; n = n / 10; }
                count += i;
                while (i > 0) { i--; str[pos] = tmp[i]; pos++; }
            } else if (*p == 'x') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[10];
                if (n == 0) { str[pos]='0'; pos++; count++; p++; continue; }
                while (n > 0) { int d = n & 0xF;
                    if (d < 10) tmp[i] = (char)('0' + d);
                    else tmp[i] = (char)('a' + d - 10);
                    i++; n = n >> 4; }
                count += i;
                while (i > 0) { i--; str[pos] = tmp[i]; pos++; }
            } else if (*p == 'X') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[10];
                if (n == 0) { str[pos]='0'; pos++; count++; p++; continue; }
                while (n > 0) { int d = n & 0xF;
                    if (d < 10) tmp[i] = (char)('0' + d);
                    else tmp[i] = (char)('A' + d - 10);
                    i++; n = n >> 4; }
                count += i;
                while (i > 0) { i--; str[pos] = tmp[i]; pos++; }
            } else if (*p == 'o') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[12];
                if (n == 0) { str[pos]='0'; pos++; count++; p++; continue; }
                while (n > 0) { tmp[i] = (char)('0' + (n & 7)); i++; n = n >> 3; }
                count += i;
                while (i > 0) { i--; str[pos] = tmp[i]; pos++; }
            } else if (*p == 'p') {
                char *px = "0x";
                while (*px) { str[pos] = *px; pos++; count++; px++; }
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[10];
                if (n == 0) { str[pos]='0'; pos++; count++; }
                else {
                    while (n > 0) { int d = n & 0xF;
                        if (d < 10) tmp[i] = (char)('0' + d);
                        else tmp[i] = (char)('a' + d - 10);
                        i++; n = n >> 4; }
                    count += i;
                    while (i > 0) { i--; str[pos] = tmp[i]; pos++; }
                }
            } else if (*p == 'c') {
                str[pos] = (char)val; pos++; count++;
            } else if (*p == 's') {
                if (val != 0) { char *s = (char*)val; while (*s) { str[pos] = *s; pos++; count++; s++; } }
                else { char *ns = "(null)"; while (*ns) { str[pos] = *ns; pos++; count++; ns++; } }
            } else {
                str[pos] = '%'; pos++; str[pos] = *p; pos++; count += 2;
            }
            p++;
        } else {
            str[pos] = *p; pos++; count++; p++;
        }
    }
    str[pos] = 0;
    return count;
}

int printf(const char *fmt, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) {
    char buf[512];
    int ret = snprintf(buf, 512, fmt, a1, a2, a3, a4, a5, a6, a7, a8);
    puts(buf);
    return ret;
}

int snprintf(char *str, int size, const char *fmt, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) {
    int pos = 0; int count = 0;
    char *p = (char*)fmt;
    int args[8];
    args[0]=a1; args[1]=a2; args[2]=a3; args[3]=a4;
    args[4]=a5; args[5]=a6; args[6]=a7; args[7]=a8;
    int ai = 0;

    while (*p) {
        if (*p == '%') {
            p++;
            if (*p == 0) break;
            if (*p == '%') { if(pos<size-1){str[pos]='%';} pos++; count++; p++; continue; }
            int val = 0;
            if (ai < 8) { val = args[ai]; ai++; }
            if (*p == 'd' || *p == 'i') {
                int n = val, cnt = 0, i = 0;
                char tmp[12];
                if (n < 0) { if(pos<size-1){str[pos]='-';} pos++; cnt++; n = -n; }
                if (n == 0) { if(pos<size-1){str[pos]='0';} pos++; count+=cnt+1; p++; continue; }
                while (n > 0) { tmp[i] = (char)('0' + (n % 10)); i++; n = n / 10; }
                cnt += i; count += cnt;
                while (i > 0) { i--; if(pos<size-1){str[pos] = tmp[i];} pos++; }
            } else if (*p == 'u') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[12];
                if (n == 0) { if(pos<size-1){str[pos]='0';} pos++; count++; p++; continue; }
                while (n > 0) { tmp[i] = (char)('0' + (n % 10)); i++; n = n / 10; }
                count += i;
                while (i > 0) { i--; if(pos<size-1){str[pos] = tmp[i];} pos++; }
            } else if (*p == 'x') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[10];
                if (n == 0) { if(pos<size-1){str[pos]='0';} pos++; count++; p++; continue; }
                while (n > 0) { int d = n & 0xF;
                    if (d < 10) tmp[i] = (char)('0' + d);
                    else tmp[i] = (char)('a' + d - 10);
                    i++; n = n >> 4; }
                count += i;
                while (i > 0) { i--; if(pos<size-1){str[pos] = tmp[i];} pos++; }
            } else if (*p == 'X') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[10];
                if (n == 0) { if(pos<size-1){str[pos]='0';} pos++; count++; p++; continue; }
                while (n > 0) { int d = n & 0xF;
                    if (d < 10) tmp[i] = (char)('0' + d);
                    else tmp[i] = (char)('A' + d - 10);
                    i++; n = n >> 4; }
                count += i;
                while (i > 0) { i--; if(pos<size-1){str[pos] = tmp[i];} pos++; }
            } else if (*p == 'o') {
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[12];
                if (n == 0) { if(pos<size-1){str[pos]='0';} pos++; count++; p++; continue; }
                while (n > 0) { tmp[i] = (char)('0' + (n & 7)); i++; n = n >> 3; }
                count += i;
                while (i > 0) { i--; if(pos<size-1){str[pos] = tmp[i];} pos++; }
            } else if (*p == 'p') {
                char *px = "0x";
                while (*px) { if(pos<size-1){str[pos] = *px;} pos++; count++; px++; }
                unsigned int n = (unsigned int)val, i = 0;
                char tmp[10];
                if (n == 0) { if(pos<size-1){str[pos]='0';} pos++; count++; }
                else {
                    while (n > 0) { int d = n & 0xF;
                        if (d < 10) tmp[i] = (char)('0' + d);
                        else tmp[i] = (char)('a' + d - 10);
                        i++; n = n >> 4; }
                    count += i;
                    while (i > 0) { i--; if(pos<size-1){str[pos] = tmp[i];} pos++; }
                }
            } else if (*p == 'c') {
                if(pos<size-1){str[pos] = (char)val;} pos++; count++;
            } else if (*p == 's') {
                if (val != 0) { char *s = (char*)val; while (*s) { if(pos<size-1){str[pos] = *s;} pos++; count++; s++; } }
                else { char *ns = "(null)"; while (*ns) { if(pos<size-1){str[pos] = *ns;} pos++; count++; ns++; } }
            } else {
                if(pos<size-1){str[pos] = '%';} pos++; if(pos<size-1){str[pos] = *p;} pos++; count += 2;
            }
            p++;
        } else {
            if(pos<size-1){str[pos] = *p;} pos++; count++; p++;
        }
    }
    if (pos < size) str[pos] = 0;
    else if (size > 0) str[size-1] = 0;
    return count;
}

int sscanf(const char *str, const char *fmt, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) {
    int *args[8];
    args[0]=(int*)a1; args[1]=(int*)a2; args[2]=(int*)a3; args[3]=(int*)a4;
    args[4]=(int*)a5; args[5]=(int*)a6; args[6]=(int*)a7; args[7]=(int*)a8;
    int ai = 0; int count = 0;
    const char *s = str;
    const char *f = fmt;

    while (*f) {
        if (*f == '%') {
            f++;
            if (*f == 0) break;
            if (*f == '%') { f++; s++; continue; }

            while (*s == ' ' || *s == '\t' || *s == '\n') s++;

            if (*f == 'd') {
                int sign = 1; int val = 0;
                if (*s == '-') { sign = -1; s++; }
                else if (*s == '+') s++;
                if (*s < '0' || *s > '9') break;
                while (*s >= '0' && *s <= '9') {
                    val = val * 10 + (*s - '0'); s++;
                }
                if (ai < 8 && args[ai]) { *args[ai] = sign * val; count++; }
                ai++;
            } else if (*f == 'x' || *f == 'X') {
                int val = 0; int digits = 0;
                int hex_loop = 1;
                while (hex_loop) {
                    if (*s >= '0' && *s <= '9') { val = (val << 4) | (*s - '0'); s++; digits++; }
                    else if (*s >= 'a' && *s <= 'f') { val = (val << 4) | (*s - 'a' + 10); s++; digits++; }
                    else if (*s >= 'A' && *s <= 'F') { val = (val << 4) | (*s - 'A' + 10); s++; digits++; }
                    else { hex_loop = 0; }
                }
                if (digits > 0 && ai < 8 && args[ai]) { *args[ai] = val; count++; }
                ai++;
            } else {
                break;
            }
            f++;
        } else if (*f == ' ' || *f == '\t') {
            while (*s == ' ' || *s == '\t') s++;
            f++;
        } else {
            if (*f == *s) { f++; s++; }
            else break;
        }
    }
    return count;
}
