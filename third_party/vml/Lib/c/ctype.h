/* ctype.h - Character handling
 * ISO C Standard 7.3
 */

#ifndef _CTYPE_H
#define _CTYPE_H

#param lib("ctype")

/* Character classification functions */
int isalnum(int c);
int isalpha(int c);
int iscntrl(int c);
int isdigit(int c);
int isgraph(int c);
int islower(int c);
int isprint(int c);
int ispunct(int c);
int isspace(int c);
int isupper(int c);
int isxdigit(int c);

/* Character conversion functions */
int tolower(int c);
int toupper(int c);

/* ASCII character codes */
#define _U  0x01  /* Upper case */
#define _L  0x02  /* Lower case */
#define _N  0x04  /* Numeral (digit) */
#define _S  0x08  /* Spacing character */
#define _P  0x10  /* Punctuation */
#define _C  0x20  /* Control character */
#define _X  0x40  /* Hexadecimal digit */
#define _B  0x80  /* Blank */

#endif /* _CTYPE_H */