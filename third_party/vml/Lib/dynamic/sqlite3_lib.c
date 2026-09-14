// Lib/dynamic/sqlite3_lib.c — SQLite3 VML 仿真库 (纯 SYSCALL 输出, 绕过 printf vararg)
static int step_counter;

// OutputChar: MOVE R0, c; SYSCALL #4
static void outc(char c)      { int x = c; asm("SYSCALL #4"); }
static void outstr(const char* s) { if (s) for (int i=0; s[i]; i++) outc(s[i]); }
static void outint(int n)     { /* 用 printf %d — %d 已验证可用 */ }

int sq_open(const char* filename, int* db) {
    outstr("[SQLite] open(");
    outstr(filename);
    outstr(") -> db=1\n");
    *db = 1; step_counter = 0; return 0;
}
int sq_close(int db)          { outstr("[SQLite] close()\n"); return 0; }
int sq_exec(int db, const char* sql) {
    outstr("[SQLite] exec("); outstr(sql); outstr(") -> OK\n"); return 0;
}
int sq_prepare(int db, const char* sql, int* stmt) {
    outstr("[SQLite] prepare("); outstr(sql); outstr(")\n"); *stmt = 100; return 0;
}
int sq_step(int stmt) {
    if (step_counter < 2) { step_counter++; outstr("[SQLite] step -> ROW\n"); return 100; }
    outstr("[SQLite] step -> DONE\n"); return 101;
}
int sq_finalize(int stmt)     { outstr("[SQLite] finalize()\n"); return 0; }
int sq_column_text(int stmt, int col) { outstr("[SQLite] column_text()\n"); return 0; }
const char* sq_errmsg(int db) { outstr("[SQLite] errmsg()\n"); return 0; }
