#param lib("bitlib")

/* browser_gfx.c - VML Browser Graphics Library
 *
 * Command-buffer protocol:
 *   Programs write drawing commands to memory at CMD_BUF (0x5000).
 *   After the WASM program exits, JS reads the buffer and calls Canvas2D.
 *
 * Buffer format (sequential, terminated by cmd_id=0):
 *   [cmd_id (4B)] [param_count (4B)] [param1 (4B)] ... [paramN (4B)]
 *
 * cmd_id:  1=clear(r,g,b)  2=fillPolygon  3=drawPolygon  4=flush  0=end
 */

#define CMD_BUF 0x5000

/* Global write pointer — offset from CMD_BUF in bytes.
 * Starts at 0 (BSS-initialized). JS resets it when done. */
volatile int __cmd_offset = 0;

/* Write one 32-bit value to the command buffer and advance. */
static void cmd_write(int val) {
    volatile int* p = (volatile int*)(CMD_BUF + __cmd_offset);
    *p = val;
    __cmd_offset += 4;
}

void browser_clear(int r, int g, int b) {
    cmd_write(1);          // cmd_id = clear
    cmd_write(3);          // param_count = 3
    cmd_write(r);
    cmd_write(g);
    cmd_write(b);
}

void browser_color(int r, int g, int b) {
    cmd_write(5);          // cmd_id = setColor
    cmd_write(3);          // param_count = 3
    cmd_write(r);
    cmd_write(g);
    cmd_write(b);
}

void browser_fill_polygon(int count, int* points) {
    cmd_write(2);          // cmd_id = fillPolygon
    cmd_write(count * 2);  // param_count (x,y pairs)
    int i;
    for (i = 0; i < count * 2; i++)
        cmd_write(points[i]);
}

void browser_draw_polygon(int count, int* points) {
    cmd_write(3);          // cmd_id = drawPolygon
    cmd_write(count * 2);
    int i;
    for (i = 0; i < count * 2; i++)
        cmd_write(points[i]);
}

void browser_flush(void) {
    cmd_write(4);          // cmd_id = flush (no-op marker for JS)
    cmd_write(0);          // param_count = 0
    cmd_write(0);          // terminator (cmd_id=0)
}
