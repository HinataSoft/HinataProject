using CaseExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HinataProject.Persistence.Utils;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures the value converter for a specific type in the model builder.
    /// </summary>
    /// <typeparam name="T">The type to configure the value converter for.</typeparam>
    /// <param name="modelBuilder">The builder being used to construct the model for the database context.</param>
    /// <param name="converter">The value converter to use for the specified type.</param>
    /// <returns>The model builder instance.</returns>
    public static ModelBuilder UseValueConverterForType<T>(this ModelBuilder modelBuilder, ValueConverter converter)
    {
        return modelBuilder.UseValueConverterForType(typeof(T), converter);
    }

    /// <summary>
    /// Configures the value converter for a specific type in the model.
    /// </summary>
    /// <param name="type">The type to configure the value converter for.</param>
    /// <param name="modelBuilder">The builder being used to construct the model for the database context.</param>
    /// <param name="converter">The value converter to use for the type.</param>
    /// <returns>The same instance of <see cref="ModelBuilder"/> for method chaining.</returns>
    public static ModelBuilder UseValueConverterForType(this ModelBuilder modelBuilder,
        Type type,
        ValueConverter converter)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // note that entityType.GetProperties() will throw an exception, so we have to use reflection 
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == type);

            foreach (var property in properties)
            {
                modelBuilder.Entity(entityType.Name).Property(property.Name)
                    .HasConversion(converter);
            }
        }

        return modelBuilder;
    }


    public static PropertyBuilder<T> HasEnumToSnakeCaseStringConversion<T>(this PropertyBuilder<T> propertyBuilder)
        where T : Enum
    {
        propertyBuilder.HasConversion(
            item => item.ToString().ToSnakeCase(), // From Enum to string for storage
            item => (T)Enum.Parse(typeof(T), item.ToPascalCase()) // From string back to Enum
        );

        return propertyBuilder;
    }

    public static PropertyBuilder<T[]> HasEnumToSnakeCaseStringConversion<T>(this PropertyBuilder<T[]> propertyBuilder)
        where T : Enum
    {
        propertyBuilder.HasConversion(
            array => array.Select(e => e.ToString().ToSnakeCase()).ToArray(), // From Enum to string for storage
            array => array.Select(s => Enum.Parse(typeof(T), s)).Cast<T>().ToArray() // From string back to Enum
        );

        propertyBuilder.Metadata.SetValueComparer(
            new ValueComparer<T[]>(
                (c1, c2) => Enumerable.SequenceEqual(c1!, c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToArray() // For snapshotting
            ));

        return propertyBuilder;
    }
}