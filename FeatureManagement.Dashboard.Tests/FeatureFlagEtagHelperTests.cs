namespace FeatureManagement.Dashboard.Tests;

public sealed class FeatureFlagEtagHelperTests
{
  [Fact]
  public void Format_wraps_version_as_strong_etag()
  {
    var result = FeatureFlagEtagHelper.Format(42);

    Assert.Equal("\"v42\"", result);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("\t")]
  public void TryParseIfMatch_rejects_empty_header(string? header)
  {
    var ok = FeatureFlagEtagHelper.TryParseIfMatch(header, out var version);

    Assert.False(ok);
    Assert.Equal(0, version);
  }

  [Theory]
  [InlineData("\"v12\"", 12)]
  [InlineData("\"V12\"", 12)]
  [InlineData(" W/\"v12\" ", 12)]
  [InlineData("W/ \"v12\"", 12)]
  [InlineData("\"v-1\"", -1)]
  public void TryParseIfMatch_accepts_valid_etag_forms(string header, int expectedVersion)
  {
    var ok = FeatureFlagEtagHelper.TryParseIfMatch(header, out var version);

    Assert.True(ok);
    Assert.Equal(expectedVersion, version);
  }

  [Theory]
  [InlineData("v1")]
  [InlineData("\"v\"")]
  [InlineData("\"x1\"")]
  [InlineData("\"vabc\"")]
  [InlineData("W/abc")]
  public void TryParseIfMatch_rejects_invalid_etag_forms(string header)
  {
    var ok = FeatureFlagEtagHelper.TryParseIfMatch(header, out var version);

    Assert.False(ok);
    Assert.Equal(0, version);
  }
}


