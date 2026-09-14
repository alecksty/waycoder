/* time.h - Time functions
 * ISO C Standard 7.12
 */

#ifndef _TIME_H
#define _TIME_H

#include <stddef.h>

/* Types */
typedef unsigned int clock_t;
typedef unsigned int time_t;

/* Number of clock ticks per second (milliseconds on VML) */
#define CLOCKS_PER_SEC 1000

/* Time structure */
struct tm {
    int tm_sec;     /* seconds [0, 61] */
    int tm_min;     /* minutes [0, 59] */
    int tm_hour;    /* hours [0, 23] */
    int tm_mday;    /* day of month [1, 31] */
    int tm_mon;     /* month [0, 11] */
    int tm_year;    /* years since 1900 */
    int tm_wday;    /* day of week [0, 6] (Sunday=0) */
    int tm_yday;    /* day of year [0, 365] */
    int tm_isdst;   /* daylight saving flag */
};

/* Typedef for use in function parameters (VML C doesn't support struct in params) */
typedef struct tm tm_t;

/* Time manipulation functions */
clock_t clock(void);
double difftime(time_t time1, time_t time0);
time_t time(time_t *timer);

/* Time conversion functions */
time_t mktime(tm_t *timeptr);
tm_t *gmtime(const time_t *timer);
tm_t *localtime(const time_t *timer);
char *asctime(const tm_t *timeptr);
char *ctime(const time_t *timer);
/* size_t strftime(char *s, size_t maxsize, const char *format, const tm_t *timeptr); */

#endif /* _TIME_H */
