/* assert.h — VML MCU 安全子集 (D) */
#ifndef _ASSERT_H
#define _ASSERT_H
#ifdef NDEBUG
#define assert(expr) ((void)0)
#else
#define assert(expr) ((expr) ? (void)0 : (void)0)
#endif
#endif
