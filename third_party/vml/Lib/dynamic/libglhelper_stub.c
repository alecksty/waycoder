// libglhelper_stub.c — 供测试用的最小桩 DLL
// 导出与 libglhelper 完全相同的符号，但函数体为空/返回默认值
// 用于验证 Windows 上 OpenGL FFI 链端到端工作
// 编译: cl /nologo /O2 /MD /LD /Fe:libglhelper.dll libglhelper_stub.c

__declspec(dllexport) int glh_init(int w, int h)
{
    (void)w; (void)h;
    return 0; // 返回第一个窗口 ID = 0
}

__declspec(dllexport) int glh_should_close(int ctx_id)
{
    (void)ctx_id;
    return 0; // 永不关闭
}

__declspec(dllexport) float glh_get_time(int ctx_id)
{
    (void)ctx_id;
    return 1.5f; // 固定时间
}

__declspec(dllexport) void glh_draw_cube(float time)
{
    (void)time;
}

__declspec(dllexport) void glh_swap(int ctx_id)
{
    (void)ctx_id;
}

__declspec(dllexport) void glh_destroy(int ctx_id)
{
    (void)ctx_id;
}
