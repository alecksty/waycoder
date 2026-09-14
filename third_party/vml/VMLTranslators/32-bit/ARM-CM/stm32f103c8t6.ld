/* STM32F103C8T6 链接脚本 */
MEMORY
{
    FLASH (rx)  : ORIGIN = 0x08000000, LENGTH = 64K
    RAM   (rwx) : ORIGIN = 0x20000000, LENGTH = 20K
}

SECTIONS
{
    .vectors : ALIGN(4)
    {
        KEEP(*(.vectors))
    } > FLASH

    .text : ALIGN(4)
    {
        *(.text)
        *(.text.*)
    } > FLASH

    .rodata : ALIGN(4)
    {
        *(.rodata)
        *(.rodata.*)
    } > FLASH

    .data : ALIGN(4)
    {
        _sdata = .;
        *(.data)
        *(.data.*)
        _edata = .;
    } > RAM AT > FLASH

    .bss : ALIGN(4)
    {
        _sbss = .;
        *(.bss)
        *(.bss.*)
        _ebss = .;
    } > RAM
}
