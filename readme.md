# FileFlows Script Repository

This is the official script repository for FileFlows.

You can make create a new script and submit a pull request to get it included in the official repository


## Types of Scipts
1. Flow Scripts
These are scripts that are executed during a flow and need to adhere to a strict format.
See the official [documentation](https://docs.fileflows.com/scripts) for more information.

2. System Scripts
These scripts are scirpts that are run by the system as either scheduled tasks or a pre-execute task on a processing node.
These do not have to follow such a strict format as the the Flow scripts as these take in no inputs and produce no outputs.

3. Shared Scripts
These are scripts that can be imported by other scripts and will not directly be called by FileFlows

## Creating a Script
Each script should be in the appropriate folder and be correctly named.

## Testing a Script
You can use the dotnet tester to test a script, which can be run like so
```
dotnet run ScriptName parameters
```

For example
```
dotnet run '.\flow\Hardware\NVIDIA - Below Encoder Limit.js' --EncoderLimit 2
```
```
dotnet run .\System\DownloadClients\PauseSABNZbd.js --var:FileFlowsUrl http://fileflows.lan --var:SABnzbd_Url http://sabnzbd.lan/ --var:SABnzbd_ApiKey 123456789ABCDEFGHIJKLMNOP
```

## Dotnet Tester Command Line

| Name | Description | Used | Example |
| :---: | :---: | :---: | :---: |
| --name | A function variable to pass into a script | function Script(MyVariable) | --MyVariable "A String" |
| --var:name | A value to set in the Variables | Variables.MyVariable | --var:MyVariable "A String" |