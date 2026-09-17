using GenDev.Models;

namespace GenDev.Generators
{
    public class SwiftCodeGenerator : GeneratorBase
    {
        public override string Language => "swift";
        public override string FileExtension => ".swift";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => "Int";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"static let {name}_ADDR: {type} = {addr}";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"static let {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"static let {name}_START = {start}\nstatic let {name}_END = {end}\nstatic let {name}_SIZE = {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"static let {name}_BASE = {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"static let {reg}_ADDR = {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"static let INT_{name} = {vector}  // {desc}";
    }
}
