using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Blocks.AspNetCore.Middlewares;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Blocks.FastEndpoints.Tests;

public sealed class ValidationReplyShapeTests(EndpointHost host)
{
    [Fact]
    public async Task OneFixtureOfFailures_GivesTheSameErrorsFromBothWriters()
    {
        var fromHandler = await SendThroughTheErrorHandlerAsync();
        var fromEndpoint = await SendThroughTheEndpointLibraryAsync();

        fromHandler.Status.Should().Be(HttpStatusCode.BadRequest);
        fromEndpoint.Status.Should().Be(HttpStatusCode.BadRequest);

        var errors = fromHandler.Reply.GetProperty("errors");
        CustomSetupTests.PropertyNames(errors).Should().Equal("name", "email", "address.street");
        CustomSetupTests.Messages(errors, "name").Should().Equal(SignUpMessages.NameRequired, SignUpMessages.NameTooShort);
        CustomSetupTests.Messages(errors, "email").Should().Equal(SignUpMessages.EmailWithoutAt);
        CustomSetupTests.Messages(errors, "address.street").Should().Equal(SignUpMessages.StreetRequired);
        fromEndpoint.Reply.GetProperty("errors").GetRawText().Should().Be(errors.GetRawText());

        CustomSetupTests.PropertyNames(fromHandler.Reply).Should().Equal("statusCode", "message", "traceId", "errors", "details");
        fromHandler.Reply.GetProperty("message").GetString().Should().Be("One or more validation errors occurred.");
        CustomSetupTests.PropertyNames(fromEndpoint.Reply).Should().Equal("statusCode", "message", "errors");
        fromEndpoint.Reply.GetProperty("message").GetString().Should().Be("Validation failed for multiple fields.");
    }

    private async Task<(HttpStatusCode Status, JsonElement Reply)> SendThroughTheEndpointLibraryAsync()
    {
        using var client = host.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/sign-up", new { name = "", email = "no-at", address = new { street = "" } }, TestContext.Current.CancellationToken);
        return (response.StatusCode, await ReadAsync(response));
    }

    private static async Task<(HttpStatusCode Status, JsonElement Reply)> SendThroughTheErrorHandlerAsync()
    {
        ValidationFailure[] failures =
        [
            new("Name", SignUpMessages.NameRequired),
            new("Name", SignUpMessages.NameTooShort),
            new("Email", SignUpMessages.EmailWithoutAt),
            new("Address.Street", SignUpMessages.StreetRequired),
        ];

        using var errorHost = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .UseEnvironment("Production")
                .Configure(app =>
                {
                    app.UseGlobalExceptionMiddleware();
                    app.Run(_ => throw new ValidationException(failures));
                }))
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = errorHost.GetTestClient();
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        return (response.StatusCode, await ReadAsync(response));
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
