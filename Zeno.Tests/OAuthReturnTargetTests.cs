using Zeno.Application.Services;

namespace Zeno.Tests;

public class OAuthReturnTargetTests
{
    private const string Front = "https://web.example";

    [Fact]
    public void Web_GoesBackToTheFrontend()
    {
        Assert.Equal($"{Front}/auth/callback?token=a%2Bb&refreshToken=c", OAuthReturnTarget.Success(null, Front, "a+b", "c"));
        Assert.Equal($"{Front}/login?oauthError=1", OAuthReturnTarget.Failure(null, Front));
    }

    [Fact]
    public void App_GoesBackToTheAppScheme()
    {
        Assert.Equal("zeno://auth/callback?token=t&refreshToken=r", OAuthReturnTarget.Success("app", Front, "t", "r"));
        Assert.Equal("zeno://auth/callback?error=1", OAuthReturnTarget.Failure("app", Front));
    }

    [Theory]
    [InlineData("APP")]
    [InlineData("zeno://evil")]
    [InlineData("")]
    public void AnyOtherState_StaysOnTheWebFlow(string state)
    {
        Assert.False(OAuthReturnTarget.IsApp(state));
        Assert.StartsWith(Front, OAuthReturnTarget.Failure(state, Front));
    }
}
