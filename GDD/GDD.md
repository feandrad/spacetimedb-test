# Guildmaster – Game Design Document (GDD)
## **1. Visão Geral**
**Gênero:** Simulação de gerenciamento, exploração e aventura multiplayer  
**Plataforma:** PC (com suporte total a controle e teclado/mouse)  
**Experiência Principal:** Multiplayer cooperativo para até 4 jogadores
Em Guildmaster, os jogadores gerenciam uma taverna localizada em frente a uma estrada. Durante o dia, exploram regiões vizinhas, coletam recursos, constroem estruturas e interagem com NPCs e outros jogadores. O jogo combina simulação de vida, construção e aventura em um mundo interconectado.

---
## **2. Estrutura de Multiplayer**
- Suporte a **servidores de até 4 jogadores**  
- **Multiplayer é o foco principal da experiência**  
- Servidores podem ser:
  - **Oficiais (hospedados pelos desenvolvedores)**
  - **Auto-hospedados pelos jogadores**, com um **server kit** oficial  
- Apenas **um jogador precisa comprar o jogo**  
  - Amigos podem entrar via **sistema de convite**  
  - Requer criação de conta no site do jogo  
- O **save data é vinculado ao servidor**, e não ao host, garantindo persistência independente de quem estiver hospedando  
- O servidor é **autoritativo**, validando todo movimento, combate e interações.  

👉 **Detalhes técnicos** sobre mapas, movimento e combate estão nos arquivos separados:  
- [[Multiplayer and Maps]] - Estrutura de mundo, instanciamento, conexões e transições  
- [[Combat and Movement]] - Movimento, prediction, reconciliação e regras de combate PvE  
- [[Controls]] - Esquema de botões, stances e ações contextuais  
- [[Technical Implementation]] - Status de implementação, sistemas integrados e testes realizados  

---
## **3. Estrutura de Mundo**
- O mundo é baseado em um **sistema de grafos**:
  - Cada mapa é um **nó**
  - Conexões representam **saídas para áreas vizinhas ou interiores**
- Ao criar um novo mundo:
  - O sistema seleciona mapas pré-definidos e seus arredores
  - Áreas como selvas, cavernas e dungeons são **geradas proceduralmente**
- **Cavernas, dungeons e wilderness**:
  - São resetadas **diariamente**
- O **tempo no jogo só avança se pelo menos um jogador estiver online**

👉 **Para detalhes técnicos de mapas e instanciamento**, ver [[Multiplayer and Maps]].

---
## **4. Estrutura de Fazenda e Construção**
- Cada jogador possui uma **fazenda própria com áreas ao redor**  
- Através de um **sistema de caravana**, é possível visitar fazendas de amigos  
- Em fazendas alheias:
  - Jogadores **só podem construir em terrenos reservados**
  - É possível **reivindicar um lote**, que pode ser construído livremente
  - O **dono da fazenda pode expulsar visitantes e revogar permissões**

---
## **5. Controles e Ações**
👉 O esquema completo de controles e ações está detalhado em [[Controls]].

---
## **6. Sistema de Colisão**
- Usa **AABB (Axis-Aligned Bounding Box)** para detecção  
- Rápido e suficiente para ambientes baseados em tiles  
- Permite colisões simples e objetivas sem alto custo computacional  

---
## **7. Movimento e Correção de Posição**
👉 O sistema de movimento, input prediction e reconciliação está detalhado em [[Combat and Movement]].

---
## **8. Tecnologias e Rede**
- **Servidor**: construído sobre **SpacetimeDB** (Rust)  
- **Cliente**: desenvolvido em **Raylib (C#)**  
- Comunicação ocorre via **reducers** (intenções) e **subscriptions** (estado validado), transportados em **WebSocket/BSATN**  
- Toda lógica crítica (movimento, combate, loot, instanciamento de mapas) é validada no servidor  
👉 **Detalhes técnicos sobre registro de recursos** (mapas, itens, entidades) estão documentados em [[Registry]].

---
## **9. Rotina Diária**
O jogo segue um calendário com **4 semanas por mês**, semelhante a Stardew Valley. Não há previsão para adicionar estações.
### **Fases do Dia**
- **Manhã**:
  - Um **vendedor especial aparece** oferecendo upgrades, missões ou desbloqueios relacionados à **progressão da guilda**.
- **Dia**:
  - Os jogadores **exploram, coletam recursos e preparam itens** (cozinhar, fermentar, organizar estoque).
  - É o momento para otimizar a produção e visitar outros mapas.
- **Crepúsculo**:
  - O visual do jogo muda, indicando que é hora de **retornar à taverna**.
  - Serve como transição natural e aviso visual para encerrar atividades externas.
- **Noite (na Taverna)**:
  - Vai até **meia-noite**.
  - Os jogadores **interagem com NPCs**, recebendo pedidos de itens, liberando diálogos, etc.
### **9.1 Consequências de Não Retornar à Taverna**
- Funcionários da taverna lidam com os atendimentos, mas o jogador:
  - **Recebe menos lucro**
  - **Perde interações com NPCs**
  - **Não presencia o evento especial da manhã seguinte**
- Se o jogador ficar fora até as 2h da manhã:
  - Um **acampamento improvisado** será montado automaticamente
  - O jogador **não recebe o bônus “descansado”**
  - Dependendo do mapa, o jogador pode:
    - **Acordar sem alguns itens**
    - **Ser atacado ou emboscado ao amanhecer**

---
## **10. A Taverna**
A taverna é o centro das atividades noturnas e da gestão da guilda em Guildmaster. Dividida em várias áreas funcionais, ela é o principal local de interação com NPCs, evolução da reputação e construção de relacionamentos.
### **10.1 Salão Principal**
- Durante a noite (até meia-noite), NPCs visitam a taverna com **pedidos de itens ou pratos específicos**.  
- O jogador pode:
  - Atender o pedido exato  
  - **Sugerir um item alternativo**, se não tiver o que foi solicitado  
- Cada atendimento influencia:
  - **Reputação** do jogador  
  - Possibilidade de receber **dicas úteis**
### **10.2 Cozinha**
- O jogador pode **preparar pratos** com ingredientes coletados durante o dia.  
- A qualidade dos ingredientes define a **qualidade do prato final**.  
- Inclui um **Livro de Receitas** e a possibilidade de contratar um **cozinheiro NPC** no progresso da guilda.
### **10.3 Acomodações**
- Quartos para hospedar viajantes.  
- Hospedar NPCs é necessário para **desbloquear eventos narrativos** e **avançar histórias**.
### **10.4 Painel de Quests**
- Quadro onde o jogador pode **postar missões** adquiridas ao conversar com NPCs.  
- **Aventureiros autônomos** podem aceitar e cumprir missões.  
- Jogador pode realizar missões pessoalmente ou delegar.
### **10.5 Área de Membros da Guilda**
- Jogador escolhe **quais NPCs convidar para a guilda** com base na lealdade.  
- NPCs aceitos passam a viver em uma **casa comunal**.  
- A lealdade continua evoluindo e desbloqueia novos eventos e funcionalidades.  
