using System.Dynamic;
using FileFlows.DataLayer.Helpers;

namespace FileFlowsTests.Tests.AuditTests;

[TestClass]
public class AuditConverterTests
{
    [TestMethod]
    public void DiffTest()
    {
        Flow flowOld = new()
        {
            Name = "old flow",
            Parts = new()
            {
                new()
                {
                    Uid = new Guid("af59ab7f-df64-4a9e-883f-3c8e3483f3b6"), FlowElementUid = "FlowPart.InputFile",
                    Outputs = 1, OutputConnections = new()
                    {
                        new()
                        {
                            Output = 1,
                            Input = 1,
                            InputNode = new Guid("cac7a33d-4456-4040-821a-d1aa9ba7e2a0")
                        }
                    }
                },

                new()
                {
                    Uid = new Guid("cac7a33d-4456-4040-821a-d1aa9ba7e2a0"), FlowElementUid = "FlowPart.FileSize",
                    Outputs = 3, OutputConnections = new()
                    {
                        new()
                        {
                            Output = 1,
                            Input = 1,
                            InputNode = new Guid("58e9bf7a-fdf7-4776-9a46-26f701c090dd")
                        }
                    },
                    Model = ExpandoHelper.Create(("Property1", "Value1"), ("Property2", 123))
                },

                new()
                {
                    Uid = new Guid("58e9bf7a-fdf7-4776-9a46-26f701c090dd"), FlowElementUid = "FlowPart.DeleteFile",
                    Outputs = 2
                },

                new()
                {
                    Uid = new Guid("b6415090-bb50-4d61-b708-8cc99d5aa4a9"), FlowElementUid = "FlowPart.MoveFile",
                    Outputs = 2,
                    Model = ExpandoHelper.Create(("DestinationPath", "/mnt/move/test"), ("FileName", "{file.Original}"))
                },
                

                new()
                {
                    Uid = new Guid("b19f7c16-471b-4fb6-9073-4768ec3da22e"), FlowElementUid = "FlowPart.Geners",
                    Outputs = 2,
                    Model = ExpandoHelper.Create(("Genres Match",  new List<string>
                    {
                        "abc", "def", "ghi", "jkl", "mno", "pqr" 
                    })),
                },
            }
        };
        
        
        Flow flowNew = new()
        {
            Name = "New flow",
            Parts = new()
            {
                new()
                {
                    Uid = new Guid("af59ab7f-df64-4a9e-883f-3c8e3483f3b6"), FlowElementUid = "FlowPart.InputFile",
                    Outputs = 1, OutputConnections = new()
                    {
                        new()
                        {
                            Output = 1,
                            Input = 1,
                            InputNode = new Guid("cac7a33d-4456-4040-821a-d1aa9ba7e2a0")
                        }
                    }
                },

                new()
                {
                    Uid = new Guid("cac7a33d-4456-4040-821a-d1aa9ba7e2a0"), FlowElementUid = "FlowPart.FileSize",
                    Name = "File Is Greater than 10mb",
                    Outputs = 3, OutputConnections = new()
                    {
                        new()
                        {
                            Output = 2,
                            Input = 1,
                            InputNode = new Guid("58e9bf7a-fdf7-4776-9a46-26f701c090dd")
                        }
                    },
                    Model = ExpandoHelper.Create(("Property1", "Updated Value"), ("Property3", 123456))
                },

                new()
                {
                    Uid = new Guid("58e9bf7a-fdf7-4776-9a46-26f701c090dd"), FlowElementUid = "FlowPart.DeleteFile",
                    Outputs = 2
                },

                new()
                {
                    Uid = new Guid("b6415090-bb50-4d61-b708-8cc99d5aa4a9"), FlowElementUid = "FlowPart.MoveFile",
                    Outputs = 2,
                    Model = ExpandoHelper.Create(("DestinationPath", "/mnt/move/new-source"), ("FileName", "{file.Original}{ext}"), ("DeleteOriginal", true))
                },

                new()
                {
                    Uid = new Guid("b19f7c16-471b-4fb6-9073-4768ec3da22e"), FlowElementUid = "FlowPart.Geners",
                    Outputs = 2,
                    Model = ExpandoHelper.Create(("Genres Match",  new List<string>
                    {
                       "abc", "DeF", "ghi", "pqr", "mno", "123" 
                    })),
                },

                new()
                {
                    Uid = new Guid("511d70e0-8980-48c2-ac80-435bef09ef2e"), FlowElementUid = "FlowPart.ZipFile",
                    Outputs = 2
                },

            }
        };

        var diff = AuditValueHelper.Audit(typeof(Flow), flowNew, flowOld);
    }


    public static class ExpandoHelper
    {
        public static dynamic Create(params (string Key, object Value)[] properties)
        {
            dynamic expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;
            foreach (var (key, value) in properties)
            {
                expandoDict[key] = value;
            }

            return expando;
        }
    }
}