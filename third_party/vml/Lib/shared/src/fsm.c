#param lib("scheduler")

// VML Shared FSM Library
// 有限状态机 — 嵌入式状态管理

// transition: 检查转换条件并执行转换
// state: [current_state, event], transitions: 每个转换 3个int [from, event, to]
// 返回新状态, 无匹配返回 -1
__stdcall int transition(int* state, int* transitions, int trans_count) {
    if (state == 0 || transitions == 0) return -1;
    int current = state[0];
    int event = state[1];
    int i;
    for (i = 0; i < trans_count; i++) {
        int from = transitions[i * 3 + 0];
        int evt  = transitions[i * 3 + 1];
        int to   = transitions[i * 3 + 2];
        if (from == current && evt == event) {
            state[0] = to;
            state[1] = 0; // 清除事件
            return to;
        }
    }
    return -1;
}

// send_event: 发送事件到状态机
__stdcall void send_event(int* state, int event) {
    if (state == 0) return;
    state[1] = event;
}

// state: 强制设置状态
__stdcall void state(int* state, int new_state) {
    if (state == 0) return;
    state[0] = new_state;
    state[1] = 0;
}

// get_state: 获取当前状态
__stdcall int get_state(int* state) {
    if (state == 0) return -1;
    return state[0];
}

// ===== 简单事件队列 =====
#define EVENT_QUEUE_SIZE 16

// event_queue_push: 事件入队, 返回1成功/0满
__stdcall int event_queue_push(int* queue, int event) {
    if (queue == 0) return 0;
    int head = queue[0];
    int tail = queue[1];
    int next = (head + 1) % EVENT_QUEUE_SIZE;
    if (next == tail) return 0; // full
    queue[2 + head] = event;
    queue[0] = next;
    return 1;
}

// event_queue_pop: 事件出队, 返回事件值或 -1(空)
__stdcall int event_queue_pop(int* queue) {
    if (queue == 0) return -1;
    int head = queue[0];
    int tail = queue[1];
    if (head == tail) return -1; // empty
    int event = queue[2 + tail];
    queue[1] = (tail + 1) % EVENT_QUEUE_SIZE;
    return event;
}

// event_queue_init: 初始化事件队列
__stdcall void event_queue_init(int* queue) {
    if (queue == 0) return;
    queue[0] = 0; // head
    queue[1] = 0; // tail
}

// event_queue_available: 队列中事件数
__stdcall int event_queue_available(int* queue) {
    if (queue == 0) return 0;
    int head = queue[0];
    int tail = queue[1];
    return (head - tail + EVENT_QUEUE_SIZE) % EVENT_QUEUE_SIZE;
}
