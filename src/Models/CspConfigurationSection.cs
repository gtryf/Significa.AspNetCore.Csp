namespace Significa.AspNetCore.Csp;

public class CspConfigurationSection
{
	public const string Name = "CspConfiguration";

	public string? NoncePlaceholder { get; set; }
	public bool ReportOnly { get; set; }
	public string? ReportUri { get; set; }
	public string? ReportTo { get; set; }

	public CspSource? Default { get; set; }
	public CspSource? Script { get; set; }
	public CspSource? Style { get; set; }
	public CspSource? Image { get; set; }
	public CspSource? Connect { get; set; }
	public CspSource? Font { get; set; }
	public CspSource? Object { get; set; }
	public CspSource? Media { get; set; }
	public CspSource? Frame { get; set; }
	public CspSource? Child { get; set; }
	public CspSource? Manifest { get; set; }

	public CspSource? BaseUri { get; set; }
	public CspSource? FormActions { get; set; }
	public CspSource? FrameAncestors { get; set; }

	public bool BlockMixedContent { get; set; }
	public bool UpgradeInsecureRequests { get; set; }
}

public class CspSource
{
	public IEnumerable<string>? Sources { get; set; }
	public bool UseNonce { get; set; }
}
