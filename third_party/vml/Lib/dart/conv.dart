// VML 全类型转换库 — Dart 包装器 (v1.66.44)
// 用法: import 'conv.dart';

String intToStr(int val)     => _convCall('int_to_str', val);
int strToInt(String s)       => _convCall('str_to_int', s);
String longToStr(int val)    => _convCall('long_to_str', val);
int strToLong(String s)      => _convCall('str_to_long', s);
String floatToStr(double f)  => _convCall('float_to_str', f);
double strToFloat(String s)  => _convCall('str_to_float', s);
String doubleToStr(double d) => _convCall('double_to_str', d);
double strToDouble(String s) => _convCall('str_to_double', s);
String boolToStr(bool b)     => _convCall('bool_to_str', b ? 1 : 0);
bool strToBool(String s)     => _convCall('str_to_bool', s) != 0;
String charToStr(String c)   => _convCall('char_to_str', c.codeUnitAt(0));
String strToChar(String s)   => String.fromCharCode(_convCall('str_to_char', s));
