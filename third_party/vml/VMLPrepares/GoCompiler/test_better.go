package main

func add(x int, y int) int {
    return x + y
}

func main() {
    a := 10
    b := 20
    result := add(a, b)
    println("Result:", result)
    
    if result > 25 {
        println("Greater than 25")
    } else {
        println("Less than or equal to 25")
    }
}