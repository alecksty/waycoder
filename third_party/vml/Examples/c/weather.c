/* weather.c —— 联网抓一周天气预报 → 写进工作区里的 weather.txt → 输出状态。
 * weather.c — fetch a one-week forecast over the network, write it to weather.txt in the workspace, then report status.
 *
 * 三段：① 裸套接字取回 api.open-meteo.com 的预报 JSON（明文 HTTP，80 端口 ——
 * Three stages: (1) fetch the forecast JSON from api.open-meteo.com over a raw socket (plain HTTP on port 80 —
 * VM 里没有 TLS，所以不能走 https）；② 剥掉响应头，把正文写进 weather.txt
 * there is no TLS in the VM, so https is impossible); (2) strip the response headers and write the body into weather.txt
 * （**相对路径**：手机上沙箱根 = 工作区，绝对路径会被拒）；③ 把 HTTP 码 / 收发字节数
 * (**relative path**: on the phone the sandbox root is the workspace, absolute paths are rejected); (3) print the HTTP code and the bytes sent/received
 * 打出来。天气源免密钥，一次请求返回正好 7 天。
 * The weather source needs no key and one request returns exactly 7 days.
 *
 * 跑法（桌面脚手架，秒级，别为改一行去打 APK）：
 * How to run (desktop scaffolding, seconds — do not build an APK just to change one line):
 *     VML_HOME=<含 Lib/ 的 vml 根> \
 *     VML_HOME=<a vml root containing Lib/> \
 *       dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/c/weather.c --timeout 60
 * 手机上：命令行页敲 `vml run examples/c/weather.c`，文件落在工作区根的 weather.txt。
 * On the phone: type `vml run examples/c/weather.c` in the command-line page; the file lands in weather.txt at the workspace root.
 *
 * ⚠ 三个已实测的坑，改动本文件前先读：
 * ⚠ Three pitfalls already confirmed by testing — read these before changing this file:
 *
 * ① **域名不由宿主解析**。`#334 SocketConnect(fd, ip, port)` 的实现是
 * ① **The host does not resolve domain names**. `#334 SocketConnect(fd, ip, port)` is implemented as
 *    `IPAddress.Parse(ip)`（见 VMLRuntime/VMLRuntime.Syscall.OS.cs），喂域名会直接抛异常
 *    `IPAddress.Parse(ip)` (see VMLRuntime/VMLRuntime.Syscall.OS.cs); feeding it a domain name throws outright
 *    返回 -1。所以必须先 `#338 DnsResolve(host)` 把域名换成 IP 字符串。
 *    and returns -1. So the host must first be turned into an IP string with `#338 DnsResolve(host)`.
 *
 * ② **不要用 Lib/shared/src/network.c 与 file.c 的包装函数**。它们写的是
 * ② **Do not use the wrappers in Lib/shared/src/network.c and file.c**. They are written as
 *        int r; asm("SYSCALL #113"); return r;
 *    而 C 前端对**带 `#` 的 SYSCALL** 会额外补一条 `move [R12-4], R0`，把结果塞进一个
 *    but the C front end adds an extra `move [R12-4], R0` for a **SYSCALL carrying a `#`**, storing the result in a
 *    **硬编码槽位**；函数的局部量却在 R12-8 —— 两者不是一个格子。于是这些函数的
 *    **hardcoded slot**, while the function's locals live at R12-8 — not the same cell. So those functions
 *    返回值是**未初始化的栈内容**（实测：真写了 3 字节的 fwrite 返回 0；真发了 170 字节的
 *    return **uninitialized stack contents** (measured: an fwrite that really wrote 3 bytes returned 0; a net_send that really sent 170 bytes
 *    net_send 返回 0；net_recv 恒返回 0 会让读循环当场退出）。**副作用是真的，只有返回值是坏的**。
 *    returned 0; net_recv always returns 0, which makes a read loop exit immediately). **The side effects are real, only the return values are broken**.
 *    本文件因此自己包一层 `sys_*`，照 Lib/shared/src/vmlsys.c 的写法：`SYSCALL` 之后
 *    This file therefore wraps its own `sys_*` layer, following Lib/shared/src/vmlsys.c: after `SYSCALL`,
 *    显式 `MOVE [_sysret], R0` —— SYSCALL 之后 R0 没人动过，搬回来就是返回值。
 *    explicitly `MOVE [_sysret], R0` — nothing touches R0 after SYSCALL, so moving it back is the return value.
 *
 * ③ **函数名别叫 `f1` / `f2` / `d0` / `l3` 这类「一个字母 + 纯数字」**。
 * ③ **Do not name functions `f1` / `f2` / `d0` / `l3` — a single letter plus digits**.
 *    `VMLAssembler.ParseOperand` 认寄存器用的是**大小写不敏感**的前缀
 *    `VMLAssembler.ParseOperand` recognizes registers by a **case-insensitive** prefix
 *    （`StartsWith("F", OrdinalIgnoreCase)` 等，R/F/D/L 四个字母都是），于是
 *    (`StartsWith("F", OrdinalIgnoreCase)` and the like — all four letters R/F/D/L count), so
 *    **小写 `f2` 被当成浮点寄存器 2**，`call f2` 在链接期被改写成 `call R2` ——
 *    **lowercase `f2` is taken as floating-point register 2**, and `call f2` is rewritten to `call R2` at link time —
 *    跳到一个从未赋值的寄存器。避开「这四个字母 + 后缀全是数字」就没事
 *    jumping to a register that was never assigned. Avoid "one of those four letters plus an all-digit suffix" and you are fine
 *    （`find_body`、`foo2`、`g1` 都正常）。
 *    (`find_body`, `foo2` and `g1` are all fine).
 */

