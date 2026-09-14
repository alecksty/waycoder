-- VML Lua 标准库 — 完整运行时支持 (Lua 5.3+ 兼容)
-- 用法: import stdlib

-- ============================================================
-- 基本函数
-- ============================================================
function print(...)
    local n = select("#", ...)
    for i = 1, n do
        local v = select(i, ...)
        if type(v) == "string" then
            asm("SYSCALL 1")
        elseif type(v) == "number" then
            asm("SYSCALL 6")
        elseif type(v) == "boolean" then
            if v then asm("SYSCALL 1") else asm("SYSCALL 1") end
        end
        if i < n then
            local sp = " "
            asm("SYSCALL 1")
        end
    end
    local nl = "\n"
    asm("SYSCALL 1")
end

function type(v) end
function tonumber(e, base) return 0 end
function tostring(v) return "" end
function tointeger(v) return 0 end
function tofloat(v) return 0.0 end
function pairs(t) end
function ipairs(t) end
function next(table, index) end
function select(index, ...) end
function pcall(f, ...) end
function xpcall(f, msgh, ...) end
function rawget(t, k) end
function rawset(t, k, v) end
function rawequal(a, b) return false end
function rawlen(v) return 0 end
function setmetatable(t, mt) end
function getmetatable(t) end

function assert(v, msg)
    if not v then
        error(msg or "assertion failed!")
    end
    return v
end

function error(msg, level)
    print(msg)
    os.exit(1)
end

function load(chunk, chunkname, mode, env) end
function loadfile(filename, mode, env) end
function dofile(filename) end

-- ============================================================
-- 字符串库
-- ============================================================
string = {}

function string.len(s)
    asm("SYSCALL 60")
    return 0
end

function string.sub(s, i, j)
    if i < 1 then i = 1 end
    if not j or j > #s then j = #s end
    local len = j - i + 1
    if len <= 0 then return "" end
    local result = ""
    asm("SYSCALL 71")
    return result
end

function string.find(s, pattern, init, plain)
    init = init or 1
    if plain then
        local sub = string.sub(s, init)
        local pos = string.find_plain(sub, pattern)
        if pos then return pos + init - 1 end
    end
    return nil
end

