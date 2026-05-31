using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FeatureManagement.Dashboard.Infrastructure.Providers;

internal interface ICurrentUserProvider
{
  string GetCurrentUserOrSystem();
}

internal sealed class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
  private static readonly string[] PreferredClaimTypes =
  [
    ClaimTypes.Email,
    "preferred_username",
    "email",
    ClaimTypes.Name,
    ClaimTypes.NameIdentifier,
    "sub"
  ];

  public string GetCurrentUserOrSystem()
  {
    var user = httpContextAccessor.HttpContext?.User;
    if (user?.Identity?.IsAuthenticated != true)
      return "system";

    foreach (var claimType in PreferredClaimTypes)
    {
      var value = user.FindFirst(claimType)?.Value;
      if (!string.IsNullOrWhiteSpace(value))
        return value.Trim();
    }

    var identityName = user.Identity?.Name;
    return string.IsNullOrWhiteSpace(identityName) ? "system" : identityName.Trim();
  }
}

