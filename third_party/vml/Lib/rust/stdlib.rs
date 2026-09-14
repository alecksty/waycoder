// VML Rust 标准库 — 运行时基础支持
// 用法: use stdlib

// 核心 I/O
pub mod core {
    pub mod prelude {
        pub use super::*;
    }

    pub fn print(s: &str) {
        unsafe { asm!("CALL shared_print_str") }
    }
    pub fn println(s: &str) {
        unsafe {
            asm!("CALL shared_print_str");
            asm!("LOAD R0 #10");
            asm!("SYSCALL #4");
        }
    }
    pub fn print_int(i: i32) {
        unsafe { asm!("SYSCALL #6") }
    }
    pub fn println_int(i: i32) {
        unsafe {
            asm!("SYSCALL #6");
            asm!("LOAD R0 #10");
            asm!("SYSCALL #4");
        }
    }
    pub fn print_float(f: f32) {
        unsafe { asm!("SYSCALL #59") }
    }
    pub fn format(args: core::fmt::Arguments) -> String {
        String::new()
    }
}

// 格式化
pub mod fmt {
    pub struct Formatter<'a> {}
    pub trait Display {
        fn fmt(&self, f: &mut Formatter) -> Result;
    }
    pub trait Debug {
        fn fmt(&self, f: &mut Formatter) -> Result;
    }
    pub type Result = core::result::Result<(), Error>;
    pub struct Error;
}

// 字符串类型
pub struct String {
    data: *mut u8,
    len: usize,
    cap: usize,
}

impl String {
    pub fn new() -> Self {
        String { data: core::ptr::null_mut(), len: 0, cap: 0 }
    }
    pub fn from(s: &str) -> Self {
        let len = string::len(s);
        let data = memory::alloc(len + 1);
        string::copy_raw(data, s, len);
        String { data: data, len: len, cap: len + 1 }
    }
    pub fn as_str(&self) -> &str { "" }
    pub fn len(&self) -> usize { self.len }
    pub fn is_empty(&self) -> bool { self.len == 0 }
    pub fn push_str(&mut self, s: &str) {
        let slen = string::len(s);
        if self.len + slen >= self.cap {
            let new_cap = if self.cap == 0 { 16 } else { self.cap * 2 };
            let new_data = memory::alloc(new_cap);
            if self.data != core::ptr::null_mut() {
                memory::memcpy(new_data, self.data, self.len);
                memory::free(self.data);
            }
            self.data = new_data;
            self.cap = new_cap;
        }
        unsafe {
            let dst = (self.data as usize + self.len) as *mut u8;
            string::copy_raw(dst, s, slen);
        }
        self.len = self.len + slen;
    }
}

// 字符串切片操作
pub mod str {
    pub fn from_utf8(v: &[u8]) -> Result<&str, Utf8Error> { Ok("") }
    pub struct Utf8Error;
}

// 向量
pub mod vec {
    pub struct Vec<T> {
        data: *mut T,
        len: usize,
        cap: usize,
    }
    impl<T> Vec<T> {
        pub fn new() -> Self {
            Vec { data: core::ptr::null_mut(), len: 0, cap: 0 }
        }
        pub fn push(&mut self, value: T) {
            if self.len >= self.cap {
                let new_cap = if self.cap == 0 { 4 } else { self.cap * 2 };
                let new_data = memory::alloc(new_cap);
                if self.data != core::ptr::null_mut() {
                    memory::memcpy(new_data, self.data, self.len);
                    memory::free(self.data);
                }
                self.data = new_data;
                self.cap = new_cap;
            }
            self.data[self.len] = value;
            self.len = self.len + 1;
        }
        pub fn pop(&mut self) -> Option<T> {
            if self.len == 0 { return None; }
            self.len = self.len - 1;
            Some(self.data[self.len])
        }
        pub fn len(&self) -> usize { self.len }
        pub fn is_empty(&self) -> bool { self.len == 0 }
        pub fn get(&self, index: usize) -> Option<&T> {
            if index < self.len {
                Some(&self.data[index])
            } else {
                None
            }
        }
        pub fn get_mut(&mut self, index: usize) -> Option<&mut T> {
            if index < self.len {
                Some(&mut self.data[index])
            } else {
                None
            }
        }
        pub fn clear(&mut self) { self.len = 0; }
        pub fn as_slice(&self) -> &[T] { &[] }
        pub fn remove(&mut self, index: usize) -> T {
            let val = self.data[index];
            let mut i = index;
            while i < self.len - 1 {
                self.data[i] = self.data[i + 1];
                i = i + 1;
            }
            self.len = self.len - 1;
            val
        }
        pub fn insert(&mut self, index: usize, value: T) {
            if self.len >= self.cap {
                let new_cap = if self.cap == 0 { 4 } else { self.cap * 2 };
                let new_data = memory::alloc(new_cap);
                if self.data != core::ptr::null_mut() {
                    memory::memcpy(new_data, self.data, self.len);
                    memory::free(self.data);
                }
                self.data = new_data;
                self.cap = new_cap;
            }
            let mut i = self.len;
            while i > index {
                self.data[i] = self.data[i - 1];
                i = i - 1;
            }
            self.data[index] = value;
            self.len = self.len + 1;
        }
    }

