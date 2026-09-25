# 绘图状态

裁剪、蒙版（含布尔运算）、透明度、资源计数。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_alpha(int v)`
全局透明度 0..255，对**之后**画的图元生效。
⚠ 只影响**形状的填充与描边** —— 渐变与图片不走这条路，不受它影响。
```c
ui_alpha(120);
ui_rect(10, 10, 100, 60, 0xFF000000, 1, 0, 0);   /* 半透明黑 */
ui_alpha(255);
```
### `ui_brush_reset(void)`
释放本窗口累积的**全部画刷 / 渐变定义**（只能整体重置 —— 句柄就是表的下标，删单个会让后面全部错位）。
按需造渐变的程序**每帧开头调一次**，否则会一直涨到上限。
```c
while (ui_win_closed() == 0) {
    if (ui_res_count(3) > 200) { ui_brush_reset(); }
    /* … */
}
```
### `ui_clip_pop(void)`
弹出一级裁剪（栈空了再弹是空操作）。
```c
ui_clip_pop();
```
### `ui_clip_push(int x, int y, int w, int h)`
压入一级**矩形裁剪**：之后画的东西只在这个矩形里可见；可以**嵌套**（与上一级求交）。
⚠ 有压必须有弹 —— 中途 `return` 忘了弹，后面的东西就全画不出来，而且看不出原因。
```c
ui_clip_push(20, 20, 200, 100);
ui_rect(0, 0, 400, 400, 0xFFFF0000, 1, 0, 0);  /* 只有框内那块出现 */
ui_clip_pop();
```
### `ui_clip_reset(void)`
**清空整个裁剪栈**（不是弹一级）—— 给“出错恢复”用：一进主循环调一次，回到干净的整屏状态。
与 `ui_alpha(255)` 配成一对“复位”。
```c
while (ui_win_closed() == 0) {
    ui_clip_reset();   /* 上一帧谁忘了弹，都不影响这一帧 */
    ui_alpha(255);
    /* …画这一帧… */
}
```
### `ui_layer_begin(void)`
开始**图层收集**：这期间画的图元先落在临时画布上，等 `ui_layer_end` 整层合成。
与 `ui_alpha` 的区别：`ui_alpha` 是“每个图元各自半透明”（**组内重叠处会互相透出来**），
图层是“先画好一整张、再整张压上去”（组内怎么重叠都**只透一次**）。
```c
ui_layer_begin();
ui_circle(200, 200, 80, 0xFFFF0000, 1, 0);
ui_circle(240, 200, 80, 0xFFFF0000, 1, 0);
ui_layer_end(120);   /* 整组半透明，重叠处不会更深 */
```
### `ui_layer_end(int alpha)`
结束收集并**整层**按 `alpha`（0..255）合成上去。
⚠ **代价**：每层一块与窗口同尺寸的临时画布 + 一次逐像素合成 —— 别开太多。
⚠ 用了图层的这一帧在手机上**会回退光栅**（矢量后端没有离屏合成的通用做法）。
```c
ui_layer_end(255);   /* 不透明（等于没开层）*/
ui_layer_end(0);     /* 全透明（整层不画，也省掉那块画布）*/
```
### `ui_mask_begin(void)`
开始收集蒙版形状：这期间画的形状**不上屏**，只当蒙版用。配 `ui_mask_end` / `ui_mask_end2` 收尾。
```c
ui_mask_begin();
ui_circle(200, 200, 120, 0xFFFFFFFF, 1, 0);   /* 这个圆只当蒙版，不画 */
ui_mask_end(1);
```
### `ui_mask_clear(void)`
取消蒙版：之后的图元**全部可见**。
```c
ui_mask_clear();
```
### `ui_mask_end(int inside)`
收下蒙版并**取代**当前蒙版。`inside`：1 = 只在形状里画 / 0 = 只在形状外画。
```c
ui_mask_end(1);   /* 只在里面画 */
/* 或者 */
ui_mask_end(0);   /* 只在外面画（等于把形状挖掉）*/
```
### `ui_mask_end2(int op)`
收下蒙版并与**当前蒙版**按 `op` 做**布尔运算** —— 老式“圆环”得画两遍，现在一次就够。
`op` 取 `VML_MASK_REPLACE`(0) / `VML_MASK_UNION`(1) / `VML_MASK_INTERSECT`(2) /
`VML_MASK_SUBTRACT`(3) / `VML_MASK_XOR`(4)。
⚠ 它**不碰 `inside`**（保持当前值）—— `inside` 是“最后整体取反”，与形状之间的组合是两件事。
```c
/* 甜甜圈：大圆减去小圆 */
ui_mask_begin();
ui_circle(200, 200, 120, 0xFFFFFFFF, 1, 0);
ui_mask_end(1);
ui_mask_begin();
ui_circle(200, 200, 60, 0xFFFFFFFF, 1, 0);
ui_mask_end2(VML_MASK_SUBTRACT);
```
### `ui_mask_path(int seg, int idx, char* buf, int cap)`
把第 seg 段第 idx 个形状导出成 **SVG 路径**写进 buf —— 程序能拿它**描洞口的边**、做外发光。
圆导出的是**真圆弧**。放不下或越界返回 -1。
```c
char buf[128];
int i;
for (i = 0; i < ui_mask_shape_count(1); i++) {
    if (ui_mask_path(1, i, buf, sizeof(buf)) > 0) {
        ui_path(buf, 0x80000000, 2, 0, "", 1, 0);   /* 描洞口 */
    }
}
```
### `ui_mask_seg_count(void)`
当前蒙版有几段（没有蒙版返回 0）。
```c
int n = ui_mask_seg_count();
```
### `ui_mask_seg_op(int seg)`
第 seg 段用的运算符（越界 -1）。
靠它认出“**哪一段是被挖掉的洞**”（`VML_MASK_SUBTRACT`）—— 只有形状列表是分不出底和洞的。
```c
if (ui_mask_seg_op(1) == VML_MASK_SUBTRACT) { /* 第 1 段是洞 */ }
```
### `ui_mask_shape_count(int seg)`
第 seg 段里有几个形状（越界 0）。
```c
int n = ui_mask_shape_count(1);
```
### `ui_mask_test(int x, int y)`
**蒙版当碰撞体**：这一点在不在当前蒙版里（1/0）。
建筑层“炸一块缺一块”之后，不必自己再维护一份洞的坐标表 —— 洞口是“可见的”（子弹能穿过去）。
⚠ 没有蒙版时**恒为 1**（处处可见）；坐标是场景坐标；**别每像素调**（一次要在形状表上跑一遍）。
```c
if (ui_mask_test(ban_x, ban_y) == 0) { /* 撞墙了 */ }
```
### `ui_res_count(int what)`
查当前占用：`0` 图元 / `1` 图像 / `2` 矢量图块 / `3` 画刷渐变；未知返回 -1。
各类都有硬上限，**到了上限是静默丢弃** —— 所以得看得见“快满了”。
```c
if (ui_res_count(0) > 10000) { ui_clear(0xFF000000); }   /* 图元快满了 */
```
