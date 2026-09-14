// VML 全类型转换库 — Objective-C 包装器 (v1.66.44)
#import <Foundation/Foundation.h>

@interface VMLConv : NSObject
+ (NSString*)intToStr:(int)val;
+ (int)strToInt:(NSString*)s;
+ (NSString*)longToStr:(long long)val;
+ (long long)strToLong:(NSString*)s;
+ (NSString*)floatToStr:(float)f;
+ (float)strToFloat:(NSString*)s;
+ (NSString*)doubleToStr:(double)d;
+ (double)strToDouble:(NSString*)s;
+ (NSString*)boolToStr:(BOOL)b;
+ (BOOL)strToBool:(NSString*)s;
+ (NSString*)charToStr:(char)c;
+ (char)strToChar:(NSString*)s;
@end