#include <waycoder_ui.h>

#define HOST    "api.open-meteo.com"
#define PORT    80
#define OUTFILE "weather.txt"

#define BUFCAP    8192   /* 响应缓冲：响应实测 ~685 字节（头 ~120 + 正文 ~563），8K 绰绰有余 */
                         /* Response buffer: the response measures ~685 bytes (headers ~120 + body ~563), so 8K is more than enough. */

/* 读循环上限：防「对端不关连接」时死读。真到了这一步说明服务端没按 Connection: close
 * Read-loop cap: prevents an endless read when the peer never closes the connection. Reaching it means the server did not honour Connection: close
 * 收场；VM 自身的超时是最后一道闸。⚠ 注释不能跨行写在 #define 那一行里 —— 预处理器
 * to finish up; the VM's own timeout is the last gate. ⚠ A comment must not span lines inside a #define line — the preprocessor
 * 按行吃掉宏体，注释的第二行会掉出来当代码。
 * eats the macro body line by line, so the second comment line would fall out as code.
 */
#define MAXROUNDS 64

/* ── 系统调用返回值落点 ─────────────────────────────────────────────── */
/* -- Where syscall return values land -- */
int _sysret = 0;     /* 所有系统调用都往这里搬；指针再 (char*) 转回去 */
                     /* Every syscall moves its result here; pointers are cast back with (char*). */

/* ── 薄薄一层 syscall 包装 ─────────────────────────────────────────────
 * -- A thin syscall wrapper --
 * `${var}` 由前端展开成「把变量装进 R0/R1/… 再发 SYSCALL」，顺序即出现顺序。
 * `${var}` is expanded by the front end into "load the variable into R0/R1/... then issue SYSCALL", in order of appearance.
 * ⚠ 实参**一律用变量**：写 `${fd}, ${ip}, 80` 的话字面量 80 不会进 R2。
 * ⚠ Arguments must **always be variables**: with `${fd}, ${ip}, 80` the literal 80 never reaches R2.
 */
void sys_dns(char* host) {
    asm("SYSCALL #338, ${host}");
    asm("MOVE [_sysret], R0");
}

void sys_socket(void) {
    asm("MOVE R0 #2");            /* AF_INET */
    asm("MOVE R1 #1");            /* SOCK_STREAM */
    asm("SYSCALL #330");
    asm("MOVE [_sysret], R0");
}

void sys_connect(int fd, char* ip, int port) {
    asm("SYSCALL #334, ${fd}, ${ip}, ${port}");
    asm("MOVE [_sysret], R0");
}

void sys_send(int fd, int addr, int len) {
    asm("SYSCALL #335, ${fd}, ${addr}, ${len}");
    asm("MOVE [_sysret], R0");
}

void sys_recv(int fd, int addr, int cap) {
    asm("SYSCALL #336, ${fd}, ${addr}, ${cap}");
    asm("MOVE [_sysret], R0");
}

void sys_sockclose(int fd) {
    asm("SYSCALL #337, ${fd}");
    asm("MOVE [_sysret], R0");
}

void sys_fileopen(char* name, int mode) {
    asm("SYSCALL #110, ${name}, ${mode}");
    asm("MOVE [_sysret], R0");
}

