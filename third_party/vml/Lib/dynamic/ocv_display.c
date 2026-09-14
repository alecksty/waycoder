/* ocv_display.c — VML OpenCV 仿真层 (纯 Win32 GDI, 无外部依赖)
 * 编译 (MSVC): cl /nologo /O2 /MD /LD /Fe:ocv_display.dll ocv_display.c /link user32.lib gdi32.lib
 * 编译 (MinGW): gcc -shared -o ocv_display.dll ocv_display.c -lgdi32 -luser32
 */

#ifndef UNICODE
#define UNICODE
#endif
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#define MAX_WINDOWS 4
#define MAX_IMAGES 16

/* 颜色结构 (BGR, 每通道 0-255) */
typedef struct { int r, g, b; } Color;

/* 绘图命令类型 */
typedef enum { CMD_RECT, CMD_CIRCLE, CMD_LINE, CMD_TEXT } CmdType;

/* 绘图命令 */
typedef struct {
    CmdType type;
    int x1, y1, x2, y2;
    int r, g, b;
    int thickness;
    char text[256];
} DrawCmd;

/* 窗口状态 */
typedef struct {
    HWND hwnd;
    HDC hdc;
    HBITMAP backbuf;
    HDC backbuf_dc;
    int width, height;
    int should_close;
    DrawCmd* commands;
    int cmd_count;
    int cmd_capacity;
} WindowState;

static WindowState _windows[MAX_WINDOWS];
static int _window_count = 0;
static COLORREF _bg_color = 0x1A1A2E; /* 深蓝灰背景 */

/* 添加绘图命令到窗口 */
static void add_cmd(WindowState* ws, CmdType type,
    int x1, int y1, int x2, int y2,
    int r, int g, int b, int thickness, const char* text)
{
    if (ws->cmd_count >= ws->cmd_capacity) {
        ws->cmd_capacity = ws->cmd_capacity == 0 ? 64 : ws->cmd_capacity * 2;
        ws->commands = realloc(ws->commands, ws->cmd_capacity * sizeof(DrawCmd));
    }
    DrawCmd* cmd = &ws->commands[ws->cmd_count++];
    cmd->type = type;
    cmd->x1 = x1; cmd->y1 = y1;
    cmd->x2 = x2; cmd->y2 = y2;
    cmd->r = r; cmd->g = g; cmd->b = b;
    cmd->thickness = thickness;
    if (text) { strncpy(cmd->text, text, 255); cmd->text[255] = 0; }
    else cmd->text[0] = 0;
}

/* 渲染所有绘图命令到后台缓冲 */
static void render_commands(WindowState* ws)
{
    RECT rect = {0, 0, ws->width, ws->height};
    HBRUSH bg = CreateSolidBrush(_bg_color);
    FillRect(ws->backbuf_dc, &rect, bg);
    DeleteObject(bg);

    for (int i = 0; i < ws->cmd_count; i++) {
        DrawCmd* c = &ws->commands[i];
        COLORREF color = RGB(c->r, c->g, c->b);
        HPEN pen = CreatePen(PS_SOLID, c->thickness > 0 ? c->thickness : 2, color);
        HBRUSH brush = CreateSolidBrush(color);
        HPEN oldPen = SelectObject(ws->backbuf_dc, pen);
        HBRUSH oldBrush = SelectObject(ws->backbuf_dc, GetStockObject(NULL_BRUSH));

        switch (c->type) {
        case CMD_RECT:
            SelectObject(ws->backbuf_dc, brush);
            Rectangle(ws->backbuf_dc, c->x1, c->y1, c->x2, c->y2);
            SelectObject(ws->backbuf_dc, GetStockObject(NULL_BRUSH));
            break;
        case CMD_CIRCLE:
            SelectObject(ws->backbuf_dc, brush);
            Ellipse(ws->backbuf_dc,
                c->x1 - c->x2, c->y1 - c->x2,
                c->x1 + c->x2, c->y1 + c->x2);
            SelectObject(ws->backbuf_dc, GetStockObject(NULL_BRUSH));
            break;
        case CMD_LINE:
            MoveToEx(ws->backbuf_dc, c->x1, c->y1, NULL);
            LineTo(ws->backbuf_dc, c->x2, c->y2);
            break;
        case CMD_TEXT: {
            /* 文字用半透明背景矩形 */
            RECT tr = {c->x1 - 4, c->y1 - 2, c->x1 + 600, c->y1 + 32};
            SetBkMode(ws->backbuf_dc, TRANSPARENT);
            SelectObject(ws->backbuf_dc, GetStockObject(NULL_BRUSH));
            HFONT font = CreateFontW(28, 0, 0, 0, FW_BOLD, 0, 0, 0,
                DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
                CLEARTYPE_QUALITY, FF_DONTCARE, L"Consolas");
            HFONT oldFont = SelectObject(ws->backbuf_dc, font);
            SetTextColor(ws->backbuf_dc, color);

            /* 转宽字符 */
            WCHAR wtext[256];
            MultiByteToWideChar(CP_UTF8, 0, c->text, -1, wtext, 256);
            DrawTextW(ws->backbuf_dc, wtext, -1, &tr, DT_LEFT | DT_TOP);

            SelectObject(ws->backbuf_dc, oldFont);
            DeleteObject(font);
            break;
        }
        }

        SelectObject(ws->backbuf_dc, oldPen);
        SelectObject(ws->backbuf_dc, oldBrush);
        DeleteObject(pen);
        DeleteObject(brush);
    }
}

