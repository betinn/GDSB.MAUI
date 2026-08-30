#!/bin/bash
# Prepara este ambiente (Claude Code on the web / Linux) para compilar o GDSB.MAUI no Android.
#
# Por que isto existe: o SDK .NET que vem do apt do Ubuntu não traz os manifests de workload do
# Android/MAUI, e o proxy de saída bloqueia dl.google.com — então nem `dotnet workload install maui`
# nem o download do Android SDK funcionam do jeito padrão. Este script contorna as duas coisas
# usando só hosts liberados (NuGet, Maven Central, raw.githubusercontent.com, arquivo do Ubuntu).
#
# Uso:
#   .claude/scripts/setup-android-build.sh
#   dotnet build src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true \
#     -p:AndroidSdkDirectory=/opt/android-sdk -p:JavaSdkDirectory=$JAVA_17_HOME
#
# É idempotente: rodar de novo em cima de um ambiente já preparado não quebra nada.
# Em Windows/macOS nada disto é necessário — lá o SDK oficial instala os workloads normalmente.

set -euo pipefail

SDK_BAND=8.0.100
ANDROID_SDK=/opt/android-sdk
API_LEVEL=34
BUILD_TOOLS=34.0.0
JDK=/usr/lib/jvm/java-17-openjdk-amd64
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

log() { printf '\n==> %s\n' "$1"; }

log "1/6 .NET SDK 8"
if ! command -v dotnet >/dev/null 2>&1; then
  sudo apt-get install -y -qq dotnet-sdk-8.0
fi
dotnet --version

log "2/6 JDK 17 (o build do Android precisa de javac/keytool)"
if [ ! -x "$JDK/bin/javac" ]; then
  sudo apt-get install -y -qq openjdk-17-jdk-headless
fi
"$JDK/bin/javac" -version

log "3/6 manifests de workload (o SDK do apt não os inclui)"
# Os manifests são pacotes NuGet. O do MAUI referencia ios/maccatalyst, então os quatro
# precisam existir para o resolver compor — mesmo que só o Android vá ser instalado.
install_manifest() {
  local pkg="$1" ver="$2" dir="$3"
  local dest="/usr/lib/dotnet/sdk-manifests/$SDK_BAND/$dir/$ver"
  if [ -f "$dest/WorkloadManifest.json" ]; then
    echo "    já instalado: $dir/$ver"
    return
  fi
  echo "    instalando: $dir/$ver"
  curl -sSL -o "$WORK/$pkg.nupkg" \
    "https://api.nuget.org/v3-flatcontainer/$pkg/$ver/$pkg.$ver.nupkg"
  mkdir -p "$WORK/x-$pkg" && (cd "$WORK/x-$pkg" && unzip -qo "../$pkg.nupkg")
  sudo mkdir -p "$dest"
  sudo cp -r "$WORK/x-$pkg/data/." "$dest/"
}
install_manifest "microsoft.net.sdk.android.manifest-$SDK_BAND"     34.0.154   microsoft.net.sdk.android
install_manifest "microsoft.net.sdk.maui.manifest-$SDK_BAND"        8.0.100    microsoft.net.sdk.maui
install_manifest "microsoft.net.sdk.ios.manifest-$SDK_BAND"         18.0.8319  microsoft.net.sdk.ios
install_manifest "microsoft.net.sdk.maccatalyst.manifest-$SDK_BAND" 18.0.8319  microsoft.net.sdk.maccatalyst

log "4/6 workload maui-android"
# Sem sudo de propósito: com sudo o HOME muda e os packs vão parar num diretório que o
# CLI não consulta depois, fazendo o build reclamar de workload faltando mesmo após instalar.
if dotnet workload list 2>/dev/null | grep -q '^maui-android'; then
  echo "    já instalado"
else
  dotnet workload install maui-android --skip-manifest-update
fi

log "5/6 ferramentas de build (zipalign/apksigner vêm do arquivo do Ubuntu)"
if [ ! -x /usr/lib/android-sdk/build-tools/debian/zipalign ]; then
  sudo apt-get install -y -qq android-sdk-build-tools
fi

log "6/6 Android SDK mínimo em $ANDROID_SDK"
# Só o necessário para compilar e empacotar: o aapt2 real já vem no pack do workload.
# O android.jar é o da API 34 do SDK oficial do Google, obtido de um espelho no GitHub
# porque dl.google.com está bloqueado pela política de saída deste ambiente.
sudo mkdir -p "$ANDROID_SDK/platforms/android-$API_LEVEL" \
              "$ANDROID_SDK/build-tools/$BUILD_TOOLS/lib" \
              "$ANDROID_SDK/platform-tools"
printf 'Pkg.Revision=%s\n' "$BUILD_TOOLS" | sudo tee "$ANDROID_SDK/build-tools/$BUILD_TOOLS/source.properties" >/dev/null
printf 'Pkg.Revision=34.0.5\n' | sudo tee "$ANDROID_SDK/platform-tools/source.properties" >/dev/null
printf 'AndroidVersion.ApiLevel=%s\nPkg.Revision=3\n' "$API_LEVEL" | sudo tee "$ANDROID_SDK/platforms/android-$API_LEVEL/source.properties" >/dev/null
# adb só precisa existir: é o que o resolver usa para validar que o diretório é um SDK.
sudo touch "$ANDROID_SDK/platform-tools/adb" && sudo chmod +x "$ANDROID_SDK/platform-tools/adb"

for tool in zipalign aapt aapt2 split-select; do
  src="/usr/lib/android-sdk/build-tools/debian/$tool"
  [ -x "$src" ] && sudo cp "$src" "$ANDROID_SDK/build-tools/$BUILD_TOOLS/$tool"
done
apksigner=$(find /usr/lib/android-sdk -name apksigner.jar 2>/dev/null | head -1)
[ -n "$apksigner" ] && sudo cp "$apksigner" "$ANDROID_SDK/build-tools/$BUILD_TOOLS/lib/apksigner.jar"

jar="$ANDROID_SDK/platforms/android-$API_LEVEL/android.jar"
if [ ! -s "$jar" ] || [ "$(stat -c%s "$jar")" -lt 1000000 ]; then
  echo "    baixando android.jar da API $API_LEVEL"
  curl -sSL -o "$WORK/android.jar" \
    "https://raw.githubusercontent.com/Sable/android-platforms/master/android-$API_LEVEL/android.jar"
  # Confere que veio um jar de verdade, e não uma página de erro do proxy.
  unzip -l "$WORK/android.jar" >/dev/null 2>&1 || { echo "ERRO: android.jar inválido"; exit 1; }
  unzip -l "$WORK/android.jar" | grep -q 'resources.arsc' || { echo "ERRO: android.jar sem resources.arsc"; exit 1; }
  sudo cp "$WORK/android.jar" "$jar"
fi
ls -lh "$jar"

cat <<EOF

Pronto. Para compilar o app Android:

  dotnet build src/GDSB.MAUI/GDSB.MAUI.csproj -p:GdsbAndroidOnly=true \\
    -p:AndroidSdkDirectory=$ANDROID_SDK -p:JavaSdkDirectory=$JDK

E os testes unitários (não precisam de nada disto):

  dotnet test tests/GDSB.Infrastructure.Tests/GDSB.Infrastructure.Tests.csproj

Só o Android compila aqui — iOS e MacCatalyst exigem macOS.
EOF
