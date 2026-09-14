// VML 全类型转换库 — Go 包装器 (v1.66.44)
package conv

// 整数 → 字符串
func IntToStr(val int) string { return vmlCall("int_to_str", val) }
func StrToInt(s string) int { return vmlCall("str_to_int", s) }

// int64 → 字符串
func LongToStr(val int64) string { return vmlCall("long_to_str", val) }
func StrToLong(s string) int64 { return vmlCall("str_to_long", s) }

// 浮点 → 字符串
func FloatToStr(f float32) string { return vmlCall("float_to_str", f) }
func StrToFloat(s string) float32 { return vmlCall("str_to_float", s) }

// 双精度 → 字符串
func DoubleToStr(d float64) string { return vmlCall("double_to_str", d) }
func StrToDouble(s string) float64 { return vmlCall("str_to_double", s) }

// 布尔 → 字符串
func BoolToStr(b bool) string { return vmlCall("bool_to_str", b) }
func StrToBool(s string) bool { return vmlCall("str_to_bool", s) }

// 字符 → 字符串
func CharToStr(c byte) string { return vmlCall("char_to_str", c) }
func StrToChar(s string) byte { return vmlCall("str_to_char", s) }
