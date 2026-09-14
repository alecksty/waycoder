// VML Shared Signal Processing Library
// 信号处理 — 滤波/插值/平滑, MCU兼容

// ===== 移动平均滤波器 =====
// moving_avg: 简单移动平均, 返回新平均值
// state: [sum, count, *buffer, window_size, write_idx]
__stdcall int moving_avg_init(int window_size, int* buffer, int* state) {
    if (state == 0 || buffer == 0) return 0;
    int i;
    for (i = 0; i < window_size; i++) buffer[i] = 0;
    state[0] = 0;  // sum
    state[1] = 0;  // count
    state[2] = (int)buffer;
    state[3] = window_size;
    state[4] = 0;  // write_idx
    return 1;
}

__stdcall int moving_avg_update(int* state, int new_value) {
    if (state == 0) return new_value;
    int sum = state[0], count = state[1];
    int* buffer = (int*)state[2];
    int window = state[3];
    int idx = state[4];

    if (count < window) {
        buffer[idx] = new_value;
        sum += new_value;
        count++;
        state[0] = sum;
        state[1] = count;
        state[4] = (idx + 1) % window;
        return sum / count;
    }
    // Full window: remove oldest, add newest
    sum = sum - buffer[idx] + new_value;
    buffer[idx] = new_value;
    state[0] = sum;
    state[4] = (idx + 1) % window;
    return sum / window;
}

// ===== 指数移动平均 (EMA / low-pass filter) =====
// ema: y[n] = alpha*x[n] + (1-alpha)*y[n-1]
// state: [current_ema], alpha in 0..1000 (0.0..1.0)
__stdcall int ema_init(int* state) {
    if (state == 0) return 0;
    state[0] = 0;
    return 1;
}

__stdcall int ema_update(int* state, int value, int alpha) {
    if (state == 0) return value;
    int prev = state[0];
    // y = (alpha*value + (1000-alpha)*prev) / 1000
    int result = (alpha * value + (1000 - alpha) * prev) / 1000;
    state[0] = result;
    return result;
}

// ===== 卡尔曼滤波器 (1D) =====
// 简化一维卡尔曼: 预测+更新两步
// state: [x, p, q, r] — x=估计值, p=估计误差, q=过程噪声, r=测量噪声
__stdcall void kalman_init(int* state, int initial_value, int process_noise, int measure_noise) {
    if (state == 0) return;
    state[0] = initial_value;  // x: 估计值
    state[1] = 1000;           // p: 初始估计误差(缩放)
    state[2] = process_noise;  // q: 过程噪声
    state[3] = measure_noise;  // r: 测量噪声
}

__stdcall int kalman_update(int* state, int measurement) {
    if (state == 0) return measurement;
    int x = state[0], p = state[1];
    int q = state[2], r = state[3];

    // 预测: x不变, p = p + q
    p = p + q;

    // 更新: k = p / (p + r), x = x + k*(z - x), p = (1-k)*p
    int denom = p + r;
    if (denom > 0) {
        int k = (p * 1000) / denom;  // Kalman gain * 1000
        x = x + (k * (measurement - x)) / 1000;
        p = ((1000 - k) * p) / 1000;
    }

    state[0] = x;
    state[1] = p;
    return x;
}

// ===== 线性插值 =====
// 查找表插值: 给定 x_table[n] 和 y_table[n], 对输入 x 做线性插值
// 假设 x_table 递增排序
__stdcall int lerp_table(int* x_table, int* y_table, int n, int x) {
    if (x_table == 0 || y_table == 0 || n < 2) return 0;
    // 边界处理
    if (x <= x_table[0]) return y_table[0];
    if (x >= x_table[n - 1]) return y_table[n - 1];
    // 二分查找区间
    int lo = 0, hi = n - 1;
    while (lo < hi - 1) {
        int mid = (lo + hi) / 2;
        if (x_table[mid] <= x) lo = mid;
        else hi = mid;
    }
    // 线性插值: y = y0 + (y1 - y0)*(x - x0)/(x1 - x0)
    int dx = x_table[hi] - x_table[lo];
    if (dx == 0) return y_table[lo];
    return y_table[lo] + ((y_table[hi] - y_table[lo]) * (x - x_table[lo])) / dx;
}

// ===== 死区/迟滞 =====
// deadband: |x| < threshold → 0, 否则返回 x
__stdcall int deadband(int value, int threshold) {
    if (value > threshold) return value;
    if (value < -threshold) return value;
    return 0;
}

// hysteresis: 施密特触发器式迟滞比较器
// state[0]=current_output(0或1)
__stdcall int hysteresis(int* state, int input, int on_threshold, int off_threshold) {
    if (state == 0) return 0;
    if (input > on_threshold) state[0] = 1;
    else if (input < off_threshold) state[0] = 0;
    return state[0];
}
