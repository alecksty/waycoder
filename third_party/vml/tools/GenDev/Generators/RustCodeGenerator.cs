using GenDev.Models;

namespace GenDev.Generators
{
    public class RustCodeGenerator : GeneratorBase
    {
        public override string Language => "rust";
        public override string FileExtension => ".rs";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => size switch
        {
            1 => "u8", 2 => "u16", 4 => "u32", 8 => "u64",
            _ => "u32"
        };

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"pub const {name}_ADDR: {type} = {addr};";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"pub const {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT: u32 = {bit};";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"pub const {name}_START: u32 = {start};\npub const {name}_END: u32 = {end};\npub const {name}_SIZE: u32 = {size};";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"pub const {name}_BASE: u32 = {baseAddr};";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"pub const {reg}_ADDR: u32 = {absAddr};";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"pub const INT_{name}: u32 = {vector};  // {desc}";
    }
}
