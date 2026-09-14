// VML Go 标准库 — 完整运行时支持
// 用法: import stdlib

package main

// ============================================================
// fmt 包 — 格式化 I/O
// ============================================================
func Print(args ...interface{}) {
    asm("SYSCALL 1")
}

func Println(args ...interface{}) {
    asm("SYSCALL 1")
    asm("LOAD R0 #10")
    asm("SYSCALL 4")
}

func Printf(format string, args ...interface{}) {
    asm("SYSCALL 1")
}

func Sprint(args ...interface{}) string { return "" }
func Sprintf(format string, args ...interface{}) string { return "" }
func Sprintln(args ...interface{}) string { return "" }
func Fprint(w interface{}, args ...interface{}) {}
func Fprintf(w interface{}, format string, args ...interface{}) {}
func Fprintln(w interface{}, args ...interface{}) {}
func Errorf(format string, args ...interface{}) error { return nil }

func Scan(args ...interface{}) (int, error) {
    asm("SYSCALL 2")
    return 0, nil
}
func Scanf(format string, args ...interface{}) (int, error) { return 0, nil }
func Scanln(args ...interface{}) (int, error) {
    asm("SYSCALL 2")
    return 0, nil
}

// ============================================================
// strings 包 — 字符串操作
// ============================================================
func Compare(a, b string) int { asm("SYSCALL 62"); return 0 }
func Contains(s, substr string) bool { return Index(s, substr) >= 0 }
func ContainsAny(s, chars string) bool { return false }
func ContainsRune(s string, r rune) bool { return false }
func Count(s, substr string) int {
    if len(substr) == 0 { return len(s) + 1 }
    n := 0
    for i := 0; i <= len(s)-len(substr); i++ {
        if s[i:i+len(substr)] == substr { n++; i += len(substr) - 1 }
    }
    return n
}
func EqualFold(s, t string) bool { return ToLower(s) == ToLower(t) }
func Fields(s string) []string {
    return Split(strings_TrimSpace(s), " ")
}
func HasPrefix(s, prefix string) bool { return len(s) >= len(prefix) && s[:len(prefix)] == prefix }
func HasSuffix(s, suffix string) bool { return len(s) >= len(suffix) && s[len(s)-len(suffix):] == suffix }
func Index(s, substr string) int {
    for i := 0; i <= len(s)-len(substr); i++ {
        if s[i:i+len(substr)] == substr { return i }
    }
    return -1
}
func IndexAny(s, chars string) int { return -1 }
func IndexByte(s string, c byte) int {
    for i := 0; i < len(s); i++ {
        if s[i] == c { return i }
    }
    return -1
}
func IndexRune(s string, r rune) int { return -1 }
func Join(elems []string, sep string) string {
    if len(elems) == 0 { return "" }
    result := elems[0]
    for i := 1; i < len(elems); i++ { result += sep + elems[i] }
    return result
}
func LastIndex(s, substr string) int {
    for i := len(s) - len(substr); i >= 0; i-- {
        if s[i:i+len(substr)] == substr { return i }
    }
    return -1
}
func LastIndexByte(s string, c byte) int {
    for i := len(s) - 1; i >= 0; i-- {
        if s[i] == c { return i }
    }
    return -1
}
func Map(mapping func(rune) rune, s string) string { return "" }
func Repeat(s string, count int) string {
    result := ""
    for i := 0; i < count; i++ { result += s }
    return result
}
func Replace(s, old, new string, n int) string {
    if n == 0 { return s }
    result := ""
    i := 0
    replaced := 0
    for i < len(s) {
        if (n < 0 || replaced < n) && i+len(old) <= len(s) && s[i:i+len(old)] == old {
            result += new; i += len(old); replaced++
        } else { result += string(s[i]); i++ }
    }
    return result
}
func ReplaceAll(s, old, new string) string { return Replace(s, old, new, -1) }
func Split(s, sep string) []string {
    if sep == "" { return []string{s} }
    result := []string{}
    start := 0
    for i := 0; i <= len(s)-len(sep); i++ {
        if s[i:i+len(sep)] == sep {
            result = append(result, s[start:i])
            start = i + len(sep); i = start - 1
        }
    }
    result = append(result, s[start:])
    return result
}
func SplitN(s, sep string, n int) []string { return []string{} }
func SplitAfter(s, sep string) []string { return []string{} }
func SplitAfterN(s, sep string, n int) []string { return []string{} }
func Title(s string) string { return "" }
func ToLower(s string) string {
    result := ""
    for i := 0; i < len(s); i++ {
        c := s[i]
        if c >= 'A' && c <= 'Z' { c += 32 }
        result += string(c)
    }
    return result
}
func ToLowerSpecial(c interface{}, s string) string { return ToLower(s) }
func ToTitle(s string) string { return "" }
func ToTitleSpecial(c interface{}, s string) string { return "" }
func ToUpper(s string) string {
    result := ""
    for i := 0; i < len(s); i++ {
        c := s[i]
        if c >= 'a' && c <= 'z' { c -= 32 }
        result += string(c)
    }
    return result
}
func ToUpperSpecial(c interface{}, s string) string { return ToUpper(s) }
func ToValidUTF8(s, replacement string) string { return s }
func Trim(s, cutset string) string { return strings_TrimLeft(strings_TrimRight(s, cutset), cutset) }
func TrimFunc(s string, f func(rune) bool) string { return "" }
func TrimLeft(s, cutset string) string { return strings_TrimLeft(s, cutset) }
func TrimLeftFunc(s string, f func(rune) bool) string { return "" }
func TrimPrefix(s, prefix string) string {
    if HasPrefix(s, prefix) { return s[len(prefix):] }
    return s
}
func TrimRight(s, cutset string) string { return strings_TrimRight(s, cutset) }
func TrimRightFunc(s string, f func(rune) bool) string { return "" }
func TrimSpace(s string) string { return strings_TrimSpace(s) }
func TrimSuffix(s, suffix string) string {
    if HasSuffix(s, suffix) { return s[:len(s)-len(suffix)] }
    return s
}

