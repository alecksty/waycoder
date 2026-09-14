# Python 标准库 - VML 实现
# 用法: import stdlib  (编译器自动链接)

# === 控制台 I/O ===
def print(*args, sep=' ', end='\n'):
    n = len(args)
    i = 0
    while i < n:
        val = args[i]
        t = type(val)
        if t == "str":
            asm("SYSCALL 1")
        elif t == "int":
            asm("SYSCALL 6")
        elif t == "float":
            asm("SYSCALL 59")
        elif t == "bool":
            if val:
                asm("SYSCALL 1")  # prints "true"
            else:
                asm("SYSCALL 1")  # prints "false"
        elif t == "char":
            asm("SYSCALL 4")
        if i < n - 1:
            asm("SYSCALL 1")  # separator
        i = i + 1
    asm("LOAD R0 #10")
    asm("SYSCALL 4")  # newline

def input(prompt=''):
    if prompt:
        asm("SYSCALL 1")
    asm("SYSCALL 2")
    return ""

def printf(fmt, *args):
    s = fmt
    i = 0
    n = len(args)
    while i < n:
        idx = str_find(s, "%")
        if idx == -1:
            break
        arg = args[i]
        t = type(arg)
        if t == "str":
            asm("SYSCALL 1")
        elif t == "int":
            asm("SYSCALL 6")
        elif t == "float":
            asm("SYSCALL 59")
        s = s[idx+2:]
        i = i + 1

def getchar():
    asm("SYSCALL 5")
    return ''

# === 类型系统 ===
def len(obj):
    t = type(obj)
    if t == "str":
        asm("SYSCALL 60")
        return 0
    elif t == "list":
        return 0  # stub
    return 0

def type(obj):
    return "unknown"

def range(start, stop=None, step=1):
    if stop is None:
        stop = start
        start = 0
    result = []
    i = start
    while i < stop:
        result.append(i)
        i = i + step
    return result

def int(x, base=10):
    t = type(x)
    if t == "str":
        asm("SYSCALL 40")
        return 0
    if t == "float":
        return int(x)
    return x

def float(x):
    t = type(x)
    if t == "str":
        asm("SYSCALL 41")
        return 0.0
    return float(x)

def str(x):
    t = type(x)
    if t == "int":
        asm("SYSCALL 42")
        return ""
    elif t == "float":
        asm("SYSCALL 58")
        return ""
    elif t == "bool":
        if x:
            return "True"
        else:
            return "False"
    return str(x)

def chr(x):
    return chr(x)

def ord(x):
    return ord(x)

def bool(x):
    if x:
        return True
    return False

def hex(x):
    asm("SYSCALL 10")
    return ""

def oct(x):
    n = x
    result = ""
    if n == 0:
        return "0o0"
    neg = n < 0
    if neg:
        n = -n
    while n > 0:
        d = n % 8
        n = n / 8
        c = chr(d + 48)
        result = c + result
    if neg:
        result = "-" + result
    return "0o" + result

def bin(x):
    n = x
    result = ""
    if n == 0:
        return "0b0"
    neg = n < 0
    if neg:
        n = -n
    while n > 0:
        d = n % 2
        n = n / 2
        c = chr(d + 48)
        result = c + result
    if neg:
        result = "-" + result
    return "0b" + result

# === 数学函数 ===
def abs(x):
    t = type(x)
    if t == "int":
        asm("SYSCALL 43")
        return 0
    elif t == "float":
        asm("SYSCALL 44")
        return 0.0
    return 0

def max(*args):
    n = len(args)
    if n == 0:
        return None
    m = args[0]
    i = 1
    while i < n:
        if args[i] > m:
            m = args[i]
        i = i + 1
    return m

def min(*args):
    n = len(args)
    if n == 0:
        return None
    m = args[0]
    i = 1
    while i < n:
        if args[i] < m:
            m = args[i]
        i = i + 1
    return m

def sum(iterable, start=0):
    result = start
    i = 0
    while i < len(iterable):
        result = result + iterable[i]
        i = i + 1
    return result

def round(x, ndigits=0):
    if ndigits == 0:
        asm("SYSCALL 31")
        return 0
    factor = 1.0
    i = 0
    while i < ndigits:
        factor = factor * 10.0
        i = i + 1
    xf = x * factor
    asm("SYSCALL 31")  # round in asm
    return 0

def divmod(a, b):
    q = a / b
    r = a % b
    return (q, r)

def pow(x, y, mod=None):
    if mod is None:
        asm("SYSCALL 26")
        return 0.0
    else:
        return pow(x, y) % mod

# 浮点数学 (math 模块)
def math_sqrt(x):
    asm("SYSCALL 20")
    return 0.0

