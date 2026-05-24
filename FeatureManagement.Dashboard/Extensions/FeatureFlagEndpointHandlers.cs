using FeatureManagement.Dashboard.Infrastructure.UseCases;
using FeatureManagement.Dashboard.Models;
using Microsoft.AspNetCore.Http;

namespace FeatureManagement.Dashboard.Extensions;

internal static class FeatureFlagEndpointHandlers
{
  public static Task<List<FeatureFlag>> GetAllAsync(IGetAllFeatureFlagsUseCase useCase)
    => useCase.ExecuteAsync();

  public static async Task<IResult> CreateAsync(ICreateFeatureFlagUseCase useCase, FeatureFlag flag)
  {
    try
    {
      var created = await useCase.ExecuteAsync(flag);
      return Results.Ok(created);
    }
    catch (Exception ex) when (UseCaseExceptionHttpMapper.TryMap(ex, out var result))
    {
      return result;
    }
  }

  public static async Task<IResult> UpdateAsync(IUpdateFeatureFlagUseCase useCase, string name, FeatureFlag updatedFlag)
  {
    try
    {
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
}

