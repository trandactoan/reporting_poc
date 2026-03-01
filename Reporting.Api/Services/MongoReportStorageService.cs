using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Reporting.Api.Services;

public class MongoReportStorageService : IReportStorageService
{
    private readonly IMongoCollection<ReportTemplateDocument> _collection;
    private readonly ILogger<MongoReportStorageService> _logger;

    public MongoReportStorageService(
        IConfiguration config,
        ILogger<MongoReportStorageService> logger)
    {
        _logger = logger;

        var connectionString = config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName     = config["MongoDB:Database"]         ?? "Reporting";
        var collectionName   = config["MongoDB:Collection"]       ?? "Reporting";

        var client = new MongoClient(connectionString);
        var db     = client.GetDatabase(databaseName);
        _collection = db.GetCollection<ReportTemplateDocument>(collectionName);
    }

    public async Task<string> SaveAsync(string name, Stream content, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);

        var doc = new ReportTemplateDocument
        {
            Id        = Guid.NewGuid().ToString("N"),
            Name      = name,
            CreatedAt = DateTime.UtcNow,
            TrdpData  = ms.ToArray()
        };

        await _collection.InsertOneAsync(doc, cancellationToken: ct);
        _logger.LogInformation("Stored template '{Name}' → MongoDB id {Id}", name, doc.Id);
        return doc.Id;
    }

    public async Task<Stream?> LoadAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ReportTemplateDocument>.Filter.Eq(d => d.Id, id);
        var doc    = await _collection.Find(filter).FirstOrDefaultAsync(ct);

        return doc is null ? null : new MemoryStream(doc.TrdpData);
    }

    public async Task<IEnumerable<(string Id, string Name, DateTime CreatedAt)>> ListAsync(CancellationToken ct = default)
    {
        var projection = Builders<ReportTemplateDocument>.Projection
            .Include(d => d.Id)
            .Include(d => d.Name)
            .Include(d => d.CreatedAt)
            .Exclude(d => d.TrdpData);

        var docs = await _collection
            .Find(Builders<ReportTemplateDocument>.Filter.Empty)
            .Project<ReportTemplateDocument>(projection)
            .SortByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(d => (d.Id, d.Name, d.CreatedAt));
    }
}

public class ReportTemplateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("trdpData")]
    public byte[] TrdpData { get; set; } = Array.Empty<byte>();
}