    // vec! macro helper
    pub fn from_slice<T>(slice: &[T]) -> Vec<T> {
        let mut v = Vec::new();
        let mut i = 0;
        while i < slice.len() {
            v.push(slice[i]);
            i = i + 1;
        }
        v
    }
}

// 迭代器 trait
pub mod iter {
    pub trait Iterator {
        type Item;
        fn next(&mut self) -> Option<Self::Item>;
        fn count(self) -> usize {
            let mut c = 0;
            while self.next().is_some() { c = c + 1; }
            c
        }
        fn collect<B: FromIterator<Self::Item>>(self) -> B {
            B::from_iter(self)
        }
        fn map<B, F: Fn(Self::Item) -> B>(self, f: F) -> Map<Self, F> {
            Map { iter: self, f: f }
        }
        fn filter<P: Fn(&Self::Item) -> bool>(self, predicate: P) -> Filter<Self, P> {
            Filter { iter: self, predicate: predicate }
        }
        fn fold<B, F: Fn(B, Self::Item) -> B>(self, init: B, f: F) -> B {
            let mut accum = init;
            let mut iter = self;
            loop {
                match iter.next() {
                    Some(val) => accum = f(accum, val),
                    None => break,
                }
            }
            accum
        }
        fn for_each<F: Fn(Self::Item)>(self, f: F) {
            let mut iter = self;
            loop {
                match iter.next() {
                    Some(val) => f(val),
                    None => break,
                }
            }
        }
    }
    pub trait IntoIterator {
        type Item;
        type IntoIter: Iterator<Item = Self::Item>;
        fn into_iter(self) -> Self::IntoIter;
    }
    pub trait FromIterator<A> {
        fn from_iter<T: IntoIterator<Item = A>>(iter: T) -> Self;
    }
    pub struct Map<I, F> { iter: I, f: F }
    pub struct Filter<I, P> { iter: I, predicate: P }
}

// 选项
pub mod option {
    pub enum Option<T> { None, Some(T) }
    impl<T> Option<T> {
        pub fn is_some(&self) -> bool {
            match self { Option::Some(_) => true, Option::None => false }
        }
        pub fn is_none(&self) -> bool { !self.is_some() }
        pub fn unwrap(self) -> T {
            match self { Option::Some(val) => val, Option::None => panic!("unwrap on None") }
        }
        pub fn unwrap_or(self, default: T) -> T {
            match self { Option::Some(val) => val, Option::None => default }
        }
        pub fn unwrap_or_else<F: Fn() -> T>(self, f: F) -> T {
            match self { Option::Some(val) => val, Option::None => f() }
        }
        pub fn map<U, F: Fn(T) -> U>(self, f: F) -> Option<U> {
            match self { Option::Some(val) => Option::Some(f(val)), Option::None => Option::None }
        }
        pub fn and_then<U, F: Fn(T) -> Option<U>>(self, f: F) -> Option<U> {
            match self { Option::Some(val) => f(val), Option::None => Option::None }
        }
        pub fn or(self, optb: Option<T>) -> Option<T> {
            match self { Option::Some(_) => self, Option::None => optb }
        }
        pub fn expect(self, msg: &str) -> T {
            match self { Option::Some(val) => val, Option::None => panic!(msg) }
        }
    }
}

