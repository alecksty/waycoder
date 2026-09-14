/* Signal handling implementation (OS mode) */
#include "signal.h"

/* ── signal: register a handler for a given signal ── */
void (*signal(int signum, void (*handler)(int)))(int)
{
    unsigned int* ivt = (unsigned int*)IVT_BASE;
    void (*old_handler)(int) = (void (*)(int))ivt[signum];
    ivt[signum] = (unsigned int)handler;
    return old_handler;
}

/* ── raise: send a signal to the current process ── */
int raise(int signum)
{
    /* Trigger software interrupt via INT instruction.
     * The runtime handler dispatches based on the vector number.
     * signum is passed via R0 register. */
    asm("MOVE R0, signum");
    asm("INT R0");
    return 0;
}
