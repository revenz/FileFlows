$pluginSrcDir = '../FileFlowsPlugins'

# build plugin
# build 0.0.1.0 so included one is always greater
dotnet build Plugin\Plugin.csproj /p:WarningLevel=1 --configuration Release  /p:AssemblyVersion=0.0.1.0 /p:Version=0.0.1.0 /p:CopyRight=$copyright --output $pluginSrcDir
Remove-Item ../../FileFlowsPlugins/FileFlows.Plugin.deps.json -ErrorAction SilentlyContinue

dotnet publish PluginInfoGenerator\PluginInfoGenerator.csproj --configuration Release --output $pluginSrcDir/build/utils/PluginInfoGenerator

Push-Location ../FileFlowsPlugins/build
./buildplugins.ps1 '../../FileFlows/Server/Plugins'
Pop-Location