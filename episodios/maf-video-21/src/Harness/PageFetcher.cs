using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;


namespace  Harness;

internal sealed partial class PageFetcher : AIFunction
{
    private static readonly HttpClient HttpClient = new();
    private readonly AIFunction _inner;
    private readonly PageFetcherOptions _options;

    public PageFetcher(PageFetcherOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _inner = AIFunctionFactory.Create(FetchPageAsync);
    }

    public override string Name => _inner.Name;
    public override string Description => _inner.Description;
    public override JsonElement JsonSchema => _inner.JsonSchema;

    protected override ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken) =>
        _inner.InvokeAsync(arguments, cancellationToken);

    [Description("Download a web page and return its content as plain text")]
    private async Task<string> FetchPageAsync(
        [Description("The web address to fetch")] string url,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return $"Error: '{url}' is not a valid URL.";

        if (uri.Scheme is not "http" and not "https")
            return $"Error: Only HTTP and HTTPS are supported. Got '{uri.Scheme}'.";

        string? blocked = await CheckAccessAsync(uri, cancellationToken);
        if (blocked is not null)
            return blocked;

        try
        {
            string html = await HttpClient.GetStringAsync(uri, cancellationToken);
            return MarkdownConverter.ToText(html);
        }
        catch (HttpRequestException ex)
        {
            return $"Error fetching {url}: {ex.Message}";
        }
    }

    private async Task<string?> CheckAccessAsync(Uri uri, CancellationToken ct)
    {
        string host = uri.Host;

        if (_options.AllowedHosts is { Count: > 0 } hosts)
        {
            foreach (string pattern in hosts)
                if (MatchesHost(host, pattern)) return null;
        }

        if (_options is { AllowPublicNetworks: false, AllowPrivateNetworks: false, AllowAllHosts: false })
            return $"Error: Access to '{host}' is blocked by the current policy. Configure PageFetcherOptions to allow access.";

        IPAddress[] addrs;
        try
        {
            addrs = await Dns.GetHostAddressesAsync(host, ct);
        }
        catch (SocketException)
        {
            return $"Error: Could not resolve host '{host}'.";
        }

        if (addrs.Length == 0)
            return $"Error: Could not resolve host '{host}'.";

        bool isPrivate = Array.Exists(addrs, IsPrivate);

        if (!isPrivate && _options.AllowPublicNetworks) return null;
        if (isPrivate && _options.AllowPrivateNetworks) return null;
        if (_options.AllowAllHosts) return null;

        string network = isPrivate ? "private/internal" : "public";
        return $"Error: '{host}' is on a {network} network which is not permitted by the current policy.";
    }

    private static bool MatchesHost(string host, string pattern)
    {
        if (string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase)) return true;
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
            return host.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private static bool IsPrivate(IPAddress addr)
    {
        if (addr.IsIPv4MappedToIPv6) addr = addr.MapToIPv4();
        if (IPAddress.IsLoopback(addr)) return true;

        if (addr.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] b = addr.GetAddressBytes();
            return b[0] switch
            {
                10 => true,                          // 10.0.0.0/8
                172 => b[1] is >= 16 and <= 31,      // 172.16.0.0/12
                192 => b[1] == 168,                  // 192.168.0.0/16
                169 => b[1] == 254,                  // 169.254.0.0/16  (link-local / metadata)
                _ => false
            };
        }

        if (addr.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] b = addr.GetAddressBytes();
            if (b[0] == 0xfe && (b[1] & 0xc0) == 0x80) return true;  // fe80::/10 link-local
            if ((b[0] & 0xfe) == 0xfc) return true;                   // fc00::/7  unique local
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // HTML → plain-text Markdown converter (no external dependencies)
    // -------------------------------------------------------------------------
    private static partial class MarkdownConverter
    {
        public static string ToText(string html)
        {
            var body = BodyRegex().Match(html);
            string s = body.Success ? body.Groups[1].Value : html;

            s = ScriptRegex().Replace(s, string.Empty);
            s = StyleRegex().Replace(s, string.Empty);
            s = HeadRegex().Replace(s, string.Empty);
            s = CommentRegex().Replace(s, string.Empty);

            s = H1Regex().Replace(s, m => $"\n# {Strip(m.Groups[1].Value)}\n");
            s = H2Regex().Replace(s, m => $"\n## {Strip(m.Groups[1].Value)}\n");
            s = H3Regex().Replace(s, m => $"\n### {Strip(m.Groups[1].Value)}\n");
            s = H4Regex().Replace(s, m => $"\n#### {Strip(m.Groups[1].Value)}\n");
            s = H5Regex().Replace(s, m => $"\n##### {Strip(m.Groups[1].Value)}\n");
            s = H6Regex().Replace(s, m => $"\n###### {Strip(m.Groups[1].Value)}\n");

            s = PreRegex().Replace(s, m => $"\n```\n{Strip(m.Groups[1].Value)}\n```\n");
            s = BlockquoteRegex().Replace(s, m =>
                "\n" + string.Join("\n", Strip(m.Groups[1].Value).Split('\n')
                    .Select(l => $"> {l.Trim()}")) + "\n");

            s = UlRegex().Replace(s, m =>
                "\n" + LiRegex().Replace(m.Groups[1].Value, li => $"- {Strip(li.Groups[1].Value)}\n"));

            var n = 0;
            s = OlRegex().Replace(s, m =>
            {
                n = 0;
                return "\n" + LiRegex().Replace(m.Groups[1].Value, li => $"{++n}. {Strip(li.Groups[1].Value)}\n");
            });

            s = HrRegex().Replace(s, "\n---\n");

            s = LinkRegex().Replace(s, m =>
            {
                string href = m.Groups[1].Value;
                string label = Strip(m.Groups[2].Value);
                if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    return label;
                return string.IsNullOrWhiteSpace(label) ? string.Empty : $"[{label}]({href})";
            });

            s = ImageRegex().Replace(s, m => $"![{m.Groups[2].Value}]({m.Groups[1].Value})");
            s = BoldRegex().Replace(s, m => $"**{m.Groups[2].Value}**");
            s = ItalicRegex().Replace(s, m => $"*{m.Groups[2].Value}*");
            s = CodeRegex().Replace(s, m => $"`{m.Groups[1].Value}`");
            s = ParaRegex().Replace(s, m => $"\n\n{m.Groups[1].Value}\n\n");
            s = BrRegex().Replace(s, "\n");
            s = TagsRegex().Replace(s, string.Empty);
            s = WebUtility.HtmlDecode(s);
            s = NewlinesRegex().Replace(s, "\n\n");
            return s.Trim();
        }

        private static string Strip(string s) => TagsRegex().Replace(s, string.Empty).Trim();

        [GeneratedRegex(@"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex BodyRegex();
        [GeneratedRegex(@"<script[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex ScriptRegex();
        [GeneratedRegex(@"<style[^>]*>.*?</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex StyleRegex();
        [GeneratedRegex(@"<head[^>]*>.*?</head>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex HeadRegex();
        [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
        private static partial Regex CommentRegex();
        [GeneratedRegex(@"<h1[^>]*>(.*?)</h1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex H1Regex();
        [GeneratedRegex(@"<h2[^>]*>(.*?)</h2>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex H2Regex();
        [GeneratedRegex(@"<h3[^>]*>(.*?)</h3>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex H3Regex();
        [GeneratedRegex(@"<h4[^>]*>(.*?)</h4>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex H4Regex();
        [GeneratedRegex(@"<h5[^>]*>(.*?)</h5>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex H5Regex();
        [GeneratedRegex(@"<h6[^>]*>(.*?)</h6>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex H6Regex();
        [GeneratedRegex(@"<pre[^>]*>(.*?)</pre>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex PreRegex();
        [GeneratedRegex(@"<blockquote[^>]*>(.*?)</blockquote>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex BlockquoteRegex();
        [GeneratedRegex(@"<ul[^>]*>(.*?)</ul>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex UlRegex();
        [GeneratedRegex(@"<ol[^>]*>(.*?)</ol>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex OlRegex();
        [GeneratedRegex(@"<li[^>]*>(.*?)</li>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex LiRegex();
        [GeneratedRegex(@"<hr\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex HrRegex();
        [GeneratedRegex(@"<a\s[^>]*href=[""']([^""']*)[""'][^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex LinkRegex();
        [GeneratedRegex(@"<img\s[^>]*src=[""']([^""']*)[""'][^>]*?(?:alt=[""']([^""']*)[""'])?[^>]*/?>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex ImageRegex();
        [GeneratedRegex(@"<(strong|b)\b[^>]*>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex BoldRegex();
        [GeneratedRegex(@"<(em|i)\b[^>]*>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex ItalicRegex();
        [GeneratedRegex(@"<code[^>]*>(.*?)</code>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex CodeRegex();
        [GeneratedRegex(@"<p[^>]*>(.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex ParaRegex();
        [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex BrRegex();
        [GeneratedRegex(@"<[^>]+>")]
        private static partial Regex TagsRegex();
        [GeneratedRegex(@"\n{3,}")]
        private static partial Regex NewlinesRegex();
    }
}
