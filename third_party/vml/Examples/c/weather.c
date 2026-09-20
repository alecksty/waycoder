/* weather.c —— 联网抓一周天气预报 → 写进工作区里的 weather.txt → 输出状态。
 *
 * 三段：① 裸套接字取回 api.open-meteo.com 的预报 JSON（明文 HTTP，80 端口 ——
 * VM 里没有 TLS，所以不能走 https）；② 剥掉响应头，把正文写进 weather.txt
 * （**相对路径**：手机上沙箱根 = 工作区，绝对路径会被拒）；③ 把 HTTP 码 / 收发字节数
 * 打出来。天气源免密钥，一次请求返回正好 7 天。
 *
 * 跑法（桌面脚手架，秒级，别为改一行去打 APK）：
 *     VML_HOME=<含 Lib/ 的 vml 根> \
 *       dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/c/weather.c --timeout 60
 * 手机上：命令行页敲 `vml run examples/c/weather.c`，文件落在工作区根的 weather.txt。
 *
 * ⚠ 三个已实测的坑，改动本文件前先读：
 *
 * ① **域名不由宿主解析**。`#334 SocketConnect(fd, ip, port)` 的实现是
 *    `IPAddress.Parse(ip)`（见 VMLRuntime/VMLRuntime.Syscall.OS.cs），喂域名会直接抛异常
 *    返回 -1。所以必须先 `#338 DnsResolve(host)` 把域名换成 IP 字符串。
 *
 * ② **不要用 Lib/shared/src/network.c 与 file.c 的包装函数**。它们写的是
 *        int r; asm("SYSCALL #113"); return r;
 *    而 C 前端对**带 `#` 的 SYSCALL** 会额外补一条 `move [R12-4], R0`，把结果塞进一个
 *    **硬编码槽位**；函数的局部量却在 R12-8 —— 两者不是一个格子。于是这些函数的
 *    返回值是**未初始化的栈内容**（实测：真写了 3 字节的 fwrite 返回 0；真发了 170 字节的
 *    net_send 返回 0；net_recv 恒返回 0 会让读循环当场退出）。**副作用是真的，只有返回值是坏的**。
 *    本文件因此自己包一层 `sys_*`，照 Lib/shared/src/vmlsys.c 的写法：`SYSCALL` 之后
 *    显式 `MOVE [_sysret], R0` —— SYSCALL 之后 R0 没人动过，搬回来就是返回值。
 *
 * ③ **函数名别叫 `f1` / `f2` / `d0` / `l3` 这类「一个字母 + 纯数字」**。
 *    `VMLAssembler.ParseOperand` 认寄存器用的是**大小写不敏感**的前缀
 *    （`StartsWith("F", OrdinalIgnoreCase)` 等，R/F/D/L 四个字母都是），于是
 *    **小写 `f2` 被当成浮点寄存器 2**，`call f2` 在链接期被改写成 `call R2` ——
 *    跳到一个从未赋值的寄存器。避开「这四个字母 + 后缀全是数字」就没事
 *    （`find_body`、`foo2`、`g1` 都正常）。
 */

#include <waycoder_ui.h>

#define HOST    "api.open-meteo.com"
#define PORT    80
#define OUTFILE "weather.txt"

#define BUFCAP    8192   /* 响应缓冲：响应实测 ~685 字节（头 ~120 + 正文 ~563），8K 绰绰有余 */

/* 读循环上限：防「对端不关连接」时死读。真到了这一步说明服务端没按 Connection: close
 * 收场；VM 自身的超时是最后一道闸。⚠ 注释不能跨行写在 #define 那一行里 —— 预处理器
 * 按行吃掉宏体，注释的第二行会掉出来当代码。 */
#define MAXROUNDS 64

/* ── 系统调用返回值落点 ─────────────────────────────────────────────── */
int _sysret = 0;     /* 所有系统调用都往这里搬；指针再 (char*) 转回去 */

