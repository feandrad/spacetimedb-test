# 🎮 Guildmaster – Controles e Ações
## 📌 Objetivo
Definir os esquemas de controle e as ações básicas do jogador em Guildmaster.  
Este documento complementa o GDD principal e é a referência para implementação de input no cliente.

---
## 🕹️ Esquema de Botões
### Botões principais
- **A / X** → Ação 1 (interação geral, não destrutiva)  
- **X / Square** → Ação 2 (interação destrutiva)  
- **B / Circle** → Cancelar  
- **Y / Triangle** → Desequipar item  
### Movimento e Equipamentos
- **Left Stick / WASD** → Movimentação  
- **Right Stick** → Girar direção do personagem sem mover  
- **D-Pad** → Equipamento rápido  
  - Direções são **configuráveis pelo jogador**  
  - Podem conter **loadouts de combate** ou **consumíveis de uso instantâneo**

---
## 🛡️ Stances
- **LT / L2** → Stance Defensiva  
  - Permite strafe lento  
  - Direcional ativa **dodge roll**  
    - Possui **i-frames no início**  
    - **Cancela a maioria das animações**  
    - **Bloqueia ações até o fim da rolagem**  
  - Shields e certos itens modificam o comportamento do strafe  

---
## 🎯 Ações Contextuais
Executadas apenas quando:
- O jogador está **desarmado**  
- Está **próximo de um objeto interativo**

| Objeto        | Ação 1 (A/X) | Ação 2 (X/Square) |
|---------------|--------------|-------------------|
| Árvore        | Sacudir      | Cortar            |
| Pedra         | Pegar        | Quebrar           |
| Bloco         | Mover        | Quebrar           |
| Borda d'água  | Pescar       | Pular             |
| Dentro d'água | Remar        | Mergulhar         |

---
## 📐 Considerações de Design
- Controles devem ser **definidos para controle em primeiro lugar** mas ser compatível com teclado/mouse sem perda de funcionalidades.  
- **Customização de binds** é essencial para acessibilidade.  
- Ações contextuais **não podem conflitar** com inputs principais; o servidor valida intenções através de reducers.  
- Dodge roll e stance defensiva serão usados como **base para sistemas de combate** (detalhados em `movement_combat.md`).  
