using GenDev.Models;

namespace GenDev.Generators
{
    public class BasicCodeGenerator : GeneratorBase
    {
        public override string Language => "basic";
        public override string FileExtension => ".bas";
        protected override string CommentPrefix => "'";

        protected override string GetTypeForSize(int size) => "INTEGER";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"CONST {name}_ADDR = {addr}";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"CONST {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"CONST {name}_START = {start}\nCONST {name}_END = {end}\nCONST {name}_SIZE = {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"CONST {name}_BASE = {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"CONST {reg}_ADDR = {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"' INT_{name} = {vector}  ' {desc}";
    }
}
