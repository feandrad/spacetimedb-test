# Registry – Sistema de Registro de Recursos
Este documento descreve como o _Guildmaster_ lida com **recursos** (mapas, entidades, itens etc.) através de um **sistema de registro** que gera identificadores dinamicamente, porém mantém referências persistentes nos arquivos de save. O objetivo é explicar em detalhes:

1. **Carregamento**: Como o jogo (cliente/servidor) processa arquivos e mods ao inicializar  
2. **Geração e uso de `id (UInt32)`**: Por que ele existe e como ele é calculado  
3. **Persistência via `keyId (String)`**: Como isso evita corromper saves ao adicionar/atualizar mods  
4. **Sincronização**: Como cliente e servidor garantem que o mesmo `id` mapeie para o mesmo recurso  
5. **Colisões**: O que acontece se dois mods tentarem usar o mesmo `keyId`, ou se o hash gerar colisão  

---
## 1. **Visão Geral**
No _Guildmaster_, cada recurso tem duas formas de identificação:

1. **`keyId (String)`** – Nome completo e persistente, ex.: `"core:overworld/road_01"`.  
   - Gravado em save files  
   - Definido pelo autor do core ou do mod  
   - Garante que não mude a menos que seja renomeado manualmente  

2. **`id (UInt32)`** – Gerado **dinamicamente** durante o boot:  
   - Usado em runtime para acesso rápido (lookup em array ou map)  
   - Reduz tamanho de pacotes em rede (por ex., ao enviar diffs de tabelas via SpacetimeDB)  
   - Pode mudar a cada inicialização, pois é um índice temporário  

---
## 2. **Carregamento do Registry**
1. **Busca de Recursos**:  
   - O jogo varre pastas de mods, configurações do core game, etc.  
   - Coleta todos os `keyIds` (por exemplo, `["core:overworld/road_01", "core:overworld/road_02", "myMod:custom_sword"]`).  

2. **Registro**:  
   - Para cada `keyId`, o **Registry** cria/associa um objeto de recurso (ex.: `MapNode`, `ItemDef`, etc.)  
   - Gera um hash ou índice único (`id (UInt32)`) para cada `keyId`.  
   - Armazena em dois maps internos:  
     - `Map<String, T>` → busca via `keyId`  
     - `Map<UInt32, T>` → busca via `id`  

3. **Verificação de Colisão**:  
   - Caso dois mods usem **exatamente** o mesmo `keyId`, a engine aplica prioridade (ex.: override) ou falha ao carregar.  
   - Se dois `keyId`s distintos gerarem **hash igual** (colisão de hash), o Registry tentará um **disambiguador** (por ex. incrementando até achar um ID livre).  

4. **Compartilhamento**:  
   - Em Multiplayer, o **servidor mantém o registro centralizado**.  
   - Clientes sincronizam a tabela `keyId` ↔ `id` ao conectar.  
   - Todos os jogadores precisam ter os mesmos mods instalados, mas a consistência de IDs é garantida via **SpacetimeDB**.  

---
## 3. **`keyId (String)` – Identificador Persistente**
- **Formato**: `"namespace:subpath[/opcional]"`  
  - `namespace` pode ser `"core"`, `"myMod"`, `"dlc2"`, etc.  

- **Usado para**:  
  - **Persistir** em saves e bancos de dados  
  - **Mensagens de log** ou ferramentas (ex.: debug)  

- **Nunca muda** a menos que um modder ou o autor do core game renomeie manualmente.  

Exemplo:
```json
{
  "player": {
    "current_map": "core:overworld/road_01",
    "pos": [12.5, 7.0]
  }
}
```
Aqui, `"core:overworld/road_01"` é o `keyId`.

---
## 4. **`id (UInt32)` – Índice de Execução**
- **Gerado** no boot do jogo por alguma função de hash (ex.: FNV-1a, xxHash).
- Pode variar entre execuções, pois depende da ordem de carregamento e do hash.
- **Não** é persistido em arquivo.
**Vantagens**:
1. Acesso rápido em tabelas ou arrays (ex.: `resources[id]`)
2. Pacotes de rede mais enxutos (enviar `4 bytes` em vez de uma string longa)
### 4.1 **Exemplo prático**
Se `"core:overworld/road_01"` for hashado como `0xF00D_1234`, ao enviar pela rede:
```bash
[ id = 0xF00D_1234, payload = ... ]
```
O cliente, sincronizado via SpacetimeDB, também terá mapeado `"core:overworld/road_01"` → `0xF00D_1234`, interpretando corretamente esse ID.

---
## 5. **Sincronização no Multiplayer**
- O **servidor gera e mantém** a tabela `keyId ↔ id`.  
- **Clientes sincronizam** essa tabela automaticamente ao conectar via SpacetimeDB.  
- Todos os jogadores devem ter a mesma lista de mods instalada. Caso contrário, o cliente não conseguirá validar recursos ausentes.  
- O **SpacetimeDB garante** que eventos, reducers e subscriptions usem IDs consistentes entre todas as partes.  

---
## 6. **Por que é seguro trocar mods sem corromper saves?**
- O **save armazena apenas `keyId`**.  
- Ao atualizar ou remover mods, as referências de `id` são regeneradas; porém, se o `keyId` existir, o jogo encontra o mesmo recurso.  
- Se um `keyId` não existir mais (por ex., mod removido), o jogo pode tratar como recurso inexistente e alertar o usuário, mas sem quebrar completamente o save.  

---
## 7. **Tratamento de Colisões**
### 7.1 Colisão de `keyId`
- Dois mods usam a mesma string.  
- O Registry segue uma regra de prioridade (ex.: o mod carregado por último sobrescreve), ou falha se detectar conflito.  
### 7.2 Colisão de `hash (UInt32)`
- Uma função de hash robusta minimiza essa chance.  
- Se ocorrer, o Registry pode rodar uma segunda estratégia (ex.: "hash + incremental" ou "hash + re-hash") até encontrar um ID livre.  

---
## 8. **Exemplo de Fluxo Completo**
### Boot
- Carrega `keyId`s: `["core:overworld/road_01", "core:overworld/road_02", "myMod:custom_sword"]`  
- Gera IDs: `0xF00D_1234, 0xF00D_5678, 0xDEAD_BEEF`  
### Jogo Carrega Save
- Lê `current_map = "core:overworld/road_01"`  
- Descobre que `"core:overworld/road_01"` mapeia para `id = 0xF00D_1234`  
### Multiplayer
- Servidor publica tabela de registros (`keyId ↔ id`).  
- Cliente sincroniza via SpacetimeDB.  
- Ao receber um evento de `UpdateMap` com `id = 0xF00D_1234`, o cliente traduz de volta para `"core:overworld/road_01"`.  

---
## 9. **Conclusão**
- **`keyId (String)`**: Identidade persistente e estável, salva em disco.  
- **`id (UInt32)`**: Índice/hashing dinâmico em runtime, vantajoso para performance e rede.  
- **Registry** garante que servidor e cliente tenham o mesmo mapeamento via SpacetimeDB.  
- **Saves não quebram** ao trocar mods, pois o `keyId` é quem manda na persistência.  
Este modelo combina flexibilidade para mods, segurança em atualizações e performance em rede — trazendo o melhor de ambos os mundos no _Guildmaster_.  
