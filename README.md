# GDSB

App de gerenciamento de senhas para Android/Windows: guarda credenciais criptografadas em um
arquivo local `.GDSBX`. Uma versão desktop separada oferece o mesmo formato de arquivo; esta é a
versão MAUI (celular/tablet), com layout responsivo entre os dois.

## Estrutura da solução

- `src/GDSB.Domain` — entidades (`Profile`, `SecretBox`) e interfaces de serviço.
- `src/GDSB.Infrastructure` — implementação da criptografia (v1 legado e v2) e acesso a arquivo.
- `src/GDSB.MAUI.ViewModels` — ViewModels e as abstrações de plataforma (clipboard, navegação,
  preferências, launcher) que os tornam testáveis sem o runtime do MAUI.
- `src/GDSB.MAUI` — o app em si: Views, Shell e as implementações de plataforma (Android/Windows).

## Formato de arquivo v2

Arquivos novos são gravados em AES-256-GCM com a chave derivada da senha mestra via
PBKDF2-HMAC-SHA256, com salt e nonce aleatórios a cada gravação e autenticação integrada (GCM) —
um arquivo adulterado é rejeitado, não só decifrado errado. O cabeçalho é auto-descritivo (guarda
o número de iterações do KDF e um id de algoritmo no próprio arquivo), então futuras atualizações
de recomendação de segurança não quebram a leitura de arquivos já gravados:

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

Arquivos `.GDSBX` antigos (v1) continuam abrindo normalmente: o formato é detectado automaticamente
e, ao salvar pela primeira vez, o arquivo é migrado para v2 de forma transparente, com o original
preservado como backup — nunca ao lado do cofre, ver "Backups" abaixo.

## Funcionalidades

- Criar, abrir, editar e excluir cofres e itens (usuário, senha, URL, observações, favoritos).
- **Desbloqueio:** escolher o arquivo do cofre vem antes da senha mestra — o campo de senha só
  habilita depois de um arquivo selecionado. Não se aplica ao modo com biometria ligada, onde o
  bloco manual inteiro dá lugar a um único botão mirando o último cofre aberto.
- **Backups.** Antes de cada gravação, a versão anterior do cofre é salva automaticamente num
  diretório próprio do app (`vault-backups`, dentro de `FileSystem.AppDataDirectory`) — nunca mais
  ao lado do arquivo original, nem no Windows, o que evita que o backup apareça sincronizado junto
  com o cofre em pastas do Google Drive/OneDrive. O nome usa o prefixo `BKP - ` (resolve a
  truncagem em tela pequena, que corta o fim do nome) e o sufixo `.bak`/`.v1.bak`. Uma tela de
  recuperação, alcançada pela tela de desbloqueio ("Recuperar de um backup"), lista todos os
  backups e permite restaurar um deles para um arquivo novo (o cofre original nunca é sobrescrito)
  ou excluir backups, um por vez ou todos de uma vez.
- **Edição de cofre.** Uma tela própria (ícone de engrenagem no cabeçalho do cofre) permite
  renomear o cofre e trocar a senha mestra — as duas únicas mudanças que afetam o ponto de
  desbloqueio, então, depois de gravar no arquivo atual, a tela oferece salvar também num arquivo
  novo (o original nunca é alterado sem essa confirmação). Trocar a senha com a biometria ativa
  re-sela o atalho automaticamente com a senha nova, e oferece excluir os backups antigos (que
  continuam cifrados com a senha anterior).
- **Proteções por cofre.** Limpeza automática da área de transferência e auto-lock por inatividade
  são configuráveis por cofre (gravadas dentro do próprio arquivo, em `Profile.Settings`), com
  tempos ajustáveis (20/45/90s para o clipboard; 1/2/5/15 min para o auto-lock) e a opção de
  desligar cada uma — com aviso de que desligar o auto-lock deixa o cofre aberto indefinidamente
  em segundo plano.
- Biometria opcional para desbloquear o último cofre aberto, sempre com a senha mestra disponível
  como alternativa.
- Layout responsivo: lista + bottom-sheet no celular, mestre-detalhe lado a lado no tablet.
- No Android, acesso a arquivo via Storage Access Framework, permitindo cofres sincronizados por
  provedores como Google Drive ou OneDrive.

## Build e testes

```
dotnet build src/GDSB.MAUI/GDSB.MAUI.csproj                 # Windows/macOS: todos os alvos
dotnet build src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true   # Linux: só Android

dotnet test tests/GDSB.Infrastructure.Tests/GDSB.Infrastructure.Tests.csproj
dotnet test tests/GDSB.MAUI.Tests/GDSB.MAUI.Tests.csproj
```
