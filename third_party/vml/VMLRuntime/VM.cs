using VMLAssembler;

namespace VMLRuntime
{
    /// <summary>
    /// 虚拟机高级接口
    /// </summary>
    public class Vm
    {
        private readonly VmRuntime _runtime;

        /// <summary>
        /// 初始化虚拟机
        /// </summary>
        /// <param name="memorySize">内存大小（字节），默认640K</param>
        public Vm(int memorySize = 1024 * 1024)
        {
            _runtime = new VmRuntime(memorySize);
        }

        /// <summary>
        /// 加载程序
        /// </summary>
        /// <param name="program">要加载的程序</param>
        public void Load(VmlProgram program)
        {
            _runtime.LoadProgram(program);
        }

        public void Run()
        {
            _runtime.Run();
        }

        public void Step()
        {
            _runtime.Step();
        }

        /// <summary>
        /// 获取寄存器值
        /// </summary>
        public int[] Registers { get { return _runtime.Registers; } }

        /// <summary>
        /// 获取内存内容
        /// </summary>
        public byte[] Memory { get { return _runtime.Memory; } }

        /// <summary>
        /// 获取当前指令指针
        /// </summary>
        public int PC { get { return _runtime.PC; } }
    }
}
