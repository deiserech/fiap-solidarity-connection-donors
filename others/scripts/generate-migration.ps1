<#
.SYNOPSIS
    Gera uma migration usando EF Core CLI e adiciona data/hora no nome.

.DESCRIPTION
    Script para simplificar a criaÃ§Ã£o de migrations com o nome prefixado pela data/hora
    (formato yyyyMMddHHmmss). Usa `dotnet ef migrations add` apontando para o projeto
    de infraestrutura (onde as migrations ficam) e para o projeto de startup (API).

.PARAMETER Name
    DescriÃ§Ã£o curta da migration (ex: AddGameTable). Opcional. Se nÃ£o for fornecido,
    o script usarÃ¡ um nome padrÃ£o baseado apenas no timestamp (ex: 20251202_153045_Migration).

.PARAMETER Project
    Caminho para o arquivo .csproj do projeto que contÃ©m o DbContext.

.PARAMETER StartupProject
    Caminho para o arquivo .csproj do projeto de startup (usualmente a API).

.PARAMETER OutputDir
    DiretÃ³rio de saÃ­da (relativo ao projeto da migration) para colocar as migrations.

.PARAMETER WhatIf
    Exibe o comando que seria executado sem executÃ¡-lo.

.EXAMPLE
    .\generate-migration.ps1 -Name AddPromotionTable

.EXAMPLE
    .\generate-migration.ps1 -Name AddPromotionTable -Project src/FiapCloudGames.Infrastructure/FiapCloudGames.Infrastructure.csproj -StartupProject src/FiapCloudGames.Api/FiapCloudGames.Api.csproj -OutputDir Migrations
#>

param(
    [Parameter(Mandatory=$false)][string]$Name,
    [Parameter(Mandatory=$false)][string]$Project = "src/SolidarityConnection.Donors.Identity.Infrastructure/FiapCloudGames.Users.Infrastructure.csproj",
    [Parameter(Mandatory=$false)][string]$StartupProject = "src/SolidarityConnection.Donors.Identity.Api/FiapCloudGames.Users.Api.csproj",
    [Parameter(Mandatory=$false)][string]$OutputDir = "Migrations",
    [switch]$WhatIf
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
try {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..\..')).Path
} catch {
    $RepoRoot = $ScriptDir
}

if (-not [System.IO.Path]::IsPathRooted($Project)) {
    $projectCandidate = Join-Path $RepoRoot $Project
    if (Test-Path $projectCandidate) {
        $Project = (Resolve-Path $projectCandidate).Path
    } else {
        Write-Host "Project file nÃ£o encontrado: $projectCandidate" -ForegroundColor Red
        exit 4
    }
}

if (-not [System.IO.Path]::IsPathRooted($StartupProject)) {
    $startupCandidate = Join-Path $RepoRoot $StartupProject
    if (Test-Path $startupCandidate) {
        $StartupProject = (Resolve-Path $startupCandidate).Path
    } else {
        Write-Host "Startup project nÃ£o encontrado: $startupCandidate" -ForegroundColor Red
        exit 5
    }
}

if (-not $Name) {
    Write-Host "Nenhum nome informado - usando nome padrÃ£o com timestamp." -ForegroundColor Yellow
    $Name = "Migration"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "O comando 'dotnet' nÃ£o foi encontrado. Instale o .NET SDK e certifique-se de que 'dotnet' estÃ¡ no PATH." -ForegroundColor Red
    exit 2
}

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$clean = $Name -replace '\s+', '_' -replace '[^0-9A-Za-z_]', ''
$migrationName = "${timestamp}_${clean}"

Write-Host "Gerando migration: $migrationName" -ForegroundColor Green
Write-Host "Project: $Project" -ForegroundColor DarkCyan
Write-Host "StartupProject: $StartupProject" -ForegroundColor DarkCyan
Write-Host "OutputDir: $OutputDir" -ForegroundColor DarkCyan

if ($WhatIf) {
    $cmd = "dotnet ef migrations add `"$migrationName`" --project `"$Project`" --startup-project `"$StartupProject`" --output-dir `"$OutputDir`""
    Write-Host "Comando (what-if):" -ForegroundColor Yellow
    Write-Host $cmd -ForegroundColor Gray
    exit 0
}

try {
    $efCommand = @('ef', 'migrations', 'add', $migrationName, '--project', $Project, '--startup-project', $StartupProject, '--output-dir', $OutputDir)
    & dotnet @efCommand
    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet ef retornou cÃ³digo $LASTEXITCODE." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Migration criada com sucesso: $migrationName" -ForegroundColor Green
} catch {
    Write-Host ("Erro ao executar dotnet ef: {0}" -f $_) -ForegroundColor Red
    exit 3
}

