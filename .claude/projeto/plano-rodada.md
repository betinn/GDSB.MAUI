# Plano em execução

> Arquivo volátil: é o único de `.claude/projeto/` que muda a cada fase. O orquestrador lê antes de
> abrir uma fase e atualiza ao fechá-la.

O plano completo da rodada — contexto, decisões fechadas, plano macro, plano micro por fase
(arquivos a criar/alterar, regras e "Pronto quando") e protótipos visuais — vive no artifact:

**➜ https://claude.ai/code/artifact/f5f89a9f-0e1f-4144-9343-2a673d03adb7**

Ele é a fonte da verdade. `WebFetch` funciona nessa URL. **Leia a seção da fase antes de escrever
qualquer código**; aqui embaixo fica só o resumo.

Artifacts anteriores, como histórico:
https://claude.ai/code/artifact/6bd2735a-f8fd-45ad-b7f3-4ff869c8de33 (rodada 3 — primeiro acesso
guiado e backups versionados) ·
https://claude.ai/code/artifact/00e12b9d-b9d9-4c72-9a4e-e111477c329d (rodada 2 — desbloqueio,
backups fora da pasta do cofre, edição de cofre).

## Rodada 4 — app multilíngue (pt-BR padrão + inglês) — **concluída**

| # | Fase | Depende de | Status | PR |
|---|------|------------|--------|-----|
| 0 | Contexto e plano | — | ✅ | — |
| 1 | Infra de idioma + seleção na home | — | ✅ | [#23](https://github.com/betinn/GDSB.MAUI/pull/23) |
| 2 | Migração do XAML restante | 1 | ✅ | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |
| 3 | Migração dos ViewModels | 1 | ✅ | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |
| 4 | Ajuda (`HelpTopics`) e tutorial | 1 | ✅ | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |
| 5 | Fechamento (revisão do inglês, README, contexto, build) | 2, 3, 4 | ✅ | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |

### Decisões fechadas com o usuário nessa rodada (não relitigar)

- Dropdown (`Picker`) na tela inicial, aplicando e gravando no próprio evento de mudança.
- A escolha sobrevive ao fechamento do app: `Preferences` (`gdsb.language`), lida na inicialização
  **antes do primeiro XAML** (em `MauiProgram`, depois de `builder.Build()` e antes de `App`).
- Troca ao vivo, sem reiniciar: cada texto é binding para o catálogo.
- Datas e números seguem o idioma; **nomes de arquivo continuam invariantes** (`VaultBackupNaming`).
- pt-BR é o idioma neutro, inglês é satélite. **Nunca definir `SatelliteResourceLanguages`.**
- Os nomes dos idiomas no dropdown nunca são traduzidos ("Português (Brasil)", "English (US)").
- Convenção de chave `Tela_Elemento` (PascalCase com `_`), prefixo `Common_` para o que aparece em
  mais de uma tela.
- Fora do catálogo de propósito: glifos e entidades (`?`, `*`, `&#9733;`, `&#9881;`, `&#8592;`, `👁`,
  a máscara `••••••••••`), números de chip, `CommandParameter`, ids de tópico (`vault.backups`), ids
  de amostra (`HelpVisual.BackupCard`), rotas (`"VaultPage"`), a marca `GDSB` e os endônimos.
- Fora do escopo: nome do app (`ApplicationTitle` continua "GDSB"), um terceiro idioma, e traduzir
  dados do usuário (nomes de cofre, de item, observações).

### Armadilhas que sobreviveram à rodada (valem para quem mexer nesses arquivos)

- **`Resources/HelpVisuals.xaml` é a que se esquece.** Seus 13 `DataTemplate` replicam telas reais e
  carregam cópias dos mesmos textos.
- **`HelpTopics.All` é materializado uma vez** no inicializador estático — precisa reconstruir a
  partir do catálogo, com cache por cultura. Os **ids** continuam constantes: são chave, não texto.
- **`HelpBlock.Value` é prosa** quando `Kind` é `Heading`/`Text` e **id de recurso** quando é
  `Visual` (aí a prosa está em `Caption`). Find/replace cego sobre `Value` corrompe o catálogo.
- **`CreateVaultPage` × `VaultSettingsPage`** são quase clones nos blocos PROTEÇÕES/BACKUPS (~20
  frases); **`VaultPage` duplica o cabeçalho inteiro** (compacto × largo). Mesma chave nos dois
  lugares, não uma por página.
- **`Picker.SelectedItem` semeado no construtor** com o valor real do serviço, e guarda de igualdade
  no handler `OnXChanged` — sem isso o dropdown abre em branco enquanto o estado está certo.
- **A ordem de aplicação da cultura no `MauiProgram` importa** (ver decisões acima).
- **Troca ao vivo depende de `SetLanguage` emitir `PropertyChanged("Item[]")` e
  `PropertyChanged(null)`** — cobre os dois caminhos do `BindingExpression` do MAUI. Só é
  verificável rodando o app. Plano B: `DynamicResource` + o serviço reescrevendo
  `Application.Current.Resources`.
- **`BackupItemViewModel` não é `ObservableObject`** — na troca de idioma quem reconstrói a coleção
  é o `BackupRecoveryViewModel`.

## Rodada 5 — a definir

Ainda não há plano aberto. Ao iniciar: criar o artifact, registrar aqui o objetivo, as fases e as
dependências, e só então abrir a primeira branch `<feature>/fase1-<nome-curto>`.
