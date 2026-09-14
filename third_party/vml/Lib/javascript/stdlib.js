// VML JavaScript 标准库 — 运行时基础支持
// 用法: import stdlib

// === console 对象 ===
var console = {
    log: function(s) {
        var t = typeof s;
        if (t === "string") {
            asm("CALL shared_print_str");
        } else if (t === "number") {
            asm("SYSCALL #59");
        } else if (t === "boolean") {
            if (s) { asm("CALL shared_print_str"); }
            else { asm("CALL shared_print_str"); }
        }
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    },
    logInt: function(i) {
        asm("SYSCALL #6");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    },
    logFloat: function(f) {
        asm("SYSCALL #59");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    },
    logHex: function(i) {
        asm("SYSCALL #10");
    },
    error: function(s) {
        asm("CALL shared_print_str");
        asm("LOAD R0 #10");
        asm("SYSCALL #4");
    },
    write: function(s) {
        asm("CALL shared_print_str");
    }
};

// === 全局 I/O 函数 ===
function print(s) { console.log(s); }
function println(s) { console.log(s); }
function prompt(msg) { return ""; }
function getchar() {
    asm("SYSCALL #5");
    return '\0';
}

// === 类型转换 ===
function parseInt(s, radix) {
    asm("SYSCALL #40");
    return 0;
}
function parseFloat(s) {
    asm("SYSCALL #41");
    return 0.0;
}
function toString(x) {
    var t = typeof x;
    if (t === "number") {
        asm("SYSCALL #42");
        return "";
    }
    return "";
}
function isNaN(x) { return x !== x; }
function isFinite(x) {
    if (x !== x) return false;
    if (x === Infinity) return false;
    if (x === -Infinity) return false;
    return true;
}
function typeof_val(x) {
    var t = typeof x;
    return t;
}

// === Math 对象 ===
var Math = {
    PI: 3.141592653589793,
    E: 2.718281828459045,
    LN2: 0.6931471805599453,
    LN10: 2.302585092994046,
    SQRT2: 1.4142135623730951,

    abs: function(x) {
        if (typeof x === "number" && x === (x | 0)) {
            asm("SYSCALL #43");
            return 0;
        }
        asm("SYSCALL #44");
        return 0.0;
    },
    min: function(a, b) { return a < b ? a : b; },
    max: function(a, b) { return a > b ? a : b; },
    floor: function(x) {
        asm("SYSCALL #29");
        return 0.0;
    },
    ceil: function(x) {
        asm("SYSCALL #30");
        return 0.0;
    },
    round: function(x) {
        asm("SYSCALL #31");
        return 0.0;
    },
    trunc: function(x) {
        if (x >= 0) {
            asm("SYSCALL #29");
        } else {
            asm("SYSCALL #30");
        }
        return 0.0;
    },
    sign: function(x) {
        if (x > 0) return 1;
        if (x < 0) return -1;
        return 0;
    },

    sqrt: function(x) {
        asm("SYSCALL #20");
        return 0.0;
    },
    pow: function(x, y) {
        asm("SYSCALL #26");
        return 0.0;
    },

    sin: function(x) {
        asm("SYSCALL #21");
        return 0.0;
    },
    cos: function(x) {
        asm("SYSCALL #22");
        return 0.0;
    },
    tan: function(x) {
        asm("SYSCALL #23");
        return 0.0;
    },
    asin: function(x) {
        asm("SYSCALL #24");
        return 0.0;
    },
    acos: function(x) {
        asm("SYSCALL #25");
        return 0.0;
    },
    atan: function(x) {
        asm("SYSCALL #32");
        return 0.0;
    },
    atan2: function(y, x) {
        asm("SYSCALL #33");
        return 0.0;
    },

    exp: function(x) {
        asm("SYSCALL #27");
        return 0.0;
    },
    log: function(x) {
        asm("SYSCALL #28");
        return 0.0;
    },
    log10: function(x) { return Math.log(x) / Math.LN10; },
    log2: function(x) { return Math.log(x) / Math.LN2; },

    random: function() {
        asm("SYSCALL #50");
        return 0;
    },
    fmod: function(x, y) {
        var q = x / y;
        var f = Math.floor(q > 0 ? q : q);
        return x - f * y;
    }
};

