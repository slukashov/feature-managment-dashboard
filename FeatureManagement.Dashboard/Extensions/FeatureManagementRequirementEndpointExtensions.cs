using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureManagement.Dashboard.Extensions;

internal static class FeatureManagementRequirementEndpointExtensions
{
  internal static RouteHandlerBuilder RequireFeatureManagement(this RouteHandlerBuilder builder,
    IAuthorizationRequirement accessRequirement) =>
    builder.AddEndpointFilter(async (context, next) =>
    {
      var httpContext = context.HttpContext;

      var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
      var result = await authorizationService.AuthorizeAsync(httpContext.User, httpContext, accessRequirement);
      if (!result.Succeeded)
        return Results.Forbid();

      return await next(context);
    });
}