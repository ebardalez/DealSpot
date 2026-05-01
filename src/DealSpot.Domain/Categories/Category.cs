using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Categories;

public sealed class Category : Entity
{
    private Category() { }

    private Category(Guid id, Guid tenantId, string name, string slug, Guid? parentId) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Slug = slug;
        ParentId = parentId;
        IsActive = true;
        SortOrder = 0;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    public static Result<Category> Create(Guid tenantId, string name, string slug, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Category>(CategoryErrors.InvalidName);
        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<Category>(CategoryErrors.InvalidSlug);

        return Result.Success(new Category(Guid.NewGuid(), tenantId, name, slug.ToLowerInvariant(), parentId));
    }

    public void UpdateSortOrder(int sortOrder) => SortOrder = sortOrder;
    public void Deactivate() => IsActive = false;
}