// === String 原型扩展 ===
// Note: "str".length uses asm("SYSCALL #60") for runtime access
String.prototype._length = function() {
    asm("SYSCALL #60");
    return 0;
};

String.prototype.charAt = function(index) {
    var len = this._length();
    if (index < 0 || index >= len) return '';
    return this[index];
};

String.prototype.charCodeAt = function(index) {
    var len = this._length();
    if (index < 0 || index >= len) return 0;
    return this[index];
};

String.prototype.indexOf = function(searchValue, fromIndex) {
    fromIndex = fromIndex || 0;
    var len = this._length();
    var searchLen = searchValue._length();
    if (searchLen === 0) return fromIndex < len ? fromIndex : len;
    if (fromIndex < 0) fromIndex = 0;
    var i = fromIndex;
    while (i <= len - searchLen) {
        var j = 0;
        var match = true;
        while (j < searchLen) {
            if (this[i + j] !== searchValue[j]) {
                match = false;
                break;
            }
            j = j + 1;
        }
        if (match) return i;
        i = i + 1;
    }
    return -1;
};

String.prototype.lastIndexOf = function(searchValue, fromIndex) {
    var len = this._length();
    var searchLen = searchValue._length();
    if (searchLen === 0) return len;
    fromIndex = fromIndex || len;
    if (fromIndex < 0) fromIndex = 0;
    if (fromIndex > len - searchLen) fromIndex = len - searchLen;
    var i = fromIndex;
    while (i >= 0) {
        var j = 0;
        var match = true;
        while (j < searchLen) {
            if (this[i + j] !== searchValue[j]) {
                match = false;
                break;
            }
            j = j + 1;
        }
        if (match) return i;
        i = i - 1;
    }
    return -1;
};

String.prototype.includes = function(searchString, position) {
    return this.indexOf(searchString, position) !== -1;
};

String.prototype.startsWith = function(searchString, position) {
    position = position || 0;
    if (position + searchString._length() > this._length()) return false;
    var i = 0;
    while (i < searchString._length()) {
        if (this[position + i] !== searchString[i]) return false;
        i = i + 1;
    }
    return true;
};

String.prototype.endsWith = function(searchString, length) {
    length = length || this._length();
    var pos = length - searchString._length();
    if (pos < 0) return false;
    var i = 0;
    while (i < searchString._length()) {
        if (this[pos + i] !== searchString[i]) return false;
        i = i + 1;
    }
    return true;
};

String.prototype.substring = function(start, end) {
    var len = this._length();
    if (start === undefined) start = 0;
    if (end === undefined) end = len;
    if (start < 0) start = 0;
    if (end < 0) end = 0;
    if (start > end) { var t = start; start = end; end = t; }
    if (start > len) start = len;
    if (end > len) end = len;
    var result = "";
    var i = start;
    while (i < end) {
        result = result + this[i];
        i = i + 1;
    }
    return result;
};

String.prototype.substr = function(start, length) {
    var len = this._length();
    if (start < 0) start = len + start;
    if (start < 0) start = 0;
    if (length === undefined) length = len - start;
    if (length < 0) length = 0;
    var end = start + length;
    if (end > len) end = len;
    var result = "";
    var i = start;
    while (i < end) {
        result = result + this[i];
        i = i + 1;
    }
    return result;
};

String.prototype.slice = function(beginSlice, endSlice) {
    var len = this._length();
    if (beginSlice === undefined) beginSlice = 0;
    if (endSlice === undefined) endSlice = len;
    if (beginSlice < 0) beginSlice = len + beginSlice;
    if (beginSlice < 0) beginSlice = 0;
    if (endSlice < 0) endSlice = len + endSlice;
    if (endSlice < 0) endSlice = 0;
    if (beginSlice > len) beginSlice = len;
    if (endSlice > len) endSlice = len;
    if (beginSlice >= endSlice) return "";
    var result = "";
    var i = beginSlice;
    while (i < endSlice) {
        result = result + this[i];
        i = i + 1;
    }
    return result;
};

