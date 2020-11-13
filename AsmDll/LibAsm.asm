;@author Kamil Pieroñczyk Gr.2 AEI, Inf, rok 3
;Sepia
.data
 DN3 QWORD 3

.code
CreateSepia proc EXPORT
    local newR: byte ;zmienna na nowy kolor czerwony
    local newG: byte ;zmienna na nowy kolor zielony
    local newB: byte ;zmienna na nowy kolor niebieski
    push RBP        ; zapisuje adresy rejestrow RBP,RDI,RSP, aby po wykonaniu procedury, zachowac spojnosc w pamieci
    push RDI                
    push RSP

    mov rsi, rcx ;przenosi tablice typu byte do rsi

    xor rax, rax ;zeruje rax
    mov rbx, 0 ;zeruje rbx
for_loop: ;pêtla po 3 byte dla kazdego piksela
    cmp    rbx, rdx ;sprawdza czy koniec tablicy
    je     end_loop ;jesli tak konczy petle

    push rdx ;zapisuje dlugosc tablicy na stosie
    xor rdx, rdx ;zeruje rdx

    xor rax, rax ;zeruje rax
    mov al, byte ptr [rsi]  ;zapisuje pierwszy byte koloru w al
    add dx, ax ;dodaje go do sumy dx
    xor rax, rax ;zeruje rax
    mov al, byte ptr [rsi+1] ;zapisuje kolejny byte koloru w al
    add dx, ax ;dodaje go do sumy dx
    xor rax, rax ;zeruje rax
    mov al, byte ptr [rsi+2] ;zapisuje kolejny byte koloru w al
   
    add dx, ax ;dodaje go do sumy dx
    xor rax, rax ;zeruje rax
    mov ax, dx ;przenosi sume 3 byte do ax

    xorps xmm0, xmm0 ;zeruje xmm0
    cvtsi2ss xmm0, eax ;przenosi eax do xmm0
    mov rdx, 3 ;zapisuje 3 w rdx
    cvtsi2ss xmm1, rdx ;przenosi rdx do xmm0
    divss xmm0, xmm1 ;wykonuje dzielenie sumy przez 3

    xor rax, rax ;zeruje rax
    cvtss2si rax, xmm0 ;przenosi wynikj dzielenia do rax

    xor rdx, rdx ;zeruje rdx
    mov newB, dl ;zeruje newB
    mov newG, dl ;zeruje newG
    mov newR, dl ;zeruje newR

    mov newB, al ;ustawia niebieski na odcien szarosci

    add ax, r8w ;dodaje do odcienia szarosci parametr sepii
    cmp ax, 255 ;sprawdza czy wynik nie wiekszy niz 255
    ja newGWiekszeNiz255
    mov newG, al ;jesli nie ustawia zielony na kolor szarosci plus parametr
    jmp Next1
newGWiekszeNiz255:
    mov newG,255 ;jesli tak ustawia zielony na 255
Next1:

    add ax, r8w ;dodaje do odcienia szarosci i parametru sepii jeszcze jeden parametr - ten sam (powiela go)
    cmp ax, 255 ;sprawdza czy wynik nie wiekszy niz 255
    ja newRWiekszeNiz255
    mov newR, al ;jesli nie ustawia czerwony na kolor szarosci plus 2*parametr
    jmp Next2
newRWiekszeNiz255:
    mov newR,255 ;jesli tak ustawia zielony na 255
Next2:
    
    xor rax, rax ;zeruje rax
    mov al, newB ;przenosi newB do al
    mov ah, newG ;przenosi newG do ah
    mov BYTE PTR [rsi], al ;ustawia niebieski na wartosc pod al
    mov BYTE PTR [rsi+1], ah ;ustawia zielony na wartosc pod ah
    xor rax, rax ;zeruje rax
    mov al, newR ;przenosi newR do al
    mov BYTE PTR [rsi+2], al ;ustawia czerwony na wartosc pod al

    inc rsi ;zwieksza wskaŸnik o 3
    inc rsi
    inc rsi

    add rbx, 3          ;zwieksza licznik o 3
    pop rdx ;przywraca dlugosc tablicy z stosu
    jmp    for_loop

end_loop:
    pop rsp        ;z koncem programu ustawiam spowrotem wartosci rejestrow pobierajac je ze stosu.
    pop rdi                        
    pop rbp                        
    
    ret            ;koniec procedury
CreateSepia endp
end