[CmdletBinding()]
param(
    [string]$InnoCompilerPath,
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $InnoCompilerPath) {
    $compilerCandidates = @(
        (Join-Path $repositoryRoot 'bin\installer\tools\InnoSetup\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe')
    )
    $InnoCompilerPath = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $InnoCompilerPath) {
        $compilerCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($compilerCommand) { $InnoCompilerPath = $compilerCommand.Source }
    }
}
if (-not $InnoCompilerPath -or -not (Test-Path -LiteralPath $InnoCompilerPath)) {
    throw 'Install Inno Setup 6 or 7 from https://jrsoftware.org/isdl.php, or supply -InnoCompilerPath.'
}

$installerWorkRoot = Join-Path $repositoryRoot 'bin\installer'
$publishPath = Join-Path $installerWorkRoot ('publish-' + [Guid]::NewGuid().ToString('N'))
$prerequisitePath = Join-Path $installerWorkRoot 'prerequisites'
$installerOutputPath = Join-Path $repositoryRoot 'bin\releases'
New-Item -ItemType Directory -Force -Path $publishPath, $prerequisitePath, $installerOutputPath | Out-Null

& dotnet publish (Join-Path $repositoryRoot 'PorksLauncher.csproj') --configuration Release --output $publishPath --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'README.md'), (Join-Path $repositoryRoot 'LICENSE') -Destination $publishPath

foreach ($requiredFile in @('PorksLauncher.exe', 'PorksLauncher.exe.config', 'Microsoft.Data.Sqlite.dll',
    'runtimes\win-x86\native\e_sqlite3.dll', 'runtimes\win-x64\native\e_sqlite3.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishPath $requiredFile))) {
        throw "Published application is missing $requiredFile"
    }
}

if (-not $Version) {
    $assemblyVersion = [Version]([System.Diagnostics.FileVersionInfo]::GetVersionInfo(
        (Join-Path $publishPath 'PorksLauncher.exe')).FileVersion)
    $Version = $assemblyVersion.ToString(3)
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Version must be numeric, for example 1.0.0.' }

# Bundle the official offline runtime: users do not need another download at install time.
$frameworkInstaller = Join-Path $prerequisitePath 'NDP48-x86-x64-AllOS-ENU.exe'
if (-not (Test-Path -LiteralPath $frameworkInstaller)) {
    Invoke-WebRequest -UseBasicParsing -Uri 'https://go.microsoft.com/fwlink/?linkid=2088631' -OutFile $frameworkInstaller -TimeoutSec 600
}
$frameworkSignature = Get-AuthenticodeSignature -LiteralPath $frameworkInstaller
if ($frameworkSignature.Status -ne 'Valid' -or $frameworkSignature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
    throw 'The bundled .NET Framework installer must have a valid Microsoft signature.'
}

& $InnoCompilerPath /Q "/DPublishDir=$publishPath" "/DPrerequisiteDir=$prerequisitePath" "/DAppVersion=$Version" "/DInstallerOutputDir=$installerOutputPath" (Join-Path $PSScriptRoot 'PorksLauncher.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }

$installerFile = Join-Path $installerOutputPath "PorksLauncher-Setup-$Version.exe"
if (-not (Test-Path -LiteralPath $installerFile)) { throw 'Compiler did not produce the expected installer.' }
$installerHash = Get-FileHash -LiteralPath $installerFile -Algorithm SHA256
"$($installerHash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($installerFile))" |
    Set-Content -LiteralPath "$installerFile.sha256" -Encoding ascii
Write-Host "Installer ready: $installerFile"
