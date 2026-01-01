# 🎮 Guildmaster – Movimento e Combate (Servidor Autoritativo)
## 📌 Objetivo
Definir as regras de movimentação e combate em Guildmaster.  
Este documento complementa o GDD principal e é a referência oficial para implementação de cliente e servidor.

---
## 🕹️ Movimento

### Autoridade
- O **servidor é a fonte da verdade** (movimento autoritativo).  
- O **cliente chama reducers** para enviar intenções (ex.: direção, ações, deltaTime).  
- O servidor valida, aplica e retorna o estado real através de tabelas assinadas.

### Prediction no Cliente
- O cliente usa **input prediction** para suavizar a experiência.  
- Ao assinar a tabela `actors`, o cliente recebe:
  - Um **snapshot inicial** (posição, velocidade, `last_seq`).  
  - **Diffs subsequentes** conforme o servidor processa novos reducers.  
- Reconciliação: cliente reaplica localmente os inputs com `seq > last_seq` retornado pelo servidor.

### Correção Híbrida (exemplo de política)
| Erro entre cliente e servidor | Ação                     |
|-------------------------------|--------------------------|
| ≤ 5 px                        | Lerp leve (fator 0.1)    |
| 5–15 px                       | Lerp mais agressivo (0.4)|
| > 15 px                       | Snap + reapply inputs    |

### Reducers de Movimento
- `InputMove(seq, ax, ay, dt)`  
  - `seq`: número de sequência para reconciliation.  
  - `ax, ay`: direção normalizada.  
  - `dt`: deltaTime do frame.  
- Servidor atualiza `actors` com posição, velocidade e `last_seq`.  
- Cliente assina `actors` filtrado por `map_instance_id`.

---
## ⚔️ Combate PvE

### Escopo
- **PvE apenas (sem PvP)** no MVP.  
- Jogadores enfrentam **NPCs/monstros**.  
- Servidor controla aggro, dano e loot via reducers e atualizações de tabelas.

### Regras Gerais
- **Friendly Fire**: OFF.  
- **Body-block**: OFF entre jogadores.  
- **Loot**: instanciado por jogador/party ou auto-split.  
- **Downed State**: jogador derrubado pode ser revivido por aliados.  
- **Respawn**: em checkpoints do mapa.

### NPCs e Ameaça
- Cada Mapa mantém uma **tabela de ameaça (threat table)**.  
- Ameaça reseta por **tempo** ou **distância (leash)**.  
- O comportamento exato para grupos perseguindo jogadores será definido em balanceamento de gameplay.  

### Eventos de Combate
- `CombatEvent(hit|heal)`  
- `AggroTransferInitiated`  
- `AggroTransferCompleted`

> Todos os eventos de combate são representados como **linhas em tabelas** (`combat_events`, `npc_states` etc.), consumidas via **subscriptions**.  
> O servidor atualiza também `actors` e `npcs` (HP, buffs, estados) e o cliente reage aos **diffs** recebidos.

---
## 🛡️ Anti-Griefing
- Sem friendly fire.  
- Sem body-block.  
- Loot protegido (instanciado ou split).  
- Interact com cooldown (ex.: 500ms).  
- Rate limit de intenções: **10/s** por ator.  
- Reducers são **idempotentes** (`player_id + seq`) para ignorar reenvios.  
- Portas/portais não podem bloquear aliados.  
- Party management: **kick** e **mute** básicos.

---
## ⚙️ Parâmetros Default (MVP)
- Correção híbrida: Lerp/Snap conforme tabela.  
- Rate limit: 10 intenções por segundo.  
- TTL Warm (mapa sem players): 60s (ver `maps.md`).  
- Respawn: em checkpoint do mapa.  
- Cooldown Interact: 500ms.

---
## 🌐 Transporte e Sincronização
- Comunicação é feita via **SpacetimeDB SDK**.  
- **Reducers**: cliente → servidor (intenções).  
- **Subscriptions**: servidor → cliente (estado validado).  
- Transporte ocorre sobre **WebSocket** usando **BSATN** (binário).  
- Não há uso direto de TCP/UDP na aplicação.
