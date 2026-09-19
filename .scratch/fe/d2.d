import std.stdio;
char[64] g_b;
void main()
{
    sprintf(g_b.ptr, "D-S=%s", "abc");
    printf("D-BUF=[%s]\n", g_b.ptr);
    puts(g_b.ptr);
}
