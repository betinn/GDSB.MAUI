# Contexto do projeto — GDSB.MAUI

> Este arquivo é o **ponto de entrada** de qualquer sessão de Claude Code neste repositório. Leia-o
> inteiro antes do primeiro comando: ele diz o que é o projeto, como preparar a máquina, onde está o
> plano em execução e em que fase ele parou.

## O que é o GDSB

App de gerenciamento de senhas para Android e Windows: guarda credenciais criptografadas em um
arquivo local `.GDSBX`. Existe uma versão desktop separada (fora do escopo deste repositório). O
usuário usa bastante em um tablet grande (Samsung S10 Ultra) e no celular, então comportamento
responsivo é requisito real, não opcional.

Estrutura da solução:

- `src/GDSB.Domain` — entidades (`Profile`, `SecretBox`) e interfaces de serviço.
- `src/GDSB.Infrastructure` — criptografia (v2 e leitor legado v1) e acesso a arquivo.
- `src/GDSB.MAUI.ViewModels` — ViewModels e as abstrações de plataforma (clipboard, navegação,
  preferências, launcher, alertas) que os tornam testáveis sem o runtime do MAUI. É um projeto
  `net10.0` "puro": não pode referenciar `GDSB.MAUI` (dependência circular), por isso as rotas do
  Shell aparecem como string literal e precisam bater com os nomes registrados em `AppShell.xaml.cs`.
- `src/GDSB.MAUI` — o app em si: Views, Shell e as implementações de plataforma (Android/Windows).
- `tests/GDSB.Infrastructure.Tests` e `tests/GDSB.MAUI.Tests` — xUnit, com fakes escritos à mão
  (sem biblioteca de mock).

## Como rodar o projeto

### 1. Instalar o .NET 10 SDK

```bash
# Linux/macOS — instala em ~/.dotnet, sem precisar de root
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version   # deve imprimir 10.x
```

```powershell
# Windows
winget install Microsoft.DotNet.SDK.10
```

### 2. Rodar os testes (não precisa de workload nenhum)

```bash
dotnet test tests/GDSB.Infrastructure.Tests/GDSB.Infrastructure.Tests.csproj
dotnet test tests/GDSB.MAUI.Tests/GDSB.MAUI.Tests.csproj
```

### 3. Compilar o app (aí sim precisa do workload MAUI)

```bash
dotnet workload restore src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true
dotnet build   src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true
```

No Linux, use sempre `-p:GdsbAndroidOnly=true` — sem isso o MSBuild tenta também os alvos de iOS e
macCatalyst. No Windows/macOS, omitir a flag compila todos os alvos. O build Android também exige o
Android SDK instalado.

## Formato de arquivo

Arquivos novos são gravados em **AES-256-GCM**, com a chave derivada da senha mestra via
PBKDF2-HMAC-SHA256 (salt e nonce aleatórios a cada gravação, autenticação integrada). O cabeçalho é
auto-descritivo:

```
offset  tamanho  campo
0       4 bytes  magic = "GDSB"
4       1 byte   versão do formato = 0x02
5       1 byte   KDF id (0x01 = PBKDF2-HMAC-SHA256; 0x02 reservado p/ Argon2id)
6       4 bytes  iterações do KDF (uint32, little-endian)
10      16 bytes salt
26      12 bytes nonce do AES-GCM
38      16 bytes tag de autenticação do AES-GCM
54      restante ciphertext (JSON do Profile, cifrado)
```

Arquivos `.GDSBX` antigos (v1) continuam abrindo: o formato é detectado pelos 4 primeiros bytes
(`ProfileFileService`) e, ao salvar pela primeira vez, o arquivo é migrado para v2 de forma
transparente, com o original preservado num backup.

**Regra permanente:** nenhuma mudança pode reintroduzir o comportamento antigo de criptografia (IV
fixo, senha ASCII ciclada como chave, ausência de autenticação) — nem em código de produção, nem em
teste, nem como fallback.

