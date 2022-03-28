$pluginSrcDir = '../FileFlowsPlugins'

# Create Server Plugins Folder
$FolderName = "deploy/Plugins"
if (!(Test-Path -Path $FolderName)) {
    #PowerShell Create directory if not exists
    New-Item $FolderName -ItemType Directory
    Write-Host "Folder Created successfully"
}

# build plugin
# build 0.0.1.0 so included one is always greater
dotnet build Plugin\Plugin.csproj /p:WarningLevel=1 --configuration Release  /p:AssemblyVersion=0.0.1.0 /p:Version=0.0.1.0 /p:CopyRight=$copyright --output $pluginSrcDir
Remove-Item ../../FileFlowsPlugins/FileFlows.Plugin.deps.json -ErrorAction SilentlyContinue

dotnet publish PluginInfoGenerator\PluginInfoGenerator.csproj --configuration Release --output $pluginSrcDir/build/utils/PluginInfoGenerator

Push-Location ../FileFlowsPlugins/build
./buildplugins.ps1 "../../FileFlows/$FolderName"
Pop-Location