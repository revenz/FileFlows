namespace FileFlows.Client.Models;


public class NavMenuGroup
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public List<NavMenuItem> Items { get; set; } = new List<NavMenuItem>();
}

public class NavMenuItem
{
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }

    public NavMenuItem(string title = "", string icon = "", string url = "")
    {
        this.Title = title == "fileflows.com" ? title : Translater.TranslateIfNeeded(title);
        this.Icon = icon;
        this.Url = url;
    }
}