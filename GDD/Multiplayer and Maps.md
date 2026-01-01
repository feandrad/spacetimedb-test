# 🗺️ Guildmaster – Sistema de Mapas (Servidor)
## 📌 Objetivo
Esta documentação define o sistema de mapas no servidor autoritativo de _Guildmaster_.  
O sistema é baseado em grafos, com suporte a:
- Navegação entre mapas conectados
- Identificação otimizada via registros
- Mapas estáticos e gerados proceduralmente
- Persistência temporária e reset diário
- Suporte a conteúdo modular (mods)
- **Instâncias sob demanda (Cold/Warm/Hot)**
- **Pré-carregamento de vizinhos para otimizar transições**
- **Perseguição de NPCs entre mapas conectados**

---
## 🧱 Identificação de Mapas

Guildmaster utiliza dois identificadores por mapa:

|Campo|Tipo|Descrição|
|---|---|---|
|`keyId`|`String`|Identificador legível e único, no formato `<namespace>:<path>/<map_name>`|
|`id`|`UInt32`|Identificador interno derivado de `keyId` (ex: hash ou ID incremental)|

### Exemplos de `keyId`

- `"core:overworld/farm"`
- `"core:dungeon/level_2"`
- `"my_mod:nether/maze_entry"`

> O `keyId` é usado em saves e modding.  
> O `id` é usado em runtime e rede binária.

---
## 📦 Estrutura de Mapa (`MapNode`)

```kotlin
data class MapNode(
    val keyId: String,     // Identificador legível
    val id: UInt32,        // ID interno rápido
    val seed: Long? = null,// Apenas em mapas randômicos
    val size: Vector2,     // Tamanho (tiles)
    val connections: List<MapConnection>
)
````

---
## 🔗 Conexões entre Mapas (`MapConnection`)

```kotlin
data class MapConnection(
    val targetKey: String, // keyId do mapa de destino
    val shape: Shape,      // Área de transição (ex: borda sul)
    val entryPoints: List<Vector2>, // pontos de spawn no destino
    val prefetchZone: Shape // zona de pré-aviso para pré-ativação
)
```

---
## 🔗 Conexões entre Mapas (`MapConnection`)

```kotlin
data class MapConnection(
    val targetKey: String,      // keyId do mapa de destino
    val shape: Shape,           // Área de transição (ex: borda sul)
    val entryPoints: List<Vector2>, // pontos de spawn no destino
    val prefetchZone: Shape     // zona de pré-aviso para pré-ativação
)
```

---
## 🔁 Registro de Mapas (Registry)

- Registra mapas via `keyId`.
- Gera `id` único (hash).
- Mantém dois mapas internos: `byKey` e `byId`.
- Apenas o `keyId` é persistido em saves.

---
## ⚡ Instâncias de Mapas (Cold/Warm/Hot)
Cada mapa tem instâncias que variam conforme uso:
- **Hot**: players presentes, simulação ativa (IA, combate, objetos dinâmicos).
- **Warm**: instância pré-ativada (objetos e áreas carregados, sem tick). TTL: ~60s.
- **Cold**: sem simulação, apenas metadados.
### Regras de transição
- **Cold → Warm**: player entra na zona de pré-aviso **ou** inimigos precisam perseguir.
- **Warm → Hot**: primeiro player entra.
- **Hot → Warm**: último player saiu (TTL curto).
- **Warm → Cold**: TTL expirado.

---
## 🧩 Grafo de Mapas
- Define conectividade entre mapas (`src → dst`).
- Armazena:
  - Tipo de ligação (porta, portal, fronteira contínua).
  - Pontos de entrada no destino.
  - Zona de pré-aviso (ativa prefetch no cliente e Warm no servidor).
  - Requisitos (chaves, progressão).

**Funções do grafo:**
- **Cliente**: pré-carregar assets de vizinhos imediatos.  
- **Servidor**: pré-ativar destinos e permitir perseguição cross-map.

---
## 🔄 Navegação e Transição

1. Cliente envia posição continuamente.  
2. Servidor verifica entrada em `MapConnection.shape`.  
3. Se positivo:
   - Determina destino (`targetKey`).
   - Atualiza instância do jogador.
   - Garante que o destino está ao menos Warm → Hot.
   - Emite eventos:
	   - `TransitionStarted`
	   - `TransitionCompleted` (ou `TransitionFailed`).

### Eventos auxiliares
- `PreTransitionHint(map_dst)` → cliente pré-carrega recursos.
- `PlayerJoined / PlayerLeft` → presença em instâncias.

---
## 🧬 NPCs e Perseguição Cross-Map
- NPCs podem seguir players em transições se próximos à borda.
- Usam **tokens de agressão**:
  - Máx. inimigos transferidos: **3**
  - TTL: **10s** após transição
  - Leash: **10s** ou **30m**
- Inimigos transferidos aparecem em pontos laterais do destino.

### Eventos
- `AggroTransferInitiated`
- `AggroTransferCompleted`

---
## 🧩 Reset e Persistência
- Cada instância guarda dados temporários (itens, eventos).
- Reset diário automático.
- Mapas temporários/dungeons excluídos após uso.
- Progresso do jogador é persistido fora da instância.

---
## 🧬 Mapas com Seed
- `seed: Long` gera layout procedural.
- Mesmo `keyId + seed` = mesmo layout.
- Usado em dungeons, arenas ou mapas temporários.

---
## 📐 Bibliotecas utilizadas

| Elemento  | Biblioteca (Java/Kotlin)                   |
| --------- | ------------------------------------------ |
| `Vector2` | `com.badlogic.gdx.math.Vector2`            |
| `Shape`   | `com.badlogic.gdx.math.Rectangle` + custom |

---
## ✅ Conclusão
O sistema de mapas de _Guildmaster_ oferece:
- **Identificação dual** (`keyId` para mods, `id` runtime).
- **Instâncias Cold/Warm/Hot** para eficiência.
- **Pré-carregamento** baseado no grafo para transições suaves.
- **Perseguição cross-map** de NPCs sem simular mapas vazios.
- **Registry centralizado** compatível com mods, procedural e resets diários.
