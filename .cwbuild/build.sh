#!/bin/bash
set -e

# Configurações do Repositório / Dashboard
APP_ID="silvadata-android" # Corresponde ao ID cadastrado no "Nova Aplicação" no BuildsDashboard

echo "--- DIAGNÓSTICO DE AMBIENTE ---"
echo "WorkDir: $(pwd)"
echo "CW_OUT_DIR: ${CW_OUT_DIR:-NÃO DEFINIDO}"
echo "GOOGLE_PLAY_KEY_PATH: ${GOOGLE_PLAY_KEY_PATH:-NÃO DEFINIDO}"
if [ -f "$GOOGLE_PLAY_KEY_PATH" ]; then
    echo "✓ Chave do Google Play encontrada em: $GOOGLE_PLAY_KEY_PATH"
else
    echo "⚠️ Chave do Google Play NÃO encontrada."
fi
echo "-------------------------------"

# Extração de Versão e Build Number do AndroidManifest
VERSION=$(grep 'android:versionName' Platforms/Android/AndroidManifest.xml | sed 's/.*android:versionName="\([^"]*\)".*/\1/')
BUILD_NUMBER=$(grep 'android:versionCode' Platforms/Android/AndroidManifest.xml | sed 's/.*android:versionCode="\([^"]*\)".*/\1/')

echo "Versão: $VERSION"
echo "Build: $BUILD_NUMBER"

# 1. Informa ao Dashboard que iniciamos (Etapa 1: Preparando)
dotnet $CW_REPORTER_PATH init --app "$APP_ID" --version "$VERSION" --build "$BUILD_NUMBER" --out-dir "$CW_OUT_DIR"

# 2. Compilação (Etapa 3: Fazendo Build)
dotnet $CW_REPORTER_PATH update --stage "Fazendo Build" --progress 10 --detail "Compilando Android AAB (Release)..." --out-dir "$CW_OUT_DIR"

echo "Iniciando dotnet publish..."
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

echo "Verificando artefatos gerados em bin/Release/net10.0-android/:"
ls -lh bin/Release/net10.0-android/ || echo "Pasta bin não encontrada!"

# 4. Movimentação dos Artefatos para o Volume (Etapa 5: Enviando para Store/Artefatos)
dotnet $CW_REPORTER_PATH update --stage "Enviando para Store" --progress 90 --detail "Copiando pacotes para o volume de saída..." --out-dir "$CW_OUT_DIR"

TARGET_FILE="$CW_OUT_DIR/SilvaData-v${VERSION}-b${BUILD_NUMBER}.aab"
echo "Copiando para: $TARGET_FILE"

find ./bin/Release/net10.0-android -name "*Signed.aab" -exec cp -v {} "$TARGET_FILE" \;

if [ -f "$TARGET_FILE" ]; then
    echo "✅ Artefato copiado com sucesso para o volume: $(ls -lh "$TARGET_FILE")"
else
    echo "❌ Erro: Artefato assinado não foi encontrado para cópia!"
    exit 1
fi

# 5. Upload para a Store (Etapa 5: Enviando para Store)
if [ -n "$GOOGLE_PLAY_KEY_PATH" ] && [ -f "$GOOGLE_PLAY_KEY_PATH" ]; then
    echo "Iniciando upload automático para a Play Store..."
    dotnet $CW_REPORTER_PATH upload-google \
        --out-dir "$CW_OUT_DIR" \
        --package-name "com.cwsoftware.silvadata" \
        --aab-path "$TARGET_FILE" \
        --track "internal" \
        --key-path "$GOOGLE_PLAY_KEY_PATH"
else
    echo "⚠️ GOOGLE_PLAY_KEY_PATH não detectado ou arquivo não encontrado. Pulando upload para a Store."
fi

# 5. Finaliza e alerta SUCESSO no portal (Etapa 6: Completado)
dotnet $CW_REPORTER_PATH finalize --status "Sucesso" --out-dir "$CW_OUT_DIR"
