var y1 = 1
var y2 = 4
var y3 = 0
var y4 = 0

loop y2 {
    y2 += 4

    var y4_low = 0
    y4_low = y4 - 8

    repeat 2 {
        y4 += 2
        ifnot y4_low {
            y4 = 0
            y3 += 1
        }
        y4_low += 2
    }
    release y4_low

    var carry = 0
    carry = y3 - 10
    ifnot carry {
        y3 = 0
        y2 += 1
        carry = y2 - 10
        ifnot release carry {
            y2 = 0
            y1 += 1
        }
    }

    var show = 0
    show = not y2
    y2 -= 4
    ifnot y2 {
        show += 1
    }
    show += y3
    show += y4
    if release show {
        repeat 8 {
            repeat 6 {
                y1 += 1
                y2 += 1
                y3 += 1
                y4 += 1
            }
        }

        write y1
        y2 += 4
        write y2
        y2 -= 4
        write y3
        write y4

        var eight = 10
        write eight
        eight -= 2
        loop eight {
            eight -= 1
            repeat 6 {
                y1 -= 1
                y2 -= 1
                y3 -= 1
                y4 -= 1
            }
        }
    }
}
