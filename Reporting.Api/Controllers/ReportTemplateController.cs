using Microsoft.AspNetCore.Mvc;
using Reporting.Api.Services;
using Reporting.Shared.Models;

namespace Reporting.Api.Controllers;

[ApiController]
[Route("api/report-templates")]
public class ReportTemplateController : ControllerBase
{
    private readonly IReportGeneratorService _generator;
    private readonly IReportStorageService   _storage;
    private readonly ILogger<ReportTemplateController> _logger;

    public ReportTemplateController(
        IReportGeneratorService generator,
        IReportStorageService storage,
        ILogger<ReportTemplateController> logger)
    {
        _generator = generator;
        _storage   = storage;
        _logger    = logger;
    }

    // POST api/report-templates/save
    [HttpPost("save")]
    public async Task<ActionResult<SaveTemplateResponse>> Save(
        [FromBody] ReportTemplateConfig config,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.TemplateName))
            return BadRequest(new ApiError { Message = "TemplateName is required." });

        var id = await _generator.SaveTemplateAsync(config, ct);

        return Ok(new SaveTemplateResponse
        {
            Id      = id,
            Message = $"Template '{config.TemplateName}' saved successfully."
        });
    }

    // POST api/report-templates/generate  →  downloads the .trdp file
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] ReportTemplateConfig config,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.TemplateName))
            return BadRequest(new ApiError { Message = "TemplateName is required." });

        var stream   = await _generator.GenerateTrdpAsync(config, ct);
        var fileName = $"{config.TemplateName.Replace(" ", "_")}.trdp";

        return File(stream, "application/octet-stream", fileName);
    }

    // GET api/report-templates
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _storage.ListAsync(ct);
        return Ok(items.Select(i => new { i.Id, i.Name, i.CreatedAt }));
    }

    // GET api/report-templates/{id}/download
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var stream = await _storage.LoadAsync(id, ct);
        if (stream is null)
            return NotFound(new ApiError { Message = $"Template '{id}' not found." });

        return File(stream, "application/octet-stream", $"{id}.trdp");
    }
}
