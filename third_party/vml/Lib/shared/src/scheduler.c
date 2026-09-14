#param lib("swtimer")

// VML Shared Cooperative Scheduler Library
// 简单协作式任务调度器 — MCU RTOS替代

#define MAX_TASKS 8

// 任务状态: [period, remaining, func_addr, state_ptr, priority]
// priority: 0=stopped, 1=idle, 2=normal, 3=high

// init: 初始化调度器
__stdcall void init(int* tasks) {
    if (tasks == 0) return;
    int i;
    for (i = 0; i < MAX_TASKS; i++) {
        tasks[i * 5 + 0] = 0;  // period
        tasks[i * 5 + 1] = 0;  // remaining
        tasks[i * 5 + 2] = 0;  // func_addr
        tasks[i * 5 + 3] = 0;  // state_ptr
        tasks[i * 5 + 4] = 0;  // priority (0=stopped)
    }
}

// add_task: 添加任务, 返回 task_id 或 -1
__stdcall int add_task(int* tasks, int period, int priority, int func_addr, int state_ptr) {
    if (tasks == 0 || period <= 0) return -1;
    int i;
    for (i = 0; i < MAX_TASKS; i++) {
        if (tasks[i * 5 + 4] == 0) {  // stopped
            tasks[i * 5 + 0] = period;
            tasks[i * 5 + 1] = 0;     // 立即到期
            tasks[i * 5 + 2] = func_addr;
            tasks[i * 5 + 3] = state_ptr;
            tasks[i * 5 + 4] = priority;
            return i;
        }
    }
    return -1;
}

// task: 移除任务
__stdcall void task(int* tasks, int task_id) {
    if (tasks == 0 || task_id < 0 || task_id >= MAX_TASKS) return;
    tasks[task_id * 5 + 4] = 0;  // priority=0 → stopped
}

// set_period: 修改任务周期
__stdcall void set_period(int* tasks, int task_id, int period) {
    if (tasks == 0 || task_id < 0 || task_id >= MAX_TASKS || period <= 0) return;
    tasks[task_id * 5 + 0] = period;
    if (tasks[task_id * 5 + 1] > period)
        tasks[task_id * 5 + 1] = period;
}

// run: 运行调度器一个tick, 返回需要执行的任务 bit掩码
__stdcall int run(int* tasks) {
    if (tasks == 0) return 0;
    int ready = 0;
    int i;
    // 更新所有倒计时
    for (i = 0; i < MAX_TASKS; i++) {
        int priority = tasks[i * 5 + 4];
        if (priority > 0) {
            int remaining = tasks[i * 5 + 1];
            if (remaining > 0) remaining--;
            if (remaining <= 0) {
                ready |= (1 << i);
                remaining = tasks[i * 5 + 0];  // 重新装载周期
            }
            tasks[i * 5 + 1] = remaining;
        }
    }
    return ready;
}

// get_task: 获取任务信息 (func_addr, state_ptr)
__stdcall int get_func(int* tasks, int task_id) {
    if (tasks == 0 || task_id < 0 || task_id >= MAX_TASKS) return 0;
    return tasks[task_id * 5 + 2];
}

__stdcall int get_state(int* tasks, int task_id) {
    if (tasks == 0 || task_id < 0 || task_id >= MAX_TASKS) return 0;
    return tasks[task_id * 5 + 3];
}
