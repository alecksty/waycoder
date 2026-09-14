; PIC16F84A BIOS - VML SYSCALL via software UART (bit-bang, 9600 baud)
        processor 16f84a
        radix dec

        include "p16f84a.inc"

; Bit-bang UART on PORTB
TX_PIN  EQU 0
RX_PIN  EQU 1
BAUD    EQU 33          ; 104µs per bit @ 4MHz (delay = 3N+5 ~ 104)

        cblock 0x0C
_str_ptr: 1
_fsr_save: 1
_tx_byte: 1
_rx_byte: 1
_bit_cnt: 1
_dly_cnt: 1
_rnd_state: 1
        endc

;----------------------------------------------------------------------
; __bios_init
;----------------------------------------------------------------------
        global __bios_init
__bios_init:
        banksel TRISB
        bcf TRISB, TX_PIN
        bsf TRISB, RX_PIN
        banksel PORTB
        return

;----------------------------------------------------------------------
; __syscall_1  - OutputString (R0=string address in W)
;----------------------------------------------------------------------
        global __syscall_1
__syscall_1:
        movwf _str_ptr
        movf FSR, W
        movwf _fsr_save
        movf _str_ptr, W
        movwf FSR
1:      movf INDF, W
        iorlw 0
        btfsc STATUS, Z
        goto 2f
        call __uart_putchar
        incf FSR, F
        goto 1b
2:      movf _fsr_save, W
        movwf FSR
        return

;----------------------------------------------------------------------
; __syscall_3  - Exit (halt)
;----------------------------------------------------------------------
        global __syscall_3
__syscall_3:
        clrwdt
        sleep

;----------------------------------------------------------------------
; __syscall_4  - OutputChar (R0=char in W)
;----------------------------------------------------------------------
        global __syscall_4
__syscall_4:
        call __uart_putchar
        return

;----------------------------------------------------------------------
; __syscall_5  - InputChar (result in W)
;----------------------------------------------------------------------
        global __syscall_5
__syscall_5:
        call __uart_getchar
        return

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR)
;----------------------------------------------------------------------
        global __syscall_50
__syscall_50:
        movf _rnd_state, W
        bcf STATUS, C
        rlf _rnd_state, F
        btfss STATUS, C
        goto 1f
        movlw 0x1D
        xorwf _rnd_state, F
1:      movf _rnd_state, W
        return

;----------------------------------------------------------------------
; Software UART bit-bang @ 9600 baud
;----------------------------------------------------------------------
__uart_putchar:
        movwf _tx_byte
        banksel PORTB
        bcf PORTB, TX_PIN
        call __uart_delay
        movlw 8
        movwf _bit_cnt
1:      btfss _tx_byte, 0
        goto 2f
        banksel PORTB
        bsf PORTB, TX_PIN
        goto 3f
2:      banksel PORTB
        bcf PORTB, TX_PIN
3:      rrf _tx_byte, F
        call __uart_delay
        decfsz _bit_cnt, F
        goto 1b
        banksel PORTB
        bsf PORTB, TX_PIN
        call __uart_delay
        return

__uart_getchar:
        banksel PORTB
1:      btfsc PORTB, RX_PIN
        goto 1b
        movlw BAUD/2
        movwf _dly_cnt
        call __uart_delay_w
        movlw 8
        movwf _bit_cnt
        clrf _rx_byte
2:      movlw BAUD
        movwf _dly_cnt
        call __uart_delay_w
        bcf STATUS, C
        btfsc PORTB, RX_PIN
        bsf STATUS, C
        rrf _rx_byte, F
        decfsz _bit_cnt, F
        goto 2b
        movlw BAUD
        movwf _dly_cnt
        call __uart_delay_w
        movf _rx_byte, W
        return

__uart_delay:
        movlw BAUD
__uart_delay_w:
        movwf _dly_cnt
1:      decfsz _dly_cnt, F
        goto 1b
        return

        end
