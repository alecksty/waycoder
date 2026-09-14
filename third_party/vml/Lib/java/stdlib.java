// VML Java 标准库 — 运行时基础支持
// 用法: import stdlib

// === System 类 — 控制台 I/O ===
public class System {
    public static void out_println(String s) {
        asm("CALL shared_print_str");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void out_print(String s) {
        asm("CALL shared_print_str");
    }
    public static void out_println(int i) {
        asm("SYSCALL #6");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void out_print(int i) {
        asm("SYSCALL #6");
    }
    public static void out_println(float f) {
        asm("SYSCALL #59");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void out_print(float f) {
        asm("SYSCALL #59");
    }
    public static void out_println(char c) {
        asm("SYSCALL #4");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static void out_print(char c) {
        asm("SYSCALL #4");
    }
    public static void out_println(boolean b) {
        if (b) out_println("true"); else out_println("false");
    }
    public static void out_print(boolean b) {
        if (b) out_print("true"); else out_print("false");
    }
    public static void out_println() {
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    }
    public static String console_readLine() {
        asm("SYSCALL #2");
        return "";
    }
    public static int console_readInt() {
        asm("SYSCALL #7");
        return 0;
    }
    public static char console_readChar() {
        asm("SYSCALL #5");
        return '\0';
    }
    public static void exit(int code) {
        asm("SYSCALL 3");
    }
    public static long currentTimeMillis() {
        asm("SYSCALL #54");
        return 0;
    }
    public static void arraycopy(Object src, int srcPos, Object dest, int destPos, int length) {
        int i = 0;
        while (i < length) {
            dest[destPos + i] = src[srcPos + i];
            i = i + 1;
        }
    }
}

// === Memory 类 — 内存管理 ===
public class Memory {
    public static int memcmp(Object a, Object b, int n) {
        asm("SYSCALL #13");
        return 0;
    }
    public static Object alloc(int size) {
        asm("SYSCALL #40");
        return null;
    }
    public static void free(Object ptr) {
        asm("SYSCALL #41");
    }
    public static void memset(Object ptr, int value, int count) {
        asm("SYSCALL #70");
    }
    public static void memcpy(Object dest, Object src, int count) {
        asm("SYSCALL #71");
    }
}

// === Math 类 ===
public class Math {
    public static final float PI = 3.141592653589793f;
    public static final float E = 2.718281828459045f;

    public static int abs(int x) {
        asm("SYSCALL #43");
        return 0;
    }
    public static float fabs(float x) {
        asm("SYSCALL #44");
        return 0.0f;
    }
    public static int min(int a, int b) {
        if (a < b) return a; else return b;
    }
    public static int max(int a, int b) {
        if (a > b) return a; else return b;
    }
    public static float fmin(float a, float b) {
        if (a < b) return a; else return b;
    }
    public static float fmax(float a, float b) {
        if (a > b) return a; else return b;
    }
    public static float sqrt(float x) {
        asm("SYSCALL #20");
        return 0.0f;
    }
    public static float pow(float x, float y) {
        asm("SYSCALL #26");
        return 0.0f;
    }
    public static float sin(float x) {
        asm("SYSCALL #21");
        return 0.0f;
    }
    public static float cos(float x) {
        asm("SYSCALL #22");
        return 0.0f;
    }
    public static float tan(float x) {
        asm("SYSCALL #23");
        return 0.0f;
    }
    public static float asin(float x) {
        asm("SYSCALL #24");
        return 0.0f;
    }
    public static float acos(float x) {
        asm("SYSCALL #25");
        return 0.0f;
    }
    public static float atan(float x) {
        asm("SYSCALL #32");
        return 0.0f;
    }
    public static float atan2(float y, float x) {
        asm("SYSCALL #33");
        return 0.0f;
    }
    public static float exp(float x) {
        asm("SYSCALL #27");
        return 0.0f;
    }
    public static float log(float x) {
        asm("SYSCALL #28");
        return 0.0f;
    }
    public static float log10(float x) { return log(x) / log(10.0f); }
    public static float ceil(float x) {
        asm("SYSCALL #30");
        return 0.0f;
    }
    public static float floor(float x) {
        asm("SYSCALL #29");
        return 0.0f;
    }
    public static int round(float x) {
        asm("SYSCALL #31");
        return 0;
    }
    public static int sign(int x) {
        if (x > 0) return 1;
        if (x < 0) return -1;
        return 0;
    }
    public static float hypot(float x, float y) {
        return sqrt(x * x + y * y);
    }
    public static float toDegrees(float rad) { return rad * 180.0f / PI; }
    public static float toRadians(float deg) { return deg * PI / 180.0f; }
}

// === Random 类 ===
public class Random {
    private long seed;

    public Random() {
        asm("SYSCALL #53");
        seed = 0;
    }
    public Random(long seed) {
        this.seed = seed;
        asm("SYSCALL #51");
    }
    public int nextInt() {
        asm("SYSCALL #50");
        return 0;
    }
    public int nextInt(int bound) {
        return nextInt() % bound;
    }
    public int nextInt(int min, int max) {
        return min + nextInt() % (max - min);
    }
    public float nextFloat() {
        return (float)nextInt() / 2147483647.0f;
    }
    public boolean nextBoolean() {
        return nextInt() % 2 == 0;
    }
}

// === StringUtil 类 — 字符串操作 ===
public class StringUtil {
    public static int length(String s) {
        asm("SYSCALL #60");
        return 0;
    }
    public static char charAt(String s, int index) {
        if (index < 0 || index >= length(s)) return '\0';
        return s[index];
    }
    public static String substring(String s, int start, int length) {
        String result = "";
        int end = start + length;
        int i = start;
        while (i < end) {
            result = result + s[i];
            i = i + 1;
        }
        return result;
    }
    public static String substring(String s, int start) {
        return substring(s, start, length(s) - start);
    }
    public static String concat(String a, String b) {
        asm("SYSCALL #63");
        return "";
    }
    public static int indexOf(String s, String value) {
        int n = length(s);
        int m = length(value);
        if (m == 0) return 0;
        if (m > n) return -1;
        int i = 0;
        while (i <= n - m) {
            int j = 0;
            while (j < m && s[i + j] == value[j]) j = j + 1;
            if (j == m) return i;
            i = i + 1;
        }
        return -1;
    }
    public static int lastIndexOf(String s, String value) {
        int n = length(s);
        int m = length(value);
        if (m == 0) return n;
        if (m > n) return -1;
        int i = n - m;
        while (i >= 0) {
            int j = 0;
            while (j < m && s[i + j] == value[j]) j = j + 1;
            if (j == m) return i;
            i = i - 1;
        }
        return -1;
    }
    public static boolean contains(String s, String value) {
        return indexOf(s, value) >= 0;
    }
    public static boolean startsWith(String s, String value) {
        int m = length(value);
        if (m > length(s)) return false;
        int i = 0;
        while (i < m) { if (s[i] != value[i]) return false; i = i + 1; }
        return true;
    }
    public static boolean endsWith(String s, String value) {
        int m = length(value);
        int n = length(s);
        if (m > n) return false;
        int i = 0;
        while (i < m) { if (s[n - m + i] != value[i]) return false; i = i + 1; }
        return true;
    }
    public static boolean isEmpty(String s) {
        return length(s) == 0;
    }
    public static String replace(String s, String oldValue, String newValue) {
        String result = "";
        int n = length(s);
        int m = length(oldValue);
        if (m == 0) return s;
        int i = 0;
        while (i < n) {
            if (i <= n - m && substring(s, i, m).equals(oldValue)) {
                result = result + newValue;
                i = i + m;
            } else {
                result = result + s[i];
                i = i + 1;
            }
        }
        return result;
    }
    public static String[] split(String s, char separator) {
        int n = length(s);
        // count segments
        int count = 1;
        int i = 0;
        while (i < n) { if (s[i] == separator) count = count + 1; i = i + 1; }
        String[] result = new String[count];
        int idx = 0;
        int start = 0;
        i = 0;
        while (i < n) {
            if (s[i] == separator) {
                result[idx] = substring(s, start, i - start);
                idx = idx + 1;
                start = i + 1;
            }
            i = i + 1;
        }
        result[idx] = substring(s, start, n - start);
        return result;
    }
    public static String join(String separator, String[] values) {
        String result = "";
        int i = 0;
        while (i < values.length) {
            if (i > 0) result = result + separator;
            result = result + values[i];
            i = i + 1;
        }
        return result;
    }
    public static String toUpper(String s) {
        String result = "";
        int i = 0;
        while (i < length(s)) {
            char c = s[i];
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);
            result = result + c;
            i = i + 1;
        }
        return result;
    }
    public static String toLower(String s) {
        String result = "";
        int i = 0;
        while (i < length(s)) {
            char c = s[i];
            if (c >= 'A' && c <= 'Z') c = (char)(c + 32);
            result = result + c;
            i = i + 1;
        }
        return result;
    }
    public static String trim(String s) {
        int n = length(s);
        int start = 0;
        int end = n - 1;
        while (start < n && (s[start] == ' ' || s[start] == '\t' || s[start] == '\n' || s[start] == '\r')) {
            start = start + 1;
        }
        while (end >= start && (s[end] == ' ' || s[end] == '\t' || s[end] == '\n' || s[end] == '\r')) {
            end = end - 1;
        }
        return substring(s, start, end - start + 1);
    }
    public static int compareTo(String a, String b) {
        asm("SYSCALL #62");
        return 0;
    }
    public static boolean equals(String a, String b) {
        return compareTo(a, b) == 0;
    }
    public static String valueOf(int i) {
        asm("SYSCALL #42");
        return "";
    }
    public static String valueOf(float f) {
        asm("SYSCALL #58");
        return "";
    }
    public static String valueOf(boolean b) {
        return b ? "true" : "false";
    }
    public static String valueOf(char c) {
        return "" + c;
    }
    public static String repeat(String s, int count) {
        String result = "";
        int i = 0;
        while (i < count) {
            result = result + s;
            i = i + 1;
        }
        return result;
    }
    public static String reverse(String s) {
        String result = "";
        int i = length(s) - 1;
        while (i >= 0) {
            result = result + s[i];
            i = i - 1;
        }
        return result;
    }
}

// === ArraysUtil 类 — 数组工具 ===
public class ArraysUtil {
    public static void sort(int[] array) {
        int n = array.length;
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
    public static void sort(float[] array) {
        int n = array.length;
        int i = 0;
        while (i < n - 1) {
            int j = i + 1;
            while (j < n) {
                if (array[j] < array[i]) {
                    float tmp = array[i];
                    array[i] = array[j];
                    array[j] = tmp;
                }
                j = j + 1;
            }
            i = i + 1;
        }
    }
    public static int binarySearch(int[] array, int key) {
        int lo = 0;
        int hi = array.length - 1;
        while (lo <= hi) {
            int mid = (lo + hi) / 2;
            if (array[mid] < key) lo = mid + 1;
            else if (array[mid] > key) hi = mid - 1;
            else return mid;
        }
        return -(lo + 1);
    }
    public static void fill(int[] array, int value) {
        int i = 0;
        while (i < array.length) { array[i] = value; i = i + 1; }
    }
    public static int[] copyOf(int[] array, int newLength) {
        int[] result = new int[newLength];
        int i = 0;
        while (i < newLength && i < array.length) {
            result[i] = array[i];
            i = i + 1;
        }
        return result;
    }
    public static int[] copyOfRange(int[] array, int from, int to) {
        int[] result = new int[to - from];
        int i = from;
        while (i < to) {
            result[i - from] = array[i];
            i = i + 1;
        }
        return result;
    }
    public static boolean equals(int[] a, int[] b) {
        if (a.length != b.length) return false;
        int i = 0;
        while (i < a.length) {
            if (a[i] != b[i]) return false;
            i = i + 1;
        }
        return true;
    }
    public static String toString(int[] array) {
        String result = "[";
        int i = 0;
        while (i < array.length) {
            if (i > 0) result = result + ", ";
            result = result + StringUtil.valueOf(array[i]);
            i = i + 1;
        }
        return result + "]";
    }
}

// === StringBuilder 类 ===
public class StringBuilder {
    private char[] data;
    private int len;
    private int cap;

    public StringBuilder() {
        cap = 16;
        data = new char[cap];
        len = 0;
    }
    public StringBuilder(int capacity) {
        cap = capacity;
        if (cap < 1) cap = 16;
        data = new char[cap];
        len = 0;
    }
    public StringBuilder(String s) {
        len = StringUtil.length(s);
        cap = len + 16;
        data = new char[cap];
        int i = 0;
        while (i < len) { data[i] = s[i]; i = i + 1; }
    }
    private void ensureCapacity(int needed) {
        if (needed > cap) {
            int newCap = cap * 2;
            if (newCap < needed) newCap = needed;
            char[] newData = new char[newCap];
            int i = 0;
            while (i < len) { newData[i] = data[i]; i = i + 1; }
            data = newData;
            cap = newCap;
        }
    }
    public StringBuilder append(String s) {
        int slen = StringUtil.length(s);
        ensureCapacity(len + slen);
        int i = 0;
        while (i < slen) { data[len + i] = s[i]; i = i + 1; }
        len = len + slen;
        return this;
    }
    public StringBuilder append(int i) {
        return append(StringUtil.valueOf(i));
    }
    public StringBuilder append(float f) {
        return append(StringUtil.valueOf(f));
    }
    public StringBuilder append(char c) {
        ensureCapacity(len + 1);
        data[len] = c;
        len = len + 1;
        return this;
    }
    public StringBuilder append(boolean b) {
        return append(StringUtil.valueOf(b));
    }
    public StringBuilder insert(int offset, String s) {
        int slen = StringUtil.length(s);
        ensureCapacity(len + slen);
        int i = len - 1;
        while (i >= offset) { data[i + slen] = data[i]; i = i - 1; }
        i = 0;
        while (i < slen) { data[offset + i] = s[i]; i = i + 1; }
        len = len + slen;
        return this;
    }
    public StringBuilder delete(int start, int end) {
        int count = end - start;
        int i = end;
        while (i < len) { data[i - count] = data[i]; i = i + 1; }
        len = len - count;
        return this;
    }
    public StringBuilder reverse() {
        int i = 0;
        while (i < len / 2) {
            char tmp = data[i];
            data[i] = data[len - 1 - i];
            data[len - 1 - i] = tmp;
            i = i + 1;
        }
        return this;
    }
    public int length() { return len; }
    public int capacity() { return cap; }
    public char charAt(int index) { return data[index]; }
    public void setCharAt(int index, char c) { data[index] = c; }

    public String toString() {
        String result = "";
        int i = 0;
        while (i < len) { result = result + data[i]; i = i + 1; }
        return result;
    }
}

// === Iterator 接口 ===
public interface Iterator {
    boolean hasNext();
    Object next();
}

// === Iterable 接口 ===
public interface Iterable {
    Iterator iterator();
}

// === CollectionsUtil 类 ===
public class CollectionsUtil {
    public static void sort(int[] list) {
        ArraysUtil.sort(list);
    }
    public static void reverse(int[] list) {
        int n = list.length;
        int i = 0;
        while (i < n / 2) {
            int tmp = list[i];
            list[i] = list[n - 1 - i];
            list[n - 1 - i] = tmp;
            i = i + 1;
        }
    }
    public static int min(int[] list) {
        int m = list[0];
        int i = 1;
        while (i < list.length) {
            if (list[i] < m) m = list[i];
            i = i + 1;
        }
        return m;
    }
    public static int max(int[] list) {
        int m = list[0];
        int i = 1;
        while (i < list.length) {
            if (list[i] > m) m = list[i];
            i = i + 1;
        }
        return m;
    }
    public static int sum(int[] list) {
        int s = 0;
        int i = 0;
        while (i < list.length) { s = s + list[i]; i = i + 1; }
        return s;
    }
    public static void fill(int[] list, int value) {
        ArraysUtil.fill(list, value);
    }
    public static int indexOf(int[] list, int value) {
        int i = 0;
        while (i < list.length) {
            if (list[i] == value) return i;
            i = i + 1;
        }
        return -1;
    }
    public static int lastIndexOf(int[] list, int value) {
        int i = list.length - 1;
        while (i >= 0) {
            if (list[i] == value) return i;
            i = i - 1;
        }
        return -1;
    }
}

// === Optional 类 ===
public class Optional {
    private Object value;
    private boolean present;

    private Optional(Object value, boolean present) {
        this.value = value;
        this.present = present;
    }
    public static Optional of(Object value) {
        return new Optional(value, true);
    }
    public static Optional empty() {
        return new Optional(null, false);
    }
    public boolean isPresent() { return present; }
    public boolean isEmpty() { return !present; }
    public Object get() { return value; }
    public Object orElse(Object other) {
        return present ? value : other;
    }
}

// === Character 类 — 字符分类 (纯 Java 实现) ===
public class Character {
    public static boolean isLetter(char c) {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }
    public static boolean isDigit(char c) {
        return c >= '0' && c <= '9';
    }
    public static boolean isLetterOrDigit(char c) {
        return isLetter(c) || isDigit(c);
    }
    public static boolean isWhitespace(char c) {
        return c == ' ' || c == '\t' || c == '\n' || c == '\r';
    }
    public static boolean isUpperCase(char c) {
        return c >= 'A' && c <= 'Z';
    }
    public static boolean isLowerCase(char c) {
        return c >= 'a' && c <= 'z';
    }
    public static char toUpperCase(char c) {
        if (c >= 'a' && c <= 'z') return (char)(c - 32);
        return c;
    }
    public static char toLowerCase(char c) {
        if (c >= 'A' && c <= 'Z') return (char)(c + 32);
        return c;
    }
    public static boolean isISOControl(char c) {
        return (c >= 0 && c <= 31) || c == 127;
    }
}

// === Integer 扩展 — toString ===
public class IntegerUtil {
    public static String toString(int i) {
        asm("CALL shared_int_to_str");
        return "";
    }
}
