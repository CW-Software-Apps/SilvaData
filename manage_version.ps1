# Script para Gerenciamento de Versão e Build (.NET MAUI)
# Salve como manage_version.ps1 na raiz do projeto

param (
    [Parameter(Mandatory=$false)]
    [int]$Action = 0
)

Set-Location $PSScriptRoot

# Forçar codificação UTF8 para evitar erros de acentuação no console
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['*:Encoding'] = 'utf8'

$projFile = Get-ChildItem *.csproj | Select-Object -First 1
$manifestPath = "Platforms/Android/AndroidManifest.xml"
$plistPath = "Platforms/iOS/Info.plist"

if (-not (Test-Path $projFile)) { 
    Write-Host "Erro: Arquivo .csproj nao encontrado!" -ForegroundColor Red
    exit 
}



function Get-CurrentVersion {
    $manifest = Get-Content $manifestPath -Raw
    $versionName = ([regex]'android:versionName="([^"]+)"').Match($manifest).Groups[1].Value
    $versionCode = ([regex]'android:versionCode="([^"]+)"').Match($manifest).Groups[1].Value
    return @{ Name = $versionName; Code = $versionCode }
}

function Update-VersionFiles($newName, $newCode) {
    # Atualizar AndroidManifest.xml
    $content = Get-Content $manifestPath -Raw
    $content = $content -replace 'android:versionName="[^"]+"', "android:versionName=`"$newName`""
    $content = $content -replace 'android:versionCode="[^"]+"', "android:versionCode=`"$newCode`""
    Set-Content $manifestPath $content

    # Atualizar Info.plist
    $content = Get-Content $plistPath -Raw
    $content = $content -replace '(?s)(<key>CFBundleShortVersionString</key>\s*<string>)[^<]+(</string>)', ('${1}' + $newName + '${2}')
    $content = $content -replace '(?s)(<key>CFBundleVersion</key>\s*<string>)[^<]+(</string>)', ('${1}' + $newCode + '${2}')
    Set-Content $plistPath $content

    # Atualizar .csproj
    $content = Get-Content $projFile -Raw
    $content = $content -replace '<ApplicationDisplayVersion>[^<]+</ApplicationDisplayVersion>', "<ApplicationDisplayVersion>$newName</ApplicationDisplayVersion>"
    $content = $content -replace '<ApplicationVersion>[^<]+</ApplicationVersion>', "<ApplicationVersion>$newCode</ApplicationVersion>"
    Set-Content $projFile $content
    
    Write-Host "`nSucesso! Atualizado para Versão $newName (Build $newCode)" -ForegroundColor Green
}

function Show-Menu {
    $v = Get-CurrentVersion
    
    # Se uma acao foi passada por parametro, executa direto e sai
    if ($Action -gt 0) {
        Execute-Action $Action $v
        return
    }

    Clear-Host
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " PROJETO: $($projFile.BaseName)" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Versao Atual: $($v.Name)"
    Write-Host " Build Atual:  $($v.Code)"
    Write-Host "----------------------------------------"
    Write-Host "1 - Incrementar Versao e Build Number"
    Write-Host "2 - Incrementar Build Number"
    Write-Host "3 - Publish Archive for iOS"
    Write-Host "q - Sair"
    Write-Host "----------------------------------------"
    
    $choice = Read-Host "Escolha uma opcao"
    if ($choice -eq "q") { return }
    
    Execute-Action $choice $v
    Show-Menu
}

function Execute-Action($choice, $v) {
    switch ($choice) {
        "1" {
            $parts = $v.Name -split '\.'
            if ($parts.Length -ge 1) {
                $parts[-1] = [int]$parts[-1] + 1
                $newName = $parts -join '.'
                $newCode = [int]$v.Code + 1
                Update-VersionFiles $newName $newCode
            }
            if ($Action -eq 0) { Read-Host "`nPressione Enter para continuar..." }
        }
        "2" {
            $newCode = [int]$v.Code + 1
            Update-VersionFiles $v.Name $newCode
            if ($Action -eq 0) { Read-Host "`nPressione Enter para continuar..." }
        }
        "3" {
            Write-Host "`nIniciando Publish para iOS (Release)..." -ForegroundColor Yellow
            $cmd = "dotnet publish -f net10.0-ios -c Release /p:ArchiveOnBuild=true"
            
            Write-Host "Executando: $cmd" -ForegroundColor Gray
            Invoke-Expression $cmd
            if ($Action -eq 0) { Read-Host "`nPressione Enter para continuar..." }
        }
        default { 
            if ($Action -eq 0) {
                Write-Host "Opcao invalida!" -ForegroundColor Red
                Start-Sleep -Seconds 1
            }
        }
    }
}

Show-Menu
