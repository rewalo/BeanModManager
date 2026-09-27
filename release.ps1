param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

$version = $Version.TrimStart("v", "V")
$tag = "v$version"
$notesFile = "RELEASE_NOTES_$version.md"

# Make sure there aren't any unrelated changes.
$allowed = "Properties/AssemblyInfo.cs|Setup/Setup.csproj|$($notesFile -replace '\.', '\\.')"
$dirty = git status --porcelain | Where-Object {
    $_ -notmatch $allowed
}

if ($dirty) {
    Write-Error "Working tree has uncommitted changes:`n$($dirty -join "`n")`nCommit or stash them first."
    exit 1
}

if (git tag --list $tag) {
    Write-Error "Tag $tag already exists."
    exit 1
}

# Update the assembly version.
$assemblyInfo = "Properties\AssemblyInfo.cs"

if (Test-Path $assemblyInfo) {
    (Get-Content $assemblyInfo) -replace '\d+\.\d+\.\d+\.\d+', "$version.0" |
        Set-Content $assemblyInfo

    Write-Host "Updated $assemblyInfo -> $version.0"
}
else {
    Write-Warning "$assemblyInfo not found - skipping. CI will set the version during the build."
}

# Update the MSI project version.
$setupProj = "Setup\Setup.csproj"
$originalSetup = Get-Content $setupProj -Raw
$updatedSetup = $originalSetup -replace '(<Version Condition[^>]*>)[\d.]+(</Version>)', "`${1}$version`${2}"

if ($updatedSetup -eq $originalSetup) {
    Write-Warning "Could not find a version to update in $setupProj. Check the <Version> element."
}

$updatedSetup | Set-Content $setupProj
Write-Host "Updated $setupProj -> $version"

# Stage the release files.
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

Write-Host "`nPushing commit and tag $tag..."
git push
git push origin $tag

Write-Host "`nDone. Release workflow: https://github.com/rewalo/BeanModManager/actions"