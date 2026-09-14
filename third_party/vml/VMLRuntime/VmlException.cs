namespace VMLRuntime
{
    public class VmlException : Exception
    {
        public VmlException() { }
        public VmlException(string message) : base(message) { }
        public VmlException(string message, Exception inner) : base(message, inner) { }
    }

    public class VmlMemoryException : VmlException
    {
        public uint Address { get; }
        public VmlMemoryException(string message, uint address = 0)
            : base(message) { Address = address; }
    }

    public class VmlRuntimeException : VmlException
    {
        public int InstructionPointer { get; }
        public VmlRuntimeException(string message, int ip = 0)
            : base(message) { InstructionPointer = ip; }
    }

    public class VmlSyscallException : VmlException
    {
        public int SyscallNumber { get; }
        public VmlSyscallException(string message, int syscallNumber = 0)
            : base(message) { SyscallNumber = syscallNumber; }
    }

    public class VmlLabelException : VmlException
    {
        public string LabelName { get; }
        public VmlLabelException(string labelName)
            : base($"未找到标签: {labelName}") { LabelName = labelName; }
    }

    public class VmlFloatException : VmlException
    {
        public VmlFloatException(string message) : base(message) { }
    }
}
