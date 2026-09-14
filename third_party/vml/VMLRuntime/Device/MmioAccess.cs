namespace VMLRuntime.Device
{
    [Flags]
    public enum MmioAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }
}
