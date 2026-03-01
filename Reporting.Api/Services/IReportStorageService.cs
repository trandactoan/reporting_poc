namespace Reporting.Api.Services;

public interface IReportStorageService
{
    Task<string> SaveAsync(string name, Stream content, CancellationToken ct = default);
    Task<Stream?> LoadAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<(string Id, string Name, DateTime CreatedAt)>> ListAsync(CancellationToken ct = default);
}
