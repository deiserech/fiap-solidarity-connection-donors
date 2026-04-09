<#
.SYNOPSIS
    Aplica migrations usando EF Core CLI (dotnet ef database update).

.DESCRIPTION
    Este script executa `dotnet ef database update` apontando para o projeto que contÃ©m o
    DbContext e para o projeto de startup. Pode aplicar uma migration especÃ­fica ou a mais
    recente se nenhum nome for informado.

.PARAMETER Migration
    Nome da migration a aplicar. Opcional - se ausente, aplica a migration mais recente.

.PARAMETER ProjectPath
    Caminho para o arquivo .csproj do projeto que contÃ©m o DbContext.

.PARAMETER StartupProjectPath
    Caminho para o arquivo .csproj do projeto de startup (API).

.PARAMETER WhatIf
    Exibe o comando que seria executado sem executÃ¡-lo.

.EXAMPLE
    .\apply-migration.ps1 -Migration 20251202_153045_Migration

.EXAMPLE
    .\apply-migration.ps1
#>

param(
    [Parameter(Mandatory=$false)][string]$Migration,
    [Parameter(Mandatory=$false)][string]$ProjectPath = "src/SolidarityConnection.Donors.Identity.Infrastructure/FiapCloudGames.Users.Infrastructure.csproj",
    [Parameter(Mandatory=$false)][string]$StartupProjectPath = "src/SolidarityConnection.Donors.Identity.Api/FiapCloudGames.Users.Api.csproj",
    [switch]$WhatIf
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
try {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..\..')).Path
} catch {
    $RepoRoot = $ScriptDir
}

if (-not [System.IO.Path]::IsPathRooted($ProjectPath)) {
    $projectCandidate = Join-Path $RepoRoot $ProjectPath
    if (Test-Path $projectCandidate) {
        $ProjectPath = (Resolve-Path $projectCandidate).Path
    } else {
        Write-Host "Project file nÃ£o encontrado: $projectCandidate" -ForegroundColor Red
        exit 4
    }
}

if (-not [System.IO.Path]::IsPathRooted($StartupProjectPath)) {
    $startupCandidate = Join-Path $RepoRoot $StartupProjectPath
    if (Test-Path $startupCandidate) {
        $StartupProjectPath = (Resolve-Path $startupCandidate).Path
    } else {
        Write-Host "Startup project nÃ£o encontrado: $startupCandidate" -ForegroundColor Red
        exit 5
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "O comando 'dotnet' nÃ£o foi encontrado. Instale o .NET SDK e certifique-se de que 'dotnet' estÃ¡ no PATH." -ForegroundColor Red
    exit 2
}

Write-Host "Aplicando migration" -ForegroundColor Green

if (-not $Migration) {
    Write-Host "Nenhuma migration informada - buscando a migration mais recente..." -ForegroundColor Yellow
    $dotnetEfTokens = @('ef', 'migrations', 'list', '--project', $ProjectPath, '--startup-project', $StartupProjectPath)
    try {
        $output = & dotnet @dotnetEfTokens 2>&1
    } catch {
        Write-Host ("Erro ao listar migrations: {0}" -f $_) -ForegroundColor Red
        exit 6
    }

    $lines = $output -split "\r?\n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -and ($_ -notmatch '^\s*info:') -and ($_ -notmatch '^\s*warn:') }
    if (-not $lines -or $lines.Count -eq 0) {
        Write-Host "Nenhuma migration encontrada." -ForegroundColor Red
        exit 7
    }

    $latest = $lines | Select-Object -Last 1
    if ($latest -match 'No migrations were found') {
        Write-Host "Nenhuma migration encontrada." -ForegroundColor Red
        exit 7
    }

    $cleanMigration = $latest -replace '\s*\(.*\)$',''
    $Migration = $cleanMigration
    Write-Host "Migration selecionada: $Migration" -ForegroundColor DarkCyan
} else {
    Write-Host "Migration: $Migration" -ForegroundColor DarkCyan
}

Write-Host "Project: $ProjectPath" -ForegroundColor DarkCyan
Write-Host "StartupProject: $StartupProjectPath" -ForegroundColor DarkCyan

if ($WhatIf) {
    if ($Migration) {
        $cmd = "dotnet ef database update `"$Migration`" --project `"$ProjectPath`" --startup-project `"$StartupProjectPath`""
    } else {
        $cmd = "dotnet ef database update --project `"$ProjectPath`" --startup-project `"$StartupProjectPath`""
    }
    Write-Host "Comando (what-if):" -ForegroundColor Yellow
    Write-Host $cmd -ForegroundColor Gray
    exit 0
}

try {
    if ($Migration) {
        $dotnetEfTokens = @('ef', 'database', 'update', $Migration, '--project', $ProjectPath, '--startup-project', $StartupProjectPath)
    } else {
        $dotnetEfTokens = @('ef', 'database', 'update', '--project', $ProjectPath, '--startup-project', $StartupProjectPath)
    }

    & dotnet @dotnetEfTokens
    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet ef retornou cÃ³digo $LASTEXITCODE." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Migration aplicada com sucesso." -ForegroundColor Green
} catch {
    Write-Host ("Erro ao executar dotnet ef: {0}" -f $_) -ForegroundColor Red
    exit 3
}