def math_pow(x, y):
    asm("SYSCALL 26")
    return 0.0

def math_sin(x):
    asm("SYSCALL 21")
    return 0.0

def math_cos(x):
    asm("SYSCALL 22")
    return 0.0

def math_tan(x):
    asm("SYSCALL 23")
    return 0.0

def math_asin(x):
    asm("SYSCALL 24")
    return 0.0

def math_acos(x):
    asm("SYSCALL 25")
    return 0.0

def math_atan(x):
    asm("SYSCALL 32")
    return 0.0

def math_atan2(y, x):
    asm("SYSCALL 33")
    return 0.0

def math_exp(x):
    asm("SYSCALL 27")
    return 0.0

def math_log(x, base=None):
    asm("SYSCALL 28")
    result = 0.0
    if base is not None:
        result = result / math_log(base)
    return result

def math_log10(x):
    return math_log(x) / math_log(10.0)

def math_ceil(x):
    asm("SYSCALL 30")
    return 0.0

def math_floor(x):
    asm("SYSCALL 29")
    return 0.0

def math_fabs(x):
    asm("SYSCALL 44")
    return 0.0

def math_fmod(x, y):
    return x - math_floor(x / y) * y

def math_degrees(x):
    return x * 180.0 / math_pi()

def math_radians(x):
    return x * math_pi() / 180.0

def math_pi():
    return 3.141592653589793

def math_e():
    return 2.718281828459045

# 随机数
def random():
    asm("SYSCALL 50")
    return 0

def random_seed(seed):
    asm("SYSCALL 51")

def randint(a, b):
    r = random() % (b - a + 1)
    return a + r

def uniform(a, b):
    r = float(random()) / 2147483647.0
    return a + r * (b - a)

# === 字符串操作 ===
def str_len(s):
    asm("SYSCALL 60")
    return 0

def str_to_upper(s):
    result = ""
    i = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c >= 'a' and c <= 'z':
            c = chr(ord(c) - 32)
        result = result + c
        i = i + 1
    return result

def str_to_lower(s):
    result = ""
    i = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c >= 'A' and c <= 'Z':
            c = chr(ord(c) + 32)
        result = result + c
        i = i + 1
    return result

def str_split(s, sep=None):
    result = []
    n = len(s)
    if n == 0:
        return result
    if sep is None:
        # split by whitespace
        word = ""
        i = 0
        while i < n:
            c = s[i]
            if c == ' ' or c == '\t' or c == '\n':
                if word != "":
                    result.append(word)
                    word = ""
            else:
                word = word + c
            i = i + 1
        if word != "":
            result.append(word)
    else:
        seplen = len(sep)
        start = 0
        i = 0
        while i <= n - seplen:
            if str_substr(s, i, seplen) == sep:
                result.append(str_substr(s, start, i - start))
                start = i + seplen
                i = start
            else:
                i = i + 1
        result.append(str_substr(s, start, n - start))
    return result

def str_substr(s, start, length):
    # helper: extract substring
    result = ""
    i = 0
    while i < length:
        result = result + s[start + i]
        i = i + 1
    return result

def str_join(iterable, sep=''):
    result = ""
    n = len(iterable)
    i = 0
    while i < n:
        if i > 0:
            result = result + sep
        result = result + iterable[i]
        i = i + 1
    return result

def str_find(s, sub, start=0):
    n = len(s)
    m = len(sub)
    if m == 0:
        return start
    i = start
    while i <= n - m:
        j = 0
        match = True
        while j < m:
            if s[i + j] != sub[j]:
                match = False
                break
            j = j + 1
        if match:
            return i
        i = i + 1
    return -1

def str_rfind(s, sub):
    n = len(s)
    m = len(sub)
    if m == 0:
        return n
    i = n - m
    while i >= 0:
        j = 0
        match = True
        while j < m:
            if s[i + j] != sub[j]:
                match = False
                break
            j = j + 1
        if match:
            return i
        i = i - 1
    return -1

def str_replace(s, old, new):
    result = ""
    n = len(s)
    m = len(old)
    i = 0
    while i < n:
        if i <= n - m and str_substr(s, i, m) == old:
            result = result + new
            i = i + m
        else:
            result = result + s[i]
            i = i + 1
    return result

def str_strip(s, chars=None):
    n = len(s)
    start = 0
    end = n - 1
    if chars is None:
        while start < n and (s[start] == ' ' or s[start] == '\t' or s[start] == '\n' or s[start] == '\r'):
            start = start + 1
        while end >= start and (s[end] == ' ' or s[end] == '\t' or s[end] == '\n' or s[end] == '\r'):
            end = end - 1
    else:
        while start < n and str_contains_char(chars, s[start]):
            start = start + 1
        while end >= start and str_contains_char(chars, s[end]):
            end = end - 1
    return str_substr(s, start, end - start + 1)

