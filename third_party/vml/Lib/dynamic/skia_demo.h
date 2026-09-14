/* skia_demo.h — Skia-style C API for ImportDynamic testing */

#ifndef SKIA_DEMO_H
#define SKIA_DEMO_H

extern int  sk_test_version(void);
extern int  sk_test_rect(int x, int y, int w, int h);
extern int  sk_test_color(int r, int g, int b, int a);
extern int  sk_test_add(int a, int b);
extern void sk_test_flush(void);

#endif
