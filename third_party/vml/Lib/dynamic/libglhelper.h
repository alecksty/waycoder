/* libglhelper.h — OpenGL 旋转立方体 FFI 接口 (纯 int 函数) */
#ifndef LIBGLHELPER_H
#define LIBGLHELPER_H

/* 窗口管理 */
extern int  glh_init(int w, int h);
extern int  glh_should_close(int ctx_id);
extern void glh_swap(int ctx_id);
extern void glh_destroy(void);

/* 标题 (整数 ID 映射) */
extern void glh_set_title(int ctx_id, int title_id);

/* 渲染 (纯 int 包装: float 值用 int bits 传递, 避免 SYSCALL #373/#375 混合类型问题) */
extern int  glh_get_time_i(int ctx_id);
extern void glh_draw_cube_i(int time_bits);
extern void glh_draw_text_3d(int time_bits, const char* text);

#endif /* LIBGLHELPER_H */
