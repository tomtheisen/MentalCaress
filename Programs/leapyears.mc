var y1 = 1
var y2 = 4
var y3 = 0
var y4 = 0

loop y2 {
    y2 = y2 + 4

    y4 = y4 - 8
    var carry = 0
    carry = not y4
    y4 = y4 + 2
    var _y4 = y4
    !ifnot _y4 {
        carry = carry + 1
    }
    y4 = y4 + 10

    # testing carry
    if carry {
        y4 = y4 - 10
        y3 = y3 + 1
        carry = y3 - 10
        ifnot carry {
            y3 = 0
            y2 = y2 + 1
            carry = y2 - 10
            !ifnot carry {
                y2 = 0
                y1 = y1 + 1
            }
        }
    }

    var show = 0
    show = not y2
    y2 = y2 - 4
    var _y2 = y2
    !ifnot _y2 {
        show = show + 1
    }
    show = show + y3
    show = show + y4
    !if show {
        var twelve = 12
        loop twelve {
            twelve = twelve - 1
            y1 = y1 + 4
            y2 = y2 + 4
            y3 = y3 + 4
            y4 = y4 + 4
        }

        write y1
        y2 = y2 + 4
        write y2
        y2 = y2 - 4
        write y3
        write y4

        twelve = 10
        write twelve
        twelve = twelve + 2
        loop twelve {
            twelve = twelve - 1
            y1 = y1 - 4
            y2 = y2 - 4
            y3 = y3 - 4
            y4 = y4 - 4
        }
    }
}
