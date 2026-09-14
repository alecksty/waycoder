// VML 全类型转换库 — JavaScript 包装器 (v1.66.44)
// 用法: import { intToStr, strToInt } from 'conv'

export function intToStr(val) { return _vml_call('int_to_str', val); }
export function strToInt(s) { return _vml_call('str_to_int', s); }
export function longToStr(val) { return _vml_call('long_to_str', val); }
export function strToLong(s) { return _vml_call('str_to_long', s); }
export function floatToStr(f) { return _vml_call('float_to_str', f); }
export function strToFloat(s) { return _vml_call('str_to_float', s); }
export function doubleToStr(d) { return _vml_call('double_to_str', d); }
export function strToDouble(s) { return _vml_call('str_to_double', s); }
export function boolToStr(b) { return _vml_call('bool_to_str', b); }
export function strToBool(s) { return _vml_call('str_to_bool', s); }
export function charToStr(c) { return _vml_call('char_to_str', c); }
export function strToChar(s) { return _vml_call('str_to_char', s); }
