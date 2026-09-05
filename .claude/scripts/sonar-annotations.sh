#!/usr/bin/env bash
# Lista os apontamentos do SonarCloud de um PR, ancorados em arquivo e linha.
#
# Por que este script existe: o comentário-resumo do sonarqubecloud[bot] só traz a contagem
# ("N New issues"), e o detalhe linha a linha vive em *check run annotations* - que o MCP do
# GitHub não expõe. Só a API direta responde, e ter isso num script permite pré-aprovar apenas
# ele na allowlist, em vez de liberar curl genérico com token.
#
# Uso:  bash .claude/scripts/sonar-annotations.sh <número do PR>
# Sai 0 quando não há apontamento nenhum.

set -uo pipefail

readonly PR="${1:-}"
readonly REPO="betinn/GDSB.MAUI"
readonly API="https://api.github.com/repos/$REPO"

if [[ -z "$PR" ]]; then
  echo "uso: bash .claude/scripts/sonar-annotations.sh <número do PR>" >&2
  exit 2
fi

if [[ -z "${GITHUB_TOKEN:-}" ]]; then
  echo "GITHUB_TOKEN não está no ambiente - sem ele a API de annotations não responde." >&2
  exit 2
fi

gh_api() {
  local url="$1"
  curl -sS --fail-with-body \
    -H "Authorization: Bearer $GITHUB_TOKEN" \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "$url"
  return $?
}

sha="$(gh_api "$API/pulls/$PR" | python3 -c 'import json,sys; print(json.load(sys.stdin)["head"]["sha"])')" || {
  echo "não consegui ler o PR #$PR" >&2; exit 1; }

echo "PR #$PR - head $sha"

gh_api "$API/commits/$sha/check-runs?per_page=100" \
  | python3 -c '
import json, sys
runs = json.load(sys.stdin).get("check_runs", [])
for r in runs:
    if "sonar" in r["name"].lower():
        print(r["id"], r["name"], r.get("conclusion"), sep="\t")
' | while IFS=$'\t' read -r id nome conclusao; do
      echo "check run: $nome (#$id, $conclusao)"
      gh_api "$API/check-runs/$id/annotations?per_page=100" \
        | python3 -c '
import json, sys
itens = json.load(sys.stdin)
if not isinstance(itens, list) or not itens:
    print("  nenhum apontamento.")
    sys.exit(0)
for a in itens:
    ini, fim = a.get("start_line"), a.get("end_line")
    linha = ini if ini == fim else f"{ini}-{fim}"
    msg = " ".join((a.get("message") or "").split())
    caminho = a["path"]
    nivel = a["annotation_level"]
    titulo = a.get("title") or msg
    print(f"  {caminho}:{linha} [{nivel}] {titulo}")
    if msg and msg != titulo:
        print(f"      {msg}")
'
    done
