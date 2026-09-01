using DyDashboard.Api.Features.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace DyDashboard.Api.Data;

/// <summary>
/// Initial dataset — mirrors the campaigns that used to live in the frontend, so
/// the dashboard looks identical the first time it talks to the real API. Applied
/// only when the table is empty.
/// </summary>
public static class DbSeeder
{
    private static readonly (string Id, string Name, string Status, string Channel, double Rate, int Visitors, string Start)[] Seed =
    [
        ("camp-001", "Homepage Hero Banner", "active", "Web", 4.8, 128_400, "2026-01-12"),
        ("camp-002", "Cart Abandonment Popup", "active", "Web", 12.3, 43_200, "2026-02-01"),
        ("camp-003", "New User Welcome Offer", "paused", "Email", 7.1, 61_000, "2025-11-20"),
        ("camp-004", "Black Friday Countdown", "ended", "Web", 15.6, 302_800, "2025-11-25"),
        ("camp-005", "Mobile App Upsell Modal", "active", "Mobile", 3.2, 89_500, "2026-03-05"),
        ("camp-006", "Loyalty Points Reminder", "paused", "Email", 9.4, 27_600, "2026-01-28"),
        ("camp-007", "Product Recommendation Carousel", "active", "Web", 6.7, 154_900, "2025-12-15"),
        ("camp-008", "Exit Intent Discount", "ended", "Web", 11.2, 76_300, "2025-10-02"),
        ("camp-009", "Push Notification Re-engagement", "active", "Mobile", 5.5, 42_100, "2026-02-18"),
        ("camp-010", "Winter Sale Landing Page", "paused", "Web", 8.9, 210_700, "2026-01-05"),
    ];

    public static async Task<int> SeedIfEmptyAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Campaigns.AnyAsync(ct)) return 0;

        var now = Clock.NowIso();
        var rows = Seed.Select(s => new Campaign
        {
            Id = s.Id,
            Name = s.Name,
            Status = s.Status,
            Channel = s.Channel,
            ConversionRate = s.Rate,
            Visitors = s.Visitors,
            StartDate = s.Start,
            CreatedAt = now,
            UpdatedAt = now,
        });

        db.Campaigns.AddRange(rows);
        return await db.SaveChangesAsync(ct);
    }
}
