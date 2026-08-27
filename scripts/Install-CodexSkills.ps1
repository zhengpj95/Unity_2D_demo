[CmdletBinding(SupportsShouldProcess)]
param(
  [string]$SkillName = "unity-mvc-development"
)

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $projectRoot ".codex\\skills\\$SkillName"
$codexDirectory = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE ".codex" }
$targetRoot = Join-Path $codexDirectory "skills"
$targetDirectory = Join-Path $targetRoot $SkillName

if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
  throw "未找到项目内 Skill：$sourceDirectory"
}

if ($PSCmdlet.ShouldProcess($targetDirectory, "安装 Codex Skill '$SkillName'")) {
  New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
  Copy-Item -LiteralPath $sourceDirectory -Destination $targetDirectory -Recurse -Force
  Write-Host "已安装 $SkillName 到 $targetDirectory"
}
