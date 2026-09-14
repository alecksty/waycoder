// VML 全类型转换库 — Rust 包装器 (v1.66.44)

extern "C" {
    fn int_to_str(val: i32) -> *const u8;
    fn str_to_int(s: *const u8) -> i32;
    fn long_to_str(val: i64) -> *const u8;
    fn str_to_long(s: *const u8) -> i64;
    fn float_to_str(f: f32) -> *const u8;
    fn str_to_float(s: *const u8) -> f32;
    fn double_to_str(d: f64) -> *const u8;
    fn str_to_double(s: *const u8) -> f64;
    fn bool_to_str(b: i32) -> *const u8;
    fn str_to_bool(s: *const u8) -> i32;
    fn char_to_str(c: u8) -> *const u8;
    fn str_to_char(s: *const u8) -> u8;
}
