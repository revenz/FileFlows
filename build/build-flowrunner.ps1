Write-Output "##################################"
Write-Output "###    Building Flow Runner    ###"
Write-Output "##################################"

if ($IsWindows) {
    $dotnet_cmd = "dotnet.exe"
} else {
    $dotnet_cmd = "dotnet"
}

. .\build-variables.ps1

$outdir = '../deploy/FileFlows-Runner'

(Get-Content ..\FlowRunner\FlowRunner.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File ..\FlowRunner\FlowRunner.csproj
(Get-Content ..\FlowRunner\FlowRunner.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File ..\FlowRunner\FlowRunner.csproj
(Get-Content ..\FlowRunner\FlowRunner.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File ..\FlowRunner\FlowRunner.csproj
  
& $dotnet_cmd publish '..\FlowRunner\FlowRunner.csproj' /p:WarningLevel=1 --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625

if ((Test-Path ../deploy/FileFlows) -eq $false) {
    New-Item -Path ../deploy/FileFlows -ItemType directory
}
if ((Test-Path ../deploy/FileFlows-Node) -eq $false) {
    New-Item -Path ../deploy/FileFlows-Node -ItemType directory
}

Copy-Item $outdir -Filter "*.*" -Destination "../deploy/FileFlows-Node/" -Recurse
Copy-Item $outdir -Filter "*.*" -Destination "../deploy/FileFlows/" -Recurse

Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue