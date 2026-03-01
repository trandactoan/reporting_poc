using System.IO.Compression;
using System.Text;
using Reporting.Shared.Models;

namespace Reporting.Api.Services;

/// <summary>
/// Stub implementation — generates a minimal TRDP-like ZIP without requiring
/// a Telerik license. Replace with TelerikReportGeneratorService once Telerik
/// NuGet feed is configured.
/// </summary>
public class StubReportGeneratorService : IReportGeneratorService
{
    private readonly ILogger<StubReportGeneratorService> _logger;
    private readonly IReportStorageService _storage;

    public StubReportGeneratorService(
        ILogger<StubReportGeneratorService> logger,
        IReportStorageService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public Task<Stream> GenerateTrdpAsync(ReportTemplateConfig config, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating stub TRDP for template: {Name}", config.TemplateName);

        // TRDP is a ZIP archive containing a report XML definition.
        var outputStream = new MemoryStream();

        using (var zip = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var xmlEntry = zip.CreateEntry("definition.xml", CompressionLevel.Optimal);
            using var writer = new StreamWriter(xmlEntry.Open(), Encoding.UTF8);
            writer.Write(BuildReportXml(config));
        }

        outputStream.Position = 0;
        return Task.FromResult<Stream>(outputStream);
    }

    public async Task<string> SaveTemplateAsync(ReportTemplateConfig config, CancellationToken ct = default)
    {
        var stream = await GenerateTrdpAsync(config, ct);
        var id = await _storage.SaveAsync(config.TemplateName, stream, ct);
        _logger.LogInformation("Saved template {Name} → {Id}", config.TemplateName, id);
        return id;
    }

    // ── XML builder ─────────────────────────────────────────────────────────

    private static string BuildReportXml(ReportTemplateConfig config)
    {
        var orientation = config.Body.Orientation == "Landscape" ? "Landscape" : "Portrait";
        var pageWidth   = orientation == "Landscape" ? "297mm" : "210mm";
        var pageHeight  = orientation == "Landscape" ? "210mm" : "297mm";

        var filters  = BuildFiltersXml(config.Filters);
        var columns  = BuildDetailColumnsXml(config.Filters);
        var header   = BuildHeaderXml(config.Header);
        var footer   = BuildFooterXml(config.Footer);

        return $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <Report Name="{Escape(config.TemplateName)}"
                    Width="{pageWidth}"
                    xmlns="http://schemas.telerik.com/reporting/2012/3.7">
              <Report.ReportParameters>
                {filters}
              </Report.ReportParameters>
              <Items>
                {header}
                <DetailSection Name="detail" Height="25mm">
                  <Items>
                    {columns}
                  </Items>
                </DetailSection>
                {footer}
              </Items>
              <PageSettings>
                <PageSettings PaperKind="A4" Landscape="{(orientation == "Landscape").ToString().ToLower()}" />
              </PageSettings>
            </Report>
            """;
    }

    private static string BuildFiltersXml(List<string> filters)
    {
        if (filters.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var f in filters)
        {
            var safe = Escape(f.Replace(" ", "_"));
            sb.AppendLine($"""
                    <ReportParameter Name="param_{safe}" Type="System.String">
                      <ReportParameter.Value>=Parameters.param_{safe}.Value</ReportParameter.Value>
                    </ReportParameter>
                """);
        }
        return sb.ToString();
    }

    private static string BuildDetailColumnsXml(List<string> filters)
    {
        if (filters.Count == 0) return string.Empty;

        var sb  = new StringBuilder();
        var x   = 0;
        var col = 120; // mm per column

        foreach (var f in filters)
        {
            sb.AppendLine($"""
                    <TextBox Name="tb_{Escape(f.Replace(" ", "_"))}"
                             Value="=Fields.{Escape(f.Replace(" ", ""))}"
                             Location="{x}mm,0mm"
                             Size="{col - 2}mm,8mm" />
                """);
            x += col;
        }
        return sb.ToString();
    }

    private static string BuildHeaderXml(ReportHeaderConfig header)
    {
        if (string.IsNullOrWhiteSpace(header.Title)) return string.Empty;

        return $"""
                <ReportHeaderSection Name="reportHeader" Height="20mm">
                  <Items>
                    <TextBox Name="reportTitle"
                             Value="{Escape(header.Title)}"
                             Location="0mm,2mm"
                             Size="180mm,12mm"
                             Style.Font.Bold="True"
                             Style.Font.Size="16pt"
                             Style.TextAlign="{header.LogoPosition}" />
                  </Items>
                </ReportHeaderSection>
                """;
    }

    private static string BuildFooterXml(ReportFooterConfig footer)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(footer.FooterText))
            lines.Add($"""
                        <TextBox Name="footerText" Value="{Escape(footer.FooterText)}"
                                 Location="0mm,1mm" Size="140mm,6mm" />
                    """);

        if (footer.ShowPageNumbers)
            lines.Add("""
                        <TextBox Name="pageNumber" Value="=PageNumber &amp; ' / ' &amp; PageCount"
                                 Location="150mm,1mm" Size="40mm,6mm"
                                 Style.TextAlign="Right" />
                    """);

        if (lines.Count == 0) return string.Empty;

        return $"""
                <ReportFooterSection Name="reportFooter" Height="12mm">
                  <Items>
                    {string.Join(Environment.NewLine, lines)}
                  </Items>
                </ReportFooterSection>
                """;
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");
}
