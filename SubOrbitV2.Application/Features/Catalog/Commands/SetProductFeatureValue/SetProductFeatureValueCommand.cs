using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Catalog.Commands.SetProductFeatureValue;

public record SetProductFeatureValueCommand : IRequest<Result<SetProductFeatureValueResponse>>
{
    public Guid ProductId { get; init; }
    public Guid FeatureId { get; init; }

    /// <summary>
    /// Boolean ise "true"/"false", Integer ise "10" gibi metinsel değer.
    /// DataType'a göre validasyondan geçer.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}