import std.stdio;
void main()
{
    char[64] b;
    printf("D-INT=%d\n", 42);
    sprintf(b.ptr, "D-S=%s", "abc");
    puts(b.ptr);
}
