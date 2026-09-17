using GenDev.Models;

namespace GenDev.Generators
{
    public class JavaCodeGenerator : GeneratorBase
    {
        public override string Language => "java";
        public override string FileExtension => ".java";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => size switch
        {
            1 => "byte", 2 => "short", 4 => "int", 8 => "long",
            _ => "int"
        };

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"public static final int {name}_ADDR = {addr};";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"public static final int {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit};";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"public static final int {name}_START = {start};\npublic static final int {name}_END = {end};\npublic static final int {name}_SIZE = {size};";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"public static final int {name}_BASE = {baseAddr};";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"public static final int {reg}_ADDR = {absAddr};";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"public static final int INT_{name} = {vector};  // {desc}";
    }
}
