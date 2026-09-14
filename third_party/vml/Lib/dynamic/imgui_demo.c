/* imgui_demo.c — Minimal ImGui-style DLL for ImportDynamic testing */

__declspec(dllexport) int igCreateContext(int reserved)
{
    return 1; /* success, returns ctx handle */
}

__declspec(dllexport) void igDestroyContext(int ctx)
{
    (void)ctx;
}

__declspec(dllexport) int igGetVersion(void)
{
    return 19290; /* v1.92.9 */
}

__declspec(dllexport) int igAdd(int a, int b)
{
    return a + b;
}

__declspec(dllexport) int igMul(int a, int b)
{
    return a * b;
}

__declspec(dllexport) void igShowDemoWindow(int show)
{
    (void)show;
}
