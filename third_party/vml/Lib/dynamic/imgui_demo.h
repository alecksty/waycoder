/* imgui_demo.h — ImGui-style C API for ImportDynamic testing */

#ifndef IMGUI_DEMO_H
#define IMGUI_DEMO_H

extern int  igCreateContext(int reserved);
extern void igDestroyContext(int ctx);
extern int  igGetVersion(void);
extern int  igAdd(int a, int b);
extern int  igMul(int a, int b);
extern void igShowDemoWindow(int show);

#endif
