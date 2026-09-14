// VML C++ 标准库 — 完整运行时支持
// 用法: #include <stdlib.hpp>

#ifndef STDLIB_HPP
#define STDLIB_HPP

namespace vml {

// ============================================================
// 控制台 I/O
// ============================================================
inline void print(const char* s)      { asm("SYSCALL 1"); }
inline void print(int n)              { asm("SYSCALL 6"); }
inline void print(float f)            { asm("SYSCALL 59"); }
inline void print(double f)           { asm("SYSCALL 59"); }
inline void print(char c)             { asm("SYSCALL 4"); }
inline void print(bool b)             { if (b) print("true"); else print("false"); }
inline void println(const char* s)    { print(s); asm("LOAD R0 #10"); asm("SYSCALL 4"); }
inline void println(int n)            { print(n); asm("LOAD R0 #10"); asm("SYSCALL 4"); }
inline void println(float f)          { print(f); asm("LOAD R0 #10"); asm("SYSCALL 4"); }
inline void println(double f)         { print(f); asm("LOAD R0 #10"); asm("SYSCALL 4"); }
inline void println()                 { asm("LOAD R0 #10"); asm("SYSCALL 4"); }
inline void print_hex(int n)          { asm("SYSCALL 10"); }
inline int  input_int()               { asm("SYSCALL 7"); return 0; }
inline void input_str(char* buf)      { asm("SYSCALL 2"); }
inline int  getchar()                 { asm("SYSCALL 5"); return 0; }
inline void putchar(char c)           { asm("SYSCALL 4"); }

// ============================================================
// 字符串
// ============================================================
inline int  strlen(const char* s)     { asm("SYSCALL 60"); return 0; }
inline void strcpy(char* d, const char* s) { asm("SYSCALL 61"); }
inline int  strcmp(const char* a, const char* b) { asm("SYSCALL 62"); return 0; }
inline void strcat(char* d, const char* s) { asm("SYSCALL 63"); }
inline const char* strstr(const char* haystack, const char* needle) { return 0; }
inline const char* strchr(const char* s, int c) { return 0; }
inline int  strncmp(const char* a, const char* b, int n) { return 0; }
inline void strncpy(char* d, const char* s, int n) {}

// ============================================================
// 字符串扩展 (C++ std::string 风格)
// ============================================================
inline int  string_length(const char* s) { return strlen(s); }
inline bool string_empty(const char* s) { return strlen(s) == 0; }
inline void string_copy(char* dst, const char* src) { strcpy(dst, src); }
inline int  string_compare(const char* a, const char* b) { return strcmp(a, b); }
inline void string_concat(char* dst, const char* src) { strcat(dst, src); }
inline const char* string_find(const char* s, const char* sub) { return strstr(s, sub); }
inline bool string_contains(const char* s, const char* sub) { return strstr(s, sub) != 0; }
inline bool string_starts_with(const char* s, const char* prefix) { return strncmp(s, prefix, strlen(prefix)) == 0; }
inline bool string_ends_with(const char* s, const char* suffix) {
    int sl = strlen(s), sul = strlen(suffix);
    if (sul > sl) return false;
    return strcmp(s + sl - sul, suffix) == 0;
}

inline void string_to_upper(char* dst, const char* src) {
    while (*src) {
        char c = *src++;
        if (c >= 'a' && c <= 'z') c -= 32;
        *dst++ = c;
    }
    *dst = 0;
}
inline void string_to_lower(char* dst, const char* src) {
    while (*src) {
        char c = *src++;
        if (c >= 'A' && c <= 'Z') c += 32;
        *dst++ = c;
    }
    *dst = 0;
}
inline void string_reverse(char* dst, const char* src) {
    int len = strlen(src);
    for (int i = 0; i < len; i++) dst[i] = src[len - 1 - i];
    dst[len] = 0;
}

// ============================================================
// 字符类型
// ============================================================
inline bool is_digit(char c)    { return c >= '0' && c <= '9'; }
inline bool is_alpha(char c)    { return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'); }
inline bool is_alnum(char c)    { return is_digit(c) || is_alpha(c); }
inline bool is_upper(char c)    { return c >= 'A' && c <= 'Z'; }
inline bool is_lower(char c)    { return c >= 'a' && c <= 'z'; }
inline bool is_space(char c)    { return c == ' ' || c == '\t' || c == '\n' || c == '\r'; }
inline bool is_print(char c)    { return c >= 32 && c <= 126; }
inline char to_upper(char c)    { return is_lower(c) ? c - 32 : c; }
inline char to_lower(char c)    { return is_upper(c) ? c + 32 : c; }

// ============================================================
// 数学
// ============================================================
inline int   abs(int x)               { asm("SYSCALL 43"); return 0; }
inline float fabs(float x)            { asm("SYSCALL 44"); return 0.0f; }
inline double dabs(double x)          { asm("SYSCALL 44"); return 0.0; }
inline int   min(int a, int b)        { asm("SYSCALL 45"); return 0; }
inline int   max(int a, int b)        { asm("SYSCALL 46"); return 0; }
inline float fmin(float a, float b)   { return a < b ? a : b; }
inline float fmax(float a, float b)   { return a > b ? a : b; }
inline double dmin(double a, double b) { return a < b ? a : b; }
inline double dmax(double a, double b) { return a > b ? a : b; }

inline float sqrt(float x)            { asm("SYSCALL 20"); return 0.0f; }
inline double dsqrt(double x)         { asm("SYSCALL 20"); return 0.0; }
inline float sin(float x)             { asm("SYSCALL 21"); return 0.0f; }
inline float cos(float x)             { asm("SYSCALL 22"); return 0.0f; }
inline float tan(float x)             { asm("SYSCALL 23"); return 0.0f; }
inline float asin(float x)            { asm("SYSCALL 24"); return 0.0f; }
inline float acos(float x)            { asm("SYSCALL 25"); return 0.0f; }
inline float atan(float x)            { asm("SYSCALL 32"); return 0.0f; }
inline float atan2(float y, float x)  { asm("SYSCALL 33"); return 0.0f; }
inline float pow(float x, float y)    { asm("SYSCALL 26"); return 0.0f; }
inline float exp(float x)             { asm("SYSCALL 27"); return 0.0f; }
inline float log(float x)             { asm("SYSCALL 28"); return 0.0f; }
inline float log10(float x)           { return log(x) / log(10.0f); }
inline float log2(float x)            { return log(x) / log(2.0f); }
inline float floor(float x)           { asm("SYSCALL 29"); return 0.0f; }
inline float ceil(float x)            { asm("SYSCALL 30"); return 0.0f; }
inline float round(float x)           { asm("SYSCALL 31"); return 0.0f; }
inline float trunc(float x)           { return x >= 0 ? floor(x) : ceil(x); }
inline float hypot(float x, float y)  { return sqrt(x*x + y*y); }

inline int   rand()                   { asm("SYSCALL 50"); return 0; }
inline void  srand(int seed)          { asm("SYSCALL 51"); }

const float  PI = 3.141592653589793f;
const float  E  = 2.718281828459045f;

// ============================================================
// 类型转换
// ============================================================
inline int   atoi(const char* s)      { asm("SYSCALL 40"); return 0; }
inline float atof(const char* s)      { asm("SYSCALL 41"); return 0.0f; }
inline void  itoa(int n, char* buf)   { asm("SYSCALL 42"); }
inline void  ftoa(float f, char* buf) { asm("SYSCALL 58"); }

// ============================================================
// 内存
// ============================================================
inline void memset(void* p, int v, int n)   { asm("SYSCALL 70"); }
inline void memcpy(void* d, const void* s, int n) { asm("SYSCALL 71"); }
inline int  memcmp(const void* a, const void* b, int n) { asm("SYSCALL 13"); return 0; }
inline void* malloc(int size)              { asm("SYSCALL 40"); return 0; }
inline void free(void* ptr)                { asm("SYSCALL 41"); }

// ============================================================
// 系统
// ============================================================
inline void exit(int code)            { asm("SYSCALL 3"); }
inline void delay(int ms)             { asm("SYSCALL 52"); }
inline void sleep(int ms)             { delay(ms); }
inline int  get_tick()                { asm("SYSCALL 53"); return 0; }
inline int  get_config(int key)       { asm("SYSCALL 60"); return 0; }
inline void get_date(char* buf)       { asm("SYSCALL 55"); }
inline void get_time(char* buf)       { asm("SYSCALL 56"); }
inline int  get_datetime()            { asm("SYSCALL 54"); return 0; }
inline void clear_screen()            { asm("SYSCALL 12"); }

// ============================================================
// 文件 I/O
// ============================================================
inline int file_open(const char* name, int mode)   { asm("SYSCALL 110"); return -1; }
inline void file_close(int handle)                  { asm("SYSCALL 111"); }
inline int file_read(int handle, void* buf, int size) { asm("SYSCALL 112"); return 0; }
inline int file_write(int handle, const void* data, int size) { asm("SYSCALL 113"); return 0; }
inline int file_seek(int handle, int offset, int whence) { asm("SYSCALL 115"); return 0; }
inline int file_tell(int handle)                    { asm("SYSCALL 116"); return 0; }

enum FileMode { READ = 0, WRITE = 1, APPEND = 2, READ_PLUS = 3, WRITE_PLUS = 4 };

// ============================================================
// STL 容器 — 轻量 vector
// ============================================================
template<typename T>
struct vector {
    T* data;
    int len;
    int cap;

    vector() : data(0), len(0), cap(0) {}

    int size() const { return len; }
    int capacity() const { return cap; }
    bool empty() const { return len == 0; }

    void push_back(const T& val) {
        if (len >= cap) {
            int new_cap = cap == 0 ? 4 : cap * 2;
            T* new_data = (T*)malloc(new_cap * sizeof(T));
            for (int i = 0; i < len; i++) new_data[i] = data[i];
            if (data) free(data);
            data = new_data;
            cap = new_cap;
        }
        data[len++] = val;
    }

    void pop_back() { if (len > 0) len--; }

    T& operator[](int index) { return data[index]; }
    const T& operator[](int index) const { return data[index]; }

    T& back() { return data[len - 1]; }
    T& front() { return data[0]; }

    void clear() { len = 0; }
    void resize(int n) {
        if (n > cap) {
            int new_cap = n;
            T* new_data = (T*)malloc(new_cap * sizeof(T));
            for (int i = 0; i < len && i < n; i++) new_data[i] = data[i];
            if (data) free(data);
            data = new_data;
            cap = new_cap;
        }
        len = n;
    }

    void reserve(int n) {
        if (n > cap) {
            T* new_data = (T*)malloc(n * sizeof(T));
            for (int i = 0; i < len; i++) new_data[i] = data[i];
            if (data) free(data);
            data = new_data;
            cap = n;
        }
    }
};

// ============================================================
// <algorithm> — 算法
// ============================================================
template<typename T>
inline const T& max_element(const T& a, const T& b) { return a > b ? a : b; }

template<typename T>
inline const T& min_element(const T& a, const T& b) { return a < b ? a : b; }

template<typename T>
void sort(T* arr, int n) {
    for (int i = 0; i < n - 1; i++)
        for (int j = i + 1; j < n; j++)
            if (arr[j] < arr[i]) { T tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp; }
}

template<typename T>
T* find(T* first, T* last, const T& value) {
    while (first != last) { if (*first == value) return first; first++; }
    return last;
}

template<typename T>
int count(T* first, T* last, const T& value) {
    int c = 0;
    while (first != last) { if (*first == value) c++; first++; }
    return c;
}

template<typename T>
void reverse(T* first, T* last) {
    while (first < last) { last--; T tmp = *first; *first = *last; *last = tmp; first++; }
}

template<typename T>
void fill(T* first, T* last, const T& value) {
    while (first != last) { *first = value; first++; }
}

template<typename T>
T* copy(T* first, T* last, T* dst) {
    while (first != last) { *dst++ = *first++; }
    return dst;
}

template<typename T>
T accumulate(T* first, T* last, T init) {
    T result = init;
    while (first != last) { result += *first; first++; }
    return result;
}

template<typename T>
void iota(T* first, T* last, T value) {
    while (first != last) { *first++ = value++; }
}

// ============================================================
// 配对
// ============================================================
template<typename T1, typename T2>
struct pair {
    T1 first;
    T2 second;
};

// ============================================================
// 引用包装
// ============================================================
template<typename T>
T&& move(T&& x) { return x; }

template<typename T>
T&& forward(T&& x) { return x; }

} // namespace vml

#endif // STDLIB_HPP
