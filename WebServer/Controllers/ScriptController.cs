using FileFlows.Shared.Helpers;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Script controller
/// </summary>
[Route("/api/script")]
[FileFlowsAuthorize(UserRole.Scripts)]
public class ScriptController : BaseController
{
    /// <summary>
    /// Gets all scripts in the system
    /// </summary>
    /// <returns>a list of all scripts</returns>
    [HttpGet]
    public Task<List<Script>> GetAll()
        => ServiceLoader.Load<ScriptService>().GetAll();

    /// <summary>
    /// Basic script list
    /// </summary>
    /// <param name="type">optional script type</param>
    /// <returns>script list</returns>
    [HttpGet("basic-list")]
    [FileFlowsAuthorize(UserRole.Nodes | UserRole.Tasks)]
    public async Task<Dictionary<Guid, string>> GetBasicList([FromQuery] ScriptType? type = null)
    {
        var items = await ServiceLoader.Load<ScriptService>().GetAll();
        if (type != null)
            items = items.Where(x => x.Type == type.Value).ToList();
        return items.ToDictionary(x => x.Uid, x => x.Name);
    }

    /// <summary>
    /// Get script templates for the function editor
    /// </summary>
    /// <param name="language">the language to get the templates for</param>
    /// <returns>a list of script templates</returns>
    [HttpGet("templates")]
    [FileFlowsAuthorize(UserRole.Scripts | UserRole.Flows)]
    public IEnumerable<Script> GetTemplates([FromQuery] string language = "javascript") 
        => ServiceLoader.Load<ScriptService>().GetFunctionTemplates(language);
    
    /// <summary>
    /// Returns a list of scripts
    /// </summary>
    /// <param name="type">the type of scripts to return</param>
    /// <returns>a list of scripts</returns>
    [HttpGet("all-by-type/{type}")]
    public Task<IEnumerable<Script>> GetAllByType([FromRoute] ScriptType type) 
        => ServiceLoader.Load<ScriptService>().GetAllByType(type);

    /// <summary>
    /// Returns a basic list of scripts
    /// </summary>
    /// <param name="type">the type of scripts to return</param>
    /// <returns>a basic list of scripts</returns>
    [HttpGet("list/{type}")]
    public Task<IEnumerable<Script>> List([FromRoute] ScriptType type)
        => ServiceLoader.Load<ScriptService>().GetAllByType(type);


    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="uid">The uid of the script</param>
    /// <returns>the script instance</returns>
    [HttpGet("{uid}")]
    public Task<Script?> Get([FromRoute] Guid uid)
        => ServiceLoader.Load<ScriptService>().Get(uid);


    /// <summary>
    /// Gets the code for a script
    /// </summary>
    /// <param name="uid">The name of the script</param>
    /// <returns>the code for a script</returns>
    [HttpGet("{uid}/code")]
    public async Task<string?> GetCode([FromRoute] Guid uid)
    {
        var script = await ServiceLoader.Load<ScriptService>().Get(uid);
        return script?.Code;
    }


    /// <summary>
    /// Validates a script has valid code
    /// </summary>
    /// <param name="args">the arguments to validate</param>
    [HttpPost("validate")]
    public IActionResult ValidateScript([FromBody] ValidateScriptModel args)
    {
        var result = ServiceLoader.Load<ScriptService>().ValidateScript(args.Code);//, args.IsFunction, args.Variables);
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok();
    }

    /// <summary>
    /// Saves a script
    /// </summary>
    /// <param name="script">The script to save</param>
    /// <returns>the saved script instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Script script)
    {
        Script? actualScript;
        string? error;
        if (script.Language == ScriptLanguage.JavaScript)
        {
            var scriptResult = Script.FromCode(script.Name, script.Code, script.Type);
            if (scriptResult.Failed(out error))
                return BadRequest(error);

            actualScript = scriptResult.Value;
        }
        else
        {
            actualScript = script;
        }

        actualScript.Uid = script.Uid;
        
        var result = await ServiceLoader.Load<ScriptService>().Save(actualScript, await GetAuditDetails());
        if (result.Failed(out error))
            return BadRequest(error);
        return Ok(result.Value);
    }


    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
    {
        var service = ServiceLoader.Load<ScriptService>();
        var auditDetails = await GetAuditDetails();
        foreach (var uid in model.Uids)
        {
            await service.Delete(uid, auditDetails);
        }
    }

    /// <summary>
    /// Exports a script
    /// </summary>
    /// <param name="uid">The uid of the script</param>
    /// <returns>A download response of the script</returns>
    [HttpGet("export/{uid}")]
    public async Task<IActionResult> Export([FromRoute] Guid uid)
    {
        var script = await ServiceLoader.Load<ScriptService>().Get(uid);
        if (script == null)
            return NotFound();

        string code = ScriptParser.GetCodeWithCommentBlock(script);
        
        
        byte[] data = System.Text.Encoding.UTF8.GetBytes(code);
        return File(data, "application/octet-stream", script.Name + ".js");
    }

    /// <summary>
    /// Imports a script
    /// </summary>
    /// <param name="filename">The name of the file</param>
    /// <param name="code">The code</param>
    /// <param name="type">the script type</param>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] string filename, [FromBody] string code, [FromQuery] ScriptType type)
    {
        var service = ServiceLoader.Load<ScriptService>();
        var name = filename.Replace(".js", "").Replace(".JS", "");
        var result = new ScriptParser().Parse(name, code, type);
        if (result.Failed(out string error))
            return BadRequest(error);
        
        var script = result.Value;
        script.Name = await service.GetNewUniqueName(script.Name);
        script.Repository = false;
        script.Path = null;
        script.Uid = Guid.Empty;
        var saveResult = await service.Save(script, await GetAuditDetails());
        if (saveResult.Failed(out error))
            return BadRequest(error);
        return Ok(saveResult.Value);
    }

    /// <summary>
    /// Duplicates a script
    /// </summary>
    /// <param name="uid">The uid of the script to duplicate</param>
    /// <returns>The duplicated script</returns>
    [HttpGet("duplicate/{uid}")]
    public async Task<Script?> Duplicate([FromRoute] Guid uid)
    {
        var service = ServiceLoader.Load<ScriptService>();
        var script = await service.Get(uid);
        if (script == null)
            return null;

        script.Name = await service.GetNewUniqueName(script.Name);
        script.Code = Regex.Replace(script.Code, "@name(.*?)$", "@name " + script.Name, RegexOptions.Multiline);
        script.Repository = false;
        script.Uid = Guid.NewGuid();
        return await service.Save(script, await GetAuditDetails());
    }
    
    /// <summary>
    /// Model used to validate a script
    /// </summary>
    public class ValidateScriptModel
    {
        /// <summary>
        /// Gets or sets the code to validate
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets if this is a function being validated
        /// </summary>
        public bool IsFunction { get; set; }

        /// <summary>
        /// Gets or sets optional variables to use when validating a script
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = [];
    }
}
