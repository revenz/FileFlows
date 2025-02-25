using FileFlows.ServerShared.Models;
using Microsoft.IdentityModel.Tokens;

namespace FileFlows.Managers;

/// <summary>
/// File Drop User Manager
/// </summary>
public class FileDropUserManager : CachedManager<FileDropUser>
{
    /// <summary>
    /// Force using cache
    /// </summary>
    protected override bool UseCache => true;

    /// <summary>
    /// Maps an external user to a local user in your system, creating the user if necessary.
    /// </summary>
    /// <param name="provider">The name of the external provider.</param>
    /// <param name="providerUid">The UID of the user from the Provider</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="name">The user's name (if available).</param>
    /// <param name="tokens">How many tokens to give if creating the user.</param>
    /// <param name="maxFileDropUsers">The maximum FileDrop users allowed</param>
    /// <param name="auditDetails">Optional audit details</param>
    /// <returns>The mapped or newly created local user.</returns>
    public async Task<Result<FileDropUser>> GetOrCreateLocalUser(string provider,
        string providerUid,
        string email,
        string name,
        int tokens,
        int maxFileDropUsers,
        AuditDetails? auditDetails)
    {
        var existing = await GetUserFromProviderInfo(provider, providerUid, email);
        if (existing.Failed(out var error))
            return Result<FileDropUser>.Fail(error);
        
        var all = await GetAll();
        if (existing.Value != null)
        {
            if (all.Count > maxFileDropUsers)
            {
                // check one if in first indexes
                int index = all.IndexOf(existing.Value);
                if(index >= maxFileDropUsers)
                    return Result<FileDropUser>.Fail("Exceeded licensed users.");
            }
            
            return existing.Value;
        }

        var current = (await GetAll()).Count;
        if (current >= maxFileDropUsers)
            return Result<FileDropUser>.Fail("Cannot create any more users.");
        
        FileDropUser user = new();
        user.Name = email.ToLower();
        user.Provider = provider;
        user.ProviderUid = providerUid;
        user.DisplayName = name;
        user.Tokens = tokens;
        user.Enabled = true;
        user.LastAutoTokensUtc = DateTime.UtcNow;
        return await AddUser(user, maxFileDropUsers, auditDetails);
    }

    private SemaphoreSlim _addSemaphore = new(1, 1);
    
    /// <summary>
    /// Adds a FileDrop user
    /// </summary>
    /// <param name="user">the user to add</param>
    /// <param name="maxAllowed">the maximum allowed FileDrop users</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>the created user or a failure</returns>
    public async Task<Result<FileDropUser>> AddUser(FileDropUser user, int maxAllowed, AuditDetails? auditDetails)
    {
        await _addSemaphore.WaitAsync();
        try
        {
            var current = (await GetAll()).Count;
            if (current >= maxAllowed)
                return Result<FileDropUser>.Fail("Cannot create any more users.");
            
            return await Update(user, auditDetails);
        }
        finally
        {
            _addSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets a user from their provider information
    /// </summary>
    /// <param name="provider">The name of the external provider.</param>
    /// <param name="providerUid">The UID of the user from the Provider</param>
    /// <param name="email">The user's email address.</param>
    /// <returns>The user if one exists, otherwise null</returns>
    private async Task<Result<FileDropUser?>> GetUserFromProviderInfo(string provider, string providerUid, string email)
    {
        var data = await GetData();
        var user = data.FirstOrDefault(x => x.Name.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user != null)
            return ValidateUser(user);

        user = data.FirstOrDefault(x => x.ProviderUid.Equals(providerUid, StringComparison.OrdinalIgnoreCase)
                                        && x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (user != null)
            return ValidateUser(user);
        
        return null;

        Result<FileDropUser?> ValidateUser(FileDropUser rUser)
        {
            if (rUser.Name.Equals(email, StringComparison.OrdinalIgnoreCase) == false)
                return Result<FileDropUser?>.Fail("Email address not valid for this user.");
            if (rUser.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) == false)
                return Result<FileDropUser?>.Fail("Email address registered to different user.");
            if (rUser.ProviderUid.Equals(providerUid, StringComparison.OrdinalIgnoreCase) == false)
                return Result<FileDropUser?>.Fail("Email address registered to different user.");
            return rUser;
        }
    }

    
    /// <summary>
    /// Gives the specified number of tokens to the user
    /// </summary>
    /// <param name="fileDropUserUid">The UID of the file drop user</param>
    /// <param name="tokens">the number of tokens to give</param>
    /// <param name="auditDetails">Optional audit details</param>
    /// <returns>the number of tokens the user now has</returns>
    public async Task<int> GiveTokens(Guid fileDropUserUid, int tokens, AuditDetails? auditDetails = null)
    {
        var user = await GetByUid(fileDropUserUid);
        if (user == null)
            return 0;
        user.Tokens += tokens;
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(user, auditDetails);
        return user.Tokens;
    }
    
    /// <summary>
    /// Takes the specified number of tokens to the user
    /// </summary>
    /// <param name="fileDropUserUid">The UID of the file drop user</param>
    /// <param name="tokens">the number of tokens to tale</param>
    /// <param name="auditDetails">Optional audit details</param>
    /// <returns>the number of tokens the user now has</returns>
    public async Task<Result<int>> TakeTokens(Guid fileDropUserUid, int tokens, AuditDetails? auditDetails = null)
    {
        var user = await GetByUid(fileDropUserUid);
        if (user == null)
            return Result<int>.Fail("User not found.");
        if(user.Tokens < tokens)
            return Result<int>.Fail("User does not have enough tokens.");
        user.Tokens -= tokens;
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(user, auditDetails);
        return user.Tokens;
    }
    
    /// <summary>
    /// Sets the specified number of tokens to the user
    /// </summary>
    /// <param name="fileDropUserUid">The UID of the file drop user</param>
    /// <param name="tokens">the number of tokens to set</param>
    /// <param name="autoTokens">if these are auto tokens</param>
    /// <param name="auditDetails">Optional audit details</param>
    public async Task SetTokens(Guid fileDropUserUid, int tokens, bool autoTokens = false, AuditDetails? auditDetails = null)
    {
        var user = await GetByUid(fileDropUserUid);
        if (user == null)
            return;
        user.Tokens = tokens;
        if (autoTokens)
            user.LastAutoTokensUtc = DateTime.UtcNow;
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(user, auditDetails);
    }

    /// <summary>
    /// Gets the email address for a user
    /// </summary>
    /// <param name="fduUid">the users UID</param>
    /// <returns>the email address or null if not found</returns>
    public async Task<string?> GetEmail(Guid fduUid)
    {
        var user = await GetByUid(fduUid);
        return user?.Name;
    }


    /// <summary>
    /// Gets a file drop user by their email
    /// </summary>
    /// <param name="email">the email of the file drop user</param>
    /// <returns>the file drop user if found</returns>
    public async Task<FileDropUser?> GetByEmail(string email)
        => await GetByName(email); // the name is their email

    /// <summary>
    /// Gets the number of FileDrop users
    /// </summary>
    /// <returns>the number of FileDrop users</returns>
    public async Task<int> GetCount()
        => (await GetAll()).Count;
}
