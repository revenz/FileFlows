using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Services;


/// <summary>
/// A service that detects clicks outside of a specified HTML element.
/// </summary>
public class ClickOutsideService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ClickOutsideService>? _dotNetRef;
    private ElementReference _element;
    private IJSObjectReference? _module;

    /// <summary>
    /// Event triggered when a click occurs outside the watched element.
    /// </summary>
    public event Action? OnClickOutside;

    /// <summary>
    /// Creates a new instance of the <see cref="ClickOutsideService"/>.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime.</param>
    public ClickOutsideService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Watches for clicks outside the given element.
    /// </summary>
    /// <param name="element">The element to watch.</param>
    public async void Watch(ElementReference element)
    {
        _element = element;

        _dotNetRef?.Dispose();
        _dotNetRef = DotNetObjectReference.Create(this);

        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/scripts/ClientOutsideService.js");

        await _module.InvokeVoidAsync("addClickOutsideListener", _element, _dotNetRef);
    }

    /// <summary>
    /// Called by JavaScript when a click outside is detected.
    /// </summary>
    [JSInvokable]
    public void NotifyClickOutside()
    {
        Console.WriteLine("Click outside detected!");
        OnClickOutside?.Invoke();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.InvokeVoidAsync("removeClickOutsideListener");
            await _module.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }
}