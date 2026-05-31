using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace FeatureManagement.Dashboard.Extensions;

internal static class UseCaseExceptionHttpMapper
{
  internal static bool TryMap(Exception exception, out IResult result)
  {
    switch (exception)
    {
      case ValidationException validationException:
        result = Results.BadRequest(new { error = GetValidationError(validationException) });
        return true;
      case FeatureFlagAlreadyExistsException alreadyExistsException:
        result = Results.Conflict(new { error = alreadyExistsException.Message });
        return true;
      case FeatureFlagNotFoundException:
        result = Results.NotFound();
        return true;
      case FeatureFlagRollbackVersionNotFoundException rollbackException:
        result = Results.NotFound(new { error = rollbackException.Message });
        return true;
      case FeatureFlagVersionConflictException versionConflictException:
        result = Results.Conflict(new
        {
          error = versionConflictException.Message,
          currentVersion = versionConflictException.CurrentVersion
        });
        return true;
      case FeatureFlagExperimentNotConfiguredException experimentNotConfiguredException:
        result = Results.NotFound(new { error = experimentNotConfiguredException.Message });
        return true;
      case FeatureFlagExperimentInvalidVariantException invalidVariantException:
        result = Results.BadRequest(new { error = invalidVariantException.Message });
        return true;
      case ArgumentException argumentException:
        result = Results.BadRequest(new { error = argumentException.Message });
        return true;
      default:
        result = null!;
        return false;
    }
  }

  private static string GetValidationError(ValidationException exception)
    => string.Join(", ", exception.Errors.Select(failure => failure.ErrorMessage).DefaultIfEmpty(exception.Message));
}

