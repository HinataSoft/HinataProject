using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using UUIDNext;

namespace HinataProject.Persistence.Utils;

public class GuidV7Generator : ValueGenerator
{
    protected override object? NextValue(EntityEntry entry)
    {
        return UUIDNext.Uuid.NewDatabaseFriendly(Database.PostgreSql);
    }

    public override bool GeneratesTemporaryValues => false;
}