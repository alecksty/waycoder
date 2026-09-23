using VMLAssembler;

namespace BasicCompiler;

public partial class CodeGenerator
{
    // 调色板表原先写死在 0x9F000 / 0x9F100（"VGA 之下、堆之上的安全位置"）——
    // **已经全部拿掉**，改走 `SysVars` 登记表里的 `.data` 槽
    // （`Sys.Palette16` 48 字节 / `Sys.Palette256` 768 字节）。
    // ⚠ 那次的实测教训就记在 `UiPaletteLabel` 的注释里：写死在固定地址时**整张表读出全 0**。

    string newLabel() => GenerateLabel();
}
