using Reporting.Shared.Models;

namespace Reporting.Api.Services;

public interface IReportGeneratorService
{
    /// <summary>
    /// Generates a TRDX (plain XML) file stream from the given template configuration.
    /// </summary>
    Task<Stream> GenerateTrdxAsync(ReportTemplateConfig config, CancellationToken ct = default);

    /// <summary>
    /// Saves the template configuration and returns a unique identifier.
    /// </summary>
    Task<string> SaveTemplateAsync(ReportTemplateConfig config, CancellationToken ct = default);
}
