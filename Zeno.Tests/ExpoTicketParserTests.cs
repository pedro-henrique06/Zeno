using Zeno.Application.Notifications;

namespace Zeno.Tests;

public class ExpoTicketParserTests
{
    private static readonly string[] Batch = ["ExponentPushToken[a]", "ExponentPushToken[b]", "ExponentPushToken[c]"];

    [Fact]
    public void Counts_OkTickets()
    {
        var (success, invalid) = ExpoTicketParser.Parse("""{"data":[{"status":"ok","id":"1"},{"status":"ok","id":"2"}]}""", Batch.Take(2).ToList());

        Assert.Equal(2, success);
        Assert.Empty(invalid);
    }

    [Fact]
    public void DeviceNotRegistered_MarksTheMatchingTokenAsInvalid()
    {
        var json = """{"data":[{"status":"ok"},{"status":"error","message":"x","details":{"error":"DeviceNotRegistered"}},{"status":"ok"}]}""";

        var (success, invalid) = ExpoTicketParser.Parse(json, Batch);

        Assert.Equal(2, success);
        Assert.Equal(["ExponentPushToken[b]"], invalid);
    }

    [Fact]
    public void OtherErrors_AreNotCountedAndDoNotInvalidateTheToken()
    {
        var json = """{"data":[{"status":"error","message":"rate","details":{"error":"MessageRateExceeded"}}]}""";

        var (success, invalid) = ExpoTicketParser.Parse(json, Batch.Take(1).ToList());

        Assert.Equal(0, success);
        Assert.Empty(invalid);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"errors":[{"code":"X"}]}""")]
    [InlineData("""{"data":{}}""")]
    public void MalformedResponses_ReturnNothing(string json)
    {
        var (success, invalid) = ExpoTicketParser.Parse(json, Batch);

        Assert.Equal(0, success);
        Assert.Empty(invalid);
    }

    [Fact]
    public void ExtraTickets_AreIgnored()
    {
        var (success, _) = ExpoTicketParser.Parse("""{"data":[{"status":"ok"},{"status":"ok"}]}""", Batch.Take(1).ToList());

        Assert.Equal(1, success);
    }
}
