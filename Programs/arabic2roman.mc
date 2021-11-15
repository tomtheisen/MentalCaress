# This doesn't really work

var d1
var d2
var d3
var d4

read d1
read d2
read d3
read d4

repeat 4 {
	ifnot d4 {
		# d4 += d3 = 0
		loop d3 {
			d3 -= 1
			d4 += 1
		}
		loop d2 {
			d2 -= 1
			d3 += 1
		}
		loop d1 {
			d1 -= 1
			d2 += 1
		}
	}
}

loop d1 {
	d1 -= 48
	loop d1 {
		d1 -= 1
		writetext "M"
	}
}
loop d2 {
	d2 -= 48
	loop d2 {
		d2 -= 1
		writetext "C"
	}
}
loop d3 {
	d3 -= 48
	loop d3 {
		d3 -= 1
		writetext "X"
	}
}
loop d4 {
	d4 -= 48 
	loop d4 {
		d4 -= 1
		writetext "I"
	}
}
