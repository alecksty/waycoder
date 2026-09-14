// ObjC file_io.m — File I/O Test

int main() {
    asm("CALL shared_file_test");
    asm("LOAD R0 #100");
    asm("SYSCALL 3");
    return 0;
}
