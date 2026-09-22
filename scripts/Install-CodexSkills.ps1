[CmdletBinding(SupportsShouldProcess, ConfirmImpact = "Medium")]
param(
  [ValidatePattern("^[a-z0-9-]+$")]
  [string]$SkillName = "unity-mvc-development",

  # Modern Codex clients discover repository-local .codex/skills automatically.
  [string]$DestinationRoot,

  # Rebuild only the selected target skill so stale files from older installs are removed.
  [switch]$Clean
)

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $projectRoot ".codex\skills\$SkillName"

if ([string]::IsNullOrWhiteSpace($DestinationRoot)) {
  $codexDirectory = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE ".codex" }
  $DestinationRoot = Join-Path $codexDirectory "skills"
}

$sourceDirectory = [System.IO.Path]::GetFullPath($sourceDirectory)
$DestinationRoot = [System.IO.Path]::GetFullPath($DestinationRoot)
$targetDirectory = [System.IO.Path]::GetFullPath((Join-Path $DestinationRoot $SkillName))

if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
  throw "Project-local skill not found: $sourceDirectory"
}

if (-not (Test-Path -LiteralPath (Join-Path $sourceDirectory "SKILL.md") -PathType Leaf)) {
  throw "Skill entry point not found: $(Join-Path $sourceDirectory 'SKILL.md')"
}

if ($sourceDirectory.Equals($targetDirectory, [System.StringComparison]::OrdinalIgnoreCase)) {
  throw "Source and destination are identical; installation is unnecessary: $sourceDirectory"
}

if (-not $PSCmdlet.ShouldProcess($targetDirectory, "Install compatibility copy '$SkillName'")) {
  return
}

New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null

if ($Clean -and (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
  # Constrain both the parent and leaf before recursively deleting the exact target skill.
  $resolvedParent = [System.IO.Path]::GetFullPath((Split-Path -Parent $targetDirectory))
  $targetLeaf = Split-Path -Leaf $targetDirectory
  if (-not $resolvedParent.Equals($DestinationRoot, [System.StringComparison]::OrdinalIgnoreCase) -or $targetLeaf -ne $SkillName) {
    throw "Refusing to clean an unexpected target: $targetDirectory"
  }

  Remove-Item -LiteralPath $targetDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

# Copy files by relative path to avoid skill-name/skill-name nesting on repeated installs.
$sourcePrefix = $sourceDirectory.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
$sourceFiles = Get-ChildItem -LiteralPath $sourceDirectory -File -Recurse -Force
foreach ($sourceFile in $sourceFiles) {
  $relativePath = $sourceFile.FullName.Substring($sourcePrefix.Length)
  $destinationFile = Join-Path $targetDirectory $relativePath
  $destinationParent = Split-Path -Parent $destinationFile
  New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
  Copy-Item -LiteralPath $sourceFile.FullName -Destination $destinationFile -Force
}

$legacyNestedDirectory = Join-Path $targetDirectory $SkillName
if (Test-Path -LiteralPath $legacyNestedDirectory -PathType Container) {
  Write-Warning "Legacy nested directory detected: $legacyNestedDirectory. After checking for custom content, reinstall with -Clean."
}

Write-Host "Compatibility copy installed at $targetDirectory. Do not install globally when repository-local discovery is available."
