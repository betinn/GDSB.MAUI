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
fase** (arquivos a criar/alterar, regras e "Pronto quando") e o **protótipo visual** das telas —
vive neste documento:

**➜ https://claude.ai/code/artifact/6bd2735a-f8fd-45ad-b7f3-4ff869c8de33**

Ele é a fonte da verdade. `WebFetch` funciona nessa URL. **Leia a seção da fase que você vai
executar antes de escrever qualquer código**; aqui embaixo fica só o resumo, para você se
localizar rápido.

O artifact da rodada anterior (desbloqueio, backups fora da pasta do cofre, edição de cofre —
fases 1 a 6, todas concluídas) continua em
https://claude.ai/code/artifact/00e12b9d-b9d9-4c72-9a4e-e111477c329d, só como histórico.

### Objetivo da rodada

Duas frentes, vindas do uso real:

1. **Backups versionados.** Hoje o `FileSystemVaultBackupStore.Store` monta sempre o mesmo caminho
   por cofre (`BKP - <nome>.GDSBX.bak`) e grava por cima: existe **um único** backup, o de antes do
   último save. Passar a guardar histórico, com um limite configurável **por cofre** — "até N
   versões" **ou** "até N dias" — e poda automática ao passar do limite.
2. **Primeiro acesso guiado.** O app não se explica para quem nunca o viu. Adicionar um tutorial de
   3 slides na tela inicial, um "?" clicável ao lado das funcionalidades que não se explicam
   sozinhas, e sinalização de campo obrigatório nos formulários.

A frente 1 vem primeiro porque cria o bloco de configuração `BACKUPS` que a frente 2 precisa
explicar — na ordem inversa, as mesmas telas seriam reabertas duas vezes.

### Status das fases

