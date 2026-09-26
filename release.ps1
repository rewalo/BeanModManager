# Release script: bumps version, commits, tags, and pushes so the
# GitHub Actions release workflow builds and publishes everything.
# Usage: .\release.ps1 1.6.2
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$version = $Version.TrimStart("v", "V")
$tag = "v$version"

# Must be a clean tree other than the version files we're about to touch
$dirty = git status --porcelain | Where-Object {
    $_ -notmatch "Properties/AssemblyInfo.cs|Setup/Setup.csproj"
}
if ($dirty) {
    Write-Error "Working tree has uncommitted changes:`n$($dirty -join "`n")`nCommit or stash them first."
    exit 1
}

if (git rev-parse $tag 2>$null) {
    Write-Error "Tag $tag already exists."
    exit 1
}

# Bump AssemblyInfo (gitignored, local-only file)
$assemblyInfo = "Properties\AssemblyInfo.cs"
if (Test-Path $assemblyInfo) {
    (Get-Content $assemblyInfo) -replace '\d+\.\d+\.\d+\.\d+', "$version.0" | Set-Content $assemblyInfo
    Write-Host "Updated $assemblyInfo -> $version.0"
}
else {
    Write-Warning "$assemblyInfo not found — skipping. The CI build stamps the version via msbuild anyway."
}

# Bump the MSI fallback version
$setupProj = "Setup\Setup.csproj"
(Get-Content $setupProj) -replace "<Version Condition=""'\$\(Version\)' == ''"">[\d.]+</Version>",
    "<Version Condition=""'`$(Version)' == ''"">$version</Version>" | Set-Content $setupProj
Write-Host "Updated $setupProj -> $version"

git add $setupProj
git commit -m "Bump version to $version"
git tag $tag

Write-Host "`nPushing commit + tag $tag (this triggers the release workflow)..."
git push
git push origin $tag

Write-Host "`nDone. Watch the build at: https://github.com/rewalo/BeanModManager/actions"
