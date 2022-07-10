# LC6464.ASPNET.Http304

[NuGet 包](https://www.nuget.org/packages/LC6464.ASPNET.Http304 "NuGet.Org")
[GitHub 项目](https://github.com/lc6464/LC6464.ASPNET.Http304 "GitHub.Com")

在 ASP.NET 中快速设置 HTTP 304 状态码。

## 使用方法
`ExampleWebAPI.csproj`:
``` xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
		<!-- 一些东西 -->
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="LC6464.ASPNET.Http304" Version="1.0.0" />
		<!-- PackageReference，请使用 Visual Studio 或 dotnet cli 等工具添加 -->
	</ItemGroup>
	<ItemGroup>
		<Using Include="LC6464.ASPNET.Http304" />
		<!-- 一些东西 -->
	</ItemGroup>
</Project>
```

`Program.cs`:
``` csharp
var builder = WebApplication.CreateBuilder(args); // 创建 builder


// -------- 添加一些服务 --------


builder.Services.AddHttp304(); // 添加 Http304 服务


// -------- 添加另外一些服务、构建 WebApplication 等 --------
```

`ExampleController.cs`:
``` csharp
// -------- 一些 Using --------

namespace ExampleWebAPI.Controllers;
[ApiController]
[Route("[controller]")]
public class ExampleController : ControllerBase {
	private readonly ILogger<GetIPController> _logger;
	private readonly IHttp304 _http304; // 接口
	private readonly IHttpConnectionInfo _info;

	public GetIPController(ILogger<GetIPController> logger, IHttpConnectionInfo info, IHttp304 http304) { // 依赖注入
		_logger = logger;
		_info = info;
		_http304 = http304; // 赋值
	}

	[HttpGet]
	[ResponseCache(CacheProfileName = "Private1m")]
	public IP? Get() {
		var address = _info.RemoteAddress;
		_logger.LogDebug("GetIP: Client {}:{} on {}", address?.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{address}]" : address, _info.RemotePort, _info.Protocol);
		
		if (_http304.TrySet(true, _info.Protocol)) return null; // 设置 304 响应
		return new(_info);
	}
}
```