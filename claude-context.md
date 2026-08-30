# Contexto do projeto — GDSB.MAUI

> Este arquivo existe para que qualquer sessão de Claude Code (claude.ai/code) tenha, desde o primeiro prompt, todo o contexto combinado com o usuário — sem precisar reexplicar nada. Mantenha-o atualizado: veja "Como manter este arquivo" no final.

## O que é o GDSB

App de gerenciamento de senhas: guarda credenciais criptografadas em um arquivo local `.GDSBX`. Existe uma versão desktop mais completa (tem insert/update). A versão MAUI (celular/tablet) hoje só abre e decifra arquivos existentes — não tem insert, update, delete ou save implementados. O usuário usa bastante em um tablet grande (Samsung S10 Ultra), então comportamento responsivo é requisito real, não opcional.

Estrutura da solução:
- `src/GDSB.Domain` — entidades (`Profile`, `SecretBox`) e interfaces.
- `src/GDSB.Infrastructure` — implementação da criptografia (`Encryption/FileDecryptionService.cs` é o código atual, fraco — ver diagnóstico abaixo).
- `src/GDSB.MAUI` — o app mobile em si (Views, Services de plataforma para file picker).

## Diagnóstico da criptografia atual (não é aproveitável)

Localização: `src/GDSB.Infrastructure/Encryption/FileDecryptionService.cs`.

- **IV fixo hardcoded no código-fonte**, reaproveitado em toda criptografia já feita pelo app.
- **Sem KDF real**: a "chave" é a senha em ASCII repetida ciclicamente até 32 bytes (`GetPasswordStringIntoByte`) — sem salt, sem iterações. Brute force offline é trivial, e senhas iguais em arquivos diferentes geram a mesma chave.
- `Encoding.ASCII` corrompe silenciosamente acentos (comuns em senha PT-BR), colapsando senhas diferentes na mesma chave.
- **Sem autenticação/integridade** (AES-CBC puro, sem HMAC/AEAD) — um arquivo adulterado não é detectado.
- A "dupla camada" de AES existente (chave/IV interno guardados junto do texto cifrado) não agrega segurança real, porque ambos ficam protegidos só pela camada externa, que é a fraca.

Conclusão já validada com o usuário: **não é para ajustar, é para reescrever** — mantendo um leitor legado só para importar arquivos antigos (ver Fase 2).

## Decisões fechadas com o usuário

1. **Desktop**: será refeito depois, como projeto separado. O MAUI não fica preso ao formato de arquivo fraco de hoje — só mantém um leitor "legacy" isolado para importar `.GDSBX` v1, migrando para o novo formato ao salvar.
2. **Nível de segurança**: uso real no dia a dia (não é só estudo). Vale KDF forte, auto-lock, limpeza de clipboard e biometria.
3. **Arquitetura**: MVVM completo com `CommunityToolkit.Mvvm`, substituindo a lógica hoje presa em code-behind.
4. **Plataforma prioritária**: Android primeiro, para entregar o CRUD completo de ponta a ponta.
5. **Visual**: tema escuro reaproveitando os tokens já existentes em `Resources/Styles/Colors.xaml` (`Primary #512BD4`, `PrimaryDark #ac99ea`, `Tertiary #2B0B98`, `MidnightBlue`, `Gray950`/`OffBlack`) + fonte Open Sans (já empacotada no app, em `Resources/Fonts`). Cores novas aprovadas: dourado `#F5B93E` (favorito) e vermelho `#E5484D` (ações destrutivas). Existe um protótipo interativo aprovado (link abaixo) — a implementação deve seguir esse visual, não reinventar.
6. **Responsivo**: breakpoint por **largura de tela**, não por tipo de dispositivo (`DeviceIdiom`). Abaixo de ~700-800dp: lista + bottom-sheet (celular). Acima disso: mestre-detalhe com painel lateral (tablet) — o mesmo formulário de item é reaproveitado nos dois layouts, só muda o container ao redor dele.
7. **Biometria**: item firme (não mais opcional) da Fase 5. A senha mestra continua sendo a única fonte da chave de criptografia — a biometria apenas destrava, via Android Keystore / iOS Keychain, uma cópia já derivada dessa chave. Senha mestra sempre disponível como fallback. Amarrado ao último cofre aberto (não precisa de seleção de perfil — uso real é um perfil único por dispositivo).

## Documentos de referência