---

## Plano em execução

O plano completo desta rodada — contexto, decisões fechadas, **plano macro**, **plano micro por
fase** (arquivos a criar/alterar, regras e "Pronto quando") e o **protótipo visual** da seleção de
idioma — vive neste documento:

**➜ https://claude.ai/code/artifact/f5f89a9f-0e1f-4144-9343-2a673d03adb7**

Ele é a fonte da verdade. `WebFetch` funciona nessa URL. **Leia a seção da fase que você vai
executar antes de escrever qualquer código**; aqui embaixo fica só o resumo, para você se
localizar rápido.

Artifacts das rodadas anteriores, só como histórico:
https://claude.ai/code/artifact/6bd2735a-f8fd-45ad-b7f3-4ff869c8de33 (rodada 3 — primeiro acesso
guiado e backups versionados) e
https://claude.ai/code/artifact/00e12b9d-b9d9-4c72-9a4e-e111477c329d (rodada 2 — desbloqueio,
backups fora da pasta do cofre, edição de cofre).

### Objetivo da rodada

**Deixar o app multilíngue: português do Brasil (padrão) e inglês.**

Hoje não existe **uma única linha** de infraestrutura de localização no repositório: nenhum
`.resx`, nenhum `NeutralLanguage`, nenhuma `CultureInfo` sendo lida em lugar nenhum (as três
ocorrências no código são o parâmetro ignorado de `IValueConverter.Convert`). Todo texto visível é
literal:

| Onde | Literais | Concentração |
|---|---|---|
| XAML (`Text=`, `Placeholder=`, `Span.Text`, `SemanticProperties.Description`) | ~173 (≈103 únicos) | 17 arquivos; `VaultSettingsPage` 38, `CreateVaultPage` 32, `HelpVisuals.xaml` 25, `BackupRecoveryPage` 21, `ItemEditorView` 20 |
| C# (ViewModels, catálogo de ajuda, code-behinds, plataformas) | ~116 | `Help/HelpTopics.cs` sozinho tem 48 (~5.150 caracteres de prosa) |
| **Total** | **≈290** | **≈320 chaves de recurso** |

O idioma é **configuração do app**, gravada em `Preferences` (chave `gdsb.language`), ao lado de
`gdsb.onboardingSeen` — **não** vai em `Profile.Settings`, onde moram as proteções e a retenção de
backup: um arquivo `.GDSBX` pode ser aberto em qualquer aparelho, e o idioma é preferência de quem
está lendo a tela, não propriedade do arquivo.

### Status das fases

