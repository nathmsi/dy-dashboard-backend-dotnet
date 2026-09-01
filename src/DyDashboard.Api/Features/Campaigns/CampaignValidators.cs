using System.Text.RegularExpressions;
using FluentValidation;

namespace DyDashboard.Api.Features.Campaigns;

// Request validators — the .NET counterpart of the Zod body/query schemas.
// A failing validation is surfaced as a 422 VALIDATION_ERROR by the validation
// endpoint filter.

public partial class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).Must(CampaignStatus.IsValid)
            .WithMessage("status must be one of: active, paused, ended");
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConversionRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Visitors).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StartDate).Matches(IsoDate())
            .WithMessage("must be an ISO date (YYYY-MM-DD)");
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    internal static partial Regex IsoDate();
}

public class UpdateCampaignRequestValidator : AbstractValidator<UpdateCampaignRequest>
{
    public UpdateCampaignRequestValidator()
    {
        // At least one field must be provided (mirrors the Zod .refine).
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Status is not null || x.Channel is not null
                       || x.ConversionRate is not null || x.Visitors is not null || x.StartDate is not null)
            .WithMessage("At least one field must be provided");

        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200));
        When(x => x.Status is not null, () =>
            RuleFor(x => x.Status).Must(CampaignStatus.IsValid)
                .WithMessage("status must be one of: active, paused, ended"));
        When(x => x.Channel is not null, () =>
            RuleFor(x => x.Channel).NotEmpty().MaximumLength(100));
        When(x => x.ConversionRate is not null, () =>
            RuleFor(x => x.ConversionRate!.Value).InclusiveBetween(0, 100));
        When(x => x.Visitors is not null, () =>
            RuleFor(x => x.Visitors!.Value).GreaterThanOrEqualTo(0));
        When(x => x.StartDate is not null, () =>
            RuleFor(x => x.StartDate).Matches(CreateCampaignRequestValidator.IsoDate())
                .WithMessage("must be an ISO date (YYYY-MM-DD)"));
    }
}

public class ListCampaignsQueryValidator : AbstractValidator<ListCampaignsQuery>
{
    public ListCampaignsQueryValidator()
    {
        When(x => x.Page is not null, () =>
            RuleFor(x => x.Page!.Value).GreaterThan(0));
        When(x => x.Limit is not null, () =>
            RuleFor(x => x.Limit!.Value).GreaterThan(0).LessThanOrEqualTo(100));
        When(x => x.Status is not null, () =>
            RuleFor(x => x.Status).Must(CampaignStatus.IsValid)
                .WithMessage("status must be one of: active, paused, ended"));
        When(x => x.Sort is not null, () =>
            RuleFor(x => x.Sort).Must(s => ListCampaignsQuery.SortColumns.Contains(s))
                .WithMessage($"sort must be one of: {string.Join(", ", ListCampaignsQuery.SortColumns)}"));
        When(x => x.Order is not null, () =>
            RuleFor(x => x.Order).Must(o => o is "asc" or "desc")
                .WithMessage("order must be asc or desc"));
    }
}
