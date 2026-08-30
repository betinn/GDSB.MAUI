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
| 1 | Fundação de criptografia nova (Domain + Infrastructure, AES-256-GCM + PBKDF2/Argon2id, formato v2) | 🚧 PR aberto: [#4](https://github.com/betinn/GDSB.MAUI/pull/4) |
| 2 | Leitor legado (v1) + migração automática ao salvar | Planejada |
| 3 | Refactor MVVM + nova UI (Android primeiro) + breakpoint responsivo | Planejada |
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

Fase 0 concluída (diagnóstico, decisões 1-7, protótipo e planos publicados). Fase 1 em andamento na branch `fase-1-criptografia-nova`: implementados `IFileCryptoServiceV2`, `InvalidPasswordOrCorruptFileException`, `GdsbFileHeader` (layout v2) e `AesGcmFileCryptoService` (PBKDF2-HMAC-SHA256 + AES-GCM), com o projeto de testes `tests/GDSB.Infrastructure.Tests` cobrindo round-trip, senha errada, ciphertext adulterado e salt/nonce distintos por chamada. Nenhuma classe da `GDSB.MAUI` foi tocada. PR aberto para review: https://github.com/betinn/GDSB.MAUI/pull/4. `dotnet build` (GDSB.Domain, GDSB.Infrastructure) e `dotnet test tests/GDSB.Infrastructure.Tests` confirmados nesta sessão: build limpo (só um warning pré-existente no `FileDecryptionService.cs` legado) e 4/4 testes passando. Falta apenas review/merge do PR.
