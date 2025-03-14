using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using BlazorContextMenu;
using FileFlows.Client;
using FileFlows.Client.Services.Frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<IHotKeysService, HotKeysService>();
builder.Services.AddSingleton<INavigationService, NavigationService>();
builder.Services.AddSingleton<IClipboardService, ClipboardService>();
builder.Services.AddSingleton<ProfileService>();
builder.Services.AddSingleton<IModalService, ModalService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ClientService>();
builder.Services.AddSingleton<IPausedService, PausedService>();
builder.Services.AddBlazorContextMenu(options =>
{
    options.ConfigureTemplate(template =>
    {
        template.MenuCssClass = "context-menu";
        template.MenuItemCssClass = "context-menu-item";
        template.Animation = Animation.Grow;
    });
});

builder.Services.AddSingleton<FFLocalStorageService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<FrontendService>();

await builder.Build().RunAsync();
