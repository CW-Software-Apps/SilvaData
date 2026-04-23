#!/bin/bash
set -e

# Configurações do Repositório / Dashboard
APP_ID="silvadata-android" # Corresponde ao ID cadastrado no "Nova Aplicação" no BuildsDashboard

# Extração de Versão e Build Number do AndroidManifest
VERSION=$(grep 'android:versionName' Platforms/Android/AndroidManifest.xml | sed 's/.*android:versionName="\([^"]*\)".*/\1/')
BUILD_NUMBER=$(grep 'android:versionCode' Platforms/Android/AndroidManifest.xml | sed 's/.*android:versionCode="\([^"]*\)".*/\1/')

# 1. Informa ao Dashboard que iniciamos (Etapa 1: Preparando)
dotnet $CW_REPORTER_PATH init --app "$APP_ID" --version "$VERSION" --build "$BUILD_NUMBER" --out-dir "$CW_OUT_DIR"

# 2. Compilação (Etapa 3: Fazendo Build)
dotnet $CW_REPORTER_PATH update --stage "Fazendo Build" --progress 10 --detail "Compilando Android AAB (Release)..." --out-dir "$CW_OUT_DIR"

dotnet publish SilvaData.csproj -f net10.0-android -c Release \
  -p:TargetFrameworks=net10.0-android \
  -p:AndroidPackageFormat=aab \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
  -p:AndroidSigningKeyAlias="$KEYSTORE_ALIAS" \
  -p:AndroidSigningKeyPass="${KEYSTORE_KEY_PASS:-$KEYSTORE_PASS}" \
  -p:AndroidSigningStorePass="$KEYSTORE_PASS" \
  -p:PublishTrimmed=false \
  -p:RunAOTCompilation=false \
  -p:MaxCpuCount=1

# 3. Assinatura & Empacotamento (Etapa 4: Assinando)
dotnet $CW_REPORTER_PATH update --stage "Assinando" --progress 80 --detail "Preparando artefato para distribuição..." --out-dir "$CW_OUT_DIR"

# 4. Movimentação dos Artefatos para o Volume (Etapa 5: Enviando para Store/Artefatos)
dotnet $CW_REPORTER_PATH update --stage "Enviando para Store" --progress 90 --detail "Copiando pacotes para o volume de saída..." --out-dir "$CW_OUT_DIR"
find ./bin/Release/net10.0-android -name "*Signed.aab" -exec cp {} "$CW_OUT_DIR/SilvaData-v${VERSION}-b${BUILD_NUMBER}.aab" \;

# 5. Finaliza e alerta SUCESSO no portal (Etapa 6: Completado)
dotnet $CW_REPORTER_PATH finalize --status "Sucesso" --out-dir "$CW_OUT_DIR"
