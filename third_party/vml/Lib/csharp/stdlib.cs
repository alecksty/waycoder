// VML C# 标准库 — 运行时基础支持
// 用法: using stdlib

// === Console 类 — 控制台 I/O ===
static class Console {
    public static void WriteLine(string s) {
        asm("CALL shared_print_str");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void Write(string s) {
        asm("CALL shared_print_str");
    }
    public static void WriteLine(int i) {
        asm("SYSCALL #6");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void Write(int i) {
        asm("SYSCALL #6");
    }
    public static void WriteLine(float f) {
        asm("SYSCALL #59");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void Write(float f) {
        asm("SYSCALL #59");
    }
    public static void WriteLine(char c) {
        asm("SYSCALL #4");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void Write(char c) {
        asm("SYSCALL #4");
    }
    public static void WriteLine(bool b) {
        if (b) WriteLine("True"); else WriteLine("False");
    }
    public static void Write(bool b) {
        if (b) Write("True"); else Write("False");
    }
    public static void WriteLine() {
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static string ReadLine() {
        asm("SYSCALL #2");
        return "";
    }
    public static int ReadInt() {
        asm("SYSCALL #7");
        return 0;
    }
    public static char ReadChar() {
        asm("SYSCALL #5");
        return '\0';
    }
    public static void Clear() {
        asm("SYSCALL #80");
    }
    public static void PrintHex(int n) {
        asm("SYSCALL #10");
    }
}

// === Math 类 ===
static class Math {
    public const float PI = 3.141592653589793f;
    public const float E = 2.718281828459045f;

    public static int Abs(int x) {
        asm("SYSCALL #43");
        return 0;
    }
    public static float Fabs(float x) {
        asm("SYSCALL #44");
        return 0.0f;
    }
    public static int Min(int a, int b) {
        asm("SYSCALL #45");
        return 0;
    }
    public static int Max(int a, int b) {
        asm("SYSCALL #46");
        return 0;
    }
    public static float Fmin(float a, float b) { return a < b ? a : b; }
    public static float Fmax(float a, float b) { return a > b ? a : b; }
    public static float Sqrt(float x) {
        asm("SYSCALL #20");
        return 0.0f;
    }
    public static float Pow(float x, float y) {
        asm("SYSCALL #26");
        return 0.0f;
    }
    public static float Sin(float x) {
        asm("SYSCALL #21");
        return 0.0f;
    }
    public static float Cos(float x) {
        asm("SYSCALL #22");
        return 0.0f;
    }
    public static float Tan(float x) {
        asm("SYSCALL #23");
        return 0.0f;
    }
    public static float Asin(float x) {
        asm("SYSCALL #24");
        return 0.0f;
    }
    public static float Acos(float x) {
        asm("SYSCALL #25");
        return 0.0f;
    }
    public static float Atan(float x) {
        asm("SYSCALL #32");
        return 0.0f;
    }
    public static float Atan2(float y, float x) {
        asm("SYSCALL #33");
        return 0.0f;
    }
    public static float Exp(float x) {
        asm("SYSCALL #27");
        return 0.0f;
    }
    public static float Log(float x) {
        asm("SYSCALL #28");
        return 0.0f;
    }
    public static float Log10(float x) { return Log(x) / Log(10.0f); }
    public static float Ceiling(float x) {
        asm("SYSCALL #30");
        return 0.0f;
    }
    public static float Floor(float x) {
        asm("SYSCALL #29");
        return 0.0f;
    }
    public static int Round(float x) {
        asm("SYSCALL #31");
        return 0;
    }
    public static int Truncate(float x) {
        return (int)(x >= 0 ? Floor(x) : Ceiling(x));
    }
    public static int Sign(int x) {
        if (x > 0) return 1;
        if (x < 0) return -1;
        return 0;
    }
    public static float Hypot(float x, float y) {
        return Sqrt(x * x + y * y);
    }
    public static float DegToRad(float deg) { return deg * PI / 180.0f; }
    public static float RadToDeg(float rad) { return rad * 180.0f / PI; }
}

// === StringUtil 类 — 字符串操作 ===
static class StringUtil {
    public static int Length(string s) {
        asm("SYSCALL #60");
        return 0;
    }
    public static char CharAt(string s, int index) {
        return s[index];
    }
    public static string Substring(string s, int start, int length) {
        string result = "";
        int i = start;
        int end = start + length;
        while (i < end) {
            result = result + s[i];
            i = i + 1;
        }
        return result;
    }
    public static string Concat(string a, string b) {
        asm("SYSCALL #63");
        return "";
    }
    public static int IndexOf(string s, string value) {
        int n = Length(s);
        int m = Length(value);
        if (m == 0) return 0;
        int i = 0;
        while (i <= n - m) {
            int j = 0;
            while (j < m && s[i + j] == value[j]) j = j + 1;
            if (j == m) return i;
            i = i + 1;
        }
        return -1;
    }
    public static int LastIndexOf(string s, string value) {
        int n = Length(s);
        int m = Length(value);
        if (m == 0) return n;
        int i = n - m;
        while (i >= 0) {
            int j = 0;
            while (j < m && s[i + j] == value[j]) j = j + 1;
            if (j == m) return i;
            i = i - 1;
        }
        return -1;
    }
    public static bool Contains(string s, string value) {
        return IndexOf(s, value) >= 0;
    }
    public static bool StartsWith(string s, string value) {
        int m = Length(value);
        if (m > Length(s)) return false;
        int i = 0;
        while (i < m) { if (s[i] != value[i]) return false; i = i + 1; }
        return true;
    }
    public static bool EndsWith(string s, string value) {
        int m = Length(value);
        int n = Length(s);
        if (m > n) return false;
        int i = 0;
        while (i < m) { if (s[n - m + i] != value[i]) return false; i = i + 1; }
        return true;
    }
    public static string Replace(string s, string oldValue, string newValue) {
        string result = "";
        int n = Length(s);
        int m = Length(oldValue);
        int i = 0;
        while (i < n) {
            if (i <= n - m && Substring(s, i, m) == oldValue) {
                result = result + newValue;
                i = i + m;
            } else {
                result = result + s[i];
                i = i + 1;
            }
        }
        return result;
    }
    public static string[] Split(string s, char separator) {
        int n = Length(s);
        // count segments
        int count = 1;
        int i = 0;
        while (i < n) { if (s[i] == separator) count = count + 1; i = i + 1; }
        string[] result = new string[count];
        int idx = 0;
        int start = 0;
        i = 0;
        while (i < n) {
            if (s[i] == separator) {
                result[idx] = Substring(s, start, i - start);
                idx = idx + 1;
                start = i + 1;
            }
            i = i + 1;
        }
        result[idx] = Substring(s, start, n - start);
        return result;
    }
    public static string Join(string separator, string[] values) {
        string result = "";
        int i = 0;
        while (i < values.Length) {
            if (i > 0) result = result + separator;
            result = result + values[i];
            i = i + 1;
        }
        return result;
    }
    public static string ToUpper(string s) {
        string result = "";
        int i = 0;
        while (i < Length(s)) {
            char c = s[i];
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);
            result = result + c;
            i = i + 1;
        }
        return result;
    }
    public static string ToLower(string s) {
        string result = "";
        int i = 0;
        while (i < Length(s)) {
            char c = s[i];
            if (c >= 'A' && c <= 'Z') c = (char)(c + 32);
            result = result + c;
            i = i + 1;
        }
        return result;
    }
    public static string Trim(string s) {
        int n = Length(s);
        int start = 0;
        int end = n - 1;
        while (start < n && (s[start] == ' ' || s[start] == '\t' || s[start] == '\n')) start = start + 1;
        while (end >= start && (s[end] == ' ' || s[end] == '\t' || s[end] == '\n')) end = end - 1;
        return Substring(s, start, end - start + 1);
    }
    public static string TrimStart(string s) {
        int n = Length(s);
        int start = 0;
        while (start < n && (s[start] == ' ' || s[start] == '\t')) start = start + 1;
        return Substring(s, start, n - start);
    }
    public static string TrimEnd(string s) {
        int n = Length(s);
        int end = n - 1;
        while (end >= 0 && (s[end] == ' ' || s[end] == '\t')) end = end - 1;
        return Substring(s, 0, end + 1);
    }
    public static int CompareTo(string a, string b) {
        asm("SYSCALL #62");
        return 0;
    }
    public static string ValueOf(int i) {
        asm("SYSCALL #42");
        return "";
    }
    public static string ValueOf(float f) {
        asm("SYSCALL #58");
        return "";
    }
    public static string ValueOf(bool b) {
        return b ? "True" : "False";
    }
    public static string Repeat(string s, int count) {
        string result = "";
        int i = 0;
        while (i < count) {
            result = result + s;
            i = i + 1;
        }
        return result;
    }
    public static string Reverse(string s) {
        string result = "";
        int i = Length(s) - 1;
        while (i >= 0) {
            result = result + s[i];
            i = i - 1;
        }
        return result;
    }
    public static bool IsNullOrEmpty(string s) {
        return s == null || Length(s) == 0;
    }
    public static string PadLeft(string s, int totalWidth, char paddingChar) {
        int pad = totalWidth - Length(s);
        if (pad <= 0) return s;
        string result = "";
        int i = 0; while (i < pad) { result = result + paddingChar; i = i + 1; }
        return result + s;
    }
    public static string PadRight(string s, int totalWidth, char paddingChar) {
        int pad = totalWidth - Length(s);
        if (pad <= 0) return s;
        string result = s;
        int i = 0; while (i < pad) { result = result + paddingChar; i = i + 1; }
        return result;
    }
}

// === ArrayUtil 类 — 数组操作 ===
static class ArrayUtil {
    public static void Sort(int[] array) {
        int n = array.Length;
        int i = 0;
        while (i < n - 1) {
            int j = i + 1;
            while (j < n) {
                if (array[j] < array[i]) {
                    int tmp = array[i];
                    array[i] = array[j];
                    array[j] = tmp;
                }
                j = j + 1;
            }
            i = i + 1;
        }
    }
    public static void Reverse(int[] array) {
        int n = array.Length;
        int i = 0;
        while (i < n / 2) {
            int tmp = array[i];
            array[i] = array[n - 1 - i];
            array[n - 1 - i] = tmp;
            i = i + 1;
        }
    }
    public static int IndexOf(int[] array, int value) {
        int i = 0;
        while (i < array.Length) {
            if (array[i] == value) return i;
            i = i + 1;
        }
        return -1;
    }
    public static int BinarySearch(int[] array, int value) {
        int lo = 0;
        int hi = array.Length - 1;
        while (lo <= hi) {
            int mid = (lo + hi) / 2;
            if (array[mid] < value) lo = mid + 1;
            else if (array[mid] > value) hi = mid - 1;
            else return mid;
        }
        return -1;
    }
    public static void Fill(int[] array, int value) {
        int i = 0;
        while (i < array.Length) { array[i] = value; i = i + 1; }
    }
    public static int[] Copy(int[] array, int length) {
        int[] result = new int[length];
        int i = 0;
        while (i < length && i < array.Length) { result[i] = array[i]; i = i + 1; }
        return result;
    }
    public static int[] Concat(int[] a, int[] b) {
        int[] result = new int[a.Length + b.Length];
        int i = 0;
        while (i < a.Length) { result[i] = a[i]; i = i + 1; }
        i = 0;
        while (i < b.Length) { result[a.Length + i] = b[i]; i = i + 1; }
        return result;
    }
}

// === Convert 类 — 类型转换 ===
static class Convert {
    public static int ToInt32(string s) {
        asm("SYSCALL #40");
        return 0;
    }
    public static float ToFloat(string s) {
        asm("SYSCALL #41");
        return 0.0f;
    }
    public static string ToString(int n) {
        asm("SYSCALL #42");
        return "";
    }
    public static string ToString(float f) {
        asm("SYSCALL #58");
        return "";
    }
    public static string ToHexString(int n) {
        asm("SYSCALL #10");
        return "";
    }
}

// === Random 类 ===
static class Random {
    private int _seed;

    public Random() {
        asm("SYSCALL #53");
        _seed = 0;
    }
    public Random(int seed) {
        _seed = seed;
        asm("SYSCALL #51");
    }
    public int Next() {
        asm("SYSCALL #50");
        return 0;
    }
    public int Next(int maxValue) {
        return Next() % maxValue;
    }
    public int Next(int minValue, int maxValue) {
        return minValue + Next() % (maxValue - minValue);
    }
    public float NextFloat() {
        return (float)Next() / 2147483647.0f;
    }
}

// === Memory 类 — 内存管理 ===
static class Memory {
    public static int Memcmp(byte[] a, byte[] b, int n) {
        asm("SYSCALL #13");
        return 0;
    }
    public static int Alloc(int size) {
        asm("SYSCALL #40");
        return 0;
    }
    public static void Free(int ptr) {
        asm("SYSCALL #41");
    }
    public static void Memset(int ptr, int value, int count) {
        asm("SYSCALL #70");
    }
    public static void Memcpy(int dest, int src, int count) {
        asm("SYSCALL #71");
    }
}

// === Environment 类 — 系统函数 ===
static class Environment {
    public static void Exit(int code) {
        asm("SYSCALL 3");
    }
    public static int TickCount {
        get {
            asm("SYSCALL #53");
            return 0;
        }
    }
    public static long DateTime {
        get {
            asm("SYSCALL #54");
            return 0;
        }
    }
    public static string GetDate() {
        asm("SYSCALL #55");
        return "";
    }
    public static string GetTime() {
        asm("SYSCALL #56");
        return "";
    }
    public static void Delay(int ms) {
        asm("SYSCALL #52");
    }
    public static int GetConfig(int key) {
        asm("SYSCALL #60");
        return 0;
    }
}

// === File 类 — 文件操作 (委托给共享库) ===
static class File {
    public static int Open(string path, int mode) {
        asm("CALL shared_fopen");
        return -1;
    }
    public static int Close(int handle) {
        asm("CALL shared_fclose");
        return -1;
    }
    public static int Read(int handle, byte[] buf, int count) {
        asm("CALL shared_fread");
        return -1;
    }
    public static int Write(int handle, byte[] data, int count) {
        asm("CALL shared_fwrite");
        return -1;
    }
    public static int Seek(int handle, int offset, int whence) {
        asm("CALL shared_fseek");
        return -1;
    }
    public static int Tell(int handle) {
        asm("CALL shared_ftell");
        return -1;
    }
    public static int Size(int handle) {
        asm("CALL shared_fsize");
        return -1;
    }
    public static int Truncate(int handle, int size) {
        asm("CALL shared_ftruncate");
        return -1;
    }
    public static string ReadAllText(string path) {
        int h = Open(path, 0);
        if (h < 0) return "";
        int sz = Size(h);
        byte[] buf = new byte[sz + 1];
        Read(h, buf, sz);
        buf[sz] = 0;
        Close(h);
        // return as string
        return "";
    }
    public static int WriteAllText(string path, string content) {
        int h = Open(path, 1);
        if (h < 0) return -1;
        int written = Write(h, null, 0);
        Close(h);
        return written;
    }
}

// === Char 类 — 字符分类 (纯 C# 实现) ===
static class Char {
    public static bool IsAlpha(char c) {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }
    public static bool IsDigit(char c) {
        return c >= '0' && c <= '9';
    }
    public static bool IsLetterOrDigit(char c) {
        return IsAlpha(c) || IsDigit(c);
    }
    public static bool IsWhiteSpace(char c) {
        return c == ' ' || c == '\t' || c == '\n' || c == '\r';
    }
    public static bool IsUpper(char c) {
        return c >= 'A' && c <= 'Z';
    }
    public static bool IsLower(char c) {
        return c >= 'a' && c <= 'z';
    }
    public static char ToUpper(char c) {
        if (c >= 'a' && c <= 'z') return (char)(c - 32);
        return c;
    }
    public static char ToLower(char c) {
        if (c >= 'A' && c <= 'Z') return (char)(c + 32);
        return c;
    }
    public static bool IsControl(char c) {
        return (c >= 0 && c <= 31) || c == 127;
    }
    public static bool IsPunctuation(char c) {
        return (c >= '!' && c <= '/') || (c >= ':' && c <= '@') ||
               (c >= '[' && c <= '`') || (c >= '{' && c <= '~');
    }
}

// === Keyboard 类 — 键盘输入 ===
static class Keyboard {
    public static bool KeyAvailable() {
        asm("CALL shared_kb_hit");
        return false;
    }
    public static string ReadLine() {
        asm("CALL shared_read_line");
        return "";
    }
}
