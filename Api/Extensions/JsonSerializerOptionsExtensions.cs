using System.Text.Json;
using System.Text.Json.Serialization;

namespace HinataProject.Api.Extensions;

public static class JsonSerializerOptionsExtensions
{
    public static void CopyTo(this JsonSerializerOptions source, JsonSerializerOptions target)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (target is null)
            throw new ArgumentNullException(nameof(target));

        if (target.IsReadOnly)
            throw new InvalidOperationException("Cannot copy to a read-only JsonSerializerOptions target.");

        target.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
        target.PropertyNamingPolicy = source.PropertyNamingPolicy;
        target.ReadCommentHandling = source.ReadCommentHandling;
        target.ReferenceHandler = source.ReferenceHandler;
        target.Encoder = source.Encoder;

#pragma warning disable SYSLIB0020
        if (source.DefaultIgnoreCondition != JsonIgnoreCondition.Never)
        {
            if (target.IgnoreNullValues)
                target.IgnoreNullValues = false;

            target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
        }
        else if (source.IgnoreNullValues)
        {
            if (target.DefaultIgnoreCondition != JsonIgnoreCondition.Never)
                target.DefaultIgnoreCondition = JsonIgnoreCondition.Never;

            target.IgnoreNullValues = source.IgnoreNullValues;
        }
        else
        {
            target.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            target.IgnoreNullValues = false;
        }
#pragma warning restore SYSLIB0020

        target.NumberHandling = source.NumberHandling;
        target.PreferredObjectCreationHandling = source.PreferredObjectCreationHandling;
        target.UnknownTypeHandling = source.UnknownTypeHandling;
        target.UnmappedMemberHandling = source.UnmappedMemberHandling;
        target.DefaultBufferSize = source.DefaultBufferSize;
        target.MaxDepth = source.MaxDepth;
        target.AllowTrailingCommas = source.AllowTrailingCommas;
        target.IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties;
        target.IgnoreReadOnlyFields = source.IgnoreReadOnlyFields;
        target.IncludeFields = source.IncludeFields;
        target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
        target.WriteIndented = source.WriteIndented;

        if (target.TypeInfoResolver != source.TypeInfoResolver)
            target.TypeInfoResolver = source.TypeInfoResolver;

        target.Converters.Clear();
        foreach (var converter in source.Converters)
        {
            target.Converters.Add(converter);
        }
    }
}