// 结果
pub mod result {
    pub enum Result<T, E> { Ok(T), Err(E) }
    impl<T, E> Result<T, E> {
        pub fn is_ok(&self) -> bool {
            match self { Result::Ok(_) => true, Result::Err(_) => false }
        }
        pub fn is_err(&self) -> bool { !self.is_ok() }
        pub fn unwrap(self) -> T {
            match self { Result::Ok(val) => val, Result::Err(_) => panic!("unwrap on Err") }
        }
        pub fn unwrap_err(self) -> E {
            match self { Result::Ok(_) => panic!("unwrap_err on Ok"), Result::Err(e) => e }
        }
        pub fn unwrap_or(self, default: T) -> T {
            match self { Result::Ok(val) => val, Result::Err(_) => default }
        }
        pub fn map<U, F: Fn(T) -> U>(self, f: F) -> Result<U, E> {
            match self { Result::Ok(val) => Result::Ok(f(val)), Result::Err(e) => Result::Err(e) }
        }
        pub fn expect(self, msg: &str) -> T {
            match self { Result::Ok(val) => val, Result::Err(_) => panic!(msg) }
        }
    }
}

// 数学
pub mod math {
    pub fn abs(x: i32) -> i32 {
        unsafe { asm!("SYSCALL #43") }
        0
    }
    pub fn fabs(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #44") }
        0.0
    }
    pub fn min(a: i32, b: i32) -> i32 { if a < b { a } else { b } }
    pub fn max(a: i32, b: i32) -> i32 { if a > b { a } else { b } }
    pub fn fmin(a: f32, b: f32) -> f32 { if a < b { a } else { b } }
    pub fn fmax(a: f32, b: f32) -> f32 { if a > b { a } else { b } }
    pub fn sqrt(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #20") }
        0.0
    }
    pub fn pow(x: f32, y: f32) -> f32 {
        unsafe { asm!("SYSCALL #26") }
        0.0
    }
    pub fn sin(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #21") }
        0.0
    }
    pub fn cos(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #22") }
        0.0
    }
    pub fn tan(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #23") }
        0.0
    }
    pub fn asin(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #24") }
        0.0
    }
    pub fn acos(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #25") }
        0.0
    }
    pub fn atan(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #32") }
        0.0
    }
    pub fn atan2(y: f32, x: f32) -> f32 {
        unsafe { asm!("SYSCALL #33") }
        0.0
    }
    pub fn exp(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #27") }
        0.0
    }
    pub fn log(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #28") }
        0.0
    }
    pub fn log10(x: f32) -> f32 { log(x) / log(10.0) }
    pub fn log2(x: f32) -> f32 { log(x) / log(2.0) }
    pub fn ceil(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #30") }
        0.0
    }
    pub fn floor(x: f32) -> f32 {
        unsafe { asm!("SYSCALL #29") }
        0.0
    }
    pub fn round(x: f32) -> i32 {
        unsafe { asm!("SYSCALL #31") }
        0
    }
    pub fn hypot(x: f32, y: f32) -> f32 { sqrt(x * x + y * y) }
    pub fn signum(x: i32) -> i32 {
        if x > 0 { 1 } else if x < 0 { -1 } else { 0 }
    }

    pub const PI: f32 = 3.141592653589793;
    pub const E: f32 = 2.718281828459045;
}

// 字符串操作
pub mod string {
    use super::String;

