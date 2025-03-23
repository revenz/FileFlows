namespace FileFlows.Client.Shared;

/// <summary>
/// FileDrop Menu
/// </summary>
public class FileDropLayout : ConfigurationLayout
{
    /// <inheritdoc />
    protected override List<NavMenuGroup> GetMenu(Profile profile)
        => GetMenuItems(profile);
    
    /// <summary>
    /// Gets the link to the first available item
    /// </summary>
    /// <param name="profile">the users profile</param>
    /// <returns>the first available item</returns>
    public static string GetFirstAvailableItem(Profile profile)
        => GetMenuItems(profile).FirstOrDefault()?.Items?.FirstOrDefault()?.Url;
    
    /// <inheritdoc />
    public static List<NavMenuGroup> GetMenuItems(Profile profile)
    {
        List<NavMenuGroup> menuItems = new();

        menuItems.Add(new()
        {
            Name = Translater.Instant("MenuGroups.Configuration"),
            Items =
            [
                new(Translater.Instant("Pages.Settings.Labels.General"), "fas fa-cogs", "file-drop/general"),
                new("Home Page", "fas fa-home", "file-drop/home-page"),
                new("Custom CSS", "fab fa-css3-alt", "file-drop/custom-css"),
            ]
        });

        menuItems.Add(new NavMenuGroup
        {
            Name = Translater.Instant("MenuGroups.Security"),
            Items =
            [
                new("Passwords", "fas fa-shield-alt", "file-drop/passwords"),
                new("Single Sign On", "fas fa-cloud", "file-drop/single-sign-on"),
                new("Users", "fas fa-users", "file-drop/auto-tokens")
            ]
        });

        menuItems.Add(new NavMenuGroup
        {
            Name = "Tokens",
            Items =
            [
                new("Tokens", "fas fa-coins", "file-drop/tokens"),
                new("Auto Tokens", "fas fa-clock", "file-drop/auto-tokens"),
            ]
        });
        
        return menuItems;
    }
}