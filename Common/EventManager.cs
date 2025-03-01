namespace FileFlows.Common;

using System;
using System.Collections.Generic;

/// <summary>
/// A static class that manages event broadcasting and subscription. 
/// It allows components in the application to communicate with each other through named events, 
/// even if they are in different assemblies, without direct references between them.
/// </summary>
public static class EventManager
{
    private static readonly Dictionary<string, Delegate> _eventHandlers = new();

    /// <summary>
    /// Subscribes to a named event with parameters of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="eventName">The name of the event to subscribe to.</param>
    /// <param name="handler">The handler to execute when the event is broadcasted.</param>
    public static void Subscribe<T>(string eventName, Action<T> handler)
    {
        if (_eventHandlers.ContainsKey(eventName))
        {
            _eventHandlers[eventName] = Delegate.Combine(_eventHandlers[eventName], handler);
        }
        else
        {
            _eventHandlers[eventName] = handler;
        }
    }

    /// <summary>
    /// Subscribes to a named event with no parameters.
    /// </summary>
    /// <param name="eventName">The name of the event to subscribe to.</param>
    /// <param name="handler">The handler to execute when the event is broadcasted.</param>
    public static void Subscribe(string eventName, Action handler)
    {
        if (_eventHandlers.ContainsKey(eventName))
        {
            _eventHandlers[eventName] = Delegate.Combine(_eventHandlers[eventName], handler);
        }
        else
        {
            _eventHandlers[eventName] = handler;
        }
    }

    /// <summary>
    /// Unsubscribes from a named event with parameters of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="eventName">The name of the event to unsubscribe from.</param>
    /// <param name="handler">The handler to remove.</param>
    public static void Unsubscribe<T>(string eventName, Action<T> handler)
    {
        if (_eventHandlers.ContainsKey(eventName))
        {
            var updatedHandler = Delegate.Remove(_eventHandlers[eventName], handler);

            if (updatedHandler != null)
            {
                _eventHandlers[eventName] = updatedHandler;
            }
            else
            {
                _eventHandlers.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// Unsubscribes from a named event with no parameters.
    /// </summary>
    /// <param name="eventName">The name of the event to unsubscribe from.</param>
    /// <param name="handler">The handler to remove.</param>
    public static void Unsubscribe(string eventName, Action handler)
    {
        if (_eventHandlers.ContainsKey(eventName))
        {
            var updatedHandler = Delegate.Remove(_eventHandlers[eventName], handler);

            if (updatedHandler != null)
            {
                _eventHandlers[eventName] = updatedHandler;
            }
            else
            {
                _eventHandlers.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// Broadcasts a named event with parameters of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="eventName">The name of the event to broadcast.</param>
    /// <param name="eventData">The data to pass to the event handlers.</param>
    public static void Broadcast<T>(string eventName, T eventData)
    {
        if (_eventHandlers.ContainsKey(eventName) && _eventHandlers[eventName] is Action<T> eventHandler)
        {
            eventHandler.Invoke(eventData);
        }
    }

    /// <summary>
    /// Broadcasts a named event with no parameters.
    /// </summary>
    /// <param name="eventName">The name of the event to broadcast.</param>
    public static void Broadcast(string eventName)
    {
        if (_eventHandlers.ContainsKey(eventName) && _eventHandlers[eventName] is Action eventHandler)
        {
            eventHandler.Invoke();
        }
    }
}
