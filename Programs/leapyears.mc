var y1 = 1
var y2 = 8
var y3 = 0
var y4 = 0

var working = 1
loop working {
    # adding 4 to y4
    y4 = y4 + 4
    working = y4 / 10
    # testing carry
    if working {
        y4 = y4 - 10
        y3 = y3 + 1
        working = y3 - 10
        ifnot working {
            y3 = 0
            y2 = y2 + 1
            working = y2 - 10
            ifnot working {
                y2 = 0
                y1 = y1 + 1
            }
        }
    }

    var show = 0
    # show = not y2
    show = not y2
    # working = y2 - 4
    working = y2 - 4
    working = not working
    show = show + working
    show = show + y3
    show = show + y4
    if show {
        var twelve = 12
        loop twelve {
            twelve = twelve - 1
            y1 = y1 + 4
            y2 = y2 + 4
            y3 = y3 + 4
            y4 = y4 + 4
        }

        write y1
        write y2
        write y3
        write y4
        writeline

        twelve = 12
        loop twelve {
            twelve = twelve - 1
            y1 = y1 - 4
            y2 = y2 - 4
            y3 = y3 - 4
            y4 = y4 - 4
        }
    }

    working = y2 - 4
}