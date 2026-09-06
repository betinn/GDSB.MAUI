# Ambiente, build e verificação

## Instalar o .NET 10 SDK

O `SessionStart` hook (`.claude/hooks/setup-dotnet.sh`) já faz isso ao abrir a sessão. Manualmente:

```bash
# Linux/macOS — instala em ~/.dotnet, sem root
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$DOTNET_ROOT:$PATH"
dotnet --version   # deve imprimir 10.x
```

Neste container o caminho que funciona é o apt: `apt-get update && apt-get install -y dotnet-sdk-10.0`
(o `builds.dotnet.microsoft.com` é bloqueado pelo proxy, então o script oficial pode falhar).

```powershell
# Windows
winget install Microsoft.DotNet.SDK.10
```

## Rodar os testes (não precisa de workload nenhum)

```bash
dotnet test tests/GDSB.Infrastructure.Tests/GDSB.Infrastructure.Tests.csproj
dotnet test tests/GDSB.MAUI.Tests/GDSB.MAUI.Tests.csproj
```

## Compilar o app (aí sim precisa do workload MAUI)

```bash
dotnet workload restore src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true
dotnet build   src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true
```

No Linux, use sempre `-p:GdsbAndroidOnly=true` — sem isso o MSBuild tenta também iOS e macCatalyst.
No Windows/macOS, omitir a flag compila todos os alvos.

## Limitação de rede deste ambiente (Claude Code na web)

A política de egress bloqueia `dl.google.com` e `builds.dotnet.microsoft.com` (403 no proxy), então
**o Android SDK não pode ser instalado aqui e o alvo `net10.0-android` não compila** (para em
`XA5300`). O workload MAUI restaura normalmente e os projetos `net10.0` compilam e testam — mas **a
compilação do XAML só é validada pelo job `build-android` do CI**. Não tente contornar o bloqueio.

`sonarcloud.io` também é bloqueado (por `WebFetch`, `curl` e API, mesmo com token). O que chega até
aqui é o comentário que o `sonarqubecloud[bot]` posta no PR — esse é um comentário normal, acessível
pelas ferramentas de GitHub.

## Verificação estática que substitui o build de XAML

Rodar antes de todo push que mexa em `src/GDSB.MAUI` (o agente `verificador` executa este roteiro):

1. XML bem formado em todo arquivo tocado:
   `python3 -c "import xml.dom.minidom,sys; xml.dom.minidom.parse(sys.argv[1])" arquivo.xaml`
2. Todo `{StaticResource X}` tem `X` definido em `App.xaml` ou no `ResourceDictionary` da página.
3. Todo `x:Name` usado no code-behind existe no XAML, e vice-versa.
4. Todo `{Binding Y}` tem `Y` no ViewModel (ou no `BindingContext` do `DataTemplate`).
5. Todo id de ajuda (`HelpVisual.*`, `vault.*`) bate com o catálogo em `Help/HelpTopics.cs`.
6. Toda chave `{loc:Tr Chave}` existe em `Resources/AppStrings.resx`.

## CI

`.github/workflows/build.yml`, em todo PR para a `main`: `test` (os dois projetos xUnit),
`build-android` (ubuntu, `-p:GdsbAndroidOnly=true`) e `build-windows`. O SonarCloud roda como check
separado, via GitHub App — não é step do workflow, não tem log acessível daqui.

## Warnings de build — linha de base e trava por processo

A rodada "warnings-zero" fechou o passivo de 521 warnings distintos (dedup por código +
`arquivo:linha`) do inventário inicial em **0**. Não é `TreatWarningsAsErrors` — é processo: build ou
teste verde com warning não é "passou", nem aqui nem no CI. `verificador`, `testes` e `entrega-pr`
reportam contagem de warning por código sempre, mesmo quando o comando não falha.

Para reextrair a contagem de um run do CI (o único lugar onde `src/GDSB.MAUI`/`net10.0-android`
compila): `mcp__github__get_job_logs` com `return_content: true` nos jobs `build-android` e
`build-windows`, coletar toda linha `warning <CÓDIGO>` e deduplicar por código + `arquivo:linha`. Um
número maior que zero é regressão da rodada — trate como pendência de PR, não como ruído do
ambiente.
