// ═══════════════════════════════════════════════════════════════════════════
//  TelerikReportGeneratorService
//  Full implementation using the Telerik Reporting object model.
//
//  HOW TO ACTIVATE:
//  1. Add the Telerik NuGet source to your NuGet.config:
//       <add key="Telerik" value="https://nuget.telerik.com/v3/index.json" />
//       (requires a Telerik account — free trial available at telerik.com)
//
//  2. Uncomment the PackageReference in Reporting.Api.csproj:
//       <PackageReference Include="Telerik.Reporting" Version="18.*" />
//
//  3. In Program.cs replace the stub registration:
//       builder.Services.AddScoped<IReportGeneratorService, TelerikReportGeneratorService>();
// ═══════════════════════════════════════════════════════════════════════════

/*
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using Reporting.Shared.Models;

namespace Reporting.Api.Services;

public class TelerikReportGeneratorService : IReportGeneratorService
{
    private readonly ILogger<TelerikReportGeneratorService> _logger;
    private readonly IReportStorageService _storage;

    public TelerikReportGeneratorService(
        ILogger<TelerikReportGeneratorService> logger,
        IReportStorageService storage)
    {
        _logger  = logger;
        _storage = storage;
    }

    public async Task<Stream> GenerateTrdpAsync(ReportTemplateConfig config, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating Telerik TRDP for: {Name}", config.TemplateName);

        var report = BuildReport(config);

        var stream = new MemoryStream();
        var packager = new ReportPackager();
        packager.PackageDocument(report, stream);   // writes the .trdp ZIP
        stream.Position = 0;

        return stream;
    }

    public async Task<string> SaveTemplateAsync(ReportTemplateConfig config, CancellationToken ct = default)
    {
        var stream = await GenerateTrdpAsync(config, ct);
        return await _storage.SaveAsync(config.TemplateName, stream, ct);
    }

    // ── Report builder ───────────────────────────────────────────────────────

    private static Report BuildReport(ReportTemplateConfig config)
    {
        bool landscape = config.Body.Orientation == "Landscape";

        var report = new Report
        {
            Name  = config.TemplateName,
            Width = new Unit(landscape ? 297 : 210, UnitType.Mm)
        };

        report.PageSettings = new PageSettings
        {
            Landscape = landscape,
            PaperKind = System.Drawing.Printing.PaperKind.A4
        };

        // ── Header ──────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(config.Header.Title))
        {
            var headerSection = new ReportHeaderSection { Height = new Unit(20, UnitType.Mm) };

            var titleBox = new TextBox
            {
                Name     = "reportTitle",
                Value    = config.Header.Title,
                Location = new PointU(new Unit(0, UnitType.Mm), new Unit(2, UnitType.Mm)),
                Size     = new SizeU(new Unit(180, UnitType.Mm), new Unit(12, UnitType.Mm))
            };
            titleBox.Style.Font.Bold = true;
            titleBox.Style.Font.Size = new Unit(16, UnitType.Point);

            headerSection.Items.Add(titleBox);
            report.Items.Add(headerSection);
        }

        // ── Detail (one column per selected filter field) ────────────────────
        var detailSection = new DetailSection { Height = new Unit(8, UnitType.Mm) };
        double x = 0;

        foreach (var field in config.Filters)
        {
            var tb = new TextBox
            {
                Name     = $"col_{field.Replace(" ", "_")}",
                Value    = $"=Fields.{field.Replace(" ", "")}",
                Location = new PointU(new Unit(x, UnitType.Mm), new Unit(0, UnitType.Mm)),
                Size     = new SizeU(new Unit(40, UnitType.Mm), new Unit(7, UnitType.Mm))
            };
            detailSection.Items.Add(tb);
            x += 42;
        }

        report.Items.Add(detailSection);

        // ── Footer ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(config.Footer.FooterText) || config.Footer.ShowPageNumbers)
        {
            var footerSection = new ReportFooterSection { Height = new Unit(12, UnitType.Mm) };

            if (!string.IsNullOrWhiteSpace(config.Footer.FooterText))
            {
                footerSection.Items.Add(new TextBox
                {
                    Value    = config.Footer.FooterText,
                    Location = new PointU(new Unit(0, UnitType.Mm),  new Unit(1, UnitType.Mm)),
                    Size     = new SizeU(new Unit(140, UnitType.Mm), new Unit(6, UnitType.Mm))
                });
            }

            if (config.Footer.ShowPageNumbers)
            {
                var pageBox = new TextBox
                {
                    Value    = "= PageNumber & \" / \" & PageCount",
                    Location = new PointU(new Unit(150, UnitType.Mm), new Unit(1, UnitType.Mm)),
                    Size     = new SizeU(new Unit(40, UnitType.Mm),   new Unit(6, UnitType.Mm))
                };
                pageBox.Style.TextAlign = HorizontalAlign.Right;
                footerSection.Items.Add(pageBox);
            }

            report.Items.Add(footerSection);
        }

        // ── Report parameters (one per filter) ───────────────────────────────
        foreach (var filter in config.Filters)
        {
            var param = new ReportParameter
            {
                Name  = $"param_{filter.Replace(" ", "_")}",
                Type  = ReportParameterType.String,
                Value = string.Empty
            };
            report.ReportParameters.Add(param);
        }

        // ── SqlDataSource (wired to scope selections) ────────────────────────
        if (config.Filters.Count > 0)
        {
            var ds = new SqlDataSource
            {
                Name             = "mainDataSource",
                ConnectionString = GetConnectionString(config.Scope),
                SelectCommand    = BuildSelectQuery(config)
            };
            report.DataSource = ds;
        }

        return report;
    }

    private static string GetConnectionString(ScopeConfig scope)
    {
        // TODO: resolve real connection string from scope / sponsor selection.
        return "Data Source=.;Initial Catalog=ClinicalDB;Integrated Security=True";
    }

    private static string BuildSelectQuery(ReportTemplateConfig config)
    {
        // Build a parameterised SELECT from the user-selected filter fields.
        var cols   = config.Filters.Count > 0
            ? string.Join(", ", config.Filters.Select(f => $"[{f}]"))
            : "*";
        var wheres = config.Filters
            .Select(f => $"[{f}] LIKE @param_{f.Replace(" ", "_")}")
            .ToList();

        var sql = $"SELECT {cols} FROM [dbo].[ReportData]";
        if (wheres.Count > 0)
            sql += " WHERE " + string.Join(" AND ", wheres);

        return sql;
    }
}
*/
