@ Replaces evt_mapCharacterSelect call to evt_findMapChibi
@ Original looks for Kyon (param2 = 1); this loads the idx of the first map chibi into param2
ahook_020664E8:
    ldr r1, =0x20C7518
    ldr r1, [r1]
    ldr r1, [r1, #0x4]
    ldr r1, [r1, #0xD4]
    ldr r1, [r1, #0x4]      @ This all loads the script's map characters section
    ldrh r1, [r1]           @ Then this loads the first map character index
    bx lr

@ Replaces evt_interactableObjectSelect call to evt_findMapChibi
@ Original looks for Kyon (param2 = 1); this loads the idx of the first map chibi into param2
ahook_02066680:
    ldr r1, =0x20C7518
    ldr r1, [r1]
    ldr r1, [r1, #0x4]
    ldr r1, [r1, #0xD4]
    ldr r1, [r1, #0x4]      @ This all loads the script's map characters section
    ldrh r1, [r1]           @ Then this loads the first map character index
    bx lr
