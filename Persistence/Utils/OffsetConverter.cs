using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace HinataProject.Persistence.Utils;

public class OffsetConverter : ValueConverter<Offset, int>
{
    public OffsetConverter() : base(
        v => v.Seconds,          // Convert from Instant to DateTime
        v => Offset.FromSeconds(v)) // Convert from DateTime to Instant
    { }
}