static void paint_window(WindowState* ws)
{
    PAINTSTRUCT ps;
    HDC hdc = BeginPaint(ws->hwnd, &ps);
    BitBlt(hdc, 0, 0, ws->width, ws->height, ws->backbuf_dc, 0, 0, SRCCOPY);
    EndPaint(ws->hwnd, &ps);
}

static LRESULT CALLBACK wnd_proc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp)
{
    switch (msg) {
    case WM_PAINT:
        for (int i = 0; i < _window_count; i++) {
            if (_windows[i].hwnd == hwnd) {
                paint_window(&_windows[i]);
                break;
            }
        }
        return 0;
    case WM_CLOSE:
    case WM_DESTROY:
        for (int i = 0; i < _window_count; i++) {
            if (_windows[i].hwnd == hwnd) {
                _windows[i].should_close = 1;
                break;
            }
        }
        return 0;
    case WM_ERASEBKGND:
        return 1; /* 避免闪烁 */
    }
    return DefWindowProc(hwnd, msg, wp, lp);
}

/* ====== 导出函数 ====== */

/* 创建图像 (分配内存, 返回 handle; 这里简化为标志) */
__declspec(dllexport) int ocv_imread(const char* filename)
{
    /* 返回一个虚拟 handle, 表示 "空图像" */
    return 1;
}

__declspec(dllexport) int ocv_imwrite(const char* filename, int img)
{
    (void)filename; (void)img;
    return 0;
}

/* 创建窗口并显示 */
__declspec(dllexport) int ocv_imshow(const char* title, int img)
{
    (void)img;
    if (_window_count >= MAX_WINDOWS) return -1;

    /* 注册窗口类 (仅一次) */
    static int class_registered = 0;
    if (!class_registered) {
        WNDCLASSW wc = {0};
        wc.lpfnWndProc = wnd_proc;
        wc.hInstance = GetModuleHandle(NULL);
        wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
        wc.lpszClassName = L"VML_OpenCV_Window";
        wc.style = CS_OWNDC;
        RegisterClassW(&wc);
        class_registered = 1;
    }

    int w = 800, h = 600;

    /* 转宽字符 */
    WCHAR wtitle[256];
    MultiByteToWideChar(CP_UTF8, 0, title, -1, wtitle, 256);

    HWND hwnd = CreateWindowExW(0, L"VML_OpenCV_Window", wtitle,
        WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, w, h,
        NULL, NULL, GetModuleHandle(NULL), NULL);
    if (!hwnd) return -3;

    HDC hdc = GetDC(hwnd);

    /* 创建后台缓冲 */
    HDC backbuf_dc = CreateCompatibleDC(hdc);
    HBITMAP backbuf = CreateCompatibleBitmap(hdc, w, h);
    SelectObject(backbuf_dc, backbuf);

    int id = _window_count;
    _windows[id].hwnd = hwnd;
    _windows[id].hdc = hdc;
    _windows[id].backbuf = backbuf;
    _windows[id].backbuf_dc = backbuf_dc;
    _windows[id].width = w;
    _windows[id].height = h;
    _windows[id].should_close = 0;
    _windows[id].commands = NULL;
    _windows[id].cmd_count = 0;
    _windows[id].cmd_capacity = 0;
    _window_count++;

    ShowWindow(hwnd, SW_SHOW);
    UpdateWindow(hwnd);

    return id;
}

__declspec(dllexport) int ocv_waitkey(int delay_ms)
{
    if (_window_count == 0) return 27; /* ESC */

    /* 泵送消息 */
    MSG msg;
    DWORD start = GetTickCount();
    while (1) {
        /* 非阻塞检查消息 */
        while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }

        /* 检查是否有窗口应关闭 */
        int any_close = 0;
        for (int i = 0; i < _window_count; i++) {
            if (_windows[i].should_close) any_close = 1;
        }
        if (any_close) return 27; /* ESC */

        /* 检查延时 */
        if (delay_ms > 0) {
            DWORD elapsed = GetTickCount() - start;
            if (elapsed >= (DWORD)delay_ms) break;
        }
        if (delay_ms == 0) break; /* 不等待 */

        Sleep(10);
    }

    /* 处理后刷新所有窗口 */
    for (int i = 0; i < _window_count; i++) {
        render_commands(&_windows[i]);
        InvalidateRect(_windows[i].hwnd, NULL, FALSE);
        UpdateWindow(_windows[i].hwnd);
    }

    return 0;
}

