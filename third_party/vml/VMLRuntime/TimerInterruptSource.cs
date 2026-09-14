namespace VMLRuntime
{
    /// <summary>
    /// 定时器中断源 — 每 N 条指令触发一次硬件中断 (向量 32 = TIMER)。
    /// 仅在 OS 模式下激活。
    /// </summary>
    public class TimerInterruptSource
    {
        private readonly InterruptController _controller;
        private long _instructionCount;
        private int _interval;

        /// <summary>触发间隔（指令条数），0 = 禁用。</summary>
        public int Interval
        {
            get => _interval;
            set => _interval = value > 0 ? value : 0;
        }

        public bool Enabled => _interval > 0;

        public TimerInterruptSource(InterruptController controller, int interval = 0)
        {
            _controller = controller;
            _interval = interval > 0 ? interval : 0;
        }

        public void OnInstructionExecuted()
        {
            if (_interval <= 0) return;
            _instructionCount++;
            if (_instructionCount >= _interval)
            {
                _instructionCount = 0;
                _controller.RequestInterrupt(32, InterruptPriority.Timer);
            }
        }

        public void Reset() => _instructionCount = 0;
    }
}
