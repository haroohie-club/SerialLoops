@ Hook into dbg_print20228DC and make it print to the no$ console

ahook_0202293C:
    push {r1}
    ldr r1, =stringAddress
    str r0, [r1]
    pop {r1} 
    add r2, r2, #4
    bx lr

ahook_02022944:
    push {lr}
    ldr r0, =stringAddress
    ldr r0, [r0]
    bl nocashPrint
    pop {lr}
    add sp, sp, #0x1000
    bx lr

stringAddress: .word 0