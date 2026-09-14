#param lib("bitlib")
#param lib("builtins")
#param lib("util")

// VML Shared Array Library
// 通用数组操作 — MCU兼容，无GC
// 数组格式: [length (int), elem0, elem1, ...] 每个元素4字节

// len: 获取数组长度
__stdcall int len(int* arr) {
    if (arr == 0) return 0;
    return arr[0];
}

// get: 获取数组元素 arr[index], 越界返回0
__stdcall int get(int* arr, int index) {
    if (arr == 0 || index < 0 || index >= arr[0]) return 0;
    return arr[index + 1];
}

// set: 设置数组元素 arr[index] = value, 越界无操作
__stdcall void set(int* arr, int index, int value) {
    if (arr == 0 || index < 0 || index >= arr[0]) return;
    arr[index + 1] = value;
}

// sort_bubble: 冒泡排序 (MCU友好, O(n^2))
__stdcall void sort_bubble(int* arr) {
    if (arr == 0) return;
    int n = arr[0];
    int i, j;
    for (i = 0; i < n - 1; i++) {
        for (j = 0; j < n - 1 - i; j++) {
            int a = arr[j + 2];  // arr[0]=len, arr[1]=elem0, arr[2]=elem1
            int b = arr[j + 3];
            if (a > b) {
                arr[j + 2] = b;
                arr[j + 3] = a;
            }
        }
    }
}

// indexof: 查找元素首次出现位置，返回索引或 -1
__stdcall int indexof(int* arr, int value) {
    if (arr == 0) return -1;
    int i;
    for (i = 0; i < arr[0]; i++) {
        if (arr[i + 1] == value) return i;
    }
    return -1;
}

// last_indexof: 查找元素最后出现位置
__stdcall int last_indexof(int* arr, int value) {
    if (arr == 0) return -1;
    int i;
    int result = -1;
    for (i = 0; i < arr[0]; i++) {
        if (arr[i + 1] == value) result = i;
    }
    return result;
}

// contains: 数组是否包含元素 (返回 1/0)
__stdcall int contains(int* arr, int value) {
    return indexof(arr, value) >= 0 ? 1 : 0;
}

// reverse: 原地反转数组
__stdcall void reverse(int* arr) {
    if (arr == 0) return;
    int n = arr[0];
    int i;
    for (i = 0; i < n / 2; i++) {
        int tmp = arr[i + 1];
        arr[i + 1] = arr[n - i];
        arr[n - i] = tmp;
    }
}

// fill: 用 value 填充整个数组
__stdcall void fill(int* arr, int value) {
    if (arr == 0) return;
    int i;
    for (i = 0; i < arr[0]; i++) {
        arr[i + 1] = value;
    }
}

// copy: 复制数组（浅拷贝）, 返回新数组指针（调用者分配内存）
// dst 必须已分配 arr[0]+1 个 int 空间
__stdcall void copy(int* src, int* dst) {
    if (src == 0 || dst == 0) return;
    int i;
    int n = src[0];
    dst[0] = n;
    for (i = 0; i < n; i++) dst[i + 1] = src[i + 1];
}

// slice: 切片 arr[start..start+count-1] → dst
__stdcall void slice(int* src, int start, int count, int* dst) {
    if (src == 0 || dst == 0) return;
    int n = src[0];
    if (start < 0) start = 0;
    if (start >= n) { dst[0] = 0; return; }
    if (start + count > n) count = n - start;
    dst[0] = count;
    int i;
    for (i = 0; i < count; i++) dst[i + 1] = src[start + i + 1];
}

// concat: 拼接 a + b → dst (调用者分配 dst)
__stdcall void concat(int* a, int* b, int* dst) {
    if (a == 0 || b == 0 || dst == 0) return;
    int na = a[0], nb = b[0];
    dst[0] = na + nb;
    int i;
    for (i = 0; i < na; i++) dst[i + 1] = a[i + 1];
    for (i = 0; i < nb; i++) dst[na + i + 1] = b[i + 1];
}

// push: 末尾添加元素（调用者确保有空间）
__stdcall void push(int* arr, int value) {
    if (arr == 0) return;
    int n = arr[0];
    arr[n + 1] = value;
    arr[0] = n + 1;
}

// pop: 弹出末尾元素，返回该值；空数组返回0
__stdcall int pop(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int n = arr[0];
    int val = arr[n];
    arr[0] = n - 1;
    return val;
}

// shift: 弹出首元素，返回该值；空数组返回0
__stdcall int shift(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int n = arr[0];
    int val = arr[1];
    int i;
    for (i = 1; i < n; i++) arr[i] = arr[i + 1];
    arr[0] = n - 1;
    return val;
}

// unshift: 首部插入元素（调用者确保有空间）
__stdcall void unshift(int* arr, int value) {
    if (arr == 0) return;
    int n = arr[0];
    int i;
    for (i = n; i > 0; i--) arr[i + 1] = arr[i];
    arr[1] = value;
    arr[0] = n + 1;
}

