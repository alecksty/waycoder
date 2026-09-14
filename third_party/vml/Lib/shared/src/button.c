#param lib("swtimer")

// VML Shared Button Library
// 按键消抖 + 长短按检测 — MCU常用

// init: 初始化按键状态
// state: [debounce_ms, hold_ms, counter, last_state, event_flags]
__stdcall void init(int* state, int debounce_ms, int hold_ms) {
    if (state == 0) return;
    state[0] = debounce_ms;  // 消抖时间(ms)
    state[1] = hold_ms;      // 长按时间(ms)
    state[2] = 0;            // 计数器
    state[3] = 1;            // 上次状态 (1=释放)
    state[4] = 0;            // 事件标志
}

// update: 每 ms 调用一次, raw=当前引脚电平(0按下/1释放)
// 返回事件: 0=无, 1=短按, 2=长按, 3=释放
__stdcall int update(int* state, int raw, int dt_ms) {
    if (state == 0) return 0;
    int debounce = state[0], hold = state[1];
    int counter = state[2], last = state[3];
    int result = 0;

    if (raw != last) {
        counter += dt_ms;
        if (counter >= debounce) {
            if (raw == 0) {
                // 按下
                result = 1;  // 短按 (暂定, 等待确认长短)
                state[4] = 0; // 清除长按标志
            } else {
                // 释放
                if (state[4] == 2) result = 3; // 长按释放
                else result = 3; // 短按释放
            }
            last = raw;
            counter = 0;
        }
    } else if (raw == 0 && last == 0) {
        // 保持按下状态
        counter += dt_ms;
        if (counter >= hold && state[4] == 0) {
            result = 2;  // 触发长按
            state[4] = 2;
        }
    } else {
        counter = 0;
    }

    state[2] = counter;
    state[3] = last;
    return result;
}

// is_pressed: 当前是否按下 (消抖后)
__stdcall int is_pressed(int* state) {
    if (state == 0) return 0;
    return state[3] == 0 ? 1 : 0;
}
