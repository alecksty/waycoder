namespace VMLAssembler
{
    /// <summary>
    /// 操作数
    /// </summary>
    public class Operand
    {
        public OperandType Type { get; set; }
        public object Value { get; set; }
        public int Size { get; set; } = 32; // 操作数大小（位）

        /// <summary>
        /// 操作数构造函数
        /// </summary>
        /// <param name="type">操作数类型</param>
        /// <param name="value">操作数值</param>
        /// <param name="size">操作数大小（位）</param>
        public Operand(OperandType type, object value, int size = 32)
        {
            Type = type;
            Value = value;
            Size = size;
        }

        public override string? ToString()
        {
            return Type switch
            {
                OperandType.REGISTER => $"R{Value}",
                OperandType.IMMEDIATE => $"#{Value}",
                OperandType.MEMORY => $"[{Value}]",
                OperandType.LABEL => $"{Value}",
                OperandType.INDIRECT => $"@{Value}",
                _ => Value.ToString()
            };
        }
    }
}
