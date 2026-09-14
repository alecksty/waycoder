/* time.c - Time functions implementation for VML
 * Uses SYSCALL 53 (GetTick), 54 (GetDateTime)
 */

#include <time.h>

/* Global buffers for gmtime/localtime (not thread-safe, but matches C standard) */
static tm_t _gmtime_buf;
static char _asctime_buf[26];

/* clock() - returns processor time in clock ticks (milliseconds since boot) */
clock_t clock(void) {
    int result;
    asm("SYSCALL #53");
    asm("MOVE result, R0");
    return (clock_t)result;
}

/* difftime() - compute difference in seconds (time1 - time0) */
double difftime(time_t time1, time_t time0) {
    return (double)((int)time1 - (int)time0);
}

/* time() - get current calendar time (Unix timestamp) */
time_t time(time_t *timer) {
    int result;
    asm("SYSCALL #54");
    asm("MOVE result, R0");
    if (timer != 0)
        *timer = (time_t)result;
    return (time_t)result;
}

/* mktime() - convert struct tm to time_t (Unix timestamp) */
time_t mktime(tm_t *timeptr) {
    if (timeptr == 0) return 0;

    int year = timeptr->tm_year + 1900;
    int mon = timeptr->tm_mon;

    /* Days from 1970 to given year */
    int days = 0;
    int y;
    for (y = 1970; y < year; y++) {
        if ((y % 4 == 0 && y % 100 != 0) || (y % 400 == 0))
            days += 366;
        else
            days += 365;
    }

    /* Days from January to given month */
    int m;
    for (m = 0; m < mon; m++) {
        int is_leap = ((year % 4 == 0 && year % 100 != 0) || (year % 400 == 0)) ? 1 : 0;
        const int mdays[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
        days += mdays[m];
        if (m == 1 && is_leap) days += 1;
    }

    /* Add day of month - 1 */
    days += timeptr->tm_mday - 1;

    time_t result = (time_t)(days * 86400 + timeptr->tm_hour * 3600 +
                              timeptr->tm_min * 60 + timeptr->tm_sec);

    /* Adjust for DST */
    if (timeptr->tm_isdst > 0)
        result -= 3600;

    return result;
}

/* Internal: convert epoch timestamp to date components */
static void epoch_to_date(int timestamp, int *year, int *mon, int *mday,
                           int *wday, int *yday) {
    int sec_of_day = timestamp % 86400;
    if (sec_of_day < 0) sec_of_day += 86400;
    int days = timestamp / 86400;
    if (timestamp < 0 && sec_of_day > 0) days--;

    int day_of_week = (days + 4) % 7; /* 1970-01-01 was Thursday */
    if (day_of_week < 0) day_of_week += 7;

    int y = 1970;
    while (1) {
        int is_leap = (y % 4 == 0 && y % 100 != 0) || (y % 400 == 0);
        int days_in_year = is_leap ? 366 : 365;
        if (days < days_in_year) break;
        days -= days_in_year;
        y++;
    }

    *year = y;
    *yday = days;

    int is_leap = (y % 4 == 0 && y % 100 != 0) || (y % 400 == 0);
    const int mdays[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
    int m;
    for (m = 0; m < 12; m++) {
        int md = mdays[m];
        if (m == 1 && is_leap) md = 29;
        if (days < md) break;
        days -= md;
    }
    *mon = m;
    *mday = days + 1;
    *wday = day_of_week;
}

/* gmtime() - convert time_t to struct tm (UTC) */
tm_t *gmtime(const time_t *timer) {
    if (timer == 0) return 0;

    int ts = (int)*timer;
    int sec_of_day = ts % 86400;
    if (sec_of_day < 0) sec_of_day += 86400;
    int days = ts / 86400;
    if (ts < 0 && sec_of_day > 0) days--;

    _gmtime_buf.tm_sec = sec_of_day % 60;
    _gmtime_buf.tm_min = (sec_of_day / 60) % 60;
    _gmtime_buf.tm_hour = sec_of_day / 3600;

    int year, mon, mday, wday, yday;
    epoch_to_date(ts, &year, &mon, &mday, &wday, &yday);

    _gmtime_buf.tm_year = year - 1900;
    _gmtime_buf.tm_mon = mon;
    _gmtime_buf.tm_mday = mday;
    _gmtime_buf.tm_wday = wday;
    _gmtime_buf.tm_yday = yday;
    _gmtime_buf.tm_isdst = 0;

    return &_gmtime_buf;
}

/* localtime() - same as gmtime() on VML (no timezone support yet) */
tm_t *localtime(const time_t *timer) {
    return gmtime(timer);
}

/* Internal: write 3-char day name */
static void write_wday(char *p, int wday) {
    if (wday == 0) { p[0] = 'S'; p[1] = 'u'; p[2] = 'n'; }
    else if (wday == 1) { p[0] = 'M'; p[1] = 'o'; p[2] = 'n'; }
    else if (wday == 2) { p[0] = 'T'; p[1] = 'u'; p[2] = 'e'; }
    else if (wday == 3) { p[0] = 'W'; p[1] = 'e'; p[2] = 'd'; }
    else if (wday == 4) { p[0] = 'T'; p[1] = 'h'; p[2] = 'u'; }
    else if (wday == 5) { p[0] = 'F'; p[1] = 'r'; p[2] = 'i'; }
    else if (wday == 6) { p[0] = 'S'; p[1] = 'a'; p[2] = 't'; }
}

/* Internal: write 3-char month name */
static void write_mon(char *p, int mon) {
    if (mon == 0) { p[0] = 'J'; p[1] = 'a'; p[2] = 'n'; }
    else if (mon == 1) { p[0] = 'F'; p[1] = 'e'; p[2] = 'b'; }
    else if (mon == 2) { p[0] = 'M'; p[1] = 'a'; p[2] = 'r'; }
    else if (mon == 3) { p[0] = 'A'; p[1] = 'p'; p[2] = 'r'; }
    else if (mon == 4) { p[0] = 'M'; p[1] = 'a'; p[2] = 'y'; }
    else if (mon == 5) { p[0] = 'J'; p[1] = 'u'; p[2] = 'n'; }
    else if (mon == 6) { p[0] = 'J'; p[1] = 'u'; p[2] = 'l'; }
    else if (mon == 7) { p[0] = 'A'; p[1] = 'u'; p[2] = 'g'; }
    else if (mon == 8) { p[0] = 'S'; p[1] = 'e'; p[2] = 'p'; }
    else if (mon == 9) { p[0] = 'O'; p[1] = 'c'; p[2] = 't'; }
    else if (mon == 10) { p[0] = 'N'; p[1] = 'o'; p[2] = 'v'; }
    else if (mon == 11) { p[0] = 'D'; p[1] = 'e'; p[2] = 'c'; }
}

/* asctime() - convert struct tm to string "Www Mmm dd hh:mm:ss yyyy\n" */
char *asctime(const tm_t *timeptr) {
    if (timeptr == 0) return 0;

    char *p = _asctime_buf;

    /* Www */
    int wd = timeptr->tm_wday;
    if (wd < 0 || wd > 6) wd = 0;
    write_wday(p, wd); p += 3;
    *p++ = ' ';

    /* Mmm */
    write_mon(p, timeptr->tm_mon); p += 3;
    *p++ = ' ';

    /* dd */
    int d = timeptr->tm_mday;
    if (d >= 10) *p++ = '0' + d / 10; else *p++ = ' ';
    *p++ = '0' + d % 10;
    *p++ = ' ';

    /* hh:mm:ss */
    int h = timeptr->tm_hour;
    *p++ = '0' + h / 10; *p++ = '0' + h % 10; *p++ = ':';
    int mi = timeptr->tm_min;
    *p++ = '0' + mi / 10; *p++ = '0' + mi % 10; *p++ = ':';
    int s = timeptr->tm_sec;
    *p++ = '0' + s / 10; *p++ = '0' + s % 10;
    *p++ = ' ';

    /* yyyy */
    int y = timeptr->tm_year + 1900;
    *p++ = '0' + (y / 1000) % 10;
    *p++ = '0' + (y / 100) % 10;
    *p++ = '0' + (y / 10) % 10;
    *p++ = '0' + y % 10;
    *p++ = '\n';
    *p = '\0';

    return _asctime_buf;
}

/* ctime() - convert time_t to string */
char *ctime(const time_t *timer) {
    if (timer == 0) return 0;
    tm_t *tm = localtime(timer);
    if (tm == 0) return 0;
    return asctime(tm);
}
