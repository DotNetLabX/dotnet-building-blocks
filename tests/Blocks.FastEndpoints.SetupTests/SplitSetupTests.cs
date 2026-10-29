using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Blocks.FastEndpoints.SetupTests;

// The endpoint library's Config is process-wide: these tests run one at a time and each restores what it set.
[CollectionDefinition(nameof(EndpointConfigCollection), DisableParallelization = true)]
public sealed class EndpointConfigCollection;

[Collection(nameof(EndpointConfigCollection))]
public sealed class SplitSetupTests
{
    [Fact]
    public void FieldMapValidationReply_Alone_LeavesTheRoutePrefixUnset()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        try
        {
            config.UseFieldMapValidationReply();

            RoutePrefix(config).Should().BeNull();
            ResponseBuilder(config).Should().NotBeSameAs(savedBuilder);
        }
        finally
        {
            config.Errors.ResponseBuilder = savedBuilder;
        }
    }

    [Fact]
    public void FieldMapValidationReply_BuildsTheMapWithStatusAndMessage()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        var savedPolicy = config.Serializer.Options.PropertyNamingPolicy;
        try
        {
            config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            config.UseFieldMapValidationReply();

            var reply = Build(config, 400, new ValidationFailure("Name", "first"), new ValidationFailure("Address.Street", "street"), new ValidationFailure("Name", "second"));

            PropertyNames(reply).Should().Equal("statusCode", "message", "errors");
            reply.GetProperty("statusCode").GetInt32().Should().Be(400);
            reply.GetProperty("message").GetString().Should().Be("Validation failed for multiple fields.");
            var errors = reply.GetProperty("errors");
            PropertyNames(errors).Should().Equal("name", "address.street");
            errors.GetProperty("name").EnumerateArray().Select(m => m.GetString()).Should().Equal("first", "second");
        }
        finally
        {
            config.Serializer.Options.PropertyNamingPolicy = savedPolicy;
            config.Errors.ResponseBuilder = savedBuilder;
        }
    }

    [Fact]
    public void FieldMapValidationReply_WithOneFailure_AnswersItsMessage()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        try
        {
            config.UseFieldMapValidationReply();

            var reply = Build(config, 422, new ValidationFailure("Email", "Email needs an @."));

            reply.GetProperty("statusCode").GetInt32().Should().Be(422);
            reply.GetProperty("message").GetString().Should().Be("Email needs an @.");
        }
        finally
        {
            config.Errors.ResponseBuilder = savedBuilder;
        }
    }

    [Fact]
    public void FieldMapValidationReply_ReadsTheSerializerPolicyAtReplyTime()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        var savedPolicy = config.Serializer.Options.PropertyNamingPolicy;
        try
        {
            config.Serializer.Options.PropertyNamingPolicy = null;
            config.UseFieldMapValidationReply();
            var unconverted = Build(config, 400, new ValidationFailure("Address.Street", "street"));
            config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            var converted = Build(config, 400, new ValidationFailure("Address.Street", "street"));

            PropertyNames(unconverted.GetProperty("errors")).Should().Equal("Address.Street");
            PropertyNames(converted.GetProperty("errors")).Should().Equal("address.street");
        }
        finally
        {
            config.Serializer.Options.PropertyNamingPolicy = savedPolicy;
            config.Errors.ResponseBuilder = savedBuilder;
        }
    }

    [Fact]
    public void FieldMapValidationReply_DeclaresItsReplyType()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        var savedType = ProducesMetadataType(config);
        try
        {
            config.UseFieldMapValidationReply();

            ProducesMetadataType(config).Should().Be(typeof(FieldMapValidationReply));
        }
        finally
        {
            config.Errors.ProducesMetadataType = savedType;
            config.Errors.ResponseBuilder = savedBuilder;
        }
    }

    [Fact]
    public void FieldMapValidationReply_KeepsItsOwnNames_UnderAnyNamingPolicy()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        var savedType = ProducesMetadataType(config);
        try
        {
            config.UseFieldMapValidationReply();
            var reply = ResponseBuilder(config)([new ValidationFailure("Name", "first")], new DefaultHttpContext(), 400);

            var unnamed = JsonSerializer.SerializeToElement(reply, new JsonSerializerOptions { PropertyNamingPolicy = null });
            var camel = JsonSerializer.SerializeToElement(reply, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            PropertyNames(unnamed).Should().Equal("statusCode", "message", "errors");
            PropertyNames(camel).Should().Equal("statusCode", "message", "errors");
        }
        finally
        {
            config.Errors.ProducesMetadataType = savedType;
            config.Errors.ResponseBuilder = savedBuilder;
        }
    }

    [Fact]
    public void EnumsAsText_Alone_AddsTheConverterAndLeavesTheRestUnset()
    {
        var config = new Config();
        var savedBuilder = ResponseBuilder(config);
        var before = config.Serializer.Options.Converters.OfType<JsonStringEnumConverter>().Count();
        try
        {
            config.UseEnumsAsText();

            config.Serializer.Options.Converters.OfType<JsonStringEnumConverter>().Count().Should().Be(before + 1);
            RoutePrefix(config).Should().BeNull();
            ResponseBuilder(config).Should().BeSameAs(savedBuilder);
        }
        finally
        {
            foreach (var added in config.Serializer.Options.Converters.OfType<JsonStringEnumConverter>().Skip(before).ToList())
                config.Serializer.Options.Converters.Remove(added);
        }
    }

    private static JsonElement Build(Config config, int statusCode, params ValidationFailure[] failures)
        => JsonSerializer.SerializeToElement(ResponseBuilder(config)([.. failures], new DefaultHttpContext(), statusCode));

    // The library's options expose these settings through getters that are not all public.
    private static Func<List<ValidationFailure>, HttpContext, int, object> ResponseBuilder(Config config)
        => ReadInternal<Func<List<ValidationFailure>, HttpContext, int, object>>(config.Errors, "ResponseBuilder");

    private static Type? ProducesMetadataType(Config config) => ReadInternal<Type?>(config.Errors, "ProducesMetadataType");

    private static string? RoutePrefix(Config config) => ReadInternal<string?>(config.Endpoints, "RoutePrefix");

    private static T ReadInternal<T>(object options, string property)
    {
        var info = options.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{options.GetType().Name}.{property} was not found.");
        var getter = info.GetGetMethod(nonPublic: true) ?? throw new InvalidOperationException($"{options.GetType().Name}.{property} has no getter.");
        return (T)getter.Invoke(getter.IsStatic ? null : options, null)!;
    }

    private static List<string> PropertyNames(JsonElement element)
        => element.EnumerateObject().Select(property => property.Name).ToList();
}
