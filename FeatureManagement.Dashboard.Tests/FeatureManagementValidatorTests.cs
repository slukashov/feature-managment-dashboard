using FeatureManagement.Dashboard.Models;
using FluentValidation;

namespace FeatureManagement.Dashboard.Tests;

public sealed class FeatureManagementValidatorTests
{
  [Fact]
  public void FeatureFlagValidator_accepts_valid_flag_and_rejects_invalid_filters()
  {
    using var provider = BuildServiceProvider();
    using var scope = provider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();

    var validResult = validator.Validate(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":25}"
        }
      ]
    });

    Assert.True(validResult.IsValid);

    var invalidResult = validator.Validate(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.TimeWindow",
          ParametersJson = "{\"Start\":\"2026-05-23T12:00:00Z\",\"End\":\"2026-05-23T11:00:00Z\"}"
        }
      ]
    });

    Assert.False(invalidResult.IsValid);
    Assert.Contains(invalidResult.Errors, error =>
      error.ErrorMessage == "Microsoft.TimeWindow filter requires valid Start and End where Start is before End.");
  }

  [Theory]
  [InlineData("", "Microsoft.Percentage", "{\"Value\":10}", "Feature name is required.")]
  [InlineData("beta-dashboard", "", "{}", "Filter name is required.")]
  [InlineData("beta-dashboard", "Custom.Filter", "{not json}", "Filter parameters must be valid JSON.")]
  [InlineData("beta-dashboard", "Microsoft.TimeWindow", "{\"Start\":\"2026-05-23T10:00:00Z\"}", "Microsoft.TimeWindow filter requires valid Start and End where Start is before End.")]
  [InlineData("beta-dashboard", "Microsoft.Percentage", "{\"Value\":101}", "Microsoft.Percentage filter requires Value between 0 and 100.")]
  public void FeatureFlagValidator_reports_expected_errors(
    string flagName,
    string filterName,
    string parametersJson,
    string expectedMessage)
  {
    using var provider = BuildServiceProvider();
    using var scope = provider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();

    var result = validator.Validate(new FeatureFlag
    {
      Name = flagName,
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = filterName,
          ParametersJson = parametersJson
        }
      ]
    });

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, error => error.ErrorMessage == expectedMessage);
  }

  [Fact]
  public void FeatureFlagValidator_accepts_valid_time_window_and_string_percentage_values()
  {
    using var provider = BuildServiceProvider();
    using var scope = provider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();

    var percentageResult = validator.Validate(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":\"42\"}"
        }
      ]
    });

    var timeWindowResult = validator.Validate(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.TimeWindow",
          ParametersJson = "{\"Start\":\"2026-05-23T10:00:00Z\",\"End\":\"2026-05-23T12:00:00Z\"}"
        }
      ]
    });

    Assert.True(percentageResult.IsValid);
    Assert.True(timeWindowResult.IsValid);
  }

  [Fact]
  public void FeatureFlagValidator_accepts_empty_parameters_for_custom_filters()
  {
    using var provider = BuildServiceProvider();
    using var scope = provider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();

    var result = validator.Validate(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Custom.Filter",
          ParametersJson = "{}"
        }
      ]
    });

    Assert.True(result.IsValid);
  }

  private static ServiceProvider BuildServiceProvider()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()), TimeProvider.System);
    return services.BuildServiceProvider();
  }
}