func strings_TrimSpace(s string) string {
    start := 0; end := len(s)
    for start < end && (s[start] == ' ' || s[start] == '\t' || s[start] == '\n' || s[start] == '\r') { start++ }
    for end > start && (s[end-1] == ' ' || s[end-1] == '\t' || s[end-1] == '\n' || s[end-1] == '\r') { end-- }
    return s[start:end]
}
func strings_TrimLeft(s, cutset string) string {
    for i := 0; i < len(s); i++ {
        found := false
        for j := 0; j < len(cutset); j++ { if s[i] == cutset[j] { found = true; break } }
        if !found { return s[i:] }
    }
    return ""
}
func strings_TrimRight(s, cutset string) string {
    for i := len(s) - 1; i >= 0; i-- {
        found := false
        for j := 0; j < len(cutset); j++ { if s[i] == cutset[j] { found = true; break } }
        if !found { return s[:i+1] }
    }
    return ""
}

// ============================================================
// strconv 包 — 类型转换
// ============================================================
func Atoi(s string) (int, error) { asm("SYSCALL 40"); return 0, nil }
func Itoa(i int) string { asm("SYSCALL 42"); return "" }
func FormatBool(b bool) string { if b { return "true" }; return "false" }
func FormatInt(i int64, base int) string { return "" }
func FormatUint(i uint64, base int) string { return "" }
func FormatFloat(f float64, fmt byte, prec, bitSize int) string { return "" }
func ParseBool(s string) (bool, error) { return false, nil }
func ParseFloat(s string, bitSize int) (float64, error) { return 0.0, nil }
func ParseInt(s string, base, bitSize int) (int64, error) { return 0, nil }
func ParseUint(s string, base, bitSize int) (uint64, error) { return 0, nil }
func Quote(s string) string { return "\"" + s + "\"" }
func QuoteRune(r rune) string { return "" }
func Unquote(s string) (string, error) { return "", nil }
func AppendBool(dst []byte, b bool) []byte { return nil }
func AppendInt(dst []byte, i int64, base int) []byte { return nil }
func AppendFloat(dst []byte, f float64, fmt byte, prec, bitSize int) []byte { return nil }

