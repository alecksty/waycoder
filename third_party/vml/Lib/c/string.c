#include "string.h"

void *memcpy(void *dest, const void *src, size_t n)
{
    char *d = (char *)dest;
    const char *s = (const char *)src;
    while (n--)
    {
        *d++ = *s++;
    }
    return dest;
}

void *memmove(void *dest, const void *src, size_t n)
{
    char *d = (char *)dest;
    const char *s = (const char *)src;
    if (d < s)
    {
        while (n--)
        {
            *d++ = *s++;
        }
    }
    else
    {
        d += n;
        s += n;
        while (n--)
        {
            *--d = *--s;
        }
    }
    return dest;
}

char *strcpy(char *dest, const char *src)
{
    char *p = dest;
    while (*src)
    {
        *p++ = *src++;
    }
    *p = '\0';
    return dest;
}

char *strncpy(char *dest, const char *src, size_t n)
{
    char *p = dest;
    while (n > 0 && *src)
    {
        *p++ = *src++;
        n--;
    }
    while (n > 0)
    {
        *p++ = '\0';
        n--;
    }
    return dest;
}

char *strcat(char *dest, const char *src)
{
    char *p = dest;
    while (*p)
        p++;
    while (*src)
    {
        *p++ = *src++;
    }
    *p = '\0';
    return dest;
}

char *strncat(char *dest, const char *src, size_t n)
{
    char *p = dest;
    while (*p)
        p++;
    while (n-- > 0 && *src)
    {
        *p++ = *src++;
    }
    *p = '\0';
    return dest;
}

int memcmp(const void *s1, const void *s2, size_t n)
{
    const char *p1 = (const char *)s1;
    const char *p2 = (const char *)s2;
    while (n--)
    {
        if (*p1 != *p2)
        {
            return *p1 - *p2;
        }
        p1++;
        p2++;
    }
    return 0;
}

int strcmp(const char *s1, const char *s2)
{
    while (*s1 && *s2)
    {
        if (*s1 != *s2)
        {
            return *s1 - *s2;
        }
        s1++;
        s2++;
    }
    return *s1 - *s2;
}

int strncmp(const char *s1, const char *s2, size_t n)
{
    while (n-- && *s1 && *s2)
    {
        if (*s1 != *s2)
        {
            return *s1 - *s2;
        }
        s1++;
        s2++;
    }
    if (n == (size_t)-1)
        return 0;
    return *s1 - *s2;
}

int strcoll(const char *s1, const char *s2)
{
    return strcmp(s1, s2);
}

size_t strxfrm(char *dest, const char *src, size_t n)
{
    size_t len = 0;
    while (*src)
    {
        if (len < n)
        {
            *dest++ = *src;
        }
        src++;
        len++;
    }
    if (n > 0)
    {
        *dest = '\0';
    }
    return len;
}

void *memchr(const void *s, int c, size_t n)
{
    const char *p = (const char *)s;
    while (n--)
    {
        if (*p == (char)c)
        {
            return (void *)p;
        }
        p++;
    }
    return (void *)0;
}

char *strchr(const char *s, int c)
{
    while (*s)
    {
        if (*s == (char)c)
        {
            return (char *)s;
        }
        s++;
    }
    return (char *)0;
}

size_t strcspn(const char *s, const char *reject)
{
    size_t count = 0;
    while (*s)
    {
        const char *p = reject;
        while (*p)
        {
            if (*s == *p++)
            {
                return count;
            }
        }
        s++;
        count++;
    }
    return count;
}

char *strpbrk(const char *s, const char *accept)
{
    while (*s)
    {
        const char *p = accept;
        while (*p)
        {
            if (*s == *p++)
            {
                return (char *)s;
            }
        }
        s++;
    }
    return (char *)0;
}

char *strrchr(const char *s, int c)
{
    char *result = (char *)0;
    while (*s)
    {
        if (*s == (char)c)
        {
            result = (char *)s;
        }
        s++;
    }
    return result;
}

size_t strspn(const char *s, const char *accept)
{
    size_t count = 0;
    while (*s)
    {
        const char *p = accept;
        int found = 0;
        while (*p)
        {
            if (*s == *p++)
            {
                found = 1;
                break;
            }
        }
        if (!found)
            break;
        s++;
        count++;
    }
    return count;
}

char *strstr(const char *haystack, const char *needle)
{
    if (*needle == '\0')
        return (char *)haystack;
    
    while (*haystack)
    {
        const char *h = haystack;
        const char *n = needle;
        while (*h && *n && *h == *n)
        {
            h++;
            n++;
        }
        if (*n == '\0')
        {
            return (char *)haystack;
        }
        haystack++;
    }
    return (char *)0;
}

char *strtok(char *str, const char *delim)
{
    static char *saved;
    char *token;
    
    if (str)
        saved = str;
    else if (!saved)
        return (char *)0;
    
    str = saved;
    
    while (*str && strchr(delim, *str))
        str++;
    
    if (*str == '\0')
    {
        saved = (char *)0;
        return (char *)0;
    }
    
    token = str;
    
    while (*str && !strchr(delim, *str))
        str++;
    
    if (*str)
    {
        *str = '\0';
        saved = str + 1;
    }
    else
    {
        saved = (char *)0;
    }
    
    return token;
}

void *memset(void *s, int c, size_t n)
{
    char *p = (char *)s;
    while (n--)
    {
        *p++ = (char)c;
    }
    return s;
}

size_t strlen(const char *s)
{
    size_t len = 0;
    while (*s)
    {
        s++;
        len++;
    }
    return len;
}

char *strerror(int errnum)
{
    return "unknown error";
}