void sys_filewrite(int handle, int addr, int len) {
    asm("SYSCALL #113, ${handle}, ${addr}, ${len}");
    asm("MOVE [_sysret], R0");
}

void sys_fileclose(int handle) {
    asm("SYSCALL #111, ${handle}");
    asm("MOVE [_sysret], R0");
}

/* ── 小工具 ─────────────────────────────────────────────────────────── */
/* -- Small helpers -- */

/* 把请求拼出来。用 HTTP/1.0 + Connection: close ⇒ 服务端回完就关连接，
 * Build the request. HTTP/1.0 plus Connection: close means the server closes the connection once it has replied,
 * 读循环靠 recv 返回 0 收尾（这是"读完了"，不是"暂时没数据"）。
 * and the read loop finishes on recv returning 0 (that means "done reading", not "no data yet").
 */
void build_request(char* req) {
    req[0] = 0;
    strcat(req, "GET /v1/forecast?latitude=22.54&longitude=114.06");
    strcat(req, "&daily=temperature_2m_max,temperature_2m_min,weather_code");
    strcat(req, "&forecast_days=7&timezone=Asia/Shanghai HTTP/1.0\r\n");
    strcat(req, "Host: ");
    strcat(req, HOST);
    strcat(req, "\r\n");
    strcat(req, "User-Agent: WayCoder-VML/1.0\r\n");
    strcat(req, "Accept: */*\r\n");
    strcat(req, "Connection: close\r\n");
    strcat(req, "\r\n");
}

/* 响应头与正文的分界（"\r\n\r\n"），返回正文首字节下标；找不到返回 -1。 */
/* The boundary between headers and body ("\r\n\r\n"); returns the index of the first body byte, or -1 if not found. */
int find_body(char* b, int len) {
    int i;
    i = 0;
    while (i + 3 < len) {
        if ((int)b[i] == 13 && (int)b[i + 1] == 10 &&
            (int)b[i + 2] == 13 && (int)b[i + 3] == 10) return i + 4;
        i = i + 1;
    }
    return -1;
}

/* 状态行 "HTTP/1.x NNN ..." 里的 NNN；认不出来返回 -1。 */
/* The NNN in the status line "HTTP/1.x NNN ..."; returns -1 if it cannot be recognized. */
int http_code(char* b, int len) {
    int i;
    int c;
    int code;
    if (len < 12) return -1;
    if (b[0] != 'H' || b[1] != 'T' || b[2] != 'T' || b[3] != 'P') return -1;
    code = 0;
    i = 9;
    while (i < len) {
        c = (int)b[i];
        if (c < 48 || c > 57) break;
        code = code * 10 + (c - 48);
        i = i + 1;
    }
    return code;
}

/* ── 主流程 ─────────────────────────────────────────────────────────── */
/* -- Main flow -- */

char rbuf[BUFCAP];   /* 整个 HTTP 响应（头 + 正文） */
                     /* The whole HTTP response (headers plus body). */

