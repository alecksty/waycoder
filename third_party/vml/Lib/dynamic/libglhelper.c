/* libglhelper.c — VML OpenGL 旋转立方体 (纯 Win32 + WGL, 无 GLFW 依赖)
 * 编译 (MSVC): cl /nologo /O2 /MD /LD /Fe:libglhelper.dll libglhelper.c /link opengl32.lib user32.lib gdi32.lib
 * 编译 (MinGW): gcc -shared -o libglhelper.dll libglhelper.c -lopengl32 -lgdi32 -luser32
 * 编译 (macOS): cc -dynamiclib -o libglhelper.dylib libglhelper.c -framework OpenGL -lglfw -DGL_SILENCE_DEPRECATION
 * 编译 (Linux): cc -shared -o libglhelper.so libglhelper.c -lGL -lglfw
 */

#ifdef _WIN32
#ifndef UNICODE
#define UNICODE
#endif
#include <windows.h>
#include <GL/gl.h>
#pragma comment(lib, "opengl32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")
#else
#include <GLFW/glfw3.h>
#endif

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

#ifdef _WIN32
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT
#endif

/* ========== 笔画字体 (跨平台共享) ========== */

typedef struct { float x1, y1, x2, y2; } StrokeSeg;

#define MAX_SEG_PER_CHAR 14

static int g_stroke_counts[128];
static StrokeSeg g_stroke_font[128][MAX_SEG_PER_CHAR];
static int g_font_loaded = 0;

static void set_seg(int ch, int i, float x1, float y1, float x2, float y2)
{
    g_stroke_font[ch][i].x1 = x1;
    g_stroke_font[ch][i].y1 = y1;
    g_stroke_font[ch][i].x2 = x2;
    g_stroke_font[ch][i].y2 = y2;
}

