using NUnit.Framework.Interfaces;

namespace FileFlowsTests.Tests;

/// <summary>
/// Initial tests the configures FileFlows.
/// And performs test on an un-configured system
/// </summary>
//[TestClass]
//[TestCategory("InitialTests")]
public class InitialTests : TestBase
{
    private static bool _hasFailed = false;

    /// <inheritdoc />
    protected override bool SetupStart()
        => _hasFailed == false;

    /// <inheritdoc />
    protected override void TestEnded(ResultState result)
    {
        if(result != ResultState.Success && result != ResultState.Skipped)
            _hasFailed = true;
    }

    /// <summary>
    /// Test the DockerMods too many error is shown
    /// </summary>
    [Test, Order(1)]
    public async Task IT_01_InitialConfiguration_TooManyDockerMods()
    {
        await Page.WaitForURLAsync(FileFlows.BaseUrl + "initial-config");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.Shown(), "The Initial Configuration should be shown.");
        ClassicAssert.AreEqual("Welcome to FileFlows", await FileFlows.InitialConfiguration.GetPageTitle());
        
        await FileFlows.InitialConfiguration.NextClick(); 
        await FileFlows.InitialConfiguration.AcceptEula();
        await FileFlows.InitialConfiguration.NextClick();
        
        // next page
        await FileFlows.InitialConfiguration.NextClick();

        ClassicAssert.AreEqual("Choose which DockerMods to install", await FileFlows.InitialConfiguration.GetPageTitle());
        var dockerMods = await FileFlows.InitialConfiguration.GetItems();
        var ffmpeg6 = dockerMods.FirstOrDefault(x => x.Name == "FFmpeg6");
        ClassicAssert.IsNotNull(ffmpeg6, "FFmpeg6 DockerMod is not found.");
        ClassicAssert.IsTrue(ffmpeg6!.Checked, "FFmpeg6 DockerMod is not checked by default.");
        await FileFlows.InitialConfiguration.CheckAllItems(); // we dont want this DockerMod installed
        await FileFlows.InitialConfiguration.NextClick();

