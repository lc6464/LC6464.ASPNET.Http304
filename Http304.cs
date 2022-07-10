namespace LC6464.ASPNET.Http304;
/// <summary>
/// 实现快速设置 HTTP 304 状态码的类。
/// </summary>
public class Http304 : IHttp304 {
	private readonly HttpRequest Request;
	private readonly HttpResponse Response;
	private readonly ILogger<Http304> _logger;
	private readonly IHttpConnectionInfo _info;
	private readonly string _lastModified;

	/// <summary>
	/// 使用 <paramref name="accessor"/>, <paramref name="info"/>, <paramref name="logger"/> 和 <paramref name="lastModified"/> (可选) 初始化所有属性的构造函数。
	/// </summary>
	/// <param name="accessor">用于初始化的 <see cref="IHttpContextAccessor"/></param>
	/// <param name="info">当前的 HTTP 连接信息</param>
	/// <param name="logger">用于记录日志的 <see cref="ILogger"/></param>
	/// <param name="lastModified">上次修改时间，默认为当前执行的程序集的执行文件的上次修改时间</param>
	public Http304(IHttpContextAccessor accessor, IHttpConnectionInfo info, ILogger<Http304> logger, DateTime? lastModified = null) {
		Request = accessor.HttpContext!.Request;
		Response = accessor.HttpContext.Response;
		_info = info;
		_logger = logger;
		_lastModified = (lastModified ?? File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToUniversalTime().ToString("R");
	}

	/// <summary>
	/// 强制指定是否设置 HTTP 304 状态码。
	/// </summary>
	/// <param name="isSet">若为 <see langword="true"/> 则设置</param>
	/// <returns>返回 <paramref name="isSet"/>.</returns>
	public bool Set(bool isSet = true) {
		if (isSet) {
			Response.Clear();
			// Set the 304 status code.
			Response.StatusCode = (int)HttpStatusCode.NotModified;
		}
		_logger.LogInformation("{}设置 HTTP 304.", isSet ? "已" : "未");
		return isSet;
	}

	/// <summary>
	/// HTTP 协商缓存验证客户端缓存有效性。
	/// </summary>
	/// <param name="withIP">是否带上 IP 地址</param>
	/// <param name="value">附加字符</param>
	/// <returns>如果有效，则为 <see langword="true"/>；否则为 <see langword="false"/>.</returns>
	public bool IsValid(bool withIP = false, string value = "") {

		ReadOnlySpan<char> ip = withIP ? _info.RemoteAddress?.ToString() ?? "" : "";

		StringValues clientLastModifiedHeaders = Request.Headers.IfModifiedSince,
			clientETagHeaders = Request.Headers.IfNoneMatch;
		_logger.LogDebug("验证 HTTP 协商缓存是否有效，客户端 If-Modified-Since: {}, 客户端 If-None-Match: {}.", clientLastModifiedHeaders, clientETagHeaders);
		if (clientETagHeaders.Count == 1 && clientLastModifiedHeaders.Count == 1 && clientETagHeaders[0].Length == 50 && clientLastModifiedHeaders[0] == _lastModified) {
			
			using SHA256 sha256 = SHA256.Create();
			var clientETag = clientETagHeaders[0].AsSpan(1, 48);
			ReadOnlySpan<char> clientSHA256 = string.Concat(clientETag[..22], clientETag[27..]),
				clientSalt = clientETag[22..27];

			byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(string.Concat(ip, clientSalt, value)));
			var computedHash = Convert.ToBase64String(hash)[..43];
			
			_logger.LogDebug("HTTP 协商缓存初步检查有效，计算得到的 SHA256: {}.", computedHash);

			return clientSHA256.ToString() == computedHash;
		}
		return false;
	}

	/// <summary>
	/// 验证客户端缓存有效性，若有效，则设置 HTTP 304 状态码。
	/// </summary>
	/// <param name="withIP">是否带上 IP 地址</param>
	/// <param name="value">附加字符</param>
	/// <returns>若已设置，则返回 <see langword="true"/>；否则返回 <see langword="false"/> 并向客户端输出相关响应头。</returns>
	public bool TrySet(bool withIP = false, string value = "") {
		bool isValid = IsValid(withIP, value);

		if (!isValid) { // 若无效
			ReadOnlySpan<char> ip = withIP ? _info.RemoteAddress?.ToString() ?? "" : "";

			string charList = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_+{}|:<>?-=[];',./"; // Salt 中可包含的字符列表
			StringBuilder sb = new();
			for (int i = 0; i < 5; i++) sb.Append(charList[Random.Shared.Next(charList.Length)]);
			ReadOnlySpan<char> salt = sb.ToString();
			sb.Clear();

			using var sha256 = SHA256.Create();
			ReadOnlySpan<char> hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(string.Concat(ip, salt, value))));

			Response.Headers.Add("Last-Modified", _lastModified);
			Response.Headers.Add("ETag", $"\"{string.Concat(hash[..22], salt, hash[22..43])}\"");
		}

		return Set(isValid);
	}


	/// <summary>
	/// HTTP 协商缓存验证客户端缓存有效性。
	/// </summary>
	/// <param name="value">附加字符</param>
	/// <returns>如果有效，则为 <see langword="true"/>；否则为 <see langword="false"/>.</returns>
	public bool IsValid(string value = "") => IsValid(false, value);

	/// <summary>
	/// 验证客户端缓存有效性，若有效，则设置 HTTP 304 状态码。
	/// </summary>
	/// <param name="value">附加字符</param>
	/// <returns>若已设置，则返回 <see langword="true"/>；否则返回 <see langword="false"/> 并向客户端输出相关响应头。</returns>
	public bool TrySet(string value = "") => TrySet(false, value);
}