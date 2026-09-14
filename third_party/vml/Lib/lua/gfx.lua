-- VML VGA 图形扩展库 (QBASIC风格) — Lua
-- 需显式 import gfx
vml = vml or {}

-- Basic VGA
function vml.vga_clear()           asm("SYSCALL 80") end
function vml.vga_putchar(x,y,c,clr) asm("SYSCALL 81") end
function vml.vga_puts(x,y,s,clr)  asm("SYSCALL 82") end

-- Screen mode & info
function vml.gfx_screen(mode) end
function vml.gfx_width()     end
function vml.gfx_height()    end
function vml.gfx_depth()     end

-- Palette
function vml.gfx_palette(i,r,g,b) end
function vml.gfx_palette_get(i)   end

-- Pixel ops
function vml.gfx_pset(x,y,c)     end
function vml.gfx_point(x,y)      end
function vml.gfx_cls()           end
function vml.gfx_cls_color(c)    end

-- Drawing
function vml.gfx_line(x1,y1,x2,y2,c)   end
function vml.gfx_rect(x1,y1,x2,y2,c)   end
function vml.gfx_rect_fill(x1,y1,x2,y2,c) end
function vml.gfx_circle(cx,cy,r,c)     end
function vml.gfx_circle_fill(cx,cy,r,c) end
function vml.gfx_arc(cx,cy,r,sa,ea,c)  end
function vml.gfx_sector(cx,cy,r,sa,ea,c) end

-- Text
function vml.gfx_print(x,y,text,c)       end
function vml.gfx_print_scale(x,y,text,c,s) end

-- Fill
function vml.gfx_flood_fill(x,y,fc,bc)  end

-- Advanced (SYSCALL)
function vml.gfx_screenshot()        asm("SYSCALL 200") return 0 end
function vml.gfx_put_image(x,y,w,h,d) asm("SYSCALL 201") return 0 end
function vml.gfx_get_image(x,y,w,h,b) asm("SYSCALL 202") return 0 end
function vml.gfx_viewport(x1,y1,x2,y2) asm("SYSCALL 203") return 0 end