| # | Fase | Depende de | Status | PR |
|---|------|------------|--------|-----|
| 0 | Contexto e plano (este arquivo + artifact) | — | ✅ Concluída | — |
| 1 | Infra de idioma + seleção na home | — | ✅ Concluída | [#23](https://github.com/betinn/GDSB.MAUI/pull/23) |
| 2 | Migração do XAML restante | 1 | ✅ Concluída | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |
| 3 | Migração dos ViewModels | 1 | ✅ Concluída | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |
| 4 | Ajuda (`HelpTopics`) e tutorial | 1 | ✅ Concluída | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |
| 5 | Fechamento (revisão do inglês, README, contexto, build) | 2, 3, 4 | ✅ Concluída | [#24](https://github.com/betinn/GDSB.MAUI/pull/24) |

Dependências:

```
                    ┌──► 2  XAML          ──┐
1  infra + seleção ─┼──► 3  ViewModels    ──┼──► 5  fechamento
                    └──► 4  ajuda/tutorial──┘
```

As fases **2, 3 e 4** são independentes entre si — só compartilham o `.resx`, que cresce por
adição. A fase 1 trava tudo porque é ela que prova o mecanismo de troca ao vivo.

**Exceção de entrega combinada nesta rodada:** **dois PRs**, não um por fase. A fase 1 sai sozinha
num PR (é ela que decide a arquitetura e vale revisar antes); as fases 2 a 5, que são migração
mecânica, saem juntas num segundo PR. A regra de "um PR por fase" da seção "Como trabalhar"
continua valendo por padrão nas rodadas seguintes.

### Arquitetura escolhida

```
GDSB.MAUI.ViewModels  (net10.0 — testável, é onde o catálogo TEM que morar)
├── Resources/AppStrings.resx        ← pt-BR (neutro, embutido no assembly principal)
├── Resources/AppStrings.en.resx     ← inglês (assembly satélite en/)
├── Localization/AppLanguage.cs      ← código + endônimo; lista fechada de 2
├── Localization/ILocalizationService.cs · LocalizationService.cs
└── Localization/LocalizedObject.cs  ← base que reemite PropertyChanged na troca

GDSB.MAUI  (o app)
└── Localization/TrExtension.cs      ← {loc:Tr Chave} no XAML → Binding para o serviço
```

O catálogo mora em `GDSB.MAUI.ViewModels` porque `tests/GDSB.MAUI.Tests` referencia **só**
`GDSB.Domain` e `GDSB.MAUI.ViewModels` — não consegue referenciar `GDSB.MAUI`, então tudo que
precisa de teste tem que estar ali. E porque o projeto já é o lugar dos catálogos de texto:
`Help/HelpTopics.cs` declara no próprio cabeçalho que o texto de ajuda mora ali "e não no XAML".

### Decisões já fechadas com o usuário

Não relitigar durante a implementação — o detalhamento de cada uma está no artifact.

- **Um dropdown (`Picker`) na própria tela inicial**, não um painel sobreposto e não uma tela de
  configurações nova. Aplica e grava **no próprio evento de mudança** — sem botão de confirmar.
- **A escolha sobrevive ao fechamento do app.** Gravada em `Preferences` (`gdsb.language`) no mesmo
  instante da troca, e lida na inicialização **antes do primeiro XAML** — o app reabre direto no
  idioma escolhido, com o dropdown já mostrando qual é. Só a primeira abertura, sem preferência
  gravada, cai no pt-BR. Verificar matando o app pelo gerenciador de tarefas: mandar para segundo
  plano não recria o processo e portanto não testa nada.
- **Troca ao vivo**, sem reiniciar: cada texto é binding para o catálogo, e a tela muda embaixo do
  dedo. Nada de recriar a Shell, nada de voltar para o começo.
- **Datas e números seguem o idioma**: `02/09/2026 14:30` → `9/2/2026 2:30 PM`; `1,5 KB` → `1.5 KB`.
- **Nomes de arquivo continuam invariantes** (`yyyy-MM-dd HH-mm-ss` em `VaultBackupNaming`), sob
  pena de um backup gravado em português deixar de ser reconhecido em inglês. É o único ponto do
  plano com risco de corromper dado — confirmar arquivo a arquivo antes de mexer em
  `DefaultThreadCurrentCulture`.
- **pt-BR é o idioma neutro**, inglês é satélite: chave faltando em inglês cai no português em vez
  de sumir da tela (`ResourceManager` faz isso de graça). **Não definir
  `SatelliteResourceLanguages`** em lugar nenhum — restringir essa lista faria o inglês sumir num
  build Release.
- **Os nomes dos idiomas no dropdown nunca são traduzidos**: "Português (Brasil)" e "English (US)"
  aparecem sempre assim, para quem caiu num idioma que não lê conseguir achar o seu.
- **Convenção de chave: `Tela_Elemento`**, PascalCase com `_` (vira nome de propriedade C#, então
  precisa ser identificador válido). Prefixo `Common_` para o que aparece em mais de uma tela —
  `Excluir` e `Cancelar` aparecem 5× cada hoje.
- **Fora do catálogo, de propósito:** glifos e entidades (`?`, `*`, `&#9733;`, `&#9881;`, `&#8592;`,
  `👁`, a máscara `••••••••••`), números soltos de chip, `CommandParameter`, ids de tópico
  (`vault.backups`), ids de amostra visual (`HelpVisual.BackupCard`), rotas (`"VaultPage"`), a
  marca `GDSB` e os endônimos do dropdown.
- **Fora do escopo:** o nome do app (`ApplicationTitle` continua "GDSB" nos dois idiomas — é
  marca), um terceiro idioma, e traduzir dados do usuário (nomes de cofre, de item, observações).

### Armadilhas conhecidas desta rodada

- **`Resources/HelpVisuals.xaml` é a que se esquece.** Seus 13 `DataTemplate` são réplicas das
  telas reais e carregam **cópias** dos mesmos textos. Traduzir as páginas e não traduzir ela deixa
  o painel de ajuda em português dentro de um app em inglês.
- **`HelpTopics.All` é materializado uma vez** no inicializador estático do tipo — trocar o idioma
  depois da primeira leitura não muda nada. Precisa reconstruir a partir do catálogo, com cache por
  cultura. Os **ids** continuam constantes: são chave, não texto.
- **`HelpBlock.Value` é prosa quando `Kind` é `Heading`/`Text` e id de recurso quando é `Visual`**
  (aí a prosa está em `Caption`). Um find/replace cego sobre `Value` corrompe o catálogo de
  amostras.
- **`CreateVaultPage` e `VaultSettingsPage` são quase clones** nos blocos PROTEÇÕES/BACKUPS (~20
  frases idênticas) e **`VaultPage` duplica o cabeçalho inteiro** (compacto vs largo, 6 frases).
  Mesma chave nos dois lugares, não uma por página.
- **`LanguageSelectorViewModel.Selected` precisa ser semeado no construtor** com
  `_localization.Current`, não deixado no `null` do campo gerado. Sem isso o app reabre no idioma
  certo mas o dropdown aparece **em branco**, porque `SelectedItem` não bate com nenhum item de
  `Options` — o estado fica correto e a tela mente sobre ele. É por isso que a guarda de igualdade
  no `OnSelectedChanged` não é opcional: a semeadura dispara o handler durante a construção.
- **A ordem de aplicação da cultura no `MauiProgram` importa:** o `TrExtension` lê o catálogo na
  cultura vigente quando o binding avalia pela primeira vez. Aplicada depois que `App` construiu a
  `AppShell`, a primeira renderização sai em português mesmo com inglês gravado. `builder.Build()`
  roda antes de `App` ser construído, então é o ponto seguro; qualquer lugar mais tarde não é.
- **Risco central do plano:** a troca ao vivo depende de o binding `[Chave]` reagir à notificação
  do serviço. `SetLanguage` deve emitir **`PropertyChanged("Item[]")` e `PropertyChanged(null)`** —
  cobre os dois caminhos do `BindingExpression` do MAUI. Isso **só é verificável rodando o app**, e
  é por isso que a fase 1 prova o mecanismo numa tela antes de aplicá-lo em 300 lugares. Plano B se
  falhar no aparelho: `DynamicResource` + o serviço reescrevendo `Application.Current.Resources`.
- **`BackupItemViewModel` não é `ObservableObject`** — na troca de idioma é o
  `BackupRecoveryViewModel` que reconstrói a coleção, não cada item que se notifica.
- **`HelpSheetView.xaml.cs` provavelmente não muda:** `Show` já relê `HelpTopics.TryGet` e
  `template.CreateContent()` a cada abertura, então acompanha sozinho.

---

## Como trabalhar

- **Uma branch por fase, sempre nomeada `fase-<N>-<nome-curto>`** (ex.: `fase-1-retencao-backups`),
  criada a partir da `main`. Vale mesmo quando a sessão já vier com uma branch designada genérica
  (tipo `claude/...`): crie a branch da fase a partir dela (ou da `main`) e faça o PR a partir da
  branch com nome de fase, não da genérica.
- **Um PR por fase.** Use a lista de arquivos e o "Pronto quando" da fase, no artifact, como
  checklist do PR. **Nesta rodada (4) vale a exceção combinada com o usuário:** dois PRs — a fase 1
  sozinha, e as fases 2 a 5 juntas. Ver "Exceção de entrega combinada nesta rodada", acima.
- Ao terminar a implementação da fase (código pronto e commitado), **abra o PR automaticamente, sem
  perguntar antes** — é parte padrão do fluxo. O PR fica aberto para review do usuário; não faça
  merge sozinho.
- O PR nunca é "mudo": a descrição sempre explica o que mudou e por quê.
- **Não fique monitorando o PR** depois de aberto além do necessário pra fechar o ciclo do Sonar
  (ver "Regra permanente" na seção SonarCloud, logo abaixo) — gasta token à toa. Fora isso, se
  precisar de ajuste, o usuário avisa.
- Não pule fases nem inverta as dependências listadas acima.

### SonarCloud

O repositório roda o SonarCloud Code Analysis como check automático em todo push pro PR (não é um
step do workflow do GitHub Actions — é uma integração via GitHub App, sem log acessível por aqui).

**Regra permanente:** depois de todo push num PR — e também ao abrir um PR novo — espere as
workflows do GitHub Actions e o check do SonarCloud terminarem, leia o comentário que o
`sonarqubecloud[bot]` posta no PR (isso funciona por aqui: é um comentário normal de PR, acessível
pelas ferramentas de GitHub, mesmo com `sonarcloud.io` bloqueado pela rede deste ambiente) e
corrija o que for corrigível — **mesmo que a Quality Gate passe**. O alvo é o código mais limpo
possível, não só o check verde: "0 New issues" importa tanto quanto "Quality Gate passed", e vale
apagar apontamentos mesmo quando eles não bloqueiam o merge. Ao corrigir, siga os "Achados
recorrentes de falso positivo" catalogados logo abaixo (pragma comentado em vez de mudar
comportamento) em vez de reinventar a correção a cada vez.

O comentário do `sonarqubecloud[bot]` normalmente só traz a contagem/condições da Quality Gate
(ex.: "4.6% Duplication on New Code (required ≤ 3%)"), não a lista de issues linha a linha. Pra ver
o detalhe é preciso pedir pro usuário colar o conteúdo da aba "Issues" do PR no SonarCloud
(`sonarcloud.io` está bloqueado pela rede deste ambiente, tanto por `WebFetch` quanto por
`curl`/API, mesmo com token — confirmado de novo nesta sessão).

Na rodada 2, os 75 issues abertos do relatório de 2026-09-01 (2 vulnerabilidades, 1 bug,
72 code smells) foram corrigidos junto com as fases 5 e 6, no PR #17. Como o Sonar continua
inacessível daqui, o "0 issues" em New Code e Overall Code nunca chegou a ser confirmado por uma
sessão; se isso importar, precisa de confirmação humana olhando o SonarCloud direto.

**Atenção redobrada na rodada 4 (multilíngue):** a regra **S1135** procura a palavra `TODO` e não
distingue idioma — o português "todo" em comentário vira um apontamento cada. Esta rodada escreve
comentários novos em dezenas de arquivos; use "cada" ou reformule.

Achados recorrentes de falso positivo neste projeto, todos ligados a como o Sonar resolve símbolos
gerados por source generator (o `[ObservableProperty]` do CommunityToolkit.Mvvm e os campos de
`x:Name` que o `InitializeComponent()` do XAML gera):

- **S2068 "Hard-coded credentials"**: flaga a *declaração* de qualquer campo/const cujo nome bata
  com password/pwd/passphrase e tenha um valor de string literal — mesmo em teste. Evite nomes com
  essas palavras para constantes de teste (ex.: `VaultUnlockCode` em vez de `Password`).
- **S2325 "Make X a static method/property"**: dispara em propriedades/métodos que só leem outra
  propriedade gerada por `[ObservableProperty]` (ex.: `CanInteract => !IsBusy`) ou que referenciam
  um elemento nomeado do XAML (ex.: `SealOverlay.PlayAsync(...)`) — nos dois casos o Sonar não
  reconhece o acesso como "dado de instância". Não dá pra genuinamente tornar esses membros
  `static` sem quebrar o binding/o comportamento; a correção é suprimir com
  `#pragma warning disable S2325` / `restore S2325` em volta do trecho, com um comentário curto
  explicando o porquê (não usar `[SuppressMessage]` nem desabilitar a regra no projeto inteiro).
- **S1135 "Complete the task associated to this 'TODO' comment"**: a regra procura a palavra
  `TODO` isolada e **não distingue idioma** - o português "todo" (em "todo painel", "todo tópico",
  "todo id") vira um apontamento cada. É provavelmente a armadilha mais fácil de cair neste
  repositório, já que os comentários são todos em português. Escreva "cada" ou reformule; "toda",
  "todos" e "todas" não disparam, só a forma exata "todo".
- **S1125 "Remove the unnecessary Boolean literal"**: `Application.Current?.Resources.TryGetValue(...) == true`
  é flagado, mesmo o `== true` sendo o que destrincha o `bool?` que o acesso condicional produz. O
  desenho que o projeto já usa e que não dispara nada está em `SelectableChip.ResourceColor`:
  guardar `Application.Current?.Resources` numa variável e testar `resources is not null && ...`.
- **Como ver os apontamentos sem o SonarCloud.** `sonarcloud.io` é bloqueado pela rede deste
  ambiente, mas o pacote `SonarAnalyzer.CSharp` vem do nuget.org normalmente. Um
  `Directory.Build.props` temporário na raiz com
  `<PackageReference Include="SonarAnalyzer.CSharp" Version="*" PrivateAssets="all" />` faz o
  `dotnet build` cuspir os mesmos `warning S####`, com arquivo e linha. **Apague o arquivo antes de
  commitar.** Para o projeto `src/GDSB.MAUI`, que não compila aqui (sem Android SDK), dá para
  montar um projeto `net10.0` de scratch que inclui os `.cs` de verdade por caminho absoluto mais
  um arquivo de stubs com os campos que o `InitializeComponent()` geraria - foi assim que a
  varredura desta rodada cobriu os code-behinds.
  **Atenção ao limite disso:** o perfil padrão do pacote NuGet é mais estreito que o "Sonar way" do
  SonarCloud. Nesta rodada, as três últimas issues (S2325 em `OnboardingViewModel.ShowFromStart` e
  duas S1125 em `HelpSheetView`) **não apareceram** no analisador local, só no SonarCloud. Ou seja:
  local limpo reduz muito o ruído, mas não substitui a leitura da aba "Issues" do PR - que, com o
  `sonarcloud.io` bloqueado, precisa ser colada pelo usuário.
- Bloco `catch (Exception) { }` vazio sem tratamento: preencha com um comentário de uma linha
  explicando por que ignorar é intencional (regra de "empty block"/"handle the exception").
- `CommandParameter` de `Button`/etc. no XAML sempre chega como `string` ao `ICommand` ligado, não
  importa o tipo do parâmetro no C# — `RelayCommand<int>` lança `InvalidCastException` em silêncio
  (o clique não faz nada, sem erro visível). Prefira declarar o método do `[RelayCommand]` com
  parâmetro `string` e converter dentro dele (`int.Parse(...)`).

Essas issues só aparecem como "New" na primeira vez que a linha em questão é tocada nesta rodada —
o mesmo padrão já existe, sem supressão, em ViewModels mais antigos (`CanInteract => !IsBusy` em
`UnlockViewModel`/`CreateVaultViewModel`, por exemplo), só não é reportado ali por ser código de
antes da janela de "New Code" do Sonar.

### Duplicação de código (métrica "Overall Code", não só "New Code")

O bloco `Button.Triggers` + `DataTrigger` + 3 `Setter` (destacar um chip quando um valor bate) se
repetia dezenas de vezes em `VaultPage.xaml`, `VaultSettingsPage.xaml` e `CreateVaultPage.xaml` —
era a maior fonte de duplicação do projeto. Resolvido com dois componentes reutilizáveis, em vez de
copiar o bloco de novo:

- `GDSB.MAUI.Controls.SelectableChip` (`src/GDSB.MAUI/Controls/SelectableChip.cs`) — subclasse de
  `Button` com a propriedade bindable `IsSelected`; aplica o destaque em C# (lendo as cores do
  `Application.Current.Resources`) em vez de um `DataTrigger` por instância. **Cuidado**:
  `BindableObject.SetDynamicResource`/`RemoveDynamicResource` são `internal` ao assembly do MAUI,
  não públicos — por isso o controle usa `ClearValue` pra voltar ao valor da `Style` (que precisa
  de `ApplyToDerivedTypes="True"`, já que o `TargetType` dela é `Button`, não `SelectableChip`).
