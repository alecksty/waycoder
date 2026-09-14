// Auto-generated function declarations for shared library

// bitops.c
__stdcall int shared_bit_and(int a, int b);
__stdcall int shared_bit_clear(int val, int bit);
__stdcall int shared_bit_not(int a);
__stdcall int shared_bit_or(int a, int b);
__stdcall int shared_bit_set(int val, int bit);
__stdcall int shared_bit_shl(int a, int bits);
__stdcall int shared_bit_shr(int a, int bits);
__stdcall int shared_bit_test(int val, int bit);
__stdcall int shared_bit_toggle(int val, int bit);
__stdcall int shared_bit_xor(int a, int b);
__stdcall int shared_rol(int val, int bits);
__stdcall int shared_ror(int val, int bits);

// builtins.c
__stdcall int vml_abs(int x);
void* vml_alloc(int size);
__stdcall void vml_assert(int cond, const char* msg);
__stdcall int vml_clamp(int x, int low, int high);
__stdcall void vml_debug_print(const char* msg);
__stdcall void vml_debug_print_int(int val);
__stdcall void vml_free(void* ptr);
__stdcall int vml_get_tick(void);
__stdcall int vml_max(int a, int b);
__stdcall int vml_min(int a, int b);
__stdcall void vml_newline(void);
__stdcall int vml_peek(int addr);
__stdcall int vml_peekb(int addr);
__stdcall void vml_poke(int val, int addr);
__stdcall void vml_pokeb(int val, int addr);
__stdcall void vml_print_hex(int val);
__stdcall void vml_print_int(int val);
__stdcall void vml_print_str(const char* str);
__stdcall void vml_putchar(char ch);
__stdcall int vml_random(void);
__stdcall void vml_sleep(int ms);

// convert.c
between byte(8-bit);
Byte conversions(8-bit);
word conversions(16-bit);
Word conversions(32-bit);
to int(decimal);
to int(supports "0x" prefix);
byte N(0=LSB);
byte N(0=LSB);
long shared_byte_to_dword(unsigned char b);
__stdcall int shared_byte_to_hword(unsigned char b);
__stdcall int shared_byte_to_word(unsigned char b);
__stdcall unsigned char shared_dword_to_byte(long long d);
__stdcall int shared_dword_to_hword(long long d);
__stdcall int shared_dword_to_int(long long d);
__stdcall int shared_dword_to_word(long long d);
__stdcall int shared_hex_to_int(const char* s);
__stdcall unsigned char shared_hword_to_byte(int h);
long shared_hword_to_dword(int h);
__stdcall int shared_hword_to_word(int h);
__stdcall int shared_int_to_hex(int val, char* buf);
__stdcall int shared_int_to_str(int val, char* buf);
__stdcall int shared_str_to_int(const char* s);
__stdcall int shared_word_bswap(int w);
__stdcall unsigned char shared_word_get_byte(int w, int n);
__stdcall int shared_word_set_byte(int w, int n, unsigned char b);
__stdcall unsigned char shared_word_to_byte(int w);
long shared_word_to_dword(int w);
long shared_word_to_dword_u(unsigned int w);
__stdcall int shared_word_to_hword(int w);
to string(decimal, buffer must be at least 12 bytes);
hex string(buffer must be at least 11 bytes for 32-bit);

// ctype.c
C 标准名(isalnum/isspace/...);
__stdcall int shared_is_alpha(int c);
return shared_is_alpha(c);
__stdcall int shared_is_digit(int c);
return shared_is_digit(c);
__stdcall int shared_isalnum(int c);
__stdcall int shared_iscntrl(int c);
__stdcall int shared_islower(int c);
__stdcall int shared_isprint(int c);
__stdcall int shared_ispunct(int c);
__stdcall int shared_isspace(int c);
__stdcall int shared_isupper(int c);
__stdcall int shared_isxdigit(int c);
__stdcall int shared_to_lower(int c);
__stdcall int shared_to_upper(int c);

