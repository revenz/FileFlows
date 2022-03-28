param(
    [ValidateSet("True", "False", 0, 1)]
    [ValidateNotNullOrEmpty()]
    [string]$tar = "False"
    , [string]$disableInstaller = "False"
)

function ConvertStringToBoolean ([string]$value) {
    $value = $value.ToLower();

    switch ($value) {
        "true" { return $true; }
        "1" { return $true; }
        "false" { return $false; }
        "0" { return $false; }
    }
}
[bool]$tar_bool = ConvertStringToBoolean($tar);
[bool]$disableInstaller_bool = ConvertStringToBoolean($disableInstaller);

if ($IsWindows) {
    $dotnet_cmd = "dotnet.exe"
} else {
    $dotnet_cmd = "dotnet"
}

Write-Output "#################################"
Write-Output "###   Building Windows Node   ###"
Write-Output "#################################"

. .\build-variables.ps1
$csVersion = "string Version = ""$version"""
(Get-Content ..\Node\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File ..\Node\Globals.cs

(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<PublishSingleFile>[^<]+</PublishSingleFile>', "" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<RuntimeIdentifier>[^<]+</RuntimeIdentifier>', "" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File ..\WindowsNode\WindowsNode.csproj

& $dotnet_cmd publish ..\WindowsNode\WindowsNode.csproj /p:WarningLevel=1 --configuration Release /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright  /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625 --output ..\deploy\FileFlows-Node

if (!($disableInstaller_bool)) {
    (Get-Content installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii
    (Get-Content installers\WindowsServerInstaller\Program.cs) -replace 'Node = false', "Node = true" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii

    # build the installer
    .\build-installer.ps1 .
}


$outdir = "..\deploy\FileFlows-Node"

if (!($tar_bool)) {
    Write-Output "Creating zip file"
    $zip_file = "$outdir-$version.zip"

    if ([System.IO.File]::Exists($zip_file)) {
        Remove-Item $zip_file
    }

    $compress = @{
        Path             = "$outdir\*"
        CompressionLevel = "Optimal"
        DestinationPath  = $zip_file
    }
    Compress-Archive @compress
} else {
    Write-Output "Creating tar file"
    $tar_file = "$outdir-$version.tar.gz"

    if ([System.IO.File]::Exists($tar_file)) {
        Remove-Item $tar_file
    }

    tar -cvzf "$tar_file" -C "$outdir" *
}

Remove-Item ..\deploy\FileFlows-Node -Recurse -ErrorAction SilentlyContinue 
