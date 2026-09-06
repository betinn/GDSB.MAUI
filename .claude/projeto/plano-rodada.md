# Plano em execução

> Arquivo volátil: é o único de `.claude/projeto/` que muda a cada fase. O orquestrador lê antes de
> abrir uma fase e atualiza ao fechá-la.

O plano completo da rodada — contexto, decisões fechadas, plano macro, plano micro por fase
(arquivos a criar/alterar, regras e "Pronto quando") e protótipos visuais — vive no artifact:

**➜ https://claude.ai/code/artifact/5853da0c-5db1-4870-9ede-a758dd5f0439**

Ele é a fonte da verdade. `WebFetch` funciona nessa URL. **Leia a seção da fase antes de escrever
qualquer código**; aqui embaixo fica só o resumo.

Artifacts anteriores, como histórico:
https://claude.ai/code/artifact/f5f89a9f-0e1f-4144-9343-2a673d03adb7 (rodada 4 — app
multilíngue) ·
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

## Rodada 5 — zerar os warnings de compilação — **em andamento**

Buildar/implantar/publicar pelo Visual Studio produz 522 warnings. O CI reproduz o número: os logs
dos jobs `build-android` e `build-windows` (run 33998740457) fecham em **521 warnings distintos**
(dedup por código + `arquivo:linha`). Dez códigos ao todo, e dois carregam 465 deles. O alvo da
rodada é **0**.

| Código | Qtd | Causa |
|---|---:|---|
| `XC0022` | 293 | `{Binding}` sem `x:DataType` — não existe nenhum `x:DataType` no projeto |
| `XC0103` | 172 | causa única: `TrExtension` sem `[AcceptEmptyServiceProvider]` |
| `CS8602` | 14 | deref de possível nulo em `Platforms/Android` e `Platforms/Windows` |
| `XC0025` | 10 | binding com `Source` explícito (`RelativeSource`/`x:Reference`) em `DataTemplate` |
| `CS0618` | 10 | `MainPage`, `DisplayAlert`, `FadeTo`, `TranslateTo`, `OpenableColumns` |
| `NU1608` | 9 | `.Ktx` do AndroidX fora da faixa dos pacotes base |
| `CS8604` | 7 | argumento possivelmente nulo em `Platforms/Android/Services` |
| `CS8625` `CS8603` `CS8600` | 6 | `FilePickerService` (Android e Windows), `BiometricUnlockService` |

Prefixo de branch da rodada: **`warnings-zero`**.

| # | Fase | Depende de | Warnings | Status | PR |
|---|------|------------|---------:|--------|-----|
| 0 | Contexto e plano | — | — | ✅ | — |
| 1 | `TrExtension` + NU1608 | 0 | −181 | ✅ | [#32](https://github.com/betinn/GDSB.MAUI/pull/32) |
| 2 | Nulabilidade nas camadas de plataforma | 0 | −27 | ✅ | [#35](https://github.com/betinn/GDSB.MAUI/pull/35) |
| 3 | APIs obsoletas (`CS0618`) | 0 | −10 | ✅ | [#35](https://github.com/betinn/GDSB.MAUI/pull/35) |
| 4 | `x:DataType` — páginas sem `CollectionView` | 0 | −211 | ⬜ | — |
| 5 | `x:DataType` + `XC0025` — `VaultPage` e `BackupRecoveryPage` | 4 | −92 | ⬜ | — |
| 6 | Trava por processo (agentes reportam warning) | 1–5 | ±0 | ⬜ | — |

As fases 1, 2 e 3 não se cruzam e podem ir em paralelo; a 5 depende da 4. As fases 2 e 3
foram executadas em paralelo e entregues no mesmo PR, porque a sessão veio com branch designada
fixa (`claude/phases-2-3-parallel-9w1qq8`) em vez das duas branches `warnings-zero/fase*` que a
convenção do `entrega.md` prevê.

Saldo depois da fase 1, relido dos logs do run 34002213953: `build-android` fecha em **335
warnings**, `build-windows` em **327**, e a união deduplicada por código + `arquivo:linha:coluna`
dá **338 distintos** — dois a menos que os 340 previstos. Zero `XC0103`, zero `NU1608` e, o que
importava conferir, **zero `NU1605`**: fixar as `.Ktx` não virou downgrade. Nenhum código novo
apareceu; o que sobrou é exatamente `XC0022`, `XC0025`, `CS0618`, `CS8600`, `CS8602`, `CS8603`,
`CS8604` e `CS8625`.

Saldo depois das fases 2 e 3, relido dos logs do run 34011866431 (PR #35): `build-android` e
`build-windows` fecham **os dois em 303**, e a união deduplicada por código + `arquivo:linha:coluna`
dá **303 distintos** — exatamente a projeção. **Zero `CS0618`** e **zero `CS86xx`**; o que sobra é
só `XC0022` (293) e `XC0025` (10), que é o passivo das fases 4 e 5. Nenhum código novo apareceu — em
particular, zero `XFC0045` e zero `XC0024`.

Duas coisas que o CI corrigiu na medição, e que valem para quem contar warning por análise estática
daqui: a fase 2 deixou passar um `CS8604` em `FilePickerService.cs:122` (`Uri.ToString()` é anulável
no binding, herda de `Object.toString`), e o Sonar apontou um `?.` redundante no
`GetPrefs()?.Edit()?.Clear()?.Apply()` do `DisableAsync`. Os dois só apareceram depois do push.

### Decisões fechadas com o usuário nessa rodada (não relitigar)

- **`XC0025`**: refatorar `SecretBoxItemViewModel` e `BackupItemViewModel` para expor os comandos do
  ViewModel dono. Nada de `NoWarn`.
- **`NU1608`**: fixar os 9 `.Ktx` explicitamente nas versões que o workload já resolve
  (`Activity.Ktx 1.13.0.1`, `Collection.Ktx 1.6.0.1`, `Fragment.Ktx 1.8.9.3`, `Lifecycle.* 2.11.0.1`,
  `SavedState.SavedState.Ktx 1.5.0.1`). `Xamarin.AndroidX.Biometric` já está na última versão
  (1.1.0.33): bumpar não resolve.
- **`CS0618`/`MainPage`**: migrar para `override CreateWindow` nesta rodada, validando no aparelho.
- **Trava anti-regressão**: **não** é `TreatWarningsAsErrors`. É processo — `verificador`, `testes` e
  `entrega-pr` passam a reportar warning ao orquestrador, que planeja a correção (fase 6).

### Armadilhas desta rodada

- **`x:DataType` transforma o binding em binding compilado.** Nome de propriedade errado deixa de
  ser silêncio em runtime e vira **erro de build** (`XFC0045`/`XC0024`). É o ganho real da rodada e
  o risco: **só o CI compila XAML aqui**. Cada fase de XAML fecha com `build-android` **e**
  `build-windows` verdes.
- **`{loc:Tr Chave}` não é afetado por `x:DataType`**: `TrExtension.ProvideValue` devolve um
  `Binding` com `Source` próprio. Não encostar nesses 172 pontos.
- **Nenhum `<Style>` contém `{Binding}`** (conferido): `x:DataType` só é necessário na raiz das
  páginas/`ContentView` e em cada `DataTemplate`.
- **Os `ContentView` herdam o `BindingContext` de quem hospeda** — o `x:DataType` deles é o VM da
  hospedeira (`ItemEditorView` → `VaultViewModel`, `BiometricOptInView` →
  `BiometricOptInCoordinator`, `OnboardingView` → `OnboardingViewModel`).
- **Nulabilidade se corrige com guarda de verdade** (checagem + retorno/exceção com mensagem), nunca
  com `!`. `BiometricUnlockService` só ganha guarda: não encosta em chave, IV nem byte gravado.
