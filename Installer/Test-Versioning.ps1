$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Versioning.ps1')

function Assert-VersionPlan([string]$CurrentVersion, [string]$RequestedVersion, [string]$ExpectedRelease, [string]$ExpectedAssembly) {
    $source = '// [assembly: AssemblyVersion("1.0.*")]' + "`r`n" +
        '[assembly: AssemblyVersion("' + $CurrentVersion + '")]' + "`r`n" +
        '[assembly: AssemblyFileVersion("' + $CurrentVersion + '")]'
    $plan = Get-InstallerVersionPlan -AssemblyInfoContent $source -RequestedVersion $RequestedVersion
    if ($plan.ReleaseVersion -ne $ExpectedRelease -or $plan.AssemblyVersion -ne $ExpectedAssembly -or
        $plan.UpdatedContent -notmatch ('AssemblyVersion\("' + [regex]::Escape($ExpectedAssembly) + '"\)') -or
        $plan.UpdatedContent -notmatch ('AssemblyFileVersion\("' + [regex]::Escape($ExpectedAssembly) + '"\)')) {
        throw "Incorrect version plan for $CurrentVersion / $RequestedVersion"
    }
}

Assert-VersionPlan '1.0.0.0' '' '1.0.1' '1.0.1.0'
Assert-VersionPlan '1.0.1.0' '' '1.0.2' '1.0.2.0'
Assert-VersionPlan '2.7.9.4' '' '2.7.10' '2.7.10.0'
Assert-VersionPlan '1.0.0.0' '2.0.0' '2.0.0' '2.0.0.0'
Assert-VersionPlan '1.0.0.0' '2.3.4.5' '2.3.4.5' '2.3.4.5'

$source = '[assembly: AssemblyVersion("1.0.0.0")]' + "`r`n" + '[assembly: AssemblyFileVersion("1.0.0.0")]'
foreach ($badVersion in @('v1.0.0', '1.0', '1.0.65535')) {
    $rejected = $false
    try { Get-InstallerVersionPlan -AssemblyInfoContent $source -RequestedVersion $badVersion | Out-Null }
    catch { $rejected = $true }
    if (-not $rejected) { throw "Invalid version was accepted: $badVersion" }
}
$overflowSource = $source.Replace('1.0.0.0', '1.0.65534.0')
$overflowRejected = $false
try { Get-InstallerVersionPlan -AssemblyInfoContent $overflowSource | Out-Null }
catch { $overflowRejected = $true }
if (-not $overflowRejected) { throw 'Patch overflow was not rejected.' }
Write-Host 'PASS: Automatic patch increments, explicit versions, assembly stamps, and invalid-version handling.'
