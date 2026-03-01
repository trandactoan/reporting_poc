namespace Reporting.Api.Services;

/// <summary>
/// Stores TRDX files on the local file system under /storage/trdx/.
/// Swap out for a database or blob-storage implementation in production.
/// </summary>
public class LocalReportStorageService : IReportStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalReportStorageService> _logger;

    public LocalReportStorageService(
        IConfiguration config,
        ILogger<LocalReportStorageService> logger)
    {
        _logger   = logger;
        _basePath = config["Storage:TrdxPath"]
                    ?? Path.Combine(AppContext.BaseDirectory, "storage", "trdx");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(string name, Stream content, CancellationToken ct = default)
    {
        var id       = Guid.NewGuid().ToString("N");
        var safeName = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        var fileName = $"{id}_{safeName}.trdx";
        var filePath = Path.Combine(_basePath, fileName);

        await using var file = File.Create(filePath);
        await content.CopyToAsync(file, ct);

        _logger.LogInformation("Stored TRDX → {Path}", filePath);
        return id;
    }

    public Task<Stream?> LoadAsync(string id, CancellationToken ct = default)
    {
        var match = Directory.GetFiles(_basePath, $"{id}_*.trdx").FirstOrDefault();
        if (match is null) return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(match);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<IEnumerable<(string Id, string Name, DateTime CreatedAt)>> ListAsync(CancellationToken ct = default)
    {
        var result = Directory.GetFiles(_basePath, "*.trdx")
            .Select(f =>
            {
                var info  = new FileInfo(f);
                var parts = info.GetFileNameWithoutExtension().Split('_', 2);
                var id    = parts[0];
                var name  = parts.Length > 1 ? parts[1] : info.Name;
                return (id, name, info.CreationTime);
            });

        return Task.FromResult(result);
    }
}

internal static class FileInfoExtensions
{
    internal static string GetFileNameWithoutExtension(this FileInfo fi) =>
        Path.GetFileNameWithoutExtension(fi.Name);
}
