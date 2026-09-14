/* skia_demo.c — Minimal Skia-style DLL for ImportDynamic testing */

__declspec(dllexport) int sk_test_version(void)
{
    return 11600; /* Skia milestone 116 */
}

__declspec(dllexport) int sk_test_rect(int x, int y, int w, int h)
{
    return w * h; /* area */
}

__declspec(dllexport) int sk_test_color(int r, int g, int b, int a)
{
    return (a << 24) | (r << 16) | (g << 8) | b;
}

__declspec(dllexport) int sk_test_add(int a, int b)
{
    return a + b;
}

__declspec(dllexport) void sk_test_flush(void)
{
}
