var p = 1
var n = 25

loop n {
	var d = p
	d -= 1
	p += 1
	var prime = 1
	loop d {
		d += 1
		var remainder = p
		remainder %= d
		ifnot release remainder {
		 	prime = 0
		}
		d -= 2
	}
	release d

	if release prime {
		writenum p
		writeline
		n -= 1
	}
}
