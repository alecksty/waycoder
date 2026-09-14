#param lib("bitops64")
#param lib("statistics64")

// VML Shared Array64 Library — 64-bit Integer Arrays (long* with long indices)

__stdcall long llen64(long* arr) { return arr[0]; }

__stdcall long lget64(long* arr, long index) {
    if (index < 0 || index >= arr[0]) return 0;
    return arr[index + 1];
}

__stdcall void lset64(long* arr, long index, long value) {
    if (index >= 0 && index < arr[0]) arr[index + 1] = value;
}

__stdcall void lsort_bubble64(long* arr) {
    long len = arr[0], i, j;
    for (i = 1; i < len; i++)
        for (j = i + 1; j <= len; j++)
            if (arr[i] > arr[j]) { long t = arr[i]; arr[i] = arr[j]; arr[j] = t; }
}

__stdcall long lindexof64(long* arr, long value) {
    long len = arr[0], i;
    for (i = 1; i <= len; i++) if (arr[i] == value) return i - 1;
    return -1;
}

__stdcall long llast_indexof64(long* arr, long value) {
    long i;
    for (i = arr[0]; i >= 1; i--) if (arr[i] == value) return i - 1;
    return -1;
}

__stdcall long lcontains64(long* arr, long value) {
    long len = arr[0], i;
    for (i = 1; i <= len; i++) if (arr[i] == value) return 1;
    return 0;
}

__stdcall void lreverse64(long* arr) {
    long len = arr[0], i;
    for (i = 1; i <= len / 2; i++) { long t = arr[i]; arr[i] = arr[len - i + 1]; arr[len - i + 1] = t; }
}

__stdcall void lfill64(long* arr, long value) {
    long i;
    for (i = 1; i <= arr[0]; i++) arr[i] = value;
}

__stdcall void lcopy64(long* src, long* dst) {
    long i;
    for (i = 0; i <= src[0]; i++) dst[i] = src[i];
}

__stdcall void lslice64(long* src, long start, long count, long* dst) {
    long i, j = 0;
    if (start < 0) start = 0;
    if (start + count > src[0]) count = src[0] - start;
    dst[0] = count;
    for (i = start; i < start + count; i++) dst[++j] = src[i + 1];
}

__stdcall void lconcat64(long* a, long* b, long* dst) {
    long i;
    dst[0] = a[0] + b[0];
    for (i = 1; i <= a[0]; i++) dst[i] = a[i];
    for (i = 1; i <= b[0]; i++) dst[a[0] + i] = b[i];
}

__stdcall void lpush64(long* arr, long value) {
    arr[0]++;
    arr[arr[0]] = value;
}

__stdcall long lpop64(long* arr) {
    if (arr[0] <= 0) return 0;
    long val = arr[arr[0]];
    arr[0]--;
    return val;
}

__stdcall long lshift64(long* arr) {
    if (arr[0] <= 0) return 0;
    long val = arr[1], i;
    for (i = 1; i < arr[0]; i++) arr[i] = arr[i + 1];
    arr[0]--;
    return val;
}

__stdcall void lunshift64(long* arr, long value) {
    long i;
    for (i = arr[0]; i >= 1; i--) arr[i + 1] = arr[i];
    arr[1] = value;
    arr[0]++;
}

__stdcall void linsert64(long* arr, long index, long value) {
    long i;
    if (index < 0 || index > arr[0]) return;
    for (i = arr[0]; i >= index + 1; i--) arr[i + 1] = arr[i];
    arr[index + 1] = value;
    arr[0]++;
}

__stdcall long lremove64(long* arr, long index) {
    if (index < 0 || index >= arr[0]) return 0;
    long val = arr[index + 1], i;
    for (i = index + 1; i < arr[0]; i++) arr[i] = arr[i + 1];
    arr[0]--;
    return val;
}

__stdcall long lmin_val64(long* arr) { return lmin_arr64(arr); }
__stdcall long lmax_val64(long* arr) { return lmax_arr64(arr); }
__stdcall long lsum_val64(long* arr) { return lsum64(arr); }

__stdcall long lstartswith64(long* a, long* b) {
    long i;
    if (b[0] > a[0]) return 0;
    for (i = 1; i <= b[0]; i++) if (a[i] != b[i]) return 0;
    return 1;
}

__stdcall long lendswith64(long* a, long* b) {
    long i, offset = a[0] - b[0];
    if (b[0] > a[0]) return 0;
    for (i = 1; i <= b[0]; i++) if (a[offset + i] != b[i]) return 0;
    return 1;
}
