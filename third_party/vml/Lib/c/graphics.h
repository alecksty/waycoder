#ifndef _GRAPHICS_H
#define _GRAPHICS_H

#param lib("graphics")

#define DETECT 0
#define VGA 1
#define VGALO 0
#define VGAMED 1
#define VGAHI 2

#define BLACK           0
#define BLUE            1
#define GREEN           2
#define CYAN            3
#define RED             4
#define MAGENTA         5
#define BROWN           6
#define LIGHTGRAY       7
#define DARKGRAY        8
#define LIGHTBLUE       9
#define LIGHTGREEN      10
#define LIGHTCYAN       11
#define LIGHTRED        12
#define LIGHTMAGENTA    13
#define YELLOW          14
#define WHITE           15

#define GFX_BASE    0xB80000
#define GFX_WIDTH   320
#define GFX_HEIGHT  240
#define PAL_BASE    0x6F00

#define COPY_PUT    0
#define XOR_PUT     1

#define SOLID_LINE  0
#define DOTTED_LINE 1
#define CENTER_LINE 2
#define DASHED_LINE 3
#define USERBIT_LINE 4

#define SOLID_FILL  1
#define EMPTY_FILL  0

extern int _graph_ok;
extern int _current_color;
extern int _bg_color;
extern int _fill_pattern;
extern int _fill_color;
extern int _cp_x;
extern int _cp_y;

void initgraph(int *gd, int *gm, const char *path);
void closegraph(void);
int graphresult(void);
char *grapherrormsg(int code);
void setcolor(int color);
void setbkcolor(int color);
int getcolor(void);
int getmaxx(void);
int getmaxy(void);
void putpixel(int x, int y, int color);
int getpixel(int x, int y);
void moveto(int x, int y);
void moverel(int dx, int dy);
void lineto(int x, int y);
void linerel(int dx, int dy);
void line(int x1, int y1, int x2, int y2);
void circle(int x, int y, int radius);
void arc(int x, int y, int stangle, int endangle, int radius);
void ellipse(int x, int y, int stangle, int endangle, int xradius, int yradius);
void rectangle(int left, int top, int right, int bottom);
void bar(int left, int top, int right, int bottom);
void bar3d(int left, int top, int right, int bottom, int depth, int topflag);
void setfillstyle(int pattern, int color);
void floodfill(int x, int y, int border);
void setlinestyle(int linestyle, int pattern, int thickness);
void outtextxy(int x, int y, const char *str);
void outtext(const char *str);
void settextstyle(int font, int direction, int charsize);
void setviewport(int x1, int y1, int x2, int y2, int clip);
void clearviewport(void);
int imagesize(int x1, int y1, int x2, int y2);
void getimage(int x1, int y1, int x2, int y2, void *buf);
void putimage(int x, int y, void *buf, int op);
void drawpoly(int numpoints, int *polypoints);
void fillpoly(int numpoints, int *polypoints);
void sector(int x, int y, int stangle, int endangle, int xradius, int yradius);
void pieslice(int x, int y, int stangle, int endangle, int radius);

#endif
