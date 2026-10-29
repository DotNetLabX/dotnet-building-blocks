using AwesomeAssertions;
using Blocks.AspNetCore.Middlewares;
using Blocks.Core;
using Blocks.Core.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Blocks.AspNetCore.Tests;

public sealed class RequestDiagnosticsMiddlewareTests
{
    private const int TakesMs = 40;

    private readonly CollectingLogger<RequestDiagnosticsMiddleware> _logger = new();

    [Theory]
    [InlineData("text/event-stream; charset=utf-8")]
    [InlineData("Text/Event-Stream")]
    public async Task AStreamedReplySlowerThanTheLimit_IsNotWarned_WhileAnOrdinaryOneIs(string streamedType)
    {
        var limits = Bind(("SlowRequestMs", "1"), ("SlowFileTransferMs", "1"));

        await RunAsync(limits, streamedType);
        Warnings().Should().BeEmpty();

        await RunAsync(limits, "application/json");
        Warnings().Should().ContainSingle();
    }

    [Fact]
    public async Task TheOrdinaryLimit_ComesFromTheBoundSettings()
    {
        await RunAsync(Bind(("SlowRequestMs", "60000"), ("SlowFileTransferMs", "1")), "application/json");
        Warnings().Should().BeEmpty();

        await RunAsync(Bind(("SlowRequestMs", "1"), ("SlowFileTransferMs", "60000")), "application/json");
        Warnings().Should().ContainSingle();
    }

    [Fact]
    public async Task TheFileTransferLimit_ComesFromTheBoundSettings()
    {
        await RunAsync(Bind(("SlowRequestMs", "1"), ("SlowFileTransferMs", "60000")), "application/json", upload: true);
        Warnings().Should().BeEmpty();

        await RunAsync(Bind(("SlowRequestMs", "60000"), ("SlowFileTransferMs", "1")), "application/json", upload: true);
        Warnings().Should().ContainSingle();
    }

    [Fact]
    public void TheDefaults_AreOneAndThreeSeconds()
    {
        var defaults = new RequestDiagnosticsOptions();

        defaults.SlowRequestMs.Should().Be(1000);
        defaults.SlowFileTransferMs.Should().Be(3000);
    }

    [Theory]
    [InlineData("SlowRequestMs", "0")]
    [InlineData("SlowRequestMs", "-1")]
    [InlineData("SlowFileTransferMs", "0")]
    [InlineData("SlowFileTransferMs", "-1")]
    public void ANonPositiveLimit_FailsTheSettingsCheck_NamingIt(string member, string value)
    {
        var act = () => Bind((member, value));

        act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain(member);
    }

    private async Task RunAsync(RequestDiagnosticsOptions limits, string contentType, bool upload = false)
    {
        var middleware = new RequestDiagnosticsMiddleware(
            async context =>
            {
                context.Response.ContentType = contentType;
                await Task.Delay(TakesMs, TestContext.Current.CancellationToken);
            },
            _logger,
            new TestWebHostEnvironment(),
            Options.Create(limits));

        await middleware.InvokeAsync(new DefaultHttpContext(), new RequestContext { IsUpload = upload });
    }

    private List<LogLevel> Warnings() => _logger.Levels.Where(level => level == LogLevel.Warning).ToList();

    private static RequestDiagnosticsOptions Bind(params (string Member, string Value)[] values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => $"{nameof(RequestDiagnosticsOptions)}:{v.Member}", v => (string?)v.Value))
            .Build();
        var services = new ServiceCollection();
        services.AddAndValidateOptions<RequestDiagnosticsOptions>(configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RequestDiagnosticsOptions>>().Value;
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Levels.Add(logLevel);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Blocks.AspNetCore.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
