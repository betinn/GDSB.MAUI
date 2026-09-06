# GDSB.MAUI — contrato da sessão principal

A sessão principal é **orquestradora e comandante**, não operária: decide, distribui, verifica e
fecha o ciclo. Quem executa são os subagentes de `.claude/agents/`.

## O que é da sessão principal

1. **Decidir** o desenho: arquitetura, quais arquivos mudam, em que ordem, o que é escopo e o que
   não é. Decisão de desenho, corte de escopo e conflito entre regras não se delegam.
2. **Distribuir** a execução, um agente por domínio, com briefing fechado.
3. **Verificar** o que voltou lendo o **diff**, não o relatório do subagente.
4. **Redigir** mensagem de commit e descrição de PR (ela é quem tem o contexto dos diffs).
5. **Conversar** com o usuário.

Ela pode editar direto ajustes de 1–2 linhas e os arquivos de `.claude/projeto/`. Migração, tela
nova, catálogo e varredura vão para subagente.

## Quando delegar

Delegue quando a saída for ruidosa (build/teste, >200 linhas) ou a edição for repetitiva (>10
pontos). Abaixo disso, faça inline: disparar subagente tem custo fixo (system prompt, partida fria,
releitura de arquivo), e para um `grep` pontual a ferramenta `Grep` direta é mais barata.

| Situação | Agente |
|---|---|
| "onde fica X?", inventário, contagem, mapa de impacto antes de decidir | `mapeador` |
| ViewModels, serviços e abstrações (`src/GDSB.MAUI.ViewModels`, `src/GDSB.Domain`) | `dev-viewmodels` |
| Views, XAML, code-behind, controles, estilos, `src/GDSB.MAUI/Platforms` | `dev-xaml` |
| Criptografia, formato `.GDSBX`, `ProfileFileService`, backups e retenção | `cripto-backup` |
| `.resx`, chaves de tradução, `TrExtension`, `HelpTopics`, `HelpVisuals.xaml` | `i18n` |
| Escrever teste xUnit e fake | `testes` |
| Triar e corrigir apontamento do Sonar | `sonar` |
| Rodar build/teste/checagem estática/varredura local e devolver o resultado | `verificador` |
| Aplicar tabela de→para fechada num lote de arquivos | `aplicador-lote` |
| Branch, commit, push, abrir PR, status de check | `entrega-pr` |

Paralelize o que não se cruza. Dois agentes escrevendo o mesmo arquivo é conflito garantido:
**só o `i18n` escreve `.resx`** — os outros consomem chaves já criadas.

## Briefing obrigatório de todo subagente

- **Objetivo** em uma frase e **critério de pronto** (o "Pronto quando" da fase).
- **Lista fechada de arquivos** que ele pode tocar. Fora dela, ele reporta, não edita.
- As **armadilhas** relevantes, copiadas de `.claude/projeto/plano-rodada.md` — não confie na
  memória dele.
- O que **não** fazer: não commitar, não abrir PR, não trocar decisão já fechada com o usuário.
- Como **verificar** antes de devolver.

Subagente não commita e não faz push. Quem publica é `entrega-pr`, com texto pronto vindo daqui.

## Regras invioláveis

- Nunca reintroduzir a criptografia antiga (IV fixo, senha ASCII ciclada como chave, sem
  autenticação) — nem em produção, nem em teste, nem como fallback.
- Nome de arquivo (cofre e backup) é invariante de cultura.
- `GDSB.MAUI.ViewModels` nunca referencia `GDSB.MAUI` (ciclo).
- Chip novo é `SelectableChip`, nunca `Button` + `DataTrigger`.
- Nunca definir `SatelliteResourceLanguages`.
- A palavra `TODO` não entra em comentário (S1135 não distingue idioma — escreva "cada").
- Neste ambiente `net10.0-android` **não compila** (a rede bloqueia o Android SDK). XAML só é
  validado pelo CI; compense com a verificação estática de `.claude/projeto/ambiente.md`.
- Nunca fazer merge de PR sozinho.
- Warning de build não é ruído do ambiente: build ou teste verde com warning não é "passou" —
  `verificador`, `testes` e `entrega-pr` reportam contagem por código, e a sessão planeja a correção.

## Onde está o resto

| Leia | Quando |
|---|---|
| `.claude/projeto/arquitetura.md` | antes de mexer em código: projetos, formato `.GDSBX`, padrões reutilizáveis |
| `.claude/projeto/ambiente.md` | build, teste, limitação de rede, checklist de verificação estática |
| `.claude/projeto/plano-rodada.md` | antes de abrir uma fase: objetivo, status, decisões e armadilhas da rodada |
| `.claude/projeto/entrega.md` | branch, PR e o ciclo obrigatório do SonarCloud |

Ao fechar uma fase, atualize `plano-rodada.md` (status e link do PR) e republique o artifact.
