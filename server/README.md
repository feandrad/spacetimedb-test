# Guildmaster Server (SpacetimeDB)

## Como rodar

### Pré-requisitos
```bash
# Instalar SpacetimeDB CLI (se necessário)
curl --proto '=https' --tlsv1.2 -sSf https://install.spacetimedb.com | sh

# Adicionar target WebAssembly ao Rust
rustup target add wasm32-unknown-unknown
```

**Por que WASM?** O SpacetimeDB compila seu código Rust para WebAssembly em vez de código nativo. Isso garante portabilidade, segurança (sandbox isolado) e comportamento determinístico entre servidores.

### Executar o servidor (Local)
```bash
# Iniciar servidor local na porta 7734 (evita conflito com porta 3000)
spacetime start -l 0.0.0.0:7734

# Em outro terminal, publicar o módulo
spacetime publish guildmaster
```

O servidor rodará em: `http://localhost:7734`

## Deploy em servidor Linux

### 1. Preparar o servidor
```bash
# Atualizar sistema
sudo apt update && sudo apt upgrade -y

# Instalar dependências
sudo apt install -y curl build-essential

# Instalar Rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
source ~/.cargo/env

# Adicionar target WASM
rustup target add wasm32-unknown-unknown

# Instalar SpacetimeDB CLI
curl --proto '=https' --tlsv1.2 -sSf https://install.spacetimedb.com | sh
```

### 2. Configurar o servidor
```bash
# Clonar o projeto
git clone <seu-repositorio>
cd guildmaster-server

# Iniciar SpacetimeDB como serviço (porta 7734)
nohup spacetime start -l 0.0.0.0:7734 > spacetime.log 2>&1 &

# Publicar o módulo
spacetime publish guildmaster
```

### 3. Configurar firewall (se necessário)
```bash
# Ubuntu/Debian com ufw
sudo ufw allow 7734/tcp

# CentOS/RHEL com firewalld
sudo firewall-cmd --permanent --add-port=7734/tcp
sudo firewall-cmd --reload
```

### 4. Criar serviço systemd (opcional)
```bash
# Criar arquivo de serviço
sudo tee /etc/systemd/system/guildmaster.service > /dev/null <<EOF
[Unit]
Description=Guildmaster SpacetimeDB Server
After=network.target

[Service]
Type=simple
User=$USER
WorkingDirectory=$HOME/guildmaster-server
ExecStart=/home/$USER/.local/share/spacetime/bin/current/spacetimedb-standalone --listen-addr 0.0.0.0:7734
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

# Habilitar e iniciar o serviço
sudo systemctl daemon-reload
sudo systemctl enable guildmaster
sudo systemctl start guildmaster

# Verificar status
sudo systemctl status guildmaster
```

O servidor ficará acessível em: `http://SEU_IP:7734`

## Status
The SpacetimeDB server setup is in progress. The current implementation has some compatibility issues with SpacetimeDB 0.11 API that need to be resolved.

## Structure
- `src/lib.rs`: Main server entry point with Player table and basic reducers
- `src/player.rs`: Player management reducers
- `src/movement.rs`: Movement validation reducers
- `src/combat.rs`: Combat system reducers
- `src/map.rs`: Map transition reducers
- `src/inventory.rs`: Inventory management reducers

## Next Steps
1. Resolve SpacetimeDB API compatibility issues
2. Implement proper table definitions
3. Add reducer implementations
4. Test server deployment

## Notes
The server is designed to be authoritative for all game logic including:
- Player movement validation
- Combat hit detection
- Inventory management
- Map transitions
- Collision detection

All client actions are validated server-side to prevent cheating and ensure consistency across multiplayer sessions.