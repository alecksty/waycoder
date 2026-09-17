using GenDev.Models;

namespace GenDev.Generators
{
    /// <summary>
    /// D 语言设备定义生成器 (immutable/static 常量).
    /// 继承 GeneratorBase 消除 ~105 行模板重复.
    /// </summary>
    public class DCodeGenerator : GeneratorBase
    {
        public override string Language => "d";
        public override string FileExtension => ".d";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => size switch
        {
            1 => "ubyte", 2 => "ushort", 4 => "uint", 8 => "ulong",
            _ => "uint"
        };

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"static immutable {type}* {name} = cast({type}*){addr};";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"static immutable int {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)} = {bit};";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"static immutable auto {name}_START = {start};\nstatic immutable auto {name}_END = {end};\nstatic immutable auto {name}_SIZE = {size};";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"static immutable auto {name}_BASE = {baseAddr};";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"static immutable {type}* {reg} = cast({type}*){absAddr};";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"static immutable int INT_{name} = {vector};  // {desc}";
    }
}
