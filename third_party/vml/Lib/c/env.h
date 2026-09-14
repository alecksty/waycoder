/* env.h - Environment variables (OS mode)
 * SYSCALL 360-362
 */

#ifndef _ENV_H
#define _ENV_H

#param lib("os")

const char *get_env(const char *name);
int set_env(const char *name, const char *value);
int get_args(void *buffer);

#endif /* _ENV_H */
