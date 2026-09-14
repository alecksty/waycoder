// VML Lua Runtime — Metamethod dispatch for arithmetic operators
// MCU-compatible: no GC, no threads, no dynamic loading
// __stdcall convention: callee cleans stack, return in R0
// Opcode: 1=ADD, 2=SUB, 3=MUL, 4=DIV, 5=MOD, 6=POW

/// lua_binop(op, a, b) — arithmetic with metamethod dispatch placeholder
/// If 'a' has a metatable with the corresponding __add/__sub/etc, call metamethod.
/// Otherwise perform standard integer operation.
__stdcall int lua_binop(int op, int a, int b)
{
    // Metamethod check: Lua tables store metatable pointer at [a-4]
    int* mt_ptr = (int*)(a - 4);
    int metatable = *mt_ptr;
    if (metatable != 0)
    {
        // TODO: full metamethod lookup via lua_rawget(metatable, "__add")
        // For now, fall through to standard op
    }

    if (op == 1) return a + b;
    if (op == 2) return a - b;
    if (op == 3) return a * b;
    if (op == 4) { if (b == 0) return 0; return a / b; }
    if (op == 5) { if (b == 0) return 0; return a % b; }
    if (op == 6) {
        int r = 1; int e = b;
        if (e < 0) return 0;
        while (e > 0) { r *= a; e--; }
        return r;
    }
    return 0;
}

/// lua_compare_mt(op, a, b) — comparison (op: 1=EQ, 2=LT, 3=LE). Returns 1/0.
__stdcall int lua_compare_mt(int op, int a, int b)
{
    if (op == 1) return a == b ? 1 : 0;
    if (op == 2) return a < b ? 1 : 0;
    if (op == 3) return a <= b ? 1 : 0;
    return 0;
}

/// lua_print1(val) — MCU single value output (SYSCALL 6 for int)
__stdcall void lua_print1(int val)
{
    asm("SYSCALL #6");
}

/// lua_sleep(ms) — MCU delay (SYSCALL 52)
__stdcall void lua_sleep(int ms)
{
    asm("SYSCALL #52");
}

/// lua_clear() — MCU clear screen (SYSCALL 74)
__stdcall void lua_clear(void)
{
    asm("SYSCALL #74");
}

/// lua_print_str(str) — MCU string output (SYSCALL 1)
__stdcall void lua_print_str(const char* str)
{
    asm("SYSCALL #1");
}

/// lua_print() — print tail hook (inline output handled by compiler codegen)
__stdcall void lua_print(void)
{
    // no-op: output handled inline by Lua compiler before CALL
}