        await FileFlows.MessageBox.Exist("Warning",
            "You have selected too many DockerMods.");
    }
    
    /// <summary>
    /// Test the license shows unlicensed
    /// </summary>
    [Test, Order(2)]
    public async Task IT_02_InitialConfiguration()
    {
        await Page.WaitForURLAsync(FileFlows.BaseUrl + "initial-config");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.Shown(), "The Initial Configuration should be shown.");
        ClassicAssert.AreEqual("Welcome to FileFlows", await FileFlows.InitialConfiguration.GetPageTitle());
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("EULA"), "The EULA page should be enabled");
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("Plugins"), "The Plugins page should be disabled");
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("DockerMods"), "The DockerMods page should be disabled");
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("Runners"), "The Runners page should be disabled");
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("Finish"), "The Finish page should be disabled");
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.PreviousButtonShown(), "The Previous button should not be shown");
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.FinishButtonShown(), "The Finish button should not be shown");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.NextButtonShown(), "The Next button should be shown");
            
        await FileFlows.InitialConfiguration.NextClick();
        ClassicAssert.AreEqual("End-User License Agreement of FileFlows", await FileFlows.InitialConfiguration.GetPageTitle());
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.PreviousButtonShown(), "The previous button should be shown and it is not.");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.NextDisabled(), "Next Button should be disabled until the EULA is accepted.");
        await FileFlows.InitialConfiguration.AcceptEula();
        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.NextDisabled(), "Next Button should not be disabled");
        await FileFlows.InitialConfiguration.NextClick();
        
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("Plugins"), "Plugins Page is not enabled");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("DockerMods"), "DockerMods Page is not enabled");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("Finish"), "Finish Page is not enabled");
        
        ClassicAssert.AreEqual("Choose which plugins to install", await FileFlows.InitialConfiguration.GetPageTitle());
        var plugins = await FileFlows.InitialConfiguration.GetItems();
        var basic = plugins.FirstOrDefault(x => x.Name == "Basic");
        ClassicAssert.IsNotNull(basic, "Basic Plugin is not found.");
        ClassicAssert.IsTrue(basic!.Checked, "Basic Plugin is not checked by default.");
        await FileFlows.InitialConfiguration.NextClick();

        ClassicAssert.AreEqual("Choose which DockerMods to install", await FileFlows.InitialConfiguration.GetPageTitle());
        var dockerMods = await FileFlows.InitialConfiguration.GetItems();
        var ffmpeg6 = dockerMods.FirstOrDefault(x => x.Name == "FFmpeg6");
        ClassicAssert.IsNotNull(ffmpeg6, "FFmpeg6 DockerMod is not found.");
        ClassicAssert.IsTrue(ffmpeg6!.Checked, "FFmpeg6 DockerMod is not checked by default.");
        await FileFlows.InitialConfiguration.ClearAllItems(); // we dont want this DockerMod installed
        await FileFlows.InitialConfiguration.NextClick();
        
        ClassicAssert.AreEqual("Specify the number of runners", await FileFlows.InitialConfiguration.GetPageTitle());
        await Page.Locator(".flow-numeric input").FillAsync("2");
        await FileFlows.InitialConfiguration.NextClick();

        ClassicAssert.IsFalse(await FileFlows.InitialConfiguration.NextButtonShown(), "Next Button is shown when it should not be present");
        ClassicAssert.IsTrue(await FileFlows.InitialConfiguration.FinishButtonShown(), "Finish Button is not shown.");

        await FileFlows.InitialConfiguration.FinishClick();
        await Page.WaitForSelectorAsync(".sidebar .nav-menu-footer .version-info");

        await FileFlows.GotoPage("Nodes");
        await FileFlows.WaitForBlockerToDisappear();
        var text = await Page.Locator("div[title='Flow Runners'] span").TextContentAsync() ?? string.Empty;
        ClassicAssert.AreEqual("2", text.Trim(), $"Flow runners expected to be 2, but was {text}");
    }

    /// <summary>
    /// Sets the FFmpeg paths
    /// </summary>
    [Test, Order(3)] 
    public async Task IT_03_SetFFmpegPath()
    {
        await FileFlows.GotoPage("Variables");
        await DoubleClickItem("ffmpeg");
        await SetTextArea("Value", "/tools/ffmpeg/ffmpeg");
        await ButtonClick("Save");
        await Task.Delay(500);
        await DoubleClickItem("ffprobe");
        await SetTextArea("Value", "/tools/ffmpeg/ffprobe");
        await ButtonClick("Save");
        await Task.Delay(500);
    }
    
    /// <summary>
    /// Test the license shows unlicensed
    /// </summary>
    [Test, Order(4)]
    public async Task IT_04_LicenseUnlicensed()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Click("License");
    
        var txtStatus = Page.Locator("input[placeholder='Status']");
        await Expect(txtStatus).ToHaveCountAsync(1);
        await Expect(txtStatus).ToHaveValueAsync("Unlicensed");
    }
    
    /// <summary>
    /// Tests the tabs in settings are only the expected tabs
    /// </summary>
    [Test, Order(5)]
    public async Task IT_05_UnlicensedTabs()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Exists("General");
        await FileFlows.Tab.Exists("Logging");
        await FileFlows.Tab.Exists("Database");
        await FileFlows.Tab.Exists("Email");
        await FileFlows.Tab.Exists("License");
        
        await FileFlows.Tab.DoesntExists("Updates");
        await FileFlows.Tab.DoesntExists("File Server");
        await FileFlows.Tab.DoesntExists("Security");
    
        await FileFlows.Tab.Click("Logging");
        await Expect(Page.Locator("label >> text=Log Every Request")).ToHaveCountAsync(0);
        await Expect(Page.Locator("label >> text=Log File Retention")).ToHaveCountAsync(0);
    }
    
    /// <summary>
    /// Tests that if a user tries to add a library before adding a flow, they are stopped with a toast error message
    /// </summary>
    [Test, Order(6)]
    public async Task IT_06_LibraryNoFlows()
    {
        await FileFlows.GotoPage("Libraries");
        await FileFlows.Table.ButtonClick("Add");
        ClassicAssert.AreEqual("There are no flows configured. Create a flow before adding or updating a library.", 
            await FileFlows.Toast.GetError());
    }
    
    /// <summary>
    /// Tests the pointer is shown for step 1.
    /// </summary>
    [Test, Order(7)]
    public async Task IT_07_FlowPointer()
    {
        await Expect(Page.Locator(".nav-item.flows .not-configured-pointer")).ToHaveCountAsync(1);
        await FileFlows.GotoPage("Flows");
        await Expect(Page.Locator(".pointer-add >> text='Add'")).ToHaveCountAsync(1);
    }
    
    /// <summary>
    /// Tests creating a flow
    /// </summary>
    [Test, Order(8)]
    public async Task IT_08_FlowCreate()
    {
        await FileFlows.GotoPage("Flows");
        await FileFlows.Table.ButtonClick("Add");
    
        var templates = await FileFlows.FlowTemplateDialog.GetTemplates();
        var templateFile = templates.FirstOrDefault(x => x.Name == "File");
        ClassicAssert.IsNotNull(templateFile);
    
        await FileFlows.FlowTemplateDialog.Select("File");
    
        await FileFlows.Flow.SetTitle(Constants.Flow_Basic);
        await FileFlows.Flow.Save();
        
        await FileFlows.GotoPage("Flows");
    
        await Expect(Page.Locator(".nav-item.libraries .not-configured-pointer")).ToHaveCountAsync(1);
    }
    
    /// <summary>
    /// Creates a library
    /// </summary>
    [Test, Order(9)]
    public async Task IT_09_LibraryCreate()
    {
        await FileFlows.GotoPage("Libraries");
        await Expect(Page.Locator(".pointer-add >> text='Add'")).ToHaveCountAsync(1);
        await FileFlows.Table.ButtonClick("Add");
        await FileFlows.Editor.Title("Library");
        await FileFlows.Inputs.SetText("Name", Constants.Library_Basic);
        await FileFlows.Editor.ButtonClick("Save");
        await FileFlows.Inputs.Error("Path", "Required");
        // await FileFlows.Inputs.Error("Flow", "Required"); // if a flow exists its auto selected
    
        await FileFlows.Inputs.SetSelect("Template", "Video Library");
        await FileFlows.Inputs.SetText("Path", "/media/basic");
        await FileFlows.Inputs.SetSelect("Flow", Constants.Flow_Basic);
        await FileFlows.Editor.ButtonClick("Save");
    
        await Expect(Page.Locator(".pointer-add >> text='Add'")).ToHaveCountAsync(0);
    }
    
    [Test, Order(10)]
    public async Task IT_10_CheckUnLicensedPages()
    {
        await Expect(Page.Locator("a[href='tasks']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("a[href='revisions']")).ToHaveCountAsync(0);
    }
    
    /// <summary>
    /// Tests language can be changed
    /// </summary>
    [Test, Order(11)]
    public async Task IT_11_ChangeLanguage()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Click("General");

        foreach (var lang in
                 new [] {
                     ("Español", "Configuraciones", "General"),
                     ("Deutsch", "Einstellungen", "Allgemein"),
                     ("Português", "Configurações", "Geral"),
                     // ("Français", "Paramètres", "Général"),
                     // ("Italiano", "Impostazioni", "Generale"),
                     // ("Nederlands", "Instellingen", "Algemeen"),
                     // ("Svenska", "Inställningar", "Allmän"),
                     ("Русский", "Настройки", "Общие"),
                 })
        {
            SetLanguage(lang.Item1, lang.Item2, lang.Item3);
        }
        
        // russian, test flow elements are in Russian
        await Page.GotoAsync(FileFlowsBaseUrl + "flows");
        await Task.Delay(250);
        await FileFlows.WaitForBlockerToDisappear();
        await FileFlows.Table.DoubleClick(Constants.Flow_Basic);
        await FileFlows.WaitForBlockerToDisappear();
        var text = await Page.Locator("div[id='FileFlows.BasicNodes.File.InputFile']").TextContentAsync() ?? string.Empty;
        text = text.Replace("\n", "").Trim();
        ClassicAssert.AreEqual("Входной файл", text);
        
        // English Last to Rest it
        await FileFlows.GotoPage("Settings", contentTitle: "Настройки");
        SetLanguage("English", "Settings", "General");
        
        void SetLanguage(string language, string settingsPage, string generalTab)
        {
            Logger.ILog("Testing Language: " + language);
            FileFlows.Inputs.SetDropDown("Language", language).Wait();
            Page.Locator("#settings-save").ClickAsync().Wait();
            Task.Delay(500).Wait();
            FileFlows.WaitForBlockerToDisappear().Wait();

            Page.Locator(".nav-item.settings a").ClickAsync().Wait();
            FileFlows.WaitForBlockerToDisappear().Wait();
            Logger.ILog("Waiting for top row text: " + settingsPage);
            Page.Locator(".nav-item.settings .text-label", new ()
            {
                HasTextString = settingsPage
            }).WaitForAsync().Wait();

            FileFlows.Tab.Click(generalTab).Wait();
        }
    }
    
    [Test, Order(12)]
    public async Task IT_12_EnterLicense()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Click("License");
    
        var licenseEmail = Environment.GetEnvironmentVariable("FF_LICENSE_EMAIL") ?? string.Empty;
        var licenseKey = Environment.GetEnvironmentVariable("FF_LICENSE_KEY") ?? string.Empty;
        ClassicAssert.IsFalse(string.IsNullOrWhiteSpace(licenseEmail), "License Email is not set");
        ClassicAssert.IsFalse(string.IsNullOrWhiteSpace(licenseKey), "License Key is not set");
    
        await Page.Locator("input[placeholder='License Email']").FillAsync(licenseEmail);
        await Page.Locator("input[placeholder='License Key']").FillAsync(licenseKey);
        await Page.Locator("button >> text=Save").ClickAsync();
    
        var txtStatus = Page.Locator("input[placeholder='Status']");
        await Expect(txtStatus).ToHaveValueAsync("Valid", new() { Timeout = 20_000 });
    
        await FileFlows.Tab.Exists("General");
        await FileFlows.Tab.Exists("Logging");
        await FileFlows.Tab.Exists("License");
        await FileFlows.Tab.Exists("Database");
        await FileFlows.Tab.Exists("Updates");
    
        await Expect(Page.Locator("a[href='tasks']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("a[href='revisions']")).ToHaveCountAsync(1);
    }
    
    [Test, Order(13)]
    public async Task IT_13_TasksNoScript()
    {
        await GotoPage("Tasks");
        await TableButtonClick("Add");
        await ToastError("No scripts found to create a task for.");
    }
    
    [Test, Order(14)]
    public async Task IT_14_GotifyFileProcessed()
    {
        await GotoPage("Scripts");
        const string name = "Gotify - Notify File Processed";
        await SkyBox("SystemScripts");
        await TableButtonClick("Repository");
        await SelectItem(name, sideEditor: true);
        await TableButtonClick("Download", sideEditor: true);
        await FileFlows.Editor.ButtonClick("Close");
        await SelectItem(name);
    
        await GotoPage("Variables");
        await TableButtonClick("Add");
        await SetText("Name", "Gotify.Url");
        await SetTextArea("Value",
            Environment.GetEnvironmentVariable("GotifyUrl") ?? "http://gotify.lan/");
        await ButtonClick("Save");
        await TableButtonClick("Add");
        await SetText("Name", "Gotify.AccessToken");
        await SetTextArea("Value", Environment.GetEnvironmentVariable("GotifyAccessToken") ?? Guid.NewGuid().ToString());
        await ButtonClick("Save");
    
        await GotoPage("Tasks");
        await TableButtonClick("Add");
        await SetText("Name", "Gotify File Processed");
        await SetSelect("Script", name);
        await SetSelect("Type", "File Processed");
        await ButtonClick("Save");
    }
    
    [Test, Order(15)]
    public async Task IT_15_TaskScript()
    {
        await GotoPage("Tasks");
        await TableButtonClick("Add");
    }
}