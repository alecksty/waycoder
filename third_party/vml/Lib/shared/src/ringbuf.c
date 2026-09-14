#param lib("swtimer")

// VML Shared Ring Buffer Library
// 环形缓冲区 — 嵌入式/MCU必备, 无锁设计

typedef struct {
    int* buffer;
    int size;       // 缓冲区容量 (必须是2的幂)
    int read_idx;   // 读指针
    int write_idx;  // 写指针
} t;

// init: 初始化环形缓冲区 (buffer已分配, size必须是2的幂)
__stdcall void init(int* buffer, int size, int* state) {
    // state[0]=buffer, state[1]=size, state[2]=read_idx, state[3]=write_idx
    if (buffer == 0 || state == 0) return;
    state[0] = (int)buffer;
    state[1] = size;
    state[2] = 0;  // read_idx
    state[3] = 0;  // write_idx
}

// write: 写入一个元素, 返回1成功/0满
__stdcall int write(int* state, int value) {
    if (state == 0) return 0;
    int* buffer = (int*)state[0];
    int size = state[1];
    int write_idx = state[3];
    int read_idx = state[2];
    int mask = size - 1;
    if (((write_idx + 1) & mask) == (read_idx & mask)) return 0; // full
    buffer[write_idx & mask] = value;
    state[3] = (write_idx + 1) & mask;
    return 1;
}

// read: 读取一个元素, 返回1成功/0空
__stdcall int read(int* state, int* value) {
    if (state == 0 || value == 0) return 0;
    int* buffer = (int*)state[0];
    int read_idx = state[2];
    int write_idx = state[3];
    if (read_idx == write_idx) return 0; // empty
    *value = buffer[read_idx & (state[1] - 1)];
    state[2] = (read_idx + 1) & (state[1] - 1);
    return 1;
}

// available: 可读元素数量
__stdcall int available(int* state) {
    if (state == 0) return 0;
    int mask = state[1] - 1;
    return (state[3] - state[2]) & mask;
}

// free: 剩余可写空间
__stdcall int free(int* state) {
    if (state == 0) return 0;
    int mask = state[1] - 1;
    return state[1] - 1 - ((state[3] - state[2]) & mask);
}

// is_empty: 缓冲区为空?
__stdcall int is_empty(int* state) {
    if (state == 0) return 1;
    return state[2] == state[3] ? 1 : 0;
}

// is_full: 缓冲区已满?
__stdcall int is_full(int* state) {
    if (state == 0) return 0;
    int mask = state[1] - 1;
    return ((state[3] + 1) & mask) == (state[2] & mask) ? 1 : 0;
}

// reset: 清空缓冲区
__stdcall void reset(int* state) {
    if (state == 0) return;
    state[2] = 0;
    state[3] = 0;
}
