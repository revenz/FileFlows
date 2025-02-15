namespace FileFlows.RemoteServices;

/// <summary>
/// A flow runner which is responsible for executing a flow and processing files
/// </summary>
public class FlowRunnerService : RemoteService, IFlowRunnerService
{
    /// <inheritdoc />
    public async Task<int> GetFileCheckInterval()
    {
        try
        {
            var result = await HttpHelper.Get<int>($"{ServiceBaseUrl}/remote/system/file-check-interval");
            if (result.Success == false)
                return 60;
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get file check interval: " + ex.Message);
            return 60;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsLicensed()
    {
        try
        {
            var result = await HttpHelper.Get<bool>($"{ServiceBaseUrl}/remote/system/is-licensed");
            if (result.Success == false)
                return false;
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get if licensed: " + ex.Message);
            return false;
        }
    }
    /// <summary>
    /// Called when a flow execution starts
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>The updated information</returns>
    public async Task Finish(FlowExecutorInfo info)
    {
        try
        {
            var result = await HttpHelper.Post($"{ServiceBaseUrl}/remote/work/finish", info);
            if (result.Success == false)
                throw new Exception(result.Body);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to finish work: " + ex.Message);
        }
    }

    /// <summary>
    /// Called when the flow execution has completed
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task<FlowExecutorInfo?> Start(FlowExecutorInfo info)
    {
        try
        {
            var result = await HttpHelper.Post<FlowExecutorInfo>($"{ServiceBaseUrl}/remote/work/start", info);
            if (result.Success == false)
                throw new Exception("Failed to start work: " + result.Body);
            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to start work: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Called to update the status of the flow execution on the server
    /// </summary>
    /// <param name="info">The information about the flow execution</param>
    /// <returns>a completed task</returns>
    public async Task Update(FlowExecutorInfo info)
    {
        try
        {
            var result = await HttpHelper.Post($"{ServiceBaseUrl}/remote/work/update", info);
            if (result.Success == false)
                throw new Exception("Failed to update work: " + result.Body);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to update work: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task SetThumbnail(Guid libraryFileUid, byte[] binaryData)
    {
        try
        {
            var result = await HttpHelper.Post($"{ServiceBaseUrl}/remote/work/set-thumbnail/{libraryFileUid}",
                binaryData);
            if (result.Success == false)
                throw new Exception("Failed to set thumbnail: " + result.Body);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to set thumbnail: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetFileDropUserUsername(Guid fileDropUserUid)
    {
        try
        {
            var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/remote/flow/file-drop-user{fileDropUserUid}/name");
            if (result.Success == false)
                throw new Exception("Failed to get file drop user username: " + result.Body);
            return result.Data ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to get file drop user username: " + ex.Message);
            return string.Empty;
        }
    }
}
