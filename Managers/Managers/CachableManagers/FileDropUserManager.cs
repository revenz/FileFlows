using FileFlows.ServerShared.Models;

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
    /// <param name="picture">The users picture.</param>
    /// <param name="auditDetails">Optional audit details</param>
    /// <returns>The mapped or newly created local user.</returns>
    public async Task<Result<FileDropUser>> GetOrCreateLocalUser(string provider,
        string providerUid,
        string email,
        string name,
        string picture, AuditDetails? auditDetails)
    {
        var existing = await GetUserFromProviderInfo(provider, providerUid, email);
        if (existing.Failed(out var error))
            return Result<FileDropUser>.Fail(error);
        if (existing.Value != null)
            return existing.Value;
        
        FileDropUser user = new();
        user.Email = email.ToLower();
        user.Provider = provider;
        user.ProviderUid = providerUid;
        user.Name = name;
        user.Picture = picture ?? string.Empty;
        return await Update(user, auditDetails);
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
        var user = data.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user != null)
            return ValidateUser(user);

        user = data.FirstOrDefault(x => x.ProviderUid.Equals(providerUid, StringComparison.OrdinalIgnoreCase)
                                        && x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (user != null)
            return ValidateUser(user);
        
        return null;

        Result<FileDropUser?> ValidateUser(FileDropUser rUser)
        {
            if (rUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase) == false)
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
    /// <param name="auditDetails">Optional audit details</param>
    public async Task SetTokens(Guid fileDropUserUid, int tokens, AuditDetails? auditDetails = null)
    {
        var user = await GetByUid(fileDropUserUid);
        if (user == null)
            return;
        user.Tokens = tokens;
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
        return user?.Email;
    }
}