/* ── 薄薄一层 syscall 包装 ─────────────────────────────────────────────
 * `${var}` 由前端展开成「把变量装进 R0/R1/… 再发 SYSCALL」，顺序即出现顺序。
 * ⚠ 实参**一律用变量**：写 `${fd}, ${ip}, 80` 的话字面量 80 不会进 R2。 */
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

/* 把请求拼出来。用 HTTP/1.0 + Connection: close ⇒ 服务端回完就关连接，
 * 读循环靠 recv 返回 0 收尾（这是"读完了"，不是"暂时没数据"）。 */
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

char rbuf[BUFCAP];   /* 整个 HTTP 响应（头 + 正文） */

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

    failed = 0;
    print_str("=== VML 天气预报 ===\n");
    print_str("主机 "); print_str(HOST); print_str("  文件 "); print_str(OUTFILE); print_str("\n");

    /* ① 域名 → IP（宿主不解析域名，见文件头 ①）
     * ⚠ 解析失败时宿主把 R0 置 -1（不是 0），所以判据是 `<= 0` ——
     *   只判 `== 0` 会拿着 -1 当地址去解引用，症状是"内存错误"而不是"DNS 失败"。 */
    sys_dns(HOST);
    if (_sysret <= 0) {
        print_str("[失败] DNS 解析失败："); print_str(HOST); print_str("\n");
        return 1;
    }
    ip = (char*)_sysret;
    print_str("[1/4] DNS  "); print_str(ip); print_str("\n");

    /* ② 连接 */
    sys_socket();
    fd = _sysret;
    if (fd < 0) {
        print_str("[失败] 创建套接字失败\n");
        return 2;
    }
    port = PORT;
    sys_connect(fd, ip, port);
    if (_sysret != 0) {
        print_str("[失败] 连接 "); print_str(HOST); print_str(" 失败\n");
        sys_sockclose(fd);
        return 3;
    }
    print_str("[2/4] 已连接 "); print_str(HOST); print_str(":80\n");

    /* ③ 发请求 + 读完整响应 */
    build_request(req);
    n = strlen(req);
    addr = (int)req;
    sys_send(fd, addr, n);
    if (_sysret < 0) {
        print_str("[失败] 发送请求失败\n");
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
        if (n < 0) { print_str("[警告] 读取出错，已读 "); print_int(total); print_str(" 字节\n"); break; }
        if (n == 0) break;          /* 对端关闭 = 读完了 */
        total = total + n;
    }
    rbuf[total] = 0;
    sys_sockclose(fd);
    print_str("[3/4] 响应 "); print_int(total); print_str(" 字节（"); print_int(rounds); print_str(" 轮 recv）\n");

    code = http_code(rbuf, total);
    if (code != 200) {
        print_str("[失败] HTTP 状态码 = "); print_int(code); print_str("\n");
        failed = 1;
    }

    /* ④ 剥头 → 写文件 */
    head = find_body(rbuf, total);
    if (head < 0) {
        print_str("[失败] 响应里找不到头/正文分界\n");
        return 5;
    }
    blen = total - head;

    mode = 1;                        /* 1 = 只写（FileMode.Create），见 VMLRuntime 的 #110 */
    sys_fileopen(OUTFILE, mode);
    fh = _sysret;
    if (fh < 0) {
        print_str("[失败] 打开文件失败："); print_str(OUTFILE); print_str("（手机上路径必须是相对的）\n");
        return 6;
    }
    addr = (int)rbuf + head;
    sys_filewrite(fh, addr, blen);
    wrote = _sysret;
    sys_fileclose(fh);

    /* ⑤ 状态 */
    print_str("[4/4] HTTP "); print_int(code);
    print_str("  正文 "); print_int(blen); print_str(" 字节");
    print_str("  写入 "); print_int(wrote); print_str(" 字节 -> "); print_str(OUTFILE);
    print_str("\n");

    if (failed || wrote != blen) {
        print_str("[失败] 结果不完整\n");
        return 7;
    }
    print_str("[成功] 一周预报已写入 "); print_str(OUTFILE); print_str("（7 天）\n");
    return 0;
}
