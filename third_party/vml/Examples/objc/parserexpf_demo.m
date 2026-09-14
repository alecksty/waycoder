#include <stdio.h>
extern double parserexpf(const char *e);
int main() { printf("Float: %f\n", parserexpf("2.5+3*1.5")); return 0; }
