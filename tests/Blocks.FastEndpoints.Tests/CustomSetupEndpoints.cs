using FastEndpoints;
using FluentValidation;

namespace Blocks.FastEndpoints.Tests;

public enum Shade
{
    Light,
    Dark,
}

public sealed record ShadeReply(Shade Shade);

public sealed class ShadeEndpoint : EndpointWithoutRequest<ShadeReply>
{
    public override void Configure()
    {
        Get("/shade");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) => Send.OkAsync(new ShadeReply(Shade.Dark), ct);
}

public sealed class SignUpAddress
{
    public string Street { get; set; } = "";
}

public sealed class SignUpRequest
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public SignUpAddress Address { get; set; } = new();
}

public sealed class SignUpValidator : Validator<SignUpRequest>
{
    public SignUpValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(SignUpMessages.NameRequired);
        RuleFor(x => x.Name).MinimumLength(3).WithMessage(SignUpMessages.NameTooShort);
        RuleFor(x => x.Email).Must(email => email.Contains('@')).WithMessage(SignUpMessages.EmailWithoutAt);
        RuleFor(x => x.Address.Street).NotEmpty().WithMessage(SignUpMessages.StreetRequired);
    }
}

public sealed class SignUpEndpoint : Endpoint<SignUpRequest>
{
    public override void Configure()
    {
        Post("/sign-up");
        AllowAnonymous();
    }

    public override Task HandleAsync(SignUpRequest req, CancellationToken ct) => Send.NoContentAsync(ct);
}

public static class SignUpMessages
{
    public const string NameRequired = "Name is required.";
    public const string NameTooShort = "Name needs three letters.";
    public const string EmailWithoutAt = "Email needs an @.";
    public const string StreetRequired = "Street is required.";
}
