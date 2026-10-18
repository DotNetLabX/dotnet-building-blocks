using System.Text.Json;
using AwesomeAssertions;
using FluentValidation.Results;
using Xunit;

namespace Blocks.AspNetCore.Tests;

public sealed class ValidationFailureExtensionsTests
{
    [Theory]
    [InlineData("Name", "name")]
    [InlineData("Address.Street", "address.street")]
    [InlineData("Items[0].StreetName", "items[0].streetName")]
    [InlineData("Order.Lines[12].Product.UnitPrice", "order.lines[12].product.unitPrice")]
    [InlineData("", "")]
    public void EveryPartOfThePath_TakesThePolicy(string propertyName, string expected)
    {
        var map = new[] { new ValidationFailure(propertyName, "message") }.ToFieldMap(JsonNamingPolicy.CamelCase);

        map.Keys.Should().Equal(expected);
    }

    [Fact]
    public void ThePolicy_SeesTheNameWithoutItsIndexer()
    {
        var map = new[] { new ValidationFailure("Items[0].Street", "message") }.ToFieldMap(new SuffixPolicy());

        map.Keys.Should().Equal("Items_f[0].Street_f");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("[0].Street", "[0].Street_f")]
    public void APartWithNoName_IsLeftForThePolicy(string propertyName, string expected)
    {
        var map = new[] { new ValidationFailure(propertyName, "message") }.ToFieldMap(new SuffixPolicy());

        map.Keys.Should().Equal(expected);
    }

    [Fact]
    public void WithoutAPolicy_KeysStayAsTheyAre()
    {
        var map = new[] { new ValidationFailure("Address.Street", "message"), new ValidationFailure("Items[0].StreetName", "other") }
            .ToFieldMap(null);

        map.Keys.Should().Equal("Address.Street", "Items[0].StreetName");
    }

    [Fact]
    public void FailuresOnOneField_ShareOneArrayInOrder_AndFieldsKeepFirstSeenOrder()
    {
        var map = new[]
        {
            new ValidationFailure("Name", "first"),
            new ValidationFailure("Email", "only"),
            new ValidationFailure("Name", "second"),
        }.ToFieldMap(JsonNamingPolicy.CamelCase);

        map.Keys.Should().Equal("name", "email");
        map["name"].Should().Equal("first", "second");
        map["email"].Should().Equal("only");
    }

    private sealed class SuffixPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name + "_f";
    }
}
