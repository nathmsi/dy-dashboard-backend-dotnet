using System.ComponentModel.DataAnnotations;

namespace DyDashboard.Api.Configuration;

/// <summary>
/// Centralized, validated configuration. Bound from the "Api" section (and
/// environment variables) and validated on startup, so the process fails fast if
/// any value is missing or malformed — the .NET counterpart of the Zod env schema.
/// </summary>
public class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>SQLite connection path. Use ":memory:" for an ephemeral in-memory DB.</summary>
    [Required]
    public string DatabasePath { get; set; } = "./.data/dashboard.db";

    /// <summary>Comma-separated list of allowed browser origins for CORS.</summary>
    [Required]
    public string CorsOrigin { get; set; } = "http://localhost:5173";

    /// <summary>Rate limiter window length, in milliseconds.</summary>
    [Range(1, int.MaxValue)]
    public int RateLimitWindowMs { get; set; } = 15 * 60 * 1000;

    /// <summary>Rate limiter: max requests per window per client.</summary>
    [Range(1, int.MaxValue)]
    public int RateLimitMax { get; set; } = 100;

    public string[] CorsOrigins =>
        CorsOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
