-- VML 全类型转换库 — Lua 包装器 (v1.66.44)
-- 用法: local conv = require("conv")

local conv = {}

function conv.int_to_str(val)
    return vml.call('int_to_str', val)
end

function conv.str_to_int(s)
    return vml.call('str_to_int', s)
end

function conv.float_to_str(f)
    return vml.call('float_to_str', f)
end

function conv.str_to_float(s)
    return vml.call('str_to_float', s)
end

function conv.double_to_str(d)
    return vml.call('double_to_str', d)
end

function conv.str_to_double(s)
    return vml.call('str_to_double', s)
end

function conv.long_to_str(l)
    return vml.call('long_to_str', l)
end

function conv.str_to_long(s)
    return vml.call('str_to_long', s)
end

function conv.bool_to_str(b)
    return vml.call('bool_to_str', b)
end

function conv.str_to_bool(s)
    return vml.call('str_to_bool', s)
end

function conv.char_to_str(c)
    return vml.call('char_to_str', c)
end

function conv.str_to_char(s)
    return vml.call('str_to_char', s)
end

return conv
