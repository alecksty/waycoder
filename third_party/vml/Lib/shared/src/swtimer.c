// VML Shared Software Timer Library
// 软件定时器 — 单次/周期, 基于tick

#define MAX_TIMERS 8

// 定时器状态数组: 每个定时器 4 个int [period, remaining, flags, callback]
// flags: bit0=active, bit1=repeat, bit2=triggered

// init: 初始化所有定时器
__stdcall void init(int* timers) {
    if (timers == 0) return;
    int i;
    for (i = 0; i < MAX_TIMERS; i++) {
        timers[i * 4 + 0] = 0;   // period
        timers[i * 4 + 1] = 0;   // remaining
        timers[i * 4 + 2] = 0;   // flags
        timers[i * 4 + 3] = 0;   // callback_id
    }
}

// start: 启动定时器, 返回 timer_id 或 -1 (无可用)
// period: 定时周期(tick), repeat: 1=重复 0=单次
__stdcall int start(int* timers, int period, int repeat, int callback_id) {
    if (timers == 0 || period <= 0) return -1;
    int i;
    for (i = 0; i < MAX_TIMERS; i++) {
        int flags = timers[i * 4 + 2];
        if (!(flags & 1)) {  // 未激活
            timers[i * 4 + 0] = period;
            timers[i * 4 + 1] = period;
            timers[i * 4 + 2] = 1 | (repeat ? 2 : 0);  // active + repeat
            timers[i * 4 + 3] = callback_id;
            return i;
        }
    }
    return -1;
}

// stop: 停止定时器
__stdcall void stop(int* timers, int timer_id) {
    if (timers == 0 || timer_id < 0 || timer_id >= MAX_TIMERS) return;
    timers[timer_id * 4 + 2] = 0;  // clear flags
}

// tick: 每 tick 调用一次, 返回触发定时器的 callback_id 位掩码
// 返回: bit[i]=1 表示定时器 i 触发
__stdcall int tick(int* timers) {
    if (timers == 0) return 0;
    int triggered = 0;
    int i;
    for (i = 0; i < MAX_TIMERS; i++) {
        int flags = timers[i * 4 + 2];
        if (flags & 1) {  // active
            int remaining = timers[i * 4 + 1];
            remaining--;
            if (remaining <= 0) {
                triggered |= (1 << i);
                if (flags & 2) {
                    // 重复: 重新装载
                    remaining = timers[i * 4 + 0];
                } else {
                    // 单次: 停用
                    timers[i * 4 + 2] = 0;
                }
            }
            timers[i * 4 + 1] = remaining;
        }
    }
    return triggered;
}

// remaining: 获取定时器剩余 tick
__stdcall int remaining(int* timers, int timer_id) {
    if (timers == 0 || timer_id < 0 || timer_id >= MAX_TIMERS) return 0;
    if (!(timers[timer_id * 4 + 2] & 1)) return 0;
    return timers[timer_id * 4 + 1];
}
