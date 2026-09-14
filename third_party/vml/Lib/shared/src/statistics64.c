#param lib("math64")

// VML Shared Statistics64 Library — 64-bit Statistics on long* arrays

__stdcall long lmean64(long* arr) {
    long len = arr[0];
    long sum = 0;
    long i;
    for (i = 1; i <= len; i++) sum += arr[i];
    if (len <= 0) return 0;
    return sum / len;
}

__stdcall long lsum64(long* arr) {
    long len = arr[0];
    long sum = 0;
    long i;
    for (i = 1; i <= len; i++) sum += arr[i];
    return sum;
}

__stdcall long lmin_arr64(long* arr) {
    long len = arr[0];
    if (len <= 0) return 0;
    long min = arr[1];
    long i;
    for (i = 2; i <= len; i++) if (arr[i] < min) min = arr[i];
    return min;
}

__stdcall long lmax_arr64(long* arr) {
    long len = arr[0];
    if (len <= 0) return 0;
    long max = arr[1];
    long i;
    for (i = 2; i <= len; i++) if (arr[i] > max) max = arr[i];
    return max;
}

__stdcall long lmedian64(long* arr) {
    long len = arr[0];
    if (len <= 0) return 0;
    // bubble sort a copy then pick middle (simple, correct for small arrays)
    long copy[65]; // max 64 elements
    long i, j;
    if (len > 64) len = 64;
    for (i = 0; i <= len; i++) copy[i] = arr[i];
    for (i = 1; i < len; i++)
        for (j = i + 1; j <= len; j++)
            if (copy[i] > copy[j]) { long t = copy[i]; copy[i] = copy[j]; copy[j] = t; }
    if (len % 2 == 1) return copy[(len+1)/2];
    return (copy[len/2] + copy[len/2 + 1]) / 2;
}

__stdcall long lrange64(long* arr) {
    long len = arr[0];
    if (len <= 0) return 0;
    long min = arr[1], max = arr[1];
    long i;
    for (i = 2; i <= len; i++) {
        if (arr[i] < min) min = arr[i];
        if (arr[i] > max) max = arr[i];
    }
    return max - min;
}

__stdcall long lcount_gt64(long* arr, long threshold) {
    long len = arr[0], count = 0, i;
    for (i = 1; i <= len; i++) if (arr[i] > threshold) count++;
    return count;
}

__stdcall long lcount_lt64(long* arr, long threshold) {
    long len = arr[0], count = 0, i;
    for (i = 1; i <= len; i++) if (arr[i] < threshold) count++;
    return count;
}

__stdcall long lcount_eq64(long* arr, long value) {
    long len = arr[0], count = 0, i;
    for (i = 1; i <= len; i++) if (arr[i] == value) count++;
    return count;
}

__stdcall long lvariance64(long* arr) {
    long len = arr[0];
    if (len <= 1) return 0;
    long m = lmean64(arr);
    long sum_sq = 0, i;
    for (i = 1; i <= len; i++) { long diff = arr[i] - m; sum_sq += diff * diff; }
    return sum_sq / (len - 1);
}

__stdcall long lstd_dev64(long* arr) {
    return lisqrt64(lvariance64(arr));
}

__stdcall long llinreg_slope64(long* x, long* y) {
    long n = x[0];
    if (y[0] < n) n = y[0];
    if (n <= 1) return 0;
    long sum_x = 0, sum_y = 0, sum_xy = 0, sum_x2 = 0, i;
    for (i = 1; i <= n; i++) {
        sum_x += x[i]; sum_y += y[i];
        sum_xy += x[i] * y[i]; sum_x2 += x[i] * x[i];
    }
    long denom = n * sum_x2 - sum_x * sum_x;
    if (denom == 0) return 0;
    return (n * sum_xy - sum_x * sum_y) / denom;
}

__stdcall long llinreg_intercept64(long* x, long* y) {
    long n = x[0];
    if (y[0] < n) n = y[0];
    if (n <= 0) return 0;
    long sum_x = 0, sum_y = 0, i;
    for (i = 1; i <= n; i++) { sum_x += x[i]; sum_y += y[i]; }
    long slope = llinreg_slope64(x, y);
    return (sum_y - slope * sum_x) / n;
}
