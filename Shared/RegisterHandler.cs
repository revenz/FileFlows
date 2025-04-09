using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileFlows.Shared;

/// <summary>
/// Handles registered listeners based on events
/// </summary>
public abstract class RegisterHandler
{
    /// <summary>
    /// Registered handlers
    /// </summary>
    protected readonly Dictionary<string, Delegate> _handlers = new();

    /// <summary>
    /// Registers a handler that takes a string and returns an object.
    /// </summary>
    public void Register(string name, Func<string, Task<object>> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes an object array and returns an object.
    /// </summary>
    public void Register(string name, Func<object[], Task<object>> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes an object array and returns an object.
    /// </summary>
    public void Register(string name, Func<object> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes a Guid and returns an object.
    /// </summary>
    public void Register(string name, Func<Guid, Task<object>> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes a string and returns nothing (void).
    /// </summary>
    public void Register(string name, Action<string> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes an object array and returns nothing (void).
    /// </summary>
    public void Register(string name, Action<object[]> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes a Guid and returns nothing (void).
    /// </summary>
    public void Register(string name, Action<Guid> handler) => _handlers[name] = handler;

    /// <summary>
    /// Registers a handler that takes a generic type and returns an object.
    /// </summary>
    public void Register<T>(string name, Func<T, Task<object?>> handler)
    {
        _handlers[name] = async (object[] parameters) =>
        {
            if (parameters.Length != 1)
                throw new InvalidOperationException($"Handler '{name}' expects exactly one parameter.");

            // Deserialize the parameter to the expected type (T)
            var deserializedParam = JsonSerializer.Deserialize<T>(parameters[0]?.ToString() ?? string.Empty);

            if (deserializedParam == null)
                throw new InvalidOperationException($"Failed to deserialize parameter to type {typeof(T)}.");

            return await handler(deserializedParam);
        };
    }
    /// <summary>
    /// Registers a handler that takes a generic type and returns an object.
    /// </summary>
    public void Register(string name, Func<string, Task> handler)
    {
        _handlers[name] = async (object[] parameters) =>
        {
            if (parameters.Length != 1)
                throw new InvalidOperationException($"Handler '{name}' expects exactly one parameter.");

            // Ensure the parameter is a Guid
            var str = parameters[0].ToString();
            
            // Invoke the handler and convert the result to Task<object>
            await handler(str);
        };
    }
    
    /// <summary>
    /// Registers a handler that takes a generic type
    /// </summary>
    /// <param name="name">the name of the event</param>
    /// <param name="handler">the event handler</param>
    /// <typeparam name="T">the type</typeparam>
    public void Register<T>(string name, Func<Guid, Task<T>> handler)
    {
        _handlers[name] = async (object[] parameters) =>
        {
            if (parameters.Length != 1)
                throw new InvalidOperationException($"Handler '{name}' expects exactly one parameter.");

            var value = parameters[0];
            
            if(value is JsonElement je && Guid.TryParse(value.ToString(), out var jeGuid))
                value = jeGuid;
            
            if(value is string str && Guid.TryParse(value.ToString(), out var strGuid))
               value = strGuid;

            // Ensure the parameter is a Guid
            if (value is Guid guid)
            {
                // Invoke the handler and convert the result to Task<object>
                T result = await handler(guid);
                return (object)result!; // Convert the result to an object
            }

            throw new InvalidOperationException($"Handler '{name}' expects a Guid as the parameter.");
        };
    }
    
    /// <summary>
    /// Registers a handler that takes a generic type
    /// </summary>
    /// <param name="name">the name of the event</param>
    /// <param name="handler">the event handler</param>
    /// <typeparam name="T">the input type</typeparam>
    /// <typeparam name="U">the return type</typeparam>
    public void Register<T, U>(string name, Func<T, Task<U>> handler)
    {
        _handlers[name] = async (object[] parameters) =>
        {
            if (parameters.Length != 1)
                throw new InvalidOperationException($"Handler '{name}' expects exactly one parameter.");

            var value = parameters[0];

            if (value is JsonElement je)
            {
                value = je.Deserialize<T>();
            }
            // Ensure the parameter is a Guid
            if (value is T tvalue)
            {
                // Invoke the handler and convert the result to Task<object>
                U result = await handler(tvalue);
                return (object)result!; // Convert the result to an object
            }

            throw new InvalidOperationException($"Handler '{name}' expects a {typeof(T).Name} as the parameter.");
        };
    }

    /// <summary>
    /// Registers a handler that takes a generic type and returns nothing (void).
    /// </summary>
    public void Register<T>(string name, Action<T> handler)
    {
        _handlers[name] = (object[] parameters) =>
        {
            if (parameters.Length != 1)
                throw new InvalidOperationException($"Handler '{name}' expects exactly one parameter.");

            // Deserialize the parameter to the expected type (T)
            var deserializedParam = JsonSerializer.Deserialize<T>(parameters[0]?.ToString() ?? string.Empty);

            if (deserializedParam == null)
                throw new InvalidOperationException($"Failed to deserialize parameter to type {typeof(T)}.");

            handler(deserializedParam);
            return Task.CompletedTask; // Return a completed task for consistency
        };
    }
    /// <summary>
    /// Invokes a registered handler with the given parameters.
    /// </summary>
    /// <param name="name">The name of the handler.</param>
    /// <param name="parameters">The parameters to pass to the handler.</param>
    /// <returns>The result of the handler execution or null if void.</returns>
    public async Task<object?> InvokeAsync(string name, params object[] parameters)
    {
        if (!_handlers.TryGetValue(name, out var handler))
        {
            Logger.Instance.WLog($"No handler registered for '{name}'");
            throw new InvalidOperationException($"No handler registered for '{name}'");
        }

        try
        {
            
            switch (handler)
            {
                case Func<string, Task<object>> stringFunc when parameters.Length == 1 && parameters[0] is string str:
                    return await stringFunc(str);
                case Func<object> funct: return funct();
                case Func<object[], Task<object>> objFunc:
                    return await objFunc(parameters);
                case Func<Guid, Task<object>> guidFunc when parameters.Length == 1 && parameters[0] is string guidStr:
                    return await guidFunc(Guid.Parse(guidStr));
                case Action<string> stringAction when parameters.Length == 1:
                    stringAction(parameters[0].ToString());
                    return null;
                case Action<object[]> objAction:
                    objAction(parameters);
                    return null;
                case Action<Guid> guidAction when parameters.Length == 1 && parameters[0] is string guidStr:
                    guidAction(Guid.Parse(guidStr));
                    return null;
            }

            if (handler is Func<object[], Task> objArrayTask)
            {
                await objArrayTask(parameters);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Error invoking handler '{handler}': {ex}");
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                Logger.Instance.ELog($"Error invoking handler '{handler}' : inner : {ex}");
            }
        }
        var paramTypes = string.Join(", ", parameters.Select(p => p?.GetType().FullName ?? "null"));
        Logger.Instance.WLog($"Handler '{name}' has unsupported signature. Handler type: {handler?.GetType().FullName}, parameter types: [{paramTypes}]");

        throw new InvalidOperationException($"Handler '{name}' has an unsupported signature. Handler type: {handler?.GetType().FullName}, parameter types: [{paramTypes}]");
    }
}