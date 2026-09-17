using GenDev.Models;

namespace GenDev.Generators
{
    public class PascalCodeGenerator : GeneratorBase
    {
        public override string Language => "pascal";
        public override string FileExtension => ".pas";
        protected override string CommentPrefix => "//";  // Pascal uses { } or (* *) but // works too

        protected override string GetTypeForSize(int size) => "LongWord";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"const {name}_ADDR = {addr};";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"const {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit};";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"const {name}_START = {start};\nconst {name}_END = {end};\nconst {name}_SIZE = {size};";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"const {name}_BASE = {baseAddr};";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"const {reg}_ADDR = {absAddr};";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"const INT_{name} = {vector};  // {desc}";
    }
}
