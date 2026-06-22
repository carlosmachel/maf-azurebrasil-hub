namespace Harness;

internal sealed class PageFetcherOptions
{
    /// <summary>
    /// Host patterns always permitted regardless of other settings.
    /// Supports wildcard prefix matching (e.g. "*.github.com").
    /// </summary>
    public IReadOnlyList<string>? AllowedHosts { get; set; }

    /// <summary>
    /// Allow fetching pages on the public internet.
    /// Default: false.
    /// </summary>
    public bool AllowPublicNetworks { get; set; }

    /// <summary>
    /// Allow fetching pages on private/loopback/link-local networks.
    /// WARNING: enables access to internal services and cloud metadata endpoints.
    /// Default: false.
    /// </summary>
    public bool AllowPrivateNetworks { get; set; }

    /// <summary>
    /// Bypass all network checks and allow any host.
    /// WARNING: disables SSRF protection — only use in isolated environments.
    /// Default: false.
    /// </summary>
    public bool AllowAllHosts { get; set; }
}