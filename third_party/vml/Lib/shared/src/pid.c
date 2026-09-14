#param lib("ringbuf")
#param lib("swtimer")

// VML Shared PID Controller Library
// PID 控制算法 — 温控/电机/机器人常用, MCU友好

// PID state: [Kp*1000, Ki*1000, Kd*1000, setpoint, integral, prev_error, out_min, out_max]
// 所有参数缩放1000倍以使用整数运算

// init: 初始化 PID 控制器
__stdcall void init(int* state, int kp, int ki, int kd, int setpoint, int out_min, int out_max) {
    if (state == 0) return;
    state[0] = kp;       // Kp * 1000
    state[1] = ki;       // Ki * 1000
    state[2] = kd;       // Kd * 1000
    state[3] = setpoint; // 目标值
    state[4] = 0;        // 积分累加
    state[5] = 0;        // 上次误差
    state[6] = out_min;  // 输出下限
    state[7] = out_max;  // 输出上限
}

// set_setpoint: 更新目标值
__stdcall void set_setpoint(int* state, int setpoint) {
    if (state == 0) return;
    state[3] = setpoint;
    state[4] = 0;  // 复位积分
    state[5] = 0;  // 复位上次误差
}

// compute: 计算 PID 输出 (返回值需除以1000)
// input: 当前测量值, dt: 时间间隔(ms)
__stdcall int compute(int* state, int input, int dt) {
    if (state == 0) return 0;
    int kp = state[0], ki = state[1], kd = state[2];
    int setpoint = state[3];
    int integral = state[4];
    int prev_error = state[5];
    int out_min = state[6], out_max = state[7];

    // 计算误差
    int error = setpoint - input;

    // 比例项
    int p_term = (kp * error) / 1000;

    // 积分项 (带抗饱和)
    integral += error * dt;
    // 限制积分
    int i_max = 1000000;
    if (integral > i_max) integral = i_max;
    if (integral < -i_max) integral = -i_max;
    int i_term = (ki * integral) / 1000;

    // 微分项
    int derivative = 0;
    if (dt > 0) derivative = (error - prev_error) / dt;
    int d_term = (kd * derivative) / 1000;

    // 总输出
    int output = p_term + i_term + d_term;

    // 限制输出范围
    if (output > out_max) output = out_max;
    if (output < out_min) output = out_min;

    // 保存状态
    state[4] = integral;
    state[5] = error;

    return output;
}

// reset: 复位 PID 状态
__stdcall void reset(int* state) {
    if (state == 0) return;
    state[4] = 0;
    state[5] = 0;
}
