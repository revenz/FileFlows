namespace FileFlows.WebServer.Controllers.Pages;

/// <summary>
/// Initial configuration
/// </summary>
[Route("initial-config")]
public class InitialConfigController : BaseController
{
    /// <summary>
    /// Gets the initial configuration
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get();
        if (settings.InitialConfigDone)
            return Redirect("/");
        
        InitialConfigViewModel model = new ();
        var langService = ServiceLoader.Load<LanguageService>();
        model.Languages = langService.GetLanguageOptions();
        model.Plugins = await GetPlugins();
        model.DockerMods = await GetDockerMods();

        foreach (var lang in model.Languages)
        {
            var json = await langService.GetLanguageJson((string)lang.Value!);
            var translater = new TranslaterInstance(json);
            LanguageTranslation translation = new()
            {
                Previous = translater.Instant("Labels.Previous"),
                Next = translater.Instant("Labels.Next"),
                
                WelcomeMessage = translater.Instant("Pages.InitialConfig.Messages.Welcome"),
                WelcomeMessageUpdate = translater.Instant("Pages.InitialConfig.Messages.WelcomeUpdate"),
                Welcome = translater.Instant("Pages.InitialConfig.Tabs.Welcome"),
                WelcomeDescription = translater.Instant("Pages.InitialConfig.Tabs.WelcomeDescription"),
                Eula = translater.Instant("Pages.InitialConfig.Tabs.Eula"),
                EulaDescription = translater.Instant("Pages.InitialConfig.Tabs.EulaDescription"),
                EulaAccept = translater.Instant("Pages.InitialConfig.Fields.EulaAccept"),
                Plugins = translater.Instant("Pages.InitialConfig.Tabs.Plugins"),
                PluginsDescription = translater.Instant("Pages.InitialConfig.Tabs.PluginsDescription"),
                DockerMods = translater.Instant("Pages.InitialConfig.Tabs.DockerMods"),
                DockerModsDescription = translater.Instant("Pages.InitialConfig.Tabs.DockerModsDescription"),
                Finish = translater.Instant("Pages.InitialConfig.Tabs.Finish"),
                FinishDescription = translater.Instant("Pages.InitialConfig.Tabs.FinishDescription"),
                FinishTop = translater.Instant("Pages.InitialConfig.Messages.Finish.Top"),
                FinishCreateFirstFlow = translater.Instant("Pages.InitialConfig.Messages.Finish.CreateFirstFlow"),
                FinishCreateFirstFlowDescription =
                    translater.Instant("Pages.InitialConfig.Messages.Finish.CreateFirstFlowDescription"),
                FinishCreateALibrary = translater.Instant("Pages.InitialConfig.Messages.Finish.CreateALibrary"),
                FinishCreateALibraryDescription =
                    translater.Instant("Pages.InitialConfig.Messages.Finish.CreateALibraryDescription"),
                FinishBottom = translater.Instant("Pages.InitialConfig.Messages.Finish.Bottom"),
                Installed = translater.Instant("Labels.Installed"),
                Runners = translater.Instant("Pages.InitialConfig.Tabs.Runners"),
                RunnersDescription = translater.Instant("Pages.InitialConfig.Tabs.RunnersDescription"),
                RunnersTop = translater.Instant("Pages.InitialConfig.Messages.RunnersTop"),
            };
            foreach (var key in translater.GetKeys())
            {
                if (key.StartsWith("Plugins."))
                    translation.ItemTranslations[key.Replace(".", "-")] = translater.Instant(key);
                if (key.StartsWith("DockerMods."))
                    translation.ItemTranslations[key.Replace(".", "-")] = translater.Instant(key);
            }
            model.Translations[(string)lang.Value!] = translation;
        }

