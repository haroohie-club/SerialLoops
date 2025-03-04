mapSettingsOffset: .word 0x020B4E18

@ Makes the event files compare the IDs of interactable objects rather than just their indices
ahook_02066678:
    push {r1}
    ldr r1, =mapSettingsOffset
    ldr r1, [r1]
    ldr r1, [r1]
    ldr r1, [r1, #0x6C]             @ Load the map's interactable objects section

    mov r0, r0, lsl #4              @ interactable objects are 16-bytes long, so idx << 4 shifts us to the correct spot in the array
    add r0, r0, #4                  @ ID is stored at 0x04 in the struct
    ldr r0, [r1, r0]                @ load the ID into r0
    mov r7, r0                      @ replicate the replaced instruction

    pop {r1}
    bx lr

ahook_020666B0:
    push {r8}
    mov r8, #6
    mul r8, r4, r8                  @ evt interactable object structs are 6 bytes long
    ldrh r8, [r6, r8]               @ r6 stores the evt interactable object table
    cmp r7, r8                      @ original instruction being replaced
    pop {r8}
    bx lr