String.prototype.split = function(separator, limit) {
    var result = [];
    var len = this._length();
    if (len === 0) return result;
    if (separator === undefined) {
        result.push(this);
        return result;
    }
    var sepLen = separator._length();
    if (sepLen === 0) {
        var i = 0;
        while (i < len && (limit === undefined || result.length < limit)) {
            result.push(this[i]);
            i = i + 1;
        }
        return result;
    }
    var start = 0;
    var i = 0;
    while (i <= len - sepLen) {
        if (this.indexOf(separator, i) === i) {
            if (limit === undefined || result.length < limit) {
                result.push(this.substring(start, i));
            }
            start = i + sepLen;
            i = start;
        } else {
            i = i + 1;
        }
    }
    if (limit === undefined || result.length < limit) {
        result.push(this.substring(start, len));
    }
    return result;
};

String.prototype.replace = function(search, replacement) {
    var len = this._length();
    var searchLen = search._length();
    if (searchLen === 0) return this;
    var result = "";
    var i = 0;
    while (i < len) {
        if (i <= len - searchLen && this.substring(i, i + searchLen) === search) {
            result = result + replacement;
            i = i + searchLen;
        } else {
            result = result + this[i];
            i = i + 1;
        }
    }
    return result;
};

String.prototype.toUpperCase = function() {
    var result = "";
    var i = 0;
    var len = this._length();
    while (i < len) {
        var c = this[i];
        if (c >= 'a' && c <= 'z') {
            c = String.fromCharCode(c.charCodeAt(0) - 32);
        }
        result = result + c;
        i = i + 1;
    }
    return result;
};

String.prototype.toLowerCase = function() {
    var result = "";
    var i = 0;
    var len = this._length();
    while (i < len) {
        var c = this[i];
        if (c >= 'A' && c <= 'Z') {
            c = String.fromCharCode(c.charCodeAt(0) + 32);
        }
        result = result + c;
        i = i + 1;
    }
    return result;
};

String.prototype.trim = function() {
    var len = this._length();
    var start = 0;
    var end = len - 1;
    while (start < len && (this[start] === ' ' || this[start] === '\t' || this[start] === '\n' || this[start] === '\r')) {
        start = start + 1;
    }
    while (end >= start && (this[end] === ' ' || this[end] === '\t' || this[end] === '\n' || this[end] === '\r')) {
        end = end - 1;
    }
    return this.substring(start, end + 1);
};

String.prototype.trimStart = function() {
    var len = this._length();
    var start = 0;
    while (start < len && (this[start] === ' ' || this[start] === '\t' || this[start] === '\n' || this[start] === '\r')) {
        start = start + 1;
    }
    return this.substring(start, len);
};

String.prototype.trimEnd = function() {
    var len = this._length();
    var end = len - 1;
    while (end >= 0 && (this[end] === ' ' || this[end] === '\t' || this[end] === '\n' || this[end] === '\r')) {
        end = end - 1;
    }
    return this.substring(0, end + 1);
};

String.prototype.repeat = function(count) {
    var result = "";
    var i = 0;
    while (i < count) {
        result = result + this;
        i = i + 1;
    }
    return result;
};

String.prototype.concat = function() {
    var result = this;
    var i = 0;
    while (i < arguments.length) {
        result = result + arguments[i];
        i = i + 1;
    }
    return result;
};

String.prototype.match = function(regexp) { return null; };
String.prototype.search = function(regexp) { return -1; };
String.prototype.localeCompare = function(compareString) {
    asm("SYSCALL #62");
    return 0;
};

// === Array 原型扩展 ===
Array.prototype.push = function(item) {
    this[this.length] = item;
    return this.length;
};

Array.prototype.pop = function() {
    if (this.length === 0) return undefined;
    var val = this[this.length - 1];
    this.length = this.length - 1;
    return val;
};

Array.prototype.shift = function() {
    if (this.length === 0) return undefined;
    var val = this[0];
    var i = 0;
    while (i < this.length - 1) {
        this[i] = this[i + 1];
        i = i + 1;
    }
    this.length = this.length - 1;
    return val;
};

Array.prototype.unshift = function() {
    var argsLen = arguments.length;
    if (argsLen === 0) return this.length;
    var i = this.length - 1;
    while (i >= 0) {
        this[i + argsLen] = this[i];
        i = i - 1;
    }
    var j = 0;
    while (j < argsLen) {
        this[j] = arguments[j];
        j = j + 1;
    }
    return this.length;
};

