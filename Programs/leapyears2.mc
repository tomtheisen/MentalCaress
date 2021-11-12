var y1 = -9
var y2 = 4
var y3 = -10
var y4 = -10

loop y2 {
    y2 -= 6

    var carry = 0
    y4 += 2
    carry = not y4
    y4 += 2
    ifnot y4 {
        carry += 1
    }

    # testing carry
    if release carry {
        y4 -= 10
        y3 += 1
        ifnot y3 {
            y3 = -10
            y2 += 1
            ifnot y2 {
                y2 = -10
                y1 += 1
            }
        }
    }

    var show = 0
    y2 += 10
    show = not y2
    y2 -= 4
    ifnot y2 {
        show += 1
    }
    y2 -= 6

    show += y3
    show += y4
    show += 20
    if release show {
        y1 += 58
        y2 += 58
        y3 += 58
        y4 += 58

        write y1
        write y2
        write y3
        write y4
        writeline

        y1 -= 58
        y2 -= 58
        y3 -= 58
        y4 -= 58
    }

    y2 += 6
}
