# Como gerar imagem de flowmap

## 🧭 1. Padrão confirmado

| Cor testada                 | Direção observada   | Interpretação                                           |
| --------------------------- | ------------------- | ------------------------------------------------------- |
| (128,128,128) “No movement” | **X+ Z−**           | o “neutro” já tem um viés embutido X+Z−                 |
| (0,128,128) “X−”            | **X− Z+**           | reduzindo R → inverte X e também inverte Z parcialmente |
| (128,128,0) “Z−”            | **Z−**              | B controla Z normalmente                                |
| (255,128,255) “X+Z+”        | **X+ (ligeiro Z+)** | Z+ é pequeno, mas consistente                           |
| (255,128,0) “X+Z−”          | **X+ (ligeiro Z−)** | coerente com neutro, apenas mais forte                  |
| (255,128,128) “X+”          | **X+Z+**            | shader mistura Z+ automaticamente em X+                 |

---

## 🧠 2. O que isso revela sobre o shader

1. **Eixo X** vem fortemente do canal **R** (vermelho) → quanto maior R, mais forte X+.
2. **Eixo Z** vem do canal **B** (azul), mas com uma inversão parcial (valores baixos dão Z−, altos dão Z+).
3. Existe um **offset fixo embutido em X+Z−**, ou seja, o shader nunca deixa o fluxo realmente neutro.
4. O shader provavelmente faz algo como:
   [
   \text{FlowDir} = normalize(\textbf{(R,0,B)} - (0.5,0,0.5)) + \textbf{bias}(+X,+Z-)
   ]

---

## 🧩 3. Conclusão prática para pintura

| Deseja fluxo...            | Pinte aproximadamente                |
| -------------------------- | ------------------------------------ |
| **Parado (neutro visual)** | (110,128,140) – compensa o bias X+Z− |
| **X+ puro (forte)**        | (255,128,140)                        |
| **X− puro**                | (0,128,120)                          |
| **Z+ puro**                | (110,128,255)                        |
| **Z− puro**                | (110,128,0)                          |
| **Diagonal X+Z+**          | (255,128,255)                        |
| **Diagonal X+Z−**          | (255,128,0)                          |

💡 Dica: use **128 no canal G** sempre, pois ele não interfere.

---

## ⚙️ 4. Recomendações técnicas no Unity

* **Desmarcar sRGB (Color Texture)** na importação.
* **Compression → None.**
* **Filter Mode → Bilinear.**
* **Wrap → Repeat.**
* Use sempre materiais com **Flow Map Influence** ajustável — às vezes é preciso compensar manualmente o bias com valores negativos.

---

Minhas observações: sRGB desabilitado e compression None
| Deseja fluxo...            | Pinte aproximadamente                |
| -------------------------- | ------------------------------------ |
| (  0,  0,  0) | X+ (amarelo) |
| (  0,128,  0) | X+ (amarelo) |
| (  0,  0,128) | X+Z-- (vermelho) |
| (  0,128,128) | Z+ (verde claro) |
| (  0,255,  0) | X+ (amarelo) |
| (  0,  0,255) | X-Z- (verde escuro) |
| (  0,255,255) | X-Z+ (verde) |
| (  0,255,128) | X+Z+ (verde) |
| (  0,128,255) | X- (verde forte) |
| (128,  0,  0) | X+ (amarelo) |
| (128,128,  0) | X+ (amarelo) |
| (128,  0,128) | X+Z-- (vermelho) |
| (128,128,128) | Z+ (verde claro) |
| (128,255,  0) | X+ (amarelo) |
| (128,  0,255) | X-Z- (verde escuro) |
| (128,255,255) | X-Z+ (verde) |
| (128,255,128) | X+Z+ (verde) |
| (128,128,255) | X- (verde forte) |
| (255,  0,  0) | X+ (amarelo) |
| (255,128,  0) | X+ (amarelo) |
| (255,  0,128) | X+Z-- (vermelho) |
| (255,128,128) | Z+ (verde claro) |
| (255,255,  0) | X+ (amarelo) |
| (255,  0,255) | X-Z- (verde escuro) |
| (255,255,255) | X-Z+ (verde) |
| (255,255,128) | X+Z+ (verde) |
| (255,128,255) | X- (verde forte) |


