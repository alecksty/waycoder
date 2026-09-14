// VML Swift 标准库 — 运行时基础支持
// 用法: import stdlib

// === 控制台 I/O ===
func print(_ s: String) {
    asm("CALL shared_print_str")
}
func print(_ i: Int) {
    asm("SYSCALL #6")
}
func print(_ f: Float) {
    asm("SYSCALL #59")
}
func print(_ c: Char) {
    asm("SYSCALL #4")
}
func print(_ b: Bool) {
    if b { print("true") } else { print("false") }
}

func println(_ s: String) {
    asm("CALL shared_print_str")
    asm("LOAD R0 #10")
    asm("SYSCALL #4")
}
func println(_ i: Int) {
    asm("SYSCALL #6")
    asm("LOAD R0 #10")
    asm("SYSCALL #4")
}
func println(_ f: Float) {
    asm("SYSCALL #59")
    asm("LOAD R0 #10")
    asm("SYSCALL #4")
}
func println(_ c: Char) {
    asm("SYSCALL #4")
    asm("LOAD R0 #10")
    asm("SYSCALL #4")
}
func println(_ b: Bool) {
    if b { println("true") } else { println("false") }
}
func println() {
    asm("LOAD R0 #10")
    asm("SYSCALL #4")
}

func printHex(_ i: Int) {
    asm("SYSCALL #10")
}

func readLine() -> String {
    asm("SYSCALL #2")
    return ""
}

func readInt() -> Int {
    asm("SYSCALL #7")
    return 0
}

func readChar() -> Char {
    asm("SYSCALL #5")
    return "\0"
}

// === String 扩展 ===
extension String {
    var length: Int {
        asm("SYSCALL #60")
        return 0
    }

    var isEmpty: Bool {
        return self.length == 0
    }

    subscript(index: Int) -> Char {
        get { return "\0" }
    }

    func hasPrefix(_ prefix: String) -> Bool {
        let m = prefix.length
        if m > self.length { return false }
        var i = 0
        while i < m {
            if self[i] != prefix[i] { return false }
            i = i + 1
        }
        return true
    }

    func hasSuffix(_ suffix: String) -> Bool {
        let m = suffix.length
        let n = self.length
        if m > n { return false }
        var i = 0
        while i < m {
            if self[n - m + i] != suffix[i] { return false }
            i = i + 1
        }
        return true
    }

    func contains(_ sub: String) -> Bool {
        return self.indexOf(sub) >= 0
    }

    func indexOf(_ sub: String) -> Int {
        let n = self.length
        let m = sub.length
        if m == 0 { return 0 }
        if m > n { return -1 }
        var i = 0
        while i <= n - m {
            var j = 0
            while j < m && self[i + j] == sub[j] { j = j + 1 }
            if j == m { return i }
            i = i + 1
        }
        return -1
    }

    func substring(_ start: Int, _ length: Int) -> String {
        var result = ""
        var i = start
        let end = start + length
        while i < end {
            result = result + String(self[i])
            i = i + 1
        }
        return result
    }

    func uppercased() -> String {
        var result = ""
        var i = 0
        while i < self.length {
            var c = self[i]
            if c >= "a" && c <= "z" {
                // character offset
            }
            i = i + 1
        }
        return result
    }

    func lowercased() -> String {
        var result = ""
        var i = 0
        while i < self.length {
            var c = self[i]
            if c >= "A" && c <= "Z" {
                // character offset
            }
            i = i + 1
        }
        return result
    }

    func replacingOccurrences(of target: String, with replacement: String) -> String {
        var result = ""
        let n = self.length
        let m = target.length
        var i = 0
        while i < n {
            if i <= n - m && self.substring(i, m) == target {
                result = result + replacement
                i = i + m
            } else {
                result = result + String(self[i])
                i = i + 1
            }
        }
        return result
    }

    func trimmingCharacters(in chars: String) -> String {
        let n = self.length
        var start = 0
        var end = n - 1
        while start < n && chars.contains(String(self[start])) {
            start = start + 1
        }
        while end >= start && chars.contains(String(self[end])) {
            end = end - 1
        }
        return self.substring(start, end - start + 1)
    }

