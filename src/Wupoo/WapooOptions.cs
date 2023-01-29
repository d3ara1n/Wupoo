using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Wupoo
{
    public class WapooOptions
    {
        public AuthenticationHeaderValue Authentication { get; set; }
        public bool IgnoreMediaTypeCheck { get; set; }
        public JsonSerializerSettings JsonSerializerOptions { get; set; }
    }
}