        return View("InitialConfig", model);
    }


    private async Task<List<ChecklistItem>> GetPlugins()
    {
        var result = await ServiceLoader.Load<PluginService>().GetPluginPackagesActual();
        if (result.Failed(out _))
            return [];

        var AvailablePlugins = result.Value.OrderBy(x => x.Installed ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant()).ToList();

        return AvailablePlugins.Select(x =>
            {
                var item = new ChecklistItem();
                item.Name = x.Name;
                item.TranslationPrefix = $"Plugins-{x.Package.Replace(".", "")}";
                item.Description = x.Description;
                item.Icon = x.Icon;
                item.ReadOnly = x.Installed;
                item.Checked = x.Installed ||
                               x.Package.Contains("Basic", StringComparison.InvariantCultureIgnoreCase) ||
                               x.Package.Contains("Video", StringComparison.InvariantCultureIgnoreCase) ||
                               x.Package.Contains("Audio", StringComparison.InvariantCultureIgnoreCase) ||
                               x.Package.Contains("Image", StringComparison.InvariantCultureIgnoreCase) ||
                               x.Package.Contains("Meta", StringComparison.InvariantCultureIgnoreCase) ||
                               x.Package.Contains("Web", StringComparison.InvariantCultureIgnoreCase);
                item.Value = x.Package;
                return item;
            })
            .OrderBy(x => x.ReadOnly ? 0 : 1)
            .ThenBy(x => x.Checked ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant())
            .ToList();
    }

    private async Task<List<ChecklistItem>> GetDockerMods()
    {
        if (Globals.IsDocker == false)
            return [];
        
        var repo = await ServiceLoader.Load<RepositoryService>().GetRepository();

        return repo.DockerMods.Select(x =>
            {
                var item = new ChecklistItem();
                item.Name = x.Name;
                item.TranslationPrefix = $"DockerMods-{x.Name.Replace(" ", "").Replace(".", "")}";
                item.Description = x.Description;
                item.Icon = x.Icon?.EmptyAsNull() ?? "fab fa-docker";
                item.Checked = x.Name.Equals("FFmpeg6", StringComparison.OrdinalIgnoreCase) ||
                               x.Name.Equals("rar", StringComparison.OrdinalIgnoreCase) ||
                               x.Name.Equals("ImageMagick", StringComparison.OrdinalIgnoreCase);
                item.Value = x.Path;
                return item;
            }).OrderBy(x => x.Checked ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant())
            .ToList();
    }

    /// <summary>
    /// View model for initial configuration
    /// </summary>
    public class InitialConfigViewModel
    {
        /// <summary>
        /// Gets or sets the languages
        /// </summary>
        public List<ListOption> Languages { get; set; }
        /// <summary>
        /// Gets or sets the translations
        /// </summary>
        public Dictionary<string, LanguageTranslation> Translations { get; set; } = [];
        
        /// <summary>
        /// Gets or sets the plugins
        /// </summary>
        public List<ChecklistItem> Plugins { get; set; } = null!;
        
        /// <summary>
        /// Gets or sets the DockerMods
        /// </summary>
        public List<ChecklistItem> DockerMods { get; set; } = null!;

    }
    
    public class LanguageTranslation
    {
        public string Previous { get; set; }
        public string Next { get; set; }
        public string WelcomeMessage { get; set; }
        public string WelcomeMessageUpdate { get; set; }
        public string Welcome { get; set; }
        public string WelcomeDescription { get; set; }
        public string Eula { get; set; }
        public string EulaDescription { get; set; }
        public string EulaAccept { get; set; }
        public string Plugins { get; set; }
        public string PluginsDescription { get; set; }
        public string DockerMods { get; set; }
        public string DockerModsDescription { get; set; }
        public string Finish { get; set; }
        public string FinishDescription { get; set; }
        public string FinishTop { get; set; }
        public string FinishCreateFirstFlow { get; set; }
        public string FinishCreateFirstFlowDescription { get; set; }
        public string FinishCreateALibrary { get; set; }
        public string FinishCreateALibraryDescription { get; set; }
        public string FinishBottom { get; set; }
        public string Installed { get; set; }
        public string Runners { get; set; }
        public string RunnersDescription { get; set; }
        public string RunnersTop { get; set; }

        public Dictionary<string, string> ItemTranslations { get; set; } = [];
    }

    /// <summary>
    /// Saves the initial configuration
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the response</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] InitialConfigurationModel model)
    {
        var service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = await service.Get();
        if (settings.InitialConfigDone)
            return BadRequest("Initial configuration is already complete");
        
        if (model.Plugins?.Any() == true)
        {
            var pluginService = ServiceLoader.Load<PluginService>();
            var availableResult = await pluginService.GetPluginPackagesActual();
            if (availableResult.Success(out var available))
            {
                var plugins = available.Where(x => model.Plugins.Contains(x.Package)).ToList();
                if (plugins.Count > 0)
                    await pluginService.DownloadPlugins(plugins);
            }
        }

        settings.EulaAccepted = true;
        settings.InitialConfigDone = true;
        settings.Language = model.Language?.EmptyAsNull() ?? "en";

        if (model.Runners > 0)
        {
            var nodeService = ServiceLoader.Load<NodeService>();
            var node = await nodeService.GetServerNodeAsync();
            if (node != null)
            {
                node.FlowRunners = model.Runners;
                await nodeService.Update(node, null);
            }
        }

        await service.Save(settings, await GetAuditDetails());

        if (model.DockerMods?.Any() == true)
        {
            var dmService = ServiceLoader.Load<DockerModService>();
            var repoService = ServiceLoader.Load<RepositoryService>();
            foreach (var dm in model.DockerMods)
            {
                var content = await repoService.GetContent(dm!);
                if (content.IsFailed == false)
                    await dmService.ImportFromRepository(content.Value, null);
            }
        }

        return Ok();
    }

    /// <summary>
    /// The initial configuration model
    /// </summary>
    public class InitialConfigurationModel
    {
        /// <summary>
        /// Gets or sets the plugins to download
        /// </summary>
        public List<string> Plugins { get; set; } = null!;
        /// <summary>
        /// Gets or sets the DockerMods to install
        /// </summary>
        public List<string> DockerMods { get; set; } = null!;
        /// <summary>
        /// Gets or sets the language
        /// </summary>
        public string Language { get; set; } = null!;
        /// <summary>
        /// Gets or sets the flow runners
        /// </summary>
        public int Runners { get; set; }
    }
    
    

    /// <summary>
    /// Represents an item in a checklist.
    /// </summary>
    public class ChecklistItem
    {
        /// <summary>
        /// Gets or sets the icon associated with the checklist item.
        /// </summary>
        public string Icon { get; set; }
        
        /// <summary>
        /// Gets or sets the translation prefix
        /// </summary>
        public string TranslationPrefix { get; set; }

        /// <summary>
        /// Gets or sets the name of the checklist item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the checklist item.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checklist item is checked.
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the checklist item is read-only.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the value associated with the checklist item.
        /// </summary>
        public object Value { get; set; }
    }

}