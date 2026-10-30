param(
    [string]$AccountId
)

$ErrorActionPreference = 'Stop'

if (-not $AccountId) {
    $AccountId = (aws sts get-caller-identity --query Account --output text).Trim()
}

if ($AccountId -notmatch '^\d{12}$') {
    Write-Error "Usage: .\scripts\configure-account.ps1 -AccountId <12-digit AWS account id>"
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$backendDir = Join-Path $root 'backend/restaurant-backend-app'
$sdctDir = Join-Path $backendDir '.syndicate-config-restaurant-backend-app'
$oldId = '747482682250'

$files = @(
    (Join-Path $root 'infra/terraform/backend.tf'),
    (Join-Path $root 'infra/terraform/variables.tf'),
    (Join-Path $sdctDir 'syndicate.yml'),
    (Join-Path $sdctDir 'syndicate_aliases.yml')
)

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file)
    [System.IO.File]::WriteAllText($file, $content.Replace($oldId, $AccountId))
}

$syndicate = Join-Path $sdctDir 'syndicate.yml'
$projectPath = ($backendDir -replace '\\', '/')
$lines = [System.IO.File]::ReadAllLines($syndicate) | ForEach-Object {
    if ($_ -like 'deploy_target_bucket:*') { "deploy_target_bucket: `"marathon2026-restaurant-artifacts-$AccountId`"" }
    elseif ($_ -like 'project_path:*') { "project_path: `"$projectPath`"" }
    else { $_ }
}
[System.IO.File]::WriteAllLines($syndicate, $lines)

Write-Host "Account id set to $AccountId"
Write-Host "Syndicate project_path set to $projectPath"
Select-String -Path $syndicate -Pattern 'account_id|deploy_target_bucket|project_path'
