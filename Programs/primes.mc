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
		var tens
		var ones
		tens, ones = p divmod 10
		if tens {
			repeat 6 {
				tens += 8
			}
			write tens
			release tens
		}
		repeat 6 {
			ones += 8
		}
		write ones
		release ones
		writeline
		n -= 1
	}
}