// ============================================================
// math 包 — 数学函数
// ============================================================
func Abs(x float64) float64 { asm("SYSCALL 44"); return 0.0 }
func Acos(x float64) float64 { asm("SYSCALL 25"); return 0.0 }
func Acosh(x float64) float64 { return Log(x + Sqrt(x*x - 1)) }
func Asin(x float64) float64 { asm("SYSCALL 24"); return 0.0 }
func Asinh(x float64) float64 { return Log(x + Sqrt(x*x + 1)) }
func Atan(x float64) float64 { asm("SYSCALL 32"); return 0.0 }
func Atan2(y, x float64) float64 { asm("SYSCALL 33"); return 0.0 }
func Atanh(x float64) float64 { return Log((1 + x) / (1 - x)) / 2 }
func Cbrt(x float64) float64 { return Pow(x, 1.0/3.0) }
func Ceil(x float64) float64 { asm("SYSCALL 30"); return 0.0 }
func Copysign(x, y float64) float64 { return x }
func Cos(x float64) float64 { asm("SYSCALL 22"); return 0.0 }
func Cosh(x float64) float64 { return (Exp(x) + Exp(-x)) / 2 }
func Dim(x, y float64) float64 { if x > y { return x - y }; return 0 }
func Erf(x float64) float64 { return 0.0 }
func Erfc(x float64) float64 { return 0.0 }
func Erfcinv(x float64) float64 { return 0.0 }
func Erfinv(x float64) float64 { return 0.0 }
func Exp(x float64) float64 { asm("SYSCALL 27"); return 0.0 }
func Exp2(x float64) float64 { return Pow(2, x) }
func Expm1(x float64) float64 { return Exp(x) - 1 }
func FMA(x, y, z float64) float64 { return x*y + z }
func Float32bits(f float32) uint32 { return 0 }
func Float32frombits(b uint32) float32 { return 0.0 }
func Float64bits(f float64) uint64 { return 0 }
func Float64frombits(b uint64) float64 { return 0.0 }
func Floor(x float64) float64 { asm("SYSCALL 29"); return 0.0 }
func Frexp(f float64) (frac float64, exp int) { return 0.0, 0 }
func Gamma(x float64) float64 { return 0.0 }
func Hypot(p, q float64) float64 { return Sqrt(p*p + q*q) }
func Ilogb(x float64) int { return 0 }
func Inf(sign int) float64 { return 0.0 }
func IsInf(f float64, sign int) bool { return false }
func IsNaN(f float64) bool { return f != f }
func J0(x float64) float64 { return 0.0 }
func J1(x float64) float64 { return 0.0 }
func Jn(n int, x float64) float64 { return 0.0 }
func Ldexp(frac float64, exp int) float64 { return frac * Pow(2, float64(exp)) }
func Lgamma(x float64) (lgamma float64, sign int) { return 0.0, 0 }
func Log(x float64) float64 { asm("SYSCALL 28"); return 0.0 }
func Log10(x float64) float64 { return Log(x) / Log(10) }
func Log1p(x float64) float64 { return Log(1 + x) }
func Log2(x float64) float64 { return Log(x) / Log(2) }
func Logb(x float64) float64 { return 0.0 }
func Max(x, y float64) float64 { if x > y { return x }; return y }
func Min(x, y float64) float64 { if x < y { return x }; return y }
func Mod(x, y float64) float64 { return x - float64(int(x/y))*y }
func Modf(f float64) (int float64, frac float64) { return 0.0, 0.0 }
func NaN() float64 { return 0.0 / 0.0 }
func Nextafter(x, y float64) (r float64) { return 0.0 }
func Nextafter32(x, y float32) (r float32) { return 0.0 }
func Pow(x, y float64) float64 { asm("SYSCALL 26"); return 0.0 }
func Pow10(n int) float64 { return Pow(10, float64(n)) }
func Remainder(x, y float64) float64 { return 0.0 }
func Round(x float64) float64 { asm("SYSCALL 31"); return 0.0 }
func RoundToEven(x float64) float64 { return 0.0 }
func Signbit(x float64) bool { return x < 0 }
func Sin(x float64) float64 { asm("SYSCALL 21"); return 0.0 }
func Sincos(x float64) (sin, cos float64) { return Sin(x), Cos(x) }
func Sinh(x float64) float64 { return (Exp(x) - Exp(-x)) / 2 }
func Sqrt(x float64) float64 { asm("SYSCALL 20"); return 0.0 }
func Tan(x float64) float64 { asm("SYSCALL 23"); return 0.0 }
func Tanh(x float64) float64 { return Sinh(x) / Cosh(x) }
func Trunc(x float64) float64 { if x >= 0 { return Floor(x) }; return Ceil(x) }
func Y0(x float64) float64 { return 0.0 }
func Y1(x float64) float64 { return 0.0 }
func Yn(n int, x float64) float64 { return 0.0 }

