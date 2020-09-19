using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Wupoo
{
    public class WapooOptions
    {
        public AuthenticationHeaderValue Authentication { get; set; }
        public bool IgnoreMediaTypeCheck { get; set; }
        public JsonSerializerSettings JsonSerializerOptions { get; set; }
    }
}