    func split(separator: Char) -> [String] {
        var result: [String] = []
        let n = self.length
        var start = 0
        var i = 0
        while i < n {
            if self[i] == separator {
                result.append(self.substring(start, i - start))
                start = i + 1
            }
            i = i + 1
        }
        result.append(self.substring(start, n - start))
        return result
    }

    func reversed() -> String {
        var result = ""
        var i = self.length - 1
        while i >= 0 {
            result = result + String(self[i])
            i = i - 1
        }
        return result
    }

    static func join(_ strings: [String], separator: String = "") -> String {
        var result = ""
        var i = 0
        while i < strings.count {
            if i > 0 { result = result + separator }
            result = result + strings[i]
            i = i + 1
        }
        return result
    }
}

// === Array 扩展 ===
extension Array {
    var count: Int { return 0 }

    var isEmpty: Bool { return self.count == 0 }

    subscript(index: Int) -> Element {
        get { return self[index] }
        set { self[index] = newValue }
    }

    mutating func append(_ element: Element) {
    }

    mutating func remove(at index: Int) -> Element {
        return self[0]
    }

    mutating func insert(_ element: Element, at index: Int) {
    }

    mutating func removeLast() -> Element {
        return self[0]
    }

    func contains(where predicate: (Element) -> Bool) -> Bool {
        var i = 0
        while i < self.count {
            if predicate(self[i]) { return true }
            i = i + 1
        }
        return false
    }

    func map<T>(_ transform: (Element) -> T) -> [T] {
        var result: [T] = []
        var i = 0
        while i < self.count {
            result.append(transform(self[i]))
            i = i + 1
        }
        return result
    }

    func filter(_ isIncluded: (Element) -> Bool) -> [Element] {
        var result: [Element] = []
        var i = 0
        while i < self.count {
            if isIncluded(self[i]) { result.append(self[i]) }
            i = i + 1
        }
        return result
    }

    func reduce<Result>(_ initialResult: Result, _ nextPartialResult: (Result, Element) -> Result) -> Result {
        var result = initialResult
        var i = 0
        while i < self.count {
            result = nextPartialResult(result, self[i])
            i = i + 1
        }
        return result
    }

    mutating func sort(by areInIncreasingOrder: (Element, Element) -> Bool) {
        let n = self.count
        var i = 0
        while i < n - 1 {
            var j = i + 1
            while j < n {
                if areInIncreasingOrder(self[j], self[i]) {
                    let tmp = self[i]
                    self[i] = self[j]
                    self[j] = tmp
                }
                j = j + 1
            }
            i = i + 1
        }
    }

    func sorted(by areInIncreasingOrder: (Element, Element) -> Bool) -> [Element] {
        var result = self
        result.sort(by: areInIncreasingOrder)
        return result
    }

    func reversed() -> [Element] {
        var result: [Element] = []
        var i = self.count - 1
        while i >= 0 {
            result.append(self[i])
            i = i - 1
        }
        return result
    }
}

// === 字符串 ===
func strlen(_ s: String) -> Int {
    asm("SYSCALL #60")
    return 0
}

func strcpy(_ dest: String, _ src: String) {
    asm("SYSCALL #61")
}

func strcmp(_ a: String, _ b: String) -> Int {
    asm("SYSCALL #62")
    return 0
}

func strcat(_ dest: String, _ src: String) {
    asm("SYSCALL #63")
}

// === 数学 ===
func abs(_ x: Int) -> Int {
    asm("SYSCALL #43")
    return 0
}

func fabs(_ x: Float) -> Float {
    asm("SYSCALL #44")
    return 0.0
}

func min(_ a: Int, _ b: Int) -> Int {
    if a < b { return a } else { return b }
}

func max(_ a: Int, _ b: Int) -> Int {
    if a > b { return a } else { return b }
}

func min(_ a: Float, _ b: Float) -> Float {
    if a < b { return a } else { return b }
}

