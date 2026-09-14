using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    // Palette addresses — fixed safe locations below VGA (0xA0000), above heap
    const int QB_PALETTE_ADDR    = 0x9F000; // 16-color EGA palette (48 bytes)
    const int QB_PALETTE13_ADDR  = 0x9F100; // 256-color palette (768 bytes)

    string newLabel() => GenerateLabel();
}
