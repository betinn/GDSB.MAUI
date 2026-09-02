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

O plano completo desta rodada — contexto, decisões fechadas, plano macro, plano micro por fase
(arquivos a criar/alterar) e roteiro de verificação — vive neste documento:

**➜ https://claude.ai/code/artifact/00e12b9d-b9d9-4c72-9a4e-e111477c329d**

Ele é a fonte da verdade. `WebFetch` funciona nessa URL. Leia a fase que você vai executar lá antes
de escrever qualquer código; aqui embaixo fica só o resumo de status, para você se localizar rápido.

### Objetivo da rodada

Três frentes vindas do uso real:

1. **Ordem do desbloqueio.** Hoje o app pede a senha antes de abrir o seletor de arquivo. Inverter:
   escolher o arquivo primeiro, e só então liberar o campo de senha. Não se aplica ao modo com
   biometria ligada, onde o bloco manual já some inteiro.
2. **Backups.** Hoje o Windows grava `<cofre>.GDSBX.bak` ao lado do original — numa pasta
   sincronizada isso aparece no celular e, com nome longo, a tela pequena trunca justamente o final.
   Equalizar pelo comportamento do Android (backup em diretório do app), renomear para
   `BKP - <arquivo>.bak` e dar ao app uma tela de recuperação/limpeza de backups.
3. **Edição de cofre.** Não existe como renomear o cofre nem trocar a senha mestra depois de criado,
   e as proteções (limpeza de clipboard, auto-lock) são constantes fixas. Criar uma tela de edição e
   tornar as proteções configuráveis **por perfil** (gravadas dentro do próprio arquivo do cofre).

### Status das fases

| # | Fase | Status | PR |
|---|------|--------|-----|
| 0 | Reset do contexto (este arquivo + artifact do plano) | ✅ Concluída | [#13](https://github.com/betinn/GDSB.MAUI/pull/13) |
| 1 | Desbloqueio: arquivo antes da senha | ✅ Concluída | [#15](https://github.com/betinn/GDSB.MAUI/pull/15) |
| 2 | Backups fora da pasta do cofre (`IVaultBackupStore`) | ✅ Concluída | [#15](https://github.com/betinn/GDSB.MAUI/pull/15) |
| 3 | Proteções configuráveis por cofre (`VaultSettings`) | ✅ Concluída | [#16](https://github.com/betinn/GDSB.MAUI/pull/16) |
| 4 | Tela de edição do cofre | ✅ Concluída | [#16](https://github.com/betinn/GDSB.MAUI/pull/16) |
| 5 | Recuperação de backup | ✅ Concluída | [#17](https://github.com/betinn/GDSB.MAUI/pull/17) |
| 6 | Fechamento (README, contexto, testes, build) | ✅ Concluída | [#17](https://github.com/betinn/GDSB.MAUI/pull/17) |

**Rodada concluída.** As fases 5 e 6 foram implementadas na mesma sessão que também corrigiu os 75
issues abertos do SonarCloud (ver seção própria abaixo) — as duas frentes foram pedidas juntas e
foram pro mesmo PR, a pedido explícito de quem pediu a sessão. Essa sessão também não tinha Android
SDK disponível (mesmo bloqueio de rede a `dl.google.com` das sessões anteriores); instalou o .NET 10
SDK e o workload `maui-android` via `apt` (mesmo caminho que já funcionou nas fases 3/4), as duas
suítes de teste passaram (87 testes: 20 em GDSB.Infrastructure.Tests + 67 em GDSB.MAUI.Tests), mas
`dotnet build -p:GdsbAndroidOnly=true` e o roteiro manual das fases 1, 4 e 5 continuam pendentes de
verificação num ambiente com Android SDK — ver o PR para detalhes.

Dependências: **2 antes de 5** (a tela de recuperação lê o store criado na fase 2) e **3 antes de 4**
(a tela de edição edita o `VaultSettings` criado na fase 3). A fase 1 é independente das demais.

As fases 1 e 2 foram implementadas juntas no PR #15 porque a sessão que as fez foi configurada com
uma única branch para as duas — não é o padrão daqui pra frente. Essa sessão não tinha Android SDK
disponível (rede bloqueava `dl.google.com`); o build Android e o roteiro manual da fase 1 ficaram
pendentes de verificação antes do merge — ver o PR para detalhes.

Pelo mesmo motivo (branch única configurada pra sessão), as fases 3 e 4 foram implementadas juntas
no PR #16. Essa sessão instalou o .NET 10 SDK e o workload `maui-android` via `apt` (mirror do
Ubuntu, que tem pacote `dotnet-sdk-10.0` — mais confiável aqui do que o `dotnet-install.sh`, que
esbarra no mesmo bloqueio de rede), e as duas suítes de teste passaram (65 testes). Mas o Android SDK
em si não foi instalável (mesmo bloqueio de `dl.google.com`), então o `dotnet build
-p:GdsbAndroidOnly=true` e o roteiro manual da fase 4 ficaram pendentes de verificação antes do
merge.

### Decisões já fechadas com o usuário

Não relitigar durante a implementação — o detalhamento de cada uma está no artifact.

- Backup se chama `BKP - <nome do arquivo>.bak` (prefixo resolve a truncagem; sufixo `.bak`/`.v1.bak`
  impede confusão com um cofre).
- Backup nunca mais ao lado do cofre, nem no Windows.
- Selecionar um arquivo que é backup mostra um popup **apenas informativo**, sem bloquear.
- Nova tela de recuperação na tela inicial: recriar um cofre a partir de um backup ou excluir backups.
- Ao trocar a senha mestra, oferecer também a exclusão dos backups antigos (opção do usuário).
- Renomear = nome interno (`Profile.Nome`) + oferta de "Salvar como" arquivo novo. O arquivo em
  disco nunca é renomeado.
- Na tela de edição (Fase 4), só **nome** e **senha mestra** — o que mexe no ponto de desbloqueio do
  cofre (a chave derivada e a identidade do arquivo) — oferecem gravar num arquivo novo, e só depois
  de a gravação no arquivo atual ter dado certo. Mudar as proteções grava no próprio arquivo, sem
  prompt e sem seletor.
- Trocar a senha com biometria ativa **re-sela** o atalho automaticamente com a senha nova.

---

## Como trabalhar

- **Uma branch por fase**, criada a partir da `main` (ex.: `fase-2-backup-store`).
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

Um plano gerado a partir de um relatório do SonarCloud de 2026-09-01 (75 issues: 2 vulnerabilidades,
1 bug, 72 code smells) foi executado nesta sessão, no mesmo PR das fases 5/6 — ver o PR pra a lista
completa do que foi corrigido. Como o Sonar continua inacessível daqui, o "0 issues" em New Code e
Overall Code não pôde ser confirmado por esta sessão; precisa de confirmação humana (ou de uma
sessão com rede liberada) olhando o SonarCloud direto.

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
