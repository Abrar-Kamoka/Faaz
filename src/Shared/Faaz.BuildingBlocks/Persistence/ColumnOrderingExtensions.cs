using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.BuildingBlocks.Persistence;

public static class ColumnOrderingExtensions
{
    // Leading block: identity/sequence columns, always first.
    private static readonly string[] Leading = ["Id", "SrNo"];

    // Trailing block, in this exact order: who/when audit, then soft-delete.
    private static readonly string[] Trailing =
        ["CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted", "DeletedBy", "DeletedAt"];

    // Applies Id, SrNo first / audit + soft-delete last, business columns in between. Pass
    // businessOrder to place the most relevant columns first within that middle block (most callers
    // should); anything on the entity not named in businessOrder falls in afterward, in whatever
    // order EF's metadata would've used by default. Reads the table's actual mapped columns from EF's
    // own metadata, so it stays correct as entities gain or lose properties — nothing to keep in sync.
    // Goes through IMutableProperty.SetColumnOrder directly rather than the PropertyBuilder fluent
    // wrapper, since that overload only binds to the generic PropertyBuilder<T> in this EF version.
    public static void ApplyStandardColumnOrder<TEntity>(
        this EntityTypeBuilder<TEntity> builder, params string[] businessOrder)
        where TEntity : class
    {
        var properties = builder.Metadata.GetProperties().ToDictionary(p => p.Name);

        var order = 0;
        foreach (var name in Leading)
            if (properties.TryGetValue(name, out var prop))
                prop.SetColumnOrder(order++);

        order = 1000;
        foreach (var name in businessOrder)
            if (properties.TryGetValue(name, out var prop))
                prop.SetColumnOrder(order++);

        var placed = new HashSet<string>(Leading);
        placed.UnionWith(Trailing);
        placed.UnionWith(businessOrder);
        foreach (var prop in properties.Values.Where(p => !placed.Contains(p.Name)))
            prop.SetColumnOrder(order++);

        order = 9000;
        foreach (var name in Trailing)
            if (properties.TryGetValue(name, out var prop))
                prop.SetColumnOrder(order++);
    }
}