def str_contains_char(s, c):
    i = 0
    while i < len(s):
        if s[i] == c:
            return True
        i = i + 1
    return False

def str_lstrip(s, chars=None):
    n = len(s)
    start = 0
    if chars is None:
        while start < n and (s[start] == ' ' or s[start] == '\t' or s[start] == '\n' or s[start] == '\r'):
            start = start + 1
    else:
        while start < n and str_contains_char(chars, s[start]):
            start = start + 1
    return str_substr(s, start, n - start)

def str_rstrip(s, chars=None):
    n = len(s)
    end = n - 1
    if chars is None:
        while end >= 0 and (s[end] == ' ' or s[end] == '\t' or s[end] == '\n' or s[end] == '\r'):
            end = end - 1
    else:
        while end >= 0 and str_contains_char(chars, s[end]):
            end = end - 1
    return str_substr(s, 0, end + 1)

def str_startswith(s, prefix):
    m = len(prefix)
    if m > len(s):
        return False
    return str_substr(s, 0, m) == prefix

def str_endswith(s, suffix):
    m = len(suffix)
    n = len(s)
    if m > n:
        return False
    return str_substr(s, n - m, m) == suffix

def str_count(s, sub):
    count = 0
    n = len(s)
    m = len(sub)
    if m == 0:
        return 0
    i = 0
    while i <= n - m:
        if str_substr(s, i, m) == sub:
            count = count + 1
            i = i + m
        else:
            i = i + 1
    return count

def str_isalpha(s):
    n = len(s)
    if n == 0:
        return False
    i = 0
    while i < n:
        c = s[i]
        if not ((c >= 'A' and c <= 'Z') or (c >= 'a' and c <= 'z')):
            return False
        i = i + 1
    return True

def str_isdigit(s):
    n = len(s)
    if n == 0:
        return False
    i = 0
    while i < n:
        c = s[i]
        if not (c >= '0' and c <= '9'):
            return False
        i = i + 1
    return True

def str_isalnum(s):
    n = len(s)
    if n == 0:
        return False
    i = 0
    while i < n:
        c = s[i]
        if not ((c >= 'A' and c <= 'Z') or (c >= 'a' and c <= 'z') or (c >= '0' and c <= '9')):
            return False
        i = i + 1
    return True

def str_isspace(s):
    n = len(s)
    if n == 0:
        return False
    i = 0
    while i < n:
        c = s[i]
        if c != ' ' and c != '\t' and c != '\n' and c != '\r':
            return False
        i = i + 1
    return True

def str_islower(s):
    has_alpha = False
    i = 0
    while i < len(s):
        c = s[i]
        if c >= 'A' and c <= 'Z':
            return False
        if c >= 'a' and c <= 'z':
            has_alpha = True
        i = i + 1
    return has_alpha

def str_isupper(s):
    has_alpha = False
    i = 0
    while i < len(s):
        c = s[i]
        if c >= 'a' and c <= 'z':
            return False
        if c >= 'A' and c <= 'Z':
            has_alpha = True
        i = i + 1
    return has_alpha

def str_capitalize(s):
    n = len(s)
    if n == 0:
        return ""
    result = ""
    first = s[0]
    if first >= 'a' and first <= 'z':
        first = chr(ord(first) - 32)
    result = result + first
    i = 1
    while i < n:
        c = s[i]
        if c >= 'A' and c <= 'Z':
            c = chr(ord(c) + 32)
        result = result + c
        i = i + 1
    return result

def str_title(s):
    result = ""
    n = len(s)
    new_word = True
    i = 0
    while i < n:
        c = s[i]
        if c == ' ' or c == '\t' or c == '\n':
            new_word = True
            result = result + c
        elif new_word:
            if c >= 'a' and c <= 'z':
                c = chr(ord(c) - 32)
            new_word = False
            result = result + c
        else:
            if c >= 'A' and c <= 'Z':
                c = chr(ord(c) + 32)
            result = result + c
        i = i + 1
    return result

def str_swapcase(s):
    result = ""
    i = 0
    while i < len(s):
        c = s[i]
        if c >= 'A' and c <= 'Z':
            c = chr(ord(c) + 32)
        elif c >= 'a' and c <= 'z':
            c = chr(ord(c) - 32)
        result = result + c
        i = i + 1
    return result

def str_repeat(s, n):
    result = ""
    i = 0
    while i < n:
        result = result + s
        i = i + 1
    return result

def str_reverse(s):
    result = ""
    i = len(s) - 1
    while i >= 0:
        result = result + s[i]
        i = i - 1
    return result

