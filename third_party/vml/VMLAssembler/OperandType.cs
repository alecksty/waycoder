namespace VMLAssembler
{
    /// <summary>
    /// 操作数类型
    /// </summary>
    public enum OperandType
    {
        REGISTER, // 寄存器
        IMMEDIATE, // 立即数
        MEMORY, // 内存地址（绝对或寄存器相对）
        LABEL, // 标签
        INDIRECT // 间接寻址
    }
}
