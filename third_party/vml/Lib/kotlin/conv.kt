// VML 全类型转换库 — Kotlin 包装器 (v1.66.44)
package vml.conv

object Conv {
    external fun intToStr(value: Int): String
    external fun strToInt(s: String): Int
    external fun longToStr(value: Long): String
    external fun strToLong(s: String): Long
    external fun floatToStr(value: Float): String
    external fun strToFloat(s: String): Float
    external fun doubleToStr(value: Double): String
    external fun strToDouble(s: String): Double
    external fun boolToStr(value: Boolean): String
    external fun strToBool(s: String): Boolean
    external fun charToStr(c: Char): String
    external fun strToChar(s: String): Char
}