int main(void) {
    char  req[640];
    char* ip;
    int  fd;
    int  port;
    int  code;
    int  total;
    int  n;
    int  want;
    int  addr;
    int  head;
    int  blen;
    int  rounds;
    int  fh;
    int  mode;
    int  wrote;
    int  failed;
    int  lang;     /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                   /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

    lang = ui_get_language();
    failed = 0;
    print_str(lang == 0 ? "=== VML 天气预报 ===\n" : "=== VML Weather Report ===\n");
    print_str(lang == 0 ? "主机 " : "Host "); print_str(HOST);
    print_str(lang == 0 ? "  文件 " : "  file "); print_str(OUTFILE); print_str("\n");

    /* ① 域名 → IP（宿主不解析域名，见文件头 ①）
     * ① Hostname to IP (the host does not resolve names, see pitfall ① in the file header)
     * ⚠ 解析失败时宿主把 R0 置 -1（不是 0），所以判据是 `<= 0` ——
     * ⚠ On a failed lookup the host sets R0 to -1 (not 0), so the test is `<= 0` —
     *   只判 `== 0` 会拿着 -1 当地址去解引用，症状是"内存错误"而不是"DNS 失败"。
     *   testing only `== 0` would dereference -1 as an address, and the symptom would be "memory error" rather than "DNS failure".
     */
    sys_dns(HOST);
    if (_sysret <= 0) {
        print_str(lang == 0 ? "[失败] DNS 解析失败：" : "[FAIL] DNS lookup failed: "); print_str(HOST); print_str("\n");
        return 1;
    }
    ip = (char*)_sysret;
    print_str("[1/4] DNS  "); print_str(ip); print_str("\n");

    /* ② 连接 */
    /* ② Connect. */
    sys_socket();
    fd = _sysret;
    if (fd < 0) {
        print_str(lang == 0 ? "[失败] 创建套接字失败\n" : "[FAIL] socket() failed\n");
        return 2;
    }
    port = PORT;
    sys_connect(fd, ip, port);
    if (_sysret != 0) {
        print_str(lang == 0 ? "[失败] 连接 " : "[FAIL] connect to "); print_str(HOST); print_str(lang == 0 ? " 失败\n" : " failed\n");
        sys_sockclose(fd);
        return 3;
    }
    print_str(lang == 0 ? "[2/4] 已连接 " : "[2/4] connected to "); print_str(HOST); print_str(":80\n");

    /* ③ 发请求 + 读完整响应 */
    /* ③ Send the request and read the whole response. */
    build_request(req);
    n = strlen(req);
    addr = (int)req;
    sys_send(fd, addr, n);
    if (_sysret < 0) {
        print_str(lang == 0 ? "[失败] 发送请求失败\n" : "[FAIL] sending the request failed\n");
        sys_sockclose(fd);
        return 4;
    }

    total = 0;
    rounds = 0;
    while (rounds < MAXROUNDS) {
        rounds = rounds + 1;
        want = BUFCAP - 1 - total;
        if (want <= 0) break;
        addr = (int)rbuf + total;
        sys_recv(fd, addr, want);
        n = _sysret;
        if (n < 0) { print_str(lang == 0 ? "[警告] 读取出错，已读 " : "[WARN] read error, got "); print_int(total); print_str(lang == 0 ? " 字节\n" : " bytes\n"); break; }
        if (n == 0) break;          /* 对端关闭 = 读完了 */
                                    /* The peer closed the connection, which means we are done reading. */
        total = total + n;
    }
    rbuf[total] = 0;
    sys_sockclose(fd);
    print_str(lang == 0 ? "[3/4] 响应 " : "[3/4] response ");
    print_int(total);
    print_str(lang == 0 ? " 字节（" : " bytes (");
    print_int(rounds);
    print_str(lang == 0 ? " 轮 recv）\n" : " recv rounds)\n");

    code = http_code(rbuf, total);
    if (code != 200) {
        print_str(lang == 0 ? "[失败] HTTP 状态码 = " : "[FAIL] HTTP status = "); print_int(code); print_str("\n");
        failed = 1;
    }

    /* ④ 剥头 → 写文件 */
    /* ④ Strip the headers and write the file. */
    head = find_body(rbuf, total);
    if (head < 0) {
        print_str(lang == 0 ? "[失败] 响应里找不到头/正文分界\n" : "[FAIL] no header/body boundary found in the response\n");
        return 5;
    }
    blen = total - head;

    mode = 1;                        /* 1 = 只写（FileMode.Create），见 VMLRuntime 的 #110 */
                                     /* 1 = write only (FileMode.Create), see #110 in VMLRuntime. */
    sys_fileopen(OUTFILE, mode);
    fh = _sysret;
    if (fh < 0) {
        print_str(lang == 0 ? "[失败] 打开文件失败：" : "[FAIL] cannot open file: "); print_str(OUTFILE);
        print_str(lang == 0 ? "（手机上路径必须是相对的）\n" : " (paths must be relative on the phone)\n");
        return 6;
    }
    addr = (int)rbuf + head;
    sys_filewrite(fh, addr, blen);
    wrote = _sysret;
    sys_fileclose(fh);

    /* ⑤ 状态 */
    /* ⑤ Status. */
    print_str("[4/4] HTTP "); print_int(code);
    print_str(lang == 0 ? "  正文 " : "  body "); print_int(blen); print_str(lang == 0 ? " 字节" : " bytes");
    print_str(lang == 0 ? "  写入 " : "  wrote "); print_int(wrote); print_str(lang == 0 ? " 字节 -> " : " bytes -> "); print_str(OUTFILE);
    print_str("\n");

    if (failed || wrote != blen) {
        print_str(lang == 0 ? "[失败] 结果不完整\n" : "[FAIL] incomplete result\n");
        return 7;
    }
    print_str(lang == 0 ? "[成功] 一周预报已写入 " : "[OK] one-week forecast written to "); print_str(OUTFILE);
    print_str(lang == 0 ? "（7 天）\n" : " (7 days)\n");
    return 0;
}
