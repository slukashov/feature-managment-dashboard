using FeatureManagement.Dashboard.Models;
using FluentValidation;
using FluentValidation.Results;

namespace FeatureManagement.Dashboard.Tests;

public sealed class FeatureFilterValidatorTests
{
  [Theory]
  [InlineData("Custom.Filter", "{}")]
  [InlineData("Custom.Filter", "   ")]
  [InlineData("Microsoft.Percentage", "{\"Value\":0}")]
  [InlineData("Microsoft.Percentage", "{\"Value\":100}")]
  [InlineData("Microsoft.Percentage", "{\"Value\":\"42\"}")]
  [InlineData("Microsoft.TimeWindow", "{\"Start\":\"2026-05-23T10:00:00Z\",\"End\":\"2026-05-23T12:00:00Z\"}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"Users\":[\"alice\"]}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"Groups\":[{\"Name\":\"beta\",\"RolloutPercentage\":50}]}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"DefaultRolloutPercentage\":25}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"DefaultRolloutPercentage\":\"25\"}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"Roles\":[\"admin\"]}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"IpRanges\":[\"10.0.0.0/24\"]}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"CustomAttributes\":{\"country\":\"US\"}}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"CustomAttributes\":{\"tenant\":[\"A\",\"B\"]}}}")]
  [InlineData("Microsoft.Targeting", "{\"Audience\":{\"users\":[\"alice\"]}}")]
  public void FeatureFilterValidator_accepts_supported_valid_inputs(string filterName, string parametersJson)
  {
    var result = ValidateFilter(filterName, parametersJson);

    Assert.True(result.IsValid);
  }

  [Fact]
  public void FeatureFilterValidator_accepts_null_parameters_for_custom_filter()
  {
    var result = ValidateFilter("Custom.Filter", null);

    Assert.True(result.IsValid);
  }

  [Fact]
  public void FeatureFilterValidator_requires_filter_name()
  {
    var result = ValidateFilter(string.Empty, "{}");

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, error => error.ErrorMessage == "Filter name is required.");
  }

  [Fact]
  public void FeatureFilterValidator_rejects_invalid_json()
  {
    var result = ValidateFilter("Custom.Filter", "{not json}");

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, error => error.ErrorMessage == "Filter parameters must be valid JSON.");
  }

  [Theory]
  [InlineData("{\"Value\":101}")]
  [InlineData("{\"Value\":true}")]
  [InlineData("{\"Value\":\"abc\"}")]
  [InlineData("{\"Value\":2147483648}")]
  [InlineData("{}")]
  [InlineData("   ")]
  [InlineData(null)]
  [InlineData("{not json}")]
  public void FeatureFilterValidator_rejects_invalid_percentage_payloads(string? parametersJson)
  {
    var result = ValidateFilter("Microsoft.Percentage", parametersJson);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors,
      error => error.ErrorMessage == "Microsoft.Percentage filter requires Value between 0 and 100.");
  }

  [Theory]
  [InlineData("{\"Start\":\"2026-05-23T10:00:00Z\"}")]
  [InlineData("{\"Start\":1,\"End\":\"2026-05-23T12:00:00Z\"}")]
  [InlineData("{\"Start\":\"   \",\"End\":\"2026-05-23T12:00:00Z\"}")]
  [InlineData("{\"Start\":\"invalid\",\"End\":\"2026-05-23T12:00:00Z\"}")]
  [InlineData("{\"Start\":\"2026-05-23T12:00:00Z\",\"End\":\"2026-05-23T12:00:00Z\"}")]
  [InlineData("{\"Start\":\"2026-05-23T13:00:00Z\",\"End\":\"2026-05-23T12:00:00Z\"}")]
  [InlineData("{not json}")]
  public void FeatureFilterValidator_rejects_invalid_time_window_payloads(string parametersJson)
  {
    var result = ValidateFilter("Microsoft.TimeWindow", parametersJson);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors,
      error => error.ErrorMessage == "Microsoft.TimeWindow filter requires valid Start and End where Start is before End.");
  }

  [Theory]
  [InlineData("{}")]
  [InlineData("{\"Audience\":{}}")]
  [InlineData("{\"Audience\":{\"Users\":[]}}")]
  [InlineData("{\"Audience\":{\"Users\":[\"\"]}}")]
  [InlineData("{\"Audience\":{\"Users\":\"alice\"}}")]
  [InlineData("{\"Audience\":{\"Groups\":[{\"Name\":\"beta\",\"RolloutPercentage\":101}]}}")]
  [InlineData("{\"Audience\":{\"Groups\":[\"beta\"]}}")]
  [InlineData("{\"Audience\":{\"Groups\":[{\"Name\":\"\",\"RolloutPercentage\":50}]}}")]
  [InlineData("{\"Audience\":{\"Groups\":[{\"Name\":\"beta\",\"RolloutPercentage\":true}]}}")]
  [InlineData("{\"Audience\":{\"Groups\":[{\"RolloutPercentage\":50}]}}")]
  [InlineData("{\"Audience\":{\"DefaultRolloutPercentage\":-1}}")]
  [InlineData("{\"Audience\":{\"DefaultRolloutPercentage\":\"NaN\"}}")]
  [InlineData("{\"Audience\":{\"Roles\":[]}}")]
  [InlineData("{\"Audience\":{\"Roles\":[\"\"]}}")]
  [InlineData("{\"Audience\":{\"IpRanges\":[]}}")]
  [InlineData("{\"Audience\":{\"IpRanges\":[\"\"]}}")]
  [InlineData("{\"Audience\":{\"CustomAttributes\":{}}}")]
  [InlineData("{\"Audience\":{\"CustomAttributes\":{\"tier\":1}}}")]
  [InlineData("{\"Audience\":{\"CustomAttributes\":{\" \":\"value\"}}}")]
  [InlineData("{\"Audience\":[]}")]
  [InlineData("null")]
  [InlineData("{not json}")]
  public void FeatureFilterValidator_rejects_invalid_targeting_payloads(string parametersJson)
  {
    var result = ValidateFilter("Microsoft.Targeting", parametersJson);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors,
      error => error.ErrorMessage ==
               "Microsoft.Targeting filter requires a valid Audience with Users, Groups, or DefaultRolloutPercentage between 0 and 100.");
  }

  private static ValidationResult ValidateFilter(string filterName, string? parametersJson)
  {
    using var provider = BuildServiceProvider();
    using var scope = provider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();

    return validator.Validate(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = filterName,
          ParametersJson = parametersJson ?? null!
        }
      ]
    });
  }

  private static ServiceProvider BuildServiceProvider()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()), TimeProvider.System);
    return services.BuildServiceProvider();
  }
}