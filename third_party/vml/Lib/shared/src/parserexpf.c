/* parserexpf.c — VML shared library: floating-point expression parser
 * Supports: + - * / ( ) and double literals (with decimal point)
 * Returns: double result of the expression
 * Usage: double result = parserexpf("2.5+3*1.5"); // → 7.0
 */

#ifndef NULL
#define NULL ((void*)0)
#endif

/* ── Lexer state ─────────────────────────────── */
static const char *g_pos;

static int is_digit(char c) {
    return c >= '0' && c <= '9';
}

static void skip_spaces(void) {
    while (*g_pos == ' ' || *g_pos == '\t' || *g_pos == '\n' || *g_pos == '\r')
        g_pos++;
}

/* ── Forward declarations ────────────────────── */
static double parse_expr_f(void);
static double parse_term_f(void);
static double parse_factor_f(void);

/* ── Factor: number | (expr) | -factor ───────── */
static double parse_factor_f(void) {
    double val = 0.0;
    skip_spaces();

    if (*g_pos == '(') {
        g_pos++;
        val = parse_expr_f();
        skip_spaces();
        if (*g_pos == ')') g_pos++;
        return val;
    }

    if (*g_pos == '-') {
        g_pos++;
        return -parse_factor_f();
    }

    /* Integer part */
    while (is_digit(*g_pos)) {
        val = val * 10.0 + (*g_pos - '0');
        g_pos++;
    }

    /* Fractional part: .digits */
    if (*g_pos == '.') {
        g_pos++;
        double frac = 0.1;
        while (is_digit(*g_pos)) {
            val += (*g_pos - '0') * frac;
            frac *= 0.1;
            g_pos++;
        }
    }

    return val;
}

/* ── Term: factor (*|/ factor)* ───────────────── */
static double parse_term_f(void) {
    double left = parse_factor_f();
    skip_spaces();
    while (*g_pos == '*' || *g_pos == '/') {
        char op = *g_pos;
        g_pos++;
        double right = parse_factor_f();
        if (op == '*')
            left = left * right;
        else {
            if (right == 0.0) return 0.0;
            left = left / right;
        }
        skip_spaces();
    }
    return left;
}

/* ── Expression: term (+|- term)* ─────────────── */
static double parse_expr_f(void) {
    double left = parse_term_f();
    skip_spaces();
    while (*g_pos == '+' || *g_pos == '-') {
        char op = *g_pos;
        g_pos++;
        double right = parse_term_f();
        if (op == '+')
            left = left + right;
        else
            left = left - right;
        skip_spaces();
    }
    return left;
}

/* ── Public API ───────────────────────────────── */
__stdcall double parserexpf(const char *expression) {
    if (expression == NULL) return 0.0;
    g_pos = expression;
    skip_spaces();
    if (*g_pos == 0) return 0.0;
    return parse_expr_f();
}