// device.c
__stdcall int shared_dev_close(int handle);
__stdcall int shared_dev_control(int handle, int cmd, int data);
__stdcall int shared_dev_open(const char* name);
__stdcall int shared_dev_read(int handle, void* buf, int offset, int count);
__stdcall int shared_dev_write(int handle, const void* buf, int offset, int count);

// file.c
__stdcall int shared_fclose(int handle);
__stdcall int shared_fopen(const char* path, int mode);
__stdcall int shared_fread(int handle, void* buf, int count);
__stdcall int shared_fseek(int handle, int offset, int whence);
__stdcall int shared_fsize(int handle);
__stdcall int shared_ftell(int handle);
__stdcall int shared_ftruncate(int handle, int size);
__stdcall int shared_fwrite(int handle, const void* buf, int count);

// float.c
__stdcall void shared_getfloat(void);
__stdcall void shared_putfloat(float f);
__stdcall void shared_puthex(int val);

// io.c
__stdcall void shared_clear_screen(void);
__stdcall int shared_getchar(void);
__stdcall int shared_input_int(void);
__stdcall int shared_input_str(void);
__stdcall int shared_kb_hit(void);
__stdcall void shared_print_hex(int val);
__stdcall void shared_print_int(int val);
__stdcall void shared_print_str(const char* str);
__stdcall void shared_print_str_no_nl(const char* str);
__stdcall void shared_print_string(const char* str);
__stdcall void shared_println_hex(int val);
__stdcall void shared_println_int(int val);
__stdcall void shared_println_str(const char* str);
__stdcall void shared_putchar(char c);
__stdcall void shared_puts(const char* str);