| # | Fase | Frente | Depende de | Status | PR |
|---|------|--------|------------|--------|-----|
| 0 | Contexto e plano (este arquivo + artifact) | — | — | ✅ Concluída | — |
| 1 | Retenção no domínio e no store | Backups | — | ✅ Concluída | [#19](https://github.com/betinn/GDSB.MAUI/pull/19) |
| 2 | Bloco BACKUPS nas telas | Backups | 1 | ✅ Concluída | [#19](https://github.com/betinn/GDSB.MAUI/pull/19) |
| 3 | Infra de ajuda e obrigatoriedade | Onboarding | — | ⬜ A fazer | — |
| 4 | Tutorial de primeiro acesso | Onboarding | 3 | ⬜ A fazer | — |
| 5 | Cofre novo e segredo novo | Onboarding | 2, 3 | ⬜ A fazer | — |
| 6 | Backup e edição do cofre | Onboarding | 2, 3 | ⬜ A fazer | — |
| 7 | Fechamento (README, contexto, testes, build) | — | todas | ⬜ A fazer | — |

Dependências:

```
1 ──► 2 ──┐
          ├──► 5, 6 ──► 7
3 ──► 4 ──┘
```

As fases **1** e **3** não dependem de nada e podem ser feitas em paralelo, em sessões diferentes.
As fases 5 e 6 só abrem depois de 2 e 3, porque as duas colocam um "?" na sessão `BACKUPS`.

**Exceção já usada:** as fases 1 e 2 foram implementadas e mergeadas juntas no PR #19, a pedido
explícito do usuário ("elas se complementam"). A regra de "um PR por fase" (seção "Como
trabalhar") continua valendo por padrão — só quebre de novo se o usuário pedir.

### Decisões já fechadas com o usuário

Não relitigar durante a implementação — o detalhamento de cada uma está no artifact.

- Retenção com **dois modos, um por vez**: "até N versões" ou "até N dias". Configuração **por
  cofre**, gravada dentro do arquivo (`Profile.Settings`), como já são as proteções.
- Defaults: `Count` / 10 versões / 5 dias. Um cofre v2 antigo, sem a chave no JSON, abre nesses
  valores pelo inicializador de propriedade — mesmo mecanismo já usado em `VaultSettings`.
- Além do modo escolhido existe um **teto rígido de 100 arquivos por cofre**, válido nos dois
  modos. Foi a opção escolhida em vez de uma janela mínima entre backups.
- **Piso:** a poda nunca apaga o backup mais recente, mesmo que a regra de idade mande apagar
  todos.
- Backups `LegacyV1` **nunca são podados** e não contam para o teto; continuam com nome sem
  timestamp (é a identidade por caminho que garante "nunca sobrescrever o original importado").
- Backups `Rolling` passam a ter timestamp no nome:
  `BKP - <nome>.GDSBX - 2026-09-02 14-30-12.bak` (`yyyy-MM-dd HH-mm-ss`, sem `:` — ilegal no
  Windows).
- O "?" abre um **painel no estilo do app** (overlay escuro, mesmo padrão dos três modais de
  `BackupRecoveryPage`), não um `DisplayAlert` nativo. Todo o texto de ajuda vive num catálogo em
  C# (`HelpTopics`), fora do XAML.
- **Todo painel de ajuda mostra o controle de que fala**, não só descreve: entre os parágrafos
  entra uma réplica inerte do botão, campo ou chip em questão, montada com os estilos de verdade de
  `Styles.xaml`. Por isso um tópico é uma lista de blocos (`Heading` / `Text` / `Visual`), não uma
  lista de parágrafos. Painel só de texto é considerado incompleto — a regra é garantida por teste
  na fase 3 ("todo tópico tem pelo menos um bloco `Visual`").
- **A tela de backup tem um "?" só**, no cabeçalho, e ele cobre a tela inteira (backup automático,
  restaurar, excluir, excluir todos). Nada de "?" nos cartões nem nos botões. A tela de edição do
  cofre **continua com um "?" por sessão** — a regra do "?" único vale só para a tela de backup.
- Campo obrigatório = **asterisco no label** em cor de destaque + legenda `* campos obrigatórios`
  no fim do bloco. Campos opcionais não recebem marca nenhuma.
- O tutorial aparece sozinho no primeiro acesso e fica revisível por um link **"Como funciona?" no
  topo** da tela inicial — acima do bloco de desbloqueio, não junto dos links de rodapé.
- O tutorial **não abre automaticamente quando a biometria está armada** (`CanUseBiometric`), para
  não brigar com o prompt do sistema que `UnlockViewModel.InitializeAsync` já dispara.

---

## Como trabalhar

- **Uma branch por fase, sempre nomeada `fase-<N>-<nome-curto>`** (ex.: `fase-1-retencao-backups`),
  criada a partir da `main`. Vale mesmo quando a sessão já vier com uma branch designada genérica
  (tipo `claude/...`): crie a branch da fase a partir dela (ou da `main`) e faça o PR a partir da
  branch com nome de fase, não da genérica.
- **Um PR por fase.** Use a lista de arquivos e o "Pronto quando" da fase, no artifact, como
  checklist do PR.
- Ao terminar a implementação da fase (código pronto e commitado), **abra o PR automaticamente, sem
  perguntar antes** — é parte padrão do fluxo. O PR fica aberto para review do usuário; não faça
  merge sozinho.
- O PR nunca é "mudo": a descrição sempre explica o que mudou e por quê.
- **Não fique monitorando o PR** depois de aberto — gasta token à toa. Se precisar de ajuste, o
  usuário avisa.
- Não pule fases nem inverta as dependências listadas acima.

### SonarCloud

O repositório roda o SonarCloud Code Analysis como check automático em todo push pro PR (não é um
step do workflow do GitHub Actions — é uma integração via GitHub App, sem log acessível por aqui).
Depois de qualquer push, confira o check antes de considerar a fase pronta: a Quality Gate pode
passar (sem bloquear o merge) mesmo com **New Issues** abertas — "0 New issues" é o alvo, não só
"Quality Gate passed". Para ver a lista, é preciso pedir pro usuário colar o conteúdo da aba
"Issues" do PR no SonarCloud (`sonarcloud.io` está bloqueado pela rede deste ambiente, tanto por
`WebFetch` quanto por `curl`/API, mesmo com token — confirmado de novo nesta sessão) — a mensagem
do check só traz a contagem, não o detalhe.

Na rodada anterior, os 75 issues abertos do relatório de 2026-09-01 (2 vulnerabilidades, 1 bug,
72 code smells) foram corrigidos junto com as fases 5 e 6, no PR #17. Como o Sonar continua
inacessível daqui, o "0 issues" em New Code e Overall Code nunca chegou a ser confirmado por uma
sessão; se isso importar antes de abrir a rodada nova, precisa de confirmação humana olhando o
SonarCloud direto.

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

## Como manter este arquivo

Ao final de cada fase (PR aberto):

1. Atualize a tabela "Status das fases" acima (status e link do PR).
2. Se algo do plano mudou no caminho, atualize também o artifact do plano — dá para republicar um
   artifact de outra sessão passando a mesma URL (veja a doc de artifacts do Claude Code:
   "Update an artifact from an earlier conversation").
3. Faça commit deste arquivo junto com o PR da fase.
