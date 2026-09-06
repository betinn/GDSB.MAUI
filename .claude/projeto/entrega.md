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
- **Todo corpo de PR termina com uma seção `## Testes manuais`** — obrigatória, mesmo quando a
  resposta é "nenhum". Ver o formato abaixo.

### `## Testes manuais` no corpo do PR

O usuário é quem roda o app no aparelho. O PR é o roteiro dele, e roteiro vago não se executa:
"conferir se o alerta aparece" não diz onde clicar, o que esperar, nem por que aquilo está na lista.

Cada cenário é um item numerado com três partes, nesta ordem:

1. **Onde** — a tela e o estado de partida (cofre aberto? primeiro acesso? tablet ou celular?).
2. **O que fazer** — os passos, no imperativo, na ordem de execução.
3. **O que tem que acontecer** — o resultado observável, específico o bastante para distinguir
   "passou" de "quase". Número quando houver número (duração de animação, contagem, texto exato).

E, em uma linha antes ou depois, **por que este cenário existe**: o que no diff pode quebrar aqui.
Sem isso o usuário não sabe o que está caçando e não tem como julgar um resultado estranho.

Regras do que entra:

- **Só o que o CI não cobre.** Se `dotnet test` ou o build já provam, não vai para a lista — o
  roteiro manual não repete o automatizado. O que sobra é comportamento de runtime, de UI e de
  plataforma: o app abrir, animação, teclado, biometria, seletor de arquivo, permissão, rotação,
  celular × tablet, troca de idioma ao vivo.
- **Só o que este PR pode ter quebrado.** Nada de regressão genérica do app inteiro. Cada cenário
  se ancora numa mudança concreta do diff.
- **Ordenado por risco**, o mais perigoso primeiro. Se um cenário falhar e derrubar o PR, ele é o
  primeiro.
- **Caminho de erro conta.** Guarda nova que passou a lançar ou a devolver "não deu" precisa de um
  cenário que force esse caminho, não só o do sucesso.
- **Fase que não muda runtime declara isso**: `## Testes manuais` com uma linha — "nenhum: a
  mudança é de compilação/documentação e o CI cobre tudo" — em vez de inventar cenário para
  preencher.

Os cenários não nascem na hora de escrever o PR: cada agente que mexeu em código devolve, no
relatório dele, o que a mudança dele exige conferir no aparelho. A sessão principal junta, corta o
que o CI já cobre, ordena por risco e escreve.
- Não fique monitorando o PR além do necessário para fechar o ciclo do Sonar (abaixo) — gasta token
  à toa. Fora isso, se precisar de ajuste, o usuário avisa.
- Não pule fases nem inverta as dependências do plano.

## Ciclo do SonarCloud (regra permanente)

O repositório roda o SonarCloud Code Analysis como check automático em todo push para PR (via
GitHub App, sem log acessível daqui).

Depois de **todo push que mexa em código** — e também ao abrir um PR novo — espere as workflows do
GitHub Actions e o check do SonarCloud terminarem, leia o comentário do `sonarqubecloud[bot]` no PR
e corrija o que for corrigível, **mesmo que a Quality Gate passe**. O alvo é o código mais limpo
possível, não só o check verde: "0 New issues" importa tanto quanto "Quality Gate passed".

**Push que não toca código não se acompanha.** Atualizar `plano-rodada.md` ao fechar uma fase,
mexer num agente, corrigir o README — nada disso muda o que o compilador vê. O CI roda de novo, mas
roda o mesmo build sobre o mesmo código do push anterior, e o Sonar não analisa Markdown: esperar
esse run e reler o comentário do bot é repetir uma leitura que já foi feita. Não acompanhe, não
peça instantâneo de check ao `entrega-pr` e não deixe assinatura de evento de PR aberta por causa
disso.

Vale como **código** qualquer arquivo em `src/` ou `tests/`, mais `.github/workflows/`, `*.csproj`,
`*.sln`, `*.props`, `*.targets`, `global.json` e `nuget.config`. Um único arquivo desses no push já
manda acompanhar. Todo o resto — `.claude/**`, `*.md`, `docs/`, `LICENSE` — é texto.

Isso não abre exceção no ciclo abaixo: o ciclo é do código, e o código foi conferido no push que o
trouxe. Commit de documentação depois de um PR verde não reabre nada.

**Quality Gate verde não encerra o ciclo.** O comentário-resumo tem que fechar em zero nas três
linhas abaixo — todas, independentemente de a Quality Gate passar:

| Linha do resumo | Alvo | Se não bater |
|---|---|---|
| **New issues** | `0` | buscar o detalhe linha a linha e corrigir |
| **Security Hotspots** | `0` | tratar como bloqueio: **segurança nunca fica em aberto**, mesmo aceita pela Quality Gate |
| **Duplication on New Code** | `0.0%` | extrair o trecho duplicado para o padrão reutilizável (`SelectableChip`, `EqualsConverter`, `VaultProtectionsFormViewModelBase`) em vez de copiar o bloco |

Coverage on New Code não é condição de fechamento hoje, mas queda nela merece um comentário no PR.

Quando "New issues" for maior que zero, é obrigatório buscar o detalhe **linha a linha** antes de
considerar a fase fechada — o resumo traz só a contagem, nunca arquivo nem linha.

Onde o detalhe está, neste repositório: o Sonar publica cada apontamento como **check run
annotation** da check run "SonarCloud Code Analysis" (é o que aparece ancorado na linha, na aba
*Files changed*). Isso **não** é review comment, e o MCP do GitHub não expõe esse endpoint. Use:

```bash
bash .claude/scripts/sonar-annotations.sh <número do PR>
```

O script resolve o head do PR, acha a check run do Sonar e imprime `caminho:linha [nível] mensagem`.
Quem roda é o agente `entrega-pr`, que repassa a saída verbatim; quem decide o que fazer com cada
item é a sessão principal.

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
