\ VML 文件操作扩展库 — Forth
: FOPEN ( addr len mode -- handle ) asm("SYSCALL 110") ;
: FCLOSE ( handle -- result ) asm("SYSCALL 111") ;
: FREAD ( handle buf count -- result ) asm("SYSCALL 112") ;
: FWRITE ( handle buf count -- result ) asm("SYSCALL 113") ;
: FSEEK ( handle offset -- result ) asm("SYSCALL 114") ;
: FTELL ( handle -- pos ) asm("SYSCALL 114") ;
