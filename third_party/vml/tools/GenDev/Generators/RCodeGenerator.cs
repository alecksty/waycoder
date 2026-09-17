using GenDev.Models;

namespace GenDev.Generators
{
    /// <summary>
    /// R 语言设备定义生成器 (赋值常量).
    /// 继承 GeneratorBase 消除 ~100 行模板重复.
    /// </summary>
    public class RCodeGenerator : GeneratorBase
    {
        public override string Language => "r";
        public override string FileExtension => ".R";
        protected override string CommentPrefix => "#";

        protected override string GetTypeForSize(int size) => "integer";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"{name}_ADDR <- {addr}  # register address";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"{parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT <- {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"{name}_START <- {start}\n{name}_END <- {end}\n{name}_SIZE <- {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"{name}_BASE <- {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"{reg}_ADDR <- {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"INT_{name} <- {vector}  # {desc}";
    }
}
