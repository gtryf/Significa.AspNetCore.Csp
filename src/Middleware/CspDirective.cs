namespace Significa.AspNetCore.Csp.Middleware;

internal class CspDirective
{
    public string Name { get; }
    public string Value { get; }

    public CspDirective(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
