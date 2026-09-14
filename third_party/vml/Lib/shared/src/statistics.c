#param lib("fixed")

// VML Shared Statistics Library
// 统计函数库 — MCU兼容，纯计算

// mean: 算术平均值
__stdcall int mean(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, sum = 0, n = arr[0];
    for (i = 0; i < n; i++) sum += arr[i + 1];
    return sum / n;
}

// sum_arr: 数组元素求和
__stdcall int sum_arr(int* arr) {
    if (arr == 0) return 0;
    int i, sum = 0;
    for (i = 0; i < arr[0]; i++) sum += arr[i + 1];
    return sum;
}

// min_arr: 数组最小值
__stdcall int min_arr(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, min = arr[1];
    for (i = 1; i < arr[0]; i++)
        if (arr[i + 1] < min) min = arr[i + 1];
    return min;
}

// max_arr: 数组最大值
__stdcall int max_arr(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int i, max = arr[1];
    for (i = 1; i < arr[0]; i++)
        if (arr[i + 1] > max) max = arr[i + 1];
    return max;
}

// median: 中位数 (假设已排序或复制后排序)
// 简单实现: 使用选择算法找第 k 小元素
__stdcall int median(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    int n = arr[0];
    // 复制数组并冒泡排序
    int i, j;
    // 原地修改副本 (简化: 直接在原数组上操作)
    // 冒泡排序
    for (i = 0; i < n - 1; i++)
        for (j = 0; j < n - 1 - i; j++)
            if (arr[j + 1] > arr[j + 2]) {
                int tmp = arr[j + 1];
                arr[j + 1] = arr[j + 2];
                arr[j + 2] = tmp;
            }
    // 中位数: 排序后的中间元素
    if (n % 2 == 1)
        return arr[n / 2 + 1];
    else
        return (arr[n / 2] + arr[n / 2 + 1]) / 2;
}

// range: 最大值 - 最小值
__stdcall int range(int* arr) {
    if (arr == 0 || arr[0] <= 0) return 0;
    return max_arr(arr) - min_arr(arr);
}

// count_if: 统计满足 arr[i] > threshold 的元素个数
__stdcall int count_gt(int* arr, int threshold) {
    if (arr == 0) return 0;
    int i, count = 0;
    for (i = 0; i < arr[0]; i++)
        if (arr[i + 1] > threshold) count++;
    return count;
}

__stdcall int count_lt(int* arr, int threshold) {
    if (arr == 0) return 0;
    int i, count = 0;
    for (i = 0; i < arr[0]; i++)
        if (arr[i + 1] < threshold) count++;
    return count;
}

__stdcall int count_eq(int* arr, int value) {
    if (arr == 0) return 0;
    int i, count = 0;
    for (i = 0; i < arr[0]; i++)
        if (arr[i + 1] == value) count++;
    return count;
}

// variance: 方差 (population variance)
__stdcall int variance(int* arr) {
    if (arr == 0 || arr[0] <= 1) return 0;
    int n = arr[0];
    int m = mean(arr);
    int i, sum_sq = 0;
    for (i = 0; i < n; i++) {
        int diff = arr[i + 1] - m;
        sum_sq += diff * diff;
    }
    return sum_sq / n;
}

// std_dev: 标准差 = sqrt(variance)
__stdcall int std_dev(int* arr) {
    int v = variance(arr);
    if (v <= 0) return 0;
    // Newton sqrt for integer
    int g = v / 2 + 1;
    int i;
    for (i = 0; i < 10; i++) {
        int ng = (g + v / (g > 0 ? g : 1)) / 2;
        if (ng >= g - 1 && ng <= g + 1) { g = ng; break; }
        g = ng;
    }
    return g;
}

// ===== 简单线性回归 =====
// y = slope * x + intercept
// 输入: x[], y[] 两个等长数组
// 输出: slope*1000 和 intercept*1000 (定点缩放)
__stdcall int linreg_slope(int* x, int* y) {
    if (x == 0 || y == 0 || x[0] != y[0] || x[0] <= 1) return 0;
    int n = x[0];
    int sum_x = 0, sum_y = 0, sum_xy = 0, sum_x2 = 0;
    int i;
    for (i = 0; i < n; i++) {
        int xi = x[i + 1], yi = y[i + 1];
        sum_x += xi;
        sum_y += yi;
        sum_xy += xi * yi;
        sum_x2 += xi * xi;
    }
    int num = n * sum_xy - sum_x * sum_y;
    int den = n * sum_x2 - sum_x * sum_x;
    if (den == 0) return 0;
    return (num * 1000) / den;  // slope * 1000
}

__stdcall int linreg_intercept(int* x, int* y) {
    if (x == 0 || y == 0 || x[0] != y[0] || x[0] <= 1) return 0;
    int n = x[0];
    int sum_x = 0, sum_y = 0;
    int i;
    for (i = 0; i < n; i++) {
        sum_x += x[i + 1];
        sum_y += y[i + 1];
    }
    int slope = linreg_slope(x, y);
    return ((sum_y * 1000) - slope * sum_x) / (n * 1000);
}
