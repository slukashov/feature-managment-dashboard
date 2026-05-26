using FeatureManagement.Dashboard.Models;
using FluentValidation;

namespace FeatureManagement.Dashboard.Infrastructure.Validators;

internal sealed class FeatureFilterValidator : AbstractValidator<FeatureFilter>
{
  public FeatureFilterValidator()
  {
    RuleFor(filter => filter.Name)
      .NotEmpty()
      .WithMessage(Constants.ErrorMessages.RequiredFilterName);

    RuleFor(filter => filter.ParametersJson)
      .Must(FeatureFilterValidatorHelper.TryParseParameters)
      .WithMessage(Constants.ErrorMessages.InvalidJson);

    RuleFor(filter => filter)
      .Must(FeatureFilterValidatorHelper.HasValidPercentage)
      .When(filter => filter.Name == Constants.PercentageFilterName)
      .WithMessage(Constants.ErrorMessages.InvalidPercentage);

    RuleFor(filter => filter)
      .Must(FeatureFilterValidatorHelper.HasValidTimeWindow)
      .When(filter => filter.Name == Constants.TimeWindowFilterName)
      .WithMessage(Constants.ErrorMessages.InvalidTimeWindow);

    RuleFor(filter => filter)
      .Must(FeatureFilterValidatorHelper.HasValidTargeting)
      .When(filter => filter.Name == Constants.TargetingFilterName)
      .WithMessage(Constants.ErrorMessages.InvalidTargeting);
  }
}