using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Categories;

public static class CategoryErrors
{
    public static readonly Error NotFound = new("Category.NotFound", "Category was not found.");
    public static readonly Error InvalidName = new("Category.InvalidName", "Category name cannot be empty.");
    public static readonly Error InvalidSlug = new("Category.InvalidSlug", "Category slug cannot be empty.");
}