def str_upper(s): return str_to_upper(s)
def str_lower(s): return str_to_lower(s)

# === 列表操作 ===
def list_append(lst, item):
    lst[len(lst)] = item

def list_extend(lst, iterable):
    i = 0
    while i < len(iterable):
        list_append(lst, iterable[i])
        i = i + 1

def list_insert(lst, index, item):
    n = len(lst)
    if index < 0:
        index = n + index
    if index < 0:
        index = 0
    if index > n:
        index = n
    i = n
    while i > index:
        lst[i] = lst[i - 1]
        i = i - 1
    lst[index] = item

def list_remove(lst, item):
    idx = list_index(lst, item)
    if idx >= 0:
        list_pop(lst, idx)

def list_pop(lst, index=-1):
    n = len(lst)
    if n == 0:
        return None
    if index < 0:
        index = n + index
    val = lst[index]
    i = index
    while i < n - 1:
        lst[i] = lst[i + 1]
        i = i + 1
    # truncate last element
    return val

def list_index(lst, item):
    i = 0
    while i < len(lst):
        if lst[i] == item:
            return i
        i = i + 1
    return -1

def list_count(lst, item):
    count = 0
    i = 0
    while i < len(lst):
        if lst[i] == item:
            count = count + 1
        i = i + 1
    return count

def list_sort(lst, reverse=False):
    n = len(lst)
    i = 0
    while i < n - 1:
        j = i + 1
        while j < n:
            if reverse:
                if lst[j] > lst[i]:
                    tmp = lst[i]
                    lst[i] = lst[j]
                    lst[j] = tmp
            else:
                if lst[j] < lst[i]:
                    tmp = lst[i]
                    lst[i] = lst[j]
                    lst[j] = tmp
            j = j + 1
        i = i + 1

def list_reverse(lst):
    n = len(lst)
    i = 0
    while i < n / 2:
        tmp = lst[i]
        lst[i] = lst[n - 1 - i]
        lst[n - 1 - i] = tmp
        i = i + 1

def list_clear(lst):
    i = 0
    while i < len(lst):
        lst[i] = None
        i = i + 1

def list_copy(lst):
    result = []
    i = 0
    while i < len(lst):
        result.append(lst[i])
        i = i + 1
    return result

def sorted(iterable, reverse=False):
    result = list_copy(iterable)
    list_sort(result, reverse)
    return result

def reversed(sequence):
    result = []
    i = len(sequence) - 1
    while i >= 0:
        result.append(sequence[i])
        i = i - 1
    return result

def enumerate(iterable, start=0):
    result = []
    i = 0
    while i < len(iterable):
        result.append((start + i, iterable[i]))
        i = i + 1
    return result

def zip(*iterables):
    result = []
    n = len(iterables)
    if n == 0:
        return result
    min_len = len(iterables[0])
    i = 1
    while i < n:
        l = len(iterables[i])
        if l < min_len:
            min_len = l
        i = i + 1
    j = 0
    while j < min_len:
        tup = []
        k = 0
        while k < n:
            tup.append(iterables[k][j])
            k = k + 1
        result.append(tup)
        j = j + 1
    return result

def map(func, iterable):
    result = []
    i = 0
    while i < len(iterable):
        result.append(func(iterable[i]))
        i = i + 1
    return result

def filter(func, iterable):
    result = []
    i = 0
    while i < len(iterable):
        if func(iterable[i]):
            result.append(iterable[i])
        i = i + 1
    return result

# === 字典操作 ===
def dict_get(d, key, default=None):
    return d[key]

def dict_keys(d):
    result = []
    return result

def dict_values(d):
    result = []
    return result

def dict_items(d):
    result = []
    return result

def dict_pop(d, key, default=None):
    val = d[key]
    return val

def dict_update(d, other):
    pass

def dict_clear(d):
    pass

def dict_copy(d):
    return {}

# === 系统 ===
def exit(code=0):
    asm("SYSCALL 3")

def delay(ms):
    asm("SYSCALL 52")

def get_tick():
    asm("SYSCALL 53")
    return 0

def get_config(key):
    asm("SYSCALL 60")
    return 0

def get_date():
    asm("SYSCALL 55")
    return ""

def get_time():
    asm("SYSCALL 56")
    return ""

def get_datetime():
    asm("SYSCALL 54")
    return 0

# === 内存 ===
def memset(ptr, value, count):
    asm("SYSCALL 70")

def memcpy(dest, src, count):
    asm("SYSCALL 71")

def memcmp(a, b, count):
    asm("SYSCALL 13")
    return 0

def malloc(size):
    asm("SYSCALL 40")
    return 0

def free(ptr):
    asm("SYSCALL 41")
