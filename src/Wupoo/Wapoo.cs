using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Wupoo;

public class Wapoo
{
    private readonly WapooOptions _options;
    private readonly string _url;
    private readonly Dictionary<int, Func<int, bool>> codeHandlers = new();

    private readonly List<(Type, Action<Exception>)> exceptionHandlers = new();
    private object jsonResultHandler;
    private Type jsonType;
    private HttpMethods method = HttpMethods.Get;
    private HttpContent postContent;
    private Action<string, Stream> streamResultHandler;
    private Action<string> stringResultHandler;

    public Wapoo(WapooOptions options, string url)
    {
        _options = options;
        _url = url;
    }

    public static WapooOptions DefaultOptions { get; } = new();

    public static Wapoo Wohoo(string url)
    {
        return new Wapoo(DefaultOptions, url);
    }

    public static Wapoo Wohoo(string url, WapooOptions options)
    {
        return new Wapoo(options, url);
    }

    public Wapoo WhenCode(int code, Func<int, bool> action)
    {
        codeHandlers.Add(code, action);
        return this;
    }

    public Wapoo WhenException<TEx>(Action<TEx> action)
        where TEx : Exception
    {
        exceptionHandlers.Add((typeof(TEx), (Action<Exception>)action));
        return this;
    }

    public Wapoo ForStringResult(Action<string> action)
    {
        stringResultHandler = action;
        return this;
    }

    public Wapoo ForJsonResult<TModel>(Action<TModel> action)
    {
        jsonType = typeof(TModel);
        jsonResultHandler = action;
        return this;
    }

    public Wapoo ForJsonResult(Action<dynamic> action)
    {
        jsonResultHandler = action;
        return this;
    }

    public Wapoo ForAnyResult(Action<string, Stream> action)
    {
        streamResultHandler = action;
        return this;
    }

    public Wapoo UseBearer(string token)
    {
        _options.Authentication = new AuthenticationHeaderValue("Bearer", token);
        return this;
    }

    public Wapoo ViaPost()
    {
        method = HttpMethods.Post;
        return this;
    }

    public Wapoo ViaGet()
    {
        method = HttpMethods.Get;
        return this;
    }

    public Wapoo WithBody(HttpContent content)
    {
        postContent = content;
        return this;
    }

    public Wapoo WithJsonBody(object obj) => WithJsonBody(obj, "application/json");

    public Wapoo WithJsonBody(object obj, string mediaType)
    {
        var content =
           _options.JsonSerializerOptions == null
               ? JsonConvert.SerializeObject(obj)
               : JsonConvert.SerializeObject(obj, _options.JsonSerializerOptions);
        postContent = new StringContent(content,
            Encoding.UTF8,
            mediaType
        );
        return this;
    }

    public async Task FetchAsync()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Wupoo", GetType().Assembly.GetName().Version!.ToString())
        );
        if (_options.Authentication != null)
            client.DefaultRequestHeaders.Authorization = _options.Authentication;
        HttpResponseMessage message;
        try
        {
            switch (method)
            {
                case HttpMethods.Get:
                    message = await client.GetAsync(_url);
                    break;

                case HttpMethods.Post:
                    if (postContent == null)
                        WithJsonBody(new object());
                    message = await client.PostAsync(_url, postContent);
                    break;

                default:
                    message = null;
                    break;
            }

            var contiuneAfterCodeHandling = true;
            if (codeHandlers.ContainsKey((int)message!.StatusCode))
                contiuneAfterCodeHandling = codeHandlers[(int)message.StatusCode](
                    (int)message.StatusCode
                );
            if (contiuneAfterCodeHandling)
            {
                streamResultHandler?.Invoke(
                    message.Content.Headers.ContentType!.MediaType,
                    await message.Content.ReadAsStreamAsync()
                );
                if (
                    stringResultHandler != null
                    && (
                        message.Content.Headers.ContentType!.MediaType == "text/plain"
                        || _options.IgnoreMediaTypeCheck
                    )
                )
                {
                    var text = await message.Content.ReadAsStringAsync();
                    stringResultHandler(text);
                }

                if (
                    jsonResultHandler != null
                    && (
                        message.Content.Headers.ContentType!.MediaType == "application/json"
                        || _options.IgnoreMediaTypeCheck
                    )
                )
                {
                    var json = await message.Content.ReadAsStringAsync();
                    object jsonObj;
                    if (jsonType == null)
                        jsonObj =
                            _options.JsonSerializerOptions == null
                                ? JsonConvert.DeserializeObject<dynamic>(json)
                                : JsonConvert.DeserializeObject<dynamic>(
                                    json,
                                    _options.JsonSerializerOptions
                                );
                    else
                        jsonObj =
                            _options.JsonSerializerOptions == null
                                ? JsonConvert.DeserializeObject(json, jsonType)
                                : JsonConvert.DeserializeObject(
                                    json,
                                    jsonType,
                                    _options.JsonSerializerOptions
                                );
                    (jsonType == null ? typeof(Action<dynamic>) : typeof(Action<>).MakeGenericType(jsonType))
                        .GetMethod("Invoke")
                        ?.Invoke(jsonResultHandler, new[] { jsonObj });
                }
            }
        }
        catch (Exception e)
        {
            foreach (var (type, action) in exceptionHandlers)
                if (e.GetType().IsAssignableTo(type))
                    action(e);
        }
    }

    public void Fetch()
    {
        FetchAsync().Wait();
    }
}
