using System.Net.Http.Json;
using Reporting.Shared.Models;

namespace Reporting.UI.Services;

/// <summary>
/// Thin HTTP client wrapper that calls Reporting.Api endpoints.
/// The base URL is configured in wwwroot/appsettings.json.
/// </summary>
public class ReportApiService
{
    private readonly HttpClient _http;

    public ReportApiService(HttpClient http) => _http = http;

    /// <summary>Saves the template and returns the assigned id.</summary>
    public async Task<SaveTemplateResponse?> SaveAsync(ReportTemplateConfig config)
    {
        var response = await _http.PostAsJsonAsync("api/report-templates/save", config);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SaveTemplateResponse>();
    }

    /// <summary>Generates a TRDP and triggers a browser download.</summary>
    public async Task<byte[]> GenerateTrdpAsync(ReportTemplateConfig config)
    {
        var response = await _http.PostAsJsonAsync("api/report-templates/generate", config);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>Lists all previously saved templates.</summary>
    public async Task<List<SavedTemplateItem>?> ListAsync()
    {
        return await _http.GetFromJsonAsync<List<SavedTemplateItem>>("api/report-templates");
    }
}

public record SavedTemplateItem(string Id, string Name, DateTime CreatedAt);
