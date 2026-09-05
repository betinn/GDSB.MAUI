# Entrega: branch, PR e ciclo do SonarCloud

## Convenção de branch

```
<plano-feature-da-rodada>/fase<n>-<nome-curto>
```

O prefixo é o nome do plano/feature da rodada e vira um namespace no `git branch`, agrupando todas
as fases da mesma rodada. Exemplos:

- `multilingue/fase1-infra-idioma`
- `multilingue/fase2-migracao-xaml`
- `backups-versionados/fase1-retencao`
- `config-agentes/fase1-orquestrador`

Regras: prefixo e nome curto em minúsculas, sem acento, palavras separadas por `-`; **sem hífen
entre `fase` e o número** (`fase1`, não `fase-1`); a branch nasce da `main`. Vale mesmo quando a
sessão vier com uma branch designada genérica (`claude/...`): crie a branch da fase e abra o PR a
partir dela, não da genérica.

## PR

- **Um PR por fase.** Use a lista de arquivos e o "Pronto quando" da fase (no artifact) como
  checklist do PR.
- Ao terminar a implementação da fase (código pronto e commitado), **abra o PR automaticamente, sem
  perguntar antes** — é parte padrão do fluxo. O PR fica aberto para review do usuário; **nunca
  faça merge sozinho.**
- **O PR nunca é mudo:** a descrição explica o que mudou e por quê. Quem redige é a sessão
  principal, que acabou de revisar os diffs — o agente `entrega-pr` só publica o texto recebido.
- Não fique monitorando o PR além do necessário para fechar o ciclo do Sonar (abaixo) — gasta token
  à toa. Fora isso, se precisar de ajuste, o usuário avisa.
- Não pule fases nem inverta as dependências do plano.

## Ciclo do SonarCloud (regra permanente)

O repositório roda o SonarCloud Code Analysis como check automático em todo push para PR (via
GitHub App, sem log acessível daqui).

Depois de **todo push num PR** — e também ao abrir um PR novo — espere as workflows do GitHub
Actions e o check do SonarCloud terminarem, leia o comentário do `sonarqubecloud[bot]` no PR e
corrija o que for corrigível, **mesmo que a Quality Gate passe**. O alvo é o código mais limpo
possível, não só o check verde: "0 New issues" importa tanto quanto "Quality Gate passed".

Ao corrigir, siga o catálogo de falsos positivos que está no agente `sonar`
(`.claude/agents/sonar.md`) em vez de reinventar a correção — pragma comentado, nunca mudança de
comportamento.

O comentário do bot normalmente traz só a contagem e as condições da Quality Gate (ex.: "4.6%
Duplication on New Code (required ≤ 3%)"), não a lista linha a linha. Para ver o detalhe é preciso
pedir ao usuário que cole o conteúdo da aba "Issues" do PR no SonarCloud — `sonarcloud.io` é
bloqueado por este ambiente.

## Manutenção do plano

Ao fechar uma fase (PR aberto):

1. Atualize a tabela "Status das fases" em `.claude/projeto/plano-rodada.md` (status e link do PR).
2. Se algo do plano mudou no caminho, republique o artifact do plano na mesma URL.
3. Faça commit desse arquivo junto com o PR da fase.