static void init_stroke_font(void)
{
    if (g_font_loaded) return;
    /* A */ g_stroke_counts['A']=5;
    set_seg('A',0, 0.0f,0.0f, 0.5f,1.0f); set_seg('A',1, 0.5f,1.0f, 1.0f,0.0f);
    set_seg('A',2, 0.17f,0.5f, 0.83f,0.5f); set_seg('A',3, 0.0f,0.0f, 0.17f,0.5f);
    set_seg('A',4, 0.83f,0.5f, 1.0f,0.0f);
    /* B */ g_stroke_counts['B']=8;
    set_seg('B',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('B',1, 0.0f,0.0f, 0.7f,0.0f);
    set_seg('B',2, 0.7f,0.0f, 0.85f,0.15f); set_seg('B',3, 0.85f,0.15f, 0.7f,0.35f);
    set_seg('B',4, 0.0f,0.5f, 0.7f,0.5f); set_seg('B',5, 0.7f,0.5f, 0.85f,0.65f);
    set_seg('B',6, 0.85f,0.65f, 0.7f,0.85f); set_seg('B',7, 0.7f,0.85f, 0.0f,1.0f);
    /* C */ g_stroke_counts['C']=5;
    set_seg('C',0, 1.0f,0.2f, 0.7f,0.0f); set_seg('C',1, 0.7f,0.0f, 0.0f,0.0f);
    set_seg('C',2, 0.0f,0.0f, 0.0f,1.0f); set_seg('C',3, 0.0f,1.0f, 0.7f,1.0f);
    set_seg('C',4, 0.7f,1.0f, 1.0f,0.8f);
    /* D */ g_stroke_counts['D']=6;
    set_seg('D',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('D',1, 0.0f,0.0f, 0.6f,0.1f);
    set_seg('D',2, 0.6f,0.1f, 0.8f,0.3f); set_seg('D',3, 0.8f,0.3f, 0.8f,0.7f);
    set_seg('D',4, 0.8f,0.7f, 0.6f,0.9f); set_seg('D',5, 0.6f,0.9f, 0.0f,1.0f);
    /* E */ g_stroke_counts['E']=4;
    set_seg('E',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('E',1, 0.0f,0.0f, 1.0f,0.0f);
    set_seg('E',2, 0.0f,0.5f, 0.7f,0.5f); set_seg('E',3, 0.0f,1.0f, 1.0f,1.0f);
    /* F */ g_stroke_counts['F']=3;
    set_seg('F',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('F',1, 0.0f,0.0f, 1.0f,0.0f);
    set_seg('F',2, 0.0f,0.5f, 0.7f,0.5f);
    /* G */ g_stroke_counts['G']=7;
    set_seg('G',0, 1.0f,0.2f, 0.7f,0.0f); set_seg('G',1, 0.7f,0.0f, 0.0f,0.0f);
    set_seg('G',2, 0.0f,0.0f, 0.0f,1.0f); set_seg('G',3, 0.0f,1.0f, 0.7f,1.0f);
    set_seg('G',4, 0.7f,1.0f, 1.0f,0.7f); set_seg('G',5, 1.0f,0.7f, 0.5f,0.5f);
    set_seg('G',6, 0.5f,0.5f, 1.0f,0.5f);
    /* H */ g_stroke_counts['H']=3;
    set_seg('H',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('H',1, 1.0f,0.0f, 1.0f,1.0f);
    set_seg('H',2, 0.0f,0.5f, 1.0f,0.5f);
    /* I */ g_stroke_counts['I']=3;
    set_seg('I',0, 0.0f,0.0f, 1.0f,0.0f); set_seg('I',1, 0.5f,0.0f, 0.5f,1.0f);
    set_seg('I',2, 0.0f,1.0f, 1.0f,1.0f);
    /* J */ g_stroke_counts['J']=4;
    set_seg('J',0, 0.3f,0.0f, 1.0f,0.0f); set_seg('J',1, 0.7f,0.0f, 0.7f,0.8f);
    set_seg('J',2, 0.7f,0.8f, 0.3f,1.0f); set_seg('J',3, 0.3f,1.0f, 0.0f,0.8f);
    /* K */ g_stroke_counts['K']=3;
    set_seg('K',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('K',1, 0.0f,0.4f, 1.0f,0.0f);
    set_seg('K',2, 0.0f,0.4f, 1.0f,1.0f);
    /* L */ g_stroke_counts['L']=2;
    set_seg('L',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('L',1, 0.0f,1.0f, 1.0f,1.0f);
    /* M */ g_stroke_counts['M']=4;
    set_seg('M',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('M',1, 0.0f,0.0f, 0.5f,0.6f);
    set_seg('M',2, 0.5f,0.6f, 1.0f,0.0f); set_seg('M',3, 1.0f,0.0f, 1.0f,1.0f);
    /* N */ g_stroke_counts['N']=3;
    set_seg('N',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('N',1, 0.0f,0.0f, 1.0f,1.0f);
    set_seg('N',2, 1.0f,0.0f, 1.0f,1.0f);
    /* O */ g_stroke_counts['O']=8;
    set_seg('O',0, 0.0f,0.2f,0.0f,0.8f); set_seg('O',1, 0.0f,0.2f,0.2f,0.0f);
    set_seg('O',2, 0.2f,0.0f,0.8f,0.0f); set_seg('O',3, 0.8f,0.0f,1.0f,0.2f);
    set_seg('O',4, 1.0f,0.2f,1.0f,0.8f); set_seg('O',5, 1.0f,0.8f,0.8f,1.0f);
    set_seg('O',6, 0.8f,1.0f,0.2f,1.0f); set_seg('O',7, 0.2f,1.0f,0.0f,0.8f);
    /* P */ g_stroke_counts['P']=5;
    set_seg('P',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('P',1, 0.0f,0.0f, 0.8f,0.0f);
    set_seg('P',2, 0.8f,0.0f, 1.0f,0.2f); set_seg('P',3, 1.0f,0.2f, 0.8f,0.5f);
    set_seg('P',4, 0.8f,0.5f, 0.0f,0.5f);
    /* Q */ g_stroke_counts['Q']=9;
    set_seg('Q',0, 0.0f,0.2f, 0.0f,0.8f); set_seg('Q',1, 0.0f,0.2f, 0.2f,0.0f);
    set_seg('Q',2, 0.2f,0.0f, 0.8f,0.0f); set_seg('Q',3, 0.8f,0.0f, 1.0f,0.2f);
    set_seg('Q',4, 1.0f,0.2f, 1.0f,0.8f); set_seg('Q',5, 1.0f,0.8f, 0.8f,1.0f);
    set_seg('Q',6, 0.8f,1.0f, 0.2f,1.0f); set_seg('Q',7, 0.2f,1.0f, 0.0f,0.8f);
    set_seg('Q',8, 0.6f,0.6f, 1.0f,1.0f);
    /* R */ g_stroke_counts['R']=6;
    set_seg('R',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('R',1, 0.0f,0.0f, 0.8f,0.0f);
    set_seg('R',2, 0.8f,0.0f, 1.0f,0.2f); set_seg('R',3, 1.0f,0.2f, 0.8f,0.5f);
    set_seg('R',4, 0.8f,0.5f, 0.0f,0.5f); set_seg('R',5, 0.5f,0.5f, 1.0f,1.0f);
    /* S */ g_stroke_counts['S']=8;
    set_seg('S',0, 1.0f,0.1f, 0.7f,0.0f); set_seg('S',1, 0.7f,0.0f, 0.0f,0.1f);
    set_seg('S',2, 0.0f,0.1f, 0.0f,0.4f); set_seg('S',3, 0.0f,0.4f, 0.3f,0.5f);
    set_seg('S',4, 0.3f,0.5f, 1.0f,0.6f); set_seg('S',5, 1.0f,0.6f, 1.0f,0.9f);
    set_seg('S',6, 1.0f,0.9f, 0.7f,1.0f); set_seg('S',7, 0.7f,1.0f, 0.0f,0.9f);
    /* T */ g_stroke_counts['T']=2;
    set_seg('T',0, 0.0f,0.0f, 1.0f,0.0f); set_seg('T',1, 0.5f,0.0f, 0.5f,1.0f);
    /* U */ g_stroke_counts['U']=5;
    set_seg('U',0, 0.0f,0.0f, 0.0f,0.8f); set_seg('U',1, 0.0f,0.8f, 0.3f,1.0f);
    set_seg('U',2, 0.3f,1.0f, 0.7f,1.0f); set_seg('U',3, 0.7f,1.0f, 1.0f,0.8f);
    set_seg('U',4, 1.0f,0.8f, 1.0f,0.0f);
    /* V */ g_stroke_counts['V']=2;
    set_seg('V',0, 0.0f,0.0f, 0.5f,1.0f); set_seg('V',1, 0.5f,1.0f, 1.0f,0.0f);
    /* W */ g_stroke_counts['W']=4;
    set_seg('W',0, 0.0f,0.0f, 0.0f,1.0f); set_seg('W',1, 0.0f,1.0f, 0.5f,0.5f);
    set_seg('W',2, 0.5f,0.5f, 1.0f,1.0f); set_seg('W',3, 1.0f,0.0f, 1.0f,1.0f);
    /* X */ g_stroke_counts['X']=2;
    set_seg('X',0, 0.0f,0.0f, 1.0f,1.0f); set_seg('X',1, 1.0f,0.0f, 0.0f,1.0f);
    /* Y */ g_stroke_counts['Y']=3;
    set_seg('Y',0, 0.0f,0.0f, 0.5f,0.5f); set_seg('Y',1, 1.0f,0.0f, 0.5f,0.5f);
    set_seg('Y',2, 0.5f,0.5f, 0.5f,1.0f);
    /* Z */ g_stroke_counts['Z']=3;
    set_seg('Z',0, 0.0f,0.0f, 1.0f,0.0f); set_seg('Z',1, 1.0f,0.0f, 0.0f,1.0f);
    set_seg('Z',2, 0.0f,1.0f, 1.0f,1.0f);
    /* 0 */ g_stroke_counts['0']=8;
    set_seg('0',0, 0.0f,0.2f, 0.0f,0.8f); set_seg('0',1, 0.0f,0.2f, 0.2f,0.0f);
    set_seg('0',2, 0.2f,0.0f, 0.8f,0.0f); set_seg('0',3, 0.8f,0.0f, 1.0f,0.2f);
    set_seg('0',4, 1.0f,0.2f, 1.0f,0.8f); set_seg('0',5, 1.0f,0.8f, 0.8f,1.0f);
    set_seg('0',6, 0.8f,1.0f, 0.2f,1.0f); set_seg('0',7, 0.2f,1.0f, 0.0f,0.8f);
    /* 1 */ g_stroke_counts['1']=3;
    set_seg('1',0, 0.3f,0.0f, 0.5f,0.0f); set_seg('1',1, 0.5f,0.0f, 0.5f,1.0f);
    set_seg('1',2, 0.2f,1.0f, 0.8f,1.0f);
    /* 2 */ g_stroke_counts['2']=6;
    set_seg('2',0, 0.1f,0.0f, 0.9f,0.0f); set_seg('2',1, 0.9f,0.0f, 1.0f,0.3f);
    set_seg('2',2, 1.0f,0.3f, 0.5f,0.5f); set_seg('2',3, 0.5f,0.5f, 0.0f,0.5f);
    set_seg('2',4, 0.0f,0.5f, 0.0f,1.0f); set_seg('2',5, 0.0f,1.0f, 1.0f,1.0f);
    /* 3 */ g_stroke_counts['3']=6;
    set_seg('3',0, 0.1f,0.0f, 0.9f,0.0f); set_seg('3',1, 0.9f,0.0f, 1.0f,0.3f);
    set_seg('3',2, 1.0f,0.3f, 0.5f,0.5f); set_seg('3',3, 0.9f,0.7f, 1.0f,1.0f);
    set_seg('3',4, 1.0f,1.0f, 0.1f,1.0f); set_seg('3',5, 0.5f,0.5f, 0.9f,0.7f);
    /* 4 */ g_stroke_counts['4']=3;
    set_seg('4',0, 0.0f,0.0f, 0.0f,0.6f); set_seg('4',1, 0.7f,0.0f, 0.7f,1.0f);
    set_seg('4',2, 0.0f,0.6f, 1.0f,0.6f);
    /* 5 */ g_stroke_counts['5']=6;
    set_seg('5',0, 0.0f,0.0f, 1.0f,0.0f); set_seg('5',1, 0.0f,0.0f, 0.0f,0.5f);
    set_seg('5',2, 0.0f,0.5f, 0.7f,0.5f); set_seg('5',3, 0.7f,0.5f, 1.0f,0.7f);
    set_seg('5',4, 1.0f,0.7f, 0.7f,1.0f); set_seg('5',5, 0.7f,1.0f, 0.0f,1.0f);
    /* 6 */ g_stroke_counts['6']=7;
    set_seg('6',0, 0.8f,0.0f, 0.2f,0.0f); set_seg('6',1, 0.2f,0.0f, 0.0f,0.2f);
    set_seg('6',2, 0.0f,0.2f, 0.0f,1.0f); set_seg('6',3, 0.0f,0.5f, 0.6f,0.5f);
    set_seg('6',4, 0.6f,0.5f, 0.8f,0.7f); set_seg('6',5, 0.8f,0.7f, 0.6f,1.0f);
    set_seg('6',6, 0.6f,1.0f, 0.0f,1.0f);
    /* 7 */ g_stroke_counts['7']=2;
    set_seg('7',0, 0.0f,0.0f, 1.0f,0.0f); set_seg('7',1, 1.0f,0.0f, 0.4f,1.0f);
    /* 8 */ g_stroke_counts['8']=12;
    set_seg('8',0, 0.2f,0.0f, 0.8f,0.0f); set_seg('8',1, 0.8f,0.0f, 1.0f,0.2f);
    set_seg('8',2, 1.0f,0.2f, 0.8f,0.45f); set_seg('8',3, 0.8f,0.45f, 0.2f,0.45f);
    set_seg('8',4, 0.2f,0.45f, 0.0f,0.55f); set_seg('8',5, 0.0f,0.55f, 0.2f,0.55f);
    set_seg('8',6, 0.2f,0.55f, 0.8f,0.55f); set_seg('8',7, 0.8f,0.55f, 1.0f,0.8f);
    set_seg('8',8, 1.0f,0.8f, 0.8f,1.0f); set_seg('8',9, 0.8f,1.0f, 0.2f,1.0f);
    set_seg('8',10, 0.2f,1.0f, 0.0f,0.8f); set_seg('8',11, 0.0f,0.8f, 0.2f,0.55f);
    /* 9 */ g_stroke_counts['9']=7;
    set_seg('9',0, 0.8f,0.0f, 0.2f,0.0f); set_seg('9',1, 0.2f,0.0f, 0.0f,0.15f);
    set_seg('9',2, 0.0f,0.15f, 0.2f,0.45f); set_seg('9',3, 0.2f,0.45f, 0.8f,0.45f);
    set_seg('9',4, 0.8f,0.45f, 1.0f,0.7f); set_seg('9',5, 1.0f,0.7f, 0.8f,1.0f);
    set_seg('9',6, 0.8f,1.0f, 0.0f,1.0f);
    /* : */ g_stroke_counts[':']=2;
    set_seg(':',0, 0.5f,0.2f, 0.5f,0.3f); set_seg(':',1, 0.5f,0.7f, 0.5f,0.8f);
    g_font_loaded = 1;
}
#undef S

static void draw_stroke_char(char ch, float ox, float oy, float scale)
{
    if (ch < 0 || ch >= 128) return;
    int n = g_stroke_counts[(int)ch];
    if (n == 0) return;
    glBegin(GL_LINES);
    for (int i = 0; i < n; i++) {
        StrokeSeg s = g_stroke_font[(int)ch][i];
        glVertex3f(ox + s.x1 * scale, oy + (1.0f - s.y1) * scale, 0.0f);
        glVertex3f(ox + s.x2 * scale, oy + (1.0f - s.y2) * scale, 0.0f);
    }
    glEnd();
}

static FILE* g_debug_log = NULL;

DLLEXPORT void glh_draw_text_3d(int time_bits, const char* text)
{
    float t;
    memcpy(&t, &time_bits, 4);
    init_stroke_font();

    /* debug log */
    if (!g_debug_log) g_debug_log = fopen("glhelper.log", "w");
    if (g_debug_log) {
        fprintf(g_debug_log, "[draw_text_3d] time=%.3f text=\"%s\" len=%d\n",
            t, text ? text : "NULL", text ? (int)strlen(text) : -1);
        fflush(g_debug_log);
    }

    glClearColor(0.05f, 0.05f, 0.12f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glLoadIdentity();
    glTranslatef(0.0f, 0.0f, -3.0f);
    glRotatef(t * 60.0f, 0.0f, 1.0f, 0.0f);
    glRotatef(t * 25.0f, 1.0f, 0.0f, 0.0f);

    /* 计算文本总宽度 */
    float total_w = 0.0f;
    int len = 0;
    for (const char* p = text; *p; p++) { total_w += 0.75f; len++; }
    if (len == 0) { if (g_debug_log) { fprintf(g_debug_log, "[draw_text_3d] EMPTY string, returning\n"); fflush(g_debug_log); } return; }

    float start_x = -total_w * 0.5f;
    float scale = 0.6f;

    /* 颜色随时间渐变 */
    float r = sinf(t * 1.3f) * 0.5f + 0.5f;
    float g = sinf(t * 1.7f + 2.0f) * 0.5f + 0.5f;
    float b = sinf(t * 2.1f + 4.0f) * 0.5f + 0.5f;
    glColor3f(r, g, b);

    if (g_debug_log) {
        fprintf(g_debug_log, "[draw_text_3d] rendering %d chars, color=(%.2f,%.2f,%.2f)\n", len, r, g, b);
        fflush(g_debug_log);
    }

    glDisable(GL_DEPTH_TEST);
    glLineWidth(3.0f);
    for (int i = 0; i < len; i++) {
        draw_stroke_char(text[i], start_x + i * 0.75f, -0.4f, scale);
    }
    glEnable(GL_DEPTH_TEST);
}

#ifdef _WIN32
/* ========== Win32 + WGL 实现 ========== */

#define MAX_WINDOWS 4

static HWND _hwnds[MAX_WINDOWS];
static HDC _hdcs[MAX_WINDOWS];
static HGLRC _hglrcs[MAX_WINDOWS];
static int _window_count = 0;
static int _should_close[MAX_WINDOWS];
static LARGE_INTEGER _freq;
static LARGE_INTEGER _start_time;

/* 语言标题映射 (整数 ID → 窗口标题) */
static const WCHAR* _title_map[] = {
    L"VML OpenGL Cube",
    L"OpenGL — C",
    L"OpenGL — BASIC",
    L"OpenGL — Pascal",
    L"OpenGL — Python",
    L"OpenGL — Lua",
    L"OpenGL — Forth",
    L"OpenGL — Rust",
    L"OpenGL — Go",
    L"OpenGL — Ladder",
    L"OpenGL — Java",
    L"OpenGL — JavaScript",
    L"OpenGL — C#",
    L"OpenGL — Swift",
    L"OpenGL — Kotlin",
    L"OpenGL — Scheme",
    L"OpenGL — C++",
};
#define TITLE_COUNT (sizeof(_title_map) / sizeof(_title_map[0]))

static LRESULT CALLBACK _wnd_proc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp)
{
    switch (msg)
    {
    case WM_CLOSE:
    case WM_DESTROY:
        for (int i = 0; i < _window_count; i++)
            if (_hwnds[i] == hwnd)
                _should_close[i] = 1;
        return 0;
    }
    return DefWindowProc(hwnd, msg, wp, lp);
}

__declspec(dllexport) int glh_init(int w, int h)
{
    if (_window_count >= MAX_WINDOWS) return -1;

    if (_window_count == 0)
    {
        QueryPerformanceFrequency(&_freq);
        QueryPerformanceCounter(&_start_time);
    }

    WNDCLASSW wc = {0};
    wc.lpfnWndProc = _wnd_proc;
    wc.hInstance = GetModuleHandle(NULL);
    wc.lpszClassName = L"VML_OpenGL_Window";
    wc.style = CS_OWNDC;
    RegisterClassW(&wc);

    RECT rect = {0, 0, w, h};
    AdjustWindowRect(&rect, WS_OVERLAPPEDWINDOW, FALSE);

    HWND hwnd = CreateWindowExW(0, L"VML_OpenGL_Window", L"VML OpenGL Cube",
        WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT,
        rect.right - rect.left, rect.bottom - rect.top,
        NULL, NULL, GetModuleHandle(NULL), NULL);

    if (!hwnd) return -3;

    HDC hdc = GetDC(hwnd);
    PIXELFORMATDESCRIPTOR pfd = {
        sizeof(PIXELFORMATDESCRIPTOR), 1,
        PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
        PFD_TYPE_RGBA, 24, 0,0,0,0,0,0, 0,0,0,0,0,0,0, 32, 0,0,
        PFD_MAIN_PLANE, 0, 0,0,0
    };
    int pf = ChoosePixelFormat(hdc, &pfd);
    SetPixelFormat(hdc, pf, &pfd);

    HGLRC hglrc = wglCreateContext(hdc);
    wglMakeCurrent(hdc, hglrc);

    int id = _window_count;
    _hwnds[id] = hwnd;
    _hdcs[id] = hdc;
    _hglrcs[id] = hglrc;
    _should_close[id] = 0;
    _window_count++;

    ShowWindow(hwnd, SW_SHOW);

    /* 设置视口和投影 */
    glViewport(0, 0, w, h);
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    float aspect = (float)w / (float)h;
    float fov = 45.0f * (float)M_PI / 180.0f;
    float f = 1.0f / tanf(fov / 2.0f);
    glFrustum(-aspect / f, aspect / f, -1.0f / f, 1.0f / f, 1.0f, 100.0f);
    glMatrixMode(GL_MODELVIEW);
    glEnable(GL_DEPTH_TEST);

    return id;
}

__declspec(dllexport) int glh_should_close(int ctx_id)
{
    if (ctx_id < 0 || ctx_id >= _window_count) return 1;
    return _should_close[ctx_id];
}

__declspec(dllexport) void glh_set_title(int ctx_id, int title_id)
{
    if (ctx_id < 0 || ctx_id >= _window_count) return;
    if (title_id < 0 || title_id >= (int)TITLE_COUNT) return;
    SetWindowTextW(_hwnds[ctx_id], _title_map[title_id]);
}

__declspec(dllexport) float glh_get_time(int ctx_id)
{
    (void)ctx_id;
    LARGE_INTEGER now;
    QueryPerformanceCounter(&now);
    return (float)((double)(now.QuadPart - _start_time.QuadPart) / (double)_freq.QuadPart);
}

static void draw_colorful_cube(void)
{
    float s = 0.7f;
    float v[8][3] = {
        {-s, -s, -s}, { s, -s, -s}, { s,  s, -s}, {-s,  s, -s},
        {-s, -s,  s}, { s, -s,  s}, { s,  s,  s}, {-s,  s,  s},
    };
    int faces[6][4] = {
        {0, 1, 2, 3}, {1, 5, 6, 2}, {5, 4, 7, 6},
        {4, 0, 3, 7}, {3, 2, 6, 7}, {4, 5, 1, 0},
    };
    float colors[6][3] = {
        {1.0f, 0.3f, 0.3f}, {0.3f, 1.0f, 0.3f}, {0.3f, 0.3f, 1.0f},
        {1.0f, 1.0f, 0.3f}, {1.0f, 0.3f, 1.0f}, {0.3f, 1.0f, 1.0f},
    };

    for (int i = 0; i < 6; i++)
    {
        glColor3fv(colors[i]);
        glBegin(GL_QUADS);
        for (int j = 0; j < 4; j++)
        {
            glVertex3fv(v[faces[i][j]]);
            /* 超出 4 颜色渐变 */;
        }
        glEnd();
    }
}

__declspec(dllexport) void glh_draw_cube(float time)
{
    glClearColor(0.1f, 0.1f, 0.15f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glLoadIdentity();
    glTranslatef(0.0f, 0.0f, -4.0f);
    glRotatef(time * 50.0f, 1.0f, 0.0f, 0.0f);
    glRotatef(time * 30.0f, 0.0f, 1.0f, 0.0f);
    glRotatef(time * 15.0f, 0.0f, 0.0f, 1.0f);

    draw_colorful_cube();
}

__declspec(dllexport) void glh_swap(int ctx_id)
{
    if (ctx_id < 0 || ctx_id >= _window_count) return;
    wglMakeCurrent(_hdcs[ctx_id], _hglrcs[ctx_id]);
    SwapBuffers(_hdcs[ctx_id]);

    /* 泵送 Windows 消息（替代 GLFW 的 glfwPollEvents） */
    MSG msg;
    while (PeekMessage(&msg, _hwnds[ctx_id], 0, 0, PM_REMOVE))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

__declspec(dllexport) void glh_destroy(void)
{
    for (int i = 0; i < _window_count; i++)
    {
        if (_hglrcs[i])
        {
            wglMakeCurrent(NULL, NULL);
            wglDeleteContext(_hglrcs[i]);
            _hglrcs[i] = NULL;
        }
        if (_hdcs[i] && _hwnds[i])
        {
            ReleaseDC(_hwnds[i], _hdcs[i]);
            _hdcs[i] = NULL;
        }
        if (_hwnds[i])
        {
            DestroyWindow(_hwnds[i]);
            _hwnds[i] = NULL;
        }
    }
    _window_count = 0;
}

/* 纯 int 包装函数 (消除 FFI 混合类型限制) */
__declspec(dllexport) int glh_get_time_i(int ctx_id)
{
    float t = glh_get_time(ctx_id);
    int bits;
    memcpy(&bits, &t, 4);
    return bits;
}

__declspec(dllexport) void glh_draw_cube_i(int time_bits)
{
    float t;
    memcpy(&t, &time_bits, 4);
    glh_draw_cube(t);
}

#else
/* ========== GLFW 实现 (macOS / Linux) ========== */

#define MAX_WINDOWS 4

static GLFWwindow* _windows[MAX_WINDOWS];
static int _window_count = 0;

int glh_init(int w, int h)
{
    if (_window_count >= MAX_WINDOWS) return -1;
    if (_window_count == 0)
    {
        if (!glfwInit()) return -2;
        glfwWindowHint(GLFW_VISIBLE, GLFW_TRUE);
    }
    GLFWwindow* win = glfwCreateWindow(w, h, "VML OpenGL Cube", NULL, NULL);
    if (!win) return -3;
    glfwMakeContextCurrent(win);
    int id = _window_count;
    _windows[_window_count++] = win;
    glViewport(0, 0, w, h);
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    float aspect = (float)w / (float)h;
    float fov = 45.0f * (float)M_PI / 180.0f;
    float f = 1.0f / tanf(fov / 2.0f);
    glFrustum(-aspect / f, aspect / f, -1.0f / f, 1.0f / f, 1.0f, 100.0f);
    glMatrixMode(GL_MODELVIEW);
    glEnable(GL_DEPTH_TEST);
    return id;
}

int glh_should_close(int ctx_id)
{
    if (ctx_id < 0 || ctx_id >= _window_count) return 1;
    return glfwWindowShouldClose(_windows[ctx_id]);
}

void glh_set_title(int ctx_id, int title_id)
{
    if (ctx_id < 0 || ctx_id >= _window_count) return;
    const char* titles[] = {
        "VML OpenGL Cube",
        "OpenGL — C", "OpenGL — BASIC", "OpenGL — Pascal",
        "OpenGL — Python", "OpenGL — Lua", "OpenGL — Forth",
        "OpenGL — Rust", "OpenGL — Go", "OpenGL — Ladder",
        "OpenGL — Java", "OpenGL — JavaScript", "OpenGL — C#",
        "OpenGL — Swift", "OpenGL — Kotlin", "OpenGL — Scheme",
        "OpenGL — C++",
    };
    if (title_id < 0 || title_id >= 17) return;
    glfwSetWindowTitle(_windows[ctx_id], titles[title_id]);
}

float glh_get_time(int ctx_id)
{
    (void)ctx_id;
    return (float)glfwGetTime();
}

static void draw_colorful_cube(void)
{
    float s = 0.7f;
    float v[8][3] = {
        {-s, -s, -s}, { s, -s, -s}, { s,  s, -s}, {-s,  s, -s},
        {-s, -s,  s}, { s, -s,  s}, { s,  s,  s}, {-s,  s,  s},
    };
    int faces[6][4] = {
        {0, 1, 2, 3}, {1, 5, 6, 2}, {5, 4, 7, 6},
        {4, 0, 3, 7}, {3, 2, 6, 7}, {4, 5, 1, 0},
    };
    float colors[6][3] = {
        {1.0f, 0.3f, 0.3f}, {0.3f, 1.0f, 0.3f}, {0.3f, 0.3f, 1.0f},
        {1.0f, 1.0f, 0.3f}, {1.0f, 0.3f, 1.0f}, {0.3f, 1.0f, 1.0f},
    };
    for (int i = 0; i < 6; i++) {
        glColor3fv(colors[i]);
        glBegin(GL_QUADS);
        for (int j = 0; j < 4; j++) glVertex3fv(v[faces[i][j]]);
        glEnd();
    }
}

void glh_draw_cube(float time)
{
    glClearColor(0.1f, 0.1f, 0.15f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    glLoadIdentity();
    glTranslatef(0.0f, 0.0f, -4.0f);
    glRotatef(time * 50.0f, 1.0f, 0.0f, 0.0f);
    glRotatef(time * 30.0f, 0.0f, 1.0f, 0.0f);
    draw_colorful_cube();
}

void glh_swap(int ctx_id)
{
    if (ctx_id < 0 || ctx_id >= _window_count) return;
    glfwSwapBuffers(_windows[ctx_id]);
    glfwPollEvents();
}

void glh_destroy(void)
{
    for (int i = 0; i < _window_count; i++)
        if (_windows[i]) glfwDestroyWindow(_windows[i]);
    int any = 0;
    for (int i = 0; i < _window_count; i++)
        if (_windows[i] != NULL) any = 1;
    if (!any) { glfwTerminate(); _window_count = 0; }
}

int glh_get_time_i(int ctx_id)
{
    float t = glh_get_time(ctx_id);
    int bits;
    memcpy(&bits, &t, 4);
    return bits;
}

void glh_draw_cube_i(int time_bits)
{
    float t;
    memcpy(&t, &time_bits, 4);
    glh_draw_cube(t);
}

#endif
