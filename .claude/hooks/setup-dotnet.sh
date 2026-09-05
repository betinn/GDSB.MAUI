#!/usr/bin/env bash
# SessionStart: garante o .NET 10 SDK no PATH da sessão.
# Idempotente e tolerante a falha: nunca derruba a sessão (sai sempre 0).

set -u

find_dotnet_root() {
  for candidato in "$HOME/.dotnet" "/usr/share/dotnet" "/usr/lib/dotnet"; do
    [[ -x "$candidato/dotnet" ]] && { echo "$candidato"; return 0; }
  done
  return 1
}

if command -v dotnet >/dev/null 2>&1; then
  echo "[gdsb] dotnet $(dotnet --version 2>/dev/null) já no PATH."
  exit 0
fi

if raiz="$(find_dotnet_root)"; then
  export DOTNET_ROOT="$raiz"
  export PATH="$DOTNET_ROOT:$PATH"
  echo "[gdsb] dotnet encontrado em $raiz (versão $("$raiz/dotnet" --version 2>/dev/null))."
  echo "[gdsb] Se o shell de uma ferramenta não enxergar: export DOTNET_ROOT=$raiz; export PATH=\$DOTNET_ROOT:\$PATH"
  exit 0
fi

# Neste container o apt é o caminho que funciona: builds.dotnet.microsoft.com é bloqueado
# pelo proxy de egress, então o script oficial dotnet-install.sh costuma falhar.
if command -v apt-get >/dev/null 2>&1 && [[ "$(id -u)" == "0" ]]; then
  echo "[gdsb] Instalando dotnet-sdk-10.0 via apt..."
  if apt-get update -qq >/dev/null 2>&1 && apt-get install -y -qq dotnet-sdk-10.0 >/dev/null 2>&1; then
    echo "[gdsb] dotnet $(dotnet --version 2>/dev/null) instalado."
    exit 0
  fi
  echo "[gdsb] apt não conseguiu instalar o SDK."
fi

echo "[gdsb] .NET 10 SDK indisponível. 'dotnet test' e 'dotnet build' não vão rodar até instalar:"
echo "[gdsb]   apt-get update && apt-get install -y dotnet-sdk-10.0"
echo "[gdsb] O alvo net10.0-android não compila neste ambiente de qualquer forma (Android SDK bloqueado)."
exit 0