// insert: 在 index 位置插入元素
__stdcall void insert(int* arr, int index, int value) {
    if (arr == 0) return;
    int n = arr[0];
    if (index < 0) index = 0;
    if (index > n) index = n;
    int i;
    for (i = n; i >= index; i--) arr[i + 2] = arr[i + 1];
    arr[index + 1] = value;
    arr[0] = n + 1;
}

// remove: 删除 index 位置的元素，返回被删元素值
__stdcall int remove(int* arr, int index) {
    if (arr == 0 || index < 0 || index >= arr[0]) return 0;
    int n = arr[0];
    int val = arr[index + 1];
    int i;
    for (i = index; i < n - 1; i++) arr[i + 1] = arr[i + 2];
    arr[0] = n - 1;
    return val;
}

// join_str: 将数组元素用分隔符连接成字符串写入 dst
// dst 必须由调用者分配足够空间
__stdcall void join_str(int* arr, int delim, char* dst) {
    if (arr == 0 || dst == 0) return;
    int n = arr[0];
    int i;
    char* p = dst;
    for (i = 0; i < n; i++) {
        // 简单整数→字符串转换
        int val = arr[i + 1];
        int neg = 0;
        if (val < 0) { neg = 1; val = -val; }
        // 转换为字符串（反转后写入）
        char buf[12];
        int pos = 0;
        if (val == 0) buf[pos++] = '0';
        while (val > 0) { buf[pos++] = '0' + (val % 10); val /= 10; }
        if (neg) buf[pos++] = '-';
        while (pos > 0) *p++ = buf[--pos];
        if (delim != 0 && i < n - 1) *p++ = (char)delim;
    }
    *p = 0;
}

// min: 数组最小值
__stdcall int min(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, min = arr[1];
    for (i = 1; i < arr[0]; i++)
        if (arr[i + 1] < min) min = arr[i + 1];
    return min;
}

// max: 数组最大值
__stdcall int max(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, max = arr[1];
    for (i = 1; i < arr[0]; i++)
        if (arr[i + 1] > max) max = arr[i + 1];
    return max;
}

// sum: 数组元素求和
__stdcall int sum(int* arr) {
    if (arr == 0) return 0;
    int i, sum = 0;
    for (i = 0; i < arr[0]; i++) sum += arr[i + 1];
    return sum;
}

// startswith: 检查数组 a 是否以数组 b 的前缀开头, 比较元素值, 返回 1/0
__stdcall int startswith(int* a, int* b) {
    if (a == 0 || b == 0) return 0;
    int na = a[0], nb = b[0];
    if (nb > na) return 0;
    int i;
    for (i = 0; i < nb; i++)
        if (a[i + 1] != b[i + 1]) return 0;
    return 1;
}

// endswith: 检查数组 a 是否以数组 b 的后缀结尾, 返回 1/0
__stdcall int endswith(int* a, int* b) {
    if (a == 0 || b == 0) return 0;
    int na = a[0], nb = b[0];
    if (nb > na) return 0;
    int i;
    for (i = 0; i < nb; i++)
        if (a[na - nb + i + 1] != b[i + 1]) return 0;
    return 1;
}

// ===== 快速排序 (Hoare分区) =====
static void _qs_swap(int* a, int i, int j) {
    int tmp = a[i + 1]; a[i + 1] = a[j + 1]; a[j + 1] = tmp;
}

static int _qs_partition(int* arr, int low, int high) {
    int pivot = arr[(low + high) / 2 + 1];
    int i = low - 1, j = high + 1;
    while (1) {
        do { i++; } while (arr[i + 1] < pivot);
        do { j--; } while (arr[j + 1] > pivot);
        if (i >= j) return j;
        _qs_swap(arr, i, j);
    }
}

static void _qs_recursive(int* arr, int low, int high) {
    if (low < high) {
        int p = _qs_partition(arr, low, high);
        _qs_recursive(arr, low, p);
        _qs_recursive(arr, p + 1, high);
    }
}

// sort_quick: 快速排序 (O(n log n) 平均, O(n^2) 最坏)
__stdcall void sort_quick(int* arr) {
    if (arr == 0 || arr[0] <= 1) return;
    _qs_recursive(arr, 0, arr[0] - 1);
}

// bsearch: 二分查找 (数组必须已排序), 返回索引或 -1
__stdcall int bsearch(int* arr, int value) {
    if (arr == 0 || arr[0] <= 0) return -1;
    int low = 0, high = arr[0] - 1;
    while (low <= high) {
        int mid = (low + high) / 2;
        int mval = arr[mid + 1];
        if (mval == value) return mid;
        if (mval < value) low = mid + 1;
        else high = mid - 1;
    }
    return -1;
}
