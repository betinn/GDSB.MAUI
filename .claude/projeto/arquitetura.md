# Arquitetura do GDSB

App de gerenciamento de senhas para Android e Windows: guarda credenciais criptografadas em um
arquivo local `.GDSBX`. Existe uma versão desktop separada (fora do escopo deste repositório). O
usuário usa bastante em um tablet grande (Samsung S10 Ultra) e no celular, então comportamento
responsivo é requisito real, não opcional.

## Estrutura da solução

- `src/GDSB.Domain` — entidades (`Profile`, `SecretBox`) e interfaces de serviço.
- `src/GDSB.Infrastructure` — criptografia (v2 e leitor legado v1) e acesso a arquivo.
- `src/GDSB.MAUI.ViewModels` — ViewModels e as abstrações de plataforma (clipboard, navegação,
  preferências, launcher, alertas) que os tornam testáveis sem o runtime do MAUI. É um projeto
  `net10.0` "puro": **não pode referenciar `GDSB.MAUI`** (dependência circular), por isso as rotas
  do Shell aparecem como string literal e precisam bater com os nomes registrados em
  `AppShell.xaml.cs`.
- `src/GDSB.MAUI` — o app em si: Views, Shell e as implementações de plataforma (Android/Windows).
- `tests/GDSB.Infrastructure.Tests` e `tests/GDSB.MAUI.Tests` — xUnit, com fakes escritos à mão
  (sem biblioteca de mock).

`tests/GDSB.MAUI.Tests` referencia **só** `GDSB.Domain` e `GDSB.MAUI.ViewModels`. Consequência
prática: tudo que precisa de teste tem que morar em `GDSB.MAUI.ViewModels` — nada de lógica
testável no code-behind.

## Formato de arquivo `.GDSBX`

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

## Localização

```
GDSB.MAUI.ViewModels
├── Resources/AppStrings.resx        ← pt-BR (neutro, embutido no assembly principal)
├── Resources/AppStrings.en.resx     ← inglês (assembly satélite en/)
├── Localization/AppLanguage.cs      ← código + endônimo; lista fechada de 2
├── Localization/ILocalizationService.cs · LocalizationService.cs
└── Localization/LocalizedObject.cs  ← base que reemite PropertyChanged na troca

GDSB.MAUI
└── Localization/TrExtension.cs      ← {loc:Tr Chave} no XAML → Binding para o serviço
```

O catálogo mora em `GDSB.MAUI.ViewModels` porque é o que os testes enxergam, e porque o projeto já é
o lugar dos catálogos de texto (`Help/HelpTopics.cs`). O idioma é configuração do app, gravada em
`Preferences` (`gdsb.language`) — **não** em `Profile.Settings`: um `.GDSBX` pode ser aberto em
qualquer aparelho, e o idioma é preferência de quem lê a tela, não propriedade do arquivo.

## Padrões reutilizáveis (não duplique o que já existe)

- **`GDSB.MAUI.Controls.SelectableChip`** (`src/GDSB.MAUI/Controls/SelectableChip.cs`) — subclasse
  de `Button` com a bindable `IsSelected`; aplica o destaque em C# lendo as cores de
  `Application.Current.Resources`. Substituiu dezenas de blocos `Button.Triggers` + `DataTrigger` +
  3 `Setter`, que eram a maior fonte de duplicação do projeto. **Todo chip novo usa
  `SelectableChip`, nunca mais `Button` + `DataTrigger`.**
  Cuidado: `SetDynamicResource`/`RemoveDynamicResource` são `internal` do MAUI — o controle usa
  `ClearValue` para voltar ao valor da `Style`, que precisa de `ApplyToDerivedTypes="True"`.
- **`GDSB.MAUI.Converters.EqualsConverter`** — registrado globalmente em `App.xaml`, não por página.
  Compara o valor bindado com o `ConverterParameter` (sempre string).
  Uso: `IsSelected="{Binding X, Converter={StaticResource EqualsConverter}, ConverterParameter=20}"`
  para grupo de valor único; `IsSelected="{Binding AlgumBool}"` para chip de modo.
- **`VaultProtectionsFormViewModelBase`** (`src/GDSB.MAUI.ViewModels/ViewModels/`) — carrega as
  `[ObservableProperty]`, as listas de opções `static` e os comandos `Select*` que
  `CreateVaultViewModel` e `VaultSettingsViewModel` compartilham. `SaveProtectionsAsync` e
  `CreateVaultAsync` continuam em cada ViewModel: fazem coisas diferentes com os mesmos valores.

## Armadilha estrutural

`CommandParameter` de `Button` no XAML sempre chega como `string` ao `ICommand`, não importa o tipo
declarado no C# — `RelayCommand<int>` lança `InvalidCastException` em silêncio (o clique não faz
nada, sem erro visível). Declare o método do `[RelayCommand]` com parâmetro `string` e converta
dentro (`int.Parse(...)`).
