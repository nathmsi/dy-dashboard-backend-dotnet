namespace DyDashboard.Api.Features.Campaigns;

/// <summary>
/// Domain entity, shared in shape with the dashboard frontend plus audit
/// timestamps managed by the persistence layer. The <c>Status</c> is stored as
/// a lowercase string ("active" | "paused" | "ended") to match the frontend.
/// </summary>
public class Campaign
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public double ConversionRate { get; set; }
    public int Visitors { get; set; }
    public string StartDate { get; set; } = default!;
    public string CreatedAt { get; set; } = default!;
    public string UpdatedAt { get; set; } = default!;
}

/// <summary>The three campaign lifecycle states accepted by the API.</summary>
public static class CampaignStatus
{
    public const string Active = "active";
    public const string Paused = "paused";
    public const string Ended = "ended";

    public static readonly string[] All = [Active, Paused, Ended];

    public static bool IsValid(string? value) => value is not null && Array.IndexOf(All, value) >= 0;
}
