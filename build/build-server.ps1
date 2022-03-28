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
Write-Output "###      Building Server      ###"
Write-Output "#################################"

. .\build-variables.ps1

$outdir = 'deploy/FileFlows'
$csVersion = "string Version = ""$version"""
Push-Location ..\

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs
(Get-Content Server\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Server\Globals.cs
(Get-Content WindowsServer\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File WindowsServer\Globals.cs

(Get-Content Server\Server.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File Server\Server.csproj
(Get-Content Server\Server.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File Server\Server.csproj
(Get-Content Server\Server.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File Server\Server.csproj
(Get-Content Client\Client.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File Client\Client.csproj
(Get-Content Client\Client.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File Client\Client.csproj
(Get-Content Client\Client.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File Client\Client.csproj


(Get-Content Server\Globals.cs) -replace 'public static bool Demo { get; set; } = (true|false);', "public static bool Demo { get; set; } = false;" | Out-File Server\Globals.cs

(Get-Content Server\Server.csproj) -replace '<AssemblyName>[^<]+</AssemblyName>', "<AssemblyName>FileFlows.Server</AssemblyName>" | Out-File Server\Server.csproj
& $dotnet_cmd publish 'WindowsServer\WindowsServer.csproj' /p:WarningLevel=1 --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625
& $dotnet_cmd publish 'Server\Server.csproj' /p:WarningLevel=1 --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625

& $dotnet_cmd publish Client\Client.csproj --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

(Get-Content $outdir\wwwroot\index.html) -replace ' && location.hostname !== ''localhost''', '' | Out-File $outdir\wwwroot\index.html -encoding ascii
Remove-Item $outdir\wwwroot\_Framework\*.dll -Recurse -ErrorAction SilentlyContinue 
Remove-Item $outdir\wwwroot\_Framework\*.gz -Recurse -ErrorAction SilentlyContinue 
Remove-Item $outdir\wwwroot\*.scss -Recurse -ErrorAction SilentlyContinue 
Remove-Item $outdir\wwwroot\*.scss -Recurse -ErrorAction SilentlyContinue 

Remove-Item $outdir\Plugins -Recurse -ErrorAction SilentlyContinue 
if ((Test-Path deploy\plugins) -eq $true) {
    Write-Output "Copying plugins"
    Copy-Item -Path deploy\Plugins -Filter "*.*" -Recurse -Destination $outdir\Plugins -Container
}

if (!($disableInstaller_bool)) {
    (Get-Content build\installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  build\installers\WindowsServerInstaller\Program.cs -Encoding ascii
    (Get-Content build\installers\WindowsServerInstaller\Program.cs) -replace 'Node = true', "Node = false" | Out-File  build\installers\WindowsServerInstaller\Program.cs -Encoding ascii

    # build the installer
    .\build\build-installer.ps1 build
}

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

Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue 

Pop-Location