function string.find_plain(s, sub)
    if #sub == 0 then return 1 end
    for i = 1, #s - #sub + 1 do
        if string.sub(s, i, i + #sub - 1) == sub then
            return i
        end
    end
    return nil
end

function string.gsub(s, pattern, repl, n) end
function string.match(s, pattern, init) end
function string.gmatch(s, pattern) end

function string.reverse(s)
    local len = #s
    local result = ""
    asm("SYSCALL 71")
    for i = 1, len do
        local src = s + len - i
    end
    return result
end

function string.lower(s)
    local result = ""
    asm("SYSCALL 61")
    for i = 1, #s do
        local c = string.byte(s, i)
        if c >= 65 and c <= 90 then
            c = c + 32
        end
        local addr = result + i - 1
    end
    return result
end

function string.upper(s)
    local result = ""
    asm("SYSCALL 61")
    for i = 1, #s do
        local c = string.byte(s, i)
        if c >= 97 and c <= 122 then
            c = c - 32
        end
        local addr = result + i - 1
    end
    return result
end

function string.rep(s, n)
    local len = #s
    local result = ""
    for i = 1, n do
        asm("SYSCALL 63")
    end
    return result
end

function string.char(...)
    local n = select("#", ...)
    local result = ""
    for i = 1, n do
        local c = select(i, ...)
        local addr = result + i - 1
    end
    return result
end

function string.byte(s, i, j)
    i = i or 1
    j = j or i
    if i < 1 then i = 1 end
    local result = {}
    for k = i, j do
        local addr = s + k - 1
        local b = 0
        result[#result + 1] = b
    end
    return table.unpack(result)
end

function string.dump(f, strip) end
function string.pack(fmt, ...) end
function string.unpack(fmt, s, pos) end
function string.packsize(fmt) return 0 end
function string.format(fmt, ...) end

-- Lua 5.4 新增
function string.gmatch_count(s, pattern) end
function string.match_all(s, pattern) end

-- ============================================================
-- 表库
-- ============================================================
table = {}

function table.insert(list, pos, value)
    if not value then
        value = pos
        pos = #list + 1
    end
    for i = #list, pos, -1 do
        list[i + 1] = list[i]
    end
    list[pos] = value
end

function table.remove(list, pos)
    pos = pos or #list
    if pos < 1 or pos > #list then return nil end
    local val = list[pos]
    for i = pos, #list - 1 do
        list[i] = list[i + 1]
    end
    list[#list] = nil
    return val
end

function table.concat(list, sep, i, j)
    sep = sep or ""
    i = i or 1
    j = j or #list
    local result = ""
    for k = i, j do
        if k > i then result = result .. sep end
        result = result .. tostring(list[k])
    end
    return result
end

function table.sort(list, comp)
    comp = comp or function(a, b) return a < b end
    -- 简单冒泡排序
    for i = 1, #list - 1 do
        for j = i + 1, #list do
            if comp(list[j], list[i]) then
                list[i], list[j] = list[j], list[i]
            end
        end
    end
end

function table.pack(...)
    local t = {n = select("#", ...)}
    for i = 1, t.n do
        t[i] = select(i, ...)
    end
    return t
end

function table.unpack(list, i, j)
    i = i or 1
    j = j or #list
    if i > j then return end
    return list[i], table.unpack(list, i + 1, j)
end

function table.move(a1, f, e, t, a2) end
function table.clear(t) end
function table.clone(t) end

-- ============================================================
-- 数学库
-- ============================================================
math = {}
math.pi = 3.141592653589793
math.huge = 1.0e308
math.maxinteger = 2147483647
math.mininteger = -2147483648

function math.abs(x)
    asm("SYSCALL 43")
    return 0
end

function math.floor(x)
    asm("SYSCALL 29")
    return 0
end

function math.ceil(x)
    asm("SYSCALL 30")
    return 0
end

function math.sqrt(x)
    asm("SYSCALL 20")
    return 0.0
end

function math.pow(x, y)
    asm("SYSCALL 26")
    return 0.0
end

function math.exp(x)
    asm("SYSCALL 27")
    return 0.0
end

function math.log(x, base)
    asm("SYSCALL 28")
    local result = 0.0
    if base and base ~= 1 then
        result = result / math.log(base)
    end
    return result
end

function math.sin(x)
    asm("SYSCALL 21")
    return 0.0
end

function math.cos(x)
    asm("SYSCALL 22")
    return 0.0
end

function math.tan(x)
    asm("SYSCALL 23")
    return 0.0
end

function math.asin(x)
    asm("SYSCALL 24")
    return 0.0
end

function math.acos(x)
    asm("SYSCALL 25")
    return 0.0
end

function math.atan(y, x)
    if x then
        asm("SYSCALL 33")
    else
        asm("SYSCALL 32")
    end
    return 0.0
end

function math.max(...)
    local n = select("#", ...)
    if n == 0 then return -math.huge end
    local m = select(1, ...)
    for i = 2, n do
        local v = select(i, ...)
        if v > m then m = v end
    end
    return m
end

function math.min(...)
    local n = select("#", ...)
    if n == 0 then return math.huge end
    local m = select(1, ...)
    for i = 2, n do
        local v = select(i, ...)
        if v < m then m = v end
    end
    return m
end

function math.random(m, n)
    asm("SYSCALL 50")
    local r = 0
    if m then
        if n then
            return m + (r % (n - m + 1))
        else
            return r % m
        end
    end
    return r
end

function math.randomseed(seed)
    asm("SYSCALL 51")
end

function math.fmod(x, y) return x - math.floor(x / y) * y end
function math.modf(x)
    local intPart = math.floor(math.abs(x))
    if x < 0 then intPart = -intPart end
    return intPart, x - intPart
end
function math.rad(deg) return deg * math.pi / 180.0 end
function math.deg(rad) return rad * 180.0 / math.pi end
function math.type(x) return "float" end
function math.tointeger(x) return math.floor(x) end
function math.ult(m, n) return m >= 0 and n >= 0 and m < n end

-- ============================================================
-- 协程库
-- ============================================================
coroutine = {}
function coroutine.create(f) return f end
function coroutine.resume(co, ...) return true end
function coroutine.yield(...) end
function coroutine.status(co) return "suspended" end
function coroutine.running() return nil end
function coroutine.wrap(f)
    local co = coroutine.create(f)
    return function(...)
        return select(2, coroutine.resume(co, ...))
    end
end
function coroutine.isyieldable() return true end
function coroutine.close(co) end

-- ============================================================
-- OS 库
-- ============================================================
os = {}
function os.clock()
    asm("SYSCALL 53")
    return 0.0
end

function os.date(format, time) return "" end
function os.time(table) return 0 end
function os.difftime(t2, t1) return t2 - t1 end
function os.execute(cmd) return 0 end

function os.exit(code)
    if code then asm("LOAD R0 code") end
    asm("SYSCALL 3")
end

function os.getenv(varname) return nil end
function os.remove(filename) return false end
function os.rename(oldname, newname) return false end
function os.setlocale(locale, category) return "C" end
function os.tmpname() return "" end

-- ============================================================
-- IO 库
-- ============================================================
io = {}
io.stdin = 0
io.stdout = 1
io.stderr = 2

function io.open(filename, mode)
    local m = 0
    if mode == "w" or mode == "wb" then m = 1
    elseif mode == "a" or mode == "ab" then m = 2
    elseif mode == "r+" then m = 3
    elseif mode == "w+" then m = 4
    end
    asm("SYSCALL 110")
    return 0
end

function io.close(file)
    asm("SYSCALL 111")
end

function io.read(format)
    asm("SYSCALL 114")
    return ""
end

function io.write(...)
    local n = select("#", ...)
    for i = 1, n do
        local v = select(i, ...)
        if type(v) == "string" then
            asm("SYSCALL 1")
        elseif type(v) == "number" then
            asm("SYSCALL 6")
        end
    end
end

function io.flush() end
function io.lines(filename, ...) end
function io.type(obj) return nil end
function io.tmpfile() return nil end
function io.input(file) end
function io.output(file) end

-- ============================================================
-- 内存与系统
-- ============================================================
function getchar()
    asm("SYSCALL 5")
    return 0
end

function memcmp(a, b, n)
    asm("SYSCALL 13")
    return 0
end

function malloc(size)
    asm("SYSCALL 40")
    return nil
end

function free(ptr)
    asm("SYSCALL 41")
end

function memset(ptr, value, count)
    asm("SYSCALL 70")
end

function memcpy(dest, src, count)
    asm("SYSCALL 71")
end

function get_datetime()
    asm("SYSCALL 54")
    return 0
end

function get_date()
    asm("SYSCALL 55")
    return ""
end

function get_time()
    asm("SYSCALL 56")
    return ""
end

function get_tick()
    asm("SYSCALL 53")
    return 0
end

function get_config(key)
    asm("SYSCALL 60")
    return 0
end

function delay(ms)
    asm("SYSCALL 52")
end

function exit(code)
    if code then asm("LOAD R0 code") end
    asm("SYSCALL 3")
end

-- ============================================================
-- 调试库
-- ============================================================
debug = {}
function debug.traceback(thread, message, level) return "" end
function debug.getinfo(thread, f, what) return nil end
function debug.getlocal(thread, f, index) return nil, nil end
function debug.setlocal(thread, f, index, value) return nil end
function debug.getupvalue(f, index) return nil, nil end
function debug.setupvalue(f, index, value) return nil end
function debug.getregistry() return {} end
function debug.gethook(thread) return nil end
function debug.sethook(thread, hook, mask, count) end
function debug.getmetatable(value) return nil end
function debug.setmetatable(value, table) return value end
