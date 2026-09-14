; x86 BIOS - VML SYSCALL via IBM PC BIOS interrupts
; Target: IBM PC 5150 / 8086
; Assembler: NASM
; Uses INT 10h for display, INT 16h for keyboard

        org 0x8000

        global __bios_init
        global __syscall_1
        global __syscall_3
        global __syscall_4
        global __syscall_5
        global __syscall_50

; BIOS data area
RNDSTATE dw 1

;----------------------------------------------------------------------
; __bios_init - Set video mode 80x25 text
;----------------------------------------------------------------------
__bios_init:
        mov ax, 0x0003      ; AH=0, AL=3 -> 80x25 text
        int 0x10
        ret

;----------------------------------------------------------------------
; __syscall_1 - OutputString (R0=string address in DS:SI)
;----------------------------------------------------------------------
__syscall_1:
        push si
        push ax
        mov si, ax
        cld
1:      lodsb
        or al, al
        jz 2f
        mov ah, 0x0E       ; INT 10h/AH=0Eh: Teletype output
        int 0x10
        jmp 1b
2:      pop ax
        pop si
        ret

;----------------------------------------------------------------------
; __syscall_4 - OutputChar (R0=char in AL)
;----------------------------------------------------------------------
__syscall_4:
        push ax
        mov ah, 0x0E       ; INT 10h/AH=0Eh: Teletype output
        int 0x10
        pop ax
        ret

;----------------------------------------------------------------------
; __syscall_5 - InputChar (R0=result in AL)
;----------------------------------------------------------------------
__syscall_5:
        mov ah, 0x00       ; INT 16h/AH=00h: Get keystroke
        int 0x16
        ret                 ; AL = ASCII char, AH = scan code

;----------------------------------------------------------------------
; __syscall_3 - Exit (halt)
;----------------------------------------------------------------------
__syscall_3:
        cli
1:      hlt
        jmp 1b

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR)
;----------------------------------------------------------------------
__syscall_50:
        push bx
        mov ax, [RNDSTATE]
        mov bx, ax
        shl ax, 1
        test bx, 0x8000
        jz 1f
        xor ax, 0x1D00
1:      mov [RNDSTATE], ax
        pop bx
        ret
