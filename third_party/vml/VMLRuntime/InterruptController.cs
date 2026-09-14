namespace VMLRuntime
{
    public enum InterruptPriority
    {
        NMI = 1,
        Exception = 2,
        Timer = 3,
        External = 4,
        Software = 5
    }

    public class InterruptRequest
    {
        public int Vector;
        public InterruptPriority Priority;
        public bool IsRecoverable;

        public InterruptRequest(int vector, InterruptPriority priority, bool isRecoverable)
        {
            Vector = vector;
            Priority = priority;
            IsRecoverable = isRecoverable;
        }
    }

    /// <summary>
    /// 中断控制器 — 管理待处理硬件中断队列，按优先级分发。
    /// 仅 OS 模式 (privilegeLevel == 0) 下激活。
    /// </summary>
    public class InterruptController
    {
        private readonly List<InterruptRequest> _pending = new();

        public bool HasPending => _pending.Count > 0;

        /// <summary>请求一个硬件中断。向量 0-7 和 255 自动标记为不可恢复。</summary>
        public void RequestInterrupt(int vector, InterruptPriority priority)
        {
            bool isRecoverable = !IsNonRecoverable(vector);
            _pending.Add(new InterruptRequest(vector, priority, isRecoverable));
        }

        /// <summary>按优先级取出最高优先级中断，同优先级 FIFO。</summary>
        public InterruptRequest? Dequeue()
        {
            if (_pending.Count == 0) return null;

            int bestIdx = 0;
            for (int i = 1; i < _pending.Count; i++)
            {
                if (_pending[i].Priority < _pending[bestIdx].Priority)
                    bestIdx = i;
            }
            var req = _pending[bestIdx];
            _pending.RemoveAt(bestIdx);
            return req;
        }

        public void Clear() => _pending.Clear();

        /// <summary>向量 0-7 (异常) 和 255 (NMI) 为不可恢复中断。</summary>
        public static bool IsNonRecoverable(int vector)
            => (vector >= 0 && vector <= 7) || vector == 255;
    }
}
