import json
import os
import subprocess

# Configurações do ambiente
SERVER_URL = "http://localhost:7734"
MAPS_DIR = "src/maps"
DB_IDENTITY = "guildmaster" # Nome ou identidade do seu banco

def run_deploy():
    all_templates = []

    # Varre a pasta src/maps
    for filename in os.listdir(MAPS_DIR):
        if filename.endswith(".csv"):
            template_name = filename.replace(".csv", "")
            path = os.path.join(MAPS_DIR, filename)

            with open(path, 'r') as f:
                lines = [l.strip() for l in f.readlines() if l.strip()]
                if not lines: continue

                height = len(lines)
                width = len(lines[0].split(','))

                # Achata o CSV (matriz -> lista linear)
                tile_data = []
                for line in lines:
                    tile_data.extend([int(t.strip()) for t in line.split(',') if t.strip()])

                all_templates.append({
                    "name": template_name,
                    "width": width,
                    "height": height,
                    "tile_data": tile_data
                })

    if not all_templates:
        print("⚠️  Nenhum mapa encontrado em src/maps")
        return

    # Formata como o SpacetimeDB espera (um vetor de templates dentro de um array de argumentos)
    args = [all_templates]

    print(f"🚀 Enviando {len(all_templates)} mapas para o Izanagi...")

    # Chama o reducer atômico
    cmd = [
        "spacetime", "call", "-s", SERVER_URL, DB_IDENTITY,
        "replace_all_templates", json.dumps(args)
    ]

    result = subprocess.run(cmd, capture_output=True, text=True)

    if result.returncode == 0:
        print("✅ Deploy realizado: Tabelas de mapa atualizadas.")
    else:
        print(f"❌ Falha no deploy: {result.stderr}")

if __name__ == "__main__":
    run_deploy()