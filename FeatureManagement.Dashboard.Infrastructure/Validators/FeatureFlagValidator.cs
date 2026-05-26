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

    RuleFor(flag => flag.Owner)
      .MaximumLength(200)
      .WithMessage(Constants.ErrorMessages.InvalidOwner);

    RuleForEach(flag => flag.Tags)
      .Must(tag => !string.IsNullOrWhiteSpace(tag) && tag.Trim().Length <= 64)
      .WithMessage(Constants.ErrorMessages.InvalidTag);

    RuleForEach(flag => flag.EnabledFor)
      .SetValidator(new FeatureFilterValidator());
  }
}