// math.c
return _cos_taylor(x);
return _log_taylor(x - 1.0);
sign * _sin_taylor(x);
__stdcall double acos(double x);
__stdcall double asin(double x);
__stdcall double atan(double x);
return atan_approx(y / x);
return atan_approx(y / x);
return atan_approx(y / x);
return atan2(x, shared_sqrt(1.0 - x * x);
return atan2(shared_sqrt(1.0 - x * x);
return atan2(x, 1.0);
__stdcall double atan2(double y, double x);
__stdcall int clamp(int x, int low, int high);
__stdcall double cos(double x);
__stdcall double exp(double x);
return exp(y * log(x);
__stdcall int is_even(int x);
__stdcall int is_odd(int x);
__stdcall double log(double x);
__stdcall int pow10(int n);
__stdcall int shared_abs(int x);
__stdcall int shared_max(int a, int b);
__stdcall int shared_min(int a, int b);
__stdcall double shared_pow(double x, double y);
__stdcall void shared_randomize(int seed);
__stdcall double shared_sqrt(double x);
__stdcall int sign(int x);
__stdcall double sin(double x);
__stdcall double tan(double x);

// memory.c
__stdcall int shared_memcmp(const void* a, const void* b, int n);
__stdcall void* shared_memcpy(void* dst, const void* src, int n);
__stdcall void* shared_memmove(void* dst, const void* src, int n);
__stdcall void* shared_memset(void* ptr, int val, int n);

// network.c
1 asm("LOAD R0 #2");
Connect asm("MOVE R0, fd");
1 asm("LOAD R0 #2");
Bind asm("MOVE R0, fd");
5 asm("MOVE R0, fd");
__stdcall void shared_net_close(int fd);
__stdcall int shared_net_connect(const char* host, int port);
__stdcall int shared_net_listen(int port);
__stdcall int shared_net_recv(int fd, void* buf, int max_len);
__stdcall int shared_net_send(int fd, void* data, int len);

// os.c
__stdcall int cond_broadcast(int cond);
__stdcall int cond_create(void);
__stdcall int cond_signal(int cond);
__stdcall int cond_wait(int cond, int mutex);
int exec(const char* path);
int file_stat(const char* path, void* buf);
int get_args(void* buf);
char* get_env(const char* name);
__stdcall int get_pid(void);
__stdcall int mkdir(const char* path);
__stdcall int mutex_create(void);
__stdcall int mutex_lock(int id);
__stdcall int mutex_unlock(int id);
void process_exit(int code);
int read_dir(const char* path, void* buf);
__stdcall int remove_file(const char* path);
__stdcall int rename_file(const char* old, const char* new);
__stdcall void set_env(const char* name, const char* val);
__stdcall int signal(int signum, void* handler);
__stdcall int socket_accept(int fd);
__stdcall int socket_bind(int fd, int port);
__stdcall int socket_close(int fd);
__stdcall int socket_connect(int fd, const char* addr, int port);
__stdcall int socket_create(int domain, int type);
__stdcall int socket_listen(int fd, int backlog);
__stdcall int socket_recv(int fd, void* buf, int len);
__stdcall int socket_send(int fd, const void* buf, int len);
__stdcall int thread_create(void* fn, int stack_size);
__stdcall void thread_exit(void);
__stdcall int thread_join(int tid);
__stdcall void thread_sleep(int ms);
__stdcall void thread_yield(void);
__stdcall int type_name(int type_id, char* buf);
__stdcall int type_of(void* addr);

// printf.c
cdecl 传参规则(右到左压栈, 调用者清理);
1 个参数(兼容旧接口);
5 个参数(最多 6 个);
输出字符串 s(限长 maxlen, maxlen<0=不限);
int shared_printf2(char *buf, const char *fmt, int a1, int a2);
int shared_printf3(char *buf, const char *fmt, int a1, int a2, int a3);
int shared_printf4(char *buf, const char *fmt, int a1, int a2, int a3, int a4);
int shared_printf5(char *buf, const char *fmt, int a1, int a2, int a3, int a4, int a5);
int shared_sprintf(char *buf, const char *fmt, int a1);
int shared_vsnprintf(char *buf, const char *fmt, const int *args, int nargs);
return shared_vsnprintf(buf, fmt, args, 1);
return shared_vsnprintf(buf, fmt, args, 2);
return shared_vsnprintf(buf, fmt, args, 3);
return shared_vsnprintf(buf, fmt, args, 4);
return shared_vsnprintf(buf, fmt, args, 5);

// readline.c
__stdcall int shared_read_line(char* buf, int max_len);

// softdouble.c
C 实现(兼容 VML C compiler);
754 双精度(64-bit);

// softfloat.c
C 实现(兼容 VML C compiler);

// string.c
to lowercase(in-place or to dst);
__stdcall int shared_str_contains(const char* s, const char* sub);
__stdcall int shared_str_repeat(char* dst, const char* src, int n);
__stdcall void shared_str_tolower(char* dst, const char* src);
__stdcall void shared_str_toupper(char* dst, const char* src);
char* shared_strcat(char* dst, const char* src);
const char* shared_strchr(const char* s, int c);
__stdcall int shared_strcmp(const char* a, const char* b);
char* shared_strcpy(char* dst, const char* src);
__stdcall int shared_strlen(const char* s);
__stdcall int shared_strncmp(const char* a, const char* b, int n);
char* shared_strncpy(char* dst, const char* src, int n);
__stdcall int shared_strrev(char* dst, const char* src);
const char* shared_strstr(const char* haystack, const char* needle);
return shared_strstr(s, sub);
to uppercase(in-place or to dst);

// sysinfo.c
Unix 时间戳(SYSCALL 54: GetDateTime);
__stdcall int shared_datetime(void);
__stdcall void shared_exit(int code);
const char* shared_get_date(void);
const char* shared_get_time(void);
__stdcall int shared_getconfig(int type);
__stdcall int shared_random(void);
__stdcall void shared_srand(int seed);

// util.c
square root(floor);
__stdcall int shared_atoi(const char* s);
__stdcall void shared_delay(int ms);
__stdcall int shared_int_pow(int base, int exp);
__stdcall int shared_int_sqrt(int n);
__stdcall int shared_sscanf(const char* s, const char* fmt, void* ptr);
__stdcall int shared_sum(int* arr, int n);

// vga_text.c
SCREEN 模式(0=文本, >0=图形);
VGA 文本帧缓冲基址(PC标准);
__stdcall void vga_text_newline(void);
__stdcall void vga_text_putchar(int c);
