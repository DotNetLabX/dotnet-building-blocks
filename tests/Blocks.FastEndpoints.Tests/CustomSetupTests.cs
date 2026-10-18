using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace Blocks.FastEndpoints.Tests;

public sealed class CustomSetupTests(EndpointHost host)
{
    [Fact]
    public async Task Routes_CarryTheApiPrefix()
    {
        using var client = host.CreateClient();

        using var prefixed = await client.GetAsync("/api/ping", TestContext.Current.CancellationToken);
        using var bare = await client.GetAsync("/ping", TestContext.Current.CancellationToken);

        prefixed.StatusCode.Should().Be(HttpStatusCode.OK);
        bare.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnEnumInTheReply_IsWrittenAsText()
    {
        using var client = host.CreateClient();

        var json = await client.GetStringAsync("/api/shade", TestContext.Current.CancellationToken);

        JsonDocument.Parse(json).RootElement.GetProperty("shade").GetString().Should().Be("Dark");
    }

    [Fact]
    public async Task FailuresOnFlatFields_AnswerAPerFieldMap()
    {
        var (status, reply) = await SignUpAsync(new { name = "", email = "no-at", address = new { street = "Main" } });

        status.Should().Be(HttpStatusCode.BadRequest);
        PropertyNames(reply).Should().Equal("statusCode", "message", "errors");
        reply.GetProperty("statusCode").GetInt32().Should().Be(400);
        reply.GetProperty("message").GetString().Should().Be("Validation failed for multiple fields.");
        var errors = reply.GetProperty("errors");
        PropertyNames(errors).Should().Equal("name", "email");
        Messages(errors, "name").Should().Equal(SignUpMessages.NameRequired, SignUpMessages.NameTooShort);
        Messages(errors, "email").Should().Equal(SignUpMessages.EmailWithoutAt);
    }

    [Fact]
    public async Task OneFailure_AnswersItsOwnMessage()
    {
        var (status, reply) = await SignUpAsync(new { name = "Ann", email = "no-at", address = new { street = "Main" } });

        status.Should().Be(HttpStatusCode.BadRequest);
        reply.GetProperty("message").GetString().Should().Be(SignUpMessages.EmailWithoutAt);
        PropertyNames(reply.GetProperty("errors")).Should().Equal("email");
    }

    private async Task<(HttpStatusCode Status, JsonElement Reply)> SignUpAsync(object request)
    {
        using var client = host.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/sign-up", request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (response.StatusCode, JsonDocument.Parse(body).RootElement);
    }

    internal static List<string> PropertyNames(JsonElement element)
        => element.EnumerateObject().Select(property => property.Name).ToList();

    internal static List<string?> Messages(JsonElement errors, string field)
        => errors.GetProperty(field).EnumerateArray().Select(message => message.GetString()).ToList();
}
