package main
func main() {
	var a float64 = 3.14
	var b float64 = 2.0
	var big int64 = 3000000000
	var add int64 = 1000000000
	var pos int64 = 4294967296
	println_str("F-MUL="); println_int(int(a * b * 100))
	println_str("D-MUL="); println_int(int(a * b * 100))
	println_str("F-NEG="); println_int(int(-0.5 * 100))
	println_str("D-NEG="); println_int(int(-0.5 * 100))
	println_str("L-ADD="); println_int(int((big + add) / 1000000000))
	println_str("L-MUL="); println_int(int(((5000000000 / 5) * 2) / 1000000000))
	println_str("L-NEG="); println_int(int((0 - pos) / 1000000000))
}
