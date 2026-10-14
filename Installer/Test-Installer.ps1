[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InstallerPath,
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$InstallerPath = (Resolve-Path -LiteralPath $InstallerPath).Path
$uninstallKeyName = '{B433FB75-75A1-40AF-A543-E33D11332D28}_is1'
foreach ($registryRoot in @('HKCU:', 'HKLM:')) {
    foreach ($registryPath in @('Software\Microsoft\Windows\CurrentVersion\Uninstall',
        'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall')) {
        if (Test-Path -LiteralPath "$registryRoot\$registryPath\$uninstallKeyName") {
            throw 'PorksLauncher is already installed. Run this installer test on a clean machine to avoid changing that installation.'
        }
    }
}
if ($VerifyOnly) {
    Write-Host 'PASS: No PorksLauncher installation remains registered.'
    return
}
$frameworkRelease = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full').Release
if ($frameworkRelease -lt 461808) { throw 'This test requires .NET Framework 4.7.2 or later already installed.' }

& dotnet build (Join-Path $repositoryRoot 'Tests\PorksLauncher.StorageTests.csproj') --configuration Debug --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Regression harness build failed.' }

$installTestPath = Join-Path $repositoryRoot ('bin\installer\test-install-' + [Guid]::NewGuid().ToString('N'))
$installLogPath = "$installTestPath.log"
$uninstallerPath = Join-Path $installTestPath 'unins000.exe'
try {
    $setupProcess = Start-Process -FilePath $InstallerPath -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES',
        '/NORESTART', '/NOICONS', "/DIR=`"$installTestPath`"", "/LOG=`"$installLogPath`"" -WindowStyle Hidden -PassThru -Wait
    if ($setupProcess.ExitCode -ne 0) { throw "Setup failed with exit code $($setupProcess.ExitCode). See $installLogPath" }
    foreach ($requiredFile in @('PorksLauncher.exe', 'PorksLauncher.exe.config', 'Microsoft.Data.Sqlite.dll',
        'runtimes\win-x86\native\e_sqlite3.dll', 'runtimes\win-x64\native\e_sqlite3.dll', 'unins000.exe')) {
        if (-not (Test-Path -LiteralPath (Join-Path $installTestPath $requiredFile))) { throw "Installation is missing $requiredFile" }
    }

    # Run the existing checks beside the installed app, so all app dependencies
    # resolve from the installer output. Test databases stay in this test folder.
    $harnessPath = Join-Path $repositoryRoot 'Tests\bin\Debug\net472'
    Copy-Item -LiteralPath (Join-Path $harnessPath 'PorksLauncher.StorageTests.exe'),
        (Join-Path $harnessPath 'PorksLauncher.StorageTests.exe.config') -Destination $installTestPath
    & (Join-Path $installTestPath 'PorksLauncher.StorageTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Installed app failed regression checks.' }
} finally {
    if (Test-Path -LiteralPath $uninstallerPath) {
        $uninstallProcess = Start-Process -FilePath $uninstallerPath -ArgumentList '/VERYSILENT',
            '/SUPPRESSMSGBOXES', '/NORESTART' -WindowStyle Hidden -PassThru -Wait
        if ($uninstallProcess.ExitCode -ne 0) { throw "Test uninstall failed with exit code $($uninstallProcess.ExitCode)." }
    }
}
if (Test-Path -LiteralPath (Join-Path $installTestPath 'PorksLauncher.exe')) { throw 'Uninstall left the installed app behind.' }
Write-Host 'PASS: Installer deployment, installed-app regression checks, and uninstall.'
