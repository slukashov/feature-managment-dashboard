using FeatureManagement.Dashboard.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFeatureManagement();
builder.Services.AddFeatureManagementUi(optionsBuilder=> optionsBuilder.UseInMemoryDatabase("FeatureManagementDashboard"));
builder.Services.AddAuthorization(options => options.AddPolicy("Policy", policy =>
{
  policy.AddRequirements(new AuthorizationRequirement());
}));
builder.Services.AddSingleton<IAuthorizationHandler, AuthorizationHandler>();

var app = builder.Build();

app.UseAuthorization();
app.MapFeatureManagementEndpoints(new AuthorizationRequirement());
app.UseFeatureManagementUi();
app.UseHttpsRedirection();

await app.RunAsync();

internal class AuthorizationRequirement : IAuthorizationRequirement;

internal class AuthorizationHandler : AuthorizationHandler<AuthorizationRequirement>
{
  protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationRequirement requirement)
  {
    context.Succeed(requirement);
    return Task.CompletedTask;
  }
}