namespace Reporting.Shared.Models;

public class ReportTemplateConfig
{
    public string TemplateName        { get; set; } = string.Empty;
    public string Description         { get; set; } = string.Empty;
    public string Version             { get; set; } = "1.0";
    public ScopeConfig     Scope      { get; set; } = new();
    public List<string>    Filters    { get; set; } = new();
    public ReportHeaderConfig Header  { get; set; } = new();
    public ReportBodyConfig   Body    { get; set; } = new();
    public ReportFooterConfig Footer  { get; set; } = new();
}

public class ScopeConfig
{
    public string Sponsor          { get; set; } = string.Empty;
    public string DataType         { get; set; } = string.Empty;
    public string TherapeuticArea  { get; set; } = string.Empty;
}

public class ReportHeaderConfig
{
    public string Title         { get; set; } = string.Empty;
    public string LogoPosition  { get; set; } = "Left";
}

public class ReportBodyConfig
{
    public string FontSize    { get; set; } = "11pt";
    public string Orientation { get; set; } = "Portrait";
}

public class ReportFooterConfig
{
    public string FooterText       { get; set; } = string.Empty;
    public bool   ShowPageNumbers  { get; set; } = true;
}