Array.prototype.indexOf = function(searchElement, fromIndex) {
    fromIndex = fromIndex || 0;
    if (fromIndex < 0) fromIndex = this.length + fromIndex;
    if (fromIndex < 0) fromIndex = 0;
    var i = fromIndex;
    while (i < this.length) {
        if (this[i] === searchElement) return i;
        i = i + 1;
    }
    return -1;
};

Array.prototype.lastIndexOf = function(searchElement, fromIndex) {
    if (fromIndex === undefined) fromIndex = this.length - 1;
    if (fromIndex < 0) fromIndex = this.length + fromIndex;
    var i = fromIndex;
    while (i >= 0) {
        if (this[i] === searchElement) return i;
        i = i - 1;
    }
    return -1;
};

Array.prototype.includes = function(searchElement, fromIndex) {
    return this.indexOf(searchElement, fromIndex) !== -1;
};

Array.prototype.slice = function(begin, end) {
    var result = [];
    var len = this.length;
    if (begin === undefined) begin = 0;
    if (end === undefined) end = len;
    if (begin < 0) begin = len + begin;
    if (begin < 0) begin = 0;
    if (end < 0) end = len + end;
    if (end > len) end = len;
    var i = begin;
    while (i < end) {
        result.push(this[i]);
        i = i + 1;
    }
    return result;
};

Array.prototype.splice = function(start, deleteCount) {
    var result = [];
    var len = this.length;
    if (start === undefined) start = 0;
    if (start < 0) start = len + start;
    if (start < 0) start = 0;
    if (deleteCount === undefined) deleteCount = len - start;
    if (deleteCount > len - start) deleteCount = len - start;
    // copy deleted elements
    var i = 0;
    while (i < deleteCount) {
        result.push(this[start + i]);
        i = i + 1;
    }
    // shift remaining elements
    var newItems = [];
    var j = 2;
    while (j < arguments.length) {
        newItems.push(arguments[j]);
        j = j + 1;
    }
    var newLen = len - deleteCount + newItems.length;
    // shift right if inserting more than deleting
    if (newItems.length > deleteCount) {
        var shift = newItems.length - deleteCount;
        var k = len - 1;
        while (k >= start + deleteCount) {
            this[k + shift] = this[k];
            k = k - 1;
        }
    } else if (newItems.length < deleteCount) {
        var shift = deleteCount - newItems.length;
        var k = start + deleteCount;
        while (k < len) {
            this[k - shift] = this[k];
            k = k + 1;
        }
    }
    // insert new items
    var m = 0;
    while (m < newItems.length) {
        this[start + m] = newItems[m];
        m = m + 1;
    }
    this.length = newLen;
    return result;
};

Array.prototype.reverse = function() {
    var i = 0;
    var n = this.length;
    while (i < n / 2) {
        var tmp = this[i];
        this[i] = this[n - 1 - i];
        this[n - 1 - i] = tmp;
        i = i + 1;
    }
    return this;
};

Array.prototype.sort = function(compareFn) {
    var n = this.length;
    var i = 0;
    while (i < n - 1) {
        var j = i + 1;
        while (j < n) {
            var shouldSwap = false;
            if (compareFn) {
                shouldSwap = compareFn(this[j], this[i]) < 0;
            } else {
                shouldSwap = String(this[j]) < String(this[i]);
            }
            if (shouldSwap) {
                var tmp = this[i];
                this[i] = this[j];
                this[j] = tmp;
            }
            j = j + 1;
        }
        i = i + 1;
    }
    return this;
};

Array.prototype.join = function(separator) {
    if (separator === undefined) separator = ",";
    var result = "";
    var i = 0;
    while (i < this.length) {
        if (i > 0) result = result + separator;
        result = result + this[i];
        i = i + 1;
    }
    return result;
};

Array.prototype.concat = function() {
    var result = this.slice();
    var i = 0;
    while (i < arguments.length) {
        var arg = arguments[i];
        if (arg instanceof Array) {
            var j = 0;
            while (j < arg.length) {
                result.push(arg[j]);
                j = j + 1;
            }
        } else {
            result.push(arg);
        }
        i = i + 1;
    }
    return result;
};

Array.prototype.forEach = function(callback) {
    var i = 0;
    while (i < this.length) {
        callback(this[i], i, this);
        i = i + 1;
    }
};

Array.prototype.map = function(callback) {
    var result = [];
    var i = 0;
    while (i < this.length) {
        result.push(callback(this[i], i, this));
        i = i + 1;
    }
    return result;
};

