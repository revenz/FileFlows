using System.Dynamic;
using FileFlows.FlowRunner;
using FileFlows.FlowRunner.Helpers;

namespace FileFlowsTests.Tests.RunnerTests;

/// <summary>
/// Tests loading flow elements
/// </summary>
[TestClass]
public class LoadFlowElementTest : TestBase
{
    /// <summary>
    /// Tests properties on a flow element can be overwritten by variables
    /// </summary>
    [TestMethod]
    public void SetFieldValueTest()
    {
        var helper = new FlowHelper(new RunInstance());
        var typeTestElement = typeof(TestElement);

        var uid = Guid.NewGuid();
        dynamic Model = new ExpandoObject();
        Model.PropString = "Model String";
        Model.PropBool = false;
        Model.PropNumber = 123;
        var instance = helper.CreateFlowElementInstance(Logger, new FlowPart()
        {
            Uid = uid,
            Model = Model
        }, typeTestElement, new()
        {
            { uid + "." + nameof(TestElement.PropString), "Variable String" },
            { uid + "." + nameof(TestElement.PropString2), "Variable String 2" },
            { uid + "." + nameof(TestElement.PropBool), true },
            { uid + "." + nameof(TestElement.PropNumber), 456 },
        }) as TestElement;

        Assert.IsNotNull(instance);

        Assert.AreEqual("Variable String 2", instance.PropString2);
        Assert.AreEqual("Variable String", instance.PropString);
        Assert.AreEqual(456, instance.PropNumber);
        Assert.AreEqual(true, instance.PropBool);
    }
}

/// <summary>
/// A test element
/// </summary>
public class TestElement : Node
{
    /// <summary>
    /// Gets or sets the value of the test string value
    /// </summary>
    public string PropString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the test string value
    /// </summary>
    public string PropString2 { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value of the test number value
    /// </summary>
    public int PropNumber { get; set; }
    /// <summary>
    /// Gets or sets the value of the test bool value
    /// </summary>
    public bool PropBool { get; set; }
}