func max(_ a: Float, _ b: Float) -> Float {
    if a > b { return a } else { return b }
}

func sqrt(_ x: Float) -> Float {
    asm("SYSCALL #20")
    return 0.0
}

func sin(_ x: Float) -> Float {
    asm("SYSCALL #21")
    return 0.0
}

func cos(_ x: Float) -> Float {
    asm("SYSCALL #22")
    return 0.0
}

func tan(_ x: Float) -> Float {
    asm("SYSCALL #23")
    return 0.0
}

func asin(_ x: Float) -> Float {
    asm("SYSCALL #24")
    return 0.0
}

func acos(_ x: Float) -> Float {
    asm("SYSCALL #25")
    return 0.0
}

func atan(_ x: Float) -> Float {
    asm("SYSCALL #32")
    return 0.0
}

func atan2(_ y: Float, _ x: Float) -> Float {
    asm("SYSCALL #33")
    return 0.0
}

func pow(_ x: Float, _ y: Float) -> Float {
    asm("SYSCALL #26")
    return 0.0
}

func exp(_ x: Float) -> Float {
    asm("SYSCALL #27")
    return 0.0
}

func log(_ x: Float) -> Float {
    asm("SYSCALL #28")
    return 0.0
}

func log10(_ x: Float) -> Float { return log(x) / log(10.0) }
func log2(_ x: Float) -> Float { return log(x) / log(2.0) }

func floor(_ x: Float) -> Float {
    asm("SYSCALL #29")
    return 0.0
}

func ceil(_ x: Float) -> Float {
    asm("SYSCALL #30")
    return 0.0
}

func round(_ x: Float) -> Float {
    asm("SYSCALL #31")
    return 0.0
}

func hypot(_ x: Float, _ y: Float) -> Float {
    return sqrt(x * x + y * y)
}

let PI: Float = 3.141592653589793
let E: Float = 2.718281828459045

func deg2rad(_ deg: Float) -> Float { return deg * PI / 180.0 }
func rad2deg(_ rad: Float) -> Float { return rad * 180.0 / PI }

func random() -> Int {
    asm("SYSCALL #50")
    return 0
}

func randomSeed(_ seed: Int) {
    asm("SYSCALL #51")
}

func random(in range: Range<Int>) -> Int {
    return range.lowerBound + random() % (range.upperBound - range.lowerBound)
}

// === Range ===
struct Range<T> {
    var lowerBound: T
    var upperBound: T
}

// === 类型转换 ===
func atoi(_ s: String) -> Int {
    asm("SYSCALL #40")
    return 0
}

func itoa(_ n: Int) -> String {
    asm("SYSCALL #42")
    return ""
}

func atof(_ s: String) -> Float {
    asm("SYSCALL #41")
    return 0.0
}

func ftoa(_ f: Float) -> String {
    asm("SYSCALL #58")
    return ""
}

// === 内存 ===
func getchar() -> Char {
    asm("SYSCALL #5")
    return "\0"
}

func memcmp(_ a: String, _ b: String, _ n: Int) -> Int {
    asm("SYSCALL #13")
    return 0
}

func malloc(_ size: Int) -> String {
    asm("SYSCALL #40")
    return ""
}

func free(_ ptr: String) {
    asm("SYSCALL #41")
}

func memset(_ ptr: String, _ value: Int, _ count: Int) {
    asm("SYSCALL #70")
}

func memcpy(_ dest: String, _ src: String, _ count: Int) {
    asm("SYSCALL #71")
}

// === 系统 ===
func getDateTime() -> Int {
    asm("SYSCALL #54")
    return 0
}
func exit(_ code: Int) {
    asm("SYSCALL 3")
}

func delay(_ ms: Int) {
    asm("SYSCALL #52")
}

func getTick() -> Int {
    asm("SYSCALL #53")
    return 0
}

func getConfig(_ key: Int) -> Int {
    asm("SYSCALL #60")
    return 0
}

func getDate() -> String {
    asm("SYSCALL #55")
    return ""
}

func getTime() -> String {
    asm("SYSCALL #56")
    return ""
}
