class FileIO {
    static void Main() {
        asm("CALL shared_file_test");
        asm("LOAD R0 #100");
        asm("SYSCALL 3");
    }
}
