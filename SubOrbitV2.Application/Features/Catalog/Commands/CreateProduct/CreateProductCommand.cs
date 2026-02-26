using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateProduct;

public record CreateProductCommand : IRequest<Result<CreateProductResponse>>
{
    public Guid CatalogCategoryId { get; init; }
    public int Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsHidden { get; init; } = false;
}