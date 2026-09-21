/* getopt.h —— 转发。函数本体在 Lib/shared/src/util.c，全局变量在 unistd.h。 */
#ifndef _GETOPT_H
#define _GETOPT_H

#param lib("util")

extern char *optarg;
extern int optind;
extern int opterr;
extern int optopt;

#define no_argument        0
#define required_argument  1
#define optional_argument  2

struct option {
    const char *name;
    int has_arg;
    int *flag;
    int val;
};

int getopt(int argc, char **argv, const char *optstring);

#endif /* _GETOPT_H */
