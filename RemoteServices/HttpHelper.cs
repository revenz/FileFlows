using System.Net;
using System.Text;
using System.Text.Json;

namespace FileFlows.RemoteServices;

/// <summary>
/// Http Helper
/// </summary>
class HttpHelper
{
    /// <summary>
    /// The HTTP Client
    /// </summary>
    private static HttpClient _Client;

    /// <summary>
    /// Static constructor
    /// </summary>
    static HttpHelper()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        _Client = new(handler);
    }
    
    /// <summary>
    /// Performs a GET request
    /// </summary>
    /// <typeparam name="T">the type of object returned by the request</typeparam>
    /// <param name="url">the URL to call</param>
    /// <returns>the request result</returns>
    public static async Task<RequestResult<T>> Get<T>(string url)
    {
        return await MakeRequest<T>(System.Net.Http.HttpMethod.Get, url);
    }
    
    /// <summary>
    /// Performs a POST request
    /// </summary>
    /// <param name="url">the URL to call</param>
    /// <param name="data">any data to send with the request</param>
    /// <param name="timeoutSeconds">the number of seconds before a timeout occurs</param>
    /// <returns>the request result</returns>
    public static async Task<RequestResult<string>> Post(string url, object? data = null, int timeoutSeconds = 0)
    {
        return await MakeRequest<string>(System.Net.Http.HttpMethod.Post, url, data, timeoutSeconds: timeoutSeconds);
    }
    
    /// <summary>
    /// Performs a POST request
    /// </summary>
    /// <typeparam name="T">the type of object returned by the request</typeparam>
    /// <param name="url">the URL to call</param>
    /// <param name="data">any data to send with the request</param>
    /// <param name="timeoutSeconds">the number of seconds before a timeout occurs</param>
    /// <returns>the request result</returns>
    public static async Task<RequestResult<T>> Post<T>(string url, object? data = null, int timeoutSeconds = 0)
    {
        return await MakeRequest<T>(System.Net.Http.HttpMethod.Post, url, data, timeoutSeconds: timeoutSeconds);
    }
    
    /// <summary>
    /// Performs a PUT request
    /// </summary>
    /// <typeparam name="T">the type of object returned by the request</typeparam>
    /// <param name="url">the URL to call</param>
    /// <param name="data">any data to send with the request</param>
    /// <returns>the request result</returns>
    public static async Task<RequestResult<T>> Put<T>(string url, object? data = null)
    {
        return await MakeRequest<T>(System.Net.Http.HttpMethod.Put, url, data);
    }

    /// <summary>
    /// Perform a DELETE request
    /// </summary>
    /// <param name="url">the URL to call</param>
    /// <param name="data">any data to send with the request</param>
    /// <returns>the request result</returns>
    public static async Task<RequestResult<string>> Delete(string url, object? data = null)
    {
        return await MakeRequest<string>(System.Net.Http.HttpMethod.Delete, url, data);
    }
    
    
    /// <summary>
    /// Converts an object to a json string content result
    /// </summary>
    /// <param name="o">the object to convert</param>
    /// <returns>the object as a json string content</returns>
    private static StringContent AsJson(object o)
    {
        string json = o.ToJson();
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Makes a HTTP Request
    /// </summary>
    /// <param name="method">The request method</param>
    /// <param name="url">The URL of the request</param>
    /// <param name="data">Any data to be sent with the request</param>
    /// <param name="timeoutSeconds">the number of seconds to wait before a timeout</param>
    /// <typeparam name="T">The request object returned</typeparam>
    /// <returns>a processing result of the request</returns>
    private static async Task<RequestResult<T>> MakeRequest<T>(System.Net.Http.HttpMethod method, string url, object? data = null, int timeoutSeconds = 0)
    {
#if (DEBUG)
        if (url.Contains("i18n") == false && url.StartsWith("http") == false)
            url = "http://localhost:6868" + url;
#endif
        var request = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(url, UriKind.RelativeOrAbsolute),
            Content = data != null ? AsJson(data) : null
        };
        
        if(string.IsNullOrWhiteSpace(ServerGlobals.AccessToken) == false)
            request.Headers.Add("x-token", ServerGlobals.AccessToken);
        request.Headers.Add("x-node", RemoteService.NodeUid.ToString());

        if (method == System.Net.Http.HttpMethod.Post && data == null)
        {
            // if this is null, asp.net will return a 415 content not support, as the content-type will not be set
            request.Content = new StringContent("", Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        if (timeoutSeconds > 0)
        {
            using var cancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            response = await _Client.SendAsync(request, cancelToken.Token);
        }
        else
            response = await _Client.SendAsync(request);

        if (typeof(T) == typeof(byte[]))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (response.IsSuccessStatusCode)
                return new RequestResult<T> { Success = true, Data = (T)(object)bytes, StatusCode = response.StatusCode, Headers = GetHeaders(response) };
            return new RequestResult<T> { Success = false, Headers = GetHeaders(response), StatusCode = response.StatusCode };
        }

        string body = await response.Content.ReadAsStringAsync();

        if (body.Contains("~/index.html"))
            throw new Exception("Invalid route: " + url);

        if (response.IsSuccessStatusCode &&
            (body.Contains("INFO") == false && body.Contains("An unhandled error has occurred.")) == false)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new Validators.ValidatorConverter() }
            };
#pragma warning disable CS8600
            T result = string.IsNullOrEmpty(body) ? default(T) :
                typeof(T) == typeof(string) ? (T)(object)body : JsonSerializer.Deserialize<T>(body, options);
#pragma warning restore CS8600
            return new RequestResult<T> { Success = true, Body = body, Data = result, StatusCode = response.StatusCode, Headers = GetHeaders(response) };
        }
        else
        {
            if (body.Contains("An unhandled error has occurred."))
                body = "An unhandled error has occurred."; // asp.net error
            else if (body.Contains("502 Bad Gateway"))
            {
                body = "Unable to connect, server possibly down";
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new Exception("Unauthorized");

            return new RequestResult<T>
                { Success = false, Body = body, Data = default(T), StatusCode = response.StatusCode, Headers = GetHeaders(response) };
        }


        Dictionary<string, string> GetHeaders(HttpResponseMessage response)
        {
            return response.Headers.Where(x => x.Key.StartsWith("x-"))
                .DistinctBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value.FirstOrDefault() ?? string.Empty);
        }
    }
}