    pub fn len(s: &str) -> usize {
        unsafe { asm!("SYSCALL #60") }
        0
    }
    pub fn copy_raw(dest: *mut u8, src: &str, n: usize) {
        unsafe {
            let mut i = 0;
            while i < n {
                let c = src[i];
                dest[i] = c;
                i = i + 1;
            }
            dest[n] = 0;
        }
    }
    pub fn cmp(a: &str, b: &str) -> i32 {
        unsafe { asm!("SYSCALL #62") }
        0
    }
    pub fn concat(dest: &mut str, src: &str) {
        unsafe { asm!("SYSCALL #63") }
    }
    pub fn find(s: &str, sub: &str) -> Option<usize> {
        let n = len(s);
        let m = len(sub);
        if m == 0 { return Some(0); }
        let mut i = 0;
        while i <= n - m {
            let mut j = 0;
            while j < m && s[i + j] == sub[j] { j = j + 1; }
            if j == m { return Some(i); }
            i = i + 1;
        }
        None
    }
    pub fn contains(s: &str, sub: &str) -> bool {
        find(s, sub).is_some()
    }
    pub fn starts_with(s: &str, prefix: &str) -> bool {
        let m = len(prefix);
        if m > len(s) { return false; }
        let mut i = 0;
        while i < m { if s[i] != prefix[i] { return false; } i = i + 1; }
        true
    }
    pub fn ends_with(s: &str, suffix: &str) -> bool {
        let m = len(suffix);
        let n = len(s);
        if m > n { return false; }
        let mut i = 0;
        while i < m { if s[n - m + i] != suffix[i] { return false; } i = i + 1; }
        true
    }
    pub fn replace(s: &str, from: &str, to: &str) -> String {
        let n = len(s);
        let m = len(from);
        let mut result = String::new();
        let mut i = 0;
        while i < n {
            if i <= n - m {
                let mut match_found = true;
                let mut j = 0;
                while j < m {
                    if s[i + j] != from[j] { match_found = false; break; }
                    j = j + 1;
                }
                if match_found {
                    result.push_str(to);
                    i = i + m;
                } else {
                    // push single char
                    i = i + 1;
                }
            } else {
                i = i + 1;
            }
        }
        result
    }
    pub fn to_upper(s: &str) -> String {
        let mut result = String::new();
        let mut i = 0;
        let n = len(s);
        while i < n {
            let mut c = s[i];
            if c >= 'a' && c <= 'z' { c = (c as u8 - 32) as char; }
            i = i + 1;
        }
        result
    }
    pub fn to_lower(s: &str) -> String {
        let mut result = String::new();
        let mut i = 0;
        let n = len(s);
        while i < n {
            let mut c = s[i];
            if c >= 'A' && c <= 'Z' { c = (c as u8 + 32) as char; }
            i = i + 1;
        }
        result
    }
    pub fn trim(s: &str) -> String {
        let n = len(s);
        let mut start = 0;
        let mut end = n;
        while start < n && (s[start] == ' ' || s[start] == '\t' || s[start] == '\n') {
            start = start + 1;
        }
        while end > start && (s[end - 1] == ' ' || s[end - 1] == '\t' || s[end - 1] == '\n') {
            end = end - 1;
        }
        let mut result = String::new();
        let mut i = start;
        while i < end {
            i = i + 1;
        }
        result
    }
    pub fn repeat(s: &str, n: usize) -> String {
        let mut result = String::new();
        let mut i = 0;
        while i < n {
            result.push_str(s);
            i = i + 1;
        }
        result
    }
}

// 类型转换
pub mod convert {
    use super::String;

    pub fn atoi(s: &str) -> i32 {
        unsafe { asm!("SYSCALL #40") }
        0
    }
    pub fn itoa(n: i32) -> String {
        unsafe { asm!("SYSCALL #42") }
        String::new()
    }
    pub fn atof(s: &str) -> f32 {
        unsafe { asm!("SYSCALL #41") }
        0.0
    }
    pub fn ftoa(f: f32) -> String {
        unsafe { asm!("SYSCALL #58") }
        String::new()
    }
}

// 系统
pub mod system {
    use super::String;

    pub fn exit(code: i32) { unsafe { asm!("SYSCALL 3") } }
    pub fn delay(ms: i32) { unsafe { asm!("SYSCALL #52") } }
    pub fn get_tick() -> i32 {
        unsafe { asm!("SYSCALL #53") }
        0
    }
    pub fn get_config(key: i32) -> i32 {
        unsafe { asm!("SYSCALL #60") }
        0
    }
    pub fn get_date() -> String {
        unsafe { asm!("SYSCALL #55") }
        String::new()
    }
    pub fn get_time() -> String {
        unsafe { asm!("SYSCALL #56") }
        String::new()
    }
    pub fn get_datetime() -> i32 {
        unsafe { asm!("SYSCALL #54") }
        0
    }
    pub fn getchar() -> i32 {
        unsafe { asm!("SYSCALL #5") }
        0
    }
    pub fn rand() -> i32 {
        unsafe { asm!("SYSCALL #50") }
        0
    }
    pub fn srand(seed: i32) { unsafe { asm!("SYSCALL #51") } }
}

// 内存
pub mod memory {
    pub fn memset(ptr: *mut u8, value: u8, count: usize) {
        unsafe { asm!("SYSCALL #70") }
    }
    pub fn memcpy(dest: *mut u8, src: *const u8, count: usize) {
        unsafe { asm!("SYSCALL #71") }
    }
    pub fn memcmp(a: *const u8, b: *const u8, n: usize) -> i32 {
        unsafe { asm!("SYSCALL #13") }
        0
    }
    pub fn alloc(size: usize) -> *mut u8 {
        unsafe { asm!("SYSCALL #40") }
        core::ptr::null_mut()
    }
    pub fn free(ptr: *mut u8) {
        unsafe { asm!("SYSCALL #41") }
    }
}
