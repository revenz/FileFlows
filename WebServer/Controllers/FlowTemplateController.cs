// namespace FileFlows.WebServer.Controllers;
//
//
// /// <summary>
// /// Controller for Flow Templates
// /// </summary>
// [Route("/api/flow-template")]
// [FileFlowsAuthorize(UserRole.Flows)]
// public class FlowTemplateController : Controller
// {
//     /// <summary>
//     /// Gets all the flow templates
//     /// </summary>
//     /// <param name="type">the type of templates to get</param>
//     /// <returns>all the flow templates</returns>
//     [HttpGet]
//     public async Task<List<FlowTemplateModel>> GetAll([FromQuery] FlowType? type)
//     {
//         List<FlowTemplateModel> templates = await ServiceLoader.Load<RepositoryFlowTemplateService>().GetTemplates();
//         FlowType any = (FlowType)(-1);
//         if (type == null)
//             type = any;
//         if (type == any || type != FlowType.SubFlow)
//         {
//             if (templates.Any())
//             {
//                 var plugins = (await new PluginService().GetAllAsync()).Where(x => x.Enabled)
//                     .Select(x => x.Name.Replace(" ", string.Empty).ToLowerInvariant().Replace("nodes", string.Empty))
//                     .ToList();
//                 foreach (var template in templates)
//                 {
//                     template.MissingDependencies = template.Plugins?.Where(pl =>
//                             plugins.Contains(
//                                 pl.ToLowerInvariant().Replace(" ", string.Empty).Replace("nodes", string.Empty)) ==
//                             false)
//                         ?.ToList() ?? new ();
//                 }
//
//                 templates = templates.Union(await LocalFlows())
//                     .Where(x => x.Type == type || type == any)
//                     .ToList();
//             }
//             else
//             {
//                 templates.AddRange(BasicFallbackFlows());
//                 
//             }
//         }
//
//         if (type == any || type == FlowType.SubFlow)
//             templates.AddRange(SubFlows());
//         
//         return templates;
//     }
//
//     private List<FlowTemplateModel> SubFlows()
//     {
//         return new List<FlowTemplateModel>()
//         {
//             new FlowTemplateModel()
//             {
//                 Path = "subflow",
//                 Name = "Blank Sub Flow",
//                 Description = "A blank sub flow",
//                 Author = "FileFlows",
//                 Tags = new List<string>() { "Basic"},
//                 Revision = 1,
//                 Type = FlowType.SubFlow,
//                 MinimumVersion = "24.02.1.100"
//             }
//         };
//     }
//
//
//     /// <summary>
//     /// Basic fall back flows in case the flow templates cannot be loaded
//     /// </summary>
//     /// <returns>the fall back flows</returns>
//     private List<FlowTemplateModel> BasicFallbackFlows()
//     {
//         return new List<FlowTemplateModel>()
//         {
//             new FlowTemplateModel()
//             {
//                 Path = "default:File",
//                 Name = "File",
//                 Description = "A blank file template with a single 'Input File' input flow element.",
//                 Author = "FileFlows",
//                 Tags = new List<string>() { "Basic" },
//                 Revision = 1,
//                 Type = FlowType.Standard,
//                 MinimumVersion = "24.02.1.100"
//             }
//         };
//     }
//     
//     private async Task<List<FlowTemplateModel>> LocalFlows()
//     {
//         var flows = (await ServiceLoader.Load<FlowService>().GetAllAsync())
//             // .Where(x => x.Properties?.Fields?.Any() == true 
//             //       && string.IsNullOrWhiteSpace(x.Properties?.Author) == false
//             //       && string.IsNullOrWhiteSpace(x.Properties?.Description) == false)
//             .OrderBy(x => x.Name.ToLowerInvariant()).ToList();
//         
//         var results = new List<FlowTemplateModel>();
//         
//         foreach (var flow in flows)
//         {
//             var ftm = new FlowTemplateModel();
//             if (flow.Properties.Author == "FileFlows")
//             {
//                 ftm.Author = string.Empty;
//                 ftm.Description = string.Empty;
//             }
//             else
//             {
//                 ftm.Author = flow.Properties.Author;
//                 ftm.Description = flow.Properties.Description;
//             }
//
//             ftm.Fields = FlowFieldToTemplateField(flow);
//             ftm.Path = "local:" + flow.Uid;
//             ftm.Name = flow.Name;
//             ftm.Revision = flow.Revision;
//             ftm.Tags = flow.Properties.Tags?.ToList() ?? new ();
//             ftm.Tags.Add("Local");
//             ftm.Type = flow.Type;
//             string typeName = Translater.Instant($"Enums.{nameof(FlowType)}." + flow.Type);
//             ftm.Icon = flow.Type switch
//             {
//                 FlowType.SubFlow => "fas fa-subway",
//                 FlowType.Failure => "fas fa-exclamation-circle",
//                 _ => "fas fa-sitemap"
//             };
//             ftm.Author = typeName;
//             ftm.Tags.Add("Local:" + typeName);
//             results.Add(ftm);
//         }
//         return results;
//     }
//
//     /// <summary>
//     /// Fetches a flow template
//     /// </summary>
//     /// <param name="model">the flow to fetch</param>
//     /// <returns>the flow</returns>
//     [HttpPost]
//     public async Task<IActionResult> FetchTemplate([FromBody] FlowTemplateModel model)
//     {
//         Flow? flow;
//         if (model.Path == "subflow")
//         {
//             string json = @"{
//     ""Name"": ""Sub Flow"",
//     ""Type"": 2,
//     ""Revision"": 1,
//     ""Properties"": {
//       ""Author"": ""FileFlows"",
//       ""Fields"": [],
//       ""Variables"": {}
//     },
//     ""Parts"": [
//       {
//         ""Uid"": ""c0807c25-7d23-44d0-8a40-2485a265c75b"",
//         ""Name"": """",
//         ""FlowElementUid"": ""SubFlowInput"",
//         ""xPos"": 450,
//         ""yPos"": 50,
//         ""Icon"": ""fas fa-long-arrow-alt-down"",
//         ""Label"": """",
//         ""Inputs"": 0,
//         ""Outputs"": 1,
//         ""OutputConnections"": [],
//         ""Type"": 0
//       }
//     ]
//   }";
//             flow = JsonSerializer.Deserialize<Flow>(json);
//         }
//         else if (model.Path == "default:File")
//         {
//             string json =  @"{
//     ""Name"": ""File"",
//     ""Type"": 0,
//     ""Revision"": 2,
//     ""Properties"": {
//       ""Description"": ""A blank file template with a single \u0022Input File\u0022 input flow element."",
//       ""Tags"": [
//         ""Basic""
//       ],
//       ""Author"": ""FileFlows"",
//       ""Fields"": [],
//       ""Variables"": {}
//     },
//     ""Parts"": [
//       {
//         ""Uid"": ""c0807c25-7d23-44d0-8a40-2485a265c75b"",
//         ""Name"": """",
//         ""FlowElementUid"": ""FileFlows.BasicNodes.File.InputFile"",
//         ""xPos"": 450,
//         ""yPos"": 50,
//         ""Icon"": ""far fa-file"",
//         ""Label"": """",
//         ""Inputs"": 0,
//         ""Outputs"": 1,
//         ""OutputConnections"": [],
//         ""Type"": 0
//       }
//     ]
//   }";
//             flow = JsonSerializer.Deserialize<Flow>(json);
//         }
//         else if (model.Path.StartsWith("local:"))
//         {
//             var uid = Guid.Parse(model.Path[6..]);
//             var tFlow = await ServiceLoader.Load<FlowService>().GetByUidAsync(uid);
//             string json = JsonSerializer.Serialize(tFlow); // we serialize this so any changes we make arent on the original flow object
//             json = TemplateHelper.ReplaceOutputPathVariable(json);
//             flow = JsonSerializer.Deserialize<Flow>(json);
//         }
//         else
//         {
//             var result = await ServiceLoader.Load<RepositoryFlowTemplateService>().LoadFlowTemplate(model.Path, model.Revision);
//             if (result.Failed(out string error))
//                 return BadRequest(error);
//             flow = result.Value;
//         }
//
//         
//         model.Fields = FlowFieldToTemplateField(flow!);
//         model.Flow = flow!;
//         return Ok(model);
//     }
//     
//     /// <summary>
//     /// Converts flow fields to template fields
//     /// </summary>
//     /// <param name="flow">the flow</param>
//     /// <returns>the results</returns>
//     private List<TemplateField> FlowFieldToTemplateField(Flow flow)
//     {
//         List<TemplateField> results = new();
//         foreach (var field in flow.Properties?.Fields ?? new())
//         {
//             var tf = new TemplateField();
//             tf.Name = field.Name;
//             tf.Label = field.Name.Replace("_" , " ");
//             tf.Default = field.DefaultValue;
//             tf.Help = field.Description;
//             tf.Suffix = field.Suffix;
//             if (string.IsNullOrWhiteSpace(field.FlowElementField) == false && Regex.IsMatch(field.FlowElementField,
//                     @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.[a-zA-Z_][a-zA-Z0-9_]*$"))
//             {
//                 // this is a strong name to a field
//                 var parts = field.FlowElementField.Split('.');
//                 tf.Uid = Guid.Parse(parts[0]);
//                 tf.Name = parts[1];
//             }
//             tf.Type = field.Type switch
//             {
//                 FlowFieldType.Directory => "Directory",
//                 FlowFieldType.Boolean => "Switch",
//                 FlowFieldType.Number => "Int",
//                 FlowFieldType.Select => "Select",
//                 FlowFieldType.NumberPercent => "NumberPercent",
//                 _ => "Text"
//             };
//
//             if (field.Type == FlowFieldType.Directory && string.IsNullOrWhiteSpace(tf.Default as string))
//                 tf.Default = DirectoryHelper.GetUsersHomeDirectory();
//
//             if (field.Type == FlowFieldType.Select)
//             {
//                 tf.Parameters = new
//                 {
//                     options = field.Options.Select(x =>
//                     {
//                         var parts = x.Split('|');
//                         if (parts.Length == 1)
//                             return new { label = parts[0], value = parts[0] };
//                         return new { label = parts[0], value = parts[1] };
//                     })
//                 };
//             }
//             
//             results.Add(tf);
//
//             if (string.IsNullOrWhiteSpace(field.IfName))
//                 continue;
//             var other = flow.Properties?.Fields?.FirstOrDefault(x => x.Name == field.IfName);
//             if (other == null)
//                 continue;
//
//             var condition = new Condition();
//             condition.Property = other.Name;
//             if (other.Type == FlowFieldType.Boolean)
//                 condition.Value = field.IfValue?.ToLowerInvariant()?.Trim() == "true";
//             else if (other.Type == FlowFieldType.Number && int.TryParse(field.IfValue?.Trim(), out int iOther))
//                 condition.Value = iOther;
//             else
//                 condition.Value = field.IfValue!;
//             condition.IsNot = field.IfNot;
//             tf.Conditions ??= new();
//             tf.Conditions.Add(condition);
//         }
//
//         return results;
//     }
// }