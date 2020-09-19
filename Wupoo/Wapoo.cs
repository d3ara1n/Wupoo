using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Wupoo.Exceptions;

namespace Wupoo
{
    public class Wapoo
    {
        public static WapooOptions DefaultOptons { get; private set; } = new WapooOptions();

        private readonly WapooOptions _options;
        private readonly string _url;

        private readonly List<(Type, Action<Exception>)> exceptionHandlers = new List<(Type, Action<Exception>)>();
        private readonly Dictionary<int, Func<int, bool>> codeHandlers = new Dictionary<int, Func<int, bool>>();
        private Action<string> stringResultHandler;
        private object jsonResultHandler;
        private Type jsonType;
        private Action<string, Stream> streamResultHandler;
        private HttpMethods method = HttpMethods.Get;
        private HttpContent postContent;


        public Wapoo(WapooOptions options, string url)
        {
            _options = options;
            _url = url;
        }

        public static Wapoo Wohoo(string url)
        {
            return new Wapoo(DefaultOptons, url);
        }

        public Wapoo WhenCode(int code, Func<int, bool> action)
        {
            codeHandlers.Add(code, action);
            return this;
        }

        public Wapoo WhenException<TEx>(Action<TEx> action) where TEx : Exception
        {
            exceptionHandlers.Add((typeof(TEx), (Action<Exception>)action));
            return this;
        }

        public Wapoo ForStringResult(Action<string> action)
        {
            stringResultHandler = action;
            return this;
        }

        public Wapoo ForJsonResult<TModel>(Action<TModel> action) where TModel : class
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

        public Wapoo WithJsonBody(object obj)
        {
            postContent = new StringContent(_options.JsonSerializerOptions == null ? JsonConvert.SerializeObject(obj) : JsonConvert.SerializeObject(obj, _options.JsonSerializerOptions), Encoding.UTF8, "application/json");
            return this;
        }

        public async Task FetchAsync()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Wupoo", GetType().Assembly.GetName().Version.ToString()));
            if (_options.Authentication != null)
            {
                client.DefaultRequestHeaders.Authorization = _options.Authentication;
            }
            HttpResponseMessage message;
            try
            {
                switch (method)
                {
                    case HttpMethods.Get:
                        message = await client.GetAsync(_url);
                        break;
                    case HttpMethods.Post:
                        if (postContent == null) throw new PostBodyNotGivenException();
                        message = await client.PostAsync(_url, postContent);
                        break;
                    default:
                        message = null;
                        break;
                }
                bool contiuneAfterCodeHandling = true;
                if (codeHandlers.ContainsKey((int)message.StatusCode))
                {
                    contiuneAfterCodeHandling = codeHandlers[(int)message.StatusCode]((int)message.StatusCode);
                }
                if (contiuneAfterCodeHandling)
                {
                    streamResultHandler?.Invoke(message.Content.Headers.ContentType.MediaType, await message.Content.ReadAsStreamAsync());
                    if (stringResultHandler != null && (message.Content.Headers.ContentType.MediaType == "plain/text" || _options.IgnoreMediaTypeCheck))
                    {
                        string text = await message.Content.ReadAsStringAsync();
                        stringResultHandler(text);
                    }
                    if (jsonResultHandler != null && (message.Content.Headers.ContentType.MediaType == "application/json" || _options.IgnoreMediaTypeCheck))
                    {
                        string json = await message.Content.ReadAsStringAsync();
                        object jsonObj;
                        if (jsonType == null)
                        {
                            jsonObj = _options.JsonSerializerOptions == null ? JsonConvert.DeserializeObject<dynamic>(json) : JsonConvert.DeserializeObject<dynamic>(json, _options.JsonSerializerOptions);
                        }
                        else
                        {
                            jsonObj = _options.JsonSerializerOptions == null ? JsonConvert.DeserializeObject(json, jsonType) : JsonConvert.DeserializeObject(json, jsonType, _options.JsonSerializerOptions);
                        }
                        (typeof(Action<>).MakeGenericType(jsonType)).GetMethod("Invoke").Invoke(jsonResultHandler, new object[] { jsonObj });
                    }
                }
            }
            catch (Exception e)
            {
                foreach ((Type type, Action<Exception> action) in exceptionHandlers)
                {
                    if (e.GetType().IsAssignableFrom(type))
                    {
                        action(e);
                    }
                }
            }
        }

        public void Fetch() => FetchAsync().Wait();

    }
}
