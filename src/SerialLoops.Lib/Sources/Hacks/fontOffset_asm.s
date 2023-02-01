lastReadChar: .word 0

ahook_0202D830:
    push {r1}
    ldr r1, =lastReadChar
    str r0, [r1]
    pop {r1}
    cmp r0, r1
    bx lr

ahook_0202D8A8:
    push {r1}
    ldr r1, =lastReadChar
    ldr r0, [r1]
    pop {r1}
    push {lr}
    bl font_calculateOffset
    pop {pc}