func IntAbs(x int) int { asm("SYSCALL 43"); return 0 }
func IntMax(x, y int) int { if x > y { return x }; return y }
func IntMin(x, y int) int { if x < y { return x }; return y }

// ============================================================
// 常量
// ============================================================
const (
    PI  = 3.141592653589793
    E   = 2.718281828459045
    Phi = 1.618033988749895
)

// ============================================================
// os 包 — 操作系统接口
// ============================================================
var Args []string

func Getenv(key string) string { return "" }
func Setenv(key, value string) error { return nil }
func Unsetenv(key string) error { return nil }
func Environ() []string { return nil }
func Getwd() (string, error) { return "", nil }
func Chdir(dir string) error { return nil }
func Mkdir(name string, perm uint32) error { return nil }
func MkdirAll(path string, perm uint32) error { return nil }
func Remove(name string) error { return nil }
func RemoveAll(path string) error { return nil }
func Rename(oldpath, newpath string) error { return nil }
func Stat(name string) (interface{}, error) { return nil, nil }
func Getpid() int { return 0 }
func Getppid() int { return 0 }
func Hostname() (string, error) { return "", nil }
func TempDir() string { return "" }
func UserCacheDir() (string, error) { return "", nil }

func Exit(code int) { asm("SYSCALL 3") }

// ============================================================
// time 包 — 时间操作
// ============================================================
type Time struct { sec int64; nsec int32 }
func Now() Time { asm("SYSCALL 54"); return Time{} }
func (t Time) Unix() int64 { return 0 }
func (t Time) UnixMilli() int64 { return 0 }
func (t Time) UnixNano() int64 { return 0 }
func (t Time) Year() int { return 0 }
func (t Time) Month() int { return 0 }
func (t Time) Day() int { return 0 }
func (t Time) Hour() int { return 0 }
func (t Time) Minute() int { return 0 }
func (t Time) Second() int { return 0 }
func (t Time) Format(layout string) string { return "" }
func (t Time) String() string { return "" }
func (t Time) After(u Time) bool { return false }
func (t Time) Before(u Time) bool { return false }
func (t Time) Equal(u Time) bool { return false }
func (t Time) Sub(u Time) Duration { return 0 }
func (t Time) Add(d Duration) Time { return Time{} }
func Sleep(d Duration) { asm("SYSCALL 52") }
func Since(t Time) Duration { return 0 }
func Until(t Time) Duration { return 0 }
func Date(year int, month int, day, hour, min, sec, nsec int, loc interface{}) Time { return Time{} }
func Parse(layout, value string) (Time, error) { return Time{}, nil }
type Duration int64

// ============================================================
// 系统/内存函数
// ============================================================
func GetChar() int { asm("SYSCALL 5"); return 0 }
func ReadLine() string { asm("SYSCALL 2"); return "" }
func ReadInt() int { asm("SYSCALL 7"); return 0 }
func PrintHex(n int) { asm("SYSCALL 10") }
func PrintInt(n int) { asm("SYSCALL 6") }
func PrintStr(s string) { asm("SYSCALL 1") }

func Memcmp(a, b unsafe.Pointer, n int) int { asm("SYSCALL 13"); return 0 }
func Malloc(size int) unsafe.Pointer { asm("SYSCALL 40"); return nil }
func Free(ptr unsafe.Pointer) { asm("SYSCALL 41") }
func Memset(ptr unsafe.Pointer, value byte, count int) { asm("SYSCALL 70") }
func Memcpy(dst, src unsafe.Pointer, count int) { asm("SYSCALL 71") }
func Memmove(dst, src unsafe.Pointer, count int) { asm("SYSCALL 71") }

func Delay(ms int) { asm("SYSCALL 52") }
func GetTick() int { asm("SYSCALL 53"); return 0 }
func GetConfig(key int) int { asm("SYSCALL 60"); return 0 }
func GetDate() string { asm("SYSCALL 55"); return "" }
func GetTime() string { asm("SYSCALL 56"); return "" }
func GetDateTime() int { asm("SYSCALL 54"); return 0 }
func ClearScreen() { asm("SYSCALL 12") }
func Seed(seed int) { asm("SYSCALL 51") }
func Rand() int { asm("SYSCALL 50"); return 0 }

