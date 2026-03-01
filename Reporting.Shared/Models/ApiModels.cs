namespace Reporting.Shared.Models;

public class SaveTemplateResponse
{
    public string Id      { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class GenerateReportResponse
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName    { get; set; } = string.Empty;
    public string Message     { get; set; } = string.Empty;
}

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
