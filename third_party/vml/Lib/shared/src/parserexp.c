#param lib("parserexpf")

/* parserexp.c — VML shared library: integer expression parser
 * Supports: + - * / ( ) and integer literals (decimal/hex)
 * Returns: integer result of the expression
 * Usage: int result = parserexp("2+3*4"); // → 14
 *
 * Compile: vmltool parserexp.c -o parserexp.vml --no-link
 */

#ifndef NULL
#define NULL ((void*)0)
#endif

/* ── Lexer state ─────────────────────────────── */
static const char *g_pos;
static int        g_current;

/* ── Character helpers ───────────────────────── */
static int is_digit(char c) {
    return c >= '0' && c <= '9';
}

static int is_hex_digit(char c) {
    return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}

static void skip_spaces(void) {
    while (*g_pos == ' ' || *g_pos == '\t' || *g_pos == '\n' || *g_pos == '\r')
        g_pos++;
}

/* ── Lexer: get next token ───────────────────── */
static void next_token(void) {
    skip_spaces();
    g_current = (unsigned char)*g_pos;
    if (g_current != 0) g_pos++;
}

/* ── Forward declarations ────────────────────── */
static int parse_expr(void);
static int parse_term(void);
static int parse_factor(void);

/* ── Factor: number | (expr) | -factor ───────── */
static int parse_factor(void) {
    int val = 0;
    skip_spaces();

    if (*g_pos == '(') {
        g_pos++;          /* skip ( */
        val = parse_expr();
        skip_spaces();
        if (*g_pos == ')') g_pos++; /* skip ) */
        return val;
    }

    if (*g_pos == '-') {
        g_pos++;          /* skip - */
        return -parse_factor();
    }

    /* Hex literal: 0xNN */
    if (*g_pos == '0' && (*(g_pos + 1) == 'x' || *(g_pos + 1) == 'X')) {
        g_pos += 2;
        while (is_hex_digit(*g_pos)) {
            char c = *g_pos;
            val = val * 16 + (c >= 'a' ? c - 'a' + 10 : c >= 'A' ? c - 'A' + 10 : c - '0');
            g_pos++;
        }
        return val;
    }

    /* Decimal number */
    while (is_digit(*g_pos)) {
        val = val * 10 + (*g_pos - '0');
        g_pos++;
    }
    return val;
}

/* ── Term: factor (*|/ factor)* ───────────────── */
static int parse_term(void) {
    int left = parse_factor();
    skip_spaces();
    while (*g_pos == '*' || *g_pos == '/') {
        char op = *g_pos;
        g_pos++;
        int right = parse_factor();
        if (op == '*')
            left = left * right;
        else {
            if (right == 0) return 0; /* division by zero → return 0 */
            left = left / right;
        }
        skip_spaces();
    }
    return left;
}

/* ── Expression: term (+|- term)* ─────────────── */
static int parse_expr(void) {
    int left = parse_term();
    skip_spaces();
    while (*g_pos == '+' || *g_pos == '-') {
        char op = *g_pos;
        g_pos++;
        int right = parse_term();
        if (op == '+')
            left = left + right;
        else
            left = left - right;
        skip_spaces();
    }
    return left;
}

/* ── Public API ───────────────────────────────── */
__stdcall int parserexp(const char *expression) {
    if (expression == NULL) return 0;
    g_pos = expression;
    skip_spaces();
    if (*g_pos == 0) return 0;
    int result = parse_expr();
    return result;
}

/* ── Simple test main (not linked in library) ───
int main() {
    // Quick test cases (expected: 14, 0, 42, 7, -5)
    int r1 = parserexp("2+3*4");
    int r2 = parserexp("2+3*4/0");
    int r3 = parserexp("(2+3)*(8+0)+2");
    int r4 = parserexp(" 3 + 4 ");
    int r5 = parserexp("-5");
    return 0;
}
*/