- `GDSB.MAUI.Converters.EqualsConverter` (registrado globalmente em `App.xaml`, não por página) —
  compara o valor bindado com o `ConverterParameter` do chip (sempre como string, já que
  `CommandParameter`/`ConverterParameter` do XAML sempre chegam como string).

Uso: `<controls:SelectableChip ... IsSelected="{Binding X, Converter={StaticResource
EqualsConverter}, ConverterParameter=20}" />` pra grupos de valor único (substitui o `DataTrigger`
com `Value="20"`), ou `IsSelected="{Binding AlgumBool}"` direto pra chips de modo (substitui
`Value="True"`). **Todo chip novo usa `SelectableChip`, nunca mais `Button` + `DataTrigger`.**

Duplicação também apareceu entre `VaultSettingsViewModel` e `CreateVaultViewModel` (as duas telas
espelham os mesmos campos de PROTEÇÕES/BACKUPS) — resolvida com a classe base
`VaultProtectionsFormViewModelBase` (`src/GDSB.MAUI.ViewModels/ViewModels/`), que carrega as
propriedades `[ObservableProperty]`, as listas de opções `static` e os comandos `Select*`
compartilhados. `SaveProtectionsAsync`/`CreateVaultAsync` continuam em cada ViewModel — fazem
coisas fundamentalmente diferentes com esses valores, não são candidatos a extração.

### Limitação de rede deste ambiente (Claude Code na web)

A política de egress bloqueia `dl.google.com` e `builds.dotnet.microsoft.com` (403 no proxy), então
**o Android SDK não pode ser instalado aqui e o alvo `net10.0-android` não compila** (para em
`XA5300`). O SDK do .NET 10 vem do apt (`apt-get update && apt-get install -y dotnet-sdk-10.0`) e o
workload MAUI restaura normalmente, então `dotnet test` e o build dos projetos `net10.0` funcionam -
mas **a compilação do XAML só é validada pelo job `build-android` do CI**. Não tente contornar o
bloqueio; conte com o CI e compense com verificação estática (XML bem formado, todo
`{StaticResource}` resolvendo, ids de ajuda batendo com o catálogo) antes do push.

## Como manter este arquivo

Ao final de cada fase (PR aberto):

1. Atualize a tabela "Status das fases" acima (status e link do PR).
2. Se algo do plano mudou no caminho, atualize também o artifact do plano — dá para republicar um
   artifact de outra sessão passando a mesma URL (veja a doc de artifacts do Claude Code:
   "Update an artifact from an earlier conversation").
3. Faça commit deste arquivo junto com o PR da fase.
