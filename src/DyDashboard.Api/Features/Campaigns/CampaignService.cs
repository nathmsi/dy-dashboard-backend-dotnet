using DyDashboard.Api.Common.Errors;

namespace DyDashboard.Api.Features.Campaigns;

/// <summary>
/// Business layer: enforces domain rules and translates "missing" into a typed
/// <see cref="NotFoundException"/>. It never touches HTTP concerns.
/// </summary>
public class CampaignService(CampaignRepository repository)
{
    public async Task<PaginatedCampaigns> ListAsync(ListCampaignsQuery query, CancellationToken ct = default)
    {
        var (data, total) = await repository.FindAllAsync(query, ct);
        var limit = query.LimitOrDefault;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)limit));
        return new PaginatedCampaigns(
            data,
            new PaginationMeta(query.PageOrDefault, limit, total, totalPages));
    }

    public async Task<Campaign> GetAsync(string id, CancellationToken ct = default)
    {
        var campaign = await repository.FindByIdAsync(id, ct);
        return campaign ?? throw new NotFoundException($"Campaign \"{id}\" not found");
    }

    public Task<Campaign> CreateAsync(CreateCampaignRequest input, CancellationToken ct = default) =>
        repository.InsertAsync(input, ct);

    public async Task<Campaign> UpdateAsync(string id, UpdateCampaignRequest input, CancellationToken ct = default)
    {
        var updated = await repository.UpdateAsync(id, input, ct);
        return updated ?? throw new NotFoundException($"Campaign \"{id}\" not found");
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var deleted = await repository.RemoveAsync(id, ct);
        if (!deleted) throw new NotFoundException($"Campaign \"{id}\" not found");
    }
}
