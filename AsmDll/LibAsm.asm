.data
 DN3 QWORD 3

.code
CreateSepia proc EXPORT
    local newR: byte
    local newG: byte
    local newB: byte
    push RBP        ; zapisuje adresy rejestrow RBP,RDI,RSP, aby po wykonaniu procedury, zachowac spojnosc w pamieci
    push RDI                
    push RSP

    mov rsi, rcx

    xor rax, rax
    mov rbx, 0
for_loop:
    cmp    rbx, rdx
    je     end_loop

    push rdx
    xor rdx, rdx

    xor rax, rax
    mov al, byte ptr [rsi]
    ;mov al, BYTE PTR [rcx+rbx]
    add dx, ax 
    xor rax, rax
    mov al, byte ptr [rsi+1]
    ;mov al, BYTE PTR [rcx+rbx+1]
    add dx, ax
    xor rax, rax
    mov al, byte ptr [rsi+2]
    ;mov al, BYTE PTR [rcx+rbx+2]
   
    add dx, ax
    xor rax, rax
    mov ax, dx

    xorps xmm0, xmm0
    cvtsi2ss xmm0, eax
    mov rdx, 3
    cvtsi2ss xmm1, rdx
    divss xmm0, xmm1

    xor rax, rax
    cvtss2si rax, xmm0

    ;mov BYTE PTR [rcx+rbx], al
    ;mov BYTE PTR [rcx+rbx+1], al
    ;mov BYTE PTR [rcx+rbx+2], al

    xor rdx, rdx
    mov newB, dl
    mov newG, dl
    mov newR, dl

    mov newB, al

    add ax, r8w
    cmp ax, 255
    ja newGWiekszeNiz255
    mov newG, al
    jmp Next1
newGWiekszeNiz255:
    mov newG,255
Next1:

    add ax, r8w
    cmp ax, 255
    ja newRWiekszeNiz255
    mov newR, al
    jmp Next2
newRWiekszeNiz255:
    mov newR,255
Next2:
    
    xor rax, rax
    mov al, newB
    mov ah, newG
    mov BYTE PTR [rsi], al
    mov BYTE PTR [rsi+1], ah
    xor rax, rax
    mov al, newR
    mov BYTE PTR [rsi+2], al

    inc rsi
    inc rsi
    inc rsi

    add rbx, 3          ; ++c
    pop rdx
    jmp    for_loop

end_loop:
    pop rsp        ;z koncem programu ustawiam spowrotem wartosci rejestrow pobierajac je ze stosu.
    pop rdi                        
    pop rbp                        
    
    ret            ;koniec procedury
CreateSepia endp
end