namespace Significa.AspNetCore.Csp.Middleware;

internal class CspDirectiveBuilder
{
    private readonly string? _nonce;
    private readonly CspConfigurationSection _config;
    private readonly List<CspDirective> _directives = new();

    public CspDirectiveBuilder(CspConfigurationSection config, string? nonce)
    {
        _config = config;
        _nonce = nonce;
    }

    private string BuildSourceValue(CspSource source)
    {
        var parts = new List<string>();
        
        if ((source.Sources?.Any() ?? false) is false && !source.UseNonce)
			return "'none'";

		parts.Add(string.Join(" ", source.Sources ?? []));

		if (source.UseNonce && _nonce is not null)
            parts.Add($"'nonce-{_nonce}'");

        return string.Join(" ", parts);
    }

    public string Build()
    {
		AddSourceDirective("default-src", _config.Default);
		AddSourceDirective("script-src", _config.Script);
        AddSourceDirective("img-src", _config.Image);
        AddSourceDirective("font-src", _config.Font);
        AddSourceDirective("connect-src", _config.Connect);
        AddSourceDirective("style-src", _config.Style);
        AddSourceDirective("object-src", _config.Object);
		AddSourceDirective("media-src", _config.Media);
		AddSourceDirective("frame-src", _config.Frame);
		AddSourceDirective("child-src", _config.Child);
		AddSourceDirective("manifest-src", _config.Manifest);

		AddSourceDirective("base-uri", _config.BaseUri);
		AddSourceDirective("form-action", _config.FormActions);
		AddSourceDirective("frame-ancestors", _config.FrameAncestors);
        
        if (_config.BlockMixedContent)
            _directives.Add(new CspDirective("block-all-mixed-content", string.Empty));
            
        if (_config.UpgradeInsecureRequests)
            _directives.Add(new CspDirective("upgrade-insecure-requests", string.Empty));
            
        AddSimpleDirective("report-uri", _config.ReportUri);
        AddSimpleDirective("report-to", _config.ReportTo);

        return string.Join("", _directives.Select(d => $"{d.Name} {d.Value};".TrimEnd()));
    }

    private void AddSourceDirective(string name, CspSource? source)
    {
        if (source is null) return;
        var value = BuildSourceValue(source);
        _directives.Add(new CspDirective(name, value));
    }

    private void AddSimpleDirective(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _directives.Add(new CspDirective(name, value));
    }
}
