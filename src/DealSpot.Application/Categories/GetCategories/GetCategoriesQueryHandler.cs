using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;

namespace DealSpot.Application.Categories.GetCategories;

public sealed record GetCategoriesQuery(Guid TenantId) : IQuery<IReadOnlyList<CategoryDto>>;

public sealed record CategoryDto(Guid Id, string Name, string Slug, Guid? ParentId);

public sealed class GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetByTenantAsync(query.TenantId, cancellationToken);

        var dtos = categories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ParentId))
            .ToList();

        return Result.Success<IReadOnlyList<CategoryDto>>(dtos);
    }
}
