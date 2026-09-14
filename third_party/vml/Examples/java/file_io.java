class FileIO {
    public static void main(String[] a) {
        asm("CALL shared_file_test");
        asm("LOAD R0 #100");
        asm("SYSCALL 3");
    }
}