Estes três links têm todo o detalhe que não está reproduzido aqui — leia-os antes de começar cada fase (`WebFetch` funciona nessas URLs, mesmo sendo `claude.ai/code/artifact/...`):

- **Protótipo visual interativo** (celular + tablet, 5 telas, aprovado): https://claude.ai/code/artifact/a754f86c-2575-4697-9956-691ee5a884f9
- **Roadmap macro** (visão das 7 fases com status): https://claude.ai/code/artifact/0e94da44-68c8-45b3-8cb5-0a6a7ea33026
- **Plano de execução detalhado** (arquivos a criar/editar, decisões técnicas como o layout de bytes do formato v2, tarefas e critério de "pronto" — uma seção `#fase-N` por fase): https://claude.ai/code/artifact/4a2ac1b9-5602-4f30-a848-05083fc03e72

## As fases

| # | Fase | Status |
|---|------|--------|
| 0 | Diagnóstico e decisões | ✅ Concluída |
| 1 | Fundação de criptografia nova (Domain + Infrastructure, AES-256-GCM + PBKDF2/Argon2id, formato v2) | ✅ Concluída — PR [#4](https://github.com/betinn/GDSB.MAUI/pull/4) mergeado |
| 2 | Leitor legado (v1) + migração automática ao salvar | ✅ Concluída — PR [#5](https://github.com/betinn/GDSB.MAUI/pull/5) |
| 3 | Refactor MVVM + nova UI (Android primeiro) + breakpoint responsivo | ✅ Concluída — PR [#5](https://github.com/betinn/GDSB.MAUI/pull/5) |
| 4 | CRUD completo (criar cofre, insert/update/delete, save) | Planejada |
| 5 | Segurança de uso real (auto-lock, clipboard, biometria) | Planejada |
| 6 | Polimento geral (remover Newtonsoft, testes, README) | Planejada |

Fora de escopo: reescrita do app desktop para o formato v2 — projeto futuro separado.

## Como trabalhar

- Uma branch por fase, criada a partir da `main` (ex.: `fase-1-criptografia-nova`).
- Um PR por fase. Use a tabela de arquivos e o "Pronto quando" da seção correspondente no plano de execução como checklist do PR.
- Ao terminar a implementação da fase (código pronto e commitado), **abra o PR automaticamente, sem perguntar antes** — isso é parte padrão do fluxo, não uma ação que precisa de confirmação. O PR fica aberto para review do usuário; não é para fazer merge sozinho.
- Não fique monitorando o PR por conta própria depois de aberto — isso gasta token à toa. Se precisar de ajuste, o usuário avisa.
- Não pule fases: a 2 depende da 1, a 3 depende da 2 (o `IProfileFileService` unificado), a 4 depende da 3 (ViewModels prontos), a 5 e a 6 podem ser paralelas entre si depois da 4.
- Nenhuma fase deve reintroduzir o comportamento antigo de criptografia (IV fixo, senha ciclada) nem em teste nem em fallback.
- Ao abrir o PR da fase, sempre inclua um comentário (descrição do PR) explicando as atualizações feitas e o objetivo da implementação — não abrir PR "mudo", mesmo quando o título já é autoexplicativo.

## Como manter este arquivo

Ao final de cada fase (PR aberto ou mergeado):

1. Atualize a tabela "As fases" acima (status e, se quiser, o link do PR).
2. Atualize a seção "Estado atual" logo abaixo com o que mudou.
3. Atualize o **roadmap macro** e o **plano de execução** publicados nos links acima — dá para revisar e republicar um artifact de outra sessão apontando a mesma URL (veja a doc de artifacts do Claude Code: "Update an artifact from a different session: give Claude its URL"). Troque o badge da fase concluída de "Planejada"/"Próxima" para "Concluída" e avance o badge "Próxima" para a fase seguinte, nos dois documentos.
4. Faça commit deste arquivo (`claude-context.md`, raiz do repositório) junto com o PR da fase.

## Estado atual

**Fases 0, 1, 2 e 3 concluídas.** A Fase 1 foi mergeada na `main` (PR #4). As Fases 2 e 3 estão na branch `claude/projeto-fases-2-3-87afz1`, aguardando review no PR [#5](https://github.com/betinn/GDSB.MAUI/pull/5). Nessa mesma branch, depois da Fase 3, entraram também a migração net8.0→net10.0 e uma leva de correções vindas do primeiro teste manual real do app (ver abaixo) — nada disso é uma fase nova, é ambiente + bugfix/polish em cima da Fase 3.

- **Fase 1** (mergeada): `IFileCryptoServiceV2`, `InvalidPasswordOrCorruptFileException`, `GdsbFileHeader` (layout v2) e `AesGcmFileCryptoService` (PBKDF2-HMAC-SHA256 + AES-GCM).
- **Fase 2**: o decifrador antigo virou `Encryption/Legacy/LegacyV1FileDecryptionService` (marcado `[Obsolete]`, só leitura). `IProfileFileService`/`ProfileFileService` detectam o formato pelo magic `GDSB` nos 4 primeiros bytes, delegam pro leitor certo e **sempre gravam em v2**, com backup do original em `<arquivo>.v1.bak` antes da primeira sobrescrita. `ProfileOpenResult.WasLegacyFormat` dispara a migração automática logo após o open.
- **Fase 3**: DI unificado em `MauiProgram.cs` (o `ServiceProvider` estático paralelo do `App.xaml.cs` foi removido — só existe um container agora). `FileFinderDecrypter` → `UnlockPage`, `MainPage` → `VaultPage`, ambas Views finas sobre `UnlockViewModel`/`VaultViewModel` (CommunityToolkit.Mvvm 8.3.2). `ItemEditorView.xaml` é o mesmo formulário nos dois layouts. `VaultPage.OnSizeAllocated` compara a largura com `ResponsiveBreakpoints.TabletMinWidth` (700dp) e alterna lista+bottom-sheet ↔ mestre-detalhe — por largura, nunca por `DeviceIdiom`. Novos `IClipboardService`/`IAlertService`/`INavigationService` isolam as APIs estáticas do MAUI. Removidos `IFileDataService`/`FileDataService`, sem uso depois do refactor.
- **Pós-Fase-3 (mesma branch)**:
  - Migração `net8.0-*` → `net10.0-*` em **todos** os projetos (`GDSB.MAUI`, `GDSB.Domain`, `GDSB.Infrastructure`, `GDSB.Infrastructure.Tests`) — o SDK/workloads MAUI instalados pararam de suportar TFMs `net8.0-*` móveis. Detalhe na seção de build abaixo.
  - **Bug de startup corrigido**: `App.xaml.cs` injetava `AppShell` direto no construtor, então o container de DI construía `AppShell`→`UnlockPage` (hidratando o XAML dela) **antes** de `App.InitializeComponent()` mesclar `Colors.xaml`/`Styles.xaml` em `Application.Resources` → crash `StaticResource not found` no primeiro `StaticResource` do XAML (`SurfaceDark`, em `UnlockPage.xaml`). Corrigido injetando `IServiceProvider` em `App` e resolvendo `AppShell` só depois de `InitializeComponent()`. **Regra daqui pra frente**: nunca injete direto no construtor de `App`/`AppShell` uma página cujo XAML use `StaticResource` de `App.xaml` — resolva via `IServiceProvider` depois de `InitializeComponent()`.
  - Primeiro teste manual real do app (Windows) apontou 3 problemas de UX, todos corrigidos:
    1. *Botões sem cursor de mão no hover (Windows)* — `Behaviors/HoverCursor.cs` (propriedade anexada cross-platform, no-op fora do Windows) + `Platforms/Windows/CursorMappings.cs` (aplica cursor de mão em **todo** `Button` automaticamente via handler mapper, e em `Label`/`Grid` marcados com `behaviors:HoverCursor.IsHand="True"` — usado nos links de URL e na linha inteira da lista). Usa reflection pra setar `UIElement.ProtectedCursor` (é `protected` no WinUI; sem API pública do MAUI pra isso ainda — técnica documentada da comunidade, não hack frágil específico deste app).
    2. *Copiar usuário/senha sem feedback* — `VaultViewModel` dispara o evento `ToastRequested`; `VaultPage` anima um toast próprio (`Border`+`Label`, fade in/out via `FadeTo`) com "Usuário copiado"/"Senha copiada". Decisão: **não** usar `CommunityToolkit.Maui.Alerts.Toast` — a versão do pacote compatível com `net10.0` exige `Microsoft.Maui.Controls >= 10.0.60`, e o workload MAUI instalado nesta máquina está em `10.0.20`; evitar esse conflito de versão por enquanto.
    3. *Bug visual no tablet* — no layout largo, o overlay do editor em bottom-sheet (só devia existir no layout compacto) vazava atrás do painel lateral, esticado até o fim da janela, porque seu `IsVisible` checava só `IsEditorOpen` sem considerar o layout. Corrigido com `VaultViewModel.IsCompactEditorOpen` (`IsEditorOpen && IsCompactLayout`).

Testes: `dotnet test tests/GDSB.Infrastructure.Tests` → **10/10 passando** (4 da Fase 1 + 6 novos da Fase 2: leitura de um `.GDSBX` v1 fabricado pelo algoritmo legado, senha errada, flag de formato no `Open`, migração completa e idempotência do `.v1.bak`). Roda em `net10.0` desde a migração pós-Fase-3.

Build: completo (Android + iOS + MacCatalyst + Windows) limpo, 0 erros — veja a seção seguinte para preparar o ambiente Android/Linux. **Testado manualmente rodando de verdade no Windows** (não só build): abre, desbloqueia, e os 3 pontos de UX acima foram confirmados corrigidos pelo usuário. Segue faltando o teste manual num emulador/dispositivo Android de verdade.

## Como compilar o app Android neste ambiente

**O build do `GDSB.MAUI.csproj` funciona aqui** — mas exige uma preparação, porque o SDK .NET que vem do apt do Ubuntu não traz os manifests de workload do Android/MAUI e o proxy de saída bloqueia `dl.google.com` (nada de `dotnet workload install maui` nem download do Android SDK pelo caminho normal).

O script `.claude/scripts/setup-android-build.sh` resolve isso usando só hosts liberados (NuGet, arquivo do Ubuntu, `raw.githubusercontent.com`). Ele é idempotente e leva alguns minutos na primeira vez:

```bash
.claude/scripts/setup-android-build.sh

dotnet build src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true \
  -p:AndroidSdkDirectory=/opt/android-sdk \
  -p:JavaSdkDirectory=/usr/lib/jvm/java-17-openjdk-amd64
```

O que ele faz: instala JDK 17; baixa do NuGet os manifests de workload (`android`, `maui`, `ios`, `maccatalyst` — os quatro, porque o do MAUI referencia os outros); instala o workload `maui-android`; pega `zipalign`/`apksigner` do pacote `android-sdk-build-tools` do Ubuntu; e monta um Android SDK mínimo em `/opt/android-sdk` com o `android.jar` oficial da API 34 (de um espelho no GitHub, já que `dl.google.com` está bloqueado). O `aapt2` real já vem dentro do pack do workload.

`-p:GdsbAndroidOnly=true` é um opt-in adicionado ao `csproj` que restringe os TFMs ao Android: **iOS e MacCatalyst só compilam em macOS**, e sem isso o restore falha. Em Windows/macOS nada disto é necessário — a propriedade não é definida e o build normal compila todos os TFMs.

Última verificação: build Android limpo com **0 erros** (confirmado no Windows via `-p:GdsbAndroidOnly=true`, já com os arquivos pós-Fase-3 — `Behaviors/HoverCursor.cs` e `Platforms/Windows/CursorMappings.cs` inclusive, este último exclusivo de `Platforms/Windows` e portanto fora do build Android). Gera o APK assinado em `src/GDSB.MAUI/bin/Debug/net10.0-android/`. O script deste ambiente Linux (banda `10.0.100`) não foi reexecutado desde essas mudanças — vale rodar de novo antes de confiar cegamente nele.

> Nota: o projeto migrou de `net8.0-*` para `net10.0-*` em todos os projetos, `GDSB.Domain`/`GDSB.Infrastructure`/testes inclusive (o SDK/workloads MAUI instalados pararam de suportar TFMs `net8.0-*` móveis, e nesta máquina só há runtime `net10.0`). O script acima já reflete a banda `10.0.100`; se ele voltar a falhar por causa de `API_LEVEL`/`BUILD_TOOLS`, é porque o workload Android `36.1.69` pode exigir uma API level mais nova que 34 — ajustar essas constantes no script.

**O que o build cobre e o que não cobre:** ele valida compilação de C#, o XAML (o XamlC roda e falharia com chave `StaticResource` inexistente ou tipo errado), recursos Android via aapt2, os wrappers Java e o empacotamento/assinatura. Não substitui **testar no emulador** — comportamento em tela, a troca de layout celular/tablet e a migração de um `.GDSBX` v1 real continuam precisando de teste manual.
