using System.Dynamic;
using FileFlows.Server;
using FileFlows.ServerShared.FileServices;

namespace FileFlowsTests.Tests.ScriptTests;

/// <summary>
/// Tests for script parameters to ensure the bindings work correctly
/// </summary>
[TestClass]
public class ScriptParameterTests: ScriptTest
{
    /// <summary>
    /// Tests a bool parameter works
    /// </summary>
    [TestMethod]
    public void BoolParameter()
    {
        string code = @"function Script(boolTrue, boolFalse) {
Logger.ILog('BoolTrue: ' + boolTrue);
Logger.ILog('BoolTrue == true: ' + (boolTrue == true));
Logger.ILog('BoolTrue ?: ' + (boolTrue ? 'true' : 'false'));
Logger.ILog('BoolTrue!!: ' + (!!boolTrue));
Logger.ILog('BoolTrue type: ' + typeof(boolTrue));

Logger.ILog('BoolFalse: ' + boolFalse);
Logger.ILog('BoolFalse == true: ' + (boolFalse == true));
Logger.ILog('BoolFalse ?: ' + (boolFalse ? 'true' : 'false'));
Logger.ILog('BoolFalse!!: ' + (!!boolFalse));
Logger.ILog('BoolFalse type: ' + typeof(boolFalse));
return 0;
}";
        var result = ExecuteScript(code, new()
        {
            { "boolTrue", true },
            { "boolFalse", false }
        });
        Assert.IsTrue(result.Log.Contains("BoolTrue: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue == true: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue ?: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue!!: true"));
        Assert.IsTrue(result.Log.Contains("BoolTrue type: boolean"));
        
        Assert.IsTrue(result.Log.Contains("BoolFalse: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse == true: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse ?: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse!!: false"));
        Assert.IsTrue(result.Log.Contains("BoolFalse type: boolean"));
    }
    
    /// <summary>
    /// Tests a script parameters work
    /// </summary>
    [TestMethod]
    public void VariableParameters()
    {
        string code = @"function Script(boolTrue, boolFalse, myString, myNumber) {
Logger.ILog('BoolTrue: ' + boolTrue);
Logger.ILog('BoolFalse: ' + boolFalse);
Logger.ILog('myString: ' + myString);
Logger.ILog('myNumber: ' + myNumber);
return 1;
}";
        var args = new NodeParameters(null, Logger, false, null, new LocalFileService(false))
        {
            Logger = Logger,
            ScriptExecutor = new ScriptExecutor(),
            Variables = new()
        };
        string myString = Guid.NewGuid().ToString();
        int myNumber = new Random(DateTime.Now.Millisecond).Next(1, 10000000);
        var element = new ScriptNode();
        dynamic Model = new ExpandoObject();
        Model.boolTrue = true;
        Model.boolFalse = false;
        Model.myString = myString;
        Model.myNumber = myNumber;
        element.Model = Model;
        element.Script = new()
        {
            Code = code,
            Language = ScriptLanguage.JavaScript,
            Parameters =
            [
                new() { Name = "boolTrue", Type = ScriptArgumentType.Bool },
                new() { Name = "boolFalse", Type = ScriptArgumentType.Bool },
                new() { Name = "myString", Type = ScriptArgumentType.String },
                new() { Name = "myNumber", Type = ScriptArgumentType.Int },
            ]
        };
        Assert.AreEqual(1, element.Execute(args));
        Assert.IsTrue(Logger.Contains("BoolTrue: true"));
        Assert.IsTrue(Logger.Contains("BoolFalse: false"));
        Assert.IsTrue(Logger.Contains("myString: " + myString));
        Assert.IsTrue(Logger.Contains("myNumber: " + myNumber));
    }
    
    /// <summary>
    /// Tests a variable can override a script parameters
    /// </summary>
    [TestMethod]
    public void VariableParameterOverride()
    {
        string code = @"function Script(boolTrue, boolFalse, myString, myNumber) {
Logger.ILog('BoolTrue: ' + boolTrue);
Logger.ILog('BoolFalse: ' + boolFalse);
Logger.ILog('myString: ' + myString);
Logger.ILog('myNumber: ' + myNumber);
return 1;
}";
        var args = new NodeParameters(null, Logger, false, null, new LocalFileService(false))
        {
            Logger = Logger,
            ScriptExecutor = new ScriptExecutor(),
            Variables = new()
        };
        var uid = Guid.NewGuid();
        string myString = Guid.NewGuid().ToString();
        int myNumber = new Random(DateTime.Now.Millisecond).Next(1, 10000000);
        var element = new ScriptNode();
        element.Uid = uid;
        dynamic Model = new ExpandoObject();
        Model.boolTrue = true;
        Model.boolFalse = false;
        Model.myString = myString;
        Model.myNumber = myNumber;
        element.Model = Model;
        element.Script = new()
        {
            Code = code,
            Language = ScriptLanguage.JavaScript,
            Parameters =
            [
                new() { Name = "boolTrue", Type = ScriptArgumentType.Bool },
                new() { Name = "boolFalse", Type = ScriptArgumentType.Bool },
                new() { Name = "myString", Type = ScriptArgumentType.String },
                new() { Name = "myNumber", Type = ScriptArgumentType.Int },
            ]
        };
        args.Variables[$"{uid}.myString"] = "I am overriden!";
        args.Variables[$"{uid}.myNumber"] = 1337;
        args.Variables[$"{uid}.boolTrue"] = false;
        args.Variables[$"{uid}.boolFalse"] = true;
        Assert.AreEqual(1, element.Execute(args));
        Assert.IsTrue(Logger.Contains("BoolTrue: false"));
        Assert.IsTrue(Logger.Contains("BoolFalse: true"));
        Assert.IsTrue(Logger.Contains("myString: I am overriden!"));
        Assert.IsTrue(Logger.Contains("myNumber: 1337"));
    }
}