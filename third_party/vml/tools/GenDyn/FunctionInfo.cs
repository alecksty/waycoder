namespace GenDyn;

public enum ParamType
{
    Int,      // int32
    Float,    // float32
    Void,
    String,   // const char* / char*
    Int64,    // long / int64_t
    Float64,  // double
    Struct,   // struct by-value
    StructPtr // struct pointer (VML addr)
}

public class ParamInfo
{
    public string Name { get; set; } = "";
    public ParamType Type { get; set; } = ParamType.Int;

    /// <summary>结构体大小 (仅用于 Struct / StructPtr 类型)</summary>
    public int StructSize { get; set; } = 0;

    /// <summary>FFI 类型码 (NativeCallEx): 0=int32, 1=float32, 2=int64, 3=float64, 4=string, 5=struct, 6=structptr</summary>
    public int FfiTypeCode => Type switch
    {
        ParamType.Int => 0,
        ParamType.Float => 1,
        ParamType.Int64 => 2,
        ParamType.Float64 => 3,
        ParamType.String => 4,
        ParamType.Struct => 5,
        ParamType.StructPtr => 6,
        _ => 0
    };

}

public class FunctionInfo
{
    public string Name { get; set; } = "";
    public ParamType ReturnType { get; set; } = ParamType.Int;
    public List<ParamInfo> Params { get; set; } = new();

    /// <summary>用于 VML 调用的原始 DLL 中的真实函数名</summary>
    public string OriginalName { get; set; } = "";

    /// <summary>C# 风格的 PascalCase 名</summary>
    public string PascalName => string.IsNullOrEmpty(Name) ? "" :
        char.ToUpper(Name[0]) + Name[1..];

    /// <summary>参数类型简写: i=Int, f=Float, v=Void, l=Int64, d=Float64, s=String, S=Struct, P=StructPtr</summary>
    public string ParamTypesShort => string.Join("",
        Params.Select(p => p.Type switch
        {
            ParamType.Float => "f",
            ParamType.Float64 => "d",
            ParamType.Int64 => "l",
            ParamType.String => "s",
            ParamType.Struct => "S",
            ParamType.StructPtr => "P",
            _ => "i"
        }));

    /// <summary>返回类型 FFI 码: 0=int32, 1=float32, 2=int64, 3=float64</summary>
    public int RetFfiTypeCode => ReturnType switch
    {
        ParamType.Float => 1,
        ParamType.Int64 => 2,
        ParamType.Float64 => 3,
        ParamType.Void => 0,
        _ => 0
    };
}
