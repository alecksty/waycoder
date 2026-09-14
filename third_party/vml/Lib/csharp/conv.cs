// VML 全类型转换库 — C# 包装器 (v1.66.44)
namespace Vml.Conv;

public static class Conv {
    public static extern string IntToStr(int val);
    public static extern int StrToInt(string s);
    public static extern string LongToStr(long val);
    public static extern long StrToLong(string s);
    public static extern string FloatToStr(float f);
    public static extern float StrToFloat(string s);
    public static extern string DoubleToStr(double d);
    public static extern double StrToDouble(string s);
    public static extern string BoolToStr(bool b);
    public static extern bool StrToBool(string s);
    public static extern string CharToStr(char c);
    public static extern char StrToChar(string s);
}
