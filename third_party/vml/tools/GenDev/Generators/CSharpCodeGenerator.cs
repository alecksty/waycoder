using GenDev.Models;

namespace GenDev.Generators
{
    /// <summary>
    /// C# 设备常量生成器 (public const).
    /// 继承 GeneratorBase 消除 ~170 行模板重复.
    /// </summary>
    public class CSharpCodeGenerator : GeneratorBase
    {
        public override string Language => "csharp";
        public override string FileExtension => ".cs";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => size switch
        {
            1 => "byte", 2 => "ushort", 4 => "uint", 8 => "ulong",
            _ => "uint"
        };

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"public const {type} {name}_ADDR = {addr};";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"public const int {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit};";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"public const uint {name}_START = {start};\npublic const uint {name}_END = {end};\npublic const uint {name}_SIZE = {size};";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"public const uint {name}_BASE = {baseAddr};";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"public const uint {reg}_ADDR = {absAddr};";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"public const int INT_{name} = {vector};  // {desc}";
    }
}
