namespace DyDashboard.Api.Features.Campaigns;

/// <summary>
/// Body accepted when creating a campaign. The id and timestamps are generated
/// server-side, so they are not part of the input.
/// </summary>
public record CreateCampaignRequest(
    string Name,
    string Status,
    string Channel,
    double ConversionRate,
    int Visitors,
    string StartDate);

/// <summary>
/// Body accepted when partially updating a campaign. Every field is optional; at
/// least one must be supplied (enforced by the validator).
/// </summary>
public record UpdateCampaignRequest(
    string? Name,
    string? Status,
    string? Channel,
    double? ConversionRate,
    int? Visitors,
    string? StartDate);

/// <summary>
/// Query parameters for the list endpoint: pagination, filtering and sorting.
/// Bound from the query string with <c>[AsParameters]</c>; nulls fall back to the
/// documented defaults inside the service.
/// </summary>
public record ListCampaignsQuery
{
    public int? Page { get; init; }
    public int? Limit { get; init; }
    public string? Status { get; init; }
    public string? Channel { get; init; }
    public string? Search { get; init; }
    public string? Sort { get; init; }
    public string? Order { get; init; }

    // Normalized, defaulted view of the raw query. Column/direction tokens are
    // validated against a whitelist, so they are safe to interpolate downstream.
    public int PageOrDefault => Page is > 0 ? Page.Value : 1;
    public int LimitOrDefault => Limit is > 0 and <= 100 ? Limit.Value : 20;
    public string SortOrDefault => SortColumns.Contains(Sort) ? Sort! : "startDate";
    public string OrderOrDefault => Order == "asc" ? "asc" : "desc";

    public static readonly string[] SortColumns =
        ["name", "status", "conversionRate", "visitors", "startDate", "createdAt"];
}

/// <summary>Pagination metadata returned alongside a page of campaigns.</summary>
public record PaginationMeta(int Page, int Limit, int Total, int TotalPages);

/// <summary>Result of a list query: the page of rows plus its pagination metadata.</summary>
public record PaginatedCampaigns(IReadOnlyList<Campaign> Data, PaginationMeta Pagination);
