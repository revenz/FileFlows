using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Reseller User Manager
/// </summary>
public class ResellerUserManager : CachedManager<ResellerUser>
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
    public async Task<Result<ResellerUser>> GetOrCreateLocalUser(string provider,
        string providerUid,
        string email,
        string name,
        string picture, AuditDetails? auditDetails)
    {
        var existing = await GetUserFromProviderInfo(provider, providerUid, email);
        if (existing.Failed(out var error))
            return Result<ResellerUser>.Fail(error);
        if (existing.Value != null)
            return existing.Value;
        
        ResellerUser user = new();
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
    private async Task<Result<ResellerUser?>> GetUserFromProviderInfo(string provider, string providerUid, string email)
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

        Result<ResellerUser?> ValidateUser(ResellerUser rUser)
        {
            if (rUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase) == false)
                return Result<ResellerUser?>.Fail("Email address not valid for this user.");
            if (rUser.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) == false)
                return Result<ResellerUser?>.Fail("Email address registered to different user.");
            if (rUser.ProviderUid.Equals(providerUid, StringComparison.OrdinalIgnoreCase) == false)
                return Result<ResellerUser?>.Fail("Email address registered to different user.");
            return rUser;
        }
    }

    
    /// <summary>
    /// Gives the specified number of tokens to the user
    /// </summary>
    /// <param name="resellerUserUid">The UID of the reseller user</param>
    /// <param name="tokens">the number of tokens to give</param>
    /// <param name="auditDetails">Optional audit details</param>
    public async Task GiveTokens(Guid resellerUserUid, int tokens, AuditDetails? auditDetails = null)
    {
        var user = await GetByUid(resellerUserUid);
        if (user == null)
            return;
        user.Tokens += tokens;
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(user, auditDetails);
    }
    
    /// <summary>
    /// Sets the specified number of tokens to the user
    /// </summary>
    /// <param name="resellerUserUid">The UID of the reseller user</param>
    /// <param name="tokens">the number of tokens to set</param>
    /// <param name="auditDetails">Optional audit details</param>
    public async Task SetTokens(Guid resellerUserUid, int tokens, AuditDetails? auditDetails = null)
    {
        var user = await GetByUid(resellerUserUid);
        if (user == null)
            return;
        user.Tokens = tokens;
        await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(user, auditDetails);
    }
}
