// VML Shared Signal64 Library — 64-bit Signal Processing (long* state arrays)

__stdcall long lmoving_avg_init64(long window_size, long* buffer, long* state) {
    state[0] = window_size; state[1] = 0; state[2] = 0; state[3] = (long)buffer;
    long i;
    for (i = 0; i < window_size && i < 64; i++) buffer[i] = 0;
    return 1;
}

__stdcall long lmoving_avg_update64(long* state, long new_value) {
    long ws = state[0], idx = state[1], sum = state[2];
    long* buf = (long*)state[3];
    sum -= buf[idx];
    buf[idx] = new_value;
    sum += new_value;
    idx = (idx + 1) % ws;
    if (state[3 + 1] < ws) state[3 + 1]++;
    state[1] = idx; state[2] = sum;
    long count = state[3 + 1];
    if (count <= 0) count = ws;
    return sum / count;
}

__stdcall long lema_init64(long* state) { state[0] = 0; state[1] = 0; return 1; }

__stdcall long lema_update64(long* state, long value, long alpha) {
    long prev = state[0];
    long result = prev + ((value - prev) * alpha) / 1000;
    state[0] = result;
    return result;
}

__stdcall void lkalman_init64(long* state, long initial_value, long process_noise, long measure_noise) {
    state[0] = initial_value; state[1] = 1000; state[2] = process_noise; state[3] = measure_noise;
}

__stdcall long lkalman_update64(long* state, long measurement) {
    long estimate = state[0], p = state[1], q = state[2], r = state[3];
    p += q;
    long k = p * 1000 / (p + (r > 0 ? r : 1));
    estimate = estimate + k * (measurement - estimate) / 1000;
    p = (1000 - k) * p / 1000;
    state[0] = estimate; state[1] = p;
    return estimate;
}

__stdcall long ldeadband64(long value, long threshold) {
    if (value < threshold && value > -threshold) return 0;
    return value;
}

__stdcall long lhysteresis64(long* state, long input, long on_threshold, long off_threshold) {
    if (input > on_threshold) { state[0] = 1; return 1; }
    if (input < off_threshold) { state[0] = 0; return 0; }
    return state[0];
}
