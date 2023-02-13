﻿using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Wupoo;

public class WapooOptions
{
    public AuthenticationHeaderValue Authentication { get; set; }
    public bool IgnoreMediaTypeCheck { get; set; }
    public JsonSerializerSettings JsonSerializerOptions { get; set; }
}
