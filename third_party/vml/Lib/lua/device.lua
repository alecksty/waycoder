-- VML 设备 I/O + 文件 + 键鼠扩展库 — Lua
-- 需显式 import device

vml = vml or {}

-- 键盘
function vml.kb_hit()
    return 0
end

function vml.kb_getch()
    return 0
end

-- 鼠标
function vml.mouse_get_x()
    return 0
end

function vml.mouse_get_y()
    return 0
end

function vml.mouse_left()
    return 0
end

function vml.mouse_right()
    return 0
end

-- 统一设备接口
function vml.dev_open(name)
    return 0
end

function vml.dev_close(handle)
    return 0
end

function vml.dev_read(handle, buf, offset, count)
    return 0
end

function vml.dev_write(handle, buf, offset, count)
    return 0
end

function vml.dev_control(handle, command, data, length)
    return 0
end

