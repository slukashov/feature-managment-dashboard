using FeatureManagement.Dashboard.Models;
using FluentValidation;

namespace FeatureManagement.Dashboard.Infrastructure.Validators;

internal sealed class FeatureFlagValidator : AbstractValidator<FeatureFlag>
{
  public FeatureFlagValidator()
  {
    RuleFor(flag => flag.Name)
      .NotEmpty()
      .WithMessage(Constants.ErrorMessages.RequiredFeatureName);

    RuleForEach(flag => flag.EnabledFor)
      .SetValidator(new FeatureFilterValidator());
  }
}