param(
    [switch]$dev = $false
    , [switch]$tar = $false
    , [switch]$disableInstaller = $false
)

. .\build-variables.ps1

Remove-Item ../deploy/* -Recurse -Force -Exclude *.ffplugin -ErrorAction SilentlyContinue 

if ($dev -eq $false) {
    if (!(Test-Path -Path "../deploy/Plugins")) {
        Write-Error "ERROR: No plugins directory found. Please run build-plugins.ps1."
        return
    }
}

.\build-spellcheck.ps1
.\build-flowrunner.ps1
.\build-server.ps1 -tar $tar -disableInstaller $disableInstaller
.\build-node.ps1 -tar $tar -disableInstaller $disableInstaller

# no longer need plugins or flowrunner, delete them
Remove-Item ..\deploy\Plugins -Recurse -ErrorAction SilentlyContinue 