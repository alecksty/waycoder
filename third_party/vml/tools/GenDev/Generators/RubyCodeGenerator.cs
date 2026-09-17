using GenDev.Models;

namespace GenDev.Generators
{
    public class RubyCodeGenerator : GeneratorBase
    {
        public override string Language => "ruby";
        public override string FileExtension => ".rb";
        protected override string CommentPrefix => "#";

        protected override string GetTypeForSize(int size) => "Integer";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"{name}_ADDR = {addr}";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"{parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"{name}_START = {start}\n{name}_END = {end}\n{name}_SIZE = {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"{name}_BASE = {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"{reg}_ADDR = {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"INT_{name} = {vector}  # {desc}";
    }
}
