using FluentValidation;
using AppValidationException = DyDashboard.Api.Common.Errors.ValidationException;

namespace DyDashboard.Api.Common.Validation;

/// <summary>
/// Endpoint filter that validates a single argument of type <typeparamref name="T"/>
/// against its registered FluentValidation validator before the handler runs.
/// On failure it throws a <see cref="ValidationException"/> (422) carrying the
/// grouped field errors — the .NET counterpart of the Zod <c>validate</c> middleware.
/// </summary>
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var argument = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (argument is not null)
        {
            var result = await validator.ValidateAsync(argument);
            if (!result.IsValid)
            {
                var fieldErrors = result.Errors
                    .GroupBy(e => ToCamelCase(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                throw new AppValidationException(new { fieldErrors });
            }
        }

        return await next(ctx);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}

public static class ValidationFilterExtensions
{
    /// <summary>Attach a FluentValidation filter for <typeparamref name="T"/> to an endpoint.</summary>
    public static RouteHandlerBuilder ValidateWith<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}
