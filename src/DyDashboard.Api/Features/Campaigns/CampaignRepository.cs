using DyDashboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DyDashboard.Api.Features.Campaigns;

/// <summary>
/// Data access layer: the only place that talks to the database. Filtering and
/// sorting are expressed through EF Core LINQ; the sort column comes from a
/// fixed whitelist on the query DTO, never from raw user input.
/// </summary>
public class CampaignRepository(AppDbContext db)
{
    public record ListResult(IReadOnlyList<Campaign> Data, int Total);

    public async Task<ListResult> FindAllAsync(ListCampaignsQuery query, CancellationToken ct = default)
    {
        var q = db.Campaigns.AsNoTracking().AsQueryable();

        if (query.Status is not null)
            q = q.Where(c => c.Status == query.Status);
        if (query.Channel is not null)
            q = q.Where(c => c.Channel == query.Channel);
        if (query.Search is not null)
            q = q.Where(c => EF.Functions.Like(c.Name, $"%{query.Search}%"));

        var total = await q.CountAsync(ct);

        q = ApplySort(q, query.SortOrDefault, query.OrderOrDefault);

        var offset = (query.PageOrDefault - 1) * query.LimitOrDefault;
        var data = await q.Skip(offset).Take(query.LimitOrDefault).ToListAsync(ct);

        return new ListResult(data, total);
    }

    private static IQueryable<Campaign> ApplySort(IQueryable<Campaign> q, string sort, string order)
    {
        var asc = order == "asc";
        return sort switch
        {
            "name" => asc ? q.OrderBy(c => c.Name) : q.OrderByDescending(c => c.Name),
            "status" => asc ? q.OrderBy(c => c.Status) : q.OrderByDescending(c => c.Status),
            "conversionRate" => asc ? q.OrderBy(c => c.ConversionRate) : q.OrderByDescending(c => c.ConversionRate),
            "visitors" => asc ? q.OrderBy(c => c.Visitors) : q.OrderByDescending(c => c.Visitors),
            "createdAt" => asc ? q.OrderBy(c => c.CreatedAt) : q.OrderByDescending(c => c.CreatedAt),
            _ => asc ? q.OrderBy(c => c.StartDate) : q.OrderByDescending(c => c.StartDate),
        };
    }

    public Task<Campaign?> FindByIdAsync(string id, CancellationToken ct = default) =>
        db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Campaign> InsertAsync(CreateCampaignRequest input, CancellationToken ct = default)
    {
        var now = Clock.NowIso();
        var campaign = new Campaign
        {
            Id = $"camp-{Guid.NewGuid().ToString("N")[..8]}",
            Name = input.Name,
            Status = input.Status,
            Channel = input.Channel,
            ConversionRate = input.ConversionRate,
            Visitors = input.Visitors,
            StartDate = input.StartDate,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task<Campaign?> UpdateAsync(string id, UpdateCampaignRequest input, CancellationToken ct = default)
    {
        var existing = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (existing is null) return null;

        existing.Name = input.Name ?? existing.Name;
        existing.Status = input.Status ?? existing.Status;
        existing.Channel = input.Channel ?? existing.Channel;
        existing.ConversionRate = input.ConversionRate ?? existing.ConversionRate;
        existing.Visitors = input.Visitors ?? existing.Visitors;
        existing.StartDate = input.StartDate ?? existing.StartDate;
        existing.UpdatedAt = Clock.NowIso();

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        var deleted = await db.Campaigns.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public Task<int> CountAsync(CancellationToken ct = default) => db.Campaigns.CountAsync(ct);
}

/// <summary>ISO-8601 UTC timestamps with millisecond precision, matching the Node API.</summary>
public static class Clock
{
    public static string NowIso() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
}
