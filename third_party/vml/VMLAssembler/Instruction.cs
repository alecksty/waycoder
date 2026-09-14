using System.Collections.Generic;
using System.Linq;

namespace VMLAssembler
{
    /// <summary>
    /// VML 指令
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// 操作码
        /// </summary>
        public OpCode Opcode { get; set; }
        /// <summary>
        /// 操作数
        /// </summary>
        public List<Operand> Operands { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public int Address { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// 对应的源码行号（-1 表示未知）
        /// </summary>
        public int SourceLine { get; set; } = -1;

        /// <summary>
        /// 初始化指令
        /// </summary>
        /// <param name="opcode">操作码</param>
        /// <param name="operands">操作数</param>
        /// <param name="address">地址</param>
        /// <param name="label">标签</param>
        public Instruction(OpCode opcode, List<Operand> operands, int address = 0, string? label = null)
        {
            Opcode   = opcode;
            Operands = operands;
            Address  = address;
            Label    = label;
        }

        /// <summary>
        /// 获取指令的字符串表示
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var labelStr = !string.IsNullOrEmpty(Label) ? $"{Label}: " : "";

            if (Opcode == OpCode.ASM && Operands.Count > 0)
            {
                var asmContent = Operands[0].Value?.ToString() ?? "";
                return $"{labelStr}asm \"{asmContent}\"";
            }

            var ops = string.Join(" ", Operands.Select(o => o.ToString()));
            return $"{labelStr}{Opcode.ToString().ToLowerInvariant()} {ops}".Trim();
        }
    }
}
