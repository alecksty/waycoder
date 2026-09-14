# VML 全类型转换库 — Python 包装器 (v1.66.44)
# 用法: from conv import *

def int_to_str(val):
    """整数 → 字符串"""
    return __conv_call__('int_to_str', val)

def str_to_int(s):
    """字符串 → 整数"""
    return __conv_call__('str_to_int', s)

def float_to_str(f):
    """浮点 → 字符串"""
    return __conv_call__('float_to_str', f)

def str_to_float(s):
    """字符串 → 浮点"""
    return __conv_call__('str_to_float', s)

def double_to_str(d):
    """双精度 → 字符串"""
    return __conv_call__('double_to_str', d)

def str_to_double(s):
    """字符串 → 双精度"""
    return __conv_call__('str_to_double', s)

def long_to_str(l):
    """长整数 → 字符串"""
    return __conv_call__('long_to_str', l)

def str_to_long(s):
    """字符串 → 长整数"""
    return __conv_call__('str_to_long', s)

def bool_to_str(b):
    """布尔 → 字符串 ("true"/"false")"""
    return __conv_call__('bool_to_str', b)

def str_to_bool(s):
    """字符串 → 布尔"""
    return __conv_call__('str_to_bool', s)

def char_to_str(c):
    """字符 → 字符串"""
    return __conv_call__('char_to_str', c)

def str_to_char(s):
    """字符串 → 字符 (首字符)"""
    return __conv_call__('str_to_char', s)
