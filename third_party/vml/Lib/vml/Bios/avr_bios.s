; AVR ATmega328P BIOS - VML SYSCALL implementations via UART
; Uses USART0 at 115200 baud

; Hardware init
.global __bios_init
__bios_init:
    ; Set baud rate 115200 @ 16MHz: UBRR0 = 8
    ldi r16, 8
    sts UBRR0L, r16
    clr r16
    sts UBRR0H, r16
    ; Enable TX, RX
    ldi r16, (1<<3)|(1<<4)  ; TXEN=3, RXEN=4
    sts UCSR0B, r16
    ; 8N1
    ldi r16, (1<<2)|(1<<1)  ; UCSZ01=2, UCSZ00=1
    sts UCSR0C, r16
    ret

; SYSCALL handlers
.global __syscall_1
__syscall_1:  ; OutputString - R0=string address
    push ZH
    push ZL
    push r16
    movw ZL, r0
1:  ld r16, Z+
    cpi r16, 0
    breq 2f
    rcall __uart_putchar
    rjmp 1b
2:  pop r16
    pop ZL
    pop ZH
    ret

.global __syscall_4
__syscall_4:  ; OutputChar - R0=char
    push r16
    mov r16, r0
    rcall __uart_putchar
    pop r16
    ret

.global __syscall_5
__syscall_5:  ; InputChar - R0=result
    push r16
    rcall __uart_getchar
    mov r0, r16
    pop r16
    ret

.global __syscall_3
__syscall_3:  ; Exit
    cli
1:  sleep
    rjmp 1b

.global __syscall_50
__syscall_50: ; Random - simple LFSR
    push r16
    push r17
    lds r16, __random_state
    lds r17, __random_state+1
    ; LFSR: x[n+1] = (x[n] << 1) ^ (x[n] & 0x80 ? 0x1D : 0)
    lsl r16
    rol r17
    brcc 1f
    ldi r16, 0x1D
    eor r17, r16
1:  sts __random_state, r16
    sts __random_state+1, r17
    mov r0, r16
    pop r17
    pop r16
    ret

.global __syscall_52
__syscall_52: ; Sleep - R0=ms (approx)
    push r16
    push r17
    push r18
    mov r16, r0
1:  ldi r17, 0
2:  ldi r18, 0
3:  dec r18
    brne 3b
    dec r17
    brne 2b
    dec r16
    brne 1b
    pop r18
    pop r17
    pop r16
    ret

; UART functions
__uart_putchar:
    lds r16, UCSR0A
    sbrs r16, 5        ; wait for UDRE
    rjmp __uart_putchar
    sts UDR0, r16
    ret

__uart_getchar:
    lds r16, UCSR0A
    sbrs r16, 7        ; wait for RXC
    rjmp __uart_getchar
    lds r16, UDR0
    ret

.data
__random_state: .word 1
