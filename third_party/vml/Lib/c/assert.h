/* assert.h — VML MCU 安全子集
 * MCU 模式下 assert() 为空操作 (生产环境禁用断言)
 */
#ifndef _ASSERT_H
#define _ASSERT_H

#ifdef NDEBUG
#define assert(expr) ((void)0)
#else
/* MCU 模式下默认禁用断言 */
#define assert(expr) ((void)0)
#endif

#endif /* _ASSERT_H */
