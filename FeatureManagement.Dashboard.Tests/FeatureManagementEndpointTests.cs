using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace FeatureManagement.Dashboard.Tests;

public sealed class FeatureManagementEndpointTests
{
  [Fact]
  public async Task Api_requests_require_access()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new DenyAllAccessRequirement());

    var response = await app.Client.GetAsync("/api/feature-flags");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task Api_can_create_update_read_and_delete_feature_flags()
  {
    var fixedTime = new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero);
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement(), new FixedTimeProvider(fixedTime));

    var created = await PostFlagAsync(app.Client, new FeatureFlag
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

    Assert.Equal(HttpStatusCode.OK, created.StatusCode);

    var createdBody = await created.Content.ReadAsStringAsync();
    Assert.False(string.IsNullOrWhiteSpace(createdBody));

    using var createdJson = JsonDocument.Parse(createdBody);
    Assert.Equal(1, createdJson.RootElement.GetProperty("version").GetInt32());
    Assert.Equal(fixedTime.UtcDateTime, createdJson.RootElement.GetProperty("updatedAtUtc").GetDateTime());

    var updateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/beta-dashboard", new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":80}"
        }
      ],
      Version = 1
    });

    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

    var flagsResponse = await app.Client.GetAsync("/api/feature-flags");
    var flagsBody = await flagsResponse.Content.ReadAsStringAsync();
    using var flagsJson = JsonDocument.Parse(flagsBody);
    Assert.Single(flagsJson.RootElement.EnumerateArray());
    var flagJson = flagsJson.RootElement[0];
    Assert.Equal(2, flagJson.GetProperty("version").GetInt32());
    Assert.Equal(fixedTime.UtcDateTime, flagJson.GetProperty("updatedAtUtc").GetDateTime());

    var staleUpdateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/beta-dashboard", new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":90}"
        }
      ],
      Version = 1
    });

    Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);
    var staleBody = await staleUpdateResponse.Content.ReadAsStringAsync();
    Assert.Contains("currentVersion", staleBody);

    var deleteResponse = await app.Client.DeleteAsync("/api/feature-flags/beta-dashboard");

    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    Assert.Contains(app.Services.GetServices<IFeatureDefinitionProvider>(),
      provider => provider.GetType().Name == "DatabaseFeatureDefinitionProvider");

    flagsResponse = await app.Client.GetAsync("/api/feature-flags");
    flagsBody = await flagsResponse.Content.ReadAsStringAsync();
    using var deletedFlagsJson = JsonDocument.Parse(flagsBody);
    Assert.Empty(deletedFlagsJson.RootElement.EnumerateArray());
  }

  [Fact]
  public async Task Ui_route_serves_the_dashboard_shell()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var response = await app.Client.GetAsync("/feature-flags");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
  }

  [Fact]
  public async Task Api_returns_bad_request_for_invalid_feature_flag_payload()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var response = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":101}"
        }
      ]
    });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var body = await response.Content.ReadAsStringAsync();
    Assert.Contains("Value between 0 and 100", body);
  }

  [Fact]
  public async Task Api_rejects_duplicate_feature_flag_names()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var firstResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":10}"
        }
      ]
    });

    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

    var duplicateResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":75}"
        }
      ]
    });

    Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    var body = await duplicateResponse.Content.ReadAsStringAsync();
    Assert.Contains("already exists", body);
  }

  [Fact]
  public async Task Api_returns_not_found_for_missing_feature_flag_on_update_and_delete()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var updateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/missing-flag", new FeatureFlag
    {
      Name = "missing-flag",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":50}"
        }
      ],
      Version = 1
    });

    var deleteResponse = await app.Client.DeleteAsync("/api/feature-flags/missing-flag");

    Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
  }

  [Fact]
  public async Task Api_returns_bad_request_for_invalid_payload_on_update()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":25}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var updateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/beta-dashboard", new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.TimeWindow",
          ParametersJson = "{\"Start\":\"2026-05-23T12:00:00Z\",\"End\":\"2026-05-23T11:00:00Z\"}"
        }
      ],
      Version = 1
    });

    Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    var body = await updateResponse.Content.ReadAsStringAsync();
    Assert.Contains("Start is before End", body);
  }

  [Fact]
  public async Task Api_update_without_version_allows_last_write_wins()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":15}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var updateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/beta-dashboard", new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":65}"
        }
      ]
    });

    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

    var flagsResponse = await app.Client.GetAsync("/api/feature-flags");
    var flagsBody = await flagsResponse.Content.ReadAsStringAsync();
    using var flagsJson = JsonDocument.Parse(flagsBody);
    Assert.Single(flagsJson.RootElement.EnumerateArray());
    var flagJson = flagsJson.RootElement[0];
    Assert.Equal(2, flagJson.GetProperty("version").GetInt32());
    Assert.Equal((int)RequirementType.Any, flagJson.GetProperty("requirementType").GetInt32());
  }

  [Fact]
  public async Task Feature_definition_provider_reflects_create_update_and_delete_after_cache_invalidation()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());
    var provider = app.Services.GetRequiredService<IFeatureDefinitionProvider>();

    var missingBeforeCreate = await provider.GetFeatureDefinitionAsync("beta-dashboard");
    Assert.Null(missingBeforeCreate);

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
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
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var createdDefinition = await provider.GetFeatureDefinitionAsync("beta-dashboard");
    Assert.NotNull(createdDefinition);
    Assert.Equal(RequirementType.All, createdDefinition.RequirementType);
    var createdFilter = Assert.Single(createdDefinition.EnabledFor);
    Assert.Equal("Microsoft.Percentage", createdFilter.Name);
    Assert.Equal("25", createdFilter.Parameters["Value"]);

    var updateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/beta-dashboard", new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":80}"
        }
      ],
      Version = 1
    });
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

    var updatedDefinition = await provider.GetFeatureDefinitionAsync("beta-dashboard");
    Assert.NotNull(updatedDefinition);
    Assert.Equal(RequirementType.Any, updatedDefinition.RequirementType);
    var updatedFilter = Assert.Single(updatedDefinition.EnabledFor);
    Assert.Equal("80", updatedFilter.Parameters["Value"]);

    var deleteResponse = await app.Client.DeleteAsync("/api/feature-flags/beta-dashboard");
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    var missingAfterDelete = await provider.GetFeatureDefinitionAsync("beta-dashboard");
    Assert.Null(missingAfterDelete);
  }

  [Fact]
  public async Task Feature_definition_provider_returns_all_feature_definitions()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());
    var provider = app.Services.GetRequiredService<IFeatureDefinitionProvider>();

    var firstResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":30}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

    var secondResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "new-checkout",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.TimeWindow",
          ParametersJson = "{\"Start\":\"2026-05-23T10:00:00Z\",\"End\":\"2026-05-23T12:00:00Z\"}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

    var definitions = new List<FeatureDefinition>();
    await foreach (var definition in provider.GetAllFeatureDefinitionsAsync())
    {
      definitions.Add(definition);
    }

    Assert.Equal(2, definitions.Count);
    Assert.Contains(definitions, definition => definition.Name == "beta-dashboard");
    Assert.Contains(definitions, definition => definition.Name == "new-checkout");
  }

  [Fact]
  public async Task Feature_definition_provider_maps_empty_filter_parameters_as_empty_configuration()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());
    var provider = app.Services.GetRequiredService<IFeatureDefinitionProvider>();

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "AlwaysOn",
          ParametersJson = "{}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var definition = await provider.GetFeatureDefinitionAsync("beta-dashboard");

    Assert.NotNull(definition);
    var filter = Assert.Single(definition.EnabledFor);
    Assert.Equal("AlwaysOn", filter.Name);
    Assert.Null(filter.Parameters["Value"]);
    Assert.Empty(filter.Parameters.AsEnumerable());
  }

  [Fact]
  public async Task Feature_definition_provider_uses_cached_flags_when_cache_version_is_unchanged()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());
    var provider = app.Services.GetRequiredService<IFeatureDefinitionProvider>();

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":40}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var firstRead = await provider.GetFeatureDefinitionAsync("beta-dashboard");
    Assert.NotNull(firstRead);
    Assert.Equal("40", Assert.Single(firstRead.EnabledFor).Parameters["Value"]);

    await using (var scope = app.Services.CreateAsyncScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
      dbContext.FeatureFlags.RemoveRange(dbContext.FeatureFlags);
      await dbContext.SaveChangesAsync();
    }

    var secondRead = await provider.GetFeatureDefinitionAsync("beta-dashboard");
    Assert.NotNull(secondRead);
    Assert.Equal("40", Assert.Single(secondRead.EnabledFor).Parameters["Value"]);
  }

  [Fact]
  public async Task Feature_definition_provider_removes_previous_cache_version_after_reload()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());
    var provider = app.Services.GetRequiredService<IFeatureDefinitionProvider>();
    var cache = app.Services.GetRequiredService<IMemoryCache>();

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
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
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    await provider.GetFeatureDefinitionAsync("beta-dashboard"); // fills v1
    Assert.True(cache.TryGetValue("feature-flags:all:v1", out _));

    var updateResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/beta-dashboard", new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      Version = 1,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":80}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

    await provider.GetFeatureDefinitionAsync("beta-dashboard"); // fills v2, evicts v1

    Assert.False(cache.TryGetValue("feature-flags:all:v1", out _));
    Assert.True(cache.TryGetValue("feature-flags:all:v2", out _));
  }

  [Fact]
  public async Task Api_and_ui_support_custom_route_prefixes()
  {
    const string apiRoutePrefix = "/api/flags";
    const string uiRoutePrefix = "/flags-ui";
    await using var app = await FeatureManagementTestHost.CreateAsync(
      new AllowAllAccessRequirement(),
      routePrefix: apiRoutePrefix,
      uiRoutePrefix: uiRoutePrefix);

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":20}"
        }
      ]
    }, apiRoutePrefix);

    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var apiReadResponse = await app.Client.GetAsync(apiRoutePrefix);
    Assert.Equal(HttpStatusCode.OK, apiReadResponse.StatusCode);

    var uiResponse = await app.Client.GetAsync(uiRoutePrefix);
    Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
    Assert.Equal("text/html", uiResponse.Content.Headers.ContentType?.MediaType);

    var defaultApiResponse = await app.Client.GetAsync("/api/feature-flags");
    Assert.Equal(HttpStatusCode.NotFound, defaultApiResponse.StatusCode);
  }

  private static Task<HttpResponseMessage> PostFlagAsync(HttpClient client, FeatureFlag flag)
    => PostFlagAsync(client, flag, "/api/feature-flags");

  private static Task<HttpResponseMessage> PostFlagAsync(HttpClient client, FeatureFlag flag, string routePrefix)
    => client.PostAsJsonAsync(routePrefix, flag);

  private sealed class AllowAllAccessRequirement : IAuthorizationRequirement;

  private sealed class DenyAllAccessRequirement : IAuthorizationRequirement;

  private sealed class AllowAllAccessRequirementHandler : AuthorizationHandler<AllowAllAccessRequirement>
  {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowAllAccessRequirement requirement)
    {
      context.Succeed(requirement);
      return Task.CompletedTask;
    }
  }

  private sealed class DenyAllAccessRequirementHandler : AuthorizationHandler<DenyAllAccessRequirement>
  {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DenyAllAccessRequirement requirement)
      => Task.CompletedTask;
  }

  private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => utcNow;
  }

  private sealed class FeatureManagementTestHost : IAsyncDisposable
  {
    private FeatureManagementTestHost(WebApplication app)
    {
      App = app;
      Client = app.GetTestClient();
    }

    private WebApplication App { get; }
    public HttpClient Client { get; }
    public IServiceProvider Services => App.Services;

    public static async Task<FeatureManagementTestHost> CreateAsync(
      IAuthorizationRequirement accessRequirement,
      TimeProvider? timeProvider = null,
      string routePrefix = "/api/feature-flags",
      string uiRoutePrefix = "/feature-flags")
    {
      var databaseName = Guid.NewGuid().ToString();
      var databaseRoot = new InMemoryDatabaseRoot();
      var builder = WebApplication.CreateBuilder(new WebApplicationOptions
      {
        EnvironmentName = "Development"
      });
      builder.WebHost.UseTestServer();
      builder.Services.AddFeatureManagementUi(
        options => options.UseInMemoryDatabase(databaseName, databaseRoot),
        timeProvider ?? TimeProvider.System);
      builder.Services.AddAuthorization();
      builder.Services.AddAuthentication("TestScheme")
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestScheme", _ => { });

      if (accessRequirement is AllowAllAccessRequirement allowRequirement)
      {
        builder.Services.AddSingleton<IAuthorizationRequirement>(allowRequirement);
        builder.Services.AddSingleton<IAuthorizationHandler, AllowAllAccessRequirementHandler>();
      }
      else if (accessRequirement is DenyAllAccessRequirement denyRequirement)
      {
        builder.Services.AddSingleton<IAuthorizationRequirement>(denyRequirement);
        builder.Services.AddSingleton<IAuthorizationHandler, DenyAllAccessRequirementHandler>();
      }

      var app = builder.Build();
      app.UseAuthentication();
      app.MapFeatureManagementEndpoints(accessRequirement, routePrefix);
      app.UseFeatureManagementUi(uiRoutePrefix);
      await app.StartAsync();

      return new FeatureManagementTestHost(app);
    }

    public async ValueTask DisposeAsync()
    {
      await App.DisposeAsync();
    }
  }

  private sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
  {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
      => Task.FromResult(AuthenticateResult.NoResult());
  }
}