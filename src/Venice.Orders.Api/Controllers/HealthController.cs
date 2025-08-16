using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Venice.Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthChecks;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public HealthController(HealthCheckService healthChecks)
        => _healthChecks = healthChecks;

    [HttpGet("live")]
    public async Task<IActionResult> Live(CancellationToken ct)
        => await ExecuteAsync(entry => entry.Tags.Contains("live"), ct);

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
        => await ExecuteAsync(entry => entry.Tags.Contains("ready"), ct);

    [HttpGet]
    public async Task<IActionResult> All(CancellationToken ct)
        => await ExecuteAsync(_ => true, ct);

    private async Task<IActionResult> ExecuteAsync(Func<HealthCheckRegistration, bool> predicate, CancellationToken ct)
    {
        var report = await _healthChecks.CheckHealthAsync(predicate, ct);

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var e = kvp.Value;
                    return new
                    {
                        status = e.Status.ToString(),
                        duration = e.Duration,
                        description = e.Description,
                        tags = e.Tags,
                        data = e.Data.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase)
                    };
                })
        };

        var statusCode = report.Status switch
        {
            HealthStatus.Healthy => 200,
            HealthStatus.Degraded => 200, // pode optar por 503 se preferir
            _ => 503
        };

        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(response, _json)
        };
    }
}
