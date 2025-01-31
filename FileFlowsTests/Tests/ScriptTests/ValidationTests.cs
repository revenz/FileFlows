// using FileFlows.Services;
//
// namespace FileFlowsTests.Tests.ScriptTests;
//
// /// <summary>
// /// Tests for validating the JS
// /// </summary>
// [TestClass]
// public class ValidationTests
// {
//     private ScriptService service = new();
//
//     [TestMethod]
//     public void TestValidScript()
//     {
//         string script = "let a = 10;";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestValidScriptWithoutSemicolon()
//     {
//         string script = "let a = 10";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestInvalidScript()
//     {
//         string script = "let a = ";
//         var result = service.ValidateScript(script);
//         Assert.IsFalse(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithSingleLineComment()
//     {
//         string script = "let a = 10; // this is a comment";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithUrlContainingDoubleSlash()
//     {
//         string script = "let url = 'http://google.com';";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithMultiLineComment()
//     {
//         string script = "let a = 10; /* this is a \n multi-line comment */ let b = 20;";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithUnterminatedMultiLineComment()
//     {
//         string script = "let a = 10; /* this is a \n multi-line comment ";
//         var result = service.ValidateScript(script);
//         Assert.IsFalse(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithFunction()
//     {
//         string script = "function test() { return 10; }";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithImportStatement()
//     {
//         string script = "import { something } from 'somewhere'; let a = 10;";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithExportClass()
//     {
//         string script = "export class Test { constructor() { this.a = 10; } }";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithTopLevelReturn()
//     {
//         string script = "return 10;";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
//     [TestMethod]
//     public void TestScriptWithStringContainingSlash()
//     {
//         string script = "let path = 'C:\\\\path\\\\to\\\\file';";
//         var result = service.ValidateScript(script);
//         Assert.IsTrue(result.Value);
//     }
//
// }