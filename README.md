# Wupoo

一行代码访问并处理http api

就像这样👇
```csharp
await Wapoo
	.Wohoo("http://example.com/login")
	.WithJsonBody(new { username = "PASSWOORD", password = "USERNAME"})
	.WhenCode(401, (code) => Console.WriteLine("账号密码搞反辽😥"))
	.ForJsonResult(result => Console.WriteLine(result.lastLogin))
	.FetchAsync();
```