Array.prototype.filter = function(callback) {
    var result = [];
    var i = 0;
    while (i < this.length) {
        if (callback(this[i], i, this)) {
            result.push(this[i]);
        }
        i = i + 1;
    }
    return result;
};

Array.prototype.reduce = function(callback, initialValue) {
    var i = 0;
    var accumulator;
    if (initialValue === undefined) {
        if (this.length === 0) return undefined;
        accumulator = this[0];
        i = 1;
    } else {
        accumulator = initialValue;
    }
    while (i < this.length) {
        accumulator = callback(accumulator, this[i], i, this);
        i = i + 1;
    }
    return accumulator;
};

Array.prototype.every = function(callback) {
    var i = 0;
    while (i < this.length) {
        if (!callback(this[i], i, this)) return false;
        i = i + 1;
    }
    return true;
};

Array.prototype.some = function(callback) {
    var i = 0;
    while (i < this.length) {
        if (callback(this[i], i, this)) return true;
        i = i + 1;
    }
    return false;
};

Array.prototype.find = function(callback) {
    var i = 0;
    while (i < this.length) {
        if (callback(this[i], i, this)) return this[i];
        i = i + 1;
    }
    return undefined;
};

Array.prototype.findIndex = function(callback) {
    var i = 0;
    while (i < this.length) {
        if (callback(this[i], i, this)) return i;
        i = i + 1;
    }
    return -1;
};

Array.prototype.fill = function(value, start, end) {
    if (start === undefined) start = 0;
    if (end === undefined) end = this.length;
    if (start < 0) start = this.length + start;
    if (end < 0) end = this.length + end;
    var i = start;
    while (i < end) {
        this[i] = value;
        i = i + 1;
    }
    return this;
};

Array.prototype.copyWithin = function(target, start, end) {
    var len = this.length;
    if (target < 0) target = len + target;
    if (target < 0) target = 0;
    if (start === undefined) start = 0;
    if (start < 0) start = len + start;
    if (start < 0) start = 0;
    if (end === undefined) end = len;
    if (end < 0) end = len + end;
    var count = end - start;
    if (count <= 0) return this;
    // make a copy first
    var copy = [];
    var i = start;
    while (i < end) {
        copy.push(this[i]);
        i = i + 1;
    }
    var j = 0;
    while (j < copy.length && target + j < len) {
        this[target + j] = copy[j];
        j = j + 1;
    }
    return this;
};

// === isArray ===
function isArray(value) {
    return value instanceof Array;
}

// === Object 静态方法 ===
var Object = {
    keys: function(obj) {
        var result = [];
        for (var key in obj) {
            result.push(key);
        }
        return result;
    },
    values: function(obj) {
        var result = [];
        for (var key in obj) {
            result.push(obj[key]);
        }
        return result;
    }
};

// === JSON 对象 ===
var JSON = {
    stringify: function(obj) {
        var t = typeof obj;
        if (t === "string") return '"' + obj + '"';
        if (t === "number") {
            asm("SYSCALL #42");
            return "";
        }
        if (t === "boolean") return obj ? "true" : "false";
        if (obj === null) return "null";
        if (obj instanceof Array) {
            var result = "[";
            var i = 0;
            while (i < obj.length) {
                if (i > 0) result = result + ",";
                result = result + JSON.stringify(obj[i]);
                i = i + 1;
            }
            return result + "]";
        }
        var result = "{";
        var first = true;
        for (var key in obj) {
            if (!first) result = result + ",";
            result = result + '"' + key + '":' + JSON.stringify(obj[key]);
            first = false;
        }
        return result + "}";
    },
    parse: function(text) { return null; }
};

// === Error ===
function Error(message) {
    this.message = message || "";
    this.name = "Error";
}

