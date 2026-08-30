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
preservado em um backup `.v1.bak` ao lado.

## Funcionalidades

- Criar, abrir, editar e excluir cofres e itens (usuário, senha, URL, observações, favoritos).
- Copiar usuário/senha para a área de transferência, com limpeza automática após 20s.
- Auto-lock por inatividade (2 minutos em background) e biometria opcional para desbloquear o
  último cofre aberto, sempre com a senha mestra disponível como alternativa.
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
