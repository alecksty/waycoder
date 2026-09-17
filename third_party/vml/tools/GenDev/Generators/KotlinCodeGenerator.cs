using GenDev.Models;

namespace GenDev.Generators
{
    public class KotlinCodeGenerator : GeneratorBase
    {
        public override string Language => "kotlin";
        public override string FileExtension => ".kt";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => "Int";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"const val {name}_ADDR = {addr}";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"const val {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"const val {name}_START = {start}\nconst val {name}_END = {end}\nconst val {name}_SIZE = {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"const val {name}_BASE = {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"const val {reg}_ADDR = {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"const val INT_{name} = {vector}  // {desc}";
    }
}
