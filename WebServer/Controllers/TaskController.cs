using FileFlows.Interfaces;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Controller for scheduled tasks
/// </summary>
[Route("/api/task")]
[FileFlowsAuthorize(UserRole.Tasks)]
public class TaskController : BaseController
{
    /// <summary>
    /// Get all scheduled tasks configured in the system
    /// </summary>
    /// <returns>A list of all configured scheduled tasks</returns>
    [HttpGet]
    public async Task<IEnumerable<FileFlowsTask>> GetAll()
        => (await ServiceLoader.Load<TaskService>().GetAllAsync()).OrderBy(x => x.Name.ToLowerInvariant());

    /// <summary>
    /// Get scheduled task
    /// </summary>
    /// <param name="uid">The UID of the scheduled task to get</param>
    /// <returns>The scheduled task instance</returns>
    [HttpGet("{uid}")]
    public Task<FileFlowsTask?> Get(Guid uid) 
        => ServiceLoader.Load<TaskService>().GetByUidAsync(uid);

    /// <summary>
    /// Get a scheduled task by its name, case insensitive
    /// </summary>
    /// <param name="name">The name of the scheduled task</param>
    /// <returns>The scheduled task instance if found</returns>
    [HttpGet("name/{name}")]
    public Task<FileFlowsTask?> GetByName(string name)
        => ServiceLoader.Load<TaskService>().GetByNameAsync(name);

    /// <summary>
    /// Saves a scheduled task
    /// </summary>
    /// <param name="fileFlowsTask">The scheduled task to save</param>
    /// <returns>The saved instance</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] FileFlowsTask fileFlowsTask)
    {
        var result = await ServiceLoader.Load<TaskService>().Update(fileFlowsTask, await GetAuditDetails());
        if (result.Failed(out string error))
            return BadRequest(error);
        return Ok(result.Value);
    }

    /// <summary>
    /// Set state of a Task
    /// </summary>
    /// <param name="uid">The UID of the Task</param>
    /// <param name="enable">Whether this Task is enabled</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<IActionResult> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = ServiceLoader.Load<TaskService>();
        var item = await service.GetByUidAsync(uid);
        if (item == null)
            return BadRequest("Task not found.");
        if (enable != null && item.Enabled != enable.Value)
        {
            item.Enabled = enable.Value;
            var result = await service.Update(item, await GetAuditDetails());
            if (result.Failed(out string error))
                return BadRequest(error);
            item = result.Value;
        }
        return Ok(item);
    }
    
    /// <summary>
    /// Delete scheduled tasks from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await ServiceLoader.Load<TaskService>().Delete(model.Uids, await GetAuditDetails());


    /// <summary>
    /// Runs a script now
    /// </summary>
    /// <param name="uid">the UID of the script</param>
    [HttpPost("run/{uid}")]
    public Task<FileFlowsTaskRun> Run([FromRoute] Guid uid)
        => ServiceLoader.Load<ITaskService>().RunByUid(uid);
}
