/* libstructtest.c — FFI struct 参数验证 DLL
 * 测试 struct by-value 和 struct pointer 参数传递
 * 编译 (macOS): cc -dynamiclib -o libstructtest.dylib libstructtest.c
 * 编译 (MSVC):  cl /nologo /O2 /MD /LD /Fe:libstructtest.dll libstructtest.c
 */

#ifdef _WIN32
#include <windows.h>
#else
#include <dlfcn.h>
#endif
#include <stdio.h>
#include <string.h>

#ifdef _WIN32
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#define LOGFILE "structtest.log"

static FILE* g_log = NULL;

static void log_open(void)
{
    if (g_log) return;
    g_log = fopen(LOGFILE, "w");
    if (g_log) {
        fprintf(g_log, "=== Struct FFI Test Log ===\n");
        fflush(g_log);
    }
}

/* ====== Point struct 测试 ====== */
typedef struct Point {
    int x;
    int y;
} Point;

/* struct by-value: 读取字段 */
DLLEXPORT int struct_dist_sq(Point a, Point b)
{
    log_open();
    int dx = a.x - b.x;
    int dy = a.y - b.y;
    int result = dx * dx + dy * dy;
    if (g_log) {
        fprintf(g_log, "[DIST_SQ] a=(%d,%d) b=(%d,%d) -> %d\n",
            a.x, a.y, b.x, b.y, result);
        fflush(g_log);
    }
    return result;
}

/* struct pointer: 修改字段 (验证 in/out 语义) */
DLLEXPORT void struct_scale(Point* p, int scale)
{
    log_open();
    if (g_log) {
        fprintf(g_log, "[SCALE]  in: (%d,%d) scale=%d\n", p->x, p->y, scale);
    }
    p->x *= scale;
    p->y *= scale;
    if (g_log) {
        fprintf(g_log, "[SCALE] out: (%d,%d)\n", p->x, p->y);
        fflush(g_log);
    }
}

/* struct pointer: 只读访问 */
DLLEXPORT int struct_sum(Point* p)
{
    log_open();
    int result = p->x + p->y;
    if (g_log) {
        fprintf(g_log, "[SUM]   (%d,%d) -> %d\n", p->x, p->y, result);
        fflush(g_log);
    }
    return result;
}

/* Rectangle struct 测试 (8 字节) */
typedef struct Rect {
    int x, y, w, h;
} Rect;

DLLEXPORT int rect_area(Rect r)
{
    log_open();
    int area = r.w * r.h;
    if (g_log) {
        fprintf(g_log, "[AREA]  (%d,%d,%d,%d) -> %d\n", r.x, r.y, r.w, r.h, area);
        fflush(g_log);
    }
    return area;
}

DLLEXPORT void rect_move(Rect* r, int dx, int dy)
{
    log_open();
    if (g_log) {
        fprintf(g_log, "[MOVE]  in: (%d,%d) dx=%d dy=%d\n", r->x, r->y, dx, dy);
    }
    r->x += dx;
    r->y += dy;
    if (g_log) {
        fprintf(g_log, "[MOVE] out: (%d,%d)\n", r->x, r->y);
        fflush(g_log);
    }
}
