var input = 0
readnum input
var star = '*'
var newline = 10
var i = 0
loop input {
	input = input - 1
	i = i + 1
	var j = i
	loop j {
		j = j - 1
		write star
	}
	write newline
}