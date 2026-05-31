using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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
  public async Task Api_supports_search_owner_and_tag_filters()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var first = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "checkout-flow",
      Owner = "team-payments",
      Tags = ["checkout", "payments"],
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":20}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, first.StatusCode);

    var second = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "profile-redesign",
      Owner = "team-profile",
      Tags = ["profile"],
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":40}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, second.StatusCode);

    var searchResponse = await app.Client.GetAsync("/api/feature-flags?search=checkout");
    var searchBody = await searchResponse.Content.ReadAsStringAsync();
    using var searchJson = JsonDocument.Parse(searchBody);
    Assert.Single(searchJson.RootElement.EnumerateArray());
    Assert.Equal("checkout-flow", searchJson.RootElement[0].GetProperty("name").GetString());

    var ownerResponse = await app.Client.GetAsync("/api/feature-flags?owner=team-profile");
    var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
    using var ownerJson = JsonDocument.Parse(ownerBody);
    Assert.Single(ownerJson.RootElement.EnumerateArray());
    Assert.Equal("profile-redesign", ownerJson.RootElement[0].GetProperty("name").GetString());

    var tagResponse = await app.Client.GetAsync("/api/feature-flags?tag=payments");
    var tagBody = await tagResponse.Content.ReadAsStringAsync();
    using var tagJson = JsonDocument.Parse(tagBody);
    Assert.Single(tagJson.RootElement.EnumerateArray());
    Assert.Equal("checkout-flow", tagJson.RootElement[0].GetProperty("name").GetString());
  }

  [Fact]
  public async Task Api_get_by_name_returns_etag_header_with_current_version()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "etag-flag",
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
    Assert.Equal("\"v1\"", createResponse.Headers.ETag?.Tag);

    var getResponse = await app.Client.GetAsync("/api/feature-flags/etag-flag");

    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    Assert.Equal("\"v1\"", getResponse.Headers.ETag?.Tag);
  }

  [Fact]
  public async Task Api_update_supports_if_match_header_for_optimistic_concurrency()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "if-match-flag",
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
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/api/feature-flags/if-match-flag")
    {
      Content = JsonContent.Create(new FeatureFlag
      {
        Name = "if-match-flag",
        RequirementType = RequirementType.Any,
        Version = 999,
        EnabledFor =
        [
          new FeatureFilter
          {
            Name = "Microsoft.Percentage",
            ParametersJson = "{\"Value\":30}"
          }
        ]
      })
    };
    updateRequest.Headers.TryAddWithoutValidation("If-Match", "\"v1\"");

    var updateResponse = await app.Client.SendAsync(updateRequest);
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

    var staleUpdateRequest = new HttpRequestMessage(HttpMethod.Put, "/api/feature-flags/if-match-flag")
    {
      Content = JsonContent.Create(new FeatureFlag
      {
        Name = "if-match-flag",
        RequirementType = RequirementType.Any,
        EnabledFor =
        [
          new FeatureFilter
          {
            Name = "Microsoft.Percentage",
            ParametersJson = "{\"Value\":35}"
          }
        ]
      })
    };
    staleUpdateRequest.Headers.TryAddWithoutValidation("If-Match", "\"v1\"");

    var staleResponse = await app.Client.SendAsync(staleUpdateRequest);
    Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

    var invalidIfMatchRequest = new HttpRequestMessage(HttpMethod.Put, "/api/feature-flags/if-match-flag")
    {
      Content = JsonContent.Create(new FeatureFlag
      {
        Name = "if-match-flag",
        RequirementType = RequirementType.Any,
        EnabledFor =
        [
          new FeatureFilter
          {
            Name = "Microsoft.Percentage",
            ParametersJson = "{\"Value\":45}"
          }
        ]
      })
    };
    invalidIfMatchRequest.Headers.TryAddWithoutValidation("If-Match", "invalid");

    var invalidIfMatchResponse = await app.Client.SendAsync(invalidIfMatchRequest);
    Assert.Equal(HttpStatusCode.BadRequest, invalidIfMatchResponse.StatusCode);
  }

  [Fact]
  public async Task Api_accepts_segment_targeting_filter_and_maps_audience_parameters()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());
    var provider = app.Services.GetRequiredService<IFeatureDefinitionProvider>();

    var response = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "targeted-dashboard",
      RequirementType = RequirementType.Any,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Targeting",
          ParametersJson =
            "{\"Audience\":{\"Users\":[\"alice\",\"bob\"],\"Groups\":[{\"Name\":\"beta\",\"RolloutPercentage\":40}],\"DefaultRolloutPercentage\":10}}"
        }
      ]
    });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var definition = await provider.GetFeatureDefinitionAsync("targeted-dashboard");
    Assert.NotNull(definition);

    var filter = Assert.Single(definition.EnabledFor);
    Assert.Equal("Microsoft.Targeting", filter.Name);
    Assert.Equal("alice", filter.Parameters["Audience:Users:0"]);
    Assert.Equal("bob", filter.Parameters["Audience:Users:1"]);
    Assert.Equal("beta", filter.Parameters["Audience:Groups:0:Name"]);
    Assert.Equal("40", filter.Parameters["Audience:Groups:0:RolloutPercentage"]);
    Assert.Equal("10", filter.Parameters["Audience:DefaultRolloutPercentage"]);
  }

  [Fact]
  public async Task Api_exposes_audit_history_and_allows_rollback_to_previous_version()
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
          ParametersJson = "{\"Value\":10}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

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

    var auditResponse = await app.Client.GetAsync("/api/feature-flags/beta-dashboard/audit");
    Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
    var auditBody = await auditResponse.Content.ReadAsStringAsync();
    using var auditJson = JsonDocument.Parse(auditBody);
    Assert.Equal(2, auditJson.RootElement.GetArrayLength());
    Assert.Equal((int)FeatureFlagAuditAction.Updated, auditJson.RootElement[0].GetProperty("action").GetInt32());
    Assert.Equal((int)FeatureFlagAuditAction.Created, auditJson.RootElement[1].GetProperty("action").GetInt32());
    Assert.Equal("test-user@example.com", auditJson.RootElement[0].GetProperty("changedBy").GetString());
    Assert.Equal("test-user@example.com", auditJson.RootElement[1].GetProperty("changedBy").GetString());

    var rollbackResponse = await app.Client.PostAsync("/api/feature-flags/beta-dashboard/rollback/1", null);
    Assert.Equal(HttpStatusCode.OK, rollbackResponse.StatusCode);

    var flagsResponse = await app.Client.GetAsync("/api/feature-flags");
    var flagsBody = await flagsResponse.Content.ReadAsStringAsync();
    using var flagsJson = JsonDocument.Parse(flagsBody);
    var flagJson = Assert.Single(flagsJson.RootElement.EnumerateArray());
    Assert.Equal(3, flagJson.GetProperty("version").GetInt32());
    Assert.Equal("{\"Value\":10}", flagJson.GetProperty("enabledFor")[0].GetProperty("parametersJson").GetString());

    var activityResponse = await app.Client.GetAsync("/api/feature-flags/beta-dashboard/activity");
    Assert.Equal(HttpStatusCode.OK, activityResponse.StatusCode);
    var activityBody = await activityResponse.Content.ReadAsStringAsync();
    using var activityJson = JsonDocument.Parse(activityBody);
    Assert.Equal("test-user@example.com", activityJson.RootElement[0].GetProperty("changedBy").GetString());
  }

  [Fact]
  public async Task Api_returns_not_found_when_rollback_version_does_not_exist()
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
          ParametersJson = "{\"Value\":10}"
        }
      ]
    });
    Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

    var rollbackResponse = await app.Client.PostAsync("/api/feature-flags/beta-dashboard/rollback/999", null);

    Assert.Equal(HttpStatusCode.NotFound, rollbackResponse.StatusCode);
    var body = await rollbackResponse.Content.ReadAsStringAsync();
    Assert.Contains("Rollback target version", body);
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
  public async Task Api_supports_experiment_mode_with_outcome_tracking_and_recommendation()
  {
    await using var app = await FeatureManagementTestHost.CreateAsync(new AllowAllAccessRequirement());

    var createResponse = await PostFlagAsync(app.Client, new FeatureFlag
    {
      Name = "checkout-flow",
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

    var configureResponse = await app.Client.PutAsJsonAsync("/api/feature-flags/checkout-flow/experiment", new FeatureFlagExperimentConfiguration
    {
      BaselineVariant = "A",
      ChallengerVariant = "B",
      BaselineTrafficPercentage = 50,
      ChallengerTrafficPercentage = 50,
      ConversionMetricName = "checkout_conversion",
      LatencyMetricName = "checkout_latency_ms",
      MinimumSampleSize = 2,
      IsActive = true
    });
    Assert.Equal(HttpStatusCode.OK, configureResponse.StatusCode);

    var assignResponse = await app.Client.PostAsJsonAsync("/api/feature-flags/checkout-flow/experiment/assign", new AssignExperimentVariantRequest
    {
      SubjectKey = "user-123"
    });
    Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

    await app.Client.PostAsJsonAsync("/api/feature-flags/checkout-flow/experiment/outcomes", new FeatureFlagExperimentOutcome
    {
      Variant = "A",
      Converted = true,
      HasError = false,
      LatencyMs = 250
    });
    await app.Client.PostAsJsonAsync("/api/feature-flags/checkout-flow/experiment/outcomes", new FeatureFlagExperimentOutcome
    {
      Variant = "A",
      Converted = false,
      HasError = true,
      LatencyMs = 350
    });
    await app.Client.PostAsJsonAsync("/api/feature-flags/checkout-flow/experiment/outcomes", new FeatureFlagExperimentOutcome
    {
      Variant = "B",
      Converted = true,
      HasError = false,
      LatencyMs = 100
    });
    await app.Client.PostAsJsonAsync("/api/feature-flags/checkout-flow/experiment/outcomes", new FeatureFlagExperimentOutcome
    {
      Variant = "B",
      Converted = true,
      HasError = false,
      LatencyMs = 120
    });

    var recommendationResponse = await app.Client.GetAsync("/api/feature-flags/checkout-flow/experiment/recommendation");
    Assert.Equal(HttpStatusCode.OK, recommendationResponse.StatusCode);
    var recommendationBody = await recommendationResponse.Content.ReadAsStringAsync();
    using var recommendationJson = JsonDocument.Parse(recommendationBody);
    Assert.Equal((int)FeatureFlagExperimentRecommendationStatus.RecommendChallenger,
      recommendationJson.RootElement.GetProperty("status").GetInt32());
    Assert.Equal("B", recommendationJson.RootElement.GetProperty("recommendedVariant").GetString());
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
    {
      var identity = new ClaimsIdentity(
      [
        new Claim(ClaimTypes.Email, "test-user@example.com"),
        new Claim(ClaimTypes.Name, "test-user")
      ], "TestScheme");
      var principal = new ClaimsPrincipal(identity);
      var ticket = new AuthenticationTicket(principal, "TestScheme");
      return Task.FromResult(AuthenticateResult.Success(ticket));
    }
  }
}