/* 绘图函数 — 记录命令, 下次 waitkey 时渲染 */
__declspec(dllexport) void ocv_rectangle(int img, int x1, int y1, int x2, int y2,
    int r, int g, int b, int thickness)
{
    (void)img;
    for (int i = 0; i < _window_count; i++) {
        add_cmd(&_windows[i], CMD_RECT, x1, y1, x2, y2, r, g, b, thickness, NULL);
    }
}

__declspec(dllexport) void ocv_circle(int img, int cx, int cy, int radius,
    int r, int g, int b, int thickness)
{
    (void)img;
    for (int i = 0; i < _window_count; i++) {
        add_cmd(&_windows[i], CMD_CIRCLE, cx, cy, radius, 0, r, g, b, thickness, NULL);
    }
}

__declspec(dllexport) void ocv_line(int img, int x1, int y1, int x2, int y2,
    int r, int g, int b, int thickness)
{
    (void)img;
    for (int i = 0; i < _window_count; i++) {
        add_cmd(&_windows[i], CMD_LINE, x1, y1, x2, y2, r, g, b, thickness, NULL);
    }
}

__declspec(dllexport) void ocv_puttext(int img, const char* text, int x, int y,
    int r, int g, int b)
{
    (void)img;
    for (int i = 0; i < _window_count; i++) {
        add_cmd(&_windows[i], CMD_TEXT, x, y, 0, 0, r, g, b, 0, text);
    }
}

/* 其他桩函数 (保持兼容) */
__declspec(dllexport) void ocv_cvtcolor(int src, int dst, int code) { (void)src; (void)dst; (void)code; }
__declspec(dllexport) void ocv_resize(int src, int dst, int w, int h) { (void)src; (void)dst; (void)w; (void)h; }
__declspec(dllexport) int  ocv_width(int img) { (void)img; return 800; }
__declspec(dllexport) int  ocv_height(int img) { (void)img; return 600; }
__declspec(dllexport) int  ocv_channels(int img) { (void)img; return 3; }
__declspec(dllexport) void ocv_blur(int src, int dst, int ksize) { (void)src; (void)dst; (void)ksize; }
__declspec(dllexport) void ocv_canny(int src, int dst, int t1, int t2) { (void)src; (void)dst; (void)t1; (void)t2; }
__declspec(dllexport) void ocv_threshold(int src, int dst, int thresh, int maxval, int type) { (void)src; (void)dst; (void)thresh; (void)maxval; (void)type; }
__declspec(dllexport) int  ocv_facedetect(int img, const char* cascade) { (void)img; (void)cascade; return 0; }

__declspec(dllexport) void ocv_release(int img)
{
    (void)img;
    for (int i = 0; i < _window_count; i++) {
        if (_windows[i].commands) free(_windows[i].commands);
        if (_windows[i].backbuf) DeleteObject(_windows[i].backbuf);
        if (_windows[i].backbuf_dc) DeleteDC(_windows[i].backbuf_dc);
        if (_windows[i].hdc) ReleaseDC(_windows[i].hwnd, _windows[i].hdc);
        if (_windows[i].hwnd) DestroyWindow(_windows[i].hwnd);
        memset(&_windows[i], 0, sizeof(WindowState));
    }
    _window_count = 0;
}

/* ====== 零参数演示 (绕过 FFI 字符串限制) ====== */

__declspec(dllexport) int ocv_demo(void)
{
    /* 创建窗口 */
    int win = ocv_imshow("VML OpenCV Demo - GDI", 0);

    /* 蓝色矩形 (x=50, y=50, w=150, h=100) */
    ocv_rectangle(0, 50, 50, 200, 150, 255, 100, 50, 3);

    /* 红色矩形 (x=250, y=80) */
    ocv_rectangle(0, 250, 80, 430, 170, 50, 50, 255, -1);

    /* 绿色圆 (cx=400, cy=300, r=60) */
    ocv_circle(0, 400, 300, 60, 50, 255, 50, 2);

    /* 黄色填充圆 (cx=150, cy=350, r=40) */
    ocv_circle(0, 150, 350, 40, 255, 255, 50, -1);

    /* 青色对角线 */
    ocv_line(0, 60, 400, 350, 250, 255, 255, 100, 2);

    /* 白色水平线 */
    ocv_line(0, 500, 100, 700, 100, 255, 255, 255, 1);

    /* 品红十字线 */
    ocv_line(0, 300, 450, 500, 450, 255, 50, 255, 4);

    /* 文字: 标题 */
    ocv_puttext(0, "Hello from VML OpenCV (GDI)!", 30, 500, 255, 255, 100);

    /* 文字: 帧率/坐标 */
    ocv_puttext(0, "Rectangle | Circle | Line | Text", 30, 540, 100, 255, 200);

    /* 文字: 底栏 */
    ocv_puttext(0, "Press ESC or close window to exit", 200, 420, 200, 200, 200);

    /* 消息循环: 等待关闭 */
    int key = 0;
    while (key != 27) {
        key = ocv_waitkey(100);
    }

    ocv_release(0);
    return 0;
}
