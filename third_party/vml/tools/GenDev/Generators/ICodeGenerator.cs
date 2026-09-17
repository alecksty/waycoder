using GenDev.Models;

namespace GenDev.Generators
{
    public interface ICodeGenerator
    {
        string Language { get; }
        string FileExtension { get; }
        string Generate(DeviceModel device);
    }
}