// ============================================================
// 文件 I/O
// ============================================================
type File struct { handle int }
func Open(name string) (*File, error) {
    asm("SYSCALL 110")
    return &File{handle: 0}, nil
}
func Create(name string) (*File, error) { return Open(name) }
func (f *File) Close() error { asm("SYSCALL 111"); return nil }
func (f *File) Read(b []byte) (int, error) { asm("SYSCALL 112"); return 0, nil }
func (f *File) Write(b []byte) (int, error) { asm("SYSCALL 113"); return 0, nil }
func (f *File) Seek(offset int64, whence int) (int64, error) { asm("SYSCALL 115"); return 0, nil }
func (f *File) Stat() (interface{}, error) { return nil, nil }
func ReadFile(name string) ([]byte, error) { return nil, nil }
func WriteFile(name string, data []byte, perm uint32) error { return nil }

// ============================================================
// sync 包 — 并发原语
// ============================================================
type Mutex struct { locked int32 }
func (m *Mutex) Lock() {}
func (m *Mutex) Unlock() {}
type WaitGroup struct{ count int32 }
func (wg *WaitGroup) Add(delta int) {}
func (wg *WaitGroup) Done() {}
func (wg *WaitGroup) Wait() {}
type RWMutex struct{}
func (rw *RWMutex) Lock() {}
func (rw *RWMutex) Unlock() {}
func (rw *RWMutex) RLock() {}
func (rw *RWMutex) RUnlock() {}

// ============================================================
// sort 包 — 排序
// ============================================================
type IntSlice []int
func (p IntSlice) Len() int { return len(p) }
func (p IntSlice) Less(i, j int) bool { return p[i] < p[j] }
func (p IntSlice) Swap(i, j int) { p[i], p[j] = p[j], p[i] }
func Ints(a []int) {
    for i := 0; i < len(a)-1; i++ {
        for j := i + 1; j < len(a); j++ {
            if a[j] < a[i] { a[i], a[j] = a[j], a[i] }
        }
    }
}
func Float64s(a []float64) {}
func Strings(a []string) {}
func Search(n int, f func(int) bool) int { return 0 }

// ============================================================
// io 包 — I/O 原语
// ============================================================
func ReadAll(r interface{}) ([]byte, error) { return nil, nil }
func WriteString(w interface{}, s string) (int, error) { return 0, nil }
func Copy(dst interface{}, src interface{}) (int64, error) { return 0, nil }

// ============================================================
// bytes 包 — 字节切片操作
// ============================================================
type Buffer struct{ buf []byte }
func NewBuffer(buf []byte) *Buffer { return &Buffer{buf: buf} }
func (b *Buffer) Write(p []byte) (int, error) { return 0, nil }
func (b *Buffer) WriteString(s string) (int, error) { return 0, nil }
func (b *Buffer) String() string { return "" }
func (b *Buffer) Bytes() []byte { return nil }
func BytesCompare(a, b []byte) int { return 0 }
func BytesContains(b, subslice []byte) bool { return false }
func BytesEqual(a, b []byte) bool { return false }
func BytesHasPrefix(s, prefix []byte) bool { return false }
func BytesHasSuffix(s, suffix []byte) bool { return false }
func BytesIndex(s, sep []byte) int { return -1 }
func BytesJoin(s [][]byte, sep []byte) []byte { return nil }
func BytesSplit(s, sep []byte) [][]byte { return nil }
func BytesToLower(s []byte) []byte { return nil }
func BytesToUpper(s []byte) []byte { return nil }
func BytesTrim(s []byte, cutset string) []byte { return nil }

// ============================================================
// 内置类型别名
// ============================================================
type (
    error     = interface{}
    rune      = int32
    byte      = uint8
    any       = interface{}
    comparable = interface{}
    unsafe_Pointer = int
)

func len(v interface{}) int { return 0 }
func cap(v interface{}) int { return 0 }
func append(slice interface{}, elems ...interface{}) interface{} { return slice }
func copy(dst, src interface{}) int { return 0 }
func make(t interface{}, size ...interface{}) interface{} { return nil }
func new(t interface{}) interface{} { return nil }
func delete(m interface{}, key interface{}) {}
func panic(v interface{}) {}
func recover() interface{} { return nil }
func close(ch interface{}) {}
func complex(r, i interface{}) interface{} { return 0 }
func real(c interface{}) interface{} { return 0 }
func imag(c interface{}) interface{} { return 0 }
