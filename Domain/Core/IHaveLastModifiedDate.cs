using NodaTime;

namespace HinataProject.Domain.Core;

public interface IHaveLastModifiedDate
{
    public Instant LastModifiedAt { get; set; }
}