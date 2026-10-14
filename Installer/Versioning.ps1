function Get-InstallerVersionPlan {
    param(
        [Parameter(Mandatory = $true)][string]$AssemblyInfoContent,
        [string]$RequestedVersion
    )

    $assemblyPattern = '(?m)^([ \t]*\[assembly:\s*AssemblyVersion\(")[^"]+("\)\])'
    $filePattern = '(?m)^([ \t]*\[assembly:\s*AssemblyFileVersion\(")(?<version>[^"]+)("\)\])'
    $fileMatches = [regex]::Matches($AssemblyInfoContent, $filePattern)
    if ($fileMatches.Count -ne 1 -or [regex]::Matches($AssemblyInfoContent, $assemblyPattern).Count -ne 1) {
        throw 'AssemblyInfo.cs must contain exactly one AssemblyVersion and one AssemblyFileVersion.'
    }

    if ($RequestedVersion) {
        if ($RequestedVersion -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Version must be numeric, for example 1.0.1.' }
        $nextVersion = [version]$RequestedVersion
        $releaseVersion = $nextVersion.ToString()
    } else {
        $currentVersion = [version]$fileMatches[0].Groups['version'].Value
        if ($currentVersion.Build -lt 0 -or $currentVersion.Build -ge 65534) {
            throw 'The patch version cannot be incremented. Specify a new major/minor version with -Version.'
        }
        $nextVersion = [version]::new($currentVersion.Major, $currentVersion.Minor, $currentVersion.Build + 1)
        $releaseVersion = $nextVersion.ToString(3)
    }

    $revision = [Math]::Max(0, $nextVersion.Revision)
    foreach ($component in @($nextVersion.Major, $nextVersion.Minor, $nextVersion.Build, $revision)) {
        if ($component -lt 0 -or $component -gt 65534) { throw 'Each assembly version component must be between 0 and 65534.' }
    }
    $assemblyVersion = '{0}.{1}.{2}.{3}' -f $nextVersion.Major, $nextVersion.Minor, $nextVersion.Build, $revision
    $updatedContent = [regex]::Replace($AssemblyInfoContent, $assemblyPattern, '${1}' + $assemblyVersion + '${2}')
    # The unnamed captures preserve the surrounding attribute syntax.
    $updatedContent = [regex]::Replace($updatedContent, $filePattern, '${1}' + $assemblyVersion + '${2}')

    [pscustomobject]@{
        ReleaseVersion = $releaseVersion
        AssemblyVersion = $assemblyVersion
        UpdatedContent = $updatedContent
    }
}
