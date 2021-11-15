# Mental Caress

## Declarations

```
var a
var b = 2
var c = b
var d = 'Z'

release a
release b
release c
release d
```

## I/O

```
read a
readnum b
write a
writenum b
writeline
writetext "Hello, world"
```

## Arithmetic

Operators are `+ - * / %`.

```
a = 1
a = b
a = b + 1
a += 2
```

## Other

```
a = not b
a, b = c divmod d
```

## Blocks

All local variables are implicitly released at the end of each block.

```
loop a {
	# loops while a is non-zero
}
if a {
}
if release a {
	# releases the control variable before entering the body
}
ifnot a {
}
ifnot release a {
}
```

