using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileFlows.NodeClient
{
    /// <summary>
    /// Handles registration and execution of RPC methods with flexible signatures.
    /// </summary>
    public class RpcRegister
    {
        private readonly Dictionary<string, Delegate> _handlers = new();

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
                if (parameters[0] is string str == false)
                    throw new InvalidOperationException($"Handler '{name}' expects a string as the parameter.");
                
                // Invoke the handler and convert the result to Task<object>
                await handler(str);
            };
        }
        
        public void Register<T>(string name, Func<Guid, Task<T>> handler)
        {
            _handlers[name] = async (object[] parameters) =>
            {
                if (parameters.Length != 1)
                    throw new InvalidOperationException($"Handler '{name}' expects exactly one parameter.");

                // Ensure the parameter is a Guid
                if (parameters[0] is Guid guid)
                {
                    // Invoke the handler and convert the result to Task<object>
                    T result = await handler(guid);
                    return (object)result; // Convert the result to an object
                }

                throw new InvalidOperationException($"Handler '{name}' expects a Guid as the parameter.");
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
                throw new InvalidOperationException($"No handler registered for '{name}'");

            switch (handler)
            {
                case Func<string, Task<object>> stringFunc when parameters.Length == 1 && parameters[0] is string str:
                    return await stringFunc(str);
                case Func<object> funct: return funct();
                case Func<object[], Task<object>> objFunc:
                    return await objFunc(parameters);
                case Func<Guid, Task<object>> guidFunc when parameters.Length == 1 && parameters[0] is string guidStr:
                    return await guidFunc(Guid.Parse(guidStr));
                case Action<string> stringAction when parameters.Length == 1 && parameters[0] is string str:
                    stringAction(str);
                    return null;
                case Action<object[]> objAction:
                    objAction(parameters);
                    return null;
                case Action<Guid> guidAction when parameters.Length == 1 && parameters[0] is string guidStr:
                    guidAction(Guid.Parse(guidStr));
                    return null;
                default:
                    throw new InvalidOperationException($"Handler '{name}' has an unsupported signature.");
            }
        }

        /// <summary>
        /// Handles an incoming RPC request, executing the corresponding registered method.
        /// </summary>
        /// <param name="json">The JSON string representing the request.</param>
        /// <returns>A JSON string containing the result.</returns>
        public async Task<string> HandleRequest(string json)
        {
            var request = JsonSerializer.Deserialize<RpcRequest>(json);
            if (request == null || !_handlers.TryGetValue(request.Method, out var handler))
                return JsonSerializer.Serialize(new { Result = "Unknown method" });

            // Deserialize parameters into the expected types based on the registered handler
            object? result = null;

            try
            {
                result = await InvokeAsync(request.Method, request.Params);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Result = "Error", Error = ex.Message });
            }

            return JsonSerializer.Serialize(new { Result = result });
        }
    }

    /// <summary>
    /// Represents an RPC request.
    /// </summary>
    public class RpcRequest
    {
        /// <summary>
        /// The name of the method to be called.
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// The parameters for the method call.
        /// </summary>
        public object[] Params { get; set; } = Array.Empty<object>();
    }
}