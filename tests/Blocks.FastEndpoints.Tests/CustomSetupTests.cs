using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
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

    [Fact]
    public async Task TheCallersConfiguration_RunsAfterTheBlocks_AndThePrefixStaysApi()
    {
        using var client = host.CreateClient();

        using var prefixed = await PostRawAsync(client, "/api/sign-up", ValidSignUp);
        using var bare = await PostRawAsync(client, "/sign-up", ValidSignUp);

        host.CallersConfigurationSawTheBlocksEnumSetting.Should().BeTrue();
        prefixed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        prefixed.Headers.TryGetValues(ConfiguredHeader.Name, out var values).Should().BeTrue();
        values.Should().Equal("yes");
        bare.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ABodyWhoseNamesDifferInCase_Binds()
    {
        using var client = host.CreateClient();

        using var response = await PostRawAsync(client, "/api/sign-up", """{"NAME":"Ann","EMAIL":"ann@example.com","ADDRESS":{"STREET":"Main"}}""");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AMalformedBody_Answers400_WithTheSerializerErrorsKeyAndItsTextAsTheMessage()
    {
        using var client = host.CreateClient();

        using var response = await PostRawAsync(client, "/api/sign-up", "{not json");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        reply.GetProperty("statusCode").GetInt32().Should().Be(400);
        var errors = reply.GetProperty("errors");
        PropertyNames(errors).Should().Equal("serializerErrors");
        var parserText = Messages(errors, "serializerErrors").Should().ContainSingle().Subject;
        parserText.Should().NotBeNullOrWhiteSpace();
        reply.GetProperty("message").GetString().Should().Be(parserText);
    }

    // Compiled, never called: keeps the call without a configuration delegate building.
    internal static void NoArgumentCall_Compiles(IApplicationBuilder app) => app.UseCustomFastEndpoints();

    private const string ValidSignUp = """{"name":"Ann","email":"ann@example.com","address":{"street":"Main"}}""";

    private static Task<HttpResponseMessage> PostRawAsync(HttpClient client, string path, string json)
        => client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

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
