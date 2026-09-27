package main
func main() {
	var a float64 = 3.14
	var b float64 = 2.0
	var h float64 = 0x10
	var d float64 = -0.5
	println_str("F-MUL="); println_int(int(a * b * 100))
	println_str("F-NEG="); println_int(int(d * 100))
	println_str("F-HEX="); println_int(int(h * 100))
}