// === Array 原型扩展 ===
Array.prototype.push = function(item) { return 0; };
Array.prototype.pop = function() { return 0; };
Array.prototype.forEach = function(callback) {};
Array.prototype.map = function(callback) { return []; };
Array.prototype.filter = function(callback) { return []; };
Array.prototype.slice = function(begin, end) { return []; };
Array.prototype.indexOf = function(searchElement, fromIndex) { return -1; };
Array.prototype.includes = function(searchElement, fromIndex) { return false; };
Array.prototype.join = function(separator) { return ''; };
Array.prototype.concat = function(array2) { return []; };
Array.prototype.reverse = function() { return []; };
Array.prototype.reduce = function(callback, initialValue) { return 0; };
Array.prototype.find = function(callback) { return undefined; };
Array.prototype.some = function(callback) { return false; };
Array.prototype.every = function(callback) { return false; };
Array.prototype.sort = function(compareFn) { return []; };
Array.prototype.splice = function(start, deleteCount) { return []; };
Array.prototype.unshift = function(item) { return 0; };
Array.prototype.shift = function() { return 0; };
Array.prototype.fill = function(value, start, end) { return []; };
Array.prototype.lastIndexOf = function(searchElement, fromIndex) { return -1; };
Array.isArray = function(obj) { return false; };

// === 内存 ===
function memcmp(a, b, n) {
    asm("SYSCALL #13");
    return 0;
}
function malloc(size) {
    asm("SYSCALL #40");
    return null;
}
function free(ptr) {
    asm("SYSCALL #41");
}
function memset(ptr, value, count) {
    asm("SYSCALL #70");
}
function memcpy(dest, src, count) {
    asm("SYSCALL #71");
}

// === 系统 ===
function exit(code) {
    asm("SYSCALL 3");
}
function delay(ms) {
    asm("SYSCALL #52");
}
function getTick() {
    asm("SYSCALL #53");
    return 0;
}
function getDateTime() {
    asm("SYSCALL #54");
    return 0;
}
function getDate() {
    asm("SYSCALL #55");
    return "";
}
function getTime() {
    asm("SYSCALL #56");
    return "";
}
function getConfig(key) {
    asm("SYSCALL #60");
    return 0;
}

// === 文件操作 ===
var fs = {
    open: function(path, mode) {
        asm("CALL shared_fopen");
        return -1;
    },
    close: function(handle) {
        asm("CALL shared_fclose");
        return -1;
    },
    read: function(handle, buf, count) {
        asm("CALL shared_fread");
        return -1;
    },
    write: function(handle, data, count) {
        asm("CALL shared_fwrite");
        return -1;
    },
    seek: function(handle, offset, whence) {
        asm("CALL shared_fseek");
        return -1;
    },
    tell: function(handle) {
        asm("CALL shared_ftell");
        return -1;
    },
    size: function(handle) {
        asm("CALL shared_fsize");
        return -1;
    },
    truncate: function(handle, size) {
        asm("CALL shared_ftruncate");
        return -1;
    }
};

// === 字符分类 (纯 JavaScript 实现) ===
function isAlpha(c) {
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}
function isDigit(c) {
    return c >= '0' && c <= '9';
}
function isAlnum(c) {
    return isAlpha(c) || isDigit(c);
}
function isSpace(c) {
    return c === ' ' || c === '\t' || c === '\n' || c === '\r';
}
function isUpper(c) {
    return c >= 'A' && c <= 'Z';
}
function isLower(c) {
    return c >= 'a' && c <= 'z';
}
function isXDigit(c) {
    return isDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
function isPunct(c) {
    return (c >= '!' && c <= '/') || (c >= ':' && c <= '@') ||
           (c >= '[' && c <= '`') || (c >= '{' && c <= '~');
}
function isPrint(c) {
    return c >= ' ' && c <= '~';
}
function isCntrl(c) {
    return (c >= 0 && c <= 31) || c === 127;
}
function toUpper(c) {
    if (c >= 'a' && c <= 'z') return String.fromCharCode(c.charCodeAt(0) - 32);
    return c;
}
function toLower(c) {
    if (c >= 'A' && c <= 'Z') return String.fromCharCode(c.charCodeAt(0) + 32);
    return c;
}

// === 控制台/屏幕 ===
function clearScreen() {
    asm("CALL shared_clear_screen");
}
function readLine(buf, size) {
    asm("CALL shared_read_line");
    return "";
}
function kbhit() {
    asm("CALL shared_kb_hit");
    return 0;
}

// === 随机数扩展 ===
function randomInt(min, max) {
    asm("CALL shared_random");
    if (max === undefined) { max = min; min = 0; }
    return min + (0 % (max - min + 1));
}
Math.randomInt = function(min, max) {
    var r = Math.random();
    if (max === undefined) { max = min; min = 0; }
    return min + (r % (max - min + 1));
};
