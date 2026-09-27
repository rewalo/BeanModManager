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
$notesFile = "RELEASE_NOTES_$version.md"

# Must be a clean tree other than the version files we're about to touch
$allowed = "Properties/AssemblyInfo.cs|Setup/Setup.csproj|$($notesFile -replace '\.', '\\.')"
$dirty = git status --porcelain | Where-Object {
    $_ -notmatch $allowed
}
if ($dirty) {
    Write-Error "Working tree has uncommitted changes:`n$($dirty -join "`n")`nCommit or stash them first."
    exit 1
}

$existingTag = git tag --list $tag
if ($existingTag) {
    Write-Error "Tag $tag already exists."
    exit 1
}

# Bump AssemblyInfo so the built binary has the correct version
$assemblyInfo = "Properties\AssemblyInfo.cs"
if (Test-Path $assemblyInfo) {
    (Get-Content $assemblyInfo) -replace '\d+\.\d+\.\d+\.\d+', "$version.0" | Set-Content $assemblyInfo
    Write-Host "Updated $assemblyInfo -> $version.0"
}
else {
    Write-Warning "$assemblyInfo not found - skipping. The CI build stamps the version via msbuild anyway."
}

# Bump the MSI fallback version
$setupProj = "Setup\Setup.csproj"
$originalSetup = Get-Content $setupProj -Raw
$updatedSetup = $originalSetup -replace '(<Version Condition[^>]*>)[\d.]+(</Version>)', "`${1}$version`${2}"
if ($updatedSetup -eq $originalSetup) {
    Write-Warning "Could not find a version to update in $setupProj. Verify the <Version> element."
}
$updatedSetup | Set-Content $setupProj
Write-Host "Updated $setupProj -> $version"

# Stage version files and release notes if present
$filesToAdd = @($assemblyInfo, $setupProj)
if (Test-Path $notesFile) {
    $filesToAdd += $notesFile
}

git add $filesToAdd
$commitArgs = @("commit", "-m", "Bump version to $version")
if (Test-Path $notesFile) {
    $commitArgs += @("-m", "See $notesFile for release notes.")
}
& git $commitArgs
git tag $tag

Write-Host "`nPushing commit + tag $tag (this triggers the release workflow)..."
git push
git push origin $tag

Write-Host "`nDone. Watch the build at: https://github.com/rewalo/BeanModManager/actions"
