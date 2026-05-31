using FeatureManagement.Dashboard.Infrastructure.UseCases;
using FeatureManagement.Dashboard.Models;
using Microsoft.AspNetCore.Http;

namespace FeatureManagement.Dashboard.Extensions;

internal static class FeatureFlagEndpointHandlers
{
  public static Task<List<FeatureFlag>> GetAllAsync(IGetAllFeatureFlagsUseCase useCase, string? search = null,
    string? owner = null, string? tag = null)
    => useCase.ExecuteAsync(search, owner, tag);

  public static async Task<IResult> GetByNameAsync(IGetFeatureFlagByNameUseCase useCase, HttpContext context, string name)
  {
    try
    {
      var featureFlag = await useCase.ExecuteAsync(name);
      context.Response.Headers.ETag = FeatureFlagEtagHelper.Format(featureFlag.Version);
      return Results.Ok(featureFlag);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> GetAuditAsync(IGetFeatureFlagAuditLogUseCase useCase, string name)
  {
    try
    {
      var auditEntries = await useCase.ExecuteAsync(name);
      return Results.Ok(auditEntries);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> GetActivityAsync(IGetFeatureFlagActivityFeedUseCase useCase, string name)
  {
    try
    {
      var activityEntries = await useCase.ExecuteAsync(name);
      return Results.Ok(activityEntries);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> CreateAsync(ICreateFeatureFlagUseCase useCase, HttpContext context, FeatureFlag flag)
  {
    try
    {
      var created = await useCase.ExecuteAsync(flag);
      context.Response.Headers.ETag = FeatureFlagEtagHelper.Format(created.Version);
      return Results.Ok(created);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> UpdateAsync(IUpdateFeatureFlagUseCase useCase, HttpContext context, string name,
    FeatureFlag updatedFlag)
  {
    try
    {
      if (context.Request.Headers.TryGetValue("If-Match", out var ifMatchHeader))
      {
        if (!FeatureFlagEtagHelper.TryParseIfMatch(ifMatchHeader.ToString(), out var ifMatchVersion))
          return Results.BadRequest(new { error = "Invalid If-Match header format. Expected \"v<version>\"." });

        updatedFlag.Version = ifMatchVersion;
      }

      await useCase.ExecuteAsync(name, updatedFlag);
      return Results.NoContent();
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> DeleteAsync(IDeleteFeatureFlagUseCase useCase, string name)
  {
    try
    {
      await useCase.ExecuteAsync(name);
      return Results.NoContent();
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> RollbackAsync(IRollbackFeatureFlagUseCase useCase, HttpContext context, string name,
    int targetVersion)
  {
    try
    {
      var rolledBack = await useCase.ExecuteAsync(name, targetVersion);
      context.Response.Headers.ETag = FeatureFlagEtagHelper.Format(rolledBack.Version);
      return Results.Ok(rolledBack);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> ScheduleAsync(IScheduleFeatureFlagChangeUseCase useCase, string name, ScheduleFeatureFlagRequest request)
  {
    try
    {
      var scheduled = await useCase.ExecuteAsync(name, request.Flag, request.ScheduledAtUtc);
      return Results.Ok(scheduled);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> ConfigureExperimentAsync(
    IConfigureFeatureFlagExperimentUseCase useCase,
    string name,
    FeatureFlagExperimentConfiguration configuration)
  {
    try
    {
      var experiment = await useCase.ExecuteAsync(name, configuration);
      return Results.Ok(experiment);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> AssignExperimentVariantAsync(
    IAssignFeatureFlagExperimentVariantUseCase useCase,
    string name,
    AssignExperimentVariantRequest request)
  {
    try
    {
      var assignment = await useCase.ExecuteAsync(name, request.SubjectKey);
      return Results.Ok(assignment);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> RecordExperimentOutcomeAsync(
    IRecordFeatureFlagExperimentOutcomeUseCase useCase,
    string name,
    FeatureFlagExperimentOutcome outcome)
  {
    try
    {
      await useCase.ExecuteAsync(name, outcome);
      return Results.NoContent();
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> GetExperimentRecommendationAsync(
    IGetFeatureFlagExperimentRecommendationUseCase useCase,
    string name)
  {
    try
    {
      var recommendation = await useCase.ExecuteAsync(name);
      return Results.Ok(recommendation);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }
}

/// <summary>
/// Request model for scheduling a feature flag change for a future time.
/// </summary>
public class ScheduleFeatureFlagRequest
{
  public required FeatureFlag Flag { get; set; }
  public required DateTime ScheduledAtUtc { get; set; }
}

public class AssignExperimentVariantRequest
{
  public required string SubjectKey { get; set; }
}

