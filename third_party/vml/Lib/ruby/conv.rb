# VML 全类型转换库 — Ruby 包装器 (v1.66.44)
# 用法: require 'conv'

module VML
  module Conv
    def self.int_to_str(val)      = _conv_call('int_to_str', val)
    def self.str_to_int(s)        = _conv_call('str_to_int', s)
    def self.long_to_str(val)     = _conv_call('long_to_str', val)
    def self.str_to_long(s)       = _conv_call('str_to_long', s)
    def self.float_to_str(f)      = _conv_call('float_to_str', f)
    def self.str_to_float(s)      = _conv_call('str_to_float', s)
    def self.double_to_str(d)     = _conv_call('double_to_str', d)
    def self.str_to_double(s)     = _conv_call('str_to_double', s)
    def self.bool_to_str(b)       = _conv_call('bool_to_str', b ? 1 : 0)
    def self.str_to_bool(s)       = _conv_call('str_to_bool', s) != 0
    def self.char_to_str(c)       = _conv_call('char_to_str', c.ord)
    def self.str_to_char(s)       = _conv_call('str_to_char', s).chr
  end
end
