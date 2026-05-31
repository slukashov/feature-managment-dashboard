using System.Security.Cryptography;
using System.Text;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class AssignFeatureFlagExperimentVariantUseCase(IFeatureManagementContext context)
  : IAssignFeatureFlagExperimentVariantUseCase
{
  public async Task<FeatureFlagExperimentVariantAssignment> ExecuteAsync(
    string featureFlagName,
    string subjectKey,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(subjectKey))
      throw new ArgumentException("Subject key is required.", nameof(subjectKey));

    var experiment = await context.FeatureFlagExperiments
      .AsNoTracking()
      .FirstOrDefaultAsync(entry => entry.FeatureFlagName == featureFlagName && entry.IsActive, cancellationToken);

    if (experiment is null)
      throw new FeatureFlagExperimentNotConfiguredException(featureFlagName);

    var bucket = GetBucket(subjectKey.Trim());
    var variant = bucket < experiment.BaselineTrafficPercentage
      ? experiment.BaselineVariant
      : experiment.ChallengerVariant;

    return new FeatureFlagExperimentVariantAssignment
    {
      Variant = variant,
      Bucket = bucket
    };
  }

  private static int GetBucket(string subjectKey)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(subjectKey));
    var value = BitConverter.ToUInt32(hash, 0);
    return (int)(value % 100);
  }
}

