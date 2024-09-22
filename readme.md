# FileFlows Repository

This is the official repository for FileFlows.

You can create a new script and submit a pull request to get it included in the official repository


## Types of Scipts
1. Flow Scripts
These are scripts that are executed during a flow and need to adhere to a strict format.
See the official [documentation](https://fileflows.com/docs/scripting/javascript/flow-scripts/) for more information.

2. System Scripts
These scripts are scirpts that are run by the system as either scheduled tasks or a pre-execute task on a processing node.
These do not have to follow such a strict format as the the Flow scripts as these take in no inputs and produce no outputs.

3. Shared Scripts
These are scripts that can be imported by other scripts and will not directly be called by FileFlows

## Types of Templates
1. Function
These are templates that are shown to the user when they edit a [Function](https://fileflows.com/docs/plugins/basic-nodes/function) node.

## Creating a Script
Each script should be in the appropriate folder and be correctly named.

## Testing a Script
You can use the dotnet tester to test a script, which can be run like so
```
dotnet run ScriptName parameters
```

For example
```
dotnet run '.\Scripts\Flow\Hardware\NVIDIA - Below Encoder Limit.js' --EncoderLimit 2
```
```
dotnet run .\Scripts\System\DownloadClients\PauseSABNZbd.js --var:FileFlows.Url http://fileflows.lan --var:SABnzbd.Url http://sabnzbd.lan/ --var:SABnzbd.ApiKey 123456789ABCDEFGHIJKLMNOP
```

## Variables
Variables are used by System and Shared scripts to read in user configurable values, e.g. a API URL or Access Token.

The should be in the format of [Product].[Name], for example FileFlows.Url


## Dotnet Tester Command Line

| Name | Description | Used | Example |
| :---: | :---: | :---: | :---: |
| --name | A function variable to pass into a script | function Script(MyVariable) | --MyVariable "A String" |
| --var:name | A value to set in the Variables | Variables.MyVariable | --var:MyVariable "A String" |


### Examples
How to use .NET List<string>
```js
var ListOfString = System.Collections.Generic.List(System.String);
var list = new ListOfString();
for(let arg of arguments){
    list.Add('' + arg);
}
Logger.ILog('List count